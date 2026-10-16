namespace eAutoskola.Contracts.Messaging;

/// <summary>Published after a paid lesson is cancelled and PayPal has accepted the refund.</summary>
public class RefundIssuedMessage
{
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }

    /// <summary>KM - the refunded installment's amount.</summary>
    public decimal Amount { get; set; }
}
