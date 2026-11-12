# DEV-501 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-12  
**Seat**: Quill  
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-501-spec` · branch `feature/501-spec`  
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` with `-clarify` and `-checklist` in one Conductor ask; later plan-challenge repair round 1 (D1–D5 + Principle V) applied from brief/CONCLUSIONS/recon only.

**Inputs**: `brief.md` (incl. D1–D5 + Principle V clarification), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `design.md` (Palette; not edited), `docs/adr/0041-explicit-subtitle-acquisition.md` (cite only), Maestri notes `DEV-501` and `recon-DEV-501` (incl. 111–130), `specs/PRODUCT.md`, `.specify/memory/constitution.md` (cite only).

**Artifacts written/updated (Quill-owned this round)**:

- `specs/DEV-501/spec.md`
- `specs/DEV-501/plan.md`
- `specs/DEV-501/research.md`
- `specs/DEV-501/data-model.md`
- `specs/DEV-501/quickstart.md`
- `specs/DEV-501/contracts/subtitle-search-download.md`
- `specs/DEV-501/tasks.md`
- `specs/DEV-501/checklists/requirements.md`
- `specs/DEV-501/checklists/ux.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `design.md` (Palette), `PLAN_ADJUDICATION.md` (Keel), ADR 0041 / ADR 0038, constitution (planned only; amendment is post-Gate-1 task), production/test code, `harness.yml`, generated OpenAPI/TS, main checkout.

**Clarify**: Session 2026-11-12 recorded in `spec.md`. Subsequent plan-challenge catalog, D1–D5, and Principle V rulings refine contracts; Gate 1 and plan freeze remain separate.

**Owner checkbox**: none (Q2 constitution maintenance ruled consistent; Principle V clarification NOT blocked: structural). Ordinary owner Gate 1 remains open.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS/design/ADR/recon. Unverified exact paths/workflows remain gated pickup checks (T003/T004/T006/T025/T026) because recon:116/119/125/128 inventories are incomplete — not product guesses and not treated as authoritative source discoveries outside recon.

## Analyze round 1–2 repair receipts (earlier)

See prior sections in git history if needed. Analyze rounds closed under two-round cap before plan challenge.

## Plan-challenge repair round 1 receipt (Keel → Quill)

Applied numbered repair list 1–8 plus ownership of `contracts/subtitle-search-download.md`, Principle V 422 clarification, independent reread corrections (T018 precheck order; shared-edit serialization; T025–T027 then T022–T024; PrepareSubtitleCommand.cs sweep-candidate labeling; exact catalog `.cs` filenames), and provenance fix (no invented D6/D7 — FR-014 cites D4 + recon incompleteness; FR-018 cites brief Final drafting and test directions / Q8).

1. **Catalog**: route/DTO/`SupportedSubtitleLanguages`/canonical normalization/empty preferences; T033 foundational dependency with exact planned filenames; contracts + data-model + checklists + quickstart propagated.
2. **D1 + Principle V**: ProblemDetails reasons; quota freshness/Retry-After TimeProvider; POST movie→binding→read-only→config→containment; known-zero 429; distinct search/download 429; caller cancellation≠timeout; 422 for ValidationException / 400 outside it; centralized handler + traceId; T018 enumerates same order as T016/spec/brief.
3. **D2**: concrete Singleton `SubtitleCandidateBinder` (no interface), `SubtitleMatchScorer`, typed TTL/cap/byte/attempt options, earliest-expiry FIFO, 32-hex seeded-Random without crypto claims, process-local stale.
4. **D3**: streaming 2 MiB, 10_000 publication attempts, staged reuse, create/pre-stage/pre-publication containment, cancellation cleanup + approved statuses.
5. **D4**: delete/update/retain table; additive qualified glossary; constitution 1.15.0→1.16.0 + ADR 0041 acceptance post-Gate-1; ADR 0038 Proposed wording-only; `PrepareSubtitleCommand.cs` labeled sweep candidate not confirmed D4 delete.
6. **Verification / sequencing**: current-source verification before deletion; installer/snapshot as pickup discovery (recon incomplete); preserve dirty harness; sequential shared options/validator/endpoint edits; after US2 execute T025→T026→T027 then T022→T023→T024.
7. **Filenames / property tests**: exact planned new `.cs` names; mandatory applicable property gate for scorer/ranking; existing paths verified at pickup only.
8. **Stale claims removed**: no “no open questions / optional helper / if-present harness / 32-task complete” assumptions; FR/SC mapped to actual tasks.

**Status at Quill handoff (historical)**: Repair round 1 complete pending Keel final freeze; plan was not yet frozen. Gate 1 NOT approved. Runtime OPEN. **Current status**: Keel subsequently reread the repairs and recorded PLAN FROZEN in PLAN_ADJUDICATION.md; Gate 1 and runtime remain separate.

**needs decision:** (none from Quill for this pass)

**Gaps / handoffs (not Quill edits)**:

- Palette-owned `design.md` D5 and Principle V repairs were completed and independently reread by Keel.
- Keel `PLAN_ADJUDICATION.md` records the final consistency and freeze receipt.
- Implementation pickup must not treat incomplete recon inventories as complete source facts.

**Next**: Conductor may prepare the spec PR for ordinary owner Gate 1 under the pipeline. This receipt grants no Gate 1 or execution approval; current analysis/freeze claims belong to Keel PLAN_ADJUDICATION.md.
