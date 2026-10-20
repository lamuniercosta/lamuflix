# Implementation Plan: DEV-341 GET /api/movies/{id}/recommendations with OpenAPI

**Branch**: `feature/341-spec` | **Date**: 2026-10-20 | **Spec**: `specs/DEV-341/spec.md`

**Input**: Feature specification from `specs/DEV-341/spec.md`, settled grill brief `specs/DEV-341/brief.md`, facts from notes `recon-DEV-341` and `recon-DEV-341-2`, Patron rulings in `specs/DEV-341/CONCLUSIONS.md`, taste receipt `specs/DEV-341/ASSUMPTIONS.md`, `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains gated on Epic 7 plus landed DEV-340 and DEV-336/337/338/339 (`specs/PRODUCT.md` sequencing; brief Q1/Q7).

## Summary

Deliver the HTTP adapter for `GET /api/movies/{id:int}/recommendations`: raw take validation against configured `CandidatePoolSize`, selected-movie preflight with Pending-only empty short-circuit, lossless mapping of DEV-340 `RecommendedMovie` to flat `RecommendedMovieDto` (string reasons), endpoint registration, focused HTTP and real-Postgres proofs, OpenAPI transformer, committed baseline/snapshot regeneration, generated TypeScript/MSW, typed fixture/typecheck, and focused MSW smoke. Consume DEV-340/DEV-338 unchanged. Nothing else ships.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| Recommendation | root `CONTEXT.md` | Suggested library movie from the details page |
| Reason | root `CONTEXT.md` | Caller-safe explanation; wire form is `Reason.Text` strings |
| RecommendedMovieDto | brief Q4 | API representation only — not a new glossary term |

## Technical Context

**Language/Version**: C# / .NET 10 Minimal API in `LamuFlix.Api`; generated TypeScript/MSW under `web/`.

**Primary Dependencies**: None added. Consume existing ASP.NET Core TypedResults/`Results<>`, Options (`RecommendationOptions`), DEV-340 query/handler contracts, DEV-338 `Reason`, and existing OpenAPI transformers / generation scripts. Tests describe the approved constitution IX stack — xUnit v3, Shouldly, NSubstitute, WebApplicationFactory, Testcontainers Postgres, AutoFixture, and Faker.Net — without claiming new package installs or verified project references.

**Storage**: Existing movie persistence via `IMovieRepository` preflight only. No migration or schema change. Ranking/candidate SQL remains in landed DEV-340.

**Testing**: `RecommendationEndpointTests` (in-process WebApplicationFactory; substituted handler; repository fake), `ApiEndToEndRecommendationTests` (real Postgres `ApiEndToEndCollection`), OpenAPI contract/drift/determinism tests, web `check:api` / typecheck / focused MSW smoke. Approved constitution IX conventions: xUnit v3, Shouldly, NSubstitute, WebApplicationFactory, Testcontainers Postgres, AutoFixture, and Faker.Net; fresh data per case; Theory + MemberData for repeated take matrices; hand-written builders only for a named domain invariant AutoFixture cannot satisfy. No new dependency claim and no verified package-reference claim — Phase B T001 confirms accessibility. No EF InMemory. No driver mocks for translation/serialization. No Gherkin/Reqnroll. **propertyTests: opt-out** per task note DEV-341 / brief Q7 (HTTP parsing/status/mapping covered by endpoint integration tests; scoring/ranking invariants stay upstream). Gate bar per brief Q7 Gauge expectations and `AGENTS.md` — do not invent unsupported analyzer specifics.

**Target Platform**: Existing `LamuFlix.Api` host and committed OpenAPI → TS contract chain. No new host. ui:none (no product UI).

**Project Type**: Existing ports and adapters, feature-organised core solution. HTTP endpoints live under `src/LamuFlix.Api/Endpoints/` (recon-DEV-341:44; constitution I). Recommendations Core feature folder is owned by landed upstream tickets; this ticket does not add Core ranking types.

**Performance Goals**: No numeric latency target in the brief. Accepted cost: two selected-movie reads for non-Pending requests with no cross-read snapshot (brief Q3).

**Constraints**: No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No new port or ADR. No schema/migration. No `Features:LocalPlay`, secrets, or `Process.Start`. No UI. No MMR, embeddings, or pgvector. Do not stub or reimplement DEV-340/338. Thresholds stay in `harness.yml`. Preserve `propertyTests: opt-out` disposition. Follow established conventions and mandatory gate scripts per brief Q7 / `AGENTS.md`.

**Scale/Scope**: Exactly the Q6–Q7 file envelope (three new production files, three new test files including MSW smoke, named forced edits and generated outputs). Frozen scope sentence (brief Q8): DEV-341 covers only the ticket GET `/api/movies/{id:int}/recommendations` HTTP adapter, take and id policy, Pending preflight, Q4 DTO/string-reason mapping, registration, specified HTTP/Postgres/contract proofs, and forced OpenAPI/snapshot/generated TS/MSW/fixture/typecheck changes within Q6-Q7 files; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Per `constitution.md:551`, every gate evaluated in this Constitution Check cites its supporting numbered principle; separate scope and prerequisite checks follow below with their own authority citations.*

- Endpoints grouped per feature via `MapRecommendationEndpoints`; TypedResults + ProblemDetails; committed OpenAPI + Verify drift + generated TS/MSW never hand-written (constitution V for ProblemDetails / consistent error responses; API and Contract Rules for `MapXxxEndpoints` and committed OpenAPI → generated TS/MSW at `.specify/memory/constitution.md:485-494`; Technology Stack Constraints for TypedResults/`Results<>`; attributed historical source `recon-DEV-341:70`; charter contract chain).
- Constitution V 400-vs-422 layers (Patron F1, no departure): HTTP query parse/binding/multiplicity and adapter `1..RecommendationOptions.CandidatePoolSize` range guards return 400 ProblemDetails via `BadHttpRequestException` before `MovieId` / `GetRecommendationsQuery` construction; Core request contracts remain validated through FluentValidation in the validation decorator, with `ValidationException` → 422 unchanged (`constitution.md:217-234`; `brief.md:129-135,349-363`; ticket `DEV-341:14`). Zero owner checkboxes.
- No new port; consume existing `IMovieRepository` and landed DEV-340 handler. Architecture port whitelist / ADR rules unchanged (constitution I; recon-DEV-341:68–69; brief Q1). ArchitectureTests enforcement named at `constitution.md:140-141` is included under the final Phase B `dotnet test` bar (T018); this gate records inclusion only and does not claim a fresh suite run here.
- Enrichment status is an existing read of `EnrichmentStatus` for Pending short-circuit and non-Pending dispatch only; this ticket adds no enrichment state transitions (constitution IV; `constitution.md:190-211`; spec FR-005).
- Dispatch uses the existing decorated handler registration path; no duplicate host wiring of tracing/logging/validation decorators (constitution VI; `constitution.md:239-258`; brief Q1). Existing tracing/registration remain Phase B recon/proof obligations, not freshly verified facts in this plan.
- No MediatR, generic repository, or UnitOfWork (PRODUCT.md; constitution II).
- No Core feature-folder cross-references introduced by this HTTP adapter (constitution I).
- No secret, `Features:LocalPlay`, or `Process.Start` (constitution VII; care list item 5; recon-DEV-341:138).
- Vocabulary: **Recommendation** and **Reason** already in root `CONTEXT.md`; no new glossary term; no ADR edit (constitution VIII; brief Q8; Q5 noted, no ticket).
- Tests: approved constitution IX stack — xUnit v3, Shouldly, NSubstitute, WebApplicationFactory, Testcontainers Postgres, AutoFixture, and Faker.Net; fresh test data per case; Theory + MemberData for repeated take matrices over repeated Facts; hand-written builders only for a named domain invariant AutoFixture cannot satisfy (`.specify/memory/constitution.md:318-325,392-393`; brief round-2 M1 decision at `brief.md:425` / `CONCLUSIONS.md:286`; brief Q7). Gates from configured scripts using harness thresholds only; never lower thresholds or invent a threshold value.

Post-design re-check: the numbered-principle Constitution Check gates above still hold (V, I, II, IV, VI, VII, VIII, IX). This plan adds no package, project, new port name, CONTEXT.md edit, ADR, or file outside the approved Q6–Q7 set. `RecommendedMovieDto.Reasons` is `ImmutableList<string>` per Keel C1-T1 / Coding Conventions (`constitution.md:429`). Scope, schema/migration, and epic-sequencing constraints remain in force under **Scope and prerequisite checks** below; they are not waived by this re-check and are not Constitution Check gates. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `design.md`) are not produced — the brief already freezes contracts, files, ordering, and proofs.

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints (Keel C2-T1). **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Placement change only — constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-341 Scope & Technical Design and Acceptance Criteria (recon-DEV-341-2:26–41; brief Q1). Nothing is added or dropped. No owner checkbox (brief Q8 / C1-T1).
- No schema or migration (care list item 3; brief Q1).
- Epic 9 recommendations sequencing: Phase B waits for Epic 7 (`specs/PRODUCT.md` §2; brief Q1). Full Phase B stop condition: user-merged DEV-341 spec PR / Gate 1, Epic 7 completion, landed DEV-340 and DEV-336/337/338/339 on main, then Wisp prerequisite/drift recon; stop on missing prerequisites or contract divergence (brief Q1; plan Phase boundaries). This is a prerequisite statement, not evidence that dependencies have landed.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-341/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `design.md` are not produced. `AGENTS.md` is not updated: outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs          # new
src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs              # new
src/LamuFlix.Api/RecommendationsOperationTransformer.cs        # new
src/LamuFlix.Api/Endpoints/ApiEndpoints.cs                     # compose mapper
src/LamuFlix.Api/Program.cs                                    # register transformer
src/LamuFlix.Api/HandlerRegistration.cs                        # conditional only after drift recon

tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs              # new
tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs           # new
tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs                     # forced update
tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs                  # route inventory
tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs                    # name inventory
tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json  # regenerate

web/src/api/openapi.json                    # regenerate
web/src/api/types.ts                        # generate:api
web/src/mocks/handlers.ts                   # generate:api
web/src/mocks/fixtures.ts                   # GetRecommendations: []
web/src/api/client.typecheck.ts             # typed exercise
web/src/mocks/recommendations.test.ts       # new MSW smoke
```

**Structure Decision**: Match existing per-feature endpoint files under `src/LamuFlix.Api/Endpoints/` and the established OpenAPI → TS/MSW chain under `web/src/api` and `web/src/mocks` (recon-DEV-341 §§3,6). No new project or Core Recommendations types in this ticket.

## Design (from settled brief)

1. **HTTP take/id policy (Q2 / F1)**: Parse raw take; default 10; inclusive `1..CandidatePoolSize`; invalid parse/binding/adapter range → 400 ProblemDetails via `BadHttpRequestException` (distinct from Core FluentValidation decorator `ValidationException` → 422); take before id/preflight/handler; nonpositive id → 404 without dispatch.
2. **Pending preflight (Q3)**: `IMovieRepository.GetAsync` after validation; null → 404; Pending → 200 `[]` without handler; other enrichment states → `GetRecommendationsQuery`; cancellation checked before preflight I/O, after preflight await, and once at the shared post-handler/pre-mapping boundary (no duplicate check solely to mirror wording; round-2 decision appendix `brief.md:410-437` / `CONCLUSIONS.md:271-298`, Compass L2 at `:435` / `:296`); errors propagate; accepted double-read cost.
3. **DTO mapping (Q4–Q5 / C1-T1)**: Flat twelve-field `RecommendedMovieDto` with `ImmutableList<string> Reasons`; map ordered `Reason.Text` via `ToImmutableList()`; explicit empty fixtures use `ImmutableList<string>.Empty` (never null/default); lossless Score and rank order; preserve zero-score empty and nonempty reason arrays; JSON/OpenAPI/generated TypeScript wire form remains ordered `string[]`; outer `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>` unchanged; no synthetic explanations.
4. **OpenAPI + generation (Q6, Q9 / F3)**: Operation transformer for this operation only. Take metadata: optional integer, default 10, minimum 1, and the exact fixed description prose `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.` No numeric maximum and no runtime interpolation; the transformer receives no runtime `RecommendationOptions` for this description; endpoint validation still reads the configured bound. Regenerate baseline+snapshot together; then `npm run generate:api`; fixture + typecheck + `recommendations.test.ts` smoke (Q9 path is technical/file-envelope placement, not taste).
5. **Proofs and order (Q7 / F2)**: Five Phase B steps — (1) Wisp prerequisite/drift recon + Gauge baseline; (2) DTO/endpoint/registration + `RecommendationEndpointTests`; (3) real-Postgres e2e + inventories; (4) transformer + OpenAPI/TS/MSW + fixture/typecheck/smoke; (5) Cog refactor + Gauge gates. At intermediate phase commits, only the named targeted proofs gate; expected inventory/drift failures are bounded and close at **T008** (`ApiHostCompositionTests` route inventory), **T009** (`LibraryEndpointsTests` name inventory), **T012** (`OpenApiContractTests` operation/schema expectations), and **T013** (committed baseline/snapshot drift). T001 confirms the actual affected tests after drift — do not assume whole classes fail. Report expected failures as expected failing/deferred with owner and closing task, never PASS/SKIPPED. Phase 3 closes route/name failures; Phase 4 and T018 leave no deferred failures. Unexpected failure, baseline regression, prior targeted failure, overdue deferral, or unconfirmed failure stops for evidence/cited decision. Targeted tests also run with the build steps they prove.
6. **Loop discipline (Q8)**: Closing bar Critical/High/Medium; Sentry/Ledger/Compass mandatory; two formal rounds per applicable loop; frozen scope sentence above.

## Complexity Tracking

> No constitution violations requiring justification. C1 closed by Keel C1-T1: `ImmutableList<string> Reasons` satisfies Coding Conventions collections-in-records (`constitution.md:429`) while preserving the ticket outer `Ok<List<RecommendedMovieDto>>` and JSON `string[]` wire contract. Patron F1 closes Constitution V without departure: HTTP adapter parse/binding/take-range `BadHttpRequestException` → 400 is distinct from Core FluentValidation decorator `ValidationException` → 422 (`constitution.md:217-234`; `brief.md:129-135`). Endpoint-local Pending policy and take parsing are ticket-mandated HTTP adapter work, not new architectural layers.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-341` only. No freeze, publish, gates, or implementation.
- **Phase B**: After user-merged spec PR (Gate 1), Epic 7, landed DEV-340/336–339, and Wisp prerequisite/drift recon with stop-on-divergence. Then Anvil implements per `tasks.md`; Cog refactor; Gauge gates.
