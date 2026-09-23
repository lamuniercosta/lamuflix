# DEV-361 research and measured scope

> **Current baseline: `origin/main` `3cd5802` (DEV-360 merged), measured 2026-09-23.** See "Re-baseline after DEV-360" at the end. It supersedes every `5a38215` count and line number below wherever they differ. Sections measured at `5a38215` are kept for the record.

The read-only Phase A command ran `run-jetbrains-inspectcode.ps1 -Files <eight Web/Models files, TagHelpers/Extensions.cs, UnitTest1.cs> -MinSeverity SUGGESTION`. It exited **1** with 162 suggestion-or-higher issues in ten candidate files. An earlier external `pwsh -File` style call exited 1 before inspection because the string-array paths were passed as positional arguments; the direct PowerShell array-bound call produced this report. Neither is a passing gate.

| Target inspection ID | Hits | File |
|---|---:|---|
| `UnusedAutoPropertyAccessor.Global` | 2 | `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` |
| `CollectionNeverUpdated.Global` | 1 | `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` |
| `UsageOfDefaultStructEquality` | 2 | `LamuFlix.Web/TagHelpers/Extensions.cs` |
| `ConditionIsAlwaysTrueOrFalse` | 4 | `LamuFlix.Test/UnitTest1.cs` |
| `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` | 1 | `LamuFlix.Test/UnitTest1.cs` |

The ten hits occupy nine source lines. Four `ConditionIsAlwaysTrueOrFalse` hits are at `UnitTest1.cs:61,72,101,112`; the `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` hit also occurs at line 112. Thus five diagnostic hits map to four conditional sites. The other five hits are `FilmesViewModel.cs:14,16,20` and `Extensions.cs:43,56` (probe log: `%TEMP%/dev-361-inspectcode-probe-bound.log:882-887,978-979,1013-1014`). These lines preserve the relevant probe evidence.

The B1 `NullableWarningSuppressionIsUsed` ID also appears at SUGGESTION. Ticket item 2 held it at `suggestion` pending the owner's answer. The owner has now answered **[x]** (reverse B1). Its measured scope is in "B1 reversal scope" below. No target ID hit outside the three frozen paths **among the ten candidates**. This candidate-only absence finding was superseded by the later solution-wide probe below, which found `Details.cshtml:60`. `run-jetbrains-inspectcode.ps1 -Help` exited 0 and confirms `-Files` and `-MinSeverity`.

### Unmodified-source WARNING+ baseline (2026-09-23)

From `feature/361-dev-361-spec`, before any source or `.editorconfig` change, `./scripts/run-jetbrains-inspectcode.ps1 -Files $files -MinSeverity WARNING` exited **0** with `InspectCode: passed (no WARNING+ issues).` Here `$files` is the exact three-path list in FR-002. This does not prove that the five currently suggestion-level IDs will pass after restoration. `./scripts/run-cyclomatic-complexity.ps1 -Files $files` exited **0**, with no method above configured threshold 15. A concurrent Roslyn run exited **1** because CS2012 locked `LamuFlix.Data/obj/Debug/net10.0/LamuFlix.Data.dll` during the complexity build; sequential `./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main -Files $files` exited **0** with no CA/IDE warnings on target files. Run subsequent gates sequentially. The three source files remain unmodified.

A separate unchanged-`origin/main` baseline invoked `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files -Threshold 6` with the same three paths. **Native exit 0**; output: `Cyclomatic complexity: passed (no methods exceed threshold of 6).` Thus the analyzer listed **no** method above 6. `UnitTest1.DynamicQuery` at `:387-446` has visible branches but was not emitted; this measured result differs from the expected hit and must not be rewritten as a failing gate. It is pre-existing and assigned to DEV-280. The solution-wide `-All -Threshold 6` diagnostic on unchanged `origin/main` (T013 F2 rerun) exited **1** listing only `LamuFlix.Web/Extensions/EntityExtensions.cs:13 DynamicQuery` (12), `ViewModelExtensions.cs:8 HasQuery` (8) and `ViewModelExtensions.cs:36 HasPropertyValue` (7), none in the frozen paths; CA1502 does not emit `UnitTest1.DynamicQuery`. No baseline exception is granted: if the three-file threshold-6 run exits non-zero in Phase B, stop and report `blocked` to Patron with the emitted method, complexity and numeric exit. Do not refactor unchanged methods, suppress CA1502, or alter thresholds in DEV-361. **[Superseded 2026-09-23 by Patron's T019 ruling for the expanded 25-path modified-file set: `DynamicQuery` helper extraction is constitution-mandated (`constitution.md:344-345`); a failure the extraction cannot remedy remains a hard stop. See `plan.md` P1.]**

The additional read-only `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity SUGGESTION` probe exited **1** with 451 SUGGESTION+ issues. It found **11** hits of the five target IDs solution-wide: the ten frozen-path hits above plus `LamuFlix.Web/Views/Filmes/Details.cshtml:60 [warning] ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract`. This already-WARNING Razor-view hit is outside the three-file code-edit envelope. The same probe exposed **39** existing WARNING+ diagnostics solution-wide, grouped as 18 `Html.IdNotResolved`, 8 `RedundantUsingDirective`, 4 `RedundantNullableDirective`, 3 `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract`, 2 `RedundantCast`, and one each of `Razor.AssemblyNotResolved` (error), `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` (the view hit), `EmptyConstructor`, and `MethodHasAsyncOverload`. None is in the frozen three C# paths. Patron's ruling is one separate size:M DEV-281 InspectCode follow-up for all 39 hits, including `Details.cshtml:60`; Rigger records it under T003 without adding it to the chain. The probe log was `%TEMP%/dev-361-all-suggestion-probe.log`; this durable path/line/ID inventory replaces dependence on that temporary file:

| Path | WARNING+ inspection | Lines |
|---|---|---|
| `LamuFlix.Data/Constants/GeneralConstants.cs` | `EmptyConstructor`; `RedundantUsingDirective` | 9; 1, 2, 3 |
| `LamuFlix.Data/Models/MovieEnrichmentMessage.cs` | `RedundantNullableDirective` | 1 |
| `LamuFlix.Test/WorkerTests.cs` | `MethodHasAsyncOverload`; `RedundantNullableDirective`; `RedundantUsingDirective` | 75; 1; 11 |
| `LamuFlix.Web/Controllers/HomeController.cs` | `RedundantUsingDirective` | 1, 2, 4, 5 |
| `LamuFlix.Web/Models/Helper/PagedListing.cs` | `RedundantCast` | 46 twice |
| `LamuFlix.Web/Services/IEnrichmentQueuePublisher.cs` | `RedundantNullableDirective` | 1 |
| `LamuFlix.Web/Services/RabbitMqEnrichmentQueuePublisher.cs` | `RedundantNullableDirective` | 1 |
| `LamuFlix.Web/Views/_ViewImports.cshtml` | `Razor.AssemblyNotResolved` **[error]** | 7 |
| `LamuFlix.Web/Views/Filmes/Details.cshtml` | `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` | 60 |
| `LamuFlix.Web/Views/Filmes/Index.cshtml` | `Html.IdNotResolved` | 58, 61, 64, 67, 70, 73, 108, 111, 114 |
| `LamuFlix.Web/Views/Filmes/MinhaLista.cshtml` | `Html.IdNotResolved` | 19, 22, 25, 28, 31, 34, 69, 72, 75 |
| `LamuFlix.Work/Services/EnrichmentJobProcessor.cs` | `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` | 139, 157, 175 |

At Phase B pickup, repeat the solution-wide WARNING+ check **after restoration** and record new diagnostic IDs, paths, lines, and numeric exit. Fix any in-scope WARNING+ hits in the three frozen files for AC2 and notify Patron; the current pre-restoration three-file baseline has none. Only out-of-scope hits go to the separate InspectCode follow-up. The three-file AC2 gate still requires exit 0 after fixes.

Patron Q2–Q4 in [CONCLUSIONS.md](CONCLUSIONS.md) define the behavior-preserving equality change, dead test branch simplification, and external-writer suppression rule. Existing `.editorconfig:68-76` has all six keys at `suggestion`. All six go to `warning` under the owner's B1 answer.

The five-target sections above measured the original three-file scope. The B1 section below supersedes them wherever the two conflict on the file set, the baselines, or the "do not refactor unchanged methods" rule. `DynamicQuery` is now refactored under T019, as `constitution.md:344-345` requires.

## B1 reversal scope: sixth inspection (Phase B, 2026-09-23)

**Authority.** The owner ticked B1 **[x]** on merged PR #3 ("restore `nullable_warning_suppression_is_used` to warning (reverses B1)?"). The user then confirmed through the Conductor on 2026-09-23 that a checked box is the source of truth. [x] means: reverse B1, set `.editorconfig:68` to `warning`, and widen DEV-361 past the frozen three files to the paths this inspection flags. This is an owner scope change. It is not a Patron ruling.

**Measurement.** On unchanged `origin/main` = `5a38215`, in worktree `F:\Dev\LamuFlix.worktrees\DEV-361`, `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity SUGGESTION` exited **1**. It reported 451 SUGGESTION+ issues and 39 WARNING+. The five-target inventory above is unchanged: 11 hits. The log is `%TEMP%/dev-361-b1-all-suggestion-probe.log` and the SARIF is `artifacts/inspectcode/inspect-report.sarif.json`. It found **114** `NullableWarningSuppressionIsUsed` hits, all `[note]`, in **24** `.cs` files. No hits were in `.cshtml`. `git grep -c 'null!'` finds 102 lines in 22 files. That undercounts because the inspection also flags postfix `expr!`, which adds `EntityExtensions.cs` and `FilmesServices.cs` and 5 more lines in `UnitTest1.cs`.

| Group | Path | Hits | Lines | Writer / reason category |
|---|---|---:|---|---|
| D | `LamuFlix.Data/LamuFlixContext.cs` | 9 | 9–16, 18 | EF Core initializes `DbSet<>` properties |
| D | `LamuFlix.Data/Models/Actor.cs` | 1 | 8 | EF Core materialization |
| D | `LamuFlix.Data/Models/Collection.cs` | 1 | 8 | EF Core materialization |
| D | `LamuFlix.Data/Models/Director.cs` | 1 | 8 | EF Core materialization |
| D | `LamuFlix.Data/Models/Genre.cs` | 1 | 8 | EF Core materialization |
| D | `LamuFlix.Data/Models/Movie.cs` | 4 | 8, 16, 17, 23 | EF Core materialization |
| D | `LamuFlix.Data/Models/MovieActors.cs` | 2 | 8, 9 | EF Core materialization (join navigations) |
| D | `LamuFlix.Data/Models/MovieDirectors.cs` | 2 | 8, 9 | EF Core materialization (join navigations) |
| D | `LamuFlix.Data/Models/MovieGenre.cs` | 2 | 8, 9 | EF Core materialization (join navigations) |
| D | `LamuFlix.Data/Models/Player.cs` | 3 | 6, 7, 8 | EF Core materialization |
| D | `LamuFlix.Data/Models/Temp.cs` | 3 | 6, 7, 8 | EF Core materialization |
| T | `LamuFlix.Test/ApiDataModel.cs` | 26 | 7–20, 22–31, 36, 37 | Newtonsoft.Json deserialization |
| T | `LamuFlix.Test/UnitTest1.cs` | 7 | 30, 141, 144, 145, 416, 418, 445 | test fixture field; deserialized result; reflection lookups |
| T | `LamuFlix.Test/WorkerTests.cs` | 2 | 37, 45 | deliberate `null` argument asserting `ArgumentNullException` |
| M | `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` | 32 | 10, 26, 33, 85–87, 98–111, 113–122, 127, 128 | MVC model binder / in-product initializer, verified per type |
| M | `LamuFlix.Web/Models/AlertModel.cs` | 2 | 5, 6 | in-product initializer (`FilmesController.cs:97,103,108`) |
| M | `LamuFlix.Web/Models/ErrorViewModel.cs` | 1 | 5 | in-product initializer (`HomeController.cs:40`) |
| M | `LamuFlix.Web/Models/FilterViewModel.cs` | 1 | 5 | MVC model binder, verified in Phase B |
| M | `LamuFlix.Web/Models/Helper/QueryableResult.cs` | 1 | 10 | in-product initializer (`FilmesServices.cs:151,350`) |
| M | `LamuFlix.Web/Models/Helper/QueryParams.cs` | 3 | 8, 25, 27 | MVC model binder, verified in Phase B |
| W | `LamuFlix.Web/TagHelpers/AlertsTagHelper.cs` | 2 | 11, 12 | Razor tag-helper attribute binding / `[ViewContext]` activation |
| W | `LamuFlix.Web/TagHelpers/PaginationTagHelper.cs` | 2 | 15, 19 | Razor tag-helper attribute binding / `[ViewContext]` activation |
| W | `LamuFlix.Web/Extensions/EntityExtensions.cs` | 4 | 46, 48, 75, 125 | postfix `!` on reflection results (`DynamicQuery`, `DynamicSort`) |
| W | `LamuFlix.Web/Services/FilmesServices.cs` | 2 | 106, 304 | postfix `!` after a null check / on injected configuration |

Totals: D 29 hits in 11 files, T 35 in 3, M 40 in 6, W 10 in 4. That is **114 in 24**. Add `LamuFlix.Web/TagHelpers/Extensions.cs`, which only has five-target hits, and the AC2 path set is **25** `.cs` files.

### Expanded-scope unchanged-source baselines (T001 rerun, 2026-09-23)

`$files` is the 25-path list in `spec.md` FR-002. Each run was sequential with `-BaseRef origin/main` against unchanged source:

| Gate | Native exit | Result |
|---|---:|---|
| `run-jetbrains-inspectcode.ps1 -Files $files -MinSeverity WARNING` | **1** | 3 pre-existing WARNING+ hits, all in `LamuFlix.Test/WorkerTests.cs`: `:75 MethodHasAsyncOverload`, `:1 RedundantNullableDirective`, `:11 RedundantUsingDirective`. These are 3 of the 39 DEV-281 inventory hits above. |
| `run-roslyn-analyzers.ps1 -Files $files` | 0 | no CA/IDE warnings on target files |
| `run-cyclomatic-complexity.ps1 -Files $files` (threshold 15) | 0 | none |
| `run-cyclomatic-complexity.ps1 -Files $files -Threshold 6` | **1** | `LamuFlix.Web/Extensions/EntityExtensions.cs:13 DynamicQuery` complexity **12**. B1 hits `:46,48,75` sit inside this method. |

Implications:

- Because `WorkerTests.cs` joins the AC2 `-Files` set, its three pre-existing WARNING+ hits must be fixed in DEV-361 for AC2 to exit 0. That leaves 36 of the 39 hits for DEV-281.
- The CA1502 script filters results by file. Any edit to `EntityExtensions.cs`, even a suppression comment that adds nothing to complexity, puts `DynamicQuery` (12) into the threshold-6 gate. The Round 1 rule was "no baseline exception, stop and report `blocked`". Under the widened scope, that rule would fail the gate every time. `constitution.md:344-345` requires helper extraction, so the plan adds T019 (`plan.md` P1).

## Re-baseline after DEV-360 (`origin/main` `3cd5802`, 2026-09-23)

**Why.** DEV-360 merged between Phase A (`5a38215`) and Phase B pickup. T001's path check found it touched 9 of the 25 FR-002 paths: `LamuFlixContext.cs`, `Models/{Actor,Collection,Director,Genre,Movie,Player}.cs`, `UnitTest1.cs`, and `FilmesServices.cs`. It did not touch `.editorconfig`, `specs/DEV-361/`, or `EntityExtensions.cs`. Keel adjudicated the drift as **updated baselines, not a re-plan**: the 25 paths, the six keys, T019, and the task set are unchanged. The DEV-360 changes that matter here:

- DEV-360 FR-004 annotated snapshot-nullable entity columns as nullable, which removed the `= null!` hits in `Actor/Collection/Director/Genre.cs:8`, `Movie.cs:8,16,17,23`, and `Player.cs:6,7,8` (-11).
- DEV-360 FR-001/FR-002 replaced the `UnitTest1.cs` `[TestInitialize]` with a lazy `_dataContext` property. That removed the `:30` hit (-1) and moved every later line down by 8.
- DEV-360 rewrote `FilmesServices.cs:106` (`GetPlayer`) without `!` (-1; the Patron F1 fix candidate is resolved upstream). It added 11 behavior-preserving `!` reads of the now-nullable columns (+11).

**Measurement.** Worktree `F:\Dev\LamuFlix.worktrees\DEV-361` at `3cd5802`, unchanged source, sequential, each `-BaseRef origin/main -Files $files` (25-path list):

| Run | Native exit | Result |
|---|---:|---|
| InspectCode `-MinSeverity WARNING` | **1** | the same 3 WorkerTests hits: `:1 RedundantNullableDirective`, `:11 RedundantUsingDirective`, `:75 MethodHasAsyncOverload` |
| Roslyn | 0 | none |
| CA1502 at 15 | 0 | none |
| CA1502 `-Threshold 6` | **1** | `EntityExtensions.cs:13 DynamicQuery` 12, only |
| `-All -MinSeverity SUGGESTION` (read-only) | **1** | 451 issues, 39 WARNING+. Composition changed; see below. SARIF: `artifacts/inspectcode/inspect-report.sarif.json` |
| `dotnet format --verify-no-changes` | 0 | clean baseline |
| `dotnet test` | 0 | Passed 18, Skipped 4, Failed 0 |

`EntityExtensions.cs` is byte-identical to `5a38215`, so the T019 desk-check (`plan.md` P1) and the `DynamicQuery` structure still hold.

**Five-target IDs: 10 solution-wide (was 11).** `Details.cshtml:60` no longer fires, because `Movie`'s columns are now nullable. The ten in-scope hits are unchanged in kind and file. Only `UnitTest1.cs` lines moved:

| Target inspection ID | Path:lines |
|---|---|
| `UnusedAutoPropertyAccessor.Global` | `FilmesViewModel.cs:14,16` |
| `CollectionNeverUpdated.Global` | `FilmesViewModel.cs:20` |
| `UsageOfDefaultStructEquality` | `TagHelpers/Extensions.cs:43,56` |
| `ConditionIsAlwaysTrueOrFalse` | `UnitTest1.cs:69,80,109,120` |
| `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` | `UnitTest1.cs:120` |

**WARNING+ inventory: still 39, one entry swapped.** `Details.cshtml:60 ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` is gone. `Details.cshtml:68 AssignNullToNotNullAttribute` [warning] is new (a nullable column now flows into a not-null parameter in the view). Every other row of the `5a38215` inventory table is unchanged. After the three WorkerTests hits move into DEV-361 (FR-009), 36 remain for DEV-281/DEV-366, now including `Details.cshtml:68` instead of `:60`. Rigger records the swap under T003.

**B1 `NullableWarningSuppressionIsUsed`: 112 hits in 18 `.cs` files (was 114 in 24).** All `[note]`, none in `.cshtml`. This table supersedes the `5a38215` B1 table:

| Group | Path | Hits | Lines | Writer / reason category |
|---|---|---:|---|---|
| D | `LamuFlix.Data/LamuFlixContext.cs` | 9 | 9–16, 18 | EF Core initializes `DbSet<>` properties |
| D | `LamuFlix.Data/Models/MovieActors.cs` | 2 | 8, 9 | EF Core materialization (required join navigations) |
| D | `LamuFlix.Data/Models/MovieDirectors.cs` | 2 | 8, 9 | EF Core materialization (required join navigations) |
| D | `LamuFlix.Data/Models/MovieGenre.cs` | 2 | 8, 9 | EF Core materialization (required join navigations) |
| D | `LamuFlix.Data/Models/Temp.cs` | 3 | 6, 7, 8 | EF Core materialization (unmapped; DEV-360 left it unedited) |
| T | `LamuFlix.Test/ApiDataModel.cs` | 26 | 7–20, 22–31, 36, 37 | Newtonsoft.Json deserialization |
| T | `LamuFlix.Test/UnitTest1.cs` | 6 | 149, 152, 153, 424, 426, 453 | deserialized result; reflection lookups |
| T | `LamuFlix.Test/WorkerTests.cs` | 2 | 37, 45 | deliberate `null` argument asserting `ArgumentNullException` |
| M | `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` | 32 | 10, 26, 33, 85–87, 98–111, 113–122, 127, 128 | MVC model binder / in-product initializer, verified per type |
| M | `LamuFlix.Web/Models/AlertModel.cs` | 2 | 5, 6 | in-product initializer (`FilmesController.cs:97,103,108`) |
| M | `LamuFlix.Web/Models/ErrorViewModel.cs` | 1 | 5 | in-product initializer (`HomeController.cs:40`) |
| M | `LamuFlix.Web/Models/FilterViewModel.cs` | 1 | 5 | MVC model binder, verified in Phase B |
| M | `LamuFlix.Web/Models/Helper/QueryableResult.cs` | 1 | 10 | in-product initializer (`FilmesServices.cs:151,350`) |
| M | `LamuFlix.Web/Models/Helper/QueryParams.cs` | 3 | 8, 25, 27 | MVC model binder, verified in Phase B |
| W | `LamuFlix.Web/TagHelpers/AlertsTagHelper.cs` | 2 | 11, 12 | Razor tag-helper attribute binding / `[ViewContext]` activation |
| W | `LamuFlix.Web/TagHelpers/PaginationTagHelper.cs` | 2 | 15, 19 | Razor tag-helper attribute binding / `[ViewContext]` activation |
| W | `LamuFlix.Web/Extensions/EntityExtensions.cs` | 4 | 46, 48, 75, 125 | postfix `!` on reflection results (`DynamicQuery`, `DynamicSort`) |
| W | `LamuFlix.Web/Services/FilmesServices.cs` | 12 | 74, 75, 126, 136, 181, 304, 308, 314 ×2, 375, 388 ×2 | `:304` DI-injected configuration; the other 11 are DEV-360's behavior-preserving reads of nullable columns (see below) |

Totals: D 18 hits in 5 files, T 34 in 3, M 40 in 6, W 20 in 4. That is **112 in 18**. `Actor.cs`, `Collection.cs`, `Director.cs`, `Genre.cs`, `Movie.cs`, and `Player.cs` now have **zero** hits. They stay in the FR-002 `-Files` set, so the AC2 gate still covers them, but DEV-361 edits nothing in them. `TagHelpers/Extensions.cs` (five-target only) makes the 19th edited file. The AC2 path set stays **25**.

**The 11 DEV-360 `FilmesServices.cs` reads.** DEV-360 added them under its FR-004 ("minimal `!` … preserve returned values and public shape"):

- `:74,75` in `AssistirFilme`: `filme.Format!` and `filme.Location!` feed `ResolvePlayerPath` / `BuildProcessStartInfo`. They are behind the `Features:LocalPlay` check (`:68`) and before `ProcessStarter` (`:76`). **Patron ruled (2026-09-23) that §2.3 item 5 does not fire for a comment-only member bracket here.** No executable line in `AssistirFilme` (`:66-77`) may change, so no fix-first form applies.
- `:126` `ThenInclude(x => x!.Movies)`, and `:136`, `:181`, `:314` ×2, `:375`, `:388` ×2: `m.Title!` / `x.Title!` inside EF `IQueryable` expression trees (projection, `Select`, `Where`).
- `:308` `movieModel.Title!` into `MovieEnrichmentMessage.Title`.

Default expectation: bracket. Removing a `!` without a replacement raises CS8602/CS8604 under `TreatWarningsAsErrors`. A `?? string.Empty` changes the reachable value, and a `!= null &&` adds CA1502 complexity and changes the predicate, so neither meets FR-008. T017 still applies fix-first per hit and records the path each one takes. The honest bracket reason is not "the value is non-null". It is: "nullable column read; `!` preserves the pre-DEV-360 contract (DEV-360 FR-004)". FR-008 lists it as its own reason category.
