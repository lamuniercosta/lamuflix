# DEV-414 — Frozen Phase A brief

Status: grill closed by Patron Q9; frozen for Quill drafting.
Size/path: L; no UI.
Worktree: F:/Dev/LamuFlix.worktrees/feature-414-spec
Branch: feature/414-spec
Phase A base and verified HEAD: 6bd2d427d09878af852e634cb50599f6adce4af2

## Authority and evidence

Ticket: DEV-414 task note, Scope & Technical Design lines 20-24 and AC line 17.
Read with specs/PRODUCT.md, lamuflix-team-charter, task-pipeline Phase 2.
Facts: recon-DEV-414-2, especially corrected lines 90-213, and the earlier recon it incorporates.
Complete question/answer exchanges, cited rationale, and implications: CONCLUSIONS.md.
Every Patron answer is also reproduced verbatim in the appendix below.

The corrected publisher evidence supersedes earlier incomplete/no-confirm reports.
Current CPM RabbitMQ.Client is 7.2.2 (Directory.Packages.props:10); historical DEV-371
6.8.1 text is stale evidence, not a dependency instruction. No historical rewrite is in scope.
Proposed new methods, files, activation rules and query behavior below are approved decisions;
they are not claims that those seams already exist.

No structural blocker, remaining open question, or owner checkbox was ruled.
No ticket-text change or constitution departure was proposed. No taste assumption was used.
ADR creation is deferred by the Conductor and is not completed by this brief.
Phase A freeze authorizes no Phase B execution and does not waive PRODUCT sequencing.

## Frozen scope and loop discipline (Q1, Q9)

Deliver the features.requested consumer; plot embedding via the existing generator and
MovieId upsert; bounded per-chain retry/DLQ within the corrected publication limits;
independent missing-features recovery with deliberate per-sweep Attempt 1 reset;
minimal existing-port/EF/publisher/composition extensions; feature liveness and tracing;
and the tests and gate ordering below.

No new dependency, project, top-level folder, architectural layer, schema/migration,
or public HTTP contract. No generic transport framework, second publisher, model/version/hash
persistence, lifetime retry suppression, recommendation algorithm, UI, LocalPlay execution,
secrets, Process.Start work, or historical DEV-371 rewrite.

Frozen scope ends with this rule: anything else is a follow-up issue, not a finding in this round.

Closing bar: Critical and High block. Medium and Low require explicit disposition:
fix in scope, follow-up ticket if the charter criteria apply, or "noted, no ticket".
Follow-up tickets are only for an out-of-scope Critical/High finding, broken behavior,
or something the user requested; do not force scope expansion for a cosmetic improvement.
Maximum two review rounds; third-round findings go to user review with the PR.
Maximum two remediation commits per round.

Gate exit 1, Could not run, and unaccepted property-test exit 2 block.
Preserve exact non-blocking verdicts: scope-empty SKIPPED, mutation NOT APPLICABLE,
harness-disabled SKIP, and recorded property-test opt-out. Never relabel them PASS.
Use live harness/pipeline settings; never lower thresholds or use mutation --since.

## Lifecycle and handler behavior (Q2, Q3)

Feature generation is a separate post-enrichment lifecycle.
Every feature success, failure, retry and recovery leaves metadata EnrichmentStatus and
EnrichmentAttempts untouched. FeaturesRequested.Attempt counts feature transport attempts.
Feature terminal state stays in messaging; add no database feature status.
The glossary warning against a second consumer constrains the Sweeper from becoming a
claiming enrichment consumer; it does not prohibit the ticket-required features consumer.

Handler loads the current plot through IMovieRepository.
Missing movie and null/whitespace plot are successful no-ops: ack, no generator and no upsert.
Whitespace handling is an explicit feature-handler decision; no inference is made about
the unverified OMDb IsMissing helper body.

For nonblank plot, generate one vector using the existing
IEmbeddingGenerator<string, Embedding<float>>, construct the existing MovieFeaturesSnapshot,
apply the existing snapshot/repository validation, and upsert via IMovieFeaturesRepository.
ComputedAt comes from injected TimeProvider.

Existing feature rows do not skip generation. Existing ON CONFLICT(movie_id) overwrites
vector and computed_at. Duplicate/concurrent redelivery therefore produces one MovieId row;
repeat inference, timestamp overwrite, and last-write-wins concurrency are accepted costs.
No plot hash, version, or model metadata is added.

## Retry, failure classification and publication (Q4 and continuation)

Reuse EnrichmentOptions.MaxAttempts and RabbitMqOptions.RetryDelay.
Use existing features.requested, features.retry, features.dead-letter topology.
A new chain starts at Attempt 1. Retryable failure below MaxAttempts publishes Attempt+1
to features.retry. A non-retryable failure or failure at/above MaxAttempts goes to DLQ.
Invalid payload, Attempt below 1, and invalid generated vector are permanent invalid_response.
Shutdown cancellation requeues the original unchanged, with no increment.

Honor EmbeddingGenerationException.Category first.
Otherwise reuse EnrichmentFailureClassifier as coded, including its exception-tree behavior:

| Condition | Feature failure code | Retryable |
| --- | --- | --- |
| HTTP 429 | rate_limited | yes |
| HTTP null status, 408, 5xx, timeout, socket, IO | provider_unavailable | yes |
| JSON or other HTTP 4xx | invalid_response | no |
| Unmatched exception | unknown | yes |

Only provider_unavailable, rate_limited, invalid_response, unknown are permitted in feature
failure headers. Never emit duplicate_imdb_id for feature processing.
Requested host cancellation bypasses classification. Reuse classifier behavior for
persistence as an approved design decision, not as a claim that the existing adapter maps it.
Typed embedding adapter category takes precedence, including its validation and circuit/timeout
mapping. Use sanitized category/attempt/exhaustion-reason headers, never raw exception text
in message headers or stored metadata. Never call metadata MarkFailed for feature failures.

Reuse RabbitMqEnrichmentQueuePublisher and its existing PublishCoreAsync:
Persistent=true, publisher confirmations and tracking enabled, mandatory=true, awaited publish.
Ack only after successful upsert/no-op or completed retry/DLQ publication.
A thrown/cancelled publication never permits ack; preserve/requeue the original delivery.
Shutdown follows the existing consumer nack/requeue pattern.

Evidence establishes the configured options, not broker-ack/nack/return await guarantees.
Do not assert exactly-once durability or unverified RabbitMQ.Client library semantics.
No application return handler or dedicated nack/ack-await test was found.
The existing unroutable PublishException expectation was read, not run.
Live-broker tests must cover retry/DLQ publication, failed publish without ack, and the
unroutable expectation; failure blocks rather than being replaced with a documentation claim.

## Recovery and sweeper isolation (Q5)

On the existing SweepInterval, the active feature recovery pass uses a new database-side
anti-join: movies with non-null/non-whitespace plot and no movie_features row.
No metadata status predicate, feature lease, batch setting, status column or queue inspection.
Enqueue FeaturesRequested(MovieId, 1). Do not embed, claim, or mutate metadata state.

Preserve the existing Pending-plus-lease enrichment recovery semantics unchanged.
The feature pass runs even if the enrichment pass finds no candidates.
Each pass is independently guarded: a failure in either does not suppress the other,
except shutdown cancellation.
Feature enqueue failure leaves the row absent and eligible on a later sweep.

Duplicate and in-flight recovery are tolerated by MovieId upsert.
MaxAttempts bounds one transport chain, not lifetime attempts. A movie that reached DLQ
remains eligible on a later sweep, which starts a fresh Attempt 1 chain.
Do not drain/remove its prior DLQ entries. Persistent suppression is explicitly rejected.
Accepted cost: a persistently failing movie is re-enqueued every SweepInterval indefinitely.

## Modules, signatures and approved file scope (Q6, Q7, Q8)

New Core types in src/LamuFlix.Core/Features/Enrichment/:

- ProcessFeaturesCommand.cs
- ProcessFeaturesCommandHandler.cs
- ProcessFeaturesOutcome.cs (feature-specific outcome; no metadata transition)
- SweepMissingMovieFeaturesCommand.cs
- SweepMissingMovieFeaturesCommandHandler.cs

Extend existing src/LamuFlix.Core/Ports/IMovieRepository.cs:
GetPlotAsync(MovieId, CancellationToken) -> Task<string?>.
Implement in src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs as no-tracking
scalar projection. Missing movie and null plot intentionally share the null result.

Extend existing src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs:
FindMovieIdsMissingFeaturesAsync(CancellationToken) -> Task<IReadOnlyList<MovieId>>.
Implement in src/LamuFlix.Infrastructure/Persistence/EfMovieFeaturesRepository.cs as the
database-side Movies/movie_features anti-join with nonblank-plot predicate.
No N+1 feature lookups. Existing upsert and schema stay unchanged.

Infrastructure/RabbitMq gains FeaturesConsumer.cs and FeaturesRouting.cs following the
existing EnrichmentConsumer/routing pattern, plus a concrete feature liveness state and
feature consumer health check. No new liveness interface or generic worker abstraction.
Extend RabbitMqEnrichmentQueuePublisher.cs with a typed FeaturesRequested retry/DLQ overload
carrying routing key and sanitized headers through existing PublishCoreAsync.

Modify StrandedMovieSweeper.cs to invoke the two sweep handlers in independent guarded passes.
Modify RabbitMqServiceCollectionExtensions.cs and Api Program.cs composition as needed for the
approved registrations and ordering, with related existing health-registration tests.
Existing tests/fakes/doubles affected by port additions may be updated; unrelated failures
are reported, not silently fixed.

These non-ticket-named existing-file edits have deliberate Patron item-6 authorization,
citing the consumer/handler/retry/sweeper AC that forces them.
Core remains behind existing ports, with explicit handlers; no MediatR/repository framework.
No production code or tests are written by this Spec ask.

## Activation, readiness and tracing (Q7)

Use a feature-specific active predicate requiring IMovieRepository,
IMovieFeaturesRepository, IEmbeddingGenerator<string, Embedding<float>>,
and the RabbitMQ queue publisher. It is independent of IMetadataProvider.
Dependency registration precedes evaluation. Register feature processing/recovery handlers
and FeaturesConsumer only when prerequisites exist.

Incomplete/minimal hosts log feature consumer inactive and never resolve missing dependencies.
The feature recovery pass runs only in the active composition; absence is logged.
Metadata consumer activation and the existing enrichment recovery pass stay unchanged.
The full active Api composition delivers the ticket behavior.

Separate feature liveness from metadata worker liveness. Register readiness health check
features-consumer on the existing readiness endpoint, with no new route/status/DTO contract.
Metadata health must not mask feature disconnect/failure.
Follow existing consumer reconnect/shutdown pattern; inject TimeProvider for delays/time.
Preserve producer -> consumer -> embedding/upsert trace propagation.
Structured logs carry MovieId, Attempt, safe category and no-input outcomes.

## Test strategy and task ordering (Q8)

Use the existing fake embedding generator and deterministic TimeProvider.
Unit tests may double Core ports using repository conventions/NSubstitute.
Real query translation, serialization, upsert, and broker behavior require Testcontainers
PostgreSQL/RabbitMQ; never mock drivers or substitute mocked Infrastructure.
No external Ollama download or live model/network requirement.

Order the Build tasks into these dependency phases:

1. Wisp pickup drift/contracts check and Gauge baseline in the Build workspace.
2. Port extensions, scalar plot read, anti-join and real-database query tests.
3. Feature handler/outcome and unit tests: success, missing/null/whitespace no-op,
   typed category precedence, invalid vector, retry/exhaustion boundaries, requested
   cancellation, deterministic timestamps, and metadata-state isolation.
4. Consumer/routing/publisher overload/activation/liveness and live RabbitMQ tests:
   persisted delivery, repeated/concurrent MovieId redelivery with one row,
   delayed Attempt+1 retry, permanent/exhausted DLQ with safe headers,
   malformed/Attempt<1, shutdown requeue, failed publish without ack, unroutable expectation.
5. Independent recovery passes and real DB/broker integration: missing-row/nonblank selection,
   no enrichment-status filter, existing-row exclusion, empty enrichment pass,
   isolated pass failure, fresh Attempt1 after terminal failure, enqueue failure eligibility.
6. Active/inactive host composition/readiness and E2E enrichment -> FeaturesRequested ->
   movie_features with fake embedding and real DB/broker; same trace, typed generator,
   timestamp/dimension, one row, unchanged metadata state.

Anvil implements one production phase per ask and commits/reports before the next.
Cog refactors once after all build phases. Gauge runs applicable gates after refactor;
three-axis review follows. No Spec workspace build/test/gate execution is claimed.
Use applicable Roslyn, complexity, InspectCode, full tests, property tests,
vulnerable-packages, format and mutation under live harness/pipeline configuration.
Never mutation --since and never lower a threshold. Q1 verdict handling applies.

## Vocabulary and downstream handoff (Q9)

Approved canonical definitions:

- Feature generation: deriving a movie plot embedding for that movie.
- Feature transport attempt: one delivery attempt in a feature-generation retry chain,
  restarted at 1 by recovery.
- Feature recovery: identifying movies with a nonblank plot and no stored features and
  requesting feature generation without claiming metadata enrichment.

These are deliberate definitions, not a claim that recon proved their absence in CONTEXT.
Patron authorizes additive CONTEXT.md entries in the same PR under constitution VIII/item 6,
retaining every existing definition verbatim. An authorized downstream writer must carry
these definitions into root CONTEXT.md. A discovered glossary conflict returns
needs decision to Patron; never silently rewrite. This Keel ask writes only feature docs.

Quill next drafts spec/clarify/plan/tasks/checklist from this frozen brief:
set PYTHONUTF8=1 for Spec Kit; apply the L-path clarify and checklist workflow after
spec creation, using settled answers without reopening Q1-Q3 or filling gaps by guess.
Checklist evaluates requirement completeness/clarity/consistency/measurability and
primary/exception/recovery coverage, with traceability to requirements.
A gap is needs decision to Keel. Missing facts are needs recon to the Conductor.
Keel then runs read-only speckit-analyze and compares plan/tasks against this brief.
ADR creation remains deferred; do not claim the later L-path ADR or plan challenge complete.
No Gate 1, review clearance, implementation readiness, or merge approval is implied.

## Appendix — Every Patron answer, verbatim

The full preceding question exchange for each answer is preserved append-only in CONCLUSIONS.md.
---
### Q1

[from Patron] DEV-414 Q1 ruling: APPROVED with gate clarification - closing bar: Critical and High block; Medium and Low require explicit disposition before close; maximum two review rounds.
- Basis (round cap): task-pipeline section 2.2 - Review cap: 2 rounds maximum, third-round findings go to user review with PR; Remediation cap: 2 fix commits per round.
- Basis (gate bar): task-pipeline Phase 5 step 6 and Phase 3 step 5 - exit 1 and Could not run never meet the bar; exit 2 scope-empty SKIPPED, mutation NOT APPLICABLE, harness-disabled SKIP, and recorded property-test opt-out are non-blocking, reported verbatim, never PASS (standing owner ruling 2026-10-04 overrides any brief/CONCLUSIONS wording to the contrary). So all applicable gate failures block means exit 1 / Could not run / unaccepted property-test exit 2.
- Basis (disposition): lamuflix-team-charter section 2.3 Patron decides/Rigger records - a follow-up ticket only for an out-of-scope finding that is Critical or High, is broken behaviour, or the user asked for; anything else is one line in CONCLUSIONS.md: noted, no ticket. Medium/Low disposition is therefore fix-in-scope, follow-up ticket if that criterion is met, or noted, no ticket - never silent drop, never forced scope expansion for a no-correctness-impact improvement.

---
### Q2

[from Patron] DEV-414 Q2 ruling: YES - feature generation is a separate post-enrichment lifecycle; EnrichmentStatus and EnrichmentAttempts are preserved untouched on every feature success, failure, and retry, and FeaturesRequested.Attempt counts only feature transport attempts. No constitution departure and no ticket-text change is required.
- Ticket basis: DEV-414 Scope lines 20-24 require a separate BackgroundService consumer on features.requested, a handler via IEmbeddingGenerator and IMovieFeaturesRepository, retry/dead-letter aligned with enrichment, a StrandedMovieSweeper extension, and idempotent upsert on MovieId; specs/PRODUCT.md section 3 makes ticket Scope authoritative, and charter section 2.3 line 28 says ticket-named text is decided, not a checkbox.
- State basis: recon-DEV-414 section 6 cites constitution IV (202-227) limiting the atomic claim/MarkFailed state machine to enrichment, and CONTEXT.md 46-52 distinguishing Enrichment Attempt (EnrichmentAttempts, per successful claim) from Transport Retry Attempt (restarts on manual retry/requeue); recon section 1 shows FeaturesRequested(MovieId, Attempt) published with Attempt 1 at ProcessEnrichmentCommandHandler.cs:102, so its Attempt is the feature transport counter only. Feature DLQ state therefore stays in messaging (features.retry / features.dead-letter topology, recon sections 1 and supplemental 3), and no new database feature status is authorised - that would be a section 2.3 item 3 schema change beyond the existing movie_features table (recon supplemental section 1).
- Glossary basis: CONTEXT.md 62-64 Avoid: a second consumer sits in the Sweeper term, read with 54-55 (Stranded Movie; sweeper re-enqueues without claiming): it bars the sweeper becoming a second claiming consumer of enrichment, not the ticket-required consumer on the distinct features.requested queue. Ruled explicitly: the feature consumer is not prohibited by that glossary line, and the sweeper remains non-claiming.

---
### Q3

[from Patron] DEV-414 Q3 ruling: APPROVED as recommended, no blocker - missing movie and null/whitespace plot are successful no-ops (ack; no generator call, no upsert); existing-row redelivery does not skip generation. No constitution departure and no ticket-text change required.
- No-input basis: recon-DEV-414 supplemental section 2 - plot maps absent/missing to null (OmdbMetadataProvider.Mapping.cs:52) and MovieRecord.Plot is nullable (MovieRecord.cs:51; EfMovieRepository.cs:132,144); supplemental section 1 - movie_features FKs to MovieRecord OnDelete Cascade (MovieFeaturesConfiguration.cs:31-34), so a missing movie has no upsert target. These are defined non-failures, not DLQ failures under DEV-414 AC line 17, since retry cannot create the plot or the FK target. Whitespace-as-no-input is ruled as handler behaviour here, expressly not inferred from the unread IsMissing body (recon supplemental section 2, line 66).
- Generation basis: nonblank plot generates one vector and upserts via MovieFeaturesSnapshot/IMovieFeaturesRepository, letting existing repository validation apply (EfMovieFeaturesRepository.cs:40-41 ValidateVector, recon supplemental section 1); computedAt comes from injected TimeProvider (constitution VII, recon section 6 line 108; Snapshot ctor takes computedAt, MovieFeaturesSnapshot.cs:9).
- Redelivery basis: EfMovieFeaturesRepository.cs:45-53 is INSERT ... ON CONFLICT (movie_id) DO UPDATE vector/computed_at - last-write-wins, no duplicate row (recon supplemental section 1 lines 54-56), satisfying ticket Scope line 24 and AC line 17. Existing rows therefore do not skip generation; no plot hash or model/version schema is authorised (section 2.3 item 3, beyond existing movie_features). Accepted cost: repeat inference and computedAt overwrite on duplicate delivery, with existing last-write-wins concurrency semantics.

---
### Q4

[from Patron] DEV-414 Q4 ruling: APPROVED as the disposition framework, with two items expressly not frozen pending recon below - reuse EnrichmentOptions.MaxAttempts, RabbitMqOptions.RetryDelay, and the existing features.requested / features.retry / features.dead-letter topology; no new options, queues, or DB state.
- Attempt/retry basis: FeaturesRequested is published at Attempt 1 (ProcessEnrichmentCommandHandler.cs:102, recon section 1); EnrichmentOptions.MaxAttempts = 3 (EnrichmentOptions.cs:13) and RabbitMqTopology.cs:91-92 derives retry delay from RabbitMqOptions.RetryDelay, with features retry/dead-letter already declared (RabbitMqTopology.cs:119-145; recon supplemental section 3). A retryable failure with Attempt below MaxAttempts republishes Attempt+1 to features.retry, mirroring EnrichmentRetryPolicy.cs:8-22 next-attempt semantics; at or above MaxAttempts it dead-letters with sanitised category/attempt/max_attempts_exhausted headers (EnrichmentConsumer.cs:262-277 pattern).
- Permanent vs retryable basis: invalid payload/attempt and invalid generated vector dead-letter without inference retry - retry cannot repair them, and repository validation already rejects a default vector (EfMovieFeaturesRepository.cs:56-60 ValidateVector, recon supplemental section 1). Transient embedding/persistence failure is retryable within the Attempt bound. Shutdown cancellation nacks requeue:true with no Attempt increment (EnrichmentConsumer.cs:211-217 pattern). Never map feature errors onto metadata MarkFailed (Q2 ruling; constitution IV state machine is enrichment-only, recon section 6).
- Disposition/tracing basis: ack only after successful upsert/no-op or after the retry/DLQ publication completes; on publication failure the original delivery is never acked and is preserved for requeue (EnrichmentConsumer.cs:283 BasicAck-after-publish pattern). Preserve tracing and structured MovieId logs (constitution VI, recon section 6 lines 107); no raw exception text in message headers or stored metadata (constitution IV :222-223, recon section 6 line 106). Accepted cost: bounded retries may repeat inference; terminal feature failure stays messaging-only.
needs recon before freezing (do not freeze these two in spec/brief until answered):
1. Exact category mapping: recon supplemental section 3 evidences only RateLimited (EnrichmentFailureCategory.cs:11,18,47; other categories expressly not reread) and one classifier return (EnrichmentFailureClassifier.cs:47). Which existing categories, if any, semantically fit embedding/persistence failures, and what sanitised category values may appear in feature failure headers?
2. Publication semantics: recon finds no ConfirmSelect/WaitForConfirms and states the publisher channel configuration is unverified (recon supplemental section 3 line 81; Not found lines 130-131). What does RabbitMqEnrichmentQueuePublisher / channel setup actually guarantee on awaited publish, so the spec never asserts broker-confirm durability unverified?

---
### Q4-continuation

[from Patron] DEV-414 Q4 continuation ruling: APPROVED as proposed - this resolves the Q4 hold with no additional recon essential, no ticket-text change, and no constitution departure.
- Classification basis: recon-DEV-414-2 section G - OllamaSharpEmbeddingGenerator.cs:102-107 rethrows EmbeddingGenerationException carrying failure.Category (mapped at :266-280: 429 rate_limited; null/408/5xx/timeout/circuit provider_unavailable; other/invalid payload invalid_response), so honour Category first. Otherwise reuse EnrichmentFailureClassifier as coded (section F lines 179-191: 429 rate_limited retryable; 408/5xx/timeout/Socket/IO provider_unavailable retryable; JSON/other 4xx invalid_response permanent; unmatched unknown retryable); section H line 206 shows persistence mapping is not established in code, so classifier reuse for persistence is this ruling as a decision, not a recon claim. Host cancellation bypasses classification and requeues unchanged (classifier MapCancellation rethrows on requested token, section F line 188; adapter cancelled rethrow, section G :98-101).
- Header whitelist basis: EnrichmentFailureCategory codes at section E lines 152-157 with IsRetryable at :43-51; only provider_unavailable, rate_limited, invalid_response, unknown are permitted in feature headers - duplicate_imdb_id is excluded because it has no classifier path and is produced only by the enrichment handler DuplicateImdbIdException catch (section F line 191), never by feature processing. Malformed payload, Attempt below 1, and invalid generated vector map to invalid_response permanent (IsRetryable false, section E line 163; vector validation invalid_response, section G line 200), no raw exception text in headers.
- Publication basis and limit: reuse RabbitMqEnrichmentQueuePublisher as read (section A lines 103-124: Persistent true, publisherConfirmationsEnabled and tracking enabled, mandatory true, awaited BasicPublishAsync, no local exception mapping); await publication and ack afterward, and a thrown/cancelled publication never permits ack - the original delivery is requeued (Q4 framework). Explicitly do not assert awaited BasicPublishAsync broker-ack/nack/return guarantees or exactly-once durability: recon section D lines 142-148 preserves that uncertainty, the unroutable PublishException test at RabbitMqPublisherTests.cs:138-147 was not executed, and no BasicReturn handler exists in src (line 143). Live-broker tests must cover successful retry/DLQ publication, publish failure without ack, and the existing unroutable expectation; a failing test blocks rather than any documentation claim substituting for it.
- Version basis: Directory.Packages.props:10 RabbitMQ.Client 7.2.2 governs at HEAD (section C lines 135-138); DEV-371 6.8.1 text is stale evidence only. No dependency change (section 2.3 item 1 not triggered) and no historical-spec rewrite (section 2.3 item 6 not triggered).

---
### Q5

[from Patron] DEV-414 Q5 ruling: APPROVED exactly as proposed, including the deliberate retry reset - persistent suppression is NOT required and is not authorised. No ticket-text change and no constitution departure.
- Recovery basis: DEV-414 Scope line 23 names the StrandedMovieSweeper extension and AC line 17 requires the sweeper to re-enqueue movies with plot but no movie_features row. Recon-DEV-414 supplemental section 4 evidences the existing cadence (StrandedMovieSweeper.cs:28-42 running on EnrichmentOptions.SweepInterval), the unchanged Pending-plus-lease enrichment candidate query (EfMovieRepository.cs:495-504), and expressly that no missing-movie_features seam exists (line 92) - so the database-side anti-join (non-null/non-whitespace plot, no movie_features row, enqueue FeaturesRequested(MovieId, 1)) is a NEW seam to implement, never claimed as existing. No metadata status predicate, lease, batch option, status column, or queue inspection is added; recon found no batch control (section 4 line 91) and none is invented here.
- Separation basis: Q2 settled that feature generation preserves EnrichmentStatus/EnrichmentAttempts and the sweeper never claims; the feature pass therefore never embeds, claims, or changes enrichment state, and the existing Pending-enrichment pass is preserved unchanged (constitution IV sweeper duty, recon-DEV-414 section 6 line 106; CONTEXT.md 54-55 sweeper re-enqueues without claiming). The feature pass must still run when the enrichment pass finds zero candidates, and a failure in either pass must not suppress the other except on shutdown - both passes are independent obligations under the same AC.
- Retry-reset basis, ruled deliberately: CONTEXT.md 50-52 defines Transport Retry Attempt as restarting on manual retry/requeue (recon-DEV-414 section 6 line 116), and Q4 settled MaxAttempts as the bound for one transport chain starting at Attempt 1. A terminally DLQ-failing movie therefore remains eligible on a later sweep, which starts a fresh Attempt 1 chain and does not drain or remove earlier DLQ entries; duplicates and in-flight recovery are tolerated by the Q3 MovieId upsert. Persistent suppression would need a new status/schema decision under section 2.3 item 3 beyond the ticket tables and is rejected here; the ticket AC contains no lifetime attempt cap. Accepted cost: a persistently failing movie is re-enqueued every SweepInterval indefinitely. Enqueue failure creates no row, so the movie stays eligible next sweep (publisher failures propagate uncaught, recon-DEV-414-2 section 4 lines 48-59).

---
### Q6

[from Patron] DEV-414 Q6 ruling: APPROVED and expressly authorised under section 2.3 item 6, as existing-layer changes only. The named signatures and filenames are approved as design proposals for the spec, not represented as discovered facts.
- Scope basis: DEV-414 Scope lines 20-23 and AC line 17 require the features.requested consumer/handler, retry/DLQ, and StrandedMovieSweeper extension. The non-ticket-named existing-file edits this forces are authorised here: IMovieRepository.GetPlotAsync and EfMovieRepository (plot load collapsing missing movie and null plot per Q3; no-tracking scalar projection), IMovieFeaturesRepository.FindMovieIdsMissingFeaturesAsync and EfMovieFeaturesRepository (database-side anti-join on Movies/movie_features with nonblank-plot predicate per Q5; no N+1, no driver mocks - constitution IX, recon-DEV-414 section 6 line 110), RabbitMqEnrichmentQueuePublisher typed FeaturesRequested retry/DLQ overload through existing PublishCoreAsync (publisher shape evidenced at recon-DEV-414-2 section 4 lines 48-59), FeaturesConsumer/FeaturesRouting in Infrastructure/RabbitMq following EnrichmentConsumer pattern (recon-DEV-414 section 1), and the RabbitMqServiceCollectionExtensions registration wiring a second consumer requires (recon-DEV-414 supplemental section 5 lines 96-99). StrandedMovieSweeper invoking both sweep handlers in independent guarded passes implements Q5.
- Architecture basis: new types (ProcessFeaturesCommand/Handler, feature-specific ProcessFeaturesOutcome with no metadata transition per Q2, SweepMissingMovieFeaturesCommand/Handler) sit in existing Core/Features/Enrichment alongside existing handlers (recon-DEV-414 section 2.3 item 2: no new layer evidenced), with explicit handlers and no MediatR (constitution II, recon section 6 line 105), Core behind existing ports (constitution I, line 104). No reusable/generic transport framework, second publisher, or generic consumer abstraction is authorised - PRODUCT.md section 2 bars speculative abstractions unless explicit in a ticket.
- Negative confirmations: no new NuGet/npm package (recon section 2.3 item 1 not evidenced), no new project, top-level folder, or architectural layer (item 2), no schema or migration change - existing movie_features upsert and schema stay unchanged (item 3; recon supplemental section 1), no public HTTP API shape (item 4; no route named), and nothing touching Features:LocalPlay, secrets, or Process.Start (item 5; not referenced). Therefore no section 2.3 item besides item 6 is triggered, and item 6 is satisfied by this deliberate authorisation citing the forcing AC above; no ticket-text change and no constitution departure arise.

---
### Q7

[from Patron] DEV-414 Q7 ruling: APPROVED, with the item 6 authorisations below - the active-only feature recovery pass and inactive-host absence preserve ticket scope. Registration ordering is approved as a design proposal, not a claim about current Program order.
- Activation basis: recon-DEV-414 supplemental section 5 lines 96-99 - AddLamuFlixRabbitMq activates EnrichmentConsumer only when IsConsumerActive (IMetadataProvider plus IMovieRepository registered, RabbitMqServiceCollectionExtensions.cs:70-71), otherwise registering InactiveEnrichmentConsumer, and a features consumer needs an equivalent prerequisite gate naming IEmbeddingGenerator and IMovieFeaturesRepository. Ruled: a feature-specific predicate requiring IMovieRepository, IMovieFeaturesRepository, IEmbeddingGenerator<string, Embedding<float>>, and the RabbitMQ queue publisher, independent of IMetadataProvider; handlers and FeaturesConsumer register only when it holds; dependency registration precedes predicate evaluation. In incomplete hosts, log the feature consumer inactive and never resolve missing dependencies; metadata consumer activation is unchanged.
- Recovery-scope basis: DEV-414 AC line 17 is delivered in the active Api composition where all four prerequisites are registered, mirroring the existing inactive-consumer pattern above, so absence in test/minimal hosts removes no ticket scope. The feature sweep pass runs only in that active composition; when inactive it is skipped as a logged absence, and per Q5 it must not suppress the existing Pending-enrichment pass, which continues unchanged. StrandedMovieSweeper is currently always hosted (Program.cs:34, recon supplemental section 5 line 99), so conditional feature-pass execution is the approved composition behaviour.
- Observability basis: constitution VI (recon-DEV-414 section 6 line 107) - a single trace across API to RabbitMQ to consumer, structured logs carrying movie id, and existing /health/live and /health/ready endpoints. Ruled: producer to consumer to embedding/upsert tracing; structured MovieId/Attempt/category with safe no-input outcome; a concrete feature liveness state (no new interface or layer, per PRODUCT.md section 2 speculative-abstraction bar) with a readiness health check named features-consumer on the existing readiness endpoint, separate from metadata worker liveness so a feature disconnect/failure is never masked by it. No new endpoint, HTTP status, or DTO contract is authorised, so section 2.3 item 4 is not triggered. Reconnect/shutdown follows the EnrichmentConsumer pattern with injected TimeProvider for delay/time (constitution VII, recon section 6 line 108).
- Item 6 authorisation: RabbitMqServiceCollectionExtensions, Api Program composition, and health-registration/test edits to existing files are expressly authorised as forced by DEV-414 Scope lines 20-23 and AC line 17. No new package, project, folder, layer, schema, or security/LocalPlay/Process.Start change; no ticket-text change and no constitution departure arise.

---
### Q8

[from Patron] DEV-414 Q8 ruling: APPROVED - the six-step ordering and acceptance matrix govern the Build workspace pickup; no gate, test, or build run is authorised or claimed in this Spec workspace.
- Ordering basis: task-pipeline workspace boundary (this Spec workspace ends at Ready to build; Phases 3-6 run in LamuFlix Build workspace) and Phase 3 step 2 (Anvil implements tasks.md one phase per ask, each phase committed and reported before the next), step 3 (Cog /refactor once over the whole build), step 5 (Gauge gates after refactor), then Phase 4 three-axis review. Steps 1-6 map onto that sequence: Wisp drift/contracts and Gauge baseline at Build pickup, then port/EF extensions, handler, consumer/routing/publisher/activation/liveness, sweeper passes, host/E2E - each production phase committed before the next begins.
- Test-evidence basis: constitution IX (recon-DEV-414 section 6 line 110) requires integration tests for retry-queue, DLQ, sweeper, and single-trace-id end-to-end, permits NSubstitute only, and forbids mocking Infrastructure; AGENTS.md bars mocking the data-access driver where real query translation or serialisation is at issue, so EF scalar plot load, the anti-join, upsert uniqueness under repeated/concurrent redelivery, and broker retry/DLQ behaviour are verified on real Testcontainers PostgreSQL/RabbitMQ, never driver mocks or an external Ollama download (FakeEmbeddingGenerator already exists for tests, recon section 1). Unit tests with fake generator, TimeProvider, and Core-port doubles cover handler outcomes, no-op cases, category precedence (Q4 continuation), invalid vector, retry/exhaustion boundaries, requested cancellation, and enrichment state unchanged (Q2).
- Matrix completeness: steps 3-6 as proposed cover every settled behaviour - Q3 no-ops and one-vector upsert, Q4 Attempt+1 delayed retry, permanent/exhausted DLQ with sanitised headers, malformed/Attempt below 1, shutdown requeue, failed-publish no-ack, unroutable expectation (live-broker test at RabbitMqPublisherTests.cs:138-147, recon-DEV-414-2 section D line 144, not executed in recon), Q5 independent sweeper passes with fresh Attempt 1 after terminal failure and enqueue-failure eligibility, and Q7 active/inactive DI, features-consumer readiness, timestamp/dimension, row uniqueness, and metadata-state isolation with the same trace.
- Gate basis: applicable Roslyn/complexity/InspectCode, full tests, property tests, vulnerable-packages, format, and mutation per harness/pipeline; mutation never with --since (charter standing ruling, Stryker since is broken from worktrees) and no threshold is ever lowered. Q1 verdicts apply exactly: exit 1 and Could not run block; scope-empty SKIPPED, mutation NOT APPLICABLE, harness SKIP, and recorded property opt-out are non-blocking, reported verbatim, never PASS.
- Item 6 authorisation: existing tests and test fakes/doubles implementing the extended IMovieRepository/IMovieFeaturesRepository ports may be edited as forced by Q6; unrelated pre-existing failures are reported, not silently fixed. No new package/project/folder/layer/API/schema/security change is introduced by this ordering; no ticket-text change and no constitution departure arise.

---
### Q9

[from Patron] DEV-414 Q9 final ruling: CONFIRMED - shared understanding reached; the grill closes and Q1-Q8 including the Q4 continuation are frozen as ruled, with no structural blocker, no remaining open question, and no owner checkbox.
- Freeze basis: Q1 closing terms (verified in specs/DEV-414/CONCLUSIONS.md) govern; Q2-Q8 rulings stand exactly as sent (patron-DEV-414-Q2.md through patron-DEV-414-Q8.md). Frozen scope is only: features.requested consumer; plot embedding via the existing generator with MovieId upsert; bounded per-chain retry/DLQ within the corrected publication limits (no asserted BasicPublishAsync broker-ack/nack/return or exactly-once guarantee, live-broker tests decide, Q4 continuation); independent missing-features sweeper recovery with deliberate per-sweep Attempt 1 reset (Q5); the minimal existing-port/EF/publisher/composition extensions authorised under section 2.3 item 6 (Q6-Q8); feature liveness/trace (Q7); and the required tests/gates ordering (Q8). Anything else is a follow-up issue under the charter criteria, not a finding in this round.
- Negative scope confirmed: no new dependency, schema/migration, or public HTTP change; no generic transport framework; no model/version/hash persistence; no lifetime retry suppression; no recommendation algorithm; no UI; nothing touching Features:LocalPlay, secrets, or Process.Start; no historical DEV-371 rewrite (its 6.8.1 text is stale evidence; CPM RabbitMQ.Client 7.2.2 governs, Q4 continuation). Every ruling cited ticket Scope/AC, PRODUCT.md, constitution, or recon as its basis, so no clause contradicts or changes ticket text and no constitution departure is proposed - the two section 2.3 escalation conditions are not met, hence no blocked: structural and no PR owner checkbox. No [assumed] taste ruling was used.
- Vocabulary authorised: constitution VIII (recon-DEV-414 section 6 line 109) requires a new domain term to be added to CONTEXT.md in the same PR, and additive CONTEXT.md entries are authorised under section 2.3 item 6 as a forced spec edit: Feature generation = deriving a movie plot embedding for that movie; Feature transport attempt = one delivery attempt in a feature-generation retry chain, restarted at 1 by recovery; Feature recovery = identifying movies with a nonblank plot and no stored features and requesting feature generation without claiming metadata enrichment. These are deliberate new definitions recorded in brief.md, all existing definitions retained verbatim; any discovered glossary conflict returns needs decision to Patron rather than a silent rewrite.
- Downstream limits: Quill drafts specify/clarify/plan/tasks/checklist solely from the frozen brief with no unapproved implementation guess (a gap is needs decision to Keel), Keel analyzes afterward (task-pipeline Phase 2 step 3). ADR creation remains deferred by Conductor instruction and is not claimed complete. This Phase A freeze authorises no Phase B execution and does not waive specs/PRODUCT.md sequencing; Build pickup still begins with Q8 step 1 in the LamuFlix Build workspace.

