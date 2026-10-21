# Tasks: DEV-342 SimilarityScorer property and golden tests

**Input**: Design documents from `specs/DEV-342/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, `contracts/`, or `ASSUMPTIONS.md` (`plan.md` records why).

**Tests**: This feature *is* the test suite. Author tests in the phase that owns them. Do not start a later phase before the earlier phase is green and committed. Do not start Phase 2 until the spec PR has merged, Gate 1 is established, and Phase B prerequisites (Epic 7 plus merged DEV-337 and DEV-338, pickup drift recon, Build-workspace intake) allow build.

**Organization**: Phases follow brief dependency-ordered plan and `spec.md` user stories US1–US2. Anvil implements one phase per ask. Phase 1 is dependency pickup only. Polish is Cog then Gauge.

**Domain vocabulary**: Use root `CONTEXT.md` Recommendation / Reason / Genre weight / GenreWeights / Genre similarity; feature-scoped labels as in `spec.md`. Do not edit root `CONTEXT.md`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 invariant properties, US2 golden rankings.
- Exact file paths are in each description.

## Phase 1: Setup — Phase B dependency pickup

**Purpose**: Confirm predecessors and pickup facts before any DEV-342 test write (brief.md Q1/Q6/Q8; plan.md Design Overview item 1).

- [x] T001 In the Build workspace, confirm Epic 7 prerequisite and merged DEV-337 / DEV-338 production implementations on the build base; record landed MovieFeatures VO bounds and constructors, SimilarityScorer and GenreWeights call signatures, string-genre / integer-ID bridge including null/blank handling, and predecessor-test overlap (including landed DEV-338 `SimilarityScorerTests.cs` / `SimilarityScorerPropertyTests.cs`) via Wisp pickup drift recon. Do not invent method names, numeric ranges, or genre-identity bridging. Do not stub or reimplement predecessors. If contracts are missing, contradict brief Q2–Q6, or overlap requires a predecessor edit, stop for a decision — do not start Phase 2.
- [x] T001a Gauge owns and records baseline gates on the build base before Builder intake (brief Q8; plan.md gates). Phase 2 does not start until this baseline is recorded.
- [x] T001b Keel owns and records Phase B spec-drift analysis against the frozen Phase A docs and landed predecessor contracts before Builder intake. Phase 2 does not start until drift analysis is clear.

**Checkpoint**: Pickup recon (T001), Gauge baseline gates (T001a), and Keel spec-drift analysis (T001b) are all recorded. Phase 2 cannot start solely on signature recon. Builder may proceed only when the full Phase 1 pickup is clear.

**Phase 1 pickup receipt — 2026-10-21 (Keel): CLEAR for Phase 2 Builder intake.** Analysis uses only note `DEV-342`, note `recon-DEV-342`, and the named feature artifacts. Landed-contract facts are from recon, not independent code searches; no implementation gates were run by Keel.

- **T001:** `recon-DEV-342:9-23,29-65,76-88` records base `541dd85985b98f9f1664a3238e9ba384c6ba17bc`, merged Phase A and DEV-337/DEV-338 production, VO bounds/constructors, scorer/IDF signatures, string-only genre scoring, and nonblocking predecessor-test overlap. Patron resolves both outstanding recon inputs (`recon-DEV-342:90-102,124-126`) in `CONCLUSIONS.md:115-123`, committed at `37952863d90c7a0b06912383c1ab37d1d184cbe9`: Epic 7 sequencing SATISFIED / APPROVED TO BUILD; Option (a) governs self-similarity eligibility.
- **T001a:** Note `DEV-342:9` records Gauge baseline Roslyn exit 0 / 0 findings, Complexity exit 0 / 0 findings, and InspectCode exit 0 / 0 WARNING+ issues. These are the recorded baseline results, not Phase 4 gate clearance.
- **T001b:** Cross-artifact drift analysis against brief Q1-Q8 and landed contracts in recon is CLEAR, with zero unresolved findings. Coverage: FR-001/002/003 -> T002; FR-004/005 -> T003; FR-006/009 -> T004/T005; FR-007 -> T006; FR-008 -> T005/T006; FR-010 -> T001/T001a/T001b; FR-011 -> approved scope and exclusions throughout. SC-001/003 -> T003/T008; SC-002/004 -> T004/T005/T006/T008; SC-005 -> T007/T008. All 11 FRs and 5 SCs have task coverage; all 10 tasks map to pickup, delivery, or required verification. No unresolved ambiguity, duplication, ordering conflict, or conflict with the constitution constraints recorded in plan.md was found within the supplied roots. No new constitution audit or review-axis clearance is claimed.
- **Builder constraint:** Apply Patron Option (a) during both generation and shrinking: self-similarity requires at least one non-blank scoring token or non-null scored scalar, AND `Genres.Count == 0 || Genres.Any(g => !string.IsNullOrWhiteSpace(g))`. Keep mixed non-blank plus null/blank genres for self-similarity; retain null/blank-only genre branches for the other three properties. Preserve four properties, 1000 cases each, the approved file set, predecessor tests, and independent oracle ordering. Patron explicitly requires no spec/plan/task semantic amendment; the existing T001 decision route and FR-002/FR-010 apply. Phase 2 intake may proceed; implementation tests and Phase 4 gates remain pending.

---

## Phase 2: User Story 1 - Scorer invariant properties (Priority: P1) 🎯 MVP

**Goal**: Local generators/shrinkers and the four 1000-case scorer properties against the real merged scorer.

**Independent Test**: All four tagged properties pass 1000 iterations each with Replay and shrinking diagnostics.

### Implementation for User Story 1

- [x] T002 [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesArbitraries.cs` with constructive MovieFeatures / pair / SimilarityWeights / GenreWeights generators and valid shrinkers per brief Q6 and spec FR-001–FR-003. Use landed VO bounds and call shapes from T001. Explicit branches for empty, single-field, partially missing, identical, disjoint/overlapping shared-vocabulary sets; case variants and preserved null/blank tokens; self-similarity restricted to nonempty seven-field profiles through generation and shrinking; finite nonnegative weights in `[0.0, double.MaxValue]` covering all-zero, single-positive, mixed, and extreme-ratio/MaxValue; real pure GenreWeights contexts (populated and empty/unknown defaults) without catalog/repository. Branch-coverage diagnostics retained. No new packages. No production edits. No explanatory comments.

- [x] T003 [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerInvariantPropertyTests.cs` with exactly four properties: exact `Score(a, a) == 1.0` for nonempty seven-field profiles including valid all-zero weights; symmetry `Math.Abs(scoreAB - scoreBA) <= 1e-12` under identical weights and the same GenreWeights snapshot; boundedness `double.IsFinite(score) && score >= 0.0 && score <= 1.0`; one-way `score > 0.0 => Reasons nonempty` (do not assert the converse). Each property: `[Property(MaxTest = 1000, Replay = "...")]` with a distinct committed Replay seed, `[Trait("Category", "Property")]`, preserved shrinking, reproducible minimized-counterexample diagnostics. Use T002 arbitraries. Featureless inputs retained for the three non-self-similarity properties. Leave unrelated existing `MaxTest = 100` property tests unchanged. Method names `Method_Condition_Expected`. xUnit v3 + Shouldly + FsCheck. AAA headers only. No mocks, IO, or Testcontainers.

**Checkpoint**: US1 properties pass 1000 iterations each under the property gate. Generators compile against landed contracts.

---

## Phase 3: User Story 2 - Independent golden top-3 rankings (Priority: P2)

**Goal**: Typed 30-movie fixture, independent oracle record, and three seed top-3 assertions.

**Independent Test**: Each of the three seeds yields the expected ordered top-3 against the real scorer and full-library GenreWeights.

### Implementation for User Story 2

- [x] T004 [P] [US2] Add `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs` with typed in-memory data for exactly 30 fixed movie identities, three seeds drawn from those 30, and explicitly constructed SimilarityWeights (`Director = 3.0`, `Writers = 2.0`, `Cast = 2.0`, `Genres = 3.0`, `YearProximity = 1.0`, `RuntimeProximity = 0.5`, `RatingBoost = 0.5`). Own initial curation of the common fixture profiles and seed choices as the first substep of this task. Only after that curation is fixed may the remaining fixture-expectation work proceed (independently derived ordered top-3 identity expectations per seed; strict score separation across top-3 and the membership boundary), and only then may T005 manual arithmetic proceed in parallel on its disjoint file. No disk I/O, JSON/CSV loader, or `LamuFlix.Tests.Common` change. Do not derive expected rankings by calling the scorer under test. Must complete before T006 starts.

- [x] T005 [P] [US2] Author `specs/DEV-342/golden-calculations.md` recording the closed-form manual arithmetic and why each seed's selected three outrank the remaining candidates, using landed DEV-337/DEV-338 formulas and the fixture's explicit weights. Write this in Phase B after prerequisites merge and after T004's initial profile/seed curation substep is fixed; do not put explanatory arithmetic in C# comments. May proceed in parallel with T004's remaining fixture-expectation work (disjoint files). Must complete before T006 starts.

- [x] T006 [US2] After T004 and T005 complete (independently derived expectations present and `golden-calculations.md` finished), add `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenTests.cs` that, for each of the three seeds, scores the other 29 movies through the real merged `SimilarityScorer` with `GenreWeights` calculated from the full 30-movie library and the fixture's explicit weights, then asserts the ordered top-3 identities match the fixture expectations. Seed removal from the candidate list is test setup only. Do not call the DEV-340 query handler or add the selected-movie exclusion property. No shadow scorer. Same testing stack as T003.

**Checkpoint**: US2 golden tests pass for all three seeds. US1 remains green. Oracle document exists.

---

## Phase 4: Polish & gates

**Purpose**: Cog refactors the committed build diff. Gauge runs gates. Not an Anvil phase. Spec workspace does not run these.

- [ ] T007 Refactor only files in the approved set in `specs/DEV-342/plan.md` until `./scripts/run-cyclomatic-complexity.ps1` meets the refactor limit in `harness.yml` (brief Q8: threshold <= 6). Extract helpers, early returns, guard clauses. Do not suppress. Do not lower thresholds. Do not add files, packages, or production paths.

- [ ] T008 After T007, run configured size:M gates and record each script verdict at the verified build head: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`. Thresholds from `harness.yml`. For this test-only diff with zero changed production C# under `src/`, `./scripts/run-mutation.ps1` must report native exit-2 `SKIPPED (scope-empty)`, nonblocking and never PASS — do not opt out, touch `src/`, or change thresholds. Web gates are N/A. Exit 1 or Could not run blocks. Property-test failure blocks (no opt-out). Route real dependency failures with Replay/counterexample rather than rewriting production, predecessor tests, or assertions. Complete mandatory three-axis review (Sentry, Ledger, Compass) under Q7 closing bar and two-round caps.

---

## Excluded (never scheduled)

Production code edits; predecessor DEV-337/DEV-338 test rewrites; DEV-340 selected-movie exclusion / query-handler tests; new NuGet/npm packages; new projects/layers; `LamuFlix.Tests.Common` changes; disk-backed golden loaders; HTTP/OpenAPI/generated clients; UI; schema/migration; secrets; `Features:LocalPlay`; `Process.Start`; Gherkin/Reqnroll; new ADR; root `CONTEXT.md` edit; `ASSUMPTIONS.md`; second assertion/mocking library; Testcontainers; mocks; SimilarityScorer/GenreWeights stubs or duplicate contracts; Spec-workspace implementation gates; unrelated cleanup. A discovered need outside this list stops for a decision.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: Dependency pickup — T001 (Wisp pickup drift recon), T001a (Gauge baseline gates), and T001b (Keel spec-drift analysis). All three must complete. Blocks all test edits.
- **Phase 2 (US1)**: Depends on complete Phase 1. Delivers arbitraries and four properties.
- **Phase 3 (US2)**: Depends on Phase 2 compiling against landed scorer (same Build workspace). Delivers fixture, calculations, golden assertions. T004 owns initial curation of common fixture profiles and seed choices as its first substep; after that curation is fixed, T004's remaining fixture-expectation work and T005 may proceed (including in parallel on disjoint files). Independently derived expectations and the completed T005 oracle record are required before T006 starts; both T004 and T005 must finish before T006.
- **Phase 4**: Depends on Phases 2–3 committed. Cog owns T007; Gauge owns T008.

### User Story Dependencies

- **US1 (P1)**: Generators + four properties — MVP proof.
- **US2 (P2)**: Golden rankings; independently testable once the real scorer is available; still required by ticket AC before review.

### Within Each User Story

- Do not widen the approved file set.
- Do not invent landed predecessor APIs or the genre bridge.
- Do not author golden data or expected rankings in Phase A / Spec workspace.

### Parallel Opportunities

- After T004's initial profile/seed curation substep is fixed, T004's remaining fixture-expectation work and T005 may proceed in parallel on disjoint files; both T004 and T005 must finish before T006 starts.
- Do not parallelize two writers on the same file.
- T002 before T003 (T003 depends on arbitraries).

## Parallel Example: User Story 2 fixture and oracle record

T004 first completes its initial profile/seed curation substep. Only after that curation is fixed (not before):

```text
Task: "T004 (remaining) Add SimilarityScorerGoldenFixture.cs expectations in tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs"
Task: "T005 Author specs/DEV-342/golden-calculations.md"
```

T006 starts only after both T004 and T005 finish.

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 pickup (T001 + T001a + T001b).
2. Deliver US1 arbitraries and four properties.
3. Validate independently, then continue US2; frozen scope requires both stories before review.

### Incremental Delivery

US2 adds the golden top-3 suite required by ticket AC. Phase 4 does not add behavior.

### Parallel Team Strategy

One implementer in phase order is the expected path. Safe splits are listed under Parallel Opportunities.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Commit after each implementation phase using `DEV-342 - {phase subject}`.
- Gate 1 comes from the merged spec PR. This task list does not authorize implementation before that, and Phase B remains gated on Epic 7 plus merged DEV-337/DEV-338, plus T001/T001a/T001b.
- Quill invented no decisions; content is from `brief.md` Q1–Q8, `CONCLUSIONS.md`, and recon facts.
