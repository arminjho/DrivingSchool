# eAutoškola - mobilna aplikacija (Učenik i Instruktor)

Flutter aplikacija za Android: jedna aplikacija, a izbornik se prilagođava ulozi iz JWT tokena.
Opis cijelog sistema i podaci za prijavu nalaze se u [README.md](../../README.md) u korijenu
repozitorija.

```bash
flutter pub get
flutter analyze
flutter test
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

`10.0.2.2` je adresa računara na kojem se pokreće Android emulator. Adresa API-ja se čita isključivo
preko `--dart-define=API_BASE_URL=...` (`lib/core/api_config.dart`); svi tekstovi na bosanskom su u
`lib/l10n/app_strings.dart`.
