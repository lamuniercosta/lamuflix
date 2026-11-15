# DEV-475 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-14  
**Seat**: Quill  
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-475-spec` · branch `feature/475-spec`  
**Method**: `/speckit-specify`, `/speckit-clarify`, `/speckit-checklist`, `/speckit-plan`, and `/speckit-tasks` in one pass; analyze-repair round 1 against Keel 6-item fix list after Patron Q10/Q11; **plan-repair round 2** against Keel 9-item consolidated list after Patron Q12 (`93b815ae`) and current `recon-DEV-475`. `$env:PYTHONUTF8=1` set for the drafting shell. `.specify/feature.json` already pointed at `specs/DEV-475` and was left unchanged.

**Inputs**: grill-closed `brief.md` (Q1–Q11; Q12 pending Keel sync), `CONCLUSIONS.md` (incl. Q12), `ASSUMPTIONS.md`, proposed ADR 0042 (Keel-owned; not edited by Quill), `PLAN_ADJUDICATION.md`, Maestri notes `DEV-475` and `recon-DEV-475` (GetPeople 8–10; filterQuery importers 12–14; MovieCard 16–18; return-URL 20–23; smoke/e2e 25–27; ticket authority 39–58; recovered summaries 61–66), `specs/PRODUCT.md`. Constitution cited only for plan gates; Quill did not edit constitution.

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-475/spec.md`
- `specs/DEV-475/plan.md`
- `specs/DEV-475/tasks.md`
- `specs/DEV-475/checklists/requirements.md`
- `specs/DEV-475/checklists/cleanup-navigation-a11y.md`
- `specs/DEV-475/_quill-repair-report.txt`
- this receipt

**Not written**: `research.md`, `data-model.md`, `contracts/`, `quickstart.md` — no public API/schema design delta.

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0042, `PLAN_ADJUDICATION.md`, production/test code, `harness.yml`, generated OpenAPI/TS, `CONTEXT.md`, constitution, main checkout.

**Clarify**: Session 2026-11-14 recorded in `spec.md`. No `[NEEDS CLARIFICATION]` markers for inventable behaviour. Q10–Q12 close former inventables for siblings, navigation path, jsx-a11y disposition, GetPeople role, chip builder, and DualRangeField ownership.

**Owner checkbox**: **Q13 structural escalation required** (`blocked: structural` — Charter §2.3(a) / PRODUCT §5). Spec PR MUST carry an unanswered owner checkbox authorizing retention of runtime `role=actor` on `GET /people` via the shared literal-typed helper while removing inline casts (Wanted #3 vs Done-when / backend 422; `CONCLUSIONS.md` Q13 commit `b3b70c15`). Q12 technical design remains intact pending that checkbox and is not owner approval. Ordinary owner Gate 1 remains pending. Quill does not invent PLAN FROZEN or claim checkbox approval.

**Decisions invented by Quill**: none. Behaviour and boundaries taken only from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT/PLAN_ADJUDICATION.

## Analyze-repair round 1 (Keel 6-item fix list) — completed earlier

1. Deferred jsx-a11y in spec/plan/checklists per Q10; removed in-delivery plugin/violation work.
2. Retired former T011/T012; renumbered former T013–T015 → T011–T013; repaired phase diagram and dependencies.
3. Replaced plan/T005/T008–T010 placeholders with Q10-ratified sibling paths and Q11 `appNavigation.ts`.
4. Updated T001 and this receipt with recon citations plus `web/package.json` command receipts.
5. Recorded verified follow-ups DEV-511 (Tailwind) and DEV-512 (jsx-a11y).
6. Resolved drafting-receipt items 1–6 individually; updated checklists for consistency.

## Plan-repair round 2 (Keel 9-item consolidated list)

Disposition of each item:

1. **APPLIED (unblocked by Q12 + recon)**: FR-009 / US1 / T004 / plan GetPeople section encode Q12 — retain runtime `role=actor` via shared literal-typed helper; remove inline casts at `LibraryView.tsx` and `movieDetailsQueries.ts`; do not require nonexistent generated role types; no OpenAPI/generated/backend edits. Cite `recon-DEV-475` lines 8–10 and CONCLUSIONS Q12.
2. **APPLIED**: T005 wires `useAppNavigation` subscription before producers stop; T006+T007 marked one atomic commit/checkpoint (T006 alone not acceptable); one programmatic push/replace path covers openMovie/navigate/replace/openMoviePlayer wrapper/details exits; per-entry-point return-URL/playerIntent from recon lines 20–23; MovieCard wrapper retained (lines 16–18).
3. **APPLIED**: Named `web/src/features/app-navigation/appNavigation.test.ts` in plan/T005; MovieCard fallback home `MovieCard.test.tsx`; consumer suites named; assertion contract (no PopStateEvent, exactly one update, unsubscribe, StrictMode, isolation); SMOKE-001/002/003 mapped to named e2e candidates/extensions (recon lines 25–27); required lint/typecheck/unit/e2e commands retained.
4. **APPLIED**: T008–T010 / FR-005 carry debounce cancel-on-nav, independent facet ownership, shell-supplied navigation callbacks (no sibling history/popstate); T009 exports pure chip builder from `LibraryFilterSummary.tsx` with unit coverage; DualRangeField owns ranges; FilterSortControls sort/direction only (Q12).
5. **APPLIED**: T001/T003/FR-007 use complete confirmed importer set (recon lines 12–14); preserve parseFilterQuery/toUrlQuery/formatRatingBound + runtime validation; importer presence ≠ edit authorization.
6. **APPLIED (Quill-owned citations)**: Spec/plan/tasks cite ticket Wanted/Done-when from restored `recon-DEV-475` lines 39–58; rulings point at brief/CONCLUSIONS (incl. Q12); DEV-511 and DEV-512 carried consistently; T000 notes spec PR body must carry split clause + both dispositions. Quill does not publish the PR in this step.
7. **APPLIED**: T000/T001/T012/T013 marked **pipeline-only**; phase diagram and dependency text repaired; repo-relative `web/` commands; historical worktree provenance labelled; status records analyze CLEAN then plan-challenge NEEDS FIXES; e2e lists and historical line counts labelled dated snapshots; ADR compliance remains pending acceptance (not unqualified PASS).
8. **NOT EDITED (by design)**: Keel/Patron-owned companions left untouched — `brief.md` still lacks Q12 section; `ASSUMPTIONS.md` still states jsx-a11y follow-up id not asserted; ADR 0042 not rewritten to Context/Decision/Consequences. Recorded here as outstanding Keel/Patron companion sync before freeze.
9. **APPLIED (depends on Q12 recorded)**: Q12 DualRangeField / pure chip builder / FilterSortControls ownership encoded in FR-005, plan Project Structure, T008–T009, and checklists; conditional “ranges only if cohesive” wording removed.

## needs decision resolutions (items 1–6 from analyze round) — still resolved

1. **RESOLVED**: Exact cohesive sibling module filenames and ownership — Patron Q10; Q12 settles DualRangeField vs FilterSortControls and chip builder. Navigation path — Patron Q11.
2. **RESOLVED (updated round 2)**: Reference/export inventory — complete `filterQuery` importer set `recon-DEV-475` lines 12–14; ticket-named libraryUrl deletion authority lines 39–58 / summaries 61–66. Lexical importer set is not edit authorization for all importers.
3. **RESOLVED (deferred)**: `eslint-plugin-jsx-a11y` — not in this delivery; verified follow-up **DEV-512**.
4. **RESOLVED (deferred / N/A for freeze)**: jsx-a11y violation inventory — not a DEV-475 freeze prerequisite under Q10; belongs to DEV-512.
5. **RESOLVED**: Exact web lint / typecheck / unit / e2e commands from `web/package.json` in plan Technical Context (repo-relative). SMOKE map from recon lines 25–27.
6. **RESOLVED**: Tailwind **DEV-511**; jsx-a11y **DEV-512** (Rigger receipts on task note DEV-475). Keel-owned ASSUMPTIONS still lags on DEV-512 assertion (repair 8).

## Q13 governance consistency (step 6.3 remaining fix)

**APPLIED** against `PLAN_ADJUDICATION.md` Step 6.3 Q13 governance repair verification:

1. `plan.md` Constitution Check / Status / Input — removed “No owner structural checkbox from this draft”; require unanswered Q13 checkbox for retaining runtime `role=actor` on `GET /people`; Gate 1 stays closed.
2. `tasks.md` T000 — removed “no structural owner checkbox on this ticket”; Spec PR body MUST carry the Q13 structural checkbox verbatim plus Done-when split and DEV-511/DEV-512 dispositions.
3. This receipt Owner checkbox line — records §2.3(a) escalation (not zero escalations).
4. Both checklists — status and consistency items require the Q13 checkbox; no remaining “zero/no structural checkbox” claims in Quill-owned governance instructions.
5. Preserved Q12 technical design, ordinary Gate 1, DEV-511/DEV-512, review caps, proposed ADR. Do not claim PLAN FROZEN or checkbox approval.

## Outstanding (not Quill-owned)

- User must answer the Q13 structural checkbox on the spec PR before Gate 1 can close (`CONCLUSIONS.md` Q13).
- needs decision: Keel independently rereads Quill Q13 governance alignment and decides freeze status (PLAN FROZEN withheld until then).

## Notes

- No production or harness edits performed in drafting or repair rounds.
- Status at Quill handoff after Q13 governance consistency: Quill-owned artifacts aligned with spec.md and Patron Q13; **not** PLAN FROZEN; Gate 1 closed pending unanswered structural checkbox + ordinary Gate 1; implementation OPEN.
