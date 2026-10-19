namespace eAutoskola.Domain.Security;

/// <summary>
/// Backs server-side logout (jti revocation) and the password-change-invalidates-everything
/// cutoff, checked on every request in JwtBearerEvents.OnTokenValidated (upute.md §5).
/// </summary>
public interface ITokenBlocklistService
{
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);
    Task RevokeAsync(int userId, string jti, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<DateTime?> GetTokensValidFromUtcAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Rejects every token issued before now for this user (after a password change or
    /// deactivation). It only STAGES the change on the shared DbContext - the caller's own
    /// SaveChangesAsync stores it in the same transaction as the change that caused it.</summary>
    Task StageTokenInvalidationAsync(int userId, CancellationToken cancellationToken);
}
