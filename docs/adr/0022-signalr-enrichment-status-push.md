# 0022. SignalR for enrichment status push

- Status: Accepted
- Date: 2026-10-26
- Ticket: DEV-330

## Context

ADR-0008 accepted client polling for enrichment status in P1 and deferred SignalR as a stretch item. DEV-330 needs live enrichment status in the SPA without polling. This ADR accepts the push channel; implementation belongs to DEV-330 and DEV-422, not here.

Basis: task note DEV-421; ADR-0008.

## Decision

Push enrichment status to the SPA over ASP.NET Core SignalR through an `EnrichmentHub` hosted in the Api host. The ASP.NET Core SignalR server is part of the shared framework, so no server NuGet package is added; the client is `@microsoft/signalr`. One Api instance serves the hub, so no backplane is introduced. On reconnect the client refetches pending movies rather than replaying missed events.

## Consequences

This ADR supersedes ADR-0008 for the enrichment status channel only: polling is no longer the answer where the SignalR push applies. No ADR-0008 history is rewritten.

The constitution still describes the pre-push position and needs a later amendment through the constitution amendment process, which is out of scope here: line 473 (enrichment status delivered by polling in P1 with SignalR as a stretch item) and line 499.
