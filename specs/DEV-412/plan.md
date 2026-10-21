# Implementation Plan: DEV-412 OllamaSharp embedding adapter

**Branch**: `feature/412-spec` | **Date**: 2026-10-21 | **Spec**: `specs/DEV-412/spec.md`

**Input**: Feature specification from `specs/DEV-412/spec.md`, settled grill brief `specs/DEV-412/brief.md`, facts from `recon-DEV-412` and `recon-DEV-412-2`, Patron rulings in `specs/DEV-412/CONCLUSIONS.md`.

**Note**: Planning artifact only (Phase 2 step 3). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until Phase B entry evidence is complete (brief task order step 3). No `design.md` (no ui tag).

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before T001. Do not run gates, tests, or analyzers from Spec. Do not assert Epic 7 complete from Spec artifacts alone. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack `Ready to build` handoff in `DEV-412` | Yes |
| Merged-spec and other ticket prerequisites for build intake | Conductor consolidates owner/Rigger evidence into `DEV-412` (prerequisite identities); checklist marks must reference that receipt | Yes |
| PRODUCT Epic 7 sequencing prerequisite evidence for this intake | Conductor consolidates owner/Rigger Epic 7 evidence references into `DEV-412` (recorded check only; no Spec-side completion claim); checklist marks must reference that receipt | Yes |
| Phase B recon drift vs `origin/main` for authorized file set | Wisp → `recon-DEV-412` | Yes |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) | Gauge → recorded in `DEV-412` | Yes |
| Keel drift check or amend against brief/spec before code | Keel handoff in `DEV-412` | Yes |

## Summary

Add the OllamaSharp runtime adapter behind the existing `IEmbeddingGenerator<string, Embedding<float>>` port: pin maintained packages, configure endpoint/model and DEV-303-parity resilience, fail-closed online 384-dimension startup verification, typed consumer failures via existing `EnrichmentFailureCategory`, passive health, and functional LamuFlix telemetry. Prove request shape, errors, resilience, composition, and telemetry with WireMock and FsCheck. Real-model acceptance stays on DEV-349.

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: Add `OllamaSharp` 5.5.0 and `Microsoft.Extensions.AI` 10.11.0 (CPM + direct Infrastructure references). Retain `Microsoft.Extensions.AI.Abstractions` 10.11.0. Reuse existing `Microsoft.Extensions.Http.Resilience` and WireMock.Net. Do not add deprecated `Microsoft.Extensions.AI.Ollama`.

**Storage**: N/A. No schema, migration, or vector persistence.

**Testing**: xUnit; existing assertion libraries; LoopbackWireMock; FsCheck property tests (no opt-out). Options FsCheck in unit Options tests; finite-vector dimension/count/order FsCheck properties in `OllamaEmbeddingGeneratorTests` using the existing IntegrationTests `[Fact]` + `[Trait("Category", "Property")]` / `Prop.ForAll` pattern (recon-DEV-412-3 §15.3; default `run-property-tests.ps1` discovers all test projects and filters `Category=Property`). Execution evidence must cover the new vector/options properties specifically; unrelated global pass counts are insufficient. For this no-opt-out property-bearing change, disabled/all-skipped/no-matching-property exit 2 does not satisfy required evidence and must not be relabeled PASS. Application `TimeProvider` plus existing `FixedTimeProvider`, shortened test-only resilience settings, and bounded real WireMock/backoff delay measurements per DEV-303 (recon-DEV-412-3 §15.2). No new timing package or arbitrary sleeps; do not assert that the DI clock alone skips Polly delays. No live Ollama in automated tests. No new test dependency.

**Target Platform**: Existing `LamuFlix.Api` host. Local Ollama at configured endpoint for real runs; WireMock for automated proofs.

**Project Type**: Existing ports-and-adapters solution. No new project or layer. Applies existing embedding port and ADR-0012; no new port or ADR.

**Performance Goals**: No separate latency target in the brief. Embedding POST is safe to retry within the configured total timeout (default 30s). Startup probe is bounded by that same resilience budget.

**Constraints**: TreatWarningsAsErrors. Actual 384 output mandatory; no dimensions override, truncation, projection, auto-pull, or model fallback. One resilience layer only. Sanitize diagnostics. No `Features:LocalPlay`, secrets, or `Process.Start`. No public HTTP/OpenAPI/web edits. Epic 9 still waits for Epic 7. Phase B entry must record Epic 7 sequencing evidence before code; Spec does not claim that evidence here.

**Scale/Scope**: Authorized file set in brief "Authorized files". Approved adapter partials for complexity/LoggerMessage and a WireMock probe helper may live in the same existing folders. Unanticipated files return to Keel.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Scope items 1–4 and AC1–3 match the brief. Q2 package interpretation satisfies generic ticket wording without an owner checkbox. No owner structural checkbox (brief Q1–Q8).
- Dependency care list (§2.3 #1; PRODUCT.md §5 #1): Patron Q2/Q8 authorize `OllamaSharp` 5.5.0 and `Microsoft.Extensions.AI` 10.11.0 pins plus direct Infrastructure references; deprecated binding is not added (constitution Technology Stack / embeddings row; recon-DEV-412-2 §14).
- No new project, folder, or architectural layer (§2.3 #2): options in existing `Core/Options`, adapter/DI/startup/health in existing `Infrastructure/Adapters`, binding in existing ServiceDefaults, composition in existing Api `Program.cs` (Q4/Q6/Q8).
- No database schema change (§2.3 #3): excluded.
- No public API shape change (§2.3 #4): no routes/DTOs/status codes; no OpenAPI or generated TypeScript.
- No LocalPlay/secrets/`Process.Start` (§2.3 #5): excluded.
- File scope (§2.3 #6): additive edits to named tracked files only; test-factory isolation is forced by Q4/Q5.
- Existing port reuse (constitution ports / ADR-0012): `IEmbeddingGenerator<string, Embedding<float>>` and `EmbeddingDimensions` already exist (DEV-345). No new port or ADR (brief).
- Enumeration rule (constitution): `EmbeddingGenerationException` reuses `EnrichmentFailureCategory`; no project-owned C# `enum` (Q5).
- Principle IV: do not fork enrichment claim/retry/DLQ; reuse categories for downstream FeaturesRequested consumers (Q5).
- Observability (constitution VI / Q8): passive untagged `ollama-embeddings` health check; functional telemetry names in `TelemetryConstants` on existing LamuFlix ActivitySource/Meter; vendor names allowed on Infrastructure adapter and vendor-mandated config keys only.
- Tests and gates (constitution IX; Static-Analysis Gates): unit + integration + property tests; configured scripts; no threshold waiver; scope-empty `SKIPPED` never PASS (brief).

Post-design re-check: the design below adds no port, schema, public HTTP contract, glossary rewrite, or ADR. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-412/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes approach, files, and order. There is no new public HTTP contract. No `design.md` (no ui tag).

### Source Code (repository root)

```text
Directory.Packages.props
src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj
src/LamuFlix.Core/Options/OllamaOptions.cs
src/LamuFlix.Core/Options/OllamaResilienceOptions.cs
src/LamuFlix.Core/Ports/EmbeddingGenerationException.cs
src/LamuFlix.Core/Pipeline/TelemetryConstants.cs
src/LamuFlix.Infrastructure/Adapters/OllamaSharpEmbeddingGenerator.cs
src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingServiceCollectionExtensions.cs
src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingStartupValidator.cs
src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingModelState.cs
src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingHealthCheck.cs
src/LamuFlix.ServiceDefaults/Extensions.cs
src/LamuFlix.Api/Program.cs
src/LamuFlix.Api/appsettings.json

tests/LamuFlix.UnitTests/Options/OllamaOptionsTests.cs
tests/LamuFlix.UnitTests/Options/OllamaResilienceOptionsTests.cs
tests/LamuFlix.UnitTests/EmbeddingGenerationExceptionTests.cs
tests/LamuFlix.IntegrationTests/OllamaEmbeddingGeneratorTests.cs
tests/LamuFlix.IntegrationTests/OllamaEmbeddingResilienceTests.cs
tests/LamuFlix.IntegrationTests/OllamaEmbeddingCompositionTests.cs
tests/LamuFlix.IntegrationTests/OllamaEmbeddingTelemetryTests.cs
tests/LamuFlix.IntegrationTests/ApiHostFactory.cs          # isolate where needed (§15.5)
tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs      # isolate where needed (§15.5)
tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs     # nested ReadyApiHostFactory where needed (§15.5)
```

Approved adapter partial files (complexity / LoggerMessage) and a WireMock probe helper may be added in the same existing folders. FsCheck cases live in the agreed relevant test files using Fact+Trait/`Prop.ForAll` in IntegrationTests. Per-test builders from the §15.5 inventory are edited only when TB004 names them as necessary. No `/web` edit.

**Structure Decision**: Existing projects and `Adapters/` folder only. Registration extension name follows the brief (`OllamaEmbeddingServiceCollectionExtensions`). Composition root calls the new registration from `Program.cs` only in the composition phase so earlier commits do not force unrelated hosts to need Ollama before factory isolation lands.

## Design Overview

1. **Packages.** Add CPM pins for `OllamaSharp` 5.5.0 and `Microsoft.Extensions.AI` 10.11.0 beside existing Abstractions 10.11.0. Add direct unversioned PackageReferences in `LamuFlix.Infrastructure.csproj`. Do not reference `Microsoft.Extensions.AI.Ollama`.

2. **Options.** `OllamaOptions` (`SectionName = "Ollama"`) with `Endpoint` and `ModelName` record initializers carrying the already approved defaults `http://localhost:11434/` and `all-minilm`. `OllamaResilienceOptions` (`SectionName = "Ollama:Resilience"`) mirrors `MetadataProviderResilienceOptions` defaults and `IValidatableObject` cross-field rules (same approved resilience defaults on the record). Bind both via existing `BindAndValidate` / `ValidateOnStart` in `ServiceDefaultsExtensions.AddLamuFlixOptions`. Api `appsettings.json` carries the same endpoint/model defaults. Schedule a focused `AddLamuFlixOptions` startup-validation proof with no `Ollama` section and no provider registration/network (defaults succeed), plus invalid explicit values failing. Preserve ServiceDefaults binding; no Worker host or Api-only binding redesign.

3. **Exception.** `EmbeddingGenerationException` in `Core/Ports` carries `EnrichmentFailureCategory` limited to `ProviderUnavailable`, `RateLimited`, `InvalidResponse`.

4. **DI and resilience.** `AddOllamaEmbeddingGenerator` (in `OllamaEmbeddingServiceCollectionExtensions`) explicitly owns and registers:
   - singleton `OllamaEmbeddingModelState`
   - `AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, OllamaSharpEmbeddingGenerator>` with BaseAddress from options, `HttpClient.Timeout = Timeout.InfiniteTimeSpan` (already chosen in brief), and one `AddStandardResilienceHandler` configured from `OllamaResilienceOptions` with the same retryable classification shape as OMDb
   - hosted validator `OllamaEmbeddingStartupValidator` as `IHostedService` whose awaited `StartAsync` performs verification before readiness (not a background-only `ExecuteAsync` probe)
   - untagged health check `"ollama-embeddings"`
   - `TryAddSingleton(TimeProvider.System)` if not already present by the same pattern as OMDb
   
   Keep `Program.cs` wiring in Phase 3 only. Focused production-registration tests exercise the extension; unrelated hosts stay isolated in Phase 3.

5. **Adapter.** `OllamaSharpEmbeddingGenerator` constructs `OllamaApiClient(httpClient, modelName)` without taking ownership of the client. `GenerateAsync`:
   - empty → empty result, no HTTP, no logical telemetry
   - argument guards before HTTP
   - never send wire `dimensions` (assert absence for caller `Dimensions` null and `Dimensions` 384); always use configured model; MUST NOT forward caller `Dimensions` to the inner client; MUST NOT mutate caller options; no blanket `AdditionalProperties` ban/allowlist
   - map transport/provider outcomes to `EmbeddingGenerationException` after pipeline exhaustion (Q5): `TimeoutRejectedException` → `ProviderUnavailable`; `OperationCanceledException` with caller token cancelled → cancellation; otherwise timeout cancellation → `ProviderUnavailable`. Cover timeout without caller cancellation and cancellation during send/backoff. Do not invent a late-cancellation race precedence rule.
   - validate every returned vector (count, finite, 384)
   - update model-state health on success/typed failure via coherent thread-safe publication/reads/updates of verified configuration/dimensions/health across transient clients; controlled overlapping mixed outcomes must yield legal snapshots, and a later valid success restores Healthy. Do not prescribe locks, priority, or a fixed winner for truly simultaneous completions.
   - record one logical span/duration/outcome with `TimeProvider` (startup probe included in the same per-outbound logical telemetry rule; one logical count including retries; retain existing TelemetryConstants keys and low-cardinality outcome/category-only tags; no new tag taxonomy)
   - `GetService` as ruled (verified metadata / self / null); never leak raw chat/API client surfaces
   - sanitized diagnostics use a safe endpoint identifier excluding URI user-info/query/fragment and unsafe path data; no raw URI/exception/body-bearing inner chains; preserve sanitized model/category/dimension diagnostics. Synthetic credential/query/body values prove the contract. No new endpoint acceptance restriction.

6. **Startup probe.** Hosted service `StartAsync` (awaited, not background-only `ExecuteAsync`) opens a short-lived scope, resolves the typed generator, generates one fixed non-secret probe string through the same resilient adapter path, requires exactly one finite 384-vector, publishes verified dimensions on the singleton state, runs `EmbeddingDimensions.Validate`, and fails the host clearly otherwise. A first-load delay that exhausts the same configured resilience budget fails closed as `ProviderUnavailable` (delayed WireMock case); no separate startup allowance, pull/warm, or cold-versus-outage diagnosis. Delayed verification must not complete host startup early. No concurrent-start bypass of completion.

7. **Health.** `OllamaEmbeddingHealthCheck` reads only `OllamaEmbeddingModelState`: Unhealthy / Healthy / Degraded per brief. Zero HTTP.

8. **Telemetry constants.** Add functional names to `TelemetryConstants.cs` only: `Embedding.Generate`, `lamuflix.embedding.duration`, `lamuflix.embedding.outcome` (and any minimal tag-key reuse already present such as outcome/failure_category). No vendor-named Core constants class. Startup probe listener assertions prove the same one-logical-call rule.

9. **Test isolation.** Cite recon-DEV-412-3 §15.5 inventory: dedicated factories `ApiHostFactory.cs`, `ApiEndToEndFactory.cs`, and nested `ReadyApiHostFactory` in `HealthEndpointTests.cs`, plus the enumerated per-test builders listed there. Existing factory-isolation authorization applies only where needed. Build recon (TB004) must refresh that inventory and name the actual necessary isolation edits before T020. No wholesale inventory edits or newly invented file authority. Dedicated composition tests use production registration against LoopbackWireMock. No production bypass flag.

10. **Proof order (brief).** Phase B entry evidence (merged spec/Gate 1; Conductor-consolidated TB002/TB003 owner/Rigger prerequisite and Epic 7 evidence receipts in `DEV-412`; recon drift including factory-isolation refresh; Gauge baseline; Keel drift check) before any code. Checklist marks must reference those receipts; checklist-only completion is not enough. Phase 1 packages/options/exception + unit/property tests → green → commit. Phase 2 adapter/DI/state/startup/health/telemetry + WireMock/FsCheck vector-property/listener tests → green → commit. Phase 3 ServiceDefaults/API composition/config + necessary factory isolation and composition/regression tests → green → commit. Author tests first; minimum compilable contracts when types are absent; behavioral failure before the behavior edit when possible; missing-type compile failures are not behavioral red. Do not register production startup in an earlier committed phase that leaves unrelated host tests requiring live Ollama. Cog refactor (read `gates.complexity.refactor` from `harness.yml` and pass that configured value to supported `-Threshold`; recon-DEV-412-3 §15.6) then Gauge gates (no-args complexity is the normal changed-diff gate and does not replace stricter refactor evidence).

## Loop discipline

- **Closing bar:** Critical, High, and Medium findings block; Low is nonblocking. A mandatory gate failure or `Could not run` blocks. Scope-empty `SKIPPED`, mutation `NOT APPLICABLE`, and configured `SKIP` stay distinct nonblocking results and are never recorded as PASS.
- **Round cap:** two review rounds, at most two fix commits per round. Third-round findings go to owner review on the PR, not another automatic round.
- **Frozen scope:** Infrastructure OllamaSharp implementation of the existing embedding port, maintained MEAI package wiring, configurable endpoint/model, resilient typed HttpClient, actual 384-dimension startup verification, runtime validation, typed failures, WireMock/property tests, forced composition/config/test isolation, and constitution-required passive health/functional telemetry. Anything else is a follow-up issue, not a finding in this round.

## Complexity Tracking

> No constitution violations requiring justification. Package pins, file placements, health, and telemetry are Patron-ruled under ticket scope and constitution-forced supporting edits without owner checkboxes.
