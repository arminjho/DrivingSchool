namespace eAutoskola.Contracts.Messaging;

/// <summary>Published once, on the first successful capture of an installment payment.</summary>
public class PaymentReceivedMessage
{
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }

    /// <summary>KM - what the student paid (the PayPal EUR amount is never shown to users).</summary>
    public decimal Amount { get; set; }
}
