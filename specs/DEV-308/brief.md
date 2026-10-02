# DEV-308 brief: compose the LamuFlix.Api host

**Status: SETTLED (2026-10-02).** Patron accepted the final Q6 ruling and confirmed shared understanding. The grill stopped at eight numbered questions; Q9-Q12 were not needed. There are no owner checkboxes, no ticket changes, no constitution departure, no new domain term and no ADR. One receipt is still pending, and it does not block: Rigger's live `size:` tag read (section 9). Quill drafts spec, plan and tasks from this file.

Owner: Keel. Rulings: `specs/DEV-308/CONCLUSIONS.md` (Patron, append-only). Taste: `specs/DEV-308/ASSUMPTIONS.md`. Recon: notes `recon-DEV-308` and `recon-DEV-308-handlers`; the completed handler findings and source citations are recorded in the Q6 rulings in `CONCLUSIONS.md`. Worktree `F:/Dev/LamuFlix.worktrees/feature-308-spec` @ `feature/308-spec`.

## 1. Ticket (decided text, read live 2026-10-02)
- **Overview.** Compose the existing minimal Api host for DEV-309 and DEV-310, which own the endpoints. Deleting LamuFlix.Web belongs to DEV-388.
- **Scope 1.** Api references Infrastructure and registers handlers through AddHandler, so each one resolves with its Validation, Logging and Tracing decorators.
- **Scope 2.** Register the Infrastructure adapters, and TimeProvider through host DI (DEV-307).
- **Scope 3.** CORS for `http://localhost:5173`.
- **Scope 4.** `app.MapDefaultEndpoints()` (DEV-307), plus the endpoint-group convention (`MapLibraryEndpoints`, `MapImportEndpoints`, ...).
- **AC1.** Api starts cleanly as a Minimal API host with no MVC or Razor.
- **AC2.** A test resolves a registered handler from the Api host provider and gets the decorated pipeline.
- **AC3.** CORS allows the React dev server origin.

## 2. Closing bar and frozen scope
- **Closing bar for Phase B:**
  - AC1-AC3 are green through the host tests in section 6.
  - Every applicable gate in section 7 passes.
  - The PR body carries the DEV-376 wording from section 5 (Q7).
  - No DEV-376 acceptance criterion is claimed.
  - No owner checkbox is pending (none has arisen).
- **Frozen scope:** sections 3-6; anything else is a follow-up issue, not a finding in this round.

## 3. Grill answers (rulings in CONCLUSIONS.md; Keel's full exchanges are kept in TEMP `DEV-308-keel-q3.md` .. `-q8.md` and `-q6.md`)

**Q1: prerequisite.**
- Phase B starts only when DEV-307 **code** is merged on the build base.
- DEV-308 uses the ServiceDefaults `TryAddSingleton(TimeProvider.System)` and `MapDefaultEndpoints` exactly once. It adds no provisional copy.
- Scope 4's convention half stays in DEV-308.
- The two Infrastructure `TryAddSingleton(TimeProvider.System)` calls stay untouched.
- Basis: constitution:235-236; recon-DEV-308:20,28.

**Q2: handler ownership.**
- One explicit AddHandler per service contract, in internal `src/LamuFlix.Api/HandlerRegistration.cs`, called from Program.cs.
- Only Api-facing handlers that DEV-309 or DEV-310 dispatch are registered there.
- No reflection or assembly scanning.
- Worker-only registrations stay untouched.
- The host test resolves every manifest entry.
- Basis: constitution:133-136; DEV-309 and DEV-310 scope text.

**Q3: CORS.**
- One globally applied named policy, `DevSpa` (`[assumed]` name), allowing only `http://localhost:5173`.
- AllowAnyMethod and AllowAnyHeader, with no credentials.
- `Location` is the only exposed header beyond the defaults.
- `UseCors` runs after routing and before endpoint execution. The exception handler is preserved.
- No options class and no config section.
- The policy name and origin are local `const` values in Program.cs.

**Q4: harness.**
- Reuse the `WebApplicationFactory<Program>` and the IntegrationTests references that DEV-307 adds. DEV-308 makes no csproj, pin or `partial class Program` change.
- Conditional: a same-project, move-only extraction of the DEV-307 factory (care item 6). The actual files are named in the pickup receipt.
- ValidateOnBuild and ValidateScopes are on.
- **Boot-blocker rule:** a defect that stops AC1 startup or the resolution of a Scope 1 manifest entry forces its minimum fix in this ticket, even in a file the ticket did not name, cited to the AC or failing gate. Validation is never narrowed, the blocker is never deferred, and green is never claimed early. Unrelated non-blockers follow the follow-up policy.
- **Config:** runtime config is supplied early enough for the production registrations, using the inherited .NET 10 factory recipe (ConfigureAppConfiguration can be too late).
- No real credentials, connection strings or machine paths are committed.
- **No containers.** The proof covers DI and `/health/live`, not driver behaviour or readiness.

**Q5: endpoint convention.**
- `Endpoints/ApiEndpoints.cs` holds internal `MapApiEndpoints(this IEndpointRouteBuilder)`, which owns the root `/api` group (basis: the DEV-309 title "GET /api/movies").
- `Endpoints/LibraryEndpoints.cs` (`MapLibraryEndpoints`) and `Endpoints/ImportEndpoints.cs` (`MapImportEndpoints`) are internal extensions on the shared `RouteGroupBuilder`. They map zero routes and declare no sub-prefix, status or DTO.
- Delete `Endpoints/.gitkeep`.
- **No explanatory comments** anywhere, in code or tests (project rule). The convention lives in this brief only: one static class per feature in `Endpoints/`, a `MapXxxEndpoints(this RouteGroupBuilder api)` extension, a call added in `MapApiEndpoints`, no P1 versioning (constitution:463).
- **Host proof:** required health routes are present and there is no `/api` business route. Doc routes inherited from DEV-307 are preserved. Do not assert an exact health-only set.
- No test that mirrors the empty methods.

**Q6: manifest and binding recipe (final ACCEPT).**

| # | Handler | Contract | Request -> Response | Endpoint ticket |
|---|---|---|---|---|
| 5 | RequestEnrichmentCommandHandler | ICommandHandler | RequestEnrichmentCommand -> MovieId | DEV-310 |
| 7 | ImportMovieFolderCommandHandler | ICommandHandler | ImportMovieFolderCommand -> MovieId | DEV-310 |
| 8 | BrowseMoviesQueryHandler | IQueryHandler | BrowseMoviesQuery -> PagedResult<MovieSummary> | DEV-309 |
| 9 | GetMovieDetailsQueryHandler | IQueryHandler | GetMovieDetailsQuery -> MovieDetails | DEV-309 |
| 10 | PlayMovieCommandHandler | ICommandHandler | PlayMovieCommand -> Unit | DEV-310 |
| 11 | AddToWatchlistCommandHandler | ICommandHandler | AddToWatchlistCommand -> Unit | DEV-310 |
| 12 | RemoveFromWatchlistCommandHandler | ICommandHandler | RemoveFromWatchlistCommand -> Unit | DEV-310 |

- **Excluded handlers.** Four enrichment-internal handlers are excluded: #1 Apply, #2 Claim, #4 RecordFailure and #6 RequeueStranded. No ticket names them and nothing calls them (CONCLUSIONS.md:392).
- **ProcessEnrichment (#3).** It keeps its inherited conditional Infrastructure registration and validator (RabbitMqServiceCollectionExtensions.cs:40-51). There is no duplicate row: AddHandler uses plain `AddScoped`, so a duplicate would silently swap the consumer's graph (CONCLUSIONS.md:392).
- **Facets.** GET /api/genres and GET /api/people stay with DEV-310 (no handler or port exists, G3). No placeholder is added.
- **G2 gap (CONCLUSIONS.md Q6 cited sources).** In the Api host as composed today, rows #7, #8, #9 and #10 fail ValidateOnBuild:
  - `IMovieCatalog` is unregistered. `AddMovieCatalog` exists at Persistence/MovieCatalogServiceCollectionExtensions.cs:8-12 (scoped), but no host calls it.
  - `IMediaLibraryScanner` is registered nowhere. The only implementation is Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs:14.
  - Under the Q4 rule both are forced minimum fixes.
- **Fix 1 (ruled).** Program.cs calls the existing `AddMovieCatalog()` immediately after `AddLamuFlixPersistence()`.
- **Fix 2 (ruled).** Register the existing `DirectoryMediaLibraryScanner` as `IMediaLibraryScanner`. It is `public sealed partial` with ctor `(IFileSystem, TimeProvider)` (DirectoryMediaLibraryScanner.cs:13-14), and file access starts only in Scan (:19-24). The recipe goes in internal `HandlerRegistration.cs`, in this order:
  1. `TryAddSingleton<IFileSystem, FileSystem>()`, using the existing System.IO.Abstractions real FileSystem.
  2. `AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>()`.
  3. The seven AddHandler rows.
  - TimeProvider is the inherited ServiceDefaults singleton and is consumed, not re-registered.
  - The System.IO.Abstractions pin already exists, so there is no new csproj change, pin, type, port, stub, validator or public extension.
  - Scoped lifetime is ValidateScopes-safe: a scoped scanner consumes singleton FileSystem and TimeProvider, and it matches the scoped import graph (repository and catalog are scoped).
- **Facts already established (Q6 cited sources):**
  - IMovieRepository is scoped and unconditional (Persistence:37).
  - IMovieCatalog is scoped (MovieCatalog:10).
  - IEnrichmentQueue is unconditional (RabbitMq:35, before the :40 conditional).
  - IMediaPlayerLauncher is always registered as a singleton (Playback:16-27). With `Features:LocalPlay` false it returns DisabledMediaPlayerLauncher (:19-21), so row #10 resolves in a LocalPlay-off host with no extra binding.
- **LocalPlay.** The host proof runs with `Features:LocalPlay` OFF and resolves row #10 without executing any media. Enabling LocalPlay as a test-only workaround is forbidden. A missing disabled-mode binding is a Q4 forced fix.
- **AC2 claim limit.** All seven ValidationDecorators have an empty validator sequence today (CONCLUSIONS.md:392). Graph resolution is not evidence that invalid input is rejected, and no text may claim validation coverage. Endpoint validation stays mandatory for DEV-309/310 (constitution V).

**Q7: DEV-376 and the error mapper.**
- `AddProblemDetails`, `AddExceptionHandler<ValidationExceptionHandler>`, `UseExceptionHandler` and the middleware order stay unchanged.
- No second mapper and no production test route.
- DEV-308 implementation supplies the Api-host AddHandler prerequisite. It closes no DEV-376 acceptance criterion.
- The first migrated validating endpoint in DEV-309 or DEV-310 carries its own constitution-required host error-path proof, which DEV-376 may use. DEV-376 keeps its acceptance until verified, and ticket ownership does not move.
- An unchanged mapper is scoped intent, not immunity from Q4.
- Rigger adds one recording-only comment on DEV-376, phrased as planned while DEV-308 is only a spec.

**Q8: envelope, gates, size, loop.** See sections 4, 7 and 8.

## 4. Approach and files
**Program.cs order.** Existing relative order is unchanged; new calls are only inserted:
1. AddServiceDefaults (DEV-307)
2. AddLamuFlixPersistence
3. **AddMovieCatalog**
4. AddMetadataProvider
5. AddLamuFlixRabbitMq
6. AddLamuFlixPlayback
7. **HandlerRegistration extension** (TryAddSingleton IFileSystem, AddScoped scanner, then the 7 rows)
8. **AddCors(DevSpa)**
9. AddProblemDetails / AddExceptionHandler
10. Build
11. UseExceptionHandler
12. **UseCors(DevSpa)** (per Q3 placement)
13. MapDefaultEndpoints (DEV-307, once)
14. **MapApiEndpoints**
15. Run

Persistence -> metadata -> RabbitMq order must hold, because it keeps the inherited ProcessEnrichment branch active (CONCLUSIONS.md:392).

**Production files**
- `src/LamuFlix.Api/Program.cs`: edit.
- `src/LamuFlix.Api/HandlerRegistration.cs`: new, internal.
- `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`, `LibraryEndpoints.cs`, `ImportEndpoints.cs`: new, internal.
- `src/LamuFlix.Api/Endpoints/.gitkeep`: delete.
- **Total:** five production .cs files (Program.cs plus four new), plus the disposable `.gitkeep` deletion. The scanner binding lives inside `HandlerRegistration.cs`, so it adds no file and no Infrastructure edit.
- **Q4 rule still applies.** A boot-blocker that surfaces only at implementation still forces its minimum fix, cited and named in the receipt.

**Test files** (`tests/LamuFlix.IntegrationTests`, inherited factory)
- `ApiHostCompositionTests.cs`: AC1 start, the AC2 seven-row theory (TracingDecorator outermost, plus one full Tracing -> Logging -> Validation -> handler walk), and the route check from Q5.
- `ApiCorsTests.cs`: AC3 per the Q3 ruling.
- Conditional, decided at pickup: the Q4 move-only factory extraction. This is the only conditional entry in the file set.

**Unchanged**
- csproj, Directory.Packages.props, appsettings, ServiceDefaults.
- Infrastructure, unless a Q4 boot-blocker appears.
- `web/`, OpenAPI, migrations.

## 5. Wording discipline (checked at review)
- "DEV-376: Api-host AddHandler prerequisite supplied (planned); no DEV-376 acceptance closed; live 422 proof not delivered here."
- Never write "decorators added by DEV-308". They pre-exist at ServiceCollectionExtensions.cs:14, :50-65.
- Never claim validation coverage for the seven rows.

## 6. Test strategy
- **Host.** Host integration tests through the inherited WAF, with the real Program.cs composition and DEV-307 health-check stubs only. No Testcontainers.
- **Config.** In-memory config per DEV-307 plan.md:270-273 (Library:RootPath, Omdb:ApiKey, Omdb:BaseUrl, RabbitMq:HostName, placeholder ConnectionStrings:DefaultConnection, Enrichment:ClaimLease below RabbitMq:RetryDelay), with `Features:LocalPlay` explicitly false. This config must reach the host before the production registrations read it, via the inherited .NET 10 factory recipe; ConfigureAppConfiguration alone can be too late (Q4). The test resolves row #10 to the DisabledMediaPlayerLauncher graph, and no media is executed.
- **AC2.** A data-driven theory over the seven rows.
- **AC3.**
  - GET `/health/live` from 5173 returns allow-origin and Expose-Headers `Location`.
  - Preflight from 5173 asserts allow-origin, allow-method and allow-headers only.
  - A request from `http://localhost:3000` gets no allow-origin header (absence, not a status code).
- **No explanatory comments in tests.** The project's stated AAA, empty-block and narrowly scoped warning exemptions remain permitted.
- **Accepted noise, not asserted.** EnrichmentConsumer broker retries. A disposal hang is reported, never hidden by a longer timeout.
- **Property tests** may take the documented no-domain-invariant opt-out under pipeline rules.

## 7. Gate expectations (Q8 as corrected)
- **Governed by** `harness.yml` and each script's own help. No invented flags or hard-coded gate list.
- **Gates that apply:**
  - the three static-analysis gates (Roslyn, cyclomatic complexity, InspectCode) on the changed .cs set, plus the refactor-stage complexity setting the harness defines;
  - format, full tests, property tests and vulnerable packages;
  - mutation at its proper stage.
- **Outcome rules:**
  - Required applicable gates must pass. Exit 1 or "could not run" blocks.
  - An applicable gate SKIPPED because its scope is empty blocks. A SKIP because the analyzer is actually disabled does not.
  - A surviving mutant means a missing test. Recording it is never a waiver.
  - There is no rigid only-additions test-count bar.
  - InspectCode findings on the near-empty mappers are fixed by shape, never by suppression.
- **Who runs what.** Drift is checked by recon plus Keel adjudication. `/speckit-analyze` runs only at its proper stage and cap. Patron runs no gates.

## 8. Prerequisites, size, loop terms
- **Pickup prerequisites (receipt):**
  - DEV-307 code is merged: AddServiceDefaults registers TimeProvider and MapDefaultEndpoints exists.
  - The IntegrationTests csproj has the Api ProjectReference and Mvc.Testing.
  - The factory type is identified, along with whether extraction is needed.
  - The drift check against main is done.
- **Size.** **M** is recommended for this composition change, with three axis reports from Sentry, Ledger and Compass. The live tag is unknown pending Rigger's receipt. If absent, Patron authorizes size:M; an existing non-M tag is reported to Patron for adjudication without silently changing the tag or process.
- **Loop terms.** The standing DEV-307 CONCLUSIONS closing terms are cited once and not reopened:
  - Critical or High findings with a concrete failure scenario block.
  - At most two rounds, with two fix commits per round.
  - Out-of-scope items become follow-up issues.
  - Never merge: report `awaiting-merge: DEV-308 (#n)`.
- **Owner checkboxes:** none.

## 9. Pending receipts (non-blocking)
- **Size tag.** Rigger has the request to read the live YouTrack `size:` tag. M is recommended; if the tag is absent, Patron authorizes size:M. An existing non-M tag returns to Patron for adjudication. No verified or absent tag is claimed here.
- **Pickup receipt (Phase B).** The section 8 prerequisites, plus the decision on the factory extraction.
- **Open owner questions:** none.
