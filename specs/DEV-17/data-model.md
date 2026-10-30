# Data model — DEV-17

No database entities. Shapes below are in-memory/configuration/filesystem contracts from brief Q4–Q6 + Phase 2 plan challenge round 1.

## ThumbnailResult (Core/Ports)

| Field | Type | Rules |
| --- | --- | --- |
| Success | bool | true only with nonempty CachePath and null FailureReason |
| CachePath | string? | nonempty path on success; null on failure |
| FailureReason | string? | null on success; closed token on failure (never raw exception/stdout/stderr/paths) |

Closed FailureReason tokens (strict precedence): `Disabled`, `InvalidSourcePath`, `BinaryNotFound`, `ProcessTimedOut`, `ProbeFailed`, `NoVideoStream`, `InvalidDuration`, `ExtractionFailed`, `InvalidOutput`, `StorageError`. No enum/factory. A non-whitespace missing/unreadable `sourcePath` is not pre-checked via `File.Exists` and surfaces as `ProbeFailed` when ffprobe fails.

## ThumbnailOptions (Core/Options)

| Member | Default / rule | Source |
| --- | --- | --- |
| SectionName | `Thumbnails` | [assumed] Q5 |
| Enabled | `false` | ticket recon:86,108 |
| FfmpegPath | `ffmpeg` | [assumed] Q5 — trusted operator config, never user/API input |
| CacheDirectory | `cache/thumbnails` | [assumed] Q5 — trusted operator config, never user/API input |
| MaxWidth | `320` (strictly positive when Enabled) | [assumed] Q5 |
| ProcessTimeoutSeconds | `30` (strictly positive when Enabled) | [assumed] Q5 — per `RunAsync` call; worst-case generation ≤ `2 *` value + termination/cleanup |

Validation when Enabled=true: non-whitespace FfmpegPath and CacheDirectory; MaxWidth > 0; ProcessTimeoutSeconds > 0. No binary probe; no directory creation at startup.

Derived (not stored options fields):

- **ffprobe path**: sibling of FfmpegPath when it has a directory component, retaining executable extension; otherwise bare `ffprobe` on PATH ([assumed] Q5).
- **Resolved cache root**: relative CacheDirectory against `IHostEnvironment.ContentRootPath` (via existing Hosting.Abstractions at Infrastructure csproj:33), else `AppContext.BaseDirectory`; rooted paths as-is ([assumed] Q5).

## ProcessResult (Infrastructure/Thumbnails)

| Field | Type | Rules |
| --- | --- | --- |
| Success | bool | true only for exit code 0 without timeout or truncation |
| ExitCode | int? | process exit when available; null on LaunchFailed/TimedOut |
| StandardOutput | string | retained ≤ 64 KiB |
| StandardError | string | retained ≤ 64 KiB |
| FailureReason | string? | closed token on failure |

Closed FailureReason tokens (precedence): `LaunchFailed`, `TimedOut`, `OutputTruncated` (before exit code; `== 65536` succeeds, `65537` truncates while draining), `NonZeroExit`. No enum/factory.

Streams continue draining to EOF even after the retention cap; truncation fails the run.

## Cache JPEG (filesystem)

- Location: under resolved CacheDirectory.
- Name: collision-resistant `<guid:N>.jpg` per attempt (generator-owned).
- Success predicate: file exists, `Length >= 2`, begins with SOI bytes `0xFF, 0xD8` (0-byte / 1-byte / reversed SOI ⇒ `InvalidOutput`).
- Lifecycle: create CacheDirectory when generation needs it; best-effort delete incomplete/invalid output on failure or cancellation; no cross-call reuse or persistent lookup in DEV-17. Retention/eviction owned by DEV-428.
