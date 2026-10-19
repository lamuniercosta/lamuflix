# Feature Specification: DEV-336 MovieFeatures value object and mapper

**Feature Branch**: `feature/336-spec`

**Created**: 2026-10-19

**Status**: gate1: provisional

**Input**: Ticket DEV-336 (size M, parent DEV-288, no ui); settled grill brief `specs/DEV-336/brief.md` (grill closed 5/12); facts from note `recon-DEV-336`; Patron rulings in `specs/DEV-336/CONCLUSIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hold persisted movie facts as one comparable feature value (Priority: P1)

A caller builds a MovieFeatures value from the nine persisted facts: genres, directors, writers, cast, release year, runtime, IMDb rating, MetaScore, and full plot. The value keeps those facts as an immutable projection. Two values built from the same facts compare equal, including when set members differ only by order or letter case. Changing any one fact makes them unequal.

**Why this priority**: The ticket's value object is the projection later mapping writes. Without it there is nothing to map into.

**Independent Test**: Construct MovieFeatures directly, with no Movie. Cover default and null collections, a runtime null element in each of the four sets, duplicate-null collapse, duplicate and case folding, source-list mutation, ordinal full-plot text, and equality and hash across all nine fields. For each set, when scalars and non-null members match, adding or removing a null member leaves the whole value hash unchanged, and equality still distinguishes that membership.

**Acceptance Scenarios**:

1. **Given** no arguments, or explicit null collections, **When** a MovieFeatures value is constructed, **Then** the four sets are empty and the five scalars are null, and the empty sets still compare membership without regard to case.
2. **Given** collection inputs that contain case-equivalent duplicates, blanks, and different orders, **When** a MovieFeatures value is constructed, **Then** duplicates collapse without regard to case or order, blanks and surrounding text are kept, and the contract does not promise which spelling survives a case-equivalent duplicate.
3. **Given** a constructed MovieFeatures value, **When** the caller mutates the lists that were passed in, **Then** the value's sets do not change.
4. **Given** two MovieFeatures values built independently from equivalent facts, including reordered and case-varied sets and sets that contain a null member, **When** they are compared, **Then** they are equal and share a hash code. Set equality is null-safe. For each set, when the scalars and the non-null members match, adding or removing a null member leaves the whole value hash unchanged, and equality still treats that null member as present or absent. Changing any one of the nine fields makes them unequal. A null value and a value of another type compare unequal. Unequal values are not required to have distinct hash codes.
5. **Given** full-plot text that is null, empty, whitespace, or differs only by letter case, **When** values are constructed and compared, **Then** that text is kept as supplied and compared with case-sensitive ordinal equality. Case folding applies to the four sets only.
6. **Given** a runtime null element in genres, in directors, in writers, and in cast, including a repeated null in each, **When** a MovieFeatures value is constructed, **Then** each set keeps that null element, duplicate nulls collapse to one member, and the element is not filtered or rejected. A null collection is still empty. Constructor parameters stay `IEnumerable<string>?` and the four properties stay `ImmutableHashSet<string>`.

---

### User Story 2 - Project a stored movie into that feature value (Priority: P2)

A caller projects one persisted Movie into a MovieFeatures value through the single mapping entry point. Every director is kept. Actors become cast. Synopsis becomes full plot. The other six facts come from the same metadata. A movie with no metadata yields empty sets and null scalars. The movie itself is left unchanged.

**Why this priority**: This is the ticket's mapping. It is useful once the value object exists, and the frozen scope includes both.

**Independent Test**: Map movies that already exist as persisted Movie and MovieMetadata values. Cover full metadata, missing metadata, a null movie, each partial collection and scalar, a runtime null element in each mapped collection, duplicate-null collapse, mapping-time duplicate folding, null-member equality and hash coherence, and source-list mutation after mapping.

**Acceptance Scenarios**:

1. **Given** a movie whose metadata carries distinct values for all nine sources, **When** it is mapped, **Then** genres, every director, writers, cast (from actors), release year, runtime, IMDb rating, MetaScore, and full plot (from synopsis) appear on the result.
2. **Given** a movie with no metadata, including one that still has a title and a parsed release year, **When** it is mapped, **Then** the four sets are empty, the five scalars are null, and `Movie.Title`, `Movie.Path`, and `Movie.ParsedReleaseYear` are not substituted.
3. **Given** a null movie, **When** it is mapped, **Then** the call fails with ArgumentNullException.
4. **Given** metadata where some collections are empty and some scalars are absent, **When** it is mapped, **Then** each present collection and scalar is kept and each absent one stays empty or null. Each collection and each scalar is covered on its own, not only as one combined example.
5. **Given** director or actor lists that contain case-equivalent duplicates, **When** the movie is mapped, **Then** those sets collapse without regard to case or order, without trimming or dropping blanks.
6. **Given** a mapped MovieFeatures value, **When** the movie's director or actor lists are mutated, **Then** the mapped sets do not change.
7. **Given** metadata that carries a runtime null element in genres, directors, writers, and actors, including a repeated null in each, **When** the movie is mapped, **Then** each resulting set keeps that null element, duplicate nulls collapse to one member, and the element is not filtered or rejected. Two mapped values that match, including those null members, compare equal and share a hash code. For each mapped set, when scalars and non-null members match, adding or removing a null member leaves the whole value hash unchanged, and equality still distinguishes that membership.

### Edge Cases

- Missing metadata and a default MovieFeatures value both yield four empty case-insensitive sets and five null scalars. Missing metadata does not throw.
- A null movie throws ArgumentNullException. A movie whose metadata is null does not.
- `Movie.Title`, `Movie.Path`, and `Movie.ParsedReleaseYear` are never fallbacks for missing metadata or missing scalars.
- Set duplicates collapse without regard to case or order. Element text is not trimmed, case-rewritten, or blank-filtered. Which spelling survives a case-equivalent duplicate is not part of the contract.
- A null element in Genres, Directors, Writers, or Cast is kept at runtime on construction and on mapping. Duplicate nulls collapse to one member. The element is not filtered or rejected. A null collection is still empty. Declared public element types stay non-nullable `string` on `IEnumerable<string>?` parameters and `ImmutableHashSet<string>` properties.
- A null set member participates in equality. For each set, when scalars and non-null members match, adding or removing that null member leaves the whole value hash unchanged, and equality still distinguishes the membership. Equal values that contain a null member share a hash code.
- A deliberately boxed `default(ImmutableArray<string>)`, passed as an `IEnumerable<string>` input, is unsupported on direct MovieFeatures construction and on a hypothetical upstream MovieMetadata call. This feature does not promise normalization to an empty set and does not promise a particular exception for that input. No `IsDefault` handling is part of the contract. Current production and test constructors do not supply a default array (`recon-DEV-336:55-85`; `MovieMetadata.cs:22-23`, `41-42`, `48-49`, `73`, `75`). That input is not claimed to be impossible in general, and the unverified BCL fast-path is not claimed to throw. Ordinary null and empty collections, and runtime null members, stay supported.
- Empty and default sets use the same case-insensitive membership comparison as populated sets.
- Mutating a source list after construction, or mutating persisted director or actor lists after mapping, does not change the feature value.
- Full plot keeps null, empty, and whitespace text. Its equality is ordinal, so a case difference is a difference. Set equality is the only case-insensitive comparison.
- Unequal feature values may share a hash code. Tests do not require distinct hashes for unequal values.
- No HTTP route, persistence write, OMDb call, recommendation ranking, or playback behavior changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a sealed MovieFeatures record in `LamuFlix.Core.Features.Recommendations`. It has exactly nine get-only properties: Genres, Directors, Writers, and Cast as `ImmutableHashSet<string>`; ReleaseYear, Runtime, and ImdbRating as `LamuFlix.Core.Domain.ReleaseYear?`, `Runtime?`, and `ImdbRating?`; MetaScore as `int?`; FullPlot as `string?`.
- **FR-002**: MovieFeatures MUST have one public constructor, in the order genres, directors, writers, cast, releaseYear, runtime, imdbRating, metaScore, fullPlot. The four collection parameters accept a sequence of strings or null. The scalar parameters match the properties. Every parameter defaults to null. There are no init setters and no second factory.
- **FR-003**: Null collection inputs MUST become empty `ImmutableHashSet<string>` values using `StringComparer.OrdinalIgnoreCase`. Non-null inputs MUST be materialized with that same comparer so a later change to the source cannot change the record. A runtime null element inside Genres, Directors, Writers, or Cast MUST be preserved as supplied. Duplicate null elements MUST collapse to one set member. Null elements MUST NOT be filtered or rejected. That rule is separate from a null collection, which remains empty. Other duplicates MUST collapse under the same comparer. Order is not part of the contract. Element text MUST be preserved: no trimming, no case rewriting, and no blank filtering. The contract MUST NOT promise which spelling survives a case-equivalent duplicate. Declared public element types stay unchanged: constructor parameters remain `IEnumerable<string>?` and the four properties remain `ImmutableHashSet<string>`.
- **FR-004**: Scalar values MUST be preserved verbatim, including a null, empty, or whitespace full plot. Mapping and construction MUST NOT substitute `Movie.Title`, `Movie.Path`, or `Movie.ParsedReleaseYear`. They MUST NOT add domain validation or correction beyond the validation the existing domain value objects already perform.
- **FR-005**: MovieFeatures MUST implement `Equals(MovieFeatures?)` and a consistent `GetHashCode()` across all nine fields. The four sets MUST compare with null-safe `SetEquals` under `StringComparer.OrdinalIgnoreCase`. A null element is a set member. Each set hash MUST be a commutative aggregate: either an unchecked sum or XOR of `StringComparer.OrdinalIgnoreCase.GetHashCode(element)`, with 0 for a null element. Set enumeration MUST NOT be fed into an order-sensitive hash combiner. The four completed set aggregates MUST then be combined with the scalar hashes; that outer combination is not otherwise fixed. Scalars MUST use their existing value equality. FullPlot equality MUST be ordinal. The FullPlot hash MUST be 0 when FullPlot is null, and `StringComparer.Ordinal.GetHashCode(FullPlot)` otherwise. Independently constructed equivalent values, including values whose sets contain a null member, MUST compare equal and MUST have equal hash codes. Equality MUST still distinguish a set that contains a null member from the same set without that member. A null value and a value of another type MUST compare unequal. The contract MUST NOT require unequal values to have distinct hash codes.
- **FR-006**: `MovieFeaturesMapper` MUST be a public static class. Its sole public mapping member MUST be `public static MovieFeatures From(Movie movie)`. It MUST NOT hold instance state and MUST NOT be constructed. A null movie MUST throw `ArgumentNullException`. A movie with null metadata MUST return four empty `OrdinalIgnoreCase` sets and five null scalars and MUST NOT throw. When metadata is present, From MUST map all nine sources, including partial and empty collections and absent nullable scalars. Every persisted director MUST be kept. Actors MUST map to Cast. Synopsis MUST map to FullPlot. MetaScore MUST be a value-preserving nullable conversion to `int?`.
- **FR-007**: Mapping MUST have no side effects. It MUST NOT mutate the Movie, its metadata, or their collections. The result MUST own immutable snapshots of the director and actor lists.
- **FR-008**: Proof MUST be focused unit tests and property tests in the three named test files, using the existing xUnit v3, Shouldly, and FsCheck integration. Test method names MUST follow `Method_Condition_Expected`. No new library, database, HTTP fixture, container, or mock is required. The property-test gate stays active, with no opt-out. Properties MUST cover permutation, case, and duplicate invariance and equality/hash coherence. Each property MUST use FsCheck.Xunit `[Property(MaxTest = 100)]` and `[Trait("Category", "Property")]`, with an explicit deterministic FsCheck Replay seed. Properties MUST NOT mirror the implementation and MUST NOT use `System.Random`. Construction properties and mapping properties MUST stay in the one file `MovieFeaturesPropertyTests.cs`, with construction properties written first.
- **FR-009**: This feature MUST NOT add HTTP, infrastructure, persistence, OMDb persistence, recommendation ranking, scoring, embedding, UI, a dependency, a project, a layer, a database schema change, playback execution, secrets, or process launching. It MUST NOT edit Movie, MovieMetadata, options, Infrastructure, Api, web, packages, or configuration. It MUST NOT add a dependency-injection registration, an interface, a service wrapper, an extension method, an extra overload, or a factory.

### Key Entities

- **MovieFeatures**: The immutable projection of persisted movie metadata. Nine fields only. It is not a recommendation result and not a new metadata source.
- **Movie**: The persisted movie passed to mapping. Its metadata may be absent. Mapping does not change it and does not read its title or parsed release year as substitutes.
- **Movie metadata**: The persisted source for the nine fields. Directors and actors are lists. Genres and writers are collections. Synopsis is the full-plot source. Release year, runtime, and IMDb rating are existing domain values. MetaScore is the persisted score widened to a nullable integer.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Mapping one movie that has all nine metadata values keeps every director and maps actors to cast and synopsis to full plot, with the other six values unchanged.
- **SC-002**: Mapping a movie with no metadata yields four empty case-insensitive sets and five null scalars, even when that movie has a title and a parsed release year. Mapping a null movie fails with ArgumentNullException and does not return a value.
- **SC-003**: Partial metadata keeps exactly the collections and scalars that are present. Each of the four collections and each of the five scalars has its own proof, not only one combined example.
- **SC-004**: Construction and mapping collapse duplicate set members regardless of case and order, keep blank text, and do not trim. They keep a runtime null element in each of the four sets, collapse duplicate nulls to one member, and do not filter or reject null elements. A null collection stays empty. Declared public element types stay `string` on `IEnumerable<string>?` parameters and `ImmutableHashSet<string>` properties. Mutating a source list afterward leaves the feature value unchanged. Empty and default sets use the same case-insensitive membership comparison.
- **SC-005**: Two equivalent feature values compare equal and share a hash code, including when sets are reordered, differ only by case, or contain a null member. Set equality is null-safe. For each set, when scalars and non-null members match, adding or removing a null member leaves the whole value hash unchanged, and equality still distinguishes that membership. Changing each of the nine fields independently makes the values unequal. Full plot keeps null, empty, and whitespace text, and a case difference in full plot makes the values unequal.
- **SC-006**: Backend verification runs with no lowered threshold and no property-test opt-out. An unchanged web tree is a scope-empty skip and is not recorded as a pass.

## Assumptions

- The set field names are Genres, Directors, Writers, and Cast. Directors keeps the whole persisted list. Cast maps actors. These are confirmed `[assumed]` conventions in `specs/DEV-336/ASSUMPTIONS.md`, not open questions.
- The four sets are case-insensitive immutable sets, including the empty defaults. This is a confirmed `[assumed]` convention in `specs/DEV-336/ASSUMPTIONS.md`, not an open question.
- MetaScore is a nullable integer and full plot is a nullable string mapped from synopsis. These are confirmed `[assumed]` conventions in `specs/DEV-336/ASSUMPTIONS.md`, not open questions.
- Persisted text is kept verbatim: no trimming, case rewriting, blank filtering, or movie-field fallback. This is a confirmed `[assumed]` convention in `specs/DEV-336/ASSUMPTIONS.md`, not an open question.
- The sole mapping entry point is named MovieFeaturesMapper.From and takes one Movie. This is a confirmed `[assumed]` convention in `specs/DEV-336/ASSUMPTIONS.md`, not an open question.
- The constructor parameter names and the get-only, single-constructor shape are confirmed `[assumed]` conventions in `specs/DEV-336/ASSUMPTIONS.md`, not open questions.
- The three test file names under the unit-test recommendations folder are confirmed `[assumed]` conventions in `specs/DEV-336/ASSUMPTIONS.md`, not open questions.
