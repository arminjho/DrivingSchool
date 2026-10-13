# CLAUDE.md — eAutoškola

This file is the permanent rulebook for this repository. It is loaded in every session. Follow it exactly. No application code has been written yet — this phase only set up documentation and project scaffolding rules.

## 1. Project summary

**eAutoškola** — digitalization and centralization of a driving school's business and educational processes. Seminarski rad, Razvoj softvera II, FIT Mostar. Student: Armin Muratspahić, IB210044. Solo project (no group).

Three roles, two client apps:

- **Admin** — desktop app (Flutter Windows). Administrative back-office: reference-data CRUD, user management, vehicle fleet, PDF reports.
- **Učenik (Student)** — mobile app (Flutter Android). Booking lessons, paying installments via PayPal, theory tests, lessons/materials, recommendations, profile.
- **Instruktor (Instructor)** — mobile app (Flutter Android), same Flutter project as Student, role-gated navigation shell. Managing lesson requests, schedule/calendar, grading students after a lesson.

Backend: ASP.NET Core Web API (C#) + a separate Worker microservice, connected via RabbitMQ, SQL Server via EF Core, Docker Compose. Solution projects (approved, see `docs/REFERENCE_ANALYSIS.md` §3): `eAutoskola.Domain` / `.Application` / `.Infrastructure` / `.API` / `.Worker` / `.Contracts` / `.Tests`. Clean-Architecture project *names*, but ClinicNow's own mechanics underneath — no MediatR, no `IApplicationDbContext` abstraction; `Application` services hold the concrete `EAutoskolaDbContext` directly. CQRS is implemented as direct Query/Command interface pairs for entities with real command complexity (Lesson, TestAttempt, payments), not as a dispatcher.

## 2. Source of truth order

When sources conflict, resolve in this order:

1. **`docs/upute.md`** — non-negotiable technical/grading rules from the faculty. These override everything, including the student's own prijava, if a conflict exists.
2. **`docs/prijava.md`** — the approved feature description and mockups. Must be implemented exactly as described. **When the narrative text and a mockup screenshot differ in a specific detail (e.g. exact button label), implement both** — satisfy the described business behavior from the text AND match the visible UI element from the mockup as closely as possible. `docs/prijava.md` already flags every such discrepancy inline where found during conversion.
3. **`docs/REFERENCE_ANALYSIS.md`** — structure/naming/style patterns extracted from the accepted colleague project (ClinicNow) and the course template (ManiFest), plus the approved architecture decision for eAutoškola (§3 — Clean Architecture with CQRS-as-method-organization, no MediatR). Use for *how* to structure code, never for *what* features to build.
4. **Your own judgement** — only when none of the above specify an answer (e.g. a reference-table not mentioned in prijava.md that upute.md §3.1 still requires because of the 10-table minimum).

## 3. Language & naming convention

**Mandatory:** ALL code and schema in English — tables, columns, entities, DTOs, requests, enums, constants, services, controllers, routes, Flutter models/providers/widgets, file names, message contracts, log messages.

**ONLY user-facing text in Bosnian** — UI labels, validation messages, notifications, e-mails, PDF content, recommendation explanations. Keep Bosnian UI strings in one place per app (a strings/constants file), not scattered through widgets, so a future English translation is a single-file change.

### Glossary (use exactly these English names in code)

| Bosanski (iz plana / prijave) | English (use in code) |
|---|---|
| Korisnik / Učenik profil / Instruktor profil | `User` / `StudentProfile` / `InstructorProfile` |
| Vozilo / Servis vozila | `Vehicle` / `VehicleService` |
| Čas / Čas status historija | `Lesson` / `LessonStatusHistory` |
| Test / Pitanje / Ponuđeni odgovor | `Test` / `Question` / `AnswerOption` |
| Pokušaj testa (Rezultat testa) / Odgovor učenika | `TestAttempt` / `AttemptAnswer` |
| Lekcija / Pod-lekcija / Učenik-lekcija ("Moje lekcije") | `StudyLesson` / `StudyLessonSection` / `StudentStudyLesson` |
| Zaduženje / Rata / Uplata / Povrat | `Charge` / `Installment` / `Payment` / `Refund` |
| Notifikacija / Preporuka log | `Notification` / `RecommendationLog` |
| RevokedToken / PasswordResetToken | (stay as-is) |
| Država / Grad | `Country` / `City` |
| Kategorija vozila / Status zahtjeva / Tip usluge | `VehicleCategory` / `RequestStatus` / `ServiceType` |
| Zona vožnje / Oblast pitanja / Uloga | `DrivingZone` / `QuestionArea` / `Role` |
| Uloge: Admin, Instruktor, Učenik | `Admin`, `Instructor`, `Student` |

Use `Lesson` for "čas vožnje" (driving lesson) consistently — never "Class", "Drive", or "Session". `TestAttempt` is the result of one student attempting one `Test` ("Rezultat testa" / "Pokušaj testa" in `docs/prijava.md`'s Bosnian text — same entity).

## 4. Database & global technical conventions

- **Database name: `210044`** (index number, no `IB` prefix — per `upute.md` §3.1).
- **Use `DateTime.UtcNow` everywhere.** Never `DateTime.Now`. No mixing — this causes inconsistency, especially in Docker (`upute.md` Appendix A.4).
- **No hardcoded configuration anywhere.** Everything through `.env` — read once per `*Options` class constructor (see §5 below and `docs/REFERENCE_ANALYSIS.md` §2.9). Nothing config-related lives in `appsettings.json` or is hardcoded in source.
- Minimum 10 non-reference tables (`upute.md` §3.1). If `docs/prijava.md`'s entities don't reach 10, add functionally justified extra tables — never filler tables.
- All FKs defined through EF Fluent configuration (`*Configuration` classes), DB-level referential integrity enforced.
- Reference data (City, Country, VehicleCategory, RequestStatus, ServiceType, DrivingZone, QuestionArea, Role) = FK to a dedicated table, never a free-text/string field.

## 5. Configuration & secrets placeholder convention

Any manual/real value a human must supply (PayPal client id/secret, PayPal webhook id, SMTP credentials, JWT signing key, DB password, etc.) is represented **only** in `.env.example` using the placeholder:

```
PAYPAL_CLIENT_ID=__REPLACE_ME_PAYPAL_CLIENT_ID__
PAYPAL_CLIENT_SECRET=__REPLACE_ME_PAYPAL_CLIENT_SECRET__
PAYPAL_WEBHOOK_ID=__REPLACE_ME_PAYPAL_WEBHOOK_ID__
SMTP_HOST=__REPLACE_ME_SMTP_HOST__
SMTP_USERNAME=__REPLACE_ME_SMTP_USERNAME__
SMTP_PASSWORD=__REPLACE_ME_SMTP_PASSWORD__
JWT_KEY=__REPLACE_ME_JWT_KEY__
```

Every place in code that reads such a value gets a matching comment at the point of read:

```csharp
// TODO(config): set PAYPAL_CLIENT_ID in .env before running
```

**Never invent a real-looking secret.** `.env` itself (the real, filled-in file) is git-ignored and never committed — only `.env.example` is tracked.

## 6. Working conventions

- **One phase per session.** Don't jump ahead to a later phase's work without being asked.
- **Target framework is `net10.0`.** The machine's default `dotnet` (under `C:\Program Files\dotnet`) is only SDK 9.0.306 — .NET 10 SDK (10.0.401) was installed user-locally to `%USERPROFILE%\.dotnet` and added to the **User**-scope `PATH`/`DOTNET_ROOT` env vars. A fresh shell picks this up automatically; if `dotnet --version` reports `9.x` instead of `10.x`, prefix the command with `export PATH="/c/Users/$USER/.dotnet:$PATH"` (Bash) or check `$env:PATH` (PowerShell) rather than reinstalling.
- **At the end of each phase:** run `dotnet build` (backend) and `flutter analyze` (each Flutter app), confirm both are clean (or explain remaining warnings), then:
  1. List every file changed in that phase.
  2. List any rule from this file that could not be satisfied in that phase, and why.
- Keep Bosnian UI strings centralized (see §3) from the very first UI phase, not retrofitted later.
- Don't write documentation files beyond what's asked.

---

## 7. Condensed checklist — every rule from `docs/upute.md`

Full original text lives in `docs/upute.md`. This is a working checklist grouped by topic; it must not drop any rule.

### 7.1. Database

- [ ] Min. 10 non-reference tables; DB named `210044`.
- [ ] SQL Server (or other relational DB); Code First or Database First allowed, with or without stored procedures.
- [ ] All FKs via EF configuration, referential integrity enforced at DB level.
- [ ] Reference tables = static/rarely-changed standardization data only (City, Country, VehicleCategory, RequestStatus, ServiceType, Role/UserRole, pure M2M join tables without extra attributes). They never hold core business data.
- [ ] If prijava's entities don't reach the 10-table minimum, add more functionally justified tables — no filler/suvišne tables.
- [ ] Required fields (NOT NULL in DB) match required fields in the UI.
- [ ] DB must contain enough seed data to actually test the app; seed may run via migrations.
- [ ] Seed is consistent: `HasData` seed and runtime seeder must use the same password-hash format. Seed includes images if the domain uses images.
- [ ] Cascade delete only where it makes sense; otherwise deletion must be blocked with a clear reason shown to the user. Soft-delete purge must respect FK order (children before parents).
- [ ] Reference data always FK, never string fields (e.g. no `string City`, only `CityId`).
- [ ] Models/DTOs/Requests must be internally consistent (don't mix `string City` in one place and `CityId` in another).

### 7.2. Microservices

- [ ] At least two separate services: main API + a Worker/consumer/notifier in its own project/container.
- [ ] Worker must do real asynchronous work (email sending, notification processing) — not just logging.
- [ ] RabbitMQ is the broker between the two services; API publishes, Worker consumes and processes in the background.
- [ ] All services (API, Worker, RabbitMQ, DB) defined in `docker-compose.yml`, each functional and network-connected.
- [ ] Hangfire inside the API does **not** count as a separate microservice. A `BackgroundService` class inside the API project does **not** count as the Worker — it must be its own project (`<Name>.Worker`) with its own Dockerfile, added as its own `docker-compose.yml` service.
- [ ] Docker image tags explicitly versioned (e.g. `postgres:16`, never `:latest` for key services).
- [ ] Prepare enough log output to reproduce problems, especially crashes during review.

### 7.3. Configuration

- [ ] Centralized configuration management, one place only — never the same value fixed in multiple locations.
- [ ] All config in `.env`, never hardcoded in source or in `appsettings.json`. Specifically: RabbitMQ (host/sender/port), SMTP (host/username/password/use-ssl/port), payment provider key, JWT key, connection string, API path, everything else.
- [ ] Flutter API base URL configurable via `--dart-define=API_BASE_URL=...`, read with `String.fromEnvironment('API_BASE_URL')`.

### 7.4. Code standards

- [ ] No unused code; widgets with no implemented functionality removed; controls load only the data they need.
- [ ] No magic numbers (e.g. `statusId = 1,2,3`) — use enums/constants. Role magic strings go in a static class.
- [ ] Controller → Service → DbContext only. Controllers never contain business logic or touch `DbContext` directly.
- [ ] Services using `DbContext` registered **Scoped**, never Transient.
- [ ] Multiple `SaveChangesAsync()` calls in one operation wrapped in an explicit transaction.
- [ ] Custom exception types (`BusinessException`, `NotFoundException`, etc.) instead of generic `Exception`, mapped to HTTP status via `ExceptionFilter`/middleware.
- [ ] No stack traces/internal errors/infra details exposed to the client outside Development; log server-side, return a standardized message.
- [ ] Controllers return DTOs, never Entity objects directly.
- [ ] No `dynamic` type — use typed DTOs.
- [ ] Avoid Service-in-Service calls that each call `SaveChanges()` — rely on the shared `DbContext`.
- [ ] Inject `IHttpContextAccessor` into services instead of manually parsing tokens.
- [ ] Create `HttpClient` via `IHttpClientFactory`, never `new HttpClient()` directly.
- [ ] CORS configured once, explicit allowed origins.
- [ ] No duplicate `Program.cs` service registrations (`UseCors`, `AddHttpContextAccessor`, `AddSwaggerGen` each called once).
- [ ] No template leftovers (`WeatherForecastController`), dead code, unused imports, commented-out code, duplicated code (DRY).
- [ ] `ILogger<T>` for logging, never `Console.WriteLine`.
- [ ] No incorrect class/file names or copy-paste leftovers from other projects.
- [ ] No `NotImplementedException` left in place.
- [ ] No "thinking out loud" comments in production code.
- [ ] `README.md` kept accurate and up to date with actual code state.
- [ ] No commented-out code blocks.
- [ ] Client never calls backend routes that don't exist.

### 7.5. Validation

- [ ] Full input validation everywhere, including edit forms.
- [ ] Error messages explicitly state format/constraints (not generic).
- [ ] Validation doesn't force editing every field unnecessarily.
- [ ] Password editing rules: don't require a new password when editing a user; use a checkbox/button "Izmijeni lozinku" to reveal new-password + confirm fields (or leave both empty and only validate if filled); admin editing another user never enters the old password; a user changing their own password must confirm the old one.
- [ ] Forms react correctly to invalid input; no false positives.
- [ ] Validation messages shown clearly below the control, never inside the input or as a dialog.
- [ ] Email and phone fields have format validation.
- [ ] Successful record creation shows a specific success message, never a generic "Success"/"Bad request".

### 7.6. Authentication & authorization

- [ ] Authentication implemented and covers every endpoint that needs authorization; no unauthorized access possible.
- [ ] `[Authorize]` on every controller touching user data.
- [ ] `[AllowAnonymous]` only on login/register endpoints.
- [ ] Write operations (POST/PUT/DELETE) never open/anonymous.
- [ ] Admin endpoints require `[Authorize(Roles = "Admin")]`, not plain `[Authorize]`. Seed role names match the attribute role names exactly.
- [ ] `userId` for "current user" operations always comes from the JWT, never from route or body. Verify a user only modifies their own data; Admin may operate on others' data.
- [ ] Register endpoint never accepts role/isAdmin from the client.
- [ ] Dev/test endpoints protected by `env.IsDevelopment()` or fully removed; no test controllers (e.g. `PayPalTest`) reachable in production.
- [ ] JWT parsing validates the signature. Logout invalidates the token server-side, not just locally.
- [ ] Login credentials sent in the POST body, never query string.
- [ ] File upload/download requires authorization + ownership check.
- [ ] Real-time endpoints (chat hub etc.) check membership (e.g. is the user a participant).
- [ ] File upload validates MIME type and magic bytes, not just the extension.
- [ ] `README.md` contains full run instructions and test credentials:

  | Context | Username | Password |
  |---|---|---|
  | Desktop | `desktop` | `test` |
  | Mobile | `mobile` | `test` |
  | Multiple roles | `<roleName>` | `test` |

### 7.7. UI

- [ ] Forms clear and user-friendly, respecting basic UI/UX principles.
- [ ] Readable design, no excessive bright colors or transparency that hurts usability.
- [ ] Intuitive UX, including an expected "X" close button top-right on forms.
- [ ] Dropdowns populated from the DB (e.g. cities from a dropdown, not a textbox).
- [ ] Correct input controls: booleans via checkbox/toggle; dates via DateTime picker; selectable items (cities etc.) via dropdown; geographic coordinates via a map/coordinate-picker modal, never typed directly into a textbox.
- [ ] After a successful save: if staying on the form, fields auto-clear; if the flow returns to a list/history, auto-redirect there with the newest record on top and the list auto-refreshed (no manual refresh needed).
- [ ] Forms never show raw DB ID values.
- [ ] Many-to-many displays never show (or consist solely of) IDs.
- [ ] Where it makes business sense, a related FK object can be added without leaving the current flow (e.g. via a modal).
- [ ] Controls don't overlap or waste space; images shouldn't exceed ~50% of a form's area. Prefer a clean two-column (label/value) or one-item-per-row layout over misaligned labels.
- [ ] Consider icons for dates, car model, fuel consumption, price, etc., to improve scanability.
- [ ] Don't allow opening an add-form if its prerequisites aren't met (e.g. an FK table has no records).
- [ ] Entities with an image show that image next to the name in relevant table/list views.
- [ ] Confirmation dialog required for irreversible actions (delete, payment, sending an order).
- [ ] "Back" button required for navigation.
- [ ] Unavailable actions shown disabled with an explanation of why.

### 7.8. Business logic / state machine

Applies because eAutoškola has driving-lesson scheduling (rezervacije/zakazivanja):

- [ ] Status field with defined states: `Pending -> Confirmed -> Cancelled / Completed`. Hard delete instead of a status change is an error.
- [ ] Centralized state-machine logic with clearly defined allowed transitions — never scattered across controllers.
- [ ] Business-rule checks on status change (don't reject an already-approved request, don't review before completion, don't cancel something paid without a refund flow).
- [ ] Slot availability/overlap checked server-side, not just client-side.
- [ ] Server-side precondition validation (expired membership, capacity, availability, etc.).
- [ ] Unique constraint or service-level duplicate check (same user + same slot/training).
- [ ] Audit trail: who approved/rejected, when, and a description within the record (→ `LessonStatusHistory`).
- [ ] Cancellation available to both user and admin/instructor; a rejection must include a reason and trigger a notification.
- [ ] Correct price calculation for edge cases (e.g. 25h, 48h duration).

### 7.9. Payments

Applies because eAutoškola includes PayPal payment:

- [ ] Payment finalized server-side via webhook or server-side API verification — client never records a successful payment itself.
- [ ] Confirm-payment is idempotent — if already `Completed`, effects are not re-applied.
- [ ] Prevent double payment of the same item by checking for an existing open/completed payment first.
- [ ] After a successful payment, UI moves to a clear "Paid" state; the pay button is hidden for already-paid items (`IsPaid` in the response DTO).
- [ ] Mobile payment is in-app (PayPal SDK / in-app browser with return, not an external browser redirect with no return).
- [ ] Refund logic implemented (prijava.md commits to it); refund is based on the actually-charged amount, never a recalculated price.
- [ ] Server owns the catalog/price; purchase never trusts the client for price.

### 7.10. Notifications

- [ ] System notifications have: read/unread status, title, text, date/time.
- [ ] "Mark as read" option.
- [ ] Mandatory auto-refresh via SignalR or polling — manual-only refresh is unacceptable.
- [ ] Notifications for all relevant events (booking, cancellation, status change, payment), not just one event type.
- [ ] News/obavijesti items have title, text, image, and date/time.

### 7.11. Performance & pagination

- [ ] `async`/`await` throughout the stack; never `.GetAwaiter().GetResult()`, `.Wait()`, `.Result`; `Thread.Sleep` → `await Task.Delay`.
- [ ] Async EF calls inside async methods (`FirstOrDefaultAsync`, `ToListAsync`, etc.).
- [ ] No N+1 queries — use `Include`, `GroupBy`, or batch queries. `MapToResponse`-style methods never re-query entities from the DB.
- [ ] Background jobs via `IHostedService`/`BackgroundService`, never `Task.Run` fire-and-forget.
- [ ] Aggregation queries use a single `GroupBy`, not many separate queries.
- [ ] Price filtering/validation at the DB level (`Where`), not loaded into memory then filtered in LINQ.
- [ ] Cache data loaded on every request with `IMemoryCache` at the service level — never an instance-level `Dictionary` on a Transient service.
- [ ] Environment variables read once in the constructor, not on every call.
- [ ] Pagination mandatory on every list endpoint; `PageSize` has a defined max (e.g. 100). An unlimited "RetrieveAll" endpoint is treated as a rejection-worthy error (DoS-adjacent risk).
- [ ] List endpoints never return large payloads (PDF blobs, large base64 images) — only what's needed for the list view; details live on a separate endpoint.

### 7.12. Appendix A — recommendations (anti-patterns to avoid)

**A.1 RabbitMQ/messaging**
- [ ] `AsyncEventingBasicConsumer` for async lambda handlers, not `EventingBasicConsumer`.
- [ ] Singleton connection / connection pool — never a new connection per publish.
- [ ] Retry logic with exponential backoff (1s → 2s → 4s → 8s) on consumer failure; never an empty catch block.
- [ ] Error handling mandatory; the worker never silently goes unavailable without logging why.

**A.2 Flutter/mobile**
- [ ] Parallelizable HTTP calls go through `Future.wait()`, not sequential `await`s.
- [ ] Base64 image decoding done once and cached, never inside `build()`; prefer a URL over a base64 string.
- [ ] Deep link handling covers both the initial-link and resumed-app scenarios.
- [ ] Frontend handles HTTP 401 correctly (redirect to login or refresh-token flow); expired tokens aren't ignored.
- [ ] Specific error messages, not generic ones; response handlers never swallow backend validation messages.
- [ ] No mock/test-named files containing production code.

**A.3 Cryptography/password security**
- [ ] `RandomNumberGenerator` (`System.Security.Cryptography`) for codes/tokens/referral codes, never `System.Random`.
- [ ] `bcrypt`, `Argon2`, or `PBKDF2` for password hashing — never unsalted SHA256, HMAC-SHA512, or a custom scheme.
- [ ] Reset tokens have a defined `ExpiryTime`; reset codes never stored in plain text.

**A.4 Date/time**
- [ ] `DateTime.UtcNow` standardized across the whole app (also in §4 above).

**A.5 Resources/dispose**
- [ ] All disposables (`Stream`, `HttpResponseMessage`, `DbConnection`, etc.) inside a `using` statement/declaration.

### 7.13. Submission

- [ ] Public GitHub repo with a correct `.gitignore`.
- [ ] `recommender-dokumentacija.md` committed to the repo (see `docs/REFERENCE_ANALYSIS.md` §2.10 for the recommended skeleton).
- [ ] Android APK build + Windows desktop build produced; build artifacts never committed to git history — only attached to a GitHub Release ZIP.
- [ ] One GitHub Release per review submission, ZIP containing both the APK (`.../build/app/outputs/flutter-apk/app-release.apk`) and the Windows Release folder (`.../build/windows/x64/runner/Release`); suggested archive name `fit-build-20gg-mm-dd.zip`. No `.env` or other secrets in the Release.
- [ ] Final submission uses an **Immutable Release**: create as draft, attach ZIP, verify, then publish; immutability enabled beforehand in repo Settings → Releases.
- [ ] No repo/Release changes allowed after the submission deadline.
- [ ] DL system gets the exact Release tag link (never `releases/latest`, never the bare repo link) + the `.env` ZIP password.
- [ ] Prefer plain HTTP (no self-signed HTTPS) for API access during review.
- [ ] Secret-bearing config files replaced with a password-protected ZIP next to the original (e.g. `.env-tajne.zip` instead of `.env`), password `fit` or similar.
- [ ] Application must run with zero post-clone modification (no code changes, no reference/library additions, no link/port/connection-string edits) and remain stable during use.
- [ ] Test in an environment other than the dev machine before submitting; fix anything found.

---

## 8. Known text-vs-mockup discrepancies to resolve during implementation

See `docs/prijava.md` for full inline detail. Summary of places where the mockup screenshot and the narrative text disagree, both to be honored per §2 rule 2:

- §4.2.7 (Rezultat testa): text describes "Pogledaj test" + "Preporuke" buttons; mockup shows "Ponovi test". Implement all three actions.
- §4.2.3 (Moji časovi) badge colors: text says Pending=orange/Confirmed=blue/Completed=green/Cancelled=red; mockup shows Potvrđen=green/Na čekanju=orange/Završen=blue/Otkazan=red. Status **enum names** stay exactly `Pending`/`Confirmed`/`Completed`/`Cancelled` per `upute.md`'s state machine; the display color/label mapping is a UI decision — pick one consistent scheme.
- §4.3.3 (Detalji zahtjeva) button label: text says "Odobri"; mockup shows "Potvrdi čas". Either label is fine as long as it consistently triggers the approve/Confirm transition.
- §4.3.4 (Moji časovi – Raspored): mockup adds a two-step "Počni čas" / "Završi čas" UI flow not mentioned in the text. Treat as UI-only steps on the client (or extra timestamp fields on `Lesson`); do **not** add a new status value outside `Pending/Confirmed/Cancelled/Completed`.
- §4.3.1 (Početni ekran instruktora): text lists "Izvještaji" as its own nav item; mockup's bottom nav only has 4 icons (Početna/Raspored/Učenici/Profil), no separate Izvještaji icon. Place instructor reports inside Profil or add a 5th nav icon — implementer's choice.
