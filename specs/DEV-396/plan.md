# Implementation Plan: Complete DEV-307 Observability and Run the DEV-308 Retrospective

**Branch**: `feature/396-spec` | **Date**: 2026-10-05 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), [brief.md](brief.md) (owns the plan decisions), [CONCLUSIONS.md](CONCLUSIONS.md) Q1-Q6, [ASSUMPTIONS.md](ASSUMPTIONS.md), note `recon-DEV-396`.

## Summary

Two pieces of work travel together. First, finish the omitted DEV-307 scope in the existing `AddServiceDefaults`: OpenTelemetry traces, metrics and logs, same-commit OMDb redaction proof, health-check membership and tags, and the handler-outcome decorator, with readiness held BLOCKED on the inherited owner Q2. Second, run the never-performed DEV-308 retrospective over `122c502b03b0eaffe18b79b0fd26183466d8f0d0..7a35e7727241c3afd92ac9ba21fa9d13da37cdd5`, correct accepted still-live Medium+ findings narrowly, and publish corrective records. No new dependency, project, layer, schema or API shape.

**Note on the Spec Kit phases**: this plan folds research, data model, contracts and quickstart into the sections below. No `research.md`, `data-model.md`, `contracts/` or `quickstart.md` is produced because no entity, endpoint or package is added; the only contract (readiness body) is owner-blocked and lives in `specs/DEV-307/spec.md`.

## Technical Context

**Language/Version**: C# on .NET 10; xUnit and Shouldly (existing conventions); WireMock.Net in the existing integration fixture.

**Primary Dependencies**: all already pinned and referenced (`recon:141-146`): `OpenTelemetry.Extensions.Hosting` and `Exporter.OpenTelemetryProtocol` 1.19.1, `Instrumentation.AspNetCore` and `Instrumentation.Http` 1.19.0, `Npgsql.OpenTelemetry` 10.0.3, `Microsoft.Extensions.Diagnostics.HealthChecks` and its EF Core package, `Microsoft.AspNetCore.Mvc.Testing` 10.0.12.

**Storage**: none changed. `AddDbContextCheck` reads the existing context.

**Testing**: `LamuFlix.UnitTests`, `LamuFlix.IntegrationTests`; `tests/LamuFlix.Test` is outside `LamuFlix.sln` and cannot build standalone (NU1010), so it contributes zero.

**Constraints**: no explanatory comments; `TimeProvider` for time; `ActivityListener` proof; environment mutation isolated in a non-parallel collection and restored in `finally`; no threshold change.

**Scale/Scope**: 7 production files (1 new), 7 test files (4 new), 5 conditional readiness files, plus evidence artifacts. 16-commit, 18-file historical range.

## Constitution Check

*Initial and post-design:*

- No new dependency, project, top-level folder, layer, schema, public API shape, `Features:LocalPlay` change, secret or `Process.Start` (`recon:219-224`).
- Tests use the existing xUnit/Shouldly conventions; integration proof of real query or host behaviour stays in `LamuFlix.IntegrationTests`; no mocked data-access driver.
- `HealthCheckTags` sits beside `TelemetryConstants` in the existing `Core/Pipeline` folder.
- OD-1 is an owner checkbox (a 2.3b departure from `constitution.md:359-360` for this ticket only if accepted); it stays open and Gate 1 stays closed. A provisional spec PR does not approve implementation.

No constitution violation or new structural decision is introduced.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-396/
|- brief.md, CONCLUSIONS.md, ASSUMPTIONS.md   (Keel / Patron)
|- spec.md, plan.md, tasks.md                  (this draft)
|- checklists/requirements.md                  (specify + checklist actions)
|- DRAFTING_RECEIPT.md
`- retrospective/                              (Phase B artifacts, created later)
```

### File Boundary (from `brief.md` File boundary)

Production completion:

- `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` (edit)
- `src/LamuFlix.ServiceDefaults/Extensions.cs` (edit; shared edits sequential)
- `src/LamuFlix.Core/Pipeline/HealthCheckTags.cs` (new)
- `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (edit)
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs` (edit)
- `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs` (edit)
- `src/LamuFlix.Infrastructure/Pipeline/TracingDecorator.cs` (edit)

Test completion:

- `tests/LamuFlix.UnitTests/ServiceDefaultsTests.cs` (edit)
- `tests/LamuFlix.UnitTests/OtlpEndpointEnvironmentTests.cs` (new)
- `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` (edit: category and capture level only)
- `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` (new)
- `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs` (new)
- `tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs` (new)
- `tests/LamuFlix.Test/TracingDecoratorTests.cs` (edit: T010 assertion only)

Conditional readiness, only after OD-1 is answered:

- `src/LamuFlix.ServiceDefaults/HealthCheckResponseWriter.cs` (new)
- `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` (new)
- `tests/LamuFlix.IntegrationTests/HealthCheckStubs.cs` (new)
- `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs`, `ApiHostCompositionTests.cs` (narrow, only if required for readiness stubs or contract assertions)

Evidence: `specs/DEV-396/*`, `specs/DEV-307/tasks.md` (individual receipt or deferral updates), task note, historical and delivery findings artifacts, corrective PR records.

Excluded: CPM/csproj/sln changes, Worker repair, unrelated consumer `MarkError` edits (`EnrichmentConsumer.cs:150,242`), threshold changes, schema, LocalPlay, process execution, `web/`. Accepted retrospective fixes need file:line and current-head proof and a narrowly recorded addition to this list before editing; their paths cannot be guessed before reports exist.

## Design

### 1. OpenTelemetry composition (US1, FR-001..FR-004)

Add `AddOpenTelemetry()` inside the existing idempotence guard of `AddServiceDefaults` (`Extensions.cs:16-34`), after the clock and Serilog registrations, so options validation, clock, console JSON and double-composition behaviour are unchanged. Tracing: ASP.NET Core, HttpClient, Npgsql, `AddSource` for the application, RabbitMQ publisher and RabbitMQ subscriber names (all from `TelemetryConstants`), then the argument-free OTLP exporter. Metrics: ASP.NET Core, HttpClient, `AddMeter` on the existing application identity, argument-free exporter. Logging: argument-free OTLP exporter, fed by the Serilog `writeToProviders` setting. No `Endpoint`, `Protocol`, delegate, `AddService` or extra identity. `TelemetryConstants` gains the two RabbitMQ source names; nothing existing is renamed.

### 2. Same-commit redaction proof (US2, FR-005)

The HttpClient instrumentation registration and the sentinel proof land in one commit. `MetadataProviderProbe` is changed only to propagate the logger category and to capture Debug and above. `MetadataProviderTelemetryTests` runs the real lookup against the fixture's stub server, asserts an actual outbound span and HttpClient-category entries exist, then asserts absence of the sentinel in span tags and in rendered, structured and exception log content. No redaction opt-out setting and no credential literal.

### 3. Health membership (US3, FR-006, FR-007)

`HealthCheckTags.Ready` is added beside `TelemetryConstants`. Persistence registers `AddDbContextCheck` over the existing context with the shared tag; RabbitMQ uses the shared constant in place of the literal; the metadata-provider registration drops its tag and stays registered. `HealthCheckRegistrationTests` reads the real registrations (not stubs). The liveness route and the WebApplicationFactory harness already exist on main (DEV-308 `2ce1e39`) and are reconciled, not reimplemented.

### 4. Handler outcome (US4, FR-008, FR-009)

`TracingDecorator` splits validation failures from other exceptions: validation leaves status unset and sets the validation-failed outcome; all others set error status and the full type name; both rethrow. Two outcome constants are added to `TelemetryConstants` (sequential after section 1). `Pipeline/TracingDecoratorTests` covers both paths with an `ActivityListener`, including the full-name assertion. The legacy `tests/LamuFlix.Test` one-assertion edit is retained and recorded as ungated.

### 5. Conditional readiness (US5, FR-010) - BLOCKED on OD-1

`/health/ready`, `HealthCheckResponseWriter` and the four-state endpoint tests are specified but not scheduled for execution. They start only after the owner answers OD-1, with no provisional body or default writer. The existing live harness clears all registrations (`ApiHostFactory.cs:48-52`), so readiness tests will need real-registration-aware stubs, to be planned only after the answer.

### 6. Retrospective (US6, FR-011..FR-013)

Conductor coordinates Rigger to create a disposable detached worktree at the full historical head; Gauge runs a cold pre-pass pinned to both full SHAs; Sentry, Ledger and Compass report; Keel adjudicates only after all three. Each accepted Medium+ finding is reproduced at the delivery head; still-live findings get a narrow fix (Cog) with a plan amendment first; resolved ones are recorded with evidence. Artifacts are retained before the checkout is retired. Historical and delivery receipts are never merged.

### 7. Evidence, gates and publication (US7, FR-014..FR-016)

Rigger restores committed `harness.yml` before gate runs; each receipt includes `git diff --quiet HEAD -- harness.yml` exit 0. Gauge runs every required gate with command, exit and verdict. Gate exit classification follows the current task pipeline: scope-empty exit 2 SKIPPED (never PASS), disabled gate SKIP, mutation NOT APPLICABLE only from the actual script verdict, property exit 2 with a recorded opt-out reason, exit 1 and Could not run never green. Per-task evidence reconciliation updates `specs/DEV-307/tasks.md` boxes only on cited receipts.

### Test and gate expectations

Provider and exporter composition is never labelled export delivery. Each instrumentation registration has a removal-sensitivity check. Baseline counts (621 passed; 16 property tests) are historical; deltas are tracked honestly. A standalone legacy build failure is a separate Could not run, not a substitute for active gates.

## Phase Order and Owners

1. Phase B pickup drift and evidence recon (Wisp); Rigger restores harness before gates.
2. Cold retrospective (Conductor, Rigger, Gauge, Sentry, Ledger, Compass, Keel); then current-head reproduction and Cog remediation.
3. OpenTelemetry composition and redaction proof (Anvil).
4. Health membership and real-registration tests (Anvil / Cog).
5. Decorator, legacy assertion, active proof (Cog alters, Anvil creates new files).
6. Conditional readiness, only after OD-1.
7. Evidence reconciliation, gates, refactor, architect, delivery review, corrective records, Keel merge-bar comment (Rigger writes tracker and PR records).

## Complexity Tracking

No constitution violation requires justification.

## Open Items

- OD-1 owner checkbox: open; Gate 1 closed.
- T020A: open pending owner-answer text.
- ADR step (L): Keel, per task-pipeline Phase 2; any ADR number comes from Wisp facts, not from this plan. No CONTEXT.md change decided.
