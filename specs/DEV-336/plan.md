# Implementation Plan: DEV-336 MovieFeatures value object and mapper

**Branch**: `feature/336-spec` | **Date**: 2026-10-19 | **Spec**: `specs/DEV-336/spec.md`

**Input**: Feature specification from `specs/DEV-336/spec.md`, settled grill brief `specs/DEV-336/brief.md`, facts from note `recon-DEV-336`, Patron rulings in `specs/DEV-336/CONCLUSIONS.md`.

**Note**: Planning artifact only (Phase A). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1 (brief task ordering).

## Summary

Add a sealed `MovieFeatures` record and a pure `MovieFeaturesMapper.From(Movie)` inside the existing Core recommendations feature namespace. The record is the immutable nine-field projection of persisted `MovieMetadata`. The mapper copies that metadata into the record, returns empty sets and null scalars when metadata is missing, and does not mutate the movie. Focused unit and property tests lock the contract. Nothing else ships.

## Technical Context

**Language/Version**: C# / .NET 10. Core and unit tests only. No web change.

**Primary Dependencies**: None added. `System.Collections.Immutable` is already an allowed Core reference (constitution I). Tests use the existing xUnit v3, Shouldly, and FsCheck integration.

**Storage**: N/A. No database, migration, or persistence edit. OMDb persistence stays DEV-411.

**Testing**: xUnit v3, Shouldly, and FsCheck in `LamuFlix.UnitTests`. The property-test gate is active, with no opt-out. No Testcontainers, HTTP fixture, or mocks.

**Target Platform**: Existing Core library. No new host.

**Project Type**: Existing ports-and-adapters solution. One new namespace folder inside existing `LamuFlix.Core`. Not a new project or layer.

**Performance Goals**: No target is set in the brief. None is invented here.

**Constraints**: TreatWarningsAsErrors. No new NuGet or npm dependency. No new project, port, handler, route, or DI registration. No interface, service wrapper, extension method, extra overload, or factory. No edit to `Movie`, `MovieMetadata`, options, Infrastructure, Api, web, packages, or configuration. No `Features:LocalPlay`, secrets, or `Process.Start` change. No new glossary entry or ADR. Thresholds stay in `harness.yml`.

**Scale/Scope**: Two new production files and three new test files, named in the brief. No other file.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules, lines 21–24). The slice matches DEV-336: a Core `MovieFeatures` value object mapped from persisted `Movie` / `MovieMetadata`, with empty sets and null scalars when metadata is missing, plus focused unit tests (note `DEV-336:18`; brief Q1). Nothing is added or dropped. No owner checkbox (brief Q1, Q5; `CONCLUSIONS.md` closure).
- Core may reference `System.Collections.Immutable` (constitution I, lines 123–135). The new types live in `LamuFlix.Core.Features.Recommendations` and reference only shared Domain types `Movie` and `MovieMetadata`. Feature folders do not reference each other (constitution I, lines 133–135). Recon records that this folder does not exist yet; the ticket names Core (Recommendations), so the folder is ticket-decided and not a new architectural layer (`specs/PRODUCT.md` care list item 2, lines 42–44; recon lines 17 and 46).
- No new use case, handler, MediatR registration, port, or `Result<T>` (constitution II, lines 149–162). `MovieFeatures` is a value object. `From` is a static function, not a handler.
- No filter, query-model, or enumeration change (constitution III, lines 168–176; Coding Conventions, lines 430–439).
- No `Movie` state transition (constitution PR Quality Gates, lines 379–380). `Movie` and `MovieMetadata` are not edited.
- No endpoint, status code, OpenAPI document, or generated TypeScript (constitution API and Contract Rules, lines 483–494; `specs/PRODUCT.md` care list item 4). Recon line 48: no public API shape.
- No schema or migration (constitution and `specs/PRODUCT.md` care list item 3). Persistence and OMDb mapping stay DEV-411 (recon lines 9 and 47).
- No secret, machine path, clock, `Features:LocalPlay`, or `Process.Start` change (constitution VII, lines 263–279; recon lines 49–50).
- Vocabulary (brief approach; `CONCLUSIONS.md` closure): `MovieFeatures` is the projection name fixed in the brief. No new glossary term and no ADR (constitution VIII, lines 284–299; Documentation Rules, lines 497–503). `CONTEXT.md` is not edited. Patron recorded no constitution deviation.
- Tests use xUnit v3, Shouldly, and FsCheck in `LamuFlix.UnitTests` (constitution IX, lines 304–325). No infrastructure mock and no EF InMemory. No NSubstitute, because the brief requires no mocks. Gates are the configured scripts with no threshold change (Static-Analysis Gates, lines 400–416; brief task ordering).
- Types are a sealed record and immutable collections (Coding Conventions, lines 426–429). No comments except AAA test headers (lines 440–450). `nameof` applies to exception parameter references (line 453). The observable guard contract remains `ArgumentNullException` only; the brief sets no message text.

Post-design re-check: the design below adds no package, project, port, handler, route, schema, glossary term, or edit outside the five named files. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-336/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes the record, the mapper, the files, and the test matrix. This ask writes the spec, plan, and tasks. There is no HTTP or other public contract to put under `contracts/`. `AGENTS.md` is not updated: it is outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs
src/LamuFlix.Core/Features/Recommendations/MovieFeaturesMapper.cs

tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesMapperTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesPropertyTests.cs
```

**Structure Decision**: Existing `LamuFlix.Core` and `LamuFlix.UnitTests` only. Recon lists no `Features/Recommendations` folder and no recommendations test folder; both are created by these files. No other path is in scope. If the contract cannot be implemented inside these five files, stop for a decision. Do not widen the set.

## Design Overview

1. **Record.** `MovieFeatures` is a sealed record in `LamuFlix.Core.Features.Recommendations`. Nine get-only properties, in this source order:

   | Property | Type | Persisted source |
   | --- | --- | --- |
   | Genres | `ImmutableHashSet<string>` | `Movie.Metadata.Genres` |
   | Directors | `ImmutableHashSet<string>` | `Movie.Metadata.Directors`, every entry |
   | Writers | `ImmutableHashSet<string>` | `Movie.Metadata.Writers` |
   | Cast | `ImmutableHashSet<string>` | `Movie.Metadata.Actors` |
   | ReleaseYear | `LamuFlix.Core.Domain.ReleaseYear?` | `Movie.Metadata.ReleaseYear` |
   | Runtime | `LamuFlix.Core.Domain.Runtime?` | `Movie.Metadata.Runtime` |
   | ImdbRating | `LamuFlix.Core.Domain.ImdbRating?` | `Movie.Metadata.ImdbRating` |
   | MetaScore | `int?` | `Movie.Metadata.MetaScore`, value-preserving nullable conversion |
   | FullPlot | `string?` | `Movie.Metadata.Synopsis` |

2. **Constructor.** One public constructor. Parameter names, in order: `genres`, `directors`, `writers`, `cast`, `releaseYear`, `runtime`, `imdbRating`, `metaScore`, `fullPlot`. The four collection parameters are `IEnumerable<string>?`. Scalar parameter types match the table. Every parameter defaults to null. No init setter. No second factory. `new MovieFeatures()` is valid and yields four empty sets and five null scalars.

3. **Set normalization.** Null collections become empty `ImmutableHashSet<string>` values created with `StringComparer.OrdinalIgnoreCase`. Non-null inputs are materialized with `ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)`. A runtime null element in Genres, Directors, Writers, or Cast is preserved. Duplicate nulls collapse to one member. Do not filter or reject null elements. A null collection stays empty; that is a different case from a null element. Q6 stays as ruled. Compass verification item 1 (`findings-DEV-336-Compass:12`) probed this call on .NET 10: `new[] { "A", null, "a" }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)` does not throw, the set count is 2, `Contains(null)` is true, and `SetEquals` is true between two sets that both contain null. That probe is the feasibility evidence for keeping null members. `ImmutableHashSet.Equals` is reference equality, so feature equality uses `SetEquals` per set. Duplicates collapse under that comparer. Order is not significant. Element text is copied as supplied: no trim, no case rewrite, no blank filtering. Which spelling survives a case-equivalent duplicate is unspecified, and set iteration order is unspecified. Declared public types stay `IEnumerable<string>?` constructor parameters and `ImmutableHashSet<string>` properties. Do not change the element type to `string?`. Runtime null members are deliberately kept under those non-nullable annotations. DEV-288 consumers must handle a null member before dereferencing it. That consumer hazard, and the unspecified spelling and iteration order, are noted with no ticket. This feature adds no consumer work. Scalars, including null, empty, and whitespace `FullPlot`, pass through. Do not add validation beyond what the existing Domain value objects already enforce. Do not unwrap `ReleaseYear`, `Runtime`, or `ImdbRating`. A deliberately boxed `default(ImmutableArray<string>)` supplied as an `IEnumerable<string>` is unsupported input, both on direct `MovieFeatures` construction and on a hypothetical upstream `MovieMetadata` call. Do not promise normalization to empty, and do not promise a particular exception. Do not add an `IsDefault` branch, an upstream `MovieMetadata` edit, or a test for that input. Recon `recon-DEV-336:55-85` records that current production and test constructors do not supply a default array: `MovieMetadata` freezes a null source to non-default `ImmutableArray<string>.Empty` (`MovieMetadata.cs:22-23`, `41-42`, `48-49`), `Genres` and `Writers` are get-only (`MovieMetadata.cs:73`, `75`) so there is no `with` or init bypass, and no production or test caller passes any `ImmutableArray`. Do not claim that boxed default input is impossible in general. The BCL identity fast-path of that hypothetical upstream call was not verified and is not asserted. Ordinary null and empty collections, and Q6 runtime null members, stay fully supported.

4. **Equality.** Implement `Equals(MovieFeatures?)` and `GetHashCode()` over all nine fields. Sets compare with null-safe `SetEquals` under `StringComparer.OrdinalIgnoreCase`. A null element is a member. Each set hash is a commutative aggregate: an unchecked sum, or XOR, of `StringComparer.OrdinalIgnoreCase.GetHashCode(element)`, using 0 when the element is null. Do not fold set enumeration into an order-sensitive hash combiner. Combine the four completed set aggregates with the scalar hashes; that outer combination is not fixed further. `FullPlot` equality is ordinal. Its hash is 0 when `FullPlot` is null, and `StringComparer.Ordinal.GetHashCode(FullPlot)` otherwise. Other scalars use their existing value equality. Equivalent values built separately, including values whose sets contain a null member, compare equal and share a hash code. Equality still distinguishes a set that contains a null member from the same set without that member, including when the two hash codes match. Null and a different runtime type compare unequal. Do not require unequal values to have different hash codes. Do not pin unchecked sum versus XOR in tests.

5. **Mapper.** `MovieFeaturesMapper` is a public static class. Its sole public mapping member is `public static MovieFeatures From(Movie movie)`. No instance state and no instance construction.
   - Null `movie`: throw `ArgumentNullException(nameof(movie))`. Do not pin the exception message.
   - Null `Movie.Metadata`: four empty `OrdinalIgnoreCase` sets and five null scalars. Do not throw.
   - Metadata present: map the nine sources in the table. Tolerate partial collections and absent scalars. Persisted MetaScore 0, 100, and null become `int?` 0, 100, and null.
   - Never substitute `Movie.Title`, `Movie.Path`, or `Movie.ParsedReleaseYear`.
   - Do not mutate `Movie`, `MovieMetadata`, or their collections. The result owns the immutable snapshots.
   - No DI registration, interface, service wrapper, extension method, extra overload, or factory.

6. **Tests.** Build movies with the existing `Movie.Rehydrate` path recon cites (`src/LamuFlix.Core/Domain/Movie.cs`, metadata parameter nullable) and existing `MovieMetadata` construction. Do not edit those types. Name every test method `Method_Condition_Expected`. Property tests stay in one file, `MovieFeaturesPropertyTests.cs`: construction properties first, mapping properties after, in that order. Each property method uses FsCheck.Xunit `[Property(MaxTest = 100)]` and `[Trait("Category", "Property")]` so `./scripts/run-property-tests.ps1` discovers them. The precedent files recon names are `MovieMetadataCollectionsPropertyTests.cs` and `MovieRehydratePropertyTests.cs`. Each property sets an explicit deterministic FsCheck Replay seed. Do not use `System.Random`. Do not add a second property-test file. Do not add a test for boxed `default(ImmutableArray<string>)` input.

7. **Proof order.** Anvil: record, its unit tests, and the construction/equality properties; commit; then mapper, its unit tests, and the mapping properties; commit. Cog refactors that bounded diff. Gauge runs the backend gates. Do not start until Gate 1.

## Loop discipline

- **Closing bar:** Zero unresolved Critical or High findings. Medium and Low do not block. Patron triages remaining findings. Out-of-scope Critical or High, or broken behaviour, goes through Patron and Rigger for a follow-up record. Other non-broken observations are `noted, no ticket`. Never silently waive or demote a blocker.
- **Round cap:** Maximum two review/remediation rounds. Stop and report an unresolved above-bar finding after the cap. Do not add a third fix round or relax a gate.
- **Frozen scope:** A sealed Core Recommendations `MovieFeatures` record, a pure mapper from persisted `Movie` / `MovieMetadata`, and focused unit and property tests for the contracts in this plan. No HTTP, infrastructure, persistence, OMDb persistence (DEV-411), recommendation ranking, scoring, embedding, UI, dependency, new project or layer, database schema, playback execution, secrets, or `Process.Start` changes. Anything else is a follow-up issue, not a finding in this round.

## Complexity Tracking

No constitution violation requires tracking. Patron recorded no constitution departure and no §2.3 escalation (brief Q1, Q5).
