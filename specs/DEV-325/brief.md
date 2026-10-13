# DEV-325 — Brief (Phase 2 step 3.1, grill close)

Ticket: DEV-325 — Create docker-compose.yml with health checks, dependency ordering, and sample library
Size: M | ui: (none) | Parent: DEV-287
Worktree: F:\Dev\LamuFlix.worktrees\feature-325-spec | Branch: feature/325-spec
Grill base HEAD: d668cbd01dccba2e4f1eaacca26e5a5452d6cd73; grill rulings committed 33be0ab
Sources: YouTrack DEV-325 via scripts/get-task.ps1; specs/PRODUCT.md; lamuflix-team-charter; notes DEV-325, recon-DEV-325 (sections 0–11); specs/DEV-325/CONCLUSIONS.md (C1–C14); specs/DEV-325/ASSUMPTIONS.md (A1–A3); specs/DEV-21/brief.md and plan.md; task-pipeline.

## Closing bar

- The grill is closed with one `blocked: structural` item: C1 (worker service). The spec PR carries C1 as an owner checkbox; Gate 1 stays closed until the owner ticks (A) drop `worker` or (B) hold for a new worker-host ticket.
- This brief, and Quill's spec/plan/tasks, proceed on the recommended (A) provisionally. If the owner ticks (B), this brief is superseded and DEV-325 is held; no Phase B work starts on (B).
- 13 of 14 grill items are ruled (C2–C14); taste calls are A1–A3 in ASSUMPTIONS.md. No other open question remains.
- This brief freezes scope, grill answers, and plan decisions for Quill. If it is not in this brief, it is not decided; a gap is `needs decision: <question>` to Keel, never a guess by Quill.
- Quill next drafts spec.md, plan.md, and tasks.md in one ask from this brief, recon-DEV-325, and specs/PRODUCT.md (Phase 2 step 3.2). Keel then runs /speckit-analyze read-only against this brief and sends one numbered fix list per round (step 3.3).
- Spec Kit is not started by this brief.

## Round caps

- Grill cap: 12 questions (task-pipeline section 2.2); beyond that, Keel recommendation tagged [assumed] in ASSUMPTIONS.md. Grill closed within cap; the 3 logged assumptions are in ASSUMPTIONS.md, plus the C1 owner checkbox above.
- Review cap: 2 rounds maximum; third-round findings go to user review with the PR.
- Remediation cap: 2 fix commits per round; a third means the finding or the plan was wrong.
- Blocked limit: 3 blocked reports parks the task.

## Frozen scope

In scope (implementation, Phase B, on provisional A):

- `docker-compose.yml` at repository root — new file: services postgres, rabbitmq, api, web (no worker, per C1-A), healthchecks, `service_healthy` dependency ordering, `Features__LocalPlay=false`, sample-library mount, web publish `3000:8080`, api not published (C14).
- `.env.example` at repository root — new file, placeholder values only (C8).
- `docker/sample-library/` — new folder with 3–5 small placeholder files with video extensions, `Title (Year).ext` names, total under 100 KB (C4, A2).
- Api migrate-on-startup wiring for C3: `Database:ApplyMigrationsOnStartup` validated option, default `false`, applying existing EF Core migrations unchanged at Api startup only when `true`; compose sets it `true`. Exact files are named in plan.md and are limited to the Api startup path plus at most one options type; any wider change is `needs decision` to Keel.
- `README.md` — one added section `Run with Docker Compose` (C11, A3). No other README change.

In scope (this Phase A): specs/DEV-325/brief.md (this file), CONCLUSIONS.md, ASSUMPTIONS.md, then Quill-drafted spec.md, plan.md, tasks.md under specs/DEV-325/.

Out of scope (adding any of these is a ticket change unless noted):

- A `worker` compose service, a fake worker running the Api image a second time, or a new worker host project (C1; (B) is the owner's alternative, delivered by its own ticket if chosen).
- `docker-compose.observability.yml` and any OTLP collector service (C10, C12).
- Editing any DEV-21 file: `docker/Dockerfile.api`, `docker/Dockerfile.worker`, `docker/Dockerfile.web`, the DEV-21 nginx conf, root `.dockerignore`; adding HEALTHCHECK to any Dockerfile (C2).
- Editing `web/vite.config.ts`, the nginx conf, or any web source to achieve port 3000 or the proxy (C5).
- Any NuGet/npm dependency change, database schema or migration change (C3 applies existing migrations only), public API route/DTO/status-code change, or contract-chain change.
- Committing a real `.env`, any secret, token, password, or connection string with a real value, or any compose default for a secret (C8, scrub list).
- Setting `Features:LocalPlay` to anything but `false`, or anything enabling `Process.Start` playback.
- Committing real media files for the sample library (C4). If the scanner needs real media content, Keel reports `needs recon: <question>` and stops that line.
- Deleting or rewriting any other tracked file the ticket does not name, except the C3 and C11 forced edits cited there.

Recorded side effect, not in scope: DEV-21 `Dockerfile.worker` publishes the retired `src/LamuFlix.Worker` and will fail DEV-21 SC-001 (C1). The recon fact goes to DEV-21 as a YouTrack comment through Rigger (Patron decides, Rigger records); DEV-325 does not fix DEV-21.

## Grill answers (every ruling, cited)

Decided by ticket text, not re-grilled (CONCLUSIONS.md preamble):

- Services postgres (PostgreSQL 17, `pg_isready`) and rabbitmq (RabbitMQ 4, `rabbitmq-diagnostics -q ping`); api depends on both with `condition: service_healthy`; web depends on api; `Features:LocalPlay=false` in container environment; sample library mount; AC1 `docker compose up -d` starts all services healthy; AC2 web UI at http://localhost:3000 with functioning API proxy. Basis: ticket Scope items 1–2 and acceptance criteria (recon section 1); PRODUCT.md section 3.

Patron rulings (CONCLUSIONS.md):

- C1. Worker: `blocked: structural` — owner checkbox on the spec PR (drop worker (A, recommended) vs hold for a new worker-host ticket (B)). Brief and plan proceed on (A) provisionally; Gate 1 stays closed until ticked. Fake worker rejected.
- C2. Images: api and web use compose `build:` from DEV-21 `docker/Dockerfile.api` / `docker/Dockerfile.web`, context repository root, no registry. Phase B is sequenced after DEV-21 Phase B merges (Dockerfiles must exist on disk). DEV-325 edits no DEV-21 file.
- C3. Schema bootstrap: Api applies existing EF Core migrations at startup only when `Database:ApplyMigrationsOnStartup=true` (validated option, default `false`); compose sets `true`. Forced by AC2/Scope item 2 under charter section 2.3 item 6; not a schema change.
- C4. Sample library: committed placeholders under `docker/sample-library/` (no new top-level folder), mounted `${LIBRARY_PATH:-./docker/sample-library}:/media/library:ro`, `Library__RootPath=/media/library`.
- C5. Port 3000: compose publish `3000:8080` on web only; nginx stays on 8080; `web/vite.config.ts` untouched.
- C6. Proxy proof: `curl -fsS http://localhost:3000/` returns 200 with the SPA index and `curl -fsS http://localhost:3000/api/genres` returns 200 with a JSON array; raw output recorded. The SPA makes no API calls, so the HTTP proof stands in for UI interaction.
- C7. Healthchecks: api probes `GET /health/ready` using only tooling already in the DEV-21 aspnet image (bash `/dev/tcp` probe expecting `200` if no curl/wget; no Dockerfile edit); web uses busybox `wget -q --spider http://127.0.0.1:8080/`; worker none under (A).
- C8. Secrets: committed `.env.example` (placeholders only: `OMDB_API_KEY`, postgres user/password, rabbitmq user/password) plus user-supplied `.env`; compose reads every secret as `${VAR:?message}` with no default. Compose also maps the required non-secret options so ValidateOnStart passes: `RabbitMq__HostName=rabbitmq`, `Enrichment__SweepInterval`, `Enrichment__ClaimLease`, `RabbitMq__RetryDelay` greater than ClaimLease (`Omdb__BaseUrl` comes from appsettings).
- C9. Web `depends_on` api: `condition: service_healthy`.
- C10. `docker-compose.observability.yml`: out (noted, no ticket).
- C11. README: one `Run with Docker Compose` section is in scope (constitution-forced): copy `.env.example` to `.env`, `docker compose up -d`, open localhost:3000, `LIBRARY_PATH` override.
- C12. OTLP: no endpoint env set, no collector; export failures non-fatal. Phase B evidence must show AC1 healthy with no collector; if exporter failures block health, report `needs decision`.
- C13. Image tags: `postgres:17-alpine`, `rabbitmq:4-alpine`, major-pinned, never `latest`.
- C14. Api port: not published to the host; only web 3000 is published, proxy reaches `api:8080` on the compose network.

Assumptions (ASSUMPTIONS.md, taste, [assumed]):

- A1. Image variants `postgres:17-alpine`, `rabbitmq:4-alpine` (no management UI) (= C13).
- A2. Sample placeholders: `Title (Year).ext`, 3–5 files, total under 100 KB (= C4).
- A3. README section title `Run with Docker Compose` (= C11).

## Plan decisions

Approach:

- One root `docker-compose.yml` with four services on provisional (A): postgres, rabbitmq, api, web. Postgres healthcheck `pg_isready`, rabbitmq healthcheck `rabbitmq-diagnostics -q ping` (ticket-fixed commands). Api `depends_on` postgres and rabbitmq `service_healthy`; web `depends_on` api `service_healthy` (C9).
- api and web build from the DEV-21 Dockerfiles with repository-root context (C2). No `image:` registry reference, no Dockerfile edit, no HEALTHCHECK added anywhere by this ticket.
- Healthchecks per C7: api `/health/ready` probe with in-image tooling only, web busybox wget spider. Postgres/rabbitmq keep the ticket commands. AC1 healthy means every service in the file reports healthy.
- Environment: `Features__LocalPlay=false` explicitly on api (ticket Scope item 2; also the code default). All secrets via `${VAR:?message}` from `.env`; `.env.example` carries placeholders only (C8). Non-secret required options mapped per C8 so the Api passes ValidateOnStart.
- `Database__ApplyMigrationsOnStartup=true` is set only in the compose api environment (C3). The code default stays `false`, so the host-run daily-driver path (constitution :506-507) is unchanged.
- Sample library mounted read-only at `/media/library` with `Library__RootPath=/media/library` and the `LIBRARY_PATH` host override (C4). Placeholders are committed dummy files, not real media.
- Web publishes `3000:8080` only; api publishes no host port (C5, C14). No OTLP endpoint is set and no collector service is added (C12).
- README gains exactly one section per C11/A3.

Files touched:

- Spec phase (Phase A): specs/DEV-325/spec.md, specs/DEV-325/plan.md, specs/DEV-325/tasks.md (Quill, from this brief); this brief, CONCLUSIONS.md, and ASSUMPTIONS.md are the Keel/Patron-written artifacts. No ADR (M ticket; ADR is L-only, task-pipeline Phase 2 step 5). No design.md (no ui: tag).
- Build phase (Phase B, for plan/tasks to enumerate, after DEV-21 Phase B merges per C2): root `docker-compose.yml`, root `.env.example`, `docker/sample-library/` placeholder files, `README.md` (one section), and the C3 Api startup/options wiring (exact files named in plan.md, limited as in Frozen scope). Nothing else. Any further file is `needs decision` to Keel before it is added.

Test strategy:

- No existing test references docker-compose (recon section 9). Acceptance is by live compose evidence in Phase B, recorded with raw command output: `docker compose up -d` reaches all-healthy (AC1), then the two C6 curl proofs (SPA index 200, `/api/genres` 200 JSON array) (AC2). Evidence must also show AC1 healthy with no OTLP collector present (C12).
- C3 is evidenced by the api reaching healthy against a fresh postgres volume with `Database:ApplyMigrationsOnStartup=true`, and by the default-`false` path remaining off outside compose (existing test suite plus a targeted test for the new option/startup gate if plan.md adds one; no schema is created or altered beyond applying existing migrations).
- Structural checks in the same evidence pass: no `worker` service (provisional A), no secret default in compose, no committed `.env`, placeholder files only under `docker/sample-library/`, image tags exactly per C13 with no `latest`, api has no host port mapping.
- If placeholder files fail any scanner/media check in Phase B, that line stops with `needs recon: <question>` (C4); real media is never committed as a workaround.
- If the owner ticks C1 (B) instead of (A), all compose acceptance above is void until the worker-host ticket ships and this brief is re-closed.

Gate expectations (Phase B, run by Gauge per task-pipeline Phase 3 step 5):

- DEV-325 Phase B starts only after DEV-21 Phase B merges (C2). The compose build evidence depends on `docker/Dockerfile.api` and `docker/Dockerfile.web` existing on disk.
- This ticket changes production C# only in the C3 startup/options wiring, so the C# gates have real scope on those files and must exit 0; the mutation gate applies to the changed production projects per the pipeline and is reported by its own exit verdict (PASSED / FAILED / NOT APPLICABLE / SKIPPED (scope-empty)), never folded into PASS.
- Gates with no scope in the diff (for example web gates if no web source changes) report exit 2 `SKIPPED (scope-empty)` verbatim: non-blocking, never PASS, never proof the code was checked. Exit 1 or `Could not run` blocks; a property-test exit 2 blocks unless the task note records the pipeline opt-out line.
- Never lower a threshold, never edit a generated file, and never substitute plain `dotnet build` for a gate script. The live compose evidence (AC1/AC2/C6) is this ticket's primary proof and is required in addition to the gate set, not instead of a gate that has real scope.

Task-ordering constraints (for Quill to encode in tasks.md):

1. Phase B does not start until DEV-21 Phase B has merged and `docker/Dockerfile.api` / `docker/Dockerfile.web` exist on disk (C2). The spec PR for DEV-325 carries the C1 owner checkbox and Gate 1 stays closed until it is ticked.
2. The C3 option/startup wiring lands before any compose acceptance run, because the api cannot serve a fresh database without it; its default remains `false` outside compose.
3. `.env.example` is written before the compose file is exercised; acceptance uses a local `.env` copied from it, which is never committed.
4. Sample-library placeholders are committed before the acceptance run; if they fail a scanner check, stop with `needs recon` instead of substituting real media (C4).
5. The README section is written last, describing only the commands and override the frozen compose file actually supports (C11).
6. No task may add a worker service, an observability compose file, a collector, a dependency, a schema change, a Dockerfile edit, or a tracked-file rewrite beyond the frozen set. Such a task is a brief violation for Keel to strike at /speckit-analyze.

## Next owner and action

- Next owner: Quill — Phase 2 step 3.2, /speckit-specify, /speckit-plan, /speckit-tasks in one ask from this brief, recon-DEV-325, and specs/PRODUCT.md, with $env:PYTHONUTF8 set to 1 first.
- Then Keel: /speckit-analyze read-only against this brief (step 3.3), checking in particular the C1 provisional-A wording and owner checkbox, the C3 file limit, the C8 secret handling, and the frozen file set.
- Gate 1, plan challenge, and the spec PR follow the pipeline after a clean analyze; the spec PR carries the C1 owner checkbox and Gate 1 stays closed until the owner ticks it. Nobody merges; the user merges every PR.
