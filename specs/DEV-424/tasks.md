# Tasks: Persist TMDb Collections and Collection Facet Filter

**Input**: `specs/DEV-424/` spec.md, plan.md, frozen brief.md (Q1–Q8 + C1 + Phase 2 step 6 @ `7a335ac3` + F7 @ `10ffc9c1`), CONCLUSIONS.md, ASSUMPTIONS.md, CONTEXT.md, recon-DEV-424, recon-DEV-424-2
**Status**: PLAN FROZEN — Keel Phase 2 step 6 adjudication; see PLAN_CHALLENGE.md. Gate 1 remains closed.
**Tests**: Required (Q6 / FR-010; step 6 ruling 6). Backend tests written **alongside** each backend task; backend suite all green before T020. Web remains red-first TDD (unchanged). Never mock the EF data-access driver for translation/serialisation cases — use Testcontainers. Extend existing `MovieQuerySurvivorTests`, `MovieQueryStringSurvivorTests`, and `EfMovieRepositorySurvivorTests` for `CollectionId` / collection branches.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.
**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; preserve unrelated existing comments; no `DateTime.Now`; do not rename `HasSameCollectionValues` / `SameCollections`; generated OpenAPI/types never hand-edited; existing assertion/mock libraries only.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm worktree and feature pin; no new packages or projects.

- [X] T001 Confirm worktree `F:/Dev/LamuFlix.worktrees/feature-424-spec` on `feature/424-spec`, `.specify/feature.json` → `specs/DEV-424`, and no new NuGet/npm dependency or project is introduced (Q1; FR-009)

---

## Phase 2: Foundational — metadata mapping (blocks persistence stories)

**Purpose**: Optional collection id/name on the enrichment data path.
**Checkpoint**: US1 mapping/malformed cases green against adapter + `MovieMetadata` without DB.
**Note (Sentry F7 @ `10ffc9c1`)**: Present `belongs_to_collection` with nonpositive id or blank name rejects the whole TMDb details response as Failed InvalidResponse (not Found with field silently dropped); missing/null is valid absent; does not claim all existing optional fields fail wholesale.

- [X] T002 [P] [US1] Extend `src/LamuFlix.Core/Domain/MovieMetadata.cs` with optional collection identity (positive id + name) and equality coverage that does not rename or overload `SameCollections` (FR-001; Q1, Q8)
- [X] T003 [US1] Extend `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Mapping.cs` response DTO and mapper to carry valid collection id/name; when `belongs_to_collection` is present with nonpositive id or blank name, reject the whole details response as Failed InvalidResponse through the existing failure seam (never Found with collection silently dropped; never persist an invented row); treat missing/null as valid absent collection (FR-001; Q5; Sentry F7 @ `10ffc9c1` — after T002)
- [X] T004 [P] [US1] Add focused adapter/domain tests **alongside** T002/T003 beside existing TMDb/metadata tests proving with-collection; missing/null as valid-absent; and malformed-present Failed InvalidResponse **not** Found (FR-001, FR-010; Q5; Q6; Sentry F7 @ `10ffc9c1`; step 6 ruling 6)

**Checkpoint**: Mapping proven; persistence can consume the metadata shape.

---

## Phase 3: User Story 1 — Collection persistence and atomic upsert (Priority: P1) 🎯 MVP core

**Goal**: `CollectionRecord` + nullable membership; required whole-save retry on natural-key conflict; migration with PK/`CollectionId` index assertions.
**Independent Test**: Testcontainers repository/migration proofs listed in Q6 / step 6 ruling 1–2.

- [X] T005 [P] [US1] Create `src/LamuFlix.Infrastructure/Persistence/Records/CollectionRecord.cs` with TMDb **int** Id as natural PK (no surrogate), Name, and movies navigation (FR-002; Q2; step 6 ruling 1)
- [X] T006 [US1] Edit `src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs` adding nullable `CollectionId` and collection navigation (FR-002; Q2)
- [X] T007 [P] [US1] Create `src/LamuFlix.Infrastructure/Persistence/Configurations/CollectionConfiguration.cs` with `ValueGeneratedNever` on Id; edit `MovieConfiguration.cs` for the nullable FK and explicit `CollectionId` index (FR-002; Q1; step 6 ruling 1; Sentry F6)
- [X] T008 [US1] Register `DbSet<CollectionRecord>` in `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` (FR-002)
- [X] T009 [US1] Generate EF migration under `src/LamuFlix.Infrastructure/Persistence/Migrations/` updating `LamuFlixDbContextModelSnapshot.cs` via existing `dotnet-ef`; assert generated migration includes natural PK on collections Id and index on movies.`CollectionId` (FR-002; Q1; Sentry F6)
- [X] T010 [US1] Extend `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` to upsert collection id/name and replace/clear membership atomically inside the existing save boundary; on collection unique/PK conflict from concurrent insert, roll back/reset the failed unit, re-read the now-existing collection row, and retry the **whole** atomic save across reachable save paths (never partial catch-and-continue; preserve caller transaction ownership; no partial retry inside an aborted ambient transaction; no invented extra infrastructure) — required implement-and-prove; recon-DEV-424-2 lines 22–33 show existing retry does not classify collection PK conflicts and ambient metadata writes have no retry loop; clear on successful metadata without collection including successful non-TMDb fallback; preserve prior metadata/membership on final failed/unavailable enrichment; no cascade-delete of movies; no cleanup/release-note job (FR-003; Q2, Q5; step 6 rulings 1–2; F7)
- [X] T011 [US1] Add Testcontainers integration tests (sibling under `tests/LamuFlix.IntegrationTests/`) and extend `EfMovieRepositorySurvivorTests` proving upsert/name update; **forced** two-movie concurrent enrichment into the same **new** collection id asserting exactly one collection row, both movies members, and no conflict surfaced (proves whole-unit rollback/reset/re-read recovery); replace; explicit fallback-clear (prior member → successful fallback without collection → membership null) plus failure-preserves (prior membership unchanged after final failed/unavailable); pre-migration rows remaining null; no cascade loss (FR-003, FR-010; Q6; step 6 rulings 1–2; F7)

**Checkpoint**: US1 persistence complete and independently testable; backend green for this phase.

---

## Phase 4: User Stories 2 & 3 — Facet API and browse collectionId (Priority: P1)

**Goal**: Genre-mirrored collections facet + browse filter/codecs/validation.
**Independent Test**: catalog/facet/browse/paging/API tests per Q3/Q6; backend green before T020.

- [X] T012 [P] [US2] Add `CollectionFacet` as `(int Id, string Name, int MovieCount)` and `GetCollectionsAsync` to `src/LamuFlix.Core/Ports/IMovieCatalog.cs`; create `GetCollectionsQuery.cs` and `GetCollectionsQueryHandler.cs` under `src/LamuFlix.Core/Features/Library/` with **no** empty `GetCollectionsQueryValidator` (FR-004; Q3; step 6 ruling 5)
- [X] T013 [US2] Implement `GetCollectionsAsync` in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`: distinct current member counts as **int**, omit zero-member rows, order by name then id, do not narrow by active filters (FR-004; Q3; step 6 ruling 5)
- [X] T014 [US2] Add `src/LamuFlix.Api/Endpoints/CollectionDto.cs` as `(int Id, string Name, int MovieCount)`; map `GET /api/collections` named `GetCollections` in `FacetEndpoints.cs`; register via existing `AddHandler` chain in `HandlerRegistration.cs` unchanged (no empty validator) (FR-005; Q3; step 6 ruling 5)
- [X] T015 [P] [US3] Add nullable positive `CollectionId` to `src/LamuFlix.Core/Library/MovieQuery.cs` equality/hash and to `MovieQueryString.cs` codecs that **reject** nonpositive/invalid values rather than encoding them as valid state (FR-006; Q3; step 6 ruling 4)
- [X] T016 [US3] Add `WhereCollection` in `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs` and wire it into `EfMovieCatalog.BrowseAsync` ANDed with existing filters (FR-006; Q3)
- [X] T017 [US3] Extend `BrowseMoviesRequest.cs` with raw `string? CollectionId` (preserve existing ReSharper disable/restore UnusedAutoPropertyAccessor.Global pair around request properties); add `CollectionIdKey` const and `ReadNumber` + repeated-key detection in `LibraryEndpoints.cs` (complexity watchlist); add `MovieQueryValidator` positive-nullable rule (null or > 0); empty/`abc`/`0`/`-3`/repeated → **422** ProblemDetails; absent unconstrained; valid unknown id → empty page; pagination unchanged (FR-006; Q3; CONCLUSIONS.md C1; step 6 ruling 4)
- [X] T018 [P] [US2] Extend `tests/LamuFlix.IntegrationTests/FacetEndpointsTests.cs` and `EfMovieCatalogFacetsTests.cs` **alongside** for populated/empty collections, orphan exclusion, int counts, and ordering (FR-004, FR-005, FR-010; step 6 ruling 6)
- [X] T019 [P] [US3] Extend unit `MovieQuery*` / codec / validator / property fixtures including existing `MovieQuerySurvivorTests` and `MovieQueryStringSurvivorTests`, plus integration `LibraryEndpointsTests` / `EfMovieCatalogBrowseTests` / paging survivors for `collectionId` behaviour, including **422** ProblemDetails for empty/`abc`/`0`/`-3`/repeated input (FR-006, FR-010; Q6; CONCLUSIONS.md C1; step 6 rulings 4, 6)

**Checkpoint**: US2 and US3 independently testable; **backend suite all green** before Phase 5.

---

## Phase 5: Contract regeneration (blocks web consumption)

**Purpose**: C# DTO → OpenAPI → generated TypeScript only. Prerequisite: Phase 4 backend green (step 6 ruling 6).

- [X] T020 Regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` via `npm run generate:api` from the C# contract **after** backend tests are green; never hand-edit; confirm `npm run check:api` (FR-007; brief scope 8; step 6 ruling 6)

**Checkpoint**: Generated `CollectionDto` / `GetCollections` / browse `collectionId` available to web.

---

## Phase 6: User Story 4 — Filter Panel Collection chips (Priority: P1)

**Goal**: Single-select Collection section + URL/active chips per Q4 / step 6 ruling 3.
**Independent Test**: web tests listed below. **web-implement TDD (Q6 step 5)**: failing tests first, then production edits (red → green → refactor). Phase still follows Q6 backend → contract (T020) → web sequence. Palette owns `design.md` layout corrections — do not edit design from these tasks.

- [X] T021 [US4] Extend failing `FilterPanel.test.tsx`, `filterQuery.test.ts`, `LibraryView.toolbar.test.tsx`, `libraryUrl.test.ts`, and mocks/client type checks for fieldset/legend + sr-only checkbox select/replace/toggle/clear/clear-all, page reset, URL restore, toolbar-only unknown-id fallback retained through loading/error/empty, legend-only empty (no helper copy), no skeleton/`aria-busy`, and plain-text hostile-name + overlong-layout oracles **before** production web edits (FR-008, FR-010; Q6; step 6 ruling 3; Sentry F4)
- [X] T022 [P] [US4] Update `web/src/mocks/handlers.ts` and `web/src/mocks/fixtures.ts` for `/api/collections` (including hostile/overlong name fixtures) so the red tests can run against the generated contract (FR-008; after T020; supports T021; Sentry F4)
- [X] T023 [US4] Extend `web/src/features/library/filterQuery.ts`, `web/src/library/libraryUrl.ts`, and `web/src/library/browseParams.ts` for single-valued URL key `collectionId` (parse/serialize/toApi) until corresponding T021 cases go green (FR-008; Q4; after T021)
- [X] T024 [US4] Extend `web/src/library/LibraryView.tsx` to fetch `/api/collections` with existing query/client pattern, pass generated DTOs into the panel, and render removable **toolbar** active-filter chip + clear-all inclusion with unknown-id fallback label `Collection <id>` retained during facet loading/error/empty until corresponding T021 cases go green (FR-008; Q4; ASSUMPTIONS.md; step 6 ruling 3; after T021)
- [X] T025 [US4] Add Collection section to `web/src/features/library/FilterPanel.tsx` using Genre fieldset/legend and `label.lamu-chip` with sr-only **checkbox** (not radio): check selects/replaces, uncheck active clears; page reset on change/clear; preserve other filters/sort; empty facet → legend/heading only (no chips, no helper copy); no new skeleton/`aria-busy`; section label `Collection`; render names as plain text until corresponding T021 cases go green (FR-008; Q4; ASSUMPTIONS.md; step 6 ruling 3; Sentry F4; after T021/T023/T024 as needed)

**Checkpoint**: US4 independently testable in web; T021 was red before T023–T025.

---

## Phase 7: Polish & verification

- [ ] T026 Confirm edits stay inside the brief affected-file manifest; no helper renames; no LocalPlay/secrets/Process.Start; no hand-edited generated contracts; no orphan-cleanup/release-note scope creep (FR-009; Q1, Q8; Sentry F5 noted no fix)
- [ ] T027 Run `./scripts/run-roslyn-analyzers.ps1` on changed `.cs` files (FR-010; Q6)
- [ ] T028 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from `harness.yml` on changed files — watch `LibraryEndpoints.cs` (FR-010; step 6 ruling 4)
- [ ] T029 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (FR-010)
- [ ] T030 `dotnet format --verify-no-changes` clean (FR-010)
- [ ] T031 Run affected backend tests then full `dotnet test` (FR-010; SC-007)
- [ ] T032 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS (FR-010; Q6)
- [ ] T033 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree; never lower threshold (FR-010; Q6)
- [ ] T034 Run web lint, `check:api`, tests, and build via web-implement workflow (FR-010; Q6)
- [ ] T035 Size M code review with Sentry, Ledger, and Compass; apply Q7 closing bar (Critical/High/Medium block; Low explicit disposition; every finding recorded); frozen scope; hard two-round remediation cap then block to Conductor; missing axis/evidence is blocked (FR-010; Q7)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: Immediate.
- **Phase 2**: After T001 — blocks Phase 3; tests alongside.
- **Phase 3 (US1 persistence)**: After Phase 2 — MVP storage; tests alongside including forced concurrency oracle.
- **Phase 4 (US2/US3)**: After Phase 3 membership exists for meaningful facet/filter proofs; tests alongside; **all backend green before Phase 5**.
- **Phase 5**: After Phase 4 API/DTO surface compiles **and** backend suite green (step 6 ruling 6).
- **Phase 6 (US4)**: After Phase 5 generated types; web red-first unchanged.
- **Phase 7**: After implementation complete.

### User Story Dependencies

- **US1**: After Phase 2 mapping — no dependency on US2–US4.
- **US2 / US3**: After US1 persistence (shared catalog data); US2 and US3 implementation files mostly parallel after T011.
- **US4**: After contract regeneration (T020); interaction rules from Q4 + step 6 ruling 3 — layout still Palette-owned.

### Parallel Opportunities

```text
# After T001:
T002 MovieMetadata.cs          # [P] with T004 once shape agreed with T003 sequencing
T004 mapping tests             # [P] alongside T002/T003 (backend tests-alongside)

# After T004:
T005 CollectionRecord.cs       # [P] with T007 configs once MovieRecord edit planned
T007 configurations            # [P] relative to T005 (ValueGeneratedNever + CollectionId index)

# After T011 (backend green for US1):
T012 Core facet port/query     # [P] with T015 MovieQuery/codecs
T015 MovieQuery + codecs       # [P] with T012
T018 facet tests               # [P] with T019 browse/query tests after API wired (alongside)

# After T019 backend green + T020 (web TDD — tests before impl; Q6 step 5):
T021 failing web tests         # red first (checkbox/toolbar/hostile oracles)
T022 mocks                     # [P] with T021 once contract exists
# After T021 red:
T023 URL/query modules         # green for codec/URL cases
T024 LibraryView.tsx           # green for fetch/toolbar fallback cases
T025 FilterPanel.tsx           # green for fieldset/checkbox Collection section
```

## MVP

T001–T011 (mapping + persistence + forced concurrency + fallback-clear) deliver ticket storage semantics. Add T012–T019 for facet/filter API (backend green), T020 contracts, then T021–T025 UI (failing tests → mocks → production) for full ticket acceptance. Gates T026–T035 before ship review.

## Notes

- `[P]` = different files, no incomplete deps; same-file extensions are sequenced.
- `design.md` exists; Palette owns further design corrections; challenge disposition/freeze and Gate 1 remain unclaimed — do not invent visual layout in these tasks.
- Quill does not write ADRs (Q8).
- Sentry F5 orphan telemetry/cleanup: out of scope, noted no fix.
- Sentry F7 closed @ `10ffc9c1`: T003/T004 require malformed-present Failed-not-Found and missing/null valid-absent oracles; not a wholesale optional-field fail claim.
- Phase B pickup: recheck DEV-331/DEV-322 landing and contracts; route missing facts as needs recon to Conductor (brief handoff).

