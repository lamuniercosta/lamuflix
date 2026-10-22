# Tasks: DEV-412 OllamaSharp embedding adapter

**Input**: Design documents from `specs/DEV-412/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why). Code Phases 1–3 also require the Phase B entry checklist below (brief task order step 3). Missing Phase B evidence blocks T001 and later code work.

**Tests**: Included. Brief test strategy and FR-014 require them. Author the behavior test before the production edit it locks. When the types under test do not yet exist, add only the minimum compilable contracts needed so those tests compile; do not treat missing-type compile failures as behavioral red tests. When types already exist and the locked behavior is not already implemented, demonstrate behavioral failure before the behavior edit when possible. Drive each phase's tests green before that phase's commit. This is not a mandatory TDD policy beyond the brief. Use existing xUnit, assertion libraries, LoopbackWireMock, and FsCheck. Application `TimeProvider` plus existing `FixedTimeProvider`, shortened test-only resilience settings, and bounded real WireMock/backoff delay measurements per DEV-303 (recon-DEV-412-3 §15.2); no new timing package or arbitrary sleeps; do not assert that the DI clock alone skips Polly delays. No live Ollama. No new test dependency. Single Anvil ownership for Phases 1–3; retain author-first / minimum-compilable-contract instructions.

**Organization**: Phases follow brief task order (Code Phases 1–3). Anvil implements Phases 1–3, one phase per ask, and stops after Phase 3. Phase 4 is Cog, then Gauge. Do not start Phase 1 until the Phase B entry checklist is satisfied (merged spec/Gate 1 plus the other brief entry prerequisites). Conductor consolidates TB002/TB003 owner/Rigger prerequisite identities and Epic 7 evidence references into `DEV-412`; checklist marks must reference that receipt (no checklist-only completion, duplicated prerequisite, or new prerequisite).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 generate vectors, US2 startup validation, US3 typed resilience failures, US4 health/telemetry.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Schedule the brief's Build workspace entry checks. Do not run gates, tests, or analyzers from this Spec workspace, and do not assert Epic 7 is complete here. Missing evidence blocks T001.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [ ] TB001 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt.
- [ ] TB002 Conductor consolidates owner/Rigger evidence of merged-spec and other ticket prerequisites required for build intake into `DEV-412`, including prerequisite identities. Checklist mark must reference that Conductor receipt; checklist-only completion is insufficient. No duplicated or new prerequisite.
- [ ] TB003 Conductor consolidates owner/Rigger PRODUCT Epic 7 sequencing prerequisite evidence references into `DEV-412` for this build intake. Schedule the check only; do not claim Epic 7 complete from Spec artifacts alone. Checklist mark must reference that Conductor receipt; checklist-only completion is insufficient. No duplicated or new prerequisite.
- [ ] TB004 Obtain Phase B recon drift against `origin/main` for the authorized file set (Wisp → `recon-DEV-412`). Refresh the recon-DEV-412-3 §15.5 factory inventory and name the actual files requiring generator/probe isolation before T020 (`ApiHostFactory.cs`, `ApiEndToEndFactory.cs`, nested `ReadyApiHostFactory` in `HealthEndpointTests.cs`, and any still-necessary per-test builders from that inventory). Existing factory-isolation authorization applies only where needed; no wholesale inventory edits or newly invented file authority.
- [ ] TB005 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef` mode) recorded in `DEV-412`.
- [ ] TB006 Keel checks or amends drift against the brief/spec before any Code Phase 1 edit. Missing Keel drift check blocks T001.

**Checkpoint**: Phase B entry evidence is complete. Only then may Anvil start T001.

---

## Phase 1: Packages, options, and typed exception (US1 foundational)

**Purpose**: CPM pins, options contracts, and `EmbeddingGenerationException`. Blocks Phases 2–3. Does not register production startup in the API host.

**Independent Test**: Unit and property tests for options ranges/cross-field rules and the exception/category contract compile and pass without Ollama or host composition.

**Checkpoint**: Pins and Infrastructure package references exist. Options validate. Exception carries only the three allowed existing categories. API host is unchanged.

- [X] T001 [P] [US1] Add `<PackageVersion Include="OllamaSharp" Version="5.5.0" />` and `<PackageVersion Include="Microsoft.Extensions.AI" Version="10.11.0" />` to `Directory.Packages.props`, retaining existing `Microsoft.Extensions.AI.Abstractions` `10.11.0`. Do not add `Microsoft.Extensions.AI.Ollama`.
- [X] T002 [P] [US1] Add direct unversioned `<PackageReference Include="OllamaSharp" />` and `<PackageReference Include="Microsoft.Extensions.AI" />` to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`.
- [X] T003 [P] [US1] Author unit and FsCheck property tests in `tests/LamuFlix.UnitTests/Options/OllamaOptionsTests.cs` for nonempty absolute http/https `Endpoint`, nonblank `ModelName`, record-initializer defaults `http://localhost:11434/` and `all-minilm`, and rejection of invalid URI/blank model. If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T005 when possible. Do not treat missing-type compile errors as the red proof.
- [X] T004 [P] [US1] Author unit and FsCheck property tests in `tests/LamuFlix.UnitTests/Options/OllamaResilienceOptionsTests.cs` covering mirrored DEV-303 defaults on the options record, ranges, `AttemptTimeout <= TotalTimeout`, and `SamplingDuration >= 2 * AttemptTimeout`. If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T006 when possible. Do not treat missing-type compile errors as the red proof.
- [X] T005 [US1] After T003 is authored (and behavioral failure shown when possible), add `OllamaOptions` (`SectionName = "Ollama"`, `Endpoint`, `ModelName`) in `src/LamuFlix.Core/Options/OllamaOptions.cs` with record initializers carrying the already approved endpoint/model defaults and validation matching the brief.
- [X] T006 [US1] After T004 is authored (and behavioral failure shown when possible), add `OllamaResilienceOptions` (`SectionName = "Ollama:Resilience"`) in `src/LamuFlix.Core/Options/OllamaResilienceOptions.cs` with record initializers carrying the already approved resilience defaults, ranges, and cross-field `IValidatableObject` rules without editing OMDb options.
- [X] T007 [P] [US1] Author tests in `tests/LamuFlix.UnitTests/EmbeddingGenerationExceptionTests.cs` for carrying `EnrichmentFailureCategory` values `ProviderUnavailable`, `RateLimited`, and `InvalidResponse` only. If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T008 when possible. Do not treat missing-type compile errors as the red proof.
- [X] T008 [US1] After T007 is authored (and behavioral failure shown when possible), add `EmbeddingGenerationException` in `src/LamuFlix.Core/Ports/EmbeddingGenerationException.cs` that carries existing `EnrichmentFailureCategory` and does not introduce a new category type or C# enum.

**Checkpoint**: Phase 1 tests green. Commit `DEV-412 - packages options and embedding exception`.

---

## Phase 2: Adapter, resilience, startup, health, and telemetry (US1–US4)

**Purpose**: Production adapter behind the port with resilient typed HttpClient, verified-state singleton, hosted startup probe, passive health, and functional telemetry. Depends on Phase 1. Do not wire ServiceDefaults/API composition yet (Phase 3), so unrelated hosts are not forced to need Ollama mid-phase.

**Independent Test**: WireMock generator/resilience/telemetry tests construct the adapter and DI pieces under test (or a focused service collection) without relying on full Api `Program.cs` production registration landing in this phase.

**Checkpoint**: Adapter returns 384-vectors; metadata and argument guards hold; resilience maps typed failures; startup probe and health/telemetry behaviors are proven against WireMock/listeners.

### Tests for Phase 2

- [X] T009 [P] [US1] Author WireMock example tests and FsCheck properties in `tests/LamuFlix.IntegrationTests/OllamaEmbeddingGeneratorTests.cs` for request shape (configured model, ordered unmodified inputs, wire `dimensions` absent for caller `Dimensions` null and `Dimensions` 384; do not forward caller `Dimensions` to the inner client; do not mutate caller options; no blanket `AdditionalProperties` ban/allowlist), single/batch success, empty/invalid calls with zero HTTP, metadata before/after verification across transient instances, keyed/unknown `GetService`, disposal ownership, dimension/count/null/empty/nonfinite/malformed failures, caller model/dimension guards, and sanitized diagnostics (safe endpoint identifier excluding URI user-info/query/fragment and unsafe path data; no raw URI/exception/body-bearing inner chains; preserve sanitized model/category/dimension diagnostics) using synthetic credential/query/body values in assertions. FsCheck properties MUST use the existing IntegrationTests `[Fact]` + `[Trait("Category", "Property")]` / `Prop.ForAll` pattern (recon-DEV-412-3 §15.3) and cover finite-vector dimension/count/order invariants over varied finite vectors and batches (preserve count and order) plus rejection when dimensions or count violate the contract. Use existing LoopbackWireMock and FsCheck only; no new test dependency. If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T013–T015 when possible. Do not treat missing-type compile errors as the red proof. Do not schedule parallel writes into this shared file from T011.
- [X] T010 [P] [US3] Author WireMock tests in `tests/LamuFlix.IntegrationTests/OllamaEmbeddingResilienceTests.cs` for transient-then-success exact attempts; exhausted 429 → `RateLimited`; `TimeoutRejectedException` → `ProviderUnavailable`; exhausted 408/5xx/network/attempt/total timeout and broken circuit → `ProviderUnavailable`; timeout without caller cancellation → `ProviderUnavailable`; `OperationCanceledException` with caller token cancelled → cancellation (including during send/backoff); otherwise timeout cancellation → `ProviderUnavailable`; permanent errors with no retry; Retry-After bounded by total timeout. Preserve Q5; do not invent a late-cancellation race precedence rule. Use application `TimeProvider` plus existing `FixedTimeProvider`, shortened test-only resilience settings, and bounded real WireMock/backoff delay measurements per DEV-303 (recon-DEV-412-3 §15.2). No new timing package or arbitrary sleeps; do not assert that the DI clock alone skips Polly delays. If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T013–T014 when possible. Do not treat missing-type compile errors as the red proof.
- [X] T011 [US2] After T009 (shared generator/composition test files and any shared minimal contracts), author focused startup/state tests (in `OllamaEmbeddingGeneratorTests.cs` or the composition file only if it can compile without Api wiring) that prove awaited `IHostedService.StartAsync` verification publishes verified 384 and calls `EmbeddingDimensions.Validate`, that delayed verification cannot complete host startup early (not a background-only `ExecuteAsync` probe), and that wrong dimensions/missing model/unavailable/malformed responses fail the validator clearly with sanitized diagnostics (safe endpoint identifier; synthetic credential/query/body values; never plot/raw URI/body-bearing chains). If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T015–T016 when possible. Do not treat missing-type compile errors as the red proof.
- [X] T012 [P] [US4] Author tests in `tests/LamuFlix.IntegrationTests/OllamaEmbeddingTelemetryTests.cs` (and health cases in the same file or generator tests only when not conflicting with T009/T011 shared-file ownership) for passive health Unhealthy/Healthy/Degraded with zero HTTP on health checks, and Activity/Meter listeners proving one logical `Embedding.Generate` span/duration/outcome across retries including the startup probe (same per-outbound logical telemetry rule; one logical count including retries), local rejects omitted, and low-cardinality outcome/category-only tags only (existing TelemetryConstants keys; no new tag taxonomy). If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T017–T018 when possible. Do not treat missing-type compile errors as the red proof.

### Implementation for Phase 2

- [X] T013 [US1] After T009 is authored (and behavioral failure shown when possible), implement `OllamaSharpEmbeddingGenerator` in `src/LamuFlix.Infrastructure/Adapters/OllamaSharpEmbeddingGenerator.cs` wrapping `OllamaApiClient` with injected `HttpClient` and configured model; enforce argument guards; omit wire `dimensions` for caller `Dimensions` null and 384; do not forward caller `Dimensions` to the inner client; do not mutate caller options; no blanket `AdditionalProperties` ban/allowlist; runtime 384/finite/count validation; sanitized failures (safe endpoint identifier; no raw URI/body-bearing chains); and `GetService` metadata rules. Add approved partials in the same folder if needed for complexity or LoggerMessage.
- [X] T014 [US1/US3] After T009 and T010 are authored (and behavioral failure shown when possible), implement `AddOllamaEmbeddingGenerator` in `OllamaEmbeddingServiceCollectionExtensions` (`src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingServiceCollectionExtensions.cs`) registering: singleton `OllamaEmbeddingModelState`; transient typed `AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, OllamaSharpEmbeddingGenerator>` with one `AddStandardResilienceHandler` configured from `OllamaResilienceOptions` (DEV-303 parity) and `HttpClient.Timeout = Timeout.InfiniteTimeSpan`; hosted validator `OllamaEmbeddingStartupValidator` as `IHostedService` with awaited `StartAsync` verification (not background-only `ExecuteAsync`); untagged health check registration ownership as completed in T016; and `TryAddSingleton(TimeProvider.System)` by the OMDb pattern. Exercise focused production registration of the extension. Do not call this from `Program.cs` yet (Phase 3).
- [X] T015 [US1/US2] After T009 and T011 are authored (and behavioral failure shown when possible), add `OllamaEmbeddingModelState` in `src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingModelState.cs` (DI singleton registered from `AddOllamaEmbeddingGenerator`, no captured HttpClient, no mutable static init; coherent thread-safe publication/reads/updates of verified configuration/dimensions/health across transient clients; focused state tests with controlled overlapping mixed outcomes yielding legal snapshots and a later valid success restoring Healthy; do not prescribe locks, priority, or a fixed winner for truly simultaneous completions) and `OllamaEmbeddingStartupValidator` in `src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingStartupValidator.cs` (hosted validator registered from `AddOllamaEmbeddingGenerator`; awaited `StartAsync` short-lived scope, fixed non-secret probe, publish verified 384, then `EmbeddingDimensions.Validate`, fail closed with sanitized diagnostics; delayed verification cannot complete host startup early).
- [X] T016 [US2/US4] After T011 and T012 are authored (and behavioral failure shown when possible), add `OllamaEmbeddingHealthCheck` in `src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingHealthCheck.cs` reading only model state (Unhealthy/Healthy/Degraded, never HTTP) and register it untagged as `ollama-embeddings` from `AddOllamaEmbeddingGenerator` in the service-collection extension (completing the registration ownership allocated in T014).
- [X] T017 [US4] After T012 is authored (and behavioral failure shown when possible), add functional constants to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` for span `Embedding.Generate`, histogram `lamuflix.embedding.duration`, and counter `lamuflix.embedding.outcome` (reuse existing outcome/failure_category tag names where applicable). No vendor-named Core telemetry type and no new tag taxonomy.
- [X] T018 [US4] After T012 and T017, record one logical outbound generate span/duration/outcome (including retries; startup probe included in the same per-outbound logical telemetry rule) with injected `TimeProvider` and low-cardinality outcome/category-only tags inside `OllamaSharpEmbeddingGenerator`, skipping empty/caller-invalid local rejects. Update health state on success and typed provider failure.

**Checkpoint**: Phase 2 tests green without Api production registration. Commit `DEV-412 - OllamaSharp embedding adapter and observability`.

---

## Phase 3: Composition, defaults, and test-host isolation (US1–US2)

**Purpose**: Bind options, default config, Api registration, and forced factory isolation. Depends on Phase 2.

**Independent Test**: Dedicated composition tests boot production registration against WireMock (384 pass, 768 fail, unavailable/missing model fail, invalid options before network). Unrelated `WebApplicationFactory` hosts stay green via test-only substitution.

**Checkpoint**: Production host wires the adapter. Unrelated factories do not require live Ollama. No production bypass flag.

- [X] T019 [P] [US2] Author composition tests in `tests/LamuFlix.IntegrationTests/OllamaEmbeddingCompositionTests.cs` for real production registration: startup 384 pass; 768 fail; unavailable or missing model fail; a delayed WireMock first-load case that exhausts the same configured resilience budget fails closed as `ProviderUnavailable` (no separate startup allowance, pull/warm, or cold-versus-outage diagnosis); delayed verification cannot complete host startup early (awaited `StartAsync`, not background-only `ExecuteAsync`); invalid URI/model/resilience options before network; focused `AddLamuFlixOptions` startup-validation with no `Ollama` section and no provider registration/network (record defaults succeed) plus invalid explicit values failing; unrelated factory isolation regressions as needed. Preserve ServiceDefaults binding; no Worker host or Api-only binding redesign. If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T022–T024 when possible. Do not treat missing-type compile errors as the red proof.
- [X] T020 [US2] After TB004 names the actual necessary isolation files from the refreshed recon-DEV-412-3 §15.5 inventory, extend only those existing API integration test factories/builders as needed — citing `ApiHostFactory.cs`, `ApiEndToEndFactory.cs`, nested `ReadyApiHostFactory` in `HealthEndpointTests.cs`, and any still-necessary per-test builders named by that refresh — with test-only substitution of the generator and/or startup probe so unrelated hosts need no Ollama. Existing factory-isolation authorization applies only where needed; no wholesale inventory edits or newly invented file authority. Author or adjust isolation assertions first where they can compile; demonstrate behavioral failure when possible. No production bypass flag. Do not mark parallel with unfinished TB004 or shared factory edits overlapping T023.
- [X] T021 [P] [US1] Add default `Ollama` endpoint/model (and resilience section defaults if required by binding precedent) to `src/LamuFlix.Api/appsettings.json` (`http://localhost:11434/`, `all-minilm`) without secrets. Options records already carry the same approved defaults (T005/T006); appsettings complements binding and does not replace record defaults.
- [X] T022 [US1/US2] After T019 is authored (and behavioral failure shown when possible), bind `OllamaOptions` and `OllamaResilienceOptions` with `ValidateOnStart` in `src/LamuFlix.ServiceDefaults/Extensions.cs` via the existing `BindAndValidate` pattern. Preserve ServiceDefaults binding; no Worker host or Api-only binding redesign. The no-`Ollama`-section defaults proof and invalid-explicit-values failure remain covered by T019.
- [X] T023 [US1/US2] After T019 and T020, call `AddOllamaEmbeddingGenerator` from `src/LamuFlix.Api/Program.cs` so production awaited `StartAsync` verification runs before readiness, with no concurrent-start / background-only `ExecuteAsync` bypass.
- [X] T024 [US1/US2] Drive T019–T020 to green against WireMock production registration and confirm unrelated factory hosts remain green without live Ollama. Add an approved WireMock probe helper in the existing integration-test folder only if needed.

**Checkpoint**: Phase 3 tests green. Commit `DEV-412 - compose Ollama embedding generator`.

---

## Phase 4: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–3 are committed, Cog refactors the whole change. Gauge then runs the configured gates.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass.

- [ ] T025 Refactor the completed change across the file set in `specs/DEV-412/plan.md` until cyclomatic complexity passes at the configured refactor threshold. Read `gates.complexity.refactor` from `harness.yml` and pass that configured value to the supported `-Threshold` parameter of `./scripts/run-cyclomatic-complexity.ps1` (recon-DEV-412-3 §15.6). Do not encode a task-owned numeric literal as configuration. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit harness/config/generated files or weaken thresholds. Preserve harness/constitution thresholds.
- [ ] T026 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no arguments (normal changed-diff gate; does not replace T025 stricter refactor evidence; recon-DEV-412-3 §15.6), `./scripts/run-jetbrains-inspectcode.ps1` with no arguments (changed diff, not `-All`), `./scripts/run-property-tests.ps1` (default all-test-project discovery with `Category=Property` per recon-DEV-412-3 §15.3; require execution evidence specifically covering the new vector/options properties from T009/T003/T004, not only unrelated global pass counts; for this no-opt-out property-bearing change, disabled/all-skipped/no-matching-property exit 2 does not satisfy required evidence and must not be relabeled PASS or invented as an opt-out), `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and worktree-safe `./scripts/run-mutation.ps1` without `--since` for changed eligible production projects. Run web gates; unchanged `/web` is scope-empty `SKIPPED`, not PASS. No threshold edits and no new opt-out. A mandatory failure, `Could not run`, or an unaccepted property-test exit 2 blocks.

---

## Excluded (never scheduled)

UI / `design.md`, real-model acceptance (DEV-349), model install/pull/fallback, dimension reduction or dimensions override, database/vector persistence/schema, worker/FeaturesRequested consumer orchestration, deployment/container changes, web/public HTTP contracts, OpenAPI/generated TypeScript, LocalPlay, secrets, `Process.Start`, deprecated `Microsoft.Extensions.AI.Ollama`, new project/layer/port/ADR, forked enrichment claim/retry/DLQ pipeline, glossary rewrite, and unrelated refactors. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Depends on Spec workspace handoff. TB001–TB006 must have recorded evidence before T001. Conductor consolidates TB002/TB003 owner/Rigger prerequisite identities and Epic 7 evidence references into `DEV-412`; checklist marks must reference that receipt. Missing merged-spec/Gate 1, prerequisites, Epic 7 sequencing evidence, recon drift (including §15.5 factory-isolation refresh for T020), Gauge baseline, or Keel drift check blocks code. These are scheduled checks for Build intake; Spec does not run them or assert Epic 7 complete. No duplicated or new prerequisite.
- **Phase 1**: Depends on Phase B entry. Blocks Phases 2–3. Author T003/T004/T007 first; establish minimum compilable contracts when types are absent; demonstrate behavioral failure before T005/T006/T008 when possible. T001/T002 can proceed in parallel with test authoring. T005/T006/T008 are not parallel with their unfinished test prerequisites. Phase 1 tests green before the phase commit. Single Anvil ownership.
- **Phase 2**: Depends on Phase 1. Author T009/T010/T012 first where files differ; T011 follows T009 when sharing generator/composition test or minimal-contract files (including T009 FsCheck vector properties via Fact+Trait/`Prop.ForAll`). Establish minimum compilable contracts when types are absent; demonstrate behavioral failure before matching production edits T013–T018 when possible. T014–T016 explicitly allocate `AddOllamaEmbeddingGenerator`, singleton state, hosted-validator, and `TryAddSingleton(TimeProvider.System)` registration in the extension. Do not edit `Program.cs` or ServiceDefaults binding in this phase. Phase 2 tests green before the phase commit. Single Anvil ownership.
- **Phase 3**: Depends on Phase 2. Author T019 before T022–T024; T020 waits on TB004 naming actual isolation files and is not parallel with unfinished TB004 or overlapping T023 factory/`Program` sequencing. Same author-first / minimum-contract / behavioral-red-when-possible rule. Factory isolation (T020) must land with or before production `Program.cs` registration (T023) so unrelated hosts never require live Ollama. Phase 3 tests green before the phase commit. Single Anvil ownership.
- **Phase 4**: Depends on Phases 1–3 being committed. Cog owns T025 (harness.yml `gates.complexity.refactor` via supported `-Threshold`). Gauge owns T026 (no-args complexity is normal changed-diff and does not replace T025).

### User Story Dependencies

- **US1 (P1)**: Starts in Phase 1; end-to-end generate proof is T009/T024.
- **US2 (P1)**: Startup probe in Phase 2 (T011/T015); full composition proof in Phase 3 (T019/T022–T024).
- **US3 (P1)**: Resilience matrix T010 with adapter registration T014 and mapping in T013/T018.
- **US4 (P2)**: Health/telemetry T012/T016–T018; composition may re-assert zero-HTTP health in T019 if needed.

### Within Each User Story

- Author the behavior test before the production edit it locks. When types are absent, add minimum compilable contracts first; demonstrate behavioral failure before the behavior edit when possible. Missing-type compile failures are not behavioral red tests.
- Packages and options before adapter.
- Adapter and DI before Api composition.
- Factory isolation with or before production hosted registration.
- Telemetry constants before adapter instrumentation that references them.
- Phase tests green before that phase's commit.

### Parallel Opportunities

- TB001–TB006 are intake evidence gates; they complete before any Code Phase 1 task. TB002/TB003 evidence is Conductor-consolidated into `DEV-412`; checklist marks reference that receipt.
- T001 and T002 (different files).
- T003/T004/T007 test authoring in parallel (different files); T005/T006/T008 only after their respective tests are authored (behavioral failure when possible) — no `[P]` on unfinished-prerequisite production edits.
- T009/T010/T012 test authoring in parallel when writing different files; T011 is sequential after T009 when sharing generator/composition test or minimal-contract files. Do not treat shared contracts as independent parallel writes.
- T021 can proceed beside T019 (different file). T020 is not `[P]` relative to unfinished TB004 or shared factory edits overlapping T023.
- T022 can proceed after T019 is authored beside finishing T021, but T023 waits on isolation strategy from T020.
- Single Anvil ownership for Phases 1–3; parallel markers never split two writers onto the same adapter or shared test/stub file.

## Parallel Example: Phase 1 tests

```text
Task: "T003 Author OllamaOptionsTests in tests/LamuFlix.UnitTests/Options/OllamaOptionsTests.cs"
Task: "T004 Author OllamaResilienceOptionsTests in tests/LamuFlix.UnitTests/Options/OllamaResilienceOptionsTests.cs"
Task: "T007 Author EmbeddingGenerationExceptionTests in tests/LamuFlix.UnitTests/EmbeddingGenerationExceptionTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 contracts.
2. Finish Phase 2 adapter generate path and WireMock success/request-shape proofs.
3. Finish Phase 3 composition so a host can resolve the port.
4. Stop there only to validate US1. Frozen scope still requires US2–US4 before review.

### Incremental Delivery

US3 is proven with Phase 2 resilience tests. US2 completes with Phase 3 composition. US4 lands with Phase 2 health/telemetry and remains constitution-forced supporting scope. Phase 4 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. Do not split two writers onto the same adapter or shared test file.

## Notes

- [P] tasks use different files and do not depend on an unfinished task or shared test/minimal-contract file still being written by another unfinished task.
- Story labels map to `spec.md`.
- Commit after each phase, not after every task, using `DEV-412 - {phase subject}`.
- Gate 1 comes from the merged spec PR. Phase B entry (TB001–TB006) must also be evidenced before T001, with Conductor consolidating TB002/TB003 owner/Rigger receipts into `DEV-412`. This task list does not authorize implementation before that.
- New unanticipated files or decisions return to Keel; do not invent requirements.

