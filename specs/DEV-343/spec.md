# Feature Specification: DEV-343 RelatedMovies on Movie Details with reason chips and MSW mocks

**Feature Branch**: `feature/343-spec`

**Created**: 2026-10-20

**Status**: gate1: provisional

**Input**: Ticket DEV-343 (size M, parent DEV-288, ui tag present); settled grill brief `specs/DEV-343/brief.md` (grill closed Q7); facts from note `recon-DEV-343`; Patron rulings in `specs/DEV-343/CONCLUSIONS.md`; taste receipt `specs/DEV-343/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md)*

These terms are already present in root `CONTEXT.md` (`recon-DEV-343:160-161`). No new glossary term is added by this ticket (brief Q1). Published Spec Kit artifacts must use them as written:

- **Recommendation**: A suggested library movie from the details page. _Avoid_: search result, related-title guess without score/reasons.
- **Reason**: A caller-safe explanation attached to a recommendation. _Avoid_: internal identifiers, scorer debug strings rewritten by this ticket.

This ticket consumes the landed DEV-341 recommendations route and generated contract; it does not amend glossary or invent an absent contract (brief Q1).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View related movies with explainable reason chips and open one (Priority: P1)

A user on a loaded Movie Details view sees a Related movies section whose populated recommendations show each movie Title, PosterUrl (with neutral missing/broken-image fallback), optional ReleaseYear, and every API Reasons string verbatim as inert text chips under the poster in returned order. Activating a card navigates to that movie details view via a single descriptive native `/movies/{id}` anchor, preserving ordinary SPA navigation and validated libraryReturnUrl handling, and retaining browser behavior for modified clicks.

**Why this priority**: Ticket acceptance requires related movies with clear, explainable reason chips and click navigation to details (`recon-DEV-343:109-110,118`).

**Independent Test**: Vitest/RTL with existing test server, typed fixtures, generated handler and per-test MSW overrides prove ordered cards, all Reason strings as text in order, empty Reasons without fabricated chips, null/missing and broken poster fallback, optional year, card details navigation and validated library return URL, and native modified activation (brief Q4–Q6).

**Acceptance Scenarios**:

1. **Given** a loaded Movie Details view and a successful recommendations response with ordered items, **When** RelatedMovies settles, **Then** cards render in API order with Title, poster (or neutral fallback), optional ReleaseYear when present, and every Reasons string as inert text chips under the poster in returned order with no truncation, sorting, synthetic explanation, HTML interpretation, or chip filter action.
2. **Given** a recommendation whose Reasons array is empty, **When** its card renders, **Then** no chips are shown and no fabricated explanation appears.
3. **Given** a recommendation with null/missing or broken PosterUrl, **When** its card renders, **Then** a neutral missing/broken-image fallback is used and poster alt stays decorative when the title labels the same link.
4. **Given** a populated RelatedMovies section, **When** the user activates a card with ordinary primary activation, **Then** navigation opens that movie details view using existing SPA navigation and validated libraryReturnUrl handling.
5. **Given** a card native `/movies/{id}` anchor, **When** the user uses a modified click, **Then** browser behavior is retained.
6. **Given** Score or other non-presentation wire fields on the landed DTO, **When** the card renders, **Then** Score is not exposed and no playback/feedback controls are added.

---

### User Story 2 - Empty, loading, and local retryable error states (Priority: P2)

A user on Movie Details sees an accessible loading placeholder while the initial recommendations request has no data yet; a successful empty array shows a message and no carousel; a failed request shows a local notice and Retry through query refetch without a false empty result or whole-page failure. A recommendations 404 does not prove the host movie is missing. Same-movie data may remain during background refetch; a failed refetch gets a local failure notice. Movie identities are isolated so previous recommendations never appear for a newly selected movie.

**Why this priority**: Ticket acceptance requires empty message with no carousel and a loading placeholder (`recon-DEV-343:119`); brief Q4 settles local error isolation and identity isolation.

**Independent Test**: Vitest/RTL covers delayed loading placeholder; empty success message with no carousel; failure/refetch retry including recommendations 404; background-refetch failure; and identity isolation including out-of-order movie requests (brief Q6).

**Acceptance Scenarios**:

1. **Given** a valid loaded Movie Details view and an initial recommendations request with absent data, **When** the section renders, **Then** an accessible loading placeholder reads `Loading related movies…`
2. **Given** a successful recommendations response that is an empty array, **When** the section settles, **Then** the message `No related movies yet.` renders and no carousel is shown.
3. **Given** a recommendations request failure (including 404), **When** the section settles, **Then** a local notice reads `Could not load related movies.` with action `Retry`, the host Movie Details view does not become a whole-page failure, and the failure is not presented as a successful empty result.
4. **Given** a local Retry action, **When** the user activates it, **Then** the query refetches.
5. **Given** same-movie data already shown, **When** a background refetch fails, **Then** a local failure notice appears without inventing an empty success.
6. **Given** the user selects a different movie, **When** recommendations for the new movie load, **Then** previous recommendations never appear for the newly selected movie (identity isolation, including out-of-order responses).

---

### User Story 3 - Exported reusable keyboard-accessible MovieCarousel (Priority: P3)

A consumer mounts the exported presentational `MovieCarousel` with an accessible label and rendered card children. The carousel owns horizontal layout and scrolling only: labelled region, semantic list, focusable scroll viewport, Previous/Next buttons disabled at their respective boundaries and both disabled when content fits, ArrowLeft/ArrowRight scrolling only when the viewport itself has focus, native card links in ordinary Tab order with Enter and modified activation retained, no wraparound, autoplay, roving focus, or descendant-key interception. Scrolling does not move movie focus. Visible focus, native touch/overflow, and reduced motion are supported. Callers compose cards, including future non-recommendation children for DEV-418 reuse; the carousel has no fetching, endpoint DTO dependency, or recommendation logic.

**Why this priority**: Ticket Scope requires a reusable horizontal keyboard-accessible `MovieCarousel` exported for DEV-418 reuse (`recon-DEV-343:108,120`).

**Independent Test**: Focused Vitest/RTL on `MovieCarousel` prove overflow, both boundaries, no-overflow state, Previous/Next buttons, viewport-only Arrow keys, normal link Tab/Enter behavior, and a reuse mount with non-recommendation children; layout is stubbed narrowly for deterministic RTL. Built-UI browser verification complements stubs for actual overflow, focus, keyboard behavior, responsive layout, and reduced motion (brief Q3, Q6).

**Acceptance Scenarios**:

1. **Given** overflowing children, **When** the carousel mounts, **Then** Previous/Next reflect start/end boundaries (disabled at their respective ends) and ArrowLeft/ArrowRight scroll only when the viewport itself has focus.
2. **Given** content that fits without overflow, **When** the carousel mounts, **Then** both Previous and Next are disabled.
3. **Given** focus on a native card link inside the carousel, **When** the user Tabs or presses Enter, **Then** ordinary link Tab order and Enter/modified activation apply; scrolling does not move movie focus.
4. **Given** a reuse mount that supplies non-recommendation children, **When** the carousel renders, **Then** it lays out and scrolls those children without recommendation fetching or DTO coupling.
5. **Given** the delivered component module, **When** consumers import it, **Then** `MovieCarousel` is exported for reuse.

### Edge Cases

- Query is keyed by movieId and enabled only for a valid loaded Movie Details view; consume the landed DEV-341 route with default parameters; do not invent limits, options, or statuses (brief Q1, Q4).
- API order of recommendations and Reasons is preserved; empty Reasons means no chips (brief Q4–Q5).
- Placement is after MoreWithPanel and before FileSection, retaining both (`recon-DEV-343:20`; ASSUMPTIONS.md Q5).
- Heading copy: `Related movies` (ASSUMPTIONS.md Q4).
- Full Reasons may wrap; Palette owns spacing, layout, and responsive visuals without changing the interaction contract (brief Q3, Q5).
- Phase B waits for merged DEV-323 and DEV-341, landed generated contract, Wisp drift recon, Gauge baseline, and Keel pickup clearance; stop on missing evidence or contract mismatch rather than inventing a bridge (brief Q1, Q6).
- No backend/API/package/schema/playback/secrets/Process.Start changes; no hand-written DTOs or hand-edited generated files (brief Q1–Q2, Q6).
- `propertyTests: opt-out — no domain invariants in this presentation/contract-only change` applies if no tagged property tests apply; never a failing-test waiver (brief Q6–Q7).
- Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS; Exit 1 and Could not run remain blocking (brief Q6).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Deliver RelatedMovies on the existing Movie Details view by additive integration into `web/src/features/movie-details/MovieDetailsView.tsx`, placed after MoreWithPanel and before FileSection, preserving all existing details functionality (`recon-DEV-343:20`; brief Q2, Q5).
- **FR-002**: Add `web/src/features/movie-details/relatedMoviesApi.ts` using the existing typed openapi-fetch client and TanStack Query, keyed by movieId, enabled only for a valid loaded Movie Details view, consuming the landed DEV-341 recommendations route with default parameters, preserving API order, and isolating movie identities so previous recommendations never appear for a newly selected movie (brief Q1, Q4).
- **FR-003**: Implement `web/src/features/movie-details/RelatedMovies.tsx` with `RelatedMovies.css` covering initial accessible loading placeholder (`Loading related movies…`), successful empty array message (`No related movies yet.`) with no carousel, populated ordered cards, and local error notice (`Could not load related movies.`) with Retry via query refetch; never a false empty result or whole-page failure; a recommendations 404 does not prove the host movie is missing; same-movie data may remain during background refetch with local failure notice on failed refetch (brief Q4; ASSUMPTIONS.md Q4).
- **FR-004**: Each populated card MUST show Title, PosterUrl with neutral missing/broken-image fallback, optional ReleaseYear, and every API Reasons string verbatim as inert text chips under the poster in returned order; empty Reasons means no chips; do not expose Score or add playback/feedback controls; use decorative poster alt when the title labels the same link (brief Q5).
- **FR-005**: Each card MUST have a single descriptive native `/movies/{id}` anchor; ordinary primary activation uses existing SPA navigation and validated libraryReturnUrl handling; modified clicks retain browser behavior (`recon-DEV-343:35-39`; brief Q5).
- **FR-006**: Export presentational `web/src/ui/MovieCarousel.tsx` with `MovieCarousel.css` and focused `MovieCarousel.test.tsx`, accepting an accessible label and rendered card children; it owns horizontal layout and scrolling only and MUST NOT fetch, depend on endpoint DTOs, or contain recommendation logic (brief Q2–Q3).
- **FR-007**: MovieCarousel keyboard and scroll contract: labelled region; semantic list; focusable scroll viewport; ArrowLeft/ArrowRight scroll only when the viewport itself has focus; native card links in ordinary Tab order with Enter and modified activation retained; Previous/Next buttons disabled at their respective boundaries and both disabled when content fits; no wraparound, autoplay, roving focus, or descendant-key interception; scrolling does not move movie focus; support visible focus, native touch/overflow, and reduced motion (brief Q3; ASSUMPTIONS.md Q3).
- **FR-008**: Consume landed generated `web/src/mocks/handlers.ts`. If upstream drift requires regeneration, use C# export → `web/src/api/openapi.json` → `npm run generate:api` → generated types/handlers. Never manually edit generated outputs or duplicate a handler. Backend API implementation belongs to DEV-341 (brief Q1, Q6; `recon-DEV-343:141-144`).
- **FR-009**: Additive typed `web/src/mocks/fixtures.ts`, handler success/status smoke tests, and `web/src/api/client.typecheck.ts` only as needed to prove ticket and contract requirements; reuse existing/upstream smoke coverage rather than duplicating it (brief Q6; `recon-DEV-343:54-58`).
- **FR-010**: Vitest/RTL with existing test server, typed fixtures, generated handler and per-test MSW overrides covering empty success, delayed loading, populated ordered cards, failure/refetch retry including recommendations 404, background-refetch failure, identity isolation including out-of-order movie requests, Reason chip rendering, poster fallbacks, optional year, card navigation and validated library return URL, native modified activation, existing details-section regressions, and carousel overflow/boundary/keyboard/reuse proofs with narrowly stubbed layout for deterministic RTL (brief Q6; `recon-DEV-343:62-67`).
- **FR-011**: Built-UI browser verification of actual overflow, focus, keyboard behavior, responsive layout, and reduced motion complements layout stubs; no new end-to-end framework or dependency is required (brief Q6).
- **FR-012**: Approved new files: `web/src/ui/MovieCarousel.tsx`, `MovieCarousel.css`, `MovieCarousel.test.tsx`; `web/src/features/movie-details/RelatedMovies.tsx`, `RelatedMovies.css`, `RelatedMovies.test.tsx`, `relatedMoviesApi.ts`. Approved additive edits: `MovieDetailsView.tsx`, `MovieDetailsView.test.tsx`, and typed fixtures/handler smoke/`client.typecheck.ts` only as needed. No new NuGet/npm dependency, project, architectural layer, database/schema change, backend route/status/DTO change, LocalPlay, secrets, Process.Start, unrelated deletion/rewrite, CONTEXT.md edit, or ADR (brief Q2, Q6–Q7; `recon-DEV-343:84-89`).
- **FR-013**: Phase B gate bar: `npm run check:api`, `scripts/run-web-gates.ps1`, and all applicable task-pipeline Phase 3 step 5 gates; Gauge records command, head and actual exit/verdict. `propertyTests: opt-out - no domain invariants in this presentation/contract-only change` if no tagged property tests apply. Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS. Exit 1 and Could not run remain blocking; no thresholds or required checks are waived (brief Q6).
- **FR-014**: Loop discipline: Critical/High block; every Medium/Low receives evidence-backed disposition; plan challenge and remediation each cap at two rounds; frozen scope is RelatedMovies on Movie Details consuming landed DEV-341; exported presentational horizontal keyboard-accessible MovieCarousel reusable by DEV-418; ordered verbatim Reason chips, safe cards and details navigation; required states with local error isolation; generated-contract MSW fixtures/proofs and specified tests/accessibility/browser/gates, with additive existing-folder integration only; anything else is a follow-up issue, not a finding in this round (brief Q7).

### Key Entities

- **Recommendation**: Suggested library movie from the details page (glossary; this ticket presents the landed DEV-341 list on Movie Details).
- **Reason**: Caller-safe explanation (exposed as API Reasons strings on each recommendation card).
- **MovieCarousel**: Presentational horizontal scroll region that accepts labelled card children for reuse (including DEV-418).
- **RelatedMovies**: Movie Details section that owns the typed recommendations query, states, cards, and Reason chips.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a loaded Movie Details view with populated recommendations, related movies display with every API Reason string as a clear inert chip under the poster in returned order.
- **SC-002**: Successful empty recommendations show `No related movies yet.` and no carousel; initial absent-data request shows the accessible loading placeholder.
- **SC-003**: Recommendations failures (including 404) stay local with Retry and never present a false empty success or whole-page details failure.
- **SC-004**: Activating a recommendation card opens that movie details view; modified clicks retain browser behavior.
- **SC-005**: `MovieCarousel` is exported and usable with non-recommendation children; keyboard scroll and Previous/Next boundary behavior match the settled interaction contract.
- **SC-006**: Existing Movie Details sections (including MoreWithPanel and FileSection) continue to work after additive integration.
- **SC-007**: Phase B web/contract gates run with honest exit/verdict recording; property opt-out accepts no-tests-tagged exit 2 only; SKIPPED/SKIP/N/A never recorded as PASS.

## Assumptions

- Carousel micro-interactions and copy listed in `specs/DEV-343/ASSUMPTIONS.md` (Q3–Q5) are Patron taste rulings tagged `[assumed]`; full basis is in `CONCLUSIONS.md`.
- Placement after MoreWithPanel and before FileSection is a Patron taste ruling tagged `[assumed]` (ASSUMPTIONS.md Q5).
- Phase B build intake remains gated on merged DEV-323 and DEV-341, landed C# DTO → OpenAPI → generated TypeScript/MSW contract, Wisp drift recon, Gauge baseline, and Keel pickup clearance (brief Q1, Q6). Specifying in Phase A does not authorize Phase B.
- Exact landed route field names, default parameters, and handler placement after upstream merge are Phase B recon obligations already ruled in brief Q1/Q6 — not silent drafting inventions. At base HEAD the route, generated types, and handler are absent (`recon-DEV-343:29,50-58,87`).
- No owner structural checkbox was identified (brief Q7). Canonical glossary and existing architectural decisions are ready to consume; no ADR or root glossary edit.
- Exclusions in brief Q7 frozen scope remain out of scope for this feature.
- `propertyTests: opt-out` text in brief Q6–Q7 is preserved and authoritative for the property gate disposition.
- Palette `design.md` must exist before plan challenge; Palette owns spacing, layout, and responsive visuals without changing the interaction contract (brief Q3, Q6).
