# Feature Specification: SPA cleanup (dead exports, navigation, cohesive splits)

**Feature Branch**: `feature/475-spec`
**Created**: 2026-11-14
**Status**: `gate1: provisional` — grill closed (Q1–Q9), updated by Patron Q10–Q14. Independent analyze reread CLEAN (2026-11-14 20:00). Plan-challenge adjudication closed and **PLAN FROZEN** declared by Keel (`brief.md` lines 3, 202–211). Gate 1 remains closed pending the unanswered Q13 structural owner checkbox for retaining runtime `role=actor` on `GET /people`, plus ordinary owner Gate 1 merge of the spec PR. Implementation, runtime acceptance, review closure, and merge remain distinct and open.
**Input**: Ticket DEV-475 (size L, parent DEV-490; authoritative Scope/Wanted/Done-when restored in `recon-DEV-475` lines 39–58); grill-closed `brief.md` (Q1–Q13); `CONCLUSIONS.md` including Q12 and Q13 (Q13 commit `b3b70c15`); `ASSUMPTIONS.md`; proposed `docs/adr/0042-explicit-spa-navigation-and-cleanup-boundaries.md`; Maestri notes `DEV-475`, `recon-DEV-475`; `specs/PRODUCT.md`; `PLAN_ADJUDICATION.md`.

## Owner question — Gate 1

**Structural owner checkbox required (Q13; unanswered — Gate 1 stays closed until checked):** Wanted #3 says "Remove role param and as unknown as casts" from Problem text claiming the endpoint takes no parameters (`recon-DEV-475` lines 47, 53), but backend `FacetEndpoints.cs:83–104` (`EnsureActorRole`) requires `role=actor` on `GET /people` and returns HTTP 422 when omitted (`recon-DEV-475` lines 8–10). Authorize amending Wanted #3 to retain the runtime `role=actor` query parameter via a single shared literal-typed helper while still removing the inline `as unknown as` casts at `LibraryView.tsx:307` and `movieDetailsQueries.ts:41` (preserving Done-when "no behaviour changes" in `recon-DEV-475` line 58 without modifying OpenAPI/backend contracts in this SPA cleanup ticket)? (`CONCLUSIONS.md` Q13; `PRODUCT.md` §3/§5; Charter §2.3(a).)

Q12 technical design (retain `role=actor` via typed helper; remove casts; no OpenAPI/backend edits; chip builder; DualRangeField/sort ownership) remains intact pending that checkbox and is not owner approval by itself. Ordinary owner Gate 1 on the spec PR remains pending after Bernstein coordination / Rigger preparation-opening of that PR and verified user merge. Spec PR body MUST carry this structural checkbox verbatim, the ticket Done-when split permission (`recon-DEV-475` line 58), and dispositions for verified follow-ups DEV-511 (Tailwind) and DEV-512 (jsx-a11y). Patron approval is not owner approval. Only Keel declares PLAN FROZEN after independent reread. Quill does not invent PLAN FROZEN or treat the checkbox as approved.

## Clarifications

### Session 2026-11-14

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical inventable ambiguity for drafting? → A: Delivery boundary, feature-local imperative navigation with shared openMovie and push/replace subscriber path (native popstate for Back/Forward only), orchestration-shell extraction boundary, deletion evidence rule, role/cast and sort/direction compatibility, verification order, closing bar/2×2 caps, and ADR 0042 outline were frozen in brief Q1–Q9. **Q10** supersedes Q1/Q6 on in-delivery jsx-a11y (defer to follow-up DEV-512) and ratifies sibling modules/ownership. **Q11** settles `web/src/features/app-navigation/appNavigation.ts`. **Q12** reconciles GetPeople `role=actor` retention via typed helper (remove inline casts; no OpenAPI/generated edits), requires an exported pure chip-building helper from `LibraryFilterSummary.tsx`, and settles DualRangeField vs FilterSortControls ownership. Tailwind follow-up DEV-511 and jsx-a11y follow-up DEV-512 are verified. Current recon citations: GetPeople lines 8–10; complete `filterQuery` importer set lines 12–14; MovieCard lines 16–18; return-URL/playerIntent lines 20–23; smoke/e2e candidates lines 25–27; ticket authority lines 39–58. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## Scope

**In scope** (`brief.md` Frozen delivery scope; Q1–Q5, Q7–Q12; ticket Wanted/Done when in `recon-DEV-475` lines 50–58):

1. **Dead aliases / exports**: Remove named `filterQuery.ts` aliases/shim and other proven unreferenced exports after the bounded symbol/reference inventory. Complete lexical importer set at pinned source (`recon-DEV-475` lines 12–14): `App.tsx`, `features/library/FilterPanel.tsx`, `FilterPanel.test.tsx`, `filterQuery.test.ts`, `features/movie-details/movieDetailsLabels.ts`, `library/LibraryView.tsx`, `library/browseParams.ts`. Preserve canonical live `parseFilterQuery` / `toUrlQuery` / `formatRatingBound` behaviour, runtime validation, and tests; do not replace removed aliases. Ticket-named `libraryUrl.ts` + test deletion remains required after evidence (`recon-DEV-475` lines 51, 61–66). Importer presence is not authorization to edit every importer.
2. **Named orphan deletion**: Delete `web/src/library/libraryUrl.ts` and `web/src/library/libraryUrl.test.ts` after the reference inventory confirms consumers. Whole-file deletion beyond this ticket-named pair requires a separate cited Patron ruling.
3. **Shared navigation without synthetic popstate**: Replace synthetic production popstate producers with feature-local `web/src/features/app-navigation/appNavigation.ts` (Q11) beside `useAppNavigation`, exposing shared `openMovie` and **one** programmatic push/replace subscriber-notification path serving `openMovie`, `navigate`, `replace`, the existing `openMoviePlayer` compatibility wrapper, and every details exit (library return, actor-filter, delete, 404). `useAppNavigation.ts` retains React route-state subscription and native Back/Forward popstate; that subscription MUST be wired to the shared mechanism before any producer stops notifying old consumers. Native `popstate` remains solely for browser Back/Forward. Preserve per-entry-point return URL and playerIntent contracts (`recon-DEV-475` lines 20–23) without unifying them into one policy. Retain `MovieCard`'s existing `openMoviePlayer` wrapper import (`recon-DEV-475` lines 16–18) unless a separate approved edit is necessary. No new router dependency, global state library, or architectural layer.
4. **Role / cast / literal typing (Q12 reconciles Q5)**: Retain the runtime `role=actor` query parameter for `GET /api/people` via a shared literal-typed helper; remove the inline `as unknown as Record<string, unknown>` casts at `LibraryView.tsx` and `movieDetailsQueries.ts` (`recon-DEV-475` lines 8–10; Q12). Do **not** require nonexistent generated role query types; do **not** alter `openapi.json`, `types.ts`, or backend endpoints. Literal-type existing sort/direction choices. Preserve allowed choices, defaults, URL parsing/serialization, invalid-value fallback, filtering, and request shapes. Runtime parsing still validates untrusted URL values.
5. **Component splits**: Retain `FilterPanel.tsx`, `LibraryView.tsx`, and `MovieDetailsView.tsx` as orchestration shells; extract Q10-ratified cohesive sibling modules. **Q12**: `DualRangeField.tsx` owns the existing local range control; `FilterSortControls.tsx` owns only sort field/direction; `LibraryFilterSummary.tsx` MUST define and export a pure chip-building helper consumed by the summary (with direct unit coverage). One state/effect owner; explicit typed props; generated API types where they exist. No line-count target, forwarding-only wrapper, UI redesign, or BrowserPlayer cleanup. Historical component line counts in recon are dated snapshots, not acceptance thresholds.
6. **Verification**: Existing web lint, typecheck, unit, and e2e all pass with no behaviour changes; targeted coverage for programmatic navigation without synthetic popstate, native Back/Forward resync, history/return URL/player intent, library filter/sort/default/fallback, details/related navigation, and listener teardown. Commands from `web/package.json` scripts (see plan/tasks). Map SMOKE-001/002/003 to named e2e candidates (`recon-DEV-475` lines 25–27) or named extensions — inventory alone is insufficient. Unrunnable e2e is Could not run and blocks Done-when. Scope-empty C# gate skips remain SKIPPED and never PASS.
7. **Loop discipline**: Zero unresolved in-scope Critical/High/Medium/Low (fix or explicitly reject with cited evidence; no silent Low waiver). Size L requires Sentry/Risk, Ledger/Standards, and Compass/Spec reports pinned to reviewed HEAD and posted on the PR. Max 2 review rounds and 2 fix commits per round; exhaustion with blockers stops blocked. Frozen scope ends: anything else is a follow-up issue, not a finding in this round.

**Out of scope**:

- Tailwind adopt-or-remove — deferred under Done-when split permission (`recon-DEV-475` line 58); verified follow-up **DEV-511**; Tailwind unchanged here (`brief.md` Q1; Q10 sync).
- `eslint-plugin-jsx-a11y` adoption, `@eslint/compat`, peer overrides, ESLint downgrade, and recommended-rule violation inventory/fixes — deferred by Patron Q10 to verified follow-up **DEV-512** under DEV-490. No plugin install in this delivery.
- BrowserPlayer cleanup, UI redesign, new router/state/service layers, additional dependencies without ruling, backend/schema/API/generated-contract changes, local playback execution, and unrelated whole-file rewrites.
- Claiming PLAN FROZEN, Gate 1, implementation acceptance, runtime PASS, or merge clearance from this Phase A draft. ADR 0042 remains **proposed** until actual acceptance is evidenced — packaging cannot manufacture acceptance.

**Precedence**: Ticket Wanted/Done when (`recon-DEV-475` lines 50–58; YouTrack DEV-475 via `scripts/get-task.ps1`) is already approved authority for delivery outcomes (`PRODUCT.md` §3), except where Q13 escalates a ticket-text contradiction to the unanswered structural owner checkbox above. Brief Q1–Q11 and `CONCLUSIONS.md` Q1–Q13 govern boundaries (Q10 supersedes Q1/Q6 on jsx-a11y; Q12 states the GetPeople technical design; Q13 requires the owner checkbox authorizing retained runtime `role=actor` before that contradiction is owner-approved). Cite brief/CONCLUSIONS for rulings; cite current `recon-DEV-475` for facts. Ordinary owner Gate 1 and the Q13 structural checkbox both remain pending. Only Keel declares PLAN FROZEN after independent reread. Do not claim PLAN FROZEN or checkbox approval from this draft.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Synthetic production popstate**
A fabricated browser `popstate` event dispatched by application code to force UI resynchronization after programmatic history changes.
_Avoid_: native Back/Forward traversal events; test-only simulated popstate used to emulate native traversal

**Shared openMovie**
The single feature-local imperative entry used by details, RelatedMovies, and the existing player flow to open a movie through the shared history-and-notification mechanism.
_Avoid_: per-call-site history hacks; duplicate history entries

**Programmatic push/replace notification**
Explicit subscriber notification after programmatic history push/replace, sharing the same internal resync path without fabricating native events or leaking listeners.
_Avoid_: fake native events; silent history mutations that leave subscribed views stale

**Orchestration shell**
The retained FilterPanel / LibraryView / MovieDetailsView module that owns coordination while cohesive sections live in sibling modules under the same directory.
_Avoid_: line-count targets; prop-forwarding-only wrappers; new generic services layers

**Evidence-led orphan removal**
Deletion of ticket-named or unreferenced exports only after a bounded reference inventory (including barrels, tests, and dynamic references) confirms current consumers.
_Avoid_: own-test-only as complete all-configuration proof; unruled whole-file deletions beyond the named pair

**Typed GetPeople role helper**
A shared literal-typed helper that supplies the required `{ role: 'actor' }` query option for `GET /api/people` without inline `as unknown as` casts and without editing OpenAPI/generated types (Q12).
_Avoid_: dropping `role=actor`; inventing generated role query types; OpenAPI edits

Consumed vocabulary (already in use; not redefined): Library, FilterPanel, Movie details, Related movies, browser Back/Forward, generated API types, OpenAPI→TypeScript contract chain.

## What the User Will See *(mandatory)*

Nothing visible as a new product feature: this delivery is SPA cleanup with **no intentional behaviour change**. Library filters/sort defaults/fallback, movie details, related-movie navigation, player intent/return URLs, and browser Back/Forward continue to work as before.

- **Where**: Library browse / filter surfaces.
- **What the user sees or does**: Same filter, sort, and URL-driven library behaviour; no new controls or removed choices from this cleanup.
- **What it needs to show up**: Default Compose stack with Api + web; library content available.

- **Where**: Movie details, related movies, and in-app movie/player navigation; browser Back/Forward.
- **What the user sees or does**: Opening a movie from details/related/player paths and using browser Back/Forward continue to show the same routes, return destinations, and player intent as before — without relying on fabricated popstate for programmatic navigation.
- **What it needs to show up**: Same Compose stack; at least one navigable movie.

### Smoke Test *(mandatory when something is visible — behavioural parity)*

- **SMOKE-001**: **Given** the library with filters available, **When** the user applies filter/sort choices and reloads or shares the URL state, **Then** the same allowed choices, defaults, and invalid-value fallback behaviour appear as before this cleanup. Candidate e2e homes (not verified sufficiency): `web/e2e/browse.e2e.ts`, `web/e2e/genre.e2e.ts` (`recon-DEV-475` lines 25–27).
- **SMOKE-002**: **Given** movie details (and related movies when present), **When** the user opens a movie and returns via in-app navigation, **Then** route, return URL, and player intent behaviour match pre-cleanup expectations. Candidate e2e homes: `web/e2e/dev455-playback.e2e.ts` (player entry); `web/e2e/library-path.e2e.ts` covers a library/details field but not return navigation — extend or name a scenario if return-URL coverage remains missing (`recon-DEV-475` lines 25–27).
- **SMOKE-003**: **Given** in-app navigation history, **When** the user uses browser Back/Forward, **Then** the SPA resynchronizes to the expected view without duplicate history entries or stuck listeners. No named e2e scenario for Back/Forward route resynchronization was found in bounded scan — extend an existing suite or add a named scenario before Done-when (`recon-DEV-475` lines 25–27).

Existing e2e traversal/library/details/player smoke coverage must run or be extended where missing (`brief.md` Q7). Exact e2e paths/commands are listed in plan/tasks. Missing/unrunnable e2e ⇒ **Could not run**, never PASS. The dated e2e inventory is a snapshot, not an acceptance threshold by itself.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Evidence-led orphan and type cleanup (Priority: P1)

Maintainers remove proven dead aliases/exports and the ticket-named `libraryUrl` pair after inventory, remove inline GetPeople role casts via a typed helper that retains `role=actor`, and literal-type sort/direction on the live library path without changing user-visible filter behaviour.

**Why this priority**: Ticket Wanted leads with dead exports / `libraryUrl` deletion and role/cast/literal typing; Q4–Q5 freeze the evidence and compatibility rules; Q12 reconciles GetPeople request-shape preservation with the ticket wording.

**Independent Test**: Reference inventory recorded; named deletions and alias removals land only with evidence; canonical `filterQuery` tests still pass; GetPeople still sends `role=actor` without inline casts; library filter/sort/default/fallback behaviour unchanged.

**Acceptance Scenarios**:

1. **Given** a bounded reference/export inventory including barrels, tests, and dynamic references (complete `filterQuery` importer set `recon-DEV-475` lines 12–14), **When** `libraryUrl.ts` / `libraryUrl.test.ts` and named filterQuery aliases/shim (plus other proven unreferenced exports) are removed, **Then** no live production importer remains for deleted symbols and canonical live filtering tests still pass.
2. **Given** uncertainty or a whole-file deletion beyond the ticket-named pair, **When** deletion is considered, **Then** work stops for recon or a separate cited Patron ruling — no speculative deletion.
3. **Given** the live library and details GetPeople callers, **When** inline `as unknown as` casts are removed, **Then** a shared literal-typed helper still supplies `role=actor`, request shapes remain unchanged, OpenAPI/generated files are untouched, and sort/direction are literal-typed to existing allowed values with runtime URL validation/fallback preserved (Q12; `recon-DEV-475` lines 8–10).

---

### User Story 2 - Shared imperative navigation without fake popstate (Priority: P1)

Maintainers replace the synthetic production popstate producers with shared `openMovie` via `appNavigation.ts` and a feature-local programmatic push/replace notification path; producers and listeners migrate in one atomic cutover; native popstate remains Back/Forward only.

**Why this priority**: Ticket Wanted requires shared openMovie and no synthetic popstate; Q2/Q11 freeze ownership and filename; plan adjudication requires atomic migration (FR-013).

**Independent Test**: `appNavigation.test.ts` authored alongside the mechanism before cutover; programmatic navigation updates subscribed views without dispatching synthetic popstate; native Back/Forward still resyncs; per-entry-point history/return URL/player intent preserved; listeners tear down cleanly; MovieCard fallback continues via existing wrapper.

**Acceptance Scenarios**:

1. **Given** movie details, RelatedMovies, the existing player open flow, and MovieCard's `openMoviePlayer` wrapper, **When** a movie is opened programmatically, **Then** navigation uses shared `openMovie` / the shared history-notification mechanism and does **not** dispatch synthetic production popstate.
2. **Given** programmatic push/replace (including library return, actor-filter, delete, and 404 exits), **When** subscribed consumers resync, **Then** they share the explicit notification/resync path with exactly one update, subscribe returning unsubscribe, no duplicate history entries, and no leaked listeners; `useAppNavigation` subscription is wired before producers stop notifying old consumers.
3. **Given** browser Back/Forward, **When** native popstate fires, **Then** the SPA resynchronizes as before; tests may still simulate native popstate for traversal, but that does not substitute for covering the imperative path through actual production entry points.

---

### User Story 3 - Cohesive component splits (Priority: P2)

Maintainers keep the three large components as orchestration shells and extract Q10/Q12-ratified cohesive sibling modules under existing directories without redesign or new layers.

**Why this priority**: Ticket Wanted requires the three splits and chip building as a pure function; Q3/Q10 freeze shells + concrete siblings; Q12 settles range/chip ownership.

**Independent Test**: Shells remain; siblings own cohesive sections with explicit props; pure chip builder exported from `LibraryFilterSummary` with unit coverage; DualRangeField owns ranges; FilterSortControls owns only sort/direction; debounce cancels on navigation/unmount; facet instances are independent; existing behavioural coverage for library/details continues to pass; no line-count assertion is used as acceptance.

**Acceptance Scenarios**:

1. **Given** FilterPanel, LibraryView, and MovieDetailsView, **When** cohesive sections are extracted into the Q10-ratified siblings, **Then** each shell remains the orchestration owner; `DualRangeField.tsx` owns the existing range control; `FilterSortControls.tsx` owns only sort/direction; siblings receive navigation callbacks from shells and do not introduce local history/popstate or duplicate URL effects (Q12).
2. **Given** extraction, **When** behaviour is exercised, **Then** rendered choices and interactions match pre-extraction behaviour; `LibraryFilterSummary.tsx` exports a pure chip-construction helper with focused pure-function regression; FilterSearchField cancels pending debounce commits on native/programmatic navigation and unmount; each FilterFacetCombobox instance owns its draft/index/refs with independent keyboard regressions (Q12; FR-005).
3. **Given** plan/tasks enumeration, **When** concrete sibling names are listed, **Then** they match Patron Q10 ratification and Q12 ownership rulings (not drafting invention). Historical line counts remain dated snapshots only.

---

### User Story 4 - Web verification and capped review (Priority: P2)

Maintainers complete the ordered delivery steps through final web verification and capped three-axis review without claiming merge clearance.

**Why this priority**: Done when requires lint, typecheck, unit, and e2e with no behaviour changes; Q7–Q8 freeze evidence and closing bar; Q10 removes the in-delivery jsx-a11y step.

**Independent Test**: All four web lanes pass (or e2e Could not run blocks Done-when); three-axis reports pinned to reviewed HEAD; work stops at awaiting-merge for the user.

**Acceptance Scenarios**:

1. **Given** the cleanup changes, **When** existing web lint, typecheck, unit, and e2e run, **Then** all pass with targeted navigation/filter/details coverage exercised or extended if missing; SMOKE-001/002/003 map to named e2e scenarios or named extensions; unrunnable e2e is Could not run and blocks Done-when.
2. **Given** Size L, **When** implementation review runs, **Then** Sentry, Ledger, and Compass reports are present at the reviewed head; zero unresolved in-scope Critical/High/Medium/Low remain (fixed or explicitly rejected with evidence); max 2 rounds × 2 fix commits.
3. **Given** delivery packaging complete, **When** agents finish, **Then** work stops at awaiting-merge — never merge or auto-merge; agents do not declare PLAN FROZEN or Gate 1; ADR 0042 proposed status is not treated as accepted.

### Edge Cases

- Own-test-only references support orphan removal investigation but do not prove deadness across all configurations (`brief.md` Q4).
- Lexical importer inventories are bounded evidence, not authorization to edit every importer (`recon-DEV-475` lines 12–14).
- Uncertainty on references → recon, not speculative deletion.
- Whole-file deletion beyond `libraryUrl.ts` + `libraryUrl.test.ts` → separate cited Patron ruling.
- Test-dispatched popstate may simulate native traversal but cannot prove the new imperative path (`brief.md` Q7).
- No replacement aliases or replacement tests for intentionally removed orphan code.
- No arbitrary file-length assertions as acceptance; historical recon line counts are non-comparable snapshots.
- Tailwind remains unchanged; follow-up DEV-511 verified.
- jsx-a11y remains unchanged in this delivery; follow-up DEV-512 verified; unavailable violation inventory is not a DEV-475 freeze prerequisite under Q10.
- GetPeople OpenAPI schema omits `role` while backend requires it — retain runtime query via typed helper; OpenAPI drift noted, no ticket (`recon-DEV-475` lines 8–10; Q12).
- Per-entry-point return URL / playerIntent behaviours differ (App shell, openMoviePlayer, RelatedMovies/details) and MUST NOT be collapsed (`recon-DEV-475` lines 20–23).
- SKIPPED/SKIP/N/A never become PASS; Could not run e2e blocks Done-when.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Delivery MUST include ticket-named cleanups (dead aliases/exports and `libraryUrl` pair deletion, shared openMovie without synthetic popstate, role-cast cleanup + sort/direction literal-typing, three cohesive component splits including pure chip builder), with Tailwind adopt-or-remove deferred to verified follow-up DEV-511 and jsx-a11y adoption deferred to verified follow-up DEV-512; Tailwind and jsx-a11y lint configuration remain unchanged in this delivery (`brief.md` Q1; Q10; ticket Wanted `recon-DEV-475` lines 50–56).
- **FR-002**: Programmatic movie navigation MUST use `web/src/features/app-navigation/appNavigation.ts` exposing shared `openMovie` and a single programmatic push/replace subscriber-notification path serving openMovie, navigate, replace, the existing openMoviePlayer compatibility wrapper, and every details exit (library/actor/delete/404); `useAppNavigation.ts` MUST retain React route-state subscription and native Back/Forward popstate and MUST subscribe to the shared mechanism before producers stop notifying old consumers; production code MUST NOT dispatch synthetic popstate for programmatic navigation; subscribe MUST return unsubscribe; no duplicate route updates/history entries (`brief.md` Q2/Q11; Q7; PLAN_ADJUDICATION repair 2).
- **FR-003**: Native `popstate` MUST remain the mechanism for browser Back/Forward resynchronization only (`brief.md` Q2).
- **FR-004**: Existing route parsing, return URLs, player intent, and library filter URL semantics MUST be preserved, including the distinct per-entry-point contracts documented in `recon-DEV-475` lines 20–23 (`brief.md` Q2/Q5/Q7).
- **FR-005**: `FilterPanel.tsx`, `LibraryView.tsx`, and `MovieDetailsView.tsx` MUST remain orchestration shells with cohesive extractions into the Q10-ratified sibling modules; `DualRangeField.tsx` MUST own the existing range control; `FilterSortControls.tsx` MUST own only sort field/direction; `LibraryFilterSummary.tsx` MUST define and export a pure chip-construction helper with focused unit coverage; FilterSearchField MUST cancel pending debounce commits on native/programmatic navigation and unmount; each FilterFacetCombobox instance MUST own its draft/index/refs; extracted siblings MUST receive navigation callbacks from shells and MUST NOT introduce local history/popstate or duplicate URL effects; no line-count target, forwarding-only wrapper, UI redesign, or BrowserPlayer cleanup (`brief.md` Q3/Q10; Q12).
- **FR-006**: Bounded edits to `App.tsx`, `useAppNavigation.ts`, `RelatedMovies.tsx`, `filterQuery.ts`, and associated tests MUST stay limited to the named cleanup and navigation integration (`brief.md` Q3).
- **FR-007**: Dead-code deletion MUST be governed by bounded reference/export inventory (barrels, tests, dynamic references) for the ticket-named `libraryUrl` pair, filterQuery alias/shim exports, and other unreferenced exports, using the complete confirmed `filterQuery` importer set (`recon-DEV-475` lines 12–14); canonical live filterQuery behaviour/tests (`parseFilterQuery` / `toUrlQuery` / `formatRatingBound` and runtime validation) MUST be preserved with no replacement aliases (`brief.md` Q4).
- **FR-008**: Whole-file deletion beyond the ticket-named `libraryUrl` pair MUST NOT proceed without a separate cited Patron ruling; uncertainty MUST route to recon (`brief.md` Q4).
- **FR-009**: Inline `as unknown as Record<string, unknown>` casts at the GetPeople callers MUST be removed; the runtime `role=actor` query MUST be retained via a shared literal-typed helper; OpenAPI/generated files and backend endpoints MUST NOT change; nonexistent generated role query types MUST NOT be required as a task outcome; sort/direction MUST be literal-typed to existing allowed values while preserving runtime URL validation, defaults, invalid-value fallback, and request shapes (`CONCLUSIONS.md` Q12 reconciling Q5; `recon-DEV-475` lines 8–10).
- **FR-010**: No hand-rolled API DTO duplicates and no OpenAPI/generated-contract changes are preapproved (`brief.md` Q5; `PRODUCT.md` contract chain; Q12).
- **FR-011**: This delivery MUST NOT add `eslint-plugin-jsx-a11y`, `@eslint/compat`, peer overrides, or an ESLint downgrade; jsx-a11y adoption and inventoried semantic fixes are deferred to DEV-512 (`brief.md` Q10).
- **FR-012**: Existing lint gates MUST be preserved without blanket disables, severity reductions, or broad exclusions (`brief.md` Approach invariant 6).
- **FR-013**: Task ordering MUST follow: inventories/baseline → regression characterization where insufficient → orphan/alias and type cleanup → shared navigation with all consumers migrated together in one atomic cutover → three cohesive extractions → final web verification. The original Q7 jsx-a11y step is deferred by Q10 (`brief.md` Ordered task plan; Q10; PLAN_ADJUDICATION repair 2).
- **FR-014**: Required delivery evidence MUST include existing web lint, typecheck, unit, and e2e all passing, with targeted coverage for programmatic navigation without synthetic popstate, native Back/Forward, history/return URL/player intent, filter/sort/default/fallback, details/related navigation, and listener teardown (`brief.md` Q7).
- **FR-015**: No new test framework and no arbitrary line-count assertions are allowed; test-simulated native popstate MUST NOT substitute for imperative-path coverage through actual programmatic entry points; unit suites MUST assert no production `PopStateEvent` dispatch on the imperative path (`brief.md` Q7).
- **FR-016**: Unrunnable e2e MUST be reported as Could not run and MUST block Done-when; scope-empty C# SKIPPED/SKIP/N/A MUST never be folded into PASS (`brief.md` Q7).
- **FR-017**: Closing bar MUST require zero unresolved in-scope Critical/High/Medium/Low (fixed or explicitly rejected with cited evidence; no silent Low waiver), all three challenger reports on Size L pinned to reviewed HEAD and posted on the PR, max 2 review rounds and 2 fix commits per round (`brief.md` Q8).
- **FR-018**: Anything outside frozen scope is a follow-up issue, not a finding in this round; Q7 gates MUST be satisfied independently of review dispositions; agents MUST NOT merge or enable auto-merge (`brief.md` Q8).
- **FR-019**: Proposed ADR 0042 records the navigation/cleanup boundaries including Q10–Q11; ADR compliance remains pending actual acceptance (not unqualified PASS from packaging); no CONTEXT.md glossary changes are authorized (`brief.md` Q9–Q11; PLAN_ADJUDICATION repair 7).
- **FR-020**: Q10-ratified sibling paths, Q11 `appNavigation.ts`, Q12 role/chip/range rulings, current `recon-DEV-475` evidence citations, verified follow-ups DEV-511 and DEV-512, and `web/package.json` command/script receipts MUST be reflected in plan/tasks — plugin-version and a11y violation inventories are not DEV-475 freeze prerequisites under Q10 (`brief.md` Handoff; Q10–Q12).

### Key Entities

- **Library URL / filter state**: Existing live filter query semantics (canonical `filterQuery` path) — preserved; orphan `libraryUrl` module removed after evidence.
- **Navigation notification**: Feature-local `appNavigation.ts` shared openMovie + push/replace subscriber contract — new behavioural seam replacing synthetic popstate producers.
- **Typed GetPeople role helper**: Shared literal-typed helper retaining `role=actor` without inline casts (Q12).
- **Orchestration shells**: FilterPanel, LibraryView, MovieDetailsView — retained; Q10/Q12-ratified siblings extracted cohesively.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Library filter/sort/default/invalid-fallback behaviour matches pre-cleanup behaviour under existing unit coverage and SMOKE-001 mapped to named e2e scenario(s) or named extensions (or e2e Could not run blocks Done-when).
- **SC-002**: Programmatic movie opening from details/related/player paths and MovieCard fallback updates the UI without synthetic production popstate, preserving per-entry-point return URL and player intent (unit + SMOKE-002).
- **SC-003**: Browser Back/Forward continues to resynchronize the SPA correctly without duplicate history entries or leaked listeners (unit + SMOKE-003 mapped to named e2e scenario or extension).
- **SC-004**: Ticket-named orphan pair and proven unreferenced aliases/exports are gone only with recorded reference evidence (complete importer set `recon-DEV-475` lines 12–14); canonical filterQuery tests remain green; GetPeople retains `role=actor` via typed helper without inline casts (Q12).
- **SC-005**: Existing web lint, typecheck, unit, and e2e lanes all pass on the delivery head; jsx-a11y is unchanged in this delivery (deferred to DEV-512).
- **SC-006**: Size L review closes with zero unresolved in-scope Critical/High/Medium/Low and three-axis reports pinned to reviewed HEAD within 2×2 caps; work stops at awaiting user merge — no agent merge claim; ADR 0042 remains proposed until accepted.

## Assumptions

- No taste rulings; none tagged `[assumed]` (`ASSUMPTIONS.md`).
- All scope/navigation/deletion/type/dependency/verification/loop/ADR decisions are explicit Patron rulings in `CONCLUSIONS.md` (including Q10–Q12), not Quill assumptions.
- Q10-ratified siblings, Q11 `appNavigation.ts`, Q12 role/chip/range rulings, and current `recon-DEV-475` evidence are integrated; plugin-version and violation inventories are deferred with DEV-512.
- Tailwind follow-up DEV-511 and jsx-a11y follow-up DEV-512 are verified recorded follow-ups (Rigger receipts on task note DEV-475). Keel-owned `brief.md`/`ASSUMPTIONS.md` may still need Q12 / DEV-512 status sync (not Quill-edited).
- Grill closure / analyze CLEAN / plan-challenge NEEDS FIXES are distinct from plan freeze, Gate 1 approval, or implementation acceptance.
- ADR 0042 is proposed status; proposed does not imply Gate 1 or owner acceptance.
