# DEV-280 grill conclusions

One Conductor ask and one Patron exchange (round 1, 2026-09-23), out of 12 allowed. Patron worked from `origin/main` `b2659b7`, using the ticket *Scope & Technical Design* (`scripts/youtrack-plan.json`), `constitution.md` IX and 294–297, `specs/PRODUCT.md` 32–41, and `specs/DEV-290/spec.md` FR-004 / SC-002.

## Settled from the documents (no question asked)

| Item | Ruling | Source |
|---|---|---|
| AutoFixture vs Faker.Net | Both. They have different jobs: anonymous data vs realistic data | `constitution.md` IX (272–274); ticket Scope |
| Package placement | Central Package Management in `Directory.Packages.props` | recon-DEV-280 #2 |
| `Tests.Common` vs `LamuFlix.UnitTests` naming | The ticket-named `tests/LamuFlix.Tests.Common/` stands; the constitution's names describe the Epic 1 target layout | ticket Scope; `constitution.md:61-63` |
| New packages / project | Ticket-named, so decided; no Gate 1 checkbox | §2.3 "ticket text counts as decided" |

## Q1 — Tests.Common contents
**Asked:** what goes in `tests/LamuFlix.Tests.Common/` when IX reserves hand-written builders for invariants that AutoFixture cannot satisfy?
**A1 (Patron, approved):** only helpers the migrated tests already share. Concretely, the EF in-memory `LamuFlixContext` factory (`UnitTest1.cs:490`, `EnrichmentTests.cs:109`). No speculative builders. `Tests.Common` declares only the packages its own code consumes. `LamuFlix.Test` gets a `ProjectReference` to it. It is not added to the `.sln`.

## Q2 — Package set
**Asked:** add every ticket-named package, or only the ones the code consumes? What about AutoFixture.Xunit3 and NSubstitute.Analyzers?
**A2 (Patron, approved):** the six ticket-named packages are decided. `AutoFixture.Xunit3` and `NSubstitute.Analyzers` are not ticket-named, so adding either would be §2.3 item 1, and both are **excluded**. Consequence: no `[AutoData]`; data comes from `new Fixture().Create<T>()` plus Faker.Net through `[Theory]` + `[MemberData]`. `Microsoft.NET.Test.Sdk` is the VSTest SDK, not an MSTest package, so it stays.

## Q3 — Moq and MSTest removal
**Asked:** is removing Moq and MSTest.* in scope?
**A3 (Patron, in scope):** yes, it is ticket-decided. R2 shows the only consumers are `LamuFlix.Test.csproj:12-14` and `Directory.Packages.props:18-20`. Remove the 3 `PackageReference`s, the 3 `PackageVersion`s, and the `using` lines. Keep `Microsoft.NET.Test.Sdk`, `Microsoft.EntityFrameworkCore.InMemory`, `RabbitMQ.Client`. If R2 changes at pickup, stop and report.

## Q4 — Acceptance
**Asked:** are "xUnit v3 discovers all tests", zero MSTest refs, and no disk access enough?
**A4 (Patron, amended):** the base list stands, with six changes:
- (a) Pin the count at 18 passed / 0 skipped / 0 failed.
- (b) Reaching 18 means migrating the 3 non-ignored `UnitTest1` tests.
- (c) Replace the bare `F:` grep with "no disk I/O and no `F:/Filmes` / `F:\Filmes` literal". *(Amended by N1 and N1(a), 2026-09-23.)* The exception is scoped by pattern, not by file. Inert drive-prefixed `Location` data is allowed anywhere under `tests/`, and the ban covers exactly the two Filmes literal forms, which must return zero hits under `tests/`. Evidence that the exception has real members: the six bare-F EnrichmentTests `Location` strings, cited by test member name in the spec because the migration moves line numbers (base `:130,168,209,242,280,322`). The two retained UnitTest1 `F:\Filmes` literals are **not** covered by the exception; they are rewritten (N1). The `Process.Start` token only appears in deleted `TestMethod4` (`:358`).
- (d) Shouldly-only assertions except `Assert.Collection`. Zero Moq / Mock / MSTest attributes anywhere.
- (e) The gates must exit 0. SKIPPED (exit 2) is not a pass.
- (f) Zero new warnings. Private helpers orphaned by the deletions are removed in the same change (IDE0051). `ApiDataModel.cs` is left unchanged and recorded as a follow-up.

## Q5 — Loop terms
**Asked:** closing bar, frozen scope, cap, Gherkin?
**A5 (Patron, approved):** the bar is Critical/High with a concrete failure. Frozen scope = the ticket Scope. Cap is 2. Gherkin is opted out. The existing EF InMemory contexts and RabbitMQ `IModel` mocks are carried debt: not findings, not fixed, not extended.

## D1 — Dependency on DEV-290 (Keel ruling, amended by Patron)
My first ruling gated Phase B on PR #9. Patron corrected it: PR #9 was DEV-290 Phase A (spec) only, and `origin/main` `b2659b7` still has `LamuFlix.Test/` at the root.
**Ruling:** spec, plan, and tasks use the post-290 paths (`tests/LamuFlix.Test/`, `tests/LamuFlix.Tests.Common/`). Phase B is gated on the DEV-290 *restructure* PR being merged, proven by `tests/LamuFlix.Test/LamuFlix.Test.csproj` on `origin/main`. Before the Phase A freeze, `feature/280-spec` is rebased onto `origin/main`; this is an operational step, not a plan change. Building DEV-280 before DEV-290 Phase B would reorder planned work, so that goes to the user.

## D2 — Declared vs referenced packages (Keel)
The six ticket-named `PackageVersion`s land in `Directory.Packages.props`. A project gets a `PackageReference` only when its own code uses the package. This reconciles A2 ("all six decided") with A1's declaration rule, and it does not force AutoFixture/Faker.Net into tests that are faithful 1:1 migrations.

## D3 — Orphaned helpers (Keel, from A4 (f))
Deleting `TestMethod1/2/4/5` also deletes every private member of `UnitTest1` that only those four reference. Standalone files (`ApiDataModel.cs`) are not named in the ticket, so they stay. They become a follow-up, never a review finding.

## D4 — Non-ignored UnitTest1 tests (OPEN, owner)
The ticket does not name the 3 `AssistirFilme_*` tests (`UnitTest1.cs:498,517,560`). Migrating them touches `Features:LocalPlay` / `ProcessStartInfo` (§2.3 item 5). Deleting them removes unnamed tests (§2.3 item 6). Neither may be assumed. **Recommendation: option A (migrate, preserving assertions and the `ProcessStarter` seam; production unchanged).** This goes on the spec PR as an owner checkbox; `gate1: provisional`.

## N1 — Filmes literals in retained UnitTest1 tests (Patron, round 3, final, 2026-09-23)
**Asked:** AC3 bans `F:/Filmes` and `F:\Filmes`, but the two retained `AssistirFilme_*` tests carry `F:\Filmes` `Location` literals. Should the ban be amended to allow them, or kept?
**Ruling: KEEP the ban and rewrite the two literals.** The AMEND recommendation assumed that changing the literals would change assertions. That premise is false. Base `UnitTest1.cs:534` and `:576` are `Location` initializers, and every assertion compares against `movie.Location` (`:556` `Assert.AreEqual(movie.Location, capturedStartInfo.ArgumentList[0])`; `:596` `Assert.AreEqual(movie.Location, capturedStartInfo.FileName)`). No assertion names an F:\Filmes string, so rewriting them needs **zero assertion edits** and keeps 1:1 fidelity. AMEND would instead carry the Principle VII violation that the constitution forbids copying (`constitution.md:196-199`, `:430-431`) forward into the file being rewritten.
- `:534` → `@"C:\TestLibrary\Test[2020]\test.mkv"`; `:576` → `@"C:\TestLibrary\Test[2020]\test.mp4"`. The values are taste: any equivalent synthetic non-Filmes value is acceptable, logged `[assumed]` in `ASSUMPTIONS.md` if it differs.
- Base grep for the two literal forms over `LamuFlix.Test/*.cs` finds 4 hits, all in `UnitTest1.cs` (`:27`, `:349`, `:534`, `:576`), and 0 in `EnrichmentTests.cs`. `:27` and `:349` go with the deleted tests. Head target: **0**. AC3 stays a crisp, non-vacuous check.
- **(a)** EnrichmentTests' bare-F `Location` strings stay as inert data, and no separate amendment is needed. The AC3 note is reworded from file-scoped to pattern-scoped (see Q4 (c)).

## N2 — Baseline vs post-change counts (Patron, round 3, 2026-09-23)
**Ruling (confirmed):** the pickup baseline is measured before any change and is **always 18 passed / 4 skipped / 0 failed**, whichever way checkbox 1 is ticked. The option decides only what is deleted afterwards. 15 is never a baseline; it is only the **Option B post-change** passed count. Any figure of 15 must say so and sit next to the **Option A post-change** figure, 18 passed / 0 skipped / 0 failed.

## N3 — Option B is not executable under the frozen plan (Patron, round 3, 2026-09-23)
**Ruling (amended):** plan.md and tasks.md describe Option A only. Ticking Option B drops 3 tests from the suite, which drops planned work. So Option B needs a **Patron plan amendment before any edit**. The rule must be written where the implementer reads it, not only here: (1) spec FR-008 and the checkbox-1 block; (2) the tasks.md pickup stop-rule (the T003 stop-and-report family): *if the owner ticks checkbox 1 Option B, stop and report to Patron; do not delete the 3 tests under the frozen plan.* Quill writes those.
Also confirmed: spec:153 and T008 file the `:534`/`:576` literals under EnrichmentTests by mistake. Correct them to `UnitTest1.cs` and cite both tests by member name (`AssistirFilme_WhenLocalPlayEnabledAndPlayerConfigured_StartsProcessWithConfiguredPlayerAndArgumentList`, `AssistirFilme_WhenLocalPlayEnabledAndPlayerNull_DefaultsToOsAssociation`). Cite the AC3 EnrichmentTests list by member name in the same way.

## Usage notes for Quill

- `brief.md` is authoritative. Draft spec, plan, and tasks from it only. Raise gaps as `needs decision:` to Keel.
- ~~Package versions stay `<latest stable, recon R4>` until recon R4 returns. Do not guess them.~~ *(Retired 2026-09-23: R4 closed, pins hardened; see R4 below and `brief.md` Packages.)*
- Translation table rows: include only the constructs recon R6 finds.
- Write every path in post-290 form.
- Do not describe checkbox 1 as decided anywhere in the spec. Write FR text for option A with the checkbox reference.

## Recon still open

| ID | Question | Blocks |
|---|---|---|
| R4 | **CLOSED 2026-09-23.** (a) Pins hardened, latest stable with provenance (recon item 7 + Patron NuGet re-verification): `xunit.v3` 4.0.1, `xunit.runner.visualstudio` 4.0.0, `NSubstitute` 6.2.0, `Shouldly` 4.3.0, `AutoFixture` 4.18.1, `Faker.Net` 2.0.163; `Microsoft.NET.Test.Sdk` latest stable is 18.10.1 (recorded for provenance only). (b) `Microsoft.NET.Test.Sdk` **stays 17.12.0**: it is above the xunit.v3 floor; the bump branch is deleted. (c) `OutputType Exe` is **unconditional** on `LamuFlix.Test.csproj`. | ~~version cells in plan.md~~ nothing |
| R6 | Inventory of MSTest/Moq constructs in `WorkerTests.cs` + `EnrichmentTests.cs` | translation table rows |
| R7 | Moot: DEV-290 Phase B is unexecuted. Re-check at the pickup drift check whether the restructure edited `LamuFlix.Test` contents or only moved it | pickup drift check |

## Round-2 freeze (Patron analyze, 2026-09-23)

- **Verdict:** FREEZABLE, C0 / H0 / M3 / L5. `analyze.md` SHA256 `AC0BAA67…` (round 2 of 2, final).
- **H7** (vacuous AC3 backslash grep: BRE reads `\F` as `F`) was raised and **fixed by Patron** in `spec.md` and `tasks.md` to `git grep -F "F:\Filmes" …`.
- **Gate 1 stays CLOSED** (`gate1: provisional`) until the owner ticks checkbox 1 (FR-008).
- **Pending:** T002 pickup drift check at Phase B.
