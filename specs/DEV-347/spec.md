# Feature Specification: DEV-347 FeaturesRequested message, topology, and publish after enrichment succeeds

**Feature Branch**: `feature/347-spec`

**Created**: 2026-10-21

**Status**: Draft

**Input**: Ticket DEV-347 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-347/brief.md` (Q1–Q9 closed); facts from note `recon-DEV-347`; Patron rulings in `specs/DEV-347/CONCLUSIONS.md`; taste receipt `specs/DEV-347/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Publish FeaturesRequested after successful enrichment with a non-empty plot (Priority: P1)

When metadata enrichment succeeds for a movie and the persisted plot is non-empty, the system enqueues exactly one `FeaturesRequested` message for that movie so the later features/embedding pipeline can pick up work. The publish owner is the Core enrichment success path after a successful save; the enrichment consumer keeps a single post-handler ack path and does not publish a second features message.

**Why this priority**: Ticket Acceptance Criteria bullet 1 requires exactly one `FeaturesRequested` on successful enrichment with a non-empty plot (canvas note DEV-347 Ticket; brief section Ticket scope and acceptance criteria — verbatim).

**Independent Test**: Unit tests on `ProcessEnrichmentCommandHandler` prove one `EnqueueAsync(FeaturesRequested(movie.Id, 1))` after a successful save for a Found enrichment with non-whitespace `Synopsis`; integration consumer tests prove one features message on the features requested queue with normal enrichment settlement (`recon-DEV-347:75-85`; brief Test strategy).

**Acceptance Scenarios**:

1. **Given** a movie claimed for enrichment and a `MetadataLookupResult.Found` result whose applied metadata yields a non-whitespace `Movie.Metadata.Synopsis`, **When** `ProcessEnrichmentCommandHandler.CompleteAsync` saves successfully, **Then** it awaits exactly one `queue.EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken)` after `movies.SaveChangesAsync`, and the movie remains `EnrichmentStatus.Enriched`.
2. **Given** that same successful non-empty-plot enrichment through the hosted enrichment consumer, **When** the handler completes and settlement runs, **Then** exactly one `FeaturesRequested` message is published to the features requested route and the `EnrichmentRequested` delivery is success-acked without a second features publish from the consumer.
3. **Given** a successful save on an eligible Found result, **When** publish/confirm of `FeaturesRequested` throws (unroutable, nack, channel/confirm failure) or cancellation occurs, **Then** the exception propagates (no silent catch in Core), the persisted row stays `Enriched`, and consumer settlement follows Patron Q9 (stopping-token cancellation nacks enrichment with `requeue: true`; other publish/confirm failures mark the activity error, log an error, and nack with `requeue: false` without a success ack).

---

### User Story 2 - Skip FeaturesRequested when the plot is empty or enrichment is not eligible (Priority: P2)

When enrichment does not leave a usable persisted plot, or enrichment does not claim/complete as a Found enrichment, the system publishes no `FeaturesRequested` message. Whitespace-only plots are treated as empty.

**Why this priority**: Ticket Acceptance Criteria bullet 2 requires that a successful enrichment with an empty plot publishes nothing (canvas note DEV-347 Ticket; brief section Ticket scope and acceptance criteria — verbatim; brief Q5).

**Independent Test**: Unit tests cover null, empty, and whitespace `Synopsis`, NotFound, failed/duplicate-IMDb lookup, refused claim, and save failure before publish; consumer end-to-end covers successful empty plot with no features message (`recon-DEV-347:77,85`; brief Test strategy).

**Acceptance Scenarios**:

1. **Given** a Found enrichment whose saved `Movie.Metadata.Synopsis` is null, empty, or whitespace-only, **When** `CompleteAsync` finishes after a successful save, **Then** no `FeaturesRequested` enqueue occurs.
2. **Given** `MetadataLookupResult.NotFound`, a failed or duplicate-IMDb lookup path that does not yield an eligible Found+non-empty plot, or a refused claim (`Completed(Claimed: false)`), **When** the handler returns, **Then** no `FeaturesRequested` enqueue occurs and no OMDb/features republish is introduced for the refused-claim path.
3. **Given** a save failure before publish, **When** the handler fails, **Then** publish is never reached.
4. **Given** a post-commit redelivery whose claim is refused, **When** the consumer settles, **Then** the delivery is acked as skipped with no features republish (brief Q9).

---

### User Story 3 - Declare features queues with dead-letter binding and leave enrichment routing unchanged (Priority: P3)

The RabbitMQ topology declares three features quorum queues on the existing `lamuflix.enrichment` exchange, each with the routing and dead-letter arguments required for request/retry/dead-letter, while every existing enrichment queue, routing key, and argument stays unchanged. Routing helpers expose features routes without changing `EnrichmentRouting.Decide(ProcessEnrichmentOutcome)` for enrichment outcomes.

**Why this priority**: Ticket Acceptance Criteria bullet 3 requires features queues with a dead-letter binding and unchanged enrichment routing (canvas note DEV-347 Ticket; brief section Ticket scope and acceptance criteria — verbatim; brief Q3–Q4).

**Independent Test**: Real-broker topology tests assert the three new quorum queues, exact features routing keys and arguments, dead-letter binding, idempotent declaration, and every existing enrichment queue/key/argument assertion still passes; unit routing tests keep enrichment dispositions unchanged (`recon-DEV-347:39-42,75,83`).

**Acceptance Scenarios**:

1. **Given** `RabbitMqTopology.EnsureDeclaredAsync` / `DeclareAsync`, **When** topology is declared, **Then** `features.requested`, `features.retry`, and `features.dead-letter` exist as quorum queues bound to durable direct exchange `lamuflix.enrichment` with routing keys `features.requested`, `features.retry`, and `features.dead-letter`.
2. **Given** the features requested queue, **When** its arguments are read, **Then** they match the enrichment requested pattern except `x-dead-letter-routing-key = "features.dead-letter"` (including `x-dead-letter-exchange = "lamuflix.enrichment"`, `x-delivery-limit = MaxAttempts`, `at-least-once`, `reject-publish`).
3. **Given** the features retry queue, **When** its arguments are read, **Then** they match the enrichment retry pattern except `x-dead-letter-routing-key = "features.requested"` (including TTL and dead-letter back to the features requested route).
4. **Given** the features dead-letter queue, **When** its arguments are read, **Then** they match `DeadLetterArguments()` (quorum).
5. **Given** existing enrichment queues `enrichment.requested`, `enrichment.retry`, and `enrichment.dead-letter` with keys `requested` / `retry` / `dead-letter`, **When** topology and `EnrichmentRouting.Decide` are exercised, **Then** those queues, bindings, arguments, and enrichment dispositions are unchanged.

### Edge Cases

- "Persisted `FullPlot` is non-empty" means saved `EnrichmentStatus.Enriched` from a Found lookup and `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)` (brief Q5; ASSUMPTIONS non-empty plot predicate).
- Initial `FeaturesRequested.Attempt` is `1` (ASSUMPTIONS initial attempt number).
- Ticket AC "exactly one" means one features enqueue call per completed successful non-empty-plot enrichment execution, not a distributed two-phase commit or transactional outbox (brief Q9).
- Ambiguous broker confirmation after a loud publish/confirm failure may leave broker acceptance uncertain; DEV-347 adds no outbox, recovery marker, features consumer, or sweeper (brief Q9; eventual recovery belongs to DEV-413/DEV-414).
- Consuming `features.requested` and embedding generation are out of scope (DEV-414). `movie_features` schema is out of scope (DEV-413). Legacy `src/LamuFlix.Worker` is not a target (canvas note DEV-347 Ticket Scope & Technical Design item 4; brief section Ticket scope and acceptance criteria — verbatim; brief Q7).
- No new Core port interface, project, layer, NuGet/npm dependency, HTTP/OpenAPI/web change, schema/migration, `Features:LocalPlay`, secrets, or `Process.Start` (brief care-list; `specs/PRODUCT.md` §5).
- PRODUCT defers Recommendations execution until Epic 7 completes (`specs/PRODUCT.md:17`); this ticket only defines the features request message/topology/publish seam and does not start Phase B Recommendations work.
- Scope-empty gate exit 2 is `SKIPPED (scope-empty)`, non-blocking and never PASS; Exit 1 and Could not run remain blocking (brief Gate expectations). This brief grants no `propertyTests` opt-out.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Add `public sealed record FeaturesRequested(MovieId MovieId, int Attempt);` in `src/LamuFlix.Core/Ports/FeaturesRequested.cs` (`namespace LamuFlix.Core.Ports`), alongside `EnrichmentRequested` (brief Q1; `recon-DEV-347:33`).
- **FR-002**: Extend existing `IEnrichmentQueue` with `Task EnqueueAsync(FeaturesRequested message, CancellationToken ct);`. Do not introduce a new port interface or alter the architecture port whitelist (brief Q2; `recon-DEV-347:34,87,96`).
- **FR-003**: In `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`, keep exchange `lamuflix.enrichment` and all existing enrichment queues/keys unchanged; declare quorum queues `features.requested`, `features.retry`, and `features.dead-letter` with the Q3 arguments and bindings (brief Q3; `recon-DEV-347:39-42`).
- **FR-004**: Extend `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs` with routing constants/helpers needed to route `FeaturesRequested`, without changing `EnrichmentRouting.Decide(ProcessEnrichmentOutcome)` enrichment semantics (brief Q4; `recon-DEV-347:55`).
- **FR-005**: Implement `FeaturesRequested` publishing in `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs` reusing mandatory routing, publisher confirms, `System.Text.Json` web defaults, `TraceContextCarrier.Inject`, and `TelemetryConstants.EnrichmentEnqueue` tagged with `TelemetryConstants.MovieId` (brief Q8; `recon-DEV-347:47-49`).
- **FR-006**: In `ProcessEnrichmentCommandHandler.CompleteAsync`, after successful `movies.SaveChangesAsync` on a Found result with non-whitespace `Synopsis`, await `queue.EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken)` without catching publish/confirm exceptions. Do not add a second consumer-side features publish; preserve `EnrichmentConsumer` single post-handler ack path and Q9 settlement behavior (brief Q6, Q9; `recon-DEV-347:54-57`).
- **FR-007**: Eligibility for publish requires Found enrichment saved as `Enriched` and `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)`. NotFound, failed/duplicate-IMDb lookup, refused claim, null/empty/whitespace plot, and save failure before publish MUST NOT enqueue `FeaturesRequested` (brief Q5; ASSUMPTIONS).
- **FR-008**: Preserve the existing save-then-enqueue dual-write contract for publish/confirm failure and redelivery: loud failure, no compensating DB rollback, no in-handler retry loop, refused-claim redelivery acked as skipped without features republish; eventual recovery of Enriched movies lacking `movie_features` is out of scope (DEV-413/DEV-414) (brief Q9).
- **FR-009**: Keep `EnrichmentConsumer` registration in `LamuFlix.Api` via existing `RabbitMqServiceCollectionExtensions`. Do not target `src/LamuFlix.Worker`. Do not consume `features.requested` or generate embeddings (brief Q7; `recon-DEV-347:50,69`).
- **FR-010**: Cover Core eligibility and failure propagation in `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs` with observable call ordering; cover routing/registration where behavior changes in `EnrichmentRoutingTests.cs` / `RabbitMqServiceCollectionExtensionsTests.cs`; cover real-broker topology, publisher, and consumer paths in `RabbitMqTopologyTests.cs`, `RabbitMqPublisherTests.cs`, and `EnrichmentConsumerTests.cs`; update `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` only if reset/cleanup must enumerate the new queues (brief Files touched / Test strategy; `recon-DEV-347:75-88`).
- **FR-011**: Architecture tests continue to pass with the existing ports whitelist and dependency boundaries (`recon-DEV-347:87`). No new NuGet/npm dependency, project, top-level folder, architectural layer, database schema/migration, public HTTP/OpenAPI/web contract change, `Features:LocalPlay`, secrets, or `Process.Start` (brief care-list; `specs/PRODUCT.md` §5; `recon-DEV-347:95-100`).
- **FR-012**: Phase B gate bar follows brief Gate expectations and task-pipeline Phase 3 step 5: Roslyn, complexity, InspectCode (no `-All`), mutation for changed production C#, property tests (no opt-out granted by this brief), vulnerable packages, format verification, `dotnet test`; web gates report scope-empty if web is unchanged. Exit 1 and Could not run block; scope-empty exit 2 is `SKIPPED (scope-empty)`, never PASS; mutation `NOT APPLICABLE` is N/A, never PASS. Thresholds stay in `harness.yml` and are never lowered.
- **FR-013**: Delivery scope is the ticket Scope & Technical Design and AC plus Patron Q1–Q9 and care-list rulings: message, three features queues/bindings/arguments, routing support, existing publisher extension, and post-save enrichment-success publish. Scope statements describe that delivery boundary and do not preclude findings about failures to meet in-scope requirements (brief Frozen scope / Plan challenge editorial decisions).

### Key Entities

- **FeaturesRequested**: Internal messaging command carrying `MovieId` and `Attempt` that requests downstream feature extraction work after enrichment leaves a usable plot.
- **Features queues**: The `features.requested`, `features.retry`, and `features.dead-letter` quorum queues on `lamuflix.enrichment`.
- **Enrichment success publish**: The Core post-save enqueue of `FeaturesRequested` for an eligible Found enrichment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A successful enrichment with a non-empty persisted plot results in exactly one `FeaturesRequested` enqueue/publish for that movie with `Attempt = 1`.
- **SC-002**: A successful enrichment with an empty or whitespace-only plot results in zero `FeaturesRequested` publishes.
- **SC-003**: Topology declaration creates the three features quorum queues with dead-letter bindings/arguments, and every existing enrichment queue, routing key, argument, and `Decide(ProcessEnrichmentOutcome)` disposition remains unchanged.
- **SC-004**: Ineligible enrichment outcomes (NotFound, failed/duplicate lookup, refused claim, save failure) produce zero `FeaturesRequested` publishes.
- **SC-005**: Post-save publish/confirm failures are loud: Core does not swallow them; consumer settlement matches Q9; the movie row stays `Enriched` with no compensating rollback or in-handler retry.
- **SC-006**: No features consumer, embeddings, `movie_features` schema, new port interface, new dependency, HTTP/OpenAPI change, or legacy Worker edit ships in this ticket.
- **SC-007**: Phase B gates run with honest exit/verdict recording; SKIPPED/SKIP/N/A are never recorded as PASS.

## Assumptions

- Features queue and routing key names `features.requested`, `features.retry`, and `features.dead-letter` (ASSUMPTIONS.md; tagged `[assumed]`).
- Initial attempt number is `1` (ASSUMPTIONS.md; tagged `[assumed]`).
- Non-empty plot predicate is `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)` (ASSUMPTIONS.md; tagged `[assumed]`).
- Ticket wording "persisted `FullPlot`" maps to domain field `MovieMetadata.Synopsis` (brief Q5).
- PRODUCT Worker wording (`specs/PRODUCT.md:10`) does not move this feature into legacy `src/LamuFlix.Worker`; ticket item 4 and brief Q7 keep registration in Api (brief Frozen scope).
- Epic 9 / Recommendations execution remains deferred until after Epic 7 (`specs/PRODUCT.md:17`); this Phase A draft does not authorize Phase B Recommendations start.
- No owner structural checkbox is opened by this draft (brief Closing bar; CONCLUSIONS care-list).
- Exclusions in brief Frozen scope remain out of scope.
