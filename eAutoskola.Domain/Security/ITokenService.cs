using eAutoskola.Domain.Entities;

namespace eAutoskola.Domain.Security;

public interface ITokenService
{
    /// <summary>Issues a signed JWT for the given user and returns it along with its jti and
    /// expiry (jti is needed by logout to revoke this specific token later).</summary>
    (string Token, string Jti, DateTime ExpiresAtUtc) IssueToken(User user);
}
