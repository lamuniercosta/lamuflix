# Implementation Plan: DEV-289 — Build-configuration hardening (CPM, strict compilation, banned ambient time)

**Branch:** `claude/ticket-scope-cs-edits-3ed088` (base `origin/main` @ `aa02408`) | **Date:** 2026-09-22 | **Spec:** `specs/DEV-289/spec.md`

**Input:** Feature specification from `specs/DEV-289/spec.md`

**Note:** Authored during Phase A (spec PR). The installed Full SDD Cycle (`specify workflow run speckit`) cannot be executed to completion non-interactively — its two `gate` steps require a TTY, and its final step is `speckit.implement`, which this ticket's brief forbids (no implementation in the spec PR). The constituent SpecKit commands were therefore executed directly with the installed `.specify/scripts/powershell/*` scripts and `.specify/templates/*` templates; see `specs/DEV-289/analyze.md`, "Spec Kit workflow execution".

## Summary

Establish central tool and package version management, compiler enforcement, and banned-symbol analysis
for `LamuFlix.sln`, per YouTrack DEV-289. Five root configuration artifacts land (`global.json`,
`Directory.Packages.props`, `Directory.Build.props`, `.editorconfig`, `BannedSymbols.txt`) and the 14
`Version` attributes across the four `.csproj` files are removed (plus one in `Directory.Build.props`).
The ticket's acceptance criteria — green build with CPM on, a `DateTime.Now` compile error, and
warnings-as-errors + nullable everywhere — are **only satisfiable once the owner's pending Scope item 6
amendment authorizes the 35-file B1 remediation** (`CONCLUSIONS.md` B1, option (a)). This spec PR is
documentation-only: it changes no build file and no `.cs` file.

## Technical Context

**Language/Version:** C# / .NET 10 (`net10.0` for all four projects); SDK pinned exactly to **10.0.400**
(`rollForward: latestPatch`, `allowPrerelease: false` — `CONCLUSIONS.md` D4(a)).
**Primary Dependencies:** none added to *production* code. Two analyzer/build packages are configured:
`Microsoft.CodeAnalysis.NetAnalyzers` (already present in `Directory.Build.props`, 9.0.0) and
`Microsoft.CodeAnalysis.BannedApiAnalyzers` (ticket Scope item 5), pinned to the ticket-approved **3.3.4**
(`CONCLUSIONS.md` D4(b)). Existing package versions to be
centralised: Pomelo.EntityFrameworkCore.MySql 9.0.0, Microsoft.EntityFrameworkCore(.InMemory) 9.0.0,
Microsoft.Extensions.Hosting/Http 10.0.1, RabbitMQ.Client 6.8.1, Newtonsoft.Json 13.0.3,
Microsoft.NET.Test.Sdk 17.12.0, MSTest 3.6.4, Moq 4.20.72, Microsoft.CodeAnalysis.NetAnalyzers 9.0.0.
**Storage:** N/A (no schema change)
**Testing:** none added. Acceptance tests opted OUT (build-tooling ticket); existing MSTest project is
untouched. See spec.md §Assumptions.
**Target Platform:** Windows dev workstation + CI; MSBuild/`dotnet build`
**Project Type:** single-solution multi-project (`LamuFlix.sln`), 4 projects
**Performance Goals:** N/A (build reproducibility, not runtime)
**Constraints:** no `.cs` edits in this PR; the 35-file remediation (34 `.cs` + 1 Razor view) is blocked on
Scope item 6; **enforcement configuration (banned-symbol wiring, warnings-as-errors) must not be merged
before that amendment is live**, because the green build requires the remediation; a **non-incremental**
build is the only accepted green proof; frozen scope B4
**Scale/Scope:** 5 configuration files (2 new, 3 edited), 4 `.csproj` `Version` removals (14 attributes),
1 `Directory.Build.props` `Version` removal, 35 gated source files (34 `.cs` + 1 Razor view)

## Constitution Check

*GATE: Must pass before implementation. Re-check after the strict settings land.*

The repository's governing rules are `.specify/memory/constitution.md` (v1.0.0), `specs/PRODUCT.md` §5,
`AGENTS.md`, and `harness.yml`.

- **Principle VII — Deterministic Time — PASS (design).** The plan wires `BannedSymbols.txt` +
  `BannedApiAnalyzers` so the `DateTime.Now` family is a build error and the three call sites inject
  `TimeProvider` (restricted by B1 to the authorized 35 files). No `DateTime.Now` is introduced.
- **Technology Stack Constraints — PASS (design).** The plan implements exactly the constitution's
  runtime row: `global.json` pinned with `rollForward: latestPatch`, CPM, `Nullable` enable,
  `TreatWarningsAsErrors` true. No forbidden choice is introduced; the existing Pomelo/MSTest/Moq/Newtonsoft
  usages are Known Technical Debt, centralised not extended.
- **§2.3 / PRODUCT.md §5 structural list — PASS, with one open owner checkbox (B1).** `global.json`,
  `Directory.Packages.props`, `Directory.Build.props`, `.editorconfig`, `BannedSymbols.txt` and the four
  `.csproj` are ticket-named (Scope items 1–5), and `BannedApiAnalyzers` is ticket-named (Scope item 5) —
  no new checkbox for them. **The 35-file edit set is §5 item 6 (File Scope) and is the open gate-1
  checkbox B1; implementation of those files is blocked until the YouTrack Scope item 6 amendment is live.**
- **Static-Analysis Gates — PASS (design).** The change makes the three gates non-vacuous (CA1502 reads its
  threshold via the existing `CodeMetricsConfig.txt` `AdditionalFiles`). `harness.yml`
  `analyzers.warningsAsErrors: false` is a *gate-script* setting; the AC's warnings-as-errors is the MSBuild
  property. They are deliberately separate: the build must be strict, while the gate scripts are invoked on
  the changed set at the implement stage and reported as they run (SKIPPED is never PASS).
- **Principle IX / Testing — N/A.** No tests are added or converted; acceptance tests opted out.
- **Never edit a generated file — PASS.** No generated file is touched.

**Result:** No constitution violations. Complexity Tracking is empty. The only outstanding gate-1 item is
B1's owner checkbox (Scope item 6), which this PR surfaces but does not resolve.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-289/
├── brief.md                 # Phase A grill brief + B1 appendix (PRESERVED verbatim)
├── CONCLUSIONS.md           # B1–B5 decision record (PRESERVED verbatim)
├── spec.md                  # This feature's specification
├── plan.md                  # This file
├── tasks.md                 # Task list
├── analyze.md               # Cross-artifact consistency analysis (this Phase A run)
└── checklists/
    └── requirements.md      # Spec quality checklist (speckit-specify step 7)
```

### Source Code (repository root)

```text
# NEW (ticket-named)
global.json                              # pin SDK 10.0.400, rollForward latestPatch, allowPrerelease false
Directory.Packages.props                 # CPM on; every package version centralised; BannedApiAnalyzers 3.3.4

# EDIT (ticket-named)
Directory.Build.props                    # TreatWarningsAsErrors=true, Nullable=enable, BannedApiAnalyzers 3.3.4 wired; Version removed
.editorconfig                            # repo-wide formatting + naming (file-scoped namespaces)
BannedSymbols.txt                        # 5 time symbols (messages → TimeProvider) + 2 Task rows preserved verbatim
LamuFlix.Web/LamuFlix.Web.csproj         # remove 2 Version attributes
LamuFlix.Data/LamuFlix.Data.csproj       # remove 1 Version attribute
LamuFlix.Work/LamuFlix.Worker.csproj     # remove 5 Version attributes (Nullable already enabled)
LamuFlix.Test/LamuFlix.Test.csproj       # remove 6 Version attributes

# GATED — Scope item 6 only (NOT authorized in this PR)
LamuFlix.Web/Controllers/FilmesController.cs   # TimeProvider injection at :28
LamuFlix.Work/Worker.cs                        # TimeProvider injection at :37,:57
+ 33 nullable/analyzer files listed in brief.md §Appendix
  (16 Data .cs + 14 Web source files incl. the Razor view Views/Filmes/Index.cshtml + 3 Test .cs)
  → 35 gated source files total: 34 .cs + 1 Razor view
```

**Structure Decision:** All five configuration artifacts live at the repository root, where MSBuild and the
analyzer discover them (CPM, `Directory.Build.props`, `global.json`) and where BannedApiAnalyzers expects
`BannedSymbols.txt` (registered as an `AdditionalFiles` input). No new project, layer, or top-level folder
is introduced beyond the two root files the ticket names. See spec.md §Authorization envelope.

## Reference-path / file impact

| File | Change | Today | After | Notes |
|---|---|---|---|---|
| `global.json` | new | absent | pins `10.0.400`, `rollForward: latestPatch`, `allowPrerelease: false` | ticket item 1 |
| `Directory.Packages.props` | new | absent | CPM on; all versions | receives 14 csproj versions + NetAnalyzers 9.0.0 + BannedApiAnalyzers 3.3.4 |
| `Directory.Build.props` | edit | `TreatWarningsAsErrors=false`; NetAnalyzers `Version="9.0.0"` | `true`; `Nullable=enable`; BannedApiAnalyzers 3.3.4 added; Version moved | ticket items 3, 5 |
| `.editorconfig` | edit | exists | formatting/naming/file-scoped namespaces | ticket item 4 |
| `BannedSymbols.txt` | edit | 5 rows (3 time + 2 Task); messages recommend `UtcNow` | 7 rows (5 time + 2 Task preserved verbatim); messages → `TimeProvider` | ticket item 5; B2 |
| `LamuFlix.Web.csproj` | edit | 2 `Version` attrs | none | ticket item 2 |
| `LamuFlix.Data.csproj` | edit | 1 `Version` attr | none | ticket item 2 |
| `LamuFlix.Worker.csproj` | edit | 5 `Version` attrs | none | ticket item 2 |
| `LamuFlix.Test.csproj` | edit | 6 `Version` attrs | none | ticket item 2 |

### B1 remediation envelope (gated — Scope item 6 only)

The gated set is **35 source files = 34 `.cs` + 1 Razor view** (`LamuFlix.Web/Views/Filmes/Index.cshtml`):
16 `LamuFlix.Data` `.cs`, 14 `LamuFlix.Web` source files (13 `.cs` + the Razor view), 3 `LamuFlix.Test`
`.cs`, plus the 2 banned-symbol files.

Authorized edits in the 35 files are exactly: `TimeProvider` injection + `GetUtcNow()`; `?`, `required`,
`= null!`, `= []`, null guards that preserve current behaviour; and the listed analyzer fixes (CS8981
lowercase type names, IDE0305 collection expressions, CA1822 static, **CA1502 complexity — by extraction
only**, SYSLIB0014 obsolete API). `TimeProvider` is injected as an optional constructor parameter
defaulting to `TimeProvider.System`, so no host `Program.cs`/`Startup.cs` edit is needed. Anything else in
those files is out of scope and a review finding (B4).

## Complexity Tracking

> No Constitution Check violations. Nothing to justify.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
