# Trace contract — DEV-314 consumer activity assertions

Binding assertion set for US2/FR-003/FR-004. Every assertion below is message-correlated to the test's own producer context.

## Initial delivery

- `Activity.Kind == ActivityKind.Consumer`
- `Activity.TraceId == producer TraceId`
- `Activity.ParentSpanId == producer span id`
- `Activity.TraceStateString == propagated tracestate`
- `Activity.Links` is empty
- Tag `messaging.rabbitmq.delivery_count == 0`

## Broker redelivery (genuine, unacked interruption)

- Same parent identity and tracestate as initial delivery
- `Activity.Links` has exactly one entry whose `Context == extracted original producer context`
- Tag `messaging.rabbitmq.delivery_count == 1`
- Completion/settlement asserted separately from span capture

## Non-goals

- Matching TraceId alone is not proof (ParentSpanId required).
- No cumulative broker-delivery counter; no retry-republish equivalence.
