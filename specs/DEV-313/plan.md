# Implementation Plan: DEV-313 API End-to-End Integration Suite

**Branch**: `feature/313-spec` | **Date**: 2026-10-08 | **Spec**: specs/DEV-313/spec.md

**Input**: Feature specification from `/specs/DEV-313/spec.md`

## Summary

Additive real-host integration proof for all nine API operations inside tests/LamuFlix.IntegrationTests, composing existing ApiHostFactory, PostgresFixture, RabbitMqFixture, MovieCatalogSeed and probe patterns. Closed: all nine mapped operations additive (Q1), tests-only disabled playback (Q2), causal import/retry-to-OMDb proof (Q3), lifecycle and configuration (Q4), matrix and filesystem (Q5), ten-file envelope (Q6), loop terms (Q7), ordering and gates (Q8).

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: Microsoft.AspNetCore.Mvc.Testing 10.0.12, WireMock.Net 2.18.0, Testcontainers.PostgreSql 4.15.0, Testcontainers.RabbitMq 4.15.0 (all already pinned; no new dependency)

**Storage**: Real Postgres via Testcontainers PostgresFixture; real RabbitMQ via RabbitMqFixture; WireMock loopback for OMDb

**Testing**: xUnit integration suite in tests/LamuFlix.IntegrationTests

**Target Platform**: Linux/Windows dev and CI with Docker available (Docker server 29.8.2 confirmed at recon)

**Project Type**: Existing test project extension (no new project, folder layer, migration, or API shape change)

**Performance Goals**: Bounded enrichment observation: 30s wall-clock deadline, 50ms poll interval, final failing assertion (EnrichmentConsumerTests.cs:645-660 precedent)

**Constraints**: ClaimLease=00:00:01 strictly less than RetryDelay=00:00:02 (RabbitMqConsumerOptionsValidator.cs:26-33 trap); config before host build (eager capture at PersistenceServiceCollectionExtensions.cs:23-31); production registration order persistence and metadata provider before RabbitMQ consumer; loopback Omdb:BaseUrl; sentinel non-secret ApiKey; no public OMDb; no hardcoded credentials

**Scale/Scope**: size:M; ten additive test files; five mandatory ticket scenarios inside the nine-operation envelope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*
*Constitution: `.specify/memory/constitution.md` v1.2.0 (ratified 2026-09-21, amended 2026-09-28).*

- I (constitution 93-120): Tests remain in the existing IntegrationTests project, compose existing ports/adapters, and add no production project, port, layer or reference-direction change. Design compliant; ArchitectureTests evidence remains pending implementation verification.
- III (147-163): HTTP browse coverage exercises existing typed filters, combined predicates, pagination and stable sorting with NULLS LAST; no dynamic query/sort implementation changes. Design compliant; runtime proof pending.
- IV (169-190): Import/retry use the real publisher and host consumer against real persistence, assert persisted Enriched and existing 409 transitions, and preserve lease/state-machine behavior. No sweeper is assumed or added; this suite does not replace the focused concurrency/retry/DLQ suites. Design compliant; runtime proof pending.
- V (196-213): 422 Unprocessable Entity RFC 7807 ProblemDetails mapped only in the single IExceptionHandler. Validation tests assert that contract. Design compliant; verification pending.
- VI (218-236): Preserve existing ServiceDefaults and production publisher/consumer/provider observability wiring; no new telemetry names, dependency or health wiring. Health-check replacement is not backing-service evidence. No new trace scenario is added to this frozen endpoint seam scope. Design compliant; regression/gate evidence pending.
- VII (241-257): Use fixture-provided connection data and runtime credentials, loopback OMDb with a non-secret sentinel, disabled LocalPlay, and a pre-build injected TimeProvider for lease expiry only. No hardcoded secrets, machine paths or production playback changes. Design compliant; verification pending.
- IX (282-309, NON-NEGOTIABLE): this ticket embodies it — WebApplicationFactory + Testcontainers + WireMock in one suite, infrastructure never mocked (299-301). The playback tripwire is a side-effect boundary held closed by the disabled gate (Q2), not a mocked infrastructure seam. Design compliant; runtime proof pending.
- No new dependency, project (I), schema, API shape, or unrelated existing-file rewrite; no new secrets or machine paths (VII) (Q1/Q6). Design compliant; verification pending.
- Disk-bound reconciliation: test-assertions rule forbids mocking the filesystem and names real infrastructure as the norm; no vendored testing rule prohibits disk in integration tests; real temp folder is an integration-test need (Q5). No departure.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-313/
├── brief.md              # Frozen grill brief (Patron-closed Q1-Q8)
├── CONCLUSIONS.md        # Append-only cited Patron rulings
├── spec.md               # Feature specification
├── plan.md               # This file
└── tasks.md              # Task list
```

### Source Code (repository root)

```text
tests/LamuFlix.IntegrationTests/
├── ApiEndToEndFactory.cs        # Pre-build configuration and host composition
├── ApiEndToEndFixture.cs        # Owned backing infrastructure, reset/cleanup, stubs
├── ApiEndToEndCollection.cs     # Isolated nonparallel collection
├── ApiEndToEndTestBase.cs       # Shared lifecycle, seeding, bounded observations
├── ApiEndToEndBrowseTests.cs    # HTTP filter/paging/sort proof
├── ApiEndToEndLibraryTests.cs   # Details, facets, watchlist persistence proof
├── ApiEndToEndImportTests.cs    # Real scanner/import-to-OMDb proof
├── ApiEndToEndEnrichmentTests.cs# Retry-to-OMDb proof and 409/404
├── ApiEndToEndPlaybackTests.cs  # Disabled LocalPlay and process tripwire
└── ApiEndToEndValidationTests.cs# HTTP 422 ProblemDetails contract
```

**Structure Decision**: Same-project additive files only, composing existing ApiHostFactory, PostgresFixture, RabbitMqFixture, MovieCatalogSeed, MetadataProviderProbe and EnrichmentConsumerTests patterns. No src, migration, CPM/csproj/solution, harness, web or generated-contract edits.

ApiEndToEndFactory reproduces the ApiHostFactory patterns in a direct WebApplicationFactory<Program> implementation because ApiHostFactory is sealed. Fixture settings are applied in CreateHost via ConfigureHostConfiguration/AddInMemoryCollection before base.CreateHost and eager persistence registration; ConfigureAppConfiguration alone is not accepted as proof of that ordering. ConfigureTestServices replaces the TimeProvider and playback-tripwire registrations with the supplied instances; host-resolution identity is asserted. The existing health-check stub and service-provider validation are preserved. Basis: recon-DEV-313 12.3-12.4; brief plan-challenge refinements. A gate- or AC-forced existing-file edit is in scope once its exact necessity is cited (Q6); anything else stops for a Patron ruling; Q8 requires disposition before production behavior changes outside the tests-only remit.

## Complexity Tracking

> No Constitution Check violations to justify. No new project, layer, or speculative abstraction.
