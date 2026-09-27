# 0015. Handler pipeline: contracts in Core, decorators in Infrastructure

- Status: Proposed (plan frozen 2026-09-27 after plan challenge; becomes Accepted when the DEV-296 spec PR merges)
- Date: 2026-09-27
- Ticket: DEV-296

## Context

ADR-0002 chose explicit handlers and decorators over MediatR. It did not say where the handler
contracts, the decorators, or their registration live. DEV-296 asks for the pipeline: a command
and query handler contract, tracing, logging, and validation decorators applied in a fixed
order, FluentValidation in the validation step, and validation failures mapped to HTTP 422.

The constitution's Core allowlist (`.specify/memory/constitution.md:83`) permits only the BCL,
`Microsoft.Extensions.Logging.Abstractions`, and `System.Collections.Immutable`. FluentValidation
and `Microsoft.Extensions.DependencyInjection.Abstractions` are not on it, so the validation
decorator and the DI registration cannot live in Core without a constitution departure.
`LamuFlix.Infrastructure` already exists and already references non-Core packages. The grill
ruling is recorded at `specs/DEV-296/CONCLUSIONS.md:5`.

## Decision

- Core holds only the contracts, in the existing shared `Pipeline/` folder: `ICommandHandler`,
  `IQueryHandler` (both `where T : class`, `Task<TResult> HandleAsync(T, CancellationToken)`),
  a sealed `ValidationException` carrying `IReadOnlyDictionary<string, string[]>` errors keyed by
  property name, and `TelemetryConstants` (ActivitySource name `"LamuFlix"` and span attribute keys;
  the span name is the request type name at runtime). Core stays BCL-only.
- Infrastructure holds the behaviour, in a new `Pipeline/` folder: sealed generic
  `TracingDecorator`, `LoggingDecorator`, and `ValidationDecorator`, and an `AddHandler`
  extension that registers a handler and composes its chain. No new project or layer is added.
- The order is fixed, outer to inner: Tracing → Logging → Validation → handler. `AddHandler`
  builds it explicitly with constructors, so the order is visible in one place and needs no
  container feature such as open-generic decoration. `TracingDecorator` tolerates a null Activity
  (no listener) and still invokes the inner handler.
- Validation throws the Core `ValidationException` before the handler runs. The Api maps it to a
  422 ProblemDetails with `traceId` and `errors` in a single `IExceptionHandler`, as constitution
  Principle V requires. The body carries only the ProblemDetails fields, `traceId`, and `errors`;
  never the exception message, stack, or type. Other exception types pass through to later handlers.
- Architecture tests enforce the shape: requests are sealed records and handlers are sealed,
  with fixtures proving each rule can fail. The existing name-pattern sealed rules exclude
  interfaces so the new contracts do not trip them. Core references stay unchanged.
- Central package management gains exactly two packages, with exact pins: FluentValidation
  `12.1.1` and `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.1`, both referenced
  from Infrastructure only (`specs/DEV-296/CONCLUSIONS.md:7`).

## Consequences

- The Core allowlist holds; no constitution change is needed.
- Any host that dispatches handlers must reference Infrastructure to call `AddHandler`. Today
  only the Api does. The Worker and the existing callers (`MoviesController`/`IMovieService`,
  `QueueWorker`/`IEnrichmentJobProcessor`) are not migrated in DEV-296; live endpoint migration
  and reaching the 422 mapper from the live host are a separate follow-up (`CONCLUSIONS.md:31`).
- Spans go to the `"LamuFlix"` ActivitySource, but ServiceDefaults does not yet `AddSource` it
  and no OpenTelemetry package is added, so spans are not exported until a follow-up wires it,
  together with `TimeProvider` registration and the error-span policy for expected
  `ValidationException` (`CONCLUSIONS.md:27,29`). Until then the pipeline falls back to
  `TimeProvider.System`.
- Validators are resolved as `IEnumerable<IValidator<T>>` and registered by hand. Assembly
  scanning (`FluentValidation.DependencyInjectionExtensions`) is out of scope.
- Only `ValidationException` has an HTTP mapping. NotFound, InvalidTransition (ADR-0014's 409),
  and FeatureDisabled mappings are follow-ups that extend the same `IExceptionHandler`.
