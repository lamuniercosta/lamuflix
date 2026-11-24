# DEV-565 Phase 5 acceptance evidence (T010–T012)

Status: T010 README complete; T011 hosted checks green; T012 live receipts **pending per deployment**. Stubbed smoke is not counted as live proof. No credential values are recorded here.

## Provenance

| Field | Value |
|---|---|
| Worktree | `F:/Dev/LamuFlix.worktrees/DEV-565` |
| Branch | `feature/DEV-565` |
| Runtime/cutover commit verified | `a37c5e2c993bcba71f7d4de945542d8b57a9a197` (`DEV-565 - Phase 4 deploy parity guard and gitleaks`) |
| Phase 5 additions verified | `README.md`, `specs/DEV-565/tasks.md`, this evidence file |
| Read-only review inputs | `src/LamuFlix.Api/appsettings.json`, `web/e2e/run.mjs`, `web/e2e/compose.e2e.yml`, `scripts/run-compose-smoke.ps1` — none edited in Phase 5 |

## T011 — hosted checks

Verdict labels are PASS / FAIL / Could not run only; a non-PASS required check is never folded into PASS.

| Required check | Command | Exit | Evidence |
|---|---|---:|---|
| Deploy-parity fixtures | `pwsh -NoProfile -File scripts/Test-DeployParityFixtures.ps1` | 0 | `5 cases, 0 failed`; cases `compose-extra-service`, `digest-mismatch`, `missing-service`, `pass`, `provider-key-env` (negative case exits 1 as expected) |
| Deploy-parity check | `pwsh -NoProfile -File scripts/Test-DeployParity.ps1` | 0 | `50 checks, 0 failed`; includes `compose api environment rejects legacy provider-key env` and `rendered api environment rejects legacy provider-key env` |
| Compose smoke | `pwsh -NoProfile -File scripts/run-compose-smoke.ps1 -CandidateRef HEAD` | 0 | `verdict: PASS`; plain startup reached API health with real Ollama readiness; stubbed full-flow phase passed import, listing, stub enrichment, thumbnail, hub update, advancing browser play and process-play 403; both project teardowns succeeded |
| E2E smoke | `npm run e2e` from `web/` | 0 | `30 passed (2.0m)`; 32 stream-acceptance cases recorded through nginx |
| Secret scan | gitleaks 8.28.0 detect, `--no-git`, over each of the 25 final changed files | 0 | `EXIT_GITLEAKS_ALL=0`, no leaks in the final Phase 5 file set; Phase 4 receipt also recorded in the `DEV-565` task note |

Compose smoke was first attempted before the local toolchain was installed: the plain-startup phase passed, then the stubbed-flow phase failed because `web/node_modules` was absent (exit 1). `npm ci` and `npx playwright install chromium` were run as local environment setup; no project manifest or lockfile changed. The re-run above is the final, green result. The initial failure is retained here for honesty, not treated as a pass.

Compose smoke and E2E smoke were run serially in this worktree. No source file outside the ticket allowlist was edited to make them pass.

### E2E settings-channel review (read-only)

Credential-redacted verification of the generated artifacts after `npm run e2e`, produced by parsing `web/e2e/.runtime/compose.env`, `web/e2e/.runtime/api.settings.json`, and `docker compose ... config --format json`:

| Field | Redacted evidence |
|---|---|
| generated env file | `web/e2e/.runtime/compose.env` |
| generated settings file | `web/e2e/.runtime/api.settings.json` |
| env file carries settings path | `true` (`E2E_SETTINGS_FILE`) |
| settings path matches generated file | `true` |
| `Features.LocalPlay` | `false` |
| `Omdb:BaseUrl` | `http://127.0.0.1:18080/` |
| `Subtitles:OpenSubtitles:BaseUrl` | `https://127.0.0.1:18443/` |
| Compose API mount source | generated `web/e2e/.runtime/api.settings.json` |
| Compose API mount target | `/app/appsettings.Production.json` |
| Mount read-only | `true` |
| Compose API environment names | `Ollama__Endpoint`, `SSL_CERT_FILE`, `Thumbnails__Enabled`, `Tmdb__Enabled` |

Connection strings, API keys, and container credentials were omitted; only key names, paths and non-secret settings are shown. The runner, its overlay, and the shipped Compose smoke script were review-only; no edits were made. Scope note: `web/e2e/compose.e2e.yml` retains a pre-existing non-secret `Tmdb__Enabled: 'false'` test override for stub determinism. It carries no key, is outside the shipped Compose/manifest parity guard that rejects legacy provider-key env, and was not changed because the E2E runner is a review-only verification input under `tasks.md` / `plan.md`.

### Missing settings source in isolated Compose verification

An isolated `git archive HEAD` copy with no `docker/api.settings.json` was created outside the worktree. `docker compose -p <isolated-project> -f docker-compose.yml up --no-deps api` was run with inert fixture values for the non-provider container credentials; the project was torn down afterwards.

| Observation | Result |
|---|---|
| Settings file in the isolated copy | absent |
| Container creation/start | Compose created and started the API container; the missing bind source did not produce a Compose validation error in this runtime |
| API startup | `Unhandled exception. System.InvalidOperationException: Connection string 'DefaultConnection' not configured. Set 'ConnectionStrings:DefaultConnection' via user secrets or the ConnectionStrings__DefaultConnection environment variable.` |
| Process outcome | `api-1 exited with code 139`; no API health was reached |
| Compose CLI exit | `0` because plain `up` was invoked without `--abort-on-container-exit`; the container crash, not the CLI code, is the startup evidence |
| Credentials | inert fixture values only; no real key or connection string was used, read, or echoed |

## T012 — live receipts

Both receipts are closure criteria. Neither is available in this verification environment; each deployment is recorded as **pending live acceptance**, never as PASS via stubbed smoke and never as an alternate acceptance outcome. Carry both pending statuses into T013.

| Deployment | Live receipt | Status | What remains |
|---|---|---|---|
| Compose | Not captured. `docker/api.settings.json` is absent in this worktree, and no real provider keys were available. The green Compose smoke generated ephemeral settings in disposable copies and stubbed external providers. | **Pending live acceptance** | Real credential-redacted startup and OMDb + TMDb enrichment of a known title from the mounted settings only; show absence of provider-key env wiring; retain prior `.env` inputs until verification; verify after rollout, then remove obsolete lines. |
| Home cluster | Not captured. `kubectl config current-context` is unset and `kubectl -n lamuflix get pods` cannot reach a cluster. A listener on `127.0.0.1:3100` answered `/health/ready` with HTTP 200, but that alone proves neither cluster identity nor mounted-settings source. | **Pending live acceptance** | Real credential-redacted startup and OMDb + TMDb enrichment of a known title from mounted `api-settings` only; show no API dependency on `tmdb-credentials`; retain external inputs until verification; record post-publish `Tmdb:Enabled` check without credentials where the opt-out applies; only then perform manual cleanup. |

Enabled/blank diagnostics: no real-provider enabled/blank TMDb diagnostic was observed in scoped verification. The isolated missing-settings run showed the fail-closed connection-string error above; the stubbed flows intentionally disabled TMDb in the E2E overlay and are not live enrichment evidence.

Rollback-input retention is a requirement for both future receipts, not a satisfied assertion in this phase. Missing receipts leave runtime acceptance pending per deployment and block implementation merge clearance at T013/T014.
