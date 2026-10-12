---
description: "Task list for DEV-324 watchlist toggle and filter"
---

# Tasks: DEV-324 Watchlist Toggle and Filter

**Input**: `specs/DEV-324/spec.md`, `specs/DEV-324/plan.md`, `specs/DEV-324/brief.md`, settled Palette `specs/DEV-324/design.md` (reviewed as an input prerequisite), CONCLUSIONS (Q1–Q12 plus Q3 addendum), ASSUMPTIONS.

**Prerequisites**: plan.md, spec.md, settled design.md reviewed. Keel speckit-analyze follows this draft; this draft claims no challenge outcome and no freeze. Phase B (pickup onward) is not authorized by this draft.

**Tests**: Included because the spec (Q12) explicitly requires them. Write tests FIRST and ensure they FAIL before implementation (`web-implement` TDD). Test tasks sharing one toggle, browse, filter, or list test file run serialized in the listed order, never in parallel; shared MSW setup is written serially too.

**Organization**: Grouped by user story. Exact hook and integration filenames are fixed at Phase B pickup drift recon; tasks name the delivered host rather than inventing a path.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Spec-kit draft only; no project initialization (hosts are prerequisites).

- [ ] T001 Record settled inputs: brief.md, settled design.md, CONCLUSIONS Q1–Q12 plus Q3 addendum, ASSUMPTIONS copy, recon parts -2 through -6 (done by this draft; Keel to verify)
- [ ] T002 Review settled Palette `design.md` as an input prerequisite before plan challenge (ui: ticket ordering per brief; claims no challenge outcome and no freeze)

---

## Phase 2: Foundational - Phase B pickup drift recon (BLOCKS all stories; not authorized here)

**Purpose**: Confirm prerequisite delivery merges and actual paths before any story work.

**CRITICAL**: No user story work begins until this phase is complete.

- [ ] T010 Verify prerequisite implementation/delivery merges exist, explicitly including the DEV-323 delivery merge (spec-PR merges do not count); confirm spec-PR Gate 1 state separately
- [ ] T011 [P] Record actual path of generated `types.ts` and generated API client under `web/src/api`
- [ ] T012 [P] Record actual path of delivered movie card/grid and its composition via `App.tsx`; record the delivered shared-state/list-reconciliation seam and the delivered focus-on-removal behavior for filter-active removal (removed-card unmount only); missing capability or scope-changing workaround means `needs decision:` before story work
- [ ] T013 [P] Record actual path of `MovieDetailsView` and its title/backdrop block (hero region); fallback: toggle next to title; no details view means stop with `needs decision:`
- [ ] T014 [P] Record actual path of delivered filter host and delivered URL/browse-state integration; record the delivered empty-state renderer seam (watchlist empty copy vs host empty behavior when the filter is off) and the URL history/hydration seam (Back/Forward, link share); missing capability or scope-changing workaround means `needs decision:` before story work
- [ ] T015 Amend tasks for recorded drift; resolve the chosen removal/empty integration after pickup, never speculatively; assign each test group an explicit disjoint test file (or record that groups share one file and must run serialized); serialize changes to shared hook, composition, shared test files, and shared MSW/test setup
- [ ] T016 Re-run read-only speckit-analyze against main after drift recon and task amendment per brief.md:64 and plan.md Phase 0; resolve reported inconsistencies before any story work begins

**Checkpoint**: Hosts confirmed and paths recorded; story work may begin.

---

## Phase 3: User Story 1 - Bookmark toggle (Priority: P1)

**Goal**: Instant optimistic bookmark feedback on the delivered card and delivered hero, sharing one movie's state.

**Independent Test**: Delayed MSW response still shows the flip first; toggle in one surface shows in the other after navigation; 204/409 retain, failures roll back with the inline error.

### Tests for User Story 1 (write FIRST, FAIL before implementation; serialized in listed order: shared toggle/integration files, shared MSW setup)

- [ ] T020 [US1] Optimistic flip before response in `web/src` toggle test (MSW delayed POST/DELETE)
- [ ] T021 [US1] 204 and 409 retain the confirmed target overlay after request settlement and across card/hero navigation in `web/src` toggle test
- [ ] T022 [US1] 404, 422, 500, representative 400, network failure, and any other failure roll back and show "Couldn't update watchlist. Try again." in `web/src` toggle test
- [ ] T023 [US1] In-flight clicks ignored with `aria-busy` in `web/src` toggle test; `aria-busy` clears on success and on failure; busy state shown while in flight
- [ ] T024 [US1] Accessible name ("Add to watchlist" / "Remove from watchlist") and `aria-pressed` in `web/src` toggle test
- [ ] T025 [US1] Shared state across card/hero navigation in `web/src` integration test (delivered card and delivered DEV-323 hero hosts only; no host construction); different-movie isolation (movie A toggle/pending/failure never affects movie B); delayed settlement across card unmount/navigation retains 204/409 and rolls back failures with the per-movie error on the mounted surface
- [ ] T026 [US1] Design §6 bookmark integration coverage using delivered hosts only (no router introduction, no host construction), in `web/src` integration test, written before T027-T029 per TDD and run serialized after T020-T025: native-button keyboard activation (Tab/Enter/Space) for the bookmark toggle; decorative bookmark SVG `aria-hidden` with name plus `aria-pressed` carrying state; inline toggle error announced to assistive technology adjacent to the failed button; visible focus indication from the delivered host styles; focus retained on the toggle through optimistic success and rollback

### Implementation for User Story 1

- [ ] T027 [US1] Implement shared optimistic hook in `web/src` (exact file per T015; Q7 addendum: module-scoped per-movie `Map` store read via `useSyncExternalStore` over host `isInWatchlist` baseline, or already-delivered sibling cache entry when pickup finds one; no Context/provider, state library, new layer, or dependency): per-movie optimistic write, POST/DELETE via generated client, 204/409 confirmed retention through settlement/navigation, other-failure rollback with per-movie inline error, shared per-movie in-flight guard with `aria-busy` clearing on settle, per-movie isolation
- [ ] T028 [US1] Wire hook into delivered card (path per T012); apply inline SVG bookmark treatment and settled copy
- [ ] T029 [US1] Wire hook into delivered hero region of `MovieDetailsView` (path per T013); no host construction, no hero-component extraction

**Checkpoint**: User Story 1 toggle fully functional and testable independently (filter-active list behavior excluded here; covered after filter integration in Phase 4).

---

## Phase 4: User Story 2 - Watchlist quick filter (Priority: P2)

**Goal**: Watchlist-only filtering with correct URL/API mapping, page reset, and retained filters.

**Independent Test**: Toggling the filter updates the URL, resets to page 1, keeps other filters, sends `inWatchlist=true` only for `watchlist=true`, and shows the empty state when nothing matches.

### Tests for User Story 2 (write FIRST, FAIL before implementation; serialized in listed order: shared browse/filter/list files, shared MSW setup; all depend on Phase 2 host paths; T035/T036 written before T037/T038 per TDD and run serialized after T030-T034, with removal behavior verified only after T038)

- [ ] T030 [US2] `watchlist=true` sends `inWatchlist=true`; absent/`false`/other sends no filter, in `web/src` browse test
- [ ] T031 [US2] Filter toggle syncs to URL and resets page to 1 while retaining other filters, in `web/src` filter test; bounded in-flight filter-switch URL/query verification against the delivered URL/browse integration at pickup, with drift stop if unsupported
- [ ] T032 [US2] Filter combines with other filters using AND, in `web/src` browse test
- [ ] T033 [US2] Empty state "Your watchlist is empty." when filter is on with no matches, in `web/src` list test; when the filter is off, the delivered host empty behavior is preserved (no watchlist copy override, no host override API)
- [ ] T034 [US2] Quick-filter label "Watchlist" with pressed state at `?watchlist=true`, in `web/src` filter test
- [ ] T035 [US2] With watchlist filter on, successful removal removes the card through the chosen pickup reconciliation and failed removal keeps it with rollback plus inline error, in `web/src` list test (written before T037/T038 per TDD; behavior verified only after T038 using the delivered filter host per T014)
- [ ] T036 [US2] Design §6 filter integration coverage using delivered hosts only (no router introduction, no host construction), in `web/src` integration test, written before T037-T038 per TDD and run serialized after T030-T035: native-button keyboard activation (Tab/Enter/Space) for the Watchlist filter toggle; Back/Forward and link-share restore the `?watchlist=true` filter state; grid-region focus after successful filter-active card removal (removed-card unmount only, never `body`); bounded in-flight filter-switch URL/query verification at pickup, with drift stop if unsupported

### Implementation for User Story 2

- [ ] T037 [US2] Wire Watchlist quick-filter toggle into delivered filter host (path per T014); URL `watchlist` to API `inWatchlist` translation, page-1 reset, retained filters, AND combination
- [ ] T038 [US2] Integrate filter-active removal behavior after T037 (depends on T037): successful removal removes the card; failed removal keeps the card with rollback plus inline error

**Checkpoint**: User Stories 1 AND 2 both work independently; filter-active removal behavior verified only after T038.

**Global guard**: `App.tsx` is a composition surface only: no rewrite, no router/host construction, no new dependency, state library, or architectural layer. Composition-only integration into delivered hosts; missing host capability means `needs decision:`, never silent construction or scope-changing workaround.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verification and review; no scope additions.

- [ ] T040 Run `web-implement` TDD suite and applicable web verification through `scripts/run-web-gates.ps1` in Phase B
- [ ] T041 Confirm OpenAPI drift test green with baseline and snapshot byte-identical; confirm no `.cs` changes (scope-empty reported as SKIPPED, never PASS)
- [ ] T042 Complete M-ticket three-axis review before clearance (no clearance granted by this draft)

---

## Dependencies & Execution Order

- **Setup (Phase 1)**: Spec-kit draft; no dependencies.
- **Foundational (Phase 2)**: Pickup drift recon (T010–T015) plus read-only speckit-analyze and inconsistency resolution (T016) BLOCKS all stories; Phase B waits on prerequisite delivery merges including DEV-323.
- **User Stories (Phases 3–4)**: Depend on Phase 2; US1 toggle (T020–T026 tests, then T027–T029) before US2 filter; US1 standalone excludes the filter-active scenario (verified with US2 after T038); bookmark design test T026 written before T027–T029 and run serialized after T020–T025; filter-active removal test T035 and filter design test T036 written before T037/T038 per TDD and run serialized after T030–T035, with removal behavior verified only after T038.
- **Polish (Phase 5)**: Depends on stories complete.

### Within Each User Story

- Tests FIRST and FAIL before implementation; shared hook before card/hero wiring; filter integration T037 before removal integration T038; no production code outside `web/src`; no hand edits to `openapi.json`, snapshot, or generated `types.ts`. Tasks sharing the toggle, browse, filter, list, or integration test file, the shared hook, composition, or shared MSW/test setup run serialized in listed order.

### Parallel Opportunities

- T011–T014 in parallel (read-only recon records). No parallel test tasks: T020–T026 share toggle/integration files and run serialized in listed order, T030–T036 share browse/filter/list/integration files and run serialized in listed order, and shared MSW/test setup is serial (explicit disjoint-file assignment per T015, otherwise serialized).
