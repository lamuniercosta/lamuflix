# DEV-570 Conclusions

## Q1 — About Cast row, unmapped names
Verdict: Approved as recommended — mapped names open the existing MoreWith panel through the existing page selection path; unmapped names remain plain text, in comma-separated stored order, with a regression test covering both kinds of name.
- Ticket Scope (recon-DEV-570:52-55): Cast names comma-separated, as today a name selects the actor and opens the existing more with panel; hero cast section stays as is — ticket text is authoritative per specs/PRODUCT.md §3.
- Current behaviour (recon-DEV-570:24-26): CastSection routes matching names to onSelect buttons and leaves unmatched names as text; selection renders the existing MoreWithPanel. Reusing it adds no new API shape, layer, or dependency under specs/PRODUCT.md §5.
- No §2.3 escalation: the ruling follows the ticket rather than contradicting it, and departs from no constitution provision.

## Q2 — Wire representation of new fields when values are missing
Verdict: Approved as recommended — when metadata exists, writers and genres are always-present string arrays (empty when missing), collectionName is an always-present nullable string (null when absent), and outer metadata stays null when the movie has no metadata; stored values are projected as-is through LibraryResponses.cs, with OpenAPI/baseline/TypeScript regenerated and the feature Zod parser aligned before UI wiring, and endpoint-JSON assertions for populated and missing cases.
- Ticket (recon-DEV-570:51,66-67): add writers, genres, and the collection name (null when the movie has none) to MovieMetadataResponse on the existing route, with contract/generated-type agreement and an API integration test — ticket text is authoritative per specs/PRODUCT.md §3.
- Stored shape (recon-DEV-570:16-23): MovieMetadata already stores writers/genres as immutable arrays and a nullable collection name, and LibraryResponses.cs already preserves null metadata; projecting as-is needs no new route, schema, data source, or dependency under specs/PRODUCT.md §5.
- No §2.3 escalation: the fields are ticket-specified, so fixing their null/empty representation is a Patron ruling, not a ticket change or constitution departure.
