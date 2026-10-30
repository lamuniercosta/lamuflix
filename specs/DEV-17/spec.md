# Feature Specification: DEV-17 — Thumbnail generation at 10 percent offset

**Feature Branch**: `feature/017-spec`

**Created**: 2026-10-30

**Status**: gate1: provisional

**Input**: Ticket Scope & Technical Design 1-5 and Acceptance Criteria (recon-DEV-17:80–117); grill brief `brief.md` closed at Q10 + Phase 2 plan challenge round 1; rulings Q2–Q10 and Phase 2 plan challenge round 1 in `CONCLUSIONS.md` (Q3 supersedes Q1); Patron-approved defaults in `ASSUMPTIONS.md`; `specs/PRODUCT.md`; ADR `docs/adr/0033-thumbnail-generator-port.md`; constitution 1.8.0 enumeration amendment.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — API image ships ffmpeg and ffprobe (Priority: P1)

As the Api host operator, I want the runtime stage of `docker/Dockerfile.api` to install ffmpeg (including ffprobe) before switching to the `app` user so containerized thumbnail extraction has the required tools.

**Why this priority**: Ticket Scope 1 and AC1; without image tooling, container delivery cannot meet acceptance.

**Independent Test**: Build `docker/Dockerfile.api` and run `ffmpeg -version` and `ffprobe -version` as the image `app` user; both succeed. Capture the exact version command outputs and the image size increase against a comparable baseline runtime image for Rigger to write on DEV-17.

**Acceptance Scenarios**:

1. **Given** the runtime stage of `docker/Dockerfile.api`, **When** the image is built, **Then** a single `RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*` layer installs ffmpeg (shipping ffprobe) as root before `USER app`.
2. **Given** the built image, **When** `ffmpeg -version` and `ffprobe -version` run as `app`, **Then** both commands succeed and their exact outputs are captured for the size/version receipt.
3. **Given** a comparable baseline runtime image, **When** the new image size is measured, **Then** the size increase (with exact tool version outputs) is captured and routed to Rigger for verified YouTrack recording on DEV-17 (no CI workflow edits).

---

### User Story 2 — Generate a thumbnail JPEG at 10 percent (Priority: P1)

As a future Core consumer (DEV-428 out of scope here), I want a process-free `IThumbnailGenerator` port with an Infrastructure ffmpeg adapter that probes duration, extracts one JPEG at 10 percent of duration with width capped, and returns a structured success or failure result without throwing on operational errors.

**Why this priority**: Ticket Scope 2–5; this is the feature deliverable that later tickets consume.

**Independent Test**: With `ThumbnailOptions.Enabled=true` and a fake or real runner, call `GenerateAsync` on a video path; success returns a nonempty cache path to a valid JPEG under the configured cache directory; operational failures return `Success=false` with a closed FailureReason token and null cache path.

**Acceptance Scenarios**:

1. **Given** `ThumbnailOptions.Enabled=true` and a source with a video stream and finite positive duration, **When** generation runs, **Then** ffprobe verifies video presence and duration, seek is `duration * 0.10` without integer truncation formatted invariantly, and ffmpeg writes one JPEG from the first video stream (`-map 0:v:0`, `-frames:v 1`) noninteractively (`-nostdin`, `-y`) with aspect-preserving width capped at MaxWidth and no upscaling.
2. **Given** a successful ffmpeg exit, **When** the generator validates output, **Then** success requires an existing file with `Length >= 2` beginning with JPEG SOI bytes `0xFF, 0xD8` at a generator-owned collision-resistant `<guid:N>.jpg` path beneath CacheDirectory (created when needed); incomplete or invalid output is deleted best-effort on failure or cancellation without masking caller cancellation.
3. **Given** `ThumbnailOptions.Enabled=false` (default), **When** `GenerateAsync` is called, **Then** it returns failure reason `Disabled` without starting a process or touching disk (short-circuits before validation/process/disk).
4. **Given** blank/whitespace `sourcePath`, missing/unreadable source (no `File.Exists` precheck — surfaces as `ProbeFailed` when ffprobe fails), missing binaries (`BinaryNotFound`), nonzero/truncated probe (`ProbeFailed`), no video stream (`NoVideoStream`), invalid duration (`InvalidDuration`), extraction errors (`ExtractionFailed`), timeout (`ProcessTimedOut`), invalid/missing/`Length < 2`/reversed-SOI output (`InvalidOutput`), or filesystem I/O / `UnauthorizedAccessException` (`StorageError`), **When** generation runs, **Then** a failed `ThumbnailResult` is returned without throwing and without raw paths/diagnostics; caller-requested cancellation cleans up and propagates `OperationCanceledException`.
5. **Given** Api host composition, **When** `AddLamuFlixThumbnails` registers, **Then** runner and generator are stateless singletons consuming options, content-root resolution, and `TimeProvider` from DI (or `TimeProvider.System` fallback), registered independently of `Features:LocalPlay`, with no eager subprocess or directory creation at startup; existing LocalPlay playback gating remains unchanged.
6. **Given** `ProcessTimeoutSeconds` (default 30), **When** generation invokes ffprobe then ffmpeg, **Then** the timeout bounds each `IProcessRunner.RunAsync` call individually, so worst-case wall-clock is up to `2 * ProcessTimeoutSeconds` plus process-tree termination and file-cleanup overhead.
7. **Given** `ThumbnailOptions.FfmpegPath` and `CacheDirectory`, **When** configuration is supplied, **Then** both are operator-controlled trusted settings never populated from untrusted user/API input.

---

### User Story 3 — Fake-runner proofs for seek and failed results (Priority: P1)

As a maintainer, I want unit tests that use a fake `IProcessRunner` (never real ffmpeg) so the 10 percent seek argument, discrete arguments, disabled no-work, and missing-binary failed-result behaviors are proven without host tools.

**Why this priority**: Ticket AC2 and AC4; these are required acceptance proofs and must not silently pass without tools.

**Independent Test**: Run UnitTests for Thumbnails/Options under a non-English / comma-decimal culture where applicable; assert seek argument is 10 percent of duration; assert missing binary returns failed result without throw; no unit test requires ffmpeg or a real media file. Deterministic runner lifecycle proofs live separately in IntegrationTests via `pwsh` fixtures.

**Acceptance Scenarios**:

1. **Given** a fake runner returning duration `125.5`, **When** the generator builds ffmpeg arguments under a comma-decimal culture (`fr-FR` or `de-DE`), **Then** the seek (`-ss`) argument equals `"12.55"` without integer truncation or culture-decimal bugs.
2. **Given** discrete ArgumentList construction with default MaxWidth `320`, **When** paths contain spaces, **Then** arguments remain separate entries (no shell concatenation) including exact flags `-nostdin`, `-y`, `-ss`, `-i`, `-map`, `0:v:0`, `-frames:v`, `1`, `-vf`, and test-example scale `scale=min(320\,iw):-2` (production substitutes configured MaxWidth into `scale=min(<MaxWidth>\,iw):-2`; `320` is the default/test example only, not a fixed runtime width).
3. **Given** a missing binary / launch failure from the runner, **When** generation runs, **Then** a failed `ThumbnailResult` with `BinaryNotFound` is returned without throwing.
4. **Given** invalid duration/video probe results, nonzero exit, timeout, empty/non-JPEG output (0-byte, 1-byte `0xFF`, reversed SOI `0xD8, 0xFF` fail; `>= 2`-byte `0xFF, 0xD8` succeeds), or Enabled=false, **When** generation runs, **Then** the corresponding closed FailureReason and partial-output cleanup are asserted; tiny temporary output bytes are used only when output validation needs disk.

---

### User Story 4 — Real-tool fixture JPEG proof (Priority: P2)

As a maintainer, I want one committed short synthetic video fixture and a marked integration test so real ffmpeg/ffprobe generation is proven end-to-end.

**Why this priority**: Ticket AC3; complements fake-runner coverage but depends on host/image tools being available for the required run. Dockerfile tooling (`T040`) must precede real-fixture execution.

**Independent Test**: `[Trait("Category", "RequiresFfmpeg")]` test uses `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/short-video.mp4`, enabled options, and a temporary cache; asserts success, valid JPEG (Length ≥ 2 + SOI), and width ≤ MaxWidth. Missing ffmpeg/ffprobe fails the test (never skipped as acceptance passed). Hosted CI tool availability is unproved and must be verified in Phase B pre-coding (`T010`) and pre-PR (`T061`) without skip/filter/workflow edits.

**Acceptance Scenarios**:

1. **Given** ffmpeg and ffprobe available and the committed short fixture, **When** the RequiresFfmpeg integration test runs, **Then** generation succeeds, writes a JPEG, and respects MaxWidth.
2. **Given** ffmpeg or ffprobe missing, **When** that required test runs, **Then** it fails rather than skip or count as acceptance passed.

---

### Edge Cases

- Blank or whitespace-only `sourcePath` yields `InvalidSourcePath`; a non-whitespace `sourcePath` pointing to a missing or unreadable file is not pre-checked via `File.Exists` and surfaces as `ProbeFailed` when ffprobe fails (so fake-runner tests need no media file).
- Files without a video stream remain out of scope for thumbnail support; absent video returns `NoVideoStream` without invoking ffmpeg; invalid duration after a video stream is present returns `InvalidDuration`.
- Output truncation above the 64 KiB per-stream retained cap fails the process run with `OutputTruncated` (no partial ffprobe JSON parse); one-stream exact-cap `== 65536` succeeds and `65537` truncates while draining to EOF; simultaneous stdout+stderr both over the cap must complete drain with no deadlock and still return `OutputTruncated` (original Q7 proof retained).
- Timeout kills the process tree and maps to `ProcessTimedOut` / process `TimedOut`; caller cancellation kills the tree and propagates `OperationCanceledException` after cleanup.
- Relative CacheDirectory resolves against `IHostEnvironment.ContentRootPath` (fallback `AppContext.BaseDirectory`); rooted paths remain as supplied. Hosting Abstractions already referenced at Infrastructure csproj:33.
- ffprobe is derived as sibling of a directory-qualified FfmpegPath (retaining extension) or bare `ffprobe` on PATH.
- Startup validation when Enabled=true rejects whitespace paths and nonpositive MaxWidth/ProcessTimeoutSeconds; it does not probe binary existence or create directories.
- Consumer endpoint DEV-428 (incl. retention/eviction, concurrency, library-root authorization), tile DEV-429, DB writes, public API/DTO changes, Playback edits, new packages/projects, and CI/compose changes stay out of scope.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The runtime stage of `docker/Dockerfile.api` MUST install ffmpeg (shipping ffprobe) via a single `RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*` layer as root before `USER app`.
- **FR-002**: Built-image acceptance MUST prove `ffmpeg -version` and `ffprobe -version` succeed as `app`, MUST capture the exact version outputs alongside image-size increase for Rigger's verified YouTrack recording on DEV-17; no `.github/workflows/*` edits.
- **FR-003**: Core MUST expose `IThumbnailGenerator` in `LamuFlix.Core/Ports` with `Task<ThumbnailResult> GenerateAsync(string sourcePath, CancellationToken cancellationToken = default)` and process-free `sealed record ThumbnailResult(bool Success, string? CachePath, string? FailureReason)` beside the port (success: nonempty CachePath + null FailureReason; failure: null CachePath + closed FailureReason token from the catalogue below; never raw exceptions/stdout/stderr/paths). No enum or factory layer.
- **FR-004**: Infrastructure MUST implement `FfmpegThumbnailGenerator`, co-located `IProcessRunner`/`SystemProcessRunner` in `SystemProcessRunner.cs` (no standalone `IProcessRunner.cs`), `ProcessResult`, and `AddLamuFlixThumbnails` under `LamuFlix.Infrastructure/Thumbnails` with no process API types in Core; playback `IProcessStarter` MUST remain untouched (brief Phase 2 correction 1; constitution:560–561).
- **FR-005**: `IProcessRunner` MUST expose `Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default)` and MUST be defined in the same file as `SystemProcessRunner`; `ProcessResult` MUST be `sealed record ProcessResult(bool Success, int? ExitCode, string StandardOutput, string StandardError, string? FailureReason)` in its own file, succeeding only for exit 0 without timeout/truncation; retain ≤64 KiB per stream, drain both to EOF, fail on truncation; use `ProcessStartInfo.ArgumentList` with `UseShellExecute=false`; kill process tree on timeout or caller cancellation; propagate caller cancellation as `OperationCanceledException` after cleanup. `SystemProcessRunner` MUST accept injected `TimeProvider` (defaulting to `TimeProvider.System` when not supplied) and drive timeout via `new CancellationTokenSource(timeout, timeProvider)` without `DateTime.Now` or new packages; `AddLamuFlixThumbnails` resolves `TimeProvider` from DI with `TimeProvider.System` fallback. Closed `ProcessResult.FailureReason` tokens (null on success), in precedence: `LaunchFailed` (missing binary / Win32Exception / start failure, `ExitCode = null`); `TimedOut` (tree-kill, `ExitCode = null`); `OutputTruncated` (stdout or stderr exceeded 64 KiB; evaluated before exit code); `NonZeroExit` (`ExitCode != 0` without timeout/truncation).
- **FR-006**: Generation MUST probe machine-readable ffprobe output for explicit video stream and finite duration > 0 (InvariantCulture); compute seek `duration * 0.10` without integer truncation; invoke ffmpeg with discrete `-nostdin`, `-y`, `-map 0:v:0`, `-frames:v 1` and aspect-preserving MaxWidth-capped non-upscaling scale; own per-attempt `<guid:N>.jpg` under CacheDirectory; succeed only after `Length >= 2` JPEG SOI `0xFF, 0xD8` validation; best-effort delete partial/invalid output on failure/cancellation without masking cancellation. Closed `ThumbnailResult.FailureReason` tokens (null on success; `CachePath = null` on failure), in strict precedence: (1) `Disabled` (`Enabled == false`, before validation/process/disk); (2) `InvalidSourcePath` (null/whitespace `sourcePath`); (3) `BinaryNotFound` (process `LaunchFailed` on probe or extract); (4) `ProcessTimedOut` (process `TimedOut` on probe or extract); (5) `ProbeFailed` (ffprobe `NonZeroExit` or `OutputTruncated`, including missing/unreadable source without `File.Exists` precheck); (6) `NoVideoStream`; (7) `InvalidDuration` (missing/unparseable/non-finite/`<= 0`); (8) `ExtractionFailed` (ffmpeg `NonZeroExit` or `OutputTruncated`); (9) `InvalidOutput` (missing, `Length < 2`, or not beginning with SOI / reversed SOI); (10) `StorageError` (`IOException` / `UnauthorizedAccessException` creating cache dir or reading/validating output). `ProcessTimeoutSeconds` bounds each runner call; worst-case wall-clock ≤ `2 * ProcessTimeoutSeconds` plus termination/cleanup overhead. `FfmpegPath` and `CacheDirectory` are trusted operator configuration, never user/API input.
- **FR-007**: `ThumbnailOptions` MUST live in `Core/Options` with SectionName `Thumbnails`, Enabled default `false`, FfmpegPath `ffmpeg`, CacheDirectory `cache/thumbnails`, MaxWidth `320`, ProcessTimeoutSeconds `30`; conditional `ThumbnailOptionsValidator` and AddLamuFlixOptions binding/startup validation when Enabled=true (nonblank paths, positive width/timeout) without binary probing or directory creation at startup.
- **FR-008**: Composition MUST register independently of `Features:LocalPlay` while honoring Enabled=false; resolve relative cache against content root (existing Hosting.Abstractions reference at Infrastructure csproj:33); derive ffprobe per ASSUMPTIONS Q5; wire ServiceDefaults bind/validator, Api `AddLamuFlixThumbnails` (TimeProvider DI/`System` fallback), architecture `PortWhitelistPattern` for `IThumbnailGenerator`, and focused OptionsStartupValidationTests coverage.
- **FR-009**: Unit tests MUST cover fake-runner fractional seek (`125.5` → `"12.55"` under comma culture), exact discrete flags/spaced paths and test-example scale under default MaxWidth `320` (`scale=min(320\,iw):-2`; production substitutes configured MaxWidth — `320` is default/test example only), SOI 0/1/reversed fail and `>= 2` correct-prefix success, probe/process failure modes, disabled no-work, missing binary without throw, invalid JPEG/cleanup, options unit/property/survivor + startup validation, and composition independence from LocalPlay. Deterministic `SystemProcessRunner` lifecycle proofs MUST live in `tests/LamuFlix.IntegrationTests/Thumbnails/` using EvaluationCommand-style `pwsh -NoProfile -NonInteractive -File` fixture scripts under `Fixtures/Thumbnails/` copied by narrow existing IntegrationTests csproj entries and MUST cover: ordinary success/capture; missing binary; nonzero exit; one-stream exact-cap (`== 65536` succeeds; `65537` → `OutputTruncated` while draining); simultaneous stdout+stderr both over the 64 KiB cap with complete drain, no deadlock, and `OutputTruncated` (original Q7 proof retained); timeout tree cleanup; caller cancellation; controlled `TimeProvider` timeout. Missing `pwsh` fails; no xunit executable reentry/custom Main/new project/package. No mandatory new FsCheck generator suite for `FfmpegThumbnailGenerator`. Infrastructure mutation N/A; Core mutation unchanged.
- **FR-010**: Integration MUST commit `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/short-video.mp4` and a `[Trait("Category", "RequiresFfmpeg")]` real-tool test that fails if tools are missing; Dockerfile tooling precedes real-fixture execution.
- **FR-011**: Governance MUST keep focused ADR 0033 and constitution MINOR 1.8.0 (`IThumbnailGenerator` on Principle I; `ThumbnailOptions` on Principle VII) present in the Phase A working tree and aligned with the whitelist (authorized Rigger inclusion in Phase A commit; `harness.yml` untouched/unstaged; no duplicate amendment task or owner checkbox); Phase B MUST reconcile/add `Thumbnail` in `CONTEXT.md` with `term`, `definition`, and `avoid` fields in the introducing PR and reconcile the Principle VIII summary list only if needed (brief Phase 2 correction 2; constitution VIII:393,408); Principle VI health-check and telemetry registration is N/A for this local in-image CLI capability — no health-check or TelemetryConstants files, no constitution departure or owner checkbox (brief Phase 2 correction 3); preserve the pre-existing `harness.yml` local modification (not part of this work). Phase B `T010`/`T061` MUST prove hosted runner `ffmpeg`/`ffprobe` availability without skip/filter/workflow edits and stop for bounded recon + Patron if unavailable; no claim that hosted tools are already proven.
- **FR-012**: Out of scope MUST remain: DEV-428 consumer/endpoint (including cache retention/eviction, queue/endpoint concurrency throttling, and library-root source-path authorization — design constraints only, no DEV-17 implementation), DEV-429 tile, no-video thumbnail support, DB schema, public API/DTO/status changes, Playback files, Directory.Packages.props, docker-compose.yml, CI workflows, generated files, health-check/TelemetryConstants additions, unrelated refactors, new packages/projects. Pre-existing ADR 0031 duplicate and constitution vs ArchitectureTests port-list mismatch are already `noted, no ticket` — no unrelated corrections in DEV-17.

### Key Entities

- **ThumbnailResult**: Process-free Core result carrying Success, optional CachePath, and optional closed FailureReason token.
- **ThumbnailOptions**: Core configuration for enablement, ffmpeg path, cache directory, MaxWidth, and process timeout (trusted operator inputs).
- **ProcessResult**: Infrastructure runner result with Success, optional ExitCode, bounded stdout/stderr, and optional closed FailureReason token.
- **Cache JPEG**: Generator-owned per-attempt file under CacheDirectory validated as Length ≥ 2 JPEG SOI before success.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `docker build` of `docker/Dockerfile.api` yields an image where `ffmpeg -version` and `ffprobe -version` succeed as `app`, exact version outputs are captured, and the measured size increase is recorded on DEV-17 via Rigger (ticket AC1 + Scope 1).
- **SC-002**: A fake-runner unit test asserts the seek argument for duration `125.5` is `"12.55"` under a comma-decimal culture (ticket AC2).
- **SC-003**: A RequiresFfmpeg fixture test writes a valid JPEG from the committed short video and fails when tools are missing (ticket AC3).
- **SC-004**: A missing-binary path returns a failed result without throwing, covered by test (ticket AC4).
- **SC-005**: Phase B delivery earns required harness gates against the same HEAD (Roslyn, cyclomatic complexity via `-Help`-documented invocation against configured `gates.complexity.refactor`, InspectCode, property tests, vulnerable packages, full `dotnet test` including ArchitectureTests and RequiresFfmpeg, format/diff, and mutation of eligible changed Core code) without lowering thresholds; Infrastructure/Api mutation exclusions remain NOT APPLICABLE for those projects only; exit-2 SKIPPED/SKIP/N/A and Could not run are never recorded as PASS; hosted-tool availability proved in T010/T061 without skip/filter/workflow edits; complete Sentry/Ledger/Compass reports; closing bar per brief Q10 (zero unresolved Critical/High/Medium; every Low dispositioned; two-round cap).

## Assumptions

- Patron-approved defaults and conventions from `ASSUMPTIONS.md` (Q5–Q7 + Phase 2 plan challenge round 1): SectionName `Thumbnails`; FfmpegPath `ffmpeg`; CacheDirectory `cache/thumbnails`; MaxWidth `320`; ProcessTimeoutSeconds `30`; 64 KiB stream cap; ffmpeg flag/scale construction; fixture path and RequiresFfmpeg trait; ffprobe sibling/PATH derivation; content-root cache resolution; closed failure-token catalogue; `pwsh` fixture test technique. Enabled=false is ticket text, not an assumption.
- Existing xUnit v3, NSubstitute, Shouldly, FsCheck stack and Core port / Infrastructure adapter / Core options architecture are reused; no new dependency or project (brief Q5/Q8; PRODUCT.md §5). Hosting.Abstractions already present at Infrastructure csproj:33.
- ADR number 0033 is reserved and drafted; constitution 1.8.0 amendment is present in the Phase A working tree. Gate 1, plan freeze, and implementation approval remain separate from this draft. Spec artifacts are valid strict UTF-8 (recon-DEV-17-2:35-40); no encoding rewrite.
- Depends on DEV-304 and DEV-314 (existing). Phase B verifies landing/baseline alignment and pickup drift before coding (brief Q9; recon:57 documents specs, not proof of landing).
- Gherkin/Reqnroll acceptance stage remains opt-out for this feature unless later requested.
