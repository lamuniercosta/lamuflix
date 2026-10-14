# Feature Specification: Docker Compose stack with health checks, dependency ordering, and sample library

**Feature Branch**: `feature/325-spec`

**Created**: 2026-10-13

**Status**: gate1: provisional

**Input**: YouTrack DEV-325 (parent DEV-287, size M, no UI). Decisions: `specs/DEV-325/brief.md` (including C16/C17 receipt), `CONCLUSIONS.md` (C1-C15), `ASSUMPTIONS.md` (A1-A3).

> **Owner decision C1 (option A, recorded owner answer at DEV-325:32,36).** The ticket names a `worker` service, but no runnable worker host exists. The owner answered (A): `worker` is dropped from DEV-325 and the enrichment consumer stays in the `api` service; four services are retained. This is a recorded owner answer, not a completed Gate 1 or plan freeze. Option (B), a new worker-host ticket, is not part of this feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One command brings the whole stack up healthy (Priority: P1)

A developer copies `.env.example` to `.env`, replaces every placeholder with local supplied values, runs `docker compose up -d`, and every service (postgres, rabbitmq, api, web) reaches healthy in dependency order.

**Why this priority**: AC1; everything else depends on a working stack.

**Independent Test**: With a local `.env` copied from the example and its placeholders replaced with local supplied values, run `docker compose up -d` on a fresh volume and observe every service report `healthy` in `docker compose ps`.

**Acceptance Scenarios**:

1. **Given** a local `.env` copied from `.env.example` with every placeholder replaced by a local supplied value, and DEV-21 Dockerfiles on disk, **When** `docker compose up -d` runs, **Then** postgres, rabbitmq, api and web all report healthy.
2. **Given** postgres or rabbitmq is not yet healthy, **When** compose starts, **Then** api does not start until both are `service_healthy`, and web does not start until api is `service_healthy`.
3. **Given** no OTLP collector exists, **When** the stack starts, **Then** all services still reach healthy (C12).
4. **Given** a fresh postgres volume, **When** the api starts with `Database__ApplyMigrationsOnStartup=true`, **Then** existing EF Core migrations are applied and the api reaches healthy (C3).

---

### User Story 2 - Web UI on port 3000 with a working API proxy (Priority: P1)

A developer opens http://localhost:3000 and the web service serves the SPA and proxies `/api` to the api service over the compose network.

**Why this priority**: AC2.

**Independent Test**: `curl -fsS http://localhost:3000/` returns 200 with the SPA index; `curl -fsS http://localhost:3000/api/genres` returns 200 with a JSON array (C6). Raw output recorded.

**Acceptance Scenarios**:

1. **Given** the stack is healthy, **When** the SPA index is requested at host port 3000, **Then** 200 with the SPA index.
2. **Given** the stack is healthy, **When** `/api/genres` is requested through port 3000, **Then** 200 with a JSON array.
3. **Given** the api, **When** the host is probed, **Then** no api host port is published (C14).

---

### User Story 3 - Sample library available for immediate testing (Priority: P2)

A developer has the committed placeholder files mounted read-only in the api container, and can point the mount at a real library via `LIBRARY_PATH`. The placeholders do not by themselves promise that browsing works; no import flow or sample-discovery check is in the accepted evidence plan.

**Why this priority**: Ticket Scope item 2, lower than a running stack.

**Independent Test**: Raw evidence that the default mount source is `./docker/sample-library`, the container mount is read-only and lists the placeholders, and a `LIBRARY_PATH` override changes the mount source.

**Acceptance Scenarios**:

1. **Given** default settings, **When** the stack starts, **Then** `${LIBRARY_PATH:-./docker/sample-library}` is mounted read-only at `/media/library` and `Library__RootPath=/media/library`.
2. **Given** `LIBRARY_PATH` set in `.env`, **When** compose runs, **Then** that path is mounted instead.

---

### User Story 4 - Secrets stay out of the repository (Priority: P2)

A developer supplies secrets only through an uncommitted `.env`; compose fails fast when one is missing.

**Independent Test**: Run `docker compose config --quiet` with the tested variable removed from both `.env` and the inherited environment and observe failure with the `${VAR:?message}` text (no resolved config printed); grep the repo for committed secrets.

**Acceptance Scenarios**:

1. **Given** the variable is absent from both the local `.env` and the inherited environment for any of `OMDB_API_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, **When** compose runs, **Then** it errors with the message.
2. **Given** the repository, **Then** `.env.example` holds placeholders only and no real `.env` is committed.

### Edge Cases

- OTLP exporter failures with no collector must be non-fatal; if they block health, report `needs decision` (C12).
- If placeholders fail a scanner/media check, stop with `needs recon`; never commit real media (C4).
- Manually replace every `.env.example` placeholder before a successful-start run. Required interpolation is not specified to recognize placeholders; automatic placeholder detection/rejection is not required (P-F3 rejected).
- Host-run Api daily-driver path stays unchanged: the migrate option defaults to `false`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Root `docker-compose.yml` MUST define services `postgres`, `rabbitmq`, `api`, `web` and no `worker` (C1-A).
- **FR-002**: postgres MUST use `postgres:17-alpine` with healthcheck `pg_isready`; rabbitmq MUST use `rabbitmq:4-alpine` with healthcheck `rabbitmq-diagnostics -q ping`; no `latest` tag (C13, A1).
- **FR-003**: `api` MUST `depends_on` postgres and rabbitmq with `condition: service_healthy`; `web` MUST `depends_on` api with `condition: service_healthy` (C9).
- **FR-004**: `api` and `web` MUST use compose `build:` with repository-root context and DEV-21 `docker/Dockerfile.api` / `docker/Dockerfile.web`; no registry `image:`, no Dockerfile edit (C2).
- **FR-005**: Before T008 selects the api healthcheck, the existing DEV-21 aspnet image tools MUST be inventoried and an exact exec-form invocation (including an explicit interpreter if used) established. The selected in-image-only probe MUST target `GET /health/ready` and have positive and negative proof before T008 uses it. If no viable existing-tool probe is available, stop and route to Patron; do not add tooling, edit Dockerfiles or change the endpoint/scope (C2, C7, C17). The web healthcheck MUST be `wget -q --spider http://127.0.0.1:8080/` (C7).
- **FR-006**: `api` environment MUST set `Features__LocalPlay=false`.
- **FR-007**: Every secret MUST be read as `${VAR:?message}` with no default; `.env.example` MUST list placeholders only for `OMDB_API_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD` (C8). The api `RabbitMq__UserName` and `RabbitMq__Password` MUST map to the same supplied `RABBITMQ_USER` and `RABBITMQ_PASSWORD` the rabbitmq service uses, with required interpolation and no defaults; these credentials are not `[Required]` in the options and are not caught by ValidateOnStart (recon-DEV-325:47).
- **FR-008**: Compose MUST map required non-secret options so ValidateOnStart passes: `RabbitMq__HostName=rabbitmq`, `Enrichment__SweepInterval` and `Enrichment__ClaimLease` greater than zero, and `RabbitMq__RetryDelay` strictly greater than `Enrichment__ClaimLease` (recon-DEV-325:48-49) (C8). The api MUST map `PGUSER: ${POSTGRES_USER:?message}` and `PGPASSWORD: ${POSTGRES_PASSWORD:?message}`; postgres maps `POSTGRES_USER: ${POSTGRES_USER:?message}` and `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?message}`. `ConnectionStrings__DefaultConnection` MUST be the non-secret literal `Host=postgres;Database=lamuflix`; it contains no credentials and is not composed from them (C16). Postgres sets `POSTGRES_DB=lamuflix`; no defaults or new supplied variables. Phase B MUST verify credential delivery through the real Api/Postgres connection without printing credentials or resolved secret-bearing configuration; this is future evidence, not a current success claim.
- **FR-009**: The Api MUST apply existing EF Core migrations at startup only when validated option `Database:ApplyMigrationsOnStartup` is `true`; default `false`; compose sets `true` (C3). No schema or migration change.
- **FR-010**: Sample library MUST be committed under `docker/sample-library/` as 3-5 placeholder files named `Title (Year).ext`, total under 100 KB, mounted `${LIBRARY_PATH:-./docker/sample-library}:/media/library:ro` with `Library__RootPath=/media/library` (C4, A2).
- **FR-011**: `web` MUST publish `3000:8080` only; `api` MUST publish no host port; `web/vite.config.ts` and nginx conf untouched (C5, C14).
- **FR-012**: No OTLP endpoint env and no collector service (C12); no `docker-compose.observability.yml` (C10).
- **FR-013**: `README.md` MUST gain exactly one section `Run with Docker Compose`: copy `.env.example` to `.env`, `docker compose up -d`, open localhost:3000, `LIBRARY_PATH` override (C11, A3).

### Key Entities

None (no data model change).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001 (AC1)**: `docker compose up -d` on a fresh volume results in all four services `healthy`, evidenced with raw `docker compose ps` output, with no collector present.
- **SC-002 (AC2)**: The two C6 curl proofs (frozen commands) return HTTP 200 with the SPA index and a JSON array respectively; raw output recorded, supplemented with explicit status-code evidence.
- **SC-003**: Structural checks pass: no `worker` service, no secret default, no committed `.env`, only placeholders in `docker/sample-library/`, tags per C13, no api host port.
- **SC-004**: Changed C# files pass the analyzer, complexity, inspection gates and the test suite with exit 0; the mutation, property-test, format and web gates are reported by their own verdicts (exit 2 never PASS).

## Assumptions

- A1 [assumed] Image variants `postgres:17-alpine`, `rabbitmq:4-alpine` (no management UI).
- A2 [assumed] Placeholder names `Title (Year).ext`, 3-5 files, under 100 KB total.
- A3 [assumed] README section title `Run with Docker Compose`.
- DEV-325 Phase B starts only after DEV-21 Phase B merges (C2).

## Out of Scope

- A `worker` service, a fake worker, or a new worker-host project (C1).
- `docker-compose.observability.yml`, OTLP collector (C10, C12).
- Editing any DEV-21 file or adding HEALTHCHECK to any Dockerfile (C2); editing `web/vite.config.ts`, nginx conf or web source (C5).
- Any NuGet/npm dependency, schema/migration, public API or contract-chain change.
- Committing a real `.env`, any secret, or real media; any `Features:LocalPlay` value but `false`.
- Fixing DEV-21 `Dockerfile.worker` (recorded side effect, commented on DEV-21 by Rigger).

