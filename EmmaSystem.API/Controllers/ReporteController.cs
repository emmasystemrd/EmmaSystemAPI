using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Reportes;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/reporte")]
[Authorize]
public sealed class ReporteController : ControllerBase
{
    private readonly IVentaRepository _ventaRepo;

    public ReporteController(IVentaRepository ventaRepo)
    {
        _ventaRepo = ventaRepo;
    }

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");

    // ═══ REPORTE: VENTAS POR COMPROBANTE ═══
    [HttpGet("ventas-comprobante")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaReporteDto>>> ReporteVentasComprobante(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] string? comprobante, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteVentasComprobanteAsync(GetIdEmpresa(), fecha1, fecha2, comprobante ?? "", ct);
        return Ok(result);
    }

    // ═══ REPORTE: ARTÍCULOS VENDIDOS ═══
    [HttpGet("articulos-vendidos")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<ArticuloVendidoReporteDto>>> ReporteArticulosVendidos(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteArticulosVendidosAsync(GetIdEmpresa(), fecha1, fecha2, ct);
        return Ok(result);
    }

    // ═══ REPORTE: VENTA POR DEPARTAMENTO ═══
    [HttpGet("venta-departamento")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaDepartamentoReporteDto>>> ReporteVentaDepartamento(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] int idDepartamento = 0, CancellationToken ct = default)
    {
        var result = await _ventaRepo.ReporteVentaDepartamentoAsync(GetIdEmpresa(), fecha1, fecha2, idDepartamento, ct);
        return Ok(result);
    }

    // ═══ REPORTE: UTILIDAD POR PRODUCTO ═══
    [HttpGet("utilidad-producto")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<UtilidadProductoReporteDto>>> ReporteUtilidadProducto(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteUtilidadProductoAsync(GetIdEmpresa(), fecha1, fecha2, ct);
        return Ok(result);
    }

    // ═══ REPORTE: COMISIÓN POR VENTAS ═══
    [HttpGet("comision-venta")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<ComisionVentaReporteDto>>> ReporteComisionVenta(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteComisionVentaAsync(GetIdEmpresa(), fecha1, fecha2, ct);
        return Ok(result);
    }

    // ═══ REPORTE: COMISIÓN POR PRODUCTO ═══
    [HttpGet("comision-producto")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<ComisionProductoReporteDto>>> ReporteComisionProducto(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] int idEmpleado, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteComisionProductoAsync(GetIdEmpresa(), fecha1, fecha2, idEmpleado, ct);
        return Ok(result);
    }

    // ═══ REPORTE: COTIZACIONES ═══
    [HttpGet("cotizaciones")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<CotizacionReporteDto>>> ReporteCotizaciones(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] string? proceso, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteCotizacionesAsync(fecha1, fecha2, proceso, ct);
        return Ok(result);
    }

    // ═══ REPORTE: PEDIDOS ═══
    [HttpGet("pedidos")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<PedidoReporteDto>>> ReportePedidos(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] string? proceso, CancellationToken ct)
    {
        var result = await _ventaRepo.ReportePedidosAsync(fecha1, fecha2, proceso, ct);
        return Ok(result);
    }

    // ═══ REPORTE: CONDUCE ═══
    [HttpGet("conduce")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<ConduceReporteDto>>> ReporteConduce(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] int? idCliente, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteConduceAsync(fecha1, fecha2, idCliente, ct);
        return Ok(result);
    }
    // ═══ REPORTE: SALDOS POR ANTIGÜEDAD ═══
    [HttpGet("saldos-antiguedad")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<SaldosAntiguedadReporteDto>>> ReporteSaldosAntiguedad(
        [FromQuery] DateTime fecha, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteSaldosAntiguedadAsync(GetIdEmpresa(), fecha, ct);
        return Ok(result);
    }

    // ═══ REPORTE: MOVIMIENTOS DE CLIENTE ═══
    [HttpGet("movimiento-cliente")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<MovimientoClienteReporteDto>>> ReporteMovimientoCliente(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] int idCliente, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteMovimientoClienteAsync(GetIdEmpresa(), fecha1, fecha2, idCliente, ct);
        return Ok(result);
    }

    // ═══ REPORTE: RECIBOS DE COBRO ═══
    [HttpGet("recibos-cobro")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<ReciboCobroReporteDto>>> ReporteRecibosCobro(
        [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, [FromQuery] int? idUsuario, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteRecibosCobroAsync(GetIdEmpresa(), fecha1, fecha2, idUsuario, ct);
        return Ok(result);
    }

    // ═══ REPORTE: ESTADO DE CUENTA ═══
    [HttpGet("estado-cuenta")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<EstadoCuentaReporteDto>> ReporteEstadoCuenta(
        [FromQuery] int idCliente, [FromQuery] DateTime fecha, [FromQuery] DateTime fecha1, [FromQuery] DateTime fecha2, CancellationToken ct)
    {
        var result = await _ventaRepo.ReporteEstadoCuentaAsync(GetIdEmpresa(), idCliente, fecha, fecha1, fecha2, ct);
        if (result == null) return NotFound(new { message = "Cliente no encontrado" });
        return Ok(result);
    }
}