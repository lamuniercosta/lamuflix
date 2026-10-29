# Tasks: DEV-425 Director facet and Director filter

**Input**: `specs/DEV-425/spec.md`, `plan.md`, frozen `brief.md` (Q1–Q8), `CONTEXT.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`; notes `DEV-425`, `recon-DEV-425`

**Prerequisites**: plan.md (required), spec.md (required). No research/data-model/contracts artifacts — frozen in brief. Palette `design.md` is a separate `ui:` input before plan challenge, not a Quill task here.

**Tests**: Required per brief Q6. Write failing tests before the implementation they drive where TDD applies. Real Postgres for query translation; no mocked EF driver for facet/browse behavior. Seeded property roundtrips in scope.

**Organization**: Phases preserve brief Q6 binding order (Core then Persistence then API then Contract then Web) while merging Core+Persistence into one executable Build phase so `IMovieCatalog.GetDirectorsAsync` and its EF adapter land before the one green checkpoint (task-pipeline Phase 3 step 2). Core dependency order inside that phase is T003 → T005 → T004 → T006 (IDs preserved, not numeric T001–T006 execution). Temporary compile-failure red is allowed while required symbols/port land; after compile closure, behavioral red is required before the green checkpoint. No placeholder/default-throwing implementation. Story tags retain acceptance traceability (US1 facet, US2 browse filter, US3 FilterPanel hosts).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: US1 / US2 / US3
- Include exact file paths

---

## Phase 1: Core and Persistence (Q6 steps 1–2)

**Goal**: `DirectorId` on MovieQuery; MovieQueryString parse/format; port + GetDirectors query/handler; EF director facet projection and `WhereDirector` browse predicate on existing schema.
**Checkpoint**: Core tests green for equality/hash/parse; handler coverage green; real-database facet and browse proofs green; solution compiles with `GetDirectorsAsync` implemented on the EF adapter. No migration.

### Core (Q6 step 1) — dependency order T003, T005, T004, T006 (IDs preserved; not numeric T001–T006)

Compile-failure red is acceptable while introducing required symbols/port; after compile closure, behavioral red evidence is required before the one Phase 1 green checkpoint. No placeholder/default-throwing implementation.

- [X] T001 [P] [US2] Write failing Core tests in `tests/LamuFlix.UnitTests/Library/MovieQueryTests.cs` and `MovieQueryStringTests.cs`: `DirectorId` equality and hash difference; null/default behavior; parse/format roundtrips including seeded property roundtrips. Update `tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs` and `MovieQueryGeneratorTests.cs` so the FsCheck generator emits non-null `DirectorId` (non-vacuous property proof). Touch `MovieQuerySurvivorTests.cs` / `MovieQueryStringSurvivorTests.cs` only if needed for non-null `DirectorId` survivor paths. Cover empty/valid/duplicate/malformed/overflowing `directorId`; unknown identities without extra validator rules (FR-004, FR-005, FR-017; Q2, Q6; PC1)
- [X] T002 [US2] Add `int? DirectorId` to `src/LamuFlix.Core/Library/MovieQuery.cs` (equality and hashing) and singular `directorId` support in `src/LamuFlix.Core/Library/MovieQueryString.cs` per Q2 strict rules so T001 passes (depends on T001; FR-004, FR-005)
- [X] T003 [P] [US1] Extend `src/LamuFlix.Core/Ports/IMovieCatalog.cs` with `DirectorFacet` and `GetDirectorsAsync` (including `CancellationToken`) following existing genre/people facet port shape (FR-002; Q1, Q6)
- [X] T005 [P] [US1] Write failing handler unit coverage in new `tests/LamuFlix.UnitTests/Features/Library/GetDirectorsQueryHandlerTests.cs` following existing GetGenres/GetPeople handler-test convention: empty and passthrough facet results; assert `CancellationToken` forwarded to the catalog port. Edit `tests/LamuFlix.Test/AddHandlerTests.cs` only if needed to assert existing registration; otherwise leave untouched. No mandatory `BrowseMoviesQueryHandlerTests.cs` edit (depends on T003; FR-017; Q6; PC1)
- [X] T004 [US1] Add `src/LamuFlix.Core/Features/Library/GetDirectorsQuery.cs` and `GetDirectorsQueryHandler.cs` following existing facet handlers, forwarding `CancellationToken` to `GetDirectorsAsync`, so T005 passes (depends on T005; FR-002; Q6)
- [X] T006 Update existing catalog test fakes only as required for the new `IMovieCatalog` member to compile (depends on T003; FR-017; Q6)

### Persistence (Q6 step 2) — preserve task order T010–T013

- [X] T010 [P] [US1] Write failing Postgres facet tests in `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs`: orphan exclusion, empty list, distinct linked library movie counts for shared movies, same-name identities, deterministic Name-then-Id order, full-library counts independent of filters (FR-001, FR-003, FR-017; Q1, Q4)
- [X] T011 [P] [US2] Write failing Postgres browse tests in `tests/LamuFlix.IntegrationTests/EfMovieCatalogBrowseTests.cs`: null passthrough (null `DirectorId` returns the query unchanged), matching director, unmatched/zero/negative → empty, AND composition including watchlist, no duplicate movie rows (FR-006, FR-017; Q2, Q3)
- [X] T012 [US1] Implement `GetDirectorsAsync` projection in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` over existing Directors / movie_directors (at-least-one-movie filter, distinct linked library movie count, Name then Id; forward `CancellationToken`) so T010 passes — exact EF expression chosen and proven at Build (depends on T003, T010; FR-002, FR-003; Q6 — no schema/ingest change)
- [X] T013 [US2] Implement `WhereDirector` in `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs` (and wire through `EfMovieCatalog.cs` browse) using existing Directors navigation `Any`; null `DirectorId` returns the query unchanged so T011 passes (depends on T002, T011; FR-006; Q6)

**Checkpoint**: After compile closure and behavioral red, Core contract, handler, and real-database facet/browse proofs green together in one checkpoint; no migration.

---

## Phase 2: API endpoints (Q6 step 3)

**Goal**: Facet endpoint + browse mapping with existing validation/problem handling.
**Independent Test**: FacetEndpointsTests / LibraryEndpointsTests.

- [X] T020 [P] [US1] Write failing API facet tests in `tests/LamuFlix.IntegrationTests/FacetEndpointsTests.cs`: field set/types, empty array HTTP 200, no required role parameter (FR-001, FR-017; Q1)
- [X] T021 [P] [US2] Write failing browse API tests in `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`: director filter applied; absent and empty `directorId` → null (no filter); duplicate raw-query keys, malformed, and overflowing `directorId` → 422 (FR-005, FR-007, FR-017; Q2; PC1)
- [X] T022 [US1] Add `src/LamuFlix.Api/Endpoints/DirectorDto.cs` and register/map `GET /api/directors` in `src/LamuFlix.Api/Endpoints/FacetEndpoints.cs` plus `src/LamuFlix.Api/HandlerRegistration.cs`, forwarding `CancellationToken` through endpoint → handler → port → EF, so T020 passes (depends on T004, T012, T020; FR-001, FR-002, FR-007; Q6)
- [X] T023 [US2] Map singular `string? DirectorId` on `src/LamuFlix.Api/Endpoints/BrowseMoviesRequest.cs` through `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs` using `ReadNumber(rawQuery, "directorId", EmptyMeansNull)` and raw `IQueryCollection` value-count duplicate rejection (before model-bound scalars can hide duplicates), with existing validation/problem handling and cancellation, so T021 passes (depends on T002, T013, T021; FR-005, FR-007; Q2, PC1)

**Checkpoint**: HTTP facet and browse contracts match Q1–Q2.

---

## Phase 3: Generated contract chain (Q6 step 4)

**Goal**: OpenAPI → generated TypeScript/mocks/typecheck; no hand edits.
**Checkpoint**: Generated types expose DirectorDto and browse `directorId`.

- [X] T030 [US1] Regenerate in contract-chain order after T022/T023: established opt-in OpenAPI workflow writes `web/src/api/openapi.json` and Verify drift snapshot together; then regenerate `web/src/api/types.ts` and update generated mocks through the established path; exercise `web/src/api/client.typecheck.ts` coverage; never hand-edit generated files (FR-008, FR-017; Q6; SC-003)
- [X] T031 Confirm OpenAPI drift enforcement (`OpenApiContractTests` / committed baseline) passes without replacing enforcement by regeneration alone (depends on T030; brief Gate expectations)

**Checkpoint**: Web may consume generated DirectorDto and browse params only after T030.

---

## Phase 4: Web FilterPanel and hosts (Q6 step 5) — US3

**Goal**: URL/state, both hosts, combobox interactions, a11y, local loading/error.
**Independent Test**: Vitest/RTL extensions named in brief Q6.
**Note**: Shared `FilterPanel.tsx` / `FilterPanel.test.tsx` edits stay serial. Palette `design.md` may inform placement/visuals but must not change settled behavior.

- [ ] T040 [P] [US3] Write failing `web/src/features/library/filterQuery.test.ts` cases: raw nonempty `directorId` preserved; first occurrence wins on duplicate keys; empty clears/omits; recognized in serialization; malformed raw scalar `abc` parse/serialize/commit roundtrip survives without numeric empty fallback (FR-009, FR-010, FR-017; Q2; PC1)
- [ ] T041 [P] [US3] Write failing new `web/src/library/browseParams.test.ts`: every nonempty raw scalar forwarded verbatim to API params, including malformed `abc` and fractional `12.5`; empty omitted; never a `formatBound`-style empty fallback for `directorId` (FR-009, FR-017; Q2; PC1)
- [ ] T042 [US3] Implement `directorId` in `web/src/features/library/filterQuery.ts` and `web/src/library/browseParams.ts` per Q2 live-web rules (verbatim nonempty raw forward including `abc`/`12.5`; no numeric empty fallback; empty omits) so T040/T041 pass (depends on T040, T041; FR-009, FR-010; do not wire `libraryUrl`)
- [ ] T043 [US3] Write failing FilterPanel tests in `web/src/features/library/FilterPanel.test.tsx`: label/placeholder/empty copy from ASSUMPTIONS.md; select/replace/clear/Clear all; page reset to 1 with other filters preserved; client search (case-insensitive substring, name/id order, ≤8 suggestions, count badges); typing does not change predicate; same-name identities; missing-selection `Director <id>` retained/clearable; query the input as `combobox` role with associated label; assert `aria-expanded=false` when closed and `true` when open; `aria-controls` only when the listbox is mounted; `aria-activedescendant` only when an active descendant exists; listbox/option roles including selected state; Arrow navigation per Q5 without wrap-at-ends; Enter commits the highlighted selection; Escape closes without changing selection or predicate; visible focus; local loading/error/retry copy and disabled suggestions (depends on T042, T030; FR-010–FR-015, FR-017; Q4, Q5; PC1; PC2-R1)
- [ ] T044 [US3] Implement Director single-select combobox in `web/src/features/library/FilterPanel.tsx` consuming generated types (no hand-rolled DTO) so T043 passes (depends on T043, T030; FR-011–FR-015; Q5, Q6)
- [ ] T045 [US3] Wire both Library and Watchlist hosts in `web/src/library/LibraryView.tsx` with independent director facet fetch; narrow `web/src/library/libraryQueries.ts` only if required by existing browse/facet path (depends on T044; FR-011; Q3, Q6)
- [ ] T046 [US3] Extend `web/src/library/browseWatchlist.test.tsx` (and Library host coverage as existing conventions require) for both hosts: URL reload/navigation restore, page reset, independent loading/error/retry, selection retention (depends on T045; FR-010, FR-011, FR-015, FR-017; Q3, Q5)

**Checkpoint**: Both hosts deliver searchable Director filter with settled URL and state behavior.

---

## Phase 5: Polish and pipeline verification

**Purpose**: Cross-cutting confirmation and gates at their pipeline stages (not during Phase A drafting).

- [ ] T050 Confirm no genre/cast behavior changes, no `libraryUrl` wiring, no schema/package/layer/LocalPlay/secrets/Process.Start edits outside Q6 boundary (FR-016; Q6, Q7)
- [ ] T051 Run applicable backend analyzers/complexity/InspectCode/format/tests/property/security/contract-drift on the changed scope at pipeline stages; respect harness thresholds; never report SKIPPED/SKIP/N/A/Could not run as PASS (FR-018; brief Gate expectations)
- [ ] T052 Run web-implement TDD and web gates for the frontend diff at its pipeline stage (brief Gate expectations)
- [ ] T053 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree; survivors require tests, never threshold reduction (FR-018; Q6)
- [ ] T054 Size M review with Sentry, Ledger, and Compass; apply Q7 closing bar and round/fix-commit caps; missing axis is blocked (FR-018; Q7)

**Checkpoint**: Record each applicable gate verdict; Gate 1 / merge approval not implied by this task list.

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 (do not reverse Q6 dependencies). Phase 1 is one Build ask: Core then Persistence before its single green checkpoint.
- T001 then T002; Core port/handler dependency order is T003 → T005 → T004 → T006 (IDs preserved; not numeric T001–T006). Compile-red allowed until symbols close; behavioral red required after compile closure; then the one green checkpoint.
- Within Phase 1 after T003: T010/T011 may run in parallel; T012 after T003+T010; T013 after T002+T011.
- T020/T021 may run in parallel; T022 after T004+T012+T020; T023 after T002+T013+T021.
- T030 after T022 and T023; T031 after T030. Web UI must not consume DirectorDto until T030.
- T040/T041 parallel; T042 after both; T043 → T044 → T045 → T046 serial on shared panel/host files.
- T050–T054 after implementation complete, at pipeline stages.

## Parallel opportunities

```text
# After Core port T003 (still inside Phase 1):
T010 EfMovieCatalogFacetsTests
T011 EfMovieCatalogBrowseTests

# After Phase 1 (Core+Persistence) checkpoint:
T020 FacetEndpointsTests
T021 LibraryEndpointsTests

# After contract:
T040 filterQuery.test.ts
T041 web/src/library/browseParams.test.ts
```

## Implementation Strategy

1. Core DirectorId + GetDirectors in dependency order T003, T005, T004, T006 (compile-red then behavioral-red) then EF facet (distinct linked count + token) and WhereDirector (null unchanged) against real Postgres — one executable phase, one green checkpoint, no stubs.
2. API DTO/endpoints with `string? DirectorId` + EmptyMeansNull + raw-query duplicate count; absent/empty and 422 proofs; CancellationToken end-to-end on facet.
3. Regenerate OpenAPI then TypeScript/mocks/typecheck.
4. Web URL scalar (abc serialize/commit) + browseParams.test.ts verbatim forward + FilterPanel combobox/ARIA + both hosts with Vitest/RTL.
5. Gates and three-axis review at later pipeline stages.

## Traceability (brief Q → tasks)

| Ruling | Tasks |
|---|---|
| Q1 Facet contract | T003–T005, T010, T012, T020, T022, T030 |
| Q2 Query/URL split | T001–T002, T011, T013, T021, T023, T040–T042, T046 |
| Q3 Hosts/composition/missing selection | T011, T013, T043–T046 |
| Q4 Counts/search/taste | T010, T012, T043–T044 |
| Q5 Interaction/a11y/states | T043–T046 |
| Q6 Approach/files/tests/gates | All phases; T050–T053 |
| Q7 Loop discipline | T054; Scope via T050 |
| Q8 Closure | Drafting receipt; Gate 1 user-owned |

## Notes

- `[P]` = different files, no incomplete deps. Shared FilterPanel/LibraryView files are never parallel-written.
- Quill invents no decisions; gaps return as `needs decision` in `DRAFTING_RECEIPT.md`.
- No ADR authored by Quill. No README/CHANGELOG. Clarify/checklist not in scope for this M ticket.
