using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Security;

namespace eAutoskola.Application.Reports;

/// <summary>Every Bosnian string used by the dashboard feed and the PDF reports, in one place.</summary>
public static class ReportTexts
{
    public const string SchoolName = "eAutoškola";

    // --- Common ---
    public const string GeneratedLabel = "Generisano";
    public const string PeriodLabel = "Period";
    public const string PeriodAllTime = "svi datumi";
    public const string PeriodFromOnly = "od {0}";
    public const string PeriodToOnly = "do {0}";
    public const string PeriodRange = "{0} - {1}";
    public const string PageLabel = "Stranica";
    public const string TotalLabel = "Ukupno";
    public const string CurrencySuffix = "KM";
    public const string UtcSuffix = "UTC";

    // --- Lesson status report ---
    public const string LessonReportTitle = "Izvještaj o statusu časova vožnje";
    public const string InstructorLabel = "Instruktor";
    public const string AllInstructors = "Svi instruktori";
    public const string CompletedColumn = "Odrađeni";
    public const string ScheduledColumn = "Zakazani";
    public const string CancelledColumn = "Otkazani";
    public const string ChartTitle = "Grafički prikaz po instruktorima";
    public const string TableTitle = "Pregled po instruktorima";
    public const string ScheduledFootnote = "Zakazani časovi obuhvataju časove na čekanju i potvrđene časove.";
    public const string NoLessons = "Nema časova za odabrani period i filtere.";

    // --- Vehicle & service report ---
    public const string VehicleReportTitle = "Izvještaj o vozilima i servisima";
    public const string VehicleCategoryLabel = "Kategorija vozila";
    public const string AllCategories = "Sve kategorije";
    public const string VehicleCountLabel = "Broj vozila";
    public const string ServiceCountLabel = "Broj servisa";
    public const string TotalCostLabel = "Ukupni troškovi";
    public const string CostByCategoryTitle = "Troškovi po kategorijama";
    public const string ServiceHistoryTitle = "Servisna historija po vozilima";
    public const string CategoryColumn = "Kategorija";
    public const string VehiclesColumn = "Vozila";
    public const string ServicesColumn = "Servisi";
    public const string CostColumn = "Trošak";
    public const string DateColumn = "Datum";
    public const string ServiceTypeColumn = "Vrsta servisa";
    public const string DescriptionColumn = "Opis";
    public const string StatusColumn = "Status";
    public const string VehicleSubtotal = "Ukupno za vozilo";
    public const string NoServices = "Nema servisa u odabranom periodu.";
    public const string NoVehicles = "Nema vozila za odabranu kategoriju.";
    public const string ServicesByStatusTitle = "Servisi po statusu";

    // --- Dashboard activity feed ---
    public const string ActivityLessonRequested = "Novi zahtjev za čas";
    public const string ActivityLessonConfirmed = "Čas odobren";
    public const string ActivityLessonCancelled = "Čas otkazan";
    public const string ActivityLessonCompleted = "Čas završen";
    public const string ActivityVehicleService = "Dodan servis vozila";
    public const string ActivityUserCreated = "Novi korisnik";

    public static string LessonStatusLabel(LessonStatus status) => status switch
    {
        LessonStatus.Pending => "Na čekanju",
        LessonStatus.Confirmed => "Potvrđen",
        LessonStatus.Completed => "Završen",
        LessonStatus.Cancelled => "Otkazan",
        _ => status.ToString(),
    };

    public static string VehicleStatusLabel(VehicleStatus status) => status switch
    {
        VehicleStatus.Active => "Aktivan",
        VehicleStatus.InService => "Na servisu",
        _ => status.ToString(),
    };

    public static string VehicleServiceStatusLabel(VehicleServiceStatus status) => status switch
    {
        VehicleServiceStatus.Scheduled => "Zakazan",
        VehicleServiceStatus.InProgress => "U toku",
        VehicleServiceStatus.Completed => "Završen",
        _ => status.ToString(),
    };

    public static string RoleLabel(string role) => role switch
    {
        Role.Student => "Učenik",
        Role.Instructor => "Instruktor",
        Role.Admin => "Administrator",
        _ => role,
    };

    public static string FormatDate(DateOnly date) => date.ToString("dd.MM.yyyy.");

    public static string FormatDate(DateTime date) => date.ToString("dd.MM.yyyy.");

    public static string FormatMoney(decimal amount) => $"{amount:F2} {CurrencySuffix}";

    public static string FormatPeriod(DateOnly? from, DateOnly? to) => (from, to) switch
    {
        (null, null) => PeriodAllTime,
        ({ } f, null) => string.Format(PeriodFromOnly, FormatDate(f)),
        (null, { } t) => string.Format(PeriodToOnly, FormatDate(t)),
        ({ } f, { } t) => string.Format(PeriodRange, FormatDate(f), FormatDate(t)),
    };
}
