# DEV-475 assumptions

No taste rulings were needed; no [assumed] labels, sort ordering, empty-state copy or new UI defaults were introduced.

Scope, navigation, deletion, type, dependency, verification, loop and ADR choices are explicit cited Patron rulings in [CONCLUSIONS.md](CONCLUSIONS.md), Q1-Q12, not assumptions.

Evidence boundaries:
- Current recon-DEV-475 pins source to 3bc27ca6f2ffbd01aa2fcaf134336ffc4944ee61. GetPeople facts are lines 8-10; seven filterQuery importers 12-14; MovieCard fallback/test home 16-18; distinct return-URL/playerIntent policies 20-23; smoke candidates and missing coverage 25-27; restored ticket text 39-59. Recovered earlier summaries (61-67) do not restore the accidentally overwritten source verbatim. No new Keel production measurements were made.
- Importer presence is not edit authorization or complete proof of deadness across configurations. Preserve canonical parseFilterQuery/toUrlQuery/formatRatingBound behavior and runtime validation; implementation checks the deletion boundary before removing the named pair.
- Q10 ratifies siblings; Q11 names appNavigation.ts; Q12 requires a shared literal-typed GetPeople helper retaining role=actor without inline casts or generated/backend edits, an exported pure chip builder consumed by LibraryFilterSummary, DualRangeField extraction and sort/direction-only FilterSortControls. No generated GetPeople role query type exists.
- Q12 request preservation conflicts with the explicit ticket Wanted 3 removal instruction. The engineering rationale is recorded; Patron Q13 (b3b70c15) records blocked: structural under PRODUCT section 5 / charter 2.3(a); the spec PR needs an unanswered owner checkbox authorizing the role-parameter retention. It is not a silent compatibility assumption or an owner approval.
- Q10 supersedes original Q1/Q6 jsx-a11y adoption. Verified follow-ups are DEV-511 (Tailwind, task-note Rigger receipt 19:24) and DEV-512 (jsx-a11y, receipt 19:48). No plugin, compat dependency, peer override, ESLint downgrade or Tailwind change is authorized here; the plugin violation inventory belongs to DEV-512.
- Smoke candidate files do not prove existing coverage. Return-navigation and Back/Forward gaps require named e2e scenarios/extensions and successful execution before Done-when (T011).
- No new domain vocabulary; no CONTEXT.md change. ADR 0042 remains proposed until actual acceptance is evidenced.
- Step 6.3 independently reread spec, plan, tasks, both checklists and repair receipts. Planning coverage is 20 FR + 6 SC across 14 tasks; runtime gates were not run. The ticket-text conflict withholds PLAN FROZEN. Gate 1, implementation acceptance, runtime acceptance, review closure and merge clearance remain separate.
