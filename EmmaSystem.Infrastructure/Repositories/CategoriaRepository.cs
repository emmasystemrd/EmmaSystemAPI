using Dapper;
using EmmaSystem.Application.DTOs.Categoria;
using EmmaSystem.Application.Interfaces;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class CategoriaRepository : ICategoriaRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    // ✅ IdEmpresa SIEMPRE es 1 dentro de cada BD de empresa
    // Los SPs lo requieren por compatibilidad con Windows Forms
    private const int EmpresaIdInterna = 1;
    public CategoriaRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Listado general para administración (CRUD). Usa spmostrar_categoria.
    /// </summary>
    public async Task<IReadOnlyList<CategoriaDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        var result = await conn.QueryAsync<CategoriaDto>(
            new CommandDefinition("[dbo].[spmostrar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    /// <summary>
    /// Listado simplificado para combos en Artículos. Usa spmostrar_categoria_articulo.
    /// Filtra por Tipo='A' internamente en el SP.
    /// </summary>
    public async Task<IReadOnlyList<CategoriaDto>> GetForArticuloAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Tipo", "A", DbType.String); // El SP espera este parámetro
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

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
    public async Task<IReadOnlyList<CategoriaDto>> SearchAsync(string texto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@TextoBuscar", texto, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        var result = await conn.QueryAsync<CategoriaDto>(
            new CommandDefinition("[dbo].[spbuscar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    public async Task CreateAsync(CategoriaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task UpdateAsync(int idCategoria, CategoriaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Idcategoria", idCategoria, DbType.Int32);
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_categoria]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idCategoria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_categoria]",
                new { Idcategoria = idCategoria },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}