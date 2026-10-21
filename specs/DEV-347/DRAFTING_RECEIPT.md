# DEV-347 Phase A Spec Kit Drafting Receipt

**Date**: 2026-10-21
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-347-spec` | **Branch**: `feature/347-spec` | **HEAD at drafting**: `8919fdd1441081ab2d8b5cd699c7ad8b3194ac6c`

**Method**: `speckit-specify`, `speckit-plan`, and `speckit-tasks` skills read. `$env:PYTHONUTF8='1'` set before the setup scripts. `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` ran with `SPECIFY_FEATURE_DIRECTORY` set to `specs/DEV-347`. Plan JSON returned `FEATURE_SPEC`, `IMPL_PLAN`, and `SPECS_DIR` under that directory, branch `feature/347-spec`. Tasks JSON returned `AVAILABLE_DOCS` empty and the core `tasks-template.md`. `.specify/extensions.yml` is absent, so no before/after hooks ran. Spec, plan, and tasks were filled from the templates and from `brief.md`, note `recon-DEV-347`, and `specs/PRODUCT.md` (Patron rulings already carried verbatim in the brief / CONCLUSIONS / ASSUMPTIONS). No decisions made by Quill.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` (plan states why). `.specify/feature.json` already contained `specs/DEV-347` and was not rewritten. `AGENTS.md` was not edited.

**Owner checkbox**: none. Q1–Q9 are closed Patron rulings. Gate 1 is not opened by this draft.

**Needs decision (for Keel)**: none raised.

**Needs recon**: none raised.

---

## Analyze round 1 correction (2026-10-21)

**Basis**: Clarified analyze fix list in task note `DEV-347:45-53` (findings at `DEV-347:41-46`); implements `brief.md:190` (focused unit and real-infrastructure tests with the relevant implementation tasks; end-to-end after the producer path is wired). Clarification only — not a new product decision. Original drafting HEAD `8919fdd1441081ab2d8b5cd699c7ad8b3194ac6c` and Method above are unchanged and remain authoritative for how the draft was produced.

**I1 MEDIUM — phase membership / dependencies / checkpoints / proof placement**: Rewrote `tasks.md` phases exactly as clarified: Phase 1 Setup T001; Phase 2 Foundational message/port/topology/routing/publisher and focused proofs T002–T009 plus T012; Phase 3 US1/US2 Core eligibility publish T010–T011; Phase 4 US1/US2 Consumer e2e and Q9 settlement T013–T014; Phase 5 Polish T015–T016. Removed the standalone topology-proof phase and moved T012 out of the consumer phase. Phase 2 dependency order and checkpoint (compile + green `RabbitMqTopologyTests` / `EnrichmentRoutingTests` / `RabbitMqPublisherTests`, registration tests if changed, fixture ready; no features consumer or Worker) recorded; Phases 3–5 depend on green prior phases. US mapping: US3 completes Phase 2; US1 publisher proofs Phase 2, Core Phase 3, consumer Phase 4; US2 Core Phase 3, consumer Phase 4. Parallel guidance and delivery sequence replaced MVP/Incremental Delivery. All task IDs and test requirements preserved. These topology/routing/publisher/Core/consumer proofs are **planned Phase B proofs**, not executed results of this correction ask.

**I2 MEDIUM — plan project-type wording**: Replaced `plan.md` Project Type line with constitutional architecture terminology: "Existing .NET solution using ports and adapters with feature-organised Core. No new project." Architecture, PRODUCT, approved file envelope, and Q1–Q9 unchanged.

**Files edited this correction**: `specs/DEV-347/tasks.md`, `specs/DEV-347/plan.md`, `specs/DEV-347/DRAFTING_RECEIPT.md`. No Spec Kit, analyze, tests, gates, or commit in this ask.

---

## Plan challenge correction (2026-10-21)

**Basis**: Task note `DEV-347:74-76` Quill corrections after Keel recorded the Plan challenge editorial decision in `brief.md` (`DEV-347:77-78`; brief Plan challenge editorial decisions). Ledger F1–F4 dispositions from adjudication `DEV-347:72-73`. Original drafting HEAD `8919fdd1441081ab2d8b5cd699c7ad8b3194ac6c` and Method above remain authoritative; analyze-round-1 correction entry above is preserved. Editorial only — no product ruling. No tests, gates, analyze, commit, freeze, or Gate 1 claim in this ask. Topology/routing/publisher/Core/consumer proofs remain **planned Phase B proofs**, not executed results.

**F1 MEDIUM — stale current ticket locators**: Replaced mutable `DEV-347:<line>` ticket citations in `plan.md` (Scope/AC fidelity bullet) and `spec.md` (US1–US3 priority bases; Worker edge-case target) with semantic canvas note DEV-347 Ticket section/item/bullet locators cross-linked to brief section Ticket scope and acceptance criteria — verbatim. CONCLUSIONS, ASSUMPTIONS, and Q&A mirrors left unchanged; historical numeric note references inside those records remain historical.

**F2 LOW — hardcoded complexity thresholds**: Removed copied numeric implementation/refactor thresholds from `plan.md` Constitution Check and the literal `-Threshold 6` from `tasks.md` T016. Gate bar, stages, ownership, and verdict rules preserved; thresholds resolve from `harness.yml` at execution via supported script invocations; no invented flag.

**F3 LOW — fidelity claim visibility**: Amended `plan.md` Scope and prerequisite ticket-authority bullet to point to Q5/ASSUMPTIONS (`FullPlot`→`Synopsis`, whitespace-empty) and Q9/`spec.md:64-67,111-114` (exactly-one emission cardinality). Ticket AC wording and the no-new-scope (“Nothing is added or dropped”) statement preserved; these remain existing Patron rulings, not new ticket changes.

**F4 LOW — durable delivery scope wording**: Reworded `plan.md` Scale/Scope, `spec.md` FR-013, and `tasks.md` Notes so scope statements describe the delivery boundary and do not preclude findings about failures to meet in-scope requirements. Task IDs, FR/SC IDs, and semantics unchanged; no suppression of valid findings.

**Invariants preserved**: Phases P1 T001; P2 T002–T009 plus T012; P3 T010–T011; P4 T013–T014; P5 T015–T016. Dependencies, focused green checkpoints, sequential broker execution, one phase per Anvil ask/commit, Q1–Q9, assumptions, approved envelope, Epic 7 prerequisite, and user-owned merged Gate 1 unchanged.

**Files edited this correction**: `specs/DEV-347/plan.md`, `specs/DEV-347/spec.md`, `specs/DEV-347/tasks.md`, `specs/DEV-347/DRAFTING_RECEIPT.md`.

