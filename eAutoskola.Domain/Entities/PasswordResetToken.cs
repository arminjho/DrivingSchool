namespace eAutoskola.Domain.Entities;

/// <summary>Reset code is generated with RandomNumberGenerator and stored hashed, never in
/// plain text (upute.md Appendix A.3).</summary>
public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public required string CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
