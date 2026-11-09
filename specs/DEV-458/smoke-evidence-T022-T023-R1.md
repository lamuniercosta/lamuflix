# DEV-458 — Round-1 remediation runtime smoke evidence (T022–T024)

Recorded 2026-11-09 in worktree `feature/DEV-458-harden-the-local-launcher` at
code-fix HEAD `1ea276bf4c4c8758bbecef142f1047a7710e1ee1`
(`DEV-458 - Remediate review round 1 code findings`).
Supersedes the gitignored `artifacts/dev458-smoke/` logs from the pre-fix HEAD.

## T022 — shipped nginx negative-launch smoke

Rebuilt images from the fix commit: api `sha256:4e42ccefbd86…`,
web `sha256:dba8cb815f05…`; `docker/nginx.conf` blob at HEAD
`cd5ea2db6fd687ee3ad1a6e4737acf8b348fc8ce` (case-insensitive `~*` deny, no comments).

Full log: `artifacts/dev458-smoke/evidence/smoke.log` (gitignored tooling output;
results transcribed here).

| Check | Expectation | Observed |
| --- | --- | --- |
| N1 `POST /api/movies/{id}/play` via nginx | proxy 403, never forwarded, zero starts | 403 from `Server: nginx/1.31.7`, api log shows the play path never arrived, markers 1→1 |
| N1b sibling `GET /api/movies` via nginx | reaches backend (proxy alive) | 200 with catalog JSON, reached backend: True |
| N1c `POST /api/movies/{id}/PLAY` via nginx | proxy 403, never forwarded, zero starts | 403 from `Server: nginx/1.31.7`, play path never arrived, markers 1→1 |
| P positive control, named LocalPlay loopback host, same build/player/file fixtures | 204, exactly one start | 204 empty body, markers 1→2 |

`SMOKE RESULT: PASS`. The positive control proves a working launcher only; it is
not presented as proof the excluded topology can launch. The shared marker file
carried one stale line from the earlier pre-fix run; every assertion above holds on
deltas (zero new starts on negatives, exactly one on the control).

## T023 — Aspire-path smoke

AppHost started unmodified via `dotnet run --project src/LamuFlix.AppHost`
(`web` keeps `WithEndpointProxySupport(false)`; no AppHost edit):

| Variable | Value |
| --- | --- |
| `Features__LocalPlay` | `true` |
| `Library__RootPath` | absolute `artifacts/dev458-smoke/aspire-library` |
| `Playback__Players__0__ExecutablePath` | absolute `artifacts/dev458-smoke/aspire-player.cmd` |
| `Ollama__Endpoint` | `http://127.0.0.1:11434/` (host stub `web/e2e/stub.mjs serve`, same embedding stub family as the compose `api-stub`) |

Two aborted runs precede the passing run and are recorded honestly: R1 used a
relative `Library__RootPath` and the API refused startup at the library-path gate;
R2 reused a stale container network namespace after a restart and the API could not
reach its stub. R3 with the values above is the passing run.

| Check | Expectation | Observed |
| --- | --- | --- |
| Aspire-injected API listeners | loopback only, accepted by validator | pid 80264 on `127.0.0.1:13341` + `[::1]:13341`; `Now listening on http://localhost:13341`, `Application started`, no validator refusal logged |
| Web-proxy attempt `POST localhost:5173/api/movies/1/play` | no forwarded launch, zero starts | 404 from Vite, markers unchanged |
| Header-bearing direct `POST 127.0.0.1:13341/api/movies/1/play` (`X-Forwarded-For: 203.0.113.9`) | API 403, zero starts | 403 generic `Forbidden` (no detail, no peer/header leak), markers unchanged |
| Named positive control, same build/player/file fixtures | 204, exactly one start | 204 empty body, markers 1→2 |

**T023: PASS.** No contradictory injection-vs-precedence evidence appeared, so none
of the Q7 return-for-recon conditions tripped and no AppHost edit was required.

## T024 — checkpoint

Evidence above is attached (this file, committed). Positive control is not
conflated with either negative topology. Stripping-loopback-proxy and
unlisted-header residuals remain unsupported and are not asserted as solved.
