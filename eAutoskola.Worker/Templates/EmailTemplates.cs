using System.Globalization;

namespace eAutoskola.Worker.Templates;

public record EmailContent(string Subject, string BodyHtml);

/// <summary>
/// Every outbound e-mail's Bosnian subject/body lives here, in one place (prompt: "E-mail
/// templates kept in one templates folder/class") - handlers only supply data, never compose
/// copy themselves.
/// </summary>
public static class EmailTemplates
{
    private static readonly CultureInfo BosnianDateCulture = CultureInfo.InvariantCulture;

    public static EmailContent PasswordResetRequested(string toName, string code) => new(
        "eAutoškola - Resetovanje lozinke",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Vaš kod za resetovanje lozinke je: <strong>{code}</strong></p>
        <p>Kod vrijedi 15 minuta. Ako niste zatražili resetovanje lozinke, zanemarite ovaj e-mail.</p>
        """);

    public static EmailContent LessonConfirmed(string toName, DateTime startsAtUtc) => new(
        "eAutoškola - Čas potvrđen",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Vaš čas {FormatDateTime(startsAtUtc)} je potvrđen.</p>
        """);

    public static EmailContent LessonRejected(string toName, string reason) => new(
        "eAutoškola - Zahtjev za čas odbijen",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Vaš zahtjev za čas vožnje je odbijen.</p>
        <p>Razlog: <strong>{reason}</strong></p>
        """);

    public static EmailContent LessonCancelled(string toName, string reason) => new(
        "eAutoškola - Čas otkazan",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Čas vožnje je otkazan.</p>
        <p>Razlog: <strong>{reason}</strong></p>
        """);

    public static EmailContent LessonCompleted(string toName, int rating) => new(
        "eAutoškola - Čas završen",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Vaš čas vožnje je uspješno odrađen.</p>
        <p>Ocjena instruktora: <strong>{rating}/5</strong></p>
        """);

    public static EmailContent PaymentReceived(string toName, decimal amount) => new(
        "eAutoškola - Potvrda uplate",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Vaša uplata u iznosu od <strong>{amount.ToString("F2", BosnianDateCulture)} KM</strong> je uspješno evidentirana.</p>
        <p>Hvala Vam na povjerenju.</p>
        """);

    public static EmailContent RefundIssued(string toName, decimal amount) => new(
        "eAutoškola - Potvrda povrata sredstava",
        $"""
        <p>Poštovani/a {toName},</p>
        <p>Izvršen je povrat sredstava u iznosu od <strong>{amount.ToString("F2", BosnianDateCulture)} KM</strong>.</p>
        <p>Sredstva će biti vidljiva na Vašem računu u skladu sa rokovima Vaše banke/PayPal naloga.</p>
        """);

    /// <summary>"15.05.2026. u 10:00" - the exact format requested.</summary>
    private static string FormatDateTime(DateTime value) => $"{value:dd.MM.yyyy}. u {value:HH:mm}";
}
