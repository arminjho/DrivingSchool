namespace eAutoskola.Infrastructure.Configuration;

public class PayPalOptions : EnvOptionsBase
{
    public string ClientId { get; }
    public string ClientSecret { get; }
    public string Mode { get; }

    /// <summary>PayPal does not support BAM, so orders are placed in this currency (default
    /// EUR); the KM -> EUR conversion happens in the Application layer.</summary>
    public string Currency { get; }

    public string ReturnUrl { get; }
    public string CancelUrl { get; }

    public string ApiBaseUrl => Mode.Equals("live", StringComparison.OrdinalIgnoreCase)
        ? "https://api-m.paypal.com"
        : "https://api-m.sandbox.paypal.com";

    public PayPalOptions()
    {
        ClientId = Require("PAYPAL_CLIENT_ID");
        ClientSecret = Require("PAYPAL_CLIENT_SECRET");
        Mode = GetOrDefault("PAYPAL_MODE", "sandbox");
        Currency = GetOrDefault("PAYPAL_CURRENCY", "EUR");
        ReturnUrl = Require("PAYPAL_RETURN_URL");
        CancelUrl = Require("PAYPAL_CANCEL_URL");
    }
}
