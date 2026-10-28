# DEV-424 - Persist TMDb collections and add the Collection facet pill filter

Status: Phase 2 step 1 complete. Patron closed the grill on Q1-Q8 (8 of maximum 12 questions). This brief freezes the decisions for Quill; it is not a spec, plan approval, implementation receipt, or Gate 1 clearance.

## Identity and authority

- Worktree: F:/Dev/LamuFlix.worktrees/feature-424-spec
- Branch: feature/424-spec
- Recon base: f698aeceb6b6af50a397d076b83ed178566641b0; Patron closure commit: 9ea787b1a359514c8ebe7765652c57d6613cad56.
- Size M; ui: ticket. Dependencies DEV-331 and DEV-322; DEV-358 depends on this delivery.
- Authoritative inputs: Maestri notes DEV-424 lines 27-30 (ticket) and recon-DEV-424; cited PRODUCT.md passages in Patron rulings; CONCLUSIONS.md Q1-Q8. No independent code reconnaissance was performed by Keel.
- Patron deliberately authorized public API shape under section 2.3 item 4 and the recon-listed supporting-file edits under item 6. No new dependency, project, architectural layer, extra schema beyond ticket tables, LocalPlay, secrets, or Process.Start work is authorized.
- No grill decision contradicts the ticket or requires a constitution departure; no owner escalation was opened. No ADR is required (Q8). CONTEXT.md is the feature glossary; ASSUMPTIONS.md records taste labels.

## Frozen scope and behavior

1. Extend the existing TMDb response mapping and MovieMetadata with optional collection id/name, then carry that value through existing enrichment persistence. DEV-331 deferred collections here; this extension is part of the ticket, not a new provider.
2. Collection identity is a positive TMDb id plus returned name. A movie belongs to zero or one collection. Add CollectionRecord and nullable MovieRecord.CollectionId through a generated EF migration; existing movies remain null until next enrichment.
3. Upsert the collection/name and replace membership atomically in the existing repository save boundary. Shared collection ids must not create duplicate records, including concurrent enrichment. Successful metadata without a collection clears membership, including successful non-TMDb fallback. Failed/unavailable enrichment preserves metadata. No provider provenance or partial-field merging. Invalid collection id/name uses existing TMDb malformed-response handling and is never persisted.
4. Retain unused collection rows but hide them from facets; no collection deletion cascades into movies. No orphan cleanup job.
5. GET /api/collections, operation GetCollections, returns CollectionDto[] containing id, name, movieCount with the genre facet types/response/count/sort conventions. Count distinct current library members, omit zero-member collections, and do not narrow counts by active filters.
6. Add nullable positive integer collectionId to browse request, MovieQuery, predicates, equality/hash, and query codecs. AND it with other filters. Absence means unconstrained; a valid unknown id gives an empty page; malformed, nonpositive, or repeated single-valued API input returns 422 ProblemDetails. Preserve existing pagination conventions. The original Q3 status is superseded by Patron C1 in CONCLUSIONS.md; the ticket delivery is unchanged.
7. Use existing Filter Panel chips in a Collection section. One selected id at a time: selecting another replaces it, reselecting clears it. URL key collectionId restores through reload and back/forward. Changes/clear reset page and preserve other filters/sort. Include a removable active chip and clear-all. A valid unknown id remains applied with fallback label Collection <id>; empty facets provide no selectable options. Reuse existing facet fetching/loading/error patterns and generated DTOs. Palette owns visual layout.
8. Contract generation is strictly C# DTO -> OpenAPI -> generated TypeScript. Never hand-edit generated contracts or create duplicate frontend DTOs.

## Loop discipline (Patron Q7)

**Closing bar:** Critical, High, and Medium findings block code-review/ship-review closure. Low findings require explicit disposition. Every finding from Sentry, Ledger, and Compass requires a recorded ruling; all three reports are mandatory for this M ticket. Missing axis/evidence is blocked, never a skip or waiver.

**Frozen review scope:** The Q1-Q6 TMDb collection metadata path, CollectionRecord and nullable movie membership/migration/upsert, genre-mirrored collection facet API, collectionId query/codec/filter, generated contracts, existing Filter Panel single-select URL/chips, and directly required tests/gates. No backfill, grid grouping, provider provenance/partial merging, cleanup job, new dependency or new layer; anything else is a follow-up issue, not a finding in this round.

**Round cap:** Hard maximum of two review/remediation rounds. Any retained blocker or missing axis/evidence after the cap is reported blocked to Conductor, without an extra review or automatic waiver. Review closure, Gate 1, and user-owned merge are distinct states. Agents never merge.

## Approach and task ordering (Patron Q6)

1. Extend metadata/TMDb mapping with focused domain and adapter tests.
2. Add collection persistence model/configuration, generated migration and atomic repository write path; prove storage semantics on a real database.
3. Add catalog/facet/query/API behavior with codecs, validation, equality/hash, filtering/count/paging tests.
4. Regenerate OpenAPI and TypeScript from the C# contract.
5. Implement web behavior via web-implement TDD, consuming the generated types; run web gates.

Use existing projects, abstractions, component patterns, assertion/mocking libraries and tooling. Do not rename generic sequence helpers such as HasSameCollectionValues; they do not represent TMDb Collections.

## Affected-file manifest (bounded by recon and Q1 authorization)

This is the authorized landing-zone manifest for Quill to translate into tasks, not a claim that implementation already exists. New siblings follow the corresponding genre patterns; no unrelated rewrite is authorized.

- Core: src/LamuFlix.Core/Domain/MovieMetadata.cs; Library/MovieQuery.cs and MovieQueryString.cs; Ports/IMovieCatalog.cs; new collection facet/query/handler siblings under the existing Core feature/port locations.
- Infrastructure: src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Mapping.cs (including response DTO); Persistence/Records/CollectionRecord.cs and MovieRecord.cs; Persistence/LamuFlixDbContext.cs; Persistence/Configurations/MovieConfiguration.cs and new CollectionConfiguration.cs; generated Persistence/Migrations migration/designer and LamuFlixDbContextModelSnapshot.cs; Persistence/EfMovieRepository.cs, EfMovieCatalog.cs, MovieRecordQueryExtensions.cs; existing query-validator seams where required by collectionId validation. Preserve existing consumers of MovieMetadata/MovieQuery.
- API: src/LamuFlix.Api/Endpoints/FacetEndpoints.cs, LibraryEndpoints.cs, BrowseMoviesRequest.cs; HandlerRegistration.cs; collection DTO/query response following the existing genre chain.
- Generated web contracts: web/src/api/openapi.json and types.ts, regenerated via npm run generate:api; API drift checked through existing tooling.
- Web: web/src/features/library/FilterPanel.tsx and filterQuery.ts; web/src/library/libraryUrl.ts, browseParams.ts, LibraryView.tsx; existing mocks/handlers.ts and fixtures.ts. Style edits only as required by Palette design and existing chip patterns.
- Tests: focused adapter/domain tests beside existing tests; integration repository, catalog facets/browse/paging and FacetEndpointsTests/LibraryEndpointsTests; unit MovieQuery/codec/validator/property fixtures; web FilterPanel.test.tsx, filterQuery.test.ts, LibraryView.toolbar.test.tsx, libraryUrl.test.ts, API client/type checks and mocks tests. Add focused sibling tests where existing files would obscure the feature, within these existing test projects/directories.
- Planning artifacts: this brief, CONCLUSIONS.md, ASSUMPTIONS.md, CONTEXT.md; Quill owns spec.md, plan.md, tasks.md and M-tier clarify/checklist output. Palette owns design.md.

## Test strategy and gate expectations

Prove mapping with/without a valid collection and malformed collection objects; persistence upsert/name updates, shared-id concurrency, replacement, clear on success, preservation on failure, old rows remaining null, and no cascade loss. Use Testcontainers for actual EF query translation/serialization and migration behavior; never mock the data-access driver for those cases.

Prove populated/empty facet payloads and distinct member counts, exclusion of orphan rows, genre-equivalent ordering, positive/invalid/repeated/unknown collection ids, AND composition, paging, query equality/hash and codec round trips with the existing property-test conventions. Prove UI select/replace/toggle/clear/clear-all, page reset preserving other filters, URL reload/back-forward, unknown-id fallback and loading/error/empty behavior.

At their pipeline stages run the configured backend build/analyzer/complexity/InspectCode, property, security, format, full test suite and architect/mutation gates; preserve configured thresholds. Run web lint/API-drift, tests and build via the web workflow. M-tier ship review requires Sentry, Ledger and Compass. A surviving mutant is a missing test; never lower a threshold. Exit-2 SKIPPED (scope-empty), SKIP (opt-out) and mutation N/A remain distinct from PASS; a gate that cannot run has not passed. Gauge baseline in DEV-424 note lines 64-84 is recorded evidence, not a rerun or delivery clearance. No implementation gates were run for this planning-only brief.

## Handoff and remaining phase boundaries

Quill drafts spec/plan/tasks from this brief, including the M-tier clarify/checklist work; the draft must not contradict these rulings or glossary. Keel then performs read-only speckit-analyze and checks plan/tasks against the brief, routing one numbered fix list per round. Await Palette design.md before ui plan challenge. Challenge adjudication/freeze, Gate 1 and implementation remain outstanding; this grill does not clear them. Recheck DEV-331/DEV-322 landing and relevant main/contracts at Phase B pickup, and route any missing facts as needs recon to Conductor. The glossary is ready for speckit-specify; no new ADR is needed.

## Complete grill record

The following is the full Patron-maintained append-only Q1-Q8 record, copied at brief creation so every answer and its cited basis is carried into this brief. The Q1 verbatim exchange was appended after its initial ruling; both entries are preserved.

# DEV-424 — Patron grill rulings

Worktree: F:/Dev/LamuFlix.worktrees/feature-424-spec; branch: feature/424-spec.
Authority: DEV-424 ticket text (Maestri note DEV-424), recon-DEV-424, specs/PRODUCT.md, assigned Patron §2.3 rules.
Status: Grill closed — Q1–Q8 ruled, no owner escalation. Keel authorized to write brief.md; Palette design.md, analyze/challenge, and Gate 1 remain separate and pending.

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

---

## Q7 — Closing bar, frozen scope, and review-round cap

**Keel — question and recommendation:**

I recommend Critical, High and Medium findings block code-review/ship-review closure; Low findings need explicit disposition, and all findings from all Sentry/Ledger/Compass reports require a recorded ruling. Freeze scope to the agreed TMDb collection metadata path, CollectionRecord and nullable movie membership/migration/upsert, genre-mirrored collection facet API, collectionId query/codec/filter, generated contracts, existing Filter Panel single-select URL/chips and directly required tests/gates; no backfill, grid grouping, provider provenance/partial merging, cleanup job, dependency or new layer; anything else is a follow-up issue, not a finding in this round. Recommend a hard cap of two review/remediation rounds; any retained blocker or missing axis/evidence after the cap is blocked and returned to Conductor, never an extra review or automatic waiver. Gate 1 and user-owned merge remain separate. Cost is stopping with an explicit blocker rather than expanding this ticket. Do you accept the closing bar, frozen scope and two-round cap? Cite basis and record full exchange.

**Patron — ruling:**

[from Patron] DEV-424 Q7 ruling: ACCEPT as the standing closing bar for this chain — Critical, High and Medium findings block code-review/ship-review closure, Low findings require an explicit disposition, and every Sentry/Ledger/Compass finding requires a recorded ruling; scope is frozen to the Q1–Q6 collection path (TMDb metadata, CollectionRecord and nullable membership/migration/upsert, genre-mirrored facet API, collectionId query/codec/filter, generated contracts, Filter Panel single-select URL/chips, directly required tests/gates) with no backfill, grid grouping, provider provenance or partial merging, cleanup job, dependency, or new layer, anything else going to a follow-up issue rather than a finding in this round; review/remediation is hard-capped at two rounds, after which any retained blocker or missing axis/evidence is blocked and returned to Conductor with no extra review and no automatic waiver; Gate 1 and user-owned merge remain separate.

- Scope basis (note DEV-424 lines 27-30; specs/PRODUCT.md:23-24; Q1–Q6 rulings above): the frozen scope is exactly the ticket's named delivery plus its ruled necessary consequences; exclusions (backfill, grid grouping) are the ticket's own out-of-scope lines, and provenance/merging/cleanup were excluded in Q2/Q5, so widening in review would contradict the ticket rather than complete it.
- Closing-bar basis (Q6 ruling; AGENTS.md gate rules): gates and the three M-tier review axes were already ruled mandatory at their stages with no threshold lowered and no exit-2 folded into PASS; a severity bar where Critical/High/Medium block and every finding gets a recorded disposition is the same no-silent-waiver principle applied to review findings.
- Cap basis (assigned Patron §2.3 standing-ruling rule; specs/PRODUCT.md:19): two rounds bound the loop without weakening any gate — a surviving blocker after the cap is reported blocked to Conductor, and neither Gate 1 nor the user-owned merge (never Patron's or any agent's to perform) is affected by review closure.

---

## Q8 — Shared understanding, glossary, and grill closure

**Keel — question and recommendation:**

I will write brief.md containing the Q1-Q7 decisions, full grill exchanges, closing bar/frozen scope/two-round cap, recon-bounded affected-file manifest, ordered approach/test/gate plan, and handoff to Quill for spec/plan/tasks plus M-tier clarify/checklist. Vocabulary: Collection means TMDb named grouping identified by positive TMDb id; Collection membership means one optional movie association; Collection facet means nonempty library collections with counts. This is distinct from generic sequence equality helpers such as HasSameCollectionValues; no helper rename or unrelated rewrite is authorized. Recommend feature-local CONTEXT.md for these glossary terms; no ADR is needed for this M-sized extension of existing patterns. Record no owner escalation from the grill; Palette design remains pending before plan challenge, analyze/challenge and Gate 1 stay separate, dependencies rechecked at Phase B. Do you confirm shared understanding and close the grill on Q1-Q8, authorizing me to write the final brief? Please record full exchange and glossary, then report directly when your files are complete so I can write without concurrent edits.

**Patron — ruling:**

[from Patron] DEV-424 Q8 ruling: CONFIRMED — shared understanding on Q1–Q7 stands as recorded, the grill is closed on Q1–Q8 with no owner escalation, and Keel is authorized to write brief.md (Q1–Q7 decisions and full exchanges, closing bar/frozen scope/two-round cap, recon-bounded affected-file manifest, ordered approach/test/gate plan, Quill handoff for spec/plan/tasks plus M-tier clarify/checklist) once this record is committed; a feature-local CONTEXT.md carries the glossary below, no ADR is required, Palette design.md remains pending before plan challenge, analyze/challenge and Gate 1 stay separate, and dependencies are rechecked at Phase B.

- Closure basis (note DEV-424 lines 27-30; specs/PRODUCT.md:23-24, 36-39): every question was answered from the ticket, recon, or ruled §2.3 items 4/6; nothing contradicts the ticket text and no constitution departure was judged necessary, so neither owner-escalation condition is met and Gate 1 is neither opened nor closed by this grill.
- No-ADR basis (recon-DEV-424 lines 93-100; Q1–Q7 rulings): the feature extends existing records, repository, facet, query, and chip patterns with no new dependency, project, layer, or provider — an M-sized extension of settled patterns, so a feature-local glossary suffices and dead or unrelated helpers (including HasSameCollectionValues, recon-DEV-424 line 60) are not renamed or rewritten under this ticket.
- Handoff basis (Q4/Q6 rulings): Keel writes brief.md without concurrent Patron edits; Quill, Palette design.md, analyze/challenge, and Gate 1 proceed as separate pipeline stages, with dependency state rechecked at Phase B.

**Glossary (for feature-local CONTEXT.md):**

- **Collection**: a TMDb named grouping of movies, identified by its positive TMDb collection id and carrying the returned name.
- **Collection membership**: the single optional association from one movie to one Collection (nullable MovieRecord.CollectionId); a movie has zero or one membership.
- **Collection facet**: the library facet listing nonempty Collections with their distinct current member counts, exposed by GET /api/collections and selected through the single-select Filter Panel chips.
- Not a Collection: generic sequence-comparison helpers such as HasSameCollectionValues use collection in the programming sense only and are unrelated to TMDb Collections.

---

## C1 decision amendment — invalid collectionId status

Patron corrected the original Q3 status on 2026-10-28, recorded in CONCLUSIONS.md C1 at commit 2754a161. Malformed, nonpositive, and repeated single-valued collectionId return 422 ProblemDetails. This amendment supersedes only the 400 status in the historical Q3 exchange above; the original questions and answers remain preserved as historical evidence.

Basis: constitution Principle V requires validation failures to return 422 through the single exception handler. Patron C1 cites the existing browse raw-query ValidationException path and confirms that the ticket specifies no status code. The correction is within Patron authority over public API shape under section 2.3 item 4, changes no ticket delivery, and requires neither a constitution departure nor an owner checkbox. Quill must propagate 422 through spec/plan/tasks and validation tests. All other Q1-Q8 decisions, review cap, and phase boundaries stand.

---

## Phase 2 step 6 decision amendment

Authority: Patron CONCLUSIONS.md commit 7a335ac3. These rulings amend the implementation decisions above; historical grill exchanges remain intact. Challenge disposition and plan freeze are separate and still pending.

[from Patron] DEV-424 Phase 2 step 6 challenge rulings: all recommendations ACCEPTED as specified below; no ticket contradiction, no constitution departure, no owner checkbox. Patron edits CONCLUSIONS.md only; Quill applies these to plan/tasks/spec wording.

1. Sentry F1 + Ledger F7 (atomic natural-key upsert): CollectionRecord uses the TMDb int id as primary key with ValueGeneratedNever, no surrogate key. Inside the existing EfMovieRepository save/transaction boundary, a collection unique/PK conflict from a concurrent insert is handled by rolling back/resetting the failed unit, re-reading the now-existing collection row, and retrying the whole atomic save (collection upsert plus movie membership) - never a partial catch-and-continue in the same change tracker. This retry is a required mechanism to implement and prove, not a recon-confirmed existing behaviour; do not claim it already works. The Testcontainers oracle must force the race (two movies enriched concurrently into the same new collection id) and assert exactly one collection row and both movies members, with no conflict surfaced.
Basis: Q5 ruling (specs/DEV-424/CONCLUSIONS.md lines 81-85) already requires atomic upsert by TMDb id inside the existing repository save boundary including two movies sharing a collection; Keel ask records that existing retry applicability is not recon-confirmed.

2. Sentry F2 (fallback clear): Q5 is RECONFIRMED - a successful non-TMDb fallback is a successful whole-metadata replacement without a collection, so it clears prior MovieRecord.CollectionId; failed or unavailable enrichment still preserves membership. Residual accepted and to be recorded: membership can stay cleared until a later successful TMDb enrichment restores it. Require an explicit fallback-clear test (prior member, successful fallback without collection, membership null; plus failure-preserves coverage). No new release-note scope is added.
Basis: Q5 ruling and whole-metadata replacement basis (CONCLUSIONS.md lines 81-85); Sentry F2 as put by Keel.

3. Ledger F1/F5 (Filter Panel semantics): Use the existing Genre fieldset/legend structure and label.lamu-chip with an sr-only checkbox input, controlled: checking a chip selects or replaces the single collection, unchecking the active chip clears it. Not radio inputs, because reselect-to-clear is required Q4 toggle semantics. Add no new loading skeleton and no aria-busy; reuse the existing facet loading/error patterns. An empty facet renders the section heading only, with no chips and no helper copy. The unknown-id fallback chip (Collection <id>) appears only in the active-filter toolbar, never as a Collection section option, and remains present in the toolbar during facet loading, error, and empty states so the applied selection is never silently dropped or broadened.
Basis: Q4 ruling (CONCLUSIONS.md lines 65-69) single-select toggle, unknown id stays applied with fallback label, existing facet fetch/loading/error patterns, Palette owns visual layout in design.md.

4. Sentry F3 + Ledger F2 (collectionId parsing and validation, per C1): BrowseMoviesRequest.CollectionId is string? (raw), with a CollectionIdKey const for collectionId in LibraryEndpoints, read through the existing single-value ReadNumber path plus repeated-key detection on the raw query. Malformed, repeated, nonpositive, and empty API values are collected as validation failures and return 422 via the existing ValidationException mapping; absent remains unconstrained, positive remains valid including unknown ids returning an empty page. MovieQueryValidator carries the positive nullable rule (null or greater than zero) for MovieQuery.CollectionId, and the MovieQueryString codecs reject nonpositive or otherwise invalid values rather than encoding them as valid state, so direct query construction and URL round-trip cannot bypass validation.
Basis: C1 ruling (CONCLUSIONS.md lines 150-154), constitution Principle V, and Q3 nullable positive collectionId as ruled there.

5. Ledger F6 (facet types and handler chain): CollectionFacet and CollectionDto are (int Id, string Name, int MovieCount). Ledger's long count assertion is rejected. Create no validator for the parameterless GetCollectionsQuery, and preserve the existing AddHandler registration chain unchanged.
Basis: recon line 52 and Compass line 21 as reported by Keel (genre facet count is int) and Q3 genre-mirroring (CONCLUSIONS.md Q3 ruling); a parameterless query has no input for Principle V validation to reject, so no empty validator is invented.

6. Ledger F3 (test sequencing): Backend tests are written alongside each backend task (Core, Infrastructure, API in the same phase) and the backend suite is all green before the contract-generation and web handoff checkpoint. Web work remains red-first TDD, unchanged. This sequencing waives no configured gate.
Basis: Q6 ruling (CONCLUSIONS.md lines 99-100) task order, Testcontainers where translation matters, and gates at their stages with no threshold lowered.

Recorded at specs/DEV-424/CONCLUSIONS.md (Phase 2 step 6 challenge section).


## Sentry F7 final decision amendment

Authority: CONCLUSIONS.md commit 10ffc9c1; recon-DEV-424-2. This supersedes pending inherited-fail-scope wording only.

[from Patron] DEV-424 Sentry F7 ruling: ACCEPT - a present belongs_to_collection object with a nonpositive id or blank name rejects the whole TMDb details response as Failed InvalidResponse through the existing failure seam; missing or null belongs_to_collection remains a valid absent collection.

- The malformed case must not be returned as Found with the collection silently dropped, and must not persist an invalid or invented collection row. A final failed or unavailable enrichment preserves prior metadata and prior membership.
- A successful non-TMDb fallback is not this failure case: under Q5 it remains a successful whole-metadata replacement without a collection and still clears prior membership.
- This is a deliberate new-field mapping rule for belongs_to_collection within the DEV-424 ticket. It does not claim that all existing optional fields fail wholesale; the general omission or unusable-value mapping reported in recon stays undisturbed for other fields.
- Required tests: malformed present collection proves Failed InvalidResponse and not Found; missing/null proves valid absent collection; failed enrichment proves prior membership preserved; the already-ruled fallback-clear test remains.

Basis: recon-DEV-424-2 lines 11-20 as reported by Keel (malformed JSON returns Failed InvalidResponse via MalformedFailure, optional omission/unusable values normally map null, and the title gate fails the whole details response, so the inherited fail scope is not singular for a new field); Q5 ruling at specs/DEV-424/CONCLUSIONS.md lines 81-85 (malformed collection object follows existing TMDb malformed-response handling and is never persisted; successful metadata without a collection clears membership including successful non-TMDb fallback); step 6 ruling 2 (fallback clear reconfirmed, failure preserves).
Recorded at specs/DEV-424/CONCLUSIONS.md, Sentry F7 section. Patron edited CONCLUSIONS.md only.


## Phase 2 step 6 final freeze receipt — 2026-10-28

Keel verified both correction rounds against all three original axis reports, Patron step 6 rulings (7a335ac3) and final F7 ruling (10ffc9c1), recon-DEV-424-2, and Palette design corrections. All 17 findings plus the informational status item have explicit dispositions in PLAN_CHALLENGE.md. No retained Critical, High, or Medium blocker; no open decision or missing axis. Accepted fallback-clear residual and retained orphan rows remain deliberate Q2/Q5 behavior; no scope expansion or owner checkbox.

Verdict: PLAN FROZEN for Gate 1 review. The hard two-round cap was honored. Gate 1 remains closed; no implementation, test/gate rerun, spec PR approval, or merge is authorized by this freeze. Phase B must recheck dependency landing and main/contract drift. PLAN_CHALLENGE.md records the frozen artifact SHA256 values and working-tree identity; this freeze includes saved uncommitted/untracked artifacts, not only HEAD.
