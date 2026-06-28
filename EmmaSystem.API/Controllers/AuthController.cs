using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Security; // ← Agregar este using
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // ← Agregar este using
using Dapper;
using EmmaSystem.Infrastructure.Data; // Para SqlConnectionFactory
namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICifradoService _cifradoService; // ← Nueva dependencia
    private readonly IConfiguration _config; // ← Nueva dependencia
    private readonly SqlConnectionFactory _centralFactory;
    private readonly ISesionService _sesionService; // ← AGREGAR
                                                    // ✅ Constructor actualizado con ICifradoService
    public AuthController(IAuthService authService, ICifradoService cifradoService,
       IConfiguration config, SqlConnectionFactory centralFactory,
       ISesionService sesionService) // ← MODIFICAR
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _cifradoService = cifradoService ?? throw new ArgumentNullException(nameof(cifradoService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _centralFactory = centralFactory ?? throw new ArgumentNullException(nameof(centralFactory));
        _sesionService = sesionService ?? throw new ArgumentNullException(nameof(sesionService));
    }

    /// <summary>
    /// Login inicial en EmmaSystemCentral.
    /// Valida credenciales del cliente y retorna lista de empresas disponibles.
    /// </summary>
    [HttpPost("login/central")]
    public async Task<ActionResult<LoginCentralResponseDto>> LoginCentral(
    [FromBody] LoginCentralRequestDto request,
    CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginCentralAsync(request, cancellationToken);

           

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Selecciona una empresa específica y genera token con contexto completo de tenant.
    /// Requiere token válido obtenido desde login/central.
    /// </summary>
    [HttpPost("seleccionar-empresa")]
    [Authorize]
    public async Task<ActionResult<SeleccionEmpresaResponseDto>> SeleccionarEmpresa(
        [FromBody] SeleccionEmpresaRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtener ID del usuario central desde los claims del token
            var idUsuarioCentralClaim = User.FindFirst("IdUsuarioCentral")?.Value;

            if (string.IsNullOrEmpty(idUsuarioCentralClaim))
                return Unauthorized(new { message = "Token inválido. Debe iniciar sesión primero en login/central." });

            var idUsuarioCentral = int.Parse(idUsuarioCentralClaim);

            var response = await _authService.SeleccionarEmpresaAsync(
                idUsuarioCentral,
                request.IdEmpresa,
                cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login directo a una empresa específica.
    /// Para usuarios operativos que solo tienen acceso a una empresa.
    /// Combina validación de credenciales + selección de empresa en un solo paso.
    /// </summary>
    [HttpPost("login/empresa")]
    public async Task<ActionResult<SeleccionEmpresaResponseDto>> LoginEmpresa(
        [FromBody] LoginEmpresaRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginEmpresaAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// ⚠️ TEMPORAL: Solo para generar hashes de prueba. ELIMINAR DESPUÉS.
    /// </summary>
    [HttpGet("generar-hash/{password}")]
    public IActionResult GenerarHash(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return Ok(new { password, hash });
    }
    /// <summary>
    /// ⚠️ SOLO DESARROLLO - Genera CadenaConexionEnc y VectorIV reales
    /// ELIMINAR ANTES DE PRODUCCIÓN
    /// </summary>
    [HttpPost("debug/generar-conexion")]
    [Authorize]
    public IActionResult GenerarConexion([FromBody] GenerarConexionRequest request)
    {
        var salt = Convert.FromHexString(request.SaltHex);
        var cadenaCifrada = _cifradoService.Cifrar(request.ConnectionString, salt, out var iv);

        return Ok(new
        {
            CadenaConexionEnc = $"0x{Convert.ToHexString(cadenaCifrada)}",
            VectorIV = $"0x{Convert.ToHexString(iv)}",
            ServidorBD = request.ServidorBD,
            NombreBD = request.NombreBD
        });
    }


    // DTO temporal
    public class GenerarConexionRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string SaltHex { get; set; } = string.Empty;
        public string ServidorBD { get; set; } = string.Empty;
        public string NombreBD { get; set; } = string.Empty;
    }
    // ⚠️ SOLO DEBUG - ELIMINAR INMEDIATAMENTE DESPUÉS
    [HttpGet("debug/clave-actual")]
    public IActionResult VerClaveActual()
    {
        var envKey = Environment.GetEnvironmentVariable("EMMA_ENCRYPTION_KEY");
        var configKey = _config["Encryption:AesKey"];

        return Ok(new
        {
            VariableEntorno = string.IsNullOrEmpty(envKey) ? "NO CONFIGURADA" : $"{envKey[..5]}...({envKey.Length} chars)",
            AppSettings = string.IsNullOrEmpty(configKey) ? "NO CONFIGURADA" : $"{configKey[..5]}...({configKey.Length} chars)",
            UsandoVariableEntorno = !string.IsNullOrEmpty(envKey),
            Coinciden = envKey == configKey
        });
    }
    /// <summary>
    /// ⚠️ SOLO DEBUG - Prueba round-trip con datos reales de la BD
    /// ELIMINAR ANTES DE PRODUCCIÓN
    /// </summary>
    [HttpGet("debug/roundtrip/{idEmpresa}")]
    [Authorize]
    public async Task<IActionResult> RoundTrip(int idEmpresa)
    {
        using var conn = _centralFactory.CreateConnection();

        // 1. Leer datos EXACTOS de la BD
        const string sql = @"
        SELECT 
            ec.CadenaConexionEnc,
            ec.VectorIV,
            c.SaltCifrado
        FROM empresas_contratadas ec
        INNER JOIN clientes c ON ec.IdCliente = c.IdCliente
        WHERE ec.IdEmpresa = @Id";

        var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = idEmpresa });

        if (row is null)
            return NotFound(new { error = "Empresa no encontrada" });

        byte[] encBytes = row.CadenaConexionEnc;
        byte[] ivBytes = row.VectorIV;
        byte[] saltBytes = row.SaltCifrado;

        // 2. Intentar descifrar
        try
        {
            var descifrado = _cifradoService.Descifrar(encBytes, saltBytes, ivBytes);
            return Ok(new
            {
                status = "✅ OK",
                cadenaDescifrada = descifrado,
                saltHex = Convert.ToHexString(saltBytes),
                ivHex = Convert.ToHexString(ivBytes),
                encLength = encBytes.Length
            });
        }
        catch (Exception ex)
        {
            // 3. Si falla, re-cifrar una cadena conocida con el MISMO salt y comparar
            var testString = "Server=.\\EmmaSystem;Database=TEST;";
            var reCifrado = _cifradoService.Cifrar(testString, saltBytes, out var nuevoIv);

            return BadRequest(new
            {
                status = "❌ FAIL",
                error = ex.Message,
                saltHex = Convert.ToHexString(saltBytes),
                saltLength = saltBytes.Length,
                ivHexOriginal = Convert.ToHexString(ivBytes),
                ivLengthOriginal = ivBytes.Length,
                encLength = encBytes.Length,
                pruebaRecifrado = Convert.ToHexString(reCifrado),
                nuevoIvHex = Convert.ToHexString(nuevoIv),
                mensaje = "El descifrado falló. Se generó un nuevo cifrado de prueba con el mismo salt para comparar."
            });
        }

    }
    /// <summary>
    /// Obtiene la cadena de conexión desencriptada para una empresa específica.
    /// Requiere token válido del login central.
    /// </summary>
    [HttpGet("empresa/{id:int}/conexion")]
    [Authorize]
    public async Task<ActionResult<EmpresaConexionResponseDto>> GetConexionEmpresa(int id, CancellationToken ct)
    {
        try
        {
            // Validar que el usuario tiene acceso a esta empresa
            var idUsuarioCentralClaim = User.FindFirst("IdUsuarioCentral")?.Value;
            var idClienteClaim = User.FindFirst("ClienteId")?.Value;

            if (string.IsNullOrEmpty(idUsuarioCentralClaim) || string.IsNullOrEmpty(idClienteClaim))
                return Unauthorized(new { message = "Token inválido." });

            var idUsuarioCentral = int.Parse(idUsuarioCentralClaim);
            var idCliente = int.Parse(idClienteClaim);

            // Validar que la empresa pertenece al cliente
            var esValida = await _authService.ValidarEmpresaDeClienteAsync(idCliente, id, ct);
            if (!esValida)
                return Forbid();

            // Obtener cadena de conexión desde la BD central
            using var conn = _centralFactory.CreateConnection();

            const string sql = @"
            SELECT 
                ec.CadenaConexionEnc,
                ec.VectorIV,
                ec.NombreEmpresa,
                c.SaltCifrado
            FROM empresas_contratadas ec
            INNER JOIN clientes c ON ec.IdCliente = c.IdCliente
            WHERE ec.IdEmpresa = @Id 
              AND ec.IdCliente = @IdCliente
              AND ec.Estado = 1";

            var row = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<dynamic>(
                conn, sql, new { Id = id, IdCliente = idCliente });

            if (row is null)
                return NotFound(new { message = "Empresa no encontrada o inactiva." });

            // Desencriptar la cadena de conexión
            byte[] encBytes = row.CadenaConexionEnc;
            byte[] ivBytes = row.VectorIV;
            byte[] saltBytes = row.SaltCifrado;

            string cadenaConexion;
            try
            {
                cadenaConexion = _cifradoService.Descifrar(encBytes, saltBytes, ivBytes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al desencriptar la conexión: {ex.Message}" });
            }

            return Ok(new EmpresaConexionResponseDto
            {
                IdEmpresa = id,
                NombreEmpresa = row.NombreEmpresa,
                CadenaConexion = cadenaConexion
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error interno: {ex.Message}" });
        }
    }

    // ──────────────────────────────────────────────
    // 4. HEARTBEAT (Mantener sesión viva)
    // ──────────────────────────────────────────────
    [HttpPost("sesion/heartbeat")]
    [Authorize]
    public async Task<IActionResult> Heartbeat(CancellationToken ct)
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _sesionService.HeartbeatAsync(token, ct);
            return Ok(new { message = "Sesión actualizada" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────
    // 5. LOGOUT (Cerrar sesión)
    // ──────────────────────────────────────────────
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _sesionService.CerrarSesionAsync(token, ct);
            return Ok(new { message = "Sesión cerrada correctamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────
    // 6. VALIDAR CREACIÓN DE EMPRESA
    // ──────────────────────────────────────────────
    [HttpGet("validar-crear-empresa")]
    [Authorize]
    public async Task<IActionResult> ValidarCrearEmpresa(CancellationToken ct)
    {
        try
        {
            var idCliente = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            var (puedeCrear, mensaje, actuales, maximo) = await _sesionService.ValidarCreacionEmpresaAsync(idCliente, ct);

            return Ok(new
            {
                puedeCrear,
                mensaje,
                empresasActuales = actuales,
                maxEmpresas = maximo
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────
    // 7. CREAR EMPRESA (Registro en BD central)
    // ──────────────────────────────────────────────
    [HttpPost("empresas/crear")]
    [Authorize]
    public async Task<IActionResult> CrearEmpresa([FromBody] CrearEmpresaRequest request, CancellationToken ct)
    {
        try
        {
            var idCliente = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            var idUsuarioCentral = int.Parse(User.FindFirst("IdUsuarioCentral")?.Value ?? "0");

            // 1. Validar que puede crear más empresas
            var (puedeCrear, mensaje, actuales, maximo) = await _sesionService.ValidarCreacionEmpresaAsync(idCliente, ct);
            if (!puedeCrear)
            {
                return StatusCode(403, new
                {
                    message = mensaje,
                    empresasActuales = actuales,
                    maxEmpresas = maximo
                });
            }

            using var conn = _centralFactory.CreateConnection();
            await conn.OpenAsync(ct);

            // 2. Obtener el SaltCifrado del cliente (necesario para encriptar)
            const string getSaltSql = "SELECT SaltCifrado FROM clientes WHERE IdCliente = @IdCliente";
            var saltBytes = await Dapper.SqlMapper.ExecuteScalarAsync<byte[]>(conn, getSaltSql, new { IdCliente = idCliente });

            if (saltBytes == null || saltBytes.Length == 0)
            {
                return BadRequest(new { message = "El cliente no tiene salt de cifrado configurado." });
            }

            // 3. Encriptar la cadena de conexión usando ICifradoService
            byte[] cadenaCifrada = null;
            byte[] vectorIV = null;

            if (!string.IsNullOrEmpty(request.CadenaConexion))
            {
                cadenaCifrada = _cifradoService.Cifrar(request.CadenaConexion, saltBytes, out vectorIV);
            }

            // 4. Verificar que no exista una empresa con el mismo NombreBD
            const string checkSql = @"
            SELECT COUNT(1) FROM empresas_contratadas 
            WHERE IdCliente = @IdCliente AND NombreBD = @NombreBD AND Estado = 1";
            var existe = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn, checkSql,
                new { IdCliente = idCliente, NombreBD = request.NombreBD });

            if (existe > 0)
            {
                return BadRequest(new { message = $"Ya existe una empresa registrada con la base de datos '{request.NombreBD}'." });
            }

            // 5. Insertar en empresas_contratadas CON la cadena de conexión encriptada
            const string sql = @"
            INSERT INTO empresas_contratadas 
                (IdCliente, NombreEmpresa, NombreBD, ServidorBD, CadenaConexionEnc, VectorIV,
                 Estado, EsEmpresaDefault, RncCedula, Ambiente)
            OUTPUT INSERTED.IdEmpresa
            VALUES 
                (@IdCliente, @NombreEmpresa, @NombreBD, @ServidorBD, @CadenaConexionEnc, @VectorIV,
                 1, 0, @RncCedula, @Ambiente)";

            var idEmpresa = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn, sql, new
            {
                IdCliente = idCliente,
                NombreEmpresa = request.NombreEmpresa,
                NombreBD = request.NombreBD,
                ServidorBD = request.ServidorBD,
                CadenaConexionEnc = cadenaCifrada,
                VectorIV = vectorIV,
                RncCedula = request.RncCedula,
                Ambiente = request.Ambiente
            });

            // 6. Registrar en log_accesos
            const string logSql = @"
            INSERT INTO log_accesos (IdCliente, IdEmpresa, TipoEvento, IPOrigen, EndpointAccedido, Resultado, FechaEvento)
            VALUES (@IdCliente, @IdEmpresa, 'CREAR_EMPRESA', @IP, @Endpoint, 'Exitoso', GETDATE())";

            await Dapper.SqlMapper.ExecuteAsync(conn, logSql, new
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Endpoint = "/api/auth/empresas/crear"
            });

            return Ok(new
            {
                message = "Empresa registrada correctamente en EmmaSystemCentral",
                idEmpresa,
                empresasActuales = actuales + 1,
                maxEmpresas = maximo
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al crear empresa: {ex.Message}" });
        }
    }
    // ──────────────────────────────────────────────
    // 8. ELIMINAR EMPRESA (Registro en BD central + historial)
    // ──────────────────────────────────────────────
    [HttpDelete("empresas/{id:int}")]
    [Authorize]
    public async Task<IActionResult> EliminarEmpresa(int id, [FromBody] EliminarEmpresaRequest request, CancellationToken ct)
    {
        try
        {
            var idCliente = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            var idUsuarioCentral = int.Parse(User.FindFirst("IdUsuarioCentral")?.Value ?? "0");

            using var conn = _centralFactory.CreateConnection();

            // 1. Obtener datos de la empresa antes de eliminarla
            const string getSql = @"
            SELECT NombreEmpresa, NombreBD, ServidorBD 
            FROM empresas_contratadas 
            WHERE IdEmpresa = @Id AND IdCliente = @IdCliente AND Estado = 1";

            var empresa = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<dynamic>(conn, getSql,
                new { Id = id, IdCliente = idCliente });

            if (empresa is null)
                return NotFound(new { message = "Empresa no encontrada o no le pertenece." });

            // 2. Insertar en historial
            const string historialSql = @"
            INSERT INTO empresas_eliminadas 
                (IdEmpresa, IdCliente, NombreEmpresa, NombreBD, ServidorBD, EliminadoPor, Motivo)
            VALUES 
                (@IdEmpresa, @IdCliente, @NombreEmpresa, @NombreBD, @ServidorBD, @EliminadoPor, @Motivo)";

            await Dapper.SqlMapper.ExecuteAsync(conn, historialSql, new
            {
                IdEmpresa = id,
                IdCliente = idCliente,
                NombreEmpresa = (string)empresa.NombreEmpresa,
                NombreBD = (string)empresa.NombreBD,
                ServidorBD = (string)empresa.ServidorBD,
                EliminadoPor = idUsuarioCentral,
                Motivo = request.Motivo
            });

            // 3. Marcar como inactiva (soft delete)
            const string updateSql = @"
            UPDATE empresas_contratadas 
            SET Estado = 0 
            WHERE IdEmpresa = @Id AND IdCliente = @IdCliente";

            await Dapper.SqlMapper.ExecuteAsync(conn, updateSql, new { Id = id, IdCliente = idCliente });

            return Ok(new { message = "Empresa eliminada correctamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al eliminar empresa: {ex.Message}" });
        }
    }

    // ──────────────────────────────────────────────
    // 9. OBTENER LISTA DE EMPRESAS DEL CLIENTE
    // ──────────────────────────────────────────────
    [HttpGet("empresas/lista")]
    [Authorize]
    public async Task<IActionResult> ObtenerListaEmpresas(CancellationToken ct)
    {
        try
        {
            var idCliente = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");

            using var conn = _centralFactory.CreateConnection();

            const string sql = @"
            SELECT 
                IdEmpresa AS IdEmpresa,
                NombreEmpresa AS Nombre,
                EsEmpresaDefault AS EsDefault
            FROM empresas_contratadas
            WHERE IdCliente = @IdCliente AND Estado = 1
            ORDER BY IdEmpresa";

            var empresas = await Dapper.SqlMapper.QueryAsync<EmpresaDisponibleDto>(conn, sql, new { IdCliente = idCliente });

            return Ok(empresas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al obtener lista de empresas: {ex.Message}" });
        }
    }
    // ──────────────────────────────────────────────
    // RENOVACIÓN SILENCIOSA DE TOKEN
    // ──────────────────────────────────────────────
    [HttpPost("renovar-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RenovarToken([FromBody] RenovarTokenRequest request, CancellationToken ct)
    {
        try
        {
            if (request.IdCliente <= 0 || request.SecretKey == null || request.SecretKey.Length == 0)
                return BadRequest(new { message = "Datos de renovación inválidos." });

            var resultado = await _authService.RenovarTokenAsync(request.IdCliente, request.SecretKey, ct);

            if (resultado == null)
                return Unauthorized(new { message = "No se pudo renovar el token. Credenciales inválidas." });

            return Ok(new
            {
                token = resultado.Token,
                expiresAt = resultado.ExpiresAt,
                message = "Token renovado exitosamente."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al renovar token: {ex.Message}" });
        }
    }

    public class RenovarTokenRequest
    {
        public int IdCliente { get; set; }
        public byte[] SecretKey { get; set; } = Array.Empty<byte>();
    }
    // DTO para la respuesta
    public class EmpresaDisponibleDto
    {
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsDefault { get; set; }
    }

    // DTOs para los nuevos endpoints
    public class CrearEmpresaRequest
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string NombreBD { get; set; } = string.Empty;
        public string ServidorBD { get; set; } = string.Empty;
        public string RncCedula { get; set; } = string.Empty;
        public byte Ambiente { get; set; } = 1; // 1 = Pruebas por defecto
        public string CadenaConexion { get; set; } = string.Empty; // ✅ NUEVO
    }

    public class EliminarEmpresaRequest
    {
        public string Motivo { get; set; } = string.Empty;
    }

    // DTO de respuesta
    public class EmpresaConexionResponseDto
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public string CadenaConexion { get; set; } = string.Empty;
    }
}