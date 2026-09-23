<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0 (MINOR — comment policy materially expanded)

Amendment 1.1.0 (2026-09-23):
  Modified sections:
    ✏️ Development Workflow → Coding Conventions → Comments: "unless required by best practice"
       replaced with "unless strictly necessary" (non-obvious WHY only); change narration, task
       references, and commented-out code explicitly banned; exemptions widened from AAA headers
       only to AAA headers, empty-block justifications, and scoped warning suppressions
       (aligns with the existing justification requirement in Static-Analysis Gates).
       Wording adapted from FMC.GrantEval constitution ("No gratuitous comments").
    ✏️ Development Workflow → Pull Request Quality Gates: comment checkbox updated to match
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no comment-policy references; no edits required
  Follow-up (upstream, harness-generated — do not edit locally, reinstall overwrites):
    ⚠ AGENTS.md, .claude/agents/test-writer.md, .codex/agents/test-writer.toml,
      .claude/skills/testing/SKILL.md, .agents/skills/testing/SKILL.md,
      .claude/rules/pipeline/coding-conventions.mdc, .cursor/rules/coding-conventions.mdc
      still call AAA headers "the one exception". Fix in dotnet-agent-harness; until then the
      constitution prevails (see Governance).

Ratification 1.0.0 (2026-09-21):

Source documents:
  - docs/architecture/architecture-plan.md (reviewed 2026-09-21 against commit 9c4765c, DEV-16)
  - F:\Dev\Valcre\Valcre.Web.AssetManager\.specify\memory\constitution.md v1.4.0 (structural base)
  - CLAUDE.md and .claude/rules/vendor/aaron-*.md (coding style, testing, SDK/dependency rules)

Added sections:
  ✅ Core Principles I–IX
  ✅ Technology Stack Constraints (mandated + forbidden choices)
  ✅ Development Workflow → Pull Request Quality Gates
  ✅ Development Workflow → Static-Analysis Gates (Roslyn, CA1502, InspectCode)
  ✅ Development Workflow → Coding Conventions
  ✅ Development Workflow → Enrichment Reliability Rules
  ✅ Development Workflow → API and Contract Rules
  ✅ Development Workflow → Documentation Rules
  ✅ Development Workflow → Known Technical Debt (Acknowledged Risks)
  ✅ Governance

Modified sections: N/A (initial version)
Removed sections: N/A (initial version)

Templates reviewed:
  ✅ .specify/templates/plan-template.md      — Constitution Check section applies as-is
  ✅ .specify/templates/spec-template.md      — no constitution-specific edits required
  ✅ .specify/templates/tasks-template.md     — Polish phase tasks added: three gate scripts + format check; CONTEXT.md/ADR update
  ✅ CLAUDE.md                                 — gate commands and MCP routing already documented
  ✅ .claude/rules/pipeline/test-assertions.mdc — project-owned override of the vendored xUnit-Assert guidance (Shouldly, NSubstitute, AutoFixture, Faker.Net); mirrored to .cursor/rules/; deviation recorded in NOTICE
  ✅ scripts/run-roslyn-analyzers.ps1          — gate script present
  ✅ scripts/run-cyclomatic-complexity.ps1     — gate script present (CA1502 threshold 15)
  ✅ scripts/run-jetbrains-inspectcode.ps1     — gate script present

Deferred TODOs:
  - Files this constitution references that do not exist yet and are Epic 1 deliverables:
    global.json, Directory.Packages.props, docs/adr/. (BannedSymbols.txt and CONTEXT.md
    landed on main with DEV-359.) Until they land, the ADR rule is enforced by review.
  - Sections that describe the TARGET layout (src/, tests/, LamuFlix.Core, ...) are binding for new
    code from Epic 1 onward. Until Epic 1 lands, the current LamuFlix.Web / LamuFlix.Data /
    LamuFlix.Work / LamuFlix.Test projects remain in place; see Known Technical Debt.
  - ADR numbering (ADR-0001 … ADR-0012) follows the architecture plan; renumber here if the plan
    is renumbered.
-->

# LamuFlix Constitution

## Core Principles

### I. Ports and Adapters with a Feature-Organised Core (NON-NEGOTIABLE)

The solution is ONE bounded context (**Movies**). It is NOT vertical-slice architecture (slices
do not own their own data access) and it is NOT four-layer Clean Architecture (no separate
Domain assembly). The application core is organised BY FEATURE, and the real seams are the four
external boundaries: **file system**, **metadata provider**, **message queue**, and **media
player process**. Each seam is a port in Core with an adapter in Infrastructure.

Project reference rules are strict and unidirectional:

| Project | May reference |
|---|---|
| `LamuFlix.Core` | BCL, `Microsoft.Extensions.Logging.Abstractions`, `System.Collections.Immutable` only |
| `LamuFlix.Infrastructure` | Core |
| `LamuFlix.Api` | Core, Infrastructure, ServiceDefaults |
| `LamuFlix.Worker` | Core, Infrastructure, ServiceDefaults |
| `LamuFlix.ServiceDefaults` | nothing application-specific |

- Core MUST NOT reference EF Core, Npgsql, RabbitMQ, `System.IO` process APIs, or any HTTP client.
- Feature folders in Core (`Library`, `Import`, `Enrichment`, `Playback`, `Watchlist`,
  `Recommendations`, `WatchHistory`) MUST NOT reference each other; shared concepts live in
  `Domain/`, `Ports/`, or `Pipeline/`.
- Ports live in `Core/Ports/`; the only ports are `IMovieRepository`, `IMovieCatalog`,
  `IMetadataProvider`, `IEnrichmentQueue`, `IMediaLibraryScanner`, `IMediaPlayerLauncher`,
  `IRecommendationCandidateSource`, `IPlaybackHistory`, and the BCL/M.E.AI abstractions
  `TimeProvider` and `IEmbeddingGenerator<string, Embedding<float>>`. Adding a port requires an
  ADR.
- These rules MUST be enforced by `LamuFlix.ArchitectureTests`; a violation fails the build.

**Rationale**: One aggregate with eight use cases is the right size for feature folders, and the
four seams are exactly the things that are slow, non-deterministic, or machine-specific. Naming
them keeps every testability, substitutability, and resilience concern in one place without the
ceremony of extra assemblies. The README MUST describe the style honestly as "ports and adapters,
feature-organised core, explicit handlers with decorators" — not as VSA or Clean Architecture.

### II. Explicit Handlers and Decorators (No MediatR)

Every use case is an `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>`
in its feature folder. Commands and queries MUST be `sealed record`s. Handlers MUST be `sealed`.

Cross-cutting behaviour is applied by hand-wired decorators registered explicitly through a
`services.AddHandler<THandler, TCommand, TResult>()` helper. Decorator order, outer to inner,
MUST be: **Tracing → Logging → Validation → handler**. No other mechanism (MediatR, pipeline
behaviours, reflection scanning, source-generated buses) may dispatch handlers.

Expected domain failures are the three exceptions `NotFoundException`, `ValidationException`,
and `InvalidTransitionException` (plus `FeatureDisabledException` for gated features). They are
mapped to HTTP once, in a single `IExceptionHandler` in `LamuFlix.Api`. A `Result<T>` type is
NOT introduced unless an ADR justifies it.

**Rationale**: Explicit registration makes the call chain readable in one file, keeps decorators
unit-testable without a framework, and matches the Valcre reference implementation. Three
failure kinds plus one mapper is simpler than a Result monad and aligns with the C# style rules.

### III. Filters Are a Typed Query Model (NON-NEGOTIABLE)

Browsing and filtering is the star feature; the filter model MUST be the best-designed code in
the repository. Reflection-based dynamic queries and string-named sort properties are forbidden.

- `MovieQuery` is a `sealed record` with typed members (`Text`, `GenreIds`, `ActorIds`,
  `RuntimeRange`, `YearRange`, `Statuses`, `InWatchlist`, `MovieSort`, `SortDirection`, `Page`).
- Sorting is a `switch` over the `MovieSort` enum — an explicit whitelist. `NULLS LAST` semantics
  MUST be preserved for nullable sort keys.
- Each predicate is one small `IQueryable<Movie>` extension (`WhereText`, `WhereGenres`,
  `WhereRuntime`, …) in Infrastructure; the browse projection goes straight to `MovieSummary`
  with no entity tracking.
- `MovieQuery` MUST be validated by a FluentValidation validator (page size ≤ 100, `Min ≤ Max`,
  at least one sort) executed by the validation decorator.
- The query string is the single source of truth for filter state in the web app; a
  property-based test MUST prove `MovieQuery` round-trips through a query string unchanged.

**Rationale**: Reflection over user-supplied property names is both an injection surface and
an unreviewable design. A typed model gives exact OpenAPI, exhaustive tests, and a UI whose URL
state is provably faithful.

### IV. Enrichment Is an Explicit State Machine (NON-NEGOTIABLE)

`Movie` is a class (EF-tracked, private setters) with intent-revealing transition methods:
`MarkEnriched`, `MarkNotFound`, `MarkFailed`, `RequestEnrichment`, `AddToWatchlist`,
`RemoveFromWatchlist`. Illegal transitions throw `InvalidTransitionException` (HTTP 409).

- `EnrichmentStatus` is `Pending = 0, Enriched = 1, NotFound = 2, Failed = 3`; the numeric values
  are part of the database contract and MUST NOT change.
- Claiming work MUST be atomic: `IMovieRepository.TryClaimForEnrichmentAsync` performs a single
  `UPDATE … WHERE Status = Pending` (`ExecuteUpdateAsync`). Exactly one of two concurrent claims
  wins; an integration test MUST prove it. A claim older than the lease window is free.
- Failures are classified into `EnrichmentFailureCategory` (`ProviderUnavailable`, `RateLimited`,
  `InvalidResponse`, `Unknown`), each with `IsRetryable` and a caller-safe message. Classification
  from the root-cause exception lives in exactly one `EnrichmentFailureClassifier`.
- Retry is category-driven: retryable and attempts < max → republish with `Attempt + 1`
  (`RateLimited` via the TTL retry queue); otherwise `MarkFailed(category)` and dead-letter.
- A `StrandedMovieSweeper` MUST periodically re-enqueue `Pending` rows whose last attempt is
  null or older than the lease, covering lost messages, failed publishes, and claim-then-crash.
- Raw exception text MUST NOT be persisted to the row or returned from the API. It goes to logs
  and traces only.

**Rationale**: Idempotency, bounded retries, and a self-healing sweeper are what make the
asynchronous pipeline trustworthy without a transactional outbox. Separating the caller-safe
category from the exception type keeps the taxonomy stable while implementations change.

### V. Validated Inputs, Consistent Error Responses

All commands, queries, and request contracts MUST be validated through FluentValidation in the
validation decorator. Validation failures MUST return `422 Unprocessable Entity`.

Every error response MUST be an RFC 7807 `ProblemDetails` produced by `AddProblemDetails()` and
one `IExceptionHandler`, and MUST include `traceId`. The exception-to-status mapping lives ONLY
in that handler:

| Exception | Status |
|---|---|
| `ValidationException` | 422 |
| `NotFoundException` | 404 |
| `InvalidTransitionException` | 409 |
| `FeatureDisabledException` | 403 |
| Unhandled | 500 |

Endpoints MUST NOT return `BadRequest(ex.Message)` or otherwise leak exception text.

**Rationale**: One mapping and one error shape keep the React client's error handling trivial
and prevent status-code drift between endpoints.

### VI. Observability Across Processes (NON-NEGOTIABLE)

One trace MUST span API → RabbitMQ → Worker → metadata provider.

- Publishers inject `traceparent`/`tracestate` into message headers via
  `Propagators.DefaultTextMapPropagator`; consumers extract them and start a consumer Activity,
  using an `ActivityLink` on redelivery.
- All span names, metric names, and attribute keys live in `TelemetryConstants`
  (`ActivitySource` "LamuFlix"; spans `Enrichment.Enqueue`, `Enrichment.Process`,
  `Metadata.Lookup`; metrics `lamuflix.enrichment.duration`, `lamuflix.enrichment.outcome`,
  `lamuflix.import.count`; attributes `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count`,
  `error.type`). Ad-hoc string literals for telemetry names are forbidden.
- Logging is structured (Serilog) and every error or processing event MUST carry the movie id
  and, where applicable, the attempt number and failure category.
- Health endpoints MUST exist and be reachable without authentication: `GET /health/live`
  (no dependency checks) and `GET /health/ready` (Postgres, RabbitMQ). Every new external
  dependency MUST register a health check. The worker MUST expose a health signal for compose.
- `ServiceDefaults` owns Serilog, OpenTelemetry (OTLP), health checks, options validation, and
  `TimeProvider` registration; hosts MUST NOT duplicate that wiring.

**Rationale**: A single clickable trace across processes is the most persuasive artefact of the
project and the primary tool for diagnosing the asynchronous pipeline.

### VII. Configuration Isolation and Deterministic Time

Secrets and machine-specific paths MUST NOT appear in source. There are NO hard-coded fallbacks
for connection strings, API keys, library roots, or player paths.

- All configuration is bound to options records (`LibraryOptions`, `PlaybackOptions`,
  `OmdbOptions`, `RabbitMqOptions`, `EnrichmentOptions`, `FeatureOptions`,
  `RecommendationOptions`) with data-annotation validation and `ValidateOnStart()`. Reading
  `IConfiguration` by magic string outside options binding is forbidden.
- Local values live in `dotnet user-secrets` or `appsettings.Development.json` (git-ignored);
  containers receive them as environment variables.
- Player mapping is configuration (`PlaybackOptions.Players[]`), never a database table.
- Local playback is registered only when `Features:LocalPlay` is true; otherwise a
  `DisabledMediaPlayerLauncher` throws `FeatureDisabledException`. Process launches MUST use
  `ProcessStartInfo.ArgumentList` — never a shell or a concatenated command line.
- Timestamps MUST come from an injected `TimeProvider`. `DateTime.Now`, `DateTime.UtcNow`,
  `DateTime.Today`, and `DateTimeOffset.Now` are banned via `BannedSymbols.txt`.

**Rationale**: The current codebase carries hard-coded MySQL credentials, an OMDb key, and
`F:/Filmes`. Validated options and banned symbols make those regressions a build failure.

### VIII. Ubiquitous Language in English

Domain terms are defined in `CONTEXT.md` (term, definition, avoid). Identifiers MUST use those
terms. Portuguese identifiers (`Filme`, `CriarFilme`, `AssistirFilme`, `ExcluirFilme`,
`MinhaLista`, `ProcessarFilme`, …) are forbidden and MUST be renamed on contact.

- **Movie** (not Filme, Film) · **Library** · **Import** (not Criar, Create, Scan) ·
  **Enrichment** (not job, task, processing) · **Enrichment Status** · **Failure Category**
  (not error type, exception) · **Metadata Provider** (never OMDb in Core names) ·
  **Watchlist** (not MinhaLista, My List) · **Playback** (not Assistir, Watch) ·
  **Recommendation** · **Reason** · **Taste Profile** · **Playback Event** · **Feedback**.
- Types, members, and telemetry identifiers MUST be named by functionality, not by vendor
  (`IMetadataProvider` and `Metadata.Lookup`, not `IOmdbClient` and `Omdb.Lookup`). Vendor
  names are permitted only on the Infrastructure adapter class itself and on vendor-mandated
  configuration keys.
- A new domain term MUST be added to `CONTEXT.md` in the same PR that introduces it.

**Rationale**: A single vocabulary shared by code, docs, API, and UI is what makes the bounded
context legible to a reviewer and survives provider or library swaps without renames.

### IX. Test Pyramid with Real Infrastructure (NON-NEGOTIABLE)

| Layer | Project | Tools |
|---|---|---|
| Domain, handlers, filters | `LamuFlix.UnitTests` | xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net |
| Property tests | `LamuFlix.UnitTests` | FsCheck.Xunit |
| Persistence, messaging, API, worker end-to-end | `LamuFlix.IntegrationTests` | Testcontainers (Postgres, RabbitMQ), `WebApplicationFactory`, WireMock.Net, Verify |
| Architecture | `LamuFlix.ArchitectureTests` | NetArchTest.Rules or ArchUnitNET |
| Mutation | `LamuFlix.UnitTests` | Stryker, break threshold 80 on `LamuFlix.Core` |

- xUnit v3 is the only test framework; MSTest and NUnit are forbidden.
- Assertions use Shouldly. xUnit's built-in `Assert` is acceptable only where Shouldly has no
  equivalent (e.g. `Assert.Collection`). FluentAssertions MUST NOT be introduced: versions
  8+ are under a paid commercial licence.
- Test data is built with AutoFixture (anonymous objects, `[AutoData]` theories) and Faker.Net
  (realistic titles, names, years). Hand-written builders are reserved for domain invariants
  that AutoFixture cannot satisfy (e.g. a `Movie` in a specific `EnrichmentStatus`).
- Mocking uses NSubstitute; Moq is forbidden. Infrastructure MUST NOT be mocked — use
  Testcontainers for Postgres and RabbitMQ, WireMock.Net for the metadata provider, and
  `MockFileSystem` (System.IO.Abstractions) for the scanner. EF Core InMemory is forbidden.
- Tests follow Arrange-Act-Assert, use `Theory` + `MemberData` over repeated `Fact`s, use fresh
  data per test, and never call `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`.
- The claim-atomicity, retry-queue, DLQ, sweeper, and single-trace-id end-to-end scenarios
  MUST each have an integration test.

**Rationale**: The plan's guarantees (atomic claim, NULLS LAST, trace propagation) are only
true against the real engine. Mutation testing on Core is what proves the state machine tests
mean something.

## Technology Stack Constraints

The following choices are established and MUST NOT be changed without a constitution amendment
and an ADR.

| Concern | Mandated Choice | Notes |
|---|---|---|
| Runtime | .NET 10 / C# 14, `global.json` pinned, `rollForward: latestPatch` | `Nullable` enable, `TreatWarningsAsErrors` true, central package management |
| Web host | ASP.NET Core Minimal API, `TypedResults`, `Results<…>` unions | No MVC controllers, Razor, or session |
| Database | PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` | One `IEntityTypeConfiguration<T>` per entity; skip navigations; single fresh `Initial` migration |
| Vector store | pgvector via `Pgvector.EntityFrameworkCore` | HNSW index on cosine distance (Recommendations Phase B) |
| Message queue | RabbitMQ via `RabbitMQ.Client` 7.x (`IChannel`, `AsyncEventingBasicConsumer`) | Quorum queues; publisher confirms; topology in one `RabbitMqTopology` class |
| Metadata provider | OMDb behind `IMetadataProvider`, typed `HttpClient` | `Microsoft.Extensions.Http.Resilience`: retry with jitter, `Retry-After`, timeout, circuit breaker |
| Embeddings | `IEmbeddingGenerator<string, Embedding<float>>` (Microsoft.Extensions.AI) | Ollama via OllamaSharp at runtime; deterministic `FakeEmbeddingGenerator` in tests |
| File system | `System.IO.Abstractions` | `MockFileSystem` in unit tests |
| Time | `TimeProvider` | `DateTime.Now` family banned |
| Validation | FluentValidation, executed by the validation decorator | |
| Test tooling | xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net | See Principle IX |
| Category types | SmartEnum-style closed sets (`Ardalis.SmartEnum` or sealed records) | Plain `enum` only for `EnrichmentStatus`, `MovieSort`, `SortDirection` |
| Serialization | `System.Text.Json`, `JsonStringEnumConverter` | Newtonsoft.Json MUST NOT be introduced |
| Object mapping | Mapster | `IRegister` configs live beside the feature they serve; AutoMapper MUST NOT be introduced |
| Logging | Serilog (console JSON; OTLP/Seq optional) | Wired in ServiceDefaults |
| Telemetry | OpenTelemetry traces/metrics/logs, OTLP exporter | Aspire dashboard as local target |
| API docs | `Microsoft.AspNetCore.OpenApi` + Scalar at `/scalar` | Document committed at `web/src/api/openapi.json` |
| Web app | Vite + React + TypeScript strict, TanStack Query, Zod, `openapi-typescript` + `openapi-fetch` | Vitest + React Testing Library + MSW |
| Containers | Multi-stage Dockerfiles, `docker-compose.yml`, `docker-compose.observability.yml` | Health checks and dependency ordering |
| CI | GitHub Actions (`.github/workflows/ci.yml`) | build, test, format, web lint/test/build, OpenAPI drift check |

**Forbidden**: MediatR, AutoMapper, Moq, MSTest, NUnit, FluentAssertions (paid licence; use
Shouldly), Bogus (use Faker.Net),
EF Core InMemory provider, Pomelo/MySQL, Newtonsoft.Json, `System.Reflection`-driven query or
sort building, `cmd.exe`/shell-based process launching.

## Development Workflow

### Pull Request Quality Gates

Every PR MUST satisfy all of the following before merging:

- [ ] Project reference rules respected; `LamuFlix.ArchitectureTests` passes
- [ ] New use cases are sealed record commands/queries with sealed handlers registered via
      `AddHandler<…>`; no MediatR or reflection dispatch introduced
- [ ] New ports have an ADR; adapters live in `LamuFlix.Infrastructure`
- [ ] New filters extend `MovieQuery` and the sort whitelist; no dynamic property access
- [ ] New `Movie` state transitions are validated methods with unit tests for every legal and
      illegal transition
- [ ] New endpoints use `TypedResults`, are covered by `WebApplicationFactory` integration
      tests, and update the committed OpenAPI document (drift check green)
- [ ] All error paths produce `ProblemDetails`; new exception types are mapped in the single
      `IExceptionHandler`
- [ ] New external dependencies register a health check and telemetry names in
      `TelemetryConstants`
- [ ] No secrets, machine paths, or `IConfiguration` magic strings; new options records are
      validated at startup
- [ ] No `DateTime.Now`-family calls; `TimeProvider` injected
- [ ] `CancellationToken` flows through every async path; no sync-over-async
- [ ] New domain terms added to `CONTEXT.md`; no Portuguese identifiers introduced
- [ ] Tests use xUnit v3 + NSubstitute + Shouldly + AutoFixture/Faker.Net;
      infrastructure via Testcontainers, WireMock.Net, or `MockFileSystem`
- [ ] No comments added unless requested or strictly necessary; the only exemptions are AAA
      test headers, empty-block justifications, and scoped warning suppressions (see Coding
      Conventions)
- [ ] New or modified `.cs` files pass all three static-analysis gates (see below)
- [ ] `dotnet format --verify-no-changes` passes

### Static-Analysis Gates

New or materially changed C# code MUST pass all three gates before a task or PR is considered
complete. They are NOT interchangeable; `dotnet build` alone surfaces none of them reliably.

| Gate | Script | Requirement |
|---|---|---|
| Roslyn analyzers (CA/IDE at warning+) | `./scripts/run-roslyn-analyzers.ps1` | Zero warnings |
| Cyclomatic complexity (CA1502) | `./scripts/run-cyclomatic-complexity.ps1` | No method > **15**; refactor gate `-Threshold 6` after implementation works |
| JetBrains InspectCode | `./scripts/run-jetbrains-inspectcode.ps1` | Zero issues at WARNING or higher |

- Scope is new or modified files (diff against `main`) unless `-All` or `-Files` is used.
- A gate whose analyzer is not actually enabled exits 1 with remediation; a skipped gate is
  never a passed gate.
- Complexity failures MUST be fixed by extracting private helpers, early returns, and guard
  clauses — not by suppression. `#pragma warning disable` requires a brief justification.
- Intentional InspectCode violations MUST use a targeted ReSharper suppression with a brief
  justification.

**Rationale**: Each gate catches a different class of problem. Automating them during agent
implementation prevents drift that would otherwise only surface when a developer opens Rider.

### Coding Conventions

Code MUST follow `.claude/rules/vendor/aaron-csharp-coding-style.md` and these project-specific rules:

- **Types**: data types are `sealed record`s; classes are `sealed` unless designed for
  inheritance; identifiers and constrained values are value objects (`MovieId`, `ImdbId`,
  `ImdbRating`, `Runtime`, `ReleaseYear`, `LibraryPath`, `MediaFormat`) with `TryParse`-style
  factories. Collections in records are `System.Collections.Immutable`.
- **Comments**: code MUST NOT carry comments (inline `//`, XML `///`, block) unless the user
  explicitly requests one or it is strictly necessary — a non-obvious WHY the code cannot
  express, such as a hidden constraint or a workaround for a specific external bug. Names carry
  the WHAT. Comments that restate the code, narrate a change, reference a task, ticket, or
  caller, or hold commented-out code MUST be removed. Only three uses are exempt:
  - **Test structure**: `// arrange`, `// act`, `// assert` section headers in test methods.
  - **Empty-block justification**: one comment inside an intentionally empty block (a
    swallowed `catch`, a no-op override, an empty `default:`) stating why it is empty.
  - **Temporary warning suppression**: `#pragma warning disable`/`restore` pairs and
    `// ReSharper disable once …` directives, scoped to the fewest lines possible, with a brief
    justification on the same line.
- **Interface placement**: ports live in `Core/Ports/`, separate from their adapters. Any other
  interface MUST be defined in the same file as its primary implementation.
- **Naming**: functionality-based, English, per `CONTEXT.md`; `nameof` for every symbol
  reference in exceptions, logging, and attributes.
- **Async**: no `async void`; `CancellationToken` flows end to end; `await` over
  `ContinueWith`; never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`;
  `CancellationTokenSource` disposed.
- **Safety**: `Try*` methods over exception-driven parsing; `TryGetValue` over indexers;
  `Uri.TryCreate` over `new Uri`.
- **Dependencies**: constructor injection kept minimal; a handler that needs more than three
  ports signals a missing abstraction.
- **Packages**: added with `dotnet add package`, versions in `Directory.Packages.props`;
  no floating versions; `dotnet list package --vulnerable` clean.

### Enrichment Reliability Rules

- The `EnrichmentRequested` message carries `MovieId` and `Attempt` ONLY. Title, year, and IMDb
  id are read from the row at processing time so a message can never be stale.
- A worker MUST claim (`TryClaimForEnrichmentAsync`) before calling the metadata provider; a
  false claim is acknowledged and ignored.
- The consumer (`EnrichmentConsumer`) is thin: it resolves a scope per message, calls Core
  handlers, and translates outcomes to ack / nack / republish. All decisions live in Core
  handlers so they are unit-tested without RabbitMQ.
- Retry policy is owned by the failure category and `EnrichmentOptions` (max attempts, lease,
  sweep interval). Application code MUST NOT implement ad-hoc retry loops.
- The insert-then-enqueue dual write is mitigated by the sweeper (ADR-0005); a transactional
  outbox is the documented stretch. Silently ignoring a failed publish is forbidden.
- Graceful shutdown: `stoppingToken` flows into handlers; an in-flight message finishes or is
  nacked with requeue on cancellation.
- `FeaturesRequested` (Recommendations) reuses the same claim, retry, DLQ, category, and
  sweeper machinery; it MUST NOT fork a second pipeline.

### API and Contract Rules

- Endpoints are grouped per feature in `MapXxxEndpoints` extension methods. No API versioning
  in P1 (single consumer); introducing versioning requires an ADR.
- Success/error shapes follow the endpoint table in the architecture plan (§6): `202` with a
  `Location` header for import and enrichment requests, `204` for watchlist and play, `403`
  when `LocalPlay` is off.
- Enums are serialized as strings. `Movie` enrichment fields are exposed as
  `EnrichmentStatus`, `EnrichedAt`, `EnrichmentAttempts`, `LastFailureCategory`; never raw error text.
- The OpenAPI document is committed at `web/src/api/openapi.json`, snapshot-tested with Verify,
  and regenerated in CI; drift fails the build. The TypeScript client and MSW handlers are
  generated from that document, never hand-written.
- Enrichment status is delivered by polling in P1 (ADR-0008); SignalR is a stretch item.

### Documentation Rules

- Architectural decisions are recorded as one-page ADRs in `docs/adr/` (status Accepted),
  numbered per the architecture plan (ADR-0001 … ADR-0012). A PR that changes an architectural
  choice MUST add or supersede an ADR.
- `CONTEXT.md` is the ubiquitous-language source of truth in the Valcre format (term,
  definition, avoid).
- The README MUST carry the modernisation narrative, a C4 container diagram, a trace screenshot,
  how to run with compose, how to run the quality gates, and links to the ADRs.
- Documentation lookups use `microsoft-learn` for Microsoft-owned technology and `context7` for
  third-party libraries, never the reverse (see `CLAUDE.md`).

### Known Technical Debt (Acknowledged Risks)

The following are documented and accepted for the current iteration. They MUST NOT be silently
worked around or extended; retiring them follows the epic order in the architecture plan.

- **Transitional layout**: until Epic 1 lands, `LamuFlix.Web`, `LamuFlix.Data`, `LamuFlix.Work`,
  and `LamuFlix.Test` remain. New code in those projects MUST already follow Principles II–IX
  (no new reflection queries, no new `DateTime.Now`, no new Portuguese identifiers, no new
  MSTest tests).
- **Current hard-coded secrets and paths** (MySQL credentials, OMDb key, `F:/Filmes`, player
  paths) are known violations of Principle VII, retired in Epics 1–3. They MUST NOT be copied.
- **Dual write on import** (insert, then enqueue) is mitigated by the sweeper; a transactional
  outbox is Epic 8 (ADR-0005).
- **Polling** for enrichment status instead of push (ADR-0008).
- **OMDb free tier** (1,000 requests/day): bulk cutover is slow by design; `RateLimited`
  retries MUST be delayed, never tight loops.
- **Single user**: no cross-user signal, so collaborative filtering is excluded (ADR-0011).
- **Embedding model download** (hundreds of MB) slows first compose start; tests default to the
  fake generator and the single Ollama Testcontainers test sits in its own category.
- **Windows daily-driver host** runs the API as a process while Postgres and RabbitMQ run in
  containers; `LocalPlay` is off in compose.

## Governance

This constitution is the authoritative, non-negotiable rulebook for LamuFlix. It supersedes
oral agreements, inline comments, and informal conventions. When in conflict with any other
document except the locked product plan (`F:\Dev\lamuflix-react-pivot-plan.md`, which owns
WHAT is built; this constitution owns HOW), the constitution prevails.

**Amendment procedure**:
1. Submit a written proposal describing the change, rationale, and impact on existing code.
2. Record the decision as an ADR in `docs/adr/` when it changes an architectural choice.
3. Provide a migration plan if existing code must be updated to comply.
4. Apply the appropriate version bump, update the Sync Impact Report and `Last Amended`, and
   review `.specify/templates/*` for required follow-up before merging.

**Version semantics**:
- MAJOR — a principle is removed, redefined, or made backward-incompatible with existing code
- MINOR — a new principle or section is added, or existing guidance is materially expanded
- PATCH — wording clarification, typo fix, or non-semantic refinement

**Compliance**: All PRs and code reviews MUST verify compliance with this constitution. The
`/speckit.plan` Constitution Check MUST cite the principle number for every gate it evaluates.
Non-compliant code MUST be blocked from merging unless an amendment is simultaneously submitted
and approved.

**Version**: 1.1.0 | **Ratified**: 2026-09-21 | **Last Amended**: 2026-09-23
