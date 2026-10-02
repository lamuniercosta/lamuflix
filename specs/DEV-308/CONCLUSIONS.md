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
