# Tasks: DEV-322 Filter Panel

**Input**: `specs/DEV-322/spec.md`, `specs/DEV-322/plan.md`, frozen brief `specs/DEV-322/brief.md` (Q1-Q12; Q12 at brief.md:170-181, F2 remedy at brief.md:204-210, F3 decision at brief.md:212-217); facts `recon-DEV-322:189-234`; rulings `specs/DEV-322/CONCLUSIONS.md` (Q12 at :54-58).

**Prerequisites**: plan.md (required), spec.md (required for user stories). No `research.md`, `data-model.md`, or `contracts/` for this ticket: approach, data shape, and contract delta are frozen in the brief plus `recon-DEV-322`.

**Tests**: Included: backend count tests (FR-011), URL/serialization tests (FR-008/FR-009), control tests, debounce tests, and URL/refetch acceptance tests per Q10. Tests are written FIRST and MUST fail before implementation.

**Organization**: Execution phases follow the frozen brief dependency order. Story tags retain acceptance traceability: US1 is the URL/refetch acceptance goal, US2 supplies genre/cast and count producers, and US3 supplies ranges/sort/status. US1 integration depends on both control producers; it is not independently implementable first.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

Resolve the DEV-320 prerequisite and Q5/Q3 pickup conditionals before editing.

- [X] T001 [Pickup] Stop unless bounded recon on the actual inherited Phase B base identifies DEV-320 generated `web/src/api/types.ts` and client facilities, the exact inherited codegen command and prerequisites, and a repeatable regeneration path after an OpenAPI delta (presence of `types.ts` alone is insufficient); if DEV-320 is unmerged or the path is missing, order DEV-322 build after DEV-320 and report the missing prerequisite; DEV-322 must not implement substitute tooling or handwritten DTOs (Q4/F3, brief.md:212-217, no code)
- [X] T002 [Pickup] Bounded recon: record whether a library page exists (Q5 mount target) and whether TanStack Query is present (Q3 conditional); recheck contract drift against main (no code)
- [X] T003 [Pickup] Pin constitution-chosen TanStack Query in `web/package.json`, `web/package-lock.json` only if absent at pickup (depends on T001, T002; Q3; skip otherwise, no code if present)

**Checkpoint**: Pickup evidence precedes any conditional package edit.

## Phase 2: Backend Counts and Contract (US2 Producers)

Write failing count tests before backend implementation; regenerate the contract before badge UI.

- [X] T030 [P] [US2] Write failing backend tests: `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs` count translation (zero movies yields zero; shared movie counts once per genre; distinct per-genre totals; ordering/empty retained) using the real-database infrastructure (FR-011, Q1/Q10)
- [X] T031 [P] [US2] Write failing backend tests: `tests/LamuFlix.UnitTests/Features/Library/GetGenresQueryHandlerTests.cs` count-bearing passthrough and empty result (FR-011, Q1)
- [X] T032 [P] [US2] Write failing endpoint tests in `tests/LamuFlix.IntegrationTests/FacetEndpointsTests.cs`: exact id/name/movieCount shape and count values; retain empty-array coverage (FR-011, Q1)
- [X] T034 [US2] Extend `GenreFacet` in `src/LamuFlix.Core/Ports/IMovieCatalog.cs` with the count; project the count of the existing `Movies` navigation in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` `GetGenresAsync` (database-translated, `AsNoTracking`, name-then-id ordering) (depends on T030, T031; recon :71-90)
- [X] T035 [US2] Add required wire `movieCount` in `src/LamuFlix.Api/Endpoints/GenreDto.cs` and map it in `src/LamuFlix.Api/Endpoints/FacetEndpoints.cs` (depends on T034, T032; Q1)
- [X] T036 [US2] Regenerate in contract-chain order after T035: set `LAMUFLIX_REGENERATE_OPENAPI=true` only for the established opt-in test `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` to write `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together (recon-DEV-322:221-225), clear the opt-in, then run the T001-established inherited DEV-320 TypeScript workflow against the updated OpenAPI so generated types expose required integer `movieCount` before badge UI is wired; exercise `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` `DriftMatchesCommittedBaseline` enforcement without replacing it by regeneration (depends on T035; Q1/Q4/F3)

**Checkpoint**: Count path and generated contract expose movieCount; frontend story acceptance is not yet complete.

## Phase 3: URL and Debounce Foundations

Write failing serialization and fake-timer tests before their respective implementations.

- [ ] T010 [P] [Found] Write failing compact-URL plus canonical-API-query tests in `web/src/features/library/filterQuery.test.ts` against independently specified expected API queries (never computed by the encoder under test): inactive groups omitted; active runtime carries all three keys; active year carries both; extent ends serialize empty; `runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false` stays valid; repeated arrays keep order; `IMDb` maps to `Rating`; no chips omits `statuses`; compact URL omits `Title`/`Ascending`/`1`/`20` defaults while the canonical API query carries all four; empty URL yields `sort=Title&direction=Ascending&page=1&pageSize=20`; explicit versus omitted defaults normalize equally; supplied `pageSize`/`inWatchlist` preserved and never overwritten by defaults; unknown URL keys stay in the URL but out of the API query/key; `page` dropped from URL on filter change (FR-008, FR-009, Q6/Q11/Q12)
- [ ] T011 [Found] Implement `web/src/features/library/filterQuery.ts` to pass T010: separate compact-URL serialization from canonical API query normalization (Q12 transport defaults), Q11 all-or-nothing groups, empty-string extent stops, repeated keys in selection order, foreign-key preservation with unknown-key exclusion from the API query/key, `page` reset with `page=1` in the API request, deterministic canonicalization as the movie query key (depends on T010)
- [ ] T012 [P] [Found] Write failing debounce tests in `web/src/features/library/useDebouncedSearch.test.ts` with fake timers: no commit before 300 ms; one commit at 300 ms; rapid input cancels stale commits (FR-001, Q9)
- [ ] T013 [Found] Implement `web/src/features/library/useDebouncedSearch.ts` (300 ms local hook) to pass T012 (depends on T012)

**Checkpoint**: URL/debounce foundations ready before panel controls and queries.

## Phase 4: Panel Controls (US2/US3 and US1 Hydration)

Keep shared FilterPanel.test.tsx and FilterPanel.tsx edits serial. Tests precede the corresponding implementation. T021/T033 precede T037; T040/T041 precede T042.

- [ ] T021 [US1] Write failing URL hydration tests in `web/src/features/library/FilterPanel.test.tsx`: reload and `popstate` restore controls; `Clear filters` resets every panel key except `sort`/`direction` (depends on T011, T013; FR-009, FR-014, Q6; also assert Search titles placeholder)
- [ ] T033 [US2] Write failing panel tests in `web/src/features/library/FilterPanel.test.tsx`: genre badges with counts sorted by name; cast name ordering, local filtering and persistent selections (depends on T021; FR-002, FR-003, Q10)
- [ ] T037 [US2] Implement the controlled debounced search input, URL hydration/popstate callbacks and Clear filters preserving sort/direction, plus genre/cast controls in `web/src/features/library/FilterPanel.tsx` consuming generated-type facet props from LibraryView queries and showing `movieCount` badges; actor options use `GET /api/people?role=actor` (depends on T011, T013, T021, T036, T033; no hand-written DTO duplicates; design.md:216-218,232)
- [ ] T040 [US3] Write failing range tests in `web/src/features/library/FilterPanel.test.tsx` with deterministic time via `vi.setSystemTime` (restore clock state): both handles use the specified steps/extents; both runtime ends at extents + unchecked omits group; either moved or checked sends all three keys with explicit `true`/`false`; 240+ plus moved minimum stays valid; both year ends at extents omits group; either moved sends both keys; one-sided ranges and URL hydration of range controls work; local-year derivation, unchanged extent across a year boundary while mounted, and new extent on remount (depends on T037; FR-004, FR-005, Q11/F5)
- [ ] T041 [US3] Write failing sort/status tests in `web/src/features/library/FilterPanel.test.tsx`: all eight sort choices send matching `sort`+`direction` (`IMDb` sends `Rating`); all status toggles; no chips omits `statuses`; include-unknown alone activates the runtime group (depends on T040; FR-006, FR-007, Q8)
- [ ] T042 [US3] Implement runtime/year sliders (native `input type=range`, year 1900..viewer local current year step 1 via the pure `currentYearExtent(now)` seam captured once at panel mount with no live rollover; runtime 0..240 step 5 with `240+ min` top stop), include-unknown checkbox, sort dropdown, and four status chips in `web/src/features/library/FilterPanel.tsx` (depends on T037, T040, T041)
- [ ] T043 [US3] Compose feature MSW handlers in `web/src/test/server.ts` only if required by the inherited DEV-320 test setup (depends on T042; Q10 conditional)

**Checkpoint**: All ticket controls and inherited MSW composition are ready for host integration; URL/refetch acceptance remains pending.

## Phase 5: Host and URL/Refetch Acceptance (US1)

Write failing host acceptance tests before LibraryView implementation. Integrate the complete panel, then verify matching URL/request state and retained results.

- [ ] T020 [US1] Write failing host acceptance tests in `web/src/features/library/LibraryView.test.tsx`: independently expected canonical API queries (never the encoder output) equal the canonicalized MSW-observed generated-client requests, including all four Q12 transport defaults on an empty URL; pending refetch preserves old titles and mounted controls; MSW never supplies missing defaults (depends on T042, T043; FR-008, FR-010, Q9/Q10/Q12; before host implementation)
- [ ] T022 [US1] Implement `web/src/features/library/LibraryView.tsx` owning the generated-client movies/genres/actors queries, compact-URL commit/hydration plus complete canonical API requests (Q12 transport defaults), and controlled FilterPanel props/callbacks; actor query uses role=actor; render `totalCount`/title results with `placeholderData: keepPreviousData`, the canonical API query string as the movie query key, and a fixed-dimension results area (depends on T011, T013, T020, T037, T042, T043; DEV-320 client prerequisite; design.md:216-218,232)
- [ ] T023 [US1] Mount the panel with minimal count/title host in `web/src/App.tsx` (or the reconfirmed library page per T002, leaving `App.tsx` alone) and update `web/src/App.test.tsx` to host AC coverage (depends on T022, T021; Q5 conditional)
- [ ] T024 [US1] Verify one fetch per committed effective API query change (canonical query key; equivalent omitted/explicit Q12 defaults share one key) and `replaceState` (no history spam) for text and discrete controls in `LibraryView.test.tsx`/`FilterPanel.test.tsx` (depends on T022, T023)

**Checkpoint**: All three stories can now be exercised through the host; every ticket control participates in URL/refetch acceptance.

## Phase 6: Polish & Cross-Cutting Concerns

Visual confirmation and applicable gates run only at their pipeline stages, not during drafting.

- [ ] T050 [P] Verify fixed control/badge dimensions in implementation and visual review without claiming a measured CLS score from RTL (Q9)
- [ ] T051 In LibraryView.test.tsx and FilterPanel.test.tsx confirm empty-result copy `No movies match these filters.`, `Clear filters` behavior, and the approved count/error copy from ASSUMPTIONS.md:12 (Q5; design.md states)
- [ ] T052 Run the applicable backend build/analyzers, complexity, InspectCode, formatting, tests, security/dependency, and contract-drift checks for the changed C# and package/contract scope at their pipeline stages; respect `harness.yml` thresholds without changing generated gate configuration (brief Gate expectations)
- [ ] T053 Run `web-implement` TDD and its web gate for the frontend diff at its pipeline stage (brief Gate expectations)
- [ ] T054 Run mutation on the eligible changed projects via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (never Stryker from a worktree) at its pipeline stage; a survivor calls for a test, not a threshold reduction (Q1/Q10, charter :66-76)

**Checkpoint**: Record each applicable gate verdict; no Gate 1 or merge approval is implied.

## Dependencies & Execution Order

- Execute phases 1 through 6 in order: pickup → count/contract → URL/debounce → complete controls → host acceptance → stage-appropriate checks.
- T001 and T002 may collect independent pickup evidence in parallel. T003 depends on both and may edit package files.
- T030/T031/T032 write different backend test files and may run in parallel. T034 follows T030/T031; T035 follows T034/T032; T036 follows T035.
- T010/T012 may run in parallel in separate test files. T011 follows T010; T013 follows T012.
- Shared-file control work is serial: T021 → T033 → T037 → T040 → T041 → T042. No parallel writer owns FilterPanel.test.tsx or FilterPanel.tsx concurrently.
- T043 follows T042; skip its source edit only if inherited DEV-320 setup needs no composition change.
- T020 follows the complete control/test setup and must fail before T022. T022 follows T020 and both control producers T037/T042. T023 follows T022/T021; T024 follows T023.
- US2 and US3 control behavior is checked during Phase 4; their hosted URL/refetch acceptance completes with US1 in Phase 5. Story priority does not override producer dependencies.
- Tests fail before the implementation they drive. All ticket stories are required; no reduced US1-only delivery checkpoint is authorized.

## Implementation Strategy

1. Confirm DEV-320 and resolve conditional dependency/mount scope by bounded pickup recon.
2. Implement database-backed counts and the endpoint shape, then regenerate OpenAPI, snapshot, and TypeScript in that order.
3. Implement tested URL normalization and debounce before controls/queries.
4. Implement and test all native panel controls, search/hydration/clear behavior, and generated-client facet queries with serial shared-file ownership.
5. Write failing host acceptance tests, integrate the panel and minimal results host, then verify one request per committed change, reload/popstate, and pending-data retention.
6. Complete visual checks and applicable gates at their pipeline stages.

## Traceability (brief Q → tasks)

- Q1 count badges: T030-T037, T054.
- Q2 IMDb wording: T010, T041.
- Q3 dependencies/native controls: T003, T037, T042.
- Q4 generated client: T001, T022, T036, T037.
- Q5 panel host and recorded taste: T002, T021, T023, T037, T051.
- Q6 URL ownership: T010, T011, T021, T022, T024, T037.
- Q7 slider extents: T040, T042.
- Q8 status chips: T041, T042.
- Q9 fluid interaction: T012, T013, T020, T022, T050.
- Q10 test scope: T010, T012, T020, T021, T030-T033, T040, T041.
- Q11 range reconciliation (authoritative over original Q6/Q7): T010, T011, T040, T041.
- Q12 transport defaults, canonical equality and query key: T010, T011, T020, T022, T024.
