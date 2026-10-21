# Tasks: DEV-343 RelatedMovies on Movie Details with reason chips and MSW mocks

**Input**: Design documents from `specs/DEV-343/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). Palette `design.md` must exist before plan challenge; it is not authored by these tasks.

**Tests**: Included. Ticket and brief Q6 require Vitest/RTL empty/loading/populated proofs, MSW recommendation-endpoint mocking, carousel keyboard/reuse proofs, integration/regression coverage, and built-UI browser verification. Author tests in the same phase as the surfaces they lock. Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 drift recon passes, the spec PR has merged, Gate 1 is established, and Phase B prerequisites (merged DEV-323 and DEV-341, landed generated contract, Keel pickup clearance) allow build intake.

**Organization**: Phases follow brief Task ordering and `spec.md` user stories US1–US3. Anvil implements one phase per ask. Polish is Cog then Gauge.

**Domain vocabulary** (must remain aligned with root `CONTEXT.md`): **Recommendation**, **Reason** — definitions in `spec.md` Domain vocabulary. No new glossary edit.

**propertyTests**: opt-out — no domain invariants in this presentation/contract-only change (brief Q6–Q7). Accepts no-tests-tagged exit 2 only; actual failure still blocks.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 populated related movies + navigation; US2 empty/loading/error states; US3 exported MovieCarousel keyboard/reuse.
- Exact file paths are in each description.

## Phase 1: Setup — prerequisite drift recon

**Purpose**: Confirm merged prerequisites and landed contract before production work.

- [ ] T001 Wisp prerequisite/drift recon against main: confirm merged DEV-323 Movie Details host and merged DEV-341 recommendations route/DTO/status/default parameters; landed C# DTO → `web/src/api/openapi.json` → generated `web/src/api/types.ts` and `web/src/mocks/handlers.ts`; fixture/typecheck surfaces; Vitest/MSW conventions under `web/src/test/server.ts` and `web/src/mocks/`; MovieDetailsView placement seams (MoreWithPanel then FileSection); navigation helpers (`openDetails` / libraryReturnUrl). Gauge supplies applicable baseline. Keel supplies pickup/drift clearance. Stop for a cited decision on missing prerequisites or any contract/placement mismatch rather than inventing a bridge. No production edits in this task.

**Checkpoint**: Prerequisites match the brief; Phase B build intake is allowed.

---

## Phase 2: Foundational — exported MovieCarousel (US3)

**Purpose**: Reusable presentational horizontal keyboard-accessible carousel with focused proofs and reuse mount. No recommendations fetching here.

**⚠️ CRITICAL**: RelatedMovies cards compose into this carousel; US1 populated rail waits on the exported component.

### Tests

- [X] T002 [P] [US3] Write focused Vitest/RTL proofs in `web/src/ui/MovieCarousel.test.tsx`: overflow; both Previous/Next boundaries; no-overflow state with both controls disabled; viewport-only ArrowLeft/ArrowRight when the scroll viewport has focus; normal native link Tab/Enter behavior; reuse mount with non-recommendation children; remeasurement of Previous/Next control boundaries after scroll, resize, and changed children (narrow layout stubs for deterministic RTL). Method names follow existing web conventions. AAA headers only.

### Implementation

- [X] T003 [US3] Add exported presentational `web/src/ui/MovieCarousel.tsx` and `web/src/ui/MovieCarousel.css` accepting an accessible label and rendered card children. Own horizontal layout and scrolling only: labelled region, semantic list, focusable scroll viewport, Previous/Next disabled at boundaries and when content fits, ArrowLeft/ArrowRight only on viewport self-focus, no wraparound/autoplay/roving focus/descendant-key interception, scrolling does not move movie focus, visible focus, native touch/overflow, reduced motion. No fetching, endpoint DTO dependency, or recommendation logic. Reuse existing directional icons from `web/src/ui/icons.tsx` when needed (`recon-DEV-343:47`).

**Checkpoint**: Named targeted proofs — `MovieCarousel.test.tsx` green for US3 interaction/reuse contract. Exported for later RelatedMovies and DEV-418 reuse.

---

## Phase 3: User Story 1 + User Story 2 - RelatedMovies query, cards, Reason chips, and states

**Goal**: Typed recommendations query; populated ordered cards with verbatim Reason chips and details navigation; empty/loading/local Retry error states with identity isolation.

**Independent Test**: `RelatedMovies.test.tsx` with generated MSW handlers and per-test overrides proves empty message without carousel; delayed loading placeholder; populated ordered cards and Reason chips; failure/refetch retry including recommendations 404; background-refetch failure; identity isolation including out-of-order movie requests; poster fallbacks; optional year; card navigation and validated library return URL; native modified activation.

### Tests

- [X] T004 [P] [US1] [US2] Write Vitest/RTL proofs in `web/src/features/movie-details/RelatedMovies.test.tsx` covering brief Q6 matrix: empty success (`No related movies yet.`, no carousel); delayed loading (`Loading related movies…`); populated ordered cards; all Reason strings as inert text in order and empty Reasons without fabricated chips; null/missing and broken poster fallback; optional ReleaseYear; card details navigation and validated library return URL; native modified activation; failure/refetch Retry including recommendations 404 (local notice `Could not load related movies.`); background-refetch failure that retains same-movie populated data, shows the local failure notice, and proves Retry triggers refetch; identity isolation including out-of-order movie requests; negative query proofs that loading, failed, missing, and invalid host Movie Details produce no recommendations request (preserve existing host states; do not invent a universal no-section rule). Use existing test server, typed fixtures, generated handler and per-test MSW overrides. AAA headers only.

### Implementation

- [X] T005 [P] [US1] [US2] Add `web/src/features/movie-details/relatedMoviesApi.ts` using the existing typed openapi-fetch client and TanStack Query, keyed by movieId, enabled only for a valid loaded Movie Details view, consuming the landed DEV-341 route with default parameters confirmed in T001, preserving API order, and isolating movie identities. Do not invent limits/options/statuses or hand-rolled DTO duplicates.

- [X] T006 [US1] [US2] Add `web/src/features/movie-details/RelatedMovies.tsx` and `RelatedMovies.css`: heading `Related movies`; loading/empty/populated/error+Retry states per ASSUMPTIONS.md Q4; compose cards into `MovieCarousel`; Title, PosterUrl fallback, optional ReleaseYear, verbatim ordered Reason chips; single descriptive native `/movies/{id}` anchor using existing SPA navigation and validated libraryReturnUrl handling; do not expose Score or add playback/feedback controls. Decorative poster alt when the title labels the same link.

- [X] T007 [P] [US1] Additive typed fixtures in `web/src/mocks/fixtures.ts` and, only as needed after T001, handler success/status smoke plus `web/src/api/client.typecheck.ts` exercises for the landed recommendations operation. Consume generated `web/src/mocks/handlers.ts`. If T001 proved regeneration is required, regenerate via C# export → `web/src/api/openapi.json` → `npm run generate:api`; never hand-edit generated outputs or duplicate a handler. Reuse existing/upstream smoke coverage rather than duplicating it.

**Checkpoint**: Named targeted proofs — `RelatedMovies.test.tsx` green for US1 populated chips/navigation and US2 empty/loading/error isolation. Contract fixtures/typecheck green for what T001 required.

---

## Phase 4: User Story 1 - MovieDetailsView additive integration and regressions

**Goal**: Mount RelatedMovies after MoreWithPanel and before FileSection; prove navigation and existing-section regressions.

**Independent Test**: Additive cases in `MovieDetailsView.test.tsx` prove RelatedMovies placement/integration, card navigation from the host view, and that MoreWithPanel and FileSection behavior remain intact.

### Tests

- [ ] T008 [US1] Extend `web/src/features/movie-details/MovieDetailsView.test.tsx` with additive integration/regression coverage: RelatedMovies appears after MoreWithPanel and before FileSection; populated recommendation card navigation; existing MoreWithPanel and FileSection behaviors still pass; host negative proofs that loading, failed, missing, and invalid Movie Details produce no recommendations request (preserve existing host states; do not invent a universal no-section rule). Same-file sequential work; do not parallelize conflicting edits with T009.

### Implementation

- [ ] T009 [US1] Integratively render RelatedMovies in `web/src/features/movie-details/MovieDetailsView.tsx` after MoreWithPanel and before FileSection (`recon-DEV-343:20`). Preserve all existing details functionality. No unrelated rewrites.

**Checkpoint**: Host integration green; existing details-section regressions green.

---

## Phase 5: Browser verification and polish — Cog refactor and Gauge gates

**Purpose**: Built-UI browser evidence for carousel/keyboard/responsive/reduced-motion; diff refactor; full applicable Phase 3 step 5 gate bar.

- [ ] T010 [US3] Built-UI browser verification of actual overflow, focus, keyboard behavior, responsive layout, reduced motion, and Previous/Next boundary remeasurement after scroll, resize, and changed children for MovieCarousel/RelatedMovies. Complements layout stubs; no new end-to-end framework or dependency. Persist a DEV-343 task-note receipt naming exercised cases, delivery HEAD, outcome, and evidence references.

- [ ] T011 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages).

- [ ] T012 Gauge gates: `npm run check:api`; `scripts/run-web-gates.ps1`; and all applicable task-pipeline Phase 3 step 5 gates for this ui slice. Record command, head, and actual exit/verdict. `./scripts/run-property-tests.ps1` honors `propertyTests: opt-out - no domain invariants in this presentation/contract-only change` (no-tests-tagged exit 2 only). Report SKIPPED (scope-empty) / SKIP / NOT APPLICABLE verbatim — never as PASS. Exit 1, Could not run, or unaccepted property exit 2 blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add an ADR. Do not claim Phase A gate results.

**Checkpoint**: Delivery ready for review loops under brief Q7 closing bar (Critical/High block; Medium/Low disposition; plan challenge and remediation each at most two rounds).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — Wisp recon must pass or stop for decision.
- **Phase 2 (US3 MovieCarousel)**: Depends on Phase 1; blocks RelatedMovies composition that mounts the carousel.
- **Phase 3 (US1/US2 RelatedMovies)**: Depends on Phase 2 export; blocks host integration that assumes the section exists.
- **Phase 4 (US1 host integration)**: Depends on Phase 3.
- **Phase 5 (Browser + Polish)**: Depends on Phases 2–4 complete.

### User Story Dependencies

- **US3 (P3)**: MovieCarousel — Phase 2; independently testable via reuse mount before RelatedMovies exists.
- **US2 (P2)**: Empty/loading/error — Phase 3 with RelatedMovies states.
- **US1 (P1)**: Populated chips + navigation — Phase 3 cards + Phase 4 host integration.

### Parallel Opportunities

- T002 can scaffold alongside T003 once the component API is named.
- T004 can scaffold alongside T005 once the landed operation shape from T001 is known.
- T007 fixture/typecheck work can proceed in parallel with T005/T006 after T001 clears the contract.
- T008 and T009 are same-folder host edits — run sequentially.

### Parallel Example: Phase 3 contract fixtures

```text
Task: "Additive typed recommendations fixtures in web/src/mocks/fixtures.ts"
Task: "Additive typed exercise in web/src/api/client.typecheck.ts (only if T001 requires)"
```

---

## Implementation Strategy

### MVP First (US3 carousel + US1/US2 RelatedMovies)

1. Complete Phase 1 recon (stop on divergence).
2. Complete Phase 2 exported MovieCarousel + focused RTL.
3. Complete Phase 3 RelatedMovies query/cards/states + MSW proofs.
4. **STOP and VALIDATE**: RelatedMovies independently before host integration.
5. Add Phase 4 MovieDetailsView integration + regressions.
6. Phase 5 browser verification, Cog refactor, Gauge gates.

### Incremental Delivery

1. Setup recon → foundation ready or blocked with cited decision.
2. MovieCarousel export → reusable keyboard rail (US3).
3. RelatedMovies section → chips, navigation, empty/loading/error (US1/US2).
4. Host integration → details page delivery.
5. Browser + refactor + gates.

### Notes

- [P] tasks = different files, no unfinished dependency.
- Do not invent route defaults, DTO fields, or handler duplicates beyond landed DEV-341 / T001 facts.
- Do not commit secrets; do not touch LocalPlay / Process.Start.
- Preserve brief `propertyTests: opt-out` disposition; do not add FsCheck suites for this ticket.
- Frozen scope (brief Q7) governs review findings.
- Copy and carousel micro-interactions come from `ASSUMPTIONS.md`; Palette owns visual spacing/layout without changing the interaction contract.
