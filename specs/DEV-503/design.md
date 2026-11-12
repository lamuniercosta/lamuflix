# DEV-503 — Browser player controls design

Status: PLAN FROZEN — Keel status-only propagation, 2026-11-12, after independent round-1 validation; see brief.md freeze decision. Gate 1 and runtime visual acceptance remain pending. Authority: `specs/DEV-503/brief.md` Q1–Q8 (Patron approved all nine Q8 points), `CONCLUSIONS.md` Q1–Q8, `ASSUMPTIONS.md`, recon `recon-DEV-503` lines 19–26, 28–90, and source-fact supplement 186–214, `PLAN_ADJUDICATION.md` round 1.
Mode: Operate — the visitor completes playback tasks. Precision and native media-chrome expectations outrank expression.

Reference assets (named by ticket; availability unverified at design time; `web/node_modules/media-chrome` absent per recon 214):
`ref-subtitles-tab.webp` (CC panel, Subtitles tab with Off, language search, results with match % and download count),
`ref-options-timing.png`, `ref-options-style-1.png`, `ref-options-style-2.png` (deferred Options content — shell context only),
`current-player-time-bug.webp` (current bug: `0:121:17:05`, no separator, no menus).
Obtaining the assets is a T001/T015 pickup prerequisite. This design matches the delivered shell and controls against the first reference and the time fix; it fabricates none of the deferred search/timing/style content. Comparison states which shell areas are delivered and which depicted future contents are out of scope.

## Design direction

Grounded in the subject: a dark cinema room, not a SaaS card kit. The video is the hero; controls are a quiet projection-booth strip that gets out of the way. One memorable move: the CC panel as a bottom-right "cue card" docked over the video, with a two-tab header and a checkmark track list — everything else stays visually silent.

- Color: stay on the incumbent player surface using existing CSS tokens (`web/src/index.css:3-45`, `web/src/features/playback/BrowserPlayer.css:1-5,30-47,49-85`); no design hex/RGBA literals. Accent reserved for the active checkmark and the existing focus-visible ring only. No new brand hue.
- Type: inherit app type; tabular numerals for the combined time readout so `0:12 / 1:17:05` does not jitter. Sentence case throughout; no all-caps labels, no eyebrow over the panel.
- Layout: single bottom control strip; panel and gear menu are overlays inside the fullscreen player subtree, bottom-right anchored, never page-level modals. At narrow widths (including 320px operability `[assumed]`) the strip wraps, seek takes its own row, and the overlay constrains width/height to the player with internal content scroll, keeping every control including volume reachable. No new breakpoint or size threshold is invented here.
- Principles: honest shell (no fake search results or dead sliders), one surface open at a time, keyboard parity with pointer, fullscreen-safe DOM.

## Route map

No new route, no URL change. The player lives inside the existing movie-details route that renders `MovieDetailsView` → `BrowserPlayer`. Panel tab, selected track, open/closed, and speed are local component state only — not reflected in the URL, not persisted. Rationale: per brief Q5/Q6, no saved preference and no new API in this ticket.

## Layout wireframes

Parenthetical lines inside the wireframes below (loading/empty/error slots, extension room) are design annotations, never rendered copy. No audio/quality placeholder entries and no fake actions are rendered.

Windowed player, panel closed:

```text
+--------------------------------------------------+
|  MovieDetailsView                                |
|  +--------------------------------------------+  |
|  | video (16:9)                               |  |
|  |                                            |  |
|  | [CC panel / gear menu overlay here,        |  |
|  |  bottom-right when open]                   |  |
|  +--------------------------------------------+  |
|  | control bar (wraps at narrow widths;       |  |
|  | seek takes its own row):                   |  |
|  | [Play] [seek =====o-----] [0:12 / 1:17:05] |  |
|  | [Mute][vol --o--] [CC] [gear] [fullscreen] |  |
|  +--------------------------------------------+  |
|  status line (polite role=status: loading /     |
|  buffering / autoplay-blocked / ended)            |
|  failure line (role=alert) with error actions     |
|  (Retry, optional Open Local when enabled)        |
+--------------------------------------------------+
```

CC panel open, Subtitles tab (bottom-right over video, bounded to player with internal scroll):

```text
| video                                        |
|                         +------------------+ |
|                         | Subtitles | Options| |
|                         |            | [x]  | |
|                         |------------------| |
|                         | v Off            | |
|                         |   English [en]   | |
|                         |   Portuguese [pt]| |
|                         | (loading / empty /| |
|                         |  error+Retry here)| |
|                         +------------------+ |
| control bar (CC trigger aria-expanded=true)    |
```

Gear menu open (bottom-right, same anchor, replaces panel; exact media-chrome settings-menu composition verified at T001 pickup — unverified here):

```text
| video                                        |
|                         +------------------+ |
|                         | Playback speed   | |
|                         |   0.5x             | |
|                         |   0.75x          | |
|                         | v 1x (Normal)    | |
|                         |   1.25x ... 2x   | |
|                         | (room: audio /   | |
|                         |  quality later)  | |
|                         +------------------+ |
```

## Component tree

```text
MovieDetailsView (route host; bounded duplicate-Play integration only)
└─ BrowserPlayer (hotkey owner; bubble handler respects defaultPrevented)
   ├─ .browser-player-frame
   │  └─ MediaController (noHotkeys per BrowserPlayer.tsx:159; fullscreen subtree root)
   │     ├─ video.browser-player-video[slot=media] (+ active caption <track> mount; Off clears the mount)
   │     ├─ MediaLoadingIndicator (incumbent indicator; no invented slot claimed)
   │     ├─ MediaControlBar
   │     │  ├─ MediaPlayButton (in-bar play is preserved)
   │     │  ├─ MediaTimeRange (seek; own row at narrow widths)
   │     │  ├─ MediaTimeDisplay[showDuration] (replaces Time + Duration pair)
   │     │  ├─ MediaMuteButton + MediaVolumeRange
   │     │  ├─ CcTrigger (button "Subtitles" [assumed], aria-expanded, aria-controls)
   │     │  ├─ GearTrigger (media-chrome settings affordance, "Settings" [assumed]; exact export verified at T001)
   │     │  └─ MediaFullscreenButton (real control composed; no fullscreen placeholder)
   │     ├─ CcPanel (non-modal labelled region; never role=dialog per BrowserPlayer.tsx:35-38 exclusion)
   │     │  ├─ tablist: Subtitles tab / Options tab + close button ("Close subtitles panel" [assumed])
   │     │  ├─ SubtitlesTabPanel (radiogroup/radio track list — replaces SubtitleSelector <select>)
   │     │  └─ OptionsShellPanel ("Subtitle timing and style options will appear here." [assumed])
   │     └─ SpeedMenu (media-chrome settings-menu composition; items 0.5–2 step 0.25 with 1x (Normal) [assumed]; exact exports/imports/open-state verified at T001)
   ├─ status live region (polite role=status for loading/buffering/autoplay-blocked/ended; distinct role=alert only for playback failure per BrowserPlayer.tsx:189-231)
   └─ error actions (error-only: Retry, optional Open Local rendered only when local play is enabled and callback exists)
```

Duplicate-Play cleanup is remove-if-present at the recon-established owner (`BrowserPlayer.tsx:233-242` per recon 195), confirmed at T001 pickup with the DEV-493 overlap check, while preserving the in-bar play button; no assumption that the duplicate belongs to `MovieDetailsView`. Dead `<select>` CSS/test selectors are removed while behavior stays covered. Co-located extraction (`SubtitlePanel.tsx` / `subtitlePanel.test.tsx`) remains the optional Q7 pair decided at planning time, not here. One authoritative local open-surface state, no new state library. Copy lives in the approved `playerCopy.ts` / `subtitleCopy.ts` modules; `useSubtitlePlayback.ts`, `subtitleContent.ts`, `subtitleTrackModes.ts`, `useBrowserPlayer.ts`, and generated `web/src/api/types.ts` stay read-only.

## Component states

| Surface | States | Presentation |
|---|---|---|
| Time | loading / ready | `0:12 / 1:17:05` with spaced separator; never the glued `0:121:17:05` |
| CC trigger | always visible | Enabled even with zero tracks or errors; `aria-expanded` reflects panel |
| Subtitles tab | ready + tracks | `radiogroup`/`radio` with `aria-checked`; Off row + served rows in existing order/labels; check on active; single tab stop; arrows move/select, `Space`/`Enter` activate |
| Subtitles tab | zero tracks / loading | Honest empty/loading copy, playback unaffected, CC stays visible |
| Subtitles tab | list error | Status text + Retry button; keeps existing `onRetryList` semantics |
| Subtitles tab | content error | Status line, selection cleared to Off-equivalent, no silent fallback |
| Options tab | shell only | Reachable tab, copy `Subtitle timing and style options will appear here.` [assumed]; no sliders, no fake results |
| Gear menu | closed / open | Exact items `0.5, 0.75, 1, 1.25, 1.5, 1.75, 2` with current-speed check sourced from live media state (ratechange/source/movie/metadata transitions resynchronize); no persistence, no forced 1x reset |
| Overlays | mutual exclusion | Trigger toggles own surface; opening one closes the other; another trigger switches surfaces once |
| Lifecycle | movie change / unmount | Surfaces reset; focus restored only when it was inside the removed surface; unmount cleans listeners without focusing detached nodes or stealing destination focus; controls stay functional across fullscreen transitions |

## Data needs (endpoint + fields)

Web code consumes generated types only (`web/src/api/types.ts`); no new contract in this ticket. Per recon 190–192:

- Track list: `GET /api/movies/{id}/subtitles` → `SubtitleTrackDto[]` (operations at `types.ts:485-524`, path IDs `id:number`); `SubtitleTrackDto` fields `id:string`, `label:string`, `language:string`, `isDefault:boolean` (`types.ts:306-311`). Drives the Subtitles tab rows via `useSubtitlePlayback`; labels and order preserved as served.
- Caption bytes: `GET /api/movies/{id}/subtitles/{trackId}/content` (operation at `types.ts:525-`, path IDs `id:number`, `trackId:string`) via the `subtitleContent.ts:1-11` helper (types the route from generated `paths`, substitutes the movie ID, URI-encodes the track ID); `useSubtitlePlayback.ts:53-62` fetches selected content and reads response text, mounting an object URL on the active `<track>`. Off clears the mount; failures surface the existing content-error state. Existing `listGeneration`/`attemptRef` stale-result protection and owned-URL cleanup (`useSubtitlePlayback.ts:41-50,53-62,64-78,98-103,118-153,171-200`; `subtitleTrackModes.ts:21-33` listener cleanup) are preserved with no mandatory `AbortController` rewrite.
- Playback state: local only — current speed from the media element, panel/menu open state synchronized to verified native menu state (no invented API or mirrored unsynchronized state; mechanism confirmed at T001 pickup), active tab (`Subtitles` default on fresh open per `[assumed]` Q5). No persistence, no query params, no new DTO.
- No shipped-on-main lineage is claimed. No helper names, element slots, or action labels beyond recon 190–214 are asserted.

## Keyboard navigation and a11y

Per Q4 contract as refined by Q8; brief interaction section is authoritative, this is its visual binding:

- Triggers are named buttons (`Subtitles`, `Settings`, `Close subtitles panel` [assumed]); visible focus on every control, tab, row, and menu item using the incumbent focus-visible rules (`BrowserPlayer.css:7-10,78-81`). Focus-ring appearance is proven in Chromium, not by RTL computed CSS.
- Tabs use `tablist`/`tab`/`tabpanel` with roving `tabindex`, `Left`/`Right`/`Home`/`End`, automatic tab activation; the selected tab enters Tab order.
- Track list uses `radiogroup`/`radio`/`aria-checked` under one consistent pattern; Off plus every served track is keyboard reachable and operable, including keyboard-only track-to-Off in windowed and native fullscreen (proven at T016 with rendered/hidden caption evidence).
- `MediaController noHotkeys` is retained with rationale: hotkeys are owned by the `BrowserPlayer` bubble handler (`BrowserPlayer.tsx:35-58,100-159`) so panel/menu controls can consume their own activation/navigation keys first; the bubble handler respects `defaultPrevented`, yielding exactly one action. Preserved keys (`Space`/`K`, left/right 10-second seek, `M`, `F`) keep working outside consumed controls, including on tabs, rows, close button, and elsewhere. Existing editable exclusions (inputs, textarea, select, contenteditable per `:35-38`) are retained; no blanket panel exclusion, no capture-phase playback handling, no promise over browser-owned `Escape`.
- Opening a surface moves focus into it. Page-delivered `Escape` closes the active surface and returns focus to its trigger before application fullscreen-exit handling; no control over browser-owned `Escape` is promised. A `fullscreenchange`-to-exited fallback closes stale surfaces and restores focus to a connected trigger, else the connected player host. Outside `pointerdown` uses `composedPath`, with the active surface plus its trigger as inside; control-bar/video content outside that boundary dismisses (in fullscreen, outside means reachable content inside the fullscreen subtree). `pointercancel` alone does not dismiss. Destination focus is preserved on outside dismissal. One coordinated effect cleans up document/fullscreen subscriptions; document capture is not forced for control activation.
- Status and error text keeps distinct semantics: polite `role=status` for loading/buffering/autoplay-blocked/ended versus `role=alert` for failures (per `BrowserPlayer.tsx:189-231`); never `role=alert` called polite and no new roles on old output.
- Reduced motion extends the incumbent block (`BrowserPlayer.css:88-92` under `prefers-reduced-motion: reduce`) to the new surfaces; the block is extended, not duplicated. Actual motion behavior is proven in Chromium; RTL asserts roles, state, focus movement, and keyboard behavior only. Touch targets stay at control-bar height; the panel is reachable and fully operable at 320px width without page-scroll coupling (layout proven in Chromium; RTL does not prove CSS width).

## Pickup verification and evidence split

Exact `media-chrome` 4.19.4 settings-menu exports, import paths, attributes/events, label customization, rates, open-state, anchor, and fullscreen behavior remain explicitly unverified (tracked manifests pin the version; installed exports unconfirmed per recon 212–214) and must be verified by Wisp in T001 before production edits, recording the chosen composition and synchronization mechanism; STOP for recon/decision on unsupported seams rather than guessing imports or replacing `media-chrome`. Reference asset availability is likewise unverified until pickup/acceptance. Chromium proves layout, focus appearance, motion, and native menu internals; RTL proves roles, state, and keyboard behavior.

