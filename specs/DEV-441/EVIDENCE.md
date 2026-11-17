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

## Phase 3 step 5 gate run (2026-11-17, HEAD 1f1be1e36f3b4d743d97328fb6b2c70d4354c59f)

Ran from the `feature/DEV-441` worktree with `pwsh -NoProfile -File`. Gates that
exit 2 `SKIPPED` are scope-empty (nothing verified, never PASS) per the gate
verdicts; they do not block.

| Gate | Exit | Verdict |
|---|---|---|
| `scripts/run-roslyn-analyzers.ps1` (no args) | 2 | `SKIPPED (scope-empty)` — no `.cs` files changed against `origin/main` |
| `scripts/run-cyclomatic-complexity.ps1` (no args) | 2 | `SKIPPED (scope-empty)` — no `.cs` files changed against `origin/main` |
| `scripts/run-jetbrains-inspectcode.ps1` (no args) | 2 | `SKIPPED (scope-empty)` — no `.cs` files changed against `origin/main` |
| `scripts/run-property-tests.ps1` | 0 | PASSED — 160 executed (157 unit + 3 integration), 0 failed |
| `scripts/run-vulnerable-packages.ps1` | 0 | PASSED — no vulnerable packages (incl. transitive) |
| `dotnet format --verify-no-changes` | 0 | PASSED — no changes (workspace-load warnings only, diagnostic-level) |
| `dotnet test` | 0 | PASSED — 5921 passed, 0 failed, 3 skipped |
| `scripts/run-web-gates.ps1` | 2 | `SKIPPED (scope-empty)` — no `web/` files changed |

`dotnet test` detail: Evaluation 81 passed; ArchitectureTests 16 passed;
UnitTests 4013 passed, 1 skipped; Evaluation.IntegrationTests 21 passed;
IntegrationTests 1790 passed, 2 skipped (Ollama embedding, OpenAPI opt-in
regeneration — both environment-gated). Total 5921 passed, 0 failed, 3 skipped.

## F1 live acceptance (2026-11-17, HEAD 1f1be1e36f3b4d743d97328fb6b2c70d4354c59f)

Environment: Windows 11, .NET 10 SDK, Docker 29.9.3, worktree
`F:/Dev/LamuFlix.worktrees/DEV-441`, branch `feature/DEV-441`.

1. Hub negotiate + event flow (live API host):
`dotnet test tests/LamuFlix.IntegrationTests --filter
'FullyQualifiedName~EnrichmentHubTests|FullyQualifiedName~ApiCorsTests'
--blame-hang --blame-hang-timeout 10m` — 9 passed, 0 failed.
`Negotiate_ReturnsConnectionTokenAndTransports` proves
`POST /hubs/enrichment/negotiate?negotiateVersion=1` returns 200 with a
connection token and transports. `ConnectedClient_CommittedStatuses_
ReceivesCamelCaseFramedEvents` proves a live connected client receives
server-originated `EnrichmentStatusChanged` events for Pending and Enriched
(the payloads that drive the Library tile Pending to Enriched without reload).
`LiveHost_ResolvesRealSignalRNotifier` proves the live host notifies through
`SignalREnrichmentStatusNotifier`.
2. Nginx `/hubs/` proxy leg (live, shipped `docker/nginx.conf` unmodified):
disposable `dev441-proof` network with a stub backend under the Compose
hostname `api:8080` and `nginxinc/nginx-unprivileged:1.31-alpine` serving the
shipped config. `POST /hubs/enrichment/negotiate?negotiateVersion=1` through
the proxy returned 200 with the upstream echo
`POST /hubs/enrichment/negotiate?negotiateVersion=1`; `GET /api/health/live`
returned 200. Containers and network removed afterward; no repo files changed
by the proof.

Not re-run live: `web/src/library/useEnrichmentStatus.test.tsx` (in-page
no-reload rendering) — `web/node_modules` is absent in this worktree and no
install was performed. The committed hook test remains the standing proof for
that half; the server halves above were observed live in this session.
