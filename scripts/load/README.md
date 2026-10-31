# DEV-427 manual movie-library load baseline

Runbook for the manual `GET /api/movies` load baseline. Manual only: this suite is not
enrolled in CI. Scope is frozen by ticket DEV-427 and `specs/DEV-427/` (`spec.md` FR-001 to
FR-012, `plan.md`, `tasks.md`, `brief.md`).

**Status: Phases 1-4 built (scripts/load skeleton, idempotent seed, workload and preflight,
measured baseline and evidence), and the Phase 5 bounded negatives are exercised and recorded
below.** Sections marked *(pending)* are completed by their named tasks; this file must not
claim a gate result or acceptance outcome before those tasks have produced real receipts.
Seed-twice, preflight, and measured-baseline receipts exist only when an operator has actually
produced them. The measured baseline receipt recorded in this file and the negative receipts
below came from real operator runs; T012 harness gate reporting remains outstanding.

## Scope and files

Tracked delivery is exactly three files:

- `scripts/load/seed.sql` - deterministic idempotent 500-movie fixture
- `scripts/load/movies.js` - k6 workload, preflight/response checks, the tagged 50-VU /
  120s measurement scenario, blocking thresholds, and per-run summary JSON
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
  tag may be shown as a label only, never as the immutable identity. The verified digest used
  for the recorded baseline is
  `grafana/k6@sha256:2dd4756ec6e14e62778916e1eb850108e4b499304c02df56760fa0bdb0fba0b8`
  (`k6 v2.3.0` as a tag label only).
- The host-facing target is compose nginx at `http://localhost:3000` (the `web` service
  publishes `3000:8080`). The script supports a `BASE_URL` override. How the k6 container
  reaches that same nginx entry through a verified Docker host/network mapping is documented
  under "Docker mapping for the k6 container" below; container `localhost` must never be
  assumed to reach the host.
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

## Workload and preflight

`scripts/load/movies.js` drives `GET /api/movies`. Every request sends explicit `page`,
`pageSize=20`, `sort=Title`, and `direction=Ascending`. The endpoint's `MovieQueryValidator`
requires non-null sort and direction, so omitting them returns HTTP 422; the explicit values
follow the Phase 3 scope ruling in `specs/DEV-427/CONCLUSIONS.md`. There are no explanatory
comments in the script; this section is the rationale.

Each VU iteration is one 80/20 cycle: four browse requests, then one title search, with a 1s
sleep after each request. Request and fixture selection use only the deterministic k6 `__VU`
and `__ITER` counters; there is no ambient clock and no `Math.random`. `BASE_URL` defaults to
`http://localhost:3000` and trailing slashes are trimmed.

### Deterministic page-wrap order

For VU `v` (`1..50`), iteration `t` (`0..`), and browse index `i` (`0..3`):

```text
page = (((v - 1) + t * 4 + i) mod 25) + 1
```

So VU 1 starts at pages 1, 2, 3, 4; VU 2 starts at 2, 3, 4, 5; the sequence advances by four
pages per iteration and wraps 25 -> 1. Each VU still cycles pages 1-25 with no random source.

### Deterministic search rotation and probe spellings

For the same VU and iteration, the searched group is
`Group((v - 1 + t) mod 10)` padded to two digits. The mixed-case probe spelling on top of that
group cycles `Group`, `group`, `GROUP`, `gRoUp` by `(group index + t) mod 4`, so the fixture's
case-insensitive title match is exercised without changing expectations. Preflight fixes the
concrete spellings `Group00`, `group01`, `GROUP02`, `gRoUp03`, `Group04`, `group05`, `GROUP06`,
`gRoUp07`, `Group08`, `group09`. Measured traffic rotates the same four spellings
continuously; both spellings are bounded implementation selections under FR-005/FR-006, not
new expectations.

### Preflight and response checks

Preflight runs in the k6 `setup()` stage before any VU traffic, so a wrong fixture or contract
fails the process nonzero instead of producing a measured result. It requests browse pages
1-25 and one search for each of the ten probe spellings above, and for every response checks:

- HTTP 200 and JSON shape with an `items` array and integer `totalCount`.
- Browse: `totalCount=500` and exactly 20 items with unique IDs within that page.
- Search: `totalCount=50` and exactly 20 items whose titles are seeded titles from the
  requested group.
- Every item carries an integer `id` and a title matching `DEV427 GroupNN MovieNNNN`.

No cross-page ordering or cross-page uniqueness is claimed. Measured requests run the same
checks and count at most one failure per request into the custom `request_errors` rate (tagged
`type=browse` or `type=search`); a transport failure, non-200 response, invalid JSON/contract,
or seed mismatch is therefore never counted twice for the same request.

Run a readiness-only pass (setup/preflight; no VU cycle) with `PREFLIGHT_ONLY=1`. Without that
variable the script runs only the tagged fixed 50-VU / 120s measurement scenario; see
"Measured baseline and evidence" below.

## Docker mapping for the k6 container

Container `localhost` is the container itself, never the host or compose nginx. Select and
record one mapping before running:

- **Compose network (selected default).** Attach k6 to the compose project network and target
the nginx service directly at `http://web:8080`, which is the same nginx container the host
reaches through `3000:8080`. Resolve the network from the running stack rather than guessing
the project name:

  ```bash
  docker inspect --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}' "$(docker compose ps -q web)"
  ```

- **Host gateway.** From a container, reach the host-published port through
  `host.docker.internal:3000` (Docker Desktop resolves it; on Linux add
  `--add-host=host.docker.internal:host-gateway`). Use only after confirming it resolves.

Verification is the preflight run itself: a `PREFLIGHT_ONLY=1` run exits 0 only when every
browse and search expectation is met through the selected mapping. A wrong mapping, an
unreachable stack, or a wrong fixture exits nonzero and is recorded as `Could not run`, never
as PASS. Establish the mapping once per environment, then pass it as `BASE_URL`:

```bash
K6_IMAGE="grafana/k6@sha256:2dd4756ec6e14e62778916e1eb850108e4b499304c02df56760fa0bdb0fba0b8"
NETWORK="$(docker inspect --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}' "$(docker compose ps -q web)")"
docker run --rm -i --network "$NETWORK" \
  -e BASE_URL=http://web:8080 \
  -e PREFLIGHT_ONLY=1 \
  "$K6_IMAGE" run - < scripts/load/movies.js
```

The digest above is the immutable identity used for the recorded baseline; any version tag is
a label only. Stdin execution (`run -`) keeps the container free of a copied checkout and
needs no extra helper file. On Git Bash for Windows, prefix `docker run` with
`MSYS_NO_PATHCONV=1` and pass the artifact bind mount as an absolute Windows path (see below),
otherwise the mount source is rewritten and the summary file cannot be written.

## Measured baseline and evidence

A measured run is a single tagged scenario: `executor: constant-vus`, `vus: 50`, `duration:
120s`, `gracefulStop: 0s`, so there is no ramp inside the window and no request is admitted
after it. Setup and preflight run under the `phase=preflight` tag, measured requests under
`phase=measure`; only the measured population feeds acceptance. Acceptance is blocking:

- `measured_duration` p95 `<=200` ms inclusive;
- `request_errors` rate `<0.01` strict over the combined browse+search population;
- `browse_samples` and `search_samples` each `count>0`.

Per-type `browse_*` / `search_*` latency, error, and sample metrics are emitted as
non-blocking diagnostics and do not add budgets. The script fails before measurement if the
required run identity variables are missing, and preflight failures exit nonzero.

### Per-run evidence file

`handleSummary` writes a collision-safe file into the bind-mounted directory:
`$SUMMARY_DIR/$RUN_ID-$RUN_UTC.json`, where `SUMMARY_DIR` defaults to `/artifacts/load` inside
the container and `RUN_UTC` is stripped to filename-safe characters. The JSON contains the
schema version, explicit run id/UTC/git HEAD/k6 image, target, configured VUs and duration,
measured p95 and error rate, browse/search sample counts and p95/error diagnostics, check
counts, each threshold's outcome, and the final `acceptance` value (`PASSED` or `FAILED`). The
file is untracked because `artifacts/` is ignored.

Required environment inputs (all explicit, never read from an ambient clock for workload
selection):

- `RUN_ID` - operator-chosen baseline identifier.
- `RUN_UTC` - operator-supplied UTC timestamp, for example `2026-10-31T03:48:38Z`.
- `GIT_HEAD` - `git rev-parse HEAD` for the revision the script was run from.
- `K6_IMAGE` - the full `grafana/k6@sha256:...` digest used for the run.
- `BASE_URL` - `http://web:8080` on the compose network (or the verified host mapping).

### Measurement command

```bash
K6_IMAGE="grafana/k6@sha256:2dd4756ec6e14e62778916e1eb850108e4b499304c02df56760fa0bdb0fba0b8"
NETWORK="$(docker inspect --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}' "$(docker compose ps -q web)")"
RUN_ID="dev427-baseline-YYYYMMDD"
RUN_UTC="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
GIT_HEAD="$(git rev-parse HEAD)"
mkdir -p artifacts/load
MSYS_NO_PATHCONV=1 docker run --rm -i --network "$NETWORK" \
  -v "$PWD/artifacts/load:/artifacts/load" \
  -e BASE_URL=http://web:8080 \
  -e RUN_ID="$RUN_ID" -e RUN_UTC="$RUN_UTC" \
  -e GIT_HEAD="$GIT_HEAD" -e K6_IMAGE="$K6_IMAGE" \
  "$K6_IMAGE" run - < scripts/load/movies.js
```

On Git Bash for Windows use the absolute Windows form of the mount source, for example
`-v "F:/Dev/LamuFlix.worktrees/DEV-427/artifacts/load:/artifacts/load"`, with
`MSYS_NO_PATHCONV=1`. On Linux, `$PWD` is correct as written. k6 exits `0` when every
threshold passes and `99` when a threshold fails; a budget miss is `FAILED` with the JSON
retained, not a scope change.

### Operator receipt fields

Record, and keep the runtime file untracked:

- both seed receipts: command (redacted), per-run output (`INSERT 0 500` / `INSERT 0 0`),
exit code, and the post-run 500/500/10x50 assertions;
- the measured command (redacted), process exit code, and summary JSON path;
- run id, UTC identity, git HEAD, and full `image@sha256:...` k6 digest;
- environment/hardware notes (Docker host, compose project, network, target);
- measured p95, error rate, browse/search sample counts, and threshold outcomes, including
the first valid measured p95 when the budget misses;
- the temporary `lamuflix_dev427` redirection notice and the restored-settings receipt.

The first valid measured p95 goes to Rigger for YouTrack recording and verified read-back.
Keel and the Builder never write YouTrack.

### Recorded baseline receipt (2026-10-31)

One real operator run on the Finding 4 B disposable database, after two seed runs and a
passing preflight:

```text
database: lamuflix_dev427 (API and SQL target verified; 11 committed migrations applied)
seed run 1: BEGIN / INSERT 0 500 / COMMIT, exit 0 -> 500 total / 500 seed paths / 10 groups x 50
seed run 2: BEGIN / INSERT 0 0 / COMMIT, exit 0 -> 500 total / 500 seed paths / 10 groups x 50
target: http://web:8080 via dev427_default
run: dev427-baseline-20261031 (2026-10-31T03:48:38Z)
git head: 6782ac241f105bf7e5dd74f2ea23c299a29d5760
k6 image: grafana/k6@sha256:2dd4756ec6e14e62778916e1eb850108e4b499304c02df56760fa0bdb0fba0b8
configured: 50 vus / 120s steady
measured: p95 13.178 ms, error rate 0, browse 4800 samples (p95 9.859 ms), search 1200 samples (p95 21.787 ms)
checks: 6000/6000 passed
thresholds: all pass; acceptance: PASSED; process exit 0
summary json: artifacts/load/dev427-baseline-20261031-20261031T034838Z.json (untracked)
restore: docker/api.settings.json restored in place to Database=lamuflix and api restarted healthy
```

This receipt is environment-specific: the p95 value is the baseline for this host, not a
portable guarantee. The Phase 4 scripts were run from the worktree before their commit; the
recorded git HEAD is the base revision of that run.

## Bounded negatives (T011 exercised 2026-10-31)

An unreachable target and an invalid fixture must each exit nonzero, and empty metrics or a
missing browse/search traffic type must not yield a passing acceptance result. Four real
bounded negatives were run after the recorded baseline, with `docker/api.settings.json` already
restored to the operator's `lamuflix` database (0 movies at the time), so the acceptance
fixture was deliberately absent for the invalid-fixture case. No seed row was inserted,
changed, or deleted for these runs and no broken fixture was committed. The full receipt is the
untracked `artifacts/load/dev427-negatives-20261031T040100Z.json`.

k6 exit codes observed: `99` means at least one blocking threshold was crossed; `107` means a
script exception, here the preflight failure thrown from `setup()`. Neither is `0`.

- **Unreachable target, preflight.** `BASE_URL=http://web:9999`, `PREFLIGHT_ONLY=1` on
  `dev427_default`: exit `107`. `setup()` threw `preflight failed: ... http status 0` for every
  browse page and search probe because the port refused connections. Acceptance remains open
  and this path is `Could not run`, never PASS.
- **Unreachable target, measured path.** `BASE_URL=http://web:9999` with `--no-setup --vus 1
  --duration 10s`: exit `99`, measured error rate `1`, `request_errors` threshold crossed,
  `acceptance: FAILED`. Summary JSON:
  `artifacts/load/dev427-negative-unreachable-measured-20261031T040100Z.json`.
- **Invalid fixture.** `BASE_URL=http://web:8080`, `PREFLIGHT_ONLY=1` against the reachable
  stack whose API database held 0 movies: exit `107`. `setup()` threw
  `browse page 1: totalCount 0 (expected 500), items 0 (expected 20)` and the matching search
  failures. A reachable-but-wrong library cannot produce a passing baseline.
- **Empty metrics.** A real measurement scenario with zero admitted VUs
  (`--no-setup --vus 1 --execution-segment 50%:100%`, required run identity variables set):
  exit `99`, `browse samples: 0`, `search samples: 0`, and both `browse_samples` and
  `search_samples` `count>0` thresholds crossed, `acceptance: FAILED`. Summary JSON:
  `artifacts/load/dev427-negative-empty-20261031T035830Z.json`.

Both request types have their own blocking `count>0` threshold, so a run that produces only one
of browse or search traffic fails the same way the empty run does; it cannot be reported as a
passing acceptance result. Syntax checks and synthetic JSON remain excluded as evidence.

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
