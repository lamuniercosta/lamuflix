# Feature Specification: Rebuild browser player controls with CC panel and settings menu

**Feature Branch**: `feature/503-spec`
**Created**: 2026-11-12
**Status**: gate1: provisional — PLAN FROZEN (Keel, 2026-11-12; `brief.md`, `PLAN_ADJUDICATION.md`). Gate 1 pending user spec-PR merge. No implementation, runtime, code-review, or merge clearance.
**Input**: Ticket DEV-503 (parent DEV-100); grill-closed `brief.md` (Q1–Q8; Q8 supersedes conflicting Q1–Q7 interaction/design/file-boundary wording); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-503`, `recon-DEV-503` (lines 19–26 supersede the earlier incomplete recon section; lines 28–90 authoritative ticket text; lines 92–184 PRODUCT/constitution findings; lines 186–214 Keel adjudication source facts); `specs/PRODUCT.md`; adjudication `PLAN_ADJUDICATION.md`. Dependencies: none declared on the ticket. Series owner rulings at recon lines 77–90 remain binding.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Status; Q1–Q8; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending after Bernstein coordination / Rigger preparation-opening of that PR and verified user merge (`PLAN_ADJUDICATION.md` repair 9). Patron approval is not owner approval. Quill does not invent PLAN FROZEN or a structural checkbox. No Gate 1, implementation, runtime, or merge clearance is claimed from these repairs.

## Clarifications

### Session 2026-11-12

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Behaviour for combined time display, control bar, CC panel shell, Off + served local-track checkmark list, media-chrome gear speed menu, duplicate Play/`select` cleanup, fullscreen/dismissal/keyboard/hotkey coordination, failure-state preservation, Vitest/RTL coverage, Chromium real-layout receipts, file boundary, and gate reporting is frozen in brief Q1–Q8 (Q8 wins over conflicting Q4 historical wording). Palette `design.md` (Palette-owned; independent challenge repair) supplies settled visual binding. Optional co-located CC panel extraction filenames settled by brief Q7: `web/src/features/playback/SubtitlePanel.tsx` and `web/src/features/playback/subtitlePanel.test.tsx` only if accessibility or complexity warrants extraction; otherwise keep the panel in the approved existing playback files. Extraction is not mandatory; no invented line-count threshold. Q8 adds `playerCopy.ts` / `subtitleCopy.ts` to the editable boundary. Exact media-chrome 4.19.4 settings-menu exports/import paths/open-state/label semantics remain unverified until T001 official-doc verification (`recon-DEV-503` 212–214). No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## Scope

**In scope** (`brief.md` Required behavior and frozen boundary; Q1–Q8; ticket Scope & Acceptance at `recon-DEV-503` lines 34–69; source facts 186–214):

1. **Combined time display**: Use Media Chrome time display with duration shown so current and duration appear in `current / duration` form (example `0:12 / 1:17:05`). Remove the separate duration display that today sits beside elapsed with no separator.
2. **Control bar composition**: Play/pause, seek, combined time, mute + volume, persistent CC button, gear button, fullscreen — in that ticket-described control set. Narrow layouts wrap the strip and put seek on its own row using existing CSS tokens so every control including volume remains reachable (`brief.md` Q8; ASSUMPTIONS). Wireframe placeholder/status/future-space annotations are never rendered as copy or fake actions.
3. **Persistent CC button**: Always shown, including zero local tracks or unavailable subtitle states, because subtitle search lives in this panel in the series (search acquisition itself is out of scope here).
4. **CC panel shell**: CC opens a non-modal labelled region over the bottom-right of the video with Subtitles and Options tabs and a close (×) control, matching the delivered shell of `ref-subtitles-tab.webp`. Not `role="dialog"`. This ticket builds the panel shell only. Overlay width/height stay bounded to the player with scrollable content when needed.
5. **Subtitles tab**: List Off plus served DEV-454 local tracks as a `radiogroup` of `radio` options with `aria-checked`, check mark, and one radio tab stop (not a `<select>`, not mixed listbox wording). Arrows move/select; Space/Enter activate. Selecting a track shows it; Off hides all tracks. Keyboard-only operation required.
6. **Options tab**: Reachable shell with honest non-actionable copy (`Subtitle timing and style options will appear here.` per ASSUMPTIONS). Timing/style behaviour belongs to a later series ticket. No fake controls or fake search results. No audio/quality placeholder entries or fake deferred actions.
7. **Gear / settings menu**: Built with media-chrome settings-menu functionality verified at T001 for 4.19.4. Playback speeds exactly `0.5`, `0.75`, `1`, `1.25`, `1.5`, `1.75`, `2`. Expose current speed selection from the actual media rate (including `ratechange` and source/movie/metadata transitions); no saved preference and no forced 1× reset contrary to live state. Leave layout room for future audio track and quality entries without fabricating working deferred controls. Native media-chrome speed semantics are retained when verified — not rewrapped into custom radio/listbox patterns. Q8 copy (`x` suffix, `1x (Normal)`) applies only after T001 confirms supported label customization; unsupported seams STOP for decision rather than library substitution.
8. **Cleanup**: Remove the subtitle `<select>` and the duplicate Play control where it is present. Recon currently locates the duplicate inside `BrowserPlayer` (`recon-DEV-503` line 195), not necessarily `MovieDetailsView`. Phase B pickup (T001) confirms owner and DEV-493 overlap; remove-if-present while preserving the in-bar play control. Remove dead select CSS/test selectors without losing behavioural tests.
9. **Overlay behaviour**: CC panel and gear menu work in windowed and fullscreen modes; dismiss on page-delivered Escape and outside click; remain keyboard operable. Overlays stay inside the fullscreen player subtree. Outside pointer dismissal uses `composedPath`: active surface plus its trigger count as inside; another trigger switches surfaces once; control-bar/video outside that boundary dismisses; in fullscreen, outside means reachable content inside the fullscreen subtree; `pointercancel` alone does not dismiss.
10. **Hotkeys**: Keep incumbent `MediaController noHotkeys` and BrowserPlayer bubble hotkey ownership (`recon-DEV-503` 199–201). Preserve Space/K, left/right 10-second seek, M, and F outside control interactions. Controls consume their activation/navigation keys first; the player bubble handler respects `defaultPrevented` so a consumed key yields exactly one action. Do not mandate capture-phase playback handling. Retain editable exclusions (`input`/`textarea`/`select`/contenteditable/`[role="dialog"]`); do not blanket-suppress all panel key events. CC is a non-modal labelled region, not `role="dialog"`.
11. **Interaction contract (Q4 as refined by Q8)**: Only one top-level CC panel or gear menu open at a time; trigger toggles its own surface; opening the other closes the first. Opening moves focus into the active surface. Page-delivered Escape closes the open surface and returns focus to its trigger before **application** fullscreen-exit handling; do not promise control over browser-owned Escape. A `fullscreenchange`-to-exited fallback closes stale surfaces and restores focus to a connected trigger, else the connected player host. Outside pointer dismissal preserves focus on the clicked destination. Named buttons; tabs use `tablist`/`tab`/`tabpanel` with roving `tabindex`, Left/Right/Home/End, and automatic tab activation; tracks use `radiogroup`/`radio`/`aria-checked` as above. On movie change, restore focus only when it was inside the removed surface; on unmount, clean listeners/subscriptions without focusing detached nodes or stealing destination focus. Keep controls functional across fullscreen transitions. One local open-surface coordinator in BrowserPlayer reconciles verified native menu state; no new state library.
12. **Failure / empty states (Q5)**: Subtitles is the active tab on every fresh panel open. Off is always available and selected when no local track is active. Preserve served-track labels/order. Preserve existing zero-track / loading / unavailable / content-load-failure / retry semantics and playback, including polite `role="status"` versus distinct `role="alert"` (optional Open Local action when local play is enabled — not invented alternate copy) (`recon-DEV-503` 194–197). No silent track fallback; no invented API/error contract. Keep CC visible across those states. Existing stale-result / owned-URL cleanup remains (`recon-DEV-503` 203–206); no mandatory `AbortController` or hook rewrite.
13. **Bounded file edits (Q3 + Q8)**: Primary edits in the approved paths listed under Approach, including Q8-approved `playerCopy.ts` and `subtitleCopy.ts`. Keep media-chrome controller and existing playback/subtitle hooks and served-track contracts read-only (`useBrowserPlayer`, `useSubtitlePlayback`, `subtitleTrackModes.ts`, `subtitleContent.ts`, generated `web/src/api/types.ts`). No new dependency, top-level folder, project, architectural layer, state library, schema, public API, server persistence, LocalPlay execution, secrets, or Process.Start changes. Contract generation is not required for this UI-only scope. Generated routes/DTO fields already established (`recon-DEV-503` 190–192): list/content endpoints and `SubtitleTrackDto` (`id`, `label`, `language`, `isDefault`); content URL helper module `subtitleContent.ts`. No fabricated shipped-on-main lineage beyond those established facts.
14. **Verification (Q6)**: Extend existing Vitest/RTL suites with meaningful coverage for roles, state, focus movement, and interaction (not computed CSS/focus-ring/media-query/320px layout proof). Separate required Chromium acceptance against real movie-folder fixture copies retaining `Subs\<video stem>-<code>.srt` layout proves CSS appearance, 320px reachability, actual reduced motion (including native menu internals), native Escape outcome, and screenshot comparison against ticket references for delivered shell areas only. Missing fixtures/references/runtime ⇒ **Could not run** with an open acceptance task — never a pass via synthetic-only substitute. Reference asset availability is unverified until obtained (`recon-DEV-503`; brief), not measured absent by Keel.
15. **Visual binding (`design.md` + Q8 [assumed])**: Delivered shell follows Palette design and Q8-ratified copy — dark cinema control strip; CC panel as bottom-right cue-card overlay inside the fullscreen player subtree; named triggers `Subtitles` and `Settings`; close control labelled `Close subtitles panel`; tabular numerals for combined time; speed items shown with `x` suffix and `1x (Normal)` for 1.0 subject to T001 label support; honest empty/loading/error presentations; extend incumbent `prefers-reduced-motion` block; operable at 320px width via wrapped strip/seek row and constrained scrollable overlay. Use existing CSS tokens, not design hex/RGBA literals. No fake search results or dead Options sliders. Distinguish design annotations from rendered copy.

**Out of scope**:

- Subtitle search acquisition / download UI and results.
- Actual subtitle timing delay or caption style controls and any localStorage persistence for those series settings (owner series ruling remains binding for later tickets; this ticket introduces neither setting nor persistence).
- Working audio-track or quality settings (room only).
- Backend subtitle layout rewrite, new public API, schema, or server persistence.
- New player library or media-chrome version swap (retain media-chrome 4.19.4; installed export surface unverified until T001).
- Hardcoded machine paths (`E:\Filmes` or similar) in source; fixture roots stay external configuration.
- New domain term, ADR, or constitution amendment.
- Inventing visual details beyond brief/ASSUMPTIONS behaviour, ticket reference comparison rules, and settled `design.md` bindings.
- Mandatory `AbortController`/hook rewrite; capture-phase playback handler mandate; guessed native menu role/export facts; forced 1× rate reset; invented line-count extraction threshold.
- Claiming PLAN FROZEN, Gate 1 clearance, implementation, or runtime PASS from this Phase A draft or challenge repairs.

**Precedence**: Ticket Scope & Acceptance is already approved authority (`PRODUCT.md` §3; `recon-DEV-503` lines 34–69). Brief Q1–Q8 and `CONCLUSIONS.md` govern boundaries; **Q8 supersedes conflicting Q4 historical wording**. Ordinary owner Gate 1 remains pending. Quill does not invent PLAN FROZEN. Challenge dispositions and required repairs: `PLAN_ADJUDICATION.md`.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**CC panel**
The browser-player overlay opened by the persistent CC control, with Subtitles and Options tabs and a close control, positioned over the bottom-right of the video; implemented as a non-modal labelled region (not `role="dialog"`).
_Avoid_: subtitle `<select>`, settings gear menu, modal dialog

**Subtitles tab**
The CC panel tab that lists Off plus served local tracks with checkmark/`radiogroup`/`radio`/`aria-checked` selection (not a native `<select>`).
_Avoid_: Options tab content, subtitle search results, listbox-as-authority wording

**Options tab (shell)**
The reachable CC panel tab reserved for later timing/style work; this ticket delivers honest non-actionable empty-shell copy only.
_Avoid_: claiming timing/style delivered; rendering wireframe annotations

**Gear menu**
The media-chrome settings menu opened by the gear control, delivering playback speed choices for this ticket via verified native media-chrome semantics and leaving room for later audio/quality entries.
_Avoid_: CC panel; custom rewrapped speed radio/listbox without T001 verification

Consumed vocabulary (already in use; not redefined): **Movie**, browser playback, Media Chrome, served local subtitle track (DEV-454), Off, fullscreen, `useBrowserPlayer`, `useSubtitlePlayback`.

## What the User Will See *(mandatory)*

- **Where**: Movie details — in-page browser player control bar.
- **What the user sees or does**: Time reads as current / duration with a separator and tabular numerals (example `0:12 / 1:17:05`). The bar exposes play/pause, seek, combined time, mute/volume, Subtitles (CC), Settings (gear), and fullscreen. The old separate duration display, subtitle `<select>`, and duplicate Play under/beside the player are gone; exactly one in-bar play control remains.
- **What it needs to show up**: Default Compose/stack with Api + web; at least one playable movie in browser playback using the established served-track contracts and content URL helper (`recon-DEV-503` 190–192).

- **Where**: Browser player — CC panel (bottom-right cue card over the video).
- **What the user sees or does**: The Subtitles control is always present. Opening it shows a bottom-right non-modal labelled panel with Subtitles (default) and Options tabs and a close control labelled for accessibility as closing the subtitles panel. Subtitles lists Off plus served local tracks with a check mark on the active choice; choosing a track shows captions; Off hides them. Options shows the honest empty-shell copy. Panel works windowed and fullscreen; closes via close control, page-delivered Escape, or outside click; operable by keyboard alone.
- **What it needs to show up**: Same player surface; local tracks when present for selection proof; Subtitles control still visible with zero tracks or unavailable states.

- **Where**: Browser player — Settings (gear) menu.
- **What the user sees or does**: Settings opens the speed menu; user picks a rate from the exact 0.5–2× / 0.25 step set (shown with `x` suffix; `1x (Normal)` for 1.0 when customization is supported) and sees the current speed indicated from live media rate; playback rate changes. Menu works windowed and fullscreen with the same dismissal/keyboard rules and mutual exclusion versus the CC panel.
- **What it needs to show up**: Same player surface with active playback.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a playable movie open in the browser player, **When** the user views the control bar, **Then** time shows current / duration with a separator and the duplicate under-player Play and subtitle `<select>` are absent while the in-bar play control remains.
- **SMOKE-002**: **Given** the browser player is showing, **When** the user opens CC, switches Subtitles/Options, and closes via × / Escape / outside click in windowed and fullscreen, **Then** the panel behaves as specified and remains keyboard operable.
- **SMOKE-003**: **Given** a movie with at least one served local track, **When** the user selects a track then Off using only the keyboard in windowed and native fullscreen, **Then** captions show then hide accordingly.
- **SMOKE-004**: **Given** the browser player is playing, **When** the user changes speed from the gear menu, **Then** playback uses the chosen rate and the current selection matches the live media rate.

Chromium visual-receipt steps (real-library-layout fixtures, screenshot comparison to `ref-subtitles-tab.webp` / `current-player-time-bug.webp`, distinguishing deferred Options reference content; 320px reachability; actual reduced motion including native menu; recorded native Escape outcome) are required acceptance evidence beyond these smoke criteria; missing fixtures/references/runtime ⇒ **Could not run**, never PASS.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Combined time and control-bar cleanup (Priority: P1)

The viewer sees a single combined time readout and a control bar without the broken adjacent duration display, without the subtitle `<select>`, and without the duplicate Play under/beside the player.

**Why this priority**: Ticket acceptance leads with readable `current / duration` on a real movie and cleanup of the broken/duplicated controls.

**Independent Test**: Render the player with a known current/duration state; assert combined display, absence of separate duration display, absence of subtitle `<select>`, exactly one in-bar play control, and absence of duplicate Play (accounting for DEV-493 overlap and T001-confirmed owner).

**Acceptance Scenarios**:

1. **Given** a playing or seekable movie in the browser player, **When** the user looks at the time control, **Then** it shows current and duration in `current / duration` form (example `0:12 / 1:17:05`).
2. **Given** the rebuilt player, **When** the user inspects controls under/around the player, **Then** the separate duration display, subtitle `<select>`, and duplicate Play are gone while the in-bar play control remains.
3. **Given** the rebuilt control bar, **When** rendered, **Then** it includes play/pause, seek, combined time, mute/volume, CC, gear, and a real fullscreen control (no fullscreen placeholder).

---

### User Story 2 - CC panel shell and local-track selection (Priority: P1)

The viewer opens CC, uses Subtitles/Options tabs, selects Off or a served local track from a checkmark radio list (not a `<select>`), and closes the panel — including with keyboard only — while CC remains visible even with zero tracks or failures.

**Why this priority**: Core ticket shell and acceptance for track/Off selection.

**Independent Test**: Open CC; assert default Subtitles tab, Off + served tracks with `radiogroup`/`radio`/`aria-checked` selected state, Options empty-shell copy, close paths, and preserved zero/loading/unavailable/retry behaviour without inventing API contracts; cover rapid track A/B/Off and movie-change-during-fetch/stale resolve/URL release through unchanged hook behaviour.

**Acceptance Scenarios**:

1. **Given** the browser player, **When** the user activates CC, **Then** a bottom-right non-modal labelled panel opens with Subtitles and Options tabs and a close control, and Subtitles is active on a fresh open.
2. **Given** served local tracks, **When** the user chooses a track from the radio list, **Then** that track shows (checkmark + `aria-checked`) and captions appear; choosing Off hides all tracks.
3. **Given** zero tracks, loading, unavailable, or content-load failure states, **When** the user views the player, **Then** CC remains visible, existing retry/unavailable semantics and polite status versus alert behaviour (including optional Open Local when enabled) and playback are preserved, and no silent track fallback occurs.
4. **Given** the Options tab, **When** opened, **Then** the user sees non-actionable copy `Subtitle timing and style options will appear here.` with no fake timing/style/search controls and no rendered wireframe annotations.
5. **Given** the CC panel open, **When** the user presses Escape, clicks outside, or activates close, **Then** the panel closes (page-delivered Escape restores focus to the CC trigger before application fullscreen-exit handling; outside click uses the composedPath boundary; `pointercancel` alone does not dismiss).

---

### User Story 3 - Gear speed menu (Priority: P2)

The viewer opens the gear menu and changes playback speed among the exact allowed steps, with the current speed indicated from live media rate.

**Why this priority**: Explicit ticket acceptance for gear/speed; ordered after shell foundations per brief.

**Independent Test**: Open gear; assert exactly the seven speeds, current selection matching real media rate across `ratechange`/source/movie/metadata transitions, and that choosing a speed updates playback rate; no off-step options; no saved preference; no forced 1×.

**Acceptance Scenarios**:

1. **Given** the browser player, **When** the user opens gear, **Then** speed choices are exactly 0.5, 0.75, 1, 1.25, 1.5, 1.75, and 2 with current selection exposed from live media rate.
2. **Given** the gear menu open, **When** the user chooses a listed speed, **Then** playback uses that rate.
3. **Given** gear and CC, **When** one is open and the user opens the other, **Then** only the newly opened surface remains open.

---

### User Story 4 - Keyboard, focus, fullscreen, and hotkey isolation (Priority: P2)

Panel/menu surfaces remain usable in fullscreen, dismiss correctly, keep focus rules, isolate hotkeys while consuming keys, and reset on movie change/unmount while preserving normal player shortcuts elsewhere.

**Why this priority**: Ticket requires fullscreen/keyboard/hotkey behaviour; brief places coordination after shell/track/gear foundations.

**Independent Test**: Exercise mutual exclusion, focus entry/restore, page-delivered Escape vs outside click, `fullscreenchange`-exited fallback, fullscreen retention, movie-change focus-only-if-inside-surface reset, unmount cleanup without detached focus, keyboard-only track/speed operation, and Space/K/left-right 10s/M/F isolation vs preservation with exactly one action when consumed.

**Acceptance Scenarios**:

1. **Given** CC or gear open in windowed or fullscreen mode, **When** the user interacts via keyboard or pointer dismissal, **Then** surfaces remain reachable, dismiss as specified (including `fullscreenchange`-exited fallback to connected trigger else player host), and stay functional across fullscreen transitions.
2. **Given** focus inside an open panel/menu control, **When** that control consumes Space/K, left/right arrows, M, or F for its own activation/navigation, **Then** the player MUST NOT also apply play/seek/mute/fullscreen actions for that consumed key (exactly one action); unconsumed keys among that set retain existing app hotkey behaviour per Q8 (no blanket suppression merely because focus is inside a control).
3. **Given** player focus outside panel/menu controls, **When** the user uses Space/K, left/right 10-second seek, M, or F, **Then** existing player hotkeys still work.
4. **Given** an open panel/menu, **When** the movie changes or the player unmounts, **Then** open surfaces reset; movie change restores focus only if it was inside the removed surface; unmount cleans subscriptions without focusing detached nodes or stealing destination focus.

---

### Edge Cases

- What happens with zero local tracks? CC stays visible; Off remains available/selected; no fabricated tracks; existing empty semantics preserved.
- What happens on subtitle list/content failure? Existing unavailable/retry behaviour and continued playback; polite status versus alert preserved; optional Open Local only when enabled; no invented error contract; CC stays visible.
- What happens if DEV-493 already removed the duplicate Play? Pickup recon records overlap and owner; cleanup removes only if present and verifies in-bar play remains.
- What happens when reference images or real-layout fixtures are missing? Chromium acceptance is **Could not run** with an open acceptance task — not PASS, not synthetic-only substitute. Availability is unverified until T015 obtains them.
- What happens if media-chrome settings-menu exports/label customization are unsupported for 4.19.4? T001 documents official exports and **STOP**s for recon/decision — do not invent import paths, replace media-chrome, or hard-bind challenger spellings.
- What about Options screenshots that show timing/style controls? Compare delivered shell only; do not claim deferred content was delivered; do not render wireframe annotations.
- What about browser-owned Escape during native fullscreen? Do not claim Chromium invariably swallows Escape; T016 records the actual native Escape outcome and page-level closure behaviour.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Player MUST show combined current / duration via MediaTimeDisplay with duration enabled, and MUST NOT show a separate MediaDurationDisplay beside it.
- **FR-002**: Control bar MUST include play/pause, seek, combined time, mute + volume, CC, gear, and a real fullscreen control (no fullscreen placeholder); narrow layouts MUST wrap the strip and give seek its own row using existing CSS tokens so every control including volume remains reachable.
- **FR-003**: CC control MUST remain visible even when there are zero local tracks or subtitle unavailable/loading/failure states.
- **FR-004**: CC MUST open a bottom-right non-modal labelled overlay region (not `role="dialog"`) with Subtitles and Options tabs and a close control; Subtitles MUST be the active tab on each fresh open; overlay MUST stay width/height-bounded to the player with scrollable content when needed.
- **FR-005**: Subtitles tab MUST list Off plus served DEV-454 local tracks as a `radiogroup` of `radio` options with `aria-checked` and checkmark (not a `<select>`); arrows move/select; Space/Enter activate; one radio tab stop; choosing a track shows it; Off hides all tracks; keyboard-only selection MUST work.
- **FR-006**: Options tab MUST be reachable and show only honest non-actionable copy `Subtitle timing and style options will appear here.` with no working timing/style/search/audio/quality controls and no rendered wireframe annotations or fake deferred actions.
- **FR-007**: Gear MUST use media-chrome settings-menu functionality verified for 4.19.4 at T001 and expose exactly speeds 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2 with current selection indicated from the actual media rate (including `ratechange` and source/movie/metadata transitions); choosing a speed MUST change playback rate; MUST NOT add saved speed preference or force a 1× reset contrary to live state; native media-chrome speed semantics MUST be retained when verified (not rewrapped); Q8 speed labels apply only after T001 confirms supported customization, otherwise STOP for decision.
- **FR-008**: Implementation MUST remove the subtitle `<select>` and the duplicate Play where present (remove-if-present at the T001-confirmed owner; recon currently locates it in BrowserPlayer), preserving the in-bar play control and coordinating with DEV-493 overlap; dead select CSS/test selectors MUST be removed without losing behavioural tests.
- **FR-009**: CC panel and gear menu MUST work in windowed and fullscreen modes, dismiss on page-delivered Escape and outside click (`composedPath` boundary: active surface + trigger inside; other trigger switches once; fullscreen reachable outside dismisses; `pointercancel` alone does not), remain keyboard operable, and live inside the fullscreen player subtree.
- **FR-010**: Only one of CC panel or gear menu MAY be open at a time; each trigger toggles its own surface; opening one closes the other; one local open-surface coordinator in BrowserPlayer MUST reconcile verified native menu state without a new state library.
- **FR-011**: Opening a surface MUST move focus into it; page-delivered Escape MUST close it and restore focus to its trigger before application fullscreen-exit handling (no claim of control over browser-owned Escape); a `fullscreenchange`-to-exited fallback MUST close stale surfaces and restore focus to a connected trigger else the connected player host; outside pointer dismissal MUST preserve destination focus; movie change MUST restore focus only when it was inside the removed surface; unmount MUST clean subscriptions without focusing detached nodes or stealing destination focus.
- **FR-012**: Keep `MediaController noHotkeys` and BrowserPlayer bubble hotkey ownership; while a panel/menu control consumes Space/K, left/right arrows, M, or F (control-first / `defaultPrevented`), the player MUST NOT also apply play/seek/mute/fullscreen hotkey actions (exactly one action); those hotkeys including left/right 10-second seek MUST remain available outside panel/menu control interactions; do not mandate capture-phase playback handling; retain editable exclusions without blanket panel key suppression.
- **FR-013**: Open surfaces MUST reset on movie change/unmount; controls MUST remain functional across fullscreen transitions; existing stale-result and owned-URL cleanup MUST be preserved without mandatory `AbortController` or hook rewrite.
- **FR-014**: Existing zero-track / loading / unavailable / content-failure / retry and movie-change/unmount/stale-request subtitle semantics MUST be preserved, including polite `role="status"` versus distinct `role="alert"` (optional Open Local when enabled); no silent track fallback; no new public API/error contract; generated subtitle routes/DTO fields and `subtitleContent.ts` helper remain as established (`recon-DEV-503` 190–192).
- **FR-015**: Implementation MUST stay within the Q3+Q8 file boundary: `BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, `BrowserPlayer.test.tsx`, `subtitleSelector.test.tsx`, `MovieDetailsView.tsx`, `MovieDetailsView.subtitles.test.tsx`, Q8-approved `playerCopy.ts` and `subtitleCopy.ts`, plus optional co-located panel component/test under `web/src/features/playback/` only if accessibility/complexity warrants it — exact optional filenames per brief Q7: `SubtitlePanel.tsx` and `subtitlePanel.test.tsx`; otherwise keep the panel in the approved existing files; extraction is not mandatory and no invented line-count threshold applies. New copy MUST route through the approved copy files. Hooks (`useBrowserPlayer`, `useSubtitlePlayback`), `subtitleTrackModes.ts`, `subtitleContent.ts`, and generated `web/src/api/types.ts` are read-only. Retain media-chrome 4.19.4; no new dependency/layer/schema/API/LocalPlay/secrets/Process.Start. Any additionally necessary edit requires a cited decision before work. No explanatory comments in test or production code beyond AGENTS exemptions.
- **FR-016**: Meaningful Vitest/RTL coverage MUST extend the approved existing suites for composition/cleanup, CC/tabs/selection/Off/failure states, rapid track A/B/Off and stale-lifecycle regressions through unchanged hooks, all speed choices with live-rate indication, mutual exclusion/focus/dismissal/`composedPath`/hotkey isolation (each preserved shortcut), lifecycle, and movie-details integration — asserting roles, state, focus movement, and interaction, not computed CSS appearance, focus-ring pixels, media-query, or 320px layout proof.
- **FR-017**: Required Chromium acceptance MUST use externally configured copies of real library movie folders retaining `Subs\<video stem>-<code>.srt`, capture receipts (fixture provenance without secrets, steps, browser/version, outcomes, screenshot paths, comparison results), prove CSS appearance / 320px reachability / actual reduced motion including native menu internals, record actual native Escape outcome, keyboard track-then-Off with visible/hidden captions in both windowed and native fullscreen, and compare delivered UI to ticket references without claiming deferred Options content; missing fixture/reference/runtime ⇒ Could not run (blocking acceptance). Reference availability is unverified until obtained.
- **FR-018**: Visual design MUST follow settled Palette `design.md` bindings and Q8-ratified `[assumed]` copy (cue-card panel, named Subtitles/Settings/Close labels, tabular time numerals, speed item presentation including `1x (Normal)` subject to T001, reduced-motion extension of the incumbent block, 320px operability via wrapped strip/seek row and constrained scrollable overlay, honest empty/error shells, existing CSS tokens) against the ticket references; implementation MUST NOT invent conflicting visuals or render wireframe annotations as copy.
- **FR-019**: CC panel MUST be a non-modal labelled region (not `role="dialog"`) exposing `tablist`/`tab`/`tabpanel` with roving `tabindex`, Left/Right/Home/End, and automatic tab activation; track list MUST use `radiogroup`/`radio`/`aria-checked` with Off plus served tracks, arrows move/select, Space/Enter activate, and one radio tab stop; native media-chrome speed pattern MUST be verified at pickup and not rewrapped; polite `role="status"` versus distinct `role="alert"` semantics MUST be preserved (never call alert polite; do not add new roles to old status output); visible focus appearance is Chromium evidence — RTL asserts focus movement/roles/state only.

### Key Entities

- **Served local subtitle track**: An existing DEV-454 track offered to the browser player (label/order/selection semantics preserved; presentation replaced); DTO fields `id`, `label`, `language`, `isDefault` per generated contracts.
- **CC panel surface**: Non-modal labelled overlay with Subtitles/Options tabs and close control.
- **Gear menu surface**: Media-chrome settings menu exposing speed choices for this ticket via verified native semantics.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a real playable movie, a viewer can read current and duration in separated `current / duration` form without the adjacent-duration concatenation bug.
- **SC-002**: A viewer can open the CC panel, switch tabs, close it, and complete Off ↔ track selection with keyboard alone in both windowed and fullscreen modes.
- **SC-003**: A viewer can change playback speed through the gear menu among only the seven allowed rates and observe the current rate indicated from live media.
- **SC-004**: Existing player hotkeys (including left/right 10-second seek) still work when panels/menus are not consuming keys, and keyboard track/speed operation does not accidentally seek or toggle playback/mute/fullscreen.
- **SC-005**: Chromium receipts with real-layout fixtures either PASS the delivered-shell comparison against ticket references or are explicitly **Could not run** with open acceptance work — never silently substituted by unit tests alone.
- **SC-006**: Web gates for in-scope web changes exit 0; scope-empty C# gates remain reported as SKIPPED (scope-empty), never PASS.

## Assumptions

- Taste assumptions tagged `[assumed]` in `specs/DEV-503/ASSUMPTIONS.md` (fresh-open Subtitles tab, retained track label/order, Options empty-shell copy, Q4 micro-interaction defaults, Q7 optional panel naming, Q8 labels/ARIA/responsive details) are Patron rulings, not Quill inventions. Q8 supersedes conflicting historical Q4 wording.
- Series owner rulings (media-chrome 4.19.4; browser-only delay/style persistence for later tickets; `Subs\` layout; real-layout Chromium fixtures) remain binding and are not narrowed here.
- Palette `design.md` (Palette-owned; challenge repairs independent of Quill) supplies settled visual binding incorporated above; Quill does not invent competing visuals (`brief.md` Gate expectations).
- Exact media-chrome settings-menu element/prop/export/open-state/label names come from official 4.19.4 docs at T001 pickup; this draft does not assert undocumented names (`brief.md` Q8; `recon-DEV-503` 212–214). Manifest pin is verified; installed exports are not. `design.md` component-tree labels (CcTrigger, CcPanel, SpeedMenu, etc.) are design vocabulary, not forced source filenames.
- Optional co-located panel component/test under `web/src/features/playback/` remains optional per Q3/Q7; if extraction is warranted, use exactly `SubtitlePanel.tsx` and `subtitlePanel.test.tsx`; otherwise keep the panel in the approved existing playback files. Extraction is not mandatory; no invented line-count threshold (`brief.md` Q7/Q8; ASSUMPTIONS.md).
- Source facts in `recon-DEV-503` lines 186–214 (contracts/helpers, status/alert/Open Local, noHotkeys, stale-result ownership, copy modules/tokens/reduced-motion, manifest-only media-chrome pin) are authoritative for this draft; unsupported shipped-on-main lineage and guessed menu APIs are omitted.
- Challenge round 1 dispositions and the consolidated repair list live in `PLAN_ADJUDICATION.md`. Keel independently validated these repairs and recorded PLAN FROZEN in brief.md; Gate 1 remains pending.

