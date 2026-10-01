# Feature Specification: Production Persistence Registration in the Api Composition Root

**Feature Branch**: `feature/392-spec`

**Created**: 2026-10-01

**Status**: gate1: provisional — Gate 1 stays closed until the owner answers Q6 (FR-013)

**Input**: Phase A grill outcome (DEV-392, parent DEV-282, size M, no UI); `brief.md`, `CONCLUSIONS.md` (Q1-Q8), recon-DEV-392, `specs/PRODUCT.md`; D1 (Keel, 2026-10-01, answering Quill's `needs decision` on IntegrationTests references)

## User Scenarios & Testing

### User Story 1 - The Api host resolves the production repository and its DbContext (Priority: P1)

As the enrichment pipeline, I need the built Api composition root to resolve `IMovieRepository` to the
production `EfMovieRepository` and to resolve its `LamuFlixDbContext` inside a scope, so DEV-18 can
later activate consumption against real persistence without a second repository implementation.

**Why this priority**: This is the whole delivery (Scope bullet 1, AC1). Nothing else in the ticket
matters until this works.

**Independent Test**: Against the `PostgresFixture` container, a `ServiceCollection` composed the way
`Program.cs` composes it produces a provider that, with Development DI validation on, resolves
`IMovieRepository` as an `EfMovieRepository` and resolves a `LamuFlixDbContext` that is the same
instance within one scope and a different instance in the next.

**Acceptance Scenarios**:

1. **Given** the Api composition root with persistence registered, **When** the container is built with Development's default DI validation, **Then** it builds without error and `IMovieRepository` resolves to `EfMovieRepository`, and `LamuFlixDbContext` resolves within a scope.
2. **Given** one open scope, **When** `LamuFlixDbContext` is resolved twice, **Then** it is the same instance; **when** it is resolved from a second scope, **Then** it is a different instance.
3. **Given** the composed Api services, **When** DEV-18's activation guard inspects the registrations, **Then** the repository and DbContext descriptors are already present, because the persistence registration is placed before any later DEV-18 guard call (review-verified, Q5; not a text test).
4. **Given** the registration, **When** it is composed, **Then** the registrations appear in this order: DbContext, clock, options validator, repository — and the repository lifetime is scoped (AC1) (review-verified, like scenario 3; Q1).

---

### User Story 2 - Invalid registered persistence fails explicitly (Priority: P1)

As an operator, I need a missing connection string or a non-positive claim lease to fail loudly and by
name at composition or startup, so a half-configured host never appears to start healthy.

**Why this priority**: AC3's retained half and Scope bullet 5. A silent default here is how a
production host ends up claiming against the wrong database or with a zero lease.

**Independent Test**: In `LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs`,
with no container and a placeholder connection string, a missing or whitespace-only connection string
throws at the call, and a missing, zero or negative `ClaimLease` throws `OptionsValidationException`
carrying the exact lease message when the registered `IStartupValidator` runs.

**Acceptance Scenarios**:

1. **Given** no `ConnectionStrings:DefaultConnection`, **When** the registration runs, **Then** it throws `InvalidOperationException` whose message tells the operator to set `ConnectionStrings:DefaultConnection` via user secrets or the `ConnectionStrings__DefaultConnection` environment variable.
2. **Given** a whitespace-only connection string, **When** the registration runs, **Then** the same `InvalidOperationException` is thrown, and the thrown message never contains the configured value.
3. **Given** `ClaimLease` missing, `00:00:00`, or negative, **When** the registered startup validator runs, **Then** it throws `OptionsValidationException` whose failures carry the message `Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered`.
4. **Given** a clock already registered before the call, **When** the registration runs, **Then** that clock is still the one resolved afterwards; a host with no clock registered gets the system clock.
5. **Given** a valid configuration, **When** the container is built with both DI validation switches enabled, **Then** it builds and the startup validator passes without error.

---

### User Story 3 - One real round trip through the resolved production repository (Priority: P1)

As a maintainer, I need the composed registration proven against a real database, so "it resolves" is
not mistaken for "it works".

**Why this priority**: AC4's retained half. Resolution alone can hide a wrong provider, a wrong
database, or an unmigrated schema.

**Independent Test**: In `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs`, over
`PostgresFixture`, the DI-resolved repository allocates an identity, adds an aggregate, saves, and
reloads the same aggregate from the migrated database.

**Acceptance Scenarios**:

1. **Given** the composed registration pointed at the container's migrated database, **When** the resolved repository allocates an identity, adds an aggregate, saves and reloads it, **Then** the reloaded aggregate equals the saved one.
2. **Given** the same composition, **When** the registered startup validator runs, **Then** it passes with no failure.
3. **Given** the container-provided connection string, **When** the test ends, **Then** no credential or connection string has been written to any committed file — it exists only in the test process at run time.

---

### User Story 4 - Nothing else moves (Priority: P2)

As a reviewer, I need this ticket to add exactly the wiring and nothing else, so DEV-301's proven
repository, the schema, and the secret-handling rules are all still intact afterwards.

**Why this priority**: AC5 and Q8. It is the guard rail that keeps a small wiring ticket small.

**Independent Test**: `git diff --stat origin/main...HEAD` lists only the frozen-scope files; the
repository, DbContext, port interface, options class and migration are unchanged; no new NuGet package
is present.

**Acceptance Scenarios**:

1. **Given** the branch diff, **When** it is reviewed, **Then** the only files changed are the new registration extension, one added statement plus one `using` directive in the Api composition root, the two new test files, the D1 `ProjectReference` line in the integration test project (plus the `FrameworkReference` line only if FR-009's fallback was needed), and the spec artifacts.
2. **Given** the merged branch, **When** DEV-301's claim predicate and strict lease expiry are read, **Then** they are byte-for-byte unchanged, and no release-claim port member exists.
3. **Given** the merged branch, **When** the migration list is read, **Then** `20260930153520_Initial` is the only migration and it is unchanged.
4. **Given** any committed file in the branch, **When** it is scanned for secrets, **Then** no connection string, API key or credential is present.

### Edge Cases

- The Api project has no `appsettings.json` (DEV-18 owns it), and no default exists for `Enrichment:ClaimLease`. With this registration the Development Api therefore requires a positive `Enrichment:ClaimLease` from user secrets or `Enrichment__ClaimLease` to start. That is the Q4 intent, not an oversight: a host that registers the production repository has a positive-lease requirement even while consumption is inactive, and a host without this registration gains no lease requirement.
- `AddLamuFlixOptions` binds and validates all six options sections, so the startup validator also fails on a missing `Library:RootPath`, `Omdb:ApiKey`, `Omdb:BaseUrl` or `RabbitMq:HostName`. Tests must therefore supply a minimum valid value for each annotated section, or the DEV-392 lease validator is not what is being observed (D1, FR-010).
- Building a service provider is not a startup proof: `ValidateOnStart` fires from the registered startup validator, so the validator is invoked explicitly in tests.
- `Program.cs` call order is verified by review, not by a text-matching test (Q5). A text test on a composition root's source is brittle and asserts nothing about runtime behaviour.
- No web host test package exists in the repository and none is added (Q5). The composition is exercised on a plain service collection, so the Api's own `WebApplication.CreateBuilder` path is covered by review only.
- The registration is invoked exactly once by the Api. Repeated invocation is unspecified and untested; no decided basis makes it idempotent.
- No connection-string pooling and no retry strategy are configured, so a transient database failure at first use surfaces immediately rather than being retried (Q1).
- Resolving a scoped repository from the root provider throws under Development's default scope validation. That is retained deliberately (Scope bullet 5).
- The repository constructor guard and the claim predicate are untouched: a non-positive lease still fails at repository construction, and this ticket adds an earlier, clearer failure at startup rather than replacing that guard.

## Requirements

### Functional Requirements

- **FR-001** (Scope 1, Q1): a new file `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` declares `public static class PersistenceServiceCollectionExtensions` with `public static IServiceCollection AddLamuFlixPersistence(this IServiceCollection services, IConfiguration configuration)` in namespace `LamuFlix.Infrastructure.Persistence`, null-guards its arguments, and returns `services`. It is a registration method in the existing adapter assembly, not a new layer; Patron approves its public surface (§2.3 item 4, non-HTTP).
- **FR-002** (Scope 1-2, Q1): the method performs these registrations in this order: (1) read `configuration.GetConnectionString("DefaultConnection")` and throw `InvalidOperationException` when it is null or whitespace, reusing the existing host wording that names user secrets and `ConnectionStrings__DefaultConnection` and never echoes the value; the key name `"DefaultConnection"` is one `private const string` in the extension, used by both the read and the message; (2) `AddDbContext<LamuFlixDbContext>(o => o.UseNpgsql(connectionString))` at the default scoped lifetime, with no pooling and no `EnableRetryOnFailure`; (3) `TryAddSingleton(TimeProvider.System)`; (4) `AddOptions<EnrichmentOptions>().Validate(o => o.ClaimLease > TimeSpan.Zero, "Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered").ValidateOnStart()`, adding no second binding of the section; (5) `AddScoped<IMovieRepository, EfMovieRepository>()`.
- **FR-003** (Scope 1, 5-6, Q2): the connection string is read only inside the registration method. No setting, connection string or credential is committed. No second configuration key is introduced, and the legacy Worker and Web MySQL hosts and their configuration are untouched.
- **FR-004** (Scope 4-5, Q4): the registration adds only the validator in FR-002(4). The `EnrichmentOptions` class, the existing options binding and the repository constructor guard are unchanged; no `ClaimLease` default is invented; no Api `appsettings.json` is added; the active-path retry-TTL-versus-lease comparison stays with DEV-18.
- **FR-005** (Scope 2, Q1): `src/LamuFlix.Api/Program.cs` gains exactly one statement, `builder.Services.AddLamuFlixPersistence(builder.Configuration);`, plus that one `using LamuFlix.Infrastructure.Persistence;` directive, placed directly after `AddServiceDefaults()` and before `Build()`, and therefore before any later DEV-18 activation-guard call. Nothing else in that file changes, and there is no service-provider override: Development keeps the default `ValidateOnBuild` and `ValidateScopes`.
- **FR-006** (Scope 1, 3, Q1): DEV-301's `EfMovieRepository` and `LamuFlixDbContext` are reused as they are. No new repository type is created, `IMovieRepository` remains the registration key, and the claim predicate and strict lease expiry are unchanged.
- **FR-007** (AC1, AC4, Scope 6, Q5): `tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs` runs with no container and a placeholder connection string, as in `EfMovieRepositoryConstructorTests.cs:8-25`. It registers the in-memory configuration with `services.AddSingleton<IConfiguration>(configuration)` before the two production calls, so `AddLamuFlixOptions`' `BindConfiguration` resolves `IConfiguration` from DI as Q5 requires. It asserts: a missing connection string throws `InvalidOperationException` whose message names user secrets and `ConnectionStrings__DefaultConnection`; a whitespace-only connection string — a multi-character, non-space-only value such as `" \t "` — throws the same and the message contains no configured value; `ClaimLease` missing, `00:00:00` and negative each make the registered `IStartupValidator.Validate()` throw `OptionsValidationException` carrying the FR-002(4) message; `IMovieRepository` maps to `EfMovieRepository` and `LamuFlixDbContext` and `TimeProvider` map to Scoped, Scoped and Singleton respectively; a `TimeProvider` registered before the call is not replaced; and a valid configuration builds a provider with `ValidateOnBuild` and `ValidateScopes` both enabled. Building the provider or reading `IOptions<T>.Value` alone is not accepted as the startup proof.
- **FR-008** (AC1, AC4, Scope 6, Q5, D1): `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` uses `IClassFixture<PostgresFixture>`, takes its configuration from an in-memory configuration built in the test, registers it with `services.AddSingleton<IConfiguration>(configuration)`, then composes `AddLamuFlixOptions()` and after it `AddLamuFlixPersistence(configuration)` in the same order as the Api composition root, and builds the provider with `ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`, and asserts: within a scope `IMovieRepository` resolves as an `EfMovieRepository` and `LamuFlixDbContext` is the same instance within that scope and a different instance in the next; one real round trip through the resolved repository (`NextIdentityAsync`, `AddAsync`, `SaveChangesAsync`, `GetAsync`) succeeds against the migrated fixture database reached through the container's runtime connection string; and `IStartupValidator.Validate()` passes.
- **FR-009** (Q5, D1): `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` gains exactly one `ProjectReference` to `src/LamuFlix.ServiceDefaults`, so the test calls the production `AddLamuFlixOptions()` rather than re-implementing `BindConfiguration` in the test. If the build shows that `FrameworkReference Microsoft.AspNetCore.App` does not flow transitively from ServiceDefaults, that one `FrameworkReference` line is added to the same project file and reported in the PR. No `Directory.Packages.props` change, no new NuGet package, and no test reference to `LamuFlix.Api`.
- **FR-010** (D1): the in-memory test configuration supplies a minimum valid value for every annotated options section so the startup validator observes only the DEV-392 lease rule: `Library:RootPath` non-empty, `Omdb:ApiKey` non-empty, `Omdb:BaseUrl` an absolute URL, `RabbitMq:HostName` non-empty, plus `ConnectionStrings:DefaultConnection` and `Enrichment:ClaimLease`. `Enrichment:MaxAttempts`, `RabbitMq:Port`, `Playback` and `Features` need no key. Every value is an obvious non-credential placeholder except the container's runtime connection string.
- **FR-011** (AC5, Q8): no secret, connection string or credential appears in any committed file, including tests; no repository implementation is duplicated; `EfMovieRepository`, `LamuFlixDbContext`, `IMovieRepository`, `EnrichmentOptions` and migration `20260930153520_Initial` are unchanged; no release-claim port member is added.
- **FR-012** (Q8): out of scope — any release-claim port member; any change to DEV-301's claim predicate or lease-expiry semantics; migrations or any schema change; an Api `appsettings.json`; the `IMetadataProvider` implementation (DEV-303); the broker adapter, consumer and activation guard (DEV-18); the legacy Worker and Web MySQL contexts and their configuration; new NuGet packages; a test reference to `LamuFlix.Api`; database-driver mocks; any new test framework; edits to `EnrichmentOptions` or to the repository constructor guard.
- **FR-013** (Q6, `blocked: structural`): DEV-392 AC2, the consumer-inactivity/no-consume/no-ack portion of AC3, and AC4's active/inactive composition-path verification are deferred to DEV-18, with DEV-303 supplying the real metadata-provider prerequisite, subject to the owner checkbox. They are not verified in this ticket, no acceptance criterion is rewritten, and no claim is made that DEV-18 already covers real-port composition. The deferred verification stays outstanding until those prerequisites exist.
- **FR-014** (gates): the Roslyn analyzer, cyclomatic-complexity (≤ 15, ≤ 6 at the refactor gate) and InspectCode gates each exit 0 on the changed `.cs` files with zero new suppressions; `dotnet format --verify-no-changes` is clean; the full `dotnet test` suite is green (integration tests need Docker); mutation survivors on the blank-connection-string guard and the lease predicate are killed by the FR-007 tests; the vulnerable-package scan is unchanged; a gate that could not run is reported "Could not run" and never as PASS.

### Key Entities

- **`AddLamuFlixPersistence`**: the new public registration method; the only place the connection string is read.
- **`IMovieRepository` descriptor**: maps to `EfMovieRepository`, scoped, the port key DEV-18's guard looks for.
- **`LamuFlixDbContext` descriptor**: scoped, supplied with the Npgsql provider and the host's connection string.
- **`TimeProvider` descriptor**: singleton, the system clock unless a host already registered one.
- **`EnrichmentOptions` lease validator**: a positive-`ClaimLease` rule with startup validation, layered on top of the existing options binding, not replacing it.

## Success Criteria

### Measurable Outcomes

- **SC-001**: both test projects compose `AddLamuFlixOptions()` + `AddLamuFlixPersistence(configuration)` and pass `IStartupValidator.Validate()` with a valid configuration, with 0 failures reported.
- **SC-002**: 100% of the missing, whitespace-only, zero-lease and negative-lease cases fail explicitly with the named messages, and 0 thrown messages contain the configured connection-string value.
- **SC-003**: one real round trip through the DI-resolved repository completes against the container, with 0 mocked database-driver behaviours.
- **SC-004**: the branch diff touches only the frozen-scope files; the csproj change is the D1 `ProjectReference` line, plus the `FrameworkReference` line only if FR-009's fallback was needed.
- **SC-005**: all three analyzer gates exit 0 on the changed files, or are explicitly reported "Could not run"; 0 new suppressions and 0 new NuGet packages.
- **SC-006**: DEV-301's claim predicate and strict lease expiry, and the migration `20260930153520_Initial`, are unchanged in the diff.

## Assumptions

- The `Program.cs` call and its position are verified by review (Compass/Ledger), not by a test; no web host test package is added (Q5, FR-012).
- Consequence of Q4, accepted: a Development Api run needs a positive `Enrichment:ClaimLease` from user secrets or the environment, because this ticket adds the validator and DEV-18 owns the Api `appsettings.json`. No test in the repository boots the Api host, so nothing existing is broken by it.
- Repeated invocation of `AddLamuFlixPersistence` is unspecified and untested; the Api calls it once and no decided basis makes the method idempotent.
- The registration composes on a plain service collection, so the `WebApplication.CreateBuilder` path in the Api is covered by review rather than by a host test (Q5).
- Size M, no ADR, two spec analyze/fix rounds and at most two fix commits per delivery round; counters are not reset (Q7). Gate 1 stays closed until the owner answers Q6; no implementation is authorised before then.
- Nothing is assumed about `IMetadataProvider`: no implementation exists in the repository, and DEV-303 owns it (FR-012, FR-013).
