namespace eAutoskola.Infrastructure.Configuration;

public class PayPalOptions : EnvOptionsBase
{
    public string ClientId { get; }
    public string ClientSecret { get; }

    /// <summary>PayPal REST API root (sandbox or live) - from PAYPAL_BASE_URL, never a literal.</summary>
    public string ApiBaseUrl { get; }

    /// <summary>PayPal does not support BAM, so orders are placed in this currency (default
    /// EUR); the KM -> EUR conversion happens in the Application layer.</summary>
    public string Currency { get; }

    public string ReturnUrl { get; }
    public string CancelUrl { get; }

    public PayPalOptions()
    {
        ClientId = Require("PAYPAL_CLIENT_ID");
        ClientSecret = Require("PAYPAL_CLIENT_SECRET");
        ApiBaseUrl = Require("PAYPAL_BASE_URL").TrimEnd('/');
        Currency = GetOrDefault("PAYPAL_CURRENCY", "EUR");
        ReturnUrl = Require("PAYPAL_RETURN_URL");
        CancelUrl = Require("PAYPAL_CANCEL_URL");
    }
}
