# Feature Specification: Resilient enrichment messaging on RabbitMQ.Client 7

**Feature Branch**: `feature/018-spec`

**Created**: 2026-09-30

**Status**: gate1: provisional

**Input**: DEV-18 (parent DEV-283): "Upgrade to RabbitMQ.Client 7.x (async API), declare resilient queue topology (quorum, TTL retry, DLQ), inject OpenTelemetry trace headers, and write ADR-0004/0005."

Alignment: `brief.md` (closing bar AC1-AC8, decisions D1-D9b, frozen scope) and `CONCLUSIONS.md` (Q1-Q17). Input notes include `recon-DEV-18-6`. This spec restates those decisions as requirements and adds nothing to them. The ticket text (T01-T23) wins over this spec if they disagree.

## Clarifications

### Session 2026-09-30

The grill is closed: 12 of 12 questions were answered, and Q13 and Q14 were ruled on the two `needs decision:` requests raised while drafting. The ambiguities below were settled by those rulings, not by this spec.

- Q: Does DEV-18 implement the sweeper? → A: no. ADR-only, design recorded (Q1). Both ADRs say so.
- Q: Who processes a consumed message? → A: a new Core handler, `ProcessEnrichmentCommandHandler`. The consumer carries transport only (D5, Q13).
- Q: What happens when no metadata provider or no movie repository is registered? → A: the consumer and the processing handler are not registered. The Api starts, logs once that the consumer is inactive, and messages wait in `enrichment.requested` (D5, D6).
- Q: Where does a retryable failure go? → A: both `Retry` and `RetryDelayed` go to the retry queue, so a retry never arrives before the claim lease can expire (D6, Q14, which supersedes Q7's `Retry -> requested`).
- Q: Is the OMDb provider or the production repository part of this ticket? → A: no. Each is a follow-up that Rigger files or folds into existing coverage (D5, D6).
- Q: Does the readiness check come with health endpoints? → A: no. The check is registered, and the `/health/live` and `/health/ready` endpoints stay deferred to a follow-up (Q15).
- Q: Which directly used API surfaces get a direct package reference? → A: all of them (Q17, commit 5533797). Infrastructure references `RabbitMQ.Client` 7.2.2 (from 6.8.1), `OpenTelemetry.Api` 1.19.1, `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.12, `Microsoft.Extensions.Options` 10.0.12 and `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 versionlessly, each pinned centrally (D8a, D9a, D9b).
- Q: Which package carries the readiness check? → A: `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.12, centrally pinned and referenced by Infrastructure (Q16, D8a).

## User Scenarios & Testing *(mandatory)*

The actor is the enrichment pipeline: a request to enrich a movie is published, consumed, processed and either completes, is retried after a delay, or is set aside as unprocessable. The operator is the person who runs the Api and reads its logs, queues and traces.

### User Story 1 - A publish is durable or it fails loudly (Priority: P1)

A handler asks for a movie to be enriched. The request is accepted only once the broker has confirmed that it was stored and routed to the main queue. If the broker refuses it, cannot route it or cannot be reached, the caller gets an error.

**Why this priority**: three Core handlers already enqueue requests (`RequestEnrichmentCommandHandler`, `RequeueStrandedMoviesCommandHandler`, `ImportMovieFolderCommandHandler`), and the port has no implementation. Until one exists, nothing can be enriched. A publish that looks successful and is not would lose work silently.

**Independent Test**: against a real broker, publish one request and read it from the main queue. Then publish with routing broken and confirm the call fails.

**Acceptance Scenarios**:

1. **Given** a running broker and declared topology, **When** a request is enqueued, **Then** the call completes only after the broker confirms it, and the persistent message is in `enrichment.requested`.
2. **Given** a message the broker returns as unroutable, or nacks, **When** the caller awaits the enqueue, **Then** it throws. It never completes successfully.
3. **Given** the broker is unreachable, **When** a request is enqueued, **Then** the call throws, and a later call can succeed once the broker is back (the failed connection attempt does not stick).
4. **Given** two requests published at the same time, **When** both run, **Then** neither shares a channel with the other.

---

### User Story 2 - A failed attempt is retried after a delay, then set aside (Priority: P1)

The consumer hands each message to the Core processing handler. A retryable failure returns the message after a fixed delay, long enough for the movie's claim lease to expire, so the next attempt can claim the movie and call the provider again. A terminal failure, or one past the attempt limit, goes to a dead-letter queue and the movie is marked Failed.

**Why this priority**: this is the ticket's resilience requirement (T10, T11, T21). Without it a transient provider error either loses the movie or loops forever.

**Independent Test**: with a real broker, the real consumer and the real processing handler, and fakes only for the provider and repository ports, drive each outcome and read where the message lands.

**Acceptance Scenarios**:

1. **Given** a provider failure that Core classifies as retryable, for either the `Retry` or the `RetryDelayed` decision, **When** it is processed, **Then** a confirmed republish to the retry queue carries the next attempt number, the original is acked only after that confirm, and the message returns to the main queue after the delay.
2. **Given** a returned message and a claim lease shorter than the delay, **When** the delay expires, **Then** the movie is reclaimed and the provider is called again.
3. **Given** a non-retryable failure, or a retryable one at the attempt limit, **When** it is processed, **Then** the movie is marked Failed, and the message lands in `enrichment.dead-letter`.
4. **Given** a body that cannot be read as an enrichment request, **When** it arrives, **Then** it is rejected without requeue and lands in `enrichment.dead-letter` through the dead-letter exchange.
5. **Given** the republish fails or its outcome is uncertain, **When** the consumer handles it, **Then** the original is never acked as a success.
6. **Given** the host is stopping, **When** a message is in flight, **Then** it is requeued, and the movie is not marked Failed.
7. **Given** the claim is refused (another attempt holds the lease), **When** the message is handled, **Then** it is acked, and logged and reported as skipped, never as a successful enrichment.

---

### User Story 3 - A request can be followed across the queue (Priority: P2)

An operator follows one enrichment from the publish to its processing in a single trace, including after a retry.

**Why this priority**: the ticket requires W3C trace context in the message headers (T15, T22), and the constitution requires extraction on the consumer side.

**Independent Test**: publish with no trace listener and read the `traceparent` header. Then register a listener and confirm the consumer's activity shares the producer's trace ID.

**Acceptance Scenarios**:

1. **Given** no tracing listener is registered, **When** a request is published, **Then** its headers still hold a valid `traceparent` (the ambient context if there is one, otherwise a new one), and `tracestate` only when one exists.
2. **Given** a listener, **When** the message is consumed, **Then** the consumer's activity is parented to the producer's context and shares its trace ID.
3. **Given** a redelivered message, **When** it is consumed, **Then** the new activity links to the earlier context.

---

### User Story 4 - Setup is safe to start, and idle until it can work (Priority: P2)

The Api always starts. It declares the topology and can publish. It consumes only when both a metadata provider and a movie repository are registered, and only when the timing options are consistent.

**Why this priority**: neither port has a production implementation yet (follow-ups in D5 and D6). Starting a consumer without them would fail on the first message, and a retry delay shorter than the lease would lose retries.

**Independent Test**: build the host with each port present and absent, and with valid and invalid timing options, and read the registrations, the startup log and the startup result.

**Acceptance Scenarios**:

1. **Given** either port is not registered, **When** the Api starts, **Then** it starts, logs once that the consumer is inactive, registers neither the consumer nor the processing handler, and messages wait in `enrichment.requested`.
2. **Given** both ports are registered and the options are valid, **When** the Api starts, **Then** the consumer runs.
3. **Given** the consumer is active and the claim lease is unset, zero or negative, **When** the Api starts, **Then** startup fails and names the option.
4. **Given** the consumer is active and the retry TTL in whole milliseconds is not strictly greater than the claim lease, **When** the Api starts, **Then** startup fails. A rounding step never erases the margin.
5. **Given** the consumer is inactive, **When** the Api starts, **Then** no lease is required.
6. **Given** a reachable broker, **When** the RabbitMQ readiness check runs, **Then** it reports Healthy; **Given** an unreachable broker, **Then** it reports Unhealthy and its description carries no credentials or raw exception text.

---

### User Story 5 - The design is written down (Priority: P2)

A maintainer reads two decision records that explain the topology, the retry and dead-letter design, the sweeper, and the dual-write risk.

**Why this priority**: the ticket requires both records (T16-T18, T23).

**Independent Test**: read both files and check the required statements.

**Acceptance Scenarios**:

1. **Given** `docs/adr/ADR-0004.md`, **When** read, **Then** it covers the topology, retry queue, DLQ and sweeper design, the client-7 async model, confirms, trace propagation, the cutover with no migration of old queues, the activation boundary, and the limitation that crash redelivery or competing work inside a lease may be refused and so depends on the deferred sweeper.
2. **Given** `docs/adr/ADR-0005.md`, **When** read, **Then** it covers dual-write mitigation: the sweeper now, a transactional outbox as a stretch.
3. **Given** either record, **When** read, **Then** it states that DEV-18 records the sweeper design and does not implement it, and its status is Accepted.

---

### Edge Cases

- A republish is confirmed by the broker but the process dies before the ack: the original is redelivered, so a duplicate is possible. The claim and idempotency behavior in Core absorbs it. Exactly-once delivery is not claimed.
- Crash redelivery, graceful shutdown, or competing work inside a live lease: the claim is refused and the message is acked as skipped. Recovery then depends on the deferred sweeper (stated in ADR-0004).
- A failed republish or an unexpected exception: the original is dead-lettered (`BasicNack(requeue: false)`) and the movie is left `Pending`. Dead-lettering on delivery-limit exhaustion also leaves the movie `Pending`. All of these are recovered only by the deferred sweeper, as ADR-0004's Limitation bullet states.
- `RetryDelay` so large it cannot become an integer number of milliseconds: startup fails, whether or not the consumer is active, because the topology always declares the TTL.
- The broker holds messages in the old `task_queue` and `task_queue_dlq`: they are not migrated or purged (Q9).
- A message with a valid `traceparent` but no `tracestate`: only `traceparent` is sent. A `tracestate` is never made up.
- The provider is registered but misconfigured: its own failure surfaces, and no stub hides it.
- Cancellation during a lookup: it is never classified as a provider failure.

## Requirements *(mandatory)*

### Functional Requirements

**Publisher (AC1; T13, T14, T21; Q10)**

- **FR-001**: The system MUST provide an implementation of the enrichment queue port in Infrastructure that publishes persistent, mandatory messages to the `lamuflix.enrichment` exchange with routing key `requested`.
- **FR-002**: Each publish MUST use its own channel with publisher confirmations and confirmation tracking enabled, and the channel MUST be disposed after the publish.
- **FR-003**: A nack, a returned (unroutable) message or a connection failure MUST surface as an exception from the enqueue call.
- **FR-004**: No channel MUST be shared between concurrent publishes.

**Topology (AC2; T07-T11; Q7, Q8)**

- **FR-005**: A single topology owner MUST declare, idempotently, the durable direct exchange `lamuflix.enrichment` and three durable quorum queues. Both the publisher (on first use) and the consumer (at startup) MUST await it.
- **FR-006**: `enrichment.requested` MUST bind to `requested`, dead-letter to the same exchange with key `dead-letter`, use a delivery limit equal to `EnrichmentOptions.MaxAttempts`, and use at-least-once dead-lettering with reject-publish overflow.
- **FR-007**: `enrichment.retry` MUST bind to `retry`, expire messages after `RabbitMqOptions.RetryDelay` (in whole milliseconds), dead-letter to the same exchange with key `requested`, and use at-least-once dead-lettering with reject-publish overflow.
- **FR-008**: `enrichment.dead-letter` MUST bind to `dead-letter` and have no automatic retry.

**Consumer (AC3; T06, T09, T10, T11, T21; Q6, Q7, Q14)**

- **FR-009**: The consumer MUST use the asynchronous consumer over a channel, with prefetch taken from `RabbitMqOptions.Prefetch`.
- **FR-010**: The consumer MUST create a scope for each message and dispatch `ProcessEnrichmentCommand` through the composed command handler (execution order Tracing, Logging, Validation, then the handler).
- **FR-011**: A `Completed` outcome MUST be acked. A claim that was refused MUST be acked, and logged and reported as skipped, never as a success.
- **FR-012**: A `Retry` or `RetryDelayed` decision MUST be republished persistently to the `retry` key with the decision's `NextAttempt`. A `DeadLetter` decision MUST be republished persistently to the `dead-letter` key. The original MUST be acked only after the republish is confirmed and routed.
- **FR-013**: The consumer MUST NOT hold any second attempt-limit check. `EnrichmentRequested.Attempt` is the counter on the wire, and Core's counting convention is kept.
- **FR-014**: A message that cannot be deserialized MUST be rejected with `requeue: false` so that it reaches the dead-letter queue.
- **FR-015**: On cancellation the message MUST be requeued (`requeue: true`). After a failed or uncertain republish, and after any exception other than cancellation (including a validation failure), the consumer MUST `BasicNack(requeue: false)`, so the original reaches `enrichment.dead-letter` through the dead-letter exchange, and MUST NOT mark the movie Failed. None of these cases may be acked as a success (D9 Risk F1).
- **FR-035**: `ProcessEnrichmentCommand` MUST be validated (FluentValidation, beside the command): `MovieId` is not the default value, and `Attempt` is at or above the existing enqueue floor of 1. A validation failure is an exception and takes the FR-015 dead-letter path (D9 Risk F4).

**Core processing (D5, Q13)**

- **FR-017**: Core MUST gain a sealed `ProcessEnrichmentCommand(MovieId, Attempt)` and `ProcessEnrichmentCommandHandler` under `Features/Enrichment`, returning a `ProcessEnrichmentOutcome` that is `Completed` (including a refused claim, flagged as skipped) or the existing `EnrichmentFailureDecision`.
- **FR-018**: The handler MUST claim before lookup, load the current movie, call `IMetadataProvider.FindAsync`, then apply the result. A provider `Failed(category)` uses its own category; only an exception is classified. The retry-or-terminal rule is one shared Core rule, used by this handler and the existing failure handler. It composes the existing logic through ports and MUST NOT call another handler.
- **FR-019**: The handler MUST use the existing `EnrichmentFailureClassifier` as the only classifier, add no retry policy of its own, and MUST NOT classify a cancellation as a failure.
- **FR-020**: The lookup MUST take its year from the movie's metadata, which is unset before the first enrichment.
- **FR-016**: A movie MUST be marked Failed, and saved, only on the terminal path. The retry path writes nothing, because the claim already stamped the attempt.

**Activation and options (AC5, AC6; Q6, Q9, Q10, Q13, Q14)**

- **FR-021**: The connection owner, topology and publisher MUST always be registered. The consumer and the processing handler MUST be registered only when both `IMetadataProvider` and `IMovieRepository` registrations exist, checked after both are registered.
- **FR-022**: When the consumer is inactive, the Api MUST start, log once that it is inactive, and neither consume nor ack any message. No stub provider or repository is installed, and scope and build validation stay on.
- **FR-023**: `RabbitMqOptions` MUST gain `RetryDelay` (default 30 s, positive, convertible to an integer millisecond TTL) and `Prefetch` (default 1, positive). `MaxAttempts` stays in `EnrichmentOptions`.
- **FR-024**: Both options MUST be bound and validated at startup through the existing options registration. Nothing reads raw configuration, no host or secret value is hardcoded as a fallback, and the Api settings gain a `RabbitMq` section without secrets.
- **FR-025**: When the consumer is active, startup validation MUST require an explicitly set, positive `EnrichmentOptions.ClaimLease` and a converted integer-millisecond retry TTL strictly greater than it. No default is invented and neither value is adjusted. An inactive host needs no lease.
- **FR-026**: There MUST be one process-owned, asynchronously initialized connection. A failed initialization MUST surface and MUST NOT stick. Shutdown and disposal belong to the owner, and cancellation is propagated.
- **FR-036**: `RabbitMqOptions` MUST NOT print `Password` in its generated `ToString` or `PrintMembers` (it shows `***`), and nothing logs `RabbitMqOptions` as a whole (D9 Risk F3).

**Tracing (AC4; T15, T22; Q3, Q4)**

- **FR-027**: The API's W3C trace-context propagator MUST be set as the default propagator before first use.
- **FR-028**: The publisher MUST start a producer activity from the `LamuFlix` source and inject `traceparent` (and `tracestate` only when present) into the message headers. When no listener makes the activity null, it MUST still inject a valid context: the ambient one, else a new one.
- **FR-029**: The consumer MUST extract the context, start a consumer activity parented to it, and add a link to the earlier context on a redelivery. Telemetry names MUST come from `TelemetryConstants`.

**Packages and boundaries (D4)**

- **FR-030**: Central pins MUST be `RabbitMQ.Client` 7.2.2 (from 6.8.1), `OpenTelemetry.Api` 1.19.1, `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.12, `Microsoft.Extensions.Options` 10.0.12 and `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 (D8a, D9a, D9b). Infrastructure MUST reference all five versionlessly. `OpenTelemetry.Api` is the only new telemetry package. Only Infrastructure references `OpenTelemetry.Api`. Core MUST reference neither RabbitMQ nor OpenTelemetry.

**Decision records (AC7; T16-T18, T23)**

- **FR-031**: `docs/adr/ADR-0004.md` and `docs/adr/ADR-0005.md` MUST be written with status Accepted, covering the content in User Story 5. Both MUST state that DEV-18 records the sweeper design and does not implement it.

**Gates (AC8; Q12)**

- **FR-032**: Every configured gate MUST pass or be reported as SKIP. A gate that was skipped or could not run is never reported as PASS.

**Readiness (D8, D8a; Q15, Q16)**

- **FR-033**: The RabbitMQ adapter MUST register an `IHealthCheck`, `RabbitMqHealthCheck`, tagged `ready`, over the shared connection owner, whenever the owner and publisher are registered, including when the consumer is inactive. It MUST report Unhealthy on a broker connection failure, never publish or consume, and never put credentials or raw exception text in the result (constitution VI:232-234, IV).
- **FR-034**: `ServiceDefaults` `AddServiceDefaults` MUST call `AddHealthChecks()` once, and the Api MUST NOT duplicate it.

### Key Entities

- **Enrichment request**: a movie ID and an attempt number. It is the message body and the counter on the wire.
- **Enrichment outcome**: what processing returned: completed, skipped (claim refused), or a failure decision with an action (retry, delayed retry, dead letter) and the next attempt.
- **Exchange and queues**: `lamuflix.enrichment`, with `enrichment.requested`, `enrichment.retry` and `enrichment.dead-letter`.
- **Claim lease**: how long a claim on a movie blocks another attempt. The retry delay must exceed it.
- **Trace context**: the `traceparent` and optional `tracestate` carried in message headers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Against a real broker, a confirmed publish is readable from `enrichment.requested`, and an unroutable or refused publish fails 100% of the time.
- **SC-002**: A retryable failure returns to the main queue after the configured delay, the movie is reclaimed, and the provider is called a second time, for both retry actions.
- **SC-003**: A terminal failure and a malformed body each land in `enrichment.dead-letter`, and no movie that can still retry is marked Failed.
- **SC-004**: No message is acked as a success after a failed, uncertain or cancelled step.
- **SC-005**: Headers carry a valid `traceparent` with no listener registered, and the consumer's trace ID equals the producer's with a listener.
- **SC-006**: With either port missing, the Api starts and the queue keeps every message. With an invalid lease or delay and both ports present, startup fails.
- **SC-007**: Core references neither RabbitMQ nor OpenTelemetry, and the architecture tests pass.
- **SC-008**: The build, analyzers, complexity (at most 15, then at most 6), inspections, format check, full test suite, vulnerable-package scan and mutation (at least 80%) gates each report PASS, or SKIP where they could not run.
- **SC-009**: Both decision records exist with status Accepted and state that the sweeper is not implemented.

## Assumptions

- The broker is the development broker on `rabbitmq:4.0.0`. There is no in-flight message migration (Q9, `[assumed]`).
- The defaults of 30 s for `RetryDelay` and 1 for `Prefetch` are taste defaults (`ASSUMPTIONS.md`).
- The routing keys `requested`, `retry` and `dead-letter` are Keel's wording, accepted by Patron (`ASSUMPTIONS.md`).
- `IMetadataProvider` and `IMovieRepository` have no production implementation in the built solution. Their production wiring (DEV-303 for the provider; the repository follow-up that Rigger files or folds in) is outside DEV-18.
- The claim-handoff question (DEV-299 Q1) and DEV-316 stay open. This spec neither answers nor preempts them.
- **Out of scope**: implementing the sweeper; a transactional outbox; deleting or editing the retired `Web`, `Worker`, `Data` and `tests/LamuFlix.Test`; purging old queues; exponential backoff; an OpenTelemetry SDK, exporter or instrumentation; a docker-compose file; schema changes; any new project or layer; mapping `/health/live` or `/health/ready`, or a Postgres health check (Q15 follow-up).
- **Health endpoints not mapped**: the `/health/live` and `/health/ready` endpoints are not mapped. That is a pre-existing gap, and a separate follow-up (Q15) owns it. The readiness check is registered but NOT reachable over HTTP, and constitution VI endpoint alignment is NOT claimed.
