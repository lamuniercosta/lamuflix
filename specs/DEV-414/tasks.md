# Tasks: DEV-414 FeaturesRequested consumer and movie_features persistence

**Input**: Design documents from `specs/DEV-414/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why). Code Phases 1–5 also require the Phase B entry checklist below. Missing Phase B evidence blocks T001.

**Tests**: Included. Brief Q8 and FR-015 require them. Author the behavior test before the production edit it locks. When the types under test do not yet exist, add only the minimum compilable contracts needed so those tests compile; do not treat missing-type compile failures as behavioral red tests. When types already exist and the locked behavior is not already implemented, demonstrate behavioral failure before the behavior edit when possible. Drive each phase's tests green before that phase's commit. Use existing xUnit, assertion libraries, FakeEmbeddingGenerator, deterministic TimeProvider, NSubstitute for Core ports, and Testcontainers PostgreSQL/RabbitMQ. Never mock drivers or Infrastructure. No external Ollama download. No new test dependency. Single Anvil ownership for Phases 1–5.

**Organization**: Phases follow brief Q8 dependency order. Anvil implements Phases 1–5, one phase per ask, and stops after Phase 5. Phase 6 is Cog, then Gauge. Do not start Phase 1 until the Phase B entry checklist is satisfied.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 persist embedding, US2 retry/DLQ, US3 recovery, US4 activation/E2E.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Schedule Build workspace entry checks. Do not run gates, tests, or analyzers from this Spec workspace. Missing evidence blocks T001.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [ ] TB000 Confirm a recorded Epic 7 (SPA) completion receipt is present in the task note (`specs/PRODUCT.md` §2: Epic 9 / recommendations-pgvector deferred until Epic 7 completes; brief Q9 does not waive sequencing). This item requires the receipt record only; it does not assert whether Epic 7 is currently complete. Absence of a recorded receipt blocks code.
- [ ] TB001 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt.
- [ ] TB002 Obtain Phase B drift recon against `origin/main` for the authorized file set (Wisp → `recon-DEV-414` refresh). Reconcile FeaturesRequested topology, IEmbeddingGenerator, IMovieFeaturesRepository, StrandedMovieSweeper, publisher PublishCoreAsync options, and EnrichmentConsumer pattern against recon-DEV-414 / recon-DEV-414-2. Off-envelope changes stop to Patron.
- [ ] TB003 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef` mode) recorded in `DEV-414`.
- [ ] TB004 Keel checks or amends drift against the brief/spec before any Code Phase 1 edit. Missing Keel drift check blocks T001.

**Checkpoint**: Phase B entry evidence is complete (including the recorded Epic 7 completion receipt). Only then may Anvil start T001.

---

## Phase 1: Port extensions, scalar plot read, and anti-join (US1 foundational)

**Purpose**: Extend IMovieRepository / IMovieFeaturesRepository and EF adapters with GetPlotAsync and FindMovieIdsMissingFeaturesAsync. Real-database query tests. Blocks handler and recovery behavior proofs that depend on these seams.

**Independent Test**: Testcontainers PostgreSQL proves scalar plot read (missing/null share null; stored plot returned unchanged, including whitespace-only values) and anti-join selection that matches handler canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode nonblank semantics (handler-nonblank + missing features only; whitespace-only variants excluded; existing features excluded; no enrichment-status filter), with a real-PostgreSQL parity corpus.

**Checkpoint**: Ports and EF query seams compile and pass real-DB query tests. No consumer/handler behavior claimed yet.

### Tests for Phase 1

- [X] T001 [P] [US1] Author integration tests for `GetPlotAsync` against pinned PostgreSQL fixture in `tests/LamuFlix.IntegrationTests/` (focused file beside existing repository tests): missing movie → null; null plot → null; whitespace-only plot behavior is not asserted here if the scalar projection returns the stored string (whitespace no-op belongs to handler Phase 2); nonblank plot returns the stored plot; CancellationToken propagation. No query/serialization mocks. Minimum compilable contracts when the method is absent; behavioral failure before T003 when possible.
- [X] T002 [P] [US1] Author integration tests for `FindMovieIdsMissingFeaturesAsync` against pinned PostgreSQL using a real-PostgreSQL parity corpus covering empty, spaces-only, tab/newline-only, Unicode-whitespace-only, and mixed whitespace/text plots: every DB-selected id MUST have handler-nonblank input under canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics, and every eligible handler-nonblank movie without a features row MUST be selected; null/missing movie and existing-row exclusions remain. Do not assert SQL TRIM/btrim parity as fact; if exact parity cannot be expressed in PostgreSQL, return needs decision to Patron. Also prove: excludes existing features rows; does not filter on EnrichmentStatus; CancellationToken propagation. No-N+1 proof requires observable real-PostgreSQL query/command evidence that the anti-join runs without per-movie feature lookups; never mock the driver or Infrastructure and add no new test dependency. Minimum compilable contracts when absent; behavioral failure before T004 when possible.

### Implementation for Phase 1

- [X] T003 [US1] After T001, add `GetPlotAsync(MovieId, CancellationToken) -> Task<string?>` to `src/LamuFlix.Core/Ports/IMovieRepository.cs` and implement no-tracking scalar projection in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` (missing movie and null plot intentionally share null per brief Q3/Q6). Update existing test fakes/doubles implementing IMovieRepository as forced by the port addition; report unrelated pre-existing failures rather than silently fixing them.
- [X] T004 [US1] After T002, add `FindMovieIdsMissingFeaturesAsync(CancellationToken) -> Task<IReadOnlyList<MovieId>>` to `src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs` and implement database-side Movies/movie_features anti-join whose nonblank-plot eligibility matches the handler's canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics in `src/LamuFlix.Infrastructure/Persistence/EfMovieFeaturesRepository.cs`. Do not move normalization into `GetPlotAsync` (scalar projection still returns the stored value). Do not claim SQL TRIM/btrim equivalence; if exact parity cannot be expressed, return needs decision to Patron. Do not change existing UpsertAsync/schema/migration. Update affected fakes/doubles; report unrelated failures.
- [X] T005 [US1] Drive T001–T002 green on real PostgreSQL. No driver mocks.

**Checkpoint**: Phase 1 tests green. Commit `DEV-414 - plot and missing-features port seams`.

---

## Phase 2: Feature handler and outcome (US1–US2 unit scope)

**Purpose**: ProcessFeaturesCommand / Handler / Outcome with unit tests for success, no-ops, classification precedence, invalid vector, retry/exhaustion boundaries, cancellation, deterministic timestamps, and metadata-state isolation.

**Independent Test**: Unit tests with FakeEmbeddingGenerator, deterministic TimeProvider, and Core-port doubles cover the Q3/Q4 matrix without a broker.

**Checkpoint**: Handler outcomes are proven in unit tests. Consumer wiring is not required yet.

### Tests for Phase 2

- [X] T006 [P] [US1] Author unit tests in `tests/LamuFlix.UnitTests/Features/Enrichment/` for ProcessFeaturesCommandHandler matching the T002 parity corpus under canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics: nonblank/mixed-text success (one GenerateAsync, one UpsertAsync, ComputedAt from TimeProvider); missing/null and whitespace-only variants (empty, spaces-only, tab/newline-only, Unicode-whitespace-only) are no-ops (no generate, no upsert); existing-row path still generates and upserts; EnrichmentStatus/EnrichmentAttempts unchanged on doubles that can observe metadata calls (assert no MarkFailed / no enrichment mutation); structured logs under review exclude plot text and embedding vector contents while retaining MovieId/Attempt/safe category/no-input outcome and no raw exception text. Minimum compilable contracts when types are absent; behavioral failure before T008 when possible.
- [X] T007 [P] [US2] Author unit tests for classification and retry-boundary outcomes: EmbeddingGenerationException.Category precedence; EnrichmentFailureClassifier reuse for unmatched/persistence-shaped exceptions; invalid vector → permanent invalid_response; Attempt below 1 / invalid payload → permanent invalid_response; retryable below MaxAttempts vs exhausted; requested cancellation bypasses classification; never emits duplicate_imdb_id; no raw exception text in outcome/safe headers helpers if present at this layer.

### Implementation for Phase 2

- [X] T008 [US1] After T006, add `ProcessFeaturesCommand.cs`, `ProcessFeaturesOutcome.cs`, and `ProcessFeaturesCommandHandler.cs` under `src/LamuFlix.Core/Features/Enrichment/` per brief Q2–Q3/Q6 (feature-specific outcome; no metadata transition; whitespace no-op; TimeProvider ComputedAt; existing MovieFeaturesSnapshot validation path).
- [X] T009 [US2] After T007, wire classification/retry decision into the handler/outcome per brief Q4 continuation (Category first; classifier otherwise; whitelist four codes; MaxAttempts from EnrichmentOptions). Do not MarkFailed.
- [X] T010 [US1/US2] Drive T006–T007 green.

**Checkpoint**: Phase 2 tests green. Commit `DEV-414 - process features handler`.

---

## Phase 3: Consumer, routing, publisher overload, activation, liveness (US2–US4)

**Purpose**: FeaturesConsumer, FeaturesRouting, publisher retry/DLQ overload, feature activation predicate, feature liveness and features-consumer readiness. Live RabbitMQ tests for delivery, redelivery uniqueness, retry/DLQ, malformed/Attempt&lt;1, shutdown requeue, failed publish without ack, unroutable expectation.

**Independent Test**: Live-broker integration matrix from brief Q8 step 4.

**Checkpoint**: Consumer path persists features and dispositions retry/DLQ without claiming unverified broker-ack semantics beyond configured options and live tests.

### Tests for Phase 3

- [X] T011 [P] [US2] Author live RabbitMQ integration tests in `tests/LamuFlix.IntegrationTests/` for FeaturesConsumer/FeaturesRouting: successful persisted delivery; delayed Attempt+1 retry; permanent/exhausted application-published DLQ with sanitized headers only from whitelist (those headers constrain only application-published retry/DLQ messages; broker-generated dead-letter entries are outside that header/accounting contract); malformed/Attempt&lt;1 → invalid_response DLQ; shutdown requeue without Attempt increment on unchanged topology; live-broker repeated shutdown-requeue coverage that records actual queue residence, broker headers/body, and application Attempt without prescribing infinite requested-queue residence or inventing observed broker behavior (recorded broker behavior governs; any required topology change returns to Patron); failed publish without ack; unroutable PublishException expectation aligned with existing `RabbitMqPublisherTests` pattern (live broker required; failure blocks); within existing logging review/coverage, confirm feature consumer logs exclude plot text and embedding vector contents while retaining MovieId/Attempt/safe category/no-input and no raw exception text. Minimum compilable contracts when types are absent; behavioral failure before T014 when possible.
- [X] T012 [P] [US1] Author live-broker/DB test for repeated/concurrent MovieId redelivery leaving exactly one movie_features row (fake embedding; real upsert).
- [X] T013 [P] [US4] Author activation/liveness/readiness tests: feature-active composition registers FeaturesConsumer and features-consumer readiness on existing ready endpoint; incomplete host logs inactive and does not resolve missing dependencies; metadata consumer activation unchanged; metadata liveness does not mask feature disconnect. Extend related existing health-registration tests as forced.

### Implementation for Phase 3

- [X] T014 [US2] After T011, add `src/LamuFlix.Infrastructure/RabbitMq/FeaturesRouting.cs` and `FeaturesConsumer.cs` following EnrichmentConsumer pattern (ack after upsert/no-op or completed retry/DLQ publication; shutdown nack/requeue; sanitized headers; TimeProvider delays).
- [X] T015 [US2] Extend `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs` with typed FeaturesRequested retry/DLQ overload through existing PublishCoreAsync (Persistent, confirms+tracking, mandatory, awaited). Do not add a second publisher or assert unverified broker-ack/nack/return library guarantees in comments or tests beyond the live expectations above.
- [X] T016 [US4] Extend `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs` (and `src/LamuFlix.Api/Program.cs` as needed) with feature-specific active predicate requiring IMovieRepository, IMovieFeaturesRepository, IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt;, and queue publisher (independent of IMetadataProvider); register ProcessFeaturesCommandHandler (processing) and FeaturesConsumer only when active; do not register SweepMissingMovieFeaturesCommandHandler here and do not stub or blanket-defer recovery registration to Phase 5; dependency registration precedes evaluation; concrete feature liveness + features-consumer readiness health check on existing readiness endpoint.
- [X] T017 [US1/US2/US4] Drive T011–T013 green on live RabbitMQ (+ PostgreSQL where persistence is asserted).

**Checkpoint**: Phase 3 tests green. Commit `DEV-414 - features consumer retry and activation`.

---

## Phase 4: Independent recovery passes (US3)

**Purpose**: SweepMissingMovieFeaturesCommand/Handler and StrandedMovieSweeper independent guarded passes with real DB/broker integration per brief Q8 step 5.

**Independent Test**: Missing-row/nonblank selection, no enrichment-status filter, existing-row exclusion, empty enrichment pass still runs feature pass, isolated pass failure, fresh Attempt 1 after terminal failure, enqueue-failure eligibility.

**Checkpoint**: Feature recovery AC satisfied without changing enrichment Pending-plus-lease semantics.

### Tests for Phase 4

- [X] T018 [P] [US3] Author integration tests for SweepMissingMovieFeatures + StrandedMovieSweeper dual passes: selects only nonblank plot without movie_features; excludes existing features; does not use enrichment status predicate; feature pass runs when enrichment candidates are empty; failure in one pass does not suppress the other except shutdown; after prior feature DLQ, later sweep enqueues Attempt 1; enqueue failure leaves movie eligible next interval. Use real PostgreSQL/RabbitMQ. Minimum compilable contracts when types are absent; behavioral failure before T020 when possible.

### Implementation for Phase 4

- [X] T019 [US3] After T018, add `SweepMissingMovieFeaturesCommand.cs` and `SweepMissingMovieFeaturesCommandHandler.cs` under `src/LamuFlix.Core/Features/Enrichment/` (enqueue FeaturesRequested(MovieId, 1) only; no embed/claim/metadata mutation). Register SweepMissingMovieFeaturesCommandHandler under the same feature-active predicate in `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs` (and `src/LamuFlix.Api/Program.cs` as needed) before Phase 4 tests/checkpoint, preserving final active-only composition and Q8 order.
- [X] T020 [US3] Modify `src/LamuFlix.Infrastructure/Enrichment/StrandedMovieSweeper.cs` to invoke enrichment and feature sweep handlers in independent guarded passes on existing SweepInterval; run feature pass only in active feature composition (logged absence when inactive); preserve existing Pending-plus-lease enrichment pass unchanged.
- [X] T021 [US3] Drive T018 green.

**Checkpoint**: Phase 4 tests green. Commit `DEV-414 - missing features recovery sweep`.

---

## Phase 5: Host composition and end-to-end (US4)

**Purpose**: Active/inactive host composition polish and E2E enrichment → FeaturesRequested → movie_features with fake embedding, real DB/broker, same trace, typed generator, timestamp/dimension, one row, unchanged metadata state (brief Q8 step 6).

**Independent Test**: ApiHostComposition / end-to-end style proof covering the ticket AC path.

**Checkpoint**: Ticket end-to-end AC path is evidenced. Ready for Cog refactor.

### Tests for Phase 5

- [ ] T022 [P] [US4] Author/extend host composition and E2E tests in `tests/LamuFlix.IntegrationTests/`: active composition delivers enrichment → FeaturesRequested → movie_features; fake embedding; real DB/broker; same trace id across producer → consumer → upsert; vector dimension/timestamp; one row; EnrichmentStatus/EnrichmentAttempts unchanged; inactive/minimal host logs feature consumer inactive without resolving missing deps. Reuse existing host test patterns (ApiHostCompositionTests / ApiEndToEndImportTests family). Minimum compilable contracts when seams are absent; behavioral failure before T023 when possible.

### Implementation for Phase 5

- [ ] T023 [US4] Complete any remaining Program/DI/health wiring required for T022 and drive T022 green. Do not add public HTTP contracts. Do not introduce Ollama network dependency.

**Checkpoint**: Phase 5 tests green. Commit `DEV-414 - features consumer composition and e2e`.

---

## Phase 6: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–5 are committed, Cog refactors the whole change. Gauge then runs the configured gates.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass.

- [ ] T024 Refactor the completed change across the file set in `specs/DEV-414/plan.md` until cyclomatic complexity passes at the configured refactor threshold. Read `gates.complexity.refactor` from `harness.yml` and pass that configured value to the supported `-Threshold` parameter of `./scripts/run-cyclomatic-complexity.ps1`. Do not encode a task-owned numeric literal as configuration. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit harness/config/generated files or weaken thresholds.
- [ ] T025 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no arguments (normal changed-diff gate; does not replace T024 stricter refactor evidence), `./scripts/run-jetbrains-inspectcode.ps1` with no arguments, `./scripts/run-property-tests.ps1` (exit 2 accepted only if task note records `propertyTests: opt-out — <reason>`; otherwise exit 2 blocks), `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and worktree-safe `./scripts/run-mutation.ps1` without `--since` for changed eligible production projects. Run web gates; unchanged `/web` is scope-empty `SKIPPED`, not PASS. No threshold edits. Apply Q1 verdict handling exactly: exit 1 / Could not run / unaccepted property-test exit 2 block; scope-empty SKIPPED, mutation NOT APPLICABLE, harness SKIP, recorded property opt-out never PASS.

---

## Excluded (never scheduled)

UI / `design.md`, new schema/migration, new NuGet/npm dependency, new project/layer, public HTTP/OpenAPI/web contracts, generic transport framework, second publisher, model/version/hash persistence, lifetime retry suppression, recommendation algorithm, LocalPlay, secrets, Process.Start, historical DEV-371 rewrite, inventing ADR body text or claiming deferred ADR complete, asserting unverified BasicPublishAsync broker-ack/nack/return or exactly-once durability, lowering gate thresholds, mutation `--since`, and unrelated refactors. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Depends on Spec workspace handoff. TB000–TB004 must have recorded evidence before T001. Missing recorded Epic 7 completion receipt, Gate 1, drift recon, Gauge baseline, or Keel drift check blocks code.
- **Phase 1**: Depends on Phase B entry. Ports/EF/query proofs. Author T001/T002 first; T003/T004; drive T005 green before phase commit.
- **Phase 2**: Depends on Phase 1. Handler unit matrix. Author T006/T007 first; T008/T009; drive T010 green.
- **Phase 3**: Depends on Phase 2. Consumer/routing/publisher/activation. Author T011–T013 first; T014–T016; drive T017 green.
- **Phase 4**: Depends on Phase 1 (anti-join) and Phase 3 (enqueue/publisher). Author T018 first; T019/T020; drive T021 green.
- **Phase 5**: Depends on Phases 3–4. Author T022; complete T023 green.
- **Phase 6**: Depends on Phases 1–5 being committed. Cog owns T024. Gauge owns T025.

### User Story Dependencies

- **US1 (P1)**: Starts in Phase 1 (ports); handler success/no-op in Phase 2; redelivery uniqueness with consumer in Phase 3.
- **US2 (P1)**: Classification/retry unit scope in Phase 2; live retry/DLQ/publication in Phase 3.
- **US3 (P1)**: Recovery in Phase 4 (needs Phase 1 anti-join and Phase 3 publisher/enqueue).
- **US4 (P1)**: Activation/liveness in Phase 3; composition/E2E in Phase 5.

### Within Each User Story

- Author the behavior test before the production edit it locks. When types are absent, add minimum compilable contracts first; demonstrate behavioral failure before the behavior edit when possible. Missing-type compile failures are not behavioral red tests.
- Phase tests green before that phase's commit.
- Anvil one production phase per ask.

### Parallel Opportunities

- TB000–TB004 are intake evidence gates; they complete before any Code Phase 1 task.
- T001 and T002 test authoring in parallel (different concerns/files).
- T006 and T007 test authoring in parallel.
- T011, T012, and T013 test authoring in parallel when files differ.
- Single Anvil ownership for Phases 1–5; parallel markers never split two writers onto the same consumer or shared test file.

## Parallel Example: Phase 1 tests

```text
Task: "T001 Author GetPlotAsync integration tests"
Task: "T002 Author FindMovieIdsMissingFeaturesAsync integration tests"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 port/query seams.
2. Finish Phase 2 handler success/no-op.
3. Stop there only to validate US1 handler path. Frozen scope still requires US2–US4 before review.

### Incremental Delivery

US2 lands with Phase 3 live retry/DLQ. US3 lands with Phase 4 sweeper passes. US4 completes with Phase 5 E2E. Phase 6 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. Do not split two writers onto the same consumer, publisher, or shared test file.

## Notes

- [P] tasks use different files and do not depend on an unfinished task or shared test/minimal-contract file still being written by another unfinished task.
- Story labels map to `spec.md`.
- Commit after each phase, not after every task, using `DEV-414 - {phase subject}`.
- Gate 1 comes from the merged spec PR. Phase B entry (TB000–TB004), including the recorded Epic 7 completion receipt, must also be evidenced before T001. This task list does not authorize implementation before that.
- Additive CONTEXT.md vocabulary is authored in the Spec PR by Quill (FR-016); it is not a Phase B Anvil task unless drift forces a rebuild edit.
- New unanticipated files or decisions return to Keel; do not invent requirements.
- Spec workspace does not run gates, tests, or analyzers for this ask.
