using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Exceptions;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace EmmaSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AuthRepository _authRepo;
    private readonly IConfiguration _config;
    private readonly SesionRepository _sesionRepo;
    private readonly ISesionService _sesionService;

    public AuthService(AuthRepository authRepo, IConfiguration config,
        SesionRepository sesionRepo, ISesionService sesionService)
    {
        _authRepo = authRepo ?? throw new ArgumentNullException(nameof(authRepo));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sesionRepo = sesionRepo ?? throw new ArgumentNullException(nameof(sesionRepo));
        _sesionService = sesionService ?? throw new ArgumentNullException(nameof(sesionService));
    }

    // ──────────────────────────────────────────────
    // 1. LOGIN CENTRAL (EmmaSystemCentral)
    // ──────────────────────────────────────────────
    public async Task<LoginCentralResponseDto> LoginCentralAsync(LoginCentralRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new EmmaSystemException("Email y contraseña son requeridos.", 400, "AUTH_MISSING_CREDENTIALS");

        // 1. Validar credenciales contra usuarios_central
        var usuarioCentral = await _authRepo.LoginCentralAsync(request.Email, ct);

        if (usuarioCentral is null)
            throw new EmmaSystemException("Credenciales inválidas.", 401, "AUTH_INVALID");

        if (usuarioCentral.Estado != 1)
            throw new EmmaSystemException("Usuario inactivo o bloqueado.", 403, "AUTH_INACTIVE");

        // 2. Verificar password (BCrypt)
        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuarioCentral.PasswordHash))
            throw new EmmaSystemException("Credenciales inválidas.", 401, "AUTH_INVALID");

        // 3. ✅ EXCEPCIÓN DEL MISMO DISPOSITIVO (Evita el efecto candado tras un crash o apagón)
        var deviceId = string.IsNullOrEmpty(request.DeviceId) ? "unknown" : request.DeviceId;
        if (deviceId != "unknown")
        {
            await _sesionService.CerrarSesionDispositivoAsync(usuarioCentral.IdCliente, deviceId, ct);
        }

        // 4. Obtener empresas disponibles del cliente
        var empresas = await _authRepo.GetEmpresasByClienteAsync(usuarioCentral.IdCliente, ct);

        if (empresas.Count == 0)
            throw new EmmaSystemException("No tiene empresas activas asignadas.", 403, "AUTH_NO_EMPRESAS");

        // 5. Obtener el límite de concurrencia permitido por el plan de este cliente
        var maxConcurrentes = await _sesionRepo.GetMaxConcurrentesAsync(usuarioCentral.IdCliente, ct);

        // 6. Generar token central de forma anticipada para poder insertarlo atómicamente si pasa el filtro
        var (token, expira) = BuildJwtCentral(usuarioCentral);

        // 7. ✅ NUEVO FLUJO ATÓMICO: Valida sesiones simultáneas e inserta la nueva sesión bajo un único bloqueo exclusivo
        var ipAddress = string.IsNullOrEmpty(request.IPAddress) ? "unknown" : request.IPAddress;
        var userAgent = "EmmaSystem Desktop";
        var nombreEquipo = string.IsNullOrEmpty(request.NombreEquipo) ? "unknown" : request.NombreEquipo;

        var (puedeIniciar, mensajeSesion) = await _sesionService.ValidarYRegistrarSesionAtomicaAsync(
            usuarioCentral.IdUsuarioCentral,
            usuarioCentral.IdCliente,
            null, // Aún no selecciona empresa
            token,
            ipAddress,
            userAgent,
            deviceId,
            nombreEquipo,
            maxConcurrentes,
            ct);

        if (!puedeIniciar)
        {
            throw new EmmaSystemException(mensajeSesion, 403, "AUTH_MAX_CONCURRENTES");
        }

        // 8. Actualizar último acceso del usuario
        await _authRepo.UpdateUltimoAccesoCentralAsync(usuarioCentral.IdUsuarioCentral, ct);

        // 9. Generar clave secreta para validación offline
        var secretKey = new byte[256];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secretKey);
        }

        // 10. Guardar clave secreta en la base de datos
        await _authRepo.UpdateSecretKeyAsync(usuarioCentral.IdCliente, secretKey, ct);

        // 11. Retornar respuesta
        return new LoginCentralResponseDto
        {
            Token = token,
            IdCliente = usuarioCentral.IdCliente,
            SecretKey = secretKey,
            NombreCliente = usuarioCentral.NombreCompleto,
            Empresas = empresas.ToList(),
            ExpiresAt = expira,
            AutoSeleccionar = empresas.Count == 1
        };
    }

    // ──────────────────────────────────────────────
    // 2. SELECCIONAR EMPRESA (Genera token con tenant)
    // ──────────────────────────────────────────────
    public async Task<SeleccionEmpresaResponseDto> SeleccionarEmpresaAsync(int idUsuarioCentral, int idEmpresa, CancellationToken ct)
    {
        // 1. Obtener datos del usuario central para validar propiedad
        var usuarioCentral = await _authRepo.GetUsuarioCentralByIdAsync(idUsuarioCentral, ct);

        if (usuarioCentral is null)
            throw new EmmaSystemException("Usuario central no encontrado.", 401, "AUTH_INVALID");

        // 2. Verificar que la empresa pertenece al cliente
        var esValida = await _authRepo.ValidarEmpresaDeClienteAsync(usuarioCentral.IdCliente, idEmpresa, ct);

        if (!esValida)
            throw new EmmaSystemException("La empresa seleccionada no está disponible o no le pertenece.", 403, "AUTH_EMPRESA_INVALIDA");

        // 3. Obtener info completa de la empresa (Nombre, RNC, Ambiente)
        var infoEmpresa = await _authRepo.GetInfoEmpresaAsync(idEmpresa, ct);

        // 4. Generar token con contexto completo de tenant + nuevos claims
        var (token, expira) = BuildJwtTenant(
            usuarioCentral,
            idEmpresa,
            infoEmpresa.NombreEmpresa,
            infoEmpresa.RncCedula,
            infoEmpresa.Ambiente);

        return new SeleccionEmpresaResponseDto
        {
            Token = token,
            IdEmpresa = idEmpresa,
            NombreEmpresa = infoEmpresa.NombreEmpresa,
            ClienteId = usuarioCentral.IdCliente,
            ExpiresAt = expira
        };
    }

    // ──────────────────────────────────────────────
    // 3. LOGIN DIRECTO A EMPRESA (Usuario operativo)
    // ──────────────────────────────────────────────
    public async Task<SeleccionEmpresaResponseDto> LoginEmpresaAsync(LoginEmpresaRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Clave))
            throw new EmmaSystemException("Usuario y contraseña son requeridos.", 400, "AUTH_MISSING_CREDENTIALS");

        // 1. Validar credenciales contra la BD de la empresa específica
        var user = await _authRepo.LoginEmpresaAsync(request.Usuario, request.Clave, request.Idempresa, ct);

        if (user is null)
            throw new EmmaSystemException("Usuario o contraseña inválidos.", 401, "AUTH_INVALID");

        if (user.Estado != "A")
            throw new EmmaSystemException("Usuario inactivo o bloqueado.", 403, "AUTH_INACTIVE");

        // 2. Obtener roles del usuario en la empresa
        var roles = await _authRepo.GetRolesByAccesoAsync(user.Idacceso, request.Idempresa, ct);
        var clienteId = await _authRepo.GetClienteIdByEmpresaAsync(request.Idempresa, ct);

        // 3. Generar token con contexto de tenant + datos operativos
        var (token, expira) = await BuildJwtEmpresa(user, roles, request.Idempresa, ct);

        return new SeleccionEmpresaResponseDto
        {
            Token = token,
            IdEmpresa = request.Idempresa,
            NombreEmpresa = user.Empresa,
            ClienteId = clienteId,
            ExpiresAt = expira
        };
    }

    // ──────────────────────────────────────────────
    // GENERADORES DE JWT
    // ──────────────────────────────────────────────

    private (string Token, DateTime Expira) BuildJwtCentral(UsuarioCentralRow usuario)
    {
        var jwtSection = _config.GetSection("Jwt");
        var keyString = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.json");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpireMinutes"] ?? "60"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuarioCentral.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("IdUsuarioCentral", usuario.IdUsuarioCentral.ToString()),
            new("ClienteId", usuario.IdCliente.ToString()),
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.NameIdentifier, usuario.IdUsuarioCentral.ToString())
        };

        if (usuario.EsSuperAdmin)
            claims.Add(new Claim("EsSuperAdmin", "true"));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    private (string Token, DateTime Expira) BuildJwtTenant(
        UsuarioCentralRow usuario,
        int idEmpresa,
        string nombreEmpresa,
        string rncCedula,
        byte ambiente)
    {
        var jwtSection = _config.GetSection("Jwt");
        var keyString = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.json");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpireMinutes"] ?? "480"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuarioCentral.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("IdUsuarioCentral", usuario.IdUsuarioCentral.ToString()),
            new("ClienteId", usuario.IdCliente.ToString()),
            new("Idempresa", idEmpresa.ToString()),
            new("NombreEmpresa", nombreEmpresa),
            new("RncCedula", rncCedula ?? ""),
            new("Ambiente", ambiente.ToString()),
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.NameIdentifier, usuario.IdUsuarioCentral.ToString())
        };

        if (usuario.EsSuperAdmin)
            claims.Add(new Claim("EsSuperAdmin", "true"));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    private async Task<(string Token, DateTime Expira)> BuildJwtEmpresa(SpLoginRow user, IReadOnlyList<int> roles, int idEmpresa, CancellationToken ct)
    {
        var jwtSection = _config.GetSection("Jwt");
        var keyString = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.json");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpireMinutes"] ?? "480"));

        var clienteId = await _authRepo.GetClienteIdByEmpresaAsync(idEmpresa, ct);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Idusuario.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("Idusuario", user.Idusuario.ToString()),
            new("Idempresa", idEmpresa.ToString()),
            new("ClienteId", clienteId.ToString()),
            new("Idacceso", user.Idacceso.ToString()),
            new("Idempleado", user.Idempleado.ToString()),
            new(ClaimTypes.Name, user.NombreUsuario),
            new(ClaimTypes.NameIdentifier, user.Idusuario.ToString())
        };

        foreach (var idrol in roles)
            claims.Add(new Claim("Idrol", idrol.ToString()));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public async Task<bool> ValidarEmpresaDeClienteAsync(int idCliente, int idEmpresa, CancellationToken ct)
    {
        return await _authRepo.ValidarEmpresaDeClienteAsync(idCliente, idEmpresa, ct);
    }

    public async Task<TokenRenovadoDto?> RenovarTokenAsync(int idCliente, byte[] secretKey, CancellationToken ct)
    {
        var storedKey = await _authRepo.GetSecretKeyAsync(idCliente, ct);

        if (storedKey == null || storedKey.Length != secretKey.Length)
            return null;

        bool keyMatch = true;
        for (int i = 0; i < storedKey.Length; i++)
        {
            if (storedKey[i] != secretKey[i])
            {
                keyMatch = false;
                break;
            }
        }

        if (!keyMatch)
            return null;

        var usuarioCentral = await _authRepo.GetUsuarioCentralByClienteIdAsync(idCliente, ct);
        if (usuarioCentral == null || usuarioCentral.Estado != 1)
            return null;

        var (token, expira) = BuildJwtCentral(usuarioCentral);

        return new TokenRenovadoDto
        {
            Token = token,
            ExpiresAt = expira
        };
    }
}