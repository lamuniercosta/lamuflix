# DEV-390 Patron grill conclusions

Append-only full exchanges. Assignment and PRODUCT.md section 5 govern over stale note escalation text.

---

## Q1 - Persisted counter contract

[from Keel] /grill-with-docs DEV-390. Phase 2 step 1, M ticket; one question at a time, maximum 12. Worktree F:/Dev/LamuFlix.worktrees/feature-390-spec, branch feature/390-spec, HEAD 0faa4f25bc91f7fa6f54827c66687f0da91e618a. Read DEV-390, recon-DEV-390, task-pipeline, lamuflix-team-charter and specs/PRODUCT.md in the named worktree. Facts come from recon-DEV-390; do not search or measure. The explicit assignment and PRODUCT.md section 5 override stale shared-note escalation language: Patron decides care-list items; escalate only ticket-scope changes or necessary constitution departures. Keel writes conclusions after each ruling. Q1: What is the canonical counter contract? I recommend EnrichmentAttempts/enrichment_attempts means cumulative successful atomic claims (processing attempts started), including claims whose provider work retries, crashes or lacks a completed outcome. Each successful claim adds exactly one; refused claims, MarkEnriched, MarkNotFound, MarkFailed and RequestEnrichment add zero; no completed-outcome counter is introduced. This preserves DEV-390:39-46 and recon:47,136-143. Cost: the historical completed-outcome terminology and domain-only Mark* tests must be explicitly superseded for DEV-390, while historical DEV-301 acceptance remains intact. Give ACCEPT/AMEND/REJECT with cited basis and implications, or needs recon. Do not write artifacts or report grill closure yet.

[from Patron] DEV-390 Q1 ruling: ACCEPT.

Verdict: EnrichmentAttempts / enrichment_attempts = cumulative successful atomic claims (processing attempts started). +1 per successful TryClaimForEnrichmentAsync; +0 for refused claims, MarkEnriched, MarkNotFound, MarkFailed, RequestEnrichment. Monotonic, never reset. No completed-outcome counter is introduced.

Basis:
- DEV-390:39-40 - one processing attempt must not count twice, and DEV-301 retains its authoritative increment-on-claim SQL. With the claim SQL fixed as the incrementer, the only reconciliation the ticket permits is removing the domain-side increment (Movie.cs:129-134 BeginPendingAttempt); claim-count is therefore the ticket-mandated meaning, not a taste call.
- DEV-390:42, :45 - monotonic counter; claim + success/not-found/terminal failure = exactly one (recon:180-181 shows today it is 1 + Mark*, i.e. two).
- recon:136-143 - DEV-301 Q7 explicitly deferred this reconciliation here; DEV-301 FR-002 (claim increment) is kept intact, satisfying DEV-390:41 do-not-change-DEV-301-scope.

Implications (binding on the spec):
- Supersession is explicit, not silent (DEV-390:41): spec.md for DEV-390 carries a Supersedes section naming DEV-294 CONCLUSIONS:45,53,57 (each Mark* ++), DEV-299 CONCLUSIONS:17 completed-outcome-count wording, and DEV-301 brief:39 / Q7 freeze of BeginPendingAttempt counting. Historical specs/DEV-294, DEV-299, DEV-301 files are NOT edited; their acceptance stands as delivered history.
- Movie.cs Mark* / BeginPendingAttempt counting change is ticket-named (DEV-390:39-40, Basis Movie.cs) - in scope, no 2.3 item 6 ruling needed. Domain-only Mark* tests asserting ++ are rewritten under this contract.
- A claim whose provider work crashes, times out or is retried counts as an attempt; the lease re-claim counts again. That is the intended meaning and must be documented for consumers (DEV-390:42) in CONTEXT.md glossary.
- The EfMovieRepository baseline/delta compensation (DEV-301 D2/D8) must still persist exactly the claim count; whether it simplifies is a plan question, not a contract one.
- EnrichmentRequested.Attempt stays a separate transport-retry number (DEV-390:44); its semantics are a later question.
- No schema change (recon:155), no new API shape, no owner checkbox: Gate 1 user merge of the spec PR is the owner gate DEV-390:41 names.


---

## Q2 - Timestamp and persistence behavior

[from Keel] DEV-390 Q2: How should timestamps and persistence interact with the counter change? Recommend the smallest domain edit: BeginPendingAttempt retains Pending validation and LastAttemptAt = now, removes only EnrichmentAttempts++; Mark* retains its current status, metadata, EnrichedAt and failure-category behavior. Preserve EfMovieRepository claim SQL, claim synchronization and baseline/delta behavior without simplification. On a preloaded aggregate, claim DB N to N+1 then Mark* aggregate N with unchanged count delta zero must retain persisted N+1. Cost: LastAttemptAt remains the existing outcome timestamp after completion, rather than redefining lease semantics; no new abstraction or timestamp API. Basis recon:35-50,112,141-143; DEV-390:39-46. ACCEPT/AMEND/REJECT with cited basis; if a needed fact is missing, needs recon. Q1 full exchange is written in specs/DEV-390/CONCLUSIONS.md.

[from Patron] DEV-390 Q2 ruling: ACCEPT.

Verdict: The smallest domain edit is to remove only EnrichmentAttempts++ from Movie.BeginPendingAttempt. RequirePending and LastAttemptAt = now stay. Mark* keeps its status, metadata, EnrichedAt and failure-category behaviour unchanged. EfMovieRepository claim SQL, claim sync (D8) and baseline/delta ApplyAttempts (D2) are kept as they are, with no simplification. Invariant: claim takes DB N to N+1 and syncs the tracked record; Mark* leaves the aggregate count unchanged, so the delta is 0 and N+1 persists.

Basis:
- DEV-390:39-41: reconcile without changing DEV-301 delivery scope. Keeping D2/D8 untouched keeps FR-002/FR-006 intact (recon:141-143).
- recon:35-39,47-49: only BeginPendingAttempt:132 carries the second increment. The other Mark* effects and the timestamp writes are not part of the defect.
- DEV-390:42: the count stays monotonic because nothing in the domain ever decrements it. The delta-0 path preserves the claimed value.

Implications:
- LastAttemptAt keeps its existing dual role: the claim stamp, then overwritten with the outcome time by Mark*. This is not redefined in DEV-390; recorded as noted, no ticket.
- Expected test changes follow from the AC (DEV-390:45) and need no item-6 ruling. The new expectation at EfMovieRepositoryTests:209 is 2 to 1 (recon:112). Domain Mark* tests drop the ++ expectation.
- Unverified: whether test doubles (LeaseAwareMovieRepository, EnrichmentConsumerTests:324) mimic the claim increment. The plan must make them match the real claim (+1 on success, 0 on refusal) so DEV-390:46 is proven at the real seam. That needs a recon fact only if the plan cannot confirm it.


---

## Q3 - Transport and retry semantics

[from Keel] DEV-390 Q3: Settle transport, retry and manual-retry boundaries. Recommend EnrichmentRequested.Attempt is a transport delivery/retry-sequence ordinal: start at 1 for manual retry and stranded requeue, retry policy emits attempt+1 and keeps existing MaxAttempts behavior. It is never copied into or used to reset persisted EnrichmentAttempts. RequestEnrichment permits handler-level NotFound/Failed only, preserves cumulative history, saves before enqueue, and adds no claim; each subsequent successful initial/retry/lease-reclaim adds one in persistence, including Retry and RetryDelayed even without Mark*. No cumulative lifetime retry budget, sweeper redesign or delivery deduplication is introduced. Cost: wire Attempt may restart while cumulative count keeps increasing, and existing sweeper limitation remains. Basis DEV-390:41,44,46; recon:54,66-85,119-130,167-169. Recon:54 already confirms LeaseAwareMovieRepository increments on successful claim; tests must still prove the real PostgreSQL seam. ACCEPT/AMEND/REJECT with citations.

[from Patron] DEV-390 Q3 ruling: ACCEPT.

Verdict: EnrichmentRequested.Attempt is a transport delivery/retry-sequence ordinal. Manual retry and stranded requeue start it at 1. EnrichmentRetryPolicy emits attempt+1 under the existing MaxAttempts rule. The value is never written to persisted EnrichmentAttempts and never used to reset it. RequestEnrichment keeps its current rules: the handler accepts NotFound/Failed only, history is preserved, it saves before enqueue, and it makes no claim. Every successful claim (initial, Retry, RetryDelayed, lease re-claim) adds exactly one, with or without a following Mark*. Nothing new is introduced: no lifetime retry budget, no sweeper redesign, no delivery dedup.

Basis:
- DEV-390:44: the contract must distinguish the transport retry Attempt. EnrichmentRequested.cs:7 is already transport-only (recon:78), and ADR-0004:34 names it the wire counter (recon:80).
- DEV-390:41,46: manual retry semantics are aligned, not redesigned. RequestEnrichmentCommandHandler:16-24 and RequeueStrandedMoviesCommandHandler already enqueue Attempt 1 (recon:71-72). Under the Q1 claim-count contract, retry/re-claim counting follows by itself.
- recon:54: LeaseAwareMovieRepository:56 already applies +1 on a successful claim, which closes my Q2 unverified item. DEV-390:46 still requires proof at the real PostgreSQL claim seam (EfMovieRepository), not only through the double.

Implications:
- ADR-0017:91-93 (unbounded requeue, MaxAttempts not applied across sweeps) is an existing, documented consequence. It is not new broken behaviour. Recorded as noted, no ticket.
- CONTEXT.md:43 is rewritten to the claim-count meaning, plus a separate wire-Attempt line. This edit is forced by DEV-390:42 (document meaning for consumers) and needs no item-6 ruling.
- ADR-0017:36-37 and the DEV-299 completed-outcome wording are not edited. ADRs stay immutable history, and the DEV-390 spec Supersedes section (Q1) lists ADR-0017's completed-outcome wording alongside DEV-294/299/301. ADR-0004:18 already matches the contract.
- Test evidence for DEV-390:46: Retry and RetryDelayed without Mark* each add +1, and manual retry (RequestEnrichment) adds 0 until the next claim. Shown at minimum on the PostgreSQL seam. The EnrichmentConsumerTests:52,68 assertions of >= 2 may be tightened to exact values where deterministic.


---

## Q4 - Frozen file set and care-list rulings

[from Keel] DEV-390 Q4: Freeze the file scope and explicit care-list rulings. Recommend production edit only src/LamuFlix.Core/Domain/Movie.cs; consumer documentation edit CONTEXT.md, scoped to EnrichmentAttempts glossary and separate wire Attempt meaning; Phase A writes only specs/DEV-390/{CONCLUSIONS,ASSUMPTIONS,brief} then Quill-owned spec/plan/tasks. Phase B may update counter assertions and add seam cases in tests/LamuFlix.Test/Domain/{MovieTests,PropertyTests}.cs, tests/LamuFlix.UnitTests/Features/Enrichment/{ApplyEnrichmentResultCommandHandlerTests,RecordEnrichmentFailureCommandHandlerTests,ProcessEnrichmentCommandHandlerTests,RequestEnrichmentCommandHandlerTests,RequeueStrandedMoviesCommandHandlerTests}.cs and tests/LamuFlix.IntegrationTests/{EfMovieRepositoryTests,EfMovieRepositoryClaimConcurrencyTests,EnrichmentConsumerTests}.cs. New seam tests stay in EfMovieRepositoryTests.cs and use its existing fixtures. Rehydrate/persistence model tests retain their arbitrary stored-value round-trip assertions. Repository, handlers, wire record, test helper, historical specs/ADRs, schema, public contracts, dependencies, LocalPlay and secrets stay unchanged. Deliberately authorize CONTEXT.md and listed test-file changes under PRODUCT section 5 item 6 rather than silently assuming permission; no architecture/dependency/schema/API/security care-list trigger. Cost: this is a narrow fix; independent adapter simplification is follow-up. Basis Q1-Q3; recon:87-159; DEV-390:39-46. Do you ACCEPT/AMEND/REJECT this file set and narrow supersession policy? This ask does not authorize code edits in Phase A.

[from Patron] DEV-390 Q4 ruling: ACCEPT, with one amendment.

Verdict: the file set is frozen exactly as proposed.
- Production: src/LamuFlix.Core/Domain/Movie.cs only.
- Docs: CONTEXT.md, limited to the EnrichmentAttempts glossary entry plus the separate wire Attempt meaning.
- Phase A: specs/DEV-390/* only.
- Phase B tests: the listed Domain, UnitTests and IntegrationTests files. New real-seam cases go in EfMovieRepositoryTests.cs using its existing fixtures. Rehydrate and persistence-model round-trip assertions on arbitrary stored values stay as they are.
- Unchanged: repository, handlers, EnrichmentRequested, LeaseAwareMovieRepository, historical specs and ADRs, schema, public contracts, dependencies, LocalPlay and secrets.
- Supersession: the narrow policy from Q1/Q3 stands. It is recorded in the DEV-390 spec only.

Basis:
- Section 2.3 item 6: Movie.cs is ticket-named (DEV-390 Basis line). CONTEXT.md is forced by DEV-390:42. The test edits are forced by DEV-390:45-46 plus the Q1 contract. Each is a cited, in-scope edit, and this ruling authorises them.
- No item 1-5 trigger: no dependency, no layer or project, no schema (recon:155), no API shape, and no LocalPlay, secrets or Process.Start.
- Q2 keeps the adapter untouched. That holds DEV-390:41 (DEV-301 delivery scope unchanged).

Amendment: an adapter baseline/delta simplification is NOT filed as a follow-up ticket. It is not Critical or High, it is not broken behaviour, and the user did not ask for it. Record it in CONCLUSIONS.md as: noted, no ticket.

Unverified: I took the tests/LamuFlix.Test/Domain path from your ask without checking it. If the real path differs, the plan uses the real one and the scope is otherwise unchanged.


---

## Q5 - Test strategy, ordering and historical data

[from Keel] DEV-390 Q5: Settle the proof strategy and task ordering. Recommend after merged spec Gate 1: first write/update failing exact-value tests for the new contract, then remove the domain increment and adjust glossary, then full verification. Domain legal Mark* keeps count N and timestamp behavior; rejected transitions and RequestEnrichment keep history. PostgreSQL Testcontainers cases use N > 0: successful claim N+1 then success/not-found/terminal failure remains N+1, with fresh load and aggregate preloaded before claim; watchlist save must not erase the claim. Refused/double/concurrent claim adds zero beyond the winning claim; retain concurrency proof. Retry and RetryDelayed: first real claim N+1, retry decision without Mark* leaves N+1, next successful real claim N+2, final outcome stays N+2; use injected TimeProvider/lease advancement without sleeping. Manual retry NotFound/Failed: request saves then enqueues wire 1 with count N unchanged, next real claim N+1, final outcome N+1; invalid states keep existing rejection. Enqueue/sweeper alone changes no counter; round-trip stored values stay intact. FsCheck transition invariant becomes count unchanged for domain actions, while preserving other expectations. Consumer double tests remain secondary, exact count where deterministic. Do not mock EF/query translation. Cost: integration cases are required at the seam, not just domain tests. No migration/backfill: existing historical overcounts cannot be reconstructed from recon, remain stored history, and future accepted claims add one. Basis DEV-390:42,45-46; recon:47-54,89-131. recon:93-99 confirms tests/LamuFlix.Test/Domain paths, closing Q4 unverified item. ACCEPT/AMEND/REJECT including no backfill.

[from Patron] DEV-390 Q5 ruling: ACCEPT, including no backfill.

Verdict: the proof strategy and the order of work are accepted as proposed.
- Order: after Gate 1, write or update failing exact-value tests first, then remove the domain increment and update the glossary, then run full verification.
- Domain and FsCheck: the transition invariant for domain actions becomes count unchanged. All other expectations stay.
- PostgreSQL Testcontainers: seam cases use a starting count N > 0. Cover a fresh load and an aggregate preloaded before the claim, the watchlist save, refused, double and concurrent claims, Retry and RetryDelayed, and manual retry. Each case has the exact N, N+1 and N+2 expectations you listed.
- Time: advance the lease with TimeProvider; no sleeps.
- No EF mocking. Consumer tests on the test double are secondary.
- Migration: none, and no backfill.

Basis:
- DEV-390:45-46 require the proof at the real claim/domain seam. Domain-only tests cannot meet that, so the Testcontainers cases are mandatory, not optional.
- DEV-390:42 requires a monotonic counter. A backfill would lower stored values, which violates it. Recon gives no way to rebuild historical attempts anyway. Stored overcounts stay as history, and the CONTEXT.md glossary may state that values written before DEV-390 can be higher.
- recon:93-99 confirms the tests/LamuFlix.Test/Domain paths, so the Q4 unverified item is closed. Section 2.3 item 3 is not triggered: no schema or data change.

Implication: the order mandates tests first. The pipeline does not require TDD, but it is binding for this ticket so that the double-count defect is shown failing before the fix.


---

## Q6 - Loop discipline, gates and closure

[from Keel] DEV-390 Q6: Settle loop discipline, gates and closing bar. Recommend closing bar: no unresolved Critical/High or correctness/spec/contract/security/test-seam defects in the frozen diff; lower-severity independent improvement suggestions are non-blocking, noted without tickets unless you deliberately rule otherwise. Review cap two rounds, at most two fix commits per round; third-round unresolved findings go to user review on PR, never silently waived. Frozen scope: claim counter reconciliation, separate wire/manual retry meaning, narrow consumer glossary and Q4-Q5 tests; anything else is a follow-up issue, not a finding in this round (this labels scope only; no automatic filing, Q4 simplification remains noted no ticket). Three axes mandatory on M. Applicable Phase B gates: Roslyn, complexity, InspectCode on diff, property tests (required due domain invariants, no opt-out), vulnerable packages, format verify, dotnet test, mutation for changed Core production C# via run-mutation.ps1 without since, and web gate only if web scope exists. Thresholds come from harness.yml unchanged; no gate runs or PASS claims in Phase A. Exit 1/Could not run block; opt-out is SKIP; mutation NOT APPLICABLE only script verdict; scope-empty exit 2 is non-blocking SKIPPED (scope-empty) per task-pipeline:101 owner ruling, overriding stale AGENTS language. Order: Quill spec/plan/tasks from brief, Keel read-only analyze and challenge, spec PR user merge Gate 1, then tests-first build/refactor/gates/review. No UI design or ADR for this M non-ui ticket. [assumed] editorial naming: processing attempt means successful atomic claim; transport retry Attempt means sequence ordinal, using existing property names. Cost: all real seam tests and applicable gates block closure. Confirm/amend these terms and whether Q1-Q6 now close the grill with no structural owner checkboxes. Basis task-pipeline:14-20,25-28,70-76,91-112; DEV-390:39-46; PRODUCT sections 2-5.

[from Patron] DEV-390 Q6 ruling: ACCEPT. The grill closes at Q1-Q6 with no structural owner checkboxes.

Verdict: the closing bar, the round and fix caps, the frozen scope, the gate list, the order of work and the two [assumed] editorial terms are accepted as proposed.
- Closing bar and caps: standing rule, cited once (task-pipeline:19,26-27). Lower-severity improvements are noted, no ticket.
- Gates (task-pipeline:91-101): mutation is expected to be APPLICABLE because Movie.cs is production C# under src/ (task-pipeline:95). Its result comes only from the script's verdict. Property tests have no opt-out (Q5 invariant).
- Scope-empty SKIPPED is non-blocking (task-pipeline:101 owner ruling).
- No ADR and no design.md: this is an M ticket with no ui: tag (task-pipeline:14,73-74).
- [assumed] terms: processing attempt = successful atomic claim; transport retry Attempt = sequence ordinal. Existing property names are kept. Log both in ASSUMPTIONS.md.

Basis:
- DEV-390:39-46 is fully answered by Q1-Q5 inside the ticket's own Scope & Technical Design. Nothing adds to, drops from or contradicts it, so there is no escalation (a) item.
- No constitution departure is needed, so there is no escalation (b) item. The 2.3 care items were ruled in Q4 with cited basis.
- The user's owner gate is the Gate 1 merge of the spec PR (DEV-390:41).

Next: you write brief.md, then the grill is closed.
