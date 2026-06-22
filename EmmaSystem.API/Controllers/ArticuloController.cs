using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Articulo;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/articulo")]
[Authorize]
public sealed class ArticuloController : ControllerBase
{
    private readonly IArticuloRepository _repo;

    public ArticuloController(IArticuloRepository repo) => _repo = repo;

    /// <summary>
    /// Obtiene el ID del usuario autenticado para auditoría
    /// </summary>
    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")!.Value);

    [HttpGet]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<ArticuloDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("buscar/venta")]
    [Permission(Modules.Cliente, Operations.Consultar)] // Asegúrate de tener Modules.Venta en Constants
    public async Task<ActionResult<IReadOnlyList<ArticuloVentaDto>>> SearchForSales(
        [FromQuery] string? texto,
        CancellationToken ct)
    {
        // ✅ Si texto es null o vacío, pasar string vacío al SP (que debe devolver todos)
        var textoBuscar = string.IsNullOrWhiteSpace(texto) ? "" : texto.Trim();
        var result = await _repo.SearchForSalesAsync(textoBuscar, ct);
        return Ok(result);
    }

    [HttpGet("buscar")]
    [Permission(Modules.Articulo, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<ArticuloDto>>> Search(
        [FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El texto de búsqueda es requerido.");

        var result = await _repo.SearchAsync(texto, ct);
        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Articulo, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] ArticuloSaveDto dto, CancellationToken ct)
    {
        var id = await _repo.CreateAsync(dto, GetIdLogin(), ct);
        return Ok(new { message = "Artículo creado exitosamente.", idArticulo = id });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] ArticuloSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, GetIdLogin(), ct);
        return Ok(new { message = "Artículo actualizado exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Articulo, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, GetIdLogin(), ct);
        return Ok(new { message = "Artículo eliminado exitosamente." });
    }

    [HttpGet("{idArticulo:int}/detalle-precio")]
    [Permission(Modules.Articulo, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<DetalleProductoPrecioDto>>> GetDetallePrecios(
        int idArticulo,
        [FromQuery] int idMedida,
        [FromQuery] string? nombre,
        CancellationToken ct)
    {
        if (idMedida <= 0)
            return BadRequest("El parámetro 'idMedida' es requerido y debe ser mayor a 0.");

        // ✅ Si texto es null o vacío, pasar string vacío al SP (que debe devolver todos)
        var textoBuscar = string.IsNullOrWhiteSpace(nombre) ? "" : nombre.Trim();

        var result = await _repo.GetDetallePreciosAsync(idArticulo, idMedida, textoBuscar, ct);
        return Ok(result);
    }
    [HttpGet("{id:int}")]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<ArticuloDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(new { message = "Producto no encontrado." });

        return Ok(result);
    }
    [HttpGet("secuencia/{tipo}")]
    [Permission(Modules.Articulo, Operations.Crear)]
    public async Task<ActionResult<int>> GetSecuencia(string tipo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tipo) || tipo.Length != 1)
            return BadRequest("El tipo debe ser un solo carácter (ej: 'P' o 'M').");

        var secuencia = await _repo.GetSecuenciaAsync(tipo, ct);
        return Ok(secuencia);
    }
}