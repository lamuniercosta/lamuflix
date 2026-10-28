# DEV-424 — Patron grill rulings

Worktree: F:/Dev/LamuFlix.worktrees/feature-424-spec; branch: feature/424-spec.
Authority: DEV-424 ticket text (Maestri note DEV-424), recon-DEV-424, specs/PRODUCT.md, assigned Patron §2.3 rules.
Status: Draft — grill in progress.

---

## Q1 — Scope and data path

**Keel — question and recommendation (verbatim from first ask):**

/grill-with-docs DEV-424. Worktree F:/Dev/LamuFlix.worktrees/feature-424-spec branch feature/424-spec. Max 12 questions, one at a time. I read DEV-424 and recon-DEV-424. Q1: Confirm scope and data path: extend TMDb response mapping and MovieMetadata to carry optional collection id and name into the existing enrichment write path; persist CollectionRecord and nullable MovieRecord.CollectionId, with no backfill or grid grouping. I recommend treating these data-path additions and the recon-listed supporting files as necessary consequences of the ticket, with no new dependency or layer. DEV-331 explicitly deferred collections here (recon lines 68-74), and ticket scope is DEV-424 note lines 27-30. Please rule with cited basis whether this stays within ticket scope and authorize the supporting-file edits under section 2.3 item 6. No production edits in this grill. Please reply directly with the ruling.

**Patron — ruling:**

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

---

## Q3 — Public API shape (§2.3 item 4)

**Keel — question and recommendation:**

I recommend GET /api/collections named GetCollections returning CollectionDto[] with id, name, movieCount (same types and response conventions as genres); count distinct current library members, omit empty collections, and mirror existing genre facet count/sort behavior rather than counts narrowed by active filters. Add nullable positive integer collectionId to browse request and MovieQuery, ANDed with existing filters; valid unknown id returns an empty page, absent means no constraint, malformed/nonpositive/repeated single-valued input follows existing browse validation conventions with 400 for invalid input. Preserve pagination conventions and include CollectionId in value equality/hash and query codecs. This uses the existing genre chain; cost is wiring the field across all query state paths. Do you accept this explicit public API shape under section 2.3 item 4? Cite basis and record full exchange. Also append the original Q1 question verbatim from my first ask so its full exchange is preserved.

**Patron — ruling:**

[from Patron] DEV-424 Q3 ruling: ACCEPT under §2.3 item 4 — GET /api/collections (operation GetCollections) returns CollectionDto[] of id, name, movieCount mirroring genre facet types, count/sort and response conventions, counting distinct current library members and omitting empty collections without narrowing by active filters; nullable positive-integer collectionId on the browse request and MovieQuery is ANDed with existing filters, absent means unconstrained, a valid unknown id returns an empty page, malformed/nonpositive/repeated input follows existing browse validation with 400, pagination is preserved, and CollectionId joins MovieQuery equality/hash and query codecs.

- Item 4 trigger and cure (recon-DEV-424 lines 93-100 at item 4; specs/PRODUCT.md:46): the ticket implies a collections facet and collectionId filter in substance but spells neither the literal route, DTO fields, nor parameter validation, so this explicit ruling — not a silent assumption — fixes that public shape.
- Mirror basis (recon-DEV-424 lines 47-55, 57-66; note DEV-424 lines 27-30): FacetEndpoints /api/genres, GenreFacet/CollectionDto chain, and the MovieQuery/MovieQueryString/LibraryEndpoints browse pipeline are the existing chain the ticket names (FacetEndpoints collections mirror genres; MovieQuery collectionId); following it adds no new layer, route family, or pagination convention.
- Contract chain (recon-DEV-424 line 89): the new route/DTO flows C# DTO → regenerated openapi.json → generated types.ts via npm run generate:api, never hand-edited, per the Q1 §2.3 item 6 authorization.

---

## Q4 — Filter Panel interaction contract

**Keel — question and recommendation:**

I recommend a Collection section in the existing Filter Panel using existing chip controls with single-select toggle semantics: select one id, selecting another replaces it, selecting the active chip clears it. Canonical URL key collectionId carries a single positive id; reload/back/forward restore selection; changing or clearing it resets page as other filters do and preserves other filters/sort. Add a removable active-filter chip and include collection in clear-all. Keep an unknown but valid URL id applied even when absent from facets, with fallback label Collection <id>, so the UI never silently broadens results; an empty facet has no selectable options. Facet fetch uses the existing query/client pattern and generated DTO, with existing loading/error behavior. These labels are taste assumptions to log in ASSUMPTIONS.md. Palette owns visual layout and design.md remains required before plan challenge. Do you accept this interaction contract? Cite basis and record full exchange plus taste assumptions.

**Patron — ruling:**

[from Patron] DEV-424 Q4 ruling: ACCEPT — a Collection section in the existing Filter Panel uses existing chip controls with single-select toggle semantics (select, replace on another, clear on re-select); canonical URL key collectionId carries one positive id restored on reload/back/forward, changing or clearing resets page like other filters and preserves other filters/sort; a removable active-filter chip and clear-all inclusion are added; a valid unknown URL id stays applied with fallback label Collection <id> rather than silently broadening results, and an empty facet offers no selectable options; facet fetch follows the existing query/client pattern with the generated DTO and existing loading/error behavior; Palette owns visual layout and design.md remains required before plan challenge.

- Ticket authority (note DEV-424 lines 27-30; specs/PRODUCT.md:23-24): single-select pills in URL in the Filter Panel (DEV-322) is the ticket's named UI scope; single-select toggle, one-value URL key, and page-reset-on-change are its direct consequences, and no grid grouping is introduced.
- Existing-pattern basis (recon-DEV-424 lines 76-91): lamu-chip controls in FilterPanel.tsx, LibraryView active-filter chips and clear behaviour, filterQuery/libraryUrl/browseParams URL state, and the useQuery client.GET facet fetch with generated types are the seams to reuse; no new component vocabulary, state library, or hand-rolled DTO is introduced (§2.3 items 2 and 6 remain clear).
- Taste logging (specs/PRODUCT.md:26-30): the section label Collection, fallback label Collection <id>, and no-options empty facet are logged tagged [assumed] in specs/DEV-424/ASSUMPTIONS.md; visual layout stays with Palette and design.md is still required before plan challenge, so no visual decision is taken here.

---

## Q5 — Edge-case semantics: atomicity, whole-metadata replacement, malformed collections

**Keel — question and recommendation:**

I recommend applying collection row upsert and movie membership atomically inside the existing repository save/transaction boundary, with unique identity by TMDb id and coverage for two movies enriched into the same collection. The new optional metadata value follows existing whole-metadata replacement semantics: successful metadata without collection clears membership, including a successful non-TMDb fallback; do not introduce provider provenance or partial-field merging. A malformed collection object (nonpositive id or blank name) follows existing TMDb malformed-response handling, not a persisted invalid row or an invented collection. Cost: no preservation of collection through a successful provider result that lacks it; this keeps absence distinct from failure without a new abstraction. Do you accept these edge-case semantics, or does ticket intent require a different ruling? Cite basis and record full exchange.

**Patron — ruling:**

[from Patron] DEV-424 Q5 ruling: ACCEPT — collection row upsert and movie membership are applied atomically inside the existing repository save/transaction boundary with unique identity by TMDb id (including two movies enriched into the same collection); the optional collection value follows existing whole-metadata replacement, so successful metadata without a collection clears membership including a successful non-TMDb fallback, with no provider provenance and no partial-field merging; a malformed collection object (nonpositive id or blank name) follows existing TMDb malformed-response handling and is never persisted as an invalid or invented row. Ticket intent requires no different ruling.

- Consistency with Q2 (specs/DEV-424/CONCLUSIONS.md Q2 ruling): success replaces/clears and failure preserves were already ruled; Q5 only extends that same success/failure line to whole-metadata replacement and fallback success, so absence on success stays distinct from failure without a provenance abstraction.
- Existing-path basis (recon-DEV-424 lines 31-33, 68-74; note DEV-424 lines 27-30): EfMovieRepository upsert/CopyMetadata is the ticket's named write seam and the enrichment save boundary; atomic upsert-by-TMDb-id inside it prevents partial collection/membership states and duplicate rows when movies share a collection, using the ticket's own tables only.
- Malformed-input basis (Q3 ruling; specs/PRODUCT.md:23-24): collection identity was ruled a positive id with a returned name, so a nonpositive id or blank name cannot form a valid row; routing it through existing TMDb malformed-response handling reuses settled provider behaviour instead of inventing a persisted invalid state or a new error path.

---

## Q6 — Task order, test strategy, and gate plan

**Keel — question and recommendation:**

I recommend task order: (1) metadata/TMDb mapping plus focused adapter/domain tests; (2) CollectionRecord, DbContext/configuration, generated EF migration, atomic repository upsert and real-database tests including shared-id concurrency, replace/clear/failure preservation and old rows remaining null; (3) collection catalog/facet/query/API, codec/equality/hash/validation tests, AND-filter/paging and counts using Testcontainers where translation matters; (4) regenerate C# DTO to OpenAPI to TypeScript; (5) web-implement TDD for facet fetch, select/replace/toggle/clear, URL restoration, page reset, unknown id and loading/error/empty cases, then web gates. Existing test/assertion/mock tools only. Gate expectations: normal backend verify/refactor/architect and ship-review gates at their pipeline stages, full suite, property tests, security/format and configured mutation threshold without lowering it; three review axes Sentry/Ledger/Compass required on M; web lint/API drift/test/build gates; exit-2 scope-empty/opt-out/N-A accurately distinct from pass. Baseline note DEV-424 lines 64-84 is evidence only, never a rerun. No implementation or Gate 1 clearance here; ui plan challenge waits for Palette design.md. Accept this approach, ordering, test strategy and gate plan? Cite basis and record full exchange.

**Patron — ruling:**

[from Patron] DEV-424 Q6 ruling: ACCEPT — task order (1) metadata/TMDb mapping with focused adapter/domain tests, (2) CollectionRecord/DbContext/configuration/generated EF migration/atomic repository upsert with real-database tests, (3) catalog/facet/query/API with codec/equality/hash/validation and AND-filter/paging/count tests using Testcontainers where translation matters, (4) C# DTO → OpenAPI → TypeScript regeneration, (5) web-implement TDD for the Q4 interactions then web gates; existing test/assertion/mock tools only, all configured gates at their pipeline stages with no threshold lowered, exit-2 outcomes reported distinctly from pass, the DEV-424 lines 64-84 baseline treated as evidence only, and no implementation or Gate 1 clearance in this grill with the ui plan challenge waiting for Palette design.md.

- Dependency order (Q1/Q3/Q5 rulings above; recon-DEV-424 lines 68-74, 89): the data path must exist before persistence, persistence before the facet/query surface, and the C# DTO before OpenAPI/TypeScript regeneration and web consumption — the proposed sequence is the contract chain in dependency order, with generated files regenerated rather than hand-written.
- Test-strategy basis (AGENTS.md non-negotiables; recon-DEV-424 lines 38-45, 53-66, 91): real-database/Testcontainers coverage is required where EF translation or serialisation matters and the data-access driver is never mocked for that behaviour; shared-id concurrency, replace/clear/failure-preservation, pre-existing rows staying null, and codec/equality/hash cases are the Q2/Q3/Q5 semantics that must be proven, using only the repo's existing assertion and mocking libraries.
- Gate basis (specs/PRODUCT.md:12; AGENTS.md verification commands and exit-code table): full suite, property, security/format, backend verify/refactor/architect and ship-review gates, the three M-tier review axes, and web lint/API-drift/test/build gates run at their own stages; the configured mutation threshold is never lowered to pass, and exit 2 is reported as SKIPPED (scope-empty), SKIP (opt-out), or NOT APPLICABLE as applicable — never folded into PASS.
