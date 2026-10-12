# Feature Specification: DEV-321 — Library browse view (grid, pagination, URL sync)

**Feature Branch**: `feature/321-spec`

**Created**: 2026-10-11

**Status**: gate1: provisional

**Input**: `specs/DEV-321/brief.md` (decision-complete, grill closed Q10/12); rulings in `specs/DEV-321/CONCLUSIONS.md`; taste in `specs/DEV-321/ASSUMPTIONS.md`; facts in `recon-DEV-321` (esp. sections 9–15); ticket body `DEV-321:19-35`; `specs/PRODUCT.md`.

No decision in this spec goes beyond `brief.md`. Anything not decided there is marked `needs decision:` in `DRAFTING_RECEIPT.md` (none expected).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Browse the Library grid (Priority: P1)

A visitor opens the SPA root and sees the main Library view: a responsive poster grid where each tile shows the movie poster (or a title-labelled placeholder at the same reserved aspect ratio when the poster is null or fails to load), title, year, runtime, IMDb rating badge, and EnrichmentStatus chip (`Pending`, `Enriched`, `NotFound`, `Failed`).

**Why this priority**: This is the ticket Overview and Scope item 1 (DEV-321:25,28) and the core value of the ticket; without it nothing else is visible.

**Independent Test**: Seed real-DB rows with populated and null `poster_url`/metadata, load the root, and confirm every tile shows title plus the five grid scalars or their specified fallbacks (placeholder, em dash).

**Acceptance Scenarios**:

1. **Given** a library with movies, **When** the user opens the SPA root, **Then** a responsive grid renders one tile per movie with poster, title, year, runtime as `<n> min`, one-decimal IMDb badge, and exact-label status chip.
2. **Given** a movie with null `posterUrl`, **When** its tile renders, **Then** a placeholder with the same reserved 2:3 aspect ratio and the movie title as accessible text is shown.
3. **Given** a movie whose poster image fails to load, **When** the error fires, **Then** the same title-labelled placeholder is shown.
4. **Given** a movie with missing year/runtime/rating, **When** its tile renders, **Then** each missing scalar shows an em dash.
5. **Given** an empty library, **When** the view loads, **Then** an empty-library state explains the state and offers clear filters.
6. **Given** filters matching nothing, **When** applied, **Then** a filtered-empty state explains the state and offers clear filters.

---

### User Story 2 — URL-synced filters, pagination, bookmarks, history (Priority: P1)

A user configures filters and pages through the Library controls; every filter value and the page number are reflected in canonical URL query parameters. Opening a bookmarked URL (including the ticket-example vocabulary) restores the corresponding filters, and browser back/forward restores all controls and query state.

**Why this priority**: This is ticket Scope item 2 and Acceptance Criterion 1 (DEV-321:29,34); it is the ticket's other acceptance criterion and is independently testable via URLs.

**Independent Test**: Navigate to a URL with query params and confirm the corresponding filters are set; change controls and confirm the canonical URL updates; use back/forward and confirm state restores.

**Acceptance Scenarios**:

1. **Given** a bookmarked URL with canonical filter/page keys, **When** the user navigates to it, **Then** all corresponding filters and page are set up from the URL.
2. **Given** the ticket-example vocabulary (`genres`, `Asc`/`Desc` aliases), **When** the user opens such a URL, **Then** the aliased values are accepted, applied, and canonicalized via `replaceState` (aliases never emitted; `genreIds` wins when both genre keys occur).
3. **Given** the user edits a filter or sort, **When** the change applies, **Then** page resets to 1 and a history entry is pushed with adapter state updated immediately.
4. **Given** the user changes page via Previous/Next, **When** the change applies, **Then** only the page changes (no filter reset) and a history entry is pushed.
5. **Given** back/forward navigation, **When** `popstate` fires, **Then** all controls and query state restore without a push.
6. **Given** initialization or alias/default/malformed normalization, **When** it runs, **Then** the current entry is replaced, not pushed.
7. **Given** a valid page beyond available results, **When** it is in the URL, **Then** it stays in the URL and shows empty content with a return-to-first-page action (no silent rewrite).
8. **Given** an unknown URL key, **When** canonicalizing, **Then** it is dropped.
9. **Given** an unparsable value, **When** canonicalizing, **Then** it normalizes to defaults; valid but out-of-range or cross-field-invalid values remain server validation's responsibility and can produce the error boundary's 422 invalid-filter state.

---

### User Story 3 — Loading, errors, and recovery (Priority: P2)

While browse data fetches via TanStack Query, the view shows reserved-size skeletons; failures surface through a hand-written Library error boundary with Retry; same-key background refetch keeps content with a polite announcement.

**Why this priority**: Ticket Scope item 3 (DEV-321:30) — fetching behavior supporting the grid, testable independently with MSW/RTL fixtures.

**Independent Test**: Render the view against MSW handlers for success/422/400/network-error and confirm skeletons, 422 invalid-filter error (never retried), recoverable fetch error for other failures including 400, retry, boundary reset, and status-based classification independent of error-key vocabulary.

**Acceptance Scenarios**:

1. **Given** a normalized-key change (filters or page), **When** fetching, **Then** reserved-size skeletons show (never previous-key placeholder data).
2. **Given** a same-key background refetch, **When** it runs, **Then** current content is retained and loading is announced politely.
3. **Given** a render or query failure, **When** it occurs, **Then** the Library boundary shows the failure with a Retry action for recoverable failures.
4. **Given** Retry on a recoverable failure, **When** activated, **Then** the boundary resets and the same canonical URL refetches without a history entry.
5. **Given** a server 422, **When** received, **Then** an invalid-filter error is shown and is never retried; **Given** any other failure including 400, **When** received, **Then** a recoverable fetch error is shown with retry available and no retry loop. The state is chosen by HTTP status, not by error-key vocabulary.
6. **Given** a normalized query-key change, **When** it occurs, **Then** the boundary resets.
7. **Given** an in-flight query superseded by a newer key, **When** the stale response arrives, **Then** it is ignored (cancellation/stale-response protection).

---

### User Story 4 — Filter controls and pagination (Priority: P2)

Every codec key has a corresponding control (text submitted on Enter/Search; sort/direction selects; status multiselect; watchlist tri-state; optional runtime/year bounds plus include-unknown; genre/actor numeric-ID entry for repeated IDs), filters apply together with page reset, and Previous/Next pagination reflects `ceil(totalCount/pageSize)` with bounds respected.

**Why this priority**: Makes every supported query value restorable in its control per AC DEV-321:34; controls without URL sync or grid have no independent value, so P2 after US1/US2.

**Independent Test**: Exercise each control, apply together, and confirm the canonical URL and grid update with page reset; page through results and confirm counts and disabled bounds.

**Acceptance Scenarios**:

1. **Given** any supported filter value in the URL, **When** the view initializes, **Then** the corresponding control shows it.
2. **Given** text typed in the search box, **When** the user presses Enter or Search, **Then** filters apply with page reset (not per keystroke).
3. **Given** sort, direction, statuses, watchlist, runtime/year bounds, include-unknown, genre/actor IDs set in controls, **When** applied together, **Then** the canonical URL carries them and page resets to 1.
4. **Given** `totalCount` and `pageSize`, **When** paginating, **Then** total pages equal `ceil(totalCount/pageSize)`, the current page and result count show, and movement past known bounds is disabled.
5. **Given** no size selector, **When** the view runs, **Then** the fixed UI pageSize applies while a URL-supplied pageSize is honored.

---

### Edge Cases

- Null scalar members stay present as JSON null on the wire (Q1).
- Text empty-string remains a distinct representable value; null text is absent (Q3).
- Repeated scalar keys fall back per Q3 (malformed/repeated scalar to its default; malformed/repeated range key drops the whole group).
- `genreIds` wins when both `genres` and `genreIds` occur; no other aliases exist (Q2).
- Keys/values are encoded and pairs joined with ampersands; reserved characters and parity with server codec fixtures are tested (Q3).
- No per-tile detail requests, no migration, no new join; if implementation needs either, stop and re-ask Patron (Q1).
- No detail/play/watchlist-mutation controls; LocalPlay untouched (Q7).
- No numeric performance target is set; Palette verifies visual layout later (Q7).
- Poster persistence (DEV-408) and enrichment/worker repair are out of scope and not claimed (Q4).
- Phase B waits for the user merge of DEV-320 #124 plus Wisp recon of merged `types.ts`, `client.ts`, and abort-signal support; no copying unmerged code, no hand-written DTO duplicates, no hand-edited generated files (Q5).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Browse contract MUST additively widen `MovieSummary` → `MovieSummaryResponse` on existing `GET /api/movies`, preserving `id`, `title`, `items`, `totalCount`, route, query parameter contract, and status codes (Q1).
- **FR-002**: `posterUrl` MUST be a nullable string passing stored `MovieRecord.PosterUrl` through unchanged; null stays present as JSON null (Q1).
- **FR-003**: `releaseYear` MUST be a nullable CLR `int` (calendar year) with the same name/type/units as details metadata (Q1).
- **FR-004**: `runtime` MUST be a nullable CLR `int` (minutes) with the same name/type/units as details metadata (Q1).
- **FR-005**: `imdbRating` MUST be a nullable CLR `decimal` (0–10, at most one decimal place) with the same semantics as details metadata (Q1).
- **FR-006**: `enrichmentStatus` MUST be the required existing `EnrichmentStatus` enum serialized as the exact string names `Pending`, `Enriched`, `NotFound`, `Failed`; OpenAPI MUST describe the four string values and generated TS MUST carry that contract (Q1).
- **FR-007**: The existing global status converter MUST NOT be added to or changed; nullability/number handling MUST follow existing generated metadata schemas (Q1).
- **FR-008**: URL state MUST use native `URLSearchParams`, `history.pushState`, `history.replaceState`, and `popstate` with no router, state, or validation dependency; the small feature URL adapter MUST be the sole owner of history and URL state (Q2).
- **FR-009**: Canonical URL keys MUST be `text`, `genreIds`, `actorIds`, `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax`, `statuses`, `inWatchlist`, `sort`, `direction`, `page`, `pageSize`; sort names `Title`, `Year`, `Rating`, `Runtime`; direction names `Ascending`, `Descending` (Q2).
- **FR-010**: Read-only aliases `genres` → `genreIds` and `Asc`/`Desc` → `Ascending`/`Descending` MUST be accepted on read, never emitted, normalized with `replaceState`; `genreIds` wins on conflict; no other aliases (Q2).
- **FR-011**: Canonical URLs MUST always emit `sort`, `direction`, `page`, `pageSize`; a supplied URL pageSize MUST be honored; no size selector is offered (Q2).
- **FR-012**: User changes MUST push a history entry and immediately update adapter state; initialization and alias/default/malformed normalization MUST replace; back/forward MUST restore without a push; the listener MUST be removed on cleanup (Q2).
- **FR-013**: Filter/sort edits MUST reset page to 1; pagination MUST change page alone (Q2).
- **FR-014**: Unknown keys MUST be dropped; unparsable values MUST normalize to defaults; server validation MUST stay the single authority for range/cross-field rules (Q2).
- **FR-015**: SPA product defaults MUST be `sort=Title`, `direction=Ascending`, `page=1`, `pageSize=24`; text/watchlist/runtime/year absent; lists empty (Q3).
- **FR-016**: Lists MUST accumulate and emit repeated keys, one value per key (Q3).
- **FR-017**: Incomplete runtime/year groups MUST read leniently (missing bounds empty, missing `runtimeIncludeUnknown` false when any runtime key exists) and emit complete groups or omit the group (Q3).
- **FR-018**: Malformed/repeated scalars MUST fall to defaults; malformed/repeated range keys MUST drop the whole group; enums exact-case except Q2 direction aliases; booleans read case-insensitively, emitted lowercase (Q3).
- **FR-019**: The grid MUST show supplied posters with a title-labelled placeholder at the same reserved aspect ratio for null or image-load error; populated/null posters proven via seeded real-DB rows; populated/null/broken images in RTL (Q4).
- **FR-020**: The delivery MUST replace the readiness `App.tsx` and its orphaned readiness test; install one `QueryClientProvider` in `main.tsx`; style the responsive view in `index.css`; add only `@tanstack/react-query` (Q5).
- **FR-021**: Fetching MUST use one browse query keyed by complete normalized filter/page state via the merged generated client with cancellation propagated; no per-tile fetches or prefetch (Q6).
- **FR-022**: Normalized-key changes MUST show reserved-size skeletons, never previous-key placeholder data; same-key background refetch MUST retain content with polite announcement (Q6).
- **FR-023**: The hand-written Library boundary MUST handle render/query failures, reset on query-key change, and offer Retry that resets/refetches the same canonical URL without a history entry; automatic query retries off; 422 is invalid-filter and is never retried, other failures including 400 are recoverable fetch error with retry available and no retry loop; the state is chosen by HTTP status, not by error-key vocabulary (Q6).
- **FR-024**: The URL adapter MUST own initial bookmark read, push/replace/popstate, and listener cleanup; App composes providers/boundary; LibraryView owns controls/grid/pagination (Q6).
- **FR-025**: Browse MUST live at the current SPA root with a responsive CSS poster grid reserving 2:3 media slots shared by images, placeholders, and skeletons (Q7).
- **FR-026**: Each tile MUST show title, year, runtime as `<n> min`, one-decimal IMDb badge, and exact enrichment-status chip labels; missing scalars use an em dash; no detail/play/watchlist-mutation controls (Q7).
- **FR-027**: Pagination MUST use `ceil(totalCount/pageSize)` with current page and result count, disable movement past known bounds, and keep a valid past-end page in the URL with empty content plus a return-to-first-page action (Q7).
- **FR-028**: Controls MUST be labelled with keyboard/focus-visible support and polite loading/status announcements (Q7).
- **FR-029**: Implementation MUST stay inside the Q8/Q9 file envelope: production `MovieSummary.cs`, `EfMovieCatalog.cs`, `LibraryResponses.cs`, conditional `Program.cs` EnrichmentStatus-scoped schema transformer only; compile-forced construction updates; regeneration via `LAMUFLIX_REGENERATE_OPENAPI=true` then DEV-320 merged tooling; never hand-edit generated files (Q8/Q9).
- **FR-030**: Backend tests MUST use real Testcontainers Postgres for translation/projection/serialization behavior with existing assertion/mocking conventions and no new test package; web tests MUST cover the Q8 RTL/Vitest list with table/generated adapter cases and no new property-testing dependency (Q8).
- **FR-031**: Gates MUST follow Q9: applicable diff-scoped Roslyn/complexity/InspectCode; mutation (no `since` in worktrees, no waiver); property tests under the Q9 amendment (no new C# URL round-trip tests; SPA adapter owns round-trip proof); vulnerable packages; format; `dotnet test`; `scripts/run-web-gates.ps1`; thresholds only from `harness.yml` (Q9).

### Key Entities

- **Browse tile**: poster (`posterUrl`, nullable) + title + year + runtime + IMDb rating + enrichment status; nulls render placeholder/em-dash fallbacks, never omitted wire members.
- **Normalized browse state**: all canonical filter values plus page/pageSize; the single query key, the URL source of truth, and the adapter mapping target for generated PascalCase API parameters.
- **Canonical browse URL**: always carries `sort`, `direction`, `page`, `pageSize`; lowercase keys; repeated keys for lists; complete runtime/year groups or omitted groups; aliases never emitted.
- **Paged browse result**: `items` + `totalCount`; pagination derives `ceil(totalCount/pageSize)`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Navigating to a URL with query params sets up the corresponding filters (ticket AC 1), covering every canonical key plus the approved aliases.
- **SC-002**: The grid renders a responsive layout with poster, title, year, runtime, IMDb badge, and status chip per tile (ticket AC 2 and Scope item 1).
- **SC-003**: The exact frozen wire shape carries all five added members with populated/null values and the four exact status strings asserted in backend tests.
- **SC-004**: No per-tile detail requests, no migration, no new join, no new dependency beyond ticket-named TanStack Query, no hand-edited generated file.
- **SC-005**: Applicable gates pass per the Q10 closing bar; any Critical/High finding, failed/unrunnable applicable gate, missing axis report, or contract mismatch blocks independently.

## Assumptions

All taste assumptions live in `specs/DEV-321/ASSUMPTIONS.md` (tagged `[assumed]`) and are not repeated here, except by reference: defaults (Q3), placeholder/boundary/retry/grid-format/control/pagination/accessibility taste per ASSUMPTIONS.md Q2–Q7 lines.
