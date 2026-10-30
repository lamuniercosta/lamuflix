# Thumbnail generation port and bounded process adapter

Status: Proposed.

Ticket: DEV-17. Authority: [brief.md](../../specs/DEV-17/brief.md), Q2–Q10
(Q3 supersedes Q1); full rulings in [CONCLUSIONS.md](../../specs/DEV-17/CONCLUSIONS.md).
Requirements: DEV-17 Scope & Technical Design 1–5 and Acceptance Criteria 1–4.

## Context

DEV-17 requires ffmpeg/ffprobe in the Api runtime image, a Core thumbnail port,
Infrastructure extraction at 10% of duration, a testable process runner, default-disabled
options, and failed results for operational errors. Core cannot reference process APIs;
the playback starter is fire-and-forget and cannot capture probe output or completion
(`src/LamuFlix.Infrastructure/Playback/SystemProcessStarter.cs`). Constitution Principles I and VII require the named-port and
options lists to be extended through the existing amendment procedure.

DEV-17 Scope 2 mandates the Core port. The MINOR enumeration extension follows the
constitution amendment procedure and preserves the existing process boundary.

## Decision

1. Add `Core/Ports/IThumbnailGenerator` exposing
   `Task<ThumbnailResult> GenerateAsync(string sourcePath, CancellationToken cancellationToken = default)`.
   Its process-free `sealed record ThumbnailResult(bool Success, string? CachePath, string? FailureReason)`
   lives beside the port. Success requires a nonempty cache path and null failure reason;
   failure requires null cache path and a bounded stable reason token, never raw diagnostics.
   Closed failure tokens, applied in execution order, are `Disabled`, `InvalidSourcePath`,
   `BinaryNotFound`, `ProcessTimedOut`, `ProbeFailed`, `NoVideoStream`, `InvalidDuration`,
   `ExtractionFailed`, `InvalidOutput`, and `StorageError`. Disabled short-circuits before
   validation; launch/timeout failures map before phase-specific process errors; video
   presence is checked before duration. Cache/read I/O or access errors map to StorageError.
   The detailed mapping is in [the contract](../../specs/DEV-17/contracts/thumbnail-generator.md).
2. Keep `FfmpegThumbnailGenerator`, `IProcessRunner`, `SystemProcessRunner` and process
   results in `Infrastructure/Thumbnails`. Define non-port `IProcessRunner` alongside
   its primary implementation in `SystemProcessRunner.cs`; keep `ProcessResult` separate.
   The runner exposes
   `Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default)`;
   `ProcessResult` is a sealed record with `bool Success`, `int? ExitCode`,
   `string StandardOutput`, `string StandardError`, and `string? FailureReason`.
   Use `ArgumentList`, `UseShellExecute=false`, simultaneous asynchronous stream draining,
   at most 64 KiB retained per stream, and failure on truncation. Missing executable,
   launch failure, nonzero exit and timeout return failures. Timeout and caller cancellation
   terminate the process tree; caller cancellation propagates `OperationCanceledException`
   after cleanup. Dispose process resources. Preserve playback's existing starter.
   Inject `TimeProvider`, resolved from DI with `TimeProvider.System` fallback, and use
   `CancellationTokenSource(timeout, timeProvider)` for per-process timeout cancellation.
   Process failure tokens are `LaunchFailed`, `TimedOut`, `OutputTruncated`, `NonZeroExit`;
   truncation takes precedence over exit code. Launch/timeout failures have null ExitCode.
3. Probe explicit video presence and finite positive duration using machine-readable
   ffprobe output. Seek to `duration * 0.10` without integer truncation, formatted invariantly.
   Extract one JPEG from the first video stream with discrete `-nostdin`, `-y`,
   `-map 0:v:0` and `-frames:v 1` arguments, preserving aspect ratio, capping width at
   MaxWidth and avoiding upscaling. Own a unique `<guid:N>.jpg` per attempt beneath the
   configured cache directory. Succeed only for an existing nonempty file beginning with
   JPEG SOI bytes `0xFF, 0xD8` (at least two bytes); remove partial/invalid output best-effort on failure or
   cancellation without masking caller cancellation. Operational probe, process and
   filesystem failures return failed thumbnail results; do not precheck source existence
   before the runner. Missing/unreadable sources surface through ffprobe as `ProbeFailed`.
4. Add Core `ThumbnailOptions` and conditional startup validation through ServiceDefaults.
   Section `Thumbnails` defaults to Enabled=false, FfmpegPath=`ffmpeg`,
   CacheDirectory=`cache/thumbnails`, MaxWidth=320 and ProcessTimeoutSeconds=30.
   When enabled, validate nonblank paths and positive width/timeout; startup performs no
   executable probe or directory creation. Derive ffprobe beside a directory-qualified
   ffmpeg path, retaining its extension, otherwise resolve bare ffprobe on PATH. Resolve
   relative cache paths against host content root, with AppContext.BaseDirectory fallback.
   `AddLamuFlixThumbnails` composes stateless singleton runner/generator in the Api host.
   FfmpegPath and CacheDirectory are operator-trusted configuration, never populated
   from untrusted request/user input. ProcessTimeoutSeconds bounds each runner call;
   generation can take up to twice that value plus process termination and file cleanup.
5. Server-side extraction is independent of `Features:LocalPlay`; existing playback
   gating remains intact. Disabled generation returns `Disabled` before process or disk
   work. Amend constitution 1.7.0 → 1.8.0 by adding only `IThumbnailGenerator` to Principle I
   and `ThumbnailOptions` to Principle VII, with version/date/sync report updates. Extend
   the architecture port whitelist during Phase B, preserving all existing entries.
   Reconcile/add the Thumbnail term (term, definition, avoid) in `CONTEXT.md` in the
   introducing PR. Principle VI health-check and telemetry registration is not applicable
   to this local in-image CLI boundary, analogous to the media-player process rather than
   a networked/backing service. Missing tools remain generation-time failed results;
   no health-check or TelemetryConstants additions are required.

## Consequences

- Core consumers gain a process-free boundary; no package, project, layer, schema or public
  API is added. Endpoint DEV-428 and tile DEV-429 remain separate. There is no persistent
  cache lookup, cross-call reuse, caller-selected destination or no-video thumbnail support.
- Fake-runner/options/composition tests, deterministic runner lifecycle/output tests and a
  committed synthetic real-video JPEG test are required by DEV-17 AC 2–4. Missing ffmpeg/ffprobe
  fails the required real-tool test. Docker proof must run both tools as `app` and capture
  image-size increase for Rigger's verified YouTrack recording. Image size and subprocess
  lifecycle handling are the accepted operational costs. Real-process runner tests live
  in `tests/LamuFlix.IntegrationTests/Thumbnails/`, invoking `pwsh -NoProfile -NonInteractive
  -File` with committed scripts in `Fixtures/Thumbnails/`, following the existing
  `tests/LamuFlix.Evaluation/TestSupport/EvaluationCommand.cs` pattern. Narrow existing
  IntegrationTests csproj entries copy fixtures. Missing pwsh fails; scripts never re-enter
  the test executable. Fake-runner unit tests retain tiny byte stubs only for output checks.
  Hosted ffmpeg/ffprobe availability must be proved before Phase B coding and rechecked
  before PR; unavailable tools block for bounded recon and Patron ruling, with no test
  skip, filter change, or CI workflow edit.
- DEV-428 owns cache retention/eviction, consumer concurrency bounds and library-root
  source-path authorization. These are future consumer design constraints; DEV-17 adds
  no consumer, endpoint, retention policy or concurrency layer.
- This amendment preserves all existing ports, options and process restrictions. No existing
  implementation is migrated in Phase A; Phase B adds the port/options and whitelist before
  adapter composition, with dependency/pickup drift checks and changed-Core mutation.
