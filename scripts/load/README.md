# DEV-427 manual movie-library load baseline

Runbook for the manual `GET /api/movies` load baseline. Manual only: this suite is not
enrolled in CI. Scope is frozen by ticket DEV-427 and `specs/DEV-427/` (`spec.md` FR-001 to
FR-012, `plan.md`, `tasks.md`, `brief.md`).

**Status: Phases 1-2 built (scripts/load skeleton and idempotent seed).** Sections marked
*(pending)* are completed by their named tasks; this file must not claim a baseline, gate
result, or acceptance outcome before those tasks have produced real receipts. Seed-twice
receipts for Phase 2 exist only when an operator has actually produced them.

## Scope and files

Tracked delivery is exactly three files:

- `scripts/load/seed.sql` - deterministic idempotent 500-movie fixture
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
   where practical. The exact commands and guards are in the seed sequence below.
5. Announce the temporary redirection: while `docker/api.settings.json` is retargeted, the
   compose UI and API serve the disposable fixture, not the operator's real library.
6. Preserve the operator's original settings privately first, and restore them plus restart
   `api` on success and on failure.
7. Do not reuse the DEV-334 Playwright runner or its helpers, and do not add helper scripts
   beyond the three authorized files.

## Seed and repeat-seed assertions

`scripts/load/seed.sql` inserts `i=1..500` with `library_path=/dev427/movie-NNNN.mp4`,
`title=DEV427 GroupGG MovieNNNN`, `format=mp4`, `is_in_watchlist=false`, `status=1`,
`enrichment_attempts=0`, supplied explicitly because those columns are NOT NULL without a
DB default. `is_watched` and `is_not_interested` are NOT NULL with a DB default and are left
to that default. The insert uses `ON CONFLICT (library_path) DO NOTHING` against the existing
unique index, so a second run inserts nothing and deletes nothing. No OMDb identifier is
written and no OMDb call is made. `movies.js` and `seed.sql` carry no explanatory comments;
this README is the rationale.

The file is wrapped in `BEGIN;`/`COMMIT;` and sets `ON_ERROR_STOP`, so a failed run rolls back
instead of leaving a partial fixture. Run it twice without deleting rows. After each run the
assertions are: exactly 500 total movies, exactly 500 distinct `/dev427/movie-%.mp4` seed
paths, and exactly 50 movies in each of Group00 through Group09.

### Full Finding 4 B sequence

Run every step from the repository root. Substitute `lamuflix_dev427` only if you deliberately
choose another disposable database name, and use the same name in the settings file.

1. Preserve the original operator settings privately before editing anything, for example by
   copying `docker/api.settings.json` to a private location outside the tracked tree and
   keeping that copy out of receipts. The file is ignored and must never be committed or
   printed in full.
2. Prove identity and create the separately named disposable database in the existing compose
   postgres container. Credentials come from the existing compose environment (`POSTGRES_USER`)
   and the container local socket; never type a password into a command, history, or log:

   ```bash
   docker compose ps postgres
   docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 -c "SELECT current_database(), current_user;"'
   docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE lamuflix_dev427"'
   ```

   The second command must report the compose project's expected `postgres` target; the last
   command fails harmlessly if the database already exists. `POSTGRES_DB` stays `lamuflix`, so
   the disposable database is a second database in the same container. The username is read
   from the container's own compose environment (`$POSTGRES_USER`) inside a single-quoted
   `sh -c` string, so it is not expanded, echoed, or stored on the host command line.
3. Retarget the API only through the ignored operator-local `docker/api.settings.json`. Edit
   the existing file **in place** (do not replace it; the compose single-file bind mount can
   keep serving a stale inode) and change only `Database=` on
   `ConnectionStrings:DefaultConnection` to `lamuflix_dev427`. Keep
   `Database.ApplyMigrationsOnStartup=true` and keep every other value as it was.
4. Restart `api` and verify the already-committed migrations actually ran; do not trust the
   restart:

   ```bash
   docker compose restart api
   docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d lamuflix_dev427 -v ON_ERROR_STOP=1 -c "SELECT count(*) AS migration_rows FROM \"__EFMigrationsHistory\";"'
   ```

   The row count must match the committed migration count in
   `src/LamuFlix.Infrastructure/Persistence/Migrations/` (11 at the time of writing); `0` or a
   failed query means the settings retarget did not take effect. This applies existing
   committed migrations at runtime only; no migration or schema definition is authored here.
5. Prove the API and the SQL session target the same database, and apply the
   empty-or-exact-seed-only guard before the first insert:

   ```bash
   docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d lamuflix_dev427 -v ON_ERROR_STOP=1' <<'SQL'
   SELECT current_database() AS sql_target;
   SELECT datname, count(*) AS connections FROM pg_stat_activity GROUP BY datname ORDER BY datname;
   SELECT count(*) AS total, count(*) FILTER (WHERE library_path LIKE '/dev427/movie-%.mp4') AS seed_rows FROM movies;
   SQL
   ```

   `sql_target` must be the disposable database, and the API's pooled connections must appear
   under that same `datname` (none under the operator's real database). The last query must
   show either `total = 0` (empty database) or `total = 500` with `seed_rows = 500` (a previous
   exact seed run). Any other combination means a contaminated or mixed library: stop, restore,
   and do not continue.
6. Announce the temporary redirection: while `docker/api.settings.json` is retargeted, the
   compose UI and API serve the disposable fixture, not the operator's real library.
7. Seed transactionally twice and retain both redacted receipts (see below).
8. Restore the original `docker/api.settings.json` content **in place** and restart `api`.
   Do this on success and on failure, including after any guard or seed error above. Confirm
   the API is back on the original database before ending the session.

### Credential-safe seed command

Pipe the tracked seed file over stdin so nothing is copied into the container and no password
appears anywhere:

```bash
docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d lamuflix_dev427 -v ON_ERROR_STOP=1 -f -' < scripts/load/seed.sql
```

A clean run prints only `BEGIN`, a row count for the insert statement, and `COMMIT`. The
`ON CONFLICT (library_path) DO NOTHING` insert reports `INSERT 0 500` on the first run and
`INSERT 0 0` on the second. Redact `$POSTGRES_USER`, connection strings, and any password
before quoting a receipt; an acceptable redacted receipt looks like:

```text
run: 1
command: docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d lamuflix_dev427 -v ON_ERROR_STOP=1 -f -' < scripts/load/seed.sql
output: BEGIN / INSERT 0 500 / COMMIT
exit: 0
```

```text
run: 2
command: docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d lamuflix_dev427 -v ON_ERROR_STOP=1 -f -' < scripts/load/seed.sql
output: BEGIN / INSERT 0 0 / COMMIT
exit: 0
```

### Verification queries after each run

Run these on the disposable database, for example through a quoted heredoc so no quoting level
changes the SQL, and record the outputs in both receipts:

```bash
docker compose exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d lamuflix_dev427 -v ON_ERROR_STOP=1' <<'SQL'
-- 1. exactly 500 total movies
SELECT count(*) AS total FROM movies;

-- 2. exactly 500 distinct deterministic seed paths
SELECT count(DISTINCT library_path) AS seed_paths
FROM movies
WHERE library_path LIKE '/dev427/movie-%.mp4';

-- 3. exactly ten groups, each holding exactly 50 movies
SELECT count(*) AS groups, bool_and(group_count = 50) AS all_groups_50
FROM (
    SELECT substring(title FROM '^DEV427 (Group[0-9]{2}) ') AS group_label, count(*) AS group_count
    FROM movies
    WHERE library_path LIKE '/dev427/movie-%.mp4'
    GROUP BY group_label
) grouped;
SQL
```

Expected after each of the two runs: `total = 500`, `seed_paths = 500`, `groups = 10`,
`all_groups_50 = t`. A library that is not exactly the 500 deterministic seed movies is a
contaminated/mixed library and is not an acceptance baseline, even if it contains those 500
rows among others. The operator's real `lamuflix` library is never queried for deletion or
modified, and the DEV-334 Playwright runner is never reused.

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
