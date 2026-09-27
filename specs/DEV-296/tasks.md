# Tasks: Handler Pipeline with Decorators

**Input**: Design documents from `specs/DEV-296/`; Grill Phase A brief.md with task-ordering constraints (lines 149-157)

**Prerequisites**: plan.md, spec.md, brief.md (frozen grill decisions)

**Tests**: Included per spec.md and brief AC7 (unit tests for decorators, DI, HTTP mapping)

**Organization**: Tasks grouped by implementation phase following brief's task-ordering constraints. Each phase represents a logical block of work with clear checkpoint validation.

---

## Phase 1: Setup — Central Package Management & Project References

**Purpose**: Establish package dependencies in CPM and update project file references before any code implementation

**Grill Constraint**: "CPM package entries and the Infrastructure PackageReferences, first; the Infrastructure decorator and `AddHandler` work depends on them. Core contracts do not." (brief:152)

- [ ] T001 Update `Directory.Packages.props` to add `FluentValidation` (`12.1.1` exact) (spec:FR-014; brief Q1 CONCLUSIONS:7)
- [ ] T002 Update `Directory.Packages.props` to add `Microsoft.Extensions.DependencyInjection.Abstractions` (`10.0.1` exact) (spec:FR-014; brief Q1 CONCLUSIONS:7)
- [ ] T003 Edit `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` to add PackageReferences to FluentValidation and DI.Abstractions (plan, Infrastructure PackageReferences section)
- [ ] T004 Edit `tests/LamuFlix.Test/LamuFlix.Test.csproj` to add ProjectReferences to `LamuFlix.Infrastructure` and `LamuFlix.Api` (plan; brief line 52)

**Checkpoint**: CPM and project files updated; builds succeed without new code yet

---

## Phase 2: Foundational — Core Pipeline Interfaces & Exceptions

**Purpose**: Define handler contracts and validation exception in Core; these are prerequisites for all decorators and handlers

**Grill Constraint**: "Core `Pipeline/`: interfaces, `ValidationException`, `TelemetryConstants`" (brief:152)

- [ ] T005 Create `src/LamuFlix.Core/Pipeline/ICommandHandler.cs` with interface `ICommandHandler<TCommand, TResult>` where TCommand : class, method `Task<TResult> HandleAsync(TCommand, CancellationToken)` (spec:FR-001)
- [ ] T006 Create `src/LamuFlix.Core/Pipeline/IQueryHandler.cs` with interface `IQueryHandler<TQuery, TResult>` where TQuery : class, method `Task<TResult> HandleAsync(TQuery, CancellationToken)` (spec:FR-002)
- [ ] T007 Create `src/LamuFlix.Core/Pipeline/ValidationException.cs` as sealed class carrying `IReadOnlyDictionary<string, string[]>` property-to-messages failures; no FluentValidation dependency (spec:FR-003; brief Q2 CONCLUSIONS)
- [ ] T008 Create `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`: constants are the ActivitySource name `"LamuFlix"` and attribute keys `lamuflix.handler.request`, `error.type`; no span-name constant; span name = request type name at runtime (spec:FR-004; brief Q6 CONCLUSIONS)
- [ ] T009 Edit `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` `AssertSealed()` (:108-116): add `.And().AreNotInterfaces()` to the Core selection so existing Command/Query/Handler name rules (:50-66) do not flag the new interfaces (spec:AC4; brief PC-1); phase 2 checkpoint: architecture suite green

**Checkpoint**: Core interfaces, exception types, constants, and sealed-interface rule compile and are accessible from Infrastructure and tests; architecture suite stays green

---

## Phase 3: Architecture & Tests Setup — Rules & Fixtures

**Purpose**: Define architecture test rules before decorators are implemented; fixtures can run in parallel with Phase 4 once Phase 2 is done (brief constraint line 153)

- [ ] T010 [P] Edit `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` to add sealed-record-request rule: reflection over concrete non-generic-definition types in Core, Infrastructure, Api implementing closed `ICommandHandler<,>`/`IQueryHandler<,>` must be sealed, and request type (`GenericTypeArguments[0]`) must be sealed AND record (detected via compiler-generated `<Clone>$` method); open generic decorators skipped; four fixtures prove non-vacuity (spec:AC6; brief PC-3)
- [ ] T011 [P] Edit `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`: sealed-handler rule is part of T010 mechanism (spec:AC6; brief PC-3)
- [ ] T012 [P] Edit `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` to add isolation rule: LamuFlix.Core must not reference `FluentValidation`, `Microsoft.Extensions.DependencyInjection`, or `OpenTelemetry`; type-level assertion only (spec:AC5; brief PC-11)
- [ ] T013 [P] Create fixture file(s) in `tests/LamuFlix.ArchitectureTests/` with four scenarios: unsealed request (fails; not sealed), sealed non-record request (fails; not record), sealed record request (passes), unsealed handler (fails); each fixture is a concrete type implementing closed `ICommandHandler<,>`/`IQueryHandler<,>` (spec:AC6; brief PC-3)

**Checkpoint**: Architecture tests compile and fail on unsealed/non-record requests and handlers; verify fixtures work

---

## Phase 4: Infrastructure — Decorators & DI Registration

**Purpose**: Implement decorator classes and AddHandler registration; decorators innermost first per brief constraint (line 154)

**Grill Constraint**: "Infrastructure decorators, innermost first: Validation, then Logging, then Tracing. Each is tested before the next." (brief:154)

### Phase 4a: ValidationDecorator

- [ ] T014 Create `src/LamuFlix.Infrastructure/Pipeline/ValidationDecorator.cs` as sealed class `<TReq, TRes>` accepting `Func<TReq, CancellationToken, Task<TRes>>` inner delegate and `IEnumerable<IValidator<TReq>>` validators (spec:FR-005, FR-008)
- [ ] T015 Implement `ValidationDecorator.HandleAsync()` to: run all validators with `ValidateAsync(…, ct)`, aggregate failures grouped by PropertyName into `IReadOnlyDictionary<string, string[]>` with messages in validator order, throw `ValidationException` if failures exist, otherwise invoke inner delegate (spec:FR-008; brief Q8)
- [ ] T016 Implement both `ICommandHandler<TReq, TRes>` and `IQueryHandler<TReq, TRes>` on ValidationDecorator, delegating to `HandleAsync()` (spec:FR-005)

**Checkpoint**: ValidationDecorator compiles; decorator can be instantiated and invoked

- [ ] T017 Create `tests/LamuFlix.Test/ValidationDecoratorTests.cs` with: test that validators are all run and failures aggregated; test that `ValidationException` is thrown before handler executes; test that handler receives zero calls on validation failure; test that zero validators pass through (spec:AC7; brief Q8)

**Checkpoint**: ValidationDecorator tests pass; validator ordering and short-circuit behavior verified

### Phase 4b: LoggingDecorator

- [ ] T018 Create `src/LamuFlix.Infrastructure/Pipeline/LoggingDecorator.cs` as sealed class `<TReq, TRes>` accepting inner delegate, `ILogger`, and `TimeProvider` (spec:FR-010)
- [ ] T019 Implement `LoggingDecorator.HandleAsync()` using source-generated `LoggerMessage` with elapsed time from `TimeProvider`; log at Information on success, Warning on `ValidationException`, Error on other exceptions (then rethrow) (spec:FR-010; brief Q7 ASSUMPTIONS)
- [ ] T020 Implement both `ICommandHandler<TReq, TRes>` and `IQueryHandler<TReq, TRes>` on LoggingDecorator (spec:FR-005)

**Checkpoint**: LoggingDecorator compiles

- [ ] T021 Create `tests/LamuFlix.Test/LoggingDecoratorTests.cs` with: test log levels (Information, Warning, Error); test elapsed time is recorded; test exception is rethrown; test that no request payload value appears in any captured log entry (spec:AC7; brief Q7)

**Checkpoint**: LoggingDecorator tests pass; logging behavior verified

### Phase 4c: TracingDecorator

- [ ] T022 Create `src/LamuFlix.Infrastructure/Pipeline/TracingDecorator.cs` as sealed class `<TReq, TRes>` accepting inner delegate and `ActivitySource` (spec:FR-009)
- [ ] T023 Implement `TracingDecorator.HandleAsync()` to: start Activity with request type name, parented to `Activity.Current`, set attributes; handle null Activity (no listener) and still invoke inner delegate (spec:FR-009; brief PC-7)
- [ ] T024 Implement error handling: on exception (if Activity is not null), set `ActivityStatusCode.Error`, set `error.type` attribute, rethrow (spec:FR-009; brief PC-7)
- [ ] T025 Implement both `ICommandHandler<TReq, TRes>` and `IQueryHandler<TReq, TRes>` on TracingDecorator (spec:FR-005)

**Checkpoint**: TracingDecorator compiles

- [ ] T026 Create `tests/LamuFlix.Test/TracingDecoratorTests.cs` with: test Activity is started with correct name and parent; test attributes are set; test error status and `error.type` on exception; test null Activity (no listener) passes result through; test exception is rethrown (spec:AC7; brief PC-7)

**Checkpoint**: TracingDecorator tests pass; tracing behavior verified

### Phase 4d: AddHandler Registration & DI Tests

- [ ] T027 Create `src/LamuFlix.Infrastructure/Pipeline/ServiceCollectionExtensions.cs` (or `HandlerRegistration.cs` per brief Q5) with `AddHandler<THandler, TReq, TRes>()` extension method (spec:FR-006)
- [ ] T028 Implement `AddHandler` to: register THandler as Scoped, type-check for `ICommandHandler` and `IQueryHandler` implementation, throw `InvalidOperationException` if neither interface found, register Scoped factories composing Tracing → Logging → Validation → Handler; factory resolves `TimeProvider` as `sp.GetService<TimeProvider>() ?? TimeProvider.System` for injection into LoggingDecorator (spec:FR-006, FR-007; brief PC-5)

**Checkpoint**: AddHandler compiles and can be called

- [ ] T029 Create `tests/LamuFlix.Test/AddHandlerTests.cs` (spec:AC7; brief PC-2) with tests for:
  - **Success path** decorator execution order: record exact sequence into one shared list (via ActivityListener, capturing logger, validator) and assert `Tracing:start`, `Validation`, `Handler`, `Logging:complete`, `Tracing:stop` in that order; verify captured log entry records `Activity.Current` = handler span (Logging runs inside Tracing)
  - **Failure path**: failing validator produces Warning log entry (Logging runs outside Validation) and handler receives zero calls
  - AddHandler resolves full chain for both `ICommandHandler<,>` and `IQueryHandler<,>` (separate registrations)
  - AddHandler rejects a handler implementing neither interface (throws InvalidOperationException)
  - Manual IValidator<> registration; a manually registered validator is applied by the resolved chain, and an unregistered validator class in the test assembly is not

**Checkpoint**: AddHandler tests pass; DI composition verified

---

## Phase 5: API — Exception Mapping & HTTP Integration

**Purpose**: Map ValidationException to 422 ProblemDetails; wire up exception handler in Program.cs

**Grill Constraint**: "Api `IExceptionHandler`, `Program.cs` wiring (needs Phase 2 only; can run in parallel with Phase 4)" (brief:156)

- [ ] T030 Create `src/LamuFlix.Api/ExceptionHandling/ValidationExceptionHandler.cs` as sealed `IExceptionHandler` (spec:FR-011; brief Q3)
- [ ] T031 Implement handler to: catch `ValidationException`, write 422 ProblemDetails via `IProblemDetailsService` with standard fields (type, title, status) plus `traceId` and `errors` extensions (no ex.Message, stack, or exception type); return `false` for all other exceptions (spec:FR-011; brief PC-8)
- [ ] T032 Edit `src/LamuFlix.Api/Program.cs` to call `AddProblemDetails()`, `AddExceptionHandler<ValidationExceptionHandler>()`, and `UseExceptionHandler()` in the correct order (spec:FR-015; brief Q3)
- [ ] T033 Create `tests/LamuFlix.Test/ValidationExceptionHandlerTests.cs` with test: construct `DefaultHttpContext` with `AddProblemDetails()` and ValidationException; invoke handler; assert 422 status, standard ProblemDetails fields (type, title, status), `traceId` and `errors` extensions; assert no message leak (spec:AC7; brief PC-8)

**Checkpoint**: Exception mapping tests pass; HTTP 422 response verified

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and gates

- [ ] T034 Run static-analysis gates on all new/modified `.cs` files (spec:SC-007; brief Q12 AC8):
  - [ ] T034a `./scripts/run-roslyn-analyzers.ps1` (no `-All`; only changed files)
  - [ ] T034b `./scripts/run-cyclomatic-complexity.ps1` with threshold 15 (and separately `-Threshold 6` for refactor gate)
  - [ ] T034c `./scripts/run-jetbrains-inspectcode.ps1` (never run with `-All`)
- [ ] T035 Run `dotnet format --verify-no-changes` on all new/modified files
- [ ] T036 Run `./scripts/run-vulnerable-packages.ps1` and `./scripts/run-property-tests.ps1` (property tests exit 2 accepted only with Q11 opt-out line); run `./scripts/run-web-gates.ps1` (exit 2 = SKIPPED when no /web changes)
- [ ] T037 Run `dotnet test` (all tests, spec:SC-007)
- [ ] T038 Execute the decorator execution-order test (T029) to confirm success-path exact sequence: `Tracing:start`, `Validation`, `Handler`, `Logging:complete`, `Tracing:stop` via shared list recording; verify captured log entry records `Activity.Current` = handler span; confirm failure path: failing validator produces Warning log and zero handler calls (spec:AC1; brief PC-2)
- [ ] T039 Verify all architecture tests pass: sealed request records, sealed handlers, Core isolation (spec:AC6)

**Checkpoint**: All gates pass with exit 0; all tests pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Core contracts and sealed amendments (T005–T009) do not depend on Phase 1; architecture rules and Phase 4 decorators do — **BLOCKS Phase 3, 4, 5**
- **Phase 3 (Architecture & Tests)**: Can start after Phase 2; can run in parallel with Phase 4
- **Phase 4 (Infrastructure)**: Depends on Phase 2; innermost-first constraint: Validation (T014–T017) → Logging (T018–T021) → Tracing (T022–T026) → AddHandler (T027–T029)
- **Phase 5 (API)**: Depends on Phase 2 only; can run in parallel with Phase 4
- **Phase 6 (Polish)**: Depends on all phases — final validation only; gates at T034–T039

### Within Phase 4: Decorator Sequence

- Validation: T014–T017 (definition + tests)
- Logging: T018–T021 (definition + tests; after Validation definition)
- Tracing: T022–T026 (definition + tests; after Logging definition)
- AddHandler: T027–T029 (uses all three decorators; after all three tested)

### Parallel Opportunities

- T010–T013 (architecture rules & fixtures) can run in parallel with T014–T017 (Validation definition & tests) once Phase 2 completes
- Phase 5 API wiring (T030–T033) can run in parallel with Phase 4 (Infrastructure decorators and DI)

---

## Implementation Strategy

### MVP Scope (Minimum Viable Infrastructure)

Complete phases in order:
1. **Phase 1**: Setup (CPM, project refs) — 1 checkpoint
2. **Phase 2**: Core interfaces and exceptions — 1 checkpoint
3. **Phase 3**: Architecture test rules (defines what "correct" looks like)
4. **Phase 4**: Full decorator chain (Validation → Logging → Tracing → Handler)
5. **Phase 5**: API exception mapping
6. **Phase 6**: Gates and validation

Result: A working, tested handler pipeline that can accept first handler implementations.

### Incremental Delivery

Since this is infrastructure, the entire pipeline is required to function. However, within Phase 4, each decorator can be validated independently before moving to the next:
- After Validation + Logging: handlers can request-validate and log
- After Tracing: handlers have observability
- After AddHandler: handlers can be registered and invoked via DI

---

## Notes

- **Test Files**: All test files are new; the existing test suite (`LamuFlix.Test`, `LamuFlix.ArchitectureTests`) provides the test infrastructure (xUnit, NSubstitute, Shouldly, ActivityListener)
- **No Mediator**: MediatR or similar is not used anywhere; decorators compose via request delegates per ADR-0002
- **Phase 3 Gates**: Roslyn, Cyclomatic ≤ 15 (≤ 6 refactor), InspectCode (no `-All`), format, dotnet test, vulnerable packages
- **Property Tests**: Opt-out per Q11 (brief:115) — mark `propertyTests: opt-out` in task note
- **Web Gates**: SKIPPED (no `/web` changes)
- **Handlers Not Included**: MoviesController/IMovieService and QueueWorker/IEnrichmentJobProcessor remain unchanged (Q10; brief:61)
- **Existing Test Infrastructure**: Leverage existing LamuFlix.Test and LamuFlix.ArchitectureTests projects; no new test project needed (Q9; brief line 70)
