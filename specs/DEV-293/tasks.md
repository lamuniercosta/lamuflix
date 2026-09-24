# DEV-293 task breakdown

**Phase:** A (spec-only) → B (implementation in separate delivery worktree after Gate 1)  
**Tracking:** YouTrack ticket DEV-293  
**Sprint:** DEV-281 (parent); independent of DEV-292  
**Team:** Quill (Phase A spec/plan), Bernstein (Phase B implementation — to be assigned)

---

## Phase A: Specification and planning (this worktree)

**Status:** Drafting → `/speckit-analyze` → Adjudication → Spec PR (Gate 1 pending)

### A1. Draft specifications (Quill)

- [x] Read brief.md and CONCLUSIONS.md from grill
- [x] Identify frozen scope: eight ticket-named mappings, unambiguous token substitutions, file/view moves, reference updates
- [x] Identify owner checkboxes: Q3 (composites/model paths), Q5-A (explicit `/MinhaLista/` route), Q5-B (JSON field `filmes`)
- [x] Draft `spec.md`: frozen scope, owner checkboxes, test strategy, gate expectations, follow-ups
- [x] Draft `plan.md`: Phase B implementation approach, sequential gates, handoff criteria
- [x] Draft `tasks.md` (this file): task breakdown for Phase B

**Output:** Three artifacts in `specs/DEV-293/`

### A2. Review and adjudication (Keel)

- [ ] Run `/speckit-analyze` on spec.md and plan.md (read-only, non-blocking findings)
- [ ] Identify plan weaknesses or conflicts with task-pipeline
- [ ] Recommend remediation if needed
- [ ] Freeze both artifacts (no further Phase A edits)

**Output:** Adjudication comment on internal spec PR or recon

### A3. Spec PR and Gate 1 (Quill + user)

- [ ] Open spec PR from this branch (feature/293-spec)
- [ ] Include exactly three unticked owner checkboxes (Q3, Q5-A, Q5-B)
- [ ] Link grill brief and conclusions as evidence
- [ ] Document follow-ups (site.min.js, route/link coverage, propertyTests opt-out notation)
- [ ] User answers three checkboxes and merges PR
- [ ] Gate 1 closes (no Phase B start before this merge)

**Output:** Merged spec PR; Gate 1 closed

---

## Phase B: Implementation (separate delivery worktree after Gate 1)

**Status:** Waiting → Implementation → Code review → Ship review → Delivery PR merge

**Prerequisites:**
- Gate 1 merge (spec PR merged, user answered owner checkboxes)
- Separate worktree from merged `main`
- Branch: `feature/293` (or configured naming)
- Commit prefix: `DEV-293 - {subject}`

### B1. Setup and scope pinning (implementation lead, Bernstein TBD)

- [ ] Create delivery worktree: `worktree add <path> main`
- [ ] Verify branch: `git branch --show-current` → `feature/293`
- [ ] **Record property-tests opt-out in task-DEV-293** (before any C# edits; consumed by gate runner)
  - Notation: `propertyTests: opt-out — no domain invariant added by pure rename`
- [ ] Review and confirm frozen scope from spec.md; owner answers never change Phase B scope
- [ ] Verify excluded scope (Q3, Q5-A/B, test method names, local text)
- [ ] **One atomic change set:** Symbol rename + file/view moves + Razor/JS edits + test updates in single commit (or two if refactoring follows separately)

**Checklist:**
- [ ] Worktree created and branch confirmed
- [ ] Property-tests opt-out recorded in task-DEV-293
- [ ] Scope pinning reviewed

### B2. Symbol-aware mechanical rename (implementation lead)

Files and symbols (ordered by edit complexity):

**C# controller rename and move:**
- [ ] Rename `src/LamuFlix.Web/Controllers/FilmesController.cs` → `MoviesController.cs` (file move)
  - [ ] Update class name `FilmesController` → `MoviesController`
  - [ ] Update action methods: `CriarFilme`/`ProcessarFilme` → `ImportMovieFolder`, `AssistirFilme` → `PlayMovie`, `ExcluirFilme` → `DeleteMovie`, `MinhaLista` → `Watchlist`, `GetDetalhesFilmeAsync` → `GetMovieDetails`
  - [ ] Exclude: hand-written `[Route("/MinhaLista/")]` (unchanged in Phase B; owner answer authorizes a follow-up only)

**C# service interface/implementation rename and move:**
- [ ] Rename `src/LamuFlix.Web/Services/FilmesServices.cs` → `MovieService.cs` (file move)
  - [ ] Leave the interface name `IFilmesService` unchanged in Phase B (owner answer authorizes a follow-up only)
  - [ ] Update class name `FilmesServices` → `MovieService`
  - [ ] Update method names: `GetMinhaLista` → `GetWatchlist`, `GetDetalhesFilmeAsync` → `GetMovieDetails`
  - [ ] Update parameter names: `filmeId` → `movieId` (lines 25,27,31,41,43,45,68,119,123,132,334,336,344,346,355,357)
  - [ ] Exclude: method `GetFilmesJson`, method `GetFilmesListAsync`, interface `IFilmesService` - all unchanged in Phase B; owner answer authorizes a follow-up only

**Razor view folder and files:**
- [ ] Move folder `src/LamuFlix.Web/Views/Filmes/` → `Views/Movies/`
- [ ] Rename `Views/Filmes/CriarFilme.cshtml` → `Views/Movies/ImportMovieFolder.cshtml`
- [ ] Rename `Views/Filmes/MinhaLista.cshtml` → `Views/Movies/Watchlist.cshtml`
- [ ] Rename `Views/Filmes/Index.cshtml` → `Views/Movies/Index.cshtml` (or keep as-is if view name unchanged)
- [ ] Rename `Views/Filmes/Details.cshtml` → `Views/Movies/Details.cshtml` (or keep as-is)

**Conventional route verification (read-only):**
- [ ] Verify `src/LamuFlix.Web/Startup.cs:72-74` conventional route `{controller=Home}/{action=Index}/{id?}`
- [ ] Confirm `/Filmes/...` → `/Movies/...` follows from controller rename; do not add explicit `[Route]` attributes

### B3. Explicit Razor and JavaScript reference updates (implementation lead)

**Razor tag helpers and links:**
- [ ] `src/LamuFlix.Web/Views/Shared/_Layout.cshtml:30-31`
  - [ ] Update controller references from `Filmes` to `Movies`
- [ ] `src/LamuFlix.Web/Views/Movies/Index.cshtml:6,85,108,111`
  - [ ] Update controller tag helper: `asp-controller="Filmes"` → `asp-controller="Movies"`
  - [ ] Update action tag helper: `asp-action="ExcluirFilme"` → `asp-action="DeleteMovie"` (line 108, 111)
  - [ ] Update method call: `ExcluirFilme()` → `DeleteMovie()` in JavaScript call (if present)
- [ ] `src/LamuFlix.Web/Views/Movies/ImportMovieFolder.cshtml:16`
  - [ ] Update form tag helper: `asp-controller` and `asp-action` to match renamed controller/action
- [ ] `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml:46,69,72`
  - [ ] Update controller/action references
  - [ ] Update method calls as needed
- [ ] `src/LamuFlix.Web/Views/Movies/Details.cshtml:17,18`
  - [ ] Update JavaScript call contracts: `AssistirFilme()` → `PlayMovie()`, `ExcluirFilme()` → `DeleteMovie()`

**JavaScript URL strings and function calls:**
- [ ] `src/LamuFlix.Web/wwwroot/js/site.js`
  - [ ] Lines 5, 9, 22, 36, 50, 79, 103, 123, 127, 144, 159 — URL strings `/Filmes/` → `/Movies/`
  - [ ] Line 5 — function call `AssistirFilme()` → `PlayMovie()`
  - [ ] Line 118 — function call `ExcluirFilme()` → `DeleteMovie()`
  - [ ] Exclude: lines 23, 37, 80 - JSON field `filmes` (unchanged in Phase B; owner answer authorizes a follow-up only)
  - [ ] Exclude: `wwwroot/js/site.min.js` (generated; follow-up)

### B4. Existing test reference updates (implementation lead)

Update only old symbol references in existing tests; method names unchanged:

- [ ] `tests/LamuFlix.Test/UnitTest1.cs`
  - [ ] Line 16: `FilmesService` → `MovieService`
  - [ ] Line 29: `FilmesService` → `MovieService`
  - [ ] Line 67: `AssistirFilme` → `PlayMovie` (if present)
  - [ ] Line 78: `FilmesService` → `MovieService`
  - [ ] Line 109: `FilmesService` → `MovieService`
- [ ] `tests/LamuFlix.Test/EnrichmentTests.cs`
  - [ ] Line 313: `FilmesService` → `MovieService`, `CriarFilme` → `ImportMovieFolder`
  - [ ] Line 317: `FilmesService` → `MovieService`

**Exclusions:**
- [ ] Do NOT rename test method names
- [ ] Do NOT add new test methods that mirror the rename
- [ ] Do NOT change Portuguese local variables or exception text

**Atomic verification (after all B2, B3, B4 edits complete):**
```powershell
dotnet build
dotnet test
# Expected: Build succeeds, all tests pass (same test results as before)
```

### B5. Diff review and smoke verification (implementation lead)

```powershell
git diff main...HEAD
```

**Verify:**
- [ ] No accidental behavior changes beyond frozen scope
- [ ] No orphaned or duplicate references
- [ ] Old `/Filmes/` URL literals addressed (Q5-A/B aside)
- [ ] Q3 composites and test method names untouched
- [ ] Hand-written `/MinhaLista/` route unchanged (owner answer authorizes a follow-up only)
- [ ] JSON field `filmes` unchanged in response (owner answer authorizes a follow-up only)

**Manual smoke tests (if app runs locally):**
- [ ] Index view loads; displays movies
- [ ] Details view displays and links work (PlayMovie, DeleteMovie)
- [ ] ImportMovieFolder view opens and submits
- [ ] DeleteMovie action removes and refreshes list
- [ ] PlayMovie action (if implemented)
- [ ] GetFilmesJson endpoint returns data (verify field names match Q5-B)
- [ ] QuickSearch still works
- [ ] AddToWatchList and RemoveFromWatchList actions work
- [ ] Watchlist view displays saved movies

**Fallback (if database prevents local startup):**
- [ ] Static check: Grep for remaining `/Filmes/` URL strings (should find none or only intentional Q5-A exceptions)
- [ ] Razor tag-helper check: Confirm `asp-controller="Movies"` and action names match renamed methods
- [ ] Log unverified route/link coverage as follow-up

**Completion:**
- [ ] Diff review approved
- [ ] Smoke tests passed or fallback checks logged

### B6. Sequential gate execution (implementation lead)

Run under pwsh 7 with `-BaseRef main` on `main...HEAD` diff:

**Gate 1: Roslyn analyzers**
```powershell
./scripts/run-roslyn-analyzers.ps1 -BaseRef main
```
- [ ] Exit 0 (pass)
- [ ] If exit 1: Fix violations and re-run (count as additional build/test + re-run, within round cap)

**Gate 2: Cyclomatic complexity (implement threshold 15)**
```powershell
./scripts/run-cyclomatic-complexity.ps1 -BaseRef main
```
- [ ] Exit 0 (pass)
- [ ] If exit 1: Extract private helpers, early returns, guard clauses; re-run

**Gate 3: InspectCode (Rider/ReSharper)**
```powershell
./scripts/run-jetbrains-inspectcode.ps1 -BaseRef main
```
- [ ] Exit 0 (pass)
- [ ] If exit 1: Fix inspections; re-run

**Gate 4: Vulnerable packages**
```powershell
./scripts/run-vulnerable-packages.ps1
```
- [ ] Exit 0 (pass)
- [ ] If exit 1: Upgrade/patch package; re-run `dotnet build` and `dotnet test`; re-run gate

**Gate 5: Solution build**
```powershell
dotnet build
```
- [ ] Success (already verified in B2)

**Gate 6: Test suite**
```powershell
dotnet test
```
- [ ] All tests pass (already verified in B4)

**Gate 7: Property tests**
```powershell
./scripts/run-property-tests.ps1
```
- [ ] Exit 2 expected (no domain invariant added by rename)
- [ ] **Property-tests opt-out recorded in task-DEV-293** (prerequisite: see B1)
- [ ] Report: SKIPPED/OPT-OUT (not PASS)

**Pre-PR Gate 8: Mutation testing (pre-PR check)**
```powershell
dotnet stryker
```
- [ ] Expected: Mutation score ≥ 80 (never lower threshold)
- [ ] If score < 80: Write tests for surviving mutants; re-run `dotnet stryker`
- [ ] If score ≥ 80: Approve for PR

**Gate completion:**
- [ ] All gates passed (exit 0, or property-tests exit 2 with recorded opt-out)
- [ ] Record gate results in PR description

### B7. Formal review findings and fixes (implementation lead + reviewers)

**Review round 1 (code review or ship review, or combined):**
- [ ] Receive findings
- [ ] Fix verified in-scope Critical/High/Medium defects (max 2 commits this round)
- [ ] Re-run `dotnet build`, `dotnet test`, and failed gate(s)
- [ ] Send updated diff

**Review round 2 (if needed, final round):**
- [ ] Receive additional findings or corrections
- [ ] Fix verified in-scope Critical/High/Medium defects (max 2 commits this round)
- [ ] Re-run gates
- [ ] Send updated diff

**At round cap (2 formal review rounds TOTAL, max 4 commits across both):**
- [ ] Report unresolved Critical/High/Medium findings to Patron as `blocked:` or `NEEDS FIXES`
- [ ] No third review round
- [ ] Low and out-of-scope findings logged as Follow-ups (source, severity retained)

### B8. Delivery PR (Bernstein or assigned)

- [ ] PR title: `DEV-293 - Portuguese identifier rename: Filme → Movie`
- [ ] PR description:
  - [ ] Summary of changes
  - [ ] Test strategy and manual smoke results
  - [ ] Gate results (all gates pass, property-tests exit 2 with opt-out)
  - [ ] Code review findings summary (resolved + follow-ups)
  - [ ] Ship review findings summary (resolved + follow-ups)
  - [ ] Follow-ups (site.min.js, route/link coverage, Q3/Q5 owner answers)
  - [ ] Owner checkpoint status
- [ ] Branch: `feature/293` → `main`
- [ ] Merge criteria:
  - [ ] All code-review and ship-review Critical/High/Medium findings resolved
  - [ ] Gates pass (property-tests exit 2 with recorded opt-out is acceptable)
  - [ ] PR description includes merge-bar proof
  - [ ] Ready for merge

**Output:** Merged delivery PR; ticket acceptance conditional on owner answer follow-ups

---

## Follow-ups (post-Gate 1, post-delivery)

These are not findings; they are intentional exclusions and logged gaps:

1. **Generated JavaScript (`site.min.js`):** Regenerate after Phase B; verify `/Movies/` references in minified output. Linked from `_Layout.cshtml:64`? Verify and update if needed.
2. **Route/link automated coverage:** In-process route tests would require `Microsoft.AspNetCore.Mvc.Testing` NuGet dependency, outside Phase B scope. Document manual smoke in PR description; open follow-up for automated regression test.
3. **Q3 owner checkpoint (model composites):** If authorized in spec PR, open follow-up issue for `Models/Filmes/` folder rename, composite identifiers, namespace updates.
4. **Q5-A owner checkpoint (explicit route):** If authorized, update hand-written `[Route("/MinhaLista/")]` → `[Route("/Watchlist/")]` in follow-up issue.
5. **Q5-B owner checkpoint (JSON field):** If authorized, update response field `filmes` → `movies` in follow-up issue. Verify all callers updated.
6. **Test method naming:** Portuguese → English test method name refactor is a separate refactor issue, not included in this pure identifier rename.

---

## Acceptance criteria (Phase B completion)

All of the following must be true:

1. ✓ Symbol rename complete: All eight ticket-named mappings applied within frozen scope
2. ✓ File and folder moves complete: Controllers, Services, Views, action-named Razor files
3. ✓ Razor and JavaScript references updated: All listed lines in spec.md updated
4. ✓ Existing tests updated: Old symbol references replaced; method names unchanged; tests pass
5. ✓ Solution builds: `dotnet build` succeeds
6. ✓ Tests pass: `dotnet test` succeeds (same as or better than before)
7. ✓ Gates pass: Roslyn (exit 0), complexity (exit 0), InspectCode (exit 0), vulnerable packages (exit 0), property-tests (exit 2 with opt-out recorded), stryker (≥80)
8. ✓ Diff review: No accidental behavior changes; exclusions (Q3, Q5-A/B, test names, local text) remain untouched
9. ✓ Smoke verification: Manual or static checks logged; unverified coverage documented as follow-up
10. ✓ Code review findings: All in-scope Critical/High/Medium resolved; low/out-of-scope logged as Follow-ups
11. ✓ Ship review findings: All in-scope Critical/High/Medium resolved; low/out-of-scope logged as Follow-ups
12. ✓ Delivery PR: Merged with merge-bar proof and follow-up documentation
13. ⊗ Conditional: Acceptance line 20 (zero Portuguese in API surface) is conditional on owner answers to Q3, Q5-A, Q5-B

Owner checkpoint answers determine scope for any Phase C follow-ups.
