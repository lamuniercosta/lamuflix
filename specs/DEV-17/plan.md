# Implementation Plan: DEV-17 — Thumbnail generation at 10 percent offset

**Branch**: `feature/017-spec` | **Date**: 2026-10-30 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`; `brief.md` (grill closed Q10 + Phase 2 plan challenge round 1); `CONCLUSIONS.md` Q2–Q10 and Phase 2 plan challenge round 1; `ASSUMPTIONS.md`; note `recon-DEV-17` / `recon-DEV-17-2`; `specs/PRODUCT.md`; ADR `docs/adr/0033-thumbnail-generator-port.md`; constitution 1.8.0.

## Summary

Install ffmpeg/ffprobe in the Api runtime image and deliver a Core `IThumbnailGenerator` port with process-free `ThumbnailResult`, Core `ThumbnailOptions` (Enabled default false), and Infrastructure `FfmpegThumbnailGenerator` / bounded `IProcessRunner` under `Thumbnails`. Extraction seeks to 10 percent of duration, writes a validated JPEG into a generator-owned cache path, and returns failed results for operational errors. Composition is independent of `Features:LocalPlay`. Consumer endpoint DEV-428 and tile DEV-429 stay out of scope. Supporting detail: [research.md](research.md), [data-model.md](data-model.md), [contracts/thumbnail-generator.md](contracts/thumbnail-generator.md), [quickstart.md](quickstart.md).

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: Existing ASP.NET Core / hosting / options stack only — including the already-present `Microsoft.Extensions.Hosting.Abstractions` reference at `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj:33` (`10.0.14` via CPM; provides `IHostEnvironment.ContentRootPath`; recon-DEV-17-2:15-18). OS package `ffmpeg` (ships `ffprobe`) in `docker/Dockerfile.api` via apt — ticket-approved; no new NuGet/npm dependency (brief Q5/Q8; PRODUCT.md §5 item 1).

**Storage**: Filesystem JPEG cache under configured CacheDirectory; no DB schema change. Cache retention/eviction is owned by consumer ticket DEV-428 (not implemented here).

**Testing**: Existing xUnit v3, NSubstitute, Shouldly, FsCheck (+ FsCheck.Xunit.v3). Fake-runner unit tests with tiny temporary byte stubs remain in UnitTests; deterministic `SystemProcessRunner` lifecycle tests live in `tests/LamuFlix.IntegrationTests/Thumbnails/` and invoke `pwsh -NoProfile -NonInteractive -File` fixture scripts under `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/` (EvaluationCommand-style; recon-DEV-17-2:21-25); RequiresFfmpeg integration fixture test. No second test framework; no new test project; no xunit executable reentry / custom Main. No mandatory new FsCheck generator suite for `FfmpegThumbnailGenerator`.

**Target Platform**: Api host (Worker retired); Linux container runtime image `mcr.microsoft.com/dotnet/aspnet:10.0`. Hosted CI `ubuntu-latest` `ffmpeg`/`ffprobe` availability is unproved in repo roots (`recon-DEV-17-2:9-12`); plan/tasks MUST NOT claim hosted tools are already proven. Full `dotnet test` including `[Trait("Category", "RequiresFfmpeg")]` retains fail-if-missing semantics; no skip/filter/workflow edits. Phase B `T010` (pre-coding) and `T061` (pre-PR) must prove hosted runner tool availability and stop for bounded recon + Patron if absent.

**Project Type**: Backend vertical capability (Core port + Infrastructure adapter + Docker image) with no public HTTP surface in this ticket.

**Performance Goals**: `ProcessTimeoutSeconds` (default 30) bounds each `IProcessRunner.RunAsync` invocation individually (`ffprobe` then `ffmpeg`); worst-case single-generation wall-clock is up to `2 * ProcessTimeoutSeconds` plus process-tree termination and file-cleanup overhead. ≤64 KiB retained stdout/stderr per stream with drain-to-EOF; no numeric throughput SLA in the ticket. Consumer concurrency throttling belongs to DEV-428.

**Constraints**: Q8 file envelope as corrected by Phase 2 plan challenge round 1; ArgumentList only (`UseShellExecute=false`); Core process-API ban; LocalPlay playback gate unchanged; no CI/compose/Playback/Directory.Packages.props/generated-file edits; preserve pre-existing `harness.yml` local modification (untouched/unstaged); no mutation `--since` from worktrees; do not lower harness thresholds. Library-root source-path authorization is owned by DEV-428.

**Scale/Scope**: Eight new production files under Core/Ports, Core/Options, Infrastructure/Thumbnails (`IProcessRunner` co-located in `SystemProcessRunner.cs`; no standalone interface file — brief Phase 2 correction 1); narrow wiring edits listed in brief including Phase B `CONTEXT.md` Thumbnail reconciliation (correction 2); focused unit/integration tests; ADR 0033 + constitution 1.8.0 present in the Phase A working tree (not merely "staged"); size-L three-axis review required.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I Ports and Adapters**: PASS — ticket-mandated Core `IThumbnailGenerator` + process-free result; Infrastructure adapter/runner; non-port `IProcessRunner` co-located with `SystemProcessRunner` (constitution:560–561; brief Phase 2 correction 1); ADR 0033 + MINOR 1.8.0 enumeration extension + architecture whitelist is the prescribed constitutional procedure (`.specify/memory/constitution.md:223,624-639`; recon-DEV-17:84,106), not a departure or owner checkbox (Q3). No new project/layer. No duplicate amendment task.
- **VI Observability / health**: N/A under Patron applicability ruling (CONCLUSIONS Phase 2 analyze item 3; brief Phase 2 correction 3; constitution:348–365,494–495) — local in-image ffmpeg/ffprobe CLI invoked as a child process (analogous to media-player process boundary), not a networked/backing service; Enabled defaults false; missing tools are generation-time failed results; queue consumer/endpoint remain DEV-428. No health-check or TelemetryConstants files; no constitution departure or owner structural checkbox.
- **VII Configuration Isolation**: PASS — `ThumbnailOptions` in Core/Options with conditional validator and ValidateOnStart binding via ServiceDefaults; Enabled default false; no secrets. `FfmpegPath` and `CacheDirectory` are operator-controlled trusted configuration, never populated from untrusted user/API input.
- **VIII Ubiquitous language**: PASS design intent — Phase B task reconciles/adds `Thumbnail` in `CONTEXT.md` (term/definition/avoid) in the introducing PR; reconcile Principle VIII summary list only if needed (constitution:393,408,500,613–614; brief Phase 2 correction 2). No assumption about existing glossary contents.
- **Process / LocalPlay**: PASS — dedicated bounded runner (Q2); extraction independent of LocalPlay; existing playback gating intact; Charter 2.3 item 5 approved.
- **IX Testing / mutation**: PASS design intent — fake-runner UnitTests + IntegrationTests `pwsh` fixture runner proofs + RequiresFfmpeg fixture; eligible changed Core types subject to mutation ≥ harness threshold; Infrastructure/Api exclusions remain NOT APPLICABLE for those projects only; no mandatory new FsCheck generator suite for `FfmpegThumbnailGenerator`.
- **Static-analysis gates**: PASS design intent — Roslyn, cyclomatic complexity (implement and refactor thresholds from `harness.yml`), InspectCode, property tests, vulnerable packages, format — thresholds from `harness.yml`, never hardcoded or lowered. Refactor invocation uses `./scripts/run-cyclomatic-complexity.ps1` with `-Help`-documented parameters against the configured `gates.complexity.refactor` value (no assumed `-Threshold` literal in tasks).

No violations; Complexity Tracking empty. This is design compliance only — no delivery gate pass is claimed here.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-17/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── clarifications.md
├── checklists/requirements.md
├── spec.md
├── plan.md              # This file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/thumbnail-generator.md
└── tasks.md
```

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/
├── IThumbnailGenerator.cs
└── ThumbnailResult.cs
src/LamuFlix.Core/Options/
├── ThumbnailOptions.cs
└── ThumbnailOptionsValidator.cs
src/LamuFlix.Infrastructure/Thumbnails/
├── ProcessResult.cs
├── SystemProcessRunner.cs                                 # defines IProcessRunner + SystemProcessRunner (co-located; injected TimeProvider)
├── FfmpegThumbnailGenerator.cs
└── ThumbnailServiceCollectionExtensions.cs                # resolves TimeProvider from DI with System fallback

docker/Dockerfile.api                                    # single RUN apt update/install/clear before USER app
src/LamuFlix.ServiceDefaults/Extensions.cs               # bind + conditional validator
src/LamuFlix.Api/Program.cs                              # AddLamuFlixThumbnails
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs   # PortWhitelistPattern
tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs
tests/LamuFlix.UnitTests/Options/                          # ThumbnailOptions unit/property/survivor
tests/LamuFlix.UnitTests/Thumbnails/                       # fake-runner + composition (tiny temp byte stubs only)
tests/LamuFlix.IntegrationTests/Thumbnails/                # SystemProcessRunnerTests (pwsh fixtures)
tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/       # short-video.mp4 + pwsh fixture scripts
CONTEXT.md                                                 # Phase B Thumbnail term reconcile/add (brief Phase 2 correction 2)
.specify/memory/constitution.md                            # 1.8.0 present in Phase A working tree; optional Principle VIII summary sync if needed
docs/adr/0033-thumbnail-generator-port.md                  # present in Phase A working tree (Keel owns editorial edits)
```

**Structure Decision**: Follow existing Core port / Infrastructure adapter / Core options layout. Eight new production files (no standalone `IProcessRunner.cs`). No new project, top-level folder, or architectural layer. No health-check or TelemetryConstants files (VI N/A). `pwsh` fixture scripts and fixture copy-to-output stay inside existing IntegrationTests project/csproj edits only.

## Delivery Phases and Dependencies (brief Q9)

### Phase A (Spec workspace — this draft)

1. ADR number reserved (0033) and focused ADR + constitution 1.8.0 present in the Phase A working tree by Keel (complete as of note DEV-17; not a duplicate amendment task).
2. Quill drafts `spec.md` / `plan.md` / `tasks.md` (+ clarify/checklist supporting docs) from brief only.
3. Read-only `/speckit-analyze` against brief; L-tier plan challenge (Sentry, Ledger, Compass) and adjudication.
4. Plan freeze → Patron provisional Gate 1 → Rigger opens spec PR including authorized inclusion of `.specify/memory/constitution.md` and `docs/adr/0033-thumbnail-generator-port.md` after `git diff` / `git status` review (`T002`); `harness.yml` stays untouched/unstaged → owner merges.
5. No production code, test execution, or gate claims in Phase A.

### Phase B (Build workspace — after Gate 1)

1. Verify DEV-304 and DEV-314 landing/baseline alignment; reread main for pickup drift; re-run cross-artifact analysis before coding (recon:57 is not landing proof). Explicit hosted-runner `ffmpeg`/`ffprobe` availability proof blocker in `T010` (no skip/filter/workflow changes; stop for bounded recon + Patron if unavailable).
2. Establish governance/port/options and architecture whitelist before Infrastructure runner/generator/composition; pair code with focused tests.
3. Install image tools in Dockerfile before Docker acceptance and RequiresFfmpeg execution (`T040` before `T050`/`T051`).
4. Refactor under configured complexity gate; full verify including pre-PR hosted-tool availability proof in `T061`; three-axis review/ship-review; Rigger size-recording receipt before merge-bar evidence is signed.
5. Owner merges every PR; no agent merge.

## Closing Bar and Merge Bar (brief Q10)

- **Closing bar**: Zero unresolved Critical/High/Medium findings; every Low has an explicit recorded disposition; all required gates and acceptance receipts earned against the same HEAD; complete Sentry, Ledger, and Compass reports. Green hosted checks alone do not satisfy this bar.
- **Frozen scope**: Ticket API-image ffmpeg/ffprobe install; Core thumbnail port/result/options/validator; focused ADR and permitted constitution/whitelist extension; Infrastructure process runner/generator/DI with co-located `IProcessRunner`; Phase B `CONTEXT.md` Thumbnail reconciliation (optional Principle VIII summary sync); narrowly approved wiring; focused tests; app-user, real-fixture, and image-size proof. Consumer/endpoint DEV-428 (including cache retention/eviction, queue/endpoint concurrency throttling, and library-root source-path authorization), tile DEV-429, no-video support, DB/public API, Playback, health-check/TelemetryConstants files, unrelated refactors, new packages/projects, and CI/compose changes remain excluded; anything else is a follow-up issue, not a finding in this round.
- **Round cap**: Two review/remediation rounds maximum. A remaining blocker at cap is reported BLOCKED with evidence; no third round, threshold reduction, or gate bypass.
- **Gate status vocabulary**: Exit 0 = PASS; scope-empty exit 2 = `SKIPPED (scope-empty)`; configured opt-out = `SKIP`; mutation exclusion = N/A/NOT APPLICABLE; unrunnable = `Could not run`. None of SKIPPED/SKIP/N/A/Could not run is PASS. A failed required gate blocks completion.

## Consumer boundaries (DEV-428)

Documented design constraints only — no DEV-17 implementation expansion:

- Cache retention / eviction
- Queue / endpoint concurrency throttling
- Library-root source-path authorization

Patron routes a verified comment to Rigger on `DEV-428` (no new ticket). Pre-existing ADR `0031` duplicate and constitution vs ArchitectureTests port-list mismatch are already `noted, no ticket` in CONCLUSIONS.md; DEV-17 makes no unrelated ADR/port corrections.

## Complexity Tracking

None — no constitution violations to justify.
