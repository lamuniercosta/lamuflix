# DEV-503 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-12
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-503-spec` · branch `feature/503-spec`
**Method**: Spec Kit `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` from the closed grill in one ask. Grill already closed (`brief.md` Q1–Q6; Q7 Phase 2 drafting clarification appended; Q8 Phase 2 challenge correction appended). `.specify/feature.json` already pointed at `specs/DEV-503` and was left unchanged. `$env:PYTHONUTF8=1` set for the drafting shell. `before_specify` grill hook treated as already satisfied by closed grill.

**Inputs**: grill-closed `specs/DEV-503/brief.md` (Q1–Q8; Q8 supersedes conflicting Q1–Q7 interaction/design/file-boundary wording), rulings in `CONCLUSIONS.md` / `ASSUMPTIONS.md`, Maestri notes `DEV-503` and `recon-DEV-503` (including source facts 186–214), `specs/PRODUCT.md`, settled Palette `specs/DEV-503/design.md` (Palette-owned; present for plan challenge), and Keel `PLAN_ADJUDICATION.md` consolidated repair list. Constitution cited only for plan gates; Quill did not edit constitution.

**Artifacts written (Quill-owned)**:

- `specs/DEV-503/spec.md`
- `specs/DEV-503/plan.md`
- `specs/DEV-503/tasks.md`
- `specs/DEV-503/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md` (Keel), `design.md` (Palette owns), constitution, ADRs, production/test code, `harness.yml`, generated OpenAPI/TS, `CONTEXT.md`.

**Owner checkbox**: none (`brief.md` Status; Q1–Q8). No new ADR. Ordinary owner Gate 1 remains pending after Bernstein coordination / Rigger preparation-opening of the spec PR and user merge. Quill does not invent PLAN FROZEN.

**Decisions invented by Quill**: none. Behaviour from brief/CONCLUSIONS/ASSUMPTIONS; recon/PRODUCT facts (including 186–214); visual binding from Palette `design.md`; challenge repairs only as numbered in `PLAN_ADJUDICATION.md`. Undocumented media-chrome prop/export names not asserted. Optional panel source filenames taken from brief Q7 only. Q8 file-boundary addition of `playerCopy.ts` / `subtitleCopy.ts` applied from Patron ruling.

## needs decision (to Keel)

None open for inventable behaviour. Prior item 1 (optional co-located CC panel filename) settled by brief Q7 / CONCLUSIONS.md / ASSUMPTIONS.md: use `web/src/features/playback/SubtitlePanel.tsx` and `web/src/features/playback/subtitlePanel.test.tsx` only if accessibility or complexity warrants extraction; otherwise keep the panel in the approved existing files; extraction is not mandatory; no invented line-count threshold (Q8). Unverified media-chrome 4.19.4 installed exports remain a documented T001 STOP prerequisite (`recon-DEV-503` 212–214; Q8 point 8), not a Quill drafting decision.

## Analyze fix round 1 (Quill)

Applied Keel numbered list (MEDIUM U1, MEDIUM C1) to Quill-owned artifacts only. No freeze or Gate 1 claim.

## Plan-challenge round 1 repairs (Quill) — 2026-11-12

Authority: `PLAN_ADJUDICATION.md` Consolidated repair list 1–9; Patron Q8 approved and appended to brief/CONCLUSIONS; taste in ASSUMPTIONS. Q8 matched before historical Q4 conflicts. No API facts invented. Palette independently repairs `design.md`. PLAN FROZEN withheld until Keel independent validation. No production edits, commits, or PR opening by Quill in this round.

| # | Repair | Applied to |
|---|---|---|
| 1 | Source authority (recon 186–214; manifest vs unverified exports; T001 inventory/STOP; no guessed menu APIs; no unsupported lineage) | `spec.md` Input/Scope/Assumptions/Edge Cases/FR-007/FR-014; `plan.md` Technical Context/Approach/Test Strategy; `tasks.md` T001/T015; this receipt; checklist Notes |
| 2 | Boundaries (Q8 `playerCopy.ts`/`subtitleCopy.ts`; explicit T013 file list; read-only hooks/helpers/types; no explanatory comments beyond AGENTS; optional Q7 only; no AbortController rewrite) | `spec.md` Scope 13/FR-015; `plan.md` Structure/Conventions; `tasks.md` Conventions/T001/T003/T007/T010/T013 |
| 3 | Accessibility (non-modal labelled region; radiogroup/radio/aria-checked; tablist roving/Home/End/auto-activate; native speed verified not rewrapped; polite status vs alert) | `spec.md` Scope/US2/FR-004/FR-005/FR-019; `plan.md` Design binding/Test Strategy; `tasks.md` T005/T006/T007/T012 |
| 4 | Event coordination (noHotkeys + bubble/`defaultPrevented`; composedPath boundary; one local coordinator; T012 each shortcut; T013 effect cleanup; no capture-phase mandate) | `spec.md` Scope 9–11/FR-009–FR-012; `plan.md` Approach/Constraints; `tasks.md` T012/T013 |
| 5 | Escape/lifecycle/subtitles (page-delivered Escape vs browser-owned; fullscreenchange fallback; movie-change/unmount focus rules; T005/T012 stale regressions; T016 Escape record) | `spec.md` Scope 11/US2/US4/FR-011/FR-013; `plan.md` Test Strategy; `tasks.md` T005/T012/T013/T016 |
| 6 | Responsive/visual evidence (Q8 copy/[assumed]; wrap/seek-row/scrollable overlay; no wireframe render; RTL ≠ CSS proof; T016 alone for appearance/320px/motion) | `spec.md` Scope 2/14–15/FR-002/FR-016–FR-018; `plan.md` Test Strategy outcomes; `tasks.md` T003/T007/T012/T016 |
| 7 | Rate state (live media rate on ratechange/source/movie/metadata; no forced 1x; T001 label STOP; seven rates + Q8 copy) | `spec.md` Scope 7/US3/FR-007; `plan.md` Constraints; `tasks.md` T001/T009/T010/T012 |
| 8 | Cleanup/runtime precision (T002 one in-bar play; T003 remove-if-present at T001 owner + real fullscreen + dead select CSS; T016 track→Off windowed+native fullscreen) | `spec.md` Scope 8/US1/FR-008; `tasks.md` T002/T003/T016 |
| 9 | Gate provenance/status (Bernstein/Rigger spec-PR producer before T000; no Build before Gate 1; link adjudication; PLAN FROZEN withheld; no freeze/Gate 1/runtime/merge claim) | `spec.md` Status/Owner question; `plan.md` Status/Approach; `tasks.md` Phase 0/T000; checklist Notes; this receipt |

**Residual precision (same round, repairs 4–5; Keel reread note)**: `spec.md` US4 acceptance scenario 2 — consumption-gated hotkey isolation (unconsumed keys retain app behaviour; no blanket suppress-on-focus). `tasks.md` T013 — explicitly implements page-delivered Escape-before-app-exit, `fullscreenchange`-exited closure/focus fallback, and movie-change conditional focus (not tests-only). No new decision or scope.

## Notes

- No `research.md`, `data-model.md`, or `contracts/` — UI-only; decisions frozen in brief; no public API delta (`plan.md`).
- `after_tasks` / plan-challenge validation is Keel's next step; Quill does not invent freeze.
- Chromium fixture/reference availability was not established by recon (`design.md` also notes reference assets); tasks T015–T016 encode Could not run rather than inventing paths; availability stated as unverified until obtained.
- `design.md` component-tree names (CcTrigger, CcPanel, SpeedMenu) treated as design vocabulary; tasks bind labels/a11y/presentation without forcing those as source filenames (Q7 optional extraction uses `SubtitlePanel` naming only when warranted).
