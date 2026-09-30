# Tasks: Core Use-Case Handlers

**Input**: `specs/DEV-299/` (`spec.md`, `plan.md`, `brief.md` Task ordering, Frozen scope, Gate expectations)

**Prerequisites**: plan.md, spec.md, brief.md (frozen decisions)

**Tests**: Included (spec FR-020; brief Q10). Each handler's test task precedes its implementation task and must fail first.

**Organization**: Brief ordering: shared types, csproj references, Watchlist, Playback, Enrichment, Api 404, Library, Import, Requeue, ADR, gates. User stories: US1 shared types, US2 Watchlist, US3 Playback, US4 Enrichment, US5 Api 404, US6 Library, US7 Import, US8 Requeue, US9 docs.

## Owner and Patron blockers (preserved from spec.md; Gate 1 closed until answered)

- [ ] **D1 / Q1** (owner): stranded requeue contract. Blocks T030a, T031-T033, the lease/sweep members of `EnrichmentOptions`, the `CONTEXT.md` term and the ADR Q1 section.
- [ ] **D2 / Q6** (owner): MovieId source on import. Blocks T028-T030.
- [ ] **D3** (owner): wiring (registration, validators, options `ValidateOnStart`) deferred past this PR, a constitution departure. Blocks no task under (A); under (B) new tasks are added.
- [x] **Q13** (Patron): feature allow-list. Patron ruled Q13 (CONCLUSIONS.md #13, e865874): add `LamuFlix.Core.Library`; T024-T027 unblocked.

`[BLOCKED: X]` tasks are planned, not dropped. Do not start them until the blocker is checked or ruled and the task is re-scoped to the answer.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallel-safe (different files, no dependencies)
- Sealed types, one type per file. Request record `XCommand.cs`/`XQuery.cs` and handler `XCommandHandler.cs` are siblings. Handlers forward the `CancellationToken` to every async port, use `TimeProvider.GetUtcNow()` for time, and carry no comments except AAA headers in tests.

---

## Phase 1: Setup

- [ ] T001 Verify worktree `F:\Dev\LamuFlix.worktrees\feature-299` (created by `/task` in Phase B; the pickup drift check confirms the exact path) and branch, record the baseline test count, and confirm `.specify/feature.json` and the untracked `recon-*`/`_tmp_*` files stay unstaged (outside frozen scope).
- [ ] T002 [Setup] Add `AutoFixture` and `Faker.Net` `PackageReference`s (no version) to `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`. Build to confirm central versions resolve (FR-020).

---

## Phase 2: US1 Shared types (Priority: P1) 🎯

**Goal**: shared Pipeline and Domain vocabulary (spec FR-003 to FR-006).

- [ ] T003 [P] [US1] Create `src/LamuFlix.Core/Pipeline/NotFoundException.cs`: `sealed`, mirroring `ValidationException` (no `IReadOnlyDictionary`; message constant, no exception text in API output).
- [ ] T004 [P] [US1] Create `src/LamuFlix.Core/Pipeline/Unit.cs`: `public sealed record Unit` with no members, a `private Unit()` constructor and `public static readonly Unit Value`; handlers return `Unit.Value`.
- [x] T005 [P] [US1] SUPERSEDED by Q14: no new file. `EnrichmentOptions` reuses DEV-300's existing `src/LamuFlix.Core/Options/EnrichmentOptions.cs` (`MaxAttempts`, `SweepInterval`, `ClaimLease` already exist); no Pipeline type is created.
- [x] T005a [US1] SUPERSEDED by Q14: `Pipeline/EnrichmentOptionsTests.cs` is not created; the existing `Core/Options/EnrichmentOptions.cs` is reused as is. Sanctioned instead: `tests/LamuFlix.UnitTests/Pipeline/NotFoundExceptionTests.cs` (mutation-gate coverage for `NotFoundException`).
- [ ] T006 [P] [US1] Create `src/LamuFlix.Core/Domain/EnrichmentFailureAction.cs`: `SmartEnum<EnrichmentFailureAction, int>` with `Retry`, `RetryDelayed`, `DeadLetter` (pattern of `EnrichmentStatus.cs`).
- [ ] T007 [US1] Create `src/LamuFlix.Core/Domain/EnrichmentFailureDecision.cs`: `public sealed record EnrichmentFailureDecision(EnrichmentFailureAction Action, int? NextAttempt)`. No constructor guard; the FR-006 invariant is asserted in T017.
- [ ] T008 [US1] Add `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs` (sealed `TimeProvider` subclass with a fixed `GetUtcNow`) and `tests/LamuFlix.UnitTests/Features/RecordingLogger.cs` (sealed `RecordingLogger<T> : ILogger<T>` capturing level, structured state pairs and the exception argument). Shared test helpers, one type per file.

**Checkpoint**: Core builds; `LamuFlix.ArchitectureTests` green.

---

## Phase 3: US2 Watchlist (Priority: P1)

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Features.Watchlist" --nologo -v q`

- [ ] T009 [P] [US2] Tests in `tests/LamuFlix.UnitTests/Features/Watchlist/AddToWatchlistCommandHandlerTests.cs` and `RemoveFromWatchlistCommandHandlerTests.cs`: happy path (saved once, `Unit`), null -> `NotFoundException` with no save, `InvalidTransitionException` propagation with no save, CT forwarded (spec US2).
- [ ] T010 [US2] Create `src/LamuFlix.Core/Features/Watchlist/AddToWatchlistCommand.cs`, `AddToWatchlistCommandHandler.cs`, `RemoveFromWatchlistCommand.cs`, `RemoveFromWatchlistCommandHandler.cs`: `IMovieRepository` only; `GetAsync`, null -> `NotFoundException`, domain transition, `SaveChangesAsync`, return `Unit`.

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 4: US3 Playback (Priority: P1)

**Independent Test**: `dotnet test --filter "FullyQualifiedName~PlayMovieCommandHandlerTests" --nologo -v q`

- [ ] T011 [P] [US3] Tests in `tests/LamuFlix.UnitTests/Features/Playback/PlayMovieCommandHandlerTests.cs`: launch called once with `Path`/`Format`; null -> `NotFoundException` and `DidNotReceive` `Launch`.
- [ ] T012 [US3] Create `src/LamuFlix.Core/Features/Playback/PlayMovieCommand.cs` and `PlayMovieCommandHandler.cs` (`IMovieCatalog`, `IMediaPlayerLauncher`). No `Process`, no `LocalPlay` check.

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 5: US4 Enrichment (Priority: P1)

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Features.Enrichment" --nologo -v q`

- [ ] T013 [P] [US4] Tests `ClaimEnrichmentCommandHandlerTests.cs` (true and false returned unchanged, CT forwarded) in `tests/LamuFlix.UnitTests/Features/Enrichment/`.
- [ ] T014 [US4] Create `ClaimEnrichmentCommand.cs` and `ClaimEnrichmentCommandHandler.cs` in `src/LamuFlix.Core/Features/Enrichment/`; result `bool`.
- [ ] T015 [P] [US4] Tests `ApplyEnrichmentResultCommandHandlerTests.cs`: Found -> Enriched saved; NotFound -> NotFound saved; Failed -> `ArgumentException` before any load/save; null -> `NotFoundException`; fixed time asserted on `LastAttemptAt`.
- [ ] T016 [US4] Create `ApplyEnrichmentResultCommand.cs` and handler (`IMovieRepository`, `TimeProvider`): result `EnrichmentStatus`.
- [ ] T017 [P] [US4] Tests `RecordEnrichmentFailureCommandHandlerTests.cs`: `Theory`/`MemberData` over 4 categories x below/at `MaxAttempts`; retry path asserts `DidNotReceive` `GetAsync` and `SaveChangesAsync`; every retry row asserts `NextAttempt = Attempt + 1`; dead-letter asserts `MarkFailed` state, save, `NextAttempt = null`; null on dead-letter -> `NotFoundException`; exactly one log entry with fields (id, attempt, category, action) and a null exception argument, captured with `RecordingLogger<T>` from T008 (no NSubstitute on `Log`; no new package: no FakeLogger, no Microsoft.Extensions.Logging.Testing); nothing published.
- [ ] T018 [US4] Create `RecordEnrichmentFailureCommand.cs` and handler (`IMovieRepository`, `TimeProvider`, `EnrichmentOptions`, `ILogger<>`): result `EnrichmentFailureDecision`; use `Category.IsRetryable`, `RateLimited` -> `RetryDelayed`.
- [ ] T019 [P] [US4] Tests `RequestEnrichmentCommandHandlerTests.cs`: `Theory` over 4 statuses (NotFound/Failed succeed; Pending/Enriched -> `InvalidTransitionException` before any domain call); save-before-enqueue ordering; enqueue `EnrichmentRequested(id, 1)`; queue failure propagates; null -> `NotFoundException`.
- [ ] T020 [US4] Create `RequestEnrichmentCommand.cs` and handler (`IMovieRepository`, `IEnrichmentQueue`): result `MovieId`. Status check uses `==` against `EnrichmentStatus.NotFound`/`Failed`; if T021 is red on `Ardalis.SmartEnum`, switch to `ReferenceEquals(movie.Status, EnrichmentStatus.X)`. Throw `InvalidTransitionException(nameof(RequestEnrichment), movie.Status.ToString())`.
- [ ] T021 [US4] **SmartEnum probe**: with T014, T016, T018 and T020 compiled (RecordFailure also references SmartEnum statics), run `dotnet test tests/LamuFlix.ArchitectureTests --nologo -v q`. Green: record "no SmartEnum allow-list change needed" in the PR body. Red naming `Ardalis.SmartEnum`: switch every SmartEnum member comparison in the Enrichment handlers (T016, T020) to `ReferenceEquals`, keep T015/T019 green, and re-run. Still red: stop and send the finding to Keel (`maestri ask "Keel" "[from Quill] needs decision: ..."`); Keel routes it to Patron for a separate ruling (per Q13). Do not add SmartEnum to the arch test.

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 6: US5 Api 404 (Priority: P1)

- [ ] T022 [US5] Extend `tests/LamuFlix.Test/ValidationExceptionHandlerTests.cs` with a `NotFoundException` -> 404 `ProblemDetails` test (title, type, `traceId`, no exception leak); existing 422 and other-exception tests unchanged.
- [ ] T023 [US5] Add the `NotFoundException` -> 404 arm to `src/LamuFlix.Api/ExceptionHandling/ValidationExceptionHandler.cs` (keep complexity <= 6; extract a helper if needed).

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 7: US6 Library

- [ ] T024 [US6] Tests `BrowseMoviesQueryHandlerTests.cs`, `GetMovieDetailsQueryHandlerTests.cs` in `tests/LamuFlix.UnitTests/Features/Library/`.
- [ ] T025 [US6] Create `BrowseMoviesQuery.cs` and handler; result `PagedResult<MovieSummary>`.
- [ ] T026 [US6] Create `GetMovieDetailsQuery.cs` and handler; result `MovieDetails`; null -> `NotFoundException`.
- [ ] T027 [US6] Add `LamuFlix.Core.Library` (and nothing else) to `Core_features_must_depend_only_on_ports_domain_or_pipeline` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` (Q13 ruling); run the architecture tests. `MovieQuery` and its Library value types stay in `LamuFlix.Core.Library`.

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 8: US7 Import [BLOCKED: D2]

- [ ] T028 [BLOCKED: D2] [US7] Tests `ImportMovieFolderCommandHandlerTests.cs`: call order Scan -> Create -> Add -> Save -> Enqueue(`EnrichmentRequested(id, 1)`); queue failure propagates; no Save and no Enqueue when `AddAsync` throws; no Enqueue when Save throws (spec US7 scenario 3).
- [ ] T029 [BLOCKED: D2] [US7] Create `ImportMovieFolderCommand.cs` (`LibraryPath Folder`) and handler (`IMediaLibraryScanner`, `IMovieRepository`, `IEnrichmentQueue`); result `MovieId`; id source per D2.
- [ ] T030 [BLOCKED: D2] [US7] Apply the D2-authorized contract edit (`IMovieRepository` member, or `Movie.cs` path) if the owner chose (A) or (B); none if (C).

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 9: US8 Requeue [BLOCKED: D1]

- [ ] T030a [BLOCKED: D1] [US8] Apply the D1-authorized contract edit: (B) add the list-stranded member to `IMovieRepository` and update its implementations; (C) change `EnrichmentRequested` only after the constitution amendment lands; (A) none. This is an unnamed-file edit (§2.3 #6) authorized by the owner's answer.
- [ ] T031 [BLOCKED: D1] [US8] Tests for `RequeueStrandedMoviesCommandHandler` per the D1 contract.
- [ ] T032 [BLOCKED: D1] [US8] Create `RequeueStrandedMoviesCommand.cs` and handler; result `int`; add lease/sweep members to `EnrichmentOptions` if D1 requires.
- [ ] T033 [BLOCKED: D1] [US8] Add the term **Stranded Movie** to `CONTEXT.md`.

**Checkpoint**: `LamuFlix.ArchitectureTests` green.

---

## Phase 10: US9 Docs and Gates

- [x] T034 [P] [US9] Keel drafts `docs/adr/0017-enrichment-decisions-in-core-handlers.md` (Q2 decision/options accepted; Q1 section "pending owner D1" until answered). Quill does not author the ADR. Done by Keel (drafted, commit 8477e1e).
- [ ] T035 Run gates on the diff: `dotnet build`; `dotnet test`; `./scripts/run-roslyn-analyzers.ps1`; `./scripts/run-cyclomatic-complexity.ps1` then `-Threshold 6`; `./scripts/run-jetbrains-inspectcode.ps1`; `dotnet format --verify-no-changes`; Stryker on `LamuFlix.Core` (break 80); `git diff --stat origin/main...HEAD` shows only frozen-scope files.
- [ ] T036 Draft the PR body note (merge-bar proof, findings summary, wiring prerequisites list per FR-018) and report to Bernstein.

## Dependencies

- T003-T008 (with T005a before T005) before all handler tasks. T030a before T031. T002 before any test task.
- T009/T011/T013/T015/T017/T019 (tests) may run in parallel after Phase 2; each implementation task follows its test.
- T021 (probe) needs T014, T016, T018 and T020 and closes Phase 5.
- Blocked phases start only after their blocker is cleared; T035 and T036 run last.
