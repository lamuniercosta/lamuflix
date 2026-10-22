# Tasks: DEV-353 Recency-weighted TasteProfile

**Input**: Design documents from `specs/DEV-353/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `CONTEXT.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Brief Q9 requires them. Author tests in the same phase as the types they lock. Once a type compiles, the phase is not done until the listed tests pass. Do not start a later phase before the earlier phase is green and committed. Do not start Phase 2 production work until the spec PR has merged, Gate 1 is established, and Phase B pickup (Phase 1) completes without unruled drift.

**Organization**: Phases follow brief task ordering and `spec.md` user stories US1–US4. Feature glossary already lives in `specs/DEV-353/CONTEXT.md` (no root glossary task).

**Domain vocabulary** (must remain aligned with `specs/DEV-353/CONTEXT.md`): **TasteProfile**, **watch signal**, **genre preference**, **plot centroid** — definitions in `spec.md` Domain vocabulary.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 calculation, US2 cold start, US3 validation/options, US4 stability/properties.
- Exact file paths are in each description.

## Phase 1: Setup — Phase B read-only pickup

**Purpose**: Verify landed predecessor contracts, Epic 7 scheduling, harness settings, and property-test discovery before production edits.

- [ ] T001 In worktree `F:/Dev/LamuFlix.worktrees/feature-353-spec` on `feature/353-spec`, read-only verify current main/landed contracts for `GenreWeights` (`GetWeight(string)`), `EmbeddingDimensions.Value` (384), `RecommendationOptions`, and DEV-413 draft-vs-landed status against `recon-DEV-353` / `recon-DEV-353-2` and brief drift boundary. Confirm Epic 7 sequencing still gates Phase B under `specs/PRODUCT.md:17`. Verify the existing property-test framework, category trait, and seed conventions for `tests/LamuFlix.Test` against the live project and `scripts/run-property-tests.ps1`; do not assume a specific property package, Property category trait, or Replay seed discovery is installed. Read live `harness.yml` for implementation and refactor cyclomatic complexity thresholds and supported command parameters; do not invent numbers. Stop with `needs recon:` or a cited ruling request on missing facts, missing property conventions, or unruled substantive drift. No production edits in this task.

**Checkpoint**: Drift/scheduling/property-discovery verified or explicitly blocked. Inherited owner/ADR statuses preserved.

---

## Phase 2: Foundational — domain contracts and RecencyDecayLambda

**Purpose**: Immutable domain records and the ticket-required options member with validation tests. Blocks calculator work.

**⚠️ CRITICAL**: No user-story calculator work begins until this phase is complete.

### Tests

- [X] T002 [P] Write `tests/LamuFlix.Test/Domain/TasteProfileTests.cs` covering `TasteProfile.IsEmpty` true when both channels empty/default, false when either channel is populated, and construction with empty `ImmutableDictionary` / `ImmutableArray<float>.Empty`. Method names `Method_Condition_Expected`. xUnit v3 + Shouldly. AAA headers only where the file uses them.

- [X] T003 [P] Write `tests/LamuFlix.Test/Options/RecommendationOptionsRecencyTests.cs` covering default `RecencyDecayLambda == Math.Log(2.0) / 30.0`, finite and strictly positive acceptance, rejection of `NaN` / infinities / `0.0` / negative via the options validation path, and unchanged default `MmrLambda == 0.7`. Method names `Method_Condition_Expected`.

### Implementation

- [X] T004 [P] Add `src/LamuFlix.Core/Domain/TasteProfileMovie.cs` as `sealed record TasteProfileMovie(MovieId MovieId, ImmutableArray<string> Genres, ImmutableArray<float> PlotEmbedding, ImmutableArray<DateTimeOffset> PlayedAt, bool IsWatched)`. No comments.

- [X] T005 [P] Add `src/LamuFlix.Core/Domain/TasteProfile.cs` as `sealed record TasteProfile(ImmutableDictionary<string, double> GenrePreferences, ImmutableArray<float> PlotEmbedding)` with computed `bool IsEmpty => GenrePreferences.IsEmpty && PlotEmbedding.IsDefaultOrEmpty`. No comments.

- [X] T006 Edit `src/LamuFlix.Core/Options/RecommendationOptions.cs` only to add `RecencyDecayLambda` (default `Math.Log(2.0) / 30.0`) and validate finite strictly `> 0.0` in the existing validation path. Do not change `MmrLambda`, Take, CandidatePoolSize, or Weights semantics. No appsettings edit.

**Checkpoint**: Domain and options tests pass. `MmrLambda` default unchanged.

---

## Phase 3: User Story 1 - Recency-weighted TasteProfile (Priority: P1) 🎯 MVP

**Goal**: Synchronous calculator produces genre preferences and plot centroid from watch signals with exponential decay.

**Independent Test**: Fixed-clock unit oracles for decay mixtures, Q2 inclusion, channel independence, genre canonicalization, single clock read, determinism.

### Tests for User Story 1

- [X] T007 [US1] Write `tests/LamuFlix.Test/Features/Recommendations/TasteProfileCalculatorTests.cs` with a test-local BCL `TimeProvider` subclass that counts `GetUtcNow` calls. Cover: two distinct MovieIds with one launch each at ages 0 and 30 days and different single genres `gA`/`gB`; weights `1.0`/`0.5` and expected `GenrePreferences(gA)=(2.0/3.0)*rA`, `GenrePreferences(gB)=(1.0/3.0)*rB` for independently established supplied GenreWeights rarity values at default lambda (different rarity values not required); fractional days; offset-equivalent timestamps; GenreWeights rarity from supplied `GenreCounts`; unnormalized 384-float basis vectors (no L2 normalization); duplicate same-instant `PlayedAt` retention; undated `IsWatched` fallback without double-counting when launches exist; launches retained when `IsWatched` is false; independent genre/plot denominators; genre trim/case canonicalization (Ordinal minimum within OrdinalIgnoreCase); exactly one clock read; repeated-call bitwise determinism for fixed clock and ordered input; input immutability. Assert scalar coordinates and dictionary entries. Method names `Method_Condition_Expected`. Theory/MemberData where appropriate.

### Implementation for User Story 1

- [X] T008 [US1] Add `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs` implementing brief Q2–Q6: watch-signal expansion, once-per-call clock sample, per-channel `age - a_min` scaled exponential weights, genre preference formula, plot centroid formula, synchronous `Calculate(ImmutableArray<TasteProfileMovie>, GenreWeights)`. No I/O, DI, caching, or handler wiring. No comments.

**Checkpoint**: US1 calculator tests pass for decay, inclusion, channels, canonicalization, and determinism.

---

## Phase 4: User Story 2 - Cold start and empty channels (Priority: P2)

**Goal**: Valid empty history and empty channels return empty results without throwing; channels stay independent.

**Independent Test**: Extend calculator tests for empty/default inputs, no-signal rows with metadata present, genre-only, plot-only, and all-zero plot mean.

### Tests for User Story 2

- [X] T009 [US2] Extend `tests/LamuFlix.Test/Features/Recommendations/TasteProfileCalculatorTests.cs` for: empty/default movies array → empty profile without throw; rows with no launches and `IsWatched == false` even with genres/embeddings → empty profile; genre-only history; plot-only history; missing embeddings omitted from plot denominator; valid zero vectors included; all-zero mean → empty plot with genre preferences preserved; `IsEmpty` semantics. Same file as T007 (not marked [P]).

### Implementation for User Story 2

- [X] T010 [US2] Complete empty-channel and cold-start branches in `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs` per brief Q5 (including all-zero mean → `ImmutableArray<float>.Empty`) without weakening US1 behavior.

**Checkpoint**: US2 cold-start and empty-channel tests pass. US1 remains green.

---

## Phase 5: User Story 3 - Options and input validation (Priority: P3)

**Goal**: Fail-fast validation for lambda, null collaborators, and corrupted materialized rows.

**Independent Test**: Constructor/Calculate exception tests for every Q7 boundary; options tests already in Phase 2 remain green.

### Tests for User Story 3

- [X] T011 [US3] Extend `tests/LamuFlix.Test/Features/Recommendations/TasteProfileCalculatorTests.cs` for: null `TimeProvider` / null `RecommendationOptions` → `ArgumentNullException`; non-finite or `<= 0.0` lambda → `ArgumentOutOfRangeException` in constructor without requiring unrelated Take/MMR validity; null `GenreWeights` → `ArgumentNullException`; null movie rows, duplicate `MovieId`, nonempty embedding length ≠ 384, non-finite embedding floats → `ArgumentException` before accumulation (including unwatched rows); default/empty Genres/PlayedAt/PlotEmbedding succeed; duplicate `PlayedAt` preserved; caller inputs unchanged after success and after failure. Same file (not marked [P]).

### Implementation for User Story 3

- [X] T012 [US3] Implement constructor and Calculate validation in `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs` per brief Q7. Validate the full movie collection before accumulation. Do not mutate caller inputs.

**Checkpoint**: All Q7 boundaries covered. Prior phases remain green.

---

## Phase 6: User Story 4 - Stability and property invariants (Priority: P4)

**Goal**: Numerical stability for old/large-lambda histories and property-test coverage with no opt-out.

**Independent Test**: Unit oracles for future clamp, old-only history, huge finite lambda, relative underflow; property suite under existing discovery.

### Tests for User Story 4

- [X] T013 [P] [US4] Extend `tests/LamuFlix.Test/Features/Recommendations/TasteProfileCalculatorTests.cs` for future-timestamp age clamp, old-only histories that are not false cold starts, huge finite `RecencyDecayLambda`, relative underflow omitting exact-zero genre entries, and independent oracles that do not copy the production implementation. Same file concerns: do not mark [P] if editing concurrently with T014; prefer completing T013 before T014 if the same helper types are shared.

- [X] T014 [US4] Write `tests/LamuFlix.Test/Features/Recommendations/TasteProfileProperties.cs` asserting finite outputs, per-channel convex bounds, identical-source idempotence, uniform-replication invariance within floating tolerance, and fixed-clock determinism over valid deterministic bounded generated histories. Respect unique movie rows and Q2 fallback rules; replication concerns watch-signal multiplicity, not duplicate MovieId rows. Use the property-test framework, category trait, and seed conventions verified at T001 (including any required deterministic Replay seed); no `System.Random`, no algorithm restatement, no new package, no property-test opt-out. Compare coordinates and dictionary entries.

### Implementation for User Story 4

- [X] T015 [US4] Adjust `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs` only as required to satisfy T013–T014 while preserving Q1–Q8 semantics (stabilize with `age - a_min`, omit exact-zero genre entries). No scope expansion.

**Checkpoint**: Unit stability oracles and property tests pass. `./scripts/run-property-tests.ps1` applicable scope is green. Prior phases remain green.

---

## Phase 7: Polish & gates

**Purpose**: Refactor for complexity, then run applicable live pipeline gates.

- [ ] T016 Refactor calculator/helpers only within the frozen file envelope to satisfy cyclomatic complexity gates at the live configured implementation threshold, then at the live configured refactor threshold, using only supported command parameters verified at T001, without changing behavior. Preserve both stages; do not lower either threshold. No new files. No comments beyond allowed exemptions.

- [ ] T017 Run applicable gates on changed files from live `harness.yml`: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` at the live configured implementation threshold then again at the live configured refactor threshold with supported parameters verified at T001, `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and changed-production Core mutation via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` in worktrees). Report exit 0 / 1 / 2 distinctly; never label SKIPPED/SKIP/N/A/Could not run as PASS. Do not lower thresholds. Unchanged `/web` is scope-empty skip.

**Checkpoint**: Required gates pass or are honestly reported. Frozen scope preserved.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (pickup)**: No code dependencies — blocks production edits on drift/missing facts.
- **Phase 2 (foundational)**: Depends on Phase 1 clear — BLOCKS US1–US4 calculator work.
- **Phase 3 (US1)**: Depends on Phase 2.
- **Phase 4 (US2)**: Depends on Phase 3 (extends same calculator/tests).
- **Phase 5 (US3)**: Depends on Phase 3 (validation on same calculator); may proceed after or with Phase 4 sequentially in one file.
- **Phase 6 (US4)**: Depends on Phases 3–5 green.
- **Phase 7 (polish)**: Depends on Phases 3–6 complete.

### User Story Dependencies

- **US1 (P1)**: After foundational types/options — MVP calculator.
- **US2 (P2)**: Builds on US1 calculator empty-channel behavior.
- **US3 (P3)**: Builds on US1 calculator validation surface (options defaults already in Phase 2).
- **US4 (P4)**: Builds on complete calculator semantics from US1–US3.

### Parallel Opportunities

- T002, T003, T004, T005 can run in parallel during Phase 2 before/with T006 coordination on options.
- T013 helpers and T014 property file can proceed in parallel only when not contending on shared unfinished test helpers; otherwise sequence T013 then T014.
- US2/US3 edits share `TasteProfileCalculatorTests.cs` / `TasteProfileCalculator.cs` — implement sequentially.

---

## Parallel Example: Phase 2

```text
Task: "Write tests/LamuFlix.Test/Domain/TasteProfileTests.cs"
Task: "Write tests/LamuFlix.Test/Options/RecommendationOptionsRecencyTests.cs"
Task: "Add src/LamuFlix.Core/Domain/TasteProfileMovie.cs"
Task: "Add src/LamuFlix.Core/Domain/TasteProfile.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 pickup
2. Complete Phase 2 foundational contracts/options
3. Complete Phase 3 US1 calculator with decay oracles
4. **STOP and VALIDATE**: US1 independently proves ticket AC for recency weighting and determinism on nonempty history

### Incremental Delivery

1. Pickup + foundational → contracts ready
2. US1 → recency-weighted profile MVP
3. US2 → cold start / empty channels
4. US3 → validation boundaries
5. US4 → stability + properties
6. Polish + gates

### Notes

- [P] tasks = different files, no dependencies on unfinished work
- [Story] label maps task to US1–US4
- Verify tests fail before implementing where TDD applies inside a phase
- Commit after each phase or logical group with `DEV-353 - {subject}`
- Stop at any checkpoint to validate independently
- Avoid: widening the file envelope, adding DI/API/cache, inventing timestamps, property-test opt-out, lowering gate thresholds
