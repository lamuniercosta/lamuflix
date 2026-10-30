# Research — DEV-17 (Phase 0)

All decisions below cite the closed grill (`brief.md`, `CONCLUSIONS.md` Q2–Q10 + Phase 2 plan challenge round 1) and recon. No new decision is made here.

## R1. Core port with process-free result (Q3 supersedes Q1)

- **Decision**: Place `IThumbnailGenerator` and `ThumbnailResult` in `LamuFlix.Core/Ports`; keep runner/process types and `FfmpegThumbnailGenerator` in `LamuFlix.Infrastructure/Thumbnails`.
- **Rationale**: Verbatim ticket Scope 2–3 (recon-DEV-17:84–85,106–107); Core bans process APIs (constitution Principle I / ArchitectureTests); Q3 authorizes ADR + MINOR enumeration amendment + whitelist rather than changing the ticket — prescribed procedure (constitution:223,624–639), not a departure or owner checkbox. No duplicate amendment task.
- **Rejected**: Infrastructure-only `IThumbnailGenerator` (Q1) — contradicted by ticket text after verbatim recon.

## R2. Dedicated bounded process runner, independent of LocalPlay (Q2)

- **Decision**: New asynchronous `SystemProcessRunner` / `IProcessRunner` with ArgumentList, no shell, stream drain, 64 KiB cap, tree kill on timeout/cancellation; injected `TimeProvider` + `CancellationTokenSource(timeout, timeProvider)`; register thumbnails outside `Features:LocalPlay`; leave playback `IProcessStarter` untouched.
- **Rationale**: Playback starter is fire-and-forget (recon:20–22); containers run with LocalPlay=false while still needing ffmpeg in the Api image (recon:15,35,53,83); Charter 2.3 item 5 approved; AGENTS.md bans `DateTime.Now`.
- **Rejected**: Reusing `IProcessStarter`; gating thumbnail DI behind LocalPlay; converting caller cancellation into a failed result; new timeout packages.

## R3. Generator-owned cache JPEG and failed-result errors (Q4)

- **Decision**: Generator owns per-attempt `<guid:N>.jpg` under CacheDirectory; succeed only after `Length >= 2` JPEG SOI validation; Enabled=false returns `Disabled` with no process/disk work; operational failures return closed FailureReason tokens without throw; no enum/factory.
- **Rationale**: Ticket Scope 2/5 and AC4 (recon:84–87,93,101); Phase 2 plan challenge round 1 item 4 closed catalogue; no DEV-428 consumer in this ticket.
- **Rejected**: Caller-selected output path; persistent cache lookup; DB writes; source deletion; exposing raw stderr/paths as FailureReason.

## R4. Options defaults, conditional validation, path derivation (Q5)

- **Decision**: Enabled=false (ticket); assumed FfmpegPath/CacheDirectory/MaxWidth/ProcessTimeoutSeconds/SectionName; validate when Enabled=true without binary probe; sibling/PATH ffprobe derivation; content-root cache resolution via existing `Microsoft.Extensions.Hosting.Abstractions` at `LamuFlix.Infrastructure.csproj:33` (recon-DEV-17-2:15-18). `FfmpegPath`/`CacheDirectory` are trusted operator config, never user/API input. Per-call timeout ⇒ worst-case ≤ `2 * ProcessTimeoutSeconds` + termination/cleanup.
- **Rationale**: Ticket names Enabled/FfmpegPath/CacheDirectory; width cap and timeout bounds required by Q2/Q6; ASSUMPTIONS.md logs taste/defaults; Hosting.Abstractions already present — reject adding a package (Ledger M5).
- **Rejected**: Startup binary existence probe (would break AC4 missing-binary generation-time proof); machine-specific absolute default paths; new Hosting.Abstractions package claim.

## R5. Probe, 10% seek, and scale contract (Q6)

- **Decision**: Machine-readable ffprobe duration + explicit video presence; seek = duration * 0.10 without integer truncation, InvariantCulture serialization; discrete noninteractive ffmpeg single-frame first-video-stream JPEG with MaxWidth-capped non-upscaling scale; missing source via ffprobe `ProbeFailed` without `File.Exists` precheck.
- **Rationale**: Ticket Scope 2 and AC2 (recon:84,91,106,115); constitution ArgumentList rule; Phase 2 plan challenge round 1 items 5/7.
- **Rejected**: Shell-interpolated command lines; integer-truncated seek; parsing truncated probe output; File.Exists precheck.

## R6. Four-part proof strategy (Q7 + Phase 2 plan challenge round 1)

- **Decision**: Fake-runner UnitTests (tiny temp byte SOI stubs only) + deterministic `SystemProcessRunnerTests` in IntegrationTests/Thumbnails via EvaluationCommand-style `pwsh -NoProfile -NonInteractive -File` fixtures under `Fixtures/Thumbnails/` with narrow csproj copy entries + RequiresFfmpeg fixture at `short-video.mp4` + Docker app-user exact version outputs + size-delta receipt via Rigger. Missing tools / missing `pwsh` fail required tests. Hosted CI ffmpeg/ffprobe availability is unproved (recon-DEV-17-2:9-12) — Phase B T010/T061 prove it without skip/filter/workflow edits. Infrastructure mutation N/A; Core mutation unchanged; no mandatory new FsCheck generator suite.
- **Rationale**: Ticket AC1–AC4; zero-byte sample-library files unusable (recon:44); recon-DEV-17-2:21-25 (no xunit custom Main / full-suite reentry); no CI workflow edits; Phase 2 plan challenge round 1 items 1/3/5.
- **Rejected**: Silent skip-if-missing for RequiresFfmpeg; new production project/package for runner tests; xunit executable reentry / custom Main; "test-side child mode" phrasing from superseded Q7/Q8; claiming hosted tools already proven; mandatory FsCheck generator suite for FfmpegThumbnailGenerator.

## R7. Bounded file envelope and ADR 0033 (Q8 + Phase 2 corrections)

- **Decision**: Eight new production files per brief Q8 as corrected in Phase 2 analyze: co-locate `IProcessRunner` in `SystemProcessRunner.cs` (no standalone interface file); keep `ProcessResult.cs` separate; Phase B reconciles/adds `Thumbnail` in `CONTEXT.md` with term/definition/avoid (optional Principle VIII summary sync); no health-check or TelemetryConstants files. Singleton stateless DI; ADR 0033 + constitution 1.8.0 present in the Phase A working tree (Rigger includes both in Phase A commit after diff/status review; `harness.yml` untouched/unstaged; no duplicate amendment task). Spec artifacts are valid strict UTF-8 (recon-DEV-17-2:35-40) — no encoding rewrite. DEV-428 owns retention/eviction, consumer concurrency, and library-root authorization (design constraints only).
- **Rationale**: Charter 2.3 item 6 approved for unnamed wiring including CONTEXT.md and IntegrationTests fixture-copy entries; constitution:560–561 co-location; VIII:393,408 glossary gate; VI N/A for local in-image CLI; Phase 2 plan challenge round 1 items 6/9/10.
- **Rejected**: Standalone `IProcessRunner.cs`; inventing an ADR number before recon; health/telemetry registration for ffmpeg; editing CI/compose/Playback/Directory.Packages.props/generated files; encoding rewrite; unrelated ADR 0031 / port-list hygiene corrections (already `noted, no ticket`).
