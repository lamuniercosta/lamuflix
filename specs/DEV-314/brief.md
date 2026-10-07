# DEV-314 — Phase A closing brief

## Phase B evidence-method refinement — T020 S1-c (2026-10-07)

Patron approved the ruling in CONCLUSIONS.md §Phase 3 T020 S1-c; commit 80c46bfc886eb3b620843f0dbee2d56cdbfc8009 is ratified as the current documentation baseline. This section refines Q2/Q5 evidence language below; corrected ticket Scope 1-3, AC1-2, constitution, Q6 envelope and behavioral contracts are unchanged.

S1-a (consumer type, requested quorum queue and autoAck false) and S1-c (async, non-blocking dispatch) remain mandatory structural obligations. T020 records source-structure review with exact file:line evidence for these properties, including ReceivedAsync dispatch to HandleAsync and its transitive awaits without introduced synchronous waits. The review receipt must state its limitation: it is structural verification, not a behavioral assertion or an async negative-control PASS. Existing provider-barrier blocking is not evidence of consumer sync-over-async.

Consumer_APrefetchOfOne_LeavesTheSecondMessageReady supplies real-broker prefetch evidence only; the second-ready-message assertion at the mapped L177 cannot distinguish task-returning dispatch from sync-over-async in the permitted observation envelope. T021 performs the prefetch break/failure/restore control and records that S1-c has no separate behavioral negative-control surface. No dispatcher instrumentation, source seam, reflection, thread-id, timing or ThreadPool heuristic is authorized.

The obligation matrix, SC-001 and empty-production-diff condition require explicit structural review receipts for S1-a/S1-c alongside named passing tests and restored negative-control receipts for all behaviorally observable obligations. No obligation is dropped or credited solely from test presence. Prefetch, initial trace, broker-redelivery trace/settlement and per-message scope/disposal behavioral controls remain mandatory. Actual review/test/control receipts are still required; this ruling supplies none of them.

Quill reconciles spec US1 evidence wording, FR-009, SC-001 and the empty-production-diff condition; plan proof language; tasks T020/T021 and proof checkpoint; and requirements checklist CHK001/CHK004/CHK008 in one bounded documentation fix round. No production/test/config/fixture edit, gate run, task completion claim or new owner checkbox follows from this refinement.

- Decision owner: Patron; recorder: Keel.
- Grill: Q1-Q12 approved; shared understanding closed on 2026-10-07.
- Size: L. No ui: tag. No Gherkin/Reqnroll stage requested.
- Spec worktree: F:/Dev/LamuFlix.worktrees/feature-314-spec.
- Spec branch: feature/314-spec.
- Authorized baseline: 48e2188d2140f5c1cd3a116d78128dab434aa1a1.
- Phase B worktree: F:/Dev/LamuFlix.worktrees/DEV-314; created through normal intake after Gate 1.
- Facts: recon-DEV-314; corrected-ticket receipt: DEV-314 note:29-33.
- Full decision exchanges: [CONCLUSIONS.md](CONCLUSIONS.md).
- No commits during this Phase 2 ask. Existing .specify/feature.json pin is preserved.

## Corrected ticket and resulting delivery

Implement/prove EnrichmentConsumer in src/LamuFlix.Infrastructure/RabbitMq/ using RabbitMQ.Client 7 async consumption and OpenTelemetry trace extraction. Ruling 1 corrected the retired Worker location and prefetch owner in YouTrack without changing delivery.

The three scope obligations remain:

1. EnrichmentConsumer : BackgroundService uses AsyncEventingBasicConsumer on the enrichment.requested quorum queue, with channel prefetch from RabbitMqOptions.Prefetch.
2. Extract traceparent/tracestate from BasicProperties.Headers. Start a consumer Activity connected to the producer trace; redelivery attaches an ActivityLink to the original extracted context.
3. Create a DI scope per message and resolve the feature handler.

Both acceptance criteria remain: asynchronous processing without blocking the worker thread, and one distributed trace from API enqueue to consumer processing. The existing Infrastructure implementation is preserved; DEV-314 completes its proof and fixes only defects exposed by that proof. Test enumeration and historical baseline gates are not delivery evidence.

## Every grill answer

| Question | Approved answer |
|---|---|
| Q1 — Ticket corrections | Infrastructure/RabbitMq replaces retired Worker; RabbitMqOptions.Prefetch replaces EnrichmentOptions as prefetch owner. Rigger verified the correction. Delivery unchanged, no owner checkbox. |
| Q2 — Delivery boundary | Preserve the existing consumer; targeted proof plus only proven behavior fixes. Empty production diff is acceptable only when every obligation has deliberate named-test evidence. No Worker resurrection/deletion, broker/topology redesign, retry/claim-count redesign or unrelated cleanup. |
| Q3 — Trace contract | Producer context is parent on every delivery; preserve TraceId, ParentSpanId and TraceStateString. Exactly one original-context link on redelivery, zero initially. delivery_count stays 0 initial / 1 redelivered, not a cumulative counter. |
| Q4 — Redelivery proof | Use real broker redelivery in the existing RabbitMqCollection with existing RabbitMQ/DB infrastructure; synchronized unacked delivery interruption and restart/release. No arbitrary sleeps, synthetic-event-only proof or retry-republish substitution. Isolate and dispose the correlated ActivityListener; verify completion/settlement separately. |
| Q5 — Acceptance evidence | Named obligation-to-assertion matrix; preserve all 12 existing tests and credit only assertions they actually pin. Add two-message distinct scoped-handler and async-disposal proof; extend carrier tracestate tests only if missing. Temporary negative controls demonstrate assertion failures and are fully restored, never committed. Do not tighten unrelated DEV-390 retry/claim-count assertions. |
| Q6 — File envelope | Consumer integration tests always allowed; carrier tests conditional on missing tracestate proof. Consumer/carrier source changes conditional on a failing obligation test. Phase A specs plus approved ADR only; other source/config/fixtures stay outside. Required expansion returns for cited ruling before edits. |
| Q7 — Care list | Existing dependencies, adapter, ports/decorators, Api wiring and options ownership suffice. No new dependency, project/layer, schema, public API, LocalPlay, secrets, Process.Start or domain vocabulary change. Q6 governs existing-file edits. Necessary architecture changes return to Patron; ticket-delivery changes or constitution departures require owner checkbox. |
| Q8 — Gates | Delivery-head/diff gates, actual tests, distinct failures/reruns and exact exit labels required. Baseline recon gates prove no delivery. Empty production diff yields mutation scope-empty only if the script confirms it; negative controls still required. |
| Q9 — Ordering and clarification | Draft/analyze/challenge/freeze/spec merge before Phase B. Drift/map/tests/negative controls/proven fixes/focused tests/refactor/gates/three-axis review/remediation/rebase/rechecks/L ship/merge bar. Anvil new test work, Cog existing-source changes, Gauge gates. Configured thresholds and stricter refactor gate supersede Q8 numeric parenthetical; no fixed threshold in this brief. |
| Q10 — Review terms | Verified in-scope Critical/High/Medium block; Low recorded nonblocking. All three axes, handshake, ticket evidence, PR publication and gates required. Two rounds maximum, two fix commits per round; exhausted blockers stay visible for owner disposition. |
| Q11 — ADR | Short ADR documents preserving the DEV-18 Infrastructure consumer and parent-plus-redelivery-link tracing, rejecting Worker resurrection and link-only tracing; no new architecture grant. Number and convention come from recon. |
| Q12 — Closure | Shared understanding and Quill handoff approved. ADR 0019, proposed status, date and section format approved. Ordinary xUnit tests supply acceptance evidence. No structural departure or current owner checkbox; Gate 1 still requires user spec merge. Grill closure does not complete delivery. |

Q9 expressly supersedes the fixed complexity parenthetical in the recorded Q8 answer. Thresholds come from harness configuration and the applicable stricter refactor stage. No unsupported script switches are prescribed by this brief.

## Approach and trace contract

Reuse the active consumer and its registered command-handler pipeline. Preserve current options, topology, activation guards and host wiring.

On initial delivery and broker redelivery, ActivityKind is Consumer, TraceId equals the producer TraceId, ParentSpanId equals the producer span id, and TraceStateString equals the propagated tracestate. A shared TraceId alone is insufficient proof. Initial delivery has zero links and messaging.rabbitmq.delivery_count = 0. Redelivery has exactly one link whose context equals the extracted original producer context and messaging.rabbitmq.delivery_count = 1.

A cumulative broker counter is outside scope: noted, no ticket. Broker redelivery and retry republish are distinct; only the former exercises the required redelivery branch.

## Allowed files

Phase A decision/draft artifacts belong under specs/DEV-314/. The approved ADR is docs/adr/0019-infrastructure-consumer-delivery-proof.md. Preserve the pre-existing Spec Kit pin without rewriting it; it is not a production deliverable.

Phase B write envelope:

| File | Permission and trigger |
|---|---|
| tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs | New/strengthened acceptance tests and minimal nested scoped test handler/synchronization helpers. |
| tests/LamuFlix.UnitTests/RabbitMq/TraceContextCarrierTests.cs | Only missing named tracestate roundtrip proof. |
| src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs | Only a behavior fix tied to a failing Q2-Q5 obligation test. |
| src/LamuFlix.Infrastructure/RabbitMq/TraceContextCarrier.cs | Only a behavior fix tied to a failing trace-extraction obligation test. |

Existing RabbitMQ/DB fixtures and composition/publisher/topology tests may be read, reused and run; they are not authorized rewrite targets. Retired Worker, Core domain, packages, schema, API, web, shared glossary and all other source/config remain outside the envelope. Stop for a cited Patron ruling before expansion; a change in ticket delivery or constitution departure becomes an owner checkbox and keeps Gate 1 closed. No new dependency or architectural layer is authorized.

## Test strategy and named obligation matrix

Existing named tests below are candidates established by recon section 4.6, not execution receipts. At Phase B, map their actual assertions to obligations and record exact assertion locations. Any unproven row remains work; existing-test presence alone earns no credit.

| Obligation | Named evidence and required assertions |
|---|---|
| Async consumer and configured prefetch | Consumer_APrefetchOfOne_LeavesTheSecondMessageReady; coordinated blocked first handler and broker observation of second ready message. Preserve async invocation; do not introduce synchronous waits. |
| Processing and lifecycle | Consumer_ASuccessfulOutcome_IsAckedAndTheMovieIsEnriched; Consumer_CancellationRequeues_AndDoesNotMarkTheMovieFailed; Consumer_ADroppedConnection_ResumesConsumptionAfterReconnect. Credit only the specific outcome/lifecycle assertions. |
| Initial producer trace | Strengthen Consumer_TheConsumerActivity_SharesTheProducerTraceId to pin ActivityKind, TraceId, ParentSpanId, TraceStateString, zero links and delivery_count = 0. |
| Broker redelivery trace | New focused consumer integration test: same correlated unacked message genuinely redelivered by RabbitMQ; exact parent/tracestate, one original-context link, delivery_count = 1, and separate completion/settlement proof. |
| Scope per message | New focused two-message integration test: distinct scoped handler instance identities, async disposal after each invocation, no root-scoped reuse. Keep small handler inside the existing test file. |
| Carrier headers | Existing carrier unit/FsCheck tests; conditional extension only where no named test already pins tracestate roundtrip. |
| Activation/topology regressions | Run existing composition, service-collection, options, publisher and topology tests as applicable without rewriting their files. |

For redelivery, establish producer headers including tracestate, hold the first invocation at an explicit barrier while delivery is unacked, interrupt via controlled consumer cancellation/channel closure, then restart/release. Exact fixture mechanics are implementation choices within the approved envelope. Bounded waits observe signals; arbitrary sleeps do not prove ordering. Capture the producer TraceId before publish and select consumer activities by that TraceId plus the expected source/name/kind; movie-id filtering alone is insufficient because its tag is set after ActivityStarted. Keep the complete Q3 parent/tracestate/link/count assertions: TraceId selection alone is not contract proof. Dispose the ActivityListener per test. Use real broker/database infrastructure, not mocked drivers.

Use existing xUnit v3, Shouldly, NSubstitute, FsCheck and Testcontainers conventions. Temporary local negative controls break each acceptance obligation, demonstrate the corresponding assertion failure, restore original behavior, and record failure/restoration evidence in the PR or brief. Never commit deliberately broken production behavior. Preserve initial failures separately from permitted reruns. No unrelated retry/claim-count tightening.

## Gate expectations

Phase A closes decisions and later checks document consistency through Keel analysis and plan challenge. Do not claim implementation gates passed from this grill.

Phase B Gauge runs the standing pipeline on the delivery head and diff: Roslyn analyzers, cyclomatic complexity, InspectCode default diff scope, property tests, vulnerable packages, format verification, full dotnet test including existing/new consumer tests, web applicability and the mutation script. Never use InspectCode -All in this pipeline. Thresholds remain harness-configured; Cog meets the stricter refactor-stage gate. Do not lower thresholds.

Mutation runs through scripts/run-mutation.ps1 when production C# changes, with since disabled. No architect substitution or hand waiver. An empty production diff permits only the script-confirmed scope-empty result; negative controls remain required.

Exit 0 is earned pass. Scope-empty exit 2 is nonblocking SKIPPED (scope-empty); harness-disabled web is nonblocking SKIP; mutation all-configured-excluded result is N/A. None is PASS. Exit 1 and Could not run block. Property-test exit 2 blocks unless a valid opt-out is recorded in the task note. Actual tests executed, initial failures and permitted reruns remain distinct. Baseline build/list-tests/recon receipts are not delivery validation.

## Task ordering and seat routing

1. Quill drafts spec.md, plan.md and tasks.md from this brief, recon and PRODUCT.md; size L adds clarify/checklist. A decision gap is needs decision to Keel, never an invented choice. Set PYTHONUTF8=1 when running Spec Kit. No Gherkin stage.
2. Keel runs read-only speckit-analyze and challenges plan/tasks against this brief, sends one numbered fix list per round. Challengers examine plan/ADR, then adjudication freezes the plan.
3. Open the spec PR through the pipeline; the user merges it to open Gate 1. No Phase B implementation beforehand.
4. Phase B intake obtains fresh recon and drift evidence against main, re-runs analysis, and reconciles drift before work.
5. Establish obligation-to-assertion mapping; add/strengthen deterministic tests and negative controls. Conductor routes new M/L test work to Anvil and existing-source alterations to Cog. Existing-code-only changes follow Cog ownership. No author is authorized to bypass the conditional-source-fix trigger.
6. If proof exposes a behavior defect, Cog fixes only the approved consumer/carrier file with the failing obligation cited. Focused tests and restored negative controls precede completion of the coherent test/fix phase.
7. Cog refactors only changed code, preserving frozen scope and meeting the stricter refactor gate. Gauge owns gate-only tasks.
8. Gauge runs delivery gates; failures route to Cog and require verified reruns. Sentry, Ledger and Compass all report before Keel adjudicates.
9. Remediate within caps, rebase/push through Rigger, rerun gates, perform size-L ship-review split, publish findings/summary and verify merge bar.
10. End at awaiting-merge; the user owns delivery merge. No seat merges, enables auto-merge or pushes main.

Quill should express these dependencies as ordered phases; small coherent test tasks may share a phase. Gate-only tasks remain Gauge, and source-fix tasks are conditional with explicit Cog routing.

## Loop discipline

**Closing bar:** verified in-scope Critical, High and Medium findings block; Low findings are documented and nonblocking. Gate failures, Could not run, missing Sentry/Ledger/Compass axis, missing handshake evidence, unmet corrected-ticket obligation, unanswered structural owner checkbox and unpublished review round always block regardless severity label.

**Frozen scope:** corrected ticket Scope 1-3 and both acceptance criteria; Q3 trace semantics; Q4-Q5 deterministic proof; Q6 file envelope; applicable Q8-Q9 gates; anything else is a follow-up issue, not a finding in this round.

**Round cap:** at most two review/remediation rounds and two fix commits per round. Exhausted-cap blockers stay visible on the PR for owner disposition, never demoted or reported clean. No unrequested extra fix round.

All three axis reports are mandatory on L. Findings and summary must be posted as PR comments; fixes reply to comments and resolve threads. The delivery merge bar additionally requires exact head evidence, acceptable gate statuses, no above-bar finding, MERGEABLE/non-dirty/non-behind state and green hosted checks including gitleaks. Only the user merges.

Follow-ups follow Patron/Rigger governance: Critical/High, broken behavior or user-requested work may warrant a ticket, checked against existing tickets first; other observations receive noted, no ticket in CONCLUSIONS.md. This does not authorize scope expansion.

## ADR and handoff disposition

[ADR 0019](../../docs/adr/0019-infrastructure-consumer-delivery-proof.md) is Proposed pending Gate 1. It documents the Infrastructure preservation boundary and producer-parent plus redelivery-link semantics, rejecting Worker resurrection and link-only tracing. Number/format follow recon sections 24-32. It grants no new architectural scope.

No new domain term was decided, so no CONTEXT.md or ASSUMPTIONS.md edit is required. No taste ruling or over-cap assumed question occurred. Existing vocabulary and ADR 0019 are ready to feed spec drafting; the spec must not contradict the approved decisions.

There is no current structural owner checkbox or constitution departure. The grill and brief are complete; plan/spec drafting, analysis, challenge and Gate 1 remain next. No implementation, test execution or delivery gate pass is claimed by this artifact.

## Plan challenge rulings (2026-10-07)

Patron ratified these Q3/Q4/Q5/Q6/Q10 refinements in CONCLUSIONS.md, section Plan challenge adjudication. They preserve the corrected ticket, trace contract, file envelope and Gate 1 boundary.

- Sentry F1 (Medium): accepted, blocking until plan/tasks mandate producer TraceId captured before publish as correlation key, expected consumer source/name/kind selection, full Q3 assertions and per-test listener disposal. Movie-id filtering alone is insufficient (recon 4.1/4.3).
- Sentry F2 (Medium): accepted, blocking until T010/T011 require verification of the actual quorum x-delivery-limit >= 2 and record reconnect timing bounded within an explicit redelivery wait before T031. T031 enforces these prerequisites using existing test-file options/helpers only; no fixture/config/topology/source expansion. If the permitted envelope cannot establish them, stop for recon/ruling before test work (recon 4.1/4.2).
- Sentry F3 (Low): noted, no ticket. Proof uses valid publisher-injected traceparent/tracestate; preserve the existing absent-header synthetic-context fallback without adding a malformed-header delivery obligation (recon 4.3). Q3 is unchanged.
- Sentry F4 (Low): noted, no ticket. T031 separately proves completion/settlement and cleans up interrupted delivery/consumer/listener state so an unacked message cannot pollute shared-queue tests. No new exhaustion test or cumulative delivery-count contract (Q3/Q4/Q5/Q10).
- Ledger: zero findings accepted. Consumer_ prefix and permitted AAA test headers remain existing conventions, no document fix.
- Compass L1 (Low): noted, no ticket. Retain conservative serialization of conditional T032; no ordering change.
- Compass L2 (Low): noted, no ticket. ADR sibling cross-links are optional; ADR 0019 remains unchanged and Proposed pending Gate 1.

One numbered documentation fix round goes to Quill for plan.md/tasks.md. Freeze follows independent readback and consistency analysis; Gate 1 remains closed until user spec-PR merge. No new owner checkbox, dependency, architecture or implementation authorization.
Freeze receipt (Keel, 2026-10-07): independently read Quill fixes at plan.md:89-94 and tasks.md:26-27,38-39. Sentry F1/F2 closed; F3/F4 and Compass L1/L2 noted nonblocking; Ledger zero findings accepted. Cross-artifact analysis retains 13 requirements, 22 tasks and full coverage with no remaining blocking inconsistency. Plan frozen for Phase 2 step 7 Patron provisional Gate 1 and spec PR; user merge still opens Gate 1. No implementation, gates, commit or merge performed.
