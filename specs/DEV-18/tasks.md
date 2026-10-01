# Tasks: Resilient enrichment messaging on RabbitMQ.Client 7

**Input**: `spec.md`, `plan.md`, `brief.md` (D1-D10). Tests come first in each phase (tests-first, constitution IX). The sweeper is ADR-only: there are no sweeper implementation tasks. Paths are relative to the repo root.

## Phase 1: Packages (brief step 1)

- [X] T001 [P] In `Directory.Packages.props`, pin `RabbitMQ.Client` 6.8.1 -> 7.2.2, `OpenTelemetry.Api` 1.19.1, `OpenTelemetry` 1.19.1 (Patron Q18: the SDK is authorized solely for `Sdk.SetDefaultTextMapPropagator`), `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.12, `Microsoft.Extensions.Options` 10.0.12 and `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 (FR-030; D8a, D9a, D9b, D10)
- [X] T002 Add versionless `PackageReference`s to all six packages in T001 in `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`, then confirm `dotnet build` is green and `dotnet list package --vulnerable` is clean (depends on T001)
- [X] T003 [P] Verify `.specify/feature.json` points at `specs/DEV-18` (already set)

## Phase 2: Options (step 2) — US4

- [X] T004 [US4] Unit tests for `RabbitMqOptions.RetryDelay` (default 30 s, positive, integer-ms representable) and `Prefetch` (default 1, positive), and that the generated printing redacts `Password` (shows `***`), in `tests/LamuFlix.UnitTests/` (FR-023, FR-024, FR-036)
- [X] T005 [US4] Add `RetryDelay` and `Prefetch` to `src/LamuFlix.Core/Options/RabbitMqOptions.cs` with validation, and redact `Password` in the generated printing (override `PrintMembers` or `ToString`); confirm binding through `AddLamuFlixOptions` (depends on T004)

## Phase 3: Core processing handler (step 5, runs beside Phases 4-5) — US2

- [X] T006 [US2] Unit tests for `ProcessEnrichmentCommandHandler` over fake ports: refused claim reported as skipped, found, not found, each classified failure, a provider `Failed(category)` uses its own category, only exceptions are classified, the terminal path marks Failed and saves, the retry path writes nothing, cancellation never classified, null `ReleaseYear` lookup; `ProcessEnrichmentCommandValidator` cases (default `MovieId`; `Attempt` below the floor of 1) (FR-016 to FR-020, FR-035)
- [X] T007 [P] [US2] Add `ProcessEnrichmentCommand` and `ProcessEnrichmentOutcome` under `src/LamuFlix.Core/Features/Enrichment/`
- [X] T007a [P] [US2] Add `ProcessEnrichmentCommandValidator` under `src/LamuFlix.Infrastructure/Pipeline/`, alongside the existing pipeline decorators and matching the repo convention that FluentValidation validators for the `AddHandler` pipeline live in `Infrastructure/Pipeline`, not in Core
- [X] T008 [US2] Add `ProcessEnrichmentCommandHandler`, reusing `EnrichmentFailureClassifier.cs:10-17` and the decision in `RecordEnrichmentFailureCommandHandler.cs:39-45`; no second retry policy (depends on T006, T007, T009)
- [X] T009a [US2] Unit tests for `EnrichmentRetryPolicy` first: retry and delayed-retry categories, `NextAttempt = Attempt + 1`, null when terminal, the `MaxAttempts` boundary
- [X] T009 [US2] (depends on T009a) Extract `EnrichmentRetryPolicy.cs` (pure rule from `RecordEnrichmentFailureCommandHandler.cs:39-45`, D7), edit the Record handler to call it, and keep its existing tests green unchanged.

## Phase 4: Topology and connection owner (step 3) — US1, US2

- [X] T010a [US2] Integration test (AC6): a failed connection initialisation surfaces and does not stick, so a later call succeeds (bounded waits, for example a paused then unpaused container or equivalent); the owner disposes on shutdown
- [X] T010 [US2] Integration test on `rabbitmq:4.0.0`: exchange, three quorum queues, every argument, idempotent re-declare (FR-005 to FR-008)
- [X] T011 [US2] (FR-026) Add `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqConnectionOwner.cs` (lazy, async, faulted creation not cached, async dispose) (depends on T010a)
- [X] T012 [US2] Add `RabbitMqTopology.cs` with `EnsureDeclaredAsync` (depends on T010, T011)

## Phase 5: Publisher and propagation (step 4) — US1, US3

- [X] T013 [US3] Unit tests for `TraceContextCarrier`: `traceparent` always, `tracestate` only when present, FsCheck round trip keeps the trace ID, valid context with no listener (FR-027, FR-028)
- [X] T014 [US3] Add `TraceContextCarrier.cs` (depends on T013). The propagator is set in T025, not here
- [X] T015 [US1] Integration tests: confirmed routed publish; two concurrent publishes, each succeeding on its own channel (FR-004); nack, return and connection failure each throw; the return case asserts `EnqueueAsync` fails for an unroutable publish, driven by `PublishReturnException` from the awaited `BasicPublishAsync` (D9b); headers carry a valid `traceparent` with no `ActivityListener`; the ambient context is used when present (FR-001 to FR-004)
- [X] T016 [US1] Add `RabbitMqEnrichmentQueuePublisher.cs` implementing `IEnrichmentQueue`, with a Producer span `Enrichment.Enqueue` and attribute `lamuflix.movie.id` (depends on T012, T014, T015)

## Phase 6: Consumer (step 6) — US2, US3

- [X] T017 [P] [US2] Unit tests for `EnrichmentRouting`: `Completed` and skipped ack; `Retry` and `RetryDelayed` to `retry`; `DeadLetter` to `dead-letter` (D6, FR-013); a skipped (`Claimed` false) outcome writes a distinct skipped log and outcome tag, never enriched
- [X] T018 [US2] Add `EnrichmentRouting.cs` (depends on T017)
- [X] T019 [US2] Lease-aware test fake repository (DEV-301 FR-002: Pending, strict expiry, claim stamps `LastAttemptAt` and increments attempts, over a test `TimeProvider`) and fake `IMetadataProvider` in test code; no always-true claim
- [X] T020 [US2] Integration tests with the real handler: `Retry` and `RetryDelayed` each return after the TTL and reclaim (ClaimLease 500 ms, RetryDelay 1 s); terminal outcome and malformed body reach `enrichment.dead-letter`; no success ack after a failed republish; cancellation requeues (depends on T019). Also:
  - (a) a failed republish (for example the retry queue at `reject-publish` capacity, or a closed connection) leaves the original in `enrichment.dead-letter`, never success-acked, and the movie is not marked Failed (FR-015);
  - (b) cancellation does not mark the movie Failed;
  - (c) prefetch: with `Prefetch = 1` and the first handler held, the second message stays Ready, checked by a passive-declare message count with bounded waits (FR-009)
- [X] T021 [US3] Integration tests: consumer activity shares the producer trace ID; link on redelivery
- [X] T022 [US2] (FR-009 to FR-012, FR-014, FR-015, FR-029) Add `EnrichmentConsumer.cs` (`BackgroundService`, per-message `IServiceScope`, confirm-before-ack, a failed or uncertain republish or unexpected exception -> `BasicNack(requeue: false)`, prefetch from options; span `Enrichment.Process` with `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count`, `error.type`; every processing log carries movie id, attempt and category where one applies; no metrics) (depends on T008, T012, T016, T018, T020, T021)

## Phase 7: Registration, guard and Api wiring (step 7) — US4

- [X] T023 [US4] Unit tests: guard with each of `IMetadataProvider` and `IMovieRepository` present or absent, exactly one inactive log line from a hosted service that logs once in `StartAsync`, `ValidateOnBuild` and `ValidateScopes` still on; validator cases (unset, zero, negative lease; TTL equal to lease; rounding edge; valid); the health check is registered with the `ready` tag in BOTH the active and inactive paths; the consumer resolves the `AddHandler`-composed handler, whose outermost registration is the tracing decorator (FR-010) (FR-021 to FR-025, FR-033)
- [X] T024a [US4] Integration test on the fixture: the check is Healthy with the broker up and Unhealthy with an unreachable broker (bounded waits); the description carries no credentials (FR-033)
- [X] T024b [US4] Add `RabbitMqHealthCheck.cs` (depends on T011, T024a)
- [X] T024 [US4] Add `RabbitMqConsumerOptionsValidator.cs` (active path only) (depends on T023)
- [X] T025 [US4] Add `RabbitMqServiceCollectionExtensions.cs` (always: owner, topology, publisher; sets the default text-map propagator to `TraceContextPropagator` through `Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator())` before first use — the sole authorized use of the `OpenTelemetry` SDK that Patron added in Q18, because `OpenTelemetry.Api` exposes no public setter; guarded: handler via `AddHandler`, consumer, validator; inactive path: the hosted service that logs once, declared in this file; always path: `AddHealthChecks().AddCheck<RabbitMqHealthCheck>(..., tags: ready)`, with a one-line code comment naming the other half, `AddHealthChecks()` in `ServiceDefaults`) (depends on T022, T023, T024, T024b)
- [X] T025a [P] [US4] Add `AddHealthChecks()` to `src/LamuFlix.ServiceDefaults/Extensions.cs` `AddServiceDefaults`; the Api does not duplicate it, with a one-line code comment naming the other half, the `AddCheck` registration in `RabbitMqServiceCollectionExtensions` (FR-034)
- [X] T026 [US4] Call it from `src/LamuFlix.Api/Program.cs` after the port registrations and add a secret-free `RabbitMq` section to `appsettings.json`; do not wire a production repository or provider (depends on T025)

## Phase 8: Decision records (step 8, parallel with Phases 3-7) — US5

- [X] T027 [P] [US5] Review `docs/adr/ADR-0004.md` (drafted by Keel, `c9cba46`) against the implementation and update it if the code diverges. It must cover: topology, retry, DLQ and sweeper design, client-7 model, confirms, tracing, no-migration cutover, activation boundary, the in-lease crash-redelivery limitation; states DEV-18 does not implement the sweeper (FR-031)
- [X] T028 [P] [US5] Review `docs/adr/ADR-0005.md` (drafted by Keel, `2d8d37d`) against the implementation and update it if the code diverges. It must cover: dual-write mitigation, sweeper now, outbox stretch; same statement (FR-031)
- [X] T029 [P] [US5] Add glossary terms to `CONTEXT.md` (glossary at `CONTEXT.md:44-48`; unconditional, constitution VIII)

## Phase 9: Gates and refactor (step 9)

- [X] T030 Run `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` and `./scripts/run-jetbrains-inspectcode.ps1`
- [X] T031 Refactor pass: `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`
- [X] T032 Run `dotnet format --verify-no-changes`, full `dotnet test`, the vulnerable-package scan, the mutation gate (at least 80%) and the architecture tests; record any skipped gate as SKIP, never PASS (FR-032)

## Dependencies

- T001 → T002. Phase 2 needs T002. Phases 3, 4 and 8 can start after T002 and run in parallel.
- Phase 5 needs T012. Phase 6 needs Phases 3, 4 and 5. Phase 7 needs Phase 6.
- Phase 9 runs last. Sweeper, outbox, OMDb provider, production repository wiring, shared health endpoints and the retired projects are out of scope.

