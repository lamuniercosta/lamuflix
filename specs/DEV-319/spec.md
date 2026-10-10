# Feature Specification: DEV-319 TelemetryConstants, named spans, custom metrics, ADR-0008

**Feature Branch**: `feature/319-spec`

**Created**: 2026-10-10

**Status**: gate1: provisional

**Input**: Ticket DEV-319 *Scope & Technical Design* as recorded in recon-DEV-319:24-39; `specs/DEV-319/brief.md` (grill closed 4/12, frozen); `specs/DEV-319/CONCLUSIONS.md` (Patron S1-S2, P1-P8, Q1-Q4 ratified); recon-DEV-319 (facts only); `specs/PRODUCT.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enrichment lookup outcomes are counted (Priority: P1)

Each resolved enrichment lookup attempt increments `lamuflix.enrichment.outcome` once with the ticket-frozen dimensions, so operators can break down enrichment results without reading traces.

**Why this priority**: This is the core of AC1; every other metric builds on the same instrument holder.

**Independent Test**: Unit tests with a test-only `MeterListener` on `ProcessEnrichmentCommandHandler`: Found records `outcome=enriched`; NotFound records `outcome=not_found`; provider Failed and classified lookup exceptions record `outcome=failed` with `failure_category` set to the actual `Category.Code`; refused claims and requested cancellation record nothing; a second lookup attempt records a second measurement.

**Acceptance Scenarios**:

1. **Given** a lookup that finds metadata, **When** the handler classifies the result, **Then** the outcome counter records one measurement with `outcome=enriched` and no `failure_category` tag.
2. **Given** a lookup that finds nothing, **When** the handler classifies the result, **Then** the outcome counter records one measurement with `outcome=not_found` and no `failure_category` tag.
3. **Given** a provider failure or classified lookup exception, **When** the handler classifies the result, **Then** the outcome counter records one measurement with `outcome=failed` and `failure_category` equal to the actual `Category.Code` (`provider_unavailable`, `rate_limited`, `invalid_response`, or `unknown`).
4. **Given** a refused claim (already-terminal, absent, or unexpired-lease delivery), **When** the consumer settles, **Then** no outcome measurement is recorded.
5. **Given** requested cancellation, **When** the lookup aborts, **Then** no outcome measurement is recorded.
6. **Given** a malformed-body or other pre-lookup error before classification, **When** the handler rejects it, **Then** no outcome measurement is recorded and existing error traces are retained with no fabricated `failure_category`.
7. **Given** a classified lookup followed by a persistence, republish, or settlement failure, **When** the failure propagates, **Then** no additional outcome measurement is recorded and existing error traces are retained with no fabricated `failure_category`.

---

### User Story 2 - Lookup duration is recorded (Priority: P1)

Every registered Infrastructure `OmdbMetadataProvider` lookup records its elapsed time once in `lamuflix.enrichment.duration`, so operators can observe provider latency including retries and failure handling.

**Why this priority**: Duration is the second ticket instrument and shares the AC1 naming/dimension proof.

**Independent Test**: Unit/integration tests with controlled `TimeProvider` timestamps and a test-only `MeterListener`: a Found, Failed, and cancelled lookup each record exactly one `Histogram<double>` measurement in seconds with only the tag `provider=omdb`.

**Acceptance Scenarios**:

1. **Given** a successful lookup, **When** it returns, **Then** one duration measurement is recorded with unit `s` and tag `provider=omdb`.
2. **Given** a failed lookup, **When** it returns a Failed outcome, **Then** one duration measurement is still recorded with the same unit and tags.
3. **Given** a cancelled lookup, **When** the token propagates, **Then** one duration measurement is still recorded while no outcome measurement is recorded.

---

### User Story 3 - Persisted imports are counted (Priority: P2)

Each movie that survives `SaveChangesAsync` increments `lamuflix.import.count` once, so the counter reflects durable imports rather than accepted commands.

**Why this priority**: Third ticket instrument; its placement rule (after save, before enqueue) is the frozen Q2 ruling.

**Independent Test**: Extend the existing `ImportMovieFolderCommandHandlerTests` success/failure tests with a test-only `MeterListener`: success records one untagged measurement; scan/add/save/duplicate failures record zero; an enqueue failure after a successful save records one.

**Acceptance Scenarios**:

1. **Given** a successful import save, **When** the handler proceeds to enqueue, **Then** the import counter records one measurement with no tags.
2. **Given** a scan, add, save, or duplicate failure, **When** the handler throws, **Then** the import counter records nothing.
3. **Given** an enqueue failure after a successful save, **When** the exception propagates, **Then** the import counter still records one measurement because the movie persists.

---

### User Story 4 - Named spans, attributes, and ADR-0008 are delivered (Priority: P2)

Existing span names, attributes, and parent/link behavior are preserved and verified, and `docs/adr/ADR-0008.md` records the Accepted polling decision for AC2.

**Why this priority**: Preservation proof plus the ticket's second deliverable; no behavior change on these paths.

**Independent Test**: Existing span/attribute assertions keep passing unchanged; host composition proof receives actual instrument identities; ADR-0008 exists with status Accepted, decision polling in P1 with SignalR deferred, and no endpoint, hub, package, interval, or future commitment.

**Acceptance Scenarios**:

1. **Given** the existing `Enrichment.Enqueue`, `Enrichment.Process`, and `Metadata.Lookup` spans, **When** the suite runs, **Then** all existing name, kind, attribute, and parent/link assertions still pass unchanged.
2. **Given** the ticket text, **When** AC2 is checked, **Then** `docs/adr/ADR-0008.md` exists with status Accepted and records polling for enrichment status in P1 with SignalR deferred.

---

### Edge Cases

- A retried lookup records one outcome measurement per attempt, not one per message.
- A lookup that both classifies a result and later fails persistence records exactly one outcome measurement.
- A cancelled lookup records duration but never an outcome or category.
- An unknown failure category can only come from the closed `EnrichmentFailureCategory.Code` set; no second category vocabulary is introduced.
- A retried import command cannot re-insert the same movie, so the import counter cannot double-count it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application meter identity MUST be `TelemetryConstants.ActivitySourceName` (`LamuFlix`); no second identity constant.
- **FR-002**: The ticket MUST add exactly three instruments and no others: `lamuflix.enrichment.duration` (Histogram), `lamuflix.enrichment.outcome` (Counter), `lamuflix.import.count` (Counter).
- **FR-003**: Metric names, tag keys (`provider`, `outcome`, `failure_category`), and outcome literals (`enriched`, `not_found`, `failed`) MUST stay exactly as ruled; renaming, a fourth value, or an extra tag is out of scope.
- **FR-004**: The outcome counter MUST be emitted in `ProcessEnrichmentCommandHandler` where the actual lookup result is known: Found adds 1 with `outcome=enriched`; NotFound adds 1 with `outcome=not_found`; provider Failed or classified lookup exception adds 1 with `outcome=failed` plus `failure_category` set to the actual `Category.Code`.
- **FR-005**: Refused claims (already-terminal, absent, unexpired-lease) and requested cancellation MUST record no outcome measurement.
- **FR-006**: One outcome measurement MUST be recorded per resolved lookup attempt including retries, at result classification, independently of later persistence/settlement; the result and exception paths MUST never double-count.
- **FR-007**: The duration instrument MUST be `Histogram<double>` with unit `s` and only the tag `provider=omdb`, measuring the whole registered Infrastructure `OmdbMetadataProvider` lookup from entry to exit with injected `TimeProvider.GetTimestamp`/`GetElapsedTime`, recorded exactly once in `finally`, including failed and cancelled calls.
- **FR-008**: The import instrument MUST be `Counter<long>` with no tags; `ImportMovieFolderCommandHandler` MUST call `Add(1)` immediately after successful `SaveChangesAsync` and before `EnqueueAsync`, preserving exception, order, and API behavior.
- **FR-009**: The outcome instrument MUST be `Counter<long>` with tag `outcome`, plus `failure_category` only when `outcome=failed`; `enriched` and `not_found` MUST omit `failure_category` entirely with no empty/null placeholder.
- **FR-010**: `TelemetryConstants` MUST gain only the ruled literal constants (`EnrichmentDurationMetricName`, `EnrichmentOutcomeMetricName`, `ImportCountMetricName`, `ProviderTagName`, `OutcomeTagName`, `FailureCategoryTagName`, `EnrichedOutcome`, `NotFoundOutcome`, `FailedOutcome`, `OmdbProviderName`) and remain a string-constant catalogue.
- **FR-011**: Instruments MUST live in a new concrete static BCL holder at `src/LamuFlix.Core/Pipeline/EnrichmentMetrics.cs` with one process-lifetime static `Meter` named `ActivitySourceName`, holding exactly the duration histogram and two counters; no interface, wrapper service, DI registration, `IMeterFactory` requirement, OpenTelemetry type, or new package.
- **FR-012**: Existing named spans (`Enrichment.Enqueue`, `Enrichment.Process`, `Metadata.Lookup`), kinds, attributes (`lamuflix.movie.id`, `messaging.rabbitmq.delivery_count`, `error.type`), parent/link behavior, the span key `enrichment.outcome` with `completed`/`skipped` values, and `lamuflix.handler.outcome` MUST be preserved unchanged.
- **FR-013**: The existing ServiceDefaults `AddMeter` subscription MUST remain the composition path; no `ServiceDefaults` registration change.
- **FR-014**: `docs/adr/ADR-0008.md` MUST record status Accepted, decision polling for enrichment status in P1 with SignalR deferred, without inventing a polling interval, endpoint, or future implementation commitment.
- **FR-015**: No new NuGet package or `PackageReference`, no new project/layer/port, no schema change, no public API change, and no `LocalPlay`, secrets, or `Process.Start` change.
- **FR-016**: No polling implementation, SignalR hub/package, frontend work, additional instrument, or additional metric tag.

### Key Entities

- **Lookup attempt**: one `OmdbMetadataProvider` invocation from entry to exit; the unit counted by the outcome counter and timed by the duration histogram.
- **Outcome measurement**: one counter increment carrying `outcome` and, only for `failed`, the actual `Category.Code` as `failure_category`.
- **Persisted import**: one movie row surviving `SaveChangesAsync`; the unit counted by the import counter.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001 (AC1)**: Metrics and traces register with consistent naming and dimensions: the three ticket literals exist as the ruled instrument types, every exercised path carries exactly the ruled tag set, and the existing span/attribute assertions still pass.
- **SC-002 (AC2)**: `docs/adr/ADR-0008.md` exists with status Accepted and the P1 polling / deferred SignalR decision.
- **SC-003**: Found, NotFound, all four failure categories, lookup exceptions, retries, refused claims, requested cancellation, and import success/add/save/enqueue-failure paths are each covered by a test asserting the exact measurement or its absence.
- **SC-004**: No test asserts a constant equals itself; every literal assertion is independent of the constant under test.

## Assumptions

- Grill decisions in `brief.md` and `CONCLUSIONS.md` are frozen; this spec takes decisions only from them and the ticket text in recon-DEV-319:24-39.
- Facts come exclusively from recon-DEV-319.
- propertyTests opt-out holds: pure instrumentation of existing behavior introduces no new domain invariant; existing property tests still run.
- Phase A is documentation planning only; no build, test, analyzer, format, or mutation execution occurs before Gate 1.
