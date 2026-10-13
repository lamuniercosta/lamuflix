# Feature Specification: DEV-327 — CI container build validation, Stryker Core gate, property-test gate

**Feature Branch**: `feature/DEV-327` (Phase B)

**Created**: 2026-10-13

**Status**: Draft

**Input**: `specs/DEV-327/brief.md`, recon note `recon-DEV-327`, note `DEV-327`, `specs/PRODUCT.md`, `specs/DEV-327/CONCLUSIONS.md` (Q1–Q9), `specs/DEV-327/ASSUMPTIONS.md`, recon note `recon-DEV-327-3`, Q2a (brief.md Plan decisions)

## User Scenarios & Testing

### User Story 1 - Container images build cleanly in CI (Priority: P1)

A maintainer opening the DEV-327 PR sees a dedicated CI job proving the two selected DEV-21 Dockerfiles (API and Web) build cleanly from the repo root.

**Why this priority**: Ticket acceptance "Dockerfiles build cleanly"; build-only validation with no push (Q1).

**Independent Test**: Push the branch and observe the container job green; each of `docker/Dockerfile.api` and `docker/Dockerfile.web` builds with `docker build -f <file> .` exit 0 from the repo root.

**Acceptance Scenarios**:

1. **Given** the selected API and Web Dockerfiles on the branch, **When** CI runs, **Then** the container job builds exactly `docker/Dockerfile.api` and `docker/Dockerfile.web` from the repo root and passes on exit 0 per file (Q2a).
2. **Given** any Dockerfile fails to build, **When** CI runs, **Then** the container job fails and names the failing file.
3. **Given** a green or red run, **When** the job finishes, **Then** no registry login step ran, no secret beyond `GITHUB_TOKEN` was used, and nothing was pushed (Q1).

---

### User Story 2 - Stryker whole-Core mutation gate with uploaded report (Priority: P1)

A maintainer sees a dedicated CI job running whole-project Stryker on `LamuFlix.Core` at break 80, failing CI below 80, with the HTML report uploaded on every run including failures.

**Why this priority**: Ticket acceptance "Stryker on LamuFlix.Core threshold >= 80" and "Stryker report uploaded".

**Independent Test**: Push the branch and observe the Stryker job: exit 0 passes; a below-80 score fails; the `stryker-report-core` artifact exists on both green and red runs (`if: always()`).

**Acceptance Scenarios**:

1. **Given** any CI run, **When** the Stryker job runs, **Then** it mutates the whole of `src/LamuFlix.Core/LamuFlix.Core.csproj`, never enables `since`, and passes only at score at or above break 80 (Q3).
2. **Given** a score below 80, **When** the run finishes, **Then** the job fails and the HTML report artifact is still uploaded (Q9).
3. **Given** a whole-Core run, **When** the script evaluates the result, **Then** the standing FAIL criteria apply (0 tested, 0 killed, score below threshold, and the other evaluation failures at run-mutation.ps1:335–368) (Q3).
4. **Given** a whole-Core run, **When** Stryker classifies the result, **Then** script exit 0 passes, exit 1 fails, and exit 2 also fails the CI job (whole-Core scope is never scope-empty) (brief Plan decisions).

---

### User Story 3 - Property tests wired into CI per project (Priority: P2)

A maintainer sees CI invoking `scripts/run-property-tests.ps1` once per enumerated project, with any non-zero exit failing the job.

**Why this priority**: Ticket names the script verbatim; exit-2-on-vanished-tests closes the silent-skip hole (Q6).

**Independent Test**: Push the branch and observe the property-test job green only when each of the three enumerated invocations exits 0.

**Acceptance Scenarios**:

1. **Given** CI runs, **When** the property-test job runs, **Then** it invokes `pwsh scripts/run-property-tests.ps1 -Project <project>` once per enumerated project: `tests/LamuFlix.Test/LamuFlix.Test.csproj`, `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`, `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` (Q6).
2. **Given** any one invocation exits 1, **When** the job finishes, **Then** the job fails.
3. **Given** any one invocation exits 2, **When** the job finishes, **Then** the job fails (tagged tests vanished or all skipped) (Q6).

---

### Edge Cases

- A selected Dockerfile (`docker/Dockerfile.api` or `docker/Dockerfile.web`) is missing: the container job fails (Q2a); no DEV-21 wait.
- Whole-Core baseline below 80 at plan stage: Phase 3 waits — test-raising is in scope only if the gap fits size M (concrete T005a test tasks plus T005b re-measure before T006); if closing the gap exceeds size M, STOP before Phase 3 and return to Patron — no silent oversized task list (Q5).
- A failing Phase B Stryker run still makes its report diagnosable with break 80 unchanged: a post-T006 CI failure uploads `stryker-report-core` (`if: always()`), while a pre-T006 below-80 baseline run (T004/T005b, before the workflow exists) leaves the local report files at the T001 deterministic location; each case records report presence and the failure in the task note.
- `scripts/run-mutation.ps1` invoked in whole-project mode on a repo with zero `src/` changes still runs the full Core mutation (never scope-empty exit 2 by lack of diff).
- Property-test project loses all `Category=Property` tests: that project's exit 2 fails CI rather than silently dropping out (explicit enumeration, Q6).
- Stryker run fails before producing a report: artifact step still runs (`if: always()`); a missing report is itself a failure (report-missing evaluation, recon section 7).

## Requirements

### Functional Requirements

- **FR-001**: CI MUST add exactly three new jobs to `.github/workflows/ci.yml` alongside the untouched `build-test-format` and `web` jobs: container build, Stryker Core, property tests (Q4, Q7).
- **FR-002**: Container job MUST build exactly `docker/Dockerfile.api` and `docker/Dockerfile.web`, each as `docker build -f <file> .` from the repo root with local tags only, exit 0 per file; a missing selected file or nonzero build fails the job (Q2a, A3).
- **FR-003**: Container job MUST NOT include a registry push, registry login, new secret, `docker/*` action, Dockerfile creation, or non-root/runtimeconfig/image-content check (Q1, Q2, Q8).
- **FR-004**: Stryker job MUST mutate the whole of `src/LamuFlix.Core/LamuFlix.Core.csproj` on every run with break = `harness.yml` `gates.mutation.threshold` (80), never enabling `since` (Q3).
- **FR-005**: Stryker job MUST pass only on whole-project-mode exit 0 (score at or above 80 with all standing FAIL criteria clear); exit 1 or exit 2 MUST fail the job (Q3, brief Plan decisions).
- **FR-006**: Stryker job MUST run `dotnet tool restore` against the tracked `.config/dotnet-tools.json` before invoking the whole-project mode (the manifest check in `run-mutation.ps1` does not install `dotnet-stryker`), MUST invoke that mode with an explicit deterministic output root of `${{ runner.temp }}/stryker-core` (never the default temporary timestamped root `%TEMP%/LamuFlix-stryker/<UTC yyyyMMdd-HHmmss>-<6 hex>`, recon section 7), and MUST upload the exact derived artifact path — `${{ runner.temp }}/stryker-core/LamuFlix.Core/reports/mutation-report.html` (plus `mutation-report.json` from the same `reports/` directory) — with `actions/upload-artifact@v4` named `stryker-report-core`, default retention, `if: always()`, and `if-no-files-found: error` so a run that dies before writing a report fails visibly instead of warning (Q8, Q9, A1).
- **FR-007**: Property-test job MUST invoke `scripts/run-property-tests.ps1` once per enumerated project (the three csproj paths in US3); every invocation MUST exit 0 or the job fails (Q6).
- **FR-008**: All three new jobs MUST run on `ubuntu-latest` with the existing triggers (push to main, pull_request to main) and no path filters (A2, brief Plan decisions).
- **FR-009**: `scripts/run-mutation.ps1` MUST gain an explicit whole-project mode for `LamuFlix.Core` reusing the tracked `stryker-config.json` reporters and the existing threshold/test-project/FAIL-evaluation guardrails, with existing diff-scoped behaviour and the 0/1/2 exit contract unchanged (Q3, Q4). The mode MUST branch above the base-ref/merge-base resolution and diff discovery (no `origin/<base>` resolution, no scope-empty exit 2 for a zero-diff run), MUST select `LamuFlix.Core` directly, and MUST pass the enumerated files under the `LamuFlix.Core` mutate scope as the changed-file set to the FAIL evaluation so the outside-changed-files and changed-files-none-tested guards (run-mutation.ps1:314–345) stay meaningful instead of tripping on every file or silently never firing. The mode MUST accept an explicit output root and write to the deterministic whole-project location `<output-root>/LamuFlix.Core/` (report at `<output-root>/LamuFlix.Core/reports/mutation-report.json`, HTML at `<output-root>/LamuFlix.Core/reports/mutation-report.html`), NOT the default temporary timestamped root (recon section 7).
- **FR-010**: `scripts/Test-RunMutation.ps1` MUST cover the new whole-project mode (classification via the whole-project dry-run, invocation via a stub `dotnet stryker` placed first on PATH in the fixture that writes a minimal `mutation-report.json` under the `-O` directory and exits 0 without a real Stryker run, the no-diff case, and both changed-file guards above) and assert the existing diff-scoped behaviour and 0/1/2 contract unchanged (Q4).
- **FR-011**: The plan MUST record a measured whole-Core Stryker baseline score before test-raising tasks are cut; Phase 3 (ci.yml edit) is BLOCKED on the baseline (T004) and gap decision (T005). A below-80 baseline that fits size M inserts concrete test-raising tasks (T005a, enumerated test files in the existing `LamuFlix.Test` / `LamuFlix.UnitTests` projects) plus a re-measure task (T005b, proceed to Phase 3 only at >= 80); an oversize gap STOPS before Phase 3 and returns to Patron (Q5, brief lines 51, 74, 98).
- **FR-012**: When a failing Phase B Stryker run occurs, Phase B MUST record report presence and the job failure (exit 1, score cited) in the task note — the `stryker-report-core` workflow artifact for a post-T006 CI `stryker-core` failure, or the local report files at the T001 deterministic location for a pre-T006 below-80 baseline run; break stays 80 and is never lowered to make the run pass (Q3, Q9).
- **FR-013**: No task MUST edit `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, or `gitleaks.yml`, change break 80, or add any NuGet/npm dependency, project, folder, schema change, API shape, `Features:LocalPlay`/secret/`Process.Start` touch (frozen scope).

### Key Entities

- **CI workflow (`ci.yml`)**: five jobs after this ticket — `build-test-format`, `web`, container build, Stryker Core, property tests; green on the DEV-327 PR means all five green (Q7).
- **Stryker whole-project mode**: new explicit mode in `scripts/run-mutation.ps1` for `LamuFlix.Core`; score = (killed + timeout) / tested x 100 (recon section 7).
- **Stryker report artifact**: HTML report from the Stryker output tree, artifact name `stryker-report-core` (Q9, A1).
- **Property-test enumeration**: the three csproj paths holding `Category=Property` tests at HEAD (35 Trait lines across 14 files at HEAD per recon-DEV-327-3; Phase A count was 33 lines, historical).

## Success Criteria

### Measurable Outcomes

- **SC-001**: The DEV-327 PR shows all five ci.yml jobs green: `build-test-format`, `web`, container build, Stryker Core (exit 0 at break 80), property tests (exit 0 x 3) (Q7).
- **SC-002**: Stryker HTML artifact `stryker-report-core` is downloadable from both a passing and a failing Stryker run (Q9).
- **SC-003**: A below-80 whole-Core score fails the Stryker job; a vanished-tag property-test project (exit 2) fails the property-test job (Q3, Q6).
- **SC-004**: `scripts/Test-RunMutation.ps1` passes, covering the new whole-project mode with the diff-scoped 0/1/2 contract unchanged (Q4).

## Assumptions

- Stryker artifact name `stryker-report-core`; GitHub default retention, no override (ASSUMPTIONS.md A1, Q9).
- New CI checks use the same triggers as ci.yml (push to main, pull_request to main); no path filters (ASSUMPTIONS.md A2 as decided in brief Plan decisions).
- Container job builds images without tagging for any registry; local tags only (ASSUMPTIONS.md A3, Q1).
- Pickup prerequisite satisfied: the selected DEV-21 API and Web Dockerfiles exist on main (brief Task-ordering constraint 1, Q2a, recon-DEV-327-3); no owner checkbox or restoration wait.
- Whole-Core baseline score is a plan-stage measurement, not a recon fact (recon section 16 contains no score).
