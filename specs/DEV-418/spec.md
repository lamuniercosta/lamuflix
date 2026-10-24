# Feature Specification: DEV-418 For You recommendation rail above the Library browse grid

**Feature Branch**: `feature/418-spec`

**Created**: 2026-10-24

**Status**: Draft

**Input**: Ticket DEV-418 (size M, parent DEV-288, ui tag present); frozen grill brief `specs/DEV-418/brief.md` (grill closed Q8); facts from note `recon-DEV-418`; Patron rulings in `specs/DEV-418/CONCLUSIONS.md`; taste receipt `specs/DEV-418/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — feature-local operational terms)*

These operational terms are settled for this ticket in brief Q5. They are **NOT FOUND** as canonical `CONTEXT.md` entries (`recon-DEV-418:99-101`). This ticket does not edit root `CONTEXT.md` and does not assert them as existing glossary definitions:

- **For You**: This Library recommendation rail.
- **Personal reason**: A returned caller-safe reason with kind `personal`.
- **Cold start**: DEV-357's no-positive-history condition (at least one `PlaybackEvent` OR `IsWatched=true` movie is positive history; Not Interested alone is not; `TasteProfile.IsEmpty` does not establish no history — `specs/DEV-357/spec.md:82`; brief Q3).
- **Library browse grid**: The existing Results region in LibraryView.

Consumed predecessor vocabulary (already in use elsewhere; not redefined here): **Recommendation**, **Reason**, **RecommendedMovieDto**, typed `{kind,text}` with lowercase `structural` / `personal`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View For You recommendations above the Library Results grid (Priority: P1)

A user browsing the Library with no active filters and no watchlist-only mode sees a For You rail mounted above the Results section. When the for-you request succeeds with a nonempty ordered list, the rail shows heading For You, composes unchanged MovieCarousel and MovieCard children in API order, passes returned reasons through unchanged MovieCard rendering (Personal only when kind is personal; structural fallback and empty reasons retained), and reuses LibraryView's existing openMovie callback and feedback clients so plain-click and modified-click card behavior stay intact. No new router, playback flow, or feedback control is introduced.

**Why this priority**: Ticket overview and scope require ForYouRail on MovieCarousel above the Library browse grid consuming GET /api/recommendations/for-you (task note DEV-418:26-28; brief Q1, Q5, Q8).

**Independent Test**: Vitest/RTL with typed MSW proves take=10 request and query identity, returned order, personal/structural/empty reasons and nullable poster through unchanged MovieCard, For You accessible carousel label, placement before Results, and existing openMovie / modified-click behavior (brief Q7).

**Acceptance Scenarios**:

1. **Given** Library is eligible (no active filters and watchlist inactive) and GET /api/recommendations/for-you returns a nonempty ordered RecommendedMovieDto array, **When** ForYouRail settles, **Then** a For You labelled MovieCarousel appears above the Results section with cards in API order, each movie passed directly to MovieCard, and returned reason text rendered through unchanged MovieCard React text rendering without prefix, relabeling, fabricated reasons, or raw HTML.
2. **Given** a recommendation whose reason kind is personal, **When** its card renders, **Then** the Personal explanation is shown; structural reasons and movies with no reasons remain as returned.
3. **Given** a recommendation with nullable posterUrl, **When** its card renders, **Then** unchanged MovieCard poster handling applies.
4. **Given** a populated For You rail, **When** the user activates a card with ordinary primary activation, **Then** LibraryView's existing openMovie callback runs; modified clicks retain existing link behavior.
5. **Given** the rail is mounted, **When** rendering completes, **Then** no new router, playback control, or feedback control has been added beyond existing MovieCard controls.

---

### User Story 2 - Hide For You when empty, pending, failed, filtered, or watchlist-only (Priority: P2)

A user never sees a cold-start or empty-candidate message from this rail. Successful 200 [] hides the complete rail including the heading. Pending before first nonempty success and initial failure render nothing; there is no rail-specific error, retry, skeleton, toast, or cold-start copy. Active filters or watchlist-only suppress mount and new request execution immediately, including when cached results exist; in-flight completion must not reveal the rail while ineligible. Clearing filters restores eligible query/render through the normal lifecycle. Library browsing remains usable regardless of rail pending or error.

**Why this priority**: Ticket and brief require hidden cold start / filtered states and quiet asynchronous behavior without blocking browse (task note DEV-418:28; brief Q3–Q4, Q7).

**Independent Test**: Vitest/RTL covers 200 [] hide; pending/400/422/500/network failure leave grid usable with no rail UI; filter and watchlist suppression of mount and requests including cached and in-flight cases; clearing filters restores eligibility (brief Q7).

**Acceptance Scenarios**:

1. **Given** eligible Library and a successful for-you response that is an empty array, **When** the rail settles, **Then** the complete rail including the For You heading is hidden; the UI does not claim this proves no history or show cold-start/empty-candidate copy.
2. **Given** eligible Library and an initial for-you request with no successful nonempty data yet (pending) or an initial failure, **When** the rail would render, **Then** nothing is shown for the rail; browse data is not delayed, cleared, or replaced.
3. **Given** active filters (`activeFilters` true via existing toUrlQuery calculation) or watchlist-only (`watchlistActive`), **When** Library renders, **Then** ForYouRail is not mounted, no new for-you request executes, and any cached rail data is hidden immediately.
4. **Given** a for-you request was in flight when the user became ineligible, **When** that request completes, **Then** the rail is not revealed while filtered or watchlist-only.
5. **Given** filters/watchlist made the rail ineligible, **When** the user clears them so eligibility returns, **Then** normal eligible query and render behavior resume.

---

### User Story 3 - Independent for-you query with ordered take=10 and quiet cache rules (Priority: P3)

While eligible, the Library uses a feature-local TanStack Query consumer against the shared QueryClient and generated openapi-fetch client: query key `['forYou', 10]`, request `take:10`, cancellation signal forwarded, `retry:false` retained, no new QueryClient/state library/npm dependency. API order is preserved. Last successful nonempty data may remain visible during background refresh only while eligible; a later successful [] hides the rail. No bespoke for-you invalidation is added to feedback/enrichment stores.

**Why this priority**: Brief Q4 and Q7 lock query identity, take, quiet cache, and no new invalidation as the integration contract for the rail.

**Independent Test**: Consumer/unit proofs for take=10, cancellation, distinct query identity, order preservation, cached nonempty refresh while unfiltered, and successful [] clearing cached rail (brief Q7).

**Acceptance Scenarios**:

1. **Given** eligible Library, **When** forYouApi runs, **Then** it requests GET /api/recommendations/for-you with take=10 under query key `['forYou', 10]` and forwards the query cancellation signal.
2. **Given** a nonempty successful result already shown and eligibility retained, **When** a background refresh is in progress, **Then** the last successful nonempty data remains visible.
3. **Given** a nonempty rail shown, **When** a later successful response is [], **Then** the rail hides.
4. **Given** feedback or enrichment invalidation elsewhere in the app, **When** those stores run, **Then** no new for-you invalidation edits were added; freshness follows normal query lifecycle.

### Edge Cases

- Eligibility is inline `!activeFilters && !watchlistActive` using existing LibraryView calculations; not `resultsMode` or visible grid item count (brief Q3, Q6; `recon-DEV-418:130-138`).
- Watchlist-only also suppresses the rail (brief Q3).
- 200 [] covers both no positive history and no eligible candidates; UI must not infer which (brief Q3; `specs/DEV-357/spec.md:82`).
- Invalid/out-of-range take handling is owned by DEV-357 (parser 400 / validation 422); the consumer requests take=10 and treats consumer-visible failures quietly (brief Q2, Q4).
- Full ticket Scope & Technical Design and separate acceptance criteria remain NOT FOUND in bounded notes; Phase A drafts against task-note scope DEV-418:26-28 as Patron ruled (brief Q2). That does not invent missing ticket text.
- ADR-0021 exists only as Proposed draft under specs/DEV-321; canonical docs/adr ADR-0021 is NOT FOUND. Preserve no-router scope and existing browse URL ownership; do not promote or rewrite the draft (brief Q2).
- Phase B STOP until DEV-357 has landed and the C# DTO → OpenAPI → generated TypeScript chain is available and checked for drift at the picked HEAD (brief Q1, Q7).
- No backend/API shape/package/schema/LocalPlay/secrets/Process.Start changes; no hand-written DTOs or hand-edited generated files (brief Q1–Q2, Q6).
- `propertyTests: opt-out — frontend presentation/query integration has no new .NET domain invariants` (brief Q7).
- Scope-empty C# analyzer/mutation exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS; Exit 1 and Could not run remain blocking (brief Q7).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Deliver ForYouRail as a frontend-only consumer of predecessor GET /api/recommendations/for-you from DEV-357. Record the consumed contract by reference to `specs/DEV-357/spec.md:79-87` (take default 10 / range 1..CandidatePoolSize; parser 400 vs validation 422 ProblemDetails with traceId; 200 RecommendedMovieDto array; 200 [] for no positive history or no eligible candidates; typed `{kind,text}` lowercase structural/personal; final MMR order preserved). Do not implement a substitute endpoint, new public route, DTO field, status code, reason kind, exporter, or template (brief Q1–Q2; `recon-DEV-418:67-93`).
- **FR-002**: Mount ForYouRail above LibraryView `<section aria-label="Results">` with eligibility `!activeFilters && !watchlistActive` using existing LibraryView calculations (`LibraryView.tsx:276,360`; `recon-DEV-418:130-138`). Suppress mount and new request execution when ineligible, including when cached results exist; in-flight completion must not reveal the rail while ineligible; clearing filters restores eligible behavior (brief Q3, Q6).
- **FR-003**: Request explicit take=10. Hide the complete rail including heading on 200 []. Do not infer client history or show cold-start/empty-candidate copy (brief Q3).
- **FR-004**: Preserve API order. Show personal explanations only when returned kind is personal; retain structural fallback and movies with no reasons; never invent Personal explanations (brief Q3).
- **FR-005**: Add `web/src/features/for-you/forYouApi.ts` as an independent feature-local TanStack Query consumer using the existing shared QueryClient and generated `api/client.ts` client.GET. Query key `['forYou', 10]`; request query `take:10`; forward cancellation signal; retain `retry:false` / current shared refresh defaults; add no global policy, new QueryClient, state library, npm dependency, or wrapper layer (brief Q4, Q6).
- **FR-006**: Before first nonempty success and on initial failure render nothing [assumed]. No rail-specific error/retry widget, skeleton, toast, or cold-start message. Rail pending/error never delays, clears, or replaces browse data. A query error remains an error, not evidence of cold start (brief Q4; ASSUMPTIONS.md).
- **FR-007**: Retain last successful nonempty data during background refresh only while eligible. A later successful [] hides the rail. Filters/watchlist hide cached data immediately. Do not add edits to feedback/enrichment invalidation stores; normal query lifecycle governs freshness (brief Q4; `recon-DEV-418:169-175`).
- **FR-008**: Implement `web/src/features/for-you/ForYouRail.tsx` (and `ForYouRail.css` only if required by Palette's design.md) with heading and accessible MovieCarousel label For You [assumed]. Use MovieCarousel `{label,children}` unchanged. Pass generated movie directly to MovieCard.movie and movie.reasons to reasons. Render caller-safe returned reason text through unchanged MovieCard React text rendering without new prefix, relabeling, fabricated reasons, or raw HTML [assumed] (brief Q5; ASSUMPTIONS.md; `recon-DEV-418:118-129`).
- **FR-009**: Pass LibraryView's existing openMovie callback and existing feedback clients. Preserve existing MovieCard plain-click interception and modified-click link behavior. Do not introduce a router or copy the details-page history implementation into the rail. Preserve existing controls unchanged; no new playback/feedback flow or execution behavior (brief Q5–Q6).
- **FR-010**: Planned new files under `web/src`: `features/for-you/ForYouRail.tsx`, `forYouApi.ts`, `ForYouRail.test.tsx`, `forYouApi.test.ts`, optional `ForYouRail.css` if Palette requires, `library/LibraryView.forYou.test.tsx`, `mocks/forYou.test.ts`. Bounded existing changes: `library/LibraryView.tsx` (mount/eligibility/callback/client wiring); `mocks/fixtures.ts` (generated-type fixtures and success aggregate); `mocks/handlers.ts` (generated output only after predecessor OpenAPI exists); `api/openapi.json` and `types.ts` (consume verified landed generated contract; regenerate only from upstream C# if necessary; never hand-edit). Read/reuse unchanged: `ui/MovieCarousel.tsx`, `library/MovieCard.tsx`, `api/client.ts`, established test infrastructure. No `movieFeedbackStore.ts` / `movieDetailsQueries.ts` rewrite. No other tracked source/config rewrite or deletion (brief Q6).
- **FR-011**: Verify landed RecommendedMovieDto assignability to MovieCard at pickup (existing generated fields at `web/src/api/types.ts:151-165` plus score and reasons — `recon-DEV-418:127`). Verify the for-you operation exists in landed OpenAPI/types/MSW; absence or drift is STOP, not permission for substitutes (brief Q1, Q2, Q7).
- **FR-012**: Meaningful Vitest/RTL + typed MSW behavior matrix per brief Q7: take=10, cancellation, distinct query identity; returned order, personal/structural/empty reasons, nullable poster via unchanged MovieCard; For You accessible label and placement before Results; 200 [] hides heading and rail; pending and 400/422/500/network failure leave Library grid usable with no rail-specific error UI; cached nonempty refresh remains visible unfiltered and successful [] clears it; active filters and watchlist-only suppress cached rail and initiate no new requests; in-flight completion cannot remount it; clearing filters restores eligibility; existing openMovie/details callback and modified-click link behavior remain intact. Use generated-type fixtures/MSW patterns from `recon-DEV-418:160-167`. Generated default handlers are never hand-written; test-local typed response overrides follow existing precedent. Do not assert implementation internals or duplicate backend ranking tests.
- **FR-013**: Phase B gate bar: `scripts/run-web-gates.ps1` and the full web suite must pass; Gauge performs applicable Phase 3 checks (Roslyn, complexity, InspectCode, mutation where applicable, property tests, vulnerable packages, format verification, dotnet tests and web gates per task-pipeline). Do not invent gate flags, thresholds, or exemptions. `propertyTests: opt-out — frontend presentation/query integration has no new .NET domain invariants` — permits property-script exit 2 under pipeline rules only; waives no other verification. Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS. Exit 1 and Could not run remain blocking (brief Q7).
- **FR-014**: Loop discipline: Closing bar is zero unresolved Critical, High, or Medium findings within the frozen diff/scope; Low findings are nonblocking and recorded. Missing any required Risk, Standards, or Spec report blocks M/L review adjudication. Failed applicable mandatory gates, Could not run, or missing contract prerequisites cannot be demoted into a pass. Round cap: two review/remediation rounds maximum; at most two fix commits per round. Frozen scope sentence (brief Q8): Frontend ForYouRail and forYouApi on unchanged MovieCarousel and MovieCard above the Library Results section; existing filter/watchlist suppression; generated DEV-357 consumer with take=10, returned order and typed reasons; quiet asynchronous states; reused navigation and existing controls; bounded LibraryView/fixture edits and generated-only contract/handlers; Q7 behavior tests and Palette design conformance; anything else is a follow-up issue, not a finding in this round (brief Q8).
- **FR-015**: Charter care-list rulings already recorded: no new dependency/swap; no new project/top-level folder/architectural layer/state library; no schema/migration; predecessor consumer contract by cited reference only; no Features:LocalPlay/secrets/Process.Start edits; bounded LibraryView/fixtures edits explicitly accepted; generated changes use generators only. No ticket-text contradiction or constitution departure; no owner structural checkbox (brief Q6).

### Key Entities

- **ForYouRail**: Library recommendation rail section owning eligibility-gated mount, quiet states, and MovieCarousel composition.
- **forYouApi**: Feature-local typed query consumer for GET /api/recommendations/for-you with key `['forYou', 10]` and take=10.
- **RecommendedMovieDto**: Predecessor DTO consumed via generated types (MovieSummaryResponse fields plus score and reasons).
- **Personal reason / Structural reason**: Typed reason kinds from DEV-357/DEV-354 wire contract; presented only as returned.
- **Eligibility**: Inline Library unfiltered and non-watchlist-only predicate reused from existing calculations.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On eligible Library with nonempty for-you results, users see a For You rail above Results with recommendations in returned order and returned reason text through existing cards.
- **SC-002**: Successful empty for-you results hide the entire rail including the heading, with no cold-start or empty-candidate message.
- **SC-003**: Pending and failed for-you requests leave the Library browse grid usable and show no rail-specific error or loading UI.
- **SC-004**: Active filters or watchlist-only hide the rail and prevent new for-you requests, including against cached and in-flight results; clearing filters restores the rail when eligible.
- **SC-005**: Activating a For You card uses existing Library navigation/feedback behavior; modified clicks remain intact.
- **SC-006**: Existing Library browse grid, filters, and watchlist behavior continue to work after additive integration.
- **SC-007**: Phase B web/contract gates run with honest exit/verdict recording; property opt-out accepts no-tests-tagged exit 2 only; SKIPPED/SKIP/N/A never recorded as PASS.

## Assumptions

- Quiet pending/error (render nothing) and For You heading/label plus direct reason text rendering are Patron taste rulings tagged `[assumed]` in `specs/DEV-418/ASSUMPTIONS.md` (Q4–Q5); full basis is in `CONCLUSIONS.md`.
- Phase B build intake remains gated on landed DEV-357 and verified C# DTO → OpenAPI → generated TypeScript/MSW chain, pickup drift check, and Keel clearance (brief Q1, Q7). Specifying in Phase A does not authorize Phase B.
- Exact landed for-you operation presence and DTO/reason shape after DEV-357 merge are Phase B recon obligations — not silent drafting inventions. At base HEAD the for-you route/client/query key/handler/fixture/invalidation are absent (`recon-DEV-418:146,158,167,175,177-179`).
- Full ticket Scope & Technical Design and separate acceptance criteria remain NOT FOUND; Patron permits UI decisions against task-note scope DEV-418:26-28 without inventing missing ticket text (brief Q2).
- No owner structural checkbox was identified (brief Q6). Proposed ADR-0021 remains untouched; CONTEXT.md remains untouched; no new ADR.
- Exclusions in brief Q8 frozen scope remain out of scope for this feature.
- `propertyTests: opt-out` text in brief Q7 is preserved and authoritative for the property gate disposition.
- Palette `design.md` must exist before plan challenge; Palette owns responsive layout, reason placement, and keyboard/focus/visual details without changing these product decisions (brief Q5, Q7).
