using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Data;
using MinimalApi.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace MinimalApi.Controller;

[ApiController]
[Route("usuarios")]
public class UsuariosController(AppDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    private const string AdminApiKeyHeader = "X-Admin-Api-Key";

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
            return NotFound("Usuário não encontrado.");
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
            return BadRequest("Nome, login e senha são obrigatórios.");
        }

        if (senha.Length < 6)
        {
            return BadRequest("A senha deve ter pelo menos 6 caracteres.");
        }

        var loginJaExiste = await dbContext.Usuarios.AnyAsync(x => x.Login == login);
        if (loginJaExiste)
        {
            return Conflict("Já existe um usuário com esse login.");
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
            return BadRequest("Login e senha são obrigatórios.");
        }

        var usuario = await dbContext.Usuarios
            .FirstOrDefaultAsync(x => x.Login == login);

        if (usuario is null)
        {
            return NotFound("Usuário não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(usuario.SenhaHash) ||
            string.IsNullOrWhiteSpace(usuario.SenhaSalt) ||
            !SenhaConfere(senha, usuario.SenhaSalt, usuario.SenhaHash))
        {
            return Unauthorized("Login ou senha inválidos.");
        }

        return Ok(ToUsuarioResponse(usuario));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioRequest request)
    {
        var unauthorizedResult = ValidarAdminApiKey();
        if (unauthorizedResult is not null)
        {
            return unauthorizedResult;
        }

        var usuario = await dbContext.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound("Usuário não encontrado.");
        }

        var nome = request.Nome.Trim();
        var login = request.Login.Trim().ToLowerInvariant();
        var senha = request.Senha.Trim();

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(login))
        {
            return BadRequest("Nome e login são obrigatórios.");
        }

        var loginJaExiste = await dbContext.Usuarios
            .AnyAsync(x => x.Id != id && x.Login == login);

        if (loginJaExiste)
        {
            return Conflict("Já existe um usuário com esse login.");
        }

        usuario.Nome = nome;
        usuario.Login = login;

        if (!string.IsNullOrWhiteSpace(senha))
        {
            if (senha.Length < 6)
            {
                return BadRequest("A senha deve ter pelo menos 6 caracteres.");
            }

            var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            usuario.SenhaSalt = salt;
            usuario.SenhaHash = GerarHashSenha(senha, salt);
        }

        await dbContext.SaveChangesAsync();

        return Ok(ToUsuarioResponse(usuario));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var unauthorizedResult = ValidarAdminApiKey();
        if (unauthorizedResult is not null)
        {
            return unauthorizedResult;
        }

        var usuario = await dbContext.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound("Usuário não encontrado.");
        }

        dbContext.Usuarios.Remove(usuario);
        await dbContext.SaveChangesAsync();

        return NoContent();
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
}
