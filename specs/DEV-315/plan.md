# Implementation Plan: DEV-315 Worker Outcome Translation

**Branch**: `feature/315-spec` | **Date**: 2026-10-08 | **Spec**: `specs/DEV-315/spec.md`

**Input**: Feature specification from `specs/DEV-315/spec.md`; decisions from `specs/DEV-315/brief.md` (Q1-Q6); facts from `recon-DEV-315`.

## Summary

Translate worker outcomes to broker settlement in `EnrichmentConsumer`: ack for completed/refused outcomes, publish-before-ack republish to `enrichment.retry` for retryable failures below Max, and publish-before-ack routing to `enrichment.dead-letter` with three closed-code failure headers for terminal failures. Actual failure category propagates through `ProcessEnrichmentOutcome.Failed`; the publisher gains a guarded `additionalHeaders` overload. Proof is real-broker Testcontainers tests reusing the existing fixture.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: RabbitMQ client, Testcontainers.RabbitMq 4.15.0 (already pinned, no new dependency)

**Storage**: N/A (no schema change)

**Testing**: xUnit (unit + Testcontainers integration); property tests per harness applicability

**Target Platform**: Linux/Windows services (API + Worker)

**Project Type**: Backend worker feature (no frontend)

**Performance Goals**: Bounded poison handling: Max-1 retry crosses to Max and terminates; no endless loops

**Constraints**: `TreatWarningsAsErrors=true`; Roslyn, cyclomatic complexity, InspectCode gates; publish-before-ack preserves at-least-once interpretation; no exact-millisecond timing in tests

**Scale/Scope**: 8 existing files, zero new files; small MaxAttempts via test options, short positive integer-ms TTL via test config

## Constitution Check

- Principle I (ports and adapters, feature-organised core): no new port, adapter, layer, or project; `IEnrichmentQueue` unchanged; the publisher overload is a concrete adapter method, not a new seam.
- Principle II (explicit handlers and decorators): no new command/query/handler shape; the category propagates through the existing `Failed` outcome; no MediatR or dispatch change.
- Principle III: N/A — no browsing, filtering, or sort change.
- Principle IV (enrichment state machine): retry stays category-driven below Max with Attempt+1, terminal otherwise; no transition, status-value, or failure-category change.
- Principle V: N/A — no HTTP endpoint, validation, or error-response change.
- Principle VI (observability): telemetry headers preserved with publisher-owned keys; no new span, metric, or health-check dependency.
- Principle VII (configuration isolation and deterministic time): test MaxAttempts via options and TTL via config; no secrets, magic strings, or `DateTime.Now`-family calls; `TimeProvider` unchanged.
- Principle VIII (ubiquitous language): Enrichment, Failure Category, Retry/DeadLetter terms retained; no new domain term and no vendor or Portuguese identifiers.
- Principle IX (test pyramid with real infrastructure): real-broker Testcontainers proofs for routing behaviour; no Infrastructure mocking; xUnit v3 + NSubstitute + Shouldly stack unchanged.
- Workflow gates: PR quality gates and all three static-analysis gates apply to the changed `.cs` files; `dotnet format --verify-no-changes` required; no new dependency, port (no ADR), filter, closed value set, state transition, endpoint, exception type, options record, or comment beyond exemptions.
- No new dependency, project, layer, schema, public API shape, or local-play change. Internal worker queue headers are not a public API (Patron ruling, brief pre-Q2).
- No violations requiring justification; the pre-fix vertical-slice wording (C1) is corrected above with no architecture change.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-315/
  spec.md              # Feature specification
  plan.md              # This file
  brief.md             # Grill decisions (authoritative input)
  CONCLUSIONS.md       # Patron rulings
  tasks.md             # Task list (Phase 2 output)
```

### Source Code (repository root)

```text
src/LamuFlix.Core/Features/Enrichment/
  ProcessEnrichmentOutcome.cs
  ProcessEnrichmentCommandHandler.cs
src/LamuFlix.Infrastructure/RabbitMq/
  EnrichmentConsumer.cs
  RabbitMqEnrichmentQueuePublisher.cs
tests/LamuFlix.UnitTests/Features/Enrichment/
  ProcessEnrichmentCommandHandlerTests.cs
tests/LamuFlix.UnitTests/RabbitMq/
  EnrichmentRoutingTests.cs
tests/LamuFlix.IntegrationTests/
  EnrichmentConsumerTests.cs
  RabbitMqPublisherTests.cs
```

**Structure Decision**: Existing ports-and-adapters layout with a feature-organised core and explicit handlers with decorators is kept; all eight files already exist and are edited in place. No new files, projects, or folders.

## Design

### Approach

1. Extend `ProcessEnrichmentOutcome.Failed` with the actual `EnrichmentFailureCategory` alongside `Decision`; supply it at both Failed returns in `ProcessEnrichmentCommandHandler`. Core keeps decision and `NextAttempt` responsibility.
2. Add the concrete publisher overload `PublishAsync(EnrichmentRequested, string, IReadOnlyDictionary<string, object?> additionalHeaders, CancellationToken)`; existing overload delegates without additions. Fresh per-publish header dictionary; telemetry owned by publisher; collisions with telemetry/`x-death`/`x-first-death-*` and null values rejected with `ArgumentException`; caller dictionary never mutated.
3. Wire the consumer: terminal disposition publishes to `enrichment.dead-letter` with the three settled headers via the new overload; retry disposition publishes to `enrichment.retry` with no failure headers; both publish-before-ack. Completed/refused outcomes ack directly.
4. Prove with the Q4 real-broker Theory matrix plus publisher guard cases, reusing the existing Testcontainers fixture. Failed-publish proof is the existing retry-republish failure test; the DLQ-republish clause is a shared catch-path expectation with no separate broker-injection proof per the Patron F1/F2 proof amendment (`brief.md`, `CONCLUSIONS.md`); the awaited mandatory-publish client contract is ADR-0004:42.

### Alternatives Considered

- New translator/policy abstraction: rejected (Patron AMEND Q1; retain existing core outcome/transport separation).
- Ack-before-publish: rejected (ticket lists actions; publish-before-ack preserves at-least-once interpretation).
- New publisher test file: rejected (reuse tracked `RabbitMqPublisherTests.cs`).
- New fixture or topology change: rejected (reuse existing fixture/package).

## Complexity Tracking

No constitution violations; nothing to track.

## Gate Expectations (Phase B)

Pipeline Roslyn, complexity, InspectCode, property tests (configured applicability), vulnerable packages, format, full `dotnet test`, and mutation with authoritative harness thresholds. Scope-empty SKIPPED, harness-disabled SKIP, and mutation-exclusion N/A keep their labels and are never promoted to PASS. Failure or Could not run blocks. `harness.yml` is excluded from ticket commits.
