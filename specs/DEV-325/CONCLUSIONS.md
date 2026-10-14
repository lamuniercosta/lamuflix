# DEV-325 — Patron rulings (Phase 2 grill)

Sources: ticket DEV-325 (recon-DEV-325 §1), recon-DEV-325 §0-11, specs/DEV-21 (merged at d668cbd), `.specify/memory/constitution.md`, chain note, team charter §2.3.
Ticket text is decided: postgres 17 + `pg_isready`, rabbitmq 4 + `rabbitmq-diagnostics -q ping`, api/worker `depends_on` both `service_healthy`, web depends on api, `Features:LocalPlay=false`, sample library mount, AC1, AC2.

## C1. Worker service — resolved: (A) no worker
**Resolved (owner):** the owner approved (A): no `worker` service; the enrichment consumer stays in the api (DEV-325:32,36, verified by Keel). The owner's word is final; this closes the C1 owner block only. Gate 1 and the plan freeze remain incomplete. Record-only closure; Keel updates the matching `brief.md` lines. Original ruling kept below for history.

blocked: structural — The ticket names a `worker` service, but no runnable worker host exists. Drop it from DEV-325 (enrichment consumer stays in the api service), or hold DEV-325 until a new worker-host ticket ships?
- Facts: the consumer runs in the Api (recon §5, RabbitMqServiceCollectionExtensions.cs:43-58). `src/LamuFlix.Worker` is retired: it is not in the sln, it uses MySQL/Pomelo, and it references a `LamuFlix.Data` that does not exist, so it cannot build (recon §5, commit 46b1d9b). DEV-21 `docker/Dockerfile.worker` publishes exactly that project (specs/DEV-21/plan.md:83), so its image cannot exist.
- Running the Api image a second time as a fake `worker` would run a second HTTP host and sweeper and pass that off as a worker. Rejected on engineering grounds.
- **Recommended for the owner: (A) drop `worker` from DEV-325's compose.** The api service carries the consumer, and its `/health/ready` healthcheck covers the worker health signal (constitution :255). The alternative, (B) a new modern worker host project, adds to what the ticket delivers and needs its own ticket. Gate 1 stays closed until the owner ticks one on the spec PR. Brief and plan proceed on (A) provisionally.
- Side effect on DEV-21: `Dockerfile.worker` will fail its SC-001 build. The recon fact goes to DEV-21 as a YouTrack comment through Rigger.

## C2. Image source: `build:` from the DEV-21 Dockerfiles
- api and web use `build: { context: ., dockerfile: docker/Dockerfile.api | docker/Dockerfile.web }`. No registry exists, and DEV-21 rules out a registry step (specs/DEV-21/brief.md:101). The Dockerfiles are approved DEV-21 files (brief.md:27-31).
- Sequencing: DEV-325 Phase B starts only after DEV-21 Phase B merges, because AC1 needs the Dockerfiles on disk (recon §2-3). DEV-325 does not edit DEV-21 files (HEALTHCHECK is frozen out of them, DEV-21 brief.md:38).

## C3. Schema bootstrap: opt-in migrate-on-startup in the Api
- AC2 (a functioning API proxy) and Scope item 2 (immediate testing) need a schema. No production migrate path exists (recon §4), so the AC forces this edit (charter §2.3 item 6, forced-edit clause).
- Mechanism: the Api applies EF Core migrations at startup only when `Database:ApplyMigrationsOnStartup=true`. The value is a validated option, default `false`, and compose sets it to `true` only in the compose environment. There is no extra container and no generated SQL file to drift.
- This is not a schema change (charter §2.3 item 3): it applies the existing migrations unchanged.

## C4. Sample library: committed placeholders under `docker/`
- Folder: `docker/sample-library/`, holding small placeholder files with video extensions. This is the ticket-named folder (Scope item 2) under the `docker/` folder that DEV-21 already approved (DEV-21 brief C1), so it adds no new top-level folder.
- Mount: `${LIBRARY_PATH:-./docker/sample-library}:/media/library:ro`, with `Library__RootPath=/media/library`. Config-only root (constitution Principle VII, recon §7). The env override lets the owner point at a real library.
- If the scanner needs real media content instead of placeholders, Keel reports `needs recon` and does not commit real media.

## C5. Port 3000: compose publish `3000:8080` on web
- nginx listens on 8080 (specs/DEV-21/plan.md:87), so the host-to-container mapping is a compose-only change. `web/vite.config.ts` and the nginx conf stay untouched (charter §2.3 item 6 not triggered).

## C6. Proxy proof (AC2)
- `curl -fsS http://localhost:3000/` returns 200 with the SPA index, and `curl -fsS http://localhost:3000/api/genres` returns 200 with a JSON array. Raw output is recorded. The SPA makes no API calls (recon §6), so the HTTP proof stands in for UI interaction.

## C7. Healthchecks: api, web (worker per C1)
- api: probe `GET /health/ready`, which checks Postgres and RabbitMQ (constitution :255, recon §4). Use only tooling already in the DEV-21 image. If `aspnet:10.0` has no curl/wget, a bash `/dev/tcp` probe expecting `200` is acceptable. No Dockerfile edits.
- web: busybox `wget -q --spider http://127.0.0.1:8080/` on nginx alpine.
- worker: none under C1 (A). Under (B), the new host defines its own health signal.

## C8. Secrets: committed `.env.example` plus a user-supplied `.env`
- `.env.example` is new and already whitelisted (.gitignore:7, :494-495). It lists `OMDB_API_KEY`, the postgres user/password, and the rabbitmq user/password with placeholder values only.
- Compose reads every secret with `${VAR:?message}`. No secret gets a default in compose (scrub list). Also map the required non-secret options (`Omdb__BaseUrl` comes from appsettings; `Enrichment__SweepInterval`, `Enrichment__ClaimLease`, and `RabbitMq__RetryDelay` > ClaimLease; `RabbitMq__HostName=rabbitmq`), or the host fails ValidateOnStart (recon §4).

## C9. web `depends_on` api: `condition: service_healthy`
- AC1 requires all services healthy, and the proxy is useless until the api is ready. The ticket gives no condition, so the strictest one is consistent with its text.

## C10. `docker-compose.observability.yml`: out
- The ticket text does not name it, so adding it would extend what DEV-325 delivers. Constitution :336 lists it as a stack element, not as part of this ticket. Noted, no ticket.

## C11. README compose section: in
- Constitution :483 says the README MUST say how to run with compose. Once compose exists the constitution forces this, so the edit is in scope (charter §2.3 item 6, forced-edit clause). Add one short section: copy `.env.example` to `.env`, run `docker compose up -d`, open `localhost:3000`, and override with `LIBRARY_PATH`.

## C12. OTLP in containers: no endpoint, no collector
- C10 rules out a collector. Leave `OTEL_EXPORTER_OTLP_ENDPOINT` unset; export failures are non-fatal. Phase B evidence must show AC1 healthy with no collector present. If exporter failures block health, Keel reports `needs decision`.

## C13. Image tags: `postgres:17-alpine`, `rabbitmq:4-alpine`
- Major-pinned per ticket text, never `latest` (specs/DEV-21/plan.md:95). Postgres matches the test fixture (PostgresFixture.cs:34). The variant is a taste call; see ASSUMPTIONS A1.

## C14. api port: not published to the host
- AC2 needs only web on 3000. The proxy reaches `api:8080` on the compose network. CORS allows only 5173 (Program.cs:19), and publishing 8080 would collide with the host-run Api daily-driver setup (constitution :528-529).

## C15. Postgres database name: fixed `lamuflix`
- postgres service sets `POSTGRES_DB=lamuflix`; the api `ConnectionStrings__DefaultConnection` uses `Database=lamuflix` with host `postgres`. User and password interpolation stays exactly as C8 (`${VAR:?message}`, no default).
- The database name is non-secret configuration, so it is a literal, not a `.env` variable (C8, recon-DEV-325:43). Resolves the T009 / plan.md:78 `needs decision`.
- Not a schema change (charter §2.3 item 3: C3 applies existing migrations unchanged into this database) and not API shape (item 4).