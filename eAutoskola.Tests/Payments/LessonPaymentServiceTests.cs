using eAutoskola.Application.Payments;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace eAutoskola.Tests.Payments;

public class LessonPaymentServiceTests
{
    private static LessonPaymentService CreateService(PaymentTestFixture f)
        => new(f.Context, f.PayPal.Object, NullLogger<LessonPaymentService>.Instance);

    /// <summary>A Confirmed lesson 3 days out whose extra-lesson charge is paid.</summary>
    private static (Lesson Lesson, Payment Payment) PaidLesson(PaymentTestFixture f, int hoursUntilStart = 72)
    {
        var lesson = f.AddLesson(DateTime.UtcNow.AddHours(hoursUntilStart));
        var installment = f.AddInstallment(lessonId: lesson.Id);
        var payment = f.AddPayment(installment, PaymentStatus.Completed);
        return (lesson, payment);
    }

    // --- extra-lesson charge on confirm ---

    [Fact]
    public async Task AddExtraLessonCharge_WithinQuota_AddsNothing()
    {
        var f = new PaymentTestFixture();
        for (var i = 0; i < PaymentConstants.PackagePracticalLessonQuota - 1; i++)
        {
            f.Context.Lessons.Add(new Lesson { StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1, Status = LessonStatus.Completed });
        }
        await f.Context.SaveChangesAsync();
        var lesson = f.AddLesson(DateTime.UtcNow.AddDays(3), LessonStatus.Pending);

        var price = await CreateService(f).AddExtraLessonChargeIfNeededAsync(lesson, default);

        Assert.Null(price);
        Assert.Equal(0, await f.Context.Charges.CountAsync());
    }

    [Fact]
    public async Task AddExtraLessonCharge_BeyondQuota_StagesChargeWithOneInstallmentDueAtLessonStart()
    {
        var f = new PaymentTestFixture();
        f.Context.ServiceTypes.Add(new ServiceType { Id = 1, Code = ServiceTypeCodes.ExtraLesson, Name = "Dodatni čas vožnje", Price = 40m });
        for (var i = 0; i < PaymentConstants.PackagePracticalLessonQuota; i++)
        {
            f.Context.Lessons.Add(new Lesson { StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1, Status = i % 2 == 0 ? LessonStatus.Completed : LessonStatus.Confirmed });
        }
        await f.Context.SaveChangesAsync();
        var lesson = f.AddLesson(DateTime.UtcNow.AddDays(3), LessonStatus.Pending);

        var price = await CreateService(f).AddExtraLessonChargeIfNeededAsync(lesson, default);
        await f.Context.SaveChangesAsync();

        Assert.Equal(40m, price);
        var charge = await f.Context.Charges.Include(c => c.Installments).SingleAsync();
        Assert.Equal(lesson.Id, charge.LessonId);
        Assert.Equal(ChargeStatus.Active, charge.Status);
        var installment = Assert.Single(charge.Installments);
        Assert.Equal(40m, installment.Amount);
        Assert.Equal(lesson.StartsAtUtc, installment.DueDate);
    }

    [Fact]
    public async Task AddExtraLessonCharge_PriceNotConfigured_BlocksConfirmation()
    {
        var f = new PaymentTestFixture();
        for (var i = 0; i < PaymentConstants.PackagePracticalLessonQuota; i++)
        {
            f.Context.Lessons.Add(new Lesson { StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1, Status = LessonStatus.Completed });
        }
        await f.Context.SaveChangesAsync();
        var lesson = f.AddLesson(DateTime.UtcNow.AddDays(3), LessonStatus.Pending);

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).AddExtraLessonChargeIfNeededAsync(lesson, default));
    }

    // --- cancelling an unpaid lesson ---

    [Fact]
    public async Task Cancel_UnpaidLesson_CancelsChargeAndInstallmentAndFailsOpenOrders()
    {
        var f = new PaymentTestFixture();
        var lesson = f.AddLesson(DateTime.UtcNow.AddDays(3));
        var installment = f.AddInstallment(lessonId: lesson.Id);
        f.AddPayment(installment, PaymentStatus.Created);
        var service = CreateService(f);

        var plan = await service.PrepareCancellationAsync(lesson, actorIsStudent: true, default);
        var refund = await service.ApplyCancellationAsync(plan, lesson, default);
        await f.Context.SaveChangesAsync();

        Assert.Null(refund);
        Assert.Equal(ChargeStatus.Cancelled, (await f.Context.Charges.SingleAsync()).Status);
        Assert.Equal(InstallmentStatus.Cancelled, (await f.Context.Installments.SingleAsync()).Status);
        Assert.Equal(PaymentStatus.Failed, (await f.Context.Payments.SingleAsync()).Status);
        f.PayPal.Verify(p => p.RefundCaptureAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_LessonWithoutCharge_HasNoPaymentPlan()
    {
        var f = new PaymentTestFixture();
        var lesson = f.AddLesson(DateTime.UtcNow.AddDays(3));

        Assert.Null(await CreateService(f).PrepareCancellationAsync(lesson, actorIsStudent: true, default));
    }

    // --- cancelling a paid lesson ---

    [Fact]
    public async Task Cancel_PaidLesson_RefundsCapturedEurAmountAndMarksEverythingRefundedCancelled()
    {
        var f = new PaymentTestFixture();
        var (lesson, payment) = PaidLesson(f);
        f.PayPal.Setup(p => p.RefundCaptureAsync("CAPTURE-1", 20.45m, "EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalRefundResult(true, "REFUND-1", null));
        var service = CreateService(f);

        var plan = await service.PrepareCancellationAsync(lesson, actorIsStudent: true, default);
        var outcome = await service.ApplyCancellationAsync(plan, lesson, default);
        await f.Context.SaveChangesAsync();

        Assert.Equal(40m, outcome!.AmountKm);
        Assert.Equal(PaymentStatus.Refunded, (await f.Context.Payments.SingleAsync()).Status);
        Assert.Equal(InstallmentStatus.Cancelled, (await f.Context.Installments.SingleAsync()).Status);
        Assert.Equal(ChargeStatus.Cancelled, (await f.Context.Charges.SingleAsync()).Status);
        var refund = await f.Context.Refunds.SingleAsync();
        Assert.Equal(RefundStatus.Completed, refund.Status);
        Assert.Equal(20.45m, refund.Amount);
        Assert.Equal(40m, refund.AmountKm);
        Assert.Equal("REFUND-1", refund.PayPalRefundId);
        Assert.Equal(payment.Id, refund.PaymentId);
    }

    [Fact]
    public async Task Cancel_PaidLesson_PayPalRefusesRefund_RecordsFailedRefundAndChangesNothingElse()
    {
        var f = new PaymentTestFixture();
        var (lesson, _) = PaidLesson(f);
        f.PayPal.Setup(p => p.RefundCaptureAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalRefundResult(false, null, "HTTP 500"));
        var service = CreateService(f);

        var plan = await service.PrepareCancellationAsync(lesson, actorIsStudent: false, default);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.ApplyCancellationAsync(plan, lesson, default));

        Assert.Contains("Povrat novca trenutno nije moguć", ex.Message);
        Assert.Equal(RefundStatus.Failed, (await f.Context.Refunds.SingleAsync()).Status);
        Assert.Equal(PaymentStatus.Completed, (await f.Context.Payments.SingleAsync()).Status);
        Assert.Equal(InstallmentStatus.Paid, (await f.Context.Installments.SingleAsync()).Status);
        Assert.Equal(ChargeStatus.Active, (await f.Context.Charges.SingleAsync()).Status);
        Assert.Equal(LessonStatus.Confirmed, (await f.Context.Lessons.SingleAsync()).Status);
    }

    [Fact]
    public async Task Cancel_PaidLesson_PayPalThrows_IsTreatedAsFailedRefund()
    {
        var f = new PaymentTestFixture();
        var (lesson, _) = PaidLesson(f);
        f.PayPal.Setup(p => p.RefundCaptureAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));
        var service = CreateService(f);

        var plan = await service.PrepareCancellationAsync(lesson, actorIsStudent: false, default);
        await Assert.ThrowsAsync<BusinessException>(() => service.ApplyCancellationAsync(plan, lesson, default));

        Assert.Equal(RefundStatus.Failed, (await f.Context.Refunds.SingleAsync()).Status);
        Assert.Equal(PaymentStatus.Completed, (await f.Context.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Cancel_PaidLesson_StudentInsideCutoff_IsBlockedBeforePayPalIsCalled()
    {
        var f = new PaymentTestFixture();
        var (lesson, _) = PaidLesson(f, hoursUntilStart: PaymentConstants.RefundCutoffHours - 2);

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(f).PrepareCancellationAsync(lesson, actorIsStudent: true, default));
        f.PayPal.Verify(p => p.RefundCaptureAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_PaidLesson_InstructorOrAdminInsideCutoff_StillRefunds()
    {
        var f = new PaymentTestFixture();
        var (lesson, _) = PaidLesson(f, hoursUntilStart: 2);
        f.PayPal.Setup(p => p.RefundCaptureAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayPalRefundResult(true, "REFUND-1", null));
        var service = CreateService(f);

        var plan = await service.PrepareCancellationAsync(lesson, actorIsStudent: false, default);
        var outcome = await service.ApplyCancellationAsync(plan, lesson, default);

        Assert.NotNull(outcome);
    }
}
