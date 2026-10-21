# Tasks: DEV-341 GET /api/movies/{id}/recommendations with OpenAPI

**Input**: Design documents from `specs/DEV-341/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Brief Q7 requires them. Author tests in the same phase as the types they lock. Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green and the phase is committed before the next phase starts. At intermediate phase commits, only those named targeted proofs gate — including all targeted prior-phase proofs — not the entire suite. Expected inventory/drift failures after route registration are bounded and close only at **T008** (`ApiHostCompositionTests` route inventory), **T009** (`LibraryEndpointsTests` name inventory), **T012** (`OpenApiContractTests` operation/schema expectations), and **T013** (committed OpenAPI baseline / Verify snapshot drift). T001 confirms the actual affected tests/conditions after upstream drift; do not assume every test in any named class fails. Report each deferred condition as **expected failing/deferred** with owner (Anvil) and closing task — never PASS or SKIPPED. Unexpected failure, baseline regression, failed targeted prior-phase proof, deferred failure beyond its closure point, or unconfirmed expected-failure set stops for evidence and a cited decision. Phase 3 closes route/name inventory failures; Phase 4 and T018 leave no deferred failures. Do not start Phase 2 production until Phase 1 drift recon passes, the spec PR has merged, Gate 1 is established, and Phase B prerequisites (Epic 7 + landed DEV-340/336–339) allow build intake.

**Organization**: Phases follow brief Q7 Phase B task order and `spec.md` user stories US1–US3. Anvil implements one phase per ask. Polish is Cog then Gauge.

**Domain vocabulary** (must remain aligned with root `CONTEXT.md`): **Recommendation**, **Reason** — definitions in `spec.md` Domain vocabulary. No new glossary edit. `RecommendedMovieDto` is API-only.

**propertyTests**: opt-out — no new domain invariants; HTTP query parsing, status precedence, and flat DTO mapping are covered by endpoint integration tests while scoring/ranking invariants belong to DEV-338/DEV-340 (task note DEV-341; brief Q7). Accepts no-tests-tagged exit 2 only; actual failure still blocks.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 ranked recommendations JSON; US2 take/id/Pending status contracts; US3 OpenAPI/TS/MSW chain.
- Exact file paths are in each description.

## Phase 1: Setup — prerequisite drift recon

**Purpose**: Confirm Epic 7 sequencing and landed dependency contracts before production work.

- [ ] T001 Wisp prerequisite/drift recon against main: confirm Epic 7 completion gate; landed DEV-340 and DEV-336/337/338/339 on main; compatibility of consumed signatures (`GetRecommendationsQuery(MovieId, int Take = 10)`, `IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>`, `RecommendedMovie` / `MovieSummary` fields, `Reason.Text`, `RecommendationOptions.CandidatePoolSize`); whether `HandlerRegistration.cs` already registers the DEV-340 handler/validator; shared registration/operation/route/name inventories; existing ApiEndToEnd seed/fixture helpers; Vitest/MSW conventions and collision check for `web/src/mocks/recommendations.test.ts`. Confirm Phase B usable existing AutoFixture/Faker.Net conventions and accessibility for the approved constitution IX stack (constitution membership does not prove integration-project package references); if unavailable, stop for needs recon/cited decision — never install a package, invent a helper API, or add a new test-data file. Record that `EnrichmentStatus` is the existing SmartEnum and that existing name-converter / JSON name serialization reuse (same presentation path as MovieSummaryResponse) is available — stop on type or converter divergence; add no new converter and do not replace the upstream type. Confirm the **actual** affected inventory/drift tests/conditions after upstream drift for the expected deferred set (route inventory → T008, name inventory → T009, OpenAPI operation/schema → T012, baseline/snapshot drift → T013); do not assume whole classes fail. Record Gauge baseline. Stop for a cited decision on missing prerequisites or any signature/placement/type/converter divergence. No production edits in this task.

**Checkpoint**: Prerequisites match the brief; Phase B build intake is allowed.

---

## Phase 2: Foundational — DTO, endpoint adapter, and HTTP proofs (US1 + US2)

**Purpose**: API DTO, Minimal API endpoint, mapper composition, conditional handler registration, and focused WebApplicationFactory proofs for ranked mapping and status precedence.

**⚠️ CRITICAL**: US3 OpenAPI generation waits on a working endpoint. E2E and inventory updates follow this phase.

### Tests

- [X] T002 [P] [US1] [US2] Write `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` using WebApplicationFactory with substituted `IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>` and existing-port `IMovieRepository` fake (recon-DEV-341:125 pattern): omitted take = 10 despite configured `RecommendationOptions.Take` override; inclusive 1 and configured `CandidatePoolSize`; configured bound changes; default 10 rejected when it exceeds a smaller valid pool; empty/whitespace/malformed/overflow/repeated/out-of-range take → 400 ProblemDetails; invariant sign/surrounding whitespace accepted; take validated before id conversion, missing lookup, and Pending handling; nonpositive and unknown id → 404 without handler dispatch; Pending → 200 `[]` with no recommendation dispatch; Enriched/Failed/NotFound dispatch; exact twelve-field camelCase payload including nullable presentation values and EnrichmentStatus string enum; preserved handler list order even if deliberately different from score sort; Score and exact `Reason.Text` strings for positive-score nonempty, zero-score nonempty underflow, and zero-score empty; cancellation at preflight/handler boundaries and dependency error propagation with request token forwarding and absence of forbidden dependency calls. Use Theory + MemberData for repeated take/default/bound/precedence matrices where the assertion structure repeats; do not remove any case or combine unlike proofs into one large theory — distinct cancellation, mapping, and infrastructure proofs may remain Facts. Build object graphs with AutoFixture and realistic titles/names/years with Faker.Net, allocating fresh data per case; pin explicit take strings, configured bounds, ids, expected statuses, ordered scores, and exact reason texts as contract stimuli/expected values (customize AutoFixture objects to those states). Hand-written builders only for a named domain invariant AutoFixture cannot satisfy — name both invariant and limitation in task/implementation evidence; Pending state or zero-score alone is not a blanket exemption. Method names `Method_Condition_Expected`. AAA headers only. Shouldly. NSubstitute. Preserve existing cancellation and token-forwarding assertions.

### Implementation

- [X] T003 [P] [US1] Add `src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs` as `sealed record RecommendedMovieDto(int Id, string Title, string? PosterUrl, int? ReleaseYear, int? Runtime, decimal? ImdbRating, EnrichmentStatus EnrichmentStatus, bool IsInWatchlist, bool IsWatched, bool IsNotInterested, double Score, ImmutableList<string> Reasons)` (`using System.Collections.Immutable`) per brief Q4 / Keel C1-T1. Materialize ordered `Reason.Text` with `ToImmutableList()`; explicit empty fixtures use `ImmutableList<string>.Empty`; never null or default. Preserve outer `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>`, all twelve fields, rank order, and JSON/OpenAPI/generated TypeScript `string[]` contract.

- [X] T004 [US1] [US2] Add `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` with `MapRecommendationEndpoints`, operationId `GetRecommendations`, summary `Get recommendations for a library movie`: raw take parsing and Q2 precedence; MovieId policy; `IMovieRepository` preflight and Pending short-circuit (Q3); dispatch `GetRecommendationsQuery` for non-Pending; local lossless DTO mapping (Q4–Q5); return `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>`.

- [X] T005 [US1] Compose `MapRecommendationEndpoints` from `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`.

- [X] T006 [US1] Conditionally update `src/LamuFlix.Api/HandlerRegistration.cs` only if T001 proved the DEV-340 handler/validator registration is missing. Reuse landed registration patterns without duplication or Core validator change. If already registered, leave the file untouched.

**Checkpoint**: Named targeted proofs only — `RecommendationEndpointTests` green for US1 mapping and US2 status contracts. Compile-red permitted while introducing types inside the phase; green/committed for the targeted set before Phase 3. After T005 route registration, expected deferred failures (confirmed by T001, not assumed class-wide) may remain: `ApiHostCompositionTests` route inventory → closes **T008**; `LibraryEndpointsTests` name inventory → closes **T009**; `OpenApiContractTests` operation/schema expectations → closes **T012**; committed baseline/snapshot drift → closes **T013**. Report each as expected failing/deferred with owner Anvil and closing task — never PASS/SKIPPED. Unexpected failure, baseline regression, or unconfirmed deferred set stops for evidence/cited decision.

---

## Phase 3: User Story 1/2 - Real-Postgres composition and inventories

**Goal**: Seeded production route/DI/handler path plus actual post-drift route/name inventories.

**Independent Test**: Real-Postgres e2e ranked JSON/reasons, missing 404, Pending 200 empty; composition/name inventories include the new route/operation without hardcoding intake counts.

### Tests

- [X] T007 [US1] [US2] Write `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` on existing `ApiEndToEndCollection` / real-Postgres fixture: seeded eligible movie returns ranked JSON with reasons; unknown id → 404; Pending → 200 empty array. Use seed/fixture helpers confirmed in T001; feed fresh AutoFixture/Faker.Net data through those existing helpers. Theory + MemberData only for genuinely repeated scenarios; distinct integration proofs may remain Facts. Hand-written builders only for a named domain invariant AutoFixture cannot satisfy — name both invariant and limitation in evidence; Pending or zero-score alone is not a blanket exemption. No driver mocks for translation or serialization. Method names `Method_Condition_Expected`. AAA only. Shouldly.

### Implementation / forced inventory updates

- [X] T008 [P] [US1] Update `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` route inventory to the actual post-drift set including the recommendations route.

- [X] T009 [P] [US1] Update `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` shared endpoint-name inventory only to include `GetRecommendations` (no unrelated metadata rewrites).

**Checkpoint**: Named targeted proofs — `ApiEndToEndRecommendationTests` green; `ApiHostCompositionTests` route inventory (**T008**) and `LibraryEndpointsTests` name inventory (**T009**) green, closing those deferred failures. Prior-phase targeted proofs (`RecommendationEndpointTests`) remain green. Only the explicitly deferred OpenAPI operation/schema (**T012**) and committed baseline/snapshot drift (**T013**) may remain expected failing/deferred (owner Anvil; never PASS/SKIPPED). Unexpected failure, baseline regression, overdue route/name deferral, or unconfirmed deferred set stops for evidence/cited decision.

---

## Phase 4: User Story 3 - OpenAPI transformer, generation, and web proofs

**Goal**: Accurate OpenAPI metadata, committed baseline/snapshot, generated TS/MSW, typed fixture/typecheck, and focused MSW smoke.

**Independent Test**: OpenApiContractTests for GetRecommendations parameter/response/schema/drift/determinism; `check:api`; client typecheck; `recommendations.test.ts` 200 `[]`.

### Implementation

- [X] T010 [US3] Add `src/LamuFlix.Api/RecommendationsOperationTransformer.cs` documenting this operation only: optional integer take default 10 / minimum 1 / exact fixed description prose `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.`; 200 DTO array; 400/404 ProblemDetails. No numeric maximum and no runtime interpolation; the transformer receives no runtime `RecommendationOptions` for this description; endpoint validation still reads the configured bound. Leave other operations unchanged.

- [X] T011 [US3] Register the transformer in existing `AddOpenApi` wiring in `src/LamuFlix.Api/Program.cs`.

- [X] T012 [US3] Update `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` assertions for GetRecommendations operation, optional integer take metadata including the exact fixed description prose `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.` (no numeric maximum / runtime interpolation), 200 array of exact DTOs, string reasons, EnrichmentStatus string enum, 400/404 ProblemDetails and media types, deterministic generation/drift, no health leak, and production documentation 404. Align expected operation/schema lists to the actual post-drift set. This task closes the deferred OpenAPI operation/schema expectation failures.

- [X] T013 [US3] Regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together via opt-in `LAMUFLIX_REGENERATE_OPENAPI=true`. Then run `npm run generate:api` in `web/` to regenerate `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. Never hand-edit generated files or rewrite the generator. This task closes the deferred committed baseline/snapshot drift failures.

- [X] T014 [P] [US3] Add typed `GetRecommendations: []` to `web/src/mocks/fixtures.ts`.

- [X] T015 [P] [US3] Add typed endpoint exercise for the new route in `web/src/api/client.typecheck.ts`.

- [X] T016 [US3] Add `web/src/mocks/recommendations.test.ts`: import generated handlers under existing Vitest/MSW conventions confirmed in T001; request the new route; assert 200 empty array using the Q6 typed fixture. Stop for a cited decision if T001 found a placement/convention mismatch.

**Checkpoint**: OpenAPI drift green; generate:api/check:api green; MSW smoke green. No deferred inventory/drift failures remain after T012/T013.

---

## Phase 5: Polish — Cog refactor and Gauge gates

**Purpose**: Diff refactor then full Phase 3 step 5 gate bar.

- [ ] T017 Cog `/refactor` on the delivery diff only (complexity helpers/early returns/guards; no scope expansion; no threshold changes).

- [ ] T018 Gauge gates on the diff (never `-All` unless scripted otherwise): `./scripts/run-roslyn-analyzers.ps1`; normal and refactor-stage cyclomatic complexity via `./scripts/run-cyclomatic-complexity.ps1` using each check's threshold configured in `harness.yml` through the supported harness invocation (exact invocation belongs to Gauge Phase B from actual script help/config — if unsupported or unestablished, stop and report **Could not run**; never substitute a threshold number, invent a flag/key/value, lower a threshold, or omit the check); `./scripts/run-jetbrains-inspectcode.ps1`; `./scripts/run-property-tests.ps1` (opt-out exit 2 no-tests-tagged only); `./scripts/run-vulnerable-packages.ps1`; `dotnet format --verify-no-changes`; `dotnet test` (includes ArchitectureTests enforcement under constitution I); `scripts/run-mutation.ps1` without `--since` (mutation no-eligible-project or unestablished gate invocation stops for a cited decision / **Could not run** — no threshold waiver, extra mutation project, duplicate cancellation check, or pre-judged mutant equivalence); `scripts/run-web-gates.ps1` including `check:api`. Report SKIPPED (scope-empty) / SKIP / NOT APPLICABLE verbatim — never as PASS. Exit 1, Could not run, or unaccepted property exit 2 blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add an ADR. No deferred inventory/drift failures may remain at this checkpoint.

**Checkpoint**: Delivery ready for review loops under brief Q8 closing bar (Critical/High/Medium; two formal rounds; Sentry/Ledger/Compass). No deferred failures remain.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — Wisp recon must pass or stop for decision.
- **Phase 2 (Foundational US1/US2 HTTP)**: Depends on Phase 1; blocks e2e/OpenAPI generation that assume the route exists.
- **Phase 3 (E2E + inventories)**: Depends on Phase 2.
- **Phase 4 (US3 OpenAPI/web)**: Depends on Phase 2 endpoint + transformer wiring; inventory expectations should match Phase 3 updates.
- **Phase 5 (Polish)**: Depends on Phases 2–4 complete.

### User Story Dependencies

- **US1 (P1)**: Ranked JSON mapping — Phase 2 implementation + T002/T007.
- **US2 (P2)**: take/id/Pending contracts — same Phase 2 endpoint + T002/T007 (no separate production files).
- **US3 (P3)**: OpenAPI/TS/MSW — Phase 4; requires US1 route to exist.

### Parallel Opportunities

- T003 can proceed in parallel with early T002 scaffolding once types are named.
- T008/T009 inventory updates can run in parallel after the route exists.
- T014/T015 fixture and typecheck can run in parallel after generate:api (T013).

### Parallel Example: Phase 4 web fixtures

```text
Task: "Add typed GetRecommendations: [] to web/src/mocks/fixtures.ts"
Task: "Add typed endpoint exercise in web/src/api/client.typecheck.ts"
```

---

## Implementation Strategy

### MVP First (US1 + US2 HTTP adapter)

1. Complete Phase 1 recon (stop on divergence; confirm actual deferred inventory/drift tests and EnrichmentStatus SmartEnum / name-converter reuse).
2. Complete Phase 2 DTO/endpoint/HTTP proofs (targeted `RecommendationEndpointTests` only; expected inventory/drift failures deferred to T008/T009/T012/T013).
3. **STOP and VALIDATE**: RecommendationEndpointTests independently.
4. Add Phase 3 real-Postgres + inventories (T008/T009 close route/name deferrals; OpenAPI deferrals may remain until T012/T013).
5. Add Phase 4 contract chain (T012/T013 close remaining deferrals; no deferred failures remain).
6. Phase 5 Cog + Gauge (T018 leaves no deferred failures; configured harness thresholds only).

### Incremental Delivery

1. Setup recon → foundation ready or blocked with cited decision.
2. HTTP adapter + focused proofs → MVP status contracts + ranked JSON (substituted handler); gate only named targeted proofs.
3. Real-Postgres composition proof + route/name inventory closure (T008/T009).
4. OpenAPI/generated client freshness (T012/T013 close operation/schema and baseline/snapshot deferrals).
5. Refactor + gates.

### Notes

- [P] tasks = different files, no unfinished dependency.
- Do not invent seed helper APIs, HandlerRegistration edits, or MSW placement alternatives beyond brief Q6–Q9 — T001 decides those facts.
- Do not commit secrets; do not touch LocalPlay / Process.Start.
- Preserve task-note `propertyTests: opt-out` disposition; do not add FsCheck suites for this ticket.
- Frozen scope (brief Q8) governs review findings; Q5 remains `noted, no ticket`.
