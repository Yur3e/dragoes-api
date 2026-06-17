using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Data;
using MinimalApi.Models;

namespace MinimalApi.Controller;

[ApiController]
[Route("quiz")]
public class QuizController(AppDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    private const string AdminApiKeyHeader = "X-Admin-Api-Key";

    [HttpGet("pergunta")]
    public async Task<IActionResult> ObterPergunta([FromQuery] int? usuarioId)
    {
        var dragoesQuery = dbContext.Dragoes.AsQueryable();

        if (usuarioId.HasValue)
        {
            var usuarioExiste = await dbContext.Usuarios.AnyAsync(x => x.Id == usuarioId.Value);
            if (!usuarioExiste)
            {
                return NotFound("Usuário não encontrado.");
            }

            var dragoesJaPontuados = await dbContext.QuizTentativas
                .Where(x => x.UsuarioId == usuarioId.Value && x.Acertou)
                .Select(x => x.DragaoId)
                .Distinct()
                .ToListAsync();

            dragoesQuery = dragoesQuery.Where(x => !dragoesJaPontuados.Contains(x.Id));
        }

        var totalDragoes = await dragoesQuery.CountAsync();
        if (totalDragoes == 0)
        {
            return NotFound(usuarioId.HasValue
                ? "Você já pontuou em todos os dragões cadastrados."
                : "Nenhum dragão cadastrado para o quiz.");
        }

        var indice = Random.Shared.Next(totalDragoes);
        var pergunta = await dragoesQuery
            .OrderBy(x => x.Id)
            .Skip(indice)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                ImagemUrl = $"/dragoes/{x.Id}/imagem",
                Pergunta = "Qual é o nome deste dragão?"
            })
            .FirstAsync();

        return Ok(pergunta);
    }

    [HttpPost("responder")]
    public async Task<IActionResult> Responder([FromBody] QuizRespostaRequest request)
    {
        var usuario = await dbContext.Usuarios.FindAsync(request.UsuarioId);
        if (usuario is null)
        {
            return NotFound("Usuário não encontrado.");
        }

        var dragao = await dbContext.Dragoes.FindAsync(request.DragaoId);
        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        if (request.TempoRespostaSegundos < 0)
        {
            return BadRequest("O tempo de resposta não pode ser negativo.");
        }

        var jaPontuouNesseDragao = await dbContext.QuizTentativas
            .AnyAsync(x => x.UsuarioId == request.UsuarioId && x.DragaoId == request.DragaoId && x.Acertou);

        if (jaPontuouNesseDragao)
        {
            return Conflict("Você já pontuou nesse dragão.");
        }

        var acertou = NormalizarTexto(request.RespostaInformada) == NormalizarTexto(dragao.Nome);
        var pontosGanhos = CalcularPontos(acertou, request.TempoRespostaSegundos);

        var tentativa = new QuizTentativa
        {
            UsuarioId = usuario.Id,
            DragaoId = dragao.Id,
            RespostaInformada = request.RespostaInformada.Trim(),
            Acertou = acertou,
            TempoRespostaSegundos = request.TempoRespostaSegundos,
            PontosGanhos = pontosGanhos,
            RespondidaEmUtc = DateTime.UtcNow
        };

        dbContext.QuizTentativas.Add(tentativa);

        if (acertou)
        {
            usuario.TotalAcertos += 1;
            usuario.PontuacaoTotal += pontosGanhos;
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            tentativa.Acertou,
            tentativa.PontosGanhos,
            usuario.TotalAcertos,
            usuario.PontuacaoTotal,
            Mensagem = acertou
                ? "Resposta correta."
                : $"Resposta incorreta. O nome certo era {dragao.Nome}."
        });
    }

    [HttpGet("ranking")]
    public async Task<IActionResult> RankingGlobal()
    {
        var ranking = await ConsultarRankingAsync();
        return Ok(ranking);
    }

    [HttpPut("ranking/recalcular")]
    public async Task<IActionResult> RecalcularRanking()
    {
        var unauthorizedResult = ValidarAdminApiKey();
        if (unauthorizedResult is not null)
        {
            return unauthorizedResult;
        }

        var usuarios = await dbContext.Usuarios
            .Include(x => x.Tentativas)
            .ToListAsync();

        foreach (var usuario in usuarios)
        {
            usuario.TotalAcertos = usuario.Tentativas.Count(x => x.Acertou);
            usuario.PontuacaoTotal = usuario.Tentativas.Sum(x => x.PontosGanhos);
        }

        await dbContext.SaveChangesAsync();

        var ranking = await ConsultarRankingAsync();

        return Ok(new
        {
            Mensagem = "Ranking recalculado com sucesso.",
            TotalUsuariosAtualizados = usuarios.Count,
            Ranking = ranking
        });
    }

    private async Task<List<object>> ConsultarRankingAsync()
    {
        return await dbContext.Usuarios
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.Login,
                x.TotalAcertos,
                x.PontuacaoTotal,
                TempoMedioResposta = x.Tentativas.Count == 0
                    ? 0
                    : x.Tentativas.Average(t => t.TempoRespostaSegundos)
            })
            .OrderByDescending(x => x.PontuacaoTotal)
            .ThenByDescending(x => x.TotalAcertos)
            .ThenBy(x => x.TempoMedioResposta)
            .Select(x => (object)new
            {
                x.Id,
                x.Nome,
                x.Login,
                x.TotalAcertos,
                x.PontuacaoTotal,
                x.TempoMedioResposta
            })
            .ToListAsync();
    }

    private IActionResult? ValidarAdminApiKey()
    {
        var configuredApiKey = configuration["Admin:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Chave administrativa não configurada.");
        }

        if (!Request.Headers.TryGetValue(AdminApiKeyHeader, out var providedApiKey))
        {
            return Unauthorized("Chave administrativa não informada.");
        }

        if (!string.Equals(providedApiKey.ToString(), configuredApiKey, StringComparison.Ordinal))
        {
            return Unauthorized("Chave administrativa inválida.");
        }

        return null;
    }

    private static int CalcularPontos(bool acertou, int tempoRespostaSegundos)
    {
        if (!acertou)
        {
            return 0;
        }

        return tempoRespostaSegundos switch
        {
            <= 2 => 30,
            <= 4 => 25,
            <= 6 => 20,
            <= 8 => 15,
            <= 10 => 10,
            <= 15 => 5,
            _ => 1
        };
    }

    private static string NormalizarTexto(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var textoNormalizado = valor.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var caractere in textoNormalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(caractere));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
