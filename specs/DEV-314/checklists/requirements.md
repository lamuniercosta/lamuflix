# Requirements Checklist: DEV-314

**Purpose**: Verify every corrected-ticket obligation and grill ruling is covered by spec, plan and tasks before the spec PR.
**Created**: 2026-10-07
**Feature**: `spec.md`

## Scope & obligations

- [ ] CHK001 Scope 1 (async consumer, quorum queue, `RabbitMqOptions.Prefetch`) maps to US1/FR-001/FR-002 and a Phase B test task; S1-a/S1-c require structural review receipts (file:line) alongside the prefetch test, which proves prefetch only.
- [ ] CHK002 Scope 2 (traceparent/tracestate extraction, consumer activity, redelivery link) maps to US2/FR-003/FR-004 and redelivery test tasks.
- [ ] CHK003 Scope 3 (DI scope per message, handler resolution) maps to US3/FR-005 and the two-message scope test task.
- [ ] CHK004 AC1 (async, non-blocking) and AC2 (single distributed trace) each have matrix evidence: AC2 and prefetch by named test; AC1 async dispatch (S1-c) by structural review with stated limitation (no async assertion or negative-control PASS).
- [ ] CHK005 Q3 exact contract (kind/parent/tracestate/0-1 links/count 0-1) appears identically in spec, plan contract and tasks.

## Proof discipline

- [ ] CHK006 Real-broker redelivery required; retry-republish substitution explicitly excluded.
- [ ] CHK007 Determinism rules present: barriers, bounded waits, no arbitrary sleeps, per-test listener disposal.
- [ ] CHK008 Negative controls required temporary, restored, recorded, never committed; prefetch, trace/redelivery/settlement and scope/disposal controls are behavioral (break/fail/restore) while S1-c has no behavioral negative-control surface (structural receipt only).
- [ ] CHK009 Existing 12 tests preserved; credit limited to assertions actually pinned.

## Envelope & gates

- [ ] CHK010 Phase A writes confined to `specs/DEV-314/` + ADR 0019; pin preserved.
- [ ] CHK011 Phase B envelope lists exactly the four conditional files with triggers; fixtures/composition/Worker/Core/API/web excluded.
- [ ] CHK012 No new dependency/project/schema/API/LocalPlay/secret/glossary grant anywhere.
- [ ] CHK013 Gate labels correct (SKIPPED/SKIP/N/A never PASS); no fixed threshold stated; no `-All`, no `--since`.
- [ ] CHK014 Ordering: spec merge opens Gate 1; drift-first Phase B; Anvil/Cog/Gauge routing; two-round caps.

## Notes

- Check items off as completed: `[x]`
- Any unchecked item at spec-PR time is `needs decision` to Keel, not a silent assumption.
