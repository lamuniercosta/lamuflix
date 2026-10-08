# Feature Specification: DEV-314 — Infrastructure consumer delivery proof

**Feature Branch**: `feature/314-spec`

**Created**: 2026-10-07

**Status**: gate1: provisional

**Input**: Corrected DEV-314 ticket (Scope 1-3, AC1-2 per Ruling 1, YouTrack receipt DEV-314 note:29-33), `brief.md`, `CONCLUSIONS.md` (Q1-Q12), recon-DEV-314, `specs/PRODUCT.md`, ADR 0019.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Async consumer with configured prefetch (Priority: P1)

The enrichment consumer processes `enrichment.requested` quorum-queue messages asynchronously without blocking the worker thread, with channel prefetch taken from `RabbitMqOptions.Prefetch`.

**Why this priority**: Scope 1 and AC1; without async non-blocking consumption nothing else is observable.

**Independent Test**: `Consumer_APrefetchOfOne_LeavesTheSecondMessageReady` — block the first handler at a synchronization barrier and observe via the broker that the second message stays ready; this named real-broker test proves prefetch only. Async non-blocking dispatch (S1-c) is a mandatory structural obligation verified by source-structure review: `ReceivedAsync` dispatch to `HandleAsync` (EnrichmentConsumer.cs:50 to :119) with transitive awaits (DispatchAsync :137, handler `HandleAsync` await :167, settlement awaits :130/:138/:146/:156) and no synchronous waits introduced. S1-a is likewise structural: `AsyncEventingBasicConsumer` (:49), `RabbitMqTopology.RequestedQueue` with `autoAck: false` (:51), prefetch `BasicQosAsync` from `options.Value.Prefetch` (:47). The second-ready assertion (mapped L177) cannot prove non-blocking dispatch, and there is no async assertion or negative-control PASS. AC1 behavior is unchanged.

**Acceptance Scenarios**:

1. **Given** two queued enrichment messages and prefetch 1, **When** the first handler blocks at a barrier, **Then** the broker still reports the second message ready.
2. **Given** a running consumer, **When** a message is delivered, **Then** `ReceivedAsync` dispatches via `HandleAsync` without blocking the worker thread (mandatory structural review, EnrichmentConsumer.cs:50 to :119 with transitive awaits; verified by review, not asserted).

---

### User Story 2 — Single distributed trace, initial and redelivered (Priority: P1)

Every consumer `Activity` is parented to the producer context extracted from `traceparent`/`tracestate`, preserving TraceId, ParentSpanId and TraceStateString; broker redelivery adds exactly one `ActivityLink` to that original context, and `messaging.rabbitmq.delivery_count` is 0 on initial delivery and 1 on redelivery.

**Why this priority**: Scope 2 and AC2; the single API-enqueue-to-consumer trace is the ticket's observable contract.

**Independent Test**: Strengthened `Consumer_TheConsumerActivity_SharesTheProducerTraceId` (initial: kind, parent identity, tracestate, zero links, count 0) plus a new real-broker redelivery test (redelivered: exact parent/tracestate, one original-context link, count 1, completion/settlement asserted separately).

**Acceptance Scenarios**:

1. **Given** a message carrying producer traceparent/tracestate, **When** first delivered, **Then** the consumer activity has `ActivityKind.Consumer`, parent equal to the extracted producer context, zero links, and delivery_count 0.
2. **Given** the same message genuinely redelivered by RabbitMQ (unacked interruption, not a republished retry), **When** reprocessed, **Then** the consumer activity keeps the producer parent and carries exactly one link whose context equals the extracted original context, with delivery_count 1.

---

### User Story 3 — Per-message DI scope with disposal (Priority: P2)

Each message is dispatched inside its own `AsyncServiceScope`, resolving the feature handler from the scope; handler instances are distinct per message and disposed asynchronously after each invocation, with no root-scoped reuse.

**Why this priority**: Scope 3; lifecycle isolation between messages.

**Independent Test**: New two-message integration test with a small scoped test handler inside `EnrichmentConsumerTests.cs` — distinct instance identities, async disposal observed after each invocation, no root-scoped handler reuse.

**Acceptance Scenarios**:

1. **Given** two delivered messages, **When** both are processed, **Then** each dispatch resolved a distinct scoped handler instance.
2. **Given** a completed dispatch, **When** the invocation returns, **Then** the scope was disposed asynchronously.

---

### Edge Cases

- Malformed body dead-letters without starting handler work (`Consumer_AMalformedBody_ReachesTheDeadLetterQueue`).
- Host stopping mid-dispatch requeues via `BasicNackAsync(requeue: true)` without marking the movie failed.
- Dropped connection resumes consumption after reconnect; broker unreachable at boot starts and stops without throwing.
- `tracestate` absent from headers: parent still extracted from `traceparent`; carrier roundtrip extended only where no named test pins it.
- Empty production diff is acceptable only when every behaviorally observable obligation above has deliberate named-test evidence that fails against a broken implementation, plus explicit structural review receipts for S1-a/S1-c.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Consumer MUST be `EnrichmentConsumer : BackgroundService` in `src/LamuFlix.Infrastructure/RabbitMq/`, consuming via `AsyncEventingBasicConsumer` on the `enrichment.requested` quorum queue with `autoAck: false`.
- **FR-002**: Channel prefetch MUST be configured from `RabbitMqOptions.Prefetch` via `BasicQosAsync`.
- **FR-003**: Consumer MUST extract `traceparent`/`tracestate` from `BasicProperties.Headers` via `TraceContextCarrier` and start an `ActivityKind.Consumer` activity parented to that extracted context on every delivery.
- **FR-004**: On broker redelivery the activity MUST carry exactly one `ActivityLink` to the extracted original context; on initial delivery zero links. `messaging.rabbitmq.delivery_count` MUST be 0 initial / 1 redelivered.
- **FR-005**: Dispatch MUST create one `AsyncServiceScope` per message and resolve `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>` from that scope.
- **FR-006**: All 12 existing `EnrichmentConsumerTests` MUST be preserved and credited only for assertions they actually pin; new/strengthened tests MUST cover the redelivery link/count gap and the two-message scope/disposal proof.
- **FR-007**: Redelivery proof MUST use real broker redelivery in the existing `RabbitMqCollection` with synchronized unacked interruption; bounded signal waits only, no arbitrary sleeps; per-test disposed `ActivityListener` filtered to the test correlation.
- **FR-008**: Source edits to `EnrichmentConsumer.cs` / `TraceContextCarrier.cs` are conditional on a failing in-scope obligation test that cites the defect; carrier test extension only where tracestate roundtrip is unpinned.
- **FR-009**: Negative controls MUST be temporary and local: break each behaviorally observable acceptance obligation, demonstrate the assertion failure, restore, record evidence; never commit broken production behavior. Prefetch, trace/redelivery/settlement and scope/disposal controls stay mandatory. S1-a (consumer type, queue, autoAck) and S1-c (async dispatch) require explicit structural review receipts instead (EnrichmentConsumer.cs:47/:49/:50/:51/:119/:167 with transitive awaits, no synchronous waits); S1-c has no behavioral negative-control surface and MUST never be reported as assertion-proven or as an async negative-control PASS.

Out of scope: Worker resurrection/deletion, broker/topology redesign, retry/claim-count redesign (no tightening of DEV-390 assertions), new dependencies/projects/layers, schema, public API, LocalPlay/secrets/`Process.Start`, glossary changes.

### Key Entities

- **EnrichmentRequested**: wire message carrying only `MovieId` and `Attempt`.
- **Producer context**: `ActivityContext` extracted from headers; parent of every consumer activity, link target on redelivery.
- **Consumer activity**: `Enrichment.Process` span with `lamuflix.movie.id` and `messaging.rabbitmq.delivery_count` tags.
- **DI scope**: one `AsyncServiceScope` per delivery owning the resolved handler's lifetime.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every Scope 1-3 obligation and both acceptance criteria MUST map to evidence: explicit structural review receipts for S1-a/S1-c (US1 Independent Test), and named passing tests with recorded break/failure/restore negative controls for every behaviorally observable obligation. S1-c has no behavioral negative-control surface and MUST NOT be reported as assertion-proven or as an async negative-control PASS.
- **SC-002**: Initial-delivery assertions pin kind Consumer, TraceId equal to the LamuFlix producer TraceId, ParentSpanId equal to the SpanId of the extracted wire context (the RabbitMQ.Client.Publisher client-publish span whose ParentSpanId equals the LamuFlix producer span id), TraceStateString equal to the propagated tracestate, zero links and count 0; redelivery assertions pin the same wire parent plus exactly one link whose context equals the extracted original wire context and count 1.
- **SC-003**: Two-message test proves distinct scoped handler instances with async disposal after each invocation.
- **SC-004**: Phase B delivery passes the standing pipeline on diff/head with no lowered threshold and no unanswered structural checkbox before the delivery merge bar.

## Assumptions

- Existing RabbitMQ.Client 7, OpenTelemetry, xUnit v3, Shouldly, NSubstitute, FsCheck and Testcontainers packages suffice; no new dependency (Q7).
- Existing `RabbitMqCollection` fixtures, composition/publisher/topology tests are reusable read/run surfaces, not rewrite targets (Q6).
- Precise redelivery fixture mechanics (barrier placement, interruption via cancellation vs channel closure) are implementation choices within the Q4 contract.
- Thresholds come from harness configuration with the stricter refactor-stage gate; no fixed number is stated here (Q9).
