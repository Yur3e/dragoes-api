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
            .ToListAsync();

        return Ok(dragoes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var dragao = await dbContext.Dragoes.FindAsync(id);

        if (dragao is null)
        {
            return NotFound("Dragao nao encontrado.");
        }

        return Ok(dragao);
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

        return CreatedAtAction(nameof(BuscarPorId), new { id = dragao.Id }, dragao);
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
            return NotFound("Dragao nao encontrado.");
        }

        dragao.Nome = dragaoAtualizado.Nome.Trim();
        dragao.ImagemUrl = dragaoAtualizado.ImagemUrl.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(dragao);
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
            return NotFound("Dragao nao encontrado.");
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
            return StatusCode(StatusCodes.Status500InternalServerError, "Chave administrativa nao configurada.");
        }

        if (!Request.Headers.TryGetValue(AdminApiKeyHeader, out var providedApiKey))
        {
            return Unauthorized("Chave administrativa nao informada.");
        }

        if (!string.Equals(providedApiKey.ToString(), configuredApiKey, StringComparison.Ordinal))
        {
            return Unauthorized("Chave administrativa invalida.");
        }

        return null;
    }
}
