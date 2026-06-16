using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Data;
using MinimalApi.Models;

namespace MinimalApi.Controller;

[ApiController]
[Route("quiz")]
public class QuizController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("pergunta")]
    public async Task<IActionResult> ObterPergunta()
    {
        var totalDragoes = await dbContext.Dragoes.CountAsync();
        if (totalDragoes == 0)
        {
            return NotFound("Nenhum dragao cadastrado para o quiz.");
        }

        var indice = Random.Shared.Next(totalDragoes);
        var dragao = await dbContext.Dragoes
            .OrderBy(x => x.Id)
            .Skip(indice)
            .Select(x => new
            {
                x.Id,
                x.ImagemUrl
            })
            .FirstAsync();

        return Ok(dragao);
    }

    [HttpPost("responder")]
    public async Task<IActionResult> Responder([FromBody] QuizRespostaRequest request)
    {
        var usuario = await dbContext.Usuarios.FindAsync(request.UsuarioId);
        if (usuario is null)
        {
            return NotFound("Usuario nao encontrado.");
        }

        var dragao = await dbContext.Dragoes.FindAsync(request.DragaoId);
        if (dragao is null)
        {
            return NotFound("Dragao nao encontrado.");
        }

        if (request.TempoRespostaSegundos < 0)
        {
            return BadRequest("O tempo de resposta nao pode ser negativo.");
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
            usuario.PontuacaoTotal
        });
    }

    [HttpGet("ranking")]
    public async Task<IActionResult> RankingGlobal()
    {
        var ranking = await dbContext.Usuarios
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
            .OrderByDescending(x => x.TotalAcertos)
            .ThenByDescending(x => x.PontuacaoTotal)
            .ThenBy(x => x.TempoMedioResposta)
            .ToListAsync();

        return Ok(ranking);
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
