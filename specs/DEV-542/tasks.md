---

description: "Task list for DEV-542 recommendation seed and evidence cards"
---

# Tasks: Recommendation cards show seed and evidence

**Input**: Design documents from `specs/DEV-542/` — `spec.md`, `plan.md`, grill-closed private `brief.md` (Q1–Q5), `CONCLUSIONS.md`, `contracts/recommended-movie-seed-evidence.md`; Maestri notes `DEV-542`, `recon-DEV-542` (lines 44–69, 71–97); `specs/PRODUCT.md`.

**Status**: PLAN FROZEN. Plan challenge dispositions and task repairs are in `PLAN_ADJUDICATION.md`. Gate 1 is complete (T000). Build and runtime acceptance remain open.

**Tests**: Required (`brief.md` Test and evidence plan; FR-001–FR-013; SC-001–SC-007; Done when). TDD optional. Prefer real serialized API JSON for contract claims. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/checkpoint tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md`; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required path/fact/decision is missing; no scoring/ranking/selection change; no DEV-538 grouping or DEV-517 load-more; no retaining `reasons` beside seed/evidence and no legacy-`reasons` bridge during web migration; no hand-edits to generated OpenAPI/TS/MSW; never invent upstream provenance; never lower harness thresholds; no explanatory comments on changed C#. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats).

**Build conventions**: Keep existing assertion and mocking libraries and `Method_Condition_Expected` naming for new C# tests. Use AAA headers where needed within the permitted comment exemptions. Keep mutable or derived values out of static initialization in mutated projects. Use `const` for literals consumed by static instances and empty or expression-bodied constructors for static initializer call sites.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (pickup DEV-543/DEV-518 checkpoint + before screenshots) — blocks production edits
Phase 2   T002 → T003 → T004 → T005 → T006   (US1 API seed/evidence contract + regeneration; T003 after T002) — API-only checkpoint
Phase 3   T007 → T009   (US2 For You presentation + locally runnable component coverage; no browser-dependent checks yet)
Phase 4   T011 → T012   (US3 Related evidence-only presentation; second consumer migrates; no reasons bridge)
Phase 5   T008 → T010   (full web/e2e + viewport/browser checks for both surfaces + home-library) — web checkpoint
Phase 6   T013 → T014   (after-screenshot path capture/record + mandatory gates with applicability/verdicts)
Phase 7   T015          (capped three-axis technical review / remediation; may use captured screenshot files; two-round cap)
Phase 8   T016          (publication + final verification: attach/read back both screenshot pairs; then merge-bar closure — user merges)
```

Ordering per brief Task ordering: **Gate 1 → dependency checkpoint/before screenshots → DTO/mapping/OpenAPI regen (API-only checkpoint; T003 after T002) → both card consumers/fixtures move in the same uncommitted working unit with locally runnable component coverage → full web/e2e including browser/viewport checks (web checkpoint) → after-screenshot paths + gates → three-axis technical review → delivery PR publication/final verification of screenshot pairs**. Sequence DEV-542 ahead of DEV-541 because they regenerate the same OpenAPI files. No dependency cycle. Do not retain legacy `reasons` to bridge the transition.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [x] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-542 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-542 in `specs/DEV-542/CONCLUSIONS.md`. Receipt only. T001–T016 MUST NOT proceed without verified merge (`brief.md` Authority and status; Q5; no structural owner checkbox invented by Quill).

---

## Phase 1: Pickup dependency checkpoint and before screenshots (blocks production edits)

**Purpose**: Verify DEV-543/DEV-518 delivery inputs and capture before screenshots before any presentation or DTO production edits.
**Independent Test**: Checkpoint receipt records real For You seeds and ordered evidence (including empty lists); before screenshot pair exists with recorded paths.
**Order**: T001 after T000. **STOP** on failed checkpoint (`brief.md` Q4; FR-012).

- [ ] T001 **Build lane**. After T000: In the Build worktree, verify the exact DEV-543 seed source supplies a real `movieId`, nonblank title and a supported signal for **every** emitted For You item. Verify DEV-518 supplies strongest-first criterion evidence, including legitimate empty lists, on both routes. Cite call sites and the public criterion vocabulary source, the Related assembly, the shared pure Core formatter and its concrete test file (reuse DEV-518 or its existing feature-local seam), and the For You component-test/style and shared MovieCard style paths. Inventory the affected consumer fixtures and route any additional tracked paths outside the envelope for a cited Patron ruling before edits. Record one shared responsive rule (file, selector and existing breakpoint) for both paths. Bound any added presentation-only work to the selected results; verify no new candidate-loop reads or per-result seed/evidence lookups. If a required source, invariant, or path is unresolved, STOP for bounded recon and a cited Patron ruling before production edits (FR-012; Q4). Capture desktop For You and one Related rail card set before production edits. Record paths, baseline build/ref, library fixture or state identifier, viewport widths and cards; recapture against the clean baseline after a pickup restart. Record the DEV-542-first/DEV-541-second regeneration handoff and expected integration base.

**Checkpoint**: T002+ unlocks only with a passing dependency checkpoint and before screenshots (with recorded paths) on record.

---

## Phase 2: User Story 1 - Recommendation response carries seed and evidence (Priority: P1) 🎯 MVP

**Goal**: Shared DTO exposes optional `seed` and required ordered `evidence`; For You always populated seed; Related omits seed key; reasons removed; artifacts regenerated together.
**Independent Test**: Integration/API suites assert the new JSON shape. Regenerated OpenAPI/TypeScript mark `seed` optional and `evidence` required, omit `reasons`, and constrain `signal` to the three supported values. T006 is an internal API/schema checkpoint within the T002–T010 commit unit.
**Order**: T002 → T003 → T004 → T005 → T006 after T001.

### Tests for User Story 1

- [ ] T002 [US1] **Build lane**. After T001: Update `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` (including current `reasons` reads near lines 431–434) and `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`. On parsed, populated responses require the expected case-sensitive JSON keys and non-null For You seed fields; enumerate properties to reject any casing of `reasons` and, for Related, any casing of `seed`, even when null. Use known inputs to assert each route's exact expected criterion sequence, server text, and complete matched-name order. Assert shared empty-array and formatted evidence invariants across both routes and preserve result IDs, scores and order. Verify pending-enrichment Related returns the existing successful status and an empty recommendation list. Assert neither route emits the retired viewing-history copy in the test fixtures (FR-001–FR-004, FR-009, FR-011; SC-005–SC-006).
- [ ] T003 [US1] **Build lane**. After T002: In the Core formatter test file identified at T001, assert the exact five-name display string `Shared cast: Name One, Name Two, Name Three +2` and the full ordered list of five names. Cover zero, three and more than three display names, control-character removal at the display boundary, and a larger list. Use the existing property-test conventions to exercise preservation of the ordered matched names, the three-name display cap and the suffix count computed from names omitted from the display. Preserve upstream name identity and ordering (FR-004; SC-003; Q3).

### Implementation for User Story 1

- [ ] T004 [US1] **Build lane**. After T002 and T003 are authored: Change `RecommendedMovieDto.cs` and `RecommendationEndpoints.cs` (`ToDto`) to map ordered DEV-518 evidence and DEV-543 For You seed. Use property-level `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` for the nullable shared seed and a closed wire mapping for the three signal values. Map criterion through the DEV-518 public identities verified at pickup. Consume the shared Core formatter identified at T001 on both paths. Preserve upstream evidence order and full matched names. Retain control-character sanitization for display text. Bind additional display-only work to the selected results and use upstream fields without new per-item or candidate-loop reads. Replace the legacy `RecommendationReasonBuilder` path and retire unused `ToReasonDto`/`Kind`; remove discarded reason types/files only within the plan envelope and with caller proof. If pickup cannot establish the upstream invariants, stop as required by T001. Route any observed runtime contract defect to Patron without adding fallback seed/criterion values, dropping items or changing statuses. Keep T002–T003 green and preserve existing result identity, scores, order, routes and statuses (FR-001–FR-004, FR-009, FR-012–FR-013).
- [ ] T005 [US1] **Build lane**. After T004: Export OpenAPI with `LAMUFLIX_REGENERATE_OPENAPI` (including `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`), then run `npm run generate:api` in `web/` to regenerate `openapi.json`, `types.ts`, and `handlers.ts`. Keep the DTO, generated artifacts, consumers, fixtures, and related tests in one working unit through T010. Publish the contract transition in one coherent commit after both consumers compile and the T010 automated web checks pass (FR-010; SC-002; Done when).
- [ ] T006 [US1] **Build lane**. After T005: **API-only checkpoint** within the working unit. Confirm passing T002 wire and T003 formatter tests. Verify regenerated OpenAPI and TypeScript describe optional shared `seed`, required `evidence`, no `reasons`, and exactly `liked`, `watched`, `watchlist` as seed signal values. Record the integration base and DEV-541 handoff: DEV-542 lands first and DEV-541 rebases onto that landed result before regenerating shared artifacts. Record only API/schema evidence here. T010 runs full web typecheck and `npm run check:api` after both consumers and fixtures migrate (FR-010; SC-002; SC-005).

**Checkpoint (API-only)**: T006 records API JSON and generated schema/type checks. Full web typecheck, `npm run check:api`, and e2e run in T010. Keep the transition uncommitted through those automated checks.

---

## Phase 3: User Story 2 - For You cards show seed then evidence (Priority: P1)

**Goal**: For You cards show Wanted item 2 seed line then capped evidence list; For You consumer and fixtures compile against the generated contract without a `reasons` bridge.
**Independent Test**: Locally runnable component coverage green against generated types for seed/evidence presentation rules. Browser-dependent / real-viewport checks belong to Phase 5 (T010) after T012.
**Order**: T007 → T009 after T006.

### Tests for User Story 2

- [ ] T007 [US2] **Build lane**. After T006: Update `web/src/library/MovieCard.test.tsx` and the For You component test located at T001. Use locally runnable coverage for zero/one/three evidence items (three → two in desktop mode), all three seed signals, no seed line when the shared optional seed is absent, verbatim plain-text evidence with the exact five-name formatter fixture, and seed-before-evidence reading order. Assert distinct recommendation/seed IDs, link target and accessible name `Open {title}`, separate focus/keyboard activation and seed click routing. Use generated data types for fixtures. T010 owns the viewport clause of SC-003 and the browser navigation checks (FR-005–FR-008, FR-011; SC-003; SMOKE-001).

### Implementation for User Story 2

- [ ] T009 [US2] **Build lane**. After T007 is authored: Update `web/src/library/MovieCard.tsx`, `web/src/features/for-you/ForYouPage.tsx`, and T001 style paths. Place the seed anchor outside the recommendation anchor as an independent navigation target. Seed activation must not trigger recommendation navigation. Render the three ticket templates in a linked seed line before the shared semantic `ul`/`li` evidence list. For an absent optional seed, omit the line. Render evidence text as delivered as plain text without client formatting or fallback. Use the T001 responsive rule so both the visible and accessible lists are capped at two on desktop and one at small widths. Make the local T007 suite green (FR-005–FR-008; Q1).

**Checkpoint**: For You consumer compiles and presents seed then evidence against the generated contract with local component coverage green; full web/e2e and browser/viewport checks still blocked until Related consumer migrates (T012).

---

## Phase 4: User Story 3 - Related cards show evidence without a seed line (Priority: P2)

**Goal**: Related rail shows capped evidence only; four named Related tests rewritten and green; Related consumer migrates with the generated contract.
**Independent Test**: Rewritten Related tests pass as locally runnable component coverage; no seed line on Related cards. Browser-dependent shared small-width/viewport verification for Related is Phase 5 after T012.
**Order**: T011 → T012 after T009 (shared `MovieCard` evidence rendering).

### Tests for User Story 3

- [ ] T011 [US3] **Build lane**. After T009: Rewrite the four tests at `web/src/features/movie-details/RelatedMovies.test.tsx` lines 125, 148, 548 and 580 using the generated evidence shape. Assert ordered evidence, evidence-only cards and no invented copy for empty evidence. Cover the shared desktop cap locally. T010 owns the both-surface small-width/viewport clause (FR-007–FR-008, FR-011; SC-004; SMOKE-002).

### Implementation for User Story 3

- [ ] T012 [US3] **Build lane**. After T011 authored: Update `web/src/features/movie-details/RelatedMovies.tsx` and `web/src/features/movie-details/RelatedMovies.css` to pass/render evidence without a seed line and remove legacy reasons presentation — do not retain `reasons` to bridge the transition (FR-007–FR-008). Make the locally runnable T011 suite green; do not gate T012 on browser/viewport execution (that is T010).

**Checkpoint**: Both generated-contract consumers (For You and Related) compile without `reasons`. Unlocks Phase 5 full web/e2e and browser/viewport checks.

---

## Phase 5: Full web/e2e and home-library (web checkpoint)

**Purpose**: Validate the migrated web contract and browser behavior on both surfaces, then record the separate home-library Hercules observation.
**Order**: T008 → T010 after T009 and T012.

- [ ] T008 [US2] **Build lane**. After T009 and T012: Update `web/e2e/for-you-review.e2e.ts` to read the seed title instead of reasons (current reads near lines 39–40 and 94–98). Pin the seed and recommendation IDs and titles in the test's fixture setup with different IDs. Use independent seed and recommendation activation assertions that resolve to their respective movie details. Keep the automated fixture distinct from the required home-library receipt (FR-006, FR-011; SC-003, SC-005; SMOKE-001).
- [ ] T010 [US2] **Build lane**. After T008, T009, and T012: **Web checkpoint**. Run full web typecheck, `npm run check:api`, affected component tests and For You e2e against the regenerated contract. Execute the SC-003/SC-004 viewport check at desktop and small widths on **both** surfaces using T001's shared rule; verify visible lines and accessible list item counts agree (two/one with three input items). Verify distinct seed and recommendation navigation with the browser fixture. Perform the home-library reviewer check that Hercules shows “Because you liked Aladdin” first with a “Same director” line beneath. Record viewport and automated web verdicts separately from the home-library observation. Commit the coherent T002–T010 contract transition after the automated checks pass and record the commit in the evidence receipt (FR-006–FR-007, FR-010–FR-011; SC-001–SC-005).

**Checkpoint (web)**: Full web/e2e, both-surface viewport/browser verification where component tests cannot, and home-library pass against seed/evidence consumers.

---

## Phase 6: Polish — after-screenshot paths and mandatory gates

**Purpose**: Capture/record after-screenshot paths and run applicable size:M gates with applicability and actual verdicts.
**Order**: T013 → T014 after T010.

- [ ] T013 **Build lane**. After T010: Capture after screenshots of desktop For You and the same Related rail card set at matching viewport widths and library fixture/state; record the build/ref, viewport widths, library state and paths alongside the T001 baselines. Resolve any mismatch with a matched recapture before publication. Record the ticket one-off source check `rg "Matches your viewing history" src web/src` (only a test asserting removal may match); T002 owns behavioral removal coverage. Attach/read back the pairs in T016 (FR-009, FR-011; SC-006–SC-007; Done when).
- [ ] T014 **Build lane**. After T013: Run applicable size:M checks and **record applicability plus actual verdicts** for each — constitution PR quality gate that `LamuFlix.ArchitectureTests` passes (`constitution.md:775`); for every changed `.cs` file the constitution static-analysis gates (`constitution.md:804–815`): `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (then `-Threshold 6` when the refactor gate applies), `./scripts/run-jetbrains-inspectcode.ps1`; applicable property-test gate (`./scripts/run-property-tests.ps1` or recorded N/A with reason); applicable vulnerable-package gate (`./scripts/run-vulnerable-packages.ps1` or recorded N/A with reason); also `dotnet format --verify-no-changes` as a supplemental formatting check, `dotnet test`, mutation via `scripts/run-mutation.ps1` without `--since` when production C# under `src/` is eligible (else record SKIPPED/N/A honestly); in `web/`: `npm run check:api` and applicable lint/component/e2e suites. Never fold SKIPPED/Could not run into PASS; never lower thresholds; add no explanatory comments (FR-013; SC-007; CLAUDE.md / AGENTS.md; plan Governance row).

**Checkpoint**: Screenshot paths recorded; gates recorded with honest applicability and verdicts, including ArchitectureTests and static-analysis on every changed `.cs` file.

---

## Phase 7: Three-axis technical review under closing bar

**Purpose**: Size:M Sentry, Ledger, and Compass technical review/remediation with the written closing bar and two-round cap. May adjudicate findings using the captured screenshot files from T001/T013. Does **not** make the final merge-bar / acceptance closure that depends on screenshot publication.
**Order**: T015 after T014.

- [ ] T015 **Review lane**. After T014: Run Sentry, Ledger, and Compass against the frozen in-scope change (seed/evidence contract and rendering, legacy reason retirement, generated artifacts, ticket tests/evidence). Adjudicate findings using captured screenshot files and other recorded evidence as needed. Fix or explicitly dispose every accepted in-scope Critical/High/Medium/Low finding with evidence-backed disposition; confirm applicable gates meet documented verdict requirements (including ArchitectureTests and static-analysis evidence from T014). Cap is two review/remediation rounds; report unresolved findings after round two for owner review (`brief.md` Q5). **Do not** claim final closure or merge-bar readiness here — screenshot **publication** is itself a ticket acceptance criterion and is completed only in T016 after attach and read-back. Out of scope: DEV-538 grouping, DEV-517 load-more/swap, scoring/ranking/selection — follow-up issues, not findings in this round.

---

## Phase 8: Delivery handoff — publication and final verification

**Purpose**: Open/update the delivery PR, attach and read back both screenshot pairs (publication), then make the final merge-bar / acceptance claim. Follows T015 technical review/remediation; two-round cap already applied in T015.
**Order**: T016 after T015.

- [ ] T016 **Bernstein/Rigger/Quill as pipeline assigns**. After T015: Open/update the delivery PR with merge-bar proof, findings dispositions from the capped T015 review, home-library receipt, and gate evidence including ArchitectureTests plus the recorded static-analysis / property-test / vulnerable-package applicability and verdicts from T014. **Attach both before/after screenshot pairs** (desktop For You and Related rail card set) using the paths recorded in T001/T013, **read back / verify publication** on the PR (publication is a Done-when acceptance criterion), and only then assert final merge-bar / acceptance readiness. Only the user merges. Do not claim Gate 1/Build/runtime acceptance from Phase A drafting.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work
- **Phase 1 (T001)**: Depends on T000; blocks production edits
- **Phase 2 (US1)**: Depends on T001 — ends at **API-only checkpoint** (T006)
- **Phase 3 (US2 presentation)**: Depends on T006 (generated types); For You consumer moves with the contract
- **Phase 4 (US3)**: Depends on T009 shared evidence rendering; Related consumer migrates; no `reasons` bridge
- **Phase 5 (web checkpoint)**: Depends on T009 and T012 — full web/e2e and browser/viewport checks only after both consumers compile
- **Phase 6–7**: Depend on web checkpoint (T010); T013 records screenshot paths; T015 runs capped technical review (may use captured files)
- **Phase 8 (T016)**: After T015 — publication + final verification (attach/read back screenshot pairs); only then final merge-bar claim

### User Story Dependencies

- **US1 (P1)**: After checkpoint — no dependency on UI stories; API-only validation at T006; T003 sequential after T002
- **US2 (P1)**: After US1 generated contract; local presentation (T007/T009) before browser/e2e (T008/T010)
- **US3 (P2)**: After shared MovieCard evidence rendering from US2; independently testable on Related fixtures with local component coverage; must complete before full web/e2e and viewport checks

### Parallel Opportunities

- T002 through T006 are sequential
- Same-file UI edits (T009/T012) are sequential with their tests
- T008 may be authored once T009 and T012 are complete (not before Related migrates); browser/viewport checks execute in T010

### Sequential Example: User Story 1 tests

```text
Task: "Update ApiEndToEndRecommendationTests + RecommendationEndpointTests for seed/evidence" (T002)
Then: "Test exact display text and full ordered names in the Core formatter test file identified at T001" (T003 after T002)
```

---

## Implementation Strategy

### API verification before the web migration

1. Complete Phase 0–1 (Gate 1 + checkpoint/before screenshots with recorded paths)
2. Complete Phase 2: US1 API contract + regeneration
3. Validate integration and generated schema/type shape at T006 inside the uncommitted unit
4. Continue to US2/US3 consumer migration and Phase 5 web checks. Publish the coherent contract transition after T010 automated checks.

### Incremental Delivery

1. US1 → contract ready (API-only checkpoint)
2. US2 presentation → For You consumer compiles on seed/evidence
3. US3 → Related consumer migrates (both consumers compile; no reasons bridge)
4. Web checkpoint → web typecheck/check:api, e2e, matched viewport checks on both surfaces, and the home-library reviewer check
5. Screenshot path capture, gates (ArchitectureTests + applicability/verdicts), capped technical review (T015), then delivery PR publication/final verification of screenshot pairs (T016)

### Notes

- `[P]` tasks = different files, no incomplete deps; T003 is sequential after T002
- Preserve recommendation identity, score, and order throughout
- DEV-542 regenerates OpenAPI ahead of DEV-541; blocks DEV-538
- Avoid: inventing upstream seed/evidence, hand-editing generated files, retaining `reasons` (including as a transition bridge), changing ranking
- Distinguish API-only checkpoint (T006) from web checkpoint (T010); browser-dependent checks live only in the web checkpoint
- Distinguish T015 technical review (two-round cap; may use captured screenshot files) from T016 publication + final verification / merge-bar claim
