# Implementation Plan: GET /api/recommendations/for-you from taste profile

**Branch**: `feature/DEV-357` | **Date**: 2026-10-22 | **Updated**: 2026-10-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/DEV-357/spec.md`, closed brief Q9, Phase B pickup amendment 2026-10-24, CONCLUSIONS.md Q1–Q9 + amendment, recon-DEV-357, recon-DEV-357-2 Sections 3–4, `specs/PRODUCT.md`.

**Status**: Draft plan aligned to Phase B Patron ruling (APPROVED WITH AMENDMENTS FOR QUILL ALIGNMENT). **Does not claim plan freeze or Gate 1.** Technical dependency readiness at HEAD `0607eb7` is established for alignment; source implementation still awaits applicable separate pipeline approvals after this Quill alignment.

**Evidence**: worktree `F:/Dev/LamuFlix.worktrees/DEV-357`; branch `feature/DEV-357`; Phase A base `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`; current source HEAD `0607eb7ea8a4ec48ffffa933767568a87eb8df30`.

## Summary

Deliver `GET /api/recommendations/for-you` that returns personalized recommendations from the taste profile. With positive history, orchestrate landed DEV-353 TasteProfile (via DEV-415 cache), acquire all eligible library candidates by composing existing `IMovieCatalog.BrowseAsync` + `IMovieRepository.GetAsync` + `IRecommendationProfileSource.GetProfileInputsAsync` (no new port), score with DEV-354 blend (`S = 0`), apply DEV-416 MMR with selector-only nonnegative relevance offset then ID remapping to original blend results, and return predecessor `RecommendedMovieDto` items with typed Personal/Structural reasons. With no positive history, return 200 `[]` before scoring/MMR. Preserve DEV-341 `take` validation (omitted defaults to constant 10 and remains range-validated; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400) and update OpenAPI baseline, drift snapshot, and both generator outputs `web/src/api/types.ts` and `web/src/mocks/handlers.ts` through the existing opt-in writer then `web generate:api` — no new exporter, never hand-edit generated outputs.

At HEAD `0607eb7`, T001–T011 (and T012 / T013a) are technically satisfied per Patron Phase B amendment. Quill alignment of spec/plan/tasks is required before Phase 2 source work. Plan freeze, Gate 1, and source-implementation authorization remain separate.

## Technical Context

**Language/Version**: .NET 10 / C# (backend); TypeScript / Vite / React (`web/`) for generated contract consumers only (no For You UI — DEV-418)

**Primary Dependencies**: Existing LamuFlix stack only — no new NuGet/npm dependency authorized. At HEAD `0607eb7`, landed DEV-338 / DEV-341 / DEV-353 / DEV-354 / DEV-415 / DEV-416 predecessor contract and implementation bundles are on `origin/main == HEAD` (Phase B amendment; recon-DEV-357-2:11-36,75,94). Consume those landed surfaces via the three-port composition; do not invent signatures or add a port/adapter.

**Storage**: No schema/migration authorized. Consume landed acquisition/cache surfaces only. Consistency is per-query freshness plus DEV-415 invalidation — no atomic cross-query snapshot or concurrent-write completeness guarantee (Phase B amendment; specs/DEV-415/CONCLUSIONS.md:51).

**Testing**: xUnit unit/property (FsCheck required — no property opt-out), Testcontainers PostgreSQL for data-dependent persistence/vector/cache-invalidation route tests, existing recommendation endpoint/E2E/OpenAPI contract tests. Never mock driver query translation or serialization; never substitute fake vectors for runtime acquisition proof (brief Q8). Required proofs include full-library paging beyond `CandidatePoolSize`, vectorless/unenriched retention, feedback exclusion/recheck, null-movie skips, cache/history semantics, and negative-score preservation through selector-only rebase (Phase B amendment).

**Target Platform**: LamuFlix API (local/personal streaming library)

**Project Type**: Ports-and-adapters with feature-organised Core (constitution Principle I) — Recommendations feature folder extension in the existing solution + generated web contract artifacts. Not vertical-slice architecture (slices do not own data access) and not four-layer Clean Architecture.

**Performance Goals**: Full-library scoring cost accepted (brief Q4). Documented acquisition cost `ceil(N/Size)` Browse pages + N+1 `GetAsync` + one `GetProfileInputsAsync` also accepted (Phase B amendment). No other performance target specified — not invented.

**Constraints**:
- No new port, project, layer, package, migration, schema, DTO shape, or cache (brief Q7; Phase B amendment).
- Contract chain: C# DTO → OpenAPI → generated TS; no hand-edits of generated files.
- Features:LocalPlay, secrets, Process.Start: untouched.
- Exact predecessor signatures are those recorded at HEAD `0607eb7` in recon-DEV-357-2; generation commands confirmed at recon-DEV-357-2:79-81 — do not invent beyond that (brief Q7–Q9; Phase B amendment).
- Gate execution belongs to Build-workspace stages (task-pipeline Phase 3 step 5); not run by this Spec draft (brief Q8).

**Scale/Scope**: M-tier; authorized Q7 file ledger only; For You UI is DEV-418.

## Constitution Check

*GATE: Separate plan freeze / Gate 1 — not claimed here. Principle numbers cited per constitution Governance / Compliance (constitution.md:562-564).*

- **Principle I** (ports and adapters, feature-organised Core): New query/handler live under `src/LamuFlix.Core/Features/Recommendations/`; new FluentValidation validator lives under `src/LamuFlix.Infrastructure/Recommendations/` (Patron Q7 amendment DEV-357:225; brief.md:71,87-91 — Core MUST NOT reference FluentValidation). No new project, top-level folder, architectural layer, Core port, or package. Consume existing landed predecessor ports only via the certified three-port composition; no silent new adapter (brief Q7, Q9; Phase B amendment; constitution.md:127-159).
- **Principle II** (explicit handlers and decorators): `GetForYouRecommendationsQuery` is a sealed record; handler is sealed `IQueryHandler`; register via existing `AddHandler<…>` helper so decorator order outer→inner is **Tracing → Logging → Validation → handler**. Alongside `AddHandler`, register scoped concrete `GetForYouRecommendationsQueryValidator` and scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to that same concrete instance in already authorized `HandlerRegistration.cs` (brief.md:104; recon-DEV-357:101). No MediatR or reflection dispatch (constitution.md:161-169; brief Q7).
- **Principle V** (validated inputs, consistent errors): FluentValidation on the query via the validation decorator → 422 ProblemDetails with `traceId`, proved through composed registration. Route-level DEV-341 `ParseTake` failures stay on the existing `BadHttpRequestException` → 400 ProblemDetails path and MUST remain distinct from direct-query validation 422. Omitted `take` defaults to constant 10 and remains range-validated; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400 (brief.md:44,112). Endpoints MUST NOT leak exception text; all error paths use centralized ProblemDetails (constitution.md:229-246; brief Q2).
- **Principle VII** (configuration and time): Consume existing `RecommendationOptions` (including `Take`, `CandidatePoolSize`, `MmrLambda`, blend weights); no new options record, secret, machine path, or `IConfiguration` magic string. No `DateTime.Now`-family; inject `TimeProvider` only if this slice needs time (constitution.md:275-291; brief Q7).
- **Principle VIII** (ubiquitous language): Use established English terms Recommendation, Reason, Taste Profile, Playback Event, Feedback; no Portuguese identifiers; no new glossary term required (constitution.md:296-311; brief Q9).
- **Principle IX** (test pyramid, real infrastructure): xUnit v3 + NSubstitute + Shouldly + AutoFixture/Faker.Net; FsCheck properties required (no opt-out); data-dependent persistence/vector/cache-invalidation proofs use real PostgreSQL/Testcontainers — never mock driver translation/serialization; never fake vectors for acquisition proof (constitution.md:316-335; brief Q8).
- **Technology Stack / endpoint rules** (supporting; nearest Principles I–II, V): Minimal API with `TypedResults` / `Results<…>` unions; thin route in existing `RecommendationEndpoints` group; committed OpenAPI + generated TypeScript contract chain (constitution.md:353,393-394,495-506; brief Q2, Q8).
- **API and Contract Rules**: Update committed `web/src/api/openapi.json` and drift snapshot through the existing opt-in writer; run existing `web generate:api` to produce both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` — never hand-edit generated outputs, replace the generator, claim each run must change bytes, or add a new exporter (constitution.md:504-506; recon-DEV-357:107-108; recon-DEV-357-2:79-81,96; brief.md:83,93-100,122; DEV-357:254-255).
- **Static-analysis / PR quality gates** (supporting; nearest Principle IX): Build-stage Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format, `dotnet test`, applicable web gates at harness thresholds — never lowered; scope-empty / SKIP / N/A / Could not run kept distinct (constitution.md:380-430; brief Q8).

### Charter / PRODUCT care-list (separate from Constitution Check)

Aligned with closed grill and Phase B amendment (no constitution departure; no owner structural checkbox — brief Q9; Phase B amendment):
- §2.3 item 1: no new dependency.
- §2.3 item 2: no new project/layer/port; three-port composition certified without a new port/adapter (Phase B amendment).
- §2.3 item 3: no schema/migration.
- §2.3 item 4: route `GET /api/recommendations/for-you` is ticket-named; DTO/reason shape consumes predecessor DEV-354 contract, not a new shape (brief Q2).
- §2.3 item 5: Features:LocalPlay / secrets / Process.Start untouched.
- §2.3 item 6: edits limited to Q7 named new files (Infrastructure validator path per DEV-357:225) plus authorized existing endpoint, registration, host-composition test (route equality + additive for-you `ManifestContracts` row only per DEV-357:254-255), and other test files forced by route/OpenAPI acceptance (brief Q7; brief.md:71,76,83,87-100).
- PRODUCT §2: Epic 7 prerequisite SATISFIED by Patron certification from owner-final `specs/DEV-338/CONCLUSIONS.md:131-133` and `specs/DEV-342/CONCLUSIONS.md:118,121` (Phase B amendment; recon-DEV-357-2:77,94-95).

If a future fact forces ticket change or constitution departure → owner §2.3 escalation; not silently assumed.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-357/
├── brief.md             # Closed grill decisions (Q9) + Phase B amendment
├── CONCLUSIONS.md       # Full Q1–Q9 exchanges + Phase B amendment
├── spec.md              # Feature specification
├── plan.md              # This file
└── tasks.md             # Implementation task ledger
```

### Authorized source / test / web ledger (exact; Q7 as amended)

Presence does not authorize inventing predecessor APIs beyond recorded signatures. No unrelated rewrites. Infrastructure/DI files outside this list are not silently authorized.

**New Core**

- `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQuery.cs`
- `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs`

**New Infrastructure (Q7 validator amendment, DEV-357:225; brief.md:71,87-91)**

- `src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs`

**Matching unit tests (new; all three named paths retained)**

- `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryValidatorTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs`

**Authorized existing edits**

- `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` (thin route; reuse take parser and `ToDto`)
- `src/LamuFlix.Api/HandlerRegistration.cs` (required registration: scoped concrete validator + scoped `IValidator<>` forwarding + `AddHandler`; brief.md:104)
- `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` (route-equality update and additive for-you `ManifestContracts` row only; DEV-357:254-255; brief.md:76,93-100 — no forced completeness/count claim)
- `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`
- `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs`
- `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` (reuse existing opt-in writer; no new exporter)
- `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`

**Generated contract outputs (via existing pipeline only)**

- `web/src/api/openapi.json`
- `web/src/api/types.ts`
- `web/src/mocks/handlers.ts` (via existing `generate:api` only; DEV-357:254-255; brief.md:83,93-100,122; recon-DEV-357:107-108; recon-DEV-357-2:79-81,96)

**Structure Decision**: New for-you query/handler orchestration in the existing Recommendations feature folder (feature-organised Core under ports and adapters — Principle I); FluentValidation validator in Infrastructure Recommendations (matching existing placement); thin API route beside the per-movie endpoint; no new project, top-level folder, Core FluentValidation reference, or Core port.

## Complexity Tracking

> No constitution violations requiring justification. Q4 candidate/`S=0` policy, Q5 `HasHistory` predicate, and Phase B three-port / MMR-offset / retention rulings are Patron design rulings on unspecified semantics, not new layers or API shapes beyond the ticket route.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |

## Policy (implementation)

### HTTP and DTO

1. Route: `GET /api/recommendations/for-you` using `TypedResults` / existing `Results<…>` union patterns.
2. Reuse existing take parser and `ToDto` in `RecommendationEndpoints.cs` (brief.md:44; recon-DEV-357:68-70).
3. take: omitted key → default constant 10 and the effective value remains range-validated in `1..CandidatePoolSize`; single integer in that range accepted; explicitly empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range → 400 ProblemDetails via `BadHttpRequestException` (DEV-341). Absence of the key alone is allowed; when startup-valid `options.Take <= CandidatePoolSize < 10`, omitted `take` yields parser 400 ProblemDetails/`traceId` because the defaulted constant exceeds the pool (brief.md:44,112). Direct FluentValidation on the query → 422 ProblemDetails with `traceId`, distinct from parser 400 (Principle V).
4. 200 array of predecessor `RecommendedMovieDto` with DEV-354 typed lowercase `{kind,text}` reasons.
5. Preserve final MMR order on the wire.
6. In `HandlerRegistration.cs`, register scoped concrete `GetForYouRecommendationsQueryValidator`, scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to that same concrete instance, and `AddHandler` so decorator order remains Tracing → Logging → Validation → handler. Prove direct-query validation through composed registration; retain unit-validator coverage and parser 400 versus query-validation 422 distinction (Principle II; brief.md:104; recon-DEV-357:101).

### History and cache

1. `HasHistory` = at least one `PlaybackEvent` OR at least one `IsWatched=true` movie; Not Interested alone is insufficient (brief Q5). Landed expression: non-empty `RecommendationProfileInputs.Movies` where adapter rows represent `IsWatched || launchedIds.Contains(Id)` (Phase B amendment; recon-DEV-357-2:41-43,88-93).
2. No history → 200 `[]` before scoring/MMR; do not equate with `TasteProfile.IsEmpty`.
3. Consume DEV-415 single-user tagged cache, configurable expiry, playback/feedback invalidation; add no cache/TTL/key/package. `GetProfileInputsAsync` bypasses the profile cache (fresh history); `GetTasteProfileAsync` uses the tagged cache.
4. Consistency: per-query freshness plus DEV-415 invalidation. No atomic cross-query snapshot or concurrent-write completeness guarantee (Phase B amendment; specs/DEV-415/CONCLUSIONS.md:51).

### Candidates, blend, MMR (Phase B refinements)

1. Candidates = all eligible library movies via three-port composition only:
   - Page `IMovieCatalog.BrowseAsync(MovieQuery, ct)` with explicit valid non-null `Sort`/`Direction`, `Page.Number >= 1`, `Page.Size` within `1..100`; terminate on first short page (`Items.Length < Size`).
   - Exclude `IsNotInterested` on paged summaries before scoring.
   - Per-ID `IMovieRepository.GetAsync(id, ct)` for full domain `Movie` rows; skip null misses; recheck `movie.IsNotInterested`.
   - One `IRecommendationProfileSource.GetProfileInputsAsync(allEligibleIds, ct)` for candidate embeddings and coherent history.
   - No selected-movie source/seed/pre-truncation/refill; no `CandidatePoolSize` truncation of the candidate universe; no new port/adapter.
2. Retain vectorless/unenriched eligible candidates using landed empty-feature (`MovieFeaturesMapper.From(movie)` → `MovieFeatures.Empty`) and empty-embedding (`CandidateEmbeddings.GetValueOrDefault(id, ImmutableArray<float>.Empty)` → genre-only / `0.0` channel) fallbacks. Corrupt vectors retain landed scoring failure behavior; missing vectors take the established fallback. Do not exclude merely for missing vectors.
3. Invoke literal DEV-354 blend with `S = 0`; retain configured weights; no renormalization/final clamp.
4. Pass blended relevance to DEV-416 MMR before Take. At `MmrSelector.Select` input only, apply pool-wide nonnegative relevance offset `-Math.Min(0.0, minPoolScore)`; map selected `MovieId`s back to original `BlendScorer` results so returned `Score` (including negative finite scores) and `Reasons` remain untouched. Consume `MmrLambda`, DEV-338 similarity, ID ties, uniqueness, `min(Take, eligible count)`.
5. Preserve blend Score and reason objects; no score resort; no MMR-objective score replacement; no fabricated Personal reasons; no clamp of output blend scores.
6. Degenerate profile / `BlendProfileWeight = 0`: keep DEV-354 Personal/Structural fallback; acceptance Personal proof uses positive-match fixture with profile weighting enabled.
7. Accepted cost: `ceil(N/Size)` paging + N+1 `GetAsync` + one `GetProfileInputsAsync`, plus Q4 full-library scoring cost.
8. Do not invent predecessor algorithm signatures.

### Contract generation

Reuse existing `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true`, then existing `web generate:api` (`web/package.json:17` → `web/scripts/generate-api.mjs`), which produces both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (recon-DEV-357:107-108; recon-DEV-357-2:79-81,96; brief.md:122; DEV-357:254-255). Never hand-edit generated outputs, replace the generator, or claim each run must change output bytes. Exact invocation commands confirmed at HEAD `0607eb7` — Quill does not invent flags beyond the named writer, env var, and `generate:api` already recorded.

## Behavioral proof matrix (Q8 + Phase B amendment)

| Area | Obligations |
|------|-------------|
| take / validator | Omitted → default constant 10, still range-validated; lower/upper bounds; explicitly empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range → parser 400 ProblemDetails/`traceId`; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` → parser 400 (default exceeds pool); direct query FluentValidation through composed registration → 422 ProblemDetails/`traceId` (distinct); unit-validator coverage retained |
| History / cold start | Playback-only; watched-only; no-history; Not Interested-only; real history with empty features (not cold start); no candidates → 200 `[]`; `HasHistory` via `Movies` non-empty (`IsWatched \|\| launched`), never `TasteProfile.IsEmpty` |
| Reasons | Positive genre/plot Personal explanations; disabled profile-weight fallback; typed lowercase wire fields |
| Candidates / blend / acquisition | Full-library paging beyond `CandidatePoolSize` with valid sort/direction and size `1..100`; Not Interested exclusion on summaries + recheck on domain rows; null `GetAsync` skips; vectorless/unenriched retention with empty-feature/embedding fallbacks; `S = 0` literal blend; watched penalty; negative finite score preservation through selector-only rebase |
| MMR | Selector-input offset `-Math.Min(0.0, minPoolScore)`; ID remap to original results; selection order changes without mutating scores/reasons; uniqueness; min-size; deterministic ordering/ties; diverse sequel fixture; FsCheck properties required |
| Cache | Repeated reads and playback/feedback invalidation consume DEV-415 behavior — do not rebuild its cache; no atomic snapshot claim |
| Integration / contract | Cancellation propagation through three ports; per-movie route regression; typed wire output; host route-equality + additive for-you `ManifestContracts` decorator-resolution row; no OpenAPI drift; generated `types.ts` and `handlers.ts` consistency |
| Persistence | Real PostgreSQL/Testcontainers for data-dependent persistence, vectors, and cache-invalidation route tests; never mock driver translation/serialization; never fake vectors for acquisition proof |

## Gate expectations (Q8; Build workspace — not executed here)

**Phase A / alignment (now)**: specification drafts only. No production tests or gates run for pass claims. After Quill alignment: Keel read-only speckit-analyze + brief conformance → plan challenge → **separate** plan-freeze and Gate 1 decisions remain outstanding (brief Q9; Phase B amendment).

**Build stage (Gauge / task-pipeline Phase 3 step 5)** at harness thresholds when implementation reaches that stage:
- Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format verification, `dotnet test`, and applicable web gates
- No threshold changes
- Scope-empty exit 2 → nonblocking `SKIPPED (scope-empty)`, never PASS
- Mutation explicit exclusions → N/A
- Harness-disabled gates → SKIP
- Exit 1 or Could not run → blocks
- No property-test opt-out (ordering/size/exclusion invariants exist)

Exact harness numbers and rendered tool config are rechecked at Build pickup — not invented or certified by this Spec draft.

**Status discipline**: Report PASS, SKIPPED (scope-empty), opt-out SKIP, mutation N/A, and Could not run as distinct. Surviving mutants → fix with tests; never lower thresholds. M-tier: Sentry, Ledger, Compass under Q3 bar/caps; ship/hosted/merge bar separate; user owns merge.

## Dependency-aware order (Q1, Q7–Q9; Phase B amendment)

1. Phase A drafting and Quill alignment to Phase B amendment (this plan); subsequent read-only analysis, plan challenge/freeze, and Gate 1 remain separate and unclaimed.
2. **Phase 1 (tasks.md)**: Technical STOP items T001–T011, T012, T013a are SATISFIED at HEAD `0607eb7` per Patron ruling. T013 Gauge baseline/gates remain separate and unclaimed. Quill alignment of these artifacts is the remaining prerequisite before Phase 2 source work. Plan freeze / Gate 1 / source-implementation authorization are still separate.
3. Anvil: Core query/handler orchestration + Infrastructure validator + unit/FsCheck (including Phase B proof matrix); commit before reporting.
4. Anvil: thin API route/registration (including explicit validator DI) + real integration coverage including host route-equality / additive `ManifestContracts` row; commit before reporting.
5. Anvil: opt-in OpenAPI/drift-snapshot export + `generate:api` for `types.ts` and `handlers.ts`; commit before reporting.
6. Cog refactor, Gauge applicable gates, all three review axes, remediation within Q3 cap — at Build workspace stages.

## Phase 1 technical readiness (recorded at HEAD `0607eb7`)

Patron Phase B amendment certifies:

1. **T001** SATISFIED — predecessor spec/implementation bundles (`DEV-338`, `DEV-341`, `DEV-353`, `DEV-354`, `DEV-415`, `DEV-416`) landed on `origin/main == HEAD`; DEV-357’s own plan freeze and Gate 1 remain separate (recon-DEV-357-2:11-36,75,94; brief.md:3,138-140).
2. **T002** SATISFIED — Epic 7 certified from owner-final `specs/DEV-338/CONCLUSIONS.md:131-133` and `specs/DEV-342/CONCLUSIONS.md:118,121` (recon-DEV-357-2:77,94-95); do not rely on unsupported `DEV-415:41-44` citation in DEV-356.
3. **T003–T011** SATISFIED — landed predecessors; three-port all-library composition; coherent history via `Movies`; exact signatures recorded; drift current; vectorless retention and selector-only MMR offset ruled (recon-DEV-357-2:30-35,41-43,63-73,88-93).
4. **T012 / T013a** refreshed and satisfied at this HEAD (recon-DEV-357-2:79-81,96).
5. **T013** Gauge baseline/gates remain separate and unclaimed.

This certification does **not** independently grant plan freeze, Gate 1, source implementation authorization, review clearance, runtime acceptance, a test/gate pass, or merge permission. Quill alignment (this plan and companion artifacts) and applicable pipeline approvals remain separate.

Forbidden still: substitute algorithms, production stubs, dependency waiver, invented signatures, ad hoc endpoint cache, silent new port/adapter, out-of-ledger typed-reason/UI edits without cited Patron decision.

## Open drafting items (not inventable here)

- **Resolved (do not re-open as needs decision)**: Q1–Q9 Patron rulings and Phase B pickup amendment in brief.md / CONCLUSIONS.md (dependency STOP, HTTP/DTO, review bar, candidate/`S=0`, HasHistory/cache boundary, MMR/degenerate reasons, file set including Infrastructure validator and host/MSW amendments, tests/gates/order, grill closure, three-port acquisition, vectorless retention, selector-only MMR offset, T001/T002 certification, per-query consistency).
- **Remaining separate pipeline items (not Quill inventables)**: plan challenge/freeze, Gate 1, Phase 2 source authorization, Build-stage gates, M-tier review, ship/merge.

Unresolved semantic product choices beyond this brief → `needs decision` to Keel/Patron; do not silently assume or widen scope.
