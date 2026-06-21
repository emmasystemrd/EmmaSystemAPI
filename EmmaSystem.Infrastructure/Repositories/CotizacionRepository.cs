using Dapper;
using EmmaSystem.Application.DTOs.Cotizacion;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class CotizacionRepository : ICotizacionRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public CotizacionRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<CotizacionDto>> GetAllAsync(int idEmpresa, string tipo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CotizacionDto>(
            new CommandDefinition("[dbo].[spmostrar_cotizacion]",
                new { Idempresa = idEmpresa, Tipo = tipo },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<CotizacionDto>> SearchAsync(string texto, string tipo, DateTime fecha1, DateTime fecha2, string proceso, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CotizacionDto>(
            new CommandDefinition("[dbo].[spbuscar_cotizacion]",
                new { TextoBuscar = texto, Tipo = tipo, Fecha1 = fecha1, Fecha2 = fecha2, Proceso = proceso, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<CotizacionDto?> GetByIdWithClientAsync(int idCotizacion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        return await conn.QueryFirstOrDefaultAsync<CotizacionDto>(
            new CommandDefinition("[dbo].[GetCotizacion_Id]",
                new { Idcotizacion = idCotizacion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<int> CreateAsync(CotizacionSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@idcotizacion", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@tipo", dto.Tipo, DbType.String);
        p.Add("@nombre_cliente", dto.Nombre_Cliente, DbType.String);
        p.Add("@no_cotizacion", dto.No_Cotizacion ?? (object)DBNull.Value, DbType.String);
        p.Add("@fecha", dto.Fecha, DbType.DateTime);
        p.Add("@idcliente", dto.Idcliente, DbType.Int32);
        p.Add("@descripcion", dto.Descripcion, DbType.String);
        p.Add("@descuento", dto.Descuento, DbType.Decimal);
        p.Add("@itbis", dto.Itbis, DbType.Decimal);
        p.Add("@subtotal", dto.Subtotal, DbType.Decimal);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_cotizacion]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        var newId = p.Get<int?>("@idcotizacion");

        Console.WriteLine($"🔍 CreateAsync: idcotizacion output = {newId?.ToString() ?? "NULL"}");

        return newId.HasValue && newId.Value > 0
            ? newId.Value
            : throw new InvalidOperationException($"SP no devolvió ID válido. Params: {dto.No_Cotizacion}, {dto.Idcliente}");
    }

    public async Task UpdateAsync(int idCotizacion, CotizacionSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_cotizacion]",
                new
                {
                    idcotizacion = idCotizacion,
                    fecha = dto.Fecha,
                    Nombre_Cliente = dto.Nombre_Cliente,
                    idcliente = dto.Idcliente,
                    descripcion = dto.Descripcion,
                    descuento = dto.Descuento,
                    itbis = dto.Itbis,
                    subtotal = dto.Subtotal
                },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idCotizacion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_cotizacion]",
                new { Idcotizacion = idCotizacion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task CloseAsync(int idCotizacion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spcerrar_cotizacion]",
                new { Idcotizacion = idCotizacion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CotizacionDetalleDto>> GetDetailsAsync(int idCotizacion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CotizacionDetalleDto>(
            new CommandDefinition("[dbo].[spmostrar_detalle_cotizacion]",
                new { textobuscar = idCotizacion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task AddDetailAsync(CotizacionDetalleSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_detalle_cotizacion]",
                new
                {
                    Idcotizacion = dto.Idcotizacion,
                    idarticulo = dto.Idarticulo,
                    cantidad = dto.Cantidad,
                    medida = dto.Medida,
                    precio = dto.Precio,
                    itbis = dto.Itbis,
                    Descuento = dto.Descuento
                },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateDetailAsync(int idDetalle, CotizacionDetalleSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_detalle_cotizacion]",
                new
                {
                    Iddetalle = idDetalle,
                    idarticulo = dto.Idarticulo,
                    cantidad = dto.Cantidad,
                    medida = dto.Medida,
                    precio = dto.Precio,
                    itbis = dto.Itbis,
                    Descuento = dto.Descuento
                },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteDetailAsync(int idDetalle, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_detalle_cotizacion]",
                new { Iddetalle = idDetalle },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task ClearDetailsAsync(int idCotizacion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[splimpiar_detalle_cotizacion]",
                new { textobuscar = idCotizacion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<VendedorDto>> GetVendedoresAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<VendedorDto>(
            new CommandDefinition("[dbo].[spmostrar_vendedor]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<CotizacionImpresionDto?> GetPrintDataAsync(string noCotizacion, string tipo, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@No_Cotizacion", noCotizacion, DbType.String);
        p.Add("@Tipo", tipo, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        return await conn.QueryFirstOrDefaultAsync<CotizacionImpresionDto>(
            new CommandDefinition("[dbo].[Imprimir_Cotizacion]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<string> GetNextSequenceAsync(string tipo, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
            SELECT RIGHT('0000000000' + CAST(ISNULL(MAX(CAST(No_Cotizacion AS INT)), 0) + 1 AS VARCHAR(10)), 10) AS SiguienteNumero
            FROM Cotizacion
            WHERE Tipo = @Tipo AND Idempresa = @IdEmpresa;";

        var result = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { Tipo = tipo, IdEmpresa = idEmpresa }, cancellationToken: ct));

        return result ?? "0000000001";
    }
}