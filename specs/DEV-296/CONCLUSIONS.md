# DEV-296 Patron conclusions

## Dependency and placement

Keep `ICommandHandler` and `IQueryHandler` in `LamuFlix.Core/Pipeline`. Put the decorators and `AddHandler` registration in `LamuFlix.Infrastructure/Pipeline`. `LamuFlix.Infrastructure` already exists in the solution and references Core, so add only its `Pipeline/` folder; create no project or architectural layer. Add centrally pinned `FluentValidation` and `Microsoft.Extensions.DependencyInjection.Abstractions` references only where the implementation needs them; no OpenTelemetry package or mediator library. Core must stay within its dependency allowlist. Basis: DEV-296 ticket note lines 14-16, 21; constitution lines 79-98, 106-114, 173-174; ADR-0002 lines 11-17; recon-DEV-296 lines 8-24, 39-44, 67-73, 104-110. This is the §2.3 dependency and placement ruling; no constitution departure.

Pin `FluentValidation` to `12.1.1` and `Microsoft.Extensions.DependencyInjection.Abstractions` to `10.0.1` in `Directory.Packages.props`; reference both only from Infrastructure. FluentValidation 12.1.1 targets .NET 8 and is compatible with the repository's .NET 10 target. Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1 is compatible with .NET 10 and matches the existing Microsoft.Extensions.Hosting and Microsoft.Extensions.Http 10.0.1 central pins. These exact versions complete the approved dependency ruling without changing package identity or scope. Basis: DEV-296 ticket note lines 14-16; `Directory.Packages.props` lines 15-16; NuGet package pages https://www.nuget.org/packages/FluentValidation/12.1.1 and https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/10.0.1.

## Exception and HTTP contract

Use a sealed, Core-owned `ValidationException` with a property-to-messages failure map. The decorator translates FluentValidation failures into that exception. Implement the minimal single `IExceptionHandler` in `LamuFlix.Api`, with `AddProblemDetails()`, to return a 422 RFC 7807 response containing `traceId` for this exception. Preserve the constitution's other listed mappings in that handler if their exception types already exist; do not create unrelated exception types for this ticket. This mapping is in scope because the ticket expressly says `mapped to 422`; deferring it would drop named delivery. Basis: DEV-296 ticket note lines 15, 18-20; constitution lines 116-119, 171-188; recon-DEV-296 lines 49-55; PRODUCT.md lines 21-24, 32-46. The specified 422 needs no new API-shape approval.

## Handler contract and registration

Both interfaces use `Task<TResult> HandleAsync(TRequest, CancellationToken)`, with a compilable `class` generic constraint. An architecture test enforces sealed-record request types and sealed handlers, because C# has no `sealed record` generic constraint. `AddHandler<THandler,TReq,TRes>()` registers a scoped handler and an explicit Tracing -> Logging -> Validation -> handler chain for each supported command or query interface, with no assembly scanning. It rejects a handler implementing neither interface. Use a request delegate or equivalent to share decorator implementation without introducing a new dispatch abstraction. Basis: DEV-296 ticket note lines 14-17, 19-21; constitution lines 106-114; ADR-0002 lines 11-22; constitution line 98.

## Tracing, logging, and validation

Use `System.Diagnostics.ActivitySource` and a minimal Core `TelemetryConstants` for the source and handler span/attribute names. Span name and request attribute identify the request type. Set error status and `error.type` on exceptions, then rethrow. Use source-generated structured logging, an injected `TimeProvider` for elapsed time, and no request payload. Inject `IEnumerable<IValidator<TReq>>`; zero validators pass through, otherwise run all asynchronously with cancellation, aggregate failures, and throw before invoking the handler. `AddHandler` does not discover or register validators. Basis: DEV-296 ticket note lines 15-20; constitution lines 111-114, 173-174, 193-210, 216-225; recon-DEV-296 lines 23-24, 51, 56-57; ADR-0002 lines 13-17. ServiceDefaults source registration is separate from this ticket's handler pipeline.

## Tests, scope, and process

Use the existing `LamuFlix.Test` and `LamuFlix.ArchitectureTests` projects. Add no test package: use an `ActivityListener` and a capturing logger. Test order, parent trace propagation, validation short-circuiting, and full DI resolution for both command and query. Test the 422 mapper against an HTTP context using the existing test stack; do not add a new test dependency solely for this seam. Leave existing MoviesController/IMovieService and QueueWorker/IEnrichmentJobProcessor callers unchanged; add only the API wiring required for the stated 422 mapping, with no Worker wiring or use-case migration. Record `propertyTests: opt-out — pipeline plumbing with no domain invariants` in the task note. Keep size L and a three-round analyze cap; Phase 3 gates must report native exits, with InspectCode run without `-All`. Basis: DEV-296 ticket note lines 14-21, 24-33; recon-DEV-296 lines 29-36, 58-61; constitution lines 98, 108-119, 171-188; task-pipeline lines 7-25, 69-76, 86-94.

## Plan challenge follow-ups

FU-1 — File a follow-up ticket for ServiceDefaults to register the `"LamuFlix"` ActivitySource with tracing and supply `TimeProvider` through DI to hosts using the handler pipeline. Neither registration exists today, while ServiceDefaults owns both. Keep this outside DEV-296's frozen handler-pipeline scope. Basis: constitution lines 195-211; recon-DEV-296 lines 56-57, 112-113; `src/LamuFlix.ServiceDefaults/Extensions.cs` lines 6-13; ADR-0015 lines 47-48; brief.md lines 62, 174, 184.

FU-2 — Fold the expected-`ValidationException` error-span policy into the FU-1 telemetry follow-up; decide the policy before exported spans make each 422 appear as an error. Do not change DEV-296's ruled exception status. Basis: CONCLUSIONS.md line 19; `spec.md` lines 25, 75-76; findings-DEV-296-Sentry line 17; brief.md lines 183-184.

FU-3 — File a separate follow-up ticket for live endpoint migration: any host dispatching handlers must reference Infrastructure for `AddHandler`, and a migrated endpoint must use the single 422 ProblemDetails mapper in its live host, including `LamuFlix.Web` while it remains the live endpoint host. Do not migrate existing callers or rewrite the Api mapping in DEV-296. Basis: CONCLUSIONS.md lines 11, 23; ADR-0015 lines 33-35, 44-46; recon-DEV-296 lines 75-81; brief.md lines 183-184; constitution lines 173-177.


