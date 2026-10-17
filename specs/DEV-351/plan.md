# Implementation Plan: WatchHistory Playback Events

**Branch**: `feature/351-spec` | **Date**: 2026-10-17 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), [brief.md](brief.md) (owns the plan decisions), [CONCLUSIONS.md](CONCLUSIONS.md) Q1–Q8, the Phase 2 step 3.3 C1–C3 rulings, and the Phase 2 step 6 adjudication, [ASSUMPTIONS.md](ASSUMPTIONS.md), note recon-DEV-351, note DEV-351:15–28, `specs/PRODUCT.md` as cited by Patron.

Step 6 corrections are in this draft and await read-only verification. They do not freeze the plan and do not authorize implementation or delivery. OD-1 stays unchecked. No third fix round is opened.

## Summary

Record one PlaybackEvent each time movie playback launches successfully. The handler resolves the movie, calls the existing launcher, samples injected `TimeProvider.GetUtcNow()`, and awaits a durable append before it returns. A 204 means that append finished. Missing movies, disabled LocalPlay, and failed launches append nothing. Failure or cancellation after launch propagates with no swallow, retry, or relaunch.

WatchHistory is the feature name only. The domain type is the ticket's two-field record. Database identity, the `playback_events` table, and cascade-on-movie-delete live in the existing persistence project. The public play route does not change.

**Note on the Spec Kit phases**: Research, data model, contracts, and quickstart are folded into this plan. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` is produced. The brief already settles the entity, the table, the unchanged play contract, and the proof. This ask writes `spec.md`, `plan.md`, `tasks.md`, and `checklists/requirements.md` only. Quill does not open a research pass or edit `AGENTS.md`. Performance and scale targets are not in the settled sources and are not invented here.

## Technical Context

**Language/Version**: C# on .NET 10 (PRODUCT.md:9; constitution Technology Stack).

**Primary Dependencies**: No additions (Q5). Existing EF Core / Npgsql.EntityFrameworkCore.PostgreSQL, xUnit v3, Shouldly, NSubstitute, Testcontainers.PostgreSql (recon-DEV-351:48). `TimeProvider` is already registered (recon-DEV-351:32,34).

**Storage**: Existing PostgreSQL. One new table `playback_events`. Existing nine application tables stay as recon-DEV-351:41 lists them. Expectations become three migrations and ten tables (Q4).

**Testing**: `tests/LamuFlix.UnitTests` and `tests/LamuFlix.IntegrationTests`, real PostgreSQL via `PostgresFixture` (recon-DEV-351:41). No EF provider mock (Q6).

**Target Platform**: Existing API host. No web change (Q1, Q8).

**Project Type**: Existing .NET API plus persistence library. No new project (Q5).

**Performance Goals**: Not specified in `brief.md`, CONCLUSIONS.md, recon-DEV-351, note DEV-351, or the cited PRODUCT.md lines. None invented.

**Constraints**: Append only during the owning movie's lifetime; cascade delete with the movie; no uniqueness on `(movie_id, played_at)`; 204 only after durable append; cancellation passed through; no hardcoded connection string; generated migration files not hand-edited; `Features:LocalPlay`, launchers, `Process.Start`, and secrets unchanged (Q2–Q5).

**Scale/Scope**: One domain record, one port, one adapter, one table, one generated migration, and the Q7 test envelope. Single-user library (recon-DEV-351:14). No concurrent-user or volume target is stated.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Initial and post-design are the same check. Phase 0 and Phase 1 artifacts were not split out; the design below is the post-design state. C2 and C3 pass on the Phase 2 step 3.3 rulings. C1 does not pass: the feature-folder conflict stays an open owner decision.

- **Principle I (ports)**: `IPlaybackHistory` is added in `Core/Ports/`. `EfPlaybackHistory` is an adapter in existing Infrastructure. No new project or layer (Q5). C2 is resolved: the constitution already names `IPlaybackHistory` in the closed port whitelist (constitution.md:136–140), and ArchitectureTests.cs:28–29 already includes it in `PortWhitelistPattern`. The ADR requirement (constitution.md:139–140, 375) applies when the closed list grows, not when this listed port is implemented (DEV-351:22). This plan does not add an ADR task and does not treat the port as a constitution departure.
- **Principle I (feature folders do not reference each other)**: C1 remains blocked: structural. constitution.md:133–135 says feature folders must not reference each other and shared concepts live in `Domain/`, `Ports/`, or `Pipeline/`. ArchitectureTests.cs:87–95 and 291–309 enforce that rule. The ticket places `PlaybackEvent` in `Features/WatchHistory/` while `PlayMovieCommandHandler` in `Features/Playback` records it (DEV-351:21–22). Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`. That recommendation is not an owner decision. This plan does not move the type, does not amend the constitution, and does not amend ArchitectureTests. Gate 1 stays closed until the owner checks OD-1 in `spec.md`.
- **Principle II (handlers)**: The existing sealed play command and handler stay the use case. No MediatR, no new exception type, no `Result<T>`. Expected failures stay the existing exceptions. Append failure propagates as existing unhandled infrastructure failure (Q2, Q5). The constructor takes four dependencies: `IMovieCatalog`, `IMediaPlayerLauncher`, `IPlaybackHistory`, and `TimeProvider`. All four are required by the ticket and by Q2, Q3, and Q5. The "more than three ports" heuristic is acknowledged. This plan does not claim that heuristic is cleared, and it does not add a facade, wrapper, or other abstraction (Q5, §2.3.2).
- **Principle V (errors)**: No new status and no new mapper. Documented play statuses stay recon-DEV-351:18. Append failure stays existing unhandled infrastructure failure and must not return 204 (Q2, Q5). Q5's mention of 500 is a reuse ruling, not evidence of a shaped 500 mapper and not a new contract. A response shape recon does not document goes to recon. No DTO or baseline change.
- **Principle VII (time and secrets)**: `PlayedAt` comes from injected `TimeProvider.GetUtcNow()`. Design-time connectivity stays `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` (recon-DEV-351:31). No secret in source.
- **Principle VIII (language)**: WatchHistory, PlaybackEvent, and PlayedAt are the feature vocabulary in `brief.md` lines 26–35 and Q1. WatchHistory is the feature-slice name (Q1, Q7). PlaybackEvent is the domain concept the step 6 ruling cites at constitution.md:134 and constitution.md:294. Q7 rules out a root `CONTEXT.md` edit, a new ADR, and a root glossary edit. This plan edits none of those.
- **Principle IX (tests)**: xUnit v3, Shouldly, NSubstitute, Testcontainers for PostgreSQL. Handler tests may substitute the port. Persistence and endpoint acceptance do not mock the driver (Q6).
- **API and contract rules**: `POST /api/movies/{id}/play` stays 204 on success (recon-DEV-351:18–19, brief Q5, CONCLUSIONS Q5). OpenAPI drift is verified, not regenerated (Q5). The Q7 envelope is unchanged.
- **Documentation rules**: A PR that changes an architectural choice must add or supersede an ADR (constitution.md:499–501). C2 resolves the port: implementing pre-whitelisted `IPlaybackHistory` is not an expansion of the closed list, so this plan adds no ADR. C1 is the unchecked owner checkbox OD-1, not an ADR task. No README and no CHANGELOG (Quill duties; the ask does not name them).
- **Technology stack "single fresh Initial migration"** (constitution.md:342): C3 is resolved. Incremental `AddPlaybackEvents` is approved. constitution.md:342 is the initial PostgreSQL replacement of legacy MySQL migrations, not a ban on later migrations. recon-DEV-351:31,41,42,50 already records `Initial`, incremental `AddMovieWriters`, and the MigrationTests expectations. This plan still generates `AddPlaybackEvents` with the existing EF tooling and keeps the upgrade, downgrade, reapply, and three-migration / ten-table regressions (Q4). No constitution change.
- **§2.3 care list**: Q5's table still disposes packages, layers, the ticket table, the public API, LocalPlay, and the file envelope. C1 supersedes the Q5 and Q8 conclusion that no owner checkbox was required. One unchecked owner decision is required. C2 and C3 require none.

C1 is the open constitution conflict recorded in Complexity Tracking. Patron's Domain recommendation does not close it, and this plan does not treat the conflict as compliant.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-351/
|- brief.md, CONCLUSIONS.md, ASSUMPTIONS.md   (Keel / Patron)
|- spec.md                                     (this draft, /speckit-specify)
|- plan.md                                     (this file, /speckit-plan)
|- tasks.md                                    (this draft, /speckit-tasks)
|- checklists/requirements.md                  (specify quality checklist)
`- DRAFTING_RECEIPT.md                         (method and any gap)
```

No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md`.

### Source Code (repository root)

```text
src/LamuFlix.Core/Features/WatchHistory/PlaybackEvent.cs          (ticket path only; OD-1 unchecked)
src/LamuFlix.Core/Ports/IPlaybackHistory.cs                       (new)
src/LamuFlix.Core/Features/Playback/PlayMovieCommandHandler.cs    (edit)
src/LamuFlix.Infrastructure/Persistence/EfPlaybackHistory.cs      (new)
src/LamuFlix.Infrastructure/Persistence/Records/PlaybackEventRecord.cs          (new)
src/LamuFlix.Infrastructure/Persistence/Configurations/PlaybackEventConfiguration.cs (new)
src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs      (edit)
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs (edit)
src/LamuFlix.Infrastructure/Persistence/Migrations/               (generated AddPlaybackEvents + designer)
src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs (generated edit)

tests/LamuFlix.UnitTests/Features/Playback/PlayMovieCommandHandlerTests.cs (edit)
tests/LamuFlix.UnitTests/Features/WatchHistory/PlaybackEventTests.cs       (only if OD-1 keeps the ticket path)
tests/LamuFlix.UnitTests/Domain/PlaybackEventTests.cs                      (only if OD-1 selects Core/Domain)
tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs        (edit)
tests/LamuFlix.UnitTests/Persistence/PersistenceCompositionTests.cs (edit)
tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs          (edit)
tests/LamuFlix.IntegrationTests/ApiEndToEndPlaybackTests.cs       (edit)
tests/LamuFlix.IntegrationTests/MigrationTests.cs                 (edit)
tests/LamuFlix.IntegrationTests/EfPlaybackHistoryTests.cs         (new)
tests/LamuFlix.IntegrationTests/PlaybackHistoryEndpointTests.cs   (new)
```

**Structure Decision**: `PlaybackEvent.cs` stays unplaced until the owner checks OD-1. The tree shows the ticket path `Features/WatchHistory/PlaybackEvent.cs` so the draft does not silently relocate it. Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`. That recommendation is not approval to move the file, and it is not approval to amend the constitution or ArchitectureTests. `PlaybackEventTests.cs` follows the same unchecked choice: `tests/LamuFlix.UnitTests/Domain/PlaybackEventTests.cs` if the owner selects `Core/Domain/PlaybackEvent.cs`, or `tests/LamuFlix.UnitTests/Features/WatchHistory/PlaybackEventTests.cs` if the owner selects `Core/Features/WatchHistory/PlaybackEvent.cs`. The namespace follows that path. Neither path is selected while OD-1 is unchecked. The port stays in the existing Core ports folder, and persistence stays in the existing Infrastructure persistence folder (Q5, Q7). The three new test files are the focused history unit test and the focused PostgreSQL integration tests Q7 tells Quill to name, inside the existing test projects. `PlaybackEventTests.cs` locks the two-field domain shape and the absence of a persistence identity. `EfPlaybackHistoryTests.cs` locks adapter durability, distinct ids, timestamp round-trip on aligned fixed UTC instants, foreign-key rejection, and two-movie cascade with exact surviving movie and event counts. `PlaybackHistoryEndpointTests.cs` is the seeded-database acceptance proof: the host resolves the real adapter through `AddLamuFlixPersistence`; 204 implies one row; disabled LocalPlay is 403 with no process and no row.

`ApiEndToEndPlaybackTests.cs` stays a forced edit limited to new dependency and schema consequences (brief Q7). recon-DEV-351:38 names that file and does not describe its host. This plan does not assign the acceptance proof to it.

`OpenApiContractTests` and `web/src/api/openapi.json` / `types.ts` are not edited (Q5). Delivery runs the existing drift check.

Architecture test sources are not in the Q7 envelope. This plan does not edit them. C1 does not authorize an ArchitectureTests or constitution amendment. C2 is already satisfied for the port name: constitution.md:136–140 and ArchitectureTests.cs:28–29 list `IPlaybackHistory`.

## Design

### 1. Domain record and port (FR-001–FR-003, FR-010, FR-012)

`PlaybackEvent` is a sealed record with the two positional ticket fields. No identity, no methods beyond what a record needs, no extra members. The file path is OD-1. The ticket path is `Features/WatchHistory/`. Patron recommends `Core/Domain/PlaybackEvent.cs`. Neither path is approved in this plan.

`IPlaybackHistory` is the port above. One method. Successful completion means the append was saved, which the adapter fulfills. The interface itself does not mention EF.

### 2. Handler order (FR-004–FR-009, FR-020, FR-024)

Current handler dependencies are `IMovieCatalog` and `IMediaPlayerLauncher` (recon-DEV-351:19). Add `IPlaybackHistory` and `TimeProvider`, so the constructor has those four dependencies (Q2, Q3, Q5). No facade or other abstraction is added.

Order on the success path:

1. Resolve the movie with the existing catalog call. A missing movie still surfaces the existing not-found failure. No append.
2. Call the existing launcher. Disabled LocalPlay and a failed launch still surface the existing failures. No append.
3. Sample `TimeProvider.GetUtcNow()`.
4. Await `RecordAsync` with a `PlaybackEvent` of that movie id and that instant, and with the handler's cancellation token.
5. Return `Unit` only after that await completes. The endpoint's existing 204 is unchanged.

On append failure or cancellation: propagate. Do not catch, retry, or call the launcher again.

The constructor may be extended before the body records, so handler tests can compile. The body change that performs steps 3–5 is the wiring step. See `tasks.md` for why those tests sit with the wiring step.

### 3. Persistence (FR-011–FR-015, FR-025)

`PlaybackEventRecord` holds generated `int Id` (integer identity), required `MovieId MovieId`, and required `DateTimeOffset PlayedAt`. `PlaybackEventConfiguration` maps table `playback_events`, snake_case columns, `id` as integer generated identity primary key, the existing `MovieIdConverter` on required `movie_id`, a nonunique index on `movie_id`, and the foreign key to `movies(id)` with `ON DELETE CASCADE`. `played_at` is required `DateTimeOffset` as `timestamp with time zone`; `.HasColumnType("timestamptz")` is the equivalent literal. No unique index on movie plus timestamp. The index name is whatever the existing configuration style and EF tooling emit; the settled requirement is that a nonunique index on `movie_id` exists, not a hand-picked name. Other Q4 lifecycle rules are unchanged. The event primary key does not follow `movie_id`.

`EfPlaybackHistory.RecordAsync` maps the domain event to a new record, adds it, and awaits `SaveChangesAsync` with the same cancellation token. It does not set the identity. Scoped registration is next to the existing repository registration in `AddLamuFlixPersistence`. The shared scoped `LamuFlixDbContext` stays. The play flow (catalog lookup, launcher, and this append) introduces no unrelated tracked entity modifications before `SaveChangesAsync`. No new context or service is added. A missing fact goes to recon.

`LamuFlixDbContext` gains the `DbSet`. `ApplyConfigurationsFromAssembly` already discovers the configuration (recon-DEV-351:28).

### 4. Migration (FR-016, FR-017)

Generate `AddPlaybackEvents` with the existing EF tooling so the designer and `LamuFlixDbContextModelSnapshot.cs` update together. Do not hand-edit those files. Down drops only `playback_events`, which discards recorded playback events by design and preserves all `movies` rows.

`MigrationTests` today pins two migrations and these nine tables: `movies`, `actors`, `directors`, `genres`, `writers`, `movie_actors`, `movie_directors`, `movie_genres`, `movie_writers` (recon-DEV-351:41). Update the count to 3, include the generated `AddPlaybackEvents` name, and add `playback_events` to the expected table set. Prove upgrade from that two-migration schema with movie data preserved, the column, index, and foreign-key requirements, downgrade without losing movie data, and reapply.

Design-time factory already reads `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` (recon-DEV-351:31). Do not add a fallback connection string.

### 5. Tests that are edits, not new proof files (FR-022, FR-023, FR-026, FR-027)

- `PlayMovieCommandHandlerTests`: the FR-024 scenarios, with NSubstitute for the port and a fixed or manual clock. No EF mock.
- `PersistenceCompositionTests`: `AddLamuFlixPersistence` registers scoped `IPlaybackHistory` to `EfPlaybackHistory`.
- `ModelMetadataTests`: the model includes `playback_events` and the FR-014 mapping, including integer generated identity (`int Id`) and required `MovieId` and `PlayedAt`. Do not rewrite unrelated metadata assertions.
- `PlaybackEndpointTests`: this host substitutes `IMovieCatalog` and `IProcessStarter` (recon-DEV-351:38). Satisfy `IPlaybackHistory` with an explicit test registration and satisfy clock injection. Keep the existing 204-and-one-process and 403-and-no-process regressions. This registration is not the acceptance proof.
- `ApiEndToEndPlaybackTests`: only the edits required so the new constructor, port, clock, and schema do not break existing playback failure regressions. Do not replace a real persistence registration with a port substitute. Do not add history assertions here.
- Existing OpenAPI drift test: run it. Do not edit baselines.

### 6. Acceptance proof (FR-025, FR-026)

`EfPlaybackHistoryTests` uses `PostgresFixture` and a fresh context after save. Timestamp comparisons there, and in the endpoint proof, use deterministic fixed UTC instants aligned to database-supported precision (whole seconds, or whole milliseconds or microseconds, and not sub-microsecond 100-nanosecond ticks). That alignment is the test method. It does not import a precision width into the spec. Substituted-port handler tests keep exact `DateTimeOffset` equality with `TimeProvider.GetUtcNow()`. Cascade proof seeds two movies that have events, deletes one, and checks exact counts: the deleted movie's events are gone, and the sibling movie and its events remain at the seeded counts.

`PlaybackHistoryEndpointTests` uses a seeded movie, a substituted `IProcessStarter`, and the real `EfPlaybackHistory` resolved through production `AddLamuFlixPersistence` on that host, alongside the unit composition coverage. It does not construct the adapter by hand. Disabled LocalPlay on that host returns 403, starts no process, and writes no row. One bounded case throws from `RecordAsync` after launch and expects a response other than 204 through existing handling, with no new mapper, status, DTO, or baseline. A response shape that recon does not document goes to recon. The same proof confirms the play flow introduces no unrelated tracked writes before `SaveChangesAsync`. No new context or service is added.

### 7. Delivery gates (brief Gate expectations, Q7)

No gate runs in this drafting step. At delivery, use live `harness.yml` and the existing scripts. Do not edit thresholds. Report each verdict on its own: build, Roslyn analyzers, cyclomatic complexity (including the constitution's refactor-gate invocation), InspectCode, `dotnet test`, property tests, vulnerable packages, format, mutation, and the unchanged OpenAPI drift check. A gate that could not run has not passed. Exit 2 scope-empty SKIPPED, opt-out SKIP, and mutation N/A stay separate from PASS.

Delivery verification also reviews the three-dot `base...head` diff envelope. That review confirms `Movie`, `IMovieRepository`, and `EfMovieRepository` are untouched (FR-013), no new NuGet or npm package, project, top-level folder, or layer was added (FR-021), and every modified or added file stays inside the Q7 envelope (FR-022). Complete planned coverage of FR-013, FR-021, and FR-022 is not claimed until that review exists and is read back. Specifying the review in this draft does not perform it.

### 8. Loop (Q8)

Closing bar: zero unresolved Critical, High, or Medium findings inside the frozen scope. Low findings are recorded and dispositioned. At most two review or remediation rounds. No third fix round and no waived required gate. Unresolved blockers at the cap stay blocked. Anything outside FR-001–FR-027 and the Q8 exclusion list is a follow-up issue, not a finding in this round. OD-1 stays an unchecked Critical owner decision, so this loop language does not freeze the plan and does not authorize implementation or delivery. Prerequisites remain spec analysis, plan challenge adjudication, and owner Gate 1 before any implementation or delivery task. The Q7 envelope is unchanged.

## Complexity Tracking

C1 is an unresolved Principle I conflict. This section does not justify proceeding with either route.

The ticket path `src/LamuFlix.Core/Features/WatchHistory/PlaybackEvent.cs` would let `Features/Playback` reference `Features/WatchHistory`, which constitution.md:133–135 and ArchitectureTests.cs:87–95, 291–309 forbid. Moving the type to `src/LamuFlix.Core/Domain/PlaybackEvent.cs` is the Patron recommendation and would change the ticket path. Amending Principle I and ArchitectureTests would depart from the constitution. Neither route is chosen, and neither alternative is rejected by this plan. OD-1 in `spec.md` is the unchecked spec-PR checkbox. Gate 1 stays closed.

C2 and C3 are not tracked as violations. The port is pre-whitelisted and needs no ADR. Incremental `AddPlaybackEvents` is approved, with the generated migration and its regressions retained.
