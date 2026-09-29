# Feature Specification: Core Ports for Persistence, Catalog, Metadata, Messaging, File System, and Playback

**Feature Branch**: `feature/298-spec`

**Created**: 2026-09-29

**Status**: Draft

**Input**: DEV-298 (parent DEV-282, size M, no ui tag); `specs/DEV-298/brief.md` (AC1-AC8, Q1-Q12 frozen); `specs/DEV-298/CONCLUSIONS.md`; recon-DEV-298

## User Scenarios & Testing

### User Story 1 - Guarded Paged Result Contract (Priority: P1)

As a handler developer consuming `IMovieCatalog.BrowseAsync`, I need a `PagedResult<T>` that cannot be constructed in an impossible state (uninitialised items, negative total, or a total smaller than the items on the page) and whose guards a `with` expression cannot bypass, so every consumer and adapter can trust its shape.

**Why this priority**: It is the only type in this ticket with behaviour (brief, Plan decisions). The catalog port signature depends on it, and it is the only place a real invariant, and so a real mutant, exists (Q10).

**Independent Test**: Construct `PagedResult<T>` with valid and invalid arguments in `tests/LamuFlix.UnitTests` and assert the accepted and rejected cases.

**Acceptance Scenarios**:

1. **Given** a non-default `ImmutableArray<T>` and `TotalCount >= Items.Length`, **When** a `PagedResult<T>` is constructed, **Then** it is created with those values.
2. **Given** `TotalCount == Items.Length`, **When** it is constructed, **Then** it is accepted (boundary).
3. **Given** `Items.IsDefault`, **When** it is constructed, **Then** `ArgumentException` is thrown.
4. **Given** non-default `Items` (for example `ImmutableArray<T>.Empty`) and `TotalCount < 0`, **When** it is constructed, **Then** `ArgumentOutOfRangeException` is thrown.
5. **Given** `TotalCount < Items.Length`, **When** it is constructed, **Then** `ArgumentOutOfRangeException` is thrown.

---

### User Story 2 - Port Contract Types (Priority: P1)

As an adapter and handler author, I need the Core contract types that the port signatures mention (`MovieSummary`, `MovieDetails`, `MetadataLookup`, `MetadataLookupResult`, `EnrichmentRequested`, `ScannedMovie`) with exactly the agreed field sets, so ports compile and adapters can be written against a stable shape.

**Why this priority**: The port interfaces cannot compile without them (Q1, Q2).

**Independent Test**: Build `LamuFlix.Core`; every type exists in `LamuFlix.Core.Ports` as a `public sealed record` with the AC4 field set (`MetadataLookupResult` excepted). `MetadataLookupResult` is a `public abstract record` with a private constructor and the sealed nested cases `Found`, `NotFound` and `Failed`.

**Acceptance Scenarios**:

1. **Given** the Core assembly, **When** the contract types are inspected, **Then** each has exactly the fields in AC4 and is a `public sealed record` (nested union cases included; `MetadataLookupResult` is a `public abstract record`).
2. **Given** `MetadataLookupResult`, **When** a consumer pattern-matches, **Then** exactly the cases `Found(MovieMetadata)`, `NotFound` and `Failed(EnrichmentFailureCategory)` exist. The private constructor blocks external derivation through construction; derivation through the synthesized protected copy constructor is a known C# record limitation, out of scope.

---

### User Story 3 - Six Core Port Interfaces (Priority: P1)

As the Core author, I need `IMovieRepository`, `IMovieCatalog`, `IMetadataProvider`, `IEnrichmentQueue`, `IMediaLibraryScanner` and `IMediaPlayerLauncher` declared in `LamuFlix.Core.Ports` with the signatures written in the ticket, so every external dependency (EF, RabbitMQ, OMDb, file system, process) sits behind an interface in Core (AC1).

**Why this priority**: This is the ticket's deliverable.

**Independent Test**: Build `LamuFlix.Core`. Each interface is public with exactly the ticket signatures (AC3), and the architecture whitelist (User Story 4) accepts all six names.

**Acceptance Scenarios**:

1. **Given** `IMovieRepository`, **Then** it declares `GetAsync`, `AddAsync`, `SaveChangesAsync` and `TryClaimForEnrichmentAsync` with the ticket signatures.
2. **Given** `IMovieCatalog`, **Then** it declares `BrowseAsync(MovieQuery, CancellationToken)` and `GetDetailsAsync(MovieId, CancellationToken)`, and `MovieQuery` stays in `LamuFlix.Core.Library`.
3. **Given** `IMediaLibraryScanner.Scan` and `IMediaPlayerLauncher.Launch`, **Then** both are synchronous with no `CancellationToken` (Q6).
4. **Given** the XML doc on `TryClaimForEnrichmentAsync`, **Then** it states that the call is an atomic claim attempt. It returns `true` only when the movie exists and this call newly claims it, and `false` when the movie is absent or already claimed. It says nothing about storage mechanics (AC6).

---

### User Story 4 - Architecture Tests Enforce the Port Boundary (Priority: P2)

As a maintainer, I need `LamuFlix.ArchitectureTests` to enforce that only constitution-listed ports live in `LamuFlix.Core.Ports` and that Core references neither HTTP client types nor process APIs, so AC2 stays true after this ticket.

**Why this priority**: It turns AC2 from a one-off observation into a guard, but it depends on the ports existing.

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~ArchitectureTests"`. The three new rules pass and the 10 existing rules are unchanged.

**Acceptance Scenarios**:

1. **Given** every interface in `LamuFlix.Core.Ports`, **When** the whitelist rule runs, **Then** each name is one of the eight constitution names (the six above plus `IRecommendationCandidateSource` and `IPlaybackHistory`).
2. **Given** Core, **When** the ban rule runs, **Then** Core has no dependency on `HttpClient`, `HttpMessageInvoker`, `HttpMessageHandler` or their subclasses. `HttpRequestException` and the `System.Net.Http` namespace itself stay permitted.
3. **Given** Core, **When** the ban rule runs, **Then** Core has no dependency on `System.Diagnostics.Process` or `System.Diagnostics.ProcessStartInfo`.

### Edge Cases

- A new interface with a name outside the whitelist is added to `Core.Ports`: the whitelist rule fails.
- An existing Core type already depends on `System.Diagnostics.Process` or `ProcessStartInfo`: stop and report `blocked:` naming the offending type. Do not weaken the rule or edit that code (brief, Baseline-red rule).
- `PagedResult<T>` is created with `default(ImmutableArray<T>)` and `TotalCount = 0`: rejected, because a default array is not a valid empty page.
- The legacy `Worker/Services/IMetadataProvider.cs` shares a simple name with the new Core port: allowed, since they live in different namespaces and the legacy one is untouched.

## Requirements

### Functional Requirements

- **FR-001**: Core MUST declare six public interfaces in `src/LamuFlix.Core/Ports/` (namespace `LamuFlix.Core.Ports`), one per file, with the exact signatures in brief AC3 (AC1, AC3; Q1, Q3).
- **FR-002**: Core MUST declare the contract types in brief AC4 in `src/LamuFlix.Core/Ports/`, one type per file, each as a `public sealed record` with exactly the listed fields, except `PagedResult<T>` (FR-004) and `MetadataLookupResult` (FR-003) (AC4; Q2, Q3).
- **FR-003**: `MetadataLookupResult` MUST be a `public abstract record` with a private constructor and the sealed nested records `Found(MovieMetadata Metadata)`, `NotFound` and `Failed(EnrichmentFailureCategory Category)`. It MUST NOT expose a `Match` method and MUST NOT use a package or SmartEnum (Q4, Plan decisions).
- **FR-004**: `PagedResult<T>` MUST be a `public sealed record` with an explicit constructor `(ImmutableArray<T> items, int totalCount)` and get-only `Items` and `TotalCount`, not a positional record, so a `with` expression cannot bypass the guards. It MUST throw `ArgumentException` when `Items.IsDefault`, and `ArgumentOutOfRangeException` when `TotalCount < Items.Length`, implemented as a single comparison that also rejects negative totals. `Items.IsDefault` is checked first (Plan decisions).
- **FR-005**: The XML doc on `IMovieRepository.TryClaimForEnrichmentAsync` MUST state the atomic-claim contract in AC6 and MUST NOT mention storage mechanics (AC6; Q11).
- **FR-006**: `IMediaLibraryScanner.Scan` and `IMediaPlayerLauncher.Launch` MUST be synchronous with no `CancellationToken`. No port method takes a `TimeProvider` (Q5, Q6).
- **FR-007**: The interfaces MUST have no default interface members. `MovieQuery` MUST stay in `LamuFlix.Core.Library` and be referenced from `IMovieCatalog` (Q3, Plan decisions).
- **FR-008**: `LamuFlix.ArchitectureTests` MUST gain a port-whitelist rule covering the eight constitution names, and two Core dependency bans, using `HaveDependencyOnAny`. The HTTP ban covers the client types `HttpClient`, `HttpMessageInvoker`, `HttpMessageHandler` and their subclasses, never the `System.Net.Http` namespace. The set is reflected at test time from the exported types in `typeof(HttpMessageInvoker).Assembly` assignable to `HttpMessageInvoker` or `HttpMessageHandler`, and the same fact asserts it is non-empty and contains `System.Net.Http.HttpClient`. The Process ban is `HaveDependencyOnAny("System.Diagnostics.Process", "System.Diagnostics.ProcessStartInfo")`, never the `System.Diagnostics` namespace. The whitelist fact also asserts at least 6 interfaces exist in `LamuFlix.Core.Ports`. Existing rules MUST NOT change; both bans are additions-only, with no fixture and no package (AC5; Q8).
- **FR-009**: `PagedResult<T>` MUST have unit tests at `tests/LamuFlix.UnitTests/Library/PagedResultTests.cs`, following the existing Core test folder convention (AC4, Q10).
- **FR-010**: The change MUST add zero packages and make zero edits to `.csproj`, `Directory.*.props` or `BannedSymbols.txt`. It MUST NOT touch adapters, DI registration, the legacy Worker/Web types, `DisabledMediaPlayerLauncher`, `Features:LocalPlay` gating, or `Process.Start` (AC7; Q1, Q7, Q9).
- **FR-011**: The 276 baseline tests MUST still pass alongside the new tests, and all gates in brief "Gate expectations" MUST exit 0 (AC8).

### Key Entities

- **Port**: a public Core interface hiding one external dependency. The six are `IMovieRepository` (persistence), `IMovieCatalog` (read model), `IMetadataProvider` (OMDb), `IEnrichmentQueue` (messaging), `IMediaLibraryScanner` (file system) and `IMediaPlayerLauncher` (process).
- **PagedResult<T>**: one page of items plus the total count across all pages.
- **MovieSummary**: a movie's identity and title, for browse lists.
- **MovieDetails**: a movie's identity, title, path, format and optional metadata.
- **MetadataLookup**: a title and optional release year to search by.
- **MetadataLookupResult**: the closed outcome of a lookup: found, not found, or failed with a category.
- **EnrichmentRequested**: a message asking for a movie to be enriched, with the attempt number.
- **ScannedMovie**: the path, title and format found by scanning a library folder.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All six ports exist with signatures identical to the ticket (0 deviations; AC3).
- **SC-002**: `LamuFlix.Core` has 0 references to concrete infrastructure libraries, enforced by the existing bans plus the new HTTP client-type and `Process`/`ProcessStartInfo` bans (AC2, AC5).
- **SC-003**: The whitelist rule accepts all eight constitution names, so DEV-340, DEV-348 and DEV-351 need 0 architecture-test edits.
- **SC-004**: `PagedResult<T>` rejects all 3 invalid classes (default array, negative total, total below the item count) and accepts the boundary `TotalCount == Items.Length`. Its explicit constructor and get-only properties mean a `with` expression cannot bypass the guards.
- **SC-005**: `git diff --stat origin/main...HEAD` shows 0 files outside the frozen scope and 0 changes to `.csproj`, `Directory.*.props` or `BannedSymbols.txt` (AC7).
- **SC-006**: `dotnet test` passes 276 baseline tests plus the new ones, and every gate exits 0. Stryker reports N/A if it finds zero mutants, never PASS (AC8, Q10).

## Assumptions

- Tests here are behavioural for `PagedResult<T>` and architectural for the ports. There are no mirror tests on the interfaces (Q10).
- `IMediaLibraryScanner.Scan` keeps the ticket's name, which takes precedence over the constitution VIII preference for "Import" (Plan decisions).
- `System.IO` is not banned wholesale. `LibraryPath` may use path APIs (brief, Out of scope).
- `.gitkeep` in `src/LamuFlix.Core/Ports/` may be deleted once the folder holds files (recon:76).
- The ports are declared but not implemented or registered here. Adapters and the `Features:LocalPlay`-gated launcher come with later tickets (Q1, Q7).
- No ADR is needed, because all six ports are on the constitution list (Q12).
- Dependency DEV-297 (`MovieQuery`) is satisfied by PR #51 (recon:16).
