using Dapper;
using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace EmmaSystem.Infrastructure.Services;

public class LicenciaService : ILicenciaService
{
    private readonly SqlConnectionFactory _centralFactory;

    public LicenciaService(SqlConnectionFactory centralFactory)
    {
        _centralFactory = centralFactory;
    }

    public async Task<LicenciaValidationResultDto> ValidarAsync(int idCliente, byte[] secretKey, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        // 1. Validar clave secreta
        const string validateKeySql = "SELECT COUNT(1) FROM clientes WHERE IdCliente = @Id AND SecretKey = @Key";
        var isValidKey = await conn.ExecuteScalarAsync<int>(validateKeySql, new { Id = idCliente, Key = secretKey });

        if (isValidKey == 0)
        {
            return new LicenciaValidationResultDto
            {
                EsValida = false,
                Mensaje = "Clave secreta inválida. Por favor, inicie sesión central nuevamente."
            };
        }

        // 2. Obtener información de licencia
        const string getLicenseSql = @"
            SELECT EstadoLicencia, FechaVencimiento, FechaGracia, UltimaValidacion
            FROM licencias
            WHERE IdCliente = @Id";

        var license = await conn.QueryFirstOrDefaultAsync<dynamic>(getLicenseSql, new { Id = idCliente });

        if (license == null)
        {
            return new LicenciaValidationResultDto
            {
                EsValida = false,
                Mensaje = "Licencia no encontrada."
            };
        }

        DateTime hoy = DateTime.UtcNow.Date;
        DateTime fechaVencimiento = license.FechaVencimiento;
        DateTime? fechaGracia = license.FechaGracia;

        bool esValida = false;
        string mensaje = string.Empty;
        int? diasGracia = null;

        // 3. Validar estado y fechas
        if ((byte)license.EstadoLicencia != 1)
        {
            mensaje = "Licencia suspendida o cancelada.";
        }
        else if (hoy <= fechaVencimiento)
        {
            esValida = true;
            mensaje = "Licencia válida.";
        }
        else if (fechaGracia.HasValue && hoy <= fechaGracia.Value)
        {
            esValida = true;
            diasGracia = (fechaGracia.Value - hoy).Days;
            mensaje = $"Licencia en período de gracia. Días restantes: {diasGracia}";
        }
        else
        {
            mensaje = "Licencia vencida. Contacte al administrador.";
        }

        // 4. Si es válida, actualizar última validación y período de gracia
        if (esValida)
        {
            const string updateSql = @"
                UPDATE licencias
                SET UltimaValidacion = @Now,
                    FechaGracia = DATEADD(day, 7, @Now)
                WHERE IdCliente = @Id";

            await conn.ExecuteAsync(updateSql, new { Now = DateTime.UtcNow, Id = idCliente });

            return new LicenciaValidationResultDto
            {
                EsValida = true,
                Mensaje = mensaje,
                UltimaValidacion = DateTime.UtcNow,
                DiasGraciaRestantes = diasGracia
            };
        }

        return new LicenciaValidationResultDto
        {
            EsValida = esValida,
            Mensaje = mensaje,
            UltimaValidacion = license.UltimaValidacion,
            DiasGraciaRestantes = diasGracia
        };
    }
}