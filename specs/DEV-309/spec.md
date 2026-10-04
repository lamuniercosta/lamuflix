# Feature Specification: Browse and retrieve movies through the Api

**Feature Branch**: `feature/309-spec`

**Created**: 2026-10-02

**Status**: the four owner decisions Q1, Q2, Q8 and Q11 are decided and approved in merged spec PR #81. Phase B also requires the DEV-308 implementation to be merged and normal Gate 1 approval (`PR #81`; `brief.md` §§2-3, 8; `CONCLUSIONS.md` Q1, Q2, Q8, Q11).

**Input**: DEV-309 — implement `GET /api/movies` and `GET /api/movies/{id}` in `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`. Ticket Scope 1 binds query string directly to `MovieQuery` via `[AsParameters]`, dispatches `BrowseMoviesQuery`, and returns `Results<Ok<PagedResult<MovieSummary>>, ValidationProblem>`. Scope 2 parses an `int` id as `MovieId`, dispatches `GetMovieDetailsQuery`, and returns `Results<Ok<MovieDetails>, NotFound<ProblemDetails>>`. Scope 3 requires `WithName`, `Produces` and `WithSummary`. AC1 requires query binding to `MovieQuery`; AC2 requires strongly typed TypedResults with OpenAPI metadata. The settled brief is `specs/DEV-309/brief.md`; cited Patron rulings are in `CONCLUSIONS.md`, and taste choices are in `ASSUMPTIONS.md`.

**Short name**: `movie-browse-details-api`

## Owner Decisions — Decided in Merged Spec PR #81

PR #81 records the owner approval of each decision below; these are decided, not open checkboxes (`PR #81`).

- **Q1 — Binding (2.3a): Approved.** Use a flat, all-string Api `[AsParameters]` request mapped to `MovieQuery` instead of the ticket's direct `[AsParameters] MovieQuery` binding. Direct binding is not supported for the nested/domain shapes and does not preserve the established query codec (`PR #81`; `CONCLUSIONS.md` Q1, Q9; recon-DEV-309-2 §§8-9, R7).
- **Q2 — Error arms (2.3a): Approved.** Use `Ok<T>` success results with 422/404 error metadata; errors are produced only by the single `IExceptionHandler`, instead of the ticket's literal `ValidationProblem`/`NotFound<ProblemDetails>` union arms. The exception handler owns 422 and 404 and keeps the `traceId` problem response contract (`PR #81`; `CONCLUSIONS.md` Q2, Q9; recon R5).
- **Q8 — Response DTOs (2.3a): Approved.** Use scalar Api response DTOs instead of the ticket's named Core success types, with the frozen field/type contract in FR-009. This exposes stable scalar values without converter or serializer-specific schema transformations (`PR #81`; `CONCLUSIONS.md` Q8; recon R2, R3).
- **Q11 — OpenAPI deferral (2.3b): Approved.** Defer the committed OpenAPI document, Verify snapshot, and drift check for these two routes to DEV-20, which owns the existing document chain (`PR #81`; `CONCLUSIONS.md` Q11; recon R4; DEV-307 FR-034).

No other grill question is open. Forced status-code-pages/traceId and validator work does not create a fifth checkbox (`CONCLUSIONS.md` Q9, Q10).

## User Scenarios & Testing

### User Story 1 - A caller browses movies using the established query codec (Priority: P1) 🎯 MVP

As a caller of the movie library, I need `GET /api/movies` to turn the established flat query-string representation into a validated `MovieQuery` and dispatch the browse handler, so filters and pagination behave consistently with the existing Core codec.

**Why this priority**: Browse is the first ticket endpoint and AC1 directly requires the query to bind correctly. The Core query includes nested ranges, smart enums, immutable arrays and a page record; ordinary property binding cannot construct it or preserve the codec's key semantics (recon-DEV-309-2 §§8-9, R7).

**Independent Test**: Through the real DEV-308 `WebApplicationFactory<Program>`, issue requests using `MovieQueryString.Format` for the full existing validator-accepted generator and assert the recording `IMovieCatalog` receives an equal `MovieQuery`. Exercise codec edge cases and assert malformed values return the mapped 422 ProblemDetails response.

**Acceptance Scenarios**:

1. **Given** a flat query request, **When** it is bound with `[AsParameters]` and mapped, **Then** the dispatched `BrowseMoviesQuery` contains a `MovieQuery` equal to the value encoded by the established `MovieQueryString` codec.
2. **Given** a scalar key repeated in the raw query, **When** the request is mapped, **Then** it fails as a field-keyed validation error; duplicate information is checked in `HttpRequest.Query`, not inferred from a scalar-bound property.
3. **Given** range keys, **When** a range is absent, fully supplied, empty-bounded, or partially supplied, **Then** absence remains distinct from present empty bounds, the codec's range rules are preserved, and a partial range fails validation.
4. **Given** `text=` and an absent `text` key, **When** mapped, **Then** empty string and null remain distinct.
5. **Given** repeated array keys, **When** mapped, **Then** values retain request order and produce the same ordered arrays as the codec.
6. **Given** integer or Enumeration query values, **When** parsed, **Then** integer parsing is invariant-culture and Enumeration names require an exact case-sensitive match; malformed or wrong-case values produce field-keyed validation failures.
7. **Given** missing `sort`, `direction`, `page` or `pageSize`, **When** browse dispatches, **Then** no server defaults are substituted: null sort/direction and `Page(0,0)` reach the real validator and produce four field failures; the catalog is not invoked.
8. **Given** syntactically representable but invalid values such as `pageSize=101` or inverted bounds, **When** browse dispatches, **Then** they reach the decorated validator and produce 422 rather than bypassing validation.
9. **Given** a valid query, **When** the handler returns a page, **Then** the endpoint maps it to the frozen browse response DTO and returns a typed 200 result.

### User Story 2 - A caller retrieves one movie by id (Priority: P1)

As a caller, I need `GET /api/movies/{id}` to parse a positive integer as `MovieId`, dispatch the existing details query, and receive a predictable details response or problem document.

**Why this priority**: The details handler already owns the unknown-movie exception path. The endpoint must use that path rather than add a competing endpoint-level mapper (`CONCLUSIONS.md` Q2; recon-DEV-309 §6).

**Independent Test**: Through the real host, assert the recording catalog receives the requested `MovieId` for a known movie, the mapped scalar DTO conforms to FR-009, and unknown/non-positive/non-integer identifiers have the specified 404 response and body.

**Acceptance Scenarios**:

1. **Given** a positive integer id, **When** `GET /api/movies/{id}` runs, **Then** it creates `MovieId`, dispatches `GetMovieDetailsQuery` through the decorated handler, maps the returned detail to the frozen DTO, and returns typed 200.
2. **Given** an unknown positive id, **When** the handler throws its existing `NotFoundException`, **Then** it bubbles to the single `IExceptionHandler` and returns a 404 ProblemDetails response; the endpoint does not catch or serialize NotFound itself.
3. **Given** a non-positive integer, **When** `MovieId.TryCreate` fails, **Then** the endpoint throws `NotFoundException` and the existing mapper returns 404.
4. **Given** a non-integer route segment, **When** routing evaluates `{id:int}`, **Then** the route misses and status-code pages supply the 404 ProblemDetails response.

### User Story 3 - API errors share the existing problem response contract (Priority: P1)

As a caller and operator, I need validation, not-found, and bodyless routing errors to produce a consistent ProblemDetails body with a trace id, so that clients can diagnose failures without parallel error-mapping logic.

**Why this priority**: The constitution requires 422 validation and one exception mapper, while route misses do not reach that mapper. The minimum status-code-pages middleware closes that path with the existing `IProblemDetailsService` (`CONCLUSIONS.md` Q2, Q9; recon R5).

**Independent Test**: Exercise mapped validation and not-found exceptions plus non-integer route and unknown-route misses. Assert status, `application/problem+json`, and a non-empty `traceId` for each error response.

**Acceptance Scenarios**:

1. **Given** a `ValidationException`, **When** the single `IExceptionHandler` processes it, **Then** the response is 422 with ProblemDetails and field-keyed `errors` extension.
2. **Given** a `NotFoundException`, **When** the same handler processes it, **Then** the response is 404 ProblemDetails.
3. **Given** a bodyless status from route matching or an unknown route, **When** status-code pages runs, **Then** it uses the existing problem-details service to produce a body and non-empty trace id.
4. **Given** either endpoint's metadata, **When** inspected, **Then** browse declares `ProducesProblem(422)` and details declares `ProducesProblem(404)` matching the response actually emitted.
5. **Given** the middleware pipeline, **When** its order is inspected, **Then** `UseStatusCodePages()` immediately follows `UseExceptionHandler()` and precedes CORS and endpoint execution; there remains exactly one exception mapper.
6. **Given** the trace id is absent from a problem response, **When** the implementation is completed, **Then** the minimum shared `AddProblemDetails` customization needed to restore it is included; unresolved body or trace-id behavior blocks acceptance.

### User Story 4 - The endpoints publish a frozen scalar response contract (Priority: P1)

As a client author, I need successful browse and details responses to expose stable scalar JSON fields, so generated clients can represent ids, paths, formats and metadata without leaking Core value-object wrappers.

**Why this priority**: The first endpoint response starts the public contract chain. The Core response graph serializes value objects as nested objects by default; the proposed internal Api DTOs provide the frozen scalar shape and preserve nulls (`CONCLUSIONS.md` Q8; recon R2/R3).

**Independent Test**: Assert both HTTP success bodies against expected JSON field names and types, including populated and null metadata and null metadata members; comparisons do not depend on JSON property order.

**Acceptance Scenarios**:

1. **Given** a browse response, **When** serialized, **Then** it has `{items:[{id:int,title:string}],totalCount:int}` with camelCase field names.
2. **Given** a details response, **When** serialized, **Then** it has `{id:int,title:string,path:string,format:string,metadata:null|{title:string,synopsis:string|null,releaseYear:int|null,runtime:int|null,imdbRating:decimal|null,imdbId:string|null}}` with camelCase field names.
3. **Given** a non-null `MovieMetadata`, **When** it is mapped, **Then** `releaseYear` is the scalar year, `runtime` is minutes and `imdbRating` is decimal; `format` is the normalized extension and `path` is the scalar path string.
4. **Given** absent metadata or nullable members, **When** serialized, **Then** JSON contains null values, not omitted fields or nested value objects.
5. **Given** either endpoint's conventions, **When** endpoint metadata is inspected, **Then** it includes `WithName`, `WithSummary`, the correct success DTO response type, and the matching problem metadata.

### User Story 5 - The contract handoff is explicit and scoped (Priority: P2)

As a maintainer, I need the OpenAPI work and generated client chain assigned to its existing owners, with the four owner decisions recorded, so no one mistakes metadata annotations for a committed contract document.

**Why this priority**: The C# DTO and route metadata provide inputs to DEV-20, but neither DEV-307 nor DEV-308 supplies document generation. Merged spec PR #81 approves the Q11 deferral (`PR #81`; `CONCLUSIONS.md` Q11; recon R4).

**Acceptance Scenarios**:

1. **Given** the DEV-20 ownership handoff, **When** this ticket is reviewed, **Then** both movie routes, DTO fields and endpoint metadata are identified as inputs, and the Q11 deferral is represented as approved by merged spec PR #81 (`PR #81`).
2. **Given** the DEV-309 diff, **When** it is checked, **Then** it contains no OpenAPI package or document generation, no committed `web/src/api/openapi.json`, no Verify snapshot, no drift check, no web folder work and no handwritten TypeScript.
3. **Given** the implementation prerequisites, **When** Phase B starts, **Then** DEV-308 implementation is merged, the four owner decisions recorded in merged spec PR #81 are present in the spec, and Gate 1 is approved (`PR #81`).
4. **Given** test and gate reports, **When** this ticket is closed, **Then** no DEV-376 acceptance is claimed; its live-error proof remains a handoff only.

### Edge Cases

- The `[AsParameters]` record contains only nullable strings and string arrays, explicitly bound from the established flat query keys. It does not bind `MovieQuery` itself.
- Scalar binding loses duplicate-key information; the mapper inspects raw `HttpRequest.Query` multiplicity and rejects repeated scalar keys.
- Empty range values and absent range keys have different codec meaning; partial ranges fail rather than becoming open ranges.
- Missing required paging/sort keys remain invalid and are rejected by the decorated query validator; there are no endpoint defaults.
- Query strings with malformed transport encoding are outside the promise that arbitrary transport failures reach endpoint validation; application parse failures are field-keyed 422 errors.
- `{id:int}` misses non-integer identifiers before endpoint code; status-code pages must supply its ProblemDetails body.
- Details success mapping uses `Runtime.Minutes`, not a nonexistent generic `Value` member.
- A missing `traceId`, problem body, or correct content type is an acceptance failure, not an unclaimed edge case.
- No response Enumeration is reachable today. If a later DTO adds one, map it to its `Name` string as required by the constitution; do not introduce a converter framework here.

## Requirements

### Functional Requirements

- **FR-001** (Ticket Scope 1, Q1, US1): The browse route is `GET /api/movies` in the existing `MapLibraryEndpoints` mapper. Its request parameter is a flat Api request record bound with `[AsParameters]`; the recommended internal name is `BrowseMoviesRequest` (`ASSUMPTIONS.md`).
- **FR-002** (Q1, Q9, US1): `BrowseMoviesRequest` contains the 14 established keys: `text`, `genreIds`, `actorIds`, `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax`, `statuses`, `inWatchlist`, `sort`, `direction`, `page`, `pageSize`. Scalars are `string?`; repeated keys are `string[]?`. Binding uses explicit key names. This is an owner-pending replacement for direct `MovieQuery` binding.
- **FR-003** (Q1, US1): Mapping reads raw query presence and multiplicity, rejects repeated scalar keys, distinguishes absent and empty values, rejects partial ranges, preserves array order and empty text, parses invariantly, and requires exact case-sensitive Enumeration names. Malformed supplied values raise field-keyed `ValidationException`.
- **FR-004** (Q4, Q9, US1): No server defaults are substituted. Missing sort/direction/page/pageSize map to null/null/`Page(0,0)` and flow through the decorated validator. Representable invalid query values also reach validation; the catalog is not called on invalid input.
- **FR-005** (Q3, Q10, US1): A sealed `BrowseMoviesQueryValidator` in Infrastructure/Library delegates to unchanged `MovieQueryValidator`. A sealed `GetMovieDetailsQueryValidator` in the same folder rejects a missing `Id`; positive-value validity is owned by `MovieId` construction.
- **FR-006** (Q10, US1/US2): Both validators are explicitly registered unconditionally in DEV-308's internal Api `HandlerRegistration.cs`, concrete scoped type plus scoped `IValidator<TQuery>` factory resolving that concrete instance, before the corresponding `AddHandler` rows. No scan/helper/duplicate handler or catalog registration is added.
- **FR-007** (Ticket Scope 2, Q2, US2): The details route is `GET /api/movies/{id:int}` and takes an `int`, constructs `MovieId` with `TryCreate`, and dispatches `GetMovieDetailsQuery` through the existing decorated handler. A non-positive id throws `NotFoundException`; an unknown id's existing handler exception bubbles. No endpoint catch or second mapper is used.
- **FR-008** (Q2, Q9, US2/US3): `UseStatusCodePages()` is added immediately after `UseExceptionHandler()`, before CORS and endpoint execution, preserving inherited registration order. It uses the existing `IProblemDetailsService` for bodyless routing/status errors. The sole `IExceptionHandler` maps validation to 422 and not-found to 404.
- **FR-009** (Q8, US1/US2/US4): As approved in merged spec PR #81, use internal sealed Api response records in `Endpoints/LibraryResponses.cs`: `BrowseMoviesResponse` (`items`, `totalCount`), `MovieSummaryResponse` (`id`, `title`), `MovieDetailsResponse` (`id`, `title`, `path`, `format`, nullable `metadata`), and `MovieMetadataResponse` (`title`, nullable `synopsis`, nullable scalar `releaseYear`, nullable `runtime` minutes, nullable decimal `imdbRating`, nullable string `imdbId`). JSON is camelCase, and null fields remain present as JSON null. Frozen wire shapes: browse `{items:[{id:int,title:string}],totalCount:int}`; details `{id:int,title:string,path:string,format:string,metadata:null|{title:string,synopsis:string|null,releaseYear:int|null,runtime:int|null,imdbRating:decimal|null,imdbId:string|null}}` (`PR #81`).
- **FR-010** (Ticket Scope 3, Q2, Q8, Q9): Both endpoints use `WithName`, `WithSummary`, and success response metadata. Browse declares `ProducesProblem(422)` for the actual ProblemDetails plus `errors` extension; details declares `ProducesProblem(404)`. Success metadata describes the scalar response DTO contract approved in merged spec PR #81 (`PR #81`).
- **FR-011** (Q11, US5): As approved in merged spec PR #81, OpenAPI document generation, the committed `web/src/api/openapi.json`, its Verify snapshot, drift check, OpenAPI packages and TypeScript work are deferred to DEV-20. The constitution departure is owner-approved; no document or generated types are produced in this ticket (`PR #81`).
- **FR-012** (Q6, Q12): DEV-309 consumes the single `IMovieCatalog` registration and handlers supplied by DEV-308. It does not add a second catalog binding or handler row. Genre and people facets remain DEV-310 scope.
- **FR-013** (Q7, Q12): Integration tests use the real DEV-308 Api host and decorated handlers, replacing only `IMovieCatalog` with a recording fake. Browse binding round-trip uses the complete existing validator-accepted `MovieQuery` generator; codec edge cases and errors, validator registration, metadata and response JSON are asserted. No container is used for this HTTP binding seam.
- **FR-014** (Q7, Q12): Unit tests cover browse-validator delegation and details-validator null-Id rejection/valid acceptance. Existing assertion, substitute and FsCheck libraries are used; no package is added.
- **FR-015** (Q2, Q9): Error acceptance covers parameter-less browse (four field failures), invalid enumeration, `pageSize=101`, inverted range, unknown id, non-positive id, non-integer id, and unknown route. Each asserts status, `application/problem+json`, and non-empty `traceId`; no DEV-376 acceptance is claimed.
- **FR-016** (Q12): Scope remains limited to the two movie routes, their binding/response records, their validators/registrations, minimum problem-body middleware wiring, and the tests needed to prove them. No Core model rewrite, converter, JSON options framework, dependency, package, migration, facet route, OpenAPI document or hand-authored TS is added.

### Key Entities

- **BrowseMoviesRequest**: Flat, string-only Api-bound representation of canonical movie query-string keys; translated to the unchanged Core `MovieQuery`.
- **MovieQuery**: Existing Core query with filters, sorting and page; validated through the existing decorated handler pipeline.
- **MovieDetails**: Existing handler response, projected to a scalar Api DTO before serialization.
- **ProblemDetails**: Shared exception and bodyless status error representation, carrying a non-empty `traceId`.
- **Movie response DTOs**: Internal Api records defining the Q8-approved frozen camelCase scalar response shapes (`PR #81`).

## Success Criteria

### Measurable Outcomes

- **SC-001**: For every value in the full existing validator-accepted query generator, formatting then requesting browse dispatches an equal `MovieQuery` to the recording catalog.
- **SC-002**: Every specified codec-invalid or validator-invalid browse request returns 422 ProblemDetails with field-keyed errors and non-empty trace id; the catalog is not called.
- **SC-003**: Known details requests return the frozen scalar JSON contract; unknown and invalid ids follow the specified 404 error contract.
- **SC-004**: All error matrix paths return `application/problem+json` with non-empty `traceId`.
- **SC-005**: Endpoint metadata includes both names and summaries, success DTO types and matching ProblemDetails status metadata.
- **SC-006**: The DEV-309 diff stays within the approved file envelope and contains no OpenAPI/web/generated-client work; the four owner decisions remain recorded and attributable to merged spec PR #81 (`PR #81`).

## Assumptions

- Taste-only names `BrowseMoviesRequest`, `LibraryResponses.cs` and the four DTO record names are `[assumed]` as recorded in `ASSUMPTIONS.md`; they do not alter the owner-approved decisions in merged spec PR #81 (`PR #81`).
- CamelCase follows the existing ASP.NET Core JSON convention; null members are emitted as null.
- Ticket and brief are the scope source. The four owner decisions were approved in merged spec PR #81; normal Gate 1 approval and the DEV-308 implementation prerequisite still apply (`PR #81`).
