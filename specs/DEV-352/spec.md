# Feature Specification: DEV-352 Explicit watched and not-interested marks

**Feature Branch**: `feature/352-spec`

**Created**: 2026-10-17

**Status**: gate1: provisional

**Input**: Ticket DEV-352 (size M, parent DEV-288); settled grill brief `specs/DEV-352/brief.md` (grill closed 8/12, supplemented by Q9–Q12); facts from `recon-DEV-352`; Patron rulings in `specs/DEV-352/CONCLUSIONS.md`. Decisions include the Q12 plan-challenge supplement in `brief.md`. Q11 leaves two structural owner checkboxes unresolved. Gate 1 stays closed.

## User Scenarios & Testing *(mandatory)*

Priority labels order independent tests. The brief does not allow any of these stories to be dropped. Together they are the frozen scope.

### User Story 1 - Mark and unmark watched and not interested (Priority: P1)

The single user explicitly marks a library movie as watched or as not interested, and explicitly removes either mark. Watched means the movie was explicitly marked seen. Not interested means an explicit negative preference. The two marks are independent of each other and of the existing watchlist. Repeating a mark or an unmark observed already in effect at load is a conflict (Q12). An unknown movie is not found. An id that is not a positive integer is rejected.

**Why this priority**: This is the ticket. The four operations are the delivery. Later stories make those marks observable and safe for movies that already exist. They are not optional follow-ups.

**Independent Test**: With a stand-in store, call each of the four operations on a known movie, on a movie already in the requested state, on an unknown positive id, on a nonpositive id, and on a malformed or overflowing id. Confirm the status, the empty success body, and whether the store was saved.

**Acceptance Scenarios**:

1. **Given** a movie whose watched mark is false, **When** the user marks it watched, **Then** the mark becomes true, the response is empty 204, and the movie is saved once.
2. **Given** a movie whose watched mark is true, **When** the user removes that mark, **Then** the mark becomes false, the response is empty 204, and the movie is saved once.
3. **Given** a movie whose not-interested mark is false, **When** the user marks it not interested, **Then** the mark becomes true, the response is empty 204, and the movie is saved once.
4. **Given** a movie whose not-interested mark is true, **When** the user removes that mark, **Then** the mark becomes false, the response is empty 204, and the movie is saved once.
5. **Given** a movie observed already in the state a mark or unmark would request when loaded, **When** the user repeats that operation, **Then** the response is 409 Conflict problem details carrying that operation's method-name action and const state token, nothing is saved, and both marks and the watchlist stay as they were. The pairs are MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, and UnmarkNotInterested/Interested. Interested means only that the not-interested mark is false.
6. **Given** any combination of watched, not interested, and watchlist, **When** the user changes exactly one of those three, **Then** the other two stay as they were.
7. **Given** a positive id that matches no movie, **When** the user marks or unmarks either preference, **Then** the response is 404 and nothing is saved.
8. **Given** a nonpositive integer id, **When** the user calls any of the four operations, **Then** the response is 422 and the errors name id.
9. **Given** a malformed or overflowing integer id, **When** the user calls any of the four operations, **Then** the response is 400.

---

### User Story 2 - Existing browse and details show the marks (Priority: P2)

After a mark changes, the user sees it on the movie list and on the movie details they already use. No new page, no new list filter, and no on-screen control is added.

**Why this priority**: A mark that cannot be read back is not observable. The brief adds the two marks to the existing list and details responses and adds nothing else to read.

**Independent Test**: Save a movie with each combination of the two marks, read the existing list and the existing details, and confirm both marks appear and no new read route or filter exists.

**Acceptance Scenarios**:

1. **Given** a movie with either or both marks set, **When** the existing movie list is read, **Then** each item includes whether it is watched and whether it is not interested.
2. **Given** that movie, **When** its existing details are read, **Then** the details include the same two marks.
3. **Given** the list and details responses, **When** they are compared with the previous field set, **Then** the only additions are those two required marks.
4. **Given** the existing list filters, **When** the user browses, **Then** no watched or not-interested filter is added.

---

### User Story 3 - Existing movies survive the upgrade and the downgrade (Priority: P3)

Movies already in the library gain both marks as false. Their other data stays. Removing the change drops only the two new marks and leaves the previous data in place.

**Why this priority**: The brief requires the marks to be stored on the existing movie and to be reversible. This is part of the same frozen scope, not a later migration task.

**Independent Test**: Against a real database, upgrade a library that already has movies, read the new marks, change a mark and the watchlist in both directions, then downgrade and confirm the previous data remains.

**Acceptance Scenarios**:

1. **Given** movies stored before this change, **When** the upgrade is applied, **Then** each movie gains watched false and not interested false, and existing rows and their other data remain.
2. **Given** a movie after that upgrade, **When** it is saved and loaded again, **Then** both marks and the watchlist round-trip.
3. **Given** a movie on the watchlist, **When** either mark changes, **Then** the watchlist membership stays. **Given** a marked movie, **When** watchlist membership changes, **Then** both marks stay.
4. **Given** the upgrade, **When** it is rolled back, **Then** only the two new marks are removed, the previous schema and previous data remain, and the removed mark values are not kept.

### Edge Cases

- Both marks may be true at the same time. All eight combinations of watched, not interested, and watchlist are valid.
- Changing one of those three never changes the other two, in either direction.
- A repeated mark or unmark that observes committed target state is a conflict. Overlapping same-target requests that both load a valid pre-state may both succeed with 204; no row-level serialisation is promised (Q12). Concurrent changes to different flags preserve one another through per-field delta writes.
- A positive unknown id is not found. Zero and negative ids are validation failures that name id. A non-integer or overflowing id is a bad request.
- New movies start with both marks false. Restoring a stored movie restores the stored marks.
- The marks are not an enrichment state, a watch history, a timestamp, a ranking, or a recommendation.
- Marking a movie does not remove it from the watchlist, and the watchlist does not clear either mark.
- No separate store of feedback is added. No user identity is added.
- No list filter, recommendation behavior, on-screen control, or new read route is added.
- A repeated transition carries a method-name action and a const state token: MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, and UnmarkNotInterested/Interested. Interested means only `IsNotInterested == false`, not a positive preference. That wording is the `[assumed]` entry in `specs/DEV-352/ASSUMPTIONS.md` (Q9).
- Upgrade and downgrade are proved in the existing `tests/LamuFlix.IntegrationTests/MigrationTests.cs`. Each proof uses its own database from `PostgresFixture.CreateEmptyDatabaseContextAsync`. The shared fixture database is never downgraded (Q10).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A movie MUST carry two required, non-null marks, watched and not interested, both initially false. Either mark MUST be allowed to be true while the other is true. All eight combinations of watched, not interested, and watchlist MUST be valid.
- **FR-002**: The user MUST be able to mark watched, remove watched, mark not interested, and remove not interested as four separate operations. A legal change flips only that mark. A repeat of a mark or unmark observed already in effect at load MUST be rejected as a conflict and MUST NOT save. Changing one mark MUST leave the other mark and the watchlist unchanged, and existing watchlist changes MUST leave both marks unchanged, including stale-load sibling-write interleaves. Same-target requests that both loaded a valid pre-state may both succeed; a repeat that observes committed target state conflicts (Q12).
- **FR-003**: A conflict from a repeated transition MUST throw `InvalidTransitionException` with the method name as the action and a const string state token, and the HTTP 409 problem details MUST carry that same pair. The pairs are MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, and UnmarkNotInterested/Interested. Interested means only `IsNotInterested == false`, not positive preference. Domain tests and HTTP 409 tests MUST assert these exact pairs (Q9).
- **FR-004**: The system MUST NOT add timestamps, watch history, recency, ranking, recommendation filtering, implicit watchlist removal, or a link between these marks and enrichment state. It MUST NOT add a user identity.
- **FR-005**: The two marks MUST be stored as two required boolean columns, `is_watched` and `is_not_interested`, on the existing movies table, defaulting to false for movies that already exist. There MUST NOT be a separate feedback table. The marks MUST round-trip through the movie, its stored record, restore, persistence mapping, and the list and details projections.
- **FR-006**: Storage MUST be added by one new migration generated with the existing tooling, plus the generated model snapshot. The existing initial migration MUST NOT be edited. Generated migration artifacts MUST NOT be hand-edited. Upgrade MUST keep existing rows, data, and flags, and MUST set both new marks false on preexisting rows. Downgrade MUST drop only the two new columns, MUST keep the previous schema and data, and MUST NOT retain the removed mark values. Generating that additive migration without a constitution amendment or ADR is an unresolved structural owner checkbox below. Q11 supersedes the historical Q6 statement that this is not a departure. This requirement does not approve the departure.
- **FR-007**: The public operations MUST be exactly these four, with an untyped movie id bound as an integer, no request body, and no success body:

  | Method and route | Operation |
  | --- | --- |
  | POST `/api/movies/{id}/watched` | MarkWatched |
  | DELETE `/api/movies/{id}/watched` | UnmarkWatched |
  | POST `/api/movies/{id}/not-interested` | MarkNotInterested |
  | DELETE `/api/movies/{id}/not-interested` | UnmarkNotInterested |

- **FR-008**: Each operation MUST return 204 with an empty body and one save on a legal change; 409 Conflict problem details and no save when the loaded movie is already in the requested state (Q12); 404 and no save when the positive id matches no movie; 422 with errors containing id and no save when the integer id is not positive; and 400 and no save when the id is malformed or overflows. Those no-save results apply to every one of the four operations. The route id MUST be validated with `MovieId.TryCreate` at the endpoint boundary. The existing validation decorator MUST stay in place through handler registration. Separate `IValidator<TCommand>` classes for these four MovieId-only commands are omitted only as the unresolved structural owner checkbox below. That omission is pending user approval. The existing error mapping MUST be reused. No second mapping mechanism is added.
- **FR-009**: The existing movie list and movie details MUST include required non-null watched and not-interested marks, exposed as `isWatched` and `isNotInterested`. No new read route and no new browse filter are added.
- **FR-010**: The contract chain MUST stay C# response types, then the committed OpenAPI document exported from that C# contract, then generated outputs. Export `web/src/api/openapi.json` with the existing C# contract flow first. `npm run generate:api` reads that committed document and writes `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. That command does not export OpenAPI. Neither generated file may be hand-edited. The OpenAPI verification baseline and the exact response field-set checks MUST be updated. The original Q11 forced compatibility edits are limited to `web/src/features/movie-details/movieDetailsApi.ts`, `web/src/api/client.typecheck.ts`, `web/src/mocks/fixtures.ts`, `web/src/mocks/handlers.test.ts`, and `web/src/mocks/generate-api.test.ts`. That generation test MUST replace its ten-operation lock with those ten plus the four approved operation identities (14) and MUST keep the existing byte and staleness checks (Q11; replacement recon-DEV-352:13-34,38-68). No on-screen control, client abstraction, or handwritten duplicate of the wire types is added.
  Phase 6 amendment to FR-010: Patron Phase 6 ruling (brief.md Phase 6 supplement; CONCLUSIONS.md; recon-DEV-352-2:1-44) additionally authorizes exactly `web/src/library/libraryQueries.ts`, `web/src/api/client.test.ts`, `web/src/App.test.tsx`, `web/src/library/libraryQueries.polling.test.tsx`, `web/src/library/LibraryView.retry.test.tsx`, `web/src/library/MovieCard.layout.test.tsx`, `web/src/library/MovieCard.test.tsx`, `web/src/library/MovieGrid.test.tsx`, `web/src/library/watchlist.integration.test.tsx`, `web/src/library/watchlistListFilter.test.tsx`. In these ten files only, add `isWatched: false` and `isNotInterested: false` to `pendingRow` and existing response literals/factory defaults, retaining existing override spreads. No UI controls, branching logic or new abstractions. `web/src/features/movie-details/MovieDetailsView.tsx` and `web/src/test/handlers.ts` remain excluded from direct edits: fix their transitive errors through the already-authorized `movieDetailsApi.ts` schema and `mocks/fixtures.ts`. Any required direct edit there needs new recon and a new Patron ruling. T025 separate verdicts and the green T016-T021 commit group remain unchanged.
- **FR-011**: The work MUST follow the existing watchlist slice: four sealed command and handler pairs under the existing Core features area, named MarkWatched, UnmarkWatched, MarkNotInterested, and UnmarkNotInterested, plus matching movie transition methods and one feedback endpoint group wired into the existing endpoint and handler registration. It MUST reuse the existing movie repository read and save, the existing empty result, and the existing exception types and libraries.
- **FR-012**: The system MUST NOT add a dependency, a project, a top-level folder, a port, a repository or service abstraction, a state library, a local-playback change, a process launch, or a secret. It MUST NOT delete or rewrite a file outside the bounded edits the brief authorizes. It MUST NOT rewrite the root glossary or add an architecture decision record.
- **FR-013**: Proof MUST cover the domain transitions and all eight combinations, including the exact Q9 action and state pairs, handler success and the no-save failures, and the HTTP matrix for all four operations. Unknown positive ids (404), nonpositive ids (422), and malformed or overflowing ids (400) MUST assert zero saves on every operation. Proof MUST cover all eight flag combinations through real-database save, fresh reload, browse and details, stale-load sibling-write interleaves including watchlist, allowed same-target overlap and observed-state repeat conflicts, explicit handler cancellation-token forwarding, and real-database round-trip and migration in both directions in `tests/LamuFlix.IntegrationTests/MigrationTests.cs` under the Q10 isolation and tripwire rules, property coverage of feedback and watchlist isolation, the exact field-set checks, and the OpenAPI baseline. Applicable web proof MUST be scheduled as separate verdicts: web typecheck, runtime-schema and contract regression tests, web tests, web build, generated-output drift checks, and the generator and generated-handler regressions in `web/src/mocks/generate-api.test.ts`. Lint alone does not schedule that set. A stand-in repository is only for orchestration. Database translation and serialization MUST use a real database. Existing test libraries only.
- **FR-014**: Verification MUST use the live harness for applicability and thresholds: build and tests, Roslyn analyzers, cyclomatic complexity, InspectCode, property tests, vulnerable packages, formatting, OpenAPI snapshot and drift, and changed-project mutation without a since limit. The web checks named in FR-013 MUST each receive their own verdict, using existing documented tooling. No command flag or threshold is invented here. Running those checks is implementation-stage work, not this drafting round. Thresholds MUST NOT be lowered. Generated outputs MUST NOT be hand-edited. A check that could not run is not a pass. Pass, failure, scope-empty skip, opt-out skip, and mutation not-applicable stay distinct.

### Key Entities

- **Movie**: The single-user library movie. It gains two independent marks, watched and not interested, beside the existing watchlist membership. Both new marks start false. Enrichment state stays a separate fact.
- **Movies table**: The existing movie storage. It gains two required boolean columns, defaulting false for rows already stored. No feedback table is added.
- **Movie list item and movie details**: The existing read shapes. Each gains the two marks so a successful change is visible without a new read route.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For each of the four operations, a legal change returns an empty 204 and saves once, and a repeat that observes committed target state returns 409 Conflict problem details and saves zero (Q12).
- **SC-002**: All eight combinations of watched, not interested, and watchlist can be stored. Changing any one of the three leaves the other two unchanged, in both directions.
- **SC-003**: Across all four operations, an unknown positive id returns 404 and saves zero, a nonpositive integer id returns 422 with errors containing id and saves zero, and a malformed or overflowing id returns 400 and saves zero.
- **SC-004**: Existing list and details responses include `isWatched` and `isNotInterested`. No new read route and no new browse filter exist.
- **SC-005**: After upgrade, preexisting movies remain, with both new marks false and their other data intact. After downgrade, only those two columns are gone and the previous data remains.
- **SC-006**: `web/src/api/openapi.json` is exported from the C# contract. Generated `web/src/api/types.ts` and `web/src/mocks/handlers.ts` match that committed document. `npm run generate:api` does not export it. The verification baseline matches that document. The generation regression locks the previous ten operations plus the four approved operation identities (14) and still checks bytes and staleness. No handwritten duplicate wire model or on-screen control is added.
- **SC-007**: Configured verification runs without a lowered threshold, a hand-edited generated file, or a new opt-out. A check that could not run is not recorded as a pass. Scope-empty skip, opt-out skip, and mutation not-applicable stay distinct from pass.

## Unresolved owner decisions

The historical grill statement that Patron recorded zero constitution departures and zero owner checkboxes is explicitly superseded by Q11. These two checkboxes stay unresolved. No Patron ruling, drafting fix, or prerequisite success resolves them. Gate 1 stays closed until the user resolves both on the spec PR and the spec PR merges. Plan challenge freeze is recorded separately in plan.md and never authorizes implementation.

- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:219-220 (all commands validated through FluentValidation in the validation decorator) for the four MovieId-only feedback commands by validating route id via MovieId.TryCreate (HTTP 422) at the endpoint boundary without separate IValidator<TCommand> classes, while retaining the existing validation decorator through AddHandler?
- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:335-342 (single fresh Initial migration without constitution amendment/ADR) by generating a reversible additive EF migration for movies.is_watched and movies.is_not_interested without editing 20260930153520_Initial.cs?

## Assumptions

- `[assumed]` Q9 current-state token for `UnmarkNotInterested` when `IsNotInterested == false`: `"Interested"`, paired with `"Watched"`, `"NotWatched"`, and `"NotInterested"`, rather than `"NotNotInterested"`. `"Interested"` denotes only the absence of the negative mark, not positive preference (`specs/DEV-352/ASSUMPTIONS.md`).
- Label wording, visual controls, sort order, and empty-state copy are outside this ticket. No UI assumption is recorded.
- Domain, storage, API, read-field, test, and loop choices are the Patron rulings Q1–Q12 in `CONCLUSIONS.md`, summarized in `brief.md`. They are not assumptions added here.
- Q9 closes the conflict-token drafting question. Q10 closes the migration-test file drafting question. Neither closure resolves the owner checkboxes above.
