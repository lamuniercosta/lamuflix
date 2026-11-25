# DEV-569 Phase 5 live Test URLs portal evidence (T012–T013)

Session: `Test URLs` portal at `http://localhost:3000/movies/512`, worktree
`F:/Dev/LamuFlix.worktrees/DEV-569`, branch `feature/DEV-569`, HEAD `d0c0509c`
for the original run and HEAD `9be74d60` for the T012–T013 rerun below (no
production code edits in either). All measurements are session-only page
evaluation; no shipping instrumentation, exports or globals are used by the
application.

## Rerun @ 605961fa — Q2 tolerance declared BEFORE any result was collected (2026-11-25T20:34:19Z)

- Rerun scope: T012–T013 only at HEAD `605961fa` after Cog's `threshold = 0 dB` makeup-gain fix (boost still maps percent/100 to gain, limiter `ratio = 12` above 100%, `knee = 0`); no production code edits in this session. This tolerance was written into this file before the first measurement of the rerun was taken.
- Q2 bounds unchanged: boost 100–300%, gain = percent/100, 100% including Reset measurably and audibly unity, limiting only above 100%, about 2x output at 200% on the same calibrated quiet stimulus below compression onset (`specs/DEV-569/CONCLUSIONS.md` Q2 / Patron 922d0753).
- Pre-declared pass tolerance for the retained ratios:
  - active 100% / independent native reference: 0.90–1.10;
  - Reset 100% / independent native reference: 0.90–1.10;
  - active 200% / active 100%: 1.80–2.20;
  - quiet-signal compressor reduction at 100%: 0 dB (transparent);
  - loud stimulus above unity at 200%: compressor reduction < -3 dB and RMS <= 1.35x the 100% loud RMS (limiting engaged);
  - mute at 200%: RMS <= 0.001 (silenced).
- Under `test-portal-policy:6`, audible unity is evidenced silently through gain exactly 1, 0 dB quiet reduction and an RMS match to the independent unbound native reference at stock element volume 1; no audio is played to speakers.

### Rerun @ 605961fa portal safety receipts

- Navigated `Test URLs` to `http://localhost:3000/movies/512` before any override; the exact line 7 stop-media command printed `true` immediately after navigation. The inherited session auto-started the player before the observer was installed; it was paused immediately with line 7 before any activation or measured playback, and this is disclosed rather than claimed as our playback.
- Applied the line 6 override with the literal string preserved as `String.fromCharCode(110,111,110,101)`; it returned `patched: true` and a probe `new AudioContext()` reported `probeSinkIdType: none` / `probeState: running` before being closed. The independent reference context reported `sinkId.type = none`; the app context created on activation reported `appSink: ["none"]` and `state: running`. No audio was played to speakers in any measured run.
- T012 used two unmuted (`muted = false`) elements at `volume = 1`: a measurement element bound by the app graph (player video paused and unbound), and a separate standalone element wired `createMediaElementSource -> AnalyserNode -> destination` with no gain or compressor in the isolated silent reference context (Patron 922d0753). T013 bound the real movie element instead.

### Rerun @ 605961fa T012 calibrated receipt (session-only)

- Observer installed before activation with `appContextCountBeforeActivation: 0`, `appAnalyserBeforeActivation: false`, reference `state: running` / `sink: none`. Stimulus: generated 4 s, 44.1 kHz, mono, 440 Hz PCM16 WAV (amplitude `0.1` quiet, `0.9` loud), identical Blob URL for both elements; 12 samples after 1.5 s at fixed stock volume 1.
- Active 100%: gain `1`, threshold `0`, ratio `1`, knee `0`, quiet reduction `-0.0001342038594884798 dB`; compressor-output RMS `0.07073961313736842`, independent native reference `0.07072468138621485`, ratio `1.0002111250395322`. PASS against 0.90–1.10.
- Reset 100% via visible `Reset boost` DOM click: first from 100% gave ratio `0.9999471195360091`; a second, stronger reset from a live 200% state (before: gain `2`, aria `200%`) gave gain `1`, compressor-output RMS `0.07071415889381909`, reference `0.07072362336574067`, ratio `0.9998661766539783`, quiet reduction `-0.0001342038594884798 dB`. PASS against 0.90–1.10; silent audible-unity proxy holds (gain 1, 0 dB reduction, matched reference).
- Active 200%: gain `2`, threshold `0`, ratio `12`, quiet reduction `-0.0001342038594884798 dB` (no makeup gain, transparent below onset); compressor-output RMS `0.14141876080887364`, reference `0.07073951371890731`, ratio to active 100% `1.9991452389519606`, ratio to same-window reference `1.9991480485831374`. PASS against the pre-declared 1.80–2.20.
- Mute at 200%: mean RMS `0` over 8 samples with the measurement element muted, then unmuted again. PASS against <= 0.001.
- Loud limiting above unity: at 100% (gain `1`, threshold `0`, ratio `1`) RMS `0.6362527750203858`, reduction `-0.0001342038594884798 dB`; at 200% (gain `2`, threshold `0`, ratio `12`) RMS `0.754085071362085`, reduction `-4.565134048461914 dB`, loud ratio `1.185197300456448`. PASS against < -3 dB and <= 1.35x.
- Cleanup: app sink `none` and reference sink `none` before close; all three tracked contexts (including one additional silent context found in the sweep) were explicitly closed and reported `closed`; prototype hooks restored, measurement elements removed, Blob URLs revoked, remembered boost restored to `100`, line 7 printed `true`.
- **T012 verdict: PASS at `605961fa`** — every pre-declared Q2 tolerance is met, including 200% `1.9991` vs `1.80–2.20`, with no production code edits in this session.

### Rerun @ 605961fa T013 real-library movie 512 receipt (session-only)

- Reloaded `http://localhost:3000/movies/512` after storing boost `200`; line 7 printed `true` immediately after reload, then the literal-preserving line 6 override returned `patched: true` with `probeSinkIdType: none`. Pre-activation observer installed with `appContextsBeforeActivation: 0`; player paused `true`, `muted: false`, `volume: 1`, `currentSrc: http://localhost:3000/api/movies/512/stream`, UI `200%` with pending-activation text visible. No measured playback before the override was active.
- Activation via a session DOM change of the visible boost range (199 -> 200) bound the real player: one app context with `sinkId.type = none`, gain `2`, threshold `0`, ratio `12`, knee `0`, analyser on the real compressor output; player still paused, unmuted, volume 1. Playback was started by a DOM click on `media-play-button`; an initial 25-sample window while buffering averaged `0.001758`, and a stabilized 25-sample window at `currentTime 14.34` gave mean `0.023759465313205122`, max `0.05036360350158583`, last `0.023312244821972278`, sink `none`, gain `2`, `muted: false`, `volume: 1`, stream URL unchanged.
- Screenshot: viewport temporarily `1440x900`, controls shown by hovering the visible boost range, session-only overlay showing `RMS 0.000672`, mean `0.023759` (25 samples), gain `2`, threshold `0 dB`, sink `none`, `muted false`, `volume 1`. Saved to `C:\Users\lamun\AppData\Local\Temp\DEV-569-T013-200pct-final.png` (1440x900 at capture; viewport restored to `980x480`). The instantaneous RMS was a quiet film moment; the recorded 25-sample mean is the receipt.
- Fullscreen: portal click on `enter fullscreen mode` gave `fullscreenElement = MEDIA-CONTROLLER`, boost range rect `x 1044.56, y 858, 96x40`, 10 analyser samples mean `0.039126881289249744`, last `0.03382083090140347`, sink `none`. Portal click on `exit fullscreen mode` returned `fullscreenElement = null`, rect `x 989.56, y 600.59, 96x40`, mean `0.04369256612119342`, last `0.04175642674370982`, sink `none`. Fullscreen and windowed PASS; fullscreen receipts are DOM/analyser readings because the portal screenshot does not render the overlay in fullscreen.
- Cleanup: app context sink `none` before close and `closed` after, hooks restored, overlay/timer removed, boost UI and stored value restored to `100`, all media paused, line 7 printed `true`.
- **T013 verdict: PASS at `605961fa`**.

### Rerun @ 605961fa checkpoint

- US4 calibrated and real-library pass evidence are both on this head; Phase 5 T012–T013 are checked off. No production file was edited in this session; all instrumentation was page-session-only and removed. Phase 6 gates/review remain the delivery checkpoint.

## Rerun @ 9be74d60 — Q2 tolerance declared BEFORE any result was collected (2026-11-25T20:17:04Z)

- Rerun scope: T012–T013 only, at HEAD `9be74d60` after the Cog threshold fix
  (`threshold = -6 dB` when limiting, unchanged `ratio = 12` and `knee = 0`);
  no production code edits in this session. The tolerance below was written
  into this file before the first measurement of the rerun was taken.
- Q2 bounds unchanged: boost 100–300%, gain = percent/100, 100% including
  Reset measurably and audibly unity, limiting only above 100%, about 2x output
  at 200% on the same calibrated quiet stimulus below compression onset
  (`specs/DEV-569/CONCLUSIONS.md` Q2 / Patron 922d0753).
- Pre-declared pass tolerance for the retained ratios:
  - active 100% / independent native reference: 0.90–1.10;
  - Reset 100% / independent native reference: 0.90–1.10;
  - active 200% / active 100%: 1.80–2.20;
  - quiet-signal compressor reduction at 100%: 0 dB (transparent);
  - loud stimulus above unity at 200%: compressor reduction < -3 dB and RMS
    <= 1.35x the 100% loud RMS (limiting engaged);
  - mute at 200%: RMS <= 0.001 (silenced).
- Under `test-portal-policy:6`, audible unity is evidenced silently through
  gain exactly 1, 0 dB quiet reduction and an RMS match to the independent
  unbound native reference at stock element volume 1; no audio to speakers.

### Rerun portal safety receipts

- Navigated `Test URLs` to `http://localhost:3000/movies/512` before any
  override or measurement; the player was found already playing by the
  inherited session and was paused immediately, before any measurement.
- Applied the `test-portal-policy:6` override while preserving the literal
  sink string with `String.fromCharCode(110,111,110,101)`; it printed
  `patched: true`. A probe `new AudioContext()` returned
  `probeSinkIdType: none`, `probeState: running`, and was closed before any
  media played. The capability probe from the first evaluation was created
  with the same silent sink, carried no media source, and was discarded on
  the subsequent page reload.
- T012 used two unmuted (`muted = false`) elements at `volume = 1`: a
  measurement element wired into the real app graph by redirecting
  `createMediaElementSource`, and an independent reference element connected
  directly `createMediaElementSource -> AnalyserNode -> destination` in an
  isolated `sinkId.type = none` context with no gain or compressor
  (Patron 922d0753). The player `<video>` stayed unbound. The app context also
  reported `sinkId.type = none`.
- The exact `test-portal-policy:7` stop-media command printed `true` after T012
  cleanup, again after the reload before T013 activation, and finally after
  T013 cleanup. Every tracked Web Audio context was explicitly closed; both
  session-only state objects, prototype hooks, analysers, Blob URLs and the
  screenshot overlay were removed before reporting. No audio was played to
  speakers during any measured run.

### Rerun T012 calibrated portal receipt (session-only)

- Observer installed before any activation with `appContextCount: 0` and the
  reference context reporting `refSink: none`, `refState: running`; both
  measurement elements were unmuted at `volume = 1`. The calibrated stimulus
  was a generated 4 s, 44.1 kHz, mono, 440 Hz PCM16 WAV (amplitude `0.1` quiet,
  `0.9` loud); stabilized windows were 12 samples after 1.6 s.
- Activation at 100%: gain `1`, threshold `0`, ratio `1`, quiet reduction
  `-0.0001342038594884798 dB`; compressor-output RMS
  `0.07073229599279808`, independent native reference RMS
  `0.07073683848256222`, ratio `0.9999357832515336`. PASS against 0.90–1.10.
- Activation at 200%: gain `2`, threshold `-6`, ratio `12`, quiet reduction
  `-0.0001342038594884798 dB`; compressor-output RMS `0.206773570324056`,
  reference RMS `0.0707270789441088`, ratio to active 100%
  `2.923326147155036`, ratio to the same-window native reference
  `2.923541780757781`. **FAIL** against the pre-declared 1.80–2.20 tolerance.
- Diagnostic gain-output tap (session-only): gain 1 produced gain-output RMS
  `0.0706983640668832` and compressor-output RMS `0.10337942284269663`
  (`1.462260466803673x`); gain 2 produced `0.1414550660765062` and
  `0.20673565778502795` (`1.4614934870781837x`). The gain mapping is exactly
  2x; Chromium's `DynamicsCompressorNode` adds about +3.3 dB of makeup gain at
  `threshold = -6 dB`, `knee = 0`, `ratio = 12` even while the quiet signal is
  below onset (`reduction` stays about 0 dB). The Cog change from `-12 dB` to
  `-6 dB` reduced the excess (prior `4.2707x`) but did not bring the retained
  compressor-output ratio inside 1.80–2.20. No production code was changed in
  this rerun.
- Reset 100% via the visible `Reset boost` DOM click: gain `1`, threshold `0`,
  ratio `1`, reduction `-0.0001342038594884798 dB`; compressor-output RMS
  `0.07069930296112006`, reference `0.0707088470219026`, ratio
  `0.9998650231027021`. PASS.
- Mute at 200%: RMS `0` while muted, with both elements unmuted again
  afterwards. PASS against <= 0.001.
- Loud limiting above unity: at 100% (gain `1`, threshold `0`, ratio `1`)
  RMS `0.6364579896875243`, reduction `-0.0001342038594884798 dB`; at 200%
  (gain `2`, threshold `-6`, ratio `12`) RMS `0.5906857401806759`, reduction
  `-9.99451200167338 dB`, ratio `0.9280828424680145`. PASS against < -3 dB and
  <= 1.35x.
- Cleanup: both contexts reported `sinkId.type = none` before closing and
  `state = closed` after; hooks restored to the original prototypes, Blob
  URLs revoked, measurement elements removed, remembered boost restored to
  `100`, all media paused, line 7 `true`.
- **T012 verdict: BLOCKED at `9be74d60`** — active 200% / active 100% is
  `2.9233` against the pre-declared `1.80–2.20` tolerance. The execution is
  receipted, but this is not a passed delivery checkpoint and T012 stays
  unchecked in `tasks.md`.

### Rerun T013 real-library movie 512 receipt (session-only)

- Page reloaded at `http://localhost:3000/movies/512` with remembered boost
  `200`; media was paused and the line 6 override re-applied immediately with
  the same literal-preserving form, returning `probeSink: none` before
  activation. Autoplay resumed buffering once before activation; it was paused
  and verified `playerPaused: true` with the app still unbound and the player
  unmuted at `volume = 1`, `currentSrc`
  `http://localhost:3000/api/movies/512/stream`, UI `200%`. No measured
  playback occurred before the override was active.
- Pre-activation observer installed with `appContextsBeforeActivation: 0`;
  activation was a session DOM change of the visible boost range to 200, which
  bound the real player element with gain `2`, threshold `-6`, ratio `12`, one
  analyser on the real compressor output and app context
  `sinkId.type = none`; the player was still paused at bind time.
- Playback via a DOM click on `media-play-button`: 25 samples over ~2.5 s from
  the real compressor output gave mean `0.027758530544200837`, max
  `0.04376243278528866`, last `0.024506079099951443`; sink `none`, gain `2`,
  player `muted = false`, `volume = 1`, stream URL unchanged.
- Screenshot: viewport temporarily `1440x900`, controls shown by hovering the
  visible boost range, session-only overlay reading `RMS 0.036543`, mean
  `0.027759`, gain `2`, sink `none`, `muted false`, `volume 1`. Saved to
  `C:\Users\lamun\AppData\Local\Temp\DEV-569-T013-200pct-windowed-rerun.png`.
  The 1440x900 viewport was needed because at `980x488` the boost row is
  clipped below the `media-controller` and Media Chrome hides controls until
  hover. Viewport restored to `980x488` afterwards.
- Fullscreen: portal click on `enter fullscreen mode` gave
  `fullscreenElement = MEDIA-CONTROLLER`, boost range rect
  `x 1044.56, y 858, 96x40`, 10 analyser samples mean `0.04587963473062895`,
  last `0.04421930157606567`, sink `none`. Portal click on `exit fullscreen
  mode` returned `fullscreenElement = null`, `aria-valuetext = 200%`, rect
  `x 989.56, y 604.59, 96x40`, mean `0.05075365970267805`, last
  `0.042374057068383535`, sink `none`. Fullscreen and windowed PASS; the
  portal screenshot in fullscreen does not render the DOM overlay, so those
  receipts are DOM/analyser readings.
- Cleanup: app context reported `sinkId.type = none` before close and
  `state = closed` after; analyser/hook/overlay/timer removed, remembered boost
  restored to `100`, viewport restored, all media paused, line 7 `true`.
- **T013 verdict: PASS on its own receipt**; the Phase 5 delivery checkpoint
  remains blocked by the T012 200% ratio.

## Prior run Q2 tolerance — declared BEFORE any result was collected (2026-11-25T19:51:55Z)

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
