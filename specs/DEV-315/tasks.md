# Tasks: DEV-315 Worker Outcome Translation

**Input**: `specs/DEV-315/spec.md`, `specs/DEV-315/plan.md`, `specs/DEV-315/brief.md` (Q6 ordering)

**Prerequisites**: spec.md, plan.md (both present). No new infrastructure needed.

**Tests**: Included explicitly per spec (unit updates + Testcontainers integration matrix). Integration proofs supplement existing unit policy/routing tests.

**Organization**: Grouped by the Q6 build order: (1) outcome/category + unit updates, (2) publisher overload + consumer wiring + guard cases, (3) broker matrix + regressions. No setup/foundational phase: all files exist.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 (ack), US2 (retry), US3 (DLQ headers), US4 (preserved paths)

## Phase 1: Outcome and category propagation + unit updates

**Purpose**: Core `Failed` carries the actual category; handler and routing unit tests updated

- [X] T001 [US1/US2/US3] Extend `ProcessEnrichmentOutcome.Failed` with actual `EnrichmentFailureCategory` alongside `Decision` in `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentOutcome.cs`
- [X] T002 [US2/US3] Supply the category at both Failed returns in `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs`
- [X] T003 [P] [US2/US3] Update six construction sites and add category propagation proof in `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs`
- [X] T004 [P] [US1/US2/US3] Update two construction sites and keep retained routing proof in `tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs`

**Checkpoint**: `dotnet test` on unit projects passes with the new `Failed` shape.

## Phase 2: Publisher overload + consumer wiring + guard cases

**Purpose**: Guarded `additionalHeaders` overload; terminal-only closed-code headers in consumer

- [ ] T005 [US3] Retain `PublishAsync(EnrichmentRequested, string, CancellationToken)` and add overload with `IReadOnlyDictionary<string, object?> additionalHeaders` before `CancellationToken`, with fresh-dictionary merge, telemetry ownership, collision/null `ArgumentException` guards, and no caller mutation in `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs`
- [ ] T006 [US1/US2/US3] Emit terminal-only closed-code headers and keep publish-before-ack in `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`; retry and out-of-table paths get no failure headers
- [ ] T007 [P] [US3] Add terminal header/telemetry, requested/retry absence-of-failure-header, collision/reserved/null rejection, and caller-dictionary preservation cases in `tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs`

**Checkpoint**: Publisher guard cases and consumer wiring compile; unit tests still green.

## Phase 3: Real-broker matrix + preserved regressions

**Purpose**: Q4 Theory matrix and Q1 regression proofs over a real broker

- [ ] T008 [US1] Real-broker rows in `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs`: Enriched / NotFound / refused claim settle requested with no retry or DLQ (ready/in-flight + lifecycle ack evidence)
- [ ] T009 [US2] Real-broker rows: transient / rate-limited below Max show independent retry ingress (same MovieId, Attempt+1, intact telemetry, no failure headers) plus actual TTL return and provider reinvocation
- [ ] T010 [US3] Real-broker rows: non-retryable below Max, retryable at Max, retryable above Max reach DLQ with unchanged failed Attempt and all three headers, terminally with no further loop
- [ ] T011 [US2/US3] Real-broker row: Max-1 retry crossing to Max proves bounded poison handling
- [ ] T012 [US4] Keep Q1 regression proofs: failed retry republish never success-acks proved by the existing retry-republish failure test (mandatory/confirmed publication failure → original terminal nack `requeue:false` via the existing broker DLQ route, movie stays Pending); add no terminal-publish-failure injection and no client-nack test; failed DLQ republish is a shared catch-path expectation only (never success-acked, terminal nack `requeue:false`, already-recorded Failed movie state unchanged); malformed body, unexpected exception, and stopping-token cancellation settlement behaviour preserved separately

**Checkpoint**: Full `dotnet test` green; broker matrix uses bounded polling/handshakes only (no sleep-only evidence, no exact-ms timing). Broker state isolated per test; small MaxAttempts; short positive integer-ms TTL.

---

Task order constraints (from brief Q6): Phase 1, then Phase 2, then Phase 3; then Cog refactor and Gauge gates. Test failures/compile updates stay within the eight-file map; any unexpected additional file is needs recon/decision, never silent scope growth. `harness.yml` stays out of ticket commits.
