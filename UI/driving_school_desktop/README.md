# eAutoškola - desktop aplikacija (Administrator)

Flutter aplikacija za Windows: dashboard, korisnici, vozni park, izvještaji (PDF), referentni podaci,
testovi i lekcije. Opis cijelog sistema i podaci za prijavu nalaze se u [README.md](../../README.md)
u korijenu repozitorija.

```bash
flutter pub get
flutter analyze
flutter test
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5000
```

Adresa API-ja se čita isključivo preko `--dart-define=API_BASE_URL=...`
(`lib/core/api_config.dart`); svi tekstovi na bosanskom su u `lib/l10n/app_strings.dart`.
