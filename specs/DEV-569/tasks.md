---
description: "Task list for DEV-569 web player volume boost above 100%"
---

# Tasks: Web player volume boost above 100%

**Input**: Design documents from `specs/DEV-569/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7 + Plan challenge decisions), `CONCLUSIONS.md` (Plan challenge ruling and Keel dispositions), `ASSUMPTIONS.md`; Maestri notes `DEV-569`, `recon-DEV-569`; `specs/PRODUCT.md`.

**Status**: Draft — Quill Spec Kit tasks + plan-challenge repair round 1 applied. Gate 1 / T000 remains **pending**; zero structural owner checkboxes. PLAN FROZEN authority is the future Keel freeze receipt in `CONCLUSIONS.md` only — this draft does not declare freeze. No Build work before verified Gate 1 merge receipt. No implementation/task-completion/freeze claims in this draft. Keep T000–T016, dependency order, Phase 2 safeguards-before-bind, five-file envelope, and gates.

**Tests**: Required (`brief.md` Test strategy / Q6 / Plan challenge decisions; FR-001–FR-012; SC-001–SC-006; SMOKE-001–003). Follow `web-implement` test-first (Vitest + RTL red→green) per plan and the web lane: author failing cases before implementation in each story phase. Use existing web test home `BrowserPlayer.test.tsx` with controllable AudioContext doubles. Chromium receipts use temporary Test URLs portal session-only evaluation — jsdom is not the receipt driver. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/review tasks are never `[P]`. Shared `BrowserPlayer.test.tsx` / shared source files are never claimed parallel across phases.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Approach (`BrowserPlayer.tsx`, `useBrowserPlayer.ts`, `BrowserPlayer.test.tsx`, and `playerCopy.ts` / `BrowserPlayer.css` only as needed); STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new dependencies or sibling modules; no LocalPlay execution edits; no production measurement hooks/exports/globals; never lower harness thresholds. Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, gates, review, and evidence per `task-pipeline`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` / `web-implement` dispatch. They are **not** canonical `task-pipeline` phases. Under task-pipeline Phase 3 step 2, Anvil receives **one local phase per ask, in order** — phases are sequential; do not claim cross-phase parallelism on shared files.

**Spec-PR provenance**: After plan challenge clearance and Patron `gate1: provisional` (if applicable), Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup + delivery-invariant AND portal capability proofs; Keel reanalysis) — blocks production edits
Phase 2   T002 ∥ T003 → T004 → T005 → T006   (US1 boost + US3 eligibility in same phase; eligibility before/with first bind) — MVP
Phase 3   T007 → T009   (US2 persistence + pending activation) — after Phase 2 checkpoint pass
Phase 4   T010 → T011   (US2+US3 interaction + re-run regressions; sequential after Phase 3; pass required before Phase 5)
Phase 5   T012 → T013   (US4 calibrated + real-library Chromium evidence via Test URLs portal)
Phase 6   T014 → T016   (web gates + honest C# SKIPPED/N/A + Size M review; delivery checkpoint needs T014 gates, T015 smoke pass, T016 review closure)
```

Ordering per brief Task order / Q3–Q7 / Plan challenge decisions and prior analyze/plan-challenge repair rounds. No dependency cycle. Live Build requires **both** delivery-invariant/Chromium corroboration **and** portal measurement capability (including independent unbound native reference) to pass in Phase 1 before T002+; any missing proof or recorded blocker stops Build — do not claim them from planning. Phases 3 and 4 are sequential (shared `BrowserPlayer.test.tsx` / source files); never dispatch them in parallel. Phase 2 dependency everywhere: after T001, **T002 and T003 may run in parallel**; then **T004 → T005 → T006** with T005 waiting for both T003 and T004. Phase 2 owns failing-first coverage and the pass bar for failure stages and same-media continuity; Phase 4 re-runs those after persistence without reauthoring.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-569 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-569 in `specs/DEV-569/CONCLUSIONS.md`. Receipt only. T001–T016 MUST NOT proceed without verified merge (`brief.md` Task order; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon, delivery invariant, and portal measurement capability (blocks production edits)

**Purpose**: Confirm authorized seams; pass **both** origin/delivery-invariant proof and portal capability proof (including independent unbound native reference) before Build.
**Checkpoint**: Written pickup receipt cites file/test seams and **passing** evidence for both proofs; Keel read-only reanalysis clear. T001 itself does not edit production files. Any missing proof or recorded blocker stops Build and does not unlock T002+.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming the five authorized paths under `web/src/features/playback/` (`BrowserPlayer.tsx`, `useBrowserPlayer.ts`, `BrowserPlayer.test.tsx`, `playerCopy.ts`, `BrowserPlayer.css`) and recording any demonstrated drift as a **separate follow-up** before production edits. Establish **both** required proofs: (1) **origin/delivery-invariant** — authoritative route/config recon identifying the existing direct same-origin HTTP(S) library delivery invariant, **separately** corroborated by live Chromium real-media and Range/retry request and redirect chains via the Test URLs portal; (2) **portal capability** — Test URLs portal session-only evaluation on controlled media, including an **independent unbound native reference**. A same-origin `src`/`currentSrc` string or a successful sample alone is **insufficient**. Record the evidence. **STOP** (block Build) if either proof is missing or a blocker is recorded (`brief.md` Q3/Q6; Plan challenge decisions 1 and 3; Task order step 2; FR-010). **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **Strictly read-only reconnaissance and analysis** aside from collecting the receipt artifacts named by the Build lane.

**Checkpoint**: T002+ unlocks only when the Wisp receipt shows **both** origin/delivery-invariant proof **and** portal capability proof (including independent unbound native reference) **pass**, Keel pickup analysis is clear, and any authorized drift follow-up amendments are already recorded and applied. Any missing proof or recorded blocker stops Build and does not unlock T002+.

---

## Phase 2: User Story 1 - Boost quiet films above 100% with eligibility safeguards (Priority: P1) 🎯 MVP

**Goal**: Fail-closed Web Audio + verified direct same-origin HTTP(S) delivery-path eligibility lands in this same executable phase as (and before or with) first graph binding/control. Prepare/connect gain → compressor → destination before source attachment. Distinguish failed/invalidated attempt on a still-live unbound element (keep that element) from stale completion against a changed/detached element (attach to neither; do not replace current element) and from partial/uncertain ownership or previously bound ineligible transition (fresh native element with same-media continuity). Eligible boost control, lazy once-per-element graph across repeated activation/re-render/remount/actual hot reload, unity/limiting, reset, mute/volume multiply, existing keyboard exclusion, fullscreen parity. Phase 2 owns the failing-first coverage and pass bar for both failure stages and continuity. Preserve persistence requirements for later phases and the approved five-file envelope; do not prescribe React key, guard representation, or external helper exports.
**Independent Test**: Web coverage for FR-001–FR-007 / SC-001–SC-002 / SC-004 paths that do not require real-library portal evidence yet.
**Order**: After T001: **T002 ∥ T003** (parallel OK — different files), then **T004 → T005 → T006** (T005 waits for both T003 and T004). Same statement as Local phase dependency, Parallel Opportunities, and Parallel Example.

### Tests for User Story 1 (+ eligibility cases required before bind)

- [x] T002 [P] [US1] [US3] **Build lane**. After T001 (may run in parallel with T003): Using `web-implement` test-first, extend `web/src/features/playback/BrowserPlayer.test.tsx` with failing cases for gain mapping (100%/200%/300% → 1/2/3), reset restores unity, no media-element source bound before activation, **controllable AudioContext doubles**, distinct **repeated activation**, **re-render**, **remount**, and **same-actual-element module-reload** checks (module-reload simulates the actual hot-reload contract; one binding, no duplicate connections, retained live context), stock mute silences boosted audio, stock volume remains an input boost multiplies, **focused native range adjustment via existing keyboard exclusion** (1pp arrow steps without player shortcut duplication; other navigation keys keep native range semantics; Space/K, arrows, M, F retain usual scope at usual targets elsewhere), windowed/fullscreen control presence, **focus/open-alone does not activate**, **hidden boost + untouched native playback when Web Audio is missing or the active element/source generation does not match the verified direct same-origin delivery path** — including **a matching URL string with unverified or cross-origin effective delivery (native fallback)**, **failed/invalidated attempt on a still-live unbound element** → keep that element, no source attached, **stale completion against a changed/detached element** → attach to neither element and do not replace the current element due to the stale completion, **partial/uncertain ownership (including uncertain source-creation throw)** → fresh native element without binding on that attempt, **previously bound ineligible transition** → fresh native element with **same-media continuity** (seek when seekable after metadata, pause/play intent within browser policy, stock volume/mute, rate, caption selection/delay) via reused Media Chrome attach/detach, stock controls, and subtitle wiring (FR-001–FR-007; SC-001–SC-002; SC-004; `brief.md` Plan challenge decisions 1, 2, 4).

### Implementation for User Story 1 (+ eligibility before/with bind)

- [x] T003 [P] [US1] **Build lane**. After T001 (may run in parallel with T002): Add Volume boost / Reset boost labels (and any required accessible valuetext helpers) in `web/src/features/playback/playerCopy.ts` only as needed per `ASSUMPTIONS.md` / Q5 (FR-001). Conditional file — skip if labels already present. T005 consumes these labels; do not start T005 until T003 is done.
- [x] T004 [US1] [US3] **Build lane**. After T002 authored: In `web/src/features/playback/useBrowserPlayer.ts`, implement **fail-closed eligibility first** — runtime predicate is Web Audio availability plus active element/source generation matching the verified direct same-origin HTTP(S) delivery path, checked on source change and immediately before binding after resume; hide and keep native playback for malformed/unsupported schemes, indirect/blob/MSE/segmented paths outside the verified class, and matching URL with unverified/cross-origin effective delivery; no speculative probe or new API. Then element-bound graph ownership — lazy AudioContext create/resume on explicit activation only (not focus/open alone); **prepare and connect GainNode → DynamicsCompressorNode → destination before** attaching `createMediaElementSource`; attach source only after resume succeeds and eligibility still holds; once-per-element across repeated activation, re-render, remount, and **actual hot reload** (module-reload of the same actual element is the test simulation, not a narrower shipped limit); retain one live bound context; unity gain + transparent compressor at 100% including reset; limiting only above 100%; multiply boost on stock volume/mute; **failed/invalidated attempt on a still-live unbound element** → keep that element, no source; **stale completion against a changed/detached element** → attach to neither, do not replace the current element due to the stale completion; **partial/uncertain ownership (including uncertain source-creation throw)** or **previously bound ineligible transition** → fresh native element without binding the replacement on that attempt, reusing Media Chrome attach/detach, stock controls, and subtitle wiring with same-media continuity (FR-002–FR-007; Q2–Q3; Plan challenge decisions 1–2). Do not prescribe React key, guard representation, or external helper exports. Make T002 graph/activation/eligibility/failure/continuity cases pass.
- [x] T005 [US1] **Build lane**. After T003 and T004: Wire the Volume boost range and Reset boost button beside MediaVolumeRange in `web/src/features/playback/BrowserPlayer.tsx`, including visible percentage, accessible valuetext, visible focus, keyboard activation path into T004, **reuse of the existing player keyboard exclusion** for the focused native range (1pp arrows without player shortcut duplication; other range keys keep native semantics; existing hotkeys unchanged at usual targets), and retention inside the existing fullscreen-enabled control row (FR-001, FR-003; Q5; Plan challenge decision 4). Add placement/focus rules in `web/src/features/playback/BrowserPlayer.css` only as needed (conditional). Make remaining T002 UI/interaction cases pass.
- [x] T006 [US1] [US3] **Build lane**. After T005: Run the Phase 2 web tests in `web/src/features/playback/BrowserPlayer.test.tsx` and record pass evidence (or an explicit blocker distinct from a passed checkpoint). Checkpoint requires **pass** evidence covering hidden control, native playback (including matching-URL-but-unverified/cross-origin effective delivery), failed/invalidated unbound attempt (keep that element), stale completion against changed/detached element (attach to neither; do not replace current element), partial/uncertain ownership and previously bound ineligible transition (fresh element + same-media continuity), focus/open-alone no-op, controllable AudioContext doubles, distinct repeated-activation / re-render / remount / same-element module-reload checks (one binding, no duplicates, retained live context), focused native range keyboard exclusion / 1pp steps, and existing hotkeys at usual targets. This is the Phase 2 pass bar for both failure stages and continuity.

**Checkpoint**: US1 independently testable via web tests for boost graph/control behaviour **with** eligibility, failure-stage, and continuity safeguards already green in this phase. Do not proceed to Phase 3 until this checkpoint **passes**. A recorded blocker is not a passed checkpoint and does not unlock Phase 3.

---

## Phase 3: User Story 2 - Remember boost on this device (Priority: P1)

**Goal**: Namespaced localStorage restore, pending-activation indication, activation apply rules, invalid/failure isolation.
**Independent Test**: Persistence/pending cases in `BrowserPlayer.test.tsx`.
**Order**: T007 → T009 after Phase 2 checkpoint (T006). Sequential — shared `BrowserPlayer.test.tsx` / player sources; not parallel with Phase 4.

### Tests for User Story 2

- [x] T007 [US2] **Build lane**. After T006: Using `web-implement` test-first, extend `web/src/features/playback/BrowserPlayer.test.tsx` for restore of valid 100–300% with pending-activation indication when above 100%, apply-on-activation unless gesture sets/resets, invalid/missing/nonfinite/out-of-range → 100%, and throwing storage read/write leaving playback and in-session adjustment usable — following the existing `subtitlePreferences.ts` namespacing/error-isolation pattern **without modifying that file** (FR-008; SC-003; Q4; Plan challenge decision 4; Ledger F7). Not `[P]` — same test file as Phase 2/4.

### Implementation for User Story 2

- [x] T008 [US2] **Build lane**. After T007 authored: Implement one namespaced browser-only localStorage key plus pending-activation indication copy in `web/src/features/playback/useBrowserPlayer.ts` and, only as needed, `web/src/features/playback/playerCopy.ts` / `BrowserPlayer.tsx`, following the `subtitlePreferences.ts` namespacing/error-isolation pattern **without changing that file** (FR-008; `ASSUMPTIONS.md`; Plan challenge decision 4). Exact key string stays inside the playback seam. Make T007 pass.
- [x] T009 [US2] **Build lane**. After T008: Run the US2-focused web tests and record pass evidence (or explicit blocker distinct from a passed checkpoint). Confirm storage failure coverage still matches the `subtitlePreferences.ts` error-isolation pattern without editing that file.

**Checkpoint**: US2 independently testable via persistence/pending web tests. Do not proceed to Phase 4 until this checkpoint **passes**. A recorded blocker is not a passed checkpoint.

---

## Phase 4: User Story 3 - Persistence × eligibility interaction (Priority: P1)

**Goal**: After T007–T009 persistence work, prove the US2+US3 interaction and **re-run** Phase 2 failure-stage and same-media continuity regressions after persistence without reauthoring them.
**Independent Test**: Restored above-unity setting with ineligible playback or transition, plus re-run of existing Phase 2 eligibility/failure/continuity cases in `BrowserPlayer.test.tsx`.
**Order**: T010 → T011 after T009 (sequential after Phase 3; shared test/source files — never parallel with Phase 3). T007–T009 remain the persistence work and are not reopened here except as interaction inputs.

### Tests for User Story 3

- [ ] T010 [US2] [US3] **Build lane**. After T009: Using `web-implement` test-first, extend `web/src/features/playback/BrowserPlayer.test.tsx` with the **US2+US3 interaction** — a restored above-unity remembered setting together with ineligible playback (hidden boost, native untouched, pending activation not applied as a bind; including matching URL with unverified/cross-origin effective delivery) and/or failure/continuity paths (failed/invalidated unbound attempt → keep that element; stale completion against changed/detached → attach to neither, do not replace current; partial/uncertain ownership or previously bound ineligible → fresh native element with same-media continuity). **Re-run** the existing T002 eligibility/failure/continuity cases after persistence **without reauthoring** them (FR-006–FR-008; SC-003–SC-004; Q3–Q4; Plan challenge decision 2). Not `[P]`.

### Implementation for User Story 3

- [ ] T011 [US2] [US3] **Build lane**. After T010 authored: If any T010 interaction or re-run case fails, fix eligibility gating and recovery/continuity behaviour in `web/src/features/playback/useBrowserPlayer.ts` and `web/src/features/playback/BrowserPlayer.tsx` (FR-006–FR-008) without expanding the five-file envelope, without a new sibling module, and without rewriting T007–T009 persistence scope. Make T010 **pass** and record pass evidence.

**Checkpoint**: US2+US3 interaction and eligibility/failure/continuity re-runs **pass**. Phase 5 unlocks only after this checkpoint **passes**. A recorded blocker is not a passed checkpoint and does not unlock Phase 5.

---

## Phase 5: User Story 4 - Prove unity, 200% level, and real-library receipt (Priority: P2)

**Goal**: Calibrated analyser evidence + real-library Chromium receipt, both executable via temporary Test URLs portal session-only evaluation (not jsdom; not shipping modules).
**Independent Test**: Temporary portal observer before activation + analyser connected to real compressor output when graph is created on activation + standalone unbound native reference + receipt artifacts.
**Order**: After Phases 2–4 behaviour is green.

- [ ] T012 [US4] **Build lane**. After Phase 4 checkpoint **passes**: Via the **Test URLs portal** in Chromium, using **temporary session-only page evaluation** (not a shipping module; not jsdom as the receipt driver), run the repeatable calibrated method from `plan.md` Verification Approach: (1) record the Q2 test tolerance **before** collecting results without changing Q2 bounds; (2) **install the temporary portal observer before activation**; (3) capture a **standalone unbound native reference without binding**; (4) on explicit activation, **connect the analyser to the real compressor output when the graph is created**; (5) same calibrated quiet stimulus below compression onset, fixed stock volume, stabilized measurement window; **explicitly record gain values and RMS ratios at 100% and 200%** (active 100%, reset 100%, 200%); check **audible unity as well as measured unity** (SC-001); capture before/during boost and mute readings; demonstrate limiting with loud material above unity; confirm mute silences boosted audio; (6) **clean up** the temporary observer/analyser afterwards (FR-011; SC-001–SC-002; Q2/Q6; Plan challenge decision 3). Pass only when retained ratios meet the pre-declared tolerance and audible unity holds at 100%. Preserve the evidence for review. Record pass evidence or an explicit blocker (blocker ≠ passed delivery checkpoint).
- [ ] T013 [US4] **Build lane**. After T012: Via the Test URLs portal in Chromium with the same temporary session-only technique, play a real library movie, install the temporary portal observer before activation, connect the analyser to the real compressor output when the graph is created on activation, boost to 200%, capture a screenshot of the visible control and an analyser level reading, check fullscreen and windowed operation, and **clean up** afterwards (FR-011; SC-005; SMOKE-001 alignment; Q6; Plan challenge decision 3). Preserve the evidence for review. Record either pass evidence or an explicit receipt blocker (blocker ≠ passed delivery checkpoint).

**Checkpoint**: US4 delivery checkpoint passes only with calibrated + real-library **pass** evidence.

---

## Phase 6: Polish — web gates, honest C# labels, Size M review

**Purpose**: Cross-cutting quality bar before delivery PR merge.
**Order**: After Phases 2–5 intended for the PR are complete with Phase 5 evidence pass (a recorded blocker is not a passed delivery checkpoint).

- [ ] T014 **Gauge**. After delivery stories for the PR: Run applicable Phase 3 step 5 / Phase 5 gates explicitly (do not infer skips from file scope). Require `scripts/run-web-gates.ps1` exit 0 on the implementation diff. **Verify the implementation diff contains no production measurement hooks, exports, or globals** (FR-011; Plan challenge decision 3; Sentry F4 / Ledger F2). Scope **static-analysis** gates to **changed `.cs` files**; scope **mutation** to **production C# under `src/`**. When a gate’s own scope is empty, record `SKIPPED (scope-empty)`. When every changed project is listed in `gates.mutation.exclusions`, the script text is `NOT APPLICABLE` — record the reported verdict as **N/A**. Never PASS either label, and never conflate `SKIPPED (scope-empty)` with exclusions **N/A** (FR-012; Q6; Task order step 4).
- [ ] T015 **Gauge**. After T014: Collect and confirm Chromium **pass** evidence for SMOKE-001–003 via the Test URLs portal using controlled non-shipping cases inside the approved envelope — including **SMOKE-002 reset** (boost above 100% → Reset boost → displayed 100% / unity) and **SMOKE-003 ineligible/unavailable hidden control with stock playback unchanged**. Do not treat prior calibrated/real-library receipts as substitutes for these smoke observations. Record pass evidence or an explicit smoke blocker distinct from a passed checkpoint.
- [ ] T016 **Sentry / Ledger / Compass → Keel**. After T015 smoke **pass** evidence: Size M three-axis review (Risk, Standards, Spec) with explicit disposition for every finding. Remediate accepted in-scope Critical/High/Medium/Low within two rounds; each accepted fix MUST be **independently verified on the reviewed head** before that finding is closed. Apply brief Q7 follow-up/disposition rules before closure: Patron routes out-of-scope Critical/High, broken behaviour, or user-requested items via Rigger after checking open YouTrack issues; record other out-of-scope items as `noted, no ticket` in `CONCLUSIONS.md`. At the round cap, report remaining blockers and the affected head to Bernstein — do not close review with unverified fixes (`brief.md` Q7; SC-006).

**Checkpoint**: Delivery head ready for user merge of the Build PR only when T014 gates are recorded, T015 smoke **pass** evidence is on record, and T016 review is closed (a gate/smoke/review blocker is not a passed Phase 6 delivery checkpoint).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all Build work
- **Phase 1**: Read-only pickup; **both** origin/delivery-invariant proof (route/config recon + live Chromium corroboration) **and** portal capability proof (including independent unbound native reference) must pass before T002+; any missing proof or recorded blocker stops Build
- **Phase 2**: MVP boost graph/control **with** fail-closed eligibility, pre-source node prep, failed/invalidated unbound keep-element vs stale-completion no-replace vs fresh-element recovery, and same-media continuity in the same phase before/with first bind; owns failing-first pass bar for failure stages and continuity; blocks Phase 3 only on checkpoint **pass**
- **Phase 3**: After Phase 2 checkpoint **pass** (T006); T007–T009 persistence only (`subtitlePreferences.ts` pattern, file unchanged) — sequential, not parallel with Phase 4
- **Phase 4**: Requires Phase 3 checkpoint **pass** (T009); US2+US3 interaction + re-run Phase 2 failure/continuity regressions without reauthoring — sequential after Phase 3; Phase 5 unlocks only after the Phase 4 checkpoint passes
- **Phase 5**: After Phase 4 checkpoint **pass** (T011); calibrated then real-library evidence via temporary Test URLs portal session-only evaluation
- **Phase 6**: After PR-scoped delivery stories with Phase 5 evidence **pass**; Phase 6 delivery checkpoint requires T014 gates (including no production measurement hooks/exports/globals), T015 smoke **pass**, and T016 review closure

### User Story Dependencies

- **US1 (P1)**: After Phase 1 — MVP; includes eligibility/failure/continuity safeguards required before/with first bind
- **US2 (P1)**: After Phase 2 checkpoint **pass**; T007–T009 own persistence
- **US3 (P1)**: Safeguards land in Phase 2; Phase 4 re-runs after persistence without reauthoring
- **US4 (P2)**: After Phase 4 checkpoint **pass**

### Parallel Opportunities

- After T001 only: **T002 ∥ T003** (different files: `BrowserPlayer.test.tsx` vs `playerCopy.ts`); then **T004 → T005 → T006** with T005 waiting for both T003 and T004. Same dependency statement as Phase 2 Order and Local phase dependency.
- No other `[P]` claims: T007/T010 share `BrowserPlayer.test.tsx` with prior phases and must stay sequential
- Phases 3 and 4 are never parallel

---

## Parallel Example: After pickup (T001) and Gate 1

```text
Task: "BrowserPlayer.test.tsx US1+US3 cases (T002)"   # [P] with T003
Task: "playerCopy.ts boost labels (T003)"             # [P] with T002; T005 waits for T003 + T004
```

Then sequential: T004 (eligibility before/with bind; pre-source prep; unbound vs fresh-element recovery) → T005 (UI + existing keyboard exclusion; conditional CSS) → T006 → T007 → T008 → T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016.

---

## Implementation Strategy

### MVP First (User Story 1 + eligibility)

1. Complete Phase 0–2 (Gate 1, both delivery-invariant and portal capability proofs, eligibility/failure/continuity-safe boost graph/control + tests)
2. **STOP and VALIDATE**: Web tests prove gain/reset/activation/mute **and** hidden/native (including matching-URL unverified/cross-origin) / failed-invalidated unbound keep-element / stale-completion no-replace / partial-uncertain + previously bound fresh-element continuity / focus no-op / AudioContext doubles / distinct repeated-activation re-render remount module-reload / keyboard exclusion / hotkeys
3. Continue persistence (T007–T009) → US2+US3 interaction + re-runs (T010–T011, pass required) → Chromium portal session-only evidence (T012–T013) → gates including no production measurement surface (T014) + T015 smoke pass → review

### Incremental Delivery

1. US1 boost control/graph with US3 eligibility/failure/continuity safeguards in the same phase
2. US2 remembered setting + pending activation (T007–T009; `subtitlePreferences.ts` pattern, file unchanged)
3. US2+US3 interaction + failure/continuity re-runs (sequential; pass before Phase 5)
4. US4 calibrated + real-library Chromium receipts via temporary Test URLs portal session-only evaluation
5. Web gates (incl. no production measurement hooks) + T015 smoke pass + Size M review → user merges delivery PR

---

## Notes

- [P] tasks = different files, no incomplete deps; shared test/source files are never `[P]` across phases
- [Story] label maps task to US1–US4 for traceability
- Authorized files only: `BrowserPlayer.tsx`, `useBrowserPlayer.ts`, `BrowserPlayer.test.tsx`, `playerCopy.ts`, `BrowserPlayer.css` — no new sibling module
- `playerCopy.ts` / `BrowserPlayer.css` edits remain conditional (only as needed)
- Never claim PLAN FROZEN / Gate 1 / runtime Chromium PASS from this tasks draft
- Gate 1 remains pending; PLAN FROZEN authority is the future Keel freeze receipt in `CONCLUSIONS.md` only
- Blockers are always recorded distinctly from passed checkpoints
- Keep 12 FR / 6 SC and T000–T016; ordinary Gate 1 pending with zero structural checkboxes
