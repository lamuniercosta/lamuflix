# DEV-553 drafting receipt (Quill)

**Date**: 2026-11-28
**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-553-spec`
**Branch**: `feature/553-spec` (verified)
**HEAD**: `6bd8be67` (unchanged; no commits)
**PYTHONUTF8**: 1

## Commands run

- `/speckit-specify` — wrote `specs/DEV-553/spec.md`; quality checklist `checklists/requirements.md`
- `/speckit-clarify` — non-interactive in one-ask Conductor run; grill Q1–Q10 recorded under `spec.md` Clarifications (no inventable gaps)
- `/speckit-plan` — wrote `specs/DEV-553/plan.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/subtitle-tracks.md` (+ mirrored plan to private Spec Kit IMPL_PLAN)
- `/speckit-tasks` — wrote `specs/DEV-553/tasks.md` (+ mirrored to private Spec Kit TASKS)
- **Repair round 1** (Keel Phase 2 step 3.3): applied numbered fix list 1–5 to Quill-owned drafts; refreshed private plan/tasks mirrors; refreshed this receipt
- **Repair round 2** (Keel Phase 2 step 3.3): applied narrow cross-artifact consistency list 1–3; refreshed private plan/tasks mirrors; refreshed this receipt
- **Phase 2 step 6 repair round 1** (Keel plan-challenge dispositions): applied numbered refinements 1–5 to Quill-owned drafts; refreshed private plan/tasks mirrors; refreshed this receipt
- Supporting: this receipt
- **Not run**: `/speckit-analyze` (Keel-owned — Quill applies fix lists only); `before_specify` grill (already closed at brief HEAD `6bd8be67`)

## Sources used (decisions vs facts)

- Decisions: `brief.md` Q1–Q10 and Phase 2 step 6 refinements, `CONCLUSIONS.md` (including plan-challenge dispositions), `ASSUMPTIONS.md` only — **no decision invention** in repair rounds
- Facts: Maestri notes `DEV-553`, `recon-DEV-553` (ticket text `:19–72`; persistence port finding `:74–82`, key evidence `:78–82`); `specs/PRODUCT.md`; constitution cite-only in plan; `task-pipeline` Phase 3 step 2 / step 5 for gate ownership
- Quill owns: `spec.md`, `plan.md`, `tasks.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/subtitle-tracks.md`, `checklists/requirements.md`, this receipt
- Keel owns: brief/conclusions/ADR (not edited); Palette owns `design.md` (not edited)
- Historic note (repair rounds 1–2 sources cited ticket text `:43–103` / persistence `:105–111` before recon line renumber; current authoritative ranges are `:19–72` / `:74–82`)

## Repair round 1 dispositions

1. **API/display boundary (HIGH)** — Applied. FR-008, track entity/data-model, contract, plan build order, T010/T018 (and related US3/web wording) now preserve discovery-derived wire `label`, project nullable `provider`/`releaseName` independently, and place `release name · provider` composition in the selector only when both are known.
2. **Dependency order (HIGH)** — Applied. Endpoint assertions moved into wire Phase 5 as T012 (authored before mappings) / T014 (pass after both mappings); OpenAPI→TS is T015 after endpoint proof; earlier phases keep handler/list/discovery proofs only. Task IDs, deps, checkpoints, plan, and quickstart updated together.
3. **Phase completion criteria (HIGH)** — Applied. T005 covers success-path tests only; T006 greens success; T007 authors+implements failure path (cancellation, no retry). Parallel section lists only true parallel pairs; discovery tests are byte-stable with no harness-noise exception.
4. **Visible proof gaps (MEDIUM)** — Applied. T016 covers first-appearance order, partial-provenance fallback, selectable manual track; T017 names absent-track handling as preserved regression; SMOKE-001–003 have an explicit downstream checkpoint/dependency; gate-only verification left to Gauge Phase 3 step 5; Phase 7 is glossary-only local Build close.
5. **Process language (MEDIUM)** — Applied. Read-only pickup/verification on Wisp/Keel; no claim that Gauge Phase 3 step 5 is hosted; exit 1/`Could not run` and exact-diff/rebased-head checks explicit downstream; Gate 1 pending at Spec Kit draft time; specs-only Spec Kit PR. Brief/CONCLUSIONS/ASSUMPTIONS/ADR/implementation not edited.

## Repair round 2 dispositions

1. **Core track projection + wire asserts** — Applied. T005/T006 introduce and prove nullable provider/release on returned Core `SubtitleTrack` (retain discovery label + identity) alongside persistence; T010 consumes that shape; T013 maps both; T012 asserts known-provenance responses preserve wire `label` and unknown fields serialize as JSON `null` independently. Proof order unchanged.
2. **Q9 / ranking consistency** — Applied. T007 immediate-outcome only; Q9 last-committed-row named in T009/T010. Data-model replaces “No row change guaranteed” with Q9 old-row/new-row wording and immediate fallback either way. Numbered ranking places missing scores after known scores within the same hash rank before UTC (FR-010). Plan/quickstart aligned.
3. **Smoke + Phase 7 Gate 1 language** — Applied. Tasks graph, downstream checkpoints, plan, and quickstart all place SMOKE-001–003 after the applicable gate set on the rebased delivery head; conditional/conflicting smoke language removed; no local Build smoke task. Phase 7 checkpoint no longer implies Gate 1 could still be pending at local Build completion (T000 already blocks Build).

## Phase 2 step 6 repair round 1 dispositions

1. **Discovery filename key invariant (Sentry F1/F2)** — Applied. Spec/plan/data-model carry the canonical discovery relative-path key (separator mapping at publication, preserve segments/casing/Unicode, adjacent vs `Subs\` distinct, same key for row/join/token/download ID). T002 proves canonical key, adjacent vs `Subs\`, and concurrent same-key upserts from separate contexts with one complete row. T012 proves Subs download/list ID and provenance round-trip.
2. **Null score + discovery-derived language (Sentry F4/F5)** — Applied. FR-002/data-model/T005–T006 carry null-score successful writes. Spec/plan/data-model/contract make discovery-derived language authoritative for partitions and wire; candidate language is provenance. T009 covers conflicting stored language; T016 groups by wire `language`.
3. **ADR 0054 Build-only (Ledger 1–3, Compass 2)** — Applied. T000 explicitly excludes the ADR file. T003 accepts ADR 0054 atomically with the port, Principle I next MINOR from then-current base, and whitelist (preserve intervening entries). ADR locks only `ISubtitleDownloadRecordStore`; Build names EF/table types by Persistence conventions. T001 and delivery rebase revalidate ADR claim / constitution Version / Last Amended / whitelist; collision → Bernstein. Proposed draft recorded as parked in `PRIVATE_TICKET_DIR` with brief/conclusions durable fallback.
4. **T015 schema/TS nullability (Ledger 5)** — Applied. After T012/T014, T015 checks OpenAPI string-or-null and TypeScript `string | null` for both fields and retains drift. Contract/quickstart aligned.
5. **Stale recon / ADR naming citations** — Applied. Quill-owned artifacts now cite ticket `:19–72` and persistence `:74–82` (key evidence `:78–82`). Stale ADR Accepted-in-spec / table-name-locked-by-ADR wording replaced. Historic receipt entries above preserved as history. Task IDs/dependencies, scope, and gate states preserved. Brief/CONCLUSIONS/ASSUMPTIONS/`design.md`/ADR not edited.

## needs decision

- none

## Notes

- `before_specify` hook (`grill-with-docs`) already satisfied by closed grill artifacts; not re-run.
- Prerequisite-resolved private mirrors: Spec Kit resolves IMPL_PLAN/TASKS from the private ticket directory. Paths: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-553\plan.md` and `...\tasks.md`. Quill keeps canonical `specs/DEV-553/plan.md` and `specs/DEV-553/tasks.md` authoritative and mirrors both so private and worktree copies stay byte-identical.
- ADR 0054 locks only `ISubtitleDownloadRecordStore`; Build names table/EF types by Persistence conventions. Proposed draft is in `PRIVATE_TICKET_DIR`; Spec Kit PR excludes the ADR file.
- No structural owner checkbox. Gate 1 remains **pending** until Spec Kit PR merge (T000); once Build starts, Gate 1 is already satisfied. Spec Kit PR stays `specs/**` only; ADR 0054 Accepted + Principle I MINOR + ArchitectureTests whitelist ship atomically in the Build PR with the port.
- `CLAUDE.md` Spec Kit marker left as the standing generic plan pointer (matches prior Quill drafting precedent).
- Implementation remains gated on DEV-551 delivery merge + pickup evidence (Q8).
- No commits in repair rounds 1–2 or Phase 2 step 6 repair round 1.

## Files written / updated (Phase 2 step 6 repair round 1)

- `specs/DEV-553/spec.md`
- `specs/DEV-553/plan.md` (+ private mirror)
- `specs/DEV-553/tasks.md` (+ private mirror)
- `specs/DEV-553/data-model.md`
- `specs/DEV-553/research.md`
- `specs/DEV-553/quickstart.md`
- `specs/DEV-553/contracts/subtitle-tracks.md`
- `specs/DEV-553/checklists/requirements.md`
- `specs/DEV-553/DRAFTING_RECEIPT.md`

## Final matching hashes (canonical = private)

- plan.md: `BE721FFD14FC3245474C7A13640E883291147FE7C25325936C6153B5B0EC4913`
- tasks.md: `D5578775D3CBFBD6DA414DE2780D1D0D61705D0A3075F02107C6A9840473076C`
