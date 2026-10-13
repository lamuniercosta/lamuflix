# Tasks: Optimized multi-stage Dockerfiles (DEV-21)

**Input**: `specs/DEV-21/spec.md`, `specs/DEV-21/plan.md`, approved brief `specs/DEV-21/brief.md` (C1–C10, A1–A2, Owner answer amendment for PR #136).

**Scope guard**: Only these 4 new files may be created: `docker/Dockerfile.api`, `docker/Dockerfile.web`, one Nginx config under `docker/`, root `.dockerignore`. `docker/Dockerfile.worker` is dropped permanently by the owner answer on PR #136 (option c); it is not deferred, no DEV-21 follow-up creates it, and no Worker publish, C3 runtimeconfig check, or Worker acceptance task remains. No task may add a dependency, compose file, `HEALTHCHECK`, CI change, registry step, tracked-file rewrite, secret, or LocalPlay change. Such a task is a brief violation.

**Resume status (2026-10-13)**: T001-T007 remain completed historical work. The C8 pack was verified at da3866cd (recon-DEV-21 section 14); four delivered file blobs remain unchanged at 764d2eac, but main changed Api sources and the generated Web contract (recon-DEV-21-3:46-73). Current-context acceptance is therefore pending in T009-T011. C13 is already reflected in this task list; no Worker obligation is restored. T008 remains Gauge-owned and is not a task for Anvil.

**Next Anvil phase**: **Phase 5: Owner-answer resume - current-context acceptance**. Execute T009, T010, T011 in that order; no completed write task is restarted. On failure, report the command, exit and failure evidence to Conductor before any implementation or scope change.

## Phase 1: Setup (shared)

**Purpose**: Common build-context foundation every story needs.

- [X] T001 Write root `.dockerignore` per plan DD-5 (exclude literal patterns `.git/`, `.agents/`, `.claude/`, `.codex/`, `.cursor/`, `.gemini/`, `.junie/`, `.specify/`, `.github/`, `docs/`, `specs/`, `scripts/`, `tests/`, `**/bin/`, `**/obj/`, `web/node_modules/`, `web/dist/`, `.env`, `.env.*`, `**/*.user`, `**/*.pfx`, `**/secrets.json`, `**/appsettings.Development.json`, plus editor/OS artifacts; keep `docker/`, `src/`, `web/`, `Directory.Build.props`, `Directory.Packages.props`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `global.json`, `web/package.json`, `web/package-lock.json` visible).

**Checkpoint**: `.dockerignore` exists — no `docker build` evidence is taken before this task (ordering constraint 1).

---

## Phase 2: User Story 1 — Api image (Priority: P1) 🎯 MVP

**Goal**: `docker/Dockerfile.api` builds from the repo root; final image runs as `USER app`.

**Independent Test**: `docker build -f docker/Dockerfile.api .` exits 0; `docker inspect` / `docker run … id` shows non-root. Independence is file/runtime-level; ordering constraint 4 governs evidence order.

- [X] T002 [US1] Write `docker/Dockerfile.api` per plan DD-2 (copy `global.json`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `Directory.Packages.props`, `Directory.Build.props`, Api plus referenced `.csproj` files first, `dotnet restore`, then copy `src/` and `dotnet publish -c Release`; `aspnet:10.0` runtime with publish output only, `USER app`, `EXPOSE 8080`, entrypoint `dotnet LamuFlix.Api.dll`; image-default assumption: the `aspnet:10.0` image sets `ASPNETCORE_HTTP_PORTS=8080`, no such value set in the Dockerfile).
- [X] T003 [US1] Acceptance evidence for the Api image: `docker build -f docker/Dockerfile.api .` from the repo root exits 0 (record raw output); non-root user via `docker inspect` / `docker run … id`; structural check (no SDK in runtime, restore precedes source copy, tags pinned, no `HEALTHCHECK` instruction, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement). Depends on T002 and T005 — no build evidence before both Dockerfiles are written (ordering constraint 4).

**Checkpoint**: US1 independently functional and evidenced.

---

## Phase 3: User Story 3 — Web SPA image (Priority: P3)

**Goal**: `docker/Dockerfile.web` plus its Nginx config build from the repo root; image serves the SPA and proxies `/api` to `api:8080` as unprivileged Nginx.

**Independent Test**: `docker build -f docker/Dockerfile.web .` exits 0; non-root user evidenced; Nginx config inspection shows SPA fallback plus `/api/...` proxy. Independence is file/runtime-level; ordering constraint 4 governs evidence order.

- [X] T004 [P] [US3] Write the Nginx config under `docker/` per plan DD-4 (`listen 8080`; serve `web/dist` assets, client-side-route fallback to the SPA entry, `location /api/` prefix-preserving `proxy_pass` with no URI part to `http://api:8080` for `/api/...` requests — a bare `/api` request without a trailing slash is not an acceptance requirement — plus `proxy_set_header` for `Host`, `X-Real-IP`, `X-Forwarded-For`, and `X-Forwarded-Proto`; the Web image requires an `api` network alias at runtime).
- [X] T005 [US3] Write `docker/Dockerfile.web` per plan DD-4 (Node `24.12-alpine` build: copy `web/package.json` plus `web/package-lock.json` first, `npm ci`, then copy `web/` source and `npm run build` as-is; `nginxinc/nginx-unprivileged:1.29-alpine` runtime with `EXPOSE 8080`, `web/dist` plus the T004 config).
- [X] T006 [US3] Acceptance evidence for the Web image: `docker build -f docker/Dockerfile.web .` from the repo root exits 0 (record raw output); non-root user via `docker inspect` / `docker run … id`; Nginx-config inspection (SPA fallback + `location /api/` prefix-preserving proxy to `api:8080` for `/api/...` requests + `listen 8080`); structural check (no Node in runtime, tags pinned, no `HEALTHCHECK` instruction, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement). Depends on T002, T004, and T005 — neither T004 nor T005 is accepted without the other (ordering constraint 3), and no build evidence before both Dockerfiles are written (ordering constraint 4).

**Checkpoint**: Both user stories independently functional and evidenced.

---

## Phase 4: Polish & gates

**Purpose**: Ticket proof assembly; no new scope.

- [X] T007 Collect the C8 evidence pack as amended: the two `docker build` raw outputs, per-image non-root evidence, structural-check results (including the T003/T006 negative checks: no `HEALTHCHECK`, no secret-bearing `ENV`/`COPY`, no `Features:LocalPlay` or `Process.Start` enablement), the Nginx-config inspection note, explicit `.dockerignore` coverage evidence for SC-003 as amended (the T001 literal patterns exclude nested `**/bin/` and `**/obj/` plus `bin/`, `obj/`, tests, and `node_modules` while keeping `docker/`, `src/`, `web/`, `Directory.Build.props`, and `Directory.Packages.props` visible), and confirmation that no development appsettings file lands in a runtime image. No Worker build, C3 record, or built-image Worker runtimeconfig record is collected.
- [ ] T008 (Owner: Gauge) Run the Phase B gate set per the brief (task-pipeline Phase 3 step 5; the implementer does not run a duplicate gate pass): gates with empty scope (no `.cs`, no `web/` source changed) report exit 2 SKIPPED (scope-empty) verbatim — non-blocking, never PASS. Gates with real scope must exit 0. Exit 1 and Could not run block; a property-test exit 2 blocks unless the task note records the opt-out line required by the pipeline. Never lower a threshold or substitute plain `dotnet build`.

---

## Phase 5: Owner-answer resume - current-context acceptance

**Owner**: Anvil. **Purpose**: Complete the owner-approved Api/Web test plan against the rebased build context; preserve historical receipts and the frozen four-file scope (brief Owner-answer resume decision).

- [X] T009 [US1] Repeat all T003 Api acceptance checks from `F:/Dev/LamuFlix.worktrees/DEV-21`: `docker build -f docker/Dockerfile.api .` must exit 0, with raw output and the actual exit recorded. Record branch, source HEAD and documentation working-tree delta; obtain the new image ID and use that ID for `docker inspect` and `docker run ... id` (uid must not be 0). Record runtime SDK absence, pinned tags, restore-before-source, and every T003 negative check. Depends on completed T001, T002, T004 and T005. Historical image IDs/tags and unchanged Dockerfile bytes alone are not current-context build evidence.
- [X] T010 [US3] Repeat all T006 Web acceptance checks from the same context after T009: `docker build -f docker/Dockerfile.web .` must exit 0, with raw output and the actual exit recorded. Record the new image ID; inspect/probe that ID for non-root uid, runtime Node absence and development-appsettings absence. Record every T006 structural/negative check and the Nginx-config inspection (`listen 8080`, SPA fallback, `location /api/` preserving the prefix to `api:8080`, required proxy headers). Use the existing configuration; the runtime `api` alias caveat remains, and no live compose-stack run is added. Depends on completed T001/T002/T004/T005 and T009 for resume execution order.
- [X] T011 Collect a separate current-context C8 evidence pack under the existing `artifacts/DEV-21/` location, preserving the historical pack/transcripts. Include all T007 collection items for the T009/T010 images: raw build outputs and command exits, new image IDs, inspect/run transcripts and non-root checks, runtime tooling checks, structural/negative results, Nginx-config note, explicit `.dockerignore` excluded/kept-visible coverage and no development appsettings in either runtime image. State branch, source HEAD, documentation working-tree delta and exact raw-output paths; report the readback in DEV-21. No Worker artifact, publish or runtimeconfig check. Depends on T009 and T010. This acceptance pack does not assert current review coverage or a merge bar; T008 gate completion remains with Gauge.

---

## Dependencies & Execution Order

- T001 first: no `docker build` evidence before `.dockerignore` exists.
- No Worker phase: the former Worker publish C3 check, Dockerfile.worker write, and Worker acceptance tasks are deleted permanently, not parked or deferred.
- T004 + T005 together: Nginx config and `Dockerfile.web` land together; T006 depends on both.
- T002/T004-T005 (writes) precede T003/T006 (build evidence): both Dockerfiles are written before the acceptance runs (ordering constraint 4).
- T007-T008 describe the original evidence/gate sequence. T007 completion is historical; T008 stays with Gauge.
- Resume sequence: T009 then T010 then T011; both existing Dockerfiles and Nginx config are prerequisites already completed. Conductor routes any resulting gate refresh to Gauge, separately from Anvil acceptance.
- Api and Web lines are otherwise independent of each other once T001 exists.
- Parallel opportunities: T002, T004, T005 touch different files and can proceed in parallel; T003, T006 are sequential per image but parallel across images once writes land.

## Parallel Example

```text
T002 "Write docker/Dockerfile.api" (docker/Dockerfile.api)
T004 "Write docker Nginx config" (docker/nginx.conf)
T005 "Write docker/Dockerfile.web" (docker/Dockerfile.web)
```
