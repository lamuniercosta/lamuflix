# Implementation Plan: Restore six InspectCode warnings

**Branch:** `feature/361-spec` (Phase A re-spec; Phase B on `feature/DEV-361`) | **Date:** 2026-09-23 (revised for owner B1 answer and Patron T019 ruling) | **Spec:** [spec.md](spec.md)

## Summary

Restore six `.editorconfig` severities to `warning`. Five are ticket-named. The sixth, `nullable_warning_suppression_is_used`, is restored because the owner answered B1 **[x]** on PR #3 and the user confirmed it on 2026-09-23. This plan resolves:

- the 10 five-target hits in 3 files;
- the 112 `NullableWarningSuppressionIsUsed` hits in 18 files (re-baselined on `origin/main` `3cd5802` after DEV-360; 114 in 24 at `5a38215`);
- the 3 pre-existing WARNING+ hits in `WorkerTests.cs`, which is now an AC2 path.

That is 25 measured paths in total. After DEV-360, 19 of them need edits, and six Data model files have zero hits but stay in the AC2 `-Files` set. Then run InspectCode on those paths. B1 legacy sites are fixed first where FR-008 allows (AC3). Otherwise they get per-type/member suppressions that name the writer. Either way there is no entity nullability, schema, DTO shape, dependency, or layer change. The Round 1 freeze (T013) is superseded. This plan was re-frozen after the Round 2 challenge (T018, 2026-09-23; [plan-challenge-adjudication.md](plan-challenge-adjudication.md)).

## Technical Context

**Language/Version:** C# / .NET 10; `.editorconfig` InspectCode settings.  
**Primary Dependencies:** The existing JetBrains InspectCode tool. No new package or annotation dependency.  
**Storage:** N/A. No schema change or migration, and EF-mapped property nullability is frozen.  
**Testing:** WARNING+ InspectCode on the 25 paths. Roslyn, CA1502 at 15 and 6, format, and solution tests in Phase B.  
**Project Type:** The existing Data/Web/Work/Test solution. No new project.  
**Constraints:** Code edits only in the 25 FR-002 paths. Suppressions at type or member scope with a reason. No `#nullable disable`, file-scoped `.editorconfig`, or `[SuppressMessage]`.

## Constitution Check

| Principle | Application | Status |
|---|---|---|
| VIII — Ubiquitous Language in English (`constitution.md:217-232`); Known Technical Debt (`constitution.md:421-429`) | Keep existing identifiers and add no Portuguese ones. Suppression reasons are in English. Rigger records a separate Epic-1 language migration issue under T003, per Patron's C1 ruling. It is not added to the chain. The widened scope touches more Portuguese-named types (e.g. `FilmesServices`) but renames none. | Conflict reconciled by C1; follow-up outside ticket |
| IX — Test Pyramid (`constitution.md:237-264`); Known Technical Debt (`constitution.md:421-429`) | Simplify dead conditions and fix or, as a fallback, suppress legacy `!` in existing MSTest files, and add no MSTest tests. The WorkerTests `MethodHasAsyncOverload` fix changes a call inside an existing test only. Size stays M (Patron P2), so the L-only mutation gate does not apply. `DynamicQuery` helper extraction (T019) has no test coverage, and none can be added; see P1. | Pass in plan |
| Development Workflow — Pull Request Quality Gates / Static-Analysis Gates (`constitution.md:301-349`) | Restore six real warning severities. Use targeted, justified ReSharper suppressions for proven non-null writers only. Run all required gates on modified C# files. The threshold-6 gate has a measured pre-existing failure in `EntityExtensions.DynamicQuery` (12). It is fixed by helper extraction (T019), as `constitution.md:344-345` requires. An unremedied failure is a hard stop. | Pass in plan, subject to the owner (A)/(B) choice |
| Charter §2.3 (outside constitution) | No new package, project, layer, schema, or API. The B1 checkbox is answered by the owner. File scope is the owner-authorized 25 paths. | Pass |

## Phase 0: Research

[research.md](research.md) has the Phase A five-target probe: 10 hits in 3 files, plus `Details.cshtml:60` routed to DEV-281 (that hit no longer fires at `3cd5802`). It also has the Phase B B1 inventory. The current baseline is `-All -MinSeverity SUGGESTION` on unchanged `origin/main` `3cd5802` (DEV-360 merged): exit 1, 451 total, 39 WARNING+ (`Details.cshtml:68 AssignNullToNotNullAttribute` replaces `:60`), 10 five-target hits solution-wide, and 112 B1 hits in 18 `.cs` files, none in `.cshtml`. `dotnet format` exits 0, and `dotnet test` passes 18, skips 4, and fails 0. The `5a38215` inventory (114 in 24) is kept in `research.md` for the record. The expanded-scope unchanged-source baselines, identical at both SHAs, are:

- InspectCode WARNING: exit 1, the 3 WorkerTests hits;
- Roslyn: exit 0;
- CA1502 at 15: exit 0;
- CA1502 at 6: exit 1, `EntityExtensions.cs:13 DynamicQuery` at 12.

## Phase 1: Design

No new data model or contract. For the five-target hits, Q2–Q4 in [CONCLUSIONS.md](CONCLUSIONS.md) still apply. B1 sites are resolved by group:

| Group | Files / hits | Resolution (fallback when no FR-008 fix exists) | Reason to name |
|---|---|---|---|
| D — Data | 5 / 18 (`LamuFlixContext`, `MovieActors`, `MovieDirectors`, `MovieGenre`, `Temp`) | Per-type suppression bracket around the entity/`DbContext` class. Change no property's nullability; DEV-360 already aligned it with the snapshot. Do not convert `DbSet` properties. | EF Core `DbSet` initialization / entity materialization |
| T — Test | 3 / 34 | `ApiDataModel`: per-type suppression. `UnitTest1`: per-member suppressions (`:149-153`, reflection `:424-453`; DEV-360 removed the `:30` fixture hit). `WorkerTests`: per-member suppressions around the two `null!` arguments. Also fix WorkerTests `:1`, `:11`, `:75` WARNING+. | Newtonsoft.Json deserialization; deliberate `null` to assert `ArgumentNullException`; reflection lookup of a known member |
| M — Web models | 6 / 40 | Per-type suppression after checking the writer for each type. Search for in-product assignments and cite them at `file:line`: `FilmesController.cs:97,103,108` for `AlertModel`, `HomeController.cs:40` for `ErrorViewModel`, `FilmesServices.cs:151,350` for `QueryableResult`. Otherwise cite the `[FromQuery]`/form binder. `FilmesViewModel.cs` may need more than one bracket where it declares several types. | MVC model binder, or in-product initializer at `file:line` |
| W — Web code | 4 / 20 | Tag helpers: per-member suppression. `EntityExtensions.cs:46,48,75` (`DynamicQuery`; these land in the T019 helpers) and `:125` (`DynamicSort`), and `FilmesServices.cs` (12: `:74,75,126,136,181,304,308,314×2,375,388×2`): per-member suppression. No `?? throw` guards, because they add CA1502 complexity and change the failure exception type. `AssistirFilme` (`:74,75`) gets a comment-only bracket, and none of its executable lines change (Patron item-5 ruling). | Razor attribute binding / `[ViewContext]` activation; reflection result on a known member; configuration injected by DI; nullable-column read, where the `!` preserves the pre-DEV-360 contract (DEV-360 FR-004) |

Fix first (AC3, FR-008): each hit is fixed in code when a fix passes every FR-008 condition. The table's suppression is the fallback for a hit that cannot be fixed without changing reachable binding or deserialization behavior. DEV-360 already resolved the old fix candidate, `FilmesServices.cs:106`, upstream. It added 11 `!` reads of nullable columns. Most of them are in EF expression trees, and none has an obvious FR-008 fix: `?? string.Empty` changes the value, and `!= null &&` adds complexity and changes the predicate. So the expected path is the bracket, and T017 still checks each hit.

## Implementation sequence

1. **Pickup (T001).** Compare `origin/main` with `3cd5802` (the re-baseline SHA) for drift in `.editorconfig`, the 25 paths, and `specs/DEV-361/`. The measurements taken on `3cd5802` (`research.md` "Re-baseline after DEV-360") are the expected values: the four baselines, `-All -MinSeverity SUGGESTION` with 112 B1 hits in 18 files and 10 five-target hits, `dotnet format` exit 0, and `dotnet test` 18/4/0. If `origin/main` moved and touches those paths, or if any count or path differs, stop and report drift to Keel before editing.
2. **Severities (T004).** Set the six keys to `warning`. Rewrite the rationale comments at `.editorconfig:67,69,72,74` (wording per `ASSUMPTIONS.md`).
3. Fix `TagHelpers/Extensions.cs:43,56` (T005). Simplify the four condition sites in `UnitTest1` (T006). Classify and resolve the three `FilmesViewModel` five-target hits (T007–T009).
4. Resolve the B1 groups by owner task: T014 D, T015 T (including the WorkerTests WARNING+ fixes), T016 M, T017 W. T014, T016's non-`FilmesViewModel` files, and T017 touch disjoint files and can run in parallel after T004. `UnitTest1.cs` (T006 → T015) and `FilmesViewModel.cs` (T007–T009 → T016) are shared, so those tasks run in sequence, per the `tasks.md` dependency section (Sentry R2-F3).
5. **Refactor gate fix (T019, owner choice (A)).** After T017, and before T010–T012, extract `DynamicQuery` helpers as a pure move (P1) so the threshold-6 run on `EntityExtensions.cs` exits 0. If it cannot, stop (hard stop).
6. **AC2 (T010).** Run `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files $files` at the default WARNING level on the 25 paths. Record exit 0. Then run the read-only `-All -MinSeverity WARNING` inventory. Route only out-of-scope hits to DEV-281, and tell Patron that the three WorkerTests hits have left that inventory.
7. **Gates (T012).** Run in sequence with the 25-path `$files`:
   - `run-roslyn-analyzers.ps1`;
   - `run-cyclomatic-complexity.ps1` at 15;
   - `run-cyclomatic-complexity.ps1 -Threshold 6`, which must exit 0 after T019;
   - `run-jetbrains-inspectcode.ps1 -MinSeverity WARNING`;
   - `dotnet format --verify-no-changes`;
   - `dotnet test`.

   Record numeric exits and findings. Exit 2 (SKIPPED) is not a pass. Do not suppress CA1502 or change thresholds.

## Patron rulings and owner plan-change choice

- **P1 — threshold-6 gate on `EntityExtensions.cs` (Patron ruled, 2026-09-23).** The script filters by file, so any edit to this file surfaces the pre-existing `DynamicQuery` at complexity 12 (measured exit 1).
  - The Round 1 three-file clause is **superseded** for the expanded 25-path modified-file set. That clause said: no refactor of unchanged methods, no baseline exception, stop and report (`research.md`, Phase A baseline).
  - T019 helper extraction is **constitution-mandated** (`constitution.md:338,344-345`): complexity failures MUST be fixed by extracting private helpers, early returns, and guard clauses, and not by suppression. No baseline exception is available.
  - A failure that the extraction cannot remedy (any method still >6 after a pure move) remains a **hard stop**. Report `blocked` with the method, its complexity, and the numeric exit.

  **Plan:** T019 extracts `DynamicQuery`'s per-property loop body, and the collection and scalar predicate branches under it, into private static helpers inside `EntityExtensions.cs`. That brings every method in the file to ≤6. The extraction is a **pure move**: no reordering of the `Expression.*` construction, no renamed locals, and no new branch. It adds no new file, type, or layer, and the public signatures and callers are unchanged (`DynamicQuery` at `FilmesServices.cs:166`; `DynamicSort` at `:171,364`).

  **Feasibility desk-check (Sentry R2-F1).** Measured at `main` `5a38215`. `EntityExtensions.cs` is unchanged at `3cd5802`, where it still measures CA1502 12. The target split is:
  - `DynamicQuery` keeps the guards `:15`, `:22`, the `foreach` `:26`, and `:28`, then calls a per-property helper. That is 5.
  - The per-property helper holds `:30-42` and the `:44` branch with its `&&`. That is 3.
  - The collection helper holds `:46-69` (if `:50` with `&&`, plus the `&&` inside the `:62` lambda). That is 4.
  - The scalar helper holds `:73-90` (if `:73`, plus the `&&` inside the `:76` lambda). That is 3.

  These counts include lambda `&&` in the enclosing method. That is confirmed by reconciliation: the ten statement branch points plus the two lambda `&&` equal the measured 12. So every method is ≤6. T019's CA1502 run is the measurement, and the hard stop stands.

  The `:40-42` locals `equal`, `lambda`, and `lambda1` span both branches. Each is therefore declared where it is first assigned inside its helper. That is the only permitted change to a local, and it is not a reorder.

  If `--color-moved=zebra` shows no clean moves, the receipt instead attaches `git diff --color-moved=zebra --color-moved-ws=allow-indentation-change origin/main -- LamuFlix.Web/Extensions/EntityExtensions.cs` plus a table mapping each original line range to its helper.

  **Known gap:** no test exercises the production `DynamicQuery`. The `UnitTest1.cs:395` copy (`:387` at `5a38215`) is separate and `[Ignore]`d, and new MSTest tests are barred (`constitution.md:427-429`). So behavior preservation is evidenced by the pure-move diff (`git diff --color-moved=zebra`) and review. It is not a test. Patron may file a coverage follow-up, as was done for `QueryStringBuilder`.
- **Owner plan-change choice (spec PR checkbox).** T019 adds planned work, so the owner chooses:
  - **(A)** Include T019 same-file private-helper extraction in DEV-361. **Patron recommends (A).**
  - **(B)** Defer the extraction to a prerequisite follow-up ticket, and block the six-key restoration (T004 onward) until it merges.

  **Owner answered (A) on 2026-09-23**, relayed by the Conductor. T019 is in DEV-361, and Phase B proceeds without waiting for the re-spec PR to merge. The (B) path above is kept for the record only.
- **P2 — size (Patron ruled).** Stays **size:M**. The scope grew from 3 files and 10 hits to 25 files and 127 findings (114 B1 hits plus 3 WorkerTests hits; 125 findings at the `3cd5802` re-baseline, which is 10 + 112 + 3, with 19 files to edit), but the work is mechanical suppression plus the Round 1 fixes. There is no ADR, `/architect`, or L-only Stryker mutation gate.
- **P3 — DEV-281 inventory (Patron ruled, 2026-09-23).** Three of its 39 hits (WorkerTests `:1,:11,:75`) move into DEV-361 (FR-009). DEV-361 keeps the measured baseline of 39. The other 36 remain with the DEV-281/DEV-366 follow-up, and the three WorkerTests findings are counted once, in DEV-361 only. Rigger records the ticket-side wording under T003. There is no chain change and no user checkbox (Compass R2-F2).

- **Item-5 ruling, `AssistirFilme` (Patron ruled, 2026-09-23; re-baseline).** DEV-360 added `FilmesServices.cs:74,75`. §2.3 item 5 does not fire for a comment-only member bracket there. No executable line of `AssistirFilme` may change. There is no checkbox.

None of these adds a §2.3 structural item beyond the owner plan-change checkbox above. T019 is a gate fix inside an owner-authorized path.

## File impact boundary

`.editorconfig` and the 25 FR-002 paths only. No file-scoped `.editorconfig` suppression, `#nullable disable`, `[SuppressMessage]`, new package, API or DTO shape change, EF-mapped nullability change, migration, or edit anywhere else.

## Complexity Tracking

No constitutional violation or added abstraction is planned. The threshold-6 conflict on `DynamicQuery` is resolved by private helper extraction in the same file (P1, T019). That is the constitution's prescribed remedy and not a new abstraction. The Round 1 freeze (T013) is superseded. Round 2 (T018), the last allowed challenge round, is adjudicated and re-frozen.
