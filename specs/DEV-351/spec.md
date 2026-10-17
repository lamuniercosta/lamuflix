# Feature Specification: WatchHistory Playback Events

**Feature Branch**: `feature/351-spec`

**Created**: 2026-10-17

**Status**: `gate1: provisional` (`blocked: structural — OD-1 (C1)` carried as an unchecked owner checkbox onto the spec PR body; Gate 1 remains closed until the owner answers OD-1 before merging).

**Input**: DEV-351 (parent DEV-288, size M). Overview: implement the WatchHistory domain model and record a PlaybackEvent upon movie playback. Acceptance criterion: launching movie playback automatically persists the playback event timestamp (note DEV-351:15–28, recon-DEV-351:94–111). Settled by `brief.md` and CONCLUSIONS.md Q1–Q8, except where the Phase 2 step 3.3 rulings and the Phase 2 step 6 adjudication supersede earlier conclusions. Step 6 replaces the Q4 event id `bigint` with integer generated identity. No `[assumed]` taste entries (`ASSUMPTIONS.md`). C1/OD-1 stays unresolved.

## Owner Decisions (Gate 1 remains closed)

- [ ] **OD-1 (C1)** — blocked: structural — Should `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)` live in shared `Core/Domain/` (or `Core/Ports/`) to satisfy constitution.md:133–135 and ArchitectureTests.cs:87–95, or should ArchitectureTests and Principle I be amended to permit Features/Playback (and Core/Ports) to reference Features/WatchHistory as literal ticket DEV-351:21 specifies?

Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`, alongside the shared Movie and MovieId types. Patron cites constitution.md:294, where Playback Event is already a core domain term. That recommendation is not an owner answer. This draft does not move the type, does not amend the constitution, and does not amend ArchitectureTests. C2 and C3 are resolved and are not owner checkboxes: `IPlaybackHistory` is already on the constitution port whitelist, so no ADR is required; incremental `AddPlaybackEvents` is approved.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A successful movie launch is remembered (Priority: P1)

As the person using LamuFlix, when I successfully launch playback of a movie, the library keeps one record that this launch happened, at the UTC instant immediately after the player launcher returns. The successful play response is not given until that record has been stored durably. The record is the launch, not proof that viewing began, progressed, or finished.

**Why this priority**: This is the ticket's only acceptance criterion (DEV-351:26–28, recon-DEV-351:111). Q1–Q8 add no second user-facing capability.

**Independent Test**: With local playback enabled and a movie that exists, one successful play request leaves exactly one new playback event for that movie, stamped with the injected UTC clock taken immediately after the launcher returns, and the successful response is withheld until that event is durable. A second successful request leaves a second event. A missing movie, disabled local playback, or a failed launch leaves the history unchanged.

**Acceptance Scenarios**:

1. **Given** local playback is enabled and the movie exists, **When** a play request launches successfully, **Then** one new playback event is stored for that movie with `PlayedAt` equal to the UTC instant sampled immediately after the launcher returns, and the existing successful play response is returned only after that store completes.
2. **Given** a playback event was just stored for a movie, **When** another play request for that movie launches successfully, including when the clock returns the same instant, **Then** a distinct second event is stored. Nothing collapses the two launches into one.
3. **Given** the movie does not exist, **When** a play request is made, **Then** the existing not-found outcome is returned and no playback event is stored.
4. **Given** local playback is disabled, **When** a play request is made, **Then** the existing disabled outcome is returned, no player process starts, and no playback event is stored.
5. **Given** the movie exists and local playback is enabled, **When** the launcher fails, **Then** no playback event is stored and no success response is returned.
6. **Given** the launcher has already returned successfully, **When** storing the event fails or is cancelled, **Then** the failure or cancellation propagates, no success response is returned, the launch is not repeated, and no automatic retry is added.
7. **Given** two movies each have a known count of playback events, **When** one movie is deleted, **Then** that movie's events are removed and the surviving movie and its events remain at those prior counts. Events are not a retained audit of removed movies.
8. **Given** the play request completes with the existing success response, **When** the public play contract is compared with the current baseline, **Then** the route, documented statuses, and generated client contract are unchanged.

### Edge Cases

- The player process and the history store do not share one atomic commit. A failure or cancellation after a successful launch can leave a player running with no event. A later request is a new launch, not a replay of the lost event. There is no cross-request exactly-once or deduplication promise, and no cleanup work is added (Q2). That existing wording disposes the non-atomic launch residual.
- If the movie is deleted between catalog resolution and the append, the foreign-key failure propagates after launch (FR-007, Q2). That delete-between-resolve-and-append case is an accepted failure residual. It adds no concurrency harness, cleanup, or deduplication.
- Cancellation is forwarded into the history store. It is not swallowed (Q2).
- Two successful launches may share a `PlayedAt` value. They stay two events. There is no uniqueness rule on movie plus timestamp (Q1, Q4).
- A history row whose movie does not exist is rejected by the movie foreign key (Q4, Q6).
- Direct append failure stays an existing unhandled infrastructure failure. The endpoint must not return 204. Documented play statuses stay recon-DEV-351:18 (204, 400, 403, 404, 422). Q5's mention of 500 is a reuse ruling. It is not evidence that a shaped 500 mapper already exists, and it is not a new contract (brief Q5, CONCLUSIONS Q5). A response shape recon does not document goes to recon. No new mapper, status, DTO, or baseline is added.
- No one can read, list, or edit history in this ticket. There is no progress, completion, resume, user identity, watchlist change, or history screen (Q1, Q8).
- Routing-only play tests that bypass a seeded database may supply a test history port and a clock. That substitute does not count as the database acceptance proof. The acceptance proof uses a seeded movie, the real history adapter, and a substituted process starter (Q6, Q7).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: WatchHistory is the name of this feature: the history of successful playback launches for movies. It is not a separate aggregate and it has no table of its own (Q1).
- **FR-002**: A PlaybackEvent is exactly `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)`. It has no user, progress, completion, or resume data (ticket DEV-351:21, Q1). Its file location is the unchecked OD-1 decision. The ticket names `src/LamuFlix.Core/Features/WatchHistory/`. Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`. The recommendation does not approve a move. Do not place the type on either path until the owner checks OD-1, and do not amend the constitution or ArchitectureTests.
- **FR-003**: `PlayedAt` is the UTC instant from the injected `TimeProvider.GetUtcNow()`, sampled immediately after the existing launcher returns successfully (Q1, Q2). `DateTime.Now` and its family are not a source.
- **FR-004**: `PlayMovieCommandHandler` resolves the movie through its existing catalog, calls the existing launcher, samples the clock, awaits history persistence, and only then returns success (Q2).
- **FR-005**: The existing `POST /api/movies/{id}/play` success response (204) means history persistence completed durably, not that a row was merely tracked (Q2, Q3, Q5).
- **FR-006**: A missing movie, disabled local playback, or a failed launch must not append an event (Q1, Q2).
- **FR-007**: Persistence failure and cancellation propagate. The handler must not swallow them, relaunch, add an automatic retry, or return 204 (Q2).
- **FR-008**: The handler passes its cancellation token into history persistence (Q2, Q3).
- **FR-009**: Repeated successful play requests create distinct events, including when their timestamps match. There is no deduplication (Q1, Q2).
- **FR-010**: `IPlaybackHistory` lives in `src/LamuFlix.Core/Ports/` and exposes `Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken = default)`. It has no read, update, or delete method and no loaded event collection (Q3). C2 is resolved: this port is already on the constitution whitelist (constitution.md:136–140) and in ArchitectureTests `PortWhitelistPattern` (ArchitectureTests.cs:28–29). Implementing the listed port does not add a port and does not require an ADR.
- **FR-011**: A scoped `EfPlaybackHistory` in the existing Infrastructure persistence area implements that port by appending one persistence record and awaiting `LamuFlixDbContext.SaveChangesAsync`. It is registered through `AddLamuFlixPersistence`. The handler receives the port and `TimeProvider` (Q3).
- **FR-012**: Database identity belongs only to the persistence record. The domain PlaybackEvent stays the two ticket fields (Q3).
- **FR-013**: `Movie`, `IMovieRepository`, and `EfMovieRepository` stay unchanged (Q3).
- **FR-014**: The existing context gains `PlaybackEventRecord`, `PlaybackEventConfiguration`, and `DbSet<PlaybackEventRecord> PlaybackEvents`. The only new table is `playback_events` (Q4):
  - `id`: `integer`, generated identity, primary key (`int Id` on `PlaybackEventRecord`). This key is independent of `movie_id` (step 6 Q4 amendment).
  - `movie_id`: required, existing `MovieIdConverter`, nonunique index, foreign key to `movies(id)` with `ON DELETE CASCADE`.
  - `played_at`: required `DateTimeOffset` stored as PostgreSQL `timestamp with time zone`. The equivalent configuration literal is `timestamptz`.
  - No unique constraint on movie plus timestamp. No new column on `movies` or any other existing table.
- **FR-015**: Deleting a movie removes its playback events. History is append-only only while that movie exists (Q1, Q4).
- **FR-016**: The `AddPlaybackEvents` migration, its designer, and the model snapshot are generated with the existing EF tooling. Generated files are not hand-edited. Down drops only `playback_events`, which discards recorded playback events by design, and preserves all `movies` rows. Upgrade from the current two-migration schema preserves existing movie data; downgrade returns to that schema without losing movie data; reapply succeeds. Migration expectations for this intake baseline become three migrations and ten tables (Q4, recon-DEV-351:31,41). C3 is resolved: this incremental third migration is approved. constitution.md:342 names a single fresh Initial migration for the PostgreSQL replacement of legacy MySQL migrations, not a ban on later migrations (recon-DEV-351:41,42,50).
- **FR-017**: Design-time database connectivity follows the existing environment handling (`LAMUFLIX_DESIGN_TIME_CONNECTION_STRING`, recon-DEV-351:31). No connection string is hardcoded and no secret is printed (Q4).
- **FR-018**: No new route, DTO field set, or status code. Documented play responses remain those recon-DEV-351:18 records (204, 400, 403, 404, 422). Existing error handling is reused (Q5).
- **FR-019**: OpenAPI and TypeScript baselines are not regenerated and not hand-edited. Delivery verifies zero contract drift. TypeScript DTOs are not hand-written (Q5, Q6).
- **FR-020**: Recording is approved only after launcher success on the existing `Features:LocalPlay` path. Local playback off means no event. Flag semantics, launchers, `Process.Start`, and secrets are unchanged (Q5).
- **FR-021**: No new NuGet or npm dependency, project, top-level folder, or architectural layer (Q5).
- **FR-022**: Production and test edits stay inside the Q7 envelope below. No unrelated file is deleted or rewritten. Generated migration artifacts come only from tooling (Q5, Q7).
- **FR-023**: Proof uses existing xUnit, Shouldly, NSubstitute, and fixed or manual `TimeProvider` patterns. The data-access driver is not mocked. Unit tests may substitute `IPlaybackHistory`. Database translation, constraints, cascade, timestamp round-trip, and the endpoint acceptance proof use real PostgreSQL Testcontainers. A port substitute does not satisfy the database acceptance proof (Q6).
- **FR-024**: Handler proof covers resolve-then-launch-then-clock-then-append order; the exact `MovieId`, clock instant, and cancellation token; waiting for history before success; no event on missing, disabled, or failed launch; propagation of append failure and cancellation without retry or relaunch; and repeated successful calls (Q6).
- **FR-025**: PostgreSQL proof covers a row that a fresh context can read, distinct identities for two plays at the same clock instant, a deterministic UTC timestamp round-trip, foreign-key rejection, cascade deletion of one movie with the sibling movie and its events left at their prior counts, and persistence DI composition (Q6, SC-005). Database comparisons use fixed UTC instants aligned to the precision the database supports. Substituted-port handler proof keeps exact event and clock equality. No numeric precision width is imported.
- **FR-026**: Endpoint acceptance uses a seeded real database, the actual history adapter resolved through production `AddLamuFlixPersistence`, and a substituted process starter. A 204 response implies one persisted event. Disabled local playback stays 403 with no process and no row. One bounded case shows that an append failure after launch does not return 204 and stays on existing handling, without a new mapper, status, DTO, or baseline. Existing playback failure regressions stay in place (Q6).
- **FR-027**: Routing-only hosts that bypass database seeding satisfy `IPlaybackHistory` by a test registration or a real database, and they satisfy clock injection. They must not silently replace real persistence in the acceptance test (Q6, Q7).

### Key Entities

- **PlaybackEvent**: One observation of a successful launcher return. Attributes are the movie identity and the UTC instant of that return. It is not an aggregate, it has no persistence identity, and it is not evidence of viewing progress or completion.
- **WatchHistory**: The feature name for the append-only sequence of those events during the owning movie's lifetime. Not an entity and not a table.
- **playback_events**: The ticket's one new table, a child of `movies`. Each stored row is one PlaybackEvent plus a database-generated identity. Removing the movie removes its rows.

### Out of Scope

Frozen by Q8. Anything here is a follow-up issue, not a finding and not work in this draft:

- History reads, public history endpoints, progress, completion, resume, user identity, UI or web work, and watchlist changes.
- Changes to the Movie aggregate or to `IMovieRepository` / `EfMovieRepository`.
- Extra dependencies, projects, layers, or redesign of existing tables.
- Player execution, `Features:LocalPlay` semantics, `Process.Start`, and secrets.
- A new ADR, a root `CONTEXT.md` edit, or a root glossary file (Q7). Vocabulary for this ticket stays in `brief.md`.
- Unrelated cleanup.

OD-1 is unchecked. The earlier Q5 and Q8 conclusion that no owner checkbox was required is superseded for C1. C2 and C3 do not add checkboxes. Gate 1 stays closed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Each successful play request that launches produces exactly one new playback event for that request. A run of N successful launches produces N events.
- **SC-002**: The successful play response is observed only after the corresponding event is durable. A failed or cancelled store does not produce that response.
- **SC-003**: A missing movie, disabled local playback, or a failed launch adds zero events.
- **SC-004**: Two successful launches whose timestamps are equal produce two events with two identities.
- **SC-005**: Deleting one of two movies removes that movie's playback events and leaves the surviving movie and its playback events at the same counts. Downgrade of the new migration drops only the new table, discards the playback events that table held, and keeps existing movie data; reapply restores the table.
- **SC-006**: The public play route, its documented status codes, and the committed API contract match the pre-change baseline (zero drift).

## Assumptions

No taste assumption is recorded. `ASSUMPTIONS.md` has no `[assumed]` entry (Q8, PRODUCT.md:28–30). Taste count stays zero.

C1/OD-1 is unresolved and unchecked. It is the owner structural checkbox that keeps Gate 1 closed. Patron's Domain recommendation is not owner approval. C2 and C3 are resolved and are not checkboxes: the port is pre-whitelisted and needs no ADR; incremental `AddPlaybackEvents` is approved.

Settled behavior used above comes from `brief.md` and CONCLUSIONS.md Q1–Q8, plus the Phase 2 step 3.3 and step 6 rulings where they amend those conclusions. Facts about the current play endpoint, ports, migrations, and tests come from recon-DEV-351. Missing facts are not filled by assumption (`ASSUMPTIONS.md`).
