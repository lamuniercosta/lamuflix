---

description: "Task list for DEV-541 deterministic For-You ranking and paging"
---

# Tasks: Deterministic For-You ranking and paging

**Input**: Design documents — public `specs/DEV-541/spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q10), `CONCLUSIONS.md`; Maestri notes `DEV-541`, `recon-DEV-541`; `specs/PRODUCT.md`.

**Status**: PLAN FROZEN · Phase 2 step 6 complete. See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md). Gate 1 verified. Build and runtime acceptance are pending.

**Tests**: Required (`brief.md` Acceptance evidence; FR-001–FR-012; SC-001–SC-006; SMOKE-001). TDD optional. Use existing unit/integration/OpenAPI/web test homes. No new test framework/dependency. Never mock the data-access driver for behaviour that depends on real translation.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Follow frozen Q7 order (caller recon → Core → API → OpenAPI/web → verification). T001 completion is a prerequisite for Core and every later Build phase. STOP for cited Patron ruling before any §2.3-protected change outside Q6 mechanical unwrap authorization. STOP `needs recon` / `needs decision` if a required fact/decision is missing. No DEV-517/538/542/543 behaviour; no movie-specific response-shape change; no new dependencies; never lower harness thresholds. Otherwise-unassigned implementation tasks (T002–T015) are owned by the generic Build lane / Anvil. T016 and T017 yield to named `task-pipeline` owners (Gauge; Phase 4 axes + Keel; Phase 5 merge bar separate).

**Pipeline note**: Local Phase 0–N headings are for task dispatch. Stage 10 refers to the canonical implementation PR step.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp caller recon confirmation) — prerequisite for Core and every later phase
Phase 2   T002 → T006   (US1 Core options/ranking/caps + unit tests) — after T001
Phase 3   T007/T008 → T009 → T004a → T010   (query paging/exclusions + exclusion regressions + HTTP boundary) — after Phase 2
Phase 4   T011 → T014   (US4 OpenAPI snapshot, generated contract, mocks, confirmed caller unwrap) — after API
Phase 5   T015 → T017   (focused verification + Size M gates + review bar + SMOKE disposition) — after OpenAPI/web
```

Ordering per brief Q7 (`brief.md` Task order; Q7): **Gate 1 → T001 → Core (T002–T006) → query and API (T007–T010, including T004a after T009) → OpenAPI/web (T011–T014) → verification (T015–T017)**. T001 must complete before Core starts. T004a carries the two US1 assertions that require the query exclusion contract (brief Q3–Q4). Web migration (T013–T014) also uses the T001 file list and pickup deltas.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [X] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-541 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-541 in `specs/DEV-541/CONCLUSIONS.md`. Receipt only. T001–T017 MUST NOT proceed without verified merge (brief; Q7/Q10).

---

## Phase 1: Caller recon confirmation (prerequisite for Core+)

**Purpose**: Complete the caller inventory so mechanical unwrap edits name exact files (`brief.md` Task order step 1; Q6/Q7).
**Independent Test**: Written recon confirms the challenge baseline against pickup HEAD and records exact file paths for the mechanical migration and any pickup deltas.
**Order**: T001 after T000; blocks T002+.

- [X] T001 **Wisp**. Confirm the Sentry F1 and Compass F3 baseline against pickup HEAD. Review `web/src/features/for-you/forYouApi.ts`, `forYouApi.test.ts`, `ForYouPage.tsx`, `ForYouPage.test.tsx`, and `web/src/mocks/forYou.test.ts` with the fixture in `web/src/mocks/fixtures.ts`. Record exact compatibility edits, callers needing verification only, and evidence for any deltas in `recon-DEV-541`. The earlier recon could not enumerate the callers. **STOP** `needs recon:` if the pickup inventory cannot be established before Core proceeds (Q6/Q7).

**Checkpoint**: T002+ unlock only after T001 completes. T013/T014 use the confirmed file list and pickup deltas.

---

## Phase 2: User Story 1 - Capped deterministic For-You ranking (Priority: P1) 🎯 MVP

**Goal**: Options, collection release-order cut, and capped MMR selection produce one deterministic full ranking.
**Independent Test / coverage labels**: Existing unit suites prove default caps, standalones, release-year/null/tie order, seed-saturation non-backfill, movie-ID ties, and Q5 beyond-`CandidatePoolSize` selection with take still bounded (FR-001–FR-005; SC-001/SC-003).
**Order**: T002–T006 after T001. T004 runs against the T005–T006 ranking implementation at the Phase 2 checkpoint.

### Tests for User Story 1

- [X] T002 [P] [US1] **Build lane**. Extend `tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs` (and related existing options tests as needed) to verify best-seed cap 3, `MaxPerCollection` 1, and `MaxExcludeCount` 50 matching default `CandidatePoolSize`. Verify each cap accepts 1 and rejects 0 and the exclude limit accepts 0 and rejects -1. Exercise the existing data-annotation / `ValidateOnStart` path for the new options (FR-004/FR-008, constitution VII).
- [X] T003 [P] [US1] **Build lane**. Extend `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorTests.cs` and `MmrSelectorSequelChainTests.cs` for whole-ranking seed/collection caps, the cap boundary including the last eligible and first skipped candidate, standalone seed-only consumption, and ascending movie-ID tie-break with caps active (FR-001/FR-004, SC-003).
- [X] T004 [P] [US1] **Build lane**. Extend `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesMapperTests.cs` and `GetForYouRecommendationsQueryHandlerTests.cs` for collection release-year cut (null last, movie-ID ties), non-backfill under seed saturation, and **Q5 regression: a result beyond `CandidatePoolSize` in the eligible input can be selected while `take` remains bounded by `CandidatePoolSize`** (FR-002/FR-003/FR-005; SC-001).

### Implementation for User Story 1

- [X] T005 [US1] **Build lane**. Update `src/LamuFlix.Core/Options/RecommendationOptions.cs` with typed best-seed cap, `MaxPerCollection` and `MaxExcludeCount`. Use `const` defaults of 3, 1 and 50 respectively and avoid mutation-sensitive static initialization. Add range annotations for ranking caps ≥ 1 and raw exclude limit ≥ 0 under existing `ValidateOnStart()` registration. `CandidatePoolSize` bounds take only for For-You (FR-004/FR-005/FR-008, constitution VII).
- [X] T006 [US1] **Build lane**. Implement collection cut and capped selection across `GetForYouRecommendationsQueryHandler.cs`, `MmrSelector.cs`, and `MovieFeaturesMapper.cs` (extend `MmrCandidate.cs` only if required). Preserve existing strong-match eligibility. Ensure identical inputs yield a stable full ranking before paging (FR-001–FR-005). Make T002–T004 assertions pass.

**Checkpoint**: T002–T004 pass against the T005–T006 implementation. Start Phase 3 once this ranking checkpoint is met. The US1 exclusion regressions are required at the Phase 3 checkpoint.

---

## Phase 3: User Stories 2–3 - Paging, exclusions, HTTP envelope (Priority: P1)

**Goal**: Query carries offset/exclusions; handler slices with overflow-safe hasMore; endpoint validates inputs at the HTTP boundary and returns `{ items, hasMore }` with ProblemDetails+`traceId` on validation failures.
**Independent Test / coverage labels**: Unit + `RecommendationEndpointTests.cs` prove exclusion-driven promotion, unknown-valid-ID no-effect, slices, hasMore boundaries, empty/beyond-end, large offset, duplicate/raw-limit exclude HTTP cases, invalid/nonpositive exclude, malformed/negative/repeated offset 400 with ProblemDetails+`traceId`, nonzero valid offset dispatch, deduplicated query dispatch, defaults, wrapped JSON, and movie-specific endpoint unchanged (FR-006–FR-009; SC-002/SC-004).
**Order**: Start T007–T008 after the Phase 2 checkpoint. Complete T009, then pass T004a before T010. T004a follows T007 and T009 because they also edit the handler suite or supply its query contract.

### Tests for User Stories 2–3

- [ ] T007 [P] [US2] **Build lane**. Extend `GetForYouRecommendationsQueryTests.cs`, `GetForYouRecommendationsQueryHandlerTests.cs`, and `GetForYouRecommendationsQueryValidatorTests.cs` for normalized offset default/bounds, deduplicated exclusion-set semantics, stable disjoint slices, both exact hasMore boundaries, empty/beyond-end pages and caps across slices. Prove a huge valid offset returns an empty page with false hasMore before MMR and match a bounded-prefix run against the same inputs' full capped ranking, including cap skips and movie-ID ties. Raw count enforcement belongs to T010 at the HTTP boundary (FR-006–FR-008, Q8, SC-002).
- [ ] T008 [US3] **Build lane**. Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` with defaults, nonzero valid offset dispatch, wrapped JSON, a huge valid offset returning an empty page and false hasMore, accepted duplicate excludes with deduplicated dispatch, and the raw limit with repeated duplicates. Cover invalid, empty, overflowing and nonpositive exclude values and malformed, negative, overflowing and repeated offsets returning HTTP 400. Assert RFC 7807 ProblemDetails with `traceId` and a static error response that omits a distinctive malformed input. Preserve the movie-specific bare-list response assertions (FR-007–FR-009, SC-004, constitution V).

### Implementation for User Stories 2–3

- [ ] T009 [US2] **Build lane**. Add `src/LamuFlix.Core/Features/Recommendations/ForYouRecommendationsPage.cs` as the Core page result. Update the query, handler and existing `HandlerRegistration.cs` binding to use it. Add offset and exclusions to the query and validate normalized invariants, including nonnegative offset, in `GetForYouRecommendationsQueryValidator.cs`. Filter excluded IDs before the collection release-year cut and MMR (brief Q3–Q4). If offset reaches the eligible post-cut count, return an empty page with false hasMore before MMR. Otherwise select up to `offset + take + 1` using widened arithmetic and clamping to that count. Slice `[offset, offset + take)` and set hasMore from selected index `offset + take`. `CandidatePoolSize` remains the take ceiling (FR-005–FR-008).
- [ ] T004a [US1] **Build lane**. After T009, extend `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` through the Core query exclusion contract. Prove that excluding the earliest eligible collection member promotes the next eligible member and that unknown valid movie IDs have no effect on the result. Pass both regressions before T010 (FR-002/FR-003, brief Q3–Q4, SC-001).
- [ ] T010 [US3] **Build lane**. In `RecommendationEndpoints.cs` enforce the existing take rules and ParseTake pattern for offset. Missing offset defaults to zero. Reject multiple values, parse failures and negatives before dispatch. Count raw repeated exclude values before deduplication at the HTTP boundary. Parse each with `NumberStyles.Integer` and `CultureInfo.InvariantCulture` before `MovieId.TryCreate`, then deduplicate valid IDs. Use `BadHttpRequestException` and static error messages for 400 ProblemDetails with `traceId`. Map to the For-You-only envelope using `src/LamuFlix.Api/Endpoints/ForYouRecommendationsPageDto.cs` with the existing item DTO. Make T007–T008 pass (FR-007–FR-009, constitution V).

**Checkpoint**: T004a passes and US2/US3 are independently testable via unit + HTTP integration.

---

## Phase 4: User Story 4 - Contract chain and mechanical web adaptation (Priority: P2)

**Goal**: OpenAPI, generated TypeScript, regenerated handlers, mock fixtures and confirmed callers agree on the envelope while the existing hook returns the items array.
**Independent Test / coverage labels**: OpenAPI snapshot/contract green with discoverable public `offset` and repeated `exclude` plus `{ items, hasMore }`; movie-specific contract preserved; web build/test gate green with unwrap where required (FR-010–FR-011; SC-005).
**Order**: T011 → T012 → T013 → T014 after T010. T001 precedes Core and supplies inventory for T013/T014.

### Implementation / contract for User Story 4

- [ ] T011 [US4] **Build lane**. Update `RecommendationsOperationTransformer.cs` to advertise the page envelope, public offset and an optional query exclude parameter with `style: form`, `explode: true`, array schema and integer items. Refresh `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` and review the diff for the expected For-You changes and the preserved movie-specific bare-list contract before accepting the baseline (FR-010, SC-005).
- [ ] T012 [US4] **Build lane**. Regenerate `web/src/api/openapi.json`, `web/src/api/types.ts` and `web/src/mocks/handlers.ts` through the existing generation path, including `npm run generate:api`. Check generated envelope and input types, including an integer array input for exclude, and the movie-specific operation. Generated outputs come exclusively from the generator (FR-010, SC-005).
- [ ] T013 [US4] **Build lane**. In `web/src/mocks/fixtures.ts`, migrate `forYouRecommendationsData` and `success.GetForYouRecommendations` to `{ items, hasMore }` and type the fixture against the generated page schema. T012 owns generated handlers (FR-010/FR-011).
- [ ] T014 [US4] **Build lane**. After T013, in `web/src/features/for-you/forYouApi.ts` unwrap `data.items` in the query function and retain the take-only, array-returning hook and current query key. Verify `ForYouPage.tsx` still renders the array and adapt the wire fixtures and assertions in `forYouApi.test.ts`, `ForYouPage.test.tsx` and `web/src/mocks/forYou.test.ts`. In the web contract test, exercise the real generated client with two distinct IDs and assert the request sends two `exclude` keys. Adapt any T001 deltas mechanically. DEV-517 owns UI use of `hasMore`. **STOP** `needs recon:` if the T001 pickup inventory is unavailable (FR-011, Q6).

**Checkpoint**: US4 independently testable via OpenAPI + web gate.

---

## Phase 5: Verification, gates, and review bar (Priority: P2)

**Goal**: Focused acceptance evidence + configured Size M gate bar (Gauge) + Phase 4 review bar + independent SMOKE disposition (Q8–Q10). Honest labels alone do not establish readiness.
**Independent Test**: Focused suites green; each Phase 3 step 5 gate meets the configured bar (Failure / Could not run block; only documented non-blocking exit-2 accepted); Phase 4 three-axis reports + Keel disposition; SMOKE-001 evidence or explicit pending stated independently.
**Order**: T015 after T014; T016 after T015; T017 after T016. Phase 5 final PR merge bar and user merge remain separate (`task-pipeline` Phase 5).

- [ ] T015 **Build lane**. Run and record the focused unit suites (options, mapper, MMR, For-You query/handler/validator), `RecommendationEndpointTests`, and focused web contract/caller tests. Include cap/limit boundaries, huge-offset short-circuit and HTTP empty envelope, prefix equivalence, hasMore boundaries, unknown-ID and beyond-pool cases, duplicate/raw-limit HTTP, static ProblemDetails with `traceId`, and nonzero offset dispatch. Against a running API with two known eligible IDs in the baseline response, exercise the generated client with both IDs as an exclude array. Record repeated keys on the wire, HTTP 200, and both IDs filtered from the returned page. This evidence is independent of SMOKE-001 (FR-012, SC-006).
- [ ] T016 **Gauge** (`task-pipeline` Phase 3 step 5). Run the full configured Size M gate matrix owned by Gauge: `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1` (refactor threshold when required), `run-jetbrains-inspectcode.ps1` (no `-All`), `run-mutation.ps1` when production C# under `src/` changed, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, OpenAPI snapshot/contract checks, and `run-web-gates.ps1` when `/web` changed. Record each actual exit. **Readiness requires the configured bar**, not honest reporting alone: Failure (exit 1) and Could not run block; only documented non-blocking exit-2 cases are accepted (scope-empty `SKIPPED`, mutation `NOT APPLICABLE` / N/A, harness-disabled `SKIP`, recorded property-test opt-out). Never fold non-pass into PASS; never lower thresholds (FR-012; Q8; plan Verification Approach).
- [ ] T017 **Phase 4 owners** (`task-pipeline` Phase 4): **Gauge** pre-pass; **Sentry** (Risk), **Ledger** (Standards), and **Compass** (Spec) axis reports required; **Keel** adjudicates disposition. Close every accepted in-scope finding including Low; record explicit rationale for other dispositions; stop after two rounds and route any third-round findings to user review on the PR (Q9–Q10). For **SMOKE-001**, either publish required Compose/runtime evidence that the visible For You list shows capped diversity, or record an explicit pending / Could not run disposition with reason — smoke pending is independent of gate and review readiness (plan Verification Approach). Do not treat implementation-PR merge as a gate/review prerequisite. The separate final PR merge bar and user merge are `task-pipeline` Phase 5 (Keel verifies merge bar on the opened PR; user merges) and are not part of T016/T017 readiness.

**Checkpoint**: Gate readiness is met only when T016 satisfies the configured Phase 3 step 5 bar (Failure / Could not run block; documented non-blocking exit-2 only). Review readiness is met only when T017 has the three axis reports and Keel disposition. SMOKE-001 may be Pass or explicitly pending independently. Final PR merge bar and user merge remain `task-pipeline` Phase 5 and are not required to record those dispositions.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work.
- **Phase 1 (T001)**: Prerequisite for Phase 2 (Core) and every later phase; also supplies the confirmed consumer file list and pickup deltas for T013–T014.
- **Phase 2 (US1)**: Depends on T001; blocks Phase 3.
- **Phase 3 (US2/US3)**: Depends on Phase 2; blocks Phase 4 contract regeneration that assumes envelope.
- **Phase 4 (US4)**: Depends on Phase 3; consumer unwrap uses T001 inventory (already complete before Core).
- **Phase 5**: Depends on Phase 4.

### User Story Dependencies

- **US1 (P1)**: Ranking work follows T001. Its exclusion regressions in T004a require the Core query contract in T009.
- **US2/US3 (P1)**: Query work follows the Phase 2 ranking checkpoint. T010 follows T009 and T004a.
- **US4 (P2)**: After US3 envelope exists; web unwrap only for T001-recorded files plus brief-named mocks.

### Parallel Opportunities

- T002, T003, T004 may run in parallel (different test files) after T001, before/alongside T005–T006 as long as assertions land against the final Core behaviour.
- T007 may proceed in parallel with early T008 scaffolding after the Phase 2 checkpoint. Run T004a after T009 and before T010.
- T011 contract work precedes T012 generation, T013 fixture migration, and T014 caller and test adaptation.

### Parallel Example: User Story 1 tests

```bash
Task: "Extend RecommendationOptionsTests for cap defaults and ValidateOnStart knobs"
Task: "Extend MmrSelectorTests for whole-ranking caps"
Task: "Extend handler/mapper tests for collection cut, seed-saturation non-backfill, and Q5 beyond-pool selection"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T000 Gate 1
2. Complete T001 caller recon (prerequisite for Core)
3. Complete US1 Core ranking/caps
4. **STOP and VALIDATE** unit evidence for deterministic capped ranking (including the Q5 regression)
5. Add the Core query contract (T009), pass the US1 exclusion regressions (T004a), then implement the HTTP boundary (T010) before contract migration

### Incremental Delivery

1. T001 recon → inventory recorded
2. US1 ranking → unit green
3. Core query paging/exclusions → US1 exclusion regressions green → HTTP envelope → integration green (ProblemDetails+`traceId` included)
4. US4 contract/mocks/callers → web gate green
5. T015 focused evidence → T016 Gauge configured gate bar → T017 Phase 4 axes + Keel disposition + independent SMOKE pending/evidence → separate Phase 5 merge bar and user merge

### Notes

- [P] tasks = different files, no incomplete deps
- Quill invents no seat names beyond Bernstein/Rigger/Keel/Wisp where the brief/task-pipeline already assigns them
- Avoid DEV-517 UI; avoid hand-editing generated OpenAPI/TS except via regeneration
- Do not invent named web callers; retain brief-named mocks and the Wisp T001 handoff
- Commit message format when Build commits: `DEV-541 - {subject}`
