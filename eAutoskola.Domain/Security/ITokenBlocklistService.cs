namespace eAutoskola.Domain.Security;

/// <summary>
/// Backs server-side logout (jti revocation) and the password-change-invalidates-everything
/// cutoff, checked on every request in JwtBearerEvents.OnTokenValidated (upute.md §5).
/// </summary>
public interface ITokenBlocklistService
{
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);
    Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<DateTime?> GetTokensValidFromUtcAsync(int userId, CancellationToken cancellationToken);
    Task InvalidateTokensValidFromAsync(int userId, CancellationToken cancellationToken);
}
