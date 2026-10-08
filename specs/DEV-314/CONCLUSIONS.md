# DEV-314 — Conclusions

## Ruling 1 (2026-10-06, grill Q1): ticket text corrections preserve delivery — approved, no owner checkbox

**Verdict:** Correcting the DEV-314 description's (a) consumer location `src/LamuFlix.Worker/` → `src/LamuFlix.Infrastructure/RabbitMq/` and (b) prefetch source `EnrichmentOptions` → `RabbitMqOptions.Prefetch` preserves every scope behavior and both acceptance criteria. Authorized as an exact ticket correction through Rigger. Not an owner checkbox: a description correction that does not change what the ticket delivers is Patron's call (assigned role; `specs/PRODUCT.md` §5 — only ticket-delivery changes and constitution departures escalate).

**Basis:**
- Worker retired: DEV-19 removed `LamuFlix.Worker` from the solution build (`specs/DEV-19/spec.md:15`); the project no longer restores (NU1010). The consumer the ticket describes already exists at `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs` (DEV-18). Constitution I places the message-queue adapter in Infrastructure. (recon-DEV-314 §2, §4.1)
- Prefetch: `EnrichmentOptions` has no `Prefetch` member (`src/LamuFlix.Core/Options/EnrichmentOptions.cs:8-18`); the implementation reads `RabbitMqOptions.Prefetch` (`EnrichmentConsumer.cs:47`). (recon-DEV-314 §8)
- The existing implementation is preserved; no Worker resurrection, no duplicate options class. The missing proof (redelivery `ActivityLink` / `messaging.rabbitmq.delivery_count` test gap, recon-DEV-314 §4.6) remains DEV-314 work.

---

## Q2 — Delivery boundary

**Keel question and recommendation:**

Q2/12 delivery boundary: Preserve the existing Infrastructure consumer and satisfy the complete corrected ticket through targeted proof plus only behavior fixes exposed by that proof. Explicitly prove redelivery ActivityLink and delivery-count telemetry; retain async processing, prefetch, trace propagation and per-message DI as acceptance obligations. No Worker resurrection or deletion, broker/topology redesign, retry/claim-count redesign, or unrelated cleanup. Basis recon sections 1, 2, 4.1-4.6, 6; Q1. Cost: deliberate tests and verification still required even if source diff stays empty. Approve this delivery boundary?
**Patron answer:**

[from Patron] DEV-314 Q2 ruling: APPROVED — preserve the existing Infrastructure consumer; deliver the complete corrected ticket through targeted proof plus only the behaviour fixes that proof exposes.

- Ticket (corrected, Q1/Ruling 1): Scope 1-3 and both ACs name the consumer at src/LamuFlix.Infrastructure/RabbitMq/, already implemented (EnrichmentConsumer.cs, DEV-18; recon §2, §4.1). Acceptance obligations stay all five: async processing without blocking (AC1), RabbitMqOptions.Prefetch (Scope 1), traceparent/tracestate extraction to a single trace (Scope 2, AC2), redelivery ActivityLink plus messaging.rabbitmq.delivery_count telemetry (Scope 2; explicit proof gap per recon §4.6), per-message DI scope (Scope 3).
- An empty production diff is acceptable only if each obligation is pinned by a deliberate, named test that fails against a broken implementation; a source change is in scope only where such a test exposes a defect (recon §4.1-4.6, §6).
- Out of scope: Worker resurrection or deletion (DEV-19 retired it; Ruling 1), broker/topology redesign, retry/claim-count redesign, unrelated cleanup — none is in the ticket text; any such finding goes to CONCLUSIONS as noted, no ticket unless Critical/High or broken behaviour.


---

## Q3 — Trace contract

**Keel question and recommendation:**

Q3/12 trace contract: Consumer ActivityKind.Consumer with producer context as parent on initial delivery and redelivery, preserving producer TraceId and tracestate; redelivery adds exactly one ActivityLink to that extracted original context, initial delivery has none. Preserve current messaging.rabbitmq.delivery_count boolean encoding 0 initial / 1 redelivered, not a new cumulative broker-delivery counter. Assert context identities and tag values explicitly. Basis corrected Scope 2, AC2, constitution VI and recon sections 4.3, 4.6 (EnrichmentConsumer.cs:227-238). Cost: telemetry proof must distinguish parent identity from mere matching TraceId. Approve?
**Patron answer:**

[from Patron] DEV-314 Q3 ruling: APPROVED — trace contract as recommended.

- Ticket Scope 2 + AC2 + constitution VI (constitution.md:218-224): an ActivityKind.Consumer Activity whose parent is the context extracted from traceparent/tracestate on every delivery, keeping the producer TraceId and tracestate; on redelivery, exactly one ActivityLink to that same extracted context; on first delivery, none. This matches the current code (EnrichmentConsumer.cs:225-238; recon §4.3).
- Tests must pin identity, not resemblance: ParentSpanId equals the producer span id, the TraceId and TraceStateString are preserved, the redelivery link count is 1 and its context equals the extracted parent, the first-delivery link count is 0. Matching TraceId alone does not satisfy AC2 (recon §4.6 gap).
- messaging.rabbitmq.delivery_count keeps its current 0 = first delivery / 1 = redelivered encoding (EnrichmentConsumer.cs:237). The ticket asks for no broker delivery counter, so a cumulative counter is out of scope (Q2 boundary, no retry/claim-count redesign). Noted, no ticket.


---

## Q4 — Deterministic redelivery proof

**Keel question and recommendation:**

Q4/12 deterministic broker redelivery proof: Integration tests in existing RabbitMqCollection using real Testcontainers RabbitMQ and existing DB fixture, no mocked driver or fabricated BasicDeliverEventArgs as sole proof. Establish producer context including tracestate, block first handler at an explicit synchronization barrier, interrupt that unacked delivery through controlled consumer cancellation/channel closure, then release/restart and observe actual broker redelivery. Use message/movie correlation and bounded signal waits, not arbitrary sleeps, to assert first and redelivery Activities and Q3 exact context/link/tag contract. Do not mistake republishing a retry for broker redelivery. Assert completion/settlement separately from span capture; listener is disposed and captures only this test message. Basis recon 4.1, 4.3, 4.6 and cancellation/reconnect tests; existing architecture/testing rules. Cost: coordinated real broker test is more involved than a synthetic branch test but proves transport flag semantics. Approve test contract, leaving precise fixture mechanics to implementation within envelope?
**Patron answer:**

[from Patron] DEV-314 Q4 ruling: APPROVED — this is the test contract; fixture mechanics are left to implementation, within these limits.

- Proof has to come from a real broker redelivery. Use the existing RabbitMqCollection (tests/LamuFlix.IntegrationTests/RabbitMqCollection.cs) with Testcontainers RabbitMQ and the existing DB fixture. To create the redelivery, leave a delivery unacked, interrupt it with controlled consumer cancellation or channel closure, then restart or release. A republished retry is not a redelivery. A synthetic BasicDeliverEventArgs test may supplement the proof but never stand in for it. Basis: ticket Scope 2 (if message is redelivered), AC2, constitution VI, recon §4.1, §4.3, §4.6.
- Deterministic only: explicit synchronization barriers, bounded signal waits, and message/movie correlation. No arbitrary sleeps. Assertions cover the Q3 contract exactly (parent identity, tracestate, link count/context, delivery_count 0/1), and completion/settlement is asserted separately from span capture.
- Isolation: the ActivityListener is scoped and disposed per test and filters to this test's correlation. Follow the existing listener pattern (EnrichmentConsumerTests.cs:276). No new test project, dependency, or fixture class beyond what is needed inside the existing collection (assigned role §2.3 items 1-2).


---

## Q5 — Acceptance proof matrix

**Keel question and recommendation:**

Q5/12 remaining acceptance proof: Named test-obligation matrix preserving existing 12 tests without assuming enumeration equals success: existing prefetch=1 second-message-ready test proves configured prefetch and async nonblocking wait; successful-outcome and cancellation/reconnect tests cover processing/lifecycle; strengthened producer-trace test plus Q4 redelivery test cover propagation. Add a focused two-message scope-disposal test with scoped handler instances: distinct instance per message, async disposal observed after each invocation, no root-scoped handler reuse. Extend carrier tests for tracestate roundtrip only where current named tests do not pin it; reuse composition tests to retain activation. Assert exact message-correlated observations without tightening unrelated DEV-390 retry claim counts. Negative controls must show each new assertion fails when its obligation is broken; no production mutation committed as a test fixture. Basis corrected Scope 1-3 / ACs, recon 4.4-4.6 and 4.6.1. Cost: DI lifecycle proof may need a small scoped test handler within EnrichmentConsumerTests.cs. Approve?
**Patron answer:**

[from Patron] DEV-314 Q5 ruling: APPROVED — a named test-obligation matrix that maps every obligation in Scope 1-3 and AC1-2 to at least one named test.

- Keep the existing 12 tests and claim each one only for what its assertions actually pin: prefetch=1 second-message-ready for Scope 1 and AC1, the outcome and cancellation/reconnect tests for lifecycle, and the strengthened producer-trace test plus the Q4 redelivery test for Scope 2 and AC2. A test that merely exists earns no row. Extend the carrier tests for the tracestate roundtrip only where no named test pins it today (recon §4.4-4.6, §4.6.1).
- Scope 3 (DI scope per message) needs a new two-message test: a distinct scoped handler instance per message, async disposal observed after each invocation, and no root-scoped reuse. The small scoped test handler lives inside EnrichmentConsumerTests.cs, with no new test project or dependency (assigned role §2.3 items 1-2).
- Negative controls are local and temporary. Break each obligation, show that its new assertion fails, revert, and record the evidence in the PR or brief. Nothing mutated gets committed. Do not tighten the DEV-390 retry/claim-count assertions (Q2 boundary).


---

## Q6 — File envelope

**Keel question and recommendation:**

Q6/12 allowed file envelope: Phase A edits confined to specs/DEV-314/ and the Q11 ADR if required; existing .specify/feature.json pin preserved, not a production deliverable. Phase B normal writes tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs and conditional tests/LamuFlix.UnitTests/RabbitMq/TraceContextCarrierTests.cs for missing tracestate proof. Conditional behavior-fix writes only src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs and TraceContextCarrier.cs, each tied to a failing Q2-Q5 obligation. Existing fixtures/composition/publisher/topology tests are reusable read/test surfaces, no rewrite authority. All other source/config, retired Worker, Core domain, packages, schema, API, web and shared glossary remain outside envelope; required expansion returns for deliberate cited decision before editing. Basis Q1-Q5, recon sections 3-6 and DEV-390 overlap. Cost: builder stops for decisions if fixture changes or other fixes prove necessary. Explicitly approve conditional edits of the two named existing source files under care-list 6?
**Patron answer:**

[from Patron] DEV-314 Q6 ruling: APPROVED — file envelope as recommended. Conditional edits to both source files are authorized.

- EnrichmentConsumer.cs: the corrected ticket names it in Scope 1, so it needs no care-list-6 ruling. TraceContextCarrier.cs: the ticket does not name it, but Scope 2 (traceparent/tracestate extraction) and AC2 require it. Under care-list 6 an edit that an AC forces is in scope. Each edit to either file has to cite the failing Q2-Q5 obligation test that drove it (recon §3-6).
- Tests: EnrichmentConsumerTests.cs is always in the envelope. TraceContextCarrierTests.cs is in only where no named test pins the tracestate roundtrip yet (Q5). Phase A writes go to specs/DEV-314/ only, plus the Q11 ADR if Q11 rules one is needed. The .specify/feature.json pin is preserved untouched.
- Everything else is outside the envelope, and that includes fixtures, composition, publisher and topology tests, the retired Worker, Core, packages, schema, API, web, and the glossary (assigned role §2.3 items 1-4 and 6; Q2 boundary; DEV-390 overlap). The builder stops and returns for a cited ruling before editing anything else.


---

## Q7 — Architecture and care-list disposition

**Keel question and recommendation:**

Q7/12 architecture and care-list closure: Existing dependencies, Infrastructure adapter placement, command-handler port/decorator chain, Api host activation and options owners remain sufficient; no new dependency/project/layer, schema or public API shape is approved, no LocalPlay/secrets/Process.Start touch is authorized. Tests use existing xUnit v3, Shouldly, NSubstitute, FsCheck and Testcontainers rather than introducing alternatives. Ticket specifies only async consumer / trace / per-message scope, not a new host or handler contract. No new domain vocabulary is introduced: consumer, enrichment request, producer context and broker redelivery keep their current meanings; general telemetry terms do not need CONTEXT.md edits. Basis recon sections 3, 4.4-4.6, 5, constitution I/II/VI and Q1-Q6. Cost: any necessary architectural change returns to Patron and a ticket-delivery change or constitution departure is owner checkbox with Gate 1 closed. Approve this explicit care-list disposition?
**Patron answer:**

[from Patron] DEV-314 Q7 ruling: APPROVED — every care-list item is closed with no grant. Nothing new is needed.

- Items 1-4 (dependency, project/layer, schema, API shape): none is needed. The ticket covers only the async consumer, trace propagation, and a per-message DI scope (corrected Scope 1-3). Existing packages are enough: RabbitMQ.Client 7 and OpenTelemetry in production, and xUnit v3, Shouldly, NSubstitute, FsCheck and Testcontainers in tests. The Infrastructure adapter placement, the handler port/decorator chain, Api host activation, and the RabbitMqOptions owner all stay as they are (constitution I/II/VI; recon §3, §4.4-4.6, §5).
- Item 5: nothing here touches Features:LocalPlay, secrets, or Process.Start, and nothing is authorized to. Item 6 is closed by the Q6 envelope.
- No new domain vocabulary, so CONTEXT.md is not edited. If the builder hits an architectural need, it comes back to Patron. A ticket-delivery change or a constitution departure is blocked: structural, an owner checkbox, and Gate 1 stays closed until it is answered.


---

## Q8 — Gate expectations

**Keel question and recommendation:**

Q8/12 gate expectations: Phase A docs consistency only, no pretending baseline recon gates verify delivery. Phase B Gauge runs pipeline gates on delivery diff/head: Roslyn, complexity, InspectCode default diff scope (never -All), property tests, vulnerable packages, format verification, full dotnet test including existing and new consumer tests, web applicability, and mutation script on M/L production-C# changes. Scope-empty exit-2 is nonblocking SKIPPED (scope-empty), harness-disabled web is SKIP, script-issued all-excluded mutation is N/A; never PASS. Exit 1 or Could not run blocks; unaccepted property exit-2 blocks; no threshold lowering, hand waiver, since, or architect substitute. If production diff empty mutation is script-confirmed scope-empty, with Q5 negative controls still required. Record actual tests run, initial failures and permitted reruns separately, no counting --list-tests as executed. Basis AGENTS verification rules, task-pipeline Phase 3 steps 4-6 and charter known tooling limitations, recon section 7. Cost: test-only delivery still needs full suite and tooling evidence. Approve?
**Patron answer:**

[from Patron] DEV-314 Q8 ruling: APPROVED — gate expectations as recommended. This restates the standing gate rules, so it is not a new grant.

- Phase B runs on the delivery diff and head: Roslyn, complexity (<=15), InspectCode at its default diff scope (never -All), property tests, the vulnerable-package scan, format verification, the full dotnet test run (existing and new consumer tests), web applicability, and mutation on M/L production C#. A skipped gate never counts as a pass (CLAUDE.md, Static-analysis gates; recon §7). Phase A checks docs consistency only, and baseline recon gates are not delivery evidence.
- Status labels: scope-empty exit 2 = SKIPPED (scope-empty), harness-disabled web = SKIP, and the mutation script's all-excluded result = N/A. Never report any of these as PASS. Exit 1, Could not run, and an unaccepted property-test exit 2 all block. No lowering thresholds, no hand waivers, no since-overrides, no architect substitute.
- If the production diff is empty, mutation is SKIPPED (scope-empty) only once the script confirms it, and the Q5 negative controls are still required. Evidence lists the tests actually executed, initial failures, and permitted reruns separately. --list-tests output is not execution.


---

## Q9 — Task ordering and threshold clarification

**Keel question and recommendation:**

Q9/12 task ordering, including Q8 threshold clarification: Quill drafts spec/plan/tasks (L clarify/checklist) from brief, Keel analyzes, challengers review and freeze, user merges spec PR before Phase B. Phase B fresh recon/drift check first, map actual assertions to all obligations, add/strengthen tests and negative controls, behavior-fix only proven failures, run focused tests, then Cog refactors changed code, Gauge gates, three-axis adjudication/remediation, rebase/rechecks and L ship split before delivery merge bar. New M/L test work belongs Anvil, existing-code alterations belong Cog; Conductor routes conditional source fixes rather than asking Anvil to alter source. Small tests may share a coherent phase but gate-only tasks remain Gauge. Q8 text mentioned complexity <=15: brief omits any fixed threshold and requires harness configuration plus tighter refactor-stage threshold, per AGENTS never hardcode and task-pipeline Phase 3 step 3; Q8 does not waive that. Cost: phase-separated evidence and author/fixer routing may add handoffs, avoiding scope drift. Approve ordering and threshold clarification?
**Patron answer:**

[from Patron] DEV-314 Q9 ruling: APPROVED — task ordering and the threshold clarification, both as recommended.

- Phase A runs Quill drafts (L clarify/checklist), then Keel analysis, then challenger review and freeze. The user merges the spec PR before Phase B starts. Phase B runs drift recon, then the obligation-to-assertion map, then tests plus negative controls, then fixes only for failures the tests prove, then focused tests, then Cog refactor, then Gauge gates, then three-axis adjudication and remediation, then rebase and rechecks, then the L ship split, then the merge bar. Basis: task-pipeline Phase 3; Q2-Q8.
- Routing: new M/L test work goes to Anvil and changes to existing source go to Cog. Conductor routes any conditional fix to EnrichmentConsumer.cs or TraceContextCarrier.cs (the Q6 envelope). Gate-only tasks stay with Gauge, and small test tasks may share one coherent phase.
- Threshold correction to Q8: the brief states no fixed complexity number. The gate uses the harness-configured threshold, and the refactor stage runs the stricter refactor gate (run-cyclomatic-complexity.ps1 -Threshold per harness config). This replaces the <=15 parenthetical in my Q8 text and waives nothing in Q8.


---

## Q10 — Loop discipline

**Keel question and recommendation:**

Q10/12 review loop terms: Closing bar: verified in-scope Critical/High/Medium findings block; Low findings documented and nonblocking; gate failures, missing axis or handshake evidence, unmet corrected ticket obligations and unanswered structural owner checkboxes always block regardless label. Frozen scope: corrected ticket Scope 1-3 and both ACs, Q3 trace semantics, Q4-Q5 proof, Q6 envelope and applicable gates; anything else is a follow-up issue, not a finding in this round. Follow-up recording follows charter: Critical/High, broken behavior or user-requested work may need Patron-routed Rigger ticket, other observations noted, no ticket. Max two review/remediation rounds, max two fix commits per round; exhausted caps preserve blockers in PR for user review, never demote or claim clean. All three Sentry/Ledger/Compass reports required on L, findings and summary must be posted on PR, fixes reply/resolve threads; only user merges. Cost: a third-round blocker can prevent clean merge bar and requires explicit owner disposition. Approve closing bar, frozen scope and caps?
**Patron answer:**

[from Patron] DEV-314 Q10 ruling: APPROVED — closing bar, frozen scope and caps as recommended.

- Closing bar: verified in-scope Critical, High and Medium findings block, and Low findings are documented without blocking. Six things always block whatever their label: a gate failure, a missing review axis (Sentry/Ledger/Compass on L), missing handshake evidence, an unmet corrected-ticket obligation, an unanswered structural owner checkbox, and a review round that never reached the PR (findings and summary posted as PR comments, with fixes replying to and resolving their threads). Basis: the assigned role's review rule, and only the user merges.
- Frozen scope: corrected Scope 1-3 and AC1-2, the Q3 trace semantics, the Q4-Q5 proof, the Q6 envelope, and the Q8/Q9 gates. Anything outside that becomes a follow-up, not a finding in this round. A follow-up gets a Patron-routed Rigger ticket only if it is Critical or High, is broken behaviour, or the user asked for it. Everything else gets one CONCLUSIONS line: noted, no ticket.
- Caps: at most 2 review/remediation rounds, and at most 2 fix commits per round. If a cap runs out, the open blockers stay visible on the PR for the user to decide, as awaiting owner disposition. They are never demoted or reported as clean. This is a standing ruling, cited once per chain.


---

## Q11 — ADR disposition

**Keel question and recommendation:**

Q11/12 ADR disposition: Size L pipeline requires Keel ADR, while domain-modeling offers new ADR sparingly. Short task-local ADR in docs/adr/ records why DEV-314 completes proof of the DEV-18 Infrastructure consumer instead of resurrecting retired Worker: preserve established async adapter/host wiring and producer-parent plus redelivery-link trace semantics; original stale ticket might otherwise mislead a future implementer. Cite Q1 corrected-ticket receipt, DEV-18/19 and constitution I/VI via recon; rejected alternatives Worker resurrection and replacing parent continuity with link-only tracing. No new architecture, glossary or dependency decision. This is consequential boundary documentation, not an authority to expand scope. ADR numbering/path requires recon because recon-DEV-314 does not list existing docs/adr entries; request that single fact through Bernstein if approved, without searching ourselves. Cost: one small ADR, its disposition incorporated in brief; drafting ready but exact filename waits for recon. Approve ADR content/disposition?
**Patron answer:**

[from Patron] DEV-314 Q11 ruling: APPROVED — one short task-local ADR, with the content and disposition as recommended.

- Content: why DEV-314 finishes proving the DEV-18 Infrastructure consumer instead of resurrecting the retired Worker. It keeps the async adapter and host wiring and the Q3 trace semantics (producer as parent on every delivery, plus one link on redelivery). Rejected alternatives: Worker resurrection, and link-only tracing that drops parent continuity. Basis: Ruling 1 and the Q1 corrected-ticket receipt (DEV-314:29-33), DEV-18/19, constitution I/VI, recon §2, §4.1, §4.3. The ADR documents a boundary already in force. It grants no scope, architecture, glossary or dependency decision (Q2, Q7).
- Location: docs/adr/ already exists in the worktree, so this adds no new folder (care-list item 2 does not apply). The next free number and the filename convention come from recon routed through Bernstein, as proposed, and Patron does not rule on them.
- Envelope: the ADR is a Phase A file alongside specs/DEV-314/ (Q6), and it is frozen with the spec PR.


---

## Q12 — Shared understanding and grill closure

**Keel question and recommendation:**

[from Keel] DEV-314 Q12/12 shared-understanding closure. Worktree F:/Dev/LamuFlix.worktrees/feature-314-spec, branch feature/314-spec, authorized HEAD 48e2188d2140f5c1cd3a116d78128dab434aa1a1 unchanged. Q1-Q11 recorded in specs/DEV-314/CONCLUSIONS.md. ADR recon complete sections 24-32: next 0019 and current metadata/section format. I recommend closing shared understanding on all prior rulings and authoring docs/adr/0019-infrastructure-consumer-delivery-proof.md, H1 # 0019. Infrastructure consumer delivery proof, Status Proposed (Gate 1 spec PR), Date 2026-10-07, Ticket DEV-314, Context/Decision/Consequences. brief.md will include every answer, corrected ticket obligations, preserve existing consumer, exact parent/tracestate/link/0-1 contract, real-broker synchronized redelivery, two-message scoped disposal proof, named test matrix and negative controls, narrow conditional file envelope, no new architecture/dependencies/schema/API or glossary changes, configured gates and stricter refactor expectations, phase/task/seat ordering, closing bar and two-round caps, ADR disposition. No Gherkin/Reqnroll stage is requested; ordinary existing xUnit integration/unit tests are the acceptance evidence. No current owner checkbox or structural departure is proposed; Gate 1 still requires user spec merge. Cost: complete Phase B proof remains mandatory even with empty production diff; this closes grill only, not implementation or delivery review. Approve shared understanding and this complete handoff to Quill? Reply cited ruling text only, no file edits or commits by Patron. Facts come from recon-DEV-314; missing fact means needs recon and stop.

**Patron answer:**

[from Patron] DEV-314 Q12 ruling: APPROVED — shared understanding is closed and the handoff to Quill is complete as described.

- The brief carries Ruling 1 and Q2-Q11 unchanged: the corrected Scope 1-3 and AC1-2 obligations, the preserved consumer, the Q3 trace contract, the Q4 real-broker redelivery, the Q5 matrix plus scoped-disposal proof and negative controls, the Q6 envelope, the Q7 care-list closure, the Q8/Q9 configured gates and stricter refactor gate, the Q9 ordering and routing, the Q10 closing bar and caps, and the Q11 ADR.
- ADR: docs/adr/0019-infrastructure-consumer-delivery-proof.md. H1 is # 0019. Infrastructure consumer delivery proof, Status: Proposed, Date: 2026-10-07, Ticket: DEV-314, with Context, Decision and Consequences sections. The number and format follow recon-DEV-314 §24-32.
- No Gherkin/Reqnroll stage (opt-in, not requested), so the existing xUnit integration and unit tests are the acceptance evidence. There is no owner checkbox and no structural departure. Gate 1 stays closed until the user merges the spec PR. This closes the grill only. The full Phase B proof is still required even if the production diff is empty.


---

## Plan challenge adjudication — Q4 implementation refinements

**Keel question and recommendation:**

Plan challenge (Sentry, Ledger, Compass) adjudicated. Accept Sentry F1 Medium: the producer TraceId captured before publish selects consumer activities by source/name/kind, since the movie id tag is set after ActivityStarted (recon 4.1/4.3); keep the complete Q3 assertions and per-test listener disposal. Accept F2 Medium: T010/T011 verify the actual quorum delivery limit is at least 2 and record fixture reconnect timing bounded within the explicit redelivery wait, using only existing test-file options and helpers; if the permitted envelope cannot establish these prerequisites, stop for recon or a ruling. F3 Low: the trace proof uses valid publisher-injected headers; the absent-header fallback stays, with no new malformed-header obligation (recon 4.3). F4 Low: explicit ack/settlement and cleanup prevent shared-queue pollution; no cumulative-count or exhaustion scope. Ledger: zero findings, Consumer_ prefix/AAA kept as existing convention. Compass: L1 conservative serialization kept; L2 ADR cross-links optional, no edit. Ratify?

**Patron answer:**

[from Patron] DEV-314 plan-challenge ruling: APPROVED — all four Sentry refinements, Ledger and Compass dispositions ratified as recommended.

- F1/F3 refine how the Q3 contract is proven; they do not change it. TraceId-based selection is needed because the movie id tag lands after ActivityStarted (recon 4.1/4.3). The Q3 assertions stay complete, listener disposal stays per test, the absent-header fallback is preserved, and there is no new malformed-header obligation.
- F2/F4 harden the Q4 real-broker redelivery proof and the Q5 scoped-disposal proof inside the Q6 envelope. That means existing test-file options and helpers only, with no fixture, config, topology or source expansion (Q6, Q7). If the envelope cannot establish the prerequisites, the work stops for recon or a Patron ruling and does not expand. There is no cumulative-count or exhaustion scope (Q10 frozen scope).
- Ledger's zero findings and the Consumer_/AAA naming are existing convention. Compass L1 (serialization) stays as it is, and L2 ADR cross-links are optional with no edit. No owner checkbox; the Gate 1 status is unchanged by this ruling.

---

## Phase 3 T020 S1-c — async-dispatch evidence method

**Keel question and recommendation:**

Anvil reports the prefetch-1 test keeps the second message ready (L177) while the synchronous provider barrier holds the dispatch prefix; returning the HandleAsync task (consumer L50) and blocking on GetResult give identical permitted observations. Q6 allows only test-file handlers and helpers, with no dispatcher instrumentation or source seam without a failing obligation test. Proposal: keep AC1 async dispatch as a mandatory structural obligation and do not claim L177 proves absence of sync-over-async. T020 records a source-structure review of ReceivedAsync to HandleAsync and its transitive awaits plus the real-broker prefetch evidence. T021 runs the prefetch negative control only and records that no behavioral async negative-control surface exists. S1-c is marked structural verification with an explicit limitation. No thread-id, timing or ThreadPool heuristics, reflection or new seam. Spec US1, FR-009 and the empty-production-diff sentence get explicit evidence-method wording.

**Patron answer:**

[from Patron] DEV-314 S1-c ruling: APPROVED — structural verification with a stated limitation; AC1 and the constitution are preserved.

- AC1 (brief:24, spec:15) fixes what is delivered (async, non-blocking consumption), not the evidence method. Naming S1-c as a reviewed structural obligation narrows only my own Q2/Q5 evidence rule (brief:31/34, spec:64), so it changes no ticket text and needs no owner checkbox. The constitution has no behavioral-proof clause for async dispatch.
- No sanctioned stronger mechanism exists inside Q6/Q7. Instrumentation or a seam needs a failing obligation test (Q6). A sync-over-async analyzer package is a new dependency (Q7). Timing, thread-id and reflection probes are non-deterministic or out of envelope (Q4/Q5). So no recon is needed. S1-c must never be reported as assertion-proven or as an async negative-control PASS, and spec:24 (US1 scenario 2) must say it is verified by review, not asserted.
- Authorized: brief-first, then one Quill documentation fix list reconciling spec (US1 Independent Test and scenario 2, FR-009, the empty-production-diff sentence), plan, tasks T020/T021 and the checklist to this evidence language for S1-a/S1-c. All behavioral negative controls (prefetch, scope/disposal, trace, redelivery) stay mandatory. Gate 1 status is unchanged.

---

## Phase 4 step 3 — remedy for High F1 (TraceParentGuard)

**Patron ruling:** REMEDY (a) — amend Patron's own Q3 parent wording to the production wire parent, remove the guard, and prove the full chain under production-representative client publisher instrumentation. No owner checkbox; no source edit.

- Basis — ticket vs. ruling: ticket AC2 requires a single distributed trace; the producer-span-id equality (brief:44, brief:61, brief:88, spec SC-002) is Patron's own Q3 refinement, not ticket text, so narrowing it to production truth needs no `blocked: structural`. Production Api wires the RabbitMQ.Client.Publisher source (ServiceDefaults Extensions.cs:41-44, Api Program.cs:16), so the wire traceparent is the client publish span, a child of Enrichment.Enqueue with the same TraceId and tracestate (findings-DEV-314-Compass:24). That is correct messaging-span behavior, not a defect: remedy (b) has no failing obligation in production code (FR-008), and dropping the client source or overriding its injector would edit ServiceDefaults/publisher outside Q6 and discard telemetry. Remedy (c), documenting only, is rejected (adjudication-DEV-314-phase4:15).
- Amended Q3 contract (replaces "ParentSpanId equals the producer span id"): on initial delivery and redelivery the consumer activity is ActivityKind.Consumer, TraceId equals the producer TraceId, ParentSpanId equals the SpanId of the extracted wire context, which is the RabbitMQ.Client.Publisher producer span whose ParentSpanId equals the LamuFlix producer span id, and TraceStateString equals the propagated tracestate. Redelivery has exactly one link equal to that extracted original wire context; delivery_count stays 0/1. TraceId alone stays insufficient. Every other Q3/Q4/Q5 obligation is unchanged.
- Work authorized, inside Q6 (EnrichmentConsumerTests.cs only): delete TraceParentGuard and every use (:284/:334/:458-469), since the remedy orphans it; the per-test ActivityListener deterministically listens to RabbitMQ.Client.Publisher as the Api host does, captures the client publish activity by producer TraceId, and asserts the chain above in T030/T031; re-run the trace and redelivery negative controls (FR-009) and the full `dotnet test`. Removing the process-global ContextInjector mutation also closes Compass F2. Docs brief-first (brief Q3 rows 44/61/88), then one Quill fix list for spec SC-002, contracts/trace-contract.md, data-model.md, checklists/trace-proof.md and ADR 0019 Decision; FR-003 already matches. This is the round-1 fix within the standing two-round cap; Sentry report completeness is a recon item for Bernstein and does not block the remedy.
