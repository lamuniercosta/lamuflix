# DEV-441 EVIDENCE — nginx /hubs/ proxy for SignalR

Ticket: nginx proxies `/hubs/` to the API so SignalR live updates work in Compose.
Acceptance: `POST /hubs/enrichment/negotiate` returns 200 in Compose; newly
imported movie tile goes Pending to Enriched without a reload.

## Finding: fix already present on base

`docker/nginx.conf` on `main` already contains the required `location /hubs/`
block (introduced by commit `1c3cf5ff`, "proxy the hub in nginx"). No
`docker/nginx.conf` edit was needed in this worktree; `git diff main -- docker/nginx.conf`
is empty.

Present block (verified by string check, 7/7 PASS on 2026-11-17):

- `location /hubs/` proxies to `http://api:8080` (same upstream as `/api/`)
- `proxy_http_version 1.1` with `Upgrade $http_upgrade` and
  `Connection $connection_upgrade` (via the `map $http_upgrade` block;
  functionally the ticket's `Connection "upgrade"` with safe non-WebSocket fallback)
- `proxy_read_timeout 3600s`, `proxy_buffering off`, plus `Host`/`X-Real-IP`/
  `X-Forwarded-For`/`X-Forwarded-Proto` headers matching the `/api/` location

## Contract endpoints (verified in tree)

- Backend hub: `src/LamuFlix.Api/Program.cs:78` —
  `app.MapHub<EnrichmentHub>("/hubs/enrichment")`
- Frontend client: `web/src/library/useEnrichmentStatus.ts:39,83` —
  connects to `${origin}/hubs/enrichment`; existing tests in
  `web/src/library/useEnrichmentStatus.test.tsx` cover the hook.

## Affected frontend routes

None. No page route changes. The only UI surface is the Library grid tile
(Pending to Enriched live update) driven by the `/hubs/enrichment` SignalR
connection through the nginx proxy.

## Gates / format

- No C#, TypeScript, or config files edited (only this note added), so
  Roslyn/cyclomatic/InspectCode gates are scope-empty and `dotnet format`
  is a no-op. Live Compose acceptance (`POST /hubs/enrichment/negotiate`
  = 200) remains operator-verified at deploy time.
