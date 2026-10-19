# Tasks: DEV-336 MovieFeatures value object and mapper

**Input**: Design documents from `specs/DEV-336/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). `setup-tasks.ps1 -Json` reported `AVAILABLE_DOCS` empty.

**Tests**: Included. Brief Q5 requires them. The production types do not exist yet, so a test cannot compile before its type exists. Author the tests in the same phase as the type they lock. Do not require a compiling red run before that type exists. Once the type compiles, the phase is not done until the tests in this list pass. Do not start a later phase before the earlier phase is green and committed.

**Organization**: Phases follow brief task ordering. Anvil implements Phase 1, commits, then implements Phase 2, and stops. Phase 3 is Cog, then Gauge. Do not start Phase 1 until the spec PR has merged and Gate 1 is established.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 feature value, US2 mapping.
- Exact file paths are in each description.

## Phase 1: User Story 1 - MovieFeatures value (Priority: P1)

**Goal**: A sealed nine-field feature value that normalizes sets, preserves scalars, and compares structurally.

**Independent Test**: Construct `MovieFeatures` with no `Movie`. Default and null collections, a runtime null element in each of the four sets, duplicate-null collapse, duplicate and case folding, source-list mutation, ordinal full plot, and equality and hash across all nine fields. For each set, when scalars and non-null members match, adding or removing a null member leaves the whole value hash unchanged, and equality still distinguishes that membership.

### Tests for User Story 1

- [ ] T001 [P] [US1] Write focused tests in `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesTests.cs` using existing xUnit v3 and Shouldly. Name every test method `Method_Condition_Expected`. Cover: null and default construction yield four empty sets and five null scalars; empty and default sets use `StringComparer.OrdinalIgnoreCase` membership; construction collapses case-equivalent duplicates regardless of order and keeps blank text without trimming or case rewriting, without asserting which spelling survives; each of Genres, Directors, Writers, and Cast preserves a runtime null element on construction, duplicate nulls collapse to one member, and null elements are not filtered or rejected, while parameters stay `IEnumerable<string>?` and properties stay `ImmutableHashSet<string>`; mutating the input sequences after construction does not change the sets; independently constructed equivalents, including reordered and case-varied sets, compare equal and share a hash code; for each of Genres, Directors, Writers, and Cast, when scalars and non-null members match, adding a null member and removing a null member leaves the whole value `GetHashCode()` unchanged, and equality still distinguishes a set that contains that null member from the same set without it; do not pin unchecked sum versus XOR; changing each of the nine fields independently makes the values unequal; null and a different type compare unequal; do not require unequal values to have distinct hashes; `FullPlot` null, empty, whitespace, and ordinal case differences are preserved, and a case difference in `FullPlot` is unequal. AAA comments only. Do not add a test for boxed `default(ImmutableArray<string>)` input.

- [ ] T002 [P] [US1] Write focused FsCheck properties in `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesPropertyTests.cs` for constructed records: permutation, case, and duplicate invariance of the four sets, preservation of runtime null elements in each set, duplicate-null collapse, and equality/hash coherence when a null member is present (equal values share a hash code; the converse is not required). Include null elements rather than filtering them. The whole-value null-member hash rule is the T001 and T003 contract; do not restate the hash combiner here. Name every test method `Method_Condition_Expected`. Annotate each property with FsCheck.Xunit `[Property(MaxTest = 100)]` and `[Trait("Category", "Property")]`, and set an explicit deterministic FsCheck Replay seed, so `./scripts/run-property-tests.ps1` discovers them the way `tests/LamuFlix.UnitTests/Domain/MovieMetadataCollectionsPropertyTests.cs` and `MovieRehydratePropertyTests.cs` are discovered. No new library, no `System.Random`, and no assertion that restates the production algorithm. This task creates `MovieFeaturesPropertyTests.cs`. T005 extends this same file later. Do not add another property-test file.

### Implementation for User Story 1

- [ ] T003 [US1] After T001 and T002 are written, add `src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs`. Sealed record in `LamuFlix.Core.Features.Recommendations`. One public constructor with parameters `genres`, `directors`, `writers`, `cast`, `releaseYear`, `runtime`, `imdbRating`, `metaScore`, `fullPlot`, all defaulting to null. Collection parameters are `IEnumerable<string>?`. Scalars are `ReleaseYear?`, `Runtime?`, `ImdbRating?`, `int?`, and `string?`. Get-only properties only: `Genres`, `Directors`, `Writers`, `Cast` as `ImmutableHashSet<string>`; `ReleaseYear`, `Runtime`, `ImdbRating`, `MetaScore`, `FullPlot`. Null collections become empty sets via `StringComparer.OrdinalIgnoreCase`. Non-null collections use `ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)`. A runtime null element in any of the four collections is preserved and is not filtered or rejected. Duplicate nulls collapse to one member. Keep declared types `IEnumerable<string>?` and `ImmutableHashSet<string>`; do not change the element type to `string?`. No trim, case rewrite, blank filter, init setter, or second factory. Implement `Equals(MovieFeatures?)` and `GetHashCode()`: sets via null-safe `SetEquals`; each set hash is a commutative unchecked sum or XOR of `StringComparer.OrdinalIgnoreCase.GetHashCode(element)`, using 0 for a null element; do not fold set enumeration into an order-sensitive hash combiner; combine the four completed set aggregates with the scalar hashes; scalars via existing value equality; `FullPlot` equality is ordinal, and its hash is 0 when `FullPlot` is null and `StringComparer.Ordinal.GetHashCode(FullPlot)` otherwise. Do not add an `IsDefault` branch. A deliberately boxed `default(ImmutableArray<string>)` is unsupported input: do not promise normalization or a particular exception, and do not add a test for it. No comments.

**Checkpoint**: US1 tests pass. Default construction is a valid empty feature value. Source mutation cannot change it. Equivalent values compare equal.

---

## Phase 2: User Story 2 - Map a persisted movie (Priority: P2)

**Goal**: `MovieFeaturesMapper.From(Movie)` projects persisted metadata into the Phase 1 value and leaves the movie unchanged.

**Independent Test**: Map a movie with all nine facts, a movie with null metadata that still has a title and parsed release year, a null movie, each partial collection and scalar, a runtime null element in each mapped collection, duplicate-null collapse, null-member equality and hash coherence, and director/actor lists mutated after mapping.

### Tests for User Story 2

- [ ] T004 [P] [US2] Write focused tests in `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesMapperTests.cs` using existing xUnit v3 and Shouldly. Name every test method `Method_Condition_Expected`. Build movies through existing `Movie.Rehydrate` (`src/LamuFlix.Core/Domain/Movie.cs`) and existing `MovieMetadata` construction. Do not edit those types. Cover: full metadata maps all nine fields with distinct values, keeps every director, maps `Actors` to `Cast` and `Synopsis` to `FullPlot`, and preserves the domain scalars; persisted MetaScore 0, 100, and null map to `int?` 0, 100, and null; null `Movie.Metadata` yields four empty `OrdinalIgnoreCase` sets and five null scalars even when the movie has a title and `ParsedReleaseYear`, with no fallback to `Movie.Title`, `Movie.Path`, or `Movie.ParsedReleaseYear`; null movie throws `ArgumentNullException` and the test does not pin the exception message; partial metadata and empty collections preserve each collection and each scalar on its own; mapping collapses case-equivalent director and actor duplicates regardless of order without trimming or dropping blanks; each of Genres, Directors, Writers, and Actors mapped to Cast preserves a runtime null element, duplicate nulls collapse to one member, and null elements are not filtered or rejected; mapped values that contain a null member compare equal and share a hash code when otherwise equivalent; for each mapped set, when scalars and non-null members match, adding or removing a null member leaves the whole value `GetHashCode()` unchanged, and equality still distinguishes that membership; do not pin unchecked sum versus XOR; mutating source `Directors` and `Actors` after `From` does not change the result; `FullPlot` null, empty, whitespace, and ordinal case differences survive mapping. AAA comments only. Do not add a test for boxed `default(ImmutableArray<string>)` input.

- [ ] T005 [P] [US2] Extend the same `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesPropertyTests.cs` that T002 created, after T002, with FsCheck properties through `MovieFeaturesMapper.From`: permutation, case, and duplicate invariance of mapped sets, preservation of runtime null elements in each mapped set, duplicate-null collapse, and equality/hash coherence of mapped values that contain a null member (equal values share a hash code). Include null elements rather than filtering them. The whole-value null-member hash rule is the T004 contract; do not restate the hash combiner here. Name every test method `Method_Condition_Expected`. Same FsCheck.Xunit `[Property(MaxTest = 100)]`, `[Trait("Category", "Property")]`, explicit deterministic Replay seed, library limits, and no `System.Random` as T002. Keep T002's construction properties in this file. Do not add another property-test file.

### Implementation for User Story 2

- [ ] T006 [US2] After T004 and T005 are written, add `src/LamuFlix.Core/Features/Recommendations/MovieFeaturesMapper.cs` as a public static class. The sole public mapping member is `public static MovieFeatures From(Movie movie)`. No instance state or construction. Null movie throws `ArgumentNullException(nameof(movie))`. Do not pin the exception message text. Null metadata returns four empty `OrdinalIgnoreCase` sets and five null scalars and does not throw. Otherwise map `Genres`, all `Directors`, `Writers`, `Actors` to `Cast`, `ReleaseYear`, `Runtime`, `ImdbRating`, `MetaScore` so persisted 0, 100, and null become `int?` 0, 100, and null, and `Synopsis` to `FullPlot`. Do not read `Movie.Title`, `Movie.Path`, or `Movie.ParsedReleaseYear` as substitutes. Do not add an `IsDefault` branch for a boxed `default(ImmutableArray<string>)`. Do not mutate `Movie`, `MovieMetadata`, or their collections. No interface, DI registration, extension method, extra overload, or factory. No comments.

**Checkpoint**: US1 and US2 tests pass. Mapping a movie with metadata and a movie without metadata matches the brief. The movie is unchanged.

---

## Phase 3: Polish

**Purpose**: Cog refactors the completed Phase 1 and Phase 2 diff. Gauge then runs the gates. Not an Anvil phase.

**Checkpoint**: Refactor complexity is at or under the harness refactor limit. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass.

- [ ] T007 Refactor only the five files named in `specs/DEV-336/plan.md` until the refactor pass of `./scripts/run-cyclomatic-complexity.ps1` meets the limit in `harness.yml`. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not lower a threshold. Do not add a type, dependency, or file.

- [ ] T008 After T007, run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1` with no `-All`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and `./scripts/run-mutation.ps1` with no `--since`. Thresholds, exclusions, kill-share, and file-span rules come from `harness.yml`. Do not edit generated configuration. An unchanged `/web` tree is scope-empty `SKIPPED (scope-empty)`, never PASS. Exit 0 is PASS only when that gate ran. Exit 1 or Could not run blocks. Property-test exit 2 blocks: this task has no property-test opt-out. Mutation that the script reports as all-excluded is N/A, not PASS.

---

## Excluded (never scheduled)

HTTP, infrastructure, persistence, OMDb persistence (DEV-411), recommendation ranking, scoring, embedding, UI, a new dependency, a new project or layer, a database schema change, `Features:LocalPlay`, secrets, `Process.Start`, edits to `Movie`, `MovieMetadata`, options, Infrastructure, Api, web, packages, or configuration, DI registration, an interface, a service wrapper, an extension method, an extra overload, a factory, `CONTEXT.md`, and ADRs. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No earlier phase. Blocks Phase 2. Author T001 and T002 before T003. Commit Phase 1 before Phase 2 starts.
- **Phase 2**: Depends on Phase 1. Author T004 and T005 before T006. Commit Phase 2 before Phase 3 starts.
- **Phase 3**: Depends on Phases 1 and 2 being committed. Cog owns T007. Gauge owns T008.

### User Story Dependencies

- **US1 (P1)**: Phase 1. No dependency on US2.
- **US2 (P2)**: Phase 2. Uses the US1 type. Its mapping tests are the independent proof. It does not change US1 behavior.

### Within Each User Story

- Author the tests before the production file in that phase.
- A compiling red run is required only after the new type exists. The phase commit waits until those tests pass.
- Set normalization and equality land with the record, before the mapper.
- The mapper calls the record constructor. It does not reimplement set equality.

### Parallel Opportunities

- T001 and T002 are different files and can be written together.
- T004 and T005 are different files and can be written together after Phase 1.
- T005 edits `MovieFeaturesPropertyTests.cs` after T002 has created it. Do not run T002 and T005 at the same time.
- T003 and T006 are sequential production files. T006 does not start until Phase 1 is committed.
- T007 and T008 are sequential. T008 does not start until the refactor commit exists.

## Parallel Example: User Story 1

```text
Task: "T001 Write MovieFeaturesTests in tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesTests.cs"
Task: "T002 Write construction properties in tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesPropertyTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 and confirm construction, normalization, and equality.
2. Stop there only to validate US1.
3. The frozen ticket scope still requires US2 before review.

### Incremental Delivery

US2 is the mapping the ticket names. It is part of the same frozen scope, not a follow-up. Phase 3 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. The only safe splits are T001 with T002, and later T004 with T005. Do not split two writers onto `MovieFeaturesPropertyTests.cs`.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Commit after each implementation phase, not after every task, using `DEV-336 - {phase subject}`.
- Gate 1 comes from the merged spec PR. This task list does not authorize implementation before that.
- Patron Q6 and Q7 in `brief.md` (Phase 2 drafting clarifications) are settled and written into `spec.md`, `plan.md`, and this list. Do not reopen them inside a phase.
