# DEV-296 — Phase A Brief

Phase A grill outcome for DEV-296 (parent DEV-282, size L): ICommandHandler/IQueryHandler pipeline with Tracing, Logging, and Validation decorators.
Rulings: Patron, in `specs/DEV-296/CONCLUSIONS.md` (structural, cited) and `specs/DEV-296/ASSUMPTIONS.md` (taste, `[assumed]`).
Facts: `recon-DEV-296` (lines 2-113) and the DEV-296 ticket note (lines 12-21).
Grill: **12 questions asked (cap 12); 12/12 ruled**, plus 1 Q1 follow-up that Patron settled on recon:67-73 (CONCLUSIONS.md:5). Owner checkboxes: **0**.
`LamuFlix.Infrastructure` already exists (recon:67-73). No project or architectural layer is added; only a `Pipeline/` folder is added inside it.

## Closing bar

The ticket's own ACs:

- **AC1 (ticket:19):** the pipeline wraps handlers with tracing, logging, and validation, outer to inner: Tracing → Logging → Validation → handler (ADR-0002; constitution II).
- **AC2 (ticket:20):** a request with validation failures throws the Core `ValidationException` before the handler executes. The handler receives zero calls.
- **AC3 (ticket:21):** no mediator library (MediatR or similar) is referenced anywhere. `Directory.Packages.props` gains only `FluentValidation` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

Closing-bar lines from the rulings (these do not change delivery):

- **AC4 (Q3, ticket:15):** a `ValidationException` thrown during an Api request returns `422` as RFC 7807 `ProblemDetails`. The body includes `traceId` and the property-to-messages errors. The mapping lives in exactly one `IExceptionHandler` in `LamuFlix.Api`.
- **AC5 (Q1):** `LamuFlix.Core.csproj` still references only `Microsoft.Extensions.Logging.Abstractions` and `System.Collections.Immutable` (recon:16-21; constitution:83). No FluentValidation, DI, or OpenTelemetry reference enters Core.
- **AC6 (Q4):** architecture tests fail on an unsealed or non-record request type, and on an unsealed handler, for any closed `ICommandHandler<,>` / `IQueryHandler<,>`. Non-vacuity is proven by fixtures, following the existing `SealedCommandFixture` / `UnsealedCommandFixture` pattern (ArchitectureTests.cs:21,51-65,108).
- **AC7 (Q9, ticket:17):** unit tests cover:
  - decorator execution order;
  - an Activity is started and parented to the ambient `Activity.Current`;
  - validation short-circuits before the handler;
  - `AddHandler` resolves the full chain, for a command and for a query;
  - `AddHandler` rejects a handler that implements neither interface.
- **AC8 (Q12):** the Phase 3 gates exit 0 natively. InspectCode runs without `-All`. `propertyTests: opt-out — pipeline plumbing with no domain invariants` (Q11).

## Frozen scope

Files in scope (all new unless marked):

- `Directory.Packages.props` (edit): add `FluentValidation` and `Microsoft.Extensions.DependencyInjection.Abstractions`, pinned exact.
  - `FluentValidation` `12.1.1` and `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.1`, exact (Patron, CONCLUSIONS.md:7; supersedes the earlier "latest stable" / "10.0.x" wording — plan challenge PC-4).
- `src/LamuFlix.Core/Pipeline/` (folder exists, holds `.gitkeep`, recon:19):
  - `ICommandHandler.cs`: `ICommandHandler<TCommand, TResult> where TCommand : class`, with `Task<TResult> HandleAsync(TCommand, CancellationToken)`.
  - `IQueryHandler.cs`: `IQueryHandler<TQuery, TResult> where TQuery : class`, same method shape.
  - `ValidationException.cs`: sealed, carries `IReadOnlyDictionary<string, string[]>` property-to-messages failures (Q2). It must not depend on FluentValidation.
  - `TelemetryConstants.cs`: ActivitySource name `"LamuFlix"`, plus only the handler span and attribute keys this ticket uses (Q6).
    - It lives in `Pipeline/` rather than a new Core folder (constitution:92 names `Pipeline/` as a shared home).
    - Keys: request-type attribute `lamuflix.handler.request`, `error.type`. The span name is the request type name.
- `src/LamuFlix.Infrastructure/Pipeline/` (new folder in an existing project, recon:73):
  - `TracingDecorator.cs`, `LoggingDecorator.cs`, `ValidationDecorator.cs`: generic `<TReq, TRes>`, sealed, each implementing both interfaces over one inner request delegate (Q5; CONCLUSIONS "request delegate or equivalent"). They add no new dispatch interface.
  - `ServiceCollectionExtensions.cs` (or `HandlerRegistration.cs`): `AddHandler<THandler, TReq, TRes>()`.
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` (edit): add PackageReferences to `FluentValidation` and `Microsoft.Extensions.DependencyInjection.Abstractions`. There are no PackageReferences today (recon:70).
- `src/LamuFlix.Api/` (Q3):
  - `ExceptionHandling/` or the `Endpoints/` sibling: one sealed `IExceptionHandler` mapping `ValidationException` → 422 ProblemDetails. Quill names the folder in the plan and must not add a new top-level folder.
  - `Program.cs` (edit, 7 lines today, recon:77-81): `AddProblemDetails()`, `AddExceptionHandler<…>()`, `UseExceptionHandler()`. No other wiring, and no `AddHandler` calls (there are no handlers yet).
- `tests/LamuFlix.Test/` (existing project, Q9):
  - `LamuFlix.Test.csproj` (edit): add ProjectReferences to `LamuFlix.Infrastructure` and `LamuFlix.Api`. The project references neither today (recon:84-86). These are project references, not packages.
  - New test files for the decorators, `AddHandler`, and the exception handler.
- `tests/LamuFlix.ArchitectureTests/` (existing project; it already references Core, Infrastructure, and Api, recon:87-89):
  - `ArchitectureTests.cs` (edit): add interface-based sealed-record-request and sealed-handler rules (AC6), and a rule that Core does not reference `FluentValidation` or `Microsoft.Extensions.DependencyInjection` (AC5).
  - Fixture file(s) beside the existing fixtures.
- `specs/DEV-296/*` artifacts; ADR draft `docs/adr/0015-handler-pipeline-placement.md` (Phase 2 step 5, Keel).

Out of scope:

- Migrating existing callers: `MoviesController`/`IMovieService` and `QueueWorker`/`IEnrichmentJobProcessor` stay unchanged (Q10; recon:32).
- Worker wiring, and `AddHandler` calls in any host.
- ServiceDefaults `AddSource("LamuFlix")` and OpenTelemetry packages (Q6; recon:56-57,112-113).
- `NotFoundException`, `InvalidTransitionException`, and `FeatureDisabledException`, which do not exist (recon:49-50,98-99) and must not be created (CONCLUSIONS "Exception and HTTP contract").
  - The Api handler maps `ValidationException` only.
  - Unhandled exceptions keep the framework default of 500 ProblemDetails via `AddProblemDetails`.
- `FluentValidation.DependencyInjectionExtensions`; validator scanning or registration (Q8).
- `Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory` (not in CPM, recon:90-91). This would be a new package; the 422 test uses `DefaultHttpContext` (CONCLUSIONS:21).
- `Microsoft.Extensions.Diagnostics.Testing` (FakeLogger). A hand-written capturing `ILogger` is used instead (Q9).
- A new test project. The constitution's `LamuFlix.UnitTests` does not exist and is not created (recon:58-61).
- Any file under `LamuFlix.Web`, `LamuFlix.Data`, or `LamuFlix.Worker`.

## Round cap

- Analyze rounds with Quill: 3 maximum (Q12).
- Review: 2 rounds maximum; remediation: at most 2 fix commits per round (standing §2.2).

## Grill answers

- **Q1 — Placement and dependencies?** Interfaces go in `Core/Pipeline`. Decorators and `AddHandler` go in `Infrastructure/Pipeline`. `FluentValidation` and `M.E.DependencyInjection.Abstractions` are pinned in CPM and referenced only by Infrastructure.
  - Core's allowlist stays intact, so there is no constitution departure.
  - Basis: the ticket places only the interfaces in Core (ticket:14); constitution:83,92,173-174; recon:8-24,39-44.
  - Follow-up (CONCLUSIONS.md:5, replacing the earlier conditional): Infrastructure exists and references Core (recon:67-73). Add `Infrastructure/Pipeline`, and add no project or architectural layer. Basis: constitution:83-92,111-114; ADR-0002:13-17; recon:104-110.
- **Q2 — Which ValidationException?** A sealed Core-owned exception carrying property-to-messages failures. `ValidationDecorator` translates FluentValidation failures into it. Basis: constitution:116-119,180-182; recon:49.
- **Q3 — 422 mapping?** In scope (overruling Keel's recommendation). Ticket:15 says "mapped to 422", so deferring it would drop named delivery.
  - Build a minimal single Api `IExceptionHandler`, with `AddProblemDetails()` and RFC 7807 including `traceId`.
  - Create no unrelated exception types.
  - Basis: constitution:171-188; recon:52-55,75-81.
- **Q4 — Sealed record constraint?** Use a `class` generic constraint, plus architecture tests enforcing sealed-record requests and sealed handlers. Basis: ticket:14; constitution:98,108-109.
- **Q5 — Shape and registration?**
  - `HandleAsync(TRequest, CancellationToken)` on both interfaces.
  - `AddHandler` registers a Scoped handler and an explicit Tracing → Logging → Validation → handler chain for whichever of the two interfaces THandler implements, with no assembly scanning.
  - It rejects a handler implementing neither interface by throwing `InvalidOperationException`.
  - Decorators share their implementation through a request delegate, with no new dispatch abstraction.
  - Basis: ticket:14-16; constitution:111-114; ADR-0002:13-17.
- **Q6 — Tracing?**
  - BCL `System.Diagnostics.ActivitySource`, with a minimal Core `TelemetryConstants`.
  - The span name and request attribute identify the request type.
  - On an exception: set `ActivityStatusCode.Error` and `error.type`, then rethrow.
  - ServiceDefaults source wiring is out of scope.
  - Basis: ticket:15,17; constitution:193-210; recon:51,57.
- **Q7 — Logging?**
  - Source-generated `LoggerMessage`, with elapsed time from an injected `TimeProvider` and no request payload.
  - `[assumed]` levels (ASSUMPTIONS.md): Information on success (request type, elapsed ms); Warning on `ValidationException`; Error on any other exception, then rethrow.
- **Q8 — Validation semantics?**
  - Inject `IEnumerable<IValidator<TReq>>`. Zero validators means pass-through.
  - Otherwise run all validators with `ValidateAsync(…, ct)`, aggregate the failures, and throw before invoking the handler.
  - No validator scanning or registration by `AddHandler`.
  - Basis: ticket:15,20; constitution:173-174.
- **Q9 — Tests?**
  - Use the existing `LamuFlix.Test` and `LamuFlix.ArchitectureTests`, with no new test package.
  - Tools: `ActivityListener`, a capturing `ILogger`, and NSubstitute/Shouldly, which are already referenced (recon:59).
  - The 422 mapper is tested against an HTTP context (CONCLUSIONS:21): a `DefaultHttpContext` whose `RequestServices` has `AddProblemDetails()`. Assert the status and the body's `traceId` and `errors`.
- **Q10 — Out of scope?** Existing callers and the Worker are untouched. Api wiring is limited to Q3.
- **Q11 — Property tests?** `propertyTests: opt-out — pipeline plumbing with no domain invariants`. Rigger/Conductor records this line in the DEV-296 task note.
- **Q12 — Process?** Size L (ticket:9); a 3-round analyze cap; Phase 3 native exit-0 gates; InspectCode without `-All`.

## Plan decisions

### Approach

- Interfaces carry only the contract.
- Each decorator is a small sealed class holding an inner `Func<TReq, CancellationToken, Task<TRes>>`, and it implements both `ICommandHandler<TReq,TRes>` and `IQueryHandler<TReq,TRes>`.
- `AddHandler<THandler, TReq, TRes>()`:
  1. `services.AddScoped<THandler>()`;
  2. checks `typeof(ICommandHandler<TReq,TRes>).IsAssignableFrom(typeof(THandler))`, and likewise for the query interface. This is a single type check, not scanning. It throws `InvalidOperationException` if neither matches;
  3. for each implemented interface, registers a Scoped factory composing `new TracingDecorator(new LoggingDecorator(new ValidationDecorator(handler.HandleAsync, validators), logger, timeProvider), activitySource)`, so the call chain reads in one method (ADR-0002 rationale).
- `TimeProvider` is injected into `LoggingDecorator`. The `AddHandler` factory resolves it with `sp.GetService<TimeProvider>() ?? TimeProvider.System`: ServiceDefaults does not register one today (Extensions.cs:6-13; recon:56,113), and this is the fallback the existing code uses. `AddHandler` registers no `TimeProvider`. Tests pass `TimeProvider.System` or a fake (plan challenge PC-5).
- The `ActivitySource` instance is a static readonly in Infrastructure, named from `TelemetryConstants`.
- Api `IExceptionHandler`:
  - on `ValidationException`, it writes 422 through `IProblemDetailsService` with an `errors` extension, and `traceId` is added by the default ProblemDetails writer;
  - it returns `false` for every other exception, so the framework default applies.

### Test strategy

- xUnit v3, Shouldly, and NSubstitute where needed; `[Theory]` over repeated `[Fact]`s where the cases differ only by data.
- Order test (PC-2; logging stays completion-only per ASSUMPTIONS.md:3, no entry log is added): prove the onion Tracing(Logging(Validation(Handler))) by observation, not by an entry-order list:
  - success path, one shared list: `Tracing:start` (ActivityListener.ActivityStarted), `Validation` (validator), `Handler`, `Logging:complete` (capturing logger), `Tracing:stop` (ActivityStopped), in exactly that order; and the captured log entry records `Activity.Current` = the handler span (Logging runs inside Tracing);
  - failure path: a failing validator produces a Warning log entry for `ValidationException` (Logging runs outside Validation) and the handler receives zero calls.
- Tracing test: start a parent Activity, invoke, and assert the child's `ParentId`, name, and tag. On an exception, assert the error status and `error.type`, and that the exception is rethrown.
- Validation test: a failing validator throws the Core `ValidationException` with the aggregated errors, and the handler substitute receives no call. Zero validators pass through.
- DI test: `ServiceCollection` + `AddHandler`, then resolve `ICommandHandler<,>` (and separately `IQueryHandler<,>`) and assert the outermost type is `TracingDecorator`. A handler implementing neither interface throws.
- Api handler test: `DefaultHttpContext` as above; assert a 422 status and a ProblemDetails body with `traceId` and `errors`. A non-validation exception returns `false`.

### Gate expectations

- Roslyn, complexity ≤ 15 (and ≤ 6 at refactor), InspectCode without `-All`, vulnerable packages, `dotnet format --verify-no-changes`, and `dotnet test`: all exit 0.
- Property tests exit 2, accepted only with the Q11 opt-out line in the task note. Web gates exit 2 (SKIPPED; no `/web` change).
- The recon baseline gates were skipped (recon:36-37). No baseline pass is claimed; Phase B recon supplies the baseline.

### Task-ordering constraints

1. CPM package entries and the Infrastructure PackageReferences, first; the Infrastructure decorator and `AddHandler` work depends on them (Core contracts do not; PC-6).
2. Core `Pipeline/`: interfaces, `ValidationException`, `TelemetryConstants`, together with the amendment of the existing name-pattern sealed rules to exclude interfaces (PC-1), so the architecture suite stays green at the step-2 checkpoint.
3. Architecture-test rules and fixtures (AC5, AC6). They can run in parallel with step 4 once step 2 is done.
4. Infrastructure decorators, innermost first: Validation, then Logging, then Tracing. Each is tested before the next.
5. `AddHandler` and its DI tests (these need all three decorators).
6. Api `IExceptionHandler`, `Program.cs` wiring, and the `LamuFlix.Test` ProjectReferences with the HTTP-context test (needs step 2 only; it can run in parallel with steps 4-5).
7. Gates.

### Implementation checks (for Anvil; not decisions)

- Recon lists `Microsoft.Extensions.Logging.Abstractions` as absent from CPM (recon:108), yet Core.csproj:10 references it (recon:19). Phase B recon confirms how it is versioned before the LoggerMessage generator is relied on transitively in Infrastructure.
  - If Infrastructure needs its own reference, that is an existing package added to a consuming project, not a new dependency.
- `LamuFlix.Test` gaining a ProjectReference to the Web-SDK `LamuFlix.Api` flows the `Microsoft.AspNetCore.App` FrameworkReference transitively. That is not a new package.

## Plan challenge rulings (Keel, 2026-09-27)

Inputs: findings-DEV-296-Ledger (Standards), findings-DEV-296-Compass (Spec), findings-DEV-296-Sentry (Risk), all read at pinned `feature/296-spec` @ 52f91a0. Each item verified at file:line. No ruling changes a Patron ruling or the ticket; owner checkboxes stay **0**.

- **PC-1 (Compass F1 H, Sentry R3 M) — accepted.** `ArchitectureTests.cs:50-66` selects Core types by `HaveNameMatching("Command"|"Query"|"Handler")` and asserts `BeSealed` (:108-116). New `ICommandHandler`/`IQueryHandler` match all three patterns and an interface is never sealed, so the suite goes red. Fix: in `AssertSealed` add `.And().AreNotInterfaces()` to the Core selection; the edit is inside the in-scope `ArchitectureTests.cs` (brief:55) and removes no rule. Land it in the same phase as the Core interfaces (ordering step 2).
- **PC-2 (Compass F2 H) — accepted; test mechanism redefined, behaviour unchanged.** A completion-only log cannot appear second in an entry-order list. Logging behaviour is Patron's `[assumed]` ruling (ASSUMPTIONS.md:3) and is kept; the order test is redefined as in Test strategy above. brief AC7 ("decorator execution order") is satisfied unchanged.
- **PC-3 (Compass F3 M, Sentry R2 M) — accepted.** Mechanism for the AC6 rules, reflection in `ArchitectureTests.cs`, no new package: for every concrete, non-generic-definition type in the scanned assemblies that implements a closed `ICommandHandler<,>`/`IQueryHandler<,>`, the handler type must be sealed and `GenericTypeArguments[0]` must be sealed and a record, detected by the compiler-generated `<Clone>$` method. Open generic types (the decorators) are skipped. Scanned: Core, Infrastructure, Api, plus the fixtures for non-vacuity. The four fixtures (unsealed request, sealed non-record request, sealed record request, unsealed handler) prove the heuristic; if `<Clone>$` misclassifies, the sealed-record fixture fails.
- **PC-4 (Compass F4 M) — accepted, clerical.** T001/T002 and this brief's scope lines take CONCLUSIONS.md:7 versions verbatim.
- **PC-5 (Compass F5 M, Sentry R6 L) — accepted.** spec.md:128 and the old brief line were false; replaced as in Approach above. The ServiceDefaults `TimeProvider` gap is a follow-up candidate for Patron, not this ticket.
- **PC-6 (Ledger L2 L) — accepted with corrected citation.** The overstatement is brief:151 / tasks.md:17 (plan.md:151 is unrelated). Narrowed above.
- **PC-7 (Sentry R1 M) — accepted.** `ActivitySource.StartActivity` returns null with no listener, and no host registers one (recon:56,113). `TracingDecorator` must tolerate a null Activity (null-conditional tag/status work) and still invoke the inner delegate; add a no-listener test that the result is returned.
- **PC-8 (Sentry R4 L) — accepted.** The 422 body carries only the ProblemDetails fields, `traceId`, and the `errors` map; never `ex.Message`, stack, or exception type (constitution:188). Validator messages become client-facing text.
- **PC-9 (Ledger M1 M) — accepted, wording.** plan.md:45 file-scope line must list the Api `ExceptionHandling/ValidationExceptionHandler.cs` and the new test and fixture files (brief:48-56).
- **PC-10 (Ledger L1 L) — accepted.** plan.md:70 ADR path is `../../docs/adr/0015-handler-pipeline-placement.md`.
- **PC-11 (Compass F6 L) — accepted.** T012 asserts type-level no-dependency on `FluentValidation`, `Microsoft.Extensions.DependencyInjection`, and `OpenTelemetry` (AC5); the csproj allowlist is checked at review from the diff (plan.md:124, no Core csproj change). FR-013 adds OpenTelemetry.
- **PC-12 (Compass F7 L) — accepted.** `TelemetryConstants` holds the source name and attribute keys only; the span name is computed from the request type name (brief:41-43).
- **PC-13 (Compass F9 L) — accepted, clerical.** checklist core-pipeline.md:43 cites T029; :40 cites FR-009 for error status.
- **Not actioned:** Compass F8 (422 handler in Api, live host is Web) is the frozen scope (CONCLUSIONS.md:11; brief:71) — noted for the endpoint-migration follow-up. Sentry R5 (every 422 becomes an errored span) follows from ruled Q6 — follow-up candidate. Sentry R7 verified, no action. Ledger/Sentry ADR-0015 checks: conforms.
- **Follow-up candidates, routed to Patron (non-blocking):** FU-1 ServiceDefaults `AddSource("LamuFlix")` + `TimeProvider` registration (R5, R6, PC-5); FU-2 error-span policy for expected `ValidationException` once tracing is exported (R5); FU-3 any host dispatching handlers must reference Infrastructure, and the 422 mapper must reach the live host when endpoints migrate (ADR-0015:44-46; F8).
- **Housekeeping:** an untracked `recon-DEV-296` file (Wisp report copy, 6663 bytes) sits at the worktree root. It is not a spec artifact and must not be committed.


## Architect (Stryker) ruling (Keel, 2026-09-28)

Input: Conductor receipt (DEV-296 task note :178-185); `StrykerOutput/2026-09-28.03-18-47/reports/mutation-report.json`; `stryker-config.json` (break 80, `since` origin/main, no `project`/`test-projects`).

- **Outcome: NOT PASSED — inconclusive tooling run, not a verdict on the code.** 1660 created, 0 tested, 1620 Ignored, 40 CompileError, score null. Native exit 0 is not a pass: no mutant was tested, so the break-80 threshold was never evaluated. A skipped gate is never a passed gate.
- **No retry until recon.** A re-run of the same command would reproduce the same result. The next run happens once, after recon names the cause, with the fix recorded here first.
- **Needs recon (Conductor → recon-DEV-296-stryker):**
  1. For each `.cs` file in `git diff --name-only <merge-base>...HEAD` under `src/`: mutant count by `status` and `statusReason` from the report. Are the Pipeline decorators, `ServiceCollectionExtensions.cs`, and `ValidationExceptionHandler.cs` Ignored, and with what reason?
  2. The 40 CompileError mutants: file:line, mutator, and the compiler error Stryker logged. Why are unchanged Web/Worker files among them?
  3. From the Stryker log: which project(s) were mutated, which test project(s) ran, and whether the initial test run discovered and passed tests.
  4. Whether report paths (`src\LamuFlix\...`) match the diff paths used by the `since` filter.
- **Scope of the gate:** this brief's Gate expectations (Roslyn, complexity, InspectCode, vulnerable packages, format, test) are unaffected and are judged on their own receipts. Architect stays open; the ticket does not reach review with a null score.
- **Worktree hygiene:** `DEV-296-phase3-stryker-report.txt` (untracked) and `StrykerOutput/` are run artifacts; they must not be committed. The report text belongs in the task note. `.junie/mcp/mcp.json` shows a working-copy modification the ticket does not name (§2.3 item 6); it must not be committed. Restoring it is for the Conductor to confirm with its owner.

## D7a-equivalent Stryker amendment (Keel, 2026-09-28)

Basis: owner direction relayed by Fixer (2026-09-28): apply the DEV-294 D7a approach, and do not wait for supplemental compiler-error recon. Precedent: DEV-294 brief D7a and its PASS adjudication (DEV-294 note :1205-1214). Cause: the `since` run at 8855b978 (DEV-296 note :178-183) reproduced the DEV-294 linked-worktree `since` failure, where every changed-file mutant was Ignored with "Removed by since filter". This amendment supersedes the "no retry until recon" line above and the invocation authorised in DEV-296 note :152-155. Recon items 2-4 above are no longer preconditions. The evidence bar stays as it is, tightened below.

- **Invocations: three, as one run.** dotnet-stryker 4.16.0 takes one `--project` (`-p`) per invocation, and `mutate` globs resolve relative to that project (the DEV-294 D7a precedent: `Domain/*.cs` under `-p LamuFlix.Core.csproj`). The 11 files span three projects, so the run is exactly three sequential invocations: Core, Infrastructure, Api. Each one runs from the worktree root with its own temp config. Together they are the single run. All three are executed even if an earlier one fails, so the evidence is complete. No invocation is retried.
- **Pre-run check (read-only). If any of these is false, stop and report to Keel:** the branch is `feature/DEV-296`; `git diff 8855b97805522ac9ea1acf61f5e5f8c8a22f7919 HEAD -- src tests stryker-config.json harness.yml` is empty (HEAD may be past 8855b978 by spec-only commits); `git diff eae97ae3 HEAD -- stryker-config.json harness.yml` is empty. Known pre-existing working-tree items are allowed and must stay untouched: `specs/DEV-296/CONCLUSIONS.md` (M), `recon-DEV-296.md`, `DEV-296-phase3-stryker-report.txt`, `StrykerOutput/`.
- **Frozen invocation, pwsh 7, run once:**
  ```powershell
  Set-Location F:\Dev\LamuFlix.worktrees\DEV-296
  if ((git branch --show-current) -ne 'feature/DEV-296') { throw 'wrong branch' }
  $base = '{"mutation-level":"Standard","since":{"enabled":false},"thresholds":{"high":90,"low":80,"break":80},"reporters":["progress","html","json"],'
  $runs = [ordered]@{
    'LamuFlix.Core.csproj'           = '"mutate":["Pipeline/ICommandHandler.cs","Pipeline/IQueryHandler.cs","Pipeline/TelemetryConstants.cs","Pipeline/ValidationException.cs"]'
    'LamuFlix.Infrastructure.csproj' = '"mutate":["Pipeline/HandlerLog.cs","Pipeline/LoggingDecorator.cs","Pipeline/ServiceCollectionExtensions.cs","Pipeline/TracingDecorator.cs","Pipeline/ValidationDecorator.cs"]'
    'LamuFlix.Api.csproj'            = '"mutate":["ExceptionHandling/ValidationExceptionHandler.cs","Program.cs"]'
  }
  foreach ($p in $runs.Keys) {
    $name = $p -replace '^LamuFlix\.|\.csproj$',''
    $cfg = Join-Path $env:TEMP "DEV-296-stryker-$name.json"
    Set-Content -Path $cfg -Value ('{"stryker-config":' + $base + $runs[$p] + '}}') -Encoding utf8NoBOM
    Get-FileHash $cfg -Algorithm SHA256
    dotnet stryker -f $cfg -p $p
    "EXIT $p = $LASTEXITCODE"
  }
  ```
  - Each temp JSON is the tracked `stryker-config.json` at eae97ae3, with `mutation-level`, `thresholds` (90/80/80) and `reporters` copied verbatim, `since` replaced by `{ "enabled": false }`, and `mutate` added. It lives outside the repo and is disposable.
  - The 11 globs are exactly `git diff --name-only eae97ae3...HEAD -- 'src/*.cs'`: Core 4, Infrastructure 5, Api 2. They are written relative to each project. `LamuFlix.Infrastructure.csproj` is in the diff but is not a `.cs` file, so it is not mutated.
- **Receipt, for each invocation:** the temp config path and SHA256, the exact command, the native exit, the StrykerOutput report path, the created/tested/killed/survived/timeout/NoCoverage/Ignored/CompileError counts, the score, and the per-file list of mutated files with each file's status counts and `statusReason` for Ignored. The receipt also states the branch and HEAD, before and after the run.
- **PASS bar (all required):**
  1. All three native exits are 0.
  2. At least 1 tested mutant across the run.
  3. The mutated files are a subset of the 11. A "mutated file" is a report entry with at least 1 mutant; zero-mutant entries (every project file is listed) are not mutated paths (DEV-294:1208).
  4. Every invocation that has at least 1 tested mutant scores at least 80, the break threshold. An invocation whose frozen files all have 0 mutants, with a null score, passes that item. This can only happen for the interface-only and constant-only files. Such files are not a failure.
- **FAIL, with no retry, report to Keel:** any non-zero exit, 0 tested overall, a mutated path outside the 11, an invocation with at least 1 mutant and a null score, a score under 80, any mutant whose `statusReason` is "Removed by since filter", or any of the 11 files that has mutants but none of them tested (all Ignored or CompileError). A cannot-run invocation is also a FAIL. "Removed by block already covered filter" is Stryker's standard dedup and is not a failure (DEV-294:1209). If the score is under 80, the survivors go back to Anvil for tests on the round counter. The threshold is never lowered.
- **Forbidden:** editing tracked `stryker-config.json` or `harness.yml`, any flag beyond `-f` and `-p`, a change to the globs, any action on or from the main checkout, a ref move, fetch or rebase, a code change, committing `StrykerOutput/`, and touching `recon-DEV-296.md`, `.junie/mcp/mcp.json`, `DEV-296-phase3-stryker-report.txt` or `specs/DEV-296/CONCLUSIONS.md` (pending Patron and Rigger, CONCLUSIONS.md:33-39).
- There is no §2.3 (a)/(b) item, so no owner checkbox. This is a gate invocation, not a ticket or constitution change. The direction came from the owner.
- **Outcome (Keel, DEV-296 note ruling after :201): FAIL** on HEAD 08b416e. See D7b.

## D7b — adjudication of the D7a-equivalent run and next route (Keel, 2026-09-28)

Input: Anvil receipt, DEV-296 note :195-201. Verified read-only by Keel:
- The three temp configs match the frozen JSON byte for byte (SHA256 A9003548…, 1964D503…, 8A5FD541…).
- `tmp-dev-296-d7a.ps1` is the frozen block word for word.
- The three reports are `StrykerOutput/2026-09-28.04-20-28`, `04-22-08` and `04-26-36`.
- The malformed-JSON attempt Anvil disclosed produced no StrykerOutput.

- **Verdict: Architect FAIL**, under the frozen bar (brief:230-235). The Api invocation exited 2 with a score of 7.14%, below break 80. No retry of this run.
- **No partial credit.**
  - The mutated files are a subset of the 11 on all three reports:
    - Core: ValidationException only.
    - Infrastructure: 4 decorator/registration files. HandlerLog has 0 mutants.
    - Api: 2 files.
  - No "Removed by since filter" mutant appears, so the D7a mechanism worked.
  - Infrastructure's "100%" is **not accepted**: all 40 tested mutants are Timeout and none are Killed. That is not evidence of detection.
  - Core's 2/2 kills (killedBy populated) are consistent, but the next run must pass all three invocations.
- **The Api result is not a credible verdict on test quality.** Five survivors are already asserted by existing tests:
  - `ValidationExceptionHandler.cs:24` (`false`→`true`) vs `ValidationExceptionHandlerTests.cs:63` `ShouldBeFalse`;
  - `:40` and `:41` (Title/Type → `""`) vs tests :41-42 `ShouldNotBeNullOrWhiteSpace`;
  - `:43` and `:44` (`"traceId"`/`"errors"` → `""`) vs tests :43-44 `GetProperty`, which throws on a missing key.

  Together with the 40/40 Infrastructure timeouts, this means the mutant-to-test link is broken for Api and Infrastructure. Writing new tests would not fix that.
- **Survivor routing:**
  1. **`ValidationExceptionHandler.cs:18, :19, :20`** (statement removal of `ThrowIfNull(httpContext)`, `ThrowIfNull(exception)` and `ThrowIfCancellationRequested`) are real test gaps: no test passes null or a cancelled token. **Anvil adds tests. This is architect-remediation round 1 of 2, with at most 2 commits, test files only, and no `src/` change.**
  2. **`:24, :37 (Timeout), :40, :41, :43, :44`** need no new tests. They must be Killed on the next run. If they are not, that is a tooling FAIL returned to Keel, not an Anvil round.
  3. **`Program.cs:7-12`** (5 statement removals in the composition root) cannot be killed in frozen scope. Only booting the host could detect them. The brief bars that at :67 (no `Mvc.Testing`/`WebApplicationFactory`), and CONCLUSIONS.md:23 says "add no test package". The mutation break of 80 is constitutionally scoped to Core (constitution.md:290), so dropping Program.cs is not a departure. **Program.cs is removed from the Api `mutate` set** for the next run. Host-level wiring coverage goes to DEV-376 (FU-3, live endpoint and 422 host). The next Api `mutate` is `["ExceptionHandling/ValidationExceptionHandler.cs"]` only.
  4. The CompileErrors (Api 4, Infrastructure 3, Core 12 in the summary) are excluded from the score by Stryker. They are not a failure under the bar and need no disposition.
- **The next run is not authorized yet.** It needs, in order:
  - (a) Recon answering the linkage questions in the DEV-296 note ruling.
  - (b) Keel freezing the tooling fix here as D7c, as settings inside the temp JSONs only. The tracked config and thresholds stay unchanged.
  - (c) Anvil's round-1 test commit.

  After that, it is one run of the D7a shape on the new HEAD. Its pre-run check becomes: `git diff 8855b978 HEAD -- src stryker-config.json harness.yml` is empty, and `tests` differ only by Anvil's round-1 commit(s). The PASS/FAIL bar at :230-235 stands, tightened by one item: an invocation whose tested mutants are all Timeout, with 0 Killed, is a FAIL.
- **Hygiene:** `tmp-dev-296-d7a.ps1` (untracked, Anvil's run script) must not be committed. Its disposal joins the Patron and Rigger cleanup route with `DEV-296-phase3-stryker-report.txt`.
- No §2.3 (a)/(b) item, so no owner checkbox. D7b narrows a gate glob inside the constitution's Core-only mutation scope, and changes no ticket text.
