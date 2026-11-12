# Feature Specification: Subtitle timing and caption style options

**Feature Branch**: `feature/500-spec`
**Created**: 2026-11-12
**Status**: gate1: provisional — PLAN FROZEN (`KEEL_REPAIR_REREAD.md`). Gate 1 pending user spec-PR merge. No implementation, dependency-ready, runtime, review-closure, or merge clearance claimed.
**Input**: Ticket DEV-500 (parent DEV-100); grill-closed `brief.md` (Q1–Q10) plus Phase 2 step 6 plan-challenge amendment; `CONCLUSIONS.md` (including Patron seven-item ruling); `ASSUMPTIONS.md`; Maestri notes `DEV-500`, `recon-DEV-500`. **Evidence qualification:** current `recon-DEV-500:7-16` supplies frontend identity observations and explicitly unavailable backend/restoration evidence; earlier ticket/reference/dependency recon line citations in Q1–Q10 are historical (prior grill snapshot) and no longer resolve in the current note — basis preserved in recorded grill exchanges, not independently restored. `specs/PRODUCT.md`. Dependency: delivered DEV-503 CC panel shell (implementation gated on merge to main + refreshed recon; `brief.md` Q2). Palette `design.md` is delivered; corrections remain pending (Palette-owned).

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Authority; Q1–Q10; plan-challenge amendment; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-12

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Behaviour for Options-tab timing/style, G/H, cue shifting without reload, shared overlay, localStorage keys/failure, provisional style profile, file boundary, tests/gates, and DEV-503 pickup is frozen in brief Q1–Q10. Screenshot fidelity remains unverified and unwaived (`brief.md` Q3/Q7/Q10). Palette `design.md` (Palette-owned) plus any extra taste rulings are prerequisites to plan challenge, not inventable by Quill. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

### Session 2026-11-12 (plan adjudication)

- Q: Plan challenge / adjudication repairs — propagate Patron seven-item rulings? → A: Yes. Identity codec, `hidden` native mode, half-open cue intervals, immutable cue snapshots, plain literal text, expanded focus guards, immediate best-effort persistence (no mandatory debounce), enum tokens, single `useSubtitleOptions` instance + props-only renderers, and co-located `subtitleTestDoubles.ts` are recorded in brief amendment / CONCLUSIONS / ASSUMPTIONS. Backend track-ID stability across rescans remains **unproven** (current recon `:7-16`), never assumed PASS. Historical recon citations remain qualified. Palette design is delivered with pending corrections. PLAN FROZEN remains pending independent Keel reread.

## Scope

**In scope** (`brief.md` Q1; ticket Scope/Acceptance — historical ticket body preserved in grill/`brief.md` from the prior recon snapshot; owner rulings likewise; current recon `:7-16` does not restate the ticket body):

1. **Options tab content**: Populate the CC Options tab to match reference screenshots `ref-options-timing.png`, `ref-options-style-1.png`, and `ref-options-style-2.png` (visual fidelity outstanding until references are inspected; provisional profile in Q3/`ASSUMPTIONS.md` permits drafting only).
2. **Subtitle timing**: “Subtitle delay” with − / value / + buttons (0.1 s ordinary, 0.5 s with Shift); help line exactly `Negative shows subtitles earlier, positive later.`; **Reset timing**; hotkeys G (earlier) and H (later) while playing; delay applies instantly by shifting loaded cue times without reload.
3. **Caption style**: Live **Subtitle preview** at the top of the Caption style section; controls Font, Text size (%), Text color, Text opacity, Background color, Background opacity (ticket default 75%), Edge style (None, Drop shadow, Raised, Depressed, Outline); **Reset to default** restores the settled provisional profile.
4. **Rendering**: Player draws subtitles in its own overlay inside the media-chrome controller; native track stays loaded with selected track `mode = 'hidden'`; preview reuses the same overlay component/style calculation; every style control changes preview and live captions, including fullscreen.
5. **Persistence**: Browser localStorage only, per device — delay keyed by numeric movie ID + exact opaque API track ID via JSON-array suffix; style global. Restore saved values on reload; never-saved pair starts at 0. Immediate best-effort persistence (no mandatory debounce). Storage failure never disrupts playback.
6. **Acceptance evidence**: Real Chromium **and** Firefox receipts against copies of actual `E:\Filmes` movie folders including `<movie folder>\Subs\<video stem>-<code>.srt`. Synthetic same-folder fixtures alone do not satisfy acceptance. Browser receipts own native-mode suppression proof, real cue loading, fullscreen, responsive scale, motion/no-transition, target-size and contrast observations.
7. **Stack constraints**: Keep media-chrome (ticket names existing 4.19.4); no new player library, server persistence, schema, public API, LocalPlay, secrets, Process.Start, or subtitle-layout changes. Preserve existing subtitle selection / Off / retry behaviour from the DEV-503 shell.

**Out of scope**:

- Building or duplicating the CC panel shell (DEV-503); cherry-picking unmerged DEV-503 commits.
- Subtitle search / download UI (later series tickets).
- Server-side preference sync, cross-tab live sync requirement (no storage-event listener required), or writing delay/style into subtitle files / cue content.
- New npm/NuGet dependency, architectural layer, state library, top-level folder, or project.
- Backend / OpenAPI / generated TypeScript contract changes.
- Claiming screenshot/default equivalence before reference inspection; inventing Palette layout/shadow geometry beyond delivered/corrected `design.md`.
- Claiming PLAN FROZEN, Gate 1, dependency readiness, implementation, runtime PASS, backend identity stability, or merge clearance from this Phase A draft.
- Mandatory debounce timing/bounded-write-count requirements absent measured jank; mandatory runtime `Object.freeze`; internal `setState` spy / referential-stability acceptance; inventing ResizeObserver or other resources not actually acquired.

**Precedence**: Ticket Scope & Acceptance is already approved authority (`PRODUCT.md` §3; historical ticket body in brief/grill). Brief Q1–Q10, plan-challenge amendment, `CONCLUSIONS.md`, and `PLAN_ADJUDICATION.md` dispositions govern plan boundaries. Ordinary owner Gate 1 remains pending.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**Subtitle delay**
Signed millisecond offset applied to loaded cue times for the active movie+track pair; negative shows earlier, positive later.
_Avoid_: playback rate; seek offset; global style

**Caption style**
Global device-local visual profile (font, size, colors/opacities, edge style) applied to preview and live overlay.
_Avoid_: per-track style; server preference

**Movie+track identity**
The unambiguous pair used to key delay persistence: canonical numeric movie ID plus exact opaque API `track.id`, encoded as `JSON.stringify([numericMovieId, trackId])`. No position, source/blob URL, case folding, or Unicode normalization. Backend cross-request/rescan stability is unavailable evidence, not a guarantee.
_Avoid_: movie-only or track-only delay keys; positional or URL identity

**Immutable loaded cue baseline**
Copied primitive cue start/end (and text) snapshot retained after load, independent of mutable browser cue objects; never cumulatively rewritten by delay adjustments.
_Avoid_: mutating browser cue objects as the sole source of truth

**Shifted effective interval**
Half-open interval `[baselineStart + delay, baselineEnd + delay)` (start inclusive, end exclusive) used by the overlay for active-cue selection at the media clock.
_Avoid_: network reload; irreversible file edits; closed end-inclusive intervals

**Shared overlay / preview**
One caption overlay component and style calculation used for live playback captions and the Options preview sample.
_Avoid_: separate style engines for preview vs live; native `::cue`-only styling for the delivered look

Consumed vocabulary (already in use; not redefined): Movie, browser playback, Media Chrome, CC panel, Options tab, Subtitles tab, Off, served local subtitle track, fullscreen.

## What the User Will See *(mandatory)*

- **Where**: Movie details — browser player CC panel → Options tab.
- **What the user sees or does**: Options shows Subtitle timing (− / signed delay value / +, help line, Reset timing) and Caption style (live preview sample, Font, Text size (%), Text color, Text opacity, Background color, Background opacity, Edge style, Reset to default). Timing stays usable only with a usable selected track; Options remains reachable with captions Off so style/preview can still be edited.
- **What it needs to show up**: Default Compose/stack with Api + web; DEV-503 CC shell present on the player; at least one playable movie; for timing proof, a usable selected local subtitle track.

- **Where**: Browser player — live captions during playback (windowed and fullscreen).
- **What the user sees or does**: Captions appear in the custom overlay using the current delay and style. Markup-looking text appears as literal characters (no HTML rendering). G/H nudge delay while playing (ignored when focus is in input/textarea/select/contentEditable/dialog contexts). Off hides live captions while retaining saved preferences. Native browser cue chrome does not show a second caption stream.
- **What it needs to show up**: Same player surface with a selected usable track (for live captions/timing); playback started for G/H.

- **Where**: Same browser on later visits / after reload.
- **What the user sees or does**: Returning to the same movie+track restores that delay; a different track starts at 0.0 s; global style restores across movies. If storage is blocked, defaults apply and playback continues.
- **What it needs to show up**: Same player; browser localStorage available for persistence proof (blocked-storage case uses a denied-storage scenario).

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a playable movie with a selected subtitle track in the browser player, **When** the user opens CC → Options and adjusts delay with − / + (and Shift for larger steps), **Then** the delay value updates, live captions shift immediately without reload, and Reset timing returns that pair to 0.0 s.
- **SMOKE-002**: **Given** the Options tab open, **When** the user changes each caption-style control, **Then** the preview and live captions (including fullscreen) reflect the change, and Reset to default restores the settled defaults.
- **SMOKE-003**: **Given** a delay saved for movie A + track 1, **When** the user reloads and returns to that pair, then switches to another track, **Then** the saved delay restores for the first pair and the other track starts at 0.0 s.
- **SMOKE-004**: **Given** playback with a usable track, **When** the user presses G or H while playing (outside input/textarea/select/contentEditable/dialog contexts and without Ctrl/Alt/Meta), **Then** delay moves earlier/later by the settled step; with no usable track or while not playing, those keys do not change delay.

Chromium **and** Firefox real-library-layout receipts (fixture copies proving `Subs\` layout; hotkeys; reload/pair isolation; independent resets; every style including fullscreen; blocked storage; uninterrupted playback; native `hidden` mode / no second caption stream; responsive scale; motion/no-transition; target-size; panel contrast observations; reference inspection before fidelity claims) are required acceptance evidence beyond these smoke criteria; missing fixtures/references/runtime ⇒ **Could not run**, never PASS via synthetic-only substitute. DOM-only assertions do not prove native suppression.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Subtitle timing delay (Priority: P1)

The viewer opens Options, adjusts subtitle delay with − / value / +, resets timing, and uses G/H while playing so captions appear earlier or later without reloading the track.

**Why this priority**: Ticket acceptance leads with delay controls, reset, hotkeys, and pair-keyed persistence.

**Independent Test**: With a usable selected track, exercise ordinary and Shift steps, sign convention, Reset timing isolation from style, G/H playing-only guards, disabled timing without a usable track, and pair restore vs new-pair zero — without requiring every style control to be finished.

**Acceptance Scenarios**:

1. **Given** a usable selected subtitle track, **When** the user clicks − or + (Shift for 0.5 s), **Then** delay changes by 0.1 s ordinary or 0.5 s with Shift, displays as signed seconds to one decimal (`0.0 s` at zero), and live captions shift immediately without reload.
2. **Given** a non-zero delay for the active movie+track, **When** the user activates Reset timing, **Then** only that pair’s delay returns to zero; global caption style is unchanged.
3. **Given** playback with a usable track and focus outside input/textarea/select/contentEditable/dialog contexts, **When** the user presses G or H (without Ctrl/Alt/Meta), **Then** delay moves earlier or later by the same steps; when not playing, no usable track, or in ignored focus/modifier cases, G/H do not change delay.
4. **Given** a saved delay for movie+track A, **When** the user reloads and returns to A, then selects a different track, **Then** A restores its delay and the other track starts at 0.0 s with no cross-pair leakage.

---

### User Story 2 - Caption style and preview (Priority: P1)

The viewer customizes caption appearance from Options, sees a live preview update, resets to defaults, and keeps a global style across movies.

**Why this priority**: Ticket acceptance requires every style control to change preview and live captions; ordered with timing as co-equal Options content.

**Independent Test**: Change each style control; assert preview updates; Reset to default restores the provisional profile; global style restores after reload; Options remains editable with captions Off.

**Acceptance Scenarios**:

1. **Given** the Options Caption style section, **When** the user changes Font, Text size (%), Text color, Text opacity, Background color, Background opacity, or Edge style, **Then** the Subtitle preview updates immediately using the same style calculation as live captions.
2. **Given** customized style values, **When** the user activates Reset to default, **Then** the provisional default profile is restored (including 75% background opacity and Edge style None) without resetting subtitle delay.
3. **Given** a saved global style, **When** the user reloads or opens another movie, **Then** the style restores; Options remains editable when captions are Off.

---

### User Story 3 - Shared live overlay and cue shifting (Priority: P2)

The viewer sees timed, styled captions from the shared overlay in windowed and fullscreen play, without duplicate native captions, and with Off hiding live captions while retaining saved values.

**Why this priority**: Ticket rendering approach; depends on preference values from US1/US2 but is the visible playback outcome.

**Independent Test**: With preferences applied, verify selected native track `mode = 'hidden'`, overlay active cues from copied immutable baseline + delay using half-open intervals, negative-start overlap at time zero, seek/pause/play/rate/resync, multiple cues/line breaks, literal markup text, fullscreen containment, Off hides live captions, and no timing/style-triggered reload.

**Acceptance Scenarios**:

1. **Given** a selected usable track with delay and style set, **When** the movie plays (windowed and fullscreen), **Then** captions render in the shared overlay inside the media-chrome fullscreen subtree and native cues are not shown as a second stream (`mode = 'hidden'`; browser receipts prove native suppression).
2. **Given** loaded cues and a delay change, seek, pause/play, rate change, or late cue readiness, **When** those events occur, **Then** the overlay resynchronizes using baseline + current delay without cumulative drift or track reload; paused playback does not keep rescheduling frames; unchanged active-cue membership/order does not produce observable repeated updates.
3. **Given** captions Off, **When** the user plays, **Then** live captions are hidden while saved delay/style values are retained for later use.

---

### User Story 4 - Storage failure safety and shell integration (Priority: P2)

The viewer keeps watching even when localStorage is blocked or corrupt; Options lives in the delivered DEV-503 shell without breaking selection/Off/retry or inventing a second panel.

**Why this priority**: Owner ruling that storage failure must never break playback; dependency reuse is a pickup gate.

**Independent Test**: Denied/invalid storage (including lazy accessor-getter throws) yields defaults and in-session adjustments without interrupting playback; shell integration preserves DEV-503 tab/focus/dismissal and selection behaviour; single preference-hook owner with props-only Options/overlay; no duplicate shell.

**Acceptance Scenarios**:

1. **Given** blocked or failing localStorage (including accessor getter throws), **When** the user opens the player and adjusts timing/style, **Then** defaults apply, session adjustments remain in memory for the mount via immediate best-effort saves, failed writes do not claim persistence, and playback continues.
2. **Given** the delivered DEV-503 CC shell after dependency pickup, **When** the user uses Subtitles selection/Off/retry and Options timing/style, **Then** selection behaviour is preserved and Options content is integrated without a duplicated panel or cherry-picked unmerged shell.

---

### Edge Cases

- What happens with no usable selected track? Timing controls disabled; G/H no-op; style/preview still editable.
- What happens when captions are Off? Live captions hidden; saved preferences retained; Options style still editable.
- What happens on invalid/malformed storage data? Invalid style falls back to the full default profile; invalid delay to zero.
- What happens on storage write/remove failure after a reset? Memory updates; removal is best effort — do not promise the reset survives reload if removal failed.
- What happens when switching movie/track quickly? Must not save the old pair’s delay under the new pair’s identity; deferred out-of-order async completion must not write the wrong pair (generation/identity guards).
- What happens with markup-looking cue text (`<i>`, `<b>`, `<img onerror=…>`, SVG onload, javascript: links, style strings)? Rendered as literal plain text; no corresponding active elements or executed handlers.
- What happens at half-open boundaries (end-at-zero, start-at-zero, negative overlap, adjacent cues)? Inclusive start / exclusive end; cue ending at zero is absent at t=0; cue starting at zero is active at t=0.
- What happens if reference screenshots are still missing? Screenshot/default fidelity remains **unverified**; do not claim equivalence; Could not run for fidelity comparison until inspected.
- What happens if DEV-503 is not yet on main at Build pickup? Implementation remains blocked until merge, worktree update/rebase, and refreshed Options integration recon (`brief.md` Q2).
- What happens if real-library fixtures are unavailable? Chromium/Firefox acceptance is **Could not run** with open acceptance work — never synthetic-only PASS.
- What about cross-tab updates? No live sync required; no storage-event subscription required. If an implementation adds one, it must not clobber in-memory state.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Options tab MUST present Subtitle timing with − / value / + controls at 0.1 s ordinary and 0.5 s with Shift, the exact help line `Negative shows subtitles earlier, positive later.`, and Reset timing.
- **FR-002**: While playing with a usable selected track, and outside focused `input` (including range/color), `textarea`, `select`, contentEditable targets, and dialog contexts (including descendants), and without Ctrl/Alt/Meta, G MUST move delay earlier and H later by the same steps as the buttons; otherwise those hotkeys MUST be no-ops for delay. Timing controls MUST be disabled when no usable track is selected.
- **FR-003**: Delay MUST be stored and computed as integer milliseconds with no arbitrary product clamp; negative means earlier and positive later; display MUST use signed seconds to one decimal with `0.0 s` at zero.
- **FR-004**: Delay MUST apply instantly by deriving shifted effective half-open intervals `[baselineStart + delay, baselineEnd + delay)` from a copied immutable loaded-cue baseline of primitive cue data (independent of mutable browser cue objects) without refetch/reload and without cumulative drift of already-shifted values. At media time zero, a cue ending at zero MUST be absent and a cue starting at zero MUST be active.
- **FR-005**: Reset timing MUST clear only the active movie+track delay; Reset to default MUST restore only the global caption-style profile and MUST NOT reset timing.
- **FR-006**: Caption style MUST include live Subtitle preview plus Font, Text size (%), Text color, Text opacity, Background color, Background opacity (default 75%), and Edge style choices None / Drop shadow / Raised / Depressed / Outline; every control MUST update preview and live captions including fullscreen. The four color/opacity controls MUST retain separately visible/programmatic labels Text color, Text opacity, Background color, Background opacity.
- **FR-007**: Default / reset style profile MUST match the Patron-ratified provisional table in `ASSUMPTIONS.md` / brief Q3 (screenshot fidelity remains unverified until references are inspected; discrepancies require an explicit Patron ruling).
- **FR-008**: Selected native subtitle track MUST remain loaded with `mode = 'hidden'`; a single shared CaptionOverlay MUST render live and preview captions inside the media-chrome fullscreen subtree, preserve multiple cues and line breaks, stay pointer-transparent and non-interactive (not aria-live), and render all text — including markup-looking strings — as literal plain text without HTML interpretation or a new renderer dependency. Real Chromium/Firefox receipts MUST prove native suppression; DOM-only assertions do not suffice.
- **FR-009**: Overlay MUST resync immediately on delay/track/seek/pause/play/rate/cue-readiness changes; playback-only frame scheduling MUST update observable state only when ordered active-cue identity membership/order changes; shifted intervals MUST be recomputed when baseline or delay changes; listeners/frames/stale-track work MUST clean up on switch/unmount. Negative effective starts MUST show the portion overlapping time zero and omit cues already ended. Paused playback MUST NOT keep rescheduling frames; unchanged active cues MUST NOT produce observable repeated updates. Acceptance MUST NOT require an internal `setState` spy or referential-stability assertion.
- **FR-010**: Persistence MUST use browser localStorage only: global style key `lamuflix:subtitle-style:v1`; per-pair delay key `lamuflix:subtitle-delay:v1:${JSON.stringify([numericMovieId, trackId])}` using canonical numeric movie ID and exact opaque API `track.id` with no position/URL/case-folding/Unicode normalization. Equivalent numeric movie representations MUST normalize to the same key; intentionally equivalent forms MUST NOT require distinct keys. Backend identity stability across rescans is unproven and MUST NOT be treated as guaranteed. No server sync, schema, or subtitle-file/cue-content writes. No cross-tab live-sync requirement and no mandatory storage-event listener.
- **FR-011**: Persistence MUST validate internal font tokens `system-sans` | `serif` | `monospace`, edge tokens `none` | `drop-shadow` | `raised` | `depressed` | `outline`, permitted numeric ranges/steps, hex colors, and safe-integer millisecond delay — never arbitrary CSS. Missing/malformed/unsupported style MUST fall back to the full default profile; invalid delay to zero. These tokens are internal browser preferences, not API fields.
- **FR-012**: Storage access/read/write/remove failures MUST be caught independently of playback, including lazy storage accessor-getter throws independently of method throws. Blocked storage MUST initialize defaults and allow in-memory session adjustments; preference changes MUST use immediate in-memory updates plus immediate best-effort saves (no mandatory debounce). Failed writes MUST retain the current adjustment without claiming persistence; independent resets MUST update memory and attempt removal of only their own key. Stale async work MUST be guarded by generation/current-identity checks on completion, with switch/unmount invalidation; existing abort and object-URL cleanup ownership MUST be preserved (no duplicate fetch/cleanup ownership).
- **FR-013**: Off MUST hide live captions while retaining saved values. Unsaved movie+track pairs MUST start at zero delay; saved pairs MUST restore their own value without cross-movie/track leakage. Real reload persistence acceptance remains mandatory.
- **FR-014**: Options MUST reuse delivered DEV-503 tab/focus/dismissal behaviour; retain ticket labels/help (including exact `Text size (%)` and the four separate color/opacity labels); all buttons/selects/ranges/color inputs MUST be labeled, keyboard operable, and visibly focused. Text and background opacity MUST operate independently; background boxes MUST fit caption lines rather than tinting the video. Font stacks and CSS-only edge mappings MUST follow brief Q7 / `ASSUMPTIONS.md`. Presentation chrome tokens (provisional panel palette, focus ring, 44px targets, checker backdrop, no transitions) follow Patron-ratified assumptions and Palette `design.md` (delivered; corrections pending). Screenshot fidelity remains unwaived.
- **FR-015**: Implementation MUST stay within the Q8 file boundary under `web/src/features/playback/` (as amended): integrate `BrowserPlayer.tsx` and delivered `SubtitleSelector.tsx`/CC shell; adjust `useSubtitlePlayback.ts` and `subtitleTrackModes.ts` only as needed for loaded/`hidden` selected-track behaviour; update `subtitleCopy.ts` for ticket labels/help and settled preview copy; add `subtitlePreferences.ts`, `subtitleTiming.ts` (owner of shifted-interval/active-cue rules), `useSubtitleOptions.ts`, `useSubtitleCues.ts`, `CaptionOverlay.tsx`, `SubtitleOptions.tsx`, `captionOverlay.css`, `subtitleOptions.css`, co-located test-only `subtitleTestDoubles.ts`, and corresponding Vitest/RTL tests including named `subtitleTiming.test.ts`; extend existing `BrowserPlayer.test.tsx`, `subtitleSelector.test.tsx`, and `subtitleTrackModes.test.ts`. Instantiate `useSubtitleOptions` once in `BrowserPlayer.tsx` and pass state/actions to props-only `SubtitleOptions` and `CaptionOverlay` (no storage/media access in those renderers). No new architectural/state layer; no backend/API/DTO/OpenAPI/generated-types/package-manifest/harness/subtitle-layout edits; no unrelated rewrite/deletion. If refreshed dependency recon names a different shell integration file, amend `brief.md` before tasks use it.
- **FR-016**: Visual layout, scale, spacing, and exact shadow geometry MUST follow Palette `design.md` (delivered; corrections pending) within Q3/Q7 bounds; Quill/implementers MUST NOT invent extra taste decisions. Screenshot fidelity remains unwaived.
- **FR-017**: Vitest/RTL MUST cover: timing steps/sign/reset isolation; pair-identity codec adversarial cases (delimiter, Unicode, case, empty/long track IDs, round-trip/pair separation, numeric movie normalization, reorder/reload with same IDs); G/H playing/focus (input including range/color, textarea, select, contentEditable, dialog)/modifier/no-track guards; pair restore/new-pair zero/global style/stale-switch races; generation/identity guards with deferred out-of-order resolution and no wrong-pair writes; rapid switches; StrictMode remount; cleanup of each acquired resource (frames/listeners/observers only if introduced — do not invent ResizeObserver); invalid/blocked/read/write/remove/quota storage plus lazy accessor-getter throws; every style control in shared renderings including independent opacity and five edges; immutable copied baseline (original mutation cannot alter snapshot); half-open boundaries including end-at-zero/start-at-zero/negative-overlap/adjacent; repeated ±100 ms exactness and large-delay empty overlay without crash; no paused frame rescheduling and no observable repeated updates for unchanged active cues; literal markup/attack-vector text (`<i>`/`<b>`, img-onerror, SVG-onload, javascript-link, style strings) with no active elements/handlers; hidden native track mode assertion in integration tests; Off; preserved selection/retry; no timing/style reload. Deterministic media/cue readiness/events via `subtitleTestDoubles.ts` with fake timers/stubbed rAF; jsdom MUST NOT be claimed to prove real browser media behaviour. Deterministic timing invariants MAY use Vitest without a new property-test library. No new test dependency. Acceptance MUST NOT require mandatory debounce timing, cross-tab listener tests for a non-existent listener, runtime freeze/mutation-throws, internal state spies, or CSS implementation prescriptions.
- **FR-018**: Required real-browser acceptance MUST produce Chromium **and** Firefox receipts naming browser/version, reviewed commit, fixture-copy path proving real `E:\Filmes` folder copies with `Subs` layout, actions, observations, and screenshots; exercise hotkeys, reload/pair isolation, independent resets, every style including fullscreen, blocked storage, uninterrupted playback, native `hidden` track / no second caption stream, responsive scale, motion/no-transition, target-size, and panel contrast observations. If acceptance-driven code fixes occur, affected tests and applicable gates MUST be rerun and relevant browser receipts refreshed at the resulting head. Missing fixture/reference/runtime ⇒ Could not run (blocking acceptance). Synthetic fixtures supplement, never substitute.
- **FR-019**: Keep media-chrome (existing 4.19.4 named by ticket); no new player library. No Features:LocalPlay, secrets, or Process.Start changes. Preserve generated contract usage without hand-rolled DTO duplicates.

### Key Entities

- **Subtitle delay preference**: Signed integer-millisecond offset for one movie+track identity (`JSON.stringify([numericMovieId, trackId])`).
- **Caption style profile**: Global device-local visual settings (font/edge tokens, size, colors/opacities) with the provisional default table.
- **Loaded cue baseline**: Copied immutable cue timing/text snapshot used to derive half-open shifted effective intervals.
- **Shared caption overlay**: Single props-fed renderer for preview sample and live active cues.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A viewer can shift subtitles earlier/later with Options controls and G/H while playing, and Reset timing returns only that movie+track delay to zero.
- **SC-002**: A viewer can change every caption-style control and see both preview and live captions (including fullscreen) update, then restore defaults without clearing delay.
- **SC-003**: After reload, a previously saved movie+track delay restores, a different track starts at zero, and global style restores across movies.
- **SC-004**: With storage blocked or corrupt, playback still works and defaults/session adjustments apply without a crash or stuck player.
- **SC-005**: Chromium and Firefox receipts against real-library-layout fixture copies either PASS the acceptance checklist or are explicitly **Could not run** with open acceptance work — never silently substituted by unit tests alone.
- **SC-006**: Applicable web gates for in-scope web changes exit 0; scope-empty C# gates remain reported as SKIPPED (scope-empty), never PASS; no threshold lowering.

## Assumptions

- Taste and provisional values tagged `[assumed]` in `specs/DEV-500/ASSUMPTIONS.md` (Q3 profile, Q4 interaction details, Q5 overlay mechanics, Q6 storage details, Q7 presentation, and Phase 2 step 6 plan-challenge amendments) are Patron rulings, not Quill inventions.
- Series owner rulings and ticket Scope/Acceptance remain binding via brief/grill (historical recon snapshot); current `recon-DEV-500:7-16` does not re-prove those lines and must not be cited as if it does.
- Palette `design.md` is delivered with corrections pending (Palette-owned); Quill does not invent those visuals (`brief.md` Q7/Q10; plan-challenge amendment).
- DEV-503 CC shell is the dependency; historical recon found it absent from pinned `origin/main` at `7243314f`. Implementation remains gated on merge + refreshed recon (`brief.md` Q2). Current provisional integration points: `BrowserPlayer.tsx:73-75,100,156,199-207` and `SubtitleSelector.tsx:26-38,60-79` (historical integration evidence — refresh at pickup).
- Screenshot/reference fidelity is outstanding visual verification, not waived by the provisional profile.
- Backend movie/track identity stability across rescans remains unproven; identity codec assumption never waives real reload persistence acceptance.
- No new glossary entry or ADR (`brief.md` Q10).
