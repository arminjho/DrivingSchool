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
