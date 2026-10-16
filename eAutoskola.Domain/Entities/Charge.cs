using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

/// <summary>
/// Zaduženje. Either a package item (LessonId null - Teorijska obuka / Praktična vožnja /
/// Administracija i ispit, created once per student) or an extra driving lesson beyond the
/// package quota (LessonId set, created when that lesson is confirmed). Never hard-deleted.
/// </summary>
public class Charge
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int ServiceTypeId { get; set; }
    public ServiceType? ServiceType { get; set; }

    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    /// <summary>KM, snapshotted from ServiceType.Price at creation so later price edits
    /// don't change what the student owes.</summary>
    public decimal Amount { get; set; }

    public required string Description { get; set; }

    public ChargeStatus Status { get; set; } = ChargeStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Installment> Installments { get; set; } = [];
}
