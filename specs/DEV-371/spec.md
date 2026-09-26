# DEV-371 — Retire EF Core InMemory and RabbitMQ Substitutes in Tests

**Feature**: Retire in-memory test doubles and replace them with containerized real services (Postgres, RabbitMQ) to improve test fidelity and eliminate false-positive test results.

**Status**: Phase A — grill closed; `gate1: provisional`. 0 owner checkboxes.

## Why This Matters

Tests currently use EF Core's InMemory provider and `NSubstitute` mocks for `IModel` (RabbitMQ channel abstraction). These test doubles do not exercise the actual database or message broker behavior, leading to tests passing locally but failing in production due to:

- InMemory database differences (no constraint validation, transaction semantics differ)
- Mocked `IModel` that never actually publishes or consumes messages
- No coverage of the constitution's target provider (Postgres via Npgsql); production still wires Pomelo/MySQL (LamuFlixContext.cs:46) until a separate cutover ticket

This ticket replaces the test doubles with containerized real services, ensuring tests validate actual system behavior.

## Acceptance Criteria

### From the Ticket

- **AC1:** No `Microsoft.EntityFrameworkCore.InMemory` reference remains under `tests/`. No `UseInMemoryDatabase` remains under `tests/`.
- **AC2:** No `Substitute.For<IModel>()` remains under `tests/`.
- **AC3:** The suite is green with Docker available, and the three static-analysis gates exit 0.

### Keel's Closing Bar (Patron Accepted)

- **AC4:** `Microsoft.EntityFrameworkCore.InMemory` is removed from `Directory.Packages.props` and from `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`.
- **AC5:** All 30 baseline tests pass against real containers (22 LamuFlix.Test + 8 ArchitectureTests).
  - Test names are unchanged.
  - Each assertion is equal to or stronger than the substitute assertion it replaces.
- **AC6:** All gates exit 0: Roslyn analyzers, cyclomatic complexity (15 implement / 6 refactor), InspectCode, vulnerable packages, format check.

### Patron's Additional Requirements

- **AC7:** `Testcontainers.PostgreSql`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `Testcontainers.RabbitMq` are registered in `Directory.Packages.props` with pinned versions. Only the consuming project references them.
- **AC8:** `LamuFlixContextFactory.CreateContext()` keeps its public static signature. `UnitTest1.cs` and `GenericRepositoryGetByIdTests.cs` have zero edits (proven by `git diff --stat`).
- **AC9:** Zero files under `src/` change (proven by `git diff`).
- **AC10:** No new project or folder, and no edit to `.github/workflows/ci.yml`. When Docker is absent, tests fail—they never skip.

## Scope & Technical Design

### Provider Decision (Q1 — Postgres)

- **EF Provider**: Postgres via `Npgsql.EntityFrameworkCore.PostgreSQL` (decided in constitution.md:295).
- **Test Provider**: Real Postgres instance managed by `Testcontainers.PostgreSql`.
- **Schema**: `Database.EnsureCreated()` from the model—no migrations (Pomelo migrations cannot run on Postgres).
- **Why Postgres in tests, not production Pomelo**: Constitution requires Postgres as the target provider. Tests exercise the constitution's provider, validating the eventual production cutover.

### Package Management (Q4, Q7)

- **Add to CPM** (`Directory.Packages.props`):
  - `Testcontainers.PostgreSql` (latest stable, EF Core 9.0 compatible, pinned version)
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (latest stable, EF Core 9.0 compatible, pinned version)
  - `Testcontainers.RabbitMq` (latest stable, pinned version)
- **Remove from CPM**: `Microsoft.EntityFrameworkCore.InMemory`.
- **Update** `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`: Reference the Postgres and RabbitMQ packages; add `RabbitMQ.Client` (already in CPM).

### Test Double Retirement (Q2, Q5, Q6)

#### Postgres Fixture (Q2, Q3)

- **Location**: `tests/LamuFlix.Tests.Common/`.
- **Implementation**: A shared `PostgreSqlContainer` in a holder class (xUnit-free), with an xUnit v3 `IAsyncLifetime` assembly fixture driving startup.
- **`CreateContext()` signature**: Unchanged (`public static LamuFlixContext CreateContext()`).
- **Behavior**:
  - Opens a fresh database within the shared container per test (e.g., `lamuflix_test_<guid>`).
  - Builds `DbContextOptions<LamuFlixContext>` using `UseNpgsql(<connection string>)`.
  - Constructs `new LamuFlixContext(options)`, calls `Database.EnsureCreated()`, and returns the context.
  - Bypasses the Pomelo `OnConfiguring` branch.
- **Lifecycle**: An xUnit v3 `IAsyncLifetime` class with `[assembly: AssemblyFixture(typeof(...))]` in EnrichmentTests.cs runs once before any test, ensuring the shared container starts before non-Enrichment test classes.
- **Q3 Unnamed Callers**: `UnitTest1.cs` and `GenericRepositoryGetByIdTests.cs` (7 total callers, unchanged) become Docker-backed. AC5 and AC3 require them green.

#### RabbitMQ Fixture (Q5, Q6)

- **Location**: `tests/LamuFlix.Tests.Common/`.
- **Implementation**: A shared `RabbitMqContainer` exposing a helper that opens a real `IConnection`/`IModel`.
- **Queue Isolation**: Each test owns test-specific queue names and purges `task_queue` and `task_queue_dlq` at start to avoid parallel-test interference.
- **Assertion Strategy** (Q6): Observable broker state, not `Received()` calls.
  - Each test publishes a real message and uses `BasicGet(autoAck:false)` to retrieve the real delivery tag.
  - Calls `ProcessMessageAsync(message, channel, tag)`.
  - Asserts ack: after processing, close the processing channel, then `QueueDeclarePassive(source).MessageCount == 0` on a fresh channel proves the ack.
  - Asserts retry: call `ConfirmSelect()` before processing and `WaitForConfirmsOrDie(TimeSpan.FromSeconds(5))` after; then `BasicGet` on `task_queue` returns the message with expected `RetryCount`.
  - Asserts DLQ: same confirmation flow; `BasicGet` on `task_queue_dlq` returns the message with `RetryCount ≥ 3`.

#### EnrichmentTests.cs (Q5, Q6)

- **5 Tests**: Lines 140, 180, 217, 255, 294 (all `Substitute.For<IModel>()` calls).
- **Changes**:
  - Declare unique queue names (e.g., test-specific routing keys).
  - Publish the input message to the real queue.
  - Call `BasicGet(autoAck:false)` to get a real delivery tag.
  - Call `ProcessMessageAsync(message, channel, tag)`.
  - Assert broker state (ack, retry, DLQ) per Q6.
  - Retain existing DB-state assertions.
- **Test Names**: Unchanged (line 308: `FilmesService_CriarFilme_...`).

### What's NOT Changing

- **Any `src/` file**: Including `LamuFlixContext.cs` (still wires Pomelo at line 46) and `EnrichmentJobProcessor.cs` (keeps `IModel`). Fixes needed for AC9 go to the owner as `blocked: structural`.
- **Production Postgres cutover**: A separate ticket (constitution.md:295).
- **RabbitMQ.Client 7.x `IChannel` migration**: A separate ticket (constitution.md:297).
- **NSubstitute package**: Stays; only `IModel` substitutes retire.
- **`UnitTest1.cs` and `GenericRepositoryGetByIdTests.cs`**: Zero edits.
- **`.github/workflows/ci.yml`**: No changes (Ubuntu CI already has Docker; Q9).
- **New projects or folders**: Tests.Common folder exists; no new folder created.

## Risk Conditions & Stop Gates

### R1 — Provider-Specific Model Configuration

**Status**: Verified provider-neutral (OnModelCreating :51-155, Sentry F7); retained as a stop condition.

**Risk**: `LamuFlixContext.OnModelCreating` or entity configurations carry MySQL-only column types or annotations (e.g., `[Collation(...)]`) that would fail `EnsureCreated()` on Postgres.

**Stop Condition**: If task 3 (Postgres fixture + `EnsureCreated()` test) fails for this reason:
- Stop implementation.
- Raise as `blocked: structural — provider-specific model annotations` to the owner.
- Fixing requires editing `src/LamuFlixContext.cs` or entity configs (AC9 violation; §2.3 #6).
- Never work around in test code.

### R2 — Docker Absent Locally

**Expectation**: Tests fail (per Q9); they never skip.

**Action**: Implementer reports Docker as unavailable; it is not a blocker.

### R3 — Files Outside Frozen Scope

**Rule**: Any file outside the Frozen Scope section must be raised as `blocked: structural — <file>` (§2.3 #6 or #1 for a package), never as `needs decision:`.

## Assumptions (Q10 — Taste)

- **Container image tags**: Pinned to explicit versions (e.g., `postgres:16.4`) rather than floating tags. The engine (Postgres) is decided; the tag string is taste ([PRODUCT.md](../PRODUCT.md) §4).
- **Container sharing**: One shared container per container type per test run (xUnit v3 assembly fixture), with isolation by fresh database / fresh queue per test. Container-per-test is rejected as slow. Taste ([PRODUCT.md](../PRODUCT.md) §4).
- **Package versions**: Latest stable versions compatible with EF Core 9.0.0 (pinned in CPM). Identities (Npgsql, Testcontainers) are decided; version numbers are taste.

See `ASSUMPTIONS.md` for full details.

## Gate 1 Readiness

- **Grill**: Closed. 0 owner checkboxes.
- **Gate 1**: `gate1: provisional` (Patron, 2026-09-26, task-pipeline Phase 2 step 7). Keel's plan challenge is adjudicated and the plan is frozen under D12 against this brief; scope preserved. Provisional only — the user decides at the spec PR.
- **Mutation Testing**: N/A (no `src/` changes). Line recorded in task note.
- **Property Tests**: Opt-out recorded (no tagged tests in scope). Gate exit 2 is accepted for this ticket only.

## Success Metrics

All ACs 1–10 passing, plus:
- All 30 baseline tests green with Docker available.
- All three static-analysis gates exit 0.
- `git diff` shows zero `src/` changes, zero new projects/folders.
- `git diff --stat` shows zero edits to `UnitTest1.cs` and `GenericRepositoryGetByIdTests.cs`.
- R1 stop condition either resolved or blocked: structural and escalated.
