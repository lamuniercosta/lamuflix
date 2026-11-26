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

**Performance Goals**: Cold converted preparation ≤ 120 s on largest proof sample; raw cold probe ≤ 300 ms; raw warm Range with no repeatable baseline difference (Q6). Seek resume ≤ 3 s on converted (Q6 bar c). No additional home-cluster cold timing threshold beyond the one recorded pre-build measurement.

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

Per the analyze ruling in `brief.md` and `CONCLUSIONS.md` (`79c86dc4`), retain existing runner behavior and Q4 child kill and partial-file cleanup: conversion failure cleans up immediately; zero-waiter in-flight cleanup follows the 1 s internal reattachment grace (`ASSUMPTIONS.md`; Step 6 R1). Q6 sets the 120 s feasibility bar only. Add no feature-specific wall-clock timeout or timeout default while a waiter remains attached. Return to Patron if proof shows the runner cannot meet Q4 or Q6 (Step 6 R2).

## Feasibility proof measurements (required in this plan before Build)

Linked evidence may supplement these tables; it does not replace them.

| Sample | Selected input codec | Cold prep → audible (s) | Duration OK | Seek ≤3 s (click + arrows) | Range-abort OK | Video copy | AAC audio | Subtitle / delay / controls parity (Q2 Compose) |
|---|---|---|---|---|---|---|---|---|
| Movie 959 | _pending proof_ | _pending proof_ | | | | | | |
| Real AC3 | _pending proof_ | _pending proof_ | | | | | | |
| Real DTS | _pending proof_ | _pending proof_ | | | | | | |

120 s cold-prep cap applies to the **largest** of the three samples (Q6 bar e).

| Environment | ffmpeg path/version | ffprobe path/version | Scratch writable | Cold prep note |
|---|---|---|---|---|
| Local Compose API | _pending_ | _pending_ | _pending_ | bars (a)–(f) + Q2 subtitle/control parity |
| Home cluster API (pre-build) | _pending_ | _pending_ | _pending_ | tools + path + scratch + **one** cold timing only (no extra cluster threshold) |

Missing ffprobe on either environment → **STOP** for Q3 provisioning ruling.

| Raw path timing | Baseline | With probe | Delta |
|---|---|---|---|
| Cold first request | _pending_ | _pending_ | must be ≤ 300 ms |
| Warm Range | _pending_ | _pending_ | no repeatable difference |

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
