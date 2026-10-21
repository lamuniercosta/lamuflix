# DEV-347 — Conclusions & Structural Rulings

**Feature:** DEV-347 — Define `FeaturesRequested` message, topology, and publish after enrichment succeeds  
**Phase:** 2 (Product & Scope Grill)  
**Authority:** Patron (Proxy Owner)

## Summary Verdict

All product, scope, and §2.3 structural questions for DEV-347 are settled from the authoritative ticket text (`DEV-347` note lines 25–49), `specs/PRODUCT.md`, `.specify/memory/constitution.md`, and `recon-DEV-347`. There are **zero** owner-only (`blocked: structural`) escalations: nothing contradicts the ticket text or requires a constitution departure.

---

## Product & Scope Rulings (Questions 1–8)

### Q1. Message contract & placement
- **Ruling:** Define `public sealed record FeaturesRequested(MovieId MovieId, int Attempt);` in `src/LamuFlix.Core/Ports/FeaturesRequested.cs` (`namespace LamuFlix.Core.Ports`).
- **Basis:** Ticket *Scope & Technical Design* item 1 (`DEV-347:40`: "`FeaturesRequested(MovieId MovieId, int Attempt)` in `src/LamuFlix.Core/Ports/`, alongside `EnrichmentRequested`"); Constitution Coding Conventions (`constitution.md:426`: data types are `sealed record`s) and Enrichment Reliability Rules (`constitution.md:467-468`, `:480-481`); matches `src/LamuFlix.Core/Ports/EnrichmentRequested.cs:7` (`recon-DEV-347:33`).

### Q2. Port boundary — reuse `IEnrichmentQueue` vs. new port interface
- **Ruling:** Do **not** introduce a new port interface in `LamuFlix.Core/Ports/`. Extend `IEnrichmentQueue` with `Task EnqueueAsync(FeaturesRequested message, CancellationToken ct);` (or keep all queue publishing behind `IEnrichmentQueue` and `RabbitMqEnrichmentQueuePublisher`).
- **Basis:** Constitution Principle I (`constitution.md:136-140`) fixes the eight allowed ports in `Core/Ports/` and requires an ADR to add a port; `ArchitectureTests.cs:28-29,152-174` enforces `PortWhitelistPattern` (`recon-DEV-347:87,96`); Constitution Enrichment Reliability Rules (`constitution.md:480-481`) mandates that `FeaturesRequested` reuses the same pipeline machinery without forking a second pipeline.

### Q3. RabbitMQ exchange, queues, routing keys, and arguments
- **Ruling:** In `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`, keep the single direct durable exchange `lamuflix.enrichment` and all existing enrichment queues (`enrichment.requested`, `enrichment.retry`, `enrichment.dead-letter`) and routing keys (`requested`, `retry`, `dead-letter`) unchanged. Declare three additional quorum queues bound to `lamuflix.enrichment`:
  - `features.requested` (routing key `features.requested`) with `RequestedArguments(enrichment.Value.MaxAttempts)` except `x-dead-letter-routing-key = "features.dead-letter"` (quorum, `x-dead-letter-exchange = "lamuflix.enrichment"`, `x-delivery-limit = MaxAttempts`, `at-least-once`, `reject-publish`).
  - `features.retry` (routing key `features.retry`) with `RetryArguments(RetryDelayMilliseconds(rabbitMq.Value))` except `x-dead-letter-routing-key = "features.requested"` (quorum, `x-message-ttl`, `x-dead-letter-exchange = "lamuflix.enrichment"`, `at-least-once`, `reject-publish`).
  - `features.dead-letter` (routing key `features.dead-letter`) with `DeadLetterArguments()` (quorum).
- **Basis:** Ticket *Scope & Technical Design* item 2 (`DEV-347:41`: declare `features.requested` quorum queue with its own retry queue and dead-letter queue, bound to existing `lamuflix.enrichment` exchange; existing enrichment queues unchanged) and AC 3 (`DEV-347:49`); Constitution Technology Stack (`constitution.md:344`: quorum queues, topology in one `RabbitMqTopology` class) and `constitution.md:480-481`; `RabbitMqTopology.cs:16-25,54-104` (`recon-DEV-347:39-42`).

### Q4. `EnrichmentRouting` responsibilities for `FeaturesRequested`
- **Ruling:** `EnrichmentRouting` (`src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs`) exposes the routing decision/constants needed to route `FeaturesRequested` upon enrichment completion (and/or parallel routing helpers for features queues alongside existing enrichment routing), while preserving `EnrichmentRouting.Decide(ProcessEnrichmentOutcome)` for `enrichment.*` queues unchanged.
- **Basis:** Ticket *Scope & Technical Design* item 2 (`DEV-347:41`: names `RabbitMqTopology` and `EnrichmentRouting`) and AC 3 (`DEV-347:49`: "existing enrichment routing is unchanged"); `EnrichmentRouting.cs:7-33` (`recon-DEV-347:55`).

### Q5. Meaning of "persisted `FullPlot` is non-empty" in the current domain model
- **Ruling:** In the domain model, the persisted plot field on `MovieMetadata` is `Synopsis` (`Movie.Metadata?.Synopsis`, mapped from the OMDb full plot and persisted to `movies.synopsis`). "Persisted `FullPlot` is non-empty" means `movie.Status == EnrichmentStatus.Enriched` (i.e. `MetadataLookupResult.Found` was applied and saved via `SaveChangesAsync`) and `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)`. Neither `MetadataLookupResult.NotFound`, nor a failed/duplicate-IMDb lookup, nor a refused claim (`Claimed: false`), nor a `Found` result with `null`/empty/whitespace `Synopsis` queues `FeaturesRequested`.
- **Basis:** Ticket *Scope & Technical Design* item 3 (`DEV-347:42`: "after enrichment succeeds, publish `FeaturesRequested` only when the persisted `FullPlot` is non-empty. A movie with no plot is not queued") and AC 1–2 (`DEV-347:47-48`); `src/LamuFlix.Core/Domain/MovieMetadata.cs:12,31,53` and `src/LamuFlix.Core/Domain/Movie.cs:41,100-113` (`recon-DEV-347:57`); `ProcessEnrichmentCommandHandler.cs:78-100`.

### Q6. Where the publish step executes ("after enrichment succeeds")
- **Ruling:** The publish of `FeaturesRequested(movie.Id, Attempt: 1)` fires after `movies.SaveChangesAsync(cancellationToken)` succeeds on a `MetadataLookupResult.Found` enrichment with non-empty `Synopsis` (`!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)`). Whether invoked directly by `ProcessEnrichmentCommandHandler` via `IEnrichmentQueue` after `SaveChangesAsync` (preferred so the decision lives in the Core handler per Constitution `constitution.md:473`: "All decisions live in Core handlers so they are unit-tested without RabbitMQ", or coordinated through `ProcessEnrichmentOutcome.Completed` into `EnrichmentConsumer.SettleAsync` / `EnrichmentRouting`), the Core decision (`ShouldQueueFeatures` / non-empty plot check) MUST be unit-testable in Core and the end-to-end consumer settlement in `EnrichmentConsumer` (registered in `LamuFlix.Api`) MUST publish `FeaturesRequested(movie.Id, 1)` to `features.requested` before/upon completing enrichment.
- **Basis:** Ticket *Scope & Technical Design* items 2–4 (`DEV-347:41-43`) and AC 1–2 (`DEV-347:47-48`); Constitution Enrichment Reliability Rules (`constitution.md:472-473`: consumer is thin, decisions live in Core handlers; `constitution.md:477`: silently ignoring a failed publish is forbidden); `ProcessEnrichmentCommandHandler.cs:78-100` and `EnrichmentConsumer.cs:243-268` (`recon-DEV-347:54-57`).

### Q7. Consumer & embedding work scope boundary (DEV-347 vs. DEV-414)
- **Ruling:** DEV-347 defines `FeaturesRequested`, declares the `features.*` queues/bindings in `RabbitMqTopology`, updates `EnrichmentRouting` / `RabbitMqEnrichmentQueuePublisher` / enrichment success path to publish `FeaturesRequested`, and keeps `EnrichmentConsumer` registration in `LamuFlix.Api` (`RabbitMqServiceCollectionExtensions`). Consuming from `features.requested` and generating embeddings are strictly out of scope (split to DEV-414). `src/LamuFlix.Worker` is untouched.
- **Basis:** Ticket *Overview* (`DEV-347:36`: "The consumer and the embedding work are split out to DEV-414") and *Scope & Technical Design* item 4 (`DEV-347:43`: "`src/LamuFlix.Worker` is a legacy project not in `LamuFlix.sln`, so it is not a target"); Constitution Sync Impact (`constitution.md:9-12,22`).

### Q8. Telemetry & Publisher confirms for `FeaturesRequested`
- **Ruling:** Publishing `FeaturesRequested` through `RabbitMqEnrichmentQueuePublisher` reuses the existing producer channel pattern (`publisherConfirmationsEnabled: true`, `publisherConfirmationTrackingEnabled: true`, `mandatory: true`), `System.Text.Json` web-defaults serialization, `TraceContextCarrier.Inject` header propagation, and `TelemetryConstants.EnrichmentEnqueue` producer activity tagged with `TelemetryConstants.MovieId`.
- **Basis:** Constitution Principle VI (`constitution.md:243-250`), Technology Stack (`constitution.md:344,352`), and Enrichment Reliability Rules (`constitution.md:480-481`); `RabbitMqEnrichmentQueuePublisher.cs:48-76,103-104` (`recon-DEV-347:48-49`).

---

## Charter §2.3 Care-List Rulings

1. **Dependencies (New NuGet/npm or swap):**
   - **Ruling:** None. Reuse pinned `RabbitMQ.Client` and `System.Text.Json`.
   - **Basis:** `Directory.Packages.props:10`, `recon-DEV-347:95`, `constitution.md:344,352`.
2. **Architecture (New project, top-level folder, or layer / port):**
   - **Ruling:** No new project, folder, layer, or Core port interface. `FeaturesRequested` is a message record in `src/LamuFlix.Core/Ports/`; publishing reuses `IEnrichmentQueue` and `src/LamuFlix.Infrastructure/RabbitMq/`.
   - **Basis:** Ticket `DEV-347:40-43`; `constitution.md:136-140`; `ArchitectureTests.cs:28-29,152-174` (`recon-DEV-347:96`).
3. **Database Schema:**
   - **Ruling:** None. No entity or migration changes (`movie_features` belongs to DEV-413).
   - **Basis:** Ticket `DEV-347:39-49`; `recon-DEV-347:97`.
4. **Public API Shape (routes, DTOs, status codes, OpenAPI):**
   - **Ruling:** None. Internal domain/messaging change only; `web/src/api/openapi.json` is unaffected.
   - **Basis:** Ticket `DEV-347:39-49`; `recon-DEV-347:98`.
5. **Security & Local Execution (`Features:LocalPlay`, secrets, `Process.Start`):**
   - **Ruling:** None touched.
   - **Basis:** `recon-DEV-347:99`.
6. **File Scope (Modifying existing tracked files not explicitly named in ticket):**
   - **Ruling:** Approved in-scope edits to existing tracked files required by the ticket's *Scope & Technical Design* and Acceptance Criteria:
     - `src/LamuFlix.Core/Ports/IEnrichmentQueue.cs` (and new `src/LamuFlix.Core/Ports/FeaturesRequested.cs`)
     - `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs` and/or `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentOutcome.cs`
     - `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`
     - `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs`
     - `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs`
     - `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`
     - Corresponding unit and integration test files under `tests/LamuFlix.UnitTests/` and `tests/LamuFlix.IntegrationTests/` (and `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` if queue reset/clean-up enumerates declared queues).
   - **Basis:** Forced by Ticket `DEV-347:40-49` and Constitution Principle IX (`constitution.md:304-328`); `recon-DEV-347:33-88,100`.
