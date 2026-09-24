# DEV-280 Specification — Migrate test suite from MSTest to xUnit v3 and NSubstitute

**Status:** draft  
**Author:** Quill (Drafter)  
**Based on:** brief.md, CONCLUSIONS.md (Q1–Q5, D1–D4), post-DEV-290 paths  

---

## Functional Requirements (FRs)

### FR-001: Replace MSTest test framework with xUnit v3

The test framework must be fully migrated from MSTest (`MSTest.TestAdapter`, `MSTest.TestFramework`) to xUnit v3 (`xunit.v3`, `xunit.runner.visualstudio`). This includes:

- Remove all MSTest attributes (`[TestClass]`, `[TestMethod]`, `[TestInitialize]`, `[Ignore]`).
- Convert test classes to plain C# classes (no base class required).
- Convert `[TestMethod]` to `[Fact]`.
- Acceptance: `dotnet test` discovers and runs every test through xUnit v3, achieving **18 passed, 0 skipped, 0 failed** (WorkerTests 5 + EnrichmentTests 10 + UnitTest1 migrated 3).

### FR-002: Replace Moq with NSubstitute for test substitutes

All test doubles must use NSubstitute instead of Moq:

- Remove `Moq` package and all `using Moq` statements.
- Replace `new Mock<T>()` with `Substitute.For<T>()`.
- Replace `.Setup(x => …).Returns(v)` with `x.Method(…).Returns(v)`.
- Replace `.Verify(…, Times.Once)` with `.Received(1).Method(…)`.
- Replace `It.IsAny<T>()` with `Arg.Any<T>()`.
- Preserve behavior semantics: same expected return values, same verification counts.

### FR-003: Use Shouldly for assertions, xUnit Assert.Collection only

Assertions must use Shouldly; no other xUnit assertions allowed:

- Convert all `Assert.AreEqual`, `Assert.IsTrue`, `Assert.IsNull`, etc. to Shouldly equivalents (`ShouldBe`, `ShouldBeTrue`, `ShouldBeNull`, etc.).
- Convert `Assert.ThrowsException<T>` to `Should.Throw<T>` / `Should.ThrowAsync<T>`.
- The only allowed xUnit `Assert.` member is `Assert.Collection`.
- Acceptance: `git grep "Assert\."` returns only `Assert.Collection` in `tests/`.

### FR-004: Remove disk-bound tests and disk I/O from test code

Four tests in `UnitTest1.cs` that depend on disk access must be deleted entirely: `TestMethod1`, `TestMethod2`, `TestMethod4`, `TestMethod5` (all `[Ignore]` marked). Additionally:

- No test performs disk I/O (no `File.` or `Directory.` operations).
- No literal `F:/Filmes` or `F:\Filmes` path in test code. The exception is pattern-scoped: inert drive-prefixed `Location` data is allowed (it never touches disk), e.g. the six bare-F EnrichmentTests `Location` strings (cite by test member name) and the two retained UnitTest1 Filmes literals (rewritten). **Do not** grep bare `F:`.
- No `Process.Start` token in `tests/` (retained tests use injected `ProcessStarter` delegate).
- Acceptance: grep for `F:/Filmes` and `F:\Filmes` returns nothing in `tests/`; `git grep "Process.Start" -- tests/` is empty.

### FR-005: Create tests/LamuFlix.Tests.Common/ for shared test helpers

A new class library must be created at `tests/LamuFlix.Tests.Common/` containing only helpers the migrated tests already share:

- Content: one EF Core in-memory `LamuFlixContext` factory, merging `UnitTest1.cs:490 CreateInMemoryContext` and `EnrichmentTests.cs:109 CreateInMemoryDb`.
- No speculative builders or AutoFixture customizations unless a migrated test needs one.
- ProjectReference from `tests/LamuFlix.Test/LamuFlix.Test.csproj` to `Tests.Common`.
- References: `ProjectReference` to the Data project (for `LamuFlixContext`); `PackageReference` to `Microsoft.EntityFrameworkCore.InMemory`.
- Not added to `LamuFlix.sln` (builds as a dependency of `LamuFlix.Test`).

### FR-006: Add ticket-named test packages to Directory.Packages.props (CPM)

Six new packages must be added to `Directory.Packages.props` as `<PackageVersion>` entries using Central Package Management:

- `xunit.v3` 4.0.1
- `xunit.runner.visualstudio` 4.0.0
- `NSubstitute` 6.2.0
- `Shouldly` 4.3.0
- `AutoFixture` 4.18.1
- `Faker.Net` 2.0.163

A project references a package only if its own code consumes it (declaration rule, D2). Excluded: `AutoFixture.Xunit3`, `NSubstitute.Analyzers` (not ticket-named, would violate §2.3 item 1).

### FR-007: Migrate WorkerTests and EnrichmentTests to xUnit v3 + NSubstitute + Shouldly

Two test files must be migrated as faithful 1:1 translations:

- `WorkerTests.cs`: 5 tests, all migrated.
- `EnrichmentTests.cs`: 10 tests, all migrated. The EF Core in-memory factory is extracted to `Tests.Common`.
- Preserved: pre-existing EF Core InMemory contexts and RabbitMQ `IModel` mocks (carried debt, translated but not fixed or extended).
- Acceptance: both files build and run under xUnit v3; assertions keep their original meaning and expected values.

### FR-008: Migrate or delete FilmesService tests in UnitTest1.cs — **DECISION REQUIRED (D4)**

**STOP and report:** The ticket does not name the 3 `AssistirFilme_*` tests in `UnitTest1.cs` (`:498`, `:517`, `:560`). Implementation cannot proceed until the owner makes an explicit choice and ticks **Checkpoint 1** below:

**Option A (Recommended):** Migrate the 3 tests, preserving:
- All assertions on `ProcessStartInfo` (`UseShellExecute`, `FileName`, `ArgumentList`).
- The injected `ProcessStarter` delegate seam (hand-fake setter, never Mock of ProcessStarter).
- The `Features:LocalPlay` configuration (false / true) they set.
- Production code (`FilmesService`, `Features:LocalPlay` gate) unchanged.
- Test count at completion: **18 passed** (confirmed by AC1).

**Option B (Not executable under frozen plan):** Not executable without Patron plan amendment. If chosen, the 3 tests are **not deleted** until plan is formally amended to lock alternative requirements (passed count would drop to 15; requires decision audit before edit begins). The 3 tests remain in code until plan amendment is complete.

**Checkpoint 1 — FR-008 (Owner decision):**
- [ ] **I choose Option A: migrate the 3 AssistirFilme tests** (execute directly under this plan).
- [ ] **I choose Option B: delete the 3 AssistirFilme tests** (STOP — do not edit; report to Patron; requires plan amendment before edit).

*Gate 1 remains closed until Checkpoint 1 is ticked. Spec is provisional until decision is made.*

### FR-009: Remove MSTest and Moq packages

Both packages and their `PackageReference` and `PackageVersion` entries must be removed from `LamuFlix.Test.csproj` and `Directory.Packages.props`:

- Remove `PackageReference` entries (3 total: MSTest.TestAdapter, MSTest.TestFramework, Moq).
- Remove `PackageVersion` entries from `Directory.Packages.props` (3 total: same packages).
- Remove `using` statements for these packages from all `.cs` files in `tests/`.
- Kept packages (PackageVersion): `Microsoft.NET.Test.Sdk`, `Microsoft.EntityFrameworkCore.InMemory` (in Tests.Common; LamuFlix.Test reference decided in T010 per D2), `RabbitMQ.Client`.
- Acceptance: zero MSTest and zero Moq references remain anywhere; `git grep` over `*.cs`, `*.csproj`, `*.props`, `*.sln` returns nothing.

### FR-010: Update LamuFlix.Test.csproj for xUnit v3

The test project must be configured for xUnit v3 execution:

- Set `<OutputType>Exe</OutputType>` unconditionally (required for xUnit v3 runner compatibility).
- Add `<PackageReference>` entries only for packages the project consumes.
- Remove MSTest + Moq `<PackageReference>` entries.
- Acceptance: `dotnet test` runs via xUnit runner; no build errors; test output reports xUnit v3 framework.

---

## System/Acceptance Criteria (SCs)

| AC# | Criterion | Acceptance Evidence |
|---|---|---|
| **AC1** | **Test discovery and execution:** `dotnet test` via xUnit v3 discovers and runs **18 passed, 0 skipped, 0 failed** | `dotnet test` summary output. 18 = WorkerTests 5 + EnrichmentTests 10 + UnitTest1 3 migrated. Baseline at `450e70f`: 18 passed / 4 skipped / 0 failed (the 4 skips are the deleted tests). |
| **AC2** | **Zero MSTest and Moq:** No `PackageReference` / `PackageVersion` for MSTest.* or Moq; no `using Microsoft.VisualStudio.TestTools.UnitTesting`; no `using Moq`; no `Mock<` or `[TestMethod]` / `[TestClass]` / `[TestInitialize]` / `[Ignore]` | `git grep` over `*.cs`, `*.csproj`, `*.props`, `*.sln` for MSTest and Moq tokens returns empty |
| **AC3** | **No disk I/O in tests:** No `File.` or `Directory.` operations; the ban is exactly the two Filmes literal forms, `F:/Filmes` and `F:\Filmes`: zero hits under `tests/`. Pattern-scoped exception: inert drive-prefixed `Location` data is allowed (it never touches disk), e.g. the six bare-F EnrichmentTests `Location` strings and the two retained UnitTest1 Filmes literals (rewritten). **Do not** grep bare `F:`. | `git grep "F:/Filmes" -- tests/ *.csproj *.props *.sln` returns empty; `git grep -F "F:\Filmes" -- tests/ *.csproj *.props *.sln` returns empty (`-F` is required: the bare form is regex-escaped and matches nothing, a false green); code review for `File.` / `Directory.` in `tests/` |
| **AC4** | **No Process.Start in tests:** Retained tests use injected `ProcessStarter` delegate only | `git grep "Process.Start" -- tests/` is empty |
| **AC5** | **Shouldly assertions, Assert.Collection only:** All assertions use Shouldly; the only xUnit `Assert.` member is `Assert.Collection` | `git grep "Assert\." -- tests/` shows only `Assert.Collection` |
| **AC6** | **Static-analysis gates PASS (exit 0):** The three gates exit 0 on the changed `.cs` set: Roslyn analyzers (CA/IDE), cyclomatic complexity ≤ 15 (then refactor at ≤ 6), Rider/ReSharper inspections | `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1` all exit 0. Exit 2 (SKIPPED) is not a pass. |
| **AC7** | **Zero new warnings; orphaned helpers removed:** No warnings on changed files; no IDE0051 for helpers orphaned by test deletions (D3) | Build and Roslyn gate output; code review of `UnitTest1.cs` deletions |
| **AC8** | **Production code unchanged:** Only `tests/`, `Directory.Packages.props`, and `specs/DEV-280/` touched | `git diff --stat main...HEAD` shows no changes outside these paths |
| **AC9** | **Format verification:** `dotnet format --verify-no-changes` exit 0 | Command exit code and output |

---

## Changed Files Summary

**Tests:** `tests/LamuFlix.Test/{WorkerTests.cs, EnrichmentTests.cs, UnitTest1.cs}` (post-DEV-290 paths)

**New:** `tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs`, class `LamuFlixContextFactory`, public static method `CreateContext()`

**Configuration:** `Directory.Packages.props` (add 6 `<PackageVersion>` entries, remove 3 `<PackageVersion>` entries), `tests/LamuFlix.Test/LamuFlix.Test.csproj` (update `<PackageReference>` and project properties)

**Project:** `specs/DEV-280/` (this spec, plan.md, tasks.md)

---

## Notes

- **Baseline (pre-migration):** 18 passed / 4 skipped / 0 failed (measured once, independent of checkbox choice).
- **Acceptance criteria (post-Phase II):**
  - If owner chooses **Option A** (migrate 3 AssistirFilme tests): **18 passed / 0 skipped / 0 failed** (all tests pass; 4 [Ignore] tests no longer skipped because they are deleted).
  - If owner chooses **Option B** (delete 3 AssistirFilme tests): **15 passed / 0 skipped / 0 failed** (7 tests total deleted: 4 [Ignore] + 3 AssistirFilme_*; requires plan amendment).
- **Gate 1 remains CLOSED** until the owner ticks the checkbox on FR-008. This spec is provisional.
- The translation table lives in plan.md to keep this spec focused on requirements.
- Pre-existing EF Core InMemory and RabbitMQ `IModel` mocks are carried debt, not findings; they are translated 1:1 and never fixed or extended (per CONCLUSIONS.md Q5).
- `ApiDataModel.cs` is orphaned after test deletions but not ticket-named; it is a follow-up, never a review finding.
- Location test-data strings use backslash form—`@"C:\TestLibrary\Test[2020]\test.mkv"` (for `AssistirFilme_WhenLocalPlayEnabledAndPlayerConfigured...`) and `@"C:\TestLibrary\Test[2020]\test.mp4"` (for `AssistirFilme_WhenLocalPlayEnabledAndPlayerNull...`)—but are inert data fixtures; they do not touch disk. UnitTest1.cs migrated tests use these exact reference values for assertions on `ProcessStartInfo` properties (`FileName`, `ArgumentList`, `UseShellExecute`), compared against `movie.Location`. The `ProcessStarter` delegate is hand-faked (injected directly), not mocked. Assertions preserve original semantics and expected values; zero assertion logic edits.

**Line count (spec.md body only):** 160 lines
