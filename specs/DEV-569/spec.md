# Feature Specification: Web player volume boost above 100%

**Feature Branch**: `feature/569-spec`

**Created**: 2026-11-24

**Status**: gate1: provisional

**Input**: Ticket DEV-569 (parent DEV-100; size M); grill-closed `brief.md` (Q1–Q7 + Plan challenge decisions); `CONCLUSIONS.md` (Plan challenge ruling and Keel dispositions); `ASSUMPTIONS.md`; Maestri notes `DEV-569`, `recon-DEV-569`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Grill answers / Plan challenge decisions; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox. Live Chromium delivery-invariant / origin proof, portal measurement capability, and Build remain Phase B work.

## Clarifications

### Session 2026-11-24

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Control placement and interaction (adjacent boost range + reset), lazy Web Audio graph and unity/limiting rules, eligibility and once-per-element lifecycle, remembered localStorage behaviour with pending-activation indication, file envelope, evidence matrix, and Size M review bar are settled in `brief.md` Q1–Q7 and `CONCLUSIONS.md`. Plan-challenge clarifications (1)–(4) in `brief.md` Plan challenge decisions and `CONCLUSIONS.md` Plan challenge ruling refine eligibility predicate, pre-source node preparation / unbound vs fresh-element recovery with same-media continuity, Test URLs portal session-only compressor-output measurement, and existing keyboard/storage/lifecycle conventions. Taste items tagged `[assumed]` in `ASSUMPTIONS.md` are Patron-ratified. Conductor asked Quill not to run `/speckit-clarify`. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: Browser movie player control row, beside the stock Media Chrome volume slider (`BrowserPlayer`), in both windowed and fullscreen playback.
- **What the user sees or does**: A **Volume boost** range (100–300%) with the current percentage visible, plus a **Reset boost** control that returns to 100% in one action. Stock volume and mute keep working; boost multiplies on top so mute still silences. When a remembered boost above 100% is restored after reload, the UI shows that value and clearly indicates that activation is still pending until the user explicitly uses the boost control. If Web Audio is unavailable or the active media does not match the verified direct same-origin HTTP(S) library delivery path, the boost controls are hidden and normal playback is unchanged.
- **What it needs to show up**: Default Compose stack with the browser player able to play a library stream covered by the verified direct same-origin HTTP(S) delivery invariant in Chromium. Ineligible or unverified streams keep native playback without the boost UI.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** an eligible library movie (verified direct same-origin HTTP(S) delivery path) playing in the browser player with boost visible, **When** the viewer sets Volume boost to `200%` (or activates a remembered pending `200%`), **Then** the control row shows `200%`, playback continues, and stock mute still silences the audio.
- **SMOKE-002**: **Given** boost above 100% is active, **When** the viewer chooses Reset boost, **Then** the displayed value returns to `100%` in one action and playback continues at unity boost.
- **SMOKE-003**: **Given** Web Audio is unavailable or the media is ineligible/unverified (including a matching URL string whose effective delivery is unverified or cross-origin), **When** the viewer opens the browser player, **Then** no boost controls appear and stock volume/mute behaviour is unchanged.

## Scope

**In scope** (`brief.md` Frozen scope / Approach and files / Test strategy; Q1–Q7; ticket Scope and Acceptance at `recon-DEV-569:39–57`):

1. **Boost control**: One boost control beside the stock Media Chrome volume slider; range 100–300%; visible current percentage; one-action reset to 100%; stock volume/mute preserved; player hotkeys preserved; windowed and fullscreen supported.
2. **Web Audio graph**: For eligible streams, prepare and connect gain → compressor → destination before source attachment; lazily attach `createMediaElementSource` on explicit activation after resume; native playback before activation and for ineligible streams; bind once per actual element across repeated activation, re-render, remount, and actual hot reload (module-reload of the same actual element is the test simulation of that contract); no duplicate connections.
3. **Unity and limiting**: At 100% (including reset) gain is 1 with a transparent compressor; limiting engages only above 100%; verify audible and measured native vs active unity and about 2× amplitude at 200% on a calibrated quiet signal below compression onset.
4. **Eligibility and lifecycle**: Hide boost when Web Audio is unavailable or the active element/source generation does not match the verified direct same-origin HTTP(S) library delivery path (re-checked on source change and immediately before bind after resume); start/resume context on explicit click or keyboard activation; bind only after resume succeeds; keep a live bound context open; failed/invalidated attempt on a still-live unbound element keeps that element; stale completion against a changed/detached element attaches to neither and does not replace the current element; partial/uncertain ownership or previously bound ineligible transition recovers on a fresh native element with same-media continuity.
5. **Remembered setting**: One namespaced browser-only localStorage key following the existing `subtitlePreferences.ts` namespacing/error-isolation pattern without editing that file; restore valid 100–300% immediately in the UI; indicate pending activation when remembered value is above 100%; audio stays native until explicit activation; apply remembered value on activation unless that gesture adjusts or resets; default missing/malformed/nonfinite/out-of-range to 100%; failed reads/writes leave playback and in-session adjustment usable.
6. **Delivery evidence**: Web tests for gain, activation, persistence, interaction, eligibility, failure stages, and same-media continuity; Chromium analyser measurement via temporary Test URLs portal session-only evaluation (install temporary portal observer before activation; connect analyser to the real compressor output when the graph is created on activation; standalone unbound native reference; cleanup afterwards; zero production measurement hooks/exports/globals); real-library Chromium receipt (200% screenshot + analyser reading; fullscreen and windowed); `scripts/run-web-gates.ps1` exit 0; honest `SKIPPED (scope-empty)` for applicable C# gates.

**Out of scope**:

- Palette `design.md` / any `ui:` tag change.
- New NuGet/npm dependency; new project/top-level folder/architectural layer; database schema; public API/contract changes; account or backend persistence; secrets hardcoding; `Process.Start`; changes to `Features:LocalPlay` execution path.
- Shipping measurement instrumentation in production UI.
- Anything else is a follow-up issue, not a finding in this round (charter threshold before filing).

**Precedence**: Ticket Scope / Acceptance (`recon-DEV-569:39–57`) are approved authority (`PRODUCT.md` §3). Brief Q1–Q7, Plan challenge decisions, and `CONCLUSIONS.md` (including Plan challenge ruling) govern plan boundaries. Quill invents no decisions. PLAN FROZEN authority is the future Keel freeze receipt in `CONCLUSIONS.md` only.

**Dependencies**: None beyond the existing browser player. Phase B (T001) must pass **both** (1) origin/delivery-invariant proof — authoritative route/config recon establishing the existing direct same-origin HTTP(S) library delivery invariant, corroborated by live Chromium real-media and Range/retry request/redirect chains via the Test URLs portal — **and** (2) portal session-only measurement capability on controlled media including an independent unbound native reference. A same-origin `src`/`currentSrc` string or a successful sample alone is insufficient. Any missing proof or recorded blocker stops Build; T002+ does not unlock until both proofs pass.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Volume boost**
Viewer-facing percentage from 100 through 300 that scales browser-player audio above the stock volume ceiling after explicit activation.
_Avoid_: changing `HTMLMediaElement.volume` past 1.0; replacing Media Chrome mute/volume

**Unity boost**
Boost at 100% (including after reset): no audible or measurable extra processing versus native playback at the same stock volume.
_Avoid_: leaving limiting engaged at 100%; treating UI 100% as still-active processing

**Pending activation**
UI state after reload when a remembered boost above 100% is shown but the audio graph is not yet attached; audio remains native until explicit boost-control activation.
_Avoid_: attaching the graph on focus/open alone; applying boosted audio before activation

**Eligible stream**
Media whose active element and source generation match the existing verified direct same-origin HTTP(S) library delivery invariant (established by T001 from route/config plus Chromium real-media and Range/retry request/redirect chains) while Web Audio is available. Runtime eligibility is that compound predicate, re-checked on source change and immediately before binding after resume. A same-origin `src`/`currentSrc` string alone, or a successful sample alone, is insufficient. Ineligible/unverified includes malformed URLs, unsupported schemes, indirect/blob/MSE/segmented paths outside the verified class, and a matching URL string whose effective delivery is unverified or cross-origin.
_Avoid_: treating a same-origin URL string as proof of delivery; attaching a media-element source to ineligible/unverified media; leaving a bound graph on an ineligible replacement source; inventing a speculative runtime probe or new API

**Demonstrably unbound failure**
Failed or invalidated activation/resume on a still-live unbound element where no media-element source attachment can be ruled in — keep that same native element; no source attached; native playback usable.
_Avoid_: remounting a fresh element when ownership was never established; attaching after a failed/invalidated attempt

**Stale completion**
Resume or bind completion that arrives against a changed or detached element — attach to neither the old nor the new element, and do not replace the current element because of that stale completion.
_Avoid_: treating stale completion as grounds to remount; attaching to a changed/detached element

**Partial or uncertain ownership**
Bind attempt where source creation or connection leaves attachment ownership partial or uncertain (including an uncertain source-creation throw) — recover on a fresh native element without binding the replacement on that attempt.
_Avoid_: treating uncertain ownership as safe to keep on the same element; binding the replacement during the recovery attempt

Consumed vocabulary (already in use; not redefined): Media Chrome / `MediaVolumeRange`, browser player, LocalPlay (untouched), Test URLs portal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Boost quiet films above 100% (Priority: P1) 🎯 MVP

A viewer watching in the browser player can raise boost above 100%, see the percentage, reset to 100% in one action, and keep stock mute/volume and hotkeys working in windowed and fullscreen modes.

**Why this priority**: This is the ticket’s primary user value and the core acceptance path.

**Independent Test**: Web tests prove 100%/200%/300% map to gain 1/2/3, reset restores unity, no source is bound before activation or after demonstrably unbound failure, stale completion attaches to neither element and does not replace the current element, controllable AudioContext doubles cover resume/bind paths, and distinct checks for repeated activation, re-render, remount, and same-actual-element module-reload simulation (the test stand-in for actual hot-reload contract) keep one binding with no duplicate connections and a retained live context, focused native range adjustment uses the existing keyboard exclusion without player shortcut duplication, and mute silences boosted audio. Stock volume remains an input that boost multiplies.

**Acceptance Scenarios**:

1. **Given** an eligible playing movie with boost visible at 100%, **When** the viewer raises Volume boost to 200%, **Then** the UI shows `200%` and boosted audio is active after that explicit activation.
2. **Given** boost above 100% is active, **When** the viewer chooses Reset boost, **Then** the value returns to `100%` in one action and audio returns to unity boost.
3. **Given** the boost native range is focused, **When** the viewer adjusts it with arrow keys, **Then** boost changes in 1 percentage-point steps via the existing player keyboard exclusion without also firing a player shortcut; other native range navigation keys keep native range semantics; Space/K, arrows, M, and F keep their usual scope at their usual targets elsewhere.
4. **Given** boost above 100% is active, **When** the viewer mutes via stock mute, **Then** audio is silent; unmuting restores boosted playback with stock volume still applied underneath.
5. **Given** the player is toggled between windowed and fullscreen, **When** boost controls are used, **Then** the same boost affordances remain available inside the existing fullscreen-enabled player.

---

### User Story 2 - Remember boost on this device (Priority: P1)

After reload, a valid remembered boost returns in the UI immediately; audio stays native until explicit activation; pending activation above 100% is clearly indicated.

**Why this priority**: Acceptance requires reload restoration without breaking autoplay policy or playback on storage failure.

**Independent Test**: Web tests restore a valid setting with pending activation above 100%, apply it on explicit activation (unless the gesture sets a new value or resets), default invalid storage to 100%, and keep playback/in-session adjustment usable when storage throws.

**Acceptance Scenarios**:

1. **Given** a valid remembered boost of 150% in browser-local storage, **When** the player reloads, **Then** the UI shows `150%` with a clear pending-activation indication and audio remains native until the boost control is explicitly used.
2. **Given** pending activation at 150%, **When** the viewer explicitly activates without changing the value, **Then** boost applies at 150%.
3. **Given** pending activation at 150%, **When** the viewer’s activation gesture sets a new percentage or resets to 100%, **Then** that gesture’s value is applied instead of blindly re-applying 150%.
4. **Given** missing, malformed, nonfinite, or out-of-range stored data, **When** the player loads, **Then** boost defaults to 100%.
5. **Given** storage read or write throws, **When** the viewer plays and adjusts boost in-session, **Then** playback and in-session adjustment remain usable.

---

### User Story 3 - Hide boost when unsafe or unavailable (Priority: P1)

Boost stays hidden when Web Audio is unavailable or the active media does not match the verified direct same-origin HTTP(S) library delivery path; native playback is preserved; failed/invalidated attempts on a still-live unbound element keep that element; stale completion against a changed/detached element attaches to neither and does not replace the current element; partial/uncertain ownership and previously bound ineligible transitions recover on a fresh native element with same-media continuity.

**Why this priority**: Ticket Scope forbids silent audio from unsafe Web Audio attachment and requires untouched playback when boost cannot run.

**Independent Test**: Phase 2 owns failing-first coverage and the pass bar for both failure stages and same-media continuity. Web tests cover missing Web Audio; ineligible/unverified paths including a matching URL string with unverified or cross-origin effective delivery (hidden boost, native fallback); failed/invalidated attempt on a still-live unbound element (keep that element, no source); stale completion against a changed/detached element (attach to neither; do not replace the current element due to the stale completion); partial/uncertain ownership including uncertain source-creation throw (fresh native element, no bind on that attempt); previously bound ineligible transition with same-media continuity (seek when seekable after metadata, pause/play intent within browser policy, stock volume/mute, rate, caption selection/delay) via reused Media Chrome attach/detach, stock controls, and subtitle wiring. Phase 4 re-runs these after persistence without reauthoring them.

**Acceptance Scenarios**:

1. **Given** Web Audio is unavailable, **When** the player renders, **Then** boost controls are hidden and stock playback is unchanged.
2. **Given** the active media is ineligible or unverified — including malformed/unsupported schemes, indirect/blob/MSE/segmented paths outside the verified class, or a matching URL string whose effective delivery is unverified or cross-origin — **When** the player renders or the source generation changes, **Then** boost controls are hidden and stock playback remains native.
3. **Given** the viewer activates boost but the attempt fails or is invalidated while the element is still live and unbound, **When** that activation attempt finishes, **Then** no media-element source is attached and that same native element/playback remains usable.
4. **Given** a stale resume/bind completion arrives against a changed or detached element, **When** that completion is handled, **Then** no source attaches to either element and the current element is not replaced because of that stale completion.
5. **Given** source creation or connection leaves ownership partial or uncertain (including an uncertain source-creation throw), or an element already bound to the boost graph becomes ineligible, **When** recovery runs, **Then** playback continues via a fresh native video element without binding the replacement on that attempt, reusing existing Media Chrome attach/detach, stock controls, and subtitle wiring; for the same media, seek (when seekable after metadata), pause/play intent (within browser autoplay policy), stock volume/mute, playback rate, and caption selection/delay carry over as applicable; a genuinely different media item follows existing source-change semantics.

---

### User Story 4 - Prove unity, 200% level, and real-library receipt (Priority: P2)

Delivery collects calibrated analyser evidence and a real-library Chromium receipt before clearance.

**Why this priority**: Acceptance criteria require measurable unity/200% behaviour and a real-movie Chromium screenshot plus analyser reading; these close the ticket but follow the interactive stories.

**Independent Test**: Temporary Test URLs portal session-only evaluation installs a temporary portal observer before activation, connects the analyser to the real compressor output when the graph is created on activation (cleaned up afterwards), and captures a standalone unbound native reference without binding; compares audible and measured native baseline to active/reset 100% at fixed stock volume; records gain values and RMS ratios at 100% and 200% on a calibrated quiet signal below compression onset; demonstrates limiting on loud material above unity; confirms mute silences boosted audio. Real-library Chromium receipt uses the same portal session-only technique. jsdom tests are not the Chromium receipt driver. Production modules carry no measurement hooks, exports, or globals.

**Acceptance Scenarios**:

1. **Given** fixed stock volume and a calibrated quiet signal below compression onset, with a temporary portal observer installed before activation and the analyser connected to the real compressor output when the graph is created on activation, **When** active or reset boost is at 100%, **Then** audible sound and measured RMS match the unbound native baseline (unity per SC-001).
2. **Given** the same calibrated quiet signal and fixed stock volume, **When** boost is 200%, **Then** recorded gain is 2 and RMS amplitude is about double the 100% level.
3. **Given** loud material above unity with boost above 100%, **When** limiting is engaged, **Then** hard clipping is constrained by the compressor path (limiting demonstrated).
4. **Given** the Test URLs portal and a real library movie in Chromium, **When** boost is set to 200%, **Then** a screenshot of the visible control and an analyser level reading from the compressor-output connection are preserved, and fullscreen/windowed operation is checked.

---

### Edge Cases

- Explicit activation required — focus or opening the player alone must not attach the graph.
- Failed or invalidated attempt on a still-live unbound element — keep that same native element; leave native playback usable; no source attached.
- Stale completion against a changed/detached element — attach to neither element; do not replace the current element due to the stale completion.
- Partial/uncertain ownership (including uncertain source-creation throw) — recover on a fresh native element without binding the replacement on that attempt; reuse Media Chrome attach/detach, stock controls, and subtitle wiring; same-media continuity as FR-007.
- Previously bound element becomes ineligible — same fresh-element recovery and same-media continuity; genuinely different media follows existing source-change semantics.
- Matching URL string with unverified or cross-origin effective delivery — hide boost; native fallback; never bind.
- Runtime eligibility re-check on source change and immediately before bind after resume — Web Audio availability plus active element/source generation matching the verified direct same-origin delivery path; no speculative probe or new API.
- Repeated activation, re-render, remount, and actual hot reload (module-reload of the same actual element is the test simulation) — still one media-element source and graph; no duplicate connections; retained live context.
- Storage failure on read or write — playback and in-session boost adjustment still work; default UI safely when restore cannot be trusted (follow `subtitlePreferences.ts` error-isolation pattern without editing that file).
- Invalid stored values — treat as 100%.
- Boost hidden — stock Media Chrome volume/mute and hotkeys unchanged; focused native range uses existing keyboard exclusion without duplicating player shortcuts.
- LocalPlay path — untouched; boost work stays off the local-playback execution path.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Player MUST show a Volume boost range (100–300%, 1 percentage-point step) with visible percentage text, accessible percent valuetext, visible focus, and a one-action Reset boost control beside the stock Media Chrome volume slider inside the existing fullscreen-enabled control row (`brief.md` Q5; `ASSUMPTIONS.md`).
- **FR-002**: Stock Media Chrome mute and volume MUST keep working; boost MUST multiply on top of stock volume/mute so mute still silences (`brief.md` Q5; `recon-DEV-569` lines 42–43).
- **FR-003**: Existing player hotkeys (Space/K, arrows, M, F) MUST keep their usual scope at their usual targets outside the focused boost range. When the boost native range is focused, the existing player keyboard exclusion MUST let the range adjust boost (1 percentage-point arrow steps) without duplicating player shortcuts; other navigation keys on the focused range MUST keep native range semantics (`brief.md` Q5; Plan challenge decision 4).
- **FR-004**: For eligible streams, the player MUST prepare and connect GainNode → DynamicsCompressorNode → destination before attaching `createMediaElementSource`; MUST lazily create/resume an AudioContext on explicit click or keyboard activation of the boost control (not focus/open alone); MUST attach the media-element source only after resume succeeds and the runtime eligibility predicate still holds for the active element/source generation; MUST bind once per actual element across repeated activation, re-render, remount, and actual hot reload, with no duplicate connections and a retained live bound context (same-actual-element module-reload is the test simulation of that hot-reload contract, not a narrower shipped limit). A failed or invalidated attempt on a still-live unbound element MUST keep that same native element with no source attached. Stale completion against a changed or detached element MUST attach to neither element and MUST NOT replace the current element because of that stale completion. Partial/uncertain ownership (including uncertain source-creation throw) or a previously bound ineligible transition MUST recover on a fresh native element without binding the replacement on that attempt. Exact React key, guard representation, and external helper exports are Build choices inside the envelope (`brief.md` Q2–Q3; Plan challenge decision 2).
- **FR-005**: At 100% boost, including after reset, gain MUST be 1 with a transparent compressor (unity vs native). Limiting MUST engage only above 100% (`brief.md` Q2).
- **FR-006**: Boost MUST be hidden when Web Audio is unavailable or when the active element/source generation does not match the verified direct same-origin HTTP(S) library delivery path; native playback MUST remain untouched. Runtime eligibility is Web Audio availability plus that verified delivery-path match, checked on source change and immediately before binding after resume. Ineligible/unverified includes malformed URLs, unsupported schemes, indirect/blob/MSE/segmented paths outside the verified class, and a matching URL string with unverified or cross-origin effective delivery. A same-origin `src`/`currentSrc` string or a successful sample alone is insufficient. No speculative runtime probe or new API (`brief.md` Q3; Plan challenge decision 1).
- **FR-007**: On partial/uncertain ownership or a previously bound ineligible transition, the player MUST continue with a fresh native video element without binding the replacement on that attempt, reusing existing Media Chrome attach/detach, stock controls, and subtitle wiring. For the same media, MUST carry seek position when seekable after metadata, pause/play intent within browser autoplay policy, stock volume/mute, playback rate, and caption selection/delay as applicable. A genuinely different media item MUST follow existing source-change semantics (`brief.md` Q3; Plan challenge decision 2).
- **FR-008**: The player MUST persist boost in one namespaced browser-only localStorage key following the existing `subtitlePreferences.ts` namespacing/error-isolation pattern without modifying that file; restore valid 100–300% values immediately in the UI; indicate pending activation when the remembered value is above 100%; keep audio native until explicit activation; apply the remembered value on activation unless that gesture changes or resets it; default invalid storage to 100%; and isolate storage failures from playback and in-session adjustment (`brief.md` Q4; Plan challenge decision 4; `ASSUMPTIONS.md`).
- **FR-009**: Delivery MUST stay inside the approved playback file envelope: `BrowserPlayer.tsx`, `useBrowserPlayer.ts`, `BrowserPlayer.test.tsx`, and `playerCopy.ts` / `BrowserPlayer.css` only as needed for labels/styling — with no new dependency, layer, sibling module, API, schema, backend storage, secrets, `Process.Start`, or LocalPlay-execution changes (`brief.md` Q6; Plan challenge decision 4; Ledger F3 no new module).
- **FR-010**: Pre-Build Phase B (T001) MUST pass **both** proofs before T002+: (1) origin/delivery-invariant proof — authoritative route/config recon establishing the existing direct same-origin HTTP(S) library delivery invariant, separately corroborated by live Chromium real-media and Range/retry request/redirect chains via the Test URLs portal; (2) portal session-only measurement capability on controlled media, including an independent unbound native reference. A same-origin `src`/`currentSrc` string or a successful sample alone is insufficient. Any missing proof or recorded blocker stops Build (`brief.md` Q3/Q6; Plan challenge decisions 1 and 3; Task order step 2).
- **FR-011**: Delivery MUST collect web-test coverage for gain/activation/persistence/interaction/eligibility/failure stages/same-media continuity. Calibrated and real-library Chromium evidence MUST use temporary Test URLs portal session-only page evaluation that installs a temporary portal observer before activation, connects the analyser to the real compressor output when the graph is created on activation, cleans up afterwards, captures a standalone unbound native reference without binding, records gain values and RMS ratios at 100% and 200%, checks audible unity as well as measured unity (SC-001), and retains before/after/reset/mute/limiting evidence within pre-declared Q2 tolerances. jsdom tests MUST NOT drive Chromium receipts. The implementation diff MUST contain no production measurement hooks, exports, or globals (`brief.md` Test strategy; Q6; Plan challenge decision 3).
- **FR-012**: Applicable web gates MUST pass (`scripts/run-web-gates.ps1` exit 0). Applicable C# gates that are scope-empty MUST be recorded as `SKIPPED (scope-empty)`, never as PASS (`brief.md` Q6; Task order step 4).

### Key Entities

- **Boost setting**: Integer percentage 100–300 remembered on-device; UI may show it while audio remains native (pending activation).
- **Boost graph binding**: At most one media-element source graph association per actual video element for an eligible activated session.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At 100% boost (active or reset), audio matches native playback at the same stock volume with no audible/measurable extra processing on the calibrated quiet-signal check.
- **SC-002**: At 200% boost on that same calibrated quiet signal, measured output is about double the 100% level (gain 2 and analyser amplitude ≈2×).
- **SC-003**: Mute silences audio at any boost level; reload restores a valid saved boost into the UI with pending-activation behaviour when above 100%.
- **SC-004**: Ineligible/unavailable conditions (including matching URL with unverified/cross-origin effective delivery) hide boost and leave stock playback working; a failed/invalidated attempt on a still-live unbound element never attaches a source and keeps that element; stale completion against a changed/detached element attaches to neither and does not replace the current element; partial/uncertain ownership and previously bound ineligible transitions recover on a fresh native element with same-media continuity as applicable.
- **SC-005**: Real-library Chromium receipt at 200% includes control screenshot, analyser reading, and fullscreen/windowed confirmation, preserved for review.
- **SC-006**: Size M review closes under the two-round cap with Risk/Standards/Spec reports, explicit dispositions, and independent reviewed-head verification of accepted Critical–Low findings; web gates exit 0.

## Assumptions

- Volume boost / Reset boost labels, 1% step, pending-activation indication, and related micro-interaction are Patron `[assumed]` items in `specs/DEV-569/ASSUMPTIONS.md`, frozen by brief Q4–Q5.
- Exact compressor parameter numbers are not frozen beyond the observable unity-at-100% and limiting-only-above-100% rules in Q2; Build chooses parameters that satisfy those acceptance measurements without inventing a second graph topology.
- Exact localStorage key string is not frozen beyond “one namespaced browser-only key”; Build selects a single namespaced key inside the existing playback seam without adding files outside the envelope.
- Exact pending-activation copy string is authored in `playerCopy.ts` under the Q4 assumed indication requirement — not invented as a second product decision here.
- Phase A web baseline is `SKIPPED (scope-empty)` and Phase A C# baseline was reported `N/A` in the DEV-569 note; delivery records actual later verdicts honestly.
