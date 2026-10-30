# DEV-17 assumptions

## Q5 — Patron-approved tuning and path conventions

[assumed] SectionName = Thumbnails; FfmpegPath = ffmpeg; CacheDirectory = cache/thumbnails; MaxWidth = 320; ProcessTimeoutSeconds = 30. Enabled=false is ticket text, not an assumption.

[assumed] Derive ffprobe as a sibling executable when FfmpegPath includes a directory, otherwise resolve bare ffprobe on PATH; retain an executable extension such as .exe. Resolve relative CacheDirectory against IHostEnvironment.ContentRootPath, falling back to AppContext.BaseDirectory when unhosted; use rooted paths as-is.

Basis: Q5 full cited ruling in CONCLUSIONS.md; recon-DEV-17:84-87,108. These tune ticket requirements without changing their scope.

## Q6 — Process output and extraction conventions

[assumed] Retain at most 64 KiB per stdout/stderr stream, continue draining to EOF, and fail on truncation. Use noninteractive ffmpeg (-nostdin, -y), first-video-stream mapping (-map 0:v:0), a single output frame (-frames:v 1), and an aspect-preserving width cap without upscaling. Exact argument construction must preserve these guarantees. Basis: Q6 full cited ruling in CONCLUSIONS.md.

## Q7 — Fixture location and test marker

[assumed] Synthetic video fixture: tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/short-video.mp4. External-tool test marker: Trait(Category, RequiresFfmpeg). Missing tools fail the required test. Basis: Q7 full cited ruling in CONCLUSIONS.md.

## Plan Challenge Round 1 — Failure-token catalogue & portable child-process fixture scripts

[assumed] Closed failure-token sets (preserving existing record signatures without enums or factories):
- `ProcessResult.FailureReason` (null on success): `LaunchFailed`, `TimedOut`, `OutputTruncated` (checked before exit code), `NonZeroExit`.
- `ThumbnailResult.FailureReason` (null on success, evaluated in precedence order): `Disabled`, `InvalidSourcePath`, `BinaryNotFound`, `ProcessTimedOut`, `ProbeFailed`, `NoVideoStream`, `InvalidDuration`, `ExtractionFailed`, `InvalidOutput`, `StorageError`.

[assumed] Deterministic `SystemProcessRunnerTests` live in `tests/LamuFlix.IntegrationTests/Thumbnails/` and spawn `pwsh` (`-NoProfile -NonInteractive -File <fixture.ps1>`) against committed PowerShell fixture scripts under `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/` (following `EvaluationCommand.cs:37-69`), copied via narrow `LamuFlix.IntegrationTests.csproj` `None`/`CopyToOutputDirectory` entries. Basis: Phase 2 plan challenge round 1 items 2–4 in CONCLUSIONS.md; `recon-DEV-17-2:21-25`.

