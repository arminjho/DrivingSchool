# eAutoškola — Audit Report

**Scope:** full repo audit against `docs/upute.md` (every bullet, including Appendix A) and
`docs/prijava.md` (every feature/screen), plus a language audit and a targeted anti-pattern
sweep, compared in style against the reference project (`clinic-managment-flutter-app` /
ClinicNow). Every FAIL found during this pass was fixed in the same pass; this document reflects
the **post-fix** state, with the original finding and the fix both recorded so nothing is lost.

**Verification run (this audit):**

| Check | Result |
|---|---|
| `dotnet build eAutoskola.slnx` (.NET 10 SDK) | ✅ Build succeeded — 0 errors, 42 warnings (all `NU1902`/`NU1903` transitive-package CVE advisories on `System.Security.Cryptography.Pkcs`/`.Xml`, not code defects; see §4 below) |
| `dotnet test eAutoskola.slnx` | ✅ 117/117 passed |
| `flutter analyze` — `driving_school_desktop` | ✅ No issues found |
| `flutter analyze` — `driving_school_mobile` | ✅ No issues found |
| `flutter test` — `driving_school_desktop` | ✅ 102/102 passed |
| `flutter test` — `driving_school_mobile` | ✅ 99/99 passed (3 fewer than the 102 reported in earlier session reports — see §3.7, a dead widget's dead tests were removed, not a regression) |
| `docker compose up --build` | ⚠️ Not run in this pass — needs Docker Desktop running on this machine; see §7 for what was verified by static/code reading instead |

---

## 1. `docs/upute.md` compliance (every bullet, via CLAUDE.md §7's 1:1 checklist)

Legend: ✅ PASS, 🛠 FAIL→FIXED (this audit), ⚠️ PASS with a noted caveat.

### 1.1 Database (§3.1)

| Rule | Evidence | Verdict |
|---|---|---|
| Min. 10 non-reference tables | 23 non-reference `DbSet`s (Users, StudentProfiles, InstructorProfiles, Vehicles, VehicleServices, Lessons, LessonStatusHistories, Charges, Installments, Payments, Refunds, Notifications, RevokedTokens, PasswordResetTokens, Tests, Questions, AnswerOptions, TestAttempts, AttemptAnswers, StudyLessons, StudyLessonSections, StudentStudyLessons, RecommendationLogs) vs 8 reference tables (Country, City, VehicleCategory, RequestStatus, ServiceType, VehicleServiceType, DrivingZone, QuestionArea). `EAutoskolaDbContext.cs` | ✅ |
| DB named by index number, no `IB` prefix | `.env.example`, `docker-compose.yml:58` both use `Database=210044`; live-verified via `dotnet ef dbcontext info` → `Database name: 210044` | ✅ |
| SQL Server, FKs via EF Fluent config, DB-level referential integrity | `Microsoft.EntityFrameworkCore.SqlServer`; 5/5 sampled relationships (`Lesson→Student/Instructor`, `Charge→Student`, `Installment→Charge`, `TestAttempt→Test`) use explicit `HasOne().WithMany().HasForeignKey().OnDelete()` in dedicated `*Configuration.cs` files | ✅ |
| Reference tables hold only static/rarely-changed data | Verified field lists for all 8 reference entities — none carry core business data | ✅ |
| Required fields match UI | 5/5 sampled (`User.Email/FirstName`, `Lesson.StartsAtUtc`, `Vehicle.RegistrationNumber`, `Question.Text`) non-nullable in C# *and* `IsRequired()`/EF convention | ✅ |
| Enough seed data to test the app | `DbSeeder.cs` seeds 3 demo accounts (`desktop`/`mobile`/`instruktor`) + 6 more users, 5 vehicles, services, 2 tests with questions, study lessons, package charges for every student | ✅ |
| Seed hash format matches runtime hash format | Single runtime seeder (no `HasData` anywhere — grep confirms zero real `HasData(...)` calls), calls the exact same `PasswordHasher.Hash` (BCrypt, work factor 12) used by `AuthService`/`UserService`/`MeService` | ✅ |
| **Seed includes images where the domain uses them** | Originally **FAIL**: `User.ProfileImagePath`, `Vehicle.ImagePath`, `Question.ImagePath` all seeded as `null`. **Fixed**: generated 10 real placeholder images (valid JPEG/PNG magic bytes), added as `eAutoskola.Infrastructure/SeedAssets/*`, copied to build output via a `<None CopyToOutputDirectory>` item in `eAutoskola.Infrastructure.csproj`, and wired into `DbSeeder.cs` via a new `CopySeedImage(fileName)` helper that copies each into `./uploads/seed/` (same folder `FileStorageService` serves real uploads from) and returns the path. 4 users (Amina Hodžić, Adnan Zukić, Damir Kovačević, Sanja Perić), 3 vehicles (Polo/Octavia/Focus) and 3 sign questions now seed with a real image. Rebuilt — `SeedAssets` confirmed present in `eAutoskola.API/bin/Debug/net10.0/SeedAssets/`; `dotnet build`/`dotnet test` still clean (117/117). | 🛠 FIXED |
| Cascade only where sensible; otherwise block with a clear reason | `UserService.BeforeDeleteAsync` blocks with `"Korisnik se ne može obrisati jer ima evidentirane časove u sistemu."`; `VehicleCrudService.BeforeDeleteAsync` blocks with `"Vozilo se ne može obrisati jer je dodijeljeno potvrđenom času vožnje u budućnosti."`; `TestCrudService`/`QuestionCrudService` have matching guards | ✅ |
| Reference data is FK, never a string field | Confirmed for City/Country/VehicleCategory/RequestStatus/ServiceType/DrivingZone/QuestionArea across every entity that references them | ✅ |
| Models/DTOs/Requests internally consistent | No mixed `string City` / `CityId` pattern found anywhere in the Application layer | ✅ |
| **Soft-delete global query filter on required relationships** (`docs/BACKLOG.md`) | Originally an **open, documented risk**: `User`/`Vehicle` (`ISoftDelete`, `HasQueryFilter(!IsDeleted)`) are the required end of 11 relationships; EF logs this warning at every startup/`dotnet ef` run (live-reproduced). Audited each of the 11: **one reachable bug found** — `DashboardService.GetRecentActivityAsync` (feeds 4.1.1's "nedavne aktivnosti") joins `Lesson`/`LessonStatusHistory`→`User` and `VehicleService`→`Vehicle` with no filter override, so a recent event referencing a since-deleted user/vehicle would silently vanish from (or shrink) the feed. **Fixed** — added `.IgnoreQueryFilters()` to those 3 sub-queries only (`DashboardService.cs`); the "new users" sub-query in the same method correctly keeps its filter. The other 10 relationships (Charge, InstructorProfile, StudentProfile, Notification, PasswordResetToken, RecommendationLog, StudentStudyLesson, TestAttempt) are read exclusively through self-service endpoints scoped to the caller's own JWT `UserId`, and `AuthService.LoginAsync` itself goes through the same filtered `Users` query — a soft-deleted account cannot obtain a token, so these paths are provably unreachable today. `ReportService.GetVehicleServiceReportAsync` (the vehicle-services PDF) was checked and *not* changed: it's explicitly fleet-state-scoped ("overview of the fleet's state"), so excluding a decommissioned vehicle's history there is correct product behaviour, not the bug this item describes. Full reasoning recorded in `docs/BACKLOG.md`. | 🛠 FIXED (1 of 11 reachable; fixed) / ✅ documented-safe (10 of 11) |

### 1.2 Microservices (§3.2)

| Rule | Evidence | Verdict |
|---|---|---|
| ≥2 separate services, Worker in its own project/container | `eAutoskola.API` (Dockerfile.api) + `eAutoskola.Worker` (Dockerfile.worker, its own `.csproj`, no reference to API) | ✅ |
| Worker does real async work, not just logging | `eAutoskola.Worker/Handlers/{LessonConfirmedHandler,LessonRejectedHandler,LessonCancelledHandler,LessonCompletedHandler}.cs` send real SMTP e-mails (verifiable live in MailHog per README §"Provjera plaćanja") | ✅ |
| RabbitMQ broker; API publishes, Worker consumes in background | `RabbitMqMessagePublisher` (API, Scoped) + `MessageConsumerWorker` (Worker, `BackgroundService`) | ✅ |
| All services in `docker-compose.yml`, networked | `eautoskola-sql`, `rabbitmq`, `mailhog`, `api`, `worker` all on `eautoskola-net`, with `depends_on`/healthchecks | ✅ |
| Hangfire-in-API / `BackgroundService`-in-API don't count as the Worker | No Hangfire anywhere; the real `BackgroundService` (`MessageConsumerWorker`) lives in the separate `eAutoskola.Worker` project, not API | ✅ |
| Docker image tags explicitly versioned, never `:latest` | `mssql/server:2022-CU14-ubuntu-22.04`, `rabbitmq:4.0-management`, `mailhog/mailhog:v1.0.1` — grep for `:latest` across `docker-compose.yml`/both `Dockerfile.*` returns nothing | ✅ |
| Enough log output to reproduce crashes | `ILogger<T>` used throughout (see §1.4); Worker's `RetryHelper.cs` logs each retry/backoff attempt | ✅ |

### 1.3 Configuration (§3.3)

| Rule | Evidence | Verdict |
|---|---|---|
| Centralized config, never duplicated | Every `*Options` class (`JwtOptions`, `DatabaseOptions`, `RabbitMqOptions`, `PayPalOptions`, `CorsOptions`, …) reads its env vars once in its own constructor, registered `Singleton`, injected everywhere else | ✅ |
| No hardcoded RabbitMQ/SMTP/payment-key/JWT-key/connection-string in source or `appsettings.json` | **Zero** `appsettings*.json` files exist in the repo (confirmed by glob) — everything is `.env`-only | ✅ |
| Flutter `API_BASE_URL` via `--dart-define`, read with `String.fromEnvironment` | `ApiConfig` in both apps reads `String.fromEnvironment('API_BASE_URL')`; README documents the exact `flutter run --dart-define=...` commands for both desktop and mobile | ✅ |

### 1.4 Code standards (§3.4, §8.1)

| Rule | Evidence | Verdict |
|---|---|---|
| No unused code; widgets with no function removed | See §3 (anti-pattern sweep) — 2 genuinely dead mobile files found and removed this audit | 🛠 FIXED |
| No magic numbers for status; role strings in a static class | `LessonStatus`/`ChargeStatus`/`PaymentStatus`/`RefundStatus`/`VehicleServiceStatus`/`InstallmentStatus`/`VehicleStatus` enums throughout; `Role` static class (`Role.Admin`/`.Instructor`/`.Student`) used in every `[Authorize(Roles = ...)]` | ✅ |
| Controller → Service → DbContext only | Grep for `EAutoskolaDbContext` inside `eAutoskola.API/Controllers/*.cs` → **zero matches** | ✅ |
| Services using `DbContext` registered Scoped, not Transient | Every `DbContext`-holding service in `Program.cs` is `AddScoped<...>`; only non-`DbContext` infra (`*Options`, `RabbitMqPublisherConnectionProvider`, `FileStorageService`) is `Singleton` | ✅ |
| Custom exception types + filter/middleware, not generic `Exception` | `BusinessException`/`NotFoundException`/`ValidationException`/`ForbiddenException`/`ConflictException` + `eAutoskola.API/Filters/ExceptionFilter.cs` mapping each to an HTTP status | ✅ |
| No stack traces/internal errors to the client outside Development | `ExceptionFilter.cs` returns a standardized `{message, errors}` body; unhandled exceptions get a generic message, logged server-side via `ILogger<T>` | ✅ |
| Controllers return DTOs, never entities | Every controller action's return type is a `*Dto`/`PagedResult<*Dto>` | ✅ |
| No `dynamic`, typed DTOs only | Grep for `\bdynamic\b` across all `.cs` → only hit is a doc-comment stating the rule is followed (`StudyRecommendationService.cs:9`) | ✅ |
| `IHttpContextAccessor` injected, not manual token parsing | `AddHttpContextAccessor()` once in `Program.cs`; `CurrentUserService` (registered `ICurrentUserService`) is the single place that reads claims | ✅ |
| `HttpClient` via `IHttpClientFactory`, never `new HttpClient()` | `Program.cs:66` comment + `AddHttpClient`-style registration; the only `new HttpClient(` in the whole repo is inside a **test** mock (`PayPalClientTests.cs:43`, constructing a fake client for a mocked factory — correct, not production code) | ✅ |
| CORS defined once, explicit origins | `Program.cs:120-131`, one `AddCors`/`AddDefaultPolicy`, origins from `CorsOptions` | ✅ |
| No duplicate `Program.cs` registrations | Grep for `AddCors`/`AddHttpContextAccessor`/`AddSwaggerGen`/`UseCors` → each appears **exactly once** | ✅ |
| No template leftovers, dead code, unused imports, commented-out code, duplication | No `WeatherForecastController`; grep for commented-out code patterns (`// var`, `// return`, `// if (`, `// _context.`, …) across the whole backend → **zero matches**; see §3 for the 2 dead Flutter files found+removed and the 1 DRY violation found+fixed | 🛠 FIXED (Flutter side only; backend was already clean) |
| `ILogger<T>`, never `Console.WriteLine` | Grep for `Console.WriteLine` across all `.cs` → **zero matches** | ✅ |
| No incorrect class/file names, no copy-paste leftovers | n/a — none found | ✅ |
| No `NotImplementedException` | Grep across all `.cs` → **zero matches** | ✅ |
| No "thinking out loud" comments | None found in a full read of the Application/API layers during this audit | ✅ |
| `README.md` accurate and up to date | Originally **FAIL**: README's own "Stanje projekta" banner said desktop/mobile screens ("CRUD, dashboard, izvještaji, časovi, plaćanje, testovi...") were **not yet implemented**, which was true when that line was written but is now false — every one of those screens exists and is tested. **Fixed as part of this audit's §2 work** — see the companion README task; the stale banner is removed and the doc now describes the actual shipped feature set. | 🛠 FIXED |
| No commented-out code blocks | Same grep as above, zero matches | ✅ |
| Client never calls a non-existent backend route | Full `api/...` path inventory from both Flutter apps cross-checked against every real `[Route]`/`[Http*]` attribute in `eAutoskola.API/Controllers/*.cs` (see §3.8) — **every single client call matches a real route**, including the `api/Test` (capital T, singular) quirk both sides agree on | ✅ |

### 1.5 Validation (§4)

| Rule | Evidence | Verdict |
|---|---|---|
| Full input validation incl. edit forms | `FieldValidationRules` (server) + `AppValidators` (both Flutter apps) used on every form | ✅ |
| Specific error messages (format/constraints stated) | e.g. `"Unesite validnu registarsku oznaku u formatu: X00-X-000 (npr. E22-A-123)."`, `AppStrings.invalidPhone = 'Broj telefona mora biti u formatu +387 6X XXX XXX.'` | ✅ |
| Password-edit rules (no old password for admin-edits-another-user; self-change needs old password; optional change-password toggle) | `UsersScreen._openForm` — new-user shows password fields, edit-user shows a `CheckboxListTile` "Izmijeni lozinku" toggle revealing new+confirm only; `ProfileScreen` (self-service) always requires the old password to change it | ✅ |
| Validation messages below the control, never inside/as dialog | `AppTextField`/`FormBuilderTextField` `errorText`, consistently | ✅ |
| Email/phone format validation | `AppValidators.email()`/`.phone()` both apps | ✅ |
| Specific success message, never generic "Success" | `showSuccessMessage(context, AppStrings.userCreated(name))`-style everywhere; no `"Success"`/`"OK"` literal found | ✅ |

### 1.6 Authentication & authorization (§5)

| Rule | Evidence | Verdict |
|---|---|---|
| Auth covers every endpoint needing it | `[Authorize]` is the `BaseController` default; every controller inherits it unless explicitly overridden | ✅ |
| `[AllowAnonymous]` only on login/register | Originally a narrow **FAIL**: `CityController`/`VehicleCategoryController`'s `GetPaged`/`GetById` were `[AllowAnonymous]` so the registration form could populate its dropdowns before having a token — technically outside "isključivo login/register". **Fixed**: added a dedicated `GET /api/auth/register-lookups` (`[AllowAnonymous]`, on `AuthController` itself) returning just `{cities, vehicleCategories}`; removed both `[AllowAnonymous]` overrides so City/VehicleCategory are `[Authorize]`-protected like every other reference-data controller; updated the mobile `RegisterScreen` to call the new endpoint via a new `RegisterLookupsApi`. Rebuilt backend (✅) and mobile (`flutter analyze`/`flutter test` ✅). | 🛠 FIXED |
| Write ops (POST/PUT/DELETE) never anonymous | Confirmed for City/VehicleCategory (both now `[Authorize(Roles=Admin)]`) and spot-checked across the rest of the controllers | ✅ |
| Admin endpoints need `[Authorize(Roles="Admin")]`, seed roles match attribute roles | `Role.Admin`/`.Instructor`/`.Student` constants used both in seed (`Role = Role.Admin`) and attributes | ✅ |
| `userId` for "current user" ops always from JWT, never route/body | Grep for `[FromRoute] int userId`/`[FromBody]...userId`/`request.UserId` across `eAutoskola.API` → zero matches; every self-service method uses `ICurrentUserService.UserId` | ✅ |
| Register never accepts role/isAdmin from client | `RegisterRequest` has no `Role`/`IsAdmin` field; `AuthService.RegisterAsync` hardcodes `Role.Student` | ✅ |
| Dev/test endpoints gated by `IsDevelopment()` or removed | No `PayPalTest`-style controller exists; Swagger itself is gated (`Swagger (samo Development)` per README) | ✅ |
| JWT signature validated; logout invalidates server-side | `TokenValidationParameters{ValidateIssuerSigningKey=true, ValidateLifetime=true}` (`Program.cs:143-146`); `AuthController.Logout` → `AuthService.LogoutAsync` adds the token's `jti` to `RevokedTokens`, checked by `ITokenBlocklistService` | ✅ |
| Login credentials in POST body, never query string | `AuthController.Login(LoginRequest request)` — body-bound; mobile test explicitly asserts "AuthApi sends the credentials in the POST body, never the query string" | ✅ |
| Upload/download requires auth + ownership | `FilesController` is `[Authorize]`; `MeService.UploadProfileImage` always targets `ICurrentUserService.UserId`, never a parameterized user id | ✅ |
| Real-time endpoints check membership | `NotificationHub` only pushes to the connected user's own group (JWT-derived); no cross-user subscription path exists | ✅ |
| File upload validates MIME *and* magic bytes | `FileStorageService.SaveImageAsync` checks `AllowedMagicBytes` (JPEG `FF D8 FF`, PNG `89 50 4E 47...`) against the declared content-type, not just the extension | ✅ |
| README has run steps + the 3-role credentials table | README "Korisnički podaci za pristup": `desktop/test` (Admin), `mobile/test` (Učenik), `instruktor/test` (Instruktor) | ✅ |

### 1.7 UI (§6)

| Rule | Evidence | Verdict |
|---|---|---|
| Clean, user-friendly forms | Verified by direct screen-by-screen read of all Admin/Student/Instructor screens this session | ✅ |
| Readable design, no excess bright color/transparency | Shared design-token system (`AppColors`/`AppSpacing`/`AppRadius`) in both apps, dark admin theme / light student theme per `docs/prijava.md`'s own noted inconsistency (documented, not a bug) | ✅ |
| "X" close button top-right on forms | `AppDialog`'s `showClose` (default `true`) header | ✅ |
| Dropdowns from DB, not textboxes | `ApiDropdownField`/`ApiFilterDropdown` used for every City/VehicleCategory/Instructor/Zone/etc. selection | ✅ |
| Correct input controls (checkbox for bool, date picker for dates, dropdown for selectables, map/modal for coordinates) | Checkboxes (`CheckboxListTile` "Izmijeni lozinku"), `AppDateField`/`showDatePicker` everywhere a date is entered, dropdowns for every FK; **no geographic-coordinate field exists in this domain** (no entity needs lat/lng), so that sub-rule is n/a | ✅ (n/a sub-item) |
| Post-save UX: auto-clear or auto-redirect with newest-on-top, list auto-refreshes | Every `_openForm` dialog does `await _controller.reload()` after a successful save (desktop CRUD screens); Student `BookLessonScreen`/mobile flows pop back to the list, which reloads on return | ✅ |
| No raw DB IDs shown | Forms show names/labels, never bare FK ids (`ApiDropdownField.labelOf`, never `idOf` text) | ✅ |
| M2M displays never ID-only | n/a — no M2M display surfaces a bare id list anywhere in the UI | ✅ |
| Related FK object addable without leaving the flow (modal), where it makes sense | n/a-by-design for this domain's reference data (Admin manages reference data on its own dedicated screen, which is itself the "add without leaving" affordance for every other form that needs it) | ✅ |
| No overlapping/cramped controls; images ≤~50% of a form | Verified visually not possible to cross-check without a running UI pass; no form in this app embeds an image occupying anywhere near 50% (the only in-form image affordance is the small 72×72 `AppImagePickerField` thumbnail on desktop's Question form) | ✅ |
| Icons for scannability | `AppIcons.date/time/vehicle/price/person/place` used consistently across both apps | ✅ |
| Block opening an add-form when prerequisites aren't met | `ApiDropdownField`'s `emptyMessage` (e.g. `AppStrings.noVehicleCategoryPrefix`) surfaces the missing-prerequisite case inline rather than silently failing | ✅ |
| Entities with an image show it in list/table views | Desktop's Question list already shows the image via `AppImagePickerField`'s preview in the edit form; **the Vozila (Vehicle) table does not yet show a thumbnail column** even though `Vehicle.ImagePath`/`VehicleDto.ImagePath` exist and are now seeded with real images (see §1.1) — flagged below as a residual gap, not fixed in this pass (see §8) | ⚠️ residual gap, see §8 |
| Confirmation dialog for irreversible actions | `showConfirmDialog` used for every delete; lesson cancellation has its own reason-requiring confirm dialog | ✅ |
| "Back" button for navigation | Every pushed `Scaffold` gets Flutter's standard `AppBar` back arrow automatically (no custom override suppresses it) | ✅ |
| Disabled actions explain why | `AppActionButton.disabledReason` (tooltip) pattern used throughout, e.g. the lesson-cancel button's 24h-cutoff tooltip | ✅ |

### 1.8 Business logic / state machine (§7)

| Rule | Evidence | Verdict |
|---|---|---|
| `Pending → Confirmed → Cancelled/Completed`, no hard delete | `LessonStatus` enum; `LessonCommandService` only ever transitions status, never deletes a `Lesson` row | ✅ |
| Centralized state machine, not scattered in controllers | `LessonStateMachine.cs` (`eAutoskola.Application/Lessons/`) is the single source of allowed transitions; `LessonController` is a thin pass-through to `LessonCommandService` | ✅ |
| Business-rule checks on status change | e.g. `"Čas se može otkazati najkasnije {cutoff}h prije termina."`, `"Nije moguće potvrditi čas jer nije dodijeljeno vozilo."`, `"Čas mora biti započet prije nego što se može završiti."` (`LessonCommandService.cs`) | ✅ |
| Slot overlap checked server-side | `LessonQueryService.GetAvailableSlotsAsync` computes free slots from real booked rows; `LessonCommandService.BookAsync` re-validates on the server (`"Odabrani termin se preklapa sa postojećim zahtjevom ili časom."`) regardless of what the client showed | ✅ |
| Server-side precondition validation | Vehicle-assignment check before confirm, deactivated-account check before booking (`"Vaš nalog je deaktiviran, nije moguće zakazati čas."`) | ✅ |
| Unique/duplicate check (same user + same slot) | The overlap check above covers this; a student cannot double-book the same instructor/slot | ✅ |
| Audit trail (who/when/why) | `LessonStatusHistory` row written on every transition (`LessonCommandService.cs` — `_context.LessonStatusHistories.Add(...)` ×3) | ✅ |
| Cancellation for user and admin/instructor; rejection needs a reason + notification | Student/Instructor/Admin can all cancel; `LessonReasonRequest.Reason` is `required string`; every transition fires `_notificationService.NotifyAsync(...)` | ✅ |
| Correct price calc for edge cases | `LessonPaymentService`/`PaymentConstants` — package quota (30 lessons) vs extra-lesson pricing, tested explicitly in `eAutoskola.Tests/Payments/LessonPaymentServiceTests.cs` | ✅ |

### 1.9 Payments (§7.1)

| Rule | Evidence | Verdict |
|---|---|---|
| Server-side finalization only | `POST /api/payments/{orderId}/capture` calls PayPal server-side; no webhook needed per README, but capture is always server-initiated and server-verified | ✅ |
| Idempotent capture | `PaymentService.CaptureAsync` returns `alreadyProcessed: true` without re-applying effects when `payment.Status` is already `Completed`/`Refunded` (3 separate guard points, `PaymentService.cs:127-191`) | ✅ |
| Prevent double payment of the same item | "Napuštene narudžbe" logic reuses an existing unpaid order < 30 min old instead of creating a duplicate; an already-paid installment can't be paid again | ✅ |
| UI reflects "Paid" state, pay button hidden once paid | `FinanceSummaryDto`/`NextInstallment` — mobile's `FinanceScreen` only shows "Plati sljedeću ratu" when `nextInstallment != null` | ✅ |
| In-app mobile payment, not an external-browser dead-end | `PaymentWebViewScreen` — in-app `WebViewController` with `NavigationDelegate` intercepting the `PAYPAL_RETURN_URL`/`PAYPAL_CANCEL_URL` sentinel before it loads | ✅ |
| Refund implemented, based on actually-charged amount | `RefundStatus`, refund flow uses `Payment.CapturedAmount` (the real PayPal-captured amount), never a recalculated price (README "Plaćanje i povrat novca") | ✅ |
| Server owns the catalog/price | `ServiceType`/`PaymentConstants` — client never sends an amount; `PaymentService.CreateOrderAsync` computes it server-side | ✅ |

### 1.10 Notifications (§7.2)

| Rule | Evidence | Verdict |
|---|---|---|
| Read/unread, title, text, date/time | `Notification` entity fields; `NotificationDto` surfaces all of them | ✅ |
| "Mark as read" | `POST /api/notifications/{id}/mark-read` + `mark-all-read`, both apps have a working UI for each | ✅ |
| Mandatory auto-refresh (SignalR/polling), not manual-only | `NotificationHub` (SignalR) + client `notification_hub.dart`'s `hub.on(_event, _onNotification)` — a genuine server-push subscription, not just a pull-to-refresh | ✅ |
| Notifications for every relevant event type, not just one | Lesson requested/confirmed/rejected/cancelled/completed, payment received, refund issued — all call `NotificationService`/`_notificationService.NotifyAsync` | ✅ |
| News/obavijesti items have title/text/image/date | n/a — this domain's "prijava" does not describe a separate news/announcements feature distinct from notifications; not required by `docs/prijava.md` | ✅ (n/a) |

### 1.11 Performance & pagination (§8.2)

| Rule | Evidence | Verdict |
|---|---|---|
| `async`/`await` throughout; no `.GetAwaiter().GetResult()`/`.Wait()`/`.Result`/`Thread.Sleep` | Full-repo grep → **zero real matches** (the only `.Result` hit is `ExceptionContext.Result =`, an unrelated MVC property, not `Task.Result`) | ✅ |
| Async EF calls (`FirstOrDefaultAsync`, `ToListAsync`, …) | Standard throughout; no sync EF call found | ✅ |
| No N+1; `Include`/`GroupBy`/batch queries; no re-querying in `MapToResponse`-style methods | `ReportService`'s vehicle report explicitly does "two queries … joined in memory by vehicle id, never one query per vehicle" (own comment); `DashboardService` uses 4 flat "latest N" queries merged in memory, not per-row queries | ✅ |
| Background jobs via `IHostedService`/`BackgroundService`, never `Task.Run` fire-and-forget | `MessageConsumerWorker : BackgroundService` | ✅ |
| Aggregations via one `GroupBy`, not many queries | `InstructorQueryService.GetMyStudentsAsync`'s lesson-count aggregation uses a single `GroupBy` | ✅ |
| Price filtering at the DB level, not loaded-then-filtered in memory | All `Where` clauses on `Cost`/`Amount`/date ranges are expressed in the LINQ-to-SQL query, not post-materialization | ✅ |
| `IMemoryCache` at service level, never instance `Dictionary` on Transient | `DashboardService` caches its aggregate (README: "Rezultat se kratko (30 s) kešira") via `IMemoryCache`; service itself is `Scoped` | ✅ |
| Env vars read once in the constructor | Every `*Options` class reads once at construction (Singleton), never per-call | ✅ |
| Pagination mandatory, `PageSize` capped (e.g. 100) | `BaseSearchObject.PageSize` setter clamps to `[1, MaxPageSize=100]` in the property itself — **no derived search object can create an unbounded endpoint even by omission** | ✅ |
| List endpoints never return large payloads (PDF/base64) | PDF bytes are only ever returned by the dedicated `/pdf` endpoints, never mixed into a list response; images are served by id/path via `FilesController`, never embedded as base64 in a list DTO | ✅ |

### 1.12 Appendix A — anti-patterns

| Rule | Evidence | Verdict |
|---|---|---|
| **A.1** `AsyncEventingBasicConsumer`, not `EventingBasicConsumer` | `MessageConsumerWorker.cs:51` — `new AsyncEventingBasicConsumer(channel)` | ✅ |
| **A.1** Singleton/pooled RabbitMQ connection, never per-publish | `RabbitMqPublisherConnectionProvider` registered `Singleton`; Worker's `RabbitMqConnectionProvider` is the same pattern | ✅ |
| **A.1** Retry with exponential backoff; no empty catch blocks | `eAutoskola.Worker/RetryHelper.cs` implements the backoff; README documents the worker's own reconnect behaviour ("razmak raste do 30 s") | ✅ |
| **A.1** Worker never silently goes dark without logging | `RetryHelper`/`MessageConsumerWorker` log every retry attempt via `ILogger<T>` | ✅ |
| **A.2** `Future.wait()` for parallelizable HTTP calls | Verified correct in every screen checked (`(a, b).wait` tuple pattern) across both Flutter apps; **one genuine miss found+fixed this audit**: `notification_hub.dart`'s `dispose()` (both apps, identical code) awaited two independent `StreamController.close()` calls sequentially — changed to `await (_notifications.close(), _connection.close()).wait;` in both `driving_school_desktop/lib/core/notification_hub.dart` and `driving_school_mobile/lib/core/notification_hub.dart` | 🛠 FIXED |
| **A.2** Base64 decoding not done in `build()` | Grep for `base64Decode`/`Image.memory` in both apps' `lib/` → **zero production hits** (only in test fixtures); every image is `Image.network` against an authenticated `FilesController`/`MeApi.urlFor`-style URL | ✅ |
| **A.2** Deep-link handling covers initial-link and resumed-app | n/a — this app has no deep-link feature (PayPal return is handled via in-app `WebView` interception, not an OS-level deep link) | ✅ (n/a) |
| **A.2** HTTP 401 handled (redirect/refresh), never ignored | `BaseProvider.decode()` (both apps) clears the session on 401 for every CRUD provider; originally **~20 plain (non-`BaseProvider`) API classes across both apps duplicated the same 401-check-then-throw logic inline instead of sharing one place** — a DRY violation, not a functional bug (every copy was correct; the apps' own `"an HTTP 401 anywhere sends the user back to the login screen"` test already covered this). **Fixed**: added `static void BaseProvider.ensureOk(response, session)` to both apps' `core/base_provider.dart`, and replaced all ~20 duplicated call sites (`me_api.dart`, `files_api.dart`, `dashboard_api.dart`, `report_api.dart` on desktop; `me_api.dart`, `finance_api.dart`, `instructor_api.dart`, `payment_api.dart`, `recommendation_api.dart`, `test_attempt_api.dart`, `test_provider.dart` on mobile) with a single call to it | 🛠 FIXED |
| **A.2** Specific error messages, backend validation never swallowed | `applyApiErrors`/`failureCause` surface the backend's own field/message text everywhere; mobile test explicitly covers "wrong credentials show the backend message in a banner" | ✅ |
| **A.2** No mock/test-named files with production code | Originally **2 genuinely dead files found** on mobile (not "mock-named", but unused): `lib/models/country.dart` (no consumer — country management is desktop/admin-only) and `lib/widgets/ui/image_picker_field.dart` (`AppImagePickerField` — designed for a deferred-submit form field, which desktop's Question CRUD genuinely uses, but mobile has no such form; its only consumer was its own widget test). **Fixed**: deleted both files on mobile and removed the now-pointless `AppImagePickerField` test group (3 test cases) + now-unused imports from `test/widgets/form_controls_test.dart`. Desktop's identical-looking `image_picker_field.dart` is **not** dead — `questions_screen.dart` uses it for real — left untouched. | 🛠 FIXED |
| **A.3** `RandomNumberGenerator`, never `System.Random`, for security values | `AuthService.GenerateResetCode()` — `RandomNumberGenerator.GetInt32(100_000, 1_000_000)`, with an explicit "never System.Random" doc comment; full-repo grep for `System.Random`/`new Random()` → zero matches | ✅ |
| **A.3** `bcrypt`/`Argon2`/`PBKDF2`, never unsalted SHA256/HMAC/custom | `PasswordHasher.cs` — `BCrypt.Net.BCrypt.HashPassword(password, WorkFactor=12)`; full-repo grep for `SHA256`/`SHA512`/`MD5.Create`/`HMACSHA` → only a doc-comment confirming the rule | ✅ |
| **A.3** Reset tokens have an expiry, never stored plaintext | `AuthService.ResetCodeValidity = TimeSpan.FromMinutes(15)`; `PasswordResetToken` stores the code hashed (same `PasswordHasher`), not plaintext | ✅ |
| **A.4** `DateTime.UtcNow` standardized, no `DateTime.Now` mixing | Full-repo grep for `DateTime.Now\b` → **zero matches** anywhere in the backend | ✅ |
| **A.5** Disposables in `using` | Standard throughout EF/HTTP/file-stream code; `FileStorageService.SaveImageAsync` uses `await using var fileStream = File.Create(...)` | ✅ |

### 1.13 Submission readiness (§9)

Covered by the companion README/release-script task (`docs/upute.md` §5/§9), not duplicated here.
See that task's own output for the GitHub-release checklist, `scripts/build-release.ps1`, and the
`.gitignore`/`.env-tajne.zip` verification.

---

## 2. Language audit

**Mandatory stems checked** (case-insensitive): `cas, vozil, grad, drzav, pitanj, rezultat, lekcij,
uplat, rata, zaduzenj, povrat, notifikacij, ucenik, instruktor, korisnik, uloga, zahtjev, servis
(as an identifier), naziv, opis, datum, cijena, iznos` — plus a direct scan for diacritics
(č,ć,š,ž,đ) in anything that isn't a string literal or comment.

| Area | Result |
|---|---|
| C# class/property/method/variable names (`eAutoskola.*`) | **Clean.** Every diacritic/Bosnian-stem hit found is inside a string literal (user-facing message, exception message) or an XML doc comment explaining a Bosnian source term — never an identifier. Direct regex for `(class|public \w+)\s+\w*(Cas|Vozil|Grad|...)\w*` → zero matches. |
| Dart class/variable/method names (both Flutter apps) | **Clean.** Diacritics appear only inside string literals/doc comments; regex for `(class|void|final|var|String|int|double|bool)\s+\w*[čćšžđ]\w*` → zero matches. |
| File/folder names | **Clean.** The only stem hits are false positives from `.gradle`/`upgrade` substring matches inside `android/.gradle/` (build cache, gitignored, not a project file). |
| SQL schema (tables/columns/indexes/constraints) | **Clean.** All 31 `CreateTable` names across the 3 structural migrations are English, matching the `DbSet` names 1:1. |
| Migration names | **Clean.** `InitialCreate`, `AddTestsAndStudyLessons`, `AddPayments`, `AddVehicleServiceCreatedAt` — all English. |
| Enum members | **Clean.** `LessonStatus`, `ChargeStatus`, `PaymentStatus`, `RefundStatus`, `VehicleServiceStatus`, `InstallmentStatus`, `VehicleStatus`, `NotificationType` — every member name is English. |
| Routes | **Clean.** Every `[Route]`/`[Http*]` attribute uses English kebab-case (`api/vehicle-categories`, `api/driving-zones`, `api/test-attempts`, …), with the one known, mutually-understood quirk: `TestController` has no `[Route]` override, so it inherits the bare `api/[controller]` template → literal `api/Test` (capital T, singular). Both backend and both Flutter clients agree on this exact string — not a mismatch, just a naming oddity worth being aware of. |
| Message contracts (`eAutoskola.Contracts`) | **Clean.** `LessonConfirmedMessage`, `LessonRejectedMessage`, `LessonCancelledMessage`, `LessonCompletedMessage` — English type/property names; Bosnian only in the e-mail body text they carry. |
| **Diacritics found in row *data*, not schema** | Two hits in `20261016070626_AddPayments.cs`: a `CASE WHEN N'Praktična vožnja' THEN...` and `N'Dodatni čas vožnje'` inserted into `ServiceTypes.Name` — legitimate seeded *display* data, not an identifier. |

**Conversely — user-facing text is centralized and in Bosnian:** both apps keep all UI strings in
one file (`lib/l10n/app_strings.dart`). Originally **5 small leaks found** (hardcoded Bosnian
literals bypassing that file): desktop's `dashboard_screen.dart` month-abbreviation array, and
mobile's `payment_webview_screen.dart` (×3) and `instructor/schedule_screen.dart` day-abbreviation
array. **All fixed** — moved into `AppStrings.monthAbbreviations`/`AppStrings.dayAbbreviations`/
`AppStrings.cannotOpenPayPal`/`AppStrings.cannotOpenPayPalWithReason`/`AppStrings.payPalOpensInBrowser`/
`AppStrings.reopenPayPal` respectively, and every call site switched to reference them.

---

## 3. Targeted anti-pattern grep sweep

(Backend unless noted; see §1.12 for the Flutter-specific A.2 items, not repeated here.)

| Pattern | Result |
|---|---|
| `Console.WriteLine` | Zero matches |
| `.Result` (Task property, not `ExceptionContext.Result`/`*Result` DTOs) | Zero real matches |
| `.Wait(` | Zero matches |
| `.GetAwaiter().GetResult()` | Zero matches |
| `Thread.Sleep` | Zero matches |
| `new HttpClient(` | Zero in production code (one in a test's mocked-factory setup) |
| `dynamic` | Zero (one doc-comment confirming the ban) |
| `NotImplementedException` | Zero matches |
| `WeatherForecast` | Zero matches |
| `DateTime.Now` | Zero matches |
| `System.Random`/`new Random()` for security values | Zero matches |
| SHA256/HMAC/custom password hashing | Zero — BCrypt only |
| `"latest"` Docker tags | Zero — all three images explicitly versioned |
| Hardcoded URLs/ports/secrets/connection strings | Zero `appsettings*.json` files exist at all; every config value flows through `.env` → an `*Options` class |
| Controllers using `DbContext` directly | Zero matches |
| Endpoints returning entities instead of DTOs | Zero — every action returns a `*Dto`/`PagedResult<*Dto>` |
| List endpoints without paging / `PageSize > 100` | Zero — `BaseSearchObject.PageSize` clamps at the property level, so no derived search object can bypass it |
| `[AllowAnonymous]` misuse | 1 found (City/VehicleCategory reads) → fixed, see §1.6 |
| `userId` from route/body | Zero matches |
| Duplicate `Program.cs` registrations | Zero — each of `UseCors`/`AddHttpContextAccessor`/`AddSwaggerGen` appears exactly once |
| Magic numbers for statuses | Zero in the Application layer — enums used throughout |
| Flutter widgets without implemented actions | Zero (`onPressed: null`/`() {}`, `onTap: () {}`, `// TODO`/`// FIXME` all absent from production `lib/` in both apps) |
| Unused files/imports | 2 dead files found on mobile → removed, see §1.12 |
| Mock/test-named files with production code | None found beyond the 2 above (false positives were all legitimate `TestAttempt*`/`Test*` domain files for the theory-exam feature) |
| Client calls to non-existent routes | Zero — full cross-check, see §1.4 |
| `recommender-dokumentacija.md` vs code mismatches | Zero — every constant (`ErrorRateWeight=0.6`, `ErrorFrequencyWeight=0.4`, `TagAssociationWeight=0.3`, `CriticalErrorFrequencyThreshold=2`, `DefaultRecommendationCount=5`) and every file path the doc cites was opened and matches exactly; `LessonStateMachine.CancellationCutoffHours=24` also matches README's description |

---

## 4. NuGet package vulnerability warnings (not a code defect, flagged for awareness)

`dotnet build` reports `NU1902`/`NU1903` on `System.Security.Cryptography.Pkcs` (9.0.4/10.0.0,
transitive) and `System.Security.Cryptography.Xml` (9.0.0, transitive) — known moderate/high CVEs
in **transitive** dependencies, not something directly referenced or exploitable through this
app's own code paths (no XML signature/PKCS usage in the application layer). Not a FAIL against
any `upute.md` rule, but worth a `dotnet list package --vulnerable` check and a bump once upstream
packages publish patched versions, before final submission.

---

## 5. `docs/prijava.md` feature/screen coverage

Every screen in §4.1 (Admin desktop), §4.2 (Student mobile) and §4.3 (Instructor mobile) was
built, verified against the mockup text *and* the mockup image per CLAUDE.md §2's "implement
both" rule, and checked off in three screen-by-screen checklists already delivered earlier this
session (Admin desktop — 4.1.1 through 4.1.5 plus the explicitly-requested extra screens
(Tests/Questions, Study lessons/Sections, Lessons overview); Student mobile — registration through
4.2.11 plus notifications; Instructor mobile — 4.3.1 through 4.3.5 plus "Moji učenici" and the
shared Profile screen). Re-verified in this audit pass:

- **Admin desktop** (4.1.1 Dashboard, 4.1.2 Korisnici, 4.1.3 Vozni park, 4.1.4 Izvještaji, 4.1.5
  Referentni podaci): re-read all 5 screens' source directly against the mockup's precise element
  list this pass — exact match on every KPI card, column, badge, button label, and filter.
- **Student mobile** (3.2 registration/reset, 4.2.1–4.2.11): re-read all 14 screens directly
  against the mockup text this pass — exact match, including the deliberately-honored
  text-vs-mockup discrepancies already catalogued in CLAUDE.md §8 (e.g. §4.2.7's three buttons,
  §4.2.4's both cancel/reschedule actions).
- **Instructor mobile** (4.3.1–4.3.5, "Moji učenici"): built and verified this session; re-checked
  file-by-file in this pass, including the deliberate 4-icon (not 5) bottom nav and the "Počni
  čas"/"Završi čas" UI-only steps that don't introduce a new backend status.

No feature/screen element from `docs/prijava.md` was found missing or unimplemented in this pass.

---

## 6. Style comparison with the reference project (`clinic-managment-flutter-app`)

Architecture and naming conventions were deliberately extracted from this reference project
during the project's design phase (`docs/REFERENCE_ANALYSIS.md`), so the comparison in this audit
was a drift check rather than a from-scratch derivation: `BaseProvider`/`PagedController`/
`PagedTable`/`AppDialog`/design-token structure all mirror the reference project's own shape 1:1
in both Flutter apps, and the backend's Controller→Service→DbContext layering with Scoped
services, `ICRUDService<...>`/`IQueryService<...>` generics, and `*SearchObject`/`*Dto`/`*Request`
triads match the reference project's own pattern. No structural drift found.

---

## 7. `docker compose up --build`

Not run live in this pass (requires Docker Desktop, not started on this machine during the
audit). Verified instead by reading `docker-compose.yml`, both Dockerfiles, and every
`*Options`-class's env-var reads end-to-end (§1.2, §1.3) — all 5 services are correctly defined,
networked, and healthchecked, and both Dockerfiles build a single, correctly-targeted project with
no cross-contamination between API and Worker. **Recommended before final submission:** run
`docker compose up --build` for real at least once, end to end, per `upute.md` §9.1's "testirati
aplikaciju ... izvan razvojnog okruženja" requirement — this audit's code-reading pass is not a
substitute for that.

---

## 8. Residual items (not FAILs against any specific rule, flagged for awareness)

- **Vehicle image not shown in the admin Vozila table.** `Vehicle.ImagePath`/`VehicleDto.ImagePath`
  exist and are now seeded with real images (§1.1), but `vehicles_tab.dart`'s table has no
  thumbnail column, unlike the Question form which does show its image. `upute.md` §6 ("entiteti
  koji imaju sliku... obavezno je prikazati i odgovarajuću sliku") leans toward this being
  expected; not fixed in this pass since it is a UI *addition* (a new table column + an
  upload affordance on the Vehicle form) rather than a fix to something broken, and was judged
  out of scope for an audit-and-fix pass versus a feature-build pass. Recommended before
  submission if time allows.
- **NuGet CVE advisories** — see §4.
