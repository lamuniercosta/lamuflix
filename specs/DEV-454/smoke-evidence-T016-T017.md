# DEV-454 — Phase 7 Compose real-browser evidence (T016–T017)

Recorded 2026-11-10 in worktree `F:/Dev/LamuFlix.worktrees/DEV-454`, branch `feature/DEV-454`,
against delivery HEAD `fe1362304c705acc1a224b9609851d43741b9f1a`
(`DEV-454 - Real-browser subtitle acceptance`). The Api and web images were rebuilt from that HEAD
before every run below; no product code was changed to obtain any result here.

**Status of this file**: an honest runtime record. All four SMOKE items were observed passing at
the recorded SHA and the suite is green. The earlier SMOKE-002 open deviation recorded against
`20d919e8` is superseded by the remediation described below and re-verified at `fe136230`.

## T016 — non-tracked sample provisioning

| Item | Value |
| --- | --- |
| Runtime library path (`LIBRARY_PATH`) | `web/e2e/.runtime/library` |
| Ignored? | Yes — `web/e2e/.gitignore:1` (`.runtime/`); no media or subtitle sample is committed |
| Provisioning | `web/e2e/fixtures.mjs` writes the SRT sidecars beside the copied video for the two seeded movies; `web/e2e/run.mjs` writes `LIBRARY_PATH` and imports each folder through `POST /api/movies/import` |
| Tracked placeholders | Not touched: `docker/sample-library/*.mp4` are unchanged by this ticket's diff |
| Compose/e2e `Features.LocalPlay` | `false` — validated by `web/e2e/run.mjs` (`validateProviderSettings`); the live capability endpoint returned `localPlayEnabled: false` in the DEV-455 specs of the same run |

Sidecars written (labels/languages/order are what `SubtitleSidecarDiscovery` derives from the
file names, not values the test asserts into existence):

| Movie folder | Sidecar file | Served label | Language | Default |
| --- | --- | --- | --- | --- |
| `seeded/Big Buck Bunny (2008)` | `Big Buck Bunny (2008).default.srt` | `Default` | `und` | yes |
| `seeded/Big Buck Bunny (2008)` | `Big Buck Bunny (2008).en.srt` | `English` | `en` | no |
| `seeded/Big Buck Bunny (2008)` | `Big Buck Bunny (2008).pob.srt` | `Portuguese (Brazil)` | `pt-BR` | no |
| `seeded/Elephants Dream (2006)` | `Elephants Dream (2006).srt` | `Subtitles` | `und` | no |

The Elephants Dream sidecar is deliberately **not** a subtitle document. It is discovered and
listed like any other sidecar and then rejected by the real content route with the sanitized
`422`, which exercises the viewer's failure handling through the shipped pipeline rather than a
faked transport. The SRT sample text for the listed tracks is synthetic; no licensed or
third-party subtitle work is committed.

## Viewer remediation applied before the recorded run

`specs/DEV-454/CONCLUSIONS.md` (Phase 7 T017, Patron ruling `677b9700`) required the post-Off
`showing` track mode to be fixed in the viewer rather than accepted as a deviation. The fix is in
`web/src/features/playback/`:

- `subtitleTrackModes.ts` — `applySubtitleTrackModes` now writes a mode only when it differs, and
  a new `watchSubtitleTrackModes` re-applies the desired selected/disabled modes on the video's
  `textTracks` `change`/`addtrack`/`removetrack` events and on `loadstart`/`loadedmetadata`.
- `BrowserPlayer.tsx` — the desired track id is held in a ref read by that watcher, applied once per
  change by an effect, and the active `<track>` keeps a stable `key` (`track.id`, no
  remount-on-Off/attempt churn) so Chromium's own text-track selection cannot resurrect a
  non-selected track. `data-track-id` is retained, so the browser probe reads unchanged.
- `subtitleSelector.test.tsx` — three new viewer-seam regressions cover browser re-selection after
  `Off`, after a switch, and after a media load.

## T017 — real-browser Compose verification through web nginx

Command (repository root): `pwsh -NoProfile -File scripts/run-e2e-smoke.ps1` →
**exit 0**, `17 passed (2.5m)`.

| Field | Value |
| --- | --- |
| Delivery SHA | `fe1362304c705acc1a224b9609851d43741b9f1a` |
| Compose URL | `http://localhost:3000` (web nginx `/api/` proxy → `api:8080`) |
| Browser | Chromium 158.0.8127.18 (Playwright 1.65.0 bundled build), `Desktop Chrome` device |
| Sample | `seeded/Big Buck Bunny (2008)` (8.000 s, 320x180, H.264 + AAC; a synthetic ffmpeg test pattern, not the film) |
| Spec | `web/e2e/dev454-subtitles.e2e.ts` |
| Raw evidence | `web/e2e/.runtime/evidence/dev454-subtitles.json` (gitignored runtime output, transcribed below) |

### SMOKE-001 — Off plus the served track labels, in served order, as plain text

**Status: PASS.**

Recorded through `GET /api/movies/1/subtitles` on the Compose path and from the rendered
`<select>`: options read `Off`, `Default`, `English`, `Portuguese (Brazil)`; the option list after
`Off` equalled the served labels in served order; the served response carried three tracks with
distinct opaque tokens (`Dg353hiY…`, `482cRtmp…`, `G4TXuLaA…`). Labels rendered as text
children of `<option>`, so a hostile basename cannot become markup.

### SMOKE-002 — choose a track shows its cues, Off clears them, video continues

**Status: PASS.**

Observed: choosing `Portuguese (Brazil)` produced exactly one `showing` track
(`othersDisabled: true`), the other two `disabled`, that track's `activeCues` contained the served
cue text (`Legenda em português: ação.`), and the video kept playing (`playingWithCues: true`).
After choosing `Off` the selector value returned to `""`, **every** rendered track reported
`disabled`, no track reported any active cue, and playback continued (verified after rewinding the
8 s fixture, which otherwise had reached its natural end).

Verbatim post-Off probe from the green run:

```text
{"selectorValue":"","textTrackCount":3,"tracks":[
{"id":"Dg353hiYGCLJ5sntEMoePrRNwSJXXT38WfOXYEPFuMQ","label":"Default","mode":"disabled","cues":[]},
{"id":"482cRtmpqrIpF3oSl1al2cA6OFEai5SdMmURLpjB7rQ","label":"English","mode":"disabled","cues":[]},
{"id":"G4TXuLaAFSmPsUrZUsQhn2BghufWBGS13jwyeCFU-dU","label":"Portuguese (Brazil)","mode":"disabled","cues":[]}]}
```

This demonstrates FR-010 on the real browser path: only the selected track is `showing`, every
other listed track is `disabled`, and choosing `Off` disables all of them. The earlier
`20d919e8` observation, where the removed-`src` English element reported Chromium's own `showing`
with zero cues, no longer reproduces.

### SMOKE-003 — unconvertible track returns to Off with `Subtitles unavailable`, video continues

**Status: PASS.**

`GET /api/movies/{id}/subtitles` for `Elephants Dream` returned 200 with one listed track; choosing
it produced content-route statuses `[422, 422]` through nginx, `Subtitles unavailable` became
visible, the selector value returned to `""`, the single track reported `disabled`, playback
continued and still advanced after the failure. Reselecting re-requested the content route
(retry path observed, not asserted as a promise about copy).

### SMOKE-004 — failing list keeps Off, offers Retry, never masquerades as empty

**Status: PASS.**

With the list route forced to `500` on the Compose path: the failed attempt was recorded
(`failedListAttempts: 1`), zero comboboxes rendered while failed (`comboboxCountWhileFailed: 0`),
`Subtitles unavailable` and the `Retry loading subtitles for Big Buck Bunny` control were visible,
video playing and still advancing after the failure. After `Retry` succeeded, the selector
recovered with options `Off`, `Default`, `English`, `Portuguese (Brazil)`.

## Gate re-run at the recorded SHA

`pwsh -NoProfile -File scripts/run-web-gates.ps1 -All` → **exit 0**,
`Web gates: PASS (lint, tsc, vitest, build)` (vitest 744 passed).

## Scope limits of this receipt

- One synthetic sample clip (Big Buck Bunny, 8 s) carries the cue assertions; the failure-path
  sample is the Elephants Dream folder. No codec or all-format claim.
- `jsdom`/Vitest results are not offered as a substitute for these observations.
- Runs before the recorded one failed on this file's own assertions or on the Compose stack
  (the Api container exited 139 during startup twice) and the `20d919e8` run's SMOKE-002 failure;
  those are superseded by the remediation and are not counted as evidence for this receipt.
