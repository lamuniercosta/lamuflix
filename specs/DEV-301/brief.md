# DEV-301 — Phase A Brief

Phase A grill outcome for DEV-301 (parent DEV-283, size M, no ui tag).
Patron decided Q1–Q12 in `specs/DEV-301/CONCLUSIONS.md` (commit 3632f11), citing sources for each, and logged one taste call in `ASSUMPTIONS.md` (the Q9 repetition count). Facts come from `recon-DEV-301`: lines 1–19 hold the ticket and epic text, and lines 21–79 are the worktree-verified answers. Keel read the cited files: `IMovieRepository.cs:7-22`, `Movie.cs:1-121`, `EnrichmentOptions.cs`, `MovieConfiguration.cs:11-78`, `MovieIdValueGenerator.cs`, and constitution `:170-188` and `:440-459`.
Grill questions: **12 asked (cap 12); 12/12 ruled**. Owner checkboxes: **0**. ADR: **none** (Q12).

## Closing bar

The ticket's ACs, verbatim (`recon-DEV-301:14-16`):

- **AC1 (ticket):** "Concurrency integration test passes reliably against real Postgres container."
- **AC2 (ticket):** "Claim is guaranteed atomic without table locking."

Keel's closing-bar lines, which come from Patron's rulings and do not change delivery:

- **AC3 (Q1):** `public sealed class EfMovieRepository : IMovieRepository` exists at `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` and implements all five members: `GetAsync`, `AddAsync`, `NextIdentityAsync`, `SaveChangesAsync`, and `TryClaimForEnrichmentAsync`.
- **AC4 (Q5):** `TryClaimForEnrichmentAsync` is one LINQ `ExecuteUpdateAsync` over `Movies`. Its predicate is `Id == id && Status == Pending && (LastAttemptAt == null || LastAttemptAt < leaseExpiry)`. It sets `LastAttemptAt = now` and `EnrichmentAttempts = EnrichmentAttempts + 1` in the database, and returns `rows == 1`. `now` is sampled once from the injected `TimeProvider`. `leaseExpiry = now - EnrichmentOptions.ClaimLease`. It forwards the CT and uses no raw SQL.
- **AC5 (Q6):** the constructor throws `ArgumentOutOfRangeException` when `ClaimLease <= TimeSpan.Zero`.
- **AC6 (Q9, AC1):** in `tests/LamuFlix.IntegrationTests`, over the existing `PostgresFixture`, the concurrency test runs 50 fresh Pending movies. For each one, two repositories on two independent contexts/connections are released by one async start gate, at a fixed test time with a positive lease. Each iteration yields exactly one `true` and one `false`, and the persisted `enrichment_attempts` goes up by exactly 1.
- **AC7 (Q9, AC2):** the captured claim commands show exactly one predicate `UPDATE` per invocation, with no `LOCK TABLE` and no `SELECT … FOR UPDATE`. The spec words this as "no explicit table-locking strategy", not as absence of PostgreSQL's normal row locking.
- **AC8 (Q10):** Postgres integration tests cover:
  - an absent id returns `false`;
  - each terminal status (Enriched, NotFound, Failed) returns `false`;
  - an unexpired lease returns `false`;
  - an expired lease returns `true`, with the increment and `last_attempt_at == now` persisted;
  - the strict boundary: `last_attempt_at == leaseExpiry` returns `false`;
  - a round trip through all five members;
  - a saved `Mark*` transition;
  - the load → claim → save seam (Q3): a save never undoes the claim's fields.
- **AC9 (Q2):** `Movie.Rehydrate(...)` exists in `src/LamuFlix.Core/Domain/Movie.cs`. It accepts all persisted aggregate state, calls no `Mark*`, and replays no transitions. Value-object and aggregate validity checks still apply. It has unit tests in `tests/LamuFlix.UnitTests/Domain/`, because the Core mutation gate applies (Q12).
- **AC10 (Q4, Q11, Q12):** there are zero new packages and zero edits to `.csproj`, `Directory.*.props`, migrations, the model snapshot, `MovieIdValueGenerator`, `EnrichmentOptions`, `BannedSymbols.txt`, or any DI extension. There is no schema change. No files outside the frozen scope change, which `git diff --stat origin/main...HEAD` proves.
- **AC11:** the gates exit 0 (see *Gate expectations*). The full suite passes, including the existing tests and the new ones.

## Frozen scope

Files in scope:

- `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` (new; ticket Scope line 1, Q1).
- `src/LamuFlix.Core/Domain/Movie.cs`: add `Rehydrate` only. This is the **sole authorised domain edit** (§2.3 item 6, ruled Q2 and Q7). `Mark*`, `BeginPendingAttempt`, and the counting semantics stay as they are.
- `src/LamuFlix.Infrastructure/Persistence/` private mapping helpers, only if a complexity gate forces extraction. They go in the same folder and create no new folder or abstraction. The `Records/MovieRecord.cs` shape is unchanged.
- `tests/LamuFlix.IntegrationTests/`: new repository test file(s), for example `Persistence/EfMovieRepositoryTests.cs` and `EfMovieRepositoryClaimConcurrencyTests.cs`, following that project's existing folder convention. A small SQL-capture helper (an EF `DbCommandInterceptor`) and a test `TimeProvider` subclass go there only if they don't already exist.
- `tests/LamuFlix.UnitTests/Domain/MovieRehydrateTests.cs` (new; or the existing Movie test file, if one exists there).
- The `specs/DEV-301/*` artifacts.

Out of scope:

- DI registration of the repository (Q11). It is deferred to wiring work. Its lifetime must be scoped when wired. No new `AddInfrastructure` or `AddPersistence` extension, and no edit to `Pipeline/ServiceCollectionExtensions.cs`.
- `EnrichmentOptions` validation and default (Q6). This goes to a follow-up.
- The claim-count vs outcome-count double increment (Q7). This goes to a follow-up.
- The retry-within-lease refusal (Q8). Rigger records it as a comment on DEV-299's claim-handoff follow-up, not as a DEV-301 change.
- The legacy `LamuFlix.Data`, `GenericRepository<T>`, `MovieService`, `EnrichmentJobProcessor`, and `tests/LamuFlix.Test/EnrichmentTests.cs`.
- `Features:LocalPlay` and `Process.Start` are untouched.

**Rigger follow-ups (pending, not DEV-301 deliverables).** Rigger checks for existing tickets first, then files or comments. Status is pending until Rigger returns a verified receipt:

- (F1, Q6) validate that `ClaimLease` is positive, with `ValidateOnStart`;
- (F2, Q7) reconcile claim-count vs outcome-count semantics;
- (F3, Q8) a comment on DEV-299's claim-handoff follow-up, or on DEV-299 itself.

## Round cap

- Grill: closed at 1 round (12/12).
- Spec/plan review (Keel ↔ Quill): at most 3 fix-list rounds, which is the three-blocked-report cap (task-pipeline §2.2).
- Code review: at most 2 rounds, with at most 2 fix commits per round.

## Grill answers

- **Q1 — Scope:** all five members in one sealed `EfMovieRepository`, flat in `Infrastructure/Persistence/`. The legacy data layer stays.
- **Q2 — Rehydration (§2.3 item 6, ruled):** option (a), `Movie.Rehydrate` in Core. No reflection, and no remapping of EF onto the domain type.
- **Q3 — Change tracking:** one identity map from `MovieId` to `(Movie, MovieRecord)` per repository/DbContext scope. `GetAsync` returns the same tracked aggregate. `AddAsync` tracks the supplied aggregate. `SaveChangesAsync` propagates aggregate changes, including metadata relationships, then saves. Claim-owned fields are preserved when the aggregate has not changed them. A successful claim on an already-loaded aggregate synchronises the claim state and tracking baseline without discarding unrelated pending changes. Blind copying of stale state is rejected, because `ExecuteUpdate` bypasses the change tracker.
- **Q4 — Identity:** `SELECT nextval(pg_get_serial_sequence('movies','id'))` through the existing context/connection, with the CT. Validate the result into `MovieId`. `AddAsync` inserts the explicit id. There is no schema change, and gaps are acceptable.
- **Q5 — Clock/lease:** see AC4.
- **Q6 — Zero lease:** a constructor guard (AC5), plus follow-up F1.
- **Q7 — Double count:** the ticket SQL is kept verbatim, with no domain counting change, plus follow-up F2. No owner checkbox.
- **Q8 — Retry vs lease:** a tracker comment only (F3). Premise corrected: the Retry branch does not refresh `LastAttemptAt` (`RecordEnrichmentFailureCommandHandler.cs:23-33`); the preceding successful claim does.
- **Q9 — Concurrency proof:** see AC6 and AC7. N = 50 is `[assumed]`.
- **Q10 — Other cases:** see AC8. They use the small test `TimeProvider` subclass convention and a fixed UTC instant PostgreSQL can represent (microsecond precision). No new package.
- **Q11 — DI:** deferred. Tests construct the repository directly in the fixture setup.
- **Q12 — ADR/gates/mutation:** no ADR and no new dependency. Mutation applies to the changed production assemblies: Core (because of `Rehydrate`) and Infrastructure, under the current script. Nothing is predeclared N/A. An unrunnable gate is reported **Could not run**, never PASS or N/A. `IntegrationTests` already references Infrastructure (`LamuFlix.IntegrationTests.csproj:20-21`).

## Plan decisions

**Approach**

1. *Constructor:* `EfMovieRepository(LamuFlixDbContext db, TimeProvider timeProvider, IOptions<EnrichmentOptions> options)`. The guard (AC5) reads `options.Value.ClaimLease` once and stores it.
2. *Mapping:* two private static mappers. `ToDomain(MovieRecord)` calls `Movie.Rehydrate`. `Apply(Movie, MovieRecord, Baseline)` writes changes back. Metadata and the skip navigations (actors/directors/genres) map through the existing records. `GetAsync` loads the record with its navigations, tracked.
3. *Baseline and diff (Q3):* on `GetAsync`/`AddAsync` the repository snapshots the aggregate's persisted state as a `Baseline`, a private record. `SaveChangesAsync` writes only the fields that differ from the baseline. `EnrichmentAttempts` is written as a **delta**: `record.EnrichmentAttempts += aggregate − baseline`. That way a claim's database-side increment survives a later `Mark*` save. After the save, the baseline is refreshed.
4. *Claim sync (Q3):* when `TryClaimForEnrichmentAsync` returns `true` and the id is in the identity map, the repository re-reads only `LastAttemptAt` and `EnrichmentAttempts` (a projection query with no tracking). It then sets them as both the current and original values on the tracked record, and advances the baseline's claim fields to match. Other pending aggregate changes are left untouched.
5. *Claim:* as in AC4. The status comparison uses `EnrichmentStatus.Pending` through the existing `EnrichmentStatusConverter`. The spec must record the rendered predicate `status = 0`.
6. *Identity:* as in Q4. Use `Database.SqlQuery<long>` (or the scalar equivalent) with a constant SQL string and no interpolation of external input. Then call `MovieId.TryCreate`, and throw `InvalidOperationException` if the value is out of range.

**Test strategy**

- *Integration (`LamuFlix.IntegrationTests`, `PostgresFixture`):* AC6–AC8. Each test seeds its own movies, so tests are independent without relying on the order they run in. The concurrency test uses a `TaskCompletionSource` start gate and `Task.WhenAll` across two contexts built from the fixture's connection string. SQL capture uses a `DbCommandInterceptor` on the claiming contexts only.
- *Unit (`LamuFlix.UnitTests/Domain`):* `Movie.Rehydrate` round-trips every field. Invalid persisted state is rejected by the value-object and aggregate checks. A rehydrated movie accepts legal transitions and rejects illegal ones. xUnit v3, Shouldly, and the existing conventions apply.
- There are no mocks of database behaviour anywhere (Q12).

**Gate expectations**

- After each `.cs` edit: Roslyn analyzers, cyclomatic complexity ≤ 15, InspectCode, and `dotnet format --verify-no-changes`. The refactor stage uses complexity ≤ 6, which means extracting mapper and claim helpers.
- The full `dotnet test` requires Docker.
- Mutation covers Core and Infrastructure per `scripts/run-mutation.ps1`. **Known risk:** that script excludes `IntegrationTests` from the Stryker-eligible test projects, so the Infrastructure mutants may have no eligible killer. If the gate fails closed for that reason, it is reported **Could not run** with the script output and brought back to Keel for a ruling. Thresholds are never lowered, and database behaviour is never mocked to satisfy it.

**Task ordering**

1. `Movie.Rehydrate` and its unit tests (Core).
2. The repository skeleton: constructor, lease guard, and AC5 test.
3. `NextIdentityAsync` and `AddAsync`, then `GetAsync`, then `SaveChangesAsync` (baseline/diff), with the round-trip and `Mark*` integration tests.
4. `TryClaimForEnrichmentAsync` with the single-case tests (AC8), then the claim sync with the load → claim → save seam test.
5. The concurrency test and SQL capture (AC6, AC7).
6. Gates, the refactor pass (complexity ≤ 6), and mutation.

Tasks 1–2 can run in parallel. Tasks 3–5 run in order.
