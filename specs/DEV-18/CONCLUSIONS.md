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

---

## Q13 — D3 precondition failed: processing handler and absent provider

Ruling: ACCEPT (a): the minimal Core processing handler is ticket-forced, with provider-gated activation; no owner checkbox. The OMDb adapter is a deduplicated follow-up, not DEV-18 delivery; (b) expands delivery and (c) leaves the consumer acceptance unproven.
- **Handler and classification:** DEV-18 T06,T09,T21 and constitution:445-451 require a working thin consumer whose decisions live in Core. Add sealed `ProcessEnrichmentCommand(MovieId, Attempt)` / `ProcessEnrichmentCommandHandler : ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>` under the existing Enrichment feature: claim before lookup, load current movie data, invoke the Core `IMetadataProvider`, apply the result or classify and record failure, and return Completed (including a false claim acknowledged/ignored) or the existing `EnrichmentFailureDecision`. Compose the existing handlers' logic through ports; do not call one handler from another or introduce a second retry policy (ADR-0017:59-60; constitution:185-186,445-453). This is necessary implementation of the named consumer, not an extra delivered feature (PRODUCT.md:23-24,34-39). `plan.md` must cite the existing built `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs:10-17`; use its single classifier, preserve cancellation, and add no replacement (constitution:181-184,456-457). This ruling replaces brief.md:32-33's nonexistent-handler precondition and ADR-0017:45-47's provider-call placement for DEV-18; transport alone remains in the consumer. No new project, layer or dependency is authorized.
- **Wiring and proof:** Always register the Infrastructure publisher and topology. Register the consumer hosted service and the processing handler via `AddHandler` only when the Core port provider is registered; evaluate this after provider registrations, and resolve processing inside a per-message scope (constitution:130-136,449-451; Infrastructure/Pipeline/ServiceCollectionExtensions.cs:14-35,50-64). With no provider, register neither provider-dependent concrete handler nor consumer, report the inactive consumer at startup, and leave persistent messages in durable `enrichment.requested`; document that activation boundary in ADR-0004. Do not consume/ack messages or install a production stub to mask absence. This delivers the conditional wiring now, not an unconditional registration deferred to another ticket. A registered but invalid provider must surface its configuration failure. Recon R4's no-startup-validation conclusion is rejected: [Microsoft Learn, .NET 10 Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#validatescopes-and-validateonbuild) confirms Development enables ValidateOnBuild and ValidateScopes by default; do not disable them. T21/Q11 remains real RabbitMQ plus the real consumer and Core processing handler, using a Core-port fake provider to drive TTL retry and DLQ; it proves transport/processing, not the deferred OMDb adapter. Q7 confirm-before-ack and cancellation rules remain binding.
- **Provider follow-up:** Rigger must check live open/resolved tickets, fold into existing coverage or file an OMDb implementation of **Core.Ports.IMetadataProvider** in Infrastructure, with typed HttpClient, resilient HTTP behavior, validated options, API-key secret configuration, registration before consumer activation, and real adapter verification; no secrets committed (constitution:99,243-251,300-301,323; PRODUCT.md:47; recon-DEV-18-3:3,11). Parent DEV-282 (Core/metadata seam; DEV-295/brief.md:11), estimate 5 story points, `size:L`; relate it to DEV-18 as the production activation prerequisite. Supplied recon finds no coverage in tracked specs, not a live tracker dedup receipt; creation is complete only when Rigger reports verified. The follow-up stays outside the current chain.

---

## Q14 — D5 repository activation and retry/lease compatibility

Ruling: ACCEPT (1a) and (2A): guard both required Core ports and route both retry actions through the TTL queue with activation-time lease validation; these are ticket-forced wiring/transport corrections within Patron's authority, not owner checkboxes, and do not decide DEV-299 Q1 or DEV-316.
- **Complete activation guard:** Replace Q13's provider-only guard with `IMetadataProvider` AND `IMovieRepository` registration, checked after both registrations; supporting options/time/logging wiring must also be resolvable. Always register the connection owner, topology and publisher; register neither the processing handler nor consumer when either port is absent, report the inactive consumer, and leave messages durably queued. AC3 uses Core-port fakes for both ports with the real handler, consumer and RabbitMQ; production repository/DbContext wiring remains outside DEV-18. Basis: Q13; DEV-18 T06,T09,T21; recon-DEV-18-4:5,17-25; DEV-301 Q11 (scoped production registration deferred, verified by Keel); constitution:99,130-136,449-451. Rigger must deduplicate that wiring work live, fold into coverage or file a follow-up related to DEV-18/DEV-301, outside the chain. No repository implementation or new Api persistence composition is authorized here.
- **Retry transport and validation:** Supersede Q7's `Retry -> requested` mapping: BOTH `Retry` and `RetryDelayed` publish persistently to `retry`; `DeadLetter -> dead-letter` is unchanged. Preserve Core's category/max-attempt decision and `NextAttempt`, with confirmed/routable republish before ack. Add Infrastructure cross-option `IValidateOptions` startup validation when consumer activation is enabled: require an explicitly configured, positive `EnrichmentOptions.ClaimLease` and a positive, representable broker TTL whose effective integer-millisecond duration is strictly greater than ClaimLease. Validate the converted TTL, so rounding cannot erase the strict margin. Keep RetryDelay's existing 30-second fallback only for configurations satisfying that relation; otherwise the operator must configure a larger RetryDelay. Do not invent a ClaimLease default or silently adjust either value. An inactive host with missing ports does not require a processing lease; an activated host with invalid options fails startup without disabling DI validation. Basis: DEV-18 T10,T21; constitution:178-186,452-453; EnrichmentOptions.cs:17; RecordEnrichmentFailureCommandHandler.cs:23-28,39-45; DEV-301 FR-002/US1 scenarios 3 and 5, F1 (Keel-verified). F1's deferral does not prohibit this consumer-required cross-relation check. Q7 is a Patron transport ruling, and neither ticket text nor constitution requires immediate non-rate-limited retries; revising it changes no ticket deliverable or constitution rule (PRODUCT.md:23-24,34-39). No release member, claim-handoff change, or closure of an owner checkbox is authorized.
- **Honest proof and residual recovery:** Reject recon-DEV-18-4:92: an unexpired lease refuses the claim; it does not permit processing. AC3's repository fake must implement DEV-301 FR-002's Pending/strict-expiry predicate and successful-claim timestamp/attempt update over a test TimeProvider; prove that the TTL-returned retry actually reclaims and invokes the provider, not merely that a message traverses queues. Both retry actions wait for the lease-compatible TTL; no claim-always-true fake may conceal the defect. A false claim remains acknowledged/ignored under Q13 and constitution:447-448, but must be identified as skipped/not claimed, never reported as successful enrichment; an exception, cancellation or failed/uncertain republish is never a success ack. ADR-0004 must state that crash redelivery or competing work inside a lease may be refused, leaving recovery dependent on the deferred sweeper; this is a remaining limitation, not proof that recovery exists. Basis: DEV-301 Q8 and existing DEV-316 (Keel-verified); Q1,Q7,Q13; constitution:187-188,447-457. DEV-299 Q1 remains the owner's open claim-handoff decision as supplied by Keel; this transport correction neither answers nor preempts it.

---

## Follow-up estimate encoding — Rigger execution ruling

Ruling: Leave the repository/DbContext wiring follow-up's YouTrack Estimated Time unset; record `Planning estimate: 3 SP (story points)` in its description, retain `size:M`, and file outside the chain. Do not encode the planning estimate as `3d` or invent a points-to-time conversion.
- Basis: Rigger's live project/script verification reports only an Estimated Time Period field and no supported story-point field or established conversion; DEV-301/DEV-303's `2d` and DEV-18's `3d` are time values, not evidence of point equivalence. This clarifies Patron's existing Q14 follow-up filing instruction under the seat's tracker-decision authority, without changing delivery scope or either dependency direction.

---

## Follow-up create estimate — mandatory helper parameter correction

Ruling: Supply `-Estimate '3d'` when creating the repository/DbContext wiring follow-up, keep `size:M` and `Planning estimate: 3 SP (story points)` in the description, and explicitly record `3d` as an independently selected time estimate, not a conversion from points. This supersedes the preceding unset-Estimated-Time ruling; do not amend the tracker helper.
- Basis: Conductor relays Rigger's verified `scripts/local/Edit-YouTrackIssue.ps1:73` Create parameter contract: Estimate is mandatory and accepts period values, so omission cannot execute through the required helper. Patron selects three days as a separate planning estimate for the scoped registration, host wiring and verification already authorized by Q14; no established SP-to-time mapping is asserted. The current seat gives Patron follow-up estimate and tracker-change decisions. Delivery scope, parent DEV-282, dependency directions and outside-chain placement are unchanged.

---

## Q15 — RabbitMQ health-check registration and absent endpoint foundation

Ruling: DEV-18 MUST register a package-free RabbitMQ readiness check over its connection owner; no owner checkbox is required for this constitution-required dependency integration. The pre-existing application-wide endpoint gap goes to a dedicated follow-up outside the chain; registration alone is not a claim that readiness is reachable or constitution VI is fully satisfied.
- **Scope and ownership:** Constitution:232-234 explicitly requires every new external dependency to register a health check, so the new built RabbitMQ adapter cannot silently defer its own check. Add `RabbitMqHealthCheck : IHealthCheck` in Infrastructure/RabbitMq and contribute it to the readiness registrations whenever the connection owner/publisher is registered, including when the consumer is inactive. Reuse the owner and cancellation; report broker connection failure as unhealthy, without publishing, consuming, or exposing credentials. Add only the generic `AddHealthChecks` foundation needed for registration in existing ServiceDefaults/Extensions.cs; ServiceDefaults owns common health wiring, Infrastructure owns the broker-specific contribution, and Api does not duplicate it (constitution:106-109,235-236). This is a deliberate file-scope ruling for that minimal ServiceDefaults edit. No package, project or architectural layer is authorized (recon-DEV-18-5:2,4; DEV-18 T07,T13; PRODUCT.md:34-48). Supersede tasks.md's exclusion of the RabbitMQ readiness check; this completes mandatory integration hygiene without adding a separate ticket deliverable or taking a constitution departure.
- **Existing gap and follow-up:** Recon-DEV-18-5:2-3 confirms that health infrastructure/routes were already absent and no ticket owns them. Rigger must file dedicated shared health-endpoint wiring, after preserving that live dedup receipt: unauthenticated `/health/live` with no dependency checks and `/health/ready` with Postgres and RabbitMQ checks, in ServiceDefaults and the existing hosts. Use DEV-18's broker contribution instead of a second check. The spec must identify the unresolved endpoint foundation and must not label it implemented, reachable or fully aligned; the follow-up addresses an existing constitution gap, not a waiver for the newly added broker check. Basis: constitution:232-236; PRODUCT.md:34-39; supplied recon. Parent DEV-283, `size:M`, Estimated Time `2d` as a time estimate; depends on DEV-18 for its broker check, outside the current chain. No claim-handoff decision or owner checkbox is closed.
- **Tracker receipt:** Rigger reports DEV-392 created and repaired in place, with final live read-back verified: parent DEV-282, size:M, planning 3 SP in description, separate Estimated Time 3d; DEV-392 depends on DEV-301 and DEV-18 depends on DEV-392; supplied comments posted/read back on both parents. This satisfies Q14's repository-wiring filing request; DEV-392 remains outside the chain. Basis: Rigger's verified creation/field/link/comment receipt received in this turn; no independent tracker mutation by Patron.
