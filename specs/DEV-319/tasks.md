---

description: "Task list for DEV-319 TelemetryConstants, named spans, custom metrics, ADR-0008"
---

# Tasks: DEV-319 TelemetryConstants, named spans, custom metrics, ADR-0008

**Input**: Design documents from `/specs/DEV-319/` (spec.md, plan.md, frozen brief.md, Patron CONCLUSIONS.md; facts from recon-DEV-319)

**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Tests**: Test tasks are included because AC1 requires measured proof of instrument names and dimensions; every code phase pairs with its tests.

**Organization**: Tasks are grouped by implementation phase per the frozen brief's ordered task constraints; each phase has explicit files, a checkpoint, and a stop boundary. No implementation before the user merges the spec PR at Gate 1. Quill drafts only.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Paths below are concrete repo paths (`src/...`, `tests/...`, `docs/...`).

## Phase 1: Constants, instrument holder, contract-test harness (US1, US2, US3)

**Purpose**: Ruled literal constants, the static BCL instrument holder, and the isolated contract tests that prove literal names, types, unit, and tag sets.

- [ ] T001 [US1/US2/US3] Add ruled literal constants to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`: `EnrichmentDurationMetricName`, `EnrichmentOutcomeMetricName`, `ImportCountMetricName`, `ProviderTagName`, `OutcomeTagName`, `FailureCategoryTagName`, `EnrichedOutcome`, `NotFoundOutcome`, `FailedOutcome`, `OmdbProviderName`; string catalogue only, no instrument types.
- [ ] T002 [US1/US2/US3] Create `src/LamuFlix.Core/Pipeline/EnrichmentMetrics.cs` as a concrete static BCL holder with one process-lifetime static `Meter` named `TelemetryConstants.ActivitySourceName` holding exactly the duration histogram and two counters; expose concrete instruments for the Core handlers and Infrastructure adapter; no interface, DI, factory, OpenTelemetry type, or new package.
- [ ] T003 [P] [US1/US2/US3] Create `tests/LamuFlix.UnitTests/Pipeline/MetricsCollection.cs` with `DisableParallelization=true` and apply it to all unit tests observing application instruments; state fixture ownership/lifecycle in this approved file, retain one collection assignment and isolation for all application-meter observers.
- [ ] T004 [US1/US2/US3] Create `tests/LamuFlix.UnitTests/Pipeline/EnrichmentMetricsTests.cs` with instrument contract tests (literal names, `Histogram<double>` vs `Counter<long>`, duration unit `s`, exact tag sets) and a disposable `MeterListener` capture helper local to the unit-test project, filtered to exact application meter/instrument names with thread-safe measurement/tag capture, observing already-published static instruments as well as initial publication; helper stays nested in the relevant test file with no new shared helper; assert ticket literals independently, never constant-equals-itself.
- [ ] T005 (Wisp, read-only) Confirm `docs/adr/ADR-0008.md` exists with status Accepted (drafted in Phase A); no endpoint, hub, package, interval, or future commitment in it.

**Checkpoint**: Constants compile, holder publishes three instruments on the `LamuFlix` meter, contract tests observe them. **Stop boundary for Anvil; read-only verification tasks belong to Wisp.**

---

## Phase 2: Import emission and tests (US3) 🎯 MVP increment

**Goal**: `lamuflix.import.count` reflects persisted movies.

**Independent Test**: `ImportMovieFolderCommandHandlerTests` with `MeterListener`: success records one untagged measurement; scan/add/save/duplicate failures record zero; enqueue failure after save records one.

- [ ] T006 [US3] Write the failing import metric tests in `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` (success, scan-failure, add-failure, save-failure, real duplicate-import with real persistence proving uniqueness and zero import count, queue-failure-after-save) and apply the Phase 1 isolation collection; reuse the existing Tests.Common `PostgresFixture` migration/reset/context lifecycle with a fixture-backed `EfMovieRepository` in this approved file (`ContainerFixture` is a static manually managed helper, not an xUnit fixture); duplicate case: first successful handler import, then a second import with a fresh context/repository against the same database and same `LibraryPath` but a distinct identity so the real database unique index rejects `SaveChangesAsync`, verifying one persisted movie and no second enqueue; capture the first import as one untagged measurement, take a nested disposable exact-meter/instrument filtered thread-safe `MeterListener` baseline (observing existing and new instruments) before the rejected import, and assert zero additional import measurements; keep separate scan/add/save/queue-failure assertions; never mock the database driver or uniqueness.
- [ ] T007 [US3] Implement `Add(1)` in `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs` immediately after successful `SaveChangesAsync` and before `EnqueueAsync`, preserving exception, order, and API behavior (depends on T006).

**Checkpoint**: US3 fully functional and testable independently: success counts one, pre-save failures count zero, post-save enqueue failure counts one. **Stop boundary for Anvil.**

---

## Phase 3: Handler outcome emission and tests (US1)

**Goal**: `lamuflix.enrichment.outcome` counts lookup attempts with frozen dimensions.

**Independent Test**: New focused handler tests plus real Postgres/RabbitMQ integration coverage for refused claims, retries, and cancellation.

- [ ] T008 [US1] Create `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentMetricsTests.cs` with failing tests for Found, NotFound, every failure category (`provider_unavailable`, `rate_limited`, `invalid_response`, `unknown`), lookup exceptions, repeated retry attempts, refused claims (no measurement), requested cancellation (no measurement), and no extra outcome on persistence/settlement failure; capture with a disposable `MeterListener` filtered to exact application meter/instrument names with thread-safe measurement/tag capture, observing already-published static instruments as well as initial publication, helper nested in this test file with no new shared helper; apply the Phase 1 isolation collection (depends on Phase 2 completion).
- [ ] T009 [US1] Emit lookup outcomes in `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs` without altering domain behavior or the `ProcessEnrichmentOutcome` contract: Found `Add(1)` with `outcome=enriched`; NotFound `Add(1)` with `outcome=not_found`; provider Failed or classified exception `Add(1)` with `outcome=failed` plus `failure_category` set to the actual `Category.Code`; record once at classification, never through both result and exception paths (depends on T008).
- [ ] T010 [US1] Extend `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` with actual lookup/retry/refused-claim/cancellation metric coverage using existing real Postgres/RabbitMQ behavior; preserve the existing RabbitMq collection and all span/attribute/parent/link assertions (depends on T009).

**Checkpoint**: US1 fully functional and testable independently: one measurement per lookup attempt including retries, silence on no-op claims and cancellation, closed category set on failures only. **Stop boundary for Anvil.**

---

## Phase 4: Provider duration and controlled-time tests (US2)

**Goal**: `lamuflix.enrichment.duration` times whole lookups in seconds.

**Independent Test**: Controlled-`TimeProvider` tests proving one `s`-unit measurement with only `provider=omdb` for success, failure, and cancellation.

- [ ] T011 [US2] Write the failing duration tests in `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` and `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` (Found, Failed, cancelled; no wall-clock sleeps) using a test-only BCL `TimeProvider` subclass nested in those named files with explicitly controlled `GetTimestamp` and a matching known `TimestampFrequency` (plus deterministic `GetUtcNow` where mapping needs it), supplied to the existing provider constructor in a focused setup local to those files with no probe/shared-helper/package/file-map change; advance timestamps explicitly at an awaited transport/test boundary for each of the three paths and assert the exact elapsed seconds value with exactly one duration measurement carrying only `provider=omdb`; capture with a disposable `MeterListener` filtered to exact application meter/instrument names with thread-safe measurement/tag capture, observing already-published static instruments as well as initial publication, helpers nested in the relevant test files with no new shared helper; preserve the MetadataProvider nonparallel collection; no consumer production edit (depends on Phase 3 completion).
- [ ] T012 [US2] Record whole-lookup duration in `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` using its injected `TimeProvider` (`GetTimestamp`/`GetElapsedTime`), elapsed `TotalSeconds` recorded exactly once in `finally`, including failed and cancelled calls; any necessary BCL constructor wiring is an AC1-forced edit; never derive provider identity from URL, credentials, or class full name (depends on T011).
- [ ] T013 [US2] Extend `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` and `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` with duration and trace verification as a dependent continuation of T011/T012 (no separate parallel duration-test claim), reusing the T011 nested BCL `TimeProvider` and local provider injection with explicit awaited-boundary advancement, exact elapsed seconds, and exactly-once Found/Failed/cancelled assertions; preserve the MetadataProvider nonparallel collection and existing span assertions (depends on T012).

**Checkpoint**: US2 fully functional and testable independently: duration emitted even when outcome is absent (cancellation). **Stop boundary for Anvil.**

---

## Phase 5: Composition proof and integration coverage (US4)

**Goal**: Actual instruments export through the existing host composition; spans and architecture proofs retained.

**Independent Test**: Extended host composition proof plus real integration behavior; existing assertions unchanged.

- [ ] T014 [US4] Extend `tests/LamuFlix.IntegrationTests/ServiceDefaultsTelemetryTests.cs` to receive actual instrument identities and their dimensions through the existing host composition and nonparallel collection, rather than merely emitting `telemetry.composition.probe` (depends on Phase 4 completion).
- [ ] T015 (Wisp, read-only) [US4] Retain existing architecture/trace proofs with no production/test architecture edits: named-span, kind, attribute, and parent/link assertions; Core no-OpenTelemetry boundary.

**Checkpoint**: AC1 proven end to end: literal names, types, unit, and dimensions observable through the composed host; no span or architecture regression. **Stop boundary for Anvil.**

---

## Phase 6: Pipeline gates (final phase)

**Purpose**: Run the full Phase B gate set in its own final phase with thresholds unchanged.

- [ ] T016 (Gauge) Run every applicable task-pipeline gate: Roslyn analyzers, cyclomatic complexity, InspectCode (no `-All`), mutation for changed production C# (never Stryker `--since` in a worktree), property tests (opt-out recorded; existing property tests still run; opt-out waives no applicable failure or mutation testing), vulnerable packages, format verification, `dotnet test`, and web applicability.
- [ ] T017 (Gauge) Record each gate verdict distinctly: PASS only on actual script success; SKIPPED (scope-empty), configured-disabled SKIP, mutation N/A, accepted property opt-out, and Could not run kept distinct; scope-empty never PASS; exit 1 and Could not run block.

**Checkpoint**: All gates adjudicated; review proceeds on the three mandatory axes for this M ticket. Owner merges all PRs; completion stops at awaiting-merge.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependencies - can start immediately after Gate 1 merge (constants/holder first; ADR already drafted in Phase A).
- **Phase 2 (US3)**: Depends on Phase 1 completion.
- **Phase 3 (US1)**: Depends on Phase 2 completion (ordered import, handler outcome, provider duration, composition, final gates per brief.md:94-103).
- **Phase 4 (US2)**: Depends on Phase 3 completion (ordered per brief.md:94-103).
- **Phase 5**: Depends on Phase 4 completion (ordered per brief.md:94-103).
- **Phase 6 (gates, Gauge)**: Depends on Phase 5 completion; gates run in their own final phase only.

### Within Each Phase

- Tests MUST be written and FAIL before implementation.
- No `TelemetryConstants` member may duplicate an existing spelling: `enrichment.outcome` (span key) and `lamuflix.handler.outcome` stay untouched.
- Commit after each task or logical group; at most two fix commits per remediation round, at most two review rounds.

### Parallel Opportunities

- T003 can run in parallel with T001/T002 (different files).
- T006, T008, T011 run in dependency order (Phase 2, then Phase 3, then Phase 4); no parallel work across phases.
- T013 is a dependent continuation of T011/T012, not a parallel duration-test claim.
- T015 is a read-only verification task owned by Wisp; T005 is likewise Wisp read-only; T016/T017 final gates are owned by Gauge.

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: constants, holder, contract harness.
2. Complete Phase 2: import counter (smallest independent increment).
3. **STOP and VALIDATE**: Test US3 independently.

### Incremental Delivery

1. Phase 1 → contract proof ready.
2. Add US3 → Test independently → checkpoint.
3. Add US1 → Test independently → checkpoint.
4. Add US2 → Test independently → checkpoint.
5. Phase 5 composition → Phase 6 gates.

---

## Notes

- [P] tasks = different files, no dependencies.
- Frozen scope only: three metrics and literal constants, AC1 emission/tests, span/attribute preservation and verification, Accepted ADR-0008. Anything else is a follow-up issue, not a finding in this round.
- Grill cap 12 (4 used); review cap two rounds; remediation cap two fix commits per round.
- Keel subsequently runs read-only speckit-analyze against the brief, followed by three-axis plan challenge and adjudication/freeze. This task list is not plan-challenge clearance or Gate 1 approval.
