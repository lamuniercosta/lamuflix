# Feature Specification: DEV-412 OllamaSharp embedding adapter

**Feature Branch**: `feature/412-spec`

**Created**: 2026-10-21

**Status**: Draft

**Input**: Ticket DEV-412 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-412/brief.md` (grill closed Q1–Q8); facts from notes `recon-DEV-412` and `recon-DEV-412-2`; Patron rulings in `specs/DEV-412/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md`. No decisions beyond the brief and those rulings. No `design.md` (no ui tag).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Plot text becomes a verified 384-dimension embedding (Priority: P1)

A recommendation-path consumer resolves the existing embedding port and receives one finite 384-element vector per plot text, in input order, from the local Ollama configuration. Empty batches return no vectors and do not call the provider. Caller mistakes are rejected before any network call.

**Why this priority**: This is acceptance criterion 1 and the core ticket delivery. Later recommendation work needs a real runtime adapter behind the existing port.

**Independent Test**: Against WireMock, register the production adapter, complete startup verification with a 384-length stub response, call generate with single and batch plot texts, and assert request shape, response vectors, metadata, and local argument guards with zero HTTP on invalid or empty input.

**Acceptance Scenarios**:

1. **Given** a configured endpoint and model that already passed startup verification, **When** generate runs for one plot text, **Then** the result is exactly one finite 384-element vector and the outbound request uses the configured model, the unmodified text, and no wire `dimensions` field.
2. **Given** an ordered batch of plot texts, **When** generate runs, **Then** the result has the same count and order, each vector is finite and length 384, and the request preserves input order.
3. **Given** an empty batch, **When** generate runs, **Then** zero embeddings are returned and no HTTP request is made.
4. **Given** a null batch, a null or blank item, a caller `ModelId` other than the configured model, or caller `Dimensions` other than null or 384, **When** generate is invoked, **Then** an `ArgumentException`-family error is thrown before HTTP.
5. **Given** caller `Dimensions` of null or 384 and a matching or absent caller `ModelId`, **When** generate runs, **Then** the configured model is sent and wire `dimensions` remains absent.
6. **Given** a successful verification, **When** unkeyed metadata is requested from a transient adapter instance, **Then** reported default dimensions are 384; keyed or unknown service requests return null; the wrapper never exposes a raw alternate-model client.

---

### User Story 2 - Non-384 models fail startup with a clear message (Priority: P1)

An operator who configures a model that does not produce 384 dimensions cannot start the API host successfully. Options syntax fails before any network call. A reachable wrong-dimension or unavailable model fails the online probe with a sanitized diagnostic that names endpoint, model, category, and expected versus observed dimensions when known, never plot text or provider bodies.

**Why this priority**: Acceptance criterion 2. A wrong-length model must fail closed before any consumer trusts vectors.

**Independent Test**: Bind production registration against WireMock. Prove invalid options fail before network; a 384 stub passes startup; a 768 stub, missing model, malformed response, unavailable endpoint, and a first-load delay that exhausts the same configured resilience budget each fail startup clearly. Unrelated API test factories substitute the generator or probe so they do not require live Ollama.

**Acceptance Scenarios**:

1. **Given** valid `Ollama` and `Ollama:Resilience` options and a stub that returns one finite 384-vector for the fixed non-secret probe text, **When** the host starts, **Then** awaited `IHostedService.StartAsync` verification completes before readiness, verified dimensions are published as 384, and existing `EmbeddingDimensions.Validate` accepts the generator.
2. **Given** a stub that returns a non-384 vector (including native 768), **When** the host starts, **Then** startup fails with a clear sanitized message that includes expected and observed dimensions when available.
3. **Given** a missing model, unavailable endpoint, exhausted retries or timeout, a first-load delay that exhausts the same configured resilience budget, malformed or nonfinite probe response, or wrong embedding count, **When** the host starts, **Then** startup fails closed as provider unavailable (or the matching sanitized category) with a sanitized diagnostic (safe endpoint identifier, model, category; never plot, raw URI user-info/query/fragment, unsafe path data, or raw body). No separate startup allowance, pull/warm path, or diagnosis that claims to distinguish cold loading from outage.
4. **Given** an invalid absolute endpoint URI, blank model name, or resilience values outside the mirrored DEV-303 ranges or cross-field rules, **When** options validate on start, **Then** startup fails before any embedding HTTP call.
5. **Given** existing unrelated `WebApplicationFactory` hosts, **When** they boot, **Then** test-only substitution of the generator or startup probe keeps them green without live Ollama; dedicated composition tests still exercise real production registration against WireMock. No production bypass flag exists.
6. **Given** a verification probe whose completion is delayed past host-start expectations, **When** the host attempts to become ready, **Then** startup cannot complete early via a background-only `ExecuteAsync` probe; readiness waits for awaited `StartAsync` verification.

---

### User Story 3 - Transient Ollama failures become typed consumer failures (Priority: P1)

A consumer calling generate sees transient provider problems after the single resilience pipeline finishes as `EmbeddingGenerationException` carrying an existing enrichment failure category. Permanent or semantic errors are not retried. Caller cancellation remains cancellation.

**Why this priority**: Acceptance criterion 3. Consumers must distinguish outage, rate limit, and invalid response without depending on vendor exception types.

**Independent Test**: WireMock resilience matrix using application `TimeProvider` plus existing `FixedTimeProvider`, shortened test-only resilience settings, and bounded real WireMock/backoff delay measurements per DEV-303 (recon-DEV-412-3 §15.2). No new timing package or arbitrary sleeps; the DI clock alone does not skip Polly delays. Cover transient-then-success attempt counts; exhausted 429/408/5xx/network/timeouts; Retry-After bounded by total timeout; breaker open; permanent statuses with exactly one attempt; timeout without caller cancellation; cancellation during send/backoff.

**Acceptance Scenarios**:

1. **Given** a transient server error then success within the retry budget, **When** generate runs, **Then** the call succeeds after the exact expected attempt count and returns valid 384-vectors.
2. **Given** exhausted HTTP 429, **When** generate finishes, **Then** `EmbeddingGenerationException` carries `RateLimited`.
3. **Given** exhausted 408/5xx, network failure, attempt or total timeout (`TimeoutRejectedException`), timeout cancellation when the caller token is not cancelled, or a broken circuit, **When** generate finishes, **Then** the exception carries `ProviderUnavailable`.
4. **Given** other non-success statuses, provider error payloads, malformed JSON, or missing/null/empty/count-mismatched/nonfinite/non-384 vectors after the pipeline, **When** generate finishes, **Then** the exception carries `InvalidResponse`.
5. **Given** permanent client errors that must not retry, **When** generate runs, **Then** exactly one HTTP attempt occurs.
6. **Given** caller cancellation during the send or during backoff (`OperationCanceledException` with caller token cancelled), **When** generate runs, **Then** cancellation is preserved as `OperationCanceledException`, not a typed provider failure. Do not invent a late-cancellation race precedence rule beyond Q5.
7. **Given** any typed or argument failure, **When** messages, logs, spans, or nested exceptions are inspected, **Then** plot text, raw provider bodies, credentials, raw URI user-info/query/fragment, unsafe path data, and body-bearing inner chains are absent; diagnostics use a safe endpoint identifier plus sanitized model/category/dimension fields.

---

### User Story 4 - Passive health and functional embedding telemetry (Priority: P2)

An operator can read dependency health for embeddings without issuing HTTP from the health check, and can see one logical generate span plus duration and outcome metrics on the existing LamuFlix instrumentation for each outbound generate that reaches the provider.

**Why this priority**: Constitution-forced supporting scope (Patron Q8). Not a separate product feature, but required observability for every new external dependency.

**Independent Test**: Composition and telemetry tests assert health-state transitions with zero HTTP on the health check path, and Activity/Meter listeners assert one logical call across retries, omission of local rejects, and low-cardinality tags only.

**Acceptance Scenarios**:

1. **Given** the host before verification completes, **When** the untagged `ollama-embeddings` health check runs, **Then** the status is Unhealthy and no HTTP is issued.
2. **Given** a successful verified embedding, **When** the health check runs, **Then** the status is Healthy with no HTTP.
3. **Given** a typed provider failure after verification, **When** the health check runs, **Then** the status is Degraded until a later valid success restores Healthy; local empty or invalid input does not mark provider failure.
4. **Given** one outbound generate that includes retries (including the startup probe, which is the same adapter `GenerateAsync` path), **When** listeners observe the existing LamuFlix ActivitySource and Meter, **Then** exactly one logical `Embedding.Generate` span, one duration histogram sample, and one outcome counter increment are recorded for that logical call, with low-cardinality outcome/category tags only (no plot, endpoint, model, or raw body). Retries remain inside that single count; no new tag taxonomy.
5. **Given** an empty batch or caller-invalid local reject, **When** generate returns or throws locally, **Then** no logical embedding span or outcome metric is recorded for that reject; HttpClient instrumentation may still exist for real HTTP.

### Edge Cases

- Empty batch: zero embeddings, zero HTTP, no logical embedding telemetry.
- Null batch or null/blank item: `ArgumentException`-family before HTTP.
- Caller `ModelId` mismatch or `Dimensions` other than null/384: argument error before HTTP.
- Caller `Dimensions` null or 384: allowed; wire `dimensions` still omitted; caller `Dimensions` MUST NOT be forwarded to the inner client.
- Native `nomic-embed-text` (768) or any non-384 output: fails startup and fails runtime validation; no truncation, projection, or requested-dimensions override.
- Raw OllamaSharp metadata dimensions are null until the wrapper publishes verified 384 after a successful probe.
- Metadata before verification reports null default dimensions; after verification, transient instances share verified 384 via coherent thread-safe DI singleton state publication/reads/updates.
- Exhausted retries map only after the sole resilience pipeline finishes; no second application retry loop.
- Startup converts provider failure into a clear startup failure via awaited `IHostedService.StartAsync` verification; it does not leave the host ready via a background-only `ExecuteAsync` probe.
- A first-load delay that exhausts the same configured resilience budget fails closed as `ProviderUnavailable`; no separate startup allowance, pull/warm, or cold-versus-outage diagnosis.
- Diagnostics use a safe endpoint identifier excluding URI user-info/query/fragment and unsafe path data; never plot text, raw URI, raw bodies, credentials, or body-bearing inner chains. Sanitized model/category/expected-versus-observed dimensions remain. This is redaction only, not a new endpoint acceptance rule.
- No automated test calls live Ollama; real-model acceptance remains DEV-349.
- PRODUCT Epic 9 delivery still waits for Epic 7; this Phase A adapter does not waive sequencing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST pin `OllamaSharp` 5.5.0 and `Microsoft.Extensions.AI` 10.11.0 in `Directory.Packages.props`, retain `Microsoft.Extensions.AI.Abstractions` 10.11.0, and add direct unversioned Infrastructure package references to both new packages. It MUST NOT add deprecated `Microsoft.Extensions.AI.Ollama`.
- **FR-002**: Infrastructure MUST implement `IEmbeddingGenerator<string, Embedding<float>>` as `OllamaSharpEmbeddingGenerator`, wrapping `OllamaApiClient` with an injected typed `HttpClient` whose `BaseAddress` is the configured endpoint and whose default model is the configured model name. Externally owned `HttpClient` disposal MUST be respected.
- **FR-003**: Registration MUST use transient `AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, OllamaSharpEmbeddingGenerator>` with one `AddStandardResilienceHandler` mirroring the DEV-303 OMDb pattern (exponential jitter, Retry-After, retry 408/429/5xx and `HttpRequestException`/`TimeoutRejectedException`, matching breaker predicates). `HttpClient.Timeout` MUST be `Timeout.InfiniteTimeSpan` so it does not preempt the configured total timeout. `TimeProvider` MUST be injected. Application-owned randomness MUST use injected seeded `Random`. No second application retry loop and no new retry dependency.
- **FR-004**: `OllamaOptions` (section `Ollama`) MUST expose `Endpoint` and `ModelName` with record initializers carrying the already approved defaults `http://localhost:11434/` and `all-minilm`, requiring a nonempty absolute `http`/`https` endpoint and a nonblank model name, with no model-name allowlist. `OllamaResilienceOptions` (section `Ollama:Resilience`) MUST mirror DEV-303 meanings, defaults, ranges, and cross-field rules (`AttemptTimeout <= TotalTimeout`, `SamplingDuration >= 2 * AttemptTimeout`) without modifying OMDb options. Both MUST bind in ServiceDefaults with data annotations, cross-field validation, and `ValidateOnStart`. Api `appsettings.json` carries the same defaults; options records alone MUST validate successfully when no `Ollama` section is present. No Worker host or Api-only binding redesign.
- **FR-005**: Startup MUST fail closed: options validate on start; an Infrastructure `IHostedService` (`OllamaEmbeddingStartupValidator`) MUST perform verification in awaited `StartAsync` (not a background-only `ExecuteAsync` probe), resolve the typed client in a short-lived scope, send one fixed non-secret probe text through the same resilient transport with no dimensions override, require exactly one finite 384-element vector, publish verified dimensions only after success, then call existing `EmbeddingDimensions.Validate`. API readiness MUST wait for that completion. Wrong count/dimensions, missing model, malformed/nonfinite response, unavailable endpoint, exhausted retries/timeout, or a first-load delay that exhausts the same configured resilience budget MUST fail startup with a sanitized diagnostic as `ProviderUnavailable` where mapping applies. No separate startup allowance, pull/warm, or cold-versus-outage diagnosis.
- **FR-006**: A DI singleton `OllamaEmbeddingModelState` MUST store verified configuration/dimensions and health without capturing `HttpClient` or using mutable static initialization, with coherent thread-safe publication/reads/updates across transient clients. Runtime generate MUST validate every result for count, finite values, and length 384.
- **FR-007**: Wrapper `GetService` MUST return metadata with verified dimension 384 for unkeyed metadata requests after verification (null before), return the wrapper when assignable, and return null otherwise. It MUST NEVER expose raw `IOllamaApiClient`/`IChatClient` or bypass validation/configured model.
- **FR-008**: Generate MUST POST `/api/embed` with the configured model and ordered unmodified inputs, omitting wire `dimensions` for both caller `Dimensions` null and `Dimensions` 384. It MUST NOT forward caller `Dimensions` to the inner client. It MUST NOT mutate caller options and MUST NOT introduce a blanket `AdditionalProperties` ban or allowlist. It MUST return one finite 384-vector per input preserving count and order. Empty batch returns zero embeddings without HTTP. Null batch, null/blank item, mismatched caller `ModelId`, or caller `Dimensions` other than null/384 MUST throw `ArgumentException`-family before HTTP.
- **FR-009**: `EmbeddingGenerationException` in Core/Ports MUST carry existing `EnrichmentFailureCategory` using only `ProviderUnavailable`, `RateLimited`, and `InvalidResponse`. No new category type, C# enum, port, or translation layer. Mapping MUST occur only after the sole resilience pipeline finishes, with the category rules in User Story 3: `TimeoutRejectedException` → `ProviderUnavailable`; `OperationCanceledException` with caller token cancelled → cancellation; otherwise timeout cancellation → `ProviderUnavailable`. Preserve Q5; do not invent a late-cancellation race precedence rule.
- **FR-010**: Messages, logs, spans, and surfaced exception chains MUST NOT reveal plot text, raw provider bodies, credentials, raw URI strings that include user-info/query/fragment, or unsafe path data. Diagnostics MUST use a safe endpoint identifier and MAY include sanitized model/category and expected/observed dimensions when available. Body-bearing inner exceptions that would bypass that rule MUST NOT be retained. This is diagnostic redaction only; it does not add a new endpoint acceptance restriction.
- **FR-011**: An untagged passive health check `ollama-embeddings` (`OllamaEmbeddingHealthCheck`) MUST read only DI state and NEVER HTTP: Unhealthy before verification, Healthy after valid embedding success, Degraded after typed provider failure until a later valid success. Local empty/invalid input MUST NOT imply provider failure.
- **FR-012**: Functional telemetry constants MUST be added to existing `TelemetryConstants` for span `Embedding.Generate`, histogram `lamuflix.embedding.duration`, and counter `lamuflix.embedding.outcome`, recorded once per logical outbound generate (including retries and including the startup probe, which uses the same adapter path) on the existing LamuFlix ActivitySource/Meter with injected `TimeProvider` and low-cardinality outcome/category tags only. Empty/caller-invalid local rejects MUST skip those logical instruments. No vendor-named Core telemetry identifiers, no new telemetry layer, and no new tag taxonomy.
- **FR-013**: Existing API integration test factories MUST substitute generator/probe only where needed so unrelated hosts need no Ollama. Recon-DEV-412-3 §15.5 inventory names `ApiHostFactory.cs`, `ApiEndToEndFactory.cs`, nested `ReadyApiHostFactory` in `HealthEndpointTests.cs`, and enumerated per-test builders; Build recon MUST refresh and name the actual necessary isolation edits before those edits. Dedicated composition/startup/resilience/telemetry tests MUST exercise production registration against WireMock. No production bypass flag. No automated test MAY call live Ollama; real-model acceptance remains DEV-349. No wholesale inventory edits or newly invented file authority.
- **FR-014**: Proof MUST use existing xUnit, assertion libraries, LoopbackWireMock, and FsCheck. Unit options tests, typed-exception contract tests, FsCheck properties for finite-vector invariants and cross-field options validity (IntegrationTests via existing `[Fact]` + `[Trait("Category", "Property")]` / `Prop.ForAll` pattern; recon-DEV-412-3 §15.3), and the WireMock matrices in the brief test strategy are required. Execution evidence MUST cover the new vector/options properties specifically. No new test dependency. For this no-opt-out property-bearing change, disabled/all-skipped/no-matching-property exit 2 does not satisfy required evidence and MUST NOT be relabeled PASS.
- **FR-015**: The system MUST NOT add UI, database/vector persistence/schema, worker/FeaturesRequested consumer orchestration, deployment/container changes, web/public HTTP contracts, LocalPlay, secrets handling, `Process.Start`, model install/pull/fallback, dimension reduction, a new project/layer/port/ADR, or a forked enrichment claim/retry/DLQ pipeline. PRODUCT Epic 9 sequencing after Epic 7 remains in force.

### Key Entities

- **Ollama options**: Endpoint URI and model name for the local embedding provider; defaults `http://localhost:11434/` and `all-minilm`.
- **Ollama resilience options**: Separately configurable retry/timeout/breaker settings mirroring the OMDb resilience meanings without sharing OMDb configuration.
- **Verified embedding model state**: DI singleton recording whether startup verification succeeded, the verified dimension, and passive health.
- **Embedding generation failure**: Exception carrying an existing enrichment failure category for provider unavailable, rate limited, or invalid response.
- **Embedding vector**: Ordered finite float sequence of length 384 produced per accepted plot text.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After startup verification against a 384-capable stub, a single plot text yields exactly one finite 384-vector; a batch preserves count and order; the outbound request uses the configured model and omits wire `dimensions`.
- **SC-002**: Configuring or probing a non-384 model (including a 768 stub), a missing model, an unavailable endpoint, or a first-load delay that exhausts the same configured resilience budget fails host startup with a sanitized clear message. Invalid options fail before network.
- **SC-003**: Transient failures retry through the single resilience pipeline; exhausted 429 surfaces as rate limited; exhausted transport/timeout/breaker surfaces as provider unavailable; malformed or wrong-length results surface as invalid response; caller cancellation remains cancellation.
- **SC-004**: Passive `ollama-embeddings` health transitions Unhealthy → Healthy → Degraded → Healthy without issuing HTTP on the health-check path. Each logical outbound generate records one span, one duration sample, and one outcome count with sanitized low-cardinality tags.
- **SC-005**: Unrelated API test hosts boot without live Ollama via test-only substitution. Dedicated composition tests still prove production registration. No automated test calls live Ollama.
- **SC-006**: Configured verification gates run without a lowered threshold or new opt-out. Unchanged `/web` is scope-empty `SKIPPED`, never PASS.

## Assumptions

- Default model `all-minilm` and default endpoint `http://localhost:11434/` are confirmed `[assumed]` configuration defaults in `specs/DEV-412/ASSUMPTIONS.md`, not open questions.
- Native upstream dimensions (`all-minilm` = 384, `nomic-embed-text` = 768) are recon facts used to execute the ticket verify-before-choosing directive; actual output validation remains mandatory.
- No owner structural checkbox is open. Anything not decided in `brief.md` is a `needs decision:` item, not an assumption added here.
