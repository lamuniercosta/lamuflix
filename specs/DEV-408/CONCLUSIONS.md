# DEV-408 — Patron conclusions

## Q1 — Poster semantics and vocabulary (grill Q1/12)

**Keel asked:** Approve an optional `string? PosterUrl` on Core `MovieMetadata`, included in its equality `State` and in rehydration; the shipped `OmdbMetadataProvider` deserializes OMDB `Poster`, mapping missing/null/blank/`N/A` to null and trimming a present string, with no URI validation and no new dependency. Rationale: lookup still succeeds when artwork is absent, and poster-only changes flow through persistence; cost is a nullable Core value plus new regression cases.

**Ruling: APPROVED as recommended.**
- Ticket authority: DEV-408 Scope items (1) and (2) require the shipped provider to carry the poster and enrichment to persist/clear it (`recon-DEV-408:18-19`); the only shipped carrier from provider to aggregate is `MetadataLookupResult.Found(MovieMetadata)` (`recon-DEV-408:32-33`), so a Core member is forced by the ticket, not a §2.3 new layer. It supersedes, for `PosterUrl` only, the earlier no-Core-counterpart stance (`specs/DEV-19/CONCLUSIONS.md:40`, `specs/DEV-301/CONCLUSIONS.md:55`), which held because no ticket then required it; tests asserting record-only poster preservation are forced edits under this AC.
- No schema change (`poster_url` text nullable exists, `recon-DEV-408:41`) and no new dependency (§2.3 items 1 and 3 untouched); `N/A`/blank normalization to null matches how absence is already expressed for optional metadata and keeps a poster-less lookup a successful `Found`.
- No URI validation is sound: the value is an opaque provider string stored as text; validation would add a failure mode the ticket does not ask for. Vocabulary `PosterUrl` matches the existing record/column (`MovieRecord.PosterUrl`, `poster_url`) and the AC wire name `posterUrl`.
