# DEV-315 grill conclusions

Worktree: F:/Dev/LamuFlix.worktrees/feature-315-spec; branch: feature/315-spec.
Phase 2, size:M. Question cap: 12. Append-only log.

## Q1 — Outcome translation boundary

### Full question

[from Keel] /grill-with-docs DEV-315 Q1/12. Worktree F:/Dev/LamuFlix.worktrees/feature-315-spec; branch feature/315-spec; task note DEV-315; recon recon-DEV-315. Read those notes, task-pipeline-2 Phase 2, lamuflix-team-charter-2 and specs/PRODUCT.md in that worktree. Begin at Q1, prior attempt had zero questions/writes. Please rule on the outcome translation boundary: I recommend retain existing Completed(Claimed true or false) => BasicAck (cover Enriched, NotFound, refused claim), Failed Retry/RetryDelayed => publish existing enrichment.retry route with NextAttempt from core, then ack only after successful publish, Failed DeadLetter => publish enrichment.dead-letter then ack. Ticket wording BasicAck + republish specifies actions, not unsafe ack-first ordering; recon lines 23,30-32,53,56,59 show existing core/routing separation and publish-before-ack. Cost: real-broker proofs and targeted gaps rather than a redundant new translator or policy. Preserve malformed-message terminal nack and cancellation requeue as existing behavior outside the outcome table. ACCEPT/AMEND/REJECT with cited basis; explicitly flag any ticket contradiction/constitution departure as blocked: structural. One decision per turn, no independent searches, code, gates or writes; Keel records each full exchange in specs/DEV-315/CONCLUSIONS.md before advancing. Facts come from recon-DEV-315; if missing, needs recon and stop. Reply via this ask or ask Keel #2 back.

### Full Patron answer

[from Patron] DEV-315 Q1 ruling: AMEND. Accept the translation boundary as Keel recommends, with one addition: the dead-letter publish must carry failure headers.
- Basis (accept): ticket Description maps Enriched/NotFound/Claim Failed to BasicAck, retryable Attempt<Max to republish enrichment.retry Attempt+1, and fatal or Attempt>=Max to enrichment.dead-letter. It names the actions but not their order. EnrichmentConsumer.SettleAsync (src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs:172-193) already calls EnrichmentRouting.Decide, publishes, and only then acks, so publish-before-ack is the ticket's at-least-once reading and is not a contradiction. Do not add a new translator or policy type; that would be a new layer with no AC behind it (charter 2.3 item 2).
- Basis (amend): the ticket text says publish enrichment.dead-letter with failure headers. Current SettleAsync republishes only a bare EnrichmentRequested(MovieId, NextAttempt ?? Attempt) with no headers. The spec must name the header set (failure kind/reason, attempt) as a FR plus a real-broker assertion. The header names are a message-contract shape, so give the exact names in the spec as a Patron 2.3 item 4 ruling; that is in scope, not blocked.
- Basis (out of table): keep malformed body nack(requeue:false) (EnrichmentConsumer.cs:124-131), unexpected-exception nack(requeue:false) (148-156), and stopping-token nack(requeue:true) (140-146) as existing behaviour outside the outcome table. Prove them, do not change them. Proofs are Testcontainers RabbitMQ, one per routing path, per the ticket text. Not blocked: no ticket contradiction and no constitution departure.
- Unverified: recon-DEV-315 is not connected to Patron #2, so I checked the cited lines against the worktree code instead of the note.

### Recorded decision and implications

AMEND: Completed outcomes acknowledge; retry outcomes publish to the existing retry route with the core-provided next attempt before acknowledging; terminal outcomes publish to the existing dead-letter route with failure headers before acknowledging. No new translator or policy layer. Preserve malformed-body, unexpected-exception and stopping-token settlement behaviour and prove it with real-broker tests.

Basis: DEV-315 Description; recon-DEV-315:22-25,30-32,53-60; PRODUCT.md sections 2-3; charter section 2.3.

Exact failure headers remain undecided. Patron disclosed that recon was not connected to its seat and inspected code instead. Its claim that current republishing has no failure headers is not adopted as a verified recon fact. Request authoritative recon of publisher/header contract and fixture before Q2. No owner escalation identified in Q1. No taste ruling occurred.

---

## Patron charter ruling — dead-letter failure-header shape (before Q2)

### Question (Conductor)

Does choosing failure-header names and types for the RabbitMQ dead-letter publish trigger charter §2.3 item 4 (public API shape not already in spec.md), or is it an internal transport detail within the ticket? Recon-DEV-315:99: no terminal-failure headers exist today, no failure category or reason is carried through outcomes, and the existing publisher headers are telemetry only.

### Ruling

Within Patron authority. Not blocked. The ticket is unchanged.

- Not §2.3 item 4. Item 4 covers a public API (route, DTO field set, status codes). The contract chain is C# DTO → OpenAPI → TS. The enrichment.dead-letter queue is internal Worker topology (PRODUCT.md §Worker line 10). Nothing outside the system consumes it and it never reaches OpenAPI. It is a message-contract detail that the ticket itself asks for ("publish enrichment.dead-letter with failure headers", DEV-315 Description). Because spec.md does not yet name the headers, Patron records the shape here deliberately rather than leaving it to an assumption.
- Not a ticket change. Carrying the failure category from Core to the transport is forced by that ticket AC. Today the handler has `EnrichmentFailureCategory` in scope at dead-letter time (src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs:60-77) but drops it from `ProcessEnrichmentOutcome.Failed`/`EnrichmentFailureDecision`. Adding the category to the Failed outcome is an in-scope Core type change. It is not a schema change and adds no new layer, and Q1 forbids adding one.
- Not a constitution departure. The values are closed codes only, in line with the caller-safe category contract (ADR-0006, DEV-295; src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs). No owner escalation is needed.

### Allowed shape (dead-letter publishes from SettleAsync only)

| Header | AMQP type | Value |
|---|---|---|
| `x-lamuflix-failure-category` | string (UTF-8) | `EnrichmentFailureCategory.Code`: `provider_unavailable`, `rate_limited`, `invalid_response`, `unknown` |
| `x-lamuflix-failure-reason` | string (UTF-8) | `non_retryable` when `!IsRetryable`; `max_attempts_exhausted` when retryable but Attempt >= MaxAttempts (the two ticket triggers) |
| `x-lamuflix-failure-attempt` | int32 | the request Attempt that failed |

Constraints:
- No free text. That means no exception message, no `SafeDescription`, no provider payload, no PII or secrets.
- Do not write broker-owned `x-death`/`x-first-death-*`.
- Existing telemetry headers stay unchanged.
- Retry publishes carry no failure headers.
- Out-of-table paths are unchanged and get no failure headers: malformed-body nack, unexpected-exception nack and stopping-token requeue.
- Each header needs one real-broker assertion on the dead-letter path.

Basis: DEV-315 Description; recon-DEV-315:99; charter §2.3 items 2 and 4; PRODUCT.md line 10; ADR-0006.

## Q2 — Failure category propagation

### Full question

[from Keel] DEV-315 Phase 2 Q2/12: failure category propagation through the existing outcome. Worktree F:/Dev/LamuFlix.worktrees/feature-315-spec; branch feature/315-spec; actual HEAD 1312a41f85630b49471e5b80291d8d7665cd60fa (matches the recorded pre-Q2 ruling commit; supplied 01caa4d is the earlier base). Read DEV-315, recon-DEV-315 and specs/DEV-315/CONCLUSIONS.md, preserving Q1 and the three settled closed-code headers. I recommend make existing ProcessEnrichmentOutcome.Failed carry the actual EnrichmentFailureCategory alongside its existing Decision, populated by ProcessEnrichmentCommandHandler on every Failed return. Retain EnrichmentFailureDecision and retry policy action/NextAttempt responsibilities, with no new layer. Consumer emits the settled terminal category code, reason non_retryable when category is not retryable otherwise max_attempts_exhausted for a terminal outcome, and current request Attempt; it does not recompute or override core routing decisions. Retry publishes carry no failure headers. This keeps causal classification available at the transport boundary without exposing raw error text; cost is updating Failed construction sites and their tests. Basis recon:53,56,59,69,75,99 plus recorded pre-Q2 header ruling. Is this minimal propagation approach ACCEPT/AMEND/REJECT? Cite basis and clarify whether any additional failure-producing caller must propagate category; if that requires a missing fact, return needs recon rather than searching. One question only in this ask; no code, tests, gates, formatting or Quill drafting. Please reply full ruling through this ask or a named response file.

### Full Patron answer

[from Patron] DEV-315 Q2 ruling: ACCEPT. Add the actual EnrichmentFailureCategory to the existing ProcessEnrichmentOutcome.Failed next to Decision. No new layer, and EnrichmentFailureDecision plus the retry policy keep their current responsibilities.
- Basis: the pre-Q2 header ruling in CONCLUSIONS.md (commit 1312a41) found that the category is in scope in ProcessEnrichmentCommandHandler.FailAsync (src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs:60-77) but is dropped from Failed. The ticket's failure-headers AC forces carrying it. That is an in-scope Core type change, not a schema change, and not a new layer (charter 2.3 items 2 and 3).
- Consumer: emit headers only when EnrichmentRouting.Decide returns the dead-letter disposition, and never recompute routing. The reason is non_retryable when !Category.IsRetryable, otherwise max_attempts_exhausted. Attempt is the current request Attempt. Retry publishes carry no failure headers.
- Other callers: none in production. A worktree grep at 1312a41 finds exactly two production construction sites, both in FailAsync (ProcessEnrichmentCommandHandler.cs:70 retry, :77 dead-letter). Test sites to update are ProcessEnrichmentCommandHandlerTests.cs:164,184,204,226,246,290 and EnrichmentRoutingTests.cs:46,61. RecordEnrichmentFailureCommandHandler calls Movie.MarkFailed but returns no ProcessEnrichmentOutcome, so it is out of scope. Not blocked: no ticket change and no constitution departure.

### Recorded decision and implications

ACCEPT: add actual EnrichmentFailureCategory to existing ProcessEnrichmentOutcome.Failed alongside Decision. ProcessEnrichmentCommandHandler propagates it on Failed returns. EnrichmentFailureDecision and retry policy retain their responsibilities. Consumer writes the three settled headers only for the dead-letter disposition returned by EnrichmentRouting.Decide and never recomputes routing. Reason is non_retryable for !Category.IsRetryable, otherwise max_attempts_exhausted. Attempt is current request Attempt. Retry publishes carry no failure headers.

Basis: DEV-315 failure-header acceptance criterion; recon-DEV-315:53,56,59,69,75,99; pre-Q2 header ruling at commit 1312a41; charter section 2.3 items 2-3. No new layer or schema change, no owner escalation, no taste assumption.

Evidence limitation: Patron reported a fresh worktree grep despite the recon-only brief. Its exact construction-site count, complete test-site list and RecordEnrichmentFailureCommandHandler exclusion are recorded above as the full answer but are not adopted as verified facts until Wisp records them in recon-DEV-315. That limitation does not change the accepted category-propagation decision.

Q2 exchange is persisted before any later question. Grill now 2/12; not closed.

---

## Q3 — Publisher extension

### Full question

[from Keel] DEV-315 Q3/12 publisher extension decision. Worktree F:/Dev/LamuFlix.worktrees/feature-315-spec; branch feature/315-spec; HEAD 1312a41f85630b49471e5b80291d8d7665cd60fa verified. Preserve Q1, Q2 and pre-Q2 three closed-code header ruling. Read connected recon-DEV-315 and specs/DEV-315/CONCLUSIONS.md. Recon:99 establishes publisher currently creates telemetry-only headers, enabled/tracked confirmations and mandatory publishing; Conductor supplies current PublishAsync(EnrichmentRequested, string, CancellationToken) and IEnrichmentQueue.EnqueueAsync(EnrichmentRequested, CancellationToken). I recommend retain existing PublishAsync signature and add a concrete-publisher overload accepting IReadOnlyDictionary<string, object?> additionalHeaders before CancellationToken, with existing overload delegating to it without extra headers. Consumer constructs exactly the three ruled terminal headers and calls this overload only for dead-letter outcome; retry/requested calls use the existing path. Publisher copies these additions into its per-publish telemetry header dictionary, preserving telemetry values and existing confirmation/mandatory behaviour; caller dictionary is not mutated. Keep IEnrichmentQueue unchanged, no new interface/type/layer/dependency. Cost: one overload and merge path plus terminal invocation, tested for headers, telemetry preservation and no retry failure headers. These existing-file edits are forced by ticket failure-header AC (charter 2.3 item 6). ACCEPT/AMEND/REJECT with cited basis? Do not search files or run code; use supplied recon facts and decisions only. One decision for Q3; return ruling via this ask or a named response file. Note my connected recon read currently returned 99 lines rather than reported 101; do not infer missing exhaustive call-site facts.

### Full Patron answer

[from Patron] DEV-315 Q3 ruling: AMEND. Accept the overload approach: keep PublishAsync(EnrichmentRequested, string, CancellationToken), add a concrete-publisher overload that takes IReadOnlyDictionary<string, object?> additionalHeaders before the CancellationToken, and have the existing overload delegate to it with no additions. IEnrichmentQueue stays unchanged, with no new interface, type, layer or dependency. The amendment is a merge rule.
- Basis: the edit is forced by the ticket's failure-headers AC on existing files (charter 2.3 item 6). Q2 kept routing in core and kept failure headers off retry publishes, so only the dead-letter call passes headers. Recon:99 (telemetry-only headers, tracked confirmations, mandatory publishing) is preserved unchanged.
- Amend (merge rule): build a fresh per-publish dictionary and never mutate the caller's. Telemetry keys are written by the publisher and are never overwritten. If an additional key collides with a telemetry key or with a broker-reserved x-death or x-first-death-* key, throw ArgumentException, so that a collision cannot silently replace trace context. Null values are rejected the same way. The three ruled header values are never null.
- Tests: the dead-letter publish carries exactly the three ruled headers with telemetry intact; the retry and requested publishes carry no failure headers; a collision throws; the caller's dictionary is unchanged. Not blocked: no ticket change and no constitution departure.

### Recorded decision and implications

AMEND: preserve PublishAsync(EnrichmentRequested, string, CancellationToken); add a concrete-publisher overload with IReadOnlyDictionary<string, object?> additionalHeaders before CancellationToken. Existing overload delegates with no additions. IEnrichmentQueue stays unchanged. Only the terminal outcome call supplies the three settled failure headers. No new interface, type, layer or dependency.

Build a fresh per-publish dictionary. Do not mutate caller additions or overwrite telemetry. Reject telemetry-key collisions, broker-owned x-death or x-first-death-* keys, and null values with ArgumentException. Keep existing mandatory publishing and tracked confirmations.

Test implications: broker-observed terminal headers with telemetry intact; requested/retry absence of failure headers; collision and null rejection; caller dictionary unchanged. Exact test implementation and file scope await the later test/plan decisions.

Basis: DEV-315 failure-header AC; recon-DEV-315:99; Q1/Q2 and pre-Q2 header ruling; charter section 2.3 item 6 (existing-file edit forced by AC). No ticket change, constitution departure, owner escalation or taste assumption.

Recon read at Q3 returned 99 lines, not the 101 reported by Conductor. The signatures supplied directly in the Conductor brief are explicitly attributed in the question. Exhaustive call-site inventory is not inferred from the missing appendix. Conductor must reconcile the connected note before file-scope freeze.

Q3 exchange persisted before any subsequent question. Grill 3/12; not closed.

---

## Q4 — Real-broker routing, TTL and boundary proof strategy

### Full question

[from Keel] DEV-315 Q4/12: real-broker routing, TTL and attempt-boundary proof strategy. Worktree F:/Dev/LamuFlix.worktrees/feature-315-spec; branch feature/315-spec; HEAD 1312a41f85630b49471e5b80291d8d7665cd60fa verified. Read recon-DEV-315, DEV-315, specs/DEV-315/brief.md and CONCLUSIONS.md. Preserve Q1-Q3. I recommend use existing Testcontainers RabbitMqFixture (recon:99), production consumer/topology/publisher and core handler/policy, with controlled provider outcomes and isolated broker state, to prove the entire ticket matrix: Enriched, NotFound and refused claim drain requested without retry/DLQ; transient and rate_limited at Attempt < Max publish retry with same MovieId and Attempt+1, retain telemetry and no failure headers, then return through broker TTL to requested/provider invocation; terminal non-retryable below Max, retryable exactly Max, and retryable above Max reach DLQ with unchanged failed Attempt and all three ruled headers, no further provider calls/retry loop. Include Max-1 retry crossing to Max terminal to prove bounded poison handling. Observe broker retry ingress independently of eventual provider recall (pause requested consumption or use bounded handshake before TTL transfer); provider recall alone is not TTL-route proof. Use configured short positive integer-millisecond TTL, bounded polling and cancellation/event handshakes, no sleep-only success assertion or exact millisecond timing. Observe settled queues/consumer lifecycle for ack evidence and distinguish requested ready from in-flight unacked state; unit policy tests alone are insufficient. Keep existing failed-publish, malformed, unexpected-exception and cancellation regression proofs from Q1; preserve their behaviour rather than redesign it. Cost: strengthened real-broker cases and synchronization, no new fixture/dependency or topology change established. ACCEPT/AMEND/REJECT this proof strategy with cited basis? This is one decision, not file-scope freeze. My recon still reads 99 lines, so no missing caller inventory is inferred. No independent searches, code, tests, gates, analyzers, formatting or Quill drafting. Return full ruling via ask or named file.

### Full Patron answer

[from Patron] DEV-315 Q4 ruling: ACCEPT the real-broker proof strategy as recommended, with three clarifications that do not change it.
- Basis: the ticket text requires Testcontainers RabbitMQ integration tests for each routing path and names the acceptance as no endless poison loops, transient/rate-limited retry through the TTL queue, and fatal errors to the DLQ. Your matrix, including the Max-1 to Max crossing and broker retry ingress observed separately from provider recall, proves each of those claims rather than inferring them. No new dependency is needed. I checked this in the worktree at 1312a41: tests/LamuFlix.Tests.Common/RabbitMqFixture.cs:8,33 is Testcontainers.RabbitMq (rabbitmq:4-management-alpine), and Testcontainers.RabbitMq 4.15.0 is already in Directory.Packages.props:14 (charter 2.3 item 1 is not triggered). Treat the earlier not-Testcontainers wording around recon:99 as superseded by this.
- Clarifications: (1) Isolation means each test gets its own queue/exchange names or a purge before use, so a shared fixture never leaks messages between tests. Do not add a fixture or change the topology. (2) Set MaxAttempts to a small value through options, and drive the TTL through config at a short positive integer of milliseconds. (3) Cases that share a routing path (Enriched/NotFound/refused claim; transient/rate_limited; the three terminal triggers) go in [Theory] rows (vendored testing rule: prefer Theory), with assertions per row and not merged.
- The Q1 regression proofs (failed publish, malformed, unexpected exception, cancellation) are kept as preserve-behaviour cases. Not blocked: no ticket change and no constitution departure.

### Recorded decision and implications

ACCEPT the proposed strategy. Reuse existing Testcontainers RabbitMqFixture and production consumer/topology/publisher/core routing. Per-row Theory cases prove Enriched, NotFound and refused claim acknowledgement; transient and rate_limited retry below Max with same MovieId, Attempt+1, intact telemetry and no failure headers; fatal below Max, retryable at Max and retryable above Max terminal DLQ with unchanged Attempt and all three failure headers. Include Max-1 retry crossing to Max terminal. Observe retry ingress separately from provider recall and prove actual broker TTL return. Queue/lifecycle observations must distinguish ready from in-flight messages for acknowledgement evidence.

Isolation: independent queue/exchange names or purge before use, preventing shared-fixture message leakage. Do not introduce a fixture or change production topology. Use small MaxAttempts via options and short positive integer-millisecond TTL via configuration. Use bounded polling and cancellation/event handshakes; no sleep-only success evidence or exact timing assertion. Each Theory row keeps its own assertions. Retain Q1 preserve-behaviour regressions for failed publish, malformed body, unexpected exception and cancellation.

Basis: DEV-315 Description and acceptance criteria; recon-DEV-315:22-25,36-42,46,49-53,79-99; Q1-Q3; Patron cited vendored testing rule preference for Theory. Fixture reuse and the existing package pin are already supported by recon:99. Patron independently checked the fixture again; its additional image-tag detail is not needed or adopted as verified plan evidence.

No new dependency, topology change, owner escalation or taste assumption. File scope remains open until the connected recon appendix is synchronized. Q4 persisted before any subsequent question. Grill 4/12; not closed.

---

## Q5 — Loop closing bar, behavioural scope and caps

### Full question

[from Keel] DEV-315 Q5/12: approve the loop discipline contract. Worktree F:/Dev/LamuFlix.worktrees/feature-315-spec; branch feature/315-spec; verified HEAD 1312a41f85630b49471e5b80291d8d7665cd60fa. Preserve Q1-Q4 and the settled internal header contract. I recommend Critical, High and Medium verified in-scope findings block code-review/ship-review closure; Low findings are non-blocking and recorded, never silently dropped. Missing required axis reports, failed applicable gates, Could not run, missing real-broker acceptance proof, or unresolved owner escalation cannot be treated as closure; apply pipeline non-blocking exit-2 distinctions without relabeling them PASS. Frozen behavioural scope: existing worker outcome-to-ack/retry/DLQ translation; publish-before-ack; category propagation on existing Failed; the three closed-code terminal headers and publisher overload/guard rules; real-broker acknowledgement, retry ingress/TTL return, bounded Attempt boundaries and terminal DLQ proofs from Q4; preserve existing malformed/unexpected-exception/cancellation/failed-publish paths. No new architecture, policy, dependency, fixture, production topology, database schema, HTTP/OpenAPI/frontend or local-play changes are part of this round; anything else is a follow-up issue, not a finding in this round. Cap review at two rounds and remediation at two fix commits per round; third-round findings go to owner review on PR, never count as clean or authorize merging, and above-bar findings remain unresolved blockers. This freezes behaviour only, not the file list: connected recon still reads 99 lines and must be synchronized before file-scope freeze. Cost: potentially report blocked rather than reopen scope or chase an endless review loop. Basis task-pipeline-2 sections 2.2, Phase 4 and Phase 5 merge bar; grill-with-docs Loop Discipline; charter 2.3; DEV-315 AC and Q1-Q4. ACCEPT/AMEND/REJECT this closing bar/scope/cap contract with cited basis? One decision only. No searches, code, tests, gates, analyzers, formatting or Quill drafting; reply full ruling via ask or named file.

### Full Patron answer

[from Patron] DEV-315 Q5 ruling: AMEND. Accept the closing bar, the frozen behavioural scope and the caps as written, with one correction to how out-of-scope findings are routed.
- Basis (accept): task-pipeline-2 Phase 4 and step 6 (review at most 2 rounds, remediation at most 2 fix commits per round, third-round findings go to user review on the PR). The Phase 5 step 6 merge bar says exit 1 and Could not run never meet the bar, and only the listed exit-2 cases are non-blocking. The frozen scope matches Q1-Q4 and DEV-315 AC. The Critical/High/Medium-block, Low-recorded bar is this ticket's above-bar definition for the findings-artifact-clean check. It is cited once in brief.md and not re-ruled per round.
- Amend: an out-of-scope finding becomes a follow-up ticket only when it is Critical or High, is broken behaviour, or the user asked for it. Everything else is one line in CONCLUSIONS.md: noted, no ticket (charter, Patron duties). Patron decides and Rigger files, after checking open tickets so it can be folded into an existing one first. Out-of-scope findings never block this round.
- File-scope freeze stays open until recon is synchronized, as you state. Not blocked: no ticket change and no constitution departure.

### Recorded decision and implications

AMEND: Critical/High/Medium verified in-scope findings block code-review/ship-review closure; Low findings are recorded and non-blocking. Missing required axis reports, failed applicable gates, Could not run, missing real-broker acceptance evidence or unresolved owner escalation prevent closure. Preserve permitted non-blocking exit-2 distinctions without reporting them PASS.

Review cap: two rounds. Remediation cap: two fix commits per round. Third-round findings go to owner review on the PR; above-bar findings remain unresolved blockers and the cap cannot produce a clean verdict or authorize merging.

Behavioural scope is the Q1-Q4 worker translation/category/terminal-header/publisher-guard contract and real-broker acknowledgement, TTL, attempt-boundary and terminal proofs, preserving existing malformed/exception/cancellation/failed-publish behaviour. No new architecture, policy, dependency, fixture, production topology, database schema, HTTP/OpenAPI/frontend or local-play work. File scope remains unfrozen pending synchronized recon.

Out-of-scope observations do not block this round. Patron decides follow-up recording; Rigger alone files after checking existing tickets for folding. Ticket eligibility is Critical/High, broken behaviour or explicit user request. Otherwise append "noted, no ticket" to CONCLUSIONS.md. Thus the frozen-scope ending means routing out of this round, not automatically creating a ticket.

Basis: task-pipeline-2 section 2.2 and Phases 4-5; charter section 2.3/Patron recording duties; grill-with-docs Loop Discipline; DEV-315 AC; Q1-Q4. No taste assumption, owner escalation or ticket/constitution departure.

Q5 persisted before any further question. Grill 5/12, open. File list, task ordering, gate expectations and remaining test mapping are not frozen.

---

## Q6 — File mapping, ordering, gates and grill closure

### Full question

[from Keel] DEV-315 Q6/12: final file map, task ordering and gate expectations. Verified worktree F:/Dev/LamuFlix.worktrees/feature-315-spec, branch feature/315-spec, HEAD 1312a41f85630b49471e5b80291d8d7665cd60fa. Targeted recon read 100 29 now succeeds with all lines 100-128, resolving the blocker; use that and recon:17-99, plus Q1-Q5 in CONCLUSIONS.md. I recommend freeze eight implementation files: (1) src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentOutcome.cs category on Failed; (2) same directory ProcessEnrichmentCommandHandler.cs propagate category at both sites; (3) src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs construct terminal-only three headers; (4) same directory RabbitMqEnrichmentQueuePublisher.cs overload and guarded fresh header merge; (5) tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs update six constructions and assert category propagation; (6) tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs update two constructions and retain decision mapping; (7) tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs Q4 broker Theory matrix and preserve regression cases; (8) new tests/LamuFlix.IntegrationTests/RabbitMqEnrichmentQueuePublisherTests.cs broker headers/telemetry and requested/retry absence plus collision/reserved-key/null rejection and input-dictionary preservation using existing fixture. New file is a proposal, not an existence claim. No other production/test/config file, no interface/policy/topology/fixture/package/schema/HTTP/web edit; RecordEnrichmentFailureCommandHandler excluded per recon:112-113. No new layer/type/dependency. Cost: one focused integration test file and targeted updates to existing tests. Task ordering: Phase 1 category/outcome with handler/routing tests; Phase 2 publisher overload/guard tests plus consumer header invocation; Phase 3 consumer broker matrix/regressions; then Cog refactor and Gauge pipeline gates, never Quill/Keel running gates here. Expected Phase B checks: Roslyn, complexity, InspectCode, property tests with configured applicability, vulnerable packages, format, full dotnet test and mutation per task-pipeline-2 Phase 3; thresholds from harness.yml, no threshold changes, no mutation since; scope-empty SKIPPED, disabled SKIP and exclusion N/A retain their labels; fail/Could not run blocks. M requires all three review axes and Q5 two-round limits. Phase A follows Quill specify/plan/tasks then Keel read-only analyze, plan challenge, spec PR/Gate1 before build. ACCEPT/AMEND/REJECT this plan and confirm shared understanding/closure if complete, citing ticket/recon/bases? File edits forced by AC are deliberate charter item 6 ruling; do not infer further files. One Q6 decision, preserve prior rulings and no searches/code/tests/gates/analyzers/formatting/Quill drafting. Reply full ruling via ask or named file.

### Full Patron answer

[from Patron] DEV-315 Q6 ruling: AMEND. The file map, phase ordering and gate expectations are accepted, with two corrections. With these the grill can close: shared understanding confirmed.
- Basis (accept): files 1-7 are the edits forced by the ticket's failure-headers and per-path Testcontainers AC (charter 2.3 item 6). They follow Q1-Q5, and RabbitMqFixture is reused (Q4). RecordEnrichmentFailureCommandHandler is excluded (Q2; recon:112-113). Phase ordering and Phase B gates follow task-pipeline-2 Phase 3-5 using the harness.yml thresholds, Q5 caps and the M three-axis review.
- Amend 1 (file 8): do not create RabbitMqEnrichmentQueuePublisherTests.cs. tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs is already tracked (git ls-files at 1312a41). Put the Q3 publisher cases in that file (headers and telemetry, no failure headers on requested and retry, collision, reserved-key and null rejection, caller dictionary unchanged). That keeps 8 files with 0 new files. A duplicate publisher test class is not allowed.
- Amend 2 (harness.yml): the worktree has an uncommitted change to tracked harness.yml, gates web enabled: true to false. DEV-315 does not name that file and no AC forces the change (charter 2.3 item 6). It also would turn the web gate into a disabled SKIP that it has not earned. Ruling: restore harness.yml to origin/main before any commit. It must not ride on the spec or build branch. The .specify/feature.json pin to specs/DEV-315 is the expected intake edit and is kept. Not blocked: no ticket change and no constitution departure.

### Recorded decision and implications

AMEND, shared understanding confirmed; grill closes at six questions of the twelve-question cap. Accept files 1-7 and replace proposed new file 8 with tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs. Approved map is eight existing files, zero new files. Q3 publisher cases belong in file 8, not a duplicate class. Category/outcome plus unit updates precede publisher/consumer changes and publisher tests, followed by broker matrix/regressions, Cog refactor and Gauge gates. Phase A still requires Quill drafting, read-only analyze, plan challenge and merged spec Gate 1 before build.

Gate expectations: task-pipeline-2 Phase 3-5 with harness thresholds unchanged, no mutation since, truthful applicability labels, all M review axes and Q5 limits. No code or gates were run in this grill.

Recon synchronization resolved by targeted read of recon-DEV-315:100-128. Those readable signatures/construction sites now support Q2 propagation and RecordEnrichmentFailureCommandHandler exclusion.

Patron newly supplied two independent-search facts: existing RabbitMqPublisherTests.cs and the exact harness.yml web-gate delta. The accepted file choice is recorded, but tracked-file identity and harness provenance await Wisp recon before handoff/commit. Do not treat Patron searches as the required recon receipt.

Patron ruling: harness.yml is outside DEV-315 scope and must not ride on spec/build commits; preserve .specify/feature.json intake pin. Patron instructed restoration to origin/main. Keel does not edit config or discard pre-existing unrelated changes: Conductor routes provenance/reconciliation to Wisp/Rigger before any restoration. This ruling changes no ticket or constitution and has no owner checkbox.

Basis: DEV-315 failure-header/per-route Testcontainers AC; Q1-Q5; recon-DEV-315:17-99,100-128; charter section 2.3 item 6; task-pipeline-2 Phases 2-5. No taste assumptions. No new ADR or glossary term was decided.

Full Q6 exchange persisted before declaring grill closed. Decision work is complete; documentary handoff is pending the two recon receipts and safe config reconciliation.

---

## Phase 2 step 6 adjudication receipt (2026-10-08)

All three challenge axes read at HEAD 1312a41f85630b49471e5b80291d8d7665cd60fa. Q1-Q6 and the eight-existing-file implementation map remain unchanged. Consolidation is in connected note adjudication-DEV-315-plan; plan freeze is blocked pending concrete failed-republish test and client/broker evidence, not a verified production-loss verdict.

Out-of-scope observations: Compass I1 / Ledger PRODUCT wording observation (PRODUCT.md:9 versus constitution I:95-99,125-126): noted, no ticket; the DEV-315 plan already follows the constitution. Sentry F3 sweeper interaction: noted, no ticket; recon:59 does not establish claim-time attempt/last-attempt writes or a broken sweeper loop, and Q4/Q6 do not authorize sweeper/repository changes. No proven broken behaviour or ticket change is adopted. Compass I2 combined US4 scenario formatting: noted, no ticket; cosmetic and non-blocking.

## Patron F1/F2 proof amendment (2026-10-08, post bounded recon)

Ruling: AMEND the settled proof, within ticket scope. F1 and F2 are not retained as blockers. Ticket deliverables, Q1-Q6 behaviour, header contract, fixture, production topology and the eight-existing-file map are unchanged. No owner checkbox: the ticket text and acceptance are unchanged and the constitution is not departed from.

- F1 (terminal-DLQ republish failure): drop the separate real-broker injection/observation requirement. Failed retry and failed DLQ republish share one consumer catch path (EnrichmentConsumer.cs:135-157 -> DeadLetterAsync nack requeue:false at :198-199; recon-DEV-315:182). The existing broker test EnrichmentConsumerTests.cs:190-209 stays as the failed-publish regression proof (Q4 row 5: keep the existing proofs, add none). MarkFailed and SaveChanges happen before the terminal publish (ProcessEnrichmentCommandHandler.cs:73-77; recon:196), which satisfies constitution IV:185-186 without a new proof. Under unchanged topology the broker behaviour when the DLQ target is unavailable cannot be observed (recon:192,203-204). So spec US4 scenario 4's DLQ-republish clause is a documented shared-path expectation, not an acceptance proof.
- F2 (pinned-client nack observable): drop as a DEV-315 proof requirement. Awaited mandatory publishing with confirmation tracking is the settled DEV-18 contract (ADR-0004:42 Accepted; specs/DEV-18/spec.md:139 FR-003; recon:177-179). DEV-315 keeps that publishing unchanged (FR-08) and does not change failure semantics. The existing unroutable assertion RabbitMqPublisherTests.cs:84-92 stays.
- Out of scope: the terminal-publish-failure broker transfer and an end-to-end test of the client nack branch. Noted, no ticket. Neither is shown to be broken behaviour (adjudication-DEV-315-plan:70).

Authorized Quill fixes (only these): (1) spec.md:67, US4 scenario 4. Keep the retry-republish clause as the real-broker proof. Reword the DLQ-republish clause as a shared catch-path expectation: never success-acked, terminal nack requeue:false, recorded Failed state unchanged. Add that it has no separate broker-injection proof under the fixed fixture and topology. (2) tasks.md:45 T012: limit the assertion to the existing retry-republish failure test. Add no terminal-publish-failure injection and no client-nack test. (3) spec.md FR-10 and plan.md, where they cite failed-publish proof: cite this amendment and ADR-0004:42 for the client contract. Then Keel rechecks.

Basis: DEV-315 Description/AC (Testcontainers test per routing path: ack, retry, DLQ; these are unchanged and fully proven); Q1, Q4 row 5, Q5 frozen scope, and the Q6 file map; recon-DEV-315:174-213; adjudication-DEV-315-plan:67-78; charter section 2.3.
