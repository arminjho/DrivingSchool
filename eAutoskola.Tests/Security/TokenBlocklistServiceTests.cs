using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eAutoskola.Tests.Security;

/// <summary>Logout and "password changed" invalidation. The invalidation only stages the change,
/// so the caller stores it together with the password change in one save.</summary>
public class TokenBlocklistServiceTests
{
    private static (TokenBlocklistService Service, EAutoskolaDbContext Context, Func<int> Saves) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.Add(new User { Id = 1, Email = "a@test.ba", PasswordHash = "x", Role = Role.Student, FirstName = "A", LastName = "B", PhoneNumber = "+387 61 000 000" });
        context.SaveChanges();

        var saves = 0;
        context.SavedChanges += (_, _) => saves++;
        return (new TokenBlocklistService(context, new MemoryCache(new MemoryCacheOptions())), context, () => saves);
    }

    [Fact]
    public async Task StageTokenInvalidation_SetsTheCutoffWithoutSaving_AndAppliesItImmediately()
    {
        var (service, context, saves) = Create();

        await service.StageTokenInvalidationAsync(1, default);

        Assert.Equal(0, saves());
        var staged = (await context.Users.FindAsync(1))!.TokensValidFromUtc;
        Assert.NotNull(staged);
        Assert.Equal(0, staged.Value.Ticks % TimeSpan.TicksPerSecond);
        Assert.Equal(staged, await service.GetTokensValidFromUtcAsync(1, default));

        await context.SaveChangesAsync();
        Assert.Equal(staged, (await context.Users.AsNoTracking().SingleAsync()).TokensValidFromUtc);
    }

    [Fact]
    public async Task StageTokenInvalidation_ForAnUnknownUser_DoesNothing()
    {
        var (service, _, saves) = Create();

        await service.StageTokenInvalidationAsync(42, default);

        Assert.Equal(0, saves());
    }

    [Fact]
    public async Task Revoke_MakesTheTokenRevokedAtOnce_AndIsIdempotent()
    {
        var (service, context, _) = Create();

        Assert.False(await service.IsRevokedAsync("jti-1", default));
        await service.RevokeAsync(1, "jti-1", DateTime.UtcNow.AddHours(1), default);
        await service.RevokeAsync(1, "jti-1", DateTime.UtcNow.AddHours(1), default);

        Assert.True(await service.IsRevokedAsync("jti-1", default));
        Assert.Equal(1, await context.RevokedTokens.CountAsync());
        Assert.Equal(1, (await context.RevokedTokens.SingleAsync()).UserId);
    }
}
