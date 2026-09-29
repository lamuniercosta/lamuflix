# Tasks: Typed Movie Query and Sort Model

**Input**: Design documents from `specs/DEV-297/`; Grill Phase A brief.md with task-ordering constraints (lines 161–172)

**Prerequisites**: plan.md, spec.md, brief.md (frozen grill decisions)

**Tests**: Included per spec.md and brief Q8 (FsCheck properties, validator boundaries, characterization tests, smoke test)

**Organization**: Tasks grouped by implementation phase following brief's task-ordering constraints (lines 161–172). Each phase represents a logical block of work with clear checkpoint validation.

---

## Phase 1: Setup — Central Package Management & Project References

**Purpose**: Establish NuGet dependencies in CPM and create the new UnitTests project before any code implementation

**Grill Constraint**: "Packages in CPM, then the Core EnrichmentStatus conversion, with the three existing tests adapted (no weakened assertions)." (brief:162)

- [X] T001 Update `Directory.Packages.props` to add `Ardalis.SmartEnum` 8.2.0 (Patron ruling, CONCLUSIONS.md:23,:25) (spec:FR-009; brief K6)
- [X] T002 Update `Directory.Packages.props` to add `Ardalis.SmartEnum.SystemTextJson` 8.1.0 (different version per Patron ruling, CONCLUSIONS.md:23,:25; JSON package accepts SmartEnum >= 8.1.0) (spec:FR-009; brief K6)
- [X] T003 Edit `src/LamuFlix.Core/LamuFlix.Core.csproj` to add PackageReference to Ardalis.SmartEnum via CPM (spec:FR-009; brief Q1 addendum, H5)
- [X] T004 Create `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` with ProjectReferences to Core and Infrastructure, mirroring the runner shape of `tests/LamuFlix.Test/LamuFlix.Test.csproj:3-17` (brief P2): `OutputType Exe`, `IsPackable false`, `IsTestingPlatformApplication false`; PackageReferences `Microsoft.NET.Test.Sdk`, `xunit.v3`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly`, `FsCheck`, `FsCheck.Xunit.v3`, plus `Ardalis.SmartEnum.SystemTextJson` (for JSON round-trip test per H5); all already pinned in Directory.Packages.props, no new dependency. Without the Test.Sdk reference the property gate and Stryker cannot discover the project (_gate-common.ps1:204-208). Also add tests/LamuFlix.UnitTests to LamuFlix.sln (format 12.00) under the `tests` solution folder per M2 (brief Q1 addendum; DEV-297:30–31 names the project) (spec:FR-010)
- [X] T005 Create `tests/LamuFlix.UnitTests/usings.cs` with global usings for Xunit, FsCheck, Shouldly, NSubstitute (project setup)

**Checkpoint**: CPM entries and project files created; both projects build successfully with no code yet. LamuFlix.Test already references Web and Testcontainers.PostgreSql arrives transitively via Tests.Common per M1.

---

## Phase 2: Foundational — Core Enumerations, Query Types, and Codec

**Purpose**: Implement Core types that serve as contracts for validators and tests

**Grill Constraint**: "Core Library types, the `MovieQueryString` codec and structural equality." (brief:163)

### Phase 2a: EnrichmentStatus Conversion

- [X] T006 Edit `src/LamuFlix.Core/Domain/EnrichmentStatus.cs`: convert enum to `SmartEnum<EnrichmentStatus, int>` with exactly these members: `Pending = 0`, `Enriched = 1`, `NotFound = 2`, `Failed = 3` (spec:FR-005; brief Q3 CONCLUSIONS:55)
- [X] T007 Edit `src/LamuFlix.Core/Domain/Movie.cs` so that `Status` (L31) and its assignments and comparisons (L16, 57, 63, 70, 76, 108, 116) compile against the SmartEnum; Movie has no Statuses member (spec:FR-005; brief Q3, H7, K7)
- [X] T008 Edit `tests/LamuFlix.Test/Domain/MovieFixture.cs`, `MovieTests.cs`, and `PropertyTests.cs` to work with converted `EnrichmentStatus` without weakening any assertions: `InlineData` SmartEnum arguments (MovieTests.cs:52-67, CS0182) become `MemberData`/`TheoryData` (constitution §IX); `Enum.GetValues<EnrichmentStatus>()` (PropertyTests.cs:156) becomes `EnrichmentStatus.List`; the constant `switch` (MovieFixture.cs:19) matches on `.Value` or `.Name`; assertions stay unchanged (spec:FR-005; brief Q3, P5)

**Checkpoint**: EnrichmentStatus is SmartEnum with pinned values; existing tests still pass with no assertion weakening.

### Phase 2b: Closed Sort Enumerations

- [X] T009 Create `src/LamuFlix.Core/Library/SortDirection.cs` as sealed Enumeration with exactly these members: `Ascending`, `Descending` (spec:FR-003; brief Q1)
- [X] T010 Create `src/LamuFlix.Core/Library/MovieSort.cs` as sealed Enumeration with exactly these members: `Title`, `Year`, `Rating`, `Runtime` (spec:FR-002; brief Q1)

**Checkpoint**: Both Enumerations compile; members are fixed and cannot be extended.

### Phase 2c: Value Objects & Query Record

- [X] T011 Create `src/LamuFlix.Core/Library/Page.cs` as a sealed record with `int Number` and `int Size`; NO guard, must be constructible with Number=0 and Size=0/101 for T025 testing; validation rules live only in MovieQueryValidator (spec:FR-004; brief Q6, H6)
- [X] T012 Create `src/LamuFlix.Core/Library/RuntimeRange.cs` as a sealed record `RuntimeRange(int? Min, int? Max, bool IncludeUnknown)` (spec:FR-004; brief Q6, K1)
- [X] T013 Create `src/LamuFlix.Core/Library/YearRange.cs` as a sealed record with `int? Min` and `int? Max` fields (spec:FR-004; brief Q6)
- [X] T014 Create `src/LamuFlix.Core/Library/MovieQuery.cs` as a sealed record with all fields per spec:FR-001: Text (string?), GenreIds, ActorIds (ImmutableArray<int>), Runtime (RuntimeRange?), Year (YearRange?), Statuses (ImmutableArray<EnrichmentStatus>), InWatchlist (bool?), Sort (MovieSort), Direction (SortDirection), Page (Page). The flat names (runtimeMin, runtimeMax, runtimeIncludeUnknown, yearMin, yearMax, page, pageSize) are ONLY the Q7 URL keys (spec:FR-001; brief Q4, C1)
- [X] T015 Implement sequence-aware equality for MovieQuery: override `Equals(MovieQuery?)` and `GetHashCode()` with order-preserving SequenceEqual over GenreIds, ActorIds and Statuses; normalize default ImmutableArray to Empty in constructor/init so default and Empty compare equal (spec:FR-001; brief Q7, H2). Add a unit test for each in `tests/LamuFlix.UnitTests/Library/MovieQueryTests.cs`

**Checkpoint**: All value objects and MovieQuery compile; no reflection needed for query building.

### Phase 2d: Query String Codec

- [X] T016 Create `src/LamuFlix.Core/Library/MovieQueryString.cs` with static methods: `static string Format(MovieQuery)` and `static bool TryParse(string, out MovieQuery)` using BCL only (Uri.EscapeDataString/Uri.UnescapeDataString, no HttpUtility; spec:FR-006; brief Q7, L6)
- [X] T017 Implement Format per D1: Runtime non-null → emit runtimeMin, runtimeMax, runtimeIncludeUnknown (empty value for null bound); Year non-null → emit yearMin, yearMax (empty value for null bound); repeated array keys (genreIds=1&genreIds=2), Enumeration values by Name, omit other nulls, always emit sort/direction/page/pageSize (spec:FR-006; brief Q7, D1, R3-2); add example tests per D1 in `tests/LamuFlix.UnitTests/Library/MovieQueryStringTests.cs` (file created here)
- [X] T018 Implement TryParse per M9 failure policy: returns false for malformed int/bool, non-exact TryFromName, duplicated scalar key, partial range, missing sort/direction/page/pageSize, or empty value on any non-range, non-text key; text= is empty string, absent text is null; unknown keys ignored; does not validate (spec:FR-006; brief Q7, M9, K5, R3-2); add one example test per rule in `tests/LamuFlix.UnitTests/Library/MovieQueryStringTests.cs`

**Checkpoint**: Codec compiles; round-trip encoding/decoding is possible.

---

## Phase 3: Foundational — Infrastructure Validator

**Purpose**: Implement the canonical validator for MovieQuery before tests that depend on it

**Grill Constraint**: "`MovieQueryValidator`." (brief:164)

- [X] T019 Create `src/LamuFlix.Infrastructure/Library/MovieQueryValidator.cs` as a sealed FluentValidation validator for `MovieQuery` with rules: Page.Size 1–100; Page.Number >= 1; Runtime.Min <= Runtime.Max and Year.Min <= Year.Max when both are present; Sort and Direction present. Empty ranges are NOT rejected per D1 (spec:FR-007; brief Q6, C2, H5). Each rule gets a boundary example test per C2
- [X] T020 Verify MovieQueryValidator is not registered in any decorator or pipeline in this ticket (spec:FR-008)

**Checkpoint**: Validator compiles and can validate MovieQuery instances.

---

## Phase 4: Testing — FsCheck Properties and Validator Boundaries

**Purpose**: Implement all UnitTests before moving to integration/characterization tests

**Grill Constraint**: "Create `tests/LamuFlix.UnitTests`: the FsCheck round-trip property, the generator-in-domain property, the validator boundary tests and the EnrichmentStatus pin tests." (brief:165)

### Phase 4a: Shared Fixtures & Generators

- [X] T021 [US4] Create `tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs` with an FsCheck `Arbitrary<MovieQuery>` generator that: produces only validator-accepted queries, includes RuntimeRange(null,null,false/true) and YearRange(null,null), includes URL-hostile characters in Text (e.g., `&`, `=`, spaces, quotes), uses valid range values, and covers the full domain per brief Q7 (spec:FR-001, SC-003, SC-004; brief Q7, L2)

**Checkpoint**: Generator compiles and produces valid MovieQuery instances.

### Phase 4b: Round-Trip Property

- [X] T022 [US4] Extend `tests/LamuFlix.UnitTests/Library/MovieQueryStringTests.cs` (created by T017/T018) with FsCheck property, annotated `[Trait("Category", "Property")]` (brief P3; PropertyTests.cs:13): `Format(q) → TryParse → q' should equal q` (property asserts equality after round trip); include URL-hostile characters and validator-accepted values (spec:FR-001, FR-006; brief Q7, R3-2); map to SC-003, SC-004
- [X] T023 [US4] Add example tests to T022: query string `sort=Title&direction=Ascending&page=1&pageSize=20` parses successfully and round-trips; same string without pageSize makes TryParse return false (spec:US4 AS2, FR-006, SC-003; brief R3-2)

**Checkpoint**: Round-trip property passes; Format/Parse preserves all query fields.

### Phase 4c: Generator-in-Domain Property

- [X] T024 [US4] Create `tests/LamuFlix.UnitTests/Library/MovieQueryGeneratorTests.cs` (separate file, per plan:126 and :197; brief P4) with a second FsCheck property, annotated `[Trait("Category", "Property")]` (brief P3): `Generator produces only values that pass MovieQueryValidator` (spec:FR-006, FR-007, SC-004; brief Q7; maps to SC-004)

**Checkpoint**: Generator property passes; all generated queries pass validation.

### Phase 4d: Validator Boundary Tests

- [X] T025 [US1] Create `tests/LamuFlix.UnitTests/Library/MovieQueryValidatorTests.cs` with explicit boundary tests per FR-007, K4: Page.Size 0 (invalid), 1 (valid), 100 (valid), 101 (invalid); Page.Number 0 (invalid), 1 (valid); Runtime Min==Max (valid), Min>Max (invalid), one bound null (valid); Year Min==Max (valid), Min>Max (invalid), one bound null (valid); empty ranges (valid per D1); Sort null (invalid), Direction null (invalid) (spec:FR-007, SC-005; brief Q6, K4)

**Checkpoint**: Boundary tests pass; validator rules are verified.

### Phase 4e: EnrichmentStatus Pin Tests & Enumeration Tests

- [X] T026 [US3] Create `tests/LamuFlix.UnitTests/Domain/EnrichmentStatusTests.cs` with tests pinning values: `EnrichmentStatus.Pending` has value 0, `Enriched` = 1, `NotFound` = 2, `Failed` = 3; and test names are "Pending", "Enriched", "NotFound", "Failed" (spec:FR-005, US3 AS1; brief Q8c, SC-002)
- [X] T027 [US2] Create `tests/LamuFlix.UnitTests/Library/EnumerationTests.cs` with TryFromName and JSON round-trip tests (R3-1): MovieSort.TryFromName("Title") succeeding; "InvalidSort" failing, plus a non-exact case such as "title" failing; SortDirection the same; JSON round-trip by Name via SmartEnumNameConverter for MovieSort, SortDirection and EnrichmentStatus (spec:US2 AS1-4, FR-002, FR-003, FR-005, FR-009; brief:11, :19, R3-1). Map to SC-001

**Checkpoint**: Status values and names are pinned; Enumeration members are fixed and JSON serializes by Name.

---

## Phase 5: Characterization Tests for Legacy Path

**Purpose**: Document current legacy behavior before replacing helpers (owner-approved, brief:210 O2)

**Grill Constraint**: "Characterization tests in `LamuFlix.Test` against the **current** legacy helpers: every sort key, both directions, null placement and every predicate. Commit them green before any replacement." (brief:166)

**Condition**: None. The owner approved the combined Q4/Q5/Q11 decision (brief:210 O2); the spec PR is skipped (brief O4), so this phase runs unconditionally after Phase 4.

- [X] T028 [US5] Create `tests/LamuFlix.Test/Library/LegacyMovieSortTests.cs` with pure IQueryable characterization tests for the existing `Data.Models.Movie` entity; pin CURRENT behavior: empty sortBy → Id desc, only exact "desc" sorts descending, unknown SortBy throws ArgumentException, current null order. Cover: every sort key (Title, Year, Duration, ImdbRating, MetaScore, RottenTomatoes, Id), both directions (ascending, descending), null placement on `Title` (the only nullable legacy sort key, Movie.cs:8; current order), every predicate (SearchField case-insensitive substring on Title, Year equality, CollectionId equality, DirectorId via Directors.Any, GenreIds/ActorIds via Any+Contains). Characterization pins CURRENT; NULLS LAST (observable only on `Title`) is the post-rewire delta verified in T035 per brief K3 (spec:FR-012, SC-007; brief Q8a, H3, K3); map to FR-012, SC-007
- [X] T029 [US5] Commit T028 tests green, with all tests passing against the current legacy DynamicQuery/DynamicSort helpers (brief:166)

**Checkpoint**: Legacy behavior is documented and passing.

---

## Phase 6: Core Model Replacement — Remove Legacy Helpers & Rewire

**Purpose**: Replace reflection-based helpers with typed query and optional legacy bridge

**Grill Constraint**: "The Web compatibility path (`LegacyMovieSort` plus explicit predicates over the existing request types) and the MovieService rewire. Then verify the intended deltas explicitly: an unknown sort falls back to `Id desc`, and NULLS LAST. Page, id, year and text behaviour must not change (Q11)." (brief:167)

**Condition**: None. Phases 6a-6d run unconditionally and in order (brief:210 O2; spec PR skipped, brief O4). Phase 6c T036 waits for Phase 6b green per K10.

### Phase 6a: Explicit Legacy Predicates & Enumeration

- [X] T030 [US5] Create `src/LamuFlix.Web/Library/LegacyMovieSort.cs` as a sealed Enumeration with exactly these members: `Id`, `Title`, `Year`, `Duration`, `ImdbRating`, `MetaScore`, `RottenTomatoes` (spec:FR-011, US5 AS1-4; brief Q5)
- [X] T031 [US5] Create `src/LamuFlix.Web/Library/MovieServiceExtensions.cs` with explicit, non-reflection filter and sort predicates for the legacy `Data.Models.Movie` entity: methods like `FilterByText(IQueryable<Movie>, string?)`, `FilterByGenres(IQueryable<Movie>, IEnumerable<int>)`, etc., covering exactly SearchField (case-insensitive substring on Title), Year equality, CollectionId equality, DirectorId via Directors.Any, GenreIds/ActorIds via Any+Contains. Drop runtime/year range, statuses, watchlist per brief H3, K3 (spec:FR-012; brief Q5, H3, K3); map to FR-012, SC-007
- [X] T032 [US5] Implement explicit predicates using LINQ expressions (no reflection, no DynamicQuery). Nullable sort key handling: use `OrderBy(x => key == null ? 1 : 0).ThenBy(key)` for ascending and `.ThenByDescending(key)` for descending to achieve NULLS LAST ordering in both directions per brief H4 (spec:FR-014, US5 AS4; brief Q6, H4); map to FR-014, SC-007

**Checkpoint**: Explicit predicates compile and produce typed, auditable query building.

### Phase 6b: MovieService Rewire & Fallback

- [X] T033 [US5] Edit `src/LamuFlix.Web/Services/MovieService.cs` callers (lines 152–180, 356–395 per recon) to: parse the legacy sort parameter via `LegacyMovieSort.TryFromName()`; if unknown or empty, fall back to `Id desc` (no 500 error) (spec:FR-013, US5 AS2; brief Q5); map to FR-013, FR-015, SC-007
- [X] T034 [US5] Rewire the same callers to apply explicit predicates from MovieServiceExtensions instead of DynamicQuery/DynamicSort; preserve the existing `QueryParams` and `MoviesFilterViewModel` request shapes (no new record types) (spec:FR-012, US5; brief Q5, M4); map to FR-015
- [X] T035 [US5] Retarget the T028 characterization tests from DynamicQuery/DynamicSort to LegacyMovieSort and MovieServiceExtensions; change ONLY the two ruled expectations (unknown sort -> Id desc; NULLS LAST on `Title`); every other assertion stays unchanged; green before T036. Verify behavior deltas explicitly: unknown sort → Id desc (vs. today's 500), NULLS LAST on `Title`, the only nullable legacy sort key (Movie.cs:8), page/id/year/text behavior unchanged (spec:FR-013, FR-014, US5 AS2-4; brief Q11); map to SC-007, FR-015

**Checkpoint**: Legacy path is rewired with explicit predicates; unknown sort falls back without error.

### Phase 6c: Remove DynamicQuery/DynamicSort (after T035 green)

- [X] T036 [US5] Edit `src/LamuFlix.Web/Extensions/EntityExtensions.cs`: remove `DynamicQuery<T>` method and `DynamicSort<T>` method; verify no remaining references via grep in whole repo (src/ and tests/) for DynamicQuery|DynamicSort (spec:FR-017, SC-006; brief Q2, L3)


Note: This deletion (T036) cannot land before the replacement for the three callers in MovieService.cs:169, :174, :372 is green (T035). The owner approved the replacement (brief:210 O2), so no reject path applies.

**Checkpoint**: Legacy reflection helpers are gone; no dangling references remain.

### Phase 6d: EF Translation Smoke Test

- [X] T037 [US5] Create `tests/LamuFlix.Test/Smoke/MovieServiceEfTests.cs` with a smoke test using the existing PostgreSQL Testcontainers fixture: build a query via explicit predicates (from T031), execute against real PostgreSQL, assert results match expectations (spec:SC-008; brief Q8b)
- [X] T038 [US5] Document in the test that MySQL translation is unproven on production's Pomelo MySQL 8.0.31; this smoke test is PostgreSQL-only (brief Q8b). Maps to SC-008

**Checkpoint**: EF translation verified on PostgreSQL; MySQL risk recorded.

---

## Phase 7: Documentation — ADR

**Purpose**: Record the decision and migration path

**Grill Constraint**: "`docs/adr/ADR-0009.md`, per Q9." (brief:171)

- [X] T039 [P] Create `docs/adr/ADR-0009.md` at exactly that path, status `Accepted`, and sections per brief Q9: Context, Decision, Consequences; include typed Core model, closed sort Enumerations, validator, URL codec, equality, pinned EnrichmentStatus values, NULLS LAST rule, removal of `DynamicQuery<T>`/`DynamicSort<T>`. Follow ADR-0010 house style, ticket and date metadata, one page. Include the content list and the required compatibility/retirement section from brief Q9 (brief:210 O2) (DEV-298/DEV-299; PostgreSQL-only translation evidence, MySQL unproven) (spec:FR-016, SC-009; brief Q9, L4); map to FR-016

**Checkpoint**: ADR is written and serves as future reference.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, gates, and test suite

**Grill Constraint**: "Run the gates." (brief:172)

- [ ] T040 [P] Run `./scripts/run-roslyn-analyzers.ps1` (spec:SC-009; brief M8)
- [ ] T041 [P] Run `./scripts/run-cyclomatic-complexity.ps1` at threshold 15, then separately at `-Threshold 6` for refactor gate (spec:SC-009; brief M8)
- [ ] T042 [P] Run `./scripts/run-jetbrains-inspectcode.ps1` (spec:SC-009; brief M8)
- [ ] T043 [P] Run `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` (Category=Property); FsCheck tests MUST carry the Category=Property trait, else gate exits 2 SKIPPED and blocks (spec:SC-009, SC-003, SC-004; brief M8)
- [ ] T044 [P] Run `./scripts/run-vulnerable-packages.ps1` (spec:SC-009; brief M8)
- [ ] T045 [P] Run `dotnet format --verify-no-changes` (spec:SC-009; brief M8)
- [ ] T046 [P] Run `dotnet test` (all tests, spec:SC-009; brief M8)
- [ ] T047 [P] Verify Core Stryker mutation gate at 80 or above, scoped by command line; root stryker-config.json unedited (spec:SC-009; brief M8)
- [ ] T048 [P] Verify all FsCheck properties pass, including round-trip and generator-in-domain (spec:SC-003, SC-004; brief Q8; maps to SC-003, SC-004)
- [ ] T049 [P] Verify all characterization tests pass (spec:SC-007; brief Q8a)
- [ ] T050 [P] Verify the smoke test passes (spec:SC-008; brief Q8b)
- [X] T051 [P] Web gates (`./scripts/run-web-gates.ps1`) are NOT in this ticket's gate set (Patron ruling, CONCLUSIONS.md:11-19). DEV-297 changes no file under `web/`, so report as `N/A: no web/ diff`, never as PASS or as a SKIPPED gate (spec:SC-010; brief:183)

**Checkpoint**: All required gates exit 0 or configured OPT-OUT; a scope-empty SKIPPED on a required gate blocks per K9.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately. **Unblocks all later phases.**
- **Phase 2 (Foundational)**: Depends on Phase 1 (packages available). **Unblocks Phase 3, 4, 5, 6.**
- **Phase 3 (Validator)**: Depends on Phase 2 (MovieQuery types). **Unblocks Phase 4.**
- **Phase 4 (UnitTests)**: Depends on Phase 2 (query types) and Phase 3 (validator). **Unblocks Phase 5 when green.**
- **Phase 5 (Characterization)**: Depends on Phase 4 (UnitTests green). **Must run before Phase 6 rewiring per brief Task order.** Unconditional (brief:210 O2; K15 superseded by O4).
- **Phase 6 (Replacement)**: Depends on Phase 2 (Core types exist) and Phase 5 (green baseline exists). **Unconditional (brief:210 O2); the removal (Phase 6c) lands only after Phase 6b is green.**
- **Phase 7 (ADR)**: Depends on Phase 6 (implementation complete). **Final documentation.**
- **Phase 8 (Polish & Gates)**: Depends on all phases — final validation only.

### Critical Path

1. **Phase 1** (T001–T005): Create project files and CPM entries
2. **Phase 2** (T006–T018): Implement all Core types and codec
3. **Phase 3** (T019–T020): Implement validator
4. **Phase 4** (T021–T027): Implement all UnitTests
5. **Phase 5** (T028–T029): Characterization baseline
6. **Phase 6** (T030–T038): Explicit predicates, MovieService rewire, remove legacy helpers, smoke test
7. **Phase 7** (T039): Write ADR
8. **Phase 8** (T040–T051): Run gates and verify

### Parallel Opportunities

- No parallel phases. Per brief Task order (line 161), phases run sequentially: Phase 4 must be green before Phase 5 starts (K15).
- **Phase 6 replacement** cannot start until **Phase 5 characterization** is committed green (brief:166).

---

## Implementation Strategy

### MVP Scope

Complete phases in order:

1. **Phase 1**: Setup (CPM, projects) — 1 checkpoint
2. **Phase 2**: Core types, value objects, codec — 3 checkpoints
3. **Phase 3**: Validator — 1 checkpoint
4. **Phase 4**: FsCheck properties, boundary tests, pin tests — 5 checkpoints
5. **Phase 5**: Characterization baseline (commit green before replacement)
6. **Phase 6**: Remove legacy, implement bridge, smoke test
7. **Phase 7**: ADR
8. **Phase 8**: Gates and validation

**Result**: A fully typed, validated, and tested query model with a temporary legacy bridge until handler migration (DEV-299).

### Incremental Delivery

The ticket is interdependent: typed query requires validator, which requires tests, which require codec. However, each component can be validated independently before moving to the next:

- After Phase 2: Core types compile; no reflection needed for query building
- After Phase 3: Validator can validate MovieQuery instances
- After Phase 4: FsCheck properties and boundary tests prove correctness
- After Phase 5: Legacy baseline is documented and passing
- After Phase 6: Replacement is complete and behavior deltas are verified
- After Phase 8: Gates confirm no regressions

---

## Notes

- The combined Q4/Q5/Q11 decision is owner-approved (brief:210 O2) and the spec PR is skipped (brief O4), so no checkbox remains. Phases 5, 6a-6d and 7 are unconditional and run in order; the brief:169 H1 reject path does not apply. The removal (Phase 6c/T036) lands only after the MovieService.cs callers are rewired and T035 is green per K10.
- Phase 5 characterization tests MUST commit green before Phase 6 replaces the helpers (brief:166).
- The ticket does not authorize changes to the Data-layer `MovieEnrichmentStatus` enum, EF schema, or the Web API contract (`web/src/api`).
- MySQL translation evidence is not collected; PostgreSQL Testcontainers fixture is used for smoke testing (brief Q8b). The risk is accepted and recorded in ADR-0009 (brief Q9).
