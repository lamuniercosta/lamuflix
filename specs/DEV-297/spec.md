# Feature Specification: Typed Movie Query and Sort Model

**Feature Branch**: `feature/DEV-297`

**Created**: 2026-09-29

**Status**: Draft

**Input**: Grill Phase A outcome; Brief, Conclusions, and Assumptions in specs/DEV-297/

## User Scenarios & Testing

### User Story 1 - Typed Query Model for Movie Browsing (Priority: P1)

As a backend handler developer migrating a browse endpoint, I need a strongly-typed, validated `MovieQuery` record that replaces the legacy reflection-based filter/sort helpers so that queries are type-safe, predictable, and auditable.

**Why this priority**: Core model that enables the Library handler (DEV-299) and future catalog compositions. Replaces unsafe reflection and provides a contract for the API.

**Independent Test**: Can be fully tested by constructing a `MovieQuery`, validating it via `MovieQueryValidator`, encoding/decoding it via `MovieQueryString`, and verifying the round-trip property with FsCheck.

**Acceptance Scenarios**:

1. **Given** a `MovieQuery` with valid fields **When** validated with `MovieQueryValidator` **Then** validation succeeds and the query is accepted
2. **Given** a `MovieQuery` with a Page.Size > 100 or < 1 **When** validated **Then** validation fails with a message for Page.Size
3. **Given** a `MovieQuery` **When** formatted to query string and parsed back **Then** the parsed query equals the original (round-trip property)
4. **Given** a query string with unknown keys **When** parsed **Then** unknown keys are ignored and parsing succeeds

---

### User Story 2 - Closed Sort Enumerations (Priority: P1)

As a developer maintaining sort logic, I need `MovieSort` and `SortDirection` sealed Enumerations with fixed members so that sort choices are closed, type-safe, and cannot be extended.

**Why this priority**: Prevents unsafe sort names from reaching the database and makes invalid sorts fail at compile or validation time, not at runtime.

**Independent Test**: Can be fully tested by parsing sort names, serializing by name, and verifying that only the four allowed `MovieSort` values (`Title`, `Year`, `Rating`, `Runtime`) and two `SortDirection` values are recognized.

**Acceptance Scenarios**:

1. **Given** a sort name "Title" **When** parsed via `MovieSort.TryFromName()` **Then** `MovieSort.Title` is returned
2. **Given** an unknown sort name "InvalidSort" **When** parsed **Then** parsing fails
3. **Given** `MovieSort.Title` **When** serialized to JSON **Then** the value is `"Title"` (by Name)
4. **Given** `SortDirection.Ascending` **When** serialized **Then** the value is `"Ascending"`

---

### User Story 3 - Core EnrichmentStatus as Enumeration (Priority: P1)

As the Movie aggregate, I need `EnrichmentStatus` to be a closed Enumeration instead of a plain enum so that domain state is type-safe and consistent with the constitution.

**Why this priority**: MovieQuery.Statuses uses `ImmutableArray<EnrichmentStatus>`, and the constitution forbids plain enums. Movie.cs `Status` (L31) and its assignments and comparisons (L16, 57, 63, 70, 76, 108, 116) compile against the SmartEnum. The conversion is scoped and bounded (recon-DEV-297:163-178 shows three test consumers only).

**Independent Test**: Can be fully tested by verifying that the four values (Pending=0, Enriched=1, NotFound=2, Failed=3) are present and have the correct integer values, and that existing Movie and EnrichmentStatus tests still pass without weakening their assertions.

**Acceptance Scenarios**:

1. **Given** `EnrichmentStatus.Pending` **When** the numeric value is accessed **Then** the value is 0
2. **Given** an existing test that validates Movie state transitions **When** the test runs against the converted EnrichmentStatus **Then** the test still passes and assertions are not weakened
3. **Given** `EnrichmentStatus.Failed` **When** serialized to JSON with SmartEnumNameConverter **Then** the value is `"Failed"` and it deserializes to Failed

---

### User Story 4 - Query String Codec and Round-Trip Property (Priority: P1)

As a handler developer, I need a `MovieQueryString` codec that encodes/decodes `MovieQuery` to/from query strings using only the BCL so that the query state can be preserved in URLs.

**Why this priority**: DEV-297:23-31 and :35 require an FsCheck round-trip property. The codec must be deterministic and handle nullable fields, array fields, and Enumeration values correctly.

**Independent Test**: Can be fully tested by generating arbitrary valid `MovieQuery` values with FsCheck, encoding them to query strings, parsing them back, and asserting equality. A second property proves that generated values pass `MovieQueryValidator`.

**Acceptance Scenarios**:

1. **Given** a `MovieQuery` with genres [1, 2, 3] **When** encoded **Then** the query string contains `genreIds=1&genreIds=2&genreIds=3` (repeated keys, order preserved)
2. **Given** a query string `sort=Title&direction=Ascending&page=1&pageSize=20` **When** parsed **Then** sort, direction, page and pageSize are always present in the output
3. **Given** a query string `sort=Title&direction=Ascending&page=1` (missing pageSize) **When** parsed **Then** TryParse returns false
4. **Given** a query with `Text=null` **When** encoded **Then** the text key is omitted from the output
5. **Given** a query with `Text=""` (empty string, not null) **When** encoded **Then** the text key is present with an empty value and parsing retrieves empty string
6. **Given** a URL with URL-hostile characters in Text (e.g., `&`, `=`, spaces) **When** encoded and parsed **Then** the round trip succeeds

---

### User Story 5 - Legacy Browse Compatibility (Priority: P2)

As a legacy MVC view, I need the current browse behavior (sort, filter, pagination) to work unchanged until DEV-299 migrates the browse handler.

**Why this priority**: The ticket requires replacing the legacy reflection helpers with the typed model while preserving the external contract. The owner approved this path (brief:210 O2); the spec PR is skipped (brief O4).

**Independent Test**: Can be fully tested by exercising the current Razor sort and filter choices (Title, Year, Duration, ImdbRating, MetaScore, RottenTomatoes, Id; case-insensitive text, directors, collections, genres, actors) against the legacy Data entity and verifying the result is unchanged.

**Acceptance Scenarios**:

1. **Given** a browse request with sort "MetaScore" (a legacy-only field) **When** submitted today **Then** sorting works and returns results in the expected order
2. **Given** a browse request with an unknown sort "XYZ" **When** submitted **Then** it defaults to Id descending (no 500 error)
3. **Given** a browse request where a genre ID is in the selected list and another is not **When** applied via the existing filter **Then** only movies matching the selected genre are returned
4. **Given** the nullable sort key `Title` (`string?`, Movie.cs:8) with null values in the database **When** sorted **Then** nulls appear last in both ascending and descending order per brief Q6 (`ImdbRating`, `RottenTomatoes` and `MetaScore` are non-nullable, Movie.cs:13-15)

---

## Requirements

### Functional Requirements

**Core Model & Enumerations**:
- **FR-001**: `MovieQuery` sealed record in `Core/Library/` with fields: `Text` (string?), `GenreIds`, `ActorIds` (ImmutableArray<int>), `Runtime` (RuntimeRange?), `Year` (YearRange?), `Statuses` (ImmutableArray<EnrichmentStatus>), `InWatchlist` (bool?), `Sort` (MovieSort?), `Direction` (SortDirection?), `Page` (Page). Values compared by order-preserving sequence equality (Equals override). The flat names (runtimeMin, runtimeMax, runtimeIncludeUnknown, yearMin, yearMax, page, pageSize) are ONLY the Q7 URL keys. Sort and Direction are nullable at construction so FR-007 can reject their absence; any query FR-006 parses or formats successfully has both (Patron P-1, brief.md:318-321).
- **FR-002**: `MovieSort` sealed Enumeration in `Core/Library/` with exactly these members: `Title`, `Year`, `Rating`, `Runtime`
- **FR-003**: `SortDirection` sealed Enumeration in `Core/Library/` with exactly these members: `Ascending`, `Descending`
- **FR-004**: Value object sealed records in `Core/Library/`: `RuntimeRange(int? Min, int? Max, bool IncludeUnknown)`, `YearRange(int? Min, int? Max)`, `Page(int Number, int Size)`
- **FR-005**: `EnrichmentStatus` in `Core/Domain/` converted from enum to `SmartEnum<EnrichmentStatus, int>` with values Pending=0, Enriched=1, NotFound=2, Failed=3. Movie.cs `Status` (L31) and its assignments and comparisons (L16, 57, 63, 70, 76, 108, 116) compile against the SmartEnum. Statuses exists only on MovieQuery. MovieFixture, MovieTests and PropertyTests are adapted without weakened assertions
- **FR-006**: `MovieQueryString` static codec in `Core/Library/` with `static string Format(MovieQuery)` and `static bool TryParse(string, out MovieQuery)` using BCL only (Uri.EscapeDataString/UnescapeDataString). Array keys are repeated, Enumeration keys are written by Name. D1 range presence rule: Runtime non-null → emit runtimeMin, runtimeMax, runtimeIncludeUnknown (empty value for null bound); Year non-null → emit yearMin, yearMax (empty value for null bound); absent keys mean null range; partial range is malformed. M9 TryParse failure policy: returns false for malformed int/bool, non-exact TryFromName, duplicated scalar key, partial range, or missing sort/direction/page/pageSize; text= is empty string, absent text is null; empty value on non-range, non-text key is malformed; unknown keys ignored; no validation. Sort, direction, page and pageSize are always emitted

**Validation**:
- **FR-007**: `MovieQueryValidator` sealed FluentValidation validator in `Infrastructure/Library/` with rules: Page.Size 1–100; Page.Number >= 1; Runtime.Min <= Runtime.Max and Year.Min <= Year.Max when both are present; Sort and Direction present. Empty ranges are NOT rejected
- **FR-008**: On the canonical handler path, the validation decorator turns validation failures into a 422 (constitution V:196-199). DEV-297 adds no decorator

**Dependency Management**:
- **FR-009**: `Ardalis.SmartEnum` 8.2.0 and `Ardalis.SmartEnum.SystemTextJson` 8.1.0 added to `Directory.Packages.props` (exact versions per CONCLUSIONS.md:23,:25); Core references SmartEnum; SystemTextJson is referenced ONLY by tests/LamuFlix.UnitTests (brief Q1 addendum, K6)
- **FR-010**: New `tests/LamuFlix.UnitTests` project created for FsCheck properties (named explicitly by DEV-297:30-31); no other NEW projects (K16)

**Legacy Compatibility** (owner-approved, brief:210 O2; spec PR skipped, brief O4):
- **FR-011**: `LegacyMovieSort` closed Enumeration in `LamuFlix.Web` with members: `Id`, `Title`, `Year`, `Duration`, `ImdbRating`, `MetaScore`, `RottenTomatoes`
- **FR-012**: Explicit, non-reflection filter and sort predicates in `LamuFlix.Web` wrapping the legacy `Data.Models.Movie` entity; existing `QueryParams` and `MoviesFilterViewModel` consumed directly (no new request record)
- **FR-013**: Unknown or empty sort names fall back to `Id desc` (no 500 error)
- **FR-014**: NULLS LAST ordering for nullable sort keys in both directions. Note: `Title` is the only nullable legacy sort key (Movie.cs:8; `ImdbRating`, `RottenTomatoes` and `MetaScore` are non-nullable, Movie.cs:13-15), so it is the only key where NULLS LAST is observable
- **FR-015**: `MovieService.cs` callers (lines 152–180, 356–395 per recon) rewired to use the legacy compatibility path (owner-approved, brief:210 O2); the canonical typed query is consumed first by DEV-299 per brief Q5; behavior and external contract preserved

**Documentation**:
- **FR-016**: `docs/adr/ADR-0009.md` at exactly that path, status `Accepted`, with Context, Decision, Consequences; records typed Core model, closed sort Enumerations, validator, URL codec, equality, pinned EnrichmentStatus values, NULLS LAST rule, and removal of `DynamicQuery<T>`/`DynamicSort<T>` (FR-017); includes a required compatibility/retirement section (brief:210 O2; names DEV-298/DEV-299 and PostgreSQL-only translation evidence)

**Removal**:
- **FR-017**: `DynamicQuery<T>` and `DynamicSort<T>` removed from `src/LamuFlix.Web/Extensions/EntityExtensions.cs`; no references remain per SC-006 (brief Q2, SC-006; K13)

### Key Entities

- **MovieQuery**: Sealed record, immutable, with order-preserving sequence equality for its three ImmutableArray members
- **MovieSort, SortDirection**: Sealed Enumerations, parsed by name, serialized by name
- **EnrichmentStatus**: SmartEnum<EnrichmentStatus, int>, replacing the plain C# enum, numeric values pinned
- **MovieQueryString**: Static codec, BCL-only, supports format/parse round-trip
- **MovieQueryValidator**: FluentValidation validator, scoped to the canonical typed model
- **LegacyMovieSort**: Closed Enumeration for legacy UI sorts, distinct from MovieSort

## Success Criteria

- **SC-001**: `MovieQuery` is a sealed record with exactly the specified fields (Sort and Direction nullable per FR-001); `MovieSort` and `SortDirection` are sealed Enumerations with exact members per brief
- **SC-002**: `EnrichmentStatus` is converted to `SmartEnum<EnrichmentStatus, int>` with pinned values; Movie.cs `Status` (L31) and its assignments/comparisons (L16, 57, 63, 70, 76, 108, 116) compile against the SmartEnum; existing tests still pass
- **SC-003**: FsCheck property in `LamuFlix.UnitTests` proves round-trip: Format(query) → Parse → query equals original; property includes URL-hostile characters and validator-accepted values
- **SC-004**: Generator property proves all generated queries pass `MovieQueryValidator`
- **SC-005**: `MovieQueryValidator` boundary tests cover Page.Size 1–100, Page.Number >= 1, Runtime.Min <= Runtime.Max and Year.Min <= Year.Max when both are present, Sort and Direction present. Each rule gets a boundary example test
- **SC-006**: `DynamicQuery<T>` and `DynamicSort<T>` are removed from `EntityExtensions.cs`; no references remain
- **SC-007**: Legacy path (owner-approved, brief:210 O2): every sort key, both directions, null placement, and every predicate covered by characterization tests; unknown sort falls back to Id desc
- **SC-008**: EF translation smoke test on PostgreSQL Testcontainers fixture passes; MySQL translation is unproven and recorded in ADR-0009
- **SC-009**: All static-analysis gates pass: Roslyn, cyclomatic complexity (15 / 6 refactor), InspectCode, property-test (Category=Property), vulnerable packages, format, dotnet test; Core Stryker >= 80
- **SC-010**: Web gates (`./scripts/run-web-gates.ps1`) are NOT in this ticket's gate set (Patron ruling, CONCLUSIONS.md:11-19). No diff under `web/src/api`; report as `N/A: no web/ diff`

## Owner decision (recorded)

- **APPROVED 2026-09-29** (brief:210 O2; spec PR skipped, brief O4). DEV-297 replaces the live legacy reflection path with a temporary, explicit Web compatibility path over the existing MVC request types (`QueryParams`, `MoviesFilterViewModel`) that preserves the wider legacy UI fields. The owner accepted three parts, kept here as the record: (1) non-reflection filter and sort predicates for `Data.Models.Movie` are placed in `LamuFlix.Web`, a temporary departure from constitution §III's Infrastructure placement; (2) canonical `MovieQuery` is not built on this path, and DEV-299 is its first consumer, which clarifies DEV-297:9-10's replacement wording; (3) the MVC path runs without the canonical handler validation decorator (§V). All three hold until DEV-298/DEV-299 migrate browse. The reject path (brief:169 H1) does not apply.

## Out of Scope

The following are explicitly not included in this feature:

- No change to the Web API contract (`web/src/api`, OpenAPI, TypeScript generation)
- No EF schema change or migration
- No Data-layer `MovieEnrichmentStatus` change (legacy Data enum untouched)
- No Features:LocalPlay changes
- No root `stryker-config.json` edit
- No mediator library (MediatR)
- No handler implementation or DEV-299 wiring
- No MySQL Testcontainers dependency (PostgreSQL smoke test only)

## Edge Cases

The following edge cases must be handled:

- Unknown sort names: fall back to `Id desc` (legacy path only)
- Null vs. empty string in Text: both valid, both encoded distinctly
- Array fields with zero elements: represented as absent keys in query string
- Nullable range endpoints: min without max is valid; max without min is valid; both null is valid (per D1, RuntimeRange(null,null,false) and YearRange(null,null) are distinct from null ranges and round-trip unchanged)
- Partial ranges in codec: if Runtime is present, all three keys (Min, Max, IncludeUnknown) must be present or all absent; same for Year. A partial range makes TryParse fail per M9
- Enumerations with the same name in legacy and typed sets (e.g., Title): legacy `LegacyMovieSort.Title` and canonical `MovieSort.Title` are distinct; no cross-contamination
- EF translation on PostgreSQL vs. MySQL: PostgreSQL is proven; MySQL is unproven and accepted per ADR-0009

## Assumptions

- `Ardalis.SmartEnum` and `Ardalis.SmartEnum.SystemTextJson` are available in the configured NuGet feed and their versions are pinned in CPM per Patron ruling
- `tests/LamuFlix.UnitTests` does not exist and must be created; it will have FsCheck.Xunit.v3 (not inherited; explicitly added)
- MovieQuery overrides `Equals(MovieQuery?)` and `GetHashCode()` with order-preserving SequenceEqual over GenreIds, ActorIds, Statuses; default ImmutableArray is normalized to Empty in constructor/init so default and Empty compare equal (H2, K12)
- Legacy browse behavior (sort, filter, pagination) is documented in the Razor views and `MoviesFilterViewModel`; the compatibility path uses the existing `QueryParams` binding unchanged
- PostgreSQL Testcontainers fixture is available in `tests/LamuFlix.Test` for smoke testing EF queries; MySQL translation is not tested and is recorded as accepted risk
- The property-test gate and vulnerable-package scan are part of the verification gate suite and must pass
