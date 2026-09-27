# Feature Specification: ICommandHandler/IQueryHandler Pipeline

**Feature Branch**: `feature/296-spec`

**Created**: 2026-09-27

**Status**: Draft

**Input**: Grill Phase A outcome; Brief, Conclusions, and Assumptions in specs/DEV-296/

## User Scenarios & Testing

### User Story 1 - Handler Request Processing with Decorator Pipeline (Priority: P1)

As a backend handler developer, I need to be able to define request handlers that are automatically wrapped with cross-cutting concerns (tracing, logging, validation) so that requests are observed, logged, and validated consistently without boilerplate in each handler.

**Why this priority**: Core infrastructure that all future handlers will depend on. Defines the execution model for the entire pipeline.

**Independent Test**: Can be fully tested by defining a single handler, registering it with AddHandler, invoking it, and verifying the decorator chain executes in the correct order with all side effects (Activity started, logs recorded, validation executed).

**Acceptance Scenarios**:

1. **Given** a handler implementing `ICommandHandler<TCommand, TResult>` **When** registered via `AddHandler` and invoked **Then** the request flows through Tracing → Logging → Validation → Handler in that order, and the Activity is started with correct span name and attributes
2. **Given** a failing validator **When** a request is validated **Then** a `ValidationException` is thrown before the handler executes, the handler receives zero calls, and no request payload is visible in any log entry
3. **Given** an exception during handler execution **When** the request completes **Then** the Activity status is set to Error with error.type attribute, and the exception is rethrown; `LoggingDecorator` logs at Error level then rethrows

**Decorator Execution Order Test (AC7)**: Success path: record exact sequence into one shared list via ActivityListener, capturing logger, validator: `Tracing:start`, `Validation`, `Handler`, `Logging:complete`, `Tracing:stop` in that order; verify captured log entry records `Activity.Current` = handler span (Logging runs inside Tracing). Failure path: failing validator produces Warning log entry and handler receives zero calls.

---

### User Story 2 - Validation Exception HTTP Mapping (Priority: P1)

As an API consumer, I need validation failures to be returned as 422 Unprocessable Entity with property-to-error details in RFC 7807 ProblemDetails format so I can provide actionable feedback to clients.

**Why this priority**: Customers of the API need consistent error handling. Paired with User Story 1; validation is worthless without a defined HTTP contract.

**Independent Test**: Can be fully tested by constructing a DefaultHttpContext with a validation exception, passing it through the IExceptionHandler, and asserting the response status is 422 and body contains traceId and errors dictionary.

**Acceptance Scenarios**:

1. **Given** a `ValidationException` with property-to-messages failures **When** the handler executes in an HTTP context **Then** the response status is 422 and the body is RFC 7807 ProblemDetails with `traceId` and `errors` properties
2. **Given** any other exception during handler execution **When** the handler completes **Then** the IExceptionHandler returns `false` to defer to default framework handling (500 ProblemDetails)

---

### User Story 3 - Sealed Record Request Type Enforcement (Priority: P1)

As a handler author, I need the framework to enforce that all request types and handlers follow sealed class patterns so that the handler architecture is predictable and can be tested via architecture rules.

**Why this priority**: Prevents accidental misuse and ensures all handlers follow the established pattern. Caught via unit tests and architecture tests.

**Independent Test**: Can be fully tested by defining sealed and unsealed request types and handlers, running architecture tests, and asserting that unsealed types are rejected and sealed types are accepted.

**Acceptance Scenarios**:

1. **Given** an unsealed request class implementing a handler interface **When** architecture tests run **Then** the test fails with a clear message
2. **Given** a sealed non-record request class implementing a handler interface **When** architecture tests run **Then** the test fails with a clear message
3. **Given** an unsealed handler implementing ICommandHandler/IQueryHandler **When** architecture tests run **Then** the test fails with a clear message
4. **Given** a sealed request record and sealed handler **When** architecture tests run **Then** the test passes

---

## Requirements

### Functional Requirements

- **FR-001**: `ICommandHandler<TCommand, TResult>` interface with `Task<TResult> HandleAsync(TCommand, CancellationToken)` method signature must exist in `LamuFlix.Core.Pipeline`
- **FR-002**: `IQueryHandler<TQuery, TResult>` interface with identical method signature must exist in `LamuFlix.Core.Pipeline`
- **FR-003**: `ValidationException` in `LamuFlix.Core.Pipeline` must carry `IReadOnlyDictionary<string, string[]>` property-to-messages and must not reference FluentValidation
- **FR-004**: `TelemetryConstants` in `LamuFlix.Core.Pipeline` must define ActivitySource name `"LamuFlix"` and span/attribute keys for the handler telemetry
- **FR-005**: `TracingDecorator`, `LoggingDecorator`, and `ValidationDecorator` in `LamuFlix.Infrastructure.Pipeline` must each accept an inner `Func<TRequest, CancellationToken, Task<TResult>>` and implement both `ICommandHandler<TRequest, TResult>` and `IQueryHandler<TRequest, TResult>`
- **FR-006**: `AddHandler<THandler, TReq, TRes>()` extension in `LamuFlix.Infrastructure/Pipeline` must register the handler and compose the full decorator chain: Tracing → Logging → Validation → Handler
- **FR-007**: `AddHandler` must throw `InvalidOperationException` if the handler implements neither `ICommandHandler` nor `IQueryHandler`
- **FR-008**: `ValidationDecorator` must run all `IEnumerable<IValidator<TReq>>` validators and aggregate failures into `ValidationException` before invoking the handler
- **FR-009**: `TracingDecorator` must start an `Activity` with the request type name, parented to `Activity.Current`, and set error status and `error.type` attribute on exception
- **FR-010**: `LoggingDecorator` must use source-generated `LoggerMessage` with elapsed time from injected `TimeProvider`, logging at Information on success and Warning on `ValidationException`, Error on other exceptions (then rethrow). Must never log the request payload (ASSUMPTIONS [assumed]; brief Q7)
- **FR-011**: An `IExceptionHandler` in `LamuFlix.Api` must map `ValidationException` to 422 ProblemDetails with `traceId` and `errors` extension
- **FR-012**: Architecture tests must enforce sealed request records (not sealed classes, but sealed AND record) and sealed handlers for all `ICommandHandler`/`IQueryHandler` implementations; the rule fails on an unsealed request AND on a sealed non-record class
- **FR-013**: Architecture tests must verify `LamuFlix.Core` does not reference `FluentValidation`, `Microsoft.Extensions.DependencyInjection`, or `OpenTelemetry`
- **FR-014**: `Directory.Packages.props` must include `FluentValidation` and `Microsoft.Extensions.DependencyInjection.Abstractions` pinned to exact versions; no floating versions
- **FR-015**: `Program.cs` in `LamuFlix.Api` must wire up exception handling: call `AddProblemDetails()`, `AddExceptionHandler<ValidationExceptionHandler>()`, and `UseExceptionHandler()` in the correct order; no other wiring and no AddHandler calls (brief Frozen scope)

### Key Entities

- **ICommandHandler<TCommand, TResult>**: Command request handler interface. TCommand must be a class.
- **IQueryHandler<TQuery, TResult>**: Query request handler interface. TQuery must be a class.
- **ValidationException**: Core exception for validation failures, carries property-to-messages dictionary.
- **TracingDecorator, LoggingDecorator, ValidationDecorator**: Decorator classes in Infrastructure that compose into the handler pipeline.

## Success Criteria

- **SC-001**: The pipeline wraps handlers with Tracing → Logging → Validation → Handler in that exact order; execution order is verified by unit tests
- **SC-002**: Validation failures result in `ValidationException` thrown before the handler executes; confirmed by unit tests
- **SC-003**: HTTP API returns 422 ProblemDetails with `traceId` and `errors` on validation failure; confirmed by HTTP context test
- **SC-004**: Architecture tests enforce sealed request records and sealed handlers; tests fail on unsealed types and pass on sealed types
- **SC-005**: No mediator library (MediatR or similar) is referenced anywhere in the codebase
- **SC-006**: Core remains isolated: references only `Microsoft.Extensions.Logging.Abstractions` and `System.Collections.Immutable`; no FluentValidation or DI references in Core
- **SC-007**: All static-analysis gates pass with exit code 0: Roslyn analyzers, cyclomatic complexity (15 / 6 at refactor), InspectCode without `-All`, vulnerable packages scan, `dotnet format --verify-no-changes`, and `dotnet test`. Property tests exit 2 is accepted only with the Q11 opt-out line. Web gates are SKIPPED

## Out of Scope

The following are explicitly not included in this feature:

- No caller migration to new handlers (existing MoviesController/IMovieService and QueueWorker remain unchanged)
- No Worker or host AddHandler calls
- No ServiceDefaults AddSource configuration and no OpenTelemetry package additions
- No other exception types (Api maps ValidationException only; unhandled exceptions keep the default 500)
- No FluentValidation.DependencyInjectionExtensions or validator scanning
- No Mvc.Testing/WebApplicationFactory
- No Diagnostics.Testing/FakeLogger
- No new test project
- No edits under Web, Data, or Worker
- No deployment, CI/CD, or infrastructure changes

## Edge Cases

The following edge cases must be handled:

- Zero validators registered: ValidationDecorator passes through without errors
- Multiple validators whose failures are aggregated per property
- Cancellation passed to ValidateAsync and the inner handler
- A handler implementing neither ICommandHandler nor IQueryHandler (throws InvalidOperationException)
- A handler implementing both ICommandHandler and IQueryHandler interfaces (the chain is registered for each)

## Assumptions

- Existing `LamuFlix.Infrastructure` project will be reused; no new projects or architectural layers are added
- `TimeProvider` is injected into `LoggingDecorator` via the `AddHandler` factory, which resolves it as `sp.GetService<TimeProvider>() ?? TimeProvider.System` (ServiceDefaults does not register one); `AddHandler` registers no `TimeProvider`; tests pass `TimeProvider.System` or a fake (brief PC-5)
- The `ActivitySource` instance is static readonly in Infrastructure, named from `TelemetryConstants`
- `IEnumerable<IValidator<TReq>>` resolver injected into `ValidationDecorator`; zero validators means pass-through
- Existing logging and tracing infrastructure (ActivityListener, ILogger) in the test suite is sufficient for testing
- No validator scanning or registration by `AddHandler`; validators are manually registered
- Request delegates are used internally; no new dispatch abstraction is introduced
- Handlers are registered as Scoped; no Singleton or Transient variants
