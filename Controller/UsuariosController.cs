using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Data;
using MinimalApi.Models;

namespace MinimalApi.Controller;

[ApiController]
[Route("usuarios")]
public class UsuariosController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await dbContext.Usuarios
            .OrderByDescending(x => x.TotalAcertos)
            .ThenByDescending(x => x.PontuacaoTotal)
            .ThenBy(x => x.Nome)
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var usuario = await dbContext.Usuarios.FindAsync(id);

        if (usuario is null)
        {
            return NotFound("Usuario nao encontrado.");
        }

        return Ok(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] UsuarioRequest request)
    {
        var nome = request.Nome.Trim();
        var login = request.Login.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(login))
        {
            return BadRequest("Nome e login sao obrigatorios.");
        }

        var loginJaExiste = await dbContext.Usuarios.AnyAsync(x => x.Login == login);
        if (loginJaExiste)
        {
            return Conflict("Ja existe um usuario com esse login.");
        }

        var usuario = new Usuario
        {
            Nome = nome,
            Login = login
        };

        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(BuscarPorId), new { id = usuario.Id }, usuario);
    }
}
