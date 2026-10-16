namespace eAutoskola.Domain.Entities;

/// <summary>Logout invalidation: jti is checked in JwtBearerEvents.OnTokenValidated
/// (upute.md §5).</summary>
public class RevokedToken
{
    public int Id { get; set; }
    public required string Jti { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime RevokedAtUtc { get; set; } = DateTime.UtcNow;
}
