# DEV-322 - Frozen grill brief

## Authority, receipt and closing bar

- Task: DEV-322, size M, ui:, Phase A. Ticket scope and acceptance criteria: note DEV-322:8; facts: recon-DEV-322:11-63 and supplemental trace :65-106.
- Worktree: F:/Dev/LamuFlix.worktrees/feature-322-spec; branch: feature/322-spec; decision HEAD verified as e7dcecb. Preserve the existing uncommitted .specify/feature.json change. This spec worktree is not authorization to build Phase B here.
- Decisions: specs/DEV-322/CONCLUSIONS.md Q1-Q11 at e7dcecb, specs/PRODUCT.md, and lamuflix-team-charter. Constitution statements below are Patron rulings quoted through CONCLUSIONS.md, not an independent constitution inspection.
- Grill closing bar: every ticket control has an implementation approach; both acceptance criteria have observable tests; genre counts have a bounded existing-data path; URL encoding agrees with the existing API; dependency and file-scope care items have cited Patron rulings; contract generation and downstream sequencing are explicit. These conditions are met by the decisions below.
- Original grill closed with 11 answers and zero blocked structural items. The resumed challenge records Patron transport reconciliation as Q12 below: 12 answers recorded, cap exhausted. Q11 supersedes the original conflicting range clauses of Q6/Q7; Q12 governs transport defaults and equality. No ticket change or constitution departure is authorized.
- Round cap: 12 grill questions total; 12 recorded, none remaining. F5 is a clarification of Q7, not an additional numbered grill answer. This does not establish a separate draft/review-round budget or silently settle any future unresolved question.
- Closure is a Phase A decision receipt, not plan freeze, Gate 1 approval, completed implementation, verification, PR clearance or merge permission.

## Frozen scope

Deliver the ticket-named rich Filter Panel: debounced free-text search; genre multi-select with global movie-count badges; cast multi-select; runtime range with include-unknown checkbox; release-year range; eight sort choices (Title/Year/IMDb/Runtime, each Asc/Desc); and enrichment-status toggle chips. A filter change updates the URL and refetches movies. Controls remain stable during loading/refetch, with prior results retained.

The panel lives under web/src/features/library/ and consumes generated API types/client from DEV-320. Mount it in App.tsx with only totalCount and a title list unless the Phase B base already supplies a library page, in which case integrate there after bounded pickup recon. No card/grid feature is added (Q5; recon :17-23,31).

The only public contract extension is required integer movieCount on GenreDto for GET /api/genres. Counts are total movies carrying each genre, independent of active filters, calculated over existing relationships. No new endpoint, status-code change, table, migration, NuGet dependency, state library, router or architectural layer. Playback execution, Features:LocalPlay, secrets and Process.Start have no panel scope (Q1-Q5; recon :51-57,85-91).

Faceted/filter-narrowed counts and server-provided slider extents remain `noted, no ticket` per CONCLUSIONS.md. inWatchlist, page and pageSize are not panel controls. Preserve foreign URL keys; reset page on filter change (Q6).

## Every grill answer

### Q1 - Genre count badges

Extend GenreDto with required movieCount (int) on GET /api/genres. Count all movies carrying each genre, globally, using the existing genre/movie relationship. Carry it through GenreFacet and the endpoint mapping. Update the genre endpoint/catalog tests and affected frozen contract shape. Regenerate C# DTO -> committed OpenAPI -> generated TypeScript through DEV-320 tooling. Basis: DEV-322:8; recon :27,32,71,77-91,94-101; Patron Q1.

### Q2 - IMDb sort wording

Display IMDb while sending sort=Rating. Keep existing contract members Title, Year, Rating and Runtime; offer Asc and Desc for each, expressed as sort plus direction. No enum rename. Basis: DEV-322:8; recon :26,29; Patron Q2.

### Q3 - Dependencies and native controls

Use two native input type=range controls for each range, native checkbox lists for multi-select, a client-side text filter for cast choices, a small local debounce hook, and URLSearchParams with history.replaceState/popstate. No slider, multi-select, debounce, router or URL-state package. TanStack Query is the sole permitted dependency addition if absent on the Phase B base, pinned as the constitution-chosen dependency per Patron Q3. Do not reimplement DEV-320 client/codegen or introduce a second client stack. Basis: recon :22,52-53; Patron Q3; PRODUCT architecture/non-goals.

### Q4 - Generated client dependency

Spec work may proceed now. Phase B starts only from a base containing DEV-320 generated types.ts and its client/codegen facilities. If DEV-320 is unmerged at pickup, order DEV-322 after it. Consume generated types; no hand-written DTO duplicates. Q1 regeneration uses DEV-320 tooling. Basis: recon :47-49,61; Patron Q4; charter contract chain.

### Q5 - Panel host

Use web/src/features/library/ with a minimal App.tsx host showing totalCount and movie titles. App.tsx and App.test.tsx changes are forced by the URL/refetch acceptance criterion. If a library page exists at Phase B pickup, mount there instead and leave App.tsx alone; resolve its exact path through recon before revising file scope. No new top-level folder/layer, grid or card design. Basis: DEV-322:8; recon :17-23,31,57; Patron Q5.

### Q6 - URL ownership and serialization

Panel query keys are text, genreIds, actorIds, runtimeMin, runtimeMax, runtimeIncludeUnknown, yearMin, yearMax, statuses, sort and direction. Arrays use repeated keys, preserving selection order. Omit defaults subject to Q11 group completeness. Preserve foreign keys, including inWatchlist/pageSize; reset page on filter changes. URL is the committed source of truth: reload and popstate hydrate controls; text commits after debounce and discrete controls commit through replaceState. Temporary typed text belongs only to the debounce interaction, not a second committed filter state. Basis: recon :26,38; Patron Q6 as reconciled by Q11.

### Q7 - Slider extents

Year slider: 1900 through current year, step 1. Runtime slider: 0 through 240 minutes, step 5; upper stop is 240+ and means open upper bound. These are Patron taste assumptions, not a new range-facet endpoint. Encode extent stops per Q11, never by dropping an individual group member. Keep Patron taste logging in ASSUMPTIONS.md; do not invent additional wording from an unread file. Basis: Patron Q7/Q11.

### Q8 - Status chips

Four toggle chips represent Pending, Enriched, NotFound and Failed. No selected chips means no statuses parameter and therefore all statuses. Send generated contract values. Patron records display-label assumptions; this brief does not invent alternate labels. Basis: recon :30; Patron Q8.

### Q9 - Fluid interaction without layout shift

Keep control dimensions fixed while loading/refetching, including fixed-width badges. Controls stay mounted and results use TanStack Query placeholderData: keepPreviousData to retain prior data during a pending refetch. Text debounce is 300 ms. A committed normalized query change causes exactly one movie fetch. No CLS tooling/dependency is introduced. Basis: DEV-322:8; Patron Q9.

### Q10 - Test scope

Vitest + RTL + MSW: slider, multi-select and fake-timer debounced-search suites, plus URL/refetch and URL hydration acceptance tests. Backend behavior changes/tests are bounded to genre movieCount and its contract consequences. Include genre endpoint/catalog integration and handler unit tests; mutation applies to changed src files. Basis: DEV-322:8; recon :35-44,94-103; Patron Q10.

### Q11 - Range reconciliation, authoritative over original Q6/Q7

Runtime is an all-or-nothing group: runtimeMin, runtimeMax and runtimeIncludeUnknown. Year is an all-or-nothing group: yearMin and yearMax. An extent stop sends an empty string, interpreted by the API as null/open bound. Never omit an individual member of an active range group.

- Both runtime ends at extents and include-unknown unchecked: omit the whole runtime group.
- Either runtime end moved or include-unknown checked: send all three runtime keys, with explicit true/false for runtimeIncludeUnknown; extent ends are empty values.
- Example: runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false means 60 minutes and up, excluding unknown runtimes.
- Both year ends at extents: omit the year group. Either moved: send both keys, with empty values at extent stops.
- Empty year ends avoid silently excluding pre-1900 or future-year titles. Runtime 240+ is runtimeMax= when its group is active, not an omitted runtimeMax.

The existing API already accepts empty/open bounds and rejects partial groups; no backend range-contract change. Basis: Patron Q11, including its cited MovieQueryString, EmptyMeansNull and MovieRecordQueryExtensions evidence; recon :38.

## Plan decisions: approach and files touched

Keep the existing query/port/persistence flow. Add MovieCount to GenreFacet, project the count of the existing genre Movies navigation in EfMovieCatalog.GetGenresAsync, retaining name-then-id ordering and AsNoTracking; map that value to GenreDto. Use database-translated counting over existing relationships, not driver mocks for translation behavior. The handler remains a passthrough; neither its return-list type nor DI wiring needs a new abstraction (recon :71-90).

Frontend work forms one feature slice: URL decode/encode and normalized query key; debounce hook; native controls; generated-client queries for movies/genres/actors; and the minimal host. Fetch actor options from GET /api/people?role=actor. Use generated BrowseMoviesResponse items/totalCount and item id/title. Do not hand-edit generated files.

Frozen existing production modifications:

| Path | Reason |
| --- | --- |
| src/LamuFlix.Core/Ports/IMovieCatalog.cs | Extend GenreFacet with count; retain GetGenresAsync signature |
| src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs | Query global count through existing Movies relationship |
| src/LamuFlix.Api/Endpoints/GenreDto.cs | Required wire movieCount |
| src/LamuFlix.Api/Endpoints/FacetEndpoints.cs | Map count into wire DTO |
| web/src/App.tsx | AC-forced minimal panel/results host, conditional per Q5 |
| web/package.json, web/package-lock.json | Only if constitution-chosen TanStack Query is absent at Phase B pickup; pin it per Q3 |

Frozen existing test/contract modifications:

- tests/LamuFlix.IntegrationTests/FacetEndpointsTests.cs: exact id/name/movieCount shape and count values; retain empty-array coverage.
- tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs: real count translation/data behavior and existing sorting/empty cases.
- tests/LamuFlix.UnitTests/Features/Library/GetGenresQueryHandlerTests.cs: count-bearing facet passthrough and empty result.
- tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs: exercise existing drift assertion; source changes only if the added field forces them.
- tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json: regenerate verified contract snapshot through its established workflow.
- web/src/api/openapi.json and web/src/api/types.ts: regenerated artifacts through contract tooling; types.ts is a DEV-320 prerequisite, not authored by DEV-322.
- web/src/App.test.tsx: replace readiness-only expectations with host integration AC coverage, conditional per Q5.
- web/src/test/server.ts: add/compose feature MSW handlers only if required by the inherited DEV-320 test setup.

Planned new handwritten files are confined to web/src/features/library/:

- FilterPanel.tsx and FilterPanel.test.tsx: all panel controls, dimensions and interaction coverage.
- LibraryView.tsx and LibraryView.test.tsx: generated-client queries, count/title results, refetch and retained-data behavior.
- filterQuery.ts and filterQuery.test.ts: URL serialization/hydration, group completeness, repeated keys and foreign-key preservation.
- useDebouncedSearch.ts and useDebouncedSearch.test.ts: 300 ms fake-timer behavior and cancellation.

These new filenames are plan decisions, not claims that recon found existing files. Palette may settle presentation within the feature slice; any resulting decision/file-scope change belongs in this brief before drafting or build. No schema/entity configuration, migration, query parser, sort/status enum, query handler, DI registration or unrelated source rewrite is planned. GenreFacet retains the same type, so empty-list implementations and DI composition expectations need no automatic edit (recon :73-74,81-82,98,102-103). If build evidence identifies additional count-bearing construction sites, request bounded recon and record the forced scope amendment before editing.

## Test strategy

1. Database-backed genre tests: zero movies gives count zero; a movie assigned to multiple genres contributes once to each; multiple movies produce distinct counts per genre; empty facets remain empty and name/id ordering remains stable. Use the existing real-database test infrastructure for translation/serialization behavior. Do not add a mocking/assertion library.
2. Handler/endpoint: passthrough preserves counts; endpoint emits required integer movieCount and the exact additive property set; empty response remains an array. Contract baseline and verified snapshot agree, with generated TypeScript exposing the field.
3. URL/ranges: inactive groups omitted; active runtime includes all three keys; active year includes both; extent ends serialize empty; 240+ plus moved minimum stays valid; include-unknown alone activates runtime group. Repeated arrays keep order, IMDb sends Rating, no chips omits statuses, defaults omitted, foreign keys preserved and page reset.
4. Controls: both handles, steps/extents, genre badges and selections, cast local filtering/selections, all eight sort choices, all status toggles and include-unknown. Exercise the ordinary defaults as well as one-sided ranges.
5. Debounce: before 300 ms no committed text fetch; at 300 ms one matching request; rapid input cancels stale pending commits. Discrete changes produce one matching movie request per committed normalized query change.
6. Host acceptance: URL parameters and MSW-observed movie request agree; reload and popstate restore controls; pending refetch preserves old titles and mounted controls. Verify fixed control/badge dimensions in implementation and visual review, without claiming a measured CLS score from RTL.

New backend behavior tests remain count-scoped. Existing range parser/validator tests provide contract evidence; DEV-322 does not change that backend behavior. Test timing uses fake timers and the existing time conventions, never wall-clock sleeps.

## Gate expectations

No gates run for this brief-writing ask. The Gauge baseline recorded in DEV-322:20-65 is historical measurement only: zero findings on 19 named files for Roslyn, complexity and InspectCode; it does not clear the delivery diff.

At the proper Phase B stages, run web-implement TDD and its web gate for the frontend diff; run applicable backend build/analyzers, complexity, InspectCode, formatting, tests, security/dependency and contract drift checks for the changed C# and package/contract scope. Respect harness.yml thresholds and script-supported flags without changing generated gate configuration. Product requires zero warnings, complexity limits and mutation coverage; do not hardcode substitute gate thresholds in the plan.

Q1 changes production C#, so mutation is applicable to its eligible changed projects. Use scripts/run-mutation.ps1, never Stryker since from a worktree. Mutation must test mutants, report a score and Timeout counts, remain within changed src scope and meet configured thresholds. Zero tested, missing score, since-ignored mutants, out-of-scope mutations, below-threshold score or no killed mutants is failure per charter :66-76. A survivor calls for a test, not a threshold reduction.

Record PASS only for a completed passing gate. Distinguish SKIPPED (scope-empty), configured-disabled SKIP, mutation N/A and Could not run; do not turn those into PASS. Applicable missing/failed gates halt. Property-test applicability follows the actual change and configured gate; no opt-out is inferred from this brief. This document claims no current property/security/web/mutation test result.

## Task ordering and downstream constraints

1. Quill drafts spec.md, plan.md and tasks.md from this brief. Keel does not draft those artifacts in this ask. Every acceptance criterion and Q1-Q11 decision must trace into the draft; Q11 must replace the superseded omission rule.
2. Because ui: is present, await Palette design.md before plan challenge. Design cannot silently alter frozen ticket behavior, API shape, dependency choices or range serialization.
3. Keel analyzes the draft read-only and checks plan/tasks against this brief. Adjudicate plan findings before plan freeze. Record any decision change here before an artifact amendment.
4. Gate 1 remains a separate owner gate; zero structural checkboxes does not grant Gate 1. Phase B cannot start merely because this grill is closed.
5. After Gate 1, Phase B pickup must contain DEV-320 generated client/types/tooling. Recheck drift against main through the authorized process; facts come from bounded recon. Confirm whether a library page and TanStack Query now exist before executing the Q5/Q3 conditional edits.
6. Implement/test the count-producing backend path, then regenerate OpenAPI/snapshot and TypeScript in contract-chain order before wiring the badge UI to movieCount. Never hand-edit a generated contract to make the frontend compile.
7. Establish/test URL normalization and debounce before controls/queries; then integrate native controls, generated-client queries and the minimal host; then close URL/refetch and pending-data acceptance coverage.
8. Run gates only at their pipeline stages. For size M review adjudication, all three axis reports (Sentry, Ledger, Compass) are required; a missing axis blocks adjudication. Persist findings and PR receipt at the proper review stage.
9. Patron decides open structural/taste matters and Rigger alone records authorized YouTrack changes. Owner-only ticket/constitution changes remain blocked structural; never silently reopen scope. Never merge, enable auto-merge or push main.

## Original brief handoff

Only specs/DEV-322/brief.md was written by the original brief-writing ask. Existing .specify/feature.json remained untouched. No production code, other spec artifact, note or tracker mutation; no Spec Kit, gates, commit or PR action. The next authorized pipeline work was Quill drafting from this brief, with Palette design required before plan challenge, not Phase B implementation.

### Analysis reconciliation (Phase 2 step 3)

- Existing Patron taste decisions are now read and incorporated by reference to ASSUMPTIONS.md:5-11: the eight sort labels and default Title (A-Z), status labels, Search titles placeholder, Include unknown runtime label/default unchecked, genre/cast name ordering and Filter cast label, Clear filters preserving sort/direction, and empty-result copy. This records already-issued decisions, not a new grill answer or scope extension; recon-DEV-322:122-143 supplies the receipt.
- Patron corrected the historical partial-range 400 citation to 422 Unprocessable Entity in CONCLUSIONS.md:43-47 at cf0f495. This agrees with recon-DEV-322:147-148 and constitution principle V. Q11 range behavior and the existing API status remain unchanged; the source-receipt issue is closed.
- Tasks must implement the count/contract path, URL/debounce foundations, all panel controls, and then host/refetch acceptance in dependency order; story priority does not make US1 independently implementable before its control producers. Shared FilterPanel test/source edits are serial. Setup dependency pinning follows pickup recon. These corrections enforce the existing ordering in this brief, not new delivery scope.
- Constitution III proof receipt is supplied by recon-DEV-322:169-185: MovieQueryStringTests.Format_RoundTripsValidatorAcceptedQueries (:151-160, equality :158) and LibraryEndpointsTests.BrowseMovies_GeneratedQuery_RoundTripsToAnEqualDispatchedQuery (:81-110, equality :106). These existing tests establish planned coverage; no test or gate was run in this analysis.
- Patron approved the Palette count/error copy in CONCLUSIONS.md:63-66 and ASSUMPTIONS.md:12 at 5532495. Design presentation stays within the frozen files. Per design.md:229, LibraryView owns the three generated-client queries; FilterPanel consumes controlled state/callbacks and facet props, with only transient interaction state locally. Its separator introduces the preserved Sort group; status still resets. Any existing-page route/mount is resolved by pickup recon, not assumed to remain `/`. These clarifications preserve Q3/Q5/Q6 and introduce no dependency, API, or scope change.

The resumed analysis ask authorizes the bounded draft corrections above. It does not authorize production edits, a new Patron ruling, plan challenge, gates, a commit, or Phase B implementation.

## Resumed plan challenge adjudication - authoritative decision update

This section supersedes earlier ambiguous URL/request equality and frontend time wording. The previous analysis receipt is historical; it does not clear the drafts against these new decisions.

- Identity verified: F:/Dev/LamuFlix.worktrees/feature-322-spec, feature/322-spec, HEAD cf0f49566f64a68357dbfcc4ee3dcd2915c88425. Preserve the existing .specify/feature.json modification and untracked drafts.
- All three axis reports read: findings-DEV-322-Sentry:1-19, findings-DEV-322-Ledger:1-37, findings-DEV-322-Compass:1-29. Compass is the recreated receipt described at :3,28.
- Evidence: task note DEV-322:8,120-148; recon-DEV-322:108-234; the named feature artifacts. Older recon quotes at :145-150 describe the earlier 400 receipt, corrected by current CONCLUSIONS.md:43-47. No external ruling files or other roots were read.
- Patron rulings are authorized by the Conductor resume ask and recorded in DEV-322:140-148. CONCLUSIONS.md currently ends at line 70 and does not yet contain Q12 or the F5 clarification. Their source receipt must be recorded by Patron before freeze; this brief records the supplied decisions now.

### Q12 - Q6 transport defaults, canonical equality and query key

Patron ruling: DEV-322:147-148 and the resume ask. Basis: existing Q6/Q9/Q10 (CONCLUSIONS.md:33-37,53-60), default Title (A-Z) (ASSUMPTIONS.md:5), and recon-DEV-322:192-219.

1. The browser URL remains the committed source of truth. Web defaults are sort Title, direction Ascending, page 1 and pageSize 20. Defaults may be omitted from the browser URL; every movie API request must carry all four required keys after normalization. These are web transport defaults, not new backend defaults.
2. An ordinary empty panel URL becomes the request query `sort=Title&direction=Ascending&page=1&pageSize=20` (recon :217-218). The endpoint itself maps absent ordering to null and absent paging to Page(0,0); no defaults are applied, and validation returns 422 (recon :203-214). MovieQueryString.TryParse also rejects missing required keys, but the HTTP endpoint uses its own mapper, not that codec (recon :194-204).
3. Preserve supplied foreign URL keys, including pageSize and inWatchlist. Defaults fill absent required API values; they do not overwrite a supplied pageSize. A filter change removes page from the URL and sends page=1 in the API request. Unknown URL keys remain in the URL; they are not part of the recognized movie API query or its query key. No invalid supplied input is silently made valid by changing backend validation.
4. Equality means the canonical API query string obtained from decoded URL state plus transport defaults, compared with the canonicalized actual generated-client request. It is not literal equality between window.location.search and the request. Use this same canonical API query string as the variable part of the movie query key. Equivalent omitted/explicit defaults yield the same key and no extra fetch; a committed change to the effective API query yields one fetch.
5. Canonicalization is deterministic over recognized API keys and preserves repeated-array value order and Q11 empty bound values. Do not sort array selections, drop an active empty bound, or forward arbitrary URL keys as generated API fields. The production serializer and request observation must agree on this representation.
6. Direction uses generated API enum spellings. Asc/Desc in earlier UI descriptions is shorthand, not authority to send those literal wire tokens; the confirmed ascending wire value is Ascending (recon :217-218). Resolve other generated member spellings from the inherited contract at pickup rather than inventing a contract rename.
7. Q11 runtime/year completeness, explicit runtimeIncludeUnknown true/false, default status omission, replaceState/popstate, foreign-key preservation and generated contract chain remain in force. No endpoint, parser, validator, DTO field or status-code change is authorized by Q12. Existing validation remains 422.

### Q7 clarification - frontend current year (Sentry F5)

Patron ruling: DEV-322:140-141 and the resume ask; existing extents: CONCLUSIONS.md:40 and ASSUMPTIONS.md:8.

- The year maximum is the viewer local current year from the browser clock, captured once at panel mount. No timer or live New Year rollover changes the mounted panel extent. A fresh mount captures the then-current local year.
- Implement the pure `currentYearExtent(now)` seam within the already planned FilterPanel.tsx file; do not add a clock package, backend request, new file or live rollover feature. Backend TimeProvider conventions continue to apply to backend code; they do not prescribe a TypeScript TimeProvider.
- T040 tests supply deterministic time through vi.setSystemTime and cover local-year derivation, an unchanged extent across a year boundary while mounted, and the new extent on remount. Restore test clock state. T042 implements the mount-time snapshot. No sleeps or current wall-clock-dependent expectations.

### Finding-by-finding disposition

| Finding | Disposition | Reason and exact evidence | Documentation owner |
| --- | --- | --- | --- |
| Sentry F1 High (:3-5) | Accepted; decision resolved by Q12, draft repair pending | spec.md:19,86,88-89,108 and tasks.md:44-45,69-72 omit a transport-default distinction. Missing required keys fail codec parsing (recon :195-198) and HTTP validation (recon :203-214). Q12 supplies defaults and canonical request equality without changing the API. | Keel records Q12 here; Patron records source ruling; Quill aligns spec/plan/tasks; Palette aligns design. |
| Sentry F2 Medium (:7-8) | Accepted as a web encoder compatibility coverage gap; bounded remedy decided below | Existing proofs use MovieQueryString.Format, not future filterQuery.ts (recon :171-185); plan.md:45 and tasks.md:44,69 do not establish web request compatibility. Require production web encoder/request observations against independently specified backend-contract cases. Existing constitution III proof remains valid (recon :154,167-185). A new backend test or cross-runtime harness is not authorized: Q10 bounds backend changes/tests to Q1 (CONCLUSIONS.md:58-61). | Quill records coverage in plan and existing T010/T011/T020/T024; implementation stays in frozen web files. |
| Sentry F3 Medium (:10-11) | Accepted for missing executable regeneration/pickup detail; rejected as an ownership contradiction | plan.md:85,122,130-132 and tasks.md:21,36 already distinguish inherited tooling from post-DTO regeneration. Not authored means no handwritten generated artifacts. Recon :223-225 now identifies the opt-in OpenAPI/snapshot workflow; :226-234 proves bounded absence of TypeScript codegen and DEV-320 pickup instructions here. Resolve via an explicit future pickup stop, not an invented present script. | Quill clarifies T001/T036 and plan contract workflow; DEV-320 provides tooling, DEV-322 implementation executes regeneration after T035. |
| Sentry F4 Low (:13-14) | Rejected | plan.md:126 and brief.md:112 discuss same record identity/empty-list/DI, not an unchanged positional constructor. plan.md:66,108-111,117-119 and tasks.md:31-35 already plan count-bearing projection and test changes. The report identifies only frozen-scope sites; no exhaustive construction-site claim is adopted. | No required correction. |
| Sentry F5 Low (:16-17) | Accepted; decision resolved by Patron Q7 clarification, draft repair pending | spec.md:117 does not specify a frontend source; tasks.md:58,60 lacks deterministic mount/year-boundary behavior. DEV-322:140-141 fixes local browser year at mount and currentYearExtent(now)/vi.setSystemTime. | Patron records ruling/taste receipt; Quill aligns spec/plan/tasks; Palette aligns year references. |
| Ledger N1 Low (:7-15) | Accepted | plan.md:7 refers to design.md but :51-58 omits it; :7,35,139 retain template instructions. brief.md:138 and design.md:229 establish its role. | Quill adds design.md to tree and removes the three template clauses while retaining actual constraints. |
| Ledger N2 Low (:17-25) | Accepted | plan.md:5 stops recon citations at :106 while :45 relies on :154-161,169-185. This adjudication also relies on :189-234. | Quill extends Input citation through :234. |
| Compass Low-1 (:7-11) | Accepted | tasks.md:70 T022 implements URL ownership and retained data; design.md:214,229 and brief.md:47,59 support adding it to Q6/Q9 trace rows at tasks.md:116,119. | Quill adds T022 to both rows. |
| Compass Low-2 (:13-17) | Accepted | tasks.md:59 T041 tests include-unknown-alone activation required by brief.md:67-70 and spec.md:59, but tasks.md:121 omits it from Q11. | Quill adds T041 to the Q11 row. |

### F2 compatibility coverage decision

Extend the already planned filterQuery.test.ts and LibraryView.test.ts suites; no new backend file or test harness. T010 compares the actual production web normalizer/encoder with independently written expected API queries tied to recon receipts. T020 observes the actual generated-client request in MSW and compares its canonical form to those expectations, not to an expected value obtained by calling the same encoder under test. MSW must not supply missing defaults or accept omitted required keys as a successful movie request.

Required cases: the empty URL produces all four defaults (recon :217-218); explicit defaults and omitted defaults normalize equally; a text/discrete change resets API page to 1 while preserving supplied pageSize/inWatchlist; repeated arrays preserve selection order; generated sort/direction/status members and lowercase true/false are used; active runtime includes both bounds (including empty extent values) and runtimeIncludeUnknown; active year includes both bounds. Q11 reference: CONCLUSIONS.md:43-46 and spec.md:58-71. T024 also covers canonical-key equivalence and one request per effective query change. Use inherited generated contracts for allowed member values at pickup.

This is an explicit web-to-existing-contract compatibility strategy, not a claim that MSW executes the real validator or that these unimplemented tests have passed. Existing backend witnesses remain unchanged: recon :197,213-214,171-185. A new live backend/browser test would require a separate cited scope decision; it is not silently added to this M-ticket plan.

### Contract regeneration and pickup decision (F3)

- At this HEAD, TypeScript generation and inherited DEV-320 pickup instructions are **not found in these roots** (recon :226-234). Do not widen the search or claim that types.ts/codegen is ready.
- T001 must stop Phase B pickup unless bounded recon on the actual inherited base identifies generated types/client, the exact inherited codegen command and prerequisites, and a repeatable regeneration path after an OpenAPI delta. Presence of types.ts alone is insufficient. If absent, order DEV-322 after DEV-320 and report the missing prerequisite; DEV-322 must not implement substitute tooling or handwritten DTOs.
- After T035, the DEV-322 implementer executing T036 sets `LAMUFLIX_REGENERATE_OPENAPI=true` only for the established opt-in test `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether`. It writes openapi.json and the Verify snapshot together (recon :223-224). Clear the regeneration opt-in before exercising `DriftMatchesCommittedBaseline` so enforcement is not replaced by regeneration (recon :225).
- Then execute the recon-verified inherited DEV-320 TypeScript generation workflow against the updated OpenAPI. Generated types must expose required integer movieCount before badge UI is wired. No exact TypeScript command exists in the supplied evidence; T001 obtains it at pickup. This future prerequisite is already authorized by Q4 (CONCLUSIONS.md:22-24), not a present analysis fact gap or permission to start Phase B.

### Authorized documentation repair list and owners

1. **Patron:** record Q12 and Q7/F5 clarification with cited basis in CONCLUSIONS.md; update ASSUMPTIONS.md taste receipt for local current year at mount/no live rollover. Preserve existing UI labels and 422/Q11. No commit is authorized by this ask. Source receipts are DEV-322:140-148 and the resume instruction, not unread temp files. Q12 is the twelfth answer; update the ruling count, not the cap.
2. **Quill, spec.md:** align Input/decision references through Q12 and recon :234; amend US1 independent test (:19), FR-006/008/009 (:86,88-89), FilterQuery description (:100), SC-001 (:108), and time/codegen assumptions (:117-118) to distinguish compact URL from complete canonical API query, record Title/Ascending/1/20 defaults, fixed-at-mount local year and the explicit codegen pickup prerequisite. Keep ticket controls and Q11 intact.
3. **Quill, plan.md:** extend Input (:5) through recon :234/Q12; remove template clauses (:7,35,139) while retaining real constraints; add design.md to :51-58 tree. Align testing/performance/constraints (:21,27,29,45), filterQuery structure (:79-80), and workflow (:121-122,130-134) with canonical equality/query key, independently expected compatibility cases, frontend time seam, verified opt-in OpenAPI workflow and codegen pickup stop. Preserve backend TimeProvider, existing property proof and all frozen source paths.
4. **Quill, tasks.md:** revise Input and T001/T036 (:21,36) for explicit regeneration prerequisites/ownership/workflow; T010/T011 (:44-45) for separate URL and canonical API serialization; T020/T022/T024 (:69-72) for complete transport defaults, independently expected MSW observations and canonical query key; T040/T042 (:58,60) for deterministic mount-time year tests/source. Add T022 to Q6/Q9 (:116,119), T041 to Q11 (:121), and a Q12 row mapping T010/T011/T020/T022/T024. Retain all 29 IDs, existing producer dependencies, serial shared-file ownership and backend count-only test scope.
5. **Palette, design.md:** align Input (:9), URL/transport distinction (:87-99), movie data request (:262), and sort wire mapping (:277,281) with Q12/generated enum spelling; align year references with local mount-time capture. Preserve all approved copy, visual structure, API scope and Q11. Conductor dispatches this bounded synchronization; no design edit is performed in this ask.

Quill doc-only work on spec.md, plan.md and tasks.md is authorized from this brief without another decision round. Patron and Palette own their named receipts/synchronization; Quill does not invent missing facts or modify their artifacts by implication. No production edits, tests/gates, commits, new dependencies/files, tracker writes or PR actions are authorized here.

### Clearance and freeze state

- Adjudication complete: all nine findings have dispositions; F1/F5 decisions and F2/F3 remedies are recorded. No outstanding present recon request or owner structural checkbox is established. The missing inherited codegen is a mandatory future Phase B pickup prerequisite.
- **Analysis is not clear for the current drafts; plan is not frozen.** Accepted documentation repairs and Patron/Palette receipt synchronization remain unperformed. The earlier CLEAR receipt predates Q12 and cannot clear these artifacts now.
- After authorized repairs, Keel must read back the amended artifacts and compare them with this brief before reporting analysis clearance or plan freeze. No automatic closure from a writer self-report. Gate 1 remains separate and closed until owner approval; no Phase B or test PASS claim.
- This resumed ask edits only brief.md. No tests, gates, Spec Kit scripts, production writes, commits or merges were run.

## Post-repair independent read-back - Keel

- Identity: F:/Dev/LamuFlix.worktrees/feature-322-spec, feature/322-spec, HEAD cf0f49566f64a68357dbfcc4ee3dcd2915c88425. Read DEV-322:1-161 and all seven named artifacts. Existing modifications/untracked drafts preserved. This receipt supersedes the pending-repair state at :229-234 only as detailed below; earlier line references in historical adjudication receipts refer to their pre-repair geometry.
- Behavioral repair clearance: Sentry F1 is repaired by spec.md:19,86,88-89,100,108; plan.md:21,27,80-81,134; tasks.md:44-45,69-72; design.md:89,99-100,265,280,284. Q12 defaults Title/Ascending/1/20, compact URL versus complete request, canonical equality/key, unknown-key exclusion, supplied pageSize/inWatchlist preservation and page reset agree with CONCLUSIONS.md:54-58 and brief.md:170-180. No backend validation/status change is planned.
- Sentry F2 is repaired at plan.md:21 and tasks.md:44-45,69,72: production encoder plus observed generated-client requests use independently specified expectations; plan explicitly forbids MSW supplying defaults or accepting missing required keys as success. Required contract cases are present in plan.md:21. Existing property-proof witnesses remain at plan.md:45; no new backend test or cross-runtime harness is added, and no execution PASS is inferred.
- Sentry F3 is repaired at spec.md:118, plan.md:122-123,131-133 and tasks.md:21,36. T001 requires inherited generated types/client, exact codegen command/prerequisites and repeatable post-delta regeneration, with a stop if missing. T036 follows T035, runs opt-in OpenAPI/snapshot regeneration together, clears the opt-in before drift enforcement, then uses the inherited TypeScript workflow. Exact inherited TypeScript command is not found in these roots; it remains a mandatory future pickup prerequisite, not permission to substitute tooling.
- Sentry F4 remains rejected per :197. Sentry F5 is repaired at spec.md:117, plan.md:29,134, tasks.md:58,60 and design.md:97,174,245,308, matching CONCLUSIONS.md:42-45 and ASSUMPTIONS.md:8: local browser year captured once at mount, no rollover, pure currentYearExtent(now), deterministic vi.setSystemTime tests including mounted boundary/remount and clock restoration.
- Ledger N2 is repaired at plan.md:5 (recon through :234). Compass Low-1 and Low-2 are repaired at tasks.md:116,119,121; Q12 trace row is present at :122. Ledger N1 is partly repaired: design.md is in plan.md:58 and prior scaffolding is removed/reworded, but the remaining stage-template sentence is listed below.
- Task identity/dependencies retained: 29 unique unchecked IDs: T001,T002,T003,T010,T011,T012,T013,T020,T021,T022,T023,T024,T030,T031,T032,T033,T034,T035,T036,T037,T040,T041,T042,T043,T050,T051,T052,T053,T054. Explicit producer dependencies remain at tasks.md:23,34-36,45,47,55-61,69-72,90-98. Count/contract precedes badge UI; foundations precede controls; shared FilterPanel files remain serial T021 -> T033 -> T037 -> T040 -> T041 -> T042; host tests precede host implementation; pickup precedes conditional package edits. No prior tasks.md byte snapshot is supplied in these roots; retention is checked against the 29-ID and producer-order receipts in DEV-322:115-117,122,134,152 and this brief, not a claimed historical byte diff.
- Q1-Q11 substance, frozen file scope and approved copy are preserved. Runtime remains all three keys or none, include-unknown alone activates it, explicit true/false and empty extent bounds remain; year remains both keys or none. Global genre counts, generated contract chain, native controls, minimal host, conditional library-page/TanStack pickup, count-only backend behavior tests and stage-specific gate constraints remain intact.

### Remaining documentation findings

1. **R1 Low - active source receipts have stale line geometry.** spec.md:71 and plan.md:45 cite CONCLUSIONS.md:43-47 for the corrected Q11/422 receipt; those lines now contain F5 plus the Q11 heading, while the actual Q11 rule/evidence/correction is :47-52. design.md:11,343 cite CONCLUSIONS.md:63-66 for count/error taste; those lines now contain Q9, while taste approval is :74-77 (ASSUMPTIONS.md:12 is still correct). tasks.md:57,70 cite design.md:229 for query ownership; :229 is now the debounce tree entry, and the ownership/wiring receipt is :216-218,232. Correct only these active pointers in their owned artifacts; no behavior change. Current authoritative pointers also supersede historical brief pointers: Q11 :47-52, Q10 :68-72, Q9 :63-66, taste :74-77, design wiring :216-218,232.
2. **R2 Low - accepted Ledger N1 template cleanup is incomplete.** plan.md:35 still says constitution checks gate Phase 0 research and are rechecked after Phase 1 design, while plan.md:61 explicitly omits Phase 0/1 artifacts. Replace that sentence with the actual constitution/pickup and separate Gate 1 constraint, or remove it while preserving plan.md:37-47. This is documentation stage wording, not a new gate or scope decision.

### Exact clearance and plan-freeze state

- Critical 0, High 0, Medium 0, Low 2 remaining documentation findings (R1/R2). F1/F2/F3/F5, Ledger N2 and both Compass items are repaired; F4 stays rejected; Ledger N1 is partial. Patron Q12/F5 source receipts and year taste are synchronized; Palette behavioral synchronization is complete. No new decision or present recon request is needed.
- **Exact post-repair analysis clearance is not granted; plan freeze is not clear yet.** The remaining findings are non-blocking in behavioral severity, but the accepted repair/read-back bar has not been fully met. Conductor may dispatch the bounded pointer/stage-wording corrections to Quill and Palette, then request Keel read-back. No change to the 12-question cap, 29 tasks, decisions or file scope is needed.
- Gate 1 remains separate and closed until owner approval. No Phase B, PR/review clearance, test PASS or merge permission is claimed. Only brief.md was edited in this ask; no other artifact edits, production writes, gates/tests/Spec Kit scripts, commits, tracker/note writes or merges. No search was widened outside the named task note and feature artifacts.

## Final post-R1/R2 independent read-back - Keel (2026-10-12)

- Identity verified before editing: F:/Dev/LamuFlix.worktrees/feature-322-spec, branch feature/322-spec, HEAD cf0f49566f64a68357dbfcc4ee3dcd2915c88425. Read task note DEV-322:1-170 and brief.md, spec.md, plan.md, tasks.md, design.md, CONCLUSIONS.md and ASSUMPTIONS.md. Existing modified files and untracked drafts are preserved. This final receipt supersedes the pending R1/R2 clearance and freeze state at :247-256; historical adjudication pointers retain their original geometry.
- **R1 closed by exact source read-back:** spec.md:71 and plan.md:45 now cite CONCLUSIONS.md:47-52, which contains the Q11 heading, all-or-nothing rule, 422 evidence, empty-bound semantics and historical status correction. design.md:11,343 now cite CONCLUSIONS.md:74-77 for Patron count/error taste approval, matching ASSUMPTIONS.md:12. tasks.md:57,70 now cite design.md:216-218,232, which identifies LibraryView query/URL ownership and controlled FilterPanel facet wiring. None of these active citations points to the superseded F5, Q9 or debounce-tree locations.
- **R2 / Ledger N1 closed:** plan.md:35 now applies constitution checks to analysis and conditional Phase B pickup and keeps Gate 1 as a separate owner gate. The Phase 0 research / Phase 1 design gate-template instruction is removed. plan.md:37-47 preserves the actual constraints and property-proof receipt; :58 includes design.md. The remaining Phase 0/1 mention at :61 explicitly excludes template artifacts and adds no stage or gate. Earlier template clauses at :7,140 no longer instruct template generation.
- **29 unique task IDs retained:** T001,T002,T003,T010,T011,T012,T013,T020,T021,T022,T023,T024,T030,T031,T032,T033,T034,T035,T036,T037,T040,T041,T042,T043,T050,T051,T052,T053,T054. All remain unchecked. Checked against :244 and DEV-322:115-117,122,134,152; no prior byte snapshot is supplied in these roots, so no historical byte-diff claim is made.
- **Dependencies retained:** tasks.md:23 requires T001/T002 before conditional T003; :34-36 orders T030/T031 -> T034, T034/T032 -> T035 -> T036; :45,47 orders foundation tests before T011/T013. Shared-file control work remains T021 -> T033 -> T037 -> T040 -> T041 -> T042 (:53-60,94), with T036 also required before badges at T037. T043 follows T042 (:61,95). Complete controls/MSW precede T020, host tests precede T022, T023 follows T022/T021 and T024 follows T022/T023 (:69-72,96). Phase order and stage-specific checks remain :90-98,80-86; no reduced US1-only delivery is introduced.
- **Q12 synchronized:** CONCLUSIONS.md:54-58 and brief.md:170-180 agree with spec.md:19,86,88-89,100,108; plan.md:21,27,80-81,134; tasks.md:44-45,69-72,122; design.md:89,99-100,265,280,284. Compact browser URL and complete canonical API requests remain distinct; absent values receive Title/Ascending/1/20, supplied pageSize/inWatchlist survive, filter changes reset API page to 1, unknown URL keys stay out of the API query/key, repeated-array order and empty bounds survive, and equivalent defaults share the canonical query key. Generated enum spellings and existing 422 validation are preserved.
- **F5 synchronized:** CONCLUSIONS.md:42-45, ASSUMPTIONS.md:8 and brief.md:182-188 agree with spec.md:117; plan.md:29,134; tasks.md:58,60; design.md:97,174,245,308. Viewer local browser year is captured once at mount with no live rollover; currentYearExtent(now) stays in FilterPanel.tsx; deterministic vi.setSystemTime coverage includes local-year derivation, mounted year boundary, remount and clock restoration. No clock package, new file or backend time behavior is introduced.
- **Prior adjudication remains satisfied:** F1/F2/F3/F5 and Ledger N1/N2 plus both Compass trace fixes are closed; F4 remains rejected per :197. F2 independent production-encoder and generated-request expectations, including MSW not supplying defaults, remain in plan.md:21 and tasks.md:44-45,69,72. F3 pickup stop and regeneration ownership remain in spec.md:118, plan.md:122-123,131-133 and tasks.md:21,36. Q6/Q9 trace rows include T022 (:116,119), Q11 includes T041 (:121), and Q12 maps T010/T011/T020/T022/T024 (:122).
- **Q1-Q11 behavior, approved copy and scope preserved:** all ticket controls, global required integer movieCount, database-backed count tests, C# DTO -> OpenAPI/snapshot -> inherited generated TypeScript chain, native controls, actor role query, controlled panel/minimal count-title host, conditional library-page/TanStack pickup, 300 ms debounce, retained results and mounted controls remain. Runtime is all three keys or none, include-unknown alone activates it with explicit true/false, year is both keys or none, and extent values remain empty/open bounds (spec.md:58-71,81-96; tasks.md:31-36,44,55-72; design.md:96-105,232,265-286). Frozen file scope matches :83-112 and plan.md:105-127; approved copy matches ASSUMPTIONS.md:5-12 and design.md:328-349. Backend behavior tests remain count-only; no new endpoint/schema/layer/dependency decision, playback scope or unrelated rewrite is added.
- **Final analysis CLEAR; plan FROZEN against this brief at the verified HEAD and current uncommitted artifact contents.** Critical 0, High 0, Medium 0, Low 0 remaining bounded findings. No current needs-recon, needs-decision or owner structural checkbox is established. The 12-question cap remains exhausted, with F5 a Q7 clarification. Exact inherited DEV-320 TypeScript codegen command is not found in these roots and remains the mandatory future Phase B pickup stop in T001, not a present clearance blocker or substitute-tool authorization.
- Gate 1 remains separate and closed until owner approval. This is Phase A analysis/plan clearance only. Only brief.md was edited for this final ask; no other artifacts, production, notes or tracker were changed, and no gates/tests/Spec Kit scripts, commit, PR action or merge were performed. No execution PASS is claimed and no search was widened beyond the named task note and feature artifacts.
