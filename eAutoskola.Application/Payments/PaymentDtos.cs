namespace eAutoskola.Application.Payments;

public class CreateOrderResultDto
{
    public int PaymentId { get; set; }
    public required string OrderId { get; set; }

    /// <summary>The URL the client opens (in-app WebView) so the buyer approves the payment.</summary>
    public required string ApprovalUrl { get; set; }

    /// <summary>KM - what the installment costs.</summary>
    public decimal Amount { get; set; }

    public decimal PayPalAmount { get; set; }
    public required string PayPalCurrency { get; set; }
}

public class CapturePaymentResultDto
{
    public int PaymentId { get; set; }
    public int InstallmentId { get; set; }
    public required string Status { get; set; }
    public decimal Amount { get; set; }

    /// <summary>True when this call only returned an already-finalized payment (a repeat
    /// capture), so no side effects ran again.</summary>
    public bool AlreadyProcessed { get; set; }
}
