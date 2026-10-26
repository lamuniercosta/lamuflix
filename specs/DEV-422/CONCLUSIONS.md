# DEV-422 — Grill Conclusions

## Q1 — Scope freeze, `@microsoft/signalr` dependency, and constitution/ticket check

**Question (Keel):**
Shall we freeze scope to the ticket refresh mechanism only: pinned `@microsoft/signalr`, `useEnrichmentStatus`, one connection per SPA session, reconnect recovery, TanStack cache updates, removal of DEV-80 enrichment polling including import-placeholder reconciliation dependence, and connection-fake Vitest coverage, preserving Pending visuals and leaving server implementation to DEV-330? I recommend this scope from DEV-422 lines 14-18 and recon sections 1, 6, 8; dependency is already ticket-approved and adds no server NuGet. Please rule with cited basis and flag any constitution departure or ticket contradiction.

**Ruling (Patron):**
Approved — freeze DEV-422 scope strictly to the SPA SignalR enrichment-status refresh mechanism and DEV-80 polling removal, with no ticket contradiction or blocking constitution departure.

- **Scope & boundaries (`DEV-422:14-18`, `recon-DEV-422` §§1, 8, `specs/PRODUCT.md:23-24`)**: Scope is frozen to `useEnrichmentStatus`, one SignalR connection per SPA session with auto-reconnect and pending refetch/invalidation on reconnect, TanStack Query cache updates on `EnrichmentStatusChanged`, removal of DEV-80 polling (`browseRefetchInterval` and import-placeholder polling dependence in `web/src/library/libraryQueries.ts:66-104,193-196`), and connection-fake Vitest coverage; Pending row visuals (`DEV-422:18`) and server `EnrichmentHub` implementation (`DEV-422:14,17`, `docs/adr/0022-signalr-enrichment-status-push.md:9,15`) are out of scope.
- **§2.3 Item 1 — Dependency (`DEV-422:15`, `docs/adr/0022-signalr-enrichment-status-push.md:15`, `specs/PRODUCT.md:43`)**: Adding pinned `@microsoft/signalr` to `web/package.json` (and `web/package-lock.json`) is approved per ticket text and ADR-0022; no server NuGet package is added.
- **Constitution & contract alignment (`.specify/memory/constitution.md:504-507,512-513`, `docs/adr/0022-signalr-enrichment-status-push.md:15-21`, `DEV-422:16`)**: No ticket contradiction or blocking constitution departure — ADR-0022 already supersedes ADR-0008 for the enrichment status push channel (and explicitly marks `constitution.md` text cleanup out of scope), while `.specify/memory/constitution.md:504-506` governs HTTP OpenAPI generation rather than SignalR hub payloads (`DEV-422:16`).


## Q2 — Hub payload-type conflict (`DEV-422:16` vs DEV-330 FR-011) and hub contract shape

**Question (Keel):**
How do we resolve the payload-type conflict? DEV-422 line 16 explicitly requires hand-typed hub payload types, while frozen unlanded DEV-330 PR #264 at b565e932 FR-011 requires generated `EnrichmentStatusChangedDto` via C# to OpenAPI to `types.ts` (recon lines 118, 130-133). I recommend generated type consumption with DEV-422 owner checkbox `blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?` This changes ticket delivery text and cannot be silently ruled away. Freeze route `/hubs/enrichment`, event `EnrichmentStatusChanged`, one argument `{id:number,enrichmentStatus:Pending|Enriched|NotFound|Failed}`, with explicit upstream landing and contract drift pickup dependency.

**Ruling (Patron):**
`blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?` — consume generated `EnrichmentStatusChangedDto` from `web/src/api/types.ts` subject to the owner checkbox on the DEV-422 spec PR, and freeze the `/hubs/enrichment` + `EnrichmentStatusChanged` contract shape.

- **§2.3(a) Ticket change escalation (`DEV-422:16`, `specs/PRODUCT.md:36-38`, `.specify/memory/constitution.md:504-506`, `recon-DEV-422:118`)**: `DEV-422:16` explicitly specifies hand-typed hub payload types, whereas the C# DTO → OpenAPI (`web/src/api/openapi.json`) → generated TypeScript (`web/src/api/types.ts`) contract chain and DEV-330 FR-011 (`specs/DEV-330/spec.md:70-72,106`, `specs/DEV-330/contracts/enrichment-hub.md:37-40`) require consuming the generated `EnrichmentStatusChangedDto` schema type without hand-rolled duplicates; because this changes ticket text (`DEV-422:16`), carry the owner checkbox on the DEV-422 spec PR keeping Gate 1 closed until answered.
- **§2.3 Item 4 — Public hub contract shape (`specs/PRODUCT.md:46`, `recon-DEV-422:102-117`, `web/src/api/types.ts:83`)**: Freeze hub route `/hubs/enrichment` (server-to-client only), event name `EnrichmentStatusChanged`, and single positional payload argument `{ id: number, enrichmentStatus: "Pending" | "Enriched" | "NotFound" | "Failed" }` (`EnrichmentStatusChangedDto`, camelCase wire JSON, string enum names only).
- **Provenance & upstream landing dependency (`DEV-422:17`, `recon-DEV-422:129-133`)**: Record that DEV-330's hub contract is currently frozen-but-unlanded spec text on PR #264 (`b565e932`, Gate 1 open, absent from `HEAD` `52e44366`), so DEV-422 implementation depends on DEV-330 landing upstream and picking up the committed `web/src/api/openapi.json` / `web/src/api/types.ts` schema before contract-drift verification (`npm run lint` / `check:api`).
