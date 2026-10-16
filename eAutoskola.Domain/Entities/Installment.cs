using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

/// <summary>Rata - the unit a student actually pays through PayPal.</summary>
public class Installment
{
    public int Id { get; set; }

    public int ChargeId { get; set; }
    public Charge? Charge { get; set; }

    /// <summary>1-based position within the charge ("Rata 2").</summary>
    public int Number { get; set; }

    /// <summary>KM.</summary>
    public decimal Amount { get; set; }

    public DateTime DueDate { get; set; }

    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payment> Payments { get; set; } = [];
}
