# Implementation Plan: Handler Pipeline with Decorators

**Branch**: `feature/296-spec` | **Date**: 2026-09-27 | **Spec**: specs/DEV-296/spec.md

**Input**: Feature specification from `specs/DEV-296/spec.md`; Grill Phase A brief, conclusions, assumptions; recon-DEV-296

## Summary

Implement a decorator-based handler pipeline for LamuFlix.Core and LamuFlix.Infrastructure that wraps `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` implementations with Tracing, Logging, and Validation decorators, executing in that order. Provide DI registration via `AddHandler<THandler, TReq, TRes>()` and HTTP 422 mapping for validation failures in LamuFlix.Api. No mediator library is used; decorators compose via request delegates. All handlers must be sealed, and Core remains isolated from FluentValidation and DI references.

## Technical Context

**Language/Version**: C# 14 / .NET 10

**Primary Dependencies**: 
- FluentValidation 12.1.1 (exact version pinned in CPM per Patron ruling)
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1 (exact version per Patron ruling, matches existing Microsoft.Extensions pins)

**Storage**: N/A — pipeline is in-memory, no database operations

**Testing**: xUnit v3, NSubstitute, Shouldly, System.Diagnostics.ActivityListener for tracing tests

**Target Platform**: .NET 10 on Windows/Linux (ASP.NET Core backend)

**Project Type**: ASP.NET Core Web API backend library (infrastructure)

**Performance Goals**: N/A

**Constraints**: 
- Core project must reference only `Microsoft.Extensions.Logging.Abstractions` and `System.Collections.Immutable` — no FluentValidation or DI references
- No new projects or architectural layers
- No mediator library (MediatR) anywhere
- Request types must be sealed record, and handlers must be sealed

**Scale/Scope**: Internal infrastructure; no user-facing scale concerns.

## Constitution Check

**Gate 1: §2.3 Care List**
- ✓ **Dependencies**: FluentValidation and M.E.DependencyInjection.Abstractions added to CPM; cited in brief and rulings
- ✓ **Architecture**: No new projects; Pipeline folder added to existing LamuFlix.Infrastructure (Q1 CONCLUSIONS.md)
- ✓ **Database Schema**: N/A — no schema changes
- ✓ **API Shape**: ValidationException → 422 mapping defined in brief AC4; cited in CONCLUSIONS.md Q3
- ✓ **Security & Local Execution**: No LocalPlay, secrets, or Process.Start concerns
- ✓ **File Scope**: Only new files in Pipeline/; existing files edited only as specified (CPM, Api ExceptionHandling/ValidationExceptionHandler.cs and Program.cs, ArchitectureTests.cs with new sealed-record-request and sealed-handler rules, Infrastructure.csproj, LamuFlix.Test.csproj); new test files in LamuFlix.Test (TracingDecoratorTests, ValidationDecoratorTests, LoggingDecoratorTests, AddHandlerTests, ValidationExceptionHandlerTests) and ArchitectureTests fixture file(s)

**Gate 2: Core Isolation**
- ✓ Core.Pipeline interfaces and ValidationException in Core; no FluentValidation or DI dependencies (AC5, CONCLUSIONS.md Q1)
- ✓ Decorators and AddHandler in Infrastructure, which already references Core

**Gate 3: No Mediator Library**
- ✓ AC3 explicitly rules out MediatR or similar; decorator pattern used instead (ADR-0002)

**Status**: ✅ **PASS** — All constitution checks satisfied; no departures from rules or new justifications needed.

## Project Structure

### Documentation (This Feature)

```text
specs/DEV-296/
├── brief.md                    # Phase A grill outcome (completed)
├── CONCLUSIONS.md              # Grill rulings (completed)
├── ASSUMPTIONS.md              # Grill assumptions (completed)
├── spec.md                      # Feature specification (this artifact)
├── plan.md                      # Implementation plan (this artifact)
├── tasks.md                     # Phase 2 — task breakdown
├── DRAFTING_RECEIPT.md          # Spec kit drafting record
├── checklists/core-pipeline.md  # Requirement quality checks
└── ../../docs/adr/0015-handler-pipeline-placement.md # Proposed ADR (Keel)
```

### Source Code Structure

**Core Layer** (`src/LamuFlix.Core/Pipeline/` — existing folder):
```text
src/LamuFlix.Core/Pipeline/
├── ICommandHandler.cs           # Command handler interface
├── IQueryHandler.cs             # Query handler interface
├── ValidationException.cs        # Core validation exception
└── TelemetryConstants.cs         # ActivitySource name and span/attribute keys
```

**Infrastructure Layer** (`src/LamuFlix.Infrastructure/Pipeline/` — NEW folder inside existing project):
```text
src/LamuFlix.Infrastructure/Pipeline/
├── TracingDecorator.cs          # Tracing decorator implementation
├── LoggingDecorator.cs          # Logging decorator implementation
├── ValidationDecorator.cs        # Validation decorator implementation
└── ServiceCollectionExtensions.cs  # AddHandler registration (or HandlerRegistration.cs)
```

**API Layer** (existing `src/LamuFlix.Api/`):
```text
src/LamuFlix.Api/
├── Program.cs                   # (edit) Add AddProblemDetails, AddExceptionHandler, UseExceptionHandler
└── ExceptionHandling/           # (new folder per Q3 CONCLUSIONS.md)
    └── ValidationExceptionHandler.cs  # IExceptionHandler for ValidationException → 422
```

**Tests**:
```text
tests/LamuFlix.Test/
├── (existing)                   # New test files for decorators, DI, exception handler
├── [TracingDecoratorTests.cs]    # Activity lifecycle, error status, error.type
├── [ValidationDecoratorTests.cs] # Validator aggregation, short-circuit, zero validators
├── [LoggingDecoratorTests.cs]    # Log levels, elapsed time, TimeProvider
├── [AddHandlerTests.cs]          # DI registration, chain composition, decorator order verification, interface validation
└── [ValidationExceptionHandlerTests.cs]  # HTTP 422 response, traceId, errors body

tests/LamuFlix.ArchitectureTests/
├── ArchitectureTests.cs         # (edit) Add sealed-record request and sealed-handler rules
└── fixture file(s)              # New fixture patterns beside the existing fixtures
```

**Package Management** (`Directory.Packages.props` — existing):
```text
<!-- Add these lines (exact versions per brief Q1 CONCLUSIONS.md) -->
<PackageVersion Include="FluentValidation" Version="12.1.1" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.1" />
```

**Project Files** (edits):
- `src/LamuFlix.Core/LamuFlix.Core.csproj` — no changes (interfaces are pure C#)
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` — (edit) add FluentValidation and DI.Abstractions PackageReferences
- `src/LamuFlix.Api/LamuFlix.Api.csproj` — no changes (exception handler is pure C#)
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` — (edit) add ProjectReferences to LamuFlix.Infrastructure and LamuFlix.Api

## Design Decisions

### Decorator Shape
Each decorator (`TracingDecorator`, `LoggingDecorator`, `ValidationDecorator`) is a sealed class that:
- Holds an inner `Func<TRequest, CancellationToken, Task<TResult>>` delegate
- Implements both `ICommandHandler<TRequest, TResult>` and `IQueryHandler<TRequest, TResult>` interfaces
- No new dispatch abstraction or interface hierarchy

**Rationale**: Allows a single decorator to serve both command and query handlers without duplication. Request delegate pattern is lightweight and avoids circular dependencies.

### Registration via AddHandler
`AddHandler<THandler, TReq, TRes>()` performs these steps:
1. Register `THandler` as Scoped (the actual handler implementation)
2. Type-check: `typeof(ICommandHandler<TReq,TRes>).IsAssignableFrom(typeof(THandler))`? and `IQueryHandler<TReq,TRes>`?
3. For each implemented interface, register a Scoped factory that composes the full chain
4. Throw `InvalidOperationException` if neither interface is implemented

**Rationale**: Simple, type-safe, no assembly scanning, single point of composition.

### Validation Flow
`ValidationDecorator` injects `IEnumerable<IValidator<TReq>>` and:
- Runs all validators with `ValidateAsync(…, cancellationToken)`
- Aggregates failures into a single `ValidationException`
- Throws before invoking the handler if failures exist
- Passes through if zero validators are registered

**Rationale**: Fail-fast, consistent error shape, handler never executes on validation failure (AC2).

### Tracing
`TracingDecorator` uses BCL `System.Diagnostics.ActivitySource`:
- Static readonly instance in Infrastructure, named from `TelemetryConstants`
- Span name = request type name
- Attributes: `lamuflix.handler.request` = request type name
- Tolerates a null Activity (no listener) and still invokes the inner delegate
- On exception: `ActivityStatusCode.Error`, `error.type` = exception type name, rethrow

**Rationale**: BCL only, no OpenTelemetry dependency in Phase A. ServiceDefaults owns source wiring (out of scope).

### Logging
`LoggingDecorator` uses source-generated `LoggerMessage`:
- Levels: Information (success), Warning (`ValidationException`), Error (any other exception, then rethrow)
- Elapsed time from injected `TimeProvider`
- No request payload logging

**Rationale**: Structured logging, efficient source generation, no sensitive data in logs.

### HTTP Mapping
Single sealed `IExceptionHandler` in `LamuFlix.Api.ExceptionHandling/`:
- Catches `ValidationException` → 422 ProblemDetails with standard RFC 7807 fields (type/title/status), plus `traceId` and `errors` extensions; never ex.Message, stack, or exception type
- Returns `false` for all other exceptions → framework default (500 ProblemDetails)
- Uses `IProblemDetailsService` to write response

**Rationale**: Centralized, RFC 7807 compliant, no unrelated exceptions created.

### Architecture Tests
Mechanism: reflection over concrete non-generic-definition types in Core, Infrastructure, Api implementing closed `ICommandHandler<,>`/`IQueryHandler<,>`. For each:
- Handler type must be sealed
- Request type (`GenericTypeArguments[0]`) must be sealed AND record, detected by compiler-generated `<Clone>$` method
- Open generic decorators skipped
- Four fixtures (unsealed request, sealed non-record, sealed record, unsealed handler) prove non-vacuity
- Existing `AssertSealed()` rule updated with `.And().AreNotInterfaces()` filter to exclude new pipeline interfaces from the sealed check (PC-1); sealed rules remain for all concrete handler implementations

Core isolation rule: `LamuFlix.Core` has no FluentValidation, Microsoft.Extensions.DependencyInjection, or OpenTelemetry references (type-level assertion).

**Rationale**: Prevents accidental misuse and guarantees handler pattern compliance.

## Complexity Tracking

No complexity violations. The architecture decision (decorators vs. mediator) was pre-decided in the grill phase (Q5 CONCLUSIONS.md, ADR-0002). No justifications needed.

## Notes

- Recon-DEV-296 confirms Core.Pipeline directory exists with `.gitkeep`
- All grill rulings (Q1–Q12) are documented in CONCLUSIONS.md and authoritative for this plan
- Phase 3 gates (Roslyn, Cyclomatic ≤ 15/6, InspectCode without `-All`, vulnerable packages, dotnet format, dotnet test) must all exit 0
- Property tests opt-out per Q11 (task note: `propertyTests: opt-out — pipeline plumbing with no domain invariants`)
- Web gates are SKIPPED (no `/web` changes)
