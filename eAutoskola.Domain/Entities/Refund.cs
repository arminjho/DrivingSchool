using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

/// <summary>
/// Povrat. Failed attempts are recorded too (Status = Failed, PayPalRefundId null) so there is
/// an audit trail when PayPal rejects a refund and the lesson stays un-cancelled.
/// </summary>
public class Refund
{
    public int Id { get; set; }

    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }

    /// <summary>In Payment.PayPalCurrency - equal to the payment's CapturedAmount.</summary>
    public decimal Amount { get; set; }

    /// <summary>KM, copied from Payment.Amount - what the finance screen displays.</summary>
    public decimal AmountKm { get; set; }

    public string? PayPalRefundId { get; set; }

    public RefundStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
