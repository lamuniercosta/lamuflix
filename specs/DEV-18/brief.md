# DEV-18 — Alignment Brief

Grill outcome for DEV-18 (parent DEV-283, size L, UI false): upgrade to RabbitMQ.Client 7.x, declare quorum topology with TTL retry and DLQ, propagate W3C trace context, and write ADR-0004/0005.

- Rulings and cited bases: `specs/DEV-18/CONCLUSIONS.md` (Q1–Q12, Patron, commit `30f2382`; Q13, commit `d90eca2`; Q14, commit `d046b24`; Q15, commit `7788471`). Taste defaults: `specs/DEV-18/ASSUMPTIONS.md`.
- Facts: notes `recon-DEV-18`, `recon-DEV-18-2`, `recon-DEV-18-3`, `recon-DEV-18-4` (its R3 conclusion is rejected by Q14), and the Conductor's placement resolution of 2026-09-30.
- Grill: **12 questions asked (budget 12), 12/12 answered.** 9 accepted, 3 changed (Q6, Q7, Q12). Q13 and Q14 were ruled after the grill on Quill's `needs decision:` requests (D5, D6). No owner checkbox: nothing changes the ticket text or departs from the constitution.
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
  - **Outcome.** `ProcessEnrichmentOutcome` is either `Completed` or the existing `EnrichmentFailureDecision`. A false claim is acked and ignored, but it is reported as skipped (not claimed), never as successful enrichment (D6).
  - **Composition.** The handler composes the existing handlers' logic **through ports**. It never calls one handler from another. It adds no second retry policy, and no `Attempt < MaxAttempts` check outside the existing Core policy.
  - **Classification.** Use the existing `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs:10-17`, the single classifier. `plan.md` cites it by file:line. Preserve cancellation: an `OperationCanceledException` from cancellation is never classified as a failure.
  - **Consumer.** The consumer creates a per-message `IServiceScope` and dispatches `ProcessEnrichmentCommand` through the composed `ICommandHandler<,>` (Validation, then Logging, then Tracing). It maps `Completed` to ack. It maps the decision to a routing key as in AC3, confirming before the ack.
  - **Guarded activation.** Infrastructure's DI extension always registers the connection owner, the topology and the publisher. It registers the consumer hosted service and `ProcessEnrichmentCommandHandler` (via `AddHandler`) **only when both `IMetadataProvider` and `IMovieRepository` registrations exist** (D6 supersedes the provider-only guard). That check runs after both registrations.
    - With either port missing, neither is registered, the Api starts, and one startup log line reports the consumer as inactive. Messages wait durably in `enrichment.requested`.
    - No production stub or no-op provider is installed, and no message is consumed or acked while inactive.
    - A registered but misconfigured provider surfaces its own failure.
  - **Startup validation.** Development enables `ValidateOnBuild` and `ValidateScopes` by default; recon R4 is rejected on this point. Keep both enabled, and never disable them to make the guard work.
  - **ADR-0004** documents the activation boundary: the consumer is inactive until a provider is registered.
  - **Proof.** AC3 tests run the real broker, the real consumer and the real `ProcessEnrichmentCommandHandler`, with **Core-port fakes for `IMetadataProvider` and `IMovieRepository`** registered in the test host (D6). That fake is not a RabbitMQ mock, so Q11 holds. The tests prove transport and processing, not the OMDb adapter.
  - **Out of scope:** the OMDb implementation of `Core.Ports.IMetadataProvider`. Rigger files it, or folds it into an existing ticket, after a live dedup: parent DEV-282, `size:L`, 5 points, related to DEV-18 as the production activation prerequisite. The follow-up stays outside this chain.

- **D6: Complete guard and lease-safe retry routing (Patron Q14, `d046b24`; recon-DEV-18-4 and DEV-301 on main).** No owner checkbox. DEV-299 Q1 and DEV-316 are not decided or preempted.
  - **Guard.** Activation requires **both** `IMetadataProvider` and `IMovieRepository` registrations, checked after both are registered. Supporting options, `TimeProvider` and logging must also be resolvable.
    - Either port missing: no processing handler, no consumer, one inactive-consumer startup log, and messages stay durably queued. Connection owner, topology and publisher are always registered.
    - Production repository and DbContext wiring is **out of scope** (DEV-301 Q11 deferred it). Rigger dedups it live, then folds it into existing coverage or files a follow-up related to DEV-18 and DEV-301, outside this chain.
  - **Routing (supersedes Q7 and AC3's `Retry -> requested`).**
    - Both `Retry` and `RetryDelayed` republish persistently to `retry`. `DeadLetter` goes to `dead-letter`, unchanged.
    - Core's decision and `NextAttempt` are preserved. Confirm and route before the ack.
  - **Cross-option validation.**
    - An Infrastructure `IValidateOptions` runs at startup **only when the consumer is active**. It requires an explicitly configured, positive `EnrichmentOptions.ClaimLease`.
    - It requires a positive, representable broker TTL whose integer-millisecond value is **strictly greater** than ClaimLease. It validates the converted value, so rounding cannot erase the margin.
    - The 30 s RetryDelay default stays. No ClaimLease default is invented, and neither value is silently adjusted.
    - An active host with invalid options fails startup. `ValidateOnBuild` and `ValidateScopes` stay on.
    - An inactive host needs no lease.
  - **Honest proof.**
    - AC3's fake repository implements DEV-301 FR-002: Pending status and a strict lease expiry (`LastAttemptAt == null || LastAttemptAt < now - ClaimLease`). A successful claim stamps `LastAttemptAt` and increments attempts, over a test `TimeProvider`.
    - The test proves that a TTL-returned retry **reclaims and invokes the provider**, not just that a message moves between queues. No fake whose claim always succeeds is allowed.
  - **Skipped claim.** A false claim is acked and ignored, but logged and reported as skipped (not claimed), never as success.
    - An exception, cancellation, or failed or uncertain republish is never acked as success.
  - **ADR-0004 limitation.** Crash redelivery, or competing work inside a lease, may be refused and acked. Recovery then depends on the deferred sweeper. ADR-0004 states this as a remaining limitation, not as recovery that exists.

- **D7: Plan decisions from analyze round 1 (Keel).** These settle the points `plan.md` left open at `136a10c`. None of them is a §2.3 trigger.
  - **Shared retry decision (required, not optional).** Duplicating the policy is forbidden (D5) and so is calling another handler, so extraction is the only conforming option.
    - Move the pure rule at `RecordEnrichmentFailureCommandHandler.cs:39-45` (`IsRetry`, `RetryAction`) into one new Core file, `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs`. It is an `internal static` class, and `LamuFlix.UnitTests` already sees internals if this repo grants that; otherwise make it `public static`.
    - Its one function takes the category, the attempt and `MaxAttempts`. It returns the retry `EnrichmentFailureDecision` (`Retry` or `RetryDelayed`, `NextAttempt = Attempt + 1`), or null when the path is terminal.
    - Both handlers call it. The edit to `RecordEnrichmentFailureCommandHandler.cs` is behaviour-preserving: it replaces two private members, and its existing tests stay green unchanged. That is an edit, not a rewrite (§2.3 item 6 is not triggered). The new file is the only Core addition beyond the D5 files.
  - **Terminal path persistence.** On the terminal path `ProcessEnrichmentCommandHandler` calls `MarkFailed(category, time.GetUtcNow())` on the movie it already loaded, calls `SaveChangesAsync`, and returns `DeadLetter` with a null `NextAttempt`, as `RecordEnrichmentFailureCommandHandler.cs:30-35` does. On a retry path it writes nothing: the claim already stamped the attempt.
  - **Categories.** A provider result that already carries a category uses that category. Only an exception goes through `EnrichmentFailureClassifier.Classify`. A cancellation on the handler's token propagates (`EnrichmentFailureClassifier.cs:92-102` rethrows it).
  - **Skipped shape (Quill plan note 1: accepted).** A flag on `Completed` conforms to D5 ("`Completed` or the existing decision"). Name it by what happened, `Claimed` (false = skipped), not by success.
    - The consumer writes a distinct skipped log event and sets the outcome tag to skipped, never to enriched.
    - Every processing log carries the movie id, the attempt and, where one applies, the category (constitution VI).
  - **Propagator location.** `Propagators.DefaultTextMapPropagator` is set in the DI registration extension (plan D-7), not in the carrier.
  - **Inactive log mechanism.** No logger exists during service registration. On the inactive path the extension registers a small hosted service that logs the inactive line once in `StartAsync`, declared in `RabbitMqServiceCollectionExtensions.cs`, so no file is added. A unit test asserts exactly one line.
  - **Telemetry names.** Use the constitution VI names: span `Enrichment.Enqueue` (producer) and `Enrichment.Process` (consumer); attributes `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count` and `error.type`. Add them to `TelemetryConstants` only where they are missing. No metric instruments: the ticket does not name them.
  - **Glossary.** `CONTEXT.md` already has a glossary (`CONTEXT.md:44-48`), so the glossary task is unconditional (constitution VIII).

- **D8: RabbitMQ readiness check (Patron Q15, `7788471`; recon-DEV-18-5).** No owner checkbox.
  - **Check.** Add `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqHealthCheck.cs` (`IHealthCheck`). It reuses the connection owner and the cancellation token. A broker connection failure reports Unhealthy. It never publishes or consumes, and it never puts credentials or raw exception text in the result description (constitution IV:189 and VI).
  - **Registration.** The Infrastructure DI extension adds the check with a `ready` tag whenever it registers the connection owner and publisher, **including when the consumer is inactive**. The check is not gated on the D5/D6 guard.
  - **Foundation.** `src/LamuFlix.ServiceDefaults/Extensions.cs` gains only the generic `AddHealthChecks()` call in `AddServiceDefaults`. ServiceDefaults already has the `Microsoft.AspNetCore.App` framework reference (`LamuFlix.ServiceDefaults.csproj`), so this needs no package. Api does not duplicate either call. This is Patron's deliberate file-scope ruling for that one edit.
  - **Not in DEV-18.** No `/health/live` or `/health/ready` route is mapped. The endpoint foundation is a pre-existing gap that goes to the Rigger follow-up (Q15). The spec names that gap and never calls readiness reachable, or constitution VI fully aligned.
  - **Tests.** Real broker (IntegrationTests): Healthy against the fixture, and Unhealthy with an unreachable broker using bounded waits. The description carries no credentials. Unit (guard): the check is registered with the `ready` tag in both the active and inactive paths.
  - **ADR-0004** records the check as the broker's readiness contribution and states that the endpoint wiring is outstanding.
  - **Prerequisite.** Q15 assumed the check needs no package, which holds only for ServiceDefaults. `IHealthCheck` lives in `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`, `AddHealthChecks()`/`AddCheck` live in `Microsoft.Extensions.Diagnostics.HealthChecks`, and `LamuFlix.Infrastructure.csproj` references neither. D8a settles this.
- **D8a: Infrastructure health-check dependency (Patron Q16, `08c3790`).** No owner checkbox.
  - Add `<PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="10.0.12" />` to `Directory.Packages.props`, next to the existing `Microsoft.Extensions.*` `10.0.12` pins (`Directory.Packages.props:18-19`).
  - Add a versionless `<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />` to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`.
  - No separate `…HealthChecks.Abstractions` reference; the full package brings it transitively and supplies `AddCheck`.
  - No `Microsoft.AspNetCore.App` framework reference in Infrastructure. ServiceDefaults keeps its existing framework reference and adds no package for `AddHealthChecks()`.
  - This corrects Q15's no-package premise only. The deliverable, the endpoint gap and the out-of-scope list are unchanged.
  - **Dependencies.** DEV-392 (production repository and DbContext wiring) was filed by Rigger and fulfils D6's follow-up. DEV-18 depends on DEV-392 only for production activation, not for delivery.

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
    - Retry **and** RetryDelayed both go to `retry`, and so come back through the TTL (D6 supersedes Q7's `Retry -> requested`).
    - DeadLetter goes to `dead-letter`.
  - The attempt policy stays in Core, with no second `Attempt < MaxAttempts` check in the adapter. `EnrichmentRequested.Attempt` is the counter on the wire, and its counting convention is preserved.
  - The original message is acked **only after** the republish is confirmed and routed. If the republish fails or its outcome is uncertain, the message is never acked as a success.
  - A message that cannot be deserialized is nacked with `requeue:false` and goes to the DLQ through the DLX.
  - On cancellation the message is requeued.
  - A movie is marked Failed only on the terminal path.
  - Processing is dispatched to `ProcessEnrichmentCommandHandler` (D5). `Completed` means ack; a skipped (false) claim is acked and logged as skipped (D6).
  - Integration tests prove three things:
    - a Retry **and** a RetryDelayed outcome (for example a RateLimited category) each come back after the TTL, **reclaim** the movie through the lease-aware fake repository, and invoke the provider again. The test uses a ClaimLease shorter than the test RetryDelay (for example 500 ms and 1 s) over a test `TimeProvider`;
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
    - `RetryDelay` (TimeSpan, default 30 s, positive, must convert to a TTL in int milliseconds). When the consumer is active, the converted TTL must be strictly greater than `ClaimLease` (D6);
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

- `Directory.Packages.props`: bump `RabbitMQ.Client` from `6.8.1` to `7.2.2`, add an `OpenTelemetry.Api` pin, and add the `Microsoft.Extensions.Diagnostics.HealthChecks` `10.0.12` pin (D8a).
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`: add package references to `RabbitMQ.Client`, `OpenTelemetry.Api` and `Microsoft.Extensions.Diagnostics.HealthChecks` (D8a).
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqHealthCheck.cs` and its `ready`-tagged registration in the DI extension (D8).
- `src/LamuFlix.ServiceDefaults/Extensions.cs`: the generic `AddHealthChecks()` call only (D8).
- New files under `src/LamuFlix.Infrastructure/RabbitMq/`:
  - `RabbitMqTopology`;
  - the connection owner;
  - `RabbitMqEnrichmentQueuePublisher`;
  - the consumer `BackgroundService`;
  - the outcome-to-routing-key mapping;
  - the propagation helper (header inject/extract);
  - the DI registration extension.

  Quill finalizes file names in `plan.md`.
- `src/LamuFlix.Core/Features/Enrichment/`: `ProcessEnrichmentCommand`, `ProcessEnrichmentCommandHandler` and `ProcessEnrichmentOutcome`, plus a validator if the pipeline convention requires one (D5), and `EnrichmentRetryPolicy.cs` with the behaviour-preserving edit to `RecordEnrichmentFailureCommandHandler.cs:39-45` (D7). No other Core enrichment file is edited unless `plan.md` justifies it by file:line.
- `src/LamuFlix.Core/Options/RabbitMqOptions.cs`: add `RetryDelay` and `Prefetch`, with validation. Core stays free of RabbitMQ and OpenTelemetry references.
- `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`: add messaging activity and tag names only if the constants they need do not already exist.
- `src/LamuFlix.Api`: wire the registration (`Program.cs` or the existing composition root) and add the `RabbitMq` section to the appsettings.
- `tests/LamuFlix.IntegrationTests`: the broker tests for AC1–AC4, and the D8 Healthy/Unhealthy readiness tests.
- `tests/LamuFlix.UnitTests`: `ProcessEnrichmentCommandHandler` over fake ports, covering a false claim, success, each classified failure path and cancellation. Also options validation, the pure outcome-to-routing mapping, the guarded-registration rule (each port present or absent), and the D6 cross-option validator (unset, zero or negative ClaimLease; TTL equal to ClaimLease; a rounding edge; the valid case). Any gap in the existing Core policy tests is extended there.
- `tests/LamuFlix.Tests.Common`: fixture changes only if a test needs them, for example exposing a connection helper.
- `docs/adr/ADR-0004.md` and `docs/adr/ADR-0005.md`.
- `CONTEXT.md`: glossary terms (retry queue, dead-letter queue, publisher confirm, traceparent) only if the file already has a glossary.

**Out of scope. Each is a follow-up, never a finding in this round:**

- implementing the sweeper (Q1);
- the OMDb (or any real) `IMetadataProvider` implementation (D5, Q13; already covered by DEV-303);
- production `IMovieRepository` and DbContext registration in Api (D6, Q14, DEV-301 Q11);
- a claim-release port member, or any claim-handoff change (DEV-299 Q1 and DEV-316 stay open);
- a transactional outbox;
- deleting or editing the retired `src/LamuFlix.Web`, `src/LamuFlix.Worker`, `src/LamuFlix.Data` or `tests/LamuFlix.Test` (D1);
- migrating or purging `task_queue`/`task_queue_dlq` on any broker (Q9);
- exponential backoff;
- an OpenTelemetry SDK, exporter or instrumentation package;
- a docker-compose file;
- mapping `/health/live` or `/health/ready`, or a Postgres health check (the Q15 shared-endpoint follow-up);
- schema changes. The sweeper would use the existing `MovieRecord` columns `LastAttemptAt`, `EnrichmentAttempts` and `Status`; the ADRs may cite them.

## Approach and task ordering

1. **Packages.** Bump `RabbitMQ.Client` to 7.2.2, add the `OpenTelemetry.Api` pin, and add the Infrastructure package references. The solution still builds because nothing in it uses the 6.x API.
2. **Options.** `RetryDelay` and `Prefetch` with validation and unit tests (test first).
3. **Topology and connection owner.** An integration test asserts the declared arguments on `rabbitmq:4.0.0`.
4. **Publisher.** Confirms, the mandatory flag, returns, and the propagation helper. Integration tests cover AC1 and AC4 (inject), including the path with no listener.
5. **Core processing handler (D5).** `ProcessEnrichmentCommandHandler` with unit tests first, over fake ports. It has no broker dependency and can run in parallel with steps 3 and 4.
6. **Consumer.** The async consumer, a per-message scope dispatching `ProcessEnrichmentCommand`, prefetch, outcome mapping, confirm before ack, malformed messages to the DLQ, and requeue on cancel. Integration tests cover AC3 and AC4 (extract and link), with a Core-port fake provider.
7. **DI registration and Api wiring,** with consumer activation gated on both ports (D5, D6) and the cross-option validator, plus the appsettings.
8. **ADR-0004 and ADR-0005.** These have no code dependency and can run in parallel with steps 3–7. ADR-0004 includes the D5 and D6 activation boundary and the D6 in-lease limitation.
9. **Gates and refactor.** All AC8 gates, then the refactor pass that brings complexity down to ≤6.

Ordering constraints:

- Step 3 comes before steps 4 and 6, because both call the one topology owner. (Corrected in analyze round 1: step 5 has no broker dependency.)
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
