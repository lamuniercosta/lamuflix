# DEV-303 — Brief (Keel)

Ticket: DEV-303 — Implement the OMDb adapter with a typed HttpClient, a resilience pipeline and WireMock tests. Size M, UI false, parent DEV-283.
Worktree: `F:\Dev\LamuFlix.worktrees\feature-303-spec` · branch `feature/303-spec` · base `f5d5d60` (grill rulings `fa6081e`).
Sources: ticket text (DEV-303 task note:13), `recon-DEV-303` (cited `R:line`), Patron rulings Q1–Q12 in `CONCLUSIONS.md`, taste in `ASSUMPTIONS.md`, questions in `grill-questions.md`.

If a decision is not in this file, it is not decided. Quill drafts `spec.md`, `plan.md` and `tasks.md` from this brief only.

**Recon (§9):** [R-1] to [R-4] are answered (`recon-DEV-303`:142-217, cited `R:line`) and folded into the items marked **[R-n]** below. Nothing is open. The brief is frozen.

---

## 1. Closing bar

DEV-303 is done when all of the following hold:

1. A `sealed OmdbMetadataProvider` in `src/LamuFlix.Infrastructure/Adapters/` implements `LamuFlix.Core.Ports.IMetadataProvider.FindAsync`. It is a typed HttpClient behind `AddStandardResilienceHandler` and is configured from validated options (Q2, Q4, Q5).
2. Every outcome follows the Q6 table (§5.1). A caller's cancellation is rethrown. Nothing else escapes as an exception for an expected provider outcome.
3. The API key comes only from `Omdb:ApiKey` (env `Omdb__ApiKey` or Development User Secrets). It has no default. It never appears in logs, exception output or telemetry (Q1, Q9).
4. `AddOmdbMetadataProvider` is called in Api `Program.cs` before `AddLamuFlixRabbitMq`, so the existing enrichment consumer is active in the Api host (Q8).
5. A `Metadata.Lookup` span on the existing `LamuFlix` ActivitySource name, source-generated warning logs, and a `ready`-tagged OMDb health check are in place (Q11, [R-2], [R-3]).
6. The WireMock.Net integration matrix (§5.3) is green, including the ticket's Found/NotFound/401/429/500 cases, the transient-retry verification, a real circuit-breaker trip, and the Retry-After and secrecy checks.
7. All gates in §5.4 pass. Full `dotnet test` is green and `dotnet format --verify-no-changes` is clean.

## 2. Frozen scope

**In scope (Q12)**
- The adapter, its private response DTO and mapping, logs, span, and health check.
- `AddOmdbMetadataProvider`: typed client, standard resilience handler, Core-port registration and health-check registration.
- `OmdbResilienceOptions` under `Omdb:Resilience`, validated at startup (Q5).
- The `TelemetryConstants.MetadataLookup` constant (Q11).
- Api `Program.cs` wiring, Api `appsettings.json` `Omdb:BaseUrl`, and a new `UserSecretsId` in Api csproj (Q8, Q9).
- Package pins and references: `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.Http` in Infrastructure, `WireMock.Net` in IntegrationTests (Q3).
- Tests (§5.3).

**Out of scope (do not touch)**
- The legacy `LamuFlix.Worker` (its provider, interface and `Program.cs`), `LamuFlix.Web` and `tests/LamuFlix.Test`. Do not delete them or fix their builds (NU1010, CS0246) (Q2, Q12).
- `EnrichmentFailureCategory.cs` (it carries a pre-existing InspectCode warning, R:131-134) and `EnrichmentFailureClassifier` (Q6).
- The Core port surface: `IMetadataProvider`, `MetadataLookup`, `MetadataLookupResult`, `MovieMetadata` and the value objects do not change (Q7, Q12).
- Schema and migrations. API routes, OpenAPI and `/web`. Response caching. imdbId lookups (Q12).
- `Features:LocalPlay` and `Process.Start`.
- Other packages, in particular `Microsoft.Extensions.TimeProvider.Testing` (Q3, Q5).
- A second ActivitySource or Meter, and custom metrics (Q11).
- A test-only public API seam (Q5).

**Ticket-text interpretation (recorded, not escalated; Q1):** the `b5a4e6d9` fallback the ticket says to remove does not exist anywhere in the tree or in git history (R:42-49), so nothing is removed. The spec states this with the recon evidence. The "no hardcoded keys" acceptance criterion is met because the new path is options-only and has no default.

## 3. Round cap

- Grill: one round, 12 of 12 questions used. Closed (`fa6081e`).
- Spec review (Keel ↔ Quill): **a hard cap of 2 rounds** (Q12). Findings still open after round 2 go to the Conductor as `blocked:`. There is no third round and no cap reset.
- Plan challenge: one adjudication pass over the Challenger findings.
- No owner checkbox. Gate 1 is unchanged (Q12).

## 4. Grill answers (Patron, Q1–Q12; full text and citations in `CONCLUSIONS.md`)

| # | Verdict | Ruling |
|---|---|---|
| Q1 | ACCEPT | The fallback is already absent and nothing is removed. The key comes only from validated `OmdbOptions.ApiKey`, with no default. |
| Q2 | ACCEPT | A `sealed OmdbMetadataProvider` implements the Core port in `Infrastructure/Adapters/`. The legacy Worker provider, Worker `Program.cs` and its tests stay untouched. |
| Q3 | ACCEPT | Add `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.Http` (already pinned) to Infrastructure, and `WireMock.Net` to IntegrationTests, all through CPM. No other direct package. Pinned versions: `Microsoft.Extensions.Http.Resilience` 10.10.0, `WireMock.Net` 2.18.0 **[R-1]**. |
| Q4 | CHANGE | One `AddStandardResilienceHandler`: exponential backoff with jitter, 3 retries, 1 s base delay. Retry and breaker handle only 5xx, 408, 429, `HttpRequestException` and `TimeoutRejectedException`. Attempt timeout 10 s, total 30 s, default rate limiter, breaker 0.1 / 30 s / 100 / 5 s. Retry-After is honoured and never shortened, and `MaxDelay` is not claimed to cap it. The total timeout cancels any wait that runs past the budget, and that outcome is ProviderUnavailable. 401 and other 4xx are never retried. No hedging and no nested retry. |
| Q5 | CHANGE | The Q4 numbers are bound from `Omdb:Resilience` into a nested, dependency-free options record. The record carries the Q4 defaults and has data-annotation and cross-field validation at startup. Tests override it through configuration with valid, shorter values. No constants and no test-only seam. |
| Q6 | ACCEPT | The adapter returns Found, NotFound or Failed per §5.1. A caller's cancellation, including during a retry wait or while reading the body, takes precedence and is rethrown. The classifier is unchanged. Programming errors are not caught. |
| Q7 | ACCEPT | Escaped query values and `type=movie`, sent to the configured BaseUrl. A private System.Text.Json DTO. Invariant-culture mapping where an optional field that fails to parse becomes null. Every value object goes through `TryCreate`. `ReleaseYear` uses the injected `TimeProvider`. A missing, blank or `N/A` Title, or a missing or unrecognised `Response`, is InvalidResponse. |
| Q8 | ACCEPT | A public `AddOmdbMetadataProvider(IServiceCollection)`, called in Api `Program.cs` before `AddLamuFlixRabbitMq`. Activating the consumer is authorised composition work. |
| Q9 | ACCEPT | Api `appsettings.json` gets only `Omdb:BaseUrl = https://www.omdbapi.com/`. Api csproj gets a new UserSecretsId (not copied from Worker). The key comes from env or User Secrets. The key, the request URI and the provider's `Error` text never reach logs, exception output or span tags. Factory redaction stays on but is not relied on alone. |
| Q10 | CHANGE | The full WireMock matrix (§5.3), plus a real breaker trip at `MinimumThroughput = 2`, an oversized Retry-After hitting the total timeout, and secrecy checks on logs, exceptions and activities. The property-test opt-out is approved: Bernstein records it in the task note and the gate reports SKIP, not PASS. The matrix is never claimed as a mutation pass. |
| Q11 | CHANGE | Source-generated structured warning logs carry status, category, exception type and inherited context. Add the `Metadata.Lookup` constant to `TelemetryConstants` and open its span on the existing source, under the current enrichment activity. Register the OMDb health check through the existing health wiring. No new source or meter. [assumed] The 401 log text is "Check the OMDb API key configuration." |
| Q12 | ACCEPT | Scope is frozen as in §2. Spec review has a hard cap of 2 rounds. |

## 5. Plan decisions

### 5.1 Approach

**Options (Q5).** Add a new `public sealed record OmdbResilienceOptions` in `src/LamuFlix.Core/Options/`, with `SectionName = "Omdb:Resilience"`.
- It is dependency-free (BCL types only) and is bound and validated through the existing `BindAndValidate<T>` in `ServiceDefaults/Extensions.cs`, with `ValidateOnStart`. It is a separate record bound to the nested path rather than a property on `OmdbOptions`, because DataAnnotations validation does not recurse into nested members, and this keeps `OmdbOptions` unchanged.
- Members, with their Q4 defaults as initialisers:
  - `MaxRetryAttempts` = 3
  - `BaseDelay` = 1 s
  - `AttemptTimeout` = 10 s
  - `TotalTimeout` = 30 s
  - `FailureRatio` = 0.1
  - `SamplingDuration` = 30 s
  - `MinimumThroughput` = 100
  - `BreakDuration` = 5 s
- The defaults are not repeated in `appsettings.json`; the record is the single source.
- `[Range]` attributes mirror the pinned library's validation minima. The plan cites them from Microsoft Learn: at least 1 retry, `MinimumThroughput` ≥ 2, `FailureRatio` in (0, 1], and the documented `BreakDuration` and timeout minima.
- `IValidatableObject` adds two cross-field rules: `AttemptTimeout` ≤ `TotalTimeout`, and `SamplingDuration` ≥ 2 × `AttemptTimeout` (the standard handler's own rule). This way an invalid config fails at startup, not at first use.

**Registration.** Add a `public static class OmdbServiceCollectionExtensions` with `AddOmdbMetadataProvider(this IServiceCollection)` in `Infrastructure/Adapters/`.
- It calls `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>`, sets `BaseAddress` from `IOptions<OmdbOptions>.BaseUrl`, and adds `.AddStandardResilienceHandler()`. The handler's options are configured from `IOptions<OmdbResilienceOptions>` through the `IServiceProvider`-aware configure overload, so tests change behaviour only through configuration.
- The retry and breaker `ShouldHandle` both use one predicate: status 5xx/408/429, `HttpRequestException`, `TimeoutRejectedException`. Set `ShouldRetryAfterHeader = true`, `BackoffType = Exponential` and `UseJitter = true`.
- It also registers the health check (below).
- Infrastructure does not call `AddLamuFlixOptions`. Hosts already get the options from `AddServiceDefaults` (R:57-58).

- It calls `services.TryAddSingleton(TimeProvider.System)` **[R-3]**. In the Api, `AddLamuFlixPersistence` already registers it the same way (R:196-197), so this is a no-op there. It keeps the registration self-contained for tests and hosts that do not call persistence, and a pre-registered fake still wins.
- `IMetadataProvider` here is always `LamuFlix.Core.Ports.IMetadataProvider`, never the legacy `LamuFlix.Worker.Services.IMetadataProvider`. The consumer guard binds only the Core port (R:215).

**Adapter flow.** `OmdbMetadataProvider` takes `HttpClient`, `TimeProvider` (from DI, see Registration) **[R-3]** and `ILogger<OmdbMetadataProvider>`, plus the health-state dependency (below), through its primary constructor. The API key comes from `IOptions<OmdbOptions>`. `FindAsync` runs in this order:
1. Start the `Metadata.Lookup` activity on the provider's own `private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);` **[R-3]**.
2. Build a relative request URI with `Uri.EscapeDataString` for the key, title and year: `?apikey=…&t=…&type=movie[&y=…]`.
3. Call `GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct)` through the pipeline.
4. Classify the result (table below).
5. On 200, deserialize the body with the token.
6. Map the body (Q7).
7. Set the outcome on the activity, and record it in the health state.

`FindAsync` stays a thin orchestrator, and each step is a private helper or lives in the mapper, to meet the ≤ 6 refactor gate.

**Outcome table (Q6).** The first matching row wins.

| Condition | Result |
|---|---|
| `ct.IsCancellationRequested` and an `OperationCanceledException` is raised (during send, a retry wait or the body read) | rethrow |
| 200, `Response:"True"`, valid Title | `Found(metadata)` |
| 200, `Response:"False"`, `Error == "Movie not found!"` | `NotFound` |
| 401 | `Failed(InvalidResponse)` + warning log "Check the OMDb API key configuration." |
| 429 (the final outcome after retries) | `Failed(RateLimited)` |
| 5xx / 408 final, `TimeoutRejectedException`, `BrokenCircuitException`, `HttpRequestException`, or a total-timeout cancellation the caller did not request | `Failed(ProviderUnavailable)` |
| other 4xx; a 200 with malformed JSON, DTO-incompatible types, a missing or unrecognised `Response`, `Response:"False"` with another `Error`, or a missing/blank/`N/A` Title | `Failed(InvalidResponse)` |

Only the exception types listed are caught. Anything else propagates (Q6: programming errors are not caught).

**Mapping (Q7).** Add an `internal static class OmdbResponseMapper` with a private `OmdbResponse` DTO: `Response`, `Error`, `Title`, `Year`, `Runtime`, `Plot`, `imdbRating`, `imdbID`, all `string?`, with `[JsonPropertyName]` where needed.
- Title must be present, not blank and not `N/A`, or the result is InvalidResponse.
- `Plot` maps to synopsis, and `N/A` or missing becomes null.
- `Year`: the leading 4 digits go through `ReleaseYear.TryCreate`, which uses `TimeProvider.GetUtcNow()`.
- `Runtime`: the `"<n> min"` form goes through `Runtime.TryCreate`.
- `imdbRating`: parsed with `decimal`/`double.TryParse` (whatever `ImdbRating` takes) using `CultureInfo.InvariantCulture`, then `ImdbRating.TryCreate`.
- `imdbID` goes through `ImdbId.TryCreate`.
- A failed parse or a domain-invalid value becomes null for that field only.
- The plan cites the actual value-object factory signatures.

**Logs (Q11).** Add an `internal static partial class OmdbLog` with `[LoggerMessage]` warnings:
- `InvalidApiKey` (401, carrying the advice text)
- `RateLimited` (429 exhausted)
- `ProviderUnavailable` (structured fields: status code if any, category, and exception **type name**)
- `InvalidResponse` (status code or "malformed body")

No URI, key, title, provider `Error` or raw exception message or object is passed to the logger. Context (movie id, trace) comes from the enclosing scope and activity, not from new parameters.

**Telemetry (Q11).**
- Add `public const string MetadataLookup = "Metadata.Lookup";` to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`.
- The span is started as a child of `Activity.Current` from a `private static readonly ActivitySource` named `TelemetryConstants.ActivitySourceName` **[R-3]**. This follows the existing pattern in `EnrichmentConsumer.cs:28` and `RabbitMqEnrichmentQueuePublisher.cs:15` (R:192-193). It is the same source *name*, so it is not a second source under Q11. `PipelineActivity.Source` is not reused or widened, and no ActivitySource goes into DI. Its tags are the outcome category (via the existing `ErrorType` constant on failure) and the HTTP status code.
- There is no title, URI or key tag.
- The built-in HttpClient and Polly telemetry is left at its defaults. `System.Net.Http` query redaction is not disabled anywhere.

**Health check (Q11; registration point [R-2]).** A passive check that makes **no network calls**. A live OMDb probe would spend the daily request quota on every poll and put the key on the wire.
- An `internal sealed class OmdbHealthState` singleton records the last lookup outcome and its time from `TimeProvider`. The adapter writes to it.
- `OmdbHealthCheck : IHealthCheck` reports:

| Condition | Status |
|---|---|
| No lookup yet, or the last lookup was Found or NotFound | Healthy |
| Last lookup was RateLimited or ProviderUnavailable | Degraded |
| Last lookup was a 401 | Unhealthy, with the description "Check the OMDb API key configuration." |

- The check is registered by `AddOmdbMetadataProvider` through `AddHealthChecks().AddCheck<OmdbHealthCheck>("omdb", tags: ["ready"])` **[R-2]**. This matches the one existing check and its one tag (`RabbitMqServiceCollectionExtensions.cs:38`, R:173-177). The comment invariant at `ServiceDefaults/Extensions.cs:14` holds: the Api does not add another `AddHealthChecks` call.
- **No health endpoint is mapped.** Nothing in the repo maps one today (R:179-182). Mapping one would be a new public route (§2.3 item 4) outside the ticket, so it stays out of scope. Tests observe the check through `HealthCheckService` and the registration through `IOptions<HealthCheckServiceOptions>` (the pattern at `RabbitMqServiceCollectionExtensionsTests.cs:102`, R:209).

**Api host (Q8, Q9).**
- In `src/LamuFlix.Api/Program.cs`, add `builder.Services.AddOmdbMetadataProvider();` after `AddLamuFlixPersistence` and before `AddLamuFlixRabbitMq` (R:28-29).
- In `src/LamuFlix.Api/appsettings.json`, add `"Omdb": { "BaseUrl": "https://www.omdbapi.com/" }`.
- In `src/LamuFlix.Api/LamuFlix.Api.csproj`, add `<UserSecretsId>` with a **newly generated** GUID (not the Worker/Web one).

### 5.2 Files touched

New:
- `src/LamuFlix.Core/Options/OmdbResilienceOptions.cs`
- `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs`
- `src/LamuFlix.Infrastructure/Adapters/OmdbResponseMapper.cs` (holds the private DTO)
- `src/LamuFlix.Infrastructure/Adapters/OmdbLog.cs`
- `src/LamuFlix.Infrastructure/Adapters/OmdbHealthCheck.cs` (plus `OmdbHealthState`, in the same file or its own file; Quill picks per the one-type-per-file convention)
- `src/LamuFlix.Infrastructure/Adapters/OmdbServiceCollectionExtensions.cs`
- `tests/LamuFlix.IntegrationTests/OmdbMetadataProviderTests.cs` (outcomes, mapping, request shape)
- `tests/LamuFlix.IntegrationTests/OmdbResilienceTests.cs` (retry, Retry-After, timeouts, breaker, cancellation)
- `tests/LamuFlix.IntegrationTests/OmdbCompositionTests.cs` (registration, consumer activation order, health check, secrecy)
- `tests/LamuFlix.UnitTests/Options/OmdbResilienceOptionsValidationTests.cs`, or new facts appended to the existing `OptionsStartupValidationTests.cs` (R:84). Quill picks the one that matches the layout.

The integration tests stay flat in the test project, following the DEV-302 D22 precedent. One WireMock fixture per class; whether it goes in `Tests.Common` or stays local to IntegrationTests is the plan's choice, with no new project.

Edited (each with a cited ruling):
- `Directory.Packages.props` (2 `PackageVersion`, Q3)
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` (2 `PackageReference`, Q3)
- `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` (`WireMock.Net` only, Q3). It already references Infrastructure directly (:20) and ServiceDefaults (:21), so no `ProjectReference` changes **[R-1]**.
- `src/LamuFlix.ServiceDefaults/Extensions.cs` (one `BindAndValidate<OmdbResilienceOptions>` line, Q5)
- `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` (one constant, Q11)
- `src/LamuFlix.Api/Program.cs`, `src/LamuFlix.Api/appsettings.json`, `src/LamuFlix.Api/LamuFlix.Api.csproj` (Q8, Q9)

No existing test is edited **[R-4]**. No test boots the Api host, and none composes `AddLamuFlixRabbitMq` with the Api registrations. Every existing `AddLamuFlixRabbitMq` test uses a bare `ServiceCollection` (R:204-213), so registering the provider in the Api cannot flip an existing assertion. No Api host test is added either: that would need `Microsoft.AspNetCore.Mvc.Testing` (a new package, out of scope under Q3) and a `public partial class Program`. Consumer activation is covered by the composition test on a `ServiceCollection` (§5.3).

Nothing is deleted.

### 5.3 Test strategy (Q10 matrix)

**Setup and conventions**
- Tests use a real HTTP server (WireMock.Net), and no live OMDb call or real key. Test config sets `Omdb:ApiKey` to a sentinel such as `test-key-SENTINEL-7f3a`, `Omdb:BaseUrl` to the WireMock loopback URL, and `Omdb:Resilience:*` to valid short values (for example base delay 10 ms, attempt timeout 500 ms, total 3 s, sampling 1 s, min throughput 2, break 1 s), all within the library minima.
- Each test builds its own `ServiceCollection` + `AddLamuFlixOptions`-equivalent binding + `AddOmdbMetadataProvider`, so it gets fresh DI and pipeline state.
- One WireMock server per class, reset per test. Classes that share a server do not run in parallel.
- Conventions (DEV-302 D23): `Method_Condition_Expected` names, `TestContext.Current.CancellationToken`, only `// arrange` / `// act` / `// assert` comments, no `DateTime.Now`.

**Outcomes and mapping**
- Found: full mapping of all six fields.
- Found with `N/A` and missing optional fields: those fields are null.
- Domain-invalid optional values: an out-of-range year, a negative runtime, a rating > 10 and a malformed imdbID each become null, and the rest of the result is still Found.
- Series-style year `"2010–2014"` → 2010.
- Invariant rating parse: run under a `pt-BR` current culture.
- NotFound: `Response:"False"` / "Movie not found!".
- `Response:"False"` with another `Error` → InvalidResponse.
- A blank, `N/A` or missing Title → InvalidResponse.
- A missing `Response` → InvalidResponse.
- Malformed JSON and DTO-incompatible JSON (for example a number where a string is expected) → InvalidResponse.

**Status handling**
- 401 → InvalidResponse, exactly 1 request, and the warning advice log is captured.
- 400, 403 and 404 → InvalidResponse, exactly 1 request each (never retried).
- 429 always → RateLimited, requests = 1 + `MaxRetryAttempts`.
- 500 always → ProviderUnavailable, requests = 1 + retries.
- 503 then 200 → Found with exactly 2 requests (the ticket's transient-retry verification).
- 429 with `Retry-After: 1`, then 200 → Found. The gap between requests is ≥ 1 s minus a small tolerance, measured with a monotonic `Stopwatch`.
- `Retry-After` larger than the total timeout → ProviderUnavailable once the total budget is spent, and no second request is sent before the header delay.
- Per-attempt timeout (a WireMock delay above the attempt timeout) → ProviderUnavailable after retries.
- A connection failure (server stopped) → ProviderUnavailable.

**Circuit breaker**
- With `MinimumThroughput = 2`, failing calls open the breaker.
- The next lookup → ProviderUnavailable with **no** new request reaching WireMock.

**Cancellation**
- A pre-cancelled token → `OperationCanceledException`.
- A token cancelled during a backoff wait → `OperationCanceledException`, not Failed.

**Request shape**
- `t`, `type=movie` and `apikey` are present.
- `y` is present only when `ReleaseYear` is supplied.
- A title with `&`, `#`, space or non-ASCII characters arrives correctly escaped.

**Secrecy**
- Across 401, 500, timeout and Found runs, the sentinel key appears nowhere: not in captured log messages, structured state values, exception `ToString()`, or the tags, names and status descriptions of `Activity` objects captured by an `ActivityListener` on the LamuFlix source and on `System.Net.Http`.

**Telemetry**
- One `Metadata.Lookup` activity per call, parented to an ambient test activity, with the outcome and status tags.

**Health**
- The `omdb` check is registered with the `ready` tag.
- The `omdb` check is Healthy initially and after Found.
- It is Degraded after ProviderUnavailable or RateLimited, and Unhealthy after a 401.

**Composition**
- `AddOmdbMetadataProvider` resolves `IMetadataProvider` as `OmdbMetadataProvider` (a typed client).
- Registering it before `AddLamuFlixRabbitMq` makes `IsConsumerActive` true (registration-time evaluation, R:214). The test asserts the active-path registrations, including the `EnrichmentConsumer` hosted service. This is the coverage for the Api's new runtime shape, because no Api host test exists or is added (§5.2, [R-4]).
- `AddOmdbMetadataProvider` alone (no persistence) still resolves `TimeProvider`, and a pre-registered fake `TimeProvider` wins. A missing `Omdb:ApiKey` or an invalid `Omdb:Resilience` (for example attempt > total, or sampling < 2 × attempt) fails at startup.

**Unit**
- `OmdbResilienceOptions` validation: every range edge and both cross-field rules. This is Core code, so Stryker covers it.

**Not claimed**
- Property tests are opted out (Q10). Bernstein records this in the `DEV-303` task note, and the gate reports SKIP.
- The integration matrix is not presented as a mutation result.

### 5.4 Gate expectations

- Roslyn analyzers, complexity ≤ 15 and InspectCode must exit 0 on every changed `.cs` file. The refactor gate requires complexity ≤ 6. Expected hotspots are the outcome classification and `OmdbResponseMapper`: split them into one helper per status class and per field rather than suppressing.
- Baseline: 0 analyzer and 0 complexity findings on the in-scope files. InspectCode's only pre-existing warning is in `EnrichmentFailureCategory.cs`, which is out of scope (R:129-134). If the gate is run with `-Files`, do not include that file.
- No suppressions without a cited ruling. `dotnet format --verify-no-changes` must be clean.
- `./scripts/run-vulnerable-packages.ps1` must exit 0 with the new packages, including transitive ones. A probe found zero advisories for both pinned versions (R:157-159). Run it with no arguments: `-IncludeTransitive $true` fails to bind under `pwsh -File`, and the no-argument form reads the setting from harness.yml (R:161).
- Full `dotnet test`, including ArchitectureTests, must be green. Vendor naming: "Omdb" may appear only on the Infrastructure adapter types and on vendor config keys (constitution :273-276). `OmdbResilienceOptions` uses the existing `OmdbOptions` naming precedent in Core/Options, so the plan cites the ArchitectureTests rule that permits it, or flags it to Keel as a `needs decision:`.
- Mutation: Core's changed executable code (`OmdbResilienceOptions.Validate`) is in Stryker scope at ≥ 80%. Infrastructure is covered by the explicit matrix (§5.3), not by a mutation claim.

### 5.5 Task-ordering constraints

1. Package pins and csproj references, then the vulnerable-packages gate.
2. `OmdbResilienceOptions`, its `BindAndValidate` line and its unit tests. `TelemetryConstants.MetadataLookup`. These two are independent.
3. The WireMock test fixture and the test configuration helper.
4. `OmdbResponseMapper`, with the mapping and outcome tests (depends on 3).
5. `OmdbMetadataProvider`, `OmdbLog` and the span, with the outcome, request-shape, cancellation, secrecy and telemetry tests (depends on 2, 3 and 4).
6. `AddOmdbMetadataProvider` with the resilience handler. This adds the retry, Retry-After, timeout and breaker tests (depends on 5). The registration test comes here too.
7. `OmdbHealthState` and `OmdbHealthCheck`, with their registration and health tests (depends on 6).
8. Api `Program.cs`, `appsettings.json` and `UserSecretsId`, plus the composition and consumer-activation tests (depends on 6).
9. Gates last: analyzers, complexity at 15 and then 6, InspectCode, format, vulnerable packages, the full suite, and ArchitectureTests.

Tasks that edit the same file are sequenced and never marked `[P]`.

## 6. Constraints carried from rules

- Commits and the PR title follow `DEV-303 - {subject}`.
- The contract chain is untouched: no DTO, OpenAPI or TypeScript changes.
- Work only in this worktree. The main checkout stays clean.
- No secret is committed. `UserSecretsId` is an identifier, not a secret.

## 7. Traceability

| Ticket requirement (task note:13) | Covered by |
|---|---|
| Typed HttpClient + Resilience | §5.1 Registration |
| Exponential backoff, jitter, 5xx/429, Retry-After, breaker, timeout | Q4, Q5, §5.3 status handling, Retry-After and breaker cases |
| Found/NotFound mapping | §5.1 table, §5.3 outcomes |
| 401 → InvalidResponse with key log; 429 → RateLimited; 5xx/timeouts → ProviderUnavailable | §5.1 table, §5.3 |
| Remove the hardcoded fallback; key from env or User Secrets | Q1, Q9, §5.1 Api host |
| WireMock tests: Found, NotFound, 401, 429, 500, transient retry | §5.3 |
| AC: correct classification; no hardcoded keys | §5.3 secrecy and composition cases, review |

## 8. Decision log

Decision changes from `/speckit-analyze` (round 1). Each one replaces the brief text it names. Where a D-entry conflicts with §1–§5, the D-entry wins.

- **D1 — Vendor naming (Patron, CONCLUSIONS.md Analyze ruling, `2ef2c04`; constitution VIII:270, 273-276).**
  - `OmdbResilienceOptions` becomes `MetadataProviderResilienceOptions`. The section key stays `Omdb:Resilience`.
  - The adapter is `public sealed partial class OmdbMetadataProvider`, split across `OmdbMetadataProvider.cs`, `OmdbMetadataProvider.Mapping.cs` and `OmdbMetadataProvider.Log.cs`. It holds three private nested types with functional names: `ResponseMapper`, `Response` (the DTO) and the partial `Log`. The files `OmdbResponseMapper.cs` and `OmdbLog.cs` are not created.
  - The health types are `MetadataProviderHealthCheck` and `MetadataProviderHealthState`, and the check is named `metadata-provider` (still tagged `ready`).
  - The extension class is `MetadataProviderServiceCollectionExtensions`, with the method `AddMetadataProvider`. This supersedes the Q8 name only; the Api placement before `AddLamuFlixRabbitMq` is unchanged.
  - Mapping, log, health and resilience behaviour stay as ruled. The "ArchitectureTests has no vendor-naming rule" claim is removed: it has no recon source, and the naming no longer depends on it.
- **D2 — Span tags (constitution VI:229, "Ad-hoc string literals for telemetry names are forbidden").** No telemetry constant exists for an outcome or status-code tag (R:140), and §2 permits only the `MetadataLookup` constant. So:
  - On `Failed`, the `Metadata.Lookup` span carries only `TelemetryConstants.ErrorType` = the failure category name, and its status is set to `ActivityStatusCode.Error`.
  - On `Found` and `NotFound`, it carries no outcome tag.
  - There is no custom HTTP status-code tag. The status code is already on the `System.Net.Http` child span.
  - This replaces "outcome category and HTTP status code" in §5.1 Telemetry and §5.3 Telemetry. The telemetry test asserts one activity per call, its parent, `ErrorType` present on a failure, and `ErrorType` absent on `Found`.
- **D3 — Accessibility (CS0051).** The public adapter's primary constructor takes the health state, so `MetadataProviderHealthState` is a `public sealed class` whose members are all `internal`. `MetadataProviderHealthCheck` stays `internal sealed`. The full constructor is `HttpClient`, `IOptions<OmdbOptions>`, `TimeProvider`, `ILogger<OmdbMetadataProvider>`, `MetadataProviderHealthState`.
- **D4 — Health status for a non-401 InvalidResponse.** This replaces the §5.1 health table gap. The state records the last outcome category **and** whether that outcome was a 401, because the category alone cannot tell a bad key from a malformed body.
  - Healthy: no lookup yet, or Found or NotFound.
  - Degraded: RateLimited, ProviderUnavailable, or InvalidResponse that was not a 401.
  - Unhealthy: a 401, with the description "Check the OMDb API key configuration."
  - The health test adds one case: Degraded after a malformed-JSON 200.
- **D5 — Task ordering (replaces §5.5).** Compile dependencies run in this order:
  1. Packages, then the vulnerable-packages gate.
  2. `MetadataProviderResilienceOptions`, its `BindAndValidate` line and its unit tests. The `MetadataLookup` constant. (The tests depend on the record and its attributes.)
  3. `MetadataProviderHealthState`.
  4. The adapter partial files: classification, mapping, log and span.
  5. `AddMetadataProvider`, with the handler configuration and health-check registration, and `MetadataProviderHealthCheck`.
  6. The WireMock fixture and the config/`ServiceCollection` helper. It calls `AddMetadataProvider`, so it depends on step 5.
  7. The provider tests (mapping, outcomes, request shape, secrecy, telemetry), the resilience tests, and the registration and health tests. All of these depend on step 6.
  8. The Api wiring and the composition tests.
  9. Gates.
  Test code may be written earlier, but the dependency lines must state the real compile dependencies.
- **D6 — Consumer activation test.** `IsConsumerActive` requires both `IMetadataProvider` and `IMovieRepository` (R:28). The composition test therefore registers an `IMovieRepository` stub on the `ServiceCollection`, following the private nested-fake pattern at `RabbitMqServiceCollectionExtensionsTests.cs` (R:209), and then calls `AddMetadataProvider` before `AddLamuFlixRabbitMq`. It also asserts the inverse: with the provider registered after `AddLamuFlixRabbitMq`, the consumer stays inactive.
- **D7 — Test shape (constitution IX:302).** Repeated cases use `Theory` with `MemberData` instead of repeated `Fact`s: 400/403/404, the blank/`N/A`/missing Title cases, the domain-invalid optional fields, and the range edges.

## 9. Recon resolved (`recon-DEV-303`:142-217)

- **[R-1] Resolved.** Pin `Microsoft.Extensions.Http.Resilience` **10.10.0**, the latest stable version, which has a native net10.0 group (R:150, 154). Pin `WireMock.Net` **2.18.0**, the latest stable version. Its highest target is net8.0, which net10.0 can consume, so this is accepted (R:151, 155). Neither version has advisories, top-level or transitive (R:159). IntegrationTests already references Infrastructure directly, so it needs no `ProjectReference` change (R:164-166). → Q3 row, §5.2, §5.4.
- **[R-2] Resolved.** There is one existing check, tagged `ready`, and it is registered at the `AddCheck` call site (R:173-177). The `omdb` check uses `tags: ["ready"]`. No health endpoint is mapped anywhere (R:179-182), and DEV-303 maps none (it would be a new route, §2.3 item 4). → §5.1 health check, §5.3 health.
- **[R-3] Resolved.** There are three separate `private`/`internal static` ActivitySource instances, all named `LamuFlix`, and none is in DI (R:188-194). The provider follows that pattern with its own `private static readonly` source, using the same name and adding no new surface. `TimeProvider` is registered in the Api through `AddLamuFlixPersistence`'s `TryAddSingleton` (R:196-197). `AddOmdbMetadataProvider` adds the same `TryAddSingleton`, so the registration does not depend on persistence (R:199). → §5.1 registration, adapter flow and telemetry.
- **[R-4] Resolved.** No test boots the Api host, and no test composes `AddLamuFlixRabbitMq` with the Api registrations (R:204-213). No existing test is edited, and no Api host test is added. The Api's activation flip and its new `EnrichmentConsumer` hosted service are covered by the `ServiceCollection` composition test. Registering after `AddLamuFlixRabbitMq` would leave the guard blind (R:214), so registration order is part of the closing bar (§1 item 4). → §5.2, §5.3 composition, §5.5.
