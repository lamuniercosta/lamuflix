# DEV-388 - Conclusions (Patron rulings)

## Q1 - Delete and director/collection filter+facet parity

**Ruling:** Ticket-required. DEV-388 delivers movie delete (Api endpoint + SPA screen) and director/collection filters + facets (Api + SPA) before Web is retired. Existing Api playback and its `Features:LocalPlay` gating stay unchanged. No owner checkbox.

- Basis: DEV-388 AC, "every MoviesController action (list+filters, details, play, import folder, search, delete, watchlist add/remove, watchlist view) has an Api endpoint and an SPA screen". Delete is named outright, and Web `Index` filters on Director/Collection (`MoviesController.cs:22-23`, recon-DEV-388 section 5). Ticket text counts as decided, so this is not a §2.3(a) escalation.
- Narrowing parity, or moving the gaps to another ticket, would drop from what the ticket delivers. Only that would need `blocked: structural`.
- The route, verb, status codes and DTO fields for delete and for the director/collection facets are a §2.3 item 4 API-shape decision. Patron rules on them separately at spec stage. Neither needs a new project, layer or schema change (recon section 10).
---
### Q1 full grill exchange (Keel / Patron)

**Question 1/12 (Keel):** Recon sections 5-6 establish delete missing in Api and SPA, and director/collection filter and facet gaps. I recommend interpreting every MoviesController action and list+filters parity literally: implement those missing capabilities within DEV-388 before retiring Web, keeping existing Api playback LocalPlay behavior. This honors the ticket rather than weakening parity, but expands delivery beyond mechanical deletion. Do you rule full delete plus director/collection filter/facet parity as ticket-required, with no owner scope checkbox? Cite your basis.

**Answer (Patron):** YES, ticket-required, no owner checkbox. Deliver movie delete (Api+SPA) and director/collection filters+facets (Api+SPA) in DEV-388, with Api playback/LocalPlay unchanged. Basis: the AC names delete and list+filters for every MoviesController action, and Web Index filters on Director/Collection (MoviesController.cs:22-23). Ticket text is decided. Narrowing parity would be the escalation. Route/verb/status/DTO shape is a separate Patron API-shape ruling at spec stage.

**Rationale and implications:** Apply the Patron ruling above. Both gaps are prerequisites to retirement; Quill must not describe existing parity as complete.

## Q2 - Movie delete behavior and public contract

**Ruling:** Accept Keel's recommendation. The contract is `DELETE /api/movies/{id:int}` with no request DTO. It returns 204 with no body on removal and 404 when the movie is missing. Invalid ids follow the existing validation/problem conventions. Delete removes the movie row and its library associations and never touches physical media files. In the SPA, the details screen gets a destructive action with confirmation, then returns to the library with cached results invalidated.

- Basis: the legacy `MovieService.DeleteMovie` (`src/LamuFlix.Web/Services/MovieService.cs:328-334`) only removes the `Movies` row. It does no file I/O, so a metadata-only delete is parity, not a narrowing.
- Join-table cleanup is already `ReferentialAction.Cascade` in `Persistence/Migrations/20260930153520_Initial.cs:99-153`. No schema change or migration is needed (§2.3 item 3 clear).
- API shape (§2.3 item 4): this is the same resource as `GET /api/movies/{id:int}` (`LibraryEndpoints.cs:49`). Returning 404 instead of the legacy silent `Ok()` for a missing movie (`MoviesController.cs:112-123`) matches the Api's existing conventions. The contract chain runs C# → `openapi.json` → `types.ts`.
