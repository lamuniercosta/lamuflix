# Feature Specification: Persist TMDb Collections and Collection Facet Filter

**Feature Branch**: `feature/424-spec`
**Created**: 2026-10-28
**Status**: PLAN FROZEN — Keel Phase 2 step 6 adjudication; see PLAN_CHALLENGE.md. Gate 1 remains closed.
**Input**: Ticket DEV-424 (parent DEV-100, Size M, `ui:`); frozen `brief.md` (Q1–Q8 + C1 + Phase 2 step 6 @ `7a335ac3` + F7 amendment @ `10ffc9c1`); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `CONTEXT.md`; Maestri notes `DEV-424`, `recon-DEV-424`, `recon-DEV-424-2`; `specs/PRODUCT.md`

## Scope

**In scope** (ticket Scope & Technical Design lines 27–30; brief frozen scope 1–8; Q1–Q6):

- Extend existing TMDb response mapping and `MovieMetadata` with optional collection id/name; carry that value through the existing enrichment persistence path (DEV-331 deferred collections here).
- Persist `CollectionRecord` and nullable `MovieRecord.CollectionId` via a generated EF migration; existing movies remain null until next successful enrichment (no backfill).
- Upsert collection id/name and replace membership atomically in the existing repository save boundary; `CollectionRecord` uses the TMDb int id as PK with `ValueGeneratedNever` (no surrogate); concurrent unique/PK conflict requires implement-and-prove whole-save rollback/reset/re-read/retry (not claimed as existing proven behaviour); shared collection ids must not create duplicate rows.
- Successful metadata without a collection clears membership (including successful non-TMDb fallback); failed/unavailable enrichment preserves prior metadata and prior membership; accepted residual: membership may stay cleared until a later successful TMDb enrichment restores it; no provider provenance or partial-field merging. Deliberate `belongs_to_collection` mapping rule (F7 @ `10ffc9c1`): a present object with nonpositive id or blank name rejects the whole TMDb details response as Failed InvalidResponse through the existing failure seam — not Found with the collection silently dropped; missing/null remains valid absent collection; this does not claim all existing optional fields fail wholesale.
- Retain unused collection rows; hide zero-member collections from the facet; no collection deletion cascades into movies; no orphan cleanup job (orphan telemetry/cleanup follow-up out of scope — Sentry F5 noted, no fix in this ticket).
- `GET /api/collections` (operation `GetCollections`) returns `CollectionDto[]` of `id`, `name`, `movieCount` with `CollectionFacet`/`CollectionDto` shape `(int Id, string Name, int MovieCount)` (long count rejected); count distinct current library members; omit zero-member collections; do not narrow counts by active filters; no empty `GetCollectionsQuery` validator; preserve existing `AddHandler` chain.
- Browse raw `BrowseMoviesRequest.CollectionId` is `string?` with `CollectionIdKey`, `ReadNumber`, and repeated-key detection; nullable positive `collectionId` on `MovieQuery`, predicates, equality/hash, codecs, and `MovieQueryValidator` (null or > 0); AND with other filters; absent = unconstrained; valid unknown id → empty page; malformed/nonpositive/repeated/empty (`abc`, `0`, `-3`, repeated) → 422 ProblemDetails; codecs reject invalid values; preserve existing pagination (Q3 status superseded by CONCLUSIONS.md C1).
- Filter Panel Collection section: existing Genre fieldset/legend + `label.lamu-chip` with sr-only checkbox single-select toggle (not radio); URL key `collectionId`; select/replace/clear, page reset, active chip, clear-all; unknown-id fallback `Collection <id>` toolbar-only and retained through facet loading/error/empty; empty facet = legend/heading only (no chips, no helper copy); no new skeleton/`aria-busy`; generated DTOs only.
- Contract generation strictly C# DTO → OpenAPI → generated TypeScript; never hand-edit generated contracts or hand-roll frontend DTOs.
- Bounded file set authorized in brief affected-file manifest and Q1 §2.3 item 6.

**Out of scope** (ticket; brief Q1–Q2, Q5, Q7–Q8):

- Grid grouping; backfill of existing rows (next enrichment only).
- Provider provenance, partial-field merging, orphan cleanup job, collection cascade-delete into movies.
- New NuGet/npm dependency, project, architectural layer, schema beyond ticket-named tables, `Features:LocalPlay`, secrets, or `Process.Start`.
- Renaming generic sequence helpers such as `HasSameCollectionValues` / `SameCollections` (programming “collection”, not TMDb Collection).
- ADR authorship (Q8: none required); README/C4 rewrites; Gherkin unless separately opted in.
- Visual layout decisions (Palette owns `design.md`; challenge disposition/freeze remain separate).
- Orphan-row telemetry/cleanup job (Sentry F5 — out of scope this round).

**Precedence**: Ticket Scope & Technical Design is already approved authority (`specs/PRODUCT.md:23–24`). Brief Q1–Q8 rulings plus Phase 2 step 6 challenge rulings (`CONCLUSIONS.md` @ `7a335ac3`) and Sentry F7 (`CONCLUSIONS.md` @ `10ffc9c1`) govern data path, storage, public API (§2.3 item 4), Filter Panel interaction, edge cases, task order/tests/gates, closing bar/frozen scope/round cap, and glossary. Patron authorized supporting-file edits under §2.3 item 6 (Q1) and explicit public API under item 4 (Q3). Historical Q3 text that said 400 for malformed/nonpositive/repeated `collectionId` is superseded in that part only by `CONCLUSIONS.md` C1 (commit `2754a161`): those inputs return **422** ProblemDetails within constitution Principle V — no departure, no owner checkbox.

## User Scenarios & Testing

### User Story 1 - Persist Collection Membership from Enrichment (Priority: P1)

As the enrichment pipeline, I need optional TMDb collection id and name carried through metadata into durable collection rows and a nullable movie membership, so later facets and filters have real library membership without backfilling old rows.

**Why this priority**: ticket named persistence path; Q1–Q2, Q5; foundation for every later story.

**Independent Test**: adapter/domain mapping cases proving with-collection, missing/null valid-absent, and malformed-present Failed InvalidResponse (not Found); real-database repository tests for upsert, name update, forced two-movie concurrent same-new-id race (one row + both memberships, no surfaced conflict), replace, explicit fallback-clear and failure-preserves, pre-migration rows remaining null, no cascade loss; migration asserts natural PK + `CollectionId` index.

**Acceptance Scenarios**:

1. **Given** a successful TMDb response with a positive collection id and non-blank name **When** enrichment persists **Then** a `CollectionRecord` for that id exists/updated with the returned name and the movie’s `CollectionId` points to it (Q1, Q2).
2. **Given** two movies forced into concurrent enrichment of the same new collection id **When** both saves complete **Then** exactly one collection row exists, both movies are members, and no conflict is surfaced (Q5; step 6 ruling 1).
3. **Given** a movie already in collection A **When** successful enrichment returns collection B **Then** membership is replaced with B atomically inside the existing save boundary via the required whole-save rollback/reset/re-read/retry on unique/PK conflict (Q2, Q5; step 6 ruling 1).
4. **Given** a prior member and successful non-TMDb fallback metadata without a collection **When** enrichment persists **Then** membership is cleared (`CollectionId` null); unused collection rows remain and are not cascade-deleted into movies; residual until next successful TMDb restore is accepted (Q2, Q5; step 6 ruling 2).
5. **Given** failed or unavailable enrichment **When** the attempt finishes **Then** existing collection membership and metadata are preserved (explicit failure-preserves coverage) (Q2; step 6 ruling 2).
6. **Given** a present `belongs_to_collection` object with nonpositive id or blank name **When** TMDb details mapping runs **Then** the whole details response is Failed InvalidResponse through the existing failure seam — not Found with the collection silently dropped — and no invalid or invented collection row is persisted (Q5; Sentry F7 @ `10ffc9c1`).
7. **Given** missing or null `belongs_to_collection` **When** TMDb details mapping runs **Then** the result is a valid absent collection (not a failure) (Q5; Sentry F7 @ `10ffc9c1`).
8. **Given** movies that existed before the migration **When** no enrichment has run yet **Then** `CollectionId` remains null (ticket out-of-scope backfill; Q2).

### User Story 2 - Collection Facet Lists Nonempty Library Collections (Priority: P1)

As a library browser, I need a collections facet that lists only collections with current members and shows distinct member counts, so I can discover franchise groupings the same way I discover genres.

**Why this priority**: ticket “FacetEndpoints collections mirror genres”; Q3.

**Independent Test**: facet endpoint and catalog tests for populated/empty payloads, distinct counts, orphan exclusion, genre-equivalent ordering (name then id), and no filter-narrowed counts.

**Acceptance Scenarios**:

1. **Given** nonempty library collections **When** `GET /api/collections` (`GetCollections`) runs **Then** response is `CollectionDto[]` with `id`, `name`, `movieCount` using genre facet type/response conventions (Q3).
2. **Given** a collection row with zero current members **When** the facet is fetched **Then** that collection is omitted (Q2, Q3).
3. **Given** multiple members of one collection **When** counts are computed **Then** `movieCount` is the distinct current library member count and is not narrowed by active browse filters (Q3).
4. **Given** the facet list **When** ordered **Then** ordering mirrors genres (name, then id) (Q3; existing `GetGenresAsync` convention).
5. **Given** no nonempty collections **When** the facet is fetched **Then** an empty array is returned (Q3).

### User Story 3 - Browse Filter by collectionId (Priority: P1)

As a library browser, I need browse requests to accept a single optional `collectionId` and AND it with other filters, so selecting a collection narrows the grid without breaking pagination or other filters.

**Why this priority**: ticket `MovieQuery collectionId`; Q3.

**Independent Test**: unit codec/equality/hash/validator coverage (including existing `MovieQuerySurvivorTests` / `MovieQueryStringSurvivorTests`); integration browse/paging with AND composition; malformed/empty/`abc`/`0`/`-3`/repeated → 422 ProblemDetails; unknown id → empty page.

**Acceptance Scenarios**:

1. **Given** no `collectionId` **When** browsing **Then** results are unconstrained by collection (Q3).
2. **Given** a valid positive `collectionId` matching members **When** browsing **Then** only those members are returned, ANDed with other active filters, with existing pagination preserved (Q3).
3. **Given** a valid positive `collectionId` with no members **When** browsing **Then** an empty page is returned (Q3).
4. **Given** malformed, empty, nonpositive, or repeated single-valued `collectionId` input (`abc`, `0`, `-3`, repeated key) **When** the browse API runs **Then** the response is 422 ProblemDetails per constitution Principle V and existing browse ValidationException mapping; codecs and `MovieQueryValidator` never construct a nonpositive `CollectionId` (Q3 status superseded by CONCLUSIONS.md C1; step 6 ruling 4).
5. **Given** two `MovieQuery` instances **When** comparing equality/hash or round-tripping codecs **Then** `CollectionId` participates like other filter fields (Q3).

### User Story 4 - Filter Panel Collection Chips and URL (Priority: P1)

As a library user, I need a Collection section of existing Filter Panel chips with single-select semantics and a durable `collectionId` URL key, so I can filter by collection, reload/back-forward restore, and clear without silently broadening an unknown selection.

**Why this priority**: ticket `ui:` / single-select pills in URL (DEV-322 Filter Panel); Q4. Visual layout remains Palette’s.

**Independent Test**: web TDD for fieldset/legend + sr-only checkbox chip select/replace/toggle/clear/clear-all, page reset preserving other filters/sort, URL reload/back-forward, toolbar-only unknown-id fallback retained through loading/error/empty, legend-only empty section (no helper copy), no skeleton/`aria-busy`, plain-text hostile-name and overlong-layout oracles, consuming generated types only.

**Acceptance Scenarios**:

1. **Given** nonempty collection facets **When** the user checks a `label.lamu-chip` sr-only checkbox **Then** that id becomes the sole selection and the URL carries `collectionId` (Q4; step 6 ruling 3).
2. **Given** a selected collection **When** the user checks another chip **Then** the previous selection is replaced; unchecking the active chip clears it (checkbox toggle, not radio) (Q4; step 6 ruling 3).
3. **Given** a selection change or clear **When** state updates **Then** page resets like other filters while other filters/sort are preserved (Q4).
4. **Given** a URL with `collectionId` **When** reload or back/forward restores state **Then** the selection is restored (Q4).
5. **Given** a valid unknown URL id absent from facets **When** UI renders **Then** the fallback chip `Collection <id>` appears only in the active-filter toolbar (never as a Collection section option), remains through facet loading/error/empty, and results are not silently broadened (Q4; ASSUMPTIONS.md; step 6 ruling 3).
6. **Given** an empty facet **When** the Collection section renders **Then** only the fieldset legend/heading is shown — no chips and no helper copy (Q4; ASSUMPTIONS.md; step 6 ruling 3).
7. **Given** an applied collection **When** the active-filter chip is removed or clear-all runs **Then** the collection filter is cleared (Q4).
8. **Given** facet fetch **When** loading or error occurs **Then** existing query/client loading/error patterns apply with no new skeleton/`aria-busy`; UI consumes generated DTOs only (Q4, brief scope 8; step 6 ruling 3).
9. **Given** a collection name containing hostile markup or an overlong string **When** chips/toolbar render **Then** the name is plain text only (inert) and layout remains usable (Sentry F4).

### Edge Cases

- Successful enrichment without collection clears membership; failure preserves it — absence on success is distinct from failure (Q2, Q5).
- Successful non-TMDb fallback without collection clears membership; residual until next successful TMDb restore accepted; no release-note task; no partial-field merge or provider provenance (Q5; step 6 ruling 2).
- Present malformed `belongs_to_collection` (nonpositive id or blank name) rejects the whole TMDb details response as Failed InvalidResponse — never Found with field silently dropped, never invents/persists an invalid row; missing/null is valid absent; final failed/unavailable enrichment preserves prior metadata/membership; successful non-TMDb fallback still clears membership under Q5; does not claim all existing optional fields fail wholesale (Q5; Sentry F7 @ `10ffc9c1`).
- Unused/orphan collection rows remain in storage but are hidden from facets; orphan telemetry/cleanup out of scope (Q2; Sentry F5).
- Valid unknown browse/URL id stays applied via toolbar-only fallback; browse returns empty page (Q3, Q4; step 6 ruling 3).
- Do not rename `HasSameCollectionValues` / `SameCollections` — unrelated to TMDb Collections (Q8; recon naming collision).
- Pre-existing movies stay null until next enrichment — no backfill job (ticket; Q2).
- Concurrent same-new-id enrichment must yield one row + both memberships with required rollback/reset/re-read/retry — not claimed as existing proven behaviour (Q5; step 6 ruling 1).

## Requirements

### Functional Requirements

- **FR-001**: Extend `TmdbMetadataProvider.Mapping.cs` (including response DTO) and `MovieMetadata` so optional collection identity (positive TMDb id + returned name) flows into the existing enrichment write path. Deliberate `belongs_to_collection` rule (Sentry F7 @ `10ffc9c1`): present object with nonpositive id or blank name rejects the whole TMDb details response as Failed InvalidResponse through the existing failure seam — not Found with the collection silently dropped — and never persists an invalid or invented row; missing/null remains valid absent collection; this does not claim all existing optional fields fail wholesale (Q1, Q5).
- **FR-002**: Add `CollectionRecord` and nullable `MovieRecord.CollectionId`; register DbSet/configuration; generate EF migration updating `LamuFlixDbContextModelSnapshot.cs`. Collection identity is the positive TMDb int id as primary key with `ValueGeneratedNever` (no surrogate) and returned name; migration asserts natural PK and an index on `MovieRecord.CollectionId`; a movie has zero or one membership. No schema beyond these ticket-named tables (Q1, Q2; step 6 ruling 1; Sentry F6).
- **FR-003**: In `EfMovieRepository`, upsert collection id/name and replace/clear membership atomically inside the existing save/transaction boundary; on collection unique/PK conflict from concurrent insert, roll back/reset the failed unit, re-read the now-existing collection row, and retry the whole atomic save across reachable save paths (never partial catch-and-continue; preserve caller transaction ownership; no partial retry inside an aborted ambient transaction; no invented extra infrastructure) — required implement-and-prove; recon-DEV-424-2 confirms existing retry does not currently classify collection PK conflicts and ambient metadata writes have no retry loop; successful metadata without collection clears membership (including successful non-TMDb fallback, with explicit fallback-clear + failure-preserves tests and accepted residual until next successful TMDb); final failed/unavailable enrichment preserves prior metadata and prior membership; no cascade-delete of movies from collection rows; no cleanup/backfill/release-note job (Q2, Q5; step 6 rulings 1–2; F7).
- **FR-004**: Add Core port/facet/query/handler siblings mirroring genres: `CollectionFacet` as `(int Id, string Name, int MovieCount)`, `GetCollectionsQuery` (+ handler, no empty validator, unchanged `AddHandler` chain), `IMovieCatalog.GetCollectionsAsync`; persistence in `EfMovieCatalog` counts distinct current members, omits zero-member rows, orders like genres (name then id), and does not narrow by active filters; reject long count (Q3; step 6 ruling 5).
- **FR-005**: Expose `GET /api/collections` named `GetCollections` returning `CollectionDto[]` (`int Id`, `string Name`, `int MovieCount`) from `FacetEndpoints`, with DTO/handler registration following the genre chain and no empty query validator (Q3; §2.3 item 4; step 6 ruling 5).
- **FR-006**: `BrowseMoviesRequest.CollectionId` is raw `string?` with `CollectionIdKey` const, `ReadNumber`, and repeated-key detection in `LibraryEndpoints` (preserve existing ReSharper suppression around request properties; complexity watchlist); add nullable positive `CollectionId` to `MovieQuery`, `MovieQueryString` codecs (reject invalid), equality/hash, `MovieQueryValidator` (null or > 0), and `MovieRecordQueryExtensions` / catalog browse predicates; AND with other filters; absent unconstrained; valid unknown → empty page; empty/`abc`/`0`/`-3`/repeated → 422 ProblemDetails; preserve pagination (Q3; CONCLUSIONS.md C1; step 6 ruling 4).
- **FR-007**: Regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` via `npm run generate:api`; never hand-edit generated contracts; never hand-roll duplicate frontend DTOs; keep API drift check green (brief scope 8; Q1, Q3).
- **FR-008**: Implement Filter Panel Collection section using Genre fieldset/legend and `label.lamu-chip` with sr-only checkbox single-select toggle (not radio); URL key `collectionId`; page reset on change/clear; removable toolbar active chip and clear-all; unknown-id fallback `Collection <id>` toolbar-only and retained through loading/error/empty; empty facet = legend/heading only (no chips, no helper copy); no new skeleton/`aria-busy`; section label `Collection`; plain-text hostile/overlong name oracles; reuse existing facet fetch patterns and generated types. Palette owns visual layout/`design.md` (Q4; ASSUMPTIONS.md; step 6 ruling 3; Sentry F4).
- **FR-009**: Edit only the recon-/Q1-authorized landing zones listed in the brief affected-file manifest (Core/Infrastructure/API/web/tests/generated contracts). Preserve existing consumers of `MovieMetadata`/`MovieQuery`. Do not rename generic sequence helpers. No new dependency, project, or layer (Q1, Q8).
- **FR-010**: Test and gate plan per Q6 + step 6 ruling 6: backend tests written alongside each backend task and all green before contract-generation/web handoff (T020); web remains red-first TDD; name and extend existing `MovieQuerySurvivorTests` / `MovieQueryStringSurvivorTests` / `EfMovieRepositorySurvivorTests` for `CollectionId` branches. Never mock the data-access driver where EF translation/serialisation matters. Existing assertion/mock libraries only. All configured backend and web gates at their stages; mutation threshold never lowered; exit-2 SKIPPED/SKIP/N/A and Could not run distinct from PASS. Size M requires Sentry, Ledger, Compass. Closing bar: Critical/High/Medium block; Low needs explicit disposition; every finding gets a recorded ruling; frozen review scope and hard two-round remediation cap per Q7. Gauge baseline in note DEV-424 lines 64–84 is evidence only.

### Key Entities

- **Collection**: TMDb named grouping identified by positive TMDb collection id with returned name (`CONTEXT.md`).
- **Collection membership**: optional association of one movie to one Collection via nullable `MovieRecord.CollectionId`.
- **Collection facet**: nonempty Collections with distinct current member counts; `GET /api/collections`; Filter Panel single-select chips.
- **MovieMetadata (extended)**: existing enrichment metadata carrying optional collection id/name into the write path.
- **MovieQuery (extended)**: existing browse filter record gaining nullable `CollectionId`.

## Success Criteria

- **SC-001**: After successful enrichment with a valid TMDb collection, the movie’s membership and a single shared collection row (TMDb int PK, `ValueGeneratedNever`) are observable; forced concurrent same-new-id enrichment yields one row + both memberships with no surfaced conflict via the required whole-save retry (Q2, Q5; step 6 ruling 1).
- **SC-002**: Successful enrichment without a collection (including non-TMDb fallback) clears membership with explicit test coverage; failed enrichment leaves prior membership unchanged; pre-migration rows stay null until enriched; residual until next successful TMDb accepted with no release-note task (Q2; step 6 ruling 2).
- **SC-003**: `GET /api/collections` returns only nonempty collections with `int` distinct counts and genre-mirrored ordering; empty library facet is an empty array; no empty query validator (Q3; step 6 ruling 5).
- **SC-004**: Browse `collectionId` AND-filters correctly; unknown id yields an empty page; empty/`abc`/`0`/`-3`/repeated yield 422 ProblemDetails; codecs/equality/validator include `CollectionId` (Q3; CONCLUSIONS.md C1; step 6 ruling 4).
- **SC-005**: Filter Panel checkbox-toggle select/replace/clear, URL restore, page reset, toolbar-only unknown-id fallback retained through loading/error/empty, legend-only empty, and clear-all behave per Q4 / step 6 ruling 3 using generated types only; hostile/overlong names render as plain text.
- **SC-006**: OpenAPI → TypeScript regeneration is the only contract path; drift check passes; no hand-rolled frontend DTOs (brief scope 8).
- **SC-007**: Required unit/integration/web tests and configured gates pass at their stages; skipped/unrunnable gates are never reported as PASS (Q6, Q7).

## Assumptions

- Taste labels from Q4 are logged in `ASSUMPTIONS.md`: section label `Collection`; fallback `Collection <id>`; empty facet shows no selectable options (legend/heading only per step 6 ruling 3).
- Visual layout and `design.md` remain Palette-owned; interaction contract includes step 6 checkbox/fieldset/toolbar rulings; challenge disposition/freeze remain separate (Q4; step 6 ruling 3).
- Genre facet conventions (DTO fields with `int MovieCount`, name-then-id sort, response shape, no empty validator) are the mirror target for collections except zero-member omission, which Q3 explicitly requires for collections (step 6 ruling 5).
- TMDb JSON membership is consumed through the existing adapter mapping extension authorized in Q1; no new provider or options surface; `belongs_to_collection` follows the deliberate F7 whole-response Failed InvalidResponse rule for malformed-present objects (`CONCLUSIONS.md` @ `10ffc9c1`).
- Dependencies DEV-331 and DEV-322 are treated as landed for Phase A drafting; Phase B pickup rechecks landing/contracts per brief handoff.

## Clarifications

`/speckit-clarify` scan across Functional Scope, Domain/Data, Interaction, NFR (tests/gates), Integration/Dependencies, and Out-of-scope: **Clear** for drafting categories closed by grill Q1–Q8, step 6 rulings, and Sentry F7 @ `10ffc9c1`. Analyze C1 (400 vs 422 for invalid `collectionId`) resolved within constitution by `CONCLUSIONS.md` C1 — no owner escalation checkbox. Sentry F7 inherited-fail-scope residual is closed: deliberate `belongs_to_collection` mapping rule (present malformed → whole-response Failed InvalidResponse; missing/null → valid absent; final failure preserves; successful non-TMDb fallback still clears). No open drafting `needs decision`. `design.md` exists; challenge disposition/freeze and Gate 1 remain separate and unclaimed.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket scope lines 27–30 | Scope; US1–US4; FR-001–FR-009 |
| Q1 data path / item 6 files | Scope; US1; FR-001, FR-002, FR-009 |
| Q2 storage / replace / orphans | US1; Edge Cases; FR-002, FR-003 |
| Q3 API shape / collectionId | US2–US3; FR-004–FR-006; SC-003–SC-004 |
| C1 invalid collectionId status (supersedes Q3 400→422 only) | Scope; US3 scenario 4; FR-006; SC-004; Clarifications |
| Q4 Filter Panel / taste | US4; FR-008; Assumptions; SC-005 |
| Q5 atomicity / whole-metadata / malformed | US1; Edge Cases; FR-001, FR-003 |
| Q6 order / tests / gates | FR-010; plan/tasks phases |
| Q7 closing bar / frozen scope / round cap | FR-010; plan Gates |
| Q8 glossary / no ADR | CONTEXT.md; Assumptions; FR-009 |
| ASSUMPTIONS.md taste | US4 scenarios 5–6; FR-008 |
| Step 6 ruling 1 (F1/F7 natural PK + whole-save retry) | Scope; US1 scenarios 2–3; FR-002, FR-003; SC-001 |
| Step 6 ruling 2 (F2 fallback-clear residual) | US1 scenarios 4–5; Edge Cases; FR-003; SC-002 |
| Step 6 ruling 3 (F1/F5 UI checkbox/fieldset/toolbar) | US4; FR-008; SC-005 |
| Step 6 ruling 4 (F3/F2 parse/validator/codec) | US3 scenario 4; FR-006; SC-004 |
| Step 6 ruling 5 (F6 int MovieCount / no empty validator) | Scope; FR-004, FR-005; SC-003 |
| Step 6 ruling 6 (F3 backend tests alongside) | FR-010; tasks phases 2–5 |
| Sentry F4/F6 (hostile name; migration index) | US4 scenario 9; FR-002, FR-008 |
| Sentry F7 (`belongs_to_collection` whole-response Failed @ `10ffc9c1`) | Scope; US1 scenarios 6–7; Edge Cases; FR-001; Clarifications; T003/T004 oracles |
| Sentry F5 (orphan telemetry) | Out of scope — noted no fix |

