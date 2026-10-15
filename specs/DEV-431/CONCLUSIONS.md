# DEV-431 — Grill Conclusions

Source: YouTrack DEV-431 (PR #166 comment `#6179617134`, base commit `66beb8a`).
Branch: `feature/DEV-431-close-dev-322-k1-acceptance-test-gaps-and-fix-pending-search`, cut from `origin/main` (`e5587d0`) in worktree `F:\Dev\LamuFlix.worktrees\feature-DEV-431-close-dev-322-k1-acceptance-test-gaps-and-fix-pending-search`.

Pre-grill recon (2026-10-15, against worktree HEAD):

- DEV-322 is merged to `main`; the task's "after DEV-322 merges to main" precondition holds, so `main` is the correct base.
- `44f5688` ("DEV-322 - Remediate Phase 4 review finding K1 acceptance coverage") already added ~470 lines to `FilterPanel.test.tsx` / `LibraryView.test.tsx` and flipped T050/T051/T053 to `[X]` in `specs/DEV-322/tasks.md`.
- Still open per recon: production — no `cancel` on `useDebouncedSearch`, no cancellation in `handleClear`, popstate listener is uncontrolled-only, no `queryClient` injection, first-load error renders "No movies match these filters."; tests — cancellation (T1), URL-default dedupe + debounced fetch-once (T2), page=3 reset (T3), component-level full range keys + extent-stop omission (T4), 4 chips / repeated keys / empty omission (T5), initial count placeholder (T6), empty/error copy incl. first-load-error suppression and refetch-keeps-rows (T7), external `cancel()` tests (T8), harness (retry:false client, `beforeEach` URL reset, timer strategy) (T9).
- T024/T040/T041/T051 were marked `[X]` by `44f5688` but the specific assertions they describe are absent; marks must be reconciled to reality at the end (Step 4 of the plan).

---

## Decision 1 — Pipeline shape: brief + implement directly

**Question.** Full Spec Kit pipeline ($speckit-specify → $speckit-tasks → $implement) for DEV-431, or the tracker plan as `specs/DEV-431/brief.md` and straight to TDD implementation?

**Options considered.**
- Full speckit pipeline — generates a new spec/tasks pair; duplicates the already plan-reviewed description.
- Brief + implement directly — write `specs/DEV-431/brief.md` (+ this CONCLUSIONS.md) from the tracker plan, then TDD.
- Plan only, no DEV-431 specs — lose the in-repo settled-decisions artifact.

**Decision.** Brief + implement directly.

**Rationale.** The tracker description already contains a plan-reviewed (2026-10-15) step-by-step plan with root-cause findings, implementation steps, and acceptance criteria. Regenerating that into a spec/tasks pair adds ceremony without new information.

**Implications.** `specs/DEV-431/brief.md` carries the plan and the loop-terms subsection; no DEV-431 `spec.md`/`tasks.md` are generated; implementation follows the plan's Steps 1–4 with each plan item re-verified against current code before coding (plan line numbers predate `44f5688`). `specs/DEV-322/tasks.md` marks are reconciled only as described in the plan's Step 4.

---

## Decision 2 — Loop terms: Medium+ blocks, plan's file scope, two rounds

**Question.** What closing bar, frozen scope, and remediation round cap govern DEV-431's `$code-review`/`$ship-review` loop?

**Decision.**
- **Closing bar:** Medium and above blocks; Low findings may be noted without blocking.
- **Frozen scope:** the plan's file set — production: `web/src/features/library/useDebouncedSearch.ts`, `FilterPanel.tsx`, `LibraryView.tsx` (+ any shared types they need); tests: `FilterPanel.test.tsx`, `LibraryView.test.tsx`, `useDebouncedSearch.test.ts`; specs: `specs/DEV-431/*` and reconciling the `specs/DEV-322/tasks.md` marks. Anything else is a follow-up issue, not a finding in this round.
- **Round cap:** two remediation rounds; leftovers become follow-up issues.

**Rationale.** Matches the size and remediation nature of the task; these are the pipeline defaults for a scoped follow-up.

**Implications.** Review rounds that surface Low findings record them but do not reopen implementation; a second failed round closes the task with follow-up issues filed instead of a third round.

---

## Decision 3 — Grill closure

**Question.** Shared understanding reached — start implementation?

**Decision.** Confirmed. Proceed: Step 1 production fixes (useDebouncedSearch `cancel` + FilterPanel cancellation/controlled popstate + LibraryView queryClient prop and error/empty rendering), then TDD Steps 2–3, then Step 4 reconciliation and gates.

**Rationale.** All structural decisions settled: pipeline shape (Decision 1), loop terms (Decision 2), plan Steps 1–4 as the working plan with per-item re-verification against current code.

**Implications.** The two micro-choices the plan leaves open (Step 3.0's timer strategy — real-then-fake vs `shouldAdvanceTime: true`; and the exact controlled-`Host` shape for popstate tests) are resolved during implementation by whichever makes the tests pass reliably, then recorded here as an addendum. CONTEXT.md gains no new terms (no naming conflict found); no ADR (the controlled-mode popstate design is reversible and locally scoped).

---

## Addendum — implementation micro-choices and Step 4 reconciliation (2026-10-15)

**A1. `cancel` API is `{ schedule, cancel }`, not a callable-with-`.cancel`.** The plan's review preferred overloads `(callback form returns ((value: T) => void) & { cancel(): void })` "rather than adding another cast". That shape cannot be built under the repo's react-hooks compiler rules: `Object.assign(schedule, { cancel })` in `useMemo` fails `react-hooks/refs` (passing a ref-reading closure into a render-time call), direct assignment `commit.cancel = cancel` fails `react-hooks/immutability` (modifying a hook-derived value) as does assignment inside `useMemo`. The plan's listed alternative — an overload returning `{ schedule, cancel }: DebouncedSearch<T>` — was taken instead. No casts remain at the `FilterPanel` call site (the old `(commitSearchText as (value: unknown) => void)(...)` cast is gone); commit is `{ schedule, cancel }`, `cancel` is identity-stable (`useCallback([])`, only touches `timerRef`), so `FilterPanel`'s popstate effect depends on `cancelSearchText` without re-subscribing. `useDebounce`/`useDebouncedCallback` aliases were switched to `export { useDebouncedSearch as ... }` to preserve overloads.

**A2. Timer strategy: pure real timers in `LibraryView.test.tsx`.** The plan offered real-then-fake or `shouldAdvanceTime: true`. Neither was needed: with an injected `retry: false` `QueryClient`, MSW responses resolve fast and the 300 ms debounce window is affordable in real time, so the whole file stays on real timers — no `findBy*`/`waitFor`-under-fake-timers hazard at all. Fake timers are used only in `FilterPanel.test.tsx`/`useDebouncedSearch.test.ts`. `beforeEach` URL reset added to `LibraryView.test.tsx`; all renders go through a `retry: false` helper.

**A3. Controlled `Host` mimics `LibraryView`.** The `FilterPanel.test.tsx` controlled-host tests re-read `window.location.search` into panel state on `popstate` (the `LibraryView` behavior), proving cancellation through the production (controlled) path for both changed-text (`?text=restored`) and unchanged-text (`?genreIds=1`) navigation. `'debounced text commit preserves a newer discrete filter selection'` passes unchanged, proving panel self-commits still keep a pending text commit alive.

**A4. Last-successful-data kept render-phase, not in an effect.** `keepPreviousData` drops `data` to `undefined` on refetch error, so prior rows needed explicit retention (Finding 9's "prior rows kept" also covers the refetch case). A `useEffect`-based `setLastMovieData` fails the repo lint (`react-hooks/set-state-in-effect`, and the gate runs `--max-warnings 0`), so `LibraryView` adjusts state during render instead (`if (moviesQuery.data && moviesQuery.data !== movieData) setMovieData(...)` — the sanctioned prop-change pattern). First-load error: `movieTitles` is `[]` with no `moviesQuery.data` → only the alert, never the empty copy.

**A5. Step 4 reconciliation of `specs/DEV-322/tasks.md`.** No mark edits were needed, but four marks changed meaning: T024/T040/T041/T051 were flipped to `[X]` by `44f5688` while their described assertions were absent; this task supplies the missing assertions (explicit-default dedupe + debounced fetch-once; full runtime/year group keys + extent-stop omission; all-4-chips order-preserving `statuses=` keys + empty omission; empty copy + `0 movies` + first-load-error suppression + refetch-keeps-rows), so `[X]` is now backed by evidence. T020/T021 `[X]` remain backed by unchanged passing tests. T052 `[ ]`, T054 `[ ]` unchanged: this branch changes no production C# under `src/` (backend gates are scope-empty; mutation is not run). T053 `[X]` is re-earned here.

---

## Addendum — remediation round 1 (PR #169 review, 2026-10-15)

**R1-G1 [Medium · coverage] — accepted and closed in one round (cap two, one spent).** The reviewer's probe was reproduced exactly: with `handleYearMin` mutated to clear the active `yearMax`, the year-extent test failed at the new intermediate assertion (`expected '' to be '?yearMin=&yearMax=2010'`) and passed unmutated (32/32). The round diff vs `ROUND_BASE 118c7acf` is the 2-line test assertion plus this bookkeeping; production code untouched. `$code-review` re-review of the round range declined as C#-only with zero findings (declined artifact beside the stage artifact in temp `$pr-review`). Full `$verify`: build PASS; `dotnet test` PASS (13 + 686 + 380, 1 ignored); property tests PASS (23); vulnerable-packages PASS; `dotnet format` PASS; Roslyn/complexity/InspectCode/mutation SKIPPED scope-empty (non-blocking, zero C# in diff); diff review PASS; web gate exit 0.
