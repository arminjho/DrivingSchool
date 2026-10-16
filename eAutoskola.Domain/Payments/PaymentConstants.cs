namespace eAutoskola.Domain.Payments;

/// <summary>Business constants for the finance module. Lives in Domain so the seeder,
/// services and unit tests all share one definition.</summary>
public static class PaymentConstants
{
    /// <summary>Practical lessons included in the package ("Praktična vožnja (30h)"). Confirmed
    /// or Completed lessons beyond this count are charged individually.</summary>
    public const int PackagePracticalLessonQuota = 30;

    /// <summary>A Student may only cancel a paid lesson (with refund) at least this many hours
    /// before it starts. Instructors and Admins are not subject to it.</summary>
    public const int RefundCutoffHours = 24;

    /// <summary>A not-yet-captured PayPal order younger than this is reused instead of creating
    /// a second one; older ones are marked Failed.</summary>
    public const int PendingOrderExpiryMinutes = 30;

    /// <summary>Fixed currency-board rate (1 EUR = 1.95583 KM). PayPal has no BAM support, so
    /// KM amounts are converted to EUR only at the PayPal boundary.</summary>
    public const decimal BamPerEur = 1.95583m;

    public const int PackageInstallmentCount = 3;
    public const int InstallmentIntervalDays = 30;
}

/// <summary>Stable ServiceType.Code values the payment logic depends on.</summary>
public static class ServiceTypeCodes
{
    public const string TheoreticalTraining = "THEORY";
    public const string PracticalDriving = "PRACTICAL";
    public const string AdministrationAndExam = "ADMIN_EXAM";
    public const string ExtraLesson = "EXTRA_LESSON";

    public static readonly string[] Package = [TheoreticalTraining, PracticalDriving, AdministrationAndExam];
}
