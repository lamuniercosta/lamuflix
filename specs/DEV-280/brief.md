# DEV-280 — Migrate test suite from MSTest to xUnit v3 and NSubstitute, removing disk-bound tests

**Seat:** Keel (Thinker) · **Owner proxy:** Patron · **Conductor:** Bernstein · **Drafter:** Quill
**Size:** M · **ui:** none · **Parent:** DEV-281 · **Worktree:** `F:\Dev\LamuFlix.worktrees\feature-280-spec` (`feature/280-spec`, base `d1d9cfa`)
**Gate 1:** `provisional` — closed until the owner ticks the checkbox below.
**Grill record:** every question and ruling is in `CONCLUSIONS.md` (Q1–Q5, D1–D4). This brief is the decision of record; if it is not here, it is not decided.

## Frozen scope

Ticket *Scope & Technical Design* only (`scripts/youtrack-plan.json` DEV-280), plus the three non-ignored `UnitTest1.cs` tests under owner checkbox 1:

1. Replace MSTest (`MSTest.TestAdapter`, `MSTest.TestFramework`) with `xunit.v3` + `xunit.runner.visualstudio`.
2. Replace Moq with NSubstitute.
3. Assertions in Shouldly; the only xUnit `Assert` member allowed is `Assert.Collection`.
4. Test data vocabulary: AutoFixture and Faker.Net (not Bogus, not FluentAssertions, not Moq).
5. Migrate `WorkerTests.cs` (5 tests) and `EnrichmentTests.cs` (10 tests).
6. **Delete** `UnitTest1.cs` `TestMethod1`, `TestMethod2`, `TestMethod4`, `TestMethod5` (the 4 `[Ignore]` disk-bound tests), and the private helpers they alone use (see D3).
7. Create `tests/LamuFlix.Tests.Common/` for shared test helpers (Q1).
8. **Checkbox-gated:** migrate the 3 non-ignored `FilmesService` tests in `UnitTest1.cs` (`:498`, `:517`, `:560`) — option A below.

Out of scope (never review findings; follow-ups at most): `LamuFlix.Test/ApiDataModel.cs` (orphaned after step 6, not ticket-named — follow-up); pre-existing EF Core InMemory contexts (`EnrichmentTests.cs:111`, `UnitTest1.cs:492`) and RabbitMQ `IModel` channel mocks (`EnrichmentTests.cs:150,189,225,262,300`) — carried debt, translated 1:1, never fixed or extended; any production code, including `FilmesService` and the `Features:LocalPlay` gate; file renames (`UnitTest1.cs` keeps its name); solution-file membership of `Tests.Common`.

## Paths and dependency ruling (D1)

- All spec/plan/tasks paths are **post-DEV-290**: `tests/LamuFlix.Test/…` (DEV-290 FR-004) and `tests/LamuFlix.Tests.Common/…`.
- **Phase B gate:** DEV-280 implementation may not start until the DEV-290 *restructure* PR (DEV-290 Phase B, `git mv LamuFlix.Test tests/LamuFlix.Test`) is merged, proven by `tests/LamuFlix.Test/LamuFlix.Test.csproj` existing on `origin/main`. PR #9 being merged does **not** satisfy this; PR #9 was DEV-290 Phase A only (`origin/main` `b2659b7` still has `LamuFlix.Test/` at the root).
- **Operational (not a plan change):** rebase `feature/280-spec` onto `origin/main` (`b2659b7` or later) before the Phase A freeze, so `specs/DEV-290/` is in the tree. The Pickup drift check re-runs `/speckit-analyze` against `main` at Phase B. *(Amended A1, analyze H5: `docs/adr/0013` is DEV-290 T023, a Phase B deliverable; `origin/main` has no `docs/`. Its presence is checked at pickup (T002), after the restructure merges, never at the pre-freeze rebase.)*
- Building DEV-280 before the DEV-290 restructure would reorder planned work: that is the user's call, not the team's.

## Closing bar and round cap (Q5)

- Blocking = Critical or High **with a concrete failure scenario**. Everything else is a follow-up.
- Frozen scope = the list above. Cap = **2** review rounds. Gherkin acceptance tests opted out.

## Acceptance (Q4, as amended by Patron)

| # | Criterion | Evidence |
|---|---|---|
| AC1 | `dotnet test` discovers and runs every test through xUnit v3: **passed 18, skipped 0, failed 0** | `dotnet test` summary. 18 = WorkerTests 5 + EnrichmentTests 10 + UnitTest1 3 migrated. Baseline 18 / 4 / 0 at `450e70f` (DEV-290 `spec.md:50`); the 4 skips are exactly the deleted tests |
| AC2 | Zero MSTest and zero Moq anywhere: no `PackageReference`/`PackageVersion`, no `using Microsoft.VisualStudio.TestTools.UnitTesting`, no `using Moq`, no `Mock<`, no `[TestMethod]`/`[TestClass]`/`[TestInitialize]`/`[Ignore]` | `git grep` over `*.cs`, `*.csproj`, `*.props`, `*.sln` returns nothing |
| AC3 | No test performs disk I/O. The ban is exactly the two Filmes literal forms, `F:/Filmes` and `F:\Filmes`: zero hits under `tests/` (N1). The exception is pattern-scoped: inert drive-prefixed `Location` data is allowed (it never touches disk), e.g. the six bare-F EnrichmentTests `Location` strings (cite by test member name). **Do not** grep bare `F:`. The two retained UnitTest1 Filmes literals are rewritten, not excepted | grep for the two `Filmes` literals: base 4 hits (all `UnitTest1.cs` `:27,:349,:534,:576`) → head 0; code review for `File.`/`Directory.` in tests |
| AC4 | No `Process.Start` token under `tests/`. Retained tests use the injected `ProcessStarter` delegate | `git grep "Process.Start" -- tests/` empty |
| AC5 | Assertions use Shouldly; the only `Assert.` member in migrated tests is `Assert.Collection` | `git grep "Assert\." -- tests/` shows only `Assert.Collection` |
| AC6 | The three static-analysis gates **PASS (exit 0)** on the changed `.cs` set. Exit 2 (SKIPPED) is not a pass; an unrunnable gate is *Could not run* and blocks | `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1` (15, then refactor at 6), `run-jetbrains-inspectcode.ps1` |
| AC7 | Zero new warnings on changed files; in particular no IDE0051 for helpers orphaned by the deletions | build + Roslyn gate output |
| AC8 | Production code unchanged: `git diff` touches only `tests/**`, `Directory.Packages.props`, and `specs/DEV-280/**` | `git diff --stat main...HEAD` |
| AC9 | `dotnet format --verify-no-changes` exit 0 | command exit |

## Plan decisions

### Packages (Q2, Q3, D2)

- **Added to `Directory.Packages.props`** (CPM; ticket-named, no Gate 1 checkbox): `xunit.v3`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly`, `AutoFixture`, `Faker.Net`. Versions (R4 **closed** 2026-09-23; latest stable, with provenance from recon item 7 and Patron's NuGet re-verification): `xunit.v3` **4.0.1**, `xunit.runner.visualstudio` **4.0.0**, `NSubstitute` **6.2.0**, `Shouldly` **4.3.0**, `AutoFixture` **4.18.1**, `Faker.Net` **2.0.163**.
- **`PackageReference` only where consumed** (D2): a project references a package only if its own code uses it. If no migrated test uses AutoFixture or Faker.Net, no project references it; its `PackageVersion` entry still lands (ticket-decided vocabulary).
- **Excluded (§2.3 item 1, not ticket-named):** `AutoFixture.Xunit3`, `NSubstitute.Analyzers.*`, and any other package. So `[AutoData]` is unavailable; data comes from `new Fixture().Create<T>()` and Faker.Net through `[Theory]` + `[MemberData]`.
- **Removed** (R2 verified the only consumers are `LamuFlix.Test.csproj:12-14` and `Directory.Packages.props:18-20`): `MSTest.TestAdapter`, `MSTest.TestFramework`, `Moq`, both the `PackageReference` and the `PackageVersion`. If R2 changes at pickup, **stop and report**; do not remove.
- **Kept:** `Microsoft.NET.Test.Sdk` (VSTest host, not MSTest), `Microsoft.EntityFrameworkCore.InMemory`, `RabbitMQ.Client`. `Microsoft.NET.Test.Sdk` **stays 17.12.0** (R4(b): above the xunit.v3 / runner floor; latest stable 18.10.1 is recorded for provenance only; the bump branch is deleted).
- xUnit v3 test projects must be executables: `<OutputType>Exe</OutputType>` is set on `LamuFlix.Test.csproj` **unconditionally** (R4(c)).

### `tests/LamuFlix.Tests.Common/` (Q1)

- A class library, not a test project (`IsPackable=false`; no test SDK, no xUnit reference).
- Content = only helpers the migrated tests already share: **one** EF Core in-memory `LamuFlixContext` factory, merging `UnitTest1.cs:490 CreateInMemoryContext` and `EnrichmentTests.cs:109 CreateInMemoryDb`. No speculative builders, no AutoFixture customizations unless a migrated test needs one.
- References: `ProjectReference` to the Data project (for `LamuFlixContext`); `PackageReference` to `Microsoft.EntityFrameworkCore.InMemory`. If `LamuFlix.Test` no longer uses InMemory types directly after the extraction, its own InMemory `PackageReference` is removed (declaration rule).
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` gets a `ProjectReference` to `Tests.Common`.
- **Not** added to `LamuFlix.sln` (the ticket does not ask; it builds as a dependency of `LamuFlix.Test`).

### Translation rules (mechanical, 1:1)

| MSTest / Moq | xUnit v3 / NSubstitute / Shouldly |
|---|---|
| `[TestClass]` | removed |
| `[TestMethod]` | `[Fact]` |
| `[DataRow]` + `[DataTestMethod]` | `[Theory]` + `[InlineData]` |
| `[TestInitialize]` / `[TestCleanup]` | constructor / `IDisposable` (or `IAsyncLifetime`) |
| `[ExpectedException]` / `Assert.ThrowsException` | `Should.Throw<T>` / `Should.ThrowAsync<T>` |
| `Assert.AreEqual/IsTrue/IsNull/…` | Shouldly `ShouldBe`/`ShouldBeTrue`/`ShouldBeNull`/… |
| `new Mock<T>()` / `.Object` | `Substitute.For<T>()` |
| `.Setup(x => …).Returns(v)` | `x.Method(…).Returns(v)` |
| `.Verify(…, Times.Once)` | `.Received(1).Method(…)` |
| `It.IsAny<T>()` | `Arg.Any<T>()` |

Every assertion keeps its meaning: same expected values, same member under test. Recon R6 (the exact construct inventory) is still open; Quill lists the constructs actually present once R6 returns and does not invent rows beyond them.

### UnitTest1.cs

- Delete `TestMethod1`, `TestMethod2`, `TestMethod4`, `TestMethod5` **and** every private member that only they reference (D3). `ApiDataModel.cs` stays (out of scope, follow-up).
- Migrate the 3 `AssistirFilme_*` tests (option A), preserving the `Features:LocalPlay` configuration they set (false / true), the `ProcessStartInfo` assertions (`UseShellExecute`, `FileName`, `ArgumentList`), and the injected `ProcessStarter` seam. `FilmesService` and the `Features:LocalPlay` gate are not touched.
- **N1 (Patron, round 3, final):** rewrite the two inert `Location` initializers that carry `F:\Filmes`. In `AssistirFilme_WhenLocalPlayEnabledAndPlayerConfigured_StartsProcessWithConfiguredPlayerAndArgumentList` (base `:534`) use `@"C:\TestLibrary\Test[2020]\test.mkv"`. In `AssistirFilme_WhenLocalPlayEnabledAndPlayerNull_DefaultsToOsAssociation` (base `:576`) use `@"C:\TestLibrary\Test[2020]\test.mp4"`. Make **zero assertion edits**: the assertions compare against `movie.Location` (base `:556`, `:596`). Values are taste; if an equivalent differs, log it `[assumed]`.
- **N3:** Option B is not executable under this frozen plan. If the owner ticks Option B, **stop and report to Patron**; Option B needs a Patron plan amendment before any edit, and the 3 tests are not deleted.

### Task ordering

1. Phase B gate check: `tests/LamuFlix.Test/LamuFlix.Test.csproj` on `origin/main`; rebase; drift check.
2. Baseline: `dotnet test` → 18 / 4 / 0 (record it). Measure it before any change; it is **always** 18 / 4 / 0 whichever way checkbox 1 is ticked (N2). Post-change: Option A = 18 / 0 / 0. Option B = 15 passed, a post-change figure only and never a baseline, and not executable without a Patron plan amendment (N3).
3. `Directory.Packages.props`: add the 6 `PackageVersion`s (keep MSTest/Moq for now so the tree stays green).
4. Create `tests/LamuFlix.Tests.Common/` with the in-memory context factory; reference it from `LamuFlix.Test`.
5. Switch `LamuFlix.Test.csproj` to xunit.v3 + runner + NSubstitute + Shouldly (+ `OutputType Exe`).
6. Migrate `WorkerTests.cs` → green (5).
7. Migrate `EnrichmentTests.cs` onto the shared factory → green (10).
8. `UnitTest1.cs`: delete the 4 tests plus their orphaned helpers; migrate the 3 `AssistirFilme_*` tests → green (3).
9. Remove the MSTest + Moq `PackageReference`s and `PackageVersion`s.
10. Verify AC1–AC9; gates at 15 then refactor at 6.

Steps 5–8 may share one commit if the project cannot compile half-migrated; otherwise one commit per step. Every commit subject starts `DEV-280 - `.

### Test strategy

This ticket *is* test code; there are no new production behaviours. The proof is: the same 18 behaviours pass under the new framework, with assertions kept 1:1 (AC1, AC5); plus the negative greps (AC2–AC4). No new tests, no property tests (no invariants), no mutation run required beyond the harness default. A migrated assertion that is weaker than the original counts as a review finding (TEST-WRONG).

### Gate expectations

- Changed `.cs` set: `tests/LamuFlix.Test/{WorkerTests,EnrichmentTests,UnitTest1}.cs` + the new `Tests.Common` factory file.
- All three gates exit 0 on that set; CC ≤ 15, then refactor gate ≤ 6. Pre-existing hits in carried debt (InMemory, `IModel` mocks) that the gates flag on moved-but-unchanged lines → report to Keel as `blocked: structural`, never suppress.

## Gate 1 owner checkboxes

- [ ] **1. UnitTest1.cs non-ignored tests (§2.3 item 5; alternative is item 6).** The ticket does not name the 3 `FilmesService` tests (`UnitTest1.cs:498`, `:517`, `:560`). **Option A (recommended):** migrate them, preserving assertions and the injected `ProcessStarter` seam. This includes the two inert `Location` replacements (`F:\Filmes` → synthetic `C:\TestLibrary\…`, N1), with assertions unchanged. Production `FilmesService` and the `Features:LocalPlay` gate stay unchanged. Post-change: 18 passed / 0 skipped / 0 failed. **Option B:** delete them. Post-change passed drops to 15 and tests the ticket does not name are removed. This drops planned work, so it is not executable under the frozen plan and needs a Patron plan amendment before any edit (N3). Zero-MSTest requires one or the other. The pickup baseline is 18 / 4 / 0 either way (N2).

## Pending records (Rigger)

- YouTrack clarification comment on DEV-280 (Patron-decided): `UnitTest1.cs` holds 7 tests; the 4 `[Ignore]` ones are deleted, the 3 non-ignored ones migrate (subject to checkbox 1); baseline 18 passed / 4 skipped / 0 failed. Rigger is not connected on this canvas; the Conductor convenes it.
- Follow-up candidate (file only, do not schedule): `LamuFlix.Test/ApiDataModel.cs` orphaned after the deletions.
- Follow-up candidate (file only, do not schedule; analyze M6): retire the carried EF Core InMemory contexts and RabbitMQ `IModel` mocks (constitution IX:254/256) via Testcontainers in the Epic 3 window. Patron decides filing; Rigger records.

## Taste

- `[assumed]` A2 (analyze M3): the shared factory is `tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs`, class `LamuFlixContextFactory`, method `public static LamuFlixContext CreateContext()`. No "TBD" anywhere. Quill logs it in `ASSUMPTIONS.md`.

## Analyze round 1 adjudication (Keel, 2026-09-23; Patron `analyze.md` C0/H6/M7/L6)

All 19 are accepted. Only A1 (H5) and A2 (M3) change a decision. The rest restate rulings already above:
- H1/H2: R4 is still open. Versions are *candidate pins, pending R4*, never "exact from recon". `Microsoft.NET.Test.Sdk` stays 17.12.0 unless R4 shows it below the xunit.v3 floor (Packages, Kept). `OutputType Exe` is conditional on R4 in spec and tasks alike (M2).
- H3/H4/H6: N2 and N3 as written.
- M1: `PackageVersion` for InMemory is kept; the `LamuFlix.Test` `PackageReference` is decided in T010 (Q1 declaration rule).
- L2/L4: no change.
- *(Superseded 2026-09-23 by R4 close: pins are hardened, not candidates; Test.Sdk stays 17.12.0; `OutputType Exe` is unconditional. See Packages.)*

## Round-2 freeze (Patron analyze, 2026-09-23)

- **Verdict: FREEZABLE**, C0 / H0 / M3 / L5. `analyze.md` SHA256 `AC0BAA67…` (round 2 of 2, final). Plan frozen.
- **H7** (the AC3 backslash grep was vacuous because BRE reads `\F` as `F`) was **fixed by Patron** in `spec.md` and `tasks.md`: `git grep -F "F:\Filmes" …`.
- **Gate 1 stays CLOSED** (`gate1: provisional`) on owner checkbox 1 (FR-008).
- **Pending:** T002 pickup drift check at Phase B.
Keel: facts I used to produce this brief: worktree feature/280-spec @ d1d9cfa (origin/main now b2659b7, PR #9 = DEV-290 Phase A only, no tests/ or src/ on main); recon-DEV-280 (Directory.Packages.props: Microsoft.NET.Test.Sdk 17.12.0, MSTest.TestAdapter 3.6.4, MSTest.TestFramework 3.6.4, Moq 4.20.72; no xUnit/NSubstitute/Shouldly/AutoFixture); Patron R2 (Moq/MSTest only in LamuFlix.Test.csproj:12-14 + Directory.Packages.props:18-20); baseline 18 passed/4 skipped/0 failed at 450e70f (DEV-290 spec.md:50) = Worker 5 + Enrichment 10 + UnitTest1 3; UnitTest1.cs 7 tests, [Ignore] TestMethod1/2/4/5, Process.Start only UnitTest1.cs:358; shared InMemory factories UnitTest1.cs:490 + EnrichmentTests.cs:109; constitution IX 258-276 + 304 + 315-316 (xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net; MSTest/Moq/FluentAssertions/Bogus forbidden); R4 versions + R6 construct inventory still open.
