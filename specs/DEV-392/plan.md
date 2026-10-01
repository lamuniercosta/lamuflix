# Implementation Plan: Production Persistence Registration

**Branch**: `feature/392-spec` | **Date**: 2026-10-01 | **Spec**: specs/DEV-392/spec.md

**Input**: `spec.md`, `brief.md`, `CONCLUSIONS.md` (Q1-Q8), recon-DEV-392, D1 (Keel, 2026-10-01)

## Summary

One new registration extension in `LamuFlix.Infrastructure/Persistence/` that binds the host's PostgreSQL
connection string to `LamuFlixDbContext`, registers `IMovieRepository` → `EfMovieRepository` scoped,
supplies a clock, and adds the positive-`ClaimLease` startup validator — plus one added line in the Api
composition root that calls it before DEV-18's future activation guard. Proven by unit tests on a plain
service collection and by one real round trip against the Postgres container.

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Dependencies**: none new. EF Core 10.0.12 and Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 are already in `Directory.Packages.props` and already referenced by `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj:17-20`. D1 adds a **project** reference, not a package.
**Storage**: PostgreSQL via the existing `PostgresFixture` (`postgres:16.4`); migration `20260930153520_Initial` unchanged.
**Testing**: xUnit v3 + Shouldly, in the existing `LamuFlix.UnitTests` and `LamuFlix.IntegrationTests`; no mocks of database behaviour, no new test framework.
**Constraints**: Core stays persistence-independent and is not touched; `Features:LocalPlay` and `Process.Start` untouched; no new project, folder, layer or abstraction.

## Constitution Check

- **New dependency**: none. §2.3 item 1 not triggered — D1 adds a `ProjectReference` to a project the test suite already references elsewhere (`tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj:33`); `Directory.Packages.props` is untouched.
- **New project / folder / layer**: none. One new file inside the existing `src/LamuFlix.Infrastructure/Persistence/` folder, beside the DEV-301 repository. §2.3 item 2 not triggered.
- **Schema change**: none. `20260930153520_Initial` is the only migration and is unchanged.
- **API shape**: one new **non-HTTP** public method on a shipped assembly (§2.3 item 4). Patron approves its surface (Q1, CONCLUSIONS.md:5). No route, DTO field or status code changes, so the OpenAPI → TypeScript contract chain is untouched.
- **Security & local execution**: `Features:LocalPlay` untouched; `Process.Start` untouched. **Secrets are touched** (§2.3 item 5): the registration reads `ConnectionStrings:DefaultConnection` at composition time, throws with the existing host wording that names user secrets and `ConnectionStrings__DefaultConnection`, and never echoes or commits the value. Handled by the established pattern (`src/LamuFlix.Worker/Program.cs:24-28`).
- **File scope**: only the frozen-scope files in `brief.md` plus the D1 csproj line. No file is deleted or rewritten that the ticket does not name; `Program.cs` is named by the ticket.
- **ADR**: none (Q7). Constitution:477-479 requires an ADR only when an architectural choice changes; this adds a registration method in an existing assembly and changes no choice.
- **Status**: PASS, no departures. One owner checkbox is open (Q6, FR-013) and Gate 1 stays closed until it is answered.

## Project Structure

```text
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs  (new: AddLamuFlixPersistence)
src/LamuFlix.Api/Program.cs                                                       (edit: one added line, after AddServiceDefaults, before Build)
tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs (new: guards + descriptors, no container)
tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs                   (new: PostgresFixture round trip)
tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj                  (edit: one ProjectReference line to ServiceDefaults, D1)
specs/DEV-392/*                                                                   (spec artifacts)
```

Not touched: `Directory.Packages.props`, any migration or model snapshot, any `appsettings*.json`,
any Core file, `EfMovieRepository`, `LamuFlixDbContext`, `EnrichmentOptions`, `IMovieRepository`,
`src/LamuFlix.ServiceDefaults/Extensions.cs`, `LamuFlix.Tests.Common`, the legacy Worker and Web hosts.

## Design

1. **Signature and shape (Q1)**. `public static IServiceCollection AddLamuFlixPersistence(this IServiceCollection services, IConfiguration configuration)` in `LamuFlix.Infrastructure.Persistence`. `ArgumentNullException.ThrowIfNull` on both arguments, matching `src/LamuFlix.Infrastructure/Pipeline/ServiceCollectionExtensions.cs:17` and `src/LamuFlix.ServiceDefaults/Extensions.cs:18`. Returns `services` so the Api can chain if it ever needs to.

2. **Connection string guard (Q2)**. `configuration.GetConnectionString("DefaultConnection")`; null or whitespace throws `InvalidOperationException` reusing the wording at `src/LamuFlix.Worker/Program.cs:25-27` — it names user secrets and the `ConnectionStrings__DefaultConnection` environment variable and does not interpolate the value. This is the only read of `IConfiguration` in the method; nothing else in the codebase reads `ConnectionStrings` by magic string (constitution:246-248 binds everything through options, and this value has no options record).

3. **DbContext (Q1)**. `services.AddDbContext<LamuFlixDbContext>(o => o.UseNpgsql(connectionString))` at the default scoped lifetime. No `AddDbContextPool` and no `EnableRetryOnFailure`. `LamuFlixDbContext` has no `OnConfiguring`, so this is the only place the provider is supplied (recon:30).

4. **Clock (Q3)**. `services.TryAddSingleton(TimeProvider.System)`. `TryAdd` is what makes a test clock registered *before* the call survive; nothing in `src` registers a `TimeProvider` today (recon:94), and `EfMovieRepository` requires one (recon:23).

5. **Lease validator (Q4)**. `services.AddOptions<EnrichmentOptions>().Validate(o => o.ClaimLease > TimeSpan.Zero, "Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered").ValidateOnStart()`. The section is **not** re-bound — `AddLamuFlixOptions` already binds `EnrichmentOptions` from `Enrichment` with `ValidateDataAnnotations().ValidateOnStart()` (`src/LamuFlix.ServiceDefaults/Extensions.cs:24,32-35`), and `AddOptions` composes with that binding. No default is invented, the options class is untouched, the repository's own constructor guard stays, and the active-path retry-TTL comparison stays with DEV-18 (`specs/DEV-18/spec.md:174`).

6. **Repository (Q1, Q6)**. `services.AddScoped<IMovieRepository, EfMovieRepository>()`. DEV-301's type is reused unchanged; `IMovieRepository` is the key DEV-18's guard inspects (`specs/DEV-18/plan.md:197`).

7. **Api call site (Q1, Scope 2)**. `builder.Services.AddLamuFlixPersistence(builder.Configuration);` inserted between `AddServiceDefaults()` and `AddProblemDetails()`. Nothing else changes: no service-provider override, so Development keeps the default `ValidateOnBuild` and `ValidateScopes` (Scope bullet 5). Placement before `Build()` is what guarantees the descriptors exist before any later DEV-18 guard call.

### Known consequence, accepted

The Api has no `appsettings.json` (DEV-18 owns it, `specs/DEV-18/plan.md:200`) and `EnrichmentOptions.ClaimLease` has no default (`src/LamuFlix.Core/Options/EnrichmentOptions.cs:17`). With step 5 in place, a Development Api run needs a positive `Enrichment:ClaimLease` from user secrets or `Enrichment__ClaimLease`. That is Q4's intent — a registered production repository carries its own positive-lease requirement — and a host that does not call this method gains no lease requirement. No test in the repository boots the Api host (recon:70), so nothing existing breaks; it is recorded as an operator note in the PR body.

## Test Strategy

- **Unit** (`tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs`, no container, no Docker). Placeholder connection string as in `EfMovieRepositoryConstructorTests.cs:15-18`, which is never opened. Composes `AddSingleton<IConfiguration>(configuration)` — the in-memory `ConfigurationBuilder` result, so `AddLamuFlixOptions`' `BindConfiguration` resolves `IConfiguration` from DI as Q5 requires — then `AddLamuFlixOptions()` + `AddLamuFlixPersistence(configuration)` on a plain `ServiceCollection`, mirroring `Program.cs` (D1). Asserts, per FR-007: the missing and whitespace-only connection-string failures with their message content and the no-echo check (the whitespace value is a multi-character, non-space-only string such as `" \t "`, so containment is a meaningful assertion); `ClaimLease` missing / `00:00:00` / negative, each failing through the registered `IStartupValidator.Validate()` with `OptionsValidationException` and the exact FR-002(4) message in `Failures` (asserting on `Failures`, not only `Message`, so a co-failing section cannot dilute the assertion); the three descriptors (`IMovieRepository`, `LamuFlixDbContext`, `TimeProvider`); and a pre-registered `TimeProvider` surviving the `TryAdd`. A valid configuration builds with `ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`.
- **Integration** (`tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs`, `IClassFixture<PostgresFixture>`). Creates and migrates one database with `fixture.CreateContext()` + `Database.MigrateAsync` (`PersistenceRoundTripTests.cs:162-167`), takes its connection string with `context.Database.GetConnectionString()` — the sibling pattern at `PersistenceRoundTripTests.cs:169-174` — and feeds it to the in-memory configuration. The container's connection string exists only in the test process; it is never written to a file. The container's default connection string is not used directly. Registers `AddSingleton<IConfiguration>(configuration)` so `BindConfiguration` resolves `IConfiguration` from DI, then composes `AddLamuFlixOptions()` + `AddLamuFlixPersistence(configuration)` as `Program.cs` does, builds with both validation switches, then asserts in a scope that `IMovieRepository` resolves as `EfMovieRepository` and that `LamuFlixDbContext` is one instance per scope, and completes one round trip through the resolved repository (`NextIdentityAsync`, `AddAsync`, `SaveChangesAsync`, `GetAsync`), reusing the existing test shape in `EfMovieRepositoryTests.cs`. `IStartupValidator.Validate()` passes with the valid configuration. No database-driver mocks (Q5, AC4).
- **Minimum valid in-memory configuration (D1, required because `AddLamuFlixOptions` validates every section)** — from `src/LamuFlix.Core/Options/`:

  | Key | Annotation | Test value |
  |---|---|---|
  | `ConnectionStrings:DefaultConnection` | none (read by the registration) | unit: non-credential placeholder; integration: container's runtime string |
  | `Library:RootPath` | `[Required]` (`LibraryOptions.cs:11-12`) | `C:/library` |
  | `Omdb:ApiKey` | `[Required]` (`OmdbOptions.cs:11-12`) | `not-a-real-key` |
  | `Omdb:BaseUrl` | `[Required][Url]` (`OmdbOptions.cs:14-16`) | `https://example.invalid/` |
  | `RabbitMq:HostName` | `[Required]` (`RabbitMqOptions.cs:11-12`) | `localhost` |
  | `Enrichment:ClaimLease` | none; Q4 validator | `00:05:00` |

  No key is needed for `Enrichment:MaxAttempts` (`[Range(1, int.MaxValue)]`, default 3), `RabbitMq:Port`, `Playback:Players` (default `[]`, nested members are not validated by `ValidateDataAnnotations`) or `Features:LocalPlay`. The lease cases drive `Enrichment:ClaimLease` to absent, `00:00:00` and a negative value while the five other keys stay valid, so the only failure observed is the DEV-392 rule.
- **Not tested here** (Q6 pending, FR-013): DEV-18's consumer active/inactive composition paths. The `Program.cs` call and its position are a review item for Compass/Ledger, not a test.

## Gates

After each `.cs` edit: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (threshold 15), `./scripts/run-jetbrains-inspectcode.ps1`, and `dotnet format --verify-no-changes`. Refactor pass: complexity threshold 6. Close-out: `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1` (unchanged — no new packages), then the full `dotnet test` (Docker required). Mutation covers the Infrastructure extension: survivors on the blank-connection-string guard and the `ClaimLease > TimeSpan.Zero` predicate must be killed by the unit tests above; if the mutation gate cannot run, report "Could not run" with the script output and return to Keel. Zero new suppressions. Never lower a threshold.
