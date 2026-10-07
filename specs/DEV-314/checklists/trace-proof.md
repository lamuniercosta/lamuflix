# Trace-proof Checklist: DEV-314

**Purpose**: Verify the Q3/Q4 trace contract and its proof mechanics are specified without gaps before Phase B.
**Created**: 2026-10-07
**Feature**: `spec.md` US2, `contracts/trace-contract.md`

## Initial delivery

- [ ] CHK101 ActivityKind is Consumer.
- [ ] CHK102 Parent equals the extracted producer context (ParentSpanId pinned, not just TraceId).
- [ ] CHK103 TraceStateString preserved from propagated tracestate.
- [ ] CHK104 Zero ActivityLinks.
- [ ] CHK105 `messaging.rabbitmq.delivery_count` is 0.

## Broker redelivery

- [ ] CHK106 Same producer parent and tracestate as initial delivery.
- [ ] CHK107 Exactly one ActivityLink whose context equals the extracted original context.
- [ ] CHK108 `messaging.rabbitmq.delivery_count` is 1.
- [ ] CHK109 Redelivery produced by genuine broker redelivery of an unacked message, not a republished retry.
- [ ] CHK110 Completion/settlement asserted separately from span capture.

## Mechanics & isolation

- [ ] CHK111 Producer headers include tracestate.
- [ ] CHK112 First invocation held at an explicit barrier while unacked; interruption via controlled cancellation/channel closure, then restart/release.
- [ ] CHK113 Message/movie correlation on every observation; bounded signal waits, no sleeps.
- [ ] CHK114 ActivityListener isolated and disposed per test; captures filtered to the test correlation.
- [ ] CHK115 Cumulative broker counter explicitly out of scope (noted, no ticket).

## Notes

- Check items off as completed: `[x]`
- Any unchecked item at spec-PR time is `needs decision` to Keel, not a silent assumption.
