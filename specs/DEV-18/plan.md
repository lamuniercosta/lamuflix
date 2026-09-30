# Implementation Plan: Resilient enrichment messaging on RabbitMQ.Client 7

**Branch**: `feature/018-spec` | **Date**: 2026-09-30 | **Spec**: [spec.md](spec.md)

**Input**: `specs/DEV-18/spec.md`, `brief.md` (D1-D9, AC1-AC8), `CONCLUSIONS.md` (Q1-Q16), notes `recon-DEV-18`, `-2`, `-3`, `-4`, `recon-DEV-18-5`

## Summary

`IEnrichmentQueue` has no implementation, and nothing consumes the queue, in the built solution. The RabbitMQ code that exists lives in projects DEV-19 retired from the solution, so DEV-18 writes new code and deletes none (D1). The work has four parts:

- Upgrade `RabbitMQ.Client` to 7.2.2 and add `OpenTelemetry.Api`.
- Add `RabbitMq/` to Infrastructure: one topology owner, one connection owner, a confirming publisher, a consumer `BackgroundService`, an outcome-to-routing-key mapping, a header propagation helper and a DI extension.
- Add a thin Core processing handler, `ProcessEnrichmentCommandHandler`, so the consumer carries transport only.
- Write ADR-0004 and ADR-0005.

The consumer and the processing handler are registered only when an `IMetadataProvider` and an `IMovieRepository` exist (D5, D6). Neither has a production implementation yet, so in the Api as it stands the consumer is inactive and messages wait in `enrichment.requested`. The tests register Core-port fakes for both.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (`net10.0` in every project).

**Primary Dependencies**:

- `RabbitMQ.Client` 7.2.2 (netstandard2.0 and net8.0 targets; `Directory.Packages.props:9` moves from 6.8.1);
- `OpenTelemetry.Api`, the latest stable 1.x that targets net8.0 or later. Wisp confirms the exact version at implementation and it is recorded in `Directory.Packages.props` (D4);
- `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.12, central pin next to the `Microsoft.Extensions.*` 10.0.12 pins (`Directory.Packages.props:18-19`), versionless `PackageReference` in Infrastructure; no separate Abstractions reference, no `Microsoft.AspNetCore.App` in Infrastructure (D8a, Q16);
- existing: FluentValidation, `Microsoft.Extensions.*`, Ardalis.SmartEnum.

**Storage**: none new. RabbitMQ queues only. No schema change. The sweeper design (ADRs only) would use the existing `MovieRecord` columns `LastAttemptAt`, `EnrichmentAttempts` and `Status`.

**Testing**: xUnit v3, Shouldly, NSubstitute, FsCheck. Unit tests cover Core, options and the pure mapping. Integration tests (`tests/LamuFlix.IntegrationTests`) use the `rabbitmq:4.0.0` container in `tests/LamuFlix.Tests.Common/ContainerFixture.cs`. No RabbitMQ mocks cover routing, TTL, dead-lettering, confirms or serialization.

**Target Platform**: the Api host (Windows dev, Linux-compatible).

**Project Type**: existing solution: Core, Infrastructure, ServiceDefaults, Api. No new project.

**Performance Goals**: none. Prefetch defaults to 1, as today.

**Constraints**:

- Core references neither RabbitMQ nor OpenTelemetry (`ArchitectureTests.cs:47-49,59-63`).
- Complexity at most 15, then at most 6 after refactor.
- Mutation at least 80%.
- `ValidateOnBuild` and `ValidateScopes` are never disabled.
- No secrets, hosts or passwords hardcoded.

**Scale/Scope**: about 8 new Infrastructure files, 3 new Core files, 1 option record edit, appsettings, about 6 test files, 2 ADRs.

## Constitution Check

| Principle / rule | Status |
|---|---|
| I. Ports and adapters: Core defines ports and records, Infrastructure implements | **Aligned.** The publisher implements `IEnrichmentQueue`. The consumer, topology and connection live in `Infrastructure/RabbitMq/`. The Api only hosts. |
| II. Explicit handlers and decorators | **Aligned.** The consumer resolves `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>` from a per-message scope. It is registered through `AddHandler` (`Infrastructure/Pipeline/ServiceCollectionExtensions.cs:14-35`), which composes the decorators. Execution order is Tracing -> Logging -> Validation -> handler, as constitution II:135 states (`:50-64`). |
| IV. Enrichment is an explicit state machine; decisions live in Core handlers (ADR-0017) | **Aligned.** The attempt policy stays in `RecordEnrichmentFailureCommandHandler.cs:39-45`. The new handler adds no retry policy. The consumer maps an action to a routing key and nothing else. |
| VI. Observability across processes | **Aligned.** Producer and consumer activities on the `LamuFlix` source, W3C inject and extract, a link on redelivery, names from `TelemetryConstants`. |
| VII. Configuration isolation and deterministic time | **Aligned.** Options are bound through `ServiceDefaults.AddLamuFlixOptions` (`Extensions.cs:18-27`). The handler and tests use `TimeProvider`. |
| IX. Test pyramid with real infrastructure | **Aligned.** Real broker and real consumer. Fakes only at the Core ports for provider and repository, in place of WireMock and Postgres (IX:299-300), on the basis of Q13 and Q14. |
| Enrichment reliability rules | **Aligned with a declared limit.** Confirm before ack, `requeue:false` for poison, a failed or uncertain republish and any unexpected exception (dead-lettered, movie not marked Failed by the consumer), requeue on cancel. In-lease redelivery (crash or graceful shutdown) may be refused and acked, and a dead-lettered failure leaves the movie Pending; ADR-0004 states both depend on the deferred sweeper. |
| New dependency needs a ruling | **Met.** `RabbitMQ.Client` bump and `OpenTelemetry.Api` (Q3, D4). |
| Schema change, new project, new layer | **None.** |

No violations, so Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-18/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── checklists/
    └── requirements.md
docs/adr/
├── ADR-0004.md   (new)
└── ADR-0005.md   (new)
```

### Source Code (repository root)

```text
Directory.Packages.props                                   # RabbitMQ.Client 7.2.2; add OpenTelemetry.Api and HealthChecks pins
src/LamuFlix.Core/
├── Options/RabbitMqOptions.cs                             # + RetryDelay, Prefetch
├── Pipeline/TelemetryConstants.cs                         # + messaging names, only those missing (plain const string)
└── Features/Enrichment/
    ├── ProcessEnrichmentCommand.cs                        # new
    ├── ProcessEnrichmentCommandValidator.cs               # new, FluentValidation (D5, D9 Risk F4)
    ├── ProcessEnrichmentCommandHandler.cs                 # new
    ├── ProcessEnrichmentOutcome.cs                        # new
    ├── EnrichmentRetryPolicy.cs                           # new, pure rule extracted (D7)
    └── RecordEnrichmentFailureCommandHandler.cs           # edit: behaviour-preserving, calls the policy (D7)
src/LamuFlix.Infrastructure/
├── LamuFlix.Infrastructure.csproj                         # + RabbitMQ.Client, OpenTelemetry.Api
└── RabbitMq/                                              # new folder
    ├── RabbitMqTopology.cs
    ├── RabbitMqConnectionOwner.cs
    ├── RabbitMqEnrichmentQueuePublisher.cs
    ├── EnrichmentConsumer.cs                              # BackgroundService
    ├── EnrichmentRouting.cs                               # outcome -> routing key (pure)
    ├── TraceContextCarrier.cs                             # header inject / extract (pure)
    ├── RabbitMqHealthCheck.cs                             # new, readiness over the connection owner (D8)
    ├── RabbitMqConsumerOptionsValidator.cs                # IValidateOptions, D6
    └── RabbitMqServiceCollectionExtensions.cs             # registration and guard
src/LamuFlix.ServiceDefaults/
└── Extensions.cs                                          # edit: AddHealthChecks() only (Patron file-scope ruling)
src/LamuFlix.Api/
├── Program.cs                                             # call the registration
└── appsettings.json                                       # RabbitMq section, no secrets
tests/LamuFlix.UnitTests/                                  # handler, options, routing, carrier, guard, validator
tests/LamuFlix.IntegrationTests/                           # topology, publisher, consumer, tracing
tests/LamuFlix.Tests.Common/                               # only if a test needs a connection helper
```

**Structure Decision**: the existing four-project layout. `RabbitMq` casing is the repo convention (it matches `RabbitMqOptions`). Names are final here (brief: Quill finalizes file names in `plan.md`). Core holds the records, ports and the processing handler. Infrastructure holds every RabbitMQ and OpenTelemetry type. The Api hosts the consumer and adds no code besides the registration call and settings. `CONTEXT.md` gets glossary terms (retry queue, dead-letter queue, publisher confirm, traceparent) unconditionally, because it already has a glossary (`CONTEXT.md:44-48`, constitution VIII:277).

## Design

Plan D-n numbers are the plan's own; brief decisions are cited as Dn.

### D-1. Packages (D4; FR-030)

- `Directory.Packages.props`: `RabbitMQ.Client` 6.8.1 → 7.2.2 (line 9). Add an `OpenTelemetry.Api` pin and a `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.12 pin.
- `LamuFlix.Infrastructure.csproj`: add all three `PackageReference`s (HealthChecks versionless). `Testcontainers.RabbitMq` 4.15.0 needs `RabbitMQ.Client >= 6.8.1`, which 7.2.2 meets.
- `Tests.Common` keeps its existing `RabbitMQ.Client` reference. The integration tests use it as the test consumer and reader. Nothing else references either package.
- The solution still builds after this step, because nothing built uses the 6.x API.

### D-2. Options (AC5; FR-023, FR-024, FR-025)

- `RabbitMqOptions`: add `RetryDelay` (`TimeSpan`, default 30 s, `[Range]`-style positive check, must convert to an `int` millisecond TTL) and `Prefetch` (`ushort`, default 1, positive). The validation is a data-annotations or `IValidatableObject` rule, so `AddLamuFlixOptions` (`Extensions.cs:23`) picks it up with `ValidateOnStart`.
- `RabbitMqOptions` is a `sealed record` with `Password`, so it redacts `Password` in its generated printing: override `PrintMembers` (or `ToString`) so the output shows `Password = ***` (D9 Risk F3, FR-036). Nothing logs `RabbitMqOptions` as a whole.
- `MaxAttempts` stays in `EnrichmentOptions` (`EnrichmentOptions.cs:13`). `ClaimLease` (`:17`) has no default and keeps none.
- The D6 cross-option check is Infrastructure's `RabbitMqConsumerOptionsValidator : IValidateOptions<...>`. It is registered only on the active path (D-7):
  - `ClaimLease` explicitly set and greater than zero (an unset value is `TimeSpan.Zero`, so zero is the unset signal and is rejected);
  - the TTL is `RetryDelay` converted to whole milliseconds, positive and representable as `int`;
  - that converted TTL is strictly greater than `ClaimLease`, compared in milliseconds so rounding cannot erase the margin.
- Validation failures name the option. The value is never adjusted.

### D-3. Topology and connection (AC2, AC6; FR-005 to FR-008, FR-026)

- `RabbitMqConnectionOwner`: one `IConnection` per process, created lazily and asynchronously. A faulted creation is not cached (the next caller retries). It is `IAsyncDisposable`, and its creation honours the cancellation token. Connection settings come from `IOptions<RabbitMqOptions>`; there is no raw `IConfiguration` read and no hardcoded host or secret.
- `RabbitMqTopology`: one idempotent `EnsureDeclaredAsync`, awaited by the publisher on first use and by the consumer at startup. It declares:

| Name | Bound by | Arguments |
|---|---|---|
| `lamuflix.enrichment` | durable direct exchange | none |
| `enrichment.requested` | `requested` | quorum; `x-dead-letter-exchange` `lamuflix.enrichment`; `x-dead-letter-routing-key` `dead-letter`; `x-delivery-limit` = `EnrichmentOptions.MaxAttempts`; `x-dead-letter-strategy` `at-least-once`; `x-overflow` `reject-publish` |
| `enrichment.retry` | `retry` | quorum; `x-message-ttl` = `RetryDelay` in ms; dead-letters to `lamuflix.enrichment` with key `requested`; `at-least-once`; `reject-publish` |
| `enrichment.dead-letter` | `dead-letter` | quorum; no automatic retry |

- Quorum at-least-once dead-lettering needs the broker's `stream_queue` feature flag. The integration test on `rabbitmq:4.0.0` asserts the declared queue type and arguments against the real broker, which also proves that prerequisite.

### D-4. Publisher and propagation (AC1, AC4; FR-001 to FR-004, FR-027, FR-028)

- `RabbitMqEnrichmentQueuePublisher : IEnrichmentQueue` serializes `EnrichmentRequested` (System.Text.Json) and publishes persistent, mandatory messages to `lamuflix.enrichment` with key `requested`. Each call creates a channel with `publisherConfirmationsEnabled` and `publisherConfirmationTrackingEnabled`, and disposes it.
- A nack, a return (unroutable) or a connection failure surfaces as an exception. A per-publish returned flag is set from the return event (or the client surfaces a return as an exception under confirmation tracking). The publisher awaits the confirm, then checks the flag, and disposes the channel only after both have settled. A confirm timeout or cancellation is uncertain and fails the call (Q10; D9 Risk F2).
- The republish path in the consumer calls the same publish method with a different key (and the next attempt). There is no second publish implementation.
- `TraceContextCarrier` (pure): injects `traceparent` and, when present, `tracestate` into `BasicProperties.Headers`, and extracts them.
- The propagator: `Propagators.DefaultTextMapPropagator` is set to the API's `TraceContextPropagator` before first use (it is a no-op until configured). This happens in the registration extension (D-7, D7), not in the carrier and not in a second telemetry stack. Caveat: the assignment is idempotent (the same `TraceContextPropagator` each time), so tests must not set a different global propagator (D9 Risk F5, note only).
- The publisher starts an `ActivityKind.Producer` span named `Enrichment.Enqueue` from the `LamuFlix` source (`TelemetryConstants.cs:5`, `ActivitySourceName`), with attribute `lamuflix.movie.id`. If `StartActivity` returns null because no listener exists, the publisher injects `Activity.Current` if there is one, otherwise a newly created W3C context. The ordinary path never depends on a test listener.

### D-5. Core processing handler (D5; FR-017 to FR-020)

- `ProcessEnrichmentCommand(MovieId Id, int Attempt)` (sealed record, matching `RecordEnrichmentFailureCommand.cs:5`).
- `ProcessEnrichmentOutcome` (sealed; `Completed`, or the existing `EnrichmentFailureDecision` at `Domain/EnrichmentFailureDecision.cs:3`). `Completed` carries whether the movie was claimed, so a refused claim is reported as skipped and never as success (plan-level shape; no new decision).
- `ProcessEnrichmentCommandHandler : ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`, sealed, depending on `IMovieRepository`, `IMetadataProvider`, `EnrichmentOptions`, `TimeProvider` and `ILogger`. Order:
  1. `IMovieRepository.TryClaimForEnrichmentAsync` (`IMovieRepository.cs:21`). If false, return `Completed` (skipped).
  2. `IMovieRepository.GetAsync`, and `NotFoundException` if absent (as `ApplyEnrichmentResultCommandHandler.cs` does).
  3. `IMetadataProvider.FindAsync(new MetadataLookup(movie.Title, movie.Metadata?.ReleaseYear), ct)` (`MetadataLookup.cs`; the year is null before the first enrichment, `Movie.cs:29`).
  4. `Found` or `NotFound`: apply it on the movie (`MarkEnriched` or `MarkNotFound`, `Movie.cs:51,60`), save, return `Completed` with `Claimed` true. A `Failed(category)` provider result uses its own category. Only an exception goes through `EnrichmentFailureClassifier.Classify` (`Features/Enrichment/EnrichmentFailureClassifier.cs:10-17`, the single classifier). The category, the attempt and `MaxAttempts` then go to `EnrichmentRetryPolicy`:
     - a retry decision is returned as the outcome and **nothing is written**, because the claim already stamped the attempt (D7);
     - on the terminal path the handler calls `MarkFailed(category, time.GetUtcNow())` on the loaded movie, then `SaveChangesAsync`, and returns `DeadLetter` with a null `NextAttempt`, as `RecordEnrichmentFailureCommandHandler.cs:30-35` does (D7).
- The handler composes the existing handlers' logic through ports and never calls another handler. A second copy of the retry decision would be a second policy (ADR-0017, D5), so the rule is extracted (required by D7): `EnrichmentRetryPolicy.cs` takes the pure rule from `RecordEnrichmentFailureCommandHandler.cs:39-45` (`IsRetry`, `RetryAction`). Its one function takes the category, the attempt and `MaxAttempts`, and returns the retry `EnrichmentFailureDecision` (`Retry` or `RetryDelayed`, `NextAttempt = Attempt + 1`) or null when the path is terminal. It is `internal static` if `LamuFlix.UnitTests` sees internals, otherwise `public static`. Both handlers call it. The edit to the Record handler replaces two private members, preserves behaviour, and its existing tests stay green unchanged. This is the only Core addition beyond the D5 files.
- Cancellation: `EnrichmentFailureClassifier` rethrows a cancellation (`EnrichmentFailureClassifier.cs:92-102`), so the handler lets an `OperationCanceledException` tied to its own token propagate. It is never turned into a failure.
- `Claimed` names what happened: false means the claim was refused and the message is skipped.
- `ProcessEnrichmentCommandValidator` (FluentValidation, beside the command) is required (D9 Risk F4, FR-035): `MovieId` is not the default value, and `Attempt` is at or above the floor of 1 that the existing enqueue handlers use: `RequestEnrichmentCommandHandler.cs:24`, `RequeueStrandedMoviesCommandHandler.cs:16` and `ImportMovieFolderCommandHandler.cs:22` each enqueue `new EnrichmentRequested(id, 1)`. A validation failure is an exception and takes the D-6 dead-letter path.

### D-6. Consumer (AC3, AC4; FR-009 to FR-016, FR-029)

- `EnrichmentConsumer : BackgroundService`, on `AsyncEventingBasicConsumer` over an `IChannel`, `BasicQos` from `RabbitMqOptions.Prefetch`, manual acks. It awaits `RabbitMqTopology` at startup.
- Per message: extract the trace context and start an `ActivityKind.Consumer` span named `Enrichment.Process` parented to it (a link on redelivery), with attributes `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count` and, on failure, `error.type`; deserialize; open an `IServiceScope`; resolve `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`; dispatch.
- `EnrichmentRouting` (pure) maps the outcome: `Completed` → ack; skipped claim → ack plus a skipped log; `Retry` and `RetryDelayed` → `retry` with `NextAttempt`; `DeadLetter` → `dead-letter`. Both retry actions use the TTL queue so a retry cannot arrive before the lease can expire (D6).
- Republish goes through the publisher. Ack the original only after the republish is confirmed and routed. On a failed or uncertain republish: no success ack.
- Malformed body: `BasicNack(requeue: false)` so the dead-letter exchange routes it to `enrichment.dead-letter`.
- Cancellation: requeue (`BasicNack(requeue: true)`). After a failed or uncertain republish, and after any other exception (an unclassified exception or a validation failure), `BasicNack(requeue: false)`: the dead-letter exchange routes the original to `enrichment.dead-letter`, and the consumer does not mark the movie Failed (it stays as the handler left it: Pending on a retry path, already Failed on the terminal path). Cancellation is the only `requeue: true` case (D9 Risk F1, FR-015).
- Telemetry names (`Enrichment.Enqueue`, `Enrichment.Process`, `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count`, `error.type`) come from `TelemetryConstants`; add only the ones that are missing (constitution VI:225-231). The additions are plain `const string`, with no OpenTelemetry type or using in Core; the existing Core-must-not-reference-OpenTelemetry architecture test covers them. Every processing log carries the movie id, the attempt and, where one applies, the category. A skipped claim writes a distinct skipped log event and sets the outcome tag to skipped, never enriched. No metric instruments.

### D-7. Registration and Api wiring (D5, D6; FR-021, FR-022, FR-024)

- `RabbitMqServiceCollectionExtensions.AddLamuFlixRabbitMq(...)` always registers the connection owner, topology and publisher (as `IEnrichmentQueue`), sets the propagator, and registers `RabbitMqHealthCheck` with the `ready` tag on this always path, not behind the D5/D6 guard (D-9).
- It then checks the `IServiceCollection` for both an `IMetadataProvider` and an `IMovieRepository` descriptor. **The call must come after both registrations.** If either is absent, it registers neither the consumer nor the handler. No logger exists during registration, so it registers a small hosted service, declared in `RabbitMqServiceCollectionExtensions.cs` (no new file), that logs the "consumer inactive" line once in `StartAsync`. A unit test asserts exactly one line.
- If both are present, it registers the handler with `AddHandler<ProcessEnrichmentCommandHandler, ProcessEnrichmentCommand, ProcessEnrichmentOutcome>()`, the consumer as a hosted service, and the cross-option validator with `ValidateOnStart`.
- Supporting options, `TimeProvider` and logging must be resolvable on the active path. `ValidateOnBuild` and `ValidateScopes` are never disabled.
- `Program.cs` (`src/LamuFlix.Api/Program.cs:5-10`) calls the extension. `appsettings.json` gains a `RabbitMq` section with no secrets; the password comes from user-secrets or the environment.
- Production wiring of `IMovieRepository` and the DbContext, and the OMDb `IMetadataProvider`, are not in this ticket. Until one of each exists, the Api runs with the consumer inactive.

### D-8. ADRs (AC7; FR-031)

- `docs/adr/ADR-0004.md`: topology, retry queue, DLQ and sweeper design. Context and consequences also cover the client-7 async model, publisher confirms, trace propagation, the cutover with no migration of old queues, the activation boundary (inactive until both ports exist), and the limitation that crash redelivery or competing work inside a lease may be refused and acked, so recovery depends on the deferred sweeper.
- `docs/adr/ADR-0005.md`: dual-write mitigation, sweeper now and a transactional outbox as a stretch.
- Both are `Status: Accepted` (the merge is the acceptance) and both state that DEV-18 records the sweeper design without implementing it. Existing ADRs use the same status (`ADR-0006.md:3`, `ADR-0010.md:3`).

### D-9. Readiness check (D8, D8a; FR-033, FR-034)

- `RabbitMqHealthCheck : IHealthCheck` reuses the shared connection owner and the cancellation token it is given. Its connection attempt is bounded by that token and by the connection factory's `RequestedConnectionTimeout`, with no retry without a bound. It never publishes or consumes. On a connection failure it returns Unhealthy with a fixed description: no credentials, no raw exception text.
- It is registered in `RabbitMqServiceCollectionExtensions` on the always path: `AddHealthChecks().AddCheck<RabbitMqHealthCheck>(..., tags: ready)`. It is active whether or not the consumer is.
- `ServiceDefaults.AddServiceDefaults` gains one `AddHealthChecks()` call, and the Api does not duplicate it. This edit is Patron's file-scope ruling (Q15, Q16).
- The `/health/live` and `/health/ready` endpoints stay unmapped (pre-existing gap, Q15 follow-up). The check is registered, not reachable over HTTP, and constitution VI endpoint alignment is not claimed.

## Test Strategy

- **Unit (pure, no broker)**: `ProcessEnrichmentCommandHandler` over fake ports (refused claim, found, not found, each classified failure, a provider `Failed(category)` using its own category, cancellation; the terminal path marks Failed and saves, the retry path writes nothing); `EnrichmentRetryPolicy`; `EnrichmentRouting`; `TraceContextCarrier` (with an FsCheck round trip that preserves the trace ID); options validation; the guard with each port present or absent; the cross-option validator (unset, zero, negative lease; TTL equal to lease; a rounding edge; the valid case).
- **Integration (real broker on `rabbitmq:4.0.0`)**, in `tests/LamuFlix.IntegrationTests`:
  - topology: queue type and every argument;
  - connection owner (AC6): a failed connection initialisation surfaces and does not stick, so a later call succeeds (bounded waits, for example a paused then unpaused container or equivalent), and the owner disposes on shutdown;
  - publisher: a confirmed routed publish, and a returned or failing publish; headers carry a valid `traceparent` with no `ActivityListener`, and the ambient context is used when present;
  - consumer, with the real handler and a **lease-aware fake repository** and a fake provider at the Core ports:
    - the fake implements DEV-301 FR-002: Pending status, strict expiry (`LastAttemptAt == null || LastAttemptAt < now - ClaimLease`), and a successful claim stamps `LastAttemptAt` and increments attempts, over a test `TimeProvider`. No always-true claim fake is allowed;
    - a `Retry` outcome and a `RetryDelayed` outcome (for example a rate-limited category) each return after the TTL, reclaim the movie and call the provider again, with a `ClaimLease` (for example 500 ms) below the test `RetryDelay` (for example 1 s);
    - a terminal or non-retryable outcome and a malformed body each land in `enrichment.dead-letter`;
    - no success ack after a failed or uncertain republish;
  - tracing: a valid `traceparent` with no listener; the consumer activity shares the producer's trace ID with a listener; the redelivery link.
- Waits are bounded and broker-driven (`BasicGetAsync` or a consumer with a timeout), with no unbounded sleeps. No new framework and no new test project.

## Ordering and Risks

Ordering follows `brief.md` (9 steps; task IDs are in `tasks.md`):

1. Packages.
2. Options (test first).
3. Topology and connection owner (before 4 and 6).
4. Publisher and propagation (before 6's republish path).
5. Core processing handler, unit tests first. No broker dependency, so it can run beside 3 and 4. It comes before 6.
6. Consumer.
7. DI registration, guard, validator and Api wiring.
8. ADR-0004 and ADR-0005, in parallel with 3-7.
9. Gates, then the refactor pass down to complexity 6.

Risks:

- **Attempt policy duplication.** D-5 must not copy the retry decision. `EnrichmentRetryPolicy` is extracted (D7), and the existing Record handler tests must stay green unchanged.
- **Lease versus delay.** A retry arriving inside the lease would be refused and acked, losing the retry. The D6 validator and the lease-aware integration fake exist to prevent this.
- **In-lease crash redelivery** stays a documented limitation that depends on the deferred sweeper.
- **Duplicates.** A crash between a confirmed republish and the ack duplicates a message. Exactly-once is not claimed.
- **Quorum feature flag.** At-least-once dead-lettering needs `stream_queue`. The real-broker topology test proves it on the pinned image.
- **Mutation coverage of Infrastructure.** DEV-382 receipts show Infrastructure is measurable; the new files must be covered by unit tests, not only by the container-backed integration tests.

## Round Caps

Analyze: 2 rounds. Review: 2 rounds, at most 2 fix commits per round. Blocking bar: Critical or High with a concrete failure scenario. Caps are hard.
