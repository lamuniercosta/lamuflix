# Feature Specification: Deterministic For-You ranking and paging

**Feature Branch**: `feature/541-spec`

**Created**: 2026-11-19

**Status**: gate1: provisional

**Input**: Ticket DEV-541 (parent DEV-490; size M); grill-closed `brief.md` (Q1–Q10); `CONCLUSIONS.md`; Maestri notes `DEV-541`, `recon-DEV-541`; `specs/PRODUCT.md`. Constitution cited only in the plan. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Scope; Q1–Q10; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR.

## Clarifications

### Session 2026-11-19

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (capped deterministic For-You MMR, collection release-order cut, stable offset slices/exclusions, `{ items, hasMore }` envelope, contract/mock migration, mechanical caller adaptations, focused verification), HTTP validation (Q4), CandidatePoolSize = take bound only (Q5), caller-recon-first then Core → API → OpenAPI/web → verification with T001 prerequisite for Core (Q7), acceptance/gate matrix (Q8), Size M closing bar (Q9), and two-round review cap (Q10) are frozen in `brief.md` / `CONCLUSIONS.md`. Incomplete named web-caller inventory is an implementation-pickup Wisp confirmation step (Q6/Q7); `recon-DEV-541` confirms no named web consumers yet, so drafts invent none and retain brief-named mocks plus the Wisp handoff. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: For You page (`/for-you`, existing For You rail / list).
- **What the user sees or does**: The same For You surface continues to show a ranked recommendation list. After this ticket, that list reflects seed and collection diversity caps and release-order collection eligibility, so sequels/same-collection clusters and same-seed saturation no longer dominate a page the way an uncapped ranking could. This ticket does **not** add load-more, swap, reason grouping, or card-layout/copy changes (those are DEV-517 / DEV-538 / DEV-542 / DEV-543).
- **What it needs to show up**: Default Compose stack with an enriched library that has strong-match candidates (including at least one multi-member collection when verifying collection behaviour). No new Compose override.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library with several strong-match unwatched candidates spanning more than one collection and more than three matches to the same best seed, **When** the viewer opens For You, **Then** the visible list is a diverse capped ranking (not one collection filling every slot; not more than three picks from the same best seed) and still loads through the existing For You page without new UI chrome.

## Scope

**In scope** (`brief.md` Scope and review terms; Ranking and paging contract; API and contract chain; Task order; Acceptance evidence; Q1–Q10):

1. **Capped deterministic For-You MMR**: Existing strong-match eligibility retained. Exclude supplied movie IDs before the collection cut and MMR. Per collection, order remaining eligible unwatched candidates by ascending release year (null last), then ascending movie ID; keep the earliest `MaxPerCollection` (default 1) before MMR. Run MMR over the full remaining candidate list with ascending movie-ID score tie-break. Each selected movie consumes one best-seed slot (default cap 3) and, when `CollectionId` is non-null, one collection slot (default cap 1). Skip when either applicable cap is full. Seed saturation during MMR never backfills a later collection member. Caps apply across the whole ranking before paging.
2. **Stable offset paging and `hasMore`**: Slice `[offset, offset + take)` of one deterministic full ranking for identical inputs. `hasMore` is true only when a selected result exists at index `offset + take`. Selecting a prefix through that extra result is sufficient. Empty and beyond-end pages return empty `items`. Overflow-safe arithmetic for large valid offsets (e.g. near `int.MaxValue`). Changed exclusion set can change the ranking, so paging with that set starts at offset zero.
3. **HTTP inputs and page envelope**: Keep existing `take` validation (`CandidatePoolSize` remains the take upper bound only — not an MMR input ceiling). Accept `offset` as one invariant-culture integer ≥ 0, default 0, no additional maximum. Accept 0..`MaxExcludeCount` raw repeated `exclude` values (default 50, matching default `CandidatePoolSize`); each raw value must be a positive `MovieId` via `MovieId.TryCreate` or 400; enforce raw limit before deduplicating; unknown valid IDs have no effect. Validate offset in `GetForYouRecommendationsQueryValidator`. Successful For-You GET returns `{ items, hasMore }`. Movie-specific recommendation endpoint keeps its bare-list response.
4. **Contract chain and mechanical callers**: Flow the For-You C# DTO through OpenAPI snapshot → `web/src/api/openapi.json` → generated `web/src/api/types.ts`. Migrate mock fixture/handler (`forYouRecommendationsData` / `handlers.ts`). At pickup, Wisp confirms bare-array web consumers and tests; mechanically adapt confirmed callers/tests to `response.items` (or equivalent unwrap) so the web build/test gate stays green without adding DEV-517 UI behaviour (charter §2.3 item 6 authorized).
5. **Focused verification**: Acceptance evidence in `brief.md` Acceptance evidence / Q8; Size M backend gates per `harness.yml` / AGENTS.md; OpenAPI snapshot and generated contract checks; existing web build/test gate. Closing bar: Risk, Standards, and Spec reports; every accepted in-scope finding closed including Low; explicit rationale for other dispositions; two review rounds max (third-round → user review on the PR).

**Out of scope**:

- DEV-517 load-more and swap UI behaviour.
- DEV-538 reason grouping.
- DEV-542 card layout and text.
- DEV-543 seed weights, threshold, and formula.
- Changing the movie-specific recommendation endpoint response shape.
- New NuGet/npm dependency or package version swap; new project, top-level folder, or architectural layer; database schema beyond ticket-owned tables (none named here); `Features:LocalPlay`, secrets hardcoding, or production `Process.Start`.
- Anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Scope & Technical Design and acceptance criteria are already approved authority (`PRODUCT.md` §3). Brief Q1–Q10 and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions. Only Keel declares PLAN FROZEN after independent reread.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Capped MMR ranking**
One deterministic ordered selection over the full eligible candidate list after exclusions and the collection cut, with seed and collection caps applied inside the selection loop before any page slice.
_Avoid_: applying caps per page independently; treating CandidatePoolSize as an MMR input ceiling

**Collection release-order cut**
Per collection, keep only the earliest `MaxPerCollection` eligible unwatched members by release year ascending, null year last, movie-ID ascending, after exclusions and before MMR.
_Avoid_: backfilling a later member when a kept representative is skipped for seed-cap saturation

**Page envelope**
Successful For-You HTTP body `{ items, hasMore }` where `items` is the offset/take slice of the capped ranking and `hasMore` reflects whether index `offset + take` exists in that ranking.
_Avoid_: wrapping the movie-specific recommendation endpoint; inventing load-more UI

**Raw exclude limit**
`MaxExcludeCount` bounds the number of raw repeated query values before deduplication for ranking.
_Avoid_: counting only distinct IDs against the raw limit

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capped deterministic For-You ranking (Priority: P1) 🎯 MVP

For identical ranking inputs, For You produces one stable capped MMR list: collection representatives chosen by release order, seed and collection slots consumed across the whole ranking, and skipped candidates when a cap is full.

**Why this priority**: Without deterministic capped ranking, paging and the page envelope have no stable universe to slice.

**Independent Test**: Unit coverage on options, collection cut, MMR selector, and For-You handler proves default caps, standalone movies, release-year order (nulls and ties), exclusion-driven promotion, seed-saturation non-backfill, and movie-ID tie-break stability.

**Acceptance Scenarios**:

1. **Given** eligible strong-match candidates including collection members and standalone movies, **When** For-You ranking runs with default caps, **Then** each selected movie consumes one best-seed slot (default 3) and, when it has a non-null collection, one collection slot (default 1), and candidates are skipped when either applicable cap is full.
2. **Given** a collection with multiple eligible unwatched members, **When** the collection cut runs after exclusions, **Then** members are ordered by ascending release year (null last) then ascending movie ID, and only the earliest `MaxPerCollection` remain as MMR inputs.
3. **Given** the earliest collection representative is excluded, **When** the cut runs, **Then** the next eligible member advances into the kept set; **Given** a kept representative is later skipped because its best-seed cap is full, **When** MMR continues, **Then** a later collection member is not backfilled.
4. **Given** identical eligible inputs, exclusions, and options, **When** ranking runs twice, **Then** the ordered selected IDs match, including ascending movie-ID MMR score ties.

---

### User Story 2 - Stable offset paging, exclusions, and hasMore (Priority: P1)

Callers obtain a stable slice of the full capped ranking with exclusions applied before ranking, and `hasMore` reflecting whether another selected result exists past the page.

**Why this priority**: Ticket Wanted paging and hasMore depend on the Q1 slice semantics.

**Independent Test**: Handler/unit and HTTP coverage prove disjoint stable slices, empty/beyond-end pages, hasMore on both sides of an exact page boundary, exclusion filtering before caps, and overflow-safe large offsets.

**Acceptance Scenarios**:

1. **Given** a fixed ranking universe, **When** consecutive non-overlapping offset/take slices are requested with the same exclusions, **Then** the concatenated `items` equal the single full ranking prefix of the same length and slices do not reshuffle.
2. **Given** a ranking whose selected length equals `offset + take`, **When** that page is requested, **Then** `hasMore` is false; **Given** at least one additional selected result exists at index `offset + take`, **When** the same page is requested, **Then** `hasMore` is true.
3. **Given** an offset at or beyond the selected ranking length, **When** For You is requested, **Then** `items` is empty and `hasMore` is false.
4. **Given** a large valid offset near `int.MaxValue`, **When** slicing and hasMore are computed, **Then** the implementation does not overflow a naive `offset + take + 1` sum (safe arithmetic or capped prefix).
5. **Given** a changed exclusion set, **When** paging continues, **Then** the caller must treat the ranking as restarted at offset zero (excluded IDs never consume page or cap slots).

---

### User Story 3 - HTTP validation and For-You page envelope (Priority: P1)

The For-You endpoint accepts validated `take`, `offset`, and repeated `exclude` inputs and returns the wrapped page DTO; invalid input yields 400 through the existing validation pattern.

**Why this priority**: Public contract for paging and exclusions must be enforceable at the HTTP boundary.

**Independent Test**: Endpoint integration tests prove dispatch, wrapped JSON, defaults, malformed/excess raw exclude, invalid offset, and that the movie-specific endpoint remains a bare list.

**Acceptance Scenarios**:

1. **Given** no `offset` or `exclude` query values, **When** `GET /recommendations/for-you` succeeds, **Then** offset defaults to 0, exclude is empty, existing take validation still applies, and the body is `{ items, hasMore }` with `items` as the recommendation page.
2. **Given** a single invariant-culture integer `offset` ≥ 0, **When** the request is handled, **Then** the query validator accepts it; malformed or negative offset yields 400.
3. **Given** 0..`MaxExcludeCount` raw `exclude` values each parseable as a positive `MovieId`, **When** the request is handled, **Then** the raw count is enforced before deduplication, duplicates do not bypass the raw limit, unknown library IDs are ignored for ranking, and excess or non-positive/malformed values yield 400.
4. **Given** a successful For-You response, **When** the movie-specific recommendation endpoint is called, **Then** it still returns the existing bare list shape.

---

### User Story 4 - Contract chain and mechanical web adaptation (Priority: P2)

OpenAPI, generated TypeScript, mocks, and confirmed bare-array consumers stay aligned with the For-You page envelope without adding out-of-scope UI behaviour.

**Why this priority**: Keeps the contract chain and web gate green after the response-shape change; depends on the API envelope from US3.

**Independent Test**: OpenAPI snapshot/contract checks pass; regenerated types expose the envelope; mock + confirmed callers/tests unwrap `items`; existing web build/test gate passes.

**Acceptance Scenarios**:

1. **Given** the updated For-You C# DTO and operation transformer, **When** OpenAPI snapshot and web generation run, **Then** `web/src/api/openapi.json` and `web/src/api/types.ts` describe `{ items, hasMore }` for GetForYouRecommendations.
2. **Given** `web/src/mocks/fixtures.ts` `forYouRecommendationsData` and `handlers.ts` success mapping, **When** mocks are migrated, **Then** the for-you mock returns the wrapped shape expected by the generated client.
3. **Given** Wisp pickup confirmation of bare-array consumers/tests, **When** mechanical `response.items` (or equivalent) adaptations are applied only where confirmed, **Then** the existing web build/test gate stays green without DEV-517 load-more/swap behaviour.

---

### Edge Cases

- Empty eligible set after exclusions/cut → empty `items`, `hasMore` false.
- Exclude duplicates and unknown valid IDs → raw limit still enforced; unknowns ignored; duplicates deduped for ranking only after the raw check.
- Standalone movie (null collection) → consumes only a seed slot.
- Exact page boundary → `hasMore` false when no selected result at `offset + take`.
- Offset near `int.MaxValue` with valid take → no arithmetic overflow when testing for an extra result.
- Movie-specific `/recommendations/{id}` path unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce one deterministic capped For-You MMR ranking for identical eligibility, exclusion, and options inputs, using ascending movie-ID as the MMR score tie-break (`brief.md` Ranking; Q1).
- **FR-002**: System MUST apply supplied exclusions before the collection release-order cut and before MMR/caps; excluded IDs MUST NOT consume page or cap slots; unknown valid IDs MUST have no effect (Q1, Q4).
- **FR-003**: System MUST order each collection's eligible unwatched candidates by ascending release year (null last) then ascending movie ID and keep only the earliest `MaxPerCollection` (default 1) before MMR; exclusion MAY promote the next member; seed-cap skip MUST NOT backfill a later member (Q2, Q3).
- **FR-004**: System MUST apply default caps of 3 per best-seed ID and 1 per non-null collection across the whole ranking before slicing; standalone movies consume only a seed slot; skip when either applicable cap is full (Q2).
- **FR-005**: System MUST treat `CandidatePoolSize` as the take upper bound only for For-You, not as an MMR input ceiling; MMR input is the full list after exclusions and the collection cut (Q5).
- **FR-006**: System MUST return the slice `[offset, offset + take)` and set `hasMore` true iff a selected result exists at index `offset + take`, using overflow-safe arithmetic for large offsets (Q1, Q5, Q8).
- **FR-007**: System MUST accept `offset` as one invariant-culture integer ≥ 0 (default 0, no extra maximum) and validate it in `GetForYouRecommendationsQueryValidator`; malformed/negative values MUST yield 400 via the existing pattern (Q4).
- **FR-008**: System MUST accept 0..`MaxExcludeCount` raw repeated `exclude` query values (default option 50 matching default `CandidatePoolSize`), validate each with `MovieId.TryCreate` as a positive ID, enforce the raw limit before deduplicating, and return 400 for invalid or excess input (Q4).
- **FR-009**: Successful For-You GET MUST return `{ items, hasMore }`; the movie-specific recommendation endpoint MUST keep its existing bare-list response (Q6).
- **FR-010**: System MUST flow the For-You page DTO through OpenAPI snapshot, `web/src/api/openapi.json`, and generated `web/src/api/types.ts`, and migrate the named mock fixture/handler (Q6).
- **FR-011**: After Wisp confirms bare-array consumers/tests, Build MUST apply only mechanical unwrap adaptations authorized by §2.3 item 6 so the web build/test gate stays green without adding DEV-517 UI behaviour (Q6, Q7).
- **FR-012**: Verification MUST cover the acceptance evidence in `brief.md` (deterministic ties; stable disjoint slices; caps; release-year/null/tie/exclusion promotion; seed-saturation non-backfill; exclude duplicates/unknowns; malformed/excess inputs; defaults/dispatch; wrapped JSON; empty/beyond-end; hasMore boundary; large-offset overflow safety) plus OpenAPI/web gates and applicable Size M backend gates, reporting each gate as actually returned (Q8).

### Key Entities

- **For-You page**: Offset/take window over the capped ranking plus `hasMore`.
- **Exclusion set**: Distinct positive movie IDs derived from raw repeated query values after validation.
- **Collection representative set**: Per-collection earliest eligible members kept before MMR.
- **Seed/collection cap counters**: Running consumption during MMR selection across the full ranking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Identical For-You ranking inputs yield the same ordered movie IDs across repeated runs (deterministic ties included).
- **SC-002**: Non-overlapping pages for the same inputs concatenate to the same ranking prefix; a full page may still have `hasMore: false` when caps leave no further selected result.
- **SC-003**: Default seed and collection caps are observable in ranking outcomes (≤3 per best seed; ≤1 per collection when collection ID present; standalones uncapped by collection).
- **SC-004**: Invalid offset or exclude input is rejected with 400; valid defaults succeed with wrapped JSON.
- **SC-005**: OpenAPI snapshot, generated web contract, mocks, and confirmed callers agree on `{ items, hasMore }` and the web build/test gate passes.
- **SC-006**: Focused unit/endpoint tests and applicable Size M gates complete with honest reported results (no threshold lowering; no folding SKIPPED/Could not run into PASS).

## Assumptions

- Existing strong-match eligibility and scoring/reason behaviour remain as on main except for the ranking/paging/envelope changes in scope; DEV-543 formula work is out of scope.
- Typed `RecommendationOptions` will gain the ticket-default cap/`MaxExcludeCount`/`MaxPerCollection` knobs named in the brief without a new options type or dependency.
- Wisp pickup recon (T001) will name the exact web consumer/test files to edit, or explicitly record none beyond brief-named mocks; `recon-DEV-541` currently confirms no named web consumers, so authoritative artifacts invent none.
- No new CONTEXT.md glossary terms are required; vocabulary above is spec-local.
- No taste copy or empty-state wording is introduced by this ticket; no `ASSUMPTIONS.md` taste entries.
