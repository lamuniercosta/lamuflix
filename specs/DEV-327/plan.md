# Implementation Plan: DEV-327 — CI container build validation, Stryker Core gate, property-test gate

**Branch**: `feature/DEV-327` (Phase B) | **Date**: 2026-10-13 | **Spec**: `specs/DEV-327/spec.md`

**Input**: `specs/DEV-327/spec.md`, `specs/DEV-327/brief.md` (frozen scope, Q1–Q9, Plan decisions, ordering constraints), recon `recon-DEV-327`, `recon-DEV-327-3`, Q2a (brief.md Plan decisions), `specs/PRODUCT.md`

## Summary

Extend `.github/workflows/ci.yml` with three separate jobs (container `docker build` validation, whole-Core Stryker at break 80, per-project property tests), backed by a new explicit whole-project mode in `scripts/run-mutation.ps1` plus `scripts/Test-RunMutation.ps1` coverage. Build-only containers for exactly the API and Web Dockerfiles (Q2a; DEV-21 prerequisite satisfied, recon-DEV-327-3); Q5 baseline measured before test-raising tasks are cut.

## Technical Context

**Language/Version**: C# on .NET 10 (`global.json`-pinned via `actions/setup-dotnet@v4`), PowerShell 7 gate scripts, YAML (GitHub Actions), `web/` Node per `web/.node-version`

**Primary Dependencies**: NONE ADDED. Runner `docker build` CLI only; `actions/upload-artifact@v4` (approved Q8, matches existing `@v4` pinning). No `docker/*` action, no NuGet/npm change (frozen scope).

**Storage**: N/A

**Testing**: xUnit (`Method_Condition_Expected`); `scripts/Test-RunMutation.ps1` for the new script mode; whole-Core Stryker score >= 80 as the Q5 proof; per-project `scripts/run-property-tests.ps1` invocations; CI run itself on the DEV-327 PR (no YAML unit tests; recon section 14 records 0 existing tests referencing ci.yml/Stryker/scripts).

**Target Platform**: GitHub-hosted `ubuntu-latest` CI; triggers push to main, pull_request to main, no path filters (brief Plan decisions).

**Project Type**: CI/workflow + test-gate wiring (backend repo infra; `ui: none`).

**Performance Goals**: Stryker runs as its own job so its minutes-scale duration never blocks the fast jobs' signal (brief Gate expectations).

**Constraints**: Break stays 80 (`harness.yml` `gates.mutation.threshold` / `stryker-config.json` break, both 80, recon sections 9–10); `since` never enabled (standing ruling); exit contracts unchanged (run-mutation 0/1/2, run-property-tests 0/1/2 with exit 2 failing CI); new jobs get no registry login, no secret beyond `GITHUB_TOKEN`, no third-party action besides `actions/upload-artifact@v4` (Q8).

**Scale/Scope**: M ticket. Files: `.github/workflows/ci.yml`, `scripts/run-mutation.ps1`, `scripts/Test-RunMutation.ps1`, plus conditional test files inside existing `LamuFlix.Test` / `LamuFlix.UnitTests` only under Q5. Nothing else without `needs decision:` to Keel; the four Q4-protected files additionally need a cited Patron ruling.

## Constitution Check

- TimeProvider / seeded Random (Principle VII; seeded Random per existing harness rule): unaffected (no production C# in this ticket except conditional Q5 test-raising, which follows existing conventions).
- No threshold lowered (Principle IX, constitution line 290; break 80 frozen); no generated file edited; no `dotnet build` / direct `dotnet stryker` substitute for a gate script (existing harness rules; Gate expectations; brief C1 decision).
- No second assertion/mocking library; no data-access driver mocking change (Principle IX); no new dependency, project, folder, schema, or API shape (Principles I and V; Technology Stack Constraints; FR-013; brief C1 decision).
- No `Features:LocalPlay`, secrets, or `Process.Start` touch (Principle VII) (recon section 15: 0 hits; new jobs introduce none).
- PASS — no violations to justify for the evaluated gates (Principles I, V, VII, IX; brief C1 decision).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-327/
├── brief.md              # Keel grill-closure brief (frozen input)
├── spec.md               # /speckit-specify output
├── plan.md               # This file (/speckit-plan output)
├── tasks.md              # /speckit-tasks output
├── CONCLUSIONS.md        # Patron Q1–Q9 rulings
└── ASSUMPTIONS.md        # 3 taste assumptions [assumed]
```

### Source Code (repository root)

```text
.github/workflows/ci.yml          # 3 new jobs added; build-test-format + web untouched
scripts/run-mutation.ps1           # new explicit whole-project mode for LamuFlix.Core
scripts/Test-RunMutation.ps1      # coverage for the new mode + unchanged-contract asserts
tests/LamuFlix.Test/              # conditional Q5 test-raising only
tests/LamuFlix.UnitTests/         # conditional Q5 test-raising only
```

**Structure Decision**: No new project, folder, or layer. All edits land in the three named files (plus conditional test files in the two existing test projects); `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, `gitleaks.yml` untouched per Q4.

## Design

### Job layout

Three new separate jobs in `ci.yml` after the existing `build-test-format` and `web` jobs, each `ubuntu-latest`, same triggers, no path filters. .NET jobs follow the existing shape (`actions/checkout@v4`, `actions/setup-dotnet@v4` with `global-json-file: global.json`); no caching beyond what existing jobs use. The `stryker-core` job additionally runs `dotnet tool restore` before invoking the mode (the tracked manifest names `dotnet-stryker` but nothing installs it on a fresh runner) and carries `timeout-minutes: 120` as an operational bound well under the GitHub 360-minute default, re-sized from the T004 baseline duration when that is recorded; no concurrency group is added, so a superseded whole-Core run still completes and uploads its diagnosable report (Q9).

### Container job

Exactly two explicit steps after checkout, one per Dockerfile — `docker build -f docker/Dockerfile.api .` and `docker build -f docker/Dockerfile.web .` — each step named for its file so a failure names the failing file in the job summary without log parsing (US1 acceptance 2); exit 0 per file; a missing selected file or nonzero build fails the job (Q2a, recon-DEV-327-3). Local tags only (A3); no login, no push, no `docker/*` action (Q1, Q8); no non-root/runtimeconfig/content checks (DEV-21 evidence, Q2).

### Stryker job — mechanism (Q4 choice, brief Plan decisions)

Extend `scripts/run-mutation.ps1` with an explicit whole-project mode for `LamuFlix.Core` and invoke that mode from CI — NOT direct `dotnet stryker`. The script already owns the harness threshold read (default 80), the eligible-test-project rule excluding `*.ArchitectureTests`/`*.IntegrationTests`, temp-config generation (mtp runner, coverage off, `since` disabled, break = threshold), reporters from tracked `stryker-config.json` (progress, html, json), and the standing FAIL evaluation (recon section 7) that Q3 requires in CI. The whole-project mode branches above the base-ref/merge-base resolution and diff discovery (run-mutation.ps1:499–535): it never resolves `origin/<base>`, never takes the scope-empty exit 2, and selects `LamuFlix.Core` directly, so the shallow `actions/checkout@v4` needs no `fetch-depth` change. It passes the enumerated files under the `LamuFlix.Core` mutate scope as the changed-file set to `Evaluate-MutationReport`, so the outside-changed-files and changed-files-none-tested guards (run-mutation.ps1:314–345) keep firing meaningfully in whole-Core runs. Coverage in `Test-RunMutation.ps1` is bounded: classification via the whole-project dry-run and invocation via a stub `dotnet stryker` first on PATH in the fixture that writes a minimal report under the `-O` directory — no real Stryker run inside the fixture. The mode takes an explicit output root and writes to the deterministic whole-project location `<output-root>/LamuFlix.Core/` — report `<output-root>/LamuFlix.Core/reports/mutation-report.json`, HTML `<output-root>/LamuFlix.Core/reports/mutation-report.html` — never the default temporary timestamped root `%TEMP%/LamuFlix-stryker/<UTC yyyyMMdd-HHmmss>-<6 hex>` (recon section 7).

Pass bar: exit 0 only (score >= 80 whole-Core, score = (killed + timeout) / tested x 100). Exit 1 fails; exit 2 fails in CI (whole-Core scope is never scope-empty — a 2 means the gate did not run). The job runs `dotnet tool restore` (tracked `.config/dotnet-tools.json`, no dependency change) before the mode and that step must exit 0; no separate `dotnet restore` step is added because Stryker restores/builds the mutated and test projects itself and the property script's `dotnet test` (run-property-tests.ps1:129, no `--no-restore`) restores implicitly. Artifact: the `stryker-core` job passes `${{ runner.temp }}/stryker-core` as the output root and uploads the exact derived path `${{ runner.temp }}/stryker-core/LamuFlix.Core/reports/mutation-report.html` (plus `mutation-report.json` from the same `reports/` directory) with `actions/upload-artifact@v4`, name `stryker-report-core`, `if: always()`, `if-no-files-found: error`, default retention (Q9, A1), part of the same workflow edit (ordering constraint 4).

### Property-test job

Two explicit steps (not runtime-discovered): `pwsh scripts/run-property-tests.ps1 -Project tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`, `.../LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` — one `-Project` per invocation (recon section 6). Every invocation must exit 0; exit 1 or 2 on any one fails the job (Q6). The script itself is not edited (Q4).

### Q5 baseline measurement (before tasks are cut)

Run the whole-project mode once for `LamuFlix.Core` as soon as it exists (T004); record the raw score in this plan. >= 80: no test-raising planned (T005 marked N/A with the score cited). < 80 and fits size M: cut concrete test-raising tasks (T005a, test files enumerated in the task note) plus a re-measure task (T005b); Phase 3 (ci.yml edit, T006) is BLOCKED until T005b records >= 80. < 80 and oversize: STOP before Phase 3 and return to Patron (ordering constraint 3; Q5, brief lines 51, 74, 98). When a failing run occurs, record report presence and the job failure in the task note — the local report files at the deterministic location for a pre-T006 below-80 baseline run, the `stryker-report-core` workflow artifact for a post-T006 CI failure; break stays 80, never lowered. A below-80 gap is sized from named inputs (tested and surviving mutant counts and the files carrying surviving mutants) before any oversize judgement. Baseline result: 100.00 percent whole-Core mutation score (pwsh -NoProfile -File ./scripts/run-mutation.ps1 -WholeProject), exit 0, threshold 80 (PASSED); T005 N/A, T005a/T005b bypassed.

## Complexity Tracking

No constitution violations; nothing to justify.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
