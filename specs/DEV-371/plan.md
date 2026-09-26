# DEV-371 — Phase A Plan

**Authorized by**: Keel (grill closure, brief:brief.md)  
**Patron approval**: Q1–Q11, with no owner checkboxes.  
**Frozen against**: `brief.md` (Phase A grill outcome).

## Approach Overview

Replace test doubles (EF Core InMemory, RabbitMQ `IModel` substitutes) with containerized real services (Postgres, RabbitMQ). Maintain test names and assertion strength; add fixture infrastructure in Tests.Common.

## Implementation Strategy

### 1. Postgres Fixture (Q2, Q3)

**Goal**: Create a shared, xUnit v3 assembly fixture that starts a `PostgreSqlContainer` in `tests/LamuFlix.Tests.Common/`.

**Implementation**:
- A holder class in Tests.Common (xUnit-free) with `static Task StartAsync()` and `static ValueTask DisposeAsync()` methods.
- An xUnit v3 `IAsyncLifetime` class in `tests/LamuFlix.Test/EnrichmentTests.cs` with `[assembly: AssemblyFixture(typeof(...))]` that drives the lifecycle.
- The fixture runs once, before any test in the assembly, ensuring both containers start before non-Enrichment test classes.
- Static accessors throw `InvalidOperationException` naming Docker if read before start.

**`LamuFlixContextFactory.CreateContext()` signature**: Keep unchanged (public static, returns `LamuFlixContext`).

**Factory Logic**:
1. Access the started container via the holder's static accessor.
2. Generate a unique database name per call (e.g., `lamuflix_test_{guid}`).
3. Build a Postgres connection string to that database using `NpgsqlConnectionStringBuilder`.
4. Create `DbContextOptions<LamuFlixContext>` using `.UseNpgsql(<connection string>)`.
5. Call `new LamuFlixContext(options).Database.EnsureCreated()` to create the schema from the model.
6. Return the context instance.

**Why this works**:
- The explicit options bypass the production `OnConfiguring()` branch (which still wires Pomelo).
- `EnsureCreated()` creates schema on-the-fly; no migrations needed.
- Fresh database per test ensures isolation.
- Shared container amortizes startup cost.
- No `Lazy<>`, `SemaphoreSlim`, or sync-over-async anywhere.

**Lifecycle Guarantee**: The xUnit v3 assembly fixture runs before `UnitTest1` and `GenericRepositoryGetByIdTests`, so all 13 callers see a started container.

**Q3 Consequence**: All 13 unnamed callers (`UnitTest1.cs`, `GenericRepositoryGetByIdTests.cs`, and internal factory calls within `tests/`) now target this Docker-backed factory. These tests must pass (AC5, AC3).

### 2. RabbitMQ Fixture (Q5, Q6)

**Goal**: Create a shared `RabbitMqContainer` in `tests/LamuFlix.Tests.Common/`, coordinated by the same xUnit v3 assembly fixture that starts Postgres.

**Implementation**:
- Extend the holder class in Tests.Common with RabbitMQ container startup (same `StartAsync()` call for both containers).
- Expose a static accessor for the started container.
- Each test purges `task_queue` and `task_queue_dlq` at start to avoid cross-test pollution.

**Queue Isolation Strategy**:
- Tests use test-owned, uniquely named source queues or purge strategy (each test purges `task_queue` and `task_queue_dlq` at start).
- The processor owns the `task_queue` and `task_queue_dlq` queue names; each test starts with those queues empty.
- Tests running in parallel stay isolated via purge discipline.
- The 5 broker tests stay in one class; xUnit v3 serializes tests within a class by default.

**Channel Acquisition**:
- Each test calls the fixture accessor to get a started container.
- Opens a real `IConnection` via `new ConnectionFactory { Uri = new Uri(container.GetConnectionString()) }`.
- Opens a channel via `connection.CreateModel()` and closes both in cleanup.

### 3. EnrichmentTests.cs Conversion (Q5, Q6)

**Goal**: Replace all 5 `Substitute.For<IModel>()` calls with real `IModel` from the RabbitMQ fixture.

**Assertion Rewrite** (Q6 — Observable Broker State):

Replace `Received()` mock assertions with broker-state assertions:

**Per-Test Pattern**:
1. Declare a test-owned source queue with a unique name.
2. Publish the input message to the source queue.
3. Call `BasicGet(autoAck: false)` on the source queue to retrieve a real delivery tag.
4. Call `ProcessMessageAsync(message, channel, deliveryTag)`.
5. Assert broker state:
   - **Ack assertion**: Close the processing channel, then `QueueDeclarePassive(source).MessageCount == 0` on a fresh channel. An unacked message would be requeued on close; count of 0 proves the ack.
   - **Retry assertion**: Call `channel.ConfirmSelect()` before processing and `WaitForConfirmsOrDie(TimeSpan.FromSeconds(5))` after. Then `BasicGet` on `task_queue` must return the message, and its body carries the expected `RetryCount`.
   - **DLQ assertion**: Same confirmation flow; `BasicGet` on `task_queue_dlq` returns the message with `RetryCount ≥ 3`. No timing sleeps.
6. Retain existing DB-state assertions.

**Why this works**:
- `BasicGet()` fetches a real delivery tag from a real queue.
- `ProcessMessageAsync(message, channel, tag)` validates the real broker interaction.
- Observable state (queue emptiness, message counts) is the contract, not mocking side effects.

**Test Names**: Unchanged (e.g., `FilmesService_CriarFilme_...` remains as-is).

### 4. Retire InMemory (Q4)

**Goal**: Remove EF Core InMemory from CPM and test project dependencies.

**Changes**:
- Remove `Microsoft.EntityFrameworkCore.InMemory` line from `Directory.Packages.props`.
- Remove InMemory package reference from `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`.
- Update `tests/LamuFlix.Test/LamuFlix.Test.csproj` if needed (e.g., if it has a direct InMemory reference; unlikely).

**Verification** (AC1, AC4):
- `rg "Microsoft.EntityFrameworkCore.InMemory"` under `tests/` returns 0 matches.
- `rg "UseInMemoryDatabase"` under `tests/` returns 0 matches.
- `git diff` shows removal from both CPM and csproj.

### 5. Add New Packages to CPM (Q7)

**Goal**: Register Postgres and RabbitMQ packages in `Directory.Packages.props`.

**Changes**:
- Add `Testcontainers.PostgreSql` with pinned version (latest stable, EF Core 9.0 compatible).
- Add `Npgsql.EntityFrameworkCore.PostgreSQL` with pinned version (latest stable, EF Core 9.0 compatible).
- Add `Testcontainers.RabbitMq` with pinned version (latest stable).
- Ensure `RabbitMQ.Client` is already in CPM (likely already present; add if missing).

**Update** `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`:
- Add package references to the three new packages.
- Remove InMemory reference.

**Verification** (AC7):
- `Directory.Packages.props` contains all three packages with version numbers.
- Only `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` references them (not in `src/` or other test projects).

## Verification & Gates

### Proof Points

1. **AC1 (InMemory retired)**: `rg "Microsoft.EntityFrameworkCore.InMemory" -- tests/` and `rg "UseInMemoryDatabase" -- tests/` both return 0.
2. **AC2 (IModel substitutes retired)**: `rg "Substitute.For<IModel>" -- tests/` returns 0.
3. **AC5 (All 30 tests green)**: `dotnet test` with Docker available exits 0.
4. **AC8 (No edits to UnitTest1.cs / GenericRepositoryGetByIdTests.cs)**: `git diff --stat` shows zero lines changed in both files.
5. **AC9 (No src/ changes)**: `git diff -- src/` returns empty (no diffs).
6. **AC6 (Gates exit 0)**:
   - `./scripts/run-roslyn-analyzers.ps1` exits 0.
   - `./scripts/run-cyclomatic-complexity.ps1` (15 implement) and `.ps1 -Threshold 6` (refactor) both exit 0.
   - `./scripts/run-jetbrains-inspectcode.ps1` exits 0.
   - Vulnerable packages scan exits 0.
   - `dotnet format --verify-no-changes` exits 0.

### Test Coverage

- **Regression Suite**: The 30 existing tests (22 in LamuFlix.Test, 8 in ArchitectureTests) are the proof points.
- **No new test names** unless a fixture needs a smoke test (e.g., "Fixture_PostgresStart_Succeeds"). Such additions are named in `tasks.md` and constitute additional acceptance tests, not test-name changes.

### Stop Conditions

#### R1 — Provider-Specific Model Configuration

**Status**: Verified provider-neutral (OnModelCreating :51-155, Sentry F7); retained as a stop condition.

If task 3 (Postgres fixture + factory) fails with `EnsureCreated()` errors due to MySQL-specific column annotations:
- Stop implementation.
- Report to Keel: `blocked: structural — provider-specific model annotations in LamuFlixContext or entity configs`.
- This requires a `src/` edit to remove/adjust annotations (§2.3 #6, AC9 violation).
- Never work around in test code.

Example failure: `Collation()` attribute unsupported on Postgres, or a MySQL-specific type (e.g., `VARBINARY(256)` without a CLR type mapping on Postgres).

#### R2 — Docker Absent

Tests fail; they do not skip (Q9).

#### R3 — Out-of-Scope Edits

Any edit to `src/`, creation of new projects/folders, or changes to CI config raises `blocked: structural — <reason>`.

## Grill Constraints & Assumptions

- **0 owner checkboxes**: All decisions are made. No approval loops.
- **Taste assumptions** (Q10): Container image tags, container sharing (vs. container-per-test), package versions. Recorded in `ASSUMPTIONS.md`.
- **Mutation testing**: N/A (no `src/` changes). Line noted in task.
- **Property tests**: Opt-out (no tests tagged for property testing in this scope). Gate exit 2 is accepted.

## Next Steps

1. Keel challenges this plan against `brief.md` (plan-challenge gate).
2. Once approved, move to implementation (Phase B).
3. Each task is measured against ACs 1–10 and the three static-analysis gates.
