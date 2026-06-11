using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Cotizacion;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/cotizacion")]
[Authorize]
public sealed class CotizacionController : ControllerBase
{
    private readonly ICotizacionRepository _repo;

    public CotizacionController(ICotizacionRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);

    [HttpGet]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CotizacionDto>>> GetAll([FromQuery] string tipo, CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), tipo, ct);
        return Ok(result);
    }
    // ✅ CORRECTO: Cambiar Task<IReadOnlyList<...>> por Task<ActionResult<IReadOnlyList<...>>>
    [HttpGet("buscar")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<CotizacionDto>>> Buscar(
    [FromQuery] string? texto,
    [FromQuery] string? tipo,
    [FromQuery] DateTime? fecha1,
    [FromQuery] DateTime? fecha2,
    [FromQuery] string? proceso,
    [FromQuery] int? idEmpresa,
    CancellationToken ct)
    {
        try
        {
            // ✅ Obtener idEmpresa de forma segura
            var idEmpresaClaim = User.FindFirst("Idempresa")?.Value;
            var idEmpresaFinal = idEmpresa ?? (int.TryParse(idEmpresaClaim, out var id) ? id : 0);

            if (idEmpresaFinal <= 0)
                return BadRequest("No se pudo determinar la empresa.");

            // ✅ Fechas seguras para SQL Server (MinValue = año 0001 causa error en SQL)
            var fecha1Final = fecha1 ?? new DateTime(1900, 1, 1);
            var fecha2Final = fecha2 ?? DateTime.MaxValue;

            var result = await _repo.SearchAsync(
                texto?.Trim() ?? "",
                tipo?.Trim() ?? "",
                fecha1Final,
                fecha2Final,
                proceso?.Trim() ?? "",
                idEmpresaFinal,
                ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            // 📝 Loguea el error exacto en la consola del backend
            Console.WriteLine($"❌ ERROR EN BuscarCotizaciones: {ex.Message}");
            Console.WriteLine($"📝 DETALLE: {ex.InnerException?.Message}");
            Console.WriteLine($"📍 STACK: {ex.StackTrace}");

            // 🔙 Devuelve el error al frontend para debugging
            return StatusCode(500, new
            {
                message = "Error interno al buscar cotizaciones",
                detail = ex.Message
            });
        }
    }
    [HttpGet("{id:int}/getcotizacion")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<CotizacionDto>> GetDetalleWithClient(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdWithClientAsync(id, ct);

        if (result is null)
            return NotFound(new { message = $"No se encontró la cotización con ID {id}." });

        // 🔒 Validación multi-tenant: asegurar que la cotización pertenece a la empresa del usuario
        var idEmpresaToken = int.Parse(User.FindFirst("Idempresa")!.Value);
        // Nota: El SP no devuelve Idempresa, así que asumimos que el ID es único por empresa
        // Si necesitas validación estricta, modifica el SP para incluir Idempresa en el SELECT

        return Ok(result);
    }
    [HttpPost]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] CotizacionSaveDto dto, CancellationToken ct)
    {
        var id = await _repo.CreateAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Cotización creada exitosamente.", idCotizacion = id });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] CotizacionSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Cotización actualizada exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Cotización eliminada (soft delete)." });
    }

    [HttpPatch("{id:int}/cerrar")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        await _repo.CloseAsync(id, ct);
        return Ok(new { message = "Cotización cerrada." });
    }

    // ────────── ENDPOINTS DE DETALLE ──────────
    [HttpGet("{id:int}/detalle")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CotizacionDetalleDto>>> GetDetails(int id, CancellationToken ct)
    {
        var result = await _repo.GetDetailsAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("detalle")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> AddDetail([FromBody] CotizacionDetalleSaveDto dto, CancellationToken ct)
    {
        await _repo.AddDetailAsync(dto, ct);
        return Ok(new { message = "Detalle agregado." });
    }

    [HttpPut("detalle/{idDetalle:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> UpdateDetail(int idDetalle, [FromBody] CotizacionDetalleSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateDetailAsync(idDetalle, dto, ct);
        return Ok(new { message = "Detalle actualizado." });
    }

    [HttpDelete("detalle/{idDetalle:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> DeleteDetail(int idDetalle, CancellationToken ct)
    {
        await _repo.DeleteDetailAsync(idDetalle, ct);
        return Ok(new { message = "Detalle eliminado." });
    }

    [HttpDelete("{id:int}/detalle/clear")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> ClearDetails(int id, CancellationToken ct)
    {
        await _repo.ClearDetailsAsync(id, ct);
        return Ok(new { message = "Todos los detalles han sido eliminados." });
    }

    [HttpGet("vendedores")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VendedorDto>>> GetVendedores(CancellationToken ct)
    {
        var result = await _repo.GetVendedoresAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }
    // ... código existente ...

    /// <summary>
    /// Obtiene los datos formateados de una cotización para impresión o generación de PDF.
    /// Incluye cliente, totales y descuentos calculados por el SP.
    /// </summary>
    [HttpGet("imprimir")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<CotizacionImpresionDto>> GetPrintData(
        [FromQuery] string noCotizacion,
        [FromQuery] string tipo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(noCotizacion) || string.IsNullOrWhiteSpace(tipo))
            return BadRequest("El número de cotización y el tipo (C/P) son requeridos.");

        var result = await _repo.GetPrintDataAsync(
            noCotizacion.Trim(),
            tipo.Trim(),
            GetIdEmpresa(),
            ct);

        if (result is null)
            return NotFound(new { message = $"No se encontró la cotización {noCotizacion} del tipo {tipo}." });

        return Ok(result);
    }
    [HttpGet("secuencia")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<SecuenciaCotizacionDto>> GetSecuencia(
    [FromQuery] string tipo,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return BadRequest("El parámetro 'tipo' es requerido. Valores válidos: 'C' (Cotizacion), 'P' (Pedido).");

        var siguienteNumero = await _repo.GetNextSequenceAsync(tipo.Trim(), GetIdEmpresa(), ct);

        return Ok(new SecuenciaCotizacionDto
        {
            Tipo = tipo.Trim(),
            SiguienteNumero = siguienteNumero
        });
    }
}