# DEV-390 decision brief

## Status and authority

Grill CLOSED: Patron accepted Q1-Q6, six of the maximum twelve questions. Full questions, recommendations, costs, rulings, rationales and implications are preserved in CONCLUSIONS.md. No unanswered question, structural escalation or owner checkbox remains. Gate 1 is still closed until the user merges the later spec PR; this brief does not authorize implementation.

Worktree: F:/Dev/LamuFlix.worktrees/feature-390-spec  
Branch: feature/390-spec  
Phase A base/HEAD: 0faa4f25bc91f7fa6f54827c66687f0da91e618a  
Task and factual sources: shared notes DEV-390 and recon-DEV-390.

DEV-390:38-46 is authoritative. PRODUCT.md sections 3 and 5 and the explicit seat assignment govern decisions. The shared charter:9-17,28 and task-pipeline:45,67 retain stale escalation wording: care-list items require deliberate Patron rulings, not automatic user checkboxes. Only a ticket-scope change or necessary constitution departure escalates. This brief neither changes those notes nor assumes either escalation.

Phase A edits in this ask are exclusively this brief, CONCLUSIONS.md and ASSUMPTIONS.md. Preserve the existing uncommitted .specify/feature.json pin to specs/DEV-390. No Spec Kit, source, glossary, historical artifact, tracker, commit or PR edits in this ask.

## Accepted contract and every grill answer

### Q1 — Counter meaning (ACCEPT)

EnrichmentAttempts / enrichment_attempts is the monotonic cumulative count of successful atomic claims: processing attempts started. It starts at zero for a new movie and never resets. A successful TryClaimForEnrichmentAsync adds one; a refused claim adds zero. MarkEnriched, MarkNotFound, MarkFailed and RequestEnrichment add zero. A claim counts even when provider work retries, crashes, times out or never completes. A later successful lease re-claim adds another one. No separate completed-outcome counter is introduced.

Basis: DEV-390:39-45; recon:35-50,136-143; Patron Q1. Keep DEV-301 increment-on-claim SQL and delivered acceptance intact.

The new spec must contain an explicit Supersedes section naming the older domain counting contract: DEV-294 CONCLUSIONS.md:45,53,57; DEV-299 CONCLUSIONS.md:17; DEV-301 brief.md:39 and Q7 domain-count freeze; ADR-0017 completed-outcome terminology. Supersession applies to future behavior under DEV-390, not retroactive rewriting of delivered acceptance. Historical specs and ADRs remain untouched.

### Q2 — Minimal fix and timestamps (ACCEPT)

Remove only EnrichmentAttempts++ from Movie.BeginPendingAttempt. Keep RequirePending, LastAttemptAt = now, and all Mark* status/metadata/EnrichedAt/failure-category behavior. Keep repository claim SQL, D8 claim synchronization and D2 baseline/delta ApplyAttempts unchanged.

Loaded-before-claim example: aggregate count N; DB claim becomes N+1 and synchronizes the tracked record; domain outcome leaves aggregate count N; aggregate-minus-baseline delta is zero; save preserves persisted N+1. Loaded-after-claim example: aggregate and baseline are N+1; outcome leaves both counts unchanged; save preserves N+1.

LastAttemptAt continues its existing claim stamp followed by outcome stamp; this ticket does not redefine leases or timestamp APIs. Adapter simplification is noted, no ticket.

Basis: DEV-390:39-42; recon:35-50,112,141-143; Patron Q2/Q4.

### Q3 — Wire retry and manual retry (ACCEPT)

EnrichmentRequested.Attempt is separate from the persisted count. Manual retry and stranded requeue enqueue wire Attempt 1. Retry policy emits attempt+1 using existing MaxAttempts semantics. Wire Attempt is never copied into or used to reset EnrichmentAttempts.

RequestEnrichmentCommandHandler continues accepting only NotFound/Failed, preserves cumulative history, saves before enqueue and performs no claim. Each subsequent successful initial, Retry, RetryDelayed or lease-reclaim claim increments the persisted count once, even without Mark*. Enqueue/requeue alone adds zero.

No lifetime retry budget, sweeper redesign or delivery deduplication. ADR-0017's existing MaxAttempts-across-sweeps limitation is noted, no ticket. Recon:54 confirms the claim-counting test helper; this closes Patron Q2's unverified item. Real PostgreSQL proof remains mandatory.

Basis: DEV-390:41,44,46; recon:54,66-85,119-130,167-169; Patron Q3.

### Q4 — Files and care-list authorization (ACCEPT with amendment)

Frozen implementation file set:

- src/LamuFlix.Core/Domain/Movie.cs: remove the outcome-side increment only.
- CONTEXT.md: narrow EnrichmentAttempts glossary update and separate wire Attempt term.
- tests/LamuFlix.Test/Domain/MovieTests.cs
- tests/LamuFlix.Test/Domain/PropertyTests.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/ApplyEnrichmentResultCommandHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/RecordEnrichmentFailureCommandHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/RequeueStrandedMoviesCommandHandlerTests.cs
- tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs
- tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs
- tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs

The listed test files are authorized for counter assertions and accepted-contract cases only; they need not all change if their existing assertions already satisfy the contract. New PostgreSQL seam cases belong in EfMovieRepositoryTests.cs using its existing fixtures. No new test file or project is planned. Recon:93-99 confirms the Domain paths, closing Patron Q4's unverified item.

Phase A artifact set: specs/DEV-390/CONCLUSIONS.md, ASSUMPTIONS.md, brief.md; later Quill creates spec.md, plan.md and tasks.md under the same directory.

Patron expressly authorizes item-6 file changes: Movie.cs is ticket-named; glossary and test changes fulfill DEV-390:42,45-46. No care-list item 1-5 trigger is needed: no dependency, project/layer, schema, public route/DTO/status, LocalPlay, secret or Process.Start change.

Keep repository, handlers, wire record, LeaseAwareMovieRepository, migrations/generated models, historical specs/ADRs and public contracts unchanged. Preserve rehydration and persistence-model arbitrary stored-value round-trip assertions. No root glossary edit in this current ask; vocabulary below is ready for Quill, and the narrow CONTEXT.md alignment belongs to implementation after Gate 1.

Amendment: independent adapter simplification is noted, no ticket; do not automatically file a follow-up.

Basis: PRODUCT.md section 5; DEV-390:39-46; recon:87-159; Patron Q4/Q5.

### Q5 — Tests first, real seam and historical values (ACCEPT)

After Gate 1, establish failing exact-value contract tests before the domain fix. PostgreSQL/Testcontainers tests, not an EF mock or only the in-memory helper, must prove:

| Scenario | Exact persisted count |
|---|---|
| Successful claim from N > 0 | N+1 |
| That claim followed by success, not-found or terminal failure | N+1 |
| Refused claim / second refused claim / losing concurrent claim | No increment beyond successful winning claims |
| Retry or RetryDelayed decision after first successful claim, without Mark* | N+1 |
| Next successful claim in retry sequence | N+2 |
| Final outcome after second claim | N+2 |
| Manual retry from NotFound or Failed, save then enqueue Attempt 1 | N |
| Next successful manual-retry claim and final outcome | N+1 |
| Enqueue or stranded requeue alone | Unchanged |

Cover fresh load and aggregate preloaded before claim for each outcome; a watchlist save must preserve the claimed count. Retain double-claim/concurrency proof. Advance the lease through injected TimeProvider; no sleeps. Keep existing manual-retry invalid-state rejection and save-before-enqueue ordering.

Domain tests prove every legal Mark* keeps count N with its existing timestamp and state effects. RequestEnrichment keeps history; rejected transitions preserve it. Update FsCheck transition-table counter invariant to unchanged for domain actions while keeping other expectations. Consumer-helper tests are secondary; use exact counts where deterministic. Keep arbitrary stored-value round trips.

No migration, data correction or backfill. Existing historical overcounts stay stored to preserve monotonicity; future successful claims add one. Consumer documentation must explain that pre-DEV-390 values can overcount historical attempts and cannot be interpreted as a retroactively corrected count.

Basis: DEV-390:42,45-46; recon:47-54,89-131; Patron Q5.

### Q6 — Loop discipline, gates and closure (ACCEPT)

Patron confirmed shared understanding and closure at six questions, with no structural owner checkbox. Two editorial wording assumptions are recorded separately in ASSUMPTIONS.md; the behavioral contract is a deliberate ruling.

Basis: DEV-390:39-46; task-pipeline:14-20,25-28,70-76,91-112; Patron Q6.

## Vocabulary ready for drafting

- Processing attempt: enrichment work admitted by a successful atomic claim. Its counted start does not require a completed outcome.
- EnrichmentAttempts: cumulative successful claims, preserved across manual retries; historical values may include pre-fix overcounts.
- Completed outcome: a success, not-found or terminal-failure domain result; it adds no claim count.
- Transport retry Attempt: the message sequence ordinal, restarted at 1 by manual retry/requeue and advanced by retry decisions.

Use these meanings in the spec. No new ADR is required for this M non-ui ticket. Preserve historical ADRs and explicitly state supersession in DEV-390 spec.md.

## Loop discipline

Closing bar: no unresolved Critical/High finding or correctness, spec, contract, security or required test-seam defect in the frozen diff. Independent lower-severity improvements are non-blocking and noted without tickets unless Patron deliberately rules otherwise. Never silently waive a blocking finding.

Review round cap: two. Remediation cap: two fix commits per round. Third-round unresolved findings accompany the PR for user review. M requires all three Risk, Standards and Spec axis reports regardless of pre-pass tier.

Frozen scope: reconcile the claim counter with Mark* outcomes; distinguish wire and manual retry semantics; align the narrow consumer glossary; prove exact counts with the authorized domain/property/handler/real-seam tests; anything else is a follow-up issue, not a finding in this round.

That scope sentence does not authorize automatic filing: adapter simplification and the existing sweep budget limitation are noted, no ticket.

## Plan approach and task-ordering constraints

1. Quill drafts spec.md, plan.md and tasks.md from this brief, CONCLUSIONS.md, recon and PRODUCT.md, including the explicit supersession section. A gap is needs decision to Keel, not permission to invent work.
2. Keel performs read-only speckit-analyze and checks plan/tasks against this brief; plan challenge follows. No ui design.md or L-only ADR/checklist/clarify work.
3. Open the spec PR through the assigned pipeline seats. Gate 1 is user merge; no implementation before it.
4. Phase B pickup obtains Wisp drift recon and Keel analyze against main before committing to the implementation file set.
5. Write/update domain, property and PostgreSQL exact-count tests first and show the double-count defect failing. Keep the small domain fix and consumer documentation after the failing tests, with passing tests before the implementation handoff.
6. Refactor only within the frozen diff, preserve adapter behavior, then Gauge runs applicable gates.
7. Complete three-axis review, bounded adjudication/remediation, rebase and rebased-head gate receipts, then delivery PR. Never merge or enable auto-merge.

Any missing factual evidence goes to Bernstein as needs recon; do not run independent searches or measurements. No baselines or gate PASS results are claimed by this brief.

## Gate expectations

Use thresholds and configuration from harness.yml unchanged; never lower thresholds or edit generated config. Phase A performs no implementation gates.

Expected Phase B checks: Roslyn analyzers, cyclomatic complexity, InspectCode on the changed diff (no -All), property tests, vulnerable packages, dotnet format --verify-no-changes, dotnet test and scripts/run-mutation.ps1 for changed production Core C#. Mutation is expected applicable; applicability and verdict come from the script, not a waiver. Never use Stryker since from a worktree. Property tests are required for these domain invariants; no propertyTests opt-out. Web checks have no expected scope because /web is unchanged.

Exit 0 records PASS only for the check actually completed. Exit 1 or Could not run blocks. Gate-disabled opt-out records SKIP, never PASS. Mutation NOT APPLICABLE is non-blocking N/A only when the script emits that exit-2 verdict. Scope-empty exit 2 records SKIPPED (scope-empty), non-blocking and never PASS, under task-pipeline:101's standing owner ruling; it overrides stale blocking wording. Unaccepted property-test exit 2 blocks.

## Handoff boundary

This ask ends after creating and reading back brief.md, CONCLUSIONS.md and ASSUMPTIONS.md, with the pre-existing pin preserved. Next authorized pipeline owner: Bernstein dispatches Quill for Phase 2 step 3. No spec/plan/tasks were drafted, no source or root glossary changed, no commit or PR created.
