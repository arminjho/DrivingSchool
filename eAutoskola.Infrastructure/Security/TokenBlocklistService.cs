using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eAutoskola.Infrastructure.Security;

/// <summary>
/// RevokedToken is a real DB table (not purely in-memory), but every lookup is memoized via
/// IMemoryCache since JWT validation runs on every single request. Negative/positive TTLs are
/// asymmetric - a revoked token never un-revokes, so a "not revoked" result can be cached much
/// more briefly than a "revoked" result.
/// </summary>
public class TokenBlocklistService : ITokenBlocklistService
{
    private static readonly TimeSpan NegativeTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PositiveTtl = TimeSpan.FromMinutes(30);

    private readonly EAutoskolaDbContext _context;
    private readonly IMemoryCache _cache;

    public TokenBlocklistService(EAutoskolaDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken)
    {
        var cacheKey = $"jwt:revoked:{jti}";
        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        var isRevoked = await _context.RevokedTokens.AsNoTracking().AnyAsync(rt => rt.Jti == jti, cancellationToken);
        _cache.Set(cacheKey, isRevoked, isRevoked ? PositiveTtl : NegativeTtl);
        return isRevoked;
    }

    public async Task RevokeAsync(int userId, string jti, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var alreadyRevoked = await _context.RevokedTokens.AnyAsync(rt => rt.Jti == jti, cancellationToken);
        if (!alreadyRevoked)
        {
            _context.RevokedTokens.Add(new RevokedToken { UserId = userId, Jti = jti, ExpiresAtUtc = expiresAtUtc });
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Promote immediately so the revocation takes effect on the very next request.
        _cache.Set($"jwt:revoked:{jti}", true, PositiveTtl);
    }

    public async Task<DateTime?> GetTokensValidFromUtcAsync(int userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"jwt:validfrom:{userId}";
        if (_cache.TryGetValue(cacheKey, out DateTime? cached))
        {
            return cached;
        }

        var validFrom = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TokensValidFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        _cache.Set(cacheKey, validFrom, NegativeTtl);
        return validFrom;
    }

    public async Task StageTokenInvalidationAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        // Whole seconds, like the token's iat claim: a token issued in the same second as the
        // change (e.g. the login that follows a password reset) must stay valid.
        var now = DateTime.UtcNow;
        var validFromUtc = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        user.TokensValidFromUtc = validFromUtc;

        // Cache the new cutoff right away so the very next request already applies it, instead of
        // evicting the entry and letting a concurrent request re-cache the old value.
        _cache.Set($"jwt:validfrom:{userId}", (DateTime?)validFromUtc, NegativeTtl);
    }
}
