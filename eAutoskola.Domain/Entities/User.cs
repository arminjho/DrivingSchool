namespace eAutoskola.Domain.Entities;

/// <summary>
/// Role is a plain string column (one of eAutoskola.Domain.Security.Role's constants), not a
/// foreign key to a reference table - matches ASP.NET Core's native string-based role claims
/// and the reference project's pattern (see docs/REFERENCE_ANALYSIS.md).
/// </summary>
public class User : ISoftDelete
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Address { get; set; }

    public int? CityId { get; set; }
    public City? City { get; set; }

    public string? ProfileImagePath { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Password-change cutoff: tokens issued before this are rejected even if unexpired.</summary>
    public DateTime? TokensValidFromUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public StudentProfile? StudentProfile { get; set; }
    public InstructorProfile? InstructorProfile { get; set; }
}
