# Feature Specification: DEV-312 OpenAPI Contract and Scalar Delivery

**Feature Branch**: `feature/312-spec`

**Created**: 2026-10-08

**Status**: gate1: provisional

**Input**: Frozen `specs/DEV-312/brief.md`, `CONCLUSIONS.md` Q1–Q5, `recon-DEV-312` sections 1–9, `specs/PRODUCT.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Read the API contract in Development (Priority: P1)

A developer runs the API in Development and reads the generated OpenAPI document and the Scalar UI.

**Why this priority**: The document and Scalar are the first two ticket deliverables; every later contract step depends on them existing.

**Independent Test**: Start the API in Development, GET `/openapi/v1.json` returns the document and GET `/scalar` returns the Scalar UI.

**Acceptance Scenarios**:

1. **Given** the API running in Development, **When** GET `/openapi/v1.json`, **Then** the OpenAPI 3.1 document named v1 is returned.
2. **Given** the API running in Development, **When** GET `/scalar`, **Then** the Scalar UI is returned and wired to that document.
3. **Given** the API running in Development, **When** the document paths are listed, **Then** exactly the nine business path/method pairs in the operation table below are present and no health route is present.

---

### User Story 2 - Silent drift fails the build (Priority: P1)

A change to an endpoint, DTO, or status code that alters the generated document fails the existing CI Test step until the committed contract is regenerated and reviewed.

**Why this priority**: Ticket acceptance requires the snapshot test to block silent drift; the committed `web/src/api/openapi.json` is the contract consumers use in every environment.

**Independent Test**: Mutate one contract field in the generated document and run the drift test; it fails and writes no tracked file. Regenerate through the documented opt-in step and the test passes with the `openapi.json` diff as the review surface.

**Acceptance Scenarios**:

1. **Given** the committed `web/src/api/openapi.json`, **When** the drift test runs unmodified, **Then** it passes.
2. **Given** a generated document that differs from the committed baseline, **When** the drift test runs in CI, **Then** it fails and no tracked file is written.
3. **Given** a deliberate contract change, **When** the maintainer runs the documented opt-in regeneration, **Then** `openapi.json` and any derived snapshot are replaced together in one commit for human review.

---

### User Story 3 - Production exposes no documentation (Priority: P2)

A Production host serves business routes with no documentation surface.

**Why this priority**: Security care item; the full endpoint map, including the LocalPlay-gated play route, stays off production hosts.

**Independent Test**: Start the host with Production environment and GET both documentation routes; each returns 404.

**Acceptance Scenarios**:

1. **Given** the host running in Production, **When** GET `/openapi/v1.json`, **Then** 404.
2. **Given** the host running in Production, **When** GET `/scalar`, **Then** 404.

---

### User Story 4 - Regenerate the contract on purpose (Priority: P2)

A maintainer regenerates the committed contract through one explicit local step that uses the same Development host and document path as the drift test.

**Why this priority**: The only legitimate way to change the baseline; everything else is drift.

**Independent Test**: Run the quickstart command; the generated file equals the drift-test input and CI never enables the opt-in switch.

**Acceptance Scenarios**:

1. **Given** a clean checkout, **When** the quickstart regeneration command runs locally, **Then** the output equals the document the drift test compares.
2. **Given** a CI run, **When** the test suite runs, **Then** the opt-in switch is unset and the normal drift test never writes.

---

### Edge Cases

- What happens when generation runs on another machine or line-ending convention? The document must be deterministic; the only permitted normalization is one fixed rule (line endings) recorded in plan.md, never scrubbing of paths, operations, parameters, schemas, status codes, or info title/version.
- How does the test behave when Verify writes a `.received.*` file? Received files stay untracked; a `.gitignore` exclusion is added only if existing rules do not already cover `*.received.*`.
- What happens when the play route appears in the Development document? It is included as contract metadata only; Production tests never invoke it and `Features:LocalPlay` stays false in every test host.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST register the OpenAPI document services with `AddOpenApi` for document name `v1` and serve the JSON document at GET `/openapi/v1.json`.
- **FR-002**: System MUST serve the Scalar UI at `/scalar` wired to that document.
- **FR-003**: Both documentation mappings MUST exist only in Development; in Production both routes MUST return 404.
- **FR-004**: The generated document MUST cover exactly the nine business operations below and MUST exclude the `/health/live` and `/health/ready` mappings, which stay untouched with no metadata edits.
- **FR-005**: Document inclusion MUST be restricted to `/api` business operations through an `/api`-path document transformer in `Program.cs` (research.md D1), with exact path-boundary matching for `/api` business paths and removal of non-business paths, carrying no `Microsoft.AspNetCore.Mvc` assembly reference; `ApiHostCompositionTests.cs` MUST remain unchanged.
- **FR-006**: The generated document MUST default to OpenAPI 3.1 with no version override.
- **FR-007**: The committed `web/src/api/openapi.json` MUST be the sole authoritative baseline and MUST never be hand-edited.
- **FR-008**: The drift test MUST compare the complete generated document and MUST fail on any difference with no auto-accept and no tracked-file writes in normal or CI runs.
- **FR-009**: If Verify naming cannot target the exact baseline path, a derived `*.verified.json` supporting snapshot is allowed ONLY with full generated-document equality to the committed `openapi.json` asserted in the same run after one fixed normalization.
- **FR-010**: Scrubbing is allowed ONLY for non-contract values varying by machine/run, each explicitly named in plan.md; paths, operations, parameters, schemas, status codes, and info title/version MUST never be scrubbed.
- **FR-011**: Intentional regeneration MUST use one explicit local opt-in path in the IntegrationTests project through the same Development host and document path, replacing `openapi.json` plus any derived snapshot together; CI MUST never set the switch.
- **FR-012**: `Microsoft.Extensions.ApiDescription.Server` (build-time generation) MUST NOT be used; generation is runtime/test-host based.
- **FR-013**: No npm package, TypeScript generation, `types.ts`, client, MSW change, `package.json` script, business route/status/DTO/handler change, new CI job/step/workflow edit, or new script file.
- **FR-014**: Existing `ApiHostCompositionTests.cs` MUST remain unchanged and green; `Features:LocalPlay` MUST stay false in test hosts and the play route MUST never be invoked by tests.
- **FR-015**: Dependencies (`Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, Verify xUnit v3 integration) MUST be pinned through Central Package Management with compatible versions supported by cited documentation. `Microsoft.AspNetCore.OpenApi` is an installable first-party NuGet package, not an in-box framework reference (recon-DEV-312:118); ticket "built-in" wording is retained unchanged per CONCLUSIONS.md Q1.

### Exact business operation set

From `recon-DEV-312:73-82`, `/api` prefix per `:72`. Tests count path/method pairs, not nine unique paths. This table records source route templates; membership assertions compare actual generated pairs against the expected generated set named in plan.md (with `GET /api/movies/{id}` for the `:int`-constrained source template), not these template spellings.

| Method | Path |
|---|---|
| GET | /api/movies |
| GET | /api/movies/{id:int} (source route template) |
| POST | /api/movies/import |
| POST | /api/movies/{id}/enrichment |
| POST | /api/movies/{id}/watchlist |
| DELETE | /api/movies/{id}/watchlist |
| POST | /api/movies/{id}/play |
| GET | /api/genres |
| GET | /api/people |

### Key Entities

- **OpenAPI document**: The generated v1 contract; authoritative copy committed at `web/src/api/openapi.json`.
- **Drift test**: The `Verify`-based integration test proving committed-document equality, determinism, and fail-closed mutation behavior.
- **Derived snapshot**: An optional `*.verified.json` supporting artifact, permitted only under FR-009; never a consumer contract.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scalar answers at `/scalar` in Development and the document at `/openapi/v1.json` lists exactly the nine business path/method pairs with retained DTO/status metadata.
- **SC-002**: The drift test passes on the committed baseline and fails on any generated-document difference without writing a tracked file.
- **SC-003**: Both documentation routes return 404 in Production.
- **SC-004**: Repeated generation produces a byte-identical result after the single fixed normalization.
- **SC-005**: `docs/adr/ADR-0007.md` (Keel-owned) reaches Status Accepted and the existing composition plus full test suites stay green.
