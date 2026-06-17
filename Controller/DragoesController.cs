using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Data;
using MinimalApi.Models;
using Microsoft.Extensions.Configuration;

namespace MinimalApi.Controller;

[ApiController]
[Route("dragoes")]
public class DragoesController(AppDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    private const string AdminApiKeyHeader = "X-Admin-Api-Key";

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var dragoes = await dbContext.Dragoes
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                ImagemUrl = "/dragoes/" + x.Id + "/imagem"
            })
            .ToListAsync();

        return Ok(dragoes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var dragao = await dbContext.Dragoes.FindAsync(id);

        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        return Ok(ToDragaoResponse(dragao));
    }

    [HttpGet("{id:int}/imagem")]
    public async Task<IActionResult> ObterImagem(int id)
    {
        var dragao = await dbContext.Dragoes.FindAsync(id);
        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        var imagemUrl = dragao.ImagemUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(imagemUrl))
        {
            return NotFound("Imagem do dragão não encontrada.");
        }

        if (imagemUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var separatorIndex = imagemUrl.IndexOf(',');
            if (separatorIndex < 0)
            {
                return BadRequest("Imagem do dragão em formato inválido.");
            }

            var metadata = imagemUrl[..separatorIndex];
            var base64 = imagemUrl[(separatorIndex + 1)..];
            var mediaType = metadata.Split(';')[0].Replace("data:", "", StringComparison.OrdinalIgnoreCase);

            try
            {
                var bytes = Convert.FromBase64String(base64);
                return File(bytes, string.IsNullOrWhiteSpace(mediaType) ? "application/octet-stream" : mediaType);
            }
            catch (FormatException)
            {
                return BadRequest("Imagem do dragão em formato inválido.");
            }
        }

        return Redirect(imagemUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] DragaoRequest novoDragao)
    {
        var unauthorizedResult = ValidarAdminApiKey();
        if (unauthorizedResult is not null)
        {
            return unauthorizedResult;
        }

        var dragao = new Dragao
        {
            Nome = novoDragao.Nome.Trim(),
            ImagemUrl = novoDragao.ImagemUrl.Trim()
        };

        dbContext.Dragoes.Add(dragao);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(BuscarPorId), new { id = dragao.Id }, ToDragaoResponse(dragao));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] DragaoRequest dragaoAtualizado)
    {
        var unauthorizedResult = ValidarAdminApiKey();
        if (unauthorizedResult is not null)
        {
            return unauthorizedResult;
        }

        var dragao = await dbContext.Dragoes.FindAsync(id);

        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        dragao.Nome = dragaoAtualizado.Nome.Trim();
        dragao.ImagemUrl = dragaoAtualizado.ImagemUrl.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(ToDragaoResponse(dragao));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var unauthorizedResult = ValidarAdminApiKey();
        if (unauthorizedResult is not null)
        {
            return unauthorizedResult;
        }

        var dragao = await dbContext.Dragoes.FindAsync(id);

        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        dbContext.Dragoes.Remove(dragao);
        await dbContext.SaveChangesAsync();

        return NoContent();
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

    private static object ToDragaoResponse(Dragao dragao)
    {
        return new
        {
            dragao.Id,
            dragao.Nome,
            ImagemUrl = $"/dragoes/{dragao.Id}/imagem"
        };
    }
}
