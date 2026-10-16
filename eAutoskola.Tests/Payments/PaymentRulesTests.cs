using eAutoskola.Application.Payments;
using eAutoskola.Domain.Payments;

namespace eAutoskola.Tests.Payments;

public class PaymentRulesTests
{
    [Theory]
    [InlineData(195.583, 100.00)]
    [InlineData(40.00, 20.45)]
    [InlineData(166.67, 85.22)]
    [InlineData(0, 0)]
    public void KmToEur_UsesFixedRateRoundedToTwoDecimals(double km, double expectedEur)
        => Assert.Equal((decimal)expectedEur, CurrencyConversion.KmToEur((decimal)km));

    [Fact]
    public void InstallmentPlanner_LastPartAbsorbsRoundingRemainder()
    {
        var parts = InstallmentPlanner.Split(200m, 3);

        Assert.Equal([66.67m, 66.67m, 66.66m], parts);
        Assert.Equal(200m, parts.Sum());
    }

    [Theory]
    [InlineData(1200, 3)]
    [InlineData(150, 1)]
    [InlineData(40, 1)]
    [InlineData(1550.01, 3)]
    public void InstallmentPlanner_PartsAlwaysSumToTotal(double total, int count)
        => Assert.Equal((decimal)total, InstallmentPlanner.Split((decimal)total, count).Sum());

    [Fact]
    public void InstallmentPlanner_ZeroParts_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => InstallmentPlanner.Split(100m, 0));

    [Theory]
    [InlineData(0, false)]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(45, true)]
    public void IsBeyondPackageQuota_StartsWithTheThirtyFirstLesson(int otherLessons, bool expected)
        => Assert.Equal(expected, LessonPaymentPolicy.IsBeyondPackageQuota(otherLessons));

    [Fact]
    public void IsRefundAllowed_StudentOutsideCutoff_Allowed()
    {
        var now = DateTime.UtcNow;
        Assert.True(LessonPaymentPolicy.IsRefundAllowed(actorIsStudent: true, now.AddHours(PaymentConstants.RefundCutoffHours + 1), now));
    }

    [Fact]
    public void IsRefundAllowed_StudentInsideCutoff_Blocked()
    {
        var now = DateTime.UtcNow;
        Assert.False(LessonPaymentPolicy.IsRefundAllowed(actorIsStudent: true, now.AddHours(PaymentConstants.RefundCutoffHours - 1), now));
    }

    [Fact]
    public void IsRefundAllowed_InstructorOrAdminInsideCutoff_AlwaysAllowed()
    {
        var now = DateTime.UtcNow;
        Assert.True(LessonPaymentPolicy.IsRefundAllowed(actorIsStudent: false, now.AddMinutes(10), now));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(29, true)]
    [InlineData(31, false)]
    [InlineData(600, false)]
    public void IsPendingOrderReusable_ExpiresAfterConfiguredMinutes(int ageMinutes, bool expected)
    {
        var now = DateTime.UtcNow;
        Assert.Equal(expected, LessonPaymentPolicy.IsPendingOrderReusable(now.AddMinutes(-ageMinutes), now));
    }
}
