namespace MinimalApi.Models;

public class QuizRespostaRequest
{
    public int UsuarioId { get; set; }
    public int DragaoId { get; set; }
    public string RespostaInformada { get; set; } = string.Empty;
    public int TempoRespostaSegundos { get; set; }
}
