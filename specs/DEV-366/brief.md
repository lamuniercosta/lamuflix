# DEV-366 — Phase A Brief

Phase A grill outcome for DEV-366 (parent DEV-281, size M, Phase A spec sprint).
Decided by Patron; facts from `recon-DEV-366` and the DEV-366 ticket note only.
Grill questions: **10/12**.

## Closing bar

- **AC1 (ticket):** `run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING` exits 0; no remaining WARNING+ diagnostic outside justified suppression.
- **AC2 (corrected):** the Details.cshtml nullable finding — `AssignNullToNotNullAttribute` at `src/LamuFlix.Web/Views/Movies/Details.cshtml:68` — and `Razor.AssemblyNotResolved` at `src/LamuFlix.Web/Views/_ViewImports.cshtml:7` are resolved.
- **AC3 (ticket):** no new package reference, project, top-level folder, DB schema, or public API/DTO shape change.

## Frozen scope

- Deliverable = the **36 findings** inventoried in `recon-DEV-366` by ID (lines 18–27) and by file (lines 31–43). That set is an exact match to the DEV-361 Phase B baseline (`specs/DEV-361/research.md` lines 99–165, re-baseline after DEV-360), zero drift (`recon-DEV-366` lines 10–12, 64–68).
- Files in scope (11):
  - `src/LamuFlix.Data/Constants/GeneralConstants.cs`
  - `src/LamuFlix.Data/Models/MovieEnrichmentMessage.cs`
  - `src/LamuFlix.Web/Controllers/HomeController.cs`
  - `src/LamuFlix.Web/Models/Helper/PagedListing.cs`
  - `src/LamuFlix.Web/Services/IEnrichmentQueuePublisher.cs`
  - `src/LamuFlix.Web/Services/RabbitMqEnrichmentQueuePublisher.cs`
  - `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs`
  - `src/LamuFlix.Web/Views/_ViewImports.cshtml`
  - `src/LamuFlix.Web/Views/Movies/Details.cshtml`
  - `src/LamuFlix.Web/Views/Movies/Index.cshtml`
  - `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml`
- Out of scope: `WorkerTests.cs` (DEV-361's 3 findings); `.editorconfig` (severity settings are DEV-361's, `recon-DEV-366` lines 51, 75); any file the ticket does not name; any public API/DTO shape change.

## Round cap

- Review: 2 rounds maximum (standing §2.2); remediation ≤ 2 fix commits per round.

## Grill answers

- **Q1 — Which finding set is authoritative?**
  The measured set in `recon-DEV-366` (36 findings, ID table lines 18–27, file table lines 31–43). It equals the DEV-361 Phase B baseline with zero drift (`recon-DEV-366` lines 12, 64–68, 74). AC1 governs the exit condition.
- **Q2 — Details.cshtml: ticket `:60 ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` vs recon `:68 AssignNullToNotNullAttribute` — which wins?**
  Recon wins. The measured inventory is exhaustive for the ticket's own 36; the two lists are identical except this one entry (1-for-1), and recon's ID table contains no `ConditionIsAlwaysTrueOrFalse*` diagnostic. Ticket line 21 is stale. Authoritative finding: `AssignNullToNotNullAttribute` at `src/LamuFlix.Web/Views/Movies/Details.cshtml:68` (`recon-DEV-366` lines 26, 41).
- **Q3 — `Views/Filmes/...` (ticket) vs `Views/Movies/...` (recon)?**
  Post-rename path. DEV-293 moved `Views/Filmes` → `Views/Movies`; line numbers are unchanged at the new paths (`recon-DEV-366` lines 45–47). Use `Views/Movies/*`.
- **Q4 — Is the ticket's `ConditionIsAlwaysTrueOrFalse` entry a 37th finding that must also be resolved?**
  No. Recon is exhaustive and zero-drift, so resolving it is not possible and would add work outside the baseline. The ticket entry is corrected to the measured finding (Q2). DEV-366 still delivers exactly 36 resolutions; nothing is added, dropped, or reordered.
- **Q5 — DEV-361 overlap?**
  No conflict, no dependency. DEV-361 absorbs the 3 `WorkerTests.cs` findings; DEV-366 owns the other 36 (DEV-366 note line 10). Recon's file table contains no `WorkerTests.cs` (lines 31–43), so there is no shared file and no duplicated work. DEV-366's 36 are the DEV-361 research baseline (`recon-DEV-366` line 12).
- **Q6 — Fix or suppress?**
  AC1 accepts "justified suppression". Recon records the frozen policy (DEV-361 Q4, `.editorconfig` lines 67–76): bracket-style `// ReSharper disable/restore` at member level with a writer reason (`recon-DEV-366` lines 49–51). Per-finding fix-vs-suppress choice is Keel's plan decision. Patron boundary: in-place suppression is authorized; do **not** edit `.editorconfig` (unnamed file, §2.3 #6); no fix may change public API/DTO shape (AC3).
- **Q7 — Any §2.3 item?**
  None. Recon line 60: "No new §2.3 triggers: No packages, projects, schema, DTO, Features:LocalPlay, file deletes." AC3 restates the same. All 11 files are inside the ticket's named baseline scope.
- **Q8 — Verification command, and is `-All` permitted?**
  AC1 mandates `run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING` → exit 0. The pipeline's blanket `-All` ban (task-pipeline Phase 3 step 5) exists to keep legacy findings out of unrelated tickets; DEV-366's deliverable *is* the legacy baseline, so `-All` is ticket-decided here and Gauge must not refuse it. Baseline measured exit 1 (`recon-DEV-366` line 10).
- **Q9 — Which gates apply in Phase B?**
  The change touches `.cs` and `.cshtml`, so the standard gates apply: Roslyn, cyclomatic complexity, InspectCode (with the AC1 `-All` invocation), vulnerable packages, `dotnet format --verify-no-changes`, `dotnet test`; property tests accepted only with a recorded opt-out line. Web gates only if `web/` changes (it does not).
- **Q10 — Any owner-only structural choice?**
  No. The only ticket change is a corrected scope descriptor and AC2 wording that do not change what the ticket delivers (charter: Patron may correct a description/acceptance criterion that does not change delivery). No checkbox is required. If the plan later needs `.editorconfig`, `WorkerTests.cs`, or any other unnamed file, or a csproj/assembly-reference change, that becomes a §2.3 #1/#6 owner checkbox before implementation.

## Plan decisions

- **Approach:** resolve each of the 36 findings by justified in-place suppression (bracket-style `// ReSharper disable/restore` at member level, writer reason) or by a local fix that does not change public API/DTO shape. No `.editorconfig` severity edits.
- **Files touched:** the 11 files in Frozen scope.
- **Test strategy:** Phase B standard suite; no new tests required unless a fix changes behavior. Property-tests opt-out permitted only if recorded in the task note.
- **Gate expectations:** InspectCode `-All -MinSeverity WARNING` exit 0 (AC1); all other applicable gates exit 0.
- **Task-ordering constraints:** none (no dependency; `recon-DEV-366` line 60, DEV-366 note line 10).

## Ticket correction (Rigger)

Correct DEV-366 Scope bullet 8 and AC2: replace `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` / `LamuFlix.Web/Views/Filmes/Details.cshtml:60` with `AssignNullToNotNullAttribute` / `src/LamuFlix.Web/Views/Movies/Details.cshtml:68`. Finding count stays 36; delivery unchanged.

## Plan-challenge round 1 decisions (Keel, 2026-09-26)

Sources: the `/speckit-analyze` receipt in the DEV-366 note (lines 29–63) and `recon-DEV-366` lines 80–91. These decisions add to the sections above. Nothing above is changed.

- **D1 (F8): AC2 resolution mechanism.** For AC2, "resolved" means the file/line/ID no longer appears in the `-All -MinSeverity WARNING` output (SC-002).
  - `Razor.AssemblyNotResolved` at `_ViewImports.cshtml:7`: resolve it by **deleting line 7** (`@addTagHelper *, AlertsTagHelper`).
    - Line 6 already registers `LamuFlix.Web`, which defines `AlertsTagHelper` (`recon-DEV-366` lines 82–87, 91).
    - Do not suppress it. Do not rewrite the operand: a corrected operand would duplicate line 6. Make no `.csproj` or reference change (recon lines 89, 91).
    - If T009 still reports the finding after the deletion, stop and report to Keel. Do not fall back to suppression or to another file.
  - `AssignNullToNotNullAttribute` at `Details.cshtml:68`: prefer a code fix. A justified suppression (D3 form) is the permitted fallback under Q6, which covers all 36 findings.
- **D2 (F2): suppression form.** FR-009 governs: a paired `ReSharper disable <ID>` / `ReSharper restore <ID>` with a one-line reason. `disable once` is not used. In `.cs` files, the pair wraps the smallest enclosing member.
- **D3 (F5): markup form.** In `.cshtml` markup, the pair wraps the smallest contiguous run of flagged lines, never the whole file. Write it as Razor comments: `@* ReSharper disable Html.IdNotResolved — <reason> *@` … `@* ReSharper restore Html.IdNotResolved *@`.
  - Expected runs: `Index.cshtml` 58–73 and 108–114; `Watchlist.cshtml` 19–34 and 69–75.
  - Recon does not establish that InspectCode honours this form, so T009 is the proof. If any bracketed finding remains, stop and report to Keel before trying another form.
- **D4 (F6): `Html.IdNotResolved` mechanism.** All 18 are resolved by justified suppression in the D3 form. This is Keel's plan decision under Q6, chosen because it leaves the rendered markup unchanged. The claim "no code fix is available" is withdrawn, because recon line 58 does not establish it. The implementer has no discretion for this ID.
- **D5 (F3, F4): gates.** Gate expectations add two gates, and the opt-out line is recorded:
  - the refactor gate `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (`constitution.md:360`), exit 0;
  - the vulnerable-packages gate (Q9), exit 0;
  - the property-test opt-out line, recorded in the DEV-366 note (Q9).
- **D6 (F1): pickup drift.** At T001, drift means either:
  - a change to any of the 11 frozen-scope files between `97dc7348` and `origin/main`; or
  - any difference in the 36-finding re-baseline.

  Changes under `specs/DEV-366/` are expected and are not drift.
- **D7 (F7): stop routing.** A need for any file outside the 11 is §2.3 #6 (or #1 if it is a package or assembly reference). It is raised as `blocked: structural — <file/change>` for the owner, never as `needs decision:`. For T006 this is not expected (recon line 91).

## Gate 1 status

**Status: `gate1: provisional`** (Patron, 2026-09-26). Phase A is frozen. Keel's plan-freeze receipt (DEV-366 note lines 84–87) confirms R1–R5 resolved, coverage 15 requirements / 11 tasks at 100%, CRITICAL 0, and owner checkboxes 0 (Q10); there is no unresolved structural question. The spec PR carries no Gate 1 checkbox and Gate 1 needs no owner checkbox.
