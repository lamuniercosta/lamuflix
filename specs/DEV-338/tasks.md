# Tasks: DEV-338 SimilarityScorer pure function

**Input**: Design documents from `specs/DEV-338/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Brief Q7 and task ordering require them. Author tests in the same phase as the behavior they lock. Once production compiles, the phase is not done until the listed tests pass. Do not start a later phase before the earlier phase is green and committed. Do not start Phase 2 until the spec PR has merged, Gate 1 is established, and Phase B prerequisites (Epic 7 plus merged DEV-336 and DEV-337, landed-signature recon, Gauge baseline gates, and Keel spec-drift analysis) allow build intake.

**Organization**: Phases follow brief task ordering and `spec.md` user stories US1–US3. Anvil implements one phase per ask. Phase 1 is dependency pickup only. Polish is Cog then Gauge.

**Domain vocabulary**: Use root `CONTEXT.md` Recommendation / Reason / Genre weight / GenreWeights / Genre similarity; Similarity score scoping as in `spec.md`. Do not edit root `CONTEXT.md`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 overlap, US2 proximity/normalization/identity, US3 Reasons.
- Exact file paths are in each description.

## Phase 1: Setup — Phase B dependency pickup

**Purpose**: Confirm predecessors, baseline gates, and spec drift before any DEV-338 production edit (brief.md:57; plan.md:84).

- [ ] T001 Confirm Epic 7 prerequisite and merged DEV-336 / DEV-337 implementations on the build base; record landed `MovieFeatures` and `GenreWeights` signatures (including genre-similarity call shape) via Wisp pickup recon. Do not invent method names or genre-identity bridging. Do not stub or reimplement predecessors. If contracts are missing or incompatible with brief Q2/Q6, stop for a decision — do not start Phase 2.
- [ ] T001a Gauge owns and records baseline gates on the build base before Builder intake (brief.md:57; plan.md:84). Phase 2 does not start until this baseline is recorded.
- [ ] T001b Keel owns and records Phase B spec-drift analysis against the frozen Phase A docs and landed predecessor contracts before Builder intake (brief.md:57; plan.md:84). Phase 2 does not start until drift analysis is clear.

**Checkpoint**: Landed-signature recon (T001), Gauge baseline gates (T001a), and Keel spec-drift analysis (T001b) are all recorded. Phase 2 cannot start solely on signature recon. Builder may proceed only when the full Phase 1 pickup is clear.

---

## Phase 2: User Story 1 - Overlap scoring and result contracts (Priority: P1) 🎯 MVP

**Goal**: `Reason`, `SimilarityResult`, and overlap terms (director, writers, cast, genres) with eligibility and argument guards.

**Independent Test**: Unit examples for Jaccard ratios, delegated genre similarity, missing-side exclusion vs disjoint zero inclusion, null/invalid-weight guards.

### Tests for User Story 1

- [ ] T002 [US1] Write `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` covering ArgumentNullException for null `a`/`b`/`weights`/`genreWeights`; ArgumentOutOfRangeException for negative/NaN/±Infinity weights; isolated Director/Writers/Cast Jaccard with known ratios under OrdinalIgnoreCase; missing-side exclusion vs nonempty disjoint zero inclusion retaining weight; Genres delegation to landed GenreWeights (including unknown-genre default 1.0) without a second formula. Method names `Method_Condition_Expected`. xUnit v3 + Shouldly. AAA headers only. No mocks, IO, or Testcontainers.

### Implementation for User Story 1

- [ ] T003 [P] [US1] Add `src/LamuFlix.Core/Features/Recommendations/Reason.cs` as `public sealed record Reason(string Text)`.

- [ ] T004 [P] [US1] Add `src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs` as `public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons)`.

- [ ] T005 [US1] Add `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs` as `public static class SimilarityScorer` with `Score(MovieFeatures a, MovieFeatures b, SimilarityWeights weights, GenreWeights genreWeights)` implementing FR-001–FR-006 and FR-011 for overlap terms and guards. Use landed GenreWeights API from T001 recon. No DI, interface, or options edits. No comments.

**Checkpoint**: US1 unit tests pass for overlap and guards. Records compile.

---

## Phase 3: User Story 2 - Proximity, normalization, identity (Priority: P2)

**Goal**: Year/runtime exponential proximity, rating proximity via `RatingBoost`, stable max-weight normalization, Q1 zero-denominator and exact-1 identity rules.

**Independent Test**: Known exp distances, rating boundaries, MaxValue stabilization, underflow, Q1 identity vs mismatch, empty vs identical profiles, exact 1.0 returns; FsCheck finite/symmetric/identity/input-snapshot properties.

### Tests for User Story 2

- [ ] T006 [US2] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` for year k=0.1 and runtime k=0.01 exponential proximity, rating formula boundaries, mixed and all-zero weights (Q1), empty vs identical seven-field profiles (Q5), exact 1.0 when every eligible positive-weight term is 1.0, finite MaxValue stabilization, and tiny-weight underflow behavior. Same file as T002 (not [P]).

- [ ] T007 [US2] Write `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerPropertyTests.cs` with FsCheck properties: score finite and in [0, 1]; symmetry within absolute tolerance 1e-12; exact `Score(a, a) == 1.0` when `a` has at least one scoring feature; inputs unchanged. Each property: FsCheck.Xunit `[Property(MaxTest = 100)]`, `[Trait("Category", "Property")]`, explicit deterministic Replay seed, no `System.Random`, no algorithm restatement. Respect landed MovieFeatures construction rules for generated inputs.

### Implementation for User Story 2

- [ ] T008 [US2] Extend `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs` with proximity/rating terms, `const double` decay rates, max-weight scaled normalization, clamp, Q1 zero-denominator boundary, and exact-1 short circuit per brief Q3/Q5 and spec FR-007–FR-010. Do not edit SimilarityWeights or RecommendationOptions.

**Checkpoint**: US2 unit and property tests listed above pass. Prior US1 coverage remains green.

---

## Phase 4: User Story 3 - Deterministic caller-safe Reasons (Priority: P3)

**Goal**: Per-term Reasons with fixed order, stable spelling, sanitization, labels/fallbacks, and one-way positive-score implication.

**Independent Test**: Reason eligibility/order/swap equality/sanitization/fallbacks/adversarial plain text/no internal IDs; FsCheck swap-identical Reasons and `Score > 0 => Reasons nonempty`.

### Tests for User Story 3

- [ ] T009 [US3] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` for Reason emission: eligibility requires `w > 0` and `s > 0`; fixed order Director → Writers → Cast → Genres → YearProximity → RuntimeProximity → RatingBoost; case-variant shared-token spelling identical under swap; null/blank/control-character sanitization with partial empty tokens omitted; generic fallbacks `Shared director` / `Shared writers` / `Shared cast` / `Shared genres`; labels per `ASSUMPTIONS.md`; Q1 `Identical feature profile`; adversarial plain text without HTML/Markdown; zero internal identifiers; Reasons still emitted when scaled underflow zeros the final score while raw contribution was positive. Same file (not [P]).

- [ ] T010 [P] [US3] Optionally add `tests/LamuFlix.UnitTests/Features/Recommendations/ReasonTests.cs` only if focused Reason construction/sanitization coverage is clearer as a separate file; otherwise keep Reason proofs in SimilarityScorerTests. Skip creating this file when T009 already covers the matrix.

- [ ] T011 [US3] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerPropertyTests.cs` with properties for deterministic swap-identical Reasons and one-way `Score > 0 => Reasons nonempty` (Q9; do not assert the superseded Q7 iff). Same tagging/Replay rules as T007. Same file as T007 (not [P] vs T007; may follow T007 sequentially).

### Implementation for User Story 3

- [ ] T012 [US3] Extend `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs` to emit Reasons per FR-012–FR-013 and `ASSUMPTIONS.md`. Sanitize display only; never alter scoring set membership. Plain text only.

**Checkpoint**: US3 unit and property tests pass. Full SimilarityScorer suite green.

---

## Phase 5: Polish & gates

**Purpose**: Cog refactors the committed build diff. Gauge runs gates. Not an Anvil phase.

- [ ] T013 Refactor only files in the approved set in `specs/DEV-338/plan.md` until `./scripts/run-cyclomatic-complexity.ps1` meets the refactor limit in `harness.yml`. Extract helpers, early returns, guard clauses. Do not suppress. Do not lower thresholds. Do not add files or dependencies.

- [ ] T014 After T013, run configured size:M gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1` (no `-All`), `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, `./scripts/run-mutation.ps1` without `--since`. Thresholds from `harness.yml`. Unchanged `/web` is `SKIPPED (scope-empty)`, never PASS. Exit 1 or Could not run blocks. Property-test exit 2 blocks (no opt-out). Mutation all-excluded is N/A, not PASS.

---

## Excluded (never scheduled)

Recommendation orchestration, candidates, storage, caching, endpoints, UI chips, configuration/options edits, predecessor MovieFeatures/GenreWeights implementations or stubs, tuning studies, new dependencies/projects/layers/schema, HTTP/OpenAPI/generated clients, secrets, `Features:LocalPlay`, `Process.Start`, Gherkin/Reqnroll, new ADR, root `CONTEXT.md` edit, second assertion/mocking library, Testcontainers, mocks, unrelated cleanup. A discovered need outside this list stops for a decision.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: Dependency pickup — T001 (Wisp landed-signature recon), T001a (Gauge baseline gates), and T001b (Keel spec-drift analysis). All three must complete. Blocks all production/test edits. Phase 2 cannot start solely on signature recon.
- **Phase 2 (US1)**: Depends on complete Phase 1 (T001 + T001a + T001b). Delivers records, guards, overlap.
- **Phase 3 (US2)**: Depends on Phase 2 scorer skeleton. Adds proximity/normalization/identity and core properties.
- **Phase 4 (US3)**: Depends on Phase 3 scoring path. Adds Reasons and Reason properties.
- **Phase 5**: Depends on Phases 2–4 committed. Cog owns T013; Gauge owns T014.

### User Story Dependencies

- **US1 (P1)**: Overlap + contracts — MVP foundation.
- **US2 (P2)**: Extends the same scorer; independently testable for proximity/identity once US1 compiles.
- **US3 (P3)**: Extends the same scorer; independently testable for Reason matrix once scores exist.

### Within Each User Story

- Author tests before or with production edits in that phase; phase commit waits until tests pass.
- Do not widen the approved file set.
- Do not invent landed predecessor APIs.

### Parallel Opportunities

- T003 and T004 in parallel before T005.
- T010 optional and parallel to T009 only when a separate ReasonTests file is justified.
- T006 then T007 then T008 within Phase 3 (T006/T007 before or with T008; T006 and T007 touch different test files and may proceed in parallel once the Phase 3 contract is known).
- T009 and T011 sequentially on their files after T012 or tests-first before T012; do not parallelize two writers on the same file.
- Do not parallelize two writers on `SimilarityScorer.cs` or on the same test file.

## Parallel Example: User Story 1 records

```text
Task: "T003 Add Reason.cs in src/LamuFlix.Core/Features/Recommendations/Reason.cs"
Task: "T004 Add SimilarityResult.cs in src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 pickup (T001 + T001a + T001b).
2. Deliver US1 records, guards, and overlap scoring with tests.
3. Validate independently, then continue US2–US3; frozen scope requires all three stories before review.

### Incremental Delivery

US2 adds proximity/normalization/identity required by ticket AC. US3 adds Reason chips required by ticket AC. Phase 5 does not add behavior.

### Parallel Team Strategy

One implementer in phase order is the expected path. Safe splits are listed under Parallel Opportunities.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Commit after each implementation phase using `DEV-338 - {phase subject}`.
- Gate 1 comes from the merged spec PR. This task list does not authorize implementation before that, and Phase B remains gated on Epic 7 plus merged DEV-336/DEV-337, plus T001/T001a/T001b (signature recon, Gauge baseline, Keel drift).
- Quill invented no decisions; content is from `brief.md` Q1–Q9, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, and recon facts.
