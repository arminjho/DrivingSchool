using eAutoskola.Application.Payments;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Payments;

public class FinanceServiceTests
{
    private static FinanceService CreateService(PaymentTestFixture f) => new(f.Context, f.CurrentUser.Object);

    private static async Task GiveStudentPackageAsync(PaymentTestFixture f)
    {
        f.AddPriceList();
        var student = await f.Context.Users.FindAsync(PaymentTestFixture.StudentId);
        await PackageChargeProvisioner.AddPackageChargesAsync(f.Context, student!, default);
        await f.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMy_ForAStudentWithNoCharges_ReturnsAnEmptySummaryAndCreatesNothing()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var saves = f.CountSaves();

        var summary = await CreateService(f).GetMyAsync(default);

        Assert.Empty(summary.CostBreakdown);
        Assert.Empty(summary.PaymentHistory);
        Assert.Equal(0m, summary.RemainingBalance);
        Assert.Null(summary.NextInstallment);
        Assert.Equal(0, saves());
        Assert.Equal(0, await f.Context.Charges.CountAsync());
        Assert.False(f.Context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task GetMy_WithThePackage_ListsTheThreeChargesAndTheFullBalance()
    {
        var f = new PaymentTestFixture();
        await GiveStudentPackageAsync(f);

        var summary = await CreateService(f).GetMyAsync(default);

        Assert.Equal(["Teorijska obuka (20h)", "Praktična vožnja (30h)", "Administracija i ispit"], summary.CostBreakdown.Select(c => c.Description));
        Assert.All(summary.CostBreakdown, c => Assert.False(c.IsPaid));
        Assert.Equal(1550m, summary.RemainingBalance);
        Assert.Equal("Teorijska obuka (20h)", summary.NextInstallment!.Description);
        Assert.Equal(1, summary.NextInstallment.Number);
    }

    [Fact]
    public async Task GetMy_AfterPayingAnInstallment_ReducesTheBalanceAndShowsItInTheHistory()
    {
        var f = new PaymentTestFixture();
        await GiveStudentPackageAsync(f);
        var first = await f.Context.Installments.Include(i => i.Charge).OrderBy(i => i.Id).FirstAsync();
        f.AddPayment(first, PaymentStatus.Completed);

        var summary = await CreateService(f).GetMyAsync(default);

        Assert.Equal(1550m - first.Amount, summary.RemainingBalance);
        var line = Assert.Single(summary.PaymentHistory);
        Assert.Equal("Payment", line.Type);
        Assert.Equal("Teorijska obuka (20h) - rata 1", line.Label);
        Assert.Equal(first.Amount, line.Amount);
        // The paid one is no longer "next"; all first installments fall due the same day, so the
        // next by due date then id is the practical-driving one.
        Assert.Equal("Praktična vožnja (30h)", summary.NextInstallment!.Description);
        Assert.Equal(1, summary.NextInstallment.Number);
    }

    [Fact]
    public async Task GetMy_RefundedExtraLesson_IsLeftOutOfBreakdownAndBalanceAndShowsAsPovratInKm()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var lesson = f.AddLesson(DateTime.UtcNow.AddDays(-3), LessonStatus.Cancelled);
        var installment = f.AddInstallment(lessonId: lesson.Id);
        var payment = f.AddPayment(installment, PaymentStatus.Refunded);
        payment.Refunds.Add(new Refund { Amount = 20.45m, AmountKm = 40m, Status = RefundStatus.Completed, PayPalRefundId = "REF-1" });
        installment.Status = InstallmentStatus.Cancelled;
        (await f.Context.Charges.SingleAsync()).Status = ChargeStatus.Cancelled;
        await f.Context.SaveChangesAsync();

        var summary = await CreateService(f).GetMyAsync(default);

        Assert.Empty(summary.CostBreakdown);
        Assert.Equal(0m, summary.RemainingBalance);
        Assert.Null(summary.NextInstallment);
        Assert.Equal(["Payment", "Refund"], summary.PaymentHistory.Select(h => h.Type));
        var refund = summary.PaymentHistory.Single(h => h.Type == "Refund");
        Assert.Equal("Povrat", refund.Label);
        Assert.Equal(40m, refund.Amount); // KM, not the 20.45 EUR sent to PayPal
    }

    [Fact]
    public async Task GetMy_FailedRefundAttempt_DoesNotAppearInTheHistory()
    {
        var f = new PaymentTestFixture();
        var installment = f.AddInstallment();
        var payment = f.AddPayment(installment, PaymentStatus.Completed);
        payment.Refunds.Add(new Refund { Amount = 20.45m, AmountKm = 40m, Status = RefundStatus.Failed });
        await f.Context.SaveChangesAsync();

        var summary = await CreateService(f).GetMyAsync(default);

        Assert.DoesNotContain(summary.PaymentHistory, h => h.Type == "Refund");
        Assert.Equal(0m, summary.RemainingBalance); // the installment is still paid
    }

    [Fact]
    public async Task GetMy_OnlyShowsTheCurrentStudentsMoney()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var other = await f.Context.Users.FindAsync(PaymentTestFixture.OtherStudentId);
        await PackageChargeProvisioner.AddPackageChargesAsync(f.Context, other!, default);
        await f.Context.SaveChangesAsync();

        var summary = await CreateService(f).GetMyAsync(default);

        Assert.Empty(summary.CostBreakdown);
        Assert.Equal(0m, summary.RemainingBalance);
    }
}
