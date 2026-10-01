# Feature Specification: OMDb Metadata Provider Adapter

**Feature Branch**: `feature/303-spec`
**Created**: 2026-10-01
**Status**: gate1: provisional
**Input**: Ticket DEV-303 (parent DEV-283, Size M, UI false); `brief.md` (§8 D1–D7 supersedes §1–§5 where they conflict), `CONCLUSIONS.md` (Q1–Q12 and the analyze naming ruling `2ef2c04`), `ASSUMPTIONS.md`, `recon-DEV-303` [R-1]–[R-4], `specs/PRODUCT.md`

## Scope

**In scope**: a sealed `OmdbMetadataProvider` implementing `LamuFlix.Core.Ports.IMetadataProvider` in the existing `src/LamuFlix.Infrastructure/Adapters/` folder, behind a typed `HttpClient` with the standard resilience handler; its private nested response DTO, mapping, logs and span; `AddMetadataProvider`; `MetadataProviderResilienceOptions` bound under `Omdb:Resilience` and validated at startup; the `TelemetryConstants.MetadataLookup` constant; the `metadata-provider` health check; Api `Program.cs`, Api `appsettings.json` `Omdb:BaseUrl` and a new Api `UserSecretsId`; the package pins and references named by Q3; the WireMock.Net test matrix (Q10).

**Out of scope** (Q12, §2.3 items 1–6): the legacy `LamuFlix.Worker` (its provider, interface and `Program.cs`), `LamuFlix.Web` and `tests/LamuFlix.Test` — not deleted, and their pre-existing NU1010/CS0246 build errors are not fixed (Q2); `EnrichmentFailureCategory.cs` and `EnrichmentFailureClassifier` (Q6); the Core port surface — `IMetadataProvider`, `MetadataLookup`, `MetadataLookupResult`, `MovieMetadata` and the value objects are unchanged (Q7); schema and migrations; API routes, OpenAPI and `/web`; response caching; imdbId lookups (`MetadataLookup` carries Title and ReleaseYear only); `Features:LocalPlay` and `Process.Start`; any package beyond `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Http` and `WireMock.Net` (Q3, Q5); a second `ActivitySource` or `Meter` and custom metrics (Q11); a test-only public API seam (Q5).

**Ticket-text interpretation (recorded, not escalated; Q1)**: the ticket asks for removal of a hardcoded `b5a4e6d9` API-key fallback. That literal exists nowhere in the tree or in git history (`recon-DEV-303`:42-49), so nothing is removed. The ticket's "no hardcoded API keys" acceptance criterion is met because the new adapter reads the key only from validated `OmdbOptions.ApiKey`, which has no default, and startup validation plus the secrecy tests prove the new path.

**Vendor naming (D1, constitution VIII:270, 273-276; Patron `2ef2c04`)**: the vendor name is permitted only on the Infrastructure adapter class and on vendor-mandated configuration keys. So the Core options record is `MetadataProviderResilienceOptions` while its section key stays `Omdb:Resilience`; the adapter's mapper, DTO and log are private nested types of `OmdbMetadataProvider` named `ResponseMapper`, `Response` and `Log`; the health types are `MetadataProviderHealthCheck` and `MetadataProviderHealthState` and the check identifier is `metadata-provider`; the registration is `MetadataProviderServiceCollectionExtensions.AddMetadataProvider`; and no test or helper type carries the vendor name. The existing `OmdbOptions` is untouched.

**Recon resolved (brief §9)**: [R-1] package pins, `Microsoft.Extensions.Http.Resilience` 10.10.0 and `WireMock.Net` 2.18.0, both without advisories; [R-2] the single existing health check is registered at its `AddCheck` call site with the `ready` tag and no health endpoint is mapped anywhere in the repo; [R-3] three separate private/internal static `ActivitySource` instances already share the name `LamuFlix`, none in DI, and `TimeProvider` is registered through `AddLamuFlixPersistence`'s `TryAddSingleton`; [R-4] no test boots the Api host or composes `AddLamuFlixRabbitMq` with the Api registrations, so no existing test is edited and no Api host test is added.

## User Scenarios & Testing

### User Story 1 - Metadata Lookup Through the Provider Port (Priority: P1)

As the enrichment consumer, I need the Core `IMetadataProvider` port filled by a real adapter, so that a movie pending enrichment is looked up at OMDb and enriched instead of sitting idle.

**Why this priority**: the Core port has no implementation anywhere in the tree (`recon-DEV-303`:24), so without this story DEV-283's enrichment path is dead.

**Independent Test**: run `FindAsync` against a WireMock OMDb stub and assert the returned `MetadataLookupResult`.

**Acceptance Scenarios**:

1. **Given** a `Response: "True"` body with every field present **When** `FindAsync` runs **Then** `Found(metadata)` carries Title, Plot as synopsis, the leading four digits of `Year` as release year, `Runtime` minutes, the invariant-parsed rating and the `imdbID` (Q7).
2. **Given** a `Response: "True"` body whose optional fields are `N/A` or absent **Then** those fields are null and the result is still `Found`.
3. **Given** a value that will not parse or is outside its domain — an out-of-range year, a non-positive runtime, a rating above 10, a malformed `imdbID` — **Then** only that field is null and the result is still `Found` (Q7).
4. **Given** `Year` of `"2010–2014"` **Then** the release year is 2010 (Q7).
5. **Given** a `pt-BR` current culture **Then** the rating still parses to the same invariant value (Q7).
6. **Given** `Response: "False"` with `Error` `"Movie not found!"` **Then** the result is `NotFound` (Q6).
7. **Given** `Response: "False"` with any other `Error` **Then** the result is `Failed(InvalidResponse)` (Q6).

**Why this priority**: the lookup and its mapping are the ticket's deliverable.

**Independent Test**: as above, one WireMock stub per case.

### User Story 2 - Correct Classification of Every Provider Outcome (Priority: P1)

As an operator, I need every provider outcome mapped to the one failure category the enrichment state machine understands, so that retryable and terminal failures are handled the same way as every other enrichment source.

**Why this priority**: the classification contract decides whether a movie is retried or dead-lettered.

**Acceptance Scenarios**:

1. **Given** HTTP 401 **Then** `Failed(InvalidResponse)` with a warning log advising the operator to check the OMDb API key configuration, after exactly one request (Q6, Q11, [assumed] copy).
2. **Given** HTTP 429 as the final outcome after retries **Then** `Failed(RateLimited)` (Q6).
3. **Given** 5xx or 408 as the final outcome, an attempt timeout, an open circuit, a transport failure, or a total-timeout cancellation the caller did not request **Then** `Failed(ProviderUnavailable)` (Q6).
4. **Given** any other 4xx, a 200 carrying malformed JSON, a 200 whose types are incompatible with the response DTO, a missing or unrecognised `Response`, or a missing, blank or `N/A` Title **Then** `Failed(InvalidResponse)` (Q6, Q7).
5. **Given** a caller's token is cancelled — before the call, during a retry wait, or while the body is read **Then** `OperationCanceledException` is rethrown, never a `Failed` result (Q6).
6. **Given** a programming error **Then** it propagates; only the listed exception types are caught (Q6).

### User Story 3 - Transient Failures Survive; Persistent Ones Do Not (Priority: P1)

As a library with a metered OMDb key, I need transient provider failures retried with backoff, oversized `Retry-After` values bounded by the total budget, and a breaker that stops hammering a dead provider, so that enrichment degrades gracefully rather than amplifying an outage.

**Why this priority**: it is the ticket's explicit resilience requirement.

**Acceptance Scenarios**:

1. **Given** a 503 followed by a 200 **Then** the lookup is `Found` after exactly two requests (ticket's transient-retry verification, Q10).
2. **Given** a 429 always **Then** the result is `Failed(RateLimited)` after `1 + MaxRetryAttempts` requests (Q10).
3. **Given** a 500 always **Then** the result is `Failed(ProviderUnavailable)` after `1 + retries` requests (Q10).
4. **Given** a 429 with `Retry-After: 1` followed by a 200 **Then** the gap between the two requests is at least the header value, and the header delay is never shortened (Q4, Q10).
5. **Given** a `Retry-After` larger than the total timeout **Then** the outcome is `Failed(ProviderUnavailable)` once the budget is spent and no second request precedes the header delay (Q4, Q10).
6. **Given** a response delayed beyond the per-attempt timeout **Then** the outcome is `Failed(ProviderUnavailable)` after retries (Q10).
7. **Given** an unreachable server **Then** `Failed(ProviderUnavailable)` (Q10).
8. **Given** failures that open the breaker **Then** the next lookup is `Failed(ProviderUnavailable)` with no new request reaching the provider (Q10).
9. **Given** HTTP 400, 403, 404 or 401 **Then** exactly one request is made; these are never retried (Q4, Q6).
10. **Given** a token cancelled during a backoff wait **Then** `OperationCanceledException`, not `Failed` (Q6).

### User Story 4 - The Request Is Well Formed and the Key Is Never Exposed (Priority: P1)

As an operator holding a paid OMDb key, I need the request built correctly and the key kept out of every diagnostic surface, so that lookups work and the key is not leaked by the thing built to help diagnose them.

**Why this priority**: a malformed request silently finds nothing, and a leaked key costs money.

**Acceptance Scenarios**:

1. **Given** any lookup **Then** `t`, `type=movie` and `apikey` are present, and `y` appears only when a release year was supplied (Q7).
2. **Given** a title containing `&`, `#`, a space or non-ASCII characters **Then** the provider receives it correctly escaped (Q7).
3. **Given** any of the 401, 5xx, timeout and `Found` runs **Then** the API key appears nowhere: not in captured log messages, not in structured state values, not in exception `ToString()`, and not in the names, tags or status descriptions of activities captured on the LamuFlix source or on `System.Net.Http` (Q9, Q10).
4. **Given** host configuration **Then** the shipped `appsettings.json` carries only the non-secret `Omdb:BaseUrl`; the key is supplied through `Omdb__ApiKey` or Development User Secrets, and it has no default anywhere (Q1, Q9).

### User Story 5 - Observability and Dependency Health (Priority: P2)

As an operator diagnosing a stalled enrichment queue, I need each lookup visible in the existing trace and the metadata provider's recent state reported by the readiness check, so that a provider problem is distinguishable from a queue problem.

**Why this priority**: constitution VI requires a span per external dependency and a health check for each; the ticket inherits that requirement (Q11).

**Acceptance Scenarios**:

1. **Given** one lookup under an ambient activity **Then** exactly one `Metadata.Lookup` activity is created, parented to it, carrying the outcome tag the ruling allows and nothing else (Q11, D2).
2. **Given** a `Failed` lookup **Then** the span carries `TelemetryConstants.ErrorType` with the failure category name and its status is `ActivityStatusCode.Error`; **Given** a `Found` or `NotFound` lookup **Then** it carries no outcome tag at all (D2, constitution VI:229).
3. **Given** any lookup **Then** the span carries no HTTP status-code tag of its own: the status code is already recorded on the `System.Net.Http` child span, and no ad-hoc telemetry string literal is introduced (D2).
4. **Given** no lookup yet, or a lookup that was `Found` or `NotFound` **Then** the `metadata-provider` health check is Healthy.
5. **Given** a lookup that was `RateLimited` or `ProviderUnavailable`, or an `InvalidResponse` that was **not** a 401 — a malformed body, for example — **Then** the check is Degraded (D4).
6. **Given** a lookup rejected with 401 **Then** the check is Unhealthy, describing that the OMDb API key configuration should be checked (Q11, [assumed] copy).
7. **Given** the Api host **Then** no health endpoint is mapped by this ticket; none is mapped today (Q12, [R-2]).

### User Story 6 - Configuration Is Validated, Not Hardcoded (Priority: P2)

As an operator tuning provider behaviour, I need the retry and breaker numbers to come from validated configuration rather than constants in the code, so that an invalid combination fails at startup instead of on the first lookup.

**Why this priority**: Q5 chose bound, validated options over constants and over a test-only seam.

**Acceptance Scenarios**:

1. **Given** a missing `Omdb:ApiKey` or an `Omdb:Resilience` value outside the library's own validation range, an attempt timeout above the total, or a sampling duration below twice the attempt timeout — **Then** startup fails (Q5, D-range table in `plan.md`).
2. **Given** no configuration at all under `Omdb:Resilience` **Then** the documented defaults apply: three retries, a one-second base delay, a ten-second attempt timeout, a thirty-second total timeout, failure ratio 0.1, sampling 30 s, minimum throughput 100 and a five-second break (Q4, Q5).
3. **Given** the host container **Then** the registration does not depend on persistence being registered: `TimeProvider` resolves on its own, and a pre-registered `TimeProvider` still wins ([R-3]).

### Edge Cases

- `ReleaseYear.TryCreate` validates against `TimeProvider.GetUtcNow()`, so a year more than five years ahead is domain-invalid and becomes null (Q7).
- `ImdbRating.TryCreate` accepts only 0.0–10.0 with at most one decimal place; `"7.85"` is domain-invalid and becomes null, not a rounded value (Q7).
- A 429 that is still in progress when the total budget expires is `ProviderUnavailable`, not `RateLimited`: the final outcome decides (Q6).
- The key travels in the query string, so `System.Net.Http` query redaction must stay enabled and no request URI may be logged (Q9).
- Four HTTP calls are an upper bound, not a guarantee: a large `Retry-After` can end the call at the total timeout with fewer requests (Q4).
- The health check is passive and makes no network call: a live probe would spend the daily request quota and put the key on the wire (Q11).
- A malformed 200 body is Degraded, not Unhealthy: only a 401 points at the key, so the health state records the failure category **and** whether that outcome was a 401 (D4).
- The breaker predicate and the retry predicate are the same set, so `BrokenCircuitException` is produced by the breaker and is classified, never retried (Q4, Q6).
- Nothing throws for an expected provider outcome; the port returns a result in every case except caller cancellation (Q6).

## Requirements

### Functional Requirements

- **FR-001**: A `public sealed partial class OmdbMetadataProvider` in `src/LamuFlix.Infrastructure/Adapters/` implements `LamuFlix.Core.Ports.IMetadataProvider.FindAsync`, spanning `OmdbMetadataProvider.cs`, `OmdbMetadataProvider.Mapping.cs` and `OmdbMetadataProvider.Log.cs`, with the private nested types `ResponseMapper`, `Response` (the DTO) and `Log`. It is the one type permitted to carry the vendor name (D1, constitution VIII:273-276, Patron `2ef2c04`). `IMetadataProvider` here is always the Core port, never the legacy `LamuFlix.Worker.Services.IMetadataProvider` (Q2).
- **FR-002**: The provider's primary constructor takes `HttpClient`, `IOptions<OmdbOptions>`, `TimeProvider`, `ILogger<OmdbMetadataProvider>` and `MetadataProviderHealthState`; the API key comes from `IOptions<OmdbOptions>`. `FindAsync` is a thin orchestrator over private helpers, so every method stays within the refactor complexity gate (Q6, D3, [R-3]).
- **FR-003**: The provider returns `Found`, `NotFound` or `Failed(category)` per the Q6 table: caller cancellation is rethrown; 200 + `Response:"True"` + valid title is `Found`; `Response:"False"` + `"Movie not found!"` is `NotFound`; 401 is `Failed(InvalidResponse)`; an exhausted 429 is `Failed(RateLimited)`; a final 5xx/408, `TimeoutRejectedException`, `BrokenCircuitException`, `HttpRequestException` or an unrequested total-timeout cancellation is `Failed(ProviderUnavailable)`; every other case is `Failed(InvalidResponse)`. Only the listed exception types are caught (Q6).
- **FR-004**: The request is built relative to the configured `BaseUrl` with `Uri.EscapeDataString` applied to the key, title and year: `?apikey=…&t=…&type=movie[&y=…]`, and is sent with `GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct)` (Q7).
- **FR-005**: The private nested `ResponseMapper` holds the private nested `Response` DTO (`Response`, `Error`, `Title`, `Year`, `Runtime`, `Plot`, `imdbRating`, `imdbID`, all `string?`, with `[JsonPropertyName]` where the name differs) and maps it. Title must be present, not blank and not `N/A`; `Plot` maps to synopsis with `N/A` or absent becoming null; `Year` takes the leading four digits through `ReleaseYear.TryCreate` with `TimeProvider.GetUtcNow()`; `Runtime` takes the `"<n> min"` form through `Runtime.TryCreate`; `imdbRating` parses with `CultureInfo.InvariantCulture` through `ImdbRating.TryCreate`; `imdbID` goes through `ImdbId.TryCreate`. Every value object goes through its factory, so a parse or domain failure nulls that field alone (Q7, D1).
- **FR-006**: The private nested `partial Log` carries source-generated `[LoggerMessage]` warnings: `InvalidApiKey` (401, with the advice copy), `RateLimited` (429 exhausted), `ProviderUnavailable` (status code if any, category, exception type name) and `InvalidResponse` (status code or "malformed body"). No request URI, API key, title, provider `Error` text, raw exception message or exception object is passed to the logger; movie and trace context is inherited from the enclosing scope and activity (Q11, D1, [assumed] advice copy).
- **FR-007**: `public const string MetadataLookup = "Metadata.Lookup"` is added to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`. The span is opened on the provider's own `private static readonly ActivitySource` named `TelemetryConstants.ActivitySourceName`, matching the pattern in `EnrichmentConsumer.cs:28`, as a child of `Activity.Current`. On `Failed` its only tag is the existing `TelemetryConstants.ErrorType` carrying the failure category name, and its status is `ActivityStatusCode.Error`; on `Found` and `NotFound` it carries no outcome tag. There is no HTTP status-code tag — the code is already on the `System.Net.Http` child span — and no title, URI or key tag, no ad-hoc telemetry string literal, no second source, no meter, and `PipelineActivity.Source` is neither reused nor widened (Q11, D2, [R-3]).
- **FR-008**: `MetadataProviderHealthState` is a `public sealed class` singleton — public because the public adapter's primary constructor takes it (CS0051), with every member `internal` — recording the last lookup's failure category, whether that outcome was a 401, and its time from `TimeProvider`. `internal sealed class MetadataProviderHealthCheck : IHealthCheck` reports Healthy for no lookup / Found / NotFound, Degraded for RateLimited / ProviderUnavailable / a non-401 `InvalidResponse`, and Unhealthy with the key-configuration description after a 401. It makes no network call (Q11, D3, D4, [R-2]).
- **FR-009**: `public static class MetadataProviderServiceCollectionExtensions.AddMetadataProvider(this IServiceCollection)` calls `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>`, sets `BaseAddress` from `IOptions<OmdbOptions>.BaseUrl`, adds `.AddStandardResilienceHandler()` configured from `IOptions<MetadataProviderResilienceOptions>` through the `IServiceProvider`-aware overload, calls `services.TryAddSingleton(TimeProvider.System)`, and registers the health check through `AddHealthChecks().AddCheck<MetadataProviderHealthCheck>("metadata-provider", tags: ["ready"])`. It does not call `AddLamuFlixOptions`; hosts already get options from `AddServiceDefaults` (Q5, Q8, D1, [R-2], [R-3]).
- **FR-010**: The standard resilience handler uses exponential backoff with jitter, `MaxRetryAttempts` 3, `BaseDelay` 1 s, `ShouldRetryAfterHeader = true`, attempt timeout 10 s, total timeout 30 s, the default rate limiter, and a breaker at failure ratio 0.1 / sampling 30 s / minimum throughput 100 / break 5 s. Retry and breaker share one `ShouldHandle` predicate: 5xx, 408, 429, `HttpRequestException`, `TimeoutRejectedException`. 401 and other 4xx are never retried. `Retry-After` is honoured and never shortened; `MaxDelay` is not claimed to cap it; a wait past the total budget is cancelled and that outcome is `ProviderUnavailable`. No hedging, no nested or manual retry (Q4).
- **FR-011**: `public sealed record MetadataProviderResilienceOptions` in `src/LamuFlix.Core/Options/`, with `SectionName = "Omdb:Resilience"`, is dependency-free (BCL types only) and is bound and validated through the existing `BindAndValidate<T>` in `ServiceDefaults/Extensions.cs` with `ValidateOnStart`. The section key stays vendor-named because it is a vendor-mandated configuration key; the type name is not (D1). It is a separate record rather than a nested member because DataAnnotations validation does not recurse into nested members, so `OmdbOptions` stays unchanged. Members with their Q4 defaults as initialisers: `MaxRetryAttempts` 3, `BaseDelay` 1 s, `AttemptTimeout` 10 s, `TotalTimeout` 30 s, `FailureRatio` 0.1, `SamplingDuration` 30 s, `MinimumThroughput` 100, `BreakDuration` 5 s. The defaults are not repeated in `appsettings.json`; the record is the single source (Q5, D1).
- **FR-012**: `MetadataProviderResilienceOptions` carries one `[Range]` attribute per member mirroring the pinned library's own validation range — the exact minimum and maximum for all eight members are tabulated in `plan.md` §Approach, read off the resolved `Polly.Core` 8.4.2 and `Microsoft.Extensions.Http.Resilience` 10.10.0 assemblies — and implements `IValidatableObject` with two cross-field rules: `AttemptTimeout ≤ TotalTimeout` and `SamplingDuration ≥ 2 × AttemptTimeout` (the standard handler's own rule), so an invalid configuration fails at startup rather than at first use (Q5).
- **FR-013**: The API key comes only from `Omdb:ApiKey`, supplied as `Omdb__ApiKey` or through Development User Secrets, and has no default. Api `appsettings.json` gains only `"Omdb": { "BaseUrl": "https://www.omdbapi.com/" }`. Api `LamuFlix.Api.csproj` gains a newly generated `UserSecretsId`, not the Worker or Web identity. The key never reaches logs, exception output or telemetry; factory redaction stays on but is not relied on alone (Q1, Q9).
- **FR-014**: `src/LamuFlix.Api/Program.cs` calls `builder.Services.AddMetadataProvider();` after `AddLamuFlixPersistence` and before `AddLamuFlixRabbitMq`, because `IsConsumerActive` is evaluated at registration time (Q8, D1, [R-4]).
- **FR-015**: `Directory.Packages.props` gains exactly two `PackageVersion` entries — `Microsoft.Extensions.Http.Resilience` 10.10.0 and `WireMock.Net` 2.18.0 — and `Microsoft.Extensions.Http` 10.0.1 stays as pinned. `LamuFlix.Infrastructure.csproj` gains `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.Http`; `LamuFlix.IntegrationTests.csproj` gains `WireMock.Net` only and no `ProjectReference`, since it already references Infrastructure and ServiceDefaults directly (Q3, [R-1]).
- **FR-016**: The WireMock.Net matrix covers, at minimum: Found with full mapping; Found with `N/A` and absent optional fields; each domain-invalid optional value (out-of-range year, non-positive runtime, rating above 10, malformed `imdbID`) nulling only that field; `"2010–2014"` → 2010; an invariant rating parse under a `pt-BR` current culture; NotFound; `Response:"False"` with another `Error`; blank, `N/A` and missing Title; a missing `Response`; malformed JSON; DTO-incompatible JSON; 401 with exactly one request and the captured advice log; 400, 403 and 404 each with exactly one request; 429 always with `1 + MaxRetryAttempts` requests; 500 always with `1 + retries` requests; 503-then-200 with exactly two requests; `Retry-After: 1` then 200 measured on a monotonic `Stopwatch`; a `Retry-After` larger than the total timeout; a per-attempt timeout; a connection failure; a real breaker trip at `MinimumThroughput = 2` with no new request; pre-cancelled and mid-backoff cancellation; the request-shape cases; the secrecy sweep across logs, structured state, exception `ToString()` and activities; one `Metadata.Lookup` activity per call with the D2 tag and status assertions; and the health states including Degraded after a malformed-JSON 200 (Q10, D2, D4).
- **FR-017**: Tests build their own `ServiceCollection` with options binding plus `AddMetadataProvider`, so each test gets fresh DI and fresh pipeline state; configuration sets `Omdb:ApiKey` to a sentinel, `Omdb:BaseUrl` to the WireMock loopback URL and `Omdb:Resilience:*` to valid short values inside the library ranges tabulated in `plan.md`. One WireMock server per test class, reset per test; classes sharing a server do not run in parallel. No live OMDb call and no real key. Repeated cases — 400/403/404, the Title variants, the domain-invalid optional fields and the range edges — are `Theory` with `MemberData`, never repeated `Fact`s (Q10, D7, constitution IX:302).
- **FR-018**: Unit tests in `LamuFlix.UnitTests` cover `MetadataProviderResilienceOptions` validation: every `[Range]` edge at its exact tabulated minimum, minimum − 1 and maximum, plus both cross-field rules. Property tests are opted out (Q10).
- **FR-019**: No Core port, DTO, OpenAPI or TypeScript change; no schema or migration; no new project, folder or layer; no existing test is edited; nothing is deleted (Q12, [R-4]).

### Key Entities

- **`MetadataProviderResilienceOptions`**: new validated Core options record under the `Omdb:Resilience` key; the single source of the Q4 numbers.
- **`OmdbMetadataProvider`**: new sealed partial Infrastructure adapter, a typed `HttpClient` implementing the Core `IMetadataProvider`, and the only type carrying the vendor name.
- **`ResponseMapper` and `Response`** (private nested in the adapter): the mapping helper and the response DTO — `Response`, `Error`, `Title`, `Year`, `Runtime`, `Plot`, `imdbRating`, `imdbID`, all `string?`.
- **`Log`** (private nested partial in the adapter): the source-generated warning log methods.
- **`MetadataProviderHealthState` / `MetadataProviderHealthCheck`**: passive last-outcome record and the `metadata-provider` readiness check.
- **`MetadataLookupResult` (`Found` / `NotFound` / `Failed`)**, **`MovieMetadata`**, **`EnrichmentFailureCategory`**: existing Core types, unchanged.
- **`ReleaseYear`, `Runtime`, `ImdbRating`, `ImdbId`**: existing value objects whose `TryCreate` factories the mapper uses.

## Success Criteria

- **SC-001**: Every case in the FR-016 matrix passes against a real local HTTP stub, with no live provider call and no real key.
- **SC-002**: Roslyn analyzers, complexity ≤ 15, InspectCode and `dotnet format --verify-no-changes` exit 0 on every changed `.cs` file; the refactor gate holds complexity ≤ 6 across **all** changed code, not only the outcome classification and the mapper; the full `dotnet test` suite, including ArchitectureTests, is green.
- **SC-003**: The API key sentinel appears in none of the captured log messages, structured state values, exception output, or activity names, tags and status descriptions across the 401, 5xx, timeout and `Found` runs.
- **SC-004**: `./scripts/run-vulnerable-packages.ps1` exits 0 with the two new packages, transitive dependencies included.
- **SC-005**: Mutation testing on the changed Core executable code (`MetadataProviderResilienceOptions.Validate`) meets the repository threshold; the integration matrix is not presented as a mutation result.
- **SC-006**: On a plain `ServiceCollection` holding an `IMovieRepository` stub, registering the provider **before** `AddLamuFlixRabbitMq` produces the enrichment consumer's active-path registrations, and registering it **after** `AddLamuFlixRabbitMq` leaves the consumer inactive — the guard is evaluated at registration time.
- **SC-007**: A missing `Omdb:ApiKey`, or an `Omdb:Resilience` value outside a tabulated `[Range]` or breaking either cross-field rule, fails at startup rather than at first lookup.

## Assumptions

- The WireMock stub stands in for the live provider in every test; the shipped `Omdb:BaseUrl` is the only place the real host is named (Q9).
- `TimeProvider` in tests is the existing `FixedTimeProvider` pattern (`tests/LamuFlix.IntegrationTests/FixedTimeProvider.cs`), not a new time-testing package (Q3, Q5).
- Property-test opt-out is ticket-scoped, recorded in the DEV-303 task note, and reported as SKIP rather than PASS (Q10).
- The 401 warning advice copy — "Check the OMDb API key configuration." — is a Patron taste assumption recorded in `ASSUMPTIONS.md` (Q11).

## Traceability

Ticket task note:13 — typed HttpClient + resilience → US3, FR-010, FR-015; exponential backoff, jitter, 5xx/429, Retry-After, breaker, timeout → US3, FR-010; Found/NotFound mapping → US1, FR-005; 401 → InvalidResponse with a key log, 429 → RateLimited, 5xx/timeouts → ProviderUnavailable → US2, FR-003, FR-006; remove the hardcoded fallback and source the key from env or User Secrets → US4, FR-013 (and Q1: nothing existed to remove); WireMock cases for Found, NotFound, 401, 429, 500 and the transient retry → US1–US3, FR-016; AC on correct classification and no hardcoded keys → US2, US4, SC-003. Constitution VI:229 observability and constitution IX:302 test shape → US5, US2/US3, FR-007, FR-017; readiness of the dependency and startup validation → US5, US6, FR-008, FR-011, FR-012; the Api's enrichment consumer actually activating → US6, FR-014, SC-006.
