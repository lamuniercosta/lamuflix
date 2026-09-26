# DEV-371 — Phase A Tasks

**Ordered by**: Brief.md (Plan decisions, §7).  
**Grill Closure**: 0 owner checkboxes. Patron accepted all Q1–Q11.  
**Review Cap**: 2 rounds maximum (standing §2.2). Max 2 fix commits per round.

## Task 1: Pickup Drift Check

**Goal**: Establish baseline; ensure the branch is current vs. `main`.

**Steps**:
1. Run `git fetch origin && git rebase origin/main` (or `git merge origin/main` if rebasing is disabled).
2. Verify no merge conflicts or rebase conflicts.
3. Run `dotnet test` to confirm baseline test suite passes before any changes.

**Acceptance**:
- No conflicts or drift.
- All existing tests pass with current infrastructure (InMemory still present).

**Commit**: None (informational only).

---

## Task 2: Add CPM Packages

**Goal**: Register Postgres and RabbitMQ packages in `Directory.Packages.props` with pinned versions.

**Files in Scope**:
- `Directory.Packages.props`
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`

**Steps**:

1. **Add to `Directory.Packages.props`**:
   - Add `Testcontainers.PostgreSql` (pinned version, latest stable compatible with EF Core 9.0).
   - Add `Npgsql.EntityFrameworkCore.PostgreSQL` (pinned version, latest stable for EF Core 9.0).
   - Add `Testcontainers.RabbitMq` (pinned version, latest stable).
   - Verify `RabbitMQ.Client` is already in CPM at 6.8.1 (recon:79); it is registered and no add is needed.
   - **Remove**: `Microsoft.EntityFrameworkCore.InMemory` (not now; deferred to Task 6).

2. **Update `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`**:
   - Add `<PackageReference Include="Testcontainers.PostgreSql" />` (no version; CPM manages it).
   - Add `<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />` (no version).
   - Add `<PackageReference Include="Testcontainers.RabbitMq" />` (no version).
   - Ensure `RabbitMQ.Client` is referenced.
   - **Do not remove InMemory here yet**; Task 6 retires it.

3. **Verify**:
   - `dotnet build` succeeds (build should resolve new packages from CPM).
   - No version conflicts; Roslyn analyzers find no dependency issues.

**Acceptance**:
- All three packages (plus RabbitMQ.Client) are in CPM with explicit pinned versions.
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` references all three.
- No other project references them (verified by `rg "Testcontainers\." -- tests/` filtered to project files).
- `dotnet build` succeeds.

**Commit Message**: `DEV-371 - Add Testcontainers and Npgsql packages to CPM`

---

## Task 3: Rewrite Postgres Fixture, Factory, and Create Assembly Fixture

**Goal**: Create a shared `PostgreSqlContainer`, update `LamuFlixContextFactory.CreateContext()` to use a fresh Postgres database per call, and wire the xUnit v3 assembly fixture.

**Files in Scope**:
- `tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs` (rewrite)
- New holder file (e.g., `tests/LamuFlix.Tests.Common/ContainerFixture.cs`)
- `tests/LamuFlix.Test/EnrichmentTests.cs` (add assembly fixture and attribute)

**Steps**:

1. **Create Holder Class in Tests.Common (xUnit-free)**:
   - Define a static class holding both `PostgreSqlContainer` and `RabbitMqContainer`.
   - Implement `static Task StartAsync()` that calls `StartAsync()` on both containers.
   - Implement `static ValueTask DisposeAsync()` that disposes both.
   - Expose static accessors (properties or methods) for each started container.
   - Accessors throw `InvalidOperationException` naming Docker if read before `StartAsync()` completes.
   - Example structure (excerpt):
     ```csharp
     public static class ContainerFixture
     {
         private static PostgreSqlContainer? PostgresContainer { get; set; }
         private static RabbitMqContainer? RabbitMqContainer { get; set; }
         
         public static async Task StartAsync()
         {
             PostgresContainer = new PostgreSqlBuilder()
                 .WithImage("postgres:16.4")
                 .Build();
             RabbitMqContainer = new RabbitMqBuilder()
                 .WithImage("rabbitmq:4.0.0")
                 .Build();
             await PostgresContainer.StartAsync();
             await RabbitMqContainer.StartAsync();
         }
         
         public static async ValueTask DisposeAsync()
         {
             if (PostgresContainer != null) await PostgresContainer.DisposeAsync();
             if (RabbitMqContainer != null) await RabbitMqContainer.DisposeAsync();
         }
         
         public static PostgreSqlContainer Postgres =>
             PostgresContainer ?? throw new InvalidOperationException("Docker not available; ContainerFixture.StartAsync() must run first");
         public static RabbitMqContainer RabbitMq =>
             RabbitMqContainer ?? throw new InvalidOperationException("Docker not available; ContainerFixture.StartAsync() must run first");
     }
     ```

2. **Create xUnit v3 Assembly Fixture in EnrichmentTests.cs** (illustrative; must compile against the pinned package version):
   - At the top of `tests/LamuFlix.Test/EnrichmentTests.cs`, after the `using` directives and before the file-scoped namespace, add the fixture class and attribute (illustrative; must compile against the pinned package version):
     ```csharp
     using Testcontainers.PostgreSql;
     using Testcontainers.RabbitMq;
     using Xunit;
     
     [assembly: AssemblyFixture(typeof(ContainerFixtureForTests))]
     
     namespace LamuFlix.Test;
     
     public class ContainerFixtureForTests : IAsyncLifetime
     {
         public ValueTask InitializeAsync() => new(ContainerFixture.StartAsync());
         public async ValueTask DisposeAsync() => await ContainerFixture.DisposeAsync();
     }
     ```
   - The attribute must come after `using` and before the `namespace` (CS1730).
   - The attribute ensures the fixture runs once before any test in the assembly.
   - The fixture is defined exactly once here; Task 4 only verifies it.

3. **Rewrite `LamuFlixContextFactory.CreateContext()`** (illustrative; must compile against the pinned package version):
   - Keep signature: `public static LamuFlixContext CreateContext()`.
   - Steps:
     1. Get the started container via `ContainerFixture.Postgres`.
     2. Generate a unique database name (e.g., `lamuflix_test_{Guid.NewGuid()}`).
     3. Build `NpgsqlConnectionStringBuilder` with the unique database name.
     4. Create `DbContextOptions<LamuFlixContext>` via `new DbContextOptionsBuilder<LamuFlixContext>().UseNpgsql(<connection string>).Options`.
     5. **Do not call `LamuFlixContext.OnConfiguring()`**; the explicit options override it.
     6. Call `new LamuFlixContext(options).Database.EnsureCreated()`.
     7. Return the context instance (not the options).
   - Example:
     ```csharp
     public static LamuFlixContext CreateContext()
     {
         var container = ContainerFixture.Postgres;
         var dbName = $"lamuflix_test_{Guid.NewGuid():N}";
         var connStr = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) 
         { 
             Database = dbName 
         }.ConnectionString;
         var options = new DbContextOptionsBuilder<LamuFlixContext>()
             .UseNpgsql(connStr)
             .Options;
         var context = new LamuFlixContext(options);
         context.Database.EnsureCreated();
         return context;
     }
     ```

3. **Test All 13 Callers**:
   - Run `dotnet test` (solution-level) to confirm all 30 tests pass.
   - Callers include: `UnitTest1.cs` (3 calls), `GenericRepositoryGetByIdTests.cs` (4 calls), and EnrichmentTests.cs (6 calls).
   - **This proves `EnsureCreated()` works on Postgres and the assembly fixture starts before non-Enrichment classes** (R1 and R1b validation).
   - If `EnsureCreated()` fails due to MySQL-specific model annotations, stop and report to Keel (`blocked: structural — provider-specific model annotations`).
   - If the assembly fixture does not run before `UnitTest1` and `GenericRepositoryGetByIdTests` classes, stop and report to Keel. Do not work around.

**Acceptance**:
- All 13 callers pass their tests (all 30 baseline tests green).
- The assembly fixture is created in EnrichmentTests.cs and runs before non-Enrichment test classes (proven by all 30 tests passing).
- `git diff -- src/` is empty (no production code changed, only test fixture).
- The signature line `public static LamuFlixContext CreateContext()` is unchanged in `git diff tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs`.

**Commit Message**: `DEV-371 - Create Postgres container fixture and update factory for real database tests`

---

## Task 4: Verify RabbitMQ Accessor and Fixture Lifecycle

**Goal**: Verify the RabbitMQ container accessor is accessible and that the assembly fixture from Task 3 starts both containers before any test.

**Files in Scope**:
- `tests/LamuFlix.Tests.Common/ContainerFixture.cs` (verify only; no new definitions)
- `tests/LamuFlix.Test/EnrichmentTests.cs` (fixture already created in Task 3)

**Steps**:

1. **Verify ContainerFixture Accessors**:
   - The holder class from Task 3 already has both `Postgres` and `RabbitMq` accessors.
   - Both throw `InvalidOperationException` if read before `StartAsync()` completes.
   - No new definitions here; Task 3 is complete.

2. **Verify Assembly Fixture Lifecycle**:
   - The xUnit v3 assembly fixture from Task 3 is wired in EnrichmentTests.cs.
   - Run `dotnet test` (solution-level) to confirm both containers start and all 30 tests pass.
   - This proves the fixture runs before non-Enrichment test classes and both containers are available.

**Acceptance**:
- Both container accessors are accessible from tests.
- `dotnet build` succeeds.
- All 30 tests pass with both containers running.
- The fixture defined in Task 3 is the only definition (not redefined here).

**Commit Message**: `DEV-371 - Create RabbitMQ container fixture with connection helper`

---

## Task 5: Convert EnrichmentTests.cs

**Goal**: Replace all 5 `Substitute.For<IModel>()` calls with real broker interactions.

**Files in Scope**:
- `tests/LamuFlix.Test/EnrichmentTests.cs` (lines 140, 180, 217, 255, 294; assembly fixture already in place from Task 3)

**Steps**:

1. **Identify all 5 `IModel` substitutes**:
   - Use `rg "Substitute.For<IModel>" -- tests/LamuFlix.Test/EnrichmentTests.cs` to locate exact lines.

2. **For each test** (illustrative; must compile against the pinned RabbitMQ.Client version):
   - **At test start, declare and purge queues** (code sample):
     ```csharp
     var connection = new ConnectionFactory 
     { 
         Uri = new Uri(ContainerFixture.RabbitMq.GetConnectionString()) 
     }.CreateConnection();
     var channel = connection.CreateModel();
     // Declare the processor's queues with the same arguments as the processor (D8)
     channel.QueueDeclare("task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
     channel.QueueDeclare("task_queue_dlq", durable: true, exclusive: false, autoDelete: false, arguments: null);
     // Purge both to ensure this test starts clean
     channel.QueuePurge("task_queue");
     channel.QueuePurge("task_queue_dlq");
     ```
   - **Declare test-owned source queue** (code sample):
     ```csharp
     var sourceQueue = $"source_{Guid.NewGuid():N}";
     channel.QueueDeclare(sourceQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
     ```
   - **Publish the input message** (e.g., `channel.BasicPublish(...)`).
   - **Get the real delivery tag** (code sample):
     ```csharp
     var basicGetResult = channel.BasicGet(sourceQueue, autoAck: false);
     if (basicGetResult == null) throw new InvalidOperationException("Message not found");
     var deliveryTag = basicGetResult.DeliveryTag;
     ```
   - **Call ProcessMessageAsync**:
     ```csharp
     await ProcessMessageAsync(message, channel, deliveryTag);
     ```
   - **Assert broker state** (D4 / Q6):
     - **Ack proof**: Close the processing channel, then `QueueDeclarePassive(sourceQueue).MessageCount == 0` on a fresh channel. Expected: 0 (message acked and consumed).
     - **Retry proof**: Before processing, call `channel.ConfirmSelect()`. After processing, call `WaitForConfirmsOrDie(TimeSpan.FromSeconds(5))`. Then `BasicGet("task_queue") != null` and its body has expected `RetryCount`. No sleeps.
     - **DLQ proof**: Same confirmation flow; `BasicGet("task_queue_dlq") != null` and `RetryCount >= 3`.
   - **Retain existing DB-state assertions** (unchanged).
   - **Cleanup**: Call `channel.QueueDelete(sourceQueue)`, then `channel.Close()` and `connection.Close()` in `finally` or `using` statements.

3. **Verify Test Names**:
   - Test names unchanged (e.g., `FilmesService_CriarFilme_...` remains).
   - Proven by `git diff -- tests/LamuFlix.Test/EnrichmentTests.cs` showing no deleted method signatures.

4. **Run Tests**:
   - `dotnet test` (solution-level) to confirm all 5 tests pass with real broker, and all 30 baseline tests remain green.

**Acceptance**:
- All 5 tests pass with real RabbitMQ broker.
- Test names are unchanged.
- Each assertion is deterministic (no timing sleeps) and observable (broker state, not mocks).
- Existing DB-state assertions intact.
- The assembly fixture runs before non-Enrichment test classes (proven by all 30 tests passing).

**Commit Message**: `DEV-371 - Convert EnrichmentTests to use real RabbitMQ channel instead of substitutes`

---

## Task 6: Retire InMemory Package

**Goal**: Remove `Microsoft.EntityFrameworkCore.InMemory` from CPM and test projects.

**Files in Scope**:
- `Directory.Packages.props`
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`
- Any other test `.csproj` files with InMemory references (likely none besides Tests.Common)

**Steps**:

1. **Remove from `Directory.Packages.props`**:
   - Delete the `Microsoft.EntityFrameworkCore.InMemory` package entry.

2. **Remove from `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`**:
   - Delete the `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />` entry.

3. **Verify Removal**:
   - `rg "Microsoft.EntityFrameworkCore.InMemory" -- .` (entire repo) returns only out-of-scope artifacts (docs, if any).
   - `rg "UseInMemoryDatabase" -- tests/` returns 0 matches.
   - `dotnet build` succeeds.

4. **Run Tests**:
   - `dotnet test` with Docker available (all 30 tests still pass via Postgres fixture).

**Acceptance**:
- AC1: No InMemory references remain under `tests/`.
- AC4: Removed from CPM and Tests.Common.csproj.
- Build succeeds.
- All tests still pass (relying on Postgres fixture, not InMemory).

**Commit Message**: `DEV-371 - Remove Microsoft.EntityFrameworkCore.InMemory from tests`

---

## Task 7: Run Gates and Collect Proofs

**Goal**: Verify all ACs 1–10 and collect proof artifacts for the task note.

**Files in Scope**:
- None (verification only).

**Steps**:

1. **Static-Analysis Gates** (AC6):
   - Run `./scripts/run-roslyn-analyzers.ps1` → must exit 0.
   - Run `./scripts/run-cyclomatic-complexity.ps1` (threshold 15) → must exit 0 (implement).
   - Run `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (refactor gate) → must exit 0 (refactor).
   - Run `./scripts/run-jetbrains-inspectcode.ps1` → must exit 0.
   - Run vulnerable packages scan (if configured) → must exit 0.
   - Run `dotnet format --verify-no-changes` → must exit 0.

2. **Test Suite** (AC3, AC5):
   - Run `dotnet test` with Docker available → all tests pass.
   - Confirm all 30 baseline tests pass (22 LamuFlix.Test + 8 ArchitectureTests).

3. **Proof Collections** (AC1, AC2, AC4, AC8, AC9, AC10) — Run on a clean tree after final commit:

   Establish the base:
   ```powershell
   $base = git merge-base origin/main HEAD
   ```

   **AC1 proof** (no InMemory):
   ```powershell
   rg "Microsoft.EntityFrameworkCore.InMemory" -- tests/
   rg "UseInMemoryDatabase" -- tests/
   ```
   Expected: 0 matches total.

   **AC2 proof** (no IModel substitutes):
   ```powershell
   rg "Substitute.For<IModel>" -- tests/
   ```
   Expected: 0 matches.

   **AC4 proof** (InMemory removed from CPM and csproj):
   ```powershell
   rg "Microsoft.EntityFrameworkCore.InMemory" -- Directory.Packages.props tests/LamuFlix.Tests.Common/
   ```
   Expected: 0 matches.

   **AC8 proof** (UnitTest1.cs and GenericRepositoryGetByIdTests.cs unchanged):
   ```powershell
   Test-Path tests/LamuFlix.Test/UnitTest1.cs
   Test-Path tests/LamuFlix.Test/GenericRepositoryGetByIdTests.cs
   git diff --name-only $base...HEAD -- tests/LamuFlix.Test/UnitTest1.cs tests/LamuFlix.Test/GenericRepositoryGetByIdTests.cs
   ```
   Expected: Both files exist, and the diff is empty (no changes).

   **AC9 proof** (no src/ changes):
   ```powershell
   git diff --name-only $base...HEAD -- src/
   ```
   Expected: Empty (no src/ files touched).

   **AC10 proof** (no new projects/folders, no CI changes):
   ```powershell
   git diff --name-only --diff-filter=A $base...HEAD | Where-Object { 
       $_ -notlike "specs/DEV-371/*" -and 
       $_ -notlike "tests/LamuFlix.Tests.Common/*.cs"
   }
   git diff --name-only $base...HEAD -- .github/workflows/ci.yml
   ```
   Expected: No new files outside `specs/DEV-371/` and `tests/LamuFlix.Tests.Common/*.cs`; ci.yml unchanged.

4. **Record Results in Task Note**:
   - Paste all gate outputs into the task note.
   - Paste all proof grep/diff outputs.
   - Record mutation testing as N/A with reason: "No src/ changes per AC9".
   - Record property-test opt-out per harness.yml:32-35, with gate exit 2 acceptance note.

**Acceptance**:
- All gates exit 0 (AC6).
- All proofs collected and recorded in task note.
- Mutation: N/A (documented).
- Property tests: Opt-out (documented).
- Ready for Keel's plan challenge and Gate 1 review.

**Commit Message**: None (verification only; results recorded in task note).

---

## Summary

| Task | Depends On | Proof |
|------|-----------|-------|
| 1. Drift Check | None | No conflicts; baseline tests pass |
| 2. Add CPM Packages | 1 | `dotnet build` succeeds; packages in CPM |
| 3. Postgres Fixture, Factory & Assembly Fixture | 2 | All 30 tests pass with Postgres and assembly fixture; signature unchanged |
| 4. Verify RabbitMQ & Fixture Lifecycle | 3 | Fixture verified; both containers started; 30 tests pass |
| 5. Convert EnrichmentTests | 4 | All 5 tests pass with real broker; queues declared before purge; no sleeps |
| 6. Retire InMemory | 5 | No InMemory references remain; build succeeds |
| 7. Gates & Proofs | 6 | All gates exit 0; all proofs collected |

## Assumptions (Q10 — Taste)

- Container image tags are pinned (e.g., `postgres:16.4`, `rabbitmq:4.0.0`), a currently supported stable version.
- One shared container per type per run (assembly fixture), with fresh DB/queue per test.
- Package versions: Latest stable compatible with EF Core 9.0.0.

See `ASSUMPTIONS.md` for full details.

## Stop Conditions

- **R1 — Provider-Specific Model Configuration** (verified provider-neutral; Sentry F7 checked OnModelCreating :51-155): If Task 3 fails due to `EnsureCreated()` incompatibility on Postgres (e.g., MySQL-only column types or annotations), stop and report to Keel. Fixing requires a `src/` edit (AC9 violation; `blocked: structural`).
- **R1b — xUnit v3 Assembly Fixture Lifecycle**: If Task 3 finds that the assembly fixture does not run before `UnitTest1` and `GenericRepositoryGetByIdTests` classes, stop and report to Keel. Do not work around. (Not a §2.3 item; Keel decides routing.)
- **R2 — Docker Absent**: Tests fail (not skip). Implementer reports unavailability; not a blocker.
- **R3 — Out-of-Scope Edits**: Any edit outside Frozen Scope raises `blocked: structural — <file>`.

## Review & Remediation Cap

- **Review rounds**: Max 2 (standing §2.2).
- **Remediation commits per round**: Max 2.
- Example flow: Task 7 gates fail → Round 1 fix commit → Re-run gates → Round 2 fix commit → Pass.
