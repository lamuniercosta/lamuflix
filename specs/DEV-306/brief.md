# DEV-306 — Phase A Brief

Phase A grill outcome for DEV-306 (parent DEV-283, size M): converge the existing Tests.Common PostgreSQL and RabbitMQ fixtures onto the ticket's contract.
Decided by Patron in `specs/DEV-306/CONCLUSIONS.md` (Q1–Q10), with naming in `specs/DEV-306/ASSUMPTIONS.md`. Ruling commit: `da99451`. This brief restates those rulings for planning. It does not reopen or override them. If this brief and CONCLUSIONS.md ever disagree, CONCLUSIONS.md wins.
Facts come from `recon-DEV-306` (baseline `3b2e998`) and from Keel's consumer-seam read at that baseline. Ticket text is the connected note `DEV-306:15`, which is authoritative.
Grill questions: **10 asked (cap 12); 10/10 ruled**. Owner checkboxes: **0**. Ticket change: none. Constitution departure: none.

## Closing bar

The ticket's own requirements and acceptance criteria (`DEV-306:15`):

- **AC1 — PostgreSQL fixture.** `PostgresFixture` starts `postgres:17-alpine`. It runs EF migrations on its default database during initialization. It exposes that database's connection string and a clean reset (Q1, Q2).
- **AC2 — RabbitMQ fixture.** `RabbitMqFixture` starts `rabbitmq:4-management-alpine`. It declares topology during initialization by running the production `RabbitMqTopology`. It keeps providing connections through the existing `CreateConnectionAsync` and `Options` (Q7).
- **AC3 — Collection fixtures.** Sharing uses xUnit v3 collection fixtures only, through `PostgresCollection` and `RabbitMqCollection`. No `AssemblyFixture` or `IClassFixture<PostgresFixture|RabbitMqFixture>` remains in `tests/LamuFlix.IntegrationTests` (Q3, Q4).
- **AC4 — Efficient sharing.** One PostgreSQL container and one RabbitMQ container start per integration run. Each one serves every class in its collection (Q3, Q10).
- **AC5 — Clean teardown.** After the run, every fixture container started by that run is gone, verified by exact container ID (Q9, Q10).

Lines derived from the rulings (they do not change delivery):

- **AC6 — No lost scenarios.** Every existing integration test case still exists and passes on the new images. Any change in test count is explained in the task note; there is no numeric threshold. New fixture-behavior tests pass (Q10).
- **AC7 — Diagnostics.** Verification evidence records the resolved image references and server versions for both containers. It also records full-suite timing against the 105-test / 1 m 17 s baseline (recon:62). This is evidence only, not a threshold (Q6, Q10).
- **AC8 — Boundaries.** `git diff 3b2e998...HEAD` shows zero changes under `src/`, and no change to `Directory.Packages.props` or any `.csproj`. `tests/LamuFlix.Tests.Common/ContainerFixture.cs` and everything under `tests/LamuFlix.Test/` are unchanged. The public static members of `LamuFlixDbContextFactory` keep their signatures and semantics (Q2, Q5, Q7).
- **AC9 — Gates.** These exit 0 on the changed `.cs` files:
  - Roslyn analyzers
  - cyclomatic complexity (15 at implement, 6 at refactor)
  - InspectCode
  - `dotnet format --verify-no-changes`

## Frozen scope

**In:**

- `tests/LamuFlix.Tests.Common/PostgresFixture.cs`: rewrite (image, startup migration, connection string, reset, migrated and empty context paths, failure-safe lifetime).
- `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs`: rewrite (image, startup topology through production code, topology reset and delete, failure-safe lifetime).
- `tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs`: additive only, if the async empty-database path needs a helper. Existing public members are unchanged.
- `tests/LamuFlix.IntegrationTests/PostgresCollection.cs` and `RabbitMqCollection.cs`: new collection definitions.
- `tests/LamuFlix.IntegrationTests/AssemblyInfo.cs`: delete. Its only content is the two `AssemblyFixture` lines (4–5).
- `tests/LamuFlix.IntegrationTests/MovieCatalogCollection.cs`: delete. Replaced by `PostgresCollection`.
- `tests/LamuFlix.IntegrationTests/MovieCatalogSeed.cs`: remove `ResetAsync` (lines 24–31). Its callers use the fixture reset.
- The nine PostgreSQL consumers and five RabbitMQ consumers listed under Q3, plus `RabbitMqProbe.cs` only if a topology-deletion helper fits better there (Q7).
- New fixture-behavior test files: `tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs` and `RabbitMqFixtureTests.cs`.

**Out:**

- anything under `src/`
- packages and project files (no Respawn: Q1)
- `ContainerFixture.cs` and `tests/LamuFlix.Test` (Q5; retirement is noted, no ticket)
- a constants class for image tags (Q6)
- a virtual-host option, a reset interface or abstraction, a third collection or mixed-resource collection, or another broker container (Q3, Q8)
- `.github/workflows/`
- `MovieCatalogRegistrationTests` (uses no fixture)

An edit to a file outside **In**, a new package, or any `src/` change stops for a cited Patron ruling (`specs/PRODUCT.md:34-48`; CONCLUSIONS.md introduction, Q4, Q5). It is never a silent edit, and the implementer does not expand this scope. Only a proposed change to what the ticket delivers, or a necessary constitution departure, becomes `blocked: structural — <question>` with an owner checkbox.

## Round cap

- Review: 2 rounds maximum (standing §2.2). Remediation: at most 2 fix commits per round. This preserves the existing cap; it is not newly ruled here.

## Grill answers

Each answer is a summary. The full ruling and its basis are in CONCLUSIONS.md under the same number.

1. **Q1 — PostgreSQL reset.** No Respawn. The fixture migrates its default database at startup and exposes an async, cancellable reset.
   - The reset is one TRUNCATE … RESTART IDENTITY CASCADE built from the EF model.
   - Table names are schema-qualified and safely quoted, with duplicates removed. Join tables are included and `__EFMigrationsHistory` is excluded.
   - It replaces `MovieCatalogSeed.ResetAsync`.
2. **Q2 — Migrated and empty paths.** Normal consumers explicitly move to `CreateMigratedContext` (ASSUMPTIONS.md) and reset before each test. Their per-test `MigrateAsync` calls are removed.
   - Direct connections use the fixture's connection string.
   - Only `MigrationTests` uses a clearly named async, cancellable empty-database path, and it still migrates itself. Its assertions (`MigrationTests.cs:51-77`) are unchanged.
   - The static factory keeps its semantics.
3. **Q3 — Collections.** `PostgresCollection` (one `ICollectionFixture<PostgresFixture>`) and `RabbitMqCollection` (one `ICollectionFixture<RabbitMqFixture>`) live in IntegrationTests. Tests inside a collection run one at a time, and the two collections may run concurrently.
   - PostgreSQL members: EfMovieCatalogBrowseTests, EfMovieCatalogDetailsTests, EfMovieCatalogPagingTests, EfMovieCatalogSortingTests, EfMovieRepositoryTests, EfMovieRepositoryClaimConcurrencyTests, PersistenceCompositionTests, PersistenceRoundTripTests, MigrationTests, plus the new PostgresFixtureTests.
   - RabbitMQ members: EnrichmentConsumerTests, RabbitMqConnectionOwnerTests, RabbitMqHealthCheckTests, RabbitMqPublisherTests, RabbitMqTopologyTests, plus the new RabbitMqFixtureTests.
   - No future mixed collection is designed.
4. **Q4 — Redundant wiring.** Remove both `AssemblyFixture` registrations and every consumer's `IClassFixture<…>`. Delete `AssemblyInfo.cs` if it is empty afterwards; keep any unrelated attribute that turns up. Delete `MovieCatalogCollection.cs`. These edits are required by the ticket.
5. **Q5 — Legacy.** `ContainerFixture.cs` and `tests/LamuFlix.Test` stay unchanged. Retirement is noted here and gets no follow-up ticket.
6. **Q6 — Images.** Use exactly `postgres:17-alpine` and `rabbitmq:4-management-alpine`, written as literals in each fixture.
   - No constants class, and no tests that only mirror the tag or the server's major version.
   - The existing catalog-contract and quorum/DLX tests are the compatibility evidence. Resolved images and server versions go into the verification evidence.
   - No automatic fallback to the old tags. An old-tag diagnostic run may help locate a regression but never satisfies acceptance. Changing a delivered tag needs a ticket-change escalation.
7. **Q7 — Topology.** At startup the fixture runs the production `RabbitMqTopology.EnsureDeclaredAsync` through a short-lived `RabbitMqConnectionOwner` that is disposed asynchronously. No exchange, queue, binding, or argument is duplicated in test code.
   - The test-only 2 s retry delay and MaxAttempts = 3 are kept.
   - `RabbitMqTopologyTests` deletes the known topology before acting and uses a fresh production topology instance. It restores the topology in async cleanup, even when an assertion fails.
8. **Q8 — Broker isolation.** Isolation does not rely on a queue purge. Before each broker test, the fixture runs an async reset: it deletes the exchange and the three queues (awaited), then re-declares them through a fresh production topology instance.
   - The reset runs after the previous test's consumers and connections have stopped.
   - `RabbitMqProbe`'s polling helpers stay in IntegrationTests.
9. **Q9 — Lifetime.** Each fixture owns its container.
   - If initialization fails, the fixture cleans up and rethrows the original exception.
   - Disposal is async, idempotent, and null-safe. Connections a caller creates belong to that caller.
   - Initialization, reset, and factory work take `TestContext.Current.CancellationToken` where the API supports it.
   - Cleanup still runs after that token is cancelled. A secondary cleanup error never replaces the initialization failure.
   - Ryuk stays enabled. Teardown is verified against the exact container IDs from this run.
10. **Q10 — Evidence.** Add focused real-infrastructure fixture tests covering:
    - startup migration and topology
    - reset, including relationships, identities, and retry-queue state
    - usability after a reset

    Also report full-suite results, container sharing, exact-ID teardown, and timing.

## Plan decisions

### Approach

1. **PostgresFixture** (Tests.Common).
   - `InitializeAsync`:
     1. Build `new PostgreSqlBuilder("postgres:17-alpine")` and start it.
     2. Open a context on the default database and `await Database.MigrateAsync(ct)`.
     3. Cache the model-derived TRUNCATE statement.
   - Wrap steps 1–3 in try/catch. On failure:
     1. Dispose the container through its actual async disposal API. That disposal is not tied to the test cancellation token, so it still runs after cancellation. Any secondary error from disposal is caught.
     2. Set the field to null.
     3. Rethrow the original exception, preserving it as the primary error.

     Builder owns the source-level shape. No disposal overload is assumed.
   - Members:
     - `ConnectionString`: the migrated default database. It throws `InvalidOperationException` before initialization, like the existing `Container` accessor.
     - `CreateMigratedContext()`: wraps `LamuFlixDbContextFactory.OpenContext(ConnectionString)`.
     - `ResetAsync(CancellationToken)`: runs the cached TRUNCATE.
     - The empty-database path (Q2): an async, cancellable member such as `CreateEmptyDatabaseContextAsync(CancellationToken)`. It issues `CREATE DATABASE` for a fresh unique name, never using `TEMPLATE` from the migrated database, and opens it. Exact spelling follows existing conventions (ASSUMPTIONS.md).
   - The current synchronous `PostgresFixture.CreateContext()` has no consumers left after the moves below, so it is removed. The static `LamuFlixDbContextFactory` members are kept unchanged (AC8).
   - The TRUNCATE target list:
     - comes from `Model.GetEntityTypes()`, including shared-type join entities for many-to-many;
     - keeps only entities with a table name, schema-qualified with a default of `public`;
     - removes duplicate (schema, table) pairs and excludes `__EFMigrationsHistory`;
     - quotes identifiers as Npgsql or EF does, never by hand-splicing.
2. **RabbitMqFixture** (Tests.Common).
   - `InitializeAsync`:
     1. Build `new RabbitMqBuilder("rabbitmq:4-management-alpine")` and start it.
     2. Build the connection factory, unchanged.
     3. Declare topology: `await using var owner = new RabbitMqConnectionOwner(Options.Create(Options))`, then `await new RabbitMqTopology(owner, Options.Create(Options), Options.Create(new EnrichmentOptions { MaxAttempts = 3 })).EnsureDeclaredAsync(ct)`.
   - Failure handling is the same as for PostgresFixture.
   - `RetryDelay` (2 s), `OptionsFor`, and `Options` keep their current signatures and semantics.
   - `CreateConnectionAsync()` stays as an existing member. Cancellation propagation (Q9) is met in one of two ways, and the plan picks one:
     - an additive `CreateConnectionAsync(CancellationToken)` overload that passes the token to `ConnectionFactory.CreateConnectionAsync`, with the existing no-token member preserved;
     - the token threaded explicitly through the existing connection-factory route.

     The brief does not promise no-token semantics for any new async path.
   - Add `ResetTopologyAsync(CancellationToken)`. It deletes the exchange and the three queues by their `RabbitMqTopology` constants, then re-declares them through a fresh owner and topology instance as at startup.
   - Add `DeleteTopologyAsync(CancellationToken)`, used for the topology tests' empty-start preparation.
   - Both methods are awaited, use their own short-lived connection, and dispose it.
   - MaxAttempts = 3 matches every current consumer (`EnrichmentConsumerTests.cs:31`, `RabbitMqPublisherTests.cs:165`, `RabbitMqTopologyTests.cs:18`).
3. **Collections and wiring** (IntegrationTests).
   - Add `PostgresCollection.cs` and `RabbitMqCollection.cs`.
   - Delete `AssemblyInfo.cs` and `MovieCatalogCollection.cs`.
   - On every consumer, replace `[Collection("MovieCatalog")]` or `IClassFixture<…>` with `[Collection(<new name>)]`. Primary-constructor fixture injection is unchanged.
4. **PostgreSQL consumers.** Each class implements `IAsyncLifetime`. `InitializeAsync` calls `fixture.ResetAsync(TestContext.Current.CancellationToken)` and `DisposeAsync` is a no-op. `MigrationTests` is excluded because it uses the empty-database path.
   - Replace `fixture.CreateContext()` with `fixture.CreateMigratedContext()` in EfMovieCatalog{Browse,Details,Paging,Sorting}Tests, EfMovieRepositoryTests (211), EfMovieRepositoryClaimConcurrencyTests (24), PersistenceCompositionTests (92), and PersistenceRoundTripTests (164).
   - Remove the now-redundant `Database.MigrateAsync` lines:
     - EfMovieRepositoryTests: 212
     - EfMovieRepositoryClaimConcurrencyTests: 25
     - PersistenceCompositionTests: 93
     - PersistenceRoundTripTests: 165
     - EfMovieCatalogPagingTests: 70, 77
   - Replace every `MovieCatalogSeed.ResetAsync(context, ct)` call with nothing, because the class-level reset covers it.
   - `EfMovieCatalogPagingTests.cs:66` switches from `fixture.Container.GetConnectionString()` to `fixture.ConnectionString`.
   - Connection-string reads off a context (`GetConnectionString()` at Details:32, Paging:35, ClaimConcurrency:26, Repository:218, Composition:94, RoundTrip:171) now return the migrated database's string. They need no edit.
   - In `MigrationTests` (53, 67), `fixture.CreateContext()` becomes `await fixture.CreateEmptyDatabaseContextAsync(ct)`. Its own `MigrateAsync` (55, 68) and all its assertions stay.
5. **RabbitMQ consumers.**
   - EnrichmentConsumerTests, RabbitMqPublisherTests, RabbitMqConnectionOwnerTests, and RabbitMqHealthCheckTests implement `IAsyncLifetime` and call `fixture.ResetTopologyAsync(ct)` in `InitializeAsync`.
   - Each test already disposes its host or owner with `await using`. Because tests in a collection run one at a time, the previous test's consumers and connections have stopped before the next test's `InitializeAsync` runs (Q8).
   - Existing `EnsureDeclaredAsync` and `probe.DrainAsync` calls inside tests stay. They are harmless and removing them is not required.
   - `RabbitMqTopologyTests`:
     - `InitializeAsync` calls `fixture.DeleteTopologyAsync(ct)`.
     - `DisposeAsync` calls `fixture.ResetTopologyAsync(CancellationToken.None)`, so cleanup still runs after an assertion failure or a cancelled token (Q7, Q9).
     - Test bodies are unchanged. `EnsureDeclaredAsync_DeclaresADurableDirectExchange` and `EnsureDeclaredAsync_DeclaresTheThreeQueues` now fail if production declares nothing.
6. **Fixture-behavior tests** (Q10).
   - `PostgresFixtureTests` (PostgresCollection):
     - Applied migrations equal the known migrations immediately after startup, without a test-side `MigrateAsync`.
     - After seeding a movie with an actor, director, and genre (join rows included), `ResetAsync` leaves every application table, join tables included, with zero rows. `__EFMigrationsHistory` is intact.
     - Identity restarts: the first insert after a reset gets the same key as the first insert after the previous reset.
     - The database stays usable for insert and query after a reset.
   - `RabbitMqFixtureTests` (RabbitMqCollection):
     - The exchange and three queues exist immediately after startup, checked by passive declare.
     - A message published to `retry` before `ResetTopologyAsync` is not present in `retry`, `requested`, or `dead-letter` afterwards.
     - Publish and get work after a reset.
   - Conventions: xUnit v3 `[Fact]`, Shouldly, `TestContext.Current.CancellationToken`, Method_Scenario_Expectation names (constitution IX).

### Test strategy

- The regression net is the 105 baseline integration tests (recon:62), with test names unchanged, plus the new fixture-behavior tests. Any change in test count is explained in the task note (AC6).
- Collect this verification evidence in the task note:
  1. **Sharing (AC4).** Each fixture writes its owned container ID through fixture or test diagnostic output.
     - The evidence shows the same PostgreSQL ID observed by at least two classes in `PostgresCollection`, and the same RabbitMQ ID observed by at least two classes in `RabbitMqCollection`.
     - Container starts are correlated to those two owned IDs only. Other same-image starts in the window belong to other runs and are not evidence for this one.
     - No extra fixture or container is created just to test sharing.
  2. **Teardown (AC5).** After the run, `docker inspect <id>` must report no such container for each of the two owned IDs. Check by ID, not by label or image sweep, so concurrent unrelated runs cannot interfere (Q9).
  3. **Diagnostics (AC7).** Record the resolved image reference and digest (`docker image inspect postgres:17-alpine rabbitmq:4-management-alpine --format …`). Record the server versions (`SHOW server_version` and the broker's `version` server property), captured by the fixture-behavior tests through test output. Record the full-suite time against 1 m 17 s.
  4. **Boundaries (AC3, AC8).** Record `git diff --stat 3b2e998...HEAD` and `rg -n "AssemblyFixture|IClassFixture<(PostgresFixture|RabbitMqFixture)>" tests/LamuFlix.IntegrationTests`, which must return nothing.
- If either new image fails an existing catalog or topology test, that is a real compatibility finding. Stop and report it to Keel (R1). Never edit tests to make it pass or fall back to the old tags.

### Gate expectations

- AC9, plus the refactor gate at threshold 6.
- Mutation testing: N/A. The change is test infrastructure only, with no `src/` code. Record that line.
- Property tests: opt-out recorded in the task note per `harness.yml:31-35`, with no harness or gate edit. There is no pure logic beyond TRUNCATE list derivation, and the real-server reset test covers that.

### Task ordering

1. Pickup drift check against `main`.
2. Add `PostgresCollection` and `RabbitMqCollection`. Move all 14 consumers onto them. Delete `AssemblyInfo.cs` and `MovieCatalogCollection.cs`. The suite must be green on the old images before continuing; this isolates the sharing change.
3. Rewrite PostgresFixture: image, startup migration, `ConnectionString`, reset, migrated and empty paths, lifetime. Then convert the 9 PostgreSQL consumers, remove `MovieCatalogSeed.ResetAsync`, and add PostgresFixtureTests. The PostgreSQL collection must be green.
4. Rewrite RabbitMqFixture: image, production topology at startup, reset and delete, lifetime. Then convert the 5 RabbitMQ consumers, including the topology-test delete and restore, and add RabbitMqFixtureTests. The RabbitMQ collection must be green.
5. Full IntegrationTests run with the evidence capture above.
6. Gates (AC9) and the refactor gate.

### Drafting decisions (answers to Quill `needs decision:`)

- **D1 — `DisableParallelization` on the new collections: omit it (Outcome A).** `PostgresCollection` and `RabbitMqCollection` are plain `[CollectionDefinition("<name>")]` with no `DisableParallelization`.
  - Basis: Q3 rules both clauses explicitly: classes within a collection serialize, *and* the two collections may run concurrently (`CONCLUSIONS.md` Q3). xUnit's default already serializes within a collection; `DisableParallelization = true` would additionally run the collection apart from every other collection, which contradicts Q3's second clause. CONCLUSIONS wins over the in-repo precedent.
  - `MovieCatalogCollection.cs:5` is not a precedent to inherit: the file is deleted by Q4, it pre-dates the two-collection split, and no ruling adopted its attribute. Why it was originally set is not recorded in any ruling, so the stop condition below covers the risk instead.
  - AC7 timing in T038 is recorded as a concurrent run against the 1 m 17 s baseline.
  - Stop condition (feeds R1/R6, not a licence to add the attribute): if task-order step 2's green-on-old-images run, or any later run, shows cross-collection interference (for example shared static telemetry or listener state between a PostgreSQL and a RabbitMQ class), stop and report to Keel with the failing tests. Adding `DisableParallelization` or a third collection needs a new ruling; it is never a silent fix.

## Risks and stop conditions

- **R1 — Image compatibility.** postgres 16.4 → 17-alpine can change catalog output (`MigrationTests.cs:64-77`). rabbitmq 4.0.0 → 4-management-alpine (a floating minor) can change how quorum, at-least-once, or reject-publish arguments are enforced (`RabbitMqTopology.cs:54-80`). Both tags are ticket text.
  - A failure stays visible. It is reported to Keel with the resolved versions, and nothing falls back to the old tags.
  - An actual fixture or consumer integration defect may be fixed within the ruled scope, provided it preserves existing contracts and the exact ticket tags.
  - A test may be corrected only when it is demonstrated to be incorrect (TEST-WRONG ruling). Assertions are never weakened to make a test pass.
  - A genuinely required production or out-of-scope change goes to Patron first, under the Frozen scope rule.
  - Only a required change to a delivered tag or other ticket text, or a necessary constitution departure, escalates as `blocked: structural`.
- **R2 — Floating-tag reproducibility.** Minor and patch contents of both tags can change between runs. This is mitigated only by the recorded resolved digests (AC7) and the existing contract tests (Q6). It is accepted, not solved.
- **R3 — TRUNCATE locking.** TRUNCATE takes ACCESS EXCLUSIVE locks. A context left open from the previous test inside an uncommitted transaction would block the reset. Consumers dispose their contexts with `await using`, which must stay true. A hang here is a consumer bug to fix, not a reason to switch reset strategy.
- **R4 — Topology-reset ordering.** Deleting queues while a previous test's consumer is still attached would break that consumer's channel and could let messages leak into the next test. Every broker test must finish disposing its host before it returns. A test that leaves a background consumer running is a defect inside the frozen scope.
- **R5 — Migration-test empty database.** The empty-database path must create from the default template (`template1`), never from the migrated database. Otherwise `MigrationTests` would silently stop proving migrate-from-empty.
- **R6 — Any file outside Frozen scope, any package, or any `src/` edit.** Stop for a cited Patron ruling (§2.3 #1 or #6; `specs/PRODUCT.md:34-48`). Only a proposed ticket-delivery change or a necessary constitution departure becomes `blocked: structural` with an owner checkbox.
- **R7 — Docker absent.** Tests fail; they never skip.

## Gate 1 status

Closed. It stays closed until the normal Phase A approval of spec, plan, and tasks, which Quill drafts from this brief. This grill claims no provisional status or approval. Having zero owner checkboxes does not open Gate 1.
