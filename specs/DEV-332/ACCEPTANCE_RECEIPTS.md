# DEV-332 — Real-host acceptance receipts (T016)

Date: 2026-10-30 (local host, UTC timestamps). Status: real-host receipts captured; `T017` recorded as skip-with-reason.

Sanitization: this file records key presence, numeric ports, timestamps, resource states and boolean outcomes only. No secret values, credential operands, private paths, raw connection strings, or dashboard environment views were read into it. Container names and non-secret service identifiers are omitted where they carry no evidential value.

## Environment

| Item | Value | Result |
|---|---|---|
| .NET SDK (global.json `10.0.401`, `latestPatch`) | 10.0.403 resolved | PASS |
| Node (`engines.node >=24 <25`) | v24.19.0 | PASS |
| npm | 9.6.7 | PASS |
| OCI runtime | Docker 29.9.2, daemon reachable | PASS |
| `web/node_modules` | absent at session start; `npm install` then run (337 packages) | PASS |
| Tracked-file side effects of `npm install` | `web/package-lock.json` modified, reverted with `git checkout --`; tree clean | PASS |
| Locally reachable Ollama at `http://localhost:11434` with `all-minilm` | **absent** on this host (see A6/prerequisite note) | NOT PRESENT initially |

## A. Full topology run (`dotnet run --project src/LamuFlix.AppHost`)

Launch used the committed `Properties/launchSettings.json` profile (log line "Usando as configurações de inicialização de src\LamuFlix.AppHost\Properties\launchSettings.json"); dashboard bound to the profile's `applicationUrl` and OTLP port; AppHost version 13.6.4.

Timeline (UTC, single run):

| # | Event | Timestamp | Result |
|---|---|---|---|
| 1 | AppHost start | 2026-10-30T09:09:59Z | PASS |
| 2 | Postgres container started | 09:10:07.004Z | PASS |
| 3 | Postgres "database system is ready to accept connections" | 09:10:08.468Z | PASS |
| 4 | Api first EF query on `__EFMigrationsHistory` (relation absent) | 09:10:16.629Z | PASS |
| 5 | Api first successful HTTP response `/health/ready` = 200 | 09:10:21Z | PASS |
| 6 | Web listening on `[::1]:5173` | 09:10:22Z | PASS |
| 7 | Dashboard login URL reachable (`https://localhost:17000`) | 09:10Z run window | PASS |

### A1 — Disposable pgvector image identity vs compose pin

| Check | Observation | Result |
|---|---|---|
| `WithImage` argument form | `WithImage("pgvector/pgvector", "0.8.7-pg17")` plus separate `WithImageSHA256("ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d")`; no `image@digest` passed as image name | PASS |
| Running container `Config.Image` | `pgvector/pgvector@sha256:ac08538c…c22a75d` (digest reference; tag resolved away) | PASS |
| Running image `RepoDigests` | `["pgvector/pgvector@sha256:ac08538c…c22a75d"]` | PASS |
| Equality with `docker-compose.yml:3` digest | identical `sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d` | PASS |
| Running image ID | `sha256:ac08538c…` | PASS |

### A2 — Cold-start migrations before the Api serves

| Check | Observation | Result |
|---|---|---|
| Fresh database (no pre-existing schema) | new disposable container; `__EFMigrationsHistory` absent on first Api query | PASS |
| Migrations applied by the Api process before any HTTP success | first successful `/health/ready` (row 5) later than migration-history query (row 4); no Api HTTP response succeeded earlier in the poll loop | PASS |
| `__EFMigrationsHistory` rows after startup | 11 | PASS |
| Migration sources in repo (non-designer, non-snapshot) | 11 | PASS |
| Latest applied migration | `20261030001510_AddOmdbDailyUsage` | PASS |
| Tables in public schema | 15 | PASS |
| `Database__ApplyMigrationsOnStartup` honoured only for the AppHost-launched Api | migration ran under AppHost; Api files unchanged (A7) | PASS |
| Postgres host mapping | local-only, dynamically assigned per run (observed different host ports across runs); no fixed host port in `Program.cs` | PASS |

### A3 — Api readiness gate and resource topology

`GET /health/ready` response body (sanitized, full):

```json
{"status":"Healthy","checks":[{"name":"postgres","status":"Healthy","durationMs":1},{"name":"rabbitmq","status":"Healthy","durationMs":0},{"name":"enrichment-consumer","status":"Healthy","durationMs":0},{"name":"features-consumer","status":"Healthy","durationMs":0},{"name":"outbox-relay","status":"Healthy","durationMs":0}]}
```

| Check | Observation | Result |
|---|---|---|
| Api `/health/live` | 200 | PASS |
| Api `/health/ready` (ready-tagged set: postgres, rabbitmq, enrichment-consumer, features-consumer, outbox-relay) | 200 Healthy | PASS |
| Dashboard resource rows | `postgres`, `DefaultConnection` (child database), `rabbitmq`, `api`, `web` — four application resources as ruled | PASS |
| `worker` resource present | none | PASS |
| Ollama resource present in topology | none | PASS |
| All resources Running at steady state | yes (Run 1) | PASS |
| Dashboard reachable with login token | yes | PASS |
| Docker/SDK/dashboard bookkeeping counted as application resources | no | PASS |

### A4 — RabbitMQ mapped AMQP port and injected option keys

| Check | Observation | Result |
|---|---|---|
| Broker image | `docker.io/rabbitmq:4-alpine` | PASS |
| Exposed host mapping | single AMQP mapping `5672/tcp -> 127.0.0.1:60956` in run 1 (a prior run mapped `127.0.0.1:53110`) | PASS |
| Dynamic host port | different mapped port each run | PASS |
| Management UI mapping | none | PASS |
| `RabbitMq__Port` integer-compatibility (`PrimaryEndpoint.Property(EndpointProperty.Port)`) | Api options validated and started; ready-gated `rabbitmq`, `enrichment-consumer`, `features-consumer` all Healthy → broker reached on the injected host/port | PASS |
| Mapped-port equality with the value the Api used | functionally proven by the three Healthy ready checks above (no credential operands read) | PASS |
| Single resource-generated credential set shared with the Api | RabbitMQ log shows AMQP connections authenticated and granted vhost access; values not recorded | PASS |

### A5 — Web on fixed 5173, injected `VITE_API_BASE_URL`, browser path

| Check | Observation | Result |
|---|---|---|
| Resource command line (dashboard) | `node node_modules/vite/bin/vite.js --port 5173 --strictPort`; executable `node`; no `cmd.exe`/shell/`Process.Start`/npm shim | PASS |
| Windows PATH resolution of `node` | resolved and started (no absolute interpreter path in the resource command) | PASS |
| Web working directory | absolute `web` directory derived from `builder.AppHostDirectory`; Vite found `node_modules` and served | PASS |
| Fixed localhost 5173 without Aspire endpoint proxy | web endpoint is the direct `http://localhost:5173`; listener `[::1]:5173` | PASS |
| Vite reported ready | `VITE v8.3.5 ready` | PASS |
| SPA loads at `http://localhost:5173` | page title `LamuFlix`, library shell rendered (0 movies on a fresh database) | PASS |
| Injected API base URL used by the SPA | browser requests went to `http://localhost:51541` (the `api.GetEndpoint("http")` value), not a hardcoded host | PASS |
| Api calls from the browser | `/api/genres`, `/api/collections`, `/api/directors`, `/api/people?role=actor`, `/api/movies?...`, `/api/recommendations/for-you?...` → all 200 | PASS |
| CORS for `http://localhost:5173` | `Access-Control-Allow-Origin: http://localhost:5173`, `Access-Control-Expose-Headers: Location` on both REST and SignalR negotiate responses | PASS |
| SignalR | negotiate 200; console `WebSocket connected to ws://localhost:51541/hubs/enrichment` | PASS |
| Console noise | one benign duplicate-negotiation abort under React StrictMode, immediately followed by a successful connection | PASS (informational) |

Note: the Api process itself listened on a separate loopback port; the browser used the injected endpoint value and reached the Api (all 200s), so the contract chain `api.GetEndpoint("http")` → `VITE_API_BASE_URL` is proven end to end.

### A6 — Local timing inequality and positive sweep

| Check | Source | Result |
|---|---|---|
| `Enrichment__ClaimLease` = `00:00:30` | `Program.cs:31` | PASS |
| `Enrichment__SweepInterval` = `00:00:10` | `Program.cs:32` | PASS |
| `RabbitMq__RetryDelay` = `00:01:00` | `Program.cs:33` | PASS |
| Invariant retry (60s) > claim lease (30s), sweep positive (10s) | 60 > 30, 10 > 0 | PASS |
| Values accepted by the Api options validation and consumer startup | Api started and ready checks Healthy | PASS |

### A7 — Unchanged compose, Api, web and contracts

`git diff --name-only origin/main...HEAD`:

```
Directory.Packages.props
LamuFlix.sln
harness.yml
specs/DEV-332/tasks.md
src/LamuFlix.AppHost/LamuFlix.AppHost.csproj
src/LamuFlix.AppHost/Program.cs
src/LamuFlix.AppHost/Properties/launchSettings.json
src/LamuFlix.AppHost/README.md
```

| Check | Result |
|---|---|
| No `docker-compose*.yml` change | PASS |
| No root `README.md`, `global.json` change | PASS |
| No `src/LamuFlix.Api`, ServiceDefaults, Infrastructure, Core change | PASS |
| No `web/` source change; `web/src/api/{openapi.json,types.ts}` untouched | PASS |
| No `harness.yml` change beyond the AppHost exclusion added in Phase 1 | PASS |
| README documents prerequisites/boundary (Phase 7 artifact present) | PASS (see prerequisite note below) |

## B. Negative proofs

### B1 — Missing private prerequisite fails clearly, no bypass

The Api was launched under an explicit Development environment with an empty user-secrets store (alternate `UserSecretsId` supplied on the command line only; no file, store or secret touched):

| Check | Observation | Result |
|---|---|---|
| Exit | non-zero, process terminated | PASS |
| Message | `Unhandled exception. System.InvalidOperationException: Connection string 'DefaultConnection' not configured. Set 'ConnectionStrings:DefaultConnection' via user secrets or the ConnectionStrings__DefaultConnection environment variable.` | PASS |
| Listener bound before failure | none | PASS |
| Validation/health bypass used | none | PASS |

### B2 — Occupied 5173 fails without fallback

| Check | Observation | Result |
|---|---|---|
| Occupant on `[::1]:5173` (the interface Vite binds on this host) | AppHost run 3: `web` resource Finished/failed while `postgres`, `DefaultConnection`, `rabbitmq`, `api` stayed Running | PASS |
| Fallback to another port | none bound (no 5174+) | PASS |
| Same command line launched directly (`node node_modules/vite/bin/vite.js --port 5173 --strictPort`) | exit 1, `Error: Port 5173 is already in use`, no alternate port bound | PASS |
| Occupant bound only to `127.0.0.1:5173` | Vite still started on `[::1]:5173`; occupancy must be on the interface Vite binds for the strict-port proof to be meaningful | PASS (observed, recorded as nuance) |

### B3 — F5 / launch profile

| Check | Observation | Result |
|---|---|---|
| Committed Development launch profile applied by `dotnet run` | profile settings took effect (dashboard `applicationUrl`, OTLP endpoint, Development environment) | PASS |
| Interactive Visual Studio F5 keypress and `launchBrowser` window | not exercised headlessly | Could not run (no interactive desktop session) |

## C. Prerequisite note surfaced by the proof (needs a ruling)

The first host run failed because the Api's pre-existing startup gate `OllamaEmbeddingStartupValidator` requires a reachable Ollama endpoint for model `all-minilm` at the default `http://localhost:11434`:

| Observation | Value |
|---|---|
| Api exit | `-532462766` (unhandled exception) after the Critical log |
| Critical log | `Embedding startup verification failed at http://localhost:11434 with model all-minilm: category provider_unavailable.` |
| Analysis resource health | `postgres`, `rabbitmq` Running/Healthy before Api startup — ordering itself worked |

To complete the Happy-path receipts, a temporary local Ollama container (`all-minilm`, `127.0.0.1:11434`) was provided outside the AppHost topology, then removed after the run; nothing was added to `Program.cs`, `README.md` or compose. Q1 rules no Ollama resource in the AppHost, so this is a host prerequisite of the AppHost-launched Api — and `src/LamuFlix.AppHost/README.md` currently lists .NET SDK, OCI runtime, Node, `npm install` and the two private Api user secrets but **not** a reachable Ollama endpoint/model. This is reported for a cited Patron ruling (README prerequisite addition) rather than changed silently.

## D. T017 — optional IntegrationTests: skipped with reason

Decision: skip (no new automated host-proof test added).

Reasons recorded per T017:

1. Meaningful coverage already exists at the level that matters: the blocking proof is the real-host receipt above, and every ready-tagged dependency (postgres, rabbitmq, both consumers, outbox relay) is exercised by the existing Api readiness checks and the existing IntegrationTests suite.
2. A host-starting automated test would have to own port 5173 exclusively and run serially, and would contend with any concurrently running developer AppHost; it would also require Docker, Node and a reachable Ollama in the test host.
3. A test that relaunches the AppHost and asserts resource wiring would assert the wiring for its own sake, which T017 and the brief forbid (no mocked Aspire/DB/broker, no wiring-only proof).
4. No new test project/packages are permitted, and `tests/LamuFlix.IntegrationTests/` project or fixture edits are outside this task's additive scope.

Feasibility: feasible in principle on a machine with the prerequisites and exclusive port ownership, not added now. Real-host receipts remain primary and blocking; none of them were skipped.

## E. Classification

| Proof item (T016) | Result |
|---|---|
| Digests / image identity vs compose pin | PASS |
| Cold-start migrations before Api serves | PASS |
| RabbitMQ dynamic mapped port, numeric injection, secret readiness | PASS |
| Running-plus-Healthy dependency ordering, dashboard, no worker | PASS |
| Browser API/CORS/SignalR via injected `VITE_API_BASE_URL` | PASS |
| Windows PATH `node` + Vite, proxy-disabled 5173 | PASS |
| Occupied 5173 fails without fallback | PASS |
| Missing prerequisite fails clearly without bypass | PASS |
| Timing inequality + positive sweep | PASS |
| Compose/Api/frontend/contracts unchanged | PASS |
| Interactive F5 keypress / `launchBrowser` window | Could not run (headless) — not reported as PASS |
| T017 automated host proof | SKIP (reason above) — not reported as PASS |

Prerequisite gap: AppHost README does not list the Ollama endpoint/model requirement (section C) — open for a cited ruling.
