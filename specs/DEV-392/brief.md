# DEV-392 — Brief (Keel)

Wire production `IMovieRepository` and `LamuFlixDbContext` registration in Api.
Size **M** · UI: no · Branch `feature/392-spec` · Parent DEV-282.

Sources: ticket DEV-392 (Scope bullets 1-6, AC1-5, in published order), note `recon-DEV-392`
(cited `R:line`), Patron rulings in `specs/DEV-392/CONCLUSIONS.md` (commit 104bbf5).
If it is not in this file, it is not decided. Gaps go to Keel as `needs decision: <question>`.

## Closing bar

The spec phase is done when all of these hold:

1. Quill has written `spec.md`, `plan.md`, `tasks.md` under `specs/DEV-392/` from this brief.
2. `/speckit-analyze` reports no CRITICAL or HIGH finding, and `plan.md` / `tasks.md` match every
   decision below (approach, files, tests, gates, ordering).
3. The spec PR (`DEV-392: spec`, diff `specs/**` only) carries the Q6 owner checkbox verbatim.
4. **Gate 1 stays closed until the owner answers Q6.** No implementation is authorised before then.

## Round cap

- Spec analyze/fix rounds with Quill: at most **2**. Third-round findings go to the owner on the PR.
- Delivery review: 2 rounds max, at most 2 fix commits per round (Q7). Counters are not reset.
- No ADR (Q7: no new port, no changed architectural choice).

## Frozen scope

**In**

- New public extension `AddLamuFlixPersistence(this IServiceCollection, IConfiguration)` in
  `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (Q1).
- One call in `src/LamuFlix.Api/Program.cs`, immediately after `AddServiceDefaults` and before
  `Build` (Q1). It must come before any later DEV-18 guard call (`specs/DEV-18/plan.md:197`).
- Unit and integration tests in the existing test projects (Q5).

**Out** (Q8)

- Any release-claim port member; any change to DEV-301 claim predicate or lease-expiry semantics.
- Migrations: `20260930153520_Initial` is unchanged; no schema change.
- An Api `appsettings.json` (DEV-18 owns it, `specs/DEV-18/plan.md:200`).
- `IMetadataProvider` implementation (DEV-303), broker adapter, consumer, activation guard (DEV-18).
- Legacy Worker/Web MySQL contexts and their configuration.
- New NuGet packages, including `Microsoft.AspNetCore.Mvc.Testing`; a test reference to `LamuFlix.Api`
  (a test reference to `LamuFlix.ServiceDefaults` is allowed, D1);
  database-driver mocks; any new test framework (Q5).
- Changes to `EnrichmentOptions` or the `EfMovieRepository` constructor guard (Q4).

**Pending owner decision (Q6)** — see below. Planning proceeds on the assumption that the deferral is
approved, with the DEV-18/DEV-303 dependency stated explicitly in `spec.md` and `plan.md`.

## Grill answers (Patron, Q1-Q8)

| Q | Ruling | Basis (abridged; full in CONCLUSIONS.md) |
|---|---|---|
| Q1 | ACCEPT public `AddLamuFlixPersistence(IServiceCollection, IConfiguration)` in `Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`. Registers `AddDbContext<LamuFlixDbContext>(o => o.UseNpgsql(cs))` and `AddScoped<IMovieRepository, EfMovieRepository>()`. Scoped; **no** pooling, **no** `EnableRetryOnFailure`. Api calls it right after `AddServiceDefaults`, before `Build` and before any DEV-18 guard. Patron approves the public surface (§2.3 item 4, non-HTTP). | Scope 1-2; R:30,33-40,78,80,92; DEV-301 CONCLUSIONS:37-38; DEV-18 plan:197 |
| Q2 | ACCEPT `ConnectionStrings:DefaultConnection` via `GetConnectionString`, read only inside the extension. Missing or whitespace-only value → `InvalidOperationException` at composition time, reusing the existing message naming user secrets and `ConnectionStrings__DefaultConnection`; the value is never echoed. No setting or credential committed. No second key; legacy hosts untouched. | Scope 1,5-6; R:41-42,81,93; Worker/Program.cs:24-28 |
| Q3 | ACCEPT `TryAddSingleton(TimeProvider.System)` inside the extension; a pre-registered test clock wins. No clock package added; tests use an existing `TimeProvider` subclass if needed. | Scope 5; R:23-24,94 |
| Q4 | ACCEPT an extra `EnrichmentOptions` validator in the extension: `ClaimLease > TimeSpan.Zero`, `.ValidateOnStart()`, message exactly: `Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered`. Existing options binding, the options class and the repository ctor guard are unchanged. No default, no Api `appsettings.json`. Retry-TTL > ClaimLease stays with DEV-18 (active path only). | Scope 4-5; R:24,36,49,51,95; DEV-18 spec:174 |
| Q5 | ACCEPT tests in existing `LamuFlix.UnitTests` and `LamuFlix.IntegrationTests` with existing dependencies only. Plain `ServiceCollection` + registered `IConfiguration` + production-equivalent `EnrichmentOptions` binding, `ValidateOnBuild = true` and `ValidateScopes = true`. Startup proof must invoke the registered `IStartupValidator.Validate()` (or start an existing generic host) — building a provider or reading `IOptions.Value` alone does **not** exercise `ValidateOnStart`. `Program.cs` call and order are verified by review, not by a text test. | AC1,AC4-5; Scope 6; R:54-74,96 |
| Q6 | **blocked: structural** — owner checkbox below. Rigger records the recon fact and the pending decision as comments on DEV-392 and DEV-18. No AC rewrite; no claim that DEV-18 already covers real-port composition. | Scope 2; AC1-4; R:45-52; DEV-18 plan:197,201 |
| Q7 | CONFIRM size M, no ADR, 2 spec review rounds, ≤ 2 fix commits per round; counters not reset. | Planning Estimate; DEV-18 plan:256 |
| Q8 | CONFIRM out-of-scope list above. Coordination and the pending Q6 decision stay in scope. | Scope 1-4; AC5; R:18,30,35,45-52,79 |

No taste decisions were made; no `ASSUMPTIONS.md` is created.

### Q6 owner checkbox (spec PR body, verbatim)

- [ ] Approve deferring DEV-392 AC2, the consumer-inactivity/no-consume/no-ack portion of AC3, and AC4's active/inactive composition-path verification to DEV-18, with DEV-303 supplying the real metadata-provider prerequisite. DEV-392 retains scoped production registration, actual EfMovieRepository/DbContext resolution, a real PostgreSQL round trip, explicit invalid-persistence failure, and registration before DEV-18's activation guard. The deferred real-port composition verification remains outstanding until those prerequisites exist.

## Plan decisions

### Approach

1. `AddLamuFlixPersistence(IServiceCollection services, IConfiguration configuration)`, in this order:
   1. Read `configuration.GetConnectionString("DefaultConnection")`; if null/whitespace, throw
      `InvalidOperationException` with the Worker/Web wording (user secrets +
      `ConnectionStrings__DefaultConnection`), never including the value.
   2. `services.AddDbContext<LamuFlixDbContext>(o => o.UseNpgsql(connectionString))` — default scoped
      lifetime; no pooling, no retry strategy.
   3. `services.TryAddSingleton(TimeProvider.System)`.
   4. `services.AddOptions<EnrichmentOptions>().Validate(o => o.ClaimLease > TimeSpan.Zero, <Q4 message>).ValidateOnStart()`.
      Do not re-bind the section; binding stays in `AddLamuFlixOptions` (R:36).
   5. `services.AddScoped<IMovieRepository, EfMovieRepository>()`.
   6. Return `services`.
2. `Program.cs`: insert `builder.Services.AddLamuFlixPersistence(builder.Configuration);` directly after
   `AddServiceDefaults`. Nothing else in `Program.cs` changes. Development keeps the default
   `ValidateOnBuild` / `ValidateScopes` (no `UseDefaultServiceProvider` override).
3. Reuse DEV-301's `EfMovieRepository` and `LamuFlixDbContext` as-is; no new repository type.

### Files touched

| File | Change |
|---|---|
| `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` | **new** — the extension |
| `src/LamuFlix.Api/Program.cs` | one added line (the call) |
| `tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs` | **new** — unit tests, no container |
| `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` | **new** — Testcontainers PostgreSQL via `PostgresFixture` |
| `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` | one added line: `ProjectReference` to `src/LamuFlix.ServiceDefaults` (D1) |
| `specs/DEV-392/*` | spec artifacts |

No `Directory.Packages.props`, migration, `appsettings*.json`, or Core file changes, and no csproj
change other than the D1 line. No new NuGet package and no test reference to `LamuFlix.Api` (Q5).
Production-equivalent `EnrichmentOptions` binding in tests is the production call itself:
both test projects call `AddLamuFlixOptions()` (`LamuFlix.UnitTests` already references
ServiceDefaults; `LamuFlix.IntegrationTests` gains it under D1). Tests do not hand-roll
`BindConfiguration`. Any further reference need is `needs decision:` to Keel, not a workaround.

### Decisions after grill

**D1 (2026-10-01, answers Quill `needs decision` on IntegrationTests references): option (a).**
Add one `ProjectReference` to `LamuFlix.ServiceDefaults` in `LamuFlix.IntegrationTests.csproj`; the
integration test composes `AddLamuFlixOptions()` + `AddLamuFlixPersistence(configuration)` exactly as
`Program.cs` does, with configuration from `ConfigurationBuilder().AddInMemoryCollection(...)`.

- Basis: Q5 requires *production-equivalent* binding; calling the production registration is the
  only form that fails if `AddLamuFlixOptions` binding regresses. Option (f) re-implements the
  binding in the test and would stay green across such a regression. Option (b) adds packages,
  which Q5 and Out bar. `LamuFlix.UnitTests.csproj:33` already references ServiceDefaults, so this
  is an existing test-to-ServiceDefaults edge, not a new architectural layer or project (§2.3
  item 2 not triggered); no package or `Directory.Packages.props` change (§2.3 item 1 not
  triggered). It does not touch ticket text; the barred `LamuFlix.Api` reference stays barred.
- Implementation check: `FrameworkReference Microsoft.AspNetCore.App`
  (`LamuFlix.ServiceDefaults.csproj:8`) is expected to flow to the referencing project. If the build
  shows it does not, add that one `FrameworkReference` line to the same csproj as well; the test
  still calls `AddLamuFlixOptions()`, never its own `BindConfiguration`. Report it in the PR.
- Test configuration: `AddLamuFlixOptions()` validates every options section with
  `ValidateDataAnnotations().ValidateOnStart()`, so `IStartupValidator.Validate()` checks all of
  them. The in-memory configuration (unit and integration) supplies the minimum valid value for any
  section whose annotations require one; that is valid test setup, not a workaround. Quill names the
  required keys in `plan.md` from the options classes in `src/LamuFlix.Core/Options/`.

### Test strategy

Unit (`LamuFlix.UnitTests`, no container, placeholder connection string as in
`EfMovieRepositoryConstructorTests.cs:8-25`):

- Missing connection string → `InvalidOperationException`; message names user secrets and
  `ConnectionStrings__DefaultConnection`; message does not contain the configured value (blank case).
- Whitespace-only connection string → same.
- `ClaimLease` missing, `00:00:00`, negative → `IStartupValidator.Validate()` throws
  `OptionsValidationException` carrying the Q4 message.
- Descriptors: `IMovieRepository` → `EfMovieRepository` Scoped; `LamuFlixDbContext` Scoped;
  `TimeProvider` Singleton; a pre-registered `TimeProvider` is not replaced.
- Valid config + both DI validation switches → provider builds without error.

Integration (`LamuFlix.IntegrationTests`, `IClassFixture<PostgresFixture>`):

- `ServiceCollection` + `IConfiguration` (fixture connection string, positive `ClaimLease`),
  `AddLamuFlixOptions()` + `AddLamuFlixPersistence` (D1), `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`.
- In a scope: `IMovieRepository` resolves as `EfMovieRepository`; `LamuFlixDbContext` resolves and is
  the same instance within the scope and different across scopes.
- One real round trip through the resolved repository (`NextIdentityAsync` / `AddAsync` /
  `SaveChangesAsync` / `GetAsync`) against the migrated fixture database.
- Startup validation passes with valid config (`IStartupValidator.Validate()`).

Not tested here (Q6 pending): DEV-18 consumer active/inactive paths. `Program.cs` call order is a
review item for Compass/Ledger, not a test.

### Gate expectations

- Baseline is green with 0 findings on all three analyzer gates for `Program.cs` (R:84-89).
- Roslyn analyzers, cyclomatic complexity (≤ 15; refactor gate ≤ 6), and InspectCode must each exit 0
  on the changed `.cs` files, with zero new suppressions.
- `dotnet format --verify-no-changes` clean; full `dotnet test` green (integration tests need Docker).
- Mutation: survivors on the extension's guard clauses (blank check, lease predicate) must be killed
  by the unit tests above.
- Vulnerable-package scan unchanged (no new packages).
- Secrets: no connection string or credential in any committed file, including tests (tests use the
  container's runtime string or an obvious non-credential placeholder).

### Task ordering constraints

1. Unit tests for the extension's guards and descriptors (may be written red first).
2. `PersistenceServiceCollectionExtensions.cs` — must exist before the `Program.cs` call compiles.
3. `Program.cs` one-line call, after `AddServiceDefaults`, before `Build`.
4. Integration tests against `PostgresFixture` (depend on 2).
5. Gates and full suite last.

Every task is blocked on Gate 1, which stays closed until the owner answers Q6.
