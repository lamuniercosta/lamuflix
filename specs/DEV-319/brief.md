# DEV-319 - Frozen grill brief

## Identity and authority

Worktree: F:/Dev/LamuFlix.worktrees/feature-319-spec. Branch: feature/319-spec.
Phase A base: a62fbe869df4d1fb2a738417b06660bf247e96d9.
Recon addendum HEAD: 21be958161abde3f6f61cca032b911a5b388a3b7.
Patron ruling commit: c63ef06df8408a3ce7e8939abba562b2af11ed03.
Ticket: DEV-319, size M, no UI. Authority: ticket as recorded in recon-DEV-319:24-39; standing S1-S2 and Patron P1-P8/Q1-Q4 in CONCLUSIONS.md; task-pipeline and lamuflix-team-charter. Facts come exclusively from recon-DEV-319. Four of twelve grill questions used. No structural block or owner checkbox remains. Existing .specify/feature.json modification is the intake spec pin and must be preserved.

## Closing terms and frozen scope

Critical and High findings and unmet ticket acceptance criteria block closure. Dispose every lower finding explicitly; additional work follows the charter follow-up policy. Review cap: two rounds. Remediation cap: two fix commits per round. A third review round belongs to user review on the PR, not an automatic repeat. Grill cap: twelve questions; later recommendations require recorded [assumed] handling.

Scope: exactly three ticket metrics and their literal constants, emission and tests required by AC1, preservation and verification of existing named spans and attributes, and Accepted docs/adr/ADR-0008.md recording polling in P1 with SignalR deferred; anything else is a follow-up issue, not a finding in this round.

No new dependencies, PackageReferences, projects, layers, telemetry ports, schema, public API shape, LocalPlay execution, secrets or Process.Start changes. No polling implementation, SignalR hub/package, frontend work, additional instruments or additional metric tags. No change to lamuflix.handler.outcome. Any newly needed package or port returns to Patron. A ticket contradiction or necessary constitution departure stays blocked: structural until the user answers its Gate 1 PR checkbox.

## Every grill answer

S1: Meter identity is TelemetryConstants.ActivitySourceName (LamuFlix); no second identity constant. S2: exactly the three ruled instruments.
P1: Metric names, tag keys and enriched|not_found|failed literals are frozen. Changing those requires structural escalation.
P2: Enrichment.Enqueue, Enrichment.Process, Metadata.Lookup, lamuflix.movie.id, messaging.rabbitmq.delivery_count and error.type already exist; preserve and verify.
P3: Emission edits outside TelemetryConstants are forced by AC1; handler placement satisfies this without consumer emission edits.
P4: No new package/reference; BCL metrics only. P5: Existing projects; no new port/layer. Q3 approves the concrete static holder below.
P6: docs/adr/ADR-0008.md, status Accepted, polling for enrichment status in P1 and SignalR deferred; ADR only.
P7: Schema/API/LocalPlay/secrets/Process.Start untouched. P8: Existing handler outcome family untouched.
Q1: Actual lookup attempts determine enrichment outcomes; no-op claims and requested cancellation do not increment. Q2: Imports count persisted movies, including subsequent enqueue failure. Q3: Existing span semantics and static Core holder approved; TimeProvider is a forced AC1 edit if needed. Q4: Closing terms, caps, property-test opt-out and gate expectations below ratified. All four answers are recorded by Patron in CONCLUSIONS.md, with basis; no structural block.

## K1 - Outcome emission

Do not translate consumer Completed(bool Claimed) into enriched: it conflates provider found/not-found and refused claims (recon:213-235). Emit in ProcessEnrichmentCommandHandler where the actual lookup result/classified exception is known.

- Found: Add(1), outcome=enriched.
- NotFound: Add(1), outcome=not_found.
- Provider Failed or classified lookup exception: Add(1), outcome=failed and failure_category=actual Category.Code.
- Refused claim, including already-terminal/absent/unexpired-lease deliveries: no outcome measurement.
- Requested cancellation: no outcome measurement.

Count one result per resolved lookup attempt, including retries; record at result classification independently of later persistence/settlement. Never count twice through both result and exception paths. Persistence, republish, malformed-body and other non-lookup errors retain their existing error traces but do not fabricate lookup outcomes/categories. No ProcessEnrichmentOutcome contract change and no consumer production edit. Counter means lookup results, not broker acknowledgements or persisted terminal status.

## K2 - Names and trace compatibility

Keep TelemetryConstants.EnrichmentOutcome = enrichment.outcome and consumer completed/skipped span values. These describe delivery/claim processing; the metric outcome describes lookup results. Keep all existing named spans, kinds, attributes and parent/link behavior.

Add distinct literal constants in TelemetryConstants: EnrichmentDurationMetricName = lamuflix.enrichment.duration; EnrichmentOutcomeMetricName = lamuflix.enrichment.outcome; ImportCountMetricName = lamuflix.import.count; ProviderTagName = provider; OutcomeTagName = outcome; FailureCategoryTagName = failure_category; EnrichedOutcome = enriched; NotFoundOutcome = not_found; FailedOutcome = failed; OmdbProviderName = omdb. Reuse the closed domain Category.Code values; no second category vocabulary. Tests assert ticket literals independently rather than proving a constant equals itself.

## K3 - Duration

Histogram<double>, unit s, tag provider=omdb only. Measure the entire registered Infrastructure OmdbMetadataProvider lookup from entry to exit, including HTTP resilience retries, mapping and failure handling, with injected TimeProvider.GetTimestamp/GetElapsedTime. Record elapsed TotalSeconds exactly once in finally, including failed and cancelled calls. No DateTime clock, timing sleeps or measurement of broker queue/lease/settlement. Existing TimeProvider is documented at recon:278-294; any necessary BCL constructor wiring is AC1-forced. Never derive provider identity from URL, credentials or class full name.

## K4 - Imports and failure dimension

Counter<long>, no tags. ImportMovieFolderCommandHandler does Add(1) immediately after successful SaveChangesAsync and before EnqueueAsync. Scan/add/save/duplicate failures count zero; enqueue failure after save counts one because the movie persists (recon:298-307). Preserve exception, order and API behavior.

Enrichment outcome Counter<long>, tags outcome plus failure_category only for failed. Allowed category values: provider_unavailable, rate_limited, invalid_response, unknown, from the actual EnrichmentFailureCategory.Code (recon:249-274). enriched and not_found omit failure_category entirely. No empty/null placeholder, exception names, failure reasons, attempt counts, movie identifiers or provider tag on this counter.

## K5 - Instrument creation and ownership

Create src/LamuFlix.Core/Pipeline/EnrichmentMetrics.cs as a concrete static BCL instrument holder in the existing project/folder. One process-lifetime static Meter named TelemetryConstants.ActivitySourceName, holding exactly the duration histogram and two counters. Expose concrete instruments for the Core handlers and Infrastructure adapter. No interface, wrapper service, DI registration, IMeterFactory requirement, OpenTelemetry type or new package. TelemetryConstants remains a string-constant catalogue. Do not dispose the shared meter per request or host. Existing ServiceDefaults AddMeter subscription remains the composition path (recon:100,309-320).

## File map for Phase B

Production files:
- src/LamuFlix.Core/Pipeline/TelemetryConstants.cs: add exact constants.
- src/LamuFlix.Core/Pipeline/EnrichmentMetrics.cs: new static instrument holder.
- src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs: emit lookup outcomes without altering domain behavior.
- src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs: increment after successful save.
- src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs: record whole-lookup duration using its TimeProvider.

Test files:
- tests/LamuFlix.UnitTests/Pipeline/EnrichmentMetricsTests.cs: new instrument contract tests and a disposable capture helper local to the unit-test project.
- tests/LamuFlix.UnitTests/Pipeline/MetricsCollection.cs: new DisableParallelization=true collection; apply to all unit tests observing application instruments.
- tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentMetricsTests.cs: new focused handler metric tests.
- tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs: extend existing success/failure tests and apply isolation collection.
- tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs: extend actual lookup/retry/refused-claim/cancellation coverage; preserve existing RabbitMq collection.
- tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs and MetadataProviderTelemetryTests.cs: extend duration and trace verification; preserve MetadataProvider nonparallel collection.
- tests/LamuFlix.IntegrationTests/ServiceDefaultsTelemetryTests.cs: prove actual instrument export through existing host composition/nonparallel collection.

Keep integration capture helpers nested in their relevant test files; no new shared production abstraction. If the existing unit handler fixture cannot support the specified new test file, Quill requests a decision rather than changing this map silently. Existing ArchitectureTests prove the Core no-OpenTelemetry boundary; run them later without production/test architecture edits. No csproj, package, ServiceDefaults registration, consumer production, endpoint or generated contract changes planned.

Planning artifacts: this brief, CONCLUSIONS.md, Quill-owned spec.md/plan.md/tasks.md and any Spec Kit support documents in specs/DEV-319; docs/adr/ADR-0008.md. The M ticket ADR text is a settled deliverable for Quill to draft from P6, not a new architecture decision. It records polling versus push trade-offs and the constitution-backed P1 choice, without inventing polling interval, endpoint or future implementation commitments.

## K6 - Verification strategy

Use test-only disposable MeterListener capture filtered to exact application meter/instrument names, with thread-safe measurements/tags and no production-only test dimensions. Isolate all observers with DisableParallelization=true collections; existing integration collections provide this (recon:322-342). Keep static instrument lifetime in mind: listeners must observe existing published instruments as well as initial creation.

Prove literal names, instrument types, duration unit s and exact tag sets. Exercise Found, NotFound, every failure category, lookup exceptions, repeated retry attempts, refused claims, requested cancellation, and import success/add/save/enqueue failure. Use controlled TimeProvider timestamps for duration; no wall-clock sleeps. Duration remains emitted for cancellation although outcome is absent. Assert no extra outcome on persistence/settlement failure.

Use existing real Postgres/RabbitMQ integration behavior for claim/refused-claim and retry/cancellation; real persistence for uniqueness semantics, never mock the database driver. WireMock remains the provider/OTLP test convention. Extend host composition proof to receive actual instrument identities and their dimensions rather than merely emitting telemetry.composition.probe. Preserve named-span, attribute and parent/link assertions. No tests or gates run during this grill.

propertyTests: opt-out - pure instrumentation of existing behavior introduces no new domain invariant. Record this exact opt-out in the task note before Phase B gates. Existing property tests still run; opt-out does not waive applicable failures or mutation testing.

## Ordered task constraints for Quill

1. Draft AC1 metric/span requirements and AC2 Accepted ADR from this brief, with precise examples for no-op, cancellation, retry and enqueue failure.
2. Draft ADR-0008 documentation; constants/instrument catalogue and isolated contract-test harness form the first Phase B code phase.
3. Implement import emission and tests as one bounded phase.
4. Implement handler outcome emission and retry/no-op/cancellation tests as one bounded phase.
5. Implement provider duration and controlled-time tests as one bounded phase.
6. Extend actual-instrument OTLP composition and real integration coverage, retain architecture/trace proofs, then run the pipeline gates in their own final phase.

Every code phase has task IDs, explicit files, checkpoint and stop boundary for Anvil; read-only tasks belong to Wisp. No implementation before the user merges the spec PR at Gate 1. Quill drafts only; Keel subsequently runs read-only speckit-analyze against the brief, followed by three-axis plan challenge and adjudication/freeze. This grill closure is not plan challenge clearance or Gate 1 approval.

## Gate expectations and handoff

Phase A: documentation planning only, no build/test/analyzer/format/mutation execution. Phase B: Gauge runs every applicable task-pipeline gate: Roslyn, complexity, InspectCode (no -All), mutation for changed production C#, property tests, vulnerable packages, format verification, dotnet test and web applicability. All three review axes remain mandatory for this M ticket.

Keep configured thresholds unchanged. PASS requires actual script success; retain SKIPPED (scope-empty), configured-disabled SKIP, mutation N/A, accepted property opt-out and Could not run distinctly. Scope-empty is nonblocking, never PASS; exit 1 and Could not run block. Never use Stryker since in a worktree. Owner merges all PRs; completion stops at awaiting-merge.

Grill closed after Patron Q1-Q4 ratification. Next owner: Quill, via Bernstein, for speckit-specify/plan/tasks. No code, gates, tests, analyzers or format work has occurred.

## Phase A three-axis plan adjudication - 2026-10-10

Reviewed draft identity: feature/319-spec at HEAD 4df0104479d3169c2ae1b8f81449730f71f8891f. All three connected reports are present: Ledger CLEAN (0), Compass CLEAN (0), Sentry R1 MEDIUM and R2 LOW. This is partial adjudication, not plan freeze or Gate 1 clearance. Existing uncommitted draft files and the intake spec pin are preserved. No implementation or gates are authorized here.

### R1 MEDIUM - pending bounded recon; feasibility allegation unproven

Location: tasks.md:46 T006; spec.md:55,60; this brief:54,75,80,90. Retain the real duplicate-import persistence and zero-count requirement. The file-map entry at :75 does not prohibit a real-persistence case in that file; feasibility within that map remains a factual question.

Sentry note:9 infers no UnitTests Postgres support from recon:300,319,337-342. Those citations establish mocked existing import cases, IntegrationTests references and collection examples; they do not establish absence of UnitTests references to Tests.Common or reusable Postgres support. Compass note:6 claims Tests.Common PostgresFixture/ContainerFixture, a Testcontainers dependency and existing EfMovieCatalogSurvivorTests usage at :12,89, but these source facts are not in the supplied recon. Neither absence nor feasibility is verified by the supplied factual record. Findings conflict explicitly remains unresolved; CLEAN on another axis does not override it.

Reject Sentry note:10 proposed substitution as insufficient: recon:304 describes an already-tracked duplicate-ID AddAsync guard, not the database unique LibraryPath constraint rejecting a second imported row at save. That proof plus a mocked generic SaveChangesAsync failure does not prove real duplicate-import uniqueness and absence of an extra measurement. No coverage weakening or new test-file allocation is approved.

Next owner: Bernstein routes the following needs recon questions to Wisp, read-only at the pinned worktree/HEAD, then Keel adjudicates the returned facts:

1. In tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj and tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj, cite exact existing ProjectReference/PackageReference lines establishing or excluding UnitTests access to Tests.Common, Infrastructure persistence types and Testcontainers. Record missing paths as not found; do not add references.
2. Locate only EfMovieCatalogSurvivorTests.cs under tests/LamuFlix.UnitTests and PostgresFixture/ContainerFixture definitions under tests/LamuFlix.Tests.Common. Cite exact declarations, fixture ownership/lifecycle, context/database creation and reset/migration APIs, and existing real persistence usage corresponding to Compass :12,89. Can these existing public APIs be reused without editing any of those files? Supply facts, not a scope ruling.
3. In tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs, the above existing fixture APIs, src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs, and src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs, cite the concrete constructor/setup and unique LibraryPath save-failure path needed to exercise two handler imports against real Postgres with a MeterListener baseline around the rejected import. Identify whether existing references/lifecycle allow this inside only the approved import test file and MetricsCollection.cs, preserving nonparallel observation; list any additional file/reference actually required. Distinguish a tracked duplicate-ID guard from a database uniqueness violation.

If Wisp confirms existing support, Quill clarifies T006 setup/isolation within the current map and Keel disposes R1. If an extra file/reference is required, stop at needs decision for Patron, with evidence; amend this brief only after that ruling. No silent map expansion or product-scope change.

### R2 LOW - accepted bounded drafting fix; Quill action pending

Location: plan.md:21 and tasks.md:73 T011. Basis: this brief K3 (:50), approved duration-test files (:77), nested-helper boundary (:80), controlled timestamps (:88); CONCLUSIONS.md P4/P5 and Q3 (:29-37,101-109). recon:294 describes FixedNow and resilience boundary values, not a controllable monotonic timestamp. It does not prove that the existing probe supplies the required elapsed-time control.

Technical clarification of the settled K3/K6 strategy: duration cases use a test-only BCL TimeProvider subclass nested in MetadataProviderLookupTests.cs and/or MetadataProviderTelemetryTests.cs, with explicitly controlled GetTimestamp and a matching known TimestampFrequency. Keep GetUtcNow deterministic where mapping needs it. Supply this instance to the existing provider constructor in a focused test setup inside those named files; construct the provider/client there as needed rather than editing MetadataProviderProbe.cs. Advance timestamps explicitly at an awaited transport/test boundary for Found, Failed and requested cancellation; assert a known elapsed TotalSeconds value and exactly one duration measurement with only provider=omdb. No real-clock sleep, base Stopwatch timing assertion, fake-time package, shared helper file, probe edit or production seam is approved. Preserve the existing MetadataProvider nonparallel collection and span assertions.

Next owner: Quill updates plan.md:21 and T011/T013 to spell out this nested clock, injection/setup path, matching frequency and controlled advancement for the three paths; report the exact documentation delta to Keel via Bernstein. Keel checks the bounded delta before closing R2. If implementing that setup actually requires a file/package/port outside the settled boundary, return needs recon for the precise obstacle and then needs decision to Patron; do not widen the map. This clarification changes neither frozen product behavior nor the approved file/package map.

Overall: R1 pending recon; R2 accepted with bounded documentation remediation pending. No plan freeze, challenge clearance or Gate 1 clearance until both are closed on cited evidence. No new review round requested.
