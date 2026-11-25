# DEV-569 Phase 5 live Test URLs portal evidence (T012–T013)

Session: `Test URLs` portal at `http://localhost:3000/movies/512`, worktree
`F:/Dev/LamuFlix.worktrees/DEV-569`, branch `feature/DEV-569`, HEAD `d0c0509c`
(no production code edits in this phase). All measurements are session-only
page evaluation; no shipping instrumentation, exports or globals are used by
the application.

## Q2 tolerance — declared BEFORE any result was collected (2026-11-25T19:51:55Z)

- Q2 bounds are unchanged: boost 100–300%, gain = percent/100, 100% including
  Reset must be audibly and measurably unity, limiting only above 100%, about
  2x output at 200% on the same calibrated quiet stimulus below compression
  onset (`specs/DEV-569/CONCLUSIONS.md` Q2 / Patron 922d0753).
- Pre-declared pass tolerance for the retained ratios:
  - active 100% / independent native reference: 0.90–1.10;
  - Reset 100% / independent native reference: 0.90–1.10;
  - active 200% / active 100%: 1.80–2.20;
  - quiet-signal compressor reduction at 100%: 0 dB (transparent);
  - loud stimulus above unity at 200% must show compressor reduction < -3 dB
    and must not exceed 1.35x the 100% loud RMS (limiting engaged);
  - mute at 200%: RMS <= 0.001 (silenced).
- Because `test-portal-policy:6` requires a silent `sinkId.type = none`
  destination, "audible unity" is evidenced silently: gain exactly 1,
  compressor reduction 0 dB and matched RMS against the independent native
  reference at stock element volume 1; no audio is played to speakers.

## Portal safety receipts

- Navigated to `http://localhost:3000/movies/512` before applying any override.
- Applied the `test-portal-policy:6` override preserving the literal sink
  string with `String.fromCharCode(110,111,110,101)`; it printed `true`.
- Probe `new AudioContext()` before any audio returned
  `{"state":"running","type":"none"}`; the probe context was closed.
- Measurement elements remain unmuted (`muted = false`, `volume = 1`) as
  required for Web Audio measurement; all measured contexts use the silent
  `none` sink.
- The exact `test-portal-policy:7` stop-media command was run at the end of
  each phase and printed `true` both times; every Web Audio context created
  in the session was closed.

## T012 — calibrated portal receipt (session-only)

- Session technique: before activation, `window.AudioContext` was subclassed
  so the app-created `DynamicsCompressorNode` was tapped by an `AnalyserNode`
  (`fftSize 2048`) at its output, and `createMediaElementSource` was redirected
  to a separate unmuted WAV `<audio>` element at stock volume 1 (player video
  paused and never bound). A second, unmuted WAV `<audio>` element at stock
  volume 1 fed a standalone `createMediaElementSource -> AnalyserNode ->
  destination` reference in an isolated `sinkId.type = none` context with no
  gain/compressor nodes (Patron 922d0753). Identical 4 s, 44.1 kHz, mono,
  440 Hz PCM16 WAVs (amplitude 0.1 quiet, 0.9 loud) were used. All contexts
  reported `sinkId.type = none`.
- App graph contexts before activation: `0`; observer installed first. Native
  reference baseline RMS: `0.07065854078588765` (unmuted, volume 1).
- Active 100% (gain `1`, threshold `0`, ratio `1`, reduction
  `-0.0001342038594884798 dB`): compressor-output RMS
  `0.07059560194090228`, reference RMS `0.07052273925129937`, ratio
  `1.0010331800831398`. Unity PASS under the pre-declared tolerance.
- Active 200% (gain `2`, threshold `-12`, ratio `12`): compressor-output RMS
  `0.3014940061918399`, reference RMS `0.07066428197923583`, ratio to active
  100% `4.270719391899649`. **FAIL** against the pre-declared `1.80–2.20`
  tolerance. A concurrent tap on the real gain-node output measured
  `0.14131339714413566` (correct 2x), so the excess comes from the
  `DynamicsCompressorNode` output (Chromium makeup/upward gain at ratio 12),
  not from the gain mapping. This is recorded as an explicit blocker, not a
  passed delivery checkpoint; no production code was changed.
- Reset 100% via `Reset boost` DOM click: gain `1`, threshold `0`, ratio `1`,
  compressor-output RMS `0.07090074756204962`, ratio to reference
  `1.0055126578730882`, reduction `-0.0001342038594884798 dB`. Reset unity
  PASS.
- Mute at 200%: muted RMS `0`, unmuted RMS `0.30182282046379594`. PASS.
- Limiting above unity: loud 100% RMS `0.6355382763089303` (reduction
  `-0.04584216699004173 dB`); loud 200% RMS `0.4621970325000387` (reduction
  `-15.446866035461426 dB`, threshold `-12`, ratio `12`), ratio `0.727`. PASS
  against the pre-declared `<=-1.35x` / `< -3 dB` limits.
- Cleanup: both contexts reported `sinkId.type = none` before closing and
  `state = closed` afterwards; hook removed, Blob URLs revoked, all media
  paused, line 7 `true`.

## T013 — real-library movie 512 receipt (session-only)

- Page reloaded at `http://localhost:3000/movies/512` with remembered boost
  `200`, then all media paused; line 6 override printed `true` and a probe
  context reported `{"state":"running","type":"none"}` before playback.
- Pre-activation observer installed with `0` app contexts; player video was
  paused, unmuted, volume `1`, `currentSrc`
  `http://localhost:3000/api/movies/512/stream`; UI showed `200%` pending.
- Activation via a session DOM click on the boost range bound the real player
  element: gain `2`, threshold `-12`, ratio `12`, one analyser on the real
  compressor output, app context `sinkId.type = none`, player still paused
  (no speaker output before binding).
- Playback via `media-play-button` DOM click: real compressor-output RMS over
  25 samples / 2.5 s: mean `0.044541990617403736`, max `0.12904528661165623`,
  last `0.08743585983990614`; sink `none`, gain `2`, player unmuted, volume 1.
- Screenshot with the visible `Volume boost ... 200% ... Reset boost` control
  and a session-only analyser readout (`RMS 0.098369`, mean `0.089090`, gain
  `200%`, sink `none`, reduction `-0.0070 dB`) saved to
  `C:\Users\lamun\AppData\Local\Temp\DEV-569-T013-200pct-windowed.png`
  (viewport temporarily `1440x900`; restored to `980x496` afterwards). At the
  original `980x496` viewport the extra control row wraps below the
  `media-controller` clip boundary (y 400–440), so the larger viewport was
  needed for a screenshot that actually shows the control.
- Fullscreen: portal click on the fullscreen button produced
  `document.fullscreenElement = MEDIA-CONTROLLER`, boost control rect
  `x 1044.56, y 858, 96x40`, and a nonzero real compressor reading (last
  `0.06977044746107397`, mean `0.05560172201013657`, sink `none`). Exiting
  fullscreen by portal click returned `fullscreenElement = null`, boost
  `aria-valuetext = 200%`, rect `x 989.56, y 608.59, 96x40`, and a nonzero
  reading (last `0.023005812639997604`, mean `0.044249969335618505`, sink
  `none`). Fullscreen and windowed operation PASS; the portal screenshot in
  fullscreen did not render the DOM overlay/control, so those receipts are DOM
  readings rather than a fullscreen image.
- Cleanup: app context reported `sinkId.type = none` before close and `state =
  closed` after; analyser/hook/overlay removed, remembered boost restored to
  `100`, portal viewport restored, all media paused, line 7 `true`.

## Portal capability notes

- `maestri portal click` by accessibility ref did not trigger the React
  `onChange`/`onClick` handlers for the boost range or reset button; session
  DOM clicks on the same visible controls did. Media Chrome play/fullscreen
  buttons responded to portal clicks.
- No production file was edited in Phase 5; all instrumentation was page-session
  evaluation and was removed before reporting.
