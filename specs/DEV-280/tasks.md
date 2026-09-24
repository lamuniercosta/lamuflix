# DEV-280 Task Breakdown (T-series)

**Status:** draft  
**Author:** Quill (Drafter)  
**Total tasks:** 13 (mapped to Phase I / Phase II / Phase III)  

---

## Phase I: Precondition and Setup (T001–T004)

### T001: Pre-freeze rebase (before user merges DEV-290 Phase B PR)

**Owner:** User / Conductor  
**Timing:** Before DEV-290 Phase B restructure PR is merged  
**Status:** ✓ **DONE** — Rebase complete; HEAD @ b2659b7; `specs/DEV-290/` in tree.  
**Action:** (completed)
- Rebased `feature/280-spec` onto `origin/main` @ `b2659b7`.
- Verified `specs/DEV-290/` in tree after rebase.

**Acceptance:**
- `git rebase origin/main` succeeded; no conflicts.
- Tree contains post-DEV-290 spec files.

**Deliverable:** ✓ Worktree ready for pickup; rebase logged.

---

### T002: Pickup drift check

**Owner:** Implementation  
**Depends on:** T001 complete; DEV-290 Phase B restructure PR merged to `origin/main`; pre-freeze rebase done  
**Action:** Run `/speckit-analyze` at pickup to confirm no untracked changes to the plan.  
**Acceptance:**
- Drift check passes; plan remains valid.
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` exists at `tests/LamuFlix.Test/` on `origin/main` (post-290 structure confirmed).
- `docs/adr/0013` exists in tree (post-restructure confirmation).

**Deliverable:** Drift check logged; plan confirmed valid for implementation.

---

### T003: Confirm owner decision and record baseline

**Owner:** Implementation  
**Depends on:** T002 complete; DEV-290 Phase B restructure merged  
**STOP RULE:** Implementation is **blocked** until Checkpoint 1 (FR-008 owner decision) is explicitly ticked:
  - If Checkpoint 1 ticked to **Option A**: proceed to Phase II immediately.
  - If Checkpoint 1 ticked to **Option B**: **STOP and report to Patron**; do not delete the 3 AssistirFilme tests; plan amendment required before edit begins.  
**Checklist:**
- [ ] **Checkpoint 1 (FR-008)** is **ticked** to Option A or Option B. Spec is no longer provisional.
- [ ] If Option B ticked: Report to Patron before proceeding further; plan amendment required.
- [ ] Working directory clean; no uncommitted changes on `feature/280-spec`.
- [ ] `tests/LamuFlix.Test/LamuFlix.Test.csproj` is at correct post-DEV-290 path (if at root, rebase failed).
- [ ] Run `dotnet test` and record baseline (pre-migration, pre-Phase II, before any checkbox dependency):
  - **Baseline:** **18 passed / 4 skipped / 0 failed** (this measurement is taken once, with no branching on owner checkbox).
  - Post-implementation, if Option A executed: **18 passed / 0 skipped / 0 failed** (4 [Ignore] tests skipped removed, all 18 migrated tests pass, 3 AssistirFilme tests included).
  - Post-implementation, if Option B executed (requires plan amendment): **15 passed / 0 skipped / 0 failed** (4 [Ignore] tests + 3 AssistirFilme tests deleted, 15 tests remain from WorkerTests + EnrichmentTests).

**Deliverable:** Owner decision confirmed in task log; baseline counts recorded; Phase II gates open.

---

## Phase II: Implementation (T004–T010)

### T004: Add packages to Directory.Packages.props

**Owner:** Implementation  
**Depends on:** T003 complete (owner checkbox ticked, baseline recorded)  
**File:** `Directory.Packages.props`  
**Action:**
- Add 6 new `<PackageVersion>` entries (hardened pins):
  - xunit.v3 4.0.1
  - xunit.runner.visualstudio 4.0.0
  - NSubstitute 6.2.0
  - Shouldly 4.3.0
  - AutoFixture 4.18.1
  - Faker.Net 2.0.163
- Keep `Microsoft.NET.Test.Sdk` at 17.12.0 (no bump required).
- Do **not** remove MSTest + Moq versions yet (tree must stay green).

**Acceptance:**
- `dotnet build` compiles successfully.
- All new versions and bumped version visible in `PackageVersion` nodes.
- Existing MSTest/Moq entries still present.

**Deliverable:** Commit `DEV-280 - add xunit.v3, NSubstitute, Shouldly, AutoFixture, Faker.Net (hardened pins)`.

---

### T005: Create tests/LamuFlix.Tests.Common project and factory

**Owner:** Implementation  
**Depends on:** T004 complete  
**Files:**
- New: `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` (class library; `IsPackable=false`).
- New: `tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs`.

**Contents of factory file:**
- Class: `LamuFlixContextFactory`
- Public static method: `CreateContext()` (returns `LamuFlixContext`).
- Merges logic from `UnitTest1.cs:490 CreateInMemoryContext` + `EnrichmentTests.cs:109 CreateInMemoryDb`.
- No speculative builders; no AutoFixture customizations unless a migrated test explicitly needs one.
- Uses `Microsoft.EntityFrameworkCore.InMemory` (pre-existing; version 9.0.0 in Directory.Packages.props).

**Project file requirements:**
- `<ProjectReference>` to the Data project (for `LamuFlixContext` type).
- `<PackageReference>` to `Microsoft.EntityFrameworkCore.InMemory` only (no test packages).
- No `IsTestProject` / test SDK attributes.

**Update tests/LamuFlix.Test/LamuFlix.Test.csproj:**
- Add `<ProjectReference>` to `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`.
- Set `<OutputType>Exe</OutputType>` unconditionally (required for xUnit v3 runner compatibility).

**Acceptance:**
- `dotnet build` succeeds; no unresolved references.
- Factory method compiles and produces a valid in-memory context.
- `tests/LamuFlix.Test` project references the new project.
- **Not added to `LamuFlix.sln`** (builds as transitive dependency).
- OutputType is set to Exe (required for xUnit v3 runner compatibility).

**Deliverable:** Commit `DEV-280 - create tests/LamuFlix.Tests.Common with factory`.

---

### T006: Update tests/LamuFlix.Test/LamuFlix.Test.csproj for xUnit v3

**Owner:** Implementation  
**Depends on:** T005 complete  
**File:** `tests/LamuFlix.Test/LamuFlix.Test.csproj` (post-DEV-290 path)  
**Actions:**
- Add `<PackageReference>` entries (non-transitive):
  - xunit.v3 4.0.1
  - xunit.runner.visualstudio 4.0.0
  - NSubstitute 6.2.0
  - Shouldly 4.3.0
  - AutoFixture 4.18.1 (conditional per D2: only if migrated tests use it)
  - Faker.Net 2.0.163 (conditional per D2: only if migrated tests use it)
- Keep (do not remove yet):
  - Microsoft.NET.Test.Sdk.
  - RabbitMQ.Client
  - Microsoft.EntityFrameworkCore.InMemory (tests use it directly; keep existing reference; removal decision deferred to T010 post-extraction per declaration rule D2).
- Keep (MSTest/Moq still present): MSTest.TestAdapter, MSTest.TestFramework, Moq.

**Acceptance:**
- `dotnet build` succeeds (tree compiles with MSTest still present).
- `tests/LamuFlix.Test` references both old and new frameworks temporarily (removed in T010).

**Deliverable:** Commit `DEV-280 - add xUnit v3, NSubstitute, Shouldly to tests/LamuFlix.Test.csproj`.

---

### T007: Migrate tests/LamuFlix.Test/WorkerTests.cs to xUnit v3 + NSubstitute + Shouldly

**Owner:** Implementation  
**Depends on:** T006 complete  
**File:** `tests/LamuFlix.Test/WorkerTests.cs` (post-DEV-290 path)  
**Changes:**
- Remove `[TestClass]` attribute.
- Convert each `[TestMethod]` to `[Fact]`.
- Replace each `new Mock<T>()` with `Substitute.For<T>()`.
- Replace all `.Setup()` and `.Verify()` chains per translation table (plan.md).
- Replace all `Assert.*` with Shouldly equivalents (except `Assert.Collection`).
- Keep test logic and expected values identical (1:1 translation).
- Total: 5 tests migrated; no tests added or deleted.

**Acceptance:**
- `dotnet test --filter "WorkerTests"` → **5 passed, 0 skipped, 0 failed**.
- No MSTest or Moq references remain in this file.
- No bracket attributes (`[TestClass]`, `[TestMethod]`, `[Ignore]`, `[TestInitialize]`, `[TestCleanup]`) remain.
- All assertions use Shouldly (or `Assert.Collection`).
- Code compiles; no warnings.

**Deliverable:** Commit `DEV-280 - migrate WorkerTests.cs to xUnit v3 and NSubstitute`.

---

### T008: Migrate tests/LamuFlix.Test/EnrichmentTests.cs to xUnit v3 + extract factory

**Owner:** Implementation  
**Depends on:** T007 complete  
**File:** `tests/LamuFlix.Test/EnrichmentTests.cs` (post-DEV-290 path)  
**Changes:**
- Remove `[TestClass]` attribute.
- Convert each `[TestMethod]` to `[Fact]`.
- Delete the private `CreateInMemoryDb()` method (`:109`); replace calls with `LamuFlixContextFactory.CreateContext()` (from `Tests.Common`).
- Replace each `new Mock<T>()` with `Substitute.For<T>()`.
- Replace all `.Setup()` and `.Verify()` chains per translation table (plan.md).
- Replace all `Assert.*` with Shouldly equivalents (except `Assert.Collection`).
- Keep EF Core InMemory contexts and RabbitMQ `IModel` mocks (carried debt, translated 1:1, never fixed).
- Keep inert test-data `Location` strings (bare `F:\…` form) in all 10 EnrichmentTests (members: `FetchEnrichmentData_WithSingleMovie_EnrichesMovieMetadata`, `FetchEnrichmentData_WithMultipleMovies_EnrichesEachMovie`, plus 8 others); they do not touch disk.
- Total: 10 tests migrated; no tests added or deleted.

**Acceptance:**
- `dotnet test --filter "EnrichmentTests"` → **10 passed, 0 skipped, 0 failed**.
- No `CreateInMemoryDb()` method remains in this file.
- No MSTest or Moq references remain; no bracket attributes.
- All assertions use Shouldly (or `Assert.Collection`).
- Code compiles; no warnings on new lines.
- Inert Location test-data strings confirmed present (no disk access).

**Deliverable:** Commit `DEV-280 - migrate EnrichmentTests.cs to xUnit v3 and extract factory to Tests.Common`.

---

### T009: Migrate tests/LamuFlix.Test/UnitTest1.cs: delete 4 ignored tests, migrate 3 AssistirFilme tests

**Owner:** Implementation  
**Depends on:** T008 complete and **Checkpoint 1 (FR-008) ticked to Option A** (gate: fixture decision required)  
**File:** `tests/LamuFlix.Test/UnitTest1.cs` (post-DEV-290 path)  
**Part A: Delete 4 ignored tests and orphaned helpers (D3)**
- Delete methods: `TestMethod1()`, `TestMethod2()`, `TestMethod4()`, `TestMethod5()` (all `[Ignore]` marked).
- Delete every private helper method in `UnitTest1` that only these 4 tests reference (D3).
- Keep `ApiDataModel.cs` (out of scope; becomes a follow-up).
- Keep the 3 non-ignored tests (`AssistirFilme_*`).

**Part B: Migrate 3 non-ignored AssistirFilme tests (Option A from FR-008)**
- Remove `[TestClass]` attribute.
- Convert each `[TestMethod]` to `[Fact]` (3 total).
- Replace test `Location` string literals with exact reference values:
  - `AssistirFilme_WhenLocalPlayEnabledAndPlayerConfigured_StartsProcessWithConfiguredPlayerAndArgumentList`: use `@"C:\TestLibrary\Test[2020]\test.mkv"`
  - `AssistirFilme_WhenLocalPlayEnabledAndPlayerNull_DefaultsToOsAssociation`: use `@"C:\TestLibrary\Test[2020]\test.mp4"`
  - Assertions preserved; compare against `movie.Location`. Zero assertion logic edits.
- Preserve all assertions on `ProcessStartInfo`: `UseShellExecute`, `FileName`, `ArgumentList` (exact values, same expected behavior).
- Preserve the injected `ProcessStarter` delegate seam: **hand-fake setter only, never Mock of ProcessStarter**. Do **not** change production seam or `Features:LocalPlay` gate.
- Replace all `Assert.*` with Shouldly equivalents. UnitTest1 has no NSubstitute substitutes; ProcessStarter is always hand-faked.
- Confirm no `Process.Start` token remains in tests (only `ProcessStarter` delegate calls).

**Acceptance:**
- `dotnet test --filter "UnitTest1"` → **3 passed, 0 skipped, 0 failed** (4 skipped tests deleted).
- Total across all files: **18 passed, 0 skipped, 0 failed** (Worker 5 + Enrichment 10 + UnitTest1 3).
- `git grep "Process.Start" -- tests/` is empty.
- `git grep -F "[TestClass]" -- tests/` is empty; `git grep -F "[TestMethod]" -- tests/` is empty; `git grep -F "[Ignore]" -- tests/` is empty.
- No Mock<ProcessStarter> remains; hand-fake evidence verified in code review.
- All assertions preserved with exact expected values.
- `ProcessStartInfo` assertions (UseShellExecute, FileName, ArgumentList) verified.
- Production `FilmesService` and `Features:LocalPlay` gate unchanged.
- IDE0051 (unused private members) resolved: no warnings for deletions.

**Deliverable:** Commit `DEV-280 - migrate UnitTest1.cs: delete 4 ignored tests, migrate 3 AssistirFilme tests with hand-fake ProcessStarter`.

---

### T010: Remove MSTest and Moq packages

**Owner:** Implementation  
**Depends on:** T009 complete and all 18 tests passing  
**Files:** `Directory.Packages.props`, `tests/LamuFlix.Test/LamuFlix.Test.csproj`  
**Actions:**
- Remove from `Directory.Packages.props`: `<PackageVersion>` entries for MSTest.TestAdapter, MSTest.TestFramework, Moq.
- Remove from `tests/LamuFlix.Test/LamuFlix.Test.csproj`: `<PackageReference>` entries for MSTest.TestAdapter, MSTest.TestFramework, Moq.
- Remove from all `.cs` files in `tests/`:
  - `using Microsoft.VisualStudio.TestTools.UnitTesting;`
  - `using Moq;`
- Verify no MSTest or Moq tokens remain anywhere.
- Verify no bracket test attributes remain (use `git grep -F "[TestMethod]"`, `git grep -F "[TestClass]"`, `git grep -F "[Ignore]"`).

**Acceptance:**
- `dotnet build` succeeds; no compile errors.
- `dotnet test` still shows **18 passed, 0 skipped, 0 failed** (no regressions).
- `git grep -E "MSTest|VisualStudio\.TestTools" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep "Moq" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep "Mock<" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep -F "[TestMethod]" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep -F "[TestClass]" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep -F "[Ignore]" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep -F "[TestInitialize]" -- tests/ *.csproj *.props *.sln` returns empty.
- `git grep -F "[TestCleanup]" -- tests/ *.csproj *.props *.sln` returns empty.

**Conditional Decision (D2 / InMemory reference):**
- After T009 completion and all 18 tests passing: check whether `tests/LamuFlix.Test` directly consumes `Microsoft.EntityFrameworkCore.InMemory`.
- If tests use InMemory directly (beyond factory): keep `<PackageReference>` in `tests/LamuFlix.Test.csproj`.
- If factory is the only consumer (tests do not call InMemory directly): remove `<PackageReference>` from `tests/LamuFlix.Test.csproj` (factory in Tests.Common already has it).
- Report decision with commit message; include in PR body.

**Deliverable:** Commit `DEV-280 - remove MSTest and Moq packages; remove all bracket test attributes; InMemory reference decision`.

---

## Phase III: Verification (T011–T013)

### T011: Run static-analysis gates

**Owner:** Implementation  
**Depends on:** T010 complete  
**Commands:**
1. `./scripts/run-roslyn-analyzers.ps1` (changed `.cs` set; CA/IDE at warning+)
2. `./scripts/run-cyclomatic-complexity.ps1` (threshold ≤ 15; then refactor at ≤ 6)
3. `./scripts/run-jetbrains-inspectcode.ps1` (Rider/ReSharper)

**Acceptance:**
- All three exit 0 (PASS).
- No new warnings on changed files.
- Pre-existing InMemory / `IModel` mock hits on moved-but-unchanged lines → report as `blocked: structural` (not a finding; carried debt).
- Zero IDE0051 warnings for orphaned helpers (deletions in T009).

**Deliverable:** Gate results logged; all exit 0; any structural blocks reported to Keel.

---

### T012: Verify acceptance criteria AC1–AC9

**Owner:** QA / Implementation  
**Depends on:** T011 complete  
**Checklist:**

- [ ] **AC1:** `dotnet test` → **18 passed, 0 skipped, 0 failed**
- [ ] **AC2:** Zero MSTest/Moq: `git grep` over `*.cs`, `*.csproj`, `*.props`, `*.sln` returns nothing
- [ ] **AC3:** No disk I/O: `git grep "F:/Filmes" -- tests/ *.csproj *.props *.sln` returns empty; `git grep -F "F:\Filmes" -- tests/ *.csproj *.props *.sln` returns empty (`-F` required: the bare form is regex-escaped and matches nothing, a false green); no `File.` or `Directory.` in tests
- [ ] **AC4:** No Process.Start: `git grep "Process.Start" -- tests/` is empty
- [ ] **AC5:** Shouldly-only assertions: `git grep "Assert\." -- tests/` shows only `Assert.Collection`
- [ ] **AC6:** Gates exit 0: `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`
- [ ] **AC7:** Zero new warnings; orphaned helpers removed (IDE0051)
- [ ] **AC8:** Production unchanged: `git diff --stat main...HEAD` shows only `tests/**`, `Directory.Packages.props`, `specs/DEV-280/**`
- [ ] **AC9:** Format verification: `dotnet format --verify-no-changes` exit 0

**Deliverable:** All 9 criteria verified and logged in task notes.

---

### T013: Prepare PR with spec, plan, tasks, and TICKED owner checkpoint

**Owner:** Quill (Drafter)  
**Depends on:** T012 complete; **Checkpoint 1 (FR-008) must be TICKED to Option A** (Option B executable only if plan formally amended to lock alternative end-state requirements)  
**Contents:**
- spec.md, plan.md, tasks.md in `specs/DEV-280/`.
- **Gate 1 condition:** Checkpoint 1 (FR-008) in spec.md TICKED to Option A (proceed directly) or Option B (requires plan amendment before Phase II start).
- PR title: `DEV-280 - Migrate test suite from MSTest to xUnit v3 and NSubstitute`.
- PR body: 
  - Merge-bar proof (AC1–AC9 all verified).
  - Key findings (if any structural blocks, report as `blocked: structural`; never a review finding).
  - Test count: 18 passed / 0 skipped / 0 failed (Option A executed under this plan).
  - Owner decision: Option A chosen (migrate 3 AssistirFilme tests).
- All tests confirmed passing under xUnit v3.
- Review cap = 2 rounds; bar = Critical/High with concrete failure.

**Acceptance:**
- PR passes all gates.
- **Checkpoint 1 (FR-008) TICKED** (not provisional; not pending).
- All AC1–AC9 verified and logged.
- Ready for Phase III review.

**Deliverable:** PR created; Bernstein notified via maestri ask (Quill → Bernstein with report).

---

## Summary

| Task | Phase | Owner | Deliverable |
|---|---|---|---|
| T001–T003 | Phase I | User / Impl | Pre-freeze rebase done; drift check clean; owner decision ticked; baseline recorded |
| T004–T010 | Phase II | Implementation | 18 tests migrated; old packages removed; all gates pass; ProcessStarter hand-fake verified |
| T011–T013 | Phase III | QA + Quill | AC1–AC9 verified; PR ready for review; Gate 1 checkbox ticked (firm) |

**Line count (tasks.md body only):** 355 lines (13 tasks total: T001–T013)

**Status:** Ready for implementation pending owner checkbox (FR-008, Decision D4) ticking. Phase I: T001 rebase @ b2659b7 complete; T002–T003 blocked until DEV-290 Phase B restructure PR merged to origin/main. Phase II blocked until Phase I complete and owner checkbox (FR-008) ticked to Option A (required for frozen plan execution).
