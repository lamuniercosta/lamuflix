# Tasks: DEV-352 Explicit watched and not-interested marks

**Input**: Design documents from `specs/DEV-352/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). `setup-tasks.ps1 -Json` reported no `research.md`, `data-model.md`, `contracts/`, or `quickstart.md`. The plan records why.

**Unresolved owner checkboxes** (Q11). The historical grill statement of zero owner checkboxes and zero constitution departures is explicitly superseded by Q11. Both checkboxes stay unresolved. No Patron ruling, drafting fix, or prerequisite success resolves them. Gate 1 stays closed until the user resolves both on the spec PR and the spec PR merges. Plan challenge freeze is recorded in plan.md; it does not authorize implementation. Do not start Phase 1 until both checkboxes are resolved and the spec PR merge establishes Gate 1.

- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:219-220 (all commands validated through FluentValidation in the validation decorator) for the four MovieId-only feedback commands by validating route id via MovieId.TryCreate (HTTP 422) at the endpoint boundary without separate IValidator<TCommand> classes, while retaining the existing validation decorator through AddHandler?
- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:335-342 (single fresh Initial migration without constitution amendment/ADR) by generating a reversible additive EF migration for movies.is_watched and movies.is_not_interested without editing 20260930153520_Initial.cs?

**Tests**: Included. Brief Q7 requires them. Author a behavior test before the production edit it locks, and confirm that test fails, when it can compile against types that already exist. When the brief's method or type is not in the tree yet, a compile failure against that missing member is the required red state. Do not weaken the test so that it compiles by skipping the assertion. Run a test to green only after its named production edit. Stand-in repositories are for handler and HTTP orchestration only. Real PostgreSQL is required for persistence, serialization, and migration. No EF InMemory. No mocked data-access driver. No new test library.

**Organization**: Phases follow brief Q8. There is one frozen delivery, split into US1, US2, and US3 only so each proof can be named. Do not start Phase 1 until the user has resolved both owner checkboxes above and the spec PR has merged, which is what establishes Gate 1 (brief handoff). Do not write production code before that. A completed drafting fix does not establish Gate 1.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 mark and unmark, US2 read the marks, US3 upgrade and downgrade.
- Exact file paths are in each description. Q12 explicitly names the existing persistence and projection files; historical recon citations below are labeled as such.

## Phase 1: Domain transitions and tests (US1)

**Purpose**: Both marks, the four transitions, eight flag combinations, and watchlist isolation on `Movie`. Blocks later phases.

**Independent Test**: A transition table covers each legal mark and unmark, each repeat, every combination of the three flags, and preservation of the two flags the action does not touch.

**Checkpoint**: New movies start with both marks false. A legal call flips one mark. A repeat throws and saves nothing on the movie. The other mark and the watchlist stay put.

- [X] T001 [P] [US1] Extend `tests/LamuFlix.Test/Domain/MovieTests.cs` with the watched and not-interested transition table: legal mark and unmark for each flag, repeat mark and unmark, all eight combinations of `IsWatched`, `IsNotInterested`, and `IsInWatchlist`, and preservation of both untouched flags. Assert each repeat throws `InvalidTransitionException` with the method-name action and the const state token from Q9: MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, UnmarkNotInterested/Interested. Interested means only `IsNotInterested == false`, the `[assumed]` wording in `specs/DEV-352/ASSUMPTIONS.md`. A compile failure because the four methods are absent is the required red state. Confirm the new cases fail before T003.
- [X] T002 [P] [US1] Extend `tests/LamuFlix.Test/Domain/PropertyTests.cs` so the existing state-sequence coverage includes feedback sequences and watchlist isolation: any one of the three flags can change without changing the other two. Confirm the new cases fail before T003. Keep this file off T001.

---

- [X] T003 [US1] After T001 and T002 are failing, add `IsWatched` and `IsNotInterested` to `src/LamuFlix.Core/Domain/Movie.cs`, both false on a new movie. Append `isWatched` and `isNotInterested` after the existing `isInWatchlist` parameter of `Rehydrate` without reordering existing parameters. Add `MarkWatched`, `UnmarkWatched`, `MarkNotInterested`, and `UnmarkNotInterested`. A legal call flips only that mark. A repeat throws `InvalidTransitionException` with the method name as the action and a const string state token, and changes nothing. The pairs are MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, and UnmarkNotInterested/Interested (Q9). Interested means only `IsNotInterested == false`. Do not clear the watchlist and do not add a timestamp.

## Phase 2: Persistence, rehydration, and generated migration (US1, US3)

**Purpose**: Two required columns, a round-trip through the existing movie mapping, and one generated reversible migration. Depends on Phase 1.

**Independent Test**: A stored movie reloads both marks. Preexisting rows read false after upgrade. Downgrade removes only the two new columns.

**Checkpoint**: `is_watched` and `is_not_interested` exist on `movies`, required, default false. Initial is unchanged. Generated migration files are not hand-edited.

- [X] T004 [US1] Add `IsWatched` and `IsNotInterested` to `src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs` after `IsInWatchlist`. Map `is_watched` and `is_not_interested` in `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs` after `is_in_watchlist`, as required booleans using `.HasColumnType("boolean").IsRequired()` without `.HasDefaultValue(false)`, matching the existing watchlist mapping (Q12; Ledger M1). Domain initialization is false; T006/T023 verify migration false backfill/default for preexisting rows. The earlier intake recon citations are historical and are not the current MSW snapshot. Do not edit `src/LamuFlix.Infrastructure/Persistence/Migrations/20260930153520_Initial.cs`.
- [X] T005 [US1] After T004, extend `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` at `ToRecord`, `ToDomain` / `Movie.Rehydrate`, `Baseline`, `Baseline.From`, and `ApplyState` for both flags. Capture each original value at load and update only its changed record property, preserving untouched sibling flags, including watchlist, under stale-load interleaves. Cover insert, restore and update paths; adding only insert/restore is insufficient (Q12). Do not add a file, a port, or a Mapster registration under `src/`.
- [X] T006 [US3] After T004, generate one additive migration, its designer, and the updated `src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs` with EF tooling. Do not hand-edit the generated designer or snapshot. Do not edit `src/LamuFlix.Infrastructure/Persistence/Migrations/20260930153520_Initial.cs`. Do not add an ADR or a constitution amendment. The timestamped filename is the tooling output. Upgrade must add only `is_watched` and `is_not_interested` and set preexisting rows false. Downgrade must drop only those two columns. Generating this migration without a constitution amendment or ADR is the second unresolved owner checkbox in Prerequisites. Immediately update the existing migration count/name tripwire in `tests/LamuFlix.IntegrationTests/MigrationTests.cs`, `MigrateAsync_EmptyDatabase_AppliesInitialAndHasNoPendingModelChanges`, from 2 to 3, retaining exact previous names and asserting the tooling-generated migration name. Do this before the Phase 2 commit rather than waiting for T023. Completing T006 does not approve that departure. Gate 1 stays closed until the user resolves it and the spec PR merges.

## Phase 3: Command and handler pairs and registration (US1)

**Purpose**: Four operations that load, transition, and save through the existing repository. Depends on Phases 1 and 2.

**Independent Test**: For each operation, success saves once and changes only its flag. A missing movie and a repeat save zero.

**Checkpoint**: Four sealed pairs are registered with `AddHandler`. No new repository method.

- [X] T007 [P] [US1] Add failing handler tests in `tests/LamuFlix.UnitTests/Features/Feedback/MarkWatchedCommandHandlerTests.cs`, mirroring `tests/LamuFlix.UnitTests/Features/Watchlist/AddToWatchlistCommandHandlerTests.cs`. Success saves once and sets only watched. Unknown id throws `NotFoundException` and does not save. A repeat propagates `InvalidTransitionException` and does not save. Use NSubstitute for `IMovieRepository` only. Assert `HandleAsync_ForwardsCancellationToken` forwards the supplied token to both `GetAsync` and `SaveChangesAsync`. Confirm failure before T011.
- [X] T008 [P] [US1] Add the matching failing tests in `tests/LamuFlix.UnitTests/Features/Feedback/UnmarkWatchedCommandHandlerTests.cs`. Success clears only watched and saves once. Missing movie and repeat save zero. Assert `HandleAsync_ForwardsCancellationToken` forwards the supplied token to both `GetAsync` and `SaveChangesAsync`. Confirm failure before T012.
- [X] T009 [P] [US1] Add the matching failing tests in `tests/LamuFlix.UnitTests/Features/Feedback/MarkNotInterestedCommandHandlerTests.cs`. Success sets only not interested and saves once. Missing movie and repeat save zero. Assert `HandleAsync_ForwardsCancellationToken` forwards the supplied token to both `GetAsync` and `SaveChangesAsync`. Confirm failure before T013.
- [X] T010 [P] [US1] Add the matching failing tests in `tests/LamuFlix.UnitTests/Features/Feedback/UnmarkNotInterestedCommandHandlerTests.cs`. Success clears only not interested and saves once. Missing movie and repeat save zero. Assert `HandleAsync_ForwardsCancellationToken` forwards the supplied token to both `GetAsync` and `SaveChangesAsync`. Confirm failure before T014.
- [X] T011 [P] [US1] After T007 fails, add `MarkWatchedCommand` and `MarkWatchedCommandHandler` in `src/LamuFlix.Core/Features/Feedback/MarkWatchedCommand.cs` and `src/LamuFlix.Core/Features/Feedback/MarkWatchedCommandHandler.cs`. Sealed record command, sealed handler. `GetAsync`, `NotFoundException` when missing, `MarkWatched`, `SaveChangesAsync` once, `Unit.Value`; forward the supplied CancellationToken to both async calls. T012–T014 retain the same forwarding requirement.
- [X] T012 [P] [US1] After T008 fails, add `UnmarkWatchedCommand` and `UnmarkWatchedCommandHandler` in `src/LamuFlix.Core/Features/Feedback/UnmarkWatchedCommand.cs` and `src/LamuFlix.Core/Features/Feedback/UnmarkWatchedCommandHandler.cs`, using `UnmarkWatched`.
- [X] T013 [P] [US1] After T009 fails, add `MarkNotInterestedCommand` and `MarkNotInterestedCommandHandler` in `src/LamuFlix.Core/Features/Feedback/MarkNotInterestedCommand.cs` and `src/LamuFlix.Core/Features/Feedback/MarkNotInterestedCommandHandler.cs`, using `MarkNotInterested`.
- [X] T014 [P] [US1] After T010 fails, add `UnmarkNotInterestedCommand` and `UnmarkNotInterestedCommandHandler` in `src/LamuFlix.Core/Features/Feedback/UnmarkNotInterestedCommand.cs` and `src/LamuFlix.Core/Features/Feedback/UnmarkNotInterestedCommandHandler.cs`, using `UnmarkNotInterested`.
- [X] T015 [US1] After T011 through T014, register the four handlers in `src/LamuFlix.Api/HandlerRegistration.cs` with `AddHandler`, appended after the existing registrations, in the order MarkWatched, UnmarkWatched, MarkNotInterested, UnmarkNotInterested. Retain the existing validation decorator that `AddHandler` already applies. Do not register a separate `IValidator<TCommand>` for these four MovieId-only commands. Endpoint `MovieId.TryCreate` (HTTP 422) stays on T017. That omission is the first unresolved owner checkbox in Prerequisites. It is pending user approval. It is not a compliant or approved exception. Completing T015 does not approve it.

## Phase 4: HTTP endpoints (US1)

**Purpose**: The four public operations and the status matrix. Depends on Phase 3.

**Independent Test**: For every operation: empty 204, 409 problem details on a repeat, 404 for an unknown positive id, 422 with errors containing id for a nonpositive id, and 400 for a malformed or overflowing id.

**Checkpoint**: `MapFeedbackEndpoints` is on the existing `/api` group. Watchlist endpoints and the exception handler are unchanged.

- [ ] T016 [US1] Add `tests/LamuFlix.IntegrationTests/FeedbackEndpointTests.cs` mirroring `tests/LamuFlix.IntegrationTests/WatchlistEndpointTests.cs`. Cover all four routes. Legal change returns empty 204 and saves once. Repeat returns 409 problem+json with title Conflict, saves zero, and carries the Q9 pair for that operation: MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, UnmarkNotInterested/Interested. Across all four operations, assert zero saves for an unknown positive id (404), a nonpositive id (422 with errors containing id), and a malformed or overflowing id (400), matching SC-003. Keep this full success, conflict, and status matrix. Build the host with `ApiHostFactory` and substitute `IMovieRepository`. Confirm the success, conflict, and zero-save id cases fail before T017.
- [ ] T017 [US1] After T016 fails, add `src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs` and append `MapFeedbackEndpoints` in `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` without reordering the existing maps. Untyped `{id}`. Operation names MarkWatched, UnmarkWatched, MarkNotInterested, UnmarkNotInterested. `.Produces(204)` and `.ProducesProblem` for 400, 404, 409, and 422. Local `ParseMovieId` uses `MovieId.TryCreate` and the existing id error text from `WatchlistEndpoints`. Success returns `TypedResults.NoContent()`. Do not edit `src/LamuFlix.Api/Endpoints/WatchlistEndpoints.cs` or `src/LamuFlix.Api/ExceptionHandling/GlobalExceptionHandler.cs`.

## Phase 5: Read fields and projections (US2)

**Purpose**: Existing list and details expose the two marks. Depends on Phases 1–4 for a stored mark, and on the ports for the field set.

**Independent Test**: Exact field-set assertions list the previous fields plus `isWatched` and `isNotInterested`, and no extra field.

**Checkpoint**: Browse and details responses carry both required bools. No new GET route and no new filter.

- [ ] T018 [US2] Before T019, extend the exact field-set assertions to require `isWatched` and `isNotInterested` and no other new field in `tests/LamuFlix.IntegrationTests/ApiEndToEndLibraryTests.cs`, `tests/LamuFlix.IntegrationTests/ApiEndToEndBrowseTests.cs`, `tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs`, and `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs`. Confirm those assertions fail before T019. Do not add a browse filter assertion.
- [ ] T019 [US2] After T018 fails, add required `bool IsWatched` and `bool IsNotInterested` after `IsInWatchlist` on `src/LamuFlix.Core/Ports/MovieSummary.cs`, `src/LamuFlix.Core/Ports/MovieDetails.cs`, and `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` (`MovieSummaryResponse` and `MovieDetailsResponse`). Extend `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` at `BrowseAsync`, `GetDetailsAsync` and `MovieDetailsRecord` for both marks (Q12). Keep watched and not-interested fields distinct; touch only the authorized projections. Do not add a GET route or a filter on `MovieQuery`.

## Phase 6: Generated contract and forced web compatibility (US2)

**Purpose**: OpenAPI, Verify, generated TypeScript, and only the web files the new fields force. Depends on Phases 4 and 5.

**Independent Test**: The OpenAPI document exported from the C# contract, the Verify baseline, generated `web/src/api/types.ts`, and generated `web/src/mocks/handlers.ts` describe the four operations and both read fields. `npm run generate:api` does not export that document. Forced web checks in T025 each have their own verdict.

**Checkpoint**: `types.ts` and `handlers.ts` are generated, not hand-edited. No UI control is added.

- [ ] T020 [US2] After T017 and T019, export committed `web/src/api/openapi.json` from the C# contract with the existing contract flow first. Then run `npm run generate:api`, which reads that document and writes `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (replacement recon-DEV-352:13-27). That command does not export OpenAPI. Update `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` through the existing Verify flow in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`. Do not hand-edit `web/src/api/types.ts`, `web/src/mocks/handlers.ts`, or the snapshot.
- [ ] T021 [US2] After T020, change only these handwritten files, and only where the new required fields or the four operations force a failure: `web/src/features/movie-details/movieDetailsApi.ts`, `web/src/api/client.typecheck.ts`, `web/src/mocks/fixtures.ts`, and `web/src/mocks/handlers.test.ts` (replacement recon-DEV-352:38-68). Also update `web/src/mocks/generate-api.test.ts`, which is inside this bounded forced-compatibility envelope: replace the ten-operation lock with the exact previous ten plus the four approved operation identities (14), and retain the existing byte and staleness checks (replacement recon-DEV-352:29-34). T025 records a separate verdict for that generator and generated-handler regression, and separate verdicts for web typecheck, runtime-schema and contract regression tests, web tests, web build, and generated-output drift checks. Lint alone does not schedule those checks. Use existing documented tooling. Do not invent command flags or thresholds. Running the checks is implementation-stage work. Do not hand-edit `web/src/api/types.ts` or `web/src/mocks/handlers.ts`. Do not add a UI control, a client abstraction, or a handwritten duplicate of the generated wire types.

## Phase 7: Real-database and migration regressions (US1, US3)

**Purpose**: Persistence and migration claims that a stand-in repository cannot prove. Depends on Phases 2–6.

**Independent Test**: Real PostgreSQL reloads both marks, serializes them on list and details, keeps watchlist and feedback independent in both directions, defaults preexisting rows to false on upgrade, and keeps previous data on downgrade.

**Checkpoint**: No data-access driver is mocked for these proofs.

- [ ] T022 [US1] Extend `tests/LamuFlix.IntegrationTests/ApiEndToEndLibraryTests.cs` to exercise all eight `(IsWatched, IsNotInterested, IsInWatchlist)` combinations through save, fresh-context reload, browse and details serialization, including watched-only and not-interested-only values. Also prove stale-load interleaves with separate repository/DbContext instances: A loads, B loads and commits a sibling change, A changes its own flag and saves; a fresh context observes both changes. Exercise each distinct pair of the three flags, both save orders and mark/unmark directions. Include same-target overlapping operations that both loaded a valid pre-state (both may succeed), followed by an operation that observes the committed target state and conflicts without saving. This is Q12 concurrency semantics, not a serialisation guarantee. Use the existing real PostgreSQL fixture. Do not mock EF translation or serialization.
- [ ] T023 [US3] After T006, extend the existing `tests/LamuFlix.IntegrationTests/MigrationTests.cs` (Q10). Give each proof its own database from `PostgresFixture.CreateEmptyDatabaseContextAsync`. Never downgrade the shared fixture database. Up calls `IMigrator.MigrateAsync("20261016195556_AddMovieWriters")` then seeds a nonempty set of rows while still on that prior schema, captures their positive count and original data, and calls `MigrateAsync(null)`. Assert unchanged positive row count, preserved original values and both flags false on every seeded row. Down calls `MigrateAsync(null)` and then `MigrateAsync("20261016195556_AddMovieWriters")`. Assert `is_watched` and `is_not_interested` are required and default false, preexisting rows and the prior schema and data survive both directions, and Down removes only those two columns. Retain the migration count/name tripwire already updated with T006; do not postpone it to this phase. Completing T023 does not resolve the additive-migration owner checkbox.

## Phase 8: Verification and review

**Purpose**: Applicable gates, then the size:M review. Not a behavior phase. Depends on Phases 1–7.

**Checkpoint**: Each mandatory gate has its own verdict. Scope-empty skip, opt-out skip, and mutation not-applicable are not recorded as pass. Critical and High findings are closed or still blocking. No third review round is opened.

- [ ] T024 Refactor the completed change across the file set in `specs/DEV-352/plan.md` until the applicable refactor complexity gate passes using live configuration and existing documented tooling: `./scripts/run-cyclomatic-complexity.ps1` without a literal threshold override (Q12). Resolve any configuration/procedure mismatch before execution; never lower a threshold. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit `harness.yml`.
- [ ] T025 Run the configured gates and record each verdict separately: build; OpenAPI snapshot/drift through the existing `OpenApiContractTests` verification with its own explicit verdict; `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no threshold argument, `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and `./scripts/run-mutation.ps1` without `--since`. Record a separate verdict for each applicable existing web check, using existing documented tooling and no invented command flags or thresholds: web typecheck; runtime-schema and contract regression tests; web tests; web build; generated-output drift checks; and the generator and generated-handler regressions in `web/src/mocks/generate-api.test.ts` from T021. `npm run lint` alone does not schedule that set (brief Q8/Q11 and historical intake recon-DEV-352:59, not the current MSW snapshot; verify existing commands at implementation). Running these checks is implementation-stage work, not this drafting round. A failure or `Could not run` blocks. Do not treat scope-empty `SKIPPED`, opt-out `SKIP`, or mutation `NOT APPLICABLE` as pass.
- [ ] T026 Apply the loop discipline in `specs/DEV-352/plan.md`: Sentry/Risk, Ledger/Standards, and Compass/Spec reports before adjudication. A missing axis blocks. Critical and High block. Medium and Low stay recorded. At most two formal review and remediation rounds. Unresolved blockers at the cap stay blocked.

---

## Excluded (never scheduled)

UI controls, browse filters, recommendations, ranking, timestamps, watch history, identity, a separate feedback table, a new dependency or project or port, `Features:LocalPlay`, secrets, `Process.Start`, edits to `WatchlistEndpoints.cs`, edits to `GlobalExceptionHandler.cs`, edits to the Initial migration, hand-edits of generated migration files, `web/src/api/types.ts`, or `web/src/mocks/handlers.ts`, a root `CONTEXT.md` rewrite, an ADR, a constitution amendment, and a README change. A discovered need outside this list stops for a decision. It is not absorbed into a phase. Resolving either owner checkbox is the user's spec-PR decision, not a task in this list.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No earlier phase. T001 and T002 fail before T003. Blocks Phases 2–7.
- **Phase 2**: Depends on Phase 1. T004 before T005 and T006. T006 does not wait on T005.
- **Phase 3**: Depends on Phases 1–2. T007–T010 fail before the matching command task. T015 after T011–T014.
- **Phase 4**: Depends on Phase 3. T016 fails before T017.
- **Phase 5**: Depends on Phase 4 for a callable mark, and on Phase 2 for a stored mark. T018 fails before T019.
- **Phase 6**: Depends on T017 and T019. T020 before T021.
- **Phase 7**: Depends on Phases 2–6. T022 uses the library end-to-end file. T023 waits on T006. Q10 names the existing migration test file, so T023 does not wait on a drafting question.
- **Phase 8**: Depends on Phases 1–7. T024 before T025. T026 after the gate verdicts exist.

### User Story Dependencies

- **US1 (P1)**: Starts at T001. Handler and HTTP proofs are T007–T017. The real-database behavior proof is T022.
- **US2 (P2)**: Starts at T018 after the marks exist on the movie. Contract proof is T020 and T021.
- **US3 (P3)**: The generated migration is T006. The upgrade and downgrade proof is T023. It is in the same frozen scope as US1.

### Within Each User Story

- Author the behavior test and confirm it fails before the production edit it locks, except T006, whose generated migration is the artifact a later test asserts, and T020, which regenerates committed contracts after the endpoints and fields exist.
- Domain methods before handlers. Handlers before endpoints. Response fields before regeneration.
- Model configuration (T004) before migration generation (T006).

### Parallel Opportunities

- T001 and T002 are different test files.
- T007, T008, T009, and T010 are different handler test files.
- T011, T012, T013, and T014 are different command files, after their own tests are failing.
- T005 and T006 can proceed together after T004 because T005 owns EfMovieRepository.cs while T006 generates migrations and updates MigrationTests.cs. T006 only reads the EF model from T004.
- T018 shares `ApiEndToEndLibraryTests.cs` with T022, so T022 stays after T018.

## Parallel Example: Phase 3

```text
Task: "T007 MarkWatchedCommandHandlerTests in tests/LamuFlix.UnitTests/Features/Feedback/MarkWatchedCommandHandlerTests.cs"
Task: "T008 UnmarkWatchedCommandHandlerTests in tests/LamuFlix.UnitTests/Features/Feedback/UnmarkWatchedCommandHandlerTests.cs"
Task: "T009 MarkNotInterestedCommandHandlerTests in tests/LamuFlix.UnitTests/Features/Feedback/MarkNotInterestedCommandHandlerTests.cs"
Task: "T010 UnmarkNotInterestedCommandHandlerTests in tests/LamuFlix.UnitTests/Features/Feedback/UnmarkNotInterestedCommandHandlerTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 and confirm the transition table.
2. Finish Phases 2–4 so a client can mark and unmark with the ruled status codes.
3. Stop there only to validate US1. The frozen ticket scope still requires US2 and US3 before review.

### Incremental Delivery

US2 is the read-back and contract phase. US3 is the migration proof. Neither is a follow-up. Phase 8 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. The parallel tasks above are the only safe splits. Do not split two writers onto `MovieTests.cs`, `ApiEndToEndLibraryTests.cs`, or `HandlerRegistration.cs`.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Brief Q8 places property regressions with the final verification. The property cases are authored in T002 (brief Q7 item 1). T025 re-runs them. That split is the brief, not a second property suite.
- Q9 closes the conflict-token question. T001 and T003 assert the exact pairs and do not stop for them. Q10 closes the migration-file question. T023 extends the existing `MigrationTests.cs` and does not stop for it. The two owner checkboxes in Prerequisites stay unresolved. Gate 1 stays closed.
- Commit only at green boundaries using `DEV-352 - {phase subject}`. Update the migration tripwire in T006 before committing Phase 2. Treat Phases 4–6 (T016–T021) as one commit group after C# endpoint/read changes, OpenAPI baseline regeneration and forced web compatibility pass; do not commit known red OpenAPI or contract windows after Phase 4 or 5. Keep task order unchanged. Other phase boundaries also require existing affected checks green (Q12).
- Gate 1 comes from the user resolving both owner checkboxes and merging the spec PR. This task list does not authorize implementation before that.
