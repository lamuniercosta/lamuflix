---
description: "Ordered implementation tasks for DEV-17"
---

# Tasks: DEV-17 — Thumbnail generation at 10 percent offset

**Input**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/thumbnail-generator.md`, `brief.md` Q8–Q10 + Phase 2 analyze decision corrections + Phase 2 plan challenge round 1, `ASSUMPTIONS.md`.

**Prerequisites**: Gate 1 (owner merges the Phase A spec PR) before any Phase B coding task. ADR 0033 and constitution 1.8.0 are present in the Phase A working tree; Phase B extends the architecture whitelist with the port and reconciles `CONTEXT.md` Thumbnail language. Hosted CI `ffmpeg`/`ffprobe` availability is unproved — `T010`/`T061` prove it without skip/filter/workflow edits.

**Tests**: Required (ticket AC2–AC4 + Q7 as corrected by Phase 2 plan challenge round 1). Prefer tests paired with or ahead of production code for each slice. RequiresFfmpeg must fail when tools are missing. Full suite fail-if-missing remains.

**Organization**: Phase A close-out, then Phase B drift foundation, then stories in priority order per brief Q9 (port/options/whitelist before Infrastructure; Dockerfile before Docker/RequiresFfmpeg acceptance).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US4 mapping to `spec.md`

## Phase 1: Setup (Phase A close-out)

**Purpose**: Spec workspace artifacts ready for analyze / challenge / Gate 1.

- [ ] T001 Quill artifacts complete and consistent with brief Q2–Q10 + Phase 2 plan challenge round 1: `spec.md`, `plan.md`, `tasks.md`, `clarifications.md`, `checklists/requirements.md`, `research.md`, `data-model.md`, `contracts/thumbnail-generator.md`, `quickstart.md`, `DRAFTING_RECEIPT.md`.
- [ ] T002 Keel read-only `/speckit-analyze` against `brief.md`; L-tier plan challenge (Sentry, Ledger, Compass) + adjudication; plan freeze; Patron provisional Gate 1; Rigger opens spec PR after verifying `git diff -- .specify/memory/constitution.md` and `git status` for constitution + ADR 0033 present in the Phase A working tree, and authorized inclusion of both `.specify/memory/constitution.md` and `docs/adr/0033-thumbnail-generator-port.md` in the Phase A commit while leaving `harness.yml` untouched/unstaged; owner merges (Gate 1). No duplicate amendment task or owner structural checkbox.

**Checkpoint**: Gate 1 open. No production code, test execution, or gate claims before this checkpoint.

---

## Phase 2: Foundational (drift — BLOCKS coding)

**Purpose**: Confirm dependencies and zero drift before implementation.

**⚠️ CRITICAL**: No user-story coding until this phase completes.

- [ ] T010 Fresh recon and drift-against-main evidence after Gate 1; verify DEV-304 and DEV-314 landing/baseline alignment; re-run cross-artifact analysis; reconcile drift against `brief.md` and constitution 1.8.0 before coding (recon:57 is not landing proof). Explicit pre-coding hosted-runner `ffmpeg`/`ffprobe` availability proof blocker: verify tools are available on hosted `ubuntu-latest` CI (or equivalent path used by `.github/workflows/ci.yml`); MUST NOT claim availability is already proven; MUST NOT add test skips, category filters, or workflow edits; if hosted tools are unavailable, stop for bounded recon + Patron ruling. Full suite / RequiresFfmpeg fail-if-missing remains.

**Checkpoint**: Drift clear; hosted-tool availability disposition recorded; build worktree ready.

---

## Phase 3: User Story 2 foundation — Core port, options, whitelist (Priority: P1)

**Goal**: Process-free Core contracts and options exist and pass architecture whitelist before any Infrastructure adapter.

**Independent Test**: ArchitectureTests whitelist includes `IThumbnailGenerator`; options bind with Enabled=false defaults; Core still has no process API references.

- [X] T020 [US2] Add `src/LamuFlix.Core/Ports/IThumbnailGenerator.cs` and `ThumbnailResult.cs` per FR-003 / contract (Q4). Must be committed/verified together with T022 as an atomic buildable slice (`ArchitectureTests.cs:155-177`).
- [X] T021 [P] [US2] Add `src/LamuFlix.Core/Options/ThumbnailOptions.cs` and `ThumbnailOptionsValidator.cs` per FR-007 (Q5 + ASSUMPTIONS).
- [X] T022 [US2] Extend `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` `PortWhitelistPattern` with `IThumbnailGenerator` (Q3/Q8). Atomic with T020 — do not land the port without the whitelist entry (or vice versa).
- [X] T023 [US2] Wire bind + conditional validator in `src/LamuFlix.ServiceDefaults/Extensions.cs`; extend `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` and add ThumbnailOptions unit/property/survivor tests under `tests/LamuFlix.UnitTests/Options/` (FR-007–FR-009).
- [X] T024 [US2] Reconcile/add `Thumbnail` in `CONTEXT.md` in the introducing PR; completion check MUST verify the entry contains `term`, `definition`, and `avoid` fields/columns per `.specify/memory/constitution.md:408`; reconcile Principle VIII summary list in `.specify/memory/constitution.md` only if needed; do not assume existing glossary contents (FR-011; brief Phase 2 correction 2).

**Checkpoint**: Core port/options + whitelist + options startup coverage compile; CONTEXT.md Thumbnail language tasked; no Infrastructure Thumbnails yet required for this checkpoint beyond compile references if any.

---

## Phase 4: User Story 2 + 3 — Runner, generator, composition, fake-runner proofs (Priority: P1)

**Goal**: Infrastructure runner/generator registered in Api; fake-runner UnitTests and IntegrationTests `pwsh` fixture runner proofs cover seek, failures, and LocalPlay independence.

**Independent Test**: UnitTests under Thumbnails/Options pass without ffmpeg; IntegrationTests/Thumbnails runner proofs pass when `pwsh` is present (fail loudly if missing); missing-binary and 10% seek assertions green; composition available with LocalPlay false.

- [X] T030 [P] [US2] Add `src/LamuFlix.Infrastructure/Thumbnails/ProcessResult.cs` and define non-port `IProcessRunner` in `SystemProcessRunner.cs` alongside its primary implementation (no standalone `IProcessRunner.cs`) per FR-005 (Q6; brief Phase 2 correction 1; constitution:560–561). Closed `ProcessResult.FailureReason` token set per contract (LaunchFailed / TimedOut / OutputTruncated / NonZeroExit).
- [X] T031 [US2] Implement `SystemProcessRunner` in `src/LamuFlix.Infrastructure/Thumbnails/SystemProcessRunner.cs`: ArgumentList, no shell, drain, 64 KiB cap, tree kill, cancellation semantics; accept injected `TimeProvider` (default `TimeProvider.System` when not supplied) and drive per-process timeout via `new CancellationTokenSource(timeout, timeProvider)` without `DateTime.Now` or new packages (FR-005; Phase 2 plan challenge round 1 item 2).
- [X] T032 [US2] Implement `src/LamuFlix.Infrastructure/Thumbnails/FfmpegThumbnailGenerator.cs` (probe, 10% seek, JPEG ownership/validation with Length ≥ 2 + SOI `0xFF, 0xD8`, Disabled short-circuit before validation/process/disk, closed `ThumbnailResult.FailureReason` precedence per contract, failed results) (Q4/Q6).
- [X] T033 [US2] Add `src/LamuFlix.Infrastructure/Thumbnails/ThumbnailServiceCollectionExtensions.cs` and call `AddLamuFlixThumbnails` from `src/LamuFlix.Api/Program.cs`; resolve `TimeProvider` from `IServiceProvider` with `TimeProvider.System` fallback (FR-008; Phase 2 plan challenge round 1 item 2).
- [X] T034 [P] [US3] Fake-runner generator tests in `tests/LamuFlix.UnitTests/Thumbnails/` (tiny temporary byte stubs only for post-exit JPEG SOI/cleanup; no filesystem abstraction): fractional non-integer duration `125.5` → seek `"12.55"` under comma-decimal culture (`fr-FR` or `de-DE`); exact discrete `ArgumentList` sequence including `-nostdin`, `-y`, `-ss`, `-i`, `-map`, `0:v:0`, `-frames:v`, `1`, `-vf`, and test-example `scale=min(320\,iw):-2` under default MaxWidth `320` (production substitutes configured MaxWidth into `scale=min(<MaxWidth>\,iw):-2`; `320` is default/test example only, not a fixed runtime width); spaced paths as separate entries; SOI boundary 0-byte / 1-byte (`0xFF`) / reversed (`0xD8, 0xFF`) fail and `>= 2`-byte `0xFF, 0xD8` prefix success; probe failures, Disabled no-work, nonzero exit, timeout, missing binary without throw, invalid/empty JPEG and cleanup (FR-009; SC-002; SC-004). No mandatory new FsCheck generator suite.
- [X] T035 [P] [US3] Deterministic `SystemProcessRunnerTests` in `tests/LamuFlix.IntegrationTests/Thumbnails/` using EvaluationCommand-style `pwsh -NoProfile -NonInteractive -File <fixture.ps1>` against committed scripts under `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/`, copied via narrow existing `LamuFlix.IntegrationTests.csproj` `None`/`CopyToOutputDirectory` entries; no xunit executable reentry, custom Main, new project, or new package; missing `pwsh` fails loudly. Cover: ordinary success/capture; missing binary; nonzero exit; one-stream exact-cap boundary (`== 65536` succeeds; `65537` fails with `OutputTruncated` while draining to EOF); simultaneous stdout+stderr both over the 64 KiB cap with complete drain, no deadlock, and `OutputTruncated`; timeout tree cleanup; caller cancellation; controlled timeout via injected `TimeProvider` without new packages (FR-009; original Q7 simultaneous over-cap proof retained alongside exact-cap boundaries).
- [X] T036 [US2] Composition tests proving registrations, no eager subprocess/directory work at startup, and thumbnail availability independent of `Features:LocalPlay` (FR-008).

**Checkpoint**: US2/US3 unit and composition proofs pass without requiring Docker or real ffmpeg for the fake-runner suite; AC2 and AC4 covered.

---

## Phase 5: User Story 1 — Docker image tooling (Priority: P1)

**Goal**: Runtime image installs ffmpeg/ffprobe before `USER app`.

**Independent Test**: `docker build` of `docker/Dockerfile.api`; both `-version` commands succeed as `app`; exact version outputs and size delta captured for Rigger.

- [ ] T040 [US1] Edit `docker/Dockerfile.api` runtime stage: single `RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*` layer as root before `USER app` (FR-001; recon:83,105).
- [ ] T041 [US1] Build image; run `ffmpeg -version` and `ffprobe -version` as `app`; capture the exact version command outputs alongside the image size increase vs comparable baseline; route verified receipt to Rigger for YouTrack DEV-17 recording (FR-002; SC-001). No CI workflow edits.

**Checkpoint**: AC1 and Scope 1 image-size recording satisfied (Rigger owns tracker write).

---

## Phase 6: User Story 4 — Real-tool fixture proof (Priority: P2)

**Goal**: Committed synthetic clip + RequiresFfmpeg integration test. Preserve SOI + real-fixture proof; Docker tooling (`T040`) before fixture execution (`T050`/`T051`).

**Independent Test**: Category RequiresFfmpeg test writes JPEG with width ≤ MaxWidth; fails if tools missing.

- [ ] T050 [US4] Commit tiny synthetic fixture at `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/short-video.mp4`; adjust IntegrationTests csproj for copy-to-output if needed (no new packages) (FR-010; ASSUMPTIONS Q7). May share the same Fixtures/Thumbnails tree as T035 `pwsh` scripts.
- [ ] T051 [US4] Add `[Trait("Category", "RequiresFfmpeg")]` integration test using enabled options + temp cache; assert Success, valid JPEG (Length ≥ 2 + SOI `0xFF, 0xD8`), width ≤ MaxWidth; missing ffmpeg/ffprobe fails the test (FR-010; SC-003). Runs only after T040.

**Checkpoint**: AC3 earned when tools present; missing tools do not count as acceptance passed.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Refactor, full gates, three-axis review, merge bar.

- [ ] T060 Refactor changed code only; invoke `./scripts/run-cyclomatic-complexity.ps1` using `-Help`-documented parameters against the refactor threshold configured in `harness.yml` `gates.complexity.refactor` (do not assume an unverified `-Threshold` flag or pin a numeric override in this task); no scope expansion.
- [ ] T061 Pre-PR hosted-runner `ffmpeg`/`ffprobe` availability proof blocker (same rules as T010: no skip/filter/workflow edits; stop for bounded recon + Patron if unavailable). Then run required gates on diff/head: Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, full `dotnet test` (including ArchitectureTests and RequiresFfmpeg fail-if-missing), format/diff, mutation of eligible changed Core (no `--since`; Infrastructure mutation N/A unchanged). Record SKIPPED/SKIP/N/A/Could not run distinctly from PASS (SC-005; brief Q9).
- [ ] T062 Three-axis review (Sentry, Ledger, Compass) with findings and summary as PR comments; remediate within two-round cap per closing bar (brief Q10).
- [ ] T063 Confirm Rigger size-increase receipt on DEV-17; same-HEAD evidence for gates/reviews; rebase/push via authorized seat; ship-review; end at awaiting-merge — owner merges.

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 → Gate 1 → Phase 2 (T010) BLOCKS Phases 3–6.
- Phase 3 (Core/options/whitelist) before Phase 4 (Infrastructure/composition/tests) per brief Q9.
- Phase 5 T040 (Dockerfile image tooling) before Phase 6 real-fixture / RequiresFfmpeg execution (brief:107; Q9; Phase 2 plan challenge round 1 item 8). Host-PATH RequiresFfmpeg remains valid after T040 when tools exist locally (recon:16); image AC (T041) remains mandatory separately.
- Phase 7 after desired stories complete.

### User Story Dependencies

- **US2 foundation (Phase 3)**: After T010; no dependency on Docker. T020 + T022 are an atomic buildable slice.
- **US2/US3 implementation (Phase 4)**: After Phase 3.
- **US1 (Phase 5)**: After T010; independent of Core code but required for AC1; ordered before merge-bar acceptance.
- **US4 (Phase 6)**: After Phase 4 generator exists and after T040; real-fixture execution may use host-PATH tools after T040 (recon:16) but must not precede Dockerfile tooling.

### Parallel Opportunities

- T021 may parallelize with T020/T022 atomic slice; T024 after Thumbnail term exists in Core artifacts (may follow T020); T030 parallel with early test scaffolding; T034/T035 in parallel after runner/generator interfaces exist.
- Do not parallelize conflicting edits to the same file (Program.cs, Extensions.cs, Dockerfile.api, ArchitectureTests.cs, CONTEXT.md).

---

## Coverage Map

| Spec key | Tasks |
| --- | --- |
| FR-001 | T040 |
| FR-002 | T041 |
| FR-003 | T020 |
| FR-004 | T030, T031, T032, T033 |
| FR-005 | T030, T031, T033, T035 |
| FR-006 | T032, T034 |
| FR-007 | T021, T023 |
| FR-008 | T022, T023, T033, T036 |
| FR-009 | T023, T034, T035, T036 |
| FR-010 | T050, T051 |
| FR-011 | T001, T002, T022 (whitelist), T024 (CONTEXT.md Thumbnail term/definition/avoid); ADR/constitution present in Phase A working tree; VI health/telemetry N/A |
| FR-012 | All phases — exclusions enforced (incl. no health-check/TelemetryConstants files; DEV-428 owns retention/concurrency/library-root auth) |
| SC-001 | T040, T041 |
| SC-002 | T034 |
| SC-003 | T050, T051 |
| SC-004 | T034, T035 |
| SC-005 | T060–T063 |

---

## Implementation Strategy

### MVP First (US2 + US3 after foundation)

1. Complete Phase 1–2 (Gate 1 + drift + hosted-tool availability proof).
2. Complete Phase 3–4 (Core contracts + Infrastructure + fake-runner / pwsh fixture proofs) → AC2/AC4.
3. Complete Phase 5 (Docker) → AC1 + size/version receipt.
4. Complete Phase 6 (RequiresFfmpeg after T040) → AC3.
5. Phase 7 gates/review/merge bar (incl. T061 hosted-tool re-proof).

### Notes

- Preserve pre-existing `harness.yml` local modification; do not treat it as DEV-17 scope; leave unstaged.
- Eight new production files only (co-located `IProcessRunner`); no health-check or TelemetryConstants files (brief Phase 2 correction 3).
- No Directory.Packages.props, docker-compose.yml, CI, Playback, or generated-file edits.
- Anything outside frozen scope is a follow-up issue, not a finding in this round.
