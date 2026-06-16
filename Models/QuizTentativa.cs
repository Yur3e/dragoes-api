namespace MinimalApi.Models;

public class QuizTentativa
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int DragaoId { get; set; }
    public Dragao? Dragao { get; set; }
    public string RespostaInformada { get; set; } = string.Empty;
    public bool Acertou { get; set; }
    public int TempoRespostaSegundos { get; set; }
    public int PontosGanhos { get; set; }
    public DateTime RespondidaEmUtc { get; set; }
}
