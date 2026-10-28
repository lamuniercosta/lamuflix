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

---

## Q2 — Storage and replacement semantics

**Keel — question and recommendation:**

I recommend collection identity is the positive TMDb collection id, with the returned name; each movie has zero or one collection. Successful enrichment upserts that id and updates its name, replaces prior membership, and clears membership when TMDb successfully returns no collection. Failed lookup or unavailable enrichment does not erase existing metadata. Retain unused collection rows, exclude zero-member rows from facets, and do not cascade collection deletion into movies or add cleanup/backfill work. This keeps next-enrichment semantics and avoids unrequested lifecycle jobs; cost is dormant collection rows. Do you accept these storage and replacement semantics?

**Patron — ruling:**

[from Patron] DEV-424 Q2 ruling: ACCEPT — collection identity is the positive TMDb collection id with its returned name; a movie has zero or one collection; successful enrichment upserts the id/name, replaces prior membership, and clears membership on a successful no-collection result; failed or unavailable enrichment preserves existing metadata; unused collection rows are retained, excluded from facets when they have zero members, and never cascade-delete movies; no cleanup or backfill work is added.

- Ticket authority (note DEV-424 lines 27-30; specs/PRODUCT.md:23-24): CollectionRecord + nullable MovieRecord.CollectionId and EfMovieRepository upsert are the ticket's named persistence path, and backfill is out of scope (next enrichment only) — so membership changes only on a successful enrichment.
- Existing write-path basis (recon-DEV-424 lines 31-33, 68-74; Q1 ruling above): EfMovieRepository.CopyMetadata is the enrichment write seam authorized in Q1; applying replace/clear only on success and preserving data on failure follows that path's semantics and sound engineering practice — a failed lookup is absence of evidence, not evidence of no collection.
- Scope containment (recon-DEV-424 lines 93-100; specs/PRODUCT.md:41-48): a single nullable FK with retained, facet-hidden zero-member rows needs no join table, cascade, or lifecycle job; dormant rows are the accepted cost and add no schema beyond the ticket's named tables, no dependency, and no layer.
