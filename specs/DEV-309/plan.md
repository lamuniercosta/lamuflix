# Implementation Plan: Browse and retrieve movies through the Api

## Summary

Implement the two movie endpoints in DEV-308's existing `MapLibraryEndpoints` mapper. Browse binds a flat, all-string `[AsParameters]` Api record and maps the canonical query-string keys to the unchanged `MovieQuery`; both requests dispatch through the inherited decorated handlers. A single inherited `IExceptionHandler` produces 422 validation and 404 not-found responses. The minimum status-code-pages middleware fills bodyless routing errors. Internal Api response DTOs provide the frozen scalar JSON contract, subject to the still-open Q8 owner decision. OpenAPI document generation, snapshot and drift enforcement are deferred to DEV-20 only if the still-open Q11 checkbox is approved.

Four owner questions (Q1, Q2, Q8 and Q11) remain unresolved. This plan describes the recommended design from `brief.md`; it does not amend ticket text or authorize Phase B. Phase B requires DEV-308 implementation merged, all four answers, and Gate 1 approval.

## Technical Context

- **Language/runtime**: C# / .NET 10, ASP.NET Core Minimal APIs.
- **Host**: `src/LamuFlix.Api`, composed and tested through DEV-308's `WebApplicationFactory<Program>`.
- **Existing route mapper**: `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`, created empty by DEV-308; no `Program.cs` route registration is needed.
- **Existing dispatch**: `IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>` and `IQueryHandler<GetMovieDetailsQuery, MovieDetails>` registered through DEV-308's `AddHandler` composition.
- **Catalog**: `IMovieCatalog` registration is DEV-308 ownership; DEV-309 consumes it once.
- **Validation**: FluentValidation decorator exists; `MovieQueryValidator` exists but is not registered and accepts a `MovieQuery`, not the handler query. New wrapper validators are required for both handler requests.
- **Error handling**: `ValidationExceptionHandler` is the one existing `IExceptionHandler`; it maps validation to 422 and `NotFoundException` to 404 through `IProblemDetailsService`. Bodyless framework errors require `UseStatusCodePages()`.
- **Tests**: Existing xUnit, Shouldly, NSubstitute and FsCheck conventions; real host with an `IMovieCatalog` recording fake. No new package or database container for this HTTP seam.
- **OpenAPI**: route metadata is supplied by `WithName`, `WithSummary`, success metadata and `ProducesProblem`; committed document/snapshot/drift work remains pending Q11 and is assigned to DEV-20.
- **Constraints**: preserve Core query types and codec semantics; no hand-written TypeScript, converters, serializer-wide JSON configuration, project/layer/dependency additions or changes to local-play behavior.

## Constitution Check

- **Query validation through the decorator**: PASS by plan. Add explicit `IValidator<TQuery>` registrations and concrete validators before endpoint dispatch. Validation failures are mapped only by the single exception handler.
- **HTTP error contract**: PASS subject to verifying the host. Require 422/404 and all routing errors to carry `application/problem+json` and non-empty `traceId`; include only the minimum shared `AddProblemDetails` customization if the existing service omits the trace id. Keep the exception handler before status-code pages.
- **No duplicate mapping**: PASS by plan. Endpoints throw/bubble exceptions; there is no catch or local ProblemDetails serialization.
- **Contract chain**: OPEN. Q8's DTO substitution and Q11's OpenAPI deferral are owner choices, not settled by this plan. Keep the frozen wire proposal explicit and do not implement until Gate 1 approves all four owner checkboxes.
- **No new dependency/layer/schema change**: PASS by plan. Use existing folders, packages and API composition.
- **Features:LocalPlay/secrets**: no change. No credentials or secrets are introduced.
- **Gate 1**: CLOSED pending owner answers and normal approval. No implementation or gate result is claimed.

## Project Structure

```text
specs/DEV-309/
├── brief.md
├── CONCLUSIONS.md                  (Patron-owned; unchanged)
├── ASSUMPTIONS.md                  (Patron-owned; unchanged)
├── spec.md
├── plan.md
└── tasks.md
src/LamuFlix.Api/
├── Endpoints/
│   ├── LibraryEndpoints.cs         (fill existing mapper)
│   ├── BrowseMoviesRequest.cs     (new flat request)
│   └── LibraryResponses.cs        (new response records, pending Q8)
├── HandlerRegistration.cs          (explicit validator registrations)
└── Program.cs                      (status-code pages; minimum traceId fix only if needed)
src/LamuFlix.Infrastructure/Library/
├── BrowseMoviesQueryValidator.cs  (new wrapper validator)
└── GetMovieDetailsQueryValidator.cs (new handler-query validator)
tests/LamuFlix.IntegrationTests/
└── LibraryEndpointsTests.cs        (host/request/metadata/contract coverage)
tests/LamuFlix.UnitTests/
└── LibraryQueryValidatorTests.cs  (focused validator tests)
```

This is the recommended file envelope, subject to the four owner answers and the inherited minimum forced-fix rule. Do not edit Core query models, `MovieQueryValidator`, catalog registration, project files, central package pins, migrations, web files, OpenAPI documents, or generated TS. If an additional edit proves necessary to satisfy a concrete acceptance criterion or gate, name its failing evidence and keep the fix minimal.

## Design

### 1. Browse request binding and codec mapping

Use one internal flat record in the existing Api `Endpoints` folder, named `BrowseMoviesRequest` as a taste assumption. It exposes the 14 canonical keys with nullable string scalars and nullable string arrays for repeated keys, and uses explicit query names with `[AsParameters]`. It does not expose typed integers, booleans, smart enums, immutable arrays, nested ranges or `Page` for framework parsing.

Keep conversion to `MovieQuery` in private focused helpers. Inspect raw `HttpRequest.Query` so scalar duplicates, absent versus present-empty values, and exact repeated-key order are preserved. Reject repeated scalar keys and partial ranges. Parse numeric/boolean values invariantly and SmartEnum names exactly/case-sensitively. Convert malformed values to field-keyed `ValidationException` entries handled by the existing mapper. Do not reduce failures to one generic field.

No server defaults are applied. Missing sort/direction/page/pageSize become null/null/`Page(0,0)` and proceed through the decorated handler. The existing validator returns the four missing-field failures; no invalid query reaches the catalog. Representable invalid inputs, including size 101 and inverted ranges, also proceed to that validator. Preserve the complete validator-accepted FsCheck generator in the Format-to-request-to-dispatched-query property.

### 2. Endpoint dispatch and exception flow

Add both routes inside the existing `MapLibraryEndpoints` mapping. Browse creates `BrowseMoviesQuery` from the mapped `MovieQuery` and dispatches `IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>`. Details uses `{id:int}`, calls `MovieId.TryCreate`, and dispatches `GetMovieDetailsQuery` to the existing handler.

For invalid non-positive ids, throw `NotFoundException`; let unknown-id `NotFoundException` from the handler bubble. No endpoint catches or serializes errors. Keep the single `ValidationExceptionHandler` as the only exception mapper.

Apply `app.UseStatusCodePages()` immediately after `app.UseExceptionHandler()` and before CORS/endpoints. It supplies ProblemDetails for route misses and unknown routes via the already-registered `IProblemDetailsService`. Preserve existing service-registration order. If host evidence shows no non-empty `traceId`, add only the minimum shared problem-details customization and verify all error paths again.

### 3. Validators and registration

Create sealed `BrowseMoviesQueryValidator` in Infrastructure/Library with `RuleFor(q => q.Query).SetValidator(new MovieQueryValidator())`. Do not edit the existing validator. Create sealed `GetMovieDetailsQueryValidator` in the same folder with an Id-not-null rule; `MovieId` construction owns positivity, while the raw non-positive endpoint path is 404 before the query is created.

Register each concrete validator scoped and expose `IValidator<TQuery>` with a scoped factory resolving the same concrete scoped instance. Add the four registrations explicitly to the existing internal `HandlerRegistration.cs` before the associated `AddHandler` rows. Registrations are unconditional, not tied to RabbitMQ. Add no helper, scan, duplicate handler or catalog row.

### 4. Scalar response contract and endpoint metadata

Subject to Q8 approval, create four internal sealed records in `LibraryResponses.cs` and map Core response values explicitly. Freeze these camelCase JSON contracts:

- Browse: `{items:[{id:int,title:string}],totalCount:int}`.
- Details: `{id:int,title:string,path:string,format:string,metadata:null|{title:string,synopsis:string|null,releaseYear:int|null,runtime:int|null,imdbRating:decimal|null,imdbId:string|null}}`.

Map value objects to their scalar members: `MovieId.Value`, `LibraryPath.Value`, `MediaFormat.Extension`, `ReleaseYear.Value`, `Runtime.Minutes`, `ImdbRating.Value`, and `ImdbId.Value`. Keep nullable metadata and nullable metadata fields present as JSON null. Do not add converters, a Contracts folder, generic response abstraction, serializer options, enrichment fields or a package. If Q8 is rejected, stop and reconcile the chosen response contract before implementation.

Both routes declare `WithName` and `WithSummary`, success DTO metadata, and their actual problem metadata: `ProducesProblem(422)` for browse and `ProducesProblem(404)` for details. The 422 body is ProblemDetails with an `errors` extension, not `HttpValidationProblemDetails`.

### 5. OpenAPI ownership boundary

No DEV-307 or DEV-308 document generation is inherited. Q11 proposes deferring the committed `web/src/api/openapi.json`, Verify snapshot and drift check for these routes to DEV-20. Until the owner answers Q11, that is an open constitutional departure and Gate 1 remains closed. If approved, DEV-309 adds metadata that DEV-20 consumes but adds no OpenAPI package, generation wiring, web folder, snapshot or hand-authored/generated TypeScript.

### 6. Host tests and observable contract

Use DEV-308's real factory, production registrations, decorated handlers and the recording `IMovieCatalog` fake. Supply inherited transient host configuration with `Features:LocalPlay` false. Assert the host starts; do not replace the handlers or validator pipeline.

Integration evidence includes:

- A Format-to-dispatched-query FsCheck property over the full existing accepted generator.
- Codec edges: empty bounds, partial ranges, duplicate scalars, empty text versus absent text, key ordering for arrays, exact/wrong-case Enumeration names and malformed numbers.
- Validator behavior for missing required query fields, page size 101 and inverted ranges; assert invalid browse never calls the catalog.
- The error matrix: parameter-less browse (422, four field failures), invalid Enumeration, size 101, inverted range, unknown positive id, id zero, non-integer id, and unknown route. Assert status, `application/problem+json` and non-empty `traceId` each time.
- Real host resolution of both validators and endpoint metadata from `EndpointDataSource`: names, summaries, success response types and 422/404 problem metadata.
- Browse and detail success JSON with populated and null metadata/member values, compared without property-order dependence.

Unit evidence covers browse delegation to `MovieQueryValidator` and details Id-not-null behavior. No Verify package or OpenAPI snapshot is used for inline JSON contract assertions. No PostgreSQL container is needed because the test seam is HTTP binding/dispatch and the catalog is replaced; query translation is not under test.

### 7. Boundary checks

Before implementation, confirm the DEV-308 prerequisite and accepted owner answers. At close-out, review `git diff <base>...HEAD` against the approved file envelope. Confirm no duplicate `IMovieCatalog`/handler registrations, no Core changes, no dependency/project/migration edits, no local-play changes, no secrets, and no OpenAPI/web/generated TS work. Report any forced extra fix with its concrete failed AC or gate. Keep DEV-376 acceptance explicitly unclaimed.

## 10. Test Strategy

Use the repo's existing xUnit/Shouldly/NSubstitute/FsCheck setup. Integration tests exercise the real HTTP pipeline and decorated handlers with only the catalog replaced. Unit tests exercise the two wrapper validators. Assertions prove exact field values, status/body/content type/trace id and metadata, not merely that a response exists. Do not add packages or containers for this seam.

## 11. Gates

At the implementation stage, consult `harness.yml` and each script's `-Help`; use actual applicable scope and do not invent flags or thresholds. Run changed-file static analysis (Roslyn, cyclomatic complexity, InspectCode), format verification, full tests, property tests and vulnerable-package analysis; mutation at its proper pipeline stage. A surviving mutant is a missing test. Exit 1 or a gate that could not run blocks. Exit 2 for empty scope is blocking `SKIPPED`; only a harness-disabled gate is non-blocking `SKIP`. Do not lower thresholds or claim a gate passed unless it ran successfully.

### Close-out

Record each applicable result with its exit code and honest outcome. Verify the exact diff and owner-checkbox state, confirm no OpenAPI/TypeScript artifacts were added under the proposed deferral, and hand the PR body merge-bar proof. No merge is performed by this ticket author.

## 12. Coverage of the Ticket's Closing Bar

- Two requested GET routes and ticket metadata are covered by US1/US2 and FR-007/FR-010.
- Query binding, codec fidelity and invalid-input validation are covered by US1 and FR-001 through FR-006.
- Success and error contracts are covered by US2 through US4 and FR-008 through FR-015.
- OpenAPI ownership and pending deferral are covered by US5 and FR-011; Q11 remains unchecked until its owner answer.
- Dependencies, Gate 1 and DEV-308 implementation prerequisites are explicit in the spec and tasks; no Phase B authorization is implied.

## 13. Complexity Tracking

The request mapper must remain below the implementation cyclomatic-complexity ceiling of 15 and the refactor ceiling of 6. Extract focused parsing/mapping helpers to meet those limits; do not suppress findings, move thresholds or collapse field-specific failures into generic errors. Confirm actual thresholds in `harness.yml` and the analyzer help at the applicable stage.
