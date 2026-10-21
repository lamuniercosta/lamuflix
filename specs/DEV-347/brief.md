# DEV-347 — Phase 2 Grill Brief

## Authority, identity, and status

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-347-spec`; branch: `feature/347-spec`.
- Original Patron grill commit: `a33da64e1a4fc7efa5738f9f53d4b94736c1b8b0`. Verified Q9 commit and current HEAD: `8919fdd1441081ab2d8b5cd699c7ad8b3194ac6c`. On resumption, the existing uncommitted brief was the sole working-tree change.
- Ticket source: current canvas note `DEV-347:44-61`; Q9 receipt and resumption are recorded at `DEV-347:22-35`. This is an M ticket with no UI tag (`recon-DEV-347:16-18`).
- Decision authority: Patron, `CONCLUSIONS.md:5-9`; ticket precedence: `specs/PRODUCT.md:21-24`; structural rules: `specs/PRODUCT.md:32-48`.
- Recon source: `recon-DEV-347`. Its receipt describes intake HEAD `347828bff3a6086784e2b5535aaa5ace45b3a100`, not the current Patron documentation commit. Its statements that the ticket body and spec directory were absent are historical intake facts, superseded by the supplied ticket note and Patron artifacts. No fresh code recon was performed.
- Status: Phase 2 grill closed using Patron Q1-Q9, including the Q6 clarification and Q9 recovery contract. No unresolved decision remains in this brief. This is grill closure, not plan freeze, provisional Gate 1, or merged Gate 1.

## Closing bar and round cap

Closing bar met: every ticket requirement is retained, all nine Patron answers and all six care-list rulings are carried below, and the three assumptions remain tagged. Patron Q9 supplies the cited reliability answer (`CONCLUSIONS.md:42-48`); Q6 now fixes direct Core publication (`:38-40`). No new scope or product ruling is supplied by Keel. Patron recorded zero owner-only escalations (`CONCLUSIONS.md:7-9`; task note `DEV-347:30-32`).

The implementation acceptance bar remains the three ticket AC verbatim below, plus the recorded topology, Core decision, publish-confirm, tracing, and scope constraints. Quill must draft spec, plan, and tasks from this brief, preserving Patron Q9 without adding a recovery mechanism (`task-pipeline:45,76-79`). Subsequent read-only analysis, plan challenge, freeze, and the spec PR remain separate steps (`task-pipeline:79-83`).

Budgets: at most 12 grill questions; after that the pipeline requires recommendations tagged `[assumed]`. Patron has recorded Q1-Q9 (nine questions, within the cap); this brief adds no new ruling. Review/challenge work uses at most two rounds, with third-round findings carried to user review on the PR; remediation allows at most two fix commits per round (`task-pipeline:25-29`).

## Ticket scope and acceptance criteria — verbatim

Current canvas line numbers below supersede the older ticket-note line numbers embedded in Patron citations; the Patron text itself is preserved without rewriting.

Overview (`DEV-347:48`):

> Define the `FeaturesRequested` message, its RabbitMQ topology and routing, and the publish step that fires after enrichment succeeds. The consumer and the embedding work are split out to DEV-414.

Scope & Technical Design (`DEV-347:52-55`):

1. **Message**: `FeaturesRequested(MovieId MovieId, int Attempt)` in `src/LamuFlix.Core/Ports/`, alongside `EnrichmentRequested`.
2. **Topology**: in `src/LamuFlix.Infrastructure/RabbitMq/` (`RabbitMqTopology`, `EnrichmentRouting`), declare a `features.requested` quorum queue with its own retry queue and dead-letter queue, bound to the existing `lamuflix.enrichment` exchange. The existing enrichment queues stay unchanged.
3. **Publish**: after enrichment succeeds, publish `FeaturesRequested` only when the persisted `FullPlot` is non-empty. A movie with no plot is not queued.
4. **Location**: the RabbitMQ code lives in `LamuFlix.Infrastructure`, and the consumer is registered in `LamuFlix.Api`. `src/LamuFlix.Worker` is a legacy project not in `LamuFlix.sln`, so it is not a target.

Acceptance Criteria (`DEV-347:59-61`):

- A successful enrichment with a non-empty plot publishes exactly one `FeaturesRequested` message.
- A successful enrichment with an empty plot publishes nothing.
- The features queues are declared with a dead-letter binding, and the existing enrichment routing is unchanged.

## Frozen scope boundary

The scope envelope is the ticket text above and Patron Q1-Q9 and care-list rulings reproduced below. It includes the message, the three features queues and their bindings/arguments, routing support, the existing publisher extension, and the post-save enrichment-success publish. It excludes consuming features messages, embeddings (DEV-414), `movie_features` schema work (DEV-413), a new port/project/layer/dependency, HTTP/OpenAPI/web changes, local playback, secrets, and the legacy Worker (`CONCLUSIONS.md:15-86`).

`specs/PRODUCT.md:17` defers Recommendations execution until Epic 7 completes; this Phase A brief authorizes no Phase B start or exception to that sequencing. The ticket and Patron Q7 place the consumer in Api; PRODUCT's general Worker wording (`specs/PRODUCT.md:10`) is not a decision to move this feature into a legacy worker. The constitution requires ports and adapters with a feature-organised Core (`.specify/memory/constitution.md:115-147`); PRODUCT's general architecture wording does not authorize a new layer.

## Recorded grill answers and cited bases — verbatim

The following is the complete current Patron record from `CONCLUSIONS.md:13-86`. Q1-Q5, Q7-Q8 and all care-list rulings are preserved verbatim; Q6 is carried in its Patron-refined form, superseding the former consumer-coordination alternative. Q9 is included verbatim with its cited basis. Embedded evidence citations are Patron citations, not independently verified new recon; historical ticket-note line numbers inside the record are preserved. Current ticket locations appear above. The `FullPlot` to `Synopsis` mapping remains Patron Q5, not a new Keel fact claim.

## Product & Scope Rulings (Questions 1–9)

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
- **Ruling:** The publish of `FeaturesRequested(movie.Id, Attempt: 1)` fires after `movies.SaveChangesAsync(cancellationToken)` succeeds on a `MetadataLookupResult.Found` enrichment with non-empty `Synopsis` (`!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)`). `ProcessEnrichmentCommandHandler.CompleteAsync` invokes `await queue.EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken);` directly after `await movies.SaveChangesAsync(cancellationToken);` when `result is MetadataLookupResult.Found` and `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)` (so the eligibility decision lives in Core and is unit-tested without RabbitMQ per `constitution.md:473`, and `EnrichmentConsumer.SettleAsync` keeps its single post-handler `AckAsync` path without a second consumer-side publish).
- **Basis:** Ticket *Scope & Technical Design* items 2–4 (`DEV-347:48-51`) and AC 1–2 (`DEV-347:55-56`); Constitution Enrichment Reliability Rules (`constitution.md:472-473`: consumer is thin, decisions live in Core handlers; `constitution.md:477`: silently ignoring a failed publish is forbidden); `ProcessEnrichmentCommandHandler.cs:78-100` and `EnrichmentConsumer.cs:243-268` (`recon-DEV-347:54-57`).

### Q9. Post-save `FeaturesRequested` publish failure, ambiguous broker confirmation, and recovery contract
- **Ruling:** Follow the existing save-then-enqueue dual-write contract (`ADR-0005` and `RequestEnrichmentCommandHandler.cs:22-25` / `EnrichmentConsumer.cs:206-228,259-267`):
  1. **No silent failure & in-process propagation:** `ProcessEnrichmentCommandHandler.CompleteAsync` awaits `queue.EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken)` after `movies.SaveChangesAsync(cancellationToken)` without catching publish/confirm exceptions. In `RabbitMqEnrichmentQueuePublisher`, mandatory routing and publisher confirms (`publisherConfirmationsEnabled: true`, `publisherConfirmationTrackingEnabled: true`, `mandatory: true`) throw on unroutable messages, broker nack, or channel/confirm failure, and propagate `OperationCanceledException` when `stoppingToken` cancels.
  2. **Consumer settlement on publish/confirm failure:** In `EnrichmentConsumer.HandleAsync` (`EnrichmentConsumer.cs:206-228`), if `stoppingToken` is cancelled during the publish/confirm await, the consumer catches `OperationCanceledException when (stoppingToken.IsCancellationRequested)` and nacks the `EnrichmentRequested` delivery with `requeue: true` (`constitution.md:478-479`); any other publish/confirm exception hits the unhandled `catch (Exception)` block, which marks the activity error, logs `LogError`, and dead-letters the `EnrichmentRequested` delivery (`BasicNackAsync(..., requeue: false)`) without success-acking (`EnrichmentConsumerTests.cs:417-436`).
  3. **No ad-hoc retry, rollback, or re-enrichment on refused claim:** The persisted `Movie` row remains `Enriched` in PostgreSQL (no compensating DB rollback and no ad-hoc in-handler retry loop, per `constitution.md:474-477`). If the `EnrichmentRequested` message is redelivered after the DB commit (e.g., host shutdown during publish/confirm), `TryClaimForEnrichmentAsync` returns `false`, `ProcessEnrichmentCommandHandler` returns `Completed(Claimed: false)` without calling OMDb or re-publishing `FeaturesRequested` (preserving Q5 and `constitution.md:469-470`), and the consumer acks the duplicate delivery as skipped.
  4. **Scope of "exactly one" AC & eventual recovery:** Ticket AC 1 ("A successful enrichment with a non-empty plot publishes exactly one `FeaturesRequested` message", `DEV-347:55`) specifies the 1-to-1 emission cardinality of a completed enrichment execution (one `EnqueueAsync(FeaturesRequested(movie.Id, 1))` call per successful non-empty-plot enrichment, vs. zero for empty plot / NotFound / failed lookup / refused claim), not distributed two-phase commit or transactional outbox (`ADR-0005:21-31`, `constitution.md:476-477`). Eventual recovery of a movie that is `Enriched` with a non-empty `Synopsis` but lacks a `movie_features` row after a post-save publish outage belongs to the downstream features consumer/sweeper pipeline (`DEV-413`/`DEV-414`, `constitution.md:480-481`), not DEV-347.
- **Basis:** Ticket `DEV-347:48-57`; Constitution Enrichment Reliability Rules (`constitution.md:469-481`); `ADR-0005.md:9-31` ("Publish failures are loud... Stranded work is recovered by a sweeper, not by distributed transactions... A transactional outbox is the stretch goal"); `ProcessEnrichmentCommandHandler.cs:78-100`, `RequestEnrichmentCommandHandler.cs:22-25`, `EnrichmentConsumer.cs:206-228,259-267`, and `EnrichmentConsumerTests.cs:417-436` (`recon-DEV-347:54-57,64,85`).

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

## Recorded assumptions — verbatim

Source: `ASSUMPTIONS.md:6-8`.

- `[assumed]` **Features queue & routing key naming convention:** Following `enrichment.requested` / `enrichment.retry` / `enrichment.dead-letter` in `RabbitMqTopology` and the ticket's explicit queue name `features.requested` (`DEV-347:41`), the companion retry and dead-letter queues are named `features.retry` and `features.dead-letter`, and the routing keys on the shared `lamuflix.enrichment` direct exchange are `features.requested`, `features.retry`, and `features.dead-letter` (keeping `requested`, `retry`, and `dead-letter` untouched for enrichment).
- `[assumed]` **Initial attempt number:** When enrichment succeeds with a non-empty plot, `FeaturesRequested` is published with `Attempt = 1`, matching the 1-based attempt convention used by `EnrichmentRequested(movie.Id, Attempt: 1)` across all enqueue handlers (`RequestEnrichmentCommandHandler`, `ImportMovieFolderCommandHandler`, `MatchMovieImdbIdCommandHandler`, `RequeueStrandedMoviesCommandHandler`).
- `[assumed]` **Non-empty plot predicate:** "Non-empty" `FullPlot` (`MovieMetadata.Synopsis`) uses `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)` so whitespace-only plot strings are treated the same as empty/null and are not queued.

## Plan decisions for Quill

### Approach and boundaries

Use Patron Q6's fixed approach: the Core enrichment handler owns the eligibility decision and calls the existing `IEnrichmentQueue` after a successful save. Add the `FeaturesRequested` overload under Q2 and extend the existing RabbitMQ publisher under Q8. Do not introduce a second publishing abstraction. The initial attempt is 1. Keep the consumer thin and existing enrichment routing semantics intact. Patron Q6 fixes this single publish owner and Q9 fixes the failure/settlement contract (`CONCLUSIONS.md:19-48,54-56`); the earlier consumer-coordination alternative is superseded.

The eligibility predicate, as ruled by Patron Q5, requires saved `Enriched` status and non-whitespace `Movie.Metadata?.Synopsis`. NotFound, failed/duplicate-IMDb lookup, refused claim, and null/empty/whitespace plot do not publish. Preserve all three ticket AC, including the words “exactly one”; do not replace that AC with “at least once” or assert an unruled distributed exactly-once guarantee.

Keep declarations in the one `RabbitMqTopology` owner. Add the features route support in `EnrichmentRouting` without changing `Decide(ProcessEnrichmentOutcome)` for enrichment. Preserve publisher confirms, mandatory routing, web-default JSON, trace injection, and the existing producer activity. Failed publish must propagate under the settled Q9 contract below (`CONCLUSIONS.md:42-48`; constitution `:471-481`). No transactional outbox, schema change, new retry loop, or new sweeper state is authorized by this brief.

### Files touched

Patron-approved production file envelope (`CONCLUSIONS.md:77-86`):

- New `src/LamuFlix.Core/Ports/FeaturesRequested.cs`.
- `src/LamuFlix.Core/Ports/IEnrichmentQueue.cs`.
- `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs`. Patron retains `ProcessEnrichmentOutcome.cs` in the approved file envelope, but Q6/Q9 require no new outcome-driven consumer publish; retain its existing claim outcome semantics.
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`.
- `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs`.
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs`.
- `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs` remains in the approved envelope; preserve the existing Q9 settlement contract and single post-handler ack path, with no second consumer-side publish.

Corresponding tests identified by recon (`recon-DEV-347:75-88`):

- `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs`.
- `tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs` and `RabbitMqServiceCollectionExtensionsTests.cs` where the changed behavior requires coverage.
- `tests/LamuFlix.IntegrationTests/RabbitMqTopologyTests.cs`, `RabbitMqPublisherTests.cs`, and `EnrichmentConsumerTests.cs`.
- `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` only if reset/cleanup needs the added queues, as Patron explicitly permits.

Keep existing Api hosting/registration; recon locates it in `RabbitMqServiceCollectionExtensions.cs` (`recon-DEV-347:50,69`), but no registration rewrite or other production file expansion is decided here. Return any necessary additional file-scope ruling to Patron. Do not alter the port whitelist to add another interface.

### Test strategy

Use xUnit v3, Shouldly, NSubstitute for Core ports, and the existing RabbitMQ/Postgres Testcontainers fixtures for actual transport/persistence behavior. Preserve the repo's test-data conventions and test pyramid (constitution `:304-328`).

- Core eligibility: Found with a saved non-whitespace plot publishes the movie ID with Attempt 1 exactly once, after save; null, empty, whitespace, NotFound, failed/duplicate lookup, and refused claim publish nothing. Save failure must not reach publish. Use observable call ordering rather than only an enqueue count.
- Core failure propagation: after save succeeds, prove that publish/confirm exceptions propagate and the persisted row remains Enriched; no rollback or in-handler retry. A refused claim on redelivery returns Completed(Claimed: false) with no metadata-provider call or features republish. Verify this separately from normal one-call emission cardinality (`CONCLUSIONS.md:42-48`).
- Real broker topology: all three new queues are quorum queues on the existing durable direct exchange; assert exact features routing keys, requested delivery/DLX arguments, retry TTL and route back to requested, dead-letter binding, and idempotent declaration. Preserve assertions for every existing enrichment queue, key, and argument (`recon-DEV-347:39-43,83`).
- Publisher: round-trip the typed message to `features.requested`, preserve JSON contract and trace headers, and exercise mandatory unroutable/unreachable failure using the existing integration-test pattern (`recon-DEV-347:48-49,84`).
- Consumer end-to-end: successful persisted non-empty plot yields one features message and normal enrichment settlement; successful empty plot yields none. Post-save publish/confirm failure marks the activity error, logs an error, and nacks the enrichment delivery with requeue: false, without a success ack. Cancellation while stopping nacks with requeue: true. Redelivery after commit with a refused claim is acked as skipped with no features republish. Exercise the ambiguous-confirm exception path without assuming whether the broker accepted the features message. Retain enrichment retry, DLQ, false-claim, and failed-republish regression coverage (`CONCLUSIONS.md:42-48`; `recon-DEV-347:85`). Do not create a features consumer, downstream sweeper, or eventual-recovery test for this ticket.
- Architecture tests must continue to pass with the existing ports and dependency boundaries (`recon-DEV-347:87`; constitution `:127-141`). No new domain-invariant property-test requirement or property-test opt-out is assumed; Gauge applies the pipeline rule below.

### Gate expectations

This ask runs no tests, gate scripts, Spec Kit, or analysis. Current Phase A next steps belong to Quill, then Keel's separate read-only analyze and plan comparison, followed by challenge and provisional Gate 1 (`task-pipeline:76-83`). Documentation completion is not a runtime gate pass.

For Phase B, Gauge owns the full gate run on the implementation diff and again after rebase: Roslyn, complexity, InspectCode with no arguments (never `-All`), mutation for changed production C#, property tests, vulnerable packages, format verification, and `dotnet test`; web gates report scope-empty if web remains unchanged (`task-pipeline:98-108,127-129`). The constitution sets complexity 15 during implementation and 6 at refactor; configured harness thresholds remain authoritative, never lowered (constitution `:400-417`; PRODUCT `:12`).

Exit 1 and “Could not run” block. Scope-empty exit 2 is `SKIPPED (scope-empty)`, non-blocking and never PASS. Mutation's own exit-2 `NOT APPLICABLE` is N/A, never PASS. A property-test exit 2 for no tagged tests requires a recorded task-note `propertyTests: opt-out — <reason>`; this brief grants no opt-out. Harness-disabled SKIP remains SKIP. Mutation is Gauge's gate, not a separate architect run (`task-pipeline:97-108,133`). M review requires all three axes before adjudication (`task-pipeline:114-119`).

### Task ordering constraints

1. Patron Q9 and the refined Q6 are recorded in this brief; grill closure is complete. Carry their settled single-owner publish, propagation, settlement, refused-claim, cardinality, and downstream-recovery boundary into every planning artifact. Do not reopen them as an implementation choice.
2. Quill drafts spec, plan, and tasks from this brief and the supplied recon; Keel separately analyzes read-only and checks them against the brief. Plan challenge and freeze follow; no implementation starts from this document alone.
3. Gate 1 is the user-merged spec PR, not this brief or provisional status. Phase B also honors the Epic 7 sequencing rule and its fresh drift recon (`PRODUCT.md:17`; `task-pipeline:18,65,83`).
4. Implement message/port contract before callers and adapter changes; topology/routing and publisher support must exist before exercising the full post-save publish path. Core publication and consumer settlement must have one publish owner, not duplicate publish steps.
5. Extend fixture isolation/cleanup before tests rely on the new queues. Add focused unit and real-infrastructure tests with the relevant implementation tasks; run end-to-end tests after the producer path is wired. Keep regression checks for enrichment routing throughout.
6. Anvil implements one tasks.md phase per ask and commits each phase; Cog refactors the completed diff, then Gauge runs gates. Follow the two-round review cap. This Spec workspace stops after its Phase A handoff; no seat merges (`task-pipeline:7,83,88-109`).

## Resolved reliability decision — Patron Q9

The former pending post-save publish question is resolved by `CONCLUSIONS.md:42-48` at commit `8919fdd1441081ab2d8b5cd699c7ad8b3194ac6c` (task note `DEV-347:30-32`). Q9 is reproduced verbatim above; these are its required planning consequences:

- Core awaits `EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken)` immediately after the successful save, for the Q5 eligible Found result. It does not catch publish/confirm exceptions or add a retry loop. Q6 leaves the consumer with one post-handler ack path and no second publish.
- Mandatory routing and publisher confirms surface unroutable delivery, nack, channel/confirm failure, and cancellation. On stopping-token cancellation, the consumer nacks the enrichment delivery with requeue: true. Other publish/confirm exceptions mark the activity error, log an error, and nack with requeue: false (dead-letter), without a success ack.
- The saved row stays Enriched; there is no compensating rollback. A post-commit redelivery whose claim is refused returns Completed(Claimed: false), calls neither OMDb nor features publish, and is acked as skipped.
- The ticket AC remains verbatim. Patron interprets “exactly one” as one features enqueue call per completed successful non-empty-plot enrichment execution, zero for the excluded cases; it is not a distributed transaction or outbox guarantee. Ambiguous confirmation is a loud failure and may leave broker acceptance uncertain; DEV-347 promises no duplicate-free distributed delivery or guaranteed repair after that failure.
- Eventual recovery of Enriched movies with a non-empty Synopsis but no movie_features row belongs to the downstream DEV-413/DEV-414 consumer/sweeper work. This is an assigned scope boundary, not evidence that downstream recovery is already implemented. No schema, outbox, recovery marker, features consumer, or sweeper work is added to DEV-347.

Basis: Patron Q6/Q9 (`CONCLUSIONS.md:38-48`), constitution `:469-481`, and the cited recon/ADR references preserved in Q9. No new product ruling, ticket amendment, or constitution departure is supplied by Keel. There are no remaining needs-decision items in this brief.

## Handoff

Artifact: `specs/DEV-347/brief.md`, updated and uncommitted at verified current HEAD `8919fdd1441081ab2d8b5cd699c7ad8b3194ac6c` on `feature/347-spec`. Phase 2 grill closed; all previously required brief sections remain present.

Exact next action: Conductor sends Quill Phase 2 step 3.2 in this worktree and branch, with `brief.md`, `recon-DEV-347`, and `specs/PRODUCT.md`, to run `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` in one ask, setting `$env:PYTHONUTF8='1'` first (`task-pipeline:76-78`). Quill reports files written and returns any genuine new gap as `needs decision:` rather than guessing. Keel's read-only analyze is the subsequent separate step (`task-pipeline:79`). No Spec Kit, analyze, commit, test, or gate was run in this closure ask. No plan freeze, provisional Gate 1, merged Gate 1, or implementation authorization is claimed.

## Plan challenge editorial decisions

Q1-Q9, cited historical text, three assumptions, approved production/test envelope, and approved phase map remain unchanged. Current ticket references use canvas note DEV-347, Ticket, Overview; Scope & Technical Design items 1-4; Acceptance Criteria bullets 1-3, cross-linked to brief section Ticket scope and acceptance criteria - verbatim. Historical numeric note references in Patron records and verbatim mirrors remain historical; semantic locators supersede current-location claims at brief lines 7 and 22-35 without changing quoted text. Scope item 1 supports Q1; item 2 and AC3 support Q3/Q4; item 3 and AC1-2 support Q5/Q6/Q9; Overview and item 4 support Q7; full ticket supports care-list scope. Resolve gate thresholds from harness.yml at execution for required implementation/refactor stages, never copied or overridden as numeric plan/task literals; this supersedes brief line 180 without changing gate bar. Scope statements describe delivery boundary and do not preclude findings about failures to meet in-scope requirements. Q5 FullPlot/Synopsis and whitespace interpretation and Q9 emission-cardinality interpretation remain existing Patron rulings, not new ticket changes. Basis: Ledger F1-F4; brief.md:20-49,72-86,115-163,176-191; CONCLUSIONS.md:34-48,77-86; spec.md:64-67,88-89; current DEV-347 Ticket section. Editorial decision only; no product ruling or constitution departure.
