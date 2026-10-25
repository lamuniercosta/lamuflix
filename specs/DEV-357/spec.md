# Feature Specification: GET /api/recommendations/for-you from taste profile

**Feature Branch**: `feature/DEV-357`

**Created**: 2026-10-22

**Updated**: 2026-10-24 — aligned to Phase B pickup decision amendment in `brief.md` / `CONCLUSIONS.md` (HEAD `0607eb7`)

**Status**: Draft (grill closed at Q9; Phase B technical readiness APPROVED WITH AMENDMENTS FOR QUILL ALIGNMENT at HEAD `0607eb7`; plan freeze, Gate 1, analysis clearance, and source-implementation authorization remain separate and unclaimed)

**Input**: Closed decision brief at `specs/DEV-357/brief.md` (Q1–Q9 plus Phase B pickup amendment 2026-10-24); rulings in `CONCLUSIONS.md`. Authoritative ticket Scope & Technical Design and acceptance text as recorded in brief.md:15-28 (decision authority; not inherited recon note line numbers). Evidence worktree `F:/Dev/LamuFlix.worktrees/DEV-357`; branch `feature/DEV-357`; Phase A base `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`; current source HEAD `0607eb7ea8a4ec48ffffa933767568a87eb8df30`. Runtime facts: recon-DEV-357 and recon-DEV-357-2 Sections 3–4 only.

**Controlling closure**: brief.md Q9 closes the grill for drafting. The Phase B amendment establishes technical dependency readiness and the acquisition/MMR approach refinements Quill must encode here; it does not claim plan freeze, Gate 1, gate1: provisional, runtime acceptance, test/gate pass, or merge permission.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Personalized for-you ranking with Personal reasons (Priority: P1)

When the library has positive watch history, a client requests personalized recommendations and receives a ranked list built from the cached taste profile, blend scoring across the eligible library, and MMR diversification, with Personal reasons where the blend contract emits them.

**Why this priority**: Delivers the ticket’s primary acceptance path (brief.md:24-28) and the mandated TasteProfile / cache / blend / MMR integration (brief.md:17-22; brief Q1).

**Independent Test**: With a positive-match history fixture and profile weighting enabled, `GET /api/recommendations/for-you` returns 200 with ranked `RecommendedMovieDto` items carrying typed lowercase `{kind,text}` reasons including Personal where DEV-354 emits them, in final MMR selection order.

**Acceptance Scenarios**:

1. **Given** positive history (at least one `PlaybackEvent` OR at least one `IsWatched=true` movie — expressed via non-empty `RecommendationProfileInputs.Movies` where adapter rows represent `IsWatched || launchedIds.Contains(Id)`) and a positive-match profile fixture with profile weighting enabled, **When** `GET /api/recommendations/for-you` runs with valid `take`, **Then** the response is 200 with ranked recommendations whose reasons include Personal explanations produced by the landed DEV-354 contract (brief Q5–Q6; Phase B amendment; brief.md:24-28; recon-DEV-357-2:41-43,88-93).
2. **Given** that history and eligible library candidates, **When** the handler orchestrates profile → blend → MMR → Take, **Then** candidates are acquired by composing existing ports only — page `IMovieCatalog.BrowseAsync(MovieQuery, ct)` (explicit valid non-null `Sort`/`Direction`, `Page.Number >= 1`, `Page.Size` within `1..100`, terminate on first short page `Items.Length < Size`), exclude `IsNotInterested` on summaries, load full domain rows via per-ID `IMovieRepository.GetAsync(id, ct)` (skip null misses; recheck `movie.IsNotInterested`), then one `IRecommendationProfileSource.GetProfileInputsAsync(allEligibleIds, ct)` for vectors and coherent history; blend uses literal DEV-354 `Score = a*S + b*P - c*W - d*N` with selected signal `S = 0`; no selected-movie seed, no `CandidatePoolSize` truncation of the candidate universe, and no new port/adapter (brief Q4; Phase B amendment; brief.md:17-22; recon-DEV-357-2:63-73,88-93).
3. **Given** blended results that may include negative finite scores, **When** MMR runs, **Then** at `MmrSelector.Select` input only the pool-wide nonnegative relevance offset `-Math.Min(0.0, minPoolScore)` is applied; selected `MovieId`s map back to original `BlendScorer` results so returned `Score` (including negative finite scores) and `Reasons` remain untouched; order is DEV-416 selection order (not a final score resort); score is not replaced by the MMR objective; uniqueness holds; size is `min(Take, eligible candidate count)`; ties follow deterministic movie-ID break (brief Q6; Phase B amendment; recon-DEV-357-2:26-36; specs/DEV-416/CONCLUSIONS.md:396-402).
4. **Given** the HTTP response, **When** Reasons are serialized, **Then** each reason is predecessor `{kind,text}` with lowercase `structural` / `personal`; no new DTO variant, reason kind, exporter, or invented reason template (brief Q2; recon-DEV-357:82,110-112).

---

### User Story 2 - Cold start returns empty list (Priority: P1)

When there is no positive watch history, the endpoint returns 200 with an empty list and does not run scoring or MMR.

**Why this priority**: Ticket Scope item 3 and acceptance require no-history 200 empty (brief.md:19,24-27); distinct from an empty calculated profile.

**Independent Test**: No-history and Not-Interested-only fixtures return 200 `[]` without invoking blend or MMR; a fixture with real history but empty features does **not** take the no-history path.

**Acceptance Scenarios**:

1. **Given** no positive history (`HasHistory` false: zero `PlaybackEvent` and zero `IsWatched=true` movies — equivalently `RecommendationProfileInputs.Movies` empty for the playback-or-watched rows), **When** `GET /api/recommendations/for-you` runs, **Then** the response is 200 with an empty list before scoring or MMR (brief Q5; Phase B amendment; brief.md:19,24-27).
2. **Given** Not Interested feedback alone and no playback/watched positive signal, **When** the endpoint runs, **Then** it is treated as no positive history and returns 200 `[]` (brief Q5).
3. **Given** real history that yields `TasteProfile.IsEmpty` or no positive comparable profile channel (or `BlendProfileWeight = 0`), **When** the endpoint runs, **Then** it does **not** treat that as no history; DEV-354 conditional Personal emission and Structural fallback apply; no fabricated Personal match (brief Q5–Q6).

---

### User Story 3 - take validation and OpenAPI contract (Priority: P1)

Clients may omit or supply `take`; invalid values are rejected as in DEV-341; the committed OpenAPI baseline and drift snapshot include the new route with no drift.

**Why this priority**: Ticket Scope items 1 and 4 plus acceptance require DEV-341 take behavior and OpenAPI no-drift (brief.md:17-22,24-28).

**Independent Test**: Omitted `take` defaults to constant 10 and remains range-validated; boundary and explicitly empty/malformed/repeated/out-of-range cases match DEV-341 parser 400 (distinct from query validation 422); omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400; opt-in paired OpenAPI regeneration then existing `generate:api` leave no drift against committed `openapi.json`.

**Acceptance Scenarios**:

1. **Given** omitted `take` (query key absent) with `CandidatePoolSize >= 10`, **When** the route runs, **Then** default is constant 10 via the existing DEV-341 `ParseTake` contract and that defaulted value is accepted (brief Q2; brief.md:44,112).
2. **Given** a single integer `take` in `1..CandidatePoolSize`, **When** the route runs, **Then** that value is accepted; `CandidatePoolSize` bounds take validation only and does not authorize truncating the candidate universe (brief Q2, Q4).
3. **Given** explicitly present but empty, whitespace-only, malformed, overflowing, repeated, noninteger, out-of-range, or otherwise invalid `take`, **When** the route parses input with the reused DEV-341 parser, **Then** it returns 400 ProblemDetails (`BadHttpRequestException` path). Absence of the `take` key alone is allowed; the effective default may still exceed the pool. **Given** omitted `take` with startup-valid `options.Take <= CandidatePoolSize < 10`, **When** the route runs, **Then** parser 400 ProblemDetails/`traceId` is returned because the defaulted constant 10 fails range validation. Direct FluentValidation failures on the query remain 422 and are distinct from parser 400 (brief Q2; brief.md:44,112; constitution Principle V).
4. **Given** the authorized route and DTO contract, **When** OpenAPI export and existing `web generate:api` complete through the existing opt-in writer then repository generator, **Then** `web/src/api/openapi.json` and the drift snapshot have no drift from the committed baseline, and both generator outputs `web/src/api/types.ts` and `web/src/mocks/handlers.ts` consume that contract (brief Q8; brief.md:83,93-100,122; recon-DEV-357:107-108; recon-DEV-357-2:79-81,96).

---

### Edge Cases

- No eligible candidates after Not Interested exclusion → 200 `[]` (brief Q6).
- Full-library candidate set larger than `CandidatePoolSize` → still page and score all eligible; only `take` is bounded by `CandidatePoolSize`; valid Browse pages require explicit sort/direction and size within `1..100`; no arbitrary candidate cap (brief Q4; Phase B amendment).
- Vectorless or unenriched eligible candidates → retain and score using landed empty-feature (`MovieFeaturesMapper.From(movie)` → `MovieFeatures.Empty`) and empty-embedding (`CandidateEmbeddings.GetValueOrDefault(id, ImmutableArray<float>.Empty)` → genre-only / `0.0` channel) fallbacks; do not exclude them merely for missing vectors. Corrupt vectors retain the landed scoring failure behavior; missing vectors take the established fallback (Phase B amendment; recon-DEV-357-2:67-72,92).
- Null `GetAsync` misses → skip that id; recheck `IsNotInterested` on loaded domain rows (Phase B amendment).
- Real history with empty features or `BlendProfileWeight = 0` → Structural fallback / no fabricated Personal; not the cold-start path (brief Q6).
- Playback-only history and watched-only history each satisfy `HasHistory` (brief Q5, Q8).
- Negative finite blend scores → preserved on the wire after selector-only nonnegative relevance rebase (Phase B amendment; brief Q6).
- Consistency: per-query read consistency plus DEV-415 cache invalidation; no atomic cross-query snapshot promise or concurrent-write completeness guarantee (Phase B amendment; specs/DEV-415/CONCLUSIONS.md:51).
- Cancellation must propagate through the three landed ports and handler orchestration (brief Q7–Q8; Phase B amendment).
- Existing per-movie recommendations route must continue to regress cleanly (brief Q8).
- At HEAD `0607eb7`, T001–T011 (and T012 / T013a) are technically satisfied per Patron Phase B amendment; Quill alignment of this spec/plan/tasks is required before Phase 2 source work. Plan freeze, Gate 1, and source-implementation authorization remain separate (brief.md Phase B amendment; recon-DEV-357-2:11-36,75-81,94-96).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose `GET /api/recommendations/for-you` with the same `take` validation as DEV-341: omitted `take` defaults to constant 10 and the effective value remains range-validated in `1..CandidatePoolSize`; a single integer in that range is accepted; explicitly empty, whitespace-only, malformed, overflowing, repeated, noninteger, or out-of-range `take` yields 400 ProblemDetails via the reused parser (`BadHttpRequestException`). Absence of the `take` key alone is allowed; when startup-valid `options.Take <= CandidatePoolSize < 10`, omitted `take` MUST yield parser 400 ProblemDetails/`traceId` because the defaulted constant exceeds the pool. Direct query FluentValidation failures remain 422 and MUST stay distinct from parser 400 (brief Q2; brief.md:44,112; DEV-357 note Scope item 1).
- **FR-002**: System MUST return HTTP 200 with an array of predecessor `RecommendedMovieDto` items; Reasons MUST be DEV-354 typed `{kind,text}` with lowercase `structural` / `personal`; MUST NOT introduce a parallel DTO, new reason kind, exporter, or invented reason template (brief Q2; brief.md:15-28; recon-DEV-357:82,110-112).
- **FR-003**: With positive history, system MUST orchestrate landed TasteProfile (DEV-353) via DEV-415 cache, score eligible library candidates with DEV-354 blend, apply DEV-416 MMR, then Take, preserving final MMR order (brief Q1, Q4, Q6; brief.md:17-22).
- **FR-004**: `HasHistory` MUST mean at least one recorded `PlaybackEvent` OR at least one `IsWatched=true` movie; Not Interested alone is not positive history. At the landed surface this MUST be evaluated from `RecommendationProfileInputs.Movies` (non-empty when adapter rows represent `IsWatched || launchedIds.Contains(Id)`), drawn from the same underlying history used for profile calculation via `GetProfileInputsAsync`. No history MUST return 200 `[]` before scoring or MMR. `TasteProfile.IsEmpty` MUST NOT establish no history (brief Q5; Phase B amendment; brief.md:19,24-27; recon-DEV-357-2:41-43,88-93).
- **FR-005**: Candidates MUST be all eligible library movies, excluding Not Interested before scoring, with no selected-movie candidate source, seed, pre-truncation, or refill. Acquisition MUST compose existing ports only: page `IMovieCatalog.BrowseAsync` until the first short page (explicit valid non-null `Sort`/`Direction`, `Page.Number >= 1`, `Page.Size` within `1..100`); exclude `IsNotInterested` on summaries; load domain rows via `IMovieRepository.GetAsync` (skip nulls; recheck Not Interested); one `IRecommendationProfileSource.GetProfileInputsAsync(allEligibleIds, ct)`. `CandidatePoolSize` MUST bound take validation only. MUST NOT add a port or adapter (brief Q4; Phase B amendment; brief.md:52-56; recon-DEV-357-2:63-73,88-93).
- **FR-006**: Blend MUST invoke the literal DEV-354 formula with selected signal `S = 0`, profile signal P and watched penalty W from mandated predecessors, configured weights retained without renormalization or final clamp; DEV-354 per-movie behavior MUST remain unchanged (brief Q4).
- **FR-007**: System MUST pass blended relevance to DEV-416 MMR before Take, consuming `RecommendationOptions.MmrLambda` (default 0.7), DEV-338 similarity, iterative relevance/diversity selection, deterministic movie-ID ties, uniqueness, and `min(Take, eligible candidate count)` output. At `MmrSelector.Select` input only, system MUST apply the landed pool-wide nonnegative relevance offset `-Math.Min(0.0, minPoolScore)`, then map selected `MovieId`s back to original blend results. MUST NOT invent another MMR algorithm, clamp output blend scores, replace Score with the MMR objective, or invent landed MMR signatures (brief Q6; Phase B amendment; recon-DEV-357-2:26-36; specs/DEV-416/CONCLUSIONS.md:396-402).
- **FR-008**: After MMR, system MUST preserve blend scores (including negative finite scores) and predecessor reason objects in MMR selection order; MUST NOT resort by score or replace Score with the MMR objective; MUST NOT fabricate Personal reasons (brief Q6; Phase B amendment).
- **FR-009**: For real history with no positive comparable profile channel or `BlendProfileWeight = 0`, system MUST retain DEV-354 conditional Personal emission and Structural fallback. Acceptance proof for history-plus-Personal MUST use a positive-match history fixture with profile weighting enabled (brief Q6).
- **FR-010**: DEV-357 MUST consume DEV-415’s single-user tagged, configurable-expiry TasteProfile cache and its playback/feedback invalidation; MUST NOT add another cache, TTL, key policy, or package. History predicate support MUST use the same underlying history used for profile calculation (`GetProfileInputsAsync` Movies rows). Consistency MUST be per-query freshness plus DEV-415 invalidation; MUST NOT introduce an atomic cross-query snapshot promise or concurrent-write completeness guarantee (brief Q5; Phase B amendment; recon-DEV-357-2:14-24; specs/DEV-415/CONCLUSIONS.md:51).
- **FR-011**: Implementation MUST stay within the Q7 bounded file set (as amended): new `GetForYouRecommendationsQuery.cs` and `GetForYouRecommendationsQueryHandler.cs` under `src/LamuFlix.Core/Features/Recommendations/`; new validator at `src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs` (Patron Q7 amendment DEV-357:225; brief.md:71,87-91 — Core MUST NOT reference FluentValidation or gain a new package); matching unit tests retained at the three named UnitTests paths; thin route in `RecommendationEndpoints.cs` reusing existing take parser and `ToDto`; registration in `HandlerRegistration.cs`; extend named integration/OpenAPI tests and snapshot; update `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` for route-equality and an additive for-you `ManifestContracts` row only (Patron DEV-357:254-255; brief.md:76,93-100 — no forced completeness/count claim); regenerate `web/src/api/openapi.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts` through existing opt-in writer then `web generate:api` (Patron DEV-357:254-255; brief.md:83,93-100,122; recon-DEV-357:107-108; recon-DEV-357-2:79-81,96). No new port, project, layer, package, migration, schema, DTO shape, or cache; no predecessor algorithm alteration; infrastructure/DI files outside that list are not silently authorized (brief Q7; Phase B amendment).
- **FR-012**: Handler MUST orchestrate landed profile/cache/history, all-library acquisition via the three-port composition, blend, selector-only MMR relevance offset, and ID remapping; results MUST be immutable; cancellation MUST propagate (brief Q7; Phase B amendment). Accepted acquisition cost: `ceil(N/Size)` Browse pages + N+1 `GetAsync` + one `GetProfileInputsAsync`, alongside Q4’s accepted full-library scoring cost.
- **FR-013**: Contract updates MUST reuse existing `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` under `LAMUFLIX_REGENERATE_OPENAPI=true`, then existing `web generate:api` which produces both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (recon-DEV-357:107-108; recon-DEV-357-2:79-81,96; `web/package.json:17`); MUST preserve C# DTO → OpenAPI → generated TypeScript; MUST NOT hand-edit generated outputs, replace the generator, claim each run must change output bytes, or introduce a new exporter. Exact generation commands are those confirmed at HEAD `0607eb7` in recon-DEV-357-2:79-81,96 — not invented beyond that landed configuration (brief Q8; brief.md:122; Phase B amendment).
- **FR-014**: At HEAD `0607eb7`, Phase 1 technical dependency readiness (T001–T011, T012, T013a) is SATISFIED per Patron Phase B amendment: predecessor contract set on `origin/main == HEAD`; Epic 7 prerequisite certified from owner-final `specs/DEV-338/CONCLUSIONS.md:131-133` and `specs/DEV-342/CONCLUSIONS.md:118,121` (recon-DEV-357-2:75-77,94-95); three-port all-library composition, coherent history, and MMR selector offset ruled. Quill MUST align `spec.md` / `plan.md` / `tasks.md` to that ruling before any Phase 2 source implementation. Plan freeze, Gate 1, source-implementation authorization, review clearance, runtime acceptance, test/gate pass, and merge remain separate and unclaimed. No substitute algorithms, production stubs, dependency waiver, invented signatures, or new port/adapter (brief Q1, Q7–Q9; Phase B amendment; PRODUCT.md §2).
- **FR-015**: The For You frontend rail is DEV-418 and out of scope (DEV-357 task note overview; brief Q8).
- **FR-016**: Vectorless/unenriched eligible candidates MUST be retained with landed empty-feature and empty-embedding fallbacks; MUST NOT be excluded solely for missing vectors. Corrupt vectors MUST retain the landed scoring failure behavior; missing vectors MUST take the established fallback (Phase B amendment; recon-DEV-357-2:67-72,92).

### Key Entities

- **TasteProfile / TasteProfileMovie**: DEV-353 domain contracts consumed via DEV-415 cache; calculator performs no I/O (recon-DEV-357:80-84). Landed types verified at HEAD `0607eb7`.
- **HasHistory**: Positive-history predicate (playback OR watched); design ruling expressed via non-empty `RecommendationProfileInputs.Movies` rows (`IsWatched || launched`), not a claimed DEV-415 API name and not `TasteProfile.IsEmpty` (brief Q5; Phase B amendment).
- **Three-port acquisition**: `IMovieCatalog.BrowseAsync` + `IMovieRepository.GetAsync` + `IRecommendationProfileSource.GetProfileInputsAsync` — no new port (Phase B amendment).
- **Blend score inputs**: Literal DEV-354 terms with `S = 0` on this route; Not Interested excluded before scoring (brief Q4).
- **MMR selection**: DEV-416 `MmrSelector.Select` over selector-input-rebased relevance; preserves original Score/reasons; orders output (brief Q6; Phase B amendment; recon-DEV-357-2:26-36).
- **Reason / RecommendedMovieDto**: Predecessor typed reasons and DTO; Personal/Structural kinds on the wire as lowercase `{kind,text}` (brief Q2).
- **RecommendationOptions**: Existing options including `Take`, `CandidatePoolSize`, `MmrLambda`, and DEV-354 blend weights — consumed, not redefined here (recon-DEV-357:80-84).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With watch history (positive-match fixture, profile weighting enabled), the endpoint returns ranked recommendations with Personal reasons (brief.md:24-28; brief Q6).
- **SC-002**: With no watch history, the endpoint returns 200 and an empty list (brief.md:19,24-27).
- **SC-003**: The OpenAPI document has no drift from the committed `openapi.json` after authorized regeneration and existing `generate:api` outputs for `types.ts` and `handlers.ts` (brief.md:22,28; brief.md:122).
- **SC-004**: Omitted `take` defaults to constant 10 and remains range-validated; explicitly empty/malformed/repeated/out-of-range `take` yields 400 ProblemDetails; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400 ProblemDetails/`traceId`; valid `take` matches DEV-341 bounds; parser 400 remains distinct from direct-query validation 422 (brief Q2; brief.md:44,112).
- **SC-005**: Output order matches DEV-416 MMR selection (not score resort); uniqueness and `min(Take, eligible count)` hold on fixtures including a diverse sequel chain; negative finite blend scores survive selector-only rebase (brief Q6, Q8; Phase B amendment; recon-DEV-357-2:26-36).
- **SC-006**: Full-library paging beyond `CandidatePoolSize`, vectorless/unenriched retention, feedback exclusion/recheck, null-movie skips, and cache/history cold-start semantics are proved by verifying tests (Phase B amendment alignment boundary).

## Assumptions

No taste or policy `[assumed]` entries were required for this grill (brief Q9). Design rulings that fill tracker gaps (candidate/`S=0` policy, `HasHistory` predicate, three-port acquisition, selector-only MMR offset, vectorless retention, per-query consistency) are recorded as Patron decisions in `CONCLUSIONS.md` Q4–Q5 and the Phase B pickup amendment, not as silent assumptions.

## Out of scope

- For You frontend rail (DEV-418).
- New NuGet/npm dependency, project, architectural layer, schema/migration, port/adapter, DTO shape, or TasteProfile cache owned by this ticket.
- Substitute algorithms, production stubs, or waiver of DEV-353/354/415/416.
- Invented predecessor signatures, generation flags, or readiness claims beyond the recorded Phase B amendment.
- Atomic cross-query snapshot or concurrent-write completeness guarantee.
- Plan freeze, Gate 1, implementation, generators, tests, or gates claimed by this draft.
- Ticket contradiction or constitution departure (none approved; future cases follow §2.3 escalation).

## Closing bar and review caps (Q3 / Q9)

- Blocking: unresolved Critical or High in-scope findings; any unmet ticket acceptance requirement or applicable gate; missing required review axis; unresolved owner escalation or missing contract needed to judge conformance.
- Medium/Low cannot waive an acceptance requirement or gate; otherwise they do not independently block.
- Round cap: two remediation rounds total per review loop, preserved across re-asks; at the cap report unresolved blockers without a third round.
- Required M-ticket axes: Sentry, Ledger, and Compass.
- Frozen scope: GET for-you, DEV-341 take behavior, named TasteProfile/cache/blend/MMR integrations, predecessor RecommendedMovieDto with Personal reasons, no-history 200 empty, required OpenAPI/snapshot/generated TypeScript and MSW handler updates and verifying tests; anything else is a follow-up issue, not a finding in this round.
- Grill closure and Phase B technical-readiness alignment do not meet this later review bar; ship review, hosted checks, and user-owned merge remain separate.
