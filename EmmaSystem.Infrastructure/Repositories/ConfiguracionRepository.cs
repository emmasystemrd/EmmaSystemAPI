using Dapper;
using EmmaSystem.Application.DTOs.Configuracion;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;
using EmmaSystem.Infrastructure.Settings; // ← 1. Agregar este using
using Microsoft.Extensions.Options;
using OpenQA.Selenium;       // ← 2. Agregar este using
namespace EmmaSystem.Infrastructure.Repositories;

public sealed class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly SqlConnectionFactory _factory;
    private readonly string _passPhrase; // ← 3. Campo para guardar la clave

    // ← 4. Modificar el constructor para inyectar IOptions<EncryptionSettings>
    public ConfiguracionRepository(
        SqlConnectionFactory factory,
        IOptions<EncryptionSettings> encryptionOptions)
    {
        _factory = factory;
        _passPhrase = encryptionOptions.Value.PassPhrase; // ← 5. Asignar la clave desde appsettings
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTURACIÓN ELECTRÓNICA
    // ═══════════════════════════════════════════════════════════════

    public async Task<ConfFacturacionElectronicaDto?> GetFacturacionElectronicaAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync<FactElectronicaSqlRow>(
            "SELECT TOP 1 Id, Envio_Inmediato, Ambiente, Certificado_Digital, Clave, Email, Clave_Email, FechaExpira FROM conf_fact_electronica",
            commandType: CommandType.Text);

        if (row == null) return null;

        return new ConfFacturacionElectronicaDto  // ✅ Debe ser ConfFacturacionElectronicaDto, NO ConfFacturacionDto
        {
            Id = row.Id,
            Envio_Inmediato = row.Envio_Inmediato,
            Ambiente = row.Ambiente,
            Email = row.Email,
            FechaExpira = row.FechaExpira,
            TieneCertificado = row.Certificado_Digital != null && row.Certificado_Digital.Length > 0,
            TieneClaveCertificado = row.Clave != null && row.Clave.Length > 0,
            TieneClaveEmail = row.Clave_Email != null && row.Clave_Email.Length > 0
        };
    }

    // ✅ Clase interna exclusiva para el mapeo seguro de SQL
    internal class FactElectronicaSqlRow
    {
        public int Id { get; set; }
        public int Envio_Inmediato { get; set; }
        public int? Ambiente { get; set; }
        public byte[]? Certificado_Digital { get; set; }
        public byte[]? Clave { get; set; }
        public string? Email { get; set; }
        public byte[]? Clave_Email { get; set; }
        public DateTime? FechaExpira { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTURACIÓN ELECTRÓNICA (Método actualizado)
    // ═══════════════════════════════════════════════════════════════

    public async Task UpdateFacturacionElectronicaAsync(ConfFacturacionElectronicaSaveDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Ambiente", dto.Ambiente);
        p.Add("@Envio", dto.Envio_Inmediato);
        p.Add("@Certificado", dto.Certificado_Digital);

        // ← 6. Usar _passPhrase en lugar del texto hardcoded
        p.Add("@PassPhrase", _passPhrase);
        p.Add("@PassPhraseBin", dto.Clave);
        p.Add("@Email", dto.Email);
        p.Add("@Clave_Email", dto.Clave_Email);
        p.Add("@FechaExpira", dto.FechaExpira);

        await conn.ExecuteAsync(@"
            UPDATE conf_fact_electronica
            SET Ambiente = @Ambiente,
                Envio_Inmediato = @Envio,
                Certificado_Digital = @Certificado,
                Clave = CASE WHEN @PassPhraseBin IS NOT NULL THEN ENCRYPTBYPASSPHRASE(@PassPhrase, CONVERT(VARBINARY, @PassPhraseBin)) ELSE Clave END,
                Email = @Email,
                Clave_Email = CASE WHEN @Clave_Email IS NOT NULL THEN ENCRYPTBYPASSPHRASE(@PassPhrase, CONVERT(VARBINARY, @Clave_Email)) ELSE Clave_Email END,
                FechaExpira = @FechaExpira",
            p, commandType: CommandType.Text);
    }


    // ═══════════════════════════════════════════════════════════════
    // PROVEEDOR / CLIENTE
    // ═══════════════════════════════════════════════════════════════

    public async Task<ConfProveedorClienteDto?> GetProveedorClienteAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ConfProveedorClienteDto>(
            new CommandDefinition(
                "[dbo].[spmostrar_configuracion_proveedor_cliente]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task UpdateProveedorClienteAsync(ConfProveedorClienteDto dto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Idempresa", idEmpresa);
        p.Add("@Idproveedor", dto.Idproveedor);
        p.Add("@Idcliente", dto.Idcliente);
        p.Add("@Idcuenta1", dto.CxP);
        p.Add("@Idcuenta2", dto.Dev_Compra);
        p.Add("@Idcuenta3", dto.Desc_Compra);
        p.Add("@Idcuenta4", dto.GastoI);
        p.Add("@Idcuenta5", dto.Caja);
        p.Add("@Idcuenta6", dto.CxC_Cia);
        p.Add("@Idcuenta7", dto.CxC);
        p.Add("@Idcuenta8", dto.Descuentos);
        p.Add("@Idcuenta9", dto.Devoluciones);
        p.Add("@Idcuenta10", dto.IngresoI);
        p.Add("@Cliente_Cumple", dto.Cliente_Cumple);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[speditar_configuracion_pc]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    // ═══════════════════════════════════════════════════════════════
    // CAPITAL
    // ═══════════════════════════════════════════════════════════════

    public async Task<(string? Capital, string? Cuenta_Cierre)> GetCapitalAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync(
            new CommandDefinition(
                "[dbo].[spmostrar_capital]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        if (result == null) return (null, null);
        return (result.Capital, result.Cuenta_Cierre);
    }

    public async Task UpdateCapitalAsync(string capital, string cuentaCierre, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[speditar_configuracion_capital]",
                new { Idempresa = idEmpresa, Idcuenta = capital, Cuenta_Cierre = cuentaCierre },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    // ═══════════════════════════════════════════════════════════════
    // IMPUESTOS
    // ═══════════════════════════════════════════════════════════════

    public async Task<ConfImpuestoDto?> GetImpuestosAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ConfImpuestoDto>(
            new CommandDefinition(
                "[dbo].[spmostrar_configuracion_impuesto]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    // ═══════════════════════════════════════════════════════════════
    // EMPLEADOS
    // ═══════════════════════════════════════════════════════════════

    public async Task<ConfEmpleadoDto?> GetEmpleadoAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ConfEmpleadoDto>(
            new CommandDefinition(
                "[dbo].[spmostrar_configuracion_empleado]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    // ═══════════════════════════════════════════════════════════════
    // TSS
    // ═══════════════════════════════════════════════════════════════

    public async Task<ConfTssDto?> GetTssAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ConfTssDto>(
            "SELECT TOP 1 * FROM Conf_TSS WHERE Idempresa = @Idempresa",
            new { Idempresa = idEmpresa },
            commandType: CommandType.Text);
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTURACIÓN (IMPRESIÓN)
    // ═══════════════════════════════════════════════════════════════

    public async Task<ConfFacturacionDto?> GetFacturacionAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ConfFacturacionDto>(
            new CommandDefinition(
                "[dbo].[spmostrar_configuracion_factura]",
                new { Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task UpdateFacturacionAsync(ConfFacturacionDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Idconf", dto.Idconf);
        p.Add("@Tipo", dto.Tipo);
        p.Add("@Vista_Previa", dto.Vista_Previa);
        p.Add("@Ancho_Papel", dto.Ancho_Papel);
        p.Add("@Margen_Papel", dto.Margen_Papel);
        p.Add("@Impresora", dto.Impresora);
        p.Add("@Mensaje", dto.Mensaje);
        p.Add("@Copia", dto.Copia);
        p.Add("@Propina_Legal", dto.Propina_Legal);
        p.Add("@Cod_PropinaLegal", dto.Cod_Propina_Legal);
        p.Add("@ITBIS_Incluido", dto.ITBIS_Incluido);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[speditar_configuracion_factura]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}