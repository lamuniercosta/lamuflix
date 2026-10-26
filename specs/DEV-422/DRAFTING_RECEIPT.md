# DEV-422 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-26
**Seat**: Quill
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-422-spec` | **Branch**: `feature/422-spec` | **HEAD at drafting**: `e960f440ae928bd3a5bdbf3261885511f1d65d05`

**Method**: `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` skills read in that order. `$env:PYTHONUTF8='1'` set before Spec Kit scripts. `setup-plan.ps1 -Json` ran with feature directory `specs/DEV-422` (branch `feature/422-spec`). After `spec.md` existed, `setup-tasks.ps1 -Json` was re-run. `.specify/extensions.yml` registers harness hooks (`before_specify` → grill-with-docs already complete per frozen brief; `after_tasks` → speckit-analyze owned by Keel next). Quill did not execute analyze, clarify, or checklist. Spec, plan, and tasks were filled from the templates and from `brief.md`, `CONCLUSIONS.md` (Q1–Q6), `ASSUMPTIONS.md`, notes `DEV-422` and `recon-DEV-422`, Accepted ADR-0022, and `specs/PRODUCT.md` sections 1–5. No decisions made by Quill. Brief and CONCLUSIONS were not edited.

**Artifacts**:
- `specs/DEV-422/spec.md`
- `specs/DEV-422/plan.md`
- `specs/DEV-422/tasks.md`
- `specs/DEV-422/DRAFTING_RECEIPT.md`

No `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `design.md`, or `checklists/requirements.md` (M path / Conductor: no clarify/checklist; plan states why; no ui tag so no Palette design.md; brief freezes approach). No ADR. `.specify/feature.json` already pinned to `specs/DEV-422` and was not rewritten. `AGENTS.md`, `brief.md`, and `CONCLUSIONS.md` were not edited. No CONTEXT.md rewrite.

**Owner checkbox** (carried verbatim into `spec.md` and tasks Phase 1): `blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?` Gate 1 is not claimed and stays closed until answered. Analyze not run by Quill.

**Focused test paths named in plan** (brief Q6 duty; paths taken from recon inventory of existing App/details tests plus brief-named new/repurpose files):
- `web/src/library/useEnrichmentStatus.test.tsx` (new)
- `web/src/library/libraryQueries.polling.test.tsx` (repurpose)
- `web/src/library/LibraryView.retry.test.tsx` (update)
- `web/src/App.test.tsx` (focused App)
- `web/src/features/movie-details/MovieDetailsView.test.tsx` (focused details)
- `web/e2e/import.e2e.ts` (conditional)

**Needs decision**: none open after analyze fix round 1 (propertyTests disposition settled by Patron Phase 2 Analyze Rulings / brief supplement).

**Needs recon**: none raised for Phase A drafting. Phase B still requires Wisp pickup drift recon after owner checkbox, landed approved upstream constitution disposition/amendment, and DEV-330 landing (tasks T001), including the exact published `@microsoft/signalr` 10.0.x patch.

**Preserved gaps / evidence limits** (not invented):
- Exact `@microsoft/signalr` 10.0.x patch NOT invented in Phase A (brief Q6).
- Landed EnrichmentHub / generated EnrichmentStatusChangedDto / `@microsoft/signalr` package NOT FOUND at recon baseline `52e44366` (recon-DEV-422:54-66, 129-133); Phase B STOP.
- Proposed ADR-0029 remains Proposed; not treated as Accepted (brief Test strategy).
- Constitution remains authoritative; Accepted ADR-0022 notes pending constitution amendment and does not supersede constitution polling text; upstream DEV-330 constitution-departure owner checkbox unresolved (recon-DEV-422:130-132; CONCLUSIONS Phase 2 Analyze Rulings). Constitution-text editing remains out of DEV-422 scope; no duplicate constitution checkbox.

## Analyze fix round 1 (2026-10-26)

Applied Keel's numbered fix list only to owned `spec.md`, `plan.md`, `tasks.md`, and this receipt. `$env:PYTHONUTF8=1`. Facts from `recon-DEV-422`, Patron rulings in CONCLUSIONS.md Phase 2 Analyze Rulings, and brief Phase 2 analyze decisions supplement. Brief/CONCLUSIONS/ASSUMPTIONS not edited.

1. **C1 CRITICAL constitution authority**: Corrected `plan.md` API/Contract Rules and Complexity Tracking so they no longer claim ADR-0022 supersedes constitution polling text or present constitutional compliance. Stated pending disposition honestly (ADR-0022 accepted transport + pending amendment; constitution authoritative; DEV-330 constitution-departure checkbox + Proposed ADR-0029 unresolved). Bound Phase B prerequisite/checkpoint wording across `spec.md` (edge cases, FR-001, FR-017, Assumptions), `plan.md` (Status, Scope/prerequisites, ordering, Phase boundaries), and `tasks.md` (Tests header, T001 + checkpoint, Phase 1 dependencies, Notes) to landed approved upstream constitution disposition/amendment alongside landed hub implementation/generated chain and drift verification. Preserved existing DEV-422 payload-type checkbox; no duplicate constitution checkbox; no constitution editing.

2. **G1 MEDIUM missing gate disposition**: Carried exact `propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants` into `plan.md` Testing, `spec.md` edge cases/FR-015/Assumptions, and `tasks.md` Tests header + T015, citing brief Phase 2 analyze decisions and CONCLUSIONS Phase 2 Analyze Rulings. Kept mandatory Vitest matrix and actual gate exit reporting; opt-out never PASS; no new library. Removed prior `needs decision:` propertyTests line from this receipt.

**Stop point**: Analyze fix round 1 applied. Next is Keel re-analyze / alignment. Gate 1 remains closed pending owner checkbox. No ready-for-plan-challenge claim. Quill did not commit and did not run tests or gates.

## Plan Challenge correction round 1 (2026-10-26)

Applied Keel's numbered fix list only to owned `spec.md`, `plan.md`, `tasks.md`, and this receipt. Facts from brief Phase 2 plan-challenge decisions and Patron CONCLUSIONS.md Phase 2 Challenge Rulings. Brief/CONCLUSIONS/ASSUMPTIONS not edited. Scope/files, FR-001–FR-017 / SC-001–SC-006 IDs, owner checkbox verbatim, propertyTests opt-out, and Phase B STOP preserved. Lows deferred per brief stay deferred. No Gate 1 freeze/claim; no gates/implementation.

1. **Freshness (Sentry M1 / brief item 1)**: Updated FR-007, US1 acceptance scenario 5, US3 acceptance scenario 4, edge cases, plan Design/Performance, and T006–T009 so coalescing cannot drop a valid event during an older refetch; explicit R1-start → E2 → stale-R1-complete → fresh-convergence proofs for root browse and mounted details; no synthetic server sequence.

2. **Ownership and bridge (Sentry M2 / Ledger M1–M2 / Compass M1 / brief item 2)**: Updated FR-004/FR-005/FR-008, Key Entities, plan Structure/Design/Constraints, and T003–T005/T009 for instance-owned connection/timer/registry; injected hook-parameter factory with private default helper; narrow subscription return passed via existing main/App/details props; publish valid events plus start/reconnect independently of cache presence/status; cleanup unsubscribe; no global mutable registry / new context/store/module / second connection / reverse cycle; T003 teardown asserts zero fake live connections and pending timers before restore; hand-written fake, no new package.

3. **Validation (Ledger M3 / Sentry L4 / brief item 3)**: Updated FR-007, plan Design item 2, and T006/T008 for existing Zod runtime validation — exactly one argument, non-null object, finite integer id, four exact status strings; malformed matrix null/missing/string/fractional/non-finite id, unknown status, 0 or 2+ args; no uncontracted positivity rule or duplicate DTO.

4. **Pin and verification hygiene (Ledger M4 / brief item 4)**: Updated FR-012/FR-015, plan Constraints/ordering, and T002/T013 for `--save-exact` with no ranges at package+lock root; post-pin `npm audit` records introduced findings and disposition without blanket-failing unrelated historical advisories; production-build size delta without new tooling/budget; browser runtime imports + `import type`; no Node-only runtime transport.

5. **Acceptance proofs (Compass L2/L3 / brief item 5)**: Updated FR-011/FR-014, US1 aria-live acceptance, US3 no-event start/reconnect acceptance, T007 (explicit no-event successful initial-start AND reconnect details reconciliation for absent-browse id), and T010 (concrete `aria-live="polite"` assertion).

6. **Failure behavior (Sentry L3 / brief item 6)**: Updated FR-007, edge cases, plan Design, and T006/T008 so a failed authoritative refetch retains the event status patch and converges on subsequent event/recovery; no rollback machinery.

**Stop point**: Plan Challenge correction round 1 applied. Next is Keel verification before plan freeze. Gate 1 remains closed. Quill did not commit and did not run tests or gates.
