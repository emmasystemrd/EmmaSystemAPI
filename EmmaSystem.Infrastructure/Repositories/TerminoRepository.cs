using Dapper;
using EmmaSystem.Application.DTOs.Termino;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class TerminoRepository : ITerminoRepository
{
    private readonly SqlConnectionFactory _factory;
    public TerminoRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<TerminoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<TerminoDto>(
            new CommandDefinition("[dbo].[spmostrar_termino]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    public async Task<IReadOnlyList<TerminoDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<TerminoDto>(
            new CommandDefinition("[dbo].[spbuscar_termino]",
                new { textobuscar = texto, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.AsList();
    }

    public async Task<TerminoDto?> GetByIdAsync(int idTermino, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<TerminoDto>(
            new CommandDefinition("[dbo].[spbuscar_termino_codigo]",
                new { Idtermino = idTermino, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result;
    }

    public async Task CreateAsync(TerminoSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@nombre", dto.Nombre, DbType.String);
        p.Add("@tipo", dto.Tipo, DbType.String);
        p.Add("@tiempo", dto.Tiempo, DbType.Int32);
        p.Add("@tasa", dto.Tasa, DbType.Decimal);
        p.Add("@no_pagos", dto.No_Pagos, DbType.Int32);
        p.Add("@dias_desc", dto.Dias_Desc, DbType.Int32);
        p.Add("@p_descuento", dto.P_Descuento, DbType.Decimal);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_termino]",
                p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idTermino, TerminoSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@idtermino", idTermino, DbType.Int32);
        p.Add("@nombre", dto.Nombre, DbType.String);
        p.Add("@tipo", dto.Tipo, DbType.String);
        p.Add("@tiempo", dto.Tiempo, DbType.Int32);
        p.Add("@tasa", dto.Tasa, DbType.Decimal);
        p.Add("@no_pagos", dto.No_Pagos, DbType.Int32);
        p.Add("@dias_desc", dto.Dias_Desc, DbType.Int32);
        p.Add("@p_descuento", dto.P_Descuento, DbType.Decimal);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_termino]",
                p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idTermino, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_termino]",
                new { idtermino = idTermino },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}