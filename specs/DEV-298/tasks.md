# Tasks: Core Ports for Persistence, Catalog, Metadata, Messaging, File System, and Playback

**Input**: Design documents from `specs/DEV-298/`; `brief.md` (Task ordering, Frozen scope, Gate expectations)

**Prerequisites**: plan.md, spec.md, brief.md (frozen decisions)

**Tests**: Included per spec FR-008 and FR-009 and brief Q10. `PagedResult<T>` guard tests are written first. There are no tests against the interfaces.

**Organization**: Tasks follow the brief's task ordering: PagedResult with tests, then the other contract types, then the ports, then the architecture tests, then the gates. User stories map as US1 = PagedResult, US2 = contract types, US3 = ports, US4 = architecture tests.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependencies)
- **[Story]**: US1 to US4
- Every new type is a `public sealed record` (except `MetadataLookupResult`, a `public abstract record`), one type per file. Every port is a `public interface` with no default members. All new source is in namespace `LamuFlix.Core.Ports`.

---

## Phase 1: Setup

**Purpose**: Confirm the baseline before any edit. No project, package or `.csproj` change is allowed (spec FR-010).

- [X] T001 Verify worktree `F:\Dev\LamuFlix.worktrees\feature-298-spec`, branch `feature/298-spec`, and record the baseline of 276 passing tests (recon:91-92). Confirm `src/LamuFlix.Core/Ports/` contains only `.gitkeep`. `.specify/feature.json` is modified in the worktree but is outside frozen scope (brief:38-45); it stays unstaged and uncommitted.

---

## Phase 2: User Story 1 - Guarded Paged Result Contract (Priority: P1) 🎯 MVP

**Goal**: `PagedResult<T>` cannot be constructed in an impossible state (spec FR-004).

**Independent Test**: `dotnet test --filter "FullyQualifiedName~PagedResultTests" --nologo -v q`

### Tests for User Story 1 (write first; they must fail before T003)

- [X] T002 [US1] Create `tests/LamuFlix.UnitTests/Library/PagedResultTests.cs` (xUnit, AAA, Shouldly; namespace and style as in `tests/LamuFlix.UnitTests/Library/EnumerationTests.cs`) with five cases: valid construction; `TotalCount == Items.Length` accepted (boundary); `default(ImmutableArray<T>)` (with `TotalCount` 0) throws `ArgumentException`; `TotalCount < 0` with non-default `Items` (for example `ImmutableArray<T>.Empty`, so it exercises the `TotalCount` guard and not the `IsDefault` guard) throws `ArgumentOutOfRangeException`; `TotalCount < Items.Length` throws `ArgumentOutOfRangeException` (spec US1 scenarios 1-5).

### Implementation for User Story 1

- [X] T003 [US1] Create `src/LamuFlix.Core/Ports/PagedResult.cs`: `public sealed record PagedResult<T>` with an explicit constructor `(ImmutableArray<T> items, int totalCount)` and get-only `Items` and `TotalCount` (not a positional record, so a `with` expression cannot bypass the guards). The constructor guards in this order: `Items.IsDefault` is checked first and throws `ArgumentException` (`Items.Length` on a default `ImmutableArray` throws); then ONE comparison, `TotalCount < Items.Length`, throws `ArgumentOutOfRangeException` (this also rejects negatives; a separate `TotalCount < 0` clause is subsumed and its deletion would be a surviving equivalent mutant) (spec FR-004; brief Plan decisions). Do not touch the `.csproj`.

**Checkpoint**: T002 tests pass against T003.

---

## Phase 3: User Story 2 - Port Contract Types (Priority: P1)

**Goal**: every type the port signatures mention exists with the AC4 field sets (spec FR-002, FR-003).

**Independent Test**: `dotnet build` of `LamuFlix.Core` succeeds and each type has exactly the AC4 fields.

- [X] T004 [P] [US2] Create `src/LamuFlix.Core/Ports/MovieSummary.cs`: `public sealed record MovieSummary(MovieId Id, string Title)`.
- [X] T005 [P] [US2] Create `src/LamuFlix.Core/Ports/MovieDetails.cs`: `public sealed record MovieDetails(MovieId Id, string Title, LibraryPath Path, MediaFormat Format, MovieMetadata? Metadata)`.
- [X] T006 [P] [US2] Create `src/LamuFlix.Core/Ports/MetadataLookup.cs`: `public sealed record MetadataLookup(string Title, ReleaseYear? ReleaseYear)`.
- [X] T007 [P] [US2] Create `src/LamuFlix.Core/Ports/EnrichmentRequested.cs`: `public sealed record EnrichmentRequested(MovieId MovieId, int Attempt)`.
- [X] T008 [P] [US2] Create `src/LamuFlix.Core/Ports/ScannedMovie.cs`: `public sealed record ScannedMovie(LibraryPath Path, string Title, MediaFormat Format)`.
- [X] T009 [P] [US2] Create `src/LamuFlix.Core/Ports/MetadataLookupResult.cs`: a `public abstract record` with a private constructor and the sealed nested records `Found(MovieMetadata Metadata)`, `NotFound` and `Failed(EnrichmentFailureCategory Category)`. No `Match` method, no package, no SmartEnum (spec FR-003; brief Q4).

**Checkpoint**: Core builds with all contract types.

---

## Phase 4: User Story 3 - Six Core Port Interfaces (Priority: P1)

**Goal**: the six ticket ports exist with the ticket signatures (spec FR-001, FR-005, FR-006, FR-007).

**Independent Test**: `dotnet build` of `LamuFlix.Core` succeeds. Signatures are checked against brief AC3.

- [X] T010 [P] [US3] Create `src/LamuFlix.Core/Ports/IMovieRepository.cs` with `Task<Movie?> GetAsync(MovieId id, CancellationToken ct)`, `Task AddAsync(Movie movie, CancellationToken ct)`, `Task SaveChangesAsync(CancellationToken ct)` and `Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)`. The XML doc on `TryClaimForEnrichmentAsync` states that it is an atomic claim attempt. It returns `true` only when the movie exists and this call newly claims it, and `false` when the movie is absent or already claimed. It says nothing about storage mechanics (AC6; brief Q11).
- [X] T011 [P] [US3] Create `src/LamuFlix.Core/Ports/IMovieCatalog.cs` with `Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)` and `Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)`. Reference `MovieQuery` from `LamuFlix.Core.Library` and do not move it (depends on T003, T004, T005).
- [X] T012 [P] [US3] Create `src/LamuFlix.Core/Ports/IMetadataProvider.cs` with `Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)` (depends on T006, T009). Leave the legacy `Worker/Services/IMetadataProvider.cs` untouched.
- [X] T013 [P] [US3] Create `src/LamuFlix.Core/Ports/IEnrichmentQueue.cs` with `Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct)` (depends on T007).
- [X] T014 [P] [US3] Create `src/LamuFlix.Core/Ports/IMediaLibraryScanner.cs` with `ScannedMovie Scan(LibraryPath folder)`. It is synchronous with no `CancellationToken` (brief Q6; depends on T008).
- [X] T015 [P] [US3] Create `src/LamuFlix.Core/Ports/IMediaPlayerLauncher.cs` with `void Launch(LibraryPath file, MediaFormat format)`. It is synchronous with no `CancellationToken`. No `Process.Start`, no `Features:LocalPlay` gating and no `DisabledMediaPlayerLauncher` (brief Q6, Q7).
- [X] T016 [US3] Delete `src/LamuFlix.Core/Ports/.gitkeep` now that the folder holds files (recon:76).

**Checkpoint**: Core builds. No port method takes a `TimeProvider` (brief Q5).

---

## Phase 5: User Story 4 - Architecture Tests Enforce the Port Boundary (Priority: P2)

**Goal**: the port whitelist and the HTTP and Process bans are enforced (spec FR-008).

**Independent Test**: `dotnet test --filter "FullyQualifiedName~ArchitectureTests" --nologo -v q` passes with the 10 existing rules unchanged.

- [X] T017 [US4] Edit `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` (additions only): add a whitelist fact asserting that at least 6 interfaces exist in `LamuFlix.Core.Ports` (to avoid a vacuous pass) and that every one is named one of `IMovieRepository`, `IMovieCatalog`, `IMetadataProvider`, `IEnrichmentQueue`, `IMediaLibraryScanner`, `IMediaPlayerLauncher`, `IRecommendationCandidateSource` or `IPlaybackHistory` (brief AC5, Q8; depends on T010-T015).
- [X] T018 [US4] In the same file, add a fact that `Core` must not depend on the HTTP client types: reflect the exported types in `typeof(HttpMessageInvoker).Assembly` assignable to `HttpMessageInvoker` or `HttpMessageHandler`, pass their full names to `HaveDependencyOnAny` over Core, and assert the set is non-empty and contains `System.Net.Http.HttpClient`. Do not ban the `System.Net.Http` namespace; `HttpRequestException` stays permitted. Additions-only, no fixture, no package (brief AC5, Plan challenge decisions).
- [X] T019 [US4] In the same file, add a fact that `Core` must not depend on `System.Diagnostics.Process` or `System.Diagnostics.ProcessStartInfo`, using `HaveDependencyOnAny("System.Diagnostics.Process", "System.Diagnostics.ProcessStartInfo")`, never the `System.Diagnostics` namespace (brief AC5). If T019 fails because of existing Core code, stop and report `blocked:` naming the offending type. Do not weaken the rule or edit that code (brief Baseline-red rule).

**Checkpoint**: 3 new architecture facts pass and the existing rules are unchanged.

---

## Phase 6: Polish & Gates

**Purpose**: prove the closing bar AC7 and AC8. Run these after the implementation works.

- [ ] T020 Run the Roslyn analyzers on the changed `.cs` files: `./scripts/run-roslyn-analyzers.ps1` (exit 0).
- [ ] T021 Run the cyclomatic complexity gate at 15, then the refactor gate: `./scripts/run-cyclomatic-complexity.ps1` and then `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (exit 0 each).
- [ ] T022 Run InspectCode: `./scripts/run-jetbrains-inspectcode.ps1` (exit 0).
- [ ] T023 Run `dotnet format --verify-no-changes` and `pwsh -NoProfile -File scripts/run-vulnerable-packages.ps1` (exit 0 each).
- [ ] T024 Run `dotnet test`: 276 baseline plus the new tests must all pass (AC8).
- [ ] T025 Run Stryker on `LamuFlix.Core` only, using the D7a pattern. Expect it to mutate only the `PagedResult<T>` guards. A zero-mutant result is reported as N/A, never PASS, and any survivor is a FAIL (brief Q10).
- [ ] T026 Prove the scope with `git diff --stat origin/main...HEAD`: only the frozen-scope files appear, and there are no changes to `.csproj`, `Directory.*.props` or `BannedSymbols.txt` (AC7). `.specify/feature.json` stays unstaged and uncommitted so the diff-stat shows only frozen-scope files. Web gates are skipped by `harness.yml`; record them as SKIPPED, not PASS.

---

## Dependencies & Execution Order

- **Phase 1** first. **US1** (T002 then T003) comes next: tests first, and they must fail before T003.
- **US2** (T004-T009) needs nothing from US1 and could run alongside it, but the brief orders PagedResult first.
- **US3** (T010-T016) depends on US1 and US2: T011 needs T003-T005, T012 needs T006 and T009, T013 needs T007, and T014 needs T008.
- **US4** (T017-T019) depends on US3, because the whitelist rule needs the ports. T017-T019 edit one file, so they are sequential.
- **Phase 6** needs everything else.

### Parallel Opportunities

- T004-T009 touch different files and run in parallel.
- T010-T015 run in parallel once their type dependencies exist.

---

## Implementation Strategy

### MVP First

1. T001, then US1 (T002, T003). Confirm the guard tests pass.
2. US2, then US3. Core builds with the full port surface.
3. US4 architecture rules.
4. Phase 6 gates and scope proof.

### Notes

- `.specify/feature.json` is modified in the worktree but outside frozen scope: leave it unstaged and uncommitted.
- Do not edit adapters, DI, legacy Worker/Web types, `.csproj` files, `Directory.*.props` or `BannedSymbols.txt`.
- No ADR (brief Q12) and no follow-up ticket (brief Q9).
- Commit after each logical group. Commit messages start `DEV-298 - `.
