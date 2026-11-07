# DEV-456 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-07
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-456-spec` · branch `feature/456-spec`
**Method**: Spec Kit `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` from the closed grill, then Quill analyze-fix rounds 1–2 (historical), then plan-challenge repair round 1 per `PLAN_CHALLENGE_FIXES.md` after brief/CONCLUSIONS Q11. Grill already closed (`brief.md` Q9; Q10 later; Q11 plan-challenge). `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge / freeze / reanalysis). `.specify/feature.json` already pointed at `specs/DEV-456` and was left unchanged.

**Inputs**: grill-closed `specs/DEV-456/brief.md` (Q1–Q11), `CONCLUSIONS.md` (Q1–Q11), `ASSUMPTIONS.md` (none), Maestri notes `DEV-456`, `recon-DEV-456`, `recon-DEV-456-2`, `recon-DEV-456-3`, `analysis-DEV-456-Keel`, `PLAN_CHALLENGE_FIXES.md`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (cite only); Keel-authored `docs/adr/0037-library-file-reader-port.md` (exists; not edited by Quill).

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-456/spec.md`
- `specs/DEV-456/plan.md`
- `specs/DEV-456/tasks.md`
- `specs/DEV-456/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADRs (including existing `docs/adr/0037-library-file-reader-port.md` — Keel-authored; Quill does not reauthor; ADR M1 remains Keel), constitution, production code, tests, `harness.yml`, generated OpenAPI/TS/MSW, `CONTEXT.md`, `PLAN_CHALLENGE_FIXES.md`.

**Owner checkbox**: none — brief Q9–Q11 record no ticket contradiction and no principle waiver / no ticket-deliverable change. Owner Gate 1 remains pending. Quill does not invent a structural checkbox.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS and recon facts + Patron Q11. Exact Infrastructure DI registration file left unnamed (Phase B Wisp per Q7–Q9). ADR path consumed as Bernstein-assigned, Keel-authored `docs/adr/0037-library-file-reader-port.md` (already exists; remains Proposed until governance/extension). Whitelist **plan** consumed from `recon-DEV-456-3` §3 (append-only `|ILibraryFileReader` before `)$` on `ArchitectureTests.cs:31`, preserve 16 entries/order); whitelist **code** lands atomically with port at T003 only. Freeze / analyze-clean / plan-challenge freeze left to Keel.

**Encoding notes (not new decisions)**:

- No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; route/statuses/headers live in spec + generation tasks.
- Q8 dependency order amended by Q10/Q11: Wisp facts → Keel analyze → T001c verify-only ADR/governance/whitelist plan → T001d Keel constitution edit → T003 atomic buildable port/exception/whitelist type slice → T002 behavioral real-disk reds → T004 reader → T005 DI → endpoint (incl. T006a/T006b/T006c) → contract → Compose → gates.
- Exact new test filenames under existing test project folders deferred to Build (Q7).
- No CONTEXT.md glossary rewrite task (Q9). Feature vocabulary recorded only in spec Domain vocabulary.
- No nginx/Compose config change task (Q6: further Patron ruling only after concrete failed proxy evidence).
- Local `tasks.md` Phase N IDs are for `/implement Phase N` dispatch and are not canonical task-pipeline phases.
- Constitution omission of `IEnrichmentOutbox` / `IRecommendationProfileSource` (`recon-DEV-456-3` §3.4) is recorded as recon fact only — not authorization to broaden this scoped extension.
- Does **not** claim Gate 1, runtime acceptance, PLAN FROZEN, analyze CLEAN, Principle I implementation PASS, or merge clearance.
- Sentry F4 accepted residual and Ledger L4 (0036 on feature/458-spec per recon-3 §2) require no scope/code task. Keel separately repairs ADR M1.

---

## needs decision

- none open for Quill. Prior Principle I / no-ADR `needs decision:` is **superseded** by Patron Q10 and authored ADR **0037**. Q11 plan-challenge rulings are recorded in brief/CONCLUSIONS. Remaining compliance work is Keel T001c/T001d + Build atomic port/whitelist **code** execution — not a Quill product decision and not an ADR-authorship task.

---

## needs recon

- Exact Infrastructure DI registration file remains the brief-delegated Phase B Wisp fact (Q7–Q9), not a product decision Quill may fill. Build-time pickup recon (T001) refreshes landed-main facts without inventing rulings; STOP on missing facts. Do not infer DI file.
- Architecture whitelist location/pattern/entries for this Phase A repair are supplied by `recon-DEV-456-3` §3; Phase B T001 refreshes only if `ArchitectureTests.cs:30-31` drifted. Code insertion remains T003-only.

---

## Analyze-fix round 1 (Quill) — historical

1. **C1**: Propagated Q10 across spec/plan/tasks/checklist/receipt. Superseded stale no-ADR / open-decision / sole-DI-prerequisite text. Retained `ILibraryFileReader`. Modeled `docs/adr/0037-library-file-reader-port.md`, scoped constitution enumeration/governance for `ILibraryFileReader` only, and append-only ArchitectureTests whitelist (16 entries preserved; atomic with port). No principle waiver, deliverable change, structural checkbox, or premature C1/Principle I PASS claimed.
2. **U1**: Added explicit focused regression tasks T006a (MIME mapping/fallback/no attachment or validators) and T006b (folder-auth-before-resolve with denied-folder preventing invocation; file-auth-before-open; single authorized open). Preserved framework range machinery and real-disk proofs (T002/T006).
3. **I1**: Split pickup — T001 Wisp facts only; T001b Keel analyze / authorized task-drift adjudication blocks feature implementation until complete; T001c ADR/constitution prerequisites before T003.

---

## Analyze-fix round 2 (Quill) — historical

1. **I2**: Fixed Phase 1 checkpoint and related sequencing so T003 is blocked on verified ADR 0037 + scoped constitution/governance + exact evidenced whitelist **plan**; ArchitectureTests whitelist **code** insertion lands atomically with T003 only (no pre-T003 whitelist code requirement).
2. **S1**: Updated status wording across Quill-owned artifacts to acknowledge ADR 0037 exists and the original C1 planning contradiction is repaired via Q10/ADR, while constitution/governance and future atomic port/whitelist execution remain mandatory and are not Principle I implementation PASS. T001c verifies existing ADR at pickup rather than reauthoring it. No blanket compliance claim; code tasks remain unchecked; behaviour/scope/other decisions preserved.

---

## Plan-challenge repair round 1 (Quill) — dispositions applied

Applied single numbered list from `PLAN_CHALLENGE_FIXES.md` after brief/CONCLUSIONS Q11. Ownership limited to five Quill artifacts. No commits, PR, gates, production/test/constitution/ADR edits.

### Finding → file:line map

| Fix # | Findings | Edited ranges (post-repair) |
|---|---|---|
| 1 | Ledger M3 + Sentry F1/F2 | `spec.md:24-46,99-114,192,196,200,206-210` (port signatures, `LibraryFileAccessDeniedException`, NotFound/404, OS/IO→500, async `FileShare.Read` + Asynchronous\|SequentialScan, concurrent-reader proof); `plan.md:11,31,35,55-57,83-84,156-158`; `tasks.md:59-61,71,75` (T003/T002/T004/T006/T007) |
| 2 | Sentry F3 | `spec.md:26-33,194,217-218` (route-local nosniff on 200/206 only); `plan.md:11,40,58,140`; `tasks.md:68-71,75,78` (T006/T007/T010) |
| 3 | Compass F1 | `plan.md:36` (Principle VI **OPEN — pending Build requirement**, not N/A); `spec.md:46,123,196`; `tasks.md:74,75,78` (new T006c + T007/T010 coverage). No claim that existing GlobalExceptionHandler supplies movie-id structuring; no new telemetry names / framework types in Core |
| 4 | Ledger M2 + Compass F2 | `tasks.md:19-20,48-49,115-116,147` (T001c Keel verify-only; new T001d Keel constitution-edit deliverable before T003; ADR remains Proposed until governance/extension; MINOR 1.11.0 if pickup still 1.10.0; no DEV-467 backfill; clear STOP, no premature PASS); `spec.md:10,48,183,200-201`; `plan.md:31,52-53,154-155` |
| 5 | Ledger M4 + Compass F3/F4 | `tasks.md:16-27,59-60,115-130,147-151` (removed `[P]` from T002 and T006/a/b; removed T002∥T001c allowance; order T001c/d → T003 → T002 → T004 → T005; buildable-red / missing-route rules); `spec.md:201` (FR-012); `plan.md:55-58,148-160` |
| 6 | Ledger L1/L2 | `tasks.md:60` (actual link/reparse fixture; Windows fail-loud, never false PASS/SKIPPED); `tasks.md:75` (T007 MIME provider injected/per-instance read-only, never mutable `static readonly` mapping instance); `spec.md:103,192,195`; `plan.md:18,39,140` |
| 7 | Compass F5 + Ledger L3 | `spec.md:5,46,178,196`; `plan.md:5,35,178-186`; `tasks.md:4,71`; `checklists/requirements.md:33-34,43` — 422 inherited-only; trimmed stale analyze-fix round-2/final-under-cap banners; status = repaired draft pending Keel plan-challenge freeze; ordinary Gate 1 pending; refer to Q10/Q11 decision record; no analyze-clean/frozen/runtime PASS claim |
| 8 | Receipt | this section + counts/trace below. Sentry F4 + Ledger L4 explicitly no-code. ADR M1 left to Keel |

### Task count (post-repair)

**28** checklist tasks: T000, T001, T001b, T001c, T001d, T003, T002, T004, T005, T006, T006a, T006b, T006c, T007, T008, T009, T010, T011, T012, T013, T014, T015, T016, T017, T018, T019, T020, T021.

Added vs prior draft: **T001d** (Keel constitution edit), **T006c** (Principle VI structured movie-id errors). No `[P]` on T002 / T006 / T006a / T006b / T006c.

### FR / SC trace (post-repair)

- **13 FR** retained (`spec.md` FR-001…FR-013): Q11 content folded into FR-003 (port/exception/open/link/concurrent), FR-005 (nosniff), FR-006 (injected MIME provider hygiene), FR-007 (policy-exception→403 only + Principle VI + inherited-only 422), FR-011 (exception file + T001d surfaces), FR-012 (T001c/d → T003 → T002 → T004 → T005 order).
- **5 SC** retained (`spec.md` SC-001…SC-005): SC-001/SC-002 carry nosniff; SC-004 carries policy/NotFound/operational distinctions.

### Residuals explicitly out of Quill repair scope

- Sentry F4 accepted personal-scale residual — no throttle/scope/code task.
- Ledger L4 — 0036 occupied on feature/458-spec per recon-DEV-456-3 §2; no numbering repair/search.
- ADR M1 — Keel repairs separately.

---

## Clarify

Coverage Clear against brief Q1–Q11 + recon-DEV-456-3 + authored ADR 0037 + `PLAN_CHALLENGE_FIXES.md`. No `[NEEDS CLARIFICATION]` markers in spec.md. No owner Gate 1 checkbox from grill / Q10–Q11.

## Status

Phase A Spec Kit drafting + historical analyze-fix rounds 1–2 preserved above. Plan-challenge repair round 1 applied on Quill-owned artifacts. Quill does **not** claim PLAN FROZEN, CLEAN, Gate 1 approval, Principle I implementation PASS, analyze-clean, runtime PASS, or implementation clearance — pending Keel independent plan-challenge freeze. Ordinary Gate 1 remains pending. No production gates, tests, commits, or PR run by Quill.
