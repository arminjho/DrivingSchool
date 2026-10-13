namespace eAutoskola.Infrastructure.Configuration;

public class PayPalOptions : EnvOptionsBase
{
    public string ClientId { get; }
    public string ClientSecret { get; }
    public string Mode { get; }
    public string WebhookId { get; }
    public string ReturnUrl { get; }
    public string CancelUrl { get; }

    public PayPalOptions()
    {
        ClientId = Require("PAYPAL_CLIENT_ID");
        ClientSecret = Require("PAYPAL_CLIENT_SECRET");
        Mode = GetOrDefault("PAYPAL_MODE", "sandbox");
        WebhookId = Require("PAYPAL_WEBHOOK_ID");
        ReturnUrl = Require("PAYPAL_RETURN_URL");
        CancelUrl = Require("PAYPAL_CANCEL_URL");
    }
}
