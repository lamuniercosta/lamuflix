# Implementation Plan: Subtitle timing and caption style options

**Branch**: `feature/500-spec` | **Date**: 2026-11-12 | **Spec**: `specs/DEV-500/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q10) plus Phase 2 step 6 plan-challenge amendment; `CONCLUSIONS.md` (Patron seven-item ruling); `ASSUMPTIONS.md`; Maestri notes `DEV-500`, `recon-DEV-500`. **Evidence qualification:** current `recon-DEV-500:7-16` supplies frontend identity observations and unavailable backend/restoration evidence; earlier ticket/reference/dependency recon citations in Q1–Q10 are historical and no longer resolve in the current note (basis preserved in grill exchanges). `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependency: delivered DEV-503 CC shell. Palette `design.md` delivered; corrections pending.

**Status**: Draft — plan adjudication repair applied (`PLAN_ADJUDICATION.md` Quill list). Not PLAN FROZEN (pending independent Keel reread). Gate 1 pending user spec-PR merge. No implementation, dependency-ready, runtime, review-closure, or merge clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — behaviour is ruled in the brief; no public API change. `design.md` is Palette-owned (delivered; corrections pending).

## Summary

Fill the browser-player CC Options tab with subtitle timing (delay −/value/+, Reset timing, G/H while playing) and caption style (live preview + font/size/color/opacity/edge controls + Reset to default). Persist delay per numeric-movie + exact track ID (`JSON.stringify([numericMovieId, trackId])`) and style globally in localStorage only via immediate best-effort saves. Render captions via a shared props-fed custom overlay inside media-chrome with selected native track `mode = 'hidden'` and instant cue shifting from a copied immutable baseline using half-open intervals. Reuse the delivered DEV-503 shell; instantiate `useSubtitleOptions` once in `BrowserPlayer` and pass props to Options/overlay. Implement only after dependency merge + refreshed recon. Verify with Vitest/RTL (including `subtitleTestDoubles.ts`) plus real Chromium and Firefox receipts against real-library-layout fixtures.

## Technical Context

**Language/Version**: TypeScript + React 19 + Vite in `web/` (UI-only ticket; no C# production edits planned).

**Primary Dependencies**: Existing media-chrome **4.19.4** only (owner series ruling; ticket-named). No new npm dependency or player library. Reuse delivered DEV-503 CC shell / tab behaviour. Retain existing playback/subtitle hooks and served-track contracts; edit `useSubtitlePlayback.ts` / `subtitleTrackModes.ts` only as needed for loaded/`hidden` selected-track integration (`brief.md` Q8).

**Storage**: Browser `localStorage` only — `lamuflix:subtitle-style:v1` (global style); `lamuflix:subtitle-delay:v1:${JSON.stringify([numericMovieId, trackId])}` (per-pair delay). Immediate in-memory updates + immediate best-effort writes (no mandatory debounce). No server persistence, schema, subtitle-file writes, or cross-tab live-sync requirement. Backend track-ID stability across rescans is unproven.

**Testing**: Vitest + React Testing Library for pure rules, preference/cue hooks, overlay, Options, and existing player/selector/track-mode suites. Co-located test-only `subtitleTestDoubles.ts` for fake media/TextTrack/cue readiness/events and stubbed rAF; fake timers as needed. Prefer failing tests before production edits (`web-implement` TDD). Deterministic timing invariants in Vitest without a new property library. jsdom does not prove real browser media behaviour. Separate required Chromium **and** Firefox real-browser acceptance with externally configured real movie-folder fixtures retaining `Subs\<video stem>-<code>.srt`; never hardcode machine paths in source. No new test framework/dependency. No explanatory comments beyond AGENTS exemptions.

**Target Platform**: Desktop Chromium and Firefox for acceptance receipts; SPA browser player inside movie details.

**Project Type**: SPA feature-folder UI change under existing `web/src/features/playback`.

**Performance Goals**: Instant delay/style application without track reload; interactive personal-library playback; no new SLA numeric target beyond usable overlay sync. No mandatory debounce without measured jank evidence.

**Constraints**: Charter §2.3 items 1–6 deliberately ruled (Q1/Q8). No new dependency/layer/schema/API/LocalPlay/secrets/Process.Start. Overlays inside media-chrome fullscreen subtree. Storage failures never break playback. Screenshot fidelity unwaived; provisional Q3 profile is drafting authority only. Palette design delivered with corrections pending. Never lower harness thresholds. Web gates via `scripts/run-web-gates.ps1`; scope-empty C# exits reported as SKIPPED (scope-empty), never PASS. Any .NET property-gate opt-out must be explicitly recorded by Conductor as `propertyTests: opt-out` with frontend-only rationale — not silently counted PASS. Gates precede browser receipts; receipt-driven code fixes require affected tests/gates rerun and receipts refreshed at resulting head.

**Scale/Scope**: Size M; frontend Options/timing/style within named playback files; three review axes and two remediation rounds maximum (`brief.md` Q1/Q9).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). UI-only scope; no owner structural checkbox.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **N/A / PASS** — no Core/Infrastructure/Api production edits; no new port. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no new commands/queries. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new public API/error contract; client-side preference validation only. |
| VI (Observability) | Safe diagnostics | **PASS** — no new telemetry names; receipts record fixture provenance without secrets. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, Process.Start, secrets, or server options-record changes; fixture roots external only; no hardcoded machine paths. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new domain term/ADR/CONTEXT edit; spec-local timing/style vocabulary only (`brief.md` Q10). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — Vitest/RTL for rules/interaction; required Chromium+Firefox runtime receipts for native fullscreen, real media, storage denial, native `hidden` proof, scale/motion/target/contrast, and screenshot fidelity unit tests cannot prove. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no contract generation; preserve existing generated-type usage; no hand-rolled DTO duplicates. |
| Governance / Static-Analysis Gates | Honest exits | Phase B: `scripts/run-web-gates.ps1` exit 0 when web in scope; C# scope-empty → SKIPPED (scope-empty); exit 1 / Could not run block; do not claim C# PASS for web-only diff. Closing bar and 2-round / 2-commit caps per brief Q1/Q9. |

### Complexity Tracking

No constitution violations requiring justification. New co-located playback modules (including test-only `subtitleTestDoubles.ts`) are planned sibling responsibilities under Q8 as amended, not a new architectural layer.

## Approach

- **Phase A (this draft)**: Quill writes/repairs spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT and `PLAN_ADJUDICATION.md`. Keel runs independent reread before PLAN FROZEN. Palette `design.md` is delivered; corrections pending (Palette-owned — Quill does not edit). Spec PR: Bernstein coordinates; Rigger prepares/opens; user merges; Keel records Gate 1 merge receipt. No Build work before Gate 1. This ask does not open/merge the PR or edit production code.
- **Phase B pickup (Build workspace, after Gate 1)**: Confirm DEV-503 merged to main; rebase/update worktree; Wisp refreshes Options-tab integration recon; Keel amends brief/tasks only for authorized demonstrated drift; obtain externally configured real-layout fixtures and ticket reference assets (availability unverified until obtained). STOP rather than duplicate shell or cherry-pick.
- **Dependency-ordered implementation (`brief.md` Q8 + plan-challenge amendment)**:
  1. Pure preferences/timing rules + tests (`subtitlePreferences.ts`, `subtitleTiming.ts` including shifted-interval/active-cue ownership, named `subtitleTiming.test.ts`; JSON-array identity codec; enum tokens).
  2. Shared overlay/media lifecycle + tests (`CaptionOverlay.tsx`, `useSubtitleCues.ts`, CSS; `subtitleTestDoubles.ts`; generation/identity guards; half-open/immutable baseline/boundary coverage; behavioral paused/unchanged-cue frame tests).
  3. Options/shortcut shell integration split into four serial seams with red→green progression: preference lifecycle/storage (`useSubtitleOptions`); presentational controls (`SubtitleOptions`, props-only); shell/shortcuts (`BrowserPlayer` single hook instance + delivered shell); loaded/`hidden` track wiring (`useSubtitlePlayback` / `subtitleTrackModes`). Same-file edits stay serial.
  4. Focused regression verifying existing DEV-503 selection/Off/retry plus new integration cases without duplicating seam test-writing responsibility.
  5. Applicable web/harness gates **before** browser receipts.
  6. Real-library Chromium **and** Firefox receipts + outstanding reference verification (native suppression, scale, motion, target-size, contrast); honest Could not run if evidence unavailable. Receipt-driven code fixes → rerun affected tests/gates and refresh receipts at resulting head.
- **media-chrome**: Keep existing controller; mount shared overlay inside fullscreen subtree; keep selected native track `mode = 'hidden'`; no new player library.
- **Persistence**: Immediate best-effort; no mandatory debounce; no cross-tab listener required; preserve abort/object-URL cleanup with existing owners.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-500/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── PLAN_ADJUDICATION.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── DRAFTING_RECEIPT.md
├── checklists/requirements.md
└── design.md            # Palette-owned (delivered; corrections pending; not authored by Quill)
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; UI-only; no API contract delta.

**Design binding**: Ticket labels/help and Q3/Q7 presentation rules are authoritative for behaviour. Responsive layout, caption scale/spacing, and exact shadow geometry come from Palette `design.md` (delivered; corrections pending) within those bounds. Screenshot fidelity remains outstanding. Do not treat design as absent; do not claim corrections complete.

### Source Code (approved boundary — Q8 / plan-challenge amendment / recon integration points)

```text
web/src/features/playback/
├── BrowserPlayer.tsx              # single useSubtitleOptions instance; mount overlay; integrate preferences, media, G/H shortcuts; props to Options/overlay
├── SubtitleSelector.tsx           # Options integration in delivered DEV-503 CC shell; preserve selection/Off/retry
├── useSubtitlePlayback.ts         # only as needed for loaded/hidden selected-track integration
├── subtitleTrackModes.ts          # only as needed; selected track mode = 'hidden'
├── subtitleCopy.ts                # ticket labels/help and settled preview copy
├── subtitlePreferences.ts         # NEW — defaults, validation, JSON-array storage codec/helpers
├── subtitleTiming.ts              # NEW — pure baseline/shifted half-open intervals/active-cue rules (green owner for cue-interval tests)
├── useSubtitleOptions.ts          # NEW — sole pair/global preference lifecycle owner
├── useSubtitleCues.ts             # NEW — media synchronization, generation/identity guards, cleanup
├── CaptionOverlay.tsx             # NEW — shared safe plain-text renderer (props-only; no storage/media access)
├── SubtitleOptions.tsx            # NEW — timing and style controls (props-only; no storage/media access)
├── captionOverlay.css             # NEW — overlay styles within Palette design
├── subtitleOptions.css            # NEW — Options styles within Palette design
├── subtitleTestDoubles.ts         # NEW — co-located TEST-ONLY media/TextTrack/rAF helpers
├── BrowserPlayer.test.tsx         # extend integration/regression
├── subtitleSelector.test.tsx      # extend shell/Options regression
├── subtitleTrackModes.test.ts     # extend loaded/hidden mode proof
├── subtitleTiming.test.ts         # NEW — named pure timing + cue-interval tests
└── corresponding new *.test.ts(x) # preferences, hooks, overlay, Options

Read-only / out of bounds: backend, API, DTO, OpenAPI, generated `web/src/api/types.ts` hand-edits, package manifests, harness.yml, subtitle file layout, unrelated rewrites/deletions. Any additionally necessary edit requires a cited brief amendment before work.
```

**Structure Decision**: Stay inside existing playback feature folder with Q8-named sibling modules plus approved test-only helper. Single preference-hook instance; props-only renderers. No new top-level folder, project, or architectural/state layer. If refreshed DEV-503 recon changes shell ownership, amend brief explicitly before tasks change.

## Test Strategy

| Family | Required evidence |
|---|---|
| Vitest — timing | Normal/Shift steps, sign convention, reset isolation, deterministic invariants |
| Vitest — identity codec | Delimiter/Unicode/case/empty/long track IDs; round-trip/pair separation; numeric movie normalization; reorder/reload with same IDs; no distinct keys for equivalent numeric forms |
| Vitest — shortcuts | Playing-only; input (incl. range/color)/textarea/select/contentEditable/dialog focus guards; modifier guards; unusable track |
| Vitest — persistence | Reload, saved-pair restore, new-pair zero, global style, stale-switch races, generation/identity + deferred out-of-order resolution, immediate best-effort (no debounce requirement) |
| Vitest — storage failure | Invalid JSON/profile/values; denied access/read/write/remove/quota; lazy accessor-getter throws; defaults; session adjustments; uninterrupted playback |
| Vitest — cues/overlay | Copied immutable baseline; half-open boundaries; long ±100 ms sequences; large-delay empty overlay; no cumulative drift; negative overlap; seek/pause/play/rate/late readiness; no paused frame rescheduling; no observable unchanged-cue updates; multiple cues; cleanup of acquired resources; literal markup/attack vectors; rapid switch; StrictMode remount |
| Vitest — styles | Every control in preview and live shared rendering, independent opacity, five edges, line breaks, reset; enum tokens |
| Vitest/RTL — integration | Selected track `mode = 'hidden'`; Off; preserved selection/retry; no timing/style reload/refetch; fullscreen containment; no duplicate captions; single hook + props-only wiring |
| Test doubles | `subtitleTestDoubles.ts` fake readiness/events; fake timers/stubbed rAF; do not claim jsdom proves real media |
| Chromium + Firefox acceptance | Real-layout fixtures with `Subs`; browser/version; commit; path; hotkeys; reload/pair isolation; independent resets; every style including fullscreen; blocked storage; uninterrupted playback; native suppression proof; responsive scale; motion/no-transition; target-size; panel contrast; screenshots; reference inspection before fidelity claims |
| Gates | `scripts/run-web-gates.ps1` exit 0 when web in scope **before** receipts; honest SKIPPED/SKIP/N/A/Could not run reporting; receipt-driven fixes → rerun affected tests/gates and refresh receipts |

Unit/component assertions cannot prove native fullscreen, real media cue loading, cross-browser presentation, native suppression, or screenshot fidelity — both browser receipts remain required (`brief.md` Q9). Preserve existing abort/blob URL cleanup ownership; do not add duplicate fetch/cleanup ownership.

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q10 and the plan-challenge amendment. No open inventable-behaviour `needs decision` items for this repair. No data-model or contracts artifacts for this UI-only ticket. Outstanding stage prerequisites remain: Palette design corrections (pending); independent Keel reread for PLAN FROZEN; DEV-503 pickup refresh; reference inspection; implementation/gates; both real-browser receipts. Do not silently convert unavailable backend/restoration facts into proven assumptions.

## Post-design Constitution Check

Re-checked after documenting the UI-only structure above: still PASS/N/A as in the Constitution Check table; no new port, handler, schema, API, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan.
