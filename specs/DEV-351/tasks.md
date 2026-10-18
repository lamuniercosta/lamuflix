# Tasks: WatchHistory Playback Events

**Input**: Design documents from `specs/DEV-351/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md), [ASSUMPTIONS.md](ASSUMPTIONS.md)

**Tests**: Required (Q6). Schema-dependent PostgreSQL tests run after the generated migration exists (brief Q7). Handler recording assertions are written in Phase 1 and are not required to pass until Phase 4.

**Organization**: One user story (US1). Phases follow brief Q7's five steps, and those steps run only after spec analysis, plan challenge adjudication, and owner Gate 1 (I1). No new project, package, or shared infrastructure, so there is no separate setup phase (Q5).

## Owner decision (C1, Gate 1 closed)

- [X] **OD-1 (C1)** — resolved on spec PR #188: the owner selected `src/LamuFlix.Core/Domain/PlaybackEvent.cs` with `tests/LamuFlix.UnitTests/Domain/PlaybackEventTests.cs`, and did not amend ArchitectureTests or Principle I. Original question: Should `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)` live in shared `Core/Domain/` (or `Core/Ports/`) to satisfy constitution.md:133–135 and ArchitectureTests.cs:87–95, or should ArchitectureTests and Principle I be amended to permit Features/Playback (and Core/Ports) to reference Features/WatchHistory as literal ticket DEV-351:21 specifies?

Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`. The recommendation is not the decision. Do not relocate the type, and do not amend the constitution or ArchitectureTests. C2 is resolved: `IPlaybackHistory` is pre-whitelisted and needs no ADR. C3 is resolved: incremental `AddPlaybackEvents` is approved, and the generated migration plus its regressions stay.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: [US1] on every task in Phases 1–4. Phase 5 has no story label
- Paths are repository-relative from the worktree root

## Phase 1: Domain record, port, and handler test coverage (Q7 step 1)

**Goal**: The two-field PlaybackEvent and `IPlaybackHistory` exist, and handler tests state the FR-024 behavior.

**Independent Test**: `PlaybackEventTests` passes. Existing `PlayMovieCommandHandlerTests` launch cases still pass. New recording cases are present and fail until T018.

- [X] T001 [US1] Add a sealed record `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)` with no further members (FR-002, FR-012). Place it only at the path the owner checks in OD-1. The ticket path is `src/LamuFlix.Core/Features/WatchHistory/PlaybackEvent.cs`. Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`. Neither path is approved in this draft. Do not choose one here, do not move the type ahead of the check, and do not amend the constitution or ArchitectureTests.
- [X] T002 [US1] Add `IPlaybackHistory` in `src/LamuFlix.Core/Ports/IPlaybackHistory.cs` with only `Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken = default)` (FR-010). Depends on T001. The port is the pre-whitelisted `IPlaybackHistory`; do not add an ADR (C2).
- [X] T003 [US1] Add PlaybackEvent unit tests proving a PlaybackEvent exposes the given MovieId and PlayedAt and has no persistence-identity member (FR-002, FR-012). Path and namespace follow OD-1 and stay unchosen while OD-1 is unchecked: `tests/LamuFlix.UnitTests/Domain/PlaybackEventTests.cs` if the owner selects `src/LamuFlix.Core/Domain/PlaybackEvent.cs`, or `tests/LamuFlix.UnitTests/Features/WatchHistory/PlaybackEventTests.cs` if the owner selects `src/LamuFlix.Core/Features/WatchHistory/PlaybackEvent.cs`. Depends on T001.
- [X] T004 [US1] Extend the constructor of `src/LamuFlix.Core/Features/Playback/PlayMovieCommandHandler.cs` to take `IPlaybackHistory` and `TimeProvider` and retain them. Do not record yet. Depends on T001 and T002.
- [X] T005 [P] [US1] In `tests/LamuFlix.UnitTests/Features/Playback/PlayMovieCommandHandlerTests.cs`, supply the port substitute and a fixed or manual `TimeProvider` on existing cases, and add the FR-024 cases: resolve, then launch, then clock, then append; exact MovieId, exact `DateTimeOffset` equality against `TimeProvider.GetUtcNow()`, and cancellation token; success waits for the append; no append on missing movie, disabled launch, or failed launch; append failure and cancellation propagate with no retry and no second launch; two successes append twice. Use xUnit, Shouldly, and NSubstitute. No EF type. Recording cases fail until T018. Existing launch cases stay green. Depends on T004.
- [X] T006 [P] [US1] In `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs`, satisfy `IPlaybackHistory` with an explicit test registration and satisfy clock injection. Keep the existing 204-and-one-process and 403-and-no-process assertions. Do not add a database assertion and do not treat this registration as the acceptance proof (FR-027, recon-DEV-351:38). Depends on T004.
- [X] T007 [US1] In `tests/LamuFlix.IntegrationTests/ApiEndToEndPlaybackTests.cs`, change only what the new constructor, port, clock, and schema require so existing playback failure regressions still run. Do not replace a real persistence registration with a port substitute. Do not add history assertions (brief Q7). recon-DEV-351:38 names this file and does not describe its host. If the host resolves the handler through `AddLamuFlixPersistence`, do the edit after T012 and do not treat a resolution failure before that registration as a Phase 1 defect. Depends on T004, and on T012 when the host uses production persistence.

**Checkpoint**: T003 passes. Existing `PlayMovieCommandHandlerTests` launch cases and `PlaybackEndpointTests` assertions still pass. New recording assertions are red until T018. That red window is Q7 step 1's coverage before step 4's wiring, not a waived gate. `ApiEndToEndPlaybackTests` is not a Phase 1 gate when it depends on T012.

---

## Phase 2: Persistence record, adapter, and composition (Q7 step 2)

**Goal**: The adapter can append through the existing context and DI exposes it. Database tests that need the new table wait for Phase 3.

**Independent Test**: `PersistenceCompositionTests` resolves scoped `IPlaybackHistory` as `EfPlaybackHistory`. Model metadata includes `playback_events` as FR-014. No PostgreSQL round-trip is required in this phase.

- [X] T008 [US1] Add `src/LamuFlix.Infrastructure/Persistence/Records/PlaybackEventRecord.cs` with generated `int Id` (integer identity), required `MovieId MovieId`, and required `DateTimeOffset PlayedAt` only (FR-012, FR-014).
- [X] T009 [P] [US1] Add `src/LamuFlix.Infrastructure/Persistence/Configurations/PlaybackEventConfiguration.cs` mapping table `playback_events`, columns `id` (`integer` generated identity, primary key, `int Id`), `movie_id` (required, existing `MovieIdConverter`, nonunique index, foreign key to `movies(id)` with `ON DELETE CASCADE`), and `played_at` (required `DateTimeOffset` as `timestamp with time zone`, using `.HasColumnType("timestamptz")` as the equivalent literal). No unique constraint on movie plus timestamp. Other Q4 semantics stay. Depends on T008.
- [X] T010 [P] [US1] Add `DbSet<PlaybackEventRecord> PlaybackEvents` to `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs`. Leave `ApplyConfigurationsFromAssembly` as it is (recon-DEV-351:28). Depends on T008.
- [X] T011 [US1] Add `src/LamuFlix.Infrastructure/Persistence/EfPlaybackHistory.cs`, scoped, mapping a PlaybackEvent to a new record, adding it, and awaiting `SaveChangesAsync` with the same cancellation token on the shared scoped `LamuFlixDbContext`. Do not set the identity. Bounded check: the play flow (`IMovieCatalog` lookup, launcher, and `RecordAsync`) introduces no unrelated tracked entity modifications before `SaveChangesAsync`. Do not add a new context or service. A missing fact goes to recon. Depends on T001, T002, and T008.
- [X] T012 [US1] Register scoped `IPlaybackHistory` to `EfPlaybackHistory` in `AddLamuFlixPersistence` inside `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (FR-011). Depends on T011.
- [X] T013 [P] [US1] Extend `tests/LamuFlix.UnitTests/Persistence/PersistenceCompositionTests.cs` so the existing persistence registration resolves scoped `IPlaybackHistory` as `EfPlaybackHistory` (FR-025). Depends on T012.
- [X] T014 [P] [US1] Extend `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs` so the model includes `playback_events` with the FR-014 integer generated-identity key (`int Id`), required `MovieId` and `PlayedAt`, nonunique `movie_id` index, and cascade foreign key. Do not rewrite unrelated assertions. Depends on T009 and T010.

**Checkpoint**: T013 and T014 pass. Do not run the new PostgreSQL proofs yet; the migration does not exist.

---

## Phase 3: Migration and database proof (Q7 step 3, plus step 2 proofs that need the table)

**Goal**: Tooling adds `playback_events`. Upgrade, downgrade, and reapply preserve movie data. The adapter proofs run against real PostgreSQL.

**Independent Test**: Migration count is 3 and the application table set is the recon-DEV-351:41 nine plus `playback_events`. A fresh context reads an appended row. Two appends at one clock instant have two identities. Deleting one of two movies removes that movie's events and leaves the sibling movie and its events at their prior counts. Down discards playback events and preserves movies.

- [ ] T015 [US1] Generate migration `AddPlaybackEvents` with the existing EF Core tooling against `LamuFlixDbContext`, using `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` without printing it and without a hardcoded connection string. Do not hand-edit the new migration, its designer, or `src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs`. Down drops only `playback_events` (FR-016, FR-017). Depends on T009, T010, and T012.
- [ ] T016 [P] [US1] Update `tests/LamuFlix.IntegrationTests/MigrationTests.cs` from two migrations and nine tables to three migrations and ten tables. Keep `20260930153520_Initial` and `20261016195556_AddMovieWriters`, and expect the generated `AddPlaybackEvents` name (recon-DEV-351:31,41). The ten tables are `movies`, `actors`, `directors`, `genres`, `writers`, `movie_actors`, `movie_directors`, `movie_genres`, `movie_writers`, and `playback_events`. Prove upgrade from the two-migration schema with movie data preserved, the FR-014 columns (integer generated identity, required `MovieId`, required `PlayedAt`), nonunique `movie_id` index, and cascade foreign key, then downgrade without losing movie data, then reapply. Down drops `playback_events` and discards recorded playback events by design while preserving all `movies` rows. Depends on T015.
- [ ] T017 [P] [US1] Add `tests/LamuFlix.IntegrationTests/EfPlaybackHistoryTests.cs` on `PostgresFixture`. Using the real `EfPlaybackHistory`: a fresh `LamuFlixDbContext` reads the appended row; two plays at the same clock instant yield distinct identities; an unknown movie id is rejected. UTC round-trip comparison uses a deterministic fixed `TimeProvider` instant aligned to database-supported precision (whole seconds, or whole milliseconds or microseconds, and not sub-microsecond 100-nanosecond ticks) and does not import a precision width. Cascade proof seeds two movies with known event counts, deletes one owner, and asserts exact counts: that movie's events are gone, and the sibling movie and its events remain at the seeded counts (FR-025, SC-005). Depends on T015.

**Checkpoint**: T016 and T017 pass. Handler recording assertions may still be red.

---

## Phase 4: Handler wiring and endpoint acceptance (Q7 step 4)

**Goal**: A successful launch awaits a durable append before `Unit`. The seeded endpoint proof shows 204 means one row, and disabled LocalPlay means 403, no process, and no row.

**Independent Test**: T005's recording cases pass. `PlaybackHistoryEndpointTests` is the acceptance proof and uses the real adapter, not a port substitute (FR-026).

- [ ] T018 [US1] In `src/LamuFlix.Core/Features/Playback/PlayMovieCommandHandler.cs`, after the existing catalog resolution and launcher call succeed, sample `TimeProvider.GetUtcNow()`, await `RecordAsync` with that movie id, that instant, and the handler cancellation token, and only then return `Unit`. Do not append on the existing missing, disabled, or failed paths. Do not catch, retry, or launch again (FR-003–FR-009, FR-020). This turns T005's recording cases green. Depends on T004 and T005.
- [ ] T019 [US1] Add `tests/LamuFlix.IntegrationTests/PlaybackHistoryEndpointTests.cs` with a seeded movie, a substituted `IProcessStarter`, and the real `EfPlaybackHistory` resolved through production `AddLamuFlixPersistence` on that host (unit composition coverage stays in T013). Do not construct the adapter by hand. A 204 response implies exactly one persisted event for that request. Disabled LocalPlay returns 403, starts no process, and writes no row. Any persisted `PlayedAt` comparison uses a deterministic fixed UTC instant aligned to database-supported precision, as in T017, and does not import a precision width. One bounded case: `IPlaybackHistory.RecordAsync` throws after launch, the response is not 204, and the request stays on existing unhandled handling. Do not add a mapper, status, DTO, or baseline. A response shape recon does not document goes to recon. Bounded check: the play flow introduces no unrelated tracked writes on the shared context before `SaveChangesAsync`. Do not add a new context or service (FR-026). Depends on T015 and T018.

**Checkpoint**: US1 acceptance proof is green, including T005 and T019.

---

## Phase 5: Unchanged contract and delivery gates (Q7 step 5)

**Purpose**: Prove the public contract did not move, review the Q7 diff envelope, then run the delivery gates. No threshold edits. Complete planned coverage of FR-013, FR-021, and FR-022 is not claimed until the T021 review exists and is read back.

- [ ] T020 Run the existing OpenAPI drift test in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` and leave `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`, `web/src/api/openapi.json`, and `web/src/api/types.ts` unedited (FR-019, recon-DEV-351:43). Depends on T018.
- [ ] T021 Review the three-dot `base...head` diff envelope, then run delivery verification from live `harness.yml` and record each verdict separately: build, `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` and the constitution refactor-gate invocation of that script, `./scripts/run-jetbrains-inspectcode.ps1`, `dotnet test`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, and `pwsh -NoProfile -File ./scripts/run-mutation.ps1`. The diff review confirms `Movie`, `IMovieRepository`, and `EfMovieRepository` are untouched (FR-013), no new NuGet or npm package, project, top-level folder, or layer was added (FR-021), and every modified or added file stays inside the Q7 envelope (FR-022). Do not claim complete planned coverage of those three requirements until this review exists and is read back. Do not edit `harness.yml` or invent a threshold. Do not report scope-empty SKIPPED, opt-out SKIP, mutation N/A, or Could not run as PASS. Depends on T016, T017, T019, and T020.

**Checkpoint**: Drift is zero and every gate has its own verdict. This phase does not authorize merge. It runs only after spec analysis, plan challenge adjudication, and owner Gate 1. Gate 1 is not a step that follows delivery.

---

## Dependencies & Execution Order

### Phase Dependencies

- Spec analysis, plan challenge adjudication, and owner Gate 1 come before every implementation and delivery task. Phase 1 has no other feature prerequisite. T002 depends on T001. T003 depends on T001. T004 depends on T001 and T002. T005 and T006 depend on T004. T007 depends on T004, and on T012 when that host uses production persistence.
- Phase 2 depends on T001 and T002. T009 and T010 depend on T008. T011 depends on T008. T012 depends on T011. T013 depends on T012. T014 depends on T009 and T010.
- Phase 3 depends on Phase 2. T016 and T017 depend on T015 and run after the migration exists.
- Phase 4 depends on Phase 1's tests and on T015 for the endpoint proof. T018 depends on T004 and T005. T019 depends on T015 and T018.
- Phase 5 depends on T016, T017, T019, and T020.

### User Story Dependencies

- **User Story 1 (P1)**: the only story. No later story. Negative paths, repeats, and cascade delete are scenarios of US1 (`spec.md`).

### Parallel Opportunities

- T005 and T006 together after T004. T007 joins them only when it does not need T012.
- T009 and T010 together after T008.
- T013 and T014 together once T012, T009, and T010 are done.
- T016 and T017 together after T015.

---

## Parallel Example: Phase 3

```text
Task: "T016 Update tests/LamuFlix.IntegrationTests/MigrationTests.cs"
Task: "T017 Add tests/LamuFlix.IntegrationTests/EfPlaybackHistoryTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

Prerequisites, in this order, before any implementation or delivery work:

1. `/speckit-analyze` on this draft.
2. Plan challenge and adjudication.
3. Owner Gate 1, including unchecked OD-1. Gate 1 stays closed until the owner answers it. Step 6 corrections do not check OD-1, freeze the plan, or authorize implementation or delivery.

Brief Q7's implementation order applies only after those prerequisites:

4. Phase 1: domain record, port, handler tests, and the routing-host dependency edit. The end-to-end host edit is T007 and waits for production registration when that host uses it.
5. Phase 2: persistence types, adapter, DI, composition, and model metadata.
6. Phase 3: generated migration, migration regressions, adapter PostgreSQL proof.
7. Phase 4: handler recording body and seeded endpoint acceptance.
8. Phase 5: drift check and delivery gates.

There is no second story to ship incrementally. Out of scope in `spec.md` is not a follow-on phase.

### Within US1

- Handler recording tests are written in T005 and become green in T018.
- Persistence code is in place before the migration is generated.
- PostgreSQL tests run only after T015.
- The acceptance proof is T019, not the port substitute in T006.

---

## Notes

- Q7 names the focused files; this list names `PlaybackEventTests.cs`, `EfPlaybackHistoryTests.cs`, and `PlaybackHistoryEndpointTests.cs` inside the existing test projects. The `PlaybackEventTests.cs` path and namespace follow OD-1 (T003). Their responsibilities are the domain shape, the adapter database proof, and the seeded endpoint proof. They do not add reads, UI, or a second port.
- Closing bar stays zero unresolved Critical, High, or Medium findings. The two-round cap stays. No third fix round. The Q7 envelope stays. Prerequisites stay before implementation and delivery. OD-1 stays unchecked. Complete planned coverage of FR-013, FR-021, and FR-022 is not claimed from this draft.
- `ApiEndToEndPlaybackTests.cs` is not the acceptance proof. recon-DEV-351 names the file and does not describe its host.
- No task edits `ArchitectureTests`, `CONTEXT.md`, an ADR, `README.md`, `AGENTS.md`, OpenAPI baselines, or `harness.yml`.
- No task adds a package, project, or layer.
- Commit grouping is the implementer's. Do not claim delivery while T005's recording cases are still red.
