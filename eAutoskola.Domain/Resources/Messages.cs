namespace eAutoskola.Domain.Resources;

/// <summary>
/// Every Bosnian user-facing string of the backend (exception messages, validation messages,
/// notification titles/texts, lesson history reasons, recommender explanations) lives here -
/// never scattered as literals in services/controllers. PDF texts live in ReportTexts and
/// e-mail texts in the Worker's EmailTemplates. Log messages stay in English, written directly
/// at the call site.
/// </summary>
public static class Messages
{
    // --- Generic ---
    public const string UnexpectedError = "Došlo je do neočekivane greške. Pokušajte ponovo kasnije.";
    public const string RecordInUseCannotModify = "Zapis se ne može izmijeniti ili obrisati jer je povezan sa drugim podacima u sistemu.";
    public const string ValidationFailed = "Dati podaci nisu validni.";
    public const string EntityNotFound = "{0} sa ID-om '{1}' nije pronađen/a.";
    public const string NotAuthenticated = "Korisnik nije prijavljen.";

    // --- Validation format hints ---
    public const string InvalidEmailFormat = "Unesite e-mail u formatu ime@domena.com.";
    public const string InvalidPhoneFormat = "Broj telefona mora biti u formatu +387 6X XXX XXX.";
    public const string RequiredField = "Ovo polje je obavezno.";
    public const string PasswordTooShort = "Lozinka mora imati najmanje 8 znakova.";
    public const string PasswordsDoNotMatch = "Nova lozinka i potvrda lozinke se ne podudaraju.";
    public const string MaxLengthExceeded = "{0} ne smije imati više od {1} znakova.";
    public const string FirstNameLabel = "Ime";
    public const string LastNameLabel = "Prezime";

    // --- Auth ---
    public const string InvalidCredentials = "Pogrešan e-mail ili lozinka.";
    public const string AccountDisabled = "Korisnički račun je deaktiviran.";
    public const string TokenRevoked = "Token je opozvan (izvršena je odjava).";
    public const string TokenMissingJti = "Token nema jti claim.";
    public const string PasswordChangedReloginRequired = "Lozinka je promijenjena - potrebna je ponovna prijava.";
    public const string OldPasswordIncorrect = "Stara lozinka nije ispravna.";
    public const string InvalidOrExpiredResetCode = "Kod za resetovanje lozinke je neispravan ili je istekao.";

    // --- Users ---
    public const string EmailAlreadyExists = "Korisnik sa ovom e-mail adresom već postoji.";
    public const string RoleInvalid = "Odabrana uloga nije validna.";
    public const string CityNotFound = "Odabrani grad ne postoji.";
    public const string VehicleCategoryNotFound = "Odabrana kategorija vozila ne postoji.";
    public const string UserHasLessons = "Korisnik se ne može obrisati jer ima evidentirane časove u sistemu.";

    // --- Ownership / authorization ---
    public const string NotOwner = "Nemate dozvolu za rad nad ovim podatkom.";
    public const string FileAccessDenied = "Nemate dozvolu za pristup ovom fajlu.";

    // --- Vehicles ---
    public const string RegistrationNumberInvalid = "Unesite validnu registarsku oznaku u formatu: X00-X-000 (npr. E22-A-123).";
    public const string RegistrationNumberExists = "Vozilo sa ovom registarskom oznakom već postoji.";
    public const string VehicleHasFutureLesson = "Vozilo se ne može obrisati jer je dodijeljeno potvrđenom času vožnje u budućnosti.";
    public const string VehicleNotFound = "Odabrano vozilo ne postoji.";

    // --- Lessons ---
    public const string StudentAccountDeactivated = "Vaš nalog je deaktiviran, nije moguće zakazati čas.";
    public const string StudentProfileNotFound = "Profil učenika nije pronađen.";
    public const string InstructorUnavailable = "Odabrani instruktor nije dostupan.";
    public const string SlotMustBeInFuture = "Termin mora biti u budućnosti.";
    public const string SlotOverlaps = "Odabrani termin se preklapa sa postojećim zahtjevom ili časom.";
    public const string LessonRescheduleOnlyPending = "Termin se može izmijeniti samo dok je zahtjev na čekanju.";
    public const string LessonConfirmNeedsVehicle = "Nije moguće potvrditi čas jer nije dodijeljeno vozilo.";
    public const string LessonCancelCutoff = "Čas se može otkazati najkasnije {0}h prije termina.";
    public const string LessonStartOnlyConfirmed = "Čas se može započeti samo ako je potvrđen.";
    public const string LessonCompleteNeedsStart = "Čas mora biti započet prije nego što se može završiti.";
    public const string RatingOutOfRange = "Ocjena mora biti između 1 i 5.";
    public const string RequestStatusMissing = "Status zahtjeva nije pronađen u šifarniku. Obratite se administratoru.";
    public const string RequestStatusCodeFixed = "Šifra ovog statusa je vezana za tok časova i ne može se mijenjati. Možete izmijeniti naziv i opis.";
    public const string RequestStatusUsedByLessonFlow = "Ovaj status koristi tok časova (Na čekanju, Potvrđen, Otkazan, Završen) i ne može se obrisati.";
    public const string LessonTransitionNotAllowed = "Prelaz iz stanja '{0}' u stanje '{1}' nije dozvoljen.";

    // --- Lesson status history reasons ---
    public const string HistoryLessonRequested = "Kreiran zahtjev za čas.";
    public const string HistoryLessonRescheduled = "Termin izmijenjen.";
    public const string HistoryLessonConfirmed = "Zahtjev potvrđen.";
    public const string HistoryLessonCancelled = "Zahtjev otkazan.";
    public const string HistoryLessonCompleted = "Čas uspješno odrađen.";

    // --- Notifications (title / text shown in the apps) ---
    public const string NotificationLessonRequestedTitle = "Novi zahtjev za čas";
    public const string NotificationLessonRequestedBody = "{0} je zatražio/la čas vožnje.";
    public const string NotificationLessonConfirmedTitle = "Čas potvrđen";
    public const string NotificationLessonConfirmedBody = "Vaš zahtjev za čas vožnje je potvrđen.";
    public const string NotificationLessonConfirmedExtraBody = "Vaš zahtjev za čas vožnje je potvrđen. Ovaj čas nije obuhvaćen paketom i potrebno ga je platiti ({0:F2} KM).";
    public const string NotificationLessonRejectedTitle = "Zahtjev odbijen";
    public const string NotificationLessonRejectedBody = "Vaš zahtjev je odbijen: {0}";
    public const string NotificationLessonCancelledTitle = "Čas otkazan";
    public const string NotificationLessonCancelledBody = "Čas vožnje je otkazan.";
    public const string NotificationLessonCancelledWithReasonBody = "Čas vožnje je otkazan: {0}";
    public const string NotificationLessonCompletedTitle = "Čas završen";
    public const string NotificationLessonCompletedBody = "Vaš čas vožnje je uspješno odrađen.";
    public const string NotificationRefundTitle = "Povrat novca";
    public const string NotificationRefundBody = "Izvršen je povrat novca u iznosu od {0:F2} KM.";
    public const string NotificationPaymentReceivedTitle = "Uplata primljena";
    public const string NotificationAdminLessonCancelledBody = "Čas vožnje učenika {0} je otkazan.";
    public const string NotificationAdminPaymentReceivedBody = "Uplata učenika {0} u iznosu od {1:F2} KM je evidentirana.";
    public const string NotificationAdminRefundBody = "Izvršen je povrat novca učeniku {0} u iznosu od {1:F2} KM.";
    public const string NotificationPaymentReceivedBody = "Vaša uplata u iznosu od {0:F2} KM je uspješno evidentirana.";

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
    public const string PayPalOrderDescription = "eAutoškola - {0}";

    // --- Tests / questions ---
    public const string TestHasNoQuestions = "Test trenutno nema pitanja.";
    public const string TestHasAttempts = "Test se ne može obrisati jer postoje evidentirani pokušaji rješavanja.";
    public const string AttemptAlreadySubmitted = "Ovaj pokušaj je već predat.";
    public const string AttemptReviewNotAvailable = "Pregled je dostupan tek nakon predaje testa.";
    public const string AnswerOptionInvalid = "Odabrani odgovor ne pripada pitanju.";
    public const string QuestionHasAnswers = "Pitanje se ne može obrisati jer postoje evidentirani odgovori učenika.";
    public const string QuestionNeedsFourOptions = "Pitanje mora imati tačno 4 ponuđena odgovora.";
    public const string QuestionNeedsOneCorrectOption = "Tačno jedan ponuđeni odgovor mora biti označen kao tačan.";

    // --- Recommender (docs: recommender-dokumentacija.md §6) ---
    public const string RecommendationExplanation = "Preporučujemo vam da ponovite lekciju '{0}' jer ste na posljednjem probnom testu imali {1}% netačnih odgovora iz ove specifične kategorije ({2}).";
    public const string RecommendationCriticalSuffix = " Ova oblast je označena kao kritična jer ste greške iz nje ponovili u više probnih testova.";
    public const string RecommendationColdStart = "Preporuke će biti dostupne nakon što riješite barem jedan probni test.";

    // --- Reports ---
    public const string ReportDateRangeInvalid = "Datum „do” ne smije biti prije datuma „od”. Unesite datume u formatu DD.MM.GGGG.";
    public const string ReportInstructorNotFound = "Odabrani instruktor ne postoji.";
    public const string ReportVehicleCategoryNotFound = VehicleCategoryNotFound;

    // --- File upload ---
    public const string InvalidFileType = "Dozvoljeni su samo fajlovi tipa JPG, PNG ili WEBP.";
    public const string FileTooLarge = "Fajl je prevelik. Maksimalna dozvoljena veličina je {0} MB.";
}
