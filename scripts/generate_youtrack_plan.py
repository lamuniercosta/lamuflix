# scripts/generate_youtrack_plan.py
"""
Generates the complete youtrack-plan.json file containing all epics and tasks
derived from docs/architecture/architecture-plan.md for LamuFlix.
"""

import json
import os

epics = [
    {
        "key": "epic-0",
        "idReadable": "DEV-99",
        "action": "modify",
        "type": "Epic",
        "priority": "Major",
        "summary": "LamuFlix Foundation (Commits DEV-12..16)",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """Container for foundational LamuFlix commits landed from the locked plan:
- DEV-12: git init and commit the .NET Core 2.1 baseline
- DEV-13: Remove the cmd.exe command-injection vector
- DEV-79: Local Play: direct Process.Start (argv), behind Features:LocalPlay
- DEV-14: Upgrade Web and Data projects to .NET 10
- DEV-15: Replace the WinForms worker with a BackgroundService
- DEV-16: Redesign the queue workload as metadata enrichment jobs

All these tasks are completed and represent the baseline up to commit 9c4765c."""
    },
    {
        "key": "epic-1",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 1: Solution Reshape and Hygiene",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: New layout builds, CI green on main, legacy folders gone.

This epic establishes the target solution structure (src/, tests/), central package management, NetAnalyzers and BannedSymbols guardrails, replaces MSTest with xUnit v3 + NSubstitute, sets up GitHub Actions CI, and codifies ubiquitous language in CONTEXT.md and initial ADRs.

Refer to section 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-2",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 2: Core Model and Pipeline",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: Core has no infrastructure references, handlers unit-tested, Stryker >= 80 on Core.

This epic implements the Movie domain aggregate with its enrichment state machine, failure category taxonomy, explicit command/query handler pipeline with hand-wired decorators (Tracing, Logging, Validation), typed MovieQuery with sort whitelist, Core ports, and feature handlers organized into feature folders.

Refer to sections 4 and 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-3",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 3: Infrastructure Adapters",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: Integration tests pass against containers; no secrets or machine paths in code.

This epic implements the infrastructure adapters for external boundaries: PostgreSQL via EF Core (fresh Initial migration, skip navigations, atomic TryClaimForEnrichmentAsync), RabbitMQ 7 topology (quorum queues, retry with TTL, DLQ, trace header propagation), OMDb adapter with Microsoft.Extensions.Resilience, file system scanner over System.IO.Abstractions, process player launcher, and Testcontainers fixtures in Tests.Common.

Refer to sections 5 and 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-4",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 4: API Host",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: All endpoints in the table exist, OpenAPI snapshot committed, ProblemDetails on every error path.

This epic sets up LamuFlix.Api as a Minimal API host with TypedResults, ServiceDefaults (Serilog, OpenTelemetry, health checks, options validation), feature endpoint mappings (Library, Import, Enrichment, Watchlist, Playback, Facets), RFC 7807 ProblemDetails with global exception handling, Scalar UI, committed openapi.json, and WebApplicationFactory integration tests.

Refer to sections 6 and 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-5",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 5: Worker Host",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: End-to-end import -> enriched test passes; DLQ and retry proven in integration tests.

This epic implements the headless asynchronous background worker: EnrichmentConsumer on AsyncEventingBasicConsumer with per-message scope and trace propagation, outcome translation (ack, retry queue republish, DLQ), StrandedMovieSweeper periodic background service, worker health endpoint, and end-to-end integration tests.

Refer to sections 7 and 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-6",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 6: React SPA",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: Browse/filter/import/polling work against the API; Vitest green in CI; linkable bar reached.

This epic implements the modern React + TypeScript frontend in web/: Vite, TS strict, ESLint/Prettier, Vitest, RTL, MSW, generated API client from openapi.json, movie grid with URL-synced filter state, rich filter panel, movie details page with LocalPlay button, folder import dialog with TanStack Query polling, watchlist toggle, and the README modernization narrative with C4 diagram and CI badge.

Refer to sections 8 and 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-7",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 7: Compose, Observability, Portfolio Polish",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: `docker compose up` runs web + api + worker + postgres + rabbitmq + aspire dashboard with the sample library; README has a trace screenshot.

This epic provides multi-stage Dockerfiles, docker-compose.yml with health checks and dependency ordering, docker-compose.observability.yml with standalone Aspire Dashboard, CI workflow for container publishing, Stryker mutation testing and property tests, and ADR / C4 diagram completion.

Refer to sections 3, 7, and 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-8",
        "idReadable": "DEV-100",
        "action": "modify",
        "type": "Epic",
        "priority": "Minor",
        "summary": "Epic 8: Stretch",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: Advanced operational resiliency, performance testing, and deferred P2 features.

Includes: Transactional Outbox pattern replacing sweeper-only mitigation, SignalR real-time status push, TMDb secondary provider with chained resolution, .NET Aspire AppHost orchestration, bulk library scan with bounded concurrency, Playwright E2E smoke tests and k6 load tests, and advanced filter facets (Rating range, Director, Collection).

Refer to section 11 of `docs/architecture/architecture-plan.md`."""
    },
    {
        "key": "epic-9",
        "action": "create",
        "type": "Epic",
        "priority": "Major",
        "summary": "Epic 9: Movie Recommendations",
        "estimatedTime": None,
        "parent": "DEV-93",
        "description": """**Goal**: The movie details page shows up to ten related movies from the library, each with reason chips, ranked on movie characteristics and the user's watch history.

Content-based hybrid recommendation system strictly constrained to the local library:
- Phase A: Structured similarity (IDF-weighted genres, director/writer/cast overlap, proximity decay, reason chips)
- Phase B: Plot embeddings (pgvector cosine similarity, Ollama local embeddings, background embedding worker)
- Phase C: Watch history & taste profile (playback events, feedback commands, dynamic profile via HybridCache, MMR diversification, offline evaluation harness)

Refer to sections 4.6 and 11 of `docs/architecture/architecture-plan.md`."""
    }
]

tasks = []

# -------------------------------------------------------------
# Epic 1 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-1.1",
    "epicKey": "epic-1",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Add global.json, Directory.Packages.props, .editorconfig, and BannedSymbols.txt",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Establish central tool and package version management, compiler enforcement, and banned symbol analysis.

### Scope & Technical Design
1. **global.json**:
   - Pin .NET SDK to `10.0.x` with `rollForward: "latestPatch"`.
2. **Directory.Packages.props**:
   - Enable Central Package Management (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`).
   - Consolidate all package versions across the solution here (EF Core, Npgsql, RabbitMQ.Client, Serilog, OpenTelemetry, etc.). Remove explicit Version attributes from csproj `<PackageReference>` items.
3. **Directory.Build.props**:
   - Enable `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
   - Enable `<Nullable>enable</Nullable>` globally across all projects.
   - Configure Roslyn analyzers: `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.
4. **.editorconfig**:
   - Standardize repo-wide formatting rules (indentation, naming conventions, file scoped namespaces).
5. **BannedSymbols.txt**:
   - Configure `Microsoft.CodeAnalysis.BannedApiAnalyzers`.
   - Ban `System.DateTime.Now`, `System.DateTime.UtcNow`, `System.DateTime.Today`, `System.DateTimeOffset.Now`, `System.DateTimeOffset.UtcNow` in favor of BCL `System.TimeProvider`.

### Acceptance Criteria
- `dotnet build` succeeds across the solution with central package management enabled.
- Adding a line using `DateTime.Now` triggers a compilation error from BannedApiAnalyzers.
- All projects build with warnings as errors and nullable enabled."""
})

tasks.append({
    "key": "task-1.2",
    "epicKey": "epic-1",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Restructure solution layout into src/ and tests/ and delete legacy assets",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Move projects into clean `src/` and `tests/` folders and purge legacy, dead, and installer code from the repository.

### Scope & Technical Design
1. **Directory Restructure**:
   - Relocate existing projects to target layout:
     - `LamuFlix.Web/` -> `src/LamuFlix.Web/` (to be evolved into `src/LamuFlix.Api/`)
     - `LamuFlix.Data/` -> `src/LamuFlix.Data/` (to be evolved into `src/LamuFlix.Infrastructure/`)
     - `LamuFlix.Work/` -> `src/LamuFlix.Worker/` (rename project and namespace)
     - `LamuFlix.Test/` -> `tests/LamuFlix.Test/`
2. **Legacy Asset Purge**:
   - Delete `LamuFlix.Web.old/` (netcoreapp2.1 baseline).
   - Delete `SetupWorker/` and `LamuFlix.WorkerSetup/` (.vdproj installer projects).
   - Delete dead files: `Temp.cs`, unused legacy bower libraries in `wwwroot/`.
3. **Solution Update**:
   - Update `LamuFlix.sln` to reference the new project paths.

### Acceptance Criteria
- Solution builds cleanly from root `LamuFlix.sln`.
- No orphan files, .vdproj files, or Web.old directories remain in git tracking."""
})

tasks.append({
    "key": "task-1.3",
    "epicKey": "epic-1",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create LamuFlix.Core, Infrastructure, ServiceDefaults, Api skeletons and NetArchTest rules",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Create the target project skeletons and add architecture tests to prevent dependency leaks from day one.

### Scope & Technical Design
1. **Target Skeletons**:
   - `src/LamuFlix.Core` (net10.0 class library): Domain models, ports, feature folders, command/query handlers. Depends ONLY on BCL, `Microsoft.Extensions.Logging.Abstractions`, `System.Collections.Immutable`. Zero database or queue references.
   - `src/LamuFlix.Infrastructure` (net10.0 class library): References Core. Adapters for Postgres, RabbitMQ, OMDb, file system, process.
   - `src/LamuFlix.ServiceDefaults` (net10.0 class library): Shared hosting: Serilog, OpenTelemetry, health checks, options validation, TimeProvider.
   - `src/LamuFlix.Api` (net10.0 Web SDK): Minimal API host. References Core, Infrastructure, ServiceDefaults.
   - `tests/LamuFlix.ArchitectureTests` (net10.0 test project): NetArchTest.Rules or ArchUnitNET.
2. **Architecture Test Rules**:
   - `LamuFlix.Core` must NOT have dependencies on EF Core, Npgsql, RabbitMQ, or Infrastructure.
   - All Command and Query records in Core must be sealed.
   - All Handlers in Core must be sealed.
   - Internal feature folder implementations in Core must not be accessed cross-feature without ports.

### Acceptance Criteria
- Solution compiles with all new project skeletons added to `LamuFlix.sln`.
- `LamuFlix.ArchitectureTests` runs and passes.
- Artificially adding an EF Core reference to `LamuFlix.Core` causes architecture tests to fail the build."""
})

tasks.append({
    "key": "task-1.4",
    "idReadable": "DEV-280",
    "epicKey": "epic-1",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Migrate test suite from MSTest to xUnit v3 and NSubstitute, removing disk-bound tests",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Migrate the existing test project (`LamuFlix.Test`) to xUnit v3 + NSubstitute, aligning with repository quality standards and eliminating flaky disk-bound tests.

### Scope & Technical Design
1. **Framework Migration**:
   - Replace MSTest packages (`MSTest.TestAdapter`, `MSTest.TestFramework`) with `xunit.v3` and `xunit.runner.visualstudio`.
   - Replace Moq with `NSubstitute`.
   - Use Shouldly for assertions and AutoFixture / Faker.Net for test data (constitution Principle IX: no FluentAssertions, Moq or Bogus; `Assert.Collection` is the only xUnit assert allowed).
2. **Test Conversions**:
   - Migrate `WorkerTests.cs` and `EnrichmentTests.cs` to xUnit facts/theories and NSubstitute mocks.
   - Delete the four ignored disk-bound tests in `UnitTest1.cs` that depended on `F:\\Filmes` (these will be replaced with `System.IO.Abstractions` / `MockFileSystem` tests in Epic 3).
3. **Common Fixtures**:
   - Establish `tests/LamuFlix.Tests.Common/` for shared builders and domain object generators.

### Acceptance Criteria
- `dotnet test` discovers and runs all tests using xUnit v3.
- Zero MSTest references or attributes (`[TestClass]`, `[TestMethod]`, `[Ignore]`) remain.
- All migrated tests pass deterministically without touching physical disk."""
})

tasks.append({
    "key": "task-1.5",
    "idReadable": "DEV-22",
    "epicKey": "epic-1",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement GitHub Actions CI workflow (build, test, format, analyzer check)",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Establish a robust GitHub Actions CI pipeline running on every push and pull request to ensure the main branch is continuously verified and green.

### Scope & Technical Design
1. **Workflow Configuration (`.github/workflows/ci.yml`)**:
   - Trigger on push to `main` and pull requests against `main`.
   - Setup runner on `ubuntu-latest` (or matrix with `windows-latest` if needed).
   - Use `actions/setup-dotnet` pinned to .NET 10 via `global.json`.
   - Step 1: `dotnet restore`
   - Step 2: `dotnet build --configuration Release --no-restore`
   - Step 3: `dotnet test --configuration Release --no-build --verbosity normal`
   - Step 4: `dotnet format --verify-no-changes`
2. **README Badge**:
   - Add GitHub Actions CI workflow status badge to `README.md`.

### Acceptance Criteria
- Workflow executes successfully on pull requests and pushes.
- Fails if code formatting violations or compiler warnings exist.
- README displays active green CI badge."""
})

tasks.append({
    "key": "task-1.6",
    "epicKey": "epic-1",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Establish CONTEXT.md ubiquitous language and initial architectural decision records (ADR-0001, 0002, 0010)",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Document the bounded context terminology and record foundational architectural decisions in formal ADRs.

### Scope & Technical Design
1. **CONTEXT.md**:
   - Define ubiquitous language in English using the Valcre format (term, definition, avoid):
     - **Movie**: avoid Filme, Film
     - **Library**: set of movies on disk under LibraryOptions.RootPath
     - **Import**: avoid Criar, Create, Scan (for single folder case)
     - **Enrichment**: avoid job, task, processing
     - **Enrichment Status**: Pending, Enriched, NotFound, Failed (avoid state, job status)
     - **Failure Category**: caller-safe taxonomy; avoid error type, raw exception
     - **Metadata Provider**: avoid hardcoding OMDb in Core names
     - **Watchlist**: avoid MinhaLista, My List
     - **Playback**: avoid Assistir, Watch
     - **Recommendation**: suggested library movie from details page
     - **Reason**: caller-safe explanation attached to recommendation
     - **Taste Profile**: recency-weighted aggregate of user's watched movies
     - **Playback Event**: record of play launch; avoid watch log, history entry
     - **Feedback**: explicit Watched / Not Interested marks
2. **ADRs in `docs/adr/`**:
   - Template: Status, Context, Decision, Consequences.
   - `ADR-0001`: Ports and adapters with feature-organised core (why not VSA, why not four-layer Clean).
   - `ADR-0002`: Explicit handlers and decorators instead of MediatR.
   - `ADR-0010`: TimeProvider, BannedSymbols, xUnit v3, and Testcontainers.

### Acceptance Criteria
- `CONTEXT.md` present at repository root.
- `docs/adr/ADR-0001.md`, `ADR-0002.md`, and `ADR-0010.md` committed with status Accepted."""
})

tasks.append({
    "key": "task-1.7",
    "epicKey": "epic-1",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Rename legacy Portuguese identifiers to CONTEXT.md ubiquitous language terms",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Perform a mechanical renaming refactor across C# codebase to replace legacy Portuguese identifiers with English ubiquitous language terms.

### Scope & Technical Design
1. **Renamings**:
   - `Filme` -> `Movie`
   - `FilmesController` -> `MoviesController`
   - `FilmesServices` -> `MovieService`
   - `CriarFilme` / `ProcessarFilme` -> `ImportMovieFolder`
   - `AssistirFilme` -> `PlayMovie`
   - `ExcluirFilme` -> `DeleteMovie`
   - `MinhaLista` -> `Watchlist`
   - `GetDetalhesFilmeAsync` -> `GetMovieDetails`
2. **Safety Guidelines**:
   - Pure mechanical rename PR. No behavioral or architectural logic changes.
   - Ensure all references and tests are updated.

### Acceptance Criteria
- Zero Portuguese identifiers remain in public or internal API surface.
- Solution builds and passes existing unit tests without failure."""
})

# -------------------------------------------------------------
# Epic 2 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-2.1",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Movie aggregate root, domain state machine, and sealed record value objects",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement the rich `Movie` domain aggregate in `src/LamuFlix.Core/Domain/` with private setters, validated state transitions, and strongly-typed value objects.

### Scope & Technical Design
1. **Movie Aggregate Root (`class Movie`)**:
   - Encapsulate internal state with private setters.
   - State transition methods:
     - `MarkEnriched(MovieMetadata metadata, DateTimeOffset now)`
     - `MarkNotFound(DateTimeOffset now)`
     - `MarkFailed(EnrichmentFailureCategory category, DateTimeOffset now)`
     - `RequestEnrichment()`: transitions Failed or NotFound back to Pending.
     - `AddToWatchlist()`, `RemoveFromWatchlist()`
   - Validation: Illegal state transitions must throw `InvalidTransitionException` (mapped to HTTP 409).
   - Enrichment tracking fields:
     - `EnrichmentStatus Status` (Pending=0, Enriched=1, NotFound=2, Failed=3)
     - `DateTimeOffset? EnrichedAt`
     - `int EnrichmentAttempts`
     - `EnrichmentFailureCategory? LastFailureCategory`
     - `DateTimeOffset? LastAttemptAt`
2. **Value Objects (`sealed record`)**:
   - `MovieId(int Value)`
   - `ImdbId(string Value)`: with `TryParse` checking pattern `^tt\\d{7,8}$`
   - `ImdbRating(decimal Value)`: constrained 0.0 to 10.0 with 1 decimal place
   - `Runtime(int Minutes)`: positive integer
   - `ReleaseYear(int Value)`: 1888 to current year + 5
   - `LibraryPath(string Value)`: rejects relative traversals (`..`)
   - `MediaFormat(string Extension)`
3. **Unit Tests**:
   - Unit tests covering every legal state transition and verifying that every illegal state transition throws `InvalidTransitionException`.
   - Unit tests for value object parsing and constraints.

### Acceptance Criteria
- 100% branch test coverage on Movie state machine.
- Value objects enforce validation rules and reject malformed inputs."""
})

tasks.append({
    "key": "task-2.2",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement EnrichmentFailureCategory taxonomy and root-cause exception classifier",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement a structured failure taxonomy for metadata enrichment failures to replace raw exception text, and author ADR-0006.

### Scope & Technical Design
1. **EnrichmentFailureCategory**:
   - Create SmartEnum or sealed record set:
     - `ProviderUnavailable`: retryable (transient 5xx, network drop)
     - `RateLimited`: retryable with delay (HTTP 429)
     - `InvalidResponse`: not retryable (malformed payload, HTTP 401)
     - `Unknown`: retryable up to max attempts
   - Properties: `string Code`, `string SafeDescription`, `bool IsRetryable`.
2. **EnrichmentFailureClassifier**:
   - Single classification service mapping root-cause exceptions (e.g. `HttpRequestException`, `TimeoutException`, status code checks) to `EnrichmentFailureCategory`.
   - Raw exception details, stack traces, and internal URLs must go to OpenTelemetry spans and logs ONLY, never to database columns or API responses.
3. **ADR-0006**:
   - Write `docs/adr/ADR-0006.md`: Failure categories separated from exception types; raw errors never leave logs.

### Acceptance Criteria
- Classifier categorizes known exceptions accurately in unit tests.
- `EnrichmentFailureCategory` exposes caller-safe descriptions.
- `docs/adr/ADR-0006.md` accepted."""
})

tasks.append({
    "key": "task-2.3",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement ICommandHandler/IQueryHandler pipeline with Tracing, Logging, and Validation decorators",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement a clean, explicit command/query pipeline in `LamuFlix.Core/Pipeline/` without third-party mediator dependencies (no MediatR), following the Valcre pattern.

### Scope & Technical Design
1. **Interfaces**:
   - `ICommandHandler<TCommand, TResult> where TCommand : sealed record`
   - `IQueryHandler<TQuery, TResult> where TQuery : sealed record`
2. **Decorators (Outer to Inner)**:
   - `TracingDecorator<TReq, TRes>`: starts an OpenTelemetry `Activity` named after the command/query type.
   - `LoggingDecorator<TReq, TRes>`: logs structured information (command type, execution time, result/failure).
   - `ValidationDecorator<TReq, TRes>`: invokes FluentValidation `IValidator<TReq>`; throws `ValidationException` (mapped to HTTP 422) on failure.
3. **Registration Helper**:
   - `services.AddHandler<THandler, TReq, TRes>()` extension method applying the decorator chain in one readable line per handler.
4. **Unit Tests**:
   - Pipeline unit tests verifying decorator order, tracing activity propagation, and validation interruption.

### Acceptance Criteria
- Pipeline wraps handlers cleanly with tracing, logging, and validation.
- Commands with validation failures throw `ValidationException` before the handler is called.
- No third-party mediator library referenced."""
})

tasks.append({
    "key": "task-2.4",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create typed MovieQuery record, MovieSort enum, and FluentValidation rules",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Replace legacy reflection-based DynamicQuery and DynamicSort with a typed `MovieQuery` model, a sort whitelist, and author ADR-0009.

### Scope & Technical Design
1. **MovieQuery Record**:
   - `string? Text`
   - `ImmutableArray<int> GenreIds`
   - `ImmutableArray<int> ActorIds`
   - `RuntimeRange? Runtime` (Min, Max, IncludeUnknown)
   - `YearRange? Year` (Min, Max)
   - `ImmutableArray<EnrichmentStatus> Statuses`
   - `bool? InWatchlist`
   - `MovieSort Sort` (enum: Title, Year, Rating, Runtime)
   - `SortDirection Direction` (enum: Ascending, Descending)
   - `Page Page` (Number, Size with maximum 100)
2. **Validation**:
   - FluentValidation validator for `MovieQuery`:
     - Page.Size between 1 and 100.
     - Page.Number >= 1.
     - Range minimums <= maximums.
3. **ADR-0009**:
   - Author `docs/adr/ADR-0009.md`: Typed MovieQuery replaces reflection-based dynamic queries.
4. **Property-Based Tests**:
   - FsCheck test in `LamuFlix.UnitTests` verifying that any valid `MovieQuery` serializes to query string and deserializes back identically.

### Acceptance Criteria
- Replaces legacy `EntityExtensions.cs` reflection query strings.
- FsCheck round-trip property tests pass.
- `docs/adr/ADR-0009.md` accepted."""
})

tasks.append({
    "key": "task-2.5",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Define Core ports for persistence, catalog, metadata, messaging, file system, and playback",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Define the external boundary seams in `src/LamuFlix.Core/Ports/` to enable pure ports-and-adapters architecture.

### Scope & Technical Design
1. **Ports Interfaces**:
   - `IMovieRepository`:
     - `Task<Movie?> GetAsync(MovieId id, CancellationToken ct)`
     - `Task AddAsync(Movie movie, CancellationToken ct)`
     - `Task SaveChangesAsync(CancellationToken ct)`
     - `Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)`
   - `IMovieCatalog`:
     - `Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)`
     - `Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)`
   - `IMetadataProvider`:
     - `Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)`
     - `MetadataLookupResult` discriminated union: Found(MovieMetadata) | NotFound | Failed(EnrichmentFailureCategory)
   - `IEnrichmentQueue`:
     - `Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct)`
     - `EnrichmentRequested(MovieId MovieId, int Attempt)`
   - `IMediaLibraryScanner`:
     - `ScannedMovie Scan(LibraryPath folder)`
   - `IMediaPlayerLauncher`:
     - `void Launch(LibraryPath file, MediaFormat format)`
2. **TimeProvider**:
   - Inject `System.TimeProvider` across all time-sensitive port methods.

### Acceptance Criteria
- All external dependencies (EF, RabbitMQ, OMDb, FileSystem, Process) abstracted behind interfaces in `LamuFlix.Core`.
- Core has zero references to concrete infrastructure libraries."""
})

tasks.append({
    "key": "task-2.6",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement feature handlers for Library, Import, Enrichment, Watchlist, and Playback",
    "estimatedTime": "3d",
    "repository": "lamuflix",
    "description": """### Overview
Implement all core use cases organized by feature folders in `src/LamuFlix.Core/Features/`.

### Scope & Technical Design
1. **Features/Library/**:
   - `BrowseMoviesQueryHandler` -> calls `IMovieCatalog.BrowseAsync`.
   - `GetMovieDetailsQueryHandler` -> calls `IMovieCatalog.GetDetailsAsync`.
2. **Features/Import/**:
   - `ImportMovieFolderCommandHandler`:
     - Calls `IMediaLibraryScanner.Scan(path)`.
     - Creates `Movie` with `EnrichmentStatus.Pending`.
     - Calls `IMovieRepository.AddAsync` & `SaveChangesAsync`.
     - Enqueues `EnrichmentRequested(movie.Id, 1)` via `IEnrichmentQueue`.
3. **Features/Enrichment/**:
   - `ClaimEnrichmentCommandHandler`: calls `IMovieRepository.TryClaimForEnrichmentAsync`.
   - `ApplyEnrichmentResultCommandHandler`: applies `MovieMetadata` or `NotFound`.
   - `RecordEnrichmentFailureCommandHandler`: evaluates retry eligibility or marks failed.
   - `RequestEnrichmentCommandHandler`: handles manual UI retry trigger for NotFound/Failed rows.
   - `RequeueStrandedMoviesCommandHandler`: claims stranded Pending movies and re-enqueues them.
4. **Features/Watchlist/**:
   - `AddToWatchlistCommandHandler`, `RemoveFromWatchlistCommandHandler`.
5. **Features/Playback/**:
   - `PlayMovieCommandHandler`: retrieves path, calls `IMediaPlayerLauncher.Launch`.
6. **Unit Tests**:
   - Comprehensive unit tests for every handler using NSubstitute doubles.

### Acceptance Criteria
- All use cases implemented with zero database or RabbitMQ dependencies.
- 100% unit test pass rate across all feature handlers."""
})

tasks.append({
    "key": "task-2.7",
    "epicKey": "epic-2",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement strongly-typed Options records with DataAnnotations and startup validation",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Eliminate magic configuration strings and hardcoded fallbacks by implementing strongly-typed options records with DataAnnotations.

### Scope & Technical Design
1. **Options Records**:
   - `LibraryOptions`: `[Required] string RootPath`
   - `PlaybackOptions`: `PlayerConfig[] Players` with `Name`, `ExecutablePath`, `Formats[]`
   - `OmdbOptions`: `[Required] string ApiKey`, `[Required, Url] string BaseUrl`
   - `RabbitMqOptions`: `[Required] string HostName`, `int Port`, `string UserName`, `string Password`
   - `EnrichmentOptions`: `int MaxAttempts = 3`, `TimeSpan SweepInterval`, `TimeSpan ClaimLease`
   - `FeatureOptions`: `bool LocalPlay`
2. **Validation on Start**:
   - Register options with `services.AddOptions<TOptions>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`.

### Acceptance Criteria
- Application terminates immediately on startup with a descriptive error if required configuration is missing.
- No raw `IConfiguration["..."]` strings used in feature code."""
})

# -------------------------------------------------------------
# Epic 3 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-3.1",
    "idReadable": "DEV-19",
    "epicKey": "epic-3",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Postgres persistence via EF Core, entity configurations, and fresh Initial migration",
    "estimatedTime": "3d",
    "repository": "lamuflix",
    "description": """### Overview
Replace MySQL/Pomelo with PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`, author `IEntityTypeConfiguration<T>` per entity, generate a single clean Initial migration, and write ADR-0003.

### Scope & Technical Design
1. **EF Core Modernization**:
   - Remove `Pomelo.EntityFrameworkCore.MySql` and MySQL connection strings.
   - Add `Npgsql.EntityFrameworkCore.PostgreSQL`.
   - Remove the `Player` table entirely (player configuration moves to `PlaybackOptions`).
   - Remove explicit join entities (`MovieActor`, `MovieDirector`, `MovieGenre`); configure EF Core skip navigations for many-to-many relationships.
2. **Entity Configurations (`src/LamuFlix.Infrastructure/Persistence/Configurations/`)**:
   - `MovieConfiguration`:
     - Primary key, table name `movies`.
     - Value object conversions (`MovieId`, `ImdbId`, `ImdbRating`, etc.).
     - Nullable fields: `Duration`, `ImdbRating`, `RottenTomatoes`, `MetaScore`, `Year`.
     - Indexes: `Title`, `Year`, `Status`, unique index on `ImdbId` (where not null), unique index on `LibraryPath`.
3. **Migration & ADR**:
   - Generate fresh single migration `Initial`.
   - Write `docs/adr/ADR-0003.md`: Postgres with single fresh migration; no data migration from MySQL.

### Acceptance Criteria
- Single `Initial` migration applies cleanly against empty PostgreSQL container.
- No MySQL packages or references exist in solution.
- `docs/adr/ADR-0003.md` accepted."""
})

tasks.append({
    "key": "task-3.2",
    "epicKey": "epic-3",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement EfMovieRepository with atomic TryClaimForEnrichmentAsync via ExecuteUpdateAsync",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `EfMovieRepository` in `LamuFlix.Infrastructure/Persistence/Repositories/` featuring atomic state transitions for worker claiming.

### Scope & Technical Design
1. **Implementation**:
   - Implement `IMovieRepository`.
   - Implement `TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)`:
     - Use EF Core `ExecuteUpdateAsync`:
       `UPDATE movies SET last_attempt_at = now, enrichment_attempts = enrichment_attempts + 1 WHERE id = @id AND status = 0 AND (last_attempt_at IS NULL OR last_attempt_at < @leaseExpiry)`
     - Returns true if 1 row updated; false if 0 rows (already claimed or terminal).
2. **Concurrency Integration Test**:
   - Using Testcontainers PostgreSQL, simulate 2 concurrent workers attempting to claim the same movie simultaneously.
   - Assert that exactly one worker receives `true` and the other receives `false`.

### Acceptance Criteria
- Concurrency integration test passes reliably against real Postgres container.
- Claim is guaranteed atomic without table locking."""
})

tasks.append({
    "key": "task-3.3",
    "epicKey": "epic-3",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement EfMovieCatalog with composable IQueryable predicates and NULLS LAST sorting",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `EfMovieCatalog` in `LamuFlix.Infrastructure/Persistence/` providing high-performance query projections with `NULLS LAST` sorting.

### Scope & Technical Design
1. **Predicate Composition**:
   - Create clean, chainable `IQueryable<Movie>` extensions:
     - `WhereText(string? text)`: case-insensitive title search (using ILike in Postgres).
     - `WhereGenres(ImmutableArray<int> genreIds)`
     - `WhereActors(ImmutableArray<int> actorIds)`
     - `WhereRuntime(RuntimeRange? range)`
     - `WhereYear(YearRange? range)`
     - `WhereStatuses(ImmutableArray<EnrichmentStatus> statuses)`
     - `WhereInWatchlist(bool? inWatchlist)`
2. **NULLS LAST Sorting**:
   - Implement explicit sort mapping using `OrderBy(m => m.Year == null).ThenBy(...)` or EF functions to ensure null years/ratings/runtimes sort to the end.
3. **Direct Projection**:
   - Project directly to `MovieSummary` records with `.Select(...)` without entity tracking (`AsNoTracking`).
4. **Integration Tests**:
   - Test each filter predicate and verify NULLS LAST sort order against Testcontainers Postgres.

### Acceptance Criteria
- Integration tests confirm correct filtering and pagination.
- Movies with null ratings/years appear at the end of sorted queries."""
})

tasks.append({
    "key": "task-3.4",
    "idReadable": "DEV-18",
    "epicKey": "epic-3",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Upgrade to RabbitMQ.Client 7.x, declare quorum topology, retry/DLQ, and trace propagation",
    "estimatedTime": "3d",
    "repository": "lamuflix",
    "description": """### Overview
Upgrade to `RabbitMQ.Client` 7.x (async API), declare resilient queue topology (quorum, TTL retry, DLQ), inject OpenTelemetry trace headers, and write ADR-0004/0005.

### Scope & Technical Design
1. **RabbitMQ.Client 7 Migration**:
   - Upgrade from 6.8 to 7.x. Adopt `IChannel` and asynchronous event consumer APIs.
2. **Topology (`RabbitMqTopology`)**:
   - Exchange: `lamuflix.enrichment` (direct, durable).
   - Main Queue: `enrichment.requested` (quorum queue, prefetch configured from options).
   - Retry Queue: `enrichment.retry` (x-message-ttl set for delay, dead-letters back to `lamuflix.enrichment`).
   - DLQ: `enrichment.dead-letter` (poison message destination).
3. **Publisher with Publisher Confirms & Tracing**:
   - Implement `RabbitMqEnrichmentQueuePublisher` implementing `IEnrichmentQueue`.
   - Enable publisher confirms.
   - Inject `traceparent` and `tracestate` into message `BasicProperties.Headers` via OpenTelemetry `DefaultTextMapPropagator`.
4. **ADRs**:
   - Write `docs/adr/ADR-0004.md`: RabbitMQ topology with retry queue, DLQ, and sweeper.
   - Write `docs/adr/ADR-0005.md`: Dual-write mitigation: sweeper now, transactional outbox as stretch.

### Acceptance Criteria
- Integration test with Testcontainers RabbitMQ validates publish confirms, TTL retry routing, and DLQ.
- Message headers contain W3C traceparent context.
- ADR-0004 and ADR-0005 accepted."""
})

tasks.append({
    "key": "task-3.5",
    "epicKey": "epic-3",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement OMDb adapter with typed HttpClient, resilience pipeline, and WireMock tests",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `OmdbMetadataProvider` implementing `IMetadataProvider` using typed `HttpClient` and Microsoft.Extensions.Http.Resilience.

### Scope & Technical Design
1. **Resilience Pipeline**:
   - Configure `AddResilienceHandler` with:
     - Retry with exponential backoff and jitter on 5xx and 429 (honoring `Retry-After` header).
     - Circuit breaker.
     - Timeout.
2. **Response Mapping**:
   - 200 with `"Response":"True"` -> `MetadataLookupResult.Found(MovieMetadata)`
   - 200 with `"Response":"False"` -> `MetadataLookupResult.NotFound`
   - 401 Unauthorized -> `MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse)` with clear log to check API key.
   - 429 Too Many Requests -> `MetadataLookupResult.Failed(EnrichmentFailureCategory.RateLimited)`
   - 5xx / Timeout -> `MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable)`
3. **Secret Hygiene**:
   - Remove hardcoded API key fallback "<redacted>". Key must come from environment or User Secrets.
4. **Integration Tests**:
   - WireMock.Net test suite verifying Found, NotFound, 401, 429, and 500 scenarios.

### Acceptance Criteria
- WireMock tests verify retry on transient errors and proper classification of failure codes.
- No hardcoded API keys exist in code."""
})

tasks.append({
    "key": "task-3.6",
    "epicKey": "epic-3",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement DirectoryMediaLibraryScanner with System.IO.Abstractions and MockFileSystem tests",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `DirectoryMediaLibraryScanner` in `LamuFlix.Infrastructure/FileSystem/` over `System.IO.Abstractions` to enable testable disk scanning.

### Scope & Technical Design
1. **Implementation**:
   - Implement `IMediaLibraryScanner`.
   - Parse Title and ReleaseYear from folder name patterns (e.g. "Inception (2010)", "The Matrix [1999]").
   - Locate primary video file (`.mkv`, `.mp4`, `.avi`, `.m4v`).
   - Extract media format and file size.
2. **Unit Tests with MockFileSystem**:
   - Test folder naming conventions, multi-word titles, year brackets vs parentheses.
   - Test missing video file handling, unsupported formats, nested folders.
   - Fully replaces the legacy ignored disk-bound tests in LamuFlix.Test.

### Acceptance Criteria
- Scanner runs against `IFileSystem` interface without physical disk requirements.
- MockFileSystem test suite achieves 100% code coverage."""
})

tasks.append({
    "key": "task-3.7",
    "epicKey": "epic-3",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement ProcessMediaPlayerLauncher and DisabledMediaPlayerLauncher behind LocalPlay flag",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement process-based media player launching behind the `Features:LocalPlay` feature gate, preserving security improvements from DEV-79.

### Scope & Technical Design
1. **Implementation**:
   - `ProcessMediaPlayerLauncher : IMediaPlayerLauncher`:
     - Resolves player executable from `PlaybackOptions.Players` based on file format.
     - Uses `ProcessStartInfo.ArgumentList.Add(...)` (never a shell string) to launch media player process.
   - `DisabledMediaPlayerLauncher : IMediaPlayerLauncher`:
     - Throws `FeatureDisabledException("LocalPlay is disabled in this environment.")`.
2. **DI Registration**:
   - If `FeatureOptions.LocalPlay == true`, register `ProcessMediaPlayerLauncher`.
   - Else, register `DisabledMediaPlayerLauncher`.
3. **Unit Tests**:
   - Test argument list sanitization and feature gate registration.

### Acceptance Criteria
- Command injection vector remains permanently eliminated.
- Throws `FeatureDisabledException` when disabled."""
})

tasks.append({
    "key": "task-3.8",
    "epicKey": "epic-3",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create reusable Testcontainers fixtures for PostgreSQL and RabbitMQ in Tests.Common",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Create centralized, high-performance Testcontainers fixtures for PostgreSQL and RabbitMQ in `tests/LamuFlix.Tests.Common/`.

### Scope & Technical Design
1. **Container Fixtures**:
   - `PostgreSqlTestContainerFixture`: starts `postgres:17-alpine`, executes migrations automatically on startup, provides connection string and clean reset mechanism.
   - `RabbitMqTestContainerFixture`: starts `rabbitmq:4-management-alpine`, declares topology, provides connection factory.
2. **xUnit Integration**:
   - Use xUnit v3 collection fixtures to share containers across integration test classes, minimizing spin-up overhead.

### Acceptance Criteria
- Integration test suite shares containers efficiently.
- Containers tear down cleanly after test runs."""
})

# -------------------------------------------------------------
# Epic 4 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-4.1",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement ServiceDefaults with Serilog, OpenTelemetry OTLP, health checks, and options validation",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement shared hosting defaults in `src/LamuFlix.ServiceDefaults/Extensions.cs` to standardise logging, telemetry, health probes, and time management.

### Scope & Technical Design
1. **Serilog**:
   - Structured JSON logging to Console; OTLP exporter for OpenTelemetry logs.
2. **OpenTelemetry**:
   - Traces and Metrics with OTLP exporter (`http://localhost:4317` or `OTEL_EXPORTER_OTLP_ENDPOINT`).
   - Standard instrumentation: ASP.NET Core, HttpClient, Npgsql, RabbitMQ.
   - ActivitySource `"LamuFlix"`.
3. **Health Checks**:
   - `/health/live` (live probe - basic ping).
   - `/health/ready` (readiness probe - verifies PostgreSQL connection and RabbitMQ channel).
4. **TimeProvider**:
   - Register `TimeProvider.System` as singleton.

### Acceptance Criteria
- Health probes respond at `/health/live` (200) and `/health/ready` (200 or 503).
- Telemetry exports via OTLP."""
})

tasks.append({
    "key": "task-4.2",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Convert LamuFlix.Web to LamuFlix.Api using Minimal APIs and delete legacy MVC/Razor assets",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Convert the web host project into `LamuFlix.Api` utilizing modern Minimal APIs and delete legacy MVC controllers, views, and session state.

### Scope & Technical Design
1. **Legacy Purge**:
   - Delete `Startup.cs`, `FilmesController.cs`, Razor Views (`Views/`), ViewModels, TagHelpers, and session configuration.
2. **Program.cs Modernization**:
   - Use `WebApplication.CreateBuilder()`.
   - Call `builder.AddServiceDefaults()`.
   - Register Core handlers, Infrastructure adapters, CORS policy for Vite SPA (`http://localhost:5173`).
   - Call `app.MapDefaultEndpoints()`.
   - Call feature endpoint mappers (`MapLibraryEndpoints`, `MapImportEndpoints`, etc.).

### Acceptance Criteria
- API starts cleanly as a Minimal API host without MVC or Razor dependencies.
- CORS configured for React dev server."""
})

tasks.append({
    "key": "task-4.3",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement GET /api/movies and GET /api/movies/{id} Minimal API endpoints",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement the core library browse and movie details Minimal API endpoints in `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`.

### Scope & Technical Design
1. **GET /api/movies**:
   - Bind query string parameters directly to `MovieQuery` record via `[AsParameters]`.
   - Dispatch `BrowseMoviesQuery` through query pipeline.
   - Return `Results<Ok<PagedResult<MovieSummary>>, ValidationProblem>`.
2. **GET /api/movies/{id}**:
   - Parse `int id` as `MovieId`.
   - Dispatch `GetMovieDetailsQuery`.
   - Return `Results<Ok<MovieDetails>, NotFound<ProblemDetails>>`.
3. **OpenAPI Metadata**:
   - Annotate with `.WithName()`, `.Produces<...>()`, `.WithSummary()`.

### Acceptance Criteria
- Query parameters bind correctly to `MovieQuery`.
- Endpoints return strongly-typed `TypedResults` with OpenAPI metadata."""
})

tasks.append({
    "key": "task-4.4",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Import, Enrichment retry, Watchlist, Playback, and Facet Minimal API endpoints",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement remaining Minimal API endpoints for folder import, enrichment retry, watchlist management, playback, and metadata facets.

### Scope & Technical Design
1. **Endpoints**:
   - `POST /api/movies/import`: Request body `ImportMovieRequest(string FolderPath)`. Returns `202 Accepted` with `Location: /api/movies/{id}`.
   - `POST /api/movies/{id}/enrichment`: Manual re-enrichment trigger. Returns `202 Accepted`.
   - `POST /api/movies/{id}/watchlist`: Adds to watchlist. Returns `204 No Content`.
   - `DELETE /api/movies/{id}/watchlist`: Removes from watchlist. Returns `204 No Content`.
   - `POST /api/movies/{id}/play`: Launches local player. Returns `204 No Content` or `403 Forbidden` if LocalPlay disabled.
   - `GET /api/genres`: Returns distinct list of `GenreDto(int Id, string Name)`.
   - `GET /api/people?role=actor`: Returns list of `PersonDto(int Id, string Name)`.

### Acceptance Criteria
- All endpoints conform to the route table in section 6 of `architecture-plan.md`.
- Endpoints return exact HTTP status codes and headers."""
})

tasks.append({
    "key": "task-4.5",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Configure ProblemDetails and IExceptionHandler for domain exception mapping",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement RFC 7807 ProblemDetails error handling and domain exception mapping via `IExceptionHandler`.

### Scope & Technical Design
1. **GlobalExceptionHandler (`IExceptionHandler`)**:
   - Map `NotFoundException` -> 404 Not Found
   - Map `ValidationException` -> 422 Unprocessable Entity (with field validation errors dictionary)
   - Map `InvalidTransitionException` -> 409 Conflict
   - Map `FeatureDisabledException` -> 403 Forbidden
   - Unhandled exceptions -> 500 Internal Server Error
2. **Payload Structure**:
   - Include `traceId` (from `Activity.Current?.Id ?? HttpContext.TraceIdentifier`).
   - Include standard RFC 7807 fields: `type`, `title`, `status`, `detail`, `instance`.
3. **JSON Serializer Configuration**:
   - Register `JsonStringEnumConverter` so `EnrichmentStatus` serializes as readable strings in JSON.

### Acceptance Criteria
- All error responses return standard `application/problem+json` containing `traceId`.
- No raw exception messages or stack traces leak to HTTP clients."""
})

tasks.append({
    "key": "task-4.6",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Configure OpenAPI, Scalar UI, commit web/src/api/openapi.json, and add drift test",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Configure OpenAPI document generation with Scalar UI, commit the generated OpenAPI document, and author ADR-0007.

### Scope & Technical Design
1. **OpenAPI & Scalar**:
   - Use built-in .NET 10 `Microsoft.AspNetCore.OpenApi`.
   - Map Scalar UI at `/scalar`.
2. **OpenAPI Snapshot & Drift Test**:
   - Commit generated OpenAPI JSON file to `web/src/api/openapi.json`.
   - In `LamuFlix.IntegrationTests`, create a Verify snapshot test comparing runtime OpenAPI document with `web/src/api/openapi.json`.
   - Fails CI if C# API models change without updating the committed OpenAPI schema.
3. **ADR-0007**:
   - Write `docs/adr/ADR-0007.md`: Minimal API with TypedResults, no versioning in P1, committed OpenAPI document with generated TS client.

### Acceptance Criteria
- Scalar UI accessible at `/scalar`.
- Snapshot test detects API schema changes and prevents silent contract drift.
- ADR-0007 accepted."""
})

tasks.append({
    "key": "task-4.7",
    "epicKey": "epic-4",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create WebApplicationFactory integration test suite with WireMock.Net for API endpoints",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Build a comprehensive API integration test suite using `WebApplicationFactory<Program>`, Testcontainers, and WireMock.Net.

### Scope & Technical Design
1. **Integration Test Suite**:
   - In `tests/LamuFlix.IntegrationTests/`:
     - Test browse filtering, pagination, and sorting against real Postgres container.
     - Test import endpoint and verify 202 response + Location header.
     - Test validation error mapping to 422 ProblemDetails.
     - Test play endpoint returning 403 when LocalPlay is disabled.
     - Test enrichment retry endpoint.

### Acceptance Criteria
- All endpoints tested end-to-end via WebApplicationFactory.
- Contract compliance verified against real Postgres and WireMock instances."""
})

# -------------------------------------------------------------
# Epic 5 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-5.1",
    "epicKey": "epic-5",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement EnrichmentConsumer BackgroundService with AsyncEventingBasicConsumer and trace propagation",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `EnrichmentConsumer` background service in `src/LamuFlix.Worker/` using RabbitMQ.Client 7 async consumer and OpenTelemetry trace extraction.

### Scope & Technical Design
1. **Async Consumer**:
   - Implement `EnrichmentConsumer : BackgroundService`.
   - Use `AsyncEventingBasicConsumer` on `enrichment.requested` quorum queue.
   - Configure channel prefetch from `EnrichmentOptions`.
2. **Trace Propagation**:
   - Extract `traceparent` and `tracestate` from `BasicProperties.Headers`.
   - Start an OpenTelemetry consumer `Activity` linked to the producer trace.
   - If message is redelivered, attach an `ActivityLink` to the original trace context.
3. **DI Scope per Message**:
   - Create DI service scope per message; resolve feature handlers.

### Acceptance Criteria
- Asynchronous message processing without blocking worker thread.
- Single distributed trace spans from API enqueue to worker processing."""
})

tasks.append({
    "key": "task-5.2",
    "epicKey": "epic-5",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement worker outcome translation for ack, retry queue republish, and DLQ routing",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement outcome handling in `EnrichmentConsumer` translating domain results into RabbitMQ ack, retry, or DLQ routing.

### Scope & Technical Design
1. **Outcome Translation**:
   - **Enriched / NotFound**: BasicAck message.
   - **Claim Failed** (already claimed or terminal): BasicAck message (idempotent no-op).
   - **Failed (Retryable, Attempts < Max)**:
     - BasicAck original message.
     - Republish to `enrichment.retry` with `Attempt + 1`. The retry queue's TTL dead-letters it back to main queue.
   - **Failed (Non-retryable OR Attempts >= Max)**:
     - BasicAck original message.
     - Publish to `enrichment.dead-letter` (DLQ) with failure reason headers.
2. **Integration Tests**:
   - Testcontainers RabbitMQ tests validating each routing path.

### Acceptance Criteria
- Poison messages never loop endlessly.
- Rate-limited and transient errors retry via TTL queue; fatal errors land in DLQ."""
})

tasks.append({
    "key": "task-5.3",
    "epicKey": "epic-5",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement StrandedMovieSweeper periodic background service for stranded Pending rows",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `StrandedMovieSweeper` in `src/LamuFlix.Worker/` to mitigate dual-write failures and worker crash scenarios.

### Scope & Technical Design
1. **Periodic Sweeper**:
   - Implement `StrandedMovieSweeper : BackgroundService` using `PeriodicTimer`.
   - Timer interval configured via `EnrichmentOptions.SweepInterval` (default 10 minutes).
2. **Re-enqueue Logic**:
   - Query for movies with `Status == Pending` where `LastAttemptAt == null` OR `LastAttemptAt < (now - ClaimLease)`.
   - Dispatch `RequeueStrandedMoviesCommand` to re-publish `EnrichmentRequested` messages.
3. **Integration Test**:
   - Insert a movie with `Status = Pending` and old timestamp without enqueuing a message.
   - Trigger sweeper and assert that message appears on RabbitMQ queue and movie is enriched.

### Acceptance Criteria
- Stranded movies are automatically recovered and re-enqueued.
- Sweeper ignores actively executing workers."""
})

tasks.append({
    "key": "task-5.4",
    "epicKey": "epic-5",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement worker health endpoint, cancellation token propagation, and graceful shutdown",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement health checks and graceful shutdown handling in `LamuFlix.Worker`.

### Scope & Technical Design
1. **Graceful Shutdown**:
   - Flow `stoppingToken` through all consumer loops and feature handlers.
   - On shutdown signal (SIGTERM), pause consumer, wait for in-flight message to finish or reject with requeue if incomplete.
2. **Health Check**:
   - Expose minimal health check (HTTP endpoint or heartbeat file) so Docker Compose can monitor worker liveness.

### Acceptance Criteria
- Worker stops cleanly within timeout during shutdown without losing messages.
- Container health check validates worker status."""
})

tasks.append({
    "key": "task-5.5",
    "epicKey": "epic-5",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create end-to-end integration test spanning API import, RabbitMQ queue, Worker, and OMDb",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Create the signature end-to-end integration test proving the entire asynchronous architecture from import trigger to enriched database state.

### Scope & Technical Design
1. **Test Scenario**:
   - Trigger `POST /api/movies/import` via `WebApplicationFactory`.
   - Assert movie row exists with `Status == Pending`.
   - Let worker consume message from Testcontainers RabbitMQ.
   - WireMock returns mock OMDb payload.
   - Poll or await worker processing.
   - Query `GET /api/movies/{id}` and assert `Status == Enriched` with populated metadata, genres, and cast.
   - Verify OpenTelemetry spans: API enqueue -> RabbitMQ -> Worker process share single trace ID.

### Acceptance Criteria
- End-to-end test passes deterministically.
- Proves end-to-end distributed trace propagation."""
})

tasks.append({
    "key": "task-5.6",
    "epicKey": "epic-5",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement TelemetryConstants, named spans, custom metrics, and write ADR-0008",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Standardize OpenTelemetry metrics, span names, and attributes in `TelemetryConstants`, and author ADR-0008.

### Scope & Technical Design
1. **TelemetryConstants**:
   - ActivitySource name: `"LamuFlix"`.
   - Spans: `Enrichment.Enqueue`, `Enrichment.Process`, `Metadata.Lookup`.
   - Metrics:
     - `lamuflix.enrichment.duration` (Histogram, tag provider).
     - `lamuflix.enrichment.outcome` (Counter, tags outcome=enriched|not_found|failed, failure_category).
     - `lamuflix.import.count` (Counter).
   - Attributes: `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count`, `error.type`.
2. **ADR-0008**:
   - Write `docs/adr/ADR-0008.md`: Polling for enrichment status in P1; SignalR deferred.

### Acceptance Criteria
- Metrics and traces register with consistent naming and dimensions.
- ADR-0008 accepted."""
})

# -------------------------------------------------------------
# Epic 6 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-6.1",
    "idReadable": "DEV-20",
    "epicKey": "epic-6",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Scaffold web/ frontend with Vite, React, TypeScript strict, Vitest, RTL, and MSW",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Initialize the `web/` frontend directory with Vite, React 19, strict TypeScript, TailwindCSS, and testing setup.

### Scope & Technical Design
1. **Frontend Toolchain**:
   - Initialize `web/` with Vite and React + TypeScript template.
   - Enable TypeScript strict mode.
   - Configure ESLint, Prettier, TailwindCSS.
2. **Testing Setup**:
   - Configure Vitest, `@testing-library/react`, `jsdom`, and Mock Service Worker (MSW).
3. **CI Integration**:
   - Add npm scripts: `build`, `lint`, `test`.
   - Integrate frontend verification into `.github/workflows/ci.yml`.

### Acceptance Criteria
- `npm run build` and `npm test` execute cleanly in local environment and CI."""
})

tasks.append({
    "key": "task-6.2",
    "epicKey": "epic-6",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Generate TypeScript API client from committed openapi.json and configure MSW handlers",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Generate typed API client from `web/src/api/openapi.json` and configure MSW mock handlers.

### Scope & Technical Design
1. **Generated Client**:
   - Use `openapi-typescript` and `openapi-fetch` to generate strongly-typed API client in `web/src/api/client.ts`.
   - Fully typed request parameters, headers, and responses.
2. **MSW Handlers**:
   - Implement mock request handlers in `web/src/mocks/handlers.ts` using the generated types.
   - Supports frontend development and testing without requiring backend services running.

### Acceptance Criteria
- Generated client provides full TypeScript autocompletion for all endpoints.
- MSW accurately mocks backend API in Vitest tests."""
})

tasks.append({
    "key": "task-6.3",
    "epicKey": "epic-6",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Library browse view with movie grid, pagination, and URL query parameter sync",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement the main Library browse view displaying a responsive movie grid with URL-synced pagination and filters.

### Scope & Technical Design
1. **Movie Grid**:
   - Responsive poster grid showing movie poster, title, year, runtime, and IMDb rating badge.
   - Display `EnrichmentStatus` chip (Pending, Enriched, NotFound, Failed).
2. **URL Synchronization**:
   - Sync all filter values and page numbers to URL query parameters (`?text=...&genres=...&sort=Year&direction=Desc&page=1`).
   - Browser back/forward navigation and bookmarked URLs restore filter state.
3. **TanStack Query**:
   - Data fetching via TanStack Query with loading skeletons and error boundaries.
4. **Tests**:
   - Vitest + RTL tests for grid rendering, pagination controls, and URL synchronization.

### Acceptance Criteria
- Navigating to a URL with query params sets up corresponding filters.
- Grid renders smoothly with responsive layout."""
})

tasks.append({
    "key": "task-6.4",
    "epicKey": "epic-6",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Filter Panel (text search, genre/cast multi-select, year/runtime ranges, status)",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement the rich interactive Filter Panel component—the signature UI showcase of LamuFlix.

### Scope & Technical Design
1. **Controls**:
   - Debounced free-text search input.
   - Genre multi-select dropdown with count badges.
   - Cast multi-select dropdown.
   - Runtime range slider (min/max minutes) with "Include unknown runtime" checkbox.
   - Release year range slider.
   - Sort dropdown: Title, Release Year, IMDb Rating, Runtime (Asc / Desc).
   - Enrichment status quick-filter chips (one click to view NotFound/Failed movies).
2. **Tests**:
   - RTL tests covering slider interactions, multi-select toggles, and debounced text search.

### Acceptance Criteria
- Changing any filter updates the URL query string and triggers data refetch.
- Fast, fluid user interaction without layout shifting."""
})

tasks.append({
    "key": "task-6.5",
    "epicKey": "epic-6",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Movie Details view with metadata display and feature-gated Play button",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement the detailed movie view displaying complete metadata, ratings, and local playback controls.

### Scope & Technical Design
1. **Details Display**:
   - Poster, high-res backdrop, title, tagline, plot summary.
   - Release year, runtime, director, writers, full cast.
   - Ratings badges: IMDb score, Rotten Tomatoes percentage, Metacritic Metascore.
2. **LocalPlay Action**:
   - "Play Movie" button triggering `POST /api/movies/{id}/play`.
   - If API returns 403 Forbidden (LocalPlay disabled in portfolio mode), display an informative modal explaining local playback vs hosted demo mode.
3. **Tests**:
   - RTL tests covering details rendering and playback error handling.

### Acceptance Criteria
- Details page renders complete metadata gracefully.
- Play button handles both success (204) and disabled (403) states cleanly."""
})

tasks.append({
    "key": "task-6.6",
    "idReadable": "DEV-80",
    "epicKey": "epic-6",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement folder import dialog and TanStack Query smart polling for Pending rows",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement the folder import modal dialog and smart polling for enrichment progress.

### Scope & Technical Design
1. **Import Modal Dialog**:
   - Text input for folder path (e.g. `F:\\Filmes\\Inception (2010)`).
   - "Import" button triggering `POST /api/movies/import`.
   - On success (202), close modal and prepend new movie row with `Pending` status chip.
2. **TanStack Query Smart Polling**:
   - Configure `refetchInterval` to poll every 2 seconds ONLY when at least one movie in current view has `EnrichmentStatus.Pending`.
   - Polling automatically stops when all rows reach terminal states (`Enriched`, `NotFound`, `Failed`).
3. **One-Click Retry**:
   - `NotFound` and `Failed` chips display a retry icon calling `POST /api/movies/{id}/enrichment` to restart enrichment.

### Acceptance Criteria
- Newly imported movies show instant visual feedback.
- Polling runs only while pending work exists and stops when queue clears."""
})

tasks.append({
    "key": "task-6.7",
    "epicKey": "epic-6",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Watchlist toggle on cards and dedicated Watchlist filter view",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement watchlist bookmarking functionality on movie cards and details view.

### Scope & Technical Design
1. **Bookmark Toggle**:
   - Bookmark icon button on movie cards and details hero.
   - Optimistic UI toggle calling `POST` or `DELETE` on `/api/movies/{id}/watchlist`.
2. **Watchlist Quick Filter**:
   - Toggle button in header/filter panel to filter view to watchlist movies only (`?watchlist=true`).

### Acceptance Criteria
- Instant visual feedback on bookmark toggle.
- Correctly filters movie collection to watchlisted titles."""
})

tasks.append({
    "key": "task-6.8",
    "idReadable": "DEV-23",
    "epicKey": "epic-6",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Publish README modernization narrative, C4 architecture diagram, and CI badge",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Author the public README.md showcasing the 2018 (.NET Core 2.1) -> 2026 (.NET 10) modernization journey, C4 diagrams, and active CI badge (Reaches Linkable Bar).

### Scope & Technical Design
1. **Modernization Narrative**:
   - Clear framing of project as a deliberate engineering modernization of an archival 2018 codebase.
   - Highlights: WinForms tray app -> headless BackgroundService, Pomelo MySQL -> PostgreSQL with skip navigations, reflection query strings -> typed MovieQuery, MVC/Razor -> Minimal API + React SPA, sync publish -> quorum queues with retry TTL and DLQ.
2. **C4 Container Diagram**:
   - Architecture diagram showing Web SPA, Minimal API, Background Worker, PostgreSQL, RabbitMQ, and OMDb.
3. **Execution Instructions**:
   - How to run with Docker Compose.
   - How to run the local quality gates (Roslyn analyzers, cyclomatic complexity, InspectCode).
   - Links to accepted ADRs in `docs/adr/`.

### Acceptance Criteria
- Polished, professional README in English.
- Links directly to ADRs and architecture documentation.
- Linkable Bar reached!"""
})

# -------------------------------------------------------------
# Epic 7 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-7.1",
    "idReadable": "DEV-21",
    "epicKey": "epic-7",
    "action": "modify",
    "type": "Task",
    "priority": "Major",
    "summary": "Create optimized multi-stage Dockerfiles for Api, Worker, and Web SPA",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Create production-ready, secure multi-stage Dockerfiles for API, Worker, and Web SPA in `docker/`.

### Scope & Technical Design
1. **`docker/Dockerfile.api`**:
   - Multi-stage build using `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0`.
   - Non-root user execution (`USER app`).
2. **`docker/Dockerfile.worker`**:
   - Multi-stage build for headless worker using `dotnet/runtime:10.0`.
   - Non-root user execution.
3. **`docker/Dockerfile.web`**:
   - Node build stage (`npm run build`).
   - Nginx alpine runtime serving static SPA assets and reverse proxying `/api` requests to `api:8080`.
   - Unprivileged nginx user.

### Acceptance Criteria
- All three Docker images build cleanly with minimal layer sizes and non-root users."""
})

tasks.append({
    "key": "task-7.2",
    "epicKey": "epic-7",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create docker-compose.yml with health checks, dependency ordering, and sample library",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Create root `docker-compose.yml` orchestrating the full application stack with container health checks and sample movie files.

### Scope & Technical Design
1. **Services**:
   - `postgres`: PostgreSQL 17 with healthcheck `pg_isready`.
   - `rabbitmq`: RabbitMQ 4 with healthcheck `rabbitmq-diagnostics -q ping`.
   - `api`: depends on postgres & rabbitmq (`condition: service_healthy`).
   - `worker`: depends on postgres & rabbitmq (`condition: service_healthy`).
   - `web`: depends on api.
2. **Configuration**:
   - `Features:LocalPlay=false` in container environment.
   - Mount sample library folder containing dummy video files for immediate testing.

### Acceptance Criteria
- `docker compose up -d` starts all services in healthy state.
- Web UI accessible at `http://localhost:3000` with functioning API proxy."""
})

tasks.append({
    "key": "task-7.3",
    "epicKey": "epic-7",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create docker-compose.observability.yml with Aspire Dashboard and OTLP collection",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Create `docker-compose.observability.yml` running standalone .NET Aspire Dashboard to visualize distributed traces and metrics.

### Scope & Technical Design
1. **Aspire Dashboard Service**:
   - Image: `mcr.microsoft.com/dotnet/aspire-dashboard:10.0`.
   - Ports: 18888 (UI), 4317 (OTLP gRPC), 4318 (OTLP HTTP).
   - Configure Api and Worker to export telemetry to Aspire Dashboard.
2. **Documentation & Screenshot**:
   - Capture end-to-end trace screenshot showing single trace spanning API -> RabbitMQ -> Worker -> OMDb.
   - Include screenshot in README and documentation.

### Acceptance Criteria
- Aspire Dashboard displays live traces, metrics, and structured logs.
- Trace screenshot added to README."""
})

tasks.append({
    "key": "task-7.4",
    "epicKey": "epic-7",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Configure CI workflows for container build/push, Stryker mutation testing, and property tests",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Extend GitHub Actions CI with mutation testing, property tests, and container image validation.

### Scope & Technical Design
1. **CI Jobs**:
   - Container validation job verifying Dockerfiles build cleanly.
   - Stryker mutation testing job on `LamuFlix.Core` (threshold >= 80), uploading HTML report as CI artifact.
   - Property tests execution via `scripts/run-property-tests.ps1`.

### Acceptance Criteria
- Stryker report generated and uploaded as CI artifact.
- All quality gates pass in CI."""
})

tasks.append({
    "key": "task-7.5",
    "epicKey": "epic-7",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Consolidate all ADRs, finalize C4 architecture diagrams, and verify zero legacy residue",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Perform final documentation review, complete C4 architecture diagrams, and verify complete elimination of legacy technical debt.

### Scope & Technical Design
1. **Documentation Review**:
   - Ensure all 12 ADRs in `docs/adr/` are finalized with status Accepted.
   - Complete C4 Container and Component diagrams in `docs/architecture/`.
   - Update CONTEXT.md.
2. **Sanity Check**:
   - Verify zero hardcoded secrets, machine paths, or Portuguese symbols remain anywhere in git history or files.

### Acceptance Criteria
- All architectural decisions formally documented and accepted.
- Architecture review passed with zero open debt items."""
})

# -------------------------------------------------------------
# Epic 8 Tasks
# -------------------------------------------------------------
tasks.append({
    "key": "task-8.1",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Implement Transactional Outbox pattern for enrichment events replacing sweeper-only mitigation",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement Transactional Outbox pattern in `LamuFlix.Infrastructure` to provide guaranteed at-least-once message dispatch during folder import.

### Scope & Technical Design
1. **Outbox Implementation**:
   - Table `outbox_messages` (Id, OccurredAt, Type, Payload, ProcessedAt).
   - EF Core interceptor saving outbox records in the same transaction as `Movie` creation.
   - Background publisher polling unprocessed outbox messages and publishing to RabbitMQ with publisher confirms.
2. **ADR Update**:
   - Update `docs/adr/ADR-0005.md` documenting transition from sweeper-only to Transactional Outbox.

### Acceptance Criteria
- Guaranteed message publishing even if RabbitMQ broker is temporarily unreachable during import."""
})

tasks.append({
    "key": "task-8.2",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Implement SignalR hub in Api and React client hook replacing HTTP polling",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement real-time push notifications for enrichment status transitions via SignalR, replacing client HTTP polling.

### Scope & Technical Design
1. **Backend**:
   - Add `EnrichmentHub` in `LamuFlix.Api`.
   - Worker broadcasts completion events to hub backplane.
2. **Frontend**:
   - Integrate `@microsoft/signalr` in React SPA.
   - Real-time updates to movie cards without polling.

### Acceptance Criteria
- UI updates instantly upon worker completion without periodic HTTP requests."""
})

tasks.append({
    "key": "task-8.3",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Implement TMDb metadata provider adapter with chained fallback resolution",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement TMDb metadata provider adapter as a secondary source, chained with OMDb for higher enrichment fidelity and rich metadata.

### Scope & Technical Design
1. **TMDb Adapter**:
   - Implement `TmdbMetadataProvider : IMetadataProvider`.
   - Fetches keywords, collections, and high-resolution posters.
2. **Chained Provider**:
   - `ChainedMetadataProvider` attempting primary provider first, falling back to secondary on NotFound or RateLimited.

### Acceptance Criteria
- Enriches movies successfully from TMDb when OMDb fails."""
})

tasks.append({
    "key": "task-8.4",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Create .NET Aspire AppHost project for developer orchestration",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Create a `.NET Aspire AppHost` project (`src/LamuFlix.AppHost/`) for local developer orchestration.

### Scope & Technical Design
1. **AppHost Project**:
   - Add `LamuFlix.AppHost` (.NET 10 Aspire).
   - Orchestrate Postgres, RabbitMQ, Api, Worker, and Vite SPA.
2. **Developer Experience**:
   - Pressing F5 in IDE launches all services with automatic service discovery.

### Acceptance Criteria
- AppHost launches entire stack locally with Aspire dashboard attached."""
})

tasks.append({
    "key": "task-8.5",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Implement bulk media library scanner with bounded concurrency and OMDb quota throttle",
    "estimatedTime": "3d",
    "repository": "lamuflix",
    "description": """### Overview
Implement recursive bulk library scanner with bounded concurrency and external quota throttling.

### Scope & Technical Design
1. **Bulk Scanner**:
   - Walk root directory `LibraryOptions.RootPath` identifying all movie subfolders.
   - Bounded concurrency pipeline (System.Threading.Channels).
2. **Quota Management**:
   - Daily OMDb request counter tracking free-tier usage (~1,000 requests/day).
   - Automatically pauses scanning when approaching daily quota.

### Acceptance Criteria
- Safely imports collections of 500+ movies without hitting external rate limits."""
})

tasks.append({
    "key": "task-8.6",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Implement Playwright end-to-end browser smoke tests and k6 load tests",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement browser automated smoke tests with Playwright and performance load testing with k6.

### Scope & Technical Design
1. **Playwright Tests**:
   - Automated tests exercising browse, filter, and import flows in headless Chromium.
2. **k6 Load Test**:
   - Benchmark script testing `GET /api/movies` under 50 concurrent virtual users.

### Acceptance Criteria
- Playwright tests pass in CI.
- k6 load test demonstrates sub-50ms p95 latency."""
})

tasks.append({
    "key": "task-8.7",
    "epicKey": "epic-8",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Add Rating range, Director facet, and Collection facet filters to API and UI",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Add advanced filter facets (IMDb rating range slider, Director search, Collection grouping) to MovieQuery, API, and React UI.

### Scope & Technical Design
1. **Query Model & EF Extensions**:
   - Add `RatingRange`, `DirectorId`, `CollectionId` to `MovieQuery`.
2. **UI Controls**:
   - Rating range slider with double handles.
   - Director combobox and collection pill filters.

### Acceptance Criteria
- Users can filter library by minimum rating and director."""
})

tasks.append({
    "key": "task-8.8",
    "idReadable": "DEV-17",
    "epicKey": "epic-8",
    "action": "modify",
    "type": "Task",
    "priority": "Minor",
    "summary": "Implement ffmpeg thumbnail generation worker consumer for local movie files",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement asynchronous thumbnail generation using ffmpeg for local video files when remote posters are unavailable.

### Scope & Technical Design
1. **Thumbnail Consumer**:
   - Queue-driven consumer extracting video frames at 10% offset using `ffmpeg`.
   - Store generated web-optimized thumbnails in local media cache.

### Acceptance Criteria
- Automatically generates thumbnails for video files lacking OMDb posters."""
})

# -------------------------------------------------------------
# Epic 9 Tasks (Movie Recommendations)
# -------------------------------------------------------------
# Phase A: Structured Similarity
tasks.append({
    "key": "task-9.1",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create MovieFeatures value object in Core and ingest full plot from OMDb",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Create `MovieFeatures` value object in `src/LamuFlix.Core/Features/Recommendations/` and ingest full plot from OMDb.

### Scope & Technical Design
1. **MovieFeatures Record**:
   - `MovieId MovieId`
   - `ImmutableHashSet<string> Genres`
   - `string? Director`
   - `ImmutableHashSet<string> Writers`
   - `ImmutableHashSet<string> Cast`
   - `int? ReleaseYear`
   - `int? RuntimeMinutes`
   - `decimal? ImdbRating`
   - `int? MetaScore`
   - `string? FullPlot`
2. **OMDb Lookup Update**:
   - Update OMDb lookup to pass `plot=full` and persist full plot text in database.

### Acceptance Criteria
- `MovieFeatures` extracts structured signals accurately from `Movie` and metadata."""
})

tasks.append({
    "key": "task-9.2",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement GenreWeights calculator using library-wide inverse document frequency (IDF)",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `GenreWeights` calculator applying Inverse Document Frequency (IDF) over the library so rare genres weigh more than ubiquitous ones.

### Scope & Technical Design
1. **IDF Calculation**:
   - `IDF(genre) = ln((TotalMovies + 1) / (MoviesWithGenre + 1)) + 1`.
   - Cached in memory; invalidated when catalog changes.
2. **Weighted Overlap**:
   - Weighted Jaccard similarity for genre comparison: shared "Western" or "Film-Noir" produces higher similarity than shared "Drama".

### Acceptance Criteria
- Rare genres produce measurably higher similarity boost than common genres."""
})

tasks.append({
    "key": "task-9.3",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement SimilarityScorer pure function with feature overlap and caller-safe Reason chips",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `SimilarityScorer` pure function computing similarity scores and emitting explainable, caller-safe `Reason` records.

### Scope & Technical Design
1. **Pure Scoring Function**:
   - `Score(MovieFeatures a, MovieFeatures b, SimilarityWeights weights, GenreWeights genreWeights) -> SimilarityResult(double Score, ImmutableList<Reason> Reasons)`
   - Overlap terms: Director, Writers, Cast, Genres (IDF weighted).
   - Penalties: Decaying exponential penalty for year distance and runtime distance.
   - Boost: Small quality boost for IMDb rating and Metascore.
2. **Reason Records**:
   - Caller-safe explanation strings:
     - "Same director: Denis Villeneuve"
     - "Shares Sci-Fi and Thriller"
     - "Features Leonardo DiCaprio"
     - "Similar era (2010s)"

### Acceptance Criteria
- Pure function with zero external side effects.
- Emits caller-safe reasons whenever score > 0."""
})

tasks.append({
    "key": "task-9.4",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Define SimilarityWeights and RecommendationOptions with validation attributes",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Define strongly-typed configuration options for recommendation scoring weights.

### Scope & Technical Design
1. **Options Records**:
   - `SimilarityWeights`: weights for Director, Writers, Cast, Genres, Year Proximity, Runtime Proximity, Rating Boost.
   - `RecommendationOptions`: `int Take = 10`, `int CandidatePoolSize = 50`, `double MmrLambda = 0.7`, blend weights `a`, `b`, `c`, `d`.
2. **Validation**:
   - DataAnnotations ensuring weights are non-negative and validated on startup.

### Acceptance Criteria
- Recommendation weights are fully configurable via appsettings without code changes."""
})

tasks.append({
    "key": "task-9.5",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement GetRecommendationsQuery handler with Phase A candidate provider",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `GetRecommendationsQueryHandler` in `src/LamuFlix.Core/Features/Recommendations/`.

### Scope & Technical Design
1. **Implementation**:
   - Query: `GetRecommendationsQuery(MovieId MovieId, int Take = 10)`.
   - Load candidate movies via `IRecommendationCandidateSource` (Phase A: all enriched movies in library except selected).
   - Score candidates using `SimilarityScorer`.
   - Rank by score descending and take top N.
   - Map to `RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons)`.

### Acceptance Criteria
- Returns top-N ranked recommendations with attached reason chips."""
})

tasks.append({
    "key": "task-9.6",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement GET /api/movies/{id}/recommendations endpoint with OpenAPI documentation",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `GET /api/movies/{id}/recommendations` Minimal API endpoint and update OpenAPI documentation.

### Scope & Technical Design
1. **Endpoint**:
   - `GET /api/movies/{id}/recommendations?take=10`.
   - Return 404 if movie not found.
   - Return empty list if movie is not yet enriched (`Pending`).
   - Return `Results<Ok<List<RecommendedMovieDto>>, NotFound>`.
2. **OpenAPI**:
   - Update committed `web/src/api/openapi.json` and regenerate TypeScript client.

### Acceptance Criteria
- Endpoint returns recommendations with reasons in JSON format.
- OpenAPI schema updated without drift."""
})

tasks.append({
    "key": "task-9.7",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Create FsCheck property-based tests and golden library tests for SimilarityScorer",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Build property-based test suite and golden fixture tests verifying mathematical correctness of `SimilarityScorer`.

### Scope & Technical Design
1. **FsCheck Properties**:
   - Self-similarity: `Score(a, a) == 1.0`.
   - Symmetry: `Score(a, b) == Score(b, a)`.
   - Boundedness: `0.0 <= Score(a, b) <= 1.0`.
   - Non-self recommendation: Selected movie is never recommended to itself.
   - Reasons: Non-empty reasons whenever score > 0.
2. **Golden Test Suite**:
   - Verify top-3 recommendations against a 30-movie curated golden library.

### Acceptance Criteria
- All property tests pass across 1000 generated iterations.
- Golden fixtures produce expected high-affinity recommendations."""
})

tasks.append({
    "key": "task-9.8",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement RelatedMovies component on Movie Details page with reason chips and MSW mocks",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Build the `RelatedMovies` UI component on the Movie Details page in React SPA.

### Scope & Technical Design
1. **Component Design**:
   - Horizontal scrolling carousel or grid of recommended movie cards.
   - Render clickable reason badges under posters (e.g. "Same director", "Shares Sci-Fi").
   - Clicking a recommendation navigates to that movie's details view.
2. **Tests**:
   - MSW handler mocking recommendation endpoint.
   - Vitest/RTL tests verifying empty, loading, and populated states.

### Acceptance Criteria
- Related movies display with clear, explainable reason chips.
- Handles empty recommendation states gracefully."""
})

tasks.append({
    "key": "task-9.9",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Author ADR-0011: Content-based hybrid recommendations constrained to library",
    "estimatedTime": "4h",
    "repository": "lamuflix",
    "description": """### Overview
Document architectural decision for content-based recommendations in `docs/adr/ADR-0011.md`.

### Scope & Technical Design
1. **ADR-0011 Content**:
   - Context: Single-user local film library.
   - Decision: Content-based hybrid over collaborative filtering (no cross-user signal). Only movies physically on disk are ever recommended. Explainable reasons required on every recommendation.
   - Consequences: Zero external user data leakage, completely private, works offline.

### Acceptance Criteria
- `docs/adr/ADR-0011.md` written and accepted."""
})

# Phase B: Plot Embeddings
tasks.append({
    "key": "task-9.10",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement IEmbeddingGenerator port, OllamaSharp infrastructure adapter, and Fake generator",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Define `IEmbeddingGenerator` port in Core and implement OllamaSharp adapter and deterministic Fake generator.

### Scope & Technical Design
1. **Port**:
   - Use `Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>>`.
2. **Infrastructure Adapter**:
   - Implement adapter using `OllamaSharp` targeting local Ollama container (`all-minilm` or `nomic-embed-text`).
3. **Fake Generator (`FakeEmbeddingGenerator`)**:
   - In `LamuFlix.Tests.Common`, implement deterministic hash-based generator for unit and integration testing without downloading LLM models.

### Acceptance Criteria
- OllamaSharp generates real vector embeddings from plot text.
- Fake generator provides deterministic vectors for fast testing."""
})

tasks.append({
    "key": "task-9.11",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Add pgvector extension, movie_features entity, vector column, and HNSW cosine index",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Enable `pgvector` in PostgreSQL and create EF Core migration for `movie_features` table with HNSW index.

### Scope & Technical Design
1. **pgvector Integration**:
   - Add `Pgvector.EntityFrameworkCore` package.
   - Entity `MovieFeaturesRecord`:
     - `MovieId` (PK, FK to movies)
     - `Vector` column `vector(384)`
     - `DateTimeOffset ComputedAt`
2. **HNSW Index**:
   - Configure HNSW index using cosine distance (`vector_cosine_ops`) for sub-millisecond nearest neighbor search.

### Acceptance Criteria
- Migration executes successfully on Postgres with pgvector extension.
- HNSW index created on vector column."""
})

tasks.append({
    "key": "task-9.12",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement FeaturesRequested worker handler for asynchronous plot embedding",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement background embedding computation in `LamuFlix.Worker` following movie metadata enrichment.

### Scope & Technical Design
1. **Flow**:
   - Upon successful metadata enrichment, worker publishes `FeaturesRequested(MovieId, Attempt)`.
   - Consumer claims task, generates plot embedding via `IEmbeddingGenerator`, and persists to `movie_features`.
   - Reuses claim lease, retry queue, DLQ, and sweeper mechanisms.

### Acceptance Criteria
- Full movie plot is automatically embedded and stored in pgvector asynchronously."""
})

tasks.append({
    "key": "task-9.13",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Postgres pgvector candidate source with cosine distance nearest-neighbor query",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `IRecommendationCandidateSource` querying top semantic neighbors via pgvector cosine distance `<=>`.

### Scope & Technical Design
1. **pgvector Query**:
   - Query top `CandidatePoolSize` (default 50) using EF Core pgvector cosine distance operator `<=>`.
2. **Re-Ranking**:
   - Core blends structured score with plot cosine similarity.
   - Emits "Similar story" reason when plot similarity exceeds threshold.

### Acceptance Criteria
- Returns semantically related movie candidates based on plot text similarity."""
})

tasks.append({
    "key": "task-9.14",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Add Ollama service to docker-compose and create isolated Testcontainers Ollama test",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Add `ollama` container to `docker-compose.yml` and create an isolated Testcontainers integration test.

### Scope & Technical Design
1. **Docker Compose**:
   - Service `ollama` with startup script pulling embedding model.
2. **Isolated Test**:
   - Testcontainers Ollama test in separate category so heavy model downloads do not slow standard CI runs.

### Acceptance Criteria
- Docker compose starts Ollama and serves embedding requests.
- CI maintains fast execution by default."""
})

tasks.append({
    "key": "task-9.15",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Author ADR-0012: pgvector with local Ollama embeddings vs hosted vector APIs",
    "estimatedTime": "4h",
    "repository": "lamuflix",
    "description": """### Overview
Document architectural decision for local pgvector + Ollama embeddings in `docs/adr/ADR-0012.md`.

### Scope & Technical Design
1. **ADR-0012 Content**:
   - Context: Storing and querying vector embeddings for movie plots.
   - Decision: pgvector in same PostgreSQL instance + local Ollama container vs external hosted vector DB (Pinecone, Qdrant Cloud, OpenAI).
   - Consequences: Zero ongoing API cost, fully functional offline, deterministic testing with fake generator.

### Acceptance Criteria
- `docs/adr/ADR-0012.md` written and accepted."""
})

# Phase C: Watch History & Taste Profile
tasks.append({
    "key": "task-9.16",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement WatchHistory domain model, PlaybackEvent recording, and database migration",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement `WatchHistory` domain model and record `PlaybackEvent` upon movie playback.

### Scope & Technical Design
1. **Domain Model**:
   - `src/LamuFlix.Core/Features/WatchHistory/` with `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)`.
   - Update `PlayMovieCommandHandler` to record event via `IPlaybackHistory`.
2. **EF Migration**:
   - Create migration adding `playback_events` table (Id, MovieId, PlayedAt).

### Acceptance Criteria
- Launching movie playback automatically persists playback event timestamp."""
})

tasks.append({
    "key": "task-9.17",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement POST/DELETE endpoints for /api/movies/{id}/watched and /not-interested",
    "estimatedTime": "1d",
    "repository": "lamuflix",
    "description": """### Overview
Implement explicit user feedback endpoints for marking movies as Watched or Not Interested.

### Scope & Technical Design
1. **Endpoints**:
   - `POST` / `DELETE` on `/api/movies/{id}/watched`.
   - `POST` / `DELETE` on `/api/movies/{id}/not-interested`.
   - Persist to `movie_feedback` table.
   - Return 204 No Content (or 404 if movie does not exist).

### Acceptance Criteria
- Users can explicitly mark movies as Watched or Not Interested."""
})

tasks.append({
    "key": "task-9.18",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement dynamic TasteProfile computation from watch history using HybridCache",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Construct dynamic `TasteProfile` representing recency-weighted user preferences, cached via .NET 10 `HybridCache`.

### Scope & Technical Design
1. **Computation**:
   - Recency-weighted average of watched movies' genre weights and plot embeddings.
   - Recent watches receive exponential decay weight (`e^(-lambda * days)`).
   - Cold start: Returns empty profile if no watch history exists.
2. **Caching**:
   - Cache in .NET 10 `HybridCache`, invalidated on new playback event or feedback action.

### Acceptance Criteria
- Adapts dynamically to user watch history; cold start handled gracefully."""
})

tasks.append({
    "key": "task-9.19",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement multi-factor blend scoring and Maximal Marginal Relevance diversification",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Apply multi-factor blend formula and Maximal Marginal Relevance (MMR) to diversify recommendations.

### Scope & Technical Design
1. **Blend Formula**:
   - `Score = a * sim(item, selected) + b * sim(item, profile) - c * alreadyWatched - d * notInterested`.
2. **MMR Diversification**:
   - Iteratively select items that balance relevance to selected movie with dissimilarity to already-selected recommendations.
   - Prevents recommending 5 consecutive sequels in the top results.
3. **Property Tests**:
   - FsCheck test asserting "Not Interested" movies never appear in results.

### Acceptance Criteria
- Recommends diverse titles rather than near-duplicate sequels."""
})

tasks.append({
    "key": "task-9.20",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement Watched and Not Interested actions in UI with dynamic reason chips",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Add feedback action controls to movie cards and display personalized reason chips in React UI.

### Scope & Technical Design
1. **UI Actions**:
   - "Mark Watched" and "Not Interested" buttons on movie cards and recommendation panels.
2. **Dynamic Reason Chips**:
   - Display personalized reason chips (e.g. "Because you watched Heat", "Matches your recent Sci-Fi interest").

### Acceptance Criteria
- User feedback updates UI immediately and refines future recommendations."""
})

tasks.append({
    "key": "task-9.21",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Major",
    "summary": "Implement offline evaluation harness measuring Hit Rate@10 and Mean Reciprocal Rank in CI",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement automated offline recommendation evaluation harness in `tests/LamuFlix.Evaluation/` running non-blocking in CI.

### Scope & Technical Design
1. **Metrics**:
   - Replay historical playback events in chronological sequence.
   - Compute **Hit Rate at 10**: percentage of time user's next watched movie appeared in top 10 recommendations.
   - Compute **Mean Reciprocal Rank (MRR)**.
2. **CI Step**:
   - Non-blocking CI step publishing evaluation metrics as build summary artifact.

### Acceptance Criteria
- Objective evaluation metrics computed automatically without manual testing."""
})

# Stretch in Epic 9
tasks.append({
    "key": "task-9.22",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Implement GET /api/recommendations/for-you and home page personal recommendation rail",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Implement personalized "For You" recommendations rail on the home page driven purely by user taste profile.

### Scope & Technical Design
1. **Endpoint & UI**:
   - `GET /api/recommendations/for-you`.
   - Displays personalized carousel on home page when watch history exists.

### Acceptance Criteria
- Home page offers personalized recommendation carousel tailored to user taste profile."""
})

tasks.append({
    "key": "task-9.23",
    "epicKey": "epic-9",
    "action": "create",
    "type": "Task",
    "priority": "Normal",
    "summary": "Incorporate TMDb keywords, complete cast, and collection signals into MovieFeatures",
    "estimatedTime": "2d",
    "repository": "lamuflix",
    "description": """### Overview
Enrich `MovieFeatures` with TMDb keywords, full cast, and franchise collection signals when TMDb provider is enabled.

### Scope & Technical Design
1. **Signals**:
   - TMDb keywords vector.
   - Franchise collection grouping (same collection boost).

### Acceptance Criteria
- Enhances recommendation accuracy for franchises and sub-genres."""
})

# Compile full plan
plan = {
    "epics": epics,
    "tasks": tasks
}

output_path = os.path.join(os.path.dirname(__file__), "youtrack-plan.json")
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(plan, f, indent=2, ensure_ascii=False)

print(f"Generated {output_path} successfully with {len(epics)} epics and {len(tasks)} tasks.")
