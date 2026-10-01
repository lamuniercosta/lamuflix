# Feature Specification: Reusable Testcontainers Fixtures

**Feature Branch**: `feature/306-spec`

**Created**: 2026-10-01

**Status**: gate1: provisional - Gate 1 closed pending owner merge of the spec PR (no Phase B authorization; see `brief.md` §Gate 1 status)

**Input**: DEV-306 (parent DEV-283, size M, UI false) — "Create centralized Testcontainers fixtures in `tests/LamuFlix.Tests.Common/`. PostgreSQL fixture starts `postgres:17-alpine`, runs migrations on startup, and provides a connection string and clean reset mechanism. RabbitMQ fixture starts `rabbitmq:4-management-alpine`, declares topology, and provides a connection factory. Use xUnit v3 collection fixtures to share containers across integration test classes." Plus Phase A grill outcome: `brief.md`, `CONCLUSIONS.md` (Q1-Q10), `ASSUMPTIONS.md`, note `recon-DEV-306`, `specs/PRODUCT.md`, `.specify/memory/constitution.md`

**Short name**: `testcontainers-fixtures`

## User Scenarios & Testing

### User Story 1 - One container per run, shared through a named collection (Priority: P1)

As a developer writing an integration test, I need the PostgreSQL and RabbitMQ containers started **once** for the whole test run and handed to every test that needs them through a named collection, so the suite does not pay for container startup once per test class and every class provably sees the same database and the same broker.

**Why this priority**: Sharing is the mechanism every other story depends on — a reset, a topology declaration and a teardown proof are all properties of a *shared* resource. It is also the one story that can be delivered and proved on today's images, before either fixture is rewritten, which is what lets the rest of the ticket be built in two independent halves.

**Independent Test**: Run the integration suite with the two new collection definitions in place and the old assembly-level wiring removed. The suite is green on the **unchanged** image tags, one PostgreSQL container and one RabbitMQ container are observed for the whole run, and at least two classes in each collection report the same container identity. No fixture behaviour is exercised yet — that is US2 to US5.

**Acceptance Scenarios**:

1. **Given** the integration test assembly, **When** it runs, **Then** every test class that needs a database is a member of a single named PostgreSQL collection and every class that needs a broker is a member of a single named RabbitMQ collection, and no class carries an assembly-level or class-level fixture registration for either fixture.
2. **Given** two classes in the same collection, **When** each acquires its fixture, **Then** both receive the same fixture instance and that instance's container.
3. **Given** the two collections, **When** the suite runs, **Then** tests inside one collection execute one at a time, and the two collections are free to run at the same time.
4. **Given** a class that needs neither resource, **When** the suite runs, **Then** it belongs to no collection, is unaffected by either fixture, and is not edited.
5. **Given** the assembly-level wiring that previously started both containers for the whole assembly, **When** the collections are in place, **Then** it is gone, together with the orphaned collection definition it replaced and every redundant per-class registration.

---

### User Story 2 - A database fixture that is ready to use (Priority: P1)

As a developer, I need the PostgreSQL fixture to hand me a database that is already migrated to the current schema, so my test can insert and query without migrating anything itself and without a second `CREATE DATABASE` round trip.

**Why this priority**: "Runs migrations on startup" and "provides a connection string" are the ticket's own words for the database half. Every other database story is meaningless until the fixture's default database is in a known, migrated state.

**Independent Test**: Ask the fixture for a context and for its connection string, then read the applied migrations without calling a migration API from the test. The known migrations are already applied, and the string opens a working connection.

**Acceptance Scenarios**:

1. **Given** a fresh fixture, **When** it finishes starting, **Then** the database reports every known migration as applied, with no pending model changes, and the test that checks this does not migrate anything itself.
2. **Given** a started fixture, **When** a test asks it for a context, **Then** the returned context is connected to the migrated default database and needs no further setup.
3. **Given** a started fixture, **When** a test asks it for its connection string, **Then** it receives the migrated default database's string, which it can use for a direct database connection.
4. **Given** a test that needs to prove migration from genuinely empty, **When** it asks the fixture for an empty database context, **Then** it receives a context on a brand-new empty database, created independently of the migrated one, and that test still runs the migration itself.
5. **Given** a fixture that has not been started, **When** a test asks for its container or its connection string, **Then** the access fails with a clear error naming the fixture rather than returning null or an empty string.
6. **Given** the pre-existing helper that mints a context, **When** this ticket lands, **Then** its existing members keep their signatures and their meaning — including the members only the retired legacy project calls.

---

### User Story 3 - A reset that leaves the database genuinely empty (Priority: P1)

As a developer, I need one call that returns the shared database to a known-empty state — every table, join tables included, key counters back at their start, migration history intact, and the database still usable — so I never have to reason about what a previous test left behind.

**Why this priority**: "A clean reset mechanism" is the ticket's own requirement, and fresh data per test is non-negotiable in this repository. This is the story that replaces the hand-rolled, catalog-only delete that currently lives in the test assembly.

**Independent Test**: Seed a movie with an actor, a director and a genre, call the reset, then count rows in every application table including the three join tables, read a key after a fresh insert, run a migration, and read a row back. Zero rows everywhere, the same key both times, the migration still present, and the read succeeds.

**Acceptance Scenarios**:

1. **Given** a started fixture, **When** the reset runs, **Then** every application table and every join table holds zero rows, and no table is missed.
2. **Given** a reset that has just run, **When** the migration history is read, **Then** every migration is still recorded as applied — the reset never touches migration history.
3. **Given** two consecutive resets, **When** the first insert after each of them is made, **Then** both inserts receive the same key, because the reset restarts identity.
4. **Given** a reset that has just run, **When** a test inserts and reads back a row, **Then** both operations succeed — the reset leaves a working database, not an empty shell.
5. **Given** the join tables that a many-to-many mapping owns, **When** the reset derives what to clear, **Then** those tables are included exactly once, and a table that two model types map is not cleared twice.
6. **Given** a table name or schema that needs quoting, **When** the reset builds its statement, **Then** the identifier is quoted by the database layer rather than assembled by hand, so a name with unusual characters is handled safely.
7. **Given** the per-test hand-rolled delete that this story replaces, **When** this ticket lands, **Then** it is gone, and the reset is the only way a database is emptied.

---

### User Story 4 - A broker fixture whose topology already exists (Priority: P1)

As a developer working on enrichment, I need the RabbitMQ fixture to have already declared the exchange, the three queues and their bindings and arguments when it hands me a connection, so my test starts from a real, correctly configured broker and never has to know how the topology is defined.

**Why this priority**: "Declares topology" is the ticket's own requirement for the broker half. Declaring it through the production code rather than a test copy is what keeps the two from drifting apart.

**Independent Test**: Ask the fixture for a connection, check the exchange and the three queues with a passive declare, and compare the queue arguments against the production declaration. Everything exists, and the arguments match what production declares rather than a hand-written copy.

**Acceptance Scenarios**:

1. **Given** a fresh broker fixture, **When** it finishes starting, **Then** the exchange and all three queues already exist, and a passive declare of each succeeds without the test declaring anything.
2. **Given** the queue arguments as the broker reports them, **When** they are compared with the production declaration, **Then** every queue type, dead-letter setting, delivery limit and overflow setting matches the production values, with no value restated in test code.
3. **Given** a started broker fixture, **When** a test asks it for a connection, **Then** it receives a working connection, and the options describing the broker keep their current shape and meaning.
4. **Given** the test-only fast retry delay this repository uses, **When** the fixture starts the broker, **Then** that same fast delay and the same attempt count are used, and no production default changes.
5. **Given** a test that inspects the broker's topology directly, **When** it uses the fixture's existing polling and draining helpers, **Then** those helpers continue to live with the tests that use them rather than moving into the shared fixture library.

---

### User Story 5 - Broker isolation between tests (Priority: P1)

As a developer, I need the broker's known topology removed and re-declared between tests so no message, count or queue state survives from one test into the next, so a test that passes means the production behaviour passed and not that the previous test left the right residue.

**Why this priority**: Retry timers and dead-letter forwarding can move a message between queues on their own, so serial execution plus a single purge is not an isolation guarantee. Without this story, a green suite does not mean a correct broker.

**Independent Test**: Publish a message into the retry queue, run the reset, then look in the retry, requested and dead-letter queues. The message is in none of them, and a subsequent publish and get still work.

**Acceptance Scenarios**:

1. **Given** a message sitting in the retry queue, **When** the reset runs, **Then** that message is present in none of the three queues afterwards.
2. **Given** a started broker fixture, **When** the reset runs, **Then** the exchange and the three queues are removed and then re-declared, so the broker ends in exactly the state a fresh start would produce.
3. **Given** a test that needs to start from nothing declared, **When** it prepares its broker, **Then** the known topology is removed and **not** re-declared, and it restores the topology afterwards even when its assertions fail.
4. **Given** the previous test's consumer or connection, **When** the next test's reset runs, **Then** the previous one has already stopped, because tests in the collection run one at a time; a test that leaves a consumer running is a defect rather than a reason to change the reset.
5. **Given** a reset or a cleanup that must run after a test's cancellation token has been cancelled, **When** it runs, **Then** it uses a token that is still live, so cleanup is not skipped.
6. **Given** a reset that has just run, **When** a test publishes and receives a message, **Then** both operations succeed — the reset leaves a working broker, not an empty one.

---

### User Story 6 - A fixture that starts safely, fails honestly and cleans up after itself (Priority: P2)

As a developer, I need a fixture whose startup failure tells me the real cause rather than a cleanup artefact, and whose container is gone when the run ends however the run ended, so a red run is diagnosable and a finished run leaves nothing running on my machine.

**Why this priority**: "Containers tear down cleanly after runs" is a ticket acceptance criterion in its own right, and the failure-path behaviour is what makes a red run useful instead of misleading. It is separable from the sharing and reset stories, which is why it is not first.

**Independent Test**: Read the two container identities the run started. After the run, ask Docker about each one **by identity** and confirm neither exists. Separately, on a fixture that was never initialised, dispose it and then dispose it again, and confirm both complete without failing — the disposal contract is proved by running it, not by reading it. The startup-failure path (scenarios 2-3) is verified **by inspection** against FR-020 rather than by forcing a failure: provoking one would need an image or builder seam on the fixture, which is a change to a published shape for a test-only hazard and is not ruled in.

**Acceptance Scenarios**:

1. **Given** a run that has finished, **When** each container the run started is looked up by its exact identity, **Then** no such container exists — checked by identity, so an unrelated concurrent run cannot make this pass or fail by accident.
2. **Given** a fixture whose startup fails part-way, **When** the failure surfaces, **Then** it is the original cause, and any error raised while cleaning up does not replace it.
3. **Given** a fixture whose startup failed, **When** cleanup runs, **Then** the partially started container is removed rather than leaked.
4. **Given** a fixture being torn down, **When** disposal runs twice, or when there is nothing to dispose, **Then** it completes once, does nothing on the second call, and does not fail on either.
5. **Given** work the fixture does on behalf of a test — starting the container, migrating, resetting, opening connections — **When** it runs, **Then** it honours the caller's cancellation token wherever the underlying API accepts one, while its own cleanup still runs after that token is cancelled.
6. **Given** a connection a test obtained from the broker fixture, **When** the fixture is torn down, **Then** the fixture does not dispose that connection; it belongs to the test that created it.
7. **Given** a machine with no container runtime, **When** the suite runs, **Then** the tests fail; they never pass by skipping.

---

### User Story 7 - Evidence that the change kept what it promised (Priority: P2)

As the team, I need the change to prove that no existing scenario was lost, that the new container versions are the ones the ticket asked for, that nothing outside the agreed boundary moved, and that the quality gates still pass, so this can be merged on evidence rather than on confidence.

**Why this priority**: These are the ticket's own remaining acceptance criteria and the pipeline's standing gates. They are collected last, once the behaviour is in place, and none of them changes what the ticket delivers.

**Independent Test**: Run the full suite and read the evidence: the scenario count against the recorded baseline, the resolved image references and server versions, the container-sharing and teardown observations, the list of changed files, and each gate's exit code.

**Acceptance Scenarios**:

1. **Given** the change, **When** the full integration suite runs, **Then** every integration scenario that existed before still exists and passes, and any change in the number of scenarios is explained in writing rather than absorbed.
2. **Given** the two delivered container versions, **When** the run's evidence is collected, **Then** it records the resolved image reference and content digest for each, and the version each server reports, so a reader can tell exactly what was tested.
3. **Given** the full-suite run time and the run's recorded baseline, **When** they are compared, **Then** the comparison is reported as evidence; no time or count threshold is invented to make the comparison pass.
4. **Given** the agreed boundary, **When** the change set is inspected, **Then** nothing under the production source tree changed, no package version file and no project file changed, the legacy shared-container helper and everything in the unbuilt legacy test project are untouched, and the pre-existing context helper's public members keep their signatures and meaning.
5. **Given** the new fixture behaviour, **When** the tests run, **Then** they cover startup readiness, reset, and usability after reset for both resources, using this repository's assertion library and naming conventions.
6. **Given** the changed test source files, **When** the quality gates run, **Then** the analyzer, complexity, inspection and formatting gates all succeed on them, with the complexity gate also run at the tighter post-implementation threshold; a gate that could not run is reported as such and never counted as passing.
7. **Given** that this change touches test infrastructure only, **When** the mutation gate is considered, **Then** it is recorded as not applicable because no production code changed, and the property-test gate's scope-empty result is recorded as this ticket's opt-out without any threshold or harness setting being changed to accommodate it.

---

### Edge Cases

- **A context left open from a previous test**: the reset takes exclusive locks. A test that leaves a context open inside an uncommitted transaction blocks the reset, and the reset's lock wait is bounded, so it fails with the database's own lock-timeout error rather than hanging. Consumers dispose their contexts; either symptom is a consumer defect to fix, not a reason to change the reset strategy.
- **A consumer still attached when the broker reset runs**: deleting a queue under a live consumer breaks that consumer's channel and can let a message escape into the next test. Every broker test must finish disposing its host before it returns.
- **An empty database cloned from the wrong source**: an empty database created from the migrated default would silently stop proving migration-from-empty. It must come from the platform's own pristine template.
- **A model type that owns a table another type also maps**: the reset must clear that table once, not twice — however many model types map it. The derived list de-duplicates schema-and-table pairs, so the statement names each table a single time regardless of how many entities reach it.
- **A table with no mapped name**: only entities that map to a real table are reset; a key-only or unmapped type contributes nothing and must not produce a malformed statement.
- **Identifiers needing quoting**: a schema or table name that must be quoted is quoted by the database layer, never by string assembly, so a name containing a quote or a space cannot produce broken or injectable SQL.
- **Migration history treated as data**: including the migration-history table in the reset would make every later migration re-run and break the suite permanently. It is excluded, and that exclusion is asserted.
- **Access before startup**: asking for a container, a connection string or a connection before the fixture is started fails clearly instead of returning null.
- **Startup failure after the container is up but before migration or topology**: the container must not be leaked, and the reported error must be the original cause.
- **A second disposal, or a disposal with nothing to dispose**: teardown is idempotent and null-safe, because a test host may dispose more than once.
- **Cleanup that must outlive a cancelled token**: a test that cancels its own token still needs its broker restored and its container removed.
- **Two collections running at the same time**: the two containers are independent, and the PostgreSQL collection's resets cannot disturb the RabbitMQ collection.
- **Container versions that float**: the delivered version tags carry a floating component, so their contents can change between runs. This is recorded through the resolved digests, not solved.
- **A broker test that asserts a queue is empty**: polling for emptiness and deleting the queue are different guarantees; the reset guarantees the second, and tests that need the first still poll.
- **A migration test that needs its own database**: it deliberately does not share the reset, because it is proving the opposite property — that migration works on nothing. That makes it the one deliberate exception to the assumption below, and it is safe precisely because it works on a database of its own.
- **The reset is the only isolation barrier, and a member without it leaks state (R8, accepted).** Per-test data isolation in the database collection rests entirely on every member resetting. Nothing else catches a member that forgot: not a fixture, not a collection definition, not a gate. A consumer added to `PostgresCollection` without a reset therefore leaves its rows behind for the next test, and the failure surfaces as an ordering-dependent error in an unrelated test. Accepted rather than defended, because the guard that would catch it — a test that enumerates collection members and checks each carries the reset — has to reflect over the test types to work, and that is exactly the kind of test that goes stale quietly and that a reader checks instead. Every member is placed and reviewed by hand (FR-002; `checklists/requirements.md` Notes item 3), and that review is the mitigation this ticket actually relies on.

## Requirements

### Functional Requirements

- **FR-001** (AC4, AC3, Q3, US1): A PostgreSQL collection definition and a RabbitMQ collection definition exist in the integration test assembly. Each owns exactly one collection fixture of the matching type. Test classes inside one collection run one at a time; the two collections are free to run concurrently. No collection is marked so as to forbid cross-collection parallelism.
- **FR-002** (AC3, Q3, US1): The PostgreSQL collection's members are `EfMovieCatalogBrowseTests`, `EfMovieCatalogDetailsTests`, `EfMovieCatalogPagingTests`, `EfMovieCatalogSortingTests`, `EfMovieRepositoryTests`, `EfMovieRepositoryClaimConcurrencyTests`, `PersistenceCompositionTests`, `PersistenceRoundTripTests`, `MigrationTests` and the new PostgreSQL fixture-behaviour tests. The RabbitMQ collection's members are `EnrichmentConsumerTests`, `RabbitMqConnectionOwnerTests`, `RabbitMqHealthCheckTests`, `RabbitMqPublisherTests`, `RabbitMqTopologyTests` and the new RabbitMQ fixture-behaviour tests. Every member is a collection member; no member is also a class fixture owner.
- **FR-003** (AC3, Q4, US1): Both assembly-level fixture registrations and every consumer's redundant class-level registration are removed. The file that held the assembly-level registrations is deleted once it is empty, preserving any unrelated attribute that turns up in it. The orphaned collection definition it replaced is deleted. No collection-fixture-free consumer is left in a state that requests a fixture it cannot receive.
- **FR-004** (AC1, Q6, US2): The database fixture starts the container image `postgres:17-alpine`, written as a literal at its point of use. No automatic fallback to a previous tag exists, and no separate constants holder exists for it.
- **FR-005** (AC1, Q2, US2): On startup, after the container is running, the fixture applies every migration to the container's default database. It then caches what the reset needs, derived from the entity model.
- **FR-006** (AC1, Q2, US2): The fixture exposes the migrated default database's connection string. Reading it before startup completes raises an error naming the fixture, matching the existing behaviour of its container accessor.
- **FR-007** (Q2, US2): The fixture exposes a context factory whose name states that the database is already migrated, and every normal consumer moves to it. The existing synchronous context member is removed once no consumer remains, so the semantic change is visible at the call site rather than hidden behind an unchanged name.
- **FR-008** (Q2, US2, R5): The fixture exposes a distinctly named, asynchronous, cancellable path to a context on a brand-new empty database. That database is created independently of the migrated default — never by copying the migrated database — and only the migration test uses it. Its own migration call and all its assertions are unchanged.
- **FR-009** (Q1, US3): The reset is a single `TRUNCATE` over all application tables with identity restart and cascading, executed once, asynchronously and cancellably. It is derived from the entity model rather than from a hand-written table list, so a model change cannot silently leave a table behind. Its lock wait is bounded, and a reset that cannot take its locks fails with the database's own lock-timeout error rather than waiting indefinitely.
- **FR-010** (Q1, US3): The reset's target list keeps only entities mapped to a table, schema-qualifies each with a default schema, removes duplicate schema-and-table pairs, and excludes the migration-history table. Identifiers are quoted by the database layer, never by hand.
- **FR-011** (Q1, Q3, US3): The reset replaces the hand-rolled catalog-only delete that lived in the test assembly; that delete and all of its call sites are removed. The seed helper it lived in keeps its remaining responsibilities.
- **FR-012** (Q1, Q10, US3): A real-database test proves the reset: after seeding a movie with an actor, a director and a genre, every application table and join table holds zero rows, the migration-history table is intact, the first insert after each of two consecutive resets receives the same key, and the database is still usable for insert and read.
- **FR-013** (AC2, Q6, US4): The broker fixture starts the container image `rabbitmq:4-management-alpine`, written as a literal at its point of use, with the same no-fallback and no-constants-holder rules as FR-004.
- **FR-014** (AC2, Q7, US4): On startup the fixture declares the topology by running the production topology class through a short-lived connection owner that is disposed asynchronously. No exchange, queue, binding or queue argument is declared in test code, and the production class remains the only place topology is defined.
- **FR-015** (Q7, US4): The fixture keeps the test-only fast retry delay and the attempt count every current consumer already uses. No production option default changes. The fixture's existing connection-creating member, its options accessor and its options-from-address helper keep their current signatures and meaning.
- **FR-016** (Q8, US5): The fixture exposes an asynchronous, cancellable broker reset that removes the exchange and the three queues by their production names — using awaited broker operations, not a purge — and then re-declares them through a fresh production topology instance on its own short-lived, disposed connection. No queue argument is restated.
- **FR-017** (Q7, US5): The fixture exposes an asynchronous, cancellable operation that removes the known topology without re-declaring it, for tests that must start from nothing declared. Both broker operations are awaited and dispose the connection they opened.
- **FR-018** (Q7, US5): The topology tests remove the known topology before acting and use a fresh production topology instance, and they restore the topology in asynchronous cleanup that runs even when an assertion fails or the test's token is cancelled. Their test bodies are otherwise unchanged, so a test that asserts a declaration fails when production declares nothing.
- **FR-019** (Q8, US5): Each broker consumer resets the broker at the start of every test, except the topology tests, which delete instead and restore on the way out as FR-018 requires. Each database consumer except the migration test resets the database at the start of every test. Where a consumer's disposal has nothing to do, that disposal completes without work rather than being omitted, so every consumer in a collection carries the same lifetime shape.
- **FR-020** (Q9, US6): Each collection fixture owns its container. A startup failure disposes the partially started container through its asynchronous disposal, not tied to the test's cancellation token, catches any secondary cleanup error, clears the stored reference, and rethrows the original exception. Disposal is asynchronous, idempotent and safe when there is nothing to dispose.
- **FR-021** (Q9, US6): Initialization, reset and connection work honour the caller's cancellation token wherever the underlying API accepts one, while cleanup still runs after that token is cancelled. A connection a caller creates belongs to that caller and is not disposed by the fixture. Container resource reaping stays enabled as a crash backstop.
- **FR-022** (AC5, Q9, US6): Teardown is verified against the exact container identities the run started, looked up by identity rather than by label or image sweep, so an unrelated concurrent run cannot make the check pass or fail.
- **FR-023** (AC2, Q8, Q10, US4, US5): A real-broker test proves the reset: the exchange and three queues exist immediately after startup by passive declare; a message published to the retry queue before the reset is in none of the three queues afterwards; and publish and get both work after a reset.
- **FR-024** (AC1, Q2, US2, US4): Redundant per-test migration calls are removed from every normal consumer, because the fixture already migrated the database. Reads of a connection string from a context return the migrated database's string and need no change.
- **FR-025** (Q10, US7): New fixture-behaviour tests use this repository's test framework, assertion library, arrange-act-assert structure, cancellation-token convention and `Method_Scenario_Expectation` naming. New tests carry the three arrange-act-assert headers and no other comment.
- **FR-026** (AC6, US7): Every integration scenario that exists before this change still exists and passes. Any change in the number of scenarios is explained in the task note; no scenario count or time figure is turned into a threshold.
- **FR-027** (AC7, US7): The verification evidence records the resolved image reference and content digest for each delivered container image, the version each server reports, the observed container sharing, the exact-identity teardown result, the full-suite result, and the full-suite time compared against the recorded baseline. The evidence is recorded; no performance or count target is invented from it.
- **FR-028** (AC8, Q5, US1, US7): Nothing outside the agreed boundary changes: no production source file, no package version file, no project file, no container-resource-reaping setting, and no gate threshold. The legacy static container helper and the unbuilt legacy test project are untouched. The pre-existing context helper's public static members keep their signatures and semantics.
- **FR-029** (AC9, US7): The analyzer gate, the cyclomatic-complexity gate at the implementation threshold, the inspection gate and the format check all succeed on the changed test source files, and the complexity gate additionally succeeds at the tighter post-implementation threshold. A gate that could not run is reported as such and is never folded into a passing verdict. The mutation gate is recorded as not applicable because no production code changed; the property-test gate's scope-empty result is recorded as this ticket's opt-out in the task note, with no harness or threshold edit.
- **FR-030** (Q1, Q4, US3, US4): No new package enters the graph. The reset uses the entity model and one truncate statement; it does not introduce a reset library, a reset abstraction or an interface. The fixtures remain in the existing shared test project and the collection definitions in the consuming test assembly; no new project or architectural layer is created.

### Key Entities

- **PostgreSQL collection fixture**: the shared, per-run owner of one database container. On startup it starts the container, migrates the default database and caches the reset statement. It hands out a migrated context, the connection string, an empty-database context, and the reset. It owns the container's lifetime.
- **RabbitMQ collection fixture**: the shared, per-run owner of one broker container. On startup it starts the container, builds the connection factory and declares the production topology on a short-lived connection. It hands out connections, the options describing the broker, a topology reset and a topology delete. It owns the container's lifetime.
- **PostgreSQL collection / RabbitMQ collection**: the two named groupings that make each fixture the property of a collection rather than of the assembly or of a class. They are the unit of sharing and of serialization.
- **Reset target list**: the model-derived set of schema-qualified table names the reset clears — de-duplicated, unmapped types excluded, migration history excluded. Derived from the model so it cannot drift from it.
- **Pre-existing context helper**: the static factory that mints contexts. Its fresh-database and open-context members keep their meaning; this ticket adds at most an asynchronous helper beside them and changes no existing member.
- **Owned container identity**: the exact identity of each container this run started, recorded so that sharing and teardown are both proved against a specific container rather than against an image, a label or a count.

## Success Criteria

### Measurable Outcomes

- **SC-001**: 0 assembly-level fixture registrations and 0 class-level registrations for either fixture remain in the integration test assembly; every class that needs a resource is a member of exactly one of the two collections, and the class that needs neither is in neither.
- **SC-002**: One PostgreSQL container and one RabbitMQ container are started for a full integration run, and each is observed with the same identity by at least two classes in its collection. Zero extra fixtures or containers are created in order to demonstrate sharing.
- **SC-003**: 0 application tables and 0 join tables hold a row after a reset, the migration-history table still lists every migration, and the first insert after two consecutive resets receives the same key both times.
- **SC-004**: Immediately after each fixture starts, the database reports every known migration applied with no pending model changes, and the broker answers a passive declare for the exchange and for all three queues with arguments equal to the production declaration.
- **SC-005**: 0 integration scenarios are lost: every scenario present before the change is present and passing after it, and any difference in count is explained in writing. The pre-existing context helper's public members compile and behave as before.
- **SC-006**: After a run, 0 of the two container identities this run started still resolves to an existing container, checked by identity.
- **SC-007**: The recorded evidence contains the resolved image reference and digest for both delivered images, the version each server reports, the sharing observation, the teardown result, the full-suite result and the full-suite time compared against the recorded baseline of 105 passing integration tests in 1 m 17 s.
- **SC-008**: 0 files change under the production source tree, in the package version file, in any project file, in the legacy static container helper, or anywhere in the unbuilt legacy test project. 0 gate thresholds and 0 harness settings change.
- **SC-009**: Each of the analyzer, complexity, inspection and format gates exits 0 on the changed files, the complexity gate also exits 0 at the tighter post-implementation threshold, and 0 gate results are reported as passing when they did not run.
- **SC-010**: A forced startup failure surfaces the original cause rather than a cleanup error, and leaves 0 containers behind; disposing a fixture twice, and disposing one that initialised nothing, both succeed.

## Assumptions

- **Carried `[assumed]` rulings** (full basis in `CONCLUSIONS.md`, restated in `ASSUMPTIONS.md`): Q3 the two collection definitions are named `PostgresCollection` and `RabbitMqCollection`, one fixture instance per matching collection; Q2 the normal-path context factory is named `CreateMigratedContext` so consumer changes visibly distinguish a startup-migrated context from an empty migration-test database. The exact spelling of the empty-database and reset members follows existing conventions without changing the ruled behaviour.
- **No user-interface taste decisions apply.** Nothing here is copy, layout or wording; structural and lifecycle choices are deliberate Patron rulings in `CONCLUSIONS.md`, not silent assumptions.
- **The delivered container tags are ticket text.** They are not performance or preference choices, and a change to either is a ticket-change escalation rather than an implementation decision (`specs/PRODUCT.md:23-24`; Q6).
- **Sharing narrows from assembly-wide to per-collection, deliberately.** Collection fixtures do not share across collections, so the change is a deliberate narrowing agreed in Q3 rather than a side effect. Every current consumer is placed in one of the two collections, so no consumer loses access.
- **The reset strategy is fixed by Q1**: a single model-derived truncate, not a third-party reset library, and not the two mechanisms this repository already uses for other purposes. `fresh database per test` remains available for the migration test only; `delete rows` is replaced by the common reset.
- **Collections do not forbid cross-collection parallelism, by ruling D1.** The collection definition this ticket replaces carried a no-parallelisation setting; `brief.md` §Drafting decisions D1 rules it omitted, because Q3 requires both intra-collection serialisation and cross-collection concurrency, and the setting would supply only the first while forbidding the second. Within a collection, serialisation is xUnit's default and needs no setting. Interference between the two collections when they overlap is a stop-and-report to Keel, not a licence to add the setting back.
- **The mutation gate is not applicable.** This change alters test infrastructure only and no production code, so there is no production mutant to survive. The property-test gate is expected to report no tagged tests and this ticket records its opt-out in its task note, as `harness.yml` provides for; no threshold moves to clear it.
- **Out of scope** (restated for the reader): anything in the production source tree; any package or project-file change; the legacy static container helper and the unbuilt legacy test project, including their retirement; a constants holder for image tags; a broker virtual-host option; a reset abstraction or interface; a third collection or a mixed-resource collection; a second broker container; the container-resource-reaping safety net; continuous-integration workflow changes; and the class that needs no fixture.
- **Verification of the container-teardown criterion is by container identity, not by absence of a label or image.** A concurrent unrelated run on the same machine would make a label or image sweep meaningless in both directions.
- **Rounding of the recorded baseline.** The comparison of full-suite time against 105 passing tests in 1 m 17 s is evidence for a reader, not a target; a slower or faster run is reported, not tuned.
