using MinimalApi.Models;

namespace MinimalApi.Service;

public class DragaoService
{
    public List<Dragao> Listar()
    {
        return ListaDragoes.Itens;
    }

    public Dragao? BuscarPorId(int id)
    {
        return ListaDragoes.Itens.FirstOrDefault(d => d.Id == id);
    }

    public Dragao Criar(DragaoRequest novoDragao)
    {
        var proximoId = ListaDragoes.Itens.Count == 0 ? 1 : ListaDragoes.Itens.Max(d => d.Id) + 1;

        var dragao = new Dragao
        {
            Id = proximoId,
            Nome = novoDragao.Nome,
            Cor = novoDragao.Cor,
            Elemento = novoDragao.Elemento,
            Idade = novoDragao.Idade
        };

        ListaDragoes.Itens.Add(dragao);

        return dragao;
    }

    public Dragao? Atualizar(int id, DragaoRequest dragaoAtualizado)
    {
        var dragao = BuscarPorId(id);

        if (dragao is null)
        {
            return null;
        }

        dragao.Nome = dragaoAtualizado.Nome;
        dragao.Cor = dragaoAtualizado.Cor;
        dragao.Elemento = dragaoAtualizado.Elemento;
        dragao.Idade = dragaoAtualizado.Idade;

        return dragao;
    }

    public bool Remover(int id)
    {
        var dragao = BuscarPorId(id);

        if (dragao is null)
        {
            return false;
        }

        ListaDragoes.Itens.Remove(dragao);
        return true;
    }
}
