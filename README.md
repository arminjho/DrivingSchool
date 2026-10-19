# eAutoškola

Sistem za upravljanje autoškolom (seminarski rad, Razvoj softvera II, FIT Mostar; student: Armin Muratspahić, IB210044). Tri uloge: **Administrator** (desktop aplikacija), **Učenik** (mobilna aplikacija) i **Instruktor** (ista mobilna aplikacija, izbornik se prilagođava ulozi).

## Sadržaj

- [Arhitektura](#arhitektura)
- [Korisnički podaci za pristup](#korisnički-podaci-za-pristup)
- [Pokretanje](#pokretanje)
- [Desktop i mobilna aplikacija iz Release arhive](#desktop-i-mobilna-aplikacija-iz-release-arhive)
- [Flutter aplikacije iz izvornog koda](#flutter-aplikacije-iz-izvornog-koda)
- [Nazivi entiteta](#nazivi-entiteta)
- [Dashboard i izvještaji](#dashboard-i-izvještaji)
- [Plaćanje i povrat novca](#plaćanje-i-povrat-novca)
- [Provjera plaćanja sa PayPal sandbox kupcem](#provjera-plaćanja-sa-paypal-sandbox-kupcem)
- [Testovi](#testovi)
- [Rješavanje problema](#rješavanje-problema)
- [Dodatna dokumentacija](#dodatna-dokumentacija)

## Arhitektura

| Projekat | Uloga |
|---|---|
| `eAutoskola.Domain` | Entiteti, enumi, izuzeci, interfejsi (npr. `IPayPalClient`, `IMessagePublisher`), centralizirani tekstovi poruka (`Resources/Messages.cs`) |
| `eAutoskola.Application` | Servisi (Controller → Service → DbContext), CQRS podjela gdje postoji stvarna kompleksnost (časovi), recommender |
| `eAutoskola.Infrastructure` | EF Core + SQL Server, migracije i seed, JWT/BCrypt, RabbitMQ publisher, PayPal klijent, upload fajlova |
| `eAutoskola.API` | ASP.NET Core Web API, SignalR (notifikacije u realnom vremenu) |
| `eAutoskola.Worker` | Poseban kontejner: čita RabbitMQ redove i šalje e-mailove (SMTP) |
| `eAutoskola.Contracts` | Poruke koje dijele API i Worker |
| `eAutoskola.Tests` | xUnit testovi |
| `UI/driving_school_desktop` | Flutter aplikacija za Windows (Administrator) |
| `UI/driving_school_mobile` | Flutter aplikacija za Android (Učenik i Instruktor) |

Docker servisi (`docker-compose.yml`): SQL Server `2022` (baza `210044`), RabbitMQ `4.0`, MailHog `v1.0.1` (lokalni SMTP hvatač), API i Worker.

## Korisnički podaci za pristup

Lozinka za sve račune je **`test`**.

| Kontekst | Korisničko ime | Lozinka |
|---|---|---|
| Desktop verzija | `desktop` | `test` |
| Mobilna verzija | `mobile` | `test` |
| Više korisničkih uloga (Instruktor) | `instruktor` | `test` |

`desktop` je Administrator, `mobile` je Učenik, `instruktor` je Instruktor (prijava mobilnom aplikacijom). Desktop aplikacija pušta samo Administratora, mobilna samo Učenika i Instruktora.

Dodatni seed računi (svi sa lozinkom `test`, prijava e-mailom):

| Korisničko ime | Uloga | Napomena |
|---|---|---|
| `mirela.tahic@eautoskola.ba` | Administrator | |
| `adnan.zukic@eautoskola.ba` | Učenik | Već je iskoristio svih 30 časova iz paketa. Ima nadolazeći **neplaćen** dodatni čas i historijski plaćen pa vraćen čas - koristi se za provjeru plaćanja i povrata. |
| `amina.hodzic@eautoskola.ba`, `emir.mustafic@eautoskola.ba` | Učenik | Časovi u različitim statusima, riješeni testovi |
| `lejla.basic@eautoskola.ba` | Učenik | Deaktiviran nalog (ne može se prijaviti) - prikazuje status „Neaktivan“ |
| `damir.kovacevic@eautoskola.ba`, `sanja.peric@eautoskola.ba`, `selma.omerovic@eautoskola.ba` | Instruktor | Kategorija B |
| `haris.delic@eautoskola.ba`, `tarik.hadzic@eautoskola.ba` | Instruktor | Kategorije C i A |

Baza se pri prvom pokretanju popunjava podacima za testiranje: 32 korisnika (2 administratora, 6 instruktora, 24 učenika), 16 vozila sa servisnom historijom, oko 240 časova u svim statusima raspoređenih kroz godinu, paketi sa ratama i historijom uplata, 6 testova sa po 10 pitanja, 42 riješena testa, 12 lekcija i obavijesti. Korisnici, vozila i pitanja imaju slike.

## Pokretanje

**Preduvjeti:** Windows 10/11 x64, [Docker Desktop](https://www.docker.com/products/docker-desktop/) (pokrenut), [7-Zip](https://www.7-zip.org/) ili drugi alat koji otvara ZIP sa šifrom, Android Studio sa Android emulatorom (AVD) za mobilnu aplikaciju i [Microsoft Visual C++ Redistributable (x64)](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) za Windows aplikaciju (na računaru na kojem je instaliran Visual Studio već postoji). Za pokretanje iz izvornog koda i testova dodatno: .NET 10 SDK i Flutter.

1. **Konfiguracija (`.env`).** Tajne vrijednosti nisu u repozitoriju u čistom tekstu: u korijenu repozitorija (isti folder kao `docker-compose.yml`) nalazi se šifrirana arhiva **`.env-tajne.zip`**. Raspakujte je u isti folder, **šifra je `fit`** - raspakivanje mora dati fajl `.env` tačno pored `.env-tajne.zip`:

   ```bash
   7z x .env-tajne.zip -pfit
   ```

   (ili desni klik na arhivu → raspakuj, pa unesite šifru `fit`). Fajl `.env` je u `.gitignore` i ne smije se commitati. Bez raspakivanja `docker compose` ne može da se pokrene. Spisak svih ključeva sa objašnjenjima je u `.env.example`.

2. **Pokrenite sve servise** (baza, RabbitMQ, MailHog, API, Worker):

   ```bash
   docker compose up -d --build
   ```

   Pri prvom pokretanju API sam primjenjuje migracije i popunjava bazu početnim podacima (seed); to traje oko minut. Stanje provjerite sa `docker compose ps` (svi servisi `running`/`healthy`). Za potpuno čisto stanje: `docker compose down -v` pa ponovo `up`. Ako RabbitMQ još nije spreman kada se Worker podigne, Worker ponavlja povezivanje (razmak raste do 30 s) dok se ne poveže; ne pada i ne restartuje se.

| Servis | Adresa |
|---|---|
| API | http://localhost:5000 (bez HTTPS-a, namjerno) |
| Swagger | http://localhost:5000/swagger - samo kada je u `.env` postavljeno `ASPNETCORE_ENVIRONMENT=Development` (zatim `docker compose up -d api`) |
| MailHog (primljeni e-mailovi) | http://localhost:8025 |
| RabbitMQ management | http://localhost:15672 (korisnik/lozinka iz `.env`) |
| SQL Server (npr. SSMS) | `localhost,14333`, korisnik `sa`, lozinka `DB_SA_PASSWORD` iz `.env` (host port 14333 je odabran jer lokalni SQL Server obično zauzima 1433) |

## Desktop i mobilna aplikacija iz Release arhive

Build za pregled nalazi se u GitHub Release-u, u arhivi `fit-build-GGGG-MM-DD.zip` (nema `.env` fajla). Arhiva sadrži dvije stvari u putanjama iz upute: `UI/driving_school_mobile/build/app/outputs/flutter-apk/app-release.apk` i folder `UI/driving_school_desktop/build/windows/x64/runner/Release/`.

- **Windows (Administrator):** raspakujte arhivu i pokrenite `UI/driving_school_desktop/build/windows/x64/runner/Release/driving_school_desktop.exe` (cijeli `Release` folder mora ostati zajedno). Aplikacija se povezuje na `http://localhost:5000`, pa Docker servisi moraju biti pokrenuti. Prijava: `desktop` / `test`.
- **Android (Učenik, Instruktor):** pokrenite emulator, obrišite staru verziju aplikacije, prevucite `app-release.apk` na prozor emulatora (ili `adb install -r app-release.apk`) i pokrenite „eAutoškola“. Aplikacija se povezuje na `http://10.0.2.2:5000` (standardna adresa računara iz Android emulatora). Prijava: `mobile` / `test` ili `instruktor` / `test`.

## Flutter aplikacije iz izvornog koda

Adresa API-ja se čita isključivo preko `--dart-define=API_BASE_URL=...` (`String.fromEnvironment('API_BASE_URL')` u `lib/core/api_config.dart`); vrijednosti su zapisane u `.env` (`API_BASE_URL_DESKTOP`, `API_BASE_URL_MOBILE`), odakle ih čita `scripts/build-release.ps1`.

| Aplikacija | `API_BASE_URL` |
|---|---|
| Desktop (Windows) | `http://localhost:5000` |
| Mobilna (Android emulator) | `http://10.0.2.2:5000` |

```bash
cd UI/driving_school_desktop   # ili UI/driving_school_mobile
flutter pub get
flutter analyze
flutter test
```

Pokretanje:

```bash
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000     # desktop
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000                 # mobilna, Android emulator
```

Build za predaju (čisti stari build, pa Android APK i Windows `Release` folder, oboje upakovano u `fit-build-GGGG-MM-DD.zip` van repozitorija):

```powershell
./scripts/build-release.ps1
```

što je ekvivalentno sa `flutter clean` + `flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5000` u `UI/driving_school_mobile` i `flutter clean` + `flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5000` u `UI/driving_school_desktop`. Pokretanje Windows builda zahtijeva uključen Developer Mode (`start ms-settings:developers`) zbog symlink-ova koje koriste pluginovi.

## Nazivi entiteta

Programski kod i baza podataka su u potpunosti na engleskom (klase, tabele, kolone, rute, poruke) - u skladu sa upute.md §3.4/CLAUDE.md §3; na bosanskom je samo tekst koji korisnik stvarno vidi (UI, poruke, PDF, e-mail), centralizovan u `lib/l10n/app_strings.dart` (oba Flutter projekta), `eAutoskola.Domain/Resources/Messages.cs`, `eAutoskola.Application/Reports/ReportTexts.cs` (PDF) i `eAutoskola.Worker/Templates/EmailTemplates.cs` (e-mail). Ispod je mapiranje nekoliko ključnih naziva iz odobrene prijave na njihove engleske nazive u kodu:

| Naziv iz prijave (Bosanski) | Naziv u kodu |
|---|---|
| Čas (vožnje) | `Lesson` |
| Vozilo | `Vehicle` |
| Rezultat testa / Pokušaj testa | `TestAttempt` |
| Pitanje | `Question` |
| Lekcija | `StudyLesson` |
| Rata | `Installment` |
| Uplata | `Payment` |
| Povrat | `Refund` |

Kompletan rječnik (uloge, referentni podaci, notifikacije, itd.) je u `CLAUDE.md` §3.

## Dashboard i izvještaji

Samo za ulogu **Admin** (`desktop` / `test`).

| Endpoint | Opis |
|---|---|
| `GET /api/dashboard` | Učenici, instruktori, vozila (aktivna / na servisu), časovi ovog mjeseca sa promjenom u odnosu na prošli, broj završenih časova po mjesecima tekuće godine, raspodjela po statusima, nedavne aktivnosti i današnji predstojeći časovi. Rezultat se kratko (30 s) kešira. |
| `GET /api/reports/lesson-status/pdf` | PDF „Izvještaj o statusu časova vožnje“. Filteri: `dateFrom`, `dateTo` (`yyyy-MM-dd`), `instructorId`. Broj odrađenih, zakazanih (na čekanju + potvrđeni) i otkazanih časova po instruktoru, sa grafikonom i tabelom. |
| `GET /api/reports/vehicle-services/pdf` | PDF „Izvještaj o vozilima i servisima“. Filteri: `vehicleCategoryId`, `dateFrom`, `dateTo`. Servisna historija i troškovi po vozilu i po kategoriji. |
| `GET /api/reports/lesson-status`, `GET /api/reports/vehicle-services` | Isti podaci kao JSON (sa istim filterima), da desktop aplikacija može nacrtati grafikon pored PDF-a. |

Svi filteri su opcionalni; izostavljen datum ostavlja kraj perioda otvoren, a izostavljen `instructorId` / `vehicleCategoryId` znači „svi“. PDF se generiše na serveru (QuestPDF), a svaka stranica ima naziv škole, vrijeme generisanja (UTC) i primijenjene filtere. Neispravan filter (npr. datum „do“ prije datuma „od“) vraća `400` sa porukom po polju. Desktop aplikacija preuzima PDF i omogućava njegovo štampanje.

## Plaćanje i povrat novca

- **Paket obuke** se sastoji od tri stavke (Teorijska obuka 200 KM, Praktična vožnja 1200 KM, Administracija i ispit 150 KM). Svaka se plaća u 3 rate. Zaduženja se kreiraju zajedno sa učenikom (samostalna registracija, administratorov „Novi korisnik“ i seed), u istoj transakciji kao korisnik i profil; `GET /api/finance/my` samo čita i ništa ne kreira.
- **Dodatni časovi:** paket uključuje **30** praktičnih časova. Svaki potvrđen čas preko te kvote dobija vlastito zaduženje („Dodatni čas vožnje“, 40 KM, jedna rata) u istoj transakciji u kojoj se čas potvrđuje.
- **Valuta:** sve se čuva i prikazuje u **KM**. PayPal ne podržava BAM, pa se iznos pretvara u EUR (fiksni kurs 1 EUR = 1.95583 KM, zaokruženo na 2 decimale) samo u trenutku poziva PayPal-a. Za povrat se koristi iznos koji je PayPal stvarno naplatio (`CapturedAmount`). Adresa PayPal API-ja (`PAYPAL_BASE_URL`) i valuta (`PAYPAL_CURRENCY`) su u `.env`.
- **Finalizacija plaćanja** je isključivo serverska: `POST /api/payments/{orderId}/capture` pita PayPal da li je kupac odobrio i naplatio uplatu. Ništa što pošalje klijent ne važi kao dokaz uplate. Webhook endpoint ne postoji. Ponovni poziv za istu narudžbu je siguran (vraća `alreadyProcessed: true`, bez ponovnih nuspojava).
- **Napuštene narudžbe:** ako za ratu postoji narudžba mlađa od 30 minuta koja nije plaćena, ista narudžba (isti `orderId` i `approvalUrl`) se vraća umjesto kreiranja nove; starija se označava kao neuspjela. Rata koja je već plaćena ne može se platiti ponovo.
- **Otkazivanje plaćenog časa = povrat novca** (nema posebnog endpointa; ide kroz `POST /api/lessons/{id}/cancel`):
  - **Učenik** može otkazati plaćeni čas uz povrat najkasnije **24 sata** prije termina; kasnije je otkazivanje blokirano.
  - **Instruktor i Administrator** mogu uvijek otkazati plaćeni čas i povrat se uvijek izvršava.
  - Povrat se prvo izvršava kod PayPal-a; tek ako uspije, čas se otkazuje i stanje se mijenja (`Uplata → Povrat`, `Rata → Otkazana`, `Zaduženje → Otkazano`). Ako PayPal odbije povrat, čas ostaje potvrđen, uplata ostaje evidentirana, a korisnik dobija poruku da pokuša ponovo ili kontaktira administraciju. Neuspjeli pokušaj se čuva u tabeli povrata.
  - Neplaćen čas se pri otkazivanju samo otkazuje zajedno sa zaduženjem (ništa se ne briše).
  - Otkazana/vraćena zaduženja ne ulaze u „Preostalo za platiti“ ni u pregled troškova; povrat se u historiji prikazuje kao posebna stavka „Povrat“.
- Obavijesti: pri uspješnoj uplati („Uplata primljena“) i pri povratu („Povrat novca“) korisnik dobija notifikaciju u aplikaciji i e-mail (vidljiv u MailHog-u).

## Provjera plaćanja sa PayPal sandbox kupcem

Seed podaci sadrže samo historijske uplate (njihovi PayPal identifikatori počinju sa `SEED-`), pa se pravi tok (uplata → povrat) provjerava ručno. Client ID i Secret iz `.env-tajne.zip` pripadaju PayPal sandbox aplikaciji.

1. Na https://developer.paypal.com/dashboard/applications/sandbox može se napraviti i vlastita sandbox aplikacija; njen Client ID / Secret upisuju se u `.env` (`PAYPAL_CLIENT_ID`, `PAYPAL_CLIENT_SECRET`), zatim `docker compose up -d --build api`.
2. U PayPal Developer Dashboard-u (Testing Tools → Sandbox Accounts) koristite **Personal (kupac)** sandbox račun. Njegove podatke ne commitajte: `__REPLACE_ME_PAYPAL_SANDBOX_BUYER__` (e-mail i lozinka sandbox kupca).
3. Prijavite se kao `adnan.zukic@eautoskola.ba` / `test` i pozovite `GET /api/finance/my`. U `nextInstallment` je rata dodatnog časa (40 KM); u `costBreakdown` stavka sa `lessonId` označava taj čas. Zapamtite `installmentId` i `lessonId`. (U mobilnoj aplikaciji isto se radi preko „Finansije i plaćanje“ → „Plati sljedeću ratu“.)
4. `POST /api/payments/installments/{installmentId}/create-order` vraća `orderId` i `approvalUrl`. Otvorite `approvalUrl` u pregledniku, prijavite se sandbox kupcem i odobrite uplatu. (U mobilnoj aplikaciji ovo presreće WebView na adresama `PAYPAL_RETURN_URL` / `PAYPAL_CANCEL_URL`; u pregledniku redirect na te adrese ne mora ništa prikazati.)
5. `POST /api/payments/{orderId}/capture` → `status: "Completed"`. Provjerite: `GET /api/finance/my` (rata plaćena, preostali iznos smanjen, stavka u historiji), notifikaciju „Uplata primljena“ i e-mail u MailHog-u.
6. Prijavite se kao `desktop` / `test` (Administrator) i pozovite `POST /api/lessons/{lessonId}/cancel` sa tijelom `{ "reason": "Test povrata" }`. Povrat se izvršava stvarnim pozivom PayPal sandbox-a.
7. Ponovo `GET /api/finance/my` kao Adnan: zaduženje je nestalo iz pregleda troškova, preostali iznos je smanjen za 40 KM, a historija ima stavku „Povrat“. Transakcija je označena kao vraćena i u PayPal sandbox dashboard-u; e-mail „Potvrda povrata sredstava“ je u MailHog-u.

Napomena: kao Adnan (učenik) isti čas ne možete otkazati uz povrat ako je do termina manje od 24 sata. Bez Swagger-a (`Production`) pozivi iz koraka 3-7 mogu se slati iz bilo kojeg HTTP klijenta sa `Authorization: Bearer <token>` (token vraća `POST /api/auth/login`).

## Testovi

```bash
dotnet test eAutoskola.slnx
```

184 testova: preporuka lekcija (vektori, kosinusna sličnost, objašnjenja), dashboard i izvještaji (agregacije, filteri, PDF), pravila plaćanja i tok narudžbe / capture-a / povrata nad EF InMemory bazom sa mockiranim PayPal klijentom, stanja časova (svaki prelaz, audit trag, obavijesti u jednom snimanju), predaja testova, pristup fajlovima, veza servis-vozilo, poništavanje tokena i provjere seed podataka (količina, kalendar bez preklapanja, slike). Flutter testovi: `flutter test` u oba projekta.

## Rješavanje problema

- **`docker compose` javlja da `.env` ne postoji / vrijednost nije postavljena:** `.env-tajne.zip` nije raspakovan u korijen repozitorija (korak 1).
- **Zauzet port:** servisi koriste 5000 (API), 8025 i 1025 (MailHog), 15672 i 5673 (RabbitMQ), 14333 (SQL Server). Zaustavite aplikaciju koja ih zauzima.
- **Logovi:** `docker compose logs api` / `docker compose logs worker`; API dodatno piše dnevni fajl `logs/api-GGGGMMDD.log` unutar kontejnera.
- **Mobilna aplikacija ne vidi API:** provjerite da je emulator pokrenut na istom računaru kao Docker i da `http://localhost:5000` radi u pregledniku računara.
- **Ponovno pokretanje od nule:** `docker compose down -v` pa `docker compose up -d --build` (briše bazu i uploadovane slike, seed se ponovo izvršava).

## Dodatna dokumentacija

- [`recommender-dokumentacija.md`](recommender-dokumentacija.md) - opis sistema preporuke lekcija (formule, signali, tekst objašnjenja, mapiranje na prijavu)
- [`docs/upute.md`](docs/upute.md), [`docs/prijava.md`](docs/prijava.md) - upute i prijava seminarskog rada
- [`docs/FINAL_COMPLIANCE.md`](docs/FINAL_COMPLIANCE.md) - provjera usklađenosti sa uputama i prijavom
- [`docs/REFERENCE_ANALYSIS.md`](docs/REFERENCE_ANALYSIS.md) - analiza referentnog projekta i odluke o arhitekturi
