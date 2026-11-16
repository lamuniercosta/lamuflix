# DEV-471 Phase 4 proof receipt — User Story 4 (T008)

Receipt for the dedicated unconditional CI wiring. Captured 2026-11-16 on `feature/DEV-471`
in `F:/Dev/LamuFlix.worktrees/DEV-471`. No credentials, generated settings values, or
connection strings appear here.

## Deliverable

New file `.github/workflows/compose-smoke.yml`. Nothing else is modified: `ci.yml`,
`promote-home.yml`, `docker-compose.yml`, `web/e2e/compose.e2e.yml`, `web/e2e/run.mjs` and
`scripts/run-compose-smoke.ps1` are untouched by this phase, per `git status` and `git diff`.

## Workflow definition

| Requirement (T008 / FR-001, FR-008–FR-012, FR-018) | Implementation |
|---|---|
| `pull_request` to `main` only, no path filters, no `workflow_dispatch` | `on: pull_request: branches: [main]`; no `paths`/`paths-ignore` anywhere in the file |
| Job id and displayed name `compose-smoke` | job `compose-smoke`, `name: compose-smoke`, single job |
| Both positive phases on every main-targeting PR including docs-only | one `Run required Compose smoke` step invoking `scripts/run-compose-smoke.ps1`, which always runs the plain-startup and stubbed-flow phases |
| Budgets derived from `budgets.json` prep output | `Read DEV-471 budgets` step validates the four integer budgets and `wholeTestRetries = 0`, then emits `jobCapMinutes`/`setupMinutes`/`runnerMinutes`/`cleanupMinutes` to `$GITHUB_OUTPUT` and the job summary; the smoke and cleanup step timeouts read those outputs |
| Single source of truth for the cap | job `timeout-minutes: 120`; the budgets step fails closed when `jobCapSeconds` no longer implies that pinned value |
| Cleanup time reserved inside the cap | `runnerMinutes = jobCapMinutes − setupMinutes − cleanupMinutes` (10 + 3 reserved from 120) |
| `-RegressionProof` triggered by change to the runner or extracted startup/health code | `Decide whether the controlled-revert proof is required` diffs `merge-base(base, HEAD)...HEAD` against `scripts/*compose-smoke*.ps1`, `*.ps1` startup and `*.ps1` health names and passes `-RegressionProof` when matched |
| Fail closed when change-base is unresolved | the step defaults `required=true` and only clears it after a successful resolved diff that did not match |
| Ordinary unrelated PRs run both positives only | no `-RegressionProof` when the diff is clean |
| Never green for Docker/model/health/timeout/assertion/teardown failure | the runner's exit code is propagated verbatim (`exit $LASTEXITCODE`); there is no `continue-on-error` and no skip path |
| Allowlisted redacted artifacts under artifact name `compose-smoke` | `Guard compose-smoke evidence` enforces a fixed allowlist, rejects unexpected files and non-evidence extensions, scans for unredacted credential keys, connection-string usernames and generated `lamuflix_*` values, stages only vetted files, then uploads artifact `compose-smoke` |
| Always-run project-scoped cleanup | `Clean up compose-smoke projects` with `if: always()`, reading only the runner-registered `projects.txt` names matching `lamuflix-smoke-(plain|regress|flow)-<hex8>`, and failing the job when cleanup fails |
| `e2e-smoke` remains distinct | `ci.yml` untouched; no job in this workflow is named or aliased `e2e-smoke` |
| No unverified upload helper claims | the workflow builds its own allowlist guard; it does not reference the evaluation-metrics or e2e-evidence helpers |

## Local verification of each scripted step

Step scripts were extracted from the committed YAML and executed locally, so the assertions below
are against the same text that runs in CI.

### Structure assertions — 12/12 PASS

Trigger is `pull_request` only; targets `main`; no path filters; no `workflow_dispatch`; single
`compose-smoke` job; job name `compose-smoke`; job timeout 120; runner step present; cleanup
`if: always()`; upload artifact name `compose-smoke`; `permissions: contents: read`; no
`paths-ignore` text anywhere.

### Budgets step

| Case | Exit | Output |
|---|---|---|
| committed `budgets.json` | 0 | `jobCapMinutes=120`, `setupMinutes=10`, `runnerMinutes=107`, `cleanupMinutes=3`, `startupMinutes=30`, `asyncAssertionSeconds=120` |
| pinned cap drifted to 60 | 1 | `budget config jobCapSeconds implies 120 minute(s) but the job timeout is 60` |

### Controlled-revert trigger step

| Case | `required` |
|---|---|
| base = HEAD (no change in range) | `false` |
| base `536ab55c` (Phase 3 range, which touched the runner) | `true` |
| empty/unresolved base | `true` (fail closed) |
| all-zero bogus base | `true` (fail closed) |

Pattern unit check: `scripts/run-compose-smoke.ps1` and `scripts/helper-startup.ps1` match;
`web/x.ts`, `scripts/_gate-common.ps1` and `scripts/run-e2e-smoke.ps1` do not.

### Evidence guard

Run against the real Phase 3 artifact directory `artifacts/compose-smoke/20261116T091845Z`
(8 files: `receipt.json`, `run.log`, `projects.txt`, three `plain-startup-candidate` files, two
`stubbed-flow` files).

| Case | Exit | Outcome |
|---|---|---|
| passing run, complete evidence | 0 | `staged 8 allowlisted compose-smoke evidence file(s)` |
| unexpected extra file `raw-settings.json` | 1 | `unexpected compose-smoke evidence: raw-settings.json` |
| required file removed | 1 | `a passing compose-smoke run is missing evidence: stubbed-flow/stubbed-flow.log` |
| proof required but regression evidence absent | 1 | missing `plain-startup-regression/*` reported |
| failed run with incomplete evidence | 0 | warns and still stages what exists, so failure diagnostics reach the artifact |
| binary `screenshot.png` present | 1 | rejected as not an allowlisted evidence type |
| injected `POSTGRES_PASSWORD` assignment with a plaintext value | 1 | `compose.log (PASSWORD)` |
| the same key with a `[redacted]` value | 0 | redacted form accepted |
| connection string carrying a generated `lamuflix_*` username | 1 | flagged as connection-string username and generated credential value |

The first password scan exposed a real defect in this phase: `\b` does not match between `_` and
`PASSWORD`, so an env-style `POSTGRES_PASSWORD` assignment with a plaintext value passed the
guard. The pattern was corrected to anchor on a non-alphanumeric boundary and the case now fails
closed as shown.

### Cleanup step

| Case | Exit | Outcome |
|---|---|---|
| `projects.txt` with smoke, flow, unrelated and malformed entries | — | accepts only `lamuflix-smoke-plain-<hex>` and `lamuflix-smoke-flow-<hex>`; `lamuflix-e2e-aaaabbbb` and blank lines are ignored |
| no `projects.txt` | 0 | `No compose-smoke project was registered; nothing to clean up.` |
| no `artifacts/` directory at all | 0 | same message; cleanup never masks the real verdict |

## Gates

| Gate | Result |
|---|---|
| YAML parses (PyYAML) | PASS |
| No tab characters (GitHub Actions requires spaces) | PASS |
| Structural assertions listed above | 12/12 PASS |
| `npm run typecheck:e2e` | N/A — no web file changed in this phase |
| `scripts/run-web-gates.ps1` | N/A — no web file changed in this phase |
| Roslyn / cyclomatic / InspectCode | N/A — no `.cs` changed |

## Limits

This is the workflow definition only, which is what the Phase 4 checkpoint asks for. It is **not**:

- proof that the workflow registers and runs on GitHub (T009/T011);
- the failure-path, planted-secret and existing-runner proofs (T009);
- the `T010` final-candidate `-RegressionProof` (which supersedes developmental `T004`);
- operator verification that `main` requires the actual published `compose-smoke` context (T011).

No job in this workflow has executed on a GitHub runner. `SC-006`/`T011` remain open and keep Done
and the implementation merge-bar non-PASS.
