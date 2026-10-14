# Implementation Plan: Docker Compose stack (DEV-325)

**Branch**: `feature/325-spec` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`, `brief.md`, `CONCLUSIONS.md` (C1-C15), `ASSUMPTIONS.md`, recon-DEV-325 and recon-DEV-325-2. C1 option A (no worker) is the owner answer recorded at DEV-325:32,36; this is not a completed Gate 1 or plan freeze. `brief.md` carries the C15 database-name ruling, Keel C3 receipt and C16/C17 correction receipt; Gate 1 and plan freeze remain incomplete.

## Summary

Add a root `docker-compose.yml` with postgres, rabbitmq, api, web; healthchecks and `service_healthy` ordering; secrets via `.env`; a committed placeholder sample library; an opt-in Api migrate-on-startup option; one README section. Sequenced after DEV-21 Phase B merges (C2).

## Technical Context

**Language/Version**: C# on .NET 10 (Api); Compose YAML; Vite/React SPA served by nginx (DEV-21, untouched).

**Primary Dependencies**: none added (images only: `postgres:17-alpine`, `rabbitmq:4-alpine`; EF Core 10 / Npgsql already present).

**Storage**: PostgreSQL 17; existing EF Core migrations applied unchanged (C3).

**Testing**: xUnit (existing) for option default/binding; integration startup test (`DatabaseStartupTests`) for absent/false/true startup (see C3 section); live compose evidence for AC1/AC2 and the true path.

**Target Platform**: Docker Compose on developer host.

**Constraints**: no Dockerfile edits, no new dependency, no schema change, no secret defaults.

## Constitution Check

- Configuration isolation (Principle VII): library root config-only via `Library__RootPath`; no hard-coded host path.
- Secrets (Principle VII): `${VAR:?message}`, placeholders only in `.env.example`.
- LocalPlay (Principle VII): `Features:LocalPlay=false` in compose; Process.Start untouched.
- Worker health signal (Principle VI, constitution :255): covered by api `/health/ready` under C1-A.
- README compose instructions (Documentation Rules, :483): satisfied by one section.
- Observability compose file (Technology Stack Constraints, :336) deferred per C10 (noted, no ticket).

## Project Structure

Files touched (frozen set; anything else is `needs decision` to Keel):

```text
docker-compose.yml                                     (new, root)
.env.example                                           (new, root)
docker/sample-library/                                 (new, 3-5 placeholder files)
README.md                                              (one section added)
src/LamuFlix.Core/Options/DatabaseOptions.cs           (new, the single options type)
src/LamuFlix.Api/Program.cs                            (option registration + gated migrate)
tests/LamuFlix.UnitTests/DatabaseOptionsTests.cs       (new, default and binding test)
tests/LamuFlix.IntegrationTests/DatabaseStartupTests.cs  (new, absent/false/true startup gate test)
```

> [!NOTE]
> The C3 limit is "Api startup path plus at most one options type". Option binding and the gated `Database.MigrateAsync` live in `Program.cs`; if a separate Api extension file proves necessary for the complexity gate, that is `needs decision` to Keel. No change to Infrastructure, ServiceDefaults or migrations. Program.cs reuses the existing shared options pattern (BindConfiguration with ValidateDataAnnotations and ValidateOnStart, recon-DEV-325:42,47) and does not duplicate or edit ServiceDefaults ownership (constitution VI/VII).

## Design

### Compose services

| Service | Source | Healthcheck | depends_on | Ports |
|---|---|---|---|---|
| postgres | `postgres:17-alpine` | `pg_isready` | none | none |
| rabbitmq | `rabbitmq:4-alpine` | `rabbitmq-diagnostics -q ping` | none | none |
| api | `build` context `.`, `docker/Dockerfile.api` | `GET /health/ready`; select only after C2-dependent image-tool inventory, exact exec-form invocation and positive/negative proof | postgres, rabbitmq `service_healthy` | none |
| web | `build` context `.`, `docker/Dockerfile.web` | `wget -q --spider http://127.0.0.1:8080/` | api `service_healthy` | `3000:8080` |

No `worker` service (C1-A). No registry `image:` for api/web. No OTLP endpoint env, no collector.

### Environment

Env-variable set used identically in `.env.example`, compose, and tasks (placeholders only in the example; no defaults in compose; no literal secrets or secret output anywhere):

| Variable | Class | Compose use |
|---|---|---|
| `OMDB_API_KEY` | secret | `Omdb__ApiKey: ${OMDB_API_KEY:?message}` |
| `POSTGRES_USER` | secret | postgres `POSTGRES_USER: ${POSTGRES_USER:?message}`; api `PGUSER: ${POSTGRES_USER:?message}` |
| `POSTGRES_PASSWORD` | secret | postgres `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?message}`; api `PGPASSWORD: ${POSTGRES_PASSWORD:?message}` |
| `RABBITMQ_USER` | secret | rabbitmq `RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:?message}` and api `RabbitMq__UserName: ${RABBITMQ_USER:?message}` |
| `RABBITMQ_PASSWORD` | secret | rabbitmq `RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:?message}` and api `RabbitMq__Password: ${RABBITMQ_PASSWORD:?message}` |
| `LIBRARY_PATH` | optional non-secret | mount source override, default `./docker/sample-library` |

- RabbitMQ credentials: api `RabbitMq__UserName` and `RabbitMq__Password` map to the SAME supplied values as the rabbitmq service, required interpolation, no defaults. Per recon-DEV-325:47 neither option is `[Required]` and `Validate()` checks only `RetryDelay`, so a missing credential is NOT caught by ValidateOnStart; the compose `${VAR:?message}` interpolation is the only fail-fast guard.
- Connection string: `ConnectionStrings__DefaultConnection=Host=postgres;Database=lamuflix` is a non-secret literal. Credentials are delivered separately to the api as `PGUSER: ${POSTGRES_USER:?message}` and `PGPASSWORD: ${POSTGRES_PASSWORD:?message}`; the postgres service receives those same supplied values through its required mappings. No credential composition or encoding is used (C16). Phase B must verify credential delivery through the real Api/Postgres connection without printing credentials or resolved secret-bearing configuration; this is future evidence, not a current success claim.
- Non-secret options (not secrets): `Features__LocalPlay=false`, `Library__RootPath=/media/library`, `Database__ApplyMigrationsOnStartup=true`, `RabbitMq__HostName=rabbitmq`, `Enrichment__SweepInterval`, `Enrichment__ClaimLease`, `RabbitMq__RetryDelay`.
- Timing constraints from recon-DEV-325:48-49: `Enrichment__SweepInterval` and `Enrichment__ClaimLease` MUST be greater than zero; `RabbitMq__RetryDelay` MUST be strictly greater than `Enrichment__ClaimLease` when the consumer is active. The concrete values are not recorded in the brief, so they remain an implementation choice within these constraints and are not a frozen decision.
- Every successful-start run requires: copy `.env.example` to `.env` AND manually replace every placeholder with a local supplied value before running compose. Required interpolation is not specified to detect unreplaced placeholders; no automatic placeholder detection/rejection is added (P-F3 rejected).
### Sample library

`docker/sample-library/` holds 3-5 tiny `Title (Year).ext` placeholder files, under 100 KB total. Mount `${LIBRARY_PATH:-./docker/sample-library}:/media/library:ro`.

### Migrate-on-startup (C3)

- `DatabaseOptions` record in `LamuFlix.Core.Options`: `SectionName = "Database"`, `ApplyMigrationsOnStartup` bool, default `false`.
- `Program.cs` binds it through the existing shared options pattern with `ValidateOnStart` and, after `Build()`, applies `LamuFlixDbContext` migrations only when the option is `true`.
- Code default stays `false`; only compose sets `true`; host-run path unchanged.
- Tests (no mocked database driver, no new dependency):
  - Default/binding unit test (evidence of default and binding only, NOT of startup behaviour): new `tests/LamuFlix.UnitTests/DatabaseOptionsTests.cs` asserts default `false` and binding from `Database:ApplyMigrationsOnStartup` via in-memory configuration.
  - Startup integration test (evidence of real Api startup behaviour for absent, false and true; Keel C3 receipt, recon-DEV-325:121-136,154): new `tests/LamuFlix.IntegrationTests/DatabaseStartupTests.cs`. The existing `ApiHostFactory` boots the real `Program`; the Postgres collection/fixture supplies one fresh empty database per absent, false and true case, and its context connection string is injected via `additionalSettings`. The existing harness removes the sweeper by default; the test MUST explicitly pass `disableEnrichmentConsumer: true`. The test calls `CreateClient` and completes `/health/live` before observing the catalog through a separate context, querying `information_schema` public BASE TABLEs before and after. Absent and false: catalog stays empty, including no `__EFMigrationsHistory`. True: existing history and application tables are created (positive control). Stubbed health is startup proof only; real fresh-volume compose readiness is still required. No pre-migrated end-to-end fixture, copied gate, mocked driver, new seam, or assumed `GetAppliedMigrationsAsync` absent-history behaviour.
  - Live true path: real fresh-volume compose run (api reaches healthy; existing migrations applied).
### README

One section `Run with Docker Compose`, written last, describing only what the frozen compose file supports.

## Evidence plan

- Live: `docker compose up -d` on fresh volumes, then `docker compose ps` (all healthy, no collector), then both C6 curls; raw output recorded.
- C3: unit binding test proves default and binding only; `DatabaseStartupTests` integration test proves absent/false/true startup (run as ordinary Anvil tests; gates stay Gauge); live compose proves the true path by api healthy on a fresh volume.
- C1 evidence: raw default sample mount, read-only container mount (`RW` false) and `LIBRARY_PATH` override output; HTTP 200 status plus SPA and JSON-array body evidence for both C6 curls; no collector and all four services healthy; missing-secret check with the variable removed from both `.env` and inherited environment. Verify C16 credential delivery through the real Api/Postgres connection without printing credentials or resolved secret-bearing configuration; this remains future evidence. No resolved secret-bearing config or credentials in receipts.
- C17 api-probe evidence before T008: after C2 prerequisites, Wisp performs the read-only inventory of tools in the actual DEV-21 api image, establishes the exact exec-form invocation (including the explicit interpreter if used), and records positive and negative proof of the selected `GET /health/ready` probe as a handoff to Anvil. Use existing image tools only; if no viable probe exists, stop and route to Patron. No binary or command is selected in this plan before that evidence.
- Structural greps for SC-003.
- Final gates by Gauge on the final diff, each by its own verdict: `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1` (no arguments, never `-All`), `run-mutation.ps1`, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, `run-web-gates.ps1`. Mutation: exit 0 PASSED, exit 1 FAILED blocks, exit 2 NOT APPLICABLE reported as N/A, exit 2 SKIPPED reported `SKIPPED (scope-empty)`, harness-disabled opt-out reported `SKIP`; never PASS. Property tests: exit 2 blocks unless the task note records `propertyTests: opt-out - <reason>`. Web gates: exit 2 `SKIPPED (scope-empty)` when /web is unchanged. Exit 1 or `Could not run` blocks. Changed C# gates are mandatory; live compose evidence is additional, not a substitute.
- Execution routing: Wisp for read-only prerequisite and evidence phases, Anvil one phase per ask for code phases (ordinary targeted TDD tests only), Gauge only for gate runs.
## Complexity Tracking

No constitution violations to justify.

