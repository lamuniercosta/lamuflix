# DEV-388 Delivery Evidence Map (T024)

Owned by Phase 6 polish. Maps each ticket acceptance criterion and SC-001 through SC-007
to the explicit test, inventory, or gate evidence produced by T003 through T023.
This artifact adds no behavior.

- Worktree: `F:/Dev/LamuFlix.worktrees/DEV-388`
- Branch: `feature/DEV-388`
- Intake HEAD: `48a94c38d2c0d7b0a59d967e66987b7607adda62`
- Delivery gates HEAD: Phase 5 commit `89d9d99` plus the Phase 6 polish diff.

## Gate results (T022)

| Gate | Command | Result |
| --- | --- | --- |
| Solution build | `dotnet build LamuFlix.sln` | PASS (0 warnings, 0 errors) |
| Architecture tests | `dotnet test` | PASS 13/13 |
| Unit tests | `dotnet test` | PASS 689/689 |
| Integration tests | `dotnet test` | 389 passed, 2 failed, 1 skipped of 392 |
| Format | `dotnet format --verify-no-changes` | PASS (exit 0) |
| Roslyn analyzers | `./scripts/run-roslyn-analyzers.ps1` | PASS (diff-scoped, exit 0) |
| Cyclomatic complexity | `./scripts/run-cyclomatic-complexity.ps1` | PASS (diff-scoped, exit 0) |
| Vulnerable packages | `./scripts/run-vulnerable-packages.ps1` | PASS (no vulnerable packages) |
| Web gates | `./scripts/run-web-gates.ps1` | lint/tsc/build PASS; `vitest run` flaky (see below) |

### Integration failures

1. `ApiHostCompositionTests.Routes_StartedComposition_ExposeTheLivenessRouteAndBothLibraryBusinessRoutes`
   (deterministic, DEV-388-caused). The DELETE endpoint is mapped as
   `MapDelete("/movies/{id}", ...)` at `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs:55`, so the
   route manifest exposes `/api/movies/{id}` instead of the frozen spec's
   `DELETE /api/movies/{id:int}` (`spec.md` FR-001, `tasks.md` T008). The pre-existing manifest test
   was never updated for the added route. This is a real contract deviation, not a test-only issue:
   the spec route text (`{id:int}`) and FR-004 (non-integer id returns 400) are mutually
   contradictory, matching GET's measured behavior (`GetMovieDetails_NonIntegerId_MissesTheRouteAndReturnsAProblemBody`).
   Delivery is stopped here; resolution (route text vs. FR-004) needs a Patron ruling and a
   gated fix outside Phase 6 scope.
2. `DatabaseStartupTests.ApplyMigrationsOnStartupSetting_DeterminesStartupMigrationOutcome(null, False)`
   (environment-caused). Reproduced deterministically in isolation. The developer machine's
   UserSecrets file (`%APPDATA%\Microsoft\UserSecrets\fa573ff3-1b02-4f5b-95b9-f718a4ce2b67\secrets.json`)
   sets `Database:ApplyMigrationsOnStartup: true`; `WebApplicationFactory` runs the host in Development,
   so the unset case applies migrations. CI has no user secrets, so this does not reproduce there. No
   repository change.

### Web flakiness

`vitest run` over the full suite reported 1-2 failures across runs
(`src/library/LibraryView.retry.test.tsx`, `src/library/watchlist.integration.test.tsx`) with
`Unable to find role="button"` timeouts under parallel jsdom load. Both files pass in isolation
(15/15, `npx vitest run <files>`). No DEV-388 file is implicated. No repository change.

## Success criteria mapping

| Criterion | Evidence |
| --- | --- |
| SC-001 present id 204 empty body; repeat/unknown 404; file remains | `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`: `DeleteMovie_PresentId_ReturnsNoContentWithEmptyBodyAndNoRequestPayload`, `DeleteMovie_AbsentId_ReturnsNotFoundProblem`; `EfMovieRepositoryDeleteTests.TryDeleteAsync_RepeatDelete_ReportsAbsentAndPreservesOtherMovieAndFile` |
| SC-002 list/watchlist absence; shared principals and another movie survive; three join sets gone | `EfMovieRepositoryDeleteTests.TryDeleteAsync_RemovesRowAndJoins_WhileSharedPrincipalsOtherMovieAndFileRemain`; `TryDeleteAsync_OverlappingSameIdDeletes_ExactlyOneRemovesTheRow` |
| SC-003 SPA cancel/404/other-failure/success behavior | `web/src/features/movie-details/MovieDetailsView.delete.test.tsx` (cancel no request, single in-flight submit, non-404 error + retry, 404 confirmed absence no error/retry with invalidated caches, success returns to library) |
| SC-004 each ticket AC action has an Api endpoint and SPA screen | T016 parity map (Phase 5 receipt); `ApiHostCompositionTests` business-route manifest (blocked by the route deviation above) |
| SC-005 build and configured tests pass with hosts absent; no Pomelo/UseMySql/LamuFlix.Data; no Web/Worker reference | T021 inventory: zero matches under `src/` and `tests/`; no Web/Worker csproj. Build PASS. **Tests: BLOCKED** by integration failure 1 |
| SC-006 Docker/compose/CI reference no retired hosts; SPA image build remains | T020: `docker/Dockerfile.web` is the Node+nginx SPA build (`ci.yml` container-build job); compose/CI clean |
| SC-007 diff-scoped analyzer gates, test suites, pre-PR mutation gate run; skips labeled | Gate table above. Mutation gate is diff-scoped to Core; the DEV-388 diff has no Core file, so it reports `SKIPPED (scope-empty)`. **BLOCKED** by integration failure 1 |

## Ticket acceptance criteria mapping

| Ticket AC | Evidence |
| --- | --- |
| MoviesController parity (the actions the ticket names) | T016 parity map; SPA screens; delete slice tests; new `DELETE /api/movies/{id}` endpoint. Route-pattern deviation flagged above |
| Clean build | `dotnet build LamuFlix.sln` PASS, 0 warnings/0 errors |
| No Docker/CI reference to retired hosts | T020: sln/docker/compose/CI clean; `docker/Dockerfile.web` is the SPA build |
| No Pomelo / LamuFlix.Data | T021: zero references under `src/` and `tests/` |

## Inventories (T020, T021)

- `LamuFlix.sln`, `docker/`, `docker-compose.yml`, `docker-compose.observability.yml`,
  `.github/workflows/ci.yml`: no `LamuFlix.Web`, `LamuFlix.Worker`, `LamuFlix.Data`, or `Pomelo`
  reference. `docker/Dockerfile.web` remains the React SPA build. Files not edited.
- `src/` and `tests/`: zero `Pomelo`, `UseMySql`, `LamuFlix.Data`, `LamuFlix.Web`, `LamuFlix.Worker`
  matches. No Web or Worker project file. `tests/LamuFlix.Test` retained outside the solution.

## Diff boundary (T023)

- `harness.yml`, `.specify/memory/constitution.md`, and
  `src/LamuFlix.Infrastructure/Persistence/Migrations/` are excluded from the DEV-388 diff.
- `docs/adr/0021-retire-legacy-web-and-worker-hosts.md`: only the status line changed, to `Accepted`.
- DEV-370 and DEV-385 are not closed by this change.
