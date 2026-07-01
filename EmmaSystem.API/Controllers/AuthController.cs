using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Dapper;
using EmmaSystem.Infrastructure.Data;
using System.Linq;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICifradoService _cifradoService;
    private readonly IConfiguration _config;
    private readonly SqlConnectionFactory _centralFactory;
    private readonly ISesionService _sesionService;

    public AuthController(IAuthService authService, ICifradoService cifradoService,
       IConfiguration config, SqlConnectionFactory centralFactory,
       ISesionService sesionService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _cifradoService = cifradoService ?? throw new ArgumentNullException(nameof(cifradoService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _centralFactory = centralFactory ?? throw new ArgumentNullException(nameof(centralFactory));
        _sesionService = sesionService ?? throw new ArgumentNullException(nameof(sesionService));
    }

    /// <summary>
    /// Login inicial en EmmaSystemCentral.
    /// Valida credenciales del cliente, evalúa concurrencia atómicamente y retorna datos de acceso.
    /// </summary>
    [HttpPost("login/central")]
    public async Task<ActionResult<LoginCentralResponseDto>> LoginCentral(
      [FromBody] LoginCentralRequestDto request,
      CancellationToken cancellationToken)
    {
        try
        {
            // Capturar IP real del cliente
            request.IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 1. Validar las credenciales básicas del usuario central
            var response = await _authService.LoginCentralAsync(request, cancellationToken);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                // 2. Extraer el IdUsuarioCentral desde los claims del Token JWT generado
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(response.Token);
                var idUsuarioCentralClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "IdUsuarioCentral" || c.Type == "sub")?.Value;
                int idUsuarioCentral = int.TryParse(idUsuarioCentralClaim, out var idUser) ? idUser : 0;

                // 3. Capturar el User-Agent actual del request
                string userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

                // 4. Bloqueo de concurrencia y registro Atómico bajo sp_getapplock
                var (puedeIniciar, mensaje) = await _sesionService.ValidarYRegistrarSesionAtomicaAsync(
                    idUsuarioCentral,
                    response.IdCliente,
                    null, // Es login central global, no hay empresa elegida todavía
                    response.Token,
                    request.IPAddress,
                    userAgent,
                    request.DeviceId,
                    request.NombreEquipo,
                    0, // Se calcula internamente en caliente en la transacción del repositorio
                    cancellationToken);

                if (!puedeIniciar)
                {
                    // Si excede las sesiones o falla el candado, denegamos el acceso inmediatamente
                    return StatusCode(403, new { message = mensaje });
                }
            }

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

    public class GenerarConexionRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string SaltHex { get; set; } = string.Empty;
        public string ServidorBD { get; set; } = string.Empty;
        public string NombreBD { get; set; } = string.Empty;
    }

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

    [HttpGet("debug/roundtrip/{idEmpresa}")]
    [Authorize]
    public async Task<IActionResult> RoundTrip(int idEmpresa)
    {
        using var conn = _centralFactory.CreateConnection();

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

    [HttpGet("empresa/{id:int}/conexion")]
    [Authorize]
    public async Task<ActionResult<EmpresaConexionResponseDto>> GetConexionEmpresa(int id, CancellationToken ct)
    {
        try
        {
            var idUsuarioCentralClaim = User.FindFirst("IdUsuarioCentral")?.Value;
            var idClienteClaim = User.FindFirst("ClienteId")?.Value;

            if (string.IsNullOrEmpty(idUsuarioCentralClaim) || string.IsNullOrEmpty(idClienteClaim))
                return Unauthorized(new { message = "Token inválido." });

            var idUsuarioCentral = int.Parse(idUsuarioCentralClaim);
            var idCliente = int.Parse(idClienteClaim);

            var esValida = await _authService.ValidarEmpresaDeClienteAsync(idCliente, id, ct);
            if (!esValida)
                return Forbid();

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

    [HttpPost("empresas/crear")]
    [Authorize]
    public async Task<IActionResult> CrearEmpresa([FromBody] CrearEmpresaRequest request, CancellationToken ct)
    {
        try
        {
            var idCliente = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            var idUsuarioCentral = int.Parse(User.FindFirst("IdUsuarioCentral")?.Value ?? "0");

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

            const string getSaltSql = "SELECT SaltCifrado FROM clientes WHERE IdCliente = @IdCliente";
            var saltBytes = await Dapper.SqlMapper.ExecuteScalarAsync<byte[]>(conn, getSaltSql, new { IdCliente = idCliente });

            if (saltBytes == null || saltBytes.Length == 0)
            {
                return BadRequest(new { message = "El cliente no tiene salt de cifrado configurado." });
            }

            byte[] cadenaCifrada = null;
            byte[] vectorIV = null;

            if (!string.IsNullOrEmpty(request.CadenaConexion))
            {
                cadenaCifrada = _cifradoService.Cifrar(request.CadenaConexion, saltBytes, out vectorIV);
            }

            const string checkSql = @"
            SELECT COUNT(1) FROM empresas_contratadas 
            WHERE IdCliente = @IdCliente AND NombreBD = @NombreBD AND Estado = 1";
            var existe = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn, checkSql,
                new { IdCliente = idCliente, NombreBD = request.NombreBD });

            if (existe > 0)
            {
                return BadRequest(new { message = $"Ya existe una empresa registrada con la base de datos '{request.NombreBD}'." });
            }

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

    [HttpDelete("empresas/{id:int}")]
    [Authorize]
    public async Task<IActionResult> EliminarEmpresa(int id, [FromBody] EliminarEmpresaRequest request, CancellationToken ct)
    {
        try
        {
            var idCliente = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            var idUsuarioCentral = int.Parse(User.FindFirst("IdUsuarioCentral")?.Value ?? "0");

            using var conn = _centralFactory.CreateConnection();

            const string getSql = @"
            SELECT NombreEmpresa, NombreBD, ServidorBD 
            FROM empresas_contratadas 
            WHERE IdEmpresa = @Id AND IdCliente = @IdCliente AND Estado = 1";

            var empresa = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<dynamic>(conn, getSql,
                new { Id = id, IdCliente = idCliente });

            if (empresa is null)
                return NotFound(new { message = "Empresa no encontrada o no le pertenece." });

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

            const string updateSql = @"
            UPDATE empresas_contratadas 
            SET Estado = 0 
            WHERE IdEmpresa = @Id AND IdCliente = @IdCliente";

            await Dapper.SqlMapper.ExecuteAsync(conn, updateSql, new { Id = id, IdCliente = idCliente });

            return Ok(new { message = "Empresa正式mente eliminada correctamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al eliminar empresa: {ex.Message}" });
        }
    }

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

    [HttpPost("renovar-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RenovarToken([FromBody] RenovarTokenRequest request, CancellationToken ct)
    {
        try
        {
            if (request.IdCliente <= 0 || request.SecretKey == null || request.SecretKey.Length == 0)
                return BadRequest(new { message = "Datos de renovación inválidos." });

            // CORREGIDO: Validación del identificador de hardware
            if (string.IsNullOrEmpty(request.DeviceId))
                return BadRequest(new { message = "El identificador del dispositivo es requerido." });

            bool dispositivoValido = await _sesionService.ExisteSesionDispositivoAsync(request.IdCliente, request.DeviceId, ct);
            if (!dispositivoValido)
            {
                return StatusCode(403, new { message = "Acceso denegado. El dispositivo no coincide o la sesión expiró." });
            }

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
        public string DeviceId { get; set; } = string.Empty; // ← CORREGIDO: Se añade esta propiedad
    }

    public class EmpresaDisponibleDto
    {
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsDefault { get; set; }
    }

    public class CrearEmpresaRequest
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string NombreBD { get; set; } = string.Empty;
        public string ServidorBD { get; set; } = string.Empty;
        public string RncCedula { get; set; } = string.Empty;
        public byte Ambiente { get; set; } = 1;
        public string CadenaConexion { get; set; } = string.Empty;
    }

    public class EliminarEmpresaRequest
    {
        public string Motivo { get; set; } = string.Empty;
    }

    public class EmpresaConexionResponseDto
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public string CadenaConexion { get; set; } = string.Empty;
    }
}