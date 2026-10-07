# Tasks: DEV-310 Minimal API Endpoints

**Input**: Design documents from `specs/DEV-310/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), brief.md, CONCLUSIONS.md Q1-Q10

**Tests**: WebApplicationFactory host tests for all seven routes, facet handler unit tests, and real persistence tests are required by the brief Q8 matrix and are included below.

**Organization**: Tasks are grouped so every Anvil handoff passes tests independently. Order is facet, then command and error arms, then wiring, then host proof. Foundational focused tests (T008-T009) pass in Phase 2. Story slices in Phases 3-7 add implementation only and pass with no new failing host tests. All full-host assertions (T010, T013, T015, T018, T021) are written in the post-wiring proof phase and pass there with T024 after T023 wiring. Cog refactor (T025) and Gauge gates (T026) sit outside the Anvil code phases.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Phase A draft and analyze context only; no code setup runs here

- [ ] T001 Confirm `specs/DEV-310/brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `spec.md`, `plan.md` are present as Phase A decision inputs. Implementation prerequisites before any code task: Gate 1 merged, Conductor and Rigger-provisioned Phase B worktree `F:/Dev/LamuFlix.worktrees/DEV-310` on branch `feature/DEV-310`, completed Wisp pickup drift check, and Keel re-analysis. No delivery worktree receipt is claimed here; Anvil never performs recon.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Facet contract and catalog reads that every endpoint story builds on (brief ordering step 1)

**CRITICAL**: No user story work can begin until this phase is complete

- [ ] T002 [Foundation] Extend `src/LamuFlix.Core/Ports/IMovieCatalog.cs` with two read-only facet operations
- [ ] T003 [P] [Foundation] Create sealed `GetGenresQuery` record and sealed handler under `src/LamuFlix.Core/Features/Library/`
- [ ] T004 [P] [Foundation] Create sealed `GetPeopleQuery` record and sealed handler under `src/LamuFlix.Core/Features/Library/`
- [ ] T005 [Foundation] Add read-only facet projections to the existing Infrastructure catalog adapter owning `IMovieCatalog`
- [ ] T006 [P] [Foundation] Add `GenreDto` file under `src/LamuFlix.Api/Endpoints/` with `int Id, string Name`
- [ ] T007 [P] [Foundation] Add `PersonDto` file under `src/LamuFlix.Api/Endpoints/` with `int Id, string Name`
- [ ] T008 [P] [Foundation] Add facet handler unit tests under `tests/LamuFlix.UnitTests/Features/Library/`
- [ ] T009 [Foundation] Add real Postgres Testcontainers facet tests under `tests/LamuFlix.IntegrationTests/` covering translation, distinct Id, same-name preservation, Name-then-Id ordering, empty tables

**Checkpoint**: Foundation ready - story slices are written sequentially in priority order

---

## Phase 3: User Story 1 - Import a movie folder (Priority: P1)

**Goal**: POST `/api/movies/import` validates `{folderPath}` through the `LibraryPath` guard, dispatches `ImportMovieFolderCommand`, returns empty 202 with `Location: /api/movies/{newId}`

**Independent Test**: WebApplicationFactory POST with valid, blank or traversal, and malformed bodies verifies 202 with Location, 422, and framework 400 in the Phase 8 proof phase after T023 wiring (T010 with T024)

### Implementation for User Story 1

- [ ] T011 [P] [US1] Create `ImportMovieRequest` file under `src/LamuFlix.Api/Endpoints/` with `folderPath`
- [ ] T012 [US1] Fill `src/LamuFlix.Api/Endpoints/ImportEndpoints.cs` with the import route, TypedResults, and complete response and error metadata

**Checkpoint**: User Story 1 slice is written and the handoff passes with no new failing host tests; host proof waits for Phase 8 after T023 wiring

---

## Phase 4: User Story 2 - Retry enrichment (Priority: P1)

**Goal**: POST `/api/movies/{id}/enrichment` dispatches `RequestEnrichmentCommand` only and returns empty 202 with `Location: /api/movies/{id}`

**Independent Test**: WebApplicationFactory POST for existing, nonpositive, and unknown ids verifies 202 with Location, 422, and 404 in the Phase 8 proof phase after T023 wiring (T013 with T024)

### Implementation for User Story 2

- [ ] T014 [US2] Create `EnrichmentEndpoints` file under `src/LamuFlix.Api/Endpoints/` with TypedResults and complete metadata

**Checkpoint**: User Story 2 slice is written and the handoff passes with no new failing host tests; host proof waits for Phase 8 after T023 wiring

---

## Phase 5: User Story 3 - Watchlist add and remove (Priority: P2)

**Goal**: POST and DELETE `/api/movies/{id}/watchlist` dispatch the existing watchlist commands and return empty 204, with repeated transitions mapped to 409

**Independent Test**: WebApplicationFactory POST and DELETE including repeated add and repeated remove verifies 204 and 409 in the Phase 8 proof phase after T023 wiring (T015 with T024)

### Implementation for User Story 3

- [ ] T016 [US3] Create `WatchlistEndpoints` file under `src/LamuFlix.Api/Endpoints/` with TypedResults and complete metadata
- [ ] T017 [US3] Add exactly the `InvalidTransitionException` to 409 arm in `src/LamuFlix.Api/ExceptionHandling/ValidationExceptionHandler.cs`

**Checkpoint**: Watchlist slice is written with 409 on repeats and the handoff passes with no new failing host tests; host proof waits for Phase 8 after T023 wiring

---

## Phase 6: User Story 4 - Local playback (Priority: P2)

**Goal**: POST `/api/movies/{id}/play` dispatches the existing decorated `PlayMovieCommand` with lookup-before-launch precedence and the DEV-394 fail-closed baseline

**Independent Test**: WebApplicationFactory POST with flag on and off, unknown and nonpositive ids, and failure injection verifies 204, 403 with zero process-starter calls (no process start) via `DisabledMediaPlayerLauncher`, 404, 422, and generic 500 in the Phase 8 proof phase after T023 wiring (T018 with T024)

### Implementation for User Story 4

- [ ] T019 [US4] Create `PlaybackEndpoints` file under `src/LamuFlix.Api/Endpoints/` with TypedResults and complete metadata
- [ ] T020 [US4] Add exactly the `FeatureDisabledException` to 403 arm in `src/LamuFlix.Api/ExceptionHandling/ValidationExceptionHandler.cs`

**Checkpoint**: Playback slice is written with flag-off 403 and fail-closed 500 and the handoff passes with no new failing host tests; host proof waits for Phase 8 after T023 wiring

---

## Phase 7: User Story 5 - Genre and people facets (Priority: P2)

**Goal**: GET `/api/genres` and GET `/api/people?role=actor` return scalar arrays with the frozen membership, sorting, and error rules

**Independent Test**: WebApplicationFactory GETs verify 200 arrays, `[]` on empty, and 422 for missing, blank, duplicate, or unsupported role in the Phase 8 proof phase after T023 wiring (T021 with T024)

### Implementation for User Story 5

- [ ] T022 [US5] Create `FacetEndpoints` file under `src/LamuFlix.Api/Endpoints/` with TypedResults and complete metadata

**Checkpoint**: All story slices are written and each handoff passes with no new failing host tests; full-host proof waits for Phase 8 after T023 wiring

---

## Phase 8: Wiring and Host Proof

**Purpose**: Feature-group wiring and full host proof (brief ordering steps 3-4); the single phase where full-host assertions are written and pass

- [ ] T023 Add feature-group wiring in `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` and explicit facet decorated registrations in `src/LamuFlix.Api/HandlerRegistration.cs`
- [ ] T010 [US1] Host test for import success and error matrix in `tests/LamuFlix.IntegrationTests/` (written here after T023 wiring; passes here)
- [ ] T013 [US2] Host test for enrichment success and error matrix in `tests/LamuFlix.IntegrationTests/` (written here after T023 wiring; passes here)
- [ ] T015 [US3] Host test for watchlist matrix in `tests/LamuFlix.IntegrationTests/` (written here after T023 wiring; passes here)
- [ ] T018 [US4] Host test for playback matrix in `tests/LamuFlix.IntegrationTests/` using the real flag registration with a recording process seam and never a real process (written here after T023 wiring; passes here)
- [ ] T021 [US5] Host test for facet matrix in `tests/LamuFlix.IntegrationTests/` (written here after T023 wiring; passes here)
- [ ] T024 Prove all seven routes at the real host plus infrastructure-dependent behavior with real database and broker tests and safe process seams. Passing checkpoint for every story host test (T010, T013, T015, T018, T021) lands here, after T023 wiring and facet registrations.

**Checkpoint**: All seven routes proven passing at the real host after wiring

---

## Phase 9: Refactor (Cog-owned, outside Anvil code phases)

**Purpose**: Whole-diff refactor after all implementation phases are committed (brief ordering step 5, first half)

- [ ] T025 [Cog] Refactor the complete delivery diff; no worker redesign or OpenAPI infrastructure implementation

---

## Phase 10: Gates (Gauge-owned, outside Anvil code phases)

**Purpose**: Full Phase B gate set on the refactored diff (brief ordering step 5, second half)

- [ ] T026 [Gauge] Run the full Phase B gate set on the refactored diff

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phases 3-7)**: All depend on Foundational phase completion
  - Slices are written sequentially in priority order (P1 to P2); each slice handoff passes with no new failing host tests
  - Full-host assertions for all stories are written and pass in Phase 8 after T023
- **Wiring and Host Proof (Phase 8)**: Depends on all story slices being written; writes and passes every story host test after T023 wiring
- **Refactor (Phase 9, Cog)**: Depends on all implementation phases being committed; whole-diff refactor only
- **Gates (Phase 10, Gauge)**: Depends on refactor completion; full gate set on the refactored diff

### Within Each User Story

- Queries and DTOs before endpoints
- Core implementation before integration
- Slice written before moving to the next priority; host proof for every slice waits for Phase 8 after T023 wiring

### Out of Scope

- Committed OpenAPI document, Verify snapshot, drift check, and generated client work remain deferred to DEV-20 per the Q6 owner approval
- No director role, pagination, search, movie-filter, worker redesign, frontend, schema, dependency, layer, or launcher change

---

## Notes

- Phase B starts only after Gate 1 is merged, in the Conductor and Rigger-provisioned Phase B worktree `F:/Dev/LamuFlix.worktrees/DEV-310` on branch `feature/DEV-310`, with a completed Wisp pickup drift check and Keel re-analysis before code tasks. No delivery worktree receipt is claimed here.
- `propertyTests: opt-out` - HTTP adapters and read projections introduce no new domain invariant; the existing property suite is retained
- Gate dispositions stay honest: SKIPPED (scope-empty), SKIP (configured opt-out), NOT APPLICABLE (script-emitted) are never PASS; Could not run and gate failures block
