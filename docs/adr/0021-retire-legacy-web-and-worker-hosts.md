# 0021. Retire legacy Web and Worker hosts

- Status: Proposed (becomes Accepted when the DEV-388 Gate 1 spec PR merges)
- Date: 2026-10-15
- Ticket: DEV-388

## Context

DEV-388 removes the legacy C# Web and Worker hosts after their required Api and React SPA replacements are evidenced. The missing capability is metadata-only movie deletion through the existing Core, Infrastructure and Api seams, and that deletion is in place before Web retirement. Patron Q3 supersedes the initial director/collection parity interpretation: existing P1 filters satisfy this retirement, and director/collection facets and collection persistence remain outside DEV-388. Constitution governance and migration constraints are verified in recon-DEV-388-2 section 14; the external locked product-plan P2 passages at F:/Dev/lamuflix-react-pivot-plan.md:33,47,94 are Patron-cited and were not independently verified within recon roots.

Basis: [brief](../../specs/DEV-388/brief.md); [conclusions](../../specs/DEV-388/CONCLUSIONS.md), Q1–Q9 and the Step 6 adjudication rulings.

## Decision

Patron Q2/Q6 selects atomic removal of one movie with existing PostgreSQL cascades, preserving shared actor/director/genre entities and physical media. recon-DEV-388-2 section 13 substantiates this without a schema change. Do not retain the legacy hosts solely for the missing delete action, and do not introduce a second service/repository layer.

Keep unrelated legacy test-project content. Remove only the tests, orphan model and project references expressly ruled in Q4. Backend contract generation precedes frontend consumption, and full parity evidence precedes Web deletion.

## Consequences

No migration, constitutional amendment, or playback execution change is approved. See the brief and the append-only conclusions for scope, behavior, verification, provenance and review limits. This ADR records the grill decision for the specification review. It does not grant Gate 1 or merge approval.

The decision becomes Accepted when the DEV-388 Gate 1 spec PR merges. The delivery PR may change only this status line to `Accepted`. A change to the decision text needs a new Patron ruling.
