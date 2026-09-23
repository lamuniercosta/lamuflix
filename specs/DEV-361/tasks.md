# Tasks: Restore six InspectCode warnings

**Input:** [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [quickstart.md](quickstart.md)  
**Branch:** spec on `feature/361-spec`. Phase B implements on `feature/DEV-361` (worktree `F:\Dev\LamuFlix.worktrees\DEV-361`) from `main` `3cd5802` (DEV-360 merged; re-baseline after the T001 drift stop, 2026-09-23). Phase A was measured at `5a38215`.

`$files` below is the 25-path FR-002 list. At `3cd5802`, `Actor`, `Collection`, `Director`, `Genre`, `Movie`, and `Player.cs` have zero hits. They stay in `$files` for the gate, and no task edits them.

```powershell
$files = @(
  'LamuFlix.Data/LamuFlixContext.cs','LamuFlix.Data/Models/Actor.cs','LamuFlix.Data/Models/Collection.cs',
  'LamuFlix.Data/Models/Director.cs','LamuFlix.Data/Models/Genre.cs','LamuFlix.Data/Models/Movie.cs',
  'LamuFlix.Data/Models/MovieActors.cs','LamuFlix.Data/Models/MovieDirectors.cs','LamuFlix.Data/Models/MovieGenre.cs',
  'LamuFlix.Data/Models/Player.cs','LamuFlix.Data/Models/Temp.cs',
  'LamuFlix.Test/ApiDataModel.cs','LamuFlix.Test/UnitTest1.cs','LamuFlix.Test/WorkerTests.cs',
  'LamuFlix.Web/Extensions/EntityExtensions.cs','LamuFlix.Web/Models/AlertModel.cs','LamuFlix.Web/Models/ErrorViewModel.cs',
  'LamuFlix.Web/Models/Filmes/FilmesViewModel.cs','LamuFlix.Web/Models/FilterViewModel.cs',
  'LamuFlix.Web/Models/Helper/QueryableResult.cs','LamuFlix.Web/Models/Helper/QueryParams.cs',
  'LamuFlix.Web/Services/FilmesServices.cs','LamuFlix.Web/TagHelpers/AlertsTagHelper.cs',
  'LamuFlix.Web/TagHelpers/PaginationTagHelper.cs','LamuFlix.Web/TagHelpers/Extensions.cs')
```

## Phase A: Plan challenge before freeze `[PHASE A — BLOCKS ALL IMPLEMENTATION]`

- [x] T013 **[PHASE A]** [Process] Round 1 plan challenge (Sentry, Ledger, Compass) and freeze, 2026-09-23. Compass 0 and Ledger 0. Sentry F1 was rejected as a freeze blocker, F2 was resolved (no baseline exception), and F3 is [assumed]. **Superseded** by the owner's B1 scope change. T018 replaces it.
- [x] T020 **[OWNER — Gate 1]** Plan-change choice on the spec PR: **(A)** include T019 same-file private-helper extraction (Patron recommends), or **(B)** defer it to a prerequisite follow-up and block the six-key restoration (T004 onward). **Answered (A) by the owner on 2026-09-23**, relayed by the Conductor as the user's direct answer. T019 runs in DEV-361. The user instructed Phase B to proceed without waiting for the re-spec PR to merge, so Gate 1 is open.
- [x] T018 **[PHASE A — BLOCKS T004 ONWARD]** [Process] P1, P2, and P3 are ruled by Patron. The Round 2 challenge (Sentry, Ledger, Compass) was adjudicated by Keel, and the plan was **re-frozen 2026-09-23** (`plan-challenge-adjudication.md`). This was Round 2 of 2, and there is no Round 3. T020 is answered (A), so Gate 1 is open.

## Phase 1: Pickup and owner gate

- [x] T001 [Process] Work in `F:\Dev\LamuFlix.worktrees\DEV-361` (`feature/DEV-361`). (Receipt: byte-for-byte spec recopy verified from Phase A 8231f65, Get-FileHash match 11/11).

  **Analysis: consume, do not re-run.** Do not run `/speckit-analyze` or `check-prerequisites.ps1` in Phase B, because the script rejects the branch name `feature/DEV-361`. T001 consumes the official Phase A report, [analyze.md](analyze.md). That report was produced in `F:\Dev\LamuFlix.worktrees\feature-361-spec` on `feature/361-spec`, rebased on base `3cd5802`, with `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` exit 0. Its metrics and Critical/High counts are in the report. Record that report's path and base SHA in the receipt. Confirm that the Phase B copy of `specs/DEV-361/` matches Phase A byte for byte (`Get-FileHash` on each file). A mismatch stops T001 and goes to Keel.

  **Drift: path check.** Run `git fetch origin`, then check `git rev-parse origin/main`. If it is not `3cd5802`, run `git diff --name-only 3cd5802 origin/main`. (The `5a38215` → `3cd5802` drift from DEV-360 was stopped, adjudicated by Keel as updated baselines, and re-measured. `research.md` has the re-baseline section.) That diff must touch none of the following, or T001 stops and reports drift to Keel before T004:
  - `.editorconfig` (the six keys);
  - the 25 paths;
  - `specs/DEV-361/`.

  On unchanged source, run the following **in sequence**, each with `-BaseRef origin/main -Files $files`:
  1. `./scripts/run-jetbrains-inspectcode.ps1 -MinSeverity WARNING`;
  2. `./scripts/run-roslyn-analyzers.ps1`;
  3. `./scripts/run-cyclomatic-complexity.ps1` at 15;
  4. `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`.

  Then run the read-only `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity SUGGESTION`. Expected, per the `research.md` re-baseline on `3cd5802` (2026-09-23). These runs were already taken on `3cd5802`. If `origin/main` is still `3cd5802`, T001 records them and does not need to re-run them.

  | Run | Expected exit | Expected result |
  |---|---:|---|
  | InspectCode WARNING | 1 | `WorkerTests.cs:1 RedundantNullableDirective`, `:11 RedundantUsingDirective`, `:75 MethodHasAsyncOverload` |
  | Roslyn | 0 | none |
  | CA1502 at 15 | 0 | none |
  | CA1502 at 6 | 1 | `EntityExtensions.cs:13 DynamicQuery` 12, only |
  | `-All` SUGGESTION | 1 | 451 issues, 39 WARNING+ (`Details.cshtml:68 AssignNullToNotNullAttribute` replaces `:60`), 10 five-target hits, 112 `NullableWarningSuppressionIsUsed` hits in the 18 files listed in the `research.md` re-baseline |
  | `dotnet format --verify-no-changes` | 0 | clean baseline |
  | `dotnet test` | 0 | Passed 18, Skipped 4, Failed 0 |

  **Read-only T019 feasibility check (Sentry R2-F1).** On `origin/main`, confirm that `EntityExtensions.DynamicQuery` still has:
  - the guards `:15`, `:22`, `:28` and the `foreach` `:26`;
  - the collection/scalar dispatch at `:44`, with its inner ifs at `:50` and `:73`;
  - the shared locals `equal`, `lambda`, and `lambda1` at `:40-42`;
  - lambda `&&` at `:62` and `:76`.

  Confirm that its CA1502 value is 12, which reconciles with the `plan.md` desk-check. If the structure or the value differs, the 5/3/4/3 split is unproven: stop and report drift to Keel before T004. Edit nothing.

  Record IDs, paths, lines, and numeric exits. If any value differs, stop and report drift to Keel before T004. A pre-existing `dotnet format` or `dotnet test` failure outside the FR-002 paths is not fixed in DEV-361 (charter §2.3 item 6). Report it to Patron, and T012 compares against this baseline (Sentry R2-F2).
- [x] T002 **[OWNER — Gate 1]** B1 reversal answered **[x]** by the owner on merged PR #3. The user confirmed via the Conductor on 2026-09-23: reverse B1, restore `resharper_nullable_warning_suppression_is_used_highlighting` to `warning`, and widen past the three-file scope. This is now FR-007/FR-008 and T004/T014–T017.
- [x] T003 [Process] Rigger records these four items: (Verified read-backs: DEV-361 comment posted, DEV-369 created, DEV-370 created).
  - Patron's DEV-361 ruling and recon comment, including the owner's B1 answer and the scope expansion to 25 paths;
  - the separate size:M DEV-281 InspectCode follow-up covering the solution-wide WARNING+ baseline, 36 hits after the three WorkerTests hits move into DEV-361 (P3), including `Razor.AssemblyNotResolved` [error]. At `3cd5802`, `Details.cshtml:68 AssignNullToNotNullAttribute` replaces the old `Details.cshtml:60` hit;
  - the separate Epic-1 Principle VIII language-migration issue for `FilmesFilterViewModel` and `AssistirFilme_*` (`UnitTest1.cs:487,506,549` at `3cd5802`) under Patron's C1 ruling;
  - Patron's P1 (T019) and P2 (size M) rulings, the P3 ruling, the item-5 `AssistirFilme` ruling (T001 re-baseline), and any `DynamicQuery` coverage follow-up Patron files.

  Verify each YouTrack read-back before claiming `(verified)`, and check T003 off only when real IDs are reported. Neither follow-up is a DEV-361 prerequisite, a chain item, or an owner checkbox.

## Phase 2: User Story 1 — warnings expose real defects (P1)

**Independent test:** the six values are `warning`, and InspectCode on `$files` exits 0 after T005–T017.

- [x] T004 [US1][US3] In `.editorconfig`, change exactly the **six** keys from `suggestion` to `warning`. Five are ticket-named. The sixth is `resharper_nullable_warning_suppression_is_used_highlighting` at `:68`. Rewrite the stale rationale comments at `.editorconfig:67,69,72,74` (wording per `ASSUMPTIONS.md`). Don't change any other severity. (Commit: `045cab5`)
- [x] T005 [US1] In `LamuFlix.Web/TagHelpers/Extensions.cs`, replace the default `KeyValuePair` equality at lines 43 and 56. Compare keys with `StringComparison.OrdinalIgnoreCase` and values ordinally, under the existing match guard. Don't change the pagination null-value behavior. (Commit: `339df33`)
- [x] T006 [US1] In `LamuFlix.Test/UnitTest1.cs`, resolve the five condition diagnostic hits at the four `if` sites (lines 69, 80, 109, 120 at `3cd5802`, where line 120 carries both IDs). Simplify the four dead null branches in `TestMethod1` and `TestMethod2`, quoting the test names in the change receipt. (Commit: `3a2378c`)
- [x] T007 [US1] Inspect the two unused-setter findings and the one collection finding in `FilmesFilterViewModel` at `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs:14,16,20`. Search for in-product assignments and verify the `[FromQuery]` MVC model binder writer before any fix or suppression. (Commit: `36a9b49`)

## Phase 3: User Story 2 — external writers remain supported (P2)

- [x] T008 [US2] For proven external-writer false positives in `FilmesViewModel.cs` (five-target IDs), add only a type/member-scoped suppression bracket with a one-line binder reason. (Commits: `36a9b49`, `4ec4938`)
- [x] T009 [US2] Fix any genuine five-target defect in `FilmesViewModel.cs` minimally. Don't delete a binder-visible property or add a package. (Commit: `4ec4938`)

## Phase 4: User Story 3 — null-forgiving operators flagged (P1, owner B1)

Each task owns only the files it lists. Fix first (AC3): each hit is fixed in code when an FR-008-compliant fix exists. Otherwise it gets an FR-008 bracket at type or member scope with its one-line reason. Record which path each hit took in the receipt. Every task must avoid:

- EF-mapped nullability changes;
- `DbSet` conversions;
- DTO property-set changes;
- `?? throw` guards;
- `#nullable disable`.

- [x] T014 [US3] **Group D (Data), 18 hits, 5 files (`3cd5802`).** `LamuFlix.Data/LamuFlixContext.cs` (9: lines 9–16, 18; reason: EF Core `DbSet` initialization). `MovieActors.cs`, `MovieDirectors.cs`, and `MovieGenre.cs` (2 each, `:8,9`; required join navigations), and `Temp.cs` (3: `:6,7,8`). Reason for the models: EF Core entity materialization. DEV-360 already removed the hits in `Actor`, `Collection`, `Director`, `Genre`, `Movie`, and `Player.cs`, so T014 does not edit those six files. (Commit: `a03b863`)
- [x] T015 [US3] **Group T (Test), 34 hits, 3 files, plus 3 WARNING+ fixes (`3cd5802`).**
  - `LamuFlix.Test/ApiDataModel.cs` (26; per type; Newtonsoft.Json deserialization).
  - `LamuFlix.Test/UnitTest1.cs` (6: `:149,152,153` deserialized result; `:424,426,453` reflection lookup of a known member). DEV-360 removed the `:30` fixture hit.
  - `LamuFlix.Test/WorkerTests.cs` (2: `:37,45`, deliberate `null` asserting `ArgumentNullException`). Also fix WorkerTests `:1 RedundantNullableDirective`, `:11 RedundantUsingDirective`, and `:75 MethodHasAsyncOverload`. For `:75`, replace `cts.Cancel()` with `await cts.CancelAsync()` in the existing async test, before `worker.StartAsync(cts.Token)`. The token is already cancelled when `StartAsync` runs, so the test's intent is unchanged.
  - Coordinate with T006 on `UnitTest1.cs`: same file, run them in sequence. (Commit: `3f2af47`)
- [x] T016 [US3] **Group M (Web models), 40 hits, 6 files.** `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` (32: `:10,26,33,85-87,98-111,113-122,127,128`; one bracket per declared type; coordinate with T007–T009 in sequence). `AlertModel.cs` (2: `:5,6`; in-product initializer `FilmesController.cs:97,103,108`). `ErrorViewModel.cs` (1: `:5`; `HomeController.cs:40`). `FilterViewModel.cs` (1: `:5`). `Helper/QueryableResult.cs` (1: `:10`; `FilmesServices.cs:151,350`). `Helper/QueryParams.cs` (3: `:8,25,27`). For each type, search for in-product writers and name either the binder or the cited initializer. (Commits: `aaef03d`, `4ec4938`)
- [x] T017 [US3] **Group W (Web code), 20 hits, 4 files (`3cd5802`).**
  - `LamuFlix.Web/TagHelpers/AlertsTagHelper.cs` (2: `:11,12`) and `PaginationTagHelper.cs` (2: `:15,19`). Reason: Razor attribute binding / `[ViewContext]` activation.
  - `LamuFlix.Web/Extensions/EntityExtensions.cs` (4: `:46,48,75` in `DynamicQuery`, `:125` in `DynamicSort`). Reason: reflection result on a known member.
  - `LamuFlix.Web/Services/FilmesServices.cs` (12):
    - `:304` `_configuration!`: DI-injected configuration.
    - `:74,75` `filme.Format!` and `filme.Location!` in `AssistirFilme`, behind the `Features:LocalPlay` check (`:68`) and before `ProcessStarter` (`:76`). **Comment-only member bracket.** Change no executable line of `AssistirFilme` (`:66-77`). Patron's item-5 ruling says §2.3 item 5 does not fire under that constraint.
    - `:126` `ThenInclude(x => x!.Movies)`, `:136`, `:181`, `:314` ×2, `:375`, `:388` ×2 (`Title!` in EF `IQueryable` expression trees), and `:308` (`movieModel.Title!` into `MovieEnrichmentMessage`). DEV-360 added these under its FR-004 as behavior-preserving `!` reads of now-nullable columns. Reason: nullable-column read, where the `!` preserves the pre-DEV-360 contract (DEV-360 FR-004).

  For each hit, fix it when an FR-008-compliant fix exists, and bracket it only otherwise. Never change behavior to remove a hit: no `?? string.Empty`, no added `!= null &&`, and no `?? throw`. A fix inside an `IQueryable` lambda must compile as an expression tree (no pattern matching, CS8122). The old fix candidate `:106` (`GetPlayer`) is already resolved upstream by DEV-360: it has no `!` and is not edited. Add no branch to `DynamicQuery`. Its `!` sites move into the T019 helpers. (Commit: `f88376e`)

- [x] T019 [US3] **Refactor gate, FR-010 (owner choice (A), T020 answered 2026-09-23 — in scope).** After T017, in `LamuFlix.Web/Extensions/EntityExtensions.cs` only, extract `DynamicQuery`'s per-property loop body and its collection/scalar predicate branches into private static helpers. Then `-Threshold 6` on this file exits 0, and `DynamicQuery` and every helper stays ≤6. It must be a pure move: (Commits: `d3f10f0`, `c1fd793`, `6390e92`, CA1502 CC6 exit 0).
  - same `Expression.*` construction order;
  - same locals and reflection calls, except that `equal`, `lambda`, and `lambda1` (`:40-42`) are declared at first assignment inside their helper (Sentry R2-F1);
  - no new branch, file, or type;
  - public signatures and callers unchanged (`DynamicQuery` at `FilmesServices.cs:166`; `DynamicSort` at `:171,364`).

  Keep the FR-008 suppressions on the helpers that now hold `:46,48,75`. Attach `git diff --color-moved=zebra origin/main -- LamuFlix.Web/Extensions/EntityExtensions.cs` to the receipt as move evidence, because no test covers this method (P1). If zebra shows no clean moves, attach the `--color-moved-ws=allow-indentation-change` diff and a table mapping each original line range to its helper (`plan.md` feasibility desk-check). If a pure move cannot bring every method to ≤6, that is a **hard stop**: report `blocked` with the method, its complexity, and the exit. Do not suppress, change thresholds, or add a baseline exception.

## Phase 5: Verification and review handoff

- [x] T010 [US1][US2][US3] **AC2.** Run `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files $files` at the default WARNING level. Resolve every WARNING+ finding in the 25 paths and record exit 0. Then run the read-only `-All -MinSeverity WARNING` inventory with its numeric exit and a path/line/ID list. Only out-of-scope hits go to DEV-281. Tell Patron about the three WorkerTests hits that left it. (Exit: 0 on target files, inventory exit 1 for out-of-scope hits).
- [x] T011 Check that `git diff origin/main --stat` lists only `.editorconfig` and FR-002 paths under source. Confirm: (Exit/Verification: PASS, clean scope).
  - exactly six severities changed;
  - no new package reference;
  - no EF-mapped nullability, `DbSet` type, DTO property-set, or public member type change;
  - no `#nullable disable`, `[SuppressMessage]`, or file-scoped `.editorconfig` section;
  - every suppression bracket carries a one-line reason;
  - `AssistirFilme` (`FilmesServices.cs`) differs from `origin/main` only in comment lines. `git diff origin/main -- LamuFlix.Web/Services/FilmesServices.cs` shows no added or removed non-comment line inside that member (Patron item-5 ruling);
  - `Actor`, `Collection`, `Director`, `Genre`, `Movie`, and `Player.cs` are absent from the diff;
  - bracket integrity is checked mechanically (Sentry R2-F4). In each FR-002 file, the counts of `ReSharper disable NullableWarningSuppressionIsUsed` and `ReSharper restore NullableWarningSuppressionIsUsed` are equal. Each disable is closed by a restore before the end of the same type or member. There is no file-level disable. Record the per-file counts.
- [x] T012 Run the gates in sequence, each with `-BaseRef origin/main -Files $files`: (Exits: Roslyn 0, Cyclomatic-15 0, Cyclomatic-6 0 [CC6 0], InspectCode 0, Format 0, Test 0).
  1. `./scripts/run-roslyn-analyzers.ps1`;
  2. `./scripts/run-cyclomatic-complexity.ps1` at 15;
  3. `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`, which must exit 0 (SC-006, after T019);
  4. `./scripts/run-jetbrains-inspectcode.ps1 -MinSeverity WARNING`;
  5. `dotnet format --verify-no-changes`;
  6. `dotnet test`.

  Record numeric exits and diagnostic evidence. Exit 2 is not a pass. A `format` or `test` failure counts only if it is new against the T001 baseline. Any failure that is also in the baseline and outside FR-002 goes to Patron, and it is never waived by the implementer. If the threshold-6 run exits non-zero after T019, stop and report `blocked` to Patron with the method, its complexity, and the numeric exit. Don't suppress CA1502, change thresholds, or refactor methods other than `DynamicQuery`.

## Dependencies and execution order

T020 and T018 blocked T004 onward, and both are now satisfied: T020 is answered **(A)** and T018 is re-frozen. Under (A), which is in force, T019 runs in DEV-361. Under (B), T019 is **not** done in DEV-361: the extraction moves to the prerequisite follow-up ticket, and T004–T017 and T010–T012 wait until it merges. T019 is then closed as moved, not executed (Ledger R2). T018 covers the P3 ruling, the Round 2 challenge, and the re-freeze. T001 is read-only and can run before T018. Task IDs are labels, not execution order. T004 enables the target findings. After that, T005, T014, T016's non-`FilmesViewModel` files, and T017 touch disjoint files and can run in parallel. `UnitTest1.cs` (T006 → T015) and `FilmesViewModel.cs` (T007 → T008/T009 → T016) are shared files, so their tasks run in sequence. T019 follows T017 on `EntityExtensions.cs`. T010–T012 follow all fixes, including T019.

## Implementation strategy

Fix B1 legacy sites first wherever FR-008 allows. Suppress only the remainder, one bracket per type or member, each with a reason, so any **new** `!` is a warning. Keep the Round 1 five-target fixes unchanged. Use the measured 25-path scope, re-baselined on `3cd5802`: 112 B1 hits in 18 files, and 19 files edited including `TagHelpers/Extensions.cs`. No package, public API, schema, entity nullability, project, layer, or extra file is implied by any warning.
