# Implementation Plan: SPA cleanup (dead exports, navigation, cohesive splits)

**Branch**: `feature/475-spec` | **Date**: 2026-11-14 | **Spec**: `specs/DEV-475/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q13); `CONCLUSIONS.md` including Q12 and Q13 (Q13 commit `b3b70c15`); `ASSUMPTIONS.md`; proposed `docs/adr/0042-explicit-spa-navigation-and-cleanup-boundaries.md`; Maestri notes `DEV-475`, `recon-DEV-475`; `specs/PRODUCT.md`; `PLAN_ADJUDICATION.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Independent analyze reread CLEAN (2026-11-14 20:00). Plan-challenge adjudication **NEEDS FIXES — NOT PLAN FROZEN** (`PLAN_ADJUDICATION.md` Q13 disposition / governance repair verification). Quill plan-repair round 2 applied; Q13 governance consistency applied across Quill-owned artifacts. Gate 1 remains closed pending the unanswered structural owner checkbox for retaining runtime `role=actor` on `GET /people`, plus ordinary owner Gate 1. Implementation, runtime acceptance, and merge remain distinct and open. Quill does not invent PLAN FROZEN or claim checkbox approval. ADR 0042 remains **proposed** (acceptance not manufactured by packaging).

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `checklists/cleanup-navigation-a11y.md`, `DRAFTING_RECEIPT.md`, `_quill-repair-report.txt`. No `research.md`, `data-model.md`, or `contracts/` — no public API/schema design delta; decisions from brief Q1–Q13 and CONCLUSIONS Q12–Q13.

## Summary

Clean up SPA agent sprawl in `web/`: remove proven dead aliases/exports and the ticket-named `libraryUrl` pair after inventory (complete `filterQuery` importer set `recon-DEV-475` lines 12–14; ticket authority lines 39–58); replace synthetic production popstate with `web/src/features/app-navigation/appNavigation.ts` shared `openMovie` and one programmatic push/replace notification beside `useAppNavigation` (Q11), with atomic producer/listener cutover and `appNavigation.test.ts` before cutover; retain GetPeople `role=actor` via typed helper while removing inline casts (Q12); literal-type existing sort/direction; split FilterPanel, LibraryView, and MovieDetailsView into orchestration shells plus Q10/Q12-ratified cohesive siblings (`DualRangeField` owns ranges; `FilterSortControls` sort/direction only; `LibraryFilterSummary` exports pure chip builder). Preserve all named behaviours and distinct return-URL/playerIntent contracts. Defer Tailwind to **DEV-511** and jsx-a11y to **DEV-512**. Prove delivery with existing web lint, typecheck, unit, and e2e (SMOKE mapped to named scenarios). Finish at awaiting user merge within Size L closing bar and 2×2 caps.

## Technical Context

**Language/Version**: TypeScript strict + React 19 SPA under `web/` (`specs/PRODUCT.md` §1; constitution.md:760-761).

**Primary Dependencies**: Existing Vite/React/TanStack Query/Zod/openapi-typescript/openapi-fetch stack. **No new npm dependency** in this delivery. Q10 defers `eslint-plugin-jsx-a11y` (do not install 6.10.2, `@eslint/compat`, peer overrides, or ESLint downgrade). No Tailwind change (`brief.md` Q1/Q10). Manifest/lockfile/config paths for evidence: `web/package.json` (Node `>=24 <25`, ESLint 10.14.0), `web/package-lock.json`, `web/eslint.config.js`.

**Storage**: N/A for this cleanup (browser history/URL only; no schema).

**Testing**: Existing Vitest + React Testing Library + MSW; existing e2e infrastructure (`brief.md` Q7; constitution.md:760-761). No new test framework. Named unit homes: `web/src/features/app-navigation/appNavigation.test.ts` (new; beside mechanism, before cutover; `.test.tsx` only if JSX needed), `web/src/features/app-navigation/useAppNavigation.test.tsx`, `web/src/features/library/filterQuery.test.ts`, `web/src/features/library/FilterPanel.test.tsx`, `web/src/library/LibraryView.composed.test.tsx`, `web/src/library/LibraryView.toolbar.test.tsx`, `web/src/library/MovieCard.test.tsx` (MovieCard fallback regression home), `web/src/features/movie-details/MovieDetailsView.test.tsx`; App tests and browseWatchlist/watchlist integration tests as consumers. Pure chip-builder unit coverage lives with `LibraryFilterSummary` extraction. Tracked e2e tree under `web/e2e/` (dated snapshot of 15 tracked paths excluding `.gitignore`; not an acceptance threshold): `browse.e2e.ts`, `card-actions-row.e2e.ts`, `compose.e2e.yml`, `dev454-subtitles.e2e.ts`, `dev455-playback.e2e.ts`, `fixtures.mjs`, `for-you-review.e2e.ts`, `genre.e2e.ts`, `import.e2e.ts`, `library-path.e2e.ts`, `manual-match.e2e.ts`, `run.mjs`, `stream-seek.e2e.ts`, `stub.mjs`, `tsconfig.json`. SMOKE map: SMOKE-001 → `browse.e2e.ts` / `genre.e2e.ts` candidates; SMOKE-002 → `dev455-playback.e2e.ts` (+ return-URL extension if missing); SMOKE-003 → named Back/Forward extension required (`recon-DEV-475` lines 25–27).

**Target Platform**: LamuFlix web SPA (Compose `docker compose up` stack for e2e/smoke).

**Project Type**: Existing `web/` SPA feature folders; frontend-only cleanup; no backend/OpenAPI edits preapproved.

**Performance Goals**: No behaviour-change cleanup; no new latency targets. Preserve current navigation/filter responsiveness qualitatively via regression coverage (`brief.md` Q7).

**Constraints**: Charter §2.3 / `PRODUCT.md` §5 care list — no new dependency this delivery (Q10); no new router/state layer (Q2/Q11); no OpenAPI/generated-contract change (Q5/Q12); no LocalPlay/secrets/Process.Start; whole-file deletion beyond named pair needs Patron ruling (Q4). Honest SKIPPED/SKIP/N/A/Could not run. User owns merges; only Keel declares PLAN FROZEN. Historical worktree provenance for intake: `F:/Dev/LamuFlix.worktrees/feature-475-spec` on `feature/475-spec` — execution commands below are **repo-relative** from `web/`.

**Scale/Scope**: Size L frontend cleanup; frozen delivery per Q1 as superseded by Q10 (jsx-a11y deferred); Tailwind sole ticket item deferred as DEV-511; jsx-a11y deferred as DEV-512. Historical component/file line counts in recon are dated snapshots, not acceptance bars.

### Exact web verification commands (repo-relative from `web/`)

| Lane | Command |
|---|---|
| lint | `npm run lint` → `npm run check:api && eslint . --max-warnings 0 && prettier --check .` |
| unit | `npm test` (or `npm run test:run`) → `vitest run` |
| e2e | `npm run e2e` → `node e2e/run.mjs` |
| typecheck | No standalone `typecheck` script. TypeScript checks used by `npm run build`: `tsc --noEmit && tsc --noEmit -p tsconfig.node.json && npm run typecheck:e2e` (`typecheck:e2e` → `tsc --noEmit -p e2e/tsconfig.json`). Delivery typecheck evidence runs those `tsc --noEmit` invocations (or the full `npm run build` typecheck prefix) without inventing a missing script name. |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Planned compliance ≠ accepted ADR.*

Gates checked against `.specify/memory/constitution.md` (worktree current). **Q13 structural owner checkbox required** (unanswered — Gate 1 stays closed): authorize retaining runtime `role=actor` on `GET /people` via the shared literal-typed helper while removing inline casts (`CONCLUSIONS.md` Q13; Charter §2.3(a); Wanted #3 vs Done-when / backend 422). Spec PR MUST carry that checkbox verbatim. Ordinary Gate 1 remains pending. Quill does not invent PLAN FROZEN or claim checkbox approval.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I–V (ports/handlers/query/domain/errors) | Backend architecture | **N/A** — no C# / API / schema work in frozen scope. |
| VI (Observability) | Safe diagnostics | **PASS (planned)** — no new telemetry surface required for this cleanup. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS (planned)** — no LocalPlay, secrets, or Process.Start touches; local-play orchestration state stays in MovieDetailsView shell without relocating execution. |
| VIII (Ubiquitous Language) | CONTEXT.md / ADRs | **PASS (planned)** — no CONTEXT.md glossary changes (Q9). Proposed ADR 0042 already authored by Keel; Quill does not write ADRs. **ADR acceptance pending** — not unqualified PASS. |
| IX (Test Pyramid) | Meaningful layers | **PASS (planned)** — existing Vitest/RTL/MSW + e2e; no new framework; both imperative and native navigation covered. |
| Web stack | Vite/React/TS/TanStack/Zod/openapi chain | **PASS (planned)** — stay on constitution.md:760-761 stack; preserve OpenAPI→TS chain; no new npm dependency this delivery (Q10); GetPeople role retained via typed helper without contract edits (Q12). |
| Governance / web gates | Honest exits | Phase B: existing web lint/typecheck/unit/e2e (commands above); web-gates scripts as scoped; C# analyzer gates likely SKIPPED (scope-empty) and must stay SKIPPED never PASS; Could not run e2e blocks Done-when. Closing bar and 2×2 caps per Q8. |

### Complexity Tracking

No constitution violations requiring justification. jsx-a11y is an explicit deferred follow-up (Q10/DEV-512), not a silent omit. Tailwind disposition is verified follow-up DEV-511. ADR 0042 remains proposed.

## Approach

- **Plan repair round 2 (this round)**: Apply Keel 9-item consolidated plan-challenge list using Q12 and current recon; do not edit Keel/Patron-owned brief/ASSUMPTIONS/ADR/CONCLUSIONS.
- **Six-step delivery** (`brief.md` Ordered task plan; Q10 removes original Q7 step 6): (1) inventories + web baseline → (2) regression characterization where coverage insufficient → (3) orphan/alias + type cleanup (Q12 typed GetPeople helper) → (4) shared navigation with atomic producer/listener cutover → (5) three cohesive extractions (Q12 ownership) → (6) final web verification.
- **Navigation contract**: `appNavigation.ts` owns shared `openMovie` and one programmatic push/replace subscriber-notification mechanism; `useAppNavigation.ts` retains React route-state and native popstate and wires subscription before producers stop; no fake events; no new router/state layer; preserve distinct return-URL/playerIntent policies; retain MovieCard wrapper import (ADR 0042 proposed; Q11; Q2; recon lines 16–23).
- **Deletion contract**: Inventory first (complete importer set lines 12–14); delete named pair + proven unreferenced exports; preserve live filterQuery tests; stop for Patron ruling beyond named pair.
- **Extraction contract**: Q10-ratified sibling paths; Q12 DualRangeField/FilterSortControls/chip-builder ownership; shells retain orchestration and URL/effect ownership; siblings receive callbacks, no local history/popstate.
- **Review / ship prep**: Size L three-axis review within 2×2 caps; PR for user merge; awaiting-merge — never merge; Quill does not declare PLAN FROZEN.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-475/
├── brief.md                    # Keel-owned (Q12 sync pending)
├── CONCLUSIONS.md              # Patron-owned (Q12 recorded)
├── ASSUMPTIONS.md              # Keel-owned
├── PLAN_ADJUDICATION.md        # Keel-owned
├── spec.md
├── plan.md                     # This file
├── tasks.md
├── DRAFTING_RECEIPT.md
├── _quill-repair-report.txt
└── checklists/
    ├── requirements.md
    └── cleanup-navigation-a11y.md

docs/adr/
└── 0042-explicit-spa-navigation-and-cleanup-boundaries.md  # proposed (Keel)
```

### Source Code (approved known paths — `brief.md` Files touched; Q10–Q12)

```text
web/src/library/
├── libraryUrl.ts                 # planned deletion after inventory
├── libraryUrl.test.ts            # planned deletion after inventory
├── LibraryView.tsx               # orchestration shell
├── MovieCard.tsx                 # retain openMoviePlayer wrapper import
├── MovieCard.test.tsx            # MovieCard fallback regression home
├── LibraryBrowseView.tsx         # NEW — browse props; view variant/panel/focus/hidden ids/grid focus
├── LibraryFilterSummary.tsx      # NEW — chips + exported pure chip-builder helper (Q12)
├── LibraryResultsToolbar.tsx     # NEW — sort/view/panel controls
└── LibraryResultsBody.tsx        # NEW — formalize ResultsBody presentation

web/src/features/library/
├── filterQuery.ts                # alias/shim cleanup + compatible types
├── FilterPanel.tsx               # orchestration shell (query sync, year snapshot, facets, public interface; rating/runtime/year fieldsets consume DualRangeField)
├── DualRangeField.tsx            # NEW/extract — owns existing local range control (Q12)
├── FilterSearchField.tsx         # NEW — search text draft/debounce/suggestions; cancel on nav/unmount
├── FilterFacetCombobox.tsx       # NEW — per-instance facet draft, active index, refs, keyboard nav
├── FilterSortControls.tsx        # NEW — sort field + sort direction ONLY (Q12)
└── (existing FilterPanel / filterQuery tests)

web/src/features/movie-details/
├── MovieDetailsView.tsx          # orchestration shell (route/load/refresh, client, local-play state, dialogs)
├── RelatedMovies.tsx             # navigation integration
├── movieDetailsQueries.ts        # GetPeople typed helper consumer (Q12)
├── MoviePoster.tsx               # NEW — image-failure state
├── ManualMatchControl.tsx        # NEW — match-open/trigger focus
├── CastSection.tsx               # NEW — existing cast section
├── MoreWithPanel.tsx             # NEW — related presentation
├── MorePosterCard.tsx            # NEW — card image-failure state
└── FileSection.tsx               # NEW — copy feedback

web/src/features/app-navigation/
├── useAppNavigation.ts           # React route-state + native Back/Forward popstate; subscribe before cutover
├── appNavigation.ts              # NEW (Q11) — shared openMovie + programmatic push/replace notifications
├── appNavigation.test.ts         # NEW — mechanism suite before cutover
└── useAppNavigation.test.tsx

web/src/App.tsx                   # listener / navigation integration
web/src/library/browseParams.ts   # filterQuery importer (inventory only unless alias cleanup requires)
web/src/features/movie-details/movieDetailsLabels.ts  # filterQuery importer (inventory only unless required)

# Q10 removes package/lockfile/ESLint and violation-file work from this delivery
```

**Ownership boundary (Q10 / Q12)**: query/URL/route/data and existing local-play orchestration stay in shells; local draft, focus, failure, and copy-feedback state stay with the cohesive sibling. Values/callbacks cross boundaries without duplicate URL/effect state. `DualRangeField` owns ranges; `FilterSortControls` owns only sort/direction; `LibraryFilterSummary` exports pure chip builder. Do not relocate local-play execution or change its feature gate.

**GetPeople contract (Q12 / `recon-DEV-475` lines 8–10)**: Backend requires `role=actor` (422 otherwise). OpenAPI/generated types omit query params. Retain runtime query via shared literal-typed helper; remove inline casts at `LibraryView.tsx` and `movieDetailsQueries.ts`; no contract file edits.

**Per-entry-point navigation contracts (`recon-DEV-475` lines 20–23)**:
- App shell `openMovie`: captures pathname+search into `libraryReturnUrl`; adds `playerIntent: true` only when requested.
- `openMoviePlayer`: captures library/prior return URL, always `playerIntent: true`, push then (today) synthetic popstate — migrate notification without changing capture/intent.
- RelatedMovies / details related navigation: pass validated `returnUrl` as `libraryReturnUrl` (or empty), without setting `playerIntent`.

Known relevant tests:

```text
web/src/features/app-navigation/appNavigation.test.ts   # NEW before cutover
web/src/features/app-navigation/useAppNavigation.test.tsx
web/src/features/library/filterQuery.test.ts
web/src/features/library/FilterPanel.test.tsx
web/src/library/LibraryView.composed.test.tsx
web/src/library/LibraryView.toolbar.test.tsx
web/src/library/MovieCard.test.tsx
web/src/features/movie-details/MovieDetailsView.test.tsx
# App tests; browseWatchlist / watchlist integration tests
# e2e: web/e2e/* listed under Technical Context (dated snapshot)
```

**Structure Decision**: Remain inside existing `web/src` feature/library directories. New files limited to: `appNavigation.ts` + `appNavigation.test.ts`, the Q10-ratified component siblings above, pure chip-builder export within `LibraryFilterSummary.tsx`, typed GetPeople helper (feature-local), and directly corresponding regression tests.

## Test Strategy

| Family | Required evidence |
|---|---|
| Unit — filterQuery / library filters | Preserve canonical live filtering, sort/default/fallback; no replacement tests for removed orphans |
| Unit — navigation (`appNavigation.test.ts` + consumers) | Programmatic push/replace **without** synthetic popstate / no `PopStateEvent` dispatch; openMovie/player/related/details exits and navigate/replace through actual entry points; subscriber UI resync; exactly one update; unsubscribe/unmount; StrictMode setup-cleanup-setup; isolation via ordinary cleanup; native Back/Forward via simulated popstate allowed for traversal only; history/return URL/player intent per entry point; MovieCard fallback in `MovieCard.test.tsx` |
| Unit — details / related / library shells | Behavioural parity after extraction and navigation migration; debounce cancel-on-nav; independent facet keyboard; pure chip-builder regression |
| jsx-a11y | **Deferred to DEV-512** — not a delivery lane here; no zero-violations claim from unavailable inventory |
| e2e smoke | SMOKE-001/002/003 mapped to named scenarios or named extensions (`recon-DEV-475` lines 25–27); Could not run blocks Done-when |
| Gates | Exact repo-relative commands in Technical Context; scoped web-gates; C# scope-empty SKIPPED stays SKIPPED never PASS |

## Phase 0 / Phase 1 note

Inventable unknowns are ruled in brief Q1–Q11 and CONCLUSIONS Q12. Q10-ratified siblings, Q11 path, Q12 role/chip/range, current recon evidence, DEV-511/DEV-512, and package.json commands are integrated. Plugin-version and recommended-rule violation inventories are follow-up work, not freeze blockers. No `research.md` / `data-model.md` / `contracts/` — no API/schema design delta; ADR 0042 already proposed by Keel. Keel-owned brief/ASSUMPTIONS/ADR companion sync for Q12 remains outside Quill edits (PLAN_ADJUDICATION repair 8).

## Post-design Constitution Check

Re-checked after documenting the web-only, feature-local structure above: still PASS/N/A (planned) as in the Constitution Check table; ADR acceptance remains pending; no new port, handler framework, schema, API, LocalPlay, secrets, Process.Start, CONTEXT glossary, or new npm dependency obligations introduced by this plan.
