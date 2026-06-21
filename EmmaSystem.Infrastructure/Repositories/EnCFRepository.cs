using Dapper;
using EmmaSystem.Application.DTOs.Configuracion;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class EnCFRepository : IEnCFRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public EnCFRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<ENcfDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<ENcfDto>(
            new CommandDefinition("[dbo].[spmostrar_encf]",
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<ENcfDto>> SearchAsync(string tipo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var searchTipo = string.IsNullOrWhiteSpace(tipo) || tipo == "0" ? "0" : tipo;

        var result = await conn.QueryAsync<ENcfDto>(
            new CommandDefinition("[dbo].[spbuscar_encf]",
                new { Tipo = searchTipo },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<int> InsertAsync(ENcfDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Tipo", dto.Tipo);
        p.Add("@Desde", dto.Desde);
        p.Add("@Hasta", dto.Hasta);
        p.Add("@Secuencia", dto.Secuencia);
        p.Add("@Aviso", dto.Aviso);
        p.Add("@Vencimiento", dto.Vencimiento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_encf]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return 0;
    }

    public async Task UpdateAsync(int id, ENcfDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Idencf", id);
        p.Add("@Tipo", dto.Tipo);
        p.Add("@Desde", dto.Desde);
        p.Add("@Hasta", dto.Hasta);
        p.Add("@Secuencia", dto.Secuencia);
        p.Add("@Aviso", dto.Aviso);
        p.Add("@Vencimiento", dto.Vencimiento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_encf]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            "DELETE FROM eNCF WHERE IdeNCF = @Id",
            new { Id = id });
    }
}