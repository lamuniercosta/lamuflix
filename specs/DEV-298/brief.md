# DEV-298 — Phase A Brief

Phase A grill outcome for DEV-298 (parent DEV-282, size M, no ui tag).
Patron decided Q1–Q12 in `specs/DEV-298/CONCLUSIONS.md` (commit d1525d3), citing sources for each. Facts come from `recon-DEV-298` (lines 1–120 intake; lines 123–189 are the grill follow-up recon, R1) and the DEV-298 ticket note (lines 3–16). Keel checked the cited constitution passages in `.specify/memory/constitution.md`: Principle I, the Core reference table and ports list (≈101–120), and Principle VII, LocalPlay and TimeProvider (≈241–258).
Grill questions: **12 asked (cap 12); 12/12 ruled**. Owner checkboxes: **0**. ADR: **none** (Q12).

## Closing bar

The ticket's two ACs, verbatim (Patron quoted them in CONCLUSIONS.md):

- **AC1 (ticket):** "All external dependencies (EF, RabbitMQ, OMDb, FileSystem, Process) abstracted behind interfaces in `LamuFlix.Core`."
- **AC2 (ticket):** "Core has zero references to concrete infrastructure libraries."

Keel's closing-bar lines, which come from Patron's rulings and do not change delivery:

- **AC3 (Q1, Q3):** six public interfaces exist in `src/LamuFlix.Core/Ports/` (namespace `LamuFlix.Core.Ports`), and their signatures match the ticket exactly:
  - `IMovieRepository`: `Task<Movie?> GetAsync(MovieId id, CancellationToken ct)`, `Task AddAsync(Movie movie, CancellationToken ct)`, `Task SaveChangesAsync(CancellationToken ct)`, and `Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)`.
  - `IMovieCatalog`: `Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)` and `Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)`.
  - `IMetadataProvider`: `Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)`.
  - `IEnrichmentQueue`: `Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct)`.
  - `IMediaLibraryScanner`: `ScannedMovie Scan(LibraryPath folder)`, which is sync with no CT (Q6).
  - `IMediaPlayerLauncher`: `void Launch(LibraryPath file, MediaFormat format)`, which is sync with no CT (Q6).
- **AC4 (Q2, Q4):** the contract types exist in `Core/Ports/` as `sealed record`s with exactly these field sets:
  - `PagedResult<T>(ImmutableArray<T> Items, int TotalCount)`. **Amended by PD2 (CONCLUSIONS.md:26):** a `sealed record` with an explicit constructor `(ImmutableArray<T> items, int totalCount)` and get-only `Items` and `TotalCount`, not a positional record, so a `with` expression cannot bypass the guards.
  - `MovieSummary(MovieId Id, string Title)`
  - `MovieDetails(MovieId Id, string Title, LibraryPath Path, MediaFormat Format, MovieMetadata? Metadata)`
  - `MetadataLookup(string Title, ReleaseYear? ReleaseYear)`
  - `ScannedMovie(LibraryPath Path, string Title, MediaFormat Format)`
  - `EnrichmentRequested(MovieId MovieId, int Attempt)`
  - `MetadataLookupResult`: an abstract record with a private constructor and the sealed nested records `Found(MovieMetadata Metadata)`, `NotFound`, and `Failed(EnrichmentFailureCategory Category)`.
- **AC5 (Q8):** `LamuFlix.ArchitectureTests` gains a port-whitelist rule: every interface in `LamuFlix.Core.Ports` has a name on the constitution's list (the six above plus `IRecommendationCandidateSource` and `IPlaybackHistory`, so DEV-340, DEV-348, and DEV-351 need no test edit). It also gains two Core bans that are not yet enforced, `System.Net.Http` and `System.Diagnostics.Process` (constitution I: "Core MUST NOT reference … `System.IO` process APIs, or any HTTP client"). All existing rules stay unchanged.
  - **Amended by PD1 (CONCLUSIONS.md:25) and plan-challenge F-R2:** the HTTP ban covers the client types (`HttpClient`, `HttpMessageInvoker`, `HttpMessageHandler` and their subclasses), not the whole `System.Net.Http` namespace. `HttpRequestException` stays permitted (EnrichmentFailureClassifier.cs; recon-DEV-298:218–246). The Process ban names the types `System.Diagnostics.Process` and `System.Diagnostics.ProcessStartInfo`, and never the `System.Diagnostics` namespace, which Core uses for CodeAnalysis. The matching mechanism must be one that recon proves actually matches a type name. It is recorded under *Plan challenge decisions* below.
- **AC6 (Q11):** the XML doc on `TryClaimForEnrichmentAsync` states that it is an atomic claim attempt. It returns `true` only when the movie exists and this call newly claims it, and `false` when the movie is absent or already claimed. It says nothing about storage mechanics.
- **AC7:** zero new packages and zero edits to `.csproj`, `Directory.*.props`, or `BannedSymbols.txt`. Zero files outside the frozen scope change, proven by `git diff --stat origin/main...HEAD`.
- **AC8:** gates (see *Gate expectations*) exit 0. The 276 baseline tests (recon:91) still pass, alongside the new ones.

## Frozen scope

Files in scope (all new except the architecture test file):

- `src/LamuFlix.Core/Ports/IMovieRepository.cs`, `IMovieCatalog.cs`, `IMetadataProvider.cs`, `IEnrichmentQueue.cs`, `IMediaLibraryScanner.cs`, and `IMediaPlayerLauncher.cs`. These are named in ticket Scope item 1.
- `src/LamuFlix.Core/Ports/PagedResult.cs`, `MovieSummary.cs`, `MovieDetails.cs`, `MetadataLookup.cs`, `MetadataLookupResult.cs`, `EnrichmentRequested.cs`, and `ScannedMovie.cs`. There is one type per file. Placement follows Q3.
- `src/LamuFlix.Core/Ports/.gitkeep`: it may be deleted once the folder holds files (recon:76).
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`: add the AC5 rules and edit nothing else.
- A new unit-test file under `tests/LamuFlix.UnitTests/` for the `PagedResult<T>` guards. Placement follows the existing Core test folder convention in that project, and there is no new project or folder.
  - **Keel clarification (analyze R1, P1):** `tests/LamuFlix.UnitTests/Ports/` does not exist, and the only source folders are `Domain` and `Library` (recon-DEV-298:191–216). So the file is `tests/LamuFlix.UnitTests/Library/PagedResultTests.cs`. Paging already lives in `Library` (`MovieQuery`, `Page`; recon:66–70), and this adds no folder.
- The `specs/DEV-298/*` artifacts.

Out of scope, per Q1, Q7, Q9, and care list #6:

- Adapters, DI registration, and any Infrastructure, Worker, Web, or Api code.
- The legacy `Worker/Services/IMetadataProvider.cs` (recon:40) keeps its name and namespace. It shares its name with the new Core port, but they live in different namespaces, and it gets no rename here.
- The legacy `Web/Services/IEnrichmentQueuePublisher.cs`, `MovieEnrichmentMessage`, `MovieService`, `GenericRepository<T>`, and `MoviesController` (`Process.Start` at :68).
- `DisabledMediaPlayerLauncher`, `FeatureDisabledException`, and `Features:LocalPlay` DI gating are deferred to the playback adapter work (Q7). DEV-298 only declares the port, so it touches no `Process.Start`.
- `IRecommendationCandidateSource` is already covered by DEV-340 and DEV-348, and `IPlaybackHistory` by DEV-351 (recon:179–189, all Todo). So no follow-up ticket is filed (Q9).
- There are no `TimeProvider` parameters on any port (Q5). There is no async or CT on `Scan`/`Launch` (Q6), because adding them would change the ticket text.
- `System.IO` is not banned wholesale. Core types such as `LibraryPath` may legitimately use path APIs, and the file-system abstraction is the `IMediaLibraryScanner` port itself.

## Round cap

- Grill: closed at 1 round (12/12).
- Spec/plan review (Keel ↔ Quill): at most 3 fix-list rounds, which is the three-blocked-report cap (task-pipeline §2.2).
- Code review: at most 2 rounds, with at most 2 fix commits per round.

## Grill answers

- **Q1 — Scope:** the six ports plus the contract types their signatures need. There are no adapters, no DI, and no legacy rewrites. Sources: ticket Overview and Scope 1; constitution I; care list #6.
- **Q2 — Field sets:** listed in AC4. The ticket itself fixes `EnrichmentRequested` and the three union cases. The rest are the smallest field sets that let each operation identify, display, locate, or search a movie. Recon confirmed every referenced type exists in Core: `ReleaseYear` (`Domain/ReleaseYear.cs:6`), and `Movie.Title`, `Path`, `Format`, and `Metadata` (`Movie.cs:21,23,25,29`).
- **Q3 — Placement:** the ports and their contract types go in `Core/Ports/`. `MovieQuery` stays in `Core/Library/`, and `IMovieCatalog` references it. Recon:164–166 confirms no rule forbids Ports → Library, because the feature-isolation rule (`ArchitectureTests.cs:81–109`) scopes only `LamuFlix.Core.Features`.
- **Q4 — Union form:** an abstract record with a private constructor and sealed nested records. It uses no package and no SmartEnum.
- **Q5 — Time:** no port method is time-sensitive. `TimeProvider` goes into future adapters and handlers through their constructors. `BannedSymbols.txt` already bans the wall-clock APIs across Core (recon:168–177).
- **Q6 — Sync:** `Scan` and `Launch` stay exactly as the ticket writes them.
- **Q7 — LocalPlay:** the port only. The disabled launcher and its gating are deferred.
- **Q8 — Enforcement:** AC2 is already enforced for Infrastructure, EF, Npgsql, RabbitMQ, FluentValidation, M.E.DI, and OpenTelemetry (`ArchitectureTests.cs:28–60`; recon:160–163). DEV-298 adds the port whitelist and the HTTP/Process bans (AC5). **Keel plan decision:** the whitelist holds all eight constitution-listed names, so the rule does not have to be edited when DEV-340, DEV-348, or DEV-351 land their ports.
- **Q9 — Other ports:** excluded, because DEV-340, DEV-348, and DEV-351 cover them. No follow-up ticket.
- **Q10 — Tests and mutation:** mirror tests on the interfaces are not needed. Test the architecture rules and the real invariants. If a Stryker run finds zero mutants, it is reported as **N/A**, never PASS.
- **Q11 — Claim contract:** the XML doc is as in AC6.
- **Q12 — ADR:** none. All six ports are already on the constitution list.

## Plan decisions

- **Approach:** declaration-only Core work. The only behaviour is `PagedResult<T>` guard clauses (**Keel plan decision**): the constructor throws `ArgumentException` when `Items.IsDefault`, and `ArgumentOutOfRangeException` when `TotalCount < 0` or `TotalCount < Items.Length`. `MetadataLookupResult` exposes no `Match` method; consumers pattern-match on it. So Q10's "cover each case" does not trigger.
- **Naming:** the ticket and constitution name `IMediaLibraryScanner.Scan` directly. That takes precedence over the constitution VIII preference for "Import (not Scan)" for this one port name, so use it verbatim.
- **Records:** every contract type is a `sealed record` (nested union cases included), and every port is a `public interface`. There are no default interface members.
- **Test strategy:**
  - ArchitectureTests: one whitelist fact, plus two dependency-ban facts that reuse the existing `AssertNoDependency` helper.
  - UnitTests: `PagedResult<T>` tests cover a valid construction, a default array, a negative total, a total below the item count, and the boundary `TotalCount == Items.Length`. xUnit and AAA, matching the existing UnitTests conventions.
  - Nothing is written against the interfaces themselves.
- **Gate expectations** (all run in `-Files` or diff mode on the changed `.cs` files):
  - Roslyn analyzers: exit 0.
  - Cyclomatic complexity: exit 0 at threshold 15, then 6 for the refactor gate.
  - InspectCode: exit 0.
  - `dotnet format --verify-no-changes`: exit 0.
  - Vulnerable packages: exit 0.
  - `dotnet test` green, at 276 plus the new tests.
  - Stryker uses the D7a pattern (charter standing ruling) on `LamuFlix.Core` only. It is expected to mutate only the `PagedResult<T>` guards. A zero-mutant result is N/A per Q10, and any survivor is a FAIL.
  - Web gates: skipped by `harness.yml`, which is not a pass (recon:112–115).
- **Baseline-red rule:** if an AC5 ban fails because of existing Core code (not new code), stop and report `blocked:` with the offending type. Do not weaken the rule or edit that code, since that is care list #6.
- **Task ordering:**
  1. `PagedResult<T>` with its guard tests (tests first).
  2. The remaining contract types: `MovieSummary`, `MovieDetails`, `MetadataLookup`, `MetadataLookupResult`, `EnrichmentRequested`, and `ScannedMovie`.
  3. The six port interfaces, including the AC6 XML doc.
  4. The architecture tests (AC5).
  5. Gates and the diff-scope check (AC7, AC8).

## Plan challenge decisions (Keel, 2026-09-29)

Inputs: findings-DEV-298-risk, findings-DEV-298-standards, and findings-DEV-298-spec. The adjudication is in the DEV-298 note, lines 63–77. Patron's rulings are PD1 and PD2 (CONCLUSIONS.md:25–26).

- **PD1:** the HTTP ban is scoped to the client types (see the AC5 amendment).
- **PD2:** `PagedResult<T>` uses an explicit constructor with get-only properties (see the AC4 amendment). The guards and their order are unchanged: `IsDefault` is checked first, then one `totalCount < items.Length` comparison.
- **F-R2 mechanism (decided after recon R3, recon-DEV-298:264–324):** in NetArchTest 1.3.2, `HaveDependencyOn` matches an exact type name. It does not match sibling types (`Process` does not match `ProcessStartInfo`), and it does not walk inheritance (`HttpMessageInvoker` does not match `HttpClient`). No assembly that ArchitectureTests loads references either kind of type, so a canary is not possible.
  - **Process ban:** `HaveDependencyOnAny("System.Diagnostics.Process", "System.Diagnostics.ProcessStartInfo")` over Core. Use the existing `AssertNoDependency` helper (`ArchitectureTests.cs:146–154`) if it takes several names. Otherwise call NetArchTest directly in the same file.
  - **HTTP ban:** at test time, compute the banned names by reflection. From the `System.Net.Http` assembly (`typeof(HttpMessageInvoker).Assembly`), collect every exported type that is assignable to `HttpMessageInvoker` or `HttpMessageHandler`. That set covers `HttpClient`, `HttpClientHandler`, `SocketsHttpHandler`, `DelegatingHandler`, and the others. Pass the full names to `HaveDependencyOnAny` over Core. A subclass declared inside Core depends on its base type, so this check catches it too. `HttpRequestException` is not in the set.
  - **Self-check against a vacuous pass:** the same fact asserts that the computed set is non-empty and contains `System.Net.Http.HttpClient`.
  - The change stays additions-only in `ArchitectureTests.cs`, with no fixture, no new using outside that file, and no package.
- **F-R4:** no violating fixture file, because the frozen scope is kept. Instead, the whitelist fact also asserts that `LamuFlix.Core.Ports` contains at least 6 interfaces, so it cannot pass while checking nothing.
- **Compass F3:** the spec drops the claim that "no other type can derive". The private constructor blocks ordinary derivation. Derivation through the synthesized protected copy constructor is a known limitation, and the AC4 union shape is unchanged.
- **Compass F4:** the negative-total test uses non-default `Items`, such as an empty array. Only the default-array case uses `default`.
- **Wording fixes:** Ledger's F-STD-2, F-STD-3, and F-STD-4 are accepted as wording fixes (the `MetadataLookupResult` abstract carve-out, `public` in spec US2, and the pinned `Library/` test path).
