# Tasks: Resilient enrichment messaging on RabbitMQ.Client 7

**Input**: `spec.md`, `plan.md`, `brief.md` (D1-D6). Tests come first in each phase (tests-first, constitution IX). The sweeper is ADR-only: there are no sweeper implementation tasks. Paths are relative to the repo root.

## Phase 1: Packages (brief step 1)

- [ ] T001 [P] Bump `RabbitMQ.Client` to 7.2.2 and add the `OpenTelemetry.Api` pin in `Directory.Packages.props` (FR-030)
- [ ] T002 Add both `PackageReference`s to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`, then confirm `dotnet build` is green and `dotnet list package --vulnerable` is clean (depends on T001)
- [ ] T003 [P] Verify `.specify/feature.json` points at `specs/DEV-18` (already set)

## Phase 2: Options (step 2) — US4

- [ ] T004 [US4] Unit tests for `RabbitMqOptions.RetryDelay` (default 30 s, positive, integer-ms representable) and `Prefetch` (default 1, positive) in `tests/LamuFlix.UnitTests/` (FR-023, FR-024)
- [ ] T005 [US4] Add `RetryDelay` and `Prefetch` to `src/LamuFlix.Core/Options/RabbitMqOptions.cs` with validation; confirm binding through `AddLamuFlixOptions` (depends on T004)

## Phase 3: Core processing handler (step 5, runs beside Phases 4-5) — US2

- [ ] T006 [US2] Unit tests for `ProcessEnrichmentCommandHandler` over fake ports: refused claim reported as skipped, found, not found, each classified failure, cancellation never classified, null `ReleaseYear` lookup (FR-017 to FR-020)
- [ ] T007 [P] [US2] Add `ProcessEnrichmentCommand` and `ProcessEnrichmentOutcome` under `src/LamuFlix.Core/Features/Enrichment/`
- [ ] T008 [US2] Add `ProcessEnrichmentCommandHandler`, reusing `EnrichmentFailureClassifier.cs:10-17` and the decision in `RecordEnrichmentFailureCommandHandler.cs:39-45`; no second retry policy (depends on T006, T007)
- [ ] T009 [US2] If the decision is extracted to a shared Core function, justify it with file:line in the change and keep the existing handler tests green (depends on T008)

## Phase 4: Topology and connection owner (step 3) — US1, US2

- [ ] T010 [US2] Integration test on `rabbitmq:4.0.0`: exchange, three quorum queues, every argument, idempotent re-declare (FR-005 to FR-008)
- [ ] T011 [US2] Add `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqConnectionOwner.cs` (lazy, async, faulted creation not cached, async dispose)
- [ ] T012 [US2] Add `RabbitMqTopology.cs` with `EnsureDeclaredAsync` (depends on T010, T011)

## Phase 5: Publisher and propagation (step 4) — US1, US3

- [ ] T013 [US3] Unit tests for `TraceContextCarrier`: `traceparent` always, `tracestate` only when present, FsCheck round trip keeps the trace ID, valid context with no listener (FR-027, FR-028)
- [ ] T014 [US3] Add `TraceContextCarrier.cs`; set `Propagators.DefaultTextMapPropagator` to `TraceContextPropagator` (depends on T013)
- [ ] T015 [US1] Integration tests: confirmed routed publish; nack, return and connection failure each throw (FR-001 to FR-004)
- [ ] T016 [US1] Add `RabbitMqEnrichmentQueuePublisher.cs` implementing `IEnrichmentQueue`, with a Producer activity (depends on T012, T014, T015)

## Phase 6: Consumer (step 6) — US2, US3

- [ ] T017 [P] [US2] Unit tests for `EnrichmentRouting`: `Completed` and skipped ack; `Retry` and `RetryDelayed` to `retry`; `DeadLetter` to `dead-letter` (D6, FR-013)
- [ ] T018 [US2] Add `EnrichmentRouting.cs` (depends on T017)
- [ ] T019 [US2] Lease-aware test fake repository (DEV-301 FR-002: Pending, strict expiry, claim stamps `LastAttemptAt` and increments attempts, over a test `TimeProvider`) and fake `IMetadataProvider` in test code; no always-true claim
- [ ] T020 [US2] Integration tests with the real handler: `Retry` and `RetryDelayed` each return after the TTL and reclaim (ClaimLease 500 ms, RetryDelay 1 s); terminal outcome and malformed body reach `enrichment.dead-letter`; no success ack after a failed republish; cancellation requeues (depends on T019)
- [ ] T021 [US3] Integration tests: consumer activity shares the producer trace ID; link on redelivery
- [ ] T022 [US2] Add `EnrichmentConsumer.cs` (`BackgroundService`, per-message `IServiceScope`, confirm-before-ack, prefetch from options) (depends on T008, T012, T016, T018, T020, T021)

## Phase 7: Registration, guard and Api wiring (step 7) — US4

- [ ] T023 [US4] Unit tests: guard with each of `IMetadataProvider` and `IMovieRepository` present or absent, the single inactive log line, `ValidateOnBuild` and `ValidateScopes` still on; validator cases (unset, zero, negative lease; TTL equal to lease; rounding edge; valid) (FR-021 to FR-025)
- [ ] T024 [US4] Add `RabbitMqConsumerOptionsValidator.cs` (active path only)
- [ ] T025 [US4] Add `RabbitMqServiceCollectionExtensions.cs` (always: owner, topology, publisher; guarded: handler via `AddHandler`, consumer, validator) (depends on T022, T023, T024)
- [ ] T026 [US4] Call it from `src/LamuFlix.Api/Program.cs` after the port registrations and add a secret-free `RabbitMq` section to `appsettings.json`; do not wire a production repository or provider (depends on T025)

## Phase 8: Decision records (step 8, parallel with Phases 3-7) — US5

- [ ] T027 [P] [US5] Write `docs/adr/ADR-0004.md` (Accepted): topology, retry, DLQ and sweeper design, client-7 model, confirms, tracing, no-migration cutover, activation boundary, the in-lease crash-redelivery limitation; states DEV-18 does not implement the sweeper (FR-031)
- [ ] T028 [P] [US5] Write `docs/adr/ADR-0005.md` (Accepted): dual-write mitigation, sweeper now, outbox stretch; same statement (FR-031)
- [ ] T029 [P] [US5] Add glossary terms to `CONTEXT.md` only if it has a glossary

## Phase 9: Gates and refactor (step 9)

- [ ] T030 Run `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` and `./scripts/run-jetbrains-inspectcode.ps1`
- [ ] T031 Refactor pass: `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`
- [ ] T032 Run `dotnet format --verify-no-changes`, full `dotnet test`, the vulnerable-package scan, the mutation gate (at least 80%) and the architecture tests; record any skipped gate as SKIP, never PASS (FR-032)

## Dependencies

- T001 → T002. Phase 2 needs T002. Phases 3, 4 and 8 can start after T002 and run in parallel.
- Phase 5 needs T012. Phase 6 needs Phases 3, 4 and 5. Phase 7 needs Phase 6.
- Phase 9 runs last. Sweeper, outbox, OMDb provider, production repository wiring and the retired projects are out of scope.
