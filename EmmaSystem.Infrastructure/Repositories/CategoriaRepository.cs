using Dapper;
using EmmaSystem.Application.DTOs.Categoria;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class CategoriaRepository : ICategoriaRepository
{
    private readonly SqlConnectionFactory _factory;
    public CategoriaRepository(SqlConnectionFactory factory) => _factory = factory;

    /// <summary>
    /// Listado general para administración (CRUD). Usa spmostrar_categoria.
    /// </summary>
    public async Task<IReadOnlyList<CategoriaDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<CategoriaDto>(
            new CommandDefinition("[dbo].[spmostrar_categoria]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    /// <summary>
    /// Listado simplificado para combos en Artículos. Usa spmostrar_categoria_articulo.
    /// Filtra por Tipo='A' internamente en el SP.
    /// </summary>
    public async Task<IReadOnlyList<CategoriaDto>> GetForArticuloAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idempresa", idEmpresa, DbType.Int32);
        p.Add("@Tipo", "A", DbType.String); // El SP espera este parámetro

        var result = await conn.QueryAsync<CategoriaDto>(
            new CommandDefinition("[dbo].[spmostrar_categoria_articulo]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    /// <summary>
    /// Búsqueda por texto (Nombre, Descripción o Tipo).
    /// Usa spbuscar_categoria con LIKE interno.
    /// </summary>
    public async Task<IReadOnlyList<CategoriaDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@TextoBuscar", texto, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryAsync<CategoriaDto>(
            new CommandDefinition("[dbo].[spbuscar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    public async Task CreateAsync(CategoriaSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task UpdateAsync(int idCategoria, CategoriaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idcategoria", idCategoria, DbType.Int32);
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idCategoria, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_categoria]",
                new { Idcategoria = idCategoria },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}