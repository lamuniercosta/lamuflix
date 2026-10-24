# Feature Specification: DEV-414 FeaturesRequested consumer and movie_features persistence

**Feature Branch**: `feature/414-spec`

**Created**: 2026-10-23

**Status**: gate1: provisional

**Input**: Ticket DEV-414 (size L, parent DEV-288, no ui tag); settled grill brief `specs/DEV-414/brief.md` (grill closed Q1–Q9 including Q4 continuation; ten Patron answers); facts from notes `recon-DEV-414` and `recon-DEV-414-2` (corrected publisher evidence at recon-DEV-414-2:90-213 governs); Patron rulings in `specs/DEV-414/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md`. No decisions beyond the brief and those rulings. No `design.md` (no ui tag). Proposed ADR `docs/adr/0026-separate-feature-generation-and-recovery.md` (status: proposed) was drafted by Keel under Conductor Phase 2 step 5 and accompanies this spec PR; acceptance, plan-challenge clearance, and Gate 1 are not claimed.

## Clarifications

### Session 2026-10-23

L-ticket `/speckit-clarify` scan against brief Q1–Q9 (including Q4 continuation), CONCLUSIONS, and recon notes. No critical ambiguities remain that require a new Patron ruling. Material answers already encoded below from the grill (settled answers; Q1–Q3 not reopened):

- Q: Review closing bar and round cap → A: Critical and High block; Medium and Low require explicit disposition (fix in scope, follow-up ticket if charter criteria apply, or noted no ticket); maximum two review rounds; maximum two remediation commits per round; exit 1 / Could not run / unaccepted property-test exit 2 block; scope-empty SKIPPED, mutation NOT APPLICABLE, harness-disabled SKIP, and recorded property-test opt-out are non-blocking and never PASS (brief Q1).
- Q: Feature vs enrichment lifecycle → A: Feature generation is a separate post-enrichment lifecycle; every feature success/failure/retry/recovery leaves EnrichmentStatus and EnrichmentAttempts untouched; FeaturesRequested.Attempt counts feature transport attempts only; feature terminal state stays in messaging; no database feature status; Sweeper glossary Avoid of a second consumer does not prohibit the ticket-required features consumer (brief Q2).
- Q: Missing movie / blank plot / existing-row redelivery → A: Missing movie and null/whitespace plot are successful no-ops (ack; no generator; no upsert); nonblank plot generates one vector and upserts; existing rows do not skip generation; ON CONFLICT(movie_id) last-write-wins; no plot hash/version/model metadata (brief Q3).
- Q: Retry, classification, publication → A: Reuse EnrichmentOptions.MaxAttempts and RabbitMqOptions.RetryDelay plus existing features.requested/retry/dead-letter topology; honor EmbeddingGenerationException.Category first else EnrichmentFailureClassifier; only provider_unavailable, rate_limited, invalid_response, unknown in feature headers; never duplicate_imdb_id; ack after successful upsert/no-op or completed retry/DLQ publication; do not assert unverified broker-ack/nack/return or exactly-once durability; live-broker tests decide (brief Q4 + Q4 continuation).
- Q: Sweeper recovery and Attempt reset → A: On SweepInterval, anti-join movies with nonblank plot and no movie_features row; enqueue FeaturesRequested(MovieId, 1); preserve enrichment Pending-plus-lease pass; independent guarded passes; MaxAttempts bounds one chain; later sweep starts fresh Attempt 1 after prior DLQ; no persistent suppression (brief Q5).

Vocabulary, module signatures, activation/liveness, test ordering, and frozen scope are further encoded in Requirements and Edge Cases from brief Q6–Q9.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Persist plot embedding from FeaturesRequested (Priority: P1)

After metadata enrichment publishes FeaturesRequested, a dedicated features consumer loads the movie’s current plot, generates one embedding via the existing generator when the plot is nonblank, and upserts a single movie_features row keyed by MovieId. Missing movies and null/whitespace plots complete as successful no-ops without calling the generator or writing storage. Redelivery of the same MovieId leaves exactly one row (last write wins).

**Why this priority**: Ticket Scope lines 20–22 and AC line 17 require async persistence after enrichment with idempotent upsert on MovieId.

**Independent Test**: With fake embedding generator, deterministic TimeProvider, and real Testcontainers PostgreSQL (and broker where the delivery path is under test), deliver FeaturesRequested for a movie with a nonblank plot and assert one movie_features row with expected vector, ComputedAt, and unchanged EnrichmentStatus/EnrichmentAttempts; prove missing/null/whitespace no-ops; prove repeated/concurrent MovieId redelivery yields one row.

**Acceptance Scenarios**:

1. **Given** a movie with a nonblank plot and a FeaturesRequested delivery at Attempt ≥ 1, **When** the features handler runs, **Then** exactly one embedding is generated, MovieFeaturesSnapshot is constructed with ComputedAt from injected TimeProvider, existing snapshot/repository validation applies, and UpsertAsync persists one movie_features row for that MovieId.
2. **Given** a missing movie or a null/whitespace plot, **When** the features handler runs, **Then** the delivery is acknowledged as a successful no-op with no generator call and no upsert.
3. **Given** an existing movie_features row for the same MovieId, **When** FeaturesRequested is redelivered, **Then** generation is not skipped, ON CONFLICT(movie_id) overwrites vector and computed_at, and exactly one row remains.
4. **Given** any feature success, no-op, failure, retry, or recovery, **When** the movie aggregate is inspected, **Then** EnrichmentStatus and EnrichmentAttempts are unchanged.

---

### User Story 2 - Bounded feature retry and dead-letter (Priority: P1)

Transient feature failures republish Attempt+1 to features.retry while Attempt is below MaxAttempts. Permanent failures and exhausted attempts are published by the application to features.dead-letter with sanitized category/attempt/exhaustion headers and no raw exception text. Application sanitized category/attempt/exhaustion headers constrain only retry/DLQ messages the application itself publishes; a broker-generated dead-letter entry, if broker limits produce one, is outside that sanitized-header contract and carries no asserted application Attempt accounting. Existing features.requested / features.retry / features.dead-letter topology is retained. Shutdown cancellation requeues the original delivery unchanged with no Attempt increment. Failed or cancelled retry/DLQ publication never acks the original.

**Why this priority**: Ticket Scope line 22 and AC line 17 require retry/DLQ aligned with enrichment and dead-lettered failures.

**Independent Test**: Live RabbitMQ Testcontainers prove delayed Attempt+1 retry, permanent/exhausted application-published DLQ with safe headers only from the whitelist, malformed/Attempt&lt;1 → invalid_response DLQ, shutdown requeue without Attempt increment, repeated shutdown-requeue coverage recording actual queue residence, broker headers/body, and application Attempt (without prescribing infinite requested-queue residence or inventing observed broker behavior), failed publish without ack, and the existing unroutable PublishException expectation. Unit tests cover typed category precedence, invalid vector, retry/exhaustion boundaries, and requested cancellation.

**Acceptance Scenarios**:

1. **Given** a retryable feature failure with Attempt below EnrichmentOptions.MaxAttempts, **When** disposition runs, **Then** FeaturesRequested with Attempt+1 is published to features.retry using RabbitMqOptions.RetryDelay topology, and the original delivery is acked only after that publication completes.
2. **Given** a non-retryable failure or a failure at/above MaxAttempts, **When** disposition runs, **Then** the application publishes to features.dead-letter with sanitized headers (category, attempt, max_attempts_exhausted as applicable) and without raw exception text; those headers apply only to application-published retry/DLQ messages.
3. **Given** malformed payload, Attempt below 1, or an invalid generated vector, **When** disposition runs, **Then** the failure is permanent invalid_response and dead-letters without inference retry.
4. **Given** EmbeddingGenerationException with a Category, **When** classification runs, **Then** that Category is honored first; otherwise EnrichmentFailureClassifier applies; only provider_unavailable, rate_limited, invalid_response, and unknown appear in feature failure headers on application-published messages; duplicate_imdb_id is never emitted for feature processing.
5. **Given** requested host shutdown cancellation, **When** the consumer handles the in-flight delivery, **Then** the original is nacked with requeue and Attempt is not incremented; existing topology is unchanged.
6. **Given** a thrown or cancelled retry/DLQ publication, **When** disposition finishes, **Then** the original delivery is not acked and remains available for requeue.
7. **Given** repeated shutdown requeue of the same delivery under live broker, **When** residence and headers are recorded, **Then** the test records actual queue residence, broker headers/body, and application Attempt; it does not assert infinite requested-queue residence or that broker DLQ cannot occur; recorded broker behavior governs the expectation, and any required topology change returns to Patron.

---

### User Story 3 - Independent missing-features recovery (Priority: P1)

On the existing SweepInterval, an active feature recovery pass selects movies that have a nonblank plot and no movie_features row via a database-side anti-join, and enqueues FeaturesRequested(MovieId, 1) without embedding, claiming, or mutating enrichment state. The existing Pending-plus-lease enrichment recovery pass is preserved. The feature pass runs even when the enrichment pass finds no candidates. Failures in either pass do not suppress the other except on shutdown. A movie that previously reached the feature DLQ remains eligible on a later sweep, which starts a fresh Attempt 1 chain.

**Why this priority**: Ticket Scope line 23 and AC line 17 require StrandedMovieSweeper to re-enqueue stranded movies with plot but no movie_features row.

**Independent Test**: Real PostgreSQL/RabbitMQ integration proves missing-row/nonblank selection, no enrichment-status filter, existing-row exclusion, empty enrichment pass still runs feature pass, isolated pass failure, fresh Attempt 1 after terminal failure, and enqueue-failure eligibility on the next interval.

**Acceptance Scenarios**:

1. **Given** a movie with nonblank plot and no movie_features row, **When** the feature recovery pass runs, **Then** FeaturesRequested(MovieId, 1) is enqueued and EnrichmentStatus/EnrichmentAttempts are unchanged.
2. **Given** a movie that already has a movie_features row, or a blank/null plot, **When** the feature recovery pass runs, **Then** that movie is not selected.
3. **Given** the enrichment recovery pass finds zero candidates, **When** SweepInterval fires, **Then** the feature recovery pass still runs.
4. **Given** a failure in one recovery pass, **When** the other pass is still eligible, **Then** the other pass is not suppressed except on shutdown cancellation.
5. **Given** a movie whose prior feature chain reached DLQ, **When** a later feature recovery pass runs, **Then** a fresh FeaturesRequested(MovieId, 1) chain is started without draining prior DLQ entries.

---

### User Story 4 - Feature consumer activation, readiness, and end-to-end trace (Priority: P1)

The features consumer and its handlers register only when IMovieRepository, IMovieFeaturesRepository, IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt;, and the RabbitMQ queue publisher are registered, independent of IMetadataProvider. Incomplete hosts log inactive and never resolve missing dependencies. Active composition exposes a features-consumer readiness check on the existing readiness endpoint, separate from metadata worker liveness, and preserves producer → consumer → embedding/upsert tracing with structured MovieId/Attempt/category logs and safe no-input outcomes.

**Why this priority**: Ticket requires a BackgroundService consumer on features.requested (Scope line 20) with constitution VI observability; Q7 freezes activation and readiness.

**Independent Test**: Active/inactive host composition and readiness tests; E2E enrichment → FeaturesRequested → movie_features with fake embedding and real DB/broker, same trace, typed generator, timestamp/dimension, one row, unchanged metadata state.

**Acceptance Scenarios**:

1. **Given** all four feature prerequisites registered, **When** the Api host starts, **Then** FeaturesConsumer and feature processing/recovery handlers are registered and the features-consumer readiness check is present on the existing readiness endpoint.
2. **Given** an incomplete/minimal host missing a feature prerequisite, **When** the host starts, **Then** the feature consumer is logged inactive, missing dependencies are not resolved, and metadata consumer activation remains unchanged.
3. **Given** an active composition, **When** enrichment publishes FeaturesRequested and the features path completes, **Then** one movie_features row exists, EnrichmentStatus/EnrichmentAttempts are unchanged, and a single trace spans producer → consumer → embedding/upsert.
4. **Given** feature disconnect or failure, **When** readiness is evaluated, **Then** metadata worker liveness does not mask the feature consumer readiness signal.

### Edge Cases

- Canonical nonblank / no-input semantics for both the feature handler and the database anti-join eligibility predicate are .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics (brief Q3/Q5/Q6; Patron F1). Empty, spaces-only, tab/newline-only, and Unicode-whitespace-only plots are no-input / ineligible; any plot containing a non-whitespace character is nonblank / eligible. Whitespace-as-no-input remains an explicit feature-handler decision; no inference is made about the unverified OMDb IsMissing helper body, and no SQL TRIM/btrim equivalence is claimed (brief Q3; recon-DEV-414:68; Patron F1).
- Scalar `GetPlotAsync` returns the stored plot value unchanged; normalization is not moved into that projection (brief Q3/Q6; Patron F1).
- Existing feature rows never skip generation; repeat inference and computedAt overwrite are accepted costs (brief Q3).
- MaxAttempts bounds one transport chain, not lifetime attempts; persistent lifetime suppression is rejected (brief Q5).
- Enqueue failure during recovery leaves the row absent and eligible on a later sweep (brief Q5).
- Application sanitized category/attempt/exhaustion headers constrain only retry/DLQ messages the application itself publishes; broker-generated dead-letter entries (if broker limits produce them) are outside that header/accounting contract and carry no asserted application Attempt accounting. Existing topology and unchanged shutdown requeue (no Attempt increment) are retained; do not assert infinite requested-queue residence or invent observed broker behavior (brief Q4; Patron F3).
- Publication configures Persistent=true, publisher confirmations and tracking enabled, mandatory=true, awaited publish; evidence establishes configured options, not broker-ack/nack/return await guarantees or exactly-once durability (brief Q4 continuation; recon-DEV-414-2:142-148).
- Live-broker tests must cover retry/DLQ publication, failed publish without ack, the unroutable PublishException expectation, and repeated shutdown-requeue coverage recording actual queue residence, broker headers/body, and application Attempt; a failing test blocks rather than a documentation claim (brief Q4 continuation; Patron F3).
- RabbitMQ.Client 7.2.2 at Directory.Packages.props:10 governs; historical DEV-371 6.8.1 text is stale evidence only; no dependency change and no historical rewrite (brief Q4 continuation).
- No MarkFailed / metadata state machine use for feature failures (brief Q2, Q4).
- No new dependency, project, top-level folder, architectural layer, schema/migration, public HTTP contract, generic transport framework, second publisher, model/version/hash persistence, recommendation algorithm, UI, LocalPlay, secrets, or Process.Start (brief frozen scope; Q6–Q9).
- PRODUCT Epic 9 sequencing is not waived; Phase B entry still requires recorded sequencing evidence and Q8 step 1 drift/baseline (brief Q9; `specs/PRODUCT.md` §2).
- Proposed ADR `docs/adr/0026-separate-feature-generation-and-recovery.md` accompanies this spec PR (status: proposed); this specification does not claim ADR acceptance, plan-challenge clearance, or Gate 1 (brief Q9; Conductor Phase 2 step 5).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Infrastructure MUST host `FeaturesConsumer` as a `BackgroundService` consuming the existing `features.requested` topology (`FeaturesRequestedQueue` / features.retry / features.dead-letter already declared), following the EnrichmentConsumer pattern, with reconnect/shutdown using injected TimeProvider (ticket Scope line 20; brief Q6–Q7; recon-DEV-414 table rows for topology and EnrichmentConsumer).
- **FR-002**: Core MUST add `ProcessFeaturesCommand`, `ProcessFeaturesCommandHandler`, and feature-specific `ProcessFeaturesOutcome` (no metadata transition) under `src/LamuFlix.Core/Features/Enrichment/` (brief Q6).
- **FR-003**: Handler MUST load the current plot via `IMovieRepository.GetPlotAsync(MovieId, CancellationToken) -> Task<string?>` (missing movie and null plot share null). Infrastructure MUST implement the method in `EfMovieRepository` as a no-tracking scalar projection (brief Q3, Q6).
- **FR-004**: For nonblank plot under the canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics in FR-005, handler MUST generate one vector via existing `IEmbeddingGenerator<string, Embedding<float>>`, construct existing `MovieFeaturesSnapshot` with ComputedAt from injected TimeProvider, apply existing snapshot/repository validation, and upsert via `IMovieFeaturesRepository`. Existing rows MUST NOT skip generation. Upsert MUST rely on existing ON CONFLICT(movie_id) last-write-wins behavior (brief Q3; recon supplemental section 1; Patron F1).
- **FR-005**: Missing movie and null/whitespace plot MUST be successful no-ops: ack, no generator, no upsert. Handler no-input MUST use canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics: empty, spaces-only, tab/newline-only, and Unicode-whitespace-only plots are no-ops; any plot containing a non-whitespace character proceeds to generation (brief Q3; Patron F1).
- **FR-006**: Feature processing MUST leave EnrichmentStatus and EnrichmentAttempts untouched on every success, failure, retry, and recovery. FeaturesRequested.Attempt MUST count feature transport attempts only. No database feature status column or status table is authorized (brief Q2).
- **FR-007**: Retry/DLQ MUST reuse EnrichmentOptions.MaxAttempts, RabbitMqOptions.RetryDelay, and existing features.requested / features.retry / features.dead-letter topology. A new chain starts at Attempt 1. Retryable failure below MaxAttempts publishes Attempt+1 to features.retry. Non-retryable or at/above MaxAttempts is published by the application to features.dead-letter. Shutdown cancellation requeues unchanged with no Attempt increment. Existing broker topology (including any configured delivery-limit arguments) is retained without asserting infinite requested-queue residence or inventing broker-produced application headers (brief Q4; Patron F3).
- **FR-008**: Classification MUST honor EmbeddingGenerationException.Category first; otherwise reuse EnrichmentFailureClassifier (including exception-tree behavior). Invalid payload, Attempt below 1, and invalid generated vector MUST map to permanent invalid_response. Only provider_unavailable, rate_limited, invalid_response, and unknown MAY appear in feature failure headers on application-published retry/DLQ messages. Application sanitized category/attempt/exhaustion headers constrain only messages the application itself publishes; a broker-generated dead-letter entry, if broker limits produce one, is outside that sanitized-header contract and carries no asserted application Attempt accounting. Never emit duplicate_imdb_id for feature processing. Never put raw exception text in headers or stored metadata. Never call metadata MarkFailed for feature failures. Requested host cancellation bypasses classification (brief Q4 continuation; Patron F3).
- **FR-009**: Publication MUST reuse `RabbitMqEnrichmentQueuePublisher` / existing PublishCoreAsync with a typed FeaturesRequested retry/DLQ overload carrying routing key and sanitized headers for application-published messages. Persistent=true, publisher confirmations and tracking enabled, mandatory=true, awaited publish. Ack only after successful upsert/no-op or completed retry/DLQ publication. Thrown/cancelled publication MUST NOT ack. Spec MUST NOT assert unverified BasicPublishAsync broker-ack/nack/return guarantees or exactly-once durability. Any required topology change returns to Patron as a new decision (brief Q4 continuation; recon-DEV-414-2 section A/D; Patron F3).
- **FR-010**: Core MUST add `SweepMissingMovieFeaturesCommand` and `SweepMissingMovieFeaturesCommandHandler`. `IMovieFeaturesRepository` MUST gain `FindMovieIdsMissingFeaturesAsync(CancellationToken) -> Task<IReadOnlyList<MovieId>>` implemented in `EfMovieFeaturesRepository` as a database-side Movies/movie_features anti-join whose nonblank-plot eligibility MUST match the handler's canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics (empty, spaces-only, tab/newline-only, and Unicode-whitespace-only excluded; any non-whitespace character included), with no N+1 feature lookups. Do not claim SQL TRIM/btrim equivalence as fact. If exact parity cannot be expressed in PostgreSQL, return needs decision to Patron rather than substituting TRIM semantics. Existing upsert and schema MUST stay unchanged. Scalar `GetPlotAsync` remains a stored-value projection and MUST NOT absorb this normalization (brief Q5–Q6; Patron F1).
- **FR-011**: `StrandedMovieSweeper` MUST invoke enrichment and feature sweep handlers in independent guarded passes on existing SweepInterval. Feature pass MUST enqueue FeaturesRequested(MovieId, 1) only, MUST NOT embed/claim/mutate enrichment state, MUST run even when enrichment finds no candidates, and MUST NOT suppress or be suppressed by the other pass except on shutdown. Fresh Attempt 1 after prior DLQ is required; no DLQ drain; no persistent suppression (brief Q5–Q6).
- **FR-012**: Feature activation MUST use a feature-specific predicate requiring IMovieRepository, IMovieFeaturesRepository, IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt;, and the RabbitMQ queue publisher, independent of IMetadataProvider. Dependency registration MUST precede evaluation. Handlers and FeaturesConsumer register only when prerequisites exist. Incomplete hosts MUST log inactive and never resolve missing dependencies. Feature recovery pass runs only in active composition (brief Q7).
- **FR-013**: Infrastructure/RabbitMq MUST add `FeaturesRouting` and a concrete feature liveness state plus features-consumer readiness health check on the existing readiness endpoint, separate from metadata worker liveness. No new liveness interface, generic worker abstraction, route, status code, or DTO contract (brief Q6–Q7).
- **FR-014**: Tracing MUST preserve producer → consumer → embedding/upsert propagation. Structured feature handler/consumer/recovery logs MUST carry MovieId, Attempt, safe category, and no-input outcomes without raw exception text, and MUST exclude plot text and embedding vector contents (brief Q7; constitution VI; Patron F7 / Quill item 3).
- **FR-015**: Proof MUST follow brief Q8 ordering and matrix: fake embedding generator and deterministic TimeProvider; unit tests may double Core ports with NSubstitute; real query translation, serialization, upsert, and broker behavior on Testcontainers PostgreSQL/RabbitMQ; never mock drivers or Infrastructure; no external Ollama download. Cover success, no-ops, category precedence, invalid vector, retry/exhaustion, cancellation, metadata isolation, redelivery uniqueness, application-published retry/DLQ/safe headers, malformed/Attempt&lt;1, shutdown requeue without Attempt increment, live-broker repeated shutdown-requeue recording queue residence/broker headers/body/application Attempt, failed publish without ack, unroutable expectation, independent recovery passes, active/inactive composition/readiness, real-PostgreSQL handler/anti-join whitespace parity corpus, and E2E enrichment → FeaturesRequested → movie_features with same trace.
- **FR-016**: Root `CONTEXT.md` MUST receive additive vocabulary entries for Feature generation, Feature transport attempt, and Feature recovery using the brief Q9 definitions verbatim, retaining every existing definition. A discovered glossary conflict returns needs decision to Patron; never silently rewrite (brief Q9; constitution VIII).
- **FR-017**: The system MUST NOT add a new NuGet/npm dependency, project, top-level folder, architectural layer, schema/migration, public HTTP/OpenAPI/web contract, generic transport framework, second publisher, model/version/hash persistence, lifetime retry suppression, recommendation algorithm, UI, LocalPlay execution, secrets, Process.Start work, or historical DEV-371 rewrite. Owner structural checkboxes: none (brief frozen scope; Q6–Q9).

### Key Entities

- **FeaturesRequested**: Existing queue message `(MovieId, Attempt)` requesting feature generation for one movie; Attempt is the feature transport attempt ordinal.
- **Feature generation**: Deriving a movie plot embedding for that movie (brief Q9 vocabulary).
- **Feature transport attempt**: One delivery attempt in a feature-generation retry chain, restarted at 1 by recovery (brief Q9 vocabulary).
- **Feature recovery**: Identifying movies with a nonblank plot and no stored features and requesting feature generation without claiming metadata enrichment (brief Q9 vocabulary).
- **Movie features snapshot / movie_features row**: Existing DEV-413 storage of one vector and ComputedAt per MovieId; upsert is last-write-wins on movie_id.
- **ProcessFeaturesOutcome**: Feature-specific handler result with no metadata EnrichmentStatus transition.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After enrichment publishes FeaturesRequested for a movie with a nonblank plot, exactly one movie_features row exists for that MovieId with a validated embedding vector and a ComputedAt from the injected clock, and EnrichmentStatus/EnrichmentAttempts are unchanged.
- **SC-002**: Redelivering FeaturesRequested for the same MovieId (including concurrent deliveries) leaves exactly one movie_features row.
- **SC-003**: Missing movie and null/whitespace plot deliveries (canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics) complete as acknowledged no-ops with zero generator calls and zero upserts.
- **SC-004**: Retryable failures below MaxAttempts appear on features.retry at Attempt+1 as application-published messages; permanent or exhausted failures appear on features.dead-letter as application-published messages with only whitelisted sanitized headers; those headers do not constrain broker-generated dead-letter entries; failed publication never acks the original; live-broker repeated shutdown-requeue coverage records actual queue residence, broker headers/body, and application Attempt without asserting infinite residence.
- **SC-005**: On SweepInterval, movies with handler-nonblank plot (same canonical whitespace semantics) and no movie_features row are enqueued at Attempt 1; movies that previously dead-lettered remain eligible for a fresh Attempt 1 chain; enrichment Pending-plus-lease recovery remains unchanged and independently guarded.
- **SC-006**: Active Api composition registers FeaturesConsumer and features-consumer readiness without masking metadata liveness; incomplete hosts log feature consumer inactive; E2E enrichment → FeaturesRequested → movie_features shares one trace.
- **SC-007**: Configured backend verification gates run without a lowered threshold and without mutation `--since`. Unchanged `/web` is scope-empty `SKIPPED`, never PASS. Q1 non-blocking verdicts are preserved verbatim.

## Assumptions

- No `[assumed]` taste items were recorded for this ticket; there is no `ASSUMPTIONS.md`.
- No owner structural checkbox is open (`blocked: structural = none`). Anything not decided in `brief.md` / `CONCLUSIONS.md` is a `needs decision:` item, not an assumption invented here.
- Proposed ADR `docs/adr/0026-separate-feature-generation-and-recovery.md` (status: proposed) was drafted by Keel under Conductor Phase 2 step 5 and accompanies this spec PR; Quill does not invent ADR body text or claim ADR acceptance, plan-challenge clearance, or Gate 1.
- Facts for existing symbols, topology, publisher options, classifier codes, and sweeper seams come from `recon-DEV-414` and `recon-DEV-414-2`; proposed new methods/files are approved design decisions in the brief, not claims that those seams already exist.
