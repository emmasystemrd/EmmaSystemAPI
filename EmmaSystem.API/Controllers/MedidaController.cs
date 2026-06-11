using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Medida;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/medida")]
[Authorize]
public sealed class MedidaController : ControllerBase
{
    private readonly IMedidaRepository _repo;

    public MedidaController(IMedidaRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);

    /// <summary>
    /// Listado completo para administración (tabla CRUD).
    /// </summary>
    [HttpGet]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<MedidaDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Listado simplificado para combos/selectores en Artículos.
    /// Devuelve formato "Mayor (Contenido)".
    /// </summary>
    [HttpGet("articulo")]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<MedidaArticuloDto>>> GetForArticulo(CancellationToken ct)
    {
        var result = await _repo.GetForArticuloAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Búsqueda por texto (nombre, abreviatura, descripción o contenido numérico).
    /// </summary>
    [HttpGet("buscar")]
    [Permission(Modules.Articulo, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<MedidaDto>>> Search([FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El texto de búsqueda es requerido.");

        var result = await _repo.SearchAsync(texto.Trim(), GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtener una medida por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<MedidaDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró la medida con ID {id}." });

        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Articulo, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] MedidaSaveDto dto, CancellationToken ct)
    {
        await _repo.CreateAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Medida creada exitosamente." });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] MedidaSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Medida actualizada exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Articulo, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Medida eliminada exitosamente." });
    }
    
    [HttpGet("detalle-producto/{idproducto:int}")]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<MedidaDetalleProductoDto>>> GetDetallesByProducto(
        int idproducto,
        CancellationToken ct)
    {
        var result = await _repo.GetDetallesByProductoAsync(idproducto, ct);
        return Ok(result);
    }

}