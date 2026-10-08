# Implementation Plan: DEV-316 StrandedMovieSweeper

**Branch**: `feature/316-spec` | **Date**: 2026-10-08 | **Spec**: `specs/DEV-316/spec.md`

**Input**: `specs/DEV-316/spec.md` (from `specs/DEV-316/brief.md`, Patron Q10 closure)

## Summary

Deliver the constitution-required periodic recovery of stranded `Pending` rows (null last attempt or expired claim lease) by re-enqueueing through the existing `RequeueStrandedMoviesCommand/Handler` (`IEnrichmentQueue`) with `EnrichmentOptions` lease/sweep durations and injected `TimeProvider`. Thin `BackgroundService` loop in Infrastructure hosted unconditionally by Api; selection/dispatch decisions in a new Core handler; positive-duration options validator; real PostgreSQL/RabbitMQ recovery evidence.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: Microsoft.Extensions (Hosting, DI, Options + `IValidateOptions`/`ValidateOnStart`), EF Core (IDs-only no-tracking query), existing `IEnrichmentQueue`/RabbitMQ publisher; no new packages

**Storage**: Existing PostgreSQL (`Pending`, `LastAttemptAt`, `EnrichmentAttempts` columns; no schema/migration change); RabbitMQ `enrichment.requested` via existing publisher

**Testing**: xUnit v3, NSubstitute, Shouldly, AutoFixture; Testcontainers PostgreSQL + RabbitMQ for recovery evidence; test-owned timer-capable `ManualTimeProvider`; bounded observable synchronization, no wall-clock sleeps

**Target Platform**: Linux containers / .NET 10 API host (`LamuFlix.Api`)

**Project Type**: Backend ports-and-adapters service with feature-organised Core (constitution:93-95; NOT vertical-slice) (Core + Infrastructure + Api + ServiceDefaults)

**Performance Goals**: Serial completion-based cadence (full `SweepInterval` delay after each completed or failed pass); long passes lengthen effective cadence; no fixed maximum recovery time promised; no batch-size limit

**Constraints**: Strict `<` lease boundary; no sweep mutation/claim/attempt change; cancellation everywhere; Error-with-exception logging; `TreatWarningsAsErrors`; complexity 15 implement / 6 refactor; mutation 80 (Api excluded only); AAA-header-only test comments

**Scale/Scope**: Size M; bounded file set: 9 new + 10 narrowly scoped edits per `brief.md`

## Constitution Check

- Thin consumer/host, decisions in Core handlers (principles I/II): PASS (selection/dispatch in Core handler; host does timing/scopes/logging only).
- `TimeProvider` for time, no `DateTime.Now` (principle VII): PASS (injected `TimeProvider`, one `now` per pass).
- No mocked infrastructure for query translation/serialisation; real PostgreSQL/RabbitMQ via Testcontainers (principle IX): PASS (EF matrix + recovery on real PostgreSQL; recovery on real RabbitMQ via Testcontainers).
- Sweeper eligibility and recovery contract (principle IV): PASS (`Pending` null-or-strictly-expired predicate, no sweep mutation/claim, dispatch via existing requeue path).
- Existing conventions preserved (principle II: handlers/decorators and result conventions; principle VIII: vocabulary; Coding Conventions: `MovieId`/value objects; no duplicate DTOs): PASS.
- No new dependency/project/layer/schema/public API/LocalPlay/secret (principles I/II; principle VII for LocalPlay/secrets): PASS (internal port member only; subfolder under existing project; composition-root registrations only).
- No threshold edits, no generated-file edits (stack and workflow rules): PASS (thresholds read from committed `harness.yml` at delivery stage).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-316/
├── brief.md              # Patron Q10 grill closure (authoritative input)
├── CONCLUSIONS.md        # Q1-Q10 full exchanges (append-only)
├── spec.md               # Feature specification
├── plan.md               # This file
└── tasks.md              # Phase 2 output
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/
│   ├── Features/Enrichment/
│   │   ├── SweepStrandedMoviesCommand.cs            # NEW
│   │   └── SweepStrandedMoviesCommandHandler.cs     # NEW
│   ├── Options/
│   │   └── EnrichmentOptionsValidator.cs            # NEW
│   └── Ports/
│       └── IMovieRepository.cs                      # EDIT: FindStrandedMovieIdsAsync
├── LamuFlix.Infrastructure/
│   ├── Enrichment/
│   │   └── StrandedMovieSweeper.cs                  # NEW (thin BackgroundService)
│   └── Persistence/
│       └── EfMovieRepository.cs                     # EDIT: IDs-only no-tracking selection
├── LamuFlix.Api/
│   ├── Program.cs                                   # EDIT: unconditional AddHostedService only
│   └── HandlerRegistration.cs                       # EDIT: AddHandler sweep + requeue
└── LamuFlix.ServiceDefaults/
    └── Extensions.cs                                # EDIT: validator registration

tests/
├── LamuFlix.UnitTests/
│   ├── Features/Enrichment/
│   │   └── SweepStrandedMoviesCommandHandlerTests.cs  # NEW
│   ├── Enrichment/
│   │   └── StrandedMovieSweeperTests.cs               # NEW
│   ├── Options/
│   │   └── EnrichmentOptionsValidatorTests.cs         # NEW
│   └── RabbitMq/
│       └── RabbitMqServiceCollectionExtensionsTests.cs # EDIT: NoMovieRepository member
├── LamuFlix.IntegrationTests/
│   ├── StrandedMovieSweeperTests.cs                   # NEW (EF matrix + real recovery)
│   ├── LeaseAwareMovieRepository.cs                   # EDIT: new member
│   ├── MetadataProviderCompositionTests.cs            # EDIT: UnusedMovieRepository member
│   └── ApiHostFactory.cs                              # EDIT: remove only sweeper (opt-in for recovery)
└── LamuFlix.Tests.Common/
    └── ManualTimeProvider.cs                          # NEW (timer-capable)

docs/adr/ADR-0004.md  # EDIT lines 92-96: short status note citing ADR-0017 + DEV-316
```

**Structure Decision**: Existing ports-and-adapters layout with feature-organised Core is reused (constitution:93-95; NOT vertical-slice); no new project or top-level folder. The sweeper lives in the existing `Infrastructure/Enrichment` subfolder; Core handler beside existing enrichment handlers.

## Complexity Tracking

No constitution violation to justify; nothing tracked.
