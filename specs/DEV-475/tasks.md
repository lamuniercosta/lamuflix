---

description: "Task list for DEV-475 SPA cleanup (dead exports, navigation, cohesive splits)"
---

# Tasks: SPA cleanup (dead exports, navigation, cohesive splits)

**Input**: Design documents from `specs/DEV-475/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q11; Q12 in `CONCLUSIONS.md`), `CONCLUSIONS.md` (incl. Q12), `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md`; Maestri notes `DEV-475`, `recon-DEV-475`; `specs/PRODUCT.md`.

**Status**: Independent analyze CLEAN (20:00). Plan-challenge **NEEDS FIXES — NOT PLAN FROZEN**. Quill plan-repair round 2 applied (Keel 9-item list + Q12). Gate 1, implementation, runtime acceptance, and merge remain distinct and open. Quill does not declare freeze or Gate 1.

**Tests**: Required (`brief.md` Q7; FR-014–FR-016). Prefer characterization/regression before risky edits where coverage is insufficient. Cover programmatic navigation without synthetic popstate **and** simulated native Back/Forward popstate; history/return URL/player intent per entry point; filter/sort/default/fallback; details/related navigation; listener teardown; MovieCard fallback; pure chip builder; debounce cancel-on-nav; independent facet keyboard. No new test framework. No arbitrary line-count assertions. No replacement tests for intentionally removed orphan code.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared inventory/gate/receipt tasks are never `[P]`.

**Pipeline-only tasks**: T000, T001, T012, and T013 are **pipeline-only** (excluded from production implementation dispatch). They retain Gate 1 / evidence / review / final-head prerequisites. Production Build-lane work is T002–T011.

**Conventions**: Edit only approved known paths and Q10/Q12-ratified siblings; STOP for cited Patron ruling before whole-file deletion beyond `libraryUrl.ts` + `libraryUrl.test.ts` or any other §2.3 surface; preserve OpenAPI→generated TypeScript chain; no Tailwind change; no jsx-a11y install; no explanatory comments beyond AGENTS exemptions. Otherwise-unassigned implementation work is Build-lane owned (do not invent named seats). Execution paths are **repo-relative** (from `web/` for npm/tsc). Historical worktree provenance: `F:/Dev/LamuFlix.worktrees/feature-475-spec` on `feature/475-spec`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. Spec PR body MUST quote the ticket Done-when split permission (`recon-DEV-475` line 58) and carry DEV-511 / DEV-512 dispositions. No Build work (T002+) before Gate 1. T001 evidence integration may precede Gate 1 when Conductor routes it.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   [pipeline-only] Bernstein/Rigger/user/Keel Gate 1 — blocks T002+
Phase 1   T001          [pipeline-only] integrate bounded inventories / citations — also prerequisite to PLAN FROZEN
Phase 2   T002          (web baseline + characterization where coverage insufficient)
Phase 3   T003–T004     (US1 orphan/alias + Q12 typed GetPeople helper + literal-type cleanup)
Phase 4   T005 then T006+T007 atomic cutover  (US2 shared navigation; one commit/checkpoint)
Phase 5   T008–T010     (US3 three cohesive extractions — Q10/Q12 ownership)
Phase 6   T011          (US4 final web lint/typecheck/unit/e2e — package.json commands)
Phase 7   T012          [pipeline-only] capped three-axis review / remediation per Q8
Phase 8   T013          [pipeline-only] delivery / merge-bar handoff — user merges
```

Ordering per brief Ordered task plan + Q10/Q12: inventories → characterization → orphan/type → navigation (atomic cutover) → extractions → final verification → review → handoff. Original jsx-a11y step deferred to DEV-512.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 2+ implementation) — pipeline-only

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build implementation.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build implementation tasks before that receipt (T001 evidence integration may precede Gate 1 when Conductor routes plan-freeze recon).

- [ ] T000 **[pipeline-only]** **Bernstein** coordinates and **Rigger** prepares/opens the DEV-475 spec PR for ordinary owner Gate 1; only the user merges. Spec PR body MUST carry: (1) the unanswered **Q13 structural owner checkbox** authorizing retention of runtime `role=actor` on `GET /people` via the shared literal-typed helper while removing inline casts (`CONCLUSIONS.md` Q13; `spec.md` Owner question — Gate 1 stays closed until checked); (2) ticket Done-when split permission (`recon-DEV-475` line 58); (3) DEV-511/DEV-512 dispositions. After merge (ordinary Gate 1 plus checked Q13 checkbox), **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-475 in `specs/DEV-475/CONCLUSIONS.md`. Receipt only. T002–T011 MUST NOT proceed without verified merge once implementation starts (`brief.md` review bar; Q8–Q9; Q13).

---

## Phase 1: Bounded inventories (blocks inventing plan-freeze facts) — pipeline-only

**Purpose**: Confirm and cite the recon facts brief Q9/Q10/Q12 require before PLAN FROZEN.
**Checkpoint**: Citations recorded; Keel may then freeze plan after independent reread. Missing facts → STOP `needs recon`, do not invent.

- [ ] T001 **[pipeline-only]** **Wisp** / Quill evidence integration (Conductor-routed): Integrate and cite without inventing product decisions: (1) reference/export inventory for `web/src/library/libraryUrl.ts`, `web/src/library/libraryUrl.test.ts`, and named `web/src/features/library/filterQuery.ts` alias/shim exports — cite complete `filterQuery` importer set `recon-DEV-475` lines 12–14 (`App.tsx`, `FilterPanel.tsx`, `FilterPanel.test.tsx`, `filterQuery.test.ts`, `movieDetailsLabels.ts`, `LibraryView.tsx`, `browseParams.ts`); ticket authority for named deletions `recon-DEV-475` lines 39–58 / recovered summaries 61–66; do not treat the former three-file list as complete; importer presence is not edit authorization; (2) Q10-ratified sibling module filenames/ownership plus Q11 `web/src/features/app-navigation/appNavigation.ts` and Q12 DualRangeField / FilterSortControls / chip-builder / GetPeople rulings; (3) jsx-a11y not in delivery — deferral to DEV-512; (4) exact web commands from `web/package.json` as listed in `plan.md` Technical Context (repo-relative from `web/`); tracked e2e paths as dated snapshot; SMOKE-001/002/003 candidate map `recon-DEV-475` lines 25–27; (5) GetPeople/MovieCard/return-URL facts `recon-DEV-475` lines 8–10, 16–18, 20–23; (6) verified follow-ups DEV-511 and DEV-512. Do not adopt/remove Tailwind. Do not install jsx-a11y. Do not edit production code in this task (`brief.md` Handoff; Q3/Q4/Q10–Q12; FR-020).

---

## Phase 2: Baseline and characterization (after T001 facts; after T000 for implementation lane)

**Purpose**: Establish current web baseline and add regression characterization only where behavioural coverage is insufficient.
**Checkpoint**: Baseline recorded; gaps in coverage for navigation/filter/details seams identified; characterization tests added only where needed.

**Baseline receipt (T002, HEAD `22261b35`, from `web/`)**:
- lint `npm run lint`: exit 0 (check:api pass, eslint 0 findings, prettier clean).
- typecheck `tsc --noEmit`: exit 0; `tsc --noEmit -p tsconfig.node.json`: exit 0; `npm run typecheck:e2e`: exit 0.
- unit `npm run test:run`: exit 0 — 65 files, 986 tests before characterization.
- e2e `npm run e2e`: exit 0 — 19 Playwright tests passed on the real Compose stack (stream acceptance 32 cases recorded).
- Coverage gaps identified (characterization added only here): (1) programmatic-entry assertions were absent for the details 404 exit and for the More-with row exit (return URL + no `playerIntent`); (2) no suite asserted popstate listener teardown or an exactly-one route update per programmatic `navigate`.
- Characterization added: `useAppNavigation.test.tsx` (+2: one route update per programmatic navigate, popstate listener removed on unmount); `MovieDetailsView.test.tsx` (404 "Back to library" click now asserted to land on `/`; More-with row asserted to retain `libraryReturnUrl` and to not set `playerIntent`).
- Post-characterization unit: 65 files, 989 tests, exit 0. No production behaviour change. Cutover proof for "no synthetic dispatch" deliberately not asserted yet — lands with T005–T007.
- Filters/sort/MovieCard/native Back-Foward coverage audited as already sufficient (`filterQuery.test.ts`, `FilterPanel.test.tsx` incl. debounce cancel-on-popstate and director keyboard, `LibraryView.composed/toolbar.test.tsx`, `MovieCard.test.tsx` fallback, `App.test.tsx` Back/Forward) — no additions.

- [X] T002 After T000 and T001: **Build lane** / Gauge as routed. Run existing web baseline using **plan.md repo-relative commands** from `web/` (lint/typecheck/unit; e2e if runnable — else record Could not run without claiming PASS). Add characterization coverage only where current behavioural coverage is insufficient for programmatic navigation, native Back/Forward, filter/sort/fallback, details/related navigation, or MovieCard fallback — in the justified existing suites among `web/src/features/app-navigation/useAppNavigation.test.tsx`, `web/src/features/library/filterQuery.test.ts`, `web/src/features/library/FilterPanel.test.tsx`, `web/src/library/LibraryView.composed.test.tsx`, `web/src/library/LibraryView.toolbar.test.tsx`, `web/src/library/MovieCard.test.tsx`, `web/src/features/movie-details/MovieDetailsView.test.tsx`, App tests, and plan-listed remaining suites. Assertions that will later prove production entry points / no synthetic dispatch may be sketched here but cutover proof lands with T005–T007. No new test framework; no line-count asserts; no production behaviour change in this task (`brief.md` Q7 step 1–2; FR-013–FR-015).

---

## Phase 3: User Story 1 - Evidence-led orphan and type cleanup (Priority: P1)

**Goal**: Remove proven dead aliases/exports and named `libraryUrl` pair; remove inline GetPeople casts via typed helper retaining `role=actor`; literal-type sort/direction without behaviour change.
**Independent Test**: Inventory evidence recorded; deletions land only with proof; canonical filterQuery tests green; GetPeople still sends `role=actor`; library filter behaviour unchanged.
**Order**: T003 → T004.

- [X] T003 [US1] **Build lane**. After T002: Using T001 / complete importer set (`recon-DEV-475` lines 12–14), remove named `filterQuery.ts` aliases/shim and other proven unreferenced exports in `web/src/features/library/filterQuery.ts` (and only export sites proven unreferenced). Preserve canonical live `parseFilterQuery` / `toUrlQuery` / `formatRatingBound` behaviour, runtime validation, and `web/src/features/library/filterQuery.test.ts`. Do not authorize edits to additional importers merely because they import the module. Delete `web/src/library/libraryUrl.ts` and `web/src/library/libraryUrl.test.ts` only if inventory confirms. **STOP** for cited Patron ruling before any other whole-file deletion. No replacement aliases or orphan replacement tests (`brief.md` Q4; FR-007–FR-008).
- [X] T004 [US1] **Build lane**. After T003: Per Q12: introduce a shared literal-typed helper that supplies the required `{ role: 'actor' }` GetPeople query option; replace inline `as unknown as Record<string, unknown>` casts at `web/src/library/LibraryView.tsx` and `web/src/features/movie-details/movieDetailsQueries.ts` (`recon-DEV-475` lines 8–10). Retain runtime `role=actor`. Do **not** require nonexistent generated role query types. Do **not** alter `web/src/api/openapi.json`, `web/src/api/types.ts`, or backend endpoints. Literal-type sort/direction to existing allowed values in the live library path (`web/src/features/library/filterQuery.ts` and direct callers as needed). Preserve runtime URL validation, defaults, invalid-value fallback, filtering, and request shapes. Update associated tests for typing/call-site truth without changing user-visible options (`CONCLUSIONS.md` Q12; FR-009–FR-010).

**Checkpoint receipt (T003–T004, HEAD `3e4925ad` + this commit)**:
- **T003 inventory (bounded, at this head).** Complete `filterQuery` importer set is unchanged from `recon-DEV-475` lines 12–14: `App.tsx`, `features/library/FilterPanel.tsx`, `FilterPanel.test.tsx`, `filterQuery.test.ts`, `features/movie-details/movieDetailsLabels.ts`, `library/LibraryView.tsx`, `library/browseParams.ts`. No barrels, no default/named-namespace imports, no dynamic `import()`/`require()` of the module anywhere under `web/src` or `web/e2e` (`rg` for callable shim, `filterQuery` specifier forms, and default-import form returned only the seven static importers).
- **Removed (proven unreferenced).** The callable module shim `filterQueryCallable` + `export default`, and every alias export: `decodeFilterQuery`, `fromUrl`, `fromSearchParams`, `fromUrlSearchParams`, `filterQueryFromUrl`, `filterQueryFromSearchParams`, `serializeFilterQuery`, `encodeFilterQuery`, `stringifyFilterQuery`, `filterQueryToUrl`, `filterQueryToUrlSearchParams`, `urlQueryFromFilterQuery`, `toUrl`, `serializeToUrl`, `filterQueryToApiQuery`, `filterQueryToCanonicalQuery`, `toMovieQueryParams`, `normalizeUrlToApiQuery`, plus the unreferenced wrappers `toUrlString`, `toQueryString`, `toUrlSearchParams`, `toApiUrlSearchParams`, `normalizeFilterQuery`, `normalizeToFilterQuery`, `canonicalizeFilterQuery`, `toCanonicalQuery`. Each had zero importers outside its own defining file. No replacement aliases were added.
- **Unexported, not deleted (live implementation).** `isRatingBoundValid` is only reached through `isRatingActive`, so it lost its `export` but kept its behaviour. `SortOption` and `SortSelection` are signature names, so `SortOption` is now module-local.
- **Preserved.** `parseFilterQuery`, `toUrlQuery`, `toApiQuery`, `toCanonicalApiQuery`, `formatRatingBound`, `SORT_OPTIONS`, the extent/`DEBOUNCE_MS` constants, runtime validation and every `filterQuery.test.ts` assertion still pass unchanged.
- **Deleted pair.** `web/src/library/libraryUrl.ts` and `web/src/library/libraryUrl.test.ts` removed with `git rm`. Inventory confirms the only reference to that module was its own test file; nothing under `web/src` or `web/e2e` imported it. This is the ticket-named pair, so no further Patron ruling was needed and no other whole file was deleted.
- **T004 helper.** New `web/src/library/peopleQuery.ts` exports the literal `GetPeopleQuery = { role: 'actor' }`, `ACTOR_PEOPLE_QUERY`, and `actorPeopleParams(): { query: GetPeopleQuery }`. Both call sites (`LibraryView.tsx`, `movieDetailsQueries.ts`) now pass `actorPeopleParams()`; no `as unknown as` cast remains at either. Runtime request shape is unchanged (`role=actor` still sent) and `openapi.json` / `types.ts` / backend are untouched.
- **T004 literal typing.** `filterQuery.ts` now exports `LibrarySort` / `LibraryDirection` literal types, `DEFAULT_SORT` / `DEFAULT_DIRECTION` values, a typed `SORT_OPTIONS`, `isLibrarySort` / `isLibraryDirection` guards and `parseSortSelection`. `LibraryView` sort handling goes through `parseSortSelection` and rejects unknown select values instead of assigning raw strings; the sort summary fallback is derived from `DEFAULT_SORT`/`DEFAULT_DIRECTION`; `FilterPanel` types `SORT_FIELDS`, `setSort` and the direction toggle to those literals. URL parsing, defaults, invalid-value fallback and the unknown-sort fallback test behaviour are unchanged.
- **Tests updated for call-site truth.** Added `web/src/library/peopleQuery.test.ts`; added sort literal/`parseSortSelection` coverage to `filterQuery.test.ts`; added a `LibraryView.composed.test.tsx` assertion that the people lookup still sends `role=actor` through the shared helper. No user-visible option changed and no orphan replacement test was written.
- **Gates.** `pwsh -NoProfile -File scripts/run-web-gates.ps1` → exit 0, `Web gates: PASS (lint, tsc, vitest, build)`; 65 files, 988 tests pass. C# gates remain scope-empty SKIPPED.

---

## Phase 4: User Story 2 - Shared imperative navigation (Priority: P1) 🎯 MVP

**Goal**: Feature-local shared `openMovie` + one push/replace notification in `appNavigation.ts`; wire `useAppNavigation` subscription first; migrate all named producers/listeners in one atomic cutover; native popstate for Back/Forward only; retain MovieCard wrapper.
**Independent Test**: `appNavigation.test.ts` green before cutover; programmatic navigation works without synthetic popstate; native traversal still resyncs; per-entry-point history/return URL/player intent preserved; listeners tear down; MovieCard fallback covered.
**Order**: T005 → **T006+T007 atomic cutover** (single commit/checkpoint; T006 alone MUST NOT be accepted).

- [X] T005 [US2] **Build lane**. After T004: Create `web/src/features/app-navigation/appNavigation.ts` exposing shared `openMovie` and the single programmatic push/replace subscriber-notification mechanism (serving openMovie, navigate, replace, openMoviePlayer compatibility wrapper, and details exits). Author `web/src/features/app-navigation/appNavigation.test.ts` alongside the mechanism **before** cutover: exercise programmatic entry points; assert subscriber UI resync, no `PopStateEvent` dispatch, preserved history/state, exactly one update, subscribe→unsubscribe, unmount, StrictMode setup-cleanup-setup, and ordinary test isolation. Wire `useAppNavigation.ts` route-state subscription to the shared mechanism **before** any producer stops notifying its old consumers. Document/preserve per-entry-point return URL and playerIntent contracts (`recon-DEV-475` lines 20–23). No router/state library or new architectural layer (`brief.md` Q2/Q11; ADR 0042 proposed; FR-002–FR-004; PLAN_ADJUDICATION repair 2–3).
- [X] T006 [US2] **Build lane** — **atomic with T007**. After T005: Migrate **all** named synthetic producers together — `web/src/features/app-navigation/useAppNavigation.ts` (player open / `openMoviePlayer` path), `web/src/features/movie-details/MovieDetailsView.tsx`, `web/src/features/movie-details/RelatedMovies.tsx`, and every details exit including library return, actor-filter, delete, and 404 routes — off synthetic production popstate onto shared `openMovie` / notification in `appNavigation.ts`. Preserve each entry point's return URL / playerIntent contract. Retain `web/src/library/MovieCard.tsx` existing `openMoviePlayer` wrapper import unless a separate approved edit is necessary; add/extend fallback regression in `web/src/library/MovieCard.test.tsx` (`recon-DEV-475` lines 16–18). **T006 MUST land in the same commit/checkpoint as T007** (`brief.md` Q2; FR-002; FR-013).
- [X] T007 [US2] **Build lane** — **atomic with T006**. Same checkpoint as T006: Migrate **all** named subscribed consumers together — `web/src/App.tsx`, `useAppNavigation.ts` listeners, `web/src/features/library/FilterPanel.tsx`, `web/src/library/LibraryView.tsx` — onto the explicit notification/resync path; keep native popstate for Back/Forward only; ensure listener teardown, no duplicate history entries, and no duplicate route updates. Extend `appNavigation.test.ts`, `useAppNavigation.test.tsx`, and other plan-justified suites so programmatic path is covered **without** relying on synthetic production popstate; retain or adjust native-popstate simulations for Back/Forward only (`brief.md` Q2/Q7; FR-003–FR-004, FR-015).

**Checkpoint**: US2 independently testable; no half-migrated producer/listener set; T006+T007 accepted only together.

**Checkpoint receipt (T005–T007, HEAD `4cb36f11` + this commit)**:
- **Mechanism (T005).** New `web/src/features/app-navigation/appNavigation.ts` owns the module-level subscriber set, a monotonic `routeKey`, `subscribeToRouteChange` / `notifyRouteChange`, `navigate` / `replace` (push unless `replace` or same URL), `openMovie`, the `openMoviePlayer` compatibility wrapper, and the moved pure helpers (`validateLibraryReturnUrl`, `readReturnUrl`, `currentReturnUrl`, `readPlayerIntent`, `libraryReturnSearch`, `isLibraryRoute`). No `PopStateEvent` is constructed anywhere in `web/src` production code. The single native `popstate` listener lives in the module, bound on the first subscriber and unbound on the last, so native Back/Forward still resyncs through the same notification path. No router/state library, no new layer, no new dependency.
- **Subscription before cutover.** `useAppNavigation.ts` was reduced to React route state: it subscribes to the shared mechanism and re-exports the helpers, so every consumer keeps its existing import site. `useAppNavigation.test.tsx` (58 tests across the directory) was green before producers moved.
- **Producers migrated (T006).** `MovieDetailsView.tsx`: local `navigateTo` gone; More-with row and library exit use `navigate(libraryHref)`, 404 "Back to library" and post-delete exit use `navigate('/')`, More-with row opens via `openMovie(id, { returnUrl: readReturnUrl(...) })` with no `playerIntent`. `RelatedMovies.tsx`: `openRecommendedMovie` uses `openMovie` with the validated return URL only. `MovieCard.tsx` keeps its `openMoviePlayer` import unchanged and now reaches the shared notification through it.
- **Consumers migrated (T007).** `App.tsx`: shell search cancels pending debounce via `subscribeToRouteChange`; the shell `openMovie` callback delegates to the shared `openMovie` (pathname+search capture and optional `playerIntent` preserved). `FilterPanel.tsx` and `LibraryView.tsx`: their uncontrolled-mode `popstate` listeners became `subscribeToRouteChange`, so a programmatic navigation resyncs them exactly as native traversal does.
- **Per-entry-point contracts preserved.** Shell `openMovie` still captures the full current location without library-route validation (so `/for-you` round-trips) and adds `playerIntent` only when asked; `openMoviePlayer` still prefers the current library URL, falls back to the stored return URL, and always sets `playerIntent`; details/related exits still pass the validated return URL only. Existing assertions on `window.history.state` in `App.test.tsx`, `MovieDetailsView.test.tsx`, `RelatedMovies.test.tsx` and `MovieCard.test.tsx` are unchanged and pass.
- **Cutover proof added.** `appNavigation.test.ts` (20 tests): programmatic push/replace with zero `PopStateEvent` dispatches, one subscriber call, preserved state/history, replace-vs-push, refused history write, multi-subscriber, unsubscribe, monotonic route key, hook resync, native popstate for Back/Forward, unmount, StrictMode setup-cleanup-setup, isolation. `useAppNavigation.test.tsx` +2 (shared-notification resync with zero synthetic dispatch; no update after unmount). No-synthetic-popstate coverage added per entry point: `App.test.tsx` (library card open), `MovieDetailsView.test.tsx` (More-with row, 404 exit, actor-filter exit), `MovieDetailsView.delete.test.tsx` (delete exit), `RelatedMovies.test.tsx` (recommendation card), `MovieCard.test.tsx` (host-less player fallback), `LibraryView.composed.test.tsx` (uncontrolled resync from a programmatic notification), `FilterPanel.test.tsx` (pending debounce cancelled by a programmatic notification).
- **Gates.** `pwsh -NoProfile -File scripts/run-web-gates.ps1` → exit 0, `Web gates: PASS (lint, tsc, vitest, build)`; 66 files, 1019 tests pass (988 at the Phase 3 receipt). Native-popstate simulations retained only for Back/Forward traversal in `App.test.tsx`, `LibraryView.*`, `FilterPanel.test.tsx`, `browseWatchlist.test.tsx`, `watchlist.integration.test.tsx` and `useAppNavigation.test.tsx`. C# gates remain scope-empty SKIPPED. No Tailwind, jsx-a11y, `openapi.json`, `types.ts`, backend or LocalPlay change.

---

## Phase 5: User Story 3 - Cohesive component splits (Priority: P2)

**Goal**: Keep three orchestration shells; extract Q10/Q12-ratified cohesive siblings with ownership invariants.
**Independent Test**: Shells remain; siblings own cohesive sections; pure chip builder exported; DualRangeField owns ranges; FilterSortControls sort/direction only; behavioural suites green; no line-count acceptance bar.
**Order**: T008 → T009 → T010 (can be sequential; same-directory extractions may serialize if shared shells).

- [X] T008 [US3] **Build lane**. After T007: Extract cohesive sections from `web/src/features/library/FilterPanel.tsx` into `web/src/features/library/DualRangeField.tsx` (owns existing local range control from FilterPanel range UI), `FilterSearchField.tsx` (search draft/debounce/suggestions; **cancel pending debounce commits on native/programmatic navigation and unmount**; add regression for navigation within debounce window), `FilterFacetCombobox.tsx` (**each instance** owns draft/active-index/refs/keyboard nav; independent cast/collection/director keyboard tests), and `FilterSortControls.tsx` (**sort field and sort direction only** — no ranges). Keep FilterPanel as orchestration shell owning query mutation/synchronization, current-year snapshot, normalized facets, rating/runtime/release-year fieldset orchestration consuming DualRangeField, and public query/callback interface. Siblings receive navigation callbacks from the shell; do not introduce local history/popstate or duplicate URL effects. Update `FilterPanel.test.tsx` as needed (`brief.md` Q3/Q10; Q12; FR-005–FR-006).
- [X] T009 [US3] **Build lane**. After T008: Extract cohesive sections from `web/src/library/LibraryView.tsx` into `web/src/library/LibraryBrowseView.tsx`, `LibraryFilterSummary.tsx`, `LibraryResultsToolbar.tsx`, and `LibraryResultsBody.tsx`; keep LibraryView as orchestration shell owning URL/query synchronization, route effects, current query/page handlers, and currentYear; LibraryBrowseView owns view variant, panel visibility, focus signal, hidden ids, and grid focus/ref management; **`LibraryFilterSummary.tsx` MUST define and export a pure chip-construction helper** consumed by the summary, with focused pure-function unit coverage (ticket Wanted #4; Q12); siblings receive values/callbacks without duplicate URL/effect state or local history/popstate; update `LibraryView.composed.test.tsx` / `LibraryView.toolbar.test.tsx` as needed (`brief.md` Q3/Q10; Q12; FR-005).
- [X] T010 [US3] **Build lane**. After T009: Extract cohesive sections from `web/src/features/movie-details/MovieDetailsView.tsx` into `web/src/features/movie-details/MoviePoster.tsx`, `ManualMatchControl.tsx`, `CastSection.tsx`, `MoreWithPanel.tsx`, `MorePosterCard.tsx`, and `FileSection.tsx`; keep MovieDetailsView / MovieDetailsViewInner as orchestration shell owning route/loading/refresh, details client, local-play request/result/in-flight state, selected actor, and removal dialog/focus; siblings own image-failure, match-open/trigger focus, cast section, related presentation/card failure, and copy feedback; siblings receive navigation callbacks from the shell and do not introduce local history/popstate or duplicate URL effects; do not relocate local-play execution or change its feature gate; update `MovieDetailsView.test.tsx` as needed. No BrowserPlayer cleanup; no UI redesign; no line-count asserts (`brief.md` Q3/Q10; FR-005).

**Checkpoint**: US3 complete for all three shells.

**Checkpoint receipt (T008–T010, HEAD `addcd25c` + this commit)**:
- **FilterPanel shell (T008).** `FilterPanel.tsx` keeps query mutation/URL synchronization, the `currentYear` snapshot, normalized/sorted facets, the rating/runtime/release-year fieldsets (each consuming `DualRangeField`), Clear all, and the public query/callback interface. New siblings: `DualRangeField.tsx` (the existing two-thumb control, unchanged clamping and captions), `FilterSearchField.tsx` (draft, debounce, clear button), `FilterFacetCombobox.tsx` (one instance per facet, each owning its draft, active index and input ref, with combobox/suggestions/filter render modes), `FilterSortControls.tsx` (sort field chips and the direction toggle only). No sibling reads `window.history` or dispatches `popstate`; the shell keeps the single `subscribeToRouteChange` subscription and passes values/callbacks down.
- **Debounce cancel-on-navigation.** The search field drops a pending commit on unmount; the shell remounts it (`key` with the route key plus a resync epoch) from its `subscribeToRouteChange` resync and from Clear all, so a native traversal, a shared programmatic notification, and Clear all all cancel the pending commit. The facet drafts reset only on Clear all, preserving the previous behaviour of keeping a typed facet draft across a route resync.
- **LibraryView shell (T009).** `LibraryView.tsx` now owns only URL/query synchronization, the route subscription, `currentYear`, and the query/page/clear handlers; it renders `LibraryBrowseView` inside `LibraryErrorBoundary`. New siblings: `LibraryBrowseView.tsx` (view variant, panel visibility, hidden ids, focus signal, grid focus/ref management, facet queries and their presentation), `LibraryFilterSummary.tsx` (chip row plus the exported pure `buildFilterChips` / `hasDirector`), `LibraryResultsToolbar.tsx` (filters toggle, count, sort select, layout switch), `LibraryResultsBody.tsx` (loading/empty/populated presentations with pagination).
- **Pure chip builder.** `buildFilterChips(query, currentYear, names)` returns per-value descriptors with the exact pre-extraction order, labels and remove wording, and each descriptor carries the mutation that clears only its value; it is pure over its arguments and touches no React state, history or URL. Covered directly in `LibraryFilterSummary.test.tsx` (10 tests: empty query, purity/non-mutation, name resolution and id fallbacks, inactive extents omitted, per-endpoint ranges, rating formatting, repeated statuses with unique keys, include-unknown, single-value removal, `hasDirector`).
- **MovieDetailsView shell (T010).** `MovieDetailsView.tsx` keeps route/loading/refresh, the details client, `useDetailsEnrichmentBridge`, library status, local-play request/result/in-flight state with the unchanged `Features:LocalPlay` capability gate, the selected actor, and the removal dialog with its focus and Escape handling. New siblings: `MoviePoster.tsx` (image failure), `ManualMatchControl.tsx` (match-open state and trigger focus), `CastSection.tsx`, `MoreWithPanel.tsx` (related presentation, receiving `onOpenMovie` / `onNavigate` callbacks), `MorePosterCard.tsx` (card image failure, receiving `onOpen`), `FileSection.tsx` (copy feedback). `NoMetadata` stays feature-local in the shell because it owns enrichment-request state, not presentation.
- **Sibling regressions.** `DualRangeField.test.tsx` (extents/captions, both clamp directions, two sliders), `FilterFacetCombobox.test.tsx` (independent draft, independent listbox/active index, commits only the instance that received the event, Escape closes only its own draft, per-instance empty message, refocus on commit), `FilterSearchField.test.tsx` (single debounced commit, re-seed from committed text, pending commit dropped on remount/unmount, synchronous empty commit with refocus), `FilterSortControls.test.tsx` (sort/direction only, no sliders or checkboxes), `MovieDetailsSiblings.test.tsx` (poster and card image failure owned locally, card reports its id upward).
- **Behaviour parity.** No test file outside the new sibling suites changed; the pre-existing `FilterPanel.test.tsx` (80), `LibraryView.composed`/`toolbar`/`retry`, `MovieCard`, `FilterPanel` collection/director suites and the whole details directory pass unmodified apart from the added sibling files.
- **Gates.** `pwsh -NoProfile -File scripts/run-web-gates.ps1` → exit 0, `Web gates: PASS (lint, tsc, vitest, build)`; 72 files, 1052 tests pass (66 files, 1019 at the Phase 4 receipt). No `PopStateEvent` constructed in `web/src` production code, no history write in any sibling, no new dependency, no Tailwind, no jsx-a11y, no `openapi.json`/`types.ts`/backend/LocalPlay change.

---

## Phase 6: User Story 4 - Final web verification (Priority: P2)

**Goal**: Final lint, typecheck, unit, and e2e evidence on delivery head with SMOKE map.
**Independent Test**: All four lanes pass, or e2e Could not run blocks Done-when explicitly.
**Order**: T011.

- [X] T011 [US4] **Build lane** / Gauge as routed. After T010: Run final web verification from `web/` using **plan.md commands** for lint (`npm run lint`), typecheck (`tsc --noEmit` / `tsc --noEmit -p tsconfig.node.json` / `npm run typecheck:e2e` as in build prefix), unit (`npm test` or `npm run test:run`), and e2e (`npm run e2e`). Confirm targeted coverage for programmatic navigation without synthetic popstate, native Back/Forward, history/return URL/player intent, filter/sort/default/fallback, details/related navigation, MovieCard fallback, listener teardown, pure chip builder, and debounce cancel-on-nav. Map SMOKE-001 → named scenario(s) in `browse.e2e.ts`/`genre.e2e.ts` (or named extension); SMOKE-002 → `dev455-playback.e2e.ts` plus return-URL coverage if missing; SMOKE-003 → named Back/Forward scenario or extension (`recon-DEV-475` lines 25–27). Record exact commands, HEAD, exit codes, and distinct PASS/FAIL/SKIPPED/SKIP/N/A/Could not run verdicts. Unrunnable e2e ⇒ Could not run and blocks Done-when. Scope-empty C# gates remain SKIPPED never PASS. No Tailwind change. No jsx-a11y install. No behaviour-change regressions (`brief.md` Q7; FR-014–FR-016; SC-005).

**Checkpoint**: Web verification evidenced or explicitly blocked on Could not run e2e.

**Verification receipt (T011, HEAD `d73471a7` + these e2e additions)**:
- **Lint** `npm run lint` → exit 0. `check:api` pass, `eslint . --max-warnings 0` 0 findings, `prettier --check .` clean.
- **Typecheck** `tsc --noEmit` → exit 0; `tsc --noEmit -p tsconfig.node.json` → exit 0; `npm run typecheck:e2e` (`tsc --noEmit -p e2e/tsconfig.json`) → exit 0.
- **Unit** `npm run test:run` → exit 0. 72 files, 1052 tests pass (unchanged from the Phase 5 receipt; no production or unit test file changed in Phase 6).
- **E2E** `npm run e2e` → exit 0. 22 Playwright tests pass on the real Compose stack (`Features.LocalPlay=false` via `api.settings.json`), plus the stream acceptance pass recording 32 cases through nginx. Baseline was 19 tests; this phase adds 3.
- **Targeted coverage confirmed present and green.** Programmatic navigation without synthetic production popstate: `appNavigation.test.ts` plus the per-entry-point suites named in the Phase 4 receipt; production `web/src` still constructs no `PopStateEvent`. Native Back/Forward: `App.test.tsx`, `LibraryView.*`, `FilterPanel.test.tsx`, `useAppNavigation.test.tsx`, and now e2e SMOKE-003. History/return URL/player intent: `appNavigation.test.ts`, `useAppNavigation.test.tsx`, `MovieDetailsView.test.tsx`, `RelatedMovies.test.tsx`, `MovieCard.test.tsx`, and now e2e SMOKE-002. Filter/sort/default/fallback: `filterQuery.test.ts`, `browseParams`, `FilterPanel.test.tsx`, and now e2e SMOKE-001. Details/related navigation: `MovieDetailsView*`, `RelatedMovies.test.tsx`. MovieCard fallback: `MovieCard.test.tsx`. Listener teardown: `appNavigation.test.ts` (subscribe→unsubscribe, last-listener unbind, unmount, StrictMode) and `useAppNavigation.test.tsx` (no update after unmount). Pure chip builder: `LibraryFilterSummary.test.tsx` (10 direct tests). Debounce cancel-on-nav: `FilterSearchField.test.tsx` plus `FilterPanel.test.tsx` programmatic-notification case.
- **SMOKE map (named scenarios, extended where recon showed the coverage missing).**
  - **SMOKE-001** → `web/e2e/browse.e2e.ts` `SMOKE-001 (DEV-475) applied filter and sort survive a reload, and defaults plus invalid values are unchanged`. Applies a genre facet and a non-default sort, asserts the URL carries `genreIds`/`sort`/`direction`, the chip row names the genre, reload restores the same choices and results, and an invalid `collectionId=abc` falls back to the full default listing (`Title|Ascending`, no alert). `genre.e2e.ts` (existing) continues to cover the genre-filter path.
  - **SMOKE-002** → new `web/e2e/details-return-url.e2e.ts` `SMOKE-002 (DEV-475) in-app details exits restore the filtered library URL and player intent is kept per entry point`. Return-URL coverage was genuinely missing (per recon lines 25–27), so this is a named extension: from a filtered library it opens a card (no `playerIntent`), asserts the back link's `href`/label carry the filter, returns to the exact filtered URL, then uses card Play to assert `playerIntent=true` with `libraryReturnUrl` preserved and the back link restoring the same filtered URL. `dev455-playback.e2e.ts` (existing) continues to cover the player entry itself.
  - **SMOKE-003** → `web/e2e/browse.e2e.ts` `SMOKE-003 (DEV-475) Back and Forward resync the library and details views without duplicate history entries`. This satisfies the named Back/Forward extension the spec required. It asserts one history entry per open, `goBack` resyncs the library to the filtered state with the same `history.length` (no duplicate push), and `goForward` returns to details with `history.length` still unchanged.
- **Verdicts**: lint PASS, typecheck PASS, unit PASS, e2e PASS. Scope-empty C# gates (`run-roslyn-analyzers`, complexity, InspectCode) remain **SKIPPED**, never PASS. No SKIP or N/A lane. Nothing is Could not run.
- **Phase 6 diff**: only `web/e2e/browse.e2e.ts` (extended) and `web/e2e/details-return-url.e2e.ts` (new) — test-only, no production change. No `openapi.json`/`types.ts`/backend/LocalPlay/Tailwind/jsx-a11y change, no new dependency.
- **Note on cost**: two intermediate e2e runs failed on test-authoring mistakes in the new specs (an inverted default-direction assertion, a link-name locator that matched both the nav tab and the back link, and an icon-only button asserted for text). Each was corrected in the test; no production code was touched. Third run is the 22/22 PASS above.

---

## Phase 7: Review / remediation (Q8 closing bar) — pipeline-only

**Purpose**: Capped three-axis implementation review under the frozen closing bar.
**Checkpoint**: Zero unresolved in-scope Critical/High/Medium/Low or unresolved items explicitly left for user review at the 2×2 cap — never auto-cleared.

- [ ] T012 **[pipeline-only]** After T011: Conduct capped implementation review/remediation (at most two rounds and two fix commits per round) requiring Sentry/Risk, Ledger/Standards, and Compass/Spec reports pinned to the reviewed head and posted on the PR. Closing bar: every in-scope Critical/High/Medium/Low fixed or explicitly rejected with cited evidence; no silent Low waiver; out-of-scope items → follow-up. Unresolved blockers at the cap remain open for Conductor/Patron disposition — never round three by default. After any remediation commit, refresh T011 web verification on final HEAD before packaging (`brief.md` Q8; FR-017–FR-018).

---

## Phase 8: Delivery handoff — pipeline-only

**Purpose**: Prepare merge-bar evidence packaging; only the user merges.
**Checkpoint**: No claim of merge clearance by agents. Final-head freshness required. ADR 0042 remains proposed until accepted.

- [ ] T013 **[pipeline-only]** After T012: Publish exact-head merge-bar evidence package for DEV-475 (web verification, three-axis review, verified follow-up status DEV-511 Tailwind and DEV-512 jsx-a11y) using only receipts that match the **final delivery/review HEAD**. Leave merge to the user. Do not merge or enable auto-merge. Do not claim PLAN FROZEN, Gate 1, ADR acceptance, or Done-when from packaging alone. Only Keel declares PLAN FROZEN after independent reread (`brief.md` Q8–Q11; SC-006).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)** [pipeline-only]: Spec PR + Gate 1 merge record blocks implementation lane (T002+)
- **Phase 1 (T001)** [pipeline-only]: Bounded evidence integration / citations block inventing freeze facts and block PLAN FROZEN
- **Phase 2 (T002)**: Baseline/characterization after T000+T001
- **US1 (Phase 3)**: After T002 — orphan/type cleanup (Q12 helper)
- **US2 (Phase 4)**: After US1 — navigation; T005 then atomic T006+T007
- **US3 (Phase 5)**: After US2 — extractions using Q10/Q12 ownership
- **US4 (Phase 6)**: After US3 — final web verification
- **Review (Phase 7 T012)** [pipeline-only]: After T011
- **Delivery (Phase 8 T013)** [pipeline-only]: After review cap with final-head freshness

### User Story Dependencies

- **US1 (P1)**: After baseline — dead code + typing (Q12 GetPeople helper)
- **US2 (P1)**: After US1 — shared navigation MVP seam (atomic cutover)
- **US3 (P2)**: After US2 — cohesive splits (Q12 chip/range ownership)
- **US4 (P2)**: After US3 — verification + review packaging (jsx-a11y deferred to DEV-512)

### Parallel Opportunities

- No parallel half-migration of navigation producers vs listeners (T006+T007 are one atomic commit/checkpoint)
- Same-file shell extractions are not `[P]`
- T008–T010 may be sequential to avoid thrashing shared navigation integration
- Inventory T001 is single-threaded evidence integration
- Pipeline-only tasks are never parallelised with Build production edits

---

## Implementation Strategy

### MVP First (Navigation without fake popstate)

1. Complete T000–T002: Gate 1 + inventories + baseline/characterization
2. Complete US1: orphan/type cleanup (Q12 typed helper)
3. Complete US2: T005 mechanism+tests, then atomic T006+T007 cutover
4. **STOP and VALIDATE**: Navigation unit coverage for both sources; then continue splits → final web verification

### Incremental Delivery

1. Inventories / citations → enable plan freeze facts
2. Orphan/type cleanup → safer surface + GetPeople cast removal
3. Shared navigation → ticket core behavioural fix
4. Extractions → maintainability without redesign (pure chip builder; DualRangeField ownership)
5. Final web lanes + capped review → awaiting user merge
6. jsx-a11y → follow-up DEV-512 (not this delivery)

### Parallel Team Strategy

Single Build lane preferred for shared navigation migration and same-shell extractions; do not invent named seats for otherwise-unassigned tasks.

---

## Notes

- Tailwind adopt-or-remove is **not** a task here; verified follow-up **DEV-511** (`brief.md` Q1).
- jsx-a11y adoption is **not** a task here; verified follow-up **DEV-512** (`brief.md` Q10).
- Concrete sibling paths and Q12 ownership in T005/T008–T010 are settled — not placeholders or conditional wording.
- T000/T001/T012/T013 are pipeline-only.
- ADR 0042 is proposed by Keel; Quill does not author ADRs; packaging does not accept the ADR.
- Anything else is a follow-up issue, not a finding in this round.
