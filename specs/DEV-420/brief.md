# DEV-420 — confirmed enrichment outbox relay

Status: grill complete at Q12/12; decisions frozen for downstream drafting, amended during Phase 2 step 3 by the cited post-grill C1 ruling below. This is not plan-challenge clearance, Gate 1 approval, implementation permission, review clearance, or a merge bar.

## Identity and authority

- Target worktree: `F:/Dev/LamuFlix.worktrees/feature-420-spec`.
- Target branch: `feature/420-spec`.
- Verified target HEAD: `8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3`.
- Ticket: DEV-420, size L, parent DEV-100; task note `DEV-420:27-30` is the scope source.
- Prerequisites: DEV-329 staged rows and DEV-419 accepted ADR-0027.
- Approved DEV-329 packet source: `F:/Dev/LamuFlix.worktrees/feature-329-spec`, branch `feature/329-spec`, actual HEAD `d85978f9957a321137626ccb43743c384869bbb7`. Conductor identifies this as PR #256. The earlier expected `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3` is its parent, not the contract-source tip.
- Facts: updated canvas note `recon-DEV-420`, especially lines 8-35 and 42-196. Initial implementation/test-layout facts are preserved in the Q2-Q11 question exchanges in `CONCLUSIONS.md`; later recon replaced the canvas note content. No new source exploration was performed to design this brief.
- Authority: `specs/PRODUCT.md` sections 2, 3 and 5; standing `lamuflix-team-charter`; accepted `docs/adr/0027-transactional-outbox-enrichment.md` in this target worktree.
- Every Patron ruling and its cited basis, with the complete question/recommendation exchange, is append-only in [CONCLUSIONS.md](CONCLUSIONS.md). The summaries below are binding drafting instructions together with the detailed decisions in this brief.

The source packet is an approved design contract, not evidence that DEV-329 implementation has landed. Before Phase B, verify landed DEV-329 source/schema/staging and DEV-419 against current main. Do not implement replacement prerequisite contracts to compensate for drift or an unmerged dependency.

## Ticket delivery and loop discipline

Deliver a single `OutboxRelayHostedService` in Infrastructure, registered from the Api host, which batch-polls durable `outbox_messages`, publishes through the existing confirmed mandatory RabbitMQ path, records processing only after confirmation, backs off on failure, deletes expired processed rows, and exposes relay health/backlog metrics. Remove direct queue enqueue only from the import-folder and request-enrichment handlers. Keep the sweeper as a safety net. Prove behavior with PostgreSQL and RabbitMQ integration tests. Concurrent relays on multiple Api instances are excluded by the ticket.

**Closing bar:** verified in-scope Critical, High and Medium findings block plan freeze and later `$code-review`/`$ship-review` clearance. Record Low observations without requiring fix commits. Do not demote accepted blockers to manufacture clearance. Speculative abstraction or other out-of-scope work follows the charter follow-up policy: Patron decides, Rigger records; check existing tickets first; eligible Critical/High broken behavior or user-requested work may receive a ticket, otherwise record `noted, no ticket`.

**Frozen scope:** unchanged DEV-329 schema/staging contracts; one Api-host Infrastructure relay; bounded ordered confirmed mandatory publication and per-row markers; failure backoff and shutdown; processed-only retention; readiness/backlog metrics; two-handler direct-enqueue cutover; Q8 acceptance proofs and the exact Q9 24-file production/test boundary below, including only the six Q7 metric-name constants in `TelemetryConstants.cs` under the post-grill C1 ruling; anything else is a follow-up issue, not a finding in this round.

**Round cap:** two review/fix rounds for plan challenge and two for later code/ship review. The grill separately ends at 12 questions, all answered. After a cap, unresolved blockers halt for Conductor disposition; no third immutable review may repair a receipt or publication defect. Each review round requires the full Sentry/Risk, Ledger/Standards and Compass/Spec reports on the same exact target HEAD; a missing axis blocks. Send Quill one numbered correction list per round. Any decision change must enter this brief before an artifact is changed to reflect it.

No owner structural escalation was raised: Patron Q12 explicitly found neither a ticket-delivery change nor a constitution departure. No taste ruling was made, so no `ASSUMPTIONS.md` is required. Gate 1 remains a later, separate owner-controlled step. No merge is authorized.

## Every grill answer

| Question | Patron answer and implication | Basis recorded in the exchange |
|---|---|---|
| Q1 | `needs recon`: approved DEV-329 schema, port/adapter and staging semantics are prerequisites; never invent substitutes. Evidence subsequently supplied and accepted at Q2/Q12; the initial pause is resolved for planning. | Task note 27-29; initial recon absence findings; ADR-0027; charter 2.3. |
| Q2 | Approved unchanged DEV-329 contract adoption, landed-dependency/pickup checks, and cutover only of the two named handlers. Preserve Stage/shared-save atomicity and errors; leave other callers and sweeper behavior intact. | Updated recon 42-196; task note 27-30; ADR-0027 9,15-17,22-23. |
| Q3 | Approved Infrastructure-only single non-overlapping worker, fresh scoped DB reads, bounded `(created_at,id)` ordering, typed Web JSON deserialization, existing confirmed mandatory publisher and individually committed post-confirm markers. No new relay port, layer, schema or dependency. | Updated recon 44-53,95-144,179-194; ADR-0027; PRODUCT 2,5. |
| Q4 | Approved abort-on-first-failure, preserved committed prefix, fresh-scope capped retries and TimeProvider delays. Uncertainty never licenses marking. Shutdown exits cleanly. Poison rows block progress; no skip, deletion, quarantine or retry columns. | ADR-0027 15-17; task backoff requirement; approved four-column schema; TimeProvider rule. |
| Q5 | Approved the exact options/defaults/bounds and startup validation below, immediate start, deterministic poll/backoff and linked publish timeout. No new toggle. | Task note 27-30; ADR-0027 15,21-22; TimeProvider rule; charter 2.3. |
| Q6 | Approved independent due cleanup inside the same worker, separate scope, bounded processed-at-ordered deletion strictly before cutoff, no schema changes, and errors isolated from publication. | Task retention requirement; ADR-0027 22-23; updated recon 44-53,188-194. |
| Q7 | Approved Ready-tagged `outbox-relay` check on existing readiness surface, cached thread-safe state, health precedence below, six low-cardinality instruments, no callback DB work or sensitive logging. | Task health/backlog requirement; approved timestamp fields; initial health/package recon; charter 2.3. |
| Q8 | Approved real Postgres/RabbitMQ end-to-end proofs, real-DB controlled publisher faults, deterministic TimeProvider tests, host composition and health/metrics tests, using existing packages/patterns. Q9/Q12 require reusing available time-test support without a new package. | Task integration requirement; ADR-0027; Testcontainers and testing rules. |
| Q9 | Initially approved the exact 23-file delivery boundary, narrow required host/test rewrites, existing test support reuse, and a cited ruling before any additional file edit. The post-grill C1 ruling below revises the current boundary to 24 files solely for the six centralized Q7 metric-name constants. | Ticket-required host registration/cutover; charter 2.3 item 6; PRODUCT 5; CONCLUSIONS.md:218-221. |
| Q10 | Approved dependency-first seven-step task ordering and later Phase B gate expectations below. This ask runs no gates and drafts only conclusions/brief. | Workspace boundary; ticket prerequisites; harness gate and mutation rules. |
| Q11 | Approved Critical/High/Medium closing bar, Q2-Q10 frozen scope, originally 23-file boundary (now 24 under post-grill C1), two-round review caps and complete three-axis evidence. | Charter follow-up/review policy; PRODUCT; ticket; ADR-0027; CONCLUSIONS.md:218-221. |
| Q12 | Confirmed shared understanding, canonical vocabulary, at-least-once/per-batch ordering, withholding invalid backlog measurements, no root glossary/constitution changes, and closure now at 12/12 with no structural owner escalation. | Task 27-30; ADR-0027; updated recon 42-196; charter; PRODUCT. |

## Prerequisite contracts and vocabulary

Consume DEV-329's `outbox_messages` unchanged:

| Column | Approved contract |
|---|---|
| `id` | bigint generated identity primary key |
| `payload` | non-null `bytea`, lossless immutable UTF-8 Web JSON bytes |
| `created_at` | non-null timestamptz, assigned once at Stage using injected TimeProvider |
| `processed_at` | nullable timestamptz, initially null |

The only approved index is `(created_at,id) WHERE processed_at IS NULL`. No FK, uniqueness, retry/claim/error/type/version metadata or other table delta is introduced here.

`LamuFlix.Core.Ports.IEnrichmentOutbox` has `void Stage(EnrichmentRequested request)`. It is synchronous tracking only: no token, save, commit, broker call or retry. Scoped `EfEnrichmentOutbox` shares the movie repository's `LamuFlixDbContext`, serializes `EnrichmentRequested(MovieId MovieId, int Attempt)` exactly once with adapter-local `JsonSerializerDefaults.Web`, and tracks `OutboxMessageRecord` (`Id`, `Payload`, `CreatedAt`, `ProcessedAt`). The two handlers Stage before one shared save on the approved `EfMovieRepository` path A. No metadata-retry paths B/C are brought into scope.

| Canonical feature-local term | Meaning |
|---|---|
| staged | Tracked in memory before the shared save |
| durable | Committed in PostgreSQL with the movie write |
| unprocessed | `ProcessedAt IS NULL` |
| confirmed | Broker acceptance settled through the existing confirmed mandatory publish path |
| processed | Post-confirm marker durably committed in PostgreSQL |
| expired processed | `ProcessedAt` strictly older than the retention cutoff |

Use these terms in Quill's artifacts. ADR-0027's informal wording must not collapse staged, durable and unprocessed. This feature-local vocabulary needs no root `CONTEXT.md` edit. No new ADR is written on this ask; the later L-ticket ADR must preserve ADR-0027's accepted guarantees and this vocabulary. Historical constitution wording stays with the separate follow-up named in ADR-0027.

## Approach and behavior

### Handler cutover

Remove only the `IEnrichmentQueue` dependency and direct `EnqueueAsync` invocation from `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler`. Preserve Stage, the shared save, movie/outbox both-or-neither atomicity and failure propagation. A successful durable save succeeds regardless of current broker availability. Stage/serialization/save exceptions and cancellation still propagate; uncertain save completion is not evidence of rollback. Dispose failed scopes without replay. Do not introduce handler retries or a transaction coordinator.

Keep `MatchMovieImdbId`, `ProcessEnrichment`, `RequeueStrandedMovies`, `SweepMissingMovieFeatures` and the sweeper's behavior outside this cutover. Public routes, DTO fields and status-code definitions are not changed.

### Relay pass

Use one non-overlapping `OutboxRelayHostedService` in the existing Infrastructure Enrichment folder. `AddLamuFlixOutboxRelay` registers it from Api `Program.cs`, together with its options, state, metrics and readiness check. One Api instance owns this relay operationally; there is no multi-instance coordination protocol.

For each pass, create a fresh DI scope/DbContext and materialize at most `BatchSize` durable unprocessed records ordered by `(CreatedAt,Id)`. Do not hold a database transaction or connection across broker waits. Deserialize each stored payload into the existing `EnrichmentRequested` type with the approved Web JSON contract; preserve MovieId and Attempt and prove semantic wire parity. Publish sequentially through the existing `IEnrichmentQueue` confirmed mandatory path. Set ProcessedAt from injected TimeProvider only after the confirm settles, and save that individual marker before proceeding to the next record.

No batch-wide marking, additional Core relay port, generic persistence abstraction, publisher rewrite, Stage mutation or new schema is allowed. Query ordering is per batch, not a global FIFO guarantee. At-least-once delivery is deliberate: a broker-confirmed publish followed by failed/uncertain marker commit may be sent again; existing claims absorb duplicate work. A persisted marker prevents normal republishing on subsequent passes/restart.

### Failure and shutdown

The first payload, publish/confirm, marker-save or DB failure aborts the pass and disposes its scope. Preserve already committed markers; never mark an uncertain row. Retry from a fresh ordered query after capped exponential delay. Nack, mandatory return, timeout, connection failure and cancellation before confirmation all leave work unprocessed. A marker commit with uncertain completion is resolved by a later fresh DB read, not by assuming rollback or reusing tracked state.

An invalid oldest payload blocks later progress and surfaces in health/backlog telemetry. There is no silent skipping, row deletion, quarantine, dead lettering or retry metadata in this ticket. Host-shutdown cancellation exits without a synthetic failed pass or retry delay. Distinguish shutdown from publish timeout.

### Options and scheduling

Bind Infrastructure `OutboxRelayOptions` under `OutboxRelay`; validate at startup. These are Patron-approved operational defaults, not measurements or changes to harness gate thresholds.

| Option | Default | Validation |
|---|---|---|
| `BatchSize` | 100 | 1 through 1000 |
| `PollInterval` | 1 second | positive |
| `PublishTimeout` | 30 seconds | positive |
| `FailureBackoffMin` | 1 second | positive |
| `FailureBackoffMax` | 60 seconds | at least FailureBackoffMin |
| `ProcessedRetention` | 7 days | positive |
| `CleanupInterval` | 1 hour | positive |
| `CleanupBatchSize` | 1000 | 1 through 10000 |
| `BacklogAgeWarning` | 5 minutes | positive |

Start polling immediately. After every successful pass, including full/empty batches, delay PollInterval. On consecutive failures, delay `min * 2^(failures - 1)`, saturating at max without overflow. Reset that sequence only after a successful pass, including a successful empty poll. No jitter/randomness or new enable/disable flag is added. Use injected TimeProvider for all timestamps, delays and timeouts. Bound each row publish with linked host-shutdown and PublishTimeout cancellation; marker saving uses the host token, not the expired publish token.

### Retention

Cleanup runs within the same non-overlapping worker on its own due schedule: first due at startup, then each CleanupInterval, including when publishing failed. Use a separate fresh DB scope. Per due run, select at most CleanupBatchSize records ordered by `(ProcessedAt,Id)` where `ProcessedAt IS NOT NULL AND ProcessedAt < now - ProcessedRetention`; recheck this predicate during deletion. Never delete unprocessed rows or filter retention by CreatedAt. Equality at the cutoff is retained.

Cleanup failure is logged and observable but does not undo markers or stop later publication. Retry cleanup at its next interval, not in a tight loop. Large eligible backlogs drain across scheduled bounded runs, so retention age is an eligibility threshold rather than a hard maximum storage age. No extra worker, table, index or migration is added.

### Health, metrics and logging

Register `OutboxRelayHealthCheck` as `outbox-relay` with the existing Ready tag and existing readiness endpoint/status mapping. Do not add an endpoint, DTO, response shape, status-code mapping or liveness change.

Maintain a thread-safe `OutboxRelayState` snapshot with worker lifecycle, latest pass outcome, latest successful sample, pending count, oldest pending CreatedAt and cleanup outcome. After successful publication passes, query backlog in a fresh scope; a sampling failure makes the pass fail. Health and metric callbacks do no database I/O. Compute oldest pending age from the sampled timestamp with TimeProvider. Withhold backlog gauge measurements for invalid/failed samples; never expose stale data as current or replace a failed query with zero. Legitimate counter history remains intact.

Health precedence: Unhealthy before the first successful sample, after failed pass/sample or on unexpected worker stop; otherwise Degraded if oldest pending age is at least BacklogAgeWarning or the latest cleanup failed; otherwise Healthy. A successful empty sample has count/age zero. A later successful pass or cleanup clears its respective fault. Retain the existing HTTP mapping for Degraded.

| Instrument | Meaning |
|---|---|
| `lamuflix.outbox.pending` | Sampled unprocessed row count |
| `lamuflix.outbox.oldest_pending_age` | Age of oldest unprocessed row, seconds |
| `lamuflix.outbox.published` | Confirmed-and-durably-marked row counter |
| `lamuflix.outbox.failures` | Failed relay-pass counter |
| `lamuflix.outbox.retention.deleted` | Deleted processed-row counter |
| `lamuflix.outbox.retention.failures` | Failed cleanup-run counter |

Keep instrumentation in Infrastructure `OutboxRelayMetrics` using existing telemetry dependencies. Define the six approved metric names above as `const` members in existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` and consume those constants from `OutboxRelayMetrics`; do not duplicate telemetry-name literals locally. No MovieId, payload or exception-message metric labels. Structured relay/cleanup failure logs allow only outbox row id (when row-scoped), exception type name, and a stable internal error category. Never pass an exception object to the logger or emit raw Message, ToString(), stack trace, inner-exception text, payload-derived values, or sensitive connection/configuration details (Patron plan-challenge Q7 ruling, CONCLUSIONS.md:233-236).

### Post-grill C1 decision — centralized metric names (2026-10-25)

Patron approved `TelemetryConstants.cs` as Q9 delivery file 24 solely for those six Q7 metric-name constants, consumed by Infrastructure `OutboxRelayMetrics` (`CONCLUSIONS.md:218-221`). Recon `recon-DEV-420:230-303` identifies the existing central definition, confirms the six names are absent, and records its omission from the original boundary. Constitution Principle VI (`.specify/memory/constitution.md:258-262`) requires centralized telemetry names. This narrow file authorization under PRODUCT section 5 item 6 preserves ticket delivery and constitution compliance; it introduces no Core port, abstraction, dependency, schema change, or owner structural escalation. The original Q9/Q11 exchanges remain historical records; this cited ruling supersedes their file count only. The grill remains closed at Q12/12 with unchanged review caps.

Quill must propagate this settled decision to spec, plan, tasks, and checklist after this brief update. Schedule the six constants before dependent `OutboxRelayMetrics` implementation and reconcile task dependencies/parallel markers. No production edit is authorized in the Spec workspace, and no further change to `TelemetryConstants.cs` is approved.

## Exact delivery file set — Q9

This is the approved future production/test boundary, not authorization to implement in the Spec workspace. Paths are relative to the task worktree. Any additional file requires a cited scope ruling before edit.

| # | Path | Action and reason |
|---|---|---|
| 1 | `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayHostedService.cs` | Add bounded relay, retry and retention loop |
| 2 | `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayOptions.cs` | Add bound/validated operational settings |
| 3 | `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayState.cs` | Add thread-safe worker snapshot |
| 4 | `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayMetrics.cs` | Add the six approved instruments |
| 5 | `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayHealthCheck.cs` | Add cached readiness check |
| 6 | `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayServiceCollectionExtensions.cs` | Add worker/options/health composition |
| 7 | `src/LamuFlix.Api/Program.cs` | Register AddLamuFlixOutboxRelay |
| 8 | `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs` | Remove direct queue dependency/call only |
| 9 | `src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs` | Remove direct queue dependency/call only |
| 10 | `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHostedServiceTests.cs` | Add deterministic loop/failure/cleanup proofs |
| 11 | `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHealthCheckTests.cs` | Add state/health/metric proofs |
| 12 | `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayOptionsTests.cs` | Add defaults/bounds/startup-validation proofs |
| 13 | `tests/LamuFlix.IntegrationTests/OutboxRelayTests.cs` | Add real DB/broker and recovery proofs |
| 14 | `tests/LamuFlix.IntegrationTests/OutboxRetentionTests.cs` | Add real DB retention/query proofs |
| 15 | `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` | Adjust cutover expectations |
| 16 | `tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs` | Adjust cutover expectations |
| 17 | `tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs` | Adjust cutover expectations |
| 18 | `tests/LamuFlix.IntegrationTests/EnrichmentEndpointTests.cs` | Adjust cutover expectations |
| 19 | `tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs` | Adjust durable-save/cutover proof |
| 20 | `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` | Prove worker/check registration and sweeper retention |
| 21 | `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` | Prove existing readiness integration |
| 22 | `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs` | Isolate unrelated tests from new worker where required |
| 23 | `tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs` | Isolate unrelated tests from new worker where required |
| 24 | `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` | Add only six Q7 `const` metric-name members consumed by Infrastructure OutboxRelayMetrics; Patron C1 ruling |

The six new plus four existing production files and five new plus nine existing test files total 24. Patron explicitly authorizes narrow host/test changes under charter 2.3 item 6 because ticket-required host wiring/cutover and verification force them, and the six centralized metric-name constants under the post-grill C1 ruling (`CONCLUSIONS.md:218-221`). Real relay tests must run the worker; removing it from a test host cannot count as delivery proof. Reuse available deterministic time-test support without introducing a package.

No publisher/queue-port, Stage adapter, DbContext/entity/configuration, migration, movie-repository, other-caller, project/package, harness threshold/configuration, generated contract/frontend, secrets or LocalPlay edits are authorized by this boundary. No new project, top-level folder, architectural layer or dependency is approved.

The documentation packet under `specs/DEV-420/` and a later dedicated L-ticket ADR are separate Phase A artifacts. This ask writes only this brief and append-only conclusions. Quill owns later spec/plan/tasks drafts; L clarify/checklist, ADR work and plan challenge are subsequent Conductor steps.

## Acceptance and test strategy — Q8

Use existing assertion/mocking conventions and shared Testcontainers fixtures/packages. Query translation and serialization behavior use real PostgreSQL, never a mocked driver. Deterministic time tests use injected TimeProvider support; no wall-clock sleeps or probabilistic timing assertions.

1. Ordinary end-to-end path for each cutover handler: broker unavailable at request time, movie plus outbox commit succeeds without direct queue enqueue, broker returns, relay emits correct MovieId/Attempt through real confirmed mandatory RabbitMQ publication, then and only then commits ProcessedAt. Verify restart does not republish already marked rows and sweeper remains registered.
2. Real DB batch proofs: order by CreatedAt then Id, bounded row count, no processing of already marked rows, and fresh-scope recovery. Prove unchanged staging/rollback atomicity rather than modifying DEV-329.
3. Controlled publisher faults with real DB: pre-confirm marker absent; nack, returned message, timeout and connection failure stay unprocessed; a confirmed publish followed by marker-save failure permits retry; committed prefix survives a mid-batch failure; no later row is silently advanced. Include a focused real-broker mandatory-return test so fault doubles do not replace broker-path evidence.
4. Real DB retention: preserve all unprocessed records, recent/future ProcessedAt and cutoff-equal records; delete only strictly older processed rows in the bounded ordered batch; prove CreatedAt is irrelevant and no schema delta was added.
5. Deterministic unit tests: immediate polling, full/empty success delay, exponential saturation/reset, linked timeout versus shutdown, clean cancellation, no overlapping passes, due cleanup even after publish failure, cleanup failure isolation and next-interval retry, options validation, and poison-row progress/readiness behavior.
6. State/metric and composition proofs: startup/empty/backlog/fault/recovery health transitions, failed-sample gauge withholding, counter semantics and low-cardinality labels, exactly one hosted relay/check, existing endpoint mapping, and retained sweeper. Dedicated relay tests must exercise the worker rather than merely asserting registration.

## Task ordering and gate expectations — Q10

Quill must derive dependency-ordered spec/plan/tasks from this brief, without adding delivery scope:

1. Before Build implementation, verify landed DEV-329 schema/Stage/payload/save contracts and DEV-419 ADR against current main; run pickup consistency/drift analysis and resolve mismatches through the evidence/decision path.
2. Establish options and deterministic state/time tests plus the real DB/broker relay-proof harness using existing support.
3. Implement bounded confirmed per-row publication/markers and real-DB failure recovery.
4. Implement isolated scheduled retention and prove strict cutoff/batch behavior.
5. Add the six centralized Q7 metric-name constants in `TelemetryConstants.cs` before their use by Infrastructure `OutboxRelayMetrics`; add state/metrics/readiness, Api registration and host-composition proofs.
6. Remove the two handlers' direct enqueue; adjust authorized tests/factories; prove broker-down request success and recovery delivery.
7. At the proper Phase B stage, run relevant then full tests and required harness gates against the actual diff, followed by exact-head three-axis review and hosted checks in their separate later phases.

Future analyzer gates are `scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1` and `scripts/run-jetbrains-inspectcode.ps1`, scoped to the actual changed files/base using their supported parameters. Future test/property/security/format/mutation checks follow the current `harness.yml` configuration and supported script flags. Run pre-PR mutation through `scripts/run-mutation.ps1`, never Stryker `--since` from a worktree. Never lower a threshold or hand-edit a generated file. Surviving mutants need meaningful tests; do not hide them by relaxing the gate.

Report actual outcomes: exit 0 is earned pass; scope-empty exit 2 is `SKIPPED (scope-empty)`, disabled gate is `SKIP`, mutation configured exclusion is N/A, and an applicable gate that cannot execute is `Could not run`. Never fold skips or unrun checks into PASS or substitute a plain build for required analyzers. Do not invent unrelated frontend or architecture checks merely to fill a checklist. Verification is for the exact reviewed head, with all three axis reports available; local gates, hosted checks, owner approval and merge remain distinct claims.

## Closure and handoff

Patron Q12 explicitly confirmed the shared understanding and authorized closing this brief at 12/12. There are no unanswered grill questions and no structural owner checkbox from this grill. The completed exchange and rationale are preserved in CONCLUSIONS.md; the initial Q1 pause is historical and superseded by the updated recon and subsequent approvals.

Only the grill and brief are complete. Quill drafting, L clarify/checklist, dedicated ADR, read-only consistency analysis, plan challenge/freeze and owner Gate 1 remain for later Conductor steps. The adopted vocabulary and existing ADR-0027 are ready to feed drafting; the later ADR must be completed before claiming the full L-ticket documentation stage. No implementation, tests, analyzers, gates, PR publication, tracker mutation or merge was performed on this ask.

Phase 2 step 3 amendment receipt (2026-10-25): the post-grill C1 ruling is now recorded in this brief before downstream corrections. Current Q9 scope is 24 files; only `brief.md` is edited on this Keel ask. Existing Quill drafts require read-only consistency review against this amendment. This amendment is not plan freeze, Gate 1, ADR completion, or Build clearance.

## Plan-challenge Q7 amendment — failure-log allowlist (2026-10-25)

Patron approved the strict relay/cleanup failure-log allowlist recorded in `CONCLUSIONS.md:225-236`: outbox row id only when row-scoped, exception type name, and stable internal error category only. Exception objects, raw Message/ToString()/stack trace/inner-exception text, payload-derived values and sensitive connection/configuration details are prohibited. This supersedes the broader exception-identification wording in the original Q7 proposal; there is no sanitized-exception escape hatch or redaction dependency.

Basis: Patron's cited ruling at `CONCLUSIONS.md:233-236`, Sentry's Medium finding (`findings-DEV-420-Sentry:3-7`), the Q7 no-payload/no-secrets contract and charter scrub/secrets rule. Apply inside the existing `OutboxRelayHostedService.cs` boundary; the 24-file Q9 set, packages, Core ports, schema and public contracts are unchanged. No new decision or owner structural escalation is introduced on this resumption.

Record order: this brief amendment precedes Keel's matching ADR-0028 logging update and Quill's spec/plan/tasks/checklist correction. Quill must propagate the exact policy and assign a logging proof in existing `OutboxRelayHostedServiceTests.cs`: payload/secret sentinels in raw and inner exceptions must not reach captured structured logs or exception-object arguments; rowless failures must not invent a row id. This is acceptance-proof ownership for the approved Q7 boundary, not a new file or production implementation.

The previously held consolidated five-item Round 1 fix list is now final in `PLAN_CHALLENGE_ADJUDICATION.md`. Five accepted Medium findings remain open until corrected and verified. The grill remains closed at 12/12; this resumption does not consume a second challenge round or reset the two-round cap. No freeze, Gate 1, tests or gates are claimed.
## Phase A freeze receipt — 2026-10-25 (Keel)

**Current status: FROZEN for Phase A planning.** This appended receipt supersedes only the historical pending-freeze statements above. Authority: Conductor freeze ask; task-chain section 0 and task-pipeline Phase 2 steps 3, 5 and 6; Q11/Q12, post-grill C1 and plan-challenge Q7 rulings in CONCLUSIONS.md. No design decision changes on this ask.

### Exact target and working artifact state

Worktree: F:/Dev/LamuFlix.worktrees/feature-420-spec. Branch: feature/420-spec. HEAD and Phase A base: 8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3. The committed base...HEAD diff is empty. All eight paths in the input manifest below are untracked working artifacts; no tracked modifications or staged changes were present. Freeze applies to these reviewed working bytes plus the append-only receipts, not to files contained in HEAD. No fetch or claim about current main/prerequisite landing is made.

### Final read-only speckit-analyze

Prerequisite command: .specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks; exit 0; FEATURE_DIR is this worktree/specs/DEV-420; AVAILABLE_DOCS includes tasks.md. No before_analyze or after_analyze hooks are registered in the read extensions.yml.

Verdict: CLEAN. Findings table: no findings. Constitution 1.4.0 checked; no new constitution alignment issue, duplication, ambiguity, uncovered requirement or unmapped task. The post-grill C1 and Q7 rulings are propagated consistently. Draft ADR-0028 records the settled relay decisions. L clarification session and requirements checklist are present; no ui design artifact is required.

| Requirement | Task coverage |
|---|---|
| FR-001 | T002, T004-T006, T009-T010, T015, T018 |
| FR-002 | T003, T007, T009-T010 |
| FR-003 | T007-T010 |
| FR-004 | T001-T002, T004, T006, T009 |
| FR-005 | T011-T013 |
| FR-006 | T005, T008, T014-T018 including T015a |
| FR-007 | T020-T024 |
| FR-008 | TB001, TB003; retained constraints on all code tasks |
| FR-009 | TB001, T015a, T024, T026-T027; exact task-path boundary |
| FR-010 | T001-T003, T007-T008, T011-T012, T014-T015, T020-T022 |
| FR-011 | TB000-TB003 and Phase 1-6 dependencies; T015a before T016 |
| FR-012 | TB002, T025-T027 |
| FR-013 | T027 checkpoint and existing Round 1 adjudication |
| FR-014 | TB001, T024, T026-T027 and task boundary/exclusions |

| Success criterion | Proof ownership |
|---|---|
| SC-001 | T007, T009-T010 |
| SC-002 | T007-T010 |
| SC-003 | T007-T010 |
| SC-004 | T011-T013 |
| SC-005 | T008, T014-T018 including T015a |
| SC-006 | T020-T024 |
| SC-007 | TB001, T015a, T024-T027 |

Coverage: 14/14 FR and 7/7 proof-based SC (100 percent each); 28 delivery/verification task IDs (T001-T027 plus T015a) and four Phase B entry IDs (TB000-TB003), all mapped. Ambiguity 0, duplication 0, Critical 0, High 0, accepted Medium open 0. These are documentation coverage judgments, not executable test or gate results.

### Challenge closure and frozen decisions

All three axis reports were read: findings-DEV-420-Sentry, findings-DEV-420-Ledger and findings-DEV-420-Compass. PLAN_CHALLENGE_ADJUDICATION.md lines 72-104 closes the single consolidated Round 1 correction list CLEAN: five accepted Medium findings CLOSED; Ledger F2 remains rejected as speculative; all six Low dispositions remain recorded without required fixes. No severity is demoted and no Round 2 is needed. The two-round cap remains unchanged.

The Q1 historical evidence pause is resolved for planning by Q2/Q12 and supplied recon. Q1-Q12, C1 and Q7 logging-policy decisions are settled; no open decision, needs-recon item or structural owner checkbox remains for this freeze. Unmet Phase A freeze criteria: none.

Freeze unchanged: the exact 24-file future production/test boundary; adopted DEV-329 contracts; single Api relay; bounded confirmed publication and post-confirm markers; failure/backoff/shutdown; isolated retention; cached readiness/six centralized metrics/strict log allowlist; two-handler cutover; all Q8 proofs; seven-step ordering and later gates. Phase B still requires merged spec/Gate 1, verified landed DEV-329 and DEV-419, pickup drift clearance and any required Gauge baseline evidence (TB000-TB003). This freeze supplies none of those later receipts.

### Input SHA256 manifest before receipt appends

| Path | SHA256 | Bytes |
|---|---|---|
| `specs/DEV-420/brief.md` | `323BB3DC1EE37F594583E907D4B2C3C968683776519AE5CF04031516CE3B6444` | 31272 |
| `specs/DEV-420/CONCLUSIONS.md` | `6DF718332B84DA2F4B0BD79C6269449D69F7A29652FDBDA7E0E87DE7BDA6F132` | 47499 |
| `specs/DEV-420/spec.md` | `CE1EE68CA078C83D2A4DA89FA71352480714D83A5D06D662B8A50B7D414B3DE6` | 29050 |
| `specs/DEV-420/plan.md` | `EEEB7F39DBD5D4519231B9299FAE7DC70ABADB6F65B9FDD464EE2CFBD11ED8AF` | 18167 |
| `specs/DEV-420/tasks.md` | `50E54056454504503DD72548598820738C73370D4305C9A7333985B3394EC852` | 20377 |
| `specs/DEV-420/checklists/requirements.md` | `9FAF1CCB099C0AF37147847DF768D686346F5FE1366D0422E85C021BDDBFCDF6` | 12658 |
| `specs/DEV-420/PLAN_CHALLENGE_ADJUDICATION.md` | `E516269FF2EE0504C0EF260D0302A11E005CE7859BA604762F7BC3F4CD833D8A` | 18476 |
| `docs/adr/0028-confirmed-enrichment-outbox-relay.md` | `F6D101F811599AF339B004CCBCFA95105387F35784B056DFF82FD81ABE0A4B4F` | 9431 |

### Write boundary and next step

Only brief.md, CONCLUSIONS.md, plan.md and tasks.md receive appended freeze receipts. Preserve every pre-existing byte; spec.md, requirements checklist, adjudication and ADR-0028 remain byte-identical. Read back each append and verify prefix preservation and unchanged-input hashes. Final whole-file hashes are reported separately in the DEV-420 task note to avoid a self-referential hash.

ADR-0028 remains Draft. Gate 1 and gate1: provisional are not set. No commit, staging, PR, tracker mutation, production edits, tests, gates, acceptance or merge is performed. Next action belongs to Conductor: separately assign Phase 2 step 7 Patron ruling and Rigger publication workflow. The user owns spec PR merge.
