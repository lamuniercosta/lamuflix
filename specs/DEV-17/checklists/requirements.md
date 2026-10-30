# Requirements Checklist: DEV-17

**Purpose**: Verify every ticket obligation and grill ruling is covered by spec, plan, and tasks before the spec PR.
**Created**: 2026-10-30
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] CHK001 Spec states WHAT/WHY with ticket-backed contracts; implementation plan carries HOW.
- [x] CHK002 All mandatory spec sections completed (scenarios, requirements, success criteria, assumptions).
- [x] CHK003 No `[NEEDS CLARIFICATION]` markers remain in `spec.md`.
- [x] CHK004 Assumptions section points at `ASSUMPTIONS.md` Q5–Q7 + Phase 2 plan challenge round 1 rather than inventing new defaults.

## Scope & obligations

- [x] CHK005 Scope 1 (Dockerfile.api single RUN apt update/install -y --no-install-recommends/clear before USER app + exact version outputs + size recording) maps to US1 / FR-001 / FR-002 / SC-001 / T040–T041.
- [x] CHK006 Scope 2–5 (Core port, Infrastructure generator/runner with TimeProvider, options Enabled=false, closed failed-result tokens) map to US2 / FR-003–FR-008.
- [x] CHK007 AC2 fake-runner fractional seek (`125.5` → `"12.55"` comma culture) maps to US3 / FR-009 / SC-002 / T034.
- [x] CHK008 AC4 missing-binary failed-result maps to US3 / FR-005 / FR-009 / SC-004.
- [x] CHK009 AC3 RequiresFfmpeg fixture JPEG (SOI Length ≥ 2) maps to US4 / FR-010 / SC-003; Docker-before-fixture ordering preserved.
- [x] CHK010 Out of scope (DEV-428 incl. retention/concurrency/library-root auth, DEV-429, no-video, DB, public API, Playback, packages, CI/compose) stated in spec and plan frozen scope.

## Proof discipline

- [x] CHK011 Fake-runner unit tests never require ffmpeg or real media; tiny temp bytes only when validating output (UnitTests/Thumbnails).
- [x] CHK012 SystemProcessRunner deterministic tests use EvaluationCommand-style `pwsh -NoProfile -NonInteractive -File` fixtures in IntegrationTests/Fixtures/Thumbnails with narrow csproj copy entries (no xunit reentry/custom Main/new project/package; missing pwsh fails).
- [x] CHK013 RequiresFfmpeg missing-tools disposition is fail, never silent skip-as-passed; full suite fail-if-missing remains; hosted CI availability unproved — T010/T061 proof blockers, no skip/filter/workflow edits.
- [x] CHK014 Docker app-user exact version proof and size-delta receipt via Rigger; no CI workflow edits.

## Envelope & gates

- [x] CHK015 New production file list is eight files with co-located `IProcessRunner`; narrow wiring + Phase B `CONTEXT.md` match brief Q8 + Phase 2 corrections; harness.yml pre-existing change preserved/untouched/unstaged, not claimed as this work.
- [x] CHK016 ADR 0033 and constitution 1.8.0 referenced as present in Phase A working tree; T002 requires diff/status review + authorized Rigger inclusion; Quill did not invent the ADR number or edit brief/ADR/constitution/CONCLUSIONS/ASSUMPTIONS; Keel owns ADR0033 editorial.
- [x] CHK017 Gate vocabulary correct (SKIPPED/SKIP/N/A/Could not run never PASS); Core mutation required; Infrastructure/Api exclusion N/A only for those projects; T060 uses `-Help`-documented invocation against configured `gates.complexity.refactor` (no assumed `-Threshold`); no mandatory new FsCheck generator suite.
- [x] CHK018 Ordering: Phase A docs → analyze/challenge → Gate 1; Phase B drift + hosted-tool proof; T020+T022 atomic; port/options before adapter; T040 before real-fixture/RequiresFfmpeg execution; T024 term/definition/avoid check; two-round review cap.
- [x] CHK019 No new NuGet/npm dependency, project, schema, or public API grant anywhere in spec/plan/tasks; Hosting.Abstractions already at Infrastructure csproj:33.
- [x] CHK020 Clarifications cite existing Patron rulings only; DRAFTING_RECEIPT has no open `needs decision` lines after Phase 2 plan challenge round 1 sync.
- [x] CHK021 Constitution Check records Principle VI health/telemetry N/A (local in-image CLI) and Principle VIII CONTEXT.md Thumbnail task; no health-check/TelemetryConstants files; no owner structural checkbox; Q3 enumeration extension is procedure not departure.
- [x] CHK022 Docker quickstart uses `--entrypoint ffmpeg` / `--entrypoint ffprobe` with `--user app` and `-version` after the image.
- [x] CHK023 Closed ProcessResult/ThumbnailResult token catalogues with precedence and mapping present in spec/contracts/data-model; per-call timeout (2× + cleanup) and trusted operator FfmpegPath/CacheDirectory documented; missing source via ProbeFailed without File.Exists; UTF-8 proven (no rewrite); DEV-428 consumer boundaries documented without implementation expansion.

## Notes

- Check items use `[x]` when the drafted artifacts cover the item. Unchecked at spec-PR time becomes `needs decision` to Keel, not a silent assumption.
- This checklist validates requirements writing quality for Gate 1 readiness; it does not claim implementation or gate execution.
