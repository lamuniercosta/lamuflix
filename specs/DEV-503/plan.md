# Implementation Plan: Rebuild browser player controls with CC panel and settings menu

**Branch**: `feature/503-spec` | **Date**: 2026-11-12 | **Spec**: `specs/DEV-503/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q8; Q8 supersedes conflicting Q1–Q7 interaction/design/file-boundary wording); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-503`, `recon-DEV-503` (lines 19–26, 28–90, 92–184, **186–214 source facts**); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only); challenge adjudication `PLAN_ADJUDICATION.md`. Ticket dependencies: none.

**Status**: PLAN FROZEN — Keel, 2026-11-12, after round-1 three-axis adjudication and independent repair validation (`brief.md` freeze decision; `PLAN_ADJUDICATION.md`). Gate 1 pending user spec-PR merge. No implementation, runtime, code-review, or merge clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`, linked `PLAN_ADJUDICATION.md`. No `research.md`, `data-model.md`, or `contracts/` — behaviour and UI-only scope are ruled in the brief; no public API change. Optional co-located panel filenames settled by brief Q7: `SubtitlePanel.tsx` / `subtitlePanel.test.tsx` only if accessibility or complexity warrants extraction; otherwise existing approved files; extraction is not mandatory; no invented line-count threshold. Q8 adds `playerCopy.ts` / `subtitleCopy.ts` to the editable boundary.

## Summary

Rebuild the in-page Media Chrome browser player controls: combined `MediaTimeDisplay` with duration, persistent non-modal CC panel shell (Subtitles/Options) with Off + served local-track `radiogroup`/`radio` checkmark list replacing the `<select>`, media-chrome settings-menu gear speeds 0.5–2× in 0.25 steps (native semantics verified at T001), removal of duplicate Play where present (recon currently in BrowserPlayer), and keyboard/fullscreen/dismissal/hotkey coordination with `composedPath` outside dismissal and page-delivered Escape-before-application-exit — inside the approved `web/src/features/playback` (+ bounded movie-details integration) files including Q8 copy modules — with Vitest/RTL role/state/focus/interaction coverage and separate Chromium real-layout visual receipts.

## Technical Context

**Language/Version**: TypeScript + React 19 + Vite in `web/` (UI-only ticket; no C# production edits planned).

**Primary Dependencies**: Existing media-chrome **4.19.4** only (owner series ruling; manifest pin verified at `web/package.json` / lock — `recon-DEV-503` 212–214; **installed exports/import paths/open-state/label APIs remain unverified** until T001 official-doc verification). No new npm dependency or player library. Retain existing `useBrowserPlayer` / `useSubtitlePlayback` hooks and served-track contracts read-only (`brief.md` Q3/Q8; `recon-DEV-503` lines 23–26, 78, 190–206). Generated routes/DTO fields and `subtitleContent.ts` helper are established facts (`recon-DEV-503` 190–192); no contract delta.

**Storage**: N/A for this ticket. No server persistence. No localStorage introduction here (series delay/style persistence belongs to later tickets; this ticket adds neither setting nor persistence).

**Testing**: Vitest + React Testing Library in existing `BrowserPlayer.test.tsx`, `subtitleSelector.test.tsx`, and `MovieDetailsView.subtitles.test.tsx` (plus optional `subtitlePanel.test.tsx` only if Q7 extraction is warranted). Prefer failing tests before production edits (`web-implement` TDD). RTL asserts roles, state, focus movement, and interaction — **not** computed CSS, focus-ring appearance, media-query, or 320px layout proof. Separate required Chromium real-browser acceptance with externally configured real movie-folder fixtures retaining `Subs\<video stem>-<code>.srt`; never hardcode machine paths. No new test framework/dependency. No explanatory comments in test or production code beyond AGENTS exemptions.

**Target Platform**: Desktop Chromium for acceptance receipts; SPA browser player inside movie details.

**Project Type**: SPA feature-folder UI change under existing `web/src/features/playback` and bounded `movie-details` integration.

**Performance Goals**: Interactive personal-library playback controls; no new SLA numeric target beyond usable control/panel response.

**Constraints**: Charter §2.3 items 1–6 deliberately ruled (Q3/Q8). No new dependency/layer/schema/API/LocalPlay/secrets/Process.Start. Overlays inside fullscreen player subtree. Official media-chrome 4.19.4 docs for exact settings-menu APIs at T001 — do not invent undocumented prop names or hard-bind challenger spellings; STOP on unsupported seams. Keep `MediaController noHotkeys` and BrowserPlayer bubble hotkeys with control-first/`defaultPrevented` ordering; do not mandate capture-phase playback. Preserve existing stale-result/URL cleanup; no mandatory `AbortController`/hook rewrite. Visual details follow Palette `design.md` + Q8 `[assumed]` copy using existing CSS tokens. Never lower harness thresholds. Web gates via `scripts/run-web-gates.ps1`; scope-empty C# exits reported as SKIPPED (scope-empty), never PASS.

**Scale/Scope**: Size M; UI control rebuild within named files; optional one small co-located panel component/test under playback if accessibility/complexity warrants — exact optional filenames `web/src/features/playback/SubtitlePanel.tsx` and `subtitlePanel.test.tsx` per brief Q7; otherwise keep in approved existing files; extraction is not mandatory; no invented line-count threshold.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). UI-only scope; no owner structural checkbox.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **N/A / PASS** — no Core/Infrastructure/Api production edits; no new port. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no new commands/queries. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new public API/error contract; preserve existing subtitle failure UX (polite status vs alert; optional Open Local when enabled) without inventing statuses. |
| VI (Observability) | Safe diagnostics | **PASS** — no new telemetry names; no secrets/paths in receipts beyond fixture provenance without secrets. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, Process.Start, secrets, or options-record changes; fixture roots external only; no hardcoded machine paths. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new domain term; spec-local CC/gear wording only; no CONTEXT.md/ADR edit. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — Vitest/RTL for composition/interaction roles/state/focus; required Chromium runtime receipts for native fullscreen, real media, CSS appearance, 320px layout, actual reduced motion, native Escape, and screenshot fidelity that unit tests cannot prove. |
| Tech stack / API docs | OpenAPI chain | **PASS** — contract generation not required; generated subtitle routes/DTO fields retained as established (`recon-DEV-503` 190–192); any newly necessary contract change would need a new cited ruling (none in brief). |
| Governance / Static-Analysis Gates | Honest exits | Phase B: `scripts/run-web-gates.ps1` exit 0 when web in scope; C# scope-empty → SKIPPED (scope-empty); exit 1 / Could not run block; do not claim C# PASS for web-only diff. Closing bar and 2×2 caps per brief Q2. |

### Complexity Tracking

No constitution violations requiring justification. Optional co-located panel extraction is authorized only if accessibility/complexity warrants it (Q3); exact optional filenames settled by Q7 as `SubtitlePanel.tsx` / `subtitlePanel.test.tsx`; otherwise keep in approved existing files; extraction is not mandatory; no invented line-count threshold (Q8).

## Approach

- **Phase A (this draft + challenge repairs)**: Quill writes/repairs spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts (including 186–214), PRODUCT, settled Palette `design.md` bindings, and `PLAN_ADJUDICATION.md` repair list 1–9. Keel independently validates before PLAN FROZEN. Capped plan challenge (≤2 rounds). Spec PR: Bernstein coordinates; Rigger prepares/opens; user merges; Keel T000 records verified merge receipt. No Build work before Gate 1. This repair ask does not open/merge the PR or edit production code.
- **Phase B pickup (Build workspace, after Gate 1)**: Fresh recon for `origin/main` drift, named file inventory (including copy modules), generated routes/fields/helper module, current roles/actions, `noHotkeys`/guard, stale-result ownership, DEV-493 duplicate-Play overlap and owner, and **official media-chrome 4.19.4 settings-menu export/import/props/state/label/open-state/anchor/fullscreen composition documentation** — STOP on drift or unsupported seams. Obtain externally configured real-layout fixtures and ticket reference assets (availability unverified until obtained).
- **Dependency-ordered implementation (`brief.md` Q6)**:
  1. Combined time + control-bar cleanup foundations (real fullscreen control; remove-if-present duplicate at T001 owner; dead select CSS/tests removed; token-based wrap/seek-row).
  2. CC panel shell foundations (non-modal labelled region; tabs/close; `radiogroup`/`radio`) then Off + served-track list; route new copy through `playerCopy.ts` / `subtitleCopy.ts`; bounded scrollable overlay.
  3. Gear speed menu via media-chrome settings-menu (exact APIs from T001-verified official docs; live rate indication; no forced 1×).
  4. Local open-surface coordinator; focus / page-delivered Escape / `fullscreenchange` fallback / `composedPath` outside click / mutual exclusion / fullscreen / hotkey isolation + movie-details integration regressions; effect cleanup named; no capture-phase mandate.
  5. Gauge web gates; Chromium acceptance receipts; capped review/remediation; merge-bar later — user merges only.
- **media-chrome**: Keep existing controller and `noHotkeys`; compose settings-menu for gear/speed only after T001 documents supported seams; keep overlays in fullscreen subtree. Consult official docs for element/prop names at pickup — never guess from challengers.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-503/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── PLAN_ADJUDICATION.md   # Keel challenge dispositions + consolidated repair list
├── spec.md
├── plan.md              # This file
├── tasks.md
├── DRAFTING_RECEIPT.md
├── checklists/requirements.md
└── design.md            # Palette-owned (present; not authored/edited by Quill)
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; UI-only; no API contract delta.

**Design binding (from `design.md` + Q8 `[assumed]`)**: Cue-card CC panel bottom-right as non-modal labelled region; triggers labelled Subtitles / Settings; close labelled Close subtitles panel; tabular time numerals; speed items with `x` suffix and `1x (Normal)` subject to T001; `tablist`/`tab`/`tabpanel` (roving tabindex, Left/Right/Home/End, automatic activation) and track `radiogroup`/`radio`/`aria-checked`; preserve polite `role=status` vs `role=alert`; extend incumbent reduced-motion block; 320px operability via wrapped strip, seek row, and constrained scrollable overlay retaining volume; existing CSS tokens; overlays inside MediaController fullscreen subtree; wireframe annotations never rendered. Optional co-located extraction filenames per Q7: `SubtitlePanel.tsx` / `subtitlePanel.test.tsx` only if warranted; otherwise existing approved files; extraction is not mandatory.

### Source Code (approved boundary — Q3 + Q8 / recon lines 23–25, 208–210)

```text
web/src/features/playback/
├── BrowserPlayer.tsx              # control bar, MediaTimeDisplay showDuration, CC/gear composition, local open-surface coordinator, overlays in fullscreen subtree
├── BrowserPlayer.css              # panel/control styles per design.md + ticket shell behaviour; extend incumbent reduced-motion; remove dead select CSS; token-based wrap/seek-row
├── SubtitleSelector.tsx           # replace <select> presentation with CC radiogroup list/shell integration as ruled
├── playerCopy.ts                  # Q8-approved editable copy module (new copy routes here)
├── subtitleCopy.ts                # Q8-approved editable copy module (new copy routes here)
├── BrowserPlayer.test.tsx         # extend Vitest/RTL (roles/state/focus/interaction)
├── subtitleSelector.test.tsx      # extend Vitest/RTL
├── SubtitlePanel.tsx              # optional only if a11y/complexity warrants (Q7); otherwise omit and keep panel in approved files
└── subtitlePanel.test.tsx         # optional pair for SubtitlePanel.tsx only; extraction is not mandatory

web/src/features/movie-details/
├── MovieDetailsView.tsx                 # bounded integration only; duplicate-Play remove-if-present solely if T001 confirms ownership here
└── MovieDetailsView.subtitles.test.tsx  # integration/regression coverage

Read-only (do not edit): useBrowserPlayer.ts, useSubtitlePlayback.ts, subtitleTrackModes.ts, subtitleContent.ts, web/src/api/types.ts (generated). No global index.css edit. Any additionally necessary edit requires a cited decision before work.
```

**Structure Decision**: Stay inside existing playback feature folder and bounded movie-details edits. No new top-level folder, project, or architectural layer. Optional panel extraction remains co-located under playback without introducing an abstraction layer. No new state library. One authoritative local open-surface coordinator in BrowserPlayer synchronized to verified native menu state with coordinated effect cleanup.

## Test Strategy

| Family | Required evidence |
|---|---|
| Vitest/RTL — composition | Combined time; control set including real fullscreen; absence of separate duration / `<select>` / duplicate Play; exactly one in-bar play remains |
| Vitest/RTL — CC | Persistent CC; tab switch (tablist/tab/tabpanel); close; Off + served tracks as radiogroup/radio/aria-checked; Options empty copy; zero/loading/unavailable/retry preserved; polite status vs alert; rapid A/B/Off + movie-change-during-fetch/stale resolve/URL release via unchanged hooks |
| Vitest/RTL — gear | Exactly seven speeds; current selection matches live media on ratechange/source/movie/metadata; rate change; no off-step options; no forced 1×; no saved preference |
| Vitest/RTL — interaction | Mutual exclusion; trigger toggle; focus entry; page-delivered Escape restore before application exit; composedPath outside (surface+trigger inside; pointercancel alone does not); hotkey isolation for each preserved shortcut incl left/right 10s seek on tabs/rows/close/speed and elsewhere; movie-change focus-only-if-inside; unmount cleanup without detached focus |
| Vitest/RTL — a11y / design binding (FR-018–FR-019) | Roles/state/focus movement only; extend incumbent reduced-motion block coverage without duplicating it or claiming computed CSS/media-query/320px layout proof in RTL |
| Vitest/RTL — integration | Movie-details bounded integration + existing playback/seek/error regressions |
| Chromium acceptance | Real-layout fixtures; readable current/duration; CC open/tabs/close/Escape/outside in windowed+fullscreen; **keyboard track→Off with visible/hidden captions in both windowed and native fullscreen**; gear speed; hotkeys; **320px-width operability** (wrap/seek-row/scrollable overlay); **actual reduced-motion including native menu internals**; visible focus appearance; status vs alert observed; **record actual native Escape outcome**; screenshots vs `ref-subtitles-tab.webp` and `current-player-time-bug.webp`; other Options refs for shell context only |
| Gates | `scripts/run-web-gates.ps1` exit 0 when web in scope; honest SKIPPED/SKIP/N/A/Could not run reporting |

**Design-binding verification outcomes (FR-018 / design.md a11y)**:
1. At 320px viewport width in Chromium real-layout acceptance: all ticket control-bar and CC/gear surface controls remain reachable and operable without clipping (wrapped strip, seek row, constrained scrollable overlay retaining volume).
2. With `prefers-reduced-motion: reduce`: panel/menu transitions are disabled by extending the incumbent BrowserPlayer reduced-motion block; Chromium proves actual motion including native menu internals. RTL does not claim computed CSS/media-query layout proof.
3. Focusable controls show a visible focus appearance in Chromium; RTL asserts focus movement/roles/state only. Status and error text retain existing polite `role=status` versus distinct `role=alert` semantics — never call alert polite; no new a11y library or source-scope expansion.

Unit/component assertions cannot prove native fullscreen, real media behaviour, screenshot fidelity, CSS appearance, full 320px real-layout operability, actual reduced motion, or native Escape outcome — Chromium receipts remain required (`brief.md` Test strategy; Q8).

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q8 (media-chrome retention, file boundary including copy modules, optional panel filenames, interaction contract as refined by Q8, failure states, gate/Chromium rules). Unverified media-chrome installed exports remain a blocking T001 prerequisite, not invented facts. No open `needs decision` items remain in `DRAFTING_RECEIPT.md` after Q7/Q8 for inventable behaviour. No data-model or contracts artifacts for this UI-only ticket. Challenge round 1 repairs: `PLAN_ADJUDICATION.md`.

## Post-design Constitution Check

Re-checked after documenting the UI-only structure above and challenge round 1 repairs: still PASS/N/A as in the Constitution Check table; no new port, handler, schema, API, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan.

