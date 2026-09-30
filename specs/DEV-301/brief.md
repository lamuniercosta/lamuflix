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
2. *Mapping:* two private static mappers. `ToDomain(MovieRecord)` calls `Movie.Rehydrate`. `Apply(Movie, MovieRecord, Baseline)` writes changes back. **Superseded by D9:** metadata maps through six flat columns. Record-only columns and the skip navigations are never read into the aggregate or written from it. `GetAsync` loads the record tracked, without `Include`.
3. *Baseline and diff (Q3):* on `GetAsync`/`AddAsync` the repository snapshots the aggregate's persisted state as a `Baseline`, a private record. `SaveChangesAsync` writes only the fields that differ from the baseline. `EnrichmentAttempts` is written as a **delta**: `record.EnrichmentAttempts += aggregate − baseline`. That way a claim's database-side increment survives a later `Mark*` save. After the save, the baseline is refreshed.
4. *Claim sync (Q3):* when `TryClaimForEnrichmentAsync` returns `true` and the id is in the identity map, the repository re-reads only `LastAttemptAt` and `EnrichmentAttempts` (a projection query with no tracking). It then sets them as both the current and original values on the tracked record. **Superseded by D2 and D8:** the private `Baseline` claim fields are not advanced, and `LastAttemptAt` is stamped from the in-process `now`, not re-read. Other pending aggregate changes are left untouched.
5. *Claim:* as in AC4. The status comparison uses `EnrichmentStatus.Pending` through the existing `EnrichmentStatusConverter`. The spec must record the rendered predicate `status = 0`.
6. *Identity:* as in Q4. **Pinned by D13.** Use a constant SQL string with no interpolation of external input. Then call `MovieId.TryCreate`, and throw `InvalidOperationException` if the value is out of range.

**Test strategy**

- *Integration (`LamuFlix.IntegrationTests`, `PostgresFixture`):* AC6–AC8. Each test seeds its own movies, so tests are independent without relying on the order they run in. The concurrency test uses a `TaskCompletionSource` start gate and `Task.WhenAll` across two contexts. **The context construction is superseded by D10 and D14** (the sibling pattern on one migrated database, with interceptor-aware options). SQL capture uses a `DbCommandInterceptor` on the claiming contexts only.
- *Unit (`LamuFlix.UnitTests/Domain`):* `Movie.Rehydrate` round-trips every field. Invalid persisted state is rejected by the value-object and aggregate checks. A rehydrated movie accepts legal transitions and rejects illegal ones. xUnit v3, Shouldly, and the existing conventions apply.
- There are no mocks of database behaviour anywhere (Q12).

**Gate expectations**

- After each `.cs` edit: Roslyn analyzers, cyclomatic complexity ≤ 15, InspectCode, and `dotnet format --verify-no-changes`. The refactor stage uses complexity ≤ 6. Private helpers are extracted only if that gate forces it; extraction is a gate outcome, not a deliverable (Ledger F6).
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

## Plan-review decisions (Keel, spec round 1)

These come from `/speckit-analyze` on Quill's draft. They settle Quill's `needs decision:` question and correct one of Keel's own plan decisions. None of them changes ticket delivery or a Patron ruling.

- **D1 — `Rehydrate` signature (answers Quill).** `public static Movie Rehydrate(MovieId id, string title, LibraryPath path, MediaFormat format, bool isInWatchlist, MovieMetadata? metadata, EnrichmentStatus status, DateTimeOffset? enrichedAt, int enrichmentAttempts, EnrichmentFailureCategory? lastFailureCategory, DateTimeOffset? lastAttemptAt)`. These are the 11 properties in `Movie.cs:19-39`, in declaration order. Validity checks: the same blank-title `ArgumentException` that `Create` applies (`Movie.cs:43-46`), plus the value objects' own construction checks, which run before the call. **No new invariants** (for example, "Enriched requires Metadata"). Adding one would be a new domain rule, which Q2 does not authorise. If a gate objects to the parameter count, report it; do not introduce a parameter object.
- **D2 — Claim sync corrects plan decision 4.** `Movie` has no setter for its claim fields, and Q2 authorises only `Rehydrate`, so a claim cannot update the in-memory aggregate's `LastAttemptAt`/`EnrichmentAttempts`. The sync therefore updates the **EF tracking baseline only**: it sets the re-read values as the tracked record's current and original values. The repository's private `Baseline` claim fields are **not** advanced, because they must keep matching the aggregate's in-memory values for the delta in decision 3 to work. Arithmetic, starting from a load with attempts = a: the claim makes the DB a+1, and the record is synced to a+1. `Mark*` makes the aggregate a+1 while the baseline is still a. Save then writes a+1 + (a+1 − a) = a+2, which is correct. If the baseline had advanced, the delta would be 0 and the `Mark*` count would be lost. `LastAttemptAt` follows the same rule: if the aggregate has not changed it, it equals the baseline, so it is not written and the claim's `now` survives. After `Mark*(now2)` it differs, so `now2` is written. This is Q3's "tracking baseline" and satisfies Q3's intent that a save never undoes the claim.
- **D3 — Seam expectations follow D2.** Load → claim → a change that does not touch the claim fields (for example `AddToWatchlist`) → save → reload: `last_attempt_at` is the claim's `now` and attempts = a+1. Load → claim → `Mark*(now2)` → save → reload: attempts = a+2, and `last_attempt_at` = `now2`. Both are asserted.
- **D4 — Test file placement.** `tests/LamuFlix.IntegrationTests` is flat (`MigrationTests.cs`, `PersistenceRoundTripTests.cs` at the root). The new files go at the root, `EfMovieRepositoryTests.cs` and `EfMovieRepositoryClaimConcurrencyTests.cs`, with no `Persistence/` subfolder. The helpers also go at the root.
- **D5 — The `NextIdentityAsync` out-of-range guard.** The guard is implemented. It is **not** exercised by manipulating the sequence (`setval`/`ALTER SEQUENCE`). *(Rationale corrected per Compass F6: each `CreateContext` call gets its own database, so the sequence is not shared. The reason for not testing it is that no production path can drive the sequence out of `MovieId` range; the guard is defensive, and forcing it would test sequence manipulation, not the repository.)* If mutation reports survivors on the guard, list them as survivors and cite D5. They are not a reason to fail closed, and not a reason to mutate shared state.
- **D6 — "Reliably" (AC1).** The concurrency test class passes on 3 consecutive filtered runs, and it passes again inside the full-suite run. The implementer records all 4 results in the receipt.
- **D7 — Order of the test `TimeProvider` subclass.** The fixed-time `TimeProvider` subclass is needed from Phase 3's single-case claim tests onward, so it is created before them, not in Phase 4. The SQL-capture interceptor stays in Phase 4.

## Plan-challenge adjudication (Keel)

Axis reports: Sentry `findings-DEV-301-risk` (R1–R7), Ledger `findings-DEV-301-standards` (F1–F8), and Compass `findings-DEV-301-spec`. Compass's note was first truncated. It was rewritten in full (79 lines, F1–F10) and is adjudicated below. All three axes are now adjudicated.

### Sentry (Risk)

- **R1 — ACCEPT (partly), D8.** `LastAttemptAt` is known exactly in process, because `now` is sampled once. So the claim sync stamps it from `now` as both the current and original value, and does not re-read it. `EnrichmentAttempts` cannot be known in process (the aggregate may be stale), so it is still re-read with a no-tracking projection. Between the `UPDATE` and that `SELECT`, the only writer that can change it is another context's save, which is the general no-concurrency-token case covered by R2. That residual race is accepted and stated in plan.md.
- **R2 — ACCEPT as a known limitation; no token in DEV-301.** Adding a concurrency token is a model or schema change, which is outside the frozen scope (AC10). plan.md states that concurrent `SaveChangesAsync` calls from different scopes are last-writer-wins, for the fields each aggregate changed. **Proposed follow-up F4, for Patron to rule on and Rigger to file:** an optimistic concurrency token on the `movies` save path. This is separate from F2.
- **R3 — ACCEPT, D10.** Concurrency test shape: the 50 iterations run **sequentially**. Each iteration creates two fresh contexts, each on its own connection from the fixture connection string, and its own `TaskCompletionSource` start gate. The two claims are released together and awaited with `Task.WhenAll`, and both contexts are disposed (`await using`) before the next iteration begins. At most two claim connections are open at once.
- **R4 — ACCEPT, D11.** Error states: `GetAsync` returns `null` for an absent id (`IMovieRepository.cs:9`, `Task<Movie?>`) and adds nothing to the identity map. `SaveChangesAsync` lets `DbUpdateException` and other EF/Npgsql exceptions propagate unchanged; there is no catch, no wrapping and no retry. `AddAsync` of an id already in the identity map throws `InvalidOperationException`. `TryClaimForEnrichmentAsync` for an id not in the map performs no sync. Cancellation surfaces as `OperationCanceledException`. `GetAsync` absent-id → `null` is added to T009.
- **R5 — NOTED; no change.** T007 is already the first test in Phase 2, so `pg_get_serial_sequence` resolution is proven there before any other Phase 2 work.
- **R6 — ACCEPT, D12.** Every database call forwards the CT: `GetAsync`'s query, the identity query, `AddAsync` (if it awaits anything), `SaveChangesAsync`, the claim `ExecuteUpdateAsync`, and the claim-sync re-read.
- **R7 — NOTED; clean.**

### Ledger (Standards)

- **F1 — ACCEPT; fixed.** Plan decision 4 above is marked superseded by D2 and D8.
- **F2 — REJECT the parameter object; D1 stands, with mitigations. Keel's plan decision; not a Patron item.** `ToDomain` calls `Rehydrate` with **named arguments**. The round-trip unit test uses distinct, non-default values for every field, including different `enrichedAt` and `lastAttemptAt` values and non-null values for both nullable enums, so any swap of same-typed arguments fails. No configured gate is known to count parameters. If one fires anyway, report it to Keel; do not add a type.
- **F3 — Keep the scoped duplicate. Keel's decision within the frozen scope.** The IntegrationTests `FixedTimeProvider` mirrors `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs` (same name, same shape). Moving it to `LamuFlix.Tests.Common` would change a file the ticket does not name (§2.3 item 6) and the project references (AC10), so it is not done here. It is a follow-up candidate only; cite this line at review if InspectCode flags the duplicate.
- **F4 — ACCEPT, D13.** Identity API: `db.Database.SqlQuery<long>($"SELECT nextval(pg_get_serial_sequence('movies', 'id')) AS \"Value\"").SingleAsync(ct)`. It is an interpolated string with **no holes**, and the `"Value"` column alias is required for a scalar `SqlQuery`. If `BannedSymbols.txt` or an analyzer rejects it, report to Keel; do not switch to a `Raw` API.
- **F5 — ACCEPT.** N = 50 is a named constant (`ClaimIterations`) in the concurrency test class.
- **F6 — ACCEPT; fixed** in *Gate expectations* above.
- **F7 — NOTED.** Infrastructure mutation is expected to come back to Keel as **Could not run**, as Q12 rules.
- **F8 — ACCEPT as a review checkpoint on T012.** `SaveChangesAsync` computes aggregate deltas only from the private `Baseline`, never from EF `OriginalValues`.

### Keel-originated correction (found while verifying Compass's F3 headline)

- **D9 — The metadata mapping corrects plan decision 2.** The domain `MovieMetadata` (`MovieMetadata.cs`) has six fields and no actors, directors or genres. `MovieRecord` stores metadata as flat columns. The mapping is `Title`↔`MetadataTitle`, `Synopsis`↔`Plot`, `Runtime`↔`RuntimeMinutes`, `ReleaseYear`↔`ReleaseYear`, `ImdbRating`↔`ImdbRating` and `ImdbId`↔`ImdbId`. `Metadata` is `null` exactly when `MetadataTitle` is `null`. Record-only data (`RottenTomatoesRating`, `MetaScore`, `PosterUrl`, and the `Actors`/`Directors`/`Genres` skip navigations) has no domain counterpart. It is never read into the aggregate and never written from it, so a save preserves it unchanged, and `GetAsync` does not `Include` the navigations. Q3's "including metadata relationships" is met by propagating the aggregate's `Metadata` changes; the aggregate carries no relationships. An integration test seeds record-only data, loads, saves after `MarkEnriched`, reloads, and asserts that the data is intact.

### Compass (Spec)

- **F1 — ACCEPT, D14; extends D10.** `LamuFlixDbContextFactory.CreateContext` creates a **new database on every call** (`LamuFlixDbContextFactory.cs:10-22`). Each concurrency iteration therefore uses one database for the whole test class: it is created and migrated once through `CreateContext(fixture)`, then movies are seeded. Both claim contexts attach to that same database through the sibling pattern (`PersistenceRoundTripTests.cs:169-174`, `source.Database.GetConnectionString()`). The container's default connection string is never used directly. The single-case and seam tests follow the same sibling pattern wherever a second context is involved.
- **F2 — ACCEPT, D15.** AC11/Q12 gates add **`./scripts/run-property-tests.ps1`** (`harness.yml` `propertyTests`) and **`./scripts/run-vulnerable-packages.ps1`** (`harness.yml` `vulnerablePackages.fail: true`). The property test is not opted out, because Rehydrate has a real invariant. The refactor pass adds an FsCheck property test in `tests/LamuFlix.UnitTests/Domain/`: for any valid field set, `Rehydrate` returns an aggregate whose 11 properties equal the inputs. No new package (FsCheck is already used by `ValueConverterPropertyTests.cs`).
- **F3 — ACCEPT; covered by D9.** The actors/directors/genres collections and the record-only columns are never surfaced by `GetAsync`. T009 asserts what is observable: the same instance on a second `GetAsync`, all 11 aggregate properties including the six `Metadata` fields, and `null` for an absent id.
- **F4 — ACCEPT; covered by D9.** Save/Apply writes only fields the aggregate models. The T011 test seeds a fully populated record (`rotten_tomatoes_rating`, `meta_score`, `poster_url`, one actor, one director, one genre), loads it, saves after `MarkEnriched` and again after `AddToWatchlist`, reloads, and asserts that all of that data survives.
- **F5 — ACCEPT; fixed.** This is the same issue as Ledger F1.
- **F6 — ACCEPT; D5 rationale corrected above.** The decision is unchanged.
- **F7 — ACCEPT.** T014 adds a sequential double claim on one fixed instant: the first returns `true`, and the second returns `false` with no second increment.
- **F8 — ACCEPT, D16.** The AC5 guard test moves to `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs`. That folder already exists (`ValueConverterTests.cs`), and UnitTests references Infrastructure (`LamuFlix.UnitTests.csproj:32`). The context is built with `UseNpgsql` options pointing at a dummy connection string that is never opened, so the test needs no Docker. The frozen scope is amended below to add this file.
- **F9 — ACCEPT; part of D14.** The concurrency test builds its two claim contexts itself, in the test file: `new DbContextOptionsBuilder<LamuFlixDbContext>().UseNpgsql(siblingConnectionString).AddInterceptors(capture).Options`. `LamuFlixDbContextFactory` (Tests.Common) is **not** edited.
- **F10 — ACCEPT.** AC9/US5 wording: `Rehydrate`'s only direct check is the blank title. Invalid value-object state is rejected earlier, when the value objects are constructed (in the mapper or the test), before `Rehydrate` is called.

### Frozen scope amendment (D16)

- Add `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs` (new).
- Add `tests/LamuFlix.UnitTests/Domain/MovieRehydratePropertyTests.cs` (new; D15). It may instead go in the same file as `MovieRehydrateTests.cs`.
- The AC5 test is removed from `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`.

## Plan freeze

The plan is **frozen** as of this section, after spec round 2/3 and the adjudication of all three plan-challenge axes. plan.md, spec.md and tasks.md carry D1–D16. The only open item is proposed follow-up F4 (concurrency token, Sentry R2), for Patron to rule on and Rigger to file; it does not gate DEV-301.
