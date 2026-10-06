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
- IDs written "inherited Txxx" refer to `specs/DEV-307/tasks.md`; local IDs are DEV-396 tasks. Local T041 carries inherited T048. A suffixed local ID (T019A) is a second half of its unsuffixed task, stays in the same phase, and is never an inherited ID.
- After every `.cs` task, run the incremental Roslyn, complexity, InspectCode and format pass with `-Files` passed as a real array. The close-out pass is T037.
- A red run is re-run at most once and only to record that it did not reproduce; never retried until green.
- No task ticks OD-1 or T020A by inference. A BLOCKED task is reported and waited on, never started provisionally.

## Phase 1: Pickup, Recon and Harness Hygiene

**Purpose**: Establish current facts and a clean gate baseline before any change.

- [ ] T001 Pickup drift check of every file in `plan.md` File Boundary against `origin/main`, the range head `7a35e77` ancestry, and `recon-DEV-396`; record deltas and the delivery-base HEAD SHA (Wisp). Drift that invalidates a recon citation goes to Keel. (inherits T001, T003, T004)
- [ ] T002 [P] Record the Q2 state as open in the task note, verbatim from `spec.md` OD-1; record the inherited DEV-307 T020A states separately rather than as one missing answer: the recorded 2026-10-03 Patron approval of N2 first half under charter 2.3(6) (`spec.md` Owner Decisions, `CONCLUSIONS.md` Q9), inherited DEV-307 Q1 evidence still unproved, T020A implementation and delivery receipts still unproved, and the inherited N2 second half open as the separate unchecked OD-2 question. The approval is never recorded as an implementation or delivery receipt, and nothing here ticks inherited DEV-307 T020A, OD-1 or OD-2. (inherits T002)
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
- [ ] T012 [US6] Record each still-live Medium+ finding's file as a narrow addition to `plan.md` File Boundary before editing; Cog applies the narrow fix with a test, inside the historical review budget: at most 2 rounds of at most 2 fix commits each, which never lends to and is never enlarged by the delivery budget in T042. A round is one three-axis review of a pinned HEAD plus its adjudication, its remediation and its own closure check, and a commit closing several findings counts once; record-only commits, a rebase on main and pre-review gauge fixes consume neither. On exhaustion - a Medium+ finding still open after round 2's closure check, or a third fix commit needed inside a round - stop and report once to Bernstein `blocked: review cap exhausted - DEV-396 historical <finding ids>`; no further commit, no third round, no extra immutable review, no severity downgrade, no waiver, no merge-bar sign-off, and the task stays not delivered.
- [ ] T013 [US6] Lower findings: write follow-up records; open a ticket only for Critical/High or broken behaviour (Patron decides tracker recording, Rigger writes it); otherwise "noted, no ticket". S7, the differing `error.type` conventions between the decorator, the consumer and the OMDb category, is recorded here as noted with no ticket: no consumer, classifier or earlier ADR is edited and no production consistency fix is made.
- [ ] T014 [US6] Retain all historical artifacts, then retire the disposable checkout.

**Checkpoint**: historical and delivery receipts are separate; no Medium+ finding is open.

## Phase 3: OpenTelemetry Composition and Redaction Proof (User Stories 1 and 2, Priority P1)

**Goal**: Traces, metrics and logs composed in `AddServiceDefaults`, with the OMDb sentinel proof in the same commit as HttpClient instrumentation.

**Independent Test**: Providers resolve with exporters; each instrumentation removal fails its own check; sentinel absent from spans and logs.

- [ ] T015 [US1] (Cog) Add `RabbitMqPublisherActivitySourceName` ("RabbitMQ.Client.Publisher") and `RabbitMqSubscriberActivitySourceName` ("RabbitMQ.Client.Subscriber") to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`; no rename or removal; copy strings exactly. (inherits T027)
- [ ] T016 [US1] (Cog) In `src/LamuFlix.ServiceDefaults/Extensions.cs`, add tracing (ASP.NET Core, HttpClient, Npgsql, three `AddSource` names) with argument-free `AddOtlpExporter()`; no `Endpoint`, `Protocol`, delegate or `AddService`. Same commit as T018 proof for the HttpClient instrumentation. Before this edit and again after that shared commit, Anvil records the IntegrationTests suite duration and the slowest `ApiHostFactory` disposal; disposal over 12 s or suite growth over 25 percent stops the phase and comes back to Patron as a Medium finding, and is never silenced by test-host exporter overrides. (inherits T028)
- [ ] T017 [US1] (Cog) On the same `AddOpenTelemetry()` call add metrics (ASP.NET Core, HttpClient, `AddMeter` on the existing identity) and logging with argument-free exporters; no invented identity or Npgsql metric. (inherits T029)
- [ ] T018 [US2] (Cog edits the probe; Anvil creates the telemetry test) Default `IHttpClientFactory` URI redaction is the protection, so there is no production logging change: no suppression, no `System.Net.Http.HttpClient.*` level filter, no extended-logging package, no credential literal, and `MetadataProviderServiceCollectionExtensions.cs` keeps health membership as its only edit. Extend `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` in exactly three ways - propagate `categoryName` on every record, capture `Debug` and above, and record scope state with active scope payloads attached to each record, because the `HTTP {HttpMethod} {Uri}` scope carries the URI and an absence check that cannot see it is vacuous. Then create `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` and assert, before any absence check: at least 1 record whose category starts with `System.Net.Http.HttpClient.` and ends in `.LogicalHandler` or `.ClientHandler`, read from the capture and never from a hardcoded typed-client name, of which at least 1 carries a structured `Uri` containing the stub server host and path and neither `apikey` nor the sentinel (positive redaction evidence; the exact `?*` rendering is not asserted); and at least 1 stopped `ActivityKind.Client` activity from the HttpClient instrumentation for the stub host plus the `LamuFlix` `Metadata.Lookup` activity. Then assert, over every captured activity and every captured record including resilience-handler records, that the sentinel and its `Uri.EscapeDataString` form appear in no tag key or value (stringified `TagObjects`), event or status description, and in no log category, rendered message, structured state key or value, scope payload or exception text. No forced failure-path case; `sentinel-key` is a test sentinel, not a credential literal. If the sentinel appears, that is a Medium+ finding that returns to Patron and is never met by an opt-out, a suppression or a weaker assertion. If the new process-wide provider changes what the existing capture tests observe (`MetadataProviderLookupTests.cs:525-556`, `EnrichmentConsumerTests.cs:276-281`), narrow correlation inside that test (source, operation name or trace id); global telemetry is never disabled and no test is skipped. Committed in the same commit as T016, coordinated between Cog and Anvil. (inherits T033A)
- [ ] T019 [US1] (Cog edits existing file) Extend `tests/LamuFlix.UnitTests/ServiceDefaultsTests.cs`: the tracer, meter and log providers resolve from the container that the production `AddServiceDefaults()` built; test names say "exporter is composed", never "exported". Exporter attachment, the six recorded sources and removal sensitivity are proved in T019A on the production-composed host, never by test-local recomposition and never by a public knob, so no UnitTests driver mock, project edit or new dependency is used here. (inherits T030)
- [ ] T019A [US1] (Anvil creates the two new files) Create `tests/LamuFlix.IntegrationTests/TelemetryCompositionCollection.cs` (`DisableParallelization = true`, holding `ICollectionFixture<PostgresFixture>` and `ICollectionFixture<RabbitMqFixture>`) and `tests/LamuFlix.IntegrationTests/ServiceDefaultsTelemetryTests.cs`, all on one production `WebApplicationFactory<Program>` host (`ApiHostFactory.cs:14`). (a) Exporter presence: set `OTEL_EXPORTER_OTLP_ENDPOINT` to the already-referenced WireMock stub and `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`, emit one span, one metric and one `ILogger` record through Serilog `writeToProviders`, call `ForceFlush` on each provider with an explicit timeout and assert it returned true, then assert the stub received `/v1/traces`, `/v1/metrics` and `/v1/logs`; the test is named "exporter is composed" and never asserts the stub's payload content. (b) Six recorded sources: the test `ActivityListener` is a passive observer whose `Sample` returns `None` and whose `SampleUsingParentId` returns `None` or is left unset, sitting beside the production-composed listener, which alone creates and records; it never returns `PropagationData`, `AllData` or `AllDataAndRecorded`, because any of those would make the test create the activities it claims to observe. A row passes only when the observer's `ActivityStopped` sees an activity from that source with both `IsAllDataRequested` and `Recorded` true. Each source needs a real trigger: an in-process request (ASP.NET Core), an outbound request to the WireMock stub (HttpClient), a real query against `PostgresFixture` (Npgsql), the real `LamuFlix` lookup emission site (`OmdbMetadataProvider.cs:47`, application source), and `BasicPublishAsync` plus `BasicGetAsync` against `RabbitMqFixture` (`RabbitMqProbe.cs:100,134`) for the RabbitMQ publisher and subscriber sources; emission is proved, not subscription. Read the exact source names from the first execution and pin them as test constants. (c) Removal sensitivity, three registrations only: each row asserts only its own source, so removing one registration can fail only its own row, and each row first runs a negative control proving that before the production host is built the same trigger yields no recorded activity from that source. Then make three local uncommitted removals of `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation` and `AddNpgsql` in `Extensions.cs`, record for each that exactly its own row fails while the other rows pass, and restore the file so `git diff --quiet -- src/LamuFlix.ServiceDefaults/Extensions.cs` exits 0; those receipts are the removal evidence. A host that never calls `AddServiceDefaults` is the negative control, never the removal proof. (d) Environment mutation lives only in this non-parallel collection and is restored in `finally`, and every host and provider is disposed in `finally`. If the production host yields zero qualifying activities on the first execution, stop and report to Patron; the observer's sampling, the environment and the row count are never changed to make a row pass. (inherits T030)
- [ ] T020 [US1] (Anvil) Create `tests/LamuFlix.UnitTests/OtlpEndpointEnvironmentTests.cs` in a non-parallel collection: resolve `OtlpExporterOptions` from the container that the production `AddServiceDefaults()` built (`ServiceDefaultsTests.cs:143-151` precedent), then assert the default endpoint, shared options across all three providers, and the standard environment variable winning; mutate process environment and restore in `finally`. Whether the options resolve from that container is unverified: if they do not, stop and report to Patron rather than dropping the default-endpoint assertion or constructing a fresh `OtlpExporterOptions`. (inherits T031)
- [ ] T021 [US1] Gates on `TelemetryConstants.cs`, `Extensions.cs`, `ServiceDefaultsTests.cs`, `OtlpEndpointEnvironmentTests.cs`, `MetadataProviderProbe.cs`, `MetadataProviderTelemetryTests.cs`, `ServiceDefaultsTelemetryTests.cs`, `TelemetryCompositionCollection.cs`, then `dotnet test` for UnitTests and IntegrationTests; record exits. (inherits T032)
- [ ] T022 [US1] Boundary checks: `rg -n "Endpoint\s*=" src/LamuFlix.ServiceDefaults` returns nothing; `rg -n "DisableUriRedaction|DISABLE_URL_QUERY_REDACTION" src tests` returns nothing; double composition does not double Serilog or OpenTelemetry registrations. (inherits T033)

**Checkpoint**: US1 and US2 delivered with receipts; no composition test is described as collector delivery.

## Phase 4: Health Membership (User Story 3, Priority P1)

- [X] T023 [P] [US3] (Anvil) Create `src/LamuFlix.Core/Pipeline/HealthCheckTags.cs` with `Ready = "ready"` beside `TelemetryConstants`. (inherits T034)
- [X] T024 [US3] (Cog) In `PersistenceServiceCollectionExtensions.cs` register the PostgreSQL check over `LamuFlixDbContext` tagged with the constant; in `RabbitMqServiceCollectionExtensions.cs` replace the literal with the constant; no schema or migration, no driver mock. (inherits T035)
- [X] T025 [US3] (Cog) In `MetadataProviderServiceCollectionExtensions.cs` remove the readiness tag and leave the check registered. (inherits T036)
- [X] T026 [US3] (Anvil) Create `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs` reading the real registrations (independent of endpoint stubs): exactly two ready-tagged checks (postgres, rabbitmq), metadata-provider registered and untagged. (inherits T036A)
- [ ] T027 [US3] Gates on the four production files and the new test; `rg -n 'tags:\s*\[\"ready\"\]' src` returns nothing; boundary check on `MapHealthChecks|MapDefaultEndpoints` sites unchanged. (inherits T036B, T041 non-readiness half)

**Checkpoint**: readiness group membership proven; no readiness route yet.

## Phase 5: Handler Outcome Decorator (User Story 4, Priority P1)

- [X] T028 [P] [US4] (Cog) Add the handler-outcome key and validation-failed value constants to `TelemetryConstants.cs` after T015 (sequential, same file). (inherits T005)
- [X] T029 [US4] (Cog) In `TracingDecorator.cs` separate validation failures (status unset, outcome validation-failed) from other exceptions (error status, full type name, `error.type`), rethrowing both unchanged; leave the consumer `MarkError` untouched. (inherits T007)
- [X] T030 [US4] (Anvil) Create `tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs`: two `[Fact]` cases with an `ActivityListener` covering status, attributes and propagation; the unexpected-exception case asserts the full type name and executes in the active project. (inherits T009)
- [X] T031 [US4] (Cog) Edit the one assertion at `tests/LamuFlix.Test/TracingDecoratorTests.cs:65` to the full-name value; record the standalone legacy check as `Could not run` (project not in `LamuFlix.sln`, NU1010); never present it as proof. (inherits T010)
- [ ] T032 [US4] Gates on the changed files and `dotnet test`; boundary diff shows only plan files. (inherits T011, T012)

**Checkpoint**: both span paths proven by an executing active test.

## Phase 6: Conditional Readiness (User Story 5, Priority P2)

**Do not start until the owner answers OD-1 including the FR-034 deferral clause. Not counted as delivered; not reported as deferred.**

- [ ] T033 [US5] map `/health/ready` filtered by the shared tag in `src/LamuFlix.ServiceDefaults/Extensions.cs`. (inherits T037)
- [X] T034 [US5] create `src/LamuFlix.ServiceDefaults/HealthCheckResponseWriter.cs` with the approved body; no provisional body or default writer. (inherits T038)
- [ ] T035 [US5] create `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` and `HealthCheckStubs.cs`, narrowly adjusting `ApiHostFactory.cs` and `ApiHostCompositionTests.cs` only if required; four readiness states plus the 503 body inspection (trace identifier, no exception text). (inherits T039)
- [ ] T036 [US5] gates and boundary checks for the readiness files. (inherits T040, T041)

**Checkpoint**: the owner accepted the FR-034 deferral (comment 7-269).

## Phase 7: Evidence, Gates and Publication (User Story 7, Priority P2)

- [ ] T037 [US7] Close-out gates over every changed `.cs` file with real-array `-Files`: Roslyn, complexity at the normal and refactor ceilings, InspectCode on the delivery diff, `dotnet format --verify-no-changes`; each receipt includes the clean-harness line from T003. (inherits T043)
- [ ] T038 [US7] Gauge records the real mutation verdict and exit, property-test exit (exit 2 needs a recorded `propertyTests` opt-out reason), vulnerable-package scan and exit, and the web gate result; full `dotnet test` per-project counts against T004; no threshold lowered; surviving mutants get tests. (inherits T042, T044, T045)
- [ ] T039 [US7] Per-task evidence reconciliation in `specs/DEV-307/tasks.md`: tick or note each box only on a cited receipt, including T013-T017 (liveness and WAF delivered through DEV-308 `2ce1e39`) and T001-T004, T006, T008, T018-T026; inherited DEV-307 T020A stays unticked, because the recorded N2 first-half approval is not an implementation or delivery receipt and inherited DEV-307 Q1 evidence plus the T020A implementation and delivery receipts remain unproved; its inherited N2 second half stays an open and separate owner decision (OD-2), which no Patron answer and no first-half approval closes, and no Kestrel work is done without explicit user approval. (inherits T013-T017 and the delivered set)
- [ ] T040 [US7] Final boundary diff `git diff --stat <base>...HEAD` limited to the plan file boundary; evidence table one row per spec success criterion. (inherits T046, T047)
- [ ] T041 [US7] Record the OD-1 owner answer (YouTrack comment 7-269: approved as worded including the deferral clause; OD-2: retain default WebApplicationFactory, no Kestrel), at the DEV-396 equivalent of DEV-307 T048; an explicit deferral is not implementation approval or an inferred answer. (inherits T048)
- [ ] T042 [US7] Full delivery review (Sentry, Ledger, Compass) with Medium+ closing bar, inside the delivery review budget: at most 2 rounds of at most 2 fix commits each, which never lends to and is never borrowed from the historical budget in T012; the round, fix-commit and exhaustion rules are T012's and are applied here unchanged, with the report naming `delivery`. Refactor and architect passes per pipeline.
- [ ] T043 [US7] Publish corrective records: retrospective findings and adjudication summary in the corrective PR; corrective comment on PR #82; DEV-307 tracker comment through Rigger only; Keel merge-bar PR comment. No merge; the terminal state is awaiting the user's merge.
- [ ] T044 [US7] (Rigger) ADR-R4 close-out in the DEV-396 delivery PR before review closes: change `docs/adr/0018-observability-composition-in-service-defaults.md` Status from `Proposed` to `Accepted` with the date, and re-word any BLOCKED readiness sentence to match the owner's Q2 answer; an absent answer stays BLOCKED and is never inferred, and no earlier ADR is edited (CONCLUSIONS.md ADR-R4; task-pipeline:74). Quill authors this task line; Rigger executes it in the delivery PR. Nothing is reassigned and the `Proposed` status and owner guards stand until the PR.

## Dependencies & Execution Order

- Phase 1 first; T003 precedes every gate in every later phase.
- Phase 2 (retrospective) precedes Phases 3-5 so accepted findings can be reproduced against the delivery head before fixes.
- Phases 3, 4, 5 are sequential where they share `TelemetryConstants.cs` or `Extensions.cs` (T015 before T028; T016 before T017 before Phase 6).
- T019A runs after the shared T016 and T018 commit lands, inside its own non-parallel collection, and never runs while the UnitTests provider-resolution collection is alive; the three uncommitted removals in T019A(c) are restored before the phase gate in T021 runs.
- Phase 6 is gated on OD-1 alone; Phases 3-5 and 7 do not wait for it except T041.
- Inherited DEV-307 T020A is outside every phase and gates nothing planned here. It stays unticked: its N2 first-half approval is recorded, inherited DEV-307 Q1 evidence and its implementation and delivery receipts are unproved, and its N2 second half is the open unchecked OD-2 question with the default `WebApplicationFactory` retained meanwhile.

## Inheritance Map (every DEV-307 task ID)

| DEV-307 task | Final evidence state in DEV-396 | Carried by |
|--------------|---------------------------------|------------|
| T001-T004 | reconcile against delivery head; setup receipts | T001, T002 |
| T005, T007, T009-T012 | delivered with receipts | T028-T032 |
| T006, T008, T022-T026 | already ticked; confirm receipts | T039 |
| T013-T017 | delivered via DEV-308; tick only on receipts | T039 |
| T018-T021 | already ticked; confirm receipts | T039 |
| T020A | unticked, never by inference: N2 first-half approval recorded; inherited DEV-307 Q1 evidence, implementation and delivery receipts unproved; N2 second half open as OD-2 | Open Items in `plan.md`, OD-2 in `spec.md`, T039 |
| T027-T033, T033A | delivered with receipts | T015-T022 |
| T034-T036B | delivered with receipts | T023-T027 |
| Inherited DEV-307 T037-T041 | OD-1 approved (7-269); scheduled | local T033-T036 |
| T042-T047 | delivered with receipts | T037, T038, T040 |
| Inherited DEV-307 T048 | OD-1 approved (7-269); scheduled | local T041 |
