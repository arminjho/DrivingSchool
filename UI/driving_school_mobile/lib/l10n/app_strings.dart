/// Every user-facing string of the mobile app, in Bosnian, in one place - so adding another
/// language later means replacing this file, not hunting through widgets.
abstract final class AppStrings {
  // --- App ---
  static const String appName = 'eAutoškola';

  // --- Roles ---
  static const String roleAdmin = 'Administrator';
  static const String roleInstructor = 'Instruktor';
  static const String roleStudent = 'Učenik';

  // --- Common actions ---
  static const String save = 'Sačuvaj';
  static const String cancel = 'Odustani';
  static const String confirm = 'Potvrdi';
  static const String delete = 'Obriši';
  static const String edit = 'Uredi';
  static const String addNew = 'Dodaj';
  static const String close = 'Zatvori';
  static const String retry = 'Pokušaj ponovo';
  static const String back = 'Nazad';
  static const String search = 'Pretraži…';
  static const String clearSearch = 'Poništi pretragu';
  static const String logout = 'Odjava';

  // --- Auth ---
  static const String loginTitle = 'Prijava';
  static const String loginSubtitle = 'Aplikacija za učenike i instruktore autoškole';
  static const String loginAction = 'Prijavi se';
  static const String usernameLabel = 'Korisničko ime ili e-mail';
  static const String passwordLabel = 'Lozinka';
  static const String showPassword = 'Prikaži lozinku';
  static const String hidePassword = 'Sakrij lozinku';
  static const String roleNotAllowed = 'Mobilna aplikacija je namijenjena učenicima i instruktorima autoškole.';
  static const String loginFailed = 'Prijava nije uspjela. Provjerite korisničko ime i lozinku.';

  // --- Shell ---
  static const String homeTitle = 'Početna';
  static const String homeSubtitle = 'Pregled aktivnosti — eAutoškola';
  static const String notificationsTitle = 'Obavijesti';
  static const String notificationsSubtitle = 'Sistemske obavijesti o vašem nalogu';
  static String welcome(String name) => 'Dobrodošli, $name';

  // --- Bottom nav (Student) ---
  static const String navHome = 'Početna';
  static const String navLessons = 'Časovi';
  static const String navTests = 'Testovi';
  static const String navStudy = 'Učenje';
  static const String navProfile = 'Profil';

  // --- Bottom nav (Instructor) ---
  static const String navSchedule = 'Raspored';
  static const String navStudents = 'Učenici';

  // --- Register ---
  static const String registerTitle = 'Registracija';
  static const String registerSubtitle = 'Napravite korisnički račun učenika';
  static const String registerAction = 'Registruj se';
  static const String haveAccount = 'Već imate račun? Prijavite se';
  static const String noAccount = 'Nemate račun? Registrujte se';
  static String registerSuccess(String name) => 'Dobrodošli, $name! Vaš račun je kreiran.';

  // --- Forgot / reset password ---
  static const String forgotPasswordTitle = 'Zaboravljena lozinka';
  static const String forgotPasswordSubtitle = 'Unesite e-mail adresu i poslaćemo vam kod za resetovanje lozinke.';
  static const String forgotPasswordAction = 'Pošalji kod';
  static const String forgotPasswordLink = 'Zaboravili ste lozinku?';
  static const String forgotPasswordSent = 'Ako nalog postoji, kod za resetovanje je poslan na e-mail.';
  static const String resetPasswordTitle = 'Resetovanje lozinke';
  static const String resetPasswordSubtitle = 'Unesite kod koji ste primili na e-mail i novu lozinku.';
  static const String resetPasswordAction = 'Resetuj lozinku';
  static const String resetPasswordSuccess = 'Lozinka je uspješno resetovana. Prijavite se.';
  static const String fieldResetCode = 'Kod za resetovanje';

  // --- Student home (4.2.1) ---
  static const String nextLessonCard = 'Sljedeći čas';
  static const String noUpcomingLesson = 'Nemate zakazanih časova.';
  static const String noUpcomingLessonHint = 'Zakažite svoj prvi čas vožnje.';
  static const String lessonDetailsLink = 'Detalji →';
  static const String menuBookLesson = 'Zakazivanje časa';
  static const String menuFinance = 'Finansije i plaćanje';
  static const String menuMyLessons = 'Moji časovi';
  static const String menuTests = 'Testovi iz teorije';
  static const String menuRecommendations = 'Preporučene lekcije';
  static const String statLessonsDone = 'Odrađenih';
  static const String statTestsSolved = 'Riješenih';
  static const String statAverage = 'Prosjek';
  static const String statLessons = 'Časovi';
  static const String statTests = 'Testovi';
  static const String statAverageLabel = 'Na testovima';

  // --- Booking (4.2.2) ---
  static const String bookLessonTitle = 'Zakazivanje časa';
  static const String bookLessonSubtitle = 'Ispunite formu za zakazivanje vožnje';
  static const String fieldInstructor = 'Instruktor';
  static const String fieldBookingDate = 'Datum';
  static const String fieldSlot = 'Termin';
  static const String fieldZone = 'Zona vožnje';
  static const String fieldNote = 'Napomena';
  static const String noteHint = 'npr. Ako može parking da vježbamo';
  static const String sendRequest = 'Pošalji zahtjev';
  static const String noSlotsForDate = 'Nema slobodnih termina za odabrani datum.';
  static const String selectInstructorFirst = 'Prvo odaberite instruktora.';
  static const String bookingSuccess = 'Zahtjev za čas je poslan.';

  // --- My lessons (4.2.3) ---
  static const String myLessonsTitle = 'Moji časovi';
  static const String tabActive = 'Aktivni';
  static const String tabCompleted = 'Završeni';
  static const String tabCancelled = 'Otkazani';
  static const String noActiveLessons = 'Nemate aktivnih časova.';
  static const String noCompletedLessons = 'Nemate završenih časova.';
  static const String noCancelledLessons = 'Nemate otkazanih časova.';
  static const String viewDetails = 'Pogledaj detalje';

  // --- Lesson details (4.2.4) ---
  static const String lessonDetailsTitle = 'Detalji časa';
  static String lessonNumberLabel(int number) => 'Čas vožnje #$number';
  static const String infoDateTime = 'Datum i termin';
  static const String infoZone = 'Zona vožnje';
  static const String infoInstructor = 'Instruktor';
  static const String infoVehicle = 'Vozilo';
  static const String infoDuration = 'Trajanje';
  static const String infoInstructorNotes = 'Bilješke instruktora';
  static const String infoRating = 'Ocjena';
  static const String cancelRequestAction = 'Otkaži zahtjev';
  static const String rescheduleAction = 'Izmijeni termin';
  static const String cancelNotAllowedCompleted = 'Završen čas se ne može otkazati.';
  static const String cancelNotAllowedCancelled = 'Čas je već otkazan.';
  static const String cancelNotAllowedCutoff = 'Otkazivanje je moguće najkasnije 24 sata prije termina.';
  static const String lessonCancelledMessage = 'Čas je otkazan.';
  static const String rescheduleSuccess = 'Termin je izmijenjen.';
  static const String noVehicleAssignedYet = 'Vozilo još nije dodijeljeno.';

  // --- Tests (4.2.5) ---
  static const String testsTitle = 'Testovi';
  static const String testsSubtitle = 'Vaši rezultati i dostupni testovi';
  static const String testPassed = 'Položen';
  static const String testFailed = 'Nije položen';
  static const String testNotAttempted = 'Nije riješeno';
  static String testScoreLine(int correct, int total) => '$correct/$total tačnih · $total pitanja';
  static const String startTest = 'Pokreni test';

  // --- Question (4.2.6) ---
  static String questionProgress(int current, int total) => 'Pitanje $current / $total';
  static const String signImagePlaceholder = 'Slika znaka';
  static const String previousQuestion = 'Prethodno';
  static const String nextQuestion = 'Sljedeće';
  static const String submitTest = 'Pošalji test';
  static const String confirmSubmitTitle = 'Predaja testa';
  static const String confirmSubmitMessage = 'Da li ste sigurni da želite predati test? Odgovori se poslije ne mogu mijenjati.';
  static const String confirmSubmitAction = 'Predaj test';
  static String unansweredWarning(int count) => count == 1 ? 'Imate 1 neodgovoreno pitanje.' : 'Imate $count neodgovorenih pitanja.';

  // --- Test result (4.2.7) ---
  static const String testResultTitle = 'Rezultat testa';
  static const String resultLabel = 'Rezultat';
  static const String correctAnswers = 'Tačnih odgovora';
  static const String incorrectAnswers = 'Netačnih odgovora';
  static const String weakAreasTitle = 'Slabe oblasti:';
  static const String viewTestReview = 'Pogledaj test';
  static const String viewRecommendations = 'Preporuke';
  static const String retakeTest = 'Ponovi test';
  static const String testReviewTitle = 'Pregled testa';
  static const String yourAnswer = 'Vaš odgovor';
  static const String correctAnswer = 'Tačan odgovor';
  static const String noAnswerGiven = 'Nije odgovoreno';

  // --- Recommendations (4.2.8) ---
  static const String recommendationsTitle = 'Preporuke';
  static const String recommenderCardTitle = 'Preporuka AI asistenta';
  static const String recommendedLessonsTitle = 'Preporučene lekcije za čitanje:';
  static const String recommendationsEmpty = 'Trenutno nemamo preporuka za vas.';
  static const String recommendationsEmptyHint = 'Riješite nekoliko probnih testova da bismo mogli prilagoditi preporuke.';
  static String readingMinutesLabel(int minutes) => '$minutes min čitanja';

  // --- Study lessons (4.2.9) ---
  static const String studyTitle = 'Učenje';
  static const String tabAllLessons = 'Sve lekcije';
  static const String tabMyLessons = 'Moje lekcije';
  static String subLessonCount(int count) => count == 1 ? '1 pod-lekcija' : '$count pod-lekcija';
  static const String noStudyLessons = 'Nema dostupnih lekcija.';
  static const String noMyStudyLessons = 'Još nemate otvorenih lekcija.';
  static const String noMyStudyLessonsHint = 'Otvorite lekciju iz "Sve lekcije" da počnete.';

  // --- Finance & payment (4.2.10) ---
  static const String financeTitle = 'Finansije i plaćanje';
  static const String costOverviewTitle = '🧾 PREGLED TROŠKOVA';
  static const String paymentHistoryTitle = '✅ HISTORIJA PLAĆANJA';
  static const String remainingBalanceTitle = 'PREOSTALO ZA PLATITI';
  static String nextDueDate(String date) => 'Sljedeći rok: $date';
  static const String payNextInstallment = 'Plati sljedeću ratu';
  static const String noRemainingBalance = 'Nemate preostalih dugovanja.';
  static const String paymentHistoryEmpty = 'Još nema uplata.';
  static const String amountToPay = 'Iznos za plaćanje';
  static const String payWithPayPal = '⚡ Plaćanje putem PayPal-a';
  static const String paypalSecureNote = '🔒 Sigurno plaćanje putem PayPal Sandbox servisa';
  static const String paymentSuccess = 'Uplata je uspješno izvršena.';
  static const String paymentCancelledMessage = 'Plaćanje je otkazano.';
  static const String paymentFailed = 'Plaćanje nije odobreno na PayPal-u. Pokušajte ponovo.';
  static const String refundLabel = 'Povrat';
  static const String cannotOpenPayPal = 'Nije moguće otvoriti PayPal stranicu.';
  static String cannotOpenPayPalWithReason(Object error) => 'Nije moguće otvoriti PayPal stranicu: $error';
  static const String payPalOpensInBrowser = 'PayPal se otvara u pregledniku';
  static const String reopenPayPal = 'Ponovo otvori PayPal';

  // --- Profile (4.2.11) ---
  static const String myProfile = 'Moj profil';
  static const String fieldFullName = 'Ime i prezime';
  static const String fieldFirstName = 'Ime';
  static const String fieldLastName = 'Prezime';
  static const String fieldEmail = 'Email adresa';
  static const String fieldPhone = 'Broj telefona';
  static const String fieldCategory = 'Kategorija';
  static const String fieldCity = 'Grad';
  static const String editProfileAction = 'Izmijeni podatke';
  static const String changePasswordAction = 'Promijeni lozinku';
  static const String changePasswordTitle = 'Promjena lozinke';
  static const String fieldOldPassword = 'Stara lozinka';
  static const String fieldNewPassword = 'Nova lozinka';
  static const String fieldConfirmPassword = 'Potvrdi lozinku';
  static const String saveNewPassword = 'Sačuvaj lozinku';
  static const String logoutAction = 'Odjavi se';
  static const String profileUpdated = 'Podaci su sačuvani.';
  static const String passwordChanged = 'Lozinka je promijenjena.';
  static const String changeProfilePhoto = 'Promijeni fotografiju';

  // --- Instructor home (4.3.1) ---
  static String instructorGreeting(String name) => 'Dobro jutro, $name';
  static const String todayCardTitle = 'Danas';
  static String lessonCountToday(int count) => count == 1 ? '1 zakazan čas' : '$count zakazanih časova';
  static const String viewScheduleAction = 'Pogledaj raspored';
  static String newRequestsBadge(int count) => count == 1 ? '1 novi zahtjev' : '$count novih zahtjeva';
  static const String menuLessonRequests = 'Zahtjevi za časove';
  static const String menuMySchedule = 'Moj raspored';
  static const String menuMyStudents = 'Moji učenici';
  static const String todaysLessonsTitle = 'Sljedeći časovi danas';
  static const String noLessonsToday = 'Nemate zakazanih časova danas.';
  static const String statCompletedThisMonth = 'Ovaj mjesec';
  static const String statCompletedTotal = 'Ukupno odrađeno';
  static const String statAverageRating = 'Prosječna ocjena';

  // --- Lesson requests (4.3.2) ---
  static const String lessonRequestsTitle = 'Zahtjevi za časove';
  static const String filterAll = 'Sve';
  static const String filterPending = 'Na čekanju';
  static const String filterConfirmed = 'Odobreni';
  static const String filterCancelled = 'Odbijeni';
  static const String rejectAction = 'Odbij';
  static const String approveAction = 'Odobri';
  static const String noLessonRequests = 'Nemate zahtjeva u ovoj kategoriji.';
  static const String fieldRejectionReason = 'Razlog odbijanja';
  static const String rejectDialogTitle = 'Odbijanje zahtjeva';
  static const String requestRejectedMessage = 'Zahtjev je odbijen.';
  static const String requestApprovedMessage = 'Čas je odobren.';

  // --- Request details (4.3.3) ---
  static const String requestDetailsTitle = 'Detalji zahtjeva';
  static String instructorLessonNumberLabel(int number) => '$number. čas';
  static const String infoLessonNumber = 'Broj časa';
  static const String studentNoteTitle = 'Napomena učenika:';
  static const String confirmLessonAction = 'Potvrdi čas';

  // --- Schedule (4.3.4) ---
  static const String scheduleTitle = 'Raspored';
  static const String scheduleEmptySlot = 'Slobodan termin';
  static const String startLessonAction = 'Počni čas';
  static const String finishLessonAction = 'Završi čas';
  static const String lessonStartedMessage = 'Čas je započet.';
  static const String noLessonsForDate = 'Nemate časova za odabrani dan.';
  static const List<String> dayAbbreviations = ['Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub', 'Ned'];

  // --- Lesson completion & grading (4.3.5) ---
  static const String completeLessonTitle = 'Završetak časa';
  static const String ratingLabel = 'Ocjena učenika';
  static const String ratingHint = 'Kliknite za ocjenu';
  static const String fieldInstructorNotes = 'Bilješke o času';
  static const String instructorNotesHint = 'Upišite bilješke o napretku učenika, greškama, itd...';
  static const String lessonCompletedMessage = 'Čas je završen.';
  static const String ratingRequired = 'Odaberite ocjenu od 1 do 5 zvjezdica.';
  static const String fieldLessonStatus = 'Status časa';
  static const String lessonCompletedStatusValue = 'Završen — Čas odrađen';

  // --- My students ---
  static const String myStudentsTitle = 'Moji učenici';
  static const String noStudents = 'Još nemate dodijeljenih učenika.';
  static String totalLessonsLabel(int count) => count == 1 ? '1 odrađen čas' : '$count odrađenih časova';

  // --- Notifications ---
  static const String notificationsEmpty = 'Nemate obavijesti.';
  static const String notificationsEmptyHint = 'Nove obavijesti će se pojaviti ovdje čim stignu.';
  static const String notificationsLoadFailed = 'Obavijesti nisu učitane';
  static const String markAllRead = 'Označi sve kao pročitano';
  static const String markAsRead = 'Označi kao pročitano';
  static const String markReadFailed = 'Obavijest nije označena kao pročitana.';
  static const String markAllReadFailed = 'Obavijesti nisu označene kao pročitane.';
  static String notificationsUnread(int count) => count == 1 ? '1 nepročitana obavijest' : '$count nepročitanih obavijesti';
  static String notificationsStale(String cause) => 'Lista možda nije najnovija. $cause';

  // --- States ---
  static const String loadFailedTitle = 'Podaci nisu učitani';
  static const String emptyTitle = 'Nema zapisa';
  static const String emptyMessage = 'Kada dodate prvi zapis, pojavit će se ovdje.';
  static String emptyForSearch(String query) => 'Nijedan zapis ne odgovara pretrazi „$query”.';

  // --- Paging ---
  static const String noResults = 'Nema rezultata';
  static const String previousPage = 'Prethodna';
  static const String nextPage = 'Sljedeća';
  static String showingRange(int first, int last, int total) => 'Prikazano $first–$last od $total';
  static String pageOf(int page, int pages) => 'Stranica $page / $pages';

  // --- Dialogs ---
  static const String confirmDeleteTitle = 'Potvrda brisanja';
  static String confirmDeleteMessage(String label) =>
      'Da li ste sigurni da želite obrisati „$label”? Ova radnja se ne može poništiti.';

  // --- Form controls ---
  static const String dropdownSelect = 'Odaberite…';
  static const String dropdownLoading = 'Učitavanje…';
  static const String dropdownEmpty = 'Nema dostupnih stavki.';
  static const String dropdownLoadFailed = 'Stavke nisu učitane. Pokušajte ponovo.';
  static const String pickDate = 'Odaberi datum';
  static const String pickTime = 'Odaberi vrijeme';
  static const String dateHint = 'DD.MM.GGGG.';
  static const String timeHint = 'HH:MM';
  static const String clearValue = 'Poništi vrijednost';
  static const String pickImage = 'Odaberi sliku';
  static const String changeImage = 'Promijeni sliku';
  static const String removeImage = 'Ukloni sliku';
  static const String imageReadFailed = 'Odabrana slika se ne može pročitati.';

  // --- Validation (shown under the control; each states the expected format) ---
  static const String required = 'Ovo polje je obavezno.';
  static const String invalidEmail = 'Unesite e-mail u formatu ime@domena.com.';
  static const String invalidPhone = 'Broj telefona mora biti u formatu +387 6X XXX XXX.';
  static const String passwordTooShort = 'Lozinka mora imati najmanje 8 znakova.';
  static const String passwordsDoNotMatch = 'Lozinka i potvrda lozinke se ne podudaraju.';
  static const String invalidNumber = 'Unesite broj, npr. 120.50.';
  static const String invalidWholeNumber = 'Unesite cijeli broj, npr. 2021.';
  static const String selectRequired = 'Odaberite jednu od ponuđenih vrijednosti.';
  static const String dateRequired = 'Odaberite datum u formatu DD.MM.GGGG.';
  static const String timeRequired = 'Odaberite vrijeme u formatu HH:MM.';
  static String maxLength(String label, int max) => '$label može imati najviše $max znakova.';
  static String numberAtLeast(num min) => 'Vrijednost ne može biti manja od ${_plain(min)}.';
  static String numberAtMost(num max) => 'Vrijednost ne može biti veća od ${_plain(max)}.';
  static String dateNotBefore(String formatted) => 'Datum ne može biti prije $formatted';
  static String dateNotAfter(String formatted) => 'Datum ne može biti poslije $formatted';
  static const String imageTypeInvalid = 'Dozvoljeni su samo fajlovi tipa JPG, PNG ili WEBP.';
  static String imageTooLarge(int megabytes) => 'Fajl je prevelik. Maksimalna dozvoljena veličina je $megabytes MB.';

  // --- Lesson status ---
  static const String lessonPending = 'Na čekanju';
  static const String lessonConfirmed = 'Potvrđen';
  static const String lessonCompleted = 'Završen';
  static const String lessonCancelled = 'Otkazan';

  // --- Errors ---
  static const String requestTimeout = 'Server ne odgovara. Provjerite internet konekciju i pokušajte ponovo.';
  static const String serverUnreachable = 'Nije moguće povezati se sa serverom. Provjerite internet konekciju.';
  static const String sessionExpired = 'Vaša prijava je istekla. Prijavite se ponovo da nastavite.';
  static const String serverFailure = 'Došlo je do greške na serveru. Pokušajte ponovo za nekoliko trenutaka.';
  static const String cannotReachServer =
      'Ne možemo se povezati sa serverom. Provjerite da li je servis pokrenut, pa pokušajte ponovo.';
  static String requestRejected(int statusCode) => 'Greška prilikom komunikacije sa serverom ($statusCode).';

  // 0 rather than 0.0 when a whole number is passed as a double.
  static String _plain(num value) => value == value.roundToDouble() ? '${value.toInt()}' : '$value';
}
