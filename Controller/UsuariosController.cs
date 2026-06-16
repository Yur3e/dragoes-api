using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Data;
using MinimalApi.Models;
using System.Security.Cryptography;

namespace MinimalApi.Controller;

[ApiController]
[Route("usuarios")]
public class UsuariosController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await dbContext.Usuarios
            .OrderByDescending(x => x.PontuacaoTotal)
            .ThenByDescending(x => x.TotalAcertos)
            .ThenBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.Login,
                x.TotalAcertos,
                x.PontuacaoTotal
            })
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

        return Ok(ToUsuarioResponse(usuario));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] UsuarioRequest request)
    {
        var nome = request.Nome.Trim();
        var login = request.Login.Trim().ToLowerInvariant();
        var senha = request.Senha.Trim();

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(senha))
        {
            return BadRequest("Nome, login e senha sao obrigatorios.");
        }

        if (senha.Length < 6)
        {
            return BadRequest("A senha deve ter pelo menos 6 caracteres.");
        }

        var loginJaExiste = await dbContext.Usuarios.AnyAsync(x => x.Login == login);
        if (loginJaExiste)
        {
            return Conflict("Ja existe um usuario com esse login.");
        }

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var usuario = new Usuario
        {
            Nome = nome,
            Login = login,
            SenhaSalt = salt,
            SenhaHash = GerarHashSenha(senha, salt)
        };

        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(BuscarPorId), new { id = usuario.Id }, ToUsuarioResponse(usuario));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UsuarioLoginRequest request)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var senha = request.Senha.Trim();

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            return BadRequest("Login e senha sao obrigatorios.");
        }

        var usuario = await dbContext.Usuarios
            .FirstOrDefaultAsync(x => x.Login == login);

        if (usuario is null)
        {
            return NotFound("Usuario nao encontrado.");
        }

        if (string.IsNullOrWhiteSpace(usuario.SenhaHash) ||
            string.IsNullOrWhiteSpace(usuario.SenhaSalt) ||
            !SenhaConfere(senha, usuario.SenhaSalt, usuario.SenhaHash))
        {
            return Unauthorized("Login ou senha invalidos.");
        }

        return Ok(ToUsuarioResponse(usuario));
    }

    private static object ToUsuarioResponse(Usuario usuario)
    {
        return new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Login,
            usuario.TotalAcertos,
            usuario.PontuacaoTotal
        };
    }

    private static string GerarHashSenha(string senha, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return Convert.ToBase64String(hash);
    }

    private static bool SenhaConfere(string senha, string salt, string senhaHash)
    {
        var hashInformado = GerarHashSenha(senha, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hashInformado),
            Convert.FromBase64String(senhaHash));
    }
}
