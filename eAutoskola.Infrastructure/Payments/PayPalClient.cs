using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using eAutoskola.Domain.Payments;
using eAutoskola.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace eAutoskola.Infrastructure.Payments;

/// <summary>
/// PayPal Orders v2 / Payments v2 over IHttpClientFactory. The OAuth2 client-credentials token
/// is cached until shortly before it expires. Every non-success response is surfaced as a
/// failed result (never thrown raw) so callers decide how to map it to a Bosnian message.
/// </summary>
public class PayPalClient : IPayPalClient
{
    private const string TokenCacheKey = "paypal-access-token";
    private const int MaxDescriptionLength = 127; // PayPal's purchase_unit description limit

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly PayPalOptions _options;
    private readonly ILogger<PayPalClient> _logger;

    public PayPalClient(IHttpClientFactory httpClientFactory, IMemoryCache cache, PayPalOptions options, ILogger<PayPalClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public string Currency => _options.Currency;

    public async Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string description, CancellationToken cancellationToken)
    {
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    description = description.Length > MaxDescriptionLength ? description[..MaxDescriptionLength] : description,
                    amount = new { currency_code = _options.Currency, value = FormatAmount(amount) },
                },
            },
            application_context = new
            {
                return_url = _options.ReturnUrl,
                cancel_url = _options.CancelUrl,
                user_action = "PAY_NOW",
            },
        };

        using var response = await SendAsync(HttpMethod.Post, "/v2/checkout/orders", body, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal create-order failed with status {Status}: {Body}", (int)response.StatusCode, responseText);
            throw new HttpRequestException($"PayPal create-order failed with status {(int)response.StatusCode}.");
        }

        using var json = JsonDocument.Parse(responseText);
        var orderId = json.RootElement.GetProperty("id").GetString()!;
        var approvalUrl = json.RootElement.GetProperty("links").EnumerateArray()
            .Where(l => l.GetProperty("rel").GetString() is "approve" or "payer-action")
            .Select(l => l.GetProperty("href").GetString())
            .FirstOrDefault()
            ?? throw new HttpRequestException("PayPal create-order response did not contain an approval link.");

        return new PayPalOrderResult(orderId, approvalUrl);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture", new { }, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The order was already captured (e.g. our database write failed after PayPal
            // succeeded) - read the existing capture back instead of reporting a failure.
            if ((int)response.StatusCode == 422 && responseText.Contains("ORDER_ALREADY_CAPTURED", StringComparison.Ordinal))
            {
                return await GetCapturedOrderAsync(orderId, cancellationToken);
            }

            _logger.LogWarning("PayPal capture for order {OrderId} failed with status {Status}: {Body}", orderId, (int)response.StatusCode, responseText);
            return new PayPalCaptureResult(false, null, 0, null, $"HTTP {(int)response.StatusCode}");
        }

        return ParseCapturedOrder(responseText);
    }

    private async Task<PayPalCaptureResult> GetCapturedOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.ApiBaseUrl + $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayPal get-order for {OrderId} failed with status {Status}: {Body}", orderId, (int)response.StatusCode, responseText);
            return new PayPalCaptureResult(false, null, 0, null, $"HTTP {(int)response.StatusCode}");
        }

        return ParseCapturedOrder(responseText);
    }

    private static PayPalCaptureResult ParseCapturedOrder(string responseText)
    {
        using var json = JsonDocument.Parse(responseText);
        var status = json.RootElement.GetProperty("status").GetString();
        if (!string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            return new PayPalCaptureResult(false, null, 0, null, $"Order status {status}");
        }

        var capture = json.RootElement.GetProperty("purchase_units")[0]
            .GetProperty("payments").GetProperty("captures")[0];
        var captureStatus = capture.GetProperty("status").GetString();
        if (!string.Equals(captureStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            return new PayPalCaptureResult(false, null, 0, null, $"Capture status {captureStatus}");
        }

        var captureId = capture.GetProperty("id").GetString()!;
        var amount = capture.GetProperty("amount");
        var value = decimal.Parse(amount.GetProperty("value").GetString()!, CultureInfo.InvariantCulture);
        return new PayPalCaptureResult(true, captureId, value, amount.GetProperty("currency_code").GetString(), null);
    }

    public async Task<PayPalRefundResult> RefundCaptureAsync(string captureId, decimal amount, string currency, CancellationToken cancellationToken)
    {
        var body = new { amount = new { value = FormatAmount(amount), currency_code = currency } };

        using var response = await SendAsync(HttpMethod.Post, $"/v2/payments/captures/{Uri.EscapeDataString(captureId)}/refund", body, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // A previous attempt refunded successfully but its database write was lost - the
            // money is already back with the buyer, so let the caller reconcile its own state.
            if ((int)response.StatusCode == 422 && responseText.Contains("CAPTURE_FULLY_REFUNDED", StringComparison.Ordinal))
            {
                _logger.LogWarning("PayPal capture {CaptureId} was already fully refunded; treating the refund as successful", captureId);
                return new PayPalRefundResult(true, null, null);
            }

            _logger.LogError("PayPal refund for capture {CaptureId} failed with status {Status}: {Body}", captureId, (int)response.StatusCode, responseText);
            return new PayPalRefundResult(false, null, $"HTTP {(int)response.StatusCode}");
        }

        using var json = JsonDocument.Parse(responseText);
        var status = json.RootElement.GetProperty("status").GetString();
        var refundId = json.RootElement.GetProperty("id").GetString();

        // PENDING refunds are still accepted by PayPal (they settle later), so only an
        // explicit FAILED/CANCELLED is treated as a failure.
        var failed = string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase);
        return failed
            ? new PayPalRefundResult(false, refundId, $"Refund status {status}")
            : new PayPalRefundResult(true, refundId, null);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(method, _options.ApiBaseUrl + path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = _httpClientFactory.CreateClient();
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiBaseUrl + "/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")]),
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal OAuth token request failed with status {Status}", (int)response.StatusCode);
            throw new HttpRequestException($"PayPal OAuth token request failed with status {(int)response.StatusCode}.");
        }

        using var json = JsonDocument.Parse(responseText);
        var token = json.RootElement.GetProperty("access_token").GetString()!;
        var expiresInSeconds = json.RootElement.GetProperty("expires_in").GetInt32();

        _cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(Math.Max(expiresInSeconds - 60, 30)));
        return token;
    }

    private static string FormatAmount(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}
