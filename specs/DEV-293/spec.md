# DEV-293 — Portuguese identifier rename: Filme → Movie

## Overview

**Ticket:** DEV-293 (parent DEV-281, size M, independent of DEV-292)  
**Status:** Phase A spec freeze (provisional); Gate 1 closed pending three owner checkboxes  
**Objective:** Mechanical rename of Portuguese identifiers to English; zero Portuguese in public/internal API surface on delivery; existing tests pass; solution builds  

This is a pure identifier and file-path rename with no behavior or architecture logic change. Acceptance is conditional on owner answers to the three checkboxes below; ticket acceptance line 20 in the task issue remains partly open until Gate 1.

## Frozen committed scope

The following renames and reference updates are authorized by ticket Scope & Technical Design (task lines 8–15, 17–21):

### Symbol mappings (ticket-named, exact)

| Portuguese | English | Context |
|---|---|---|
| `Filme` | `Movie` | Model, type prefix |
| `FilmesController` | `MoviesController` | Controller class |
| `FilmesServices` | `MovieService` | Service class |
| `CriarFilme` + `ProcessarFilme` | `ImportMovieFolder` | Action method, shared target |
| `AssistirFilme` | `PlayMovie` | Action method, JS call contract |
| `ExcluirFilme` | `DeleteMovie` | Action method, JS call contract |
| `MinhaLista` | `Watchlist` | Action method, view |
| `GetDetalhesFilmeAsync` | `GetMovieDetails` | Service method |

### Unambiguous token substitutions (ticket-mapping derived, in-scope files/types)

- `filmeId` → `movieId` in service interface/implementation parameters and implementations at `FilmesServices.cs:25,27,31,41,43,45,68,119,123,132,334,336,344,346,355,357`
- `GetMinhaLista` → `GetWatchlist` at `FilmesServices.cs:47,368`
- `AssistirFilme` → `PlayMovie` in call contract at `site.js:5` and Razor callers `Details.cshtml:17,18`, `Index.cshtml:108,111`
- `ExcluirFilme` → `DeleteMovie` in call contract at `site.js:118` and Razor callers `MinhaLista.cshtml:69,72`

### Physical file and folder moves (convention-derived and reference updates)

| Old path | New path |
|---|---|
| `src/LamuFlix.Web/Controllers/FilmesController.cs` | `src/LamuFlix.Web/Controllers/MoviesController.cs` |
| `src/LamuFlix.Web/Services/FilmesServices.cs` | `src/LamuFlix.Web/Services/MovieService.cs` |
| `src/LamuFlix.Web/Views/Filmes/` | `src/LamuFlix.Web/Views/Movies/` |
| `src/LamuFlix.Web/Views/Filmes/CriarFilme.cshtml` | `src/LamuFlix.Web/Views/Movies/ImportMovieFolder.cshtml` |
| `src/LamuFlix.Web/Views/Filmes/MinhaLista.cshtml` | `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml` |

### Route changes (conventional, derived from controller rename)

- `src/LamuFlix.Web/Startup.cs:72-74`: conventional route `{controller=Home}/{action=Index}/{id?}` changes `/Filmes/...` to `/Movies/...` with the controller rename; do not preserve old `/Filmes/...` endpoints through explicit route attributes.

### Razor and JavaScript reference updates (explicit edits)

**Razor files:**
- `src/LamuFlix.Web/Views/Shared/_Layout.cshtml:30-31` (link references)
- `src/LamuFlix.Web/Views/Movies/Index.cshtml:6,85,108,111` (controller/action references; former `FilmesController`, former `ExcluirFilme`/`DeleteMovie`, `AssistirFilme`/`PlayMovie`)
- `src/LamuFlix.Web/Views/Movies/ImportMovieFolder.cshtml:16` (form target, former `CriarFilme`)
- `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml:46,69,72` (controller/action references, method calls)
- `src/LamuFlix.Web/Views/Movies/Details.cshtml:17,18` (JS call contracts `AssistirFilme`/`PlayMovie`, `ExcluirFilme`/`DeleteMovie`)

**JavaScript:**
- `src/LamuFlix.Web/wwwroot/js/site.js:5` (URL string `/Filmes/` → `/Movies/`, call `AssistirFilme()` → `PlayMovie()`)
- `src/LamuFlix.Web/wwwroot/js/site.js:9` (URL string)
- `src/LamuFlix.Web/wwwroot/js/site.js:22` (URL string)
- `src/LamuFlix.Web/wwwroot/js/site.js:36` (URL string)
- `src/LamuFlix.Web/wwwroot/js/site.js:50,79,103,123,127,144,159` (URL strings)
- `src/LamuFlix.Web/wwwroot/js/site.js:118` (call `ExcluirFilme()` → `DeleteMovie()`)

### Test updates (existing references only; method names unchanged)

- `tests/LamuFlix.Test/UnitTest1.cs:16,29,67,78,109` (`FilmesService` → `MovieService`, `AssistirFilme` → `PlayMovie`)
- `tests/LamuFlix.Test/EnrichmentTests.cs:313,317` (`FilmesService` → `MovieService`, `CriarFilme` → `ImportMovieFolder`)

### Exclusions and required renames

- Model folder `Models/Filmes/` and its contents (`FilmesViewModel.cs`, `FilmesFilterViewModel`, `FilmesListViewModel`, `CriarFilmeViewModel`) — Q3 owner checkbox; required by Constitution VIII - a NO answer re-scopes DEV-293
- Service interface `IFilmesService` and derived composites (`GetFilmesListAsync`, `GetFilmesJson`, `CreateFilmesListQuery`) — Q3 owner checkbox; required by Constitution VIII - a NO answer re-scopes DEV-293
- Namespace `LamuFlix.Web.Models.Filmes` — Q3 owner checkbox; required by Constitution VIII - a NO answer re-scopes DEV-293
- Explicit route attribute `[Route("/MinhaLista/")]` at `src/LamuFlix.Web/Controllers/FilmesController.cs:159` — Q5-A owner checkbox; optional - a route literal is not an identifier
- JSON response field name `filmes` at `src/LamuFlix.Web/Controllers/FilmesController.cs:113,115` and `site.js:23,37,80` — Q5-B owner checkbox; required by Constitution VIII - the anonymous-object property is an identifier
- Razor UI copy (Portuguese labels, empty states)
- Local variable names and exception text in `FilmesServices.cs:75`
- Test method names
- `wwwroot/js/site.min.js` (generated; logged as follow-up)
- No behavior or architecture logic change; no new NuGet or npm dependency; `Features:LocalPlay` gate and playback behavior unchanged

## Owner checkboxes (exactly three, unticked: Q3 required, Q5-B required, Q5-A optional)

### Q3 — Composite identifiers and model paths

- [ ] **Authorize these exact targets? YES or NO.** Folder `Models/Filmes/` -> `Models/Movies/`, file `FilmesViewModel.cs` -> `MoviesViewModel.cs`, namespace `LamuFlix.Web.Models.Filmes` -> `LamuFlix.Web.Models.Movies`, and the composite identifiers listed below.
  - `FilmesFilterViewModel` → `MoviesFilterViewModel`
  - `FilmesListViewModel` → `MoviesListViewModel`
  - `CriarFilmeViewModel` → `ImportMovieFolderViewModel`
  - `IFilmesService` → `IMovieService` (interface; see Q3)
  - `GetFilmesListAsync` → `GetMoviesListAsync`
  - `GetFilmesJson` → `GetMoviesJson`
  - `CreateFilmesListQuery` → `CreateMoviesListQuery`

**Rationale:** Constitution VIII forbids these Portuguese identifiers and Governance blocks a non-compliant merge; because the ticket targets are ambiguous, the owner confirms the exact names. YES makes them frozen Phase B scope once Patron amends DEV-293; NO re-scopes or closes DEV-293.

### Q5-A — Explicit route attribute

- [ ] **Authorize the URL change to `/Watchlist/`? YES or NO (optional).** The hand-written route literal `[Route("/MinhaLista/")]` at `src/LamuFlix.Web/Controllers/FilmesController.cs:159` is not an identifier, so Constitution VIII does not require it.

**Rationale:** A route literal is not an identifier, so Constitution VIII does not require it; it is a section 2.3.4 public API shape choice and stays optional, recommended because the action and view are renamed to `Watchlist`.

### Q5-B — JSON response field

- [ ] **Authorize `filmes` -> `movies`? YES or NO (required).** The property in `Json(new { filmes = ... })` at `src/LamuFlix.Web/Controllers/FilmesController.cs:113,115` is a C# identifier, so Constitution VIII forbids it; the contract change needs owner confirmation, and its consumers at `src/LamuFlix.Web/wwwroot/js/site.js:23,37,80` are updated in the same delivery.

**Rationale:** The `filmes` property is a C# identifier, so Constitution VIII forbids it; the rename changes the response contract, so the owner confirms the target under section 2.3.4. YES makes it frozen Phase B scope once Patron amends DEV-293; NO re-scopes or closes DEV-293.

Constitution VIII forbids Portuguese identifiers and requires them renamed on contact (`Filme`, `CriarFilme`, `AssistirFilme`, `ExcluirFilme`, `MinhaLista`, `ProcessarFilme` and similar), and Governance blocks a non-compliant merge unless an amendment is simultaneously submitted and approved. Q3 and Q5-B are therefore compliance requirements, not optional follow-ups: the owner answers YES to the exact targets, or DEV-293 is re-scoped or closed and does not merge with Portuguese identifiers present. Q5-A is a route literal, not an identifier, so it remains the owner optional choice. On a YES answer Patron amends DEV-293 so the named targets are frozen Phase B scope; on a NO answer the ticket returns to the owner for re-scoping or closure.

## Test strategy

- **Existing test reference updates only:** Rename old symbols in `UnitTest1.cs` and `EnrichmentTests.cs`; method names remain unchanged.
- **No new behavior tests:** Adding test methods that only mirror a rename is out of scope.
- **Manual smoke verification:** Document manual checks for Index, Details, PlayMovie, ImportMovieFolder, DeleteMovie, GetFilmesJson, QuickSearch, AddToWatchList, RemoveFromWatchList, and Watchlist views/actions. If the configured database prevents local startup, fall back to static checks of old `/Filmes/` URL strings and Razor tag-helper targets; log unverified route/link coverage as a follow-up.
- **Property-tests opt-out:** Per-ticket opt-out to be recorded in task-DEV-293 before Phase B; no domain invariant added by this rename, so `run-property-tests.ps1` exit 2 is expected and accepted as SKIPPED/OPT-OUT.
- **LocalPlay flag:** Existing LocalPlay tests remain green; no behavior changes to flag-gated code.

## Gates and acceptance

### Expected pass criteria (Phase B)

Under pwsh 7, with `-BaseRef main` on changed diff:

| Gate | Command | Threshold | Exit | Notes |
|---|---|---|---|---|
| Roslyn analyzers | `run-roslyn-analyzers.ps1 -BaseRef main` | CA/IDE warning+ | 0 | Catches semantic errors |
| Cyclomatic complexity | `run-cyclomatic-complexity.ps1 -BaseRef main` | 15 | 0 | Refactor gate later: threshold 6 |
| InspectCode | `run-jetbrains-inspectcode.ps1 -BaseRef main` | Configured | 0 | Rider/ReSharper inspections |
| Vulnerable packages | `run-vulnerable-packages.ps1` | Configured | 0 | Supply chain check |
| Solution build | `dotnet build` | — | Success | Compiler verification |
| Unit/integration tests | `dotnet test` | — | Pass | Existing suite; no new tests |
| Mutation testing | `dotnet stryker` (pre-PR) | 80 | ≥ 80 | Fix surviving mutants by tests; never lower threshold |
| Property tests | `run-property-tests.ps1` | Per-ticket opt-out | 2 (OPT-OUT) | Expected; recorded opt-out in task-DEV-293 makes exit 2 acceptable as SKIPPED/OPT-OUT |

**No `/web` changes:** Vitest and web build gates are not in scope.

### Review round cap

- **Two formal code review + ship review rounds maximum,** at most two fix commits per round (per `remediate/SKILL.md:12` and precedent in DEV-280, DEV-290, DEV-360, DEV-361).
- **Verified in-scope Critical/High/Medium findings block** both reviews and must be fixed before merge.
- **Low and out-of-scope findings are Follow-ups,** retaining source and severity.
- **Unresolved Critical/High Follow-ups still prevent READY** under ship-review line 148 and code-review line 248.
- **Owner checkboxes (Q3, Q5-A, Q5-B) consume no review round** and travel on the spec PR outside committed scope.
- **At round cap:** Report unresolved above-bar findings to Patron as `blocked: —` or `NEEDS FIXES`; no third round or severity relabel.

## Follow-ups (post-Gate 1, not in committed scope)

- **Generated JavaScript:** `wwwroot/js/site.min.js` is generated and not directly edited; logged as follow-up to regenerate/verify after Phase B.
- **Route/link automated coverage:** In-process route test would require new `Microsoft.AspNetCore.Mvc.Testing` NuGet dependency, outside scope. Manual smoke documented above; full automated route/link regression test is a follow-up.
- **Q3, Q5-A, Q5-B:** Owner answers on the three checkboxes will unlock follow-up work or confirm current behavior.
- **Test method names:** Remain unchanged; renaming them is a separate refactor issue.

## Relationship to ticket acceptance

Ticket acceptance criteria (task issue lines 17–21):
- ✓ Mechanical identifier rename with no behavior/architecture change (in scope)
- ✓ All references and tests updated (in scope)
- ⊗ Zero Portuguese identifiers in public/internal API surface (conditional; Q3, Q5-A, Q5-B await owner answers)
- ✓ Solution builds (Phase B gate)
- ✓ Existing tests pass (Phase B gate)

Gate 1 remains closed until owner provides answers to the three checkboxes and this spec PR merges.
