# DEV-431 — Close DEV-322 K1 acceptance test gaps and fix pending search debounce cancellation on Clear/navigation

Source: YouTrack DEV-431 (from PR #166 comment `#6179617134` at `66beb8a`). Pipeline shape: the reviewed tracker plan below is the working plan; no DEV-431 spec/tasks pair is generated (CONCLUSIONS.md Decision 1).

## Context

DEV-322 is merged to `main`. Commit `44f5688` added K1 remediation tests, but the production fixes and several specific assertions below are still missing; re-verify each plan item against current code before coding (the plan was written at `66beb8a`; line numbers may have moved).

## Root-cause findings (verified at `66beb8a`, re-verified 2026-10-15)

1. **Pending debounce cancellation bug + missing regression tests** — `useDebouncedSearch` returns a bare `(value: T) => void` with no `.cancel()`; `FilterPanel` never cancels the pending timer on `Clear filters`, `popstate`, or external `query.text` change. Typing then clearing/navigating within 300 ms re-commits the stale text. Test gap: no cancellation tests at all.
2. **Equivalent explicit/omitted-default dedupe & fetch-once** — missing: `/` vs `/?sort=Title&direction=Ascending&page=1&pageSize=20` share one canonical query key (no second fetch), plus fetch-once for committed debounced text in `LibraryView`.
3. **Integrated page reset from `page=3`** — missing: from `/?text=dune&page=3`, a real filter interaction must produce both URL `page` omission and MSW request `page=1`.
4. **Range default-extent omission & complete group keys** — missing: `runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false`, include-unknown alone (`runtimeMin=&runtimeMax=&runtimeIncludeUnknown=true`), complete year-group keys (`yearMin=2000&yearMax=`, `yearMin=&yearMax=2010`), and extent-stop returns omitting whole groups.
5. **Status chips** — all 4 chips (`Pending`/`Enriched`/`Not found`/`Failed`), multi-status repeated `statuses=` keys in order, explicit empty/omitted assertions.
6. **Initial count placeholder** — assert `p[aria-hidden="true"]` inside `[aria-live="polite"]`, `/^\d+ movies?$/` null, `ul[aria-busy="true"]` before resolution; `2 movies` after.
7. **T051 empty/error copy** — `No movies match these filters.`, `0 movies`, `Movies could not be refreshed.` unasserted.
8. **(review) Controlled-mode navigation path** — `LibraryView` mounts `FilterPanel` controlled; the panel's popstate handler is uncontrolled-only. Cancelling in the render-time sync block is a render side effect; cancelling on every `query` prop change breaks the preserve-pending-discrete-commit test.
9. **(review) First-load error renders empty-result copy next to the error.**
10. **(review) Test-harness hazards** — default `retry: 3` exceeds timeouts; `queryByText(/movies/i)` matches facet suffixes (use anchored `/^\d+ movies?$/`); non-feeding `vi.fn()` setups break cumulative assertions; no `beforeEach` URL reset; plain fake timers hang RTL waits.

## Plan

### Step 1 — Production fixes (frozen file scope: `useDebouncedSearch.ts`, `FilterPanel.tsx`, `LibraryView.tsx` + their test files)
1. `useDebouncedSearch`: add identity-stable `cancel` (clears/resets `timerRef`); attach to the callback form (overloads: callback form returns `((value: T) => void) & { cancel(): void }`) — no new casts.
2. `FilterPanel`: `handleClear` calls `commitSearchText.cancel()` + `setSearchText('')` before committing. Register `popstate` in controlled mode too: on `popstate`, always `cancel()` and set the search box from `parseFilterQuery(window.location.search).text` (uncontrolled `setInternalQuery`/`onHydrate`/`onPopstate` stays uncontrolled-only). Never cancel in the render-time `query.text !== prevQueryText` block; never cancel on every `query` prop change (panel self-commits must keep pending text alive).
3. `LibraryView`: optional `queryClient?: QueryClient` prop on the default export, falling back to `new QueryClient()`.
4. `LibraryView`: render `No movies match these filters.` only when movie data exists with zero items; first-load error shows only the alert (keep fixed-dimension results area); refetch error keeps prior rows (`keepPreviousData` already retains `data`).
5. Stay inside the frozen file scope.

### Step 2 — FilterPanel.test.tsx + useDebouncedSearch.test.ts
1. Cancellation tests: cancelled by `Clear filters`; cancelled by popstate via a controlled `Host` (production path) for both `text` changing (`?text=restored`) and unchanged (`?genreIds=1`); keep `'debounced text commit preserves a newer discrete filter selection'` passing unchanged.
2. `useDebouncedSearch.test.ts`: `cancel()` stops the pending commit and the hook still schedules afterwards; `cancel()` with nothing pending is a no-op.
3. Range keys/extents (T040): uncontrolled or feeding-Host setup, cumulative interactions, `vi.setSystemTime` pinned for year extent; full runtime group key string, include-unknown alone, year full keys, extent-stop omission to `''`.
4. Status chips (T041): four chips, order-preserving repeated `statuses=` keys (`['NotFound','Failed','Pending','Enriched']` → `?statuses=NotFound&statuses=Failed&statuses=Pending&statuses=Enriched`), uncheck-all → `[]` and `''`.

### Step 3 — LibraryView.test.tsx
0. Harness: `beforeEach` URL reset; helper injecting `retry: false` QueryClient; debounce test loads initial data with real timers then fakes the typing window (or `shouldAdvanceTime: true`); no `findBy*`/`waitFor` under fully faked timers.
1. T024: `/` → explicit defaults via replaceState+popstate ⇒ `requestCount` stays 1; debounced text = exactly one additional fetch after 300 ms, none before.
2. Page reset from `?text=dune&page=3`: interaction ⇒ URL omits `page` AND MSW request is `text=dune&statuses=Pending&sort=Title&direction=Ascending&page=1&pageSize=20`.
3. End-to-end pending-search-cancelled-by-navigation: type `'stale'`, within 300 ms replaceState to `/?genreIds=1` + popstate ⇒ URL stays `?genreIds=1`, search box `''`, no request carried `text=stale`.
4. Initial placeholder assertions (anchored regex) + `2 movies` after resolve.
5. T051: empty result ⇒ `No movies match these filters.` + `0 movies`; first-load 500 ⇒ alert `Movies could not be refreshed.` and `queryByText('No movies match these filters.')` null; refetch 500 ⇒ alert + prior rows kept.

### Step 4 — Reconcile and gates
1. Verify T020/T021/T024/T040/T041/T051 (+T052/T053/T054) marks in `specs/DEV-322/tasks.md` match delivered evidence.
2. Gates: `pwsh -NoProfile -File ./scripts/run-web-gates.ps1` (lint, tsc, vite build, Vitest) exit 0; backend/repo gates if needed.

## Acceptance criteria

See the tracker plan's Acceptance Criteria section (all carried verbatim): cancellation on Clear/popstate incl. controlled mode and unchanged-text popstate; popstate hydrates the box from URL; pending commit survives panel self-commits; `useDebouncedSearch.test.ts` covers `cancel()`; first-load error shows alert without empty copy; refetch error keeps rows; harness (injected client, URL reset, no faked-timer waits); all 6 K1 bullets + T051 asserted; web gate exits 0.

## Loop discipline

- **Closing bar:** `$code-review`/`$ship-review` findings at Medium and above block; Low is noted, non-blocking.
- **Frozen scope:** production `useDebouncedSearch.ts`, `FilterPanel.tsx`, `LibraryView.tsx` (+ shared types they need); tests `FilterPanel.test.tsx`, `LibraryView.test.tsx`, `useDebouncedSearch.test.ts`; specs `specs/DEV-431/*` and reconciling `specs/DEV-322/tasks.md` marks. Anything else is a follow-up issue, not a finding in this round.
- **Round cap:** two remediation rounds; leftovers become follow-up issues.
