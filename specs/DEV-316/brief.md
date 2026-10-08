# DEV-316 - StrandedMovieSweeper grill brief

## Status and authority

Phase 2 step 1 grill CLOSED by Patron Q10, after ten questions of a maximum twelve. Q4 and Q8 continuations resolve their existing questions and do not add questions. No owner structural checkbox or constitution departure remains. This brief authorizes Quill's specification drafting, not implementation, gate completion, PR publication, or merge.

Worktree: F:/Dev/LamuFlix.worktrees/feature-316-spec
Branch: feature/316-spec
Recon HEAD: 57244c3833e6ce1a071b310b760f9b7d186167b4
Facts: connected note recon-DEV-316, including host placement, implementation sites/fixtures, and consumer gate/handler registration sections. Decisions: append-only [CONCLUSIONS.md](CONCLUSIONS.md). Historical statements there about open questions are superseded only by their later explicit rulings.

Ticket scope, verbatim as supplied in recon:
> Deliver the constitution-required StrandedMovieSweeper re-enqueueing stranded Pending rows via IEnrichmentQueue using EnrichmentOptions lease/sweep + TimeProvider.

Acceptance, verbatim as supplied in recon:
> Integration evidence covers a lost/failed enqueue and a stale claim recovered by the sweep.

## Review loop discipline

Closing bar: no unresolved Critical, High, or Medium in-scope findings; Low findings explicitly adjudicated and recorded. Required checks must have actual successful receipts or the applicable explicitly non-blocking verdict. A gate that could not run blocks closure.

Frozen scope: periodic recovery of Pending rows with no last attempt or an expired enrichment claim lease; Core selection orchestration and repository port; read-only EF IDs query; existing requeue/queue dispatch; positive lease/sweep-duration validation; decorated Api handler composition and hosted-service registration; thin infrastructure loop; compile-compatible repository doubles; deterministic unit/lifecycle tests; real PostgreSQL/RabbitMQ recovery integration evidence; isolated unrelated Api test hosts; the bounded existing ADR-0004 status note. anything else is a follow-up issue, not a finding in this round.

Round cap: two rounds per review loop. An unresolved blocking finding at the cap stops that loop; no unbounded extra repair/review round. The grill cap is twelve questions total; ten were used.

For size M, require Sentry Risk, Ledger Standards, and Compass Spec reports before Keel adjudicates. A missing axis is blocked: missing axis <name>, never a skip. Account for security and mutation/coverage lanes as well. Mechanical passes alone do not establish readiness. Review evidence must identify the exact reviewed head, scope, and gate provenance. Findings and the review summary must be published on the PR; user owns merge. Never merge or enable auto-merge.

## Every grill answer

1. **Q1 ACCEPT, amended:** eligibility is Status == Pending AND (LastAttemptAt == null OR LastAttemptAt < now - ClaimLease), with strict < and injected TimeProvider. No sweeper claim or status/attempt mutation. Dispatch through existing RequeueStrandedMoviesCommand/Handler, not a second enqueue path. Basis: constitution:187-188; ADR-0004:94; ADR-0017:69-83; existing atomic claim predicate in recon.
2. **Q2 ACCEPT:** no sweeper EnrichmentAttempts filter, cap, or mutation. Requeue uses existing message Attempt=1. Worker bounded per-message retry/Failed policy remains authoritative; Failed is outside Pending. Persistent publish failure or claim-then-crash can recur at the lease/interval cadence. No new terminal-state policy or follow-up ticket was ordered. Basis: constitution:185-188; ADR-0017:69-93. Q3 confirms that “leaves Pending” means transitions out of Pending to Failed.
3. **Q3 ACCEPT, amended:** immediate first pass, serial BackgroundService, fresh scope per pass, one now per pass, a full SweepInterval delay after each successful or failed pass through TimeProvider, no within-instance overlap. Thin host handles timing/scopes/logging; Core handles selection/dispatch. Log pass exceptions at Error with exception; retry at next interval only. Forward stoppingToken to query and enqueue; host cancellation exits cleanly. Long passes lengthen cadence and a failed enqueue aborts the pass, deferring remaining work. Basis: constitution:449-457; ADR-0017; ADR-0005:24.
4. **Q4 Core ACCEPT; host continuation ACCEPT alternative:** new SweepStrandedMoviesCommand/Handler in Core and FindStrandedMovieIdsAsync(leaseCutoff, ct) on existing IMovieRepository. EF projects IDs without tracking or mutation using Q1. Infrastructure/Enrichment/StrandedMovieSweeper.cs hosts the loop; explicit Api Program.cs AddHostedService registration. Api is the established enrichment-composition host, legacy Worker is untouched, no AppHost is introduced. Persistence registration stays queue-free. Internal port expansion is not a public API, dependency, schema, or architectural layer. Basis: ADR-0004:94; ADR-0017; host recon.
5. **Q5 ACCEPT, validator seam amended:** positive ClaimLease and SweepInterval validated at startup by sealed EnrichmentOptionsValidator : IValidateOptions<EnrichmentOptions> in Core/Options, registered alongside existing ServiceDefaults option binding/ValidateOnStart. Messages name the invalid configuration key. Preserve defaults, MaxAttempts Range validation, and existing EfMovieRepository lease guard. No upper limit or new package. Q7 replaces the original existing-options-test edit with dedicated validator tests.
6. **Q6 AMEND:** both acceptance scenarios use real PostgreSQL and RabbitMQ with Testcontainers and the actual registered sweeper/Core path; no substituted infrastructure queue in those integration tests. Lost enqueue is the dual-write gap: persist Pending with null LastAttemptAt and publish no message. Stale recovery follows a real claim and deterministic time advancement. Observe EnrichmentRequested(id,1) on the real queue and prove subsequent real claim succeeds. Assert no sweep mutation before that intentional later claim. EF exclusion matrix, handler unit tests, deterministic hosted lifecycle tests, and composition tests are required. Unrelated Api hosts remove only the sweeper; recovery hosts retain it. Basis: constitution:299-305; ADR-0005. Unit-level ports may be substituted.
7. **Q7 ACCEPT file set/order; registration AMEND:** use Api/HandlerRegistration.cs AddHandler for both sweep and requeue handlers, preserving decorators. Sweep dispatch injects the decorated command-handler abstraction, not concrete requeue handler. Dedicated validator tests and timer-capable ManualTimeProvider accepted. New port member must be reflected in all listed implementations/test doubles. HandlerRegistration edit is deliberately ruled in scope under section 2.3 item 6. Q8 recon confirms requeue registration was absent.
8. **Q8 conditional, then CONFIRM resolved:** the consumer gate checks only service descriptors; publisher registration is unconditional. Therefore the sweeper is registered unconditionally in Api Program.cs, independent of local consumer activation. Both handlers use AddHandler and scoped ICommandHandler interfaces; hosted service resolves the sweep interface per fresh scope. Preserve existing command-result conventions. No new flag or gate extraction. Basis: recon:118-141, Q1/Q4/Q7, ADR-0005.
9. **Q9 ACCEPT, gate authority AMEND:** review terms and lanes above accepted. Gate authority is committed harness.yml; an unrelated uncommitted web-disabled toggle is not an authorized opt-out. Report actual scope-empty web verdict. Patron rejects that toggle as outside ticket scope and requests operator reconciliation. Cleanup and commit requests are routed to Bernstein/Rigger; Keel does not restore config, delete recon scratch files, or stage another seat's pin. No thresholds or generated configuration files are edited.
10. **Q10 CLOSURE CONFIRMED, bounded doc addition:** Keel writes this brief; Quill drafts spec, plan, tasks. No owner structural checkbox. Quill adds a short status note to existing ADR-0004:92-96 citing ADR-0017:69-83 and DEV-316 because design-only/open-handoff wording is stale; preserve earlier decision history and create no new ADR. This unnamed-file doc edit is deliberately ruled in scope under section 2.3 item 6. Conclusions must be committed before the spec PR opens; operator housekeeping is accepted as Bernstein/Rigger's work.

Full decision exchanges, rationale, amendments, and continuations remain in CONCLUSIONS.md. No taste choice requiring an ASSUMPTIONS.md entry was made.

## Approach and contract

Capture injected TimeProvider.GetUtcNow once in the Core sweep handler, subtract configured ClaimLease, then call the read-only ID-selection repository member. Use existing MovieId and command-result conventions, no duplicate domain or transport DTOs. Route the selected IDs through decorated existing RequeueStrandedMoviesCommand handler. Selection, dispatch, and every delay accept cancellation.

The repository predicate excludes equal-boundary and fresh leases and all non-Pending states. It never claims or updates a row. Existing atomic worker claiming protects cross-instance duplicate delivery. The sweep does not increment EnrichmentAttempts, refresh LastAttemptAt, change status, or apply a MaxAttempts predicate. No batch-size limit, new retry counter, terminal policy, scheduling library, or arbitrary duration bound is introduced.

The host creates/disposes a scope per pass and resolves the decorated sweep handler. A non-shutdown query or publish failure is logged at Error with its exception, followed by the normal interval delay. Cancellation from host shutdown exits without treating orderly shutdown as a sweep failure. Serial completion-based cadence is deliberate; no immediate retry loop. Cross-instance overlap remains safe through existing claiming.

Canonical vocabulary is Pending, claim lease, stranded row, and requeue as already defined by the established enrichment decisions. No new glossary term or architecture is introduced; no root CONTEXT.md edit or new ADR is required. The existing ADR-0004 status note is the sole ADR change.

## Bounded file set

New implementation and test files:

- src/LamuFlix.Core/Features/Enrichment/SweepStrandedMoviesCommand.cs
- src/LamuFlix.Core/Features/Enrichment/SweepStrandedMoviesCommandHandler.cs
- src/LamuFlix.Core/Options/EnrichmentOptionsValidator.cs
- src/LamuFlix.Infrastructure/Enrichment/StrandedMovieSweeper.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/SweepStrandedMoviesCommandHandlerTests.cs
- tests/LamuFlix.UnitTests/Enrichment/StrandedMovieSweeperTests.cs
- tests/LamuFlix.UnitTests/Options/EnrichmentOptionsValidatorTests.cs
- tests/LamuFlix.IntegrationTests/StrandedMovieSweeperTests.cs
- tests/LamuFlix.Tests.Common/ManualTimeProvider.cs

Existing files, narrowly scoped edits:

- src/LamuFlix.Core/Ports/IMovieRepository.cs: new read-only selection member.
- src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs: implementation with IDs-only/no-tracking selection.
- src/LamuFlix.Api/Program.cs: explicit unconditional sweeper hosted-service registration.
- src/LamuFlix.Api/HandlerRegistration.cs: decorated sweep and requeue AddHandler registrations.
- src/LamuFlix.ServiceDefaults/Extensions.cs: options-validator registration alongside existing binding.
- tests/LamuFlix.IntegrationTests/LeaseAwareMovieRepository.cs: implement new member.
- tests/LamuFlix.IntegrationTests/MetadataProviderCompositionTests.cs: compile-compatible UnusedMovieRepository member.
- tests/LamuFlix.UnitTests/RabbitMq/RabbitMqServiceCollectionExtensionsTests.cs: compile-compatible NoMovieRepository member.
- tests/LamuFlix.IntegrationTests/ApiHostFactory.cs: unrelated-host sweeper removal with recovery-host opt-in.
- Existing ADR-0004 in docs/adr/, specifically lines 92-96 identified by Patron: short status note only, citing ADR-0017 and DEV-316, prepared by Quill in the spec PR.

Use existing database/broker fixtures without rewriting them. No edits to Worker, RabbitMQ activation gate, persistence-registration extension, existing MaxAttempts tests, schema/migrations, package/project files, public endpoints/DTOs, web generated contracts, LocalPlay, secrets, or process execution are part of this file set. Operator handling of pre-existing harness/pin/scratch changes is separate from implementation scope.

## Test strategy and task-ordering constraints

1. Establish positive-duration validator and repository port, and update all listed concrete implementations/test doubles so each later step compiles. Validator tests cover zero/negative for each duration, valid defaults, key-specific failures, and actual startup validation wiring.
2. Implement EF selection with real-database coverage for null last attempt, strictly expired lease, fresh lease, exact boundary, non-Pending states, and no persistence mutation. Query translation is never tested through a mocked driver.
3. Implement Core orchestration with unit evidence for empty selection, captured cutoff, dispatch through existing requeue path, cancellation forwarding, and failure propagation. Existing requeue message Attempt=1 behavior is retained.
4. Implement the thin infrastructure hosted loop with timer-capable test-owned TimeProvider. Cover immediate pass, interval after success/failure, serial execution, scope disposal, Error exception logging, later recovery, query/enqueue cancellation, and clean shutdown. Use bounded observable synchronization; no wall-clock sleeps or new fake-time package.
5. Wire Api AddHandler composition and hosted service, options validator, and ApiHostFactory isolation together before running Api regression tests. Prove recovery hosts retain the sweeper registration and unrelated hosts remove only that service. Preserve existing decorated command-result conventions.
6. Real PostgreSQL/RabbitMQ integration acceptance tests run the actual registered sweeper: lost dual-write-gap recovery and expired real-claim recovery. Set up rows and infrastructure before the tested pass, isolate test data/queue observation deterministically, observe Attempt=1, and distinguish no sweep mutation from the subsequent intentional real claim.
7. After implementation/refactor, run all applicable configured gates, capture exact-head receipts, complete every size-M review axis and other required lane, and adjudicate findings within the agreed cap. Quill's spec/plan/tasks must preserve this dependency order and all preceding decisions.

Use the existing xUnit v3, NSubstitute, Shouldly, and AutoFixture conventions supplied by recon. Only permitted test comments are AAA headers and other project-approved narrow exemptions. A surviving mutant requires a test fix, never a threshold change.

## Gate expectations and handoff

No implementation gates were run for this docs-only grill. Recon reports analyzers warnings-as-errors, complexity 15 at implement and 6 at refactor, InspectCode/property tests enabled, vulnerable-package failures including transitive, and mutation threshold 80 with Api exclusion. Thresholds are descriptive receipts from harness.yml, never duplicated as implementation constants. Delivery verifies current committed configuration at its stage; the rejected pre-existing uncommitted web toggle cannot create a gate opt-out.

Expected delivery checks: build/analyzers, appropriate complexity gate, InspectCode, complete tests/property tests, vulnerable packages, formatting, diff review, and pre-PR mutation for every eligible changed project. Api's explicit mutation exclusion is reported separately; it does not exempt changed Core, Infrastructure, or ServiceDefaults. Required unrunnable or unlisted ineligible-project gates block. SKIPPED (scope-empty), configured SKIP, mutation N/A, and Could not run are distinct and never PASS. With no web changes the web gate must report its actual scope-empty verdict under authoritative configuration.

Quill drafts spec.md, plan.md, tasks.md from this brief and adds the bounded ADR-0004 status note. Keel then performs read-only speckit-analyze and checks plan/tasks against this brief; a decision change is recorded here first. No further grill decision is open. Bernstein/Rigger owns reconciliation of the pre-existing gate toggle, scratch-file handling, and documentation/pin commits; commit messages use DEV-316 - {subject}, and CONCLUSIONS.md must be committed before spec PR publication. The user retains all merge authority.

