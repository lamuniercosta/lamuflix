# DEV-522 — Plan adjudication

**Verdict: PLAN FROZEN — 2026-11-17, Keel.**

All 18 findings are explicitly dispositioned: Ledger 8, Compass 4, Sentry 6. This is Phase 2 step 6 design closure, not Gate 1, implementation authorization, executed verification, runtime acceptance, implementation-review closure or merge clearance. T000–T016 remain unchecked. The two-round/two-fix-commit closing bar remains unchanged.

## Evidence and authority

- Reviewed all three named findings notes. Initial challenge base was 01c6d482; Patron rulings are committed as ca667dc7 on feature/522-spec. Repairs below are the subsequent documentation working diff, not production-code findings at that commit.
- Facts come only from recon-DEV-522: initial ticket/import evidence (ticket line 24, code/test references 41–75, read before the note was replaced), current failure persistence/projection update lines 5–16 and web standards update lines 22–33. Current note replaces the earlier note; old numeric references identify the earlier snapshot, not current line numbers.
- Patron CONCLUSIONS.md:97–108 approves the existing projection-path file extension, corrects root-cause acceptance to preserve the actual historical investigation, and settles spec-local vocabulary. Earlier Q4 mechanism-only language in the append-only grill transcript is historical and superseded.
- Keel recorded decisions first in brief.md authoritative amendments, then amended plan.md/tasks.md/spec.md/checklist/receipt. No new dependency, port, schema, layer, status or route. No production/test files changed.

## All findings and dispositions

| Finding | Severity | Disposition and concrete repair |
|---|---|---|
| Ledger M1 | Medium | Accepted, repaired. T013 names tests/LamuFlix.UnitTests/Features/Import/ImportIdentityPropertyTests.cs in the existing FsCheck unit project (recon web standards 29–30). Properties run in memory; DB claims stay T003. Removed manufactured project/location discovery stop. |
| Ledger M2 | Medium | Accepted, repaired. Brief/spec/plan explicitly identify component SMOKE mapping as a Patron-approved departure from browser e2e convention, citing post-grill 01c6d482 and Constitution IX/stack. T014 checks existing import/library-path e2e Failed-text compatibility; no browser-run claim or unauthorized e2e edit. |
| Ledger M3 | Medium | Accepted, repaired as an explicit investigation obligation. T001 records the concrete existing IMDb carrier and ManualImdbId participation/unique-conflict behavior. T002–T004 use that evidence. Carrier absent requires an evidence-backed applicability disposition, never invented input, silent N/A, new metadata lookup or dropped ticket requirement. Existing-port inability stops needs recon/decision. This is a Build evidence gate, not a claim that a carrier exists now. |
| Ledger M4 | Medium | Accepted, repaired by Patron ca667dc7. Four explanatory labels are spec-local and prohibited as new code/domain identifiers; API fields project existing category Code/SafeDescription. Brief/spec/plan explicitly record why no CONTEXT addition is required. |
| Ledger L1 | Low | Accepted, repaired. Recon web standards 33 confirms MovieCard.test.tsx exists. Brief/plan/T010 now require extending the existing test file. |
| Ledger L2 | Low | Accepted, repaired. Removed P from T007/T013 and from serial T009/T010. Dependencies and parallel opportunities show T009 -> T011 -> T010/T012; T002/T003 remain the actual disjoint parallel pair. |
| Ledger L3 | Low | Partially accepted, fully dispositioned. T014 names npm run lint, npx tsc --noEmit, npm run test:run, npm run build from web per recon 23–24. Format retained with CI:59–60 authority (recon 27). The claim of a configured OpenAPI drift gate is unsupported: none found in authorized harness/script/CI roots (recon 24). Do not invent a command or PASS. T011/T014 require generation procedure establishment at pickup and regeneration/diff verification; no generated hand edits. |
| Ledger L4 | Low | Accepted, repaired. T001 requires durable real-DB reproduction tests retained in named T003/T007 Testcontainers suites, with sanitized logs/row summaries retained with ticket/review receipts; credentials, connection strings and unsafe identifying paths are redacted. |
| Compass H1 | High | Accepted, repaired without reducing ticket scope. Patron ca667dc7 supersedes Q4: T001 explicitly investigates historical logs/DB for actual scan/rebuild/reimport trigger plus separate reproduction; unavailable historical evidence stops needs recon. Brief/spec FR-001 and SC-006/tasks now preserve full Done-when 3. No present owner checkbox is needed because mechanism-only acceptance was withdrawn. A future mechanism-only proposal requires an owner checkbox and cannot be accepted by Patron alone. |
| Compass M1 | Medium | Accepted, recon supplied and design repaired. Category persists on Movie via repository/configuration/converter (recon 6–10); existing summary/detail inputs omit it (15). Patron ca667dc7 explicitly extends envelope to MovieSummary.cs, MovieDetails.cs, EfMovieCatalog.cs only. Brief/plan/T011 carry nullable category through existing projections; response Code/SafeDescription only for Failed, null otherwise. No new port/schema/layer. |
| Compass L1 | Low | Accepted, repaired. Spec precedence cites actual ticket Problem / Wanted / Done when sections instead of nonexistent Scope & Technical Design. |
| Compass L2 | Low | Accepted, repaired. Checklist annotates and unchecks blanket nontechnical/no-implementation-detail/technology-agnostic claims; historical draft claim is superseded. T002 now says extend coverage, preserving optional TDD. |
| Sentry H1 | High | Accepted, repaired as investigation plus counted regression requirements. T001 records enqueue ordering relative to durable persistence/conflict translation. T003 counts enqueue calls at the actual seam on winner and losing uniqueness-conflict paths, at most one; lookup-skip assertions alone do not satisfy it. No present ordering fact is assumed. |
| Sentry H2 | High | Accepted, repaired. Brief/spec/plan and paired T004/T007/T008 state phase matrix: import known-IMDb conflict -> existing skipped/already-present, no Failed; enrichment late collision -> Failed/duplicate_imdb_id/canonical message, no merge/delete. Shared exception type does not authorize shared outcome. |
| Sentry M1 | Medium | Accepted, repaired. T002/T003 and spec refinements assert distinct-path known-IMDb reuse reports existing movie identity under existing skip semantics, no second row, no second enqueue and no Failed settlement for that path. No new public result shape. |
| Sentry M2 | Medium | Accepted, repaired. T006/T007/T008 cover duplicate_imdb_id retry: no new row, no indefinite enqueue loop or Pending regression; preserve existing non-retryable no-op or same Failed settlement/category/message. Existing UI control remains unchanged. |
| Sentry M3 | Medium | Accepted, repaired. T001 records bulk commit/visibility granularity. T003 covers two new paths with same already-known IMDb in one request: one row, deterministic loser skip and remaining-item processing under existing semantics. No unverified commit model is assumed. |
| Sentry L1 | Low | Accepted, clarified. Brief/spec/plan use existing path/key equivalence; variants matching existing key skip, variants remaining distinct follow late enrichment collision -> Failed. No new normalization or silent merge. |

## Independent consistency reread and freeze bar

- All 18 IDs above are unique and covered; no missing axis or undispositioned finding. High findings are repaired in design, not waived.
- FR-001/002 -> T001; FR-003–007 -> T002–T006; FR-008/009 -> T007/T008; FR-010–012 -> T009/T011; FR-013 -> T010/T012; FR-014 -> T005; FR-015 -> T001; FR-016 -> T013/T014. SC-006 now requires historical evidence and Rigger receipt, not mechanism-only closure. T015/T016 retain capped review/final-head delivery duties.
- Strict sequence: Gate 1/T000 -> T001 historical investigation/reproduction/receipt -> T002–T006 import -> T007/T008 settlement -> T009/T011 projection+generation -> T010/T012 UI -> T013/T014 properties/gates -> T015/T016 review/handoff. Conditional T005/T008 may be evidence-backed not needed; no speculative edits.
- Current recon answers plan-level feasibility and standards questions. T001 future runtime facts remain deliberate implementation-blocking evidence obligations, not unresolved plan assumptions or permission to bypass the gate.
- Generated chain remains C# DTO -> OpenAPI -> TypeScript -> UI. Honest PASS/SKIPPED/SKIP/N/A/Could not run/FAIL meanings retained. No backend/web/mutation/runtime result is claimed by this documentation adjudication.

**PLAN FROZEN.** Next is ordinary spec-PR / owner Gate 1. Only the user merges. Build and historical root-cause delivery acceptance remain open.
