# Implementation Plan: Web player volume boost above 100%

**Branch**: `feature/569-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-569-spec`) | **Date**: 2026-11-24 | **Spec**: `specs/DEV-569/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7 + Plan challenge decisions); `CONCLUSIONS.md` (Plan challenge ruling and Keel dispositions); `ASSUMPTIONS.md`; Maestri notes `DEV-569`, `recon-DEV-569`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Quill Spec Kit plan + plan-challenge repair round 1 applied. Gate 1 remains **pending**; zero structural owner checkboxes. PLAN FROZEN authority is the future Keel freeze receipt in `CONCLUSIONS.md` only — this draft does not declare freeze. Implementation and live Chromium acceptance remain pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill Q1–Q7 freeze inventable unknowns; no schema or public API change.

## Summary

Add a 100–300% Volume boost control beside the stock Media Chrome volume slider in the existing browser player. For streams matching the verified direct same-origin HTTP(S) library delivery path with Web Audio available, prepare gain → compressor → destination before attaching a once-per-element media-element source on explicit activation after resume; keep native playback before activation and when ineligible/unverified. Remember the setting in one namespaced browser-local key (following `subtitlePreferences.ts` pattern without editing that file) with pending-activation UI above 100%. Verify with web tests (including failure stages and same-media continuity), calibrated analyser evidence, and a real-library Chromium receipt via temporary Test URLs portal session-only evaluation. Stay inside the five-file playback envelope with no new dependencies, sibling modules, or LocalPlay changes.

## Technical Context

**Language/Version**: TypeScript / React 19 / Vite (`web/`) per `specs/PRODUCT.md` §1. No C# production changes expected.

**Primary Dependencies**: Existing Media Chrome player controls, existing Vitest/RTL web test tooling, browser Web Audio API already available in Chromium. No new npm/NuGet dependency (`brief.md` Q6; `PRODUCT.md` §5 item 1).

**Storage**: Browser-only `localStorage` (one namespaced key). No account/backend persistence. No database/migration change.

**Testing**: Existing `web/src/features/playback/BrowserPlayer.test.tsx` with controllable AudioContext doubles and module-reload same-element reinitialization; Chromium calibrated/real-library receipts via temporary Test URLs portal session-only evaluation (not jsdom as receipt driver; no production measurement hooks). No new test framework.

**Target Platform**: Browser player in Compose/local web app; Chromium for origin proof and acceptance receipts; windowed and fullscreen.

**Project Type**: Existing SPA feature under `web/src/features/playback/`; no API/contract chain work.

**Performance Goals**: Boost graph is a short audio chain on one media element; no invented latency SLA.

**Constraints**: Charter §2.3 care-list items deliberately ruled out by Q6 (no dependency/layer/schema/API/LocalPlay/secrets/`Process.Start`). Public behaviour frozen by ticket Scope/Acceptance. Ordinary owner Gate 1; zero structural owner checkboxes. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar includes Critical–Low in-scope; max two review/fix rounds (`brief.md` Q7). Honest gate labels; never lower thresholds.

**Scale/Scope**: Size M; five authorized playback files; parent DEV-100.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft (`PRODUCT.md` §5; brief Q7).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **N/A** — web-only player UI/audio wiring; no new Core port or C# layer (out of scope for this ticket). |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no application/domain use case; browser-player control only (out of scope). |
| III (Typed query model) | MovieQuery | **N/A** — no browse/filter change. |
| IV (Domain transitions) | Movie mutators | **N/A** — no Movie mutation (out of scope). |
| V (Validated inputs / errors) | ProblemDetails | **N/A** — no new HTTP surface; invalid storage defaults to 100% in-browser (out of scope for backend ProblemDetails). |
| VI (Observability) | Safe diagnostics | **PASS** — no secrets; Chromium measurement is temporary Test URLs portal session-only evaluation only; zero production measurement hooks, exports, or globals. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — browser-local preference only; no Options/IConfiguration work; **do not touch** LocalPlay execution path (`Features:LocalPlay` remains untouched). No secrets/`Process.Start`. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — extend existing Vitest/RTL player tests; Chromium receipts via Test URLs portal for acceptance evidence. |

### Stack / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs (OpenAPI chain) | **N/A** — no OpenAPI/DTO/TypeScript contract-chain change (out of scope). |
| Static-analysis gates (C#) | Preserve applicable Phase 3 step 5 C# gates scoped to **changed `.cs` files**. When that scope is empty, record `SKIPPED (scope-empty)` honestly — never as PASS, and never infer a skip without running the gate. |
| Web gates | Required: `scripts/run-web-gates.ps1` exit 0 on the implementation diff (Phase 3 step 5 / Phase 5 applicable web gate). |
| Mutation | Preserve applicable Phase 3 step 5 mutation gate scoped to **production C# under `src/`**. When none changed, expect script exit 2 `SKIPPED (scope-empty)` — report that label only, never as PASS. When every changed project is listed in `gates.mutation.exclusions`, the script text is `NOT APPLICABLE`; report the recorded verdict as **N/A** (never PASS). Do not conflate `SKIPPED (scope-empty)`, exclusions **N/A**, or Constitution **N/A**. |

### Complexity Tracking

No constitution violations requiring justification. No new architectural layer or dependency.

## Approach

- **Phase A (this draft)**: Quill writes `spec.md` / `plan.md` / `tasks.md` / quality checklist / receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT only; plan-challenge repair round 1 applies clarifications (1)–(4). `before_specify` grill-with-docs already satisfied by closed grill artifacts. Keel owns analyze / freeze receipt; Quill does not declare PLAN FROZEN. Spec PR: Bernstein coordinates; Rigger prepares/opens; user merges; Keel records Gate 1 merge receipt. No Build work before Gate 1.
- **Phase B pickup**: Confirm authorized file/test seams against pickup HEAD (read-only). Pass **both** proofs before T002+: (1) origin/delivery-invariant — authoritative route/config recon establishing the existing direct same-origin HTTP(S) library delivery invariant, separately corroborated by live Chromium real-media and Range/retry request/redirect chains via the Test URLs portal; (2) portal session-only measurement capability on controlled media, including an independent unbound native reference. A same-origin `src`/`currentSrc` string or successful sample alone is insufficient. Any missing proof or recorded blocker **STOP**s Build. Keep native fallback for ineligible/unverified streams in implementation and tests.
- **Task order (`brief.md` Task order / Q3–Q6 / Plan challenge decisions; `web-implement` test-first; task-pipeline Phase 3 step 2 sequential phase dispatch)**:
  1. Gate 1, then Phase B recon requiring **both** delivery-invariant/Chromium corroboration **and** portal measurement capability (including unbound native reference) to pass; any missing proof or blocker stops Build.
  2. Establish failing web tests first (`BrowserPlayer.test.tsx`) for gain/activation; controllable AudioContext doubles; distinct repeated-activation, re-render, remount, and same-actual-element module-reload checks (module-reload simulates actual hot-reload contract; one binding, no duplicate connections, retained live context); fail-closed eligibility including matching URL with unverified/cross-origin effective delivery (hidden + native fallback); failed/invalidated attempt on still-live unbound element (keep that element); stale completion against changed/detached element (attach to neither; do not replace current element); partial/uncertain ownership including uncertain source-creation throw (fresh element, no bind on that attempt); previously bound ineligible transition with same-media continuity; focus/open-alone no-op; existing keyboard exclusion for focused native range (1pp arrows without player shortcut duplication; other range keys keep native semantics; existing hotkeys at usual targets); mute/volume multiplication; then persistence/pending activation following `subtitlePreferences.ts` pattern; then Phase 4 re-run of failure/continuity after persistence without reauthoring.
  3. Implement inside `useBrowserPlayer.ts` / `BrowserPlayer.tsx` (+ `playerCopy.ts` / `BrowserPlayer.css` only as needed): **fail-closed Web Audio + verified delivery-path eligibility in the same executable phase as (and before or with) first graph binding/control**; prepare/connect gain → compressor → destination before source attachment; bind source only after resume succeeds and the runtime predicate still holds (re-checked on source change and immediately before bind); once-per-element across repeated activation, re-render, remount, and actual hot reload; failed/invalidated unbound attempt → keep that element; stale completion → attach to neither, do not replace current element; partial/uncertain ownership or previously bound ineligible → fresh native element with Media Chrome attach/detach, stock controls, subtitle wiring reuse and same-media continuity; unity/limiting, reset, persistence, pending-activation indication. Do not prescribe React key, guard representation, or external helper exports. Preserve the approved five-file envelope; no new sibling module (Ledger F3).
  4. Via temporary Test URLs portal session-only evaluation (Chromium): install temporary portal observer before activation; connect analyser to the real compressor output when the graph is created on activation; capture standalone unbound native reference without binding; record gain values and RMS ratios at 100% and 200%; check audible unity as well as measured unity (SC-001); loud-signal limiting; mute; reset; real-library 200% screenshot + analyser reading + fullscreen/windowed check; Chromium SMOKE-002/SMOKE-003 evidence; clean up afterwards. Never put Chromium instrumentation in a shipping module; never treat jsdom as the receipt driver. Record blockers distinctly from passed checkpoints.
  5. Run applicable Phase 3 step 5 and Phase 5 gates explicitly (do not infer skips from file scope): require web gates exit 0; verify implementation diff has no production measurement hooks/exports/globals; record static-analysis against changed `.cs` files and mutation against production C# under `src/` with honest `SKIPPED (scope-empty)` vs exclusions verdict **N/A** (script text `NOT APPLICABLE`); Size M three-axis review under closing bar (max two rounds).
- **Do not**: add dependencies or sibling modules; touch LocalPlay execution; ship production measurement hooks/exports/globals; invent a second graph topology; claim Gate 1 / PLAN FROZEN / runtime PASS from this draft; lower gate thresholds; bind Web Audio before eligibility/recovery safeguards land; remount a fresh element when ownership was demonstrably never established; use speculative runtime probes or new APIs for eligibility.

## Verification Approach

- Web tests (`BrowserPlayer.test.tsx`, `web-implement` red→green): gain mapping 1/2/3; reset → unity; no bind before activation; controllable AudioContext doubles; distinct **repeated activation**, **re-render**, **remount**, and **same-actual-element module-reload** checks (module-reload simulates the actual hot-reload contract; one binding, no duplicate connections, retained live context); focus/open-alone never activates; existing keyboard exclusion for focused native range (1pp arrows without player shortcut duplication; other range keys keep native semantics; existing hotkeys at usual targets); hidden boost + native playback when Web Audio missing or delivery path ineligible/unverified — including a matching URL string with unverified or cross-origin effective delivery; **failed/invalidated attempt on still-live unbound element** → keep that element, no source attached; **stale completion against changed/detached element** → attach to neither, do not replace the current element; **partial/uncertain ownership** (including uncertain source-creation throw) → fresh native element, no bind on that attempt; **previously bound ineligible transition** → fresh native element with same-media continuity (seek when seekable after metadata, pause/play intent within browser policy, stock volume/mute, rate, caption selection/delay) via reused Media Chrome attach/detach, stock controls, and subtitle wiring; genuinely different media follows existing source-change semantics; storage restore/pending/invalid/throw following `subtitlePreferences.ts` namespacing/error-isolation pattern without editing that file; Phase 4 re-runs failure/continuity and US2+US3 interaction after persistence without reauthoring Phase 2 cases; stock mute/volume multiplication. Phase 2 owns the failing-first coverage and pass bar for both failure stages and continuity.
- Chromium calibrated check (temporary Test URLs portal session-only evaluation — never a shipping module; never jsdom as receipt driver):
  1. **Capability**: T001 must pass portal session-only evaluation on controlled media, including an independent unbound native reference; missing proof or recorded blocker stops Build.
  2. **Before results**: record the test tolerance that will judge Q2 outcomes (same-level at 100% vs native; about 2× at 200% vs 100%) without changing Q2 bounds.
  3. **Temporary observer**: install the temporary portal observer **before** explicit activation; connect the analyser to the **real compressor output when the graph is created on activation**; clean up afterwards.
  4. **Stimulus**: same calibrated quiet signal below compression onset; fixed stock volume; stabilized measurement window; RMS amplitude from the compressor-output connection.
  5. **Native reference**: capture a standalone unbound native baseline without binding (no `createMediaElementSource` on that reference element).
  6. **After explicit activation**: record **gain values** and **RMS ratios** at active 100%, reset 100%, and 200% on the same stimulus/volume/window; check **audible unity** as well as measured unity (SC-001); retain raw measurements and ratios; capture before/during boost and mute readings; demonstrate limiting on loud material; confirm mute silences boosted audio.
  7. Pass only when retained ratios meet the pre-declared tolerance under Q2 and audible unity holds at 100%.
- Chromium smoke evidence (Test URLs portal, controlled session-only cases): SMOKE-002 reset-to-100% receipt; SMOKE-003 ineligible/unavailable hidden control with stock playback unchanged. Blockers are recorded distinctly from passed checkpoints. Phase 6 delivery checkpoint requires T015 smoke **pass** evidence.
- Real-library receipt (Test URLs portal session-only): play real movie; install temporary portal observer before activation; connect analyser to real compressor output when the graph is created on activation; boost to 200%; screenshot control; capture analyser reading; check fullscreen and windowed; preserve evidence for review; clean up temporary instrumentation.
- Gates: run applicable Phase 3 step 5 / Phase 5 gates; `scripts/run-web-gates.ps1` exit 0; T014 verifies implementation diff has **no** production measurement hooks, exports, or globals; static-analysis scoped to changed `.cs` files; mutation scoped to production C# under `src/`; record `SKIPPED (scope-empty)` when that gate’s scope is empty; for mutation exclusions report verdict **N/A** (script text `NOT APPLICABLE`) — never fold either into PASS; never infer a skip without running the gate.
- Review: Sentry/Ledger/Compass; explicit dispositions; independent reviewed-head verification; two-round cap; follow-up routing per brief Q7.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-569/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill decisions freeze Spec Kit research unknowns; no schema or public API.

### Source Code (authorized envelope — brief Approach; ticket Scope)

```text
web/src/features/playback/
├── BrowserPlayer.tsx        # control row; eligibility/activation/reset/source transitions; existing keyboard exclusion
├── useBrowserPlayer.ts      # element-bound graph/gain; pre-source node prep; unbound vs fresh-element recovery; local remembered state
├── BrowserPlayer.test.tsx   # graph lifecycle, failure/continuity, storage, interaction, controllable AudioContext doubles
├── playerCopy.ts            # Volume boost / Reset boost / pending-activation copy as needed
└── BrowserPlayer.css        # placement/focus styling only as needed
```

**Structure Decision**: Extend the existing playback feature seam only. No new project, folder, dependency, sibling module, or API contract files (Ledger F3 — no repair requiring a new module). Exact localStorage key string and compressor parameter numbers remain within Q2/Q4 behavioural bounds and are chosen at Build inside this envelope without expanding file scope. Exact React key, guard representation, and external helper exports are Build choices. Chromium measurement stays in temporary Test URLs portal session-only evaluation, not in these shipping modules. STOP for cited Patron ruling before any out-of-envelope edit.

## Phase 0 / Phase 1 Spec Kit outputs

- **research.md**: omitted — Q1–Q7 resolve inventable unknowns.
- **data-model.md**: omitted — no schema entities.
- **contracts/**: omitted — no public HTTP/API contract.
- **quickstart.md**: omitted — verification lives in brief Test strategy and tasks.
