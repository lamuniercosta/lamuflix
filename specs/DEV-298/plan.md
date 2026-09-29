# Implementation Plan: Core Ports for Persistence, Catalog, Metadata, Messaging, File System, and Playback

**Branch**: `feature/298-spec` | **Date**: 2026-09-29 | **Spec**: specs/DEV-298/spec.md

**Input**: Feature specification from `specs/DEV-298/spec.md`; `specs/DEV-298/brief.md`, `CONCLUSIONS.md`; recon-DEV-298 (lines 1-120 intake, 123-189 grill follow-up R1)

## Summary

Declaration-only work in `LamuFlix.Core`: six port interfaces and the seven contract types their signatures need, all in `src/LamuFlix.Core/Ports/` (namespace `LamuFlix.Core.Ports`). The only behaviour is the `PagedResult<T>` guard clauses. `LamuFlix.ArchitectureTests` gains a port-whitelist rule and two Core dependency bans (HTTP client types, `Process`/`ProcessStartInfo`). No adapters, DI, packages or project changes (brief Q1, AC7).

## Technical Context

**Language/Version**: C# 14 / .NET 10

**Primary Dependencies**: none new. Core already references `Microsoft.Extensions.Logging.Abstractions`, `Ardalis.SmartEnum` and `System.Collections.Immutable` (recon:78, 81). This work uses `System.Collections.Immutable` only.

**Storage**: N/A. No schema or migration change (recon:83).

**Testing**: xUnit v3, Shouldly, AAA, matching `tests/LamuFlix.UnitTests` (e.g. `Library/EnumerationTests.cs`); NetArchTest-style rules in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` reusing `AssertNoDependency`.

**Target Platform**: .NET 10 backend on Windows/Linux

**Project Type**: Core library (`LamuFlix.Core`) plus two existing test projects

**Performance Goals**: N/A

**Constraints**:
- Zero new packages; zero edits to `.csproj`, `Directory.*.props`, `BannedSymbols.txt` (AC7)
- No `TimeProvider` on any port (Q5); `Scan` and `Launch` stay sync with no CT (Q6)
- Legacy `Worker/Services/IMetadataProvider.cs` and other legacy types stay untouched (brief, Out of scope)
- `Features:LocalPlay` gating and `Process.Start` untouched. Only the launcher port is declared (Q7)

**Scale/Scope**: 6 interfaces, 7 contract types (one is `PagedResult<T>`), 1 unit-test file, 3 new architecture rules.

## Constitution Check

**Gate 1: §2.3 Care List**
- ✓ **Dependencies**: none added (recon:81).
- ✓ **Architecture**: `src/LamuFlix.Core/Ports/` is named in the ticket's Scope item 1 and constitution I (recon:82). No new project, folder or layer.
- ✓ **Database Schema**: no change (recon:83).
- ✓ **API Shape**: no HTTP route or DTO change; internal Core ports only (recon:84).
- ✓ **Security / LocalPlay / Process.Start**: the port is declared only. No `Process.Start` is added, and gating is deferred to the adapter work (Q7; recon:85).
- ✓ **File Scope**: NEW `src/LamuFlix.Core/Ports/{IMovieRepository,IMovieCatalog,IMetadataProvider,IEnrichmentQueue,IMediaLibraryScanner,IMediaPlayerLauncher,PagedResult,MovieSummary,MovieDetails,MetadataLookup,MetadataLookupResult,EnrichmentRequested,ScannedMovie}.cs`; NEW unit-test file `tests/LamuFlix.UnitTests/Library/PagedResultTests.cs`; EDITED `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` (additions only); DELETED `src/LamuFlix.Core/Ports/.gitkeep` (recon:76); NEW `specs/DEV-298/*`. Nothing else changes.

**Gate 2: Core Isolation**
- ✓ Ports and contracts reference only Core types (`Domain`, `Library`) and BCL. `Ports → Library` is allowed, because the feature-isolation rule scopes only `LamuFlix.Core.Features` (recon:164-166).
- ✓ The new bans (HTTP client types, `Process`/`ProcessStartInfo`) implement the constitution I wording.

**Gate 3: Time**
- ✓ No port method is time-sensitive (Q5). `BannedSymbols.txt` already bans wall-clock APIs in Core (recon:168-177).

**Naming note**: the ticket and constitution name `Scan` directly, which takes precedence over the constitution VIII preference for "Import" for this one port name (brief, Plan decisions).

**Status**: ✓ No departures. No ADR (Q12).

## Design

### Contract types (`Core/Ports/`, one type per file, `public sealed record`, except `MetadataLookupResult`, a `public abstract record`)

| Type | Shape (brief AC4) |
|------|-------------------|
| `PagedResult<T>` | explicit constructor `(ImmutableArray<T> items, int totalCount)` with get-only `Items` and `TotalCount`; not positional, so `with` cannot bypass the guards |
| `MovieSummary` | `(MovieId Id, string Title)` |
| `MovieDetails` | `(MovieId Id, string Title, LibraryPath Path, MediaFormat Format, MovieMetadata? Metadata)` |
| `MetadataLookup` | `(string Title, ReleaseYear? ReleaseYear)` |
| `ScannedMovie` | `(LibraryPath Path, string Title, MediaFormat Format)` |
| `EnrichmentRequested` | `(MovieId MovieId, int Attempt)` |
| `MetadataLookupResult` | `public abstract record` with a private constructor (blocks external derivation through construction; derivation through the synthesized protected copy constructor is a known C# record limitation, out of scope); sealed nested `Found(MovieMetadata Metadata)`, `NotFound`, `Failed(EnrichmentFailureCategory Category)`. No `Match`; consumers pattern-match |

`PagedResult<T>` guards, in the explicit constructor, in this order: `Items.IsDefault` throws `ArgumentException` (checked first, because `Items.Length` on a default `ImmutableArray` throws); then a single comparison `TotalCount < Items.Length` throws `ArgumentOutOfRangeException`, which also rejects negatives. No separate `TotalCount < 0` clause: it would be subsumed, and deleting it would leave an equivalent surviving mutant. Because `Items` and `TotalCount` are get-only and the constructor is explicit, a `with` expression cannot produce an unguarded instance.

### Ports (`Core/Ports/`, `public interface`, no default members)

| Port | Members |
|------|---------|
| `IMovieRepository` | `Task<Movie?> GetAsync(MovieId id, CancellationToken ct)`; `Task AddAsync(Movie movie, CancellationToken ct)`; `Task SaveChangesAsync(CancellationToken ct)`; `Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)` with the AC6 XML doc |
| `IMovieCatalog` | `Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)`; `Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)` |
| `IMetadataProvider` | `Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)` |
| `IEnrichmentQueue` | `Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct)` |
| `IMediaLibraryScanner` | `ScannedMovie Scan(LibraryPath folder)` |
| `IMediaPlayerLauncher` | `void Launch(LibraryPath file, MediaFormat format)` |

Referenced types exist in Core: `Movie`, `MovieId`, `LibraryPath`, `MediaFormat`, `MovieMetadata`, `EnrichmentFailureCategory`, `ReleaseYear` under `Domain/`, and `MovieQuery` under `Library/` (recon:20-58, 125-156).

### Architecture tests (`ArchitectureTests.cs`, additions only)

1. Whitelist fact: every interface in `LamuFlix.Core.Ports` has a name in `{IMovieRepository, IMovieCatalog, IMetadataProvider, IEnrichmentQueue, IMediaLibraryScanner, IMediaPlayerLauncher, IRecommendationCandidateSource, IPlaybackHistory}`, and at least 6 interfaces exist there, to avoid a vacuous pass. The whitelist holds all eight so DEV-340, DEV-348 and DEV-351 need no test edit (Keel plan decision, Q8).
2. Ban fact: Core must not depend on the HTTP client types. At test time, reflect the exported types in `typeof(HttpMessageInvoker).Assembly` assignable to `HttpMessageInvoker` or `HttpMessageHandler`, and pass their full names to `HaveDependencyOnAny` over Core. The same fact asserts the set is non-empty and contains `System.Net.Http.HttpClient`. The `System.Net.Http` namespace is not banned; `HttpRequestException` stays permitted.
3. Ban fact: Core must not depend on `System.Diagnostics.Process` or `System.Diagnostics.ProcessStartInfo`, via `HaveDependencyOnAny("System.Diagnostics.Process", "System.Diagnostics.ProcessStartInfo")`. A single "Process" string would miss `ProcessStartInfo`; the `System.Diagnostics` namespace is not banned.

Both bans are additions-only in `ArchitectureTests.cs`, with no fixture and no package. Both gates are expected at exit 0.

Existing rules, including the bans at lines 28-60 (recon:160-163), stay unchanged. `System.IO` is not banned wholesale.

**Baseline-red rule**: if a new ban fails because of existing Core code, stop and report `blocked:` with the offending type. Do not weaken the rule or edit that code.

### Unit tests (`tests/LamuFlix.UnitTests/Library/PagedResultTests.cs`)

It goes in the existing `Library/` folder and adds no new folder or project. Five cases: valid construction, default array, negative total, total below the item count, and the boundary `TotalCount == Items.Length`. Nothing is written against the interfaces (Q10).

### Verification (brief, Gate expectations)

- Roslyn analyzers, cyclomatic complexity at 15 and then at 6, and InspectCode: each exit 0, in `-Files` or diff mode on the changed `.cs` files.
- `dotnet format --verify-no-changes` and the vulnerable-packages scan: exit 0.
- `dotnet test` green: 276 baseline plus new tests.
- Stryker on `LamuFlix.Core` only, using the D7a pattern. It should mutate only the `PagedResult<T>` guards. Zero mutants is reported as N/A, never PASS, and any survivor is a FAIL.
- Web gates are skipped by `harness.yml`, which is not a pass (recon:113-116).
- Scope proof: `git diff --stat origin/main...HEAD`.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-298/
├── brief.md          # Phase A brief (frozen)
├── CONCLUSIONS.md    # Patron rulings (frozen)
├── ASSUMPTIONS.md
├── spec.md           # /speckit-specify
├── plan.md           # This file (/speckit-plan)
└── tasks.md          # /speckit-tasks
```

No `research.md`, `data-model.md`, `contracts/` or `quickstart.md` is produced. There is nothing unresolved to research, the entities are in the Design section above, and no external interface is exposed. Ticket size M runs without `-clarify` or `-checklist`.

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/
├── IMovieRepository.cs
├── IMovieCatalog.cs
├── IMetadataProvider.cs
├── IEnrichmentQueue.cs
├── IMediaLibraryScanner.cs
├── IMediaPlayerLauncher.cs
├── PagedResult.cs
├── MovieSummary.cs
├── MovieDetails.cs
├── MetadataLookup.cs
├── MetadataLookupResult.cs
├── EnrichmentRequested.cs
└── ScannedMovie.cs            # .gitkeep removed

tests/LamuFlix.UnitTests/Library/PagedResultTests.cs      # new
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs   # additions only
```

**Structure Decision**: extend the existing `LamuFlix.Core/Ports/` folder and the two existing test projects. No new project, folder or layer.

## Complexity Tracking

No constitution violations. Nothing to justify.
