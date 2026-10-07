# 0019. Infrastructure consumer delivery proof

- Status: Proposed (pending Gate 1 spec PR merge)
- Date: 2026-10-07
- Ticket: DEV-314

## Context

DEV-18 already implemented the async enrichment consumer in Infrastructure. DEV-19 retired the Worker project from the solution; its remaining files are not the active host. DEV-314 originally named that retired location and the wrong prefetch options owner. Patron approved corrections that preserve all three scope items and both acceptance criteria, and Rigger verified them in YouTrack (DEV-314 note:29-33). The consumer still needs explicit proof of redelivery tracing and per-message scope lifetime.

Basis: recon-DEV-314 sections 2, 4.1, 4.3 and 4.6; constitution I and VI; [DEV-314 conclusions](../../specs/DEV-314/CONCLUSIONS.md), Ruling 1 and Q2-Q7/Q11-Q12.

## Decision

Complete delivery by proving the existing Infrastructure consumer, preserving its async adapter, Api host wiring and RabbitMqOptions.Prefetch ownership. Change the consumer or trace carrier only when an in-scope obligation test exposes a defect.

Every consumer Activity uses the producer context extracted from traceparent/tracestate as its parent, preserving TraceId, ParentSpanId and tracestate. Broker redelivery additionally links exactly once to that original context; initial delivery has no link. Keep messaging.rabbitmq.delivery_count encoded as 0 for initial delivery and 1 for redelivery.

Reject Worker resurrection: it would duplicate an established adapter and contradict the approved location correction. Reject link-only tracing: it would drop the producer-parent continuity required for the single distributed trace.

## Consequences

Proof uses the existing real RabbitMQ/DB test infrastructure, deterministic synchronization and correlated span assertions. Republished retries do not substitute for broker redelivery. Two-message tests establish distinct handler scopes and async disposal.

This records an existing boundary and its proof strategy; it grants no new architecture, dependency, schema, API or glossary changes. Retired Worker files remain untouched. The delivery can have an empty production diff only after every obligation has deliberate named-test evidence; full Phase B gates and review remain required. See [brief](../../specs/DEV-314/brief.md) for the frozen file envelope and review terms.
