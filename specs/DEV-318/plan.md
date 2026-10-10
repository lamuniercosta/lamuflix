# Implementation Plan: DEV-318 End-to-End Import-to-Enrichment Integration Proof

**Branch**: `feature/318-spec` | **Date**: 2026-10-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/DEV-318/spec.md`

## Summary

Extend the existing ApiEndToEnd import scenario into a deterministic Pending-to-Enriched
proof plus request-correlated trace ancestry proof, using only the real stack (API host with
active EnrichmentConsumer, Testcontainers Postgres/RabbitMQ, WireMock OMDb) and a test-only
scoped outer gate around the fully decorated production enrichment handler. No production,
contract, schema, or dependency change.

## Technical Context

**Language/Version**: C# 14 / .NET 10

**Primary Dependencies**: xUnit v3, Shouldly, Testcontainers (PostgreSQL,
RabbitMQ), WireMock.Net, WebApplicationFactory; no new packages

**Storage**: PostgreSQL (Testcontainers; existing fixture); no migration, no schema change

**Testing**: `dotnet test`; targeted scenario first, then full suite via Gauge

**Target Platform**: API integration-test host

**Project Type**: Integration-test-only change in `tests/LamuFlix.IntegrationTests`

**Performance Goals**: Existing 30-second integration wait budget with cancellation; bounded
gate timeout; bounded post-Enriched span wait so persistence cannot race capture

**Constraints**: `TreatWarningsAsErrors=true`; `TimeProvider` for time (no `DateTime.Now`
family); `CancellationToken` end to end; no sync-over-async; ValidateScopes/ValidateOnBuild
must pass; no secrets or machine paths in source; scenario-level try/finally releases the
gate before host disposal and guarantees capture/listener disposal with no blocking gate
or leaked listener on success and failure exits

**Scale/Scope**: One extended scenario plus optional test-only helper extractions beside it;
no independently broader implementation phase

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Ports and Adapters**: PASS. No new port, adapter, project, or reference change. The
  gate wraps the existing `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`
  registration from outside its decorator chain; decorator order Tracing, Logging, Validation,
  handler is preserved untouched.
- **II. Explicit Handlers**: PASS. No handler, command, or registration change in production;
  no MediatR, reflection dispatch, or second dispatch mechanism introduced.
- **III. Typed filters**: N/A. No query or sort surface touched.
- **IV. Enrichment state machine**: PASS. Transitions Pending to Enriched exercised through
  the real claim path (`TryClaimForEnrichmentAsync`); no transition method added or changed.
- **V. Errors**: PASS. No error shape or status mapping change; 202 with Location and GET 200
  geometries unchanged.
- **VI. Observability**: PASS. Proves one trace API to RabbitMQ to consumer to provider with
  `TelemetryConstants` names; no ad-hoc telemetry literals; no new span or metric names.
- **VII. Config/time**: PASS. Sentinel and container-generated settings only; `TimeProvider`
  and test-clock seams; no secret, path, or magic-string change.
- **VIII. Language**: PASS. No new domain term; vendor name OMDb stays on the adapter only.
- **IX. Test pyramid**: PASS. Real Postgres, RabbitMQ, WireMock; no mocked infrastructure, no
  EF InMemory, no fixed delay; xUnit v3, Shouldly, Arrange-Act-Assert.

Re-check after Phase 1 design: no design element alters the above; the gate adds no Activity,
creates no provider, retains no scoped services, and adds no port or layer.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-318/
├── spec.md                                                # Feature specification (/speckit-specify output)
├── plan.md                                                # This file (/speckit-plan output)
├── research.md                                            # Phase 0 output
├── data-model.md                                          # Phase 1 output
├── quickstart.md                                          # Phase 1 output
├── checklists/                                            # L checklist output (/speckit-checklist)
├── tasks.md                                               # Phase 2 output (/speckit-tasks command)
└── adr-0020-request-correlated-import-enrichment-proof.md  # Gate 1 review/preservation copy
```

Canonical Phase B publication location:
`docs/adr/0020-request-correlated-import-enrichment-proof.md` (delivery PR only; the
specs copy above is the durable Gate 1 review/preservation artifact).

No `contracts/` directory: FR-005 authorizes no public API, OpenAPI, or generated TypeScript
change, so there is no contract to record.

### Source Code (repository root)

```text
tests/
└── LamuFlix.IntegrationTests/
    ├── ApiEndToEndImportTests.cs      # Extend existing scenario (default: nested helpers)
    ├── ApiImportEnrichmentGate.cs     # Optional extraction only if clarity/gates require
    └── ApiImportTraceCapture.cs       # Optional extraction under the same condition
```

**Structure Decision**: Test-only change inside `tests/LamuFlix.IntegrationTests`.
`ApiEndToEndFixture`, `ApiEndToEndFactory`, and `ApiEndToEndTestBase` remain unchanged.
Default is private nested synchronization/capture helpers in the extended test file; extract
to the sibling files only if clarity or gate results require it. No new layer, project, or
architectural seam.

**Proof clarifications** (brief.md Sentry addendum; positive path only):

- Original-descriptor capture: assert exactly one descriptor matching
  `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`, assert its scoped
  lifetime and non-null ImplementationFactory, capture that exact factory, then replace only
  that descriptor.
- Ambient Activity precondition: after request preparation, immediately before sending the
  POST, assert `Activity.Current` is null; no propagation suppression, no additional Activity.
- Measurement boundary: measurement relies on the existing fixture warm-up followed by
  WireMock Reset; assert exactly one measured OMDb GET filtered by the scenario sentinel
  apikey and selected t/type values, preserving existing method/path/query, parameter, and
  secret-scrub assertions. Retry-path behavior is outside this proof; a second matching
  request fails the positive-path count assertion. No added Reset, fixture change, duplicate
  suppression, count weakening, or resilience change.
- Cleanup ownership: the scenario-level try/finally owns cleanup on both success and failure
  exits; its finally releases the gate before host disposal and guarantees capture/listener
  disposal on either exit. Explicitly verify release-before-host-disposal and capture/listener
  disposal on both exits. No new disposal abstraction and no separate business failure-path
  scenario.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations. No entries.
