# Tasks: DEV-355 Watched and Not Interested actions with reason chips

**Input**: Design documents from `specs/DEV-355/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). Palette `design.md` must exist before plan challenge; it is not authored by these tasks.

**Tests**: Included. Ticket and brief Q6 require Vitest/RTL for both actions, optimistic lifecycle, concurrency, recommendation refresh, reason chips, and keyboard/accessibility. Author tests in the same phase as the surfaces they lock. Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 Wisp bind passes, the spec PR has merged, Gate 1 is established, and Build pickup drift check allows build intake.

**Organization**: Phases follow brief Task ordering and `spec.md` user stories US1–US3. Anvil implements one phase per ask via `web-implement`. Polish is Cog then Gauge.

**Domain vocabulary** (must remain aligned with root `CONTEXT.md`): **Feedback**, **Reason**, **Taste Profile** — definitions in `spec.md` Domain vocabulary. No new glossary edit.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 mark actions + optimistic refresh; US2 reason chips; US3 a11y/failure/concurrency.
- Exact file paths are in each description.

## Phase 1: Setup — Wisp DTO/cache/consumer bind

**Purpose**: Confirm required contract fields, existing cache keys, and consumer propagation inside the approved file envelope before production work.

- [X] T001 Wisp bind recon in the Build worktree against the approved surface: map `IsWatched` / `IsNotInterested` / reasons field availability across Library, Movie Details, and recommendation representations; list existing cache/query keys used by `libraryQueries.ts`, `movieDetailsQueries.ts`, and `relatedMoviesApi.ts`; list current recommendation consumers that mount the shared card; confirm POST routes and generated types still match `recon-DEV-355:14-23,29-38`. Gauge supplies applicable baseline. Keel supplies pickup/drift clearance after Gate 1. Stop for cited recon / Patron amendment if required fields are absent, unexpected consumers appear, or a necessary file falls outside the brief Q5 envelope — do not invent defaults, cache keys, or expand the envelope. No production edits in this task.

**Checkpoint**: Bind facts recorded; Phase B build intake is allowed.

---

## Phase 2: Foundational — typed endpoints and shared mutation state (US1/US3)

**Purpose**: Feature-local POST helpers and shared per-movie optimistic/pending store with RTL proofs. No Library/Details host chrome yet beyond what the store tests need.

**⚠️ CRITICAL**: Shared controls and host integration wait on this foundation.

### Tests

- [X] T002 [P] [US1] [US3] Write Vitest proofs in `web/src/library/movieFeedback.test.ts` for typed POST `/movies/{id}/watched` and POST `/movies/{id}/not-interested` using generated contract types and existing request interception: correct movie/route; NoContent success path; no automatic retry. AAA headers only. Mirror conventions from `recon-DEV-355:35-38`.

- [X] T003 [P] [US1] [US3] Write Vitest/RTL proofs in `web/src/library/movieFeedbackStore.test.tsx` covering brief Q6 store matrix: deferred completion updates only the selected flag before response; preserves the other flag; already-marked non-dispatch; shared per-movie exclusion blocks both actions for one movie across instances while another movie proceeds, including after a settled POST while success or failure reconciliation remains deferred; at-most-once under duplicate activation; unmount mid-pending then remount still blocked with deferred settlement/reconciliation once and observable mutation error/retry on the remounted instance; POST failure rolls back only this operation and reconciles server state for this movie via T001-bound keys while another movie's concurrent operation proceeds; explicit mutation retry under the same per-movie exclusion (double-activate retry dispatches at most one POST; retry during deferred reconciliation issues no POST); stale in-flight response cannot erase optimistic state; success invalidates/refetches affected movie/library data and all global-profile-dependent recommendation queries per T001 keys (inactive queries stale for next fetch); refresh failure after success keeps confirmed mark with separate refresh-only retry that performs refresh without another POST; no automatic POST retry. AAA headers only. Reuse watchlist-pattern testing approach (`recon-DEV-355:33,38`).

### Implementation

- [X] T004 [US1] Add `web/src/library/movieFeedback.ts` typed endpoint functions for POST watched and POST not-interested using generated contract types from `web/src/api/types.ts`. Do not hand-edit OpenAPI, generated TypeScript, or generated MSW. Do not call DELETE as product surface.

- [X] T005 [US1] [US3] Add `web/src/library/movieFeedbackStore.ts` shared per-movie mutation state/hook reusing the existing watchlist mutation approach (`recon-DEV-355:33`) in a narrow feature-local implementation. Initialize flags only from fields confirmed in T001. Protect against stale in-flight responses; retain pre-operation state; keep recommendation membership/reasons until authoritative refresh; wire invalidate/refetch using T001-bound keys only. Per-movie exclusion covers update/reconciliation, not only in-flight POST: after a settled POST, both actions stay blocked across same-movie instances until success or failure reconciliation completes, while other movies remain interactive. Pending/reconciliation and mutation error/retry state is store-owned and survives initiating-control unmount/remount so remounts render pending/blocked and can observe error/retry. On POST failure, roll back only this operation, expose accessible mutation error with explicit retry, and reconcile server state for this movie using only T001-bound keys without erasing other movies' concurrent updates. Explicit mutation retry re-issues that POST once under the same per-movie exclusion (at most one in-flight POST; no POST while reconciliation is active). Expose separate refresh-only error/retry state that retains the confirmed mark and retries refresh without another POST. No new state-management library or architectural layer. Do not rewrite watchlist files.

**Checkpoint**: Named targeted proofs — `movieFeedback.test.ts` and `movieFeedbackStore.test.tsx` green for US1/US3 mutation lifecycle.

---

## Phase 3: User Story 1 + User Story 3 - Shared controls and accessibility

**Goal**: Shared Mark Watched / Not Interested controls with keyboard, labels, pending, and error/retry presentation.

**Independent Test**: `MovieFeedbackActions.test.tsx` proves Enter/Space, accessible names including title, visible focus, pending announcement, mutation error alert with explicit mutation retry under the same per-movie exclusion, user-facing refresh-only error/retry that retains the confirmed mark and retries refresh without another POST, and navigation isolation from parent activation.

### Tests

- [X] T006 [P] [US1] [US3] Write Vitest/RTL proofs in `web/src/library/MovieFeedbackActions.test.tsx`: labels `Mark Watched` / `Watched` and `Not Interested` / `Not Interested (marked)` (ASSUMPTIONS.md); disabled marked non-dispatch; Enter/Space activation; accessible names including movie title; visible focus; pending announcement; accessible mutation error with explicit retry under the same per-movie exclusion (double-activate retry dispatches at most one POST; no POST while reconciliation is active); rendered refresh-only error/retry after POST success keeps the confirmed mark and retry performs refresh without another POST; activating feedback does not trigger parent-card navigation. AAA headers only.

### Implementation

- [X] T007 [US1] [US3] Add `web/src/library/MovieFeedbackActions.tsx` shared controls wired to `movieFeedbackStore`. Add `web/src/library/MovieFeedbackActions.css` only if Palette `design.md` requires scoped styling. Native keyboard-operable buttons; no undo/toggle UI. Wire explicit mutation retry through the same per-movie exclusion as the initial POST. Expose user-facing refresh-only error/retry that retains the confirmed mark and retries refresh without another POST. Placement/visual styling follow Palette within the settled interaction contract.

**Checkpoint**: Shared controls green for US1 labels/actions and US3 accessibility.

---

## Phase 4: User Story 1 + User Story 2 - Library, Details, and recommendation integration

**Goal**: Host shared actions on Library cards and Movie Details; reuse shared card on current recommendation consumers; render optional verbatim reason chips; preserve sensible focus when refresh removes a focused recommendation card.

**Independent Test**: Additive RTL in Library/Details/RelatedMovies tests proves both actions on each surface, optimistic no-reload update, recommendation refresh including removal, rendered refresh error/retry after POST success that retains the confirmed mark and retries refresh without another POST, verbatim Personal/Structural example strings in API order (duplicates and HTML-looking literals), empty/absent reasons with no chips, no fabricated Library/Details reasons, and focus preservation on card removal.

### Tests

- [ ] T008 [P] [US1] [US2] [US3] Extend `web/src/library/MovieCard.test.tsx`, and as needed `MovieCard.layout.test.tsx` / `MovieGrid.test.tsx`, with additive proofs: both actions on Library cards; optimistic selected-flag update before response; already-marked non-dispatch; keyboard activation; rendered refresh error/retry after POST success retains the confirmed mark and retry performs refresh without another POST; no fabricated reason chips on ordinary Library data; parent navigation isolation. Same-folder sequential coordination with T011. AAA headers only.

- [ ] T009 [P] [US1] [US2] [US3] Extend `web/src/features/movie-details/MovieDetailsView.test.tsx` with additive proofs: both actions on Details; optimistic update; preserved other flag; pending shared with other instances including through deferred reconciliation; mutation error/retry; rendered refresh error/retry after POST success retains the confirmed mark and retry performs refresh without another POST; no fabricated Details reasons. Same-folder sequential coordination with T012. AAA headers only.

- [ ] T010 [P] [US1] [US2] [US3] Extend `web/src/features/movie-details/RelatedMovies.test.tsx` with additive proofs: current recommendation card reuses shared actions; success refreshes recommendation query results including membership/flags/reasons/removal; inactive-query staleness for next fetch; rendered refresh error/retry after POST success retains the confirmed mark and retry performs refresh without another POST; verbatim reasons in API order including duplicates and HTML-looking literals; empty/absent reasons create no chips; sensible focus target when refresh removes the focused card. Mirror existing verbatim/empty reason coverage at `recon-DEV-355:26`. Same-folder sequential coordination with T013. AAA headers only.

### Implementation

- [ ] T011 [US1] Integratively mount `MovieFeedbackActions` on `web/src/library/MovieCard.tsx` and adjust `web/src/library/libraryQueries.ts` only as required by T001-bound invalidate/refetch wiring. No unrelated rewrites. No fabricated Library reasons.

- [ ] T012 [US1] Integratively mount `MovieFeedbackActions` on `web/src/features/movie-details/MovieDetailsView.tsx` and adjust `movieDetailsApi.ts` / `movieDetailsQueries.ts` only as required by T001-bound fields and invalidate/refetch wiring. Preserve existing details functionality. No unrelated rewrites.

- [ ] T013 [US1] [US2] Integrate shared card actions and optional reasons presentation input through `web/src/features/movie-details/RelatedMovies.tsx` and `relatedMoviesApi.ts` as required by T001. Render every returned reason string verbatim in API order as non-interactive chips; empty/absent means no chips; escape as React text. Edit `web/src/ui/MovieCarousel.tsx` only if shared card action/reason props require it (brief Q5). Do not build the DEV-418 panel. Preserve sensible panel focus when refresh removes the focused recommendation card.

**Checkpoint**: Library, Details, and current recommendation integration green for US1 actions/refresh and US2 chips; US3 host a11y/failure cases covered in additive tests.

---

## Phase 5: Polish — Cog refactor and Gauge gates

**Purpose**: Diff refactor; full applicable Phase 3 step 5 gate bar for this ui slice.

- [ ] T014 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages; no envelope expansion).

- [ ] T015 Gauge gates: configured contract freshness checks; `scripts/run-web-gates.ps1`; and all applicable task-pipeline Phase 3 step 5 gates for the actual diff. Record command, head, and actual exit/verdict. Gauge determines C#-only gate applicability. Report SKIPPED (scope-empty) / SKIP / NOT APPLICABLE verbatim — never as PASS. Exit 1, Could not run, or unaccepted property exit 2 blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add an ADR. Do not claim Phase A gate results.

**Checkpoint**: Delivery ready for review loops under brief Q1 closing bar (Critical/High/Medium block; Low only if it violates frozen acceptance or an applicable gate; two review rounds; two fix commits per round).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — Wisp bind must pass or stop for recon/Patron amendment.
- **Phase 2 (Foundational endpoints/store)**: Depends on Phase 1; blocks shared controls and host integration.
- **Phase 3 (Shared controls)**: Depends on Phase 2; blocks host mounts that assume `MovieFeedbackActions`.
- **Phase 4 (Host integration + reasons)**: Depends on Phase 3.
- **Phase 5 (Polish)**: Depends on Phases 2–4 complete.

### User Story Dependencies

- **US1 (P1)**: Mark actions + refresh — Phases 2–4.
- **US2 (P2)**: Reason chips — Phase 4 recommendation/shared-card integration (existing RelatedMovies verbatim pattern retained/extended).
- **US3 (P3)**: A11y/failure/concurrency — Phase 2 store proofs, Phase 3 control proofs, Phase 4 host proofs.

### Parallel Opportunities

- T002 and T003 can scaffold in parallel once T001 bind facts are known.
- T004 then T005 are sequential (store depends on endpoint helpers).
- T008/T009/T010 test scaffolds can proceed in parallel after Phase 3 export; T011/T012/T013 implementation edits are per-folder sequential with their matching tests.

### Parallel Example: Phase 2 proofs

```text
Task: "Endpoint proofs in web/src/library/movieFeedback.test.ts"
Task: "Store lifecycle proofs in web/src/library/movieFeedbackStore.test.tsx"
```

---

## Implementation Strategy

### MVP First (US1 actions + store)

1. Complete Phase 1 Wisp bind (stop on gaps).
2. Complete Phase 2 typed endpoints + shared store with RTL.
3. Complete Phase 3 shared controls + a11y RTL.
4. **STOP and VALIDATE**: Controls/store independently before host integration.
5. Add Phase 4 Library/Details/recommendation integration and reason chips.
6. Phase 5 Cog refactor and Gauge gates.

### Incremental Delivery

1. Setup bind → foundation ready or blocked with cited recon/decision.
2. Endpoints + store → optimistic mark lifecycle (US1/US3 core).
3. Shared controls → accessible actions (US1/US3).
4. Host integration → Library/Details/recommendation + chips (US1/US2).
5. Polish → refactor and honest gate recording.

### Notes

- [P] tasks = different files, no unfinished dependency.
- Preserve brief Q5 file envelope; stop rather than expand.
- Facts for cache keys/DTO availability/consumers come only from T001 / `recon-DEV-355` — never invent.
- Commit after each phase in Build: `DEV-355 - <phase title>`.
- Palette design precedes plan challenge; these tasks do not author `design.md`.
