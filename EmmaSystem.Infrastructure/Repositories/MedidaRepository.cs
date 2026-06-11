using Dapper;
using EmmaSystem.Application.DTOs.Medida;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class MedidaRepository : IMedidaRepository
{
    private readonly SqlConnectionFactory _factory;
    public MedidaRepository(SqlConnectionFactory factory) => _factory = factory;

    /// <summary>
    /// Listado general para administración (CRUD). Usa spmostrar_medida.
    /// </summary>
    public async Task<IReadOnlyList<MedidaDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<MedidaDto>(
            new CommandDefinition("[dbo].[spmostrar_medida]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    /// <summary>
    /// Listado simplificado para combos en Artículos. Usa spmostrar_medida_articulo.
    /// Devuelve "Mayor (Contenido)" como Nombre.
    /// </summary>
    public async Task<IReadOnlyList<MedidaArticuloDto>> GetForArticuloAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<MedidaArticuloDto>(
            new CommandDefinition("[dbo].[spmostrar_medida_articulo]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    /// <summary>
    /// Búsqueda por texto (Mayor, Detalle, Descripción o Contenido numérico).
    /// Usa spbuscar_medida con TRY_CAST interno.
    /// </summary>
    public async Task<IReadOnlyList<MedidaDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@TextoBuscar", texto, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryAsync<MedidaDto>(
            new CommandDefinition("[dbo].[spbuscar_medida]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    /// <summary>
    /// Obtener medida por ID exacto. Usa spbuscar_medida_id.
    /// </summary>
    public async Task<MedidaDto?> GetByIdAsync(int idMedida, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<MedidaDto>(
            new CommandDefinition("[dbo].[spbuscar_medida_id]",
                new { TextoBuscar = idMedida },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result;
    }

    public async Task CreateAsync(MedidaSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Mayor", dto.Mayor, DbType.String);
        p.Add("@Detalle", dto.Detalle, DbType.String);
        p.Add("@Contenido", dto.Contenido, DbType.Decimal);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_medida]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task UpdateAsync(int idMedida, MedidaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idmedida", idMedida, DbType.Int32);
        p.Add("@Mayor", dto.Mayor, DbType.String);
        p.Add("@Detalle", dto.Detalle, DbType.String);
        p.Add("@Contenido", dto.Contenido, DbType.Decimal);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_medida]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idMedida, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_medida]",
                new { Idmedida = idMedida },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
    

    public async Task<IReadOnlyList<MedidaDetalleProductoDto>> GetDetallesByProductoAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var result = await conn.QueryAsync<MedidaDetalleProductoDto>(
            new CommandDefinition(
                "[dbo].[spmostrar_medida_detalle_producto]",
                new { Idproducto = idProducto },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    
}