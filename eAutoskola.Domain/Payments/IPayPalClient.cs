namespace eAutoskola.Domain.Payments;

/// <summary>
/// PayPal Orders v2 / Payments v2 REST calls. Lives in Domain (implemented in Infrastructure)
/// for the same dependency-direction reason as ICurrentUserService. All amounts here are in
/// <see cref="Currency"/> - KM never crosses this boundary.
/// </summary>
public interface IPayPalClient
{
    string Currency { get; }

    Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string description, CancellationToken cancellationToken);

    /// <summary>Server-side verification of the buyer's approval - the only way a payment is
    /// ever finalized. Success is false when PayPal has not completed the capture (e.g. the
    /// buyer has not approved yet).</summary>
    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken);

    Task<PayPalRefundResult> RefundCaptureAsync(string captureId, decimal amount, string currency, CancellationToken cancellationToken);
}

public record PayPalOrderResult(string OrderId, string ApprovalUrl);

public record PayPalCaptureResult(bool Success, string? CaptureId, decimal CapturedAmount, string? Currency, string? FailureReason);

public record PayPalRefundResult(bool Success, string? RefundId, string? FailureReason);
