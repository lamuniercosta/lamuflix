# DEV-280 Implementation Plan

**Status:** draft  
**Author:** Quill (Drafter)  
**Phases:** Precondition → Implementation → Verification  

---

## Phase Overview

### Phase I: Precondition and Setup (No code changes)

**Goal:** Confirm preconditions, establish baselines, owner decision, and prepare the environment.

**External gates:**
- DEV-290 Phase B restructure PR merged: `tests/LamuFlix.Test/LamuFlix.Test.csproj` exists on `origin/main` at pickup.
- Owner checkbox 1-(FR-008) ticked to Option A or Option B before Phase II implementation begins. **Note:** Option B (delete 3 AssistirFilme tests) is not executable under frozen plan; formal plan amendment required if Option B chosen.
- Recon-DEV-280 pins filed and hardened (exact versions in spec and plan tables).

**Pre-freeze rebase (before user merge of DEV-290 Phase B):**
- Rebase `feature/280-spec` onto `origin/main` @ `b2659b7` or later; verify `specs/DEV-290/` in tree.

**Pickup steps (at start of implementation):**
1. Run drift check: `/speckit-analyze` on `main` to confirm no untracked changes to the plan.
2. Confirm owner checkbox 1-(FR-008) ticked (Option A or B decided).
3. Record baseline (measured once, pre-Phase II, before any checkbox-dependent branching): `dotnet test` → **18 passed / 4 skipped / 0 failed** (this baseline is independent of checkbox choice).
4. Verify no conflicts with active branches; working directory clean.
5. **Owner checkbox 1-(FR-008):** Must be ticked before Phase II begins (checkbox in brief.md).

**Deliverable:** Baseline recorded; owner decision confirmed; worktree ready for Phase II.

---

### Phase II: Package Setup and Refactoring (Core implementation)

**Goal:** Add packages, extract shared helpers, migrate tests, and remove old frameworks.

**Gated on:** Phase I complete; owner checkbox 1-(FR-008) ticked (Gate 1 conditions met).

**Steps:**

1. **Setup packages** — Add 6 `<PackageVersion>` entries to `Directory.Packages.props` (hardened pins filed); keep MSTest/Moq for now (tree stays green).
2. **Create Tests.Common** — New project `tests/LamuFlix.Tests.Common/` with in-memory context factory; add `ProjectReference` from `LamuFlix.Test`.
3. **Switch test project** — Update `LamuFlix.Test.csproj`: add xunit.v3 + runner + NSubstitute + Shouldly `<PackageReference>`; set `OutputType Exe` unconditionally (required for xUnit v3 runner compatibility); add reference to `Tests.Common`.
4. **Migrate WorkerTests** → green (5 tests).
5. **Migrate EnrichmentTests** → extract factory, run green (10 tests).
6. **Migrate UnitTest1.cs** → delete 4 ignored tests + orphaned helpers; migrate 3 `AssistirFilme_*` tests → green (3 tests).
7. **Remove old packages** — Delete MSTest + Moq `<PackageReference>` and `<PackageVersion>` entries; remove `using` statements.
8. **Verify AC1–AC9** — All acceptance criteria pass; gates exit 0 (CC ≤ 15, then refactor at ≤ 6).

**Deliverable:** All 18 tests passing; zero MSTest/Moq; spec/plan/tasks in PR.

---

### Phase III: Review and Merge

**Goal:** Review and approve the PR; prepare for follow-up work.

**Gate:** Closing bar = Critical/High with concrete failure scenario; cap = 2 review rounds.

**Post-merge:** Follow-up candidate (`ApiDataModel.cs` orphaned).

---

## Package Management (CPM)

### Versions to be Added to Directory.Packages.props

| Package | Current Version | Pin | Notes |
|---|---|---|---|
| `xunit.v3` | — | 4.0.1 | Hardened; filing resolves R4 |
| `xunit.runner.visualstudio` | — | 4.0.0 | Hardened; filing resolves R4 |
| `NSubstitute` | — | 6.2.0 | Hardened; filing resolves R4 |
| `Shouldly` | — | 4.3.0 | Hardened; filing resolves R4 |
| `AutoFixture` | — | 4.18.1 | Hardened; filing resolves R4; conditional per D2 |
| `Faker.Net` | — | 2.0.163 | Hardened; filing resolves R4; conditional per D2 |

### Versions to be Removed from Directory.Packages.props

| Package | Current Version | Reason |
|---|---|---|
| `MSTest.TestAdapter` | 3.6.4 | Replaced by xUnit v3 |
| `MSTest.TestFramework` | 3.6.4 | Replaced by xUnit v3 |
| `Moq` | 4.20.72 | Replaced by NSubstitute |

### Kept Packages (With Versions Updated as Needed)

| Package | Current Version | New Version | Reason |
|---|---|---|---|
| `Microsoft.NET.Test.Sdk` | 17.12.0 | 17.12.0 | VSTest host (not MSTest); kept as-is. |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.0 | 9.0.0 | Pre-existing; carried in tests; referenced by `tests/LamuFlix.Tests.Common` for factory. `LamuFlix.Test` reference conditional (D2, decided in T010). |
| `RabbitMQ.Client` | Pre-existing | Carried | Carried in tests; retained 1:1. |

### Declaration Rule (D2)

A project gets a `<PackageReference>` only when its own code consumes the package:

- `tests/LamuFlix.Test/LamuFlix.Test.csproj` references: xunit.v3, xunit.runner.visualstudio, NSubstitute, Shouldly, plus carried packages (RabbitMQ.Client, Test.Sdk). AutoFixture and Faker.Net are conditional (only referenced if migrated tests use them).
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` references: Microsoft.EntityFrameworkCore.InMemory (for factory implementation). InMemory reference in `tests/LamuFlix.Test` is conditional per D2: removed after factory extraction if tests do not directly consume it.

If a migrated test uses neither AutoFixture nor Faker.Net, its project does not reference them. The `<PackageVersion>` entry stays in `Directory.Packages.props` (ticket-decided). InMemory reference removal decision moves to T010 (post-extraction, post-T009).

---

## tests/LamuFlix.Tests.Common/ Structure

**Project:** `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` (class library; `IsPackable=false`, no test SDK, no xUnit reference).

**Contents:**

- **File:** `LamuFlixContextFactory.cs`
  - **Class:** `LamuFlixContextFactory`
  - **Public static method:** `public static LamuFlixContext CreateContext()`
  - **Purpose:** EF Core in-memory `LamuFlixContext` factory.
  - **Merges:** `UnitTest1.cs:490 CreateInMemoryContext` + `EnrichmentTests.cs:109 CreateInMemoryDb`.
  - **API:** Public static factory method (e.g., `public static LamuFlixContext CreateContext() => ...`).
  - **No speculative builders, no AutoFixture customizations unless a migrated test explicitly needs one.**

**Dependencies:**
- `ProjectReference` to the Data project (for `LamuFlixContext` type).
- `PackageReference` to `Microsoft.EntityFrameworkCore.InMemory` (exists in `Directory.Packages.props` per plan).

**Integration:**
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` adds `<ProjectReference>` to `Tests.Common`.
- **Not added to `LamuFlix.sln`** (firm decision per brief.md Q1; builds as transitive dependency).
- **Conditional per D2:** If no migrated test uses `Microsoft.EntityFrameworkCore.InMemory` directly, remove its `PackageReference` from `tests/LamuFlix.Test/LamuFlix.Test.csproj` after extraction (the package is used by `Tests.Common` only). Include this check as part of Phase II cleanup.

---

## Translation Table: MSTest / Moq → xUnit v3 / NSubstitute / Shouldly

**Scope:** Rows include only constructs recon-DEV-280 confirms are present in `WorkerTests.cs` + `EnrichmentTests.cs`. (DataRow/DataTestMethod, TestInitialize/TestCleanup not present; table reflects actual usage per R6.) Rows 9–10 (exception handling), row 11 (IsNotNull assertion), and row 12 (argument matching) apply to all migrated files.

| # | MSTest / Moq | xUnit v3 / NSubstitute / Shouldly | Example |
|---|---|---|---|
| 1 | `[TestClass]` | Remove (plain C# class) | `public class WorkerTests` (no base) |
| 2 | `[TestMethod]` | `[Fact]` | `[Fact] public void TestFoo() { }` |
| 3 | `new Mock<T>()` / `.Object` | `Substitute.For<T>()` | `var fakeService = Substitute.For<IService>();` |
| 4 | `.Setup(x => x.Method()).Returns(v)` | `fakeService.Method().Returns(v);` | `fakeService.DoWork().Returns(42);` |
| 5 | `.Verify(x => x.Method(), Times.Once)` | `fakeService.Received(1).Method();` | `fakeService.Received(1).DoWork();` |
| 6 | `Assert.AreEqual(expected, actual)` | `actual.ShouldBe(expected)` | `result.ShouldBe(42)` |
| 7 | `Assert.IsTrue(condition)` | `condition.ShouldBeTrue()` | `isValid.ShouldBeTrue()` |
| 8 | `Assert.IsNull(value)` | `value.ShouldBeNull()` | `result.ShouldBeNull()` |
| 9 | `Assert.ThrowsException<TException>(() => Action())` | `Should.Throw<TException>(() => Action())` | `Should.Throw<ArgumentException>(() => sut.ValidateInput(null))` |
| 10 | `Assert.ThrowsExceptionAsync<TException>(async () => await Action())` | `Should.ThrowAsync<TException>(() => Action())` | `Should.ThrowAsync<InvalidOperationException>(() => sut.ProcessAsync(invalid))` |
| 11 | `Assert.IsNotNull(value)` | `value.ShouldNotBeNull()` | `result.ShouldNotBeNull()` |
| 12 | `It.IsAny<T>()` | `Arg.Any<T>()` | `fakeLogger.Received(1).Log(Arg.Any<string>(), Arg.Any<LogLevel>())` |

**Note:** Assertions preserve exact meaning (same expected values, same logic). Row 10 is async exception handling (EnrichmentTests pattern per recon-8). Row 11 adds assert-not-null (recon-11 T2/T3).

---

## Key Milestones

| Milestone | Gate | Criteria |
|---|---|---|
| **Baseline and post-change outcomes** | Phase I complete | Baseline (pre-Phase II): 18 / 4 / 0 (one measurement, no checkbox branch). Post-Phase II: Option A outcome 18 / 0 / 0, Option B outcome 15 / 0 / 0 (if plan amended). |
| **Setup packages** | Phase II step 1 | 6 new `<PackageVersion>` entries added to `Directory.Packages.props` (hardened pins); tree compiles |
| **Create Tests.Common** | Phase II step 2 | Project builds; factory compiles; reference added to `tests/LamuFlix.Test` |
| **Switch test project** | Phase II step 3 | xunit.v3, runner, NSubstitute, Shouldly added; `OutputType Exe` set; reference added; tree compiles |
| **Migrate WorkerTests** | Phase II step 4 | 5 tests green; AC1–AC5 pass on this file |
| **Migrate EnrichmentTests** | Phase II step 5 | 10 tests green; factory extraction verified; AC1–AC5 pass; InMemory reference conditional check complete |
| **Migrate UnitTest1** | Phase II step 6 | 4 tests deleted, 3 tests migrated (or 7 tests deleted if Option B); total passing matches baseline choice; AC1–AC4 pass (no disk I/O); ProcessStarter hand-fake verified |
| **Remove old packages** | Phase II step 7 | AC2 verified (zero MSTest/Moq refs); git grep -F for bracket attributes clean |
| **Verify all gates** | Phase II step 8 | AC6–AC9 all pass; exit codes 0; no new warnings; format clean |
| **PR ready** | Phase III | spec.md, plan.md, tasks.md in PR; owner checkbox ticked; `git diff --stat` shows only expected changes |

---

## Risk Mitigation

**Recon-DEV-280 pins hardened and filed:** Exact versions are in spec FR-006 and plan package tables (xunit.v3 4.0.1, runner 4.0.0, NSubstitute 6.2.0, Shouldly 4.3.0, AutoFixture 4.18.1, Faker.Net 2.0.163). If any version changes before implementation starts, report to Keel; gate remains closed.

**Pre-existing InMemory / IModel mocks flagged as findings on changed lines:** Report to Keel as `blocked: structural` (carried debt, not a review finding; per CONCLUSIONS.md Q5); never suppress.

**DEV-290 Phase B restructure delayed past pre-freeze rebase window:** The pre-freeze rebase @ b2659b7 is complete; pickup drift check confirms post-290 structure exists. If Phase B restructure is not merged by pickup, implementation is blocked until it is (hard gate).

---

## Commit Structure

**Expected commits:** 1 commit per Phase II step (steps 1–8) + final commit for cleanup if needed. Steps 4–8 (test migrations) may be combined into 1 commit if xUnit + NSubstitute + Shouldly + project changes prevent mid-migration compilation; otherwise separate commits preserve reviewability.

**Commit message format:** `DEV-280 - {subject}` (per brief.md and CLAUDE.md).

Examples:
- `DEV-280 - add xunit.v3, NSubstitute, Shouldly to Directory.Packages.props`
- `DEV-280 - create tests/LamuFlix.Tests.Common with in-memory context factory`
- `DEV-280 - migrate WorkerTests to xUnit v3 and NSubstitute`
- `DEV-280 - migrate EnrichmentTests to xUnit v3, extract factory to Tests.Common`
- `DEV-280 - migrate UnitTest1.cs: delete 4 ignored tests, migrate 3 AssistirFilme tests`
- `DEV-280 - remove MSTest and Moq packages`

---

## Success Criteria

**Phase II is complete when:**
1. All tests pass under xUnit v3 with post-change outcome (18 passed / 0 skipped / 0 failed for Option A executed; 0 skipped always).
2. AC1–AC9 verified; all gates exit 0.
3. `git diff --stat main...HEAD` shows only `tests/**`, `Directory.Packages.props`, `specs/DEV-280/**`.
4. PR carries spec.md, plan.md, tasks.md, and **owner checkbox 1-(FR-008) is ticked** (Option A chosen; Option B requires prior plan amendment).
5. Ready for Phase III review (cap = 2 rounds; bar = Critical/High with concrete failure).

**Line count (plan.md body only):** 204 lines
