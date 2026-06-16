namespace MinimalApi.Models;

public static class ListaDragoes
{
    public static List<Dragao> Itens { get; } =
    [
        new Dragao { Id = 1, Nome = "Smaug", Cor = "Vermelho", Elemento = "Fogo", Idade = 300 },
        new Dragao { Id = 2, Nome = "Drogon", Cor = "Preto", Elemento = "Fogo", Idade = 12 },
        new Dragao { Id = 3, Nome = "Falkor", Cor = "Branco", Elemento = "Vento", Idade = 150 }
    ];
}
