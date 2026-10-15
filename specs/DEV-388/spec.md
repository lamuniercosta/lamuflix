# Feature Specification: DEV-388 Retire legacy Web and Worker hosts

**Feature Branch**: `feature/388-spec`

**Created**: 2026-10-15

**Status**: gate1: provisional

**Input**: Ticket DEV-388 (task, size L, parent DEV-286); settled grill brief `specs/DEV-388/brief.md` (grill closed at Q9 of 12); facts from notes `recon-DEV-388` and `recon-DEV-388-2`; Patron rulings in `specs/DEV-388/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md`. No decisions beyond the brief and those rulings. `/speckit-clarify` added no question: Q1–Q9 already settle behavior, contract, schema, test cleanup, file boundaries, architecture, verification, and review limits.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remove a movie from the library (Priority: P1)

A library owner opens a movie's details, confirms removal, and the movie leaves the library and the watchlist. The file on disk stays. Removing an id that is not in the library reports that the movie is absent.

**Why this priority**: Delete is the only MoviesController action that still has neither an Api endpoint nor an SPA screen. Patron Q1 keeps delete in scope after Q3 drops director and collection parity. Web cannot be retired until this action exists.

**Independent Test**: With Web and Worker still present, remove one persisted movie through `DELETE /api/movies/{id}` and through the details confirmation, then repeat the removal. The first removal returns 204 with no body. The repeat and an unknown id return 404. Overlapping deletes of that same id produce exactly one removal (204) and one absence (404). The on-disk file, shared actors, directors, genres, and another movie remain. A details DELETE that returns 404 invalidates the library, watchlist, and details caches, returns to the library, and shows no error and no retry.

**Acceptance Scenarios**:

1. **Given** a movie in the library, **When** the owner confirms removal on details, **Then** the movie row is gone, the owner is back on the library, and the library and watchlist no longer show that movie.
2. **Given** a movie in the library, **When** `DELETE /api/movies/{id}` is called for that id, **Then** the response is 204 with no body and no request body was required.
3. **Given** the movie was already removed or never existed, **When** delete is called for that id, **Then** the response is 404 and nothing else is removed.
4. **Given** the owner opens confirmation and cancels, **When** the dialog closes, **Then** no delete request is sent and the movie remains.
5. **Given** a delete request is in flight, **When** the owner activates the confirm control again, **Then** a second request is not sent.
6. **Given** delete fails for a reason other than 404, **When** the error is shown, **Then** details stays usable and offers retry.
7. **Given** the details DELETE returns 404 because the movie is already absent, **When** the response arrives, **Then** the library, watchlist, and details caches are invalidated, the owner is on the library, and no error banner or retry is offered.

---

### User Story 2 - Retire the legacy worker (Priority: P2)

The owner no longer has a separate Worker host. Metadata enrichment still runs through the Api replacement that already exists.

**Why this priority**: The ticket allows Worker to go before Web. Enrichment replacement evidence is already located; delete parity is not a precondition for Worker.

**Independent Test**: After the named Api enrichment replacement evidence is still present, including the Api-hosted `EnrichmentConsumer`, its registration and Api wiring, and the existing processing proofs, `src/LamuFlix.Worker/` is gone, `WorkerTests.cs` is gone, and the Worker project reference is gone from `tests/LamuFlix.Test/LamuFlix.Test.csproj`. The solution build does not include Worker. Enrichment endpoint and publisher tests named in recon section 7 still exist, and the consumer proofs named in bounded recon-DEV-388 section 3 still exist.

**Acceptance Scenarios**:

1. **Given** the Api publisher, enrichment endpoint, `RabbitMqPublisherTests`, `EnrichmentEndpointTests`, the Api-hosted `EnrichmentConsumer` and its wiring, `EnrichmentConsumerTests`, `ApiEndToEndEnrichmentTests`, and `RabbitMqServiceCollectionExtensionsTests` still cover the replacement, **When** Worker is retired, **Then** those replacements remain and the Worker tree does not.
2. **Given** Worker is retired ahead of Web, **When** the solution is built, **Then** the build does not require `LamuFlix.Worker`.

---

### User Story 3 - Retire the legacy web host (Priority: P3)

The owner uses only the React SPA. The legacy Web host, including its movie service and enrichment publisher, is gone after every required MoviesController action has an Api endpoint and an SPA screen.

**Why this priority**: This is the ticket outcome. It waits on User Story 1 and on the existing P1 coverage for list, filters, search, details, play, folder import, and watchlist.

**Independent Test**: With User Story 1 in place, map each required action to its existing Api endpoint and SPA screen, then delete `src/LamuFlix.Web/`. The SPA still browses, filters with the existing P1 set, searches, opens details, plays, imports a folder, and adds, removes, and views the watchlist. Delete remains available. Director and collection filters are not added.

**Acceptance Scenarios**:

1. **Given** delete and the existing P1 actions are evidenced, **When** Web is removed, **Then** `src/LamuFlix.Web/` is gone, including `Library/`, `Properties/`, and `appsettings` files.
2. **Given** Web is gone, **When** the owner uses the SPA, **Then** the actions listed in the ticket AC still have an endpoint and a screen, reading list-plus-filters as the existing P1 filter set.
3. **Given** Web is gone, **When** playback is used, **Then** Api `Features:LocalPlay` behavior is unchanged.

---

### Edge Cases

- A second delete of an id that was already removed reports absence (404) and does not return the legacy silent success. Overlapping deletes of the same id are proved by PostgreSQL Testcontainers: exactly one call is a removal and returns 204, and the other is an absence and returns 404. Shared principals survive, and another movie is preserved. Neither case removes another movie.
- An id the existing `GET /api/movies/{id:int}` route already rejects uses that same outcome. An id less than or equal to zero is 404. A non-integer id is 400. This feature adds no id rule, no id-specific 422, no empty validator, and no authentication. The host stays anonymous.
- Removing a movie removes that movie's watchlist membership and metadata columns with the row, and removes its rows in `movie_actors`, `movie_directors`, and `movie_genres` by the existing database cascades.
- Actors, directors, and genres shared with other movies remain. Another movie remains. No collection table is created.
- The physical media file addressed by the movie's library path remains on disk. Delete performs no file delete, move, or process launch.
- Confirmation cancel sends no request. Both a 204 removal and a 404 confirmed absence invalidate the library cache, the watchlist cache, and the details cache the details view owns, then show the library. A 404 shows no error banner and offers no retry. Any other failure leaves details on screen with the assumed error copy and retry. Invalidating only an app-level cache does not satisfy the details-cache requirement.
- Solution, Docker, compose, and CI already omit Web and Worker at intake. Delivery rechecks that fact and does not edit those files to manufacture a change.
- The external product-plan lines cited for deferring director and collection work were not read inside recon roots. The exclusion still stands as Patron Q3, and the spec does not claim those lines were verified.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose `DELETE /api/movies/{id:int}` with no request DTO and no request body, on the existing movie resource (`GET /api/movies/{id:int}` in `LibraryEndpoints.cs`).
- **FR-002**: A delete that removes the movie MUST return 204 with no response body.
- **FR-003**: A delete for an id the existing `GET /api/movies/{id:int}` route accepts, when that id matches no movie, MUST return 404 as a problem response including `traceId`, via the existing not-found mapping. A repeat delete is this case. The legacy silent `Ok()` MUST NOT be kept.
- **FR-004**: Invalid ids MUST use the same outcomes as the existing `GET /api/movies/{id:int}` route. An id less than or equal to zero MUST return 404 through that route's existing movie-id check. A non-integer id MUST return 400 through the existing route constraint. This feature MUST NOT add an id rule, an id-specific 422, an empty validator, or an authentication mechanism. The endpoint stays anonymous, as that GET route is. Delete endpoint tests MUST verify these invalid-id outcomes on DELETE itself. The sibling enrichment-route test is not DELETE proof.
- **FR-005**: Delete MUST be metadata-only. It removes that one `movies` row, including watchlist and metadata columns on the row, and relies on the existing cascades to remove that movie's `movie_actors`, `movie_directors`, and `movie_genres` rows. Shared actor, director, and genre rows MUST survive. Any other movie MUST survive. The physical media file MUST remain.
- **FR-006**: Persistence MUST report removal versus absence in one atomic operation, without a read-then-delete race. There MUST be no schema change, migration, new table, or edit to `Initial`.
- **FR-007**: The slice MUST extend the existing `IMovieRepository` port, the Library feature command and handler, Infrastructure persistence, and the Api movie endpoints. It MUST NOT add a port, project, top-level folder, repository or service layer, or a MediatR-style bus. The Library feature MUST NOT reference another feature folder; watchlist membership disappears with the movie row.
- **FR-008**: The contract MUST flow from the C# endpoint surface to generated `web/src/api/openapi.json` to generated `web/src/api/types.ts`, and the committed OpenAPI snapshot MUST be updated with that document. Generated files MUST NOT be hand-edited. The SPA MUST consume the generated types.
- **FR-009**: Movie details MUST offer one destructive confirmation using the assumed copy and placement in `specs/DEV-388/ASSUMPTIONS.md`. The control follows existing details actions and does not restyle the application.
- **FR-010**: Cancel MUST send no request. While a request is pending, a second submit MUST NOT be sent. Both a 204 removal and a 404 confirmed absence MUST invalidate the library cache, the watchlist cache, and the details cache the details view owns, and MUST return to the library. A 404 means the movie is gone: it MUST show no error banner and MUST offer no retry. Invalidating only an app-level cache does not meet the details-cache requirement. Any other failure (network, 5xx, or a problem response other than 404) MUST leave details usable, show the assumed error, and offer retry.
- **FR-011**: Existing P1 browse, filter, search, details, play, folder import, watchlist add, watchlist remove, and watchlist view behavior MUST stay. Director filters, collection filters, director facets, collection facets, and collection storage MUST NOT be added. Api `Features:LocalPlay` and process execution MUST stay unchanged. No secret or connection string may be copied out of the legacy hosts.
- **FR-012**: Before `MovieService.cs` or the Web enrichment publisher is deleted, delivery MUST confirm the Api import and enrichment replacements recon section 7 already names: Library, Playback, Import, and Watchlist handler and endpoint tests, plus `RabbitMqPublisherTests` and `EnrichmentEndpointTests`. Before Worker is retired, delivery MUST also confirm the Api-hosted replacement bounded recon-DEV-388 section 3 names: `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`, its registration in `RabbitMqServiceCollectionExtensions` when both required ports are present, Api `Program.cs` ordering that registers persistence, the movie catalog, the metadata provider, and then RabbitMQ, plus the existing proofs in `EnrichmentConsumerTests` (successful enrichment and ack, and the retry cases that recon lists), `ApiEndToEndEnrichmentTests`, and `RabbitMqServiceCollectionExtensionsTests`. A missing replacement stops for recon. This ticket MUST NOT add a consumer or expand those proofs.
- **FR-013**: Delivery MUST delete all of `src/LamuFlix.Web/` and `src/LamuFlix.Worker/`, including `Library/`, `Properties/`, and `appsettings` files. Worker MAY be deleted once FR-012's enrichment evidence holds, before Web. Web deletion waits until User Story 1 and the P1 parity map in FR-012 both hold.
- **FR-014**: Delivery MUST delete `tests/LamuFlix.Test/UnitTest1.cs`, `tests/LamuFlix.Test/Library/LegacyMovieSortTests.cs`, `tests/LamuFlix.Test/Smoke/MovieServiceEfTests.cs`, `tests/LamuFlix.Test/WorkerTests.cs`, `tests/LamuFlix.Test/GenericRepositoryGetByIdTests.cs`, and `tests/LamuFlix.Test/ApiDataModel.cs`. In `tests/LamuFlix.Test/EnrichmentTests.cs` it MUST remove only the Worker tests bounded recon-DEV-388 section 2 names (`OmdbMetadataProvider_Success_ReturnsMetadata`, `OmdbMetadataProvider_NotFound_ReturnsNull`, `OmdbMetadataProvider_ServerError_ThrowsException`, `OmdbMetadataProvider_MissingApiKey_ThrowsInvalidOperationException`, `EnrichmentJobProcessor_TransitionsToEnriched`, `EnrichmentJobProcessor_TransitionsToNotFound_RowRemainsVisible`, `EnrichmentJobProcessor_Idempotency_SkipsAlreadyEnriched`, `EnrichmentJobProcessor_RetryOnFailure_RepublishesWhenRetryCountBelowThree`, `EnrichmentJobProcessor_ExceedsMaxRetries_MarksFailedAndForwardsToDlq`), the Web test `FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage`, and declarations that exist only to support those removed tests. It MUST inventory remaining members before each partial edit and MUST preserve unrelated behavior, including the assembly fixture. Absence of a Core usage is not a reason to delete the file. It MUST remove only the Web and Worker `ProjectReference`s from `tests/LamuFlix.Test/LamuFlix.Test.csproj`. Bounded recon shows no direct `LamuFlix.Data` project reference; Core, Infrastructure, Api, and Tests.Common references stay. The project stays outside the solution, with its unrelated tests.
- **FR-015**: `README.md` edits MUST be limited to dead-host claims in the sentences bounded recon-DEV-388 section 5 records. Those line numbers are snapshot locators. If the text has drifted, delivery stops for recon. Line 10 is the sentence that the Web and Data projects, as well as the `LamuFlix.Test` suite, were upgraded from .NET Core 2.1 to .NET 10 (`net10.0`): remove the Web and Data upgrade claim and retain unrelated `LamuFlix.Test` content. Line 23 is the bullet that upgraded `LamuFlix.Worker.csproj` to an SDK-style `Microsoft.NET.Sdk.Worker` project targeting `net10.0`: remove that Worker claim. Line 26 is the bullet that added unit tests for `QueueWorker` in `LamuFlix.Test`: remove that `QueueWorker` claim and retain unrelated test-project content. The constitution MUST NOT be edited. `LamuFlix.sln`, Docker, compose, and CI MUST NOT be edited; delivery rechecks that they already omit the retired C# hosts. `docker/Dockerfile.web` stays the React SPA build. The pre-existing `harness.yml` diff MUST stay out of DEV-388 commits. No package, dependency, or threshold change is in scope.
- **FR-016**: After retirement, no `Pomelo`, `UseMySql`, or `LamuFlix.Data` reference MAY remain under `src/` or `tests/`, and no Web or Worker project reference MAY remain.
- **FR-017**: Proof MUST include handler tests for success and absence, PostgreSQL Testcontainers tests for 204 then repeat 404, an absent id, overlapping same-id deletes that prove exactly one removal (204) and one absence (404) with shared principals and another movie preserved, row and column removal, all three join cascades, survival of shared principals and another movie, and the media file remaining, plus contract and snapshot coverage of DELETE with no payload. SPA tests MUST cover confirm, cancel with no request, pending duplicate prevention, visible error and retry for failures other than 404, successful navigation, refreshed library, watchlist, and stale details, and the DELETE 404 path (cache invalidation, navigation to the library, no error banner, and no retry). The data-access driver MUST NOT be mocked for cascade behavior. No Gherkin lane is in scope.
- **FR-018**: Required gates MUST run at their configured thresholds. A scope-empty result is `SKIPPED (scope-empty)`, a configured opt-out is `SKIP`, a mutation exclusion may be N/A, and an unrunnable gate is `Could not run`. None of those is PASS. Thresholds MUST NOT be lowered. The intake count of 1082 passed tests is historical only.
- **FR-019**: DEV-370 and DEV-385 MUST NOT be closed in this change. After the delivery PR merges and the target files are gone, Rigger checks live ticket state and closes them only when that state still warrants it.
- **FR-020**: No new domain term is introduced, so `CONTEXT.md` is unchanged. The retirement record is `docs/adr/0021-retire-legacy-web-and-worker-hosts.md`. In the spec PR its status is `Proposed (becomes Accepted when the DEV-388 Gate 1 spec PR merges)`. The delivery PR may change only that status line to `Accepted`. A decision-text change needs a new Patron ruling.

### Key Entities

- **Movie**: One library row. Delete removes that row and the watchlist and metadata columns stored on it.
- **Movie association**: The three join sets for that movie (`movie_actors`, `movie_directors`, `movie_genres`). Existing cascades remove them when the movie row goes. No new relationship is added.
- **Actor, Director, Genre**: Shared principals. They survive deletion of one movie.
- **Media file**: The on-disk file addressed by the movie library path. It is not a row, and delete does not touch it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: One present movie can be removed with an empty 204, and a repeat or unknown id is reported as 404, with the on-disk file still present.
- **SC-002**: After removal, that movie is absent from the library list and the watchlist; shared actors, directors, genres, and another movie are still present; its three join sets are gone.
- **SC-003**: Cancel sends no delete. A 404 confirmed absence returns to the library with the affected caches invalidated, no error, and no retry. Any other failed delete leaves details usable with an error and retry. A successful delete returns to a library view that no longer lists the movie.
- **SC-004**: Each ticket AC action (list plus the existing P1 filters, details, play, import folder, search, delete, watchlist add, watchlist remove, watchlist view) has an Api endpoint and an SPA screen. Director and collection filters are not part of that set.
- **SC-005**: The solution builds and the configured tests pass with both legacy host trees absent, with no `Pomelo`, `UseMySql`, or `LamuFlix.Data` reference under `src/` or `tests/`, and with no Web or Worker project reference.
- **SC-006**: Docker, compose, and CI still contain no reference to the retired C# hosts, and the SPA image build remains. Solution and deployment files are unchanged by this ticket.
- **SC-007**: Diff-scoped analyzer gates, the test suites, and the pre-PR mutation gate have been run. Failed or unrunnable required gates block delivery. Skips keep their literal labels.

Delivery records which evidence proves each ticket acceptance criterion and each outcome SC-001 through SC-007. That final mapping belongs to polish, after the story evidence and the retirement inventories exist. An unmapped criterion blocks delivery.

## Assumptions

- Confirmation wording and placement are the `[assumed]` recommendation in `specs/DEV-388/ASSUMPTIONS.md`. Navigation back to the library after success is part of that assumption and of Patron Q2.
- Director and collection exclusion follows Patron Q3. Constitution governance, the single `Initial` migration constraint, and DEV-19's seven-table success criterion were verified in recon section 14. The locked product-plan sentences at `F:/Dev/lamuflix-react-pivot-plan.md:33,47,94` remain Patron-cited and were not verified inside recon roots.
- Exact new command, handler, and method names follow the existing Library feature conventions at implementation. The spec locks the port, the feature folder, the route, and the statuses, not a new type name beyond those conventions.
- No latency or scale target was ruled. None is added. Delete is one movie row plus the cascades the schema already has.
- Intake solution, Docker, and CI satisfaction, and the 1082-test baseline, are recon facts at a pinned head. Delivery rechecks them. They are not a pass of this change.
- `tests/LamuFlix.Test` staying outside the solution, with unrelated tests retained, is the ruled boundary. Leftover tests not running in CI stay noted and are not a new ticket.

## Out of Scope

- Director or collection filters, facets, storage, or a migration.
- A new dependency, project, architectural layer, glossary term, or constitution amendment.
- A full README rewrite (that remains DEV-23).
- Editing `harness.yml`, lowering a gate, or merging. Only the user merges.
- Closing DEV-370 or DEV-385 before the delivery PR merges.
- Opt-in Gherkin, unrelated UI restyling, and any LocalPlay or `Process.Start` behavior change.
- Anything else is a follow-up issue, not a finding in this round. Out-of-scope follow-up tickets are only for Critical, High, or broken behavior. An open Low inside the frozen scope is one noted-no-ticket line in `CONCLUSIONS.md`.
