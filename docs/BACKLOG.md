# Backlog

Known issues deliberately deferred. Each item says where it shows up and what the audit should decide.

## Phase 17 audit

### EF Core warnings: global query filters on required relationships (User, Vehicle)

**Where:** logged by EF Core at API startup and by every `dotnet ef` command, e.g.

```
[WRN] Entity 'User' has a global query filter defined and is the required end of a relationship
with the entity 'Charge'. This may lead to unexpected results when the required entity is
filtered out. Either configure the navigation as optional, or define matching query filters
for both entities in the navigation.
```

`User` (soft delete via `ISoftDelete`) is the required end of relationships with `Charge`, `InstructorProfile`, `Lesson`, `LessonStatusHistory`, `Notification`, `PasswordResetToken`, `RecommendationLog`, `StudentProfile`, `StudentStudyLesson` and `TestAttempt`. `Vehicle` (also soft-deleted) is the required end of `VehicleService`.

**Why it matters:** once a user or vehicle is soft-deleted, an `Include`/join from a dependent row to its filtered-out principal drops the row (inner join) or leaves a null navigation on a column that is declared required. For example, a lesson or charge could silently vanish from a list, or `lesson.Student!` could be null, after the student account is deleted.

**Decide in the audit, per relationship:** (a) give the dependent entity a matching query filter, (b) make the navigation optional, or (c) keep the filter off for that query with `IgnoreQueryFilters()` where historical rows must stay visible (lessons, charges, payments and refunds are financial/audit history and arguably must not disappear when an account is deleted). Then confirm the startup log is free of these warnings.

### Resolution (docs/AUDIT.md review)

The EF Core warning is a static, model-level check - it fires regardless of whether any current
query path can actually hit the failure mode, so it is not itself proof of a reachable bug.
Checked each of the 11 flagged relationships against the actual code:

- **`DashboardService.GetRecentActivityAsync`** (feeds 4.1.1's "nedavne aktivnosti"): confirmed
  reachable - it joins `Lesson`/`LessonStatusHistory`→`Student`/`Instructor` (`User`) and
  `VehicleService`→`Vehicle`, and a user/vehicle referenced by a *recent* event can later be
  soft-deleted independently of that event. Fixed with `IgnoreQueryFilters()` on those three
  sub-queries (option (c) - a recent-activity feed must not silently shrink or skip entries).
  `_context.Users` in the same method (the "new user" activity type) deliberately keeps the
  filter: a deleted user showing up as a *new* user would be wrong.
- **`ReportService.GetVehicleServiceReportAsync`** ("Izvještaj o vozilima i servisima", 4.1.4):
  already groups services strictly by the *current* fleet (`vehicleQuery` is iterated and a
  deleted vehicle's services would need a synthetic fleet entry to appear at all) - this report
  is explicitly fleet-state-scoped per its own comment ("i overview of the fleet's state"), so
  excluding a decommissioned vehicle's rows here is correct product behavior, not the bug this
  item describes. Left as-is.
- **Charge, InstructorProfile, StudentProfile, Notification, PasswordResetToken,
  RecommendationLog, StudentStudyLesson, TestAttempt** (all anchored to `User`): every current
  read of these is self-service, scoped to `ICurrentUserService.UserId` - i.e. the caller's own
  JWT. `AuthService.LoginAsync` looks up the user through the same `!IsDeleted`-filtered
  `_context.Users`, so a soft-deleted user cannot obtain a token in the first place and these
  paths are unreachable for a deleted account today. No admin-facing cross-user endpoint reads
  any of them. Left unfixed - revisit if such an endpoint is ever added (apply the same
  `IgnoreQueryFilters()` pattern at that point).

Startup log still carries the 11 warnings (resolving them at the *model* level, e.g. an explicit
matching filter on each dependent entity, would reintroduce exactly the data loss this item is
about for the one relationship that's actually reachable). They're expected now, not evidence of
a live bug - see docs/AUDIT.md for the full writeup.
