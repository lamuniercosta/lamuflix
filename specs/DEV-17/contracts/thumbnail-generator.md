# Contract: Thumbnail generator and process runner (DEV-17)

Authority: ticket Scope 2–5 (recon-DEV-17:84–87,106–109); brief Q2–Q6 + Phase 2 plan challenge round 1; ADR 0033. No public HTTP routes in this ticket.

## IThumbnailGenerator (Core/Ports)

```csharp
Task<ThumbnailResult> GenerateAsync(
    string sourcePath,
    CancellationToken cancellationToken = default);
```

```csharp
sealed record ThumbnailResult(bool Success, string? CachePath, string? FailureReason);
```

Invariants:

- Success ⇒ nonempty CachePath ∧ FailureReason is null.
- Failure ⇒ CachePath is null ∧ FailureReason is one of the closed tokens below.
- Never place raw exceptions, paths, stdout, or stderr into FailureReason.
- No enum or factory layer — preserve the record signature.
- Enabled=false ⇒ FailureReason `Disabled` before validation/process/disk.
- Caller cancellation ⇒ cleanup then `OperationCanceledException` (not a failed ThumbnailResult).
- Non-whitespace missing/unreadable `sourcePath` is not pre-checked with `File.Exists`; surfaces as `ProbeFailed` when ffprobe fails.
- `ProcessTimeoutSeconds` bounds each runner invocation; worst-case wall-clock ≤ `2 * ProcessTimeoutSeconds` plus termination/cleanup overhead.
- `FfmpegPath` and `CacheDirectory` are trusted operator configuration, never user/API input.

### ThumbnailResult.FailureReason (closed set; null on Success)

Strict precedence:

1. `Disabled` — `Enabled == false` (before validation/process/disk).
2. `InvalidSourcePath` — null/empty/whitespace `sourcePath`.
3. `BinaryNotFound` — probe or extract `ProcessResult.FailureReason == "LaunchFailed"`.
4. `ProcessTimedOut` — probe or extract `ProcessResult.FailureReason == "TimedOut"`.
5. `ProbeFailed` — ffprobe `NonZeroExit` or `OutputTruncated` (includes missing/unreadable source).
6. `NoVideoStream` — ffprobe succeeded but no video stream.
7. `InvalidDuration` — duration missing, unparseable, non-finite, or `<= 0` (after video stream present).
8. `ExtractionFailed` — ffmpeg `NonZeroExit` or `OutputTruncated`.
9. `InvalidOutput` — output missing, `Length < 2`, or not beginning with SOI `0xFF, 0xD8` (incl. reversed SOI).
10. `StorageError` — `IOException` / `UnauthorizedAccessException` creating cache dir or reading/validating output.

## IProcessRunner (Infrastructure/Thumbnails)

File placement: define `IProcessRunner` in `SystemProcessRunner.cs` alongside `SystemProcessRunner` (no standalone `IProcessRunner.cs`; brief Phase 2 correction 1; constitution:560–561). `ProcessResult` remains in `ProcessResult.cs`.

```csharp
Task<ProcessResult> RunAsync(
    string executable,
    IReadOnlyList<string> arguments,
    TimeSpan timeout,
    CancellationToken cancellationToken = default);
```

```csharp
sealed record ProcessResult(
    bool Success,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    string? FailureReason);
```

Invariants:

- `ProcessStartInfo.ArgumentList` only; `UseShellExecute = false`.
- Retain ≤ 64 KiB per stream; drain both to EOF; truncation ⇒ failed ProcessResult (`OutputTruncated`).
- Success ⇒ exit code 0 without timeout/truncation.
- Injected `TimeProvider` drives `new CancellationTokenSource(timeout, timeProvider)`; DI resolves `TimeProvider` with `TimeProvider.System` fallback; no `DateTime.Now`; no new packages.
- Timeout and caller cancellation kill the process tree; caller cancellation propagates `OperationCanceledException` after cleanup.
- No process API types leak into Core.
- No enum or factory layer — preserve the record signature.

### ProcessResult.FailureReason (closed set; null on Success)

Precedence:

1. `LaunchFailed` — missing executable / `Win32Exception` / start failure (`ExitCode = null`).
2. `TimedOut` — exceeded timeout; tree killed (`ExitCode = null`).
3. `OutputTruncated` — stdout or stderr exceeded 64 KiB while draining to EOF (checked before exit code).
4. `NonZeroExit` — completed within timeout and output cap with `ExitCode != 0`.

## Extraction argument contract (FfmpegThumbnailGenerator)

1. ffprobe (via runner): machine-readable output; InvariantCulture parse; require explicit video stream and finite duration > 0; otherwise failed ThumbnailResult without ffmpeg.
2. seek = `duration * 0.10` (no integer truncation); serialize `-ss` with InvariantCulture (e.g. `125.5` → `"12.55"`).
3. ffmpeg (via runner): discrete args including `-nostdin`, `-y`, `-ss`, `-i`, `-map`, `0:v:0`, `-frames:v`, `1`, `-vf`, and aspect-preserving non-upscaling scale that substitutes the configured `ThumbnailOptions.MaxWidth` into `scale=min(<MaxWidth>\,iw):-2` [assumed] Q6. The literal `320` appears only as the default MaxWidth / test-example value (brief Q5/Q6); production MUST NOT hardcode width `320` when MaxWidth differs.
4. Output path: generator-owned `<guid:N>.jpg` under resolved CacheDirectory.
5. Post-exit validation: exists, `Length >= 2`, SOI `0xFF 0xD8` before Success (0-byte / 1-byte / reversed SOI ⇒ `InvalidOutput`).

## Composition

- `AddLamuFlixThumbnails`: singleton `IProcessRunner` → `SystemProcessRunner` (injected `TimeProvider`); singleton `IThumbnailGenerator` → `FfmpegThumbnailGenerator`; consume `IOptions<ThumbnailOptions>` and host content root; no eager work.
- Register independently of `Features:LocalPlay`; honor Enabled=false.
- ServiceDefaults binds `ThumbnailOptions` and registers conditional `ThumbnailOptionsValidator`.
- Api `Program.cs` calls `AddLamuFlixThumbnails`.
- ArchitectureTests `PortWhitelistPattern` includes `IThumbnailGenerator`.
- Existing `Microsoft.Extensions.Hosting.Abstractions` at `LamuFlix.Infrastructure.csproj:33` supplies `IHostEnvironment` (no new package).

## Test technique (FR-009)

- Fake-runner + tiny temp byte SOI stubs: `tests/LamuFlix.UnitTests/Thumbnails/`.
- Real-process `SystemProcessRunnerTests`: `tests/LamuFlix.IntegrationTests/Thumbnails/` invoking `pwsh -NoProfile -NonInteractive -File` fixtures under `Fixtures/Thumbnails/` via narrow IntegrationTests csproj copy entries; missing `pwsh` fails; no xunit reentry/custom Main/new project/package.

## Out of contract for DEV-17

HTTP endpoints, DTOs, OpenAPI/TS generation, queue consumers, tiles, DB persistence, Playback starter changes, no-video thumbnail support, health-check registration, TelemetryConstants entries (Principle VI N/A per brief Phase 2 correction 3).

Consumer ticket **DEV-428** owns (design constraints only — no DEV-17 implementation expansion):

- Cache retention / eviction
- Queue / endpoint concurrency throttling
- Library-root source-path authorization
