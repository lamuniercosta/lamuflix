# Tasks: Optimized multi-stage Dockerfiles (DEV-21)

**Input**: `specs/DEV-21/spec.md`, `specs/DEV-21/plan.md`, approved brief `specs/DEV-21/brief.md` (C1–C10, A1–A2).

**Scope guard**: Only these 5 new files may be created: `docker/Dockerfile.api`, `docker/Dockerfile.worker`, `docker/Dockerfile.web`, one Nginx config under `docker/`, root `.dockerignore`. No task may add a dependency, compose file, `HEALTHCHECK`, CI change, registry step, tracked-file rewrite, secret, or LocalPlay change. Such a task is a brief violation.

## Phase 1: Setup (shared)

**Purpose**: Common build-context foundation every story needs.

- [X] T001 Write root `.dockerignore` per plan DD-5 (exclude literal patterns `.git/`, `.agents/`, `.claude/`, `.codex/`, `.cursor/`, `.gemini/`, `.junie/`, `.specify/`, `.github/`, `docs/`, `specs/`, `scripts/`, `tests/`, `**/bin/`, `**/obj/`, `web/node_modules/`, `web/dist/`, `.env`, `.env.*`, `**/*.user`, `**/*.pfx`, `**/secrets.json`, `**/appsettings.Development.json`, plus editor/OS artifacts; keep `docker/`, `src/`, `web/`, `Directory.Build.props`, `Directory.Packages.props`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `global.json`, `web/package.json`, `web/package-lock.json` visible).

**Checkpoint**: `.dockerignore` exists — no `docker build` evidence is taken before this task (ordering constraint 1).

---

## Phase 2: Foundational — Worker publish C3 check (blocking)

**Purpose**: C3 evidence that MUST precede `Dockerfile.worker` acceptance.

- [ ] T002 [US2] Publish `src/LamuFlix.Worker/LamuFlix.Worker.csproj` (`dotnet publish -c Release`) and inspect the publish-output runtimeconfig for `Microsoft.AspNetCore.App`. If present: STOP the Worker line, do not edit any runtime image, and report to Patron for a `blocked: structural` ruling. If absent: record the raw evidence in the task note and close C3. (Ordering constraint 2.)

**Checkpoint**: C3 closed by evidence, or the Worker line parked on Patron escalation — `Dockerfile.worker` is not marked done until then.

---

## Phase 3: User Story 1 — Api image (Priority: P1) 🎯 MVP

**Goal**: `docker/Dockerfile.api` builds from the repo root; final image runs as `USER app`.

**Independent Test**: `docker build -f docker/Dockerfile.api .` exits 0; `docker inspect` / `docker run … id` shows non-root. Independence is file/runtime-level; ordering constraint 4 governs evidence order.

- [ ] T003 [US1] Write `docker/Dockerfile.api` per plan DD-2 (copy `global.json`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `Directory.Packages.props`, `Directory.Build.props`, Api plus referenced `.csproj` files first, `dotnet restore`, then copy `src/` and `dotnet publish -c Release`; `aspnet:10.0` runtime with publish output only, `USER app`, `EXPOSE 8080`, entrypoint `dotnet LamuFlix.Api.dll`; image-default assumption: the `aspnet:10.0` image sets `ASPNETCORE_HTTP_PORTS=8080`, no such value set in the Dockerfile).
- [ ] T004 [US1] Acceptance evidence for the Api image: `docker build -f docker/Dockerfile.api .` from the repo root exits 0 (record raw output); non-root user via `docker inspect` / `docker run … id`; structural check (no SDK in runtime, restore precedes source copy, tags pinned, no `HEALTHCHECK` instruction, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement). Depends on T003, T005, and T008 — no build evidence before all three Dockerfiles are written (ordering constraint 4).

**Checkpoint**: US1 independently functional and evidenced.

---

## Phase 4: User Story 2 — Worker image (Priority: P2)

**Goal**: `docker/Dockerfile.worker` builds from the repo root; final image runs non-root on `dotnet/runtime:10.0`.

**Independent Test**: C3 check (T002) closed; `docker build -f docker/Dockerfile.worker .` exits 0; non-root user evidenced. Independence is file/runtime-level; ordering constraint 4 governs evidence order.

- [ ] T005 [US2] Write `docker/Dockerfile.worker` per plan DD-3 (copy `global.json`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `Directory.Packages.props`, `Directory.Build.props`, and the Worker `.csproj` first, `dotnet restore`, then copy source and `dotnet publish -c Release`; `dotnet/runtime:10.0` runtime with publish output only, non-root via the image built-in `USER app` with no pre-authorised created-user fallback, entrypoint `dotnet LamuFlix.Worker.dll`). Depends on T002.
- [ ] T006 [US2] Acceptance evidence for the Worker image: `docker build -f docker/Dockerfile.worker .` from the repo root exits 0 (record raw output); non-root user via `docker inspect` / `docker run … id` (fail-safe for the DD-3 built-in-user statement); structural check (no SDK in runtime, restore precedes source copy, tags pinned, no `HEALTHCHECK` instruction, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement); inspect the built-image runtimeconfig for `Microsoft.AspNetCore.App` — if present, STOP the Worker line, do not edit any runtime image, and report to Patron for a `blocked: structural` ruling. Depends on T002, T003, T005, and T008 — no build evidence before all three Dockerfiles are written (ordering constraint 4).

**Checkpoint**: US1 and US2 both independently evidenced.

---

## Phase 5: User Story 3 — Web SPA image (Priority: P3)

**Goal**: `docker/Dockerfile.web` plus its Nginx config build from the repo root; image serves the SPA and proxies `/api` to `api:8080` as unprivileged Nginx.

**Independent Test**: `docker build -f docker/Dockerfile.web .` exits 0; non-root user evidenced; Nginx config inspection shows SPA fallback plus `/api/...` proxy. Independence is file/runtime-level; ordering constraint 4 governs evidence order.

- [ ] T007 [P] [US3] Write the Nginx config under `docker/` per plan DD-4 (`listen 8080`; serve `web/dist` assets, client-side-route fallback to the SPA entry, `location /api/` prefix-preserving `proxy_pass` with no URI part to `http://api:8080` for `/api/...` requests — a bare `/api` request without a trailing slash is not an acceptance requirement — plus `proxy_set_header` for `Host`, `X-Real-IP`, `X-Forwarded-For`, and `X-Forwarded-Proto`; the Web image requires an `api` network alias at runtime).
- [ ] T008 [US3] Write `docker/Dockerfile.web` per plan DD-4 (Node `24.12-alpine` build: copy `web/package.json` plus `web/package-lock.json` first, `npm ci`, then copy `web/` source and `npm run build` as-is; `nginxinc/nginx-unprivileged:1.29-alpine` runtime with `EXPOSE 8080`, `web/dist` plus the T007 config).
- [ ] T009 [US3] Acceptance evidence for the Web image: `docker build -f docker/Dockerfile.web .` from the repo root exits 0 (record raw output); non-root user via `docker inspect` / `docker run … id`; Nginx-config inspection (SPA fallback + `location /api/` prefix-preserving proxy to `api:8080` for `/api/...` requests + `listen 8080`); structural check (no Node in runtime, tags pinned, no `HEALTHCHECK` instruction, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement). Depends on T003, T005, T007, and T008 — neither T007 nor T008 is accepted without the other (ordering constraint 3), and no build evidence before all three Dockerfiles are written (ordering constraint 4).

**Checkpoint**: All three user stories independently functional and evidenced.

---

## Phase 6: Polish & gates

**Purpose**: Ticket proof assembly; no new scope.

- [ ] T010 Collect the C8 evidence pack: the three `docker build` raw outputs, per-image non-root evidence, structural-check results (including the T004/T006/T009 negative checks: no `HEALTHCHECK`, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement), the T002 C3 record, the T006 built-image runtimeconfig record, the Nginx-config inspection note, explicit `.dockerignore` coverage evidence for SC-003 (the T001 literal patterns exclude nested `**/bin/` and `**/obj/` plus `bin/`, `obj/`, tests, and `node_modules` while keeping `docker/`, `src/`, `web/`, `Directory.Build.props`, and `Directory.Packages.props` visible), and confirmation that no development appsettings file lands in a runtime image.
- [ ] T011 (Owner: Gauge) Run the Phase B gate set per the brief (task-pipeline Phase 3 step 5; the implementer does not run a duplicate gate pass): gates with empty scope (no `.cs`, no `web/` source changed) report exit 2 SKIPPED (scope-empty) verbatim — non-blocking, never PASS. Gates with real scope must exit 0. Exit 1 and Could not run block; a property-test exit 2 blocks unless the task note records the opt-out line required by the pipeline. Never lower a threshold or substitute plain `dotnet build`.

---

## Dependencies & Execution Order

- T001 first: no `docker build` evidence before `.dockerignore` exists.
- T002 before T005/T006: Worker publish/runtimeconfig C3 check precedes `Dockerfile.worker` acceptance; failure branch stops the Worker line and escalates (never swaps the image).
- T007 + T008 together: Nginx config and `Dockerfile.web` land together; T009 depends on both.
- T003/T005/T007-T008 (writes) precede T004/T006/T009 (build evidence): all Dockerfiles are written before the acceptance runs (ordering constraint 4).
- T010–T011 last: evidence pack and gates after all stories.
- Api and Worker lines are otherwise independent of each other and of Web once T001 exists.
- Parallel opportunities: T003, T005 (after T002), T007, T008 touch different files and can proceed in parallel; T004, T006, T009 are sequential per image but parallel across images once writes land.

## Parallel Example

```text
T003 "Write docker/Dockerfile.api" (docker/Dockerfile.api)
T005 "Write docker/Dockerfile.worker" after T002 (docker/Dockerfile.worker)
T007 "Write docker Nginx config" (docker/nginx.conf)
T008 "Write docker/Dockerfile.web" (docker/Dockerfile.web)
```
