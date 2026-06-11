using EmmaSystem.Application.DTOs.Auth;

namespace EmmaSystem.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
}