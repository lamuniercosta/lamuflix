# DEV-454 — Phase 7 Compose real-browser evidence (T016–T017)

Recorded 2026-11-10 in worktree `F:/Dev/LamuFlix.worktrees/DEV-454`, branch `feature/DEV-454`,
against implementation HEAD `20d919e87cfcfa3e98ac7e1ce61c8baa75ad05d5`
(`DEV-454 - Browser subtitle selector`). The Api and web images were rebuilt from that HEAD
before every run below; no product code was changed to obtain any result here.

**Status of this file**: an honest runtime record, not a PASS claim for every check.
SMOKE-001, SMOKE-003 and SMOKE-004 were observed passing. SMOKE-002's cue-display and
cue-clear behaviour was observed; its final "every track element reports `disabled`" property
was **not** observed in Chromium and is recorded as an open deviation below. The suite is
therefore red and T017 is **not** complete.

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

## T017 — real-browser Compose verification through web nginx

Command (repository root): `pwsh -NoProfile -File scripts/run-e2e-smoke.ps1` →
**exit 1**, `16 passed`, `1 failed`.

| Field | Value |
| --- | --- |
| Delivery SHA | `20d919e87cfcfa3e98ac7e1ce61c8baa75ad05d5` |
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

**Status: cue display PASS; "all track elements disabled after Off" NOT OBSERVED (open deviation).**

Observed: choosing `Portuguese (Brazil)` produced exactly one `showing` track, the other two
`disabled`, that track's `activeCues` contained the served cue text
(`Legenda em português: ação.`), and the video kept playing. After choosing `Off` the selector
value returned to `""`, no track reported any active cue, and playback continued (verified after
rewinding the 8 s fixture, which otherwise had reached its natural end).

Not observed, quoted verbatim from the failure message:

```text
Off clears every track: {"selectorValue":"","textTrackCount":3,"tracks":[
{"id":"Dg353hiYGCLJ5sntEMoePrRNwSJXXT38WfOXYEPFuMQ","label":"Default","mode":"disabled","cues":[]},
{"id":"482cRtmpqrIpF3oSl1al2cA6OFEai5SdMmURLpjB7rQ","label":"English","mode":"showing","cues":[]},
{"id":"G4TXuLaAFSmPsUrZUsQhn2BghufWBGS13jwyeCFU-dU","label":"Portuguese (Brazil)","mode":"disabled","cues":[]}]}
```

So the user-visible contract (cues clear on Off, video continues, selector reads Off) held, while
one track element whose `src` had been removed reported Chromium's own `showing` mode with zero
cues. FR-010's literal wording ("only the selected track is `showing`") is therefore not
demonstrated on this path. This was not investigated further inside this phase.

### SMOKE-003 — unconvertible track returns to Off with `Subtitles unavailable`, video continues

**Status: PASS.**

`GET /api/movies/{id}/subtitles` for `Elephants Dream` returned 200 with one listed track; choosing
it produced content-route statuses `[422, 422]` through nginx, `Subtitles unavailable` became
visible, the selector value returned to `""`, the single track reported `disabled`, playback
continued and still advanced after the failure. Reselecting re-requested the content route
(retry path observed, not asserted as a promise about copy).

### SMOKE-004 — failing list keeps Off, offers Retry, never masquerades as empty

**Status: PASS.**

With the list route forced to `500` on the Compose path: one failed attempt recorded, zero
comboboxes rendered while failed (`comboboxCountWhileFailed: 0`), `Subtitles unavailable` and the
`Retry loading subtitles for Big Buck Bunny` control visible, video playing and still advancing
after the failure. After `Retry` succeeded, the selector recovered with options `Off`, `Default`,
`English`, `Portuguese (Brazil)`.

## Scope limits of this receipt

- One synthetic sample clip (Big Buck Bunny, 8 s) carries the cue assertions; the failure-path
  sample is the Elephants Dream folder. No codec or all-format claim.
- `jsdom`/Vitest results are not offered as a substitute for these observations.
- Runs before the recorded one failed on this file's own assertions or on the Compose stack
  (the Api container exited 139 during startup twice); those runs are not results and are not
  counted as evidence.
