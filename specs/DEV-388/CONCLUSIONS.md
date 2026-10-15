# DEV-388 - Conclusions (Patron rulings)

## Q1 - Delete and director/collection filter+facet parity

**Ruling:** Ticket-required. DEV-388 delivers movie delete (Api endpoint + SPA screen) and director/collection filters + facets (Api + SPA) before Web is retired. Existing Api playback and its `Features:LocalPlay` gating stay unchanged. No owner checkbox.

- Basis: DEV-388 AC, "every MoviesController action (list+filters, details, play, import folder, search, delete, watchlist add/remove, watchlist view) has an Api endpoint and an SPA screen". Delete is named outright, and Web `Index` filters on Director/Collection (`MoviesController.cs:22-23`, recon-DEV-388 section 5). Ticket text counts as decided, so this is not a §2.3(a) escalation.
- Narrowing parity, or moving the gaps to another ticket, would drop from what the ticket delivers. Only that would need `blocked: structural`.
- The route, verb, status codes and DTO fields for delete and for the director/collection facets are a §2.3 item 4 API-shape decision. Patron rules on them separately at spec stage. Neither needs a new project, layer or schema change (recon section 10).
