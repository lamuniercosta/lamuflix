# DEV-371 — Phase A Brief

Phase A grill outcome for DEV-371 (parent DEV-281, size M).
Decided by Patron in `ruling-DEV-371` (92 lines, Q1–Q11 with sources). Facts come from `recon-DEV-371` and the DEV-371 ticket note. Keel verified the cited constitution lines in `.specify/memory/constitution.md`: 263, 274–276, 295, 297, 303, 312, 316, and 349.
Grill questions: **11 asked (cap 12); 11/11 ruled**. Owner checkboxes: **0**.

## Closing bar

The ticket's own three ACs:

- **AC1 (ticket):** no `Microsoft.EntityFrameworkCore.InMemory` reference remains under `tests/`. No `UseInMemoryDatabase` remains under `tests/`.
- **AC2 (ticket):** no `Substitute.For<IModel>()` remains under `tests/`.
- **AC3 (ticket):** the suite is green with Docker available, and the three static-analysis gates exit 0.

Keel's closing-bar lines, which Patron accepted (Q11):

- **AC4:** `Microsoft.EntityFrameworkCore.InMemory` is removed from `Directory.Packages.props` and from `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`.
- **AC5:** all 30 baseline tests pass against real containers (recon:10: 22 LamuFlix.Test + 8 ArchitectureTests).
  - Test names are unchanged.
  - Each assertion is equal to or stronger than the substitute assertion it replaces.
- **AC6:** these gates exit 0: Roslyn analyzers; cyclomatic complexity (15 implement, 6 refactor); InspectCode; vulnerable packages; `dotnet format --verify-no-changes` (constitution.md:349).

Patron's added lines (Q11 a–d), which do not change delivery:

- **AC7 (a):** `Testcontainers.PostgreSql`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `Testcontainers.RabbitMq` are registered in `Directory.Packages.props` with pinned versions. Only the consuming project references them.
- **AC8 (b):** `LamuFlixContextFactory.CreateContext()` keeps its public static signature. `UnitTest1.cs` and `GenericRepositoryGetByIdTests.cs` have zero edits, proven by `git diff --stat`.
- **AC9 (c):** zero files under `src/` change, proven by `git diff`.
- **AC10 (d):** no new project or folder, and no edit to `.github/workflows/ci.yml`. When Docker is absent, tests fail. They never skip.

## Frozen scope

Files in scope:

- `Directory.Packages.props`: add 3 packages, remove InMemory. Ticket Scope §4.
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`: swap package references and add `RabbitMQ.Client` (an existing CPM entry). Inside the ticket-named folder.
- `tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs`: rewrite it onto a Postgres container. Ticket-named.
- New fixture file(s) under `tests/LamuFlix.Tests.Common/` (Postgres container, RabbitMQ container). Ticket-named folder, no new folder.
- `tests/LamuFlix.Test/EnrichmentTests.cs`: all 5 `IModel` substitutes (lines 140, 180, 217, 255, 294) move to a real channel. Ticket-named.
- `tests/LamuFlix.Test/LamuFlix.Test.csproj`: only if the fixture wiring needs it (xUnit v3 assembly fixture registration, or a project reference already present). No new packages here except those in AC7.
- `specs/DEV-371/*` artifacts.

Out of scope:

- Any `src/` file. This includes `LamuFlixContext.cs` (it still wires Pomelo at :46) and `EnrichmentJobProcessor.cs` (it keeps `IModel`).
- The production Postgres cutover and the single `Initial` migration (constitution.md:295). This is a separate ticket.
- The RabbitMQ.Client 7.x `IChannel` migration (constitution.md:297). This is a separate ticket.
- The NSubstitute package and every non-`IModel` substitute (Q8).
- `UnitTest1.cs` and `GenericRepositoryGetByIdTests.cs` (Q3).
- `ci.yml` (Q9).
- A `LamuFlix.IntegrationTests` project (constitution.md:263 names it; creating it is §2.3 #2; the ticket names Tests.Common).

## Round cap

- Review: 2 rounds maximum (standing §2.2). Remediation: at most 2 fix commits per round.

## Grill answers

- **Q1 — EF provider?** Postgres plus `Npgsql.EntityFrameworkCore.PostgreSQL`. This is decided, not a checkbox.
  - constitution.md:295 names Npgsql as the database provider.
  - constitution.md:274–276 forbid EF InMemory and require Testcontainers.
  - constitution.md:316 forbids Pomelo/MySQL.
  - The ticket names the Postgres fixture.
  - Recon fact for the board: production `OnConfiguring` still wires Pomelo (`LamuFlixContext.cs:46`). DEV-371 tests therefore exercise the constitution's target provider, not today's production driver.
- **Q2 — Schema creation?** `Database.EnsureCreated()` from the model, with no migrations. The Pomelo migrations cannot run on Postgres. Adding a Postgres migration would be §2.3 #3.
- **Q3 — Unnamed callers (13 total; recon:24-38)?** Permitted with no checkbox.
  - `CreateContext()` keeps its public static signature. It is backed by a lazily started shared Postgres container, with a fresh database per call.
  - `UnitTest1.cs` (3 callers, LocalPlay/Process.Start) and `GenericRepositoryGetByIdTests.cs` (4 callers) get zero edits. So neither §2.3 #5 nor #6 applies.
  - Consequence: those 7 tests become Docker-backed, and AC3/AC5 require them green.
- **Q4 — InMemory retirement?** Remove it from both CPM and `Tests.Common.csproj`. Sources: the ticket AC, constitution.md:276 and :316.
- **Q5 — RabbitMQ scope?** All 5 `IModel` substitutes go. There is no production signature change. `IModel` stays until the 7.x ticket.
- **Q6 — Assertion style?** Observable broker state is the contract, not `Received()` calls. A fabricated `deliveryTag` closes a real channel (recon:57-61).
  - Each test publishes a real message and uses `BasicGet` to get a real delivery tag.
  - Assert the ack: the queue is drained and unacked is 0.
  - Assert the retry: a republish to `task_queue` when RetryCount < 3.
  - Assert the DLQ: a forward to `task_queue_dlq` when RetryCount ≥ 3.
- **Q7 — Fixture home?** `tests/LamuFlix.Tests.Common`. There is no new project or folder. `RabbitMQ.Client` is an existing CPM entry.
- **Q8 — NSubstitute?** It stays, along with every non-`IModel` substitute (constitution.md:274, :303).
- **Q9 — Docker and CI?** No CI edits: ci.yml runs plain `dotnet test` on ubuntu-latest, which has Docker (ci.yml:30-31).
  - When Docker is absent, tests fail. They never skip.
  - If a Docker-less target ever arises, raise `blocked: structural — ci.yml edit`.
- **Q10 — Image pins and parallelism?** Taste, `[assumed]`. Quill logs both in `specs/DEV-371/ASSUMPTIONS.md`.
  - Image tags are pinned. The Postgres engine is decided by Q1; the tag string is taste.
  - There is one shared container per type per test run, with a fresh database or queue per test.
- **Q11 — Gates and closing bar?** See the Closing bar above.
  - Mutation: N/A, with the line recorded (no `src/` change).
  - Property tests: opt-out recorded in the task note per harness.yml:32-35. Gate exit 2 ("no tests tagged") is accepted for this ticket only.

## Plan decisions

- **Approach:**
  1. **Postgres fixture.** A shared, lazily started `PostgreSqlContainer` in Tests.Common.
     - `CreateContext()` builds `DbContextOptions<LamuFlixContext>` with `UseNpgsql(<connection string with a fresh unique database name>)`, uses the existing options constructor (`LamuFlixContext.cs:28`), and calls `EnsureCreated()`.
     - The explicit options mean the Pomelo `OnConfiguring` branch is not taken.
     - Container start is thread-safe (a `Lazy<>`/`SemaphoreSlim` guard) because xUnit runs classes in parallel.
     - A start failure throws. It never skips.
  2. **RabbitMQ fixture.** A shared `RabbitMqContainer` in Tests.Common exposes a helper that opens a real `IConnection`/`IModel`. Each test uses unique queue names or purges its queues, so parallel tests stay isolated.
  3. **EnrichmentTests.cs.**
     - Each of the 5 tests declares its queue, publishes the input message, and runs `BasicGet(autoAck:false)` to get a real delivery tag.
     - It then calls `ProcessMessageAsync(message, channel, tag)` and asserts broker state per Q6, plus the existing DB-state assertions.
     - Test names are unchanged. `FilmesService_CriarFilme_...` (line 308) changes only through the factory.
  4. **Retire InMemory.** Drop the package from CPM and from `Tests.Common.csproj`.
- **Test strategy:**
  - The existing 30 tests are the regression net. No new test names unless a fixture needs a smoke test; if one is added, it is named in `tasks.md`.
  - The proof points for AC1/AC2 and AC8/AC9 are `rg` greps and `git diff --stat` output, recorded in the task note.
- **Gate expectations:** AC6, plus the refactor gate at threshold 6. Mutation N/A and property-test opt-out lines are recorded.
- **Task ordering:**
  1. Run the pickup drift check against `main`.
  2. Add the CPM packages.
  3. Rewrite the Postgres fixture and factory. All 13 callers must go green before continuing; this proves EnsureCreated works on Postgres.
  4. Add the RabbitMQ fixture.
  5. Convert the 5 EnrichmentTests.
  6. Remove InMemory.
  7. Run the gates and collect the grep/diff proofs.

## Risks and stop conditions

- **R1 — Provider-specific model configuration.** Recon does not establish whether `LamuFlixContext.OnModelCreating` or the entity configurations carry MySQL-only column types or annotations that would make `EnsureCreated()` fail on Postgres. If step 3 fails for that reason, stop and report to Keel. Fixing it needs a `src/` edit, which breaks AC9 and is §2.3 #6, so it goes to the owner as `blocked: structural`. It is never worked around inside tests.
- **R2 — Docker absent locally.** Tests fail, per Q9. The implementer reports it; it is not a skip.
- **R3 — Any file outside Frozen scope.** Raise it as `blocked: structural — <file>` (§2.3 #6, or #1 for a package). Never as `needs decision:`.

## Plan-challenge round 1 decisions (Keel, 2026-09-26)

Sources: `findings-DEV-371-Sentry`, `findings-DEV-371-Compass`, and `findings-DEV-371-Ledger`. Keel verified each at file:line. These decisions add to the sections above.

- **D1 — Signature fact.** The frozen signature is `public static LamuFlixContext CreateContext()` (`tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs:9`). It returns the context, not options.
  - The new body builds Npgsql options, constructs `new LamuFlixContext(options)`, calls `Database.EnsureCreated()`, and returns that context.
  - All 13 callers consume the returned context, which is why AC8 holds.
- **D2 — Container lifecycle. This replaces the "`Lazy<>`/`SemaphoreSlim` guard" in Plan decisions 1–2.**
  - Tests.Common stays xUnit-free and gets no new package. It holds one static container holder class with:
    - `static Task StartAsync()`, which calls `StartAsync()` on both the `PostgreSqlContainer` and the `RabbitMqContainer`;
    - `static ValueTask DisposeAsync()`;
    - static accessors for the started containers.
  - An accessor that is read before start throws `InvalidOperationException` with a message naming Docker. It fails and never skips (Q9).
  - A small xUnit v3 `IAsyncLifetime` class registered with `[assembly: AssemblyFixture(typeof(...))]` drives the lifecycle. Both the class and the attribute live in the ticket-named `tests/LamuFlix.Test/EnrichmentTests.cs`. The fixture runs once, before any test in the assembly.
  - There is no `Lazy<>`, no `SemaphoreSlim`, and no sync-over-async (`.GetAwaiter().GetResult()`/`.Wait()`/`.Result`) anywhere. This resolves Ledger M1 and Sentry F4.
  - Proof: Task 3 runs all 13 callers green, which proves the assembly fixture starts before the non-Enrichment test classes.
  - If xUnit v3 does not create the assembly fixture before `UnitTest1`/`GenericRepositoryGetByIdTests` run, stop and report to Keel. Do not work around it: there is no sync start, and no edit to those files.
- **D3 — Connection strings come from the container.**
  - Postgres: `new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = "lamuflix_test_<guid-n>" }`.
  - RabbitMQ: `new ConnectionFactory { Uri = new Uri(container.GetConnectionString()) }`.
  - Never hardcode a host, port, user, or password (scrub rule).
- **D4 — Broker-state mechanics for Q6.**
  - Each test publishes its input to a **test-owned source queue** with a unique name, then calls `BasicGet(autoAck:false)` to get a real tag.
  - **Ack proof:** after processing, close the processing channel, then call `QueueDeclarePassive(source).MessageCount == 0` on a fresh channel. An unacked message would be requeued on close, so a count of 0 proves the ack. The artifact must not claim that an "unacked count" is readable.
  - **Retry/DLQ proof:** call `channel.ConfirmSelect()` before `ProcessMessageAsync`, then `WaitForConfirmsOrDie(TimeSpan.FromSeconds(5))` after it. Then `BasicGet` on `task_queue` / `task_queue_dlq` must return the message, and the body is asserted (RetryCount). This is deterministic, with no timing sleeps.
  - **Isolation:** the processor owns the retry and DLQ queue names, so each test purges `task_queue` and `task_queue_dlq` at its start (`QueuePurge`). The 5 broker tests stay in one class, and xUnit v3 serializes tests within a class. No other class touches RabbitMQ.
- **D5 — Scope proofs are base-anchored.**
  - `$base = git merge-base origin/main HEAD`. Every AC8/AC9/AC10 proof uses `git diff --name-only $base...HEAD -- <path>`, run on a clean working tree (`git status --short` empty apart from `specs/DEV-371/` before commit).
  - Paths are `tests/LamuFlix.Test/...` and `tests/LamuFlix.ArchitectureTests`.
  - AC1/AC2/AC4 greps run on the committed tree, with space-separated paths.
- **D6 — R1 cleared.** Sentry F7 and Compass verified that `LamuFlixContext.OnModelCreating` (`:51-155`) is provider-neutral: no `HasColumnType`, no `IEntityTypeConfiguration`, no DateTime columns, and string/int `HasData` seeds. R1 stays as a stop condition, but no problem is expected.
- **D7 — Accepted as information, no artifact change.**
  - Sentry F8: the LocalPlay tests become Docker-backed. Accepted per Q3.
  - Sentry F9: image pins are taste (Q10). Quill picks a currently supported RabbitMQ tag, not `3.13.0`, and records it in ASSUMPTIONS.md.

No new §2.3 item arises. D2 adds a class and an assembly attribute to the ticket-named `EnrichmentTests.cs`, and one holder file in ticket-named Tests.Common. It adds no package, project, folder, or unnamed-file edit. Owner checkboxes: still 0.

## Plan-challenge round 2 decisions (Keel, 2026-09-26)

Source: Keel's re-check of Quill's R1 revision (spec 3CC9C6AD…, plan 454FE84E…, tasks DCF28FEB…). These decisions add to D1–D7.

- **D8 — Declare before purge.** A `QueuePurge` or `QueueDeclarePassive` on a queue that does not exist raises a 404 channel exception and closes the channel. Swallowing that exception leaves a dead channel.
  - At the start of each broker test, call `QueueDeclare("task_queue", true, false, false, null)` and `QueueDeclare("task_queue_dlq", true, false, false, null)`. These arguments are identical to the processor's own declares (recon:59-60), so they cannot cause PRECONDITION_FAILED. Then call `QueuePurge` on both.
  - Declare the test-owned source queue with the same arguments (durable, non-exclusive, non-auto-delete). This avoids RabbitMQ 4.x's deprecated transient non-exclusive queues and lets a fresh channel see the queue. After the test, delete the source queue with `QueueDelete`.
  - There is no `try { } catch { }` around any broker call.
- **D9 — Test command.** `dotnet test` accepts one project or solution, so `dotnet test tests/LamuFlix.Test tests/LamuFlix.ArchitectureTests` fails with MSB1008. This was Keel's own wording in R1-03. The command is solution-level `dotnet test`, which covers both projects (the 30-test baseline, recon:10).
- **D10 — Fixture lands in Task 3 (restates D2 / R1-04).** Task 3 acceptance requires the 13 callers green. Only the assembly fixture starts the containers, so both the holder and the `IAsyncLifetime` fixture with its `[assembly: AssemblyFixture]` attribute are created in Task 3. Task 4 only verifies the fixture and the RabbitMQ accessor; it does not redefine them. The fixture is defined exactly once.
  - The xUnit v3 `IAsyncLifetime.InitializeAsync` returns `ValueTask`, so the fixture uses `ValueTask InitializeAsync() => new(ContainerFixture.StartAsync());`.
  - The `[assembly: …]` attribute goes after the `using` directives and before the file-scoped `namespace` (CS1730).
- **D11 — Lifecycle stop routing.** The xUnit fixture-order stop (D2) is `stop and report to Keel`. It is not `blocked: structural`, because it is not a §2.3 item. Keel decides whether it escalates.

No new §2.3 item. Owner checkboxes: still 0.

## Plan freeze (Keel, 2026-09-26, after round 2, cap reached)

Frozen: spec b34710af…, plan 43534ba5…, tasks bc700be1…, and ASSUMPTIONS 47951acf….

- **D12 — Precedence and errata.** Where the files disagree, brief.md (D1–D11) wins, then tasks.md, then spec.md/plan.md. The known stale lines are overridden as follows:
  - plan:51-56 and spec:77 ("parallel-test interference" / "parallel … purge discipline"): the D8 wording governs. Declare `task_queue`/`task_queue_dlq` with the processor's arguments, then purge. The 5 broker tests are serialized in one class.
  - plan:42 "internal factory calls within tests/": read it as `EnrichmentTests.cs` (6).
  - plan:112 "add if missing": RabbitMQ.Client is already in CPM at 6.8.1 (recon:79). Adding a package is never permitted (R3).
  - plan §4/§5 order: the tasks.md order governs (CPM add in Task 2, InMemory retire in Task 6).
  - tasks:171-172: base-anchor both proofs with `git diff --name-only $base...HEAD -- …` (`$base = git merge-base origin/main HEAD`).
  - tasks:80-108 holder sample: it is illustrative and must compile against the pinned versions.
  - tasks:114-115: the `using Testcontainers.*` lines in EnrichmentTests.cs are illustrative. Include only the usings the file needs; an unused using fails the analyzer gate.
  - tasks:204: Task 4 changes no file, so it gets no commit. The RabbitMQ holder parts are committed in Task 3.
  - `rabbitmq:4.0.0` stays a taste pin `[assumed]`. The implementer may use the current 4.x patch tag and log it in ASSUMPTIONS.md.
- None of these changes a decision, a file in scope, an AC, or the order.

## Gate 1 status

**gate1: provisional** — set by Patron 2026-09-26 (task-pipeline Phase 2 step 7). Phase A grill is closed. Owner checkboxes: **0**. **Plan frozen under D12** (see above); frozen scope preserved (spec b34710af…, plan 43534ba5…, tasks bc700be1…, ASSUMPTIONS 47951acf…). **Provisional only — the user decides at the spec PR.** The open drafting items go to Bernstein. Earlier history: round 1 revision re-checked: R1-01, R1-02, R1-03, R1-06, and R1-07 landed. R1-04, R1-05, R1-08, R1-09, R1-10, and R1-11 are partial or not done. Quill revises per round-2 fix list R2-01…R2-10 (DEV-371 note), then Keel re-checks. This is the last plan-challenge round inside the 2-round cap. Anything still open after it goes to Bernstein.
