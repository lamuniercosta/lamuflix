# Tasks: DEV-314 — Infrastructure consumer delivery proof

**Input**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/trace-contract.md`, `brief.md` §Task ordering, Q9-Q10.

**Prerequisites**: plan.md, spec.md. Gate 1 (user spec-PR merge) opens before any Phase B task.

**Tests**: Required — this delivery is test-dominant proof (Q2/Q5). Tests are written FIRST and MUST fail against a broken obligation before any source fix. Structural rows S1-a/S1-c are verified by documented review in T020, not by behavioral assertions; the FIRST/fail-first rule does not demand nonexistent behavioral assertions for those rows.

**Organization**: Ordered phases per Q9 (brief:96-109); small coherent test tasks may share a phase (brief:109). Gate-only tasks stay with Gauge; existing-source alterations belong to Cog; new M/L test work to Anvil.

**Task-path legend** (Q6 envelope, brief:59-64; read/reuse/run only for fixtures and regression suites, brief:66,80):
- T020/T021/T030/T031/T033/T040/T041 → `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` (new/strengthened acceptance tests; minimal nested scoped test handler/synchronization helpers stay in this file).
- T032 (conditional: only where tracestate roundtrip is unpinned) → `tests/LamuFlix.UnitTests/RabbitMq/TraceContextCarrierTests.cs`.
- T050 (conditional: only on a failing T020-T041 obligation test, cited) → `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`.
- T051 (conditional: only on a failing T030-T033 trace-extraction test, cited) → `src/LamuFlix.Infrastructure/RabbitMq/TraceContextCarrier.cs`.

## Phase 1: Setup (Phase A close-out)

- [ ] T001 Quill artifacts complete: spec/clarifications/checklists/plan/research/data-model/quickstart/contracts/tasks consistent with brief Q1-Q12.
- [ ] T002 Keel read-only analysis + challenger review + freeze; open spec PR; user merges (Gate 1).

**Checkpoint**: Gate 1 open; Phase B intake creates `F:/Dev/LamuFlix.worktrees/DEV-314` normally.

## Phase 2: Foundational (drift + mapping — BLOCKS test work)

- [ ] T010 Fresh recon and drift-against-main evidence; re-run analysis; reconcile drift before work. Verify the actual quorum x-delivery-limit >= 2 and record existing fixture reconnect timing bounded within the explicit redelivery wait before T031.
- [ ] T011 Obligation-to-assertion map (after T010): record exact assertion locations of the 12 existing tests against Scope 1-3/AC1-2; unproven rows stay work, presence earns no credit. Carry forward the T010 quorum limit and reconnect-timing record as T031 prerequisites.

**Checkpoint**: Mapping frozen; unproven rows define the test backlog.

## Phase 3: User Story 1 — Async consumer + prefetch (P1)

- [X] T020 [US1] Verify `Consumer_APrefetchOfOne_LeavesTheSecondMessageReady` pins prefetch with actual broker evidence; strengthen only missing observable prefetch assertions (Anvil; sequential owner of `EnrichmentConsumerTests.cs` in Phase 3). Record mandatory structural review receipts for S1-a (type/queue/autoAck: EnrichmentConsumer.cs:47/:49/:51) and S1-c (`ReceivedAsync` to `HandleAsync` :50 to :119 with transitive awaits, no synchronous waits); state the limitation that the second-ready assertion (mapped L177) cannot prove non-blocking dispatch, with no async assertion or negative-control PASS.
- [X] T021 [US1] Temporary prefetch negative control only: break, observe failure, restore, record (Anvil; never committed). Record explicitly that S1-c has no separate behavioral negative-control surface.

## Phase 4: User Story 2 — Trace proof initial + redelivery (P1)

- [X] T030 [US2] Strengthen `Consumer_TheConsumerActivity_SharesTheProducerTraceId` to the full initial contract (kind/parent/tracestate/zero-links/count 0): capture producer TraceId before publish, select by TraceId plus expected source/name/kind (movie-id-only filter insufficient, tag set after ActivityStarted), keep full Q3 assertions, dispose listener per test (Anvil; sequential edit of `EnrichmentConsumerTests.cs` after Phase 3).
- [X] T031 [US2] New real-broker redelivery test: enforce T010/T011 quorum x-delivery-limit >= 2 and bounded reconnect-timing prerequisites using existing test-file options/helpers only (stop for recon/ruling if the Q6 envelope cannot establish them, no expansion); barrier-held unacked delivery, controlled interruption, restart/release; valid publisher-injected headers only (missing-header fallback unchanged, no malformed-header obligation); capture producer TraceId before publish and select by TraceId plus expected source/name/kind; exact Q3 parent/tracestate/one-link/count-1 contract + separate completion/settlement proof; clean up interrupted delivery/consumer/listener state against shared-queue pollution; dispose listener per test; no new exhaustion/count scope (Anvil; sequential edit of `EnrichmentConsumerTests.cs` after T030).
- [X] T032 [US2] Conditional carrier tracestate roundtrip extension only where unpinned (Anvil; `TraceContextCarrierTests.cs` only; skipped when already pinned).
- [X] T033 [US2] Temporary negative controls for each trace assertion: break, observe failure, restore, record (Anvil; never committed; sequential edit of `EnrichmentConsumerTests.cs` after T031).

## Phase 5: User Story 3 — Per-message scope + disposal (P2)

- [X] T040 [US3] New two-message test with nested scoped handler: distinct instances, async disposal per invocation, no root reuse (Anvil; sequential edit of `EnrichmentConsumerTests.cs` after Phase 4).
- [X] T041 [US3] Temporary negative controls for scope/disposal obligations (Anvil; never committed).

## Phase 6: Conditional behavior fixes (Cog only, cited trigger each)

- [ ] T050 `EnrichmentConsumer.cs` fix ONLY where a T020-T041 test exposes a Q2-Q5 defect; cite the failing test (Cog).
- [ ] T051 `TraceContextCarrier.cs` fix ONLY where a T030-T033 test exposes a trace-extraction defect; cite the failing test (Cog).
- [ ] T052 Focused-test rerun after each fix; negative controls restored and re-verified (Cog).

**Checkpoint**: All behaviorally observable obligations pinned by passing named tests, plus structural review receipts for S1-a/S1-c; production diff empty only if every row has evidence.

## Phase 7: Refactor + gates

- [X] T060 Refactor changed code only, frozen scope preserved, stricter refactor gate met (Cog; must complete before T061).
- [ ] T061 Delivery gates on diff/head (after T060; Gauge): Roslyn, complexity, InspectCode (default scope, never `-All`), property tests, vulnerable packages, format verification, full `dotnet test` (all 12 consumer tests plus T030/T031/T040 additions), web applicability, mutation script (no `--since`). Acceptable statuses per brief:90-94 only: exit 0 is pass; scope-empty exit 2 is nonblocking SKIPPED (scope-empty), harness-disabled web is nonblocking SKIP, mutation all-configured-excluded is N/A — none is PASS. Exit 1 and Could not run block; property-test exit 2 blocks unless a valid opt-out is recorded in the task note.
- [X] T062 Verified reruns for any gate failure routed via Cog; initial failures vs permitted reruns recorded distinctly (Gauge).
- [X] T063 Run existing composition, service-collection, options, publisher and topology regression tests as applicable without rewriting their files, alongside all 12 consumer tests (brief:80) (Gauge).

## Phase 8: Review + merge bar

- [ ] T070 Three-axis review (Sentry/Ledger/Compass), findings + summary as PR comments, handshake evidence (all seats).
- [ ] T071 Remediation within caps (2 rounds, 2 fix commits/round); exhausted blockers stay visible for owner disposition.
- [ ] T072 Rebase/push via Rigger, rechecks, size-L ship-review split, merge-bar proof; end at awaiting-merge — user merges, no auto-merge.

## Dependencies & Execution Order

- Phase 1 → Gate 1 → Phase 2 BLOCKS Phases 3-5 → Phase 6 (only on proven defects) → Phase 7 → Phase 8.
- Phase 2 is sequential: T010 drift evidence first, then T011 mapping; T011 mapping completion blocks Phases 3-5.
- Phases 3-5 run sequentially in task order (T020 → T021 → T030 → T031 → T032 → T033 → T040 → T041): one owner at a time for `EnrichmentConsumerTests.cs`; no parallel same-file edits. T032 carrier work is conditional (skipped when tracestate is already pinned).
- T050/T051 never precede their failing test (T050 cites a failing T020-T041 test; T051 cites a failing T030-T033 test); T052 follows each fix; T060 follows T052; T061 follows T060; T070 follows acceptable T061-T063 statuses per brief:90-94 (never shorthand "green").
- Frozen scope: corrected Scope 1-3, AC1-2, Q3 semantics, Q4-Q5 proof, Q6 envelope, Q8-Q9 gates; anything else is a follow-up, not a finding.

## Closing bar and merge bar (brief:113-119)

- Closing bar: verified in-scope Critical, High and Medium findings block; Low findings are documented nonblocking. Gate failures, Could not run, missing Sentry/Ledger/Compass axis, missing handshake evidence, unmet corrected-ticket obligation, unanswered structural owner checkbox and unpublished review round always block regardless of severity label. Round cap: at most two review/remediation rounds, two fix commits per round; exhausted blockers stay visible for owner disposition.
- Merge bar: exact head evidence, acceptable gate statuses per brief:90-94, no above-bar finding, MERGEABLE/non-dirty/non-behind state, and hosted checks including gitleaks. Only the user merges; end at awaiting-merge with no auto-merge.
