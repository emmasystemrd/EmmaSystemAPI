using Dapper;
using EmmaSystem.Application.DTOs.Departamento;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class DepartamentoRepository : IDepartamentoRepository
{
    private readonly SqlConnectionFactory _factory;

    public DepartamentoRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<DepartamentoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idempresa", idEmpresa);

        var result = await conn.QueryAsync<DepartamentoDto>(
            new CommandDefinition("[dbo].[spmostrar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.AsList();
    }

    public async Task<IReadOnlyList<DepartamentoDto>> SearchAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@TextoBuscar", textoBuscar);
        p.Add("@Idempresa", idEmpresa);

        var result = await conn.QueryAsync<DepartamentoDto>(
            new CommandDefinition("[dbo].[spbuscar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.AsList();
    }

    public async Task CreateAsync(DepartamentoCreateDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Nombre", dto.Nombre);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Idempresa", idEmpresa);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idDepartamento, DepartamentoUpdateDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Iddepartamento", idDepartamento);
        p.Add("@Nombre", dto.Nombre);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idDepartamento, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Iddepartamento", idDepartamento);

        // Esto ejecuta el Soft Delete (cambia Estado a 'E')
        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_departamento]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}