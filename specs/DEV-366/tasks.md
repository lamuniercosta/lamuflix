# Tasks: Resolve 36 remaining InspectCode WARNING+ findings

**Input:** [spec.md](spec.md), [plan.md](plan.md), `recon-DEV-366`  
**Branch:** spec on `feature/366-spec`. Phase B implements on `feature/DEV-366` (separate worktree) from `origin/main` at HEAD `97dc7348aa5aa02605ee6741825d72b1f24d0920` (`recon-DEV-366` lines 5, 72).

`$files` below is the 11-file frozen-scope list (`brief.md` lines 17–27; `recon-DEV-366` lines 31–43):

```powershell
$files = @(
  'src/LamuFlix.Data/Constants/GeneralConstants.cs',
  'src/LamuFlix.Data/Models/MovieEnrichmentMessage.cs',
  'src/LamuFlix.Web/Controllers/HomeController.cs',
  'src/LamuFlix.Web/Models/Helper/PagedListing.cs',
  'src/LamuFlix.Web/Services/IEnrichmentQueuePublisher.cs',
  'src/LamuFlix.Web/Services/RabbitMqEnrichmentQueuePublisher.cs',
  'src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs',
  'src/LamuFlix.Web/Views/_ViewImports.cshtml',
  'src/LamuFlix.Web/Views/Movies/Details.cshtml',
  'src/LamuFlix.Web/Views/Movies/Index.cshtml',
  'src/LamuFlix.Web/Views/Movies/Watchlist.cshtml')
```

## Phase 1: Pickup

- [ ] T001 [Process] Work in the Phase B worktree (`feature/DEV-366`). Verify branch with `git branch --show-current`. Run `git fetch origin` and check `git rev-parse origin/main`. If it differs from `97dc7348`, run `git diff --name-only 97dc7348 origin/main`. Drift means either: (a) that diff touches any of the 11 frozen-scope files, or (b) the baseline InspectCode run below reports a finding count or ID/file/line different from the 36-finding `recon-DEV-366` inventory. Changes under `specs/DEV-366/` are expected and are not drift (D6). Stop and report drift before proceeding.

  Run the baseline read-only InspectCode:

  ```powershell
  ./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING
  ```

  Expected: exit 1, exactly 36 findings matching the `recon-DEV-366` ID table (lines 18–27) and file table (lines 31–43). If count, ID, file, or line differs, stop and report drift.

  | Run | Expected exit | Expected result |
  |---|---:|---|
  | `InspectCode -All WARNING` | 1 | 36 findings across 11 files (IDs and lines per `recon-DEV-366`) |

  Record the numeric exit and finding count. A drift stop goes to Keel before T002 onward.

## Phase 2: Redundant constructs

T002 and T004 both edit `GeneralConstants.cs` and are sequential: T004 follows T002 on that file. T003 touches disjoint files and runs in parallel with T002 after T001.

- [ ] T002 [P][US1][FR-002] In `src/LamuFlix.Data/Constants/GeneralConstants.cs`, remove the 3 `RedundantUsingDirective` findings at lines 1, 2, and 3. In `src/LamuFlix.Web/Controllers/HomeController.cs`, remove the 4 `RedundantUsingDirective` findings at lines 1, 2, 4, and 5. Confirm no remaining `using` directive is in use (compile check). Total: 7 removals.

- [ ] T003 [P][US1][FR-003] Remove the `RedundantNullableDirective` at line 1 of each of these three files:
  - `src/LamuFlix.Data/Models/MovieEnrichmentMessage.cs`
  - `src/LamuFlix.Web/Services/IEnrichmentQueuePublisher.cs`
  - `src/LamuFlix.Web/Services/RabbitMqEnrichmentQueuePublisher.cs`

  Confirm the project-level nullable setting covers these files after removal.

- [ ] T004 [US1][FR-005][FR-008] In `src/LamuFlix.Web/Models/Helper/PagedListing.cs`, remove both `RedundantCast` findings at line 46. In `src/LamuFlix.Data/Constants/GeneralConstants.cs`, remove the `EmptyConstructor` finding at line 9 (T004 follows T002 on `GeneralConstants.cs`; they may be combined in a single commit on that file). Confirm the cast removals do not change the assigned type. Total: 3 removals.

## Phase 3: Conditional access

- [ ] T005 [US1][FR-004] In `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs`, inspect the 3 `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` findings at lines 139, 157, and 175. For each:
  - If the non-null flow is provable from the surrounding code, remove the conditional access (`?.` → `.`).
  - If removal would change observable behavior, apply a member-level `// ReSharper disable/restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` bracket with a one-line reason naming the API contract guarantee.

  Record fix vs. suppress decision per site in the receipt.

## Phase 4: AC2 targets

- [ ] T006 [US2][FR-006] Resolve `Razor.AssemblyNotResolved` at `src/LamuFlix.Web/Views/_ViewImports.cshtml:7` by **deleting line 7** (`@addTagHelper *, AlertsTagHelper`). Line 7 is redundant: line 6 already registers `LamuFlix.Web`, which defines `AlertsTagHelper`, and the bare `AlertsTagHelper` assembly token on line 7 does not resolve (`recon-DEV-366` lines 82–87, 91). Do not suppress. Do not rewrite the operand. Make no `.csproj` or assembly-reference change (D1; recon lines 89, 91). If T009 still reports the finding after the deletion, stop and report to Keel; do not fall back to suppression or another file. If the fix requires any file outside the 11 frozen-scope files, stop and report: **blocked: structural — `Razor.AssemblyNotResolved` fix requires a file outside the authorized 11 — list the required change and the target file** (D7; §2.3 #6).

- [ ] T007 [US2][FR-007] Resolve `AssignNullToNotNullAttribute` at `src/LamuFlix.Web/Views/Movies/Details.cshtml:68`. This is the corrected AC2 item (Q2). Prefer a code fix: correct the null assignment at line 68 without changing public API or DTO shape. If the null assignment must remain (e.g., external binding sets it post-construction), apply a paired Razor comment suppression bracket (`@* ReSharper disable AssignNullToNotNullAttribute — <reason> *@` … `@* ReSharper restore AssignNullToNotNullAttribute *@`) with a one-line reason (D3 form; brief Q6/D1).

  Record fix vs. suppress decision in the receipt.

## Phase 5: Html.IdNotResolved

- [ ] T008 [US1][FR-001][FR-009] Apply justified suppression to all 18 `Html.IdNotResolved` findings using paired Razor comments (`@* ReSharper disable Html.IdNotResolved — <reason> *@` … `@* ReSharper restore Html.IdNotResolved *@`), wrapping the smallest contiguous run of flagged lines, never the whole file (D3; FR-009; SC-005). Files, expected runs, and line ranges:
  - `src/LamuFlix.Web/Views/Movies/Index.cshtml`: run 58–73 (lines 58, 61, 64, 67, 70, 73) and run 108–114 (lines 108, 111, 114). 9 findings total.
  - `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml`: run 19–34 (lines 19, 22, 25, 28, 31, 34) and run 69–75 (lines 69, 72, 75). 9 findings total.

  Each bracket pair includes a one-line reason naming the Razor-generated id as the suppression rationale.

## Phase 6: Verification and review handoff

- [ ] T009 [US1][US2][SC-001][SC-002] **AC1 and AC2.** Run:

  ```powershell
  ./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING
  ```

  Record exit 0. Confirm the output contains no `AssignNullToNotNullAttribute` at `Details.cshtml:68` and no `Razor.AssemblyNotResolved` at `_ViewImports.cshtml:7`. If any finding remains, identify its ID/file/line and resolve before T010. If any bracketed finding from T008 remains in the output, stop and report to Keel; do not try another suppression form (D3).

- [ ] T010 [Process][SC-004][SC-005] Confirm `git diff origin/main --stat` lists only the 11 frozen-scope files. Verify:
  - No `.editorconfig` change.
  - No `WorkerTests.cs` change.
  - No NuGet package or `.csproj` change.
  - No public API or DTO property-set change.
  - No `[SuppressMessage]`, `#nullable disable`, or file-scoped suppression section.
  - Every suppression bracket carries a one-line reason.

  Record PASS or each deviation found.

- [ ] T011 [Process][SC-003] Run the standard Phase B gates in sequence. Steps 1–4 take `-BaseRef origin/main -Files $files`; steps 5–7 do not:

  1. `./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main -Files $files`
  2. `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files`
  3. `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6 -BaseRef origin/main -Files $files` (refactor gate; `constitution.md:360`) — run after step 2.
  4. `./scripts/run-jetbrains-inspectcode.ps1 -MinSeverity WARNING -BaseRef origin/main -Files $files`
  5. `dotnet format --verify-no-changes`
  6. `dotnet test`
  7. `dotnet list package --vulnerable` (Q9; `constitution.md:405`) — expect a clean result (no vulnerable packages reported).
  8. Record the property-test opt-out line in the DEV-366 note (Q9; `brief.md` Q9).

  Record numeric exits and diagnostic evidence. Exit 2 (SKIPPED) is not a pass. A gate failure that is also in the T001 baseline and outside `$files` goes to Patron; it is never waived by the implementer.

  | Gate | Expected result |
  |---|---:|
  | Roslyn | exit 0 |
  | Cyclomatic complexity | exit 0 |
  | Cyclomatic complexity (refactor, `-Threshold 6`) | exit 0 |
  | InspectCode WARNING (`$files`) | exit 0 |
  | `dotnet format` | exit 0 |
  | `dotnet test` | exit 0 |
  | `dotnet list package --vulnerable` | clean (no vulnerabilities) |

## Dependencies and Execution Order

T001 is read-only and runs first. T002 and T004 share `GeneralConstants.cs`: T004 follows T002 on that file; they may be combined in a single commit on it. T003 touches disjoint files and runs in parallel with T002 after T001. T005 runs independently after T001. T006 and T007 run independently after T001; they touch disjoint files. T008 runs independently after T001. T009–T011 follow all fixes and suppressions (T002–T008).

## Implementation Strategy

Fix first: remove redundant constructs (usings, nullable directives, casts, empty constructor) wherever the removal is purely mechanical. Apply code fixes to conditional access and null-assignment sites where the fix preserves behavior and does not change public API. Suppress only where no behavior-preserving code fix exists, using a paired `// ReSharper disable/restore` bracket with a writer reason (FR-009). The `Html.IdNotResolved` findings are suppressed by justified suppression (D4): this is Keel's plan decision under Q6, chosen to leave rendered markup unchanged; the implementer has no discretion for this ID. Any need for a file outside the 11 frozen-scope files is raised as `blocked: structural — <file/change>` for the owner (D7; §2.3 #6, or #1 for a package or assembly reference); it is never waived by the implementer.
