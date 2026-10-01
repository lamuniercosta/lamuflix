# Implementation Plan: Reusable Testcontainers Fixtures

**Branch**: `feature/306-spec` | **Date**: 2026-10-01 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`, `brief.md` (Phase A brief, 223 lines), `CONCLUSIONS.md` (Q1-Q10), `ASSUMPTIONS.md`, note `recon-DEV-306` (baseline `3b2e998`), `.specify/memory/constitution.md`, `specs/PRODUCT.md`, `harness.yml`

**Note on the Spec Kit phases**: this plan is the `/speckit-plan` output. Phase 0 (`research.md`) and Phase 1 (`data-model.md`, `contracts/`, `quickstart.md`) produce **no files** here, deliberately: every unknown those phases would resolve was already decided by a Patron ruling in `CONCLUSIONS.md` and restated in `brief.md`, and the feature exposes no external interface — no HTTP route, no DTO, no wire format, no public package (`tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj:5-6` is `IsPackable=false`, `IsTestProject=false`, and no `src/` project references it). The entities, contracts and commands that would have gone in those files are instead inlined below as Design §1-§6, Test Strategy and Gates, which is the precedent DEV-303 and DEV-304 set. Writing three empty artefacts would be worse than stating why they are absent.

## Summary

Converge the two container fixtures this repository already has onto the contract DEV-306 asks for. Rewrite `PostgresFixture` to start `postgres:17-alpine`, migrate its default database at startup, expose that database's connection string, hand out migrated and empty-database contexts, and reset every application table with one model-derived `TRUNCATE … RESTART IDENTITY CASCADE`. Rewrite `RabbitMqFixture` to start `rabbitmq:4-management-alpine`, declare the **production** `RabbitMqTopology` at startup through a short-lived connection owner, and expose a topology reset (delete the exchange and three queues, re-declare) plus a topology delete. Replace the assembly-scoped sharing with two xUnit v3 collection definitions, `PostgresCollection` and `RabbitMqCollection`, move all fourteen consumers onto them, delete the assembly-level wiring and the collection definition it replaces, and remove the hand-rolled catalog-only delete. Add real-infrastructure fixture-behaviour tests for both resources and collect the sharing, teardown, image-version, boundary and gate evidence the ticket's acceptance criteria ask for.

Every decision below is already fixed by `brief.md`, `CONCLUSIONS.md` or `ASSUMPTIONS.md`. Where the brief offers a branch, this plan records the branch taken and why it needed no new ruling — and where this drafting pass had to ask, §8 records the question, the ruling, and the obligation the ruling added.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`), `Nullable` enable, `TreatWarningsAsErrors` true, central package management on
**Primary Dependencies**: unchanged. `Testcontainers.PostgreSql` 4.15.0 and `Testcontainers.RabbitMq` 4.15.0 (`Directory.Packages.props:13-14`), `xunit.v3` 4.0.1 (`:30`), `xunit.v3.extensibility.core` 4.0.1 (`:31`), `RabbitMQ.Client` 7.2.2 (`:9`), `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (`:12`), `Microsoft.EntityFrameworkCore` 10.0.12 (`:10-11`). **No package is added and no version moves** (Q1, AC8). The new image tags are reachable through the `PostgreSqlBuilder(string image)` and `RabbitMqBuilder(string image)` overloads already in use (recon-DEV-306:78).
**Storage**: PostgreSQL 17 via a real container, and RabbitMQ 4 via a real container. This is the only storage the feature touches, and it is test-only. `src/LamuFlix.Infrastructure/Persistence` is read (its `LamuFlixDbContext` and its entity model) and never edited.
**Testing**: xUnit v3 4.0.1 with Shouldly, `TestContext.Current.CancellationToken`, and `IAsyncLifetime`. Sharing moves from `AssemblyFixture` to `ICollectionFixture` + `CollectionDefinition`. No data-access driver is mocked (constitution.md:300-301) and EF Core InMemory stays forbidden.
**Target Platform**: any OS with a container runtime; the containers are version-pinned by tag, not by platform. Docker 29.8.1 / SDK 10.0.400 on the recon baseline (recon-DEV-306:62).
**Project Type**: two rewrites inside an existing shared test library, two new collection definitions inside an existing test project, and consumer edits in that same test project. **No new project and no architectural layer** (constitution.md:284 places persistence and messaging integration tests in `LamuFlix.IntegrationTests`, which already exists).
**Performance Goals**: none stated by the ticket, and none invented. The recorded baseline is 105 passing integration tests in 1 m 17 s (recon-DEV-306:62) and is reported as evidence, not as a target (Q10, AC7).
**Constraints**: zero changes under `src/`; zero changes to `Directory.Packages.props` or any `.csproj`; `ContainerFixture.cs` and everything under `tests/LamuFlix.Test/` untouched; the public static members of `LamuFlixDbContextFactory` keep their signatures and semantics; no third-party reset library; no reset abstraction; no exchange/queue/binding/argument declared in test code; no automatic fallback to the old image tags; no explanatory comment beyond the three AAA test headers; every async path takes a `CancellationToken` and no sync-over-async.
**Scale/Scope**: 24 files change — 2 fixture rewrites, 1 additive-only helper edit, 2 new collection definitions, 1 method removed from an existing seed helper, 14 existing consumer classes converted, 2 new test files, 2 deleted files — ~20 call sites dropped, 4 new identity-emitting test cases, 0 packages, 0 schema changes, 0 API changes.

## Constitution Check

*Gate: must pass before research; re-checked after design.*

- **Dependencies** (§2.3 item 1): none added. Q1 rules out a third-party reset library and requires one model-derived truncate instead. The two new image tags are constructor arguments to builders already referenced. Verified: `Microsoft.Extensions.Options` — the one assembly the broker fixture's new startup path needs for `Options.Create` — is a `PackageReference` in `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` and therefore already flows transitively into `tests/LamuFlix.Tests.Common`, which references that project. No new package, no `csproj` edit, no version move. **PASS.**
- **Architecture** (§2.3 item 2): no new project, no top-level folder, no layer. The shared fixtures stay in `tests/LamuFlix.Tests.Common/`, which exists, is in the solution, and is named by the ticket; the collection definitions go in the consuming assembly, which is xUnit's own requirement and is what Q3 rules. Nothing is abstracted, no interface is added, no mediator or repository layer appears. **PASS.**
- **Database schema** (§2.3 item 3): none. The fixture applies the single existing `Initial` migration that already exists; it creates no migration, alters no model and changes no snapshot. `MigrationTests.cs:57-61` asserts exactly one migration ending in `_Initial` with no pending model changes, and the image bump must not change that count. The reset's `TRUNCATE` targets tables that the migration already created. **PASS.**
- **API shape** (§2.3 item 4): none. No HTTP route, DTO field set, status code, OpenAPI document or `/web` change. The contract chain C# DTO → OpenAPI → generated TypeScript is untouched because this feature adds no endpoint. The test-fixture members that change are in a non-packable, non-test project consumed only by test assemblies. **PASS.**
- **Security & local execution** (§2.3 item 5): `Features:LocalPlay` untouched, no `Process.Start`, no media playback execution. All credentials are generated at runtime by the container and read out of the container's URI (`RabbitMqFixture.cs:45-52`); nothing is hardcoded and no secret is committed. `RabbitMqOptions.PrintMembers` redacts the password (`RabbitMqOptions.cs:50`) and stays intact — the fixture never logs options. **PASS.**
- **File scope** (§2.3 item 6): the two fixture rewrites are named by the brief's frozen scope; the collection additions, the two deletions and the consumer edits are authorized by Q3 and Q4, which rule that the ticket's collection-fixture requirement forces them. `ContainerFixture.cs` and `tests/LamuFlix.Test/` are explicitly left alone (Q5). No other file is deleted or rewritten. **PASS.**
- **Test pyramid** (`constitution.md:282-303`): this feature *is* the test pyramid's infrastructure row. Infrastructure is exercised through Testcontainers for PostgreSQL and RabbitMQ, never mocked; EF Core InMemory stays forbidden. The reset adds no new mocking seam — it is proved against a real server. **PASS.**
- **Static analysis** (`constitution.md:378-394`): no method over the complexity ceiling of 15, and none over 6 at the refactor gate. The two fixtures each gain a guarded startup and one or two small helpers; the helpers exist precisely to keep `InitializeAsync` under the refactor ceiling. Complexity failures are fixed by extraction, never by suppression. **PASS.**
- **Comment and suppression rules** (`AGENTS.md`): no explanatory comment is added. Two pre-existing comments are in the blast radius and neither may grow: `RabbitMqFixture.cs:12` ("One declared topology is shared by the whole assembly, so every test must agree on the delay") becomes **false** under collection fixtures, and `ContainerFixture.cs:10-11` is out of scope and untouched. The first is a real, small consequence of Q3 and is handled as a task below — see Design §7.
- **Async and cancellation** (`constitution.md:433-435`): every new async member takes a `CancellationToken`; cleanup paths deliberately take none, so teardown still runs after cancellation (Q9). No `.Result`, `.Wait()` or `.GetAwaiter().GetResult()`. **PASS.**
- **ADR**: none. No new port, no architectural layer, no public type outside a test project. ADRs are Keel's, not the writer's.
- **Owner checkboxes**: none. No ticket change and no constitution departure. Gate 1 stays closed.

**Status: PASS, no departures, no escalations from the constitution.** One behavioural question was escalated on other grounds and is now ruled — see §8, D1.

## Project Structure

```text
tests/LamuFlix.Tests.Common/
├── PostgresFixture.cs                     (rewrite: image, startup migration, ConnectionString,
│                                           CreateMigratedContext, CreateEmptyDatabaseContextAsync,
│                                           ResetAsync, guarded lifetime)
├── RabbitMqFixture.cs                     (rewrite: image, production topology at startup,
│                                           CreateConnectionAsync(ct) overload, ResetTopologyAsync,
│                                           DeleteTopologyAsync, guarded lifetime)
├── LamuFlixDbContextFactory.cs            (additive only: async empty-database helper; no existing
│                                           public member changes signature or meaning)
├── ContainerFixture.cs                    (UNCHANGED — Q5)
└── LamuFlix.Tests.Common.csproj           (UNCHANGED — AC8)

tests/LamuFlix.IntegrationTests/
├── PostgresCollection.cs                  (new)
├── RabbitMqCollection.cs                  (new)
├── AssemblyInfo.cs                        (DELETE — the two AssemblyFixture lines are all it holds)
├── MovieCatalogCollection.cs              (DELETE — replaced by PostgresCollection)
├── MovieCatalogSeed.cs                    (edit: remove ResetAsync at lines 24-31; rest unchanged)
├── RabbitMqProbe.cs                       (UNCHANGED — polling helpers stay with their consumers, Q8)
├── PostgresFixtureTests.cs                (new)
├── RabbitMqFixtureTests.cs                (new)
└── 14 consumer classes                    (edit: Collection attribute or IClassFixture removed,
                                            IAsyncLifetime added, context/migration/reset call sites moved)
```

**Twenty-four files change; two are deleted.** Nothing else. In particular: no `src/` file, no `.csproj`, no `Directory.Packages.props`, no `harness.yml`, no `stryker-config.json`, no migration, no model snapshot, no DI extension, no `Program.cs`, and nothing under `tests/LamuFlix.Test/`. Of the twenty-four, **two are the deletions** and the remaining **twenty-two** are the files that exist on disk afterwards — which is exactly the list the gate pass below walks, because a deleted file cannot be analysed.

**The fourteen existing consumers, plus the two new fixture-behaviour classes.** PostgreSQL (9 existing + `PostgresFixtureTests`): `EfMovieCatalogBrowseTests`, `EfMovieCatalogDetailsTests`, `EfMovieCatalogPagingTests`, `EfMovieCatalogSortingTests`, `EfMovieRepositoryTests`, `EfMovieRepositoryClaimConcurrencyTests`, `PersistenceCompositionTests`, `PersistenceRoundTripTests`, `MigrationTests`, `PostgresFixtureTests`. RabbitMQ (5 existing + `RabbitMqFixtureTests`): `EnrichmentConsumerTests`, `RabbitMqConnectionOwnerTests`, `RabbitMqHealthCheckTests`, `RabbitMqPublisherTests`, `RabbitMqTopologyTests`, `RabbitMqFixtureTests`. `MovieCatalogRegistrationTests` needs neither and is not edited (brief frozen scope, out).

**No `research.md`, `data-model.md`, `contracts/` or `quickstart.md`**, and the reason is stated under **Note on the Spec Kit phases** above rather than left implicit.

## Design

### 1. The two collection definitions

```csharp
namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

[CollectionDefinition(nameof(RabbitMqCollection))]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>;
```

**`DisableParallelization` is omitted, by ruling D1.** `MovieCatalogCollection.cs:5` — the only collection definition in the repository, and the file this ticket deletes — carries `[CollectionDefinition("MovieCatalog", DisableParallelization = true)]`. In xUnit, tests inside a collection already never run in parallel; `DisableParallelization = true` additionally prevents that collection from running *alongside any other collection*. Omitting it therefore satisfies Q3 in full — intra-collection serialisation comes from xUnit's default, and the two collections overlap — which is exactly what D1 rules and what AC7's concurrent timing evidence depends on. **The attribute is not to be added back as a fix for anything:** cross-collection interference is a stop-and-report to Keel, and adding it or splitting into a third collection needs a new ruling (plan §8, D1).

**Collection name strings — `nameof`, and no holder file.** The ruled type names are `PostgresCollection` and `RabbitMqCollection` (ASSUMPTIONS.md, Q3). xUnit matches a consumer's `[Collection("…")]` string against the definition's `[CollectionDefinition("…")]` string, and the string has to agree across every member of the collection. `nameof(PostgresCollection)` on the definition and `nameof(PostgresCollection)` on each consumer makes the **type name itself** the one source: the compiler derives every string from it, so the definition and the consumers cannot drift, and renaming the type is a compile error rather than a silent split. That buys the single-source guarantee without a constants holder. The holder this replaces — `tests/LamuFlix.IntegrationTests/IntegrationCollections.cs` — is **not created**: the brief's Frozen scope **In** list (`brief.md`:33-41) does not name it, and `brief.md`:53 makes any file outside **In** a stop for a Patron ruling, so a `needs decision:` escalation would be the price of a convenience the type name already provides. Recorded as a code-shape choice in §9, not escalated; the ruled type names are unchanged either way. The dropped `Collection` suffix of `MovieCatalogCollection` / `[Collection("MovieCatalog")]` is not inherited: `nameof` yields the type name verbatim, and a type named `PostgresCollection` in collection `nameof(PostgresCollection)` is the shape xUnit documents.

**MigrationTests is in `PostgresCollection` but does not reset.** Q3 lists it as a member; Q2 gives it the empty-database path and its own `MigrateAsync`, because it proves the opposite property. Its membership and its non-reset are both ruled, and the asymmetry is recorded in `checklists/requirements.md` Notes item 3 so a reader auditing "does every consumer reset" reads it as a ruling rather than a miss.

### 2. `PostgresFixture` startup

`InitializeAsync`, in this order:

1. `_container = new PostgreSqlBuilder("postgres:17-alpine").Build(); await _container.StartAsync();`
2. `await using var context = LamuFlixDbContextFactory.OpenContext(Container.GetConnectionString()); await context.Database.MigrateAsync(cancellationToken);`
3. `_truncate = BuildTruncate(context);` — the cached, model-derived statement (Design §3).

Steps 1-3 are wrapped in `try`/`catch`. On failure the handler, in order: dispose the container through its **asynchronous** disposal API with **no** cancellation token tied to the test's, so the disposal still happens after the test token is cancelled (Q9); catch and swallow any secondary error from that disposal; set the field to `null`; `throw;` the original (Q9, AC/US6 scenario 2-3). Rethrowing the caught exception — not wrapping, not rethrowing the cleanup error — is what keeps the original cause the reported one.

`ConnectionString` reads the migrated default database and throws `InvalidOperationException` naming the fixture before initialisation, exactly as the existing `Container` accessor does at `PostgresFixture.cs:13-15`. That shape is preserved deliberately: it is the fixture's existing, tested contract for "not ready yet", and Q9 keeps the member.

**Cancellation in `InitializeAsync`.** Q9 asks for `TestContext.Current.CancellationToken` "where the API supports it", and it does support it here: a collection fixture's own `InitializeAsync` runs under a current test context, and `ITestContext.CancellationToken` in the pinned `xunit.v3.core` 4.0.1 carries no nullability caveat, so the token is used with no fallback needed. The *cleanup* path is untied from the token either way (Design §2 above), which is the part Q9 actually cares about. Note that `ITestContext.TestOutputHelper` is the opposite — see Test Strategy evidence 1 for why the sharing evidence cannot be emitted from here.

**`CreateMigratedContext()`** wraps `LamuFlixDbContextFactory.OpenContext(ConnectionString)` (Q2, ASSUMPTIONS.md). The existing synchronous `CreateContext()` at `PostgresFixture.cs:31` is removed once no consumer remains, so the semantic change — migrated database, not a fresh one — is visible at each call site rather than hidden behind a name that used to mean something else (Q2's explicit requirement, AC8 keeping the static factory's own members intact).

**`CreateEmptyDatabaseContextAsync(CancellationToken)`** is the only empty-database path. It issues `CREATE DATABASE` for a fresh unique name against the admin connection and opens it. It carries **no** `TEMPLATE` clause, which is what makes R5 hold: with no clause, PostgreSQL copies `template1`, the platform's pristine template — never the migrated default database. Omitting the clause is therefore the whole guarantee, and it is the shape the existing private helper already uses (`LamuFlixDbContextFactory.cs:44`). `MigrationTests` is its only caller.

### 3. The reset statement

`BuildTruncate` derives the target list from the entity model, in this order:

1. `context.Model.GetEntityTypes()`, which includes the shared-type join entities that own `movie_actors`, `movie_directors` and `movie_genres` — these are why the model is the source and a hand-written list is not.
2. Keep only entities whose `GetTableName()` is non-null. A key-only or unmapped type contributes nothing and must not produce a malformed statement.
3. Schema-qualify each: `GetSchema() ?? "public"`.
4. Remove duplicate `(schema, table)` pairs, so the emitted statement names each table once however many model types map it (Q1). This is a de-duplication the ruling requires, not a workaround for a server restriction: PostgreSQL accepts a repeated relation in one `TRUNCATE` list and skips the repeat (`ExecuteTruncate`). No test therefore *observes* the de-duplication; what a test can observe is that every join table was cleared at all, which the model walk is what affects.
5. Exclude `__EFMigrationsHistory`. It is not in the model, so the exclusion is belt-and-braces; it is asserted anyway by the fixture-behaviour test, because truncating it would make every later migration re-run and break the suite permanently.
6. Quote each identifier through the database layer's own quoting, never by string assembly, so a name containing a quote or a space cannot produce broken or injectable SQL (Q1, AC/US3 scenario 6).
7. Emit one statement: `TRUNCATE TABLE <quoted list> RESTART IDENTITY CASCADE`.

The result is cached at startup and reused. `ResetAsync(CancellationToken)` executes exactly that one statement and nothing else.

**Why one statement and not a loop.** `RESTART IDENTITY` and `CASCADE` are what the story needs — identities restart (US3 scenario 3) and a join row cannot survive its parent (US3 scenario 1). `CASCADE` is also what makes a single statement safe against a foreign key from a table the model does not list.

**The lock risk is named, not solved away.** `TRUNCATE` takes `ACCESS EXCLUSIVE` locks. A context left open from a previous test inside an uncommitted transaction blocks it, which is R3. The mitigation is that every consumer already disposes its contexts with `await using` and must keep doing so; a hang here is a consumer defect to fix, not a reason to change the reset strategy. A test that leaves a context open is therefore a defect this ticket must catch, and the fixture-behaviour test for "usable after reset" is what surfaces it early.

### 4. `RabbitMqFixture` startup

`InitializeAsync`, in this order:

1. `container = new RabbitMqBuilder("rabbitmq:4-management-alpine").Build(); await container.StartAsync();`
2. `factory = new ConnectionFactory { Uri = new Uri(container.GetConnectionString()) };` — unchanged from `RabbitMqFixture.cs:28`.
3. Declare topology through production code:
   ```csharp
   await using var owner = new RabbitMqConnectionOwner(Options.Create(Options));
   await new RabbitMqTopology(owner, Options.Create(Options), Options.Create(new EnrichmentOptions { MaxAttempts = 3 }))
       .EnsureDeclaredAsync(cancellationToken);
   ```
   A **short-lived** owner, asynchronously disposed by `await using`, so the fixture holds no connection of its own. `MaxAttempts = 3` matches every current consumer (`EnrichmentConsumerTests.cs:30`, `RabbitMqPublisherTests.cs`, `RabbitMqTopologyTests.cs:18,198-199`) and no production default changes (Q7).

Failure handling is identical to the database fixture's (Q9). `RetryDelay` (2 s, `RabbitMqFixture.cs:13`), `OptionsFor` (`:43-56`) and `Options` (`:22`) keep their signatures and semantics (FR-015).

**`CreateConnectionAsync(CancellationToken)` — the branch this plan takes.** The brief offers two ways to satisfy Q9's cancellation propagation here: an additive token-accepting overload, or threading the token through the existing route. **This plan takes the additive overload**, passing the token to `ConnectionFactory.CreateConnectionAsync(ct)` and **preserving the existing no-token member**. Two reasons, both mechanical: `RabbitMqProbe.cs:201` is the only current caller of the no-token member, so preserving it means `RabbitMqProbe.cs` — which the brief only conditionally authorises touching — needs **no edit at all**; and `RabbitMqConnectionOwner.cs:37` already demonstrates that the token-accepting `CreateConnectionAsync(ct)` overload is the one the rest of this repository uses. No new option, no refactor, no behaviour change for existing callers. Recorded in §9 as a branch the brief delegated to the plan.

### 5. Broker reset and delete

`ResetTopologyAsync(CancellationToken)`: open one short-lived connection; on one channel, `QueueDeleteAsync` for `RabbitMqTopology.RequestedQueue`, `RetryQueue` and `DeadLetterQueue` in that order, then `ExchangeDeleteAsync` for `RabbitMqTopology.ExchangeName` — queues before exchange, because a binding still referencing the exchange blocks its deletion; then re-declare through a **fresh** `RabbitMqTopology` instance on a fresh short-lived owner, exactly as at startup. Every operation is awaited, and the connection is disposed by the method. Names come from the production constants (`RabbitMqTopology.cs:16-19`); no queue name and no queue argument is restated in test code (Q8, FR-016).

`DeleteTopologyAsync(CancellationToken)`: the same three queue deletions and the exchange deletion, with **no** re-declaration — the empty-start preparation for the topology tests (FR-017).

**Why delete-and-redeclare rather than purge.** Retry TTL and at-least-once dead-letter forwarding can move a message between queues on their own (`RabbitMqTopology.cs:65-74`), so serial execution plus one purge pass does not rule out cross-test contamination. Deleting the queues destroys the messages outright, which is the guarantee the story needs (Q8). A test that needs the *opposite* — to observe a message arriving — still polls; the reset guarantees deletion, not observability, and `RabbitMqProbe`'s polling helpers stay in the test assembly (Q8).

**Ordering is a consumer obligation, not a fixture guarantee.** A reset that deletes a queue while the previous test's consumer is still attached breaks that consumer's channel and can let a message escape into the next test (R4). Because tests inside a collection run one at a time, the previous test has stopped before the next one's `InitializeAsync` runs — **provided every broker test finishes disposing its host before it returns.** Each of the four does so with `await using` today. `RabbitMqTopologyTests` is the exception: it deletes rather than resets on the way in, and restores with `ResetTopologyAsync(CancellationToken.None)` on the way out, so the topology is back for the next test even when an assertion failed or the test's token was cancelled (Q7, Q9).

### 6. Consumer conversion

Every database consumer except `MigrationTests` implements `IAsyncLifetime`: `InitializeAsync` calls `fixture.ResetAsync(TestContext.Current.CancellationToken)`, and `DisposeAsync` is the expression-bodied `public ValueTask DisposeAsync() => ValueTask.CompletedTask;`. That form leaves no empty block, so `AGENTS.md`'s empty-block justification exemption is never invoked and no comment is added. Primary-constructor fixture injection is unchanged, which is what makes the `[Collection(...)]` attribute the only wiring change for eight of the nine database consumers.

Call-site moves, per the brief:

| Move | Where |
|---|---|
| `fixture.CreateContext()` → `fixture.CreateMigratedContext()` | `EfMovieCatalogBrowseTests` (8 sites), `Details` (6), `Paging` (3), `Sorting` (3), `EfMovieRepositoryTests:211`, `ClaimConcurrency:24`, `PersistenceCompositionTests:92`, `PersistenceRoundTripTests:164` |
| Drop the now-redundant `Database.MigrateAsync` | `EfMovieRepositoryTests:212`, `ClaimConcurrency:25`, `PersistenceCompositionTests:93`, `PersistenceRoundTripTests:165`, `EfMovieCatalogPagingTests:70,77` |
| Drop every `MovieCatalogSeed.ResetAsync(context, ct)` call | the four `EfMovieCatalog*Tests`, ~20 call sites |
| `fixture.Container.GetConnectionString()` → `fixture.ConnectionString` | `EfMovieCatalogPagingTests:66` |
| `fixture.CreateContext()` → `await fixture.CreateEmptyDatabaseContextAsync(ct)` | `MigrationTests:53,67` — its own `MigrateAsync` (`:55,68`) and every assertion (`:51-77`) stay |
| Connection-string reads off a context | `Details:32`, `Paging:35`, `ClaimConcurrency:26`, `Repository:218`, `Composition:94`, `RoundTrip:171` — **no edit**; they now return the migrated database's string, which is what those tests need |
| `IClassFixture<RabbitMqFixture>` → `[Collection(...)]` + `IAsyncLifetime` calling `ResetTopologyAsync` | `EnrichmentConsumerTests:28`, `RabbitMqConnectionOwnerTests:17`, `RabbitMqHealthCheckTests:12`, `RabbitMqPublisherTests:21` |
| Topology tests: delete on the way in, restore on the way out | `RabbitMqTopologyTests:16` |

Two of these are worth stating as consequences rather than mechanics. First, `EfMovieCatalogPagingTests:66` currently builds a second context against the *container's* default database; after the change it uses the fixture's migrated string, so the two-context concurrency scenario that test exercises still has two contexts against one migrated database rather than one migrated and one empty. That is what the test means. Second, `MovieCatalogSeed.ResetAsync` (lines 24-31) is removed because the class-level reset covers it, and the class keeps `Create`, `AddAsync` and `FixedTime` — so it remains a seed helper rather than becoming a reset helper.

### 7. A consequence of Q3 that needs a small, named edit

`RabbitMqFixture.cs:12` carries the comment *"One declared topology is shared by the whole assembly, so every test must agree on the delay."* Under collection fixtures that sentence is **false**: sharing is now **per collection**, not assembly-wide, so one RabbitMQ-owning collection is what every broker test agrees on rather than the whole assembly, and the stated reason — assembly-wide sharing — is gone with it. `AGENTS.md` forbids explanatory comments, so the fix is deletion, not rewriting: the retry delay's rationale is not a fact a future reader needs restated, and the value's meaning is fixed by Q7 and the production class. Task T027 removes the comment line. Recorded here because deleting a comment is exactly the kind of edit a scope review flags, and it should be seen as required by Q3 rather than as cleanup.

### 8. Ruled: the `DisableParallelization` question (D1)

**Ruled in `brief.md` §Drafting decisions as D1: omit the attribute.** Keel, spec-review round 1, answering this plan's `needs decision:` escalation.

`PostgresCollection` and `RabbitMqCollection` are plain `[CollectionDefinition("<name>")]` with **no** `DisableParallelization`. The basis: Q3 rules both clauses explicitly — classes within a collection serialise, *and* the two collections may run concurrently — and xUnit's default already serialises within a collection, so the attribute would add nothing except the exclusion of every other collection, which is Q3's second clause contradicted. `CONCLUSIONS.md` outranks the `MovieCatalogCollection.cs:5` precedent; that file is deleted by Q4 in any case, it pre-dates the two-collection split, and no ruling adopted its attribute.

Three consequences the ruling fixes, which this plan now carries instead of leaving open:

1. **AC7's timing is a concurrent run.** T036 compares the full-suite time against the 1 m 17 s baseline as a run in which the two collections overlapped, not one in which they were serialised.
2. **Cross-collection interference is a stop, not a tuning knob.** If the green-on-old-images run at task-order step 2 — or any later run — shows interference between a PostgreSQL class and a RabbitMQ class (shared static telemetry, listener state, anything of that shape), the implementer stops and reports it to Keel with the failing tests. Adding `DisableParallelization`, or splitting into a third collection, is a **new ruling** and is never a silent fix. That is D1's stop condition, feeding R1/R6.
3. **Why the attribute was there is not recoverable.** No ruling records why `MovieCatalogCollection` set it, so the stop condition above — not a reconstruction of the original intent — is what covers the risk D1 accepts.

### 9. Choices this plan makes, recorded as branches rather than rulings

| Choice | Branch taken | Why it needed no ruling |
|---|---|---|
| Connection-cancellation shape (`brief.md` Design §2 offers two) | additive `CreateConnectionAsync(CancellationToken)` overload, existing no-token member preserved | `RabbitMqProbe.cs:201` is the no-token member's only caller, so preserving it leaves that file unedited; `RabbitMqConnectionOwner.cs:37` already uses the token-accepting overload. Q9 is satisfied either way |
| Collection name strings (rulings fix the type names only) | `nameof(PostgresCollection)` / `nameof(RabbitMqCollection)` on the definitions and on every consumer; **no holder file** | The type name becomes the one source for the string xUnit matches on, so definition and consumers cannot drift and a rename is a compile error. A holder file would need a Frozen-scope ruling for no gain (`brief.md`:33-41, :53). No new project, layer, or abstraction; the ruled type names are unchanged |
| Cached truncate as a field vs rebuilt per reset | built once at startup, cached | `brief.md` Approach 1 step 3: "Cache the model-derived TRUNCATE statement." Rebuilding per test would re-walk the model on every reset for no benefit |
| `DeleteAsync` overload used for teardown | the container's asynchronous disposal, called with no token | Q9 requires cleanup to run after cancellation; the existing code already calls it that way at `PostgresFixture.cs:27` |
| `RestartIdentity` for identity restarts | `RESTART IDENTITY` on the one truncate | US3 scenario 3 requires it; it is the only server feature that gives it, and it costs nothing over a plain truncate |
| Deleting the stale fixture comment (`RabbitMqFixture.cs:12`) | delete, do not rewrite | Q3 makes the sentence false; `AGENTS.md` forbids explanatory comments, so there is nothing to rewrite it into |
| Empty-database creation shape | no `TEMPLATE` clause, matching the existing private helper | R5 is satisfied by the absence of the clause, and `LamuFlixDbContextFactory.cs:44` already has the right shape. An async cancellable variant is the only addition |

**Two implementation checkpoints, recorded not escalated** (both in `checklists/requirements.md` Notes): the context helper's `CreateContext(PostgresFixture)` overload becomes unreferenced and must be left alone (AC8); and `Options.Create` resolves transitively, where if it somehow did not, the correct response is a §2.3 stop, not a `.csproj` edit (AC8). Design §2's `TestContext.Current` question is no longer a checkpoint — it is answered in both halves, one available and one not, and the answer is what fixes the sharing evidence's placement (Test Strategy evidence 1, `checklists/requirements.md` items 6-7).

## Test Strategy

The regression net is the **105 baseline integration tests** (recon-DEV-306:62), with every test name unchanged, plus the new fixture-behaviour tests. The 74 `[Fact]`s and 5 `[Theory]`s across the fifteen existing test classes become 105 cases; none is deleted, renamed, or weakened (AC6). Any difference in count is explained in the task note — there is no numeric threshold, and none is invented (Q10).

**The suite is built in green steps, not one red one.** The collection move lands first and the suite must be green on the **unchanged** image tags before either fixture is touched (brief task ordering item 2). That isolates the sharing change from the image change, so a failure afterwards is unambiguously one or the other — which is the only way R1 stays diagnosable.

**New fixture-behaviour tests.** `PostgresFixtureTests` (in `PostgresCollection`): applied migrations equal the known migrations immediately after startup, with no test-side `MigrateAsync`; the container identity this collection owns is emitted for the sharing evidence; after seeding a movie with an actor, a director and a genre, the reset leaves every application table and join table at zero rows with `__EFMigrationsHistory` intact; the first insert after each of two consecutive resets receives the same key; the database is still usable for insert and query after a reset. `RabbitMqFixtureTests` (in `RabbitMqCollection`): the exchange and three queues exist immediately after startup, checked by passive declare; the container identity this collection owns is emitted for the sharing evidence; a message published to the retry queue before the reset is in none of the three queues afterwards; publish and get work after a reset. Both use `[Fact]`, Shouldly, `TestContext.Current.CancellationToken`, and `Method_Scenario_Expectation` names, with the three AAA headers and no other comment (FR-025, constitution.md:293). Each collection's **second** identity-emitting case sits in one existing consumer of that collection, so the sharing evidence is genuinely cross-class (Test Strategy evidence 1).

**No full-suite green is asked for before the reset lands.** The same ordering applies inside the database half: moving eight consumers onto one shared migrated database (Q2) lands **before** the reset exists, so between those two tasks no test in that collection may see a green full-suite run — they would share rows with no reset, and a red run there would point at the wrong fix. The first full-suite green on `postgres:17-alpine` is therefore the task that also lands the reset, and the gate task before it observes only the catalog-contract test. Brief task ordering item 3 asks for the collection to be green at the **end** of that step, which is where this puts it.

The retry-queue row is race-free by construction: the reset **deletes** the queues rather than draining them, so the message dies with the queue whatever the 2 s retry TTL does. No polling is needed to observe the absence.

**No property test.** The only logic beyond the fixtures' control flow is the truncate-list derivation, and it is proved against a real server by the reset test rather than by a generator. The property gate is expected to report scope-empty and DEV-306's opt-out is recorded in its task note per `harness.yml:30-35`. **No mutation gate** — the change is test infrastructure with no `src/` code, so there is no production mutant to survive; that line is recorded rather than the gate being quietly skipped.

**Evidence to collect in the task note** (Q10, AC7, AC4, AC5, AC3, AC8):

1. **Sharing.** The mechanism is fixed, and it is fixed because the obvious one does not exist. **The two owned container identities are emitted from `[Fact]` bodies through `TestContext.Current.TestOutputHelper`**, one emitting case in `PostgresFixtureTests`, a second in `EfMovieRepositoryTests`, one in `RabbitMqFixtureTests`, and a second in `RabbitMqPublisherTests` — so each collection has **two distinct classes** emitting the identity of the fixture instance they received, which is what the criterion asks to be observed. Each emitting case reads the identity off the fixture's already-public container accessor (`fixture.Container.Id`, `IContainer.Id`) and writes it as one tagged line; no fixture member is added for it, because the accessor already is the one source. The evidence is those lines in the run's captured test output, read from `dotnet test --logger "console;verbosity=detailed"` or from the `--logger trx` file's `<Output><StdOut>` — **both are `dotnet test` command-line options, so no runner configuration file and no `.csproj` change is involved**.

   **Why not from the fixture's or the consumer's `InitializeAsync`, which is where the first draft put it.** `TestContext.Current.TestOutputHelper` is unusable in both places in the pinned xUnit. A collection fixture is created during the *test case's* initialisation, where `ITestContext.Test` is `null` and `ITestContext.TestOutputHelper` is documented as available only when `Test` is not null. A test class's `IAsyncLifetime.InitializeAsync` runs before `TestRunnerBase.OnTestStarting` calls `TestOutputHelper.Initialize(messageBus, test)`, and `TestOutputHelper.Write` guards on that not-yet-assigned state with `Guard.NotNull("There is no currently active test.", state)` — so the write throws rather than being silently dropped. `[CaptureConsole]` funnels into the same helper (`ConsoleCaptureTestOutputWriter` writes through `TestContextAccessor.Current.TestOutputHelper`) and inherits the same limitation. Assembly-scope `SendDiagnosticMessage` would work, but only if diagnostic messages are enabled, and under `xunit.runner.visualstudio` that means an `xunit.runner.json` or a `.runsettings` — a new file outside the Frozen scope **In** list, and therefore a `needs decision:` escalation rather than something this plan may assume. A `[Fact]` body is the one place the helper is contractually live, so that is where the emission goes.

   The evidence must show the same PostgreSQL identity emitted by at least two classes in `PostgresCollection` and the same RabbitMQ identity by at least two in `RabbitMqCollection`. Container starts are correlated to those two owned identities only — other same-image starts in the window belong to other runs and are not evidence for this one. No extra fixture or container is created to demonstrate sharing. The four emitting cases are new test cases, so AC6's "any change in count is explained" line covers them explicitly rather than leaving the difference unexplained.
2. **Teardown.** After the run, each of the two owned identities emitted in (1) is looked up **by identity**. A label sweep or an image sweep is not acceptable: a concurrent unrelated run on the same machine makes it meaningless in both directions. The identities used are the ones from the **final** full-suite run, not from an earlier phase's run, because an earlier run's containers were already gone and prove nothing about this one.
3. **Diagnostics.** Resolved image reference and content digest for both delivered tags, via `docker image inspect postgres:17-alpine rabbitmq:4-management-alpine --format …`. Server versions: `SHOW server_version` for PostgreSQL and the broker's `version` server property for RabbitMQ, captured by the fixture-behaviour tests through test output. Full-suite time against 1 m 17 s.
4. **Boundaries.** `git diff --stat 3b2e998...HEAD` restricted to the twenty-four files above — twenty-two that still exist plus the two deletions — and `rg -n "AssemblyFixture|IClassFixture<(PostgresFixture|RabbitMqFixture)>" tests/LamuFlix.IntegrationTests` returning **nothing**.

**If a new image breaks an existing contract test, that is a finding, not a failure to engineer around.** R1 names the two real risks: `postgres 16.4 → 17-alpine` can change the catalog output that `MigrationTests.cs:64-77` asserts against a live server, and `rabbitmq 4.0.0 → 4-management-alpine` is a floating minor that can change how quorum, at-least-once or reject-publish arguments are enforced. A failure is reported to Keel with the resolved versions and **nothing falls back to the old tags**. A genuine fixture or consumer integration defect may be fixed inside the ruled scope. A test may be corrected only when it is demonstrated to be wrong (TEST-WRONG), never weakened to make it pass.

## Gates

Run after every `.cs` edit (constitution.md:375, :380-388): `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, then `dotnet format --verify-no-changes`. A gate that cannot run is reported **"Could not run"**, never as a pass or a skip-green (`AGENTS.md`).

**`-Files` must be a real array.** A single comma-joined string silently collapses to one checked file, and a `pwsh -File` launch hands the array over as separate native arguments so only the first binds. The call operator is the reliable form:

```powershell
$changed = @(
  'tests/LamuFlix.Tests.Common/PostgresFixture.cs',
  'tests/LamuFlix.Tests.Common/RabbitMqFixture.cs',
  'tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs',
  'tests/LamuFlix.IntegrationTests/PostgresCollection.cs',
  'tests/LamuFlix.IntegrationTests/RabbitMqCollection.cs'
)
& ./scripts/run-roslyn-analyzers.ps1 -Files $changed
& ./scripts/run-cyclomatic-complexity.ps1 -Files $changed
& ./scripts/run-jetbrains-inspectcode.ps1 -Files $changed
```

### Close-out gates

One pass over **every** changed `.cs` file — the twenty-two that exist after the change; a deleted file cannot be analysed — with the complexity gate run twice:

```powershell
$all = $changed + @(
  'tests/LamuFlix.IntegrationTests/MovieCatalogSeed.cs',
  'tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs',
  'tests/LamuFlix.IntegrationTests/RabbitMqFixtureTests.cs'
) + @(
  'tests/LamuFlix.IntegrationTests/EfMovieCatalogBrowseTests.cs',
  'tests/LamuFlix.IntegrationTests/EfMovieCatalogDetailsTests.cs',
  'tests/LamuFlix.IntegrationTests/EfMovieCatalogPagingTests.cs',
  'tests/LamuFlix.IntegrationTests/EfMovieCatalogSortingTests.cs',
  'tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs',
  'tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs',
  'tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs',
  'tests/LamuFlix.IntegrationTests/PersistenceRoundTripTests.cs',
  'tests/LamuFlix.IntegrationTests/MigrationTests.cs',
  'tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs',
  'tests/LamuFlix.IntegrationTests/RabbitMqConnectionOwnerTests.cs',
  'tests/LamuFlix.IntegrationTests/RabbitMqHealthCheckTests.cs',
  'tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs',
  'tests/LamuFlix.IntegrationTests/RabbitMqTopologyTests.cs'
)                                   # 5 + 3 + 14 = 22
& ./scripts/run-roslyn-analyzers.ps1 -Files $all
& ./scripts/run-cyclomatic-complexity.ps1 -Files $all                  # ceiling 15
& ./scripts/run-cyclomatic-complexity.ps1 -Files $all -Threshold 6      # refactor ceiling
& ./scripts/run-jetbrains-inspectcode.ps1 -Files $all
```

Then, in order: full `dotnet test` green; `dotnet format --verify-no-changes` clean; the evidence capture above; `git diff --stat 3b2e998...HEAD` limited to the twenty-four files listed under Project Structure — the twenty-two that still exist plus the two deletions.

**Two gates deliberately not run, and why.** `./scripts/run-vulnerable-packages.ps1` is out of scope: `harness.yml:23-25` makes it materially relevant when the dependency graph changes, and nothing is added or removed here. `./scripts/run-property-tests.ps1` is expected to exit **2** (scope-empty); per `AGENTS.md` a scope-empty skip is blocking and never green, and per `harness.yml:30-35` a ticket with no domain invariants records its opt-out in its task note, which the pipeline then accepts **for this ticket only**. That note is a task. `pwsh -NoProfile -File ./scripts/run-mutation.ps1` is recorded as **not applicable**: no `src/` code changes, so Stryker's Core-only scope would report nothing about this work. No threshold moves to clear any of these, and no harness or `stryker-config.json` setting is edited.

## Coverage of the Brief's Closing Bar

| Closing-bar item | Where satisfied |
|---|---|
| AC1 — `postgres:17-alpine`, startup migration, connection string, clean reset | Design §2, §3; FR-004 to FR-012 |
| AC2 — `rabbitmq:4-management-alpine`, production topology at startup, existing connection contract | Design §4; FR-013 to FR-015 |
| AC3 — collection fixtures only; no `AssemblyFixture` or `IClassFixture` left | Design §1, §6; FR-001 to FR-003; boundaries evidence |
| AC4 — one container per run, shared by every class in its collection | Design §1; FR-002; Test Strategy evidence 1 (mechanism: T018, T022, T028, T031; read at T036 and re-confirmed at T038); SC-002 |
| AC5 — every container gone after the run, by exact identity | Design §2, §4; FR-020 to FR-022; Test Strategy evidence 2 (T037, against the identities T036 recorded); SC-006 |
| AC6 — no lost scenarios; new fixture tests pass | Test Strategy; FR-012, FR-023, FR-025, FR-026; SC-005 |
| AC7 — resolved images, server versions, timing against baseline | Test Strategy evidence 3; FR-027; SC-007 |
| AC8 — no `src/`, no props or csproj change, legacy untouched, factory semantics preserved | Project Structure; Design §6, §9; FR-028; SC-008 |
| AC9 — analyzer, complexity (15 then 6), InspectCode, format all exit 0 | Gates; FR-029; SC-009 |
