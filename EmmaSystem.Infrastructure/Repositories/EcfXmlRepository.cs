using Dapper;
using EmmaSystem.Application.DTOs.Ecf;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class EcfXmlRepository : IEcfXmlRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private const string ClaveCifrado = "Diosesbueno@7";

    public EcfXmlRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<long> InsertarComprobanteAsync(
        string tipoComprobante, string secuencia, string? rncReceptor,
        decimal montoTotal, DateTime fechaComprobante,
        string xmlCompleto, string? xmlResumen,
        string codigoSeguridad, DateTime fechaFirma,
        long? idDocumentoOrigen = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@tipo_comprobante", tipoComprobante);
        p.Add("@secuencia", secuencia);
        p.Add("@rnc_receptor", rncReceptor);
        p.Add("@monto_total", montoTotal);
        p.Add("@fecha_comprobante", fechaComprobante.Date);
        p.Add("@xml_completo", xmlCompleto);
        p.Add("@xml_resumen", xmlResumen);
        p.Add("@codigo_seguridad", codigoSeguridad);
        p.Add("@fecha_firma", fechaFirma);
        p.Add("@id_documento_origen", idDocumentoOrigen);

        var result = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition("[dbo].[spinsertar_ECF]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result;
    }

    public async Task ActualizarEstadoAsync(
        long idEcf, string estadoEcf, string? codigoRespuesta,
        string? mensajeError, string? trackId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        string estadoFinal = estadoEcf;
        if (string.IsNullOrEmpty(estadoFinal))
            estadoFinal = !string.IsNullOrEmpty(trackId) ? "RECHAZADO" : "PENDIENTE";

        string mensajeFinal = estadoFinal == "Aceptado" ? "" : mensajeError ?? "";

        const string sql = @"
            UPDATE ecf_xml
            SET TrackId = @TrackId,
                estado_ecf = @EstadoEcf,
                codigo_respuesta_dgii = @CodigoRespuesta,
                mensaje_error_dgii = @MensajeError,
                fecha_envio = GETDATE()
            WHERE id_ecf = @IdEcf";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            IdEcf = idEcf,
            TrackId = trackId,
            EstadoEcf = estadoFinal,
            CodigoRespuesta = codigoRespuesta,
            MensajeError = mensajeFinal
        }, cancellationToken: ct));
    }

    public async Task AnularAsync(long idEcf, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"UPDATE ecf_xml SET estado_ecf = 'ANULADO' WHERE id_ecf = @IdEcf";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { IdEcf = idEcf }, cancellationToken: ct));
    }

    public async Task<string> ObtenerEstadoAsync(string secuencia, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(secuencia)) return "NO_ENVIADO";

        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"SELECT TOP 1 estado_ecf FROM ecf_xml WHERE secuencia = @Secuencia";
        var result = await conn.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { Secuencia = secuencia }, cancellationToken: ct));

        return result ?? "NO_ENVIADO";
    }

    public async Task<EcfResultadoDto?> ObtenerPorSecuenciaAsync(string secuencia, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(secuencia)) return null;

        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
            SELECT TOP 1 id_ecf AS IdEcf, xml_completo AS XmlCompleto,
                         xml_resumen AS XmlResumen, codigo_seguridad AS CodigoSeguridad,
                         fecha_firma AS FechaFirma
            FROM ecf_xml WHERE secuencia = @Secuencia";

        return await conn.QueryFirstOrDefaultAsync<EcfResultadoDto?>(
            new CommandDefinition(sql, new { Secuencia = secuencia.Trim() }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<EcfEstadoDto>> ListarAsync(
        DateTime? fechaInicio, DateTime? fechaFin,
        string? estadoEcf, string? tipoComprobante, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
            SELECT
                e.id_ecf AS IdEcf,
                CASE
                    WHEN e.tipo_comprobante='31' THEN '31- CRÉDITO FISCAL'
                    WHEN e.tipo_comprobante='32' THEN '32- CONSUMO'
                    WHEN e.tipo_comprobante='33' THEN '33- NOTA DÉBITO'
                    WHEN e.tipo_comprobante='34' THEN '34- NOTA CRÉDITO'
                    WHEN e.tipo_comprobante='41' THEN '41- COMPRAS'
                    WHEN e.tipo_comprobante='43' THEN '43- GASTOS MENORES'
                    WHEN e.tipo_comprobante='44' THEN '44- REGÍMENES ESPECIALES'
                    WHEN e.tipo_comprobante='45' THEN '45- GUBERNAMENTAL'
                    WHEN e.tipo_comprobante='46' THEN '46- EXPORTACIONES'
                    WHEN e.tipo_comprobante='47' THEN '47- PAGOS AL EXTERIOR'
                    ELSE e.tipo_comprobante
                END AS TipoComprobante,
                e.secuencia AS Secuencia,
                v.NCF AS NcfModificado,
                e.rnc_receptor AS RncReceptor,
                e.monto_total AS MontoTotal,
                e.fecha_comprobante AS FechaComprobante,
                CASE WHEN e.estado_ecf='' THEN 'PENDIENTE' ELSE UPPER(e.estado_ecf) END AS EstadoEcf,
                e.TrackId,
                e.codigo_respuesta_dgii AS CodigoRespuesta,
                e.mensaje_error_dgii AS MensajeError,
                CASE WHEN e.xml_resumen IS NULL THEN 0 ELSE 1 END AS TieneResumen,
                e.fecha_envio AS FechaEnvio
            FROM ecf_xml e
            LEFT JOIN Venta1 v ON v.IdVenta1 = e.id_documento_origen
            WHERE (@FechaInicio IS NULL OR e.fecha_comprobante BETWEEN @FechaInicio AND @FechaFin)
              AND (@TipoComprobante IS NULL OR e.tipo_comprobante = @TipoComprobante)
              AND (@EstadoEcf IS NULL OR e.estado_ecf = @EstadoEcf)
              AND e.estado_ecf != 'ANULADO'
            ORDER BY e.fecha_comprobante DESC, e.id_ecf DESC";

        var result = await conn.QueryAsync<EcfEstadoDto>(
            new CommandDefinition(sql, new
            {
                FechaInicio = fechaInicio?.Date,
                FechaFin = fechaFin?.Date,
                EstadoEcf = estadoEcf,
                TipoComprobante = tipoComprobante
            }, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<(string? XmlCompleto, string RncEmisor, string Secuencia)> ObtenerXmlCompletoAsync(
        long idEcf, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
            SELECT xml_completo AS XmlCompleto,
                   (SELECT TOP 1 RNC FROM Empresa) AS RncEmisor,
                   secuencia AS Secuencia
            FROM ecf_xml WHERE id_ecf = @IdEcf";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, new { IdEcf = idEcf }, cancellationToken: ct));

        if (result == null) return (null, "RNC", "NCF");

        return (
            XmlCompleto: (string?)result.XmlCompleto,
            RncEmisor: (string)result.RncEmisor ?? "RNC",
            Secuencia: (string)result.Secuencia ?? "NCF"
        );
    }

    public async Task<(string? XmlResumen, string RncEmisor, string Secuencia)> ObtenerXmlResumenAsync(
        long idEcf, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
            SELECT xml_resumen AS XmlResumen,
                   (SELECT TOP 1 RNC FROM Empresa) AS RncEmisor,
                   secuencia AS Secuencia
            FROM ecf_xml WHERE id_ecf = @IdEcf";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, new { IdEcf = idEcf }, cancellationToken: ct));

        if (result == null) return (null, "RNC", "NCF");

        return (
            XmlResumen: (string?)result.XmlResumen,
            RncEmisor: (string)result.RncEmisor ?? "RNC",
            Secuencia: (string)result.Secuencia ?? "NCF"
        );
    }

    public async Task<ConfCertificadoDto?> ObtenerConfiguracionCertificadoAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
            SELECT TOP 1
                Ambiente,
                CASE WHEN Certificado_Digital IS NOT NULL AND DATALENGTH(Certificado_Digital) > 0 THEN 1 ELSE 0 END AS TieneCertificado,
                CASE WHEN Clave IS NOT NULL AND DATALENGTH(Clave) > 0 THEN 1 ELSE 0 END AS TieneClave,
                Email,
                CASE WHEN Clave_Email IS NOT NULL AND DATALENGTH(Clave_Email) > 0 THEN 1 ELSE 0 END AS TieneClaveEmail,
                FechaExpira
            FROM conf_fact_electronica";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, cancellationToken: ct));

        if (result == null) return null;

        int ambiente = (int)result.Ambiente;
        string ambienteNombre = ambiente switch
        {
            1 => "testecf",
            2 => "certecf",
            3 => "ecf",
            _ => "testecf"
        };

        return new ConfCertificadoDto
        {
            Ambiente = ambiente,
            AmbienteNombre = ambienteNombre,
            TieneCertificado = (int)result.TieneCertificado == 1,
            TieneClave = (int)result.TieneClave == 1,
            Email = (string?)result.Email,
            TieneClaveEmail = (int)result.TieneClaveEmail == 1,
            FechaExpira = (DateTime?)result.FechaExpira
        };

    }
    public async Task<(byte[]? Certificado, string? Clave)> ObtenerCertificadoDigitalAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT TOP 1 
            Certificado_Digital,
            CONVERT(varchar(100), DECRYPTBYPASSPHRASE('Diosesbueno@7', Clave)) AS Clave
        FROM conf_fact_electronica";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, cancellationToken: ct));

        if (result == null) return (null, null);

        return (
            Certificado: (byte[]?)result.Certificado_Digital,
            Clave: (string?)result.Clave
        );
    }

    public async Task<(string RNC, string Nombre, string Direccion, string Email, string Url)> ObtenerDatosEmpresaAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT TOP 1 
            RNC, Nombre, Direccion, Email, Url
        FROM Empresa
        WHERE Idempresa = @Idempresa";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, new { Idempresa = idEmpresa }, cancellationToken: ct));

        if (result == null) return ("", "", "", "", "");

        return (
            RNC: (string?)result.RNC ?? "",
            Nombre: (string?)result.Nombre ?? "",
            Direccion: (string?)result.Direccion ?? "",
            Email: (string?)result.Email ?? "",
            Url: (string?)result.Url ?? ""
        );
    }

    public async Task<(string NumDocumento, string RazonSocial, string Email, string Direccion)> ObtenerDatosClienteAsync(int idCliente, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT TOP 1 
            Num_Documento, Razon_Social, Email, Direccion
        FROM Cliente1
        WHERE Idcliente = @Idcliente";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, new { Idcliente = idCliente }, cancellationToken: ct));

        if (result == null) return ("", "", "", "");

        return (
            NumDocumento: (string?)result.Num_Documento ?? "",
            RazonSocial: (string?)result.Razon_Social ?? "",
            Email: (string?)result.Email ?? "",
            Direccion: (string?)result.Direccion ?? ""
        );
    }
    public async Task<Dictionary<string, string>> ObtenerEstadosPorNcfAsync(IEnumerable<string> ncfs, CancellationToken ct = default)
    {
        if (ncfs == null || !ncfs.Any())
            return new Dictionary<string, string>();

        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT secuencia, estado_ecf
        FROM ecf_xml
        WHERE secuencia IN @Ncfs";

        var result = await conn.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { Ncfs = ncfs }, cancellationToken: ct));

        return result.ToDictionary(
            r => (string)r.secuencia,
            r => (string?)r.estado_ecf ?? "PENDIENTE"
        );
    }
    public async Task<EcfDatosImpresionDto?> ObtenerDatosEcfAsync(string ncf, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT TOP 1 
            secuencia AS Secuencia,
codigo_seguridad AS CodigoSeguridad,
fecha_firma AS FechaFirma,
            (SELECT TOP 1 Ambiente FROM conf_fact_electronica) AS Ambiente,
            estado_ecf AS EstadoEcf
        FROM ecf_xml
        WHERE secuencia = @Ncf";

        return await conn.QueryFirstOrDefaultAsync<EcfDatosImpresionDto>(
            new CommandDefinition(sql, new { Ncf = ncf }, cancellationToken: ct));
    }
}