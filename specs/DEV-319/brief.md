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
