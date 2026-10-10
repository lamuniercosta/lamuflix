# Implementation Plan: DEV-319 TelemetryConstants, named spans, custom metrics, ADR-0008

**Branch**: `feature/319-spec` | **Date**: 2026-10-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/DEV-319/spec.md`; frozen grill `brief.md`; Patron `CONCLUSIONS.md`; facts from recon-DEV-319.

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Add exactly the three ticket-ruled metric instruments (outcome counter, duration histogram, import counter) on the existing `LamuFlix` meter identity, emitted where each result is actually known (enrichment handler, OMDb provider, import handler), while preserving every existing named span and attribute and delivering Accepted `docs/adr/ADR-0008.md` (polling in P1, SignalR deferred). No new package, project, port, schema, API, or polling/SignalR implementation.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: BCL `System.Diagnostics.Metrics` only (no new package); OpenTelemetry packages already pinned and untouched; architecture guard forbids OpenTelemetry in Core.

**Storage**: N/A (no schema or migration change).

**Testing**: xUnit; test-only disposable `MeterListener` capture filtered to exact application meter/instrument names, with thread-safe measurement/tag capture, observing already-published static instruments as well as initial publication; capture helpers nested in their relevant test files with no new shared helper; isolated `DisableParallelization=true` collections preserved (existing integration collections kept, including the existing MetadataProvider nonparallel collection); fixture ownership/lifecycle for the import case stated in the approved `MetricsCollection.cs`/import test file with `DisableParallelization=true`, one collection assignment, and isolation for all application-meter observers; the duplicate-import case reuses the existing Tests.Common `PostgresFixture` migration/reset/context lifecycle with a fixture-backed `EfMovieRepository` inside the approved `ImportMovieFolderCommandHandlerTests.cs` (`ContainerFixture` is a static manually managed helper, not an xUnit fixture): first successful handler import, then a second import with a fresh context/repository against the same database and same `LibraryPath` but a distinct identity so the real database unique index rejects `SaveChangesAsync`, verifying one persisted movie and no second enqueue; capture the first import as one untagged measurement, take a listener baseline before the rejected import, and assert zero additional import measurements; existing real Postgres/RabbitMQ Testcontainers behavior; WireMock for provider/OTLP; duration cases use a test-only BCL `TimeProvider` subclass nested in `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` and/or `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` with explicitly controlled `GetTimestamp` and a matching known `TimestampFrequency` (plus deterministic `GetUtcNow` where mapping needs it), supplied to the existing provider constructor in a focused setup local to those named files with no probe/shared-helper/package/file-map change; timestamps advance explicitly at an awaited transport/test boundary for Found, Failed, and requested cancellation, asserting a known elapsed `TotalSeconds` value and exactly one duration measurement carrying only `provider=omdb`; no wall-clock sleeps.

**Target Platform**: LamuFlix backend (`src/`) and worker (`LamuFlix.Worker`); no UI, no frontend work.

**Project Type**: Existing backend vertical-slice services; instrumentation only.

**Performance Goals**: One measurement per lookup attempt; duration recorded once in `finally`; no per-request meter disposal.

**Constraints**: Exactly three instruments; frozen names, tag keys, and outcome literals (P1); `TelemetryConstants` stays a string-constant catalogue; static meter is process-lifetime and never disposed per request or host.

**Scale/Scope**: Five production files (one new), eight test files/areas (three new/extended per the brief file map); one ADR file.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Meter identity `TelemetryConstants.ActivitySourceName`, no second identity (S1; DEV-307 CONCLUSIONS:71-74; subscribed at `ServiceDefaults/Extensions.cs:50`). No new constant for identity.
- Exactly three ruled instruments, no others (S2; DEV-307 tasks T029).
- Ticket literals frozen; any rename, fourth outcome value, or extra tag is `blocked: structural` (P1).
- Spans and attributes already delivered; preserve and verify, do not re-create (P2).
- Emission edits beyond `TelemetryConstants` are AC1-required, not file-scope rulings (P3); handler placement means no consumer production edit.
- No new NuGet package or `PackageReference` in any project; BCL metrics only; any needed package or port returns to Patron (P4, P5).
- ADR-0008 file/status/scope as ticketed; ADR only, no endpoint/hub/package (P6).
- Schema/API/LocalPlay/secrets/`Process.Start` untouched (P7); `lamuflix.handler.outcome` untouched (P8).
- propertyTests opt-out recorded: pure instrumentation, no new domain invariant; existing property tests still run.
- Phase A runs no executable gates; Phase B runs every applicable pipeline gate with thresholds unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-319/
├── spec.md               # Feature specification (/speckit-specify output)
├── plan.md               # This file (/speckit-plan output)
├── tasks.md              # Phase 2 output (/speckit-tasks output)
├── brief.md              # Frozen grill brief (authority, not Spec Kit output)
└── CONCLUSIONS.md        # Patron rulings (authority, not Spec Kit output)
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/
│   ├── Pipeline/
│   │   ├── TelemetryConstants.cs      # add ruled literal constants only
│   │   └── EnrichmentMetrics.cs       # NEW: static BCL instrument holder
│   └── Features/
│       ├── Enrichment/
│       │   └── ProcessEnrichmentCommandHandler.cs  # emit lookup outcomes
│       └── Import/
│           └── ImportMovieFolderCommandHandler.cs  # Add(1) after save
├── LamuFlix.Infrastructure/
│   └── Adapters/
│       └── OmdbMetadataProvider.cs    # record whole-lookup duration
└── LamuFlix.ServiceDefaults/
    └── Extensions.cs                  # unchanged composition path

tests/
├── LamuFlix.UnitTests/
│   ├── Pipeline/
│   │   ├── EnrichmentMetricsTests.cs   # NEW: instrument contract + local capture helper
│   │   └── MetricsCollection.cs        # NEW: DisableParallelization=true collection
│   └── Features/
│       ├── Enrichment/
│       │   └── ProcessEnrichmentMetricsTests.cs  # NEW: focused handler metric tests
│       └── Import/
│           └── ImportMovieFolderCommandHandlerTests.cs  # extend + isolation collection
└── LamuFlix.IntegrationTests/
    ├── EnrichmentConsumerTests.cs       # extend lookup/retry/refused-claim/cancellation
    ├── MetadataProviderLookupTests.cs   # extend duration + trace verification
    ├── MetadataProviderTelemetryTests.cs# extend duration + trace verification
    └── ServiceDefaultsTelemetryTests.cs # actual instrument export via host composition

docs/
└── adr/
    └── ADR-0008.md  # NEW: Accepted, polling in P1, SignalR deferred
```

**Structure Decision**: Existing projects and folders only. The static holder goes in the existing `src/LamuFlix.Core/Pipeline/` folder beside `TelemetryConstants`; this is file organization, not a new layer, port, or abstraction (Q3 ratified). No csproj, `ServiceDefaults` registration, consumer production, endpoint, or generated-contract change.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations. No table entries.
