# Implementation Plan: DEV-324 Watchlist Toggle and Filter

**Branch**: `feature/324-spec` | **Date**: 2026-10-12 | **Spec**: `specs/DEV-324/spec.md`

**Input**: `specs/DEV-324/spec.md` (from `specs/DEV-324/brief.md`, CONCLUSIONS Q1–Q12 plus Q3 addendum, ASSUMPTIONS, recon notes) and settled Palette `specs/DEV-324/design.md` (reviewed as an input prerequisite).

**Note**: Drafted by Quill from the settled brief, settled design, and bounded evidence; no implementation, test, gate, commit, or merge performed. Keel speckit-analyze follows; this draft claims no challenge outcome and no freeze.

## Summary

Frontend-only React SPA change: one shared optimistic watchlist hook plus toggle integration into the delivered card and the DEV-323 `MovieDetailsView` hero region, with SPA URL `watchlist=true` translated to API `inWatchlist=true`. No backend, contract, dependency, layer, or LocalPlay change. Phase B waits on prerequisite delivery merges including DEV-323; this plan does not authorize it.

## Technical Context

**Language/Version**: TypeScript (web/), React 19, Vite.

**Primary Dependencies**: react and react-dom 19.3.0 only; no new npm dependency, no state library, no Context/provider, no new layer. One small shared hook owning a module-scoped per-movie store read via React built-in `useSyncExternalStore`, overlaid on each delivered host's fetched `isInWatchlist` baseline; if pickup drift recon finds an already-delivered shared client cache for movie data, update its per-movie entry instead and do not build the module store. Generated `web/src/api/types.ts` and the Epic 6 generated client only; no hand-written request/response types. Baseline `isInWatchlist` availability on browse/details DTOs is unverified at pickup: absence means `needs decision:`, never a contract change.

**Storage**: N/A (no schema or migration change; `is_in_watchlist` persistence already shipped).

**Testing**: Vitest, React Testing Library, MSW; `web-implement` TDD workflow.

**Target Platform**: Web SPA (`web/`).

**Project Type**: Web application frontend slice (existing backend untouched).

**Performance Goals**: Instant (pre-response) optimistic toggle feedback; in-flight clicks ignored.

**Constraints**: `web/src` only; `App.tsx` is a composition surface only (no rewrite, no router/host construction, no new dependency, state library, or layer); C# DTO → OpenAPI → generated TypeScript chain intact; `openapi.json`, verified snapshot, and generated `types.ts` never hand-edited; MVC app untouched; `Features:LocalPlay`, secrets, and `Process.Start` untouched. No Context/provider, store library, or speculative abstraction.

**Scale/Scope**: Bookmark toggles on card and details hero, one Watchlist quick-filter toggle, one shared hook, URL/API mapping. Anything else is a follow-up issue.

## Constitution Check

- No new dependency, project, folder, layer, schema, API shape, secret, or LocalPlay touch: passes the §2.3 care list; nothing needs a Patron ruling beyond the recorded Q1–Q12 plus Q3 and Q7 addenda. The Q7 addendum settles the module-scoped per-movie store (sibling-cache alternative); no further store/Context/architecture decision is open.
- Ticket scope unchanged ("card and details hero"); DEV-324 builds no host: no ticket-change escalation.
- No constitution departure identified.
- Re-check after Phase 1 design and at Phase B pickup against drift recon.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-324/
├── spec.md               # This feature spec
├── plan.md               # This file
├── tasks.md              # Phase 2 output
├── brief.md              # Settled Patron grill record (input)
├── CONCLUSIONS.md        # Patron rulings incl. Q3 addendum (input)
└── ASSUMPTIONS.md        # Settled taste assumptions (input)
```

### Source Code (repository root)

```text
web/src/
├── api/                  # generated types.ts + generated client (prerequisite, DEV-320)
├── features/library/     # delivered card, grid, filter host, URL/browse state (prerequisites, DEV-321/322)
├── MovieDetailsView      # actual path fixed at pickup drift recon (prerequisite, DEV-323)
├── shared hook           # small optimistic hook added by DEV-324 (exact file fixed at pickup)
└── App.tsx               # composition surface only
```

**Structure Decision**: All DEV-324 implementation and frontend tests live under the existing `web/src` tree; no new top-level folder, project, or layer. Exact hook and integration filenames are fixed at Phase B pickup drift recon against the delivered hosts; this draft states them as unresolved rather than inventing them.

## Plan

### Phase 0 - Pickup drift recon (Phase B entry, not authorized here)

1. Record actual paths for the generated client/types, delivered card/grid, delivered filter host, delivered URL/browse integration, and the real `MovieDetailsView` path with its title/backdrop block.
2. Confirm prerequisite implementation/delivery merges exist, explicitly including DEV-323; a merged spec PR alone does not count.
3. Record the delivered seams without building anything: the shared-state/list-reconciliation seam for filter-active removal, the empty-state renderer seam (watchlist empty copy vs host empty behavior), the focus-on-removal behavior of the delivered list, and the URL history/hydration seam (Back/Forward, link share). If a capability is absent, or the only workaround would change scope (host/router construction, contract change, new dependency/layer), stop with `needs decision:` before any story work; resolve the chosen removal/empty integration after pickup, never speculatively here.
4. If DEV-323 has no distinct hero block, place the toggle next to the title in `MovieDetailsView`. If DEV-323 delivers no details view, stop with `needs decision:`.
5. Confirm both delivered hosts read `isInWatchlist` from their generated DTOs; absence means `needs decision:`, never a contract change.
6. Re-run speckit-analyze against main and amend tasks for drift; serialize changes to shared hook, composition, and shared test files.

### Phase 1 - Shared optimistic hook

Small shared hook in `web/src` (exact file fixed at pickup) owning a module-scoped per-movie store (`Map` keyed by `movieId`, private to the hook module, reset helper for tests only) read via `useSyncExternalStore`, overlaid on each delivered host's fetched `isInWatchlist` baseline; if pickup finds an already-delivered shared client cache, update its per-movie entry instead and do not build the module store. Behavior per movie: optimistic `{ target, pending: true }` write before the request; POST on add / DELETE on remove via the generated client; 204/409 retain the confirmed overlay through settlement, unmount, and cross-surface navigation; any other result removes the overlay (rollback to baseline) with a per-movie inline error; shared per-movie in-flight guard with `aria-busy` clearing on success or failure; settled accessible names and `aria-pressed`; overlays, guards, and errors keyed per `movieId` so movie A never affects movie B. No Context/provider, state library, new layer, or dependency.

### Phase 2 - Card and hero integration

Wire the shared hook into the delivered card and the delivered hero region so both read the same per-movie state across navigation. Apply the settled icon treatment and copy. Filter-active list behavior (successful removal removes the card through the chosen pickup reconciliation; failed removal keeps it) is excluded here; it is integrated and verified after the filter wiring (tasks T037/T038), not as part of this phase alone.

### Phase 3 - Quick filter and URL/API mapping

Wire the Watchlist toggle in the delivered filter host: only `watchlist=true` sends `inWatchlist=true`; switching updates the URL, resets page to 1, retains other filters, combines with AND; empty state "Your watchlist is empty."

### Phase 4 - Verification

`web-implement` TDD tests per spec (optimism, 204/409, rollbacks, in-flight ignore, URL/API mapping, page reset, accessible states, shared state across navigation) plus bounded design §6 integration coverage using only the delivered hosts with no router introduction and no host construction: bookmark tests before card/hero wiring covering native-button keyboard activation (Tab/Enter/Space), decorative SVG `aria-hidden` with name plus `aria-pressed` carrying state, announced inline toggle errors adjacent to the failed button, visible focus indication from the delivered host styles, and focus retained on the toggle through optimistic success and rollback; filter tests before filter wiring covering Back/Forward plus link-share URL restoration of the filter and grid-region focus after successful filter-active card removal (removed-card unmount only); applicable web verification through `scripts/run-web-gates.ps1`; OpenAPI drift test green and unchanged; no `.cs` changes expected (any C# diff applicability reported by Gauge, scope-empty as SKIPPED never PASS). M-ticket three-axis review before clearance; no clearance granted here.

## Complexity Tracking

No constitution violations to justify. No new abstraction, layer, or dependency is introduced.
