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

## C1 — speckit-analyze CRITICAL: collectionId invalid-input status (400 vs 422)

**Keel — question:** speckit-analyze found C1 CRITICAL: brief Q3/frozen scope 6 requires malformed, nonpositive, repeated collectionId input return 400; constitution Principle V mandates FluentValidation semantic validation failures return 422. spec.md:83,125 and tasks.md:59 copy 400. Give a deliberate cited ruling: reconcile within existing constitution if supported, or judge necessary constitution departure and carry blocked: structural owner checkbox under section 2.3(b). Do not silently override ticket/brief or declare no departure. Quill is repairing TDD ordering and wording but must retain C1 needs decision until ruled.

**Patron — ruling:**

[from Patron] DEV-424 C1 ruling: RESOLVED WITHIN CONSTITUTION, NO DEPARTURE — malformed, nonpositive, and repeated single-valued collectionId all return 422, not 400; the 400 in brief Q3/frozen scope 6, spec.md and tasks.md is corrected, and no blocked: structural owner checkbox is carried.

- Constitution basis (.specify/memory/constitution.md Principle V, lines 273-290): all commands, queries and request contracts MUST be validated through FluentValidation in the validation decorator and validation failures MUST return 422; ValidationException maps to 422 in the sole IExceptionHandler, and endpoints MUST NOT return BadRequest(ex.Message).
- Existing-convention basis (src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs MappingState.Fail/ThrowIfInvalid throwing LamuFlix.Core.Pipeline.ValidationException; src/LamuFlix.Api/ExceptionHandling/GlobalExceptionHandler.cs): the existing browse raw-query parse/validation path already throws ValidationException, which maps to 422, and the browse endpoint declares only ProducesProblem 422; 400 there is reserved for BadHttpRequestException, which this manually parsed query key never raises. So existing browse validation conventions yield 422, and Q3's 400 was an erroneous status in Patron's own Q3 ruling, not a ticket requirement.
- Authority basis (brief.md Q3 record; Q3 ruling above): the ticket names CollectionRecord, MovieRecord.CollectionId and MovieQuery collectionId but spells no status code; Q3 fixed validation status under §2.3 item 4 (public API shape), which is Patron's to correct. Correcting 400 to 422 changes no ticket delivery (same parameter, AND filter, empty page for a valid unknown id), so §2.3(a) is not triggered, and because 422 follows Principle V, no constitution departure exists for §2.3(b). Retaining 400 would require a BadRequest-style path Principle V forbids; that departure is not judged necessary and is rejected.

**Required artifact changes (Quill; C1 needs decision cleared):** brief.md frozen scope 6 (line 22) and Q3 ruling paragraph (line 120) read 422, annotated superseded in that part only by this ruling, without rewriting Keel's original Q3 question; spec.md every 400 for this input (lines 18, 76, 83, 125, 144 and any other occurrence) becomes 422, keeping line 32 no constitution departure citing this ruling; tasks.md T017 (line 59) and dependent test wording assert 422 ProblemDetails; analyze report marks C1 resolved by this ruling; no constitution amendment, no owner checkbox in the spec PR, and this ruling alone does not mark gate1 provisional.

---

## Phase 2 step 6 — Plan challenge rulings (Sentry / Ledger / Compass)

**Keel — questions:** Sentry F1 plus Ledger F7: choose concrete atomic natural-key upsert mechanism within Q5; recommendation TMDb int PK ValueGeneratedNever, EF existing save retry boundary handles collection unique-conflict by rollback/reset/re-read and retry whole atomic save; concurrent two-movie same-new-id Testcontainers oracle must force race and assert one row plus both memberships. Existing retry applicability is not recon-confirmed; do not claim it already works. Sentry F2: reconfirm Q5 successful non-TMDb fallback clears prior membership, record residual and explicit fallback-clear test, no new release-note scope. Ledger F1/F5: choose existing Genre fieldset/legend and label.lamu-chip with sr-only checkbox controlled single-select toggle (not radio, because reselect clears); no new loading skeleton/aria-busy; heading-only empty section, no helper copy; unknown-id fallback only toolbar, also preserved during facet loading/error/empty. Sentry F3 plus Ledger F2: string? request CollectionId, key const, ReadNumber plus repeated-key detection; malformed/repeated/nonpositive/empty API value 422 via existing ValidationException; positive nullable rule in MovieQueryValidator plus codecs reject invalid values. Ledger F6 asserts long count but recon line 52 and Compass line 21 say genre count int: recommend CollectionFacet and CollectionDto (int Id,string Name,int MovieCount), no new empty query validator and preserve existing AddHandler chain. Ledger F3: backend tests alongside and all green before handoff, web red-first unchanged. No ticket contradiction or constitution departure proposed.

**Patron — rulings:**

[from Patron] DEV-424 step 6 ruling 1 (Sentry F1 + Ledger F7): ACCEPT — CollectionRecord uses the TMDb int id as primary key with ValueGeneratedNever and no surrogate key; inside the existing EfMovieRepository save/transaction boundary, a collection unique/PK conflict from a concurrent insert is handled by rolling back/resetting the failed unit, re-reading the now-existing collection row, and retrying the whole atomic save (collection upsert plus movie membership), never a partial catch-and-continue in the same change tracker; this retry is a required mechanism to implement and prove, not a recon-confirmed existing behaviour, and the Testcontainers oracle must force the race (two movies enriched concurrently into the same new collection id) and assert exactly one collection row, both movies members, and no conflict surfaced.

- Basis: Q5 ruling (lines 81-85 above) already requires atomic upsert by TMDb id inside the existing repository save boundary including two movies sharing a collection; Keel's ask records that existing retry applicability is not recon-confirmed, so the mechanism is mandated without claiming it already works.

[from Patron] DEV-424 step 6 ruling 2 (Sentry F2): RECONFIRMED — a successful non-TMDb fallback is a successful whole-metadata replacement without a collection, so it clears prior MovieRecord.CollectionId, while failed or unavailable enrichment still preserves membership; the accepted residual (membership can stay cleared until a later successful TMDb enrichment restores it) is recorded, an explicit fallback-clear test is required (prior member, successful fallback without collection, membership null, plus failure-preserves coverage), and no new release-note scope is added.

- Basis: Q5 ruling and whole-metadata replacement basis (lines 81-85 above); Sentry F2 as put by Keel.

[from Patron] DEV-424 step 6 ruling 3 (Ledger F1/F5): ACCEPT — the Collection section uses the existing Genre fieldset/legend structure and label.lamu-chip with an sr-only checkbox input, controlled: checking a chip selects or replaces the single collection, unchecking the active chip clears it, and radio inputs are rejected because reselect-to-clear is required; no new loading skeleton and no aria-busy are added, existing facet loading/error patterns are reused; an empty facet renders the section heading only with no chips and no helper copy; the unknown-id fallback chip (Collection <id>) appears only in the active-filter toolbar, never as a Collection section option, and remains present in the toolbar during facet loading, error, and empty states so the applied selection is never silently dropped or broadened.

- Basis: Q4 ruling (lines 65-69 above) single-select toggle, unknown id stays applied with fallback label, existing facet fetch/loading/error patterns, and Palette owning visual layout in design.md.

[from Patron] DEV-424 step 6 ruling 4 (Sentry F3 + Ledger F2): ACCEPT — BrowseMoviesRequest.CollectionId is string? (raw) with a CollectionIdKey const for collectionId in LibraryEndpoints, read through the existing single-value ReadNumber path plus repeated-key detection on the raw query; malformed, repeated, nonpositive, and empty API values return 422 via the existing ValidationException mapping, absent remains unconstrained, and a positive unknown id still returns an empty page; MovieQueryValidator carries the positive nullable rule (null or greater than zero) for MovieQuery.CollectionId, and the MovieQueryString codecs reject nonpositive or otherwise invalid values rather than encoding them as valid state, so direct query construction and URL round-trip cannot bypass validation.

- Basis: C1 ruling (lines 150-154 above), constitution Principle V, and Q3 nullable positive collectionId as ruled there.

[from Patron] DEV-424 step 6 ruling 5 (Ledger F6): ACCEPT — CollectionFacet and CollectionDto are (int Id, string Name, int MovieCount) and Ledger's long count assertion is rejected; no validator is created for the parameterless GetCollectionsQuery, and the existing AddHandler registration chain is preserved unchanged.

- Basis: recon line 52 and Compass line 21 as reported by Keel (genre facet count is int), Q3 genre-mirroring (Q3 ruling above), and Principle V, which a parameterless query gives no input to reject, so no empty validator is invented.

[from Patron] DEV-424 step 6 ruling 6 (Ledger F3): ACCEPT — backend tests are written alongside each backend task (Core, Infrastructure, API in the same phase) and the backend suite is all green before the contract-generation and web handoff checkpoint; web work remains red-first TDD unchanged, and this sequencing waives no configured gate.

- Basis: Q6 ruling (lines 99-100 above) task order, Testcontainers where translation matters, and gates at their stages with no threshold lowered.

**Scope note:** Patron edited CONCLUSIONS.md only for this round; Quill applies the rulings above to plan/tasks/spec wording. No ticket contradiction or constitution departure is proposed or found.
