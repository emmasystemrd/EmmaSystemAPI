using Dapper;
using EmmaSystem.Application.DTOs.Configuracion;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class EnCFRepository : IEnCFRepository
{
    private readonly SqlConnectionFactory _factory;

    public EnCFRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<ENcfDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<ENcfDto>(
            new CommandDefinition("[dbo].[spmostrar_encf]", commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<ENcfDto>> SearchAsync(string tipo, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        // Si viene "0" o vacío, el SP lo convierte a NULL y trae todo
        var searchTipo = string.IsNullOrWhiteSpace(tipo) || tipo == "0" ? "0" : tipo;

        var result = await conn.QueryAsync<ENcfDto>(
            new CommandDefinition("[dbo].[spbuscar_encf]", new { Tipo = searchTipo }, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<int> InsertAsync(ENcfDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Tipo", dto.Tipo);
        p.Add("@Desde", dto.Desde);
        p.Add("@Hasta", dto.Hasta);
        p.Add("@Secuencia", dto.Secuencia);
        p.Add("@Aviso", dto.Aviso);
        p.Add("@Vencimiento", dto.Vencimiento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_encf]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));

        // Opcional: Si necesitas el ID insertado, agrega OUTPUT al SP o haz un SELECT SCOPE_IDENTITY()
        return 0;
    }

    public async Task UpdateAsync(int id, ENcfDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idencf", id);
        p.Add("@Tipo", dto.Tipo);
        p.Add("@Desde", dto.Desde);
        p.Add("@Hasta", dto.Hasta);
        p.Add("@Secuencia", dto.Secuencia);
        p.Add("@Aviso", dto.Aviso);
        p.Add("@Vencimiento", dto.Vencimiento);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_encf]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM eNCF WHERE IdeNCF = @Id",
            new { Id = id });
    }
}