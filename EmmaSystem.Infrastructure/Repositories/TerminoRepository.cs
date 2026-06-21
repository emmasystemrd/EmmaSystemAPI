using Dapper;
using EmmaSystem.Application.DTOs.Termino;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class TerminoRepository : ITerminoRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public TerminoRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<TerminoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<TerminoDto>(
            new CommandDefinition("[dbo].[spmostrar_termino]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<TerminoDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<TerminoDto>(
            new CommandDefinition("[dbo].[spbuscar_termino]",
                new { textobuscar = texto, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<TerminoDto?> GetByIdAsync(int idTermino, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryFirstOrDefaultAsync<TerminoDto>(
            new CommandDefinition("[dbo].[spbuscar_termino_codigo]",
                new { Idtermino = idTermino, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task CreateAsync(TerminoSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

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
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

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
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_termino]",
                new { idtermino = idTermino },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}