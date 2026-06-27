using Dapper;
using EmmaSystem.Application.DTOs.Admin;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using EmmaSystem.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace EmmaSystem.Infrastructure.Services;

public sealed partial class AdminService : IAdminService
{
    private readonly SqlConnectionFactory _centralFactory;
    private readonly ICifradoService _cifradoService;

    private const string SqlUser = "sa";
    private const string SqlPassword = "$mmaSystem2021";
    private const string TemplateDbName = "EmmaSystem_Template";

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]{1,127}$")]
    private static partial Regex NombreBdValidoRegex();

    public AdminService(
        SqlConnectionFactory centralFactory,
        ICifradoService cifradoService)
    {
        _centralFactory = centralFactory ?? throw new ArgumentNullException(nameof(centralFactory));
        _cifradoService = cifradoService ?? throw new ArgumentNullException(nameof(cifradoService));
    }

    // ═══════════════════════════════════════════════════════
    // VALIDACIÓN PREVIA
    // ═══════════════════════════════════════════════════════
    public async Task<ValidarRegistroResponseDto> ValidarRegistroAsync(
        RegistrarClienteRequestDto request, CancellationToken ct = default)
    {
        var errores = new List<string>();

        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        var primeraEmpresa = request.Empresas?.FirstOrDefault();

        // 1. Validar nombre de BD
        if (primeraEmpresa == null || string.IsNullOrWhiteSpace(primeraEmpresa.NombreBD))
        {
            errores.Add("El nombre de la base de datos es obligatorio.");
        }
        else if (!NombreBdValidoRegex().IsMatch(primeraEmpresa.NombreBD))
        {
            errores.Add("El nombre de la base de datos no es válido para SQL Server. " +
                        "Debe comenzar con letra o guion bajo, sin espacios ni caracteres especiales, " +
                        "máximo 128 caracteres.");
        }

        // 2. Validar que la BD no exista
        if (primeraEmpresa != null && !string.IsNullOrWhiteSpace(primeraEmpresa.NombreBD)
            && NombreBdValidoRegex().IsMatch(primeraEmpresa.NombreBD))
        {
            var bdExiste = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @Nombre",
                    new { Nombre = primeraEmpresa.NombreBD },
                    cancellationToken: ct));

            if (bdExiste > 0)
                errores.Add($"La base de datos '{primeraEmpresa.NombreBD}' ya existe en el servidor.");
        }

        // 3. Validar que EmmaSystem_Template exista
        var templateExiste = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @Nombre",
                new { Nombre = TemplateDbName },
                cancellationToken: ct));

        if (templateExiste == 0)
            errores.Add($"La base de datos plantilla '{TemplateDbName}' no existe en el servidor. Contacte al administrador.");

        // 4. Validar email admin único
        if (!string.IsNullOrWhiteSpace(request.EmailAdmin))
        {
            var emailExiste = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM usuarios_central WHERE Email = @Email AND Estado = 1",
                    new { Email = request.EmailAdmin.Trim().ToLower() },
                    cancellationToken: ct));

            if (emailExiste > 0)
                errores.Add($"El email '{request.EmailAdmin}' ya está registrado.");
        }

        // 5. Validar RNC único
        if (!string.IsNullOrWhiteSpace(request.RNC))
        {
            var rncExiste = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM clientes WHERE RNC = @RNC AND Estado = 1",
                    new { RNC = request.RNC },
                    cancellationToken: ct));

            if (rncExiste > 0)
                errores.Add($"El RNC '{request.RNC}' ya está registrado.");
        }

        // 6. Validar correo principal único
        if (!string.IsNullOrWhiteSpace(request.CorreoPrincipal))
        {
            var correoExiste = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM clientes WHERE CorreoPrincipal = @Correo AND Estado = 1",
                    new { Correo = request.CorreoPrincipal.Trim().ToLower() },
                    cancellationToken: ct));

            if (correoExiste > 0)
                errores.Add($"El correo '{request.CorreoPrincipal}' ya está registrado.");
        }

        // 7. Validar plan activo
        if (request.IdPlan <= 0)
        {
            errores.Add("Debe seleccionar un plan.");
        }
        else
        {
            var planActivo = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM planes WHERE IdPlan = @IdPlan AND Estado = 1",
                    new { IdPlan = request.IdPlan },
                    cancellationToken: ct));

            if (planActivo == 0)
                errores.Add("El plan seleccionado no existe o no está activo.");
        }

        // 8. Validar contraseña
        if (string.IsNullOrWhiteSpace(request.PasswordAdmin) || request.PasswordAdmin.Length < 6)
            errores.Add("La contraseña debe tener mínimo 6 caracteres.");

        // 9. Validar nombre empresa
        if (primeraEmpresa == null || string.IsNullOrWhiteSpace(primeraEmpresa.NombreEmpresa))
            errores.Add("El nombre de la empresa es obligatorio.");

        return new ValidarRegistroResponseDto
        {
            EsValido = errores.Count == 0,
            Errores = errores
        };
    }

    // ═══════════════════════════════════════════════════════
    // REGISTRO COMPLETO
    // ═══════════════════════════════════════════════════════
    public async Task<RegistrarClienteResponseDto> RegistrarClienteAsync(
        RegistrarClienteRequestDto request, CancellationToken ct = default)
    {
        var validacion = await ValidarRegistroAsync(request, ct);
        if (!validacion.EsValido)
            throw new ArgumentException(string.Join("; ", validacion.Errores));

        // ✅ Leer datos de empresa DESDE EL ARRAY
        var empresa = request.Empresas.First();

        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        // ═══ PASO 1: Restaurar BD Template desde backup estático ═══
        // El backup pre-existente está en C:\EmmaSystem\EmmaSystem_Template.bak
        // Solo se hace RESTORE WITH MOVE, sin BACKUP previo

        var nombreBDSeguro = empresa.NombreBD.Replace("]", "]]");
        const string backupPath = @"C:\EmmaSystem\EmmaSystem_Template.bak";

        // 1a. Verificar que el backup estático exista
        if (!File.Exists(backupPath))
        {
            throw new InvalidOperationException(
                $"El backup estático '{backupPath}' no existe. " +
                "Ejecute BACKUP DATABASE [EmmaSystem_Template] TO DISK = 'C:\\EmmaSystem\\EmmaSystem_Template.bak' en SSMS.");
        }

        // 1b. Obtener rutas lógicas del backup para el MOVE
        const string sqlFileList = "RESTORE FILELISTONLY FROM DISK = @BackupPath";
        var fileList = await conn.QueryAsync<dynamic>(
            new CommandDefinition(sqlFileList,
                new { BackupPath = backupPath },
                cancellationToken: ct));

        var logicalDataName = fileList.First(f => f.Type == "D").LogicalName as string;
        var logicalLogName = fileList.First(f => f.Type == "L").LogicalName as string;

        // 1c. Determinar carpetas de datos/log por defecto del servidor
        var dataPath = await conn.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "SELECT SERVERPROPERTY('InstanceDefaultDataPath')",
                cancellationToken: ct)) ?? @"C:\Program Files\Microsoft SQL Server\MSSQL16.EMMASYSTEM\MSSQL\DATA";

        var logPath = await conn.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "SELECT SERVERPROPERTY('InstanceDefaultLogPath')",
                cancellationToken: ct)) ?? dataPath;

        var newDataFile = Path.Combine(dataPath, $"{empresa.NombreBD}.mdf");
        var newLogFile = Path.Combine(logPath, $"{empresa.NombreBD}_log.ldf");

        // 1d. Restaurar como nueva BD
        var sqlRestore = $@"
    RESTORE DATABASE [{nombreBDSeguro}] 
    FROM DISK = @BackupPath 
    WITH 
        MOVE @LogicalData TO @NewDataFile,
        MOVE @LogicalLog TO @NewLogFile,
        REPLACE";

        try
        {
            await conn.ExecuteAsync(
                new CommandDefinition(sqlRestore,
                    new
                    {
                        BackupPath = backupPath,
                        LogicalData = logicalDataName,
                        NewDataFile = newDataFile,
                        LogicalLog = logicalLogName,
                        NewLogFile = newLogFile
                    },
                    commandTimeout: 120,
                    cancellationToken: ct));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error al restaurar la base de datos plantilla: {ex.Message}", ex);
        }

        // ✅ Ya no hay backup temporal que limpiar
        var bdCreada = true;

        try
        {
            // ═══ PASO 2: Transacción para inserts ═══
            using var transaction = conn.BeginTransaction();

            try
            {
                // 2a. Crear Cliente + Salt
                var saltBytes = RandomNumberGenerator.GetBytes(64);
                var codigoCliente = await GenerarCodigoClienteAsync(conn, transaction, ct);

                const string sqlCliente = @"
                    INSERT INTO clientes (CodigoCliente, RazonSocial, RNC, CorreoPrincipal, Telefono, Estado, SaltCifrado)
                    VALUES (@CodigoCliente, @RazonSocial, @RNC, @CorreoPrincipal, @Telefono, 1, @SaltCifrado);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var idCliente = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlCliente,
                        new
                        {
                            CodigoCliente = codigoCliente,
                            request.RazonSocial,
                            request.RNC,
                            CorreoPrincipal = request.CorreoPrincipal.Trim().ToLower(),
                            request.Telefono,
                            SaltCifrado = saltBytes
                        },
                        transaction: transaction, cancellationToken: ct));

                // 2b. Crear Usuario Central
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordAdmin);

                const string sqlUsuario = @"
                    INSERT INTO usuarios_central (IdCliente, Email, PasswordHash, NombreCompleto, EsSuperAdmin, Estado)
                    VALUES (@IdCliente, @Email, @PasswordHash, @NombreCompleto, 1, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var idUsuarioCentral = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlUsuario,
                        new
                        {
                            IdCliente = idCliente,
                            Email = request.EmailAdmin.Trim().ToLower(),
                            PasswordHash = passwordHash,
                            NombreCompleto = request.NombreCompletoAdmin
                        },
                        transaction: transaction, cancellationToken: ct));

                // 2c. Crear Licencia
                // ✅ DESPUÉS (Convertir a DateTime para compatibilidad con Dapper)
                var fechaInicio = DateTime.UtcNow.Date;
                var fechaVencimiento = fechaInicio.AddYears(1);
                var fechaGracia = fechaVencimiento.AddDays(15);

                const string sqlLicencia = @"
    INSERT INTO licencias (IdCliente, IdPlan, EstadoLicencia, FechaInicio, FechaVencimiento, FechaGracia)
    VALUES (@IdCliente, @IdPlan, 1, @FechaInicio, @FechaVencimiento, @FechaGracia);
    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var idLicencia = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlLicencia,
                        new
                        {
                            IdCliente = idCliente,
                            request.IdPlan,
                            FechaInicio = fechaInicio,           // ← DateTime ✅
                            FechaVencimiento = fechaVencimiento,  // ← DateTime ✅
                            FechaGracia = fechaGracia             // ← DateTime ✅
                        },
                        transaction: transaction, cancellationToken: ct));

                // 2d. Generar cadena de conexión y cifrar
                // ✅ Usar empresa.ServidorBD y empresa.NombreBD
                var connectionString = $"Server={empresa.ServidorBD};Database={empresa.NombreBD};User Id={SqlUser};Password={SqlPassword};TrustServerCertificate=True;";

                // ✅ CORRECCIÓN: Cifrar retorna byte[], el IV sale por out parameter
                byte[] iv;
                var cadenaCifrada = _cifradoService.Cifrar(connectionString, saltBytes, out iv);

                // 2e. Crear Empresa Contratada
                // ✅ Usar empresa.NombreEmpresa, empresa.NombreBD, empresa.ServidorBD, empresa.RncCedula
                const string sqlEmpresa = @"
                    INSERT INTO empresas_contratadas 
                        (IdCliente, NombreEmpresa, NombreBD, ServidorBD, CadenaConexionEnc, VectorIV, Estado, EsEmpresaDefault, RncCedula, Ambiente)
                    VALUES 
                        (@IdCliente, @NombreEmpresa, @NombreBD, @ServidorBD, @CadenaConexionEnc, @VectorIV, 1, 1, @RncCedula, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var idEmpresa = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlEmpresa,
                        new
                        {
                            IdCliente = idCliente,
                            NombreEmpresa = empresa.NombreEmpresa,
                            NombreBD = empresa.NombreBD,
                            ServidorBD = empresa.ServidorBD,
                            CadenaConexionEnc = cadenaCifrada,
                            VectorIV = iv,
                            RncCedula = empresa.RncCedula
                        },
                        transaction: transaction, cancellationToken: ct));

                // 2f. Asignar Empresa al Usuario Central
                const string sqlAsignacion = @"
                    INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa)
                    VALUES (@IdUsuarioCentral, @IdEmpresa);";

                await conn.ExecuteAsync(
                    new CommandDefinition(sqlAsignacion,
                        new { IdUsuarioCentral = idUsuarioCentral, IdEmpresa = idEmpresa },
                        transaction: transaction, cancellationToken: ct));

                transaction.Commit();

                return new RegistrarClienteResponseDto
                {
                    IdCliente = idCliente,
                    CodigoCliente = codigoCliente,
                    IdUsuarioCentral = idUsuarioCentral,
                    IdLicencia = idLicencia,
                    IdEmpresa = idEmpresa,
                    Mensaje = $"Cliente '{request.RazonSocial}' registrado con empresa '{empresa.NombreEmpresa}'."
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex) when (bdCreada)
        {
            // ═══ COMPENSACIÓN: Eliminar BD si los inserts fallaron ═══
            try
            {
                var sqlDesconectar = $"ALTER DATABASE [{nombreBDSeguro}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                await conn.ExecuteAsync(new CommandDefinition(sqlDesconectar, cancellationToken: ct));

                var sqlEliminar = $"DROP DATABASE [{nombreBDSeguro}]";
                await conn.ExecuteAsync(new CommandDefinition(sqlEliminar, cancellationToken: ct));
            }
            catch
            {
                // En producción, agregar logging aquí
            }

            throw new InvalidOperationException(
                $"Error al registrar cliente. La base de datos '{empresa.NombreBD}' fue eliminada como compensación. " +
                $"Detalle: {ex.Message}", ex);
        }
    }

    private static async Task<string> GenerarCodigoClienteAsync(
        IDbConnection conn, IDbTransaction transaction, CancellationToken ct)
    {
        const string sql = @"
            SELECT RIGHT('CLI' + RIGHT('000' + CAST(ISNULL(MAX(CAST(RIGHT(CodigoCliente, LEN(CodigoCliente) - 3) AS INT)), 0) + 1 AS VARCHAR(3)), 3), 6)
            FROM clientes;";

        var resultado = await conn.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, transaction: transaction, cancellationToken: ct));

        return resultado ?? "CLI001";
    }
    // ═══════════════════════════════════════════════════════
    // REGISTRO DE BD EXISTENTE (MIGRACIÓN)
    // ═══════════════════════════════════════════════════════
    public async Task<RegistrarClienteResponseDto> RegistrarBDExistenteAsync(
        RegistrarBDExistenteRequestDto request, CancellationToken ct = default)
    {
        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        // ═══ VALIDACIONES ═══
        var errores = new List<string>();

        // 1. Validar que la BD exista en el servidor
        var bdExiste = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @Nombre",
                new { Nombre = request.NombreBD },
                cancellationToken: ct));

        if (bdExiste == 0)
            errores.Add($"La base de datos '{request.NombreBD}' no existe en el servidor.");

        // 2. Validar que la BD tenga tabla Empresa (es una BD de EmmaSystem)
        if (bdExiste > 0)
        {
            try
            {
                var connectionString = $"Server={request.ServidorBD};Database={request.NombreBD};User Id={SqlUser};Password={SqlPassword};TrustServerCertificate=True;";
                using var bdConn = new SqlConnection(connectionString);
                await bdConn.OpenAsync(ct);

                var tablaExiste = await bdConn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Empresa'");

                if (tablaExiste == 0)
                    errores.Add($"La base de datos '{request.NombreBD}' no parece ser una base de datos de EmmaSystem (no tiene tabla Empresa).");
            }
            catch (Exception ex)
            {
                errores.Add($"No se pudo conectar a la base de datos '{request.NombreBD}': {ex.Message}");
            }
        }

        // 3. Validar email admin único
        if (!string.IsNullOrWhiteSpace(request.EmailAdmin))
        {
            var emailExiste = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM usuarios_central WHERE Email = @Email AND Estado = 1",
                    new { Email = request.EmailAdmin.Trim().ToLower() },
                    cancellationToken: ct));

            if (emailExiste > 0)
                errores.Add($"El email '{request.EmailAdmin}' ya está registrado.");
        }

        // 4. Validar RNC único
        if (!string.IsNullOrWhiteSpace(request.RNC))
        {
            var rncExiste = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM clientes WHERE RNC = @RNC AND Estado = 1",
                    new { RNC = request.RNC },
                    cancellationToken: ct));

            if (rncExiste > 0)
                errores.Add($"El RNC '{request.RNC}' ya está registrado.");
        }

        // 5. Validar plan activo
        if (request.IdPlan <= 0)
        {
            errores.Add("Debe seleccionar un plan.");
        }
        else
        {
            var planActivo = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM planes WHERE IdPlan = @IdPlan AND Estado = 1",
                    new { IdPlan = request.IdPlan },
                    cancellationToken: ct));

            if (planActivo == 0)
                errores.Add("El plan seleccionado no existe o no está activo.");
        }

        // 6. Validar contraseña
        if (string.IsNullOrWhiteSpace(request.PasswordAdmin) || request.PasswordAdmin.Length < 6)
            errores.Add("La contraseña debe tener mínimo 6 caracteres.");

        if (errores.Count > 0)
            throw new ArgumentException(string.Join("; ", errores));

        // ═══ REGISTRO ═══
        using var transaction = conn.BeginTransaction();

        try
        {
            // 1. Crear Cliente + Salt
            var saltBytes = RandomNumberGenerator.GetBytes(64);
            var codigoCliente = await GenerarCodigoClienteAsync(conn, transaction, ct);

            const string sqlCliente = @"
            INSERT INTO clientes (CodigoCliente, RazonSocial, RNC, CorreoPrincipal, Telefono, Estado, SaltCifrado)
            VALUES (@CodigoCliente, @RazonSocial, @RNC, @CorreoPrincipal, @Telefono, 1, @SaltCifrado);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idCliente = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sqlCliente,
                    new
                    {
                        CodigoCliente = codigoCliente,
                        request.RazonSocial,
                        request.RNC,
                        CorreoPrincipal = request.CorreoPrincipal.Trim().ToLower(),
                        request.Telefono,
                        SaltCifrado = saltBytes
                    },
                    transaction: transaction, cancellationToken: ct));

            // 2. Crear Usuario Central
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordAdmin);

            const string sqlUsuario = @"
            INSERT INTO usuarios_central (IdCliente, Email, PasswordHash, NombreCompleto, EsSuperAdmin, Estado)
            VALUES (@IdCliente, @Email, @PasswordHash, @NombreCompleto, 1, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idUsuarioCentral = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sqlUsuario,
                    new
                    {
                        IdCliente = idCliente,
                        Email = request.EmailAdmin.Trim().ToLower(),
                        PasswordHash = passwordHash,
                        NombreCompleto = request.NombreCompletoAdmin
                    },
                    transaction: transaction, cancellationToken: ct));

            // 3. Crear Licencia
            var fechaInicio = DateTime.UtcNow.Date;
            var fechaVencimiento = fechaInicio.AddYears(1);
            var fechaGracia = fechaVencimiento.AddDays(15);

            const string sqlLicencia = @"
            INSERT INTO licencias (IdCliente, IdPlan, EstadoLicencia, FechaInicio, FechaVencimiento, FechaGracia)
            VALUES (@IdCliente, @IdPlan, 1, @FechaInicio, @FechaVencimiento, @FechaGracia);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idLicencia = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sqlLicencia,
                    new
                    {
                        IdCliente = idCliente,
                        request.IdPlan,
                        FechaInicio = fechaInicio,
                        FechaVencimiento = fechaVencimiento,
                        FechaGracia = fechaGracia
                    },
                    transaction: transaction, cancellationToken: ct));

            // 4. Generar cadena de conexión y cifrar
            var connectionString = $"Server={request.ServidorBD};Database={request.NombreBD};User Id={SqlUser};Password={SqlPassword};TrustServerCertificate=True;";
            byte[] iv;
            var cadenaCifrada = _cifradoService.Cifrar(connectionString, saltBytes, out iv);

            // 5. Crear Empresa Contratada (sin crear BD, solo registrar)
            const string sqlEmpresa = @"
            INSERT INTO empresas_contratadas
                (IdCliente, NombreEmpresa, NombreBD, ServidorBD, CadenaConexionEnc, VectorIV, Estado, EsEmpresaDefault, RncCedula, Ambiente)
            VALUES
                (@IdCliente, @NombreEmpresa, @NombreBD, @ServidorBD, @CadenaConexionEnc, @VectorIV, 1, 1, @RncCedula, 3);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idEmpresa = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sqlEmpresa,
                    new
                    {
                        IdCliente = idCliente,
                        NombreEmpresa = request.NombreEmpresa,
                        NombreBD = request.NombreBD,
                        ServidorBD = request.ServidorBD,
                        CadenaConexionEnc = cadenaCifrada,
                        VectorIV = iv,
                        RncCedula = request.RncCedula
                    },
                    transaction: transaction, cancellationToken: ct));

            // 6. Asignar Empresa al Usuario Central
            const string sqlAsignacion = @"
            INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa)
            VALUES (@IdUsuarioCentral, @IdEmpresa);";

            await conn.ExecuteAsync(
                new CommandDefinition(sqlAsignacion,
                    new { IdUsuarioCentral = idUsuarioCentral, IdEmpresa = idEmpresa },
                    transaction: transaction, cancellationToken: ct));

            transaction.Commit();

            return new RegistrarClienteResponseDto
            {
                IdCliente = idCliente,
                CodigoCliente = codigoCliente,
                IdUsuarioCentral = idUsuarioCentral,
                IdLicencia = idLicencia,
                IdEmpresa = idEmpresa,
                Mensaje = $"Cliente '{request.RazonSocial}' registrado con empresa existente '{request.NombreEmpresa}'."
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}