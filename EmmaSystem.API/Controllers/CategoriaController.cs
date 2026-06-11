using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Categoria;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/categoria")]
[Authorize]
public sealed class CategoriaController : ControllerBase
{
    private readonly ICategoriaRepository _repo;

    public CategoriaController(ICategoriaRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);

    /// <summary>
    /// Listado completo para administración (tabla CRUD). Top 100 más recientes.
    /// </summary>
    [HttpGet]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Listado simplificado para combos/selectores en Artículos.
    /// Solo devuelve categorías de Tipo 'A' (Artículo).
    /// </summary>
    [HttpGet("articulo")]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> GetForArticulo(CancellationToken ct)
    {
        var result = await _repo.GetForArticuloAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Búsqueda por texto (nombre, descripción o tipo).
    /// </summary>
    [HttpGet("buscar")]
    [Permission(Modules.Articulo, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Search([FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El texto de búsqueda es requerido.");

        var result = await _repo.SearchAsync(texto.Trim(), GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Articulo, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] CategoriaSaveDto dto, CancellationToken ct)
    {
        await _repo.CreateAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Categoría creada exitosamente." });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Categoría actualizada exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Articulo, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Categoría eliminada exitosamente." });
    }
}