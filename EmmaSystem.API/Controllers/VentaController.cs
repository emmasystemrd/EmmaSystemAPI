using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Venta;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/venta")]
[Authorize]
public sealed class VentaController : ControllerBase
{
    private readonly IVentaRepository _repo;

    public VentaController(IVentaRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");
    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")?.Value ?? "1");

    // === LISTADOS ===

    /// <summary>
    /// Obtiene las últimas 100 ventas activas de la empresa del usuario.
    /// </summary>
    [HttpGet("listado")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaListadoDto>>> GetVentasActivas(CancellationToken ct)
    {
        var result = await _repo.GetVentasActivasAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene ventas pendientes de cobro (contado) para la empresa del usuario.
    /// </summary>
    [HttpGet("pendientes")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaPendienteDto>>> GetVentasPendientes(CancellationToken ct)
    {
        var result = await _repo.GetVentasPendientesAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Busca ventas pendientes por nombre de cliente.
    /// </summary>
    [HttpGet("pendientes/buscar")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaPendienteDto>>> SearchPendientes(
        [FromQuery] string texto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El parámetro 'texto' es requerido.");

        var result = await _repo.SearchVentasPendientesAsync(GetIdEmpresa(), texto.Trim(), ct);
        return Ok(result);
    }

    // === CARGA DE DETALLE ===

    /// <summary>
    /// Obtiene el detalle completo de una venta por NCF.
    /// </summary>
    [HttpGet("ncf/{ncf}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<VentaDetalleDto>> GetByNcf(string ncf, CancellationToken ct)
    {
        var result = await _repo.GetByNcfAsync(GetIdEmpresa(), ncf, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró la venta con NCF {ncf}." });

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle completo de una venta por ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<VentaDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(GetIdEmpresa(), id, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró la venta con ID {id}." });

        return Ok(result);
    }

    /// <summary>
    /// Obtiene información de pago de una venta por NCF.
    /// </summary>
    [HttpGet("{ncf}/pago")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<VentaPagoDto>> GetPagoInfo(string ncf, CancellationToken ct)
    {
        var result = await _repo.GetPagoInfoAsync(GetIdEmpresa(), ncf, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró información de pago para NCF {ncf}." });

        return Ok(result);
    }

    // === CRUD ===

    /// <summary>
    /// Crea una nueva venta.
    /// </summary>
    [HttpPost]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult<int>> Create([FromBody] VentaSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var idVenta = await _repo.InsertAsync(dto, GetIdEmpresa(), ct);
        return CreatedAtAction(nameof(GetById), new { id = idVenta }, idVenta);
    }

    /// <summary>
    /// Actualiza una venta existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] VentaSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _repo.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>
    /// Anula una venta (cambia Estado a 'E').
    /// </summary>
    [HttpDelete("{id:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, GetIdLogin(), ct);
        return NoContent();
    }

    // === BÚSQUEDA AVANZADA ===

    /// <summary>
    /// Búsqueda avanzada de ventas por columna y rango de fechas.
    /// </summary>
    [HttpGet("buscar")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaListadoDto>>> Search(
        [FromQuery] DateTime fecha1,
        [FromQuery] DateTime fecha2,
        [FromQuery] bool isFecha,
        [FromQuery] string texto,
        [FromQuery] string columna,
        CancellationToken ct)
    {
        var result = await _repo.SearchByColumnAsync(
            fecha1, fecha2, isFecha, texto, columna, GetIdEmpresa(), ct);

        return Ok(result);
    }
    /// <summary>
    /// Genera el siguiente NCF secuencial según el tipo de comprobante
    /// </summary>
    [HttpPost("generar-ncf")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<ActionResult<NcfSecuenciaDto>> GenerarNcf(
        [FromQuery] string tipo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return BadRequest(new { message = "El tipo de comprobante es requerido." });

        var result = await _repo.GenerarSecuenciaNcfAsync(tipo.Trim(), ct);

        if (result is null || result.Desde == 0)
        {
            return BadRequest(new
            {
                message = $"No puede facturar Comprobante {tipo}. Solicítalo en la oficina virtual de la DGII."
            });
        }

        // Validar vencimiento
        if (result.Vencimiento.HasValue && result.Vencimiento.Value < DateTime.Today)
        {
            return BadRequest(new
            {
                message = $"La secuencia de comprobantes {tipo} está vencida desde {result.Vencimiento.Value:dd/MM/yyyy}. Renueve en la DGII."
            });
        }

        // Validar que no esté cerca del límite (80% usado)
        var porcentajeUsado = (double)(result.Secuencia - result.Desde) / (double)(result.Hasta - result.Desde) * 100;
        if (porcentajeUsado >= 80 && !string.IsNullOrEmpty(result.Aviso))
        {
            // Retornar con warning pero permitir continuar
            result.Aviso = $"⚠️ {result.Aviso} (Uso: {porcentajeUsado:F1}%)";
        }

        return Ok(result);
    }
    // ═══════════════════════════════════════════════════════════════
    // ENDPOINTS DE DETALLE DE VENTA
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los detalles (productos) de una venta específica
    /// </summary>
    [HttpGet("{idVenta:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<VentaDetalleItemDto>>> GetDetalles(
        int idVenta,
        CancellationToken ct)
    {
        var result = await _repo.GetDetallesByVentaAsync(idVenta, ct);
        return Ok(result);
    }

    /// <summary>
    /// Agrega un detalle (producto) a una venta existente
    /// </summary>
    [HttpPost("{idVenta:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult> InsertarDetalle(
        int idVenta,
        [FromBody] VentaDetalleItemDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _repo.InsertarDetalleAsync(dto, idVenta, ct);
        return Ok(new { message = "Detalle agregado correctamente." });
    }

    /// <summary>
    /// Actualiza un detalle existente
    /// </summary>
    [HttpPut("detalles/{idDetalle:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<ActionResult> UpdateDetalle(
        int idDetalle,
        [FromBody] VentaDetalleItemDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        dto.Iddetalle = idDetalle;
        await _repo.UpdateDetalleAsync(dto, ct);
        return Ok(new { message = "Detalle actualizado correctamente." });
    }

    /// <summary>
    /// Elimina un detalle específico
    /// </summary>
    [HttpDelete("detalles/{idDetalle:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<ActionResult> DeleteDetalle(
        int idDetalle,
        CancellationToken ct)
    {
        await _repo.DeleteDetalleAsync(idDetalle, ct);
        return Ok(new { message = "Detalle eliminado correctamente." });
    }

    /// <summary>
    /// Elimina TODOS los detalles de una venta (útil antes de actualizar)
    /// </summary>
    [HttpDelete("{idVenta:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<ActionResult> DeleteDetallesByVenta(
        int idVenta,
        CancellationToken ct)
    {
        await _repo.DeleteDetallesByVentaAsync(idVenta, ct);
        return Ok(new { message = "Todos los detalles de la venta fueron eliminados." });
    }

    // ═══════════════════════════════════════════════════════════════
    // ENDPOINTS PARA IMPRESIÓN DE FACTURA
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los datos de cabecera para imprimir una factura por su NCF
    /// </summary>
    [HttpGet("reporte/{noFactura}")]
    [Permission(Modules.Cliente, Operations.Ver)] // Ajusta el módulo si es necesario
    public async Task<ActionResult<FacturaReporteDto>> GetFacturaReporte(
        string noFactura,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(noFactura))
            return BadRequest(new { message = "El NCF es requerido." });

        var result = await _repo.GetFacturaReporteAsync(noFactura.Trim(), GetIdEmpresa(), ct);

        if (result is null)
            return NotFound(new { message = $"No se encontró la factura con NCF: {noFactura}" });

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle (productos) de una factura para imprimir, por su ID de venta
    /// </summary>
    [HttpGet("reporte/{idVenta:int}/detalle")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<FacturaDetalleReporteDto>>> GetFacturaDetalleReporte(
        int idVenta,
        CancellationToken ct)
    {
        var result = await _repo.GetFacturaDetalleReporteAsync(idVenta, ct);
        return Ok(result);
    }
}