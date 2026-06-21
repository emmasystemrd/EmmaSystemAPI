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
    // ✅ Constructor actualizado con ICifradoService
    public AuthController(IAuthService authService, ICifradoService cifradoService, IConfiguration config, SqlConnectionFactory centralFactory)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _cifradoService = cifradoService ?? throw new ArgumentNullException(nameof(cifradoService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _centralFactory = centralFactory;

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
}