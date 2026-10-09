# Tasks: DEV-316 StrandedMovieSweeper

**Input**: `specs/DEV-316/spec.md`, `specs/DEV-316/plan.md` (from `specs/DEV-316/brief.md`)

**Dependency order** (per brief section 7): validator/port + production EF selection + doubles -> Core orchestration -> hosted loop -> Api wiring/isolation -> real recovery + composition/startup validation -> gates + size-M review.

## Phase 1: Foundational - validator, port, production EF selection, compile-compatible doubles

- [X] T001 [US1] Create sealed `EnrichmentOptionsValidator : IValidateOptions<EnrichmentOptions>` in `src/LamuFlix.Core/Options/EnrichmentOptionsValidator.cs` (ClaimLease/SweepInterval > zero, key-naming messages; preserve defaults, MaxAttempts Range, EF lease guard).
- [X] T002 [US1] Add `FindStrandedMovieIdsAsync(leaseCutoff, ct)` to `src/LamuFlix.Core/Ports/IMovieRepository.cs` (read-only ID selection).
- [X] T003 [US1] Implement IDs-only no-tracking selection in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` (`Pending AND (LastAttemptAt == null OR < cutoff)`, strict `<`, no mutation).
- [X] T004 [P] [US1] Implement new member in `tests/LamuFlix.IntegrationTests/LeaseAwareMovieRepository.cs`.
- [X] T005 [P] [US1] Implement new member in `UnusedMovieRepository` in `tests/LamuFlix.IntegrationTests/MetadataProviderCompositionTests.cs`.
- [X] T006 [P] [US1] Implement new member in `NoMovieRepository` in `tests/LamuFlix.UnitTests/RabbitMq/RabbitMqServiceCollectionExtensionsTests.cs`.
- [X] T007 [P] [US1] Create timer-capable `ManualTimeProvider` in `tests/LamuFlix.Tests.Common/ManualTimeProvider.cs`.
- [X] T008 [US1] Register validator alongside existing binding in `src/LamuFlix.ServiceDefaults/Extensions.cs` (existing `ValidateOnStart` runs it).
- [X] T009 [US1] Dedicated validator tests in `tests/LamuFlix.UnitTests/Options/EnrichmentOptionsValidatorTests.cs` (zero/negative per duration, valid defaults, key-specific failures, startup wiring); MaxAttempts tests untouched.
- [X] T010 [US1] EF selection coverage in `tests/LamuFlix.IntegrationTests/StrandedMovieSweeperTests.cs`: null last attempt selected; strictly expired selected; fresh excluded; exact boundary excluded; non-`Pending` excluded; no persistence mutation (real database; never a mocked driver).

**Checkpoint**: Port + production EF selection + validator + all doubles compile; each later step builds clean.

## Phase 2: User Story 1 - Core orchestration (P1)

**Goal**: Core dispatch of EF-selected stranded IDs through the existing requeue path.

**Independent Test**: Handler unit tests (empty selection, captured cutoff, requeue-path dispatch, cancellation forwarding, failure propagation).

- [X] T011 [US1] Create `SweepStrandedMoviesCommand` in `src/LamuFlix.Core/Features/Enrichment/SweepStrandedMoviesCommand.cs` (existing `MovieId`/result conventions).
- [X] T012 [US1] Implement `SweepStrandedMoviesCommandHandler` in `src/LamuFlix.Core/Features/Enrichment/SweepStrandedMoviesCommandHandler.cs` (one `TimeProvider` now per pass, cutoff = now - ClaimLease, read-only selection, dispatch existing requeue handler via decorated abstraction, Attempt=1 retained, cancellation forwarded, failure propagated; no attempt filter/mutation).
- [X] T013 [US1] Unit tests in `tests/LamuFlix.UnitTests/Features/Enrichment/SweepStrandedMoviesCommandHandlerTests.cs` (empty selection, captured cutoff, requeue-path dispatch, cancellation forwarding, failure propagation).

## Phase 3: User Story 3 - Hosted loop, wiring, isolation (P3)

**Goal**: Thin serial hosted loop, decorated Api composition, host isolation, ADR note.

**Independent Test**: Lifecycle unit tests + wiring/isolation edits (recovery hosts retain sweeper, unrelated hosts remove only it).

- [X] T014 [US3] Implement thin `StrandedMovieSweeper` in `src/LamuFlix.Infrastructure/Enrichment/StrandedMovieSweeper.cs` (immediate first pass, scope per pass, resolve decorated sweep handler, full `SweepInterval` delay via `TimeProvider` after success/failure, serial, Error-with-exception log, `stoppingToken` into query/enqueue, clean shutdown).
- [X] T015 [US3] Lifecycle tests in `tests/LamuFlix.UnitTests/Enrichment/StrandedMovieSweeperTests.cs` (immediate pass, interval after success/failure, serial execution, scope disposal, Error logging, later recovery, query/enqueue cancellation, clean shutdown; bounded synchronization, no wall-clock sleeps).
- [X] T016 [US3] Register sweep + requeue handlers via `AddHandler` in `src/LamuFlix.Api/HandlerRegistration.cs` (decorators preserved; sweep injects decorated requeue abstraction).
- [X] T017 [US3] Unconditional `AddHostedService<StrandedMovieSweeper>` in `src/LamuFlix.Api/Program.cs` (no gate/flag changes).
- [X] T018 [US3] Isolation edit in `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs` (unrelated hosts remove only sweeper; recovery hosts opt in and assert registration).
- [X] T019 [US3] Bounded status note in `docs/adr/ADR-0004.md` lines 92-96 (design-only/open-handoff wording now stale; cite ADR-0017:69-83 and DEV-316; preserve earlier decision history; no new ADR).

## Phase 4: Real recovery + composition/startup validation (US1/US2 acceptance)

**Goal**: Lost dual-write-gap and stale-claim recovery on real PostgreSQL + RabbitMQ with the actual registered sweeper, plus runnable composition/startup assertions.

**Independent Test**: Real PostgreSQL + RabbitMQ recovery tests (lost gap + stale claim, each observing `EnrichmentRequested(id,1)` with no sweep mutation before the intentional later claim) plus composition/startup assertions.

- [X] T020 [US1] Lost-dual-write-gap recovery test in `tests/LamuFlix.IntegrationTests/StrandedMovieSweeperTests.cs` (after hosting and composition): retain the actual registered sweeper; persist `Pending` row with null `LastAttemptAt` and publish no message with broker prepared before the immediate pass; observe `EnrichmentRequested(id,1)` on the real queue; verify no sweep mutation of status/`LastAttemptAt`/`EnrichmentAttempts`; then verify a subsequent real claim succeeds.
- [X] T021 [US2] Stale-claim recovery test in `tests/LamuFlix.IntegrationTests/StrandedMovieSweeperTests.cs` (real claim + deterministic advancement; observe `EnrichmentRequested(id,1)` on real queue; prove subsequent real claim; assert no sweep mutation of status/`LastAttemptAt`/`EnrichmentAttempts`).
- [X] T022 [US3] Composition and startup assertions after Api wiring: both decorated `ICommandHandler` registrations (sweep + requeue) resolve in scope; recovery hosts retain the unconditional sweeper; unrelated hosts remove only the sweeper; nonpositive `ClaimLease`/`SweepInterval` fail startup with key-specific messages.

## Phase 5: Gates, review, handoff

- [ ] T023 Run all applicable configured gates at the exact delivery head and capture receipts: build/analyzers (`run-roslyn-analyzers.ps1`), complexity (`run-cyclomatic-complexity.ps1`), InspectCode (`run-jetbrains-inspectcode.ps1`), tests + property tests (`dotnet test`, `run-property-tests.ps1`), vulnerable packages (`run-vulnerable-packages.ps1`), formatting, diff review, pre-PR mutation (`run-mutation.ps1`); report Api exclusion separately and web scope-empty verdict under committed config; unrunnable required gate blocks; SKIPPED/SKIP/N/A never PASS.
- [ ] T024 Complete size-M three-axis review (Sentry Risk, Ledger Standards, Compass Spec) plus security and mutation/coverage lanes within the two-round cap; publish findings + summary on the PR; adjudicate Lows explicitly; unresolved blocking finding at cap stops the loop.

## Dependencies & Execution Order

- Phases run in order 1 -> 2 -> 3 -> 4 -> 5 (brief section 7 dependency order preserved: EF selection -> Core orchestration -> hosted loop -> wiring and isolation -> real recovery plus composition and startup -> gates).
- Within Phase 1, T004/T005/T006/T007 parallelize after T002; T003 after T002; T009 after T001/T008; T010 after T003.
- Actual registered-host acceptance tests (T020/T021) run after hosted-loop and Api composition/isolation tasks (Phase 3); composition/startup assertions (T022) run after Api wiring.
- No web, Worker, schema, package, endpoint, LocalPlay, or secret work is in scope; anything else is a follow-up issue, not a task.

## Parallel Opportunities

- T004, T005, T006, T007 in parallel (disjoint files).
- EF matrix (T010), handler unit tests (T013), lifecycle tests (T015), validator tests (T009) are disjoint files and may run in parallel once their subjects exist.
