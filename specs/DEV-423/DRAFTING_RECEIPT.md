# DEV-423 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-26
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-423-spec` · branch `feature/423-spec`
**Method**: `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-checklist` in one ask. `/speckit-analyze` not run by Quill (Conductor instruction; Keel owns read-only analyze). `.specify/feature.json` already pointed at `specs/DEV-423` and was left unchanged.

**Inputs**: frozen `specs/DEV-423/brief.md` (Q1–Q7 + PC1–PC6 appendix), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, Maestri notes `DEV-423`, `recon-DEV-423`, `recon-DEV-423-2`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates only; no edits). Round 1 challenger notes: `findings-DEV-423-Sentry`, `findings-DEV-423-Ledger`, `findings-DEV-423-Compass`.

**Artifacts written / repaired**:

- `specs/DEV-423/spec.md`
- `specs/DEV-423/plan.md`
- `specs/DEV-423/tasks.md`
- `specs/DEV-423/checklists/requirements.md`
- this receipt

**Owner checkbox**: none for DEV-423. Upstream DEV-331 Q7 constitution departure is inherited as an explicit Phase B blocker (T000/T001); no duplicate checkbox and no silent clearance (Q1). Preserved through plan-challenge repairs.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ticket and Patron PC1–PC6. Quill did not edit `brief.md`, `CONCLUSIONS.md`, or adjudication.

**needs decision:** none.

**Clarify**: coverage Clear; no `[NEEDS CLARIFICATION]` markers; Clarifications section recorded in `spec.md`.

**Analyze fix round 1** (Keel numbered list; draft artifacts only; no decision/production/upstream change):

1. HIGH I1 — `plan.md` Health/resilience row and `tasks.md` T012: split eligible primary failure → secondary Found (preserve primary degraded snapshot) from primary 401 → InvalidResponse with zero secondary calls and primary Unhealthy advice visible; fallback rules unchanged.
2. MEDIUM C1 — `tasks.md` T002/T004: add explicit null/empty provider-list rejection unit guard work in `MetadataProviderChainTests.cs` mapped to FR-001/FR-006.
3. MEDIUM I2 — `tasks.md`: remove MUST-fail / must-fail-until sequencing; TDD remains optional; tests may precede implementation; required coverage green before verification. File dependencies and coverage kept.

**Plan-challenge Round 1 repairs** (Keel numbered list; verification of repairs only; no new challenger round; PC1–PC6 already in brief/CONCLUSIONS before edits):

1. **S-01** — All operative unit paths → `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs`, namespace `LamuFlix.UnitTests.Adapters`, class unchanged (FR-006, plan Project Structure, T002/T004).
2. **S-02 / S-03 / L5** — Eligibility = exact record equality to `RateLimited`/`ProviderUnavailable`; future categories terminal; no mutable static initialisation; null → `ArgumentNullException`, empty → `ArgumentException`; snapshot `ToArray` + caller-list-mutation unit coverage; matrix categories derived from type (current 7×7, expands); no raw-capture primary constructor incompatible with guards (FR-001/FR-002/FR-006, plan Approach/Test Strategy, T002/T003/T004).
3. **F1 / S-05 / L4** — Scope/FR-004/FR-005/FR-008/Approach/T001/T005: OMDb port-typed registration replaced with concrete; TMDb typed client/BaseUrl normalization/resilience/secondary health retained verbatim; preserve private shared `Configure`, `MetadataProviderResilienceOptions`, unrelated comments; factory entries `GetRequiredKeyedService` only; no TMDb re-authorship or duplicate-entry guard.
4. **F2** — FR-008/file sets/T007/T008: only type or behaviour assertions pinning unkeyed port to bare OMDb in the two composition files; preserve unrelated health/telemetry/resilience assertions; broader drift needs cited ruling.
5. **F3** — T009 keyed resolution under both Enabled modes tested separately from T010 disabled-chain zero secondary resolve/invoke/HTTP.
6. **F4 / F5** — Scope citation FR-008 (was FR-010); remove T002 `[P]` and dependent examples; optional draft before T003, compiles with implementation.
7. **M1 / L1** — Explicit later-terminal → dead-letter accepted cost in Edge Cases and plan Risks; Unknown terminal in chain but retryable downstream; L2/L3/L6/L4/S-04 dispositions stay as ruled with no new work. Receipt/checklist/traceability aligned; T000/T001 Phase B gating preserved.

**Status**: plan-challenge repaired; ready for Keel freeze / Gate 1 review. Gate 1 not approved. Quill does not claim plan freeze. No production gates run.

**Verification (self)**: grepped owned artifacts for former root unit path, Scope FR-010 citation, T002 `[P]`, raw primary-ctor Approach, and ambiguous “registers … TMDb” authorship; confirmed PC path/namespace, eligibility/guards/snapshot language, OMDb-authored/TMDb-retained boundary, `GetRequiredKeyedService`, bare-OMDb assertion narrowness, T009/T010 split, and M1 Edge Cases/Risks present. Did not edit brief/CONCLUSIONS/adjudication/production.

**Next**: Keel verifies returned artifacts and freezes the plan (or returns a numbered repair list). Gate 1 remains owner/Keel.
