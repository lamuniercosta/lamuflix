# Implementation Plan: GET /api/recommendations/for-you from taste profile

**Branch**: `feature/357-spec` | **Date**: 2026-10-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/DEV-357/spec.md`, closed brief Q9, CONCLUSIONS.md Q1–Q9, recon-DEV-357, `specs/PRODUCT.md`.

**Status**: Draft plan for post-grill specification. **Does not claim plan freeze or Gate 1.** Phase A remains specification-only until separate freeze/Gate 1 decisions after Keel read-only analysis/conformance and plan challenge.

**Evidence**: worktree `F:/Dev/LamuFlix.worktrees/feature-357-spec`; branch `feature/357-spec`; Phase A base / HEAD `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`.

## Summary

Deliver `GET /api/recommendations/for-you` that returns personalized recommendations from the taste profile. With positive history, orchestrate landed DEV-353 TasteProfile (via DEV-415 cache), score the eligible library with DEV-354 blend (`S = 0`), apply DEV-416 MMR, and return predecessor `RecommendedMovieDto` items with typed Personal/Structural reasons. With no positive history, return 200 `[]` before scoring/MMR. Preserve DEV-341 `take` validation (omitted defaults to constant 10 and remains range-validated; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400) and update OpenAPI baseline, drift snapshot, and both generator outputs `web/src/api/types.ts` and `web/src/mocks/handlers.ts` through the existing opt-in writer then `web generate:api` — no new exporter, never hand-edit generated outputs.

Phase B source work is gated on Wisp read-only pickup proving merged spec, PRODUCT §2 Epic 7 prerequisite, landed DEV-353/354/415/416, real vector/acquisition inputs, exact signatures, coherent history support, full-library source, and typed-reason wire/consumer/fixture coherence. Missing support → STOP for bounded recon and a cited Patron amendment before any new port or adapter (or before out-of-ledger typed-reason edits).

## Technical Context

**Language/Version**: .NET 10 / C# (backend); TypeScript / Vite / React (`web/`) for generated contract consumers only (no For You UI — DEV-418)

**Primary Dependencies**: Existing LamuFlix stack only — no new NuGet/npm dependency authorized. Relies on landed DEV-353, DEV-354, DEV-415, DEV-416 (and their real vector/acquisition dependencies) at Phase B start. At this Phase A base the named DEV-353/354/415/416 runtime contracts remain unlanded (recon-DEV-357:20-32,76,80). Later recon-DEV-357-2:50-60 supersedes earlier blanket playback/vector absence claims: playback event/write/persistence and movie-feature vector record/column/snapshot/repository exist at this HEAD. That does not establish a history-read contract, complete all-library acquisition, a confirmed `IMovieFeaturesRepository` interface signature, or runtime readiness (recon-DEV-357-2:56,60,62; brief.md:11,134). Named dependency STOP and actual vector/acquisition pickup proof stay mandatory.

**Storage**: No schema/migration authorized. Playback write/persistence and movie-feature vector surfaces exist at this HEAD (recon-DEV-357-2:50-60); history-read and complete all-library acquisition remain pickup proofs. DEV-357 consumes landed acquisition/cache surfaces only.

**Testing**: xUnit unit/property (FsCheck required — no property opt-out), Testcontainers PostgreSQL for data-dependent persistence/vector/cache-invalidation route tests, existing recommendation endpoint/E2E/OpenAPI contract tests. Never mock driver query translation or serialization; never substitute fake vectors for runtime acquisition proof (brief Q8).

**Target Platform**: LamuFlix API (local/personal streaming library)

**Project Type**: Ports-and-adapters with feature-organised Core (constitution Principle I) — Recommendations feature folder extension in the existing solution + generated web contract artifacts. Not vertical-slice architecture (slices do not own data access) and not four-layer Clean Architecture.

**Performance Goals**: Full-library scoring cost accepted (brief Q4). No other performance target specified — not invented.

**Constraints**:
- No new port, project, layer, package, migration, schema, DTO shape, or cache (brief Q7).
- Contract chain: C# DTO → OpenAPI → generated TS; no hand-edits of generated files.
- Features:LocalPlay, secrets, Process.Start: untouched.
- Exact predecessor signatures and generation command details: pickup prerequisites; do not invent (brief Q7–Q9).
- Gate execution belongs to Build-workspace stages (task-pipeline Phase 3 step 5); not run by this Spec draft (brief Q8).

**Scale/Scope**: M-tier; authorized Q7 file ledger only; For You UI is DEV-418.

## Constitution Check

*GATE: Separate plan freeze / Gate 1 — not claimed here. Principle numbers cited per constitution Governance / Compliance (constitution.md:562-564).*

- **Principle I** (ports and adapters, feature-organised Core): New query/handler live under `src/LamuFlix.Core/Features/Recommendations/`; new FluentValidation validator lives under `src/LamuFlix.Infrastructure/Recommendations/` (Patron Q7 amendment DEV-357:225; brief.md:71,87-91; recon-DEV-357-2:8-13 — Core MUST NOT reference FluentValidation). No new project, top-level folder, architectural layer, Core port, or package. Consume existing landed predecessor ports/adapters only; if they cannot supply all-library/history/vector inputs at pickup → STOP for cited Patron amendment, never a silent new adapter (brief Q7, Q9; constitution.md:127-159).
- **Principle II** (explicit handlers and decorators): `GetForYouRecommendationsQuery` is a sealed record; handler is sealed `IQueryHandler`; register via existing `AddHandler<…>` helper so decorator order outer→inner is **Tracing → Logging → Validation → handler**. Alongside `AddHandler`, register scoped concrete `GetForYouRecommendationsQueryValidator` and scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to that same concrete instance in already authorized `HandlerRegistration.cs` (brief.md:104; recon-DEV-357-2:16-20). No MediatR or reflection dispatch (constitution.md:161-169; brief Q7).
- **Principle V** (validated inputs, consistent errors): FluentValidation on the query via the validation decorator → 422 ProblemDetails with `traceId`, proved through composed registration. Route-level DEV-341 `ParseTake` failures stay on the existing `BadHttpRequestException` → 400 ProblemDetails path and MUST remain distinct from direct-query validation 422. Omitted `take` defaults to constant 10 and remains range-validated; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` yields parser 400 (brief.md:44,112; recon-DEV-357-2:65-71). Endpoints MUST NOT leak exception text; all error paths use centralized ProblemDetails (constitution.md:229-246; brief Q2).
- **Principle VII** (configuration and time): Consume existing `RecommendationOptions` (including `Take`, `CandidatePoolSize`, `MmrLambda`, blend weights); no new options record, secret, machine path, or `IConfiguration` magic string. No `DateTime.Now`-family; inject `TimeProvider` only if this slice needs time (constitution.md:275-291; brief Q7).
- **Principle VIII** (ubiquitous language): Use established English terms Recommendation, Reason, Taste Profile, Playback Event, Feedback; no Portuguese identifiers; no new glossary term required (constitution.md:296-311; brief Q9).
- **Principle IX** (test pyramid, real infrastructure): xUnit v3 + NSubstitute + Shouldly + AutoFixture/Faker.Net; FsCheck properties required (no opt-out); data-dependent persistence/vector/cache-invalidation proofs use real PostgreSQL/Testcontainers — never mock driver translation/serialization; never fake vectors for acquisition proof (constitution.md:316-335; brief Q8).
- **Technology Stack / endpoint rules** (supporting; nearest Principles I–II, V): Minimal API with `TypedResults` / `Results<…>` unions; thin route in existing `RecommendationEndpoints` group; committed OpenAPI + generated TypeScript contract chain (constitution.md:353,393-394,495-506; brief Q2, Q8).
- **API and Contract Rules**: Update committed `web/src/api/openapi.json` and drift snapshot through the existing opt-in writer; run existing `web generate:api` to produce both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` — never hand-edit generated outputs, replace the generator, claim each run must change bytes, or add a new exporter (constitution.md:504-506; recon-DEV-357-2:95-106,112-115; brief.md:83,93-100,122; DEV-357:254-255).
- **Static-analysis / PR quality gates** (supporting; nearest Principle IX): Build-stage Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format, `dotnet test`, applicable web gates at harness thresholds — never lowered; scope-empty / SKIP / N/A / Could not run kept distinct (constitution.md:380-430; brief Q8).

### Charter / PRODUCT care-list (separate from Constitution Check)

Aligned with closed grill (no constitution departure; no owner structural checkbox — brief Q9):
- §2.3 item 1: no new dependency.
- §2.3 item 2: no new project/layer/port; if landed ports cannot supply all-library/history/vector inputs → STOP for cited Patron amendment, never silent adapter (brief Q7, Q9).
- §2.3 item 3: no schema/migration.
- §2.3 item 4: route `GET /api/recommendations/for-you` is ticket-named; DTO/reason shape consumes predecessor DEV-354 contract, not a new shape (brief Q2).
- §2.3 item 5: Features:LocalPlay / secrets / Process.Start untouched.
- §2.3 item 6: edits limited to Q7 named new files (Infrastructure validator path per DEV-357:225) plus authorized existing endpoint, registration, host-composition test (route equality + additive for-you `ManifestContracts` row only per DEV-357:254-255), and other test files forced by route/OpenAPI acceptance (brief Q7; brief.md:71,76,83,87-100).
- PRODUCT §2: Epic 9 Recommendations deferred until after Epic 7 SPA — Phase B waits for that prerequisite (brief Q1; PRODUCT.md:17).

If a future fact forces ticket change or constitution departure → owner §2.3 escalation; not silently assumed.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-357/
├── brief.md             # Closed grill decisions (Q9)
├── CONCLUSIONS.md       # Full Q1–Q9 exchanges
├── spec.md              # Feature specification
├── plan.md              # This file
└── tasks.md             # Implementation task ledger
```

### Authorized source / test / web ledger (exact; Q7 as amended)

Presence does not require inventing predecessor APIs. No unrelated rewrites. Infrastructure/DI files outside this list are not silently authorized.

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
- `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` (route-equality update and additive for-you `ManifestContracts` row only; DEV-357:254-255; brief.md:76,93-100; recon-DEV-357-2:82-93 — no forced completeness/count claim)
- `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`
- `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs`
- `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` (reuse existing opt-in writer; no new exporter)
- `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`

**Generated contract outputs (via existing pipeline only)**

- `web/src/api/openapi.json`
- `web/src/api/types.ts`
- `web/src/mocks/handlers.ts` (via existing `generate:api` only; DEV-357:254-255; brief.md:83,93-100,122; recon-DEV-357-2:95-106,112-115)

**Structure Decision**: New for-you query/handler orchestration in the existing Recommendations feature folder (feature-organised Core under ports and adapters — Principle I); FluentValidation validator in Infrastructure Recommendations (matching existing placement); thin API route beside the per-movie endpoint; no new project, top-level folder, Core FluentValidation reference, or Core port.

## Complexity Tracking

> No constitution violations requiring justification. Q4 candidate/`S=0` policy and Q5 `HasHistory` predicate are Patron design rulings on unspecified semantics, not new layers or API shapes beyond the ticket route.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |

## Policy (implementation)

### HTTP and DTO

1. Route: `GET /api/recommendations/for-you` using `TypedResults` / existing `Results<…>` union patterns.
2. Reuse existing take parser and `ToDto` in `RecommendationEndpoints.cs` (recon-DEV-357:10-11).
3. take: omitted key → default constant 10 and the effective value remains range-validated in `1..CandidatePoolSize`; single integer in that range accepted; explicitly empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range → 400 ProblemDetails via `BadHttpRequestException` (DEV-341). Absence of the key alone is allowed; when startup-valid `options.Take <= CandidatePoolSize < 10`, omitted `take` yields parser 400 ProblemDetails/`traceId` because the defaulted constant exceeds the pool (brief.md:44,112; recon-DEV-357-2:65-71). Direct FluentValidation on the query → 422 ProblemDetails with `traceId`, distinct from parser 400 (Principle V).
4. 200 array of predecessor `RecommendedMovieDto` with DEV-354 typed lowercase `{kind,text}` reasons.
5. Preserve final MMR order on the wire.
6. In `HandlerRegistration.cs`, register scoped concrete `GetForYouRecommendationsQueryValidator`, scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to that same concrete instance, and `AddHandler` so decorator order remains Tracing → Logging → Validation → handler. Prove direct-query validation through composed registration; retain unit-validator coverage and parser 400 versus query-validation 422 distinction (Principle II; brief.md:104; recon-DEV-357-2:16-20).

### History and cache

1. `HasHistory` = at least one `PlaybackEvent` OR at least one `IsWatched=true` movie; Not Interested alone is insufficient (brief Q5).
2. No history → 200 `[]` before scoring/MMR; do not equate with `TasteProfile.IsEmpty`.
3. Consume DEV-415 single-user tagged cache, configurable expiry, playback/feedback invalidation; add no cache/TTL/key/package.
4. Exact landed cache/history signatures and snapshot consistency: verify at pickup; if absent → STOP.

### Candidates, blend, MMR

1. Candidates = all eligible library movies; exclude Not Interested before scoring; no selected-movie source/seed/pre-truncation/refill.
2. Invoke literal DEV-354 blend with `S = 0`; retain configured weights; no renormalization/final clamp.
3. Pass blended relevance to DEV-416 MMR before Take; consume `MmrLambda`, DEV-338 similarity, ID ties, uniqueness, `min(Take, eligible count)`.
4. Preserve blend Score and reason objects; no score resort; no MMR-objective score replacement; no fabricated Personal reasons.
5. Degenerate profile / `BlendProfileWeight = 0`: keep DEV-354 Personal/Structural fallback; acceptance Personal proof uses positive-match fixture with profile weighting enabled.
6. Do not invent predecessor algorithm signatures.

### Contract generation

Reuse existing `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true`, then existing `web generate:api` (`web/package.json:17` → `web/scripts/generate-api.mjs`), which produces both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (recon-DEV-357-2:95-106,112-115; brief.md:122; DEV-357:254-255). Never hand-edit generated outputs, replace the generator, or claim each run must change output bytes. **Exact invocation commands and any landed flag details are confirmed by Wisp at pickup** — Quill does not invent them beyond the named writer, env var, and `generate:api` already recorded in recon/brief.

## Behavioral proof matrix (Q8)

| Area | Obligations |
|------|-------------|
| take / validator | Omitted → default constant 10, still range-validated; lower/upper bounds; explicitly empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range → parser 400 ProblemDetails/`traceId`; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` → parser 400 (default exceeds pool); direct query FluentValidation through composed registration → 422 ProblemDetails/`traceId` (distinct); unit-validator coverage retained |
| History / cold start | Playback-only; watched-only; no-history; Not Interested-only; real history with empty features (not cold start); no candidates → 200 `[]` |
| Reasons | Positive genre/plot Personal explanations; disabled profile-weight fallback; typed lowercase wire fields |
| Candidates / blend | All-library beyond `CandidatePoolSize`; Not Interested exclusion before scoring; `S = 0` literal blend; watched penalty; negative finite score preservation |
| MMR | Selection order changes without mutating scores/reasons; uniqueness; min-size; deterministic ordering/ties; diverse sequel fixture; FsCheck properties required |
| Cache | Repeated reads and playback/feedback invalidation consume DEV-415 behavior — do not rebuild its cache |
| Integration / contract | Cancellation propagation; per-movie route regression; typed wire output; host route-equality + additive for-you `ManifestContracts` decorator-resolution row; no OpenAPI drift; generated `types.ts` and `handlers.ts` consistency |
| Persistence | Real PostgreSQL/Testcontainers for data-dependent persistence, vectors, and cache-invalidation route tests; never mock driver translation/serialization; never fake vectors for acquisition proof |

## Gate expectations (Q8; Build workspace — not executed here)

**Phase A (now)**: specification drafts only. No production tests or gates run for pass claims. After this draft: Keel read-only speckit-analyze + brief conformance → plan challenge → **separate** plan-freeze and Gate 1 decisions remain outstanding (brief Q9).

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

## Dependency-aware order (Q1, Q7–Q9)

1. Phase A drafting (this plan); subsequent read-only analysis, plan challenge/freeze, and Gate 1 remain separate and unclaimed.
2. **First Phase B tasks.md phase**: Wisp read-only pickup STOP (merged spec, Epic 7, landed DEV-353/354/415/416, real vector/acquisition inputs, exact signatures, coherent history support, full-library source, typed-reason wire/types plus RelatedMovies consumers/tests/mock-fixture coherence, drift vs Phase A base). Gauge baseline measurements are separate. Missing support → bounded recon + cited Patron amendment; never new port/adapter by guess; typed-reason incoherence → ownership recon + cited Patron decision before out-of-ledger edits.
3. Anvil: Core query/handler orchestration + Infrastructure validator + unit/FsCheck; commit before reporting.
4. Anvil: thin API route/registration (including explicit validator DI) + real integration coverage including host route-equality / additive `ManifestContracts` row; commit before reporting.
5. Anvil: opt-in OpenAPI/drift-snapshot export + `generate:api` for `types.ts` and `handlers.ts`; commit before reporting.
6. Cog refactor, Gauge applicable gates, all three review axes, remediation within Q3 cap — at Build workspace stages.

## Phase B STOP prerequisites (mandatory — first tasks.md phase)

Before any source implementation, Wisp verifies read-only:

1. Merged specifications for this feature’s required contract set.
2. PRODUCT section 2 Epic 7 prerequisite satisfied.
3. Landed DEV-353, DEV-354, DEV-415, and DEV-416 contracts in src (not draft-only).
4. Real vector/acquisition dependencies available (candidate and contributor 384-vector surfaces as required by predecessors). At this Phase A base, playback event/write/persistence and movie-feature vector record/column/snapshot/repository exist (recon-DEV-357-2:50-60); do not infer complete all-library acquisition, a confirmed `IMovieFeaturesRepository` interface signature, a history-read contract, or runtime readiness (recon-DEV-357-2:56,60,62). Named DEV-353/354/415/416 dependency STOP and actual vector/acquisition pickup proof remain mandatory.
5. Fresh bounded drift recon against Phase A base `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`.
6. Exact landed predecessor signatures identified (not guessed).
7. Coherent history-predicate support from the same underlying history used for profile calculation.
8. All-library summary/feedback/384-vector acquisition source available without inventing a port.
9. Landed typed-reason wire/types, existing RelatedMovies consumers/tests, and mock fixtures are coherent with predecessor typed reasons; if not → STOP for bounded ownership recon and a cited Patron decision before out-of-ledger changes (brief.md:106; recon-DEV-357-2:41-44). Do not assign unproven predecessor ownership, send fixes to DEV-418, authorize UI work, or waive web gates.

If any item fails: **STOP**. Forbidden: substitute algorithms, production stubs, dependency waiver, invented signatures, ad hoc endpoint cache, silent new port/adapter, out-of-ledger typed-reason/UI edits without cited Patron decision.

## Open drafting items (not inventable here)

- **needs recon (Phase B pickup only)**: Exact landed signatures for DEV-353/354/415/416 and all-library/history/vector acquisition surfaces against then-current main. Named predecessor runtime contracts remain unlanded at Phase A base; playback/vector surfaces exist per recon-DEV-357-2:50-60 but history-read, complete all-library acquisition, confirmed `IMovieFeaturesRepository` signature, and runtime readiness are not established (:56,60,62).
- **needs recon (Phase B pickup only)**: Exact OpenAPI regeneration and `web generate:api` invocation from landed configuration (writer name, `LAMUFLIX_REGENERATE_OPENAPI=true`, and both outputs `types.ts` / `handlers.ts` are already recorded; do not invent additional flags).
- **Resolved (do not re-open as needs decision)**: Q1–Q9 Patron rulings in brief.md / CONCLUSIONS.md (dependency STOP, HTTP/DTO, review bar, candidate/`S=0`, HasHistory/cache boundary, MMR/degenerate reasons, file set including Infrastructure validator and host/MSW amendments, tests/gates/order, grill closure).

Unresolved semantic product choices beyond this brief → `needs decision` to Keel/Patron; do not silently assume or widen scope.
