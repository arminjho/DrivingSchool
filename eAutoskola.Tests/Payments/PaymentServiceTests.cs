using eAutoskola.Application.Notifications;
using eAutoskola.Application.Payments;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace eAutoskola.Tests.Payments;

public class PaymentServiceTests
{
    private static PaymentService CreateService(PaymentTestFixture f)
        => new(f.Context, f.CurrentUser.Object, f.PayPal.Object,
            new NotificationService(f.Context, f.CurrentUser.Object, f.Pusher.Object),
            f.Publisher.Object, NullLogger<PaymentService>.Instance);

    private static void SetupCreateOrder(PaymentTestFixture f, string orderId = "NEW-ORDER")
        => f.PayPal.Setup(p => p.CreateOrderAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalOrderResult(orderId, $"https://paypal.test/approve/{orderId}"));

    // --- create-order ---

    [Fact]
    public async Task CreateOrder_NewInstallment_ConvertsKmToEurAndStoresBothAmounts()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        SetupCreateOrder(f);

        var result = await CreateService(f).CreateOrderAsync(installment.Id, default);

        f.PayPal.Verify(p => p.CreateOrderAsync(20.45m, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("NEW-ORDER", result.OrderId);
        Assert.Equal(40m, result.Amount);
        Assert.Equal(20.45m, result.PayPalAmount);
        var payment = await f.Context.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(40m, payment.Amount);
        Assert.Equal("EUR", payment.PayPalCurrency);
    }

    [Fact]
    public async Task CreateOrder_FreshAbandonedOrder_ReturnsSameOrderWithoutCallingPayPal()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Created, DateTime.UtcNow.AddMinutes(-5), "OLD-ORDER");

        var result = await CreateService(f).CreateOrderAsync(installment.Id, default);

        Assert.Equal("OLD-ORDER", result.OrderId);
        Assert.Equal("https://paypal.test/approve/OLD-ORDER", result.ApprovalUrl);
        f.PayPal.Verify(p => p.CreateOrderAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(1, await f.Context.Payments.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_ExpiredOrder_IsMarkedFailedAndANewOrderIsCreated()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Created, DateTime.UtcNow.AddMinutes(-45), "OLD-ORDER");
        SetupCreateOrder(f);

        var result = await CreateService(f).CreateOrderAsync(installment.Id, default);

        Assert.Equal("NEW-ORDER", result.OrderId);
        Assert.Equal(PaymentStatus.Failed, (await f.Context.Payments.SingleAsync(p => p.PayPalOrderId == "OLD-ORDER")).Status);
        Assert.Equal(PaymentStatus.Created, (await f.Context.Payments.SingleAsync(p => p.PayPalOrderId == "NEW-ORDER")).Status);
    }

    [Fact]
    public async Task CreateOrder_AlreadyPaidInstallment_IsBlocked()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Completed);

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).CreateOrderAsync(installment.Id, default));
        f.PayPal.Verify(p => p.CreateOrderAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_OtherStudentsInstallment_IsForbidden()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment(PaymentTestFixture.OtherStudentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService(f).CreateOrderAsync(installment.Id, default));
    }

    [Fact]
    public async Task CreateOrder_CancelledCharge_IsBlocked()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        installment.Status = InstallmentStatus.Cancelled;
        await f.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).CreateOrderAsync(installment.Id, default));
    }

    [Fact]
    public async Task CreateOrder_PayPalDown_BecomesBusinessExceptionAndStoresNothing()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.PayPal.Setup(p => p.CreateOrderAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).CreateOrderAsync(installment.Id, default));
        Assert.Equal(0, await f.Context.Payments.CountAsync());
    }

    // --- capture ---

    [Fact]
    public async Task Capture_FirstTime_MarksInstallmentPaidNotifiesAndPublishesOnce()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Created);
        f.PayPal.Setup(p => p.CaptureOrderAsync("ORDER-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalCaptureResult(true, "CAPTURE-9", 20.45m, "EUR", null));

        var result = await CreateService(f).CaptureAsync("ORDER-1", default);

        Assert.False(result.AlreadyProcessed);
        var payment = await f.Context.Payments.Include(p => p.Installment).SingleAsync();
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(20.45m, payment.CapturedAmount);
        Assert.Equal("CAPTURE-9", payment.PayPalCaptureId);
        Assert.NotNull(payment.CompletedAt);
        Assert.Equal(InstallmentStatus.Paid, payment.Installment!.Status);
        Assert.Single(await f.Context.Notifications.Where(n => n.Type == NotificationType.PaymentReceived).ToListAsync());
        f.Publisher.Verify(p => p.PublishAsync(It.Is<PaymentReceivedMessage>(m => m.Amount == 40m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Capture_RepeatCall_IsIdempotentAndRepeatsNoSideEffects()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Created);
        f.PayPal.Setup(p => p.CaptureOrderAsync("ORDER-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalCaptureResult(true, "CAPTURE-9", 20.45m, "EUR", null));
        var service = CreateService(f);

        await service.CaptureAsync("ORDER-1", default);
        var second = await service.CaptureAsync("ORDER-1", default);

        Assert.True(second.AlreadyProcessed);
        f.PayPal.Verify(p => p.CaptureOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(await f.Context.Notifications.Where(n => n.Type == NotificationType.PaymentReceived).ToListAsync());
        f.Publisher.Verify(p => p.PublishAsync(It.IsAny<PaymentReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Capture_BuyerHasNotApproved_ChangesNothing()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Created);
        f.PayPal.Setup(p => p.CaptureOrderAsync("ORDER-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalCaptureResult(false, null, 0, null, "HTTP 422"));

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).CaptureAsync("ORDER-1", default));

        var payment = await f.Context.Payments.Include(p => p.Installment).SingleAsync();
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(InstallmentStatus.Pending, payment.Installment!.Status);
        f.Publisher.Verify(p => p.PublishAsync(It.IsAny<PaymentReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Capture_UnknownOrder_IsNotFound()
    {
        var f = new PaymentTestFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(f).CaptureAsync("NOPE", default));
    }

    [Fact]
    public async Task Capture_OtherStudentsOrder_IsForbiddenAndPayPalIsNeverCalled()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment(PaymentTestFixture.OtherStudentId);
        f.AddPayment(installment, PaymentStatus.Created);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService(f).CaptureAsync("ORDER-1", default));
        f.PayPal.Verify(p => p.CaptureOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Capture_FailedExpiredOrder_IsRejectedWithoutCallingPayPal()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Failed);

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).CaptureAsync("ORDER-1", default));
        f.PayPal.Verify(p => p.CaptureOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Capture_NotificationFailure_DoesNotUndoTheCapture()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        f.AddPayment(installment, PaymentStatus.Created);
        f.PayPal.Setup(p => p.CaptureOrderAsync("ORDER-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalCaptureResult(true, "CAPTURE-9", 20.45m, "EUR", null));
        f.Pusher.Setup(p => p.PushAsync(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("signalr down"));

        var result = await CreateService(f).CaptureAsync("ORDER-1", default);

        Assert.Equal("Completed", result.Status);
        Assert.Equal(PaymentStatus.Completed, (await f.Context.Payments.SingleAsync()).Status);
    }
}
