# DEV-315 Phase 2 decision brief — grill closed, recon handoff pending

Ticket: Implement worker outcome translation for ack, retry-queue republish, and DLQ routing. Size:M, no ui tag.
Worktree: F:/Dev/LamuFlix.worktrees/feature-315-spec; branch: feature/315-spec.
Base per intake/recon: 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc.
Evidence: DEV-315, recon-DEV-315, task-pipeline-2 Phase 2, lamuflix-team-charter-2, specs/PRODUCT.md.

## Status

Q6 AMEND persisted. Patron confirms shared understanding; grill closed at 6/12. Q1-Q6 settle approach, behavioural scope, header contract, proof strategy, loop terms, eight-file map, ordering and gates. Targeted recon lines 100-128 are readable; original synchronization blocker resolved. Handoff remains pending Wisp evidence for the existing publisher test-file identity and harness change newly cited by Patron, plus Rigger safe reconciliation of the excluded config delta. Do not start Quill drafting in this ask. Full exchanges are in CONCLUSIONS.md. Verified HEAD remains 1312a41f85630b49471e5b80291d8d7665cd60fa; 01caa4d is the earlier base.

Q6 resume recon receipt: the targeted command `maestri note read "recon-DEV-315" 99 10` returned `[lines 99-99 of 99]`, with only the existing line 99 appendix and no lines 100-101. This is not an output-budget truncation of the requested missing lines. DEV-315:70-71 records Wisp reporting 101 lines, but that reported receipt does not replace this seat reading the named evidence. Q6 remains unasked, grill remains 5/12, and file scope is unfrozen. Needs recon: make the cited literal signatures and exhaustive construction-site appendix readable in the exact recon-DEV-315 note connected to Keel #2, then resume Q6.

## Settled answer and approach (Q1)

Patron AMEND: retain existing core outcome / transport settlement separation. Completed(Claimed true or false) acknowledges, covering Enriched, NotFound and refused claim. Failed Retry/RetryDelayed publishes to enrichment.retry with core-provided NextAttempt, then acknowledges after successful publish. Failed DeadLetter publishes to enrichment.dead-letter with failure headers, then acknowledges. The ticket lists actions rather than ack-first ordering; publish-before-ack preserves the existing at-least-once interpretation. No new translator or policy abstraction.

Preserve malformed-body and unexpected-exception terminal nack(requeue:false), and stopping-token nack(requeue:true), outside the outcome table. Patron requires real-broker proofs, using Testcontainers as the ticket explicitly specifies. No ticket contradiction or constitution departure identified in Q1.

Basis: DEV-315 Description; recon-DEV-315:22-25,30-32,53-60; PRODUCT.md sections 2-3; charter section 2.3. Recon line 99 now confirms telemetry-only publish headers and the existing Testcontainers fixture/package; the earlier unsupported absence claim is resolved by recon.

## Settled internal header contract (pre-Q2 Patron ruling)

The queue is internal Worker transport; public API care item 4 is not triggered. No owner escalation. Terminal SettleAsync publishes only:

| Header | AMQP type | Value |
|---|---|---|
| x-lamuflix-failure-category | UTF-8 string | actual Category.Code: provider_unavailable, rate_limited, invalid_response, unknown |
| x-lamuflix-failure-reason | UTF-8 string | non_retryable when !IsRetryable; otherwise max_attempts_exhausted for terminal outcomes |
| x-lamuflix-failure-attempt | int32 | current request Attempt |

No raw error text, SafeDescription, provider payload, PII or secrets. Do not write broker-owned x-death/x-first-death-* headers. Preserve telemetry headers. Retry and out-of-table paths get no failure headers. Each header requires a real-broker assertion.

## Settled category propagation (Q2)

Patron ACCEPT: extend existing ProcessEnrichmentOutcome.Failed with actual EnrichmentFailureCategory alongside Decision; ProcessEnrichmentCommandHandler supplies it on Failed returns. Keep EnrichmentFailureDecision and retry policy responsibilities. Consumer emits headers only for the dead-letter disposition; never recompute routing. This is forced by the ticket failure-header AC, adds no layer/schema, and needs no owner escalation.

Basis: DEV-315 Description; recon-DEV-315:53,56,59,69,75,99; pre-Q2 ruling commit 1312a41; charter section 2.3 items 2-3. Exact construction/test-site inventory and RecordEnrichmentFailureCommandHandler exclusion in Patron reply await Wisp recon; do not treat the fresh search as plan evidence.

## Settled publisher extension (Q3)

Patron AMEND: preserve concrete PublishAsync(EnrichmentRequested, string, CancellationToken) and add an overload taking IReadOnlyDictionary<string, object?> additionalHeaders before CancellationToken. Existing overload delegates without additions. IEnrichmentQueue remains unchanged. Only terminal outcome publishes pass the three settled headers. No new interface, type, layer or dependency.

Build a fresh per-publish header dictionary; never mutate caller additions. Publisher owns telemetry keys and rejects collisions with telemetry, x-death or x-first-death-* keys using ArgumentException. Reject null values with ArgumentException. Preserve existing mandatory publishing and tracked confirmations. Prove terminal headers/telemetry, requested/retry absence of failure headers, collision/null rejection and caller-dictionary preservation.

Basis: DEV-315 failure-header AC; recon-DEV-315:99; Q1/Q2; charter section 2.3 item 6. Existing publisher/consumer edits are forced by the AC. No owner escalation or taste assumption.

## Settled real-broker proof strategy (Q4)

Patron ACCEPT: reuse existing Testcontainers RabbitMqFixture with production consumer/topology/publisher and core handler/policy, controlling provider outcomes. No new fixture/dependency or production topology change. Isolate broker state with independent test queue/exchange names or purge before use. Set small MaxAttempts via options and short positive integer-millisecond TTL through config.

| Path | Required per-row proof |
|---|---|
| Enriched / NotFound / refused claim | requested settled, no retry or DLQ; distinguish ready/in-flight state and lifecycle for ack evidence |
| transient / rate_limited below Max | retry ingress independently observed, same MovieId, Attempt+1, intact telemetry, no failure headers; actual broker TTL return to requested/provider invocation |
| non-retryable below Max / retryable at Max / retryable above Max | DLQ with unchanged failed Attempt and all three settled headers; terminal behaviour, no further retry/provider loop |
| Max-1 retry crossing to Max | retry then terminal proves bounded poison handling |
| failed publish / malformed / unexpected exception / cancellation | keep Q1 regression proofs and existing settlement behaviour |

Cases sharing a route use Theory rows with separate assertions. Synchronize with bounded polling, cancellation/event handshakes; do not use sleep-only success evidence or exact millisecond timing. Retry provider recall by itself is insufficient TTL-route proof. Integration proofs supplement existing unit policy/routing tests.

Basis: DEV-315 Description/AC; recon-DEV-315:22-25,36-42,46,49-53,79-99; Q1-Q3. No owner escalation or taste assumption. This matrix does not freeze file scope.

## Loop discipline (Q5)

Patron AMEND. Closing bar for code-review/ship-review: verified in-scope Critical, High and Medium findings block; Low findings are recorded and non-blocking. Missing any required review axis, failed applicable gate, Could not run, missing real-broker acceptance evidence or unresolved owner escalation cannot meet closure. Apply pipeline-permitted non-blocking exit-2 outcomes with their original labels, never promote them to PASS. This bar is settled once for DEV-315, not re-ruled per round.

Review round cap: two. Remediation cap: two fix commits per round. Third-round findings go to owner review on the PR. Above-bar unresolved findings remain blockers; reaching a cap never grants a clean verdict or merge authorization. User retains every merge.

Out-of-scope observations never block this round. Patron decides recording and Rigger files only when Critical/High, broken behaviour or explicitly requested by user, after checking whether an existing ticket can receive the finding. Other observations become one append-only CONCLUSIONS.md line: "noted, no ticket". The scope ending below routes other work out of this round; it does not require automatic ticket creation.

Frozen behavioural scope: existing worker outcome translation to ack/retry/DLQ; publish-before-ack; actual category propagation through Failed with Core retaining decisions/NextAttempt; three closed-code terminal headers; concrete publisher overload and dictionary guard contract; Q4 real-broker ack, independent retry ingress and TTL return, bounded attempt transitions and terminal DLQ evidence; preserve malformed-body, unexpected-exception, cancellation and failed-publish paths. No new architecture, policy, dependency, fixture, production topology, schema, HTTP/OpenAPI/frontend or local-play change. File scope remains unfrozen pending recon. anything else is a follow-up issue, not a finding in this round.

Basis: task-pipeline-2 section 2.2 and Phases 4-5 merge bar; charter section 2.3 and Patron recording duties; grill-with-docs Loop Discipline; DEV-315 AC; Q1-Q4. No owner escalation or taste assumption.

## Approved file map and task order (Q6)

Patron approves eight existing implementation/test files, zero new files:

| File relative to worktree | Change |
|---|---|
| src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentOutcome.cs | actual Category alongside Decision on Failed |
| src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs | propagate Category at both Failed returns |
| src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs | terminal-only closed-code headers and existing publish-before-ack |
| src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs | existing signature retained; overload and guarded fresh merge |
| tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs | six construction updates and Category propagation proof |
| tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs | two construction updates and retained routing proof |
| tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs | Q4 Theory matrix and preserved regression proofs |
| tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs | Q3 header/telemetry, absence, collision/reserved/null and caller preservation cases |

File 8 replaces the proposed new publisher test file: no duplicate publisher class. Its tracked identity is a newly supplied Patron fact requiring Wisp recon before handoff. Files 1-7 are forced by ticket AC; no additional implementation/test/config file is authorized. RecordEnrichmentFailureCommandHandler and IEnrichmentQueue remain unchanged, as do retry policy, production topology, fixture, packages, schema, HTTP/OpenAPI and web.

Ordering: (1) outcome/category propagation and handler/routing unit updates; (2) publisher overload/guard cases and consumer terminal header call; (3) consumer broker matrix/regressions; then Cog refactor and Gauge gates. Test failures/compile updates stay within the approved map; an unexpected additional file is needs recon/decision rather than silent scope growth.

Gate expectations for Phase B: pipeline Roslyn, complexity, InspectCode, property tests with configured applicability, vulnerable packages, format, full dotnet test and mutation. Harness thresholds are authoritative and unchanged; never use mutation since. Keep scope-empty SKIPPED, harness-disabled SKIP and mutation exclusion N/A distinct; failure/Could not run blocks. No gate was run in Phase A. M review requires all three axes and Q5 limits. Phase A next follows Quill specify/plan/tasks, Keel read-only analyze, plan challenge, spec PR and merged Gate 1 before build.

Q6 excludes harness.yml from ticket commits. Patron instructs restoring its reported web enabled true-to-false delta to origin/main and retaining .specify/feature.json intake pin. Keel preserves pre-existing unrelated changes and performs no config edit. Wisp must record the exact delta/provenance and Rigger must reconcile it safely before any commit; it cannot be silently included or treated as an earned disabled gate.

Basis: DEV-315 AC; Q1-Q5; recon-DEV-315:17-99,100-128; charter section 2.3 item 6; task-pipeline-2 Phases 2-5.

## Historical undecided items — settled by Q6

- Publisher extension, outcome shape and real-broker strategy are settled (Q3/Q2/Q4); precise test implementation/file mapping remains undecided.
- Required production/test file changes; existing RabbitMqFixture is Testcontainers and Testcontainers.RabbitMq 4.15.0 is already pinned (recon:99), so no new dependency is established as necessary.
- Complete files touched, remaining publisher guard test mapping, task ordering and gate expectations.
- Final production/test file mapping, ordering and gate expectations need Patron ruling after recon synchronization; Q5 settles behavioural scope and loop terms only.

## Recon synchronization required before file-scope freeze

Resolved: targeted `maestri note read "recon-DEV-315" 100 29` now returned lines 100-128 of 128 with literal signatures, RecordEnrichmentFailureCommandHandler return/exclusion and exhaustive Failed sites. Earlier receipts below are retained as history, not current blockers. Q6 now fixes the file map. Remaining handoff recon is narrower: confirm tracked tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs and exact harness.yml delta/provenance cited by Patron; route config reconciliation to Rigger. No new grill decision is required unless that evidence contradicts the Q6 map/ruling.

Second Q6 targeted readback: `maestri note read "recon-DEV-315" 99 1` returned line 99 successfully, but that line only cites publisher/interface file locations (RabbitMqEnrichmentQueuePublisher.cs:13,30; IEnrichmentQueue.cs:6-9) and the existing telemetry/confirmations/fixture facts. It contains neither literal method signatures nor RecordEnrichmentFailureCommandHandler return/exclusion nor exhaustive production/test Failed-site evidence. DEV-315:80-82 reports those facts as present, but they are absent from this returned line. This is a content gap, not a line-count blocker: 99 lines is acceptable if the required evidence is added inline. Q6 remains unasked and file scope unfrozen. Provide that cited evidence in the exact connected note, or a newly named and connected recon appendix authorized by Conductor.

Conductor supplies PublishAsync(EnrichmentRequested, string, CancellationToken), IEnrichmentQueue.EnqueueAsync(EnrichmentRequested, CancellationToken), production Failed constructions at handler lines 70/77, exhaustive test sites, and RecordEnrichmentFailureCommandHandler returning Task<EnrichmentFailureDecision>. However the connected recon read at Q3 still returns 99 lines, not the reported 101. Synchronize the named connected note so those literal signatures and cited exhaustive construction sites are readable there before fixing the complete plan file list. No fresh search was run by Keel.

Conductor reports recon-DEV-315 connected to Patron #2. Further factual answers must use recon rather than independent searches. Current maestri list shows chain-3; the earlier chain-4 lookup failure does not reopen settled decisions.

## Assumptions and owner escalation

No taste assumptions; no ASSUMPTIONS.md created. No owner checkbox identified so far. No ADR or glossary change decided. Keep unrelated existing changes untouched. No production code, tests, gates, analyzers, format scripts or commits in this ask.

## Patron F1/F2 proof amendment (2026-10-08, post bounded recon)

Ruling: AMEND the settled proof, within ticket scope. F1 and F2 are not retained as blockers. Ticket deliverables, Q1-Q6 behaviour, header contract, fixture, production topology and the eight-existing-file map are unchanged. No owner checkbox: the ticket text and acceptance are unchanged and the constitution is not departed from.

- F1 (terminal-DLQ republish failure): drop the separate real-broker injection/observation requirement. Failed retry and failed DLQ republish share one consumer catch path (EnrichmentConsumer.cs:135-157 -> DeadLetterAsync nack requeue:false at :198-199; recon-DEV-315:182). The existing broker test EnrichmentConsumerTests.cs:190-209 stays as the failed-publish regression proof (Q4 row 5: keep the existing proofs, add none). MarkFailed and SaveChanges happen before the terminal publish (ProcessEnrichmentCommandHandler.cs:73-77; recon:196), which satisfies constitution IV:185-186 without a new proof. Under unchanged topology the broker behaviour when the DLQ target is unavailable cannot be observed (recon:192,203-204). So spec US4 scenario 4's DLQ-republish clause is a documented shared-path expectation, not an acceptance proof.
- F2 (pinned-client nack observable): drop as a DEV-315 proof requirement. Awaited mandatory publishing with confirmation tracking is the settled DEV-18 contract (ADR-0004:42 Accepted; specs/DEV-18/spec.md:139 FR-003; recon:177-179). DEV-315 keeps that publishing unchanged (FR-08) and does not change failure semantics. The existing unroutable assertion RabbitMqPublisherTests.cs:84-92 stays.
- Out of scope: the terminal-publish-failure broker transfer and an end-to-end test of the client nack branch. Noted, no ticket. Neither is shown to be broken behaviour (adjudication-DEV-315-plan:70).

Authorized Quill fixes (only these): (1) spec.md:67, US4 scenario 4. Keep the retry-republish clause as the real-broker proof. Reword the DLQ-republish clause as a shared catch-path expectation: never success-acked, terminal nack requeue:false, recorded Failed state unchanged. Add that it has no separate broker-injection proof under the fixed fixture and topology. (2) tasks.md:45 T012: limit the assertion to the existing retry-republish failure test. Add no terminal-publish-failure injection and no client-nack test. (3) spec.md FR-10 and plan.md, where they cite failed-publish proof: cite this amendment and ADR-0004:42 for the client contract. Then Keel rechecks.

Basis: DEV-315 Description/AC (Testcontainers test per routing path: ack, retry, DLQ; these are unchanged and fully proven); Q1, Q4 row 5, Q5 frozen scope, and the Q6 file map; recon-DEV-315:174-213; adjudication-DEV-315-plan:67-78; charter section 2.3.
