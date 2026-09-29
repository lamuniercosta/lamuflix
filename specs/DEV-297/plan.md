# Implementation Plan: Typed Movie Query and Sort Model

**Branch**: `feature/DEV-297` | **Date**: 2026-09-29 | **Spec**: specs/DEV-297/spec.md

**Input**: Feature specification from `specs/DEV-297/spec.md`; Grill Phase A brief, conclusions, assumptions; recon-DEV-297

## Summary

Replace the legacy reflection-based `DynamicQuery<T>` and `DynamicSort<T>` helpers with a strongly-typed `MovieQuery` record, closed `MovieSort` and `SortDirection` Enumerations, and a `MovieQueryString` codec. Convert `Core/Domain/EnrichmentStatus` from enum to `SmartEnum<EnrichmentStatus, int>`. Create a new `tests/LamuFlix.UnitTests` project with FsCheck round-trip properties and validator boundary tests. Replace the live legacy `MovieService` callers with an explicit Web compatibility path that preserves current browse behavior. Validate with `MovieQueryValidator` in `Infrastructure/Library/`. Record the decision and PostgreSQL-only translation evidence in `docs/adr/ADR-0009.md`.

## Technical Context

**Language/Version**: C# 14 / .NET 10

**Primary Dependencies**:
- Ardalis.SmartEnum 8.2.0 (exact version per Patron ruling, brief Q1 addendum, CONCLUSIONS.md:23,:25; neither package currently present per recon-DEV-297:54-57, 160-161)
- Ardalis.SmartEnum.SystemTextJson 8.1.0 (exact version per Patron ruling, brief Q1 addendum, CONCLUSIONS.md:23,:25; neither package currently present per recon-DEV-297:54-57, 160-161)

**Storage**: N/A — query model is in-memory, no new database operations or schema changes

**Testing**: xUnit v3, FsCheck.Xunit.v3, NSubstitute, Shouldly; Testcontainers PostgreSQL fixture for EF translation smoke test

**Target Platform**: .NET 10 on Windows/Linux (ASP.NET Core backend)

**Project Type**: Core domain model library (LamuFlix.Core) + Infrastructure validation + temporary Web compatibility bridge

**Performance Goals**: N/A

**Constraints**:
- Core project must reference only `Microsoft.Extensions.Logging.Abstractions`, `System.Collections.Immutable`, and SmartEnum — no FluentValidation, DI, or reflection
- No new projects beyond the required `tests/LamuFlix.UnitTests`
- No schema change to Data-layer enums or EF mappings
- No change to Web API contract (`web/src/api`)
- Legacy Data `MovieEnrichmentStatus` enum untouched
- MySQL translation is unproven; PostgreSQL smoke test only

**Scale/Scope**: Core infrastructure; no user-facing scale concerns.

## Constitution Check

**Gate 1: §2.3 Care List**
- ✓ **Dependencies**: Ardalis.SmartEnum packages added to CPM per brief Q1 addendum ruling; cited in CONCLUSIONS.md
- ✓ **Architecture**: New `tests/LamuFlix.UnitTests` project created; no other new projects. Core/Library/ folder added to existing LamuFlix.Core. Infrastructure/Library/ folder added to existing LamuFlix.Infrastructure. Web compatibility path in existing LamuFlix.Web (temporary, owner-approved brief:210 O2; spec PR skipped, brief O4)
- ✓ **Database Schema**: No schema change; no migration. Legacy Data enum untouched per brief Q3
- ✓ **API Shape**: No change to Web API contract; external browsy behavior preserved per brief Q2 and Q5
- ✓ **Security & Local Execution**: No LocalPlay, secrets, or Process.Start concerns
- ✓ **File Scope**: EDITED: Directory.Packages.props; src/LamuFlix.Core/LamuFlix.Core.csproj; Core/Domain/EnrichmentStatus.cs; Core/Domain/Movie.cs; tests/LamuFlix.Test/Domain/{MovieFixture,MovieTests,PropertyTests}.cs; src/LamuFlix.Web/Extensions/EntityExtensions.cs; LamuFlix.sln; and src/LamuFlix.Web/Services/MovieService.cs. NEW: Core/Library/{MovieQuery,MovieSort,SortDirection,Page,RuntimeRange,YearRange,MovieQueryString}.cs; Infrastructure/Library/MovieQueryValidator.cs; tests/LamuFlix.UnitTests/** (the csproj plus plan files); docs/adr/ADR-0009.md; src/LamuFlix.Web/Library/{LegacyMovieSort,MovieServiceExtensions}.cs and tests/LamuFlix.Test/{Library/LegacyMovieSortTests,Smoke/MovieServiceEfTests}.cs. NOT EDITED: Infrastructure, Web and Test csproj files and ArchitectureTests. ArchitectureTests.cs requires no edits per M7

**Gate 2: Core Isolation**
- ✓ MovieQuery with ImmutableArray members (GenreIds, ActorIds, Statuses) in Core/Library/; Enumerations and codec in Core/Library/; SmartEnum and immutable-only dependencies. EnrichmentStatus converted in Core/Domain/; Movie.cs `Status` compiles against SmartEnum. No FluentValidation or DI in Core per K7
- ✓ MovieQueryValidator in Infrastructure/Library/ (the canonical handler path per brief Q4)

**Gate 3: No Reflection**
- ✓ DynamicQuery<T> and DynamicSort<T> removed per brief Q2; explicit predicates and closed LegacyMovieSort in Web per brief Q5

**Status**: ⚠️ **DEPARTURES, owner-approved 2026-09-29 (brief:210 O2; spec PR skipped, brief O4)** —
- §III placement (predicates for Data.Models.Movie in LamuFlix.Web) — retired by DEV-298/DEV-299
- §V (MVC path without handler validation decorator) — retired by DEV-299
Both were disclosed in the combined Q4/Q5/Q11 owner decision (brief Q5, brief:51-52), approved directly by the owner. Canonical scope (Core types, Infrastructure validator) has no departures.

## Project Structure

### Documentation (This Feature)

```text
specs/DEV-297/
├── brief.md                    # Phase A grill outcome (completed)
├── CONCLUSIONS.md              # Grill rulings (completed)
├── ASSUMPTIONS.md              # Grill assumptions (completed)
├── spec.md                      # Feature specification (this artifact)
├── plan.md                      # Implementation plan (this artifact)
├── tasks.md                     # Task breakdown
└── DRAFTING_RECEIPT.md          # Spec kit drafting record (post-drafting)
```

### Source Code Structure

**Core Layer** (`src/LamuFlix.Core/`):
```text
src/LamuFlix.Core/
├── Domain/
│   ├── EnrichmentStatus.cs      # (edit) enum → SmartEnum<EnrichmentStatus, int>
│   └── Movie.cs                 # (edit) Status compiles against SmartEnum; no Statuses member
└── Library/                      # (new folder)
    ├── MovieQuery.cs            # Sealed record with all query fields
    ├── MovieSort.cs             # Sealed Enumeration: Title, Year, Rating, Runtime
    ├── SortDirection.cs         # Sealed Enumeration: Ascending, Descending
    ├── Page.cs                  # Value object Page(int Number, int Size)
    ├── RuntimeRange.cs          # Value object RuntimeRange(int? Min, int? Max, bool IncludeUnknown)
    ├── YearRange.cs             # Value object YearRange(int? Min, int? Max)
    └── MovieQueryString.cs      # Static codec: Format, TryParse (BCL-only)
```

**Infrastructure Layer** (`src/LamuFlix.Infrastructure/`):
```text
src/LamuFlix.Infrastructure/
└── Library/                      # (new folder)
    └── MovieQueryValidator.cs   # FluentValidation: Page.Size 1–100, Page.Number >= 1, Runtime.Min <= Runtime.Max, Year.Min <= Year.Max (when present), Sort and Direction present
```

**Web Layer** (`src/LamuFlix.Web/`):
```text
src/LamuFlix.Web/
├── Extensions/
│   └── EntityExtensions.cs      # (edit) remove DynamicQuery<T>, DynamicSort<T>
├── Services/
│   └── MovieService.cs          # (edit) rewire callers to legacy compatibility path
└── Library/                      # (new folder)
    ├── LegacyMovieSort.cs       # Sealed Enumeration: Id, Title, ..., RottenTomatoes
    └── MovieServiceExtensions.cs # Explicit predicates for legacy Data.Models.Movie (no reflection)
```

**API Layer** (`src/LamuFlix.Api/`):
```text
src/LamuFlix.Api/
└── Program.cs                   # (no changes to spec/contract scope)
```

**Tests**:
```text
tests/LamuFlix.UnitTests/         # (new project)
├── Library/
│   ├── MovieQueryFixture.cs      # Shared fixture for FsCheck generator per L2, K14
│   ├── MovieQueryTests.cs        # Sequence-aware equality tests (T015)
│   ├── MovieQueryStringTests.cs  # Round-trip FsCheck property + D1/M9 example tests
│   ├── MovieQueryGeneratorTests.cs # Generator property: all generated values pass validator
│   ├── MovieQueryValidatorTests.cs # Boundary tests per FR-007
│   └── EnumerationTests.cs       # TryFromName and JSON round-trip tests (K8)
├── Domain/
│   └── EnrichmentStatusTests.cs  # Pin values 0–3 and names

tests/LamuFlix.Test/              # (no .csproj edit)
├── Library/
│   └── LegacyMovieSortTests.cs   # Characterization tests for sort/filter
├── Smoke/
│   └── MovieServiceEfTests.cs    # EF translation smoke test on PostgreSQL
└── (existing tests adapted, e.g., MovieFixture, MovieTests, PropertyTests with EnrichmentStatus)

tests/LamuFlix.ArchitectureTests/
└── ArchitectureTests.cs          # (no changes needed; new model types are in scope)
```

**Package Management** (`Directory.Packages.props` — existing):
```xml
<!-- Add these lines (exact versions per brief Q1 addendum CONCLUSIONS.md:23,:25; versions deliberately differ) -->
<PackageVersion Include="Ardalis.SmartEnum" Version="8.2.0" />
<PackageVersion Include="Ardalis.SmartEnum.SystemTextJson" Version="8.1.0" />
```

**Project Files** (edits):
- `src/LamuFlix.Core/LamuFlix.Core.csproj` — reference Ardalis.SmartEnum via CPM (for SmartEnum types)
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` — FluentValidation already present; no new reference to SmartEnum (referenced via Core) per H5
- `src/LamuFlix.Web/LamuFlix.Web.csproj` — adds a ProjectReference to LamuFlix.Core (LamuFlix.Web.csproj:19), required by the FR-011 compatibility path; ratified by Patron P-1 (brief.md:320)
- `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` — (new project file) add ProjectReferences to Core and Infrastructure. Mirror the runner shape of `tests/LamuFlix.Test/LamuFlix.Test.csproj:3-17` (brief P2; _gate-common.ps1:204-208 only discovers projects whose text contains `Microsoft.NET.Test.Sdk` or `<IsTestProject>true`): `OutputType Exe`, `IsPackable false`, `IsTestingPlatformApplication false`; PackageReferences `Microsoft.NET.Test.Sdk`, `xunit.v3`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly`, `FsCheck`, `FsCheck.Xunit.v3`, plus `Ardalis.SmartEnum.SystemTextJson` for the JSON round-trip test per H5. All are already pinned in Directory.Packages.props, so no new dependency
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` — no edits needed; already references Web and Testcontainers.PostgreSQL arrives transitively via Tests.Common per recon-DEV-297:183-184, M1

**Documentation** (`docs/adr/`):
```text
docs/adr/ADR-0009.md            # (new) Typed Query Model and Enumeration Replacement
```

## Design Decisions

### MovieQuery Sealed Record Shape
`MovieQuery` is a sealed record in `Core/Library/` with immutable fields: Text (string?), arrays (ImmutableArray<int> for GenreIds, ActorIds, ImmutableArray<EnrichmentStatus> for Statuses), nested ranges (RuntimeRange? for Runtime, YearRange? for Year), and typed properties (MovieSort, SortDirection, Page). Custom Equals(MovieQuery?)/GetHashCode override with order-preserving SequenceEqual over GenreIds, ActorIds and Statuses. Default ImmutableArray is normalized to Empty in the constructor/init so default and Empty compare equal per brief Q7 and H2.

**Rationale**: Sealed record pattern enforces immutability, enables pattern matching, and carries well-defined equality semantics. Immutable arrays prevent accidental mutation. Nullable fields simplify "not filtered" representation.

### Closed Enumerations via Ardalis.SmartEnum
`MovieSort` and `SortDirection` are sealed `SmartEnum<T, int>` with fixed members (no extension). Parsing uses `TryFromName`; serialization uses `.Name`. This matches the constitution's Enumeration requirement and enables safe SQL where clauses (no invalid sort names).

**Rationale**: SmartEnum prevents invalid members at compile time. Name-based parsing/serialization is transparent to JSON and URL codecs. The constitution forbids plain C# enums (Coding Conventions, lines 408–410).

### EnrichmentStatus SmartEnum Conversion
The existing `Core/Domain/EnrichmentStatus.cs` enum is converted to `SmartEnum<EnrichmentStatus, int>` with pinned numeric values (Pending=0, Enriched=1, NotFound=2, Failed=3). This unifies the domain model with the Enumeration convention. The Data-layer `MovieEnrichmentStatus` enum is left untouched; no EF conversion or migration is authorized per brief Q3.

**Rationale**: MovieQuery.Statuses is `ImmutableArray<EnrichmentStatus>`, and the constitution forbids plain enums. Movie.cs `Status` (L31) and its assignments/comparisons (L16, 57, 63, 70, 76, 108, 116) compile against the SmartEnum. The ripple is bounded (three test consumers per recon-DEV-297:163-178). The numeric contract is preserved, so EF and Data layer are unaffected per K7.

### MovieQueryString Codec
A static `MovieQueryString` class in `Core/Library/` provides `static string Format(MovieQuery)` and `static bool TryParse(string, out MovieQuery)`, using BCL types only (Uri.EscapeDataString/Uri.UnescapeDataString, LINQ). D1 range presence: Runtime non-null emits runtimeMin, runtimeMax, runtimeIncludeUnknown (empty for null bounds); Year non-null emits yearMin, yearMax (empty for null bounds); absent keys mean null range; partial range is malformed. M9 TryParse failure policy: returns false for malformed int/bool, non-exact TryFromName, duplicated scalar, partial range, or missing sort/direction/page/pageSize; text= is empty string; absent text is null; empty on non-range, non-text key is malformed; unknown keys ignored; no validation. Arrays are repeated keys (e.g., `genreIds=1&genreIds=2`); order is preserved. Enumerations are written by `.Name`. Sort, direction, page and pageSize are always emitted.

**Rationale**: BCL-only codec keeps Core free of HTTP dependencies. Repeated keys are standard HTTP form encoding. Deterministic output enables testing round-trip equality. Always-emitted sort/direction/page/pageSize ensures the query string is self-describing. D1 and M9 ensure codec is lossless and deterministic.

### Sequence Equality for ImmutableArray
`MovieQuery` overrides `Equals(MovieQuery?)` and `GetHashCode()` with order-preserving `SequenceEqual` over its three ImmutableArray members (GenreIds, ActorIds, Statuses). Default record equality compares by reference, which fails across round-trip parses.

**Rationale**: Sequence comparison ensures Format/Parse round-trip equality per brief Q7 H2. Normalization of default ImmutableArray to Empty in constructor/init ensures default and Empty compare equal.

### Web Compatibility Path
Per the owner's approval (brief:210 O2; spec PR skipped, brief O4), the legacy MVC path in `LamuFlix.Web` gets explicit, non-reflection predicates over `Data.Models.Movie` and a closed `LegacyMovieSort` Enumeration (Id, Title, Year, Duration, ImdbRating, MetaScore, RottenTomatoes). It preserves every Razor sort and filter choice: SearchField (case-insensitive substring), Year, DirectorId, CollectionId, GenreIds, ActorIds. Unknown sort names fall back to `Id desc`. Only an exact "desc" sorts descending (current behavior: only exact "desc" sorts descending; unknown SortBy throws ArgumentException; pinned null order). The only intended deltas, verified after the rewire, are unknown sort → Id desc and NULLS LAST. The `QueryParams` and `MoviesFilterViewModel` are consumed directly (no new request record). This is a temporary constitution §III departure until DEV-298/DEV-299 migrate the handler.

**Rationale**: The ticket requires replacing the reflection helpers now; the canonical `MovieQuery` is scoped to the new handler (DEV-299). A bridge keeps the legacy UI working during the migration. Explicit predicates are type-safe and auditable. The fallback prevents 500 errors for unknown sorts. Runtime/year range, statuses and watchlist do not exist in the legacy path per brief H3, K3; the exact set is SearchField, Year, CollectionId, DirectorId, GenreIds, ActorIds.

### Test Strategy
**FsCheck Property (UnitTests)**:
- `MovieQueryStringTests.cs`: Generator produces validator-accepted queries with URL-hostile characters. Property asserts Format(q) → Parse → q equals original after round trip.
- `MovieQueryGeneratorTests.cs`: Generator property proves all generated queries pass `MovieQueryValidator`.

**Boundary Tests (UnitTests)**:
- `MovieQueryValidatorTests.cs`: Explicit boundary cases per FR-007 (Page.Size, Page.Number, Runtime.Min/Max, Year.Min/Max, Sort, Direction).
- `EnumerationTests.cs` (K8): TryFromName tests (known names succeed, unknown fail), JSON round-trip by Name for MovieSort, SortDirection, EnrichmentStatus via Ardalis.SmartEnum.SystemTextJson (SmartEnumNameConverter). Maps to FR-002, FR-003, FR-005, FR-009.

**Characterization Tests (LamuFlix.Test)**:
- `LegacyMovieSortTests.cs`: Every sort key (Title, Year, Duration, ImdbRating, MetaScore, RottenTomatoes, Id), both directions, null placement (current order), every predicate (SearchField, Year, CollectionId, DirectorId, GenreIds, ActorIds). Commit these green before replacing the helpers per brief K3. Retarget these tests from DynamicQuery/DynamicSort to LegacyMovieSort and MovieServiceExtensions; change ONLY the two ruled expectations (unknown sort -> Id desc; NULLS LAST); every other assertion stays unchanged; green before T036.
- `MovieServiceEfTests.cs`: Smoke test EF translation on PostgreSQL Testcontainers fixture. MySQL translation is unproven (production uses Pomelo 8.0.31); risk recorded in ADR-0009.

**Existing Tests**:
- `MovieFixture`, `MovieTests`, `PropertyTests`: Adapt to converted `EnrichmentStatus` without weakening assertions (assertions unchanged, FR-005). SmartEnum members are not constants, so `InlineData` arguments (MovieTests.cs:52-67, CS0182) become `MemberData`/`TheoryData` (constitution §IX); `Enum.GetValues<EnrichmentStatus>()` (PropertyTests.cs:156) becomes `EnrichmentStatus.List`; the constant `switch` (MovieFixture.cs:19) matches on `.Value` or `.Name`.

### Validation Strategy
`MovieQueryValidator` in `Infrastructure/Library/` applies rules only to the canonical `MovieQuery` per FR-007:
- Page.Size: 1–100
- Page.Number: >= 1
- Runtime.Min <= Runtime.Max and Year.Min <= Year.Max when both are present
- Sort and Direction present
- Empty ranges are NOT rejected per D1

The legacy path has no validation decorator; the owner accepted this (brief:210 O2, Q11).

**Rationale**: Validation belongs in Infrastructure, co-located with handlers. The legacy path is transitional and doesn't block browse until DEV-299 migrates.

## Grill Decisions Reflected

- **Q1 & Q1 addendum**: SmartEnum and SystemTextJson packages in CPM; Core references SmartEnum only; SystemTextJson referenced ONLY by tests/LamuFlix.UnitTests; `tests/LamuFlix.UnitTests` created per ticket name
- **Q2**: DynamicQuery/DynamicSort removed; MovieService rewired to legacy compatibility path (typed query consumed by DEV-299) per K11
- **Q3**: EnrichmentStatus converted to SmartEnum; Movie.cs `Status` (L31) and assignments/comparisons compile against SmartEnum, no Statuses member (recon-DEV-297:163-178); test consumers adapted without weakening per K7
- **Q4**: Query types in Core/Library/; Validator in Infrastructure/Library/; Web predicates in LamuFlix.Web (owner-approved, brief:210 O2)
- **Q5**: Web bridge with explicit predicates and LegacyMovieSort; canonical MovieQuery first consumed by DEV-299; no new request record or validator
- **Q6**: NULLS LAST on nullable sort keys via `OrderBy(x => key == null ? 1 : 0).ThenBy(key)` pattern; legacy PageSize not clamped; 1..100 and >= 1 rules only in MovieQueryValidator
- **Q7**: MovieQueryString codec (BCL-only); sequence equality for ImmutableArray; FsCheck round-trip property with URL-hostile characters; empty ranges distinct from null
- **Q8**: Characterization tests; PostgreSQL smoke test; EnrichmentStatus pin tests (UnitTests); full dotnet test and Core Stryker >= 80; MySQL translation unproven
- **Q9**: ADR-0009 at exact path; Accepted status; required compatibility/retirement section; PostgreSQL-only translation evidence
- **Q10 & Closing bar**: Combined Q4/Q5/Q11 approved directly by the owner (brief:210 O2); spec PR skipped, no Gate 1 checkbox (brief O4); Phases 5, 6a-6d and 7 run in order; step 7 (deletion, T036) lands only after 6b is green

## Risk Summary

- **MySQL Translation**: Production uses Pomelo MySQL 8.0.31. EF translation is tested only on PostgreSQL Testcontainers. Risk is accepted and recorded in ADR-0009 per brief Q8. No MySQL Testcontainers dependency added.
- **Legacy Behavior Deltas** (owner-approved, brief:210 O2):
  - Unknown sort falls back to `Id desc` (today: 500 error)
  - NULLS LAST ordering (deliberate per Q6; observable only on `Title`, the sole nullable legacy sort key, Movie.cs:8)
- **Transitional Web Path**: No handler validation decorator; accepted by the owner (brief:210 O2). DEV-299 owns retirement.

## Review & Remediation Caps

Per brief Q10:
- **Spec Kit analyze/fix**: 3 rounds
- **Review**: 2 rounds
- **Remediation**: 2 commits per round
- **Severity**: Verified Critical/High findings always block. In-scope Medium behavior, spec, or gate defects block. Low and non-blocking Medium maintenance findings go to follow-ups.

## Gate Expectations

Report each gate's native exit code per brief Q10, brief:174-183. Exit codes: 0 pass, 1 fail, 2 = SKIPPED (scope-empty, BLOCKING) or OPT-OUT (disabled in harness.yml, non-blocking):
- `./scripts/run-roslyn-analyzers.ps1`
- `./scripts/run-cyclomatic-complexity.ps1` (at most 15), then `-Threshold 6` (refactor)
- `./scripts/run-jetbrains-inspectcode.ps1`
- `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` (Category=Property); FsCheck tests MUST carry the Category=Property trait, else gate exits 2 SKIPPED and blocks
- `./scripts/run-vulnerable-packages.ps1`
- `dotnet format --verify-no-changes`
- `dotnet test`
- Core Stryker >= 80, scoped by command line (root stryker-config.json unedited)
- Web gates (`./scripts/run-web-gates.ps1`): outside this ticket's required gate set (Patron, CONCLUSIONS.md:11-19). Not scheduled or run; reported `N/A: no web/ diff`, never PASS or a required scope-empty SKIPPED

**Success**: All required gates exit 0 or configured OPT-OUT; a scope-empty SKIPPED on a required gate blocks per K9.
