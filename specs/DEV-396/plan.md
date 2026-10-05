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

**Scale/Scope**: 7 production files (1 new), 9 test files (6 new), 5 conditional readiness files, plus evidence artifacts. 16-commit, 18-file historical range.

## Constitution Check

*Initial and post-design:*

- Principle I (architectural and reference boundaries): no new dependency, project, top-level folder, layer, schema, public API shape, `Features:LocalPlay` change, secret or `Process.Start` (`recon:219-224`); `HealthCheckTags` stays in the existing Core pipeline folder.
- Principle IX (testing): tests use the existing xUnit/Shouldly conventions; integration proof of real query or host behaviour stays in `LamuFlix.IntegrationTests`; no mocked data-access driver.
- Principle II (decorator pipeline): `TracingDecorator` keeps its position; only outcome handling changes. Principle VII (secrets, time, configuration): `TimeProvider` kept, no secret or endpoint literal, argument-free exporters. Principle VI (observability): traces, metrics and logs composed as specified.
- API/Contract Rules (`constitution.md:359-360`): OD-1 is an unresolved proposed constitution departure (2.3b), never an accepted exception; it stays open and Gate 1 stays closed. A provisional spec PR does not approve implementation.

No additional departure beyond OD-1 is proposed; OD-1 remains unresolved and is not an accepted exception.

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
- `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` (edit: under Patron P1, the captured-record logger category, the Debug-and-above capture level, and captured scope state with active scope payloads attached to each record; no other change in this file)
- `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` (new)
- `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs` (new)
- `tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs` (new)
- `tests/LamuFlix.IntegrationTests/ServiceDefaultsTelemetryTests.cs` (new)
- `tests/LamuFlix.IntegrationTests/TelemetryCompositionCollection.cs` (new)
- `tests/LamuFlix.Test/TracingDecoratorTests.cs` (edit: T010 assertion only)

The two `ServiceDefaultsTelemetryTests.cs` and `TelemetryCompositionCollection.cs` entries are the only additions to `brief.md`'s list. They are test-only, authorized by `CONCLUSIONS.md` Q8 P2 as the proof placement for exporter composition, the six recorded sources and the three removal receipts, and they need no csproj, package or solution change.

Conditional readiness, only after OD-1 is answered:

- `src/LamuFlix.ServiceDefaults/HealthCheckResponseWriter.cs` (new)
- `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` (new)
- `tests/LamuFlix.IntegrationTests/HealthCheckStubs.cs` (new)
- `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs`, `ApiHostCompositionTests.cs` (narrow, only if required for readiness stubs or contract assertions)

Evidence: `specs/DEV-396/*`, `specs/DEV-307/tasks.md` (individual receipt or deferral updates), task note, historical and delivery findings artifacts, corrective PR records.

Excluded: CPM/csproj/sln changes, Worker repair, unrelated consumer `MarkError` edits (`EnrichmentConsumer.cs:150,242`), threshold changes, schema, LocalPlay, process execution, `web/`. Accepted retrospective fixes need file:line and current-head proof and a narrowly recorded addition to this list before editing; their paths cannot be guessed before reports exist.

## Design

### 1. OpenTelemetry composition (US1, local FR-001..FR-004)

Add `AddOpenTelemetry()` inside the existing idempotence guard of `AddServiceDefaults` (`Extensions.cs:16-34`), after the clock and Serilog registrations, so options validation, clock, console JSON and double-composition behaviour are unchanged. Tracing: ASP.NET Core, HttpClient, Npgsql, `AddSource` for the application, RabbitMQ publisher and RabbitMQ subscriber names (all from `TelemetryConstants`), then the argument-free OTLP exporter. Metrics: ASP.NET Core, HttpClient, `AddMeter` on the existing application identity, argument-free exporter. Logging: argument-free OTLP exporter, fed by the Serilog `writeToProviders` setting. No `Endpoint`, `Protocol`, delegate, `AddService` or extra identity. `TelemetryConstants` gains the two RabbitMQ source names; nothing existing is renamed.

### 2. Same-commit redaction proof (US2, local FR-005)

The HttpClient instrumentation registration and the sentinel proof land in one commit. The narrow preventive action is the proof itself, because default `IHttpClientFactory` URI redaction is the protection: the query is replaced by `*` and user-info and fragment are removed by default, so no production logging change is made - no suppression, no `System.Net.Http.HttpClient.*` level filter, no extended-logging package - and `MetadataProviderServiceCollectionExtensions.cs`, already named in the boundary, keeps health membership as its only edit. `MetadataProviderProbe` is changed only to propagate the logger category, capture Debug and above, and record scope state with active scope payloads attached to each record, because the `HTTP {HttpMethod} {Uri}` scope carries the URI and an absence check that cannot see it is vacuous.

`MetadataProviderTelemetryTests` runs the real lookup against the fixture's stub server and records, before any absence assertion: at least one record whose category starts with `System.Net.Http.HttpClient.` and ends in `.LogicalHandler` or `.ClientHandler`, read from the capture rather than from a hardcoded typed-client name, of which at least one carries a structured `Uri` containing the stub host and path and neither `apikey` nor the sentinel; at least one stopped client activity for the stub host and the `LamuFlix` `Metadata.Lookup` activity. It then asserts absence of the sentinel and its escaped form across every captured activity and every captured record, including resilience-handler records, covering tags, events, status descriptions, categories, rendered messages, structured keys and values, scope payloads and exception text. The redaction check is a positive one and the exact `?*` rendering is not asserted. If the sentinel appears, that is a Medium+ finding that returns to Patron; it is never met by an opt-out, a suppression or a weaker assertion. The sentinel key is a test sentinel, not a credential literal.

### 3. Health membership (US3, local FR-006, FR-007)

`HealthCheckTags.Ready` is added beside `TelemetryConstants`. Persistence registers `AddDbContextCheck` over the existing context with the shared tag; RabbitMQ uses the shared constant in place of the literal; the metadata-provider registration drops its tag and stays registered. `HealthCheckRegistrationTests` reads the real registrations (not stubs). The liveness route and the WebApplicationFactory harness already exist on main (DEV-308 `2ce1e39`) and are reconciled, not reimplemented.

### 4. Handler outcome (US4, local FR-008, FR-009)

`TracingDecorator` splits validation failures from other exceptions: validation leaves status unset and sets the validation-failed outcome; all others set error status and the full type name; both rethrow. Two outcome constants are added to `TelemetryConstants` (sequential after section 1). `Pipeline/TracingDecoratorTests` covers both paths with an `ActivityListener`, including the full-name assertion. The legacy `tests/LamuFlix.Test` one-assertion edit is retained and recorded as ungated.

### 5. Conditional readiness (US5, local FR-010) - BLOCKED on OD-1

`/health/ready`, `HealthCheckResponseWriter` and the four-state endpoint tests are specified but not scheduled for execution. They start only after the owner answers OD-1, with no provisional body or default writer. The existing live harness clears all registrations (`ApiHostFactory.cs:48-52`), so readiness tests will need real-registration-aware stubs, to be planned only after the answer.

### 6. Retrospective (US6, local FR-011..FR-013)

Conductor coordinates Rigger to create a disposable detached worktree at the full historical head; Gauge runs a cold pre-pass pinned to both full SHAs; Sentry, Ledger and Compass report; Keel adjudicates only after all three. Each accepted Medium+ finding is reproduced at the delivery head; still-live findings get a narrow fix (Cog) with a plan amendment first; resolved ones are recorded with evidence. Artifacts are retained before the checkout is retired. Historical and delivery receipts are never merged.

Review cap accounting is two independent budgets of the same size, one for this retrospective and one for the delivery review (Phase Order step 7), and neither lends to the other. Each allows at most 2 rounds of at most 2 fix commits; the numbers are the existing cap and are not enlarged. A round is one three-axis review of a pinned HEAD plus its adjudication and remediation, and the closure check after that round's fixes belongs to the round: it is scoped to the fixed finding IDs and done by the originating axis, never a fresh review. A fix commit is one that changes files to close an accepted finding of that round, so a commit addressing several findings counts once. Record-only commits (CONCLUSIONS, ASSUMPTIONS, receipts, notes), a rebase on main and pre-review gauge fixes consume neither.

Exhaustion is terminal: a Medium+ finding still open after round 2's closure check, or a third fix commit needed inside a round, stops the work with no further commit, no third round, no additional immutable review, no severity downgrade, no waiver and no merge-bar sign-off. The task stays not delivered and the single report is `blocked: review cap exhausted - DEV-396 <historical|delivery> <finding ids>` to Bernstein. Only the owner can extend a cap.

Lower findings become follow-up records. S7, the differing `error.type` conventions between the decorator, the consumer and the OMDb category, is recorded as noted with no ticket: it is an inherited consistency concern, not broken behaviour, so no consumer, classifier or earlier ADR is edited and no production consistency fix is made.

### 7. Evidence, gates and publication (US7, local FR-014..FR-016)

Rigger restores committed `harness.yml` before gate runs; each receipt includes `git diff --quiet HEAD -- harness.yml` exit 0. Gauge runs every required gate with command, exit and verdict. Gate exit classification follows the current task pipeline: scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS, disabled gate SKIP, mutation NOT APPLICABLE only from the actual script verdict, property exit 2 with a recorded opt-out reason, exit 1 and Could not run never green. Per-task evidence reconciliation updates `specs/DEV-307/tasks.md` boxes only on cited receipts.

### Test and gate expectations

Provider and exporter composition is never labelled export delivery. Baseline counts (621 passed; 16 property tests) are historical; deltas are tracked honestly. A standalone legacy build failure is a separate Could not run, not a substitute for active gates.

**Composition proof seam.** No public API lists a provider's exporters and none is added, so exporter attachment is proved behaviourally on the production-composed host, never by test-local recomposition. `ServiceDefaultsTelemetryTests` builds the real host with `OTEL_EXPORTER_OTLP_ENDPOINT` pointed at the already-referenced WireMock stub and `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`, emits one span, one metric and one `ILogger` record through the existing Serilog provider bridge, calls `ForceFlush` on each of the three providers with an explicit timeout and asserts each returned true, then asserts the stub received requests on `/v1/traces`, `/v1/metrics` and `/v1/logs`. That single test proves all three providers carry an OTLP exporter, that the environment wins over any argument, and that a positive log reaches the composed provider. The test name says `exporter is composed`, the stub's payload content is never asserted, and no csproj, package, `InternalsVisibleTo` or public API is added.

Default endpoint and shared options stay in UnitTests: `OtlpEndpointEnvironmentTests` resolves `OtlpExporterOptions` from the container the production `AddServiceDefaults()` built and asserts the default endpoint plus the environment override. Whether that resolves is unverified; if it does not, work stops and reports to Patron rather than dropping the assertion or constructing a fresh `OtlpExporterOptions`.

**Six recorded sources and removal sensitivity.** The six sources are recorded on one production `WebApplicationFactory<Program>` host inside the new non-parallel collection, each with a real trigger: an in-process request for ASP.NET Core, an outbound request to the WireMock stub for HttpClient, a real query against `PostgresFixture` for Npgsql, the real `LamuFlix` lookup emission site for the application source, and `BasicPublishAsync` plus `BasicGetAsync` against `RabbitMqFixture` for the RabbitMQ publisher and subscriber sources. Emission is proved, not subscription. The test-side `ActivityListener` is a passive observer beside the production-composed listener, which alone creates and records: its `Sample` returns `None` and its `SampleUsingParentId` returns `None` or is left unset, and it never returns `PropagationData`, `AllData` or `AllDataAndRecorded`, because any of those would make the test create the activities it claims to observe. A row passes only when the observer's `ActivityStopped` sees an activity from that source with both `IsAllDataRequested` and `Recorded` true; both are required, because an unsampled fallback activity can exist with `Recorded` false. Exact source names are read from the first execution and pinned as test constants. This is a source-level conclusion and not a measurement: if the production host yields zero qualifying activities on the first execution, work stops and reports to Patron, and the observer's sampling, the environment and the row count are never changed to make it pass.

Removal sensitivity is per-row and executed, never asserted in words. Each row asserts only its own source, so removing one registration can fail only its own row. Each row first runs a negative control: before the production host is built, the same trigger yields no recorded activity from that source, which is what proves no other listener props the check up. Anvil then performs three local uncommitted removals of `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation` and `AddNpgsql` in `Extensions.cs`, records for each that exactly its own row fails while the other rows pass, and restores the file so `git diff --quiet -- src/LamuFlix.ServiceDefaults/Extensions.cs` exits 0; those receipts are the removal evidence. A host that never calls `AddServiceDefaults` is the negative control and never the removal proof. SC-001 and SC-002 keep their existing wording and their budgets of three exporters and three instrumentations.

**No collector, and deterministic isolation.** In production, when no collector is reachable the exporters fail to `OpenTelemetry-Exporter-OpenTelemetryProtocol` and `OpenTelemetrySdkEventSource` only, never to `ILogger` or Serilog: the application keeps running and telemetry is dropped. Operators can opt into exporter diagnostics with `OTEL_DIAGNOSTICS.json`; no diagnostics file is committed and none is produced by tests. Exporters stay argument-free, so no timeout, protocol, endpoint or processor argument is set, and shutdown spends at most the documented 10 s per-exporter budget. That budget is accepted as documented and is still unmeasured.

In tests, every assertion about exporter output follows an explicit `ForceFlush(timeout)` that returned true, and nothing sleeps or waits out a batch delay. Every host and provider a telemetry test builds is disposed in `finally`. Process-environment mutation lives only in the non-parallel collections and is restored in `finally`, so the recorded-activity and negative-control checks never run while another composed provider is alive. If the new process-wide provider changes what the existing capture tests observe (`MetadataProviderLookupTests.cs`, `EnrichmentConsumerTests.cs`), the fix is narrower correlation inside that test - source, operation name or trace id; global telemetry is never disabled and no test is skipped. Anvil records the IntegrationTests suite duration before and after the composition commit and the slowest host disposal; disposal over 12 s or suite growth over 25 percent is a Medium finding that returns to Patron and is never silenced by test-host exporter overrides.

**Serilog bridge.** Because the OTLP logging pipeline is fed through the existing Serilog provider bridge, production log eligibility follows Serilog's own filtering and configuration, and console output and the OTLP path coexist without either suppressing the other. Debug-and-above capture in the redaction test is a test requirement, not a promise that production emits every Debug record. A local positive log reaching the composed provider is non-vacuity evidence for the composition seam, not proof of network export.

## Phase Order and Owners

1. Phase B pickup drift and evidence recon (Wisp); Rigger restores harness before gates.
2. Cold retrospective (Conductor, Rigger, Gauge, Sentry, Ledger, Compass, Keel); then current-head reproduction and Cog remediation.
3. OpenTelemetry composition and redaction proof (Cog alters existing files, including the `MetadataProviderProbe.cs` change; Anvil creates new tests; T016 and T018 land in one coordinated commit; each shared file has one exclusive owner at a time).
4. Health membership and real-registration tests (Cog owns edits to existing production files; Anvil creates the new file `HealthCheckTags.cs` and the new test).
5. Decorator, legacy assertion, active proof (Cog alters, Anvil creates new files).
6. Conditional readiness, only after OD-1.
7. Evidence reconciliation, gates, refactor, architect, delivery review, corrective records, Keel merge-bar comment (Rigger writes tracker and PR records).

## Complexity Tracking

No additional departure beyond the unresolved OD-1 is proposed; OD-1 is not an accepted exception.

## Open Items

- OD-1 owner checkbox: open; Gate 1 closed.
- T020A: open pending owner-answer text.
- ADR step (L): Keel, per task-pipeline Phase 2; any ADR number comes from Wisp facts, not from this plan. No CONTEXT.md change decided.
