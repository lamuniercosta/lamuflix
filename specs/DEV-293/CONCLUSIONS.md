# DEV-293 Grill Conclusions

## Q1 — Controller rename and conventional routes

**Keel asked:** Renaming `FilmesController` to `MoviesController` could change convention-derived HTTP routes. Do PRODUCT/ticket/spec authorize a route change, or must existing route shapes stay fixed? I recommend preserving routes because the ticket requires a mechanical identifier rename with no behavior change; the cost is explicit route attributes or an owner checkbox if the current route is derived from the class name.

**Patron answered:** The route change is authorized and required; existing `/Filmes/...` route shapes must not be preserved. Ticket Scope & Technical Design line 9 explicitly decides `FilmesController` → `MoviesController`, and `specs/PRODUCT.md` section 3 makes the ticket's named scope authoritative. `src/LamuFlix.Web/Startup.cs:72-74` uses conventional `{controller=Home}/{action=Index}/{id?}` routing, so `/Movies/...` follows from the named class rename. Ticket line 18 requires references and tests updated; `site.js` and Filmes views carry old paths. Ticket acceptance line 20 requires zero Portuguese identifiers in the public or internal API surface. This is not an unresolved §2.3.4 choice.

**Decision and implications:** Rename the controller and update its conventional route references to `/Movies/...`; do not preserve old `/Filmes/...` endpoints through route attributes. No behavior or architecture logic change beyond the ticket's mechanical rename. No owner checkbox or taste assumption for this point.

---

## Q2 — Files and paths implied by named identifiers

**Keel asked:** Ticket names identifiers but not physical filenames or view folders. Does §2.3.6 allow matching file/path renames needed for those symbols and references, or must unnamed file/path moves be owner checkboxes? I recommend treating exact matching renames and necessary reference edits as the ticket's mechanical scope; the cost is reviewing each moved path to prevent unrelated rewrites.

**Patron answered:** Exact matching file/path renames and required reference edits are authorized. Ticket lines 8–15 name the symbols; line 17 requires a pure mechanical rename; line 18 requires all references and tests updated; acceptance line 20 requires zero Portuguese identifiers in API surfaces. `specs/PRODUCT.md` section 3 makes ticket Scope authoritative. §2.3.6 applies to collateral deletion or rewriting outside the ticket. In scope: `Controllers/FilmesController.cs` → `MoviesController.cs`, `Services/FilmesServices.cs` → `MovieService.cs`, `Views/Filmes` → `Views/Movies`, action-named views `CriarFilme.cshtml` → `ImportMovieFolder.cshtml` and `MinhaLista.cshtml` → `Watchlist.cshtml`, plus reference edits in `site.js`, `_Layout.cshtml`, `Index.cshtml`, and tests. `Models/Filmes/`, `FilmesViewModel.cs`, and other folder restructuring are not covered by this ruling because their symbols are not individually named.

**Decision and implications:** Permit matching moves and reference edits for ticket-named symbols; prohibit unrelated folder restructuring. The scope of generic `Filme` → `Movie` as applied to composite identifiers needs a separate decision. No taste assumption.

---

## Q3 — Composite identifiers and model paths

**Keel asked:** Recon finds `FilmesViewModel` and Patron identified `Models/Filmes/`, while the ticket has a generic `Filme` → `Movie` mapping and requires zero Portuguese API identifiers. Does that mapping authorize renaming composite model identifiers and their files/folders, or is this an owner checkbox under §2.3.6? I recommend a narrow owner checkbox if the ticket does not explicitly name the composite; the cost is Gate 1 waiting for the owner. Please reconcile this with Q2's boundary.

**Patron answered:** This is an owner checkbox under §2.3.6. Q2 covers only exact counterparts of symbols named in ticket Scope & Technical Design lines 8–15, such as `Views/Filmes` accompanying `FilmesController`. The ticket does not name `FilmesFilterViewModel`, `FilmesListViewModel`, `CriarFilmeViewModel`, `IFilmesService`, `GetFilmesListAsync`, `GetFilmesJson`, `CreateFilmesListQuery`, namespace `LamuFlix.Web.Models.Filmes`, folder `Models/Filmes/`, or file `Models/Filmes/FilmesViewModel.cs`. Its naming targets mix plural and singular, so no reliable composite target rule follows. Acceptance line 20 still requires zero Portuguese identifiers in the API surface. Patron recommends, subject to owner confirmation: `MoviesFilterViewModel`, `MoviesListViewModel`, `ImportMovieFolderViewModel`, `IMovieService` and `MovieService`, `GetMoviesListAsync`, `GetMoviesJson`, `CreateMoviesListQuery`, namespace `LamuFlix.Web.Models.Movies`, folder `Models/Movies/`, file `Models/Movies/MoviesViewModel.cs`.

**Decision and implications:** `blocked: structural —` Owner must authorize renaming the listed composite identifiers and moving the listed model folder/file, and confirm their exact target names. Gate 1 remains closed. Do not assume or perform these moves. No taste assumption.

---

## Q4 — Code-review and ship-review closing bar

**Keel asked:** For code-review and ship-review, which severities block this mechanical rename's closing bar? I recommend all verified Critical/High/Medium defects block, with Low issues recorded as follow-ups when outside frozen scope; the cost is resolving every Medium before merge. The Q3 composite checkbox stays on the spec PR outside committed scope.

**Patron answered:** Every verified in-scope Critical, High, or Medium finding blocks both stages. Low remains a Follow-up with its source and severity, never relabelled Non-blocking. Out-of-frozen-scope findings are Follow-ups regardless of severity. Critical/High remain non-discretionary even in Follow-ups: `.claude/skills/ship-review/SKILL.md:148` requires none unresolved in the consolidated report for READY, and `.claude/skills/code-review/SKILL.md:248` says a deferred Critical/High prevents the stage clearing. Medium is an edge-case bug, partially implemented requirement, or costly structural smell under code-review line 257; ticket lines 17 and 20–21 make this rename's completeness and behavior preservation binary. The Q3 composites stay outside the Q1–Q2 frozen scope under ship-review line 126. This is stricter than the Critical/High house defaults in DEV-280, DEV-360, and DEV-361.

**Decision and implications:** Verified in-scope Critical/High/Medium block; Low and out-of-scope findings are Follow-ups with honest severity. A Critical/High Follow-up still prevents READY until resolved under the cited stage rules. The Q3 checkbox does not expand committed scope. Frozen scope and round cap await separate decisions.

---

## Q5 — Frozen scope and API-shape exceptions

**Keel asked:** Confirm exactly the ticket-named mappings, matching file/view/route moves and necessary reference/test edits from Q1–Q2, with no behavior or architecture logic change. Exclude Q3 composites and other unnamed renames; anything else is a follow-up issue, not a finding in this round. The cost is that acceptance's zero-Portuguese criterion cannot fully close before owner answers.

**Patron answered:** Confirmed in: the eight ticket-named mappings at task lines 8–15, with `CriarFilme`/`ProcessarFilme` sharing target `ImportMovieFolder`; exact controller/service file moves, `Views/Filmes` → `Views/Movies`, `CriarFilme.cshtml` → `ImportMovieFolder.cshtml`, `MinhaLista.cshtml` → `Watchlist.cshtml`; convention-derived `/Filmes/...` → `/Movies/...` routes from `Startup.cs:72-74`; necessary reference and test edits including `site.js`, `_Layout.cshtml`, `Index.cshtml`, `MinhaLista.cshtml`, and tests. Ticket line 17 forbids behavior or architecture logic change. Confirmed out: Q3 composites, `Models/Filmes/`, `FilmesViewModel.cs`, and unnamed renames. Patron clarified: freeze matching file/view/route moves to convention-derived routes only; hand-written API-shape literals the ticket does not name stay out under §2.3.4.

**Owner checkbox A — hand-written route:** The ticket names the `MinhaLista` action (line 14) but not `[Route("/MinhaLista/")]` at `src/LamuFlix.Web/Controllers/FilmesController.cs:159`. Rename the action to `Watchlist` and view to `Watchlist.cshtml` in this scope, leaving the URL unchanged. Owner must choose: authorize a later URL change to `/Watchlist/`, or confirm `/MinhaLista/` stays.

**Owner checkbox B — JSON response field:** The ticket does not name JSON field `filmes` emitted at `src/LamuFlix.Web/Controllers/FilmesController.cs:113,115`, consumed at `src/LamuFlix.Web/wwwroot/js/site.js:23,37,80`. Leave the field name unchanged in this scope. Owner must choose: authorize a later response-contract rename to `movies`, or confirm `filmes` stays.

**Decision and implications:** Frozen committed scope is the confirmed IN envelope, excluding Q3 and both §2.3.4 literals. Anything else is a follow-up issue, not a finding in this round. Put all three unticked owner checkboxes on the spec PR. Gate 1 stays closed until answered; ticket acceptance line 20 remains partly open. No taste assumption.

---

## Q6 — Formal review round cap

**Keel asked:** What is the review round cap for the frozen mechanical rename? I recommend two formal rounds, matching the default, with unresolved owner-only checkboxes carried on the spec PR rather than spending rounds to guess them; the cost is any verified blocker after round two needs explicit process disposition.

**Patron answered:** Two formal review rounds, at most two fix commits per round. Sources: `.agents/skills/remediate/SKILL.md:12`, `task-pipeline:23-29`, and DEV-280/DEV-290/DEV-360/DEV-361 precedent. At the cap, no more fix commits or third round. Remaining findings retain source and severity. An unresolved Critical/High still prevents READY under ship-review line 148; an unresolved in-scope Medium prevents clearing under the Q4 bar and code-review line 248. Report blocked or NEEDS FIXES to Patron. Q3 and both Q5 owner checkboxes consume no review round.

**Decision and implications:** Cap is two formal rounds with no more than two fix commits each. Above-bar findings at cap remain blockers with explicit disposition, never a silent relabel. Owner checkboxes travel on the spec PR outside the round budget. No taste assumption.

---

## Q7 — Implementation approach and files

**Keel asked:** Should Quill plan a symbol-aware mechanical rename of only the frozen mappings, then update references/views/routes/tests and review the diff for accidental behavior changes? I recommend it, at the cost of manual Razor/JavaScript handling and careful exclusion checks. No independent code search by Keel.

**Patron answered:** Confirmed as the ticket's own mandate (lines 17–18). Use compiler/Roslyn-assisted symbol rename restricted to the eight frozen mappings. Explicitly edit Razor references in `_Layout.cshtml:30-31`, `Views/Filmes/Index.cshtml:6,85`, `CriarFilme.cshtml:16`, `MinhaLista.cshtml:46`; move `Views/Filmes` and the two action-named views; update `site.js` URL strings at lines 5, 9, 22, 36, 50, 79, 103, 123, 127, 144, 159. Leave untouched the hand-written `/MinhaLista/` route literal, `filmes` JSON field, Q3 composites and model paths, and `wwwroot/js/site.min.js`. `_Layout.cshtml:64` loads `site.js`, not `site.min.js`; the latter is generated and belongs to a follow-up. End with diff review for behavior changes and standard gates. No opportunistic cleanup/refactor.

**Decision and implications:** Plan bounded symbol rename, explicit non-C# references, the listed path moves, excluded-surface checks, and diff/gates in that order. The file list is the recon/Patron evidence for planning, not permission for incidental edits. No taste assumption.

---

## Q8 — Test strategy

**Keel asked:** Update existing tests naming old symbols, add only a focused in-scope route/link regression test if coverage is missing, then run solution tests; avoid new behavior tests that mirror a rename. Cost: manual review of untested Razor/JS links.

**Patron answered:** Ticket lines 17–18 and acceptance line 21 require reference updates, build, and existing tests without behavior changes. Update existing references in `tests/LamuFlix.Test/UnitTest1.cs:16,29,67,78,109` (`FilmesService` → `MovieService`, `AssistirFilme` → `PlayMovie`) and `EnrichmentTests.cs:313,317` (`FilmesService` → `MovieService`, `CriarFilme` → `ImportMovieFolder`). Add no behavior tests merely mirroring a rename; existing LocalPlay tests cover the flag-gated path and must remain green without changing execution. An in-process route test would need new `Microsoft.AspNetCore.Mvc.Testing` beyond the test project's listed dependencies, a §2.3.1 owner item, so exclude it. Document a manual smoke check for Index, Details, PlayMovie, ImportMovieFolder, DeleteMovie, GetFilmesJson, QuickSearch, AddToWatchList, RemoveFromWatchList, and Watchlist view; log missing automated route coverage as a follow-up. Keep test method names unchanged. Record the per-ticket property-tests opt-out in the task note because there is no new domain invariant, avoiding a blocking SKIPPED. Do not alter the harness mutation threshold of 80.

**Decision and implications:** Update only existing test symbol references, run the existing solution suite, document the manual smoke and coverage follow-up, and carry the property-tests opt-out to the task note. No new dependency or test method rename. No taste assumption.

---

## Q9 — Gate expectations

**Keel asked:** Which gates and exact pass/skip expectations should Quill plan for this C# rename? I recommend changed-diff analyzer/complexity/InspectCode gates, solution build/test, vulnerability check, pre-PR mutation gate, and the recorded Q8 property-test opt-out. Exit 2 needs reason classification; tooling failure blocks evidence.

**Patron answered:** `AGENTS.md`, `harness.yml`, and ticket acceptance line 21 govern. Under pwsh 7 on the `main...HEAD` changed diff, run each analyzer sequentially with `-BaseRef main` and expect exit 0: `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1` (implement threshold 15), `run-jetbrains-inspectcode.ps1`. Run `run-vulnerable-packages.ps1` with configured settings (expect 0), solution `dotnet build` (succeed), and `dotnet test` (pass). `run-property-tests.ps1` is expected to exit 2 because no tests are tagged; accept only with the Q8 per-ticket opt-out recorded in task-DEV-293, and report SKIPPED/OPT-OUT, never PASS. Pre-PR run `dotnet stryker` against configured mutation threshold 80; fix surviving mutants by tests, never lower threshold. No `/web` change, so no Vitest or web build gate. Scope-empty analyzer exit 2 is blocking. An unwired or otherwise unrunnable gate is `Could not run`, never green. Plain `dotnet build` cannot replace an analyzer. Gate-runner classifies mechanical evidence only.

**Decision and implications:** Plan the named commands and verdict rules. No `-All` on analyzer gates. The property opt-out must be durably recorded before its exit 2 is accepted. No threshold changes. No taste assumption.

---

## Q10 — Task ordering, pending phase correction

**Keel asked:** Confirm a plan ordering of scope/exclusion pinning and owner checkboxes, C# symbol rename, exact file/view/Razor/JS edits, existing test references, diff and route/link smoke, gates and follow-ups, then spec PR. Cost: Gate 1 conditional.

**Patron answered:** Confirmed with amendments: worktree/branch check first, leaving main clean and using `DEV-293 - {subject}` commits; record property-tests opt-out in task-DEV-293 at scope pinning; treat code rename, path/reference edits, and test updates as one atomic change set; run solution build and `dotnet test` immediately afterward, rerunning later only if edits occur; if the app cannot run due to configured database, use static stale `/Filmes/` and Razor tag-helper checks and report the unverified link contract as a follow-up; route follow-ups to Patron, not directly into the chain. Gate 1 and acceptance line 20 stay conditional on owner checkboxes.

**Open dependency detected by Keel:** This proposed order places implementation in the spec worktree before the spec PR, contradicting `task-chain` §0 lines 6–9, where Phase A spec PR and user merge/Gate 1 precede Phase B implementation in a separate delivery worktree. Q11 must correct phase placement before the ordering can be frozen. No code work is authorized by Q10.

---

## Q11 — Correct phase split

**Keel asked:** Q10 placed implementation before the spec PR, contrary to `task-chain` §0 lines 6–9. Confirm Phase A in this spec worktree as brief → Quill spec/plan/tasks → analyze/challenge/freeze → spec PR with three checkboxes; Phase B only after Gate 1 in a separate delivery worktree, using Q10's code/test/gate sequence.

**Patron answered:** Confirmed. Phase A touches specs only: this brief, Quill artifacts, clean `/speckit-analyze`, adjudicated plan challenge/freeze, then spec PR with exactly three unticked owner checkboxes (Q3 composites/model paths, Q5-A hand-written `/MinhaLista/` route, Q5-B `filmes` JSON field). Record follow-ups for `site.min.js`, unchanged test method names, and route/link coverage. Gate 1 stays closed until user answers the boxes and merges; no Phase B before merge. Phase B starts in its own DEV-293 delivery worktree from merged default branch, checks worktree/branch and commit format, pins scope and task-note property opt-out, performs atomic code/path/reference/test rename, builds/tests early, inspects diff and smokes links with static fallback, runs Q9 gates, routes follow-ups to Patron, and opens delivery PR. Q4–Q10 decisions otherwise stand.

**Decision and implications:** Freeze the two-phase order above. Q10's code tasks belong exclusively to Phase B. This Phase A writes only spec artifacts; the spec worktree is not the delivery worktree. No taste assumption.

---

## Q12 — Shared understanding and final scope correction

**Keel asked:** Confirm Q1–Q2 and Q4–Q11 as complete and mutually consistent for Quill, with exactly three owner checkboxes outside committed scope, no other unresolved structural decision, and a spec PR allowed while Gate 1 remains closed. Recommended yes, at the cost of acceptance line 20 staying conditional.

**Patron answered:** Confirmed after one scope correction. Add three in-scope cases: `filmeId` → `movieId` in service interface parameters and implementations at `FilmesServices.cs:25,27,31,41,43,45,68,119,123,132,334,336,344,346,355,357`; `GetMinhaLista` → `GetWatchlist` at that file's lines 47,368; shipped JS call-contract symbols `AssistirFilme` → `PlayMovie` and `ExcluirFilme` → `DeleteMovie` at `site.js:5,118` and Razor callers at `Details.cshtml:17,18`, `Index.cshtml:108,111`, `MinhaLista.cshtml:69,72`. These are unambiguous substitutions from ticket-named mappings inside in-scope files/types and are not new §2.3 choices. Decision rule: IN for an unambiguous ticket-mapping token substitution inside an in-scope file and declared type. OUT as an owner checkbox for ambiguous target names (`Filmes` singular/plural composite cases), unnamed file/folder moves, or public API shape changes (Q5 literal route/JSON field). `IFilmesService`, `GetFilmesListAsync`, `GetFilmesJson`, `CreateFilmesListQuery`, `FilmesListViewModel`, and `FilmesFilterViewModel` stay in Q3. Explicitly OUT and not a finding: existing test method names, Portuguese local and exception text at `FilmesServices.cs:75`, hardcoded `_filePath`, and Razor UI copy. Exactly three owner checkboxes remain; no other structural decision is open. The spec PR may open, Gate 1 stays closed, and acceptance line 20 is conditional until owner answers.

**Decision and implications:** Apply Patron's correction to the Q5 frozen envelope. The Q12 shared understanding is complete with that correction. No taste assumption.
