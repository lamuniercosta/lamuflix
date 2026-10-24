# Tasks: DEV-415 Cache TasteProfile in HybridCache

**Input**: Design documents from `specs/DEV-415/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why). Code Phases 1–5 also require the Phase B entry checklist below. Missing Phase B evidence blocks T001.

**Tests**: Included. Brief test strategy and FR-012 require them. Author the behavior test before the production edit it locks. When the types under test do not yet exist, add only the minimum compilable contracts needed so those tests compile; do not treat missing-type compile failures as behavioral red tests. When types already exist and the locked behavior is not already implemented, demonstrate behavioral failure before the behavior edit when possible. Drive each phase's tests green before that phase's commit. Use existing xUnit v3, Shouldly, NSubstitute, property-test conventions, real HybridCache 10.11.0, and Testcontainers PostgreSQL. Never mock drivers or Infrastructure. No EF InMemory. No new test dependency. Single Anvil ownership for Phases 1–5.

**Organization**: Phases follow brief task order items 2–7. Anvil implements Phases 1–5, one phase per ask, and stops after Phase 5. Phase 6 is Cog, then Gauge. Do not start Phase 1 until the Phase B entry checklist is satisfied.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 repeated-read cache, US2 cold/empty semantics, US3 invalidation, US4 freshness/cancellation.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Schedule Build workspace entry checks from the brief task order and PRODUCT §§1–3,5. Do not run gates, tests, or analyzers from this Spec workspace. Missing evidence blocks T001.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [x] TB001 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt. Receipt: `DEV-415` note, Handoff from Spec and Phase 1 Intake Confirmation; PR #244 merged at `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`, Ready to build verified.
- [x] TB002 Obtain Phase B drift recon against `origin/main` for the authorized file set (Wisp → `recon-DEV-415` refresh). Reconcile Hybrid absence, `IRecommendationProfileSource`, `EfRecommendationProfileSource`, `EfPlaybackHistory`, `EfMovieRepository`, `GetRecommendationsQueryHandler`, `GenreWeightsCache`, and persistence DI against recon-DEV-415 / recon-DEV-415-2 and frozen Q1–Q10. Off-envelope changes stop to Patron. Receipt: `recon-DEV-415`, Phase B intake drift recon sections 1–7 at `7c7b407`; `DEV-415` Phase 1 Drift Recon. Current source facts reconcile with frozen decisions; no production/test drift or off-envelope change.
- [x] TB003 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef` mode) recorded in `DEV-415`. Receipt: `DEV-415` Phase 1 Baseline Gate Counts; Roslyn, complexity, InspectCode each exit 0 with 0 findings. These are baseline receipts, not post-implementation gate results.
- [x] TB004 Keel checks or amends drift against the brief/spec before any Code Phase 1 edit. Missing Keel drift check blocks T001. Receipt: Keel Phase B pickup analysis below, 2026-10-24 at `7c7b407`; no blocking drift; Anvil cleared for Code Phase 1 (T001–T006).

**Checkpoint**: Phase B entry evidence is complete. Only then may Anvil start T001.

---

## Phase 1: Package pin, options, and DI registration (US1–US4 foundational)

**Purpose**: Central Hybrid 10.11.0 pin, Infrastructure reference, validated `TasteProfileCacheOptions`, `AddHybridCache`, and DI wiring for uncached concrete plus singleton adapter registration surface. Blocks adapter/handler behavior proofs. Restore must prove CPM resolution.

**Independent Test**: Options unit tests prove five-minute default, positive binding, and nonpositive startup failure. Host/DI composition can resolve HybridCache and options without claiming cache-hit behavior yet.

**Checkpoint**: Package restores; options validate; Hybrid registered process-local L1 only. Adapter body may still be incomplete stubs only as forced by registration.

### Tests for Phase 1

- [X] T001 [P] [US4] Author unit tests for `TasteProfileCacheOptions` in `tests/LamuFlix.UnitTests/` (beside existing options tests): section name `TasteProfileCache`; default Expiration five minutes; strictly positive duration accepted; zero/negative rejected at validation/startup path used by persistence registration. Minimum compilable contracts when types are absent; behavioral failure before T004 when possible.
- [X] T002 [P] [US1] Author focused DI/registration tests in `tests/LamuFlix.IntegrationTests/` or existing composition test location proving `AddLamuFlixPersistence` registers HybridCache, binds validated options, and registers a singleton adapter type for `IRecommendationProfileSource` without a Core Hybrid reference. Minimum compilable contracts when absent; behavioral failure before T005 when possible.

### Implementation for Phase 1

- [X] T003 [P] [US1] Add `<PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="10.11.0" />` to `Directory.Packages.props` and direct unversioned `<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" />` to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`. Run restore to prove CPM resolution; do not infer runtime from NuGet metadata alone. No Core package reference.
- [X] T004 [US4] After T001, add `TasteProfileCacheOptions` in `src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs` (section `TasteProfileCache`, default Expiration five minutes, strictly positive validation).
- [X] T005 [US1] After T002–T004, extend `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` to call `AddHybridCache` (process-local L1 only, no distributed provider), bind/validate options so both Hybrid `Expiration` and `LocalCacheExpiration` match configured duration, keep/register uncached `EfRecommendationProfileSource` for owned-scope resolution, and register one singleton `HybridCachedRecommendationProfileSource` instance for `IRecommendationProfileSource` and invalidation callers (stub type allowed only until Phase 3 if required for compilation; no request DbContext capture).
- [X] T006 [US1/US4] Drive T001–T002 green after restore.

**Checkpoint**: Phase 1 tests green. Commit `DEV-415 - HybridCache package options and DI`.

---

## Phase 2: Port extension and uncached derived-profile computation (US1–US2)

**Purpose**: Extend `IRecommendationProfileSource` with cancellation-only `GetTasteProfileAsync`; implement uncached computation on `EfRecommendationProfileSource` via existing calculators. Handler not switched yet.

**Independent Test**: Real PostgreSQL / unit coverage proves cold empty-only-without-history and compute-with-history on the uncached concrete; CancellationToken propagation; no caller genre-weight parameter.

**Checkpoint**: Port and uncached concrete compile and prove Q1/Q7 computation semantics without requiring the cache hit path.

### Tests for Phase 2

- [X] T007 [P] [US2] Author tests in existing `EfRecommendationProfileSourceTests` location under `tests/LamuFlix.IntegrationTests/` (or forced adjacent fixture) for uncached `GetTasteProfileAsync`: no qualifying history → empty profile; qualifying history → computed non-empty derived profile; CancellationToken propagation. Real PostgreSQL; no driver mocks. Minimum compilable contracts when method absent; behavioral failure before T009 when possible.
- [X] T008 [P] [US1] Author unit/architecture-adjacent coverage as needed proving the port surface retains `GetProfileInputsAsync` and adds cancellation-only derived retrieval (no caller genre weights). Update forced fakes/doubles only.

### Implementation for Phase 2

- [X] T009 [US1/US2] After T007–T008, extend `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs` with `GetTasteProfileAsync(CancellationToken)` returning `Task<TasteProfile>` via the repository-consistent async convention; retain `GetProfileInputsAsync`.
- [X] T010 [US2] After T007, implement uncached derived-profile calculation in `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs` using existing `GenreWeightsCalculator` and `TasteProfileCalculator` (all inputs inside this concrete’s work; cancellation only).
- [X] T011 [US1/US2] Drive T007–T008 green. Update only constructor/contract-forced fixtures.

**Checkpoint**: Phase 2 tests green. Commit `DEV-415 - uncached GetTasteProfileAsync port`.

---

## Phase 3: Singleton cached adapter (US1–US2, US4)

**Purpose**: Implement `HybridCachedRecommendationProfileSource` with owned scopes, one key/tag envelope, generation/age checks, coalescing, cancellation rules, and internal invalidation API. Wire the singleton body registered in Phase 1.

**Independent Test**: Real HybridCache 10.11.0 + Testcontainers PostgreSQL prove repeated-hit single computation; cold empty/history; fake TimeProvider age boundary; same-key coalescing; barrier old-fill across invalidation; one/all waiter cancellation and request-scope disposal; factory failure/cancellation without cached fallback and later recovery.

**Checkpoint**: Cache hit/miss/freshness/generation/cancellation AC paths proven at the adapter. Persistence hooks and handler switch remain Phase 4–5.

### Tests for Phase 3

- [X] T012 [P] [US1] Author Hybrid integration tests in `tests/LamuFlix.IntegrationTests/` for repeated reads reusing one computed derived profile and candidates remaining uncached (instrument with counters/interception as needed; no production hooks). Minimum compilable contracts when adapter absent; behavioral failure before T016 when possible.
- [X] T013 [P] [US2] Author Hybrid integration tests for cold miss empty-only-without-history and compute-with-history through the cached adapter path.
- [X] T014 [P] [US4] Author Hybrid integration/unit tests for: valid duration binding already covered in Phase 1; fake TimeProvider application age just before and at expiry (do not claim native MemoryCache expiry); same-key coalescing; barrier-controlled old fill across invalidation cannot supply old generation to a post-invalidation read; generation/age rejection without stale-value spin; one waiter cancels while another succeeds; all waiters cancel; initiating request scope disposes while owned-scope work continues safely; eventual owned-scope disposal; factory failure/cancellation does not cache error/empty fallback and later read recovers; retained `GetProfileInputsAsync` on the singleton creates/disposes a fresh owned scope per call (no request-scope capture), resolves the uncached concrete directly, propagates the caller token, and focused lifetime coverage proves scope disposal after the call (FR-008 / brief Q6).

### Implementation for Phase 3

- [X] T015 [US1/US2/US4] After T012–T014, implement `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs`: singleton dependencies only (HybridCache, scope factory, TimeProvider, validated options); one fixed trusted key and tag; envelope with profile/generation/CreatedAt; factory-owned scope resolving uncached concrete directly (no recursive port resolve); each factory envelope keeps a pre-read generation snapshot (and CreatedAt/time); after every awaited cache/fill result compare that snapshot with the same singleton adapter current generation and application age; stale results use the frozen eviction and cancellation-aware awaited retry path and never relabel old results; internal invalidation for Infrastructure callers MUST advance that same singleton adapter current generation with concurrency-safe increment/read semantics before reporting successful completion; caller tokens on waits/invalidation and shared token on owned-scope reads; retained `GetProfileInputsAsync` path on the singleton creates and disposes a fresh owned scope per call, resolves the uncached concrete directly (no recursive port resolve), and propagates the caller cancellation token (FR-008 / brief Q6). T014 remains the Build proof for barrier-controlled post-completed-invalidation rejection and cancellation/non-spin coverage.
- [X] T016 [US1/US2/US4] Drive T012–T014 green on real HybridCache + PostgreSQL.

**Checkpoint**: Phase 3 tests green. Commit `DEV-415 - HybridCachedRecommendationProfileSource`.

---

## Phase 4: Persistence invalidation hooks (US3)

**Purpose**: After successful persistence, invalidate from `EfPlaybackHistory.RecordAsync` and `EfMovieRepository.SaveChangesAsync` for playback and watched/not-interested set/unset. Preserve `GenreWeightsCache` behavior. Prove failed persistence does not invalidate; failed invalidation propagates.

**Independent Test**: Integration coverage for playback-then-recompute and four feedback set/unset paths; persistence-failure and invalidation-failure cases.

**Checkpoint**: Next-read recomputation AC after playback/feedback is evidenced.

### Tests for Phase 4

- [X] T017 [P] [US3] Author/extend integration tests beside existing `EfPlaybackHistoryTests` / movie repository tests in `tests/LamuFlix.IntegrationTests/`: after successful playback persist, next derived-profile read recomputes; failed playback persist does not invalidate.
- [X] T018 [P] [US3] Author/extend integration tests for watched set, watched unset, not-interested set, and not-interested unset: each successful `SaveChangesAsync` invalidates and forces recompute; failed save does not invalidate; failed invalidation propagates and does not claim database rollback. Preserve existing `GenreWeightsCache` expectations.

### Implementation for Phase 4

- [X] T019 [US3] After T017, hook post-successful-persistence invalidation in `src/LamuFlix.Infrastructure/Persistence/EfPlaybackHistory.cs` via the shared singleton adapter; do not invalidate on persistence failure.
- [X] T020 [US3] After T018, hook post-successful-save invalidation in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` for watched/not-interested set and unset; preserve existing `GenreWeightsCache` invalidation behavior; do not invalidate on persistence failure; let invalidation failure propagate.
- [X] T021 [US3] Drive T017–T018 green.

**Checkpoint**: Phase 4 tests green. Commit `DEV-415 - playback and feedback cache invalidation`.

---

## Phase 5: Handler consumption and regressions (US1)

**Purpose**: Route `GetRecommendationsQueryHandler` derived-profile reads through the existing port; keep other genre/candidate work uncached; retain calculator/property regressions and architecture rules (Hybrid out of Core; no new ports/layers).

**Independent Test**: Handler unit/property coverage plus architecture checks; existing calculator regressions remain green.

**Checkpoint**: Ticket end-to-end consumption path is evidenced. Ready for Cog refactor.

### Tests for Phase 5

- [ ] T022 [P] [US1] Author/extend unit tests for `GetRecommendationsQueryHandler` in `tests/LamuFlix.UnitTests/` proving derived profile is obtained via `GetTasteProfileAsync` on the port and candidates/other genre work stay outside the TasteProfile cache entry. Minimum compilable contracts when signature changes force them; behavioral failure before T024 when possible.
- [ ] T023 [P] [US1] Author/extend architecture tests in `tests/LamuFlix.ArchitectureTests/` keeping Hybrid out of Core and forbidding new ports/layers for this change. Retain existing calculator/property regressions in their current locations.

### Implementation for Phase 5

- [ ] T024 [US1] After T022, update `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` to consume derived profile through the existing port; leave required uncached genre/candidate work uncached.
- [ ] T025 [US1] Drive T022–T023 green and confirm existing calculator/property regressions still pass.

**Checkpoint**: Phase 5 tests green. Commit `DEV-415 - handler uses cached TasteProfile port`.

---

## Phase 6: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–5 are committed, Cog refactors the whole change. Gauge then runs the configured gates. Review loops follow brief Q10 closing bar and two-round cap.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass.

- [ ] T026 Refactor the completed change across the file set in `specs/DEV-415/plan.md` until cyclomatic complexity passes at the configured refactor threshold. Read `gates.complexity.refactor` from `harness.yml` and pass that configured value to the supported `-Threshold` parameter of `./scripts/run-cyclomatic-complexity.ps1`. Do not encode a task-owned numeric literal as configuration. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit harness/config/generated files or weaken thresholds.
- [ ] T027 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no arguments (normal changed-diff gate; does not replace T026 stricter refactor evidence), `./scripts/run-jetbrains-inspectcode.ps1` with no arguments, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and worktree-safe `./scripts/run-mutation.ps1` without `--since` for changed eligible production projects. Unchanged `/web` is scope-empty `SKIPPED`, not PASS. No threshold edits. Preserve Q10 verdict handling: skipped/opt-out/N/A/could-not-run never PASS.

---

## Excluded (never scheduled)

UI / `design.md`, clarify/checklist, ADR, CONTEXT.md or ASSUMPTIONS.md rewrite, new schema/migration, new NuGet/npm dependency beyond Hybrid 10.11.0, new project/layer/port, public HTTP/OpenAPI/web contracts, distributed cache provider, multi-server consistency claims, native MemoryCache clock guarantee, LocalPlay/secrets/`Process.Start`, playback execution changes, lowering gate thresholds, mutation `--since`, and unrelated refactors. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Depends on Spec workspace handoff. TB001–TB004 must have recorded evidence before T001.
- **Phase 1**: Depends on Phase B entry. Package/options/DI. Author T001/T002 first; T003–T005; drive T006 green before phase commit.
- **Phase 2**: Depends on Phase 1. Port/uncached computation. Author T007/T008 first; T009/T010; drive T011 green.
- **Phase 3**: Depends on Phase 2. Cached adapter. Author T012–T014 first; T015; drive T016 green.
- **Phase 4**: Depends on Phase 3 (invalidation target). Author T017/T018 first; T019/T020; drive T021 green.
- **Phase 5**: Depends on Phases 2–3 (port + adapter registration). Author T022/T023; T024; drive T025 green.
- **Phase 6**: Depends on Phases 1–5 being committed. Cog owns T026. Gauge owns T027.

### User Story Dependencies

- **US1 (P1)**: DI in Phase 1; port in Phase 2; hit path in Phase 3; handler in Phase 5.
- **US2 (P1)**: Uncached cold semantics in Phase 2; cached cold path in Phase 3.
- **US3 (P1)**: Invalidation hooks in Phase 4 (needs Phase 3 adapter).
- **US4 (P1)**: Options in Phase 1; freshness/cancellation/races in Phase 3.

### Within Each User Story

- Author the behavior test before the production edit it locks. When types are absent, add minimum compilable contracts first; demonstrate behavioral failure before the behavior edit when possible. Missing-type compile failures are not behavioral red tests.
- Phase tests green before that phase's commit.
- Anvil one production phase per ask.

### Parallel Opportunities

- TB001–TB004 are intake evidence gates; they complete before any Code Phase 1 task.
- T001 and T002 test authoring in parallel.
- T007 and T008 test authoring in parallel.
- T012, T013, and T014 test authoring in parallel when files differ.
- T017 and T018 test authoring in parallel when files differ.
- T022 and T023 test authoring in parallel.
- Single Anvil ownership for Phases 1–5; parallel markers never split two writers onto the same adapter or shared test file.

## Parallel Example: Phase 3 tests

```text
Task: "T012 Author repeated-read Hybrid integration tests"
Task: "T013 Author cold-miss Hybrid integration tests"
Task: "T014 Author freshness/cancellation Hybrid tests"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 package/options/DI.
2. Finish Phase 2 port/uncached computation.
3. Finish Phase 3 cache hit path.
4. Stop there only to validate US1. Frozen scope still requires US2–US4 before review.

### Incremental Delivery

US2 lands with Phases 2–3 cold semantics. US4 lands with Phase 3 freshness/cancellation. US3 lands with Phase 4 invalidation. Handler consumption completes in Phase 5. Phase 6 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. Do not split two writers onto the same adapter, persistence hook, or shared test file.

## Notes

- [P] tasks use different files and do not depend on an unfinished task or shared test/minimal-contract file still being written by another unfinished task.
- Story labels map to `spec.md`.
- Commit after each phase, not after every task, using `DEV-415 - {phase subject}`.
- Gate 1 comes from the merged spec PR. Phase B entry (TB001–TB004) must also be evidenced before T001. This task list does not authorize implementation before that.
- New unanticipated files or decisions return to Keel; do not invent requirements.
- Spec workspace does not run gates, tests, or analyzers for this ask.

## Keel Phase B pickup analysis — 2026-10-24

- Identity: Build worktree `F:/Dev/LamuFlix.worktrees/feature-DEV-415-cache-tasteprofile-in-hybridcache-with-invalidation-on-playb`, branch `feature/DEV-415-cache-tasteprofile-in-hybridcache-with-invalidation-on-playb`, HEAD `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`. Worktree clean before this checklist update. Recon records `origin/main` at the same HEAD; local `main` is stale and was not used.
- Analysis result: zero blocking inconsistencies, ambiguities, duplications, coverage gaps, or constitution conflicts across brief/spec/plan/tasks. All 22 requirements (FR-001–FR-015 and SC-001–SC-007) have task coverage; 31 checklist tasks total (TB001–TB004 and T001–T027). No unmapped tasks; entry and verification tasks support workflow obligations.
- Coverage: FR-001 → T003/T027; FR-002 → T002/T005; FR-003 → T001/T004/T005/T014; FR-004 → T008/T009; FR-005 → T007/T010; FR-006 → T002/T005/T014/T015; FR-007 → T012–T016; FR-008 → T007/T010/T014/T015; FR-009 → T017–T021; FR-010 → T012/T022/T024; FR-011 → T014/T015; FR-012 → T001/T002/T007/T008/T012–T014/T017/T018/T022/T023/T027; FR-013 → T023; FR-014 → approved file set, Excluded section and T023/T027; FR-015 → T026/T027. SC-001 → T012/T022/T024; SC-002 → T007/T013; SC-003 → T017–T021; SC-004 → T001/T004/T005/T014; SC-005 → T014–T016; SC-006 → T017/T018/T019/T020; SC-007 → T026/T027.
- Current facts are supplied by refreshed `recon-DEV-415`, not independent source searches. Its ten-path file-set reconciliation and current seam/fixture facts align with Q1–Q10. Conservative invalidation after every successful repository save is already approved by Q5. Planned new files and Hybrid references remain absent as expected before code.
- Evidence limits: original ticket line references and historical `recon-DEV-415-2` are not available in the current note roots (refreshed recon section 7). This pickup uses the frozen brief/spec/CONCLUSIONS as decision authority and refreshed recon as current source evidence; it does not independently re-establish historical package/API research. Restore/CPM, runtime compatibility, race/cancellation proofs and post-change gates remain scheduled Build obligations.
- Spec Kit prerequisite command ran once and exited 1 because it rejects the repository `feature/DEV-415-...` branch format. Artifact analysis used the explicitly assigned `specs/DEV-415/` paths; no successful prerequisite-script verdict is claimed. No before/after analyze hooks are registered. Analysis was read-only; this separately authorized checklist/receipt update changes no frozen scope or implementation tasks.
- Disposition: TB001–TB004 complete on the cited receipts. Anvil may start Code Phase 1, T001–T006 only, following the tests-first and phase-commit instructions. Later phases, verification, review clearance and merge remain separate steps. No code, tests or gates were run by Keel.
