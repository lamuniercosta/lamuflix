# DEV-424 — Patron grill rulings

Worktree: F:/Dev/LamuFlix.worktrees/feature-424-spec; branch: feature/424-spec.
Authority: DEV-424 ticket text (Maestri note DEV-424), recon-DEV-424, specs/PRODUCT.md, assigned Patron §2.3 rules.
Status: Draft — grill in progress.

---

## Q1 — Scope and data path

[from Patron] DEV-424 Q1 ruling: ACCEPT — extending TMDb response mapping and MovieMetadata to carry optional collection id/name into the existing enrichment write path, persisting CollectionRecord and nullable MovieRecord.CollectionId, stays within ticket scope; the recon-listed supporting-file edits are authorized as necessary consequences under §2.3 item 6, with no new dependency or layer, no backfill, and no grid grouping.

- Ticket authority (note DEV-424 lines 27-30; specs/PRODUCT.md:23-24): scope is CollectionRecord + MovieRecord.CollectionId migration, EfMovieRepository upsert, facet and pill filter; data from TMDb adapter DEV-331; out of scope is grid grouping and backfill (next enrichment only).
- Deferred-to-here basis (recon-DEV-424 lines 68-74, citing specs/DEV-331/brief.md:31, spec.md:28, CONCLUSIONS.md:46): DEV-331 explicitly deferred collections to DEV-424, so TmdbMetadataProvider.Mapping.cs, MovieMetadata, and EfMovieRepository.CopyMetadata are the existing data path this ticket must extend, not new scope.
- §2.3 screen (recon-DEV-424 lines 93-100; specs/PRODUCT.md:41-48): no new NuGet/npm dependency and no new project/layer; schema is the ticket's own named tables (item 3 not triggered beyond them); supporting files (DbContext, configurations, migration, catalog/query/codec, endpoint/handler registration, frontend URL/filter/mocks, regenerated openapi.json/types.ts via generate:api) are forced consequences of the named edits, authorized under item 6 — generated contract files are regenerated, never hand-written.
