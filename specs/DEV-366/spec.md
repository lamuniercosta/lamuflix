# Feature Specification: Resolve 36 remaining InspectCode WARNING+ findings

**Feature Branch**: `feature/366-spec` (Phase A spec sprint)  
**Created**: 2026-09-25  
**Status**: Phase A — `gate1: provisional` (Patron, 2026-09-26); the spec PR is open and Gate 1 needs no owner checkbox (`brief.md` Q10).  
**Input**: DEV-366 ticket note, `recon-DEV-366`, and `specs/DEV-366/brief.md` (Patron grill 10/12, 2026-09-25).

## User Scenarios & Testing

### User Story 1 — InspectCode exits clean (Priority: P1)

As a maintainer, running `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING` produces exit 0 with no WARNING+ diagnostic outside justified suppression, so the tool reports a clean codebase. **Independent test:** that command exits 0 after all fixes and suppressions are applied. The 36 findings inventoried in `recon-DEV-366` (lines 14–43) are resolved.

### User Story 2 — Authoritative findings are verified at measured locations (Priority: P1)

As a maintainer, the two findings called out in the ticket — `AssignNullToNotNullAttribute` at `src/LamuFlix.Web/Views/Movies/Details.cshtml:68` and `Razor.AssemblyNotResolved` at `src/LamuFlix.Web/Views/_ViewImports.cshtml:7` — are resolved, so the corrected AC2 scope is verifiably closed. **Independent test:** these two file/line/ID triples no longer appear in the `-All -MinSeverity WARNING` output.

### Edge Cases

- The ticket's original `:60 ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` entry is stale. Recon wins (Q2): the authoritative finding is `AssignNullToNotNullAttribute` at `Details.cshtml:68`. There is no 37th finding (Q4); delivery is exactly 36 resolutions.
- View paths use `Views/Movies/*` (post-DEV-293 rename). The pre-rename paths (`Views/Filmes/*`) are obsolete (Q3).
- `.editorconfig` severity settings are frozen at their DEV-361 values and are out of scope (Q6, `recon-DEV-366` lines 49–51).
- `WorkerTests.cs` is out of scope; its 3 findings belong to DEV-361 (`brief.md` line 28; DEV-366 note line 10).
- Fix-vs-suppress choice is per-finding. In-place suppression is authorized where no suitable code fix exists, using the form required by FR-009: member-level `// ReSharper disable/restore` pair in `.cs` files, or the Razor comment pair in `.cshtml` files (D3), with a writer reason (`brief.md` Q6, `recon-DEV-366` lines 49–51). `Html.IdNotResolved` is suppressed by design, chosen to leave rendered markup unchanged (D4).
- No fix may change a public API or DTO shape (AC3).

## Requirements

### Functional Requirements

- **FR-001**: Resolve all 18 `Html.IdNotResolved` findings in `src/LamuFlix.Web/Views/Movies/Index.cshtml` (lines 58, 61, 64, 67, 70, 73, 108, 111, 114) and `src/LamuFlix.Web/Views/Movies/Watchlist.cshtml` (lines 19, 22, 25, 28, 31, 34, 69, 72, 75). Technique (fix or justified suppression) is a plan decision.
- **FR-002**: Resolve all 7 `RedundantUsingDirective` findings: `src/LamuFlix.Data/Constants/GeneralConstants.cs` (lines 1, 2, 3) and `src/LamuFlix.Web/Controllers/HomeController.cs` (lines 1, 2, 4, 5). Remove or suppress each directive.
- **FR-003**: Resolve all 3 `RedundantNullableDirective` findings at line 1 of `src/LamuFlix.Data/Models/MovieEnrichmentMessage.cs`, `src/LamuFlix.Web/Services/IEnrichmentQueuePublisher.cs`, and `src/LamuFlix.Web/Services/RabbitMqEnrichmentQueuePublisher.cs`. Remove or suppress each directive.
- **FR-004**: Resolve all 3 `ConditionalAccessQualifierIsNonNullableAccordingToAPIContract` findings in `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs` (lines 139, 157, 175). Fix or justify each conditional access.
- **FR-005**: Resolve both `RedundantCast` findings in `src/LamuFlix.Web/Models/Helper/PagedListing.cs` (both at line 46). Remove the redundant casts.
- **FR-006**: Resolve the `Razor.AssemblyNotResolved` finding at `src/LamuFlix.Web/Views/_ViewImports.cshtml:7` (AlertsTagHelper reference). This is a named AC2 item (`brief.md` line 10).
- **FR-007**: Resolve the `AssignNullToNotNullAttribute` finding at `src/LamuFlix.Web/Views/Movies/Details.cshtml:68`. This is the corrected AC2 item (Q2; the ticket's stale entry `:60 ConditionIsAlwaysTrueOrFalse*` is superseded by recon).
- **FR-008**: Resolve the 1 `EmptyConstructor` finding in `src/LamuFlix.Data/Constants/GeneralConstants.cs:9`. Remove or suppress the empty constructor.
- **FR-009**: All suppressions must be paired `// ReSharper disable <ID>` / `// ReSharper restore <ID>` at member level with a one-line writer reason (in `.cs` files). In `.cshtml` markup, the pair is written as Razor comments: `@* ReSharper disable <ID> — <reason> *@` … `@* ReSharper restore <ID> *@`, wrapping the smallest contiguous run of flagged lines, never the whole file (D3). No `.editorconfig` severity edits, no `[SuppressMessage]`, no `#nullable disable`, and no file-scoped suppression sections.
- **FR-010**: No new package reference, project, top-level folder, DB schema change, or public API/DTO shape change (AC3). All edits are in the 11 files named in the frozen scope.

## Success Criteria

- **SC-001**: `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING` exits 0 after all changes are applied. No WARNING+ diagnostic remains outside justified suppression (AC1).
- **SC-002**: AC2 "resolved" means the file/line/ID no longer appears in the `-All -MinSeverity WARNING` output (D1). `AssignNullToNotNullAttribute` at `Details.cshtml:68` and `Razor.AssemblyNotResolved` at `_ViewImports.cshtml:7` must both be absent from that output (AC2).
- **SC-003**: All applicable Phase B gates exit 0: Roslyn analyzers, cyclomatic complexity, cyclomatic complexity refactor gate (`./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`, `constitution.md:360`), InspectCode (`-All -MinSeverity WARNING`), `dotnet format --verify-no-changes`, and `dotnet test`. `dotnet list package --vulnerable` (Q9; `constitution.md:405`) reports clean. The property-test opt-out line is recorded in the DEV-366 note (Q9). Web gates do not apply (no `web/` changes).
- **SC-004**: `git diff origin/main --stat` lists only the 11 frozen-scope files. No `.editorconfig`, `WorkerTests.cs`, NuGet package, or public API/DTO change appears in the diff.
- **SC-005**: Every suppression bracket carries a one-line reason. No file-level disable bracket and no `[SuppressMessage]` appear in the diff.

## Gate 1 and authorization envelope

Gate 1 is open. No owner checkbox is required (`brief.md` Q10). The ticket correction (Rigger) — replacing the stale `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract / Details.cshtml:60` entry with `AssignNullToNotNullAttribute / Details.cshtml:68` — is a description/acceptance-criterion correction that does not change delivery; the charter permits it without a checkbox. The authorized file set is the 11 files in the frozen scope. Any need for a file outside the 11 is raised as `blocked: structural — <file/change>` for the owner (§2.3 #6, or §2.3 #1 if it is a package or assembly reference); it is never assumed or waived by the implementer (D7). T007 (`Details.cshtml:68`) prefers a code fix; justified suppression (D3 form) is the permitted fallback under Q6/D1.

- [x] **Process (status record):** Patron set `gate1: provisional` on 2026-09-26. Phase A is frozen: Keel's plan-freeze receipt (DEV-366 note lines 84–87) confirms R1–R5 resolved, coverage 15 requirements / 11 tasks at 100%, CRITICAL 0, and owner checkboxes 0 (Q10); no structural question is open. The spec PR carries no Gate 1 checkbox.

