# Feature Specification: DEV-425 Director facet and Director filter

**Feature Branch**: `feature/425-spec`

**Created**: 2026-10-28

**Status**: Draft

**Input**: Ticket DEV-425 (parent DEV-100, `size:M`, `ui:`; depends DEV-411, DEV-322); frozen grill brief `specs/DEV-425/brief.md` (Q1–Q8, grill closed); `CONTEXT.md`; Patron rulings `CONCLUSIONS.md`; taste `ASSUMPTIONS.md`; Maestri notes `DEV-425`, `recon-DEV-425` (Q2 URL evidence; intake facts cited in brief/CONCLUSIONS); `specs/PRODUCT.md`. No decisions beyond the brief.

## Scope

**In scope** (ticket Scope; brief Q1–Q6):

- `GET /api/directors` with no required role parameter, returning `DirectorDto[]` of `{ id, name, movieCount }` (including `[]` when empty); only directors linked to at least one library movie.
- Existing query/handler/port architecture: `DirectorFacet` and `GetDirectorsAsync`, `GetDirectorsQuery`/`GetDirectorsQueryHandler`, `DirectorDto`, endpoint registration.
- `MovieQuery.DirectorId` (`int?`) included in equality and hashing; URL/API key singular `directorId`; API and C# `MovieQueryString` parse absent/empty as null, one valid int32 as select, duplicates/malformed/overflowing as failure (API 422, C# parse false); unknown/zero/negative int yields no matches without existence or positive-id validation.
- EF browse predicate via existing Directors navigation / `movie_directors` (`WhereDirector` with `Any`), AND-composed with all active browse predicates including watchlist, without duplicate movie rows.
- Searchable single-select Director control in both Library and Watchlist FilterPanel hosts; live-web raw scalar URL handling; page reset to 1 on select/replace/clear while preserving other filters; independent facet fetch per host; full-library counts independent of filters/watchlist.
- Contract chain: C# DTO → committed `web/src/api/openapi.json` → generated `web/src/api/types.ts` (and established mocks/typecheck path); web consumes generated types only.
- Matching tests and IMovieCatalog test-fake updates forced by the new port member.

**Out of scope** (ticket; brief Q1–Q7):

- Multi-director selection; genre/cast multi-select behavior changes.
- Wiring the unused strict `libraryUrl` parser; broad parser cleanup.
- Schema, migration, metadata ingestion, new NuGet/npm dependency, new project/layer/state library, LocalPlay, secrets, `Process.Start`, tracked-file deletion, broad rewrite.
- Endpoint search or pagination for directors.
- ADR (no new irreversible architecture decision for this M ticket).

**Precedence**: Ticket Scope & Technical Design is already approved authority. Brief Q1–Q7 (Q8 closure) govern delivery. Spec and plan must not contradict them. Gate 1 remains closed until the user merges the later spec PR. This draft authorizes no Build and claims no gate result.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Director facet for the library (Priority: P1)

A caller requests the director facet and receives every director credited on at least one library movie, each with id, name, and distinct linked-movie count. Same-name directors remain separate identities. The list is ordered by Name then Id. Empty libraries return an empty array. Counts always describe the full library.

**Why this priority**: Ticket-named facet endpoint; prerequisite for the Filter Panel control.

**Independent Test**: Against real PostgreSQL/Testcontainers, seed directors with and without movies (including shared names and shared movies), call `GET /api/directors` / catalog facet, assert field set, orphan exclusion, distinct counts, order, and empty array. No required role parameter.

**Acceptance Scenarios**:

1. **Given** directors linked to library movies, **When** `GET /api/directors` is called, **Then** HTTP 200 returns `DirectorDto[]` with `id`, `name`, and `movieCount` for those directors only (Q1, Q4).
2. **Given** a director with zero linked movies, **When** the facet is read, **Then** that director is excluded (Q1).
3. **Given** no linked directors, **When** the facet is read, **Then** the body is `[]` with HTTP 200 (Q1).
4. **Given** two directors with the same name and different ids, **When** the facet is read, **Then** both appear as separate identities with their own counts (Q1, Q4).
5. **Given** one movie credited to multiple directors, **When** counts are computed, **Then** that movie contributes once to each linked director's distinct count (Q4).
6. **Given** the facet list, **When** ordered, **Then** entries follow Name then Id via existing database ordering (Q4).
7. **Given** active browse filters or watchlist context, **When** the facet is fetched, **Then** counts remain full-library totals, independent of those filters (Q3, Q4).

---

### User Story 2 - Browse movies by a single director (Priority: P1)

A browse request may include singular `directorId`. Valid selection limits results to movies credited to that director via `movie_directors`, AND-composed with every other active predicate (including watchlist) without duplicate rows. Invalid scalars fail at the API/C# boundary; unknown identities return an empty match set.

**Why this priority**: Ticket-named `MovieQuery.DirectorId` + browse join; core filter semantics.

**Independent Test**: Core equality/hash and MovieQueryString parse/format (including seeded property roundtrips); real-Postgres browse for null passthrough, match, unmatched/zero/negative, AND with watchlist, no duplicate rows; API integration for filter and 422 failures.

**Acceptance Scenarios**:

1. **Given** absent or empty `directorId`, **When** MovieQueryString / API parse, **Then** `DirectorId` is null and no director predicate is applied (Q2).
2. **Given** one valid int32 `directorId`, **When** browse runs, **Then** only movies linked to that director via `movie_directors` are returned (Q2).
3. **Given** duplicate `directorId` keys, a malformed value, or an overflowing integer, **When** the API parses, **Then** HTTP 422; **When** C# MovieQueryString parses, **Then** parse fails (Q2).
4. **Given** an unknown, zero, or negative int32 `directorId`, **When** browse runs, **Then** zero matching movies; no existence or positive-id validator is added (Q2).
5. **Given** active watchlist and other browse predicates plus a director filter, **When** browse runs, **Then** predicates AND-compose and movie rows are not duplicated (Q3).
6. **Given** two `MovieQuery` values that differ only in `DirectorId`, **When** equality and hash are evaluated, **Then** they differ (Q2).

---

### User Story 3 - Searchable Director control in Library and Watchlist (Priority: P2)

A user opens Library or Watchlist FilterPanel, searches directors client-side, selects one director (replacing any prior selection), clears only the director or clears all filters including director, and sees the movie list and URL update. Reload and navigation restore `directorId`. Loading and fetch failures stay local to the director control.

**Why this priority**: Ticket-named Filter Panel combobox with URL `directorId`; `ui:` delivery across both hosts.

**Independent Test**: Vitest/RTL covering filterQuery scalar/first-wins/omission, browseParams forwarding, URL reload/navigation, page reset with other filters preserved, selection/replace/clear/Clear all, same-name suggestions, typing without predicate change, keyboard/ARIA, missing-selection fallback, independent loading/error/retry, both hosts; generated contract/typecheck.

**Acceptance Scenarios**:

1. **Given** either Library or Watchlist FilterPanel, **When** the panel renders, **Then** a Director single-select control is present and each host fetches the director facet independently (Q3, Q5).
2. **Given** the director list, **When** the user types in the search field, **Then** suggestions filter client-side with case-insensitive name substring match, deterministic name/id order, at most eight suggestions with movie-count badges; typing does not change the active director predicate (Q4, Q5).
3. **Given** no selection, **When** the user selects a director, **Then** that id becomes the sole `directorId`, other filters are preserved, and `page` resets to 1 (Q2, Q5).
4. **Given** a selected director, **When** the user selects another, **Then** the new id replaces the previous sole selection, other filters are preserved, and `page` resets to 1 (Q5).
5. **Given** a selected director, **When** the user activates Clear Director, **Then** only `directorId` clears (omitted from URL/API), other filters stay, and `page` resets to 1; **When** Clear all runs, **Then** `directorId` is included in the clear (Q5).
6. **Given** a URL with nonempty `directorId`, **When** the page reloads or navigates, **Then** the selection restores; web keeps the raw nonempty scalar (first occurrence wins on duplicates) and forwards it for API integer validation; empty clears and omits (Q2).
7. **Given** a selected id absent from facet results (including during loading or fetch error), **When** the control renders, **Then** selection remains selected and clearable with `Director <id>` fallback; the predicate is not silently removed (Q3, Q5).
8. **Given** facet loading or failure, **When** the director control updates, **Then** suggestions are disabled, other filters stay usable, copy shows `Loading directors` or `Could not load directors` with Retry via existing query refetch (Q5).
9. **Given** keyboard use, **When** the user navigates the combobox, **Then** Arrow keys move, Enter selects, Escape closes, focus is visible, label is associated, and combobox/listbox/option semantics include selected and expanded state (Q5).

---

### Edge Cases

- Director identity is distinct from actor identity even when names match (CONTEXT.md; Q1).
- Malformed web `directorId` is preserved as a raw scalar and forwarded; the API is authoritative for int validation (Q2). Do not wire dead `libraryUrl` or clean up other parsers.
- Fractional or signed web scalars that survive lenient coercion still forward; API/C# reject non-int32 forms as specified for the strict boundary (Q2).
- Empty search results show `No directors found` (ASSUMPTIONS.md).
- Palette supplies responsive placement and visual details within settled semantics before plan challenge; design cannot alter Q1–Q6 behavior (Q5, Q7).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `GET /api/directors` MUST return HTTP 200 and `DirectorDto[]` `{ id, name, movieCount }`, including `[]` when empty; MUST require no role parameter; MUST include only directors linked to at least one library movie (Q1).
- **FR-002**: Facet projection MUST use existing architecture: `DirectorFacet` / `GetDirectorsAsync` on `IMovieCatalog`, `GetDirectorsQuery` / handler, `DirectorDto`, FacetEndpoints registration and HandlerRegistration (Q1, Q6).
- **FR-003**: Facet counts MUST be distinct linked library movies per director, independent of active filters/watchlist; same-name identities MUST remain separate; order MUST be Name then Id via existing database ordering (Q3, Q4).
- **FR-004**: `MovieQuery` MUST gain `int? DirectorId` included in equality and hashing; URL/API key MUST be singular `directorId` (Q2).
- **FR-005**: API and C# `MovieQueryString` MUST treat absent/empty as null; one valid int32 as select; duplicate occurrences, malformed and overflowing integers as failure (API 422, C# parse false). Unknown, zero, or negative int32 MUST yield no matching movies without existence or positive-id validation (Q2).
- **FR-006**: Persistence MUST apply `WhereDirector` via existing Directors navigation / `Any` over `movie_directors` without schema or ingestion change; predicate MUST AND-compose with all active browse predicates including watchlist without duplicate movie rows (Q3, Q6).
- **FR-007**: BrowseMoviesRequest / LibraryEndpoints MUST map `directorId` through existing validation/problem handling and cancellation conventions (Q2, Q6).
- **FR-008**: Contract chain MUST regenerate `web/src/api/openapi.json` then `web/src/api/types.ts` (and established mocks/typecheck); MUST NOT hand-edit generated files; web MUST consume generated DirectorDto and browse-parameter types with no duplicate DTO or cast workaround (Q6).
- **FR-009**: Live web MUST preserve raw nonempty `directorId` strings, first occurrence wins on duplicate URL keys, serialize/forward the scalar to the API, and omit on empty clear; MUST NOT wire unused `libraryUrl` or perform broad parser cleanup (Q2).
- **FR-010**: Director selection, replacement, and clearing MUST reset `page` to 1 and preserve all other filter values; `directorId` MUST participate in URL recognition/serialization and API browse parameters; valid URL reload and navigation MUST restore it (Q2).
- **FR-011**: Both Library and Watchlist FilterPanel hosts MUST expose the Director control; each host needing the facet MUST fetch it independently through the existing query architecture (Q3).
- **FR-012**: Combobox MUST be the existing dependency-free pattern: selecting replaces the sole director; Clear Director clears only `directorId`; Clear all includes `directorId`; client-side case-insensitive substring search with deterministic name/id ordering and at most eight suggestions; movie-count badges; selection retained separately from search text (Q4, Q5).
- **FR-013**: Taste copy MUST use ASSUMPTIONS.md: label `Director`, placeholder `Search directors`, empty `No directors found`, `Clear Director`, `Loading directors`, `Could not load directors`, `Retry`, missing-selection `Director <id>` (Q4, Q5).
- **FR-014**: Accessibility MUST support Arrow navigation, Enter selection, Escape close, visible focus, associated label, and combobox/listbox/option semantics including selected and expanded state (Q5).
- **FR-015**: Loading and failures MUST be local to the director control; other filters remain usable; suggestions disabled while loading/error; selection retained and clearable; Retry via existing query refetch (Q5).
- **FR-016**: Implementation MUST follow brief Q6 file boundary and order only; no new dependency, project, layer, migration, schema/ingest, LocalPlay, secrets, Process.Start, tracked-file deletion, or broad rewrite; genre/cast behavior MUST NOT change as part of these edits (Q6, Q7).
- **FR-017**: Tests MUST prove behavior per brief Q6 (Postgres facet/browse, Core equality/hash/parse/property roundtrips, API integration, web Vitest/RTL including both hosts and generated contract/typecheck). Do not mock query translation. Update catalog test fakes only as compilation requires (Q6).
- **FR-018**: Loop discipline: Critical/High/Medium block code-review/ship-review; Low recorded nonblocking; two review rounds max and at most two fix commits per round; third-round unresolved above-bar finding escalates on the PR; frozen scope is Q1–Q6 only; all three Sentry/Ledger/Compass axes required for M; no threshold lowering; mutation via `run-mutation.ps1` never Stryker-from-worktree; scope-empty exit-2 is SKIPPED never PASS (Q7; brief Gate expectations).

### Key Entities

- **Director**: A person credited with directing a movie; identity distinct from actor identity even when names match (`CONTEXT.md`).
- **Director facet**: Full-library list of directors with at least one linked movie; items carry `id`, `name`, `movieCount`.
- **Director filter**: Singular `DirectorId` / `directorId` single-select browse filter over that identity.
- **MovieQuery.DirectorId**: Optional int filter applied through `movie_directors`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `GET /api/directors` returns the exact field set with orphan exclusion, empty-array success, distinct counts, same-name separation, and Name-then-Id order (Q1, Q4).
- **SC-002**: Browse with valid `directorId` matches via `movie_directors`; invalid scalars yield API 422 / C# parse false; unknown/zero/negative yield empty matches; AND composition with watchlist produces no duplicate rows (Q2, Q3).
- **SC-003**: Generated OpenAPI and TypeScript expose DirectorDto and browse `directorId` without hand edits or hand-rolled web DTOs (Q6).
- **SC-004**: Library and Watchlist hosts both offer the Director control with URL roundtrip, page reset on director change, Clear Director vs Clear all, and independent loading/error/retry (Q2, Q3, Q5).
- **SC-005**: Combobox search, eight-suggestion cap, count badges, keyboard/ARIA, and missing-selection fallback behave per acceptance scenarios (Q4, Q5).
- **SC-006**: Applicable backend and web gates pass at their pipeline stages without threshold changes; SKIPPED/SKIP/N/A/Could not run never reported as PASS (brief Gate expectations).

## Assumptions

- Taste defaults tagged `[assumed]` in `specs/DEV-425/ASSUMPTIONS.md` (Q4, Q5).
- Directors and `movie_directors` already exist from DEV-411 / Initial; no migration (brief Q6; intake recon cited in CONCLUSIONS).
- Palette `design.md` (required before plan challenge on this `ui:` ticket) may settle presentation within settled semantics and MUST NOT alter Q1–Q6 behavior (Q5, Q7).
- Clarify/checklist stages apply to L tickets only under live task-pipeline; this M ticket does not run them (brief Gate expectations).
- Phase A drafting runs no production/test gates and claims no implementation verification (brief).

## Clarifications

All grill categories were resolved by Patron Q1–Q7 before drafting; Q8 confirmed faithful transcription. No `[NEEDS CLARIFICATION]` markers. Live task-pipeline assigns clarify/checklist to L only.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket Scope (facet, DirectorId, combobox, URL) | US1–US3; FR-001–FR-012; SC-001–SC-005 |
| Q1 Facet contract | US1; FR-001–FR-003; SC-001 |
| Q2 Query/URL split | US2–US3; FR-004–FR-005, FR-009–FR-010; SC-002, SC-004 |
| Q3 Hosts/composition/missing selection | US2–US3; FR-006, FR-011, FR-015; SC-002, SC-004 |
| Q4 Counts/search/taste | US1, US3; FR-003, FR-012–FR-013; SC-001, SC-005 |
| Q5 Interaction/a11y/states | US3; FR-012–FR-015; SC-004–SC-005 |
| Q6 Approach/files/tests/gates | FR-002, FR-007–FR-008, FR-016–FR-017; SC-003, SC-006 |
| Q7 Loop discipline / frozen scope | FR-018; Scope out-of-scope |
| Q8 Closure | Status Draft; Gate 1 user-owned |
