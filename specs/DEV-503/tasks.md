---

description: "Task list for DEV-503 browser player controls rebuild"
---

# Tasks: Rebuild browser player controls with CC panel and settings menu

**Input**: Design documents from `specs/DEV-503/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q8; Q8 supersedes conflicting Q1–Q7 wording), `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-503`, `recon-DEV-503` (including source facts 186–214); `specs/PRODUCT.md`; challenge adjudication `PLAN_ADJUDICATION.md`. Palette `design.md` when delivered (Palette-owned; plan-challenge prerequisite).

**Status**: PLAN FROZEN — Keel, 2026-11-12, after round-1 three-axis adjudication and independent repair validation (`brief.md` freeze decision; `PLAN_ADJUDICATION.md`). Gate 1 pending user spec-PR merge. No implementation, runtime, code-review, or merge clearance.

**Tests**: Required (`brief.md` Q6; FR-016–FR-017). Prefer failing Vitest/RTL before production edits (`web-implement` TDD). RTL covers roles/state/focus movement/interaction only — not computed CSS, focus-ring appearance, media-query, or 320px layout proof. Chromium real-layout receipts are separate required acceptance evidence. No new test dependency/framework.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/receipt tasks are never `[P]`.

**Conventions**: Web tests Vitest/RTL; **no explanatory comments in test or production code** beyond AGENTS exemptions; fixture roots via external configuration only (never hardcode `E:\Filmes` or machine paths); retain media-chrome 4.19.4; do not invent undocumented media-chrome prop/export/import/open-state names — verify official 4.19.4 docs at T001 and STOP on unsupported seams; optional co-located CC panel extraction uses `web/src/features/playback/SubtitlePanel.tsx` and `subtitlePanel.test.tsx` only if accessibility or complexity warrants it (brief Q7); otherwise keep the panel in the approved existing files; extraction is not mandatory; no invented line-count threshold; Q8-approved editable copy files `playerCopy.ts` / `subtitleCopy.ts`; hooks/`subtitleTrackModes.ts`/`subtitleContent.ts`/generated types are read-only; no mandatory `AbortController`/hook rewrite.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` / `web-implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + DEV-493 overlap + media-chrome 4.19.4 export verification + Keel reanalysis + Gauge baseline) — blocks T002+
Phase 2   T002 → T004   (web-implement; US1 combined time + cleanup)
Phase 3   T005 → T008   (web-implement; US2 CC shell + track list)
Phase 4   T009 → T011   (web-implement; US3 gear speed)
Phase 5   T012 → T014   (web-implement; US4 focus/fullscreen/hotkeys + movie-details)
Phase 6   T015 → T016   (Chromium real-layout acceptance receipts)
Phase 7   T017          (Gauge web gates)
Phase 8   T018          (capped review / remediation per Q2 bar)
Phase 9   T019          (delivery / merge-bar handoff — user merges)
```

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [X] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-503 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-503 in `specs/DEV-503/CONCLUSIONS.md`. Receipt only. T001–T019 MUST NOT proceed without verified merge (`brief.md` Gate expectations; Q8 point 9; `PLAN_ADJUDICATION.md` repair 9; no structural owner checkbox on this ticket).
  - Receipt (remediation round 2): Gate 1 clearance is VERIFIED — spec commit `9642646` (`DEV-503 - spec`, parent `5f07ce2`) is present on `origin/main` (confirmed via `git log origin/main --grep='DEV-503 - spec'` and `git show -s 9642646` this session). Menu seams are verified by working code (media-chrome 4.19.4 settings/speed menu wired in `BrowserPlayer.tsx`, seven exact rates from the live media rate, web gates PASS). DEV-493 overlap is verified clean (DEV-493 landed on main as `187e7d1b`; DEV-503 rebased over it with conflicts resolved, exactly one in-bar play asserted).

---

## Phase 1: Pickup recon (blocks implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; inventory approved files and recon 186–214 facts; record DEV-493 duplicate-Play owner/overlap; verify media-chrome **4.19.4** official exports/semantics before any production edit; locate reference assets and fixture configuration seams without hardcoding paths; incorporate any settled Palette `design.md` requirements already present.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision/unsupported media-chrome seams stop — no invention. Baseline captured.

- [X] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: approved editable files `web/src/features/playback/BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, `playerCopy.ts`, `subtitleCopy.ts`, `BrowserPlayer.test.tsx`, `subtitleSelector.test.tsx`, optional Q7 targets `SubtitlePanel.tsx` / `subtitlePanel.test.tsx` only if extraction is later warranted, `web/src/features/movie-details/MovieDetailsView.tsx`, `MovieDetailsView.subtitles.test.tsx`; read-only hooks/`subtitleTrackModes.ts`/`subtitleContent.ts`/generated `web/src/api/types.ts`; concrete generated subtitle routes/fields and helper module (`recon-DEV-503` 190–192); current polite status vs alert roles/actions including optional Open Local when enabled (`recon-DEV-503` 194–197); `MediaController noHotkeys` and BrowserPlayer hotkey guard (`recon-DEV-503` 199–201); copy/CSS token/reduced-motion ownership (`recon-DEV-503` 208–210); stale-result/`listGeneration`/`attemptRef`/owned-URL cleanup (`recon-DEV-503` 203–206); media-chrome **4.19.4** manifest pin versus **official documented** settings-menu exports, import paths, rates/labels, native semantics, open-state, anchor, and fullscreen composition (installed exports were unverified at `recon-DEV-503` 212–214 — document chosen composition/synchronization; do not hard-bind challenger spellings); DEV-493 duplicate-Play cleanup overlap and **current owner** (recon currently locates duplicate in BrowserPlayer `:233-242`); whether Palette `design.md` / ticket reference assets are available in the Build workspace (availability unverified — do not claim measured absence). **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit web-gates baseline. **STOP** `needs recon` on missing/drifted seams; **STOP** `needs decision` rather than inventing undocumented media-chrome APIs, replacing media-chrome, inventing visual details absent from brief/design.md, or forcing unsupported label customization (`brief.md` Q3/Q6/Q7/Q8; FR-015; FR-018; `PLAN_ADJUDICATION.md` repairs 1/7). Optional panel filenames are already settled by Q7; extraction remains optional and is not mandatory; no invented line-count threshold. No production edits in this task. No mandatory `AbortController`/hook rewrite.
  - Receipt (remediation round 2): Gate 1 clearance is VERIFIED via spec commit `9642646` present on `origin/main`. Menu seams are verified by working code — installed `media-chrome` `4.19.4` (`package.json:2-3`) exports `MediaSettingsMenu`, `MediaSettingsMenuItem`, `MediaSettingsMenuButton` plus React wrappers (`dist/menu/index.d.ts:3-5`, `dist/react/menu/index.js:34-42,44-52,64-72`), wired in `BrowserPlayer.tsx` (`MediaSettingsMenuButton` + `MediaPlaybackRateMenu` with the seven exact rates and `noautohide`), covered by `BrowserPlayer.test.tsx` with web gates PASS. DEV-493 overlap is verified clean — DEV-493 landed on `origin/main` as `187e7d1b` (confirmed ancestor of this head via `merge-base --is-ancestor` this session); the DEV-503 rebase resolved the BrowserPlayer/MovieDetailsView conflicts and exactly one in-bar play is asserted. The `plan.md:7,62` Gate-1-pending note is superseded by the landed spec commit.

---

## Phase 2: User Story 1 - Combined time and control-bar cleanup (Priority: P1) 🎯 MVP

**Goal**: Combined `MediaTimeDisplay` with duration; control bar composition with real fullscreen; remove separate duration display, subtitle `<select>`, and duplicate Play where present (respect DEV-493 overlap and T001 owner); token-based narrow wrap/seek-row foundations.
**Independent Test**: Vitest/RTL proves combined time presentation, exactly one in-bar play, absence of duplicate Play/`<select>`/separate duration, without requiring CC/gear behaviour complete.
**Order**: T002 → T003 → T004.

### Tests for User Story 1

- [X] T002 [US1] After T001: Add failing Vitest/RTL coverage in `web/src/features/playback/BrowserPlayer.test.tsx` (and movie-details assertions in `web/src/features/movie-details/MovieDetailsView.subtitles.test.tsx` only if T001 confirms ownership/integration need) for combined current/duration presentation, absence of separate duration display, absence of subtitle `<select>`, **exactly one in-bar play control**, absence of duplicate Play, and presence of play/pause, seek, combined time, mute/volume, CC, gear, and real fullscreen controls (FR-001–FR-002, FR-008, FR-016; SMOKE-001)

### Implementation for User Story 1

- [X] T003 [US1] After T002: In `web/src/features/playback/BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, and Q8-approved `playerCopy.ts` / `subtitleCopy.ts` as needed: implement combined `MediaTimeDisplay` with duration enabled (tabular numerals per `design.md`), remove separate `MediaDurationDisplay`, compose control bar (play/pause, seek, combined time, mute/volume, Subtitles/Settings triggers, **real MediaFullscreenButton / fullscreen control — no fullscreen placeholder**) per `design.md` (wireframe annotations never rendered), apply token-based wrapped strip / seek-own-row foundations retaining volume reachability, remove subtitle `<select>` wiring and **dead select CSS/test selectors while preserving behavioural tests**, route new copy through approved copy files, and **remove duplicate Play remove-if-present at the owner confirmed by T001** (recon currently BrowserPlayer; edit `MovieDetailsView.tsx` only if T001 confirms ownership/necessary integration), accounting for DEV-493 overlap and preserving the in-bar play control (FR-001–FR-002, FR-008, FR-015, FR-018; `PLAN_ADJUDICATION.md` repairs 2/6/8). Do not edit read-only hooks/helpers/generated types. No global `index.css` edit.
- [X] T004 [US1] After T003: Make T002 green against T003 without weakening assertions (FR-016)

**Checkpoint**: US1 independently testable.

---

## Phase 3: User Story 2 - CC panel shell and local-track selection (Priority: P1)

**Goal**: Persistent CC; bottom-right non-modal labelled panel with Subtitles/Options/close; Off + served-track `radiogroup`/`radio` checkmark list; Options empty-shell copy; preserve failure/empty and stale-result semantics without hook rewrite.
**Independent Test**: Open CC; tabs; Off/track selection with accessible selected state; Options copy; zero/unavailable/retry preserved; rapid A/B/Off and stale lifecycle through unchanged hooks — without requiring gear complete.
**Order**: T005 → T008. Optional co-located panel files `SubtitlePanel.tsx` / `subtitlePanel.test.tsx` only if accessibility or complexity warrants extraction (brief Q7); otherwise keep the panel in the approved existing files; extraction is not mandatory; no invented line-count threshold.

### Tests for User Story 2

- [X] T005 [P] [US2] After T004: Add failing Vitest/RTL coverage in `web/src/features/playback/subtitleSelector.test.tsx` for Off + served-track checkmark list as `radiogroup`/`radio`/`aria-checked` (not `<select>`; not mixed listbox authority), accessible selected state, label/order retention, preserved zero/loading/unavailable/content-failure/retry semantics, and **rapid track A/B/Off plus movie-change-during-fetch / stale resolve / owned-URL release regressions through unchanged hook behaviour** (no AbortController redesign) (FR-005, FR-013–FR-014, FR-016; SMOKE-003; `PLAN_ADJUDICATION.md` repairs 3/5)
- [X] T006 [US2] After T004: Add failing Vitest/RTL coverage in `web/src/features/playback/BrowserPlayer.test.tsx` for persistent CC visibility across empty/failure states, non-modal labelled panel open with Subtitles default tab (`tablist`/`tab`/`tabpanel`), Options empty-shell copy `Subtitle timing and style options will appear here.`, close control, basic open/close, and polite `role=status` versus distinct `role=alert` preservation (never call alert polite) (FR-003–FR-006, FR-014, FR-016, FR-019; SMOKE-002)

### Implementation for User Story 2

- [X] T007 [US2] After T005–T006: Implement CC panel shell and Subtitles/Options presentation in `web/src/features/playback/BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, and Q8-approved `playerCopy.ts` / `subtitleCopy.ts` per `design.md` and Q8 (cue-card panel as **non-modal labelled region, not role=dialog**; Subtitles trigger with aria-expanded/controls; close labelled Close subtitles panel; `tablist`/`tab`/`tabpanel` with roving tabindex, Left/Right/Home/End, automatic activation; track `radiogroup`/`radio`/`aria-checked` with Off, arrows move/select, Space/Enter activate, one radio tab stop; Options empty-shell copy; **token-based bounded scrollable bottom-right overlay**; **extend incumbent reduced-motion block**; wireframe annotations never rendered) — plus optional co-located `web/src/features/playback/SubtitlePanel.tsx` and `subtitlePanel.test.tsx` **only if** accessibility or complexity warrants extraction (brief Q7); otherwise keep the panel in the named approved files; extraction is not mandatory; keep overlays inside the fullscreen player subtree; preserve read-only hook contracts and stale-result/URL cleanup; no fake search/timing/style controls (FR-003–FR-006, FR-009, FR-013–FR-015, FR-018–FR-019; `PLAN_ADJUDICATION.md` repairs 2/3/6)
- [X] T008 [US2] After T007: Make T005–T006 green against T007 (FR-016)

**Checkpoint**: US1 + US2 independently testable.

---

## Phase 4: User Story 3 - Gear speed menu (Priority: P2)

**Goal**: Media-chrome settings-menu gear with exact seven speeds, current selection from live media rate, playback rate change, mutual exclusion with CC.
**Independent Test**: Gear exposes exactly 0.5–2 / 0.25 steps; selection matches live rate across transitions; opening gear closes CC and vice versa.
**Order**: T009 → T011.

### Tests for User Story 3

- [X] T009 [US3] After T008: Add failing Vitest/RTL coverage in `web/src/features/playback/BrowserPlayer.test.tsx` for gear menu speeds exactly `0.5, 0.75, 1, 1.25, 1.5, 1.75, 2`, current selection indication **matching real media rate on ratechange / source / movie / metadata changes**, playback rate change, no off-step options, no saved preference, **no forced 1× reset**, and mutual exclusion versus CC (FR-007, FR-010, FR-016; SMOKE-004; `PLAN_ADJUDICATION.md` repair 7)

### Implementation for User Story 3

- [X] T010 [US3] After T009: Wire media-chrome settings-menu Settings/speed in `web/src/features/playback/BrowserPlayer.tsx` / `BrowserPlayer.css` / Q8-approved `playerCopy.ts` using **T001-verified** official media-chrome 4.19.4 docs for exact APIs and `design.md` / Q8 presentation (`0.5x`…`2x`, `1x (Normal)` only if label customization supported — else STOP for decision rather than custom menu/library substitution; room for future audio/quality without fabricating working deferred controls or rendered placeholder entries); reflect actual media rate including ratechange/source/movie/metadata transitions; retain verified native speed semantics (not rewrapped); no persistence and no forced 1× (FR-007, FR-015, FR-018; `PLAN_ADJUDICATION.md` repairs 2/7)
- [X] T011 [US3] After T010: Make T009 green against T010 (FR-016)

**Checkpoint**: US1–US3 independently testable.

---

## Phase 5: User Story 4 - Keyboard, focus, fullscreen, hotkey isolation (Priority: P2)

**Goal**: Focus entry/restore, page-delivered Escape / `fullscreenchange` fallback / `composedPath` outside click, fullscreen retention, hotkey isolation vs preservation (each shortcut), movie-change/unmount reset, movie-details integration regressions, coordinated effect cleanup.
**Independent Test**: Interaction matrix passes in component tests (roles/state/focus/interaction only); integration regressions green.
**Order**: T012 → T014.

### Tests for User Story 4

- [X] T012 [US4] After T011: Add failing Vitest/RTL coverage in `web/src/features/playback/BrowserPlayer.test.tsx` (and `subtitleSelector.test.tsx` / `MovieDetailsView.subtitles.test.tsx` / optional `subtitlePanel.test.tsx` as needed) for mutual exclusion already covered plus focus entry on open; page-delivered Escape focus restore to trigger before application fullscreen-exit handling; `fullscreenchange`-to-exited fallback closing surfaces with connected-trigger else player-host focus; outside-click destination focus via `composedPath` (active surface + trigger inside; other trigger switches once; fullscreen reachable outside dismisses; `pointercancel` alone does not); keyboard-only tab/track/Off/speed operation; **hotkey consumption with exactly one action** while panel/menu controls consume keys, and **preserved Space/K/left-right 10-second seek/M/F** on tabs, rows, close, speed, and elsewhere; movie-change surface reset with focus restore only if focus was inside the removed surface; unmount cleanup without focusing detached nodes or stealing destination focus; rapid track A/B/Off / movie-change-during-fetch / stale resolve / URL release via unchanged hooks; existing playback/seek/error regressions; live rate indication across ratechange/source/movie/metadata; polite status vs alert roles/state; **extend incumbent reduced-motion block coverage without claiming computed CSS/media-query/320px layout or focus-ring appearance proof in RTL** (FR-009–FR-014, FR-016, FR-018–FR-019; `PLAN_ADJUDICATION.md` repairs 3–7)

### Implementation for User Story 4

- [X] T013 [US4] After T012: Implement focus/dismissal/hotkey/fullscreen coordination and lifecycle reset **only in these named files as required**: `web/src/features/playback/BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, Q8-approved `playerCopy.ts` / `subtitleCopy.ts`, optional Q7 `SubtitlePanel.tsx` / `subtitlePanel.test.tsx` if extracted, and bounded `web/src/features/movie-details/MovieDetailsView.tsx` integration with existing named tests `BrowserPlayer.test.tsx` / `subtitleSelector.test.tsx` / `MovieDetailsView.subtitles.test.tsx` (plus optional `subtitlePanel.test.tsx`). Explicitly implement: **page-delivered Escape closes the open surface and restores trigger focus before application fullscreen-exit handling** (no claim of control over browser-owned Escape); **`fullscreenchange`-to-exited fallback that closes stale surfaces and restores focus to a connected trigger else the connected player host**; **movie-change conditional focus restore only when focus was inside the removed surface**; unmount cleans subscriptions without focusing detached nodes or stealing destination focus; `composedPath` outside dismissal and control-first/`defaultPrevented` hotkey consumption (unconsumed keys retain app behaviour). Keep `MediaController noHotkeys` and BrowserPlayer bubble hotkeys; one local open-surface coordinator reconciled to **T001-verified** native menu state; name coordinated effect cleanup for document/fullscreen subscriptions; do not mandate capture-phase playback or guessed native state props; preserve read-only hooks/`subtitleTrackModes.ts`/`subtitleContent.ts`/generated types and existing stale-result contracts; no new state library; no widening refactor (FR-009–FR-015; `PLAN_ADJUDICATION.md` repairs 2/4/5; H1 implementation closure)
- [X] T014 [US4] After T013: Make T012 green against T013; confirm US1–US3 regressions still green (FR-016)

**Checkpoint**: All user stories independently functional in component tests.

---

## Phase 6: Chromium real-layout acceptance (required evidence)

**Purpose**: Prove behaviours unit tests cannot (native fullscreen, real media, CSS appearance, 320px layout, actual reduced motion including native menu, native Escape outcome, screenshot fidelity) against real-library-layout fixtures.
**Checkpoint**: Receipts published with PASS or explicit **Could not run** + open acceptance task — never synthetic-only PASS.

- [ ] T015 After T014: Obtain externally configured real movie-folder copies retaining `Subs\<video stem>-<code>.srt` and ticket reference assets (`ref-subtitles-tab.webp`, `current-player-time-bug.webp`, and Options refs for shell context only). Record fixture provenance without secrets. Reference availability is unverified until obtained. If fixtures/references/runtime unavailable, mark acceptance **Could not run** and keep the task open — do not substitute unit tests or synthetic same-folder-only fixtures as PASS (FR-017; `brief.md` Test strategy; `PLAN_ADJUDICATION.md` repair 1)
- [ ] T016 After T015 (when runnable): Execute Chromium acceptance — play a real movie; capture readable current/duration; exercise CC open/both tabs/close/outside click in windowed and fullscreen; **record actual native Escape outcome and page-level closure** (no unsupported browser-event guarantee); **keyboard-only select track then Off with visible/hidden captions in both windowed and native fullscreen**; gear speed change with current-state indication matching live rate; focus/mutual exclusion; preserved Space/K/left-right 10s seek/M/F; verify **320px-width operability** so control-bar and CC/gear controls are not clipped or unreachable (wrapped strip, seek row, constrained scrollable overlay retaining volume); verify **actual `prefers-reduced-motion: reduce` behaviour including native menu internals** by extending the incumbent block; confirm visible focus appearance and existing status vs alert behaviour in real layout; capture screenshots and compare delivered shell to references without claiming deferred Options content. Publish receipts (steps, browser/version, outcomes including Escape/320px/reduced-motion/focus/live-region/track-Off windowed+fullscreen results, screenshot locations, comparison results) (FR-017–FR-019; SMOKE-001–004 runtime; SC-005; `PLAN_ADJUDICATION.md` repairs 5/6/8)

---

## Phase 7: Web gates

**Purpose**: Honest gate reporting for in-scope web changes.
**Checkpoint**: `scripts/run-web-gates.ps1` exit 0; C# scope-empty reported SKIPPED (scope-empty) never PASS.

- [ ] T017 After T014 (and with T016 status recorded): **Gauge** runs `scripts/run-web-gates.ps1` for implementation web changes and applicable configured gates. Web gate exit 0 required when web is in scope. Report scope-empty C# exits as SKIPPED (scope-empty), never PASS; harness-disabled SKIP and mutation N/A only when scripts report them; exit 1 and Could not run block. Do not claim C# gates passed for a web-only diff; do not substitute plain build (`brief.md` Gate expectations; SC-006)

---

## Phase 8: Review / remediation (Q2 closing bar)

**Purpose**: Capped challenge/review under the frozen closing bar.
**Checkpoint**: Zero above-bar findings or unresolved items explicitly left for user review on the PR at the 2×2 cap — never auto-cleared.

- [ ] T018 After T016–T017: Conduct capped review/remediation (at most two rounds and two fix commits per round) against the Q2 closing bar — every verified in-scope Critical/High/Medium/Low correctness, explicit-AC, accessibility, contract, or documented-standard violation blocks closure until fixed or demonstrated invalid; preference/unrelated improvements are follow-ups only (`brief.md` Loop discipline)

---

## Phase 9: Delivery handoff

**Purpose**: Prepare merge-bar evidence packaging; only the user merges.
**Checkpoint**: No claim of merge clearance by agents.

- [ ] T019 After T018: Publish exact-head merge-bar evidence package for DEV-503 per pipeline; leave merge to the user. Do not claim PLAN FROZEN retroactively from Build work alone.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (spec-PR + T000)**: Bernstein/Rigger produce/open; user merges; Keel receipt blocks all Build work
- **Phase 1 (T001)**: Depends on T000; blocks implementation
- **US1 (Phase 2)**: Depends on T001
- **US2 (Phase 3)**: Depends on US1 checkpoint (T004)
- **US3 (Phase 4)**: Depends on US2 checkpoint (T008)
- **US4 (Phase 5)**: Depends on US3 checkpoint (T011)
- **Chromium (Phase 6)**: Depends on US4 (T014); may **Could not run** if fixtures missing
- **Gates (Phase 7)**: Depends on implementation complete; records Chromium status honestly
- **Review (Phase 8)**: Depends on gates + Chromium status recorded
- **Delivery (Phase 9)**: Depends on review cap reached

### User Story Dependencies

- **US1 (P1)**: After T001 — MVP
- **US2 (P1)**: After US1 — CC/track value
- **US3 (P2)**: After US2 foundations — gear
- **US4 (P2)**: After US3 — coordination across surfaces

### Parallel Opportunities

- T005 can run in parallel with T006 once T004 is done (different primary test files)
- No parallel production edits to the same `BrowserPlayer.tsx` / `SubtitleSelector.tsx` file

### Parallel Example: User Story 2 tests

```bash
Task: "Add failing subtitleSelector.test.tsx coverage for Off/track radiogroup list and stale-lifecycle regressions (T005)"
Task: "Add failing BrowserPlayer.test.tsx coverage for CC shell (T006)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T000–T001
2. Complete US1 (T002–T004)
3. **STOP and VALIDATE** combined time + cleanup independently

### Incremental Delivery

1. US1 → combined time/cleanup
2. US2 → CC shell + tracks
3. US3 → gear speeds
4. US4 → focus/fullscreen/hotkeys
5. Chromium receipts + web gates + capped review

### Suggested MVP scope

User Story 1 only (readable time + control cleanup) is the smallest demonstrable slice; ticket acceptance still requires US2–US4 and Chromium receipts before closure.

---

## Notes

- Incorporate Palette `design.md` settled requirements when present; do not invent visuals (FR-018). Palette independently repairs `design.md` for challenge round 1.
- Optional co-located panel extraction uses `SubtitlePanel.tsx` / `subtitlePanel.test.tsx` only if accessibility or complexity warrants it (brief Q7); otherwise keep in approved existing files; extraction is not mandatory; no invented line-count threshold
- Reference comparison must state delivered-shell areas vs deferred Options contents; wireframe annotations are never rendered
- Never lower gate thresholds; never treat SKIPPED (scope-empty) as PASS
- Challenge round 1 authority: `PLAN_ADJUDICATION.md`. PLAN FROZEN recorded by Keel after independent validation; Gate 1 remains pending

