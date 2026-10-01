# DEV-303 — Patron grill rulings

Worktree: F:/Dev/LamuFlix.worktrees/feature-303-spec; branch: feature/303-spec.
Authority: DEV-303 intake/task note:13 (verified ticket text), specs/PRODUCT.md, constitution, current assigned Patron §2.3; recon facts are trusted as supplied by Wisp/Keel. The connected legacy team-charter care-list escalation wording is stale against the current assigned role and PRODUCT §5; only ticket changes and necessary constitution departures escalate. No such departure is authorised here and no owner checkbox is created.

This records the single Q1–Q12 round at the grill cap. It is a docs-only decision receipt, not an implementation or verification receipt; Gate 1 remains closed. No brief, glossary, ADR, implementation or gate1 edit is made by Patron.

---

## Q1

**Keel — question and recommendation:**

**Q1 — The `b5a4e6d9` fallback does not exist (R§2:42-53).** Nothing in the tree or git history contains it. The legacy Worker provider already throws when the key is missing. Ticket AC: "no hardcoded API keys."
Recommendation: this is not a ticket change. Treat the "remove" clause as already satisfied, and record that as recon evidence. The AC is met by the new adapter: it reads the key only from `OmdbOptions.ApiKey`, has no default, and is covered by a test plus a review check. The spec states that nothing was removed, and why.

**Patron — ruling, basis and implications:**

ACCEPT — The absent fallback is already satisfied by recon; remove nothing, state that fact, and require the new adapter to obtain ApiKey exclusively from validated OmdbOptions without a default.

- Basis: DEV-303 task note:13 (no hardcoded keys; environment or User Secrets); recon-DEV-303:42-58 verifies absence and existing validation; constitution:243-251 prohibits secret fallbacks. Missing-key startup validation and a diff review prove the new path; no ticket-text change is required.

---

## Q2

**Keel — question and recommendation:**

**Q2 — Placement and the legacy provider.** The Core port `LamuFlix.Core.Ports.IMetadataProvider` has no implementation (R§1:24). The legacy `LamuFlix.Worker.Services.OmdbMetadataProvider` implements the Worker interface. The Worker is outside the solution and does not build (R§3:89).
Recommendation: create a new `sealed` class `OmdbMetadataProvider` in the existing `src/LamuFlix.Infrastructure/Adapters/` folder (R§6:112), which implements the Core port. Do not touch the legacy Worker provider, the Worker `Program.cs`, or the `LamuFlix.Test` tests (§2.3 item 6). Under constitution :273-276, the vendor name is allowed on the adapter class.

**Patron — ruling, basis and implications:**

ACCEPT — Add a sealed OmdbMetadataProvider implementing LamuFlix.Core.Ports.IMetadataProvider in the existing Infrastructure/Adapters folder; leave the legacy Worker provider, Program and tests untouched.

- Basis: ticket task note:13 requires the provider port; recon-DEV-303:13-24/89/112 distinguishes the legacy interface and existing folder; constitution:273-276 permits the vendor name on the adapter. No new layer or project is authorised.

---

## Q3

**Keel — question and recommendation:**

**Q3 — New NuGet dependencies (§2.3 item 1, R§6:110).** Add `Microsoft.Extensions.Http.Resilience` (to Infrastructure) and `WireMock.Net` (to IntegrationTests). Infrastructure also needs `Microsoft.Extensions.Http`, which is already pinned at 10.0.1 (R§4:94) but is new to that csproj.
Recommendation: approved. The ticket text names both packages, and constitution :323 and :288/:300 mandate them. Pin the latest stable versions compatible with net10.0 in `Directory.Packages.props`, run the vulnerable-packages gate, and add no other packages (in particular, no `Microsoft.Extensions.TimeProvider.Testing`; see Q5).

**Patron — ruling, basis and implications:**

ACCEPT — Approve Microsoft.Extensions.Http.Resilience for Infrastructure, WireMock.Net for IntegrationTests, and the already-pinned Microsoft.Extensions.Http reference for Infrastructure; use CPM and no other new direct package.

- Basis: ticket task note:13 explicitly names both new dependencies; constitution:288/300/318/323 mandates this stack; recon-DEV-303:94-106 establishes current references. Keel must pin concrete stable net10-compatible versions during planning and carry the transitive vulnerability gate; this ruling does not claim versions were verified or packages installed.

---

## Q4

**Keel — question and recommendation:**

**Q4 — Resilience pipeline shape.** Choose between `AddStandardResilienceHandler` with configured options and a custom `AddResilienceHandler`. The ticket asks for exponential backoff with jitter on 5xx/429 that honours Retry-After, plus a circuit breaker and a timeout.
Recommendation: `AddStandardResilienceHandler(o => …)`. Its rate limiter is left at the default. Retry: exponential + jitter, `ShouldRetryAfterHeader = true`, 3 retries, base delay 1 s, and `ShouldHandle` limited to 5xx, 408, 429, `HttpRequestException` and the per-attempt timeout. **401 and the other 4xx are never retried.** Per-attempt timeout 10 s, total timeout 30 s, and the circuit breaker at the standard defaults (failure ratio 0.1, sampling 30 s, minimum throughput 100, break 5 s). Rule on the numbers. Should Retry-After be capped, so that a large header value cannot hold the consumer for minutes? Recommendation: a cap of `MaxDelay` = 30 s, aligned with the total timeout.

**Patron — ruling, basis and implications:**

CHANGE — Use one AddStandardResilienceHandler with exponential jitter, three retries, 1 s base delay, retry/circuit predicates for 5xx/408/429/HttpRequestException/TimeoutRejectedException only, attempt timeout 10 s, total timeout 30 s, default rate limiter, and breaker ratio 0.1 / sampling 30 s / throughput 100 / break 5 s; honour Retry-After without shortening it and let the total timeout cancel a wait that exceeds the remaining budget.

- Basis: ticket task note:13 and constitution:323 require retry, Retry-After, breaker and timeout. [Microsoft Learn standard handler](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) establishes strategy order, transient predicates and defaults; 1 s base delay is a deliberate policy choice rather than the documented 2 s default. 401/other 4xx and BrokenCircuitException are never retried; no nested/manual retry or hedging is authorised.
- [ShouldRetryAfterHeader](https://learn.microsoft.com/dotnet/api/microsoft.extensions.http.resilience.httpretrystrategyoptions.shouldretryafterheader) uses a delay generator; [Polly MaxDelay](https://www.pollydocs.org/api/Polly.Retry.RetryStrategyOptions-1.html) does not cap generated delays. Do not claim MaxDelay caps Retry-After or send an early retry. A huge header ends at the total timeout as ProviderUnavailable under Q6; four HTTP calls are an upper bound, not guaranteed when the time budget expires.

---

## Q5

**Keel — question and recommendation:**

**Q5 — Configurability and test speed.** Real backoff makes the retry tests slow. `OmdbOptions` holds only `ApiKey` and `BaseUrl` (R§2:56).
Recommendation: keep the resilience numbers as `const` values in the adapter's registration extension. Expose one internal-test seam, an overload of the registration extension that takes an `Action<HttpStandardResilienceOptions>` override. Tests use it to shrink the delays and timeouts to milliseconds. `OmdbOptions` gets no new fields, which keeps it free of config-schema churn. The alternative is to add `Omdb:Resilience:*` options. Which do you rule?

**Patron — ruling, basis and implications:**

CHANGE — Bind the operational retry/breaker/timeout numbers under Omdb:Resilience using a nested dependency-free options record with the Q4 defaults, data-annotation and cross-field validation at startup; keep HttpStandardResilienceOptions in Infrastructure and let tests override configuration with valid shorter intervals and lower breaker throughput.

- Basis: constitution:246-249 requires validated bound options; AGENTS.md non-negotiables prohibit hardcoded thresholds. This chooses Keel's configuration alternative over constants in registration; extending OmdbOptions and its existing ServiceDefaults validation is supporting work for the ticket's resilience delivery, not a new architecture layer or database schema. Keep key/BaseUrl validation mandatory.
- Use the existing options/DI seam, rather than a test-only public API or a new time-testing package. Tests must respect the pinned library's validation minima and keep attempt timeout within the total budget; unrelated options and generated configuration remain untouched.

---

## Q6

**Keel — question and recommendation:**

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

**Patron — ruling, basis and implications:**

ACCEPT — Return Found/NotFound/Failed for expected provider outcomes exactly as recommended; rethrow caller cancellation, including cancellation during retry waits or body reading, and leave the exception-shaped EnrichmentFailureClassifier unchanged.

- Basis: ticket task note:13 specifies 401 → InvalidResponse with key advice, exhausted 429 → RateLimited, and 5xx/timeouts → ProviderUnavailable; recon-DEV-303:15/27/87 identifies the result contract and caller behaviour. Other 4xx, invalid bodies and unrecognised false responses are InvalidResponse; timeout, circuit-open and transport failures are ProviderUnavailable.
- Caller cancellation takes precedence when the supplied token is cancelled; do not catch unrelated programming errors into a provider failure. Exhausted 429 is RateLimited only when the final outcome is 429, rather than an overriding total timeout (Q4).

---

## Q7

**Keel — question and recommendation:**

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

**Patron — ruling, basis and implications:**

ACCEPT — Use escaped title/year/key query values and type=movie against configured BaseUrl, deserialize into a private System.Text.Json DTO, and apply the recommended invariant mapping with null optional fields; missing/blank/N/A Title or an absent/unrecognised Response discriminator is InvalidResponse.

- Basis: recon-DEV-303:14/16/56 defines lookup, required title and config; ticket task note:13 requires successful mapping. Map Plot to synopsis, leading four-digit Year, min-suffixed runtime, invariant decimal rating and ImdbId.TryCreate; use TryCreate for every value object so a parsed but out-of-domain optional value also becomes null. ReleaseYear validation uses injected TimeProvider.GetUtcNow, never ambient time (constitution VII; existing ReleaseYear.TryCreate).
- Missing/null optional fields and N/A become null; field-format/domain failures do not discard otherwise valid metadata. Structurally malformed JSON and non-string fields incompatible with the private DTO remain InvalidResponse. Preserve the Core port and value-object surface.

---

## Q8

**Keel — question and recommendation:**

**Q8 — DI registration and host wiring.** `IsConsumerActive` is evaluated at registration time and needs `IMetadataProvider` registered **before** `AddLamuFlixRabbitMq` (R§1:28-29). Wiring the provider into Api `Program.cs` would switch on the enrichment consumer in the Api host.
Recommendation: add a public `AddOmdbMetadataProvider(this IServiceCollection)` in `Infrastructure/Adapters/`. It registers the typed client, the resilience handler, and `IMetadataProvider → OmdbMetadataProvider`. **Host wiring is in scope:** call it in `src/LamuFlix.Api/Program.cs` before `AddLamuFlixRabbitMq`. Otherwise the ticket delivers an adapter that nothing runs, and DEV-283's enrichment path stays dead. Does activating the consumer in the Api host count as a ticket change? Rule. If you judge it out of scope, the fallback is: extension only, no `Program.cs` edit, and a follow-up ticket.

**Patron — ruling, basis and implications:**

ACCEPT — Add public AddOmdbMetadataProvider(IServiceCollection) with a typed-client registration for the Core port and invoke it in Api Program before AddLamuFlixRabbitMq; activating the existing enrichment consumer is authorised composition work for this adapter.

- Basis: ticket task note:13 requires a usable typed-client adapter; recon-DEV-303:27-31 proves the existing registration-time consumer guard and ADR-0004 conditional composition; specs/PRODUCT.md:8-10 identifies backend/background enrichment. This fills the existing port without adding a new hosted process, changing RabbitMQ ownership, or editing the guard; §2.3 item 6 permits a cited supporting-file ruling and does not itself create an owner checkbox.

---

## Q9

**Keel — question and recommendation:**

**Q9 — Key and BaseUrl sourcing (§2.3 item 5, secrets).** `OmdbOptions` is already validated at startup (R§2:57-58). Api `appsettings.json` has no `Omdb` section, and the Api csproj has no `UserSecretsId` (R§2:59-60). The ticket says "environment or User Secrets".
Recommendation:
- (a) Add `"Omdb": { "BaseUrl": "https://www.omdbapi.com/" }` to Api `appsettings.json`. BaseUrl is not a secret, and the ApiKey is never put in any appsettings file.
- (b) Add a `UserSecretsId` to `LamuFlix.Api.csproj` so that User Secrets work in Development, as the ticket text says (constitution :246-251). This is a csproj edit for a project the ticket does not name; is it approved under the ticket's "User Secrets" wording?
- (c) Env var `Omdb__ApiKey`.
- (d) The key must never appear in logs, exception messages or telemetry. The API key travels in the query string, so the adapter must not log the request URI. It relies on the .NET 9+ default query-string redaction in HttpClient logging, and a test asserts that captured logs do not contain the key.

**Patron — ruling, basis and implications:**

ACCEPT — Ship only nonsecret Omdb:BaseUrl=https://www.omdbapi.com/ in Api appsettings, add a new nonsecret UserSecretsId to Api csproj, accept Omdb__ApiKey/environment or Development User Secrets, and prevent the key from reaching logs, exception output or telemetry.

- Basis: ticket task note:13 expressly authorises environment/User Secrets; constitution:243-251 and recon-DEV-303:55-61 establish the options/startup seam and missing host support. These specific Api config/project edits are approved supporting work; no actual key, copied legacy secrets identity, secret fallback or new secret provider is authorised. WireMock may use a loopback HTTP BaseUrl.
- [Microsoft Learn query redaction](https://learn.microsoft.com/dotnet/core/compatibility/networking/9.0/query-redaction-logs) covers IHttpClientFactory logging only. Keep that redaction enabled; never log request URI/body/provider Error or raw exception text that can echo the URI/key. Inspect captured structured state, exception output and emitted activity tags as well as rendered messages; do not assume log redaction proves telemetry safety.

---

## Q10

**Keel — question and recommendation:**

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

**Patron — ruling, basis and implications:**

CHANGE — Approve the proposed WireMock matrix and property-test opt-out, but also exercise breaker opening/short-circuit behaviour with test-configured MinimumThroughput=2, total timeout for oversized Retry-After, and key-free telemetry/exception output; assertions of options alone do not prove breaker behaviour.

- Basis: ticket task note:13 mandates WireMock cases and transient-error verification; constitution:288-305/323 requires real provider infrastructure; [Polly breaker options](https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html) permits throughput >=2, so the production value 100 is not a reason to omit a behaviour test. Use fresh DI/pipeline state per test and reset the class server; exclude shared-state parallel interference. Keep retry call-count assertions, mapping/domain-invalid optional values, all non-retry 4xx behaviour, registration order/consumer activation, and cancellation during backoff.
- Retry-After uses a valid header, such as integer delta-seconds 1, not fractional milliseconds. Observe request timing with a monotonic test clock and bounded tolerance; include a header greater than the total test budget and verify no premature retry. No live OMDb call or API key is used.
- harness.yml:31-36 explicitly permits a ticket-scoped property opt-out with no new domain invariant: Bernstein records it in the task note, leaves the repository switch enabled, and reports SKIP rather than PASS. Constitution:290 scopes Stryker to Core; do not call this matrix a mutation pass or waive mutation of changed executable Core code, should any be introduced.

---

## Q11

**Keel — question and recommendation:**

**Q11 — Logging and telemetry.** Use source-generated `[LoggerMessage]` for: 401 (Warning, "check the OMDb API key configuration"), 429 exhausted (Warning), and unavailable (Warning, with the exception). No new `ActivitySource` or meter, and no `TelemetryConstants` additions (R§8:140). The `HttpClient` and Polly built-in telemetry is enough. Agree?

**Patron — ruling, basis and implications:**

CHANGE — Use source-generated structured warning logs with the recommended key-configuration advice, safe status/category/exception-type fields and inherited movie/attempt context; reuse the existing LamuFlix ActivitySource, add the Metadata.Lookup name to TelemetryConstants, and register the OMDb dependency health check through existing health wiring.

- Basis: constitution VI:220-236 explicitly requires the Metadata.Lookup span, central names, error context and a health check for every new external dependency; recon-DEV-303:140 establishes the missing constant. Built-in HTTP/Polly telemetry is useful but does not satisfy the named span. The constant addition and bounded health registration are forced conformance edits under §2.3 item 6, not a constitution departure or ticket expansion; do not create a second source/meter or duplicate ServiceDefaults exporter setup.
- Start the lookup Activity under the current enrichment Activity and preserve its trace; use inherited context rather than extending MetadataLookup with movie identity. Emit no request URI, API key, title or provider Error in logs/spans; omit raw exceptions when their text is untrusted. [assumed] Warning advice copy: Check the OMDb API key configuration. New custom metrics are not required for this adapter.

---

## Q12

**Keel — question and recommendation:**

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

**Patron — ruling, basis and implications:**

ACCEPT — Freeze DEV-303 to the adapter, typed-client/resilience registration, validated resilience options, explicitly approved Api composition/config/UserSecretsId edits, required telemetry constant/health support, and corresponding tests/package references; retain the listed legacy/classifier/schema/API/web/caching/imdbId exclusions and a hard two-round Keel↔Quill spec-review cap.

- Basis: ticket task note:13 and recon-DEV-303:89/114-118/134 bound the change; Q3/Q5/Q8/Q9/Q11 identify the deliberate supporting edits. No cleanup or repair of legacy Worker/Web/LamuFlix.Test, no schema, no Core port changes, no new endpoint/OpenAPI/web contract, no cache and no imdbId lookup. Anything else is a follow-up issue, not a finding in this round.
- Apply the existing chain closing bar without re-ruling it: .cursor/rules/agent-pipeline.mdc:72-94 requires frozen scope, hard cap and continued NEEDS FIXES for deferred above-bar findings. No third formal round, silent scope extension or cap reset; no new closing bar is decided by this confirmation.

---

## Analyze ruling — Constitution VIII naming (supersedes Q8 naming only)

(a) ACCEPT — Rename the new Core record to MetadataProviderResilienceOptions; retain the binding key Omdb:Resilience.

- Basis: .specify/memory/constitution.md:270,273-276 requires functional Core/type names and permits vendor configuration keys; VII:246-249 names existing OmdbOptions but grants no exception for a new Core type. Q5's defaults, validation and binding design remain unchanged.

(b) ACCEPT — Use a sealed partial OmdbMetadataProvider across OmdbMetadataProvider.cs, OmdbMetadataProvider.Mapping.cs and OmdbMetadataProvider.Log.cs, with private nested ResponseMapper, Response and partial Log types; use MetadataProviderHealthCheck, MetadataProviderHealthState, MetadataProviderServiceCollectionExtensions and health-check identifier metadata-provider.

- Basis: .specify/memory/constitution.md:273-276 permits the vendor name only on the Infrastructure adapter class itself, not helper types or telemetry identifiers. Private nesting is encapsulation, not a vendor-naming exemption; nested type/member names must remain functional. The ticket (DEV-303 task note:14) explicitly names OmdbMetadataProvider; Q7's mapping and Q11's logging/health behaviour, registration and ready tag remain unchanged.

(c) ACCEPT — Rename AddOmdbMetadataProvider to AddMetadataProvider; this supersedes Q8's member name only, preserving Infrastructure/Adapters placement and Api invocation before AddLamuFlixRabbitMq.

- Basis: .specify/memory/constitution.md:273-276 also governs members; CONCLUSIONS.md:146-148 authorises the existing composition and order. These conformance corrections preserve the ticket's delivery and make no constitution departure, so specs/PRODUCT.md:34-39 requires no owner checkbox. This ruling does not assert clean analysis or open Gate 1.

---

## D18 — Shared BaseUrl transport validation (Sentry M4)

FOLLOW-UP — Require HTTPS for non-loopback OmdbOptions.BaseUrl values through shared startup validation; preserve Q9's loopback HTTP allowance for WireMock. Rigger must check open tickets for this same validation gap and fold into one if present, otherwise file a size:S, 2h follow-up under DEV-283. DEV-303 scope stays frozen; no owner checkbox.

- Basis: src/LamuFlix.Core/Options/OmdbOptions.cs:14-16 accepts a URL without enforcing HTTPS; recon-DEV-303:52/56 and brief.md:351 identify the cleartext API-key exposure. This is broken security validation and the requested follow-up, not an extra DEV-303 deliverable.
- Basis: .specify/memory/constitution.md:246-249 and src/LamuFlix.ServiceDefaults/Extensions.cs:19-24 place validation at the existing shared options/startup seam; CONCLUSIONS.md:165-167 requires the shipped HTTPS endpoint and explicitly permits loopback HTTP for WireMock. The follow-up owns the shared-host configuration impact; no new dependency or validation layer is authorised.
- Basis: CONCLUSIONS.md:237-240 freezes current scope; specs/PRODUCT.md:34-39 and assigned Patron §2.3 require neither ticket change nor constitution departure here. Tracker filing is requested, not yet verified; only Rigger performs and reports that mutation.

---

## D18 recovery — Partial follow-up DEV-393

RECOVER IN PLACE — Retain DEV-393 and its verified approved summary/description; never repeat Create or delete it. Rigger may set size:S now through the existing Edit-YouTrackIssue.ps1 -Ticket DEV-393 -Tag size:S path. Completion requires Type Task, parent DEV-283, estimate 2h and size:S, with State Todo retained and all fields read back; until then the follow-up is incomplete.

- Basis: Rigger's supplied receipt verifies duplicate search, partial creation and exact description but reports HTTP 400 and missing metadata; CONCLUSIONS.md:262 specifies parent, estimate and size. A partial create is not a completed follow-up.
- Basis: scripts/local/Edit-YouTrackIssue.ps1:249-254 creates the issue before a combined metadata command; :275-293 supports tag recovery but not Type/parent/estimate edits. The failing command's cause is unverified: do not blindly replay it, weaken the required metadata or improvise direct tracker writes outside the mandated script.
- Route the tooling gap to Bernstein for an isolated tooling recovery, outside the DEV-303 branch/delivery. The bounded requirement is an explicit existing-issue recovery path in Edit-YouTrackIssue.ps1 that targets DEV-393, applies only missing required metadata using live project-supported fields/values, and verifies each result before reporting completion. Only Rigger runs the tracker mutation after that path exists. No DEV-303 scope change or owner checkbox.
