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

## Q3 — About section when no rows are eligible
Verdict: Approved as recommended — omit the entire About section when metadata is null or every About row has no data; keep the existing no-metadata notice for its current null-metadata condition; with any eligible row, render About <title> in ticket order, omit only empty rows, use Director/Directors and Writer/Writers by displayed count plus fixed Cast, Genres, and Collection, as one heading-labelled dl with dt/dd pairs, existing page tokens, and values wrapping at phone width, tested for fully populated, missing-writers-only, all-rows-empty, null-metadata, singular/plural, and accessible naming.
- Ticket (recon-DEV-570:52-59,64-68): About rows in the stated order, a row with no data is not shown, no-metadata movies show no section and keep the existing notice, dl/dt/dd labelled by the heading at phone width — ticket text is authoritative per specs/PRODUCT.md §3.
- Current page (recon-DEV-570:24,28): the existing NoMetadata state and metadata-derived rendering are preserved; no About-row, singular/plural, or empty-section case exists yet, so the named tests are required.
- Taste (specs/PRODUCT.md §4): Writer/Writers by displayed count parallels the ticket-explicit Director/Directors rule; it is logged tagged [assumed] in specs/DEV-570/ASSUMPTIONS.md. No §2.3 escalation: no ticket change or constitution departure.
