# Implementation Plan: Browser-unsupported audio stream conversion

**Branch**: `feature/572-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-572-spec`) | **Date**: 2026-11-26 | **Spec**: `specs/DEV-572/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-572`, `recon-DEV-572`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Gate 1 pending. Plan-challenge repair carries Step 6 Patron rulings (F1 / R1) and the adjudicated findings table. Core port ADR is `docs/adr/0050-browser-stream-audio-conversion-port.md` (Proposed now; Accepted in the Build PR). Spec Kit PR remains `specs/**` only. Keel records the plan-challenge freeze in `CONCLUSIONS.md`. Zero structural owner checkboxes. Ordinary Gate 1, feasibility proof, Build, integrated acceptance, and Size L review are pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `checklists/stream-conversion.md`, `DRAFTING_RECEIPT.md`; ADR reference `docs/adr/0050-browser-stream-audio-conversion-port.md` (Keel-owned; Accepted in Build PR, not Spec Kit PR). No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill Q1–Q7 and Step 6 freeze inventable unknowns; API shape is the existing stream route (documented in Scope/FR, not a new contracts package).

## Summary

When the browser cannot decode the movie’s selected audio track (AC3, E-AC3, DTS), the API prepares a completed seekable MP4 (video stream-copied, selected audio to AAC stereo) in API-owned scratch and serves it through the existing authenticated `GET /api/movies/{id}/stream` with normal byte ranges and Content-Type `video/mp4`. Supported codecs stay on the raw path. An isolated feasibility proof must pass before production code. Cleanup, LocalPlay non-interference, and Size L review close the ticket.

## Technical Context

**Language/Version**: .NET 10 (C#) API per `specs/PRODUCT.md` §1. No production TypeScript/React change within scope (`brief.md` Q5; size L no UI).

**Primary Dependencies**: Existing stream endpoint and library file boundary (`StreamEndpoints.cs`, `recon-DEV-572` lines 13–15); existing `IProcessRunner` / `SystemProcessRunner` and `ThumbnailOptions.FfmpegPath` (`recon` line 17); ffmpeg already in `docker/Dockerfile.api` line 18 (`recon` line 38). No new NuGet/npm dependency; no ffmpeg wrapper library (`brief.md` Q3, Q5; PRODUCT §5 item 1).

**Storage**: API-owned scratch files only (no DB schema). Library remains read-only (`recon` line 30). Probe cache is in-memory only (Q3).

**Testing**: Extend/add unit tests in existing unit test projects for codec/track/cache decisions (actual selected-track mapping; deterministic reattach-within-grace; grace-expiry kill/delete; overlapping waiters); extend `tests/LamuFlix.IntegrationTests/StreamEndpointTests.cs` with named AC3 fixture → copied-video / AAC stereo metadata, raw response/Range parity, 200/206/416, known conversion failure (existing API error path; no raw fallback or redirect), denied/missing/invalid (no child), LocalPlay-off raw parity (`StreamEndpointTests.cs:466-488`), absolute scratch-path stage/publish, and explicit concurrency/disconnect/cleanup. Chromium proof and post-build acceptance against real media (Q6). `LamuFlix.ArchitectureTests` required green after atomic `IStreamConversionService` whitelist delivery. No new test framework.

**Target Platform**: Compose API container and home-cluster API image; supported Chromium browser player via existing stream consumer.

**Project Type**: Existing minimal API stream slice + Infrastructure/Core conversion service inside current layers (Core port `IStreamConversionService` with ADR 0050 Accepted in Build).

**Performance Goals**: Cold preparation to audible start ≤ 300 s on the largest Compose proof sample; raw cold probe ≤ 300 ms; raw warm Range with no repeatable baseline difference (Q6). Seek resume ≤ 3 s on converted (Q6 bar c). No additional home-cluster cold timing threshold beyond the one recorded pre-build measurement.

**Constraints**: Charter §2.3 / PRODUCT §5 items deliberately ruled in Q1–Q5 (Process.Start allowed for server-side conversion; no LocalPlay read/change; no new API shape beyond Content-Type; no schema/deps/layers; no out-of-envelope rewrites). No secrets; no library writes; child args are only server-resolved paths plus generated scratch filenames with shell disabled and existing authorization-before-child. Authorized Q5 source/test envelope edits remain allowed; deploy-only edits (`Dockerfile.api` / Compose) only when AC7 proof requires them; any other tracked file needs the existing return ruling. Size L requires Risk (Sentry), Standards (Ledger), Spec (Compass). Two review rounds; residual blockers to Bernstein (Q7). Honest gate labels; never lower thresholds.

**Scale/Scope**: Size L; parent DEV-100; file envelope in brief Q5 / Structure below.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5; brief Q7).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS (design)** — Core port `IStreamConversionService`; Infrastructure adapter uses existing `IProcessRunner`; no new project/layer (`brief.md` Q5 / Step 6 F1). **Build PR (atomic, one delivery)**: introduce the port; mark `docs/adr/0050-browser-stream-audio-conversion-port.md` Accepted; amend `.specify/memory/constitution.md` Principle I enumeration (MINOR 2.0.0 → 2.1.0) with Sync Impact Report and Last Amended; add the matching `PortWhitelistPattern` entry in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`; keep `LamuFlix.ArchitectureTests` green. No unrelated Principle or whitelist edits. Spec Kit PR stays specs-only; conforming amendment — no structural owner checkbox. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — stream path remains endpoint orchestration + library query already used by raw stream (`recon-DEV-572` lines 13–15); conversion is a focused service, not a new application command handler for LocalPlay. |
| III (Typed query model) | MovieQuery | **N/A** — no browse/filter change. |
| IV (Domain transitions) | Movie mutators | **N/A** — no Movie mutation; library read-only. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — denied/missing/invalid keep current responses with no child; probe failure → warn + raw; known conversion failure → existing API error handling without new status codes, redirect, or silent raw fallback (Q3–Q4). |
| VI (Observability) | Safe diagnostics | **PASS** — structured processing and error logging MUST carry movie id; probe-failure warnings structured; no secrets in logs; process args are server-resolved paths plus generated scratch filenames only. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated; ArgumentList | **PASS** — reuse `ThumbnailOptions.FfmpegPath`; current plan uses Q3 sibling ffprobe lookup beside `FfmpegPath` (any additional key or provisioning returns through Q3); new scratch/conversion typed options under existing Options pattern with startup validation (`ValidateOnStart`) and the four `[assumed]` scratch defaults; relative scratch paths resolve under content root in Infrastructure; stage and publish inside the same resolved scratch directory; **Process.Start allowed** under Q1/Q5 for server-side conversion (not local playback execution); **do not read or change** `Features:LocalPlay`; keep `StreamEndpointTests` LocalPlay-off case (`:466-488`) green; `UseShellExecute = false` + `ArgumentList` via existing runner; authorize/resolve before any child; `TimeProvider` for idle/expiry and the 1 s grace; no secrets; no library writes; no feature-specific wall-clock conversion timeout. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local terms (converted stream, raw stream, source version, API-owned scratch); do not overload LocalPlay “Playback” folder/types; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit decisions + stream integration + Chromium proof/acceptance evidence (Q6; AC8); ArchitectureTests required. |

### Stack / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs (OpenAPI chain) | **PASS** — no DTO/route contract change; Content-Type of converted body is `video/mp4` (Q2). Preserve generated contract parity if any incidental OpenAPI touch occurs; do not invent new operations. |
| Static-analysis gates (C#) | Applicable Phase 3 step 5 / Phase 5 C# gates scoped to **changed `.cs` files**: Roslyn, cyclomatic complexity, InspectCode. When that scope is empty, record `SKIPPED (scope-empty)` honestly — never as PASS. |
| PR quality / ArchitectureTests | Build PR MUST keep `LamuFlix.ArchitectureTests` green after the atomic Principle I / `PortWhitelistPattern` / ADR 0050 Accepted delivery for `IStreamConversionService` (constitution PR Quality Gates; Step 6 F1). |
| Web gates | Not required for production `web/` (no production edit). If a return ruling later authorizes a web edit, run `scripts/run-web-gates.ps1` on that diff. |
| Other brief gates | Property tests, vulnerable packages, format, `dotnet test` (incl. ArchitectureTests). See Verification Approach. |
| Mutation | Applicable Phase 3 step 5 / Phase 5 mutation gate scoped to **production C# under `src/`**. Exit 2 `SKIPPED (scope-empty)` or exclusions **N/A** reported honestly — never as PASS. |

### Complexity Tracking

No constitution violations requiring justification. Process.Start use is an explicit §2.3 item-5 ruling (Q1/Q5), not a silent exception.

## Approach

- **Phase A**: Quill drafted Spec Kit artifacts after the grill; Keel drafted ADR 0050 (Proposed) and Step 6 decisions. Keel records the plan-challenge freeze in `CONCLUSIONS.md`. Patron then sets provisional Gate 1. Bernstein coordinates and Rigger opens the Spec Kit PR (`specs/**` only; ADR Accepted moves with Build). Owner merge and Keel’s verified T000 receipt unlock pickup. No structural owner checkbox.
- **Phase B pickup**: Confirm authorized seams against pickup HEAD (read-only). Confirm exact DI composition site for the Streaming registration (return for Q5 ruling if outside the authorized envelope). Record deployment replica and scratch-ownership topology and derive aggregate limits from evidence; require exclusive owned cleanup domains if storage is shared before AC7 cluster acceptance. Establish `SystemProcessRunner` cancel/kill and output-capture contracts with cited evidence. Confirm ffmpeg/ffprobe tool availability and a reproducible AC3 fixture source (missing prerequisite → explicit blocker, not a silent green). Confirm Q3 sibling ffprobe resolution beside `ThumbnailOptions.FfmpegPath`. STOP for cited Patron ruling before any out-of-envelope edit or missing-ffprobe provisioning.
- **Feasibility proof (before any production code; `brief.md` Task order item 1; Q2; Q6; Step 6 R2)**: Isolated, read-only proof writing only to API-owned scratch; byte-range-capable origin; supported Chromium; movie 959 + one real AC3 + one real DTS. On Compose record bars (a)–(f), Q2 subtitle / subtitle-delay / existing-control parity for those samples, selected input codecs in the three sample rows, tool versions/paths, and writable scratch. Demonstrate last-waiter cancellation of a deliberately long-running child with no leftover process or partials after the 1 s grace; if that path cannot meet AC6, **STOP and return to Patron** with evidence (analyze / Step 6 R2). On home cluster (pre-build) record tool/path/scratch plus one cold-preparation timing only — do not invent an additional cluster timing threshold; full cluster playback bars stay post-build. **Require** proof measurements in this `plan.md` as ruled; a linked evidence note may supplement but does not replace the plan tables. Miss → reopen Q1/Q2/Q3 as ruled — do not re-encode video or silently pass incompatible audio. Missing ffprobe → Q3 provisioning stop.
- **Local Build (`tasks.md` phases; Anvil one phase per ask)**:
  1. Gate 1, then pickup recon (T001 evidence above).
  2. Proof tasks complete and recorded in plan measurements, including forced-hang cancel proof.
  3. Unit-tested codec/track/cache + conversion/scratch service (failing tests first), including selected-track mapping, bounded probe JSON cases, version recheck/mismatch discard, shared-conversion waiter semantics, deterministic reattach-within-grace and grace-expiry cleanup; typed options with startup validation; instance-scoped lifecycle + owned-orphan startup cleanup; structured processing/error logs with movie id.
  4. Introduce `IStreamConversionService` **atomically** with ADR 0050 Accepted, Principle I 2.0.0 → 2.1.0 (Sync Impact + Last Amended), and matching `PortWhitelistPattern`; ArchitectureTests green.
  5. Wire thin dispatch in `StreamEndpoints.cs` at the DI site confirmed in T001; runner change only if AC6 kill-on-cancel needs it (cite AC6).
  6. Integration coverage including named AC3 fixture, raw/Range/auth/error/no-process cases, LocalPlay-off raw parity, absolute scratch-path stage/publish, concurrency/disconnect (incl. grace) / cleanup.
  7. Local Build ends at integrated automated verification — Chromium post-build acceptance is Conductor-sequenced evidence, not an Anvil claim of hosted merge readiness.
- **Downstream after Build**: Cog `/refactor` → Gauge Phase 3 step 5 → Gauge pre-pass + Sentry/Ledger/Compass (Size L) → Keel adjudicates findings → Phase 5 rebase and gate rerun → post-build acceptance on Compose and home cluster enumerating FR-015 integrated route checks (disconnect cases observed after grace). Two rounds max; residual blockers to Bernstein (Q7).
- **Do not**: add dependencies/layers/schema; change route shape; touch LocalPlay; write into the library; put conversion code in LocalPlay `Infrastructure/Playback`; put probe cache / in-flight map / scratch index in static mutable fields (instance-scoped only; codec set as `const` or computed getter); edit production `web/` without the existing return ruling; claim Gate 1 / proof PASS / runtime PASS without receipts; lower gate thresholds; invent a second Core port or ADR number; add a wall-clock conversion timeout. Authorized Q5 source/test envelope edits remain allowed and include the Build-only constitution + ArchitectureTests whitelist files named by Step 6 F1. `Dockerfile.api` / Compose are the deploy-only edit envelope and only when AC7 proof shows need; any other tracked file outside that authorized Q5 + F1 envelope needs the existing return ruling.

## Assumed scratch defaults (Q4 taste)

Logged in `ASSUMPTIONS.md` as `[assumed]` (four defaults only):

| Setting | Default | Notes |
|---|---|---|
| Max concurrent conversions per API instance | `2` | `[assumed]` — still one in-flight per source version; caps distinct sources converting at once |
| Idle expiry after last reader release | `60 minutes` | `[assumed]` — completed assets only |
| Scratch size bound | `20 GiB` | `[assumed]` — LRU among completed assets with zero readers |
| Scratch directory default | `cache/stream-conversion` (under content root unless absolute path configured) | `[assumed]` — isolated from library; mirrors thumbnail cache style |

Per the analyze ruling in `brief.md` and `CONCLUSIONS.md` (`79c86dc4`), retain existing runner behavior and Q4 child kill and partial-file cleanup: conversion failure cleans up immediately; zero-waiter in-flight cleanup follows the 1 s internal reattachment grace (`ASSUMPTIONS.md`; Step 6 R1). The reopened Q6 ruling sets 300 s for the Compose feasibility proof only. Add no feature-specific wall-clock timeout or timeout default while a waiter remains attached. Return to Patron if proof shows the runner cannot meet Q4 or Q6 (Step 6 R2).

## Feasibility proof measurements (required in this plan before Build)

Linked evidence may supplement these tables; it does not replace them.

| Sample | Selected input codec | Cold prep → audible (s) | Duration OK | Seek ≤3 s (click + arrows) | Range-abort OK | Video copy | AAC audio | Subtitle / delay / controls parity (Q2 Compose) |
|---|---|---|---|---|---|---|---|---|
| Movie 959 | E-AC3 (`eac3`, first audio stream index 1, no default flag) | **290.873 s** end-to-end on the resumed proof (start epoch 1795695231332 → audible epoch 1795695522205): ffmpeg 288.540 s plus 1.122 s metadata/play/audio. Under the 300 s cap. | PASS: browser duration 10473.600 s = source 10473.600 s | PASS: real timeline click to 1200 s 2181 ms, ArrowRight 661 ms, ArrowLeft 859 ms to audible | PASS: AbortError + new range resumed in 1369 ms; HEAD 200 after abort | PASS: `hevc` Main 10 1440x1080; source/output video packet MD5 both `01503f96424c9a0d099af85c52276d24` | PASS: `aac` LC stereo at 194272 bit/s | PASS: toggles, delay ±0.5 s and controls identical to raw AAC comparator |
| Real AC3 | AC3 (`ac3`, first/default audio stream index 1); verified from `E:\Filmes\Studio Ghibli Collection\Pom Poko[1994]\Pom Poko 1994 720p BluRay x264 AC3 - Ozlem.mp4` | **155.769 s composed** (ffmpeg 142.415 s + Chromium load-to-audible 13.354 s; intro audio starts at ~12.5 s) | PASS: browser 7151.393833 s vs source 7151.394250 s (0.0004 s container rounding) | PASS: real timeline click to 600 s 807 ms, ArrowRight 404 ms, ArrowLeft 403 ms | PASS: AbortError + ArrowRight resumed in 404 ms; HEAD 200 after abort | PASS: `h264` High 1280x688; packet MD5 both `87c83d2609f598ff227cc09f71dbf7fd` | PASS: `aac` LC stereo at 194574 bit/s | PASS: same toggle/delay/control observations as raw comparator |
| Real DTS | DTS (`dts`, first/default audio stream index 1; second stream is non-default AC3); verified from `E:\Filmes\Justice League Collection\Justice League - Doom[2012]\Justice.League.Doom.2012.720p.BluRay.x264.DTS-HDChina.mkv` | **149.733 s composed** (final selected-track ffmpeg 146.247 s + Chromium load-to-audible 3.486 s) | PASS: browser 4612.320 s vs source 4612.384 s (0.064 s AAC frame rounding) | PASS: real timeline click to 120 s 744 ms, ArrowRight 657 ms, ArrowLeft 656 ms | PASS: aborted + ArrowRight resumed in 506 ms; HEAD 200 after abort | PASS: `h264` High 1280x720; packet MD5 both `f0d186e8b2637de12c51e2d7f51c09dc` | PASS: `aac` LC stereo at 196692 bit/s | PASS: same toggle/delay/control observations as raw comparator |

The Compose cap is **300 s from cold preparation to audible start** on the **largest** of the three samples (reopened Q6 bar e, `CONCLUSIONS.md`). Movie 959 is the longest sample (10473.600 s) and measured **290.873 s end-to-end**, so Q6 bar (e) passes with 9.127 s headroom. The earlier 272.786 s (`+faststart`) and 285.625 s (no faststart) conversion-only timings failed the former 120 s bar and are preserved in the receipt below; they are superseded for the current bar by the resumed end-to-end run. The resumed output begins with `ftyp`/`free`/`mdat`, has `moov` in the final 8 MiB, and is Range-servable. T002 is complete. T003 (cluster pre-build tool/scratch check and one cold timing) is also complete; the cluster number is recorded only and is not judged against the Compose bar.

| Environment | ffmpeg path/version | ffprobe path/version | Scratch writable | Cold prep note |
|---|---|---|---|---|
| Local Compose API | `/usr/bin/ffmpeg`, `ffmpeg version 6.1.1-3ubuntu5` | `/usr/bin/ffprobe`, `ffprobe version 6.1.1-3ubuntu5` | `/app/cache/stream-conversion-proof` writable = yes; dedicated `{src,out,bin,logs}` inside API-owned scratch | Resumed T002: movie 959 290.873 s cold prep → audible (bar e PASS), AC3 155.769 s composed, DTS 149.733 s composed; tools re-verified in `lamuflix-api-1` |
| Home cluster API (pre-build) | `/usr/bin/ffmpeg`, `ffmpeg version 6.1.1-3ubuntu5` (built with gcc 13 / Ubuntu 13.2.0-23ubuntu3) | `/usr/bin/ffprobe`, `ffprobe version 6.1.1-3ubuntu5` | `/app/cache/stream-conversion-proof` writable = yes (created `{src,out,logs}`, write/read/remove test passed, app:app-owned overlay under `/app/cache`) | **273.051 s** cold preparation to completed, published MP4: start epoch 1795696155498 → end epoch 1795696428549, `elapsed_ms=273051`, `ffmpeg_rc=0`, final `speed=38.4x`, atomic rename in the same scratch dir. Movie 959 only; browser-audible timing and full playback bars stay post-build (FR-015). No cluster cap is inferred. |

Missing ffprobe on either environment → **STOP** for Q3 provisioning ruling.

| Raw path timing | Baseline | With probe | Delta |
|---|---|---|---|
| Cold first request | 136.3 ms first Range from origin with no probe (raw AAC sample) | 46/48/47/62/94 ms ffprobe calls in the API container, then 115.0 ms first Range | PASS: worst probe 94 ms ≤ 300 ms; probe adds no measured Range regression |
| Warm Range | 8 requests: 136.3/121.5/121.6/123.1/132.5/120.3/113.4/121.3 ms (median 121.4 ms) | 8 requests after three ffprobe calls: 115.0/119.2/121.2/108.9/122.3/119.5/112.4/118.5 ms (median 119.35 ms) | PASS: median delta −2.05 ms; no repeatable difference (Q6 bar f) |

### T002 re-proof receipt — Anvil, before production code

- Worktree `F:/Dev/LamuFlix.worktrees/DEV-572`, branch `feature/DEV-572`; first partial receipt at HEAD `23866df574eb705b38f918d3fa267a0d084c5dff`, Patron re-proof ruling committed at `26c20a9e`. No production conversion code was edited; only this plan is updated with proof evidence.
- Compose container `lamuflix-api-1`: `command -v ffmpeg` → `/usr/bin/ffmpeg`, `command -v ffprobe` → `/usr/bin/ffprobe`; both report `6.1.1-3ubuntu5`. `/app/cache` is writable; scratch proof subdirectories were created there. No `Dockerfile.api` or Compose edit was required or made.
- Real samples were located and copied read-only from the host library into API-owned scratch: movie 959 `You.Can.See.Everything.2026.1080p.WEBRip.10Bit.DDP5.1.x265-NeoNoir.mkv` (2,320,887,181 bytes; source video `hevc` Main 10 1440x1080, selected audio stream 1 `eac3`, duration 10473.600 s); real AC3 `Pom Poko 1994 720p BluRay x264 AC3 - Ozlem.mp4` (990,186,485 bytes; `h264` + `ac3`, duration 7151.394 s); real DTS `Justice.League.Doom.2012.720p.BluRay.x264.DTS-HDChina.mkv` (2,967,209,285 bytes; `h264` + default `dts`, duration 4612.384 s). ffprobe in the API container confirmed these codecs and selected tracks.
- First invocation used the Q2 design: video `-c:v copy`, selected audio `-c:a aac -b:a 192k -ac 2`, non-fragmented MP4 with `-movflags +faststart`; output written only to `/app/cache/stream-conversion-proof/out`. Movie 959 measured `elapsed_ms=272786` (272.786 s); log `/app/cache/stream-conversion-proof/logs/movie959.mkv.ffmpeg.log`.
- Per Patron's 26c20a9e Q1 ruling, the re-proof invocation dropped `+faststart` only: `/usr/bin/ffmpeg -nostdin -y -i src/movie959.mkv -map 0:v:0 -map 0:a:0 -c:v copy -c:a aac -b:a 192k -ac 2 out/movie959-nofs.mp4`, with all paths inside `/app/cache/stream-conversion-proof`. Measured `rc=0 elapsed_ms=285625` (285.625 s); final progress `speed=37.4x`; log `/app/cache/stream-conversion-proof/logs/movie959-nofs.log`. Output ffprobe: `hevc` Main 10 1440x1080 copied video, `aac` stereo audio, duration 10473.600 s, size 2,245,194,955 bytes. First boxes are `ftyp`/`free`/`mdat`; `moov` is in the final 8 MiB — non-fragmented and Range-servable, but 13.8 s slower than the faststart run.
- Both invocations failed the 120 s Q6 bar (e) in effect at the time on the largest sample. Per the 26c20a9e return condition this is `blocked` with both numbers returned to Patron; no bar or model change was made, and no bar (a)–(d), raw timing, or forced-hang cancellation proof was run after the second failure. Q2 compatibility/browser proof remains unverified.
- AC3/DTS conversions were not attempted after either stop; the AC3 attempt in the first batch failed only because of an incorrect audio-map index (`0:a:1` on a single-audio file) and produced no output, so that malformed invocation is not proof evidence.
- `T002` remains unchecked. No production conversion code was changed; no commit was created for the failed re-proof.

### T002 resumed proof receipt — Anvil, before production code

- Worktree `F:/Dev/LamuFlix.worktrees/DEV-572`, branch `feature/DEV-572`, HEAD `1421d21346ddfca361ef84e6c36b1d7b834dcb33` (Keel's 300 s bar update). Only `specs/DEV-572/plan.md` and `specs/DEV-572/tasks.md` are changed by this receipt; no `src/`, `web/`, Dockerfile, Compose, secrets, or `Features:LocalPlay` access. Tool check in `lamuflix-api-1`: `command -v ffmpeg` → `/usr/bin/ffmpeg`, `command -v ffprobe` → `/usr/bin/ffprobe`; both `6.1.1-3ubuntu5`; `/app/cache/stream-conversion-proof` writable.
- Isolated origin: a Node host-side byte-range server read only from the API container path via `docker exec dd`, supporting `Accept-Ranges`, 206 `Content-Range`, HEAD and abort. It wrote no media outside `/app/cache/stream-conversion-proof`; its request log and JSON observations are in `C:/Users/lamun/AppData/Local/Temp/dev572-proof/` (supplemental only; numbers are in the tables above). Chromium was Playwright `HeadlessChrome/158.0.0.0` (`Google Inc.`). Audio audibility was measured with a Web Audio `AnalyserNode` on the media element (RMS peaks 0.0014–0.156), not by inferring from `play()`.
- Conversions used the Q2 command shape with `+faststart` dropped: `/usr/bin/ffmpeg -nostdin -y -i <server-resolved source> -map 0:v:0 -map 0:a:<selected> -c:v copy -c:a aac -b:a 192k -ac 2 <partial>`, followed by an atomic rename to the final name inside the same scratch directory. Selected tracks were `0:a:0` for all three (movie 959 has only one audio stream; AC3 is default; DTS is first and default, with a non-default AC3 second stream). An earlier DTS attempt with `0:a:1` mapped the wrong stream and is excluded from evidence; the final DTS output was rebuilt with `0:a:0`.
- Output metadata: movie 959 `hevc` Main 10 1440x1080 + `aac` LC stereo, 10473.600 s, 2,245,194,955 bytes; AC3 `h264` High 1280x688 + `aac` LC stereo, 7151.393833 s, 1,080,947,550 bytes; DTS `h264` High 1280x720 + `aac` LC stereo, 4612.320 s, 2,094,913,610 bytes. Source-versus-output copied-video packet MD5s matched exactly for all three (values in the sample table), which closes Q6 bar (a).
- Movie 959 cold run was end-to-end: t0 recorded immediately before the ffmpeg invocation; the browser polled HEAD until publication, loaded metadata, played, and reported audible at 1795695522205. ffmpeg `rc=0 elapsed_ms=288540`; browser `coldPrepToAudibleMs=290873`, `audibleWaitMs=1122`, `peakRms=0.0769`. This is **≤ 300 s** on the largest sample. AC3 and DTS numbers are the sum of their conversion elapsed times and separately measured load-to-audible waits (13.354 s / 3.486 s), because their conversions completed before the browser run was added; both are well under the cap and do not determine the largest-sample bar.
- Q6 bar (c): real Playwright pointer clicks on the timeline landed at 1200 s (959, 2181 ms to audible), 600 s (AC3, 807 ms), and 120 s (DTS, 744 ms). ArrowRight +10 s and ArrowLeft −10 s from the seek sequences returned to audible in 661/859 ms (959), 404/403 ms (AC3), and 657/656 ms (DTS). All nine observations are ≤ 3000 ms with `playing=true` and advancing `currentTime`; the click targets differ only because the native range thumb padding made a 120 s click unreliable on the two very long timelines, so larger click offsets were used to exercise the same seek path. Q6 bar (d): a bounded Range fetch was aborted (`AbortError` on 959/AC3; `aborted` on DTS), a fresh ArrowRight seek resumed audible playback in 1369/404/506 ms, and HEAD still returned 200 with 206 range activity in the origin log.
- Q2 parity: the proof page used the app's ±10 s key behavior, a clickable timeline, play/pause, mute and a WebVTT track with ±0.5 s cue shifting. At 6.2 s, baseline had no cue, +0.5 s showed `DEV-572 parity cue A` in `TextTrack.activeCues`, and −1.0 s again had no cue. Converted samples and the supported-codec raw AAC comparator produced identical `trackCount=1`, `subtitlesOnMode=showing`, `playChanged=true`, `mutedChanged=true`, `cuesOn=[]`, `cuesDelayed=[A]`, `cuesEarlier=[]` results.
- Q6 bar (f): ffprobe on the raw AAC sample measured 46/48/47/62/94 ms (all ≤ 300 ms). Baseline warm Range latencies (median 121.4 ms) and post-probe warm Range latencies (median 119.35 ms) show no repeatable difference; bar (f) passes.
- AC6 forced-hang proof: a temporary harness compiled the exact worktree `SystemProcessRunner.cs`, `ProcessResult.cs` and `WindowsJobObject.cs` (no production edit) and ran inside `lamuflix-api-1` through the existing `IProcessRunner.RunAsync` cancellation path on a 1-hour synthetic ffmpeg child writing `out/forced-hang.partial.mkv`. With 262,144 partial bytes present and one ffmpeg process, caller cancellation produced `OperationCanceledException` at 221 ms; after the 1 s zero-waiter grace, `processesAfterGraceMs1000=0` and `partialExistsAfterGrace=False`. No feature-specific wall-clock timeout was added; the runner's API-required `TimeSpan.FromHours(1)` was not reached. Final container check showed no ffmpeg/ffprobe process and no `*.partial*`/`*.tmp` file.
- Verdict: all Compose T002 bars (a)–(f), Q2 parity, tool/scratch checks and forced-hang cancellation pass on the resumed run. T002 is marked complete; T003 remains the next, separate task.

### T003 cluster pre-build receipt — Anvil, before production code

- Worktree `F:/Dev/LamuFlix.worktrees/DEV-572`, branch `feature/DEV-572`, HEAD `4f247608e26566465f3746ef4ab3bf6a6ca7ac0a`. Only this plan and `tasks.md` T003 are changed. No `src/`, `web/`, Dockerfile, Compose, secrets, or `Features:LocalPlay` access; no new dependency, schema, route, or cluster threshold. Compose was not substituted for the cluster check.
- Running home cluster provenance: cluster `lamuflix-home` via the scratch kubeconfig named in `F:/Dev/LamuFlix-home/home.config.json` (not the user kubeconfig); node `lamuflix-home-control-plane` `Ready`, Kubernetes `v1.37.0`; API pod `lamuflix-home/api-5dd847b445-4j288` `1/1 Running`; image `ghcr.io/lamuniercosta/lamuflix-api@sha256:1dd1dc9467921956306617f8f06859ee7bd72fb19b6d9541387b65318615ac7a`. All checks below are `kubectl exec` inside that running API container, not inside Compose.
- Tool check in the running API image as `uid=1654(app)`: `command -v ffmpeg` → `/usr/bin/ffmpeg`; `command -v ffprobe` → `/usr/bin/ffprobe`; first version lines `ffmpeg version 6.1.1-3ubuntu5 Copyright (c) 2000-2023 the FFmpeg developers` and `ffprobe version 6.1.1-3ubuntu5 Copyright (c) 2007-2023 the FFmpeg developers`, both `built with gcc 13 (Ubuntu 13.2.0-23ubuntu3)`. ffprobe is present, so the Q3 provisioning stop does not apply.
- Scratch check: `/app/cache` is app-owned; `/app/cache/stream-conversion-proof/{src,out,logs}` were created under it, and a `scratch-write-test.txt` write → read → remove cycle succeeded. `df -h /app/cache` reported an overlay with 914G available. Scratch is pod-local/ephemeral (T001 topology); no shared scratch or deployment change was introduced.
- Source handling: the running pod's `/media/library` mount reported `No such device` at check time (the kind node shows the `E:\` 9p mount at `/media/library`), so the named movie 959 file was copied read-only from the host library `E:\Filmes\You Can See Everything[2026]\You.Can.See.Everything.2026.1080p.WEBRip.10Bit.DDP5.1.x265-NeoNoir.mkv` into `/app/cache/stream-conversion-proof/src/movie959.mkv` with `kubectl cp`; both host and pod report 2,320,887,181 bytes. The library was not written, and no Dockerfile/Compose/manifest edit was made. Pod ffprobe confirmed `hevc` Main 10 1440x1080, selected audio stream 1 `eac3`, duration 10473.600000 s — the same T002 sample.
- One cold timing (AC7): `/usr/bin/ffmpeg -nostdin -y -i /app/cache/stream-conversion-proof/src/movie959.mkv -map 0:v:0 -map 0:a:0 -c:v copy -c:a aac -b:a 192k -ac 2 /app/cache/stream-conversion-proof/out/movie959.partial.mp4` (no faststart, matching the resumed T002 command shape), log `/app/cache/stream-conversion-proof/logs/movie959.cluster.ffmpeg.log`. `start_epoch_ms=1795696155498`, `end_epoch_ms=1795696428549`, `elapsed_ms=273051` (273.051 s), `ffmpeg_rc=0`, final progress `speed=38.4x`, then an atomic `mv` to `/app/cache/stream-conversion-proof/out/movie959.mp4` in the same scratch directory; output 2,245,194,955 bytes.
- Output ffprobe: `hevc` Main 10 video + `aac` LC stereo (48000 Hz, 2 channels), duration 10473.600000 s; first boxes `ftyp`/`free`, consistent with the non-fragmented Range-servable MP4 design. After completion `pgrep -x ffmpeg` = none, `pgrep -x ffprobe` = none, and a scratch scan found no `*.partial*` or `*.tmp` file.
- Scope of the number: 273.051 s is **cold preparation to the completed, published MP4 on the cluster CPU**, not cold-preparation-to-audible and not a browser bar. Per FR-013/FR-015, cluster audible playback, seek, subtitle/control parity and fault cases remain post-build acceptance; no extra cluster threshold is asserted. Verdict: T003 PASS with the required tool/path/scratch check and one recorded cold timing; no AC7 deploy edit was needed.

## Verification Approach

- **Proof**: Q6 bars (a)–(f) + Q2 Compose subtitle/delay/control parity + selected codecs in the three sample rows + Compose tool/scratch evidence + forced-hang last-waiter cancel/kill after grace + cluster pre-build tool/scratch/one cold timing; measurements required in plan tables above before production edits; failed cancel proof returns to Patron.
- **Unit**: Codec list, actual selected-track mapping (default/first), no-audio, other codecs, probe fault → structured warn+raw (distinct from cancellation), bounded/malformed/oversized/missing-field probe JSON → warn+raw, cache key reuse/invalidation, mid-flight source-version mismatch discard, one-in-flight sharing, **deterministic reattach-within-grace**, **grace-expiry kills child and deletes partials**, overlapping waiters stay alive, publish/evict rules (never evict active readers; coordinate open/disposal contention).
- **Integration**: Named AC3 fixture stream → copied-video / AAC stereo via metadata (tool/fixture missing → explicit blocker); raw response/Range parity; 200/206/416; known conversion failure uses existing API error path with no raw fallback or redirect; denied/missing/invalid with no child; LocalPlay-off raw parity (`StreamEndpointTests.cs:466-488`; route seams `recon-DEV-572` lines 13–15); absolute scratch-path stage/publish in the same directory; explicit concurrency, disconnect (incl. grace window), and cleanup coverage at this layer where HTTP-visible.
- **Implementation/verification trust rules**: no secrets; no library writes; only server-resolved paths plus generated scratch filenames; retain existing authorization-before-child and structured-argument / shell-disabled rules; typed options startup-validated; relative scratch under content root in Infrastructure; instance-scoped lifecycle + owned-orphan startup cleanup; structured processing/error logs carry movie id.
- **Post-build acceptance (both deployments, integrated route; FR-015)**: on the three named converted samples (959, real AC3, real DTS) — audible AAC and video-copy output evidence; full duration; both seek modes within 3 s; old-range abort and new-range continuity; subtitle/delay/control parity; three fault/cleanup cases (forced failure, mid-conversion disconnect observed after the 1 s grace, cancelled seek) leave no process or partials; and separately, supported-codec raw cold/warm baseline comparison. Preserve pre-build cluster (tool/scratch + one cold timing recorded in plan measurement tables) vs post-build full playback bars; do not invent a new cluster timing threshold.
- **Gates (downstream)**: Roslyn, cyclomatic complexity, InspectCode, scoped mutation, property tests under standing task-note rules, vulnerable packages, format, `dotnet test` including `LamuFlix.ArchitectureTests` after the atomic whitelist amendment. Record `SKIPPED (scope-empty)`, harness `SKIP`, exclusions **N/A** honestly; never fold into PASS. Exit 1 and `Could not run` block.
- **Review (downstream)**: Size L three axes; ADR 0050 Accepted with Principle I / whitelist delivery; every finding dispositioned; Critical/High or AC/gate/trust-boundary failures block; two rounds then Bernstein (Q7).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-572/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── stream-conversion.md
```

### Source Code (authorized envelope — brief Q5)

```text
src/LamuFlix.Api/Endpoints/
└── StreamEndpoints.cs                 # thin auth / resolve / raw-vs-converted dispatch only
# Exact DI composition call site for Streaming registration: confirm at T001 (Q5 return if outside envelope)

src/LamuFlix.Core/
├── Options/                           # stream-conversion / scratch typed options (+ ValidateOnStart)
└── Ports/
    └── IStreamConversionService.cs    # single narrow Core port (ADR 0050)

src/LamuFlix.Infrastructure/
├── Thumbnails/
│   ├── SystemProcessRunner.cs         # change ONLY if AC6 kill-on-cancel/disconnect requires it (cite AC6)
│   └── ThumbnailServiceCollectionExtensions.cs  # existing IProcessRunner registration reused
└── Streaming/                         # NEW subfolder (not a new layer; NOT LocalPlay Playback/)
    ├── StreamConversionService        # probe, cache, convert, instance-scoped lifetime + startup orphan cleanup
    └── StreamingServiceCollectionExtensions  # DI registration (exact Api composition site from T001)

.specify/memory/constitution.md        # Build-only: Principle I append IStreamConversionService; 2.0.0 → 2.1.0; Sync Impact; Last Amended
docs/adr/0050-browser-stream-audio-conversion-port.md  # Proposed now; Accepted in same Build PR
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs  # Build-only: PortWhitelistPattern += IStreamConversionService

tests/LamuFlix.UnitTests/…             # codec/track/cache/lifetime/grace unit tests
tests/LamuFlix.IntegrationTests/
└── StreamEndpointTests.cs             # extend; AC3 fixture + parity/cleanup/absolute-scratch cases
tests/.../fixtures/                    # reproducible small AC3 fixture (T001 records source)

docker/Dockerfile.api                  # ONLY if proof shows AC7 need (Q5 deploy envelope)
compose files                          # ONLY if proof shows AC7 need (Q5); any other tracked file needs existing return ruling
```

**Structure Decision**: Keep conversion out of `Infrastructure/Playback` (that folder owns LocalPlay launchers — `ProcessMediaPlayerLauncher`, `DisabledMediaPlayerLauncher`). A new `Infrastructure/Streaming` subfolder is allowed under Q5 (“new subfolder … is not a new layer”) and avoids LocalPlay entanglement. Single port `IStreamConversionService` + one adapter; do not split probe/scratch/converter into extra ports. Reuse `SystemProcessRunner` in place; do not move/rename Thumbnails types (§5 item 6). Current ffprobe plan is sibling lookup beside `FfmpegPath`; any additional key or provisioning returns through Q3. Mutable probe cache, in-flight map, and scratch index are instance-scoped singleton state; no static mutable caches. Owned-orphan startup cleanup lives with that instance lifecycle (no dedicated hosted-service type required by the settled contract). Build PR delivers the port, ADR 0050 Accepted, Principle I 2.0.0 → 2.1.0 amendment, and matching `PortWhitelistPattern` atomically with ArchitectureTests green (Step 6 F1). Authorized Q5 source/test edits plus those named F1 files remain in scope. Deploy-only edits (`Dockerfile.api` / Compose) are conditional on AC7 proof need. STOP for cited Patron ruling before any other tracked-file edit, production `web/` change, or ffprobe provisioning work.

## Phase 0 / Phase 1 Spec Kit outputs

Skipped as separate files: inventable unknowns are frozen by Q1–Q7; no new public API contract package; no schema data model. This plan plus `spec.md` / `tasks.md` are the design record.
