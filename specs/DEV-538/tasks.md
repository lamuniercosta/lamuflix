---

description: "Task list for DEV-538 For You page grouped by reason"
---

# Tasks: For You page grouped by reason

**Input**: Design documents from `specs/DEV-538/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q8), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `contracts/for-you-grouping-fields.md`; Maestri notes `DEV-538`, `recon-DEV-538`; `specs/PRODUCT.md`.

**Status**: PLAN FROZEN — Keel Phase 2 step 6. See [plan adjudication](PLAN_ADJUDICATION.md). Gate 1 verified in [the merge receipt](CONCLUSIONS.md#gate-1-spec-kit-pr-merge-receipt-t000). Build pickup and runtime acceptance remain pending.

**Tests**: Required (`brief.md` Plan steps 4–5; ticket Done when; FR-001–FR-010; SC-001–SC-006; SMOKE-001–003). TDD optional. Use existing unit/integration/OpenAPI/web test homes. No new test framework/dependency. Never mock the data-access driver for behaviour that depends on real translation.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/checkpoint tasks are never `[P]`.

**Conventions**: Follow frozen Q7 order (prereq confirm → backend fields → OpenAPI/web contract → grouped UI → verification). Edit only the authorized envelope in `plan.md`; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no scoring/ranking/schema/dependency change; no themed-row paging; no hand-edits to generated OpenAPI/TS/MSW; never lower harness thresholds; no explanatory comments on changed C#. Otherwise-unassigned implementation tasks are owned by the generic Build lane (Anvil creates; Cog alters). Named seats own Spec PR, pickup analysis, refactor, gates, review, and ship per `task-pipeline:89-139`.

**Build conventions**: Keep existing assertion and mocking libraries and `Method_Condition_Expected` naming for new C# tests. Use AAA headers where needed within the permitted comment exemptions. Keep mutable or derived values out of static initialization in mutated projects. Use `const` for literals consumed by static instances. Changed `.cs` tasks are complete only after ArchitectureTests and the three static-analysis gates pass for those files (`constitution.md:511,773-775,801-817`). Gauge runs that Phase 2 checkpoint in T005a after T005 and before regeneration; Phase 6 T014 re-runs after Cog refactor and always runs mutation whenever production C# under `src/` changed (script decides applicability).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance and Patron `gate1: provisional`, Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges after resolving the owner checkbox; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1. Spec PR MUST NOT open before plan challenge / provisional clearance (`task-pipeline` Phase 2 steps 6–7).

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (plan challenge + provisional → Bernstein/Rigger open; user checkbox+merge; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup evidence → Keel pickup analysis → before screenshots) — blocks production edits
Phase 2   T002 → T005a   (US1 backend startedCollectionId + hasSeeds + tests + Phase 2 ArchitectureTests/static gates) — checkpoint before regen
Phase 3   T006 → T007   (contract regeneration OpenAPI → types shape) — after Phase 2 checkpoint; blocks web grouping
Phase 4   T008 → T011   (US2–US4 grouping, MovieCard opt-out, empty states + sequential component tests)
Phase 5   T012 → T013   (US5 More for you / DEV-517 integration + Load more/Swap regressions)
Phase 6   T014 → T016   (Cog refactor + Gauge full matrix incl. mutation whenever src/ C# changed; Size L Phase 4 review/remediate; Phase 5 ship/merge-bar/smoke)
```

Ordering per brief Q7: **Gate 1 → Wisp/Keel pickup → backend response fields (tests green) → regenerate contract → grouped UI → More for you actions → Cog/Gauge/review/ship**. No dependency cycle.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: After plan challenge and provisional clearance, produce/open the Spec Kit PR, obtain owner checkbox decision, then confirm Gate 1 merge before Build work.
**Checkpoint**: Plan challenge cleared; Patron `gate1: provisional` set; Spec PR opened by Rigger under Bernstein coordination; owner checkbox resolved; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [x] T000 After plan challenge clearance and Patron `gate1: provisional`: **Bernstein** coordinates and **Rigger** prepares/opens the DEV-538 Spec Kit PR carrying the owner structural checkbox from `spec.md`; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-538 in `specs/DEV-538/CONCLUSIONS.md`. Receipt only. T001–T016 MUST NOT proceed without verified merge (`brief.md` Owner decision; Q8; `task-pipeline` Phase 2 steps 6–7).

---

## Phase 1: Pickup prerequisite confirmation and before screenshots (blocks production edits)

**Purpose**: Wisp evidence + Keel pickup analysis of landed DEV-541/542/543/517 contracts, then before screenshots, before production edits.
**Independent Test**: Written Wisp pickup receipt cites the landed contracts against HEAD; Keel read-only pickup analysis cites that evidence; desktop and phone before screenshots exist with recorded paths.
**Order**: T001 after T000. **STOP** on failed checkpoint (`brief.md` Plan step 1).

- [ ] T001 After T000: **Wisp** gathers pickup evidence in the Build worktree for landed DEV-541 paging/exclusions (`{ items, hasMore }`), DEV-542 seed/evidence, DEV-543 strong-match selection, and DEV-517 Load more/Swap request semantics (including follow-up lifecycle and Swap eligibility prerequisites) against pickup HEAD. Cite the exact existing liked/watched/watchlist-presence and watched-collection read paths and how T004 can reuse them without per-movie round trips. Verify DEV-542 supplies machine-readable best-seed movie ID and signal plus the title or a landed label source for both heading variants. Pin affected Core result construction sites and test builders, including Related. Record any prerequisite still unlanded at pickup. Then check `spec.md`/`plan.md` drift vs main; cite the For You handler, endpoint mapping, DTO files, `forYouApi.ts` paging/swap entry points, and `MovieCarousel`/`MovieCard` reuse seams into `recon-DEV-538`. **Keel** performs a strictly read-only pickup analysis based on that Wisp evidence before any production edits. After analysis clears, capture desktop and phone For You before screenshots; record paths, baseline build/ref, library fixture/state, and viewport widths. **STOP** `needs recon:` / Patron ruling if a required contract, seed identity/label, or sufficient existing read path cannot be established (FR-009–FR-010; Q7; `task-pipeline` Phase 3 step 2 read-only / recon ownership).

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt, Keel pickup analysis, and before screenshots on record.

---

## Phase 2: User Story 1 - For You response carries series and seed-presence signals (Priority: P1) 🎯 MVP

**Goal**: Handler computes `StartedCollectionId` and `HasSeeds`; API maps them on the existing For You GET (`HasSeeds` from Core result onto the page DTO); Related omits null `startedCollectionId`.
**Independent Test**: Unit + endpoint/integration coverage green for FR-001–FR-002 / SC-004 field semantics.
**Order**: T002–T005a after T001. Backend checkpoint MUST pass before Phase 3 regeneration.

### Tests for User Story 1

T002 and T003 may be authored red against Core/API result members that T004 and T005 introduce. Both suites must compile and pass before T005a.

- [x] T002 [P] [US1] **Build lane**. After T001: Extend `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` for watched vs unwatched vs absent collection, an excluded watched member and a disliked watched member qualifying their collections, and `HasSeeds` on empty eligibility after exclusions, on the ranking signal guard, and past the last offset. Include watchlist-only and playback-history-only fixtures for true and false respectively. Update `tests/LamuFlix.UnitTests/Features/Recommendations/ForYouRecommendationsPageTests.cs` for the Core envelope (FR-001–FR-002; SC-004).
- [x] T003 [P] [US1] **Build lane**. After T001: Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` for populated collection ID on the wire, omitted null `startedCollectionId` cases, both empty-page `hasSeeds` boolean values, required `hasSeeds` on successful For You JSON, and Related JSON unaffected / free of `startedCollectionId` when null (FR-001–FR-002; SC-004). Reserve OpenAPI assertions for schema/type shape in T006 (required boolean `hasSeeds`, optional nullable item `startedCollectionId`).

### Implementation for User Story 1

- [x] T004 [US1] **Build lane**. After T002–T003 are authored: In `GetForYouRecommendationsQueryHandler.cs`, use the existing read paths verified in T001 and reuse the browse pass where possible, with no new per-movie round trips. Qualify collections from library watched membership independently of candidate eligibility, dislike, and exclusions. Compute `HasSeeds` from liked/watched/watchlist presence, separately from `HasPositiveSignal` or its landed equivalent. Preserve eligibility, scoring, ordering, and paging. Carry the values through `ForYouRecommendationsPage.cs` and `RecommendedMovie.cs` and update affected existing construction sites/test builders only as needed. Supply the correct `HasSeeds` on all empty returns, including empty eligible candidates, the ranking signal guard, and offset past the end (FR-001–FR-002; Q7).
- [x] T005 [US1] **Build lane**. After T004: Update `ForYouRecommendationsPageDto.cs` with required `HasSeeds`, `RecommendedMovieDto.cs` with final positional member `int? StartedCollectionId = null` decorated `[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`. Retain the existing scoped ReSharper suppression. Update the `RecommendationEndpoints.cs` For You mapping to **map `HasSeeds` from the Core For You result onto the page DTO** and populate `StartedCollectionId` only on the For You projection (Related stays null/omitted on the wire). Make T002–T003 pass (FR-001–FR-002; Q7).
- [x] T005a [US1] **Gauge checkpoint**. After T005 and green T002–T003: Run and record `LamuFlix.ArchitectureTests` and the three static gates on this phase’s changed `.cs` (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`). Record the read-path review against T001, including confirmation that no per-movie round trips were added. Backend C# tasks clear the gate only after the checks pass (`constitution.md:511,773-775,801-817`).

**Checkpoint**: US1 independently testable via unit + HTTP integration **green**, and Phase 2 ArchitectureTests + three static gates pass, before contract regeneration. Do not start T006 until T002–T005a pass.

---

## Phase 3: Contract chain regeneration (blocks web grouping)

**Goal**: OpenAPI snapshot, `openapi.json`, `types.ts`, and MSW handlers expose `hasSeeds` and optional `startedCollectionId`.
**Independent Test**: OpenAPI/web contract checks show the new schema/type shape; generated TypeScript consumed by later web tasks.
**Order**: T006 → T007 after Phase 2 checkpoint (backend tests + Phase 2 ArchitectureTests/static gates green).

- [ ] T006 [US1] **Build lane**. After Phase 2 checkpoint: Refresh `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` via the existing OpenAPI regeneration path (`LAMUFLIX_REGENERATE_OPENAPI` or project-standard equivalent) and review the diff for For You **schema/type shape only** — required boolean `hasSeeds`, optional nullable item `startedCollectionId` (FR-009; SC-004). Related JSON omission and populated-value / omitted-null / empty-page boolean behaviour stay in T003 HTTP tests — OpenAPI does not assert them.
- [ ] T007 [US1] **Build lane**. After T006: Run `npm run generate:api` in `web/` to regenerate `web/src/api/openapi.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts`. Update the hand-maintained `web/src/mocks/fixtures.ts` and `web/src/test/handlers.ts` for the new For You envelope. Extend `web/src/features/for-you/forYouApi.test.ts` and `web/src/mocks/forYou.test.ts` for the envelope and optional item field. No hand-edits to generated outputs (FR-009; SC-004).

**Checkpoint**: Web types available; grouping work may begin.

---

## Phase 4: User Stories 2–4 - Grouped rows, card chrome, empty states (Priority: P1)

**Goal**: Client groups the initial page into ordered rows with caps/folding; carousels + hidden seed lines; dual empty states; loading/error retained.
**Independent Test**: `ForYouPage.test.tsx` / `MovieCard.test.tsx` cover Done when behaviours for US2–US4; one initial GET asserted.
**Order**: T008 → T009 → T010 → T011 after T007 (sequential authorship; shared `ForYouPage.test.tsx` is never `[P]`).

### Tests for User Stories 2–4

- [ ] T008 [US2] **Build lane**. After T007: Extend `web/src/features/for-you/ForYouPage.test.tsx` for Wanted row order with series-assignment priority, empty-row omission, series selection with known years, null/omitted years, both years unknown, and equal years where API rank and movie ID order differ; duplicate initial movie IDs assigned once using their first API occurrence; best-seed uniqueness after series selection, seed groups by seed ID plus reported signal (liked vs watched), seed-group count/rank ties, retained API order within seed/watchlist rows and More for you including folded overflow, singleton folding, overflow past four/six caps into More for you, caps defined in one file, and exactly one initial For You GET (FR-003; SC-001–SC-002; Done when; `brief.md` Q3).
- [ ] T009 [US3] **Build lane**. After T008: Extend `ForYouPage.test.tsx` and `web/src/library/MovieCard.test.tsx` for labelled regions, `h2` headings, carousel arrow-key/Previous/Next behaviour (reuse `MovieCarousel` expectations), hidden seed lines with retained evidence on For You row cards, and unchanged default seed-line behaviour for other callers (FR-007–FR-008; SC-002).
- [ ] T010 [US4] **Build lane**. After T009: Extend `ForYouPage.test.tsx` for 6a and 6b empty copy + “Browse the library” link on a successful initial empty GET, including a zero-item response carrying `hasMore: true` that still shows the appropriate empty state; retain loading and error/Retry coverage (FR-006; SC-003; SMOKE-002–003). Empty follow-up regressions for Load more and Swap belong in T012/T013, not here.

### Implementation for User Stories 2–4

- [ ] T011 [US2] [US3] [US4] **Build lane**. After T008–T010 are authored: Implement pure grouping + single-file caps colocated with `web/src/features/for-you/` per brief Q3 (series priority; seed ID + signal groups; API order in watchlist/More including folded overflow); update `ForYouPage.tsx` / `ForYouPage.css` / `forYouApi.ts` boundaries; render `MovieCarousel` per populated row with labelled region + `h2`; add `MovieCard` seed-line opt-out prop (default unchanged); implement Q6 empty states; keep loading/error/Retry and `/for-you` route/nav unchanged. Make T008–T010 pass without depending on More for you follow-up wiring (FR-003, FR-006–FR-008; Q3, Q5–Q7).

**Checkpoint**: US2–US4 independently demonstrable on fixed fixtures without More for you follow-up actions.

---

## Phase 5: User Story 5 - More for you Load more and Swap (Priority: P2)

**Goal**: DEV-517 controls on More for you only; themed rows frozen; cross-row dedup; zero-card+`hasMore` reachable Load more with `h2`; Swap only under DEV-517 prerequisites.
**Independent Test**: Component tests for More for you-only controls, zero-card+`hasMore`+`h2`, Swap eligibility, dedup, stable themed rows, empty Load more and empty Swap follow-ups.
**Order**: T012 → T013 after T011.

### Tests for User Story 5

- [ ] T012 [US5] **Build lane**. After T011: Extend `ForYouPage.test.tsx` for Load more/Swap appearing only on More for you, a nonempty initial page with zero More cards and `hasMore` showing labelled region **including its `h2`** + Load more and mounting carousel at one card, Swap offered only when existing DEV-517 prerequisites hold and omitted otherwise, cross-row deduplication, rapid repeated or mixed Load more/Swap attempts with a pending response (only one action request and no duplicate or overwritten cards), obsolete response after a page reset, stable themed rows across actions, empty Load more follow-up and empty Swap follow-up each preserving cards without rule 6 empty states, and omission when More for you has neither cards nor controls (FR-004–FR-005; SC-002; Q4–Q5).

### Implementation for User Story 5

- [ ] T013 [US5] **Build lane**. After T012 is authored: Wire DEV-517 Load more/Swap into More for you only using established request semantics and Swap eligibility prerequisites; exclude/dedupe against all rendered movie IDs; freeze themed rows from the initial GET; use one shared in-flight guard for both controls, suppress duplicate dispatch until settlement, and ignore obsolete responses after reset; implement zero-card+`hasMore` with region `h2` and omit-empty-section behaviour. Make T012 pass (FR-004–FR-005; Q4–Q5).

**Checkpoint**: US5 independently testable; themed rows unchanged by More actions.

---

## Phase 6: Polish — refactor, gates, Size L review, ship (Priority: P2)

**Purpose**: Cog refactor, Gauge full gate matrix (re-run ArchitectureTests/static gates after refactor; always run mutation whenever `src/` production C# changed), Size L Phase 4 review/remediation, Phase 5 ship/merge-bar, screenshot read-back, smoke dispositions (`task-pipeline:89-139`). Phase 2 already ran ArchitectureTests + three static gates for backend changed `.cs`; T014 does not own that first pass.
**Order**: T014 → T016 after T013.

- [ ] T014 After T013: Use existing Vitest/RTL example coverage for client grouping determinism. Capture desktop and phone after screenshots matched to T001 before shots; record paths. **Cog** runs `/refactor` once over `<base>...HEAD` (complexity `-Threshold 6`). **Gauge** then runs and records the Size L gate matrix (`task-pipeline:98-112`): re-run `LamuFlix.ArchitectureTests` and the three static gates after refactor (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1` incl. refactor threshold, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`); **always** run `scripts/run-mutation.ps1` without `--since` whenever production C# under `src/` changed — do not pre-decide eligibility; the script decides applicability (exit 0 = PASSED; exit 1 = FAILED blocking; exit 2 `NOT APPLICABLE` or `SKIPPED (scope-empty)` non-blocking, never PASS); `scripts/run-property-tests.ps1` (exit 2 accepted only if the task note records `propertyTests: opt-out — <reason>`); `scripts/run-vulnerable-packages.ps1`; `dotnet format --verify-no-changes`; `dotnet test`; `scripts/run-web-gates.ps1` (exit 0 if `/web` changed; exit 2 = `SKIPPED (scope-empty)` otherwise). Record honest verdicts only (FR-010; SC-005; constitution:511,773-775,801-817).
- [ ] T015 After T014: Size L Phase 4 (`task-pipeline:116-125`): **Gauge** pre-pass artifact; **Sentry** (Risk+Security), **Ledger** (Standards), and **Compass** (Spec) axis reports required; **Keel** adjudicates (`pr-review/findings@1`) under the written closing bar — High/Medium and accepted in-scope Low closed with evidence; remaining Lows explicitly disposed; two review rounds max; **Cog** remediates (<= 2 fix commits per round); escalate unresolved in-scope findings to the owner on the PR after the cap (`brief.md` Q8; SC-006).
- [ ] T016 After T015: Size L Phase 5 ship (`task-pipeline:127-141`): **Rigger** runs `scripts/rebase-task-branch.ps1 -Push`; **Gauge** re-runs Phase 3 gates on the rebased head; L `/ship-review` split runs; **Quill** drafts `pr-body-DEV-538` outside the worktree; **Rigger** publishes the delivery PR; **Keel** verifies the merge bar on that PR and posts it. Attach and read back desktop and phone before-and-after screenshot pairs. Explicitly execute or record the disposition of each required smoke scenario (SMOKE-001, SMOKE-002, SMOKE-003). Claim merge-bar readiness only from recorded gate/review evidence. User merges (FR-010; SC-005–SC-006).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all Build work until Gate 1 merge receipt (including owner checkbox); Spec PR opens only after plan challenge + provisional.
- **Phase 1**: Blocks production edits until Wisp evidence, Keel pickup analysis, and before screenshots exist.
- **Phase 2**: Backend fields + green unit/HTTP + ArchitectureTests/three static gates on Phase 2 changed `.cs` before contract regeneration (gate run is scheduled here, not deferred to T014).
- **Phase 3**: Blocks web grouping until generated types exist.
- **Phase 4**: Grouping/chrome/empty states before More for you action wiring (T011 must pass without follow-up wiring; T013 depends on T011).
- **Phase 5**: Depends on Phase 4.
- **Phase 6**: Depends on Phase 5; Cog refactor + Gauge gates before Phase 4 axes; Phase 5 ship after review closes.

### User Story Dependencies

- **US1 (P1)**: After Phase 1 — no dependency on UI stories.
- **US2–US4 (P1)**: After US1 contract regeneration.
- **US5 (P2)**: After US2 themed/More buckets exist.
- **Verification (P2)**: After US1–US5.

### Parallel Opportunities

- T002/T003 after T001 (different test projects/files; may be authored red against the new result members).
- T008 → T009 → T010 are sequential (shared `ForYouPage.test.tsx`; no `[P]`).
- Screenshot capture and gate scripts are not `[P]` with each other when they share the working tree state under review.

### Parallel example: User Story 1 tests

```text
Task: "Extend GetForYouRecommendationsQueryHandlerTests.cs for StartedCollectionId / HasSeeds"
Task: "Extend RecommendationEndpointTests.cs for populated ID / omitted null / empty-page hasSeeds / Related"
```

---

## Implementation Strategy

### MVP First (US1 + minimal grouping proof)

1. Complete Phase 0–1 (plan challenge → provisional → Spec PR → Wisp/Keel pickup).
2. Deliver US1 fields + green backend checkpoint + regenerated contract.
3. Deliver US2–US4 grouped UI with component proof (no follow-up wiring required).
4. **STOP and VALIDATE** against Done when component criteria before US5 polish.

### Incremental Delivery

1. Gate 1 → Wisp/Keel pickup → backend fields → contract.
2. Grouped rows + empty states → demoable For You.
3. More for you Load more/Swap → full Wanted rule 5.
4. Cog refactor → Gauge full matrix (mutation whenever `src/` C# changed) → Size L review/remediate → rebase/ship-review/PR/merge-bar → smoke dispositions.

### Notes

- `[P]` tasks = different files, no incomplete deps. Shared `ForYouPage.test.tsx` tasks are never `[P]`.
- Owner checkbox failure (reject amendment / drop 1a or 6 dual states) requires Patron/user disposition before Build; do not silently drop Wanted behaviours. Keep the owner checkbox open until the user answers.
- Avoid: vague tasks, same-file false `[P]`, lowering thresholds, opening Spec PR before provisional clearance.
