# DEV-361 — Restore five InspectCode severities

**Ticket:** DEV-361 · size:M · no `ui:` tag · Phase A spec sprint on `feature/361-dev-361-spec` · **gate1: provisional** (Patron, 2026-09-23: spec PR ready for owner review; Gate 1 stays closed on the B1 owner checkbox and the owner merge).

## Scope and authority

`task-DEV-361:14-30` authorizes five `.editorconfig` rules to return to `warning` and bounds fixes to the files those inspections flag. It explicitly keeps `resharper_nullable_warning_suppression_is_used_highlighting` at `suggestion` pending the user's reversal of DEV-289 B1. The five rule IDs and no-new-package constraint are ticket decisions under `specs/PRODUCT.md` §3 and charter §2.3; they need no new checkbox.

Among ten candidate files, the read-only SUGGESTION probe found ten target issues in `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` (3), `LamuFlix.Web/TagHelpers/Extensions.cs` (2), and `LamuFlix.Test/UnitTest1.cs` (5), as recorded in [CONCLUSIONS.md](CONCLUSIONS.md). A later solution-wide probe found an eleventh target hit at `LamuFlix.Web/Views/Filmes/Details.cshtml:60`; it belongs to the separate InspectCode follow-up. The three C# paths are frozen for AC2. The candidate probe exited 1 because 162 suggestions met the deliberately low threshold; it was not a passing gate.

## Review loop

Critical or High findings with a concrete failure scenario block. Frozen scope: the five ticket-authorized restorations, the three measured flagged files, and ticket acceptance criteria; anything else is a follow-up issue, not a finding in this round. Maximum two review rounds and two fix commits per round (`task-pipeline:23-29`; [CONCLUSIONS.md](CONCLUSIONS.md)).

## Gate 1 owner checkbox

- [ ] **blocked: structural — restore `nullable_warning_suppression_is_used` to warning (reverses B1)?** Until the user answers, `.editorconfig:68` stays `suggestion` (`task-DEV-361:21,30`; charter §2.3).

`gate1: provisional` (Patron, 2026-09-23) opens this spec PR for owner review; it does not close the box above. No Patron grill ruling answers B1, and Phase B stays blocked on T002 until the user answers on the PR and merges.

## Taste assumptions

The suppression wording and local syntax choice are logged in [ASSUMPTIONS.md](ASSUMPTIONS.md).
