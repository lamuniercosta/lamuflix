# DEV-412 — OllamaSharp embedding adapter brief

## Status and authority

Phase 2 grill complete: Q1-Q8 settled with Patron; zero owner structural checkboxes. This brief is input for Quill, not plan freeze, provisional/merged Gate 1, implementation authorization or verification. No code, tests, gates, commits or PR publication performed by Keel.

Worktree F:/Dev/LamuFlix.worktrees/feature-412-spec; branch feature/412-spec; Phase A base 541dd85985b98f9f1664a3238e9ba384c6ba17bc from task/recon receipt, not a new fetch. Size M, no ui tag, parent DEV-288.

Authority: full YouTrack Description in recon-DEV-412 section 13 (Scope items 1-4 and AC1-3); package/API/model documentation in recon-DEV-412-2 section 14; specs/PRODUCT.md; task-pipeline Phase 2; lamuflix-team-charter. Both prior needs recon requests are resolved. Facts remain bounded to those notes; new missing facts return to Conductor as needs recon. Complete exchanges are append-only in CONCLUSIONS.md. Every full Patron answer is also reproduced below. ASSUMPTIONS.md records Q3 defaults.

## Loop discipline and frozen scope

Critical, High and Medium findings block; Low is nonblocking. At most two review rounds and two fix commits per round. Third-round findings go to owner review on the PR, not another automatic round.

Scope: Infrastructure OllamaSharp implementation of IEmbeddingGenerator<string, Embedding<float>>, maintained MEAI binding/package wiring, configurable endpoint/model and resilient typed HttpClient, actual 384-dimension startup verification, runtime validation and typed failures, WireMock/property tests, forced composition/config/test isolation, and constitution-required passive health/functional telemetry.

Exclude real-model acceptance/quality execution (DEV-349), model installation/pull/fallback, dimension reduction, database/vector persistence/schema, worker/FeaturesRequested consumer orchestration, deployment/container changes, web/public HTTP contracts, LocalPlay, secrets/process launching and unrelated refactors. Do not fork the enrichment claim/retry/DLQ pipeline. PRODUCT Epic 9 delivery still waits for Epic 7; Phase A does not waive sequencing. Anything else is a follow-up issue, not a finding in this round.

## Package, model and configuration decisions

Pin OllamaSharp 5.5.0 and Microsoft.Extensions.AI 10.11.0 in Directory.Packages.props; retain AI.Abstractions 10.11.0. Add direct unversioned Infrastructure PackageReferences to both new packages. Use OllamaSharp native MEAI implementation; no deprecated Microsoft.Extensions.AI.Ollama. Q2 supersedes Q1 legacy-package shorthand: Patron rules the native binding satisfies the generic ticket wording without changing delivery.

OllamaOptions (section Ollama) has Endpoint and ModelName, default http://localhost:11434/ and all-minilm. Require nonempty absolute http/https endpoint and nonblank model, no name allowlist. Recon verifies native all-minilm=384 and nomic-embed-text=768. Selecting all-minilm implements the ticket verify-before-choosing directive; do not repeat its stale both-384 claim. Actual output must validate; no requested dimensions override, projection/truncation of vectors, automatic pull or model fallback. Patron routed the factual tracker comment to Rigger; verified publication is not asserted here.

OllamaResilienceOptions (section Ollama:Resilience) mirrors DEV-303 meanings, defaults and validation, separately configurable without modifying OMDb:

| Setting | Default |
|---|---|
| MaxRetryAttempts | 3 |
| BaseDelay | 1 second |
| AttemptTimeout | 10 seconds |
| TotalTimeout | 30 seconds |
| FailureRatio | 0.1 |
| SamplingDuration | 30 seconds |
| MinimumThroughput | 100 |
| BreakDuration | 5 seconds |

Mirror current ranges: retries >=1; base delay 0..1 day; attempt/total timeout 10ms..1 day; ratio 0..1; sampling/break duration 500ms..1 day; minimum throughput >=2. AttemptTimeout <= TotalTimeout and SamplingDuration >= twice AttemptTimeout. Bind both options in ServiceDefaults with data annotations, cross-field validation and ValidateOnStart.

One AddStandardResilienceHandler: exponential jitter, Retry-After, retry 408/429/5xx and HttpRequestException/TimeoutRejectedException, matching breaker predicates. Embedding POST is safe to retry. No semantic/permanent-status retries or second application retry loop. HttpClient.Timeout must not preempt configured total timeout (use infinite). Inject TimeProvider; any application-owned randomness uses injected seeded Random. No new retry/testing dependency.

## Adapter, startup and lifetime

Name adapter OllamaSharpEmbeddingGenerator. Register transient AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, OllamaSharpEmbeddingGenerator>, wrapping OllamaApiClient with injected HttpClient BaseAddress and configured model. Respect externally owned HttpClient disposal; no extra unmanaged client.

A small DI singleton OllamaEmbeddingModelState stores verified configuration/dimensions and health, no captured HttpClient or mutable static initialization. Infrastructure IHostedService OllamaEmbeddingStartupValidator validates options and resolves the typed client in a short-lived scope; one fixed nonsecret text probe goes through the same resilient transport. Require exactly one finite 384-element vector. Publish verified dimensions only after successful probe for the configured endpoint/model, then call existing EmbeddingDimensions.Validate. API readiness waits for completion, with no concurrent-start bypass. Wrong count/dimensions, missing model, malformed/nonfinite response, unavailable endpoint or exhausted retries/timeout fails startup clearly. Diagnostics name sanitized endpoint/model/category and expected/observed dimensions when available, never plot/provider raw body. Model load is bounded by configured resilience budget.

Raw OllamaSharp dimensions metadata is null. Wrapper GetService returns metadata for unkeyed metadata requests with verified dimension 384 (null before verification), itself when assignable, otherwise null. Never expose raw IOllamaApiClient/IChatClient or bypass validation/configured model. Shared state works across transient instances; validate every runtime result to catch later model replacement.

Existing API test factories, including ApiEndToEndFactory as needed, replace generator/probe only in tests so unrelated WebApplicationFactory tests need no Ollama. Dedicated startup/composition tests exercise production registration against WireMock, including failed starts. No production bypass flag. Startup compatibility implements AC2; DEV-349 owns actual model acceptance/quality. No automated tests call live Ollama.

## Inputs, results and consumer failures

POST /api/embed sends configured model and ordered unmodified plot inputs, no dimensions override. Return one finite 384-value vector per input preserving count/order. Empty batch returns zero embeddings without HTTP. Null batch, null/blank item, different caller ModelId or caller Dimensions other than null/384 throws ArgumentException-family before HTTP. Same model/384 is allowed but wire dimensions remains absent. Optional library options must preserve these invariants.

Core/Ports/EmbeddingGenerationException carries existing EnrichmentFailureCategory, using only ProviderUnavailable, RateLimited and InvalidResponse; no new category type, C# enum, port or translation layer.

- Exhausted 429: RateLimited.
- Exhausted 408/5xx, network/attempt/total timeout, non-caller timeout cancellation or broken circuit: ProviderUnavailable.
- Other non-success, provider error payload, malformed JSON, missing/null/empty/count-mismatched/nonfinite/non384 vector: InvalidResponse.

Map after the sole resilience pipeline finishes. Caller-requested cancellation remains OperationCanceledException, including during backoff. Startup converts provider failure to a clear startup failure. Messages, logs, spans and surfaced exception chains must not reveal plot text, raw provider bodies or credentials; sanitize diagnostics and do not retain body-bearing inner exceptions that bypass this rule.

## Passive health and telemetry — unconditional Q8 ruling

Register untagged ollama-embeddings passive OllamaEmbeddingHealthCheck reading only DI state, never HTTP: Unhealthy before verification, Healthy after valid embedding success, Degraded after typed provider failure until a later valid success. Local empty/invalid input does not imply provider failure.

Reuse existing LamuFlix ActivitySource/Meter, with functional names/attribute keys in Core/Pipeline/TelemetryConstants.cs: Embedding.Generate span, lamuflix.embedding.duration histogram, lamuflix.embedding.outcome counter. Record one logical outbound GenerateAsync span/duration/outcome including retries with injected TimeProvider. Low-cardinality outcome/category tags only; no plot, endpoint, model or raw body. Skip empty/caller-invalid local rejects; retain HttpClient instrumentation. Follow existing enrichment instrumentation conventions, no vendor-named telemetry constants or new layer. These are constitution-forced supporting edits without owner checkbox. Q8 replaces Q7 conditional observability wording.

## Authorized files

Add:

- src/LamuFlix.Core/Options/OllamaOptions.cs
- src/LamuFlix.Core/Options/OllamaResilienceOptions.cs
- src/LamuFlix.Core/Ports/EmbeddingGenerationException.cs
- src/LamuFlix.Infrastructure/Adapters/OllamaSharpEmbeddingGenerator.cs
- src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingServiceCollectionExtensions.cs
- src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingStartupValidator.cs
- src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingModelState.cs
- src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingHealthCheck.cs
- tests/LamuFlix.UnitTests/Options/OllamaOptionsTests.cs
- tests/LamuFlix.UnitTests/Options/OllamaResilienceOptionsTests.cs
- tests/LamuFlix.UnitTests/EmbeddingGenerationExceptionTests.cs
- tests/LamuFlix.IntegrationTests/OllamaEmbeddingGeneratorTests.cs
- tests/LamuFlix.IntegrationTests/OllamaEmbeddingResilienceTests.cs
- tests/LamuFlix.IntegrationTests/OllamaEmbeddingCompositionTests.cs
- tests/LamuFlix.IntegrationTests/OllamaEmbeddingTelemetryTests.cs

Modify additively:

- Directory.Packages.props
- src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj
- src/LamuFlix.ServiceDefaults/Extensions.cs
- src/LamuFlix.Api/Program.cs
- src/LamuFlix.Api/appsettings.json
- src/LamuFlix.Core/Pipeline/TelemetryConstants.cs
- Existing API integration test factories only where needed for host isolation.

Approved adapter partial files for complexity/LoggerMessage and WireMock test-probe helper may live in the same existing folders. Keep FsCheck tests in agreed relevant test files following current conventions. New unanticipated files/decisions return to Keel; Quill does not guess.

No project, layer, schema/migration, public HTTP DTO/route, web or generated edits. No new port or ADR: this M task applies existing port/ADR-0012. Vocabulary remains existing embedding generator, model, vector, dimensions; no glossary rewrite.

## Test strategy and gates

Existing xUnit/assertion libraries, LoopbackWireMock and FsCheck; no new test dependencies or property opt-out. Unit options tests cover ranges/cross-field rules and typed exception/category contract. FsCheck properties cover finite-vector dimension/count/order invariants and cross-field configuration validity.

WireMock coverage: exact request shape/model/ordered text/absent dimensions; single/batch success; metadata before/after verification and across transient instances; keyed/unknown GetService and disposal ownership; empty/invalid calls with zero HTTP; dimension/count/null/empty/nonfinite/malformed failures; caller model/dimension guards; sanitized messages and nested exceptions.

Composition: real production registration; startup 384 pass/768 fail/unavailable or missing model fail; invalid URI/model/resilience options before network; unrelated factory isolation. Resilience: transient then success with exact attempts; exhausted 429/408/5xx/network/attempt/total timeout; Retry-After bounded by total timeout; breaker opening; permanent errors no retry; cancellation during call/backoff. TimeProvider/existing mechanisms instead of sleeps. Passive health transitions cause zero HTTP. Activity/Meter listeners verify logical-call counting across retries, local rejects omitted and sanitized low-cardinality tags.

Gauge later runs applicable Roslyn, complexity, InspectCode on changed diff (not All), property tests, vulnerable packages, format, full dotnet test, worktree-safe mutation without since for changed eligible production projects. All changed production projects accounted for; thresholds exclusively harness.yml. Mutation N/A/exclusions only from script verdict. Web unchanged: SKIPPED (scope-empty), nonblocking never PASS; same scope-empty semantics elsewhere. Applicable failures/Could not run block; no threshold waiver. No baseline/gate pass is claimed by this brief.

## Task order and phase boundaries

1. Quill drafts spec.md, plan.md, tasks.md from this brief and recon; a gap returns as needs decision, never a guessed requirement.
2. Keel read-only speckit-analyze/brief check, one numbered fix list per round; no Palette prerequisite (no ui). Plan challenge/adjudication, freeze, Patron provisional Gate 1, Rigger spec-only PR, user merge follow. No build work in Spec workspace.
3. Build workspace Phase B checks merged spec/prerequisites/Epic 7, obtains recon drift and Gauge baseline; Keel checks/amends drift before code.
4. Code Phase 1: packages/options/exception with unit/property tests, pass tests then commit.
5. Code Phase 2: adapter/resilient DI/state/startup/health/telemetry with WireMock/property/listener tests, depends on Phase 1, tests then commit.
6. Code Phase 3: ServiceDefaults and API composition/config with all forced factory isolation and composition/regression tests, depends on Phase 2, tests then commit. Do not register production startup in an earlier committed phase that leaves unrelated host tests requiring live Ollama.
7. Cog refactors whole build diff; Gauge gates; all three Risk/Standards/Spec reports before Keel adjudication on M. Missing axis blocks. Capped fixes and ship; publish/read back review receipts. Never merge or enable auto-merge; user owns merge.

## Every grill answer

Full Patron answers below, in order. Q2 supersedes Q1 package shorthand; Q5 selects reuse of existing category and test-only substitution; Q6 fixes adapter name/exposure; Q8 fixes unconditional observability/direct references. Full questions, rationale and implications remain in CONCLUSIONS.md.

---

[from Patron] DEV-412 Q1: ACCEPT — loop discipline and frozen scope ratified; no ticket or constitution contradiction.
- Closing bar & round cap: Critical/High/Medium block, Low nonblocking; max 2 review rounds and 2 fix commits per round (task-pipeline:28-29; standing ruling per specs/PRODUCT.md §3).
- Frozen scope: OllamaSharp and Microsoft.Extensions.AI.Ollama package wiring in Directory.Packages.props, LamuFlix.Infrastructure adapter behind IEmbeddingGenerator<string, Embedding<float>> with typed HttpClient and DEV-303 OMDb resilience pattern, endpoint/model options (384-dim startup validation), typed failure mapping on transient failures, WireMock request-shape and error-mapping tests, plus forced composition/config/test edits (recon-DEV-412:136-153; lamuflix-team-charter §2.3 #1, #6).
- Exclusions: real-model quality/acceptance check (DEV-349 per recon-DEV-412:145), database/vector persistence, worker/consumer orchestration, web/public HTTP contracts, Features:LocalPlay, and unrelated refactors (specs/PRODUCT.md §2, §5; constitution.md:138-140, 346).

---

[from Patron] DEV-412 Q2: ACCEPT — pin OllamaSharp 5.5.0 + Microsoft.Extensions.AI 10.11.0 (retaining Microsoft.Extensions.AI.Abstractions 10.11.0); do not add deprecated Microsoft.Extensions.AI.Ollama; no ticket contradiction or owner checkbox required.
- Authoritative ticket satisfaction (no ticket change): ticket item 1 says "add OllamaSharp and the Microsoft.Extensions.AI Ollama binding to Directory.Packages.props" generically without a package ID (recon-DEV-412:142), and constitution.md:346 mandates "`IEmbeddingGenerator<string, Embedding<float>>` (Microsoft.Extensions.AI) | Ollama via OllamaSharp at runtime". In 5.x, OllamaSharp natively implements `IEmbeddingGenerator<string, Embedding<float>>` and depends on `Microsoft.Extensions.AI` >= 10.8.0 (recon-DEV-412-2:13, 22, 41), whereas `Microsoft.Extensions.AI.Ollama` (9.7.0-preview) is officially deprecated on NuGet in favour of OllamaSharp and lacks a net10.0 target (recon-DEV-412-2:14-17).
- §2.3 #1 dependency ruling: add `<PackageVersion Include="OllamaSharp" Version="5.5.0" />` and `<PackageVersion Include="Microsoft.Extensions.AI" Version="10.11.0" />` to `Directory.Packages.props` alongside existing `Microsoft.Extensions.AI.Abstractions` `10.11.0`, referencing `OllamaSharp` (and `Microsoft.Extensions.AI` if needed) in `LamuFlix.Infrastructure.csproj` (specs/PRODUCT.md §5 #1; constitution.md:462-463; recon-DEV-412:31-35; recon-DEV-412-2:10-13).
- Q1 shorthand correction: supersede Q1's shorthand mention of `Microsoft.Extensions.AI.Ollama` with `OllamaSharp 5.5.0` + `Microsoft.Extensions.AI 10.11.0`, satisfying both items in ticket Scope & Technical Design #1 without dropping any deliverable.

---

[from Patron] DEV-412 Q3: ACCEPT — select `all-minilm` as the default model (taste/config default logged `[assumed]` if applicable), keep endpoint and model name configurable with no hardcoded name allowlist, enforce mandatory 384-dimension startup validation with no dimension override/truncation, and require no owner checkbox.
- Authority & no owner checkbox: Ticket Scope & Technical Design #3 explicitly instructs `(all-minilm or nomic-embed-text, both 384-dimension models; verify before choosing)` and AC #1-#2 mandate 384-dimension output and startup validation failure for any non-384 model (recon-DEV-412:144, 151-152). Selecting `all-minilm` after recon verified `all-minilm` = 384 native and `nomic-embed-text` = 768 native (recon-DEV-412-2:63-65) directly executes the ticket's `verify before choosing` directive without changing what the ticket delivers (lamuflix-team-charter §2.3; specs/PRODUCT.md §3, §5).
- Configuration contract: Endpoint must be a non-empty absolute `http`/`https` URI (default `http://localhost:11434/` in `appsettings.json` matching `OmdbOptions` precedent at `appsettings.json:17-19`, recon-DEV-412:75) and model name must be non-blank (default `all-minilm`), with no hardcoded model-name allowlist, no client-side truncation/override, and no automatic model pull or fallback (constitution.md:265-272; recon-DEV-412-2:63-65, 69).
- YouTrack record: Patron will hand Rigger a factual comment on DEV-412 recording the `verify before choosing` dimension recon (`all-minilm` = 384, `nomic-embed-text` = 768) and package binding fact (`task-pipeline:49`).

---

[from Patron] DEV-412 Q4: ACCEPT WITH ONE CAVEAT ON ApiEndToEndFactory — ratify fail-closed online startup dimension verification (`ValidateOnStart` + `IHostedService` startup probe + `EmbeddingDimensions.Validate`) and the forced composition/config file edits, provided either (a) `ApiEndToEndFactory` stubs/replaces the startup probe or `IEmbeddingGenerator` (matching its `RemoveSweeper` / health-check override at `ApiEndToEndFactory.cs:67-86`) or (b) `Program.cs` host wiring is scoped so existing `WebApplicationFactory<Program>` tests do not fail startup when Ollama is not stubbed.
- Fail-closed startup & runtime dimension contract (AC #1, #2; recon-DEV-412:151-152; recon-DEV-412-2:59, 71-77): `BindAndValidate` (`ValidateDataAnnotations().ValidateOnStart()`) validates endpoint URI, model name, and resilience syntax; an Infrastructure `IHostedService` startup probe calls the typed adapter through the resilient `HttpClient` with fixed non-secret probe text and no `dimensions` override, requiring exactly 1 finite 384-element vector before setting `EmbeddingGeneratorMetadata.DefaultModelDimensions = 384` and invoking `EmbeddingDimensions.Validate` (`src/LamuFlix.Core/Ports/EmbeddingDimensions.cs:10`). Wrong count, dimension != 384, non-finite values, missing model, or exhausted timeout/retry fails startup with a clear sanitized message naming endpoint, model, failure category, and observed dimension (never plot/body); runtime `GenerateAsync` enforces the same count, finite-value, and 384-dimension invariants on every call.
- Separation from DEV-349 & ADR-0012 compliance (recon-DEV-412:94, 145; `docs/adr/ADR-0012.md:15`): This startup probe checks dimension compatibility via WireMock in automated tests (`DEV-412` Scope #4 & AC #2), whereas real-Ollama container quality/acceptance testing belongs to `DEV-349` (`constitution.md:527`).
- §2.3 #2 & #6 care-list ruling (specs/PRODUCT.md §5 #2, #6; lamuflix-team-charter §2.3 #2, #6): Placing options in `src/LamuFlix.Core/Options/`, adapter + DI + startup probe in `src/LamuFlix.Infrastructure/Adapters/`, options binding in `src/LamuFlix.ServiceDefaults/Extensions.cs`, default config in `src/LamuFlix.Api/appsettings.json`, and composition in `src/LamuFlix.Api/Program.cs` (plus `tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs` if `Program.cs` registers the hosted probe so existing `WebApplicationFactory<Program>` tests stay green without a live Ollama stub) are forced additive edits in existing folders/projects with no new project or architectural layer.

---

[from Patron] DEV-412 Q5: CHANGE ON CATEGORY TYPE (reuse `EnrichmentFailureCategory` or make `EmbeddingFailureCategory` an Enumeration, never a C# `enum`), ACCEPT everything else including Q4 caveat (a) test-only substitution.
- Constitution Enumeration rule & Principle IV reuse (`constitution.md:202-204, 351, 377-378, 430-439, 480-481`; `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs:5-25`): Project-owned C# `enum` is strictly forbidden by the constitution ("never a C# `enum`"; "Plain `enum` forbidden for project-owned types"), and Principle IV / Enrichment Reliability Rules state "`FeaturesRequested` (Recommendations) reuses the same claim, retry, DLQ, category, and sweeper machinery; it MUST NOT fork a second pipeline." Therefore, `EmbeddingGenerationException` in `src/LamuFlix.Core/Ports/` MUST either carry the existing `EnrichmentFailureCategory` (`ProviderUnavailable`, `RateLimited`, `InvalidResponse`) or define `EmbeddingFailureCategory` as a `SmartEnum` / `sealed record` Enumeration following the exact `const`-code pattern of `EnrichmentFailureCategory` (never a C# `enum`). Reusing `EnrichmentFailureCategory` (with its existing `ProviderUnavailable`, `RateLimited`, `InvalidResponse` members) is preferred so downstream `FeaturesRequested` consumers do not need a category translation layer.
- Typed failure mapping & caller-options contract (AC #3; recon-DEV-412:70-71, 153; recon-DEV-412-2:33, 39, 45-48): Because `IEmbeddingGenerator<string, Embedding<float>>.GenerateAsync` returns `Task<GeneratedEmbeddings<Embedding<float>>>` directly, throwing `EmbeddingGenerationException` after the transport resilience pipeline exhausts satisfies AC #3 ("surface as a typed failure to the consumer") without wrapping the M.E.AI port. Map exhausted HTTP 429 to `RateLimited`; exhausted 408/5xx, network errors (`HttpRequestException`), attempt/total timeout (`TimeoutRejectedException` / non-caller `OperationCanceledException`), and `BrokenCircuitException` to `ProviderUnavailable`; and other non-success statuses, provider error JSON (`OllamaException` / `ResponseError`), malformed JSON, missing/empty/count-mismatched vectors, non-finite floats, and dimension != 384 to `InvalidResponse`. Preserve caller cancellation (`ct.IsCancellationRequested`) as `OperationCanceledException`; return zero embeddings without HTTP on empty batch; throw `ArgumentException`-family (`ArgumentNullException` / `ArgumentException` / `ArgumentOutOfRangeException`) before HTTP on null batch, null/blank element, mismatched `options.ModelId`, or `options.Dimensions` != 384 (when `Dimensions == 384` or null, omit wire `dimensions` and always send configured model); never include plot text or raw provider bodies in exception messages or logs.
- §2.3 #2 & #6 ruling + Q4 caveat (a): Adding `EmbeddingGenerationException` in `src/LamuFlix.Core/Ports/` and substituting the generator/startup probe in existing API integration test factories (`ApiEndToEndFactory` etc.) while dedicated composition/resilience tests exercise real production registration against WireMock is approved under §2.3 #2 and #6 with no production bypass flag.

---

[from Patron] DEV-412 Q6: ACCEPT WITH ONE NAMING ADJUSTMENT ON THE ADAPTER CLASS — ratify the resilience/options (`OllamaOptions`, `OllamaResilienceOptions`) and transient `AddHttpClient` + DI singleton verified-state plan, and name the Infrastructure adapter class `OllamaSharpEmbeddingGenerator` (or internalize it) so it does not collide with `Microsoft.Extensions.AI.OllamaEmbeddingGenerator`.
- Options & DEV-303 resilience parity (`DEV-412` Scope #2, #3; `constitution.md:268-271, 297-298`; `src/LamuFlix.Core/Options/MetadataProviderResilienceOptions.cs:10-61`; `recon-DEV-412:65-78`): `OllamaOptions` (`SectionName = "Ollama"`, `Endpoint` + `ModelName`) and `OllamaResilienceOptions` (`SectionName = "Ollama:Resilience"`, mirroring `MetadataProviderResilienceOptions` defaults: `MaxRetryAttempts = 3`, `BaseDelay = 1s`, `AttemptTimeout = 10s`, `TotalTimeout = 30s`, `FailureRatio = 0.1`, `SamplingDuration = 30s`, `MinimumThroughput = 100`, `BreakDuration = 5s`, plus `AttemptTimeout <= TotalTimeout` and `SamplingDuration >= 2 * AttemptTimeout` validation) are bound in `ServiceDefaultsExtensions.AddLamuFlixOptions` with `ValidateDataAnnotations().ValidateOnStart()`. Note: `constitution.md:297-298` permits vendor names on vendor-mandated configuration keys (`Ollama`, `Ollama:Resilience`) and on the Infrastructure adapter class itself; keep the options types in `LamuFlix.Core.Options` matching `OmdbOptions` precedent (`src/LamuFlix.Core/Options/OmdbOptions.cs:8`).
- Single retry layer & HttpClient timeout (`MetadataProviderServiceCollectionExtensions.cs:22-74`; `recon-DEV-412:65-66`): Register `.AddStandardResilienceHandler().Configure(...)` with exponential backoff + jitter, `ShouldRetryAfterHeader = true`, retrying HTTP 408/429/5xx and `HttpRequestException`/`TimeoutRejectedException` with matching circuit-breaker predicates; set `HttpClient.Timeout = Timeout.InfiniteTimeSpan` (or ensure it does not preempt `TotalTimeout`) so the standard resilience pipeline is the sole timeout/retry layer.
- Transient typed client, DI singleton verified-model state & class name (`recon-DEV-412-2:22-40, 54-59`; `AGENTS.md` Stryker static-init rule): Register `AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, OllamaSharpEmbeddingGenerator>` (named `OllamaSharpEmbeddingGenerator` to avoid any type-name ambiguity with `Microsoft.Extensions.AI` and to match the ticket summary "OllamaSharp adapter") wrapping `new OllamaApiClient(httpClient, options.ModelName)` with `httpClient.BaseAddress = new Uri(options.Endpoint)`. Use a small DI-registered singleton state object (no mutable static state, no captured `HttpClient`) to record verified `DefaultModelDimensions = 384` for the configured `(Endpoint, ModelName)` after the startup `IHostedService` probe resolves a scoped/transient generator and completes sequentially before API readiness; `GetService` returns `new EmbeddingGeneratorMetadata("ollama", endpointUri, modelName, verifiedDimensions)` when `serviceType == typeof(EmbeddingGeneratorMetadata)` and `serviceKey is null`, returns `this` when `serviceType.IsInstanceOfType(this)` (never leaking raw `IOllamaApiClient` or `IChatClient` to bypass the configured model/validation), and returns `null` otherwise.

---

[from Patron] DEV-412 Q7: ACCEPT — implementation scope, file set, test & FsCheck property-test plan, task order, and verification gate bar ratified; shared understanding confirmed, zero owner checkboxes, grill complete.
- Scope, file set & §2.3 rulings (`recon-DEV-412:136-153`; `specs/PRODUCT.md` §3, §5; `lamuflix-team-charter` §2.3 #1, #2, #6): Approve adding `Core/Options/OllamaOptions.cs`, `Core/Options/OllamaResilienceOptions.cs`, `Core/Ports/EmbeddingGenerationException.cs` (reusing `EnrichmentFailureCategory`), and `Infrastructure/Adapters/{OllamaSharpEmbeddingGenerator,OllamaEmbeddingServiceCollectionExtensions,OllamaEmbeddingStartupValidator,OllamaEmbeddingModelState}.cs` (plus partial files if needed for CA1502 complexity / `LoggerMessage`), alongside forced edits to `Directory.Packages.props`, `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`, `src/LamuFlix.ServiceDefaults/Extensions.cs`, `src/LamuFlix.Api/Program.cs`, `src/LamuFlix.Api/appsettings.json`, and existing API integration test factories (`ApiEndToEndFactory` etc.) for test isolation, plus the unit/FsCheck/WireMock integration test files. Note: if Ledger/Standards checks `constitution.md:254-255, 385` on health check/telemetry constants for external adapters, keep health-check/telemetry additions out unless forced by a gate or include a passive state check mirroring `MetadataProviderHealthCheck` only if you judge it forced by Principle VI (`constitution.md:254-255`); otherwise keep the exact file set above.
- Verification & test bar (`constitution.md:304-328, 400-418`; `AGENTS.md` Verification commands; `task-pipeline:76-83`): WireMock tests cover `POST /api/embed` request shape (model, ordered inputs, no `dimensions` override), single/batch 384-dim success and metadata before/after verification, count/dimension/null/empty/non-finite/malformed errors (`InvalidResponse`), caller argument/options guards before HTTP, startup validation (URI/model/resilience options, 384 pass, 768 fail, unavailable/missing model fail), resilience retry/timeout/Retry-After/circuit-breaker (`RateLimited` / `ProviderUnavailable`), caller cancellation preservation, and transient DI metadata sharing, using injected `TimeProvider` and FsCheck property tests without opt-out; all gates (`run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and worktree-safe `run-mutation.ps1`) apply with exact `SKIPPED (scope-empty)` / `N/A` semantics preserved.
- Grill closure: Zero owner structural checkboxes (`blocked: structural` = none); Q1–Q7 are settled. Keel may write `specs/DEV-412/CONCLUSIONS.md`, `specs/DEV-412/ASSUMPTIONS.md`, and `specs/DEV-412/brief.md` and proceed to Phase 2 step 3 (Quill Spec Kit drafting).

---

[from Patron] DEV-412 Q8: CHANGE TO UNCONDITIONAL CONSTITUTION-ALIGNED OBSERVABILITY & PACKAGE WIRING — include passive health check + `TelemetryConstants` / `LamuFlix` ActivitySource & Meter observability as constitution-forced supporting scope (no owner checkbox, and no vendor name in Core telemetry identifiers), and add direct `OllamaSharp` + `Microsoft.Extensions.AI` `PackageReference`s in `LamuFlix.Infrastructure.csproj`.
- Constitution-forced passive health check (`constitution.md:254-255, 385-386`; `src/LamuFlix.Infrastructure/Adapters/MetadataProviderHealthCheck.cs:8-32`): Every new external dependency must register a health check (`constitution.md:254-255, 385`). Register passive `OllamaEmbeddingHealthCheck` (or `EmbeddingGeneratorHealthCheck`) in `src/LamuFlix.Infrastructure/Adapters/` reading only the DI singleton `OllamaEmbeddingModelState` (never making an HTTP request on health checks), registered untagged under `"ollama-embeddings"` (matching `"metadata-provider"` at `MetadataProviderServiceCollectionExtensions.cs:34`): `Unhealthy` before startup verification completes, `Healthy` after a valid 384-dim embedding succeeds, and `Degraded` after a typed provider failure (`ProviderUnavailable`, `RateLimited`, `InvalidResponse`) until a subsequent valid embedding succeeds.
- Constitution-forced `TelemetryConstants` & `LamuFlix` ActivitySource/Meter (`constitution.md:246-250, 295-298, 385-386`; `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs:3-52`; `src/LamuFlix.Core/Pipeline/EnrichmentMetrics.cs:5-19`): `constitution.md:246-250` mandates "All span names, metric names, and attribute keys live in `TelemetryConstants` (`ActivitySource` "LamuFlix" ...). Ad-hoc string literals for telemetry names are forbidden" and `constitution.md:295-297` mandates "Types, members, and telemetry identifiers MUST be named by functionality, not by vendor (`IMetadataProvider` and `Metadata.Lookup`, not `IOmdbClient` and `Omdb.Lookup`)." Therefore, add the functional span/metric constants to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` (e.g. span `Embedding.Generate`, duration histogram `lamuflix.embedding.duration`, outcome counter `lamuflix.embedding.outcome`, using the existing `ActivitySourceName = "LamuFlix"` and `Meter("LamuFlix")` already wired in `ServiceDefaultsExtensions.cs:42-50`) rather than a vendor-named `OllamaEmbeddingTelemetry` class, recording one logical `GenerateAsync` span/duration/outcome per outbound call (measuring with injected `TimeProvider`, low-cardinality outcome/category tags only, never plot/endpoint/model/raw body, and skipping empty/caller-invalid local rejects). Dedicated tests in `OllamaEmbeddingCompositionTests.cs` / `OllamaEmbeddingTelemetryTests.cs` verify passive health transitions (zero HTTP calls during health check) and sanitized telemetry/metrics.
- Concrete Q2 package wiring & final grill closure (`Directory.Packages.props`, `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`; §2.3 #1, #6): Pin `OllamaSharp` `5.5.0` and `Microsoft.Extensions.AI` `10.11.0` in `Directory.Packages.props` (retaining `Microsoft.Extensions.AI.Abstractions` `10.11.0`) and add direct `<PackageReference Include="OllamaSharp" />` and `<PackageReference Include="Microsoft.Extensions.AI" />` to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` so both central pins are actively referenced without adding deprecated `Microsoft.Extensions.AI.Ollama`. Zero owner checkboxes (`blocked: structural` = none); Q1–Q8 are unconditionally closed.

## Phase 2 plan challenge clarifications (Keel, 2026-10-21)

Authority remains Q1-Q8 and the frozen scope above. Decision changes: none to product scope, Patron rulings, public contracts, dependencies, defaults, sequencing or closing bar. The following execution/proof clarifications implement those rulings; they do not reopen the grill or establish freeze/Gate 1. Bounded facts: recon-DEV-412-3 section 15.

- Registration tasks must explicitly own AddOllamaEmbeddingGenerator, singleton model state, hosted validator, and TryAddSingleton(TimeProvider.System), as already described by plan.md:123-128. Startup must await verification in IHostedService.StartAsync before readiness, without a background/concurrent-start bypass; delayed-probe tests prove this existing Q4/Q6 boundary.
- Slow cold loading that exhausts the same configured resilience budget fails startup as ProviderUnavailable. No separate budget, automatic pull/warm, or diagnostic claim distinguishing cold load from outage is added. Test this bounded failure with WireMock.
- Preserve Q5 mappings explicitly: TimeoutRejectedException -> ProviderUnavailable; OperationCanceledException when the caller token is cancelled remains cancellation, otherwise timeout cancellation -> ProviderUnavailable. Prove timeout without caller cancellation and cancellation during send/backoff; do not invent a separate late-cancellation race policy.
- Explicit caller Dimensions=384 must still produce no wire dimensions field. Do not forward Dimensions unaltered to OllamaSharp (recon-DEV-412-2:34). Preserve existing optional-options invariants; no blanket AdditionalProperties prohibition or new allowlist is authorized.
- Sanitized endpoint diagnostics exclude URI user-info, query, fragment, and any unsafe path content; use a safe endpoint identifier instead of raw URI or exception text. Preserve sanitized model/category and expected/observed dimensions where available. Synthetic credential/query/body-bearing exception tests prove the existing no-secrets/no-body contract. This is diagnostic redaction only, not a new endpoint acceptance rule.
- Singleton verified configuration/dimensions/health must be published coherently and read/updated thread-safely across transient clients. Controlled overlapping success/failure tests prove legal snapshots and the existing later-valid-success restoration rule. No prescribed locking primitive, success-priority rule, or deterministic winner for simultaneous completions is introduced.
- Inject TimeProvider for application timing. DEV-303 tests use existing FixedTimeProvider plus shortened test-only resilience settings and bounded real HTTP/backoff delays (recon section 15.2); they do not demonstrate virtual-clock control of Polly. No new timing dependency or arbitrary sleeps. Clarify this established test mechanism rather than promise virtual delay skipping.
- Existing options-record defaults must contain the already approved endpoint/model and resilience defaults as well as Api config. Prove AddLamuFlixOptions validation with no Ollama section succeeds without registering or calling the provider; invalid explicit values still fail. Keep ServiceDefaults binding, no invented Worker host or production bypass.
- Existing-factory authorization remains limited to forced test isolation. Recon section 15.5 identifies ApiHostFactory.cs, ApiEndToEndFactory.cs, and ReadyApiHostFactory in HealthEndpointTests.cs; additional per-test builders are inventoried there. Plan/tasks must cite that inventory and require TB004 to refresh it and name the actual files requiring isolation before T020. No wholesale edit of the inventory is authorized; a newly discovered file outside the existing factory-isolation category returns to Keel.
- Integration vector properties stay in the approved generator test file using the existing IntegrationTests Fact plus Trait(Category, Property) / FsCheck Prop.ForAll convention (recon section 15.3), without a new dependency. The default property runner discovers test projects and filters Category=Property. Require evidence that the new properties actually execute; an overall successful run with only unrelated properties is insufficient. For this no-opt-out property-bearing change, disabled/all-skipped/no-matching-property exit 2 does not satisfy required evidence.
- Use the already selected infinite HttpClient.Timeout. Startup is an outbound GenerateAsync through the same adapter and therefore emits the same one logical span/duration/outcome; retries remain inside that single count. Keep existing TelemetryConstants keys and low-cardinality outcome/category-only tags; no new tag taxonomy is required.
- Keep test-author-first/minimum-compilable-contract instructions and the existing single Anvil ownership. Parallel markers must respect prerequisites and shared test/stub files; do not treat shared contracts as independent parallel writes. Conductor consolidates TB002/TB003 prerequisite/Epic 7 evidence from the responsible owner/Rigger handoff into DEV-412; missing evidence still blocks code. Checklist marks must reference that receipt and cannot replace it.
- Refactor and normal complexity invocations are distinct, supported modes (recon section 15.6). For T025 read gates.complexity.refactor from harness.yml and pass that configured value through the supported -Threshold parameter, rather than a task-owned numeric literal. T026 no-args is the normal changed-diff gate and does not replace refactor evidence. No gate/config/generated-file edit or threshold reduction.

No change to CONCLUSIONS.md or ASSUMPTIONS.md is required. Q5-Q8 are complete in source; UTF-8/control-character concerns were reader artifacts. Untagged embeddings health stays as Patron ruled; recon section 15.1 shows it is excluded by the existing live/ready endpoint predicates. No endpoint/tag/status change is authorized.

