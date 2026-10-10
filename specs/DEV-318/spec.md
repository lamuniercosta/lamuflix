# Feature Specification: DEV-318 End-to-End Import-to-Enrichment Integration Proof

**Feature Branch**: `feature/318-spec`

**Created**: 2026-10-09

**Status**: gate1: provisional

**Input**: Ticket summary in DEV-318 note:10-11; grill decisions in specs/DEV-318/brief.md (Q1-Q7, closed 7/12); facts in recon-DEV-318:18-136,146-182; specs/PRODUCT.md; constitution I, VI-IX.

## Clarifications

L-path clarify pass held with the decision set in brief.md. The Patron grill closed with
shared understanding at Q7 of 12; Q1-Q7 are approved and persisted in CONCLUSIONS.md.
No open clarification items, no `[assumed]` decisions, no ASSUMPTIONS.md. A gap found
during drafting would be reported as `needs decision:` to Keel, never guessed; none was found.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pending-to-Enriched HTTP import proof (Priority: P1)

Extend the existing `ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence`
scenario additively: POST `/api/movies/import` against the real API host (WebApplicationFactory)
with real Postgres and RabbitMQ Testcontainers and a WireMock OMDb stub, observe the movie as
Pending through a deterministic test seam, release enrichment, then observe Enriched and assert
the unchanged GET details payload plus measured OMDb evidence.

**Why this priority**: This is the ticket acceptance: API import, queue, hosted consumer, and
OMDb proven as one deterministic path. Nothing else in this feature has value without it.

**Independent Test**: Run the single extended scenario; it passes only when POST returns 202
with a Location id, persisted status reads Pending before release and Enriched after, GET
returns 200 with enriched metadata, and exactly one measured OMDb request is recorded.

**Acceptance Scenarios**:

1. **Given** the API host running with the real stack and a held enrichment gate, **When**
   POST `/api/movies/import` completes, **Then** status is 202, the Location header yields the
   movie id, and a fresh Postgres context reads Pending for that id.
2. **Given** Pending observed and the gate released, **When** enrichment completes within the
   bounded wait, **Then** persisted status is Enriched and GET `/api/movies/{id}` returns 200
   with the existing MovieDetailsResponse contract and all metadata fields.
3. **Given** the completed run, **When** WireMock logs are inspected, **Then** exactly one GET
   request with the sentinel apikey, folder title, and type=movie is recorded.

---

### User Story 2 - Request-correlated trace ancestry proof (Priority: P2)

In the same run, prove one trace spans HTTP request, broker publish, active consumer, and
metadata lookup: capture stopped spans with a disposable ActivityListener, send a unique W3C
traceparent/tracestate with no ambient test Activity, and assert parent-span linkage
(API server span, import ancestry, Enrichment.Enqueue, RabbitMQ.Client.Publisher publish span,
Enrichment.Process consumer, processing handler, Metadata.Lookup) plus Consumer kind,
propagated tracestate, and no redelivery link.

**Why this priority**: Constitution VI requires one trace across API, RabbitMQ, consumer, and
provider. A shared TraceId alone does not pass; ancestry must be proven. It rides the US1 run
and is not an independent delivery.

**Independent Test**: Cannot be tested without US1; verified by the trace assertions inside the
same scenario run, failing with the missing span named.

**Acceptance Scenarios**:

1. **Given** a unique request traceparent and pre-host capture, **When** the US1 run completes,
   **Then** every named span shares the request TraceId, import ancestry and Enrichment.Enqueue
   descend from the API server span, publish.ParentSpanId equals enqueue.SpanId,
   consumer.ParentSpanId equals publish.SpanId, the processing handler descends from the
   consumer span, and Metadata.Lookup descends from the processing handler span.
2. **Given** this first delivery, **When** consumer activity is inspected, **Then** kind is
   Consumer, tracestate matches propagation, and zero links hold.

### Edge Cases

- Gate never reached (consumer did not arrive): bounded wait fails with an explicit stuck-gate
  diagnosis naming the missing arrival, not a bare timeout.
- Enriched never observed after release: fail naming the missing span/status; gate is still
  released in finally before host disposal.
- Redelivery, retry, DLQ, broker failure, and OTLP exporter scenarios are outside this
  positive-path proof and must not be asserted here.
- Ambient test-process Activity present at request time: forbidden; the request must carry only
  the unique traceparent so correlation is by request TraceId, never global span counts.
- Verbatim ticket text requiring a Status field in the GET body: see Provenance caveat below;
  on that evidence the ruling becomes `blocked: structural`, never a silent API change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Test MUST extend the existing import scenario additively; every existing
  import, scanner, details, metadata, measured WireMock, and owned-folder cleanup assertion
  MUST remain unweakened.
- **FR-002**: Test MUST reuse ApiEndToEnd WebApplicationFactory, real Postgres and RabbitMQ
  Testcontainers, WireMock OMDb, and the active EnrichmentConsumer hosted in the API; the
  retired worker project MUST NOT be resurrected and no host, project, or dependency added.
- **FR-003**: Synchronization MUST use a cancellation-aware asynchronous gate wrapping the
  fully decorated production `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`
  outside its decorator chain and before TryClaimForEnrichmentAsync, via the existing
  installStubs/configureTestServices seam; the gate MUST start no Activity.
- **FR-004**: Gate registration MUST capture the original scoped ImplementationFactory
  descriptor, assert its expected scoped factory form, and replace only that interface
  descriptor with a scoped outer gate invoking the captured factory with the same scoped
  provider; never recursively resolve the replaced interface, construct the bare handler, copy
  decorator composition, create a second provider, or retain scoped services in per-test state.
- **FR-005**: Test MUST assert Pending and Enriched from persisted Postgres state using the
  existing status seam, parse the movie id from the 202 Location header, and assert GET 200
  with the unchanged MovieDetailsResponse contract; no public Status field, OpenAPI, or
  generated TypeScript change.
- **FR-006**: Trace capture MUST install a disposable ActivityListener before host startup,
  sample AllData, record stopped spans thread-safely, use TelemetryConstants names, wait
  boundedly for stopped spans after Enriched, and dispose in finally.
- **FR-007**: Gate MUST be released in finally before host shutdown, use a bounded timeout,
  pass ValidateScopes/ValidateOnBuild, keep continuations asynchronous with cancellation, and
  hold no scoped-service state across the test.
- **FR-008**: No production code, package/project, schema/migration, public API, LocalPlay,
  or secret change is authorized; the existing test alteration is the ticket-forced edit.
- **FR-009**: Time MUST flow through TimeProvider and existing test-clock seams; the existing
  30-second integration wait budget with cancellation applies; these waits observe test
  completion, not production retry policy.
- **FR-010**: Queue, repository, and metadata provider MUST NOT be mocked and no fixed response
  delay substituted; production delivery MUST NOT be stolen with BasicGet for inspection.

### Key Entities *(include if feature involves data)*

- **Movie**: persisted row whose EnrichmentStatus transitions Pending to Enriched during the run.
- **EnrichmentStatus**: Pending (0) to Enriched (1); numeric values are the database contract.
- **Trace spans**: API server span, Enrichment.Enqueue, publisher publish span,
  Enrichment.Process consumer span, processing handler span, Metadata.Lookup span.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The extended scenario passes deterministically: 202 with Location id, Pending
  before release, Enriched after, GET 200 with full metadata, one measured OMDb request.
- **SC-002**: Trace assertions prove full ancestry (parent-span equalities, Consumer kind,
  tracestate, no redelivery link) correlated by the request TraceId on every run.
- **SC-003**: No new production, contract, schema, or dependency surface: diff touches only
  the bounded test file set; all pipeline gates meet their required expectations.

## Assumptions

- The ApiEndToEnd fixture, factory, and base remain unchanged and supply real Postgres,
  RabbitMQ, WireMock OMDb, sentinel credentials, and container-generated settings.
- The existing owned real-scanner folder, 30-second wait budget, and owned-folder cleanup
  cover setup, waiting, and teardown needs.
- Existing property tests still run; `propertyTests: opt-out — no domain invariants change`
  is recorded in the DEV-318 task note for the no-tag exit-2 acceptance.
- The L-path ADR is drafted at its later step, not in these artifacts.

## Provenance caveat (carried to the spec PR)

The supplied DEV-318 note contains a ticket summary. On that evidence, persisted status plus
unchanged GET metadata meets the ticket and needs no owner checkbox. If verbatim ticket text
explicitly requires a Status field in the GET response, the ruling becomes
`blocked: structural`; do not silently broaden the API or drop that requirement.
