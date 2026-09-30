# DEV-18 — Alignment Brief

Grill outcome for DEV-18 (parent DEV-283, size L, UI false): upgrade to RabbitMQ.Client 7.x, declare quorum topology with TTL retry and DLQ, propagate W3C trace context, and write ADR-0004/0005.

- Rulings and cited bases: `specs/DEV-18/CONCLUSIONS.md` (Q1–Q12, Patron, commit `30f2382`; Q13, commit `d90eca2`). Taste defaults: `specs/DEV-18/ASSUMPTIONS.md`.
- Facts: notes `recon-DEV-18`, `recon-DEV-18-2`, `recon-DEV-18-3`, and the Conductor's placement resolution of 2026-09-30.
- Grill: **12 questions asked (budget 12), 12/12 answered.** 9 accepted, 3 changed (Q6, Q7, Q12). Q13 was ruled after the grill on Quill's `needs decision:` (D5). No owner checkbox: nothing changes the ticket text or departs from the constitution.
- The ticket text (YouTrack DEV-18, cited as T01–T23 in CONCLUSIONS.md) is authoritative. If this brief and the ticket disagree, the ticket wins and the disagreement is a defect in this brief.

## Solution facts this brief rests on

- The built solution (`LamuFlix.sln`) contains Core, Infrastructure, ServiceDefaults, Api, ArchitectureTests, Tests.Common, UnitTests and IntegrationTests.
- `src/LamuFlix.Web`, `src/LamuFlix.Worker`, `src/LamuFlix.Data` and `tests/LamuFlix.Test` were retired from the solution by DEV-19 (`46b1d9be3`). The RabbitMQ code recon found there (the 6.x publisher, the `QueueWorker`, `EnrichmentJobProcessor`, `MovieEnrichmentMessage`, and the `task_queue`/`task_queue_dlq` names) is **legacy and not built**.
- `IEnrichmentQueue` (the `EnqueueAsync(EnrichmentRequested, CancellationToken)` port) has **no implementation** today (recon-DEV-18-2:37).
- Three Core handlers already call the port: `RequestEnrichmentCommandHandler`, `RequeueStrandedMoviesCommandHandler` and `ImportMovieFolderCommandHandler`.
- `RabbitMQ.Client` is centrally pinned at `6.8.1`. The latest stable 7.x release is `7.2.2`, which targets netstandard2.0 and net8.0. `Testcontainers.RabbitMq` `4.15.0` requires `>= 6.8.1`, which 7.2.2 satisfies.
- No OpenTelemetry package exists yet. ArchitectureTests forbids Core from referencing RabbitMQ (lines 47-49) and OpenTelemetry (line 63).

## Decisions after the grill (Keel, recorded here first)

- **D1: Q5 condition resolved (no deletion).**
  - Neither legacy type is built, and neither has a built consumer: Web's `IEnrichmentQueuePublisher` and Data's `MovieEnrichmentMessage` exist only in retired, unbuilt projects.
  - So nothing in the solution needs migrating, and **DEV-18 deletes no legacy file.** It delivers a new implementation in Infrastructure.
  - Removing the retired folders is not named in the ticket (§2.3 item 6). If no existing ticket covers it, it is a follow-up for Rigger to file after checking for duplicates.
- **D2: Q11 test targets retargeted.**
  - `WorkerTests` and `EnrichmentTests` live in the retired `tests/LamuFlix.Test`, which is not built.
  - Patron's rule "update, do not delete" is met by leaving them untouched.
  - The new broker tests go in `tests/LamuFlix.IntegrationTests` and use the RabbitMQ container in `tests/LamuFlix.Tests.Common/ContainerFixture.cs` (image `rabbitmq:4.0.0`).
- **D3: Consumer host.**
  - Api is the only host in the solution, so the consumer is a `BackgroundService` in `LamuFlix.Infrastructure/RabbitMq/`, registered through Infrastructure's DI extension and run by Api.
  - This adds no project and no layer.
  - The consumer dispatches each `EnrichmentRequested` to the existing Core enrichment handler(s) through the existing handler pipeline. It maps the outcome from `RecordEnrichmentFailureCommandHandler` (Retry / RetryDelayed / DeadLetter) to routing keys as Q7 sets out.
  - ~~Precondition for the plan: cite the existing Core processing handler.~~ **Superseded by D5.** The precondition failed: no such handler exists (recon-DEV-18-3 R2). The consumer still must not grow enrichment logic of its own. Transport stays in the consumer, and decisions stay in Core.
- **D4: Package pins.**
  - `RabbitMQ.Client` goes to `7.2.2`.
  - `OpenTelemetry.Api` is pinned to the latest stable 1.x release that targets net8.0 or later. Wisp confirms the exact version at implementation; it is recorded in `plan.md` and in Directory.Packages.props.
  - Only `LamuFlix.Infrastructure` references `OpenTelemetry.Api` (Q3). Only Infrastructure and Tests.Common/IntegrationTests reference `RabbitMQ.Client`, as test consumers.
- **D5: Core processing handler and provider-gated activation (Patron Q13, `d90eca2`; recon-DEV-18-3).** No owner checkbox: Q13 rules the handler ticket-forced by T06, T09 and T21.
  - **Handler.** Add sealed `ProcessEnrichmentCommand(MovieId, Attempt)` and `ProcessEnrichmentCommandHandler : ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>` under `src/LamuFlix.Core/Features/Enrichment/`. It runs in this order:
    1. claim first;
    2. load the current movie data;
    3. call `Core.Ports.IMetadataProvider.FindAsync`;
    4. apply the result, or classify the exception and record the failure.
  - **Outcome.** `ProcessEnrichmentOutcome` is either `Completed` or the existing `EnrichmentFailureDecision`. A false claim also counts as `Completed`: it is acked and ignored.
  - **Composition.** The handler composes the existing handlers' logic **through ports**. It never calls one handler from another. It adds no second retry policy, and no `Attempt < MaxAttempts` check outside the existing Core policy.
  - **Classification.** Use the existing `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs:10-17`, the single classifier. `plan.md` cites it by file:line. Preserve cancellation: an `OperationCanceledException` from cancellation is never classified as a failure.
  - **Consumer.** The consumer creates a per-message `IServiceScope` and dispatches `ProcessEnrichmentCommand` through the composed `ICommandHandler<,>` (Validation, then Logging, then Tracing). It maps `Completed` to ack. It maps the decision to a routing key as in AC3, confirming before the ack.
  - **Guarded activation.** Infrastructure's DI extension always registers the connection owner, the topology and the publisher. It registers the consumer hosted service and `ProcessEnrichmentCommandHandler` (via `AddHandler`) **only when an `IMetadataProvider` registration exists**. That check runs after provider registrations.
    - With no provider, neither is registered, the Api starts, and one startup log line reports the consumer as inactive. Messages wait durably in `enrichment.requested`.
    - No production stub or no-op provider is installed, and no message is consumed or acked while inactive.
    - A registered but misconfigured provider surfaces its own failure.
  - **Startup validation.** Development enables `ValidateOnBuild` and `ValidateScopes` by default; recon R4 is rejected on this point. Keep both enabled, and never disable them to make the guard work.
  - **ADR-0004** documents the activation boundary: the consumer is inactive until a provider is registered.
  - **Proof.** AC3 tests run the real broker, the real consumer and the real `ProcessEnrichmentCommandHandler`, with a **Core-port fake `IMetadataProvider`** registered in the test host. That fake is not a RabbitMQ mock, so Q11 holds. The tests prove transport and processing, not the OMDb adapter.
  - **Out of scope:** the OMDb implementation of `Core.Ports.IMetadataProvider`. Rigger files it, or folds it into an existing ticket, after a live dedup: parent DEV-282, `size:L`, 5 points, related to DEV-18 as the production activation prerequisite. The follow-up stays outside this chain.

## Closing bar

Every item below is required. They map to T21–T23 plus the Q4/Q10 behaviours.

- **AC1: Publisher confirms (T13, T14, T21, Q10).**
  - `RabbitMqEnrichmentQueuePublisher : IEnrichmentQueue` publishes persistent, mandatory messages to `lamuflix.enrichment` with routing key `requested`.
  - Each publish uses its own channel, created with `publisherConfirmationsEnabled` and `publisherConfirmationTrackingEnabled`.
  - A nack, a return (unroutable message) or a connection failure surfaces as an exception from `EnqueueAsync`. None of them may complete successfully.
  - The integration test proves both a confirmed routed publish and a failing unroutable or returned publish.
- **AC2: Topology (T07–T11, Q7, Q8).**
  - A single `RabbitMqTopology` declares the durable direct exchange `lamuflix.enrichment` and three durable **quorum** queues, all idempotently.
  - `enrichment.requested`:
    - bound to `requested`;
    - DLX `lamuflix.enrichment`, with dead-letter key `dead-letter`;
    - `x-delivery-limit` = `EnrichmentOptions.MaxAttempts`;
    - at-least-once dead-lettering with `x-overflow=reject-publish`.
  - `enrichment.retry`:
    - bound to `retry`;
    - `x-message-ttl` = `RabbitMqOptions.RetryDelay` in milliseconds;
    - DLX `lamuflix.enrichment`, with dead-letter key `requested`;
    - at-least-once dead-lettering with `x-overflow=reject-publish`.
  - `enrichment.dead-letter`: bound to `dead-letter`, with no automatic retry.
  - The publisher (on first use) and the consumer (at startup) both await this one owner.
  - The integration test asserts the declared queue type and arguments against the real broker.
- **AC3: TTL retry routing and DLQ (T10, T11, T21, Q6, Q7).**
  - The consumer runs on `AsyncEventingBasicConsumer` over an `IChannel` (T06), with prefetch set from `RabbitMqOptions.Prefetch`.
  - Core outcomes route as follows:
    - Retry goes to `requested`.
    - RetryDelayed goes to `retry`, and so comes back through the TTL.
    - DeadLetter goes to `dead-letter`.
  - The attempt policy stays in Core, with no second `Attempt < MaxAttempts` check in the adapter. `EnrichmentRequested.Attempt` is the counter on the wire, and its counting convention is preserved.
  - The original message is acked **only after** the republish is confirmed and routed. If the republish fails or its outcome is uncertain, the message is never acked as a success.
  - A message that cannot be deserialized is nacked with `requeue:false` and goes to the DLQ through the DLX.
  - On cancellation the message is requeued.
  - A movie is marked Failed only on the terminal path.
  - Processing is dispatched to `ProcessEnrichmentCommandHandler` (D5). `Completed` means ack.
  - Integration tests prove three things:
    - a RetryDelayed outcome (for example a RateLimited category) comes back after the TTL, using a RetryDelay of about 1 s in the test;
    - a terminal or non-retryable outcome lands in `enrichment.dead-letter`;
    - a malformed body lands in `enrichment.dead-letter`.
- **AC4: W3C propagation (T15, T22, Q3, Q4).**
  - `Propagators.DefaultTextMapPropagator` is explicitly set to the API's `TraceContextPropagator` (W3C) before first use. It stays a no-op until configured.
  - The publisher starts an `ActivityKind.Producer` activity from the existing `LamuFlix` ActivitySource and injects `traceparent` (and `tracestate` only when present) into `BasicProperties.Headers`.
  - If `StartActivity` returns null because nothing is listening, the publisher still injects a valid W3C context: the ambient context if there is one, otherwise a new one.
  - The consumer extracts the context and starts an `ActivityKind.Consumer` activity parented to it. A redelivery adds an `ActivityLink` to the earlier context.
  - Telemetry names come from `TelemetryConstants`.
  - Tests cover:
    - valid `traceparent` headers on the ordinary path with **no** ActivityListener;
    - a consumer activity that shares the producer's TraceId, with a listener registered;
    - the redelivery link.
- **AC5: Options (Q6, Q9).**
  - `RabbitMqOptions` gains:
    - `RetryDelay` (TimeSpan, default 30 s, positive, must convert to a TTL in int milliseconds);
    - `Prefetch` (ushort, default 1, positive).
  - `MaxAttempts` stays in `EnrichmentOptions`.
  - Both are bound through `ServiceDefaults.AddLamuFlixOptions` with startup validation.
  - Nothing reads the raw `IConfiguration`, and no host or secret value is hardcoded as a fallback.
  - The Api appsettings gain a `RabbitMq` section with no secrets. The password comes from user-secrets or the environment.
- **AC6: Connection lifetime (Q10).**
  - There is one process-owned, asynchronously initialized `IConnection`.
  - If initialization fails, the error surfaces and a later attempt can retry; the failure does not stick.
  - Shutdown and disposal are handled by the owner, cancellation is propagated, and no channel is shared between concurrent publishes.
- **AC7: ADRs (T16–T18, T23, Q1, Q2).**
  - `docs/adr/ADR-0004.md` covers the topology, retry queue, DLQ and sweeper design. Its context and consequences also cover the client-7 async model, publisher confirms, trace propagation, and the cutover with no migration of old queues.
  - `docs/adr/ADR-0005.md` covers dual-write mitigation: the sweeper now, a transactional outbox as a stretch goal.
  - Both are `Status: Accepted` in the PR; your merge is the acceptance.
  - Both state that DEV-18 **records** the sweeper design but does **not** implement it.
- **AC8: Gates (Q12).** Every configured gate applies:
  - build;
  - Roslyn analyzers;
  - cyclomatic complexity: ≤15, then ≤6 in the refactor gate;
  - InspectCode;
  - `dotnet format --verify-no-changes`;
  - the full `dotnet test`;
  - the vulnerable-package scan;
  - mutation at ≥80%;
  - the architecture tests: Core still references neither RabbitMQ nor OpenTelemetry.

  A gate that was skipped or could not run is reported as SKIP, never as PASS.

## Frozen scope

- `Directory.Packages.props`: bump `RabbitMQ.Client` from `6.8.1` to `7.2.2`, and add an `OpenTelemetry.Api` pin.
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`: add package references to `RabbitMQ.Client` and `OpenTelemetry.Api`.
- New files under `src/LamuFlix.Infrastructure/RabbitMq/`:
  - `RabbitMqTopology`;
  - the connection owner;
  - `RabbitMqEnrichmentQueuePublisher`;
  - the consumer `BackgroundService`;
  - the outcome-to-routing-key mapping;
  - the propagation helper (header inject/extract);
  - the DI registration extension.

  Quill finalizes file names in `plan.md`.
- `src/LamuFlix.Core/Features/Enrichment/`: `ProcessEnrichmentCommand`, `ProcessEnrichmentCommandHandler` and `ProcessEnrichmentOutcome`, plus a validator if the pipeline convention requires one (D5). No other Core enrichment file is edited unless `plan.md` justifies it by file:line.
- `src/LamuFlix.Core/Options/RabbitMqOptions.cs`: add `RetryDelay` and `Prefetch`, with validation. Core stays free of RabbitMQ and OpenTelemetry references.
- `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`: add messaging activity and tag names only if the constants they need do not already exist.
- `src/LamuFlix.Api`: wire the registration (`Program.cs` or the existing composition root) and add the `RabbitMq` section to the appsettings.
- `tests/LamuFlix.IntegrationTests`: the broker tests for AC1–AC4.
- `tests/LamuFlix.UnitTests`: `ProcessEnrichmentCommandHandler` over fake ports, covering a false claim, success, each classified failure path and cancellation. Also options validation, the pure outcome-to-routing mapping, and the guarded-registration rule with and without a provider. Any gap in the existing Core policy tests is extended there.
- `tests/LamuFlix.Tests.Common`: fixture changes only if a test needs them, for example exposing a connection helper.
- `docs/adr/ADR-0004.md` and `docs/adr/ADR-0005.md`.
- `CONTEXT.md`: glossary terms (retry queue, dead-letter queue, publisher confirm, traceparent) only if the file already has a glossary.

**Out of scope. Each is a follow-up, never a finding in this round:**

- implementing the sweeper (Q1);
- the OMDb (or any real) `IMetadataProvider` implementation (D5, Q13);
- a transactional outbox;
- deleting or editing the retired `src/LamuFlix.Web`, `src/LamuFlix.Worker`, `src/LamuFlix.Data` or `tests/LamuFlix.Test` (D1);
- migrating or purging `task_queue`/`task_queue_dlq` on any broker (Q9);
- exponential backoff;
- an OpenTelemetry SDK, exporter or instrumentation package;
- a docker-compose file;
- schema changes. The sweeper would use the existing `MovieRecord` columns `LastAttemptAt`, `EnrichmentAttempts` and `Status`; the ADRs may cite them.

## Approach and task ordering

1. **Packages.** Bump `RabbitMQ.Client` to 7.2.2, add the `OpenTelemetry.Api` pin, and add the Infrastructure package references. The solution still builds because nothing in it uses the 6.x API.
2. **Options.** `RetryDelay` and `Prefetch` with validation and unit tests (test first).
3. **Topology and connection owner.** An integration test asserts the declared arguments on `rabbitmq:4.0.0`.
4. **Publisher.** Confirms, the mandatory flag, returns, and the propagation helper. Integration tests cover AC1 and AC4 (inject), including the path with no listener.
5. **Core processing handler (D5).** `ProcessEnrichmentCommandHandler` with unit tests first, over fake ports. It has no broker dependency and can run in parallel with steps 3 and 4.
6. **Consumer.** The async consumer, a per-message scope dispatching `ProcessEnrichmentCommand`, prefetch, outcome mapping, confirm before ack, malformed messages to the DLQ, and requeue on cancel. Integration tests cover AC3 and AC4 (extract and link), with a Core-port fake provider.
7. **DI registration and Api wiring,** with provider-gated consumer activation (D5), plus the appsettings.
8. **ADR-0004 and ADR-0005.** These have no code dependency and can run in parallel with steps 3–7. ADR-0004 includes the D5 activation boundary.
9. **Gates and refactor.** All AC8 gates, then the refactor pass that brings complexity down to ≤6.

Ordering constraints:

- Step 3 comes before steps 4 and 5, because both call the one topology owner.
- Step 4 comes before step 6's republish path, because the republish reuses the publisher's confirm and mandatory logic. There must be no second publish implementation.
- Step 5 comes before step 6, because the consumer dispatches the handler.

## Test strategy

- The broker is real for everything about routing, TTL, dead-lettering, confirms and serialization. No RabbitMQ mocks cover those semantics.
- Tests wait with bounded, broker-driven polling (`BasicGetAsync` or a consumer with a timeout). No unbounded sleeps.
- Unit tests stay pure: options validation, outcome-to-routing mapping, and the header carrier.
- Consider FsCheck property tests for the header carrier round trip (inject then extract preserves the TraceId).
- No new test framework and no new test project.

## Round cap

- Analyze: 2 rounds.
- Review: 2 rounds, with at most 2 fix commits per round.
- Blocking review bar: Critical or High, with a concrete failure scenario. Other observations are "noted, no ticket" unless they meet the charter's filing criteria.
- Caps are hard. Once a cap is reached, stop, keep any unresolved blockers, and post each formal review on the PR.

## Human gate 1

Owner-held. `spec.md`, `plan.md` and `tasks.md` go into the spec PR. There are no owner checkboxes from this grill. Merging is the user's action.
