using Dapper;
using EmmaSystem.Application.DTOs.Reportes;
using EmmaSystem.Application.DTOs.Venta;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class VentaRepository : IVentaRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private const int EmpresaIdInterna = 1;
    public VentaRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    // === LISTADOS ===

    public async Task<IReadOnlyList<VentaListadoDto>> GetVentasActivasAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<VentaListadoDto>(
            new CommandDefinition("[dbo].[spmostrar_venta]",
                new { Idempresa = EmpresaIdInterna },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<VentaPendienteDto>> GetVentasPendientesAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<VentaPendienteDto>(
            new CommandDefinition("[dbo].[spmostrar_venta1_pendiente]",
                new { Idempresa = EmpresaIdInterna },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<VentaPendienteDto>> SearchVentasPendientesAsync(int idEmpresa, string textoBuscar, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<VentaPendienteDto>(
            new CommandDefinition("[dbo].[spbuscar_venta1_pendiente]",
                new { Idempresa = EmpresaIdInterna, TextoBuscar = textoBuscar },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    // === CARGA DE DETALLE ===

    public async Task<VentaDetalleDto?> GetByNcfAsync(int idEmpresa, string ncf, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryFirstOrDefaultAsync<VentaDetalleDto>(
            new CommandDefinition("[dbo].[spcargar_venta1_ncf]",
                new { Idempresa = EmpresaIdInterna, NCF = ncf },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task<VentaDetalleDto?> GetByIdAsync(int idEmpresa, int idVenta, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryFirstOrDefaultAsync<VentaDetalleDto>(
            new CommandDefinition("[dbo].[spcargar_venta1_id]",
                new { Idempresa = EmpresaIdInterna, Idventa = idVenta },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task<VentaPagoDto?> GetPagoInfoAsync(int idEmpresa, string noFactura, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryFirstOrDefaultAsync<VentaPagoDto>(
            new CommandDefinition("[dbo].[spventa_pagada]",
                new { Idempresa = EmpresaIdInterna, No_Factura = noFactura },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    // === CRUD ===

    public async Task<int> InsertAsync(VentaSaveDto venta, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        //conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var p = new DynamicParameters();
            p.Add("@Idventa1", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@Fecha", venta.Fecha);
            p.Add("@Idcliente", venta.Idcliente);
            p.Add("@Nombre_Cliente", venta.Nombre_Cliente);
            p.Add("@Contado", venta.Contado);
            p.Add("@Tipo", venta.Tipo);
            p.Add("@NCF", venta.NCF);
            p.Add("@Idtermino", venta.Idtermino);
            p.Add("@Tipo_Ingreso", venta.Tipo_Ingreso);
            p.Add("@Subtotal", venta.Subtotal);
            p.Add("@ITBIS", venta.ITBIS);
            p.Add("@Descuento", venta.Descuento);
            p.Add("@Monto_Descuento", venta.Monto_Descuento);
            p.Add("@Vencimiento", venta.Vencimiento);
            p.Add("@Interes", venta.Interes);
            p.Add("@Propina_Legal", venta.Propina_Legal);
            p.Add("@Descripcion", venta.Descripcion);
            p.Add("@Cta_Ingreso", venta.Cta_Ingreso);
            p.Add("@Monto_Servicios", venta.Monto_Servicios);
            p.Add("@ITBIS_Servicios", venta.ITBIS_Servicios);
            p.Add("@Iddepartamento", venta.Iddepartamento);
            p.Add("@Idlogin", venta.Idlogin);
            p.Add("@Idvendedor", venta.Idvendedor);
            p.Add("@Idempresa", EmpresaIdInterna);

            await conn.ExecuteAsync(
                new CommandDefinition("[dbo].[spinsertar_venta1]", p,
                    transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));

            var idVenta = p.Get<int>("@Idventa1");

            if (venta.Detalles != null && venta.Detalles.Any())
            {
                foreach (var detalle in venta.Detalles)
                {
                    var pDetalle = new DynamicParameters();
                    pDetalle.Add("@Idventa1", idVenta);
                    pDetalle.Add("@Idarticulo", detalle.Idarticulo);
                    pDetalle.Add("@Cantidad", detalle.Cantidad);
                    pDetalle.Add("@Medida", detalle.Medida);
                    pDetalle.Add("@Precio_Venta1", detalle.Precio_Venta1);
                    pDetalle.Add("@P_Itbis", detalle.P_Itbis);
                    pDetalle.Add("@Descuento", detalle.Descuento);

                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[spinsertar_detalle_venta1]", pDetalle,
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
            }

            transaction.Commit();
            return idVenta;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(int idVenta, VentaSaveDto venta, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
       // conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var p = new DynamicParameters();
            p.Add("@Idventa1", idVenta);
            p.Add("@Fecha", venta.Fecha);
            p.Add("@Idcliente", venta.Idcliente);
            p.Add("@Nombre_Cliente", venta.Nombre_Cliente);
            p.Add("@Contado", venta.Contado);
            p.Add("@Idvendedor", venta.Idvendedor);
            p.Add("@Idtermino", venta.Idtermino);
            p.Add("@Tipo_Ingreso", venta.Tipo_Ingreso);
            p.Add("@Subtotal", venta.Subtotal);
            p.Add("@ITBIS", venta.ITBIS);
            p.Add("@Descuento", venta.Descuento);
            p.Add("@Monto_Descuento", venta.Monto_Descuento);
            p.Add("@Propina_Legal", venta.Propina_Legal);
            p.Add("@Descripcion", venta.Descripcion);
            p.Add("@Cta_Ingreso", venta.Cta_Ingreso);
            p.Add("@Monto_Servicios", venta.Monto_Servicios);
            p.Add("@ITBIS_Servicios", venta.ITBIS_Servicios);
            p.Add("@Iddepartamento", venta.Iddepartamento);
            p.Add("@Idlogin", venta.Idlogin);

            await conn.ExecuteAsync(
                new CommandDefinition("[dbo].[speditar_venta1]", p,
                    transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));

            await conn.ExecuteAsync(
                "DELETE FROM Detalle_Venta1 WHERE Idventa1 = @Idventa1",
                new { Idventa1 = idVenta }, transaction: transaction);

            if (venta.Detalles != null && venta.Detalles.Any())
            {
                foreach (var detalle in venta.Detalles)
                {
                    var pDetalle = new DynamicParameters();
                    pDetalle.Add("@Idventa1", idVenta);
                    pDetalle.Add("@Idarticulo", detalle.Idarticulo);
                    pDetalle.Add("@Cantidad", detalle.Cantidad);
                    pDetalle.Add("@Medida", detalle.Medida);
                    pDetalle.Add("@Precio_Venta1", detalle.Precio_Venta1);
                    pDetalle.Add("@P_Itbis", detalle.P_Itbis);
                    pDetalle.Add("@Descuento", detalle.Descuento);

                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[spinsertar_detalle_venta1]", pDetalle,
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAsync(int idVenta, int idLogin, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        //conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(
                "DELETE FROM Detalle_Venta1 WHERE Idventa1 = @Idventa1",
                new { Idventa1 = idVenta }, transaction: transaction);

            await conn.ExecuteAsync(
                new CommandDefinition("[dbo].[speliminar_venta]",
                    new { IdVenta1 = idVenta, Idlogin = idLogin },
                    transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // === BÚSQUEDA AVANZADA ===

    public async Task<IReadOnlyList<VentaListadoDto>> SearchByColumnAsync(
        DateTime fecha1, DateTime fecha2, bool isFecha,
        string textoBuscar, string columna, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var allowedColumns = new[] {
            "v.Nombre_Cliente", "v.NCF", "c.Codigo", "v.Fecha", "v.Tipo"
        };
        if (!allowedColumns.Contains(columna))
            columna = "v.Nombre_Cliente";

        var result = await conn.QueryAsync<VentaListadoDto>(
            new CommandDefinition("[dbo].[spbuscar_venta1_columna]",
                new
                {
                    Fecha1 = fecha1,
                    Fecha2 = fecha2,
                    IsFecha = isFecha ? 1 : 0,
                    TextoBuscar = textoBuscar,
                    Columna = columna,
                    Idempresa = EmpresaIdInterna
                },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<VentaDetalleDto?> SearchByIdWithVencimientoAsync(string textoBuscar, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryFirstOrDefaultAsync<VentaDetalleDto>(
            new CommandDefinition("[dbo].[spbuscar_venta_ID]",
                new { TextoBuscar = textoBuscar },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task<NcfSecuenciaDto?> GenerarSecuenciaNcfAsync(string tipo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Tipo", tipo.Trim());

        var result = await conn.QueryFirstOrDefaultAsync<NcfSecuenciaDto>(
            new CommandDefinition("[dbo].[spgenerar_secuencia_ncf]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    // ═══ DETALLE DE VENTA ═══

    public async Task<int> InsertarDetalleAsync(VentaDetalleItemDto detalle, int idVenta, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Idventa1", idVenta);
        p.Add("@Idarticulo", detalle.Idarticulo);
        p.Add("@Cantidad", detalle.Cantidad);
        p.Add("@Medida", detalle.Medida);
        p.Add("@Precio_Venta1", detalle.Precio_Venta1);
        p.Add("@P_Itbis", detalle.P_Itbis);
        p.Add("@Descuento", detalle.Descuento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_detalle_venta1]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return 0;
    }

    public async Task UpdateDetalleAsync(VentaDetalleItemDto detalle, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Iddetalle", detalle.Iddetalle);
        p.Add("@Idarticulo", detalle.Idarticulo);
        p.Add("@Cantidad", detalle.Cantidad);
        p.Add("@Medida", detalle.Medida);
        p.Add("@Precio_Venta1", detalle.Precio_Venta1);
        p.Add("@P_Itbis", detalle.P_Itbis);
        p.Add("@Descuento", detalle.Descuento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_detalle_venta1]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteDetalleAsync(int idDetalle, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_detalle_venta1]",
                new { Iddetalle = idDetalle },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<VentaDetalleItemDto>> GetDetallesByVentaAsync(int idVenta, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT 
            d.Iddetalle,
            d.IdVenta1,
            d.Idarticulo,
            a.Codigo,
            a.Nombre AS Producto,
            d.Cantidad,
            m.Mayor AS Medida,
            d.Precio_Venta1,
            d.ITBIS AS P_Itbis,
            d.Descuento,
            (d.Cantidad * d.Precio_Venta1) AS Subtotal,
            (d.Cantidad * d.Precio_Venta1 * d.ITBIS) AS ITBIS
        FROM Detalle_Venta1 d
        INNER JOIN Detalle_Producto dp ON dp.Iddetalle = d.Idarticulo
        INNER JOIN Articulo1 a ON a.Idarticulo = dp.Idarticulo
        INNER JOIN Medida m ON m.Idmedida = dp.Idmedida
        WHERE d.IdVenta1 = @IdVenta";

        var result = await conn.QueryAsync<VentaDetalleItemDto>(
            new CommandDefinition(sql, new { IdVenta = idVenta }, cancellationToken: ct));

        return result.ToList();
    }

    public async Task DeleteDetallesByVentaAsync(int idVenta, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            "DELETE FROM Detalle_Venta1 WHERE Idventa1 = @Idventa1",
            new { Idventa1 = idVenta });
    }

    // ═══ REPORTES DE IMPRESIÓN ═══

    public async Task<FacturaReporteDto?> GetFacturaReporteAsync(string noFactura, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryFirstOrDefaultAsync<FacturaReporteDto>(
            new CommandDefinition("[dbo].[spreporte_factura]",
                new { no_factura = noFactura, Idempresa =  EmpresaIdInterna },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task<IReadOnlyList<FacturaDetalleReporteDto>> GetFacturaDetalleReporteAsync(int idVenta, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<FacturaDetalleReporteDto>(
            new CommandDefinition("[dbo].[spreporte_detalle_factura]",
                new { Idventa = idVenta },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }
    public async Task<IReadOnlyList<VentaReporteDto>> ReporteVentasComprobanteAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, string comprobante, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        EXEC r_ventas_comprobante 
            @Idempresa = @IdEmpresa,
            @Fecha1 = @Fecha1,
            @Fecha2 = @Fecha2,
            @Comprobante = @Comprobante";

        var result = await conn.QueryAsync<VentaReporteDto>(
            new CommandDefinition(sql, new
            {
                IdEmpresa = idEmpresa,
                Fecha1 = fecha1,
                Fecha2 = fecha2,
                Comprobante = comprobante ?? ""
            }, cancellationToken: ct));

        return result.ToList();
    }
    public async Task<IReadOnlyList<ArticuloVendidoReporteDto>> ReporteArticulosVendidosAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        EXEC r_articulos_vendidos 
            @Idempresa = @IdEmpresa,
            @fecha1 = @Fecha1,
            @fecha2 = @Fecha2";

        var result = await conn.QueryAsync<ArticuloVendidoReporteDto>(
            new CommandDefinition(sql, new
            {
                IdEmpresa = idEmpresa,
                Fecha1 = fecha1,
                Fecha2 = fecha2
            }, cancellationToken: ct));

        return result.ToList();
    }
    // ═══ REPORTES ADICIONALES ═══

    public async Task<IReadOnlyList<VentaDepartamentoReporteDto>> ReporteVentaDepartamentoAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, int idDepartamento, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_ventas_detalle @Idempresa=@IdEmpresa, @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Iddepartamento=@IdDepartamento";
        var result = await conn.QueryAsync<VentaDepartamentoReporteDto>(new CommandDefinition(sql, new { IdEmpresa = idEmpresa, Fecha1 = fecha1, Fecha2 = fecha2, IdDepartamento = idDepartamento }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<UtilidadProductoReporteDto>> ReporteUtilidadProductoAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_articulos_utilidad @Idempresa=@IdEmpresa, @fecha1=@Fecha1, @fecha2=@Fecha2";
        var result = await conn.QueryAsync<UtilidadProductoReporteDto>(new CommandDefinition(sql, new { IdEmpresa = idEmpresa, Fecha1 = fecha1, Fecha2 = fecha2 }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<ComisionVentaReporteDto>> ReporteComisionVentaAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_comision_venta @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Idempresa=@IdEmpresa";
        var result = await conn.QueryAsync<ComisionVentaReporteDto>(new CommandDefinition(sql, new { Fecha1 = fecha1, Fecha2 = fecha2, IdEmpresa = idEmpresa }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<ComisionProductoReporteDto>> ReporteComisionProductoAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, int idEmpleado, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_comision_producto @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Idempleado=@IdEmpleado, @Idempresa=@IdEmpresa";
        var result = await conn.QueryAsync<ComisionProductoReporteDto>(new CommandDefinition(sql, new { Fecha1 = fecha1, Fecha2 = fecha2, IdEmpleado = idEmpleado, IdEmpresa = idEmpresa }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<CotizacionReporteDto>> ReporteCotizacionesAsync(DateTime fecha1, DateTime fecha2, string? proceso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC RListado_Cotizacion @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Proceso=@Proceso";
        var result = await conn.QueryAsync<CotizacionReporteDto>(new CommandDefinition(sql, new { Fecha1 = fecha1, Fecha2 = fecha2, Proceso = proceso ?? "" }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<PedidoReporteDto>> ReportePedidosAsync(DateTime fecha1, DateTime fecha2, string? proceso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC RListado_Pedido @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Proceso=@Proceso";
        var result = await conn.QueryAsync<PedidoReporteDto>(new CommandDefinition(sql, new { Fecha1 = fecha1, Fecha2 = fecha2, Proceso = proceso ?? "" }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<ConduceReporteDto>> ReporteConduceAsync(DateTime fecha1, DateTime fecha2, int? idCliente, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC RListado_Conduce @Idcliente=@IdCliente, @Fecha1=@Fecha1, @Fecha2=@Fecha2";
        var result = await conn.QueryAsync<ConduceReporteDto>(new CommandDefinition(sql, new { IdCliente = idCliente ?? 0, Fecha1 = fecha1, Fecha2 = fecha2 }, cancellationToken: ct));
        return result.ToList();
    }
    // ═══ REPORTES DE CLIENTES ═══

    public async Task<IReadOnlyList<SaldosAntiguedadReporteDto>> ReporteSaldosAntiguedadAsync(int idEmpresa, DateTime fecha, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_clientes_resumen @Idempresa=@IdEmpresa, @Fecha=@Fecha";
        var result = await conn.QueryAsync<SaldosAntiguedadReporteDto>(new CommandDefinition(sql, new { IdEmpresa = idEmpresa, Fecha = fecha }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<MovimientoClienteReporteDto>> ReporteMovimientoClienteAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, int idCliente, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_movimiento_clientes @Idempresa=@IdEmpresa, @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Idcliente=@IdCliente";
        var result = await conn.QueryAsync<MovimientoClienteReporteDto>(new CommandDefinition(sql, new { IdEmpresa = idEmpresa, Fecha1 = fecha1, Fecha2 = fecha2, IdCliente = idCliente }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<ReciboCobroReporteDto>> ReporteRecibosCobroAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, int? idUsuario, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        const string sql = "EXEC r_cobro1 @Idempresa=@IdEmpresa, @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Idusuario=@IdUsuario";
        var result = await conn.QueryAsync<ReciboCobroReporteDto>(new CommandDefinition(sql, new { IdEmpresa = idEmpresa, Fecha1 = fecha1, Fecha2 = fecha2, IdUsuario = idUsuario ?? 0 }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<EstadoCuentaReporteDto?> ReporteEstadoCuentaAsync(int idEmpresa, int idCliente, DateTime fecha, DateTime fecha1, DateTime fecha2, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // Obtener datos del cliente
        const string sqlCliente = "EXEC r_datos_cliente @TextoBuscar=@IdCliente, @Idempresa=@IdEmpresa";
        var cliente = await conn.QueryFirstOrDefaultAsync<EstadoCuentaReporteDto>(new CommandDefinition(sqlCliente, new { IdCliente = idCliente, IdEmpresa = idEmpresa }, cancellationToken: ct));

        if (cliente == null) return null;

        // Obtener saldos por antigüedad
        const string sqlSaldos = "EXEC r_saldos_antiguedad_cliente @Idcliente=@IdCliente, @Fecha=@Fecha";
        var saldos = await conn.QueryFirstOrDefaultAsync(new CommandDefinition(sqlSaldos, new { IdCliente = idCliente, Fecha = fecha }, cancellationToken: ct));

        if (saldos != null)
        {
            cliente.No_Vencida = saldos.No_Vencida ?? 0;
            cliente.dias_30 = saldos.dias_30 ?? 0;
            cliente.dias_60 = saldos.dias_60 ?? 0;
            cliente.dias_90 = saldos.dias_90 ?? 0;
            cliente.dias_120 = saldos.dias_120 ?? 0;
            cliente.mas_120 = saldos.mas_120 ?? 0;
        }

        // Obtener movimientos
        const string sqlMovimientos = "EXEC r_movimiento_clientes @Idempresa=@IdEmpresa, @Fecha1=@Fecha1, @Fecha2=@Fecha2, @Idcliente=@IdCliente";
        var movimientos = await conn.QueryAsync<MovimientoClienteReporteDto>(new CommandDefinition(sqlMovimientos, new { IdEmpresa = idEmpresa, Fecha1 = fecha1, Fecha2 = fecha2, IdCliente = idCliente }, cancellationToken: ct));
        cliente.Movimientos = movimientos.ToList();

        return cliente;
    }
}