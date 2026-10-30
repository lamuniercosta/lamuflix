# DEV-17 — Thumbnail generation grill brief

Grill closed by Patron at Q10/12 on 2026-10-30. This brief records the settled decisions for Quill; it is not plan freeze, provisional Gate 1, implementation approval, or evidence that tests/gates passed.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-017-spec`; branch: `feature/017-spec` (verified before document edits). Recon baseline: `477ac3934f93a09bd0e78e95b7b5ca04bd92aa84`. Facts come exclusively from canvas notes `DEV-17` and `recon-DEV-17`; the latter contains the verbatim ticket at lines 69–120. Full questions, cited Patron answers, rationale and implications are append-only in [CONCLUSIONS.md](CONCLUSIONS.md). Patron-approved default/convention choices are tagged `[assumed]` in [ASSUMPTIONS.md](ASSUMPTIONS.md).

## Authority and decision ledger

The ticket is authoritative, particularly recon lines 80–87 and 89–101. Q1 used the initial intake summary; the verbatim ticket revealed a placement contradiction. Q3 expressly supersedes Q1. Do not reuse the Infrastructure-only generator-interface decision.

| Question | Binding answer |
| --- | --- |
| Q1 | Initial Infrastructure-only placement is superseded by Q3; retained only as history. |
| Q2 | Dedicated asynchronous runner; ArgumentList, no shell, bounded lifecycle and tree cleanup. Operational errors return failure; caller cancellation propagates. Extraction is independent of LocalPlay. |
| Q3 | Ticket-mandated Core port and process-free result; Infrastructure adapter/runner; Core options default disabled. ADR, MINOR constitution amendment and whitelist extension are permitted constitutional procedure, with no owner checkbox. |
| Q4 | Exact generator/result shape, generator-owned per-attempt cache output, validation, cleanup, disabled behavior and failed-result boundaries below. |
| Q5 | Exact options/defaults, conditional startup validation, ffprobe derivation and content-root cache resolution below; assumed conventions logged. |
| Q6 | Exact runner/result shape, bounded output, explicit video/duration probe, fractional invariant 10% seek and capped non-upscaling JPEG below. |
| Q7 | Fake-runner/options/runner tests, committed synthetic real-tool fixture test, image app-user and size-delta proofs; no silent missing-tool pass. |
| Q8 | Bounded production/test/governance file envelope and unnamed wiring edits; stateless singleton composition; ADR numbering is a pre-drafting recon prerequisite. |
| Q9 | Phase A/B ordering, dependency/drift checks, L-tier gates, all three review axes and changed-Core mutation. |
| Q10 | Shared understanding reached; grill closed at Q10; closing bar, frozen scope and two-round cap approved. |

The prior recon stop after Q2 is resolved by recon lines 69–120. No ticket contradiction or constitution departure remains approved in the final rulings; there is no owner structural checkbox. Ordinary owner Gate 1/merge authority remains intact.

## Approach and contracts

Work runs in the Api host, not the retired Worker (recon:80). Follow the existing Core port / Infrastructure adapter / Core options architecture and the existing xUnit v3, NSubstitute, Shouldly and FsCheck stack (recon:24,29,32–35,39–42).

- `IThumbnailGenerator` in `LamuFlix.Core/Ports` exposes `Task<ThumbnailResult> GenerateAsync(string sourcePath, CancellationToken cancellationToken = default)`.
- Process-free `sealed record ThumbnailResult(bool Success, string? CachePath, string? FailureReason)` lives beside the port. Success requires a nonempty cache path and null failure reason; failure requires null cache path and a bounded stable reason token. Never expose raw exceptions/stdout/stderr as its reason.
- `FfmpegThumbnailGenerator`, `IProcessRunner`, `SystemProcessRunner`, process results and DI extension live in `LamuFlix.Infrastructure/Thumbnails`. No process API type enters Core.
- `IProcessRunner` exposes `Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default)`.
- `sealed record ProcessResult(bool Success, int? ExitCode, string StandardOutput, string StandardError, string? FailureReason)` succeeds only for exit 0 without timeout/truncation. Retain at most 64 KiB per stream, continue draining both to EOF, and fail on truncation.

The runner uses `ProcessStartInfo.ArgumentList` with `UseShellExecute = false`, asynchronously drains stdout/stderr, disposes resources and terminates the process tree on timeout or caller cancellation. Missing executable, nonzero exit, launch failure, timeout and output truncation return structured failures. Caller-requested cancellation propagates `OperationCanceledException` after cleanup. Do not reuse or modify playback's fire-and-forget `IProcessStarter`.

The generator probes duration and explicit video-stream presence using machine-readable ffprobe output. Reject absent/unparseable/nonfinite/nonpositive duration and absent video before ffmpeg. Compute `duration * 0.10` without integer truncation and serialize seek invariantly. Invoke ffmpeg noninteractively (`-nostdin`, `-y`), select the first video stream (`-map 0:v:0`), write one JPEG frame (`-frames:v 1`), preserve aspect ratio, cap width at MaxWidth and avoid upscaling. Pass flags and values as discrete arguments.

When disabled, return failure reason `Disabled` before any process or disk activity. Validate sourcePath is nonblank; do not require a real source file before the runner, so fake-runner tests need no media file. Missing/unreadable source, missing binaries, process/probe failures, no video stream, invalid output and filesystem I/O failures return failed results. Supporting thumbnails for files without video remains outside scope.

The generator creates the configured cache directory only when generation needs it, owns a collision-resistant `<guid:N>.jpg` filename per attempt beneath that directory, and returns success only after the target exists, is nonempty and begins with JPEG SOI bytes `0xFF, 0xD8`. Delete incomplete/invalid output best-effort on failure/cancellation; cleanup must preserve cancellation semantics. No caller-selected destination, source deletion, persistent cache lookup, DB writes or cross-call reuse is included. The real-tool test proves JPEG generation beyond fake-runner byte stubs.

## Options and composition

`ThumbnailOptions` lives in `Core/Options`, with conditional `ThumbnailOptionsValidator` and normal AddLamuFlixOptions binding/startup validation.

| Member | Settled default/rule |
| --- | --- |
| SectionName | `Thumbnails` [assumed] |
| Enabled | `false`, required by ticket recon:86,108 |
| FfmpegPath | `ffmpeg` [assumed] |
| CacheDirectory | `cache/thumbnails` [assumed] |
| MaxWidth | `320` [assumed], strictly positive when enabled |
| ProcessTimeoutSeconds | `30` [assumed], strictly positive when enabled |

When enabled, reject whitespace FfmpegPath/CacheDirectory and nonpositive width/timeout at startup. Do not probe executable availability or create directories during registration/startup; missing binary remains a generation-time failed-result test. Disabled configuration does not require binaries or touch disk.

Derive ffprobe from the sibling of FfmpegPath when it contains a directory, retaining an executable extension; otherwise use bare ffprobe on PATH. Resolve relative CacheDirectory against `IHostEnvironment.ContentRootPath`, falling back to `AppContext.BaseDirectory` when unhosted; rooted cache paths remain as supplied. These conventions are [assumed] under Q5.

`AddLamuFlixThumbnails` registers stateless runner and generator singletons consuming `IOptions<ThumbnailOptions>` and the host content root. Register independently of `Features:LocalPlay`, while honoring `ThumbnailOptions.Enabled=false`. Server-side decoding is not media-player execution; existing LocalPlay playback gating remains unchanged (Q2/Q3).

## Files touched: approved envelope

New production files:

- `src/LamuFlix.Core/Ports/IThumbnailGenerator.cs`
- `src/LamuFlix.Core/Ports/ThumbnailResult.cs`
- `src/LamuFlix.Core/Options/ThumbnailOptions.cs`
- `src/LamuFlix.Core/Options/ThumbnailOptionsValidator.cs`
- `IProcessRunner` is defined in `src/LamuFlix.Infrastructure/Thumbnails/SystemProcessRunner.cs` alongside its primary implementation; no standalone interface file (post-grill Patron ruling; constitution:560–561).
- `src/LamuFlix.Infrastructure/Thumbnails/ProcessResult.cs`
- `src/LamuFlix.Infrastructure/Thumbnails/SystemProcessRunner.cs`
- `src/LamuFlix.Infrastructure/Thumbnails/FfmpegThumbnailGenerator.cs`
- `src/LamuFlix.Infrastructure/Thumbnails/ThumbnailServiceCollectionExtensions.cs`

Narrow edits to existing files, not whole-file rewrites/deletions:

- `docker/Dockerfile.api`: install ffmpeg (including ffprobe) in runtime as root before `USER app`, using `apt-get install --no-install-recommends` and clearing apt lists (ticket recon:83,105).
- `src/LamuFlix.ServiceDefaults/Extensions.cs`: bind ThumbnailOptions and register its conditional validator.
- `src/LamuFlix.Api/Program.cs`: call AddLamuFlixThumbnails.
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`: extend PortWhitelistPattern with the ticket-mandated port.
- `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs`: focused startup-validation coverage.
- `.specify/memory/constitution.md`: focused MINOR 1.7.0 → 1.8.0 amendment adding IThumbnailGenerator to Principle I and ThumbnailOptions to Principle VII via the established enumeration-extension procedure.
- Existing test `.csproj` files only as needed for fixture copy-to-output or deterministic test-child execution; no new packages/projects.

Add focused tests under `tests/LamuFlix.UnitTests/Options/` and `tests/LamuFlix.UnitTests/Thumbnails/`; add thumbnail integration tests under the existing IntegrationTests project and synthetic fixture `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/short-video.mp4`. Permit the test-side child mode required for deterministic runner tests within existing test projects.

One focused ADR in `docs/adr/` records the Core-port extension, process boundary, result contract and LocalPlay distinction. **Before drafting it, bounded recon must reserve the next four-digit ADR number.** Q8 explicitly permits brief closure before this numbering fact is supplied. Do not invent a filename number or treat the ADR as already authored. Subsequent Phase A spec/plan/tasks belong to Quill, not this grill deliverable.

No edits to Directory.Packages.props, docker-compose.yml, CI workflows, existing Playback files, generated files, or unrelated code. Preserve the pre-existing harness.yml modification recorded at recon:6; it is not part of this work.

## Test strategy and acceptance receipts

1. Fake-runner generator tests assert the 10% fractional seek argument under a non-English culture; discrete arguments/paths with spaces; invalid duration/video, disabled no-work, nonzero exit, timeout, missing binary, invalid/empty JPEG and partial-output cleanup. No unit test needs ffmpeg or a real source clip; use tiny temporary output bytes only when output validation requires them.
2. Options unit/property/survivor and startup tests cover defaults, conditional validation and path/executable derivation. Composition tests prove registrations, no eager work and thumbnail availability independent of LocalPlay.
3. Deterministic SystemProcessRunner tests cover ordinary success/capture, missing binary without throw, nonzero exit, simultaneous stdout/stderr above the cap with no deadlock, truncation failure, timeout tree cleanup and caller cancellation. Use a test-side executable mode within an existing test project, not a new production project or package.
4. A `[Trait("Category", "RequiresFfmpeg")]` real-tool integration test uses the committed tiny synthetic video, enabled options and a temporary cache. Assert generation succeeds, produces a JPEG and respects MaxWidth. Missing ffmpeg/ffprobe fails this required test; it is never silently skipped or reported as acceptance passed. Fixture path and marker are [assumed] under Q7. The existing zero-byte sample-library files are unusable (recon:44).
5. Build `docker/Dockerfile.api`; verify `ffmpeg -version` and `ffprobe -version` succeed as `app`. Capture image-size increase against a comparable baseline runtime image. Route measured evidence to Rigger for verified YouTrack recording; Keel never writes the tracker. No CI workflow changes are authorized.

AC mapping: ticket recon:90/114 → image/app-user proof; :91/115 → fake-runner 10% seek; :92/116 → marked real-video JPEG test; :93/117 → missing-binary failed-result test. Image-size ticket recording is additionally mandatory Scope item 1 (:83/105).

## Gate expectations and ordering constraints

Phase A after this brief: bounded ADR-number recon → Keel ADR and focused constitution amendment → Quill spec/plan/tasks → read-only speckit-analyze against this brief → all L-tier plan challenge axes and adjudication → plan freeze → Patron provisional Gate 1 → Rigger spec PR → owner merge. Any additional factual gap returns to bounded recon; decisions return to Patron. The ADR/constitution amendments are not claimed complete by this brief.

Phase B starts only after Gate 1: verify DEV-304/DEV-314 landing and baseline alignment, reread main for pickup drift and re-run cross-artifact analysis before coding. Recon:57 documents specs, not proof of dependency landing. Establish governance/port/options and architecture whitelist before Infrastructure runner/generator/composition; pair code with focused tests. Install image tools before Docker acceptance and real-fixture execution. Then refactor, verify, complete three-axis review/ship-review and Rigger's size-recording receipt before merge-bar evidence is signed.

Required gates use actual harness.yml thresholds: Roslyn analyzers, cyclomatic complexity, InspectCode, property tests, vulnerable-package scan, full dotnet test including architecture and RequiresFfmpeg, format/diff checks and mutation of eligible changed Core code. Do not lower thresholds, substitute build for an unrunnable gate, or use mutation `--since` from worktrees. Configured Infrastructure/Api mutation exclusions remain NOT APPLICABLE for those projects; they do not waive eligible Core mutation. All Sentry/Ledger/Compass reports are required for this L task. Evidence must match the final PR HEAD.

Report scope-empty exit 2 as `SKIPPED (scope-empty)`, configured opt-out as `SKIP`, mutation exclusion as N/A/NOT APPLICABLE, and an unrunnable gate as `Could not run`; none is PASS. A failed required gate blocks completion. No tests, analyzer gates, runtime acceptance, implementation, commit, push or merge is claimed by this grill. The owner merges every PR.

## Charter §2.3 dispositions

| Item | Deliberate ruling |
| --- | --- |
| 1 — dependency | No NuGet/npm dependency added. OS ffmpeg installation is explicitly ticket-approved (recon:83); Q5/Q8 prohibit package additions. |
| 2 — architecture/port | Core IThumbnailGenerator is explicitly ticket-approved (recon:84). Q3 supersedes Q1 and authorizes ADR + MINOR enumeration amendment + whitelist update; no new project/top-level layer or constitution departure. |
| 3 — schema | No DB schema change (recon:51,99–100; Q10). |
| 4 — public API | No routes/DTO/status-code changes or generated-contract changes; endpoint remains DEV-428 (recon:52,99–100; Q10). |
| 5 — Process.Start/LocalPlay/secrets | Triggered and approved by Q2/Q3: dedicated safe runner, output/lifecycle bounds, extraction independent of LocalPlay with Enabled=false. Existing playback gate remains. No secrets or credential edits. |
| 6 — unnamed files | Q3/Q8 expressly authorize only the narrow wiring, validator, whitelist, test-project and governance edits listed above. No unrelated deletions or rewrites. |

Only a ticket-deliverable change or a constitution departure judged necessary by Patron would create a `blocked: structural` owner checkbox. Neither is approved here. Missing ADR numbering is a recon prerequisite, not an owner question.

## Loop discipline (Patron Q10)

Closing bar for code-review/ship-review: zero unresolved Critical/High/Medium findings; every Low has an explicit recorded disposition; all required gates and acceptance receipts earned against the same HEAD; complete Sentry, Ledger and Compass reports. Green hosted checks alone do not satisfy this bar.

Frozen scope: ticket API-image ffmpeg/ffprobe installation; Core thumbnail port/result/options/validator; focused ADR and permitted constitution/whitelist extension; Infrastructure process runner/generator/DI; narrowly approved wiring; focused tests; app-user, real-fixture and image-size proof. Consumer/endpoint DEV-428, tile DEV-429, no-video thumbnail support, DB/public API, Playback changes, unrelated refactors, new packages/projects, and CI/compose changes remain excluded; anything else is a follow-up issue, not a finding in this round.

Round cap: two review/remediation rounds maximum. A remaining blocker at cap is reported BLOCKED with evidence; no third round, threshold reduction or gate bypass. Grill used ten of twelve allowed questions. Brief closure does not freeze Quill's future plan or close Gate 1.

## Phase 2 analyze decision corrections (Patron, 2026-10-30)

The full cited question and answer are appended in CONCLUSIONS.md under Phase 2 analyze. These corrections supersede the affected Q8 file list while preserving the settled runtime contracts and two-round cap.

1. Define the non-port IProcessRunner interface alongside SystemProcessRunner in `src/LamuFlix.Infrastructure/Thumbnails/SystemProcessRunner.cs`; remove the standalone IProcessRunner.cs from the approved envelope. ProcessResult.cs remains separate. Basis: constitution:560–561.
2. Authorize narrow `CONTEXT.md` reconciliation/addition of `Thumbnail` in term/definition/avoid format, with an explicit Phase B task in the introducing PR. Reconcile the Principle VIII summary list only if needed. This does not assume whether a glossary entry already exists. Basis: constitution VIII:393,408, quality gate:500 and documentation:613–614; Patron expressly permits these unnamed-file edits under Charter 2.3 item 6.
3. Principle VI health-check and telemetry registration is NOT APPLICABLE to this local in-image CLI capability under Patron interpretation of constitution:348–365,494–495. No health-check or TelemetryConstants files are added: this capability is analogous to the local player-process boundary, defaults disabled, and missing tools remain generation-time failed results. Queue consumer/endpoint remain DEV-428. Record the applicability disposition in plan and ADR. No constitution departure or owner structural checkbox is introduced.

These are decision corrections only. Analysis clearance, plan freeze, Gate 1, tests and delivery approval remain separate.
## Phase 2 plan challenge round 1 — Binding decision corrections

The following complete Patron ruling supersedes affected Q7-Q9 wording and file/test envelopes above. No spec/plan/tasks correction is complete until Quill repair and Keel passage verification. This is no Gate 1 or delivery gate approval.

[from Patron] DEV-17 Phase 2 plan challenge round 1 rulings (all 10 decisions / 30 adjudication-DEV-17-plan entries resolved; recorded in specs/DEV-17/CONCLUSIONS.md and specs/DEV-17/ASSUMPTIONS.md; zero ticket-deliverable changes, zero constitution departures, zero owner structural checkboxes):

Verdict: APPROVE all 10 bounded plan-challenge round 1 rulings below.

1. CI `ffmpeg`/`ffprobe` Availability & Phase B Pre-Coding Blocker (Ledger H1, Sentry M1):
   - Verdict: ACCEPT.
   - Retain full `dotnet test` execution including `[Trait("Category", "RequiresFfmpeg")]`, fail-if-missing semantics (`AGENTS.md`: "A gate that could not run has not passed"; `brief.md:98`), and zero `.github/workflows/*` edits (`Charter §2.3` item 6).
   - Because `.github/workflows/ci.yml:16,34-35` runs `dotnet test --filter "Category!=OllamaIntegration"` on `ubuntu-latest` while hosted runner `ffmpeg`/`ffprobe` presence is unproved in repo roots (`recon-DEV-17-2:9-12`), Quill MUST remove any claim in `plan.md:21` or `tasks.md` that hosted runner tools are already proven, and MUST add an explicit Phase B pre-coding prerequisite in `T010` (and pre-PR gate check in `T061`) to verify hosted runner tool availability — prohibiting test skips or filter edits and stopping for bounded recon + Patron ruling if hosted `ubuntu-latest` lacks `ffmpeg`/`ffprobe`.

2. `TimeProvider` Timeout Injection & Test Placement Split (Ledger M1, Ledger M2):
   - Verdict: ACCEPT.
   - `SystemProcessRunner` MUST accept injected `TimeProvider` (defaulting to `TimeProvider.System` when not supplied or resolved from DI; `AddLamuFlixThumbnails` resolves `TimeProvider` from `IServiceProvider` with `TimeProvider.System` fallback) and drive per-process timeout via `new CancellationTokenSource(timeout, timeProvider)` without `DateTime.Now` or new packages (`AGENTS.md` Non-negotiables; `.cursor/rules/vendor/aaron-csharp-testing.mdc`).
   - Reconcile `Ledger M2` with `brief.md:87,95,97`: keep fast fake-runner `FfmpegThumbnailGeneratorTests` in `tests/LamuFlix.UnitTests/Thumbnails/` using tiny temporary byte stubs only for the post-exit JPEG SOI/cleanup assertions (`Q7` approved exception; no filesystem abstraction added), and move all real-process/disk `SystemProcessRunnerTests` (`T035`) to `tests/LamuFlix.IntegrationTests/Thumbnails/`.

3. Portable Safe Child-Process Test Technique via `pwsh` Fixture Scripts (Ledger M7, Sentry L7; `Charter §2.3` item 6 correction to Q7/Q8):
   - Verdict: ACCEPT (`[assumed]` logged in `ASSUMPTIONS.md`).
   - Because all five runnable test projects use xUnit v3's injected entry point that executes the full test suite by default and no custom `Main` override is established (`recon-DEV-17-2:21-24`), supersede Q7/Q8's "test-side executable mode" wording with the existing repo precedent in `tests/LamuFlix.Evaluation/TestSupport/EvaluationCommand.cs:37-69` (`recon-DEV-17-2:24`).
   - `SystemProcessRunnerTests` in `tests/LamuFlix.IntegrationTests/Thumbnails/` (`T035`) MUST invoke `pwsh` (`-NoProfile`, `-NonInteractive`, `-File`, `<script-path>`, plus discrete test arguments) against committed fixture script(s) under `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/` copied via narrow `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` `None`/`CopyToOutputDirectory` entries (`recon-DEV-17-2:25`; `Charter §2.3` item 6 approved).
   - Production `SystemProcessRunner` continues to use `ProcessStartInfo.ArgumentList` with `UseShellExecute = false` (never a shell wrapper), child scripts never invoke the test executable (eliminating recursive test reentry), and missing `pwsh` fails the test loudly.

4. Closed Failure-Token Catalogue & Precedence (Ledger M4, Sentry L2):
   - Verdict: ACCEPT (`[assumed]` logged in `ASSUMPTIONS.md`).
   - Preserve `sealed record ThumbnailResult(bool Success, string? CachePath, string? FailureReason)` and `sealed record ProcessResult(bool Success, int? ExitCode, string StandardOutput, string StandardError, string? FailureReason)` with no enum or factory methods (`brief.md:31,34`).
   - `ProcessResult.FailureReason` (null when `Success == true`, evaluated in precedence):
     1. `LaunchFailed` — missing executable / `Win32Exception` / process start failure (`ExitCode = null`).
     2. `TimedOut` — process exceeded `timeout` and tree was killed (`ExitCode = null`).
     3. `OutputTruncated` — stdout or stderr exceeded the 64 KiB retention cap while draining to EOF (checked before exit code so a truncated exit-0 process still fails).
     4. `NonZeroExit` — process completed within timeout and output cap with `ExitCode != 0`.
   - `ThumbnailResult.FailureReason` (null when `Success == true`; `CachePath = null` on failure; evaluated in strict step order):
     1. `Disabled` — `ThumbnailOptions.Enabled == false` (short-circuits before argument check, process, or disk).
     2. `InvalidSourcePath` — `sourcePath` is null, empty, or whitespace.
     3. `BinaryNotFound` — `ffprobe` or `ffmpeg` returned `ProcessResult.FailureReason == "LaunchFailed"`.
     4. `ProcessTimedOut` — `ffprobe` or `ffmpeg` returned `ProcessResult.FailureReason == "TimedOut"`.
     5. `ProbeFailed` — `ffprobe` returned `NonZeroExit` or `OutputTruncated` (includes missing/unreadable source files).
     6. `NoVideoStream` — `ffprobe` output succeeded but contains no video stream.
     7. `InvalidDuration` — `ffprobe` duration is missing, unparseable, non-finite, or `<= 0`.
     8. `ExtractionFailed` — `ffmpeg` returned `NonZeroExit` or `OutputTruncated`.
     9. `InvalidOutput` — target JPEG file does not exist after exit 0, has `Length < 2`, or does not begin with SOI bytes `0xFF, 0xD8`.
     10. `StorageError` — `IOException` or `UnauthorizedAccessException` when creating `CacheDirectory` or reading/validating the output file.
   - Never expose raw file paths, exception messages, stdout, or stderr in `FailureReason`.

5. Mutation Boundary & Strengthened Unit/Boundary Assertions (Ledger M3, Sentry M4):
   - Verdict: ACCEPT.
   - Preserve `LamuFlix.Infrastructure` mutation `NOT APPLICABLE` (`harness.yml:29-35`; `recon-DEV-17:48`) and mutate eligible changed `LamuFlix.Core` types (`ThumbnailResult`, `ThumbnailOptions`, `ThumbnailOptionsValidator`) without `--since`.
   - Do not add a mandatory FsCheck property suite for `FfmpegThumbnailGenerator`; instead require explicit deterministic unit/integration assertions in `T034`/`T035` covering:
     - Fractional non-integer duration (`e.g. 125.5` -> `"12.55"`) formatted under a comma-decimal culture (`fr-FR` or `de-DE`) to catch integer truncation and culture bugs;
     - Exact discrete `ArgumentList` sequence including `-nostdin`, `-y`, `-ss`, `-i`, `-map`, `0:v:0`, `-frames:v`, `1`, `-vf`, and `scale=min(320\,iw):-2` (proving width cap and non-upscaling);
     - Post-exit JPEG validation boundary: 0-byte file fails, 1-byte file (`0xFF`) fails, reversed SOI (`0xD8, 0xFF`) fails, and valid `>= 2`-byte `0xFF, 0xD8` succeeds;
     - Output retention cap boundary in `T035`: `== 65536` bytes succeeds (`Success == true`), `65537` bytes fails with `FailureReason == "OutputTruncated"` while draining to EOF.

6. `IHostEnvironment` Reference, Constitution Working-Tree Diff & Q3 Confirmation (Ledger M5, Ledger M6, Compass Low-2, Compass Info-2):
   - Verdict: ACCEPT.
   - `Ledger M5`: REJECT — `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj:33` already references `Microsoft.Extensions.Hosting.Abstractions` (`10.0.14` in `Directory.Packages.props:25`), which provides `Microsoft.Extensions.Hosting.IHostEnvironment.ContentRootPath` with zero new packages (`recon-DEV-17-2:15-18`). Update `plan.md` / `research.md` to cite `LamuFlix.Infrastructure.csproj:33`.
   - `Ledger M6`: REJECT — confirm `Q3`: `IThumbnailGenerator` in `LamuFlix.Core/Ports` is explicitly mandated by ticket Scope 2 (`recon-DEV-17:84,106`), and extending Principle I/VII enumerations via ADR 0033 + MINOR `1.8.0` amendment + `PortWhitelistPattern` is the constitution's prescribed procedure (`.specify/memory/constitution.md:223,624-639`), not a departure or owner checkbox.
   - `Compass Low-2` & `Compass Info-2`: `recon-DEV-17-2:29-30` proves `.specify/memory/constitution.md` already has the 30-line `1.8.0` working-tree modification (`M`) and `docs/adr/0033-thumbnail-generator-port.md` exists untracked (`??`), though neither is `git add`-staged yet. Reject adding a duplicate authoring task; update `spec.md`, `plan.md`, and `tasks.md` (`T002`) to say "present in the Phase A working tree" rather than "staged", and require `T002` / Phase A spec PR preparation to verify `git diff -- .specify/memory/constitution.md` and stage/commit both `.specify/memory/constitution.md` and `docs/adr/0033-thumbnail-generator-port.md` while leaving `harness.yml` unstaged (`recon-DEV-17-2:32`).

7. Task & Contract Clarifications (Ledger L1, Ledger L4, Compass Low-1, Compass Low-3, Sentry L1, Sentry L3, Sentry L5):
   - Verdict: ACCEPT.
   - `Ledger L1`: Remove `[P]` from `T020` and state in `T020`/`T022` that `IThumbnailGenerator` (`T020`) and `ArchitectureTests.PortWhitelistPattern` (`T022`) must be committed/verified together as an atomic buildable slice (`ArchitectureTests.cs:155-177`).
   - `Ledger L4`: Update `T024` so its completion check explicitly verifies `CONTEXT.md` contains the `Thumbnail` entry with `term`, `definition`, and `avoid` fields (`.specify/memory/constitution.md:408`).
   - `Compass Low-1`: Update `T060` to remove the unverified `-Threshold` parameter and instead invoke `./scripts/run-cyclomatic-complexity.ps1` (checking `-Help` if flags are needed, per `AGENTS.md` Verification commands) against the refactor threshold configured in `harness.yml` `gates.complexity.refactor`.
   - `Compass Low-3`: Clarify in `spec.md`, `data-model.md`, and `contracts/thumbnail-generator.md` that a non-whitespace `sourcePath` pointing to a missing or unreadable file is not pre-checked via `File.Exists` and surfaces through `ffprobe` as `ThumbnailResult.FailureReason == "ProbeFailed"`.
   - `Sentry L1`: Update `T040`/`T041` and `spec.md` FR-001/FR-002 to specify a single `RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*` layer in `docker/Dockerfile.api` before `USER app`, and require `T041` to capture the exact `ffmpeg -version` / `ffprobe -version` output alongside the image size delta for Rigger's YouTrack receipt.
   - `Sentry L3`: Clarify in `plan.md`, `spec.md`, `contracts/thumbnail-generator.md`, and `docs/adr/0033-thumbnail-generator-port.md` that `ProcessTimeoutSeconds` (default 30s) applies per `IProcessRunner.RunAsync` call (`ffprobe` and `ffmpeg` separately), so worst-case single-generation wall-clock duration is up to `2 * ProcessTimeoutSeconds` plus process-tree termination and file-cleanup overhead.
   - `Sentry L5`: Clarify in `spec.md`, `contracts/thumbnail-generator.md`, and `docs/adr/0033-thumbnail-generator-port.md` that `ThumbnailOptions.FfmpegPath` and `CacheDirectory` are operator-controlled trusted configuration settings, never populated from untrusted request/user input.

8. JPEG SOI Validation & Phase B Task Ordering (Sentry L6, Sentry L8):
   - Verdict: ACCEPT.
   - `Sentry L6`: Preserve `brief.md:42` post-exit validation (`File.Exists`, `Length >= 2`, SOI bytes `0xFF, 0xD8`) paired with `T051` real-tool JPEG proof; no image decoder library is added.
   - `Sentry L8`: Preserve `brief.md:107` / `Q9` task ordering requiring `T040` (`Dockerfile.api` tooling) before `T050`/`T051` real-fixture execution.

9. Out-of-Scope & Pre-Existing Anomalies Routing (Ledger L3, Compass Info-1, Sentry M2, Sentry M3, Sentry L4):
   - Verdict: ACCEPT.
   - Keep DEV-17 scope frozen (`brief.md:130`).
   - Document in `plan.md`, `contracts/thumbnail-generator.md`, and `docs/adr/0033-thumbnail-generator-port.md` that cache retention/eviction (`Sentry M2`), queue/endpoint concurrency throttling (`Sentry M3`), and library-root source-path authorization (`Sentry L4`) are consumer responsibilities owned by `DEV-428` (`recon-DEV-17:80,99-101`); Patron will route a comment to Rigger to record those three constraints on `DEV-428` in YouTrack (no new ticket).
   - Pre-existing duplicate ADR `0031` (`Ledger L3`, `Compass Info-1`; `recon-DEV-17:165-166`) and the pre-existing `constitution.md:246` vs `ArchitectureTests.cs:30-31` port-list mismatch (`IRecommendationProfileSource`, `IEnrichmentOutbox`; `recon-DEV-17-2:31`) are Low/Info pre-existing repo hygiene observations, not Critical/High or broken behaviour: per Charter §2.3 ("anything else is one line in `CONCLUSIONS.md`: `noted, no ticket`"), both are recorded in `CONCLUSIONS.md` as `noted, no ticket` with zero changes to unrelated ADRs or unrelated port entries in DEV-17.

10. UTF-8 Encoding Proof & ADR 0033 Editorial Cleanup (Ledger L5, Ledger L2):
    - Verdict: ACCEPT.
    - `Ledger L5`: REJECT — byte-level inspection (`recon-DEV-17-2:35-40`) proves all 14 files under `specs/DEV-17/` and `docs/adr/0033-thumbnail-generator-port.md` are valid strict UTF-8 without BOM or NUL bytes; no encoding defect exists.
    - `Ledger L2`: AUTHORIZE narrow editorial cleanup of `docs/adr/0033-thumbnail-generator-port.md` (and `specs/DEV-17/brief.md` if needed for consistency) to replace ephemeral canvas-note citations (`recon-DEV-17:...`) with durable repository/ticket references (`DEV-17 Scope 1–5`, `AC 1–4`, and repo file paths) and to incorporate the closed failure-token list, per-process timeout note, `TimeProvider` runner seam, `pwsh` fixture test placement, and `DEV-428` consumer boundaries from items 2, 3, 4, 7, and 9 above.


## PLAN FROZEN — Phase 2 receipt

Date: 2026-10-30. Signed: Keel.
Worktree: `F:/Dev/LamuFlix.worktrees/feature-017-spec`; verified branch: `feature/017-spec`.

**PLAN FROZEN.** Authority: the final adjudication in canvas note `adjudication-DEV-17-plan` (final dispositions and independent passage verification), together with this brief and its binding Phase 2 corrections. This receipt supersedes earlier statements that plan freeze remains pending; historical grill and adjudication records remain intact.

- Plan challenge: CLEAN; Sentry, Ledger and Compass present; all 30 entries explicitly dispositioned.
- Unresolved plan-challenge findings: 0. Accepted residual risks and pre-existing observations have explicit dispositions and are not open repairs.
- Ticket contradictions: 0.
- Constitution departures: 0. The authorized MINOR enumeration amendment follows the prescribed procedure.
- Owner structural checkboxes: 0.
- Repair round cap: 2/2 consumed; final bounded repairs independently passage-verified, with no third round.

Frozen scope, contracts, file envelope, test strategy, gate expectations and task ordering are those in this brief as superseded by the recorded Phase 2 Patron rulings and incorporated into the adjudicated plan/tasks. Hosted ffmpeg/ffprobe availability remains unproved and blocking at Phase B T010/T061; dependency landing and pickup drift still require their specified checks.

This is plan freeze only. Patron provisional Gate 1, spec PR preparation, owner Gate 1/merge, implementation, runtime acceptance and delivery gates remain separate. No stage, commit, push, test/gate execution or merge is asserted. Preserve the unrelated harness.yml modification outside DEV-17 scope.