# DEV-308 grill conclusions

## Authority and evidence

Patron applies the current assigned role and specs/PRODUCT.md section 5. Care items 1-6 require cited Patron rulings, not owner checkboxes. Only a ticket deliverable change or a constitution departure Patron judges necessary escalates as blocked: structural. The older canvas charter/task-pipeline escalation wording is superseded by the current direct instruction and PRODUCT.md.

Named worktree: F:/Dev/LamuFlix.worktrees/feature-308-spec, branch feature/308-spec, intake HEAD 6e0f09b1e476fc10ec3e5829d7b8c73ba4c80d03. Preserve Rigger's unrelated tracked .specify/feature.json edit.

Evidence: live scripts/get-task.ps1 -TaskId DEV-308, DEV-376, DEV-309 and DEV-310 read on 2026-10-02; task note DEV-308:6-18; recon-DEV-308:7-49 (trusted Wisp source citations); chain:5-8; specs/PRODUCT.md; .specify/memory/constitution.md:128-144,196-236,282-295,348-370,461-472. No artifact receipt exists under artifacts/DEV-308; the live task read supplies ticket text. No baseline gates were run; none are claimed green.

## Decided ticket envelope

Live DEV-308 ticket Scope & Technical Design:

1. Api references Infrastructure and registers the handler pipeline through AddHandler, so every handler resolves with its validation, logging and tracing decorators (from DEV-376).
2. Register the Infrastructure adapters, and TimeProvider through host DI (see DEV-307).
3. CORS policy for the Vite dev server (http://localhost:5173).
4. Call app.MapDefaultEndpoints() (DEV-307) and set up the endpoint-group mapping convention (MapLibraryEndpoints, MapImportEndpoints, ...) that DEV-309 and DEV-310 extend.

Acceptance Criteria (verbatim):

- Api starts cleanly as a Minimal API host without MVC or Razor dependencies.
- A test resolves a registered handler from the Api host's service provider and gets the decorated pipeline.
- CORS allows the React dev server origin.

The ticket Overview assigns endpoints to DEV-309/DEV-310 and removal of LamuFlix.Web to DEV-388 after SPA parity. No scope item may be silently omitted, duplicated into a new deliverable, or deferred away. No implementation is authorized by this grill.

---

## Q1 - DEV-307 implementation prerequisite and shared defaults ownership

Question (Keel): does DEV-308 Phase B require DEV-307 implemented on its build base, consuming ServiceDefaults TimeProvider and MapDefaultEndpoints rather than adding provisional copies?

Recommendation (Keel): yes. Phase A proceeds now. Phase B waits for DEV-307 code merged to main; pickup verifies AddServiceDefaults registers TryAddSingleton(TimeProvider.System) and MapDefaultEndpoints exists. Consume the shared registration and mapping; keep any MapDefaultEndpoints call DEV-307 T013 already added rather than duplicating it. Retain the endpoint-group convention half of scope 4. Leave the existing Infrastructure TryAddSingleton calls untouched. Cost: DEV-308 build waits for DEV-307 implementation; no chain reorder.

Patron verdict: ACCEPT; Phase A proceeds, Phase B requires DEV-307 implementation on its merged build base, and DEV-308 consumes the shared defaults once.

- Basis: DEV-308 Scope 2 and 4 expressly reference DEV-307; .specify/memory/constitution.md:235-236 assigns TimeProvider and health wiring to ServiceDefaults and forbids host duplication. Trusted recon-DEV-308:20,28 cites specs/DEV-307/plan.md:172,225-231 and T013 at :87; chain:5-6 already records the dependency.
- Preserve scope 4 in full: retain a single app.MapDefaultEndpoints() call and establish the MapXxxEndpoints convention. Host integration assertions prove the consumed services and mapping, rather than treating a merged spec as implemented code. No owner checkbox or schedule change is required.
- Existing Infrastructure fallback registrations at src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs:31 and Adapters/MetadataProviderServiceCollectionExtensions.cs:30 remain outside this edit. No cleanup is forced by the ticket; noted, no ticket. This ruling neither closes DEV-307 owner checkboxes nor authorizes implementation.

---

## Q2 - Api handler ownership and explicit registrations

Question (Keel): which handlers does the Api host register?

Recommendation (Keel): register every handler called by the DEV-309/DEV-310 endpoints through one explicit AddHandler line per handler in one Api registration file, called by Program.cs. Worker-only handlers such as ProcessEnrichmentCommandHandler remain with the Worker. A host integration test resolves each Api handler and checks its tracing outer decorator. The recon supplies counts rather than exact names; Keel requested the full handler names, current callers, ticket ownership and validators from Bernstein before freezing the manifest.

Patron verdict: ACCEPT the Api-facing ownership principle; freeze the exact manifest only from the requested recon, before the brief closes.

- Basis: DEV-308 Overview composes the Api for DEV-309/DEV-310; Scope 1 requires every registered handler to resolve through AddHandler. DEV-309 Scope 1-2 names BrowseMoviesQuery and GetMovieDetailsQuery; DEV-310 Scope 1 names the remaining dispatching endpoint families. Constitution:133-136 requires explicit registration and Tracing -> Logging -> Validation -> handler; reflection scanning is forbidden. Worker-only dispatch is outside this Api ticket (recon-DEV-308:14-16).
- Approve one internal registration helper in src/LamuFlix.Api/HandlerRegistration.cs called from Program.cs. This is composition in the existing host, not a new project or architectural layer (PRODUCT.md section 2 and section 5 care item 2). Preserve the RabbitMQ-owned ProcessEnrichmentCommandHandler registration; do not duplicate it in the Api inventory merely because the adapter is registered there.
- Host integration coverage resolves every frozen Api service contract through the real AddHandler registrations, using the existing helper's decorator-order tests as supporting evidence (recon-DEV-308:14). Missing concrete names/callers/validator or constructor dependencies remain needs recon, not a silent assumption or owner checkbox. No inventory is declared complete by this principle ruling.

---

## Q3 - CORS policy and browser-visible headers

Question (Keel): what CORS policy and host proof should DEV-308 establish for the Vite dev server?

Recommendation (Keel): one globally applied named policy for the fixed http://localhost:5173 origin, allowing request methods/headers without credentials and exposing Location for DEV-310's Accepted response. Test the real DEV-307 /health/live route with the allowed origin, allowed POST preflight, and a rejected http://localhost:3000 origin. Do not add fake production routes or a configurable origin options class. Cost: future ports/production origins require separate scoped work; the test depends on the DEV-307/WAF prerequisites. Development-only and per-group policies risk environment-dependent acceptance or forgotten coverage.

Patron verdict: ACCEPT the exact origin globally, AllowAnyMethod and AllowAnyHeader, no credentials, and expose only Location beyond default browser-visible headers; [assumed] name the policy DevSpa.

- Basis: DEV-308 Scope 3/AC3 expressly decides http://localhost:5173; DEV-310 Scope 1 expressly requires 202 plus Location for import. No additional origin, wildcard, credentials, authentication mechanism, options class, or route is introduced. A localhost browser origin is public configuration, not a secret or machine-specific media path (constitution VII). The existing Web shared framework supplies CORS (recon-DEV-308:24); no dependency ruling is needed.
- Microsoft Learn CORS documentation (queried through microsoft-learn on 2026-10-02): https://learn.microsoft.com/aspnet/core/security/cors?view=aspnetcore-10.0#set-the-allowed-origins and #preflight-requests. WithOrigins constrains the origin independently of allowed methods/headers; UseCors handles preflight; WithExposedHeaders makes Location readable. Rejected-origin proof asserts absence of Access-Control-Allow-Origin, not a required denial status. Allowed preflight proves allowed origin/method/request headers; Location exposure is asserted on the actual GET response or resolved policy, not assumed to appear on preflight.
- Apply UseCors(DevSpa) after routing and before endpoint execution, with UseExceptionHandler preserved. Keep AC proof on the inherited health route. Policy name is taste (PRODUCT.md section 4); HTTP behavior is this deliberate ruling under section 5 care item 4. No owner checkbox is required.

---

# DEV-308 grill Q4/12 - Host test harness (Keel)

## Question
Where and how do the AC2 (decorated handler resolution) and AC3 (CORS) host tests boot the Api, and with what config and infrastructure?

## Recommendation
1. **Harness = the DEV-307 `WebApplicationFactory<Program>` in `tests/LamuFlix.IntegrationTests`, reused, not re-decided.** DEV-307 brief D1 / plan section 8 N2 first half makes it the default and adds the only two prerequisites to `LamuFlix.IntegrationTests.csproj`: a ProjectReference to LamuFlix.Api and Microsoft.AspNetCore.Mvc.Testing pinned 10.0.0 (DEV-307 brief.md:58,62; tasks T020A, T016). DEV-308 adds **no csproj edit, no package pin, no `partial class Program`** (.NET 10 source-generates it; recon-DEV-308:36). Under the Q1 prerequisite this is already on main at DEV-308 pickup; the Phase B drift check confirms the csproj carries both references and records which factory type T016 produced.
2. **Factory reuse rule.** If DEV-307 leaves a reusable factory type (deriving from `WebApplicationFactory<Program>` with in-memory config + stub checks), DEV-308 tests use it as-is. If DEV-307 left it private/inline in HealthEndpointTests, DEV-308 may extract it to a shared file in the same test project. That is an edit to a file DEV-308 does not otherwise name, so it is a care item 6 ruling: I ask you to pre-authorise that single extraction (move only, no behaviour change) now, or reject it and accept a second DEV-308-local factory with the identical config.
3. **Startup-validated config, supplied by the test, never by production defaults.** The in-memory set DEV-307 plan section 7 items 1-2 fixes (plan.md:270-273): `Library:RootPath`, `Omdb:ApiKey`, `Omdb:BaseUrl` (valid URL), `RabbitMq:HostName`, `ConnectionStrings:DefaultConnection` (placeholder), `Enrichment:ClaimLease` > 0 and < `RabbitMq:RetryDelay` (e.g. 00:00:10). The host is **started** (not just built) so `ValidateOnStart` runs. AC1 "starts cleanly" is then evidenced by the same factory start succeeding, plus the existing fact that Api has no MVC/Razor reference (recon-DEV-308:11).
4. **Real infrastructure registrations, no containers.** The tests run the real `Program.cs` composition - real AddLamuFlixPersistence / AddMetadataProvider / AddLamuFlixRabbitMq / AddLamuFlixPlayback / the HandlerRegistration extension - with no substitute for any adapter or handler. Only the DEV-307 health-check stubs stay in place (ConfigureTestServices). No Postgres or RabbitMQ Testcontainers: AC2 resolves handler graphs inside a created scope (constructing a DbContext opens no connection) and AC3 hits `/health/live` (no checks). DB/broker round-trips belong to DEV-309/310 endpoint tests on the DEV-306 shared container fixtures.
5. **AC2 test shape.** Start host -> create a scope -> for every manifest entry (frozen from the pending recon, per the Q2 ruling) resolve the handler service contract and assert the instance is the TracingDecorator (outermost). One entry additionally walks inner -> LoggingDecorator -> ValidationDecorator -> concrete handler, proving the host graph matches Compose (ServiceCollectionExtensions.cs:50-65); full ordering stays owned by AddHandlerTests.cs. Data-driven over the manifest (xUnit theory) so DEV-309/310 extend coverage by adding a row. The factory also turns on `ValidateScopes` + `ValidateOnBuild` (`UseDefaultServiceProvider`) so a missing adapter dependency of any manifest handler fails at boot, not at first request.
6. **AC3 test shape** exactly as the Q3 ruling: GET from 5173 -> allow-origin + Expose-Headers Location; preflight -> origin/method/headers only; 3000 -> absence of allow-origin.
7. **Known noise accepted, not asserted.** EnrichmentConsumer retries every 5 s against the absent broker (DEV-307 plan.md:274 item 3); RabbitMqConnectionOwner disposal can wait the connect timeout (plan.md:276). A disposal hang is reported; a timeout is never lengthened to hide it.

## Basis
constitution.md:288 and :359-360 (WAF for API end-to-end); DEV-307 brief D1, brief.md:58,62, plan.md:27,268-276, tasks T016/T020A; recon-DEV-308:32-37; ticket AC1-AC3; Q1 ruling (DEV-307 code is the build base) and Q2 ruling (manifest; host test resolves every entry).

## Rationale
One harness for the Api host, owned by the ticket that introduced it, avoids a second pin decision and a second config recipe that drifts. Real composition + stubbed health checks proves exactly what AC2 claims (the host DI graph) without making a DI test depend on Docker. ValidateOnBuild turns "every handler resolves" from a per-row assertion into a whole-graph guarantee at near-zero cost.

## Cost
- DEV-308 tests cannot run until DEV-307 T016/T020A land (already the Q1 prerequisite).
- If the DEV-307 factory is not reusable: one extraction edit in a DEV-307 test file (needs your care item 6 ruling here) or a duplicated ~20-line factory.
- ValidateOnBuild may surface a latent DI defect in existing Infrastructure registrations (e.g. a scoped service captured by a singleton). That is a real finding: fixed only if inside DEV-308 files, otherwise a follow-up ticket, and the test records the narrowed validation explicitly - never hidden.
- No live DB/broker evidence in this ticket, by design; endpoint tickets own it.

## Alternatives rejected
- Testcontainers Postgres/RabbitMQ for AC2: slow, Docker-dependent, proves nothing extra about DI wiring.
- Hand-built ServiceCollection test (no host): does not execute Program.cs, so AC2 "from the Api host service provider" is unproven.
- Tests in tests/LamuFlix.Test (already references Api, recon:34): splits API host tests across two projects against constitution.md:288 and the DEV-307 precedent.

## Asks of Patron in this ruling
(a) accept the harness / config / no-container shape; (b) rule care item 6 on the conditional factory extraction; (c) accept ValidateOnBuild + ValidateScopes in the factory with the latent-defect handling above.


Patron verdict: ACCEPT the inherited WAF harness, same-project move-only extraction if needed, and factory scope/build validation; CHANGE the proposed handling of boot-blocking DI defects.

- Basis: DEV-308 AC1 requires host startup and AC2 requires the host service provider. Constitution:288 places API integration coverage in LamuFlix.IntegrationTests; recon-DEV-308:34-36 and DEV-307 brief:58,62/tasks T016,T020A identify inherited references and factory prerequisites. Reuse these after Q1; no new package pin, project, explicit Program declaration, or csproj edit is authorized by this ruling. Conditional extraction from the actual DEV-307 health test into a same-project shared factory is approved under PRODUCT.md section 5 care item 6, move-only with unchanged behavior, and must be named in the pickup receipt and touched-file list before edits.
- No containers are needed for service-graph resolution or /health/live: neither tests query translation, serialization against the driver, broker delivery, or dependency readiness. Keep real production handler/adapter registrations and inherited health stubs; startup validation stays active. Reuse transient test-owned configuration, with no real credential, connection string, or machine path committed. Microsoft Learn integration-test docs (queried 2026-10-02): https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0#customize-the-%60webapplicationfactory%60-with-test-configurations distinguish early host configuration from late ConfigureAppConfiguration; ensure required composition-time values are available before production registration reads them, using .NET 10 APIs.
- ValidateOnBuild/ValidateScopes may expose defects outside the initial file list. If one prevents required startup or manifest resolution, AC1/Scope 1 forces the minimum fix: bring its file into this ticket with the cited failure, rather than narrowing validation or marking a failing gate green. Unrelated non-blocking defects follow the existing follow-up policy. No test should pretend container-free host startup proves live DB/broker behavior. Report any startup/disposal hang without extending timeouts to conceal it. No owner checkbox is required.

---

# DEV-308 grill Q5/12 - Endpoint-group convention without routes (Keel)

## Question
What exactly does the "endpoint-group mapping convention" half of scope 4 deliver when DEV-308 owns no routes? Which files, which method signatures, which prefixes, and what proves it?

## Recommendation
1. **One composition entry point.** New `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`: `internal static class ApiEndpoints` with `MapApiEndpoints(this IEndpointRouteBuilder app)`. It creates the single root group `app.MapGroup("/api")` and passes that group to each feature mapper in turn. `Program.cs` calls `app.MapDefaultEndpoints()` (inherited from DEV-307, Q1) and then `app.MapApiEndpoints()`, after `UseCors(DevSpa)` per the Q3 ruling. Health stays outside `/api` because DEV-307 owns `/health/*`.
2. **Exactly the two feature mappers the ticket names, and no others.** `Endpoints/LibraryEndpoints.cs` holds `internal static class LibraryEndpoints` with `MapLibraryEndpoints(this RouteGroupBuilder api)`. `Endpoints/ImportEndpoints.cs` holds `internal static class ImportEndpoints` with `MapImportEndpoints(this RouteGroupBuilder api)`. Each returns `api` and maps **zero routes and no sub-prefix**. The sub-prefix (`/movies`, the import path) and every route, status code and DTO belong to the owning ticket: DEV-309 names `Endpoints/LibraryEndpoints.cs` itself (recon-DEV-308:30), and DEV-310 owns the import surface. The "..." features (enrichment, watchlist, playback, genres, people) are added by DEV-310 as new `MapXxxEndpoints` files following the same shape. DEV-308 does not pre-create them, which is speculative scope.
3. **The convention is written down once,** as a short XML doc comment on `ApiEndpoints` and nowhere else: one static class per feature in `Endpoints/`, a `MapXxxEndpoints(this RouteGroupBuilder api)` extension, a call added to `MapApiEndpoints`, no versioning in P1. This restates constitution.md:463, which already decides the convention (recon-DEV-308:29), so DEV-308 implements a rule rather than inventing one.
4. **Visibility is `internal`.** Api is an executable and nothing outside it calls these methods. The tests reach them through the host, not by calling them, so no `InternalsVisibleTo` is needed.
5. **The `/api` prefix is fixed here.** Basis: the DEV-309 ticket title, "GET /api/movies and GET /api/movies/{id}". It is the only route-shape fact DEV-308 asserts, and it is ticket text, so care item 4 is not triggered. No OpenAPI registration: `AddOpenApi` is not in this ticket. The DEV-307 N1 OpenAPI half stays carried as it is, and `web/src/api/openapi.json` stays untouched. Nothing feeds the contract chain until DEV-309 adds a DTO.
6. **`Endpoints/.gitkeep` is deleted** once real files occupy the folder. It is a placeholder in a folder the ticket names (recon-DEV-308:9), and removing it is the minimum tidy-up. I ask you to rule it under care item 6, or to keep it if you prefer zero unnamed-file deletions.
7. **Proof, in the Q4 host test class.** After the host starts, read `EndpointDataSource` from `factory.Services`. Assert that the route set equals exactly the DEV-307 health routes, so no endpoint carries an `/api` prefix and nothing else is mapped. A separate pure test is not worth its cost: an empty group cannot be observed through HTTP, and the route-table assertion is what stops DEV-308 from smuggling in a route. DEV-309 and DEV-310 will update this one assertion when they add routes, which is intended friction. The test's comment says so.

## Basis
- Ticket scope 4: the convention is named and extended by DEV-309/310. Ticket overview: DEV-309 and DEV-310 own the endpoints.
- constitution.md:463: per-feature `MapXxxEndpoints`, no versioning.
- recon-DEV-308:9 (empty `Endpoints/` with `.gitkeep`), :29-30 (convention decided; DEV-309 names `LibraryEndpoints.cs`), :43 (this ticket's route table is empty).
- Q1 (`MapDefaultEndpoints` consumed once), Q3 (`DevSpa` applied before endpoint execution), Q4 (host harness).

## Rationale
- **Convention, not routes.** Shipping the root group, the aggregator and the two ticket-named mappers gives DEV-309 and DEV-310 a fixed place to put routes with no edit to `Program.cs`. Pre-creating the "..." features or sub-prefixes would freeze route shape that the endpoint tickets own (care item 4).
- **Executable proof.** The exact-route-table assertion turns "no routes of its own" into a checked fact.

## Cost
- Two near-empty mapper methods exist until DEV-309 and DEV-310 fill them. ReSharper may flag the returned value as unused, or the class as trivially small. If InspectCode raises a WARNING+ finding, it is fixed by shape (for example `MapApiEndpoints` uses the return), never by suppression. I expect the Roslyn gates to stay clean, because the parameter is used.
- `/api` is committed one ticket early, from the DEV-309 title.
- The route-table assertion needs a one-line update in each endpoint ticket.

## Alternatives rejected
- Mapping per-feature sub-prefixes (`/api/movies`, ...) now: decides DEV-309/310 route shape.
- Mapping only the `/api` group with no named mappers: under-delivers the ticket parenthetical (`MapLibraryEndpoints`, `MapImportEndpoints`).
- Public extension methods: no external caller, and they widen the public surface for nothing.
- `MapGroup` per feature directly in `Program.cs`: spreads composition, and DEV-309/310 would then edit `Program.cs`.

## Asks of Patron in this ruling
- (a) Accept the file set, the signatures, the `/api` root and the zero-route mappers.
- (b) Rule the deletion of `Endpoints/.gitkeep` under care item 6.
- (c) Accept the exact-route-table proof and its intended friction for DEV-309/310.

## Note on pending recon
The Conductor filed recon-DEV-308-handlers while Q5 was being written: 12 concrete handlers, not 14 (the other 2 files are the pipeline interfaces); G0-G3 findings. Q5 does not depend on it. I will use it in Q6 to freeze the exact Api handler manifest, including G2 (IMediaLibraryScanner has no DI registration). Under the Q4 ruling, G2 is a boot blocker for any manifest entry that needs that scanner. Note: the stored note body appears cut off at line 44 (mid-sentence in the DEV-309 ownership section, and the G0-G3 detail is missing), so Q6 may first need the rest of it re-filed.


Patron verdict: ACCEPT internal /api aggregation and the two ticket-named zero-route feature mappers; CHANGE explanatory comments and the health-only exact-route assumption; delete Endpoints/.gitkeep when populated.

- Basis: DEV-308 Scope 4 names MapLibraryEndpoints/MapImportEndpoints and assigns business endpoints to DEV-309/310. Constitution:463 fixes per-feature mapping and no P1 versioning; DEV-309's named /api/movies routes justify the /api root. Approve ApiEndpoints.cs, LibraryEndpoints.cs and ImportEndpoints.cs in the existing Endpoints directory, with internal extensions using the shared RouteGroupBuilder and no sub-prefix/route/status/DTO declaration. No new layer, public assembly contract, glossary term or ADR is introduced.
- Project non-negotiables forbid explanatory comments, including the proposed XML convention comment and test explanation. Record the convention in brief.md; method/class names express it in code. Endpoints/.gitkeep is a disposable placeholder and can be deleted once real files occupy the ticket-named folder. Preserve inherited OpenAPI wiring/contract work without redesigning DEV-307's accepted scope or its existing owner decisions.
- Host proof checks both required /health routes and absence of business /api endpoints; do not assume all inherited DEV-307 routes are health routes, since prerequisite pickup may include approved docs routes. Record the inherited route surface at pickup and preserve it. This proof belongs in the existing host coverage, not a new standalone mirror test for empty methods. Follow-up endpoint tickets extend the route surface; no owner checkbox is required.

---

# DEV-308 grill Q7/12 - DEV-376 overlap and error-mapper ownership (Keel)

## Question
Ticket scope 1 says the decorators come "from DEV-376". DEV-376 also asks that each dispatching host reference Infrastructure, call AddHandler, and use the single IExceptionHandler/ProblemDetails contract for 422. What does DEV-308 own, what does it leave to DEV-376, and what must DEV-308 never claim?

## Recommendation
1. **The error mapper is inherited unchanged and owned by nobody in DEV-308.** `AddProblemDetails()` (Program.cs:16), `AddExceptionHandler<ValidationExceptionHandler>()` (:17) and `UseExceptionHandler()` (:19) stay exactly as they are. `ExceptionHandling/ValidationExceptionHandler.cs` is not edited: it already maps ValidationException -> 422 with errors (:24-27, :46-56) and NotFoundException -> 404 (:28-31) (recon-DEV-308:48). DEV-308 adds no second handler, no status-code mapping, no ProblemDetails customisation, and no `Results.Problem` helper.
2. **Middleware order is preserved, not redesigned.** Exception handling stays outermost. Per the Q3 ruling, `UseCors(DevSpa)` sits after it and before endpoint execution. Then `MapDefaultEndpoints()` and `MapApiEndpoints()` (Q1, Q5). DEV-308 does not reorder or add other middleware: no auth, no HTTPS redirection, no `UseRouting` unless the CORS placement ruled in Q3 needs it explicitly.
3. **DEV-308 delivers only the Api-host half of the DEV-376 composition clause.** The Api references Infrastructure (already true, recon-DEV-308:10) and registers the frozen manifest through AddHandler (Q2 and Q6). That satisfies the "dispatching host references Infrastructure and calls AddHandler" clause for the Api host only. The Worker side, MoviesController/IMovieService migration, and endpoint migration stay with DEV-376 or DEV-388 as their ticket text says (recon-DEV-308:48).
4. **Never claimed by DEV-308:**
   - live-endpoint 422 acceptance, because there is no endpoint in this ticket and so no request can reach a validator;
   - any DEV-376 acceptance criterion as complete;
   - "decorators added by DEV-308". The decorators and the Compose order pre-exist (ServiceCollectionExtensions.cs:14, :50-65); DEV-308 only consumes them.

   The spec, plan, PR body and final report must say "DEV-376: Api-host registration prerequisite supplied; 422 live proof not delivered here".
5. **There is no 422 test in DEV-308.** The decorated graph is proven by AC2 (TracingDecorator outermost, one full walk, Q4). Mapper behaviour is already unit-covered where it lives. The first live 422 proof belongs to the first endpoint that accepts input and has a validator: DEV-309 query validation or DEV-310 commands, whichever registers a validator first. That is a ticket-ownership fact for Rigger to record, not a DEV-308 deliverable.
6. **YouTrack record (Rigger, Patron-decided, recording rather than escalation).** Add one comment on DEV-376 stating that DEV-308 supplies Api-host AddHandler registration for the frozen manifest and does not deliver the live 422 acceptance. No change to the DEV-376 summary or acceptance criteria. A follow-up ticket is filed only if the brief later finds a DEV-376 clause that no ticket owns.

## Basis
- DEV-308 ticket scope 1 ("from DEV-376") and AC1-AC3: none of them mentions 422.
- recon-DEV-308:10, :14, :48 (DEV-376 text, existing mapper, and Program.cs lines).
- Q1 (DEV-307 build base), Q2/Q6 (manifest), Q3 (`DevSpa` placement with the exception handler preserved), Q4 (host harness, AC2 proof), Q5 (endpoint mapping).
- Care item 6: `ValidationExceptionHandler.cs` is not named by the ticket, so rewriting it is out.
- Care item 4: status-code mapping is public API shape that DEV-309/310 inherit and that DEV-308 does not change.
- Patron start instruction: "DEV-376 ownership without claiming its live endpoint 422 acceptance completed".

## Rationale
The mapper already exists and works. Touching it in a ticket with no endpoints would be untested churn. Saying exactly which half of DEV-376 DEV-308 supplies stops DEV-376 being closed by implication, and stops reviewers flagging the missing 422 proof as a DEV-308 defect.

## Cost
- The 422 contract stays unproven end-to-end until the first validating endpoint lands.
- Rigger writes one YouTrack comment.
- The wording discipline in item 4 is checked at review: a PR body or report claiming DEV-376 completion is a finding.

## Alternatives rejected
- Adding a test-only endpoint to prove 422 now: it is a route DEV-308 must not own (Q5 zero-route ruling), and it is test scaffolding in production `Program.cs`.
- Moving ValidationExceptionHandler into ServiceDefaults or Infrastructure: care items 2 and 6, and no ticket text asks for it.
- Declaring DEV-376 done for the Api: DEV-376 acceptance includes live-endpoint behaviour that does not exist yet.

## Asks of Patron in this ruling
- (a) Accept "inherited unchanged" for the mapper and middleware order.
- (b) Accept the exact never-claim wording.
- (c) Decide the DEV-376 YouTrack comment for Rigger.
- (d) Confirm the first live 422 proof is owned by whichever of DEV-309 or DEV-310 first registers a validator. If you prefer a fixed ticket, name it.


Patron verdict: ACCEPT preserving the inherited mapper and exception middleware; DEV-308 supplies only the planned Api registration prerequisite, while DEV-376 retains its live migrated-endpoint 422 acceptance.

- Basis: DEV-308 Scope 1/AC1-AC3 require decorated host registration, not endpoint behavior; Overview assigns business endpoints to DEV-309/310. Trusted recon-DEV-308:48 and repaired recon-DEV-308-handlers:56 identify the existing single mapper and DEV-376's live-host criterion. No second mapper, new error shape or production test route is needed. A boot-blocking defect still follows Q4's minimum forced-fix rule, rather than an absolute ban on an initially unnamed file.
- The first migrated validating endpoint in DEV-309 or DEV-310 must carry the constitution-required host integration proof of its real input/error path (constitution:198-213,359-362), and can furnish evidence to DEV-376. DEV-376 stays open until its own acceptance is verified; no completion, migration ownership transfer, ticket change or constitution departure is implied by this grill. Use the brief wording: DEV-376: Api-host registration prerequisite supplied by DEV-308 implementation; 422 live proof not delivered here.
- Approve one recording-only comment on DEV-376 through Rigger, describing this planned division and preserving the current acceptance criterion. No follow-up ticket is needed for already-owned migration work. No owner checkbox is required.

---

# DEV-308 grill Q8/12 - File set, gates, size and loop terms (Keel)

## Question
What is the frozen file set (provisional only where Q6 is pending), the gate expectations, the review size and the round/closing terms that brief.md records?

## Recommendation
1. **Production files.** Five are certain. Each Q6 outcome can add at most one more.
   - `src/LamuFlix.Api/Program.cs` (edit): add `AddCors` with the `DevSpa` policy, the call to the HandlerRegistration extension, `UseCors(DevSpa)` after the exception handler, and `MapApiEndpoints()`. The `MapDefaultEndpoints()` and TimeProvider wiring inherited from DEV-307 are kept exactly once (Q1). The policy name and origin are local `const` values in Program.cs. There is no separate CORS file, because the policy has one consumer and a new type would be ceremony.
   - `src/LamuFlix.Api/HandlerRegistration.cs` (new, internal): one explicit AddHandler line per manifest entry (Q2; manifest frozen at Q6).
   - `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`, `LibraryEndpoints.cs`, `ImportEndpoints.cs` (new, internal): Q5.
   - `src/LamuFlix.Api/Endpoints/.gitkeep` (delete): Q5.
   - Conditional, one file at most: the minimum boot-blocker fix Q6 rules for G2 (IMediaLibraryScanner), cited to AC1/AC2 under the Q4 rule. Its exact path is named in the Q6 ruling, not here.
2. **Test files.** Both are new in `tests/LamuFlix.IntegrationTests`, using the factory inherited from DEV-307 (Q4).
   - `ApiHostCompositionTests.cs`: the AC1 host start, the AC2 manifest theory (TracingDecorator outermost for each entry, plus one full walk), and the route-table check (required health routes present, no `/api` business route; Q5).
   - `ApiCorsTests.cs`: AC3 exactly as the Q3 ruling.
   - Conditional: the move-only factory extraction (Q4 care item 6), with the actual files named in the pickup receipt.
   - No unit-test project change. There are no comments in tests (Q5 ruling).
3. **No other file changes.** No csproj, no Directory.Packages.props, no appsettings, no ServiceDefaults, no Infrastructure edit beyond any Q6 boot-blocker fix, and no `web/`, OpenAPI or migration files.
4. **Gates, all required and none waived:**
   - Run `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1` (<= 15, then `-Threshold 6` at refactor) and `run-jetbrains-inspectcode.ps1` on the changed .cs set.
   - Run `dotnet format --verify-no-changes`, then the full `dotnet test`. Compare the test count with the pickup baseline: only additions are allowed, no removals.
   - Run `run-vulnerable-packages.ps1` with its exit code recorded. No new pin is expected, but the gate still runs.
   - The architect stage runs as the pipeline defines it. A surviving mutant in composition code is routed to a test or recorded with its reason, never waived silently.
   - A skipped gate is reported as skipped, never as passed.
   - InspectCode findings on the near-empty mappers are fixed by changing the code's shape, never suppressed (Q5 cost).
5. **Pickup prerequisites (Phase B, recorded in the receipt).**
   - DEV-307 code is merged on the build base (Q1). `AddServiceDefaults` registers TimeProvider and `MapDefaultEndpoints` exists.
   - The IntegrationTests csproj carries the Api ProjectReference and Mvc.Testing (Q4). Record the factory type T016 produced and whether extraction is needed.
   - Re-run `/speckit-analyze` against main. Amend tasks.md if anything has drifted.
6. **Size and review.** I recommend treating DEV-308 as **M** for review: three axis reports (Sentry, Ledger, Compass) and no adjudication without all three. The scope is composition plus two test files across 7-9 files, with no schema, no new dependency, no new layer and no route. Recon could not read the size tag (note DEV-308:3), so Rigger confirms the YouTrack `size:` tag with `-Show`. If the tag says L, the L process applies and Keel owes an ADR only if a real architectural decision has emerged, and none has so far. If it says S, I still recommend M review, because AC2 asserts on the whole host DI graph.
7. **Loop terms, inherited from the standing DEV-307 CONCLUSIONS closing terms:**
   - Only Critical or High findings with a concrete failure scenario block.
   - The cap is two review rounds and two fix commits per round.
   - Scope is frozen at this brief. Anything outside it is a follow-up issue filed by Rigger on Patron's decision, not a finding in this round.
   - Unrelated latent defects follow the follow-up policy. Boot blockers follow Q4.
   - Never merge. Report `awaiting-merge: DEV-308 (#n)`.
8. **Closing bar for Phase B.** AC1-AC3 green through the host tests. All gates green or reported. The PR body carries the DEV-376 wording from the Q7 ruling. No DEV-376 acceptance criterion claimed. CONCLUSIONS.md and ASSUMPTIONS.md stay Patron-owned and append-only.

## Basis
- Ticket scope 1-4 and AC1-AC3.
- Rulings Q1-Q7 (CONCLUSIONS.md).
- CLAUDE.md gate section.
- Keel duties §5 (M/L three-axis rule, ADR for L).
- DEV-307 CONCLUSIONS closing terms (cited by the Patron start instruction).
- recon-DEV-308:9-10, :35-37.
- Note DEV-308:3 (size not surfaced).

## Rationale
- A file list frozen before plan drafting keeps Quill's plan honest and gives Step 7 a boundary diff to check against.
- Local constants avoid a speculative CORS type.
- M review matches a change that is small but touches the host's whole DI graph.

## Cost
- The file list carries at most two conditional entries until Q6 and the pickup receipt resolve them.
- The M review costs three axis reports for a small diff.
- Rigger performs one size-tag read.

## Alternatives rejected
- A `DevSpaCorsPolicy.cs` type or a CORS extension method: one consumer, so it adds no value.
- Folding the CORS tests into the composition test file: mixes AC2 and AC3 evidence and makes the axis review harder.
- Fixing the review size as S now: unknown tag, and the risk sits in the DI graph.

## Asks of Patron in this ruling
- (a) Accept the file set, with the conditional entries bounded as stated.
- (b) Accept the gate list and the no-waiver wording.
- (c) Rule on the review size: M, pending Rigger's tag read.
- (d) Confirm the inherited loop terms.

## Close note
After Q6 (manifest and the G2 boot-blocker ruling) I see no independent decision left. Unless Q6 opens something, I will propose closing at Q6 + shared-understanding confirmation rather than filling the 12-question cap.


Patron verdict: ACCEPT the file envelope and M review recommendation pending the live size tag; CHANGE gate wording/flags and retain the existing loop terms without reopening them.

- Basis: DEV-308 Scope 1-4/AC1-AC3 and Q1-Q5/Q7 justify Program.cs, HandlerRegistration.cs, the three Endpoints files, disposable .gitkeep removal, two integration test files and only the conditional inherited-factory extraction/Q6 forced DI fix. Do not impose an arbitrary one-file maximum on a required blocker fix; freeze its actual minimum file set from Q6 evidence. No speculative package/project/config/route/schema/web change is approved. Rigger confirms the live size tag; if absent, record size:M; an existing non-M tag comes back for adjudication rather than silently changing it.
- Required applicable gates must pass. Exit 1 or Could not run blocks; exit 2 scope-empty SKIPPED blocks an applicable gate, and only an actual disabled gate is non-blocking SKIP. Never call all gates green merely because failures were reported. Analyzer/complexity/InspectCode scope and thresholds come from harness.yml and each script's Help; do not invent -Threshold or hardcode thresholds. Include property/vulnerability/format/full test checks and the stage-appropriate mutation gate; every surviving mutant is a missing test, not a reason to waive it. Property tests may be documented as opt-out for composition with no domain invariant, according to the pipeline, but otherwise their gate remains required. Compare actual failures/coverage at pickup, not a rigid test-count rule forbidding legitimate replacement of obsolete tests.
- Standing DEV-307 CONCLUSIONS closing terms and task-pipeline section 2.2 remain in force: Critical/High concrete failures block; two review rounds, at most two fix commits per round. Frozen scope is DEV-308's complete ticket deliverables and the minimum wiring/verification required by them; anything else is a follow-up issue, not a finding in this round. A deferred blocking failure does not become clean. Pickup recon records merged DEV-307 code, factory/references and inherited route surface; Keel adjudicates meaningful drift and invokes analysis only at its proper pipeline stage/within the existing cap. No owner checkbox is required by Q8.

---

# DEV-308 grill Q6/12 - Frozen Api handler manifest and minimum adapter bindings (Keel)

## Question
Which handler rows go into `HandlerRegistration.cs`? Which existing adapter bindings must DEV-308 add so that every row resolves under ValidateOnBuild + ValidateScopes (Q4)? And what does DEV-308 leave alone?

## Recommendation

### A. Manifest: exactly seven rows, one explicit AddHandler each (Q2)
| # | Handler | Contract | Request -> Response | Endpoint owner |
|---|---|---|---|---|
| 5 | RequestEnrichmentCommandHandler | ICommandHandler | RequestEnrichmentCommand -> MovieId | DEV-310 POST /api/movies/{id}/enrichment |
| 7 | ImportMovieFolderCommandHandler | ICommandHandler | ImportMovieFolderCommand -> MovieId | DEV-310 POST /api/movies/import |
| 8 | BrowseMoviesQueryHandler | IQueryHandler | BrowseMoviesQuery -> PagedResult<MovieSummary> | DEV-309 GET /api/movies |
| 9 | GetMovieDetailsQueryHandler | IQueryHandler | GetMovieDetailsQuery -> MovieDetails | DEV-309 GET /api/movies/{id} |
| 10 | PlayMovieCommandHandler | ICommandHandler | PlayMovieCommand -> Unit | DEV-310 POST /api/movies/{id}/play |
| 11 | AddToWatchlistCommandHandler | ICommandHandler | AddToWatchlistCommand -> Unit | DEV-310 POST /api/movies/{id}/watchlist |
| 12 | RemoveFromWatchlistCommandHandler | ICommandHandler | RemoveFromWatchlistCommand -> Unit | DEV-310 DELETE /api/movies/{id}/watchlist |

Source: recon-DEV-308-handlers sections 1 and 5. The ticket mapping is verbatim from the DEV-309 and DEV-310 text.

- **Excluded.** The four enrichment-internal rows (#1 Apply, #2 Claim, #4 RecordFailure, #6 RequeueStranded) are excluded: no API ticket names them and no production caller exists (section 4).
- **#3 ProcessEnrichment** stays exactly as inherited: the conditional registration at RabbitMqServiceCollectionExtensions.cs:42-46 and its validator inside the same block (G0/G1). DEV-308 adds no duplicate line. The Api still resolves #3 through that registration as long as the adapter call order in Program.cs is preserved. AC2 does not assert on #3, because it is not a DEV-308 manifest row. Its inherited registration has its own tests (RabbitMqServiceCollectionExtensionsTests).
- **Facets.** GET /api/genres and GET /api/people have no handler today (G3). They stay with DEV-310. DEV-308 adds no row, port or placeholder for them.

### B. Minimum existing-adapter bindings (G2), no new type of any kind
1. **IMovieCatalog (rows 8, 9, 10).** Call the existing `AddMovieCatalog` (Persistence/MovieCatalogServiceCollectionExtensions.cs:8-12) from Program.cs, immediately after `AddLamuFlixPersistence`, because the catalog depends on what persistence registers. Program.cs is already a named file, so no unnamed file is touched.
2. **IMediaLibraryScanner (row 7).** Register the existing `DirectoryMediaLibraryScanner` (Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs:14) as `IMediaLibraryScanner`. Where the line goes depends on the facts in section D:
   - If its constructor needs only framework services or already-registered options (for example `IOptions<LibraryOptions>`, `ILogger<>`, `TimeProvider`), it is one line in the internal `HandlerRegistration.cs`, placed before the AddHandler rows. It is the Api-composition prerequisite for row 7, so no public extension is added for a single binding.
   - If its constructor needs Infrastructure-internal types not visible to Api, or the type itself is `internal`, the binding goes into the established Infrastructure extension pattern next to it (an existing `Add...` method if one already covers FileSystem). Only if none exists does a minimal `AddLamuFlixFileSystem`-style extension in Infrastructure get added, called from Program.cs. The Q4 rule (AC-forced minimum fix, even in an unnamed file) authorises it, and the brief records the exact file.
   - **Lifetime.** Match the lifetime the scanner's dependencies allow, so ValidateScopes passes. Choose singleton if it is stateless with singleton-safe dependencies, otherwise scoped. Never transient-by-default without a reason. The choice is frozen from the constructor fact, not guessed.
3. **No other binding is added on speculation.** Rows 5, 7, 11 and 12 need `IMovieRepository` and `IEnrichmentQueue`, and row 10 needs `IMediaPlayerLauncher`. These are expected to come from the existing AddLamuFlixPersistence, AddLamuFlixRabbitMq and AddLamuFlixPlayback calls, but that is unconfirmed (section D). Any gap found there falls under the Q4 boot-blocker rule: minimum fix, cited to AC2, files frozen in the brief.

### C. Ordering and claims
- **Program.cs order:**
  1. AddServiceDefaults (inherited)
  2. AddLamuFlixPersistence
  3. AddMovieCatalog (new)
  4. AddMetadataProvider
  5. AddLamuFlixRabbitMq
  6. AddLamuFlixPlayback
  7. the HandlerRegistration extension (new; scanner binding + 7 rows)
  8. AddCors(DevSpa) (new)
  9. AddProblemDetails / AddExceptionHandler (inherited)

  The inherited relative order of every existing call is unchanged, so the conditional #3 branch stays active exactly as today. New calls are only inserted.
- **AC2 claim limit.** AC2 proves that each of the seven rows resolves through Tracing -> Logging -> Validation. The validator sequence is optional today, and none of the seven rows has a registered validator (only ProcessEnrichmentCommandValidator exists). So no test, spec line or PR text may claim input-validation coverage for these rows. No constitution exception is needed: DEV-308 adds no input endpoint, and validation stays mandatory for the DEV-309/310 endpoints.

### D. Facts still needed before the brief closes (from recon-DEV-308-handlers-2, which Patron can read and Keel cannot)
- F1. The DirectoryMediaLibraryScanner constructor dependencies, its visibility (public or internal), and its file:line.
- F2. Whether IMediaPlayerLauncher is registered unconditionally by AddLamuFlixPlayback, or only when `Features:LocalPlay` is on (PlaybackServiceCollectionExtensions.cs:19). If it is conditional, row 10 fails ValidateOnBuild in a LocalPlay-off host. That would be a care item 5 (LocalPlay) ruling, not a DEV-308 guess. My recommendation for that case: the Q4 factory sets `Features:LocalPlay` explicitly, and the brief records which value AC2 runs under. DEV-308 does not change the LocalPlay gate.
- F3. Whether IEnrichmentQueue and IMovieRepository are registered unconditionally, or (like #3) only inside the RabbitMq IsConsumerActive block. If only inside it, rows 5 and 7 have the same exposure as F2.
- F4. The lifetimes of IMovieCatalog, IMovieRepository and IEnrichmentQueue, to freeze the scanner lifetime and confirm that ValidateScopes passes.

If recon-DEV-308-handlers-2 already answers F1-F4, please quote the lines in your ruling and I will freeze them into brief.md. Any fact it lacks is routed once as needs recon to Bernstein; I will not guess it.

## Basis
- recon-DEV-308-handlers sections 1, 4 and 5 (inventory, constructors, callers, verbatim ticket text).
- Patron G2 evidence summary (recon-DEV-308-handlers-2:48-70): the scanner implementation exists and is unregistered; AddMovieCatalog exists and is uncalled.
- G0/G1 (inherited #3 descriptor and validator), G3 (facets have no handler).
- Rulings Q2 (explicit rows, internal HandlerRegistration.cs, Worker-only registrations untouched), Q4 (ValidateOnBuild/ValidateScopes; an AC-forced minimum fix even in an unnamed file), Q7 (no 422 or validation claim), Q8 (no file cap on an AC-forced DI fix).
- Care items 2 and 6: no new port, adapter or validator.

## Rationale
Seven rows are exactly the handlers the two endpoint tickets dispatch, so DEV-309/310 only map routes. The two bindings wire existing adapters that already have implementations, which is composition rather than new behaviour. Without them, AC2 cannot pass under ValidateOnBuild for rows 7-10. Keeping the binding in the internal registration file avoids a public extension that exists only for one line.

## Cost
- The scanner placement and lifetime stay conditional until F1 and F4 arrive.
- F2 and F3 may surface a LocalPlay or consumer-gate interaction. That needs one more Patron ruling, but no owner checkbox: it is a test-configuration choice, not a gate change.
- Excluding rows 1, 2, 4 and 6 means a future API need for them is a new ticket.

## Alternatives rejected
- **Registering all 12 rows:** registers four internal handlers with no caller, and duplicates #3.
- **Excluding row 7 to dodge G2:** under-delivers DEV-310's prerequisite and the "every handler resolves" intent of scope 1.
- **A new IMediaLibraryScanner adapter or a stub:** the implementation already exists; care item 2.
- **Narrowing ValidateOnBuild to skip the scanner:** forbidden by the Q4 ruling.

## Asks of Patron in this ruling
- (a) Accept the seven rows and the four exclusions.
- (b) Accept the AddMovieCatalog placement and the scanner-binding decision rule.
- (c) Supply F1-F4 from recon-DEV-308-handlers-2, or authorise one needs-recon request.
- (d) Rule the F2/F3 contingency in advance: set the test config explicitly and do not change the gates.


Patron verdict: ACCEPT the exact seven-row manifest, exclusions, existing AddMovieCatalog call and preserving inherited order; final scanner binding recipe remains needs recon, not an owner checkbox.

- Basis: repaired recon-DEV-308-handlers:52-56 maps DEV-309/310 to RequestEnrichment, ImportMovieFolder, BrowseMovies, GetMovieDetails, PlayMovie, AddToWatchlist and RemoveFromWatchlist; part2:78-87 identifies the four unused enrichment-internal handlers. Exclude those four and do not duplicate the inherited ProcessEnrichment row/validator (part2:24-36). Preserve persistence -> metadata -> RabbitMq order, which makes the existing consumer registration active. Facets belong to DEV-310; no placeholder query/port/handler is added here (part2:72-76).
- DEV-308 Scope 2 authorizes wiring existing Infrastructure adapters. Call AddMovieCatalog immediately after AddLamuFlixPersistence: part2:48-64 proves this existing scoped adapter and the missing scanner binding cause four Api-facing rows to fail resolution. Approve registration of existing DirectoryMediaLibraryScanner rather than a new adapter or test stub. The exact constructor dependencies and minimum binding/file recipe remain unverified; one bounded Wisp request was sent through Bernstein for F1-F4. Repository/catalog are scoped (part2:26,50); queue registration precedes the :40 conditional (part2:42). No speculative API/helper/dependency change or test-configuration-only workaround is approved while those facts remain incomplete.
- Host proof must retain LocalPlay off and resolve the play handler without executing media; any unconditional-resolution defect gets the Q4 minimum forced fix, rather than enabling execution merely to hide it. Preserve the gate and single mapper. The seven ValidationDecorators have an empty validator sequence today (part2:10-18); graph resolution is not evidence that invalid input was rejected. Future endpoint validation remains mandatory under constitution V. Complete the binding recipe before declaring shared understanding or writing a settled brief.

---

## Q6 final exchange - Existing scanner dependencies and disabled playback

Keel follow-up after the full part2 receipt: the seven-row manifest and three existing registration facts are understood; F1 remains the scanner's visibility, constructor dependencies and lifetime, which decides whether binding belongs in Api or an Infrastructure extension. The brief is held until that fact is ruled.

Patron content read (named worktree, no search, build or gate): src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs:13-14 declares public sealed partial DirectoryMediaLibraryScanner(IFileSystem fileSystem, TimeProvider timeProvider) : IMediaLibraryScanner. Its Scan method begins file access at :19-24, not construction. src/LamuFlix.Infrastructure/Playback/PlaybackServiceCollectionExtensions.cs:16-27 always registers a singleton IMediaPlayerLauncher; :19-21 returns DisabledMediaPlayerLauncher when LocalPlay is false.

Patron verdict: ACCEPT and freeze the final DI recipe: TryAddSingleton for the existing real FileSystem/IFileSystem adapter and AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner> in internal HandlerRegistration.cs before the seven AddHandler lines; use inherited TimeProvider and call existing AddMovieCatalog after persistence. Shared understanding is reached; Keel may now write the settled brief and matching note.

- Basis: DEV-308 Scope 2 requires existing Infrastructure adapter composition; the scanner's two public constructor dependencies above need no new adapter, project or public registration extension. Constitution:325 already selects System.IO.Abstractions; use its existing FileSystem implementation and existing dependency pin, with TryAdd preserving any inherited/test registration. Scoped scanner aligns with the scoped handler/import graph and safely consumes the singleton filesystem/time services; no singleton-to-scoped capture is introduced. Only Program.cs and the already-approved HandlerRegistration.cs need these bindings.
- Keep Features:LocalPlay false in the host fixture and preserve PlaybackServiceCollectionExtensions unchanged: its unconditional singleton binding selects the disabled launcher, so handler resolution does not require enabling execution. No media launch, gate change, new process behavior or secret/config default is introduced. Repository/catalog scoped and queue-before-consumer facts remain from part2:26,42,50; existing relative adapter order stays unchanged. Preserve the inherited ProcessEnrichment descriptor/validator once, exclude the four unused enrichment-internal handlers, and leave facets to DEV-310.
- The minimum production file set is now the five files from Q8 plus deletion of the disposable Endpoints/.gitkeep; the two host-test files reuse the DEV-307 factory, with only its previously approved move-only extraction conditional at pickup. The exact manifest and binding decisions are settled. No owner checkbox, ticket change or constitution departure is required; no glossary/ADR edit is warranted for this reversible composition. Quill may draft after the settled brief is written; Gate 1 and implementation remain closed until their separate pipeline requirements are met.

---

## Phase B intake - Worktree and branch naming

Patron verdict: (b) Rigger renames the existing worktree to F:/Dev/LamuFlix.worktrees/DEV-308 with git worktree move and its branch to feature/DEV-308 with git branch -m before Phase 3; Conductor aligns chain/task references with those exact names.

- Basis: task-pipeline Phase 1 step 5 (:58) explicitly requires F:/Dev/LamuFlix.worktrees/DEV-### on feature/DEV-###. The script's ToLowerInvariant at scripts/new-task-branch.ps1:89 and slash-to-hyphen folder construction at :239 explain the generated names; they do not override the named Phase B requirement.
- Basis: task-pipeline Phase 6 introduction and step 2 (:132,136) identify the delivery branch as feature/DEV-### and remove F:/Dev/LamuFlix.worktrees/DEV-###. Renaming now preserves that intake-to-sweep contract without changing the standing pipeline. Rigger verifies the resulting path/branch; Patron does not perform the move.
- Separately, Todo -> In Progress is Rigger's mechanical Phase 1 step 3 action (task-pipeline:53-55), requiring the verified state receipt. This naming correction changes neither ticket deliverables nor constitution; no owner checkbox is required (specs/PRODUCT.md:34-39).

---

## Owner decision 2026-10-03 - Absorb the missing host prerequisites

Patron verdict: RECORD the owner's authorized charter section 2.3(a) scope addition, relayed by Conductor on 2026-10-03: DEV-308 absorbs the WAF harness (the IntegrationTests Api ProjectReference, the Microsoft.AspNetCore.Mvc.Testing package reference and central pin, and a WebApplicationFactory<Program> host) and the missing MapDefaultEndpoints implementation with exactly one Api call. FR-033 and the Q1/Q4 assumptions that these prerequisites are delivered by DEV-307 and merely reused are superseded by this ruling; DEV-308 now makes the csproj edit, pin, factory host and MapDefaultEndpoints call.

- Gap evidence: Conductor and Anvil independently verified origin/main HEAD 122c502 has no MapDefaultEndpoints or WebApplicationFactory; tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj has no Api ProjectReference or Mvc.Testing package. The DEV-308 task note:27-39 records the unmet T001 prerequisite and its effect on AC1/AC2/AC3 and Scope 4. This ruling trusts their verified evidence.
- Delivery evidence: DEV-307 delivery PR #82 merged only ServiceDefaults observability; its N2 (Api ProjectReference plus Mvc.Testing) and N3 (Mvc.Testing version) owner decisions remain unchecked. Its merge therefore did not supply the assumed harness or MapDefaultEndpoints. This owner decision does not retroactively mark those PR #82 checkboxes answered.
- Authority: the owner explicitly chose DEV-308 absorption on 2026-10-03 under charter section 2.3(a); specs/PRODUCT.md:23-24,32-39 reserves ticket deliverable changes to the owner. The settled envelope in this file:15-24 supplies Scope 4 and AC1/AC2/AC3; spec.md:186 (FR-033) and this file's Q1/Q4 reuse assumptions yield to the owner's decision. Keel and Quill handle the resulting brief/tasks changes; this append records the ruling only.

Patron verdict: CONFIRM (a)/(b): DEV-308's MapDefaultEndpoints absorption creates /health/live only (Predicate = _ => false, AllowAnonymous), with exactly one Api call; /health/ready, the readiness tag, aggregate/membership and response writer remain for a follow-up, consistent with b19e883 and within its expanded scope (basis: owner absorption above:418-422; re-frozen brief at 5b329ef, Q1:33-34; DEV-307 plan section 5:241 explicitly separates the ungated method/liveness from Q2-gated readiness/writer).

Patron verdict: ACCEPT option (b): do not add a UnitTests -> LamuFlix.Api project reference in DEV-308; record Api mutation as Could not run (harness design, exit 1), trusting Conductor's verified 184 passing IntegrationTests as host-test evidence only, not mutation proof. Basis: ticket Scope/AC envelope in CONCLUSIONS.md:15-24 and constitution.md:286-290 place API host proof in IntegrationTests and name Core for mutation coverage; a reference alone supplies neither Api unit tests nor a mutation score. Phase 5 proceeds; required-gate blocking remains under spec.md:192-193 (FR-039/FR-040) and brief.md:189; this ruling grants no gate waiver or green PR-readiness claim. Optional Api unit-mutation expansion: noted, no ticket.
