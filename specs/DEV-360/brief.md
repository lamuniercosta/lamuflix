# DEV-360 — Close DEV-289 leftovers

**Ticket:** DEV-360 · size:M · no `ui:` tag · Phase A spec sprint on `feature/360-dev-360-spec`.

## Scope and authority

The six items in `task-DEV-360:14-29` are the authorized delivery. The ticket decides the two connection fallback removals and their missing-variable outcomes, `GenericRepository.Delete(object id)` not-found exception, CLR nullability aligned to the existing snapshot without a migration, `harness.yml` warning setting, and a bounded DEV-289 Standards review. `specs/PRODUCT.md` §§1,3 and charter §2.3 make the ticket's Scope & Technical Design authoritative. Owner decisions D1–D3 of 2026-09-23 (`task-DEV-360:81-84`) are recorded in [spec.md](spec.md): keep `ProcessStarter` unchanged, defer the Delete not-found test to DEV-280, and add design-time `Microsoft.EntityFrameworkCore.Design` 9.0.x with a pinned `dotnet-ef` 9.0.x.

Patron confirmed the DB-only test guard and navigation relationship rule in [CONCLUSIONS.md](CONCLUSIONS.md). Current chain order is DEV-360 before DEV-290 and DEV-280 (`chain:7-11`), so `Player.cs` is in the live nullability scope (`Temp.cs` is unmapped and out of FR-004 scope) and `UnitTest1.cs` has not been deleted. At Phase B pickup, recheck this against `main`.

## Review loop

Critical or High findings with a concrete failure scenario block. Frozen scope: DEV-360 Scope & Technical Design and its acceptance criteria; anything else is a follow-up issue, not a finding in this round. Maximum two review rounds and two fix commits per round (`task-pipeline:23-29`; [CONCLUSIONS.md](CONCLUSIONS.md)).

## Gate 1 owner checkboxes (answered 2026-09-23)

D1–D3 are answered; the canonical checkboxes are in [spec.md](spec.md) "Gate 1 and authorization envelope". Patron rulings C1/C2, H1, H2, and M2 (`task-DEV-360:89-90`) need no owner choice.

Gate 1 not yet passed: it passes when analysis is clean, the plan is frozen, and the user merges the spec PR.

## Taste assumptions

The exception message wording is the sole `[assumed]` taste choice; see [ASSUMPTIONS.md](ASSUMPTIONS.md). No test project, package, or owner option is assumed; all three were decided by the owner.
