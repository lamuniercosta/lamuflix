# Tasks: DEV-396 Complete DEV-307 Observability and Run the DEV-308 Retrospective

**Input**: Design documents from `specs/DEV-396/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md), [ASSUMPTIONS.md](ASSUMPTIONS.md)

**Tests**: Required. Every implementation phase carries its own tests; tests accompany each phase.

## Format: `[ID] [P?] [Story] Description (inherits DEV-307 Txxx)`

- **[P]**: Can run in parallel because it touches distinct files and has no dependency on another task.
- **[Story]**: User story from `spec.md`.
- **Inherits**: the `specs/DEV-307/tasks.md` task ID this task completes or reconciles. The Inheritance Map at the end proves every inherited ID is covered.
- Shared `Extensions.cs` and `TelemetryConstants.cs` edits are sequential, never concurrent, with one exclusive owner at a time.
- Ownership: Cog alters existing files (including `MetadataProviderProbe.cs`); Anvil creates new files and tests. Phase order, scope and caps are unchanged.
- IDs written "inherited Txxx" refer to `specs/DEV-307/tasks.md`; local IDs are DEV-396 tasks. Local T041 carries inherited T048.
- After every `.cs` task, run the incremental Roslyn, complexity, InspectCode and format pass with `-Files` passed as a real array. The close-out pass is T037.
- A red run is re-run at most once and only to record that it did not reproduce; never retried until green.
- No task ticks OD-1 or T020A by inference. A BLOCKED task is reported and waited on, never started provisionally.

## Phase 1: Pickup, Recon and Harness Hygiene

**Purpose**: Establish current facts and a clean gate baseline before any change.

- [ ] T001 Pickup drift check of every file in `plan.md` File Boundary against `origin/main`, the range head `7a35e77` ancestry, and `recon-DEV-396`; record deltas and the delivery-base HEAD SHA (Wisp). Drift that invalidates a recon citation goes to Keel. (inherits T001, T003, T004)
- [ ] T002 [P] Record the Q2 and T020A state as open in the task note, verbatim from `spec.md` OD-1; record that no owner-answer text for DEV-307 Q1 or T020A has been located. (inherits T002)
- [ ] T003 Rigger restores committed `harness.yml` (`git restore harness.yml`) and records `git diff --quiet HEAD -- harness.yml` exit 0; never commit the deletion, change a threshold or add a waiver (Q4).
- [ ] T004 Record the delivery baseline at the delivery head: `dotnet test` per-project counts, 16 property tests, scoped analyzers, format, vulnerable packages, mutation verdict and web gate verdict; recon values are historical only. (inherits T001 baseline)

**Checkpoint**: facts current, harness clean, OD-1 recorded open.

## Phase 2: Retrospective of DEV-308 (User Story 6, Priority P1)

**Goal**: A cold, three-axis, adjudicated review of `122c502b03b0eaffe18b79b0fd26183466d8f0d0..7a35e7727241c3afd92ac9ba21fa9d13da37cdd5`.

**Independent Test**: All artifacts name both full SHAs; three axis reports exist before adjudication.

- [ ] T005 [US6] Conductor coordinates Rigger to create a disposable detached worktree at the full head `7a35e7727241c3afd92ac9ba21fa9d13da37cdd5` (never the main checkout or `feature/396-spec`); verify `git rev-parse HEAD` equals the full SHA.
- [ ] T006 [US6] Gauge runs a fresh pre-pass pinned to base `122c502b03b0eaffe18b79b0fd26183466d8f0d0` and the full head; no borrowed artifact (the existing `pre-pass-0c9c052` belongs to another run).
- [ ] T007 [P] [US6] Sentry axis report over the range, naming both SHAs.
- [ ] T008 [P] [US6] Ledger axis report over the range, naming both SHAs.
- [ ] T009 [P] [US6] Compass axis report over the range, naming both SHAs.
- [ ] T010 [US6] Keel adjudicates only after T007-T009 all exist; a missing axis is blocked, never waived. Publish the adjudication artifact.
- [ ] T011 [US6] For each accepted Medium+ finding, reproduce at the delivery head; record file:line and result. Resolved findings are recorded with evidence; still-live findings continue to T012.
- [ ] T012 [US6] Record each still-live Medium+ finding's file as a narrow addition to `plan.md` File Boundary before editing; Cog applies the narrow fix with a test; at most 2 fix commits per round, 2 rounds in total.
- [ ] T013 [US6] Lower findings: write follow-up records; open a ticket only for Critical/High or broken behaviour (Patron decides tracker recording, Rigger writes it); otherwise "noted, no ticket".
- [ ] T014 [US6] Retain all historical artifacts, then retire the disposable checkout.

**Checkpoint**: historical and delivery receipts are separate; no Medium+ finding is open.

## Phase 3: OpenTelemetry Composition and Redaction Proof (User Stories 1 and 2, Priority P1)

**Goal**: Traces, metrics and logs composed in `AddServiceDefaults`, with the OMDb sentinel proof in the same commit as HttpClient instrumentation.

**Independent Test**: Providers resolve with exporters; each instrumentation removal fails its own check; sentinel absent from spans and logs.

- [ ] T015 [US1] (Cog) Add `RabbitMqPublisherActivitySourceName` ("RabbitMQ.Client.Publisher") and `RabbitMqSubscriberActivitySourceName` ("RabbitMQ.Client.Subscriber") to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`; no rename or removal; copy strings exactly. (inherits T027)
- [ ] T016 [US1] (Cog) In `src/LamuFlix.ServiceDefaults/Extensions.cs`, add tracing (ASP.NET Core, HttpClient, Npgsql, three `AddSource` names) with argument-free `AddOtlpExporter()`; no `Endpoint`, `Protocol`, delegate or `AddService`. Same commit as T018 proof for the HttpClient instrumentation. (inherits T028)
- [ ] T017 [US1] (Cog) On the same `AddOpenTelemetry()` call add metrics (ASP.NET Core, HttpClient, `AddMeter` on the existing identity) and logging with argument-free exporters; no invented identity or Npgsql metric. (inherits T029)
- [ ] T018 [US2] (Cog edits the probe; Anvil creates the telemetry test) Extend `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` for category propagation and Debug-and-above capture only; create `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs`: real lookup with the sentinel key, assert an actual outbound span and HttpClient-category entries exist, then assert the sentinel is absent from span tags and rendered, structured and exception log content. No URI-redaction opt-out, no credential literal. Committed in the same commit as T016, coordinated between Cog and Anvil. (inherits T033A)
- [ ] T019 [US1] (Cog edits existing file) Extend `tests/LamuFlix.UnitTests/ServiceDefaultsTests.cs`: tracer, meter and log providers resolve and each carries an OTLP exporter; test names say "exporter is composed", never "exported"; application and both RabbitMQ source names are collected; one theory with three rows (ASP.NET Core via a real in-process request, HttpClient, Npgsql) proves each instrumentation reaches a recording processor; removal of each registration fails its own row. (inherits T030)
- [ ] T020 [US1] (Anvil) Create `tests/LamuFlix.UnitTests/OtlpEndpointEnvironmentTests.cs` in a non-parallel collection: default endpoint, shared options across all three providers, and the standard environment variable winning; mutate process environment and restore in `finally`. (inherits T031)
- [ ] T021 [US1] Gates on `TelemetryConstants.cs`, `Extensions.cs`, `ServiceDefaultsTests.cs`, `OtlpEndpointEnvironmentTests.cs`, `MetadataProviderProbe.cs`, `MetadataProviderTelemetryTests.cs`, then `dotnet test` for UnitTests and IntegrationTests; record exits. (inherits T032)
- [ ] T022 [US1] Boundary checks: `rg -n "Endpoint\s*=" src/LamuFlix.ServiceDefaults` returns nothing; `rg -n "DisableUriRedaction|DISABLE_URL_QUERY_REDACTION" src tests` returns nothing; double composition does not double Serilog or OpenTelemetry registrations. (inherits T033)

**Checkpoint**: US1 and US2 delivered with receipts; no composition test is described as collector delivery.

## Phase 4: Health Membership (User Story 3, Priority P1)

- [ ] T023 [P] [US3] (Anvil) Create `src/LamuFlix.Core/Pipeline/HealthCheckTags.cs` with `Ready = "ready"` beside `TelemetryConstants`. (inherits T034)
- [ ] T024 [US3] (Cog) In `PersistenceServiceCollectionExtensions.cs` register the PostgreSQL check over `LamuFlixDbContext` tagged with the constant; in `RabbitMqServiceCollectionExtensions.cs` replace the literal with the constant; no schema or migration, no driver mock. (inherits T035)
- [ ] T025 [US3] (Cog) In `MetadataProviderServiceCollectionExtensions.cs` remove the readiness tag and leave the check registered. (inherits T036)
- [ ] T026 [US3] (Anvil) Create `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs` reading the real registrations (independent of endpoint stubs): exactly two ready-tagged checks (postgres, rabbitmq), metadata-provider registered and untagged. (inherits T036A)
- [ ] T027 [US3] Gates on the four production files and the new test; `rg -n 'tags:\s*\[\"ready\"\]' src` returns nothing; boundary check on `MapHealthChecks|MapDefaultEndpoints` sites unchanged. (inherits T036B, T041 non-readiness half)

**Checkpoint**: readiness group membership proven; no readiness route yet.

## Phase 5: Handler Outcome Decorator (User Story 4, Priority P1)

- [ ] T028 [P] [US4] (Cog) Add the handler-outcome key and validation-failed value constants to `TelemetryConstants.cs` after T015 (sequential, same file). (inherits T005)
- [ ] T029 [US4] (Cog) In `TracingDecorator.cs` separate validation failures (status unset, outcome validation-failed) from other exceptions (error status, full type name, `error.type`), rethrowing both unchanged; leave the consumer `MarkError` untouched. (inherits T007)
- [ ] T030 [US4] (Anvil) Create `tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs`: two `[Fact]` cases with an `ActivityListener` covering status, attributes and propagation; the unexpected-exception case asserts the full type name and executes in the active project. (inherits T009)
- [ ] T031 [US4] (Cog) Edit the one assertion at `tests/LamuFlix.Test/TracingDecoratorTests.cs:65` to the full-name value; record the standalone legacy check as `Could not run` (project not in `LamuFlix.sln`, NU1010); never present it as proof. (inherits T010)
- [ ] T032 [US4] Gates on the changed files and `dotnet test`; boundary diff shows only plan files. (inherits T011, T012)

**Checkpoint**: both span paths proven by an executing active test.

## Phase 6: Conditional Readiness (User Story 5, Priority P2) - BLOCKED on OD-1

**Do not start until the owner answers OD-1 including the FR-034 deferral clause. Not counted as delivered; not reported as deferred.**

- [ ] T033 [US5] BLOCKED (OD-1): map `/health/ready` filtered by the shared tag in `src/LamuFlix.ServiceDefaults/Extensions.cs`. (inherits T037)
- [ ] T034 [US5] BLOCKED (OD-1): create `src/LamuFlix.ServiceDefaults/HealthCheckResponseWriter.cs` with the approved body; no provisional body or default writer. (inherits T038)
- [ ] T035 [US5] BLOCKED (OD-1): create `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` and `HealthCheckStubs.cs`, narrowly adjusting `ApiHostFactory.cs` and `ApiHostCompositionTests.cs` only if required; four readiness states plus the 503 body inspection (trace identifier, no exception text). (inherits T039)
- [ ] T036 [US5] BLOCKED (OD-1): gates and boundary checks for the readiness files. (inherits T040, T041)

**Checkpoint**: if the owner declines the FR-034 deferral, the ticket is `blocked: structural` and Gate 1 stays closed.

## Phase 7: Evidence, Gates and Publication (User Story 7, Priority P2)

- [ ] T037 [US7] Close-out gates over every changed `.cs` file with real-array `-Files`: Roslyn, complexity at the normal and refactor ceilings, InspectCode on the delivery diff, `dotnet format --verify-no-changes`; each receipt includes the clean-harness line from T003. (inherits T043)
- [ ] T038 [US7] Gauge records the real mutation verdict and exit, property-test exit (exit 2 needs a recorded `propertyTests` opt-out reason), vulnerable-package scan and exit, and the web gate result; full `dotnet test` per-project counts against T004; no threshold lowered; surviving mutants get tests. (inherits T042, T044, T045)
- [ ] T039 [US7] Per-task evidence reconciliation in `specs/DEV-307/tasks.md`: tick or note each box only on a cited receipt, including T013-T017 (liveness and WAF delivered through DEV-308 `2ce1e39`) and T001-T004, T006, T008, T018-T026; T020A stays open pending owner-answer text. (inherits T013-T017 and the delivered set)
- [ ] T040 [US7] Final boundary diff `git diff --stat <base>...HEAD` limited to the plan file boundary; evidence table one row per spec success criterion. (inherits T046, T047)
- [ ] T041 [US7] Record the eventual OD-1 answer, or an explicit owner deferral recorded as such, at the DEV-396 equivalent of DEV-307 T048; an explicit deferral is not implementation approval or an inferred answer. BLOCKED until the owner answers. (inherits T048)
- [ ] T042 [US7] Full delivery review (Sentry, Ledger, Compass) with Medium+ closing bar, within the 2-round, 2-fix-commit cap; refactor and architect passes per pipeline.
- [ ] T043 [US7] Publish corrective records: retrospective findings and adjudication summary in the corrective PR; corrective comment on PR #82; DEV-307 tracker comment through Rigger only; Keel merge-bar PR comment. No merge; the terminal state is awaiting the user's merge.

## Dependencies & Execution Order

- Phase 1 first; T003 precedes every gate in every later phase.
- Phase 2 (retrospective) precedes Phases 3-5 so accepted findings can be reproduced against the delivery head before fixes.
- Phases 3, 4, 5 are sequential where they share `TelemetryConstants.cs` or `Extensions.cs` (T015 before T028; T016 before T017 before Phase 6).
- Phase 6 is gated on OD-1 alone; Phases 3-5 and 7 do not wait for it except T041.
- T020A is outside every phase: it stays open and gates nothing planned here.

## Inheritance Map (every DEV-307 task ID)

| DEV-307 task | Final evidence state in DEV-396 | Carried by |
|--------------|---------------------------------|------------|
| T001-T004 | reconcile against delivery head; setup receipts | T001, T002 |
| T005, T007, T009-T012 | delivered with receipts | T028-T032 |
| T006, T008, T022-T026 | already ticked; confirm receipts | T039 |
| T013-T017 | delivered via DEV-308; tick only on receipts | T039 |
| T018-T021 | already ticked; confirm receipts | T039 |
| T020A | stays open; not ticked by inference | Open Items in `plan.md`, T039 |
| T027-T033, T033A | delivered with receipts | T015-T022 |
| T034-T036B | delivered with receipts | T023-T027 |
| Inherited DEV-307 T037-T041 | BLOCKED on OD-1; not counted delivered or deferred | local T033-T036 |
| T042-T047 | delivered with receipts | T037, T038, T040 |
| Inherited DEV-307 T048 | BLOCKED until owner answers OD-1 | local T041 |
