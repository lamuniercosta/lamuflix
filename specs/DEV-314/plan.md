# Implementation Plan: DEV-314 — Infrastructure consumer delivery proof

**Branch**: `feature/314-spec` | **Date**: 2026-10-07 | **Spec**: `spec.md`

**Input**: `spec.md`; `brief.md`; `CONCLUSIONS.md` Q1-Q12; recon-DEV-314; ADR 0019; `specs/PRODUCT.md`.

## Summary

Preserve the DEV-18 `EnrichmentConsumer` in `src/LamuFlix.Infrastructure/RabbitMq/` and deliver the corrected ticket through targeted proof: async/prefetch, exact Q3 trace/link/count contract on initial and real-broker redelivery, and per-message scope disposal. Source changes only where a failing obligation test exposes a defect. See `research.md` for rejected alternatives, `contracts/trace-contract.md` for the assertion contract, `data-model.md` for telemetry/shape facts, `quickstart.md` for Phase B validation.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: RabbitMQ.Client 7.2.2 (`AsyncEventingBasicConsumer`), OpenTelemetry 1.19.1 / OpenTelemetry.Api 1.19.1 — existing pins only, no new dependency (Q7).

**Storage**: Existing RabbitMQ quorum queue `enrichment.requested` + existing DB fixture via `RabbitMqCollection`; no schema change.

**Testing**: xUnit v3, Shouldly, NSubstitute, FsCheck, Testcontainers — existing conventions only.

**Target Platform**: Linux/Windows CI with containerized RabbitMQ; Api host wires the consumer via `AddLamuFlixRabbitMq`.

**Project Type**: Backend proof task (test-dominant; production diff may be empty).

**Performance Goals**: Deterministic proof — explicit barriers and bounded waits; no arbitrary sleeps.

**Constraints**: Q6 file envelope; temporary negative controls never committed; no DEV-390 retry/claim-count tightening; no `-All`, no `--since`, no lowered thresholds.

**Scale/Scope**: 4 Phase B files max (2 test always/conditional, 2 source conditional); 12 existing tests preserved.

## Constitution Check

GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.

- I (Infrastructure adapter, no Worker resurrection): PASS — consumer stays in Infrastructure.
- II (handler port/decorator chain, `AddHandler` order): PASS — reuse, no new abstraction.
- VI (observability: propagate via `DefaultTextMapPropagator`, link on redelivery, constitutional tag keys): PASS — Q3 contract pins it.
- VII (time/configuration): PASS — design uses `TimeProvider` and existing `RabbitMqOptions` ownership; no `DateTime.Now`; no new configuration surface.
- IX (testing): PASS — design keeps ordinary xUnit proof, existing conventions only, no mocked transport driver (Testcontainers per brief:82); no second assertion/mock library.
- Project rule (seeded `Random`, AGENTS.md non-negotiable): PASS as design intent — no new randomness; new tests use barriers and bounded waits, not sleeps.

No violations; Complexity Tracking empty. This is design compliance only: no delivery gate pass is claimed here. Executed gate receipts belong to Phase B Gauge tasks (T061-T063) under the brief:90-94 acceptable-status policy.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-314/
├── spec.md
├── clarifications.md
├── checklists/requirements.md
├── checklists/trace-proof.md
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/trace-contract.md
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/LamuFlix.Infrastructure/RabbitMq/
├── EnrichmentConsumer.cs      # conditional fix only (Q6 trigger)
└── TraceContextCarrier.cs     # conditional fix only (Q6 trigger)
tests/LamuFlix.IntegrationTests/
└── EnrichmentConsumerTests.cs # strengthened + new tests, nested handler/helpers
tests/LamuFlix.UnitTests/RabbitMq/
└── TraceContextCarrierTests.cs # conditional tracestate extension only
```

**Structure Decision**: No new project, folder or layer (Q7). Phase A writes stay under `specs/DEV-314/` plus frozen ADR 0019; Phase B uses the four Q6 files only.

## Delivery Phases and Dependencies (brief:96-109)

Ordered Phase B delivery; task IDs live in `tasks.md`:

1. Phase A close-out → spec PR → user merge opens Gate 1; no Phase B work before.
2. Phase B intake: fresh recon + drift-against-main evidence, re-run analysis, reconcile drift (T010) before mapping (T011); mapping blocks all test work.
3. Obligation-to-assertion mapping of the 12 existing tests; unproven rows are the backlog.
4. Sequential test proof in task order (T020 → T041): one owner at a time for `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs`; carrier extension (T032) conditional on missing tracestate proof. Anvil owns new M/L test work.
5. Conditional source fixes only on cited failing tests (T050/T051, Cog); focused reruns + restored negative controls (T052) complete the test/fix phase.
6. Cog refactors changed code only under the stricter refactor gate (T060) before Gauge gates (T061-T063).
7. Gauge runs delivery gates plus applicable composition, service-collection, options, publisher and topology regression tests (brief:80); failures route to Cog with verified reruns.
8. Three-axis review (Sentry/Ledger/Compass), findings + summary as PR comments, handshake evidence; remediation within caps; rebase/push via Rigger; rechecks; size-L ship-review split; end at awaiting-merge — user merges, no auto-merge.

## Redelivery Proof Strategy (Plan challenge, brief:131-143; CONCLUSIONS Plan challenge adjudication)

- Correlation: capture the producer TraceId before publish; select expected consumer activities by that TraceId plus expected source/name/kind. Movie-id-only filtering is insufficient because its tag is set after ActivityStarted (recon 4.1/4.3). TraceId selection does not replace the full Q3 parent/tracestate/link/count assertions.
- Prerequisites (before T031): intake/mapping verifies the actual quorum `x-delivery-limit >= 2` and records existing fixture reconnect timing bounded within the explicit redelivery wait. No invented numeric reconnect duration is stated here. T031 enforces these prerequisites using existing test-file options/helpers only — no fixture/config/topology/source expansion; if the Q6 envelope cannot establish them, stop for recon/ruling rather than expand.
- Headers: trace proof uses valid publisher-injected traceparent/tracestate; the existing missing-header fallback is unchanged and no new malformed-header obligation is added.
- Isolation: T031 proves completion/settlement separately from span capture and cleans up interrupted delivery/consumer/listener state so no unacked message pollutes shared-queue tests. No new exhaustion test or cumulative delivery-count scope. The ActivityListener is disposed per test.

## S1-a/S1-c evidence method (CONCLUSIONS.md Phase 3 T020 S1-c; brief:3-13)

S1-a (consumer type, requested quorum queue, autoAck false) and S1-c (async non-blocking dispatch) are mandatory structural obligations verified by source-structure review with exact file:line receipts, not by behavioral assertion. The review pins `AsyncEventingBasicConsumer` (EnrichmentConsumer.cs:49), `RabbitMqTopology.RequestedQueue` with `autoAck: false` (:51), prefetch `BasicQosAsync` from `options.Value.Prefetch` (:47), and `ReceivedAsync` dispatch to `HandleAsync` (:50 to :119) with transitive awaits and no synchronous waits introduced. `Consumer_APrefetchOfOne_LeavesTheSecondMessageReady` proves prefetch only; its second-ready assertion (mapped L177) cannot prove non-blocking dispatch, and S1-c has no behavioral negative-control surface — it is never reported as assertion-proven or as an async negative-control PASS. No dispatcher instrumentation, source seam, reflection, thread-id/timing/ThreadPool heuristic, or new dependency is authorized; the Q6 envelope is unchanged.

## Closing Bar and Merge Bar (brief:113-119)

- Closing bar: verified in-scope Critical, High and Medium findings block; Low findings are documented nonblocking. Gate failures, Could not run, missing Sentry/Ledger/Compass axis, missing handshake evidence, unmet corrected-ticket obligation, unanswered structural owner checkbox and unpublished review round always block regardless of severity label. At most two review/remediation rounds, two fix commits per round; exhausted blockers stay visible for owner disposition.
- Merge bar: exact head evidence, acceptable gate statuses per brief:90-94 (exit 0 pass; SKIPPED scope-empty, SKIP configured-disabled, mutation N/A — none is PASS; exit 1 / Could not run block; property exit 2 blocks absent recorded opt-out), no above-bar finding, MERGEABLE/non-dirty/non-behind state, and hosted checks including gitleaks. Only the user merges.

## Complexity Tracking

None — no constitution violations to justify.
