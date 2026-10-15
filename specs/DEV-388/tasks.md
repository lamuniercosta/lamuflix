# Tasks: DEV-388 Retire legacy Web and Worker hosts

**Input**: Design documents from `specs/DEV-388/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md` Q1–Q9).

**Prerequisites**: `plan.md` (required), `spec.md` (required). `research.md`, `data-model.md`, `contracts/`, and `quickstart.md` are not separate files; their decisions are in the brief and the plan design overview.

**Pre-implementation prerequisite** (brief.md task ordering 1–2): Setup and Foundational do not authorize story work. Before any story task (T003 onward), all of the following are complete:

- Plan challenge and freeze are complete.
- The separate owner Gate 1 is complete. Gate 1 is not the plan freeze.
- Phase B pickup has fresh main drift/overlap recon and a fresh analyze. Amend task planning only for demonstrated drift. Do not treat intake PR or HEAD snapshots as current.

**Tests**: Included. The ticket AC and Patron Q7 require handler tests, PostgreSQL Testcontainers tests, contract and snapshot coverage, and Vitest/RTL tests. Write the tests so they fail before the production change they cover. Do not mock the data-access driver for cascade behavior. No Gherkin tasks.

**Organization**: Setup, then a no-schema foundation, then the pre-implementation prerequisite, then User Story 1 (delete), User Story 2 (Worker), User Story 3 (Web), then polish. User Story 2 may proceed beside User Story 1. User Story 3 waits on User Story 1 and on the shared legacy-test edits from User Story 2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: User story for story phases only (`US1`, `US2`, `US3`)
- Exact file paths are in each description

## Phase 1: Setup

**Purpose**: Pin the worktree change set before any edit.

**Checkpoint**: Later commits exclude the pre-existing `harness.yml` diff.

- [ ] T001 Confirm the worktree `F:/Dev/LamuFlix.worktrees/feature-388-spec` is on `feature/388-spec` with `.specify/feature.json` pinning `specs/DEV-388`, and exclude `harness.yml` from every DEV-388 commit.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Freeze the schema and dependency boundary that every story must keep.

**Checkpoint**: No migration, package, project, or new port is part of the change. Story work waits on the pre-implementation prerequisite.

- [ ] T002 Leave `src/LamuFlix.Infrastructure/Persistence/Migrations/` unchanged, including `20260930153520_Initial.cs`. Add no package, project, or port. Delete extends the existing `IMovieRepository` port only.

---

## Phase 3: User Story 1 - Remove a movie from the library (Priority: P1)

**Goal**: Metadata-only delete through the existing backend and a confirmed details action, with generated contracts.

**Independent Test**: `DELETE /api/movies/{id}` returns 204 with no body for a present movie and 404 for a repeat or unknown id. An id less than or equal to zero returns 404, and a non-integer id returns 400, both proved on this endpoint. The file on disk, shared actors, directors, genres, and another movie remain. Details confirmation matches cancel, pending, non-404 error, success, and DELETE 404 behavior in the spec.

### Tests for User Story 1

- [ ] T003 [P] [US1] Write failing handler tests for removed-versus-absent outcome propagation in `tests/LamuFlix.UnitTests/Features/Library/`.
- [ ] T004 [P] [US1] Write failing PostgreSQL Testcontainers tests in `tests/LamuFlix.IntegrationTests/` for 204 then repeat 404, an absent id, overlapping same-id deletes that prove exactly one removal (204) and one absence (404) with shared principals and another movie preserved, removal of the movie row with its watchlist and metadata columns, removal of that movie's `movie_actors`, `movie_directors`, and `movie_genres` rows, survival of shared principals and another movie, and the media file remaining on disk.

### Implementation for User Story 1

- [ ] T005 [US1] Add the atomic removed-versus-absent member on `IMovieRepository` in `src/LamuFlix.Core/Ports/` and implement it under `src/LamuFlix.Infrastructure/Persistence/` with no read-then-delete and no migration.
- [ ] T006 [US1] Add the sealed Library command and the sealed handler under `src/LamuFlix.Core/Features/Library/`, registered with the existing handler decorator helper. Map absence to `NotFoundException`. Do not add an id rule, an empty validator, or an authentication mechanism. Do not reference the Watchlist feature folder.
- [ ] T007 [US1] Write failing endpoint behavior tests in `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` for the route, the statuses, and the absence of a request or success payload. Include this endpoint's own invalid-id outcomes: an id less than or equal to zero returns 404, and a non-integer id returns 400. Do not treat the sibling enrichment-route test as DELETE proof. T007 precedes the production endpoint change.
- [ ] T008 [US1] Map `DELETE /api/movies/{id:int}` in `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`: no request body, 204 with no body on removal, 404 problem details on absence and on an id less than or equal to zero, 400 for a non-integer id, no id-specific 422, and no new authentication. The endpoint stays anonymous, matching GET. Depends on T005, T006, and T007.
- [ ] T009 [US1] Regenerate the contract through the existing tooling used by `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`, writing `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together. Do not assume a flag the brief and recon do not name, and do not hand-edit either document.
- [ ] T010 [US1] Generate `web/src/api/types.ts` only with the existing generator. Do not hand-edit it.
- [ ] T011 [US1] Write failing Vitest and React Testing Library tests beside `web/src/features/movie-details/` for confirm, cancel with no request, no second submit while pending, visible error and retry for failures other than 404, return to the library, and invalidated library, watchlist, and details caches. The details assertion must target the key `['movieDetails', movieId]` on the local QueryClient owned by `MovieDetailsView.tsx`, not only an app-level key. Include the DELETE 404 path: confirmed absence invalidates those caches, navigates to the library, and shows no error banner and no retry.
- [ ] T012 [US1] Implement the assumed confirmation control in `web/src/features/movie-details/MovieDetailsView.tsx` using the generated client. On both 204 and 404, invalidate the details cache key `['movieDetails', movieId]` owned by the local QueryClient in that view (`web/src/features/movie-details/movieDetailsApi.ts`, read by `MovieDetailsView.tsx`), and invalidate the library and watchlist caches through their existing owners (`web/src/library/watchlistStore.ts` and `web/src/library/useBrowseMovies.ts`). Invalidating only an app-level key does not prove details invalidation. A 404 navigates to the library with no error banner and no retry. Any other failure leaves details usable with the assumed error and retry.

**Checkpoint**: User Story 1 works with Web and Worker still present. This is the MVP increment for the missing action. It is not ticket completion.

---

## Phase 4: User Story 2 - Retire the legacy worker (Priority: P2)

**Goal**: Remove the Worker host after the Api enrichment replacement is still evidenced. May run beside User Story 1 and before User Story 3.

**Independent Test**: `src/LamuFlix.Worker/` is gone, `tests/LamuFlix.Test/WorkerTests.cs` is gone, the Worker project reference is gone, and the enrichment publisher tests plus the Api-hosted `EnrichmentConsumer`, its wiring, and the processing proofs named below still exist. The solution build does not require Worker.

### Implementation for User Story 2

- [ ] T013 [P] [US2] Confirm these replacements are still present before any Worker deletion: `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs`, `src/LamuFlix.Api/Endpoints/EnrichmentEndpoints.cs`, `tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs`, `tests/LamuFlix.IntegrationTests/EnrichmentEndpointTests.cs`, `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`, its registration in `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs` when both required ports are present, the Api `Program.cs` ordering that registers persistence, the movie catalog, the metadata provider, and then RabbitMQ, `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` (including `Consumer_ASuccessfulOutcome_IsAckedAndTheMovieIsEnriched`, `Consumer_ARetryableOutcome_ReclaimsAndCallsTheProviderAgain`, `Consumer_ADelayedRetryOutcome_ReclaimsAndCallsTheProviderAgain`, and `Consumer_ARetryableFailureBelowMax_RepublishesToRetryAndReturnsThroughTheTtl`), `tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs`, and registration proof in `tests/LamuFlix.UnitTests/RabbitMq/RabbitMqServiceCollectionExtensionsTests.cs`. If one is missing, stop for recon. Do not add a consumer or expand those proofs.
- [ ] T014 [US2] Delete the whole `src/LamuFlix.Worker/` tree, including `appsettings` files, `Program.cs`, and `LamuFlix.Worker.csproj`.
- [ ] T015 [US2] Delete `tests/LamuFlix.Test/WorkerTests.cs`. In `tests/LamuFlix.Test/EnrichmentTests.cs`, remove only the Worker tests bounded recon-DEV-388 section 2 names: `OmdbMetadataProvider_Success_ReturnsMetadata`, `OmdbMetadataProvider_NotFound_ReturnsNull`, `OmdbMetadataProvider_ServerError_ThrowsException`, `OmdbMetadataProvider_MissingApiKey_ThrowsInvalidOperationException`, `EnrichmentJobProcessor_TransitionsToEnriched`, `EnrichmentJobProcessor_TransitionsToNotFound_RowRemainsVisible`, `EnrichmentJobProcessor_Idempotency_SkipsAlreadyEnriched`, `EnrichmentJobProcessor_RetryOnFailure_RepublishesWhenRetryCountBelowThree`, and `EnrichmentJobProcessor_ExceedsMaxRetries_MarksFailedAndForwardsToDlq`, plus declarations that exist only to support those tests. Inventory remaining members before the edit. Keep `FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage`, the assembly fixture (`ContainerFixtureForTests` and the assembly fixture attribute), and unrelated behavior. Do not delete the file because it has no Core usage. Remove only the Worker `ProjectReference` at `tests/LamuFlix.Test/LamuFlix.Test.csproj` (bounded recon line 24). There is no direct Data project reference to remove. Leave Core, Infrastructure, Api, and Tests.Common references, and leave unrelated tests in that project.

**Checkpoint**: Worker is retired. Web and the delete slice are untouched by this phase.

---

## Phase 5: User Story 3 - Retire the legacy web host (Priority: P3)

**Goal**: Remove Web only after User Story 1 and the existing P1 action map are evidenced.

**Independent Test**: `src/LamuFlix.Web/` is gone. The SPA still covers list and the existing P1 filters, search, details, play, folder import, watchlist add, watchlist remove, watchlist view, and delete. Api playback gating is unchanged. Director and collection filters were not added.

### Implementation for User Story 3

- [ ] T016 [US3] Map each ticket action to the Api endpoints and SPA screens in recon sections 5–7 plus the User Story 1 tests, including import coverage before deleting `src/LamuFlix.Web/Services/MovieService.cs`. Keep the Q3 exclusion and its unverified external-plan limit. If a named replacement is missing, stop for recon.
- [ ] T017 [US3] Delete the whole `src/LamuFlix.Web/` tree, including `Library/`, `Properties/`, `appsettings` files, `Services/MovieService.cs`, and `Services/RabbitMqEnrichmentQueuePublisher.cs`.
- [ ] T018 [US3] Delete `tests/LamuFlix.Test/UnitTest1.cs`, `tests/LamuFlix.Test/Library/LegacyMovieSortTests.cs`, `tests/LamuFlix.Test/Smoke/MovieServiceEfTests.cs`, `tests/LamuFlix.Test/GenericRepositoryGetByIdTests.cs`, and `tests/LamuFlix.Test/ApiDataModel.cs`. After T015, remove `FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage` from `tests/LamuFlix.Test/EnrichmentTests.cs` (bounded recon-DEV-388 section 2) and declarations that exist only to support that remaining Web and Data usage. Inventory remaining members before the edit. Keep the assembly fixture and unrelated behavior. Do not delete the file because it has no Core usage. Remove only the Web `ProjectReference` at `tests/LamuFlix.Test/LamuFlix.Test.csproj` (bounded recon line 23). Keep `tests/LamuFlix.Test` and its unrelated tests outside the solution.

**Checkpoint**: Both legacy host trees are gone and the ruled test files are gone. Unrelated legacy tests remain.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Dead documentation references, retirement inventories, configured gates, and the final ticket-AC and SC-to-evidence mapping. No story label.

- [ ] T019 Remove only the dead-host claims bounded recon-DEV-388 section 5 records. Line numbers are snapshot locators; if the text has drifted, stop for recon. Line 10 is the sentence that the Web and Data projects, as well as the `LamuFlix.Test` suite, were upgraded from .NET Core 2.1 to .NET 10 (`net10.0`): remove the Web and Data upgrade claim and retain unrelated `LamuFlix.Test` content. Line 23 is the bullet that upgraded `LamuFlix.Worker.csproj` to an SDK-style `Microsoft.NET.Sdk.Worker` project targeting `net10.0`: remove that Worker claim. Line 26 is the bullet that added unit tests for `QueueWorker` in `LamuFlix.Test`: remove that `QueueWorker` claim and retain unrelated test-project content. Do not rewrite the rest of `README.md`.
- [ ] T020 [P] Recheck `LamuFlix.sln`, `docker/`, `docker-compose.yml`, `docker-compose.observability.yml`, and `.github/workflows/ci.yml` for no retired C# host reference, and confirm `docker/Dockerfile.web` remains the SPA build. Do not edit those files when they are already clean. If a new reference is present, stop for recon.
- [ ] T021 [P] Inventory `src/` and `tests/` for zero `Pomelo`, `UseMySql`, and `LamuFlix.Data` references and zero remaining Web or Worker project references.
- [ ] T022 Run `dotnet test` and the web job steps already present in `.github/workflows/ci.yml`. Run `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, and `dotnet format --verify-no-changes` using each script's own documented parameters. Run `./scripts/run-mutation.ps1` for the pre-PR mutation gate. Do not hardcode or lower a threshold, and do not treat `SKIPPED (scope-empty)`, `SKIP`, N/A, or `Could not run` as PASS.
- [ ] T023 Confirm the diff excludes `harness.yml`, `.specify/memory/constitution.md`, and `src/LamuFlix.Infrastructure/Persistence/Migrations/`. The only allowed edit to `docs/adr/0021-retire-legacy-web-and-worker-hosts.md` is the status line, to `Accepted`. Do not change the decision text. Do not close DEV-370 or DEV-385 in this change.
- [ ] T024 Own the final mapping from each ticket acceptance criterion and from SC-001 through SC-007 to the explicit test, inventory, or gate evidence produced by T003 through T023. A criterion or success outcome with no evidence stops delivery. This task adds no behavior.

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1) has no dependencies.
- Foundational (Phase 2) depends on Setup and blocks every story. It does not authorize story work.
- The pre-implementation prerequisite (completed plan challenge/freeze, the separate owner Gate 1, and fresh Phase B pickup recon and analyze) blocks every story. Amend planning only for demonstrated drift.
- User Story 1 (Phase 3) depends on Foundational and on that prerequisite.
- User Story 2 (Phase 4) depends on Foundational and on that prerequisite, and does not depend on User Story 1.
- User Story 3 (Phase 5) depends on that prerequisite, on User Story 1 (T012 and T016), and on T015, because T018 continues `EnrichmentTests.cs` and `LamuFlix.Test.csproj`.
- Polish depends on User Stories 2 and 3. T024 depends on T016 and on T019 through T023, and it owns the final ticket-AC and SC-to-evidence mapping.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational and the pre-implementation prerequisite. No dependency on User Story 2 or 3.
- **User Story 2 (P2)**: Starts after Foundational and the pre-implementation prerequisite. Independent of delete. Must finish T015 before T018.
- **User Story 3 (P3)**: Starts after the pre-implementation prerequisite, after User Story 1 is evidenced, and after T015.

### Within Each User Story

- Tests are written to fail before the production edit they describe.
- Port and persistence (T005) precede the handler (T006).
- Endpoint behavior tests (T007) precede the production endpoint change (T008). T008 depends on T005, T006, and T007.
- Backend behavior precedes contract generation (T009). Generated TypeScript (T010) precedes the SPA production edit (T012). SPA tests (T011) are written against that generated client.
- Worker evidence (T013) precedes Worker deletion (T014, T015).
- P1 parity map (T016) precedes Web deletion (T017, T018).

### Parallel Opportunities

- T003 and T004 touch different test projects and can run together.
- User Story 2 (T013–T015) can run beside User Story 1 (T003–T012).
- T020 and T021 can run together after the trees and test edits land.
- T018 must not run beside T015.

---

## Parallel Example: User Story 1

```text
T003 tests/LamuFlix.UnitTests/Features/Library/
T004 tests/LamuFlix.IntegrationTests/
```

User Story 2 can run at the same time as those tasks, in `src/LamuFlix.Worker/` and `tests/LamuFlix.Test/`.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete the pre-implementation prerequisite: plan challenge/freeze, the separate owner Gate 1, and fresh Phase B pickup recon and analyze. Amend planning only for demonstrated drift.
3. Complete Phase 3 (T003–T012).
4. Stop and validate delete, cascades, the file on disk, and the details confirmation without retiring either host.

### Incremental Delivery

1. Setup, foundation, and the pre-implementation prerequisite.
2. User Story 1 delivers the missing delete action.
3. User Story 2 removes Worker once enrichment evidence holds. This may land before or beside User Story 1's SPA work, and it must not wait on director or collection work.
4. User Story 3 removes Web only after the parity map.
5. Polish runs the inventories and gates. T024 owns the final ticket-AC and SC-to-evidence mapping. Ticket closure of DEV-370 and DEV-385 stays after merge.

### Parallel Team Strategy

After Phase 2 and the pre-implementation prerequisite, one implementer can take User Story 1 and another User Story 2. User Story 3 starts when User Story 1 is evidenced and T015 has finished.

---

## Notes

- [P] tasks use different files and do not wait on an incomplete task.
- Story labels map only story-phase tasks.
- A fact that delivery needs and that `recon-DEV-388` and `recon-DEV-388-2` do not contain is `needs recon`, not a new design.
- Do not schedule Gherkin, a glossary edit, a constitution edit, a dependency change, or a decision-text rewrite of the retirement ADR. The delivery PR may change only its status line to `Accepted`.
