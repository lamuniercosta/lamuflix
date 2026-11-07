# DEV-455 Design — In-page browser player (Media Chrome)

Status: draft amended in the one authorized plan-challenge pass. Sources: `brief.md` Q1–Q12
plus Plan challenge decision register, `CONCLUSIONS.md` Plan challenge adjudication
(Patron 94c06b57), `spec.md`, `ASSUMPTIONS.md`, facts from note `recon-DEV-455`.
Grill closed; this document adds no behavior, only layout, states, and labels.
Proposed wording refinements are marked `[assumed]` taste for Patron record.
Existing approved taste in `ASSUMPTIONS.md` is preserved.

## 1. Route map and URL structure

No new route. No modal. One player region lives inside the movie details page.

- `/` library — cards keep their existing card-level Play control.
  Card Play navigates to details and carries player intent (existing
  app-navigation transport only), then opens the same in-page player.
- `/movies/{id}` details — primary Play opens the in-page labelled player
  region and requests the DEV-456 stream under that user gesture.
- Player intent survives client-side navigation; a direct load of
  `/movies/{id}` without intent shows the same details page with Play
  available but the player region not mounted (Closed/idle initial state
  only). No close affordance is rendered.

## 2. Page layout

### 2.1 Details, player not yet opened (default, Closed/idle initial only)

```text
+----------------------------------------------------------+
| Details header: title, year, actions                      |
| [Play]  [Open in BS Player]*  ...existing actions         |
+----------------------------------------------------------+
| Synopsis / metadata (unchanged)                           |
| Trailer section (unchanged YouTube embed)                 |
| Status region (existing local-launch alerts, unchanged)   |
+----------------------------------------------------------+
* secondary action rendered only when localPlayEnabled true.
```

### 2.2 Details, player open

```text
+----------------------------------------------------------+
| Details header (unchanged)                                |
+----------------------------------------------------------+
| <section aria-label="Movie player: {title}">              |
|   +-------------------+-------------------------------+  |
|   | video (16:9,      | control bar (Media Chrome):   |  |
|   | single active     | play/pause, seek timeline,    |  |
|   | element)          | elapsed/duration, volume,     |  |
|   |                   | speed 0.5/1/1.5/2, fullscreen |  |
|   +-------------------+-------------------------------+  |
|   | state line: loading / error + [Retry] (+ local    |  |
|   | option shown exactly when capability true,       |  |
|   | hidden otherwise)                                 |  |
| +-----------------------------------------------------+  |
+----------------------------------------------------------+
| Synopsis / metadata pushed below (in-page space accepted) |
+----------------------------------------------------------+
```

Focus moves to the labelled player region when it opens. Page navigation
stays usable; unmount, navigation away, or movie change tears the player
down. No close affordance exists, so there is no closing or
focus-return-on-close behavior. Exactly one active video element at a time.

### 2.3 Card Play

Card layout unchanged. Card Play keeps its existing position and navigates to
`/movies/{id}` with player intent; the details page then matches §2.2 and
attempts playback, keeping an explicit Play control when autoplay is blocked.

## 3. Component tree

New UI stays under the existing feature structure; names are proposed,
behavior follows `brief.md` Q9.

```text
MovieDetailsView.tsx
+-- PlayButton (existing, retargeted: opens browser player, never POSTs /play)
+-- OpenInBsPlayerButton (existing launch wiring; rendered only when
|   capability localPlayEnabled is true; sole caller of usePlayMovie POST)
+-- BrowserPlayer (new, web/src/features/playback/)
|   +-- PlayerRegion (section with accessible label, focus target)
|   +-- MediaChrome chrome (play/pause, seek, time, volume, speed, fullscreen)
|   +-- PlayerStateLine (loading / buffering / ended / error + Retry)
|   +-- useBrowserPlayer (new hook: source, native events, teardown guards)
+-- usePlaybackCapabilities (new hook: loading / true / false / failure)
MovieCard.tsx (existing card Play -> navigate + player intent only)
useAppNavigation.ts (existing transport for card player intent only)
```

No new project, top-level folder, or state library. Media Chrome is the only
new npm dependency (exact stable version at implementation pickup, Q1).

## 4. Component states

### 4.1 Player region

| State | What the user sees |
|---|---|
| Closed / idle (initial only) | No video element mounted. Details Play (and card intent) is the entry. No close affordance; this state exists only before first open, never as a dismiss target. |
| Loading | Labelled region with loading indicator; no premature failure during buffering. Stream requested under user gesture. |
| Playing | Video plus full Media Chrome bar; elapsed/duration advance from native events. |
| Paused | Same bar, paused frame; explicit Play resumes. Native `pause` event authoritative. |
| Buffering | Loading indicator over current frame; never a terminal error by itself. |
| Autoplay denied | Recoverable: explicit Play control remains, no terminal error shown for the denial alone. |
| Ended | Ended frame with explicit replay via Play/Retry from the beginning. No automatic loop. |
| Terminal error | Readable generic message + Retry (restarts the same source from the beginning) + local option shown exactly when capability is true, hidden otherwise. No automatic retry, no automatic local fallback, never auto-launch. Retry reuses the same source; credential compatibility and stream-URL lifetime/reuse are subject to the T001 STOP boundary — no signed-URL, auth-redesign, or re-resolution behavior is designed here. |
| Torn down | Unmount, navigation away, or movie change: outgoing playback pauses and its source clears before any incoming source attaches; resources release; stale async results are rejected/ignored; no overlapping audio; exactly one active video element. |

Empty and partial data: the player never renders without a movie id and a
delivered stream source. Capability loading or failure hides only the
secondary action; browser Play stays available. No persistent progress or
history is written in any state.

### 4.2 Secondary action visibility

| Capability | Open in BS Player |
|---|---|
| Loading or failed | Hidden; browser Play available. |
| `localPlayEnabled: false` | Hidden; browser Play available. |
| `localPlayEnabled: true` | Shown next to Play and inside terminal-error recovery. Only this action invokes local launch. |

Launch outcomes keep the existing generated-contract and current-UI meanings
for 204 / 403 / 404 / 409 / 422 / other / network; generic retry only where
that contract has no specific meaning. No status meanings are redefined here.

## 5. Data needs (endpoint + fields)

Binds Compass contract check and Cog OpenAPI to TypeScript regen. All web
types come from the generated chain; no hand-written duplicates.

- `GET /api/playback/capabilities` → 200 `application/json` →
  `PlaybackCapabilitiesDto { localPlayEnabled: boolean }` only. No paths,
  player configuration, or extra fields. DEV-455-owned.
- `GET /api/movies/{id}/stream` → DEV-456-owned stream URL used as the media
  source under gesture. Consumed, not defined, by this ticket; absent at recon
  base, so Phase B reconfirms the delivered generated contract before use.
- `POST /api/movies/{id}/play` → existing local-launch contract, invoked only
  by Open in BS Player when capability is true. Meanings preserved exactly.

## 6. Controls and keyboard

Media Chrome bar: play/pause, seek timeline, elapsed/duration display,
volume/mute, speed presets 0.5 / 1 / 1.5 / 2, fullscreen. Pointer and keyboard
share the same actions; native media events are authoritative.

Scoped shortcuts, active only while player-owned focus is inside the player
region, never in editable fields, dialogs, or other UI, and never as global
interception:

| Keys | Action |
|---|---|
| Space, K | Single toggle play/pause. Space on a focused control toggles exactly once with scroll prevented; region focus behaves equivalently. |
| Left, Right | Seek minus/plus 10s, clamped to seekable bounds |
| M | Mute/unmute |
| F | Toggle fullscreen |
| Escape | Exit browser fullscreen (existing dialog/overlay Escape behavior untouched) |

Seeks are ignored until valid seekable bounds exist; never throw or corrupt
timeline state. Seek past a bound clamps to that bound. Shortcuts do not fire from inputs,
textareas, selects, content-editable regions, or open dialogs.

## 7. BS Player button

- Visible copy `Open in BS Player`; shown only when `localPlayEnabled` is true.
- Descriptive accessible label names the movie, e.g. `[assumed]` taste:
  `Open {title} in BS Player (local playback)`.
- The label does not assert every configured launcher is BS Player.
- Terminal-error recovery always offers the same gated action when capability
  is true and hides it otherwise; it never auto-invokes launch from the
  browser path.

## 8. Accessibility

- Player wrapped in a labelled region (`Movie player: {title}`); focus moves
  there on open. No close affordance exists; teardown and focus wording refer
  only to unmount, navigation away, or movie change.
- Every control labelled, Tab-reachable, with visible focus at all times.
- Elapsed/duration exposed as text; state changes (loading, error, ended)
  announced through a polite live region; decorative chrome hidden from
  assistive tech where Media Chrome provides it.
- Full keyboard parity for every pointer action (§6); focus scope keeps
  shortcuts out of editable fields and dialogs.
- Error message states what happened in plain terms and what to do next
  (Retry; local option only when permitted). Errors never apologize and never
  cite invented statuses or codec promises.
- Reduced motion respected; no autoplay of motion outside user gesture;
  responsive layout down to mobile widths with the 16:9 frame preserved
  (mobile widths promise layout only). Fullscreen acceptance targets desktop
  browsers; no mobile-specific fullscreen fallback.

## 9. Copy

Visible copy stays `Play`, `Open in BS Player`, `Retry` (Q12). Palette
refinements, proposed as `[assumed]` taste:

- Primary: `Play` with label `Play {title} in browser`.
- Secondary: `Open in BS Player` with label `Open {title} in BS Player (local playback)`.
- Recovery: `Retry` with label `Retry playing {title} from the beginning`.
- Loading: `Loading player…`. Buffering keeps the current frame with the same text.
- Terminal error (generic, no invented status): `Couldn't play this movie. Check your connection and try again.` plus `Retry`.
- Autoplay-denied (not an error): `Playback is ready. Choose Play to start watching.`

## 10. Boundaries

Out for this design: transcoding, format conversion, captions, playlists,
casting, persisted resume/progress, history writes, range/resolver/security
redesign, launcher hardening, schema changes, new routes or modals, and any
second player or state library. Missing browser or sample at verification time
is Could not run, never PASS.
