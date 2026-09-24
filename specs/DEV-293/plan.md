# DEV-293 Phase A → Phase B transition plan

## Phase A (spec worktree, this branch)

**Status:** In progress  
**Artifacts:** `spec.md`, `plan.md`, `tasks.md` (this file)  
**Gate:** Phase A freeze after `/speckit-analyze` and adjudication  
**Output:** Spec PR with exactly three unticked owner checkboxes (Q3, Q5-A, Q5-B)  
**Next:** User answers boxes on spec PR; Gate 1 merge  

### Completion checklist

- [x] Read brief and conclusions
- [x] Draft `spec.md` with frozen scope and owner checkboxes
- [x] Draft `plan.md` (implementation approach for Phase B)
- [x] Draft `tasks.md` (task breakdown)
- [ ] Run `/speckit-analyze` (read-only, Keel)
- [ ] Address findings / freeze
- [ ] Open spec PR with three unticked owner checkboxes
- [ ] User answers and merges (Gate 1)

---

## Phase B (delivery worktree, separate from spec-worktree)

**Trigger:** Gate 1 merge (user answers three checkboxes)  
**Worktree:** New delivery worktree from merged default branch, separate from this spec worktree  
**Branch:** `feature/293` (or `fix/293`, per naming preference)  
**Commit prefix:** `DEV-293 - {subject}`  

### Approach (bounded symbol rename + explicit reference edits + existing test updates)

#### 1. Setup (delivery worktree, Phase B start)

- Verify worktree location and branch: `git branch --show-current`
- Record property-tests opt-out in `task-DEV-293` before any edits (consumed by gate, §0)
- Pin scope: frozen envelope from spec.md Frozen committed scope and owner answers

#### 2. Symbol-aware mechanical rename (atomic change set)

Use compiler/Roslyn-assisted symbol rename restricted to the eight frozen mappings:

| Portuguese | English |
|---|---|
| `Filme` | `Movie` |
| `FilmesController` | `MoviesController` |
| `FilmesServices` | `MovieService` |
| `CriarFilme`/`ProcessarFilme` | `ImportMovieFolder` |
| `AssistirFilme` | `PlayMovie` |
| `ExcluirFilme` | `DeleteMovie` |
| `MinhaLista` | `Watchlist` |
| `GetDetalhesFilmeAsync` | `GetMovieDetails` |

**Scope restriction:** Apply only within in-scope files and declared types; exclude Q3 composites, Q5-A/B literals, test method names, local variable names, and Portuguese UI text.

**Files affected:**
- `src/LamuFlix.Web/Controllers/FilmesController.cs` → `MoviesController.cs`
- `src/LamuFlix.Web/Services/FilmesServices.cs` → `MovieService.cs`
- `src/LamuFlix.Web/Views/Filmes/` folder → `Views/Movies/` (directory move)
- `src/LamuFlix.Web/Views/Filmes/CriarFilme.cshtml` → `Views/Movies/ImportMovieFolder.cshtml`
- `src/LamuFlix.Web/Views/Filmes/MinhaLista.cshtml` → `Views/Movies/Watchlist.cshtml`
- `src/LamuFlix.Web/Views/Movies/Index.cshtml` (update references)
- `src/LamuFlix.Web/Views/Movies/Details.cshtml` (update references)
- `src/LamuFlix.Web/Views/Shared/_Layout.cshtml` (update link references)
- `src/LamuFlix.Web/Startup.cs` (verify conventional route `/Filmes/...` → `/Movies/...` follows from controller rename; read-only)

#### 3. Explicit non-C# reference updates

**Razor files (manual edits):**
- `_Layout.cshtml:30-31` — navigation links
- `Views/Movies/Index.cshtml:6,85,108,111` — controller/action tag helpers and method calls
- `Views/Movies/ImportMovieFolder.cshtml:16` — form target
- `Views/Movies/Watchlist.cshtml:46,69,72` — controller/action and method calls
- `Views/Movies/Details.cshtml:17,18` — JS call contracts

**JavaScript (manual edits in `site.js`):**
- Lines 5, 9, 22, 36, 50, 79, 103, 123, 127, 144, 159 — URL strings `/Filmes/` → `/Movies/`
- Line 5 — function call `AssistirFilme()` → `PlayMovie()`
- Line 118 — function call `ExcluirFilme()` → `DeleteMovie()`

**Exclusions (leave unchanged):**
- `[Route("/MinhaLista/")]` at `FilmesController.cs:159` (unchanged in Phase B; owner answer authorizes a follow-up only)
- JSON field `filmes` at `FilmesController.cs:113,115` and `site.js:23,37,80` (unchanged in Phase B; owner answer authorizes a follow-up only)
- `wwwroot/js/site.min.js` (generated; follow-up)
- Model folder `Models/Filmes/` and composites (unchanged in Phase B; owner answer authorizes a follow-up only)

#### 4. Existing test reference updates (atomic with rename)

**Update old symbol references in existing tests:**
- `tests/LamuFlix.Test/UnitTest1.cs:16,29,67,78,109`
  - `FilmesService` → `MovieService`
  - `AssistirFilme` → `PlayMovie`
- `tests/LamuFlix.Test/EnrichmentTests.cs:313,317`
  - `FilmesService` → `MovieService`
  - `CriarFilme` → `ImportMovieFolder`

**Keep unchanged:**
- Test method names
- Portuguese local variables and exception text
- No new behavior tests; no property-test mirrors of the rename

#### 5. Immediate build and test (atomic validation)

After renaming and reference edits as one change set:

```powershell
dotnet build
dotnet test
```

Both must succeed before proceeding to diff review and gates.

#### 6. Diff review and link smoke check

```powershell
git diff main...HEAD
```

Verify:
- No accidental behavior or architecture changes beyond the frozen scope
- Orphaned or duplicate references from partial renames
- Unchanged `/Filmes/` literals that should have changed (Q5-A/B aside)
- Excluded Q3/test-method names remain untouched

**Manual smoke verification** (if app runs locally):
- Index view loads and displays movies
- Details view displays and links (PlayMovie, DeleteMovie)
- ImportMovieFolder view opens and submits
- DeleteMovie action removes and refreshes
- PlayMovie action (if implemented)
- GetFilmesJson endpoint (verify field names — Q5-B)
- QuickSearch still works
- AddToWatchList and RemoveFromWatchList actions
- Watchlist view displays

**If database configuration prevents local startup:**
- Static check: Grep for old `/Filmes/` URL strings and verify they've been updated
- Razor tag-helper check: Confirm controller/action names in tag helpers match renamed types
- Log unverified route/link coverage as a follow-up (per spec.md)

#### 7. Sequential gate execution (Phase B only, after implementation stable)

Under pwsh 7, with `-BaseRef main` on `main...HEAD` diff. Run each analyzer sequentially; rerun `dotnet build` and `dotnet test` only if code edits occur:

```powershell
# Gate 1: Roslyn analyzers
./scripts/run-roslyn-analyzers.ps1 -BaseRef main
# Exit 0 expected; exit 1 blocks merge

# Gate 2: Cyclomatic complexity (implement threshold 15)
./scripts/run-cyclomatic-complexity.ps1 -BaseRef main
# Exit 0 expected; exit 1 blocks merge

# Gate 3: InspectCode (ReSharper inspections)
./scripts/run-jetbrains-inspectcode.ps1 -BaseRef main
# Exit 0 expected; exit 1 blocks merge

# Gate 4: Vulnerable packages
./scripts/run-vulnerable-packages.ps1
# Exit 0 expected; exit 1 blocks merge

# Gate 5: Solution build (already run; confirm)
dotnet build
# Success expected

# Gate 6: Test suite
dotnet test
# Pass expected

# Gate 7: Property tests (exit 2 expected; opt-out recorded in task-DEV-293)
./scripts/run-property-tests.ps1
# Exit 2 expected with opt-out recorded; report SKIPPED/OPT-OUT (not PASS)

# Pre-PR Gate 8: Mutation testing
dotnet stryker
# Expected: mutation score ≥ 80 (threshold not lowered)
# Fix surviving mutants via test coverage, never by threshold reduction
```

**Gate failure:** If any exit code is non-zero (except property-tests exit 2 with recorded opt-out):
- Fix the issue
- Re-run `dotnet build` and `dotnet test`
- Re-run failed gate
- Create a second fix commit in this round if needed (per round cap: max 2 commits/round)

**No `/web` gates:** No Vitest, web build, or frontend changes.

#### 8. Address code review and ship review findings

- **Round 1:** Receive findings; fix verified Critical/High/Medium in-scope defects; create fix commit(s) (≤2); rerun gates; send updated diff.
- **Round 2:** Receive findings; fix remaining verified Critical/High/Medium in-scope defects; create fix commit(s) (≤2); rerun gates; send updated diff.
- **At round cap:** Report unresolved above-bar findings to Patron as `blocked:` or `NEEDS FIXES`; no third round.
- **Low and out-of-scope findings:** Log as Follow-ups in the PR description (source, severity retained).

#### 9. Open delivery PR

**PR title:** `DEV-293 - Portuguese identifier rename: Filme → Movie`

**PR body:**
- Summary of changes (mechanical rename, files moved, references updated)
- Test strategy (existing tests updated; manual smoke; property-tests opt-out recorded)
- Gate results (Roslyn, complexity, InspectCode, vulnerable packages, build, test, stryker)
- Follow-ups from code/ship review (low, out-of-scope, unresolved above-bar)
- Owner checkbox status and any conditional acceptance items

**Branches:**
- Merge to: `main`
- From: `feature/293` (or assigned naming convention)

**Merge criteria:**
- All code-review and ship-review findings at or above closing bar are resolved
- Gates pass (or property-tests exit 2 with recorded opt-out)
- PR description includes merge-bar proof (gate screenshots/output)
- Owner checkboxes answered (if not, acceptance remains conditional per spec.md)

---

## Handoff and dependencies

### From Phase A to Phase B

**Trigger:** Gate 1 (user merge of spec PR with answered owner checkboxes)

**Deliverables from Phase A (this spec worktree):**
1. `spec.md` — frozen scope, owner checkboxes, test strategy, gate expectations
2. `plan.md` — implementation approach and gate sequence (this file)
3. `tasks.md` — task breakdown and assignments
4. Spec PR comment thread — findings from `/speckit-analyze` and any adjudication

**Owned by Phase B:**
1. Delivery worktree (separate from spec worktree)
2. C# symbol rename and file moves
3. Razor/JavaScript reference updates
4. Existing test reference updates
5. Gate execution and findings resolution
6. Delivery PR

### Dependency on owner answers

Phase B applies **frozen scope only** (Q1-Q2, the eight ticket mappings and their references) plus any Q3 and Q5-B targets the owner answers YES to and Patron then names in DEV-293. Constitution VIII and Governance make Q3 (composite identifiers and the `Models/Filmes/` move) and Q5-B (the `filmes` response field) compliance requirements, not optional follow-ups; Q5-A stays optional:

- **Q3:** On YES, Patron amends DEV-293 with the exact targets (`MoviesFilterViewModel`, `MoviesListViewModel`, `ImportMovieFolderViewModel`, `IMovieService`, `GetMoviesListAsync`, `GetMoviesJson`, `CreateMoviesListQuery`, namespace `LamuFlix.Web.Models.Movies`, folder `Models/Movies/`, file `MoviesViewModel.cs`) and they are frozen Phase B scope. On NO, the ticket is re-scoped or closed; no merge with Portuguese identifiers present.
- **Q5-A:** Optional. On YES, the URL changes to `/Watchlist/` in this delivery; on NO, it stays `/MinhaLista/`.
- **Q5-B:** Required by Constitution VIII. On YES, Patron amends DEV-293 and `filmes` becomes `movies` in frozen Phase B scope with `site.js:23,37,80` updated; on NO, the ticket is re-scoped or closed.
- **If a required answer is NO or absent:** Gate 1 does not close and DEV-293 does not merge.

---

## No code or implementation in Phase A

This plan is **specification and planning only.** Phase A writes `spec.md`, `plan.md`, `tasks.md`, and the spec PR.

**Phase B implementation does not start until after Gate 1 (user spec PR merge)** in a separate delivery worktree from the merged default branch.

Main checkout (`F:\Dev\LamuFlix`) remains clean throughout both phases.
