# Feature Specification: EfMovieRepository with Atomic Enrichment Claim

**Feature Branch**: `feature/301-spec`

**Created**: 2026-09-30

**Status**: gate1: provisional

**Input**: Phase A grill outcome (DEV-301, parent DEV-283, size M, no UI); `brief.md`, `CONCLUSIONS.md` (Q1-Q12), `ASSUMPTIONS.md`, recon-DEV-301, `specs/PRODUCT.md`

## User Scenarios & Testing

### User Story 1 - Exactly one worker claims a movie for enrichment (Priority: P1)

As the enrichment pipeline, when two workers try to claim the same Pending movie at the same moment, exactly one must win, so a movie is never enriched twice in parallel.

**Why this priority**: This is the ticket's core guarantee (AC1, AC2) and the reason the repository exists.

**Independent Test**: Against a real Postgres container, 50 fresh Pending movies are each claimed by two repositories on two independent connections released by one start gate; every iteration yields exactly one `true` and one `false`, and `enrichment_attempts` rises by exactly 1.

**Acceptance Scenarios**:

1. **Given** a Pending movie never attempted, **When** two repositories claim it concurrently, **Then** exactly one returns `true`, the other `false`, and `enrichment_attempts` increases by exactly 1.
2. **Given** the claim commands issued, **When** the captured SQL is inspected, **Then** each invocation is exactly one predicate `UPDATE`, with no `LOCK TABLE` and no `SELECT ... FOR UPDATE` (no explicit table-locking strategy; PostgreSQL's normal row locking is not excluded).
3. **Given** a Pending movie whose lease is unexpired, **When** a claim is attempted, **Then** it returns `false` and nothing changes.
4. **Given** a Pending movie whose lease expired, **When** a claim is attempted, **Then** it returns `true`, `last_attempt_at` equals the sampled `now`, and the attempt count increases by 1.
5. **Given** `last_attempt_at` exactly equal to the lease expiry, **When** a claim is attempted, **Then** it returns `false` (strict boundary).
6. **Given** an absent id, or a movie in Enriched, NotFound or Failed status, **When** a claim is attempted, **Then** it returns `false`.

---

### User Story 2 - Movie aggregates persist and reload faithfully (Priority: P1)

As application code, I need to add, load and save `Movie` aggregates through the `IMovieRepository` port with all persisted state restored, so handlers work against the domain type and not the EF records.

**Why this priority**: The claim is useless without the other four members of the port.

**Independent Test**: A Postgres round trip through `NextIdentityAsync`, `AddAsync`, `SaveChangesAsync`, `GetAsync` returns an aggregate equal to the original, including all six `Metadata` fields (D9). A fully populated record's record-only data (rotten tomatoes, meta score, poster URL, actors, directors, genres) survives saves of the aggregate.

**Acceptance Scenarios**:

1. **Given** an id from `NextIdentityAsync`, **When** a movie is added and saved, **Then** it inserts with that explicit id and `GetAsync` returns the same state.
2. **Given** a loaded movie, **When** a `Mark*` transition is applied and saved, **Then** the transition is persisted.
3. **Given** a loaded movie, **When** `GetAsync` is called again in the same scope, **Then** the same tracked aggregate is returned.
4. **Given** an absent id, **When** `GetAsync` is called, **Then** it returns `null` (D11).
5. **Given** a record with record-only data, **When** it is loaded, transitioned (`MarkEnriched`, then `AddToWatchlist`), saved and reloaded, **Then** that data is intact (D9).

---

### User Story 3 - A save never undoes a claim (Priority: P1)

As the pipeline, when a movie is loaded, claimed, then saved after a transition, the claim's `last_attempt_at` and attempt increment must survive.

**Why this priority**: `ExecuteUpdate` bypasses change tracking; a blind save of stale state would silently destroy the claim (Q3).

**Independent Test**: Load, claim, apply a change, save, reload: the claim increment is never lost; `last_attempt_at` is the claim `now` unless the aggregate itself changed it (`Mark*`), and the aggregate's own changes persist.

**Acceptance Scenarios**:

1. **Given** a loaded aggregate with attempts = a, **When** a claim succeeds, then a change that does not touch the claim fields (e.g. `AddToWatchlist`) is applied, and the aggregate is saved, **Then** after reload `last_attempt_at` is the claim's `now`, attempts = a+1, and the unrelated pending change is persisted (not discarded).
2. **Given** a loaded aggregate with attempts = a, **When** a claim succeeds, then `Mark*(now2)` is applied, and the aggregate is saved, **Then** after reload attempts = a+2 and `last_attempt_at` = `now2`.

---

### User Story 4 - An unusable lease is rejected up front (Priority: P2)

As an operator, a zero or negative claim lease must fail at construction, so a claim can never silently stop being exclusive.

**Independent Test**: A unit test in `LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs` constructs with `ClaimLease <= TimeSpan.Zero` over a context that has a dummy Npgsql connection string, which is never opened (no Docker; D16), and gets `ArgumentOutOfRangeException`.

**Acceptance Scenarios**:

1. **Given** `ClaimLease` of zero or negative, **When** the repository is constructed, **Then** `ArgumentOutOfRangeException` is thrown.

---

### User Story 5 - Rehydrate a Movie from persisted state (Priority: P2)

As the repository, I need a domain factory that restores a `Movie` from all persisted state without replaying transitions, so `GetAsync` can honour its contract without reflection.

**Independent Test**: Unit tests in `LamuFlix.UnitTests/Domain` round-trip every field with distinct non-default values, reject a blank title, and show legal/illegal transitions behave normally afterwards; an FsCheck property test (`MovieRehydratePropertyTests.cs`, D15) shows any valid field set round-trips.

**Acceptance Scenarios**:

1. **Given** valid persisted state, **When** `Movie.Rehydrate` is called, **Then** every field is restored and no `Mark*` is invoked.
2. **Given** a blank title, **When** `Movie.Rehydrate` is called, **Then** it throws `ArgumentException` (the same check as `Create`). Other invalid state is rejected earlier, when the value objects it takes are constructed; `Rehydrate` adds no new invariants (D1).

### Edge Cases

- Cancellation token is forwarded to every database call.
- `NextIdentityAsync` returning a value outside `MovieId` range throws `InvalidOperationException` (guard not integration-tested; D5); sequence gaps are acceptable.
- A claim on an id absent from the identity map still works (no sync step needed).
- Two claims after one another within an unexpired lease: second returns `false`, with no second increment (tested, T014).
- `AddAsync` of an id already tracked throws `InvalidOperationException`; EF/Npgsql exceptions propagate unchanged (D11).
- Known limitations, accepted: no concurrency token, so saves from different scopes are last-writer-wins for the fields each changed (Sentry R2, proposed follow-up F4); a save committed by another scope between the claim and the claim-sync re-read is picked up by the re-read (Sentry R1, D8).

## Requirements

### Functional Requirements

- **FR-001** (AC3, Q1): `public sealed class EfMovieRepository : IMovieRepository` at `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` implements `GetAsync`, `AddAsync`, `NextIdentityAsync`, `SaveChangesAsync`, `TryClaimForEnrichmentAsync`.
- **FR-002** (AC4, Q5): `TryClaimForEnrichmentAsync` is one LINQ `ExecuteUpdateAsync` over `Movies` with predicate `Id == id && Status == Pending && (LastAttemptAt == null || LastAttemptAt < leaseExpiry)`, setting `LastAttemptAt = now` and `EnrichmentAttempts = EnrichmentAttempts + 1` in the database, returning `rows == 1`. `now` is sampled once from the injected `TimeProvider`; `leaseExpiry = now - EnrichmentOptions.ClaimLease`; the CT is forwarded; no raw SQL.
- **FR-003** (AC5, Q6): The constructor `EfMovieRepository(LamuFlixDbContext, TimeProvider, IOptions<EnrichmentOptions>)` throws `ArgumentOutOfRangeException` when `ClaimLease <= TimeSpan.Zero`.
- **FR-004** (Q4, D13): `NextIdentityAsync` runs `SELECT nextval(pg_get_serial_sequence('movies','id'))` via `Database.SqlQuery<long>` (interpolated, no holes) through the existing context/connection with the CT, validates into `MovieId` (guard implemented, not integration-tested; D5); `AddAsync` inserts the explicit id. No schema change.
- **FR-005** (Q3): One identity map `MovieId -> (Movie, MovieRecord)` per repository/DbContext scope; `GetAsync` returns the same tracked aggregate; `AddAsync` tracks the supplied aggregate; `SaveChangesAsync` propagates changes to fields the aggregate models (including the six `Metadata` columns per the D9 mapping) and saves, preserving claim-owned fields the aggregate has not changed. Record-only data (`RottenTomatoesRating`, `MetaScore`, `PosterUrl`, actors/directors/genres) is never read or written (D9). `AddAsync` of an already-tracked id throws `InvalidOperationException`; `GetAsync` of an absent id returns `null`; database exceptions propagate unchanged (D11).
- **FR-006** (Q3, D2, D8): After a successful claim on an already-loaded aggregate, the tracked record's `LastAttemptAt` is stamped from the in-process `now` and its `EnrichmentAttempts` is re-read (no-tracking projection, CT forwarded); both are set as current and original values, without discarding unrelated pending changes. The repository's private `Baseline` claim fields are not advanced.
- **FR-007** (AC9, Q2): `Movie.Rehydrate(...)` exists in `src/LamuFlix.Core/Domain/Movie.cs`, takes the 11 aggregate properties with the D1 signature, calls no `Mark*`, replays no transitions, and keeps `Create`'s blank-title check without adding invariants. It is the only domain edit.
- **FR-008** (AC6, AC7, Q9): Concurrency and SQL-capture integration tests as described in User Story 1 over `PostgresFixture`, N = 50 (`[assumed]`, ASSUMPTIONS.md), run sequentially on one migrated database with a fresh context pair per iteration (D10, D14).
- **FR-009** (AC8, Q10): Postgres integration tests cover absent id, each terminal status, unexpired lease, expired lease (with persisted increment and time), strict boundary, a sequential double claim, five-member round trip, a saved `Mark*`, record-only preservation, and the load/claim/save seam. Fixed UTC instant representable at microsecond precision; small test `TimeProvider` subclass.
- **FR-010** (AC10, Q4, Q11, Q12): No new packages; no edits to `.csproj`, `Directory.*.props`, migrations, the model snapshot, `MovieIdValueGenerator`, `EnrichmentOptions`, `BannedSymbols.txt`, or any DI extension; no DI registration of the repository (deferred; scoped lifetime required when wired); `git diff --stat origin/main...HEAD` shows only frozen-scope files.
- **FR-011** (AC11, Q12, D15): Static-analysis gates, format check, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1` and the full test suite exit 0; mutation covers Core and Infrastructure; an unrunnable gate is reported "Could not run", never PASS/N/A.

### Key Entities

- **Movie (aggregate)**: domain type with enrichment status (Pending, Enriched, NotFound, Failed), `LastAttemptAt`, `EnrichmentAttempts`, and an optional `MovieMetadata` value (title, synopsis, release year, runtime, IMDb rating, IMDb id). It has no actors, directors or genres (D9).
- **MovieRecord**: EF persistence shape; unchanged. It also holds record-only data the aggregate does not model; the repository preserves that data untouched.
- **EnrichmentOptions.ClaimLease**: lease duration defining claim expiry.

## Success Criteria

### Measurable Outcomes

- **SC-001**: 50 of 50 concurrent two-worker claim iterations yield exactly one winner, with exactly one persisted increment each, per D6: 3 consecutive filtered runs of the concurrency class plus a pass inside the full suite, all 4 results recorded in the receipt.
- **SC-002**: Every claim invocation issues exactly one `UPDATE`; zero `LOCK TABLE` / `SELECT ... FOR UPDATE` commands observed.
- **SC-003**: All AC8 cases pass against real Postgres, with no mocked database behaviour.
- **SC-004**: Zero new packages and zero files changed outside the frozen scope.
- **SC-005**: Gates exit 0, or are explicitly reported "Could not run".

## Assumptions

- Concurrency test repetition count of 50 is a taste call `[assumed]` (Q9).
- Status `Pending` renders as `status = 0` via the existing `EnrichmentStatusConverter`; the plan records the rendered predicate.
- Out of scope: DI registration (Q11); `EnrichmentOptions` validation/default (F1, Q6); claim-count vs outcome-count reconciliation (F2, Q7); retry-within-lease refusal (F3, Q8, tracker comment on DEV-299); legacy `LamuFlix.Data`, `GenericRepository<T>`, `MovieService`, `EnrichmentJobProcessor`, `tests/LamuFlix.Test/EnrichmentTests.cs`; `Features:LocalPlay` and `Process.Start` untouched.
- Follow-ups F1-F3 belong to Rigger and are pending a verified receipt.
- Known risk: `scripts/run-mutation.ps1` excludes `IntegrationTests`; if Infrastructure mutation fails closed, it is reported "Could not run" and returned to Keel.
