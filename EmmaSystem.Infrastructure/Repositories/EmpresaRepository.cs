using Dapper;
using EmmaSystem.Application.DTOs.Empresa;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class EmpresaRepository : IEmpresaRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public EmpresaRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<EmpresaDto?> GetByIdAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var row = await conn.QueryFirstOrDefaultAsync<EmpresaSqlRow>(
            @"SELECT Idempresa, Nombre, Tipo, RNC, Direccion, Telefono,
                     Email, Url, Instagram, Facebook, Registrado, Fecha_Cierre,
                     CASE WHEN Logo IS NOT NULL THEN 1 ELSE 0 END AS TieneLogo
              FROM Empresa
              WHERE Idempresa = @Idempresa",
            new { Idempresa = idEmpresa },
            commandType: CommandType.Text);

        if (row is null) return null;

        return new EmpresaDto
        {
            Idempresa = row.Idempresa,
            Nombre = row.Nombre ?? "",
            Tipo = row.Tipo ?? "",
            RNC = row.RNC ?? "",
            Direccion = row.Direccion ?? "",
            Telefono = row.Telefono ?? "",
            Email = row.Email ?? "",
            Url = row.Url ?? "",
            Instagram = row.Instagram ?? "",
            Facebook = row.Facebook ?? "",
            Registrado = row.Registrado,
            Fecha_Cierre = row.Fecha_Cierre,
            TieneLogo = row.TieneLogo == 1,
            LogoBase64 = null
        };
    }

    public async Task<byte[]?> GetLogoAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        return await conn.QueryFirstOrDefaultAsync<byte[]?>(
            "SELECT Logo FROM Empresa WHERE Idempresa = @Id",
            new { Id = idEmpresa },
            commandType: CommandType.Text);
    }

    public async Task UpdateAsync(EmpresaDto dto, byte[]? logoBytes, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var actuales = await conn.QueryFirstOrDefaultAsync(
            "SELECT Logo, Fecha_Cierre FROM Empresa WHERE Idempresa = @Id",
            new { Id = dto.Idempresa },
            commandType: CommandType.Text);

        byte[]? logoFinal = logoBytes;
        if (logoFinal == null && actuales != null)
            logoFinal = (byte[]?)actuales.Logo;

        DateTime? fechaFinal = dto.Fecha_Cierre;
        if (fechaFinal == null && actuales != null)
            fechaFinal = (DateTime?)actuales.Fecha_Cierre;

        var p = new DynamicParameters();
        p.Add("@Idempresa", dto.Idempresa);
        p.Add("@Nombre", dto.Nombre);
        p.Add("@Tipo", dto.Tipo);
        p.Add("@RNC", dto.RNC);
        p.Add("@Direccion", dto.Direccion);
        p.Add("@Telefono", dto.Telefono);
        p.Add("@Email", dto.Email);
        p.Add("@Url", dto.Url);
        p.Add("@Instagram", dto.Instagram);
        p.Add("@Facebook", dto.Facebook);
        p.Add("@Logo", logoFinal ?? (object)DBNull.Value);
        p.Add("@Registrado", dto.Registrado);
        p.Add("@Fecha_Cierre", fechaFinal ?? (object)DBNull.Value);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_empresa]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}

internal class EmpresaSqlRow
{
    public int Idempresa { get; set; }
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? RNC { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Url { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public bool Registrado { get; set; }
    public DateTime? Fecha_Cierre { get; set; }
    public int TieneLogo { get; set; }
}