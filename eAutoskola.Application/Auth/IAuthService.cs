using eAutoskola.Application.Auth.Dto;
using eAutoskola.Application.Auth.Requests;

namespace eAutoskola.Application.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
