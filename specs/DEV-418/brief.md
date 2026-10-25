# DEV-418 — Frozen grill brief

## Identity and phase boundary

- Worktree: F:/Dev/LamuFlix.worktrees/feature-418-spec

- Branch: feature/418-spec

- Pinned base HEAD: 7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947

- Size M; ui: ticket. Task note DEV-418:3-7,26-28.

- Patron confirmed shared understanding and grill closure at Q8 on 2026-10-24. Q1-Q8 full questions, answers, rationale and cited bases are preserved append-only in CONCLUSIONS.md. Taste decisions are recorded in ASSUMPTIONS.md.

- This freezes grill decisions for Quill. Plan challenge/freeze, Palette design, Gate 1, Phase B readiness, reviews and merge clearance remain separate future steps. No Spec Kit artifacts, production changes, gates or tests were produced by this grill.

## Frozen scope and loop discipline — Q8

Frontend ForYouRail and forYouApi on unchanged MovieCarousel and MovieCard above the Library Results section; existing filter/watchlist suppression; generated DEV-357 consumer with take=10, returned order and typed reasons; quiet asynchronous states; reused navigation and existing controls; bounded LibraryView/fixture edits and generated-only contract/handlers; Q7 behavior tests and Palette design conformance; anything else is a follow-up issue, not a finding in this round.

Closing bar: zero unresolved Critical, High or Medium findings within the frozen diff/scope. Low findings are nonblocking and recorded. Missing any required Risk, Standards or Spec report blocks M/L review adjudication. Failed applicable mandatory gates, Could not run, or missing contract prerequisites cannot be demoted into a pass.

Round cap: two review/remediation rounds maximum; at most two fix commits per round. No third autonomous review/fix round. Remaining material goes to user PR review; the cap never implies clearance. Follow-up filing follows charter limits: Critical/High, broken behavior, or user-requested; otherwise noted, no ticket. Existing matching tickets must be checked before a new ticket is filed, through Patron/Rigger.

## Q1 — Ownership and dependencies

DEV-418 is a frontend-only consumer of GET /api/recommendations/for-you from DEV-357, using ForYouRail on MovieCarousel from DEV-343 (task note DEV-418:27-28; recon:20-32). Do not implement a substitute endpoint or hand-written API DTOs. Phase A can specify against the evidenced predecessor design. Phase B must STOP until DEV-357 has landed and the C# DTO -> OpenAPI -> generated TypeScript chain is available and checked for drift at the picked HEAD. Preserving the ticket dependency triggers no owner escalation.

## Q2 — Consumer API contract and evidence boundaries

The future DEV-418 spec must explicitly record the consumed contract by reference to specs/DEV-357/spec.md:79-87, not define a competing API:

- GET /api/recommendations/for-you, take default 10, accepted range 1..CandidatePoolSize.

- Invalid/empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range take gives parser 400 ProblemDetails; direct query validation gives 422 ProblemDetails, with traceId per predecessor design.

- 200 RecommendedMovieDto array; 200 [] for no positive history or no eligible candidates.

- Reasons are typed {kind,text}, lowercase structural/personal; final MMR order is preserved.

- No new public route, DTO field, status code, reason kind, exporter or template is authorized.

Recon:67-93 provides that design contract. Earlier sections A/E reported exact DTO fields NOT FOUND; supplement 2 now locates the existing generated RecommendedMovieDto at web/src/api/types.ts:151-165. It contains the MovieSummaryResponse fields (id, title, posterUrl, releaseYear, runtime, imdbRating, enrichmentStatus, isInWatchlist, isWatched, isNotInterested) plus score and reasons (recon:127). This supersedes only the earlier generated-field absence, not the missing for-you operation. Verify the landed predecessor shape rather than assuming it cannot drift.

Full ticket Scope and Technical Design and separate acceptance criteria remain NOT FOUND in the bounded worktree/notes; no additional acceptance text was supplied (recon:59-64,109-111,177-179). Patron permits UI decisions against the supplied task-note scope at DEV-418:26-28 without blocking grill closure. That ruling does not prove missing ticket text complete.

ADR-0021 exists only as Proposed specs/DEV-321/adr-0021-library-browse-contract-and-url-ownership.md:5-13 (recon:103-107), governing browse/URL context, not this rail contract. Canonical docs/adr ADR-0021 is NOT FOUND. Preserve no-router scope and existing browse URL ownership. Do not promote or rewrite the draft.

## Q3 — Placement, request and visibility

Mount above LibraryView.tsx <section aria-label="Results">. Q6 refines the earlier approximate gating description: eligibility is inline !activeFilters && !watchlistActive, not resultsMode or visible grid item count. activeFilters is the existing toUrlQuery({ ...query, unknown: {} }, currentYear) !== '' calculation; watchlistActive combines the inWatchlist prop and query flag (LibraryView.tsx:276,360; recon:130-138). No exported unfiltered predicate exists. Reuse these calculations; invent no separate field list. Watchlist-only also suppresses the rail.

Suppress mount and new request execution when ineligible, including when cached results exist. In-flight completion must not reveal the rail while filtered. Clearing filters restores eligible query/render behavior through the normal lifecycle.

Request explicit take=10. Hide the complete rail, including heading, on 200 []; do not infer client history or show cold-start/empty-candidate copy. DEV-357 defines history as a PlaybackEvent OR IsWatched=true movie; Not Interested alone is not positive history and TasteProfile.IsEmpty does not establish no history (spec.md:82; recon:84-88). Empty candidates also yield []; the UI must not claim this proves no history.

Preserve API order. Show personal explanations only when returned kind is personal; retain structural fallback and movies with no reasons. Never invent Personal explanations.

## Q4 — Query and asynchronous behavior

Independent feature-local TanStack Query consumer using existing shared QueryClient and generated api/client.ts client.GET. No new QueryClient, state library, npm dependency or wrapper layer. Key ['forYou', 10]; request query take:10 and forward query cancellation signal. Retain retry:false/current shared refresh defaults; add no global policy.

Before first nonempty success and on initial failure render nothing [assumed]. No rail-specific error/retry widget, skeleton, toast, or cold-start message. Library browsing must remain usable; rail pending/error never delays, clears, or replaces browse data. A query error remains an error, not evidence of cold start.

Retain last successful nonempty data during background refresh only while eligible. A later successful [] hides the rail. Filters/watchlist hide cached data immediately. There is no existing for-you invalidation (recon:169-175); do not add edits to feedback/enrichment invalidation stores. Normal query lifecycle governs freshness. Future bespoke invalidation: noted, no ticket.

## Q5 — Presentation, navigation and vocabulary

Heading and accessible MovieCarousel label: For You [assumed]. Use MovieCarousel {label,children} unchanged. Pass generated movie directly to MovieCard.movie and movie.reasons to reasons. Render caller-safe returned reason text through unchanged MovieCard React text rendering, without new prefix, relabeling, fabricated reasons or raw HTML [assumed]. Existing reason classes support kind styling (MovieCard.tsx:140-151; recon:118-129).

Pass LibraryView's existing openMovie callback and existing feedback clients. Existing MovieCard preserves plain-click interception and modified-click link behavior. Do not introduce a router or copy the details-page history implementation into the rail. Preserve existing controls unchanged; no new playback/feedback flow or execution behavior is authorized.

Palette design.md resolves responsive layout, reason placement and keyboard/focus/visual details before plan challenge, within these product decisions.

Feature-local operational vocabulary:

- For You: this Library recommendation rail.

- Personal reason: a returned caller-safe reason with kind personal.

- Cold start: DEV-357's no-positive-history condition described above.

- Library browse grid: the existing Results region.

These terms, including cold start, are NOT FOUND as canonical CONTEXT.md entries in recon:99-101. They are not asserted to be existing glossary definitions. CONTEXT.md remains untouched. No new ADR is required for reuse of existing architecture; Proposed ADR-0021 remains untouched.

## Q6 — Concrete files and charter rulings

Planned new files under web/src:

- features/for-you/ForYouRail.tsx

- features/for-you/forYouApi.ts

- features/for-you/ForYouRail.test.tsx

- features/for-you/forYouApi.test.ts

- features/for-you/ForYouRail.css only if required by Palette's design

- library/LibraryView.forYou.test.tsx

- mocks/forYou.test.ts

Bounded existing changes:

- web/src/library/LibraryView.tsx: mount/eligibility and existing callback/client wiring.

- web/src/mocks/fixtures.ts: generated-type fixtures and success aggregate entry.

- web/src/mocks/handlers.ts: generated output only, after predecessor OpenAPI exists.

- web/src/api/openapi.json and types.ts: predecessor contract ownership; consume verified landed generated contract, regenerate only from upstream C# source if necessary, never hand-edit or invent a shape.

Read/reuse unchanged: ui/MovieCarousel.tsx, library/MovieCard.tsx, api/client.ts and established test infrastructure. No movieFeedbackStore.ts/movieDetailsQueries.ts rewrite. No other tracked source/config rewrite or deletion is authorized by this brief.

Deliberate charter item rulings:

1. No new dependency or swap (Q4/Q6).

2. No new project/top-level folder/architectural layer/state library; feature-local files reuse the established pattern (Q4/Q6).

3. No schema or migration change (Q8 frozen scope).

4. Record predecessor consumer contract in DEV-418 spec by cited reference; no new API shape (Q2).

5. No edits to Features:LocalPlay, secrets or Process.Start; unchanged MovieCard reuse preserves execution behavior (Q6).

6. Bounded LibraryView/fixtures edits are forced by ticket placement and MSW scope and explicitly accepted; generated changes use generators only (Q6).

No ticket-text contradiction or constitution departure was ruled necessary; no owner structural checkbox is raised. Sources: task note DEV-418:26-28; recon:118-175; charter section 2.3; PRODUCT.md sections 1-5; constitution.md:504-506,511-514 as cited by recon.

Not found as existing implementation: ForYouRail, for-you route/client/query key/handler/fixture/invalidation, exported unfiltered predicate. These are planned additions or explicit exclusions, never claims of landed code (recon:146,158,167,175,177-179).

## Q7 — Tasks, tests and gates

Task ordering:

1. Quill drafts spec.md, plan.md, tasks.md from this frozen brief and recon. No unrecorded decision is delegated: gaps return needs decision.

2. Palette supplies design.md for ui: scope. Keel performs read-only speckit-analyze and brief alignment. All three plan challengers run before separate plan freeze and Gate 1 process.

3. After Gate 1, Build workspace performs pickup drift check and verifies landed DEV-357 route, exact DTO/reasons, generated OpenAPI/types/MSW and MovieCard assignability. Missing/drifted contract is STOP, not permission for substitutes.

4. Typed fixtures and generated MSW consumer smoke, then forYouApi/query and ForYouRail using web-implement Vitest/RTL TDD.

5. Library integration, eligibility/navigation/cached-state regression tests and Palette conformance.

6. Full existing web suite and applicable Phase 3 gates, followed by separate review/merge phases. No build work runs in Spec workspace.

Meaningful Vitest/RTL + typed MSW behavior matrix:

- take=10, request cancellation and distinct query identity through consumer behavior.

- Returned order, personal reasons, structural fallback, empty reasons, nullable poster handling through unchanged MovieCard.

- For You accessible carousel label and placement before Results.

- 200 [] hides heading and rail; represents cold start/no candidates without duplicating backend scoring/history algorithms.

- Pending and 400/422/500/network failure leave Library grid usable with no rail-specific error UI.

- Cached nonempty refresh remains visible unfiltered; successful [] clears it.

- Active filters and watchlist-only suppress cached rail and initiate no new requests; in-flight completion cannot remount it; clearing filters restores eligibility.

- Existing openMovie/details callback and modified-click link behavior remain intact.

Use generated-type fixtures/MSW patterns from recon:160-167. Generated default handlers are never hand-written; test-local typed response overrides follow the existing test precedent. Do not assert implementation internals or duplicate backend ranking tests.

Gauge performs applicable Phase 3 checks in Build workspace: Roslyn, complexity, InspectCode, mutation where applicable, property tests, vulnerable packages, format verification, dotnet tests and web gates per task-pipeline:98-108. scripts/run-web-gates.ps1 and the full web suite must pass. Do not invent gate flags, thresholds or exemptions. Scope-empty C# analyzer/mutation results are SKIPPED (scope-empty), nonblocking and never PASS. Other allowed N/A/SKIP outcomes retain their exact pipeline meaning. Could not run or failed mandatory checks block.

Patron authorizes this task-note line, which Conductor must persist before relying on it:
propertyTests: opt-out — frontend presentation/query integration has no new .NET domain invariants

That line only permits property-script exit 2 under pipeline rules; it waives no other verification.

## Handoff

Grill complete at Q8. Quill may draft Spec Kit artifacts from this brief; Keel has not drafted them. Next: Conductor assigns Quill, persists the property-test opt-out line, and arranges Palette before plan challenge. DEV-357 remains a Phase B prerequisite, not a blocker to Phase A document drafting. No owner escalation is open. Do not infer plan approval, Gate 1, implementation or merge clearance from this brief.


## Phase 2 step 6 - Keel plan-challenge adjudication and freeze (2026-10-24)

Identity: F:/Dev/LamuFlix.worktrees/feature-418-spec; feature/418-spec; base HEAD 7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947. Reviewed spec.md, plan.md, tasks.md, design.md, CONCLUSIONS.md, ASSUMPTIONS.md and this brief as current untracked working artifacts, not feature content pinned by that HEAD. Read task note DEV-418, recon-DEV-418, all three findings notes and specs/PRODUCT.md. No production searches or measurements were performed; implementation facts remain recon-derived.

All required axes are present and adjudicated:

- Sentry Risk/Security (findings-DEV-418-Sentry:3-16): accept zero findings. The independent query, cancellation, host eligibility mount gate, quiet states, unchanged escaped MovieCard reason rendering, generated-only contract and T001 STOP controls address the reported risk categories. Sources: brief Q1-Q7; spec FR-001..FR-012; tasks T001,T004-T009; recon:118-175. The considered rapid-toggle request observation remains a dropped non-finding: normal lifecycle is settled by Q4/Q6, and no debounce expansion is required.
- Ledger Standards (findings-DEV-418-Ledger:16-66): accept zero findings. Feature-local composition, unchanged incumbent components, no added dependencies/layers, generated contract ownership, corrected numbered/unnumbered constitution checks, bounded files, ordered test/implementation phases, design focus proofs and honest gate reporting match Q1-Q8. Sources: plan Constitution Check and Design sections; tasks T001-T012; PRODUCT sections 1-5; recon:95-107,118-175. This accepts the planning controls, not results of unrun gates.
- Compass Spec (findings-DEV-418-Compass:9-44): accept coverage/alignment/design/consumer-contract conclusions with zero Critical/High/Medium and two nonblocking Low observations. All 15 FR and 7 SC are accounted for in T001-T012; Palette CSS/focus refinements are reflected in plan Design item 5 and tasks T006-T009 within Q5/Q6 file and design authority. The ask-input spec-brief.md discrepancy is resolved by the actual authoritative brief.md named in this adjudication request; no missing new artifact or correction is required.

Every reported finding disposition:

1. Compass L1 (spec.md:102 and CONCLUSIONS.md Q6): accepted as Low citation imprecision, nonblocking and recorded; no correction required for closure. Compass reports full generated DTO span 151-169 rather than 151-165. Recon:127 already establishes the substantive generated MovieSummaryResponse fields plus score/reasons and direct MovieCard assignability; spec FR-011 still mandates landed-shape verification at pickup. The citation observation changes neither contract nor task. Historical verbatim grill exchanges remain preserved. This adjudication does not independently certify the reviewer-supplied replacement source range.
2. Compass L2 (design.md:304): accepted as Low citation imprecision, nonblocking and recorded; no correction required for closure. Compass reports RelatedMovies focus-effect/handler ranges 46-65 rather than 64-71. The required focus behavior is explicit in design.md:301-311, plan Design item 5 and T006-T009; its implementation/proofs do not depend on the inaccurate precedent range. Q5 expressly delegates keyboard/focus details to Palette. This adjudication does not independently certify the reviewer-supplied replacement source range.

Basis for no remediation: Q8 closing bar expressly permits recorded Low findings; neither observation changes behavior, file scope, contract, test obligation or a settled decision. No new fact outside recon is needed to apply that bar. Both remain noted, no ticket, under Q8 follow-up limits. No Quill fix list and no additional autonomous remediation round are required. No owner structural checkbox is triggered; no ticket change or constitution departure is proposed.

Disposition: PLAN CHALLENGE CLOSED; reviewed Phase A plan is FROZEN under the existing Q1-Q8 scope and loop terms, including Palette design and the already-remediated C1/U1 controls. This append-only status supersedes earlier draft/not-frozen status statements for the reviewed working artifacts; it does not rewrite their historical drafting receipts. No other artifact correction is requested.

Gate 1 remains separate and is not established by this adjudication. Proceed only to the spec PR / Gate 1 process. Phase B remains STOP until DEV-357 lands with verified generated C# DTO -> OpenAPI -> TypeScript/MSW contract, MovieCard assignability, and pickup drift checks/Keel clearance at the picked HEAD (Q1/Q7; tasks T001). No implementation, tests, gates, commits, publishing, build clearance or merge clearance occurred in this ask. Maximum two review/remediation rounds and two fix commits per round remain unchanged.

## Phase 4 step 4 - Patron M2 ruling (Round 1 remediation, 2026-10-25)

Deliberate cited Patron ruling received during review adjudication at
`5b3bc391523e7ca6787a2125fb64fc8dae003ffe` (verbatim text preserved in
`C:/Users/lamun/AppData/Local/Temp/patron-DEV-418-phase4-m2.txt`):

The narrow `<section aria-label="Results">`-scoping edits to
`web/src/library/LibraryView.composed.test.tsx`,
`web/src/library/LibraryView.toolbar.test.tsx`, and
`web/src/library/watchlist.integration.test.tsx` are accepted within DEV-418
under Charter §2.3 item 6 (charter line 24 forced-edit exemption) as
gate- and AC-forced test adjustments, superseding only the literal "no other
tracked source/config rewrite" freeze in Q6 above without requiring remediation.

- Forced-edit basis: mounting ForYouRail above `<section aria-label="Results">`
  in unfiltered browse with typed default MSW handlers (Q6, spec FR-002, FR-010)
  adds For You cards alongside Results cards in unfiltered LibraryView renders,
  directly forcing existing global browse assertions in those three tests to scope
  queries to Results so `scripts/run-web-gates.ps1` and the full existing web
  suite pass (spec SC-006, FR-013).
- No precedent against gates: an earlier Patron Q6 ruling is never precedent
  against a mandatory verification gate or acceptance criterion (FR-002, FR-013,
  SC-006).
- Strict scope: only the verified narrow Results-scoping query adjustments in
  those three test files are authorized; all other unnamed tracked source,
  config, and store files remain frozen.
