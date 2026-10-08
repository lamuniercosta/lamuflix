# Feature Specification: DEV-315 Worker Outcome Translation (ack / retry / DLQ)

**Feature Branch**: `feature/315-spec`

**Created**: 2026-10-08

**Status**: gate1: provisional

**Input**: Ticket DEV-315 *Scope & Technical Design*; `specs/DEV-315/brief.md` (grill closed 6/12); `recon-DEV-315`; `specs/PRODUCT.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Successful and refused outcomes are acknowledged (Priority: P1)

A message whose enrichment completes (claimed or not), or whose claim is refused, leaves the broker with no retry and no dead-letter entry, and the original is acknowledged.

**Why this priority**: Core settlement correctness; everything else builds on it.

**Independent Test**: Real-broker Testcontainers test per row: requested queue settled, no retry or DLQ ingress, distinguishing ready/in-flight state and lifecycle for ack evidence.

**Acceptance Scenarios**:

1. **Given** an Enriched outcome, **When** the consumer settles, **Then** the message is acknowledged and neither `enrichment.retry` nor `enrichment.dead-letter` receives it.
2. **Given** a NotFound outcome, **When** the consumer settles, **Then** the message is acknowledged and neither retry nor DLQ receives it.
3. **Given** a refused claim, **When** the consumer settles, **Then** the message is acknowledged and never enriched.

### User Story 2 - Retryable failures republish to the retry queue (Priority: P1)

A Failed outcome with a retryable category (transient or rate-limited) and Attempt below Max is published to `enrichment.retry` with Attempt+1 and intact telemetry, then acknowledged; after the TTL the message returns to the requested queue.

**Why this priority**: Bounded retry without poison loops is the ticket's central acceptance.

**Independent Test**: Real-broker Testcontainers test: retry ingress independently observed with same MovieId, Attempt+1, intact telemetry, no failure headers; actual broker TTL return to requested queue and provider reinvocation (provider recall alone is not TTL proof).

**Acceptance Scenarios**:

1. **Given** a transient failure with Attempt below Max, **When** the consumer settles, **Then** `enrichment.retry` receives the message with Attempt+1 and the original is acknowledged.
2. **Given** a rate-limited failure with Attempt below Max, **When** the consumer settles, **Then** the retry publication carries no failure headers and the message returns to requested after the TTL.
3. **Given** a message at Max-1, **When** it fails retryably, **Then** it retries once and then terminates (bounded poison handling).

### User Story 3 - Terminal failures reach the DLQ with closed-code headers (Priority: P1)

A Failed outcome that is non-retryable, or retryable at or above Max, is published to `enrichment.dead-letter` with the three settled failure headers and the unchanged failed Attempt, then acknowledged, with no further retry or provider loop.

**Why this priority**: Fatal-error routing with observable, PII-free headers is the ticket's other acceptance.

**Independent Test**: Real-broker Testcontainers test per row: DLQ entry with unchanged failed Attempt and all three headers; terminal behaviour with no further retry or provider loop.

**Acceptance Scenarios**:

1. **Given** a non-retryable failure below Max, **When** the consumer settles, **Then** the DLQ receives the message with headers `x-lamuflix-failure-category`, `x-lamuflix-failure-reason`, `x-lamuflix-failure-attempt` and no retry follows.
2. **Given** a retryable failure at or above Max, **When** the consumer settles, **Then** the DLQ receives the message with the same three headers and the provider is not invoked again.

### User Story 4 - Existing settlement paths are preserved (Priority: P2)

Malformed bodies, unexpected exceptions, cancellation, and failed republishes keep their existing settlement behaviour.

**Why this priority**: Regression guard; no behaviour change on these paths.

**Independent Test**: Existing Q1 regression proofs kept: malformed-body and unexpected-exception terminal `nack(requeue:false)`; stopping-token `nack(requeue:true)`; failed-publish behaviour.

**Acceptance Scenarios**:

1. **Given** a malformed body, **When** handling fails, **Then** the message is terminally nacked with `requeue:false`.
2. **Given** an unexpected exception, **When** handling fails, **Then** the message is terminally nacked with `requeue:false`.
3. **Given** a stopping token, **When** handling is cancelled, **Then** the message is nacked with `requeue:true`.
4. **Given** a mandatory/confirmed retry republish failure, **When** the publication fails, **Then** the original is never success-acked; it is terminally nacked with `requeue:false` via the existing broker DLQ route and the movie remains Pending (real-broker proof). **Given** a mandatory/confirmed DLQ republish failure, **When** the publication fails, **Then** as a shared catch-path expectation the original is never success-acked, is terminally nacked with `requeue:false`, and the already-recorded Failed movie state stays unchanged; it has no separate broker-injection proof under the fixed fixture and topology.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-01**: `Completed` outcomes (claimed true or false) and refused claims MUST acknowledge the original message with no retry or DLQ publication.
- **FR-02**: `Failed` outcomes with a retryable category and Attempt below Max MUST publish to `enrichment.retry` with core-provided `NextAttempt`, then acknowledge after successful publish (publish-before-ack).
- **FR-03**: `Failed` outcomes that are non-retryable, or at/above Max, MUST publish to `enrichment.dead-letter` with failure headers, then acknowledge after successful publish.
- **FR-04**: `ProcessEnrichmentOutcome.Failed` MUST carry the actual `EnrichmentFailureCategory` alongside the `Decision`; `ProcessEnrichmentCommandHandler` MUST supply it on both Failed returns. Core retains decision and `NextAttempt` responsibility.
- **FR-05**: The consumer MUST emit failure headers only for the dead-letter disposition and never recompute routing. Header contract (AMQP types):
  - `x-lamuflix-failure-category`: UTF-8 string, actual `Category.Code` (`provider_unavailable`, `rate_limited`, `invalid_response`, `unknown`).
  - `x-lamuflix-failure-reason`: UTF-8 string, `non_retryable` when not retryable, otherwise `max_attempts_exhausted` for terminal outcomes.
  - `x-lamuflix-failure-attempt`: int32, current request Attempt.
- **FR-06**: No raw error text, SafeDescription, provider payload, PII, or secrets in headers. Broker-owned `x-death` / `x-first-death-*` headers MUST NOT be written. Telemetry headers are preserved. Retry and out-of-table paths get no failure headers.
- **FR-07**: The existing `PublishAsync(EnrichmentRequested, string, CancellationToken)` signature MUST be retained; a concrete overload adding `IReadOnlyDictionary<string, object?> additionalHeaders` before `CancellationToken` MUST be added. `IEnrichmentQueue` is unchanged. Only terminal outcome publishes pass the three settled headers.
- **FR-08**: Each publish MUST build a fresh header dictionary and never mutate caller additions. The publisher owns telemetry keys and MUST reject collisions with telemetry, `x-death`, or `x-first-death-*` keys with `ArgumentException`, and reject null values with `ArgumentException`. Existing mandatory publishing and tracked confirmations are preserved.
- **FR-09**: Malformed-body and unexpected-exception terminal `nack(requeue:false)` and stopping-token `nack(requeue:true)` MUST be preserved outside the outcome table.
- **FR-10**: All routing proofs MUST run against a real broker via the existing Testcontainers `RabbitMqFixture` with production consumer/topology/publisher and core handler/policy. No new fixture, dependency, or production topology change. Broker state is isolated per test (independent queue/exchange names or purge). Small MaxAttempts via options; short positive integer-millisecond TTL via config. Synchronization uses bounded polling/cancellation/event handshakes; no sleep-only success evidence or exact-millisecond timing. Failed-publish proof is the existing retry-republish failure test; the DLQ-republish clause is a shared catch-path expectation with no separate broker-injection proof per the Patron F1/F2 proof amendment (`brief.md`, `CONCLUSIONS.md`); the awaited mandatory-publish client contract is ADR-0004:42.

### Key Entities

- `ProcessEnrichmentOutcome`: `Completed(bool Claimed)`; `Failed(EnrichmentFailureDecision Decision, EnrichmentFailureCategory Category)`.
- `EnrichmentFailureDecision`: action (`Retry`, `RetryDelayed`, `DeadLetter`) with `NextAttempt`.
- `EnrichmentFailureCategory`: `provider_unavailable`, `rate_limited`, `invalid_response`, `unknown`, each with retryability.
- Dead-letter headers: `x-lamuflix-failure-category`, `x-lamuflix-failure-reason`, `x-lamuflix-failure-attempt`.

## Success Criteria *(mandatory)*

- **SC-01**: No endless poison loops: a Max-1 retry crossing to Max terminates in the DLQ.
- **SC-02**: Transient and rate-limited failures below Max retry via the TTL queue and return to requested.
- **SC-03**: Fatal errors (non-retryable, or at/above Max) reach the DLQ with all three settled headers and unchanged Attempt.
- **SC-04**: Every header in the contract has a real-broker assertion; retry paths assert the absence of failure headers.
- **SC-05**: Existing unit policy/routing tests keep passing; integration proofs supplement them.

## Scope Boundaries

Out of scope: new translator or policy abstraction; new fixture, dependency, production topology, schema, HTTP/OpenAPI, frontend, or local-play change; `RecordEnrichmentFailureCommandHandler` and `IEnrichmentQueue` changes; retry policy changes. Anything else is a follow-up issue, not part of this spec.

## Assumptions & Dependencies

- Settled brief `specs/DEV-315/brief.md` (Q1-Q6) is authoritative; recon `recon-DEV-315` lines 100-137 supply construction-site and publisher-test/harness evidence.
- Existing `RabbitMqPublisherTests.cs` (`tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs`) is reused; no new publisher test file.
- `harness.yml` excluded from ticket commits (restored to `enabled: true`).
