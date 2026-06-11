using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Exceptions;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text; // 👈 ESTE ES EL QUE SOLUCIONA EL ERROR DE BinaryReader

namespace EmmaSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AuthRepository _authRepo;
    private readonly IConfiguration _config;

    public AuthService(AuthRepository authRepo, IConfiguration config)
    {
        _authRepo = authRepo;
        _config = config;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct)
    {
        // 1. Validar credenciales contra la BD (vía SP)
        var user = await _authRepo.LoginAsync(request.Usuario, request.Clave, request.Idempresa, ct);

        if (user is null)
            throw new EmmaSystemException("Usuario o contraseña inválidos.", 401, "AUTH_INVALID");

        if (user.Estado != "A")
            throw new EmmaSystemException("Usuario inactivo o bloqueado.", 403, "AUTH_INACTIVE");

        // 2. Obtener roles
        var roles = await _authRepo.GetRolesByAccesoAsync(user.Idacceso, ct);

        // 3. Generar Token
        var tokenResult = BuildJwt(user, roles, request.Idempresa);

        // 4. Retornar DTO
        return new LoginResponseDto
        {
            Token = tokenResult.Token,
            Expira = tokenResult.Expira,
            Idusuario = user.Idusuario,
            Idempresa = request.Idempresa, // Usamos el del request ya que el SP no lo devuelve explícitamente
            Idacceso = user.Idacceso,
            NombreEmpleado = user.Nombres,
            Puesto = user.NombrePuesto, // Mapeado desde la columna "Nombre" del SP
            Empresa = user.Empresa,
            Foto=user.Foto,
            Idroles = roles
        };
    }

    private (string Token, DateTime Expira) BuildJwt(SpLoginRow user, IReadOnlyList<int> roles, int idempresa)
    {
        var jwtSection = _config.GetSection("Jwt");
        var keyString = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.json");

        // Conversión correcta de string a byte[] (Aquí estaba el error del BinaryReader)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpireMinutes"] ?? "60"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Idusuario.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("Idusuario", user.Idusuario.ToString()),
            new("Idempresa", idempresa.ToString()),
            new("Idacceso",  user.Idacceso.ToString()),
            new("Idempleado", user.Idempleado.ToString()),
            new(ClaimTypes.Name, user.Idusuario.ToString()),
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
}