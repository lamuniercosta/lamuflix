# DEV-471 Phase 3 proof receipt — User Story 3 (T005–T007)

Scrubbed receipt for the stubbed fixture-correlated full-flow smoke. Captured 2026-11-16 on
`feature/DEV-471` in `F:/Dev/LamuFlix.worktrees/DEV-471`. No credentials, generated settings
values, or connection strings appear here.

## Environment

| Item | Value |
|---|---|
| Docker server | 29.9.3 |
| Node | v24.19.0 |
| Playwright / Chromium | 1.65.0 / 158.0.8127.18 |
| Candidate commit | the Phase 3 commit on `feature/DEV-471` (runner resolves `HEAD`) |
| Budgets | startup 1800s, assertion 120s, teardown 180s, job cap 7200s, whole-test retries 0 |

## Fixture correlation (T005)

| Item | Value |
|---|---|
| Folder staged into the isolated runtime library | `dev471/Dev471 Smoke Fixture (2008)` |
| Source media | `docker/sample-library/Big Buck Bunny (2008).mp4` (reused, not duplicated) |
| Media bytes / SHA-256 | 202927 / `3a82a333fe6127cc03bcdbc4e3aff69a6c99abcd34a9a14d60e870d9ced8de07` |
| Decodability basis | T001 Chromium probe, `recon-DEV-471:12-35` (exit 0, PASS) |
| Stub enrichment identity | `Dev471 Smoke Fixture`, 2008, `tt9904711`, 96 min, 7.3, Animation/Comedy |
| Budget config | `web/e2e/fixtures/dev471/budgets.json` untouched (T002-owned) |

## Assertions discharged (T006)

All assertions are fixture-correlated. Only external providers (OMDb-shaped HTTP and the
Ollama embedding endpoint) are stubbed by the unchanged `web/e2e/compose.e2e.yml` overlay plus
`web/e2e/stub.mjs`. No application API or hub response is intercepted or mocked.

| Contract item | Evidence |
|---|---|
| `/hubs/` established through shipped nginx **before** import | `/hubs/enrichment/negotiate` 200 recorded, WebSocket opened on the nginx origin, then import |
| Import through the real stack | `POST /api/movies/import/library` 200, `imported: 1`, observed `Pending` row in the UI grid before release |
| Persisted library listing | `GET /api/movies` returns exactly one record for the fixture title |
| Deterministic stub enrichment fields | `enrichmentStatus: Enriched`, year 2008, rating 7.3, runtime 96 min, all from stub-defined values |
| Genuine server-originated hub update | a real `EnrichmentStatusChanged` frame for the fixture id arrived on the in-page hub socket, and **no** library listing response ever reported the fixture as already-`Enriched`, so a refetch or poll cannot stand in for the hub event; no page reload occurred |
| Nonempty decodable thumbnail | `GET /api/movies/<id>/thumbnail` 200 `image/jpeg`, nonempty body, decoded in the real browser via `createImageBitmap` with width/height > 0 |
| In-page playback through shipped nginx | player region visible, `videoWidth`/`videoHeight` > 0, finite duration > 0, `currentTime` advanced while playing; all stream responses 200/206 |
| LocalPlay false and active process-play 403 | `localPlayEnabled: false`; `POST /api/movies/<id>/play` through nginx returned **403** with no process launch; the local play control is absent |

## Thumbnail cache ruling applied

The Patron ruling at `specs/DEV-471/CONCLUSIONS.md` ("Phase 3 Ruling") approved setting
`Thumbnails:CacheDirectory` to `/tmp/thumbnails` in the ephemeral runtime test settings via the
existing `E2E_SETTINGS_FILE` seam. `web/e2e/run.mjs` generates that section and fails closed if
the value is not absolute. `docker-compose.yml`, `web/e2e/compose.e2e.yml` and
`docker/Dockerfile.api` are unchanged. Without it the unprivileged API container cannot write
the default relative cache under `/app`, and every thumbnail fails with `StorageError` — the
defect routed to a separate follow-up ticket under `DEV-492`.

## Ordinary-e2e exclusion and strict exit (T007)

| Check | Result |
|---|---|
| Required smoke entrypoint (`E2E_SMOKE_MODE=dev471`) | selects only `dev471-compose-smoke.e2e.ts` — 1 test, passed |
| Ordinary `npm run e2e` (no smoke mode) | 22 tests, all passed, DEV471 **not** selected |
| Missing DEV-471 smoke context | spec fails closed with an explicit error rather than skipping |
| Phase-2 runner coordination | `scripts/run-compose-smoke.ps1` runs phase 1 then phase 2 under a second randomized project; both PASS |

## End-to-end runner executions

Two consecutive runs of `pwsh -NoProfile -File scripts/run-compose-smoke.ps1`, both exit 0:

```text
[Candidate] PASS: plain shipped startup reached API health after dependency and model readiness
[Candidate] tore down project-scoped stack lamuflix-smoke-plain-<random>
[Stubbed flow] PASS: fixture-correlated import, stub enrichment, listing, thumbnail,
               hub update, advancing browser play and process-play 403
project lamuflix-smoke-flow-<random> already has no containers; teardown satisfied
verdict: PASS
```

The PR checkout baseline was unchanged across both runs (recorded by the runner), and each run
used a randomized Compose project registered before startup and torn down afterwards.

## Gates

| Gate | Exit | Verdict |
|---|---|---|
| `npm run typecheck:e2e` | 0 | PASS |
| `eslint e2e --max-warnings 0` | 0 | PASS |
| `prettier --check` (edited files) | 0 | PASS |
| `scripts/run-web-gates.ps1 -All` | 0 | PASS (lint, tsc, vitest, build) |
| Roslyn / cyclomatic / InspectCode | 2 | SKIPPED (scope-empty: no `.cs` changed) |
| `dotnet test` | not run | N/A for Phase 3 (no C# changed) |

## Limits

Runtime acceptance evidence only, for the stubbed overlay stack. This is not the dedicated CI
workflow (Phase 4), not the failure-path/planted-secret/existing-runner proofs (Phase 5), not the
`T010` final-candidate `-RegressionProof` (which supersedes developmental `T004`), and not the
operator required-check verification (`T011`). `SC-006`/`T011` remain open and keep Done and the
implementation merge-bar non-PASS.
