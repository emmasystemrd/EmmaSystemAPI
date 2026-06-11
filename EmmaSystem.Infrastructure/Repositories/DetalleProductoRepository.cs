using Dapper;
using EmmaSystem.Application.DTOs.Articulo;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class DetalleProductoRepository : IDetalleProductoRepository
{
    private readonly SqlConnectionFactory _factory;
    public DetalleProductoRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<DetalleProductoDto>> GetByIdArticuloAsync(int idArticulo, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        // Usamos el SP existente que ya trae la medida y existencia calculada
        var result = await conn.QueryAsync<DetalleProductoDto>(
            new CommandDefinition("[dbo].[spmostrar_detalle_producto]",
                new { Idarticulo = idArticulo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    public async Task CreateAsync(DetalleProductoSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idarticulo", dto.Idarticulo, DbType.Int32);
        p.Add("@Codigo_Barra", dto.CodigoBarra, DbType.String);
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Idmedida", dto.Idmedida, DbType.Int32);
        p.Add("@Unidades", dto.Unidades, DbType.Decimal);
        p.Add("@Costo", dto.Costo, DbType.Decimal);
        p.Add("@Margen", dto.Margen, DbType.Decimal);
        p.Add("@Precio", dto.Precio, DbType.Decimal);
        // El SP calcula Existencia automáticamente o la deja en 0
        p.Add("@Existencia", 0m, DbType.Decimal);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_detalle_producto]",
                p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idDetalle, DetalleProductoSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Iddetalle", idDetalle, DbType.Int32);
        p.Add("@Codigo_Barra", dto.CodigoBarra, DbType.String);
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Idmedida", dto.Idmedida, DbType.Int32);
        p.Add("@Unidades", dto.Unidades, DbType.Decimal);
        p.Add("@Costo", dto.Costo, DbType.Decimal);
        p.Add("@Margen", dto.Margen, DbType.Decimal);
        p.Add("@Precio", dto.Precio, DbType.Decimal);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_detalle_producto]",
                p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idDetalle, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Iddetalle", idDetalle, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_detalle_producto]",
                p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}