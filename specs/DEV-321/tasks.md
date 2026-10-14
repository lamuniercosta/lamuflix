# Tasks: DEV-321 — Library browse view

**Input**: `specs/DEV-321/spec.md`, `specs/DEV-321/plan.md` (authority: `specs/DEV-321/brief.md`, grill closed Q10/12).

**Prerequisites**: plan.md, spec.md. Phase A order per brief Q9: this spec/plan/tasks → Keel read-only analysis → Palette design → later ADR/axes/adjudication → spec PR → user merge (Gate 1). Phase B additionally waits for the user merge of DEV-320 #124 + Wisp recon of merged types/client/signal support.

**Tests**: Included — the ticket Scope item 4 (DEV-321:31) explicitly requires them, and brief Q8/Q9 mandates the backend + web test envelope.

**Organization**: [Story] tags mark story affinity for traceability; phases mark Phase B execution order, which follows brief Q9 §Phase B steps 2–7 as corrected (adapter proof → query/boundary/provider → grid tiles → composition/controls/pagination). Each phase checkpoint gates the next.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prerequisites and frozen-shape verification before any edit.

- [ ] T001 [Setup] Verify Phase B prerequisites state: user-merged DEV-321 spec PR (Gate 1) and user merge of DEV-320 #124 plus Wisp recon of merged `web/src/api/types.ts`, `client.ts`, abort-signal support; stop if unmet (brief Q5).
- [ ] T002 [P] [Setup] Wisp current-head recon of generated `types.ts`/`client.ts`/signal support and changed-file drift vs main; Keel drift analysis; Gauge baseline (brief Q9 Phase B step 1).
- [ ] T003 [P] [Setup] Record the committed-baseline `enrichmentStatus` schema state read-only (no regeneration): note whether the baseline already describes it as a string schema with all four names. Regeneration happens only at T014, after prerequisite clearance, drift/baseline recon, and C# widening (brief Q1/Q8/Q9).

---

## Phase 2: Foundational — additive browse contract (US1 backend; BLOCKS stories)

**Purpose**: Widened C# contract + regeneration that every story depends on. No migration, no new join, no global serializer change.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T010 [US1] Widen port record in `src/LamuFlix.Core/Ports/MovieSummary.cs` (add nullable `posterUrl`/`releaseYear`/`runtime`/`imdbRating` + required `enrichmentStatus`; names/types/units per brief Q1).
- [X] T011 [US1] Widen same-row projection in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` (`BrowseAsync` projects existing row columns; still no JOIN; owns the production `new MovieSummary(` construction update at `EfMovieCatalog.cs:31`).
- [X] T012 [US1] Widen browse DTO + mapping in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` (`MovieSummaryResponse` + `From` mapping, nulls stay present as JSON null).
- [X] T013 [US1] Apply the remaining compile-forced construction updates in test files: `BrowseMoviesQueryHandlerTests.cs:29` (within T016) and `RecordingMovieCatalog` in `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs:688` (within T017); production site stays with T011 (brief Q8 amendment).
- [X] T014 [US1] Regenerate `web/src/api/openapi.json` + `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together via `LAMUFLIX_REGENERATE_OPENAPI=true` (OpenAPI + derived snapshot only; generated TS waits for T015); never hand-edit generated files.
- [X] T015 [US1] Inspect the regenerated schema: add the `Program.cs` EnrichmentStatus-scoped OpenAPI schema transformer ONLY if the four names are still missing (scoped to that one schema, no global serializer change); if added, regenerate OpenAPI + derived snapshot again; then verify the exact four-name string schema and generate `web/src/api/types.ts` via DEV-320 merged tooling.

### Backend tests for the contract (extend FIRST with failing assertions before T010–T015; construction updates compile them green after widening)

- [X] T016 [US1] Extend `BrowseMoviesQueryHandlerTests.cs` (new wire-shape assertions FIRST, red before widening; then its compile-forced construction update per T013; preserve unchanged-query/returned-page proof).
- [X] T017 [US1] Extend `LibraryEndpointsTests.cs` (`BrowseMovies_SuccessBody_MatchesTheFrozenScalarShape` widened to exact new wire shape FIRST, red before widening; then its `RecordingMovieCatalog` construction update per T013; populated/null + status-string assertions; reuse nullable-stays-present patterns).
- [X] T018 [P] [US1] Extend `ApiEndToEndBrowseTests.cs` (exact item assertions widened; seeded full-stack grid scalar/status proof with populated/null poster and metadata; seed `PosterUrl` directly — never rely on enrichment).
- [X] T019 [P] [US1] Extend `EfMovieCatalogBrowseTests.cs` (real Postgres projection proof: all four statuses; populated/null grid scalars).
- [X] T020 [P] [US1] Update `EfMovieCatalogPagingTests.cs` (`BrowseAsync_PageQuery_SelectsOnlySummaryColumns`: grid summary columns replace id/title-only expectation; retain no-JOIN + empty ChangeTracker assertions).
- [X] T021 [P] [US1] Extend `OpenApiContractTests.cs` (generated browse schema/status/nullability proof + baseline/snapshot drift). `ApiEndToEndLibraryTests.cs` stays a read-only pattern, not an edit target.

**Checkpoint**: Contract widened, regenerated chain green, backend tests proving wire + projection + schema.

---

## Phase 3: User Story 2 — URL/history adapter proof (Priority: P1)

**Goal**: Canonical URL vocabulary with aliases/defaults/groups, push/replace/popstate ownership, bookmark + back/forward adapter-state restoration — proven before any consumer exists. Controls, LibraryView, and App integration arrive in Phase 6.

**Independent Test**: Bookmark URLs (canonical + alias forms) restore adapter normalized state; adapter push/replace/popstate semantics hold without push on restore; Vitest table/generated round-trip cases.

- [X] T040 [P] [US2] Create `web/src/library/libraryUrl.ts` (canonical keys; `genres`/`Asc`/`Desc` read-only aliases with `replaceState` canonicalization, `genreIds` precedence, aliases never emitted; always emit `sort`/`direction`/`page`/`pageSize`; honor supplied pageSize; SPA defaults Title/Ascending/1/24; lenient group reads; malformed/repeated fallback; exact-case enums; lowercase booleans; ampersand join + encoding).
- [X] T041 [US2] Create `web/src/library/useLibraryUrl.ts` (adapter as sole history/URL owner: initial bookmark read, push on user change, replace on normalization, popstate restore without push, listener cleanup; filter/sort edits reset page to 1, pagination changes page alone; typed mapping of lowercase keys to generated PascalCase API params).
- [X] T042 [P] [US2] Vitest round-trip tests for `libraryUrl` (every canonical key; aliases/precedence/defaults/groups/malformed/repeated/encoding; reserved characters; server-codec parity; text empty-string vs null) — table + generated cases, no new package.
- [X] T043 [US2] History/bookmark/back-forward integration tests (push vs replace semantics, popstate restores adapter normalized state without push, listener cleanup; actual control/query restoration proof stays with T063 in Phase 6).

**Checkpoint**: URL/history adapter proof complete (adapter normalized-state + history/listener behavior only); no consumers yet.

---

## Phase 4: User Story 3 — Query, boundary, provider + loading/error proof (Priority: P2)

**Goal**: TanStack provider installed before consumers; one normalized-key query via the merged generated client; skeletons; boundary + retry; cancellation.

**Independent Test**: MSW success/422/400/error fixtures prove skeletons, 422 invalid-filter without retry, other-error (including 400) retry/reset, status-based classification, and cancellation behavior.

- [X] T033 [US1] Add only `@tanstack/react-query` to `web/package.json` + lockfile; install one `QueryClientProvider` in `web/src/main.tsx`; responsive styling in `web/src/index.css` (TanStack install + provider land here so they precede all consumers; `App.tsx` replacement stays with T053 composition).
- [X] T050 [US3] Create `web/src/library/useBrowseMovies.ts` (one query keyed by complete normalized filter/page state; merged generated client; signal propagated; no per-tile fetch/prefetch; key change → skeletons, same-key refetch retains content + polite announcement; retries off).
- [X] T051 [US3] Create `web/src/library/LibraryErrorBoundary.tsx` (hand-written; render/query failures; resets on query-key change; Retry resets/refetches the same canonical URL without history entry; 422 invalid-filter never retried vs recoverable fetch error including 400, classified by HTTP status not error-key vocabulary).
- [X] T052 [P] [US3] RTL tests: skeletons + same-key refetch; 422-no-retry / other-error (including 400) retry/reset with status-based classification; cancellation/stale-response protection.

**Checkpoint**: Loading/error/recovery behavior independently verified (tests carry their own provider; app wiring waits for Phase 6 composition).

---

## Phase 5: User Story 1 — Browse grid tiles (Priority: P1) 🎯 MVP

**Goal**: Responsive poster grid with all tile fields, placeholders, em-dash fallbacks, empty states — isolated tile proof.

**Independent Test**: Seeded real-DB rows (populated/null poster + metadata) render exact tiles; RTL covers populated/null/broken images, responsive structure, empty-library/filtered-empty states with clear-filters.

- [ ] T030 [P] [US1] Create `web/src/library/MovieCard.tsx` (poster/placeholder at reserved 2:3 slot, title, year, `<n> min` runtime, one-decimal IMDb badge, exact-label status chip, em dash for missing scalars).
- [ ] T031 [P] [US1] Create `web/src/library/MovieGrid.tsx` (responsive CSS grid; skeleton parity; empty/filtered-empty states with clear-filters + return-to-first-page action for past-end pages).
- [ ] T034 [P] [US1] RTL tests: populated/null/broken poster, tile fields, em-dash fallbacks, responsive structure, empty states (adjacent `MovieCard.test.tsx` / `MovieGrid.test.tsx`; isolated card tests are independent).

**Checkpoint**: Isolated tile proof only from the widened contract; live browse acceptance waits for Phase 6 composition (Phase 4 provider/query proven, integration in Phase 6).

---

## Phase 6: Composition — controls, pagination, integrated history (Priority: P2)

**Goal**: LibraryView/App composition follows controls and pagination creation and their interfaces: full codec control surface + Previous/Next pagination wired to URL + grid, closed by integrated bookmark/back-forward tests.

**Independent Test**: Every control restores from URL and applies together with page reset; pagination honors `ceil(totalCount/pageSize)` bounds; integrated bookmark/back-forward passes through controls + grid + pagination.

- [ ] T032 [US1] Create `web/src/library/LibraryView.tsx` (composes controls/grid/pagination; owns controls/grid/pagination per brief Q6; at current SPA root).
- [ ] T060 [P] [US4] Create `web/src/library/LibraryFilters.tsx` (text Enter/Search; sort/direction selects; status multiselect; watchlist tri-state; runtime/year bounds + include-unknown; genre/actor numeric-ID entry; apply together with page reset).
- [ ] T061 [P] [US4] Create `web/src/library/Pagination.tsx` (Previous/Next; current page + result count; disabled past known bounds; past-end empty state with return-to-first-page).
- [ ] T062 [P] [US4] RTL tests: every URL key restorable in its control; apply-together + page-reset; first/last/empty/past-end pagination; labels, keyboard/focus-visible, polite announcements.
- [ ] T035 [US1] Convert `web/src/App.test.tsx` (orphaned readiness test) into Library integration tests.
- [ ] T053 [US3] Replace readiness `web/src/App.tsx` with the main Library view; wire App composition (`QueryClientProvider` + boundary + `LibraryView`) in `web/src/App.tsx` / `main.tsx`.
- [ ] T063 [US4] Integrated bookmark/back-forward test through controls + grid + pagination: actual control/query restoration proof (all web work via `web-implement` TDD; closes the phase).

**Checkpoint**: All user stories functional and integrated (grid + URL sync + fetching + controls + history).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Gates, reviews, and handoff under the Q10 closing bar and Q9 order.

- [ ] T070 [Polish] Cog refactor pass on touched code (Conductor-assigned; Anvil/Cog seat rules per brief Q9).
- [ ] T071 [P] [Polish] Run diff-scoped Roslyn / cyclomatic-complexity / InspectCode gates; fix in code, never lower thresholds.
- [ ] T072 [Polish] Run mutation (`scripts/run-mutation.ps1`, no `since` in worktrees) + property tests (Q9 amendment: URL round-trip needs no new C# tests; any wider opt-out only if the changed C# introduces no invariant, reason recorded — unchanged `MovieQueryString` alone does not waive it) + vulnerable-packages + format verification + `dotnet test`.
- [ ] T073 [Polish] Run `scripts/run-web-gates.ps1` (web + contract drift/generated-output checks).
- [ ] T074 [Polish] Gauge complete mixed backend/web gates; three-axis review + Keel adjudication under Q10; bounded remediation (≤2 rounds, ≤2 fix commits/round); L ship review/rebased gates; PR with exact-head merge bar; end at `awaiting-merge` (user merges; no auto-merge).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — starts immediately; BLOCKS Phase 2 on prerequisite failure.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (prerequisite clearance + drift/baseline + C# widening, then contract regeneration with schema inspection before TS).
- **Frontend (Phases 3–6)**: Depend on Foundational; execute adapter proof (Phase 3) → query/boundary/provider + loading/error proof (Phase 4) → grid tiles (Phase 5) → composition/controls/pagination + integrated history (Phase 6). A phase checkpoint gates the next; isolated card tests (T034) are independent but live browse acceptance waits for Phase 6 composition.
- **Polish (Phase 7)**: Depends on all desired stories complete.

### Within Each Phase

- Tests FIRST, FAIL before implementation (TDD; web via `web-implement`).
- Contract/projection before DTO before regeneration (T010–T012) before schema inspection/conditional transformer (T015) before TS consumers.
- Adapter (`libraryUrl`, `useLibraryUrl`) before hook (`useBrowseMovies`) before view composition; TanStack install + provider (T033) before all consumers.
- Phase checkpoint before moving to the next phase.

### Parallel Opportunities

- T003 recon may run alongside T001/T002 (read-only, different surfaces).
- Backend test assertion extensions T016–T021 can run in parallel (different files); their construction updates follow widening per T013.
- `MovieCard`/`MovieGrid` (T030/T031) and their RTL tests (T034) parallelize by file.
- `libraryUrl` (T040) + its Vitest cases (T042) parallelize against hook work after the adapter interface freezes.
- Gate runs T071–T073 parallelize where tooling allows; remediation stays sequential per round caps.

---

## Implementation Strategy

### MVP First (backend + adapter + fetching + tiles)

1. Complete Phase 1 (Setup/prerequisites).
2. Complete Phase 2 (Foundational contract + regeneration + backend tests).
3. Complete Phase 3 (URL/history adapter proof).
4. Complete Phase 4 (query/boundary/provider + loading/error proof).
5. Complete Phase 5 (grid tiles).
6. **STOP and VALIDATE**: isolated tile proof only (populated/null tiles from the widened contract); live browse acceptance after Phase 6 composition.

### Incremental Delivery

1. Setup + Foundational → contract ready.
2. URL/history adapter → proven state vocabulary with no consumers.
3. Query/boundary/provider → resilient loading/recovery on the adapter.
4. Grid tiles → isolated tile proof; live browse acceptance waits for Phase 6 composition.
5. Composition/controls/pagination → complete browse experience with integrated history.
6. Polish → gates, review, `awaiting-merge`.

---

## Notes

- Exact file envelope only: no unnamed-file edits beyond compile-forced construction sites; `ApiEndToEndLibraryTests.cs` is a read-only pattern.
- Seed `PosterUrl` directly in tests; never claim live enrichment produces posters (DEV-408 is the follow-up).
- Existing fixtures/seeds reused; `web/src/test/server.ts` + `setup.ts` reused with feature fixtures/handlers added in tests; synthetic harness behavior preserved.
- No `since` flag on mutation from worktrees; no threshold waiver; scope-empty exit 2 is SKIPPED, never PASS.


