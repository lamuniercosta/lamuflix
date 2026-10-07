# Research — DEV-314 (Phase 0)

## R1. Preserve the existing Infrastructure consumer

- **Decision**: Prove `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs` as-is; fix only defects a failing obligation test exposes.
- **Rationale**: DEV-18 implemented the ticket's consumer; DEV-19 retired the Worker (build fails NU1010); recon §2, §4.1.
- **Rejected**: Worker resurrection (duplicates an established adapter, contradicts Ruling 1).

## R2. Producer-parent plus redelivery-link trace semantics

- **Decision**: Parent every consumer activity to the extracted producer context; add exactly one link to that context on redelivery; count tag 0/1.
- **Rationale**: Corrected Scope 2 + AC2 + constitution VI; matches `EnrichmentConsumer.cs:225-238`.
- **Rejected**: Link-only tracing (drops parent continuity required for the single trace).

## R3. Real-broker redelivery with synchronized interruption

- **Decision**: Unacked delivery held at a barrier, interrupted via controlled cancellation/channel closure, restarted/released in `RabbitMqCollection`; bounded waits, correlated assertions.
- **Rationale**: Only genuine redelivery exercises the `delivery.Redelivered` branch; recon §4.1/§4.3/§4.6.
- **Rejected**: Synthetic `BasicDeliverEventArgs`-only proof (supplement at most); retry-republish substitution (different branch); arbitrary sleeps (non-deterministic).

## R4. Per-message scope proof via nested test handler

- **Decision**: Two-message test with a small scoped handler inside `EnrichmentConsumerTests.cs`; distinct instances, async disposal, no root reuse.
- **Rationale**: Scope 3 obligation; no new test project or dependency permitted (Q5/Q7).
- **Rejected**: Root-registered test double (cannot prove per-message scoping).

## R5. Conditional source edits, temporary negative controls

- **Decision**: Each `EnrichmentConsumer.cs`/`TraceContextCarrier.cs` edit cites its failing Q2-Q5 test; negative controls break-restore-record locally and are never committed.
- **Rationale**: Q2 boundary (empty diff acceptable with full evidence) + Q5/Q6 triggers.
- **Rejected**: Pre-emptive source rewrite; committed mutation fixtures.
