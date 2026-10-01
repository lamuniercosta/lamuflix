# DEV-303 — Grill questions (Keel → Patron)

Ticket: DEV-303 — Implement `OmdbMetadataProvider` as an `IMetadataProvider` (typed HttpClient, Microsoft.Extensions.Http.Resilience, WireMock.Net tests). Size M, parent DEV-283.
Facts: `recon-DEV-303` (cited as `R§n:line`). Cap: 12 questions, one round.
Each question carries Keel's recommendation. Rule on each, cite your basis, record it in `specs/DEV-303/CONCLUSIONS.md`, and log taste rulings `[assumed]` in `ASSUMPTIONS.md`. Use `blocked: structural — <question>` only for the two §2.3 escalations (ticket-text change or constitution departure).

---

**Q1 — The `b5a4e6d9` fallback does not exist (R§2:42-53).** Nothing in the tree or git history contains it. The legacy Worker provider already throws when the key is missing. Ticket AC: "no hardcoded API keys."
Recommendation: this is not a ticket change. Treat the "remove" clause as already satisfied, and record that as recon evidence. The AC is met by the new adapter: it reads the key only from `OmdbOptions.ApiKey`, has no default, and is covered by a test plus a review check. The spec states that nothing was removed, and why.

**Q2 — Placement and the legacy provider.** The Core port `LamuFlix.Core.Ports.IMetadataProvider` has no implementation (R§1:24). The legacy `LamuFlix.Worker.Services.OmdbMetadataProvider` implements the Worker interface. The Worker is outside the solution and does not build (R§3:89).
Recommendation: create a new `sealed` class `OmdbMetadataProvider` in the existing `src/LamuFlix.Infrastructure/Adapters/` folder (R§6:112), which implements the Core port. Do not touch the legacy Worker provider, the Worker `Program.cs`, or the `LamuFlix.Test` tests (§2.3 item 6). Under constitution :273-276, the vendor name is allowed on the adapter class.

**Q3 — New NuGet dependencies (§2.3 item 1, R§6:110).** Add `Microsoft.Extensions.Http.Resilience` (to Infrastructure) and `WireMock.Net` (to IntegrationTests). Infrastructure also needs `Microsoft.Extensions.Http`, which is already pinned at 10.0.1 (R§4:94) but is new to that csproj.
Recommendation: approved. The ticket text names both packages, and constitution :323 and :288/:300 mandate them. Pin the latest stable versions compatible with net10.0 in `Directory.Packages.props`, run the vulnerable-packages gate, and add no other packages (in particular, no `Microsoft.Extensions.TimeProvider.Testing`; see Q5).

**Q4 — Resilience pipeline shape.** Choose between `AddStandardResilienceHandler` with configured options and a custom `AddResilienceHandler`. The ticket asks for exponential backoff with jitter on 5xx/429 that honours Retry-After, plus a circuit breaker and a timeout.
Recommendation: `AddStandardResilienceHandler(o => …)`. Its rate limiter is left at the default. Retry: exponential + jitter, `ShouldRetryAfterHeader = true`, 3 retries, base delay 1 s, and `ShouldHandle` limited to 5xx, 408, 429, `HttpRequestException` and the per-attempt timeout. **401 and the other 4xx are never retried.** Per-attempt timeout 10 s, total timeout 30 s, and the circuit breaker at the standard defaults (failure ratio 0.1, sampling 30 s, minimum throughput 100, break 5 s). Rule on the numbers. Should Retry-After be capped, so that a large header value cannot hold the consumer for minutes? Recommendation: a cap of `MaxDelay` = 30 s, aligned with the total timeout.

**Q5 — Configurability and test speed.** Real backoff makes the retry tests slow. `OmdbOptions` holds only `ApiKey` and `BaseUrl` (R§2:56).
Recommendation: keep the resilience numbers as `const` values in the adapter's registration extension. Expose one internal-test seam, an overload of the registration extension that takes an `Action<HttpStandardResilienceOptions>` override. Tests use it to shrink the delays and timeouts to milliseconds. `OmdbOptions` gets no new fields, which keeps it free of config-schema churn. The alternative is to add `Omdb:Resilience:*` options. Which do you rule?

**Q6 — Result vs exception (the classification contract).** The port returns `MetadataLookupResult` (`Found`, `NotFound`, `Failed(category)`; R§1:15). The handler routes `Failed` to `FailAsync` (R§1:27). Separately, `EnrichmentFailureClassifier.Classify(Exception)` already maps exceptions (R§3:87).
Recommendation: the adapter **returns** results and does not throw for every expected outcome. After the pipeline:
- 200 + `Response:"True"` → `Found`
- 200 + `Response:"False"` with `Error` "Movie not found!" → `NotFound`
- 401 → `Failed(InvalidResponse)`, plus a Warning log telling the operator to check the OMDb API key
- 429 (retries exhausted) → `Failed(RateLimited)`
- 5xx / 408, `TimeoutRejectedException`, `BrokenCircuitException` and `HttpRequestException` → `Failed(ProviderUnavailable)`
- other 4xx, malformed JSON, or `Response:"False"` with any other error → `Failed(InvalidResponse)`
- caller-token cancellation → rethrow `OperationCanceledException`

Do not reuse `EnrichmentFailureClassifier`, which is exception-shaped, and leave it unchanged (R§7:134: touching `EnrichmentFailureCategory.cs` pulls in a pre-existing InspectCode warning).

**Q7 — Request and response mapping.** Request: `GET {BaseUrl}?apikey=…&t={title}&type=movie`, plus `&y={year}` when `ReleaseYear` is present. The legacy provider used plain `http` (R§2:52).
Recommendation: build the URL with escaped query values and rely on `BaseUrl` from config (https in the shipped value; see Q9). Response → `MovieMetadata(title, synopsis, releaseYear, runtime, imdbRating, imdbId)` (R§1:16):
- `"N/A"` or a missing field → null
- `Year` takes the leading 4 digits (so `"2010–2014"` gives 2010)
- `Runtime` `"148 min"` → 148
- `imdbRating` is parsed with the invariant culture
- `imdbID` goes through `ImdbId`'s factory
- a value that will not parse → null for that field, never a failure
- a blank `Title` on a `Response:"True"` body → `Failed(InvalidResponse)`

Deserialize with System.Text.Json into a private DTO record.

**Q8 — DI registration and host wiring.** `IsConsumerActive` is evaluated at registration time and needs `IMetadataProvider` registered **before** `AddLamuFlixRabbitMq` (R§1:28-29). Wiring the provider into Api `Program.cs` would switch on the enrichment consumer in the Api host.
Recommendation: add a public `AddOmdbMetadataProvider(this IServiceCollection)` in `Infrastructure/Adapters/`. It registers the typed client, the resilience handler, and `IMetadataProvider → OmdbMetadataProvider`. **Host wiring is in scope:** call it in `src/LamuFlix.Api/Program.cs` before `AddLamuFlixRabbitMq`. Otherwise the ticket delivers an adapter that nothing runs, and DEV-283's enrichment path stays dead. Does activating the consumer in the Api host count as a ticket change? Rule. If you judge it out of scope, the fallback is: extension only, no `Program.cs` edit, and a follow-up ticket.

**Q9 — Key and BaseUrl sourcing (§2.3 item 5, secrets).** `OmdbOptions` is already validated at startup (R§2:57-58). Api `appsettings.json` has no `Omdb` section, and the Api csproj has no `UserSecretsId` (R§2:59-60). The ticket says "environment or User Secrets".
Recommendation:
- (a) Add `"Omdb": { "BaseUrl": "https://www.omdbapi.com/" }` to Api `appsettings.json`. BaseUrl is not a secret, and the ApiKey is never put in any appsettings file.
- (b) Add a `UserSecretsId` to `LamuFlix.Api.csproj` so that User Secrets work in Development, as the ticket text says (constitution :246-251). This is a csproj edit for a project the ticket does not name; is it approved under the ticket's "User Secrets" wording?
- (c) Env var `Omdb__ApiKey`.
- (d) The key must never appear in logs, exception messages or telemetry. The API key travels in the query string, so the adapter must not log the request URI. It relies on the .NET 9+ default query-string redaction in HttpClient logging, and a test asserts that captured logs do not contain the key.

**Q10 — Test strategy.** WireMock.Net in `tests/LamuFlix.IntegrationTests` (constitution :300).
Recommendation, as a matrix:
- Found (full mapping, plus `N/A` fields)
- NotFound (`Response:"False"`)
- 401 → InvalidResponse, Warning log asserted, exactly 1 call (no retry)
- 429 → RateLimited after retries, call count = 1 + retries
- 429 with `Retry-After` honoured: the second attempt waits at least the header value, with a tiny header value under the test override
- 500 → ProviderUnavailable after retries
- **transient 500 then 200 → Found, call count 2** (ticket retry verification)
- per-attempt timeout via a WireMock delay → ProviderUnavailable
- malformed JSON → InvalidResponse
- year present/absent appears in the query string
- caller cancellation rethrows
- the key is absent from logs
- registration: resolving `IMetadataProvider` gives `OmdbMetadataProvider`

The circuit-breaker trip is **not** integration-tested, because minimum throughput 100 makes it impractical; it is asserted only through the configured options. One WireMock server per test class, reset per test. Stryker targets Core only, so the explicit matrix stands in for mutation (DEV-302 D4 precedent). Unit tests for the field parsers in `LamuFlix.UnitTests` are allowed if the parsers are public or internal-visible; otherwise the integration matrix covers them. Property tests: opt out (no new Core invariant). Is that opt-out approved?

**Q11 — Logging and telemetry.** Use source-generated `[LoggerMessage]` for: 401 (Warning, "check the OMDb API key configuration"), 429 exhausted (Warning), and unavailable (Warning, with the exception). No new `ActivitySource` or meter, and no `TelemetryConstants` additions (R§8:140). The `HttpClient` and Polly built-in telemetry is enough. Agree?

**Q12 — Out-of-scope list and round cap.**
Out of scope:
- the legacy Worker/Web/`LamuFlix.Test` (no deletion, no fix of the NU1010 or Web build)
- `EnrichmentFailureCategory.cs` and its pre-existing InspectCode warning
- `EnrichmentFailureClassifier`
- schema/migrations
- API routes/OpenAPI/web
- caching of OMDb responses
- search-by-imdbId lookups (the port's `MetadataLookup` is Title + ReleaseYear only)

Round cap: spec review Keel ↔ Quill, 2 rounds hard cap. Confirm.
