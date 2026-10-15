# DEV-388 - Conclusions (Patron rulings)

## Q1 - Delete and director/collection filter+facet parity

**Ruling:** (Director/collection part SUPERSEDED by Q3. Delete stands.) Ticket-required. DEV-388 delivers movie delete (Api endpoint + SPA screen) and director/collection filters + facets (Api + SPA) before Web is retired. Existing Api playback and its `Features:LocalPlay` gating stay unchanged. No owner checkbox.

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

---
### Q2 full grill exchange (Keel / Patron)

**Question 2/12 (Keel):** Please settle delete behavior and public contract. I recommend DELETE /api/movies/{id:int}, no request DTO, 204 with no body when removed, 404 for a missing movie, and existing validation/problem conventions for invalid ids. Delete removes library metadata and dependent library associations, never physical media files; SPA details offers a destructive action with confirmation and returns to the library with cached results invalidated. Basis: named delete AC plus existing /api/movies/{id} resource and Api/SPA contract chain. Cost: association cleanup and absent/error cases need behavioral tests. Recon names legacy DeleteMovie at MovieService.cs:328 but does not describe its effects; if deciding parity requires those effects or persistence feasibility, identify the exact missing fact rather than assuming it. What is your ruling?

**Answer (Patron):** Accept your recommendation as stated: DELETE /api/movies/{id:int}, no DTO, 204 / 404 / existing problem conventions. It is a metadata-only delete that never touches files, and the SPA details screen gets a confirmed destructive action that returns to the library and invalidates the cache. No missing fact. Legacy DeleteMovie (MovieService.cs:328-334) only removes the Movies row, with no file I/O. Join tables already cascade (Initial.cs:99-153), so there is no schema change. 404 replaces the legacy silent Ok to match GET /api/movies/{id}. Confirmation copy is logged [assumed] in ASSUMPTIONS.md.

**Rationale and implications:** The public contract and interaction are Patron decisions. The factual assertions about legacy deletion and current relationship cascades are outside recon-DEV-388 sections 5 and 7, which only locate the method and list replacement gaps. Under the bounded-facts instruction these assertions await a recon addendum before supporting implementation planning or a no-schema-change conclusion. Grill paused after question 2 of 12; brief not complete.

---
### Q2 recon closure

Conductor resumed the grill after Wisp filed recon-DEV-388-2 section 13. Sections 13.1-13.2 now substantiate legacy metadata-only deletion and current movie-to-join cascades, with shared actor/director/genre rows surviving. Legacy cascade configuration remains unknown and is not claimed. Q2 no-schema-change reasoning is valid for deletion only. Section 13.2 also establishes that the current seven-table schema has no collection storage, so Q1 collection parity requires a separate explicit ruling.

## Q3 - Collection persistence; director/collection filters (amends Q1)

**Ruling:** DEV-388 delivers no director/collection filters or facets, and no collection persistence, migration or schema change. The ticket's `list+filters` AC is met by the existing P1 filter set on `GET /api/movies` plus `LibraryView`/`FilterPanel`. Movie delete (Q1/Q2) stays in scope. No structural escalation.

- Basis: the locked product plan `F:\Dev\lamuflix-react-pivot-plan.md:47` says "Deferred to P2: Rating range, Director facet, Collection facet", and `:94` lists Director/Collection filters as later work. `:33` says Collections start empty and are not migrated. The constitution (`.specify/memory/constitution.md:533-536`) says that plan owns WHAT is built and outranks other documents. The ticket AC lists the action (`list+filters`), not a filter set, so reading it against the plan is not a contradiction of ticket text.
- New fact since Q1 (recon-DEV-388-2 lines 17 and 33): there is no collection storage at all. Adding it would need a second migration, which conflicts with the constitution's `single fresh Initial migration` (`constitution.md:342`) and DEV-19 SC-001's seven-table set (`specs/DEV-19/spec.md:32,111,135`). That is a departure not worth making for a P2 feature, so the constitution question is moot.
- Q1's director/collection clause was ruled without those facts. Patron's earlier rulings are not precedent against new evidence. Director/Collection facets remain P2 product-plan work: noted, no ticket.
