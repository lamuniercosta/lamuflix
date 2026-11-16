# DEV-471 Phase 5 proof receipt — User Story 5 (T009–T011)

Scrubbed receipt for the failure-path proofs, the final-candidate controlled-revert evidence, and the
required-check registration status. Captured 2026-11-16 on `feature/DEV-471` in
`F:/Dev/LamuFlix.worktrees/DEV-471`. No credentials, generated settings values, or connection
strings appear here.

## Environment

| Item | Value |
|---|---|
| Docker server | 29.9.3 |
| Candidate commit for T010 | `c57b7d3781ab429ed89861ef3ffefd20e5d9e99f` |
| Budgets | startup 1800s, assertion 120s, teardown 180s, job cap 7200s, whole-test retries 0 |
| Validated budget config | `web/e2e/fixtures/dev471/budgets.json` — **unmodified** by this phase |

## T009 — failure-path proofs

New named mode on the existing runner: `scripts/run-compose-smoke.ps1 -FailurePathProof`
(documented in `-Help`). It drives a deterministic orchestration double,
`web/e2e/fixtures/dev471/fake-docker.ps1`, that stands in for the **docker CLI only**. It never
contacts a Docker daemon and never starts a container. `brief.md` Approach step 5 permits
meaningful orchestration simulation for these proofs and requires that it not replace real-stack
acceptance, which T010 below supplies.

Children run with `DEV471_FAILURE_PATH_CHILD=1`, receive bounded simulation budgets through
`DEV471_FAILURE_PATH_BUDGETS` (validated inside `3..60` seconds; the committed budget config is
never relaxed), and write their evidence to `artifacts/compose-smoke-failure-paths/` so the
single-run invariant of the CI artifact base is untouched.

Nine proofs, all PASS. `pwsh -NoProfile -File scripts/run-compose-smoke.ps1 -FailurePathProof`
exited **0** with `verdict: PASS (9 of 9 proofs held)`.

| # | Proof | Observed |
|---|---|---|
| 1 | Startup timeout | required smoke exit **1**; diagnostics captured before teardown; teardown attempted; receipt cause `candidate plain startup never reached API health (timeout)` |
| 2 | Unavailable Docker | required smoke exit **1**; no project was ever registered, so no teardown was attempted; receipt cause `Docker is not available: ... docker daemon running?` |
| 3 | Assertion failure | required smoke exit **1**; dependencies and API healthy, so the stubbed full-flow assertion is what failed; diagnostics and teardown both recorded |
| 4 | Teardown failure | required smoke exit **1**; receipt cause carries the daemon removal error; cleanup failure surfaces as failure, never PASS |
| 5 | Planted secret (stdout) | required smoke exit **1**; runner stdout carried **no** planted value |
| 6 | Planted secret (upload) | the workflow's own evidence guard exits **1** and stages nothing when a planted credential is in `compose.log`; the same file with `[redacted]` exits **0** and stages the allowlisted set |
| 7 | Cancellation cleanup | the runner registers its Compose project before startup, so a hard kill leaves that project behind; the workflow's always-run cleanup script then targets exactly that project, and a simulated removal failure is reported as cleanup failure |
| 8 | Unrelated projects untouched | the always-run cleanup reclaimed both registered smoke projects and never touched `lamuflix-e2e-*` or an unrelated stack |
| 9 | Existing-runner nonregression | `scripts/run-e2e-smoke.ps1` still answers its own **SKIPPED exit 2** with Docker unavailable, while the required smoke answered **exit 1** for the same condition |

### Defect this phase fixed

`Protect-Text` used `\b(password|pwd|apikey|api_key|secret|token)\b`. A word boundary does not
match between `_` and `PASSWORD`, so an environment-style postgres credential assignment reached
stdout and evidence unredacted. The pattern now anchors on a non-alphanumeric boundary, adds
`passwd` and `pwd`, and matches quoted values. The planted-secret proof pins the repair: that same
assignment now renders its value as `[redacted]`.

### Limits of T009

Orchestration simulation only. It proves the runner's and workflow's failure/cleanup/redaction
handling, not that a real stack fails the same way. Real positive Compose acceptance and the
controlled-revert proof are supplied separately by T010 below.

## T010 — final-candidate positive plus controlled revert

`pwsh -NoProfile -File scripts/run-compose-smoke.ps1 -RegressionProof` on the final implemented
candidate. Exit **0**, `verdict: PASS`. Evidence
`artifacts/compose-smoke/20261116T101400Z/`.

| Phase | Verdict | Detail |
|---|---|---|
| Phase 1 plain startup (candidate) | PASS | `api healthy (status=running, health=healthy)` after healthy Postgres/RabbitMQ and real Ollama model readiness; plain shipped `docker-compose.yml`, no e2e endpoint override |
| Phase 2 stubbed full flow | PASS | `smokeExitCode=0`: fixture-correlated import, stub enrichment, listing, decodable thumbnail, server-originated hub frame, advancing browser play, and active process-play 403 through shipped nginx |
| Regression (reversed `4646476f`) | PASS | identical health assertion failed: `api exited (status=exited, health=unhealthy)`; attributable cause `embedding request to http://localhost:11434/api/embed through the default localhost endpoint; api container exited 139` |

Source and patch identities, from the run receipt:

| Field | Value |
|---|---|
| candidate commit | `c57b7d3781ab429ed89861ef3ffefd20e5d9e99f` |
| reversed commit | `4646476fb1f8afb8539a07c8f404c911a5fc174e` |
| removed binding | `Ollama__Endpoint: http://ollama:11434/` |
| patch SHA-256 | `00f570ada4717f443ff4658193e6b961c90946724b4b728b7e6f701ff858bb54` |
| compose before / after | `8e2931f4e74c…` / `2ff9c9654995…` |
| PR checkout baseline | unchanged before and after the run |

This evidence **supersedes** the developmental T004 proof. Repeat obligation: any change to the
startup runner or the extracted startup/health assertion invalidates it, and the workflow's
change-triggered `-RegressionProof` re-runs it, failing closed to run when the change base cannot
be resolved. Ordinary unrelated PRs run both positive phases only.

## T011 — required-check registration: OPEN (not PASS)

Recorded as **open/non-PASS**. Nothing here claims registration.

Read-only observation, no mutation performed:

| Check | Result |
|---|---|
| `gh api repos/lamuniercosta/lamuflix/branches/main/protection` | returns protection, but carries **no** `required_status_checks` object at all |
| Repository rulesets | none returned |

Per `spec.md` FR-015 / SC-006 and `recon-DEV-471:70`, that omission is **not** evidence of
registration and is **not** evidence that no other enforcement exists. T011 needs an authorized
repository operator/owner to confirm that `main` requires the **actual published** `compose-smoke`
context, with no bypass. Build seats must not perform that admin mutation, and none was attempted.

Additional fact: no PR exists yet for `feature/DEV-471` (`gh pr list --head feature/DEV-471`
returns nothing), so the `compose-smoke` check context has not been published by any workflow run.

Consequence: SC-006/T011 stay open, which keeps ticket **Done** and the implementation merge-bar
**non-PASS**. Spec Gate 1 remains independent and unaffected.

## Gates

| Gate | Exit | Verdict |
|---|---|---|
| `scripts/run-compose-smoke.ps1 -FailurePathProof` | 0 | PASS (9 of 9) |
| `scripts/run-compose-smoke.ps1 -RegressionProof` | 0 | PASS (three phases) |
| `scripts/run-compose-smoke.ps1 -Help` | 0 | PASS (documents `-FailurePathProof`) |
| Parse check, runner and shim | — | PASS |
| `npm run typecheck:e2e` | — | N/A: no TypeScript changed in this phase |
| `scripts/run-web-gates.ps1` | — | N/A: no `/web` source changed in this phase |
| Roslyn / cyclomatic / InspectCode | 2 | SKIPPED (scope-empty: no `.cs` changed) |
| `dotnet test` | — | N/A: no C# changed |

The new fixture is PowerShell, so Prettier cannot parse it (`No parser could be inferred`); it is
not a web source file and is not covered by the web gates.

## Files touched by this phase

| File | Change |
|---|---|
| `scripts/run-compose-smoke.ps1` | `-FailurePathProof` mode, child budget/evidence seams, hardened `Protect-Text`, Docker-unavailable evidence retention |
| `web/e2e/fixtures/dev471/fake-docker.ps1` | new deterministic docker-CLI double for the proofs |

`docker-compose.yml`, `web/e2e/compose.e2e.yml`, `docker/Dockerfile.api`,
`.github/workflows/ci.yml`, `.github/workflows/promote-home.yml` and
`web/e2e/fixtures/dev471/budgets.json` are untouched. No committed secret, no PR-checkout
mutation, no new dependency, and LocalPlay remains false.
