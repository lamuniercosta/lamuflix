# DEV-420 grill conclusions

## Q1/12 — prerequisite contract evidence (2026-10-25)

Status: needs recon. Grill paused after Q1; scope is not frozen and brief is not closed. No Gate 1 or implementation clearance is given.

### Keel question and recommendation

[from Keel] DEV-420 /grill-with-docs Q1/12. Phase 2 step 1 only: grill and brief, later clarify/checklist/ADR. Worktree F:/Dev/LamuFlix.worktrees/feature-420-spec, feature/420-spec at 8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3 verified clean. Please use task note DEV-420, recon-DEV-420, standing charter, PRODUCT and ADR-0027; no searches outside that evidence. Task note 27-30 makes DEV-329 staged rows a dependency, requires Infrastructure relay in Api host, confirmed batch publishing, backoff, two-handler enqueue removal, retention, health/backlog metrics, Postgres+RabbitMQ integration, excludes multiple concurrent relays. Recon 38-49 and 226-230 finds no outbox source/schema and zero DEV-329 commits at this baseline. ADR-0027 requires same-transaction staging, confirmed mandatory publish, ProcessedAt only after settled broker confirm, uncertain outcomes remain pending, retention and sweeper safety net. I recommend treating DEV-329 contracts as authoritative prerequisite evidence, never designing replacement staging/schema in DEV-420; request bounded recon of the approved DEV-329 contract before settling relay interfaces or freezing scope. Cost: grill pauses for evidence; avoids inventing a dependency or expanding ticket delivery. Do you rule that this prerequisite blocks continuing the grill until that contract is supplied? Please cite basis and answer this one decision. I will append the complete exchange to specs/DEV-420/CONCLUSIONS.md; no production writes. Report back to Keel when complete.

### Patron answer

[from Patron] DEV-420 Q1/12 ruling: needs recon — prerequisite blocks continuing grill until bounded recon of approved DEV-329 contract is supplied; see C:/Users/lamun/AppData/Local/Temp/patron-dev-420-q1.txt

Full ruling supplied at that exact path:

[from Patron] Ruling: needs recon — DEV-420 Q1 prerequisite blocks continuing the grill until bounded recon of the approved DEV-329 contract (outbox_messages table schema, entity/port types, and staging semantics) is supplied; never invent or replace DEV-329 staging/schema in DEV-420.
- Task note DEV-420 lines 27-29: explicitly declares DEV-329 (staged rows) a prerequisite dependency and scopes DEV-420 to relaying outbox_messages, cutover of ImportMovieFolder and RequestEnrichment direct enqueue, retention delete, and backlog metrics.
- recon-DEV-420 lines 38-49 and 226-230: verifies zero outbox source/schema matches and 0 DEV-329 commits in feature/420-spec at 8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3, while docs/adr/0027-transactional-outbox-enrichment.md lines 9, 15, 22-23 split staging (DEV-329) from relay/retention follow-up (DEV-420).
- Standing charter §2.3 (items 3, 6) and specs/PRODUCT.md §3, §5 (items 3, 6): ticket text is authoritative; designing replacement staging or schema in DEV-420 would violate ticket scope and touch unnamed schema/files, so missing prerequisite contract evidence routes as needs recon via the Conductor path before settling relay interfaces or freezing scope.

### Implications and resumption

Conductor must supply bounded recon of the approved DEV-329 outbox_messages schema, entity/port types, and staging semantics with source identity and citations. Do not invent replacement contracts. Q1 is settled and must not be re-asked. Resume at Q2/12 after that evidence arrives; 11 questions remain. Closing bar, frozen scope, round cap, remaining relay policy and plan decisions are not yet settled. No brief.md is created because shared understanding and the closing decisions have not been reached. Later Phase 2 clarify/checklist and ADR work remains outside this ask.

---

## Q2/12 — integration and cutover

### Keel question

[from Keel] DEV-420 /grill-with-docs Q2/12: dependency integration and cutover. Updated recon-DEV-420 now pins approved DEV-329 packet to feature/329-spec d85978f9957a321137626ccb43743c384869bbb7 (PR #256 per Conductor); bf1cb3b9 is its parent. Sections 2-4 supply four-column outbox_messages, partial pending index, scoped EfEnrichmentOutbox and void IEnrichmentOutbox.Stage(EnrichmentRequested), adapter-local Web JSON UTF-8 payload, Stage -> shared save -> direct enqueue in the two handlers, with DEV-420 owning confirmed processing/retention. I recommend adopt those contracts unchanged, require landed DEV-329 and pickup drift verification before implementation, and cut over only ImportMovieFolder and RequestEnrichment by removing their IEnrichmentQueue dependencies/direct publishes while preserving Stage before shared save and atomicity/error semantics. Successful durable save then succeeds regardless of broker availability; save/staging failures still propagate and no handler retries are added. All other enqueue callers and sweeper behavior remain outside cutover. This follows DEV-420 task note 27-30, recon 148-194 and ADR-0027. Cost: asynchronous delivery and possible at-least-once duplicates, with dependency merge required before build. Do you approve this integration/cutover boundary? Please give a cited ruling, one decision, and reply directly to Keel; I record the full exchange in CONCLUSIONS.md.

### Patron ruling

[from Patron] Ruling: Approved — adopt the frozen DEV-329 contracts (`outbox_messages` four-column schema + pending index, `IEnrichmentOutbox.Stage(EnrichmentRequested)`, scoped `EfEnrichmentOutbox`, `OutboxMessageRecord`, Web JSON UTF-8 `bytea` payload) unchanged, require landed DEV-329 and pickup drift verification before Phase B implementation, and cut over only `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler` by removing their `IEnrichmentQueue` dependency and direct `EnqueueAsync` call while preserving `Stage` before the shared save, atomicity/error propagation, and all other `IEnrichmentQueue` callers and sweeper behavior unchanged.
- Task note `DEV-420:27-30` and `docs/adr/0027-transactional-outbox-enrichment.md:9,15-17,22-23`: explicitly scopes DEV-420 to relaying `outbox_messages` staged by DEV-329, removing `queue.EnqueueAsync` from `ImportMovieFolder` and `RequestEnrichment` handlers, retention delete, and keeping the sweeper as the safety net.
- `recon-DEV-420:42-196` (`specs/DEV-329/CONCLUSIONS.md:60,83,104,125,146,262` and `specs/DEV-329/spec.md:93-98`): establishes the authoritative prerequisite schema, port/adapter types, and `Stage` -> shared `SaveChangesAsync` atomicity boundary owned by DEV-329 while delegating confirmed processing, direct-publish removal, and retention to DEV-420.
- Standing charter §2.3 (items 3, 6) and `specs/PRODUCT.md` §3, §5 (items 3, 6): ticket text is authoritative; confining handler cutover strictly to the two ticket-named handlers and adopting DEV-329's schema/staging contracts unchanged avoids touching unnamed callers (`MatchMovieImdbId`, `ProcessEnrichment`, `RequeueStrandedMovies`, `SweepMissingMovieFeatures`) or altering DEV-329's schema.


Implication: Q1 evidence prerequisite satisfied for planning by updated recon; implementation still awaits landed DEV-329 and pickup drift verification.

---

## Q3/12 — relay persistence and publish lifecycle

### Keel question

[from Keel] DEV-420 /grill-with-docs Q3/12: relay persistence boundary and batch lifecycle. I recommend one non-overlapping OutboxRelayHostedService loop in existing Infrastructure, registered in Api host, using a fresh DI scope/DbContext per pass and bounded reads of durable unprocessed rows ordered created_at then id. Materialize at most BatchSize, publish sequentially and mark each row individually only after confirmed success; save that marker before proceeding. Do not hold a DB transaction/connection across broker waits, batch-mark unconfirmed rows, mutate Stage, add a Core relay port, claim metadata, schema, project, library or generic service layer. Use the existing Infrastructure persistence context/record directly for relay reads/marking and existing IEnrichmentQueue confirmed mandatory path for publishing. Deserialize stored bytes as EnrichmentRequested using the DEV-329 Web JSON contract; preserve MovieId and Attempt and prove semantic wire parity in integration tests. Malformed/unusable payload is a failure and remains unprocessed. Basis: recon-DEV-420 44-53,95-144,179-194; ADR-0027 confirmed marking, at-least-once and single Api relay; PRODUCT no speculative abstraction. Cost: per-row marker saves and duplicates if publish succeeds before marker commit; no multi-instance ownership. Do you approve this Infrastructure-only persistence/publish lifecycle? Please cite basis and return ruling directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — implement one non-overlapping `OutboxRelayHostedService` loop in `LamuFlix.Infrastructure` registered in the `LamuFlix.Api` host, using a fresh DI scope/`LamuFlixDbContext` per pass to query up to `BatchSize` durable unprocessed `OutboxMessageRecord` rows ordered by `(created_at, id)` (`WHERE processed_at IS NULL`), deserialize each payload via the DEV-329 `JsonSerializerDefaults.Web` `EnrichmentRequested` contract, publish sequentially through the existing confirmed mandatory `IEnrichmentQueue` path, and set/save `ProcessedAt` via injected `TimeProvider` per row only after settled confirm without holding a DB transaction across broker waits or adding any Core relay port, schema column, or wrapper layer.
- Task note `DEV-420:27-30` and `docs/adr/0027-transactional-outbox-enrichment.md:15-17,21-23`: places `OutboxRelayHostedService` in `Infrastructure` registered from the single `Api` host, mandates confirmed mandatory publishing and setting `ProcessedAt` only after the broker confirm settles, leaves uncertain or failed rows unprocessed (`ProcessedAt IS NULL`), and relies on enrichment claims to absorb at-least-once duplicates.
- `recon-DEV-420:44-53,95-144,179-194`: establishes the DEV-329 four-column `outbox_messages` schema, partial index `(created_at, id) WHERE processed_at IS NULL`, `OutboxMessageRecord`, and `JsonSerializerDefaults.Web` UTF-8 `EnrichmentRequested(MovieId, Attempt)` payload contract, which `(created_at, id)` batch reads and Web JSON deserialization consume directly with semantic wire parity.
- Standing charter §2.3 (items 1, 2, 3) and `specs/PRODUCT.md` §2, §5 (items 1, 2, 3): forbids speculative abstractions, extra ports/layers, or unapproved schema/dependency additions when `OutboxRelayHostedService` is an `Infrastructure`-internal worker operating on `LamuFlixDbContext` and `IEnrichmentQueue`.


---

## Q4/12 — failure and cancellation policy

### Keel question

[from Keel] DEV-420 /grill-with-docs Q4/12: failure and cancellation policy. I recommend fail the current pass on the first deserialize/publish/marker-save/database failure, dispose the scope, keep every uncertain row unprocessed, and retry from a fresh ordered read after capped exponential backoff. Confirm timeout, nack, mandatory return, connection loss, cancellation before confirmation or uncertain marker commit must never justify marking sent; cancellation during shutdown exits without a retry delay or synthetic failure. Previously committed markers survive. A successful confirmed publish followed by failed marker commit may redeliver; existing claim semantics absorb duplicates. Do not attempt in-memory retry, quarantine, dead lettering, extra columns, dropping rows, or advancing past poison rows in this ticket. Thus an invalid oldest row blocks later delivery and must surface as failed progress/readiness until corrected operationally; this is preferable to silently skipping durable work. Reset consecutive failure backoff only after a successful pass, including a successful empty DB poll; use TimeProvider for delays/time. Basis ADR-0027:15-17, recon-DEV-420 44-53,169-194, task failure-backoff requirement. Cost: head-of-line blocking and temporary duplicate delivery instead of data loss. Do you approve this failure/retry contract? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — abort the current relay pass on the first deserialize, broker publish/confirm, marker-save, or database failure, dispose the DI scope, keep any unconfirmed or uncertain row unprocessed (`ProcessedAt IS NULL`) while preserving previously committed markers, retry from a fresh `(created_at, id)` read after capped exponential backoff (resetting consecutive failures only after a successful pass, including an empty poll, and using `TimeProvider` for all time/delays), exit cleanly on host shutdown cancellation without retry delay or synthetic failure, and never skip, drop, quarantine, or add columns for failed/poison rows.
- `docs/adr/0027-transactional-outbox-enrichment.md:15-17`: mandates setting `ProcessedAt` only after settled broker confirm, leaving rows unprocessed on confirm timeout, nack, mandatory return, or connection failure, and relying on enrichment claim leases to absorb at-least-once duplicates if a publish confirms before marker commit succeeds.
- Task note `DEV-420:28` and `recon-DEV-420:44-53,169-194`: requires failure backoff while constraining `outbox_messages` to the frozen four-column DEV-329 schema with no retry/error/quarantine columns or silent data loss, so an unprocessable oldest row halts progress and surfaces via health/backlog telemetry rather than being skipped.
- `AGENTS.md` Non-negotiables and standing charter §2.3 (item 3): requires injected `TimeProvider` for all time and delay operations and forbids schema additions beyond the approved `outbox_messages` table.


---

## Q5/12 — configuration and scheduling

### Keel question

[from Keel] DEV-420 /grill-with-docs Q5/12: configuration and scheduling policy. I recommend Infrastructure OutboxRelayOptions bound under OutboxRelay, validated at startup: BatchSize default 100 range 1..1000; PollInterval default 1s positive; PublishTimeout default 30s positive; FailureBackoffMin default 1s positive; FailureBackoffMax default 60s >= min; ProcessedRetention default 7 days positive; CleanupInterval default 1 hour positive; CleanupBatchSize default 1000 range 1..10000; BacklogAgeWarning default 5 minutes positive. Exact proposed names are internal config, no public endpoint/DTO changes. Start relay polling immediately; delay PollInterval after each successful pass (even full batch), with failure delay min*2^(consecutiveFailures-1) saturated at max, no jitter or new randomness. Bound each row publish with linked shutdown/timeout cancellation and TimeProvider; marker DB work uses host cancellation, never the already-expired publish token. No new enable/disable flag; one Api instance must host it. Cleanup scheduling policy is next question. These are proposed tunable operational defaults, not existing facts or gate thresholds. Basis task note 28 batch/backoff/retention and ADR-0027 one relay, recon four-column schema; no schema/dependency additions. Cost: capped throughput and configuration surface, deterministic scheduling. Do you approve this options/timing contract? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — bind and startup-validate `OutboxRelayOptions` under configuration section `OutboxRelay` in `LamuFlix.Infrastructure` with the proposed defaults and bounds (`BatchSize` 100 in 1..1000, `PollInterval` 1s > 0, `PublishTimeout` 30s > 0, `FailureBackoffMin` 1s > 0, `FailureBackoffMax` 60s >= min, `ProcessedRetention` 7d > 0, `CleanupInterval` 1h > 0, `CleanupBatchSize` 1000 in 1..10000, `BacklogAgeWarning` 5m > 0), start polling immediately on host startup, delay `PollInterval` after each successful pass and `min * 2^(consecutiveFailures - 1)` capped at `max` (no jitter/randomness) via `TimeProvider`, bound per-row publish with a linked shutdown + `PublishTimeout` token while saving `ProcessedAt` under the host shutdown token, and add no feature toggle.
- Task note `DEV-420:27-30` and `docs/adr/0027-transactional-outbox-enrichment.md:15,21-22`: requires batch polling with confirms, failure backoff, retention delete, and health/backlog telemetry on a single `Api` relay instance without an opt-out toggle.
- `AGENTS.md` Non-negotiables: requires `TimeProvider` for all time/delay/timeout operations and avoids introducing unseeded randomness or hardcoded untunable constants by making operational intervals and bounds configurable via options.
- Standing charter §2.3 (items 1, 3, 4) and `specs/PRODUCT.md` §5 (items 1, 3, 4): internal `OutboxRelay` options add no NuGet package, no database schema change, and no public HTTP route or DTO surface.


---

## Q6/12 — bounded processed-row retention

### Keel question

[from Keel] DEV-420 /grill-with-docs Q6/12: bounded retention lifecycle. I recommend cleanup in the same single worker, independently scheduled within its non-overlapping loop, first due at startup and then each CleanupInterval using TimeProvider; attempt a due cleanup even if relay publishing failed, in a separate fresh DB scope. At most CleanupBatchSize rows per due run, ordered processed_at then id, deleting only rows with processed_at strictly older than now minus ProcessedRetention and rechecking that predicate in the delete. Never delete an unprocessed row or retain by created_at. A large eligible set drains over subsequent scheduled cleanup runs; cleanup failure does not undo published markers or block later publication, is separately logged/observable, and is retried at next CleanupInterval rather than a tight loop. No new table, index, retention worker, or schema migration. Basis task retention delete, ADR-0027:22 retention window and recon-DEV-420:44-53,188-194 DEV-420 processing boundary. Cost: processed storage may temporarily exceed retention age; cleanup is bounded and cannot starve delivery. Do you approve this retention policy? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — execute retention cleanup inside the same `OutboxRelayHostedService` loop on an independent `TimeProvider` schedule (first due at startup, then every `CleanupInterval`, even if the publish pass failed) in its own fresh DI scope, deleting at most `CleanupBatchSize` rows per due run ordered by `(processed_at, id)` where `processed_at IS NOT NULL AND processed_at < now - ProcessedRetention` (rechecked in the delete predicate), never deleting unprocessed rows or filtering by `created_at`, isolating cleanup errors from publish/marker progress, and adding no new hosted service, table, index, or migration.
- Task note `DEV-420:28` and `docs/adr/0027-transactional-outbox-enrichment.md:22-23`: requires retention delete of processed rows after a retention window inside `OutboxRelayHostedService` without deleting on publish.
- `recon-DEV-420:44-53,188-194`: assigns retention behavior to DEV-420 while fixing the `outbox_messages` schema and single partial index in DEV-329, so bounded `(processed_at, id)` batch deletes with `processed_at < cutoff` preserve every unprocessed row (`processed_at IS NULL`) without schema or index changes.
- `AGENTS.md` Non-negotiables and standing charter §2.3 (items 2, 3): uses `TimeProvider` for retention cutoffs and interval scheduling and avoids introducing an unrequested second background service or extra migration.


---

## Q7/12 — relay health and backlog telemetry

### Keel question

[from Keel] DEV-420 /grill-with-docs Q7/12: health and telemetry contract. I recommend one Infrastructure OutboxRelayHealthCheck registered with Ready tag under name outbox-relay, using existing /health/ready infrastructure and existing status mapping, with no new route, response body, DTO or live-endpoint change. A thread-safe worker-owned snapshot records lifecycle, latest publish-pass outcome, latest successful DB backlog sample, pending count, oldest pending CreatedAt and cleanup outcome; gauges read the cached snapshot with zero DB I/O in metric callbacks. Sample count and oldest pending after each successful pass in a fresh query scope; sample failure is a relay pass failure, stale samples must never be presented as current or zero. Unhealthy until first successful sample, when latest pass/sample failed or worker unexpectedly stopped; Degraded when oldest unprocessed age >= BacklogAgeWarning or latest cleanup failed; Healthy otherwise. Empty backlog age is zero only from a successful empty sample. A later successful pass/cleanup clears its respective fault. Export stable low-cardinality metrics lamuflix.outbox.pending (rows), lamuflix.outbox.oldest_pending_age (seconds), lamuflix.outbox.published (confirmed-and-marked rows), lamuflix.outbox.failures (failed passes), lamuflix.outbox.retention.deleted (rows), lamuflix.outbox.retention.failures (cleanup runs); no MovieId/payload/exception-message labels. Structured failure logs may contain outbox row id and exception, never payload/secrets. No new telemetry package or Core abstraction; internal OutboxRelayState/OutboxRelayMetrics in Infrastructure is sufficient. Basis task note 28 relay health backlog metrics, recon approved CreatedAt/ProcessedAt; existing Ready endpoint from initial recon. Cost: cached backlog is sampled, Degraded uses existing HTTP mapping. Do you approve this observable-state contract? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — register `OutboxRelayHealthCheck` (`outbox-relay`, `Ready` tag) in `LamuFlix.Infrastructure` on the existing `/health/ready` endpoint without any new route or DTO, backed by a thread-safe worker-owned `OutboxRelayState` snapshot updated after each successful pass in a fresh DB query scope (`Unhealthy` before first sample, on failed pass/sample, or unexpected worker stop; `Degraded` when oldest pending age >= `BacklogAgeWarning` or latest cleanup failed; `Healthy` otherwise, with zero DB I/O in health/metric callbacks) and export the six low-cardinality `lamuflix.outbox.*` instruments in `OutboxRelayMetrics` with structured logs omitting payload/secrets.
- Task note `DEV-420:28` and `recon-DEV-420:44-53` (plus initial `recon-DEV-420:207-208,231-237`): requires relay health and backlog metrics while existing `/health/ready` (`Ready`-tagged checks) and OpenTelemetry packages already exist, and the DEV-329 partial index `(created_at, id) WHERE processed_at IS NULL` supports sampling pending count and oldest `created_at` directly.
- Standing charter §2.3 (items 1, 2, 4, 5) and `specs/PRODUCT.md` §5 (items 1, 2, 4, 5): reuses existing health routes, status mappings, and OpenTelemetry dependencies in `Infrastructure` without adding any public HTTP endpoint, DTO, NuGet dependency, or Core abstraction, and never logs payloads or secrets.
- `AGENTS.md` Non-negotiables: computes oldest pending age via injected `TimeProvider` from the sampled `CreatedAt` timestamp and keeps metric/health callbacks deterministic and free of synchronous DB I/O or stale-zero masking.


---

## Q8/12 — acceptance proof strategy

### Keel question

[from Keel] DEV-420 /grill-with-docs Q8/12: acceptance proof strategy. I recommend real PostgreSQL + RabbitMQ Testcontainers using existing shared fixtures/packages for the ordinary path: commit a staged movie/request through each cutover handler while broker is unavailable (no direct enqueue), restore broker, run relay, observe correct MovieId/Attempt wire payload and ProcessedAt only after confirmed mandatory publish, verify no republish after marker survives restart, and prove batch limit/order. Real Postgres tests cover unprocessed/future/recent/cutoff-equal retention survivors, bounded oldest processed deletion, empty backlog, transaction rollback and no schema change. Controlled publisher faults (not a mocked DB driver) plus real DB prove pre-confirm marker absence, nack/return/timeout/connection failures, confirmed-publish then marker-save failure, fresh-scope recovery/redelivery and committed prefix preservation; a focused real broker mandatory-return scenario proves failures really propagate through the existing publisher. Deterministic TimeProvider tests cover polling/backoff saturation/reset/shutdown, timeout distinction, cleanup schedule/error isolation, options bounds, cached health/metrics recovery and poison-row head-of-line failure; no wall-clock sleeps or probabilistic timing assertions. Host composition/readiness tests prove exactly one worker/check registration and sweeper retained. Reuse existing assertion/mocking patterns; no dependency addition or generated contract edits. This fulfills task note 28 and Q2-Q7 without expanding delivery; cost is container-backed integration runtime. Do you approve this acceptance-proof boundary? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — verify DEV-420 with real PostgreSQL and RabbitMQ Testcontainers (reusing existing shared fixtures/packages) for the end-to-end cutover and relay path (handler commit while broker unavailable, relay confirmed mandatory publish with wire-parity `MovieId`/`Attempt`, `ProcessedAt` persistence, restart idempotence, batch order/limit, retention cutoff/batch bounds on real Postgres, and a real-broker mandatory-return failure proof), paired with controlled `IEnrichmentQueue` fault injection against a real Postgres DB for pre-confirm/mid-batch/post-confirm marker failure scenarios and deterministic `FakeTimeProvider` unit/host-composition tests for scheduling, backoff, options validation, health/metrics snapshot transitions, poison-row head-of-line blocking, and single worker/health-check registration alongside `StrandedMovieSweeper`.
- Task note `DEV-420:27-28` and `docs/adr/0027-transactional-outbox-enrichment.md:15-17,22`: explicitly requires an integration test with Postgres and RabbitMQ covering confirmed relay publishing, direct-enqueue cutover of `ImportMovieFolder` and `RequestEnrichment`, failure backoff, retention delete, health/backlog metrics, and sweeper retention.
- `AGENTS.md` Non-negotiables: mandates Testcontainers (never mocking the data-access driver for query translation or serialization), `TimeProvider` for deterministic time/delay testing without wall-clock sleeps, and matching the repo's existing assertion and mocking libraries.
- Standing charter §2.3 (items 1, 3, 4) and initial `recon-DEV-420:207-208,216-219`: reuses pinned `Testcontainers.PostgreSql` and `Testcontainers.RabbitMq` packages without adding dependencies, schema changes, or contract edits.


---

## Q9/12 — exact delivery file boundary

### Keel question

[from Keel] DEV-420 /grill-with-docs Q9/12: bounded delivery file authorization under charter 2.3. I recommend new production files only src/LamuFlix.Infrastructure/Enrichment/{OutboxRelayHostedService,OutboxRelayOptions,OutboxRelayState,OutboxRelayMetrics,OutboxRelayHealthCheck,OutboxRelayServiceCollectionExtensions}.cs. Existing production edits only src/LamuFlix.Api/Program.cs for AddLamuFlixOutboxRelay registration and the two ticket-named Core handlers for Q2 cutover. No publisher, queue port, Stage adapter, DbContext/record/configuration, migration, repository, other enqueue caller, contract/generated frontend, project/package, harness, secrets or LocalPlay edits. New tests only tests/LamuFlix.UnitTests/Enrichment/{OutboxRelayHostedServiceTests,OutboxRelayHealthCheckTests,OutboxRelayOptionsTests}.cs and tests/LamuFlix.IntegrationTests/{OutboxRelayTests,OutboxRetentionTests}.cs. Existing test adjustments limited to tests/LamuFlix.UnitTests/Features/{Import/ImportMovieFolderCommandHandlerTests,Enrichment/RequestEnrichmentCommandHandlerTests}.cs and tests/LamuFlix.IntegrationTests/{ImportEndpointTests,EnrichmentEndpointTests,ImportMovieFolderCommandHandlerPersistenceTests,ApiHostCompositionTests,HealthEndpointTests,ApiHostFactory,ApiEndToEndFactory}.cs, solely for cutover expectations, host/check registration, and isolating unrelated factory tests from the new worker. Real relay tests must explicitly run the worker; test-host removal cannot count as delivery proof. Test support must reuse available TimeProvider support without a new package; this proposal makes no claim that a particular fake type is already present. Specs/DEV-420 packet and later dedicated ADR are documentation only; current ask writes CONCLUSIONS and brief only. These paths are proposed plan decisions based on the original recon test/layout facts, not new search results. If a later drift fact requires another file, return for a cited scope ruling before editing it. Cost: fixed paths require explicit scope reconciliation rather than broad cleanup. Do you approve this exact bounded file set and the required narrow rewrites of unnamed host/test files? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — authorize the exact 23-file delivery boundary for DEV-420: 6 new files under `src/LamuFlix.Infrastructure/Enrichment/` (`OutboxRelayHostedService`, `OutboxRelayOptions`, `OutboxRelayState`, `OutboxRelayMetrics`, `OutboxRelayHealthCheck`, `OutboxRelayServiceCollectionExtensions`), 3 modified production files (`src/LamuFlix.Api/Program.cs`, `ImportMovieFolderCommandHandler.cs`, `RequestEnrichmentCommandHandler.cs`), 5 new test files (`OutboxRelayHostedServiceTests`, `OutboxRelayHealthCheckTests`, `OutboxRelayOptionsTests`, `OutboxRelayTests`, `OutboxRetentionTests`), and 9 narrowly adjusted existing test/factory files (`ImportMovieFolderCommandHandlerTests`, `RequestEnrichmentCommandHandlerTests`, `ImportEndpointTests`, `EnrichmentEndpointTests`, `ImportMovieFolderCommandHandlerPersistenceTests`, `ApiHostCompositionTests`, `HealthEndpointTests`, `ApiHostFactory`, `ApiEndToEndFactory`), with any additional file requiring a cited scope ruling before edit.
- Task note `DEV-420:28`: explicitly names `OutboxRelayHostedService` in `Infrastructure` registered from the `Api` host, direct-enqueue cutover of `ImportMovieFolder` and `RequestEnrichment`, retention delete, health/backlog metrics, and Postgres + RabbitMQ integration tests.
- Standing charter §2.3 (item 6) and `specs/PRODUCT.md` §5 (item 6): modifying unnamed tracked files (`src/LamuFlix.Api/Program.cs` and the 9 existing test/factory files) is a forced edit required by the ticket's acceptance criteria (Api host registration and removing `IEnrichmentQueue` from the two handlers) and verification gates (`dotnet test`), while all publisher, port, DEV-329 persistence/migration, and other caller files remain untouched.
- Standing charter §2.3 (items 1-5): places new relay classes inside the existing `src/LamuFlix.Infrastructure/Enrichment` feature folder without adding any project, top-level folder, NuGet package, database migration, or public API route/DTO.


---

## Q10/12 — task ordering and gate expectations

### Keel question

[from Keel] DEV-420 /grill-with-docs Q10/12: task ordering and gate expectations. I recommend Quill derive spec/plan/tasks only from the closed brief, in dependency order: (1) before build, verify DEV-329 landed contracts/schema/Stage and DEV-419 ADR against current main, re-run read-only consistency/drift analysis and resolve any mismatch; (2) introduce options + deterministic state/time tests and real DB/broker relay proof harness; (3) implement bounded confirmed per-row publish/marker loop and failure recovery against real DB; (4) implement isolated bounded retention and prove cutoffs; (5) add health/metrics + host registration and composition proof; (6) remove direct enqueue in the two handlers, adjust only authorized test expectations/factories, prove broker-down request success and broker-recovery relay; (7) run changed-file analyzer/complexity/InspectCode, relevant then full dotnet tests/property/security/format checks and pre-PR mutation as dictated by current harness.yml, never --since or changed thresholds, then exact-head Risk/Standards/Spec review and hosted checks at their later phase. Unit behavior tests may use controlled IEnrichmentQueue; DB query/serialization behavior always real Postgres. Current Phase A ask runs no gates/tests/analyzers and drafts no ADR/spec/plan/tasks. L clarify/checklist, dedicated ADR and plan challenge remain later Conductor steps, with Gate 1 distinct from implementation permission. A gate unable to run is Could not run; exit-2 scope-empty or opt-out remains named SKIPPED/SKIP or N/A as applicable, never PASS. No architecture or frontend gate is invented merely to fill a checklist. Cost: dependency and full failure proof precede final cutover. Do you approve this task/gate sequencing contract? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — derive `spec.md`, `plan.md`, and `tasks.md` strictly from the closed `brief.md` in the proposed 7-step dependency order (pre-build DEV-329/DEV-419 pickup drift verification, options + deterministic/Testcontainers proof harness, confirmed per-row relay loop + failure recovery, isolated bounded retention, health/metrics + host registration, handler direct-enqueue cutover + authorized test/factory adjustments, and Phase B harness verification gates), running zero gates/tests/analyzers or spec/ADR drafts in this Phase A step 1 ask.
- Standing charter Workspace boundary (§1) and `task-pipeline`: Phase A step 1 in `LamuFlix - Spec` writes only `CONCLUSIONS.md` and `brief.md`, while Quill's spec/plan/tasks drafting, L-tier `-clarify`/`-checklist`/ADR, `/speckit-analyze`, plan challenge, and Gate 1 (`gate1: provisional` then owner merge) precede Phase B implementation in the Build workspace.
- Task note `DEV-420:27-30` and `recon-DEV-420:42-196`: requires DEV-329's staged `outbox_messages` contracts to be verified landed on `main` before build and sequences relay publication, retention, and health/metrics ahead of cutting over `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler`.
- `AGENTS.md` Verification commands & Non-negotiables and standing charter §6 Known Tooling Limitations: mandates real PostgreSQL/RabbitMQ Testcontainers for DB query/serialization/broker proofs, `scripts/run-mutation.ps1` without `--since`, strict exit-code reporting (`Could not run`, `SKIPPED (scope-empty)`, `SKIP`, `N/A` never folded into `PASS`), and zero threshold changes.


---

## Q11/12 — closing bar, frozen scope and round cap

### Keel question

[from Keel] DEV-420 /grill-with-docs Q11/12: loop discipline and frozen delivery scope. I recommend closing bar: Critical, High and Medium verified in-scope findings block plan freeze and later code-review/ship-review clearance; Low observations are recorded without requiring a fix. Unnecessary abstraction beyond this ticket is Follow-up, never an automatic fix commit. Follow charter follow-up policy: eligible Critical/High broken behavior or user-requested work goes through Patron then Rigger, check for an existing ticket; otherwise record noted, no ticket. No accepted blocking finding is demoted to obtain clearance. Frozen scope: adopt DEV-329 staging/schema unchanged; single Api-host Infrastructure relay; bounded ordered confirmed mandatory publish with per-row markers; failure backoff/shutdown; processed-only retention; readiness/backlog metrics; two-handler direct enqueue removal; Q8 evidence and Q9 exact 23-file boundary; anything else is a follow-up issue, not a finding in this round. Round cap: two review/fix rounds for plan challenge and two for later code/ship review, with the grill independently capped at 12 questions. After cap, unresolved blockers halt for Conductor disposition; never add a third immutable review to repair a receipt or publication problem. Each round needs the complete Risk/Standards/Spec evidence set from Sentry/Ledger/Compass on the same exact target; missing axis blocks and cannot be waived by pre-pass. Draft consistency corrections go to Quill as one numbered list per round; Keel changes a decision in brief first. Later owner Gate 1 and user-owned merge remain separate. Cost: lower findings do not expand this round, and the cap can halt delivery. Do you approve these closing bar, frozen scope and round-cap terms? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — set the closing bar to verified in-scope Critical, High, and Medium findings (recording Low observations without fix commits and routing speculative abstractions or out-of-scope items per charter follow-up policy as `noted, no ticket` unless eligible Critical/High broken behavior or user-requested via Patron -> Rigger after duplicate check), freeze scope to the Q2-Q10 decisions and exact 23-file Q9 boundary, and cap review/fix loops at 2 rounds each for plan challenge and later code/ship review (with the 12-question grill cap and complete Sentry/Ledger/Compass Risk/Standards/Spec evidence on the exact target HEAD per round).
- Standing charter §2.3 ("Patron decides, Rigger records, the user owns the rest", "Answer size", and "Halt Conditions") and `specs/PRODUCT.md` §2, §3, §5: forbids speculative abstractions, demoting blockers without a YouTrack issue id, or silent scope expansion, and routes out-of-scope non-critical observations as `noted, no ticket` in `CONCLUSIONS.md`.
- Task note `DEV-420:27-30` and `docs/adr/0027-transactional-outbox-enrichment.md:15-17,21-23`: anchors the frozen scope to the approved DEV-329 schema/staging prerequisites, single-instance `OutboxRelayHostedService`, confirmed per-row publishing, backoff, retention delete, readiness/backlog metrics, two-handler cutover, and the 23-file Q9 boundary.
- `task-pipeline` and standing charter Workspace boundary (§1): requires all three challenger axes (Risk/Sentry, Standards/Ledger, Spec/Compass) on the same target commit, routes draft consistency fixes to Quill as one numbered list per round with any decision change landing in `brief.md` first, and keeps Gate 1 (`gate1: provisional` + user merge) separate from Phase B build.


---

## Q12/12 — shared understanding and grill closure

### Keel question

[from Keel] DEV-420 /grill-with-docs Q12/12: final shared understanding and permission to close the grill brief. I read back Q1 prerequisite supplied by updated recon from DEV-329 d85978f9957a321137626ccb43743c384869bbb7; Q2 unchanged Stage/save contracts and two-handler-only cutover; Q3 ordered bounded fresh-scope relay through existing confirmed mandatory IEnrichmentQueue, Web JSON semantic parity and per-row markers; Q4 fail-first with preserved committed prefix, uncertain outcomes retried, poison head-of-line surfaced, no skip/quarantine; Q5 validated options and deterministic TimeProvider schedule/backoff/timeout; Q6 separate-scope processed_at-only bounded retention; Q7 cached readiness/backlog with six named metrics; Q8 real Postgres+RabbitMQ/fault-injection/deterministic acceptance proofs; Q9 exact 23-file delivery boundary; Q10 dependency-first tasks and later harness gates; Q11 Critical/High/Medium block and two review rounds with all three axes. Canonical feature-local vocabulary: staged is tracked before shared save, durable is committed in Postgres, unprocessed is ProcessedAt null, confirmed is broker acceptance through mandatory confirmed path, processed is marker durably committed after confirmation, expired processed is ProcessedAt strictly older than retention cutoff. At-least-once is explicit, never exactly-once or global FIFO; ordering is only each pending batch query. For Q7 invalid or failed samples withhold backlog gauge measurements instead of reporting stale values as current or false zeros; counters still retain legitimate history and health reports Unhealthy. Reuse TimeProvider test support without introducing a package. Root CONTEXT and constitution are not changed; ADR-0027 historical constitution follow-up is not silently added. No owner ticket-change or constitution-departure is proposed by this brief; if you judge either necessary please explicitly mark blocked structural. Current ask ends with CONCLUSIONS and brief only: no Quill draft, L clarify/checklist, new ADR, implementation or Gate 1 clearance yet. Do you confirm shared understanding is complete, these precise read-back semantics are accepted, and I should close brief.md now without a thirteenth question? Please cite basis and reply directly to Keel.

### Patron ruling

[from Patron] Ruling: Approved — confirm shared understanding across Q1-Q12 is complete, ratify the canonical feature-local vocabulary (`staged`, `durable`, `unprocessed`, `confirmed`, `processed`, `expired processed`), explicit at-least-once/per-batch ordering semantics, Q7 gauge withholding on invalid/failed samples, and zero root `CONTEXT.md` or constitution edits, and authorize closing `specs/DEV-420/brief.md` and `specs/DEV-420/CONCLUSIONS.md` now at 12/12 without a 13th question or any owner escalation (`blocked: structural` is not triggered).
- Task note `DEV-420:27-30`, `docs/adr/0027-transactional-outbox-enrichment.md:15-17,21-24,28-30`, and `recon-DEV-420:42-196`: every Q1-Q12 decision delivers the exact ticket scope (relaying DEV-329 `outbox_messages` with publisher confirms, failure backoff, two-handler direct-enqueue removal, retention delete, health/backlog metrics, and Postgres + RabbitMQ integration proof) while keeping the sweeper and leaving historical constitution wording to its separate follow-up per ADR-0027:30.
- Standing charter §2.3, §3.1, §5 and `specs/PRODUCT.md` §3, §5: all six §2.3 care items are settled with cited rulings within the 12-question grill cap, and neither of the two owner escalations (ticket change or constitution departure) is present.
- Standing charter Workspace boundary (§1) and `task-pipeline`: Phase 2 step 1 ends with `specs/DEV-420/CONCLUSIONS.md` and `specs/DEV-420/brief.md` closed; Quill spec/plan/tasks drafting, L-tier `-clarify`/`-checklist`, dedicated ADR, `/speckit-analyze`, plan challenge, and Gate 1 remain subsequent Conductor steps.


### Closure receipt

All 12 questions are answered; no thirteenth question was asked. Q1 evidence blocker is superseded by the supplied DEV-329 contract recon and Q2/Q12 approvals. The grill is complete; brief.md now records its settled decisions. This closes only the grill, not plan challenge, Gate 1, implementation, review, or merge clearance. No owner structural escalation was raised. No production code, tests, gates, or later Phase 2 artifacts were executed or drafted on this ask.

---

## Post-grill scope ruling — C1 `TelemetryConstants.cs` as 24th Q9 delivery file (2026-10-25)

### Question

[from Conductor] Will you authorize adding the existing tracked `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` as a 24th Q9 delivery file solely to define the six Q7 `lamuflix.outbox` metric-name constants consumed by Infrastructure `OutboxRelayMetrics`?

### Patron ruling

[from Patron] Ruling: Approved — authorize adding the existing tracked `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` as the 24th Q9 delivery file (revising the frozen boundary from 23 to 24 files: 6 new production files under `src/LamuFlix.Infrastructure/Enrichment/`, 4 modified production files [`src/LamuFlix.Api/Program.cs`, `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs`, `src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs`, `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`], 5 new test files, and 9 narrowly adjusted existing test/factory files) solely to define `const` metric-name constants for the six approved Q7 instruments (`lamuflix.outbox.pending`, `lamuflix.outbox.oldest_pending_age`, `lamuflix.outbox.published`, `lamuflix.outbox.failures`, `lamuflix.outbox.retention.deleted`, `lamuflix.outbox.retention.failures`) consumed by `OutboxRelayMetrics`, with Keel updating `specs/DEV-420/brief.md` before Quill updates `spec.md`, `plan.md`, `tasks.md`, and `checklists/requirements.md`.
- Task note `DEV-420:28,64-66` and `specs/DEV-420/CONCLUSIONS.md:114,118,146,150` (`specs/DEV-420/brief.md:130-135,141,145-169`): ticket scope requires relay health/backlog metrics, Q7 approved the exact six `lamuflix.outbox.*` instruments in `OutboxRelayMetrics`, and Q9 explicitly reserved cited scope rulings before editing any additional tracked file needed for delivery.
- `recon-DEV-420:230-303`: verifies that `TelemetryConstants` at `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs:3-67` is the existing central metric-name definition file (`EnrichmentMetrics.cs:7,11,15,18`), that all six `lamuflix.outbox.*` constants are currently absent from it and all `.cs` files, and that `TelemetryConstants.cs` was omitted from the initial 23-file Q9 list.
- Standing charter §2.3 (item 6) and `specs/PRODUCT.md` §5 (item 6): modifying `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` is a forced narrow edit required to satisfy the ticket's metric acceptance criteria and constitutional centralized telemetry-naming rule (Principle VI) without introducing a constitution departure, new Core port/abstraction, package, or schema change.

---

## Plan-challenge ruling — Sentry M1 Q7 failure-log allowlist (2026-10-25)

### Question

[from Conductor] Rule whether Q7 failure logs must be limited to outbox row id, exception type and stable error category, prohibiting exception objects and raw Message, ToString, stack or inner-exception text, payload-derived values and sensitive configuration (Sentry Medium finding at `specs/DEV-420/spec.md:108` and `docs/adr/0028-confirmed-enrichment-outbox-relay.md:27`).

### Patron ruling

[from Patron] Ruling: Approved — refine Q7 failure logging so structured relay/cleanup failure logs are restricted to an explicit allowlist of outbox row id (when row-scoped), exception type name, and a stable internal error category, strictly prohibiting passing the exception object to the logger or logging raw `Message`, `ToString()`, stack trace, inner-exception text, payload-derived values, or sensitive connection/configuration details.
- `findings-DEV-420-Sentry:3-7`, `specs/DEV-420/spec.md:108`, `docs/adr/0028-confirmed-enrichment-outbox-relay.md:27`, and `specs/DEV-420/brief.md:23,137` (`specs/DEV-420/CONCLUSIONS.md:114,118,182`): Sentry identified a verified in-scope Medium safety-policy gap (which blocks plan freeze under the Q11 closing bar) because deserialization, database, and broker exception messages/inner exceptions can embed payload-derived values or connection/configuration secrets if raw exception text or objects are logged.
- Standing charter §2.3 (item 5 — Scrub list & secrets) and `specs/PRODUCT.md` §5 (item 5): enforces the Q7 no-payload/no-secrets logging mandate deterministically via a strict field allowlist rather than relying on unverified exception-message formatting or introducing a redaction dependency.
- `specs/DEV-420/CONCLUSIONS.md:150,218` (`specs/DEV-420/brief.md:145-177`): satisfies the logging safety boundary entirely within `OutboxRelayHostedService.cs` inside the frozen 24-file Q9 delivery boundary without adding any package, Core port, schema change, or new file, with Keel updating `brief.md` and `docs/adr/0028-confirmed-enrichment-outbox-relay.md` before Quill propagates to `spec.md`, `plan.md`, `tasks.md`, and `checklists/requirements.md`.


## Phase A freeze receipt — 2026-10-25 (Keel)

**Grill and plan decisions: FROZEN for Phase A planning.** Read-only speckit-analyze is CLEAN and the consolidated plan-challenge Round 1 is CLOSED/CLEAN (PLAN_CHALLENGE_ADJUDICATION.md:72-104). Five accepted Medium findings are closed; Ledger F2 remains rejected; six Low dispositions are preserved; no Round 2 or open decision is required. See brief.md, Phase A freeze receipt, for the analysis coverage, exact input SHA256 manifest, scope and limitations.

Target: F:/Dev/LamuFlix.worktrees/feature-420-spec, feature/420-spec, HEAD/base 8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3. The eight reviewed artifacts remain untracked working bytes, not HEAD-pinned content. All preceding text is preserved as historical stage content; this append supersedes only its pending-freeze status. No design, Q9 24-file boundary, Q7 policy, task completion mark or round-cap change is made.

No unmet Phase A freeze criterion. ADR-0028 remains Draft; Gate 1 and gate1: provisional remain unset. Phase B entry TB000-TB003 still requires its own evidence. No implementation, tests/gates, commit, PR or tracker action is performed. Phase 2 step 7 remains the later Conductor/Patron/Rigger action; user merge remains separate.


---

## Phase B post-rebase scope ruling — T023 constructor cutover in `EnrichmentOutboxPersistenceTests.cs` and `EfMovieRepositoryTests.cs` (2026-10-26)

### Question

[from Conductor] Under charter section 2.3 item 6 and FR-009, authorize extending the Q9 file boundary to edit `tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs` and `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` to resolve `CS1729` compilation failures caused by T023 removing `IEnrichmentQueue` from `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler`?

### Patron ruling

[from Patron] Ruling: Approved — extend the DEV-420 Q9 delivery boundary from 24 to 26 files by authorizing narrow edits to `tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs` (file 25) and `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` (file 26) solely to update `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler` constructor call sites (and remove unused `IEnrichmentQueue` constructor arguments) to match the T023 cutover signatures while preserving all DEV-329 outbox staging and shared-save persistence assertions unchanged.
- Standing charter §2.3 (item 6) and `specs/DEV-420/CONCLUSIONS.md:36-39,150-152` (`specs/DEV-420/brief.md:21,43,78,158-159,176`): removing `IEnrichmentQueue` from `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler` is explicitly required by the ticket's acceptance criteria and Q2/FR-007 cutover, and resolving the resulting `CS1729` compilation error in existing callers is a forced edit required by the build and `dotnet test` verification gates.
- `recon-DEV-420-2:11,18,23` and `DEV-420:23-35`: verifies that DEV-329 landed on `origin/main` (`329edd18`) after the Phase A base (`8063eed0`) and added/modified `EnrichmentOutboxPersistenceTests.cs` and `EfMovieRepositoryTests.cs` outside the original Q9 list to test handler staging and shared-save persistence.
- `specs/DEV-420/brief.md:43,147,176,250` (`FR-009` and `CONCLUSIONS.md:146,150,218-221`): explicitly reserves cited Patron scope rulings before editing any additional tracked file required by post-base main drift; full `spec.md`, `tasks.md`, and the two test files are not in the four named roots for this ask and were not read.
