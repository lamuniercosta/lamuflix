# Implementation Plan: DEV-310 Minimal API Endpoints

**Branch**: `feature/310-spec` | **Date**: 2026-10-06 | **Spec**: `specs/DEV-310/spec.md`

**Input**: Feature specification from `specs/DEV-310/spec.md`, frozen `brief.md`, `CONCLUSIONS.md` Q1-Q10, `recon-DEV-310`

## Summary

Expose the seven ticket routes as Minimal API endpoints over the existing Core handlers and a facet-read extension of the existing `IMovieCatalog`, following the brief ordering: facet contract and reads first, then command endpoints with exactly two error-mapping arms, then feature-group wiring, then real-host proof, then Cog whole-diff refactor and Gauge gates. Each Anvil implementation handoff passes tests independently: foundational focused tests pass in Phase 2, story slices add implementation only with no new failing host tests, and all full-host assertions are written post-wiring in the proof phase where they pass. OpenAPI document, snapshot, and drift work stay deferred to DEV-20 per the Q6 owner approval.

## Technical Context

**Language/Version**: C# with .NET 10

**Primary Dependencies**: ASP.NET Core Minimal APIs with TypedResults; existing Core handler and decorator chain; no new NuGet/npm dependency

**Storage**: Existing Postgres tables and skip-navigation tables for movie genres, actors, directors; no schema change or migration

**Testing**: xUnit with Shouldly and NSubstitute; WebApplicationFactory host tests; Testcontainers Postgres and RabbitMQ; MockFileSystem for the scanner; no EF InMemory, no driver mocks, no new assertion or mocking library

**Target Platform**: Linux server API plus local-play execution path behind `Features:LocalPlay`

**Project Type**: Backend API feature slice (no web work)

**Performance Goals**: N/A beyond existing gate behavior

**Constraints**: Exact ticket statuses, headers, empty command bodies, scalar facet arrays, and ProblemDetails error shape; DEV-394 fail-closed playback baseline preserved; `TimeProvider` for time and injected seeded `Random` for randomness

**Scale/Scope**: Seven routes; file envelope per brief.md Q7

## Constitution Check

- Principle I (ports and adapters): facet reads extend the whitelisted `IMovieCatalog`; no new port, no whitelist relaxation, no ADR.
- Principle II (vertical slices): sealed query records and handlers in the existing Features/Library slice; per-feature `MapXxxEndpoints`; explicit decorated registration; Tracing, Logging, Validation, handler order unchanged.
- Principle V (errors): single exception handler gains exactly `InvalidTransitionException` to 409 and `FeatureDisabledException` to 403; existing 404, 422, and generic 500 preserved; no exception text leaks.
- Principle VII (local execution): no launcher, config, flag, or registration change; lookup-before-launch precedence kept; recording substitute tests only, never a real process.
- Principle IX (tests): Testcontainers and WebApplicationFactory proof; NSubstitute only for Core ports or the safe process seam; `propertyTests: opt-out` with the brief reason recorded in the task note.
- Contract chain: C# DTO to OpenAPI to generated TypeScript; DEV-310 supplies complete metadata; document, snapshot, and drift ownership stays with DEV-20 per the Q6 owner approval and is never claimed green here.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-310/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Api/
│   ├── Endpoints/
│   │   ├── ImportEndpoints.cs
│   │   ├── EnrichmentEndpoints.cs
│   │   ├── WatchlistEndpoints.cs
│   │   ├── PlaybackEndpoints.cs
│   │   ├── FacetEndpoints.cs
│   │   ├── ImportMovieRequest.cs
│   │   ├── GenreDto.cs
│   │   ├── PersonDto.cs
│   │   └── ApiEndpoints.cs
│   ├── HandlerRegistration.cs
│   └── ExceptionHandling/
│       └── ValidationExceptionHandler.cs
├── LamuFlix.Core/
│   ├── Ports/
│   │   └── IMovieCatalog.cs
│   └── Features/
│       └── Library/
│           ├── GetGenresQuery.cs
│           ├── GetPeopleQuery.cs
│           └── handlers
└── LamuFlix.Infrastructure/
    └── existing catalog adapter (facet read projections)

tests/
├── LamuFlix.IntegrationTests/
│   └── new matching endpoint host tests
├── LamuFlix.UnitTests/
│   └── Features/Library/ facet handler tests
└── existing command, worker, facet persistence, and DEV-394 playback tests retained
```

**Structure Decision**: Existing vertical-slice layout is kept; new endpoint, DTO, query, and handler files land in the slices named above, with the catalog adapter identified by `IMovieCatalog` ownership rather than a guessed filename.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitution departure beyond the Q6 owner-approved OpenAPI deferral | N/A |
