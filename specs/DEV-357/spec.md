# Feature Specification: GET /api/recommendations/for-you from taste profile

**Feature Branch**: `feature/357-spec`

**Created**: 2026-10-22

**Status**: Draft (grill closed at Q9; plan freeze, Gate 1, analysis clearance, and implementation readiness not granted)

**Input**: Closed decision brief at `specs/DEV-357/brief.md` (Q1–Q9); rulings in `CONCLUSIONS.md`. Authoritative ticket Scope & Technical Design and acceptance text as reproduced in recon-DEV-357:108-117. Evidence worktree `F:/Dev/LamuFlix.worktrees/feature-357-spec`; branch `feature/357-spec`; Phase A base / HEAD `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`.

**Controlling closure**: brief.md Q9 closes the grill for drafting only. This draft does not claim plan freeze, Gate 1, gate1: provisional, runtime readiness, or landed predecessor APIs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Personalized for-you ranking with Personal reasons (Priority: P1)

When the library has positive watch history, a client requests personalized recommendations and receives a ranked list built from the cached taste profile, blend scoring across the eligible library, and MMR diversification, with Personal reasons where the blend contract emits them.

**Why this priority**: Delivers the ticket’s primary acceptance path (recon-DEV-357:115) and the mandated TasteProfile / cache / blend / MMR integration (recon-DEV-357:109-110; brief Q1).

**Independent Test**: With a positive-match history fixture and profile weighting enabled, `GET /api/recommendations/for-you` returns 200 with ranked `RecommendedMovieDto` items carrying typed lowercase `{kind,text}` reasons including Personal where DEV-354 emits them, in final MMR selection order.

**Acceptance Scenarios**:

1. **Given** positive history (at least one `PlaybackEvent` OR at least one `IsWatched=true` movie) and a positive-match profile fixture with profile weighting enabled, **When** `GET /api/recommendations/for-you` runs with valid `take`, **Then** the response is 200 with ranked recommendations whose reasons include Personal explanations produced by the landed DEV-354 contract (brief Q6; recon-DEV-357:115).
2. **Given** that history and eligible library candidates, **When** the handler orchestrates profile → blend → MMR → Take, **Then** blend uses literal DEV-354 `Score = a*S + b*P - c*W - d*N` with selected signal `S = 0`, Not Interested excluded before scoring, no selected-movie seed or candidate-pool truncation by `CandidatePoolSize`, and MMR receives blended relevance before Take (brief Q4; recon-DEV-357:109-110,131-133).
3. **Given** MMR selection completes, **When** results are returned, **Then** each item preserves its blended Score and predecessor reason objects, order is DEV-416 selection order (not a final score resort), score is not replaced by the MMR objective, uniqueness holds, size is `min(Take, eligible candidate count)`, and ties follow deterministic movie-ID break (brief Q6; recon-DEV-357:131-135).
4. **Given** the HTTP response, **When** Reasons are serialized, **Then** each reason is predecessor `{kind,text}` with lowercase `structural` / `personal`; no new DTO variant, reason kind, exporter, or invented reason template (brief Q2; recon-DEV-357:101-102).

---

### User Story 2 - Cold start returns empty list (Priority: P1)

When there is no positive watch history, the endpoint returns 200 with an empty list and does not run scoring or MMR.

**Why this priority**: Ticket Scope item 3 and acceptance require no-history 200 empty (recon-DEV-357:111,116); distinct from an empty calculated profile.

**Independent Test**: No-history and Not-Interested-only fixtures return 200 `[]` without invoking blend or MMR; a fixture with real history but empty features does **not** take the no-history path.

**Acceptance Scenarios**:

1. **Given** no positive history (`HasHistory` false: zero `PlaybackEvent` and zero `IsWatched=true` movies), **When** `GET /api/recommendations/for-you` runs, **Then** the response is 200 with an empty list before scoring or MMR (brief Q5; recon-DEV-357:111,115-116).
2. **Given** Not Interested feedback alone and no playback/watched positive signal, **When** the endpoint runs, **Then** it is treated as no positive history and returns 200 `[]` (brief Q5).
3. **Given** real history that yields `TasteProfile.IsEmpty` or no positive comparable profile channel (or `BlendProfileWeight = 0`), **When** the endpoint runs, **Then** it does **not** treat that as no history; DEV-354 conditional Personal emission and Structural fallback apply; no fabricated Personal match (brief Q5–Q6).

---

### User Story 3 - take validation and OpenAPI contract (Priority: P1)

Clients may omit or supply `take`; invalid values are rejected as in DEV-341; the committed OpenAPI baseline and drift snapshot include the new route with no drift.

**Why this priority**: Ticket Scope items 1 and 4 plus acceptance require DEV-341 take behavior and OpenAPI no-drift (recon-DEV-357:109,112,117).

**Independent Test**: Omitted `take` defaults to constant 10 and remains range-validated; boundary and explicitly empty/malformed/repeated/out-of-range cases match DEV-341 parser 400 (distinct from query validation 422); omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400; opt-in paired OpenAPI regeneration then existing `generate:api` leave no drift against committed `openapi.json`.

**Acceptance Scenarios**:

1. **Given** omitted `take` (query key absent) with `CandidatePoolSize >= 10`, **When** the route runs, **Then** default is constant 10 via the existing DEV-341 `ParseTake` contract and that defaulted value is accepted (brief Q2; brief.md:44,112; recon-DEV-357-2:65-71).
2. **Given** a single integer `take` in `1..CandidatePoolSize`, **When** the route runs, **Then** that value is accepted; `CandidatePoolSize` bounds take validation only and does not authorize truncating the candidate universe (brief Q2, Q4).
3. **Given** explicitly present but empty, whitespace-only, malformed, overflowing, repeated, noninteger, out-of-range, or otherwise invalid `take`, **When** the route parses input with the reused DEV-341 parser, **Then** it returns 400 ProblemDetails (`BadHttpRequestException` path). Absence of the `take` key alone is allowed; the effective default may still exceed the pool. **Given** omitted `take` with startup-valid `options.Take <= CandidatePoolSize < 10`, **When** the route runs, **Then** parser 400 ProblemDetails/`traceId` is returned because the defaulted constant 10 fails range validation. Direct FluentValidation failures on the query remain 422 and are distinct from parser 400 (brief Q2; brief.md:44,112; recon-DEV-357-2:65-71; constitution Principle V).
4. **Given** the authorized route and DTO contract, **When** OpenAPI export and existing `web generate:api` complete through the existing opt-in writer then repository generator, **Then** `web/src/api/openapi.json` and the drift snapshot have no drift from the committed baseline, and both generator outputs `web/src/api/types.ts` and `web/src/mocks/handlers.ts` consume that contract (brief Q8; brief.md:83,93-100,122; recon-DEV-357:112,117; recon-DEV-357-2:95-106,112-115).

---

### Edge Cases

- No eligible candidates after Not Interested exclusion → 200 `[]` (brief Q6).
- Full-library candidate set larger than `CandidatePoolSize` → still score all eligible; only `take` is bounded by `CandidatePoolSize` (brief Q4).
- Real history with empty features or `BlendProfileWeight = 0` → Structural fallback / no fabricated Personal; not the cold-start path (brief Q6).
- Playback-only history and watched-only history each satisfy `HasHistory` (brief Q5, Q8).
- Cancellation must propagate through the handler orchestration (brief Q7–Q8).
- Existing per-movie recommendations route must continue to regress cleanly (brief Q8).
- Exact landed predecessor signatures, cache APIs, history-read contract, complete all-library acquisition, and runtime readiness of named DEV-353/354/415/416 remain **pickup prerequisites**. At this Phase A base, playback event/write/persistence and movie-feature vector record/column/snapshot/repository exist (recon-DEV-357-2:50-60); that does not establish history-read, complete all-library acquisition, a confirmed `IMovieFeaturesRepository` interface signature, or runtime readiness (recon-DEV-357-2:56,60,62; brief.md:11,134). Typed-reason wire/consumer/fixture coherence is also a first-phase pickup check (brief.md:106; recon-DEV-357-2:41-44).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose `GET /api/recommendations/for-you` with the same `take` validation as DEV-341: omitted `take` defaults to constant 10 and the effective value remains range-validated in `1..CandidatePoolSize`; a single integer in that range is accepted; explicitly empty, whitespace-only, malformed, overflowing, repeated, noninteger, or out-of-range `take` yields 400 ProblemDetails via the reused parser (`BadHttpRequestException`). Absence of the `take` key alone is allowed; when startup-valid `options.Take <= CandidatePoolSize < 10`, omitted `take` MUST yield parser 400 ProblemDetails/`traceId` because the defaulted constant exceeds the pool. Direct query FluentValidation failures remain 422 and MUST stay distinct from parser 400 (brief Q2; brief.md:44,112; recon-DEV-357-2:65-71; DEV-357 note Scope item 1).
- **FR-002**: System MUST return HTTP 200 with an array of predecessor `RecommendedMovieDto` items; Reasons MUST be DEV-354 typed `{kind,text}` with lowercase `structural` / `personal`; MUST NOT introduce a parallel DTO, new reason kind, exporter, or invented reason template (brief Q2; recon-DEV-357:101-102,110).
- **FR-003**: With positive history, system MUST orchestrate landed TasteProfile (DEV-353) via DEV-415 cache, score eligible library candidates with DEV-354 blend, apply DEV-416 MMR, then Take, preserving final MMR order (brief Q1, Q4, Q6; recon-DEV-357:109-110).
- **FR-004**: `HasHistory` MUST mean at least one recorded `PlaybackEvent` OR at least one `IsWatched=true` movie; Not Interested alone is not positive history. No history MUST return 200 `[]` before scoring or MMR. `TasteProfile.IsEmpty` MUST NOT establish no history (brief Q5; recon-DEV-357:111,115-116).
- **FR-005**: Candidates MUST be all eligible library movies, excluding Not Interested before scoring, with no selected-movie candidate source, seed, pre-truncation, or refill. `CandidatePoolSize` MUST bound take validation only (brief Q4; recon-DEV-357:119-120 ruled in Q4).
- **FR-006**: Blend MUST invoke the literal DEV-354 formula with selected signal `S = 0`, profile signal P and watched penalty W from mandated predecessors, configured weights retained without renormalization or final clamp; DEV-354 per-movie behavior MUST remain unchanged (brief Q4).
- **FR-007**: System MUST pass blended relevance to DEV-416 MMR before Take, consuming `RecommendationOptions.MmrLambda` (default 0.7), DEV-338 similarity, iterative relevance/diversity selection, deterministic movie-ID ties, uniqueness, and `min(Take, eligible candidate count)` output. MUST NOT invent another MMR algorithm or invent landed MMR signatures (brief Q6; recon-DEV-357:129-135).
- **FR-008**: After MMR, system MUST preserve blend scores and predecessor reason objects in MMR selection order; MUST NOT resort by score or replace Score with the MMR objective; MUST NOT fabricate Personal reasons (brief Q6).
- **FR-009**: For real history with no positive comparable profile channel or `BlendProfileWeight = 0`, system MUST retain DEV-354 conditional Personal emission and Structural fallback. Acceptance proof for history-plus-Personal MUST use a positive-match history fixture with profile weighting enabled (brief Q6).
- **FR-010**: DEV-357 MUST consume DEV-415’s single-user tagged, configurable-expiry TasteProfile cache and its playback/feedback invalidation; MUST NOT add another cache, TTL, key policy, or package. Landed acquisition/cache support MUST expose or support the history predicate coherently with the underlying history used for profile calculation; exact signatures and snapshot consistency remain pickup prerequisites (brief Q5; recon-DEV-357:122-127).
- **FR-011**: Implementation MUST stay within the Q7 bounded file set (as amended): new `GetForYouRecommendationsQuery.cs` and `GetForYouRecommendationsQueryHandler.cs` under `src/LamuFlix.Core/Features/Recommendations/`; new validator at `src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs` (Patron Q7 amendment DEV-357:225; brief.md:71,87-91; recon-DEV-357-2:8-13 — Core MUST NOT reference FluentValidation or gain a new package); matching unit tests retained at the three named UnitTests paths; thin route in `RecommendationEndpoints.cs` reusing existing take parser and `ToDto`; registration in `HandlerRegistration.cs`; extend named integration/OpenAPI tests and snapshot; update `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` for route-equality and an additive for-you `ManifestContracts` row only (Patron DEV-357:254-255; brief.md:76,93-100; recon-DEV-357-2:82-93 — no forced completeness/count claim); regenerate `web/src/api/openapi.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts` through existing opt-in writer then `web generate:api` (Patron DEV-357:254-255; brief.md:83,93-100,122). No new port, project, layer, package, migration, schema, DTO shape, or cache; no predecessor algorithm alteration; infrastructure/DI files outside that list are not silently authorized (brief Q7).
- **FR-012**: Handler MUST orchestrate landed profile/cache/history, all-library acquisition, blend, and MMR; results MUST be immutable; cancellation MUST propagate (brief Q7).
- **FR-013**: Contract updates MUST reuse existing `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` under `LAMUFLIX_REGENERATE_OPENAPI=true`, then existing `web generate:api` which produces both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (recon-DEV-357-2:95-106,112-115; `web/package.json:17`); MUST preserve C# DTO → OpenAPI → generated TypeScript; MUST NOT hand-edit generated outputs, replace the generator, claim each run must change output bytes, or introduce a new exporter. Exact generation commands are supplied by Wisp from landed configuration at pickup — not invented here (brief Q8; brief.md:122).
- **FR-014**: Phase B source work MUST NOT begin until Wisp read-only pickup verifies: merged spec, PRODUCT section 2 Epic 7 prerequisite, landed DEV-353/354/415/416, real vector/acquisition dependencies, fresh bounded drift against Phase A base, exact signatures, coherent history-predicate support, all-library summary/feedback/384-vector acquisition, and that landed typed-reason wire/types plus existing RelatedMovies consumers/tests and mock fixtures are coherent with predecessor typed reasons. If typed-reason/consumer/fixture coherence fails → STOP for bounded ownership recon and a cited Patron decision before out-of-ledger edits (do not assign unproven predecessor ownership, send fixes to DEV-418, authorize UI work, or waive web gates; brief.md:106; recon-DEV-357-2:41-44). If other support is absent → STOP for bounded dependency recon and a cited Patron amendment before any new port or adapter. No substitute algorithms, production stubs, dependency waiver, or invented signatures (brief Q1, Q7–Q9; PRODUCT.md §2; recon-DEV-357:20-32; recon-DEV-357-2:50-60,56,60,62).
- **FR-015**: The For You frontend rail is DEV-418 and out of scope (DEV-357 task note overview; brief Q8).

### Key Entities

- **TasteProfile / TasteProfileMovie**: DEV-353 domain contracts consumed via DEV-415 cache; calculator performs no I/O (recon-DEV-357:55-60). Exact landed types verified at pickup.
- **HasHistory**: Positive-history predicate (playback OR watched); design ruling, not a claimed DEV-415 API name (brief Q5).
- **Blend score inputs**: Literal DEV-354 terms with `S = 0` on this route; Not Interested excluded before scoring (brief Q4).
- **MMR selection**: DEV-416 iterative selection over blended relevance; preserves Score/reasons; orders output (brief Q6; recon-DEV-357:129-135).
- **Reason / RecommendedMovieDto**: Predecessor typed reasons and DTO; Personal/Structural kinds on the wire as lowercase `{kind,text}` (brief Q2).
- **RecommendationOptions**: Existing options including `Take`, `CandidatePoolSize`, `MmrLambda`, and DEV-354 blend weights — consumed, not redefined here (recon-DEV-357:14,67,131).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With watch history (positive-match fixture, profile weighting enabled), the endpoint returns ranked recommendations with Personal reasons (recon-DEV-357:115; brief Q6).
- **SC-002**: With no watch history, the endpoint returns 200 and an empty list (recon-DEV-357:116).
- **SC-003**: The OpenAPI document has no drift from the committed `openapi.json` after authorized regeneration and existing `generate:api` outputs for `types.ts` and `handlers.ts` (recon-DEV-357:117; brief.md:122).
- **SC-004**: Omitted `take` defaults to constant 10 and remains range-validated; explicitly empty/malformed/repeated/out-of-range `take` yields 400 ProblemDetails; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400 ProblemDetails/`traceId`; valid `take` matches DEV-341 bounds; parser 400 remains distinct from direct-query validation 422 (brief Q2; brief.md:44,112; recon-DEV-357-2:65-71).
- **SC-005**: Output order matches DEV-416 MMR selection (not score resort); uniqueness and `min(Take, eligible count)` hold on fixtures including a diverse sequel chain (brief Q6, Q8; recon-DEV-357:131-135).

## Assumptions

No taste or policy `[assumed]` entries were required for this grill (brief Q9). Design rulings that fill tracker gaps (candidate/`S=0` policy, `HasHistory` predicate) are recorded as Patron decisions in `CONCLUSIONS.md` Q4–Q5, not as silent assumptions.

## Out of scope

- For You frontend rail (DEV-418).
- New NuGet/npm dependency, project, architectural layer, schema/migration, port/adapter, DTO shape, or TasteProfile cache owned by this ticket.
- Substitute algorithms, production stubs, or waiver of DEV-353/354/415/416.
- Invented predecessor signatures, generation flags, or readiness claims at Phase A base.
- Plan freeze, Gate 1, implementation, generators, tests, or gates claimed by this draft.
- Ticket contradiction or constitution departure (none approved; future cases follow §2.3 escalation).

## Closing bar and review caps (Q3 / Q9)

- Blocking: unresolved Critical or High in-scope findings; any unmet ticket acceptance requirement or applicable gate; missing required review axis; unresolved owner escalation or missing contract needed to judge conformance.
- Medium/Low cannot waive an acceptance requirement or gate; otherwise they do not independently block.
- Round cap: two remediation rounds total per review loop, preserved across re-asks; at the cap report unresolved blockers without a third round.
- Required M-ticket axes: Sentry, Ledger, and Compass.
- Frozen scope: GET for-you, DEV-341 take behavior, named TasteProfile/cache/blend/MMR integrations, predecessor RecommendedMovieDto with Personal reasons, no-history 200 empty, required OpenAPI/snapshot/generated TypeScript and MSW handler updates and verifying tests; anything else is a follow-up issue, not a finding in this round.
- Grill closure does not meet this later review bar; ship review, hosted checks, and user-owned merge remain separate.
