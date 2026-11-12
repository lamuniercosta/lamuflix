---

description: "Task list for DEV-500 subtitle timing and caption style options"
---

# Tasks: Subtitle timing and caption style options

**Input**: Design documents from `specs/DEV-500/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q10) plus Phase 2 step 6 amendment, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md`; Maestri notes `DEV-500`, `recon-DEV-500` (current `:7-16` — historical ticket/reference/dependency citations are qualified); `specs/PRODUCT.md`. Palette `design.md` delivered; corrections pending (Palette-owned).

**Status**: Draft — plan adjudication repair applied. Not PLAN FROZEN (pending independent Keel reread). Gate 1 pending user spec-PR merge. No implementation, dependency-ready, runtime, review-closure, or merge clearance.

**Tests**: Required (`brief.md` Q9; FR-017–FR-018). Prefer failing Vitest/RTL before production edits (`web-implement` TDD). Deterministic timing invariants in Vitest without a new property library. Co-located test-only `subtitleTestDoubles.ts` for fake media/cue readiness and stubbed rAF. Chromium **and** Firefox real-layout receipts are separate required acceptance evidence (after gates). No new test dependency/framework.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/receipt tasks are never `[P]`. Execute by explicit After-dependencies / phase order — do not run by numeric ID alone.

**Conventions**: Web tests Vitest/RTL; **no explanatory comments in test or production code** beyond AGENTS exemptions; fixture roots via external configuration only (never hardcode `E:\Filmes` or machine paths); retain media-chrome 4.19.4; reuse delivered DEV-503 shell without duplication or cherry-pick; Q8 file boundary only (including `subtitleTestDoubles.ts`); single `useSubtitleOptions` instance with props-only renderers; no new architectural/state layer; no backend/API/contracts/package/harness/subtitle-layout edits; preserve generated contract usage; backend identity stability remains unproven.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` / `web-implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp DEV-503 pickup recon + Keel reanalysis + Gauge baseline) — blocks T002+
Phase 2   T002 → T005   (web-implement; US1 pure timing + delay persistence / JSON-array identity)
Phase 3   T006 → T009   (web-implement; US2 caption style preferences + preview rules / enum tokens)
Phase 4   T010 → T013   (web-implement; US3 cue intervals owner + shared overlay lifecycle)
Phase 5   T014 → T022   (web-implement; US4 four seams red→green + focused regression; same-file serial)
Phase 6   T023          (Gauge web gates / applicable harness checks) — BEFORE receipts
Phase 7   T024 → T025   (fixtures + Chromium/Firefox real-layout acceptance receipts)
Phase 8   T026          (capped review / remediation per Q1 bar)
Phase 9   T027          (delivery / merge-bar handoff — user merges)
```

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-500 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-500 in `specs/DEV-500/CONCLUSIONS.md`. Receipt only. T001–T027 MUST NOT proceed without verified merge (`brief.md` Gate expectations; Q8/Q10; no structural owner checkbox on this ticket). PLAN FROZEN / plan challenge remain Keel-owned and are not claimed by Quill drafting.

---

## Phase 1: Pickup recon (blocks implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; confirm DEV-503 CC shell delivered; refresh Options integration points; locate fixtures/references; incorporate Palette `design.md` (delivered; apply pending corrections if present).
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved dependency/decision/reference gaps stop — no invention. Baseline captured.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main verifying: DEV-503 CC shell merged and present; exact Options-tab integration ownership/files (provisional historical recon points `BrowserPlayer.tsx:73-75,100,156,199-207` and `SubtitleSelector.tsx:26-38,60-79` — amend brief before tasks if ownership drifted); approved editable Q8 files under `web/src/features/playback/` including `subtitleTestDoubles.ts`; read-only/out-of-bounds surfaces (generated types, package manifests, harness, backend); Palette `design.md` delivered with any pending corrections applied (do not treat design as absent; do not invent geometry beyond design); whether ticket reference assets (`ref-options-timing.png`, `ref-options-style-1.png`, `ref-options-style-2.png`) are available (availability unverified — do not invent defaults from missing images); external fixture-root configuration seams for real `E:\Filmes` copies with `Subs\<video stem>-<code>.srt` without hardcoding paths. Qualify that current `recon-DEV-500:7-16` does not re-prove historical ticket lines and that backend identity stability remains unproven. **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit web-gates baseline. **STOP** rather than duplicating the CC shell, cherry-picking unmerged DEV-503 commits, inventing visual geometry absent from brief/`design.md`, or claiming screenshot fidelity without inspected references (`brief.md` Q2/Q7/Q8/Q9; FR-015–FR-016). No production edits in this task.

---

## Phase 2: User Story 1 - Subtitle timing delay (Priority: P1) 🎯 MVP

**Goal**: Pure timing math and delay preference codec — integer ms steps, sign convention, JSON-array pair identity, reset isolation — with tests green before UI wiring.
**Independent Test**: Unit tests prove 100/500 ms steps, signed one-decimal display including zero as `0.0 s`, pair key encode/decode (adversarial + normalization), reset-only-delay, invalid delay → 0, without requiring overlay or Options shell.
**Order**: T002 → T005 (execute by After-deps, not numeric ID alone).

### Tests for User Story 1

- [ ] T002 [P] [US1] After T001: Add failing Vitest coverage in `web/src/features/playback/subtitleTiming.test.ts` for ordinary 100 ms / Shift 500 ms steps, negative-earlier/positive-later sign convention, signed one-decimal display including zero as `0.0 s`, no arbitrary clamp, and deterministic timing invariants (FR-002–FR-004, FR-017; SMOKE-001/004 foundations; `brief.md` Q4/Q7)
- [ ] T003 [P] [US1] After T001: Add failing Vitest coverage in `web/src/features/playback/subtitlePreferences.test.ts` for delay key `lamuflix:subtitle-delay:v1:${JSON.stringify([numericMovieId, trackId])}`, unambiguous JSON-array pair encoding, delimiter/Unicode/case/empty/long track-ID cases, round-trip/pair separation, numeric movie normalization (equivalent numeric forms share one key; do not require distinct keys for intentionally equivalent forms), reorder/reload with unchanged IDs, safe-integer validation, invalid delay → 0, saved-pair restore vs new-pair zero, and reset removing only the delay key — noting backend identity stability is unproven and real reload acceptance remains mandatory (FR-005, FR-010–FR-013, FR-017; SMOKE-003; plan-challenge item 1)

### Implementation for User Story 1

- [ ] T004 [US1] After T002–T003: Implement pure timing helpers in `web/src/features/playback/subtitleTiming.ts` and delay codec/validation/defaults in `web/src/features/playback/subtitlePreferences.ts` per brief Q4/Q6, plan-challenge amendment, and ASSUMPTIONS (integer ms; JSON-array identity; no arbitrary CSS; no server writes; immediate best-effort persistence contract at codec layer) (FR-003–FR-005, FR-010–FR-011, FR-015)
- [ ] T005 [US1] After T004: Make T002–T003 green against T004 without weakening assertions (FR-017)

**Checkpoint**: US1 pure timing/persistence independently testable.

---

## Phase 3: User Story 2 - Caption style and preview rules (Priority: P1)

**Goal**: Global style profile defaults/validation/storage codec and preview-sample copy constants matching the provisional Q3 profile with internal enum tokens.
**Independent Test**: Unit tests prove default profile (including 75% background opacity, Edge None), range/step validation, font/edge token enums, invalid style → full default profile, global key round-trip, and style reset independence from delay.
**Order**: T006 → T009.

### Tests for User Story 2

- [ ] T006 [US2] After T005: Add failing Vitest coverage in `web/src/features/playback/subtitlePreferences.test.ts` (extend) for global key `lamuflix:subtitle-style:v1`, provisional default profile from ASSUMPTIONS/Q3, permitted ranges/steps, internal font tokens `system-sans`|`serif`|`monospace`, edge tokens `none`|`drop-shadow`|`raised`|`depressed`|`outline`, hex colors, invalid/malformed style → full default profile, and Reset-to-default removing only the style key without clearing delay (FR-006–FR-007, FR-010–FR-012, FR-017; SMOKE-002; plan-challenge item 6)

### Implementation for User Story 2

- [ ] T007 [P] [US2] After T006: Extend `web/src/features/playback/subtitlePreferences.ts` with style profile defaults/validation/codec per Q3/Q6/ASSUMPTIONS and ratified enum tokens (FR-006–FR-007, FR-010–FR-011, FR-015)
- [ ] T008 [P] [US2] After T006: Add ticket labels/help and settled preview sample copy to `web/src/features/playback/subtitleCopy.ts` (`Negative shows subtitles earlier, positive later.`; preview sample `The quick brown fox jumps over the lazy dog.`; separate labels Text color, Text opacity, Background color, Background opacity; exact `Text size (%)`) (FR-001, FR-006, FR-014, FR-015; Q7)
- [ ] T009 [US2] After T007–T008: Make T006 green against T007–T008 (FR-017)

**Checkpoint**: US1 + US2 preference rules independently testable.

---

## Phase 4: User Story 3 - Shared live overlay and cue shifting (Priority: P2)

**Goal**: Named `subtitleTiming.ts` ownership of shifted half-open intervals/active cues; shared CaptionOverlay + cue synchronization from copied immutable baseline + delay; `hidden` native track proof deferred to US4/browsers; cleanup; preview reuse.
**Independent Test**: `subtitleTiming.test.ts` proves baseline copy immutability, half-open boundaries, long sequences, large delay; overlay/cue tests prove resync, playback-only scheduling behavioural pauses/unchanged-cue no-updates, multi-cue/line breaks, literal markup, generation/identity guards, and cleanup via `subtitleTestDoubles.ts` without Options shell complete.
**Order**: T010 → T013 (T010 green owner is T012; T011 green owner is T013).

### Tests for User Story 3

- [ ] T010 [P] [US3] After T009: Add failing Vitest coverage in `web/src/features/playback/subtitleTiming.test.ts` (extend; this named file only — no unnamed cue-helper test module) for copied immutable baseline of primitive cue data (mutating originals cannot alter snapshot; no mandatory runtime `Object.freeze` / mutation-throws assertion), effective half-open interval `[start+delay, end+delay)`, no cumulative drift, negative-start overlap at 0, omit-already-ended cues, end-at-zero/start-at-zero/negative-overlap/adjacent boundaries, repeated +100 ms then −100 ms exactness, and large-delay empty active set without crash (FR-004, FR-009, FR-017; plan-challenge item 2; Risk F5/F6)
- [ ] T011 [P] [US3] After T009: Add co-located test-only helper `web/src/features/playback/subtitleTestDoubles.ts` (fake media/TextTrack/cue readiness/events; stubbed rAF; usable with fake timers) and failing Vitest/RTL coverage in `web/src/features/playback/CaptionOverlay.test.tsx` and `web/src/features/playback/useSubtitleCues.test.ts` for shared style application (preview sample + live cues), independent text/background opacity, five edge mappings, multiple cues/line breaks, literal plain-text rendering of markup-looking italic/bold text and img-onerror / SVG-onload / javascript-link / style-string attack vectors with no corresponding active elements or executed handlers, pointer-transparent non-interactive rendering, resync on delay/track/seek/pause/play/rate/cue-ready, playback-only frame loop with no paused frame rescheduling and no observable repeated updates for unchanged ordered active-cue identities (behavioural evidence — no internal `setState` spy), generation/identity guards with deferred out-of-order resolution producing no wrong-pair/stale writes, rapid switches, StrictMode remount, and cleanup of each actually acquired frame/listener/observer on switch/unmount (do not invent ResizeObserver); preserve existing abort/object-URL cleanup ownership without duplicate fetch/cleanup owners (FR-006, FR-008–FR-009, FR-012, FR-014, FR-017; SMOKE-002 foundations; `brief.md` Q5; Risk F1/F5/F8; Ledger 3)

### Implementation for User Story 3

- [ ] T012 [US3] After T010: Extend `web/src/features/playback/subtitleTiming.ts` as the explicit green owner of shifted-interval/active-cue rules to make T010 green (half-open intervals; copied immutable baseline; boundary/long-sequence/large-delay behaviour) without weakening assertions (FR-004, FR-009, FR-015, FR-017; Ledger 1–2)
- [ ] T013 [US3] After T011–T012: Implement `web/src/features/playback/CaptionOverlay.tsx`, `captionOverlay.css`, and `useSubtitleCues.ts` per Q5/Q7, plan-challenge amendment, and Palette `design.md` bounds (shared live/preview props-fed plain-text renderer inside media-chrome fullscreen subtree; playback-only scheduling; generation/identity guards; cleanup; no new dependency); make T011 green (FR-008–FR-009, FR-015–FR-017)

**Checkpoint**: Overlay/cue lifecycle independently testable; cue-interval rules have named green owner.

---

## Phase 5: User Story 4 - Options seams, hotkeys, storage failure, hidden track (Priority: P2)

**Goal**: Four serial seams — preference lifecycle/storage, presentational controls, shell/shortcuts, loaded/`hidden` track — each with red→green progression; single hook owner; props-only renderers; storage failure safety; focused regression without duplicated test-writing.
**Independent Test**: Each seam’s tests cover its contract; integration proves Options + player behaviour including focus set, immediate persistence, accessor throws, `mode = 'hidden'`, and preserved selection/Off/retry.
**Order**: T014 → T022. Same-file edits are serial (never `[P]` across shared files). Execute by After-deps.

### Seam A — Preference lifecycle / storage (`useSubtitleOptions`)

- [ ] T014 [US4] After T013: Add failing Vitest coverage in `web/src/features/playback/useSubtitleOptions.test.ts` for pair/global preference lifecycle; stale-switch races; generation/identity completion guards; immediate in-memory adjustments plus immediate best-effort persistence (no mandatory debounce timing or bounded-write-count requirement); separate storage access/read/write/remove/quota failure paths; lazy storage accessor-getter throw coverage independently of method throws; blocked access initializes defaults; failed write retains current in-memory adjustment without claiming persistence; independent reset updates memory and attempts removal of only its own key; failed removal does not promise the reset survives reload; uninterrupted playback; no cross-tab live-sync / no mandatory storage-event listener test (FR-010–FR-013, FR-015, FR-017; Risk F4/F7; plan-challenge items 1/4/7)
- [ ] T015 [US4] After T014: Implement `web/src/features/playback/useSubtitleOptions.ts` as the sole preference lifecycle owner and make T014 green (FR-010–FR-013, FR-015, FR-017)

### Seam B — Presentational controls (`SubtitleOptions`, props-only)

- [ ] T016 [US4] After T015: Add failing Vitest/RTL coverage in `web/src/features/playback/SubtitleOptions.test.tsx` for Options timing/style controls and labels (including signed one-decimal delay display with zero as `0.0 s`; separate Text color / Text opacity / Background color / Background opacity; exact `Text size (%)`); timing disabled without usable track; Options editable when Off; props-only rendering (no direct storage/media access in the component under test) (FR-001, FR-006, FR-014–FR-015, FR-017; SMOKE-001–002)
- [ ] T017 [US4] After T016: Implement props-only `web/src/features/playback/SubtitleOptions.tsx` and `subtitleOptions.css` per Q7 and Palette `design.md` (provisional chrome/targets/checker/no-transitions within design bounds); make T016 green (FR-001, FR-006, FR-014–FR-016, FR-017)

### Seam C — Shell / shortcuts (`BrowserPlayer` + delivered shell)

- [ ] T018 [US4] After T017: Add failing Vitest/RTL coverage extending `web/src/features/playback/BrowserPlayer.test.tsx` and `web/src/features/playback/subtitleSelector.test.tsx` for single `useSubtitleOptions` instance wired with props into Options/overlay; G/H playing-only with focus guards for `input` (including range/color), `textarea`, `select`, contentEditable, and dialog contexts (including descendants) plus Ctrl/Alt/Meta and no-usable-track guards; Options coexistence in delivered DEV-503 shell; rapid switches; StrictMode remount; no timing/style reload/refetch; fullscreen overlay mount; no duplicate native/custom captions at the shell level (FR-002, FR-008, FR-012–FR-015, FR-017, FR-019; SMOKE-004; Risk F1/F9; plan-challenge items 3/7). Same-file serial ownership: do not parallelize with T019/T022.
- [ ] T019 [US4] After T018: Integrate into `web/src/features/playback/BrowserPlayer.tsx` (instantiate `useSubtitleOptions` once; mount overlay; G/H in existing shortcut scope; pass props to Options/overlay) and delivered DEV-503 shell via `web/src/features/playback/SubtitleSelector.tsx` / CC shell — do not duplicate shell or cherry-pick; make T018 green (FR-002, FR-008, FR-012–FR-016, FR-019)

### Seam D — Loaded / hidden track

- [ ] T020 [US4] After T019: Add failing Vitest coverage extending `web/src/features/playback/subtitleTrackModes.test.ts` and any needed `useSubtitlePlayback` tests asserting selected track `mode = 'hidden'` (literal target) and one custom stream; DOM-alone does not prove native suppression — browser receipts remain mandatory (FR-008, FR-015, FR-017; Risk F2)
- [ ] T021 [US4] After T020: Adjust `web/src/features/playback/useSubtitlePlayback.ts` and `web/src/features/playback/subtitleTrackModes.ts` only as needed for loaded/`hidden` selected-track behaviour; make T020 green (FR-008, FR-015, FR-017)

### Focused regression (deduplicated)

- [ ] T022 [US4] After T021: Verify existing DEV-503 selection/Off/retry behavioural assertions remain green and add only net-new integration cases required for Options coexistence in `web/src/features/playback/BrowserPlayer.test.tsx` and `web/src/features/playback/subtitleSelector.test.tsx` without rewriting or duplicating T016/T018 test-authoring responsibility; preserve prior assertions (FR-015, FR-017; Ledger 6). Same-file serial after T018–T019.

**Checkpoint**: All user stories independently functional in component tests; four US4 seams each have red→green owners.

---

## Phase 6: Web gates (focused regression → applicable gates)

**Purpose**: Honest gate reporting for in-scope web changes after focused regression (T022), **before** real-browser receipts (`brief.md` Q8; `plan.md` dependency order; Ledger 7).
**Checkpoint**: `scripts/run-web-gates.ps1` exit 0; C# scope-empty reported SKIPPED (scope-empty) never PASS.

- [ ] T023 After T022: **Gauge** runs `scripts/run-web-gates.ps1` for implementation web changes and applicable configured gates. Web gate exit 0 required when web is in scope. Report scope-empty C# exits as SKIPPED (scope-empty), never PASS; harness-disabled SKIP and mutation N/A only when scripts report them; exit 1 and Could not run block. Any .NET property-gate opt-out must be explicitly recorded by Conductor as `propertyTests: opt-out` with frontend-only rationale — do not silently count exit 2 as PASS. Do not claim C# gates passed for a web-only diff; do not substitute plain build (`brief.md` Q9; SC-006). Gates MUST complete before T024–T025.

---

## Phase 7: Chromium and Firefox real-layout acceptance (required evidence)

**Purpose**: Prove behaviours unit tests cannot (native `hidden` suppression, real media, fullscreen, cross-browser presentation, blocked storage, responsive scale, motion/no-transition, target-size, panel contrast, screenshot fidelity) against real-library-layout fixtures — after applicable gates.
**Checkpoint**: Receipts published with PASS or explicit **Could not run** + open acceptance task — never synthetic-only PASS. If acceptance fixes change code, rerun affected tests (T002–T022 as applicable), rerun T023 gates, and refresh receipts at the resulting head.

- [ ] T024 After T023: Obtain externally configured real movie-folder copies retaining `Subs\<video stem>-<code>.srt` and ticket reference assets (`ref-options-timing.png`, `ref-options-style-1.png`, `ref-options-style-2.png`). Record fixture provenance without secrets. Reference availability is unverified until obtained. If fixtures/references/runtime unavailable, mark acceptance **Could not run** and keep the task open — do not substitute unit tests or synthetic same-folder-only fixtures as PASS (FR-018; `brief.md` Q9)
- [ ] T025 After T024 (when runnable): Execute **Chromium and Firefox** acceptance — exercise delay ± / Shift / Reset timing / G/H; reload and pair isolation; every style control including fullscreen; Reset to default; blocked storage with uninterrupted playback; Off retention; assert selected track remains `mode = 'hidden'` / no second caption stream; observe responsive scale, motion/no-transition behaviour, interactive target size, and panel contrast (do not infer measured AA PASS from prose alone; user-adjusted caption contrast remains user-controlled); capture receipts (browser/version, reviewed commit, fixture-copy path proving real-library `Subs` layout, actions, observations, screenshots). Inspect actual references before claiming screenshot/default fidelity; record unverified honestly if references still missing (FR-018; SMOKE-001–004 runtime; SC-005; Risk coverage summary). Receipt-driven code fixes require affected tests + T023 gates rerun and these receipts refreshed at resulting head.

---

## Phase 8: Review / remediation (Q1 closing bar)

**Purpose**: Capped challenge/review under the frozen closing bar.
**Checkpoint**: Zero above-bar findings or unresolved items explicitly left for user review on the PR at the 2-round / 2-commit-per-round cap — never auto-cleared.

- [ ] T026 After T023 and T025 status recorded: Conduct capped review/remediation (at most two rounds and two fix commits per round) against the Q1 closing bar — every verified in-scope Critical/High/Medium/Low correctness, acceptance, and documented-standard finding blocks closure until fixed or demonstrated invalid; out-of-scope enhancements are follow-ups only. Size M requires Sentry, Ledger, and Compass axis reports; missing reports block adjudication (`brief.md` Q1/Q9)

---

## Phase 9: Delivery handoff

**Purpose**: Prepare merge-bar evidence packaging; only the user merges.
**Checkpoint**: No claim of merge clearance by agents.

- [ ] T027 After T026: Publish exact-head merge-bar evidence package for DEV-500 per pipeline; leave merge to the user. Do not claim PLAN FROZEN, Gate 1, dependency readiness, backend identity stability, or runtime PASS retroactively from Build work alone.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (spec-PR + T000)**: Bernstein/Rigger produce/open; user merges; Keel receipt blocks all Build work
- **Phase 1 (T001)**: Depends on T000; blocks implementation; requires DEV-503 delivered on main; design delivered (corrections pending)
- **US1 (Phase 2)**: Depends on T001
- **US2 (Phase 3)**: Depends on US1 checkpoint (T005)
- **US3 (Phase 4)**: Depends on US2 checkpoint (T009); T012 greens T010; T013 greens T011
- **US4 (Phase 5)**: Depends on US3 checkpoint (T013); seams A→B→C→D→regression (T014→T022) serial across shared files
- **Gates (Phase 6 / T023)**: Depends on US4 focused regression (T022); runs **before** fixtures/browser receipts
- **Browsers (Phase 7 / T024–T025)**: Depends on T023; may **Could not run** if fixtures/references missing — never blocks honest gate reporting by preceding it; receipt-driven fixes re-enter affected tests + T023
- **Review (Phase 8)**: Depends on T023 complete and T025 status recorded (PASS or Could not run)
- **Delivery (Phase 9)**: Depends on review cap reached

### User Story Dependencies

- **US1 (P1)**: After T001 — timing/persistence MVP rules + JSON-array identity
- **US2 (P1)**: After US1 — style profile rules/copy + enum tokens
- **US3 (P2)**: After US2 — cue-interval owner + shared overlay consumes preferences
- **US4 (P2)**: After US3 — four Options seams + hidden track + storage failure

### Parallel Opportunities

- T002 can run in parallel with T003 once T001 is done (different primary test files)
- T007 can run in parallel with T008 once T006 is written (different files)
- T010 can run in parallel with T011 once T009 is done (different primary files / test-only helper)
- No parallel production edits to the same `BrowserPlayer.tsx` / `SubtitleSelector.tsx` / `useSubtitleOptions.ts` / track-mode files (T015/T017/T019/T021/T022 stay serial on their owned files)

### Parallel Example: User Story 1 tests

```bash
Task: "Add failing subtitleTiming.test.ts coverage for step/sign invariants (T002)"
Task: "Add failing subtitlePreferences.test.ts coverage for JSON-array delay keys (T003)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T000–T001
2. Complete US1 (T002–T005)
3. **STOP and VALIDATE** pure timing/persistence independently

### Incremental Delivery

1. US1 → timing/delay persistence + identity codec
2. US2 → caption style profile + copy + enum tokens
3. US3 → cue-interval owner + shared overlay/cue lifecycle
4. US4 → four seams (lifecycle → controls → shell/shortcuts → hidden track) → focused regression
5. Applicable web gates → Chromium+Firefox receipts → capped review

### Suggested MVP scope

User Story 1 pure timing/persistence is the smallest demonstrable slice; ticket acceptance still requires US2–US4 and both browser receipts before closure.

---

## Notes

- Palette `design.md` is delivered; corrections pending — incorporate settled/corrected requirements; do not invent layout/shadow geometry; do not imply design is unavailable (FR-016; Ledger 8 / Compass 5)
- Screenshot/reference fidelity stays explicitly unverified until T024–T025 inspect assets; provisional Q3 profile is not a fidelity claim
- Backend identity stability stays unproven; historical recon citations stay qualified
- Never lower gate thresholds; never treat SKIPPED (scope-empty) as PASS
- Quill does not invent PLAN FROZEN; Keel owns independent reread / freeze after this repair
