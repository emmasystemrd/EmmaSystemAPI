using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Articulo;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/articulo/{idArticulo:int}/presentaciones")]
[Authorize]
public sealed class DetalleProductoController : ControllerBase
{
    private readonly IDetalleProductoRepository _repo;

    public DetalleProductoController(IDetalleProductoRepository repo) => _repo = repo;

    /// <summary>
    /// Obtiene todas las presentaciones de un artículo específico
    /// </summary>
    [HttpGet]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<DetalleProductoDto>>> GetAll(int idArticulo, CancellationToken ct)
    {
        var result = await _repo.GetByIdArticuloAsync(idArticulo, ct);
        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Create(int idArticulo, [FromBody] DetalleProductoSaveDto dto, CancellationToken ct)
    {
        // 🔍 TEMPORAL: Ver errores específicos
        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new { message = "Errores de validación", errores });
        }

        if (idArticulo != dto.Idarticulo)
            return BadRequest("El ID del artículo en la ruta no coincide con el del cuerpo.");

        await _repo.CreateAsync(dto, ct);
        return Ok(new { message = "Presentación agregada exitosamente." });
    }

    /// <summary>
    /// Actualiza una presentación existente
    /// </summary>
    [HttpPut("{idDetalle:int}")]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Update(int idArticulo, int idDetalle, [FromBody] DetalleProductoSaveDto dto, CancellationToken ct)
    {
        if (idArticulo != dto.Idarticulo)
            return BadRequest("El ID del artículo en la ruta no coincide con el del cuerpo.");

        await _repo.UpdateAsync(idDetalle, dto, ct);
        return Ok(new { message = "Presentación actualizada exitosamente." });
    }

    /// <summary>
    /// Elimina una presentación del artículo
    /// </summary>
    [HttpDelete("{idDetalle:int}")]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Delete(int idArticulo, int idDetalle, CancellationToken ct)
    {
        await _repo.DeleteAsync(idDetalle, ct);
        return Ok(new { message = "Presentación eliminada exitosamente." });
    }
}