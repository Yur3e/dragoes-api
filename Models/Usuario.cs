namespace MinimalApi.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string SenhaSalt { get; set; } = string.Empty;
    public int TotalAcertos { get; set; }
    public int PontuacaoTotal { get; set; }

    public List<QuizTentativa> Tentativas { get; set; } = [];
}
