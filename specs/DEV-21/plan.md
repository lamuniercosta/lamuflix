# Implementation Plan: Optimized multi-stage Dockerfiles (DEV-21)

**Branch**: `feature/021-spec` | **Date**: 2026-10-12 | **Spec**: `specs/DEV-21/spec.md`

**Input**: `specs/DEV-21/spec.md`, approved brief `specs/DEV-21/brief.md` (grill close, HEAD 56afcf8fdc901a18b4235b7cc894e01d40db7150), `specs/DEV-21/CONCLUSIONS.md` (C1–C10), `specs/DEV-21/ASSUMPTIONS.md` (A1–A2), notes DEV-21 and recon-DEV-21 §§1–9.7, `specs/PRODUCT.md`.

## Summary

Create three optimized multi-stage Dockerfiles — `docker/Dockerfile.api`, `docker/Dockerfile.worker`, `docker/Dockerfile.web` — plus one Nginx config under `docker/` and a root `.dockerignore`, all built from the repository root. Api: `sdk:10.0` → `aspnet:10.0` (`USER app`). Worker: `sdk:10.0` → `dotnet/runtime:10.0`, non-root, gated by the C3 runtimeconfig check. Web: Node build of the existing SPA as-is → pinned unprivileged Nginx alpine serving the SPA with `/api` → `api:8080` proxy. No dependencies, no compose, no HEALTHCHECK, no CI/registry, no secrets, no LocalPlay.

## Technical Context

**Language/Version**: .NET 10 (`net10.0`; Api `Microsoft.NET.Sdk.Web`, Worker `Microsoft.NET.Sdk.Worker`); Web SPA Node >=24 <25 per `web/package.json` engines, Vite build.

**Primary Dependencies**: None new. Base images only (not NuGet/npm): `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0`, `mcr.microsoft.com/dotnet/runtime:10.0`, Node build image, unprivileged Nginx alpine. `Directory.Packages.props` and `web/package.json` MUST NOT change (C4).

**Storage**: N/A (no schema change; C10).

**Testing**: No new unit/integration test project; no test file rewritten (recon §5: 0 Dockerfile hits in tests). Acceptance is C8 evidence: three `docker build` runs exit 0 from the repo root; non-root user per image via `docker inspect` / `docker run id`; structural checks (no SDK/Node in runtime, restore-before-source, `.dockerignore` coverage, pinned tags); C3 runtimeconfig check; Nginx config inspection.

**Target Platform**: Linux containers.

**Project Type**: Ops containerization of existing `src/` (.NET) + `web/` (Vite SPA) — no source-code change.

**Performance Goals**: Qualitative minimal layers (C8/A1): multi-stage, cached restore layer, `.dockerignore`. No numeric size threshold.

**Constraints**: Build context is the repository root (C6). All tags pinned, never `latest` (C5). No `HEALTHCHECK`, compose, CI, or registry steps (C9). No secrets baked in; no `Features:LocalPlay` / `Process.Start` enablement (C7). Contract chain unaffected; Web built as-is (C10).

**Scale/Scope**: 5 new files: `docker/Dockerfile.api`, `docker/Dockerfile.worker`, `docker/Dockerfile.web`, one Nginx config under `docker/`, root `.dockerignore`. Nothing else; any further file is `needs decision:` to Keel.

## Constitution Check

- New top-level `docker/` folder: approved by ticket text (C1); not a constitution departure.
- No new NuGet/npm dependency, project, layer, schema, API-shape, secret, LocalPlay, or tracked-file rewrite (C4, C7, C10; frozen file set). No departure claimed.
- Static-analysis/mutation gates: this ticket changes no `.cs` and no `web/` source, so gates with empty scope report exit 2 SKIPPED (scope-empty), non-blocking, never PASS; gates with real scope must exit 0; never lower a threshold (brief Gate expectations).
- No ADR (M ticket; ADR is L-only). No `design.md` (no ui: tag).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-21/
├── brief.md              # Approved brief (Keel, Phase 2 step 3.1)
├── spec.md               # Feature spec (this plan's input)
├── plan.md               # This file
├── CONCLUSIONS.md        # Patron grill rulings C1–C10
├── ASSUMPTIONS.md        # Taste assumptions A1–A2
└── tasks.md              # Task list (Phase 2 step 3.2 output)
```

No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md`: no data model, no API contract, no open research beyond the C3 build-time check, which is a Phase B evidence task, not plan-stage research.

### Source Code (repository root)

```text
docker/
├── Dockerfile.api        # NEW: Api multi-stage image
├── Dockerfile.worker     # NEW: Worker multi-stage image
├── Dockerfile.web        # NEW: Web SPA multi-stage image
└── nginx.conf            # NEW: Web image Nginx config (SPA + /api proxy)
.dockerignore            # NEW: root build-context exclusions
src/                     # unchanged (build inputs only)
web/                     # unchanged (build input only; output web/dist)
```

**Structure Decision**: New top-level `docker/` holds the three Dockerfiles plus the one Nginx config; root `.dockerignore` scopes the root build context. `src/` and `web/` are read-only inputs.

## Design Decisions

### DD-1 Pinned image tags (C5; Keel verifies pins at /speckit-analyze)

- `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0`, `mcr.microsoft.com/dotnet/runtime:10.0` (ticket-fixed .NET 10.0 line, C2; exempt from minor-pin wording — pinned here means no `latest` and the ticket-fixed line).
- Node build: `docker.io/library/node:24.12-alpine` — matches `web/package.json` engines `>=24 <25` and `web/.node-version` (`24`); pinned minor, never `latest`.
- Web runtime: `nginxinc/nginx-unprivileged:1.29-alpine` — unprivileged Nginx alpine variant, pinned version, never `latest`.

### DD-2 Dockerfile.api

Build stage `sdk:10.0`: copy `global.json`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `Directory.Packages.props`, `Directory.Build.props`, `src/LamuFlix.Api/LamuFlix.Api.csproj` plus referenced project files (`Core`, `Infrastructure`, `ServiceDefaults` per recon §9.2) first, run `dotnet restore` (if the `sdk:10.0` image cannot satisfy `global.json`, Phase B stops and returns to Keel and Patron rather than changing any tag), then copy `src/` and `dotnet publish -c Release`. Runtime stage `aspnet:10.0`: copy publish output only, `USER app`, `EXPOSE 8080` (the image default sets `ASPNETCORE_HTTP_PORTS=8080`; no `ASPNETCORE_HTTP_PORTS` value is set in the Dockerfile), entrypoint `dotnet LamuFlix.Api.dll`.

### DD-3 Dockerfile.worker (C3-gated)

Build stage `sdk:10.0`: copy `global.json`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `Directory.Packages.props`, `Directory.Build.props`, and `src/LamuFlix.Worker/LamuFlix.Worker.csproj` first, run `dotnet restore` (Worker has 0 ProjectReference; closure is `{LamuFlix.Worker}` per recon §9.3; if the `sdk:10.0` image cannot satisfy `global.json`, Phase B stops and returns to Keel and Patron rather than changing any tag), then copy source and `dotnet publish -c Release`. Runtime stage `dotnet/runtime:10.0`: copy publish output only, non-root user via the image built-in `USER app` (no created-user fallback is pre-authorised; if the image lacks that user or the build fails on it, Phase B stops and returns to Keel for a decision), entrypoint `dotnet LamuFlix.Worker.dll`. C3: runtime stays `runtime:10.0`; the Phase B C3 check inspects the publish runtimeconfig before the Dockerfile is accepted — `Microsoft.AspNetCore.App` present means stop and escalate (`blocked: structural`), never silently swap to `aspnet:10.0`.

### DD-4 Dockerfile.web + Nginx config

Node build stage: copy `web/package.json` plus `web/package-lock.json` (tracked) first, run `npm ci`, then copy `web/` source and run `npm run build` on the existing `web/` SPA as-is (build script runs `tsc` checks plus `vite build`; output `web/dist`, the Vite default — `vite.config.ts` sets no `outDir`). Runtime stage: pinned unprivileged Nginx with `EXPOSE 8080`; copy `web/dist` to the Nginx html directory and the new `docker/nginx.conf` (server: `listen 8080`; serve static assets; `try_files` fallback to the SPA entry for client-side routes; `location /api/` prefix-preserving `proxy_pass` with no URI part to `http://api:8080` for `/api/...` requests — a bare `/api` request without a trailing slash is not an acceptance requirement — plus `proxy_set_header` for `Host`, `X-Real-IP`, `X-Forwarded-For`, and `X-Forwarded-Proto`). The Web image requires an `api` network alias at runtime. Config and Dockerfile land together; neither is accepted without the other.

### DD-5 .dockerignore (written before any build evidence)

Must exclude these literal patterns: `.git/`, `.agents/`, `.claude/`, `.codex/`, `.cursor/`, `.gemini/`, `.junie/`, `.specify/`, `.github/`, `docs/`, `specs/`, `scripts/`, `tests/`, `**/bin/`, `**/obj/`, `web/node_modules/`, `web/dist/`, `.env`, `.env.*`, `**/*.user`, `**/*.pfx`, `**/secrets.json`, `**/appsettings.Development.json`, plus editor/OS artifacts. Must NOT exclude: `docker/`, `src/`, `web/` sources, `Directory.Build.props`, `Directory.Packages.props`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `global.json`, `web/package.json`, `web/package-lock.json`. Rationale: restores need manifests plus referenced sources; everything else stays out of the context (C6, C8).

### DD-6 Acceptance evidence (C8; primary proof alongside gates)

From the repo root, each `docker build -f docker/Dockerfile.{api,worker,web} .` exits 0 (raw output recorded). Per image: `docker inspect` / `docker run … id` shows non-root (uid not 0). Structural pass: no SDK/Node in runtime stages; restore layer precedes source copy; `.dockerignore` covers `bin/`, `obj/`, tests, `node_modules`; no `latest` tag. Worker adds the C3 runtimeconfig check with its stop-and-escalate branch. Web proxy/SPA behavior is proven by Nginx-config inspection (C9 keeps a live compose run out of scope).

## Complexity Tracking

No constitution violations to justify. No new project, layer, dependency, or abstraction.
