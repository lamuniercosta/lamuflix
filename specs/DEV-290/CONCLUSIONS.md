# DEV-290 grill conclusions

Q1–Q4 were decided in the prior grill and were never committed. Their only record is the `DEV-290` canvas note, and Patron re-held them on 2026-09-23. D1–D5 come from the 2026-09-23 drift re-confirm against `recon-DEV-290` at `450e70f`. That exchange used 1 of the 12 allowed.

## Q1 — Unused legacy bower libraries
All 8 `LamuFlix.Web/wwwroot/lib` folders are used by live views. **Ruling: no-op; delete none.**

## Q2 — Scope guard
**Ruling:** this ticket changes paths only. No `.cs` or test content edits. No Web→Api or Data→Infrastructure rename. No DEV-289 fixes.

## Q3 — ADR
**Ruling: yes.** The number is set by D5.

## Q4 — Loop terms
**Ruling:** only Critical or High findings with a concrete failure scenario block. The frozen scope is the ticket's Scope only. The cap is 2 rounds. Acceptance tests are opted out.

## D1 — LamuFlix.Web.old drift
The folder is absent at `450e70f` (`git ls-tree` returns 0 paths) and from all reachable history. The old inventory base `e65cd71` no longer resolves. **Ruling:** the sub-item is void by drift. It stays as an acceptance check (`Test-Path` False). No delete commit, no change to the summary or acceptance criteria, no plan change. A recon comment is queued for Rigger.

## D2 — Gates on pure renames
The gate obligation covers new or materially changed C# (`constitution.md:327,332`). A rename at 100% similarity changes no code. The DEV-361 P1 hard stop does not apply here, because DEV-290 names no content edit. **Ruling:** the pass bar is no finding missing from a `450e70f` `-Files` baseline measured in a throwaway detached worktree. Findings are matched by rule and code line, ignoring the path prefix. The bar covers complexity at both 15 and 6. Pre-existing hits are carried, never fixed or suppressed, and listed as follow-ups (DEV-281, DEV-366; precedent DEV-360 `plan.md:29`). A new finding that only a `.cs` edit could clear → `blocked: structural` to Patron.

## D3 — Temp.cs
The deletion is named in the ticket, so it is not a Q2 content edit. Nothing references the file: no other references, no DbSet, no EF snapshot entity. **Evidence:** a green build plus the zero-reference check.

## D4 — Non-.cs path references
**Ruling (narrowed):** editing a path string counts as neither deleting nor rewriting, so §2.3 item 6 does not fire. At `450e70f` the complete set is `LamuFlix.sln` plus 5 `ProjectReference` lines in 3 csproj files. `scripts/`, `.github/`, `stryker-config.json`, `Directory.*.props`, and `harness.yml` contain no project paths. The plan-time recon addendum lists the exact lines. Any other file, or any edit beyond a path string → back to Patron.

## D5 — Spec Kit and ADR number
`.specify/` is tracked on main, so the Spec Kit blocker is gone. **Ruling:** the ADR number is **0013**, not 0001, because the constitution reserves ADR-0001 through ADR-0012 for the architecture plan. The file is `docs/adr/0013-src-tests-solution-layout.md`, and it cites plan §3 and Epic 1 item 2. Fact: none of the prior Phase A artifacts was ever on disk, so all of them are rewritten.
