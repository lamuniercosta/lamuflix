# DEV-18 — Patron grill conclusions

Date: 2026-09-30. Branch: feature/018-spec. Phase A decisions only.
Question source: Keel's twelve-question dispatch and supplied recommendations.
Authority: live DEV-18 fetched with scripts/get-task.ps1; PRODUCT.md; constitution 1.2.0; current Patron seat assignment.
The connected lamuflix-team-charter:9-17,28 has stale escalation language; PRODUCT.md:34-40 and the current seat assignment govern. Care-list decisions belong to Patron; only ticket changes and necessary constitution departures become owner checkboxes.
No implementation, Gate 1 closure, or provisional marker is authorized by this grill. Q5's caller recon remains unverified: recon-DEV-18-2 contained only its heading on this read.

## Ticket citation key (live description, numbered lines)

    T01 ### Overview
    T02 Upgrade to RabbitMQ.Client 7.x (async API), declare resilient queue topology (quorum, TTL retry, DLQ), inject OpenTelemetry trace headers, and write ADR-0004/0005.
    T03
    T04 ### Scope & Technical Design
    T05 1. RabbitMQ.Client 7 Migration:
    T06    - Upgrade from 6.8 to 7.x. Adopt IChannel and asynchronous event consumer APIs.
    T07 2. Topology (RabbitMqTopology):
    T08    - Exchange: lamuflix.enrichment (direct, durable).
    T09    - Main Queue: enrichment.requested (quorum queue, prefetch configured from options).
    T10    - Retry Queue: enrichment.retry (x-message-ttl set for delay, dead-letters back to lamuflix.enrichment).
    T11    - DLQ: enrichment.dead-letter (poison message destination).
    T12 3. Publisher with Publisher Confirms & Tracing:
    T13    - Implement RabbitMqEnrichmentQueuePublisher implementing IEnrichmentQueue.
    T14    - Enable publisher confirms.
    T15    - Inject traceparent and tracestate into message BasicProperties.Headers via OpenTelemetry DefaultTextMapPropagator.
    T16 4. ADRs:
    T17    - Write docs/adr/ADR-0004.md: RabbitMQ topology with retry queue, DLQ, and sweeper.
    T18    - Write docs/adr/ADR-0005.md: Dual-write mitigation: sweeper now, transactional outbox as stretch.
    T19
    T20 ### Acceptance Criteria
    T21 - Integration test with Testcontainers RabbitMQ validates publish confirms, TTL retry routing, and DLQ.
    T22 - Message headers contain W3C traceparent context.
    T23 - ADR-0004 and ADR-0005 accepted.

---

## Q1 — Sweeper scope

Question/recommendation: Ticket ADR-0004 says "retry queue, DLQ, and sweeper"; ADR-0005 "sweeper now, transactional outbox as stretch". Scope items 1-3 do not list a sweeper implementation. Is implementing a sweeper in DEV-18's delivery, or is it ADR-only (design recorded) with a follow-up ticket? REC: ADR-only + follow-up ticket via Rigger; implementing it would add to what the ticket delivers (§2.3a).

Ruling: ACCEPT ADR-only sweeper design in DEV-18; Rigger must find existing implementation coverage or record a deduplicated follow-up, without adding it to the chain.
- Basis: DEV-18 T05-T18 names executable migration/topology/publisher work and places the sweeper under ADR deliverables; PRODUCT.md:23-24 makes that scope authoritative.
- Constitution:187-188,454-455 still requires the eventual sweeper. Both ADRs must state honestly that DEV-18 records the mitigation design and does not deliver its implementation; this is ticket sequencing, not a waiver of that requirement.
- Adding sweeper implementation to DEV-18 would require the §2.3(a) owner checkbox. The selected ADR-only delivery does not require that change; do not invent an owner checkbox for it.

---

## Q2 — ADR titles and acceptance

Question/recommendation: Conductor's dispatch called ADR-0005 "RabbitMQ.Client 7.x Async Model & Trace Propagation"; the ticket says ADR-0005 = "Dual-write mitigation: sweeper now, transactional outbox as stretch". REC: ticket text governs; client-7 async model + propagation rationale lives in ADR-0004 (Context/Consequences) and plan research, no third ADR. ADRs land with Status "Accepted" in the PR (the user's merge = acceptance).

Ruling: ACCEPT the ticket's ADR-0004/0005 titles, place async-client/propagation rationale in ADR-0004 and plan research, and use Status: Accepted for the merged artifacts; an open PR is still awaiting owner acceptance.
- Basis: DEV-18 T17-T18,T23; PRODUCT.md:23-24 and :20; existing docs/adr/ADR-0006.md:3 and ADR-0010.md:3 use Accepted.
- No third ADR and no claim that Patron's draft status closes Gate 1 or preempts the user's merge.

---

## Q3 — OpenTelemetry dependency

Question/recommendation: Ticket names OpenTelemetry DefaultTextMapPropagator (OpenTelemetry.Api package, Propagators.DefaultTextMapPropagator). That is a new NuGet package, but ticket-named. REC: add OpenTelemetry.Api only (no SDK, no exporter, no instrumentation packages), central-pinned in Directory.Packages.props, referenced only by the project hosting the RabbitMQ adapters. Reject the BCL DistributedContextPropagator alternative (contradicts ticket text).

Ruling: ACCEPT OpenTelemetry.Api as the sole new telemetry package, centrally pinned and directly referenced by Infrastructure's messaging adapters; explicitly initialize W3C DefaultTextMapPropagator before use without adding the SDK.
- Basis: DEV-18 T13-T15; PRODUCT.md:12,23-24; constitution:99,105-111,440-441. BCL substitution does not satisfy T15.
- [OpenTelemetry propagation documentation](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/customizing-the-sdk/README.md): the API's default propagator is no-op until configured. Use the API's TraceContextPropagator as the configured default; injecting through an unconfigured default would fail T22.
- Existing ServiceDefaults wiring remains the host-wide telemetry owner (constitution:235-236); messaging registration may initialize the API propagator without duplicating an SDK/exporter setup. This ruling does not authorize a second telemetry stack or a package in Core.

---

## Q4 — Consumer extraction

Question/recommendation: AC only requires traceparent in published headers. Should the Worker also extract traceparent/tracestate and start an ActivityKind.Consumer activity parented to it? REC: yes — ticket summary says "trace propagation" and the consumer is rewritten anyway for item 1; publisher starts an ActivityKind.Producer activity from the existing "LamuFlix" ActivitySource so a context exists to inject.

Ruling: ACCEPT producer/consumer activities and extracted remote parent context, adding the constitution-required ActivityLink on redelivery and valid W3C propagation even when StartActivity returns null.
- Basis: DEV-18 T02,T06,T15,T22; constitution:220-229 explicitly requires extraction, consumer activity, redelivery links, the LamuFlix source and centralized telemetry names.
- StartActivity alone does not guarantee a context in the absence of an SDK/listener: preserve a valid ambient context or establish a W3C context for injection; a listener in tests must not conceal the API-only ordinary path.
- Traceparent is required for a valid context; tracestate is emitted when present, not fabricated. [API TraceContextPropagator](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Api/Context/Propagation/TraceContextPropagator.cs).

---

## Q5 — Publisher placement and retired types

Question/recommendation: Publisher placement (pending recon-DEV-18-2 R1/R2). Ticket: "RabbitMqEnrichmentQueuePublisher implementing IEnrichmentQueue" (Core port). Today it lives in LamuFlix.Web implementing Web's IEnrichmentQueuePublisher(MovieEnrichmentMessage). REC: publisher moves to LamuFlix.Infrastructure implementing IEnrichmentQueue(EnrichmentRequested); Web's IEnrichmentQueuePublisher and MovieEnrichmentMessage are retired and callers switched to IEnrichmentQueue (§2.3 item 6 — rule on deleting files the ticket does not name). Conditional on recon showing no other consumer of those types.

Ruling: ACCEPT Infrastructure placement and migration to IEnrichmentQueue/EnrichmentRequested; delete the legacy publisher interface/message only after R1/R2 proves all production and test consumers are migrated.
- Basis: DEV-18 T13; constitution:99,105-111,445-451; recon-DEV-18:23-32 confirms the Core port/message and current Web adapter/interface.
- This is a deliberate care-list file-scope ruling under PRODUCT.md:34-48 and the current seat's dead-code rule; ticket-forced adapter/caller rewrites are in scope.
- Conditional evidence remains open, not owner-blocked: recon-DEV-18-2 has no R1/R2 result yet. Unexpected consumers require recon/adjudication before deletion; do not report that deletion is cleared.

---

## Q6 — Retry values and ownership

Question/recommendation: Today RetryCount < 3 then DLQ. REC: RabbitMqOptions gains MaxAttempts (default 3), RetryDelay (TimeSpan, default 30s → retry queue x-message-ttl), Prefetch (ushort, default 1, preserving today's BasicQos 1); all validated at startup (ValidateOnStart). Attempt counter travels in EnrichmentRequested.Attempt (already exists). Fixed delay, no exponential backoff (a single queue-level TTL cannot express backoff).

Ruling: CHANGE: keep the single MaxAttempts policy in existing EnrichmentOptions (default 3); add RabbitMqOptions.RetryDelay (30s) and Prefetch (1), validate them at startup, and retain the existing Core handler's Attempt < MaxAttempts decision.
- Basis: constitution:185-186,246-249,445-453; src/LamuFlix.Core/Options/EnrichmentOptions.cs:14 and RecordEnrichmentFailureCommandHandler.cs:23-44 already own the category/attempt policy.
- EnrichmentRequested.Attempt is the wire counter; preserve the Core policy's counting convention rather than introducing a competing Attempt+1 < MaxAttempts test in the adapter. Lease/sweep settings remain in EnrichmentOptions.
- Fixed queue TTL is accepted for RetryDelayed. Require positive prefetch and a positive RetryDelay convertible to RabbitMQ TTL milliseconds; reuse ServiceDefaults validation registration. Defaults are recorded as [assumed].

---

## Q7 — Retry and DLQ routing

Question/recommendation: Exchange lamuflix.enrichment (direct, durable) with routing keys requested→enrichment.requested, retry→enrichment.retry, dead-letter→enrichment.dead-letter. enrichment.retry: x-message-ttl=RetryDelay, x-dead-letter-exchange=lamuflix.enrichment, x-dead-letter-routing-key=requested. enrichment.requested: x-dead-letter-exchange=lamuflix.enrichment, x-dead-letter-routing-key=dead-letter, so a nack(requeue:false) of an undeserialisable/poison message lands in the DLQ. Handler failure: Attempt+1 < MaxAttempts → publish to retry (confirmed) then ack; else publish to dead-letter then ack; movie marked Failed as today.

Ruling: CHANGE: accept the proposed bindings/DLX/TTL but translate existing Core outcomes: Retry→requested, RetryDelayed→retry, DeadLetter→dead-letter; await a confirmed, routable, persistent republish before acknowledging the original.
- Basis: DEV-18 T08-T14,T21; constitution:185-186,445-457; RecordEnrichmentFailureCommandHandler.cs:23-44 returns category-driven decisions and marks Failed only on the terminal path.
- Do not retry every handler failure or mark a still-retryable movie Failed. Malformed messages use nack(requeue:false) to the configured DLQ; a failed/uncertain republish never triggers a success ack. Cancellation requeues as constitution:456-457 requires.
- Broker transfers and publish/ack crash windows can duplicate messages; retain Core claim/idempotency behavior and do not claim exactly-once delivery.

---

## Q8 — Queue types and crash protection

Question/recommendation: All three queues quorum (x-queue-type=quorum), durable; enrichment.requested also x-delivery-limit (= MaxAttempts) as a crash-loop guard; retry queue uses x-dead-letter-strategy=at-least-once + x-overflow=reject-publish so TTL expiry cannot drop messages.

Ruling: ACCEPT all three queues as durable quorum; use the configured EnrichmentOptions.MaxAttempts as requested's broker delivery limit, and enable at-least-once dead-lettering plus reject-publish on both queues that dead-letter (requested and retry).
- Basis: DEV-18 T02,T09-T11; constitution:322; [RabbitMQ quorum queue documentation](https://www.rabbitmq.com/docs/quorum-queues) requires a DLX, reject-publish and the stream_queue feature flag for at-least-once transfer.
- The broker delivery count protects crash redeliveries of one broker message; it is distinct from EnrichmentRequested.Attempt across application republishes. The pinned RabbitMQ 4.0.0 fixture must exercise these arguments and prerequisites.
- At-least-once protects transfer while destinations are unavailable but permits duplicates; it is not an unconditional no-loss or exactly-once guarantee. DLQ has no automatic retry loop.

---

## Q9 — Legacy topology/configuration

Question/recommendation: Today: queues task_queue / task_queue_dlq, keys RabbitMQ:QueueName / RabbitMQ:DlqName read from raw IConfiguration. REC: remove those keys and names; Worker and publisher bind RabbitMqOptions (section "RabbitMq") via IOptions; no migration of in-flight messages from the old queues (personal app, dev broker); documented in ADR-0004.

Ruling: ACCEPT removal of legacy application queue names/keys and reuse RabbitMq options binding/IOptions; DEV-18 performs no in-flight migration or automatic deletion/purge of old broker queues, with that cutover limitation documented in ADR-0004.
- Basis: DEV-18 T08-T11 replaces the topology; constitution:243-251 bans raw configuration access and hardcoded machine/secret fallbacks; recon-DEV-18:34-41 identifies the old keys.
- Edits to affected host config and dead legacy wiring are forced by the named topology and options rules, hence in scope under the current seat's file rule. Reuse ServiceDefaults.AddLamuFlixOptions rather than duplicate host binding.
- The dev-broker/no-migration premise is recorded [assumed]; an environment with retained old messages needs an operational cutover decision before deployment. Do not claim those messages were drained or migrated.

---

## Q10 — Lifetime, confirms and topology owner

Question/recommendation: Today publisher opens a new connection per publish. REC: one singleton IConnection per process (lazy, async); publisher uses a channel per publish with publisher confirms enabled (CreateChannelOptions publisherConfirmationsEnabled + trackingEnabled; BasicPublishAsync throws on nack → surfaces as failure); a single RabbitMqTopology class declares exchange/queues/bindings idempotently and is called by both publisher (first use) and Worker (startup).

Ruling: ACCEPT one asynchronously initialized, process-owned connection, a disposed confirm-tracking channel per publish, and one shared RabbitMqTopology definition awaited by both hosts; use mandatory publishes and surface returns/nacks/connection failures.
- Basis: DEV-18 T07,T13-T14; constitution:322,368,455-457; [RabbitMQ .NET client API](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/projects/RabbitMQ.Client/PublicAPI/PublicAPI.Shipped.netstandard2.0.txt) names publisherConfirmationsEnabled and publisherConfirmationTrackingEnabled.
- Confirm success alone does not prove routing; mandatory:true and returned-message failure handling prevent silently accepting unroutable messages. Retry/DLQ republishes have the same confirm-before-ack requirement as initial enqueue.
- Do not concurrently publish on a shared unprotected channel; own shutdown/disposal and propagate cancellation. Initialization failure must surface and must not permanently poison later initialization attempts; connection recovery cannot be treated as proof a failed publish was delivered.

---

## Q11 — Test strategy and acceptance mapping

Question/recommendation: Testcontainers RabbitMQ integration tests (existing ContainerFixture, rabbitmq:4.0.0): (a) publish is confirmed and lands in enrichment.requested with quorum type; (b) failing handler routes via enrichment.retry and redelivers after TTL (RetryDelay ~1s in test); (c) after MaxAttempts the message is in enrichment.dead-letter; (d) published headers contain a valid W3C traceparent (ActivityListener registered in test) and the consumer activity shares its TraceId. Pure retry decision (retry vs dead-letter) unit-tested without a broker. Existing WorkerTests/EnrichmentTests updated to the 7.x API, not deleted.

Ruling: ACCEPT the existing RabbitMQ Testcontainers fixture and retained 7.x-updated tests, mapping delayed retry to RateLimited/Core RetryDelayed and using the existing Core policy tests for category/attempt boundaries.
- Basis: DEV-18 T21-T22; constitution:282-304,445-451; recon-DEV-18:49-56,116 identifies the fixture and affected tests.
- Verify confirmed/routable publish, declared quorum arguments, TTL redelivery, terminal/nonretryable DLQ, valid W3C headers and consumer TraceId/redelivery link. Exercise a failed/returned publish to prove no success ack; the ordinary API-only propagation path must not depend on the test listener.
- Use bounded broker-driven waits rather than unbounded sleeps; no RabbitMQ mocks for routing/serialization semantics, no new test framework, and no deletion of WorkerTests/EnrichmentTests.

---

## Q12 — Closing bar and round caps

Question/recommendation: Closing bar = ticket ACs + Q4/Q10 behaviours, all gates green (roslyn, complexity ≤15 then refactor ≤6, inspectcode, full dotnet test, format). Analyze 2 rounds; review 2 rounds, max 2 fix commits per round; review findings bar Critical/High with concrete failure scenario; everything else = follow-up ticket.

Ruling: CHANGE: accept ticket ACs plus Q4/Q10, two analysis rounds, two review rounds and at most two fix commits per review round; retain every applicable configured gate, and use Critical/High concrete failure scenarios as the blocking review bar.
- Basis: DEV-18 T21-T23; PRODUCT.md:12; constitution:375-398; harness.yml:12-29 sets complexity 15/6, mutation 80%, vulnerability and InspectCode gates. The listed analyzer/test/format checks do not waive property, security, mutation or other applicable pipeline checks.
- Frozen scope: DEV-18 T05-T23 plus these adjudicated adapter/consumer/transport behaviors; anything else is a follow-up issue, not a finding in this round. Critical/High broken behavior or user-requested out-of-scope work goes to Rigger after deduplication; other observations are "noted, no ticket" under the current seat assignment.
- Caps are hard; post each formal review on the PR and stop after the cap with unresolved blockers preserved. Missing/unrunnable/blocking-skipped gates are not green; a configured opt-out is reported SKIP, never PASS. These delivery gates are not claims that Phase A has executed them.
