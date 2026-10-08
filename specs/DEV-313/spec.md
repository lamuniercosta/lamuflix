# Feature Specification: DEV-313 API End-to-End Integration Suite

**Feature Branch**: `feature/313-spec`

**Created**: 2026-10-08

**Status**: Draft - gate1: provisional

**Input**: brief.md, CONCLUSIONS.md (Q1-Q8), recon-DEV-313 sections 10-11, specs/PRODUCT.md

## User Scenarios & Testing

### User Story 1 - Browse library over real HTTP and Postgres (Priority: P1)

A client browses the movie library through HTTP against a real Postgres-backed host.

**Why this priority**: Covers the ticket's browse bullet and the largest existing seam (real Postgres yes, HTTP no).

**Independent Test**: Seed rows via real DbContext, issue GET /api/movies with filter/paging/sort combinations, assert HTTP status plus persisted JSON shape.

**Acceptance Scenarios**:

1. **Given** seeded movies with varied genres, actors, years, runtimes, statuses and watchlist flags, **When** GET /api/movies with combined filters, **Then** only matching rows return.
2. **Given** seeded rows, **When** GET /api/movies across page boundaries, **Then** paging is stable and total counts match.
3. **Given** rows with sort ties and null sort keys, **When** GET /api/movies with sort/direction, **Then** ordering is stable with nulls placed per contract.
4. **Given** invalid browse inputs, **When** GET /api/movies, **Then** 422 application/problem+json with field-keyed errors.

---

### User Story 2 - Movie details, facets and watchlist over HTTP (Priority: P1)

A client reads details, facet lists and mutates the watchlist through HTTP with persistence proof.

**Why this priority**: Completes the read/library envelope and the watchlist mutations inside the frozen nine-operation scope.

**Independent Test**: Seed rows, call each endpoint over HTTP, then re-read persisted state through the details endpoint or DbContext.

**Acceptance Scenarios**:

1. **Given** a seeded movie, **When** GET /api/movies/{id}, **Then** 200 with the frozen scalar shape; unknown id returns 404.
2. **Given** seeded metadata, **When** GET /api/genres and GET /api/people, **Then** facet values derive from persisted metadata.
3. **Given** a seeded movie, **When** POST then DELETE /api/movies/{id}/watchlist, **Then** each mutation persists the watchlist transition.

---

### User Story 3 - Import folder through real scanner to OMDb enrichment (Priority: P1)

A client imports a media folder; the host scans the real filesystem, enqueues, consumes via real RabbitMQ, calls OMDb over HTTP, and persists enrichment.

**Why this priority**: Ticket import bullet plus the causal import-to-OMDb proof frozen in Q3.

**Independent Test**: Create real temp `<Title> (<Year>)` folder with one tiny .mkv/.mp4 file, POST /api/movies/import, poll persisted status to Enriched, confirm via GET details and WireMock request log.

**Acceptance Scenarios**:

1. **Given** a real temp folder named `<Title> (<Year>)` with one tiny supported video file, **When** POST /api/movies/import, **Then** 202 with relative Location, empty body, a persisted movie, and eventual Enriched status caused by a matching WireMock OMDb request.
2. **Given** an invalid folderPath (blank or containing `..`), **When** POST /api/movies/import, **Then** 422 application/problem+json with field-keyed errors.

---

### User Story 4 - Enrichment retry through real consumer to OMDb (Priority: P2)

A client retries enrichment on an eligible movie; the host consumer calls OMDb and persists the outcome.

**Why this priority**: Ticket retry bullet plus the causal retry-to-OMDb proof frozen in Q3.

**Independent Test**: Seed an eligible movie, POST enrichment, poll to Enriched, confirm via GET details and WireMock log; assert 409/404 contracts on ineligible/missing rows.

**Acceptance Scenarios**:

1. **Given** a seeded eligible movie, **When** POST /api/movies/{id}/enrichment, **Then** 202 with Location and eventual Enriched status caused by a matching WireMock OMDb request.
2. **Given** a movie already enriched or pending, **When** POST /api/movies/{id}/enrichment, **Then** 409 conflict; unknown id returns 404.

---

### User Story 5 - Disabled playback safety (Priority: P2)

A client attempts playback while LocalPlay is disabled; the host rejects without launching a process.

**Why this priority**: Ticket play bullet under the Q2 tests-only ruling.

**Independent Test**: With Features:LocalPlay=false, POST play over HTTP and assert 403 problem JSON plus zero IProcessStarter.Start calls.

**Acceptance Scenarios**:

1. **Given** Features:LocalPlay=false, **When** POST /api/movies/{id}/play, **Then** 403 application/problem+json and zero process-start calls.

---

### Edge Cases

- Polling exceeds the 30s wall-clock deadline: the test fails loudly with diagnostics, no secret values in output.
- ClaimLease expiry needed mid-poll: only the pre-build injected TimeProvider advances the lease; the poll bound stays wall-clock.
- Temp import folder deleted after host stop so no residue remains.
- Warm-up provider request is followed by WireMock reset; warm-up traffic is never asserted as endpoint proof.

## Requirements

### Functional Requirements

- **FR-001**: Suite MUST cover all nine operations end-to-end through WebApplicationFactory: GET /api/movies, GET /api/movies/{id}, POST /api/movies/import, POST /api/movies/{id}/enrichment, POST /api/movies/{id}/play, POST and DELETE /api/movies/{id}/watchlist, GET /api/genres, GET /api/people.
- **FR-002**: The five ticket Scope bullets MUST be mandatory cases inside that envelope: browse filter/paging/sort, import 202 + Location, validation 422 ProblemDetails, disabled-play 403, enrichment retry.
- **FR-003**: Import and retry success MUST prove the causal chain HTTP -> EF/Postgres -> real RabbitMQ publisher -> API-host EnrichmentConsumer -> real OmdbMetadataProvider -> WireMock -> persisted Enriched, observed by bounded DB polling then GET details plus matching WireMock request log.
- **FR-004**: Play coverage MUST stay tests-only with Features:LocalPlay=false, asserting 403 problem JSON and zero IProcessStarter.Start calls; no test enables LocalPlay and no src playback code changes.
- **FR-005**: Host MUST be configured before build with fixture DefaultConnection, RabbitMQ host/port/credentials, loopback Omdb:BaseUrl, non-secret sentinel Omdb:ApiKey, Enrichment:ClaimLease=00:00:01, RabbitMq:RetryDelay=00:00:02, preserving persistence/metadata-before-consumer registration order.
- **FR-006**: Suite MUST use owned container instances in its own nonparallel collection; host MUST stop/dispose before DB, broker-topology or WireMock-log reset.
- **FR-007**: Import MUST use a real temp `<Title> (<Year>)` directory with one tiny .mkv/.mp4 file, deleted after host stop; no playable media, no process execution.
- **FR-008**: Suite MUST be additive: ten new files under tests/LamuFlix.IntegrationTests; existing double-based endpoint tests and focused EF/provider/consumer suites stay untouched and are not duplicated.
- **FR-009**: Assertions MUST be deterministic hand-authored seeds with semantic JSON/DTO/header/status checks and generated IDs; no Verify, AutoFixture or Faker references added.

### Key Entities

- **ApiEndToEnd harness**: Factory, fixture, collection and base lifecycle composing existing ApiHostFactory, PostgresFixture, RabbitMqFixture, MovieCatalogSeed and probe patterns.
- **Seeded movie**: Deterministic row created via real DbContext driving browse, details, facet, watchlist, retry and playback scenarios.
- **Import folder**: Real temp `<Title> (<Year>)` directory with one tiny supported video file.
- **Enrichment proof**: Persisted Enriched status plus matching WireMock OMDb request for one import and one retry.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All nine operations execute against the real-backed host with green results.
- **SC-002**: One import and one retry each show persisted Enriched status with a matching WireMock request in the log.
- **SC-003**: Full IntegrationTests regression stays green with no existing test modified.
- **SC-004**: Required verification gates report actual outcomes with no threshold or harness edits.

## Assumptions

- Patron closed the grill at Q8 with shared understanding; no structural blocker and no owner checkbox arose.
- No taste rulings requiring ASSUMPTIONS.md; no new domain vocabulary or ADR needed.
- Pre-existing `.specify/feature.json` and `harness.yml` modifications are preserved and excluded from DEV-313 authorship.
