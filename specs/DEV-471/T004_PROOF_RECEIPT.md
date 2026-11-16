# DEV-471 T004 — Developmental proof receipt (US2)

Status: **developmental only**. Superseded by T010 final-candidate proof.
Recorded by Anvil (Build lane) on 2026-11-16 in worktree
`F:/Dev/LamuFlix.worktrees/DEV-471`, branch `feature/DEV-471`.

This receipt records one executed developmental run of
`scripts/run-compose-smoke.ps1 -RegressionProof`. It is not the delivery
evidence package and does not close T010, T011, T012, T013 or T014.

## Command

```text
pwsh -NoProfile -File scripts/run-compose-smoke.ps1 -RegressionProof
```

Exit code: **0** (strict smoke PASS; the script has no SKIPPED result).

Two identical developmental runs passed. The recorded one below is the final
verification run `20261116T080922Z`, executed against the committed runner after
the pre-commit secret-scan false positive on a credential variable name was
resolved. An earlier full run `20261116T075010Z` produced the same verdicts and
was discarded as superseded. Cold run took about 8 minutes; the warm re-run
(cached images and model) took 73 seconds.

## Runner and budget provenance

- Runner: `scripts/run-compose-smoke.ps1` (new in T002/T003), CmdletBinding with `-Help`.
- Budget config: `web/e2e/fixtures/dev471/budgets.json`, created before the runner was
  first used. Values: 1800s stack startup, 120s async assertion, 180s teardown per
  project, 7200s job cap, 0 whole-test retries — the single `ASSUMPTIONS.md` budget set.
- Evidence root (local, gitignored): `artifacts/compose-smoke/20261116T080922Z/`.

## Candidate positive phase

| Field | Value |
|---|---|
| Mode | `candidate` (plain shipped startup, no e2e endpoint override) |
| Project | `lamuflix-smoke-plain-e24aa4fd` |
| Source commit | `c03be7387ca2fb736977c9d3436067802cb7a82b` (working-tree HEAD at run time) |
| Source staging | disposable `git archive` copy under `%TEMP%`, outside the PR checkout |
| Settings | ephemeral `docker/api.settings.json` generated inside the disposable copy only, `Features:LocalPlay` false |
| Prerequisites | docker available, api image build succeeded, postgres healthy, rabbitmq healthy, real Ollama `all-minilm` model ready |
| Health | api `running` / `healthy` on `/health/ready` |
| API container exit | 0 |
| Verdict | PASS |

The candidate copy keeps the shipped `docker-compose.yml` untouched, including the
DEV-440 production binding `Ollama__Endpoint: http://ollama:11434/`. No overlay and no
endpoint override were used.

## Regression negative phase

| Field | Value |
|---|---|
| Mode | `regression` (`-RegressionProof`) |
| Project | `lamuflix-smoke-regress-4ef82de4` |
| Source commit | `c03be7387ca2fb736977c9d3436067802cb7a82b` (same candidate HEAD; historical parent never used) |
| Patch | reverse of only `4646476fb1f8afb8539a07c8f404c911a5fc174e` |
| Patch sha256 | `00f570ada4717f443ff4658193e6b961c90946724b4b728b7e6f701ff858bb54` |
| Removed line | `Ollama__Endpoint: http://ollama:11434/` |
| compose sha256 | `8e2931f4e74c989ef9457c1741fb3a1cc2de68db0b086f1bdbbf3877fa42ac09` -> `2ff9c9654995b4099067b2e222b56d6e23c661f4d437a4c9d4852342e751745d` |
| Prerequisites | identical: docker available, build succeeded, settings generated, postgres healthy, rabbitmq healthy, real Ollama model ready |
| Health | api **never healthy** — container `exited` / `unhealthy` |
| API container exit | **139** (optional corroboration only) |
| Attributable cause | embedding request to `http://localhost:11434/api/embed` through the default localhost endpoint |
| Verdict | PASS |

The failure is attributable to the reverted production binding: with the binding removed
the API falls back to the `OllamaOptions` default `http://localhost:11434/` and its startup
embedding probe fails, so `/health/ready` never becomes healthy. Docker, build, settings
generation, and Postgres/RabbitMQ/model readiness all succeeded first, so this is not an
infrastructure failure or an unexplained timeout.

## Failure-path observations

- Patch fail-closed behaviour was exercised separately during T003 development: a second
  reversal attempt against an already-reversed copy was rejected, and a copy without the
  binding is refused before startup.
- One earlier developmental attempt polled `compose ps -q` without `--all`, so the
  successfully-finished one-shot `ollama-model-init` container looked merely absent and the
  model-readiness wait ran to its timeout. That was a runner defect, not a stack failure;
  it was fixed (`ps -a -q`) before the recorded runs. The aborted attempt's containers and
  staging copy were cleaned up manually.

## Hygiene

- Original PR checkout baseline hash unchanged across the run (`checkoutBaseline.unchanged`
  = true); pre-existing modified spec files stayed modified and no other file was touched.
- Every created Compose project was registered in `projects.txt` before startup and torn
  down scoped to its own `-p` project name with `down --volumes --remove-orphans`.
- No leftover `lamuflix-smoke-*` containers and no leftover staging directory after the run.
- Evidence scan found 18 `[redacted]` markers and no generated credential, connection
  string, or username token in any captured file (compose log, compose ps, receipts).

## Limits of this receipt

- Developmental only; **T010 final-candidate proof supersedes it**.
- Phase 1 (plain startup) only. The stubbed full-flow phase, active process-play 403,
  failure-path suite, dedicated workflow and branch-protection verification remain open.
- Not re-run after any later runner or health-assertion change. Refresh required before use
  as current evidence.
