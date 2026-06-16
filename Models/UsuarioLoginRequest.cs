namespace MinimalApi.Models;

public class UsuarioLoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}
