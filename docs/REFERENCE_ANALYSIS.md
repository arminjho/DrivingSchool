# REFERENCE_ANALYSIS.md

Deep implementation-level analysis of the reference repository, used to decide eAutoškola's own architecture and to set concrete coding patterns. Source: `clinic-managment-flutter-app` (ClinicNow), a same-course, already-accepted project, surveyed **read-only** — nothing in it was modified. Template scaffold `RSII-Template-APP` (ManiFest) is referenced where relevant as the course-provided starting point ClinicNow itself extended.

See `CLAUDE.md` for the source-of-truth order (`docs/upute.md` > `docs/prijava.md` > this file > own judgement). This file documents *how to structure code*, never *what features to build* — eAutoškola's own domain comes only from `docs/prijava.md`.

> Section 3 documents the approved architecture decision for eAutoškola (Clean Architecture project names, CQRS via direct Query/Command interfaces — no MediatR, no `IApplicationDbContext` — matching the reference project's own mechanics as closely as possible per explicit instruction). Everything else is factual analysis of the reference repository.

---

## 1. Solution/project layout & naming conventions

### Backend — five-project split (confirmed, not guessed — read from `.sln`/`.csproj`)

| Project | Role | Key folders |
|---|---|---|
| `ClinicNow.API` | ASP.NET Core Web API, entry point only | `Controllers/` (+ `BaseController`, `BaseCRUDController`), `Filters/ExceptionFilter.cs`, `RateLimiterPolicies.cs`, `Program.cs` |
| `ClinicNow.Model` | Shared contracts, **zero package references** (pure POCO project) | `Common/` (enums, `PagedResult<T>`), `Configuration/` (`*Options : EnvOptionsBase`), `Dto/`, `Requests/`, `SearchObjects/`, `Exceptions/`, `Messaging/` (shared message contracts), `Security/Roles.cs` |
| `ClinicNow.Services` | Business logic + EF Core | `Database/` (`DbContext`, `Entities/`, `Configurations/`, `Migrations/`, seeder), one feature folder per domain area holding `I<Name>Service`+`<Name>Service` side by side, `Messaging/` (RabbitMQ **publishers**), `Mapping/` (Mapster), `Security/` (hashing, token blocklist), `Validation/` (shared validators), `Reports/` (QuestPDF) |
| `ClinicNow.Worker` | Separate deployable, no web server | `Messaging/` (consumer connection), one `BackgroundService` per queue, `Mail/`/`Push/` senders |
| `ClinicNow.Tests` | xUnit, mirrors `Services` feature folders 1:1 | `TestSupport/` |

Namespace = project name, sub-namespace mirrors folder path, file-scoped `namespace X;` throughout.

### Flutter — two fully separate packages (confirmed)

`UI/clinicnow_desktop` (Windows, admin/staff) and `UI/clinicnow_mobile` (Android, patient) — **not** a monorepo shared package; each has its own `pubspec.yaml`; small cross-cutting files (`core/api_http.dart`, `core/base_provider.dart`, `core/design_tokens.dart`, `core/app_theme.dart`, `layouts/app_shell.dart`) are duplicated verbatim per app rather than shared via a `melos`/package-import setup. Both share the same internal skeleton: `lib/core/`, `lib/layouts/`, `lib/models/`, `lib/providers/`, `lib/screens/<feature>/`, `lib/widgets/ui/`, `lib/main.dart`.

### Naming conventions to adopt as-is

**C#:** Controllers `<Entity>Controller`; Services `<Entity>Service`+`I<Entity>Service`; output DTOs suffix `Dto`; input payloads suffix `Request` (`<Entity>InsertRequest`/`<Entity>UpdateRequest`); filter objects suffix `SearchObject` extending `BaseSearchObject`; entities bare noun under `Database/Entities/` with a matching `<Entity>Configuration` under `Database/Configurations/`; config classes suffix `Options` deriving `EnvOptionsBase`; exceptions suffix `Exception` with one shared base; Mapster registrations suffix `MappingConfig`.

**Flutter:** files snake_case, classes PascalCase; models bare noun (`lesson.dart` → class `Lesson`); providers `<entity>_provider.dart`; screens `<feature>_screen.dart`/`<action>_dialog.dart` grouped in feature subfolders; shared design-system widgets in `widgets/ui/`, prefixed `app_`.

---

## 2. Backend implementation patterns (confirmed from code, to adopt)

### 2.1. Base CRUD pattern — exact shapes to replicate

```csharp
// API
[ApiController, Route("api/[controller]"), Authorize]
public abstract class BaseController<TModel, TSearch> : ControllerBase
    where TSearch : BaseSearchObject
{
    protected readonly IService<TModel, TSearch> Service;
    [HttpGet]
    public virtual async Task<ActionResult<PagedResult<TModel>>> GetPaged([FromQuery] TSearch criteria, CancellationToken ct)
        => Ok(await Service.GetPagedAsync(criteria, ct));
    [HttpGet("{id:int}")]
    public virtual async Task<ActionResult<TModel>> GetById(int id, CancellationToken ct) { ... }
}

public abstract class BaseCRUDController<TModel, TSearch, TInsert, TUpdate> : BaseController<TModel, TSearch>
{
    // POST / PUT / DELETE, each [EnableRateLimiting(RateLimiterPolicies.Write)]
}
```

> **Gotcha worth preserving:** the `[FromQuery]` parameter is named `criteria`, never `search` — ASP.NET Core's model binder silently drops filter properties when a query key collides with the action parameter's own name.

```csharp
// Services
public interface IService<TModel, in TSearch> where TSearch : BaseSearchObject {
    Task<PagedResult<TModel>> GetPagedAsync(TSearch search, CancellationToken ct = default);
    Task<TModel?> GetByIdAsync(int id, CancellationToken ct = default);
}
public interface ICRUDService<TModel, in TSearch, in TInsert, in TUpdate> : IService<TModel, TSearch> {
    Task<TModel> InsertAsync(TInsert request, CancellationToken ct = default);
    Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default); // soft if ISoftDelete, else hard
}
```

`BaseService` holds `Context`+`Mapper`; `GetPagedAsync` = `ApplyFilter` → `CountAsync` (before paging) → `ApplySorting` (validated against an allowlist, else throws `ValidationException` — never an "is this sortable" oracle) → `Skip/Take` → `AsNoTracking().ToListAsync()` → `Mapper.Map<List<TModel>>`. `BaseCRUDService` adds `Before*Async`/`After*Async` hooks per entity, soft-deletes via an `ISoftDelete` marker interface else hard-deletes, and translates `DbUpdateException` (FK violation) into a `BusinessException` with a Bosnian user-facing message.

### 2.2. Paging/search shape (adopt exactly — already matches `upute.md` §8.2's 100-cap rule)

```csharp
public class PagedResult<T> { public List<T> ResultList { get; set; } = []; public int Count { get; set; } }

public abstract class BaseSearchObject {
    public int Page { get; set; } = 1;               // clamped >= 1
    public int PageSize { get; set; } = 10;           // clamped to [1, MaxPageSize]
    public const int MaxPageSize = 100;                // hard cap, enforced in the setter itself
    public string? OrderBy { get; set; }
    public string? SortDirection { get; set; }
    protected virtual ISet<string> SortableColumns { get; } = new HashSet<string>();
    public bool IsSortable(string column) => SortableColumns.Contains(column);
}
```

Clamping `PageSize` **inside the setter** means no derived search object can accidentally create an unbounded "get all" endpoint — this directly satisfies `upute.md` §8.2 ("PageSize mora imati definisan maksimalni limit... Endpointi tipa RetrieveAll bez limita smatraju se greškom").

### 2.3. Mapping — Mapster (adopt)

`Mapster` + `Mapster.DependencyInjection`, `TypeAdapterConfig.GlobalSettings.Scan(assembly)` auto-discovers every `IRegister`. One `*MappingConfig.cs` per domain area, each only overriding the handful of properties convention-mapping can't infer (denormalized names, flattening):

```csharp
public class CodebookMappingConfig : IRegister {
    public void Register(TypeAdapterConfig config) {
        config.NewConfig<Location, LocationDto>()
            .Map(dest => dest.CityName, src => src.City != null ? src.City.Name : string.Empty);
    }
}
```

Chosen over AutoMapper for lower overhead and because the accepted reference uses it successfully — no reason to introduce a second mapping library.

### 2.4. Validation — hand-written, no FluentValidation/DataAnnotations (adopt)

Zero `FluentValidation`/`AbstractValidator`/`[Required]` anywhere in the reference. Two patterns:
1. **Ad hoc in-service** — e.g. `if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("name", "Naziv grada je obavezan.");`
2. **Shared rules helper** — `Services/Validation/ContactRules.cs`: static helpers (`RequireEmail`, `OptionalPhone`, `RequireText`, `RequirePassword`) appending to an `IDictionary<string,string[]> errors` bag, using regex + column-length constants mirrored from the EF configuration, with Bosnian messages that *state the exact constraint* — directly satisfies `upute.md` §4 ("Poruke o grešci moraju eksplicitno navoditi format i ograničenja unosa"). Thrown as one `ValidationException(errors)`, surfaced by the client as `{ "errors": { "field": ["message"] } }` — maps cleanly onto `flutter_form_builder`'s per-field error display, satisfying the "ispod kontrola, ne kao dijalog" UI rule too.

**Decision: adopt this pattern as-is** (a `FieldValidationRules` static class, same shape as `ContactRules`) rather than introducing FluentValidation — zero extra dependency, and the error shape it produces already matches what both UI rules require.

### 2.5. Exception middleware & hierarchy (adopt)

`Filters/ExceptionFilter.cs`, an MVC `ExceptionFilterAttribute` (not middleware) registered via `options.Filters.Add<ExceptionFilter>()`:

| Exception | HTTP status |
|---|---|
| `ValidationException` (dict of field→messages) | 400 |
| `BusinessException` | 400 |
| `NotFoundException(entityName, key)` | 404 |
| `ForbiddenException` | 403 |
| `AuthenticationException` | 401 (distinct from 403, so the client can special-case "bad login" vs "not allowed") |
| anything else | 500, generic message in Production, full `.ToString()` only in Development |

`ClinicNowException`-derived → `LogWarning`; anything else → `LogError` with full stack. Directly satisfies `upute.md` §3.4 (custom exception types + filter/middleware mapping) and §3.4's "no stack traces to the client outside Development" rule.

### 2.6. Auth — JWT + DB-backed revocation + password-change cutoff (adopt)

- Standard `AddJwtBearer` validating issuer/audience/signing key/lifetime (30s clock skew).
- `OnTokenValidated`: (1) `jti`-based blocklist lookup — a real `RevokedToken` DB table (not purely in-memory), **memoized through `IMemoryCache`** (negative TTL 60s, positive TTL 30min — asymmetric since revocation never un-revokes) because this check runs on every request; (2) a per-user `TokensValidFromUtc` cutoff so a password change invalidates every previously issued token at once, also cached.
- Logout calls `RevokeAsync(jti, expiresAtUtc)`, idempotent, immediately promotes the cache entry so the revocation is visible on the very next request — satisfies `upute.md` §5 ("Logout mora invalidirati token na serveru, a ne samo lokalno obrisati token").
- Roles as a static class of string constants (`Roles.Administrator` etc.), never literals in `[Authorize(Roles=...)]` attributes.
- Base controller is `[Authorize]` by default; only login/register/forgot-password/reset-password are `[AllowAnonymous]`.

**For eAutoškola:** same mechanism, roles renamed to `Role.Admin`/`Role.Instructor`/`Role.Student` per `CLAUDE.md`'s glossary.

### 2.7. RabbitMQ wiring (adopt, with one addition noted in §5)

- **Publisher:** singleton connection provider (double-checked lock via `SemaphoreSlim`), initial connect wrapped in retry-with-backoff. Publish via the **default exchange** (routing key = queue name, no custom exchange needed for this scale), `durable: true`, `Persistent = true`, **publisher confirmations enabled** (`CreateChannelOptions(publisherConfirmationsEnabled: true, ...)` — an awaited `BasicPublishAsync` means the broker acked, not just "bytes on the socket"). Publish failures are caught, logged, return `false` — a broker outage must never break the triggering API request.
- **Consumer:** separate connection provider (Worker has no project reference to Services), one `BackgroundService` per queue, `AsyncEventingBasicConsumer` (never the deprecated sync consumer — satisfies `upute.md` Appendix A.1 exactly), manual ack (`autoAck: false`), retry-with-exponential-backoff per message via a shared `RetryHelper`, failures always logged (never an empty catch), `BackgroundServiceExceptionBehavior.StopHost` so an unrecoverable connection failure kills the container rather than leaving a zombie (relies on `restart: unless-stopped`).

### 2.8. Docker (adopt the shape)

- Multi-stage `Dockerfile`: `sdk:10.0` build stage (copy `.csproj`s, `restore`, copy source, `publish`), `aspnet:10.0` (API) or `runtime:10.0` (Worker, no web server) runtime stage, non-root `USER app`.
- `docker-compose.yml`: DB (pinned tag, healthcheck, named volume) + `rabbitmq:<pinned>-management` (healthcheck via `rabbitmq-diagnostics -q ping`) + API (`env_file: .env` + container-network overrides, `depends_on: ... condition: service_healthy`) + Worker (same `.env` + overrides, `depends_on` RabbitMQ healthy) + optional dev-only SMTP catcher (`mailhog`) driven purely by `.env`. Single bridge network, one named DB volume.

### 2.9. Configuration / `.env` loading (adopt)

`DotNetEnv.Env.TraversePath().Load();` as the literal first line of `Program.cs` in **both** API and Worker, before `CreateBuilder(args)` — `TraversePath()` walks up directories so it works regardless of launch CWD (plain `dotnet run`, IDE, `dotnet ef` design-time tooling); no-ops harmlessly in Docker where env vars arrive from compose directly. `EnvOptionsBase` (`Require(key)` throws on missing required var; `GetOrDefault` for optional) is the **sole** gateway to `Environment.GetEnvironmentVariable` — every concrete `*Options` class reads its vars once in its own constructor, registered `AddSingleton`.

### 2.10. Recommender pattern (adopt the *shape*, not the algorithm — see §5)

Own feature folder (`IRecommenderService`/`RecommenderService`, Scoped), thin controller endpoint, user id always from JWT. Output DTO carries an explicit `Reason` string field. A narrative spec doc (`recommender-dokumentacija.md`) at repo root is the **binding spec** — code must match it, not vice versa (`upute.md` §2.4). Recommended skeleton for eAutoškola's own `recommender-dokumentacija.md`:

1. Svrha i cilj
2. Pristup i motivacija (primary approach + justification; cold-start fallback)
3. Izvori podataka i signali (table: signal | source table/field | when written | role in recommendation)
4. Izgradnja obilježja (feature engineering steps)
5. Algoritam sličnosti i bodovanje (exact math: cosine similarity formula + weighting)
6. Objašnjive preporuke (explanation-string generation, example sentences)
7. Upravljanje modelom (lifecycle, DI scope)
8. Integracija u aplikaciju (endpoint, auth, consuming screens)
9. Rubni slučajevi (new user, no candidates, sparse catalog, all already booked)
10. Ograničenja i moguća poboljšanja
11. Sažetak usklađenosti sa zahtjevima (checklist)

---

## 3. Architecture decision for eAutoškola — **APPROVED** (2026-10-13)

### Decision

Resolved directly by you against the three open points, each checked against §2's findings:

1. **Clean Architecture project names are kept, CQRS is required, structure stays as close to ClinicNow as possible.**
2. **MediatR: ClinicNow uses it nowhere (§2.1 confirms zero matches for `MediatR`/`IRequestHandler`/`ICommand`/`IQuery`, no `Commands`/`Queries` folders) → not used here either.** CQRS is still implemented, but as direct interface/class separation (query interface + command interface per entity), never as a mediator/handler dispatch — this is "whatever alternative approach [the reference] uses" applied to the *command/query* axis specifically, since ClinicNow's own service interfaces already separate reads (`IService`) from writes (`ICRUDService` adds Insert/Update/Delete on top) at the interface level. eAutoškola takes that one step further by also splitting complex entities' *commands* out from their *queries*, using the same direct-call style.
3. **`IApplicationDbContext`: ClinicNow's `BaseService`/`BaseCRUDService` hold `protected readonly ClinicNowContext Context` — the concrete class, no interface → not introduced here either.** `Application`-layer services take the concrete `EAutoskolaDbContext` (from `Infrastructure`) directly as a constructor dependency. This means `Application` has a project reference to `Infrastructure`, which inverts the textbook Clean-Architecture dependency arrow — a deliberate, acknowledged trade: the four project **names** (`Domain`/`Application`/`Infrastructure`/`API`) stay, for navigability and to honor "Clean Architecture is acceptable," but the **dependency graph and code mechanics are ClinicNow's**, not a textbook Clean Architecture's. This is exactly what "align the structure as closely as possible with the reference project" requires once MediatR and the interface abstraction are both off the table — ClinicNow's `Services` project *is* simultaneously "Application" and "Infrastructure" (business logic + DbContext in one project); splitting it into two projects that still see each other's concrete types preserves that same merged reality under more descriptive folder/project names.

### Final project layout

```
eAutoskola.Domain          — Entities (bare noun, e.g. Lesson, TestAttempt, Vehicle), enums/status constants,
                              Role static class, state-machine transition rules (pure functions, e.g.
                              LessonStatusRules.CanTransition(from, to)), base exception hierarchy
                              (EAutoskolaException and subclasses, same shape as ClinicNowException's).
                              Zero package references (mirrors ClinicNow.Model's "pure POCO" project).

eAutoskola.Application      — DTOs (`Dto` suffix), Requests (`Request` suffix, Insert/Update pairs),
                              SearchObjects (`SearchObject` suffix extending BaseSearchObject, PageSize
                              capped at 100 in the setter), PagedResult<T>, Mapster mapping configs, shared
                              validation-rule helpers (ContactRules-equivalent). Service interfaces AND
                              implementations:
                                - simple reference/codebook entities (City, Country, VehicleCategory,
                                  RequestStatus, ServiceType, DrivingZone, QuestionArea): ONE
                                  I<Entity>Service : ICRUDService<...> per entity — identical shape to
                                  ClinicNow's CityService, no CQRS split (it would be pure ceremony here,
                                  and ClinicNow doesn't do it either).
                                - entities with real command variety (Lesson, TestAttempt, Charge/
                                  Installment/Payment/Refund, User/profile actions): split into
                                  I<Entity>QueryService (GetPagedAsync/GetByIdAsync) and
                                  I<Entity>CommandService (domain-specific commands, e.g. RequestLessonAsync/
                                  ConfirmLessonAsync/RejectLessonAsync/CompleteLessonAsync/CancelLessonAsync)
                                  — this IS the CQRS requirement, satisfied with two plain interfaces/classes,
                                  no dispatcher.
                              Base classes: BaseQueryService<TModel,TSearch,TDbEntity> and
                              BaseCommandService<TModel,TInsert,TUpdate,TDbEntity> (renamed equivalents of
                              ClinicNow's BaseService/BaseCRUDService); BaseCRUDService<...> combines both
                              for the simple codebook case. All hold EAutoskolaDbContext (concrete, from
                              Infrastructure) + IMapper directly — no IApplicationDbContext interface.
                              References: Domain, Infrastructure.

eAutoskola.Infrastructure   — EAutoskolaDbContext : DbContext (concrete; Application depends on this
                              project to get it); EF entity configurations; migrations; seeding; QuestPDF
                              report generation; PayPal client wrapper; RabbitMQ publisher (implements an
                              IEmailPublisher/INotificationPublisher interface declared in Application —
                              same pattern ClinicNow already uses, not a new abstraction); SMTP sender;
                              *Options config classes (bound once from .env, see §2.9).
                              References: Domain.

eAutoskola.API               — Thin controllers (BaseController/BaseCRUDController unchanged from §2.1;
                              complex entities get a dedicated controller injecting both the Query and
                              Command service interfaces, one action per command, zero business logic),
                              ExceptionFilter, SignalR hub (notifications), Program.cs composition root,
                              Swagger w/ JWT bearer (Development only), rate-limiting policies.
                              References: Application, Infrastructure (composition root only — DI
                              registration, not business logic).

eAutoskola.Worker            — Separate deployable, own Dockerfile. RabbitMQ consumer(s)
                              (AsyncEventingBasicConsumer, retry-with-backoff, manual ack), mail/push
                              senders. Does NOT reference Infrastructure/DbContext — exactly like
                              ClinicNow.Worker: it only performs the external side effect (send the
                              email); any DB write that must happen when a lesson is confirmed/rejected/
                              paid (e.g. creating the in-app Notification row) is done by the API's own
                              Command service, in the same transaction as the triggering action, BEFORE
                              publishing the message — the Worker only has to deliver the email/push copy.
                              References: Contracts.

eAutoskola.Contracts         — Message contracts shared between Infrastructure's publisher and Worker's
                              consumer (e.g. EmailMessage). No other dependencies.

eAutoskola.Tests             — xUnit, mirrors Application's feature folders (ClinicNow.Tests pattern).
```

### Why this reconciles "Clean Architecture acceptable" with "align closely with the reference"

The four layer *names* exist and each has a clear single responsibility (entities/rules vs. application services vs. EF/external integrations vs. HTTP surface) — genuinely useful for navigation and for explaining the codebase at odbrana. But none of ClinicNow's proven, already-accepted mechanics are replaced with something new: no mediator pipeline, no repository/interface abstraction over the database, no change to `PagedResult`/`SearchObject`/exception/validation/auth/RabbitMQ/Docker patterns from §2. The only genuinely new thing introduced is the **Query/Command interface split**, applied selectively (only where an entity's command surface is non-trivial), which is the minimum necessary to honestly say "CQRS is implemented" without inventing ceremony ClinicNow itself doesn't have anywhere.

### Still true from before

The runtime call path for every single HTTP request remains the literal three hops `upute.md` §3.4 requires: **Controller → Application service method → DbContext → SaveChangesAsync**. State-machine transition rules live in `Domain` as pure functions, called by the `Application` command service — centralized, not scattered across controllers (§7's explicit requirement), and not duplicated per entity.

---

## 4. Flutter patterns & decisions for eAutoškola

### Adopt as-is from ClinicNow

- **State management:** `provider` (`ChangeNotifier`), a shared `AuthSession extends ChangeNotifier` holding token/roles/identity, `MultiProvider` at the app root, root widget swapping `LoginScreen`/`AppShell` via `Consumer<AuthSession>`.
- **`BaseProvider<T>`** generic REST abstraction: static `baseUrl = String.fromEnvironment('API_BASE_URL', ...)`, shared `http.Client`, `requestTimeout`, `getPaged`/`getById`/`insert`/`update`/`delete`, decodes `{errors: {...}}` into a typed `ApiException`, and **clears `AuthSession` on HTTP 401** so the UI auto-redirects to login — directly satisfies `upute.md` Appendix A.2 ("Frontend mora pravilno obraditi HTTP 401 odgovor"). A thinner `api_http.dart` companion (`apiGet`/`apiPost`/...) handles bespoke non-CRUD actions (confirm/cancel/PDF download/PayPal flow).
- **Theming:** numeric design tokens (`AppSpacing`/`AppRadius`/`AppDuration`, 4px grid) + a `ThemeExtension<AppColors>` with named semantic colors, `ThemeData` built from `ColorScheme.fromSeed` then overridden with hand-tuned values (avoids the generic "default Material app" look), `themeMode: ThemeMode.system` for automatic dark mode.
- **Form validation:** `flutter_form_builder` + `form_builder_validators`, with a shared Dart `ContactRules`-equivalent validator class mirroring the backend's shared validation rules, so client and server messages/constraints stay in sync.
- **PDF reports:** server-side generation (QuestPDF, see §2.10), Flutter only **previews/shares/prints already-generated bytes** via the `printing` package — no client-side PDF construction. For eAutoškola this only applies to the **desktop/Admin app** (reports are an Admin-only feature per `docs/prijava.md` §3.1/§4.1.4); the mobile app needs no `printing` dependency.
- **Reusable widget library:** `widgets/ui/` prefixed `app_` — `AppCard`, `AppBadge`/`AppStatusBadge`, `AppDialog`, `AppDataTable` (desktop) / `AppListCard` (mobile card-row equivalent), `AppStates` (empty/error/skeleton/notice), `AppFields` (search box, toolbar, segmented control), `StatCard` (KPI tile for the dashboard). Replicate this exact widget set for eAutoškola's own dashboard/list/form screens.

### Deliberate divergence from ClinicNow

- **One Flutter app for both mobile roles, not two separate packages.** ClinicNow splits desktop and mobile into two fully independent Flutter packages because its mobile app is single-role (Patient only). `docs/prijava.md` describes **one mobile app with two distinct interfaces** (Učenik and Instruktor) — so eAutoškola needs **two Flutter projects total**: `driving_school_desktop` (Admin, Windows) and `driving_school_mobile` (Student + Instructor, Android, role-gated navigation shell chosen after login based on the JWT's role claim — same `AppShell`-swap-by-role pattern ClinicNow uses for desktop-vs-mobile-role gating, just applied within one app instead of across two apps).
- **Token persistence across app restarts.** ClinicNow's `AuthSession` is in-memory only — closing the app logs the user out. This isn't a rule violation, just a simplification not worth repeating: recommend adding `flutter_secure_storage` to persist the JWT (and a lightweight silent-refresh-or-redirect-to-login check on app start) so Učenik/Instruktor aren't forced to re-login every time they open the app. This is a UX improvement, not mandated by `upute.md` — flagging it here for awareness, not as something that blocks approval of §3.

---

## 5. What NOT to blindly copy

The K-scan (rule-compliance check against every explicit `upute.md` requirement: auth coverage, `Console.WriteLine` usage, blocking async calls, unbounded list endpoints, hardcoded config, `System.Random` for security values, password hashing scheme) found **no violations** — ClinicNow reads as a deliberately rule-compliant reference implementation; nearly every file carries a doc-comment tying a decision back to a named rulebook section. So this is not a "these parts are against the rules" list — it's a **"this is ClinicNow's domain content or a judgment call, not a transferable pattern"** list:

- **Role names** (`Administrator`/`Staff`/`Doctor`/`Patient`) are ClinicNow's own domain — eAutoškola uses `Admin`/`Instructor`/`Student` per `CLAUDE.md`'s glossary. Don't copy the literal strings, only the "static class of role constants" pattern.
- **Recommender algorithm.** ClinicNow trains an ML.NET pipeline. `docs/prijava.md` §6.2 already commits eAutoškola to a specific, simpler, **hand-computed cosine similarity over explicit feature vectors** (error-rate/error-frequency per `QuestionArea` vs. `StudyLesson` tags) — no trained model, no ML.NET dependency needed. Copy the *folder shape and explainability-DTO pattern* (§2.10), not the ML.NET implementation. `upute.md` §2.4 requires the implementation to match the submitted documentation exactly, and our own documentation already specifies the simpler approach.
- **RabbitMQ consumer always acks after one retry sequence, no dead-letter queue.** Compliant with `upute.md` Appendix A.1 as written (retried with backoff, failure always logged, never an empty catch) — but a message that exhausts retries is simply dropped with a log line, not preserved anywhere. Not required by `upute.md`, but worth considering a DLQ as a nice-to-have if time allows; not a blocker.
- **In-memory-only token storage** in Flutter — see §4 above; a conscious simplification to not repeat, not a rule violation.
- **PayPal SDK specifics** (`PayPalServerSDK` package, ClinicNow's own catalog/appointment pricing logic) — the *integration pattern* (server owns the catalog price, webhook/server-side verification finalizes payment, in-app WebView with `url_launcher` fallback for the approval flow) transfers directly per `upute.md` §7.1; the actual charge/installment domain model is eAutoškola's own (`Charge`/`Installment`/`Payment`/`Refund` per `CLAUDE.md`'s glossary), not ClinicNow's.
- **Two separate Flutter packages for desktop/mobile** — see §4 divergence above; eAutoškola's mobile side needs one app with two role-gated interfaces, not ClinicNow's one-role-per-package split.

---

## 6. Library versions

### Confirmed from ClinicNow's `.csproj`/`pubspec.yaml` files (all current as of this project's own build, same timeframe as eAutoškola)

**Backend**, all projects on `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`:

| Package | Version | Project |
|---|---|---|
| DotNetEnv | 3.1.1 | API, Services, Worker |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 | API |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.0 | Services |
| Microsoft.EntityFrameworkCore.Design / .Tools | 10.0.0 | API/Services (pinned equal — a Tools 11.0 preview broke `dotnet ef`) |
| Swashbuckle.AspNetCore | 7.2.0 | API |
| Mapster / Mapster.DependencyInjection | 7.4.0 / 1.0.1 | Services |
| BCrypt.Net-Next | 4.2.0 | Services |
| QuestPDF | 2026.7.2 (Community license) | Services |
| RabbitMQ.Client | 7.2.2 | Services, Worker |
| System.IdentityModel.Tokens.Jwt | 8.22.0 | Services |
| System.Linq.Dynamic.Core | 1.6.0.2 | Services (dynamic `OrderBy(string)` for sorting, allowlist-validated) |
| Microsoft.ML | 5.0.0 | Services (recommender — **not needed** for eAutoškola, see §5) |
| PayPalServerSDK | 2.4.0 | Services |
| MailKit | 4.17.0 | Worker |
| xunit / Microsoft.NET.Test.Sdk | 2.9.3 / 17.14.1 | Tests |

**Flutter**, both apps on `sdk: ^3.12.2` (Dart):

| Package | Version | Where |
|---|---|---|
| provider | ^6.1.5 | both |
| http | ^1.6.0 | both |
| flutter_form_builder | 10.3.0+2 (pinned) | both |
| form_builder_validators | ^11.3.0 | both |
| intl | 0.20.2 (exact) | both |
| google_fonts | ^6.2.1 | both |
| fl_chart | ^0.69.2 | desktop only |
| printing | ^5.14.2 | desktop only |
| webview_flutter | ^4.14.1 | mobile only (PayPal approval UI) |
| url_launcher | ^6.3.1 | mobile only (fallback) |
| firebase_core / firebase_messaging | ^4.14.0 / ^16.6.0 | mobile only (push) |
| flutter_local_notifications | ^22.3.0 | mobile only |

### Proposed for eAutoškola

Adopt the same baseline — it's already current for this exact timeframe (confirmed `.NET 10` is the active LTS as of October 2026, support until November 2028; Flutter 3.47.x is current stable, 3.50 due November 2026). Concretely:

- **.NET 10** (LTS), same package set minus `Microsoft.ML`/`PayPalServerSDK`-specifics (swap the PayPal package for whatever the current PayPal .NET SDK is at implementation time — check `dotnet add package PayPalServerSDK` for its latest version rather than hand-pinning 2.4.0 today), plus `flutter_secure_storage`-equivalent is a Flutter-side change only.
- **Flutter**: run `flutter create` fresh at skeleton time rather than hand-pinning a Dart SDK constraint — this pulls whatever is the actual installed current-stable toolchain, which will already be ≥3.47.x. Reuse the same package list, add `flutter_secure_storage` for token persistence (see §4).
- At solution-skeleton time, run `dotnet list package --outdated` and `flutter pub outdated` once the first package references are added, to catch anything that shipped a newer patch between this analysis and implementation — don't hand-copy version numbers from this document without that check.

Sources: [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy) · [Flutter SDK archive](https://docs.flutter.dev/install/archive)
