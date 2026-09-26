# Implementation Plan: Resolve 36 remaining InspectCode WARNING+ findings

**Branch:** `feature/366-spec` (Phase A spec sprint; Phase B implements on a separate worktree) | **Date:** 2026-09-25 | **Spec:** [spec.md](spec.md)

## Summary

Resolve all 36 WARNING+ findings in the DEV-366 frozen scope, as inventoried in `recon-DEV-366` (ID table lines 18–27, file table lines 31–43), which is an exact match of the DEV-361 Phase B baseline with zero drift. The `-All -MinSeverity WARNING` run exits 0 when every finding is either fixed in code or justified with a suppression in the form required by FR-009 (member-level `// ReSharper disable/restore` pair in `.cs` files; Razor comment pair in `.cshtml` files per D3) with a writer reason. `Html.IdNotResolved` is suppressed by justified suppression chosen to leave rendered markup unchanged (D4). No `.editorconfig` severity change, no new dependency, no unnamed file edit.

The 36 findings decompose across 8 inspection IDs and 11 files:

| Inspection ID | Count | Files |
|---|---:|---|
| `Html.IdNotResolved` | 18 | `Movies/Index.cshtml`, `Movies/Watchlist.cshtml` |
| `RedundantUsingDirective` | 7 | `GeneralConstants.cs`, `HomeController.cs` |
| `RedundantNullableDirective` | 3 | `MovieEnrichmentMessage.cs`, `IEnrichmentQueuePublisher.cs`, `RabbitMqEnrichmentQueuePublisher.cs` |
| `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` | 3 | `EnrichmentJobProcessor.cs` |
| `RedundantCast` | 2 | `PagedListing.cs` |
| `Razor.AssemblyNotResolved` | 1 | `_ViewImports.cshtml` |
| `AssignNullToNotNullAttribute` | 1 | `Movies/Details.cshtml` |
| `EmptyConstructor` | 1 | `GeneralConstants.cs` |

## Technical Context

**Language/Version:** C# / .NET 10; Razor views (`.cshtml`).  
**Primary Dependencies:** Existing JetBrains InspectCode tool. No new package or reference.  
**Storage:** No schema change or migration. No EF-mapped property change.  
**Testing:** Standard Phase B gates on modified files.  
**Project Type:** Existing Data/Web/Worker/Test solution. No new project.  
**Constraints:** Edits only in the 11 frozen-scope files. Suppressions at member scope with a reason. No `.editorconfig`, `[SuppressMessage]`, `#nullable disable`, or file-scoped sections.

## Constitution Check

| Principle | Application | Status |
|---|---|---|
| Static-Analysis Gates (`constitution.md:301-349`) | Resolve all WARNING+ findings. Use targeted, justified ReSharper suppressions where no code fix is feasible without changing behavior. Run all required gates on modified files. | Pass in plan |
| Charter §2.3 | No new package, project, layer, schema, or API. File scope is the 11 frozen-scope paths. No owner checkbox required (`brief.md` Q10). | Pass |

## Phase 0: Research

`recon-DEV-366` is the authoritative baseline. 36 findings at identical IDs, files, and lines. Zero drift against the DEV-361 Phase B baseline (`specs/DEV-361/research.md` lines 99–165). Worktree HEAD `97dc7348`. InspectCode `-All -MinSeverity WARNING` exits 1. No §2.3 trigger. Full evidence in `recon-DEV-366` lines 70–78.

## Phase 1: Design

No new data model or contract. Each finding is resolved by one of two mechanisms:

- **Code fix:** Remove the redundant construct (redundant using, nullable directive, cast, empty constructor) or correct the pattern that triggers the diagnostic (conditional access on non-nullable, null assignment to non-null). Preferred wherever the fix does not change observable behavior or public API shape.
- **Justified suppression:** Paired `// ReSharper disable <ID>` / `// ReSharper restore <ID>` at member level in `.cs` files, or the Razor comment pair in `.cshtml` files (FR-009; D3), with a one-line reason. Used only where a code fix would change behavior, a public API shape, or is not applicable. For `Html.IdNotResolved`: chosen to leave rendered markup unchanged (D4).

Per-finding resolution design:

| Inspection ID | Planned mechanism | Rationale |
|---|---|---|
| `RedundantUsingDirective` (7) | **Fix:** remove unused `using` directives | Purely mechanical; no behavior change |
| `RedundantNullableDirective` (3) | **Fix:** remove `#nullable` directives made redundant by project-level nullable settings | Purely mechanical; no behavior change |
| `RedundantCast` (2) | **Fix:** remove redundant casts in `PagedListing.cs:46` | Purely mechanical; no behavior change |
| `EmptyConstructor` (1) | **Fix:** remove the empty parameterless constructor in `GeneralConstants.cs:9` | Purely mechanical; no behavior change |
| `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` (3) | **Fix or suppress:** remove the conditional access (`?.`) where the non-null flow is clear; suppress with a member-level bracket naming the API contract if removal would change behavior | Depends on each site's context in `EnrichmentJobProcessor.cs:139,157,175` |
| `Html.IdNotResolved` (18) | **Justified suppression:** Razor comment pair `@* ReSharper disable Html.IdNotResolved — <reason> *@` … `@* ReSharper restore Html.IdNotResolved *@` wrapping the smallest contiguous run of flagged lines (D3). Expected runs: `Index.cshtml` 58–73 and 108–114; `Watchlist.cshtml` 19–34 and 69–75. | Justified suppression chosen to leave rendered markup unchanged (D4; brief Q6). Implementer has no discretion for this ID. |
| `Razor.AssemblyNotResolved` (1) | **Fix:** delete line 7 of `_ViewImports.cshtml` (`@addTagHelper *, AlertsTagHelper`). Line 7 is redundant: line 6 already registers `LamuFlix.Web`, which defines `AlertsTagHelper`; the bare `AlertsTagHelper` assembly token on line 7 does not resolve (`recon-DEV-366` lines 82–87, 91). No `.csproj` or reference change (D1; recon lines 89, 91). | Named AC2 item; must be resolved, not suppressed. |
| `AssignNullToNotNullAttribute` (1) | **Fix or suppress:** correct the null assignment at `Details.cshtml:68` or add a justified bracket naming the nullable flow (D3 form; brief Q6/D1) | Named AC2 item (`brief.md` Q2); must be resolved. Prefer code fix; justified suppression is the permitted fallback (D1). |

> [!NOTE]
> The fix-vs-suppress choice for `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` and `AssignNullToNotNullAttribute` is confirmed by the implementer during Phase B when each site is inspected. The plan authorizes both paths; the brief authorizes justified suppression (`brief.md` Q6).

## Implementation Sequence

1. **Pickup (T001).** Verify branch `feature/DEV-366` (Phase B worktree), check `origin/main` for drift against recon SHA `97dc7348`. Drift means either: (a) `git diff --name-only 97dc7348 origin/main` touches any of the 11 frozen-scope files, or (b) the baseline InspectCode `-All -MinSeverity WARNING` run reports a finding count, ID, file, or line different from the 36-finding `recon-DEV-366` inventory. Changes under `specs/DEV-366/` are expected and are not drift (D6). If drift is detected, stop and report before proceeding.

2. **Redundant constructs (T002–T004).** T002 and T004 share `GeneralConstants.cs` and are sequential (T004 follows T002 on that file). T003 touches disjoint files and runs in parallel with T002 after T001:
   - T002: Remove 7 `RedundantUsingDirective` hits (`GeneralConstants.cs:1,2,3`; `HomeController.cs:1,2,4,5`).
   - T003 [parallel with T002]: Remove 3 `RedundantNullableDirective` hits (`MovieEnrichmentMessage.cs:1`, `IEnrichmentQueuePublisher.cs:1`, `RabbitMqEnrichmentQueuePublisher.cs:1`).
   - T004 [after T002]: Remove 2 `RedundantCast` hits (`PagedListing.cs:46`) and 1 `EmptyConstructor` hit (`GeneralConstants.cs:9`).

3. **Conditional access (T005).** Fix or suppress the 3 `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` findings in `EnrichmentJobProcessor.cs:139,157,175`. Record fix vs. suppress decision for each site.

4. **AC2 targets (T006–T007).** In sequence:
   - T006: Resolve `Razor.AssemblyNotResolved` at `_ViewImports.cshtml:7` by deleting line 7 (`@addTagHelper *, AlertsTagHelper`). Line 7 is redundant: line 6 already registers `LamuFlix.Web`; the bare `AlertsTagHelper` assembly token on line 7 does not resolve (`recon-DEV-366` lines 82–87, 91). No suppression, no operand rewrite, no `.csproj` change (D1).
   - T007: Resolve `AssignNullToNotNullAttribute` at `Details.cshtml:68`. Prefer a code fix; justified suppression (D3 form) is the permitted fallback under Q6/D1.

5. **Html.IdNotResolved (T008).** Apply justified suppression to all 18 `Html.IdNotResolved` findings using Razor comment pairs (`@* ReSharper disable Html.IdNotResolved — <reason> *@` … `@* ReSharper restore Html.IdNotResolved *@`), wrapping the smallest contiguous run of flagged lines (D3; D4). Expected runs: `Index.cshtml` 58–73 (lines 58, 61, 64, 67, 70, 73) and 108–114 (lines 108, 111, 114); `Watchlist.cshtml` 19–34 (lines 19, 22, 25, 28, 31, 34) and 69–75 (lines 69, 72, 75).

6. **Verification (T009–T011).** After all fixes and suppressions:
   - T009: Run `-All -MinSeverity WARNING` and confirm exit 0. Record numeric exit and confirm no remaining findings. If any bracketed finding from T008 remains, stop and report to Keel (D3).
   - T010: Confirm `git diff origin/main --stat` lists only the 11 frozen-scope files. Verify no `.editorconfig`, `WorkerTests.cs`, NuGet, or API/DTO change in the diff.
   - T011: Run standard gates in sequence. Steps 1–4 take `-BaseRef origin/main -Files $files`: Roslyn, cyclomatic complexity, cyclomatic complexity refactor gate (`./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`, `constitution.md:360`), InspectCode (`-Files $files -MinSeverity WARNING`). Steps 5–7 do not: `dotnet format --verify-no-changes`, `dotnet test`, `dotnet list package --vulnerable` (Q9; `constitution.md:405`, expect clean). Record property-test opt-out in the DEV-366 note (Q9).

## File Impact Boundary

The 11 frozen-scope files only:

- `src/LamuFlix.Data/Constants/GeneralConstants.cs`
- `src/LamuFlix.Data/Models/MovieEnrichmentMessage.cs`
- `src/LamuFlix.Web/Controllers/HomeController.cs`
- `src/LamuFlix.Web/Models/Helper/PagedListing.cs`
- `src/LamuFlix.Web/Services/IEnrichmentQueuePublisher.cs`
- `src/LamuFlix.Web/Services/RabbitMqEnrichmentQueuePublisher.cs`
- `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs`
- `src/LamuFlix.Web/Views/_ViewImports.cshtml`
- `src/LamuFlix.Web/Views/Movies/Details.cshtml`
- `src/LamuFlix.Web/Views/Movies/Index.cshtml`
- `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml`

No `.editorconfig`, `WorkerTests.cs`, `.csproj`, or unnamed file is touched. Any need for a file outside this list is raised as `blocked: structural — <file/change>` for the owner (§2.3 #6, or §2.3 #1 for a package or assembly reference); it is never assumed or waived by the implementer (D7).

## Complexity Tracking

No constitutional complexity concern. None of the 11 files contains a method at or near a CA1502 threshold. Redundant-construct removal and conditional-access fixes reduce code, not increase it. No refactor gate is expected to fire. The refactor gate (`./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`, `constitution.md:360`) is still run as part of T011, expected exit 0 (D5).
