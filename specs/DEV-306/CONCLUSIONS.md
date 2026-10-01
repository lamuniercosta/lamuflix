# DEV-306 Conclusions

Patron rulings for Keel grill batch 1, Q1-Q10. Ticket basis: connected note `DEV-306:15`; recon basis: `recon-DEV-306` at baseline `3b2e998687d058552a4fa79841691be24ae5e691`. Source line citations below refer to that baseline. No ticket change or constitution departure is required; no owner checkbox is opened.

## Q1 — Clean PostgreSQL reset

Approved: no Respawn; the fixture migrates its default database on startup and exposes a connection string plus an asynchronous, cancellable reset of all application tables using one model-derived, schema-qualified, safely quoted TRUNCATE with RESTART IDENTITY CASCADE, preserving migration history.

- The ticket requires startup migrations and a clean reset, without specifying Respawn (`DEV-306:15`); dependency decisions belong to Patron (`specs/PRODUCT.md:34-43`).
- Fresh-database creation remains useful for migration tests but does not replace the normal fixture reset (`tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs:16-21`).
- Replace the catalog-only ExecuteDeleteAsync reset with the common reset; deduplicate model table mappings, cover join tables, and exclude migration history (`tests/LamuFlix.IntegrationTests/MovieCatalogSeed.cs:24-31`; constitution IX, `.specify/memory/constitution.md:299-303`).

## Q2 — Migrated and empty database paths

Approved with correction: normal consumers explicitly move to CreateMigratedContext and reset before each test; MigrationTests alone uses a clearly named empty-database path and runs migrations itself; preserve the existing static factory semantics for legacy consumers.

- Startup migration and fixture reuse are ticket requirements (`DEV-306:15`); fresh data is mandatory (constitution IX, `.specify/memory/constitution.md:302-303`).
- Preserve migrate-from-empty and catalog-contract assertions (`tests/LamuFlix.IntegrationTests/MigrationTests.cs:51-77`); remove redundant per-test migrations from normal consumers, and use the fixture connection string for direct connections.
- Make the normal-path semantic change visible at call sites rather than silently changing CreateContext; any new empty-database creation path is asynchronous and cancellable, and all its databases disappear with the owning container (`tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs:10-21`; constitution async rule, `.specify/memory/constitution.md:433-435`).

## Q3 — Collections and sharing scope

Approved: define PostgresCollection and RabbitMqCollection in the integration-test assembly, each owning one corresponding ICollectionFixture; classes within a collection serialize, while the two collections may run concurrently.

- PostgreSQL members: EfMovieCatalogBrowseTests, EfMovieCatalogDetailsTests, EfMovieCatalogPagingTests, EfMovieCatalogSortingTests, EfMovieRepositoryTests, EfMovieRepositoryClaimConcurrencyTests, PersistenceCompositionTests, PersistenceRoundTripTests, MigrationTests. RabbitMQ members: EnrichmentConsumerTests, RabbitMqConnectionOwnerTests, RabbitMqHealthCheckTests, RabbitMqPublisherTests, RabbitMqTopologyTests. New fixture-contract tests join their respective collection. MovieCatalogRegistrationTests needs neither fixture (Keel batch 1, consumer-seam inventory).
- Fixtures remain reusable in Tests.Common; collection definitions live in the consuming test assembly. One container of each kind is shared by the current integration consumers (`DEV-306:15`; [xUnit shared context](https://xunit.net/docs/shared-context#collection-fixtures), [xUnit parallelism](https://xunit.net/docs/running-tests-in-parallel), consulted by Keel through Context7).
- No future mixed-resource collection is designed in this ticket; the current consumer inventory needs none. Naming is recorded in ASSUMPTIONS.md.

## Q4 — Retiring redundant wiring

Approved: remove the two integration AssemblyFixture registrations and every fixture consumer's redundant IClassFixture registration; replace MovieCatalogCollection with the PostgreSQL collection and add collection membership to all current consumers.

- This is forced by the ticket's collection-fixture requirement (`DEV-306:15`; `tests/LamuFlix.IntegrationTests/AssemblyInfo.cs:4-5`; `recon-DEV-306:36-38,56`).
- Delete AssemblyInfo.cs if empty after removing its registrations and delete the orphaned MovieCatalogCollection.cs; preserve unrelated assembly attributes if any appear during implementation (`tests/LamuFlix.IntegrationTests/MovieCatalogCollection.cs:5-6`; seat rules, dead-code cleanup).
- These test wiring and consumer edits are authorized under §2.3 file scope; they introduce no project, architectural layer, database schema, HTTP API, or production behavior change (`specs/PRODUCT.md:34-48`).

## Q5 — Legacy static fixture

Approved: leave ContainerFixture.cs and tests/LamuFlix.Test unchanged; legacy retirement is noted, no ticket.

- Legacy consumers still reference the static fixture, and the legacy project is outside the solution (`recon-DEV-306:47,85`; `tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs:16-21`).
- DEV-306 converges the reusable PostgresFixture/RabbitMqFixture and current integration consumers; it does not deliver legacy-project retirement (`DEV-306:15`).
- The recon reports no new Critical/High broken behavior requiring a follow-up; do not ask Rigger to file one solely for cleanup (seat rule, follow-up-ticket bar).

## Q6 — Image versions and risk

Approved with correction: use exactly postgres:17-alpine and rabbitmq:4-management-alpine; exercise existing migration/catalog and broker-topology contracts on those tags, record resolved images/server versions in verification evidence, and keep compatibility failures visible without automatically falling back.

- Both tags are explicit ticket decisions (`DEV-306:15`; `specs/PRODUCT.md:23-24`); no package bump or digest-only substitution is needed (`recon-DEV-306:68-78`).
- Floating minor/patch image contents are a named reproducibility/compatibility risk; server-major-only tests cannot detect that drift. Existing real-server catalog and quorum/DLQ tests provide the meaningful compatibility evidence (`tests/LamuFlix.IntegrationTests/MigrationTests.cs:64-77`; `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs:54-103`).
- Do not introduce a new constants class solely to hold two once-used image literals or add tests that merely mirror those literals. An old-tag diagnostic run may identify a regression but cannot satisfy ticket acceptance; a delivered tag change would require a ticket-change escalation (`specs/PRODUCT.md:18,38`; constitution IX).

## Q7 — Topology ownership and tests

Approved: fixture startup invokes production RabbitMqTopology with a short-lived, asynchronously disposed RabbitMqConnectionOwner; keep all exchange, queue, binding, and argument declarations in the production topology class.

- This satisfies fixture-declared topology while retaining the constitution's single topology owner (`DEV-306:15`; `.specify/memory/constitution.md:322`; `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs:85-103`).
- Preserve the existing test-only two-second retry delay and compatible MaxAttempts=3; no production options/default change is authorized (`tests/LamuFlix.Tests.Common/RabbitMqFixture.cs:13`; `src/LamuFlix.Core/Options/EnrichmentOptions.cs:13`; `tests/LamuFlix.IntegrationTests/RabbitMqTopologyTests.cs:18,195-199`).
- Topology tests remove the known topology before acting, use a fresh production topology instance, and restore it in async cleanup even after assertion failure; this prevents startup declarations masking a no-op declaration (`tests/LamuFlix.IntegrationTests/RabbitMqTopologyTests.cs:22-56,187-199`; serial broker collection, Q3).

## Q8 — Broker isolation

Approved with correction: use an asynchronous fixture reset that removes and re-declares the known production topology between broker tests, after previous test consumers/connections have stopped; a queue purge alone is not the isolation guarantee.

- Retry TTL and at-least-once dead-letter forwarding can move messages between queues, so serial execution plus one purge pass does not rule out cross-test contamination (`src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs:54-73`; constitution IX, fresh data at `.specify/memory/constitution.md:302-303`).
- Remove the exchange and queues with awaited broker operations, then re-declare through a fresh production topology instance; fixture reset references production names and duplicates no queue arguments. Topology tests use the empty-topology preparation from Q7 instead of pre-declaring their subject.
- Keep RabbitMqProbe's polling/assertion helpers in IntegrationTests; do not introduce another broker container, virtual-host options field, source-layer edit, or reset abstraction (`recon-DEV-306:39`; `specs/PRODUCT.md:18`).

## Q9 — Initialization and teardown

Approved: each collection fixture owns its container, cleans up failed initialization before rethrowing the original failure, and supports null-safe idempotent async disposal; factory-created connections belong to their callers.

- Clean teardown is explicit acceptance (`DEV-306:15`); migrations and topology initialization are part of the fixture lifetime (`tests/LamuFlix.Tests.Common/PostgresFixture.cs:17-29`; `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs:24-41`).
- Propagate the current xUnit cancellation token for initialization/reset/factory operations where supported; cleanup must still run when that token is canceled, and secondary cleanup errors must not erase the initialization failure (constitution async rule, `.specify/memory/constitution.md:433-435`).
- Leave Testcontainers resource reaping enabled as a crash backstop. Verify normal teardown against the exact container IDs owned by this run, avoiding assumptions about unrelated concurrent test runs (`DEV-306:15`; collection-fixture ownership in Q3).

## Q10 — Acceptance evidence

Approved with correction: add focused real-infrastructure fixture behavior tests for startup migration/topology, clean reset including relationships and retry state, and usability after reset; preserve every existing integration scenario and report full-suite results, container sharing, and teardown.

- The new behavior is fixture startup and reset; test those observable seams with the repo's xUnit v3/Shouldly conventions, keeping topology subject tests unmasked per Q7 (`DEV-306:15`; constitution IX, `.specify/memory/constitution.md:282-305`).
- Verify one container per collection per run, including evidence of sharing across classes and exact-ID cleanup; baseline is 105 passing integration tests in 1m17s (`recon-DEV-306:61-63`). Report timing as evidence rather than inventing a performance or test-count threshold.
- Preserve all current test cases; test-count changes need an explanation, not an arbitrary >=105+N gate. Implementation runs the required changed-C# gates; Patron does not rerun those gates in this grill (`specs/PRODUCT.md:12`; seat lane rule).
