# DEV-500 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-12
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-500-spec` · branch `feature/500-spec`
**Method**: Spec Kit `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` from the closed grill in one ask, then plan-adjudication repair from `PLAN_ADJUDICATION.md` Quill 8-item list. Grill already closed (`brief.md` Q1–Q10). Phase 2 step 6 amendment + Patron seven-item ruling + ASSUMPTIONS amendments read before repair. `.specify/feature.json` already pointed at `specs/DEV-500` and was left unchanged. `$env:PYTHONUTF8=1` set for the drafting shell. `before_specify` grill hook treated as already satisfied by closed grill. Pre-existing `harness.yml` modification left untouched.

**Inputs**: grill-closed `specs/DEV-500/brief.md` (Q1–Q10 + Phase 2 step 6 amendment), rulings in `CONCLUSIONS.md` / `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md`, Maestri notes `DEV-500` and `recon-DEV-500` (current `:7-16`; historical citations qualified), `specs/PRODUCT.md`. Constitution cited only for plan gates; Quill did not edit constitution. Palette `design.md` delivered with corrections pending — Quill did not edit design.

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-500/spec.md` — repaired
- `specs/DEV-500/plan.md` — repaired
- `specs/DEV-500/tasks.md` — repaired (US4 seam split; IDs T000–T027)
- `specs/DEV-500/checklists/requirements.md` — repaired
- this receipt — repaired

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md` (Keel), `design.md` (Palette), constitution, ADRs, production/test code, `harness.yml`, generated OpenAPI/TS, `CONTEXT.md`.

**Owner checkbox**: none (`brief.md` Authority; Q1–Q10; plan-challenge ruling). No new ADR. Ordinary owner Gate 1 remains pending after Bernstein coordination / Rigger preparation-opening of the spec PR and user merge. Quill does not invent PLAN FROZEN; freeze awaits independent Keel reread.

**Decisions invented by Quill**: none. Behaviour from brief/CONCLUSIONS/ASSUMPTIONS/plan-challenge amendment; recon/PRODUCT facts with historical-citation qualification; Q8 file names (plus approved `subtitleTestDoubles.ts`) used as planned responsibilities only. Screenshot fidelity left unverified. Backend identity stability left unproven. DEV-503 pickup left as implementation gate. Design marked delivered with corrections pending (not absent).

## Plan adjudication repair map (Quill 8-item list)

1. Propagated seven ratified decisions into FRs / approach / file boundary / tests; qualified historical recon + unproven backend stability; preserved reload persistence, fidelity, Chromium/Firefox, Gate 1/dependency prerequisites.
2. Specified JSON-array pair identity + adversarial/normalization/reorder/reload tests; named `subtitleTiming.test.ts` and explicit `subtitleTiming.ts` green owner (T012) for shifted intervals.
3. Added approved test-only `subtitleTestDoubles.ts`; deterministic media/cue readiness, fake timers/stubbed rAF; generation/identity + deferred out-of-order tests; preserved abort/blob ownership; rapid switch / StrictMode / acquired-resource cleanup coverage.
4. Added `hidden` mode, half-open intervals, copied immutable baseline, boundary/long-sequence/large-delay tests, behavioural paused/unchanged-cue frame tests; native-suppression proof retained in browsers.
5. Added literal markup/attack vectors, explicit input/select/range/dialog focus guards, lazy storage accessor-getter throws; immediate best-effort persistence; explicitly excluded mandatory debounce, cross-tab listener, runtime freeze, internal state spy, CSS prescriptions.
6. Split US4 into preference lifecycle (T014–T015), presentational controls (T016–T017), shell/shortcuts (T018–T019), loaded/hidden track (T020–T021); pinned single hook + props-only; same-file serial; deduplicated regression as T022; renumbered gates/receipts/review/delivery to T023–T027 with After-deps order.
7. Marked Palette design delivered with pending corrections; gates-before-receipts explicit (T023 before T024–T025); receipt-driven rerun/refresh required; browser acceptance includes scale/motion/target/contrast observations.
8. Refreshed this receipt and checklist traceability; no PLAN FROZEN or completed-gates claim.

## needs decision (to Keel)

None open for inventable behaviour from this repair. Outstanding stage prerequisites already ruled (not Quill decisions): Palette design corrections (pending); independent Keel reread before PLAN FROZEN; DEV-503 merge/pickup refresh before implementation; reference screenshot inspection before fidelity claims; Chromium and Firefox real-library receipts; backend identity stability remains unproven evidence (never convert to proven assumption).

## Notes

- No `research.md`, `data-model.md`, or `contracts/` — UI-only; decisions frozen in brief; no public API delta (`plan.md`).
- No production or harness edits performed.
- Independent Keel reread required before PLAN FROZEN; Quill does not invent freeze.
