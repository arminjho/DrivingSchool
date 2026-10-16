namespace eAutoskola.Domain.Resources;

/// <summary>
/// Every Bosnian user-facing string (exception messages, validation messages) lives here -
/// never scattered as literals in services/controllers (prompt 1 item 8). Log messages stay in
/// English, written directly at the call site.
/// </summary>
public static class Messages
{
    // --- Generic ---
    public const string UnexpectedError = "Došlo je do neočekivane greške. Pokušajte ponovo kasnije.";
    public const string RecordInUseCannotModify = "Zapis se ne može izmijeniti ili obrisati jer je povezan sa drugim podacima u sistemu.";

    // --- Validation format hints ---
    public const string InvalidEmailFormat = "Unesite e-mail u formatu ime@domena.com.";
    public const string InvalidPhoneFormat = "Broj telefona mora biti u formatu +387 6X XXX XXX.";
    public const string RequiredField = "Ovo polje je obavezno.";
    public const string PasswordTooShort = "Lozinka mora imati najmanje 8 znakova.";
    public const string PasswordsDoNotMatch = "Nova lozinka i potvrda lozinke se ne podudaraju.";

    // --- Auth ---
    public const string InvalidCredentials = "Pogrešan e-mail ili lozinka.";
    public const string AccountDisabled = "Korisnički račun je deaktiviran.";
    public const string TokenRevoked = "Token je opozvan (izvršena je odjava).";
    public const string TokenMissingJti = "Token nema jti claim.";
    public const string PasswordChangedReloginRequired = "Lozinka je promijenjena - potrebna je ponovna prijava.";
    public const string OldPasswordIncorrect = "Stara lozinka nije ispravna.";
    public const string InvalidOrExpiredResetCode = "Kod za resetovanje lozinke je neispravan ili je istekao.";

    // --- Ownership / authorization ---
    public const string NotOwner = "Nemate dozvolu za rad nad ovim podatkom.";

    // --- Payments ---
    public const string PaymentUnavailable = "Plaćanje trenutno nije moguće. Pokušajte ponovo kasnije.";
    public const string InstallmentAlreadyPaid = "Rata je već plaćena.";
    public const string InstallmentNotPayable = "Ova rata se ne može platiti.";
    public const string PaymentOrderExpired = "Narudžba za plaćanje je istekla ili je otkazana. Pokrenite plaćanje ponovo.";
    public const string PaymentNotCompleted = "Plaćanje nije završeno. Potvrdite plaćanje u PayPal-u i pokušajte ponovo.";
    public const string RefundFailed = "Povrat novca trenutno nije moguć. Čas nije otkazan, pokušajte ponovo ili kontaktirajte administraciju.";
    public const string PaidLessonRefundCutoff = "Čas je plaćen. Otkazivanje uz povrat novca moguće je najkasnije {0}h prije termina.";
    public const string ExtraLessonPriceNotConfigured = "Cijena dodatnog časa nije podešena, nije moguće potvrditi čas.";
    public const string RefundLabel = "Povrat";

    // --- Reports ---
    public const string ReportDateRangeInvalid = "Datum „do” ne smije biti prije datuma „od”. Unesite datume u formatu DD.MM.GGGG.";
    public const string ReportInstructorNotFound = "Odabrani instruktor ne postoji.";
    public const string ReportVehicleCategoryNotFound = "Odabrana kategorija vozila ne postoji.";

    // --- File upload ---
    public const string InvalidFileType = "Dozvoljeni su samo fajlovi tipa JPG, PNG ili WEBP.";
    public const string FileTooLarge = "Fajl je prevelik. Maksimalna dozvoljena veličina je {0} MB.";
}
