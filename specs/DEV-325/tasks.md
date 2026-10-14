# Tasks: Docker Compose stack (DEV-325)

**Input**: `specs/DEV-325/spec.md`, `plan.md`, `brief.md`

**Format**: `[ID] [P?] [Story] Description` (`[P]` = parallel, different files)

**Routing (task-pipeline Phase 3)**: read-only prerequisite and evidence phases go to Wisp; code phases go to Anvil one phase per ask with a phase Checkpoint; gate-only tasks go to Gauge. Tasks are not executed by this draft.

**Ordering constraints (brief)**:
- Phase B does not start until DEV-21 Phase B has merged and `docker/Dockerfile.api` / `docker/Dockerfile.web` exist on disk (C2).
- The C3 wiring and compose inputs land before any compose acceptance run.
- `.env.example` and sample placeholders exist before the acceptance run; the local `.env` is never committed.
- README is written last among authored implementation files.
- No task may add a worker service, observability compose file, collector, dependency, schema change, Dockerfile edit, or tracked-file rewrite beyond the frozen set.
- No literal secret and no secret-bearing output (resolved compose config, credentials) in any file, log or receipt.

**Env-variable set (all tasks)**: `OMDB_API_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD` (secrets); `LIBRARY_PATH` (optional, non-secret).

## Phase 0: Prerequisite (Wisp, read-only)

- [ ] T000 Confirm DEV-21 Phase B is merged and `docker/Dockerfile.api` / `docker/Dockerfile.web` are on disk. If not, stop and report. Checkpoint: report of the merge state and the file existence check.

**Checkpoint (Phase 0 evidence handoff, Wisp; read-only)**: after T000 confirms the C2 prerequisites, inventory tools in the actual DEV-21 api image, establish the exact exec-form `GET /health/ready` invocation (including an explicit interpreter if used), and record positive and negative proof. Hand off the evidence to Anvil before T008; use existing image tools only. If no viable probe exists, stop and route to Patron. No new task ID or task reordering.

## Phase 1: C3 migrate-on-startup (Anvil; blocks acceptance)

- [X] T001 [US1] Write failing xUnit tests (AAA) in new `tests/LamuFlix.UnitTests/DatabaseOptionsTests.cs`: default `ApplyMigrationsOnStartup` is `false`; binds from `Database:ApplyMigrationsOnStartup` via in-memory configuration. This is default/binding evidence only, not evidence of startup behaviour. Also write failing xUnit tests (AAA) in new `tests/LamuFlix.IntegrationTests/DatabaseStartupTests.cs` (Keel C3 receipt, recon-DEV-325:121-136,154): reuse `ApiHostFactory` booting the real `Program`; the Postgres collection/fixture supplies one fresh empty database per absent, false and true case, injected via `additionalSettings`; existing harness removes the sweeper by default, and the test MUST explicitly pass `disableEnrichmentConsumer: true`; `CreateClient` and complete `/health/live` before observing the catalog through a separate context; query `information_schema` public BASE TABLEs before and after. Absent and false stay empty (no `__EFMigrationsHistory`); true creates the existing history and application tables (positive control). Stubbed health is startup proof only; real fresh-volume compose readiness is still required. No pre-migrated end-to-end fixture, copied gate, mocked driver, new seam, or assumed `GetAppliedMigrationsAsync` absent-history behaviour.
- [X] T002 [US1] Add `src/LamuFlix.Core/Options/DatabaseOptions.cs` (`SectionName = "Database"`, bool default `false`).
- [X] T003 [US1] Edit `src/LamuFlix.Api/Program.cs`: bind `DatabaseOptions` through the existing shared BindConfiguration, ValidateDataAnnotations and ValidateOnStart pattern (no ServiceDefaults edit); after `Build()`, apply existing EF Core migrations unchanged only when `true`. No migration, schema, Infrastructure or ServiceDefaults change.
- [X] T004 [US1] Run the targeted tests for T001 (`dotnet test --filter` on `DatabaseOptionsTests` and `DatabaseStartupTests`, run as ordinary Anvil tests; gates stay Gauge) and the existing unit tests until green. No analyzer, complexity or inspection gate run in this phase.

**Checkpoint (Phase 1)**: targeted `DatabaseOptionsTests` and `DatabaseStartupTests` pass and `dotnet test` passes locally; only `DatabaseOptions.cs`, `Program.cs`, `DatabaseOptionsTests.cs` and `DatabaseStartupTests.cs` changed; tasks T001-T004 marked [X]; commit `DEV-325 - C3 migrate-on-startup option`, report the SHA, stop.

## Phase 2: Compose inputs (Anvil)

- [X] T005 [P] [US4] Create `.env.example` with placeholders only for `OMDB_API_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, plus an optional `LIBRARY_PATH` note. Note in the file that the example must be copied to `.env` and every placeholder replaced with local supplied values; no literal secret. Every placeholder value is shorter than 8 characters (for example `change`) so the pre-commit secret scan passes; no scanner exclusion.
- [X] T006 [P] [US3] Create `docker/sample-library/` with 3-5 `Title (Year).ext` placeholder files, total under 100 KB, no real media. If a scanner check fails, stop with `needs recon`.

**Checkpoint (Phase 2)**: `.env.example` contains exactly the listed names with placeholder values; `docker/sample-library/` has 3-5 files under 100 KB total; no `.env` tracked; T005-T006 marked [X]; commit `DEV-325 - Compose inputs`, report the SHA, stop.

## Phase 3: Compose file (Anvil)

- [X] T007 [US1] Create root `docker-compose.yml`: postgres (`postgres:17-alpine`, `pg_isready`), rabbitmq (`rabbitmq:4-alpine`, `rabbitmq-diagnostics -q ping`), api and web (`build:` root context, DEV-21 Dockerfiles), api `depends_on` both `service_healthy`, web `depends_on` api `service_healthy`.
- [X] T008 [US1] Consume the Wisp read-only C2-dependent evidence handoff from Phase 0: select and add the proven api healthcheck using its recorded exact exec-form invocation and positive/negative proof. Preserve `GET /health/ready` and use existing image tools only. If no viable existing-tool probe is available, stop and route to Patron; do not add tooling, edit Dockerfiles or change endpoint/scope (C2, C7, C17). Add the existing web healthcheck per FR-005; no Dockerfile edit.
- [X] T009 [US4] Add environment with the consistent variable set: `POSTGRES_USER`/`POSTGRES_PASSWORD` map to postgres `POSTGRES_USER`/`POSTGRES_PASSWORD` and api `PGUSER: ${POSTGRES_USER:?message}`/`PGPASSWORD: ${POSTGRES_PASSWORD:?message}`; api `ConnectionStrings__DefaultConnection` is the non-secret literal `Host=postgres;Database=lamuflix`, with no credential composition or encoding (C16), and postgres sets `POSTGRES_DB=lamuflix` (C15). Map `RABBITMQ_USER`/`RABBITMQ_PASSWORD` both to the rabbitmq service (`RABBITMQ_DEFAULT_USER`/`RABBITMQ_DEFAULT_PASS`) and to api `RabbitMq__UserName`/`RabbitMq__Password` (same supplied values; credentials are not `[Required]` and not caught by ValidateOnStart, recon-DEV-325:47); map `OMDB_API_KEY` to `Omdb__ApiKey`; every secret as `${VAR:?message}` with no default. Non-secret: `Features__LocalPlay=false`, `Database__ApplyMigrationsOnStartup=true`, `RabbitMq__HostName=rabbitmq`, `Enrichment__SweepInterval` and `Enrichment__ClaimLease` greater than zero, `RabbitMq__RetryDelay` strictly greater than `Enrichment__ClaimLease` (recon-DEV-325:48-49). Phase B acceptance must verify credential delivery through the real Api/Postgres connection without printing credentials or resolved secret-bearing configuration; this is future evidence, not a current success claim.
- [X] T010 [US3] Add library mount `${LIBRARY_PATH:-./docker/sample-library}:/media/library:ro` and `Library__RootPath=/media/library`.
- [X] T011 [US2] Publish `3000:8080` on web only; no api host port; no OTLP endpoint env; no worker service.

**Checkpoint (Phase 3)**: structural greps over `docker-compose.yml` pass (four services, no `worker`, no `latest`, no api `ports`, no secret default, mount and `Library__RootPath` present); `docker compose config --quiet` exits as expected without printing resolved config; T007-T011 marked [X]; commit `DEV-325 - Compose file`, report the SHA, stop.

## Phase 4: Acceptance evidence (Wisp; raw output recorded, no secret output)

- [ ] T012 [US1] Copy `.env.example` to an uncommitted `.env` AND manually replace every placeholder with local supplied values before running compose; required interpolation is not specified to detect unreplaced placeholders (P-F3 rejected). Run `docker compose up -d` on a fresh volume with no collector. Record `docker compose ps` showing all four services healthy (AC1, C3, C12) and confirm no collector service exists. Verify C16 credential delivery through the real Api/Postgres connection without printing credentials or resolved secret-bearing configuration; this is future evidence, not a current success claim. Do not print resolved compose config or credentials.
- [ ] T013 [US2] Record the frozen `curl -fsS http://localhost:3000/` and `curl -fsS http://localhost:3000/api/genres` outputs, supplemented with status evidence (for example `curl -sS -o NUL -w "%{http_code}"` per URL) showing HTTP 200, the SPA index body, and a JSON-array body for `/api/genres` (AC2, C6).
- [ ] T014 [US4] Run SC-003 structural checks: no `worker` service, no secret default, no committed `.env`, placeholders only, tags per C13 without `latest`, no api host port. Record explicit library evidence: default mount source is `./docker/sample-library`; the container mount is read-only (`docker inspect` mounts show `RW` false, no environment printed) and lists the placeholders; a `LIBRARY_PATH` override changes the mount source. Missing-secret test: for each tested variable, remove it from both the local `.env` input and the inherited environment so supplied values cannot mask failure, run `docker compose config --quiet`, and record only the exit code and the `${VAR:?message}` text.
- [ ] T015 Report any unmet condition as `needs decision` (OTLP blocking health) or `needs recon` (placeholders); do not work around.

**Checkpoint (Phase 4)**: raw receipts for T012-T015 recorded in the task note with no secret values; read-only, nothing committed.

## Phase 5: README (Anvil, last authored implementation file)

- [ ] T016 [US1] Add to `README.md` one section `Run with Docker Compose`: copy `.env.example` to `.env` and replace placeholders with local supplied values, `docker compose up -d`, open http://localhost:3000, `LIBRARY_PATH` override. No other README change.

**Checkpoint (Phase 5)**: only `README.md` changed with one added section describing what the compose file supports; T016 marked [X]; commit `DEV-325 - README compose section`, report the SHA, stop.

## Phase 6: Gates (Gauge only, final diff)

- [ ] T017 Gauge runs the final gate set on the final diff, each reported by its own verdict: `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1` (no arguments, never `-All`), `run-mutation.ps1` (exit 0 PASSED; exit 1 FAILED blocks; exit 2 NOT APPLICABLE reported as N/A; exit 2 SKIPPED reported `SKIPPED (scope-empty)`; harness-disabled opt-out reported `SKIP`; never PASS), `run-property-tests.ps1` (exit 2 blocks unless the task note records `propertyTests: opt-out - <reason>`), `run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, `run-web-gates.ps1` (exit 2 reported `SKIPPED (scope-empty)` when /web is unchanged). Exit 1 or `Could not run` blocks. Changed C# gates are mandatory; Phase 4 acceptance evidence is additional, not a substitute.

## Dependencies

T000 -> Phase 1 (T001..T004) -> Phase 2 (T005, T006 parallel) -> Phase 3 (T007..T011) -> Phase 4 (T012..T015, Wisp) -> Phase 5 (T016) -> Phase 6 (T017).
