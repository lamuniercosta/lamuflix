# Cleanup / Navigation / a11y Requirements Checklist: DEV-475

**Purpose**: Unit tests for English — validate clarity, completeness, consistency, and coverage of cleanup, navigation, deletion-evidence, and deferred-a11y requirements before plan freeze / Gate 1
**Created**: 2026-11-14
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [tasks.md](../tasks.md)
**Audience**: Spec author (Quill), analyzer (Keel), Gate 1 reviewers
**Status**: Plan-repair round 2 + Q13 governance consistency. Analyze CLEAN then plan-challenge NEEDS FIXES — NOT PLAN FROZEN. Gate 1 closed pending unanswered Q13 structural owner checkbox (retain runtime `role=actor` on `GET /people`). Not a runtime verification checklist.

## Scope & boundaries

- [x] CHK001 Is Tailwind adopt-or-remove explicitly excluded from this delivery and routed to verified follow-up DEV-511?
- [x] CHK002 Are BrowserPlayer cleanup, UI redesign, new router/state layers, backend/API/schema, and LocalPlay/secrets/Process.Start explicitly out of scope?
- [x] CHK003 Does frozen scope end with “anything else is a follow-up issue, not a finding in this round”?
- [x] CHK004 Is whole-file deletion beyond `libraryUrl.ts` + `libraryUrl.test.ts` gated on a separate cited Patron ruling?
- [x] CHK004a Is eslint-plugin-jsx-a11y adoption explicitly deferred to verified follow-up DEV-512 (Q10), with no install/`@eslint/compat`/peer override/ESLint downgrade in this delivery?

## Navigation contract

- [x] CHK005 Is shared `openMovie` required for details, RelatedMovies, and the existing player open flow via `appNavigation.ts` (Q11)?
- [x] CHK006 Is synthetic production popstate forbidden for programmatic navigation?
- [x] CHK007 Is native popstate reserved for browser Back/Forward only (retained on `useAppNavigation.ts`)?
- [x] CHK008 Are route parsing, return URL, player intent, and library filter URL semantics required to be preserved, including distinct per-entry-point contracts (`recon-DEV-475` lines 20–23)?
- [x] CHK009 Is a new router dependency / global state library / architectural layer explicitly rejected for this cleanup?
- [x] CHK010 Are duplicate history entries and leaked listeners called out as failure modes?
- [x] CHK010a Is producer/listener cutover required to be atomic (T006+T007 one commit/checkpoint) with `useAppNavigation` subscription wired before producers stop?
- [x] CHK010b Is `appNavigation.test.ts` named as the mechanism suite before cutover, and `MovieCard.test.tsx` as the fallback regression home?
- [x] CHK010c Is MovieCard’s existing `openMoviePlayer` wrapper import retained unless a separate approved edit is necessary?

## Deletion & typing evidence

- [x] CHK011 Must dead-code removal wait on a bounded reference inventory including barrels, tests, and dynamic references?
- [x] CHK012 Is own-test-only explicitly insufficient as complete all-configuration proof, and is the complete `filterQuery` importer set cited (`recon-DEV-475` lines 12–14)?
- [x] CHK013 Are canonical live filterQuery behaviour and tests preserved with no replacement aliases?
- [x] CHK014 Does Q12 retain runtime `role=actor` via a typed helper while removing inline casts, without requiring nonexistent generated role types or OpenAPI edits?
- [x] CHK014a Does Q13 require an unanswered structural owner checkbox on the spec PR authorizing that retention (Wanted #3 vs Done-when), with Gate 1 closed until checked?
- [x] CHK015 Is the OpenAPI → generated TypeScript contract chain required to remain untouched?
- [x] CHK015a Are ticket Wanted/Done-when cited from restored authority (`recon-DEV-475` lines 39–58) and rulings from brief/CONCLUSIONS rather than nonexistent historical note lines?

## Component extraction

- [x] CHK016 Must FilterPanel, LibraryView, and MovieDetailsView remain orchestration shells?
- [x] CHK017 Are cohesive siblings required to live under existing parent directories with one state/effect owner and typed props?
- [x] CHK018 Are line-count targets and forwarding-only wrappers explicitly not acceptance bars (historical recon counts = dated snapshots only)?
- [x] CHK019 Are concrete sibling module names the Q10-ratified set (DualRangeField / FilterSearchField / FilterFacetCombobox / FilterSortControls; LibraryBrowseView / LibraryFilterSummary / LibraryResultsToolbar / LibraryResultsBody; MoviePoster / ManualMatchControl / CastSection / MoreWithPanel / MorePosterCard / FileSection)?
- [x] CHK019a Does DualRangeField own range controls and FilterSortControls own only sort/direction (Q12)?
- [x] CHK019b Does LibraryFilterSummary define and export a pure chip-building helper with unit coverage (Q12 / Wanted #4)?
- [x] CHK019c Are debounce cancel-on-navigation, independent facet instance ownership, and no sibling-local history/popstate required?

## jsx-a11y policy (deferred)

- [x] CHK020 Is in-delivery eslint-plugin-jsx-a11y adoption explicitly superseded by Patron Q10 deferral to DEV-512?
- [x] CHK021 Are blanket disables, severity reductions, and broad exclusions still forbidden for existing lint gates?
- [x] CHK022 Is the unavailable recommended-rule violation inventory explicitly not a DEV-475 freeze prerequisite under Q10?
- [x] CHK023 Are verified DEV-511 and DEV-512 follow-up receipts recorded consistently in Quill-owned artifacts?

## Verification & review bar

- [x] CHK024 Is the post-Q10 task order explicit (inventory → characterize → orphan/type → navigation → extract → verify) without an in-delivery a11y wiring step?
- [x] CHK025 Must both programmatic navigation (no synthetic popstate) and native Back/Forward coverage exist, with imperative path exercised through production entry points?
- [x] CHK026 Do lint, typecheck, unit, and e2e all remain required Done-when evidence with repo-relative commands from `web/package.json` / plan.md?
- [x] CHK027 Is unrunnable e2e defined as Could not run blocking Done-when?
- [x] CHK028 Are SKIPPED/SKIP/N/A forbidden from being folded into PASS?
- [x] CHK029 Is the Size L closing bar zero unresolved Critical/High/Medium/Low with three-axis reports and 2×2 caps?
- [x] CHK030 Is agent merge / auto-merge / PLAN FROZEN / Gate 1 / ADR-acceptance / Q13-checkbox-approval claim explicitly forbidden from drafting alone?
- [x] CHK030a Are SMOKE-001/002/003 mapped to named e2e candidates or named extensions (`recon-DEV-475` lines 25–27)?
- [x] CHK030b Does T000 require the Q13 structural owner checkbox verbatim on the spec PR body (retain runtime `role=actor` on `GET /people`)?

## Consistency across artifacts

- [x] CHK031 Do spec.md, plan.md, and tasks.md agree on Q1/Q10 delivery boundary (Tailwind DEV-511 and jsx-a11y DEV-512 deferred)?
- [x] CHK032 Do spec/plan/tasks agree that producers and listeners migrate together atomically via `appNavigation.ts`?
- [x] CHK033 Do tasks mark T000/T001/T012/T013 as pipeline-only and enumerate Q10/Q12 sibling ownership plus Q11 `appNavigation.ts`?
- [x] CHK034 Is ADR 0042 referenced as proposed (Keel-owned) without Quill inventing ADR content or claiming acceptance?
- [x] CHK035 Are DRAFTING_RECEIPT plan-repair round-2 items resolved against the Keel 9-item list?
- [x] CHK036 Do Quill-owned artifacts record analyze CLEAN then plan-challenge NEEDS FIXES (not an unqualified clean/frozen claim)?
- [x] CHK037 Do plan.md Constitution Check, tasks.md T000, DRAFTING_RECEIPT Owner checkbox, and both checklists require the Q13 structural checkbox (no remaining “zero/no structural checkbox” claims)?

## Notes

- Items marked complete mean the **requirements text** is present and consistent — not that implementation has landed.
- Plan-challenge repair round 2 and Q13 governance consistency are recorded in `DRAFTING_RECEIPT.md` / `_quill-repair-report.txt`.
- Q13 checkbox remains unanswered; PLAN FROZEN remains Keel's after independent reread.
- This checklist is not a substitute for Keel re-read after repairs.
