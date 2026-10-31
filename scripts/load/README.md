# DEV-427 manual movie-library load baseline

Runbook for the manual `GET /api/movies` load baseline. Manual only: this suite is not
enrolled in CI. Scope is frozen by ticket DEV-427 and `specs/DEV-427/` (`spec.md` FR-001 to
FR-012, `plan.md`, `tasks.md`, `brief.md`).

**Status: skeleton (tasks.md Phase 1).** Sections marked *(pending)* are completed by their
named tasks; this file must not claim a baseline, gate result, or acceptance outcome before
those tasks have produced real receipts.

## Scope and files

Tracked delivery is exactly three files:

- `scripts/load/seed.sql` - deterministic idempotent 500-movie fixture *(pending T002)*
- `scripts/load/movies.js` - k6 workload, preflight/checks, tagged measurement, summary JSON
  *(pending T004-T008)*
- `scripts/load/README.md` - this runbook

No tracked compose, CI, API, schema, manifest, generated-contract, or harness file is edited.
No evaluated migration or schema definition is authored. The unrelated existing `harness.yml`
modification in the worktree is preserved and is not part of this delivery. Runtime output
under the ignored `artifacts/` root stays untracked.

`.agents/skills/k6-load-testing/SKILL.md` is implementation guidance only. The ticket/frozen
metrics, product dependencies, and manual-only scope prevail over any example there.

## Execution model: Docker only

- Everything runs through Docker. A host `k6` install and a host `psql` install are not
  required and must not be added.
- No NuGet, npm, or package-manifest change is needed for this delivery.
- Seeding and SQL verification run in-container: `docker compose exec -T postgres psql`.
- Load runs use a digest-pinned `grafana/k6` image, referenced as `image@sha256:...`. A version
  tag may be shown as a label only, never as the immutable identity. The exact verified digest
  is selected and recorded during implementation *(pending T009)*.
- The host-facing target is compose nginx at `http://localhost:3000` (the `web` service
  publishes `3000:8080`). The script supports a `BASE_URL` override. How the k6 container
  reaches that same nginx entry through a verified Docker host/network mapping is documented
  in *(pending T006)*; container `localhost` must never be assumed to reach the host.
- `artifacts/load/` is bind-mounted for per-run summary JSON. It is already covered by the
  root `artifacts/` ignore, so runtime output is never committed.

### Credentials

Credentials come only from existing environment/private runtime configuration (compose env
and the operator's ignored `docker/api.settings.json`). Never put credentials in a command
line literal, shell history, log line, receipt, or committed file. Operator receipts redact
sensitive values.

## Isolated database provisioning (recon Finding 4 B) - outline

The acceptance dataset must be exactly 500 deterministic movies, not 500 seed rows added to
an arbitrary library. A mixed or contaminated library is not an acceptance baseline. The
operator's existing `lamuflix` library is never deleted or modified.

1. Create a separately named disposable database inside the existing compose postgres
   container using credential-safe `docker compose exec -T postgres psql`. `POSTGRES_DB`
   stays `lamuflix`; the disposable database is a second database in the same container.
2. Select it only through the ignored operator-local `docker/api.settings.json` by changing
   `Database=` on `ConnectionStrings:DefaultConnection` to the disposable database name. Keep
   `Database.ApplyMigrationsOnStartup=true`. Edit the existing file in place: the compose
   single-file bind mount can keep serving a stale inode if the file is replaced.
3. Restart `api` so the already-committed migrations apply to the empty disposable database.
   Verify they actually ran (for example a `__EFMigrationsHistory` row count) instead of
   trusting the restart.
4. Before the first insert, prove compose project/container/database identity, prove the API
   and the SQL session target the same database (for example `current_database()`), and assert
   the `movies` table is empty or already contains exactly the seed set. Seed transactionally
   where practical.
5. Announce the temporary redirection: while `docker/api.settings.json` is retargeted, the
   compose UI and API serve the disposable fixture, not the operator's real library.
6. Preserve the operator's original settings privately first, and restore them plus restart
   `api` on success and on failure.
7. Do not reuse the DEV-334 Playwright runner or its helpers, and do not add helper scripts
   beyond the three authorized files.

## Seed and repeat-seed assertions *(pending T003)*

`scripts/load/seed.sql` inserts `i=1..500` with `library_path=/dev427/movie-NNNN.mp4`,
`title=DEV427 GroupGG MovieNNNN`, `format=mp4`, `is_in_watchlist=false`, `status=1`,
`enrichment_attempts=0`, and `ON CONFLICT (library_path) DO NOTHING`; no OMDb identifiers or
calls. The exact credential-safe run command and redacted receipt examples are added in T003.

Seed runs twice without deleting rows. After each run the assertions are:

- exactly 500 total movies;
- exactly 500 distinct `/dev427/movie-%.mp4` seed paths;
- exactly 50 movies in each of Group00 through Group09.

## Workload and preflight *(pending T003-T006)*

Four browse requests then one search per VU cycle, one request per iteration, 1s sleep after
each request, deterministic counters/seed terms only, explicit `page`/`pageSize=20`, optional
`sort`/`direction` omitted. Browse pages cycle 1-25; search rotates Group00-Group09 including
mixed-case probes at `page=1&pageSize=20`. Deterministic page-wrap order and the concrete
mixed-case probe spellings are recorded here once selected. Preflight validates the exact
fixture and response expectations before any load.

## Measured baseline and evidence *(pending T007-T009)*

Fixed 50-VU, exactly 120s steady measurement with no ramp in that window; setup, teardown,
preflight, and any warm-up are excluded by the measurement scenario tag. Acceptance uses the
combined measured browse+search population: p95 <=200ms inclusive, custom request-level error
rate <0.01 strict, and nonzero browse and search samples. Full image digest, artifact
bind-mount, env-supplied run ID/UTC/HEAD inputs, and the operator receipt fields are recorded
in T009.

## Bounded negatives *(pending T011)*

An unreachable target and an invalid fixture must each exit nonzero, and empty metrics or a
missing browse/search traffic type must not yield a passing acceptance result.

## Gate reporting *(pending T012)*

Applicable harness gate exits are recorded by their real names: `SKIPPED (scope-empty)` on
empty production-C# mutation scope, `SKIP` for a disabled gate, `Could not run` for a missing
or unreachable runtime, and `FAILED` for a measured budget miss. None of those is reported as
`PASS`. `dotnet build` or unrelated suites are never substituted for load acceptance.

## Excluded

Tracked compose/CI/API/schema/manifest/harness edits; authored migrations or schema
definitions; production dependencies; `Features:LocalPlay`; secrets or credential literals in
source, CLI, history, or logs; `Process.Start`; DEV-334 Playwright or runner reuse;
existing-library deletion; pre-baseline budget tightening; CI enrollment for this suite;
explanatory comments in `movies.js` or `seed.sql`; example-driven k6 dependencies or budgets;
unrelated cleanup.
