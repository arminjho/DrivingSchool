using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

/// <summary>
/// Uplata - one PayPal order for one installment. Amounts are kept in KM for everything the
/// user sees; PayPal does not support BAM, so the order itself is placed in EUR
/// (PayPalAmount/PayPalCurrency) and refunds use CapturedAmount - what PayPal reports it
/// actually captured.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int InstallmentId { get; set; }
    public Installment? Installment { get; set; }

    /// <summary>KM - equal to the installment amount.</summary>
    public decimal Amount { get; set; }

    public decimal PayPalAmount { get; set; }
    public required string PayPalCurrency { get; set; }

    /// <summary>Set from PayPal's capture response; null until captured.</summary>
    public decimal? CapturedAmount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    public required string PayPalOrderId { get; set; }
    public required string PayPalApprovalUrl { get; set; }
    public string? PayPalCaptureId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>Optimistic concurrency - two simultaneous capture calls for the same order
    /// can't both apply the first-capture side effects.</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<Refund> Refunds { get; set; } = [];
}
