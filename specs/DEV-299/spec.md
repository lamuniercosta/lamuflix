# Feature Specification: Core Use-Case Handlers (Library, Import, Enrichment, Watchlist, Playback)

**Feature Branch**: `feature/299-spec`

**Created**: 2026-09-30

**Status**: Draft (Gate 1 closed: D1, D2 owner checkboxes and the Q13 Patron ruling are open)

**Input**: DEV-299 (parent DEV-282, size L, no ui tag); `specs/DEV-299/brief.md` (AC1-AC8, Q1-Q12 ruled, Q13 open); `specs/DEV-299/CONCLUSIONS.md`; `specs/DEV-299/ASSUMPTIONS.md`; `specs/PRODUCT.md`

## Owner Decisions (Gate 1 stays closed until answered)

- [ ] **D1 / Q1: stranded requeue contract.** blocked: structural. May DEV-299 change the stranded-work claim and message contract so that the sweeper can enqueue work a worker can then claim?
  - The ticket's literal "claims stranded Pending movies and re-enqueues them" livelocks. The sweeper's `TryClaimForEnrichmentAsync` succeeds, the worker's mandatory claim (constitution 447-448) then returns false, and the message is acked unprocessed (CONCLUSIONS Q1).
  - Candidate answers, for the owner (not decided):
    - (A) drop `RequeueStrandedMoviesCommandHandler` from DEV-299 into a follow-up that owns the claim-handoff contract;
    - (B) the sweeper does not claim; a new `IMovieRepository` list-stranded method (lease-aged `Pending`, constitution 187-188) feeds enqueue, and the worker claim remains the only claim;
    - (C) a claim token in `EnrichmentRequested`, which changes a type constitution 445 fixes at `MovieId` + `Attempt` and so needs an amendment.
  - Blocks: User Story 8, FR-016, the `CONTEXT.md` term **Stranded Movie**, the lease and sweep-interval members of `EnrichmentOptions`, and the Q1 section of the ADR.
- [ ] **D2 / Q6: MovieId source on import.** blocked: structural. May DEV-299 extend the import contract to obtain a unique `MovieId` before `Movie.Create`, and which port or caller allocates it?
  - `MovieId.TryCreate` only validates a positive int. `Movie.Create` requires the id before `AddAsync`, and no port allocates or returns one (CONCLUSIONS Q6; `Movie.cs:41`).
  - Candidate answers (not decided):
    - (A) `IMovieRepository` gains an id-allocation member;
    - (B) the store assigns the id on save and `Movie` gains a pre-persistence construction path, which edits `Movie.cs`;
    - (C) defer `ImportMovieFolderCommandHandler` to a follow-up.
  - Blocks: User Story 7 and FR-015.

**Patron ruling still open (not an owner checkbox): Q13.** The `Core_features_must_depend_only_on_ports_domain_or_pipeline` architecture rule (V:1170-1195) forbids a `Features.*` type from depending on `LamuFlix.Core.Library` (where `MovieQuery` lives) and may flag `Ardalis.SmartEnum` member references. Blocks User Story 6 and FR-013 and FR-014. Recommended ruling (Keel): add `LamuFlix.Core.Library`, and `Ardalis.SmartEnum` if the probe in T021 fails, to the allow-list in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`.

## Clarifications

### Session 2026-09-30 (from CONCLUSIONS Q1-Q12; authoritative there)

- Q: How does the handler layer express "retry or dead-letter" without touching RabbitMQ? -> A: `RecordEnrichmentFailureCommandHandler` returns an `EnrichmentFailureDecision`; only the consumer republishes or dead-letters (Q2).
- Q: What does a missing movie produce? -> A: `NotFoundException` (new, Core/Pipeline), mapped to 404 `ProblemDetails` by the single Api `IExceptionHandler` in this PR (Q4).
- Q: What is the shared no-value result? -> A: `Unit`, a sealed record in Core/Pipeline (Q5, [assumed]).
- Q: What happens on a double watchlist add or remove? -> A: The domain's `InvalidTransitionException` propagates (Q8).
- Q: Is playback gated in the handler? -> A: No. The handler calls `IMediaPlayerLauncher`; `Features:LocalPlay` gating stays adapter-side (Q8).
- Q: Where does the manual retry restart? -> A: Only from NotFound or Failed; the message attempt restarts at 1 (Q7).
- Q: Which test libraries? -> A: xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net, referenced in the UnitTests csproj now with existing central versions (Q10).

## User Scenarios & Testing

### User Story 1 - Shared Pipeline and Domain Types (Priority: P1)

As a handler author, I need `NotFoundException`, `Unit`, `EnrichmentOptions`, `EnrichmentFailureAction` and `EnrichmentFailureDecision` in Core, so every handler shares one not-found signal, one no-value result and one failure-decision vocabulary.

**Why this priority**: Every other story depends on these types (brief, Task ordering 1-2).

**Independent Test**: `dotnet build` of `LamuFlix.Core` succeeds; `EnrichmentFailureAction` exposes exactly `Retry`, `RetryDelayed`, `DeadLetter`; the architecture tests pass unchanged.

**Acceptance Scenarios**:

1. **Given** the Core assembly, **When** the types are inspected, **Then** `NotFoundException` is a `sealed` exception mirroring `ValidationException`, `Unit` is a `sealed record`, and `EnrichmentOptions` is a `sealed record` with `MaxAttempts` and data annotations and no `IOptions` dependency.
2. **Given** `EnrichmentFailureAction`, **When** its members are listed, **Then** they are `Retry`, `RetryDelayed` and `DeadLetter`.
3. **Given** `EnrichmentFailureDecision(Action, NextAttempt)`, **When** it is constructed, **Then** it is a `sealed record` in `LamuFlix.Core.Domain`.

---

### User Story 2 - Watchlist Handlers (Priority: P1)

As a viewer, I need to add and remove a movie from my watchlist through use-case handlers, so the watchlist changes persist through the repository port only.

**Why this priority**: Smallest complete slice; proves the handler pattern, NotFound, and the test conventions.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Features.Watchlist" --nologo -v q`

**Acceptance Scenarios**:

1. **Given** a stored movie not in the watchlist, **When** `AddToWatchlistCommand` is handled, **Then** the movie is in the watchlist, `SaveChangesAsync` is called once and `Unit` is returned.
2. **Given** a stored movie in the watchlist, **When** `RemoveFromWatchlistCommand` is handled, **Then** it is removed, saved once, and `Unit` is returned.
3. **Given** no such movie, **When** either command is handled, **Then** `NotFoundException` is thrown and nothing is saved.
4. **Given** a movie already in the watchlist, **When** add is handled (or a movie not in it, when remove is handled), **Then** `InvalidTransitionException` propagates and nothing is saved.
5. **Given** a cancellation token, **When** either handler calls a port, **Then** the same token is passed to `GetAsync` and `SaveChangesAsync`.

---

### User Story 3 - Play Movie Handler (Priority: P1)

As a viewer, I need a play use case that resolves a movie and hands its file to the launcher port, so playback stays behind `IMediaPlayerLauncher`.

**Why this priority**: The ticket lists it; it carries the `Features:LocalPlay` boundary (§2.3 #5).

**Independent Test**: `dotnet test --filter "FullyQualifiedName~PlayMovieCommandHandlerTests" --nologo -v q`

**Acceptance Scenarios**:

1. **Given** `IMovieCatalog.GetDetailsAsync` returns details, **When** `PlayMovieCommand` is handled, **Then** `Launch(details.Path, details.Format)` is called once and `Unit` is returned.
2. **Given** `GetDetailsAsync` returns null, **When** it is handled, **Then** `NotFoundException` is thrown and `Launch` is never called.
3. **Given** the handler source (review check, not a test; enforced by FR-008 and code review), **When** inspected, **Then** it contains no `Process` API and no `Features:LocalPlay` check (adapter-side, Q8).

---

### User Story 4 - Enrichment Handlers (Priority: P1)

As the enrichment pipeline, I need claim, apply-result, record-failure and request-enrichment handlers, so the consumer, sweeper and API can drive enrichment through Core decisions with no RabbitMQ or database types.

**Why this priority**: It is the bulk of the ticket and the only story with branching decision logic (Q2, Q3, Q7).

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Features.Enrichment" --nologo -v q`

**Acceptance Scenarios**:

1. **Given** the repository claim returns true or false, **When** `ClaimEnrichmentCommand` is handled, **Then** that bool is returned unchanged.
2. **Given** a Pending movie and `MetadataLookupResult.Found`, **When** `ApplyEnrichmentResultCommand` is handled, **Then** `MarkEnriched(metadata, now)` runs, the movie is saved, and `EnrichmentStatus.Enriched` is returned.
3. **Given** a Pending movie and `NotFound`, **When** it is handled, **Then** `MarkNotFound(now)` runs, the movie is saved, and `NotFound` is returned.
4. **Given** `MetadataLookupResult.Failed`, **When** it is handled, **Then** `ArgumentException` is thrown before any mutation, load or save.
5. **Given** no such movie, **When** Apply is handled, **Then** `NotFoundException` is thrown and nothing is saved.
6. **Given** a retryable category and `Attempt < MaxAttempts`, **When** `RecordEnrichmentFailureCommand` is handled, **Then** the decision is `Retry` (or `RetryDelayed` for `RateLimited`) with `NextAttempt = Attempt + 1`; the movie is not loaded, mutated or saved.
7. **Given** a non-retryable category, or `Attempt >= MaxAttempts`, **When** it is handled, **Then** the movie is loaded (null -> `NotFoundException`), `MarkFailed(category, now)` runs, the movie is saved, and the decision is `DeadLetter` with `NextAttempt = null`.
8. **Given** any path of RecordFailure, **When** it completes, **Then** one structured log entry carries the movie id, attempt, category and action, and no exception text.
9. **Given** a movie in NotFound or Failed, **When** `RequestEnrichmentCommand` is handled, **Then** `RequestEnrichment()` runs, the movie is saved, then `EnrichmentRequested(id, 1)` is enqueued (save strictly before enqueue), and the `MovieId` is returned.
10. **Given** a movie in Pending or Enriched, **When** it is handled, **Then** `InvalidTransitionException(nameof(RequestEnrichment), status)` is thrown before any domain call; nothing is saved or enqueued.
11. **Given** a queue failure on enqueue, **When** RequestEnrichment is handled, **Then** the exception propagates (the sweeper is the recovery, constitution 454-455).

---

### User Story 5 - Not Found Maps to 404 (Priority: P1)

As an API client, I need a missing-movie failure to arrive as a 404 `ProblemDetails`, so handler `NotFoundException` never surfaces as a 500.

**Why this priority**: The constitution checklist (361-362) forces this edit once `NotFoundException` exists (Q4).

**Independent Test**: `dotnet test --filter "FullyQualifiedName~ValidationExceptionHandlerTests" --nologo -v q`

**Acceptance Scenarios**:

1. **Given** a `NotFoundException`, **When** the Api exception handler runs, **Then** the response is 404 `ProblemDetails` with a title and type, a `traceId`, and no exception type or message in the body.
2. **Given** a `ValidationException`, **When** it runs, **Then** the existing 422 behaviour is unchanged.
3. **Given** any other exception, **When** it runs, **Then** it returns false.

---

### User Story 6 - Library Query Handlers (Priority: P2) [BLOCKED: Q13]

As a browsing viewer, I need browse and details queries over the catalog port.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Features.Library" --nologo -v q`

**Acceptance Scenarios**:

1. **Given** a `MovieQuery`, **When** `BrowseMoviesQuery` is handled, **Then** the query is passed unchanged to `IMovieCatalog.BrowseAsync` and its `PagedResult<MovieSummary>` is returned.
2. **Given** a stored movie, **When** `GetMovieDetailsQuery` is handled, **Then** `MovieDetails` is returned.
3. **Given** no such movie, **When** it is handled, **Then** `NotFoundException` is thrown.

---

### User Story 7 - Import Movie Folder Handler (Priority: P2) [BLOCKED: D2]

As the library owner, I need to import a folder so a Pending movie exists and enrichment is requested.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Features.Import" --nologo -v q`

**Acceptance Scenarios** (final shape depends on D2):

1. **Given** a `LibraryPath`, **When** `ImportMovieFolderCommand` is handled, **Then** the order is Scan, `Movie.Create`, `AddAsync`, `SaveChangesAsync`, `EnqueueAsync(EnrichmentRequested(id, 1))`, and the `MovieId` is returned.
2. **Given** the queue throws, **When** it is handled, **Then** the exception propagates with no catch (the movie stays Pending for the sweeper).

---

### User Story 8 - Requeue Stranded Movies Handler (Priority: P2) [BLOCKED: D1]

As the sweeper, I need a handler that re-enqueues stranded Pending movies without a claim livelock. Result: `int` (count enqueued). Contract, ports and behaviour are defined by the D1 answer.

---

### User Story 9 - Documentation (Priority: P3)

As a maintainer, I need the ADR and the domain term, so the decision to keep enrichment decisions in Core is recorded.

**Acceptance Scenarios**:

1. **Given** the ADR `docs/adr/0017-enrichment-decisions-in-core-handlers.md`, **When** read, **Then** it records Q2's decision/options contract as accepted and Q1's section as "pending owner D1" until D1 is answered.
2. **Given** D1 keeps the requeue handler, **When** the PR lands, **Then** `CONTEXT.md` defines **Stranded Movie**.

### Edge Cases

- `Attempt` below 1 or `MaxAttempts` below 1 reaching RecordFailure: not validated in the handler. `EnrichmentOptions` annotations and validators belong to wiring (FR-018). With the formula `Attempt < MaxAttempts`, an out-of-range value dead-letters or retries deterministically and does not throw.
- A movie deleted between retry decision and dead-letter load: `NotFoundException`.
- A duplicate-path import: not detected here. Noted, no ticket filed until existing tickets are checked (Patron, Q6).
- Cancellation before a port call: the token reaches every async port; handlers do not swallow `OperationCanceledException`.
- Handler tests that build a Movie in a given status use the real domain transitions (`Create` -> `MarkNotFound`), not reflection.

## Requirements

### Functional Requirements

- **FR-001**: Every use case MUST be a `sealed` class implementing `ICommandHandler<,>` or `IQueryHandler<,>` over a `sealed record` request, in `src/LamuFlix.Core/Features/<Feature>/`, one type per file.
- **FR-002**: `LamuFlix.Core` MUST reference no database, RabbitMQ or new package (AC1, AC7). No handler takes more than three ports; `TimeProvider`, options and the logger are not ports.
- **FR-003**: `NotFoundException` MUST exist in `LamuFlix.Core.Pipeline`, `sealed`, mirroring `ValidationException`.
- **FR-004**: `Unit` MUST exist in `LamuFlix.Core.Pipeline` as a `sealed record`.
- **FR-005**: `EnrichmentOptions` MUST exist in `LamuFlix.Core.Pipeline` as a plain `sealed record` with `MaxAttempts` and data annotations, with no `IOptions`. Lease and sweep-interval members wait for D1.
- **FR-006**: `EnrichmentFailureAction` (SmartEnum: `Retry`, `RetryDelayed`, `DeadLetter`) and `EnrichmentFailureDecision(EnrichmentFailureAction Action, int? NextAttempt)` MUST exist in `LamuFlix.Core.Domain`.
- **FR-007**: Add/RemoveFromWatchlist handlers MUST return `Unit`, throw `NotFoundException` on a null movie, and let `InvalidTransitionException` propagate.
- **FR-008**: `PlayMovieCommandHandler` MUST use `IMovieCatalog` and `IMediaPlayerLauncher`, and MUST NOT check `Features:LocalPlay` or start a process.
- **FR-009**: `ClaimEnrichmentCommandHandler` MUST return the `TryClaimForEnrichmentAsync` result unchanged.
- **FR-010**: `ApplyEnrichmentResultCommandHandler` MUST implement the behaviour in User Story 4 scenarios 2-5 and return the new `EnrichmentStatus`.
- **FR-011**: `RecordEnrichmentFailureCommandHandler` MUST never publish, MUST follow scenarios 6-8, and MUST log without exception text.
- **FR-012**: `RequestEnrichmentCommandHandler` MUST follow scenarios 9-11 with save before enqueue.
- **FR-013** [BLOCKED: Q13]: `BrowseMoviesQueryHandler` MUST pass the query straight to the catalog.
- **FR-014** [BLOCKED: Q13]: `GetMovieDetailsQueryHandler` MUST throw `NotFoundException` on null.
- **FR-015** [BLOCKED: D2]: `ImportMovieFolderCommandHandler` MUST follow User Story 7 and take a `LibraryPath`.
- **FR-016** [BLOCKED: D1]: `RequeueStrandedMoviesCommandHandler` MUST return the count enqueued under the D1 contract.
- **FR-017**: The Api `IExceptionHandler` MUST map `NotFoundException` to 404 `ProblemDetails` (User Story 5), with tests extended beside the existing ones.
- **FR-018**: The spec MUST list these wiring prerequisites as required-before-use and NOT deliver them: DI/`AddHandler` registration, FluentValidation validators, `EnrichmentOptions` binding, and the LocalPlay-gated launcher.
- **FR-019**: Every async port call MUST receive the `CancellationToken`; `TimeProvider.GetUtcNow()` MUST be the only time source; there MUST be no logging beyond RecordFailure's decision log and no comments except AAA headers.
- **FR-020**: Handler tests MUST live in `tests/LamuFlix.UnitTests/Features/<Feature>/`, cover every legal and exception path, and `LamuFlix.UnitTests.csproj` MUST add `AutoFixture` and `Faker.Net` `PackageReference`s with no version attribute.
- **FR-021**: `LamuFlix.ArchitectureTests` MUST pass unchanged unless the Q13 ruling authorizes the allow-list edit.
- **FR-022**: The diff MUST contain only frozen-scope files (brief, Frozen scope).

### Key Entities

- **Movie** (existing aggregate): Pending/Enriched/NotFound/Failed status, watchlist flag, attempt count. Not edited unless D1/D2 authorizes it.
- **EnrichmentFailureDecision**: the closed outcome of a failed attempt (`Action`, `NextAttempt`).
- **Stranded Movie** (new term, D1-gated): a Pending movie whose lease has aged out.

## Out of Scope

DI registration; FluentValidation validators (Core cannot reference them); options binding; endpoints and OpenAPI; `EnrichmentConsumer`, the sweeper hosted service and adapters; `DisabledMediaPlayerLauncher` and LocalPlay gating; `TelemetryConstants` additions including `lamuflix.import.count`; `IMetadataProvider` invocation and `MetadataLookup` construction; duplicate-path import detection; legacy Web/Data/Worker/Test code including `MovieService.PlayMovie`, `UnitTest1` and `EnrichmentFailureClassifier` with its `LamuFlix.Test` test; any `Movie.cs` or port-signature edit not authorized by D1/D2.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All unblocked handlers compile with zero warnings and Core has zero new package references (AC1).
- **SC-002**: 100% of handler unit tests pass, with every legal and exception path covered (AC2, AC6).
- **SC-003**: The Api returns 404 `ProblemDetails` for `NotFoundException` and the 422 mapping is unchanged (AC4).
- **SC-004**: The three static-analysis gates exit 0, the cyclomatic refactor gate at threshold 6 passes, and `dotnet format --verify-no-changes` is clean (AC8).
- **SC-005**: Stryker on `LamuFlix.Core` meets the break threshold of 80.
- **SC-006**: `git diff --stat origin/main...HEAD` lists only frozen-scope files (FR-022).

## Assumptions

- `EnrichmentFailureCategory.IsRetryable` defines "retryable", and `RateLimited` is the only delayed-retry category (Q2).
- One type per file; request record and handler are sibling files (Quill's convention choice, as in DEV-298).
- `Unit` carries no members beyond a shared instance if the record shape needs one; the name is [assumed] in ASSUMPTIONS.md.
- `Ardalis.SmartEnum` is already referenced by Core (used by `EnrichmentStatus`), so `EnrichmentFailureAction` adds no package.
