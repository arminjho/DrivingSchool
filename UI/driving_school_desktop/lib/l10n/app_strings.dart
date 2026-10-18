/// Every user-facing string of the desktop app, in Bosnian, in one place - so adding another
/// language later means replacing this file, not hunting through widgets.
abstract final class AppStrings {
  // --- App ---
  static const String appName = 'eAutoškola';
  static const String adminPanel = 'Admin Panel';

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
  static const String loginSubtitle = 'Administratorski panel autoškole';
  static const String loginAction = 'Prijavi se';
  static const String usernameLabel = 'Korisničko ime ili e-mail';
  static const String passwordLabel = 'Lozinka';
  static const String showPassword = 'Prikaži lozinku';
  static const String hidePassword = 'Sakrij lozinku';
  static const String roleNotAllowed = 'Desktop aplikacija je namijenjena administratorima autoškole.';
  static const String loginFailed = 'Prijava nije uspjela. Provjerite korisničko ime i lozinku.';

  // --- Shell ---
  static const String homeTitle = 'Dashboard';
  static const String homeSubtitle = 'Pregled aktivnosti — eAutoškola';
  static const String notificationsTitle = 'Obavijesti';
  static const String notificationsSubtitle = 'Sistemske obavijesti o vašem nalogu';
  static const String toggleSidebar = 'Sakrij ili prikaži meni';
  static const String breadcrumbRoot = 'eAutoškola';
  static const String myProfile = 'Moj profil';
  static const String changePassword = 'Promijeni lozinku';
  static String welcome(String name) => 'Dobrodošli, $name';

  // --- Sidebar ---
  static const String navDashboard = 'Dashboard';
  static const String navUsers = 'Korisnici';
  static const String navVehicleFleet = 'Vozni park';
  static const String navReports = 'Izvještaji';
  static const String navReferenceData = 'Referentni podaci';
  static const String navTests = 'Testovi';
  static const String navStudyLessons = 'Lekcije';
  static const String navLessons = 'Časovi';

  // --- Dashboard ---
  static const String dashboardTotalStudents = 'Ukupno učenika';
  static String dashboardNewThisMonth(int count) => '+$count ovaj mjesec';
  static const String dashboardInstructors = 'Instruktori';
  static const String dashboardAllActive = 'Svi aktivni';
  static const String dashboardVehicles = 'Vozila';
  static String dashboardInService(int count) => '$count na servisu';
  static const String dashboardLessonsThisMonth = 'Časovi ovaj mj.';
  static const String dashboardVsLastMonth = 'u odnosu na prošli mjesec';
  static const String dashboardLessonsPerMonth = 'Časovi po mjesecima';
  static String dashboardLessonsPerMonthSubtitle(int year) => 'Broj realizovanih časova $year';
  static const String dashboardStatusChart = 'Status časova';
  static const String dashboardStatusChartSubtitle = 'Raspodjela po statusima';
  static const String dashboardRecentActivity = 'Nedavne aktivnosti';
  static const String dashboardRecentActivityEmpty = 'Nema nedavnih aktivnosti.';
  static const String dashboardTodayLessons = 'Predstojeći časovi danas';
  static const String dashboardTodayLessonsEmpty = 'Danas nema zakazanih časova.';
  static const String dashboardLoadFailed = 'Dashboard nije učitan';

  // --- Users ---
  static const String usersTitle = 'Korisnici';
  static const String usersSubtitle = 'Upravljanje korisničkim nalozima';
  static const String usersSearchHint = 'Pretraži korisnike…';
  static const String usersAllRoles = 'Sve uloge';
  static const String usersAddNew = '+ Novi korisnik';
  static const String usersColumnName = 'Ime i prezime';
  static const String usersColumnEmail = 'Email';
  static const String usersColumnRole = 'Uloga';
  static const String usersColumnStatus = 'Status';
  static const String usersStatusActive = 'Aktivan';
  static const String usersStatusInactive = 'Neaktivan';
  static const String userFormNewTitle = 'Novi korisnik';
  static const String userFormEditTitle = 'Uredi korisnika';
  static const String fieldFirstName = 'Ime';
  static const String fieldLastName = 'Prezime';
  static const String fieldEmail = 'Email';
  static const String fieldPhone = 'Broj telefona';
  static const String fieldAddress = 'Adresa';
  static const String fieldCity = 'Grad';
  static const String fieldRole = 'Uloga';
  static const String fieldVehicleCategory = 'Kategorija vozila';
  static const String fieldPassword = 'Lozinka';
  static const String fieldConfirmPassword = 'Potvrda lozinke';
  static const String changePasswordToggle = 'Izmijeni lozinku';
  static const String fieldNewPassword = 'Nova lozinka';
  static String userCreated(String name) => 'Korisnik $name je dodan.';
  static String userUpdated(String name) => 'Korisnik $name je sačuvan.';
  static String userDeleted(String name) => 'Korisnik $name je obrisan.';
  static String userActivated(String name) => 'Korisnik $name je aktiviran.';
  static String userDeactivated(String name) => 'Korisnik $name je deaktiviran.';
  static const String activate = 'Aktiviraj';
  static const String deactivate = 'Deaktiviraj';

  // --- Vehicle fleet ---
  static const String vehicleFleetTitle = 'Vozni park';
  static const String vehicleFleetSubtitle = 'Pregled vozila i servisnih zapisa';
  static const String vehiclesTab = 'Vozila';
  static const String vehicleServicesTab = 'Servisi';
  static const String vehiclesAddNew = '+ Novo vozilo';
  static const String vehicleServicesAddNew = '+ Novi servis';
  static const String vehiclesSearchHint = 'Pretraži po registraciji…';
  static const String vehiclesAllCategories = 'Sve kategorije';
  static const String columnRegistration = 'Registracija';
  static const String columnMakeModel = 'Marka/Model';
  static const String columnCategory = 'Kategorija';
  static const String columnYear = 'Godina';
  static const String columnStatus = 'Status';
  static const String columnVehicle = 'Vozilo';
  static const String columnDate = 'Datum';
  static const String columnServiceType = 'Vrsta servisa';
  static const String columnDescription = 'Opis';
  static const String columnCost = 'Cijena';
  static const String vehicleStatusActive = 'Aktivan';
  static const String vehicleStatusInService = 'Na servisu';
  static const String vehicleServiceStatusScheduled = 'Zakazan';
  static const String vehicleServiceStatusInProgress = 'U toku';
  static const String vehicleServiceStatusCompleted = 'Završen';
  static const String vehicleFormNewTitle = 'Novo vozilo';
  static const String vehicleFormEditTitle = 'Uredi vozilo';
  static const String fieldRegistrationNumber = 'Registarska oznaka';
  static const String fieldMake = 'Marka';
  static const String fieldModel = 'Model';
  static const String fieldManufactureYear = 'Godina proizvodnje';
  static const String vehicleServiceFormNewTitle = 'Novi servis';
  static const String vehicleServiceFormEditTitle = 'Uredi servis';
  static const String fieldServiceDate = 'Datum servisa';
  static const String fieldServiceType = 'Vrsta servisa';
  static const String fieldCost = 'Cijena';
  static const String fieldServiceStatus = 'Status servisa';
  static String vehicleCreated(String registration) => 'Vozilo $registration je dodano.';
  static String vehicleUpdated(String registration) => 'Vozilo $registration je sačuvano.';
  static String vehicleDeleted(String registration) => 'Vozilo $registration je obrisano.';
  static const String vehicleServiceCreated = 'Servis je dodan.';
  static const String vehicleServiceUpdated = 'Servis je sačuvan.';
  static const String vehicleServiceDeleted = 'Servis je obrisan.';
  static const String noVehicleCategoryPrefix = 'Prvo dodajte kategoriju vozila u Referentni podaci.';
  static const String noVehiclesPrefix = 'Prvo dodajte vozilo u Vozni park.';

  // --- Reports ---
  static const String reportsTitle = 'Izvještaji';
  static const String reportsSubtitle = 'Generisanje i preuzimanje izvještaja';
  static const String lessonStatusReportTitle = 'Izvještaj o statusu časova';
  static const String lessonStatusReportSubtitle = 'Generiši izvještaj prema filteru';
  static const String vehicleServiceReportTitle = 'Izvještaj o vozilima';
  static const String vehicleServiceReportSubtitle = 'Pregled stanja voznog parka';
  static const String fieldDateFrom = 'Datum od';
  static const String fieldDateTo = 'Datum do';
  static const String fieldInstructor = 'Instruktor';
  static const String allInstructors = 'Svi instruktori';
  static const String downloadPdf = '⬇ Preuzmi PDF';
  static const String printPdf = 'Štampaj';
  static const String reportGenerating = 'Generisanje izvještaja…';
  static const String reportGenerateFailed = 'Izvještaj nije generisan.';
  static const String reportDateRangeInvalid = 'Datum "do" mora biti isti ili nakon datuma "od".';
  static const String reportCompleted = 'Završeno';
  static const String reportScheduled = 'Zakazano';
  static const String reportCancelledCount = 'Otkazano';

  // --- Reference data ---
  static const String referenceDataTitle = 'Referentni podaci';
  static const String referenceDataSubtitle = 'Šifarnici i katalozi';
  static const String tabCountries = 'Države';
  static const String tabCities = 'Gradovi';
  static const String tabVehicleCategories = 'Kategorije vozila';
  static const String tabRequestStatuses = 'Statusi zahtjeva';
  static const String tabServiceTypes = 'Tipovi usluga';
  static const String tabDrivingZones = 'Zone vožnje';
  static const String tabQuestionAreas = 'Oblasti pitanja';
  static const String fieldCode = 'Šifra';
  static const String fieldName = 'Naziv';
  static const String fieldDescription = 'Opis';
  static const String fieldPostalCode = 'Poštanski broj';
  static const String fieldCountry = 'Država';
  static const String fieldPrice = 'Cijena';
  static const String addCountry = '+ Dodaj državu';
  static const String addCity = '+ Dodaj grad';
  static const String addVehicleCategory = '+ Dodaj kategoriju';
  static const String addRequestStatus = '+ Dodaj status';
  static const String addServiceType = '+ Dodaj tip usluge';
  static const String addDrivingZone = '+ Dodaj zonu';
  static const String addQuestionArea = '+ Dodaj oblast';
  static String entryCreated(String label) => '„$label” je dodano.';
  static String entryUpdated(String label) => '„$label” je sačuvano.';
  static String entryDeleted(String label) => '„$label” je obrisano.';

  // --- Tests / questions (admin) ---
  static const String testsTitle = 'Testovi';
  static const String testsSubtitle = 'Upravljanje probnim testovima i pitanjima';
  static const String testsAddNew = '+ Novi test';
  static const String columnTitle = 'Naziv';
  static const String columnQuestionArea = 'Oblast';
  static const String columnQuestionCount = 'Broj pitanja';
  static const String columnPassThreshold = 'Prag za prolaz';
  static const String testFormNewTitle = 'Novi test';
  static const String testFormEditTitle = 'Uredi test';
  static const String fieldTestTitle = 'Naziv testa';
  static const String fieldQuestionAreaId = 'Oblast pitanja';
  static const String fieldPassThreshold = 'Prag za prolaz (%)';
  static const String openQuestions = 'Pitanja';
  static const String questionsTitle = 'Pitanja';
  static String questionsSubtitle(String testTitle) => 'Pitanja za test „$testTitle”';
  static const String questionsAddNew = '+ Novo pitanje';
  static const String questionFormNewTitle = 'Novo pitanje';
  static const String questionFormEditTitle = 'Uredi pitanje';
  static const String fieldQuestionText = 'Tekst pitanja';
  static const String fieldQuestionImage = 'Slika (opciono)';
  static const String answerOptionsTitle = 'Ponuđeni odgovori (odaberite točan)';
  static String answerOptionLabel(int index) => 'Odgovor ${String.fromCharCode(65 + index)}';
  static const String answerOptionsMustHaveFour = 'Potrebne su tačno 4 ponuđene opcije.';
  static const String answerOptionsNeedOneCorrect = 'Odaberite tačno jedan točan odgovor.';
  static String testCreated(String title) => 'Test „$title” je dodan.';
  static String testUpdated(String title) => 'Test „$title” je sačuvan.';
  static String testDeleted(String title) => 'Test „$title” je obrisan.';
  static const String questionCreated = 'Pitanje je dodano.';
  static const String questionUpdated = 'Pitanje je sačuvano.';
  static const String questionDeleted = 'Pitanje je obrisano.';

  // --- Study lessons (admin) ---
  static const String studyLessonsTitle = 'Lekcije';
  static const String studyLessonsSubtitle = 'Upravljanje teorijskim lekcijama i sekcijama';
  static const String studyLessonsAddNew = '+ Nova lekcija';
  static const String columnChapter = 'Poglavlje';
  static const String columnReadingMinutes = 'Minuta čitanja';
  static const String columnSectionCount = 'Broj sekcija';
  static const String studyLessonFormNewTitle = 'Nova lekcija';
  static const String studyLessonFormEditTitle = 'Uredi lekciju';
  static const String fieldLessonTitle = 'Naziv lekcije';
  static const String fieldTags = 'Tagovi';
  static const String fieldValidForYear = 'Važi za godinu';
  static const String fieldReadingMinutes = 'Minuta čitanja';
  static const String fieldChapterLabel = 'Oznaka poglavlja';
  static const String openSections = 'Sekcije';
  static const String sectionsTitle = 'Sekcije';
  static String sectionsSubtitle(String lessonTitle) => 'Sekcije lekcije „$lessonTitle”';
  static const String sectionsAddNew = '+ Nova sekcija';
  static const String sectionFormNewTitle = 'Nova sekcija';
  static const String sectionFormEditTitle = 'Uredi sekciju';
  static const String fieldSectionTitle = 'Naziv sekcije';
  static const String fieldContent = 'Sadržaj';
  static const String fieldSortOrder = 'Redoslijed';
  static String studyLessonCreated(String title) => 'Lekcija „$title” je dodana.';
  static String studyLessonUpdated(String title) => 'Lekcija „$title” je sačuvana.';
  static String studyLessonDeleted(String title) => 'Lekcija „$title” je obrisana.';
  static const String sectionCreated = 'Sekcija je dodana.';
  static const String sectionUpdated = 'Sekcija je sačuvana.';
  static const String sectionDeleted = 'Sekcija je obrisana.';

  // --- Lessons overview (admin) ---
  static const String lessonsOverviewTitle = 'Časovi';
  static const String lessonsOverviewSubtitle = 'Pregled svih časova vožnje';
  static const String lessonsAllStatuses = 'Svi statusi';
  static const String columnStudent = 'Učenik';
  static const String columnZone = 'Zona';
  static const String columnDateTime = 'Datum i vrijeme';
  static const String cancelLessonTitle = 'Otkazivanje časa';
  static const String cancelLessonReasonLabel = 'Razlog otkazivanja';
  static const String cancelLessonReasonHint = 'Razlog se šalje učeniku i instruktoru uz obavijest o otkazivanju.';
  static const String cancelLessonAction = 'Otkaži čas';
  static const String lessonCancelledMessage = 'Čas je otkazan.';
  static const String cannotCancelCompleted = 'Završen čas se ne može otkazati.';
  static const String cannotCancelAlready = 'Čas je već otkazan.';

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
