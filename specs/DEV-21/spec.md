# Feature Specification: Optimized multi-stage Dockerfiles (Api, Web SPA)

**Feature Branch**: `feature/DEV-21`

**Created**: 2026-10-12

**Status**: gate1: provisional

**Input**: Approved brief `specs/DEV-21/brief.md` including the Owner answer amendment (PR #136, option c — `docker/Dockerfile.worker` dropped permanently, CONCLUSIONS C13 closing C12). Sources: YouTrack DEV-21 Scope & Technical Design via `scripts/get-task.ps1`; `specs/PRODUCT.md`; `specs/DEV-21/CONCLUSIONS.md` (C1–C13); `specs/DEV-21/ASSUMPTIONS.md` (A1–A2); notes DEV-21, recon-DEV-21.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build and run the Api image (Priority: P1)

An operator builds `docker/Dockerfile.api` from the repository root and runs the resulting image; the Api serves traffic as a non-root user.

**Why this priority**: The Api is the primary backend surface; its image is the minimum viable container output of this ticket.

**Independent Test**: `docker build -f docker/Dockerfile.api .` exits 0 from the repo root, and the final image runs as a non-root user (uid not 0) via `docker inspect` / `docker run id`. Independence is file/runtime-level; ordering constraint 4 in tasks.md governs evidence order.

**Acceptance Scenarios**:

1. **Given** the repository root as build context, **When** the operator runs `docker build -f docker/Dockerfile.api .`, **Then** the build exits 0.
2. **Given** the built Api image, **When** the operator inspects/runs it (`docker inspect` / `docker run … id`), **Then** the runtime user is non-root (`USER app`).

---

### User Story 2 - Worker image — dropped permanently (no priority)

`docker/Dockerfile.worker` is dropped permanently by the closing owner answer on PR #136 (option c, CONCLUSIONS C13 closing C12): no Worker Dockerfile, publish, C3 runtimeconfig check, or Worker acceptance remains, the Worker project is untouched, and no Worker follow-up is created by DEV-21.

---

### User Story 3 - Build and serve the Web SPA image (Priority: P3)

An operator builds `docker/Dockerfile.web` from the repository root; the image serves the existing SPA as-is and reverse-proxies `/api` to `api:8080`.

**Why this priority**: The Web image delivers the frontend container independently of the .NET images.

**Independent Test**: `docker build -f docker/Dockerfile.web .` exits 0 from the repo root; the final image runs as a non-root (unprivileged Nginx) user; the Nginx config serves SPA assets with client-side-route fallback and proxies `/api/...` to `api:8080` on inspection. Independence is file/runtime-level; ordering constraint 4 in tasks.md governs evidence order.

**Acceptance Scenarios**:

1. **Given** the repository root as build context, **When** the operator runs `docker build -f docker/Dockerfile.web .`, **Then** the build exits 0.
2. **Given** the built Web image, **When** the operator inspects/runs it, **Then** the runtime user is non-root (unprivileged Nginx).
3. **Given** the new Nginx config under `docker/`, **When** it is inspected, **Then** it serves the SPA build output, falls back to the SPA entry for client-side routes, and reverse-proxies `/api/...` to `api:8080` via `location /api/` with a prefix-preserving proxy (a bare `/api` request without a trailing slash is not an acceptance requirement).

---

### Edge Cases

- A Dockerfile runtime stage containing SDK or Node tooling: rejected at acceptance (CONCLUSIONS C8).
- An image tag of `latest` anywhere: rejected; all tags are pinned to versions (CONCLUSIONS C5).
- A task adding compose files, `HEALTHCHECK`, CI image builds, registry publishing, a dependency change, a schema/API change, secrets in an image, `Features:LocalPlay`, or a tracked-file rewrite: rejected as a brief violation (CONCLUSIONS C4, C7, C9, C10).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: New file `docker/Dockerfile.api` MUST multi-stage build `src/LamuFlix.Api/LamuFlix.Api.csproj`: `mcr.microsoft.com/dotnet/sdk:10.0` build stage publishing the Api, `mcr.microsoft.com/dotnet/aspnet:10.0` runtime stage, `USER app`, entrypoint is the published Api assembly.
- **FR-002**: `docker/Dockerfile.worker` is dropped permanently by the closing owner answer on PR #136 (option c, CONCLUSIONS C13 closing C12): no Worker Dockerfile, publish, C3 runtimeconfig check, or Worker acceptance is in scope, and the Worker project is untouched.
- **FR-003**: New file `docker/Dockerfile.web` MUST multi-stage build the existing `web/` SPA as-is (`npm run build`; build output `web/dist` per the existing Vite default — `vite.config.ts` sets no `outDir`), then copy the output into an unprivileged Nginx alpine runtime with a pinned tag.
- **FR-004**: One new Nginx configuration file under `docker/` MUST serve the SPA assets, fall back so client-side routes resolve to the SPA entry, and reverse-proxy `/api/...` to `api:8080` via `location /api/` with a prefix-preserving proxy (a bare `/api` request without a trailing slash is not an acceptance requirement).
- **FR-005**: New root `.dockerignore` MUST keep the root build context minimal: exclude `bin/`, `obj/`, tests, `node_modules`, and version-control/local artifacts; it MUST NOT exclude `docker/`, `src/`, `web/` sources, `Directory.Build.props`, or `Directory.Packages.props`.
- **FR-006**: Every Dockerfile build stage MUST copy project/manifest files first for a cached restore layer, then copy source and publish; no SDK or Node tooling may appear in any runtime stage. .NET images: copy `global.json`, `CodeMetricsConfig.txt`, `BannedSymbols.txt`, `Directory.Packages.props`, `Directory.Build.props`, and the target plus referenced `.csproj` files first, run `dotnet restore`, then copy `src/` and publish. Web image: copy `web/package.json` plus `web/package-lock.json` first, run `npm ci`, then copy `web/` source and run `npm run build`.
- **FR-007**: All image tags MUST be pinned so `latest` MUST NOT appear. The ticket-fixed .NET tags (`mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0` per CONCLUSIONS C2) are exempt from minor-pin wording and stay on the ticket-fixed `:10.0` line; Node and Nginx tags MUST carry concrete pinned versions.
- **FR-008**: No image may bake in secrets, connection strings, or credentials (runtime env/config only); no Dockerfile may set `Features:LocalPlay` or enable `Process.Start` playback.
- **FR-009**: No Dockerfile may contain a `HEALTHCHECK` instruction; no compose file, CI workflow change, registry step, dependency change, schema change, or API-shape change is in scope.

### Key Entities

- **Dockerfile.api**: Multi-stage Api image definition (`docker/Dockerfile.api`); attributes: sdk:10.0 build, aspnet:10.0 runtime, `USER app`.
- **Dockerfile.worker**: Dropped permanently by the closing owner answer on PR #136 (option c, CONCLUSIONS C13 closing C12); no Worker image is delivered and the Worker project is untouched.
- **Dockerfile.web + Nginx config**: Multi-stage Web image (`docker/Dockerfile.web`) plus its Nginx config under `docker/`; attributes: Node build running `npm run build`, pinned unprivileged Nginx alpine runtime, SPA fallback plus `/api` → `api:8080` proxy.
- **.dockerignore**: Root build-context exclusion list; keeps restores working while excluding build output, tests, `node_modules`, and local artifacts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `docker build -f docker/Dockerfile.api .` and `docker build -f docker/Dockerfile.web .` each exit 0 from the repository root.
- **SC-002**: Each of the two final images (Api, Web) runs as a non-root user, evidenced by `docker inspect` / `docker run … id` (uid not 0).
- **SC-003**: Each runtime stage contains no SDK or Node tooling; each .NET Dockerfile restores before copying source; `.dockerignore` excludes `bin/`, `obj/`, tests, and `node_modules`.
- **SC-004**: The Nginx config serves the SPA with client-side-route fallback and proxies `/api/...` to `api:8080` via `location /api/` with a prefix-preserving proxy (a bare `/api` request without a trailing slash is not an acceptance requirement) on inspection; no compose stack run is required.

## Assumptions

- [assumed] `minimal layer sizes` is qualitative — multi-stage, cached restore layer, `.dockerignore` — with no numeric image-size threshold (CONCLUSIONS C8; ASSUMPTIONS A1).
- [assumed] The Web runtime is an unprivileged Nginx alpine variant with a pinned tag; the exact tag is set in `plan.md` (CONCLUSIONS C5; ASSUMPTIONS A2).
- The Web build output directory is `web/dist` (Vite default; the existing `web/vite.config.ts` sets no `outDir`).
