# Implementation Plan: Compose the LamuFlix.Api host (handler registration, adapter wiring, CORS, endpoint-group convention)

**Branch**: `feature/308-spec` | **Date**: 2026-10-02 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`, `brief.md` (the SETTLED brief, which owns the plan decisions as Q1 to Q8), `CONCLUSIONS.md` (eight Patron verdicts, append-only), `ASSUMPTIONS.md` (one `[assumed]` naming entry), notes `recon-DEV-308`, `recon-DEV-308-handlers`, `recon-DEV-308-handlers-2`, `recon-DEV-308-handlers-3`, `.specify/memory/constitution.md`, `specs/PRODUCT.md`, `harness.yml`.

**Note on the Spec Kit phases.** This plan is the `/speckit-plan` output. Phase 0 (`research.md`) and Phase 1 (`data-model.md`, `contracts/`, `quickstart.md`) produce **no files** here, for the reasons DEV-303, DEV-304, DEV-306 and DEV-307 each gave. This ticket adds **no** domain entity: it registers handlers that already exist, binds two adapters whose implementations already exist, adds one CORS policy and one route-group convention. There is no field, no migration, no state transition and no persistence change. There is no new port, so no ADR and nothing for `data-model.md` to describe. The only external contract this ticket touches is the CORS response shape, and it is **not written to a `contracts/` file** because `CONCLUSIONS.md` Q3 fixes it as exactly one origin, any method, any header, no credentials and one exposed header — a shape fully stated in `spec.md` FR-019 to FR-024 and in Design §4 below, with no field, no type and no DTO left to decide. The decisions Phase 0 would have resolved are all already ruled in `CONCLUSIONS.md`, `ASSUMPTIONS.md` or the brief; **none is left open**, and the two conditional entries in the file set are resolved at pickup by observation rather than by a question (Design §7).

**Verification basis for every citation below.** Each `file:line` in this plan was opened and read at the HEAD of `feature/308-spec` during this drafting pass, not taken from a recon note. The recon claims that were load-bearing and were re-checked here are: `AddMovieCatalog` exists at `Persistence/MovieCatalogServiceCollectionExtensions.cs:8-12` and no host calls it; `DirectoryMediaLibraryScanner` is `public sealed partial` with ctor `(IFileSystem, TimeProvider)` at `FileSystem/DirectoryMediaLibraryScanner.cs:14` and does no file access before `Scan`; `IMediaPlayerLauncher` is registered unconditionally as a singleton at `Playback/PlaybackServiceCollectionExtensions.cs:16-27` and returns `DisabledMediaPlayerLauncher` at `:19-21` when the flag is false; the conditional `ProcessEnrichment` block with its validator sits at `RabbitMq/RabbitMqServiceCollectionExtensions.cs:40-51` and is decided by `IsConsumerActive` at `:59-60`; and the `AddHandler` helper and its `Compose` order are at `Pipeline/ServiceCollectionExtensions.cs:14` and `:50-65`. **DEV-307 is not merged at this HEAD** — `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj:20-24` references Infrastructure, ServiceDefaults and Tests.Common but neither the Api nor `Microsoft.AspNetCore.Mvc.Testing`, and no factory or health-route test file exists in that project. That is the Q1 prerequisite in its unfulfilled state, and every harness statement below is written against what it will be once DEV-307's code is merged.

## Summary

Give the existing minimal Api host the four things the ticket names, and nothing else: the seven handlers DEV-309 and DEV-310 will dispatch, registered through the existing `AddHandler` helper so each resolves through Tracing → Logging → Validation → handler; the Infrastructure adapters those handlers need, which means two bindings that exist nowhere today; one exact-origin CORS policy for the Vite dev server; and the `/api` route-group convention the two endpoint tickets extend, with two deliberately empty feature mappers.

The whole ticket is composition. Five production C# files change — one edited, four new — plus one disposable `.gitkeep` deletion, plus two host-test files. The two adapter bindings are the only production work that is not a single line in `Program.cs`, and both are forced: with `ValidateOnBuild` on, the catalog and the scanner are the two missing links between the manifest and a host that starts at all (Design §3). `Program.cs`'s existing relative order is preserved exactly, because `AddLamuFlixRabbitMq` decides whether the enrichment consumer is active by looking at what was registered before it (Design §1).

**What this plan does not deliver, and says so in the places a reader will look for it.** No DEV-376 acceptance criterion, no live 422 proof, and no input-validation coverage for the seven rows — their validator sequences are empty today, so graph resolution is evidence of composition and nothing more (`spec.md` FR-008, FR-042, FR-043).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`), `Nullable` enable, `TreatWarningsAsErrors` true, central package management on (`harness.yml`, `constitution.md:318`)

**Primary Dependencies**: the versions that exist today, unchanged. `System.IO.Abstractions` 22.3.0 (`Directory.Packages.props:28`), `RabbitMQ.Client` (`:9`), `Npgsql.EntityFrameworkCore.PostgreSQL` (`:12`), `Microsoft.EntityFrameworkCore` (`:10-11`), `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and the `Microsoft.AspNetCore.App` shared framework the Api's `Microsoft.NET.Sdk.Web` supplies — which is where CORS comes from. `xunit.v3`, `Shouldly` and `NSubstitute` for the tests. **This ticket adds no package and moves no pin** (FR-039, SC-013). The one package the host tests need, `Microsoft.AspNetCore.Mvc.Testing`, is DEV-307's to add; DEV-308 consumes it and writes no project file.

**Storage**: untouched. The host registers `AddDbContext<LamuFlixDbContext>` and the catalog, and **no connection is ever opened** in this ticket's tests. The persistence path is asserted by resolution inside a created scope, not by a round trip (`constitution.md:299-301`; FR-034).

**Testing**: xUnit v3 with Shouldly (`constitution.md:292-298`), through the **inherited** `WebApplicationFactory<Program>` in `tests/LamuFlix.IntegrationTests` (`constitution.md:288`). Real `Program.cs` composition, DEV-307's health-check stubs only, `ValidateOnBuild` and `ValidateScopes` on. No Testcontainers, no WireMock server, no data-access driver mock. The seven manifest rows are one `[Theory]` with `[MemberData]` so DEV-309 and DEV-310 extend coverage by adding a row (`constitution.md:302-303`; Design §6).

**Target Platform**: the ASP.NET Core Minimal API host on `net10.0`, running locally on Windows for the host proof. The one browser-facing surface is `http://localhost:5173`, the Vite dev server.

**Project Type**: edits inside the existing `LamuFlix.Api` and `tests/LamuFlix.IntegrationTests` projects. **No new project, no new top-level folder, no architectural layer, no new project-reference edge** (`PRODUCT.md:44`; FR-017).

**Performance Goals**: none. The ticket states none and this plan invents none. The only timing recorded is what the gate scripts already measure.

**Constraints**: seven manifest rows and four exclusions, no more (FR-003, FR-004); one `MapDefaultEndpoints()` call, consumed once (FR-025); one exact-origin CORS policy, no options class, no configuration section, no package (FR-019 to FR-022); two feature mappers that map zero routes and declare no route shape (FR-027); `Features:LocalPlay` off in the host proof and the gate itself untouched (FR-015); no explanatory comment in any file this ticket touches (FR-038); the existing relative adapter order preserved (FR-016); no file outside the frozen list deleted or rewritten (FR-036, FR-045).

**Scale/Scope**: **8 files change** — 5 production C# files (1 edited, 4 new), 1 `.gitkeep` deletion, 2 new test files — plus **1 conditional** entry, the previously approved move-only extraction of the DEV-307 factory, which is a **move rather than a change** and is named in the receipt before any edit. Nothing is deleted but the placeholder. 0 pins added, 0 pins moved, 0 configuration sections, 0 migrations, 0 OpenAPI or TypeScript changes.

## Constitution Check

*Gate: must pass before research; re-checked after design. Every gate cites its principle number, per `constitution.md:529-530`.*

- **I. Ports and Adapters with a Feature-Organised Core** (`:93-126`) — **PASS.** The Api already references Core and Infrastructure (`LamuFlix.Api.csproj:8-9`); **no project-reference edge is drawn or removed**. Every type this ticket registers is either a Core handler (`LamuFlix.Core/Features/**`) or an Infrastructure implementation already behind a Core port. The two new bindings cross no boundary: `IMovieCatalog` and `IMediaLibraryScanner` are both Core ports, and `EfMovieCatalog` and `DirectoryMediaLibraryScanner` are both Infrastructure adapters. No new port, no new adapter type, no stub, no test double substituted for production. `LamuFlix.ArchitectureTests` must stay green.
- **II. Explicit Handlers and Decorators (No MediatR)** (`:128-145`) — **PASS, and this ticket is its implementation at the host.** `:133-136` requires handlers registered explicitly through `services.AddHandler<THandler, TCommand, TResult>()` with the order Tracing → Logging → Validation → handler, and forbids MediatR, pipeline-behaviour frameworks, reflection scanning and source-generated buses. FR-002 delivers exactly one explicit `AddHandler` call per row in one file; FR-006 restates the order; the order itself is composed by the existing helper at `Pipeline/ServiceCollectionExtensions.cs:50-65` and **not authored here**. `:137-139` keeps the single `IExceptionHandler` in `LamuFlix.Api` — FR-031 preserves it unchanged and adds no second mapper. The six named exceptions keep their one mapping; none of them is moved.
- **V. Validated Inputs, Consistent Error Responses** (`:196-216`) — **PASS, with the honest limit recorded rather than papered over.** `:197-199` requires every command and query to be validated through the validation decorator and a failure to be 422. **This ticket adds no endpoint and no validator**, so it neither satisfies nor relaxes that requirement; the seven rows' validator sequences are empty today (part 2:10-18), so FR-008 forbids any claim of validation coverage here and FR-042 forbids it in the PR body. Endpoint-level validation stays mandatory for DEV-309 and DEV-310, and neither is blocked by this ticket. `:200-212` requires one `ProblemDetails` producer and one `IExceptionHandler` carrying `traceId`; FR-031 leaves `AddProblemDetails` and `AddExceptionHandler<ValidationExceptionHandler>` exactly as they are, and **no text in this ticket claims the 422 path is proved** (FR-042, SC-014).
- **VI. Observability Across Processes** (`:218-239`) — **PASS, by construction.** `:235-236` gives `ServiceDefaults` ownership of Serilog, OpenTelemetry, health checks, options validation and `TimeProvider` registration, and forbids hosts duplicating that wiring. This ticket **consumes** the shared defaults: it calls `AddServiceDefaults` (DEV-307's line) and does not register Serilog, telemetry, health checks or a second clock. FR-013 forbids a second `TimeProvider` registration, and the two existing Infrastructure `TryAddSingleton(TimeProvider.System)` calls stay untouched. `:232-234` requires the health routes to exist and be anonymous; FR-025 keeps `app.MapDefaultEndpoints()` at exactly one call site so the Api maps neither route itself. No telemetry name is written as a literal anywhere in this ticket.
- **VII. Configuration Isolation and Deterministic Time** (`:241-260`) — **PASS.** `TimeProvider.System` is consumed through the shared defaults' single `TryAddSingleton` (FR-013) and injected into the scanner through its constructor. No `DateTime.Now`-family member is introduced — the family is banned and `RS0030` is an error. The CORS origin is a fixed local `const` in `Program.cs`, not a configuration key (FR-022), so no environment-specific value enters configuration. **No connection string, API key, path or other machine detail is added**: the host tests supply a syntactically valid placeholder connection string that is never opened, and the test-owned key set is transient (FR-035).
- **VIII. Ubiquitous Language in English** (`:262-280`) — **PASS.** All identifiers are English and functionality-named; no vendor name escapes into a Core identifier. **No `CONTEXT.md` entry is required**: `:277` binds that rule to a new *domain term*, and this ticket introduces no domain vocabulary — a route-group convention and a CORS policy name are wiring vocabulary. The one taste value it does introduce, the CORS policy name, is recorded in `ASSUMPTIONS.md` as `[assumed]` with its basis, which is where `PRODUCT.md` section 4 puts it. Recorded so a reader auditing `CONTEXT.md` reads it as a ruling and not a miss.
- **IX. Test Pyramid with Real Infrastructure** (`:282-309`) — **PASS, once DEV-307's code is on the base, and recorded as a prerequisite rather than claimed today.** The `:288` table places API end-to-end work in `LamuFlix.IntegrationTests` with `WebApplicationFactory`, and FR-032 puts both host test files there and drives them through the real `Program.cs`. The remaining rules hold by construction: xUnit v3 only, Shouldly only, `Theory` + `MemberData` over repeated `Fact`s for the seven rows (Design §6), Arrange-Act-Assert, and no `.Result`, `.Wait()` or `.GetAwaiter().GetResult()` anywhere. `:299-301` forbids mocking infrastructure and requires Testcontainers for Postgres and RabbitMQ — **this ticket mocks nothing and starts no container, and that is not a violation**, because neither test queries translation, serialises against the driver, delivers a message or reads dependency readiness. FR-034 states the boundary rather than hiding it: container-free host startup proves the DI graph and `/health/live`, **not** live database or broker behaviour, and no test in this ticket claims otherwise. **The one thing that is not satisfied at this HEAD is reachability**: the project carries no `ProjectReference` to the Api and no test-hosting package, and the DEV-307 factory does not exist. That is FR-033's prerequisite and the Q1 ruling's merge condition, not a design choice this plan may take silently. `MockFileSystem` stays available in the test project for future scanner tests and is **not** substituted here, because FR-032 requires the real registrations.
- **Technology Stack Constraints** (`:311-342`) — **PASS.** `:325` selects `System.IO.Abstractions`; FR-010 binds the real `FileSystem` from that already-pinned package, so the constitution's own choice is what ships and no new dependency is introduced. `:318` keeps versions in `Directory.Packages.props` — this ticket writes nothing there. `:320` is untouched: no schema, no migration, no snapshot. No vendor package is added and no existing pin moves.
- **PR Quality Gates** (`:346-376`) — **PASS, with three halves named explicitly rather than waved through.** Project references respected and `LamuFlix.ArchitectureTests` green; no MediatR and no reflection dispatch; no new port and therefore no ADR; no secrets, machine paths or magic `IConfiguration` strings; `CancellationToken` untouched (no new async path is written). **The `TypedResults` half is N/A**: the two feature mappers map zero routes, so no `IResult` and no `Results<…>` union exists to bind to — which is exactly why the mapper shape is the thing Design §5 reasons about. **The `WebApplicationFactory` half is met by reuse**, not by a new project reference (FR-033). **The committed OpenAPI document half is not engaged**: this ticket adds no DTO and no route, so nothing enters the C# → OpenAPI → TypeScript chain and `web/src/api/openapi.json` and `web/src/api/types.ts` are untouched (FR-037). The comment rule at `:418-428` is met by adding **no** comment anywhere (FR-038) — including the XML convention comment that was proposed and rejected, and including any comment in the tests.
- **Static-Analysis Gates** (`:378-395`) — **PASS by construction, with one item called out as the risk it is.** No method this ticket writes exceeds the harness's complexity ceiling: `Program.cs` is a flat list of top-level statements, and the registration extension is three grouped calls. **The inspection gate is the one that may complain**, because two of the mappers return a group builder they were handed and declare no route; FR-041 requires that finding to be fixed by changing the code's shape and forbids a suppression, a pragma or an exclusion (Design §5). No threshold is lowered and no gate is skipped to make one pass.
- **Async and cancellation** (`:433-435`) — **PASS.** This ticket writes no asynchronous code path. The registration extension is synchronous, as `AddHandler` and the Infrastructure extensions already are, and the endpoint mappers are synchronous mapping calls. No `async void`, no `.Result`, no `.Wait()`.
- **Packages** (`:440-441`) — **PASS by construction.** No `PackageVersion` and no `PackageReference` is written. The vulnerable-packages gate is still run and its exit code recorded (FR-039), because "no pin moved" is a claim the diff proves and the gate corroborates.
- **ADR** — **none.** No new port, no architectural layer, no public type outside the Api executable, and no versioning decision (FR-029). ADRs are Keel's, not the writer's (`constitution.md:478-479`).
- **Owner checkboxes** (`PRODUCT.md:36`) — **none open, and none needed.** The grill closed at Q8 with Q9 to Q12 unused; every structural choice is a cited Patron ruling in `CONCLUSIONS.md`, the one taste value is in `ASSUMPTIONS.md`, and the two conditional file entries are decided at pickup by observation. Gate 1 is the pipeline's own step and this plan neither claims it open nor claims it passed.

**Status: every gate below is met or met-with-a-named-condition; none rests on an answer that has not been given.** The single condition is reachability: the harness FR-032 names is DEV-307's to introduce, and the Q1 ruling makes its merge the entry condition for Phase B. That is a prerequisite with a named owner and a receipt, not a gap in the design.

## Project Structure

```text
src/LamuFlix.Api/
├── Program.cs                        (edit: AddMovieCatalog() after AddLamuFlixPersistence();
│                                        AddLamuFlixHandlers() after AddLamuFlixPlayback();
│                                        AddCors with the DevSpa policy; UseCors(DevSpa) after
│                                        UseExceptionHandler; MapApiEndpoints() after
│                                        MapDefaultEndpoints(). Two local const values. The
│                                        existing lines and their relative order are untouched.)
├── HandlerRegistration.cs            (new, internal: TryAddSingleton<IFileSystem, FileSystem>();
│                                        AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>();
│                                        seven explicit AddHandler rows)
└── Endpoints/
    ├── ApiEndpoints.cs               (new, internal: MapApiEndpoints(this IEndpointRouteBuilder);
    │                                    owns the one /api group, calls the two feature mappers)
    ├── LibraryEndpoints.cs           (new, internal: MapLibraryEndpoints(this RouteGroupBuilder);
    │                                    zero routes, returns the group)
    ├── ImportEndpoints.cs            (new, internal: MapImportEndpoints(this RouteGroupBuilder);
    │                                    zero routes, returns the group)
    └── .gitkeep                      (delete: disposable placeholder, folder now populated)

src/LamuFlix.Infrastructure/          (UNCHANGED — the two bindings consume what already exists:
│                                        MovieCatalogServiceCollectionExtensions.cs:8-12 and
│                                        FileSystem/DirectoryMediaLibraryScanner.cs:14)

tests/LamuFlix.IntegrationTests/
├── ApiHostCompositionTests.cs        (new: AC1 host start; the AC2 seven-row theory plus one full
│                                        Tracing -> Logging -> Validation -> handler walk; the
│                                        route-surface check)
├── ApiCorsTests.cs                   (new: AC3, three cases — allowed GET, allowed preflight,
│                                        rejected origin asserted as header absence)
└── <the DEV-307 factory>             (CONDITIONAL, decided at pickup: a same-project move-only
                                         extraction. Named in the receipt and in the touched-file
                                         list before any edit. No behaviour change.)
```

**Eight files change; one is a deletion; one is conditional.** Five production C# files, two new test files, and the disposable `.gitkeep`. The scanner binding lives **inside** `HandlerRegistration.cs` rather than in a new Infrastructure extension, which is why the two forced adapter fixes add no sixth production file and no Infrastructure edit at all (Design §3). `LamuFlix.Api.csproj` is **unchanged**: the Api already references Infrastructure, and the ticket's Scope 1 sentence is a statement about what the host registers, not a project-reference to add.

**No `research.md`, `data-model.md`, `contracts/` or `quickstart.md`**, and the reason is stated under **Note on the Spec Kit phases** rather than left implicit.

## Design

### 1. `Program.cs`, and why the order is a design decision

The ticket's Scope 4, Scope 2 and the shared defaults are all delivered by one file, and its **call order is load-bearing rather than incidental**. The fifteen steps, with the four insertions marked:

| # | Call | Source |
|---|---|---|
| 1 | `AddServiceDefaults` | inherited (DEV-307) |
| 2 | `AddLamuFlixPersistence` | inherited |
| 3 | **`AddMovieCatalog`** | **new — Design §3, Fix 1** |
| 4 | `AddMetadataProvider` | inherited |
| 5 | `AddLamuFlixRabbitMq` | inherited |
| 6 | `AddLamuFlixPlayback` | inherited |
| 7 | **`AddLamuFlixHandlers`** | **new — Design §2** |
| 8 | **`AddCors` with the `DevSpa` policy** | **new — Design §4** |
| 9 | `AddProblemDetails` / `AddExceptionHandler` | inherited, unchanged |
| 10 | `Build` | inherited |
| 11 | `UseExceptionHandler` | inherited, unchanged |
| 12 | **`UseCors(DevSpa)`** | **new — Design §4** |
| 13 | `MapDefaultEndpoints` | inherited (DEV-307), called once |
| 14 | **`MapApiEndpoints`** | **new — Design §5** |
| 15 | `Run` | inherited |

**Why 3 sits between 2 and 4 rather than anywhere else.** `AddMovieCatalog` registers a scoped `EfMovieCatalog`; placing it immediately after persistence keeps the persistence-then-catalog-then-metadata sequence readable and puts the catalog next to the repository it shares a `DbContext` with.

**Why 4 must stay before 5, and this is the sharpest edge in the ticket.** `AddLamuFlixRabbitMq` calls `IsConsumerActive`, which asks whether `IMetadataProvider` **and** `IMovieRepository` are already registered (`RabbitMq/RabbitMqServiceCollectionExtensions.cs:40, 59-60`). Persistence supplies the repository at `:37` and the metadata provider supplies the port at step 4, so the **active** branch runs and `ProcessEnrichmentCommandHandler`, its validator, the `RabbitMqOptions` validation and the `EnrichmentConsumer` hosted service are all registered. Reorder steps 4 and 5 — or move either after the RabbitMQ call — and the host silently takes the inactive branch: no validator, no options validation, and a different hosted service. **Nothing in this ticket's tests would fail on that swap**, which is why the order is a stated design fact (FR-016) and a boundary check (Design §8) rather than a preference.

**Why 7 comes after 6 and not earlier.** The registration extension binds the scanner and the seven handlers, and rows 7 to 10 consume services the persistence, catalog, RabbitMQ and playback registrations supply. Placing it last among the adapters means every dependency it names already exists in the collection. It would also work earlier — the container is not validated until `Build` at step 10 — but "register after what it depends on" is the order a reader can check.

**Why 8 is a service registration and 12 is middleware.** `AddCors` builds the policy; `UseCors` applies it. Splitting them across steps 8 and 12 is the framework's shape, and both halves must carry the same policy name, which is why the name is one `const` rather than a string written twice (Design §4).

**The two constants.** The policy name and the origin are local `const` values in `Program.cs`, used by steps 8 and 12 and by nothing else. They are not a configuration key, because a development-only fixed origin is public configuration and not a machine detail (`CONCLUSIONS.md` Q3), and they are not a type, because a one-consumer policy does not need a class (FR-022).

### 2. `HandlerRegistration.cs`, and the shape of the seven rows

One `internal static class` with one `internal static IServiceCollection` extension on `IServiceCollection`, called from step 7 and written in three groups in this order:

1. `TryAddSingleton<IFileSystem, FileSystem>()` — the existing real adapter from the already-pinned `System.IO.Abstractions` 22.3.0. `TryAdd`, not `Add`, so an inherited or test registration is preserved rather than replaced (`CONCLUSIONS.md` Q6 final).
2. `AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>()` — the existing implementation. Scoped is `ValidateScopes`-safe: it consumes a singleton filesystem adapter and a singleton clock, and it sits in a scoped import graph whose repository and catalog are both scoped.
3. The seven `AddHandler` rows, one per service contract.

| Contract | Handler | Request → Response | Endpoint ticket |
|---|---|---|---|
| `ICommandHandler<RequestEnrichmentCommand, MovieId>` | `RequestEnrichmentCommandHandler` | `RequestEnrichmentCommand` → `MovieId` | DEV-310 |
| `ICommandHandler<ImportMovieFolderCommand, MovieId>` | `ImportMovieFolderCommandHandler` | `ImportMovieFolderCommand` → `MovieId` | DEV-310 |
| `IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>` | `BrowseMoviesQueryHandler` | `BrowseMoviesQuery` → `PagedResult<MovieSummary>` | DEV-309 |
| `IQueryHandler<GetMovieDetailsQuery, MovieDetails>` | `GetMovieDetailsQueryHandler` | `GetMovieDetailsQuery` → `MovieDetails` | DEV-309 |
| `ICommandHandler<PlayMovieCommand, Unit>` | `PlayMovieCommandHandler` | `PlayMovieCommand` → `Unit` | DEV-310 |
| `ICommandHandler<AddToWatchlistCommand, Unit>` | `AddToWatchlistCommandHandler` | `AddToWatchlistCommand` → `Unit` | DEV-310 |
| `ICommandHandler<RemoveFromWatchlistCommand, Unit>` | `RemoveFromWatchlistCommandHandler` | `RemoveFromWatchlistCommand` → `Unit` | DEV-310 |

**The `Unit` rows are not a special case.** `Unit` is the response type `AddHandler`'s second type argument takes, and a `204`-shaped command still needs a response type to compose the decorator chain with. The row is written exactly like the others.

**Three rows the file does not contain, and each absence is deliberate.** The four enrichment-internal handlers (`ApplyEnrichmentResult`, `ClaimEnrichment`, `RecordEnrichmentFailure`, `RequeueStrandedMovies`) are absent because no ticket names them and no production caller exists. `ProcessEnrichmentCommandHandler` is absent because it is registered — with its validator — inside the existing conditional block in Infrastructure, and `AddHandler` uses plain `AddScoped` (`Pipeline/ServiceCollectionExtensions.cs:28`), so a second row would **silently replace** the consumer's graph with a differently-validated one. No test would catch that swap, so its absence is a design fact and a boundary check, not an omission.

**Nothing else goes in this file.** No options, no health checks, no clock, no telemetry. The clock is the shared defaults' singleton and is consumed through the scanner's constructor, not re-registered (FR-013).

### 3. The two forced adapter bindings, and the boot-blocker rule

In the Api host as composed at this HEAD, **four of the seven rows cannot resolve** under `ValidateOnBuild`: rows 7, 8, 9 and 10. Two causes, both of which are missing links rather than missing code:

- **`IMovieCatalog` is unregistered.** `AddMovieCatalog` exists at `Persistence/MovieCatalogServiceCollectionExtensions.cs:8-12` and registers a scoped `EfMovieCatalog`, and **no host calls it**. Rows 8, 9 and 10 take `IMovieCatalog` as a constructor dependency.
- **`IMediaLibraryScanner` is registered nowhere.** The only implementation is `FileSystem/DirectoryMediaLibraryScanner.cs:14`, `public sealed partial` with ctor `(IFileSystem, TimeProvider)`; `Scan` at `:19-24` is where file access begins, so constructing it touches no disk. Row 7 takes it as a constructor dependency.

Both are fixed by **consuming what exists**, which is the distinction that keeps this ticket reversible:

- **Fix 1** is one line in `Program.cs` — `AddMovieCatalog()` at step 3. No new adapter, no new extension, no Infrastructure edit.
- **Fix 2** is the two bindings in Design §2's groups 1 and 2. No new adapter, no new extension, no Infrastructure edit, no port, no stub, no validator, no public registration method, and **no csproj change** because the `System.IO.Abstractions` pin and its `PackageReference` in Infrastructure already exist.

**Why the scanner binding lives in the Api's registration file rather than in an Infrastructure extension.** It has exactly one consumer, and the Api can see both types — `IMediaLibraryScanner` is a public Core port and `DirectoryMediaLibraryScanner` is a public Infrastructure type. An Infrastructure `Add…` extension would be a public assembly contract created for one line, which is care item 2's territory. If a second host ever needs the binding, that is the moment an extension earns its place, and it is a new ticket's decision.

**The boot-blocker rule, and what it is not.** `ValidateOnBuild` is on, so a gap surfaces at startup. A defect that prevents AC1 startup or the resolution of a manifest row forces its **minimum** fix inside this ticket, in whichever file that turns out to be, cited to the acceptance criterion or the failing gate. Three things that rule explicitly forbids, and all three are the failure modes a "helpful" implementer reaches for: narrowing the validation so the gap stops being reported; deferring the blocker to an endpoint ticket; and claiming green before the fix is in. Everything else — a non-blocking defect, a latent issue, anything outside the frozen list — is a follow-up issue filed on the owner's decision, not a finding in this round (FR-018, FR-045).

### 4. The CORS policy, and its middleware placement

One named policy, added at step 8 and applied at step 12, both referring to the same `const` name:

- `WithOrigins` — the single fixed dev-server origin. An exact origin, never a wildcard, because a wildcard and a credentialed request are mutually exclusive and this project is a single-consumer SPA.
- `AllowAnyMethod` and `AllowAnyHeader` — the ticket names a dev server, not a method allowlist, and an endpoint allowlist would freeze DEV-309's and DEV-310's method choices in a ticket that owns no endpoint.
- **No credentials.** Nothing in this application authenticates a browser request, and permitting credentials with a fixed origin is a capability no ticket asked for.
- `WithExposedHeaders("Location")` — the one header beyond the browser's defaults, because DEV-310's import and enrichment responses are specified to answer `202` with a `Location` header, and a browser cannot read a response header the server did not expose. The word **beyond the defaults** matters: `Location` is exposed *in addition to* the CORS-safelisted response headers, not instead of them.

**Placement, and why it is where it is.** CORS runs after routing and before endpoint execution, with the exception handler preserved ahead of it and unchanged (FR-021). The exception handler stays outermost, so a handler that throws produces the same problem document whether or not CORS applies. `UseCors` sits after `UseExceptionHandler` at step 12 and before `MapDefaultEndpoints` at step 13, so a preflight is answered by the CORS middleware and a real request still reaches its endpoint.

**What the CORS test asserts, and where each assertion belongs.** `Expose-Headers` is meaningful on the response that actually carries the header, so it is asserted on the real `GET`. A preflight carries allow-origin, allow-method and allow-headers, and the assertion **stops there** — asserting `Location` on a preflight would be asserting something a browser never reads at that point. A rejected origin is asserted as the **absence** of `Access-Control-Allow-Origin`, not as a status code: what the middleware returns to a disallowed origin is not what this ticket promises, and pinning a status would pin an implementation detail the framework does not guarantee (FR-023, FR-024; Design §6).

**No dependency, no configuration, no type.** The ASP.NET Core shared framework supplies CORS, the origin is a `const`, and there is no options class and no configuration section (FR-022). A future production origin is separate scoped work with its own ticket.

### 5. The endpoint convention, and the finding it will draw

`MapApiEndpoints(this IEndpointRouteBuilder)` owns the one root group at `/api` and calls each feature mapper in turn. The prefix is fixed here because it is **ticket text**: DEV-309's own title is "GET /api/movies and GET /api/movies/{id}", and a root group with no prefix would mean every endpoint ticket re-decides the prefix. `MapLibraryEndpoints` and `MapImportEndpoints` are extensions on the shared `RouteGroupBuilder`, and each **maps zero routes and declares no sub-prefix, no status code and no DTO**, returning the group it was handed.

**Why empty is the deliverable, not a stub.** DEV-309 names `Endpoints/LibraryEndpoints.cs` itself and DEV-310 owns the import surface. Writing a sub-prefix, a status code or a DTO today would freeze route shape that belongs to two other tickets, and adding placeholder mappers for the other features would be speculative scope (FR-027, FR-029).

**The finding the inspection gate will probably raise, and the only permitted fix.** A method that receives a builder and returns it without mapping anything is a shape two engines dislike: a return value nothing consumes, and a class too small to be worth a file. **The fix is the code's shape** — for example, having the aggregator use the return value rather than discard it, or expressing the feature calls as a sequence whose value is returned — and never a suppression, a pragma, an exclusion or a threshold move (FR-041). This is called out in the task that writes the file, so the implementer meets it as a known hazard rather than as a surprise.

**Visibility is `internal`, and no `InternalsVisibleTo` is needed.** The Api is an executable; nothing outside it calls these methods, and the tests reach the convention through the host's endpoint data source rather than by calling the mappers (FR-026, FR-030).

**The convention is written down once, and it is not in the code.** The project forbids explanatory comments, so the rule — one static class per feature in `Endpoints/`, a `MapXxxEndpoints(this RouteGroupBuilder api)` extension, a call added in the aggregator, no P1 versioning — lives in `brief.md` §3 Q5 and in `spec.md` FR-026, and the names carry it (FR-038).

**Why `Endpoints/.gitkeep` is deleted and not kept.** It is a disposable placeholder in a folder the ticket names, and once three real files occupy the folder it is noise. The deletion is the minimum tidy-up and the only deletion in the ticket (FR-028).

### 6. The host tests, and the shape that makes them extendable

Both files live in `tests/LamuFlix.IntegrationTests` and drive the **inherited** `WebApplicationFactory<Program>`, running the real `Program.cs` composition with only DEV-307's health-check stubs in place (FR-032). `ValidateOnBuild` and `ValidateScopes` are on, so a missing adapter is a startup failure the suite reports once rather than a first-request failure a user finds.

**The configuration the host needs, and when it must arrive.** The host reads several values at composition time — the connection string is read *inside* `AddLamuFlixPersistence` and **throws** if it is missing (`Persistence/PersistenceServiceCollectionExtensions.cs:22-28`) — so the test's configuration has to reach the host **before the production registrations run**, not merely before the first request. A late configuration callback can be too late for a value read during composition, which is why the tests use the inherited recipe rather than assuming either ordering works, and why the pickup task verifies the host actually starts instead of trusting the mechanism. The key set, all transient and test-owned: `Library:RootPath`, `Omdb:ApiKey`, `Omdb:BaseUrl` (a valid URL, because the option carries a URL validator), `RabbitMq:HostName`, a syntactically valid placeholder `ConnectionStrings:DefaultConnection` that is **never opened**, and `Enrichment:ClaimLease` **strictly below** `RabbitMq:RetryDelay` — `00:00:10` against the default 30 s retry delay passes; `00:05:00` does not. **`Features:LocalPlay` is set explicitly to false** (FR-015, FR-035).

**`ApiHostCompositionTests.cs`**, three pieces of evidence:

- **AC1** — the host starts, and the Api is a Minimal API host: the composition completes and both `ICommandHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>` and the catalog resolve. The "no MVC or Razor" half is a **source and project-file check** rather than a runtime one, because MVC's absence is a fact about references and mapping calls, and asserting it through the service provider would be asserting the wrong thing. This composition test is a fixture, not a second proof of the same claim.
- **AC2** — one `[Theory]` with seven `[MemberData]` rows, one per manifest contract. Each row creates a scope on the started host, resolves its contract, and asserts the instance is a `TracingDecorator`. **The theory shape is the point**: DEV-309 and DEV-310 extend AC2 coverage by adding a row, and a repeated-`Fact` version would make that a copy-paste (Design §9, the row that records it). One row additionally walks the whole chain — tracing, then logging, then validation, then the concrete handler — reading each decorator's inner delegate, which is what proves the host's graph matches what `Compose` builds at `Pipeline/ServiceCollectionExtensions.cs:50-65`. **The walk asserts composition only.** It proves the decorators are present and in order; it proves nothing about validation, because all seven validator sequences are empty today, and no test name, comment or task note may imply otherwise (FR-008).
- **Route surface** — read the endpoint data source from the built provider. The required health routes are present, and no endpoint carries the `/api` business prefix. The assertion deliberately does **not** require the route set to equal the health routes, because DEV-307's own scope may include approved documentation routes that must survive; the inherited surface is recorded at pickup and preserved (FR-030). DEV-309 and DEV-310 will extend this one assertion when they add routes, which is intended friction.

**`ApiCorsTests.cs`**, three cases, on the real `/health/live` route the inherited defaults map: the allowed `GET` carrying allow-origin and `Expose-Headers: Location`; the preflight carrying allow-origin, allow-method and allow-headers and nothing further; and the `http://localhost:3000` request carrying no allow-origin header at all (FR-023, FR-024; Design §4).

**Accepted noise, and the one thing that must never be done about it.** `EnrichmentConsumer` retries against a broker that is not running and logs on each attempt; that is noise, asserted by nothing. Disposing the host while a broker connection attempt is in flight can block for as long as the client's connect timeout. **A hang is reported verbatim; no timeout is lengthened to hide it** (FR-044).

### 7. The two conditional entries, and how each is decided

Neither is a question for the owner. Both are observations made at pickup, recorded in the receipt before any edit.

- **The factory extraction.** If DEV-307 left a reusable factory type, both test files use it as-is and nothing moves. If it left one private or inline, **one same-project move-only extraction** happens: the same type, the same configuration, the same behaviour, relocated so two test classes can share it. It is named in the receipt and in the touched-file list **before** the edit, it carries no behaviour change, and there is no second factory. The decision is made by **reading the merged code**, not by asking.
- **The boot-blocker fix.** Design §3 fixes the two bindings this ticket expects to need. If `ValidateOnBuild` reveals a third gap, FR-018 governs: minimum fix, cited to the acceptance criterion or the failing gate, named in the receipt, brought into this ticket. The frozen list is the expected minimum, not a wall.

### 8. Boundary checks, as greps rather than judgements

Five greps run at the end of every phase from the first implementation phase on, so a later non-empty result is unambiguous:

- `rg -n "AddMovieCatalog" src` — exactly one call site, in `Program.cs` (Design §3, Fix 1).
- `rg -n "AddLamuFlixPersistence|AddMetadataProvider|AddLamuFlixRabbitMq" src/LamuFlix.Api/Program.cs` — the three inherited adapters present, in that order (FR-016; the `IsConsumerActive` edge).
- `rg -n "AddHandler" src/LamuFlix.Api` — exactly seven rows, and `rg -n "ProcessEnrichmentCommandHandler" src/LamuFlix.Api` returns **nothing** (FR-003, FR-005).
- `rg -n "TimeProvider" src/LamuFlix.Api` — no registration, only consumption (FR-013).
- `rg -n "MapDefaultEndpoints" src` — exactly one call site, and `rg -n "MapControllers|MapRazorPages|AddControllers|AddRazorPages" src` returns **nothing** (FR-001, FR-025, FR-038).

### 9. Choices this plan makes, recorded as branches rather than rulings

| Choice | Branch taken | Why it needed no ruling |
|---|---|---|
| The extension method's name and shape | `AddLamuFlixHandlers()` on `IServiceCollection`, in an `internal static class`, mirroring the `AddLamuFlix*` naming the Infrastructure extensions already use | the file is named by `CONCLUSIONS.md` Q2 and the shape is fixed by Q6; only the identifier is a writer's choice, and matching the existing family costs nothing. Naming it `AddHandlers` would collide conceptually with the Infrastructure `AddHandler` singular, which is one handler's helper |
| Where `IMovieCatalog` is bound | a call to the **existing** `AddMovieCatalog()` in `Program.cs` | the extension exists at `MovieCatalogServiceCollectionExtensions.cs:8-12` and no host calls it; writing a second registration for a service that already has one would be a duplicate, and `AddMovieCatalog` at step 3 is the only call site |
| Where `IMediaLibraryScanner` is bound | the Api's `internal` registration file, `AddScoped` | one consumer, both types public, no new public contract (Design §3). Scoped is forced by the graph around it, not chosen for tidiness |
| `IFileSystem` lifetime and style | `TryAddSingleton<IFileSystem, FileSystem>()`, real adapter | `TryAdd` is the pattern the two existing clock registrations already use, and the constitution picks this package at `:325`; a test double belongs to a future scanner test, not to this composition |
| The CORS policy type | none — a local `const` name and a local `const` origin, and a lambda passed to `AddCors` | one consumer, no options class, no configuration section (`CONCLUSIONS.md` Q3). A dedicated `DevSpaCorsPolicy` type would be ceremony for one call site |
| The `/api` prefix's source | the DEV-309 ticket title | it is ticket text, so care item 4 is not triggered; the alternative — no prefix until DEV-309 — would push the decision into two tickets at once |
| Whether the aggregator returns or discards | shaped so the aggregator **uses** its return value | the two near-empty mappers are the inspection gate's most likely finding, and the permitted fix is shape (FR-041, Design §5). The exact shape is the implementer's, judged against "no suppression" |
| Test location | `tests/LamuFlix.IntegrationTests` for both files | `constitution.md:288` places API end-to-end work there and DEV-307's harness lives there; `tests/LamuFlix.Test` is transitional (`:492-495`), and splitting host tests across two projects would contradict both |
| The seven rows as one theory plus one walk, rather than eight assertions in one test | one `[Theory]`, seven rows, plus a separate full-walk case | `constitution.md:302-303` prefers `Theory` + `MemberData`, and the theory is what makes DEV-309's and DEV-310's extension a matter of adding a row. The full walk is a separate case because it reads the chain rather than the outermost type |
| Where the "no MVC or Razor" proof lives | a project-file and source check, beside the host-start test | MVC's absence is a fact about references and mapping calls; asserting it through a running provider would assert a proxy for the claim, not the claim |

## 10. Test Strategy

Two new test files in `tests/LamuFlix.IntegrationTests`, both through the inherited `WebApplicationFactory<Program>`, both running the real composition, no container and no mock of the data-access driver anywhere.

**`ApiHostCompositionTests.cs`** (new) — AC1 as the host starting with a valid graph; AC2 as the seven-row theory plus the one full walk; the route-surface check. Full detail in Design §6. The configuration it supplies is the transient key set named there, with `Features:LocalPlay` explicitly false, and **the play row's resolution is asserted to contain the disabled launcher** so that a future change to the playback registration cannot quietly make the row pass only when the flag is on. **No comment in the file**, and the AAA headers the project permits are the only comment-shaped text in it (FR-038).

**`ApiCorsTests.cs`** (new) — AC3's three cases, on the inherited `/health/live` route: allowed `GET` with allow-origin and `Location` exposed; preflight with allow-origin, allow-method and allow-headers; `http://localhost:3000` with no allow-origin header. Full detail in Design §4 and §6.

**What no test in this ticket claims, stated rather than hidden.** No round trip to PostgreSQL or RabbitMQ: the graph is proved by resolution inside a created scope, and the liveness route runs no check (FR-034). No validation behaviour: all seven validator sequences are empty, so resolution is composition evidence and nothing more (FR-008). No 422 path: this ticket owns no endpoint, so no request can reach a validator (FR-042). No live database or broker health: `/health/ready` depends on checks this ticket does not stub and does not assert.

**No property test.** This ticket adds no domain invariant — no value object, no state transition, no query model — so the property gate is expected to report scope-empty, and the documented opt-out is recorded in the task note under the pipeline's own rule (FR-040). That is a recorded outcome, not a skipped gate.

**Mutation is stage-appropriate, and a survivor is a missing test.** The composition code this ticket adds is registration, which a mutant can silently weaken — a dropped `TryAdd`, a swapped lifetime, a removed row. Where the gate is scoped to the projects it covers, its result is reported as itself; where a mutant survives in changed code, the answer is a test or a recorded reason, never a waiver (FR-040).

## 11. Gates

Governed by `harness.yml` and each script's own `-Help`. **No flag, threshold or gate list is invented here, and no threshold is lowered** (`AGENTS.md`; `CONCLUSIONS.md` Q8). After every `.cs` task, the three static-analysis gates and the format check run on the files that task touched; at close-out they run once over the whole changed set, with complexity at the harness's refactor ceiling as well as its implementation ceiling.

### Close-out

1. The three static-analysis gates over all seven changed C# files, complexity at both ceilings the harness defines, then `dotnet format --verify-no-changes`.
2. Full `dotnet test`, compared against the pickup baseline recorded at T001 — actual failures and coverage, not a rigid only-additions count (FR-040).
3. The property-test gate, run and reported, with the scope-empty opt-out recorded.
4. The vulnerable-packages gate, run and its exit code recorded. No pin was added, so the gate corroborates rather than investigates — and it is still run.
5. The mutation gate at its proper stage, with survivors routed to a test.
6. The boundary greps of Design §8, and the frozen-list diff check: `git diff --stat <base>...HEAD` limited to the eight files under Project Structure, plus the conditional factory extraction when the receipt records it.

**Three result rules, carried verbatim from `CONCLUSIONS.md` Q8 and not restated in weaker form.** A required applicable gate must pass; an exit code of 1 or a `Could not run` **blocks**. An applicable gate reporting exit 2 because its scope is empty **also blocks**; only a gate genuinely disabled in `harness.yml` is a non-blocking `SKIP`. And **a surviving mutant is a missing test, not a reason to waive it** (FR-039, FR-040).

## 12. Coverage of the ticket's Closing Bar

| Item | Where satisfied |
|---|---|
| AC1 — Api starts cleanly as a Minimal API host with no MVC or Razor | Design §1, §3, §6; FR-001, FR-014, FR-032, FR-038; `ApiHostCompositionTests` host start plus the project-file check; SC-001, SC-002 |
| AC2 — a test resolves a registered handler from the Api host's provider and gets the decorated pipeline | Design §2, §6; FR-002, FR-003, FR-006, FR-007, FR-008; the seven-row theory and the one full walk; SC-003 |
| AC3 — CORS allows the React dev server origin | Design §4, §6; FR-019 to FR-024; `ApiCorsTests` three cases; SC-007 |
| Scope 1 — handlers registered through `AddHandler` with the Validation, Logging and Tracing decorators | Design §2; FR-001, FR-002, FR-006, FR-007; SC-003, SC-005 |
| Scope 2 — the Infrastructure adapters, and `TimeProvider` through host DI | Design §1, §2, §3; FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-015; SC-002, SC-004, SC-006 |
| Scope 4, first half — `app.MapDefaultEndpoints()` | Design §1 step 13, §5; FR-025; the Design §8 grep for exactly one call site; SC-009 |
| Scope 4, second half — the endpoint-group convention DEV-309 and DEV-310 extend | Design §5, §6; FR-026, FR-027, FR-028, FR-029, FR-030; the route-surface check; SC-009, SC-010 |
| Preserved: the exception mapper, the middleware order around it, the health routes, the worker registrations, the two Infrastructure clock registrations | Design §1 steps 5 and 9 to 11, §2, §3; FR-005, FR-013, FR-016, FR-031; the Design §8 greps; SC-005, SC-008, SC-013 |
| Frozen scope — nothing outside sections 3 to 6 of the brief | Project Structure; FR-036, FR-037, FR-045; the close-out frozen-list diff |
| Boot-blocker rule — minimum fix, cited, never deferred, validation never narrowed | Design §3, §7; FR-014, FR-018; SC-002 |
| DEV-376 — prerequisite supplied, nothing claimed, wording fixed | Design §2, §10; FR-042, FR-043; SC-014 |
| Gates and the outcome rules | §11; FR-039, FR-040, FR-041; SC-011, SC-012 |
| Review terms — M, two rounds, no self-merge, follow-ups not findings | `CONCLUSIONS.md` Q8; FR-046, FR-047 |

## 13. Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | This ticket records **no** constitution violation. It adds no project, no top-level folder, no architectural layer, no package, no project-reference edge, no port, no new domain term and no public type outside an executable. The nearest thing to a judgement call is the location of the scanner binding inside the Api's `internal` registration file rather than a new Infrastructure extension, and that is recorded in Design §3 and Design §9 as a branch with its reason, not as a deviation. DEV-307's one pre-existing deviation — `LamuFlix.ServiceDefaults` referencing `LamuFlix.Core` — is neither created nor deepened here, and this ticket does not touch that reference |
