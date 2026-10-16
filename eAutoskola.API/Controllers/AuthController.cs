using System.IdentityModel.Tokens.Jwt;
using eAutoskola.Application.Auth;
using eAutoskola.Application.Auth.Dto;
using eAutoskola.Application.Auth.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

/// <summary>
/// [AllowAnonymous] only on register/login/forgot-password/reset-password - every other
/// action requires a valid token (upute.md §5). Login credentials are read from the POST body
/// only, never query string.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, cancellationToken));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

        if (jti is not null && long.TryParse(expClaim, out var expUnixSeconds))
        {
            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime;
            await _authService.LogoutAsync(jti, expiresAtUtc, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
