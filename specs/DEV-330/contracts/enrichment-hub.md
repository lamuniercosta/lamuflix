# Contract: EnrichmentHub (DEV-330)

**Transport**: ASP.NET Core SignalR (shared framework server)  
**Hub route**: `/hubs/enrichment`  
**Negotiation**: `/hubs/enrichment/negotiate` (framework)  
**Direction**: Server → client only (no client-to-server hub methods, groups, or subscriptions)  
**Audience**: All connected clients (`Clients.All`) on the single Api instance  
**CORS**: Existing `DevSpa` — origin `http://localhost:5173`, any method/header, exposed `Location`, no credentials  

## Event

| Name | Arguments |
|------|-----------|
| `EnrichmentStatusChanged` | One positional argument: `EnrichmentStatusChangedDto` |

## Payload JSON

```json
{
  "id": 123,
  "enrichmentStatus": "Pending"
}
```

`enrichmentStatus` ∈ `Pending` | `Enriched` | `NotFound` | `Failed` (strings only).

## C# shape

```csharp
EnrichmentStatusChangedDto(int Id, EnrichmentStatus EnrichmentStatus)
```

Api immutable positional record conventions; SignalR JSON name converters configured independently of HTTP JSON.

## OpenAPI / TypeScript chain

- Exposed in `components.schemas` via document transformer (no REST path/operation).
- Regenerate: `LAMUFLIX_REGENERATE_OPENAPI=true` filtered test → `web/src/api/openapi.json` + Verify snapshot → `npm run generate:api` → `types.ts`.
- Hub-only schema produces no MSW path handler.
- DEV-422 consumes generated type; never hand-edit generated artifacts.

## Delivery semantics (not wire fields)

- Exactly one event per committed actual status transition (insert Pending counts).
- No event on failed save, no-op status, unrelated update, or pre-commit cancellation.
- One sequential awaited post-commit send attempt per transitioned movie with `CancellationToken.None`; repository per-movie isolation continues remaining movies; no application-level timeout/fan-out bound.
- Adapter attempt-start/failure logs carry movie id and committed status; not durable crash-window detection.
- No replay / outbox / guaranteed receipt; crash loss and reconnect/refetch belong to DEV-422.
