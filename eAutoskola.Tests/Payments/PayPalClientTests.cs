using System.Net;
using System.Text;
using System.Text.Json;
using eAutoskola.Infrastructure.Configuration;
using eAutoskola.Infrastructure.Payments;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace eAutoskola.Tests.Payments;

public class PayPalClientTests
{
    private sealed record Recorded(HttpMethod Method, string Path, string? Authorization, string Body);

    private sealed class StubHandler(Func<HttpRequestMessage, string, (HttpStatusCode Status, string Json)> respond) : HttpMessageHandler
    {
        public List<Recorded> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new Recorded(request.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), body));

            var (status, json) = respond(request, body);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private const string TokenJson = """{"access_token":"TOKEN-1","expires_in":3600}""";

    private static (PayPalClient Client, StubHandler Handler) CreateClient(Func<HttpRequestMessage, string, (HttpStatusCode, string)> api)
    {
        Environment.SetEnvironmentVariable("PAYPAL_CLIENT_ID", "test-client");
        Environment.SetEnvironmentVariable("PAYPAL_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("PAYPAL_BASE_URL", "https://paypal.test");
        Environment.SetEnvironmentVariable("PAYPAL_RETURN_URL", "https://app.test/return");
        Environment.SetEnvironmentVariable("PAYPAL_CANCEL_URL", "https://app.test/cancel");

        var handler = new StubHandler((request, body) => request.RequestUri!.AbsolutePath == "/v1/oauth2/token"
            ? (HttpStatusCode.OK, TokenJson)
            : api(request, body));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));

        var client = new PayPalClient(factory.Object, new MemoryCache(new MemoryCacheOptions()), new PayPalOptions(), NullLogger<PayPalClient>.Instance);
        return (client, handler);
    }

    private static string CompletedOrderJson(string captureId = "CAP-1", string value = "20.45")
        => $$$"""
        {"id":"ORD-1","status":"COMPLETED","purchase_units":[{"payments":{"captures":[{"id":"{{{captureId}}}","status":"COMPLETED","amount":{"currency_code":"EUR","value":"{{{value}}}"}}]}}]}
        """;

    [Fact]
    public async Task CreateOrder_SendsAmountCurrencyAndReturnCancelUrls_AndParsesApprovalLink()
    {
        var (client, handler) = CreateClient((_, _) => (HttpStatusCode.Created,
            """{"id":"ORD-1","status":"CREATED","links":[{"rel":"self","href":"https://api/self"},{"rel":"payer-action","href":"https://paypal.test/approve"}]}"""));

        var order = await client.CreateOrderAsync(20.45m, "eAutoškola - test", default);

        Assert.Equal("ORD-1", order.OrderId);
        Assert.Equal("https://paypal.test/approve", order.ApprovalUrl);

        var request = handler.Requests.Single(r => r.Path == "/v2/checkout/orders");
        Assert.Equal("Bearer TOKEN-1", request.Authorization);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("CAPTURE", body.RootElement.GetProperty("intent").GetString());
        var amount = body.RootElement.GetProperty("purchase_units")[0].GetProperty("amount");
        Assert.Equal("20.45", amount.GetProperty("value").GetString());
        Assert.Equal("EUR", amount.GetProperty("currency_code").GetString());
        var context = body.RootElement.GetProperty("application_context");
        Assert.Equal("https://app.test/return", context.GetProperty("return_url").GetString());
        Assert.Equal("https://app.test/cancel", context.GetProperty("cancel_url").GetString());
        Assert.Equal("PAY_NOW", context.GetProperty("user_action").GetString());
    }

    [Fact]
    public async Task CreateOrder_TruncatesLongDescriptionToPayPalLimit()
    {
        var (client, handler) = CreateClient((_, _) => (HttpStatusCode.Created,
            """{"id":"ORD-1","links":[{"rel":"approve","href":"https://paypal.test/approve"}]}"""));

        await client.CreateOrderAsync(10m, new string('x', 300), default);

        using var body = JsonDocument.Parse(handler.Requests.Single(r => r.Path == "/v2/checkout/orders").Body);
        Assert.Equal(127, body.RootElement.GetProperty("purchase_units")[0].GetProperty("description").GetString()!.Length);
    }

    [Fact]
    public async Task CreateOrder_PayPalError_Throws()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.UnprocessableEntity, """{"name":"UNPROCESSABLE_ENTITY"}"""));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.CreateOrderAsync(10m, "x", default));
    }

    [Fact]
    public async Task AccessToken_IsCachedAcrossCalls()
    {
        var (client, handler) = CreateClient((_, _) => (HttpStatusCode.Created,
            """{"id":"ORD-1","links":[{"rel":"approve","href":"https://paypal.test/approve"}]}"""));

        await client.CreateOrderAsync(10m, "x", default);
        await client.CreateOrderAsync(10m, "y", default);

        Assert.Single(handler.Requests, r => r.Path == "/v1/oauth2/token");
    }

    [Fact]
    public async Task Capture_Completed_ReturnsCaptureIdAndActualCapturedAmount()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.Created, CompletedOrderJson("CAP-7", "20.45")));

        var result = await client.CaptureOrderAsync("ORD-1", default);

        Assert.True(result.Success);
        Assert.Equal("CAP-7", result.CaptureId);
        Assert.Equal(20.45m, result.CapturedAmount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public async Task Capture_BuyerHasNotApproved_ReportsNotSuccessful()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.UnprocessableEntity, """{"details":[{"issue":"ORDER_NOT_APPROVED"}]}"""));

        var result = await client.CaptureOrderAsync("ORD-1", default);

        Assert.False(result.Success);
        Assert.Null(result.CaptureId);
    }

    [Fact]
    public async Task Capture_AlreadyCaptured_ReadsTheExistingCaptureBack()
    {
        var (client, handler) = CreateClient((request, _) => request.Method == HttpMethod.Post
            ? (HttpStatusCode.UnprocessableEntity, """{"details":[{"issue":"ORDER_ALREADY_CAPTURED"}]}""")
            : (HttpStatusCode.OK, CompletedOrderJson("CAP-3", "20.45")));

        var result = await client.CaptureOrderAsync("ORD-1", default);

        Assert.True(result.Success);
        Assert.Equal("CAP-3", result.CaptureId);
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Get && r.Path == "/v2/checkout/orders/ORD-1");
    }

    [Fact]
    public async Task Capture_OrderCompletedButCaptureStillPending_IsNotSuccessful()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.Created, CompletedOrderJson().Replace("\"status\":\"COMPLETED\",\"amount\"", "\"status\":\"PENDING\",\"amount\"")));

        var result = await client.CaptureOrderAsync("ORD-1", default);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Refund_SendsCapturedAmountAndCurrency_AndReturnsRefundId()
    {
        var (client, handler) = CreateClient((_, _) => (HttpStatusCode.Created, """{"id":"REF-1","status":"COMPLETED"}"""));

        var result = await client.RefundCaptureAsync("CAP-1", 20.45m, "EUR", default);

        Assert.True(result.Success);
        Assert.Equal("REF-1", result.RefundId);
        var request = handler.Requests.Single(r => r.Path == "/v2/payments/captures/CAP-1/refund");
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("20.45", body.RootElement.GetProperty("amount").GetProperty("value").GetString());
        Assert.Equal("EUR", body.RootElement.GetProperty("amount").GetProperty("currency_code").GetString());
    }

    [Fact]
    public async Task Refund_Pending_IsStillAccepted()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.Created, """{"id":"REF-1","status":"PENDING"}"""));

        Assert.True((await client.RefundCaptureAsync("CAP-1", 20.45m, "EUR", default)).Success);
    }

    [Fact]
    public async Task Refund_AlreadyFullyRefunded_IsTreatedAsSuccessSoARetryCanReconcile()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.UnprocessableEntity, """{"details":[{"issue":"CAPTURE_FULLY_REFUNDED"}]}"""));

        var result = await client.RefundCaptureAsync("CAP-1", 20.45m, "EUR", default);

        Assert.True(result.Success);
        Assert.Null(result.RefundId);
    }

    [Fact]
    public async Task Refund_Rejected_ReportsFailure()
    {
        var (client, _) = CreateClient((_, _) => (HttpStatusCode.BadRequest, """{"name":"INVALID_REQUEST"}"""));

        Assert.False((await client.RefundCaptureAsync("CAP-1", 20.45m, "EUR", default)).Success);
    }
}
