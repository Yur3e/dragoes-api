using Microsoft.AspNetCore.Mvc;
using MinimalApi.Models;
using MinimalApi.Service;

namespace MinimalApi.Controller;

[ApiController]
[Route("dragoes")]
public class DragoesController : ControllerBase
{
    private readonly DragaoService _dragaoService;

    public DragoesController(DragaoService dragaoService)
    {
        _dragaoService = dragaoService;
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(_dragaoService.Listar());
    }

    [HttpGet("{id:int}")]
    public IActionResult BuscarPorId(int id)
    {
        var dragao = _dragaoService.BuscarPorId(id);

        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        return Ok(dragao);
    }

    [HttpPost]
    public IActionResult Criar([FromBody] DragaoRequest novoDragao)
    {
        var dragao = _dragaoService.Criar(novoDragao);
        return CreatedAtAction(nameof(BuscarPorId), new { id = dragao.Id }, dragao);
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] DragaoRequest dragaoAtualizado)
    {
        var dragao = _dragaoService.Atualizar(id, dragaoAtualizado);

        if (dragao is null)
        {
            return NotFound("Dragão não encontrado.");
        }

        return Ok(dragao);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remover(int id)
    {
        var removido = _dragaoService.Remover(id);

        if (!removido)
        {
            return NotFound("Dragão não encontrado.");
        }

        return NoContent();
    }
}
