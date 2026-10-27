# Tasks: DEV-422 SignalR enrichment-status client hook

**Input**: Design documents from `specs/DEV-422/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). No Palette `design.md` (no ui tag; visuals out of scope).

**Tests**: Included. Brief Test strategy requires Vitest + RTL connection-fake coverage for lifecycle, payload/cache, details bridge, mutation races, and Pending live-region behavior. Author tests in the same phase as the surfaces they lock (web-implement TDD). Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 prerequisite recon passes, the spec PR has merged, Gate 1 is established (owner checkbox answered), and Phase B prerequisites (landed approved upstream constitution disposition/amendment, landed DEV-330 hub implementation/generated chain, drift check) allow build intake. **propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants** (brief Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings). Vitest matrix remains mandatory; opt-out is never PASS; no new property-testing library.

**Organization**: Phases follow brief plan ordering and `spec.md` user stories US1–US4. Anvil implements one phase per ask. Polish is Cog then Gauge.

**Domain vocabulary** (retained; do not edit CONTEXT.md): **enrichment status**, **Pending**, **enrichment-status event**, **reconnect/refetch** — definitions in `spec.md` Domain vocabulary.

**Owner checkbox** (Gate 1): `blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?` — must be answered before Phase B production.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 live events without polling; US2 session connection/reconnect; US3 details bridge; US4 polling removal / import-retry convergence.
- Exact file paths are in each description.

## Phase 1: Setup — owner gate, upstream contract, and pin recon

**Purpose**: Confirm Gate 1 and landed DEV-330 contract before production work; identify the exact npm patch to pin.

- [ ] T001 Wisp prerequisite/drift recon against picked HEAD: confirm owner checkbox on the DEV-422 spec PR is answered; confirm landed approved upstream DEV-330 constitution disposition/amendment (existing DEV-330 constitution-departure owner gate and Proposed ADR-0029 resolved upstream — do not invent a duplicate constitution checkbox on DEV-422; constitution editing stays out of scope); confirm DEV-330 Gate 1 resolved and hub implementation landed; confirm generated `EnrichmentStatusChangedDto` (or owner-approved alternative) present in `web/src/api/openapi.json` / `web/src/api/types.ts` with shape `{ id: number, enrichmentStatus: Pending|Enriched|NotFound|Failed }` and no MSW path handler required for hub-only schema; confirm contract drift check path (`npm run lint` / `check:api`) against main; confirm absence-to-presence of hub route `/hubs/enrichment` and event `EnrichmentStatusChanged` relative to planning evidence PR #264 / `b565e932` (planning evidence only until landing); list the exact published compatible `10.0.x` `@microsoft/signalr` patch available at pickup (do not invent). Gauge supplies applicable baseline. Keel supplies pickup/drift clearance. Stop for a cited decision on missing prerequisites or any contract mismatch rather than inventing a substitute. No production edits in this task.

**Checkpoint**: Owner checkbox answered; landed approved upstream constitution disposition/amendment; DEV-330 hub implementation/generated chain landed and drift-checked; exact npm patch identified; Phase B build intake is allowed.

---

## Phase 2: Foundational — pin `@microsoft/signalr`

**Purpose**: Add the exact published pin and lockfile entry identified in T001. No hook yet.

- [X] T002 Pin exact published `@microsoft/signalr` `10.0.x` (patch from T001) with `--save-exact` in `web/package.json` and update `web/package-lock.json` so neither package nor lock root carries a caret/tilde range. No other dependency changes. No server NuGet package. After pin, run existing `npm audit` and record DEV-422-introduced findings with disposition; do not blanket-fail unrelated historical advisories or alter thresholds. Confirm browser runtime imports and `import type` for type-only symbols will be used by consumers; Node-only transports must not enter browser runtime (verified with T013).

**Checkpoint**: Lockfile contains the exact pin with no ranges at package/lock root; `npm` install/resolves cleanly in `web/`; audit disposition for introduced findings recorded.

---

## Phase 3: User Story 2 - Connection lifecycle hook and root mount (Priority: P1)

**Goal**: `useEnrichmentStatus` with instance-owned connection/timer/registry, injected factory (default private helper), narrow subscription return; single session connection; URL/no-credentials; handlers before start; StrictMode-safe cleanup including unsubscribe; automatic reconnect plus outer retry schedule; mount once under root QueryClientProvider; pass subscription seam via existing main/App props.

**Independent Test**: `useEnrichmentStatus.test.tsx` proves one active connection across routes/StrictMode, URL/no-credentials options, handler registration/disposal, failed starts, reconnect exhaustion/backoff, timer and late-start cleanup races, and teardown with zero fake live connections/pending timers before restoring fake timers (brief Test strategy; Phase 2 plan-challenge item 2).

### Tests

- [ ] T003 [P] [US2] Write Vitest/RTL proofs in `web/src/library/useEnrichmentStatus.test.tsx` using a hand-written connection fake (no new package): one active connection across remount/StrictMode; hub URL built from `VITE_API_BASE_URL || window.location.origin` with path `/hubs/enrichment`; `withCredentials: false` and no auth headers; handlers registered before start and disposed on cleanup; failed initial start and exhausted reconnect follow outer delays `1s, 2s, 4s, 8s, 16s` then cap `30s` until dispose; late start after disposal writes no cache updates; only one start/retry owner; teardown asserts zero fake live connections and zero pending timers before restoring fake timers. AAA headers only. Do not use MSW SignalR emulation, a real network connection, or a new fake package.

### Implementation

- [ ] T004 [US2] Add `web/src/library/useEnrichmentStatus.ts`: keep connection, timers, and callback-subscription registry per mounted root hook instance (no module-global mutable owner/registry); inject connection factory via hook parameter/default seam with the default factory helper private in this file; return a narrow subscribe/unsubscribe seam; build `/hubs/enrichment` URL; `withCredentials: false`; default `withAutomaticReconnect()`; cancellable outer retry per ASSUMPTIONS.md; register event/lifecycle handlers before `start()`; dispose handlers, stop connection, cancel timers/in-flight start, and clear subscription consumers on cleanup; single start/retry owner. Publish valid events and start/reconnect recovery through the subscription seam independently of browse-cache presence or status changes. Consume hub payload type per owner-checkbox outcome (generated `EnrichmentStatusChangedDto` preferred); never hand-edit generated files; no new context/provider/store/event-bus module.

- [ ] T005 [US2] Mount `useEnrichmentStatus` once under `web/src/main.tsx` root `QueryClientProvider` (shell survives Library/Details route changes). Pass the returned narrow subscription seam through existing `web/src/main.tsx` → `web/src/App.tsx` props toward movie-details without a second connection, new QueryClient, new context/provider/store, or reverse invalidation cycle. Add focused proofs in `web/src/App.test.tsx` that the shell hosts a single session connection across route changes and forwards the subscription seam.

**Checkpoint**: Named targeted proofs — `useEnrichmentStatus.test.tsx` and focused `App.test.tsx` green for US2 lifecycle/mount.

---

## Phase 4: User Story 1 + User Story 2 + User Story 3 - Event cache updates, reconnect recovery, details bridge (Priority: P1/P2)

**Goal**: Zod-validate events; patch/invalidate browse without coalescing away in-flight-event freshness; start/reconnect invalidate including Pending; one-way details bridge via passed subscription seam without a second connection or invalidation cycle; retain event patch on failed refetch.

**Independent Test**: Connection-fake proofs in `useEnrichmentStatus.test.tsx` for payload/cache/freshness matrix; focused `MovieDetailsView.test.tsx` for absent-browse id, no-event start/reconnect, all transitions, same-status recovery, R1/E2 freshness, one-way bridge (brief Test strategy; Phase 2 plan-challenge items 1, 3, 5).

### Tests

- [ ] T006 [P] [US1] [US2] Extend `web/src/library/useEnrichmentStatus.test.tsx` for event/cache/recovery: ignore malformed payloads with explicit matrix — null/missing/string/fractional/non-finite `id`, unknown status, zero arguments, and two-or-more arguments; all four exact status strings patch matching existing `['browseMovies']` rows via `setQueriesData` and invalidate browse variants; duplicates and absent rows never fabricate rows; multiple filtered cache variants converge; metadata/filter/sort via invalidation; HTTP/event races settled by invalidation/refetch; coalesce concurrent refresh without dropping a valid event that arrives during an older refetch — prove R1 starts, E2 arrives, stale R1 completes, then fresh browse state converges (no synthetic server sequence); successful initial start and reconnect invalidate browse including Pending; failed authoritative refetch retains the in-place event patch until a subsequent event or recovery refetch (no rollback). AAA headers only. Do not impose an uncontracted positive-only id rule.

- [ ] T007 [P] [US3] Write focused Vitest/RTL proofs in `web/src/features/movie-details/MovieDetailsView.test.tsx` for the one-way bridge via the passed subscription seam: details id absent from all browse caches still reconciles on a valid event; explicitly prove successful initial-start and reconnect reconciliation with no event for an absent-browse id; all status transitions; same-status/duplicate recovery independent of browse presence/status diffs; R1-start then E2 then stale R1 completion then fresh mounted-details convergence; cleanup unsubscribes; no second hub connection; no feedback cycle from details bridge back into root invalidation alone.

### Implementation

- [ ] T008 [US1] [US2] In `web/src/library/useEnrichmentStatus.ts` (and any minimal helpers kept in that file), implement Zod-validated `EnrichmentStatusChanged` handling with existing Zod: require exactly one positional argument, non-null object, finite integer `id`, and one of the four exact status strings; ignore the malformed matrix from T006; no new validation dependency, no duplicate DTO, no uncontracted positivity rule. Patch only `enrichmentStatus` on matching existing browse items across root `['browseMovies']` variants; invalidate all browse variants; coalesce concurrent refresh without swallowing an event that arrives during an older refetch (fresh authoritative pass after E2); retain event patch when refetch fails and converge on subsequent event/recovery — no rollback machinery; do not touch recommendation caches; on every successful initial start and `onreconnected`, invalidate root browse including Pending and publish through the subscription seam independently of cache presence/status.

- [ ] T009 [US3] Implement one-way root→details reconciliation in `web/src/features/movie-details/movieDetailsQueries.ts` and `web/src/features/movie-details/MovieDetailsView.tsx`: receive the narrow subscription seam via existing props from `App.tsx`; subscribe on mount and unsubscribe on cleanup; keep nested details QueryClient; on published valid events and start/reconnect recovery notifications invalidate mounted `['movieDetails', movieId]` independently of browse-cache presence or status changes; works when id absent from browse pages, on no-event start/reconnect, and on same-status recovery; honor freshness so an event during an older details refetch still yields a fresh pass; no second connection; no new context/store/module; no cycle back to root invalidation.

**Checkpoint**: Event/cache/recovery and details-bridge named proofs green.

---

## Phase 5: User Story 4 - Remove polling; import/retry convergence (Priority: P2)

**Goal**: Remove browse enrichment polling; preserve optimistic Pending helpers; mutation success invalidates browse; event/mutation races converge; repurpose polling/retry tests; preserve visuals and live region.

**Independent Test**: Repurposed `libraryQueries.polling.test.tsx` and updated `LibraryView.retry.test.tsx` prove no interval-driven browse requests, event-before-mutation and mutation-before-event convergence, optimistic import/retry reconciliation, terminal metadata refresh, and preserved `aria-live="polite"` / status labels (brief Q5).

### Tests

- [ ] T010 [US4] Repurpose `web/src/library/libraryQueries.polling.test.tsx` in place: remove `browseRefetchInterval` / 2s cadence assertions; prove no interval-driven browse requests; cover event/invalidation reconciliation including event-before-mutation-response and mutation-before-event; concretely assert `aria-live="polite"` remains present/green after polling removal (not merely coverage intent). AAA headers only. Do not delete the file.

- [ ] T011 [P] [US4] Update `web/src/library/LibraryView.retry.test.tsx` in place: replace assertions that `browseRefetchInterval(...) === 2000` with no-polling / invalidation reconciliation expectations after retry marks Pending. Preserve status label/visual expectations.

### Implementation

- [ ] T012 [US4] In `web/src/library/libraryQueries.ts`, remove `browseRefetchInterval` and browse `refetchInterval` / `refetchIntervalInBackground` options. Preserve `pendingRow`, `markRowPending`, `armImportPlaceholder` / `reconcileImportPlaceholder` / row-id reconciliation. On successful import and retry/request-enrichment/match mutations, immediately `invalidateQueries({ queryKey: ['browseMovies'] })` (coordinate with `web/src/App.tsx` and `web/src/features/movie-details/movieDetailsQueries.ts` call sites as required by brief Q5). Do not restore HTTP polling timers.

**Checkpoint**: No-polling and mutation-race named proofs green; Pending visuals and live region preserved.

---

## Phase 6: Polish — full web suite, Cog refactor, and Gauge gates

**Purpose**: Full web verification; honest live-hub e2e reporting; diff refactor; applicable gates.

- [ ] T013 Run from `web/`: `npm run lint` (includes check:api, ESLint, Prettier), full Vitest suite, and production build. Record production-build output size delta against baseline using existing build output — no new bundle tooling or budget. Confirm browser runtime imports and `import type` for type-only symbols; confirm Node-only transports are absent from browser runtime output. Reconcile/record post-pin `npm audit` disposition for DEV-422-introduced findings (from T002); do not blanket-fail unrelated historical advisories or alter thresholds. Fix only in-scope failures. If `web/e2e/import.e2e.ts` must change for refresh behavior, update only as required and run against a real landed hub when available; if the live hub is unavailable, record `Could not run`, never PASS. Do not invent gate flags, thresholds, or exemptions.

- [ ] T014 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages beyond the approved pin).

- [ ] T015 Gauge gates: web lint/test/build bar above; and all applicable task-pipeline Phase 3 gates for this web-only slice. Record command, head, and actual exit/verdict. **propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants** (brief Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings). Keep the mandatory Vitest connection-fake matrix; record the propertyTests opt-out with actual gate exit reporting — opt-out is never PASS; add no property-testing library. No backend mutation run is required for a web-only diff. C# scope-empty analyzer/mutation exits are `SKIPPED (scope-empty)`, never PASS. Report SKIPPED / SKIP / NOT APPLICABLE / Could not run / opt-out verbatim — never as PASS. Exit 1 and Could not run remain blocking. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add/promote an ADR. Do not claim Phase A gate results.

**Checkpoint**: Delivery ready for review loops under brief Loop discipline (High/Medium block; Low/Informational defer unless concrete acceptance violation; round cap two; owner checkbox and upstream pickup are independent prerequisites, not review dispositions).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — owner checkbox, landed approved upstream constitution disposition/amendment, DEV-330 hub/generated-chain landing, drift check, and npm patch identification must pass or stop for decision.
- **Phase 2 (Pin)**: Depends on Phase 1; blocks hook implementation that imports `@microsoft/signalr`.
- **Phase 3 (US2 lifecycle/mount)**: Depends on Phase 2; blocks event handlers that assume a live connection owner.
- **Phase 4 (US1/US2/US3 cache + bridge)**: Depends on Phase 3.
- **Phase 5 (US4 polling removal)**: Depends on Phase 4 event/invalidation path existing so tests can assert reconciliation without intervals.
- **Phase 6 (Polish)**: Depends on Phases 2–5 complete.

### User Story Dependencies

- **US2 (P1)**: Session connection — Phase 3; independently testable with connection fake before cache logic lands.
- **US1 (P1)**: Live events — Phase 4; needs US2 connection owner.
- **US3 (P2)**: Details bridge — Phase 4; needs root event/recovery notifications.
- **US4 (P2)**: Polling removal — Phase 5; needs event/invalidation path from US1/US2.

### Parallel Opportunities

- T006 and T007 can proceed in parallel after Phase 3 connection seam exists.
- T010 and T011 can proceed in parallel during Phase 5 test authoring (different files); T012 implementation coordinates shared query/mutation files sequentially with those tests.
- T003 can scaffold alongside T004 once the pin from T002 is present.

### Parallel Example: Phase 4 proofs

```text
Task: "Event/cache/recovery proofs in web/src/library/useEnrichmentStatus.test.tsx"
Task: "Details bridge proofs in web/src/features/movie-details/MovieDetailsView.test.tsx"
```

---

## Implementation Strategy

### MVP First (US2 + US1)

1. Complete Phase 1: Setup recon
2. Complete Phase 2: npm pin
3. Complete Phase 3: lifecycle hook + root mount
4. Complete Phase 4: event cache + reconnect recovery + details bridge
5. **STOP and VALIDATE**: Live status without polling works under connection-fake tests
6. Complete Phase 5: remove polling / import-retry convergence
7. Complete Phase 6: verification and gates

### Incremental Delivery

1. Setup + pin → dependency ready
2. US2 connection → independently testable session lifecycle
3. US1 + US3 cache/bridge → event-driven refresh and details convergence
4. US4 polling removal → DEV-80 interval gone with preserved optimistic UX
5. Each phase adds value without expanding frozen scope

### Parallel Team Strategy

Not assumed. Single Anvil stream per phase unless Conductor assigns otherwise.

---

## Notes

- [P] tasks = different files, no unfinished dependencies
- [Story] labels map to spec.md US1–US4
- Gate 1 owner checkbox, landed approved upstream constitution disposition/amendment, and DEV-330 hub/generated-chain landing are Phase B STOP conditions, not silent assumptions
- Exact `@microsoft/signalr` patch comes from T001 pickup, never invented in Phase A
- propertyTests opt-out is recorded for T015; Vitest matrix remains mandatory; opt-out never PASS
- Avoid: vague tasks, file-scope expansion, restoring browse polling, hand-edited generated types, second hub connection, module-global mutable registry, new context/provider/store/event-bus module, reverse invalidation cycle, recommendation-cache writes, uncontracted positivity id rule, duplicate DTO, rollback machinery, visual Pending redesign, new ADR/CONTEXT.md edits, duplicate constitution owner checkbox, constitution editing
- Deferred Lows per brief Phase 2 plan-challenge stay deferred (`noted, no ticket`): Ledger L5/L6, Sentry L1/L2/L5, informational acknowledgements — do not expand tasks for them
