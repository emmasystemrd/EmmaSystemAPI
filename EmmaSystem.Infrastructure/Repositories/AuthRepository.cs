using Dapper;
using EmmaSystem.Infrastructure.Data;
using EmmaSystem.Infrastructure.Security;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public class SpLoginRow
{
    public int Idusuario { get; set; }
    public string Nombres { get; set; } = default!;
    public int Idacceso { get; set; }
    public decimal Max_Descuento { get; set; }
    public decimal Max_Credito { get; set; }
    public string NombrePuesto { get; set; } = default!;
    public byte[]? Foto { get; set; }
    public string Estado { get; set; } = default!;
    public string NombreUsuario { get; set; } = default!;
    public string Empresa { get; set; } = default!;
    public int Idempleado { get; set; }
    public int Idempresa { get; set; }
}

public sealed class AuthRepository
{
    private readonly SqlConnectionFactory _factory;

    public AuthRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<SpLoginRow?> LoginAsync(string usuario, string password, int idEmpresa, CancellationToken ct)
    {
        // 1. Encriptar el nombre de usuario con TripleDES (igual que Windows Forms)
        string usuarioEncriptado = CryptoHelper.EncryptUsername(usuario);

        using var conn = _factory.CreateConnection();

        // PASO 1: Validar credenciales (Réplica exacta del SELECT del SP, pero con alias para Dapper)
        const string validationSql = @"
            SELECT 
                u.Idusuario,
                LTRIM(t.Nombres + ' ' + t.Apellido1 + ' ' + t.Apellido2) AS Nombres,
                u.Idacceso,
                u.Max_Descuento,
                u.Max_Credito,
                p.Nombre AS NombrePuesto,
                t.Foto,
                u.Estado,
                u.Nombre AS NombreUsuario,
                e.Nombre AS Empresa,
                t.Idempleado,
                u.Idempresa
            FROM Usuario u 
            INNER JOIN Empleado t ON u.Idempleado = t.Idempleado
            INNER JOIN Puesto p ON p.Idpuesto = t.Idpuesto
            INNER JOIN Empresa e ON e.Idempresa = u.Idempresa
            WHERE u.Nombre = @usuario 
              AND CONVERT(varchar(20), DECRYPTBYPASSPHRASE('Diosesbueno@7', u.Clave)) = @password 
              AND u.Idempresa = @Idempresa;";

        var parameters = new DynamicParameters();
        parameters.Add("@usuario", usuarioEncriptado, DbType.String);
        parameters.Add("@password", password, DbType.String); // Texto plano
        parameters.Add("@Idempresa", idEmpresa, DbType.Int32);

        var user = await conn.QueryFirstOrDefaultAsync<SpLoginRow>(
            new CommandDefinition(validationSql, parameters, cancellationToken: ct));

        // Si las credenciales son inválidas, retornamos null y NO tocamos Historial (Evita el bug del SP)
        if (user is null)
            return null;

        // PASO 2: Registrar el login en Historial (Réplica exacta del INSERT del SP)
        const string historialSql = @"
            INSERT INTO Historial (idusuario, tipo, fecha, descripcion, idempresa)
            VALUES (
                @Idusuario,
                'Sesión',
                GETDATE(),
                'El usuario ha iniciado sesión correctamente en fecha: ' + FORMAT(GETDATE(), 'dd/MM/yyyy HH:mm:ss'),
                @Idempresa
            );";

        await conn.ExecuteAsync(
            new CommandDefinition(
                historialSql,
                new { user.Idusuario, Idempresa = idEmpresa },
                cancellationToken: ct));

        return user;
    }

    public async Task<IReadOnlyList<int>> GetRolesByAccesoAsync(int idAcceso, CancellationToken ct)
    {
        const string sql = @"
            SELECT Idrol
            FROM dbo.Permiso
            WHERE Idacceso = @Idacceso;";

        using var conn = _factory.CreateConnection();
        var roles = await conn.QueryAsync<int>(
            new CommandDefinition(sql, new { Idacceso = idAcceso }, cancellationToken: ct));

        return roles.ToList();
    }
}