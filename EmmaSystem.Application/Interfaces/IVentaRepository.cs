using EmmaSystem.Application.DTOs.Venta;

namespace EmmaSystem.Application.Interfaces;

public interface IVentaRepository
{
    // === LISTADOS ===

    /// <summary>
    /// Obtiene las últimas 100 ventas activas de una empresa.
    /// Ejecuta [dbo].[spmostrar_venta].
    /// </summary>
    Task<IReadOnlyList<VentaListadoDto>> GetVentasActivasAsync(int idEmpresa, CancellationToken ct = default);

    /// <summary>
    /// Obtiene ventas pendientes de cobro (contado) para una empresa.
    /// Ejecuta [dbo].[spmostrar_venta1_pendiente].
    /// </summary>
    Task<IReadOnlyList<VentaPendienteDto>> GetVentasPendientesAsync(int idEmpresa, CancellationToken ct = default);

    /// <summary>
    /// Busca ventas pendientes por nombre de cliente.
    /// Ejecuta [dbo].[spbuscar_venta1_pendiente].
    /// </summary>
    Task<IReadOnlyList<VentaPendienteDto>> SearchVentasPendientesAsync(int idEmpresa, string textoBuscar, CancellationToken ct = default);

    // === CARGA DE DETALLE ===

    /// <summary>
    /// Carga una venta completa por NCF.
    /// Ejecuta [dbo].[spcargar_venta1_ncf].
    /// </summary>
    Task<VentaDetalleDto?> GetByNcfAsync(int idEmpresa, string ncf, CancellationToken ct = default);

    /// <summary>
    /// Carga una venta completa por ID.
    /// Ejecuta [dbo].[spcargar_venta1_id].
    /// </summary>
    Task<VentaDetalleDto?> GetByIdAsync(int idEmpresa, int idVenta, CancellationToken ct = default);

    /// <summary>
    /// Carga venta por ID de detalle (para edición desde línea).
    /// Ejecuta [dbo].[spcargar_detalle_venta1_id].
    /// </summary>

    /// <summary>
    /// Obtiene información de pago de una venta por NCF.
    /// Ejecuta [dbo].[spventa_pagada].
    /// </summary>
    Task<VentaPagoDto?> GetPagoInfoAsync(int idEmpresa, string noFactura, CancellationToken ct = default);

    // === CRUD ===

    /// <summary>
    /// Inserta una nueva venta y devuelve el ID generado.
    /// Ejecuta [dbo].[spinsertar_venta1].
    /// </summary>
    Task<int> InsertAsync(VentaSaveDto venta, int idEmpresa, CancellationToken ct = default);

    /// <summary>
    /// Actualiza una venta existente.
    /// Ejecuta [dbo].[speditar_venta1].
    /// </summary>
    Task UpdateAsync(int idVenta, VentaSaveDto venta, CancellationToken ct = default);

    /// <summary>
    /// Anula una venta (cambia Estado a 'E').
    /// Ejecuta [dbo].[speliminar_venta].
    /// </summary>
    Task DeleteAsync(int idVenta, int idLogin, CancellationToken ct = default);

    // === BÚSQUEDA AVANZADA ===

    /// <summary>
    /// Búsqueda dinámica por columna y rango de fechas.
    /// Ejecuta [dbo].[spbuscar_venta1_columna].
    /// </summary>
    Task<IReadOnlyList<VentaListadoDto>> SearchByColumnAsync(
        DateTime fecha1,
        DateTime fecha2,
        bool isFecha,
        string textoBuscar,
        string columna,
        int idEmpresa,
        CancellationToken ct = default);

    /// <summary>
    /// Busca venta por ID con cálculo de vencimiento desde ENCF.
    /// Ejecuta [dbo].[spbuscar_venta_ID].
    /// </summary>
    Task<VentaDetalleDto?> SearchByIdWithVencimientoAsync(string textoBuscar, CancellationToken ct = default);

    Task<NcfSecuenciaDto?> GenerarSecuenciaNcfAsync(string tipo, CancellationToken ct = default);


    // ═══ DETALLE DE VENTA ═══
    Task<int> InsertarDetalleAsync(VentaDetalleItemDto detalle, int idVenta, CancellationToken ct = default);
    Task UpdateDetalleAsync(VentaDetalleItemDto detalle, CancellationToken ct = default);
    Task DeleteDetalleAsync(int idDetalle, CancellationToken ct = default);
    Task<IReadOnlyList<VentaDetalleItemDto>> GetDetallesByVentaAsync(int idVenta, CancellationToken ct = default);
    Task DeleteDetallesByVentaAsync(int idVenta, CancellationToken ct = default);

    // === REPORTES DE IMPRESIÓN ===
    Task<FacturaReporteDto?> GetFacturaReporteAsync(string noFactura, int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<FacturaDetalleReporteDto>> GetFacturaDetalleReporteAsync(int idVenta, CancellationToken ct = default);
}