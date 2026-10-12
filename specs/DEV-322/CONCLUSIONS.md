# DEV-322 - Patron grill conclusions

Grill seeded 2026-10-12 by Patron on the Bernstein ask. Base: feature/322-spec @ cb6fe48. Sources: note DEV-322:8 (ticket text), recon-DEV-322:11-63, specs/PRODUCT.md, .specify/memory/constitution.md:335, lamuflix-team-charter §2.3. 10 of the 12-question budget used. No `blocked: structural` item: nothing below changes ticket text or departs from the constitution.

## Q1 - Genre count badges have no data source (API shape, care item 4)
**Ruling: extend `GenreDto` additively with `movieCount` (int, required) on `GET /api/genres`; no new route, no status code change.**
- The ticket names "genre multi-select w/ count badges" (DEV-322:8), so counts are delivered scope; facets carry id+name only (recon-DEV-322:27,32).
- Count = total movies carrying the genre, global (not narrowed by the active filters). Computed by query over existing tables; no schema change (care item 3 not triggered).
- Chain per charter: C# DTO -> regenerate committed `web/src/api/openapi.json` -> regenerated `types.ts`. Update `FacetEndpointsTests`/`EfMovieCatalogFacetsTests` and the frozen-shape tests the field touches (forced edit, AC).

## Q2 - Sort label "IMDb" vs contract member `Rating`
**Ruling: UI label "IMDb" maps to `sort=Rating`; no contract rename.**
- MovieSort.cs:7-10 has Title/Year/Rating/Runtime; `Rating` is the only rating in the domain and is `ImdbRating` (MovieMetadata.cs:12,36).
- Dropdown offers 8 options: Title, Year, IMDb, Runtime x Asc/Desc, sent as `sort` + `direction` (recon-DEV-322:26).

## Q3 - npm dependencies (care item 1)
**Ruling: no new npm dependency for slider, multi-select, debounce, router or URL state; TanStack Query is allowed because the constitution already chose it.**
- Constitution:335 fixes the web stack (TanStack Query, Zod, openapi-typescript + openapi-fetch); adding TanStack Query here is a pinned addition of an already-chosen dependency if the Phase B base lacks it.
- Range sliders: two native `<input type=range>` per range; multi-select: native checkbox list (cast list with a client-side text filter); debounce: a small local hook; URL state: `URLSearchParams` + `history.replaceState`/`popstate`. No react-router, no state library (care item 2 not triggered).

## Q4 - Generated TypeScript client absent (contract chain)
**Ruling: DEV-322 consumes the DEV-320 generated client; it does not hand-roll types or own codegen.**
- types.ts not found at cb6fe48 (recon-DEV-322:47,61); DEV-320 owns generation (specs/DEV-396/brief.md:24, recon-DEV-322:49).
- Sequencing: Phase B starts only on a base containing DEV-320's `types.ts`; if DEV-320 is unmerged then, Keel orders DEV-322 build after it. Spec work proceeds now. Q1's regeneration then runs through DEV-320's script.

## Q5 - No host page; App.tsx edit (care items 2 and 6)
**Ruling: build the panel as a self-contained feature folder under `web/src/features/library/` and mount it with a minimal result count/list in `App.tsx`; the App.tsx/App.test.tsx edit is forced by the AC.**
- AC "filter change updates URL query + refetch" (DEV-322:8) needs a host that fetches; none exists (recon-DEV-322:22-23,57).
- A feature folder under existing `web/src` is not a new top-level folder or layer. If the Phase B base already contains a library page, mount there instead and leave App.tsx alone.
- The minimal host shows only `totalCount` and titles (MovieSummaryResponse is id+title, recon-DEV-322:31); no grid/card design in scope.

## Q6 - URL query contract
**Ruling: URL keys mirror the API query keys 1:1 (`text, genreIds, actorIds, runtimeMin, runtimeMax, runtimeIncludeUnknown, yearMin, yearMax, statuses, sort, direction`); arrays as repeated keys; defaults are omitted from the URL.**
- Keys from LibraryEndpoints.cs:21-34 / MovieQueryString.cs:12-25 (recon-DEV-322:26); repeated keys preserve order (LibraryEndpointsTests BrowseMovies_RepeatedArrayKeys_PreserveRequestOrder).
- A range at its full extent sends neither bound (avoids the partial-range 400 path, recon-DEV-322:38); a moved range always sends both bounds.
- `inWatchlist`, `page`, `pageSize` are not panel controls; a filter change resets `page`. Unknown/foreign keys in the URL are preserved untouched.
- URL is the single source of truth: panel state derives from it, so back/forward and reload restore filters. Text writes `replaceState` after debounce; discrete controls also `replaceState` (no history spam).

## Q7 - Range bounds without a range facet (API shape)
**Ruling: static slider extents, no new endpoint.** Year 1900..current year, step 1; runtime 0..240 min, step 5, top stop meaning "240+" sends no `runtimeMax`. Logged `[assumed]`.

## Q8 - Status chips
**Ruling: four toggle chips for EnrichmentStatus Pending, Enriched, NotFound, Failed (EnrichmentStatus.cs:7-10); none selected = no `statuses` param = all.** Labels in ASSUMPTIONS.md.

## Q9 - "Fast fluid interaction without layout shift" (AC, DEV-322:8)
**Ruling: testable as: panel controls keep fixed dimensions while loading/refetching (badges fixed-width, results keep previous data via TanStack Query `placeholderData: keepPreviousData`), and no control unmounts on refetch.**
- RTL tests assert the panel stays mounted and the old result stays visible during a pending refetch; no CLS metric tooling (no new dependency).
- Debounce 300 ms for text (ASSUMPTIONS.md); each URL change triggers exactly one fetch (query key = normalized params).

## Q10 - Test scope
**Ruling: Vitest + RTL + MSW, the three named suites plus the URL/refetch AC; backend tests only for Q1.**
- Named: slider (DEV-322:8 "RTL slider"), multi-select, debounced search (fake timers).
- AC: a filter change updates `window.location.search` and MSW sees one request with matching params; reload from a URL hydrates controls.
- Backend: GenreDto `movieCount` integration + handler unit tests; mutation gate applies to the changed `src` files.

## Noted, no ticket
- Faceted (filter-narrowed) genre counts: noted, no ticket.
- Server-provided year/runtime extents: noted, no ticket.
