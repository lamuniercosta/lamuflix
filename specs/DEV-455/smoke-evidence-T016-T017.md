# DEV-455 — Phase 6 Compose real-browser evidence (T016–T017)

Recorded 2026-11-09 in worktree `F:/Dev/LamuFlix.worktrees/DEV-455`, branch
`feature/DEV-455`, against implementation HEAD
`7b0741a2704691d77b1d19204eae8d3c624be650` (`DEV-455 - Open in BS Player`).
That is the SHA the running stack was built from. This file and
`web/e2e/dev455-playback.e2e.ts` are the only additions of the committing change;
neither alters the shipped behaviour measured below.

## T016 — non-tracked sample provisioning

| Item | Value |
| --- | --- |
| Runtime library path (`LIBRARY_PATH`) | `web/e2e/.runtime/library` |
| Ignored? | Yes — `web/e2e/.gitignore:1` (`.runtime/`), confirmed by `git check-ignore -v` |
| Provisioning | `web/e2e/fixtures.mjs` copies the corpus into the runtime library; `web/e2e/run.mjs:92` writes `LIBRARY_PATH`; `web/e2e/run.mjs:366-388` imports each folder through `POST /api/movies/import` |
| Scan/import result | Both seeded titles reached `enrichmentStatus: Enriched` and were browsable before any playback assertion (`waitForSeededState`) |
| Tracked placeholders | Not replaced and not touched — `docker/sample-library/*.mp4` are unchanged in this ticket's diff (`git diff 7b0741a2` shows no `docker/` change) |
| Compose/e2e `Features.LocalPlay` | `false` — generated settings are validated (`run.mjs:107`) and the live capability endpoint returned `localPlayEnabled: false` |

Corpus provenance and licence position. The four clips are **synthetic test
patterns generated with ffmpeg**, not the films their filenames name. `ffprobe`
reports the same encoder for all four (`Lavf61.7.100`), 320x180, 30 fps, H.264
High + AAC LC; a decoded frame at 00:00:02.000 of
`Big Buck Bunny (2008).mp4` is an SMPTE-style colour-bar pattern with burnt-in
timecode and a noise overlay. They were committed by DEV-456 under the Patron
Q13 ruling (`specs/DEV-456/CONCLUSIONS.md`, "Option 1") that replaced the
pre-existing 0-byte placeholders. Because the bytes are machine-generated and
contain no third-party audiovisual work, no licence obligation attaches and no
unlicensed media is committed. Filenames are corpus labels only; this receipt
claims nothing about the named films.

| File | Duration | Size | Video | Audio | SHA-256 |
| --- | --- | --- | --- | --- | --- |
| `Big Buck Bunny (2008).mp4` | 8.000 s | 202927 B | h264 High 320x180 | aac LC | `3A82A333FE6127CC03BCDBC4E3AFF69A6C99ABCD34A9A14D60E870D9CED8DE07` |
| `Elephants Dream (2006).mp4` | 10.000 s | 255070 B | h264 High 320x180 | aac LC | `48F5F3C0AFBB947AB7287B9E4DC7A40CEB79439D4B47C13D8E6B6F0009B2D1E2` |
| `Sintel (2010).mp4` | 12.000 s | 298750 B | h264 High 320x180 | aac LC | `F98BA6B2029337A2A9DD251DDB06A5BB1A2B7A12847032673FDB30A2E48C305D` |
| `Tears of Steel (2012).mp4` | 14.000 s | 341884 B | h264 High 320x180 | aac LC | `54A44D661A866C2880B1014FC0110EEBA4C046FE89E8D4577FD8663F84A3554D` |

## T017 — real-browser Compose verification through web nginx

Command (repository root): `pwsh -NoProfile -File scripts/run-e2e-smoke.ps1`
→ **exit 0**, `PASS:`, `13 passed (2.2m)`.

| Field | Value |
| --- | --- |
| Delivery SHA | `7b0741a2704691d77b1d19204eae8d3c624be650` |
| Compose URL | `http://localhost:3000` (web nginx `/api/` proxy → `api:8080`) |
| Browser | Chromium 158.0.8127.18 (Playwright 1.65.0 bundled build), desktop 1280x720 and 390x844 viewports |
| Codec/container | MP4 (`isom`/`avc1`/`mp41`), H.264 High 320x180 @30 fps + AAC LC |
| Duration | 8 s (Big Buck Bunny corpus clip); seeks exercised to 5.60 s |
| Spec | `web/e2e/dev455-playback.e2e.ts` |
| Raw evidence | `web/e2e/.runtime/evidence/{dev455-playback,browser-seek,stream-acceptance}.json` (gitignored runtime output, transcribed below) |
| Status | **PASS** for the checks below |

### SMOKE-001 — details Play opens the in-page player and playback proceeds

| Check | Expectation | Observed |
| --- | --- | --- |
| Capability on the Compose path | `localPlayEnabled: false` | `GET /api/playback/capabilities` → 200 `{"localPlayEnabled":false}` |
| Player visible, region labelled and focused | `region "Movie player: …"` visible, owns focus | visible; `toBeFocused()` passed |
| Stream through nginx | every response 200/206 | statuses `[206]`, ranged request present |
| Playback advances | `currentTime` increases | 4.036 s → beyond, then post-seek advance > 0 |
| Resumed seeking / range | seek moves position and playback continues | back-seek to 0.017 s, playback resumed and advanced; `Range: bytes=0-` served `206` (`browser-seek.json`: target 5.6 s, landed 5.600311 s, advanced to 6.2605 s, `mediaError: null`) |
| Desktop fullscreen acceptance | control enters and exits real fullscreen | `document.fullscreenElement !== null` after control; `null` after Escape |
| LocalPlay-off hidden secondary | zero `Open in BS Player` controls in the player | 0 |
| Browser Play never launches | zero `POST /api/movies/{id}/play` | `[]` |
| Mobile width (390x844) | layout-only, one desktop fullscreen control, no mobile fallback | player box 340 px wide; `fullscreenControlCount: 1`; playback advanced |
| Terminal error path | generic alert + Retry, no local fallback, no auto-launch | alert text "Couldn't play this movie. Check your connection and try again."; `localRecoveryCount: 0`; Retry re-requested the same source and playback resumed; `launchPosts: []` |

### SMOKE-002 — card Play opens the same player

| Check | Expectation | Observed |
| --- | --- | --- |
| Navigation | library card Play → `/movies/{id}` | URL matched `/movies/1` |
| Same player | labelled region present | visible |
| Playback proceeds | playing, or explicit Play after autoplay denial | `autoplayDenied: false`, played without an extra gesture; explicit `Play <title> in browser` control present (`explicitPlayOffered: true`) for the denial path |
| Final state | not paused | `paused === false` |

### SMOKE-003 — LocalPlay off hides the secondary action

| Check | Expectation | Observed |
| --- | --- | --- |
| Capability response on the details path | 200, `localPlayEnabled: false` | 200, `false` |
| `Open in BS Player` | absent | `localControlCount: 0` |
| Browser Play | present and opens the player | visible; region appeared after the click |
| Launch POST | none | `launchPosts: []` |

### Streaming matrix (same run, through nginx)

`stream-acceptance.json` recorded 32 cases against SHA `7b0741a2…`:
2 full (`200`, byte-exact, `Content-Length` = source length), 18 range slices
(`206` with exact `Content-Range`/`Content-Length`, sequential and backwards),
2 beyond-EOF (`416` with `bytes */202927`), 8 malformed/unit/multiple
characterization cases (observations only), and 2 `LocalPlay=false` stream cases
with `playback_events` count 0 before and after. `Accept-Ranges: bytes` and
`X-Content-Type-Options: nosniff` present on 200 and 206.

## Scope limits of this receipt

- One corpus clip (Big Buck Bunny, 8 s) carries the seek/fullscreen assertions;
  the full range matrix covers both seeded clips. No all-format codec claim.
- `jsdom`/Vitest results are not offered as a substitute for these observations.
- Enabled-launch coexistence (`Open in BS Player` visible) is **not** covered
  here: it needs the separate DEV-458-approved loopback host. Compose
  `Features.LocalPlay` remains `false` and was not weakened for this run.
- The three earlier runs of this suite in the same session failed only on this
  file's own assertions (fullscreen read before the async `requestFullscreen`
  settled; a forward 10 s seek that clamped past the 8 s clip's end). Both were
  test-side defects, fixed before the passing run; no product behaviour,
  `docker/nginx.conf` or Compose setting was changed to obtain this result.
