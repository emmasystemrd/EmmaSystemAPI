using Dapper;
using EmmaSystem.Application.DTOs.Cobro;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class CobrosRepository : ICobrosRepository
{
    private readonly SqlConnectionFactory _factory;

    public CobrosRepository(SqlConnectionFactory factory) => _factory = factory;

    // ═══ COBROS NORMALES ═══

    public async Task<IReadOnlyList<CobroListadoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CobroSqlRow>(
            new CommandDefinition(
                "[dbo].[spmostrar_cobro1]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return rows.Select(MapToListado).ToList();
    }

    public async Task<IReadOnlyList<CobroListadoDto>> SearchAsync(int idEmpresa, DateTime? fecha1, DateTime? fecha2, bool isFecha, string texto, string columna, int adjunto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Fecha1", fecha1 ?? DateTime.MinValue);
        p.Add("@Fecha2", fecha2 ?? DateTime.MaxValue);
        p.Add("@IsFecha", isFecha ? 1 : 0);
        p.Add("@TextoBuscar", texto ?? "");
        p.Add("@Columna", columna ?? "cl.Razon_Social");
        p.Add("@Adjunto", adjunto);
        p.Add("@Idempresa", idEmpresa);

        var rows = await conn.QueryAsync<CobroSqlRow>(
            new CommandDefinition(
                "[dbo].[spbuscar_cobro1_columna]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return rows.Select(MapToListado).ToList();
    }

    public async Task<CobroDetalleDto?> GetByIdAsync(int idCobro, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<CobroDetalleSqlRow>(
            new CommandDefinition(
                "[dbo].[spbuscar_cobro1_id]",
                new { Idcobro = idCobro, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        if (row == null) return null;
        return MapToDetalle(row);
    }

    public async Task<CobroDetalleDto?> GetByCodigoAsync(string codigo, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<CobroDetalleSqlRow>(
            new CommandDefinition(
                "[dbo].[spbuscar_cobro1_codigo]",
                new { TextoBuscar = codigo, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        if (row == null) return null;
        return MapToDetalle(row);
    }

    public async Task<int> InsertAsync(CobroSaveDto dto, int idLogin, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var p = new DynamicParameters();
            p.Add("@Idcobro", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@Codigo", dto.Codigo);
            p.Add("@No_Cuota", dto.No_Cuota);
            p.Add("@Fecha", dto.Fecha);
            p.Add("@Idcliente", dto.Idcliente);
            p.Add("@Balance", dto.Balance);
            p.Add("@Descuento", dto.Descuento);
            p.Add("@Retencion_ITBIS", dto.Retencion_ITBIS);
            p.Add("@Retencion_ISR", dto.Retencion_ISR);
            p.Add("@Efectivo", dto.Efectivo);
            p.Add("@Cheque", dto.Cheque);

            // ✅ CORRECCIÓN: Especificar DbType para campos NULL
            p.Add("@Banco_Ck", dto.Banco_Ck, DbType.Int32);
            p.Add("@Num_Ck", dto.Num_Ck);
            p.Add("@Transferencia", dto.Transferencia);
            p.Add("@Banco_Transf", dto.Banco_Transf, DbType.Int32);
            p.Add("@Ref_Transf", dto.Ref_Transf);
            p.Add("@Tarjeta", dto.Tarjeta);
            p.Add("@Tipo_Tarjeta", dto.Tipo_Tarjeta, DbType.Int32);
            p.Add("@Ref_Tarjeta", dto.Ref_Tarjeta);
            p.Add("@Devuelta", dto.Devuelta);
            p.Add("@Idlogin", idLogin);
            p.Add("@Idempresa", idEmpresa);

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "[dbo].[spinsertar_cobro1]",
                    p,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

            var idCobro = p.Get<int>("@Idcobro");

            // Insertar detalles
            foreach (var det in dto.Detalles)
            {
                if (det.Monto <= 0) continue;

                var pDet = new DynamicParameters();
                pDet.Add("@Idcobro", idCobro);
                pDet.Add("@Iddocumento", det.Iddocumento);
                pDet.Add("@Balance", det.Balance);
                pDet.Add("@p_descuento", det.p_descuento);
                pDet.Add("@Descuento", det.Descuento);
                pDet.Add("@Idretencion_ITBIS", det.Idretencion_ITBIS);
                pDet.Add("@Idretencion_ISR", det.Idretencion_ISR);
                pDet.Add("@p_isr", det.p_isr);
                pDet.Add("@p_itr", det.p_itr);
                pDet.Add("@isr", det.isr);
                pDet.Add("@itr", det.itr);
                pDet.Add("@Interes", det.Interes);
                pDet.Add("@Cargos", det.Cargos);
                pDet.Add("@Monto", det.Monto);

                await conn.ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[spinsertar_detalle_cobro1]",
                        pDet,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));
            }

            transaction.Commit();
            return idCobro;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(int idCobro, CobroSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var p = new DynamicParameters();
            p.Add("@Idcobro", idCobro);
            p.Add("@Fecha", dto.Fecha);
            p.Add("@Balance", dto.Balance);
            p.Add("@Descuento", dto.Descuento);
            p.Add("@Retencion_ITBIS", dto.Retencion_ITBIS);
            p.Add("@Retencion_ISR", dto.Retencion_ISR);
            p.Add("@Efectivo", dto.Efectivo);
            p.Add("@Cheque", dto.Cheque);

            // ✅ CORRECCIÓN: Especificar DbType para campos NULL
            p.Add("@Banco_Ck", dto.Banco_Ck, DbType.Int32);
            p.Add("@Num_Ck", dto.Num_Ck);
            p.Add("@Transferencia", dto.Transferencia);
            p.Add("@Banco_Transf", dto.Banco_Transf, DbType.Int32);
            p.Add("@Ref_Transf", dto.Ref_Transf);
            p.Add("@Tarjeta", dto.Tarjeta);
            p.Add("@Tipo_Tarjeta", dto.Tipo_Tarjeta, DbType.Int32);
            p.Add("@Ref_Tarjeta", dto.Ref_Tarjeta);
            p.Add("@Devuelta", dto.Devuelta);
            p.Add("@Idlogin", idLogin);

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "[dbo].[speditar_cobro1]",
                    p,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

            // Eliminar detalles anteriores y reinsertar
            await conn.ExecuteAsync(
                "DELETE FROM Detalle_Cobro1 WHERE Idcobro = @Idcobro",
                new { Idcobro = idCobro },
                transaction: transaction);

            foreach (var det in dto.Detalles)
            {
                if (det.Monto <= 0) continue;

                var pDet = new DynamicParameters();
                pDet.Add("@Idcobro", idCobro);
                pDet.Add("@Iddocumento", det.Iddocumento);
                pDet.Add("@Balance", det.Balance);
                pDet.Add("@p_descuento", det.p_descuento);
                pDet.Add("@Descuento", det.Descuento);
                pDet.Add("@Idretencion_ITBIS", det.Idretencion_ITBIS);
                pDet.Add("@Idretencion_ISR", det.Idretencion_ISR);
                pDet.Add("@p_isr", det.p_isr);
                pDet.Add("@p_itr", det.p_itr);
                pDet.Add("@isr", det.isr);
                pDet.Add("@itr", det.itr);
                pDet.Add("@Interes", det.Interes);
                pDet.Add("@Cargos", det.Cargos);
                pDet.Add("@Monto", det.Monto);

                await conn.ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[spinsertar_detalle_cobro1]",
                        pDet,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> InsertAvanceAsync(AvanceClienteSaveDto dto, int idLogin, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Codigo", dto.Codigo);
        p.Add("@Fecha", dto.Fecha);
        p.Add("@Idcliente", dto.Idcliente);
        p.Add("@Efectivo", dto.Efectivo);
        p.Add("@Cheque", dto.Cheque);

        // ✅ CORRECCIÓN: Especificar DbType para campos NULL
        p.Add("@Banco_Ck", dto.Banco_Ck, DbType.Int32);
        p.Add("@Num_Ck", dto.Num_Ck);
        p.Add("@Transferencia", dto.Transferencia);
        p.Add("@Banco_Transf", dto.Banco_Transf, DbType.Int32);
        p.Add("@Ref_Transf", dto.Ref_Transf);
        p.Add("@Tarjeta", dto.Tarjeta);
        p.Add("@Tipo_Tarjeta", dto.Tipo_Tarjeta, DbType.Int32);
        p.Add("@Ref_Tarjeta", dto.Ref_Tarjeta);
        p.Add("@Idlogin", idLogin);
        p.Add("@Idempresa", idEmpresa);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[spinsertar_avance_cliente]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return await conn.ExecuteScalarAsync<int>("SELECT CAST(SCOPE_IDENTITY() AS INT)");
    }

    public async Task UpdateAvanceAsync(int idCobro, AvanceClienteSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idcobro", idCobro);
        p.Add("@Fecha", dto.Fecha);
        p.Add("@Efectivo", dto.Efectivo);
        p.Add("@Cheque", dto.Cheque);

        // ✅ CORRECCIÓN: Especificar DbType para campos NULL
        p.Add("@Banco_Ck", dto.Banco_Ck, DbType.Int32);
        p.Add("@Num_Ck", dto.Num_Ck);
        p.Add("@Transferencia", dto.Transferencia);
        p.Add("@Banco_Transf", dto.Banco_Transf, DbType.Int32);
        p.Add("@Ref_Transf", dto.Ref_Transf);
        p.Add("@Tarjeta", dto.Tarjeta);
        p.Add("@Tipo_Tarjeta", dto.Tipo_Tarjeta, DbType.Int32);
        p.Add("@Ref_Tarjeta", dto.Ref_Tarjeta);
        p.Add("@Idlogin", idLogin);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[speditar_avance_cliente]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idCobro, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[speliminar_cobro]",
                new { Idlogin = idLogin, Idcobro = idCobro },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    // ═══ AVANCES DE CLIENTE ═══

    public async Task<IReadOnlyList<CobroListadoDto>> GetAllAvancesAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CobroSqlRow>(
            new CommandDefinition(
                "[dbo].[spmostrar_avance_cliente]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return rows.Select(MapToListado).ToList();
    }

    public async Task<IReadOnlyList<CobroListadoDto>> SearchAvancesAsync(int idEmpresa, DateTime? fecha1, DateTime? fecha2, bool isFecha, string texto, string columna, int adjunto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Fecha1", fecha1 ?? DateTime.MinValue);
        p.Add("@Fecha2", fecha2 ?? DateTime.MaxValue);
        p.Add("@IsFecha", isFecha ? 1 : 0);
        p.Add("@TextoBuscar", texto ?? "");
        p.Add("@Columna", columna ?? "cl.Razon_Social");
        p.Add("@Adjunto", adjunto);
        p.Add("@Idempresa", idEmpresa);

        var rows = await conn.QueryAsync<CobroSqlRow>(
            new CommandDefinition(
                "[dbo].[spbuscar_avance_cliente_columna]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return rows.Select(MapToListado).ToList();
    }
    // ═══ DETALLES ═══

    public async Task<IReadOnlyList<DetalleCobroDto>> GetDetallesAsync(int idCobro, int idCliente, DateTime fecha, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@TextoBuscar", idCobro);
        p.Add("@Idpersona", idCliente);
        p.Add("@Fecha", fecha);
        p.Add("@Idempresa", idEmpresa);

        var result = await conn.QueryAsync<DetalleCobroDto>(
            new CommandDefinition(
                "[dbo].[spmostrar_detalle_cobro1]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.ToList();
    }

    public async Task<int> GetIdDocumentoAsync(int idEmpresa, string tipo, string noFactura, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "[dbo].[spobtener_iddocumento]",
                new { Idempresa = idEmpresa, Tipo = tipo, No_Factura = noFactura },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    // ═══ REPORTE ═══

    public async Task<IReadOnlyList<CobroReporteDto>> GetReporteAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, int idUsuario, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<CobroReporteDto>(
            new CommandDefinition(
                "[dbo].[r_cobro1]",
                new { Idempresa = idEmpresa, Fecha1 = fecha1, Fecha2 = fecha2, Idusuario = idUsuario },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.ToList();
    }

    // ═══ PDF ═══

    public async Task<byte[]?> GetPdfAsync(int idCobro, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<byte[]?>(
            "SELECT PDF FROM Cobro1 WHERE Idcobro = @Idcobro",
            new { Idcobro = idCobro });
    }

    // ═══ MAPEADORES ═══

    private CobroListadoDto MapToListado(CobroSqlRow r) => new()
    {
        Idcobro = r.Idcobro,
        Codigo = r.Codigo ?? "",
        Fecha = r.Fecha,
        Razon_Social = r.Razon_Social ?? "",
        Balance = r.Balance,
        Descuento = r.Descuento,
        Valor = r.Valor,
        Pendiente = r.Pendiente,
        TienePDF = r.PDF != null && r.PDF.Length > 0
    };

    private CobroDetalleDto MapToDetalle(CobroDetalleSqlRow r) => new()
    {
        Idcobro = r.Idcobro,
        Codigo = r.Codigo ?? "",
        Fecha = r.Fecha,
        Num_Documento = r.Num_Documento ?? "",
        Razon_Social = r.Razon_Social ?? "",
        Balance = r.Balance,
        Descuento = r.Descuento,
        Retencion_ITBIS = r.Retencion_ITBIS,
        Retencion_ISR = r.Retencion_ISR,
        Valor = r.Valor,
        Pendiente = r.Pendiente,
        Efectivo = r.Efectivo,
        Cheque = r.Cheque,
        Banco_Ck = r.Banco_Ck,
        Num_Ck = r.Num_Ck ?? "",
        Transferencia = r.Transferencia,
        Banco_Transf = r.Banco_Transf,
        Ref_Transf = r.Ref_Transf ?? "",
        Tarjeta = r.Tarjeta,
        Tipo_Tarjeta = r.Tipo_Tarjeta,
        Ref_Tarjeta = r.Ref_Tarjeta ?? "",
        Devuelta = r.Devuelta
    };
}

// ═══ Clases internas para mapeo seguro ═══

internal class CobroSqlRow
{
    public int Idcobro { get; set; }
    public string? Codigo { get; set; }
    public DateTime Fecha { get; set; }
    public string? Razon_Social { get; set; }
    public decimal Balance { get; set; }
    public decimal Descuento { get; set; }
    public decimal Valor { get; set; }
    public decimal Pendiente { get; set; }
    public byte[]? PDF { get; set; }
}

internal class CobroDetalleSqlRow
{
    public int Idcobro { get; set; }
    public string? Codigo { get; set; }
    public DateTime Fecha { get; set; }
    public string? Num_Documento { get; set; }
    public string? Razon_Social { get; set; }
    public decimal Balance { get; set; }
    public decimal Descuento { get; set; }
    public decimal Retencion_ITBIS { get; set; }
    public decimal Retencion_ISR { get; set; }
    public decimal Valor { get; set; }
    public decimal Pendiente { get; set; }
    public decimal Efectivo { get; set; }
    public decimal Cheque { get; set; }
    public int? Banco_Ck { get; set; }
    public string? Num_Ck { get; set; }
    public decimal Transferencia { get; set; }
    public int? Banco_Transf { get; set; }
    public string? Ref_Transf { get; set; }
    public decimal Tarjeta { get; set; }
    public int? Tipo_Tarjeta { get; set; }
    public string? Ref_Tarjeta { get; set; }
    public decimal Devuelta { get; set; }
}