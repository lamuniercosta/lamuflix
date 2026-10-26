# DEV-422 — Grill Conclusions

## Q1 — Scope freeze, `@microsoft/signalr` dependency, and constitution/ticket check

**Question (Keel):**
Shall we freeze scope to the ticket refresh mechanism only: pinned `@microsoft/signalr`, `useEnrichmentStatus`, one connection per SPA session, reconnect recovery, TanStack cache updates, removal of DEV-80 enrichment polling including import-placeholder reconciliation dependence, and connection-fake Vitest coverage, preserving Pending visuals and leaving server implementation to DEV-330? I recommend this scope from DEV-422 lines 14-18 and recon sections 1, 6, 8; dependency is already ticket-approved and adds no server NuGet. Please rule with cited basis and flag any constitution departure or ticket contradiction.

**Ruling (Patron):**
Approved — freeze DEV-422 scope strictly to the SPA SignalR enrichment-status refresh mechanism and DEV-80 polling removal, with no ticket contradiction or blocking constitution departure.

- **Scope & boundaries (`DEV-422:14-18`, `recon-DEV-422` §§1, 8, `specs/PRODUCT.md:23-24`)**: Scope is frozen to `useEnrichmentStatus`, one SignalR connection per SPA session with auto-reconnect and pending refetch/invalidation on reconnect, TanStack Query cache updates on `EnrichmentStatusChanged`, removal of DEV-80 polling (`browseRefetchInterval` and import-placeholder polling dependence in `web/src/library/libraryQueries.ts:66-104,193-196`), and connection-fake Vitest coverage; Pending row visuals (`DEV-422:18`) and server `EnrichmentHub` implementation (`DEV-422:14,17`, `docs/adr/0022-signalr-enrichment-status-push.md:9,15`) are out of scope.
- **§2.3 Item 1 — Dependency (`DEV-422:15`, `docs/adr/0022-signalr-enrichment-status-push.md:15`, `specs/PRODUCT.md:43`)**: Adding pinned `@microsoft/signalr` to `web/package.json` (and `web/package-lock.json`) is approved per ticket text and ADR-0022; no server NuGet package is added.
- **Constitution & contract alignment (`.specify/memory/constitution.md:504-507,512-513`, `docs/adr/0022-signalr-enrichment-status-push.md:15-21`, `DEV-422:16`)**: No ticket contradiction or blocking constitution departure — ADR-0022 already supersedes ADR-0008 for the enrichment status push channel (and explicitly marks `constitution.md` text cleanup out of scope), while `.specify/memory/constitution.md:504-506` governs HTTP OpenAPI generation rather than SignalR hub payloads (`DEV-422:16`).
