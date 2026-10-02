# Feature Specification: Compose the LamuFlix.Api host (handler registration, adapter wiring, CORS, endpoint-group convention)

**Feature Branch**: `feature/308-spec`

**Created**: 2026-10-02

**Status**: draft for Gate 1. **No owner checkbox is open and none is needed** — the grill closed at eight questions (Q1 to Q8) with Q9 to Q12 unused, the brief is SETTLED, and `CONCLUSIONS.md` records every structural choice as a cited Patron ruling. **Phase B is nonetheless blocked on a prerequisite that is not a question**: DEV-307's *code* must be merged on the build base before any task in `tasks.md` starts (Q1, `brief.md:28-33`). Gate 1 is the pipeline's own step and is not claimed as open or closed here.

**Input**: DEV-308 (size M, UI false) — "Compose the existing minimal Api host for DEV-309 and DEV-310, which own the endpoints. Deleting LamuFlix.Web belongs to DEV-388." Scope 1 (Api references Infrastructure and registers handlers through `AddHandler`, so each resolves with its Validation, Logging and Tracing decorators); Scope 2 (register the Infrastructure adapters, and `TimeProvider` through host DI, see DEV-307); Scope 3 (CORS for `http://localhost:5173`); Scope 4 (`app.MapDefaultEndpoints()` from DEV-307, plus the endpoint-group convention `MapLibraryEndpoints`, `MapImportEndpoints`, ... that DEV-309 and DEV-310 extend). Acceptance criteria: AC1 Api starts cleanly as a Minimal API host with no MVC or Razor; AC2 a test resolves a registered handler from the Api host's service provider and gets the decorated pipeline; AC3 CORS allows the React dev server origin. Plus the settled brief `specs/DEV-308/brief.md` (Q1 to Q8), `CONCLUSIONS.md` (the eight Patron verdicts, append-only, Patron-owned), `ASSUMPTIONS.md` (one `[assumed]` naming entry), and the recon notes `recon-DEV-308`, `recon-DEV-308-handlers`, `recon-DEV-308-handlers-2`, `recon-DEV-308-handlers-3`.

**Short name**: `api-host-composition`

**Two things this specification deliberately does not do.** It claims **no DEV-376 acceptance criterion**: DEV-308 supplies the Api-host `AddHandler` registration prerequisite, and the live 422 proof belongs to the first migrated validating endpoint in DEV-309 or DEV-310 (`CONCLUSIONS.md` Q7, `brief.md:103-109`). And it claims **no input-validation coverage for the seven manifest rows**: all seven `ValidationDecorator`s have an empty validator sequence today (part 2:10-18), so graph resolution is not evidence that invalid input is rejected (Q6, `brief.md:101`). Neither claim is softened elsewhere in this document, in `plan.md`, in `tasks.md`, in the PR body or in the final report.

**Every `file:line` below was read at the HEAD of `feature/308-spec` during this drafting pass** (intake HEAD `6e0f09b`), not taken from a recon note. The two load-bearing claims that came from recon and were re-checked here are the state of the two forced adapter bindings — `AddMovieCatalog` exists at `Persistence/MovieCatalogServiceCollectionExtensions.cs:8-12` and no host calls it, and `IMediaLibraryScanner` is registered nowhere while its only implementation is `FileSystem/DirectoryMediaLibraryScanner.cs:14` — and the fact that the conditional `ProcessEnrichment` branch is kept alive by call order (`RabbitMq/RabbitMqServiceCollectionExtensions.cs:40-51` reads `IsConsumerActive`, which is satisfied because `AddLamuFlixPersistence` runs before `AddLamuFlixRabbitMq`).

## User Scenarios & Testing

### User Story 1 - The Api host starts, and its whole service graph is valid at boot (Priority: P1) 🎯 MVP

As the person who will run this process, I need the Api to boot as a Minimal API host with every registration it declares actually resolvable, so that a missing adapter is a startup failure I read once rather than a first-request failure a user finds for me.

**Why this priority**: AC1 makes it the ticket's first acceptance criterion, and it is the prerequisite for the other two: a host that does not start cannot resolve a handler (AC2) and cannot answer a CORS request (AC3). It is also the cheapest story to prove and the only one whose proof forces production code beyond `Program.cs`, because `ValidateOnBuild` turns each manifest row's dependency graph into a boot-time check.

**Independent Test**: start the Api through the inherited `WebApplicationFactory<Program>` with `Features:LocalPlay` off and the in-memory configuration of §Test evidence supplied, and read the built provider. The host starts, `ICommandHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>` and the other rows registered so far resolve, `IMovieCatalog` and `IMediaLibraryScanner` resolve, and the adapter graph is validated on build and on scope. No database and no broker are contacted.

**Acceptance Scenarios**:

1. **Given** the Api host, **When** it starts, **Then** it starts as a Minimal API host, and neither MVC nor Razor is present: no `AddControllers`, no `AddRazorPages`, no `MapControllers`, no `MapRazorPages`, and no MVC or Razor `PackageReference`.
2. **Given** the built host, **When** the provider is built, **Then** the whole graph is validated on build and on scope, so an unresolvable dependency is a startup failure rather than a first-request failure.
3. **Given** the built host, **When** `IMovieCatalog` is resolved, **Then** it resolves to the existing scoped `EfMovieCatalog`, because the existing `AddMovieCatalog()` is called from the host.
4. **Given** the built host, **When** `IMediaLibraryScanner` is resolved, **Then** it resolves to the existing `DirectoryMediaLibraryScanner`, scoped, built from the existing `IFileSystem` adapter and the inherited `TimeProvider`.
5. **Given** the built host, **When** `IFileSystem` is resolved, **Then** it resolves to the real `System.IO.Abstractions` `FileSystem`, registered with `TryAdd` so an inherited or test registration is preserved rather than replaced.
6. **Given** the built host, **When** `TimeProvider` is resolved, **Then** it resolves through the shared defaults' single registration, and this ticket adds no second one.
7. **Given** the host composition, **When** the existing calls are read, **Then** their relative order is unchanged — persistence, then metadata provider, then RabbitMQ — so the existing conditional `ProcessEnrichment` registration stays active exactly as it is today.
8. **Given** the host with `Features:LocalPlay` false, **When** `IMediaPlayerLauncher` is resolved, **Then** it resolves to the existing disabled launcher, and no media process is started, no gate changes, and the `Features:LocalPlay` flag itself is not touched by this ticket.

---

### User Story 2 - Every Api-facing handler resolves through the full decorator pipeline (Priority: P1)

As the developer writing the first endpoint, I need the seven handlers that DEV-309 and DEV-310 dispatch to be registered in the Api host already, each with the Tracing, Logging and Validation decorators already wrapped around it, so that I map a route and dispatch a command without touching dependency injection at all.

**Why this priority**: AC2 is the ticket's second acceptance criterion and Scope 1 is the whole of its first scope. It is P1 rather than P2 because both endpoint tickets are blocked on it: DEV-309 names `BrowseMoviesQuery` and `GetMovieDetailsQuery`, and DEV-310 names the other five, so a manifest that is short or wrong blocks two tickets rather than one.

**Independent Test**: start the host, create one scope, and resolve each of the seven service contracts from that scope. Each resolves to a `TracingDecorator`. One row additionally walks the whole chain — Tracing, then Logging, then Validation, then the concrete handler — reading each decorator's inner delegate, so the host graph is proved against the same order `AddHandler` composes.

**Acceptance Scenarios**:

1. **Given** a started host, **When** a scope is created and each of the seven service contracts is resolved, **Then** every one resolves, and each resolves to a `TracingDecorator`, so the tracing decorator is the outermost for all seven.
2. **Given** one manifest row, **When** its decorator chain is walked from the outside in, **Then** the order is Tracing, then Logging, then Validation, then the concrete handler.
3. **Given** the seven rows, **When** the registrations are read, **Then** they are seven explicit `AddHandler` calls, one per service contract, in one internal file in the Api project — no reflection, no assembly scanning, no convention-based discovery.
4. **Given** the host composition, **When** the worker-side registrations are read, **Then** they are untouched, and `ProcessEnrichmentCommandHandler` keeps its existing conditional Infrastructure registration and its existing validator rather than gaining a second row that would swap the consumer's graph.
5. **Given** the host, **When** a registered contract is resolved, **Then** resolution succeeding is evidence of **composition only**. No test, requirement or report in this ticket claims that invalid input is rejected for these rows, because their validator sequences are empty today.
6. **Given** the four enrichment-internal handlers, **When** the manifest is read, **Then** they are absent — `ApplyEnrichmentResult`, `ClaimEnrichment`, `RecordEnrichmentFailure` and `RequeueStrandedMovies` — because no ticket names them and nothing calls them.
7. **Given** `GET /api/genres` and `GET /api/people`, **When** this ticket's manifest is read, **Then** neither has a row, a port, a handler or a placeholder, because none exists yet and both belong to DEV-310.
8. **Given** the host, **When** the play handler is resolved, **Then** it resolves to a graph containing the disabled media player launcher, so AC2 holds with `Features:LocalPlay` off and no media is executed.

---

### User Story 3 - The React dev server may call this API, and no other browser origin may (Priority: P1)

As the developer running the Vite dev server, I need the Api to accept browser requests from `http://localhost:5173` and to expose the `Location` header DEV-310's import response will set, so that the SPA can talk to the API during development without a proxy and without a CORS error in the console.

**Why this priority**: AC3 is the ticket's third acceptance criterion and Scope 3 is the smallest of the four. It is P1 rather than P2 because the dev loop it unblocks is how both endpoint tickets get developed at all — without it every browser call fails before it reaches a handler.

**Independent Test**: through the started host, make a real request to the inherited `/health/live` route from the allowed origin, send a preflight from the allowed origin, and send a request from `http://localhost:3000`. The first carries allow-origin and `Location` in `Expose-Headers`; the second carries allow-origin, allow-method and allow-headers; the third carries no allow-origin header at all.

**Acceptance Scenarios**:

1. **Given** the started host, **When** `GET /health/live` is requested with `Origin: http://localhost:5173`, **Then** the response carries `Access-Control-Allow-Origin: http://localhost:5173` and `Access-Control-Expose-Headers` naming `Location`.
2. **Given** the started host, **When** a preflight is sent from `http://localhost:5173`, **Then** the response carries allow-origin, allow-method and allow-headers, and the assertion stops there — `Location` exposure is asserted on the actual response that sets it, not assumed to appear on a preflight.
3. **Given** the started host, **When** a request is sent from `http://localhost:3000`, **Then** the response carries no allow-origin header. The proof is the **absence** of that header, not a required denial status code.
4. **Given** the CORS configuration, **When** it is read, **Then** it is one named policy applied globally, allowing any method and any header from `http://localhost:5173` only, with no credentials, and with `Location` the only exposed header beyond the browser's defaults.
5. **Given** the middleware order, **When** the pipeline is read, **Then** CORS runs after routing and before endpoint execution, and the existing exception handler is preserved ahead of it and unchanged.
6. **Given** the CORS configuration, **When** its source is read, **Then** the policy name and the origin are local constants in `Program.cs`, and there is no options class, no configuration section and no separate CORS file.
7. **Given** the CORS configuration, **When** the dependency graph is read, **Then** no package is added for it — the ASP.NET Core shared framework already supplies CORS.

---

### User Story 4 - There is one fixed place for the endpoints DEV-309 and DEV-310 will add (Priority: P1)

As the developer writing the first endpoint, I need the Api's route-group convention already in place, so that adding `GET /api/movies` means adding a route inside a mapper that already exists rather than editing the composition root and inventing a shape.

**Why this priority**: it is the convention half of Scope 4, and the reason it is P1 rather than P2 is that both endpoint tickets extend it. Shipping the root group, the aggregator and the two ticket-named mappers is what lets DEV-309 and DEV-310 add routes with **no edit to `Program.cs`** at all.

**Independent Test**: start the host, read the endpoint data source from the built provider, and assert the two required health routes are present and that no endpoint carries the `/api` business prefix. Also read the inherited DEV-307 route surface at pickup and assert it is preserved.

**Acceptance Scenarios**:

1. **Given** the host, **When** endpoints are mapped, **Then** exactly one root group at `/api` exists, and every feature mapper receives that one group builder.
2. **Given** the two feature mappers, **When** the host starts, **Then** each maps **zero** routes and declares no sub-prefix, no status code and no DTO, because the route shape belongs to DEV-309 and DEV-310.
3. **Given** the host's endpoint data source, **When** it is read, **Then** the required health routes are present and no endpoint carries an `/api` business prefix, so this ticket cannot smuggle in a route of its own.
4. **Given** the route surface DEV-307 shipped, **When** it is read, **Then** it is preserved — including any approved documentation routes — and the proof does not assert an exact health-only set.
5. **Given** the endpoint files, **When** they are read, **Then** each is an `internal` static class in `Endpoints/` with one `MapXxxEndpoints` extension, and no explanatory comment appears in any of them, because project non-negotiables forbid explanatory comments and the convention is recorded in `brief.md` rather than in code.
6. **Given** the `Endpoints/` folder, **When** it is populated, **Then** the disposable `.gitkeep` placeholder is deleted, once and only once real files occupy it.
7. **Given** the convention, **When** it is extended, **Then** no API versioning is introduced in P1; versioning would require an ADR and is not this ticket's to decide.
8. **Given** the host, **When** the shared health endpoints are mapped, **Then** `app.MapDefaultEndpoints()` is called **exactly once** — the call DEV-307 introduced is consumed, not duplicated, and no provisional copy is added.

---

### User Story 5 - Evidence that the change did what it claimed, and nothing more (Priority: P2)

As the reviewer of this change, I need the observable claims proved by tests, the boundaries checked, and the neighbouring ticket's acceptance left visibly unclaimed, so that this can be merged on evidence and so that DEV-376 is not closed by implication.

**Why this priority**: none of these claims changes what the ticket delivers, so they are collected last — after each story's own proof is already in place. It is P2 because a reviewer waiting on them is not blocked on the other four stories, whereas the reverse is true.

**Acceptance Scenarios**:

1. **Given** the changed C# files, **When** the three static-analysis gates and the format check run, **Then** each exits zero, with complexity additionally run at the tighter post-implementation threshold the harness defines; a gate that could not run is reported as `Could not run` and never folded into a passing verdict.
2. **Given** the applicable gates, **When** one is skipped because its scope is empty, **Then** that is reported as a blocking `SKIPPED`; only a gate that is genuinely disabled in `harness.yml` is a non-blocking `SKIP`. A surviving mutant is a missing test, and recording it is never a waiver.
3. **Given** the diff, **When** it is inspected against the frozen file list, **Then** no file outside the list changed, no project file changed, no package pin moved, no configuration section was added, no migration or OpenAPI artefact changed, and no secret, connection string or machine path was committed.
4. **Given** the PR body, **When** it is read, **Then** it carries the exact DEV-376 sentence — "DEV-376: Api-host AddHandler prerequisite supplied (planned); no DEV-376 acceptance closed; live 422 proof not delivered here." — and it does not say the decorators were added by this ticket, because they pre-exist in the `AddHandler` helper.
5. **Given** the test files, **When** they are read, **Then** they contain no explanatory comment, and the accepted `EnrichmentConsumer` broker-retry noise is not asserted by any of them.
6. **Given** a startup or disposal hang, **When** it is observed, **Then** it is reported verbatim, and no timeout is lengthened to conceal it.

### Edge Cases

- **A manifest row whose adapter is not registered.** In the host as composed today, four of the seven rows cannot resolve: `IMovieCatalog` has an existing registration method that no host calls, and `IMediaLibraryScanner` is registered nowhere. With `ValidateOnBuild` on this is a startup failure, not a first-request failure — which is the correct place for it to surface and the reason the two bindings are in this ticket rather than left to the endpoint tickets.
- **The play handler with `Features:LocalPlay` off.** `IMediaPlayerLauncher` is registered unconditionally as a singleton and returns the disabled launcher when the flag is false, so the row resolves with no extra binding and no media process. Enabling `LocalPlay` as a test-only workaround would be a gate change; it is forbidden.
- **A second `AddHandler` row for `ProcessEnrichmentCommandHandler`.** `AddHandler` uses plain `AddScoped`, so a duplicate row would silently replace the graph the RabbitMQ consumer's existing conditional registration built, with a different validator set. A duplicate here is a defect that no test would catch, so the manifest has no such row.
- **The adapter call order.** `AddLamuFlixRabbitMq` decides whether the enrichment consumer is active by asking whether `IMetadataProvider` **and** `IMovieRepository` are already registered. Reordering persistence, metadata provider and RabbitMQ turns the active branch into the inactive one and drops the hosted service. The existing relative order is therefore preserved, and new calls are only inserted.
- **A request from a browser origin that is not the dev server.** The proof is the absence of `Access-Control-Allow-Origin`, not a status code the framework is obliged to return. Asserting a specific denial status would be asserting an implementation detail the CORS middleware does not promise.
- **Preflight and `Location`.** `Expose-Headers` is meaningful on the response that actually carries the header, so it is asserted on the real `GET`. Asserting it on a preflight would be asserting something the browser never reads there.
- **An empty route group.** An `/api` group with no routes cannot be observed through HTTP at all. The route-table read from the endpoint data source is the only executable proof, and DEV-309 and DEV-310 will have to extend that one assertion when they add routes — which is intended friction, not a nuisance.
- **An inherited documentation route.** The route proof asserts the required health routes are present and that no `/api` business route exists. It deliberately does **not** assert that the route set is exactly the health routes, because DEV-307's own scope may include approved documentation routes that must survive.
- **Runtime configuration arriving after the production registrations read it.** The in-memory configuration has to reach the host early enough for the production registrations to see it; a late configuration callback can be too late for values read at composition time. The test supplies it through the inherited recipe and the task verifies the host actually starts rather than assuming the ordering works.
- **The broker being absent.** `EnrichmentConsumer` retries against a broker that is not running and logs a warning every few seconds. That is accepted noise, asserted by nothing, and never a failed startup.
- **Disposal waiting on a connection attempt.** Disposing the host while a broker connection attempt is in flight can block for as long as the client's connect timeout. This is reported when it happens, never hidden by a longer timeout.
- **The near-empty mappers under the inspection gate.** A mapper that returns a group it was handed may draw a "return value unused" or "type is too small" finding. The fix is the code's shape, never a suppression and never a pragma.
- **The property-test gate.** This ticket adds no domain invariant, so that gate is expected to report scope-empty. The opt-out is recorded in the task note under the pipeline's own rule; it is not a silently skipped gate, and an exit 2 that is scope-empty is reported as such.
- **A gate that cannot run.** An analyzer the harness has not wired is a gate that did not pass. It is reported as `Could not run`, which blocks, and never reported as a skip-green.
- **A boot blocker found at implementation time.** The frozen file list is the minimum this ticket expects to touch, not a wall. A defect that stops AC1 startup or the resolution of a manifest row forces its minimum fix inside this ticket, in whichever file that is, cited to the acceptance criterion or the failing gate. Narrowing the validation instead, or deferring the blocker, is forbidden. Everything else follows the follow-up policy.
- **A finding outside the frozen scope.** Anything outside it becomes a follow-up issue filed on the owner's decision. It is not a finding in this round, and it is not fixed here.

## Requirements

### Functional Requirements

**Scope 1 — handler registration through `AddHandler`**

- **FR-001** (Scope 1, AC2, US2): The Api project registers its handlers through the existing `AddHandler<THandler, TReq, TRes>()` helper, which composes the decorators in the order Tracing, then Logging, then Validation, then the handler. This ticket **consumes** that helper; the decorators and that order pre-exist in `Pipeline/ServiceCollectionExtensions.cs:14` and `:50-65` and are not authored, changed or claimed here.
- **FR-002** (Scope 1, `CONCLUSIONS.md` Q2, US2): Registration is **explicit** — one `AddHandler` call per service contract, in one `internal` file in the Api project, called from `Program.cs`. No reflection, no assembly scanning and no convention-based discovery.
- **FR-003** (Scope 1, US2): The manifest is exactly seven rows: `RequestEnrichmentCommandHandler` (`ICommandHandler<RequestEnrichmentCommand, MovieId>`), `ImportMovieFolderCommandHandler` (`ICommandHandler<ImportMovieFolderCommand, MovieId>`), `BrowseMoviesQueryHandler` (`IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>`), `GetMovieDetailsQueryHandler` (`IQueryHandler<GetMovieDetailsQuery, MovieDetails>`), `PlayMovieCommandHandler` (`ICommandHandler<PlayMovieCommand, Unit>`), `AddToWatchlistCommandHandler` (`ICommandHandler<AddToWatchlistCommand, Unit>`) and `RemoveFromWatchlistCommandHandler` (`ICommandHandler<RemoveFromWatchlistCommand, Unit>`).
- **FR-004** (Scope 1, US2): The four enrichment-internal handlers — `ApplyEnrichmentResultCommandHandler`, `ClaimEnrichmentCommandHandler`, `RecordEnrichmentFailureCommandHandler` and `RequeueStrandedMoviesCommandHandler` — are **not** registered. No ticket names them and no production caller exists.
- **FR-005** (Scope 1, US2): `ProcessEnrichmentCommandHandler` keeps its existing conditional Infrastructure registration and its existing validator. It gains no second row, because a duplicate `AddScoped` would silently swap the consumer's graph.
- **FR-006** (Scope 1, `constitution.md:133-136`, US2): The decorator order is Tracing, then Logging, then Validation, then the handler, and no other mechanism dispatches handlers.
- **FR-007** (Scope 1, AC2, US2): A test resolves **every** manifest row from a scope on the started host and asserts the resolved instance is the `TracingDecorator`, and one row additionally walks the full chain to the concrete handler.
- **FR-008** (Scope 1, US2, and `CONCLUSIONS.md` Q6): **No test, requirement, plan line, PR line or report claims input-validation coverage for these rows.** Their validator sequences are empty today, so graph resolution is evidence of composition only. Endpoint-level validation remains mandatory for DEV-309 and DEV-310 under `constitution.md:196-216`, and this ticket does not relax it.

**Scope 2 — the Infrastructure adapters and the clock**

- **FR-009** (Scope 2, US1): The existing `AddMovieCatalog()` is called from `Program.cs` immediately after `AddLamuFlixPersistence()`. This is the existing extension at `Persistence/MovieCatalogServiceCollectionExtensions.cs:8-12`; no new adapter, port, stub or registration method is introduced.
- **FR-010** (Scope 2, `CONCLUSIONS.md` Q6, US1): `IMediaLibraryScanner` is bound to the existing `DirectoryMediaLibraryScanner` (`FileSystem/DirectoryMediaLibraryScanner.cs:14`), scoped. Its two public constructor dependencies — `IFileSystem` and `TimeProvider` — need no new adapter: `IFileSystem` is bound to the existing real `System.IO.Abstractions` `FileSystem` with `TryAdd`, and `TimeProvider` is the inherited shared-defaults singleton, consumed and not re-registered.
- **FR-011** (Scope 2, US1): The two bindings are written in that order — `IFileSystem`, then the scanner, then the manifest rows — in the one `internal` registration file, so the scanner exists before the row that needs it.
- **FR-012** (Scope 2, US1): The scanner's lifetime is **scoped**, which is `ValidateScopes`-safe: it consumes a singleton filesystem adapter and a singleton clock, and it matches the scoped import graph around it (the repository and the catalog are both scoped).
- **FR-013** (Scope 2, US1): `TimeProvider` resolves through the shared defaults' single `TryAddSingleton(TimeProvider.System)`. This ticket adds no second clock registration, and the two existing Infrastructure `TryAddSingleton(TimeProvider.System)` calls stay untouched.
- **FR-014** (Scope 2, US1): The host is built with `ValidateOnBuild` and `ValidateScopes` on, so the whole graph is checked at startup.
- **FR-015** (Scope 2, US1): The host runs with `Features:LocalPlay` **false**. `IMediaPlayerLauncher` is registered unconditionally and returns the existing disabled launcher in that state, so the play row resolves with no extra binding. The `Features:LocalPlay` gate, `Process.Start` and local playback execution are untouched, and enabling the flag as a test workaround is forbidden.
- **FR-016** (Scope 2, US1): The existing relative order of the adapter calls is unchanged — persistence, then metadata provider, then RabbitMQ — so the existing conditional `ProcessEnrichment` branch stays active. New calls are only **inserted**.
- **FR-017** (Scope 2, US1): The Api project already references Infrastructure and this ticket draws **no new project-reference edge**, adds no new project, no new top-level folder and no architectural layer.
- **FR-018** (Scope 2, `CONCLUSIONS.md` Q4, US1): **Boot-blocker rule.** A defect that prevents AC1 startup or the resolution of a manifest row forces its minimum fix inside this ticket, in whichever file that turns out to be, cited to the acceptance criterion or the failing gate. Validation is never narrowed to make it pass, the blocker is never deferred, and green is never claimed early. A non-blocking defect outside that bar follows the follow-up policy.

**Scope 3 — CORS**

- **FR-019** (Scope 3, AC3, US3): One named CORS policy is applied globally, allowing `http://localhost:5173` and no other origin.
- **FR-020** (Scope 3, `CONCLUSIONS.md` Q3, US3): The policy allows any method and any header, permits **no** credentials, and exposes `Location` as the only header beyond the browser's defaults.
- **FR-021** (Scope 3, `CONCLUSIONS.md` Q3, US3): CORS runs after routing and before endpoint execution, and the existing exception handler is preserved ahead of it and unchanged.
- **FR-022** (Scope 3, US3): The policy name and the origin are local constants in `Program.cs`. There is no options class, no configuration section, no separate CORS file and no CORS package.
- **FR-023** (Scope 3, AC3, US3): A test proves the allowed origin end to end — a real `GET` from `http://localhost:5173` carrying allow-origin and `Expose-Headers: Location`; a preflight from the same origin carrying allow-origin, allow-method and allow-headers; and a request from `http://localhost:3000` carrying **no** allow-origin header, asserted as the absence of the header rather than as a status code.
- **FR-024** (Scope 3, US3): `Location` exposure is asserted on the response that carries it, never assumed to appear on a preflight.

**Scope 4 — the shared endpoints and the endpoint-group convention**

- **FR-025** (Scope 4, `CONCLUSIONS.md` Q1, US4): `app.MapDefaultEndpoints()` is called **exactly once**, consuming the call DEV-307 introduced. No provisional copy is added and no health route is mapped by the Api itself.
- **FR-026** (Scope 4, `constitution.md:461-463`, US4): One `internal` static class per feature lives in the Api's `Endpoints/` folder, each exposing one `MapXxxEndpoints(this RouteGroupBuilder api)` extension, and the call is added in the single aggregator. The convention is recorded in `brief.md`; **no explanatory comment documents it in code**, because project non-negotiables forbid explanatory comments.
- **FR-027** (Scope 4, `CONCLUSIONS.md` Q5, US4): The aggregator owns the one root `/api` group, and the two feature mappers this ticket ships — `MapLibraryEndpoints` and `MapImportEndpoints` — map **zero** routes and declare no sub-prefix, no status code and no DTO. Each returns the group it was handed.
- **FR-028** (Scope 4, US4): The disposable `Endpoints/.gitkeep` placeholder is deleted once real files occupy the folder.
- **FR-029** (Scope 4, `constitution.md:463`, US4): No API versioning is introduced in P1, and no placeholder mapper is created for a feature no ticket has yet. Adding versioning would require an ADR, which this ticket does not write.
- **FR-030** (Scope 4, `CONCLUSIONS.md` Q5, US4): The host proof asserts the required health routes are present and that no endpoint carries an `/api` business prefix. It does **not** assert an exact health-only route set, and the inherited DEV-307 route surface, including any approved documentation routes, is recorded at pickup and preserved. No standalone mirror test for the empty mappers is written.
- **FR-031** (Scope 4, US4): `AddProblemDetails`, `AddExceptionHandler<ValidationExceptionHandler>` and `UseExceptionHandler` are preserved exactly as they are. No second exception mapper, no new error shape and no production test route is added.

**Cross-cutting — evidence, gates and boundaries**

- **FR-032** (AC1, AC2, AC3, US1-US4): All three acceptance criteria are proved through the **inherited** `WebApplicationFactory<Program>` harness that DEV-307 introduces, running the real `Program.cs` composition, with only the DEV-307 health-check stubs in place. No substitute adapter, no substitute handler and no hand-rolled `ServiceCollection` that bypasses `Program.cs` is used.
- **FR-033** (`constitution.md:288`, US1-US4): The host tests live in `tests/LamuFlix.IntegrationTests`, and DEV-308 makes **no** change to that project file, to `Directory.Packages.props`, to any `appsettings` file or to the shared defaults. The project reference to the Api and the test-hosting package are DEV-307's to add, and this ticket consumes them.
- **FR-034** (`constitution.md:299-301`, US1-US4): **No container.** The proof covers dependency injection and the liveness route, not query translation, driver serialisation, broker delivery or readiness. No data-access driver is mocked, and no database or broker round trip is claimed anywhere in this ticket.
- **FR-035** (US1-US4): The runtime configuration the host needs is supplied by the test, early enough for the production registrations to read it, and every value is transient and test-owned: **no real credential, connection string, API key or machine path is committed.** A syntactically valid placeholder connection string is used, and no connection is ever opened.
- **FR-036** (US1-US4): The file set is exactly five production C# files — one edited (`Program.cs`) and four new (`HandlerRegistration.cs`, `Endpoints/ApiEndpoints.cs`, `Endpoints/LibraryEndpoints.cs`, `Endpoints/ImportEndpoints.cs`) — plus the `.gitkeep` deletion and two new test files. The single conditional entry is the previously approved move-only extraction of the DEV-307 factory, decided at pickup and named in the receipt before any edit.
- **FR-037** (US1-US4): `LamuFlix.Web` is not deleted, moved or touched. That belongs to DEV-388, after SPA parity. `web/`, the committed OpenAPI document and the generated TypeScript client are likewise untouched: this ticket adds no DTO, so nothing enters the contract chain.
- **FR-038** (US1-US4): **No explanatory comment** appears in any production or test file this ticket adds or edits, beyond the three exemptions project non-negotiables permit. The three `Endpoints/` classes and the registration file carry no XML documentation, and the tests carry no AAA headers beyond the permitted form and no explanatory text.
- **FR-039** (US5, `constitution.md:346-376`): Every applicable gate defined by `harness.yml` and by each script's own help is run on the changed file set — the three static-analysis gates, the format check, the full test run, the property-test gate, the vulnerable-packages gate and the mutation gate at its proper stage. Scope, thresholds and flags come from the harness and the scripts; none is invented, hardcoded or lowered.
- **FR-040** (US5): A required applicable gate must pass. An exit code of 1 or a `Could not run` result **blocks**. An applicable gate reporting exit 2 because its scope is empty **also blocks**; only a gate that is genuinely disabled in `harness.yml` is a non-blocking `SKIP`. A surviving mutant is a missing test and is routed to a test, never waived. No rigid only-additions test-count rule is applied; actual failures and coverage are compared against the pickup baseline.
- **FR-041** (US5): A finding the inspection gate raises on the near-empty mappers is fixed by changing the code's shape, never by a suppression, a pragma or an exclusion.
- **FR-042** (US5, `CONCLUSIONS.md` Q7): The PR body and the final report carry the exact sentence "DEV-376: Api-host AddHandler prerequisite supplied (planned); no DEV-376 acceptance closed; live 422 proof not delivered here." **No DEV-376 acceptance criterion is claimed as closed**, no migration ownership moves, and DEV-376's live 422 acceptance stays with the first migrated validating endpoint in DEV-309 or DEV-310, which carries its own host error-path proof.
- **FR-043** (US5, `brief.md:152-154`): The phrase "decorators added by DEV-308" is never written. The decorators and their order pre-exist in the `AddHandler` helper, and this ticket's contribution is registration.
- **FR-044** (US5): The accepted `EnrichmentConsumer` broker-retry noise is asserted by no test. A startup or disposal hang is reported verbatim, and no timeout is lengthened to conceal it.
- **FR-045** (US5): No database schema change, no migration, no model snapshot change, no new port, no architectural layer, no new domain term, no ADR, no secret, no machine path and no gate or harness threshold change. `CONCLUSIONS.md` and `ASSUMPTIONS.md` are Patron-owned and append-only, and this ticket does not edit them.
- **FR-046** (US5): The review size is **M** — three axis reports, no adjudication without all three — as recommended and ruled while the live `size:` tag is unread. If the tag reads L, the L process applies. No verified tag is claimed by this document.
- **FR-047** (US5, `CONCLUSIONS.md` Q8): The standing review terms are carried once and not reopened: a Critical or High finding with a concrete failure scenario blocks; at most two review rounds, with at most two fix commits per round; out-of-scope items become follow-up issues rather than findings in this round; and this ticket never merges itself — the report is `awaiting-merge: DEV-308 (#n)`.

### Key Entities

- **Api host composition**: the single `Program.cs` composition root of the Minimal API host. It calls the shared defaults, the Infrastructure adapters, the one registration extension, the CORS policy, the inherited default endpoints and the Api endpoint aggregator, in an order whose relative sequence is load-bearing rather than incidental.
- **Handler manifest**: the seven Api-facing service contracts, one explicit `AddHandler` call each, that DEV-309 and DEV-310 dispatch. It is exactly the intersection of "a handler exists" and "a ticket will call it".
- **Decorated handler graph**: what resolving a manifest contract yields — the tracing decorator outermost, then logging, then validation, then the concrete handler — and the evidence that the host's graph matches the one the `AddHandler` helper composes.
- **Adapter binding**: the wiring of an existing implementation to an existing port. Two of them are missing today and both are what stand between the manifest and a booting host; neither is a new type, and neither is a new registration method.
- **LocalPlay-off launcher graph**: the state the host runs in for its tests — the media player launcher resolving to the disabled implementation, so the play handler resolves while nothing is executed.
- **CORS policy**: one named, globally applied, exact-origin policy whose whole surface is a fixed development origin, any method, any header, no credentials and one exposed header.
- **Endpoint group convention**: the root `/api` group, the aggregator that owns it, and the per-feature mappers that extend it. In this ticket the mappers are empty on purpose, because their route shapes belong to the endpoint tickets.
- **Inherited route surface**: whatever DEV-307's `MapDefaultEndpoints` and any approved documentation routes put in the Api's endpoint data source. It is recorded at pickup and preserved, not asserted to be exactly the health routes.
- **Host test configuration**: the transient, test-owned key set the host needs to boot — the library root path, the OMDb key and base URL, the broker host name, a placeholder connection string, and a claim lease strictly below the retry delay. It contains no real credential and no machine path.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The Api starts through `WebApplicationFactory<Program>` with `Features:LocalPlay` off and the host configuration supplied, and 0 MVC or Razor registrations, `PackageReference` entries or mapping calls exist in the Api project.
- **SC-002**: The provider is built with `ValidateOnBuild` and `ValidateScopes` on, and all 7 of the 7 manifest contracts resolve from a created scope. A dependency gap is a startup failure, not a first-request failure.
- **SC-003**: 7 of 7 resolved manifest instances are `TracingDecorator`s, and the one full walk reads Tracing, then Logging, then Validation, then the concrete handler — in that order.
- **SC-004**: `IMovieCatalog` and `IMediaLibraryScanner` each resolve, `IFileSystem` resolves to the real `System.IO.Abstractions` `FileSystem`, and `TimeProvider` resolves through the one inherited registration. 0 second clock registrations, 0 new ports, 0 new adapters and 0 new registration methods.
- **SC-005**: The enrichment consumer's conditional registration is still the active branch, proved by the registration graph rather than by a comment: 0 duplicate rows for `ProcessEnrichmentCommandHandler`, 0 rows for the 4 enrichment-internal handlers, and 0 rows, ports or placeholders for the facet endpoints.
- **SC-006**: With `Features:LocalPlay` false, the play handler's graph contains the disabled launcher, and 0 media processes are started and 0 gate or flag values are changed.
- **SC-007**: A real `GET /health/live` from `http://localhost:5173` returns allow-origin for that origin and `Location` in `Access-Control-Expose-Headers`; a preflight from the same origin returns allow-origin, allow-method and allow-headers; and a request from `http://localhost:3000` returns **no** allow-origin header. The absence is asserted, not merely unchecked.
- **SC-008**: The pipeline order is exception handling, then CORS, then endpoint execution. The existing `AddProblemDetails`, `AddExceptionHandler` and `UseExceptionHandler` lines are unchanged, and there is 1 error mapper in the project.
- **SC-009**: `app.MapDefaultEndpoints()` appears exactly once in the Api, exactly 1 root `/api` group is mapped, and 0 endpoints in the data source carry the `/api` business prefix. The required health routes are present and the inherited DEV-307 route surface is preserved.
- **SC-010**: The 2 feature mappers map 0 routes and declare 0 sub-prefixes, 0 status codes and 0 DTOs. `Endpoints/.gitkeep` is gone and 0 explanatory comments appear in any file this ticket touches.
- **SC-011**: The analyzer, complexity (at both the harness's implementation and refactor ceilings), inspection and format gates exit zero on every changed C# file. 0 gate results are reported as passing when they did not run, and 0 applicable gates are reported green on the strength of failures that were merely recorded.
- **SC-012**: The full test run, the property-test gate, the vulnerable-packages gate and the stage-appropriate mutation gate are all run and reported. 0 surviving mutants are waived. Where the property-test gate reports scope-empty, the documented no-domain-invariant opt-out is recorded rather than the gate being described as passing.
- **SC-013**: The diff touches exactly the frozen file set — 5 production C# files, 1 `.gitkeep` deletion, 2 new test files, and the single conditional move-only factory extraction when pickup records it. 0 project-file changes, 0 package-pin changes, 0 configuration sections, 0 migrations, 0 OpenAPI or TypeScript changes, 0 threshold changes, 0 secrets and 0 machine paths.
- **SC-014**: 0 statements anywhere in the spec, plan, tasks, PR body or report claim a DEV-376 acceptance criterion, a live 422 proof, or input-validation coverage for the 7 manifest rows; and 0 statements say the decorators were added by this ticket. The DEV-376 sentence from FR-042 is present in the PR body verbatim.

## Assumptions

- **Carried `[assumed]` ruling**: the single named CORS policy is called `DevSpa`. Basis in `ASSUMPTIONS.md`: the ticket names the origin, not an internal policy identifier, and `specs/PRODUCT.md` section 4 permits recorded naming taste. The behaviour — exact origin, any method, any header, no credentials, `Location` exposed — is a deliberate ruling in `CONCLUSIONS.md` Q3 and is **not** an assumption.
- **The DEV-307 prerequisite is a merge, not a question.** Phase B starts only when DEV-307's code is on the build base, and the pickup receipt records that `AddServiceDefaults` registers `TimeProvider`, that `MapDefaultEndpoints` exists, and that the inherited route surface is known. Nothing in this document asks the owner to decide any of it.
- **The one conditional file entry is decided at pickup, not now.** If DEV-307 left the factory reusable, the two test files use it as-is. If it left it private or inline, one same-project **move-only** extraction is performed, named in the receipt and in the touched-file list before any edit. There is no second factory and no behaviour change.
- **The handler manifest is frozen, not discovered at implementation.** Seven rows, four exclusions, one inherited conditional row left alone. If a boot blocker reveals a further gap in an adapter that is not one of the two named here, FR-018 governs: minimum fix, cited, in this ticket. It does not open a new manifest question.
- **`Features:LocalPlay` stays off in the host proof**, and this is a test-configuration choice rather than a gate change. The `LocalPlay` flag, its `403` behaviour, `Process.Start` and local playback execution are all outside this ticket, and enabling the flag to make a test pass is forbidden.
- **No UI taste decisions apply to the parts DEV-309 and DEV-310 own.** This ticket writes no route, no status code, no DTO, no endpoint label and no empty-state copy, so there is nothing here for `ASSUMPTIONS.md` to record beyond the CORS policy name.
- **Out of scope**, restated from the ticket and the brief: the business endpoints themselves (DEV-309, DEV-310); the facet endpoints `GET /api/genres` and `GET /api/people` (DEV-310, and no handler or port exists for either today); the Worker-side half of the DEV-376 composition clause; removal of `LamuFlix.Web` (DEV-388); the committed OpenAPI document and the generated TypeScript client, since no DTO is added here; the live 422 proof and any DEV-376 acceptance criterion; and container-backed endpoint behaviour.
- **The review size is M because the risk sits in the DI graph, not because the diff is large.** The recommendation and the ruling agree while the live `size:` tag is unread; if the tag reads L, the L process applies. No verified tag is claimed.
- **A property test is not expected to exist here.** This ticket introduces no domain invariant — no value object, no state transition, no query model — so the property gate's scope-empty result and its documented opt-out are the expected outcome, recorded rather than quietly skipped.
- **The framework's own CORS behaviour is a fact, not a ruling.** What the middleware does with a disallowed origin is not what this ticket promises; what it promises is that no allow-origin header is returned for one. The middleware's other defaults are unchanged because nothing configures them.
