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
  static const String homeTitle = 'Početna';
  static const String homeSubtitle = 'Pregled aktivnosti — eAutoškola';
  static const String notificationsTitle = 'Obavijesti';
  static const String notificationsSubtitle = 'Sistemske obavijesti o vašem nalogu';
  static const String toggleSidebar = 'Sakrij ili prikaži meni';
  static String welcome(String name) => 'Dobrodošli, $name';

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
