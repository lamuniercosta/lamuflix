# DEV-309 brief: GET /api/movies and GET /api/movies/{id}

**Status: SETTLED (2026-10-02).**
- Patron ruled all twelve grill questions in two rounds: round 1 is commit da8985f (Q1-Q7) and round 2 is commit c17b9fc (Q8-Q12). Patron confirmed the final owner set at Q11.
- **Four owner checkboxes are open (section 2).** Gate 1 stays closed until the user answers them on the spec PR.
- Quill drafts spec, plan and tasks from this file, using the recommended answers and keeping the four checkboxes unchecked.
- One receipt is pending, and it does not block: Rigger's verified DEV-20 ownership comment (section 9).

**References.**
- Owner: Keel.
- Rulings: `specs/DEV-309/CONCLUSIONS.md` (Patron, append-only). Cite them; do not restate verdicts.
- Taste: `specs/DEV-309/ASSUMPTIONS.md`.
- Grill packets: TEMP `DEV-309-keel-q1.md` and `-q2.md`, with Patron's replies `DEV-309-patron-q1.md` and `-q2.md`.
- Recon notes: `recon-DEV-309`, `recon-DEV-309-2` (section 15 repaired) and `recon-DEV-309-R2` .. `R7`.
- Worktree `F:/Dev/LamuFlix.worktrees/feature-309-spec` @ `feature/309-spec`.

## 1. Ticket (decided text, recon-DEV-309 section 1)
- **Scope 1.** GET /api/movies binds the query string directly to MovieQuery via [AsParameters], dispatches BrowseMoviesQuery and returns `Results<Ok<PagedResult<MovieSummary>>, ValidationProblem>`.
- **Scope 2.** GET /api/movies/{id} parses an int id as MovieId, dispatches GetMovieDetailsQuery and returns `Results<Ok<MovieDetails>, NotFound<ProblemDetails>>`.
- **Scope 3.** WithName, Produces and WithSummary.
- **AC1.** The query parameters bind correctly to MovieQuery.
- **AC2.** The endpoints return strongly typed TypedResults with OpenAPI metadata.
- The ticket names `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`. DEV-308 creates that file empty.

## 2. Owner checkboxes (section 2.3; all unchecked; each one keeps Gate 1 closed)
The spec PR carries these verbatim as `blocked: structural` checkboxes. Each recommended answer is what the spec drafts. If the owner rejects one, it is reconciled, never assumed away. Converters or nested objects are not a silent fallback (Q8).

1. **Q1, binding (2.3a).** Ticket text: direct `[AsParameters] MovieQuery`. Recommended: `[AsParameters]` on a flat, all-string Api request (`BrowseMoviesRequest`), mapped to MovieQuery.
   - Why: direct binding is impossible (recon-2 section 8).
2. **Q2, error arms (2.3a).** Ticket text: the literal `ValidationProblem` / `NotFound<ProblemDetails>` union arms. Recommended: `Ok<T>`, with 422/404 declared as metadata and produced only by the single IExceptionHandler.
   - Why: constitution V requires 422 and a single mapper with a traceId.
3. **Q8, response DTOs (2.3a).** Ticket text: the named Core success types. Recommended: scalar Api response DTOs with the frozen field/type contract in section 4.3.
   - Why: they keep the C# DTO -> OpenAPI -> TS chain faithful.
4. **Q11, OpenAPI deferral (2.3b, constitution departure).** Recommended: defer the constitution-required committed `web/src/api/openapi.json`, its Verify snapshot and the drift check for these two routes to DEV-20, the existing chain owner per DEV-307 FR-034.
   - DEV-307's approval does not cover DEV-309.
   - Until this is approved, the requirement keeps blocking.

Everything else is Patron-ruled. The forced status-code-pages, traceId and validator edits create no fifth checkbox (Q11).

## 3. Closing bar and frozen scope
- **Closing bar for Phase B:**
  - All four checkboxes answered.
  - DEV-308 implementation merged.
  - Normal Gate 1 approval.
  - Every test in section 6 is green, and every applicable gate in section 7 passes.
  - No DEV-376 acceptance claimed.
- **Frozen scope:** sections 4-6, as recommended and subject to the four owner answers plus the inherited minimum forced-fix rule (DEV-308 Q4). Anything else becomes a follow-up issue, not a finding in this round.

## 4. Grill answers (rulings: CONCLUSIONS.md Q1-Q12)

### 4.1 GET /api/movies binding (Q1, Q4, Q9)
- **Request shape.**
  - `[AsParameters] BrowseMoviesRequest` (internal, `[assumed]` name).
  - Its 14 members carry exactly the MovieQueryString keys: text, genreIds, actorIds, runtimeMin, runtimeMax, runtimeIncludeUnknown, yearMin, yearMax, statuses, inWatchlist, sort, direction, page, pageSize.
  - Each member is bound explicitly from its named key: `string?` for scalars, `string[]?` for repeated keys.
  - No member is int, bool or a SmartEnum, so browse has no framework binding-failure path.
- **The mapping keeps codec semantics (DEV-297 brief:69, :81-91).** Ordinary nullable-primitive binding is not enough, so the mapping must:
  - inspect `HttpRequest.Query` for key presence and multiplicity, since scalar binding alone loses duplicates, and reject a repeated scalar key;
  - distinguish an absent range from present empty bounds, and reject a partially present range;
  - keep empty `text` (empty string) distinct from absent `text` (null);
  - keep ordered array values;
  - parse with invariant culture;
  - require exact Enumeration names (`TryFromName`, case-sensitive).
- **Malformed supplied values.** These throw a field-keyed `ValidationException`, mapped to 422 by the existing handler. Do not collapse them into one generic error.
- **No server defaults (Q4).** A missing sort, direction, page or pageSize maps to null, null and `Page(0,0)`, then flows through the real decorator.
  - A parameter-less GET returns 422 with four field failures (MovieQueryValidator rules 1-4, recon R7).
  - Representable but invalid values (pageSize 101, inverted bounds) also go through the validator.
  - The catalog is never called with invalid input.
- **Dispatch.** BrowseMoviesQuery goes through the decorated `IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>`, and the PagedResult is mapped to the browse DTO.
- **Core is unchanged.** MovieQuery, MovieQueryString and MovieQueryValidator are not edited.

### 4.2 GET /api/movies/{id} (Q2)
- **Route and id.**
  - The route is `{id:int}`; the handler parameter is `int id`.
  - If `MovieId.TryCreate` fails (id <= 0), the endpoint throws `NotFoundException`, giving 404.
- **Not found.** The handler's `NotFoundException` (GetMovieDetailsQueryHandler.cs:14) bubbles to the existing handler.
  - The endpoint has no catch and no endpoint-built NotFound.
  - There is no second mapper.
- **Non-integer id.** A non-integer id misses the route and gets 404, with its body supplied by status-code pages (4.4).
- **Dispatch.** GetMovieDetailsQuery goes through the decorated handler, and MovieDetails is mapped to the details DTO.

### 4.3 Response contract (Q8; owner checkbox 3)
- **Records.** Internal sealed records in one `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`. The names are `[assumed]`: BrowseMoviesResponse, MovieSummaryResponse, MovieDetailsResponse, MovieMetadataResponse.
- **Frozen wire contract (camelCase; nullable means JSON null):**
  - browse `{items:[{id:int,title:string}],totalCount:int}`
  - details `{id:int,title:string,path:string,format:string,metadata:null|{title:string,synopsis:string|null,releaseYear:int|null,runtime:int|null,imdbRating:decimal|null,imdbId:string|null}}`
  - `runtime` is minutes (Runtime.Minutes). `format` is the normalized extension (MediaFormat.Extension).
- **Not added:** no converters, no ConfigureHttpJsonOptions, no Contracts folder, no generic response layer, no package and no enrichment fields.
- **Recorded note, no ticket.** No Enumeration reaches either response today (R2/R3). A later response Enumeration must map to its Name string (constitution :468-469); it would otherwise serialize as {name, value}.

### 4.4 Error bodies (Q2, Q9)
- **Status-code pages.** `app.UseStatusCodePages()` goes immediately after `UseExceptionHandler()`, before CORS and endpoint execution.
  - It fills bodyless status responses (route misses, unknown routes) through the existing IProblemDetailsService.
  - The single exception mapper is kept, and the DEV-308 service-registration order is unchanged.
- **Every error path** must return `application/problem+json` with a non-empty `traceId`. That covers 422, 404 from the handler, 404 for a non-positive id, a route miss and an unknown route.
  - If traceId is missing, the minimum shared AddProblemDetails customization in Program.cs is pre-authorized as a forced fix.
  - An unresolved body or traceId failure blocks acceptance.
- **Metadata.** `ProducesProblem(422)` on browse and `ProducesProblem(404)` on details, matching the schema actually emitted: ProblemDetails plus an `errors` extension (ValidationExceptionHandler.cs:46-55), not HttpValidationProblemDetails. Each endpoint also declares its success DTO metadata, WithName and WithSummary.
- **Limit.** This does not promise that arbitrary malformed HTTP transport requests reach endpoint validation.

### 4.5 Validators (Q3, Q10)
- **BrowseMoviesQueryValidator.** Sealed, in `src/LamuFlix.Infrastructure/Library/BrowseMoviesQueryValidator.cs`. It delegates `RuleFor(q => q.Query).SetValidator(new MovieQueryValidator())`.
- **GetMovieDetailsQueryValidator.** Sealed, in `src/LamuFlix.Infrastructure/Library/GetMovieDetailsQueryValidator.cs`. It has one rule: Id not null.
  - MovieId already enforces positivity, so there is no second positivity policy.
  - An empty validator sequence is not accepted as constitution V coverage.
- **Registration.**
  - Unconditional and explicit, in DEV-308's internal `src/LamuFlix.Api/HandlerRegistration.cs` before the AddHandler rows.
  - Copy the RabbitMq double-registration pattern (RabbitMqServiceCollectionExtensions.cs:43-45): concrete `AddScoped<T>()` plus `AddScoped<IValidator<TQuery>>(sp => sp.GetRequiredService<T>())`.
  - Not conditional on RabbitMQ.
  - No helper, no scanning, no duplicate handler or catalog row.

### 4.6 Out of scope (Q6, Q11)
- **IMovieCatalog.** Registration belongs to DEV-308 (`AddMovieCatalog`). DEV-309 consumes it once and never adds a second binding (plain AddScoped would silently swap it).
- **Facets.** GET /api/genres and /api/people stay with DEV-310.
- **OpenAPI document.** The committed `openapi.json`, its snapshot, the drift check, OpenAPI packages and the web folder are pending checkbox 4, and nothing in this ticket adds them. No handwritten TS.

## 5. Approach and files (Q12; envelope, not a line cap)
**Production**
- `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`: fill in both routes, the mapping and the metadata.
- `src/LamuFlix.Api/Endpoints/BrowseMoviesRequest.cs`: new, internal.
- `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`: new, internal sealed DTO records (checkbox 3).
- `src/LamuFlix.Infrastructure/Library/BrowseMoviesQueryValidator.cs` and `GetMovieDetailsQueryValidator.cs`: new.
- `src/LamuFlix.Api/HandlerRegistration.cs`: edit (DEV-308 file). Four scoped registrations plus the imports they need.
- `src/LamuFlix.Api/Program.cs`: edit. `UseStatusCodePages()`, plus only a necessary shared traceId fix.

**Unchanged:** Core (MovieQuery, MovieQueryString, port records), MovieQueryValidator, csproj and Directory.Packages.props, ServiceDefaults, `web/`, migrations, and any JSON options.

**Forced fixes.** Any other edit follows the inherited minimum forced-fix rule. It must cite its concrete failing AC or gate, and be named in the pickup receipt.

**Mapping logic.** Keep the request-to-MovieQuery mapping in small private helpers so it clears cyclomatic complexity 15, and 6 at the refactor stage. Fix by extracting helpers, never by suppressing.

## 6. Test strategy (Q7, Q12)
**Host integration tests** (`tests/LamuFlix.IntegrationTests`) run on the DEV-308 `WebApplicationFactory<Program>`.
- **Setup:**
  - Real Program.cs composition with the real decorated handlers.
  - `IMovieCatalog` is replaced, via ConfigureTestServices, by a recording fake. No containers: EF translation is DEV-299's.
  - Inherited in-memory config, with `Features:LocalPlay` false.
- **AC1 round trip.** An FsCheck property over the FULL existing MovieQuery generator, including empty and half-bounded ranges, with the domain not narrowed: for every q, `GET /api/movies?{MovieQueryString.Format(q)}` makes the fake receive a MovieQuery equal to q.
- **Codec edge cases:**
  - empty bound;
  - partial range, which gives 422;
  - repeated scalar key, which gives 422;
  - `text=` versus absent text;
  - exact versus wrong-case Enumeration names (wrong case gives 422);
  - malformed integer, which gives 422.
- **Error matrix.** Each case asserts the status, `application/problem+json` and a non-empty `traceId`:
  - parameter-less browse: 422, with the four field failures in `errors`;
  - unknown sort name: 422;
  - pageSize 101: 422;
  - inverted range: 422;
  - unknown id: 404;
  - `/api/movies/0`: 404;
  - `/api/movies/abc`: 404;
  - an unknown route: 404.
  - These are also the DEV-376 live error-path proof handed over by DEV-308 CONCLUSIONS:216. They do not complete DEV-376.
- **Validator registration.** The real host resolves `IValidator<BrowseMoviesQuery>` and `IValidator<GetMovieDetailsQuery>`.
- **Metadata.** From `EndpointDataSource`, for both endpoints: WithName, WithSummary, the success response type, and ProblemDetails 422 or 404.
- **Success JSON contracts.**
  - Both bodies are asserted against the section 4.3 fields and types, with populated and null metadata and null members.
  - Use inline expected JSON compared without depending on property order.
  - No Verify package. This is not the deferred OpenAPI snapshot.

**Unit tests** (`tests/LamuFlix.UnitTests`):
- BrowseMoviesQueryValidator delegates to MovieQueryValidator's rules.
- GetMovieDetailsQueryValidator rejects a null Id and accepts a valid one.

**Conventions.**
- Use the existing assertion, substitute and FsCheck libraries, and TheoryData/MemberData.
- No explanatory comments (the DEV-308 project rule).
- No new package.

## 7. Gate expectations (inherited DEV-308 Q8)
- **Governed by** `harness.yml` and each script's help. No invented flags.
- **Gates that apply:**
  - the three static-analysis gates on the changed .cs set (Roslyn, cyclomatic complexity <= 15, InspectCode), and the refactor-stage threshold of 6;
  - format, full tests, property tests and vulnerable packages;
  - mutation at its stage.
- **Outcome rules:**
  - Exit 1 or "could not run" blocks.
  - A SKIP for an empty scope blocks.
  - A surviving mutant is a missing test.
  - Findings are fixed by shape, never by suppression.

## 8. Prerequisites, size, loop terms
- **Pickup prerequisites (receipt):**
  - DEV-308 implementation merged: LibraryEndpoints.cs, HandlerRegistration.cs, AddMovieCatalog and the WAF harness are on the build base.
  - The four owner checkboxes answered.
  - Gate 1 approved.
  - The drift check against main done.
  - A spec PR merge alone does not supply dependency code.
- **Size and loop terms.**
  - Size M, with three axis reports (Sentry, Ledger, Compass) at plan challenge and review. Rigger reconciles if the live tag differs.
  - The standing DEV-307 CONCLUSIONS:45 terms apply: Critical/High findings with a concrete failure scenario block, at most two rounds, at most two fix commits per round, out-of-scope items become follow-ups, and never merge.

## 9. Pending receipts (non-blocking)
- **DEV-20 comment.** Rigger posts one ownership comment on DEV-20 (Patron Q11, text in TEMP `DEV-309-DEV-20-comment.md`). It names both routes and the pending deferral checkbox. It is not treated as recorded until Rigger reports it verified. It does not close checkbox 4 or complete DEV-20/DEV-320.
- **Pickup receipt (Phase B).** The section 8 prerequisites.
