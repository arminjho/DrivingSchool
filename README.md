# eAutoškola

Sistem za upravljanje autoškolom (seminarski rad, Razvoj softvera II, FIT Mostar). Tri uloge: **Administrator** (desktop), **Učenik** (mobilna aplikacija) i **Instruktor** (mobilna aplikacija).

> **Stanje projekta:** backend (API, Worker, baza, RabbitMQ) je implementiran i pokreće se kroz Docker. Flutter aplikacije u folderu `UI/` su za sada samo prazni projekti.

## Sadržaj

- [Arhitektura](#arhitektura)
- [Pokretanje](#pokretanje)
- [Korisnički podaci za pristup](#korisnički-podaci-za-pristup)
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

**Preduvjeti:** Docker Desktop. (Za pokretanje testova van Dockera: .NET 10 SDK.)

1. Kopirajte `.env.example` u `.env`.
2. U `.env` zamijenite sve `__REPLACE_ME_*__` vrijednosti (lozinka baze, JWT ključ, RabbitMQ korisnik/lozinka, SMTP, PayPal). Fajl `.env` je u `.gitignore` i ne smije se commitati.
   - `DB_CONNECTION_STRING` i `DB_SA_PASSWORD` moraju koristiti istu lozinku.
   - Za pregled u Swagger-u postavite `ASPNETCORE_ENVIRONMENT=Development`.
3. Pokrenite sve servise:

   ```bash
   docker compose up -d --build
   ```

   Pri prvom pokretanju API sam primjenjuje migracije i popunjava bazu početnim podacima (seed). Za potpuno čisto stanje: `docker compose down -v` pa ponovo `up`.

   Ako RabbitMQ još nije spreman kada se Worker podigne, Worker se jednom ili dva puta automatski restartuje (`restart: unless-stopped`) i zatim radi normalno.

| Servis | Adresa |
|---|---|
| API | http://localhost:5000 (bez HTTPS-a, namjerno) |
| Swagger (samo `Development`) | http://localhost:5000/swagger |
| MailHog (primljeni e-mailovi) | http://localhost:8025 |
| RabbitMQ management | http://localhost:15672 (korisnik/lozinka iz `.env`) |

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

## Plaćanje i povrat novca

- **Paket obuke** se sastoji od tri stavke (Teorijska obuka 200 KM, Praktična vožnja 1200 KM, Administracija i ispit 150 KM). Svaka se plaća u 3 rate. Zaduženja se kreiraju pri prvom otvaranju ekrana "Finansije i plaćanje" (`GET /api/finance/my`).
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

Pokrivaju preporuku lekcija (vektori, kosinusna sličnost, objašnjenja), pravila plaćanja (konverzija KM → EUR, podjela na rate, kvota, rok za povrat, ističanje narudžbi), tok kreiranja narudžbe / capture-a / povrata nad EF InMemory bazom sa mockiranim PayPal klijentom, te sam `PayPalClient` nad simuliranim PayPal odgovorima.

## Dodatna dokumentacija

- [`recommender-dokumentacija.md`](recommender-dokumentacija.md) - opis sistema preporuke lekcija
- [`docs/upute.md`](docs/upute.md), [`docs/prijava.md`](docs/prijava.md) - upute i prijava seminarskog rada
- [`docs/REFERENCE_ANALYSIS.md`](docs/REFERENCE_ANALYSIS.md) - analiza referentnog projekta i odluke o arhitekturi
