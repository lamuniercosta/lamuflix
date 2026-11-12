# DEV-500 — Options tab and caption overlay design

Status: Delivered — plan-adjudication corrections applied. Owner: Palette. Worktree: `F:/Dev/LamuFlix.worktrees/feature-500-spec`, branch `feature/500-spec`.
Authority: grill-closed `brief.md` Q1–Q10 plus Phase 2 step 6 amendment items 1–7 as the recorded decision basis; ticket Scope/Acceptance snapshot `recon-DEV-500:28-57` with owner rulings `:63-71` is historical evidence preserved via the brief/grill, not the current 16-line recon (which does not contain that snapshot); `CONCLUSIONS.md` Q1–Q10 plus plan-challenge seven-item ratification; `ASSUMPTIONS.md` Q3–Q7 plus amended items; `spec.md` FR-001–FR-019; `specs/PRODUCT.md` §§3–5. No new dependency, layer, schema, API, LocalPlay, secrets, or `Process.Start` change. Provisional chrome/focus palette, 44px targets, neutral checker backdrop, no transitions, and enum tokens are Patron-ratified `[assumed]` per amendment item 5–6; screenshot fidelity is unverified and unwaived (`brief.md` Q3/Q7/Q10): this design works within the ratified provisional profile and never claims inspected-reference equivalence.

Skills applied: `frontend-design` (plan → review → build), `impeccable` in Operate mode (task surface: complete a setting quickly while video keeps playing), `web-design-guidelines` checklist from the skill body only. The guidelines fetch URL was not retrieved per the step-4 root constraint (no reads outside the named roots).

## 1. Route map

No new route, no URL parameter, no deep link.

- Existing surface: movie details → browser player → CC panel → Options tab.
- Tab state (`Subtitles` / `Options`) is local panel state owned by the delivered DEV-503 CC shell, not a route. This design adds no query string or hash for tab, delay, or style.
- Fullscreen does not change the route. The overlay and the preview both render inside the existing media-chrome fullscreen subtree (`brief.md` Q5).

## 2. Design plan

Operate surface in a dark cinema room. The captions are the hero; the panel stays quiet so the video keeps attention. One memorable move: the preview box is a miniature of the real overlay (same component and style math), not a mock swatch — what is tuned is what plays.

- Color (panel chrome is quiet and provisional per Patron-ratified amendment item 5, reference-unverified; caption colors come from the user profile, defaults below):
  - `panel` `#141417` — Options panel surface.
  - `panel-line` `#2A2A30` — section dividers and control borders.
  - `panel-text` `#EDEDEF` — labels and values.
  - `dim` `#A7A7B3` — help line and secondary text.
  - `focus` `#7DD3FC` — visible focus ring (`2px solid` outline + `2px` offset).
  - Caption defaults are user data, not chrome: text `#FFFFFF`, background `#000000` at 75% (ticket-mandated).
- Type: system stacks only, per `brief.md` Q7. One UI voice; the three caption Font choices reuse the same stacks:
  - System sans-serif: `system-ui, sans-serif` (UI + caption default).
  - Serif: `Georgia, 'Times New Roman', serif`.
  - Monospace: `ui-monospace, monospace`.
  - Delay value and percentages use tabular numerals where the platform provides them; display format is fixed by spec (`0.0 s`, integer `%`).
- Layout: single-column settings stack inside the existing CC panel width. Preview first in Caption style, then one control per row (label left, control right on wide panel; stacked on narrow). Timing is a compact cluster: label, stepper row, help line, reset.
- Principles: reuse the DEV-503 tab/focus/dismissal behavior unchanged; keep ticket labels and the exact help line verbatim; every control is a native labeled element; the overlay never intercepts pointer or keyboard; text and background opacity stay independent layers.

Plan review against the brief: the first draft leaned on a generic card grid with an eyebrow label above each section and a gradient preview wash. Cut all three — the content is a settings sequence, not a marketing page, and the preview must show the real caption box on transparency, not decoration. What remains is deliberately plain panel chrome with one cinematic touch: the line-fitted caption boxes with theatrical edge treatments.

## 3. Wireframes

### 3.1 Options tab

```text
+--------------------------------------------------+
| CC panel                        [Subtitles|Options]|  <- tabs: DEV-503 shell-owned
+--------------------------------------------------+
| Subtitle timing                               |
|   Subtitle delay                              |
|   [ - ]   [ 0.0 s ]   [ + ]                   |
|   Negative shows subtitles earlier, positive  |
|   later.                                      |
|   [ Reset timing ]                            |
+--------------------------------------------------+
| Caption style                                 |
|   Subtitle preview                            |
|   +----------------------------------------+  |
|   | The quick brown fox jumps over the     |  |
|   | lazy dog.                              |  |
|   +----------------------------------------+  |
|   Font            [ System sans-serif  v ]     |
|   Text size (%)   [ slider ]  [ 100% ]         |
|   Text color      [ swatch #FFFFFF ]            |
|   Text opacity    [ slider ]  [ 100% ]         |
|   Background color[ swatch #000000 ]            |
|   Background opacity [ slider ]  [ 75% ]       |
|   Edge style      [ None           v ]         |
|   [ Reset to default ]                        |
+--------------------------------------------------+
```

Notes:

- Exact visible and programmatic labels: `Subtitle delay`, `Reset timing`, `Caption style`, `Subtitle preview`, `Font`, `Text size (%)`, `Text color`, `Text opacity`, `Background color`, `Background opacity`, `Edge style`, `Reset to default`. Help line exactly `Negative shows subtitles earlier, positive later.`
- Text color and Background color rows each show a native color input with its own label; Text opacity and Background opacity rows each show a stepped range (5-point steps) with a `%` readout and its own label; Text size (%) shows a stepped range (10-point steps) with a `%` readout. Font and Edge style are native selects with the ratified choice sets. The four color/opacity labels stay separate in this diagram, in §6, and in implementation; they are never merged into combined rows.
- Timing stepper: `-` / value / `+` buttons; Shift+click is 0.5 s, ordinary click 0.1 s. Value is a read-only status, not an editable textbox.
- With captions Off, this whole tab stays editable (style and preview keep working); timing cluster disables only when there is no usable selected track (see §5).

### 3.2 Caption overlay (windowed and fullscreen)

```text
+--------------------------------------------------+
|                    video                         |
|                                                  |
|              +------------------+                |
|              | line-fitted cue  |                |  <- box fits each line,
|              +------------------+                   never full-video tint
|              +------------------+                |
|              | second cue,      |                |  <- multiple cues stack
|              | wraps two lines  |                   upward, centered
|              +------------------+                |
+--------------------------------------------------+
```

- Bottom-centered stack inside the media-chrome controller subtree so fullscreen containment follows the video, in both windowed and fullscreen play.
- Each cue is its own line-fitted box (background painted per cue line box, padding around glyphs). Stack grows upward; newest cue nearest the bottom safe area.
- Preview reuses this same box geometry at panel scale: the preview sample renders through the shared `CaptionOverlay` style calculation with identical font/size/color/opacity/edge mapping.

## 4. Component tree

Shell nodes marked DEV-503-owned are the historical `recon-DEV-500` §Options-tab integration snapshot as preserved via the brief/grill (absent from the current 16-line recon), treated here as integration points, not discoveries of this step.

```text
BrowserPlayer (existing; single useSubtitleOptions instance owner, mounts live overlay, wires preference lifecycle/media/shortcuts)
+- SubtitleSelector / CC shell (DEV-503-owned: tabs, focus/dismissal, selection/Off/retry)
|  +- Subtitles tab (DEV-503-owned; untouched)
|  +- Options tab (new content in delivered shell)
|     +- SubtitleOptions (new, props-only: no storage/media access; receives state/actions via props)
|     |  +- TimingCluster (delay stepper, help line, Reset timing)
|     |  +- StyleSection (preview + Font/Text size (%)/Text color/Text opacity/Background color/Background opacity/Edge + Reset to default)
|     |     +- CaptionOverlay -- preview mode (shared renderer, sample text; same style calculation as live)
|     +- (selection/Off/retry behavior preserved; no second panel)
+- CaptionOverlay -- live mode (shared renderer, props-only, active cues; pointer-transparent)
+- media session wiring (useSubtitleOptions single instance, useSubtitleCues; selected native track mode `hidden` explicitly)
```

New modules map to `brief.md` Q8 / `spec.md` FR-015: `SubtitleOptions.tsx`, `CaptionOverlay.tsx`, `useSubtitleOptions.ts`, `useSubtitleCues.ts`, `subtitlePreferences.ts`, `subtitleTiming.ts`, `subtitleCopy.ts` (labels/help/sample), `captionOverlay.css`, `subtitleOptions.css`.

## 5. States

| Surface | State | Presentation |
|---|---|---|
| Timing cluster | Default (pair never saved) | Value `0.0 s`; Reset timing enabled but no-op-safe (clears only this pair). |
| Timing cluster | Customized | Signed one-decimal value (e.g. `+0.5 s`, `-1.0 s`); Reset timing clears only this movie+track pair; style untouched. |
| Timing cluster | No usable selected track | Stepper, Reset timing, and G/H disabled / no-op. Style section and preview stay editable. |
| Timing cluster | Captions Off | Live overlay hidden; saved delay retained; Options tab stays open and editable. |
| Style section | Default | Provisional profile (§6); preview sample on neutral `12px` checker (`#2A2A30`/`#1E1E22`, no tint wash). |
| Style section | Customized | Every control updates preview and live captions immediately, fullscreen included; Reset to default restores the §6 profile without touching delay. |
| Style section | Invalid stored profile | Whole-profile fallback to §6 defaults; no partial merge of corrupt values. |
| Overlay live | No active cues at media time | Renders nothing (takes no space, intercepts nothing); selected native track stays `hidden`; preview sample still renders in Options. Half-open `[start, end)` applies: cue ending at current time is already absent. |
| Overlay live | Late cue readiness / seek / rate change | Resyncs on the next event/frame; playback-only frame loop updates state only when the active-cue set changes. |
| Overlay live | Negative effective start | Shows the portion overlapping time zero; cues already ended are omitted (half-open `[baselineStart + delay, baselineEnd + delay)`). |
| Overlay live | Multiple cues / line breaks | Stacked line-fitted boxes; breaks preserved; all text literal as plain text including markup-looking strings, no HTML interpretation. |
| Storage | Blocked / denied / quota / corrupt | Defaults apply; session adjustments stay in memory for this mount; no `Saved` claim is shown; failed removal never promises the reset survives reload. Playback continues. |
| Storage | Stale-switch race | Switching movie/track never writes the old pair's delay under the new pair's identity. |

Loading: there is no skeleton for Options — the panel renders from defaults instantly and applies stored values when validated. Error: no blocking error UI; storage failure is silent-by-design except that persistence claims are withheld. Empty: overlay empty state is absence (correct for timed captions); the preview is never empty.

## 6. Data needs

No endpoint, no DTO, no OpenAPI change, no generated-TypeScript change. Compass contract check binds to this statement: there is no contract delta on this ticket. Cog regen is not triggered.

| Key | Scope | Value |
|---|---|---|
| `lamuflix:subtitle-style:v1` | Global, device-local | Object: `font` enum (`system-sans`/`serif`/`monospace`), `textSize` 50–200 step 10 (%), `textColor` hex, `textOpacity` 0–100 step 5 (%), `bgColor` hex, `bgOpacity` 0–100 step 5 (%), `edge` enum (`none`/`drop-shadow`/`raised`/`depressed`/`outline`). Defaults per table below. Invalid object → whole default profile. Four color/opacity fields keep separate labels everywhere; text and background opacity stay independent layers. |
| `lamuflix:subtitle-delay:v1:${JSON.stringify([numericMovieId, trackId])}` | Per movie+track pair | Signed integer milliseconds, no product clamp. `numericMovieId` is the canonical numeric movie ID already consumed by playback; `trackId` is the exact opaque API track ID with no position/URL use, no case folding, no Unicode normalization. Invalid → `0`. Never-saved pair starts at `0`. Resets remove only their own key. Backend track-ID stability across rescans is unavailable evidence, not a guarantee; no new API/schema field is added. |

Provisional default profile (from `ASSUMPTIONS.md` Q3; screenshot fidelity unverified):

| Setting | Default |
|---|---|
| Font | System sans-serif |
| Text size | 100% |
| Text color | `#FFFFFF` |
| Text opacity | 100% |
| Background color | `#000000` |
| Background opacity | 75% |
| Edge style | None |

Concrete edge mappings (CSS `text-shadow` only; numeric geometry set here under Q7):

- None (`none`): `text-shadow: none;`.
- Drop shadow (`drop-shadow`): `text-shadow: 2px 2px 2px rgba(0,0,0,0.9);`.
- Raised (`raised`): `text-shadow: 1px 1px 0 rgba(255,255,255,0.85), -1px -1px 1px rgba(0,0,0,0.85);` (light below-right over dark above-left).
- Depressed (`depressed`): `text-shadow: -1px -1px 0 rgba(255,255,255,0.55), 1px 1px 1px rgba(0,0,0,0.9);` (dark below-right over light above-left, opposing Raised).
- Outline (`outline`): `text-shadow: -1px 0 0 rgba(0,0,0,0.9), 1px 0 0 rgba(0,0,0,0.9), 0 -1px 0 rgba(0,0,0,0.9), 0 1px 0 rgba(0,0,0,0.9);` (four-direction, no blur).

Numeric spacing and caption scale (Q7 geometry):

- Panel spacing: panel padding `16px`; section gap `20px`; control row gap `12px`; preview padding `12px`; preview margin-bottom `12px`; cue line-box padding `4px 8px`; cue stack gap `6px`.
- Base caption size: `20px` at `100%` for a `640px`-wide video box.
- Shared caption calculation (identical for live overlay and preview): `surfaceBase = 20px * (surfaceWidthPx / 640)`; `clampedBase = clamp(surfaceBase, surfaceBaseMin, surfaceBaseMax)`; `captionPx = clampedBase * (textSizePct / 100)`. The surface clamp is applied BEFORE the user percentage; the user percentage is applied LAST and is never re-clamped within the 50–200% range, so the Text size control always changes the rendered size on every surface. Surface inputs: live windowed uses the CSS width of the video box with base clamp `14–34px`; live fullscreen uses the CSS width of the fullscreen video box with base clamp `18–48px`; preview uses the fixed `320px` panel reference width with base clamp `14–20px` (at `100%` the preview base is `14px`: `20 * (320/640) = 10 → clamped to 14`). Line height `1.35` scales with `captionPx`; background boxes fit each cue line box, never full-video tint.
- Representative results (monotonic at every step — the control is never flattened by clamping): preview (`320px` ref) `50% → 7.0px`, `100% → 14.0px`, `200% → 28.0px`; live windowed (`640px` ref video box) `50% → 10.0px`, `100% → 20.0px`, `200% → 40.0px` (user-chosen extremes intentionally reach outside the `14–34px` surface-base band, which bounds surface scaling only, never the user's percentage); live fullscreen (`1920px` ref video box, base `20 * (1920/640) = 60 → clamped to 48`) `50% → 24.0px`, `100% → 48.0px`, `200% → 96.0px`. The preview sample renders through this same calculation at panel scale, so what is tuned is what plays.

Cue semantics bound to this presentation: selected native track mode is explicitly `hidden`; effective interval is `[baselineStart + delay, baselineEnd + delay)` start-inclusive/end-exclusive (a cue ending at `0` is absent, a cue starting at `0` is active); all subtitle text renders literally as plain text including markup-looking strings with no HTML interpretation.

Preview sample: `The quick brown fox jumps over the lazy dog.`

## 7. Accessibility and keyboard

- All controls are native labeled elements with separate programmatic labels: buttons (`-`, `+`, `Reset timing`, `Reset to default`), selects (Font, Edge style), ranges (Text size (%), Text opacity, Background opacity), color inputs (Text color, Background color). Every control has its own programmatic label; readouts (`0.0 s`, `%`) are `aria-live="off"` status text, not announcements.
- Visible focus on every control: `2px solid #7DD3FC` outline with `2px` offset, never removed (Patron-ratified provisional focus, reference-unverified). Tab order follows the wireframe top to bottom; the shell's tab/focus/dismissal behavior is reused unchanged.
- Hotkeys G (earlier) and H (later) fire only while playing, inside the existing player shortcut scope, ignoring any focused `input` (including range/color), `textarea`, `select`, `contentEditable` target or dialog context including descendants, and `Ctrl`/`Alt`/`Meta` modifiers. 100 ms per press; Shift+G/H is 500 ms. No-op with no usable selected track or when captions-relevant guards apply.
- Overlay captions are `aria-hidden`, pointer-transparent, non-interactive, and never take focus. They are not an aria-live stream.
- Reduced motion and transitions: no entrance animation on the panel or overlay; style changes apply instantly with no CSS transitions on panel or caption boxes (Patron-ratified provisional no-transition rule, reference-unverified). Honor `prefers-reduced-motion` by keeping it that way (no motion to reduce).
- Preview backdrop: neutral checkerboard of `12px` squares (`#2A2A30` alternating `#1E1E22`, provisional neutral pair in the ratified chrome family) behind the line-fitted caption boxes; no tint wash. Numeric checker geometry is Q7 geometry set here.
- Contrast: panel chrome uses provisional `#EDEDEF` labels and `#A7A7B3` help text on `#141417` with `#2A2A30` dividers. No measured AA PASS is claimed in this design. Panel contrast, responsive/fullscreen geometry, `44px` minimum target size, and no-transition behavior are verified in Chromium/Firefox browser receipts, not inferred from these values. Caption contrast is user-controlled by design; the default white text at 100% on black at 75% is the highest-contrast combination in the provisional profile.
- Responsive and targets: single column down to `320px` panel width; label/control rows stack below `360px`; caption font follows the §6 scaling/clamp rule so fullscreen and windowed share geometry. Every interactive target (`-`, `+`, resets, selects, ranges, color swatches) is at least `44px` in its hit area (Patron-ratified provisional minimum, reference-unverified).

## 8. Open items (not invented here)

- Reference screenshot fidelity remains unverified until `ref-options-timing.png`, `ref-options-style-1.png`, `ref-options-style-2.png` are inspected; any discrepancy needs an explicit Patron ruling (owner checkbox if ticket-changing). This design never claims inspected-reference equivalence.
- Numeric shadow/edge geometry, spacing, base size, and video/preview scaling/clamps are specified in §6 (shared calculation with explicit surface inputs, before-percentage clamp order, and representative 50/100/200% results) and §7 within Q3/Q7 bounds; the preview-scale underspecification is closed by that §6 formula, so no open geometry remains in this artifact.
- Any further taste beyond this artifact needs an explicit Patron ruling (`brief.md` Q7/Q10). No extra taste decision was taken in these corrections beyond the Q7 geometry remit.
