# Implementation Plan: Core Use-Case Handlers

**Branch**: `feature/299-spec` | **Date**: 2026-09-30 | **Spec**: `specs/DEV-299/spec.md`

**Input**: `specs/DEV-299/spec.md`, `brief.md` (Frozen scope, Handler contract table, Plan decisions), `CONCLUSIONS.md` Q1-Q12

## Summary

Add thin, sealed command/query handlers to `LamuFlix.Core/Features/<Feature>/`, each an orchestration of ports and domain methods. Build the shared Pipeline and Domain types first, then handlers feature by feature. Handlers gated by D1 (requeue), D2 (import) and Q13 (library) are planned but their tasks are `[BLOCKED]` until the owner or Patron answers. No handler calls another handler or references another feature.

## Technical Context

**Language/Version**: C# 14 / .NET 10

**Primary Dependencies**: none new in `src/`. Core already references `Ardalis.SmartEnum` and `Microsoft.Extensions.Logging.Abstractions`. Tests add `AutoFixture` 4.18.1 and `Faker.Net` 2.0.163 (already pinned in `Directory.Packages.props:31-32`, referenced without a version).

**Storage**: N/A (ports only; no schema change)

**Testing**: xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net; Stryker on `LamuFlix.Core`, break 80

**Target Platform**: library (`LamuFlix.Core`) plus one Api exception-handler arm

**Project Type**: web-service backend, vertical-slice feature folders

**Constraints**: zero warnings (`TreatWarningsAsErrors`); cyclomatic complexity <= 15, then <= 6 at the refactor gate; `Core` gains no package; at most three ports per handler; no comments except AAA headers

**Scale/Scope**: 11 handlers (Browse, GetDetails, Import, Claim, Apply, RecordFailure, RequestEnrichment, Requeue, Add, Remove, Play), 5 shared types, 1 Api arm, 1 ADR

## Constitution Check

*Gate: passes for the unblocked scope. The four open points are D1, D2, D3 (owner) and Q13 (Patron).*

| Rule | Status |
|---|---|
| II: sealed handlers over sealed records, `Features/<Feature>/` | Pass (FR-001) |
| IV: ports only; no EF/RabbitMQ in Core | Pass (FR-002) |
| V: `NotFoundException` mapped to a status in the single `IExceptionHandler` | Pass (FR-017) |
| VI/VII: injected `TimeProvider`, `CancellationToken` flow, structured logs without exception text | Pass (FR-019) |
| IX: AutoFixture + Faker.Net referenced | Pass (FR-020) |
| Enrichment Reliability 445-459: `EnrichmentRequested` fixed at `MovieId` + `Attempt`; worker claim is the only claim | Pass unless D1 option (C); then needs an amendment |
| Feature dependency allow-list (arch rule V:1170-1195) | **Open (Q13)** for `LamuFlix.Core.Library`; SmartEnum probe in T021 |
| Ticket text unchanged | Pass unless D1(A)/D2(C) drop a handler (owner decision) |
| AddHandler registration (checklist 351-352), FluentValidation validation (V) and options validated at startup (VII; checklist 365-366) | **Departure, owner checkbox D3**: deferred to wiring by Patron Q9; FR-018 names the prerequisites, including the `Attempt >= 1` and `MaxAttempts >= 1` rules |
| Closed sets are Enumerations (checklist) | Pass: `EnrichmentFailureAction` is a SmartEnum (Q2) |

## Research and Decisions

- **"Retryable"**: `EnrichmentFailureCategory.IsRetryable` (existing sealed record). `RateLimited` -> `RetryDelayed`; `ProviderUnavailable`/`Unknown` -> `Retry`; `InvalidResponse` -> dead letter at any attempt.
- **Retry math**: `Attempt < MaxAttempts` retries with `NextAttempt = Attempt + 1`; otherwise dead-letter. No I/O on the retry path.
- **Status check in RequestEnrichment**: compare `movie.Status` with `EnrichmentStatus.NotFound`/`Failed` using `==`. This is the SmartEnum probe input for Q13(ii). `==` resolves to the operator inherited from `SmartEnum<,>`, which is an `Ardalis.SmartEnum` reference. Fallback if the probe is red: `ReferenceEquals(movie.Status, EnrichmentStatus.X)`, which is correct because SmartEnum members are singletons and references only `System.Object` and Domain (Compass S5).
- **SmartEnum probe (T021)**: after T014, T016, T018 and T020 compile (Claim, Apply, RecordFailure, RequestEnrichment), run `LamuFlix.ArchitectureTests`. Green means no allow-list edit for SmartEnum. Red naming `Ardalis.SmartEnum`: first switch every handler comparison of SmartEnum members to `ReferenceEquals` and re-run. Only if it is still red does the finding go to Keel, who routes it to Patron under Q13; do not edit the test.
- **File convention**: one type per file; `XCommand.cs` and `XCommandHandler.cs` are siblings in the feature folder.
- **Where `Unit` lives**: `Core/Pipeline` (Q5); member-less sealed record with a private constructor and `Unit.Value` (plan challenge, Ledger F3).
- **RecordFailure log capture**: a hand-written `RecordingLogger<T> : ILogger<T>` in `tests/LamuFlix.UnitTests/Features/RecordingLogger.cs`; NSubstitute cannot match `Log<TState>` for the internal or generated state types (Ledger F1).
- **`EnrichmentOptions` shape**: non-positional sealed record, `[Range(1, int.MaxValue)] public int MaxAttempts { get; init; }`, because a positional parameter's attribute never reaches the property (Compass S4). T005a tests the annotation.
- **D1 contract edit**: T030a applies the port or message edit that D1(B) or D1(C) authorizes, mirroring T030 for D2 (Compass S3).
- **Import failure semantics (D2-gated)**: one folder, one movie, one save, no compensation; a throwing call stops everything after it (Sentry L1). `EnrichmentFailureAction`/`Decision` live in `Core/Domain` beside `EnrichmentFailureCategory` (Keel plan decision).
- **Api test location**: the existing `ValidationExceptionHandler` tests are in `tests/LamuFlix.Test/ValidationExceptionHandlerTests.cs` (legacy project). The brief says extend the existing tests, so the 404 test goes there; every new handler test goes to `LamuFlix.UnitTests`.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-299/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── checklists/requirements.md
```

### Source Code

```text
src/LamuFlix.Core/
├── Pipeline/   NotFoundException.cs, Unit.cs, EnrichmentOptions.cs                (new)
├── Domain/     EnrichmentFailureAction.cs, EnrichmentFailureDecision.cs           (new)
└── Features/
    ├── Watchlist/    AddToWatchlist*, RemoveFromWatchlist*                        (new)
    ├── Playback/     PlayMovie*                                                   (new)
    ├── Enrichment/   ClaimEnrichment*, ApplyEnrichmentResult*,
    │                 RecordEnrichmentFailure*, RequestEnrichment*                 (new)
    │                 RequeueStrandedMovies*                                       (new, BLOCKED D1)
    ├── Library/      BrowseMovies*, GetMovieDetails*                              (new, BLOCKED Q13)
    └── Import/       ImportMovieFolder*                                           (new, BLOCKED D2)

src/LamuFlix.Api/ExceptionHandling/ValidationExceptionHandler.cs                   (edit: 404 arm)
tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj                                 (edit: 2 refs)
tests/LamuFlix.UnitTests/Features/{Watchlist,Playback,Enrichment,Library,Import}/  (new)
tests/LamuFlix.UnitTests/Pipeline/EnrichmentOptionsTests.cs                         (new, T005a)
tests/LamuFlix.Test/ValidationExceptionHandlerTests.cs                             (edit: 404 test)
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs                              (edit only if Q13 rules so)
CONTEXT.md                                                                         (edit only if D1 keeps requeue)
docs/adr/0017-enrichment-decisions-in-core-handlers.md                             (new, Keel drafts)
```

**Structure Decision**: extend the existing feature-folder layout in `LamuFlix.Core`. No new project, folder, layer or package.

## Test Strategy

One test class per handler. NSubstitute doubles of the ports. Movies in a given status are built through real domain transitions. AutoFixture and Faker.Net supply anonymous ids and titles. `Theory` + `MemberData` for RecordFailure (4 categories x below/at `MaxAttempts`) and RequestEnrichment (4 statuses). Assert `Received`/`DidNotReceive`, save-before-enqueue ordering (RequestEnrichment, Import), and no Save or Enqueue on every exception path. Time comes from a small test `TimeProvider` subclass with a fixed `GetUtcNow`.

## Gates

`dotnet build` zero warnings; `dotnet test` green; `LamuFlix.ArchitectureTests` green; `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (15, then `-Threshold 6`), `./scripts/run-jetbrains-inspectcode.ps1` exit 0; `dotnet format --verify-no-changes`; Stryker break 80; `git diff --stat origin/main...HEAD` shows only frozen-scope files.

## Complexity Tracking

One departure, owner checkbox D3: handlers ship without `AddHandler` registration, FluentValidation validators and `EnrichmentOptions` `ValidateOnStart()`, deferred to wiring by Patron Q9. D1 option (C) would additionally need a constitution amendment; both are the owner's call.
