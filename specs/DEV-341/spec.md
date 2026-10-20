# Feature Specification: DEV-341 GET /api/movies/{id}/recommendations with OpenAPI

**Feature Branch**: `feature/341-spec`

**Created**: 2026-10-20

**Status**: Draft

**Input**: Ticket DEV-341 (size M, parent DEV-288, ui:none); settled grill brief `specs/DEV-341/brief.md` (grill closed Q9/12); facts from notes `recon-DEV-341` and `recon-DEV-341-2`; Patron rulings in `specs/DEV-341/CONCLUSIONS.md`; taste receipt `specs/DEV-341/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md)*

These terms are already present in root `CONTEXT.md` (recon-DEV-341:72). No new glossary term is added by this ticket (brief Q8). Published Spec Kit artifacts must use them as written:

- **Recommendation**: A suggested library movie from the details page. _Avoid_: search result, related-title guess without score/reasons.
- **Reason**: A caller-safe explanation attached to a recommendation. _Avoid_: internal identifiers, scorer debug strings rewritten by this ticket.

`RecommendedMovieDto` is an API wire representation of a **Recommendation**, not a new domain term (brief Q8). This ticket consumes DEV-340 `RecommendedMovie` / `GetRecommendationsQuery` and DEV-338 `Reason.Text` unchanged (brief Q1).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Return ranked recommendations with reason strings (Priority: P1)

A caller requests recommendations for a library movie that is present and not Pending. After take/id validation and selected-movie preflight, the endpoint dispatches DEV-340 `GetRecommendationsQuery` and returns HTTP 200 with a JSON array of flat `RecommendedMovieDto` objects preserving handler rank order, scores, and DEV-338 reason texts as strings.

**Why this priority**: Ticket acceptance requires ranked recommendations with reasons in JSON.

**Independent Test**: `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` (WebApplicationFactory; substituted recommendation handler; existing-port repository fake) plus `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` (real Postgres `ApiEndToEndCollection`) prove exact twelve-field camelCase payload, EnrichmentStatus string enum, nullable presentation values, preserved input rank order, Score and Reason.Text passthrough (positive-score nonempty, zero-score nonempty underflow, zero-score empty), Enriched/Failed/NotFound enrichment-state dispatch, cancellation/error propagation, and seeded production route/DI/handler ranked JSON (brief Q3–Q5, Q7).

**Acceptance Scenarios**:

1. **Given** a present selected movie in Enriched, Failed, or NotFound enrichment state and a substituted handler returning ranked `RecommendedMovie` values, **When** `GET /api/movies/{id}/recommendations` runs with a valid take (or omitted take defaulting to 10), **Then** the response is 200 with a JSON array whose length, order, scores, and `reasons` string arrays match the handler output after flat Summary field mapping.
2. **Given** handler results that include zero-score items with empty and nonempty reason lists and positive-score nonempty reasons, **When** the endpoint maps to DTOs, **Then** every item is retained and `reasons` are exactly `Reason.Text` in original order with no synthetic copy and no filtering (brief Q5).
3. **Given** a deliberately non-score-sorted handler list, **When** the endpoint responds, **Then** JSON array order matches the handler list order (no rerank).
4. **Given** a cancelled token at preflight or after handler await, or a dependency failure, **When** the request runs, **Then** cancellation or the error propagates through existing infrastructure; failures are not converted into success-empty results.
5. **Given** a seeded library on real PostgreSQL, **When** the production route is called for an eligible movie, **Then** ranked JSON with reasons is returned via the landed DI/handler path without mocking query translation or serialization.

---

### User Story 2 - Validate take, missing movie, and Pending short-circuit (Priority: P2)

Callers that supply invalid take, an unknown movie, a nonpositive id, or a Pending selected movie receive the ticket status contract: 400 ProblemDetails for bad take, 404 for missing/nonpositive id, and 200 empty array for Pending without recommendation dispatch.

**Why this priority**: Ticket acceptance explicitly requires out-of-range take → 400 and unknown movie → 404; Pending empty is ticket Scope.

**Independent Test**: Same `RecommendationEndpointTests.cs` covers omitted take = 10 independent of `RecommendationOptions.Take`, inclusive 1 and configured `CandidatePoolSize`, configured bound changes, default 10 rejected when it exceeds a smaller valid pool, empty/whitespace/malformed/overflow/repeated/out-of-range take → 400, accepted integer signs/surrounding whitespace, take-before-id/missing/Pending precedence, nonpositive and unknown id → 404 without handler dispatch, Pending → 200 `[]` with no handler/candidate/scoring dispatch. End-to-end suite covers missing 404 and Pending 200 empty (brief Q2–Q3, Q7).

**Acceptance Scenarios**:

1. **Given** omitted `take`, **When** the request is valid otherwise, **Then** effective take is literal 10 even if `RecommendationOptions.Take` is configured differently.
2. **Given** explicit `take` empty, whitespace-only, malformed, overflowing, repeated, or outside inclusive `1..CandidatePoolSize`, **When** the endpoint runs, **Then** response is 400 ProblemDetails via the existing BadHttpRequestException path; ValidationException→422 policy is unchanged.
3. **Given** invalid take and a missing or Pending movie, **When** the endpoint runs, **Then** 400 wins; take is validated before MovieId conversion and before repository/handler dispatch.
4. **Given** valid take and a nonpositive integer id, **When** the endpoint runs, **Then** 404 ProblemDetails is returned without dependency dispatch.
5. **Given** valid take and an unknown movie id, **When** preflight `IMovieRepository.GetAsync` returns null, **Then** parameterless `NotFoundException` maps to 404.
6. **Given** valid take and selected enrichment Pending, **When** preflight completes, **Then** response is 200 with an empty JSON array and the recommendation handler is not invoked.
7. **Given** non-integer route segments, **When** the request hits `{id:int}`, **Then** existing integer route-constraint behavior applies.

---

### User Story 3 - OpenAPI document and generated client stay in sync (Priority: P3)

The committed OpenAPI baseline, Verify drift snapshot, generated TypeScript types, and MSW handlers describe `GetRecommendations` with optional integer `take` (default 10, minimum 1, and the exact fixed upper-bound description `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.`), 200 DTO array, and 400/404 ProblemDetails. Typed fixture, client typecheck, and focused generated-mock smoke prove the chain.

**Why this priority**: Ticket acceptance requires OpenAPI with no drift from committed `openapi.json`, regenerated TS client, and updated drift snapshot.

**Independent Test**: `OpenApiContractTests` assertions for the new operation/schema/query/response; opt-in `LAMUFLIX_REGENERATE_OPENAPI=true` regenerates baseline and snapshot together; `npm run generate:api` regenerates `types.ts` and `handlers.ts`; `web/src/mocks/fixtures.ts` adds `GetRecommendations: []`; `web/src/api/client.typecheck.ts` exercises the typed call; `web/src/mocks/recommendations.test.ts` asserts 200 empty array via generated handlers (brief Q6–Q7, Q9). Inventories in OpenApiContractTests, ApiHostCompositionTests, and LibraryEndpointsTests update to the actual post-drift set.

**Acceptance Scenarios**:

1. **Given** the Development OpenAPI document after the endpoint and transformer land, **When** drift tests run, **Then** generated document equals committed `web/src/api/openapi.json` and the Verify snapshot; no health paths leak; production documentation route remains 404.
2. **Given** operation `GetRecommendations`, **When** OpenAPI metadata is inspected, **Then** `take` is optional integer default 10 / minimum 1 with exact fixed description prose `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.` (no numeric maximum, no runtime interpolation), 200 is an array of the exact DTO schema, reasons are strings, EnrichmentStatus is string enum, and 400/404 are ProblemDetails.
3. **Given** regenerated `openapi.json`, **When** `npm run generate:api` runs, **Then** `web/src/api/types.ts` and `web/src/mocks/handlers.ts` match generation without hand edits.
4. **Given** the typed empty success fixture, **When** the focused MSW smoke test requests the new route, **Then** it receives 200 `[]`.

### Edge Cases

- Omitted take defaults to 10; `RecommendationOptions.Take` never overrides HTTP default or explicit take.
- Inclusive range `1..options.Value.CandidatePoolSize`; default 10 must fail if configured pool is smaller than 10.
- Integer parse: invariant culture, `NumberStyles.Integer`, one value only; standard sign and surrounding whitespace accepted.
- Pending-only HTTP short-circuit; Enriched/Failed/NotFound dispatch; no Failed/NotFound-to-empty shortcut at the HTTP layer.
- Accepted double-read cost for non-Pending: preflight then handler; deletion between reads → normal handler 404; no transaction/cache/snapshot (brief Q3).
- Zero-score items and empty `reasons: []` preserved; no synthetic explanations (brief Q5; ADR-0011 harmonized as noted, no ticket).
- Cancellation checked before preflight I/O, after preflight await, and once at the shared post-handler/pre-mapping point (no duplicate check solely to mirror wording).
- No ranking/scoring/persistence reimplementation; no placeholders; no MMR, embeddings, pgvector, schema, new package/project/port, LocalPlay, secrets, Process.Start, or UI (brief Q1).
- Phase B waits for user-merged DEV-341 spec PR, Epic 7, landed DEV-340 and DEV-336/337/338/339 on main, then Wisp prerequisite/drift recon; signature divergence stops for a cited decision (brief Q1; PRODUCT.md §2).
- `propertyTests: opt-out` recorded in task note DEV-341 (brief Q7); scoring/ranking invariants remain upstream.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Expose `GET /api/movies/{id:int}/recommendations` via `MapRecommendationEndpoints` in `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs`, composed from `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`. OperationId `GetRecommendations`; summary `Get recommendations for a library movie` (ASSUMPTIONS.md Q6).
- **FR-002**: Return type MUST be `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>`. Success uses `TypedResults.Ok(...)`. Out-of-range/invalid take uses existing `BadHttpRequestException` → 400 ProblemDetails. Missing movie uses parameterless `NotFoundException` → 404 ProblemDetails. Do not change global ValidationException → 422 behavior.
- **FR-003**: Query parameter `take`: omitted → effective value 10 independent of `RecommendationOptions.Take`. Explicit take MUST be a single value parsed with `int.TryParse(..., NumberStyles.Integer, CultureInfo.InvariantCulture, ...)`. Effective value MUST lie in inclusive `1..IOptions<RecommendationOptions>.Value.CandidatePoolSize`. Empty, whitespace-only, malformed, overflowing, repeated, and out-of-range values MUST yield 400. Validate take before `MovieId.TryCreate` and before repository or handler dispatch.
- **FR-004**: After take validation, nonpositive id failing `MovieId.TryCreate` MUST 404 without dependency dispatch. Non-integer route segments follow the existing `{id:int}` constraint.
- **FR-005**: After take/id validation, check cancellation, await existing `IMovieRepository.GetAsync(movieId, ct)`, check cancellation again. Null → parameterless `NotFoundException`. `EnrichmentStatus.Pending` → `TypedResults.Ok(new List<RecommendedMovieDto>())` without invoking `IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>`. Enriched, Failed, and NotFound → dispatch `new GetRecommendationsQuery(movieId, take)` with the request `CancellationToken`. When no intervening work exists between handler await and mapping, one cancellation check at that shared post-handler/pre-mapping point satisfies both boundaries (do not add a duplicate check solely to mirror wording). Preserve order, Score, and Reasons; propagate cancellation and dependency errors; never success-empty on failure.
- **FR-006**: Define API-owned `sealed record RecommendedMovieDto(int Id, string Title, string? PosterUrl, int? ReleaseYear, int? Runtime, decimal? ImdbRating, EnrichmentStatus EnrichmentStatus, bool IsInWatchlist, bool IsWatched, bool IsNotInterested, double Score, ImmutableList<string> Reasons)` in `src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs` (`using System.Collections.Immutable`; brief Q4 / Keel C1-T1; `constitution.md:429`). Serialize camelCase JSON; EnrichmentStatus as string enum. Flatten the ten Summary fields using existing MovieSummaryResponse presentation conversions including `Id.Value`; preserve Score; map ordered `Reason.Text` via `recommendedMovie.Reasons.Select(reason => reason.Text).ToImmutableList()` (empty source yields an empty immutable list). Explicit empty DTO fixtures use `ImmutableList<string>.Empty`; never null or default. Outer endpoint signature remains `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>`. JSON/OpenAPI/generated TypeScript wire form of `Reasons` is an ordered `string[]`. No nested Summary, domain wrappers, file paths, derived rank, diagnostic sub-scores, extra detail reads, filtering, reranking, truncation, or synthetic explanations.
- **FR-007**: Consume DEV-340 `GetRecommendationsQuery(MovieId, int Take = 10)` / `IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>` and DEV-338 `Reason.Text` unchanged. No ranking, scoring, persistence, or placeholder reimplementation. Touch `src/LamuFlix.Api/HandlerRegistration.cs` only if Phase B drift recon proves missing required DEV-340 handler registration; never duplicate registration or change Core validators.
- **FR-008**: Add `src/LamuFlix.Api/RecommendationsOperationTransformer.cs` and register it in existing `AddOpenApi` wiring in `src/LamuFlix.Api/Program.cs`. Document this operation only: optional integer take default 10 / minimum 1 / exact fixed description prose `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.`; 200 DTO array; 400/404 ProblemDetails. No numeric maximum and no runtime interpolation in transformer metadata; the transformer receives no runtime `RecommendationOptions` for this description; endpoint validation still reads the configured `CandidatePoolSize` bound. Do not alter other operations. Read raw take query values for Q2 multiplicity/parse policy; do not expose raw string binding as the public schema.
- **FR-009**: Regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together via opt-in `LAMUFLIX_REGENERATE_OPENAPI=true`. Then run `npm run generate:api` in `web/` for `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. Never hand-edit generated files or rewrite the generator.
- **FR-010**: Update forced tracked inventories and fixtures: `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` (operation/schema/query/response), `ApiHostCompositionTests.cs` (route inventory), `LibraryEndpointsTests.cs` (shared endpoint-name inventory only), `web/src/mocks/fixtures.ts` (`GetRecommendations: []`), `web/src/api/client.typecheck.ts` (typed exercise). Update inventories from the actual post-drift set, not an assumed intake count of fifteen.
- **FR-011**: Add `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` covering the brief Q7 HTTP proof matrix (take default/bounds/malformed/precedence, id 404, Pending short-circuit, enrichment-state dispatch, twelve-field mapping, rank/reason cases, cancellation/errors). Add `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` on existing `ApiEndToEndCollection` for seeded ranked JSON/reasons, missing 404, Pending 200 empty; no driver mocks for translation/serialization. Exact fixture/seed helper APIs are Phase B recon facts, not invented here.
- **FR-012**: Add focused generated-MSW smoke at `web/src/mocks/recommendations.test.ts`: import generated handlers, request the new route, assert 200 empty array using the typed fixture. Wisp Phase B recon verifies placement/collision and Vitest/MSW conventions before edits; mismatch stops for a cited decision (brief Q9).
- **FR-013**: Approved new production files: `RecommendationEndpoints.cs`, `RecommendedMovieDto.cs`, `RecommendationsOperationTransformer.cs`. Approved new test files: `RecommendationEndpointTests.cs`, `ApiEndToEndRecommendationTests.cs`, `web/src/mocks/recommendations.test.ts`. Approved edits and generated outputs are exactly the Q6–Q7 set. No new NuGet/npm dependency, project, architectural layer, port, schema/migration, LocalPlay, secrets, Process.Start, UI, CONTEXT.md edit, or ADR.
- **FR-014**: `propertyTests: opt-out — no new domain invariants; HTTP query parsing, status precedence, and flat DTO mapping are covered by endpoint integration tests while scoring/ranking invariants belong to DEV-338/DEV-340` (task note DEV-341; brief Q7). Accepts no-tests-tagged exit 2 only; actual property-test failure still blocks. Phase B Gauge bar: Roslyn, complexity, InspectCode on diff (never All), mutation via `scripts/run-mutation.ps1` without since, property, vulnerable packages, format, `dotnet test`, web gates and `check:api`. Exit 1 / Could not run / unaccepted property exit 2 block. Scope-empty SKIPPED, configured SKIP, and mutation N/A are never PASS.

### Key Entities

- **RecommendedMovieDto**: Flat API wire DTO for one recommendation row (twelve fields including Score and string Reasons).
- **Recommendation**: Suggested library movie from the details page (glossary; this ticket exposes the ranked HTTP list).
- **Reason**: Caller-safe explanation (owned by DEV-338; exposed as strings via `Text`).
- **GetRecommendationsQuery / RecommendedMovie**: DEV-340 Core contracts consumed unchanged.
- **RecommendationOptions.CandidatePoolSize**: Inclusive upper bound for HTTP take (DEV-339).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For an eligible selected movie, the endpoint returns recommendations in handler rank order with scores and reason strings matching upstream output.
- **SC-002**: Invalid take yields 400 ProblemDetails; unknown or nonpositive id yields 404; Pending yields 200 empty array without recommendation dispatch.
- **SC-003**: Omitted take behaves as 10 independent of `RecommendationOptions.Take`; effective take is always validated against configured `CandidatePoolSize`.
- **SC-004**: Wire payload exposes exactly the twelve Q4 fields in camelCase with EnrichmentStatus string enum and string reason arrays; zero-score empty and nonempty reason cases match handler output.
- **SC-005**: Committed OpenAPI baseline, Verify drift snapshot, and generated TypeScript/MSW have no drift; focused MSW smoke returns 200 `[]` for the new route fixture.
- **SC-006**: Real-Postgres end-to-end path returns ranked JSON with reasons for a seeded eligible movie and honors missing/Pending status contracts.
- **SC-007**: Phase B gates run with `harness.yml` thresholds only; property opt-out accepts no-tests-tagged exit 2 only; SKIPPED/SKIP/N/A never recorded as PASS.

## Assumptions

- Mapper name `MapRecommendationEndpoints`, operationId `GetRecommendations`, and summary `Get recommendations for a library movie` are Patron taste rulings tagged `[assumed]` in `specs/DEV-341/ASSUMPTIONS.md` (brief Q6).
- Q9 smoke-test path `web/src/mocks/recommendations.test.ts` is a Patron technical/file-envelope placement ruling (brief Q9 / Compass L2), not taste; no `[assumed]` ASSUMPTIONS entry and no scope/proof change.
- Phase B build intake remains gated on user-merged DEV-341 spec PR, Epic 7 completion (`specs/PRODUCT.md` Non-Goals & Sequencing), landed DEV-340 and DEV-336/337/338/339 on main, and Wisp prerequisite/drift recon (brief Q1, Q7). Specifying in Phase A does not authorize Phase B.
- Exact e2e seed/fixture helper APIs, HandlerRegistration necessity, and MSW smoke placement collision checks are Phase B recon obligations already ruled in brief Q6–Q9 — not silent drafting inventions.
- No owner structural checkbox was identified (brief Q8; Patron F1 no Constitution V departure). Q5 ADR harmonization is `noted, no ticket`.
- Exclusions in brief Q8 frozen scope remain out of scope for this feature.
- `propertyTests: opt-out` text in task note DEV-341 is preserved and authoritative for the property gate disposition (brief Q7).
