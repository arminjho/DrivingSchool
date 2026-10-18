# eAutoškola

Sistem za upravljanje autoškolom (seminarski rad, Razvoj softvera II, FIT Mostar). Tri uloge: **Administrator** (desktop), **Učenik** (mobilna aplikacija) i **Instruktor** (mobilna aplikacija).

> **Stanje projekta:** aplikacija je kompletna i funkcionalna. Backend (API, Worker, baza, RabbitMQ) se pokreće kroz Docker. Desktop aplikacija (Administrator) ima Dashboard, upravljanje korisnicima, voznim parkom, izvještaje, referentne podatke, testove/pitanja, lekcije i pregled časova. Mobilna aplikacija (Učenik i Instruktor, jedna aplikacija, meni se prilagođava ulozi) ima registraciju/prijavu/reset lozinke, zakazivanje časova, historiju časova, finansije i plaćanje preko PayPal-a, probne testove, lekcije i preporuke, te - za instruktora - zahtjeve za časove, raspored i ocjenjivanje.

## Sadržaj

- [Arhitektura](#arhitektura)
- [Pokretanje](#pokretanje)
- [Flutter aplikacije](#flutter-aplikacije)
- [Nazivi entiteta](#nazivi-entiteta)
- [Korisnički podaci za pristup](#korisnički-podaci-za-pristup)
- [Dashboard i izvještaji](#dashboard-i-izvještaji)
- [Plaćanje i povrat novca](#plaćanje-i-povrat-novca)
- [Provjera plaćanja sa PayPal sandbox kupcem](#provjera-plaćanja-sa-paypal-sandbox-kupcem)
- [Testovi](#testovi)
- [Dodatna dokumentacija](#dodatna-dokumentacija)

## Arhitektura

| Projekat | Uloga |
|---|---|
| `eAutoskola.Domain` | Entiteti, enumi, izuzeci, interfejsi (npr. `IPayPalClient`, `IMessagePublisher`), centralizirani tekstovi poruka |
| `eAutoskola.Application` | Servisi (Controller → Service → DbContext), CQRS podjela gdje postoji stvarna kompleksnost (časovi) |
| `eAutoskola.Infrastructure` | EF Core + SQL Server, migracije i seed, JWT/BCrypt, RabbitMQ publisher, PayPal klijent, upload fajlova |
| `eAutoskola.API` | ASP.NET Core Web API, SignalR (notifikacije u realnom vremenu) |
| `eAutoskola.Worker` | Poseban kontejner: čita RabbitMQ redove i šalje e-mailove (SMTP) |
| `eAutoskola.Contracts` | Poruke koje dijele API i Worker |
| `eAutoskola.Tests` | xUnit testovi |

Docker servisi: SQL Server (baza `210044`), RabbitMQ, MailHog (lokalni SMTP hvatač), API i Worker.

## Pokretanje

**Preduvjeti:** Docker Desktop. (Za pokretanje testova van Dockera: .NET 10 SDK, Flutter.)

1. Postavite `.env` u korijenu repozitorija (isti folder kao `docker-compose.yml`), na jedan od dva načina:
   - **Pregled rada (predati repozitorij):** u repozitoriju se nalazi `.env-tajne.zip` umjesto `.env` fajla (upute.md §9.2 - konfiguracijski fajlovi sa tajnama se ne commitaju u plain-textu). Raspakujte ga šifrom koja je predata na DL sistemu (podrazumijevano `fit`) u korijen repozitorija - raspakivanje mora dati fajl `.env` tačno u istom folderu u kojem se nalazi `.env-tajne.zip`. Nakon toga nastavite na korak 2.
   - **Samostalni razvoj (vlastiti clone, bez `.env-tajne.zip`):** kopirajte `.env.example` u `.env`, pa u njemu zamijenite sve `__REPLACE_ME_*__` vrijednosti (lozinka baze, JWT ključ, RabbitMQ korisnik/lozinka, SMTP, PayPal). Fajl `.env` je u `.gitignore` i ne smije se commitati.
     - `DB_CONNECTION_STRING` i `DB_SA_PASSWORD` moraju koristiti istu lozinku.
     - Za pregled u Swagger-u postavite `ASPNETCORE_ENVIRONMENT=Development`.
2. Pokrenite sve servise:

   ```bash
   docker compose up -d --build
   ```

   Pri prvom pokretanju API sam primjenjuje migracije i popunjava bazu početnim podacima (seed). Za potpuno čisto stanje: `docker compose down -v` pa ponovo `up`.

   Ako RabbitMQ još nije spreman kada se Worker podigne, Worker ponavlja povezivanje (razmak raste do 30 s) dok se ne poveže; ne pada i ne restartuje se.

| Servis | Adresa |
|---|---|
| API | http://localhost:5000 (bez HTTPS-a, namjerno) |
| Swagger (samo `Development`) | http://localhost:5000/swagger |
| MailHog (primljeni e-mailovi) | http://localhost:8025 |
| RabbitMQ management | http://localhost:15672 (korisnik/lozinka iz `.env`) |

## Flutter aplikacije

`UI/driving_school_desktop` (Windows, Administrator) i `UI/driving_school_mobile` (Android, Učenik i Instruktor - jedna aplikacija, meni se prilagođava ulozi iz JWT tokena). Obje dijele isti obrazac: `lib/core/` (sesija, HTTP klijent, obavijesti, validatori), `lib/l10n/app_strings.dart` (sve vidljive poruke na bosanskom), `lib/widgets/ui/` (zajednički widgeti), tamna tema.

```bash
cd UI/driving_school_desktop   # ili UI/driving_school_mobile
flutter pub get
flutter analyze
flutter test
```

Pokretanje desktop aplikacije (Windows):

```bash
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
```

Pokretanje mobilne aplikacije (Android emulator - `10.0.2.2` je standardna adresa emulatora za host mašinu):

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

`API_BASE_URL` se čita isključivo preko `--dart-define` (`String.fromEnvironment('API_BASE_URL')`), nikad hardkodirano. Pokretanje Windows builda zahtijeva uključen Developer Mode (`start ms-settings:developers`) zbog symlink-ova koje koriste pluginovi.

## Nazivi entiteta

Programski kod i baza podataka su u potpunosti na engleskom (klase, tabele, kolone, rute,
poruke) - u skladu sa upute.md §3.4/CLAUDE.md §3; na bosanskom je samo tekst koji korisnik
stvarno vidi (UI, poruke, PDF, e-mail), centralizovan u `lib/l10n/app_strings.dart` (oba Flutter
projekta) i `eAutoskola.Domain/Resources/Messages.cs` (backend). Ispod je mapiranje nekoliko
ključnih naziva iz odobrene prijave na njihove engleske nazive u kodu:

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

## Korisnički podaci za pristup

Lozinka za sve račune je **`test`**.

| Kontekst | Korisničko ime | Lozinka | Uloga |
|---|---|---|---|
| Desktop verzija | `desktop` | `test` | Administrator |
| Mobilna verzija | `mobile` | `test` | Učenik |
| Mobilna verzija (instruktor) | `instruktor` | `test` | Instruktor |

Dodatni seed računi (svi sa lozinkom `test`):

| Korisničko ime | Uloga | Napomena |
|---|---|---|
| `adnan.zukic@eautoskola.ba` | Učenik | Već je iskoristio svih 30 časova iz paketa. Ima nadolazeći **neplaćen** dodatni čas i historijski plaćen pa vraćen čas - koristi se za provjeru plaćanja i povrata. |
| `amina.hodzic@eautoskola.ba`, `emir.mustafic@eautoskola.ba`, `lejla.basic@eautoskola.ba` | Učenik | Primjer časova u različitim statusima |
| `damir.kovacevic@eautoskola.ba`, `sanja.peric@eautoskola.ba` | Instruktor | |

## Dashboard i izvještaji

Samo za ulogu **Admin** (`desktop` / `test`).

| Endpoint | Opis |
|---|---|
| `GET /api/dashboard` | Učenici, instruktori, vozila (aktivna / na servisu), časovi ovog mjeseca sa promjenom u odnosu na prošli, broj završenih časova po mjesecima tekuće godine, raspodjela po statusima, nedavne aktivnosti i današnji predstojeći časovi. Rezultat se kratko (30 s) kešira. |
| `GET /api/reports/lesson-status/pdf` | PDF "Izvještaj o statusu časova vožnje". Filteri: `dateFrom`, `dateTo` (`yyyy-MM-dd`), `instructorId`. Broj odrađenih, zakazanih (na čekanju + potvrđeni) i otkazanih časova po instruktoru, sa grafikonom i tabelom. |
| `GET /api/reports/vehicle-services/pdf` | PDF "Izvještaj o vozilima i servisima". Filteri: `vehicleCategoryId`, `dateFrom`, `dateTo`. Servisna historija i troškovi po vozilu i po kategoriji. |
| `GET /api/reports/lesson-status`, `GET /api/reports/vehicle-services` | Isti podaci kao JSON (sa istim filterima), da desktop aplikacija može nacrtati grafikon pored PDF-a. |

Svi filteri su opcionalni; izostavljen datum ostavlja kraj perioda otvoren, a izostavljen `instructorId` / `vehicleCategoryId` znači "svi". PDF se generiše na serveru (QuestPDF), a svaka stranica ima naziv škole, vrijeme generisanja (UTC) i primijenjene filtere. Neispravan filter (npr. datum "do" prije datuma "od") vraća `400` sa porukom po polju.

## Plaćanje i povrat novca

- **Paket obuke** se sastoji od tri stavke (Teorijska obuka 200 KM, Praktična vožnja 1200 KM, Administracija i ispit 150 KM). Svaka se plaća u 3 rate. Zaduženja se kreiraju zajedno sa učenikom (samostalna registracija, administratorov "Novi korisnik" i seed), u istoj transakciji kao korisnik i profil; `GET /api/finance/my` samo čita i ništa ne kreira. Baze napravljene prije ove izmjene nemaju zaduženja za postojeće učenike, pa ih treba ponovo napraviti (`docker compose down -v`).
- **Dodatni časovi:** paket uključuje **30** praktičnih časova. Svaki potvrđen čas preko te kvote dobija vlastito zaduženje ("Dodatni čas vožnje", 40 KM, jedna rata) u istoj transakciji u kojoj se čas potvrđuje.
- **Valuta:** sve se čuva i prikazuje u **KM**. PayPal ne podržava BAM, pa se iznos pretvara u EUR (fiksni kurs 1 EUR = 1.95583 KM, zaokruženo na 2 decimale) samo u trenutku poziva PayPal-a. Za povrat se koristi iznos koji je PayPal stvarno naplatio (`CapturedAmount`).
- **Finalizacija plaćanja** je isključivo serverska: `POST /api/payments/{orderId}/capture` pita PayPal da li je kupac odobrio i naplatio uplatu. Ništa što pošalje klijent ne važi kao dokaz uplate. Webhook endpoint ne postoji. Ponovni poziv za istu narudžbu je siguran (vraća `alreadyProcessed: true`, bez ponovnih nuspojava).
- **Napuštene narudžbe:** ako za ratu postoji narudžba mlađa od 30 minuta koja nije plaćena, ista narudžba (isti `orderId` i `approvalUrl`) se vraća umjesto kreiranja nove; starija se označava kao neuspjela. Rata koja je već plaćena ne može se platiti ponovo.
- **Otkazivanje plaćenog časa = povrat novca** (nema posebnog endpointa; ide kroz `POST /api/lessons/{id}/cancel`):
  - **Učenik** može otkazati plaćeni čas uz povrat najkasnije **24 sata** prije termina; kasnije je otkazivanje blokirano.
  - **Instruktor i Administrator** mogu uvijek otkazati plaćeni čas i povrat se uvijek izvršava.
  - Povrat se prvo izvršava kod PayPal-a; tek ako uspije, čas se otkazuje i stanje se mijenja (`Uplata → Povrat`, `Rata → Otkazana`, `Zaduženje → Otkazano`). Ako PayPal odbije povrat, čas ostaje potvrđen, uplata ostaje evidentirana, a korisnik dobija poruku da pokuša ponovo ili kontaktira administraciju. Neuspjeli pokušaj se čuva u tabeli povrata.
  - Neplaćen čas se pri otkazivanju samo otkazuje zajedno sa zaduženjem (ništa se ne briše).
  - Otkazana/vraćena zaduženja ne ulaze u "Preostalo za platiti" ni u pregled troškova; povrat se u historiji prikazuje kao posebna stavka "Povrat".
- Obavijesti: pri uspješnoj uplati ("Uplata primljena") i pri povratu ("Povrat novca") korisnik dobija notifikaciju u aplikaciji i e-mail (vidljiv u MailHog-u).

## Provjera plaćanja sa PayPal sandbox kupcem

Seed podaci ne sadrže stvarnu PayPal uplatu, pa se pravi tok (uplata → povrat) provjerava ručno:

1. Na https://developer.paypal.com/dashboard/applications/sandbox napravite sandbox aplikaciju i upišite njen Client ID / Secret u `.env` (`PAYPAL_CLIENT_ID`, `PAYPAL_CLIENT_SECRET`). Zatim `docker compose up -d --build api`.
2. U PayPal Developer Dashboard-u (Testing Tools → Sandbox Accounts) koristite **Personal (kupac)** sandbox račun. Njegove podatke ne commitajte: `__REPLACE_ME_PAYPAL_SANDBOX_BUYER__` (e-mail i lozinka sandbox kupca).
3. Prijavite se kao `adnan.zukic@eautoskola.ba` / `test` i pozovite `GET /api/finance/my`. U `nextInstallment` je rata dodatnog časa (40 KM); u `costBreakdown` stavka sa `lessonId` označava taj čas. Zapamtite `installmentId` i `lessonId`.
4. `POST /api/payments/installments/{installmentId}/create-order` vraća `orderId` i `approvalUrl`. Otvorite `approvalUrl` u pregledniku, prijavite se sandbox kupcem i odobrite uplatu. (U mobilnoj aplikaciji će ovo presresti WebView na adresama `PAYPAL_RETURN_URL` / `PAYPAL_CANCEL_URL`; u pregledniku redirect na te adrese ne mora ništa prikazati.)
5. `POST /api/payments/{orderId}/capture` → `status: "Completed"`. Provjerite: `GET /api/finance/my` (rata plaćena, preostali iznos smanjen, stavka u historiji), notifikaciju "Uplata primljena" i e-mail u MailHog-u.
6. Prijavite se kao `desktop` / `test` (Administrator) i pozovite `POST /api/lessons/{lessonId}/cancel` sa tijelom `{ "reason": "Test povrata" }`. Povrat se izvršava stvarnim pozivom PayPal sandbox-a.
7. Ponovo `GET /api/finance/my` kao Adnan: zaduženje je nestalo iz pregleda troškova, preostali iznos je smanjen za 40 KM, a historija ima stavku "Povrat". Transakcija je označena kao vraćena i u PayPal sandbox dashboard-u; e-mail "Potvrda povrata sredstava" je u MailHog-u.

Napomena: kao Adnan (učenik) isti čas ne možete otkazati uz povrat ako je do termina manje od 24 sata.

## Testovi

```bash
dotnet test eAutoskola.Tests
```

Pokrivaju preporuku lekcija (vektori, kosinusna sličnost, objašnjenja), dashboard (agregacije, granice mjeseci i godine, keširanje), izvještaje (filteri, grupisanje, validacija, generisanje PDF-a), pravila plaćanja (konverzija KM → EUR, podjela na rate, kvota, rok za povrat, ističanje narudžbi), tok kreiranja narudžbe / capture-a / povrata nad EF InMemory bazom sa mockiranim PayPal klijentom, te sam `PayPalClient` nad simuliranim PayPal odgovorima.

## Dodatna dokumentacija

- [`recommender-dokumentacija.md`](recommender-dokumentacija.md) - opis sistema preporuke lekcija
- [`docs/upute.md`](docs/upute.md), [`docs/prijava.md`](docs/prijava.md) - upute i prijava seminarskog rada
- [`docs/REFERENCE_ANALYSIS.md`](docs/REFERENCE_ANALYSIS.md) - analiza referentnog projekta i odluke o arhitekturi
