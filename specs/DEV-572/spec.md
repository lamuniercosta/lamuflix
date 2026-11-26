# Feature Specification: Browser-unsupported audio stream conversion

**Feature Branch**: `feature/572-spec`

**Created**: 2026-11-26

**Status**: gate1: provisional

**Input**: Ticket DEV-572 (parent DEV-100; size L, no UI); grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-572`, `recon-DEV-572`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope; Q7; `PRODUCT.md` §5). Ordinary owner Gate 1 remains **pending** until the user merges the Spec Kit PR. Sequence: clean plan challenge → Keel freeze receipt (PLAN FROZEN) → Patron provisional / Spec PR → owner merge completes Gate 1. Feasibility proof, Build, integrated acceptance, and Size L review remain later stages.

**Spec PR note (not a checkbox)**: Q6 bar (f) allows up to 300 ms of cold first-request probe cost on the raw path as Patron’s reading of AC2 (`brief.md` Q6–Q7; `CONCLUSIONS.md` Q6–Q7).

## Clarifications

### Session 2026-11-26

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Delivery model, `video/mp4` container, probe/track/codec policy, scratch lifetime and abort semantics, architecture/trust boundary, proof bars and ordering, frozen scope and closing bar are settled in `brief.md` Q1–Q7 and `CONCLUSIONS.md`. Scratch size/concurrency/idle-expiry defaults are taste-level for the plan, tagged `[assumed]` in `ASSUMPTIONS.md`. Conductor asked Quill to run specify/clarify/plan/tasks/checklist in one ask without inventing decisions. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: Browser movie player (existing stream consumer of `GET /api/movies/{id}/stream`).
- **What the user sees or does**: Movies whose first/default audio track is AC3, E-AC3, or DTS play with audible sound. Movies with browser-supported audio (AAC, MP3, Opus, Vorbis) behave as today. Full duration, click/arrow-key ±10 s seeking, subtitles, subtitle delay, and existing player controls match current raw playback behaviour. No new player UI, route, or client API.
- **What it needs to show up**: Compose (or home-cluster) API with ffmpeg/ffprobe available, writable API-owned scratch, and library movies including unsupported-audio samples (movie 959 plus real AC3 and DTS files for acceptance). No web production change within this scope.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library movie whose first/default audio is AC3, E-AC3, or DTS (including movie 959 and one real AC3 and one real DTS sample), **When** the viewer plays it in the browser player, **Then** audible sound plays with full duration shown.
- **SMOKE-002**: **Given** that same converted movie is playing, **When** the viewer seeks by click or arrow-key ±10 s, **Then** playback resumes with audible sound within 3 s and existing subtitle/controls behaviour is unchanged.
- **SMOKE-003**: **Given** a movie whose audio is already AAC, MP3, Opus, or Vorbis, **When** the viewer plays and seeks it, **Then** playback matches today’s raw path (no conversion delay on seek; warm range behaviour unchanged).

## Scope

**In scope** (`brief.md` Frozen scope / Implementation decisions / Task order; Q1–Q7; ticket intent and AC1–8 at `recon-DEV-572` lines 27–39):

1. **Feasibility proof (read-only, before production code)**: Isolated proof with API-owned scratch and a byte-range-capable origin; movie 959 plus one real AC3 and one real DTS file on supported Chromium; record Q6 bars (a)–(f), Q2 Compose subtitle / subtitle-delay / existing-control parity for those samples, and deployment tool/scratch evidence for Compose and home cluster (`brief.md` Task order item 1; Q2; Q6).
2. **Probe and dispatch**: At play time, probe real media with ffprobe JSON; select first default audio track else first audio in input order; convert only AC3 / E-AC3 / DTS; keep AAC/MP3/Opus/Vorbis, other codecs, and successful probes with no audio on the raw path; cache successful probes in memory by path + size + last-write time; on probe failure warn and serve raw; propagate cancellation (Q3).
3. **Conversion and scratch**: Completed non-fragmented MP4 (`video/mp4`); copy primary video; AAC stereo for the selected track only; one in-flight conversion per source version per API instance; on conversion failure kill child and delete partials immediately; on zero-waiter transition apply the 1 s internal same-source reattachment grace then kill child and delete partials at expiry if still zero waiters (`brief.md` Step 6; R1); publish by atomic rename staging and publishing inside the same resolved scratch directory; completed assets survive Range abort; govern by idle expiry, size-bound LRU (never while a reader holds), source-version change, and owned orphan startup cleanup; known conversion failure uses existing API error handling with no redirect and no silent raw fallback (Q1–Q2, Q4; Step 6).
4. **Route and trust boundary**: Keep authenticated `GET /api/movies/{id}/stream`, existing browser caller, and status set 200/206/416; only Content-Type differs for converted bytes (`video/mp4`); authorize/resolve primary media through the existing library boundary before any child; structured args, shell disabled; library read-only; no secrets; do not read or change `Features:LocalPlay` (Q1, Q5).
5. **Code envelope**: Thin `StreamEndpoints` auth/resolution/dispatch; one playback-specific conversion service behind Core port `IStreamConversionService` plus options in existing Core/Infrastructure layers (not the LocalPlay `Infrastructure/Playback` folder); reuse `SystemProcessRunner` and `ThumbnailOptions.FfmpegPath`; runner change only if AC6 requires it, cited in the plan; tests/fixtures including AC3 fixture; Dockerfile/Compose only if proof shows AC7 need; no production `web/` edit unless returned for ruling (Q5; Step 6 F1).
6. **Verification**: Unit and integration coverage (AC8); applicable harness gates; post-build integrated acceptance on Compose and home cluster including AC5–AC6 fault cases observed after grace expiry where disconnect applies (Q6–Q7; Step 6 R1).

**Out of scope**:

- The separate .mkv content-type note.
- New NuGet/npm dependency or ffmpeg wrapper library; new project, top-level folder, or architectural layer; database schema; new route/query/header/status/DTO/protocol; secrets; `Features:LocalPlay` access; production `web/` edits without a return ruling; re-encoding video; silent pass-through of incompatible audio.
- Anything else is a follow-up issue, not a finding in this round. File a follow-up only for Critical, High, or broken behaviour under Q7; otherwise `noted, no ticket`.

**Precedence**: Ticket intent and AC1–8 (`recon-DEV-572` lines 27–39) are approved authority (`PRODUCT.md` §3; ticket has no dedicated Scope & Technical Design). Brief Q1–Q7, Step 6 decisions, and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions. Core port ADR is `docs/adr/0050-browser-stream-audio-conversion-port.md` (Proposed now; Accepted in the Build PR before delivery closes). Spec Kit PR remains `specs/**` only; ordinary owner Gate 1 has no structural checkbox. Only Keel declares PLAN FROZEN after the full step-3+ challenge path completes.

**Dependencies**: Existing stream route and browser caller (`recon-DEV-572` lines 13–15); existing `SystemProcessRunner` / `ThumbnailOptions.FfmpegPath` (`recon` line 17); ffmpeg already installed in `Dockerfile.api` line 18 (`recon` line 38). No new services or packages.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Converted stream**
Completed, seekable temporary MP4 produced server-side for browser playback when the selected audio track is AC3, E-AC3, or DTS: primary video stream-copied, selected audio transcoded to AAC stereo, served from API-owned scratch over the existing movie stream route.
_Avoid_: calling this local playback; HLS or other new protocols; rewriting library files

**Raw stream**
Existing direct byte-range serve of the primary library file when conversion is not required (supported codecs, no audio, or probe failure fallback).
_Avoid_: treating probe failure as a hard API error; silently serving unplayable unsupported audio after a known conversion failure

**Source version**
Identity of a primary media file for probe cache and in-flight/conversion sharing: absolute path plus size plus last-write time.
_Avoid_: persisting codec on the movie row; filename-token heuristics (`FolderNameCleaner`)

**API-owned scratch**
Bounded temporary storage outside the media library for partial and completed conversion artifacts, cleaned on failure/disconnect/orphan/idle/size/version rules.
_Avoid_: writing into the library tree (except the unrelated `Subs\` exception named by ticket intent)

Consumed vocabulary (already in use; not redefined): movie stream route, browser player, LocalPlay, SystemProcessRunner, ThumbnailOptions.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hear sound for unsupported browser audio (Priority: P1) 🎯 MVP

A viewer plays a movie whose first/default audio track is AC3, E-AC3, or DTS and hears audible sound in the existing browser player, with video quality preserved (no video re-encode).

**Why this priority**: This is the ticket’s primary user value (AC1, AC3).

**Independent Test**: On the integrated route (after proof and Build), play movie 959 and one real AC3 and one real DTS file; confirm audible AAC output and ffprobe evidence that video was stream-copied; unit/integration coverage of selected-track mapping, codec decision, and named AC3 fixture → copied-video / AAC stereo metadata.

**Acceptance Scenarios**:

1. **Given** a movie whose selected audio is AC3, E-AC3, or DTS, **When** the viewer plays it via the existing stream URL, **Then** audible sound plays in the browser player.
2. **Given** that converted response, **When** the output is inspected, **Then** video is stream-copied and audio is AAC stereo in a completed MP4 served as `video/mp4`.
3. **Given** a known conversion failure after unsupported audio was detected, **When** the stream request completes, **Then** the API uses existing error handling (no redirect, no silent raw fallback).

---

### User Story 2 - Keep supported audio on the raw path (Priority: P1)

A viewer plays a movie with AAC, MP3, Opus, Vorbis, another non-listed codec, or no audio and gets today’s raw streaming behaviour, including warm Range timing parity and at most 300 ms cold first-request probe overhead.

**Why this priority**: AC2 forbids regressing the common path; probe cost is capped by Q6 bar (f).

**Independent Test**: Raw-path unit decisions for supported/other/no-audio and probe-failure→raw; integration LocalPlay-off raw parity; proof/acceptance timing vs baseline.

**Acceptance Scenarios**:

1. **Given** selected audio is AAC, MP3, Opus, or Vorbis, **When** the movie is streamed, **Then** bytes come from the raw primary file with no conversion.
2. **Given** a successful probe with no audio, or any codec outside the conversion list, **When** the movie is streamed, **Then** the raw path is used.
3. **Given** a probe failure, **When** the movie is streamed, **Then** a warning is logged and the raw path is used (cancellation is not treated as failure).
4. **Given** the raw path under proof/acceptance, **When** cold first-request and warm Range timings are compared to baseline, **Then** warm Range shows no repeatable difference and cold probe adds at most 300 ms (else reopen Q3).

---

### User Story 3 - Seek and full duration on converted movies (Priority: P1)

A viewer seeking a converted movie sees the real full duration and resumes with sound within 3 s after click or arrow-key ±10 s seeks, including when Chromium aborts an old Range and issues a new one.

**Why this priority**: AC4 and Q4 abort semantics are the reason completed-file delivery was chosen (Q1–Q2).

**Independent Test**: Proof and post-build Chromium acceptance for duration, seek timing, and abort-plus-new-range continuity; integration coverage of 200/206/416 on completed converted output.

**Acceptance Scenarios**:

1. **Given** a converted movie in the player, **When** the timeline is shown, **Then** the full source duration is visible.
2. **Given** that movie is playing, **When** the viewer seeks ±10 s by click or arrow keys, **Then** audible playback resumes within 3 s.
3. **Given** Chromium aborts a Range while seeking and issues a new Range, **When** playback continues, **Then** the completed scratch asset remains available (not deleted on abort) and sound continues.

---

### User Story 4 - Preserve subtitles and controls (Priority: P2)

Subtitles, subtitle delay, and existing player controls behave the same for converted playback as for raw playback.

**Why this priority**: AC5; secondary to audible playback and seeking but required for closure.

**Independent Test**: Post-build integrated acceptance on Compose and home cluster comparing subtitle/control behaviour to raw.

**Acceptance Scenarios**:

1. **Given** a converted movie with subtitles, **When** the viewer toggles subtitles and adjusts subtitle delay, **Then** behaviour matches raw playback of the same title.
2. **Given** existing player controls, **When** used during converted playback, **Then** they behave as on the raw path.

---

### User Story 5 - Clean up processes and partial files (Priority: P2)

Failed conversions, client disconnects before completion, and cancelled seeks leave no leftover ffmpeg process or partial scratch files; completed assets expire under bounded policy without racing active readers.

**Why this priority**: AC6 and Q4 lifetime rules; required for safe Process.Start use.

**Independent Test**: Forced failure, mid-conversion disconnect (observed after the 1 s grace), and cancelled-seek acceptance cases on both deployments; unit coverage of deterministic reattach-within-grace and grace-expiry kill/delete; integration concurrent/disconnect coverage; atomic publish; eviction that skips active readers.

**Acceptance Scenarios**:

1. **Given** a conversion in flight, **When** conversion fails, **Then** the ffmpeg child is killed immediately and all partial output is deleted.
2. **Given** a conversion in flight that reaches zero waiters, **When** an authorized same-source-version waiter reattaches within the 1 s internal grace, **Then** the in-flight work is reused; **When** the grace expires still at zero waiters, **Then** the child is killed and all partial output is deleted.
3. **Given** a completed scratch asset, **When** a Range response aborts, **Then** the asset is not deleted solely because of that abort.
4. **Given** idle expiry, size pressure, source-version change, or startup, **When** cleanup runs, **Then** owned orphans and eligible completed assets are removed without deleting an asset a reader still holds.

---

### Edge Cases

- Probe failure vs successful probe with no audio (raw in both cases; only probe failure warns).
- Concurrent authorized requests for the same source version share one in-flight conversion; zero-waiter seek gaps use the 1 s reattachment grace before kill/cleanup.
- Denied, missing, or invalid movie ids return current responses with no process started.
- Source file replaced (size/last-write change) invalidates probe cache and completed scratch for that version; conversion start and pre-publish recheck the same tuple and discard a mismatched result.
- Scratch at size limit evicts least-recently-used completed assets that have no active readers.
- Missing ffprobe on Compose or cluster during proof returns for a provisioning ruling (Q3); does not assume install beyond current image evidence.
- Representative video that cannot be stream-copied into a playable MP4 returns to Q2 before implementation (no video re-encode; no silent incompatible audio).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST serve converted playback through the existing authenticated `GET /api/movies/{id}/stream` with status set 200/206/416 unchanged and no new query parameters, headers, status codes, DTOs, or streaming protocols (`brief.md` Q1; `recon-DEV-572` lines 13–15, 32–35).
- **FR-002**: Converted bytes MUST be a completed non-fragmented MP4 with Content-Type `video/mp4`, primary video stream-copied, and only the selected audio track as AAC stereo (`brief.md` Q2; AC3).
- **FR-003**: System MUST probe media with ffprobe JSON at play time, select the first default-marked audio track or else the first audio track in input order, and map that exact track (`brief.md` Q3).
- **FR-004**: System MUST convert only when that track’s codec is AC3, E-AC3, or DTS; AAC, MP3, Opus, Vorbis, other codecs, and successful probes with no audio MUST stay raw (`brief.md` Q3; AC1–AC2).
- **FR-005**: System MUST cache successful probe results in memory keyed by path, size, and last-write time; MUST NOT persist codec on the movie; MUST NOT use filename heuristics; MUST recheck that same tuple at conversion start and immediately before publication and, on mismatch, abort, invalidate cache and scratch entries for that key, and allow the next request to re-probe (`brief.md` Q3; PRODUCT §5 item 3; Step 6 R4).
- **FR-006**: On probe failure the system MUST log a structured warning (movie id; probe exit/stderr class; explicit probe-failed bypass marker) and serve raw; cancellation MUST propagate and MUST NOT be treated as probe failure; ffprobe JSON capture MUST be bounded to required fields and a bounded stdout size, with malformed, oversized, or missing-field shapes mapping to the same warn-and-raw fallback (a valid empty-audio result stays raw) (`brief.md` Q3; Step 6 R7–R8).
- **FR-007**: System MUST share one in-flight conversion per source version per API instance. On conversion failure the system MUST kill the child and delete partial files immediately. On a zero-waiter transition during in-flight work the system MUST start the 1 s internal same-source reattachment grace (`ASSUMPTIONS.md`); an authorized waiter for the same source version that attaches before expiry MUST reuse the in-flight work; if the grace expires still at zero waiters the system MUST kill the child and delete partial files. The grace is an internal constant (not an options key or wall-clock conversion timeout while a waiter remains attached) (`brief.md` Q4 / Step 6 R1; AC6; analyze timeout ruling).
- **FR-008**: System MUST stage and publish completed output by atomic rename inside the same resolved API-owned scratch directory outside the library (including when that directory is an absolute configured path); completed assets MUST survive Range abort and MUST be governed only by idle expiry, size-bound LRU (never while a reader holds), source-version change, and owned orphan startup cleanup on the API instance (`brief.md` Q4; Step 6 R5 / F4).
- **FR-009**: On known conversion failure the system MUST use existing API error handling with no redirect and no silent raw fallback, and MUST NOT advertise a new status code (`brief.md` Q4; Q1).
- **FR-010**: Before any child process, the system MUST authorize and resolve the registered primary file through the same library boundary as the raw path; only the movie id comes from the client; arguments MUST be structured with shell execution disabled; library remains read-only (`brief.md` Q5; `recon` lines 13, 30).
- **FR-011**: `StreamEndpoints` MUST stay limited to auth, resolution, and raw-versus-converted dispatch; probe/cache/conversion lifetime MUST live in one playback-specific service behind Core port `IStreamConversionService` plus options inside existing Core/Infrastructure layers (`Infrastructure/Streaming`), reusing `SystemProcessRunner` and `ThumbnailOptions.FfmpegPath` in place; mutable lifecycle state MUST be instance-scoped (not static); relative scratch paths MUST resolve under content root in Infrastructure (`brief.md` Q5; Step 6 F1 / F4 / F5 / F7 / F8).
- **FR-012**: System MUST NOT read or change `Features:LocalPlay`, MUST NOT add secrets, MUST NOT add NuGet/npm/ffmpeg-wrapper dependencies, and MUST NOT edit production `web/` files within this scope (`brief.md` Q1, Q5).
- **FR-013**: Before production changes, an isolated read-only feasibility proof MUST pass Q6 bars (a)–(f) on Compose for movie 959 plus one real AC3 and one real DTS file, including Q2 subtitle / subtitle-delay / existing-control parity on those samples, MUST record selected input codecs and proof measurements in `plan.md` (linked evidence may supplement), MUST demonstrate last-waiter cancellation of a deliberately long-running child with no leftover process or partials after grace expiry, and MUST record tool/path/scratch checks plus one cold-preparation timing on the home-cluster API image without inventing an additional cluster timing threshold (`brief.md` Q2, Q6; Step 6 R2 / R10; AC7). Missing ffprobe on Compose or cluster returns for a Q3 provisioning ruling. Failed cancel/kill proof returns to Patron before production conversion.
- **FR-014**: Cold preparation to audible start MUST be ≤ 120 s on the largest proof sample or reopen Q1; raw warm Range MUST show no repeatable baseline difference and cold probe ≤ 300 ms or reopen Q3 (`brief.md` Q6). No production wall-clock conversion timeout is derived from the 120 s proof bar (analyze ruling).
- **FR-015**: Post-build integrated acceptance on Compose and home cluster MUST enumerate and verify on the deployed route: (1) on the three named converted samples (movie 959, one real AC3, one real DTS) — audible AAC and video-copy output evidence; full duration; both seek modes (click and arrow-key ±10 s) within 3 s; old-range abort and new-range continuity; subtitle / subtitle-delay / existing-control parity; and the three fault/cleanup cases (forced failure, mid-conversion disconnect observed after the 1 s grace, cancelled seek) with no leftover ffmpeg process or partial files; (2) separately, supported-codec raw cold/warm baseline comparison (parity with the raw path baseline, not converted-sample evidence) (`brief.md` Task order item 3; Q6; Step 6 R1). Pre-build cluster evidence remains tool/scratch plus one cold timing only; full cluster playback bars stay post-build.
- **FR-016**: Automated tests MUST cover needs-transcode decisions per codec/track/cache including actual selected-track mapping, bounded/malformed probe shapes, probe-failure versus cancellation distinction, and mid-flight source-version mismatch discard; unit lifetime coverage MUST assert shared waiters, deterministic reattach-within-grace, and grace-expiry kill/delete of child and partials; an integration test MUST stream a small named AC3 fixture and assert copied-video / AAC stereo evidence via metadata, with missing tool/fixture prerequisites producing an explicit blocker (not a silent green); and automated coverage MUST include raw response/Range parity, known conversion failure on the existing API error path with no raw fallback or redirect, denied/missing/invalid current responses with no child invocation, LocalPlay-off raw parity, absolute scratch-path stage/publish, and explicit concurrency / disconnect / cleanup at the appropriate test layer (`brief.md` Q6; AC8; Step 6 R4–R8 / F6; `recon-DEV-572` lines 13–15, 39).
- **FR-017**: Closing review MUST use two rounds max, all applicable harness gates green (including `LamuFlix.ArchitectureTests`), AC1–8 evidence including Q6 deployment bars, Core port ADR `docs/adr/0050-browser-stream-audio-conversion-port.md` Accepted in the Build PR that also delivers `IStreamConversionService`, the Principle I amendment (MINOR 2.0.0 → 2.1.0 with Sync Impact Report and Last Amended), and the matching `ArchitectureTests` `PortWhitelistPattern` entry atomically, all three Size L axes (Sentry Risk, Ledger Standards, Compass Spec), explicit dispositions, and block on unresolved Critical/High or any unresolved finding that fails a frozen AC, required gate, or Q5 trust boundary (`brief.md` Q7 / Step 6 F1; constitution PR Quality Gates / Documentation Rules). Spec Kit PR stays specs-only with ordinary owner Gate 1 and no structural checkbox.

### Key Entities

- **Source version**: Path + size + last-write time identifying probe cache entries and conversion identity.
- **Probe result**: Cached successful ffprobe interpretation (selected track index/codec; conversion needed or not).
- **In-flight conversion**: Shared work item producing partial output until publish or abort.
- **Completed scratch asset**: Atomically published MP4 available for byte-range reads until eviction/expiry/version change.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On movie 959 and one real AC3 and one real DTS library file, the browser player produces audible sound for previously unsupported default/first audio (AC1).
- **SC-002**: Converted output shows stream-copied video and AAC stereo audio in a completed MP4 (`video/mp4`) with full source duration visible in the player (AC3–AC4).
- **SC-003**: Click and arrow-key ±10 s seeks on converted movies return to audible playback within 3 s; aborting a Range and issuing a new one continues playback (AC4; Q4).
- **SC-004**: Supported-codec movies keep raw streaming; warm Range timing shows no repeatable difference from baseline; cold first-request probe adds ≤ 300 ms (AC2; Q6 bar f).
- **SC-005**: Cold preparation to audible start is ≤ 120 s on the largest of the three proof samples, or Q1 is reopened with recorded numbers (Q6 bar e).
- **SC-006**: Subtitles, subtitle delay, and existing controls match raw behaviour for converted playback (AC5).
- **SC-007**: After forced failure, mid-conversion disconnect observed after the 1 s grace, and cancelled seek, no leftover ffmpeg process or partial scratch files remain on Compose or home cluster (AC6; Step 6 R1).
- **SC-008**: Unit and integration suites cover FR-016; applicable harness gates are green or honestly labelled; Size L three-axis review closes under FR-017 / Q7; Build delivers ADR 0050 Accepted with the Principle I / whitelist atomic amendment.

## Assumptions

- Output container is completed non-fragmented MP4 (`video/mp4`) per Q2, subject to proof stop condition (`ASSUMPTIONS.md`).
- Scratch size, concurrency, idle-expiry, and default scratch directory numeric/path defaults are the four taste-level values proposed in `plan.md` and tagged `[assumed]` in `ASSUMPTIONS.md` (Q4). No conversion-child process timeout is assumed.
- In-flight zero-waiter reattachment grace is 1 s, an internal constant tagged `[assumed]` in `ASSUMPTIONS.md` (Step 6 R1).
- ffprobe is available beside the configured ffmpeg path from the existing API image install unless proof returns for provisioning (Q3; `recon` line 38).
- Existing browser caller and player controls need no production `web/` change for AC4–AC5 within this scope (Q5); return for ruling if evidence forces otherwise.
- Standing chain ruling on closing bar/round cap governs if one already exists; Q7 restates it (Q7).
