using Dapper;
using EmmaSystem.Application.DTOs.Departamento;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class DepartamentoRepository : IDepartamentoRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private const int EmpresaIdInterna = 1;
    public DepartamentoRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<DepartamentoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Idempresa", EmpresaIdInterna);

        var result = await conn.QueryAsync<DepartamentoDto>(
            new CommandDefinition("[dbo].[spmostrar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<DepartamentoDto>> SearchAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@TextoBuscar", textoBuscar);
        p.Add("@Idempresa", EmpresaIdInterna);

        var result = await conn.QueryAsync<DepartamentoDto>(
            new CommandDefinition("[dbo].[spbuscar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task CreateAsync(DepartamentoCreateDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Nombre", dto.Nombre);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idDepartamento, DepartamentoUpdateDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Iddepartamento", idDepartamento);
        p.Add("@Nombre", dto.Nombre);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idDepartamento, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Iddepartamento", idDepartamento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}