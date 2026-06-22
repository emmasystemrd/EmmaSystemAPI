using Dapper;
using EmmaSystem.Application.DTOs.Medida;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class MedidaRepository : IMedidaRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private const int EmpresaIdInterna = 1;

    public MedidaRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<MedidaDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var result = await conn.QueryAsync<MedidaDto>(
            new CommandDefinition("[dbo].[spmostrar_medida]",
                new { Idempresa = EmpresaIdInterna },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<MedidaArticuloDto>> GetForArticuloAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var result = await conn.QueryAsync<MedidaArticuloDto>(
            new CommandDefinition("[dbo].[spmostrar_medida_articulo]",
                new { Idempresa = EmpresaIdInterna },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<MedidaDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var p = new DynamicParameters();
        p.Add("@TextoBuscar", texto, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        var result = await conn.QueryAsync<MedidaDto>(
            new CommandDefinition("[dbo].[spbuscar_medida]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<MedidaDto?> GetByIdAsync(int idMedida, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var result = await conn.QueryFirstOrDefaultAsync<MedidaDto>(
            new CommandDefinition("[dbo].[spbuscar_medida_id]",
                new { TextoBuscar = idMedida },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task CreateAsync(MedidaSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var p = new DynamicParameters();
        p.Add("@Mayor", dto.Mayor, DbType.String);
        p.Add("@Detalle", dto.Detalle, DbType.String);
        p.Add("@Contenido", dto.Contenido, DbType.Decimal);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_medida]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idMedida, MedidaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var p = new DynamicParameters();
        p.Add("@Idmedida", idMedida, DbType.Int32);
        p.Add("@Mayor", dto.Mayor, DbType.String);
        p.Add("@Detalle", dto.Detalle, DbType.String);
        p.Add("@Contenido", dto.Contenido, DbType.Decimal);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_medida]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idMedida, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_medida]",
                new { Idmedida = idMedida },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MedidaDetalleProductoDto>> GetDetallesByProductoAsync(
        int idProducto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct);

        var result = await conn.QueryAsync<MedidaDetalleProductoDto>(
            new CommandDefinition("[dbo].[spmostrar_medida_detalle_producto]",
                new { Idproducto = idProducto },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }
}