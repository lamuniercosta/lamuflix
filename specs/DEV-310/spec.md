# Feature Specification: DEV-310 Minimal API Endpoints

**Feature Branch**: `feature/310-spec`

**Created**: 2026-10-06

**Status**: Draft

**Input**: Frozen `specs/DEV-310/brief.md`, `CONCLUSIONS.md` Q1-Q10, `ASSUMPTIONS.md`, `recon-DEV-310` sections 1-12, `specs/PRODUCT.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import a movie folder (Priority: P1)

A user imports a movie folder into the library through the API.

**Why this priority**: Import is the entry point; without it no library content exists to enrich, list, or play.

**Independent Test**: POST a valid `{folderPath}` and receive empty 202 with `Location: /api/movies/{newId}`; the new movie id resolves through the existing catalog.

**Acceptance Scenarios**:

1. **Given** a valid non-blank folder path without traversal, **When** POST `/api/movies/import` with `{folderPath}`, **Then** empty 202 with `Location: /api/movies/{newId}`.
2. **Given** a blank folder path or one containing traversal, **When** POST `/api/movies/import`, **Then** 422 ProblemDetails.
3. **Given** a malformed request body or binding, **When** POST `/api/movies/import`, **Then** framework 400 ProblemDetails.

---

### User Story 2 - Retry enrichment for a movie (Priority: P1)

A user requests enrichment for a movie that needs metadata.

**Why this priority**: Enrichment drives metadata quality; manual retry unblocks failed items.

**Independent Test**: POST to `/api/movies/{id}/enrichment` for an existing movie and receive empty 202 with `Location: /api/movies/{id}`.

**Acceptance Scenarios**:

1. **Given** an existing movie id, **When** POST `/api/movies/{id}/enrichment`, **Then** empty 202 with `Location: /api/movies/{id}` via `RequestEnrichmentCommand` only.
2. **Given** a nonpositive id, **When** POST `/api/movies/{id}/enrichment`, **Then** 422 ProblemDetails.
3. **Given** an unknown movie id, **When** POST `/api/movies/{id}/enrichment`, **Then** 404 ProblemDetails.

---

### User Story 3 - Manage the watchlist (Priority: P2)

A user adds a movie to and removes a movie from the watchlist.

**Why this priority**: Core library curation behavior built on existing domain transitions.

**Independent Test**: POST then DELETE `/api/movies/{id}/watchlist` each return empty 204; a repeated add or repeated remove returns 409.

**Acceptance Scenarios**:

1. **Given** an existing movie not on the watchlist, **When** POST `/api/movies/{id}/watchlist`, **Then** empty 204.
2. **Given** a movie already on the watchlist, **When** POST `/api/movies/{id}/watchlist` again, **Then** 409 ProblemDetails.
3. **Given** a movie on the watchlist, **When** DELETE `/api/movies/{id}/watchlist`, **Then** empty 204.
4. **Given** a movie not on the watchlist, **When** DELETE `/api/movies/{id}/watchlist`, **Then** 409 ProblemDetails.

---

### User Story 4 - Play a movie locally (Priority: P2)

A user starts local playback of a movie when the `Features:LocalPlay` flag is enabled.

**Why this priority**: Playback is the primary consumption path; flag-off behavior is a safety contract.

**Independent Test**: POST `/api/movies/{id}/play` on an enabled host returns empty 204 with a recorded process-starter call; on a disabled host with an existing movie returns 403 via `DisabledMediaPlayerLauncher` with zero process-starter calls (no process start).

**Acceptance Scenarios**:

1. **Given** an existing movie and LocalPlay enabled with a successful launch, **When** POST `/api/movies/{id}/play`, **Then** empty 204.
2. **Given** an existing movie and LocalPlay disabled, **When** POST `/api/movies/{id}/play`, **Then** 403 ProblemDetails and no process started.
3. **Given** an unknown movie id, **When** POST `/api/movies/{id}/play`, **Then** 404 regardless of the flag.
4. **Given** a nonpositive id, **When** POST `/api/movies/{id}/play`, **Then** 422 ProblemDetails.
5. **Given** a launch or configuration failure, **When** POST `/api/movies/{id}/play`, **Then** generic 500 ProblemDetails with no raw text.

---

### User Story 5 - Browse genre and people facets (Priority: P2)

A user lists stored genres and actor people for browsing.

**Why this priority**: Facet reads power browsing over the existing catalog tables.

**Independent Test**: GET `/api/genres` returns 200 with the stored `GenreDto` array; GET `/api/people?role=actor` returns 200 with the stored `PersonDto` array.

**Acceptance Scenarios**:

1. **Given** stored genres, **When** GET `/api/genres`, **Then** 200 array of `{id,name}` distinct by Id, sorted by Name then Id.
2. **Given** no stored genres, **When** GET `/api/genres`, **Then** 200 `[]`.
3. **Given** `role=actor`, **When** GET `/api/people?role=actor`, **Then** 200 array of `{id,name}` distinct by Id, sorted by Name then Id.
4. **Given** a missing, blank, duplicated, or unsupported role value (including `director`), **When** GET `/api/people`, **Then** 422 ProblemDetails.
5. **Given** two distinct entities sharing a Name, **When** either facet is read, **Then** both entities are preserved.

---

### Edge Cases

- Nonpositive movie ids return 422; malformed transport binding or body returns framework 400.
- Unknown movie ids return 404, including playback with the flag off (lookup-before-launch precedence).
- Repeated watchlist add or remove returns 409 via `InvalidTransitionException`.
- Unhandled failures return generic 500 ProblemDetails without exception text.
- Facet reads never merge actor and director identities; no pagination, search, movie-filter, or extra fields.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose POST `/api/movies/import` accepting `ImportMovieRequest {folderPath}`, validating through the `LibraryPath` guard, dispatching `ImportMovieFolderCommand`, returning empty 202 with `Location: /api/movies/{newId}`.
- **FR-002**: System MUST expose POST `/api/movies/{id}/enrichment` dispatching `RequestEnrichmentCommand` only, returning empty 202 with `Location: /api/movies/{id}`.
- **FR-003**: System MUST expose POST `/api/movies/{id}/watchlist` dispatching `AddToWatchlistCommand`, returning empty 204.
- **FR-004**: System MUST expose DELETE `/api/movies/{id}/watchlist` dispatching `RemoveFromWatchlistCommand`, returning empty 204.
- **FR-005**: System MUST expose POST `/api/movies/{id}/play` dispatching the existing decorated `PlayMovieCommand`, returning empty 204 on launch and 403 ProblemDetails when the movie exists and LocalPlay is disabled.
- **FR-006**: System MUST expose GET `/api/genres` returning 200 with a JSON array of `GenreDto(int Id, string Name)`, distinct by Id, all stored rows, sorted by Name then Id, `[]` when empty.
- **FR-007**: System MUST expose GET `/api/people?role=actor` requiring exactly one `actor` role value and returning 200 with a JSON array of `PersonDto(int Id, string Name)` under the same membership and sorting rules; any other role state returns 422 ProblemDetails.
- **FR-008**: System MUST map ids as int then `MovieId.TryCreate`: nonpositive to 422, malformed binding/body to framework 400, domain validation to 422, not found to 404, `InvalidTransitionException` to 409, `FeatureDisabledException` to 403, unhandled to generic 500, all as ProblemDetails through the single exception handler.
- **FR-009**: System MUST extend the existing `IMovieCatalog` with two read-only facet operations backed by read-only projections in its existing Infrastructure adapter over the existing genre/actor/director tables, with sealed `GetGenresQuery`/`GetPeopleQuery` records and sealed handlers in the existing Core Features/Library slice.
- **FR-010**: System MUST use TypedResults with complete response and error metadata on all seven routes, per-feature `MapXxxEndpoints` grouping, and existing decorated handler registration; facet handlers use the existing Tracing, Logging, Validation, handler order.
- **FR-011**: System MUST preserve lookup-before-launch precedence and the DEV-394 fail-closed baseline: configured executable first, no process start on unmapped format, `ArgumentList` only, no OS-association fallback, no new shell or process path, no launcher/config/flag/registration edits.
- **FR-012**: System MUST supply complete C# DTO and TypedResults metadata for all seven routes so DEV-20 generation produces a faithful document; committed OpenAPI document, Verify snapshot, and drift-check work remain deferred to DEV-20 per the Q6 owner approval and are never claimed green in DEV-310.

### Key Entities

- **ImportMovieRequest**: Request shape `{folderPath}` for the import endpoint.
- **GenreDto**: Facet item `{id, name}` with `int Id` and `string Name`.
- **PersonDto**: Facet item `{id, name}` with `int Id` and `string Name`.
- **Facet queries**: `GetGenresQuery` and `GetPeopleQuery` sealed records with sealed handlers in Core Features/Library.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Both verbatim ticket acceptance criteria hold: "All endpoints conform to the route table in section 6 of architecture-plan.md." and "Endpoints return exact HTTP status codes and headers." The concrete contract is the ticket route table plus constitution API rules; `architecture-plan.md` is absent (traceability context only).
- **SC-002**: All seven routes are proven at the real host covering success, exact statuses and headers, empty command bodies, scalar arrays, and the full error matrix including 409, 403, and fail-closed 500.
- **SC-003**: Facet reads over real Postgres preserve distinct Ids, same-name entities, Name-then-Id ordering, and empty-table `[]`.
- **SC-004**: Owner Q6 checkbox answered on the spec PR and Gate 1 merged; no OpenAPI drift claimed green inside DEV-310.

## Assumptions

- Facet arrays sort by Name then Id for deterministic display ([assumed] taste, ASSUMPTIONS.md).
- `architecture-plan.md` is absent; the ticket route table plus constitution API rules are the concrete contract (traceability context only).
- Director role support is out of scope; the role domain is actor-only per the ticket.
- `propertyTests: opt-out` — HTTP adapters and read projections introduce no new domain invariant; the existing property suite is retained.
- No new NuGet/npm dependency, project, top-level folder, architectural layer, schema change, or migration is introduced.
- No worker-side `ProcessEnrichmentCommand` endpoint, duplicate worker registration, `RecordEnrichmentFailureCommand` endpoint, or stranded-sweeper endpoint is added.
- No web, generation, snapshot, frontend, pagination, search, movie-filter, or launcher change is in scope.
