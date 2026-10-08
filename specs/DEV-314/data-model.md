# Data Model — DEV-314 (Phase 1)

No schema or migration change (Q7). Entities below are wire/telemetry shapes the proof asserts.

- **EnrichmentRequested** (`src/LamuFlix.Core/Ports/EnrichmentRequested.cs`): record `(MovieId, Attempt)`; wire carries only these two fields per constitution reliability rules.
- **Message headers**: `traceparent` (always), `tracestate` (when present); UTF-8 byte encoding; read via `TraceContextCarrier.TryReadHeader` (bytes or string forms).
- **Producer context**: LamuFlix producer `ActivityContext` (`Enrichment.Enqueue`) from `TraceContextCarrier.ExtractContext` ancestry; the extracted wire context is the RabbitMQ.Client.Publisher client-publish span whose parent is the LamuFlix producer span; the wire context is the parent of every consumer activity and the link target on redelivery.
- **Consumer activity**: name `Enrichment.Process`, kind Consumer, tags `lamuflix.movie.id` and `messaging.rabbitmq.delivery_count` (0 initial / 1 redelivered, not cumulative).
- **Redelivery signal**: `BasicDeliverEventArgs.Redelivered`; broker-set, never synthesized as sole proof.
- **Scope lifetime**: one `AsyncServiceScope` per delivery owning `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`; disposed async after each invocation.
