using eAutoskola.Domain.Payments;

namespace eAutoskola.Application.Payments;

/// <summary>Pure payment rules tied to lessons, kept free of EF so they can be unit tested.</summary>
public static class LessonPaymentPolicy
{
    /// <param name="otherConfirmedOrCompletedCount">The student's Confirmed + Completed lessons,
    /// not counting the lesson being confirmed.</param>
    public static bool IsBeyondPackageQuota(int otherConfirmedOrCompletedCount)
        => otherConfirmedOrCompletedCount >= PaymentConstants.PackagePracticalLessonQuota;

    /// <summary>Only a Student is limited by the cutoff; Instructors and Admins can always
    /// cancel (and refund) a paid lesson.</summary>
    public static bool IsRefundAllowed(bool actorIsStudent, DateTime lessonStartsAtUtc, DateTime nowUtc)
        => !actorIsStudent || lessonStartsAtUtc - nowUtc >= TimeSpan.FromHours(PaymentConstants.RefundCutoffHours);

    public static bool IsPendingOrderReusable(DateTime orderCreatedAtUtc, DateTime nowUtc)
        => nowUtc - orderCreatedAtUtc < TimeSpan.FromMinutes(PaymentConstants.PendingOrderExpiryMinutes);
}
