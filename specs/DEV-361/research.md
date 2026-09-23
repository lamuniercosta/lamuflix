# DEV-361 research and measured scope

The read-only Phase A command ran `run-jetbrains-inspectcode.ps1 -Files <eight Web/Models files, TagHelpers/Extensions.cs, UnitTest1.cs> -MinSeverity SUGGESTION`. It exited **1** with 162 suggestion-or-higher issues in ten candidate files. An earlier external `pwsh -File` style call exited 1 before inspection because the string-array paths were passed as positional arguments; the direct PowerShell array-bound call produced this report. Neither is a passing gate.

| Target inspection ID | Hits | File |
|---|---:|---|
| `UnusedAutoPropertyAccessor.Global` | 2 | `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` |
| `CollectionNeverUpdated.Global` | 1 | `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` |
| `UsageOfDefaultStructEquality` | 2 | `LamuFlix.Web/TagHelpers/Extensions.cs` |
| `ConditionIsAlwaysTrueOrFalse` | 4 | `LamuFlix.Test/UnitTest1.cs` |
| `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` | 1 | `LamuFlix.Test/UnitTest1.cs` |

The ten hits occupy nine source lines. Four `ConditionIsAlwaysTrueOrFalse` hits are at `UnitTest1.cs:61,72,101,112`; the `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` hit also occurs at line 112. Thus five diagnostic hits map to four conditional sites. The other five hits are `FilmesViewModel.cs:14,16,20` and `Extensions.cs:43,56` (probe log: `%TEMP%/dev-361-inspectcode-probe-bound.log:882-887,978-979,1013-1014`). These lines preserve the relevant probe evidence.

The B1 `NullableWarningSuppressionIsUsed` ID also appears at SUGGESTION, but ticket item 2 explicitly leaves that key at `suggestion` pending owner answer. No target ID hit outside the three frozen paths **among the ten candidates**. This candidate-only absence finding was superseded by the later solution-wide probe below, which found `Details.cshtml:60`. `run-jetbrains-inspectcode.ps1 -Help` exited 0 and confirms `-Files` and `-MinSeverity`.

### Unmodified-source WARNING+ baseline (2026-09-23)

From `feature/361-dev-361-spec`, before any source or `.editorconfig` change, `./scripts/run-jetbrains-inspectcode.ps1 -Files $files -MinSeverity WARNING` exited **0** with `InspectCode: passed (no WARNING+ issues).` Here `$files` is the exact three-path list in FR-002. This does not prove that the five currently suggestion-level IDs will pass after restoration. `./scripts/run-cyclomatic-complexity.ps1 -Files $files` exited **0**, with no method above configured threshold 15. A concurrent Roslyn run exited **1** because CS2012 locked `LamuFlix.Data/obj/Debug/net10.0/LamuFlix.Data.dll` during the complexity build; sequential `./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main -Files $files` exited **0** with no CA/IDE warnings on target files. Run subsequent gates sequentially. The three source files remain unmodified.

A separate unchanged-`origin/main` baseline invoked `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files -Threshold 6` with the same three paths. **Native exit 0**; output: `Cyclomatic complexity: passed (no methods exceed threshold of 6).` Thus the analyzer listed **no** method above 6. `UnitTest1.DynamicQuery` at `:387-446` has visible branches but was not emitted; this measured result differs from the expected hit and must not be rewritten as a failing gate. It is pre-existing and assigned to DEV-280. The solution-wide `-All -Threshold 6` diagnostic on unchanged `origin/main` (T013 F2 rerun) exited **1** listing only `LamuFlix.Web/Extensions/EntityExtensions.cs:13 DynamicQuery` (12), `ViewModelExtensions.cs:8 HasQuery` (8) and `ViewModelExtensions.cs:36 HasPropertyValue` (7), none in the frozen paths; CA1502 does not emit `UnitTest1.DynamicQuery`. No baseline exception is granted: if the three-file threshold-6 run exits non-zero in Phase B, stop and report `blocked` to Patron with the emitted method, complexity and numeric exit. Do not refactor unchanged methods, suppress CA1502, or alter thresholds in DEV-361.

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

Patron Q2–Q4 in [CONCLUSIONS.md](CONCLUSIONS.md) define the behavior-preserving equality change, dead test branch simplification, and external-writer suppression rule. Existing `.editorconfig:68-76` has all six keys at `suggestion`.
