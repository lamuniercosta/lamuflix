# DEV-475 — Phase 2 step 6 plan adjudication

Date: 2026-11-14. Adjudicator: Keel. Worktree: `feature-475-spec`; branch: `feature/475-spec`.

**Verdict: NEEDS FIXES — NOT PLAN FROZEN.** All three required axes are present. The role/query contract requires recon and a cited Patron ruling before its repair can be specified safely. No implementation, Gate 1, runtime acceptance, review closure, or merge clearance is claimed.

## Evidence and limits

- Risk: `findings-DEV-475-Sentry`, reviewed base HEAD `3bc27ca6`, H1–H5, M1–M7, L1: **13 findings**. The task-note receipt saying 12 findings is a counting error; every individually labelled finding is adjudicated below.
- Standards: `findings-DEV-475-Ledger`, findings 1–10: **10 findings**.
- Spec: `findings-DEV-475-Compass`, reviewed base HEAD `3bc27ca6`, findings 1–7: **7 findings**.
- Total: **30 findings**, including overlapping findings. These reports concern the working planning documents on that base, not a claim that the documents were committed at that SHA.
- Independently reread the cited planning passages: `plan.md:7`, `:23`, `:48–59`, `:104–130`, `:154–171`; `tasks.md:47–116`, `:127–136`; `spec.md:164–183`; `brief.md` scope, invariants, file boundaries and test strategy; `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0042. Production-code claims below are attributed to the challenger reports, not new Keel measurements.
- Current `recon-DEV-475` is a 34-line addendum. It does not contain the historical ticket receipt cited in several rulings. No source-code search, API experiment, dependency installation, gate run, or independent ticket retrieval was performed in this adjudication.

## Risk dispositions

| ID | Severity | Disposition and cited basis | Repair |
|---|---|---|---|
| Sentry H1 | High | Accepted; unresolved recon/decision dependency. `tasks.md:76` and `spec.md:172–173` require removal, generated role types, unchanged requests and no contract change simultaneously. Sentry reports backend `FacetEndpoints.cs:83–104` requires `role=actor`, while generated GetPeople declares no query. Neither deleting the request parameter nor authorizing an interim cast is settled by Q5. | 1 |
| Sentry H2 | High | Accepted coverage gap. Q2/Q7 require all programmatic paths; `tasks.md:89` does not name MovieCard fallback. Prefer retaining its existing `openMoviePlayer` import as a wrapper over the shared mechanism, avoiding a new MovieCard production edit. The fallback still needs a named regression home and recon confirmation. | 2, 3 |
| Sentry H3 | High | Accepted. `tasks.md:86–90` orders separate producer/listener tasks without an enforceable atomic boundary, despite Q2/Q7 and FR-013. | 2 |
| Sentry H4 | High | Accepted. `tasks.md:89` focuses on openMovie; FR-002 and Q2 prohibit synthetic events for every programmatic destination, including library-return, actor-filter and deletion exits. | 2, 3 |
| Sentry H5 | High | Accepted preservation gap. Q2/Q5/Q7 and FR-004 require existing return-URL/player-intent semantics. The report identifies three different call-site behaviours; the shared helper must preserve each rather than select one policy by accident. Reported code facts require recon confirmation. | 1, 2, 3 |
| Sentry M1 | Medium | Accepted. `tasks.md:88–90` does not specify how navigate/replace reach non-hook subscribers or prevent duplicate route updates; Q11 names one shared push/replace notification mechanism. | 2, 3 |
| Sentry M2 | Medium | Accepted clarification. `plan.md:159` requires teardown but `tasks.md:88` lacks an explicit subscription lifecycle contract. Q2/Q7 prohibit listener leaks. | 2, 3 |
| Sentry M3 | Medium | Accepted. T008 moves draft/debounce ownership but does not expressly preserve cancel-on-navigation. Q3/Q7 and FR-004/FR-006 require behavioural parity. | 4 |
| Sentry M4 | Medium | Accepted explicitness gap, not an observed implementation defect. Q10 and `brief.md:47` put facet draft/index/refs with each combobox; T008 should make independent instances and their keyboard regressions explicit. | 4 |
| Sentry M5 | Medium | Accepted. FR-015 and Q7 distinguish native simulation from imperative coverage; T002/T005/T007 must identify assertions that exercise production entry points and prove no synthetic dispatch. | 3 |
| Sentry M6 | Medium | Accepted inventory defect requiring recon. The three-import list in `tasks.md:56` is not a safe deletion boundary if Sentry's additional multiline imports are confirmed. Existing recon itself warns lexical counts are limited; Q4/FR-007 require complete bounded evidence. | 1, 5 |
| Sentry M7 | Medium | Accepted clarification. `plan.md:137` already keeps URL/effect ownership in shells; T008–T010 should explicitly carry that invariant through extraction so no sibling recreates history/event code. | 4 |
| Sentry L1 | Low | Accepted. `plan.md:23` inventories e2e files but `tasks.md:116` does not map the three smoke scenarios to coverage. Q7 and SC-001–SC-003 require relevant behaviour, not merely a successful command. | 3 |

## Standards dispositions

| ID | Severity | Disposition and cited basis | Repair |
|---|---|---|---|
| Ledger 1 | Medium | Accepted; duplicate of Sentry H3. Q2/Q7/FR-013 require an atomic migration. | 2, 3 |
| Ledger 2 | Medium | Accepted missing test home; use the existing Vitest/RTL convention rather than applying a C# naming rule to TypeScript. Q7/FR-015 require the new subscriber contract to be exercised before cutover. | 3 |
| Ledger 3 | Medium | Accepted separation of implementation and pipeline tasks and durable ADR text. T001 includes useful evidence but is not a production-code task; preserve its prerequisite rather than discard it. Explicitly mark T000/T001/T012/T013 as pipeline-only or move them to a linked checklist, with complete dependencies. | 7, 8 |
| Ledger 4 | Low | Partially accepted: ADR needs clear Context/Decision/Consequences and concise durable citations. Rejected as a requirement to mark it Accepted now or automatically at packaging: Q9/FR-019 explicitly authorize a proposed ADR; acceptance needs its actual governance receipt. Case alone is not evidence of an architectural defect. | 8 |
| Ledger 5 | Low | Accepted. Current recon cannot resolve historical ticket lines; Q8 requires cited evidence. Versioned evidence must preserve facts and direct authority without copying the known incomplete importer list as authoritative. | 1, 5, 6, 8 |
| Ledger 6 | Low | Accepted stale follow-up statement and supersession clarity. Recon lines 31–34 and task-note receipt verify DEV-512. Preserve historical Patron answers; append a current-status/supersession annotation rather than rewriting the transcript. | 6, 8 |
| Ledger 7 | Low | Partially accepted: use repo-relative execution paths in T001. Rejected blanket deletion of historical worktree provenance or the e2e inventory: `brief.md` Provenance explicitly identifies the reviewed checkout, and Q7 benefits from named coverage. Label the inventory as a snapshot and add a smoke map rather than replace it with an untestable wildcard. | 3, 7 |
| Ledger 8 | Low | Accepted unresolved extraction ownership. Q10 ratifies cohesive siblings but carries conditional range ownership into T008. Patron must settle the exact allocation; recommended: move existing range control into new DualRangeField file, sort/direction only in FilterSortControls. | 4, 9 |
| Ledger 9 | Low | Accepted teardown/isolation requirement; rejected mandatory production test-reset API. Q11 already settles naming, and unsubscribe with test cleanup can establish isolation without another public seam. Add reset machinery only if demonstrated necessary and ruled on. | 2, 3 |
| Ledger 10 | Low | Partially accepted stale template gate/status claim. Remove the Phase 0 template instruction and distinguish planned constitutional compliance from an accepted ADR. Rejected blank-line YAML criticism as a blocker: the shown frontmatter remains valid and no parser failure is reported. Do not couple an Accepted status change to routine T013 packaging. | 7, 8 |

## Spec dispositions

| ID | Severity | Disposition and cited basis | Repair |
|---|---|---|---|
| Compass 1 | Medium | Accepted. The ticket wording reported by Compass explicitly requires chip building as a pure function; a component alone does not express that deliverable. Add the pure builder within LibraryFilterSummary and its regression contract, preserving Q10's boundary. | 4, 9 |
| Compass 2 | Medium | Accepted nonexistent-generated-role-type defect. **Rejected proposed unconditional removal remedy pending recon:** absence from generated types does not establish that the backend accepts omission; Sentry H1 reports the opposite with endpoint lines. Q5's request-shape preservation remains binding until Patron reconciles the confirmed facts with ticket text. | 1 |
| Compass 3 | Medium | Accepted provenance break. Current 34-line recon does not contain historical cited ticket lines. Compass independently reports that the ticket permits splitting follow-ups; this supports existing deferrals but does not repair the missing durable receipt. | 1, 6 |
| Compass 4 | Low | Accepted. `plan.md:7` is stale relative to the recorded 20:00 independent analyze receipt. Current status must also record this new NEEDS FIXES verdict; do not preserve an unqualified clean/frozen claim. | 7 |
| Compass 5 | Low | Accepted; duplicate of Ledger 6. DEV-512 is verified, so pending-receipt wording in brief/ASSUMPTIONS/ADR must be repaired. | 6, 8 |
| Compass 6 | Low | Accepted; duplicate of Ledger 2. | 3 |
| Compass 7 | Low | Rejected requirement for a fresh authoritative line-count measurement. Q3 and FR-005/FR-015 explicitly exclude line-count acceptance bars, and the reports identify different historical snapshots. Preserve those as historical, non-comparable values; omit counts from current acceptance claims. No new measurement is needed to adjudicate this plan. | 7 (provenance clarification only) |

## Consolidated numbered repair list for Quill

One consolidated plan-challenge round-1 list follows. It is not an instruction to Quill to invent missing facts or edit Keel/Patron/Wisp-owned artifacts. The earlier six-item analyze-repair list is a separate stage receipt. Keep the agreed maximum two rounds and two fix commits per round; no automatic extra round.

1. **BLOCKED — recon then Patron: role contract and authority receipt.** Bernstein must obtain an append-only Wisp recon receipt covering the exact ticket Scope/Technical Design and acceptance text, backend GetPeople required/optional role behaviour and status on omission, OpenAPI/generated query surface, both reported role-cast callers, and the full filterQuery importer set. Confirm the three reported return-URL/player-intent behaviours and MovieCard fallback at the same pinned source revision. Preserve prior recon instead of replacing it. Patron then records a cited Q5 reconciliation in CONCLUSIONS and Keel synchronizes brief before Quill edits FR-009/T004. Do not drop `role=actor`, introduce a new cast/exception, change DTO/OpenAPI/generated files, or expand scope by assumption. If the necessary resolution contradicts ticket delivery or requires a constitution departure, record `blocked: structural — <question>` for the user checkbox; no such resolution is authorized here. Generated role types that do not exist cannot remain a task requirement.
2. **Make navigation cutover executable and atomic.** T005 explicitly wires useAppNavigation's route subscription before any producer stops notifying its old consumers. T006+T007 form one atomic commit/checkpoint, or become one task; T006 alone cannot be accepted. One programmatic push/replace notification mechanism serves openMovie, navigate, replace, the existing openMoviePlayer compatibility wrapper, and every details exit including library/actor/delete/404 routes. Keep native traversal distinct, no duplicate route updates/history entries, subscribe returns unsubscribe, and no synthetic production popstate. After recon confirmation, document per-entry-point capture/preserve return URL and playerIntent contracts without unifying away existing behaviour. Retain MovieCard's wrapper import unless a separate approved edit is necessary.
3. **Name meaningful test homes and smoke coverage.** Name `web/src/features/app-navigation/appNavigation.test.ts` (or `.test.tsx` only if JSX is needed) in plan/T005, with tests authored alongside the new mechanism before cutover. Name corresponding existing consumer suites and a MovieCard fallback regression home after recon identifies it. Exercise openMovie/player/related/details exits and navigate/replace through actual programmatic entry points; assert subscriber UI resync, no PopStateEvent dispatch, preserved history/state, exactly one update, unsubscribe/unmount, StrictMode setup-cleanup-setup, and isolation between tests using ordinary cleanup. Native popstate simulation proves traversal only. Map SMOKE-001/002/003 to named existing e2e scenarios or named extensions once evidence is supplied; the inventory alone is insufficient. Keep the existing required lint/typecheck/unit/e2e commands and honest verdicts.
4. **Carry ownership invariants into T008–T010 and spec/plan.** The debounce owner cancels pending commits on native/programmatic navigation and unmount; add a regression for navigation within its debounce window. Each facet instance owns its draft/index/refs, with independent cast/collection/director keyboard tests. Extracted siblings receive navigation callbacks from shells and do not introduce local history/popstate or duplicate URL effects. T009/FR-005 explicitly retain an exported pure chip-construction function inside or re-exported by LibraryFilterSummary, with a focused pure-function regression. Range/sort ownership must use the cited Patron decision from item 9, not the current conditional wording.
5. **Repair the deletion evidence boundary after recon.** T001/T003 must use the complete confirmed importer set, including reported multiline imports in FilterPanel, browseParams and movieDetailsLabels if confirmed, rather than asserting the current three-file list is complete. Preserve canonical parseFilterQuery/toUrlQuery/formatRatingBound behaviour and runtime validation. Do not confuse token counts with callers or authorize edits to additional callers merely because they import the module.
6. **Repair durable authority and follow-up citations.** With Wisp's restored ticket receipt, cite YouTrack DEV-475 directly for scope/precedence and quote the exact split permission with provenance in versioned evidence. Point current spec/plan requirements at brief/CONCLUSIONS rather than nonexistent historical note lines. Carry verified DEV-511 and DEV-512 consistently; no pending jsx-a11y receipt claim remains. Ensure spec PR body carries the ticket split clause and both dispositions; Quill drafts that body but this is not PR publication evidence.
7. **Separate process and refresh status without losing gates.** Mark T000/T001/T012/T013 pipeline-only (excluded from production implementation dispatch) or move them to a linked process checklist; retain Gate 1/evidence/review/final-head prerequisites and repair every reference/diagram. Use repo-relative execution paths; retain explicitly labelled historical worktree provenance. Replace stale pending-reread and Phase 0 template text with the analyze-clean receipt followed by this plan-challenge NEEDS FIXES state. Treat e2e lists and historical line counts as dated snapshots, not acceptance thresholds. YAML spacing alone needs no repair. ADR compliance remains pending actual acceptance, not unqualified PASS.
8. **Keel/Patron-owned companion repairs; Quill must not edit them.** Once decisions are available, Keel records them in brief first, updates current DEV-512 status in brief/ASSUMPTIONS, and rewrites ADR 0042 into Context/Decision/Consequences with durable repo citations and no seat/plan-freeze/receipt chatter. Patron annotates historical Q1/Q6 adoption provisions as superseded by Q10 without deleting answers, and records any new ruling in CONCLUSIONS. Keep ADR proposed until actual acceptance is evidenced; packaging cannot manufacture that acceptance.
9. **Patron decision requested after recon, then synchronize brief.** Confirm two bounded implementations: (a) new DualRangeField.tsx receives the existing local range control; FilterSortControls owns only sort/direction; (b) LibraryFilterSummary owns/exports a pure chip builder satisfying the ticket while retaining the ratified presentation boundary. These are recommendations, not new Keel scope rulings. Record the cited response before Quill implements item 4. The role decision in item 1 is separate and depends on confirmed API/ticket evidence.

## Outstanding dependency and next handoff

**needs recon: At a pinned source revision, what are the exact DEV-475 ticket requirements and split permission, the real GetPeople role/422 contract versus OpenAPI/generated types and both cast callers, the complete filterQuery importer set, the MovieCard fallback/test home, the three current return-URL/playerIntent policies, and named e2e coverage for SMOKE-001/002/003? Restore the authoritative ticket receipt append-only in recon-DEV-475.**

Bernstein routes recon to Wisp, then the bounded decisions to Patron; Quill receives this single consolidated list with its dependencies. Keel does not search production to settle those facts. No speculative plan fix or frozen declaration precedes the missing evidence and rulings. After repairs, independently reread the changed artifacts and close or explicitly reject every accepted item under the agreed cap. None of the accepted repairs is marked completed by this document.


## Step 6.3 independent repair reread — 2026-11-14

**Current verdict: NOT PLAN FROZEN — structural disposition pending.** This section supersedes the earlier next-handoff state; it does not erase the original axis dispositions. Read-only SpecKit analysis ran independently of the subsequent expressly authorized companion edits. Prerequisites returned FEATURE_DIR=specs/DEV-475 successfully; no before/after-analyze extension hooks are registered. No production search, runtime test or delivery gate was performed.

Reread spec.md, plan.md, tasks.md, both checklists, DRAFTING_RECEIPT.md, _quill-repair-report.txt, Q12 in CONCLUSIONS.md, current recon-DEV-475 and task-note receipts. Synchronized brief.md first, then ASSUMPTIONS.md and proposed ADR 0042. ADR now uses Context/Decision/Consequences and durable repository citations without operating-seat or freeze chatter. The exact ticket Wanted 3, Wanted 4 and Done-when split clause are preserved in brief.md. Q12 is copied in full without changing the historical Q1-Q11 transcript. DEV-512 is no longer described as awaiting a receipt.

### Specification analysis report

| ID | Category | Severity | Location | Finding / required disposition |
|---|---|---|---|---|
| I1 | Authority inconsistency | CRITICAL | spec.md Owner question / FR-009; CONCLUSIONS.md Q12; brief.md Provenance and structural dependency | Restored ticket Wanted 3 explicitly requires removing the role parameter; Q12 and FR-009 retain it. Backend 422 evidence justifies the engineering recommendation but does not delegate authority to replace an explicit ticket deliverable. PRODUCT section 5 and charter 2.3(a) require Patron structural disposition and the user checkbox for a ticket-text contradiction. The spec currently says no checkbox. Keel requested Patron disposition; do not freeze or treat Q12 as owner approval. |

### Coverage summary

| Requirements | Tasks | Notes |
|---|---|---|
| FR-001 | T003-T011 | Bounded delivery and explicit deferrals |
| FR-002-FR-004 | T005-T007, T011 | One mechanism, subscription first, atomic cutover, distinct entry-point policies |
| FR-005-FR-006 | T004-T010 | Q12 chip/range ownership, debounce, facets, shell callbacks |
| FR-007-FR-008 | T001, T003 | Seven lexical importers, canonical behavior and deletion evidence |
| FR-009-FR-010 | T004 | Typed role helper; I1 remains an authority blocker despite task coverage |
| FR-011-FR-012 | T001, T011 | DEV-512 deferral and existing lint preserved |
| FR-013 | T001-T011 | Sequencing and atomic T006+T007 |
| FR-014-FR-016 | T002, T005-T007, T011 | Actual imperative tests, native traversal, honest verification |
| FR-017-FR-018 | T012-T013 | Three axes, zero unresolved findings, 2x2 cap, user merge |
| FR-019 | T001, T013 | Proposed ADR; acceptance remains separate |
| FR-020 | T001, T004-T010 | Rulings, recon and command receipts integrated |
| SC-001 | T002, T008-T009, T011 | Library parity |
| SC-002-SC-003 | T005-T007, T011 | MovieCard, imperative and native navigation |
| SC-004 | T001, T003-T004 | Evidence-led deletion and type cleanup |
| SC-005 | T011 | Four required web lanes |
| SC-006 | T012-T013 | Capped review and final-head packaging |

Metrics: 20 FR + 6 SC; 14 tasks T000-T013; 100% planning coverage; 0 unmapped tasks (T000 is the governing Gate 1 prerequisite); 0 duplicate requirement findings; 0 remaining implementation ambiguity findings; 1 Critical authority inconsistency. ADR acceptance is still pending actual governance evidence; no constitution departure is authorized.

### Repair dispositions and remaining handoff

- Repairs 1 and 9: technical Q12 instructions are encoded consistently; I1 prevents authority closure of the role-parameter portion.
- Repairs 2-5: repaired navigation/test/ownership/importer text meets the listed planning requirements. E2e paths are expressly candidates; T011 must provide named scenarios/extensions and actual execution for missing return-navigation and Back/Forward coverage. No existing runtime coverage is claimed.
- Repair 6: restored ticket text, exact split clause and both verified follow-up dispositions are carried in durable brief and Quill artifacts. T000 requires them on the eventual spec PR; publication is not claimed.
- Repair 7: pipeline-only tasks, repo-relative commands, historical status/snapshot labeling and proposed-ADR status are present. Historical pending-sync statements in Quill handoff receipts describe the handoff time; this section and brief give the current state.
- Repair 8: Keel-owned companion synchronization is complete. Explicit in-place supersession annotations of historical Q1/Q6 in CONCLUSIONS remain Patron-owned and were requested with the I1 disposition; Q10 already states their supersession.

Next: Bernstein coordinates Patron disposition, then Quill applies the resulting owner-question/status repair if required within the existing cap. No extra challenger round or fix commit is authorized by this reread. Gate 1 remains closed pending the ordinary owner action and any required structural checkbox. PLAN FROZEN is withheld until the authority conflict is disposed of.


### Q13 disposition read-back and single remaining fix list

Patron Q13, committed at b3b70c15 and independently read back, confirms **blocked: structural** for I1. Q12 remains intact. Q1/Q6 now have the requested supersession annotations. Repair 8 companion synchronization is complete; brief includes the complete Q13 exchange. The technical recommendations can proceed to owner consideration, but the contradiction is not owner-approved and PLAN FROZEN remains withheld.

1. **Quill, via Bernstein:** Apply Q13 to the spec Owner question, current status/authority text in spec/plan/tasks, both checklists and drafting receipt: remove the unqualified no-structural-checkbox claims, carry the unanswered owner question authorizing retained runtime role=actor via the shared helper, and require it on the spec PR. Preserve Q12 technical design, ordinary Gate 1, both verified deferrals, existing review caps and proposed ADR. Do not claim PLAN FROZEN or checkbox approval. This is a bounded governance repair from the reread, not authorization for a new challenger round. User resolution of the checkbox remains required under Q13.

## Step 6.3 Q13 governance repair verification — 2026-11-14

**Current verdict: NEEDS FIXES — NOT PLAN FROZEN.** Independently reread the requested spec, Quill repair report, adjudication and Q13 ruling, then checked the companion governance passages named in the existing single-item fix list. No production searches, measurements, tests or gates were performed.

- **Confirmed:** `spec.md:5–15` now carries Q13 in status/input and the unanswered structural owner question, requires its inclusion on the spec PR and keeps Gate 1 closed. Its Precedence paragraph distinguishes Q12 technical design from Q13 owner authority. `_quill-repair-report.txt` accurately limits the latest edit to spec governance; it does not establish completion of the whole remaining fix list.
- **Still unresolved:** `plan.md:48` says "No owner structural checkbox from this draft." `tasks.md:49` still says "no structural owner checkbox on this ticket" and T000 omits the required Q13 checkbox from its spec-PR body instructions. `DRAFTING_RECEIPT.md:26` still asserts "none (zero §2.3(a)/(b) escalations...)". These are active contradictory governance instructions, not merely historical pending-sync receipts.
- **Disposition accounting:** The original 30 challenge findings remain individually adjudicated above, including partial acceptances and explicit rejections. The prior independent reread closed the technical repairs from both reported repair rounds; their authority closure is not clean while the remaining Q13 fix is only partially applied. Do not describe all findings and repairs as resolved cleanly.

**Existing fix item remains open; no additional challenger round authorized:** Quill must finish the previously requested Q13 alignment across plan/tasks, both checklists and drafting receipt, removing the three contradictory claims above and carrying the structural checkbox into T000. Keep Q12 design, both follow-ups, ordinary Gate 1, caps and proposed ADR intact. After repair, Keel independently rereads before deciding freeze status; the user checkbox remains unanswered and Gate 1 remains closed. `brief.md` retains NOT PLAN FROZEN. No owner approval, implementation, runtime acceptance, review publication or merge clearance is claimed.

### Step 6.3 final governance repair read-back (2026-11-14)

Keel independently reread the completed Quill Q13 alignment in plan, T000, drafting receipt, both checklists and repair report against spec Owner question / Patron Q13. The three previously contradictory active instructions are removed; every named governance artifact requires the unanswered structural owner checkbox, and T000 carries it into the spec PR instructions alongside the split clause and DEV-511/DEV-512. The single remaining Quill repair item is closed. No extra challenger round or owner approval is inferred.

Current planning verdict: **PLAN FROZEN**, declared in `brief.md` after this reread. This supersedes the earlier planning NEEDS FIXES / NOT PLAN FROZEN statuses above. I1 remains `blocked: structural` for owner resolution on the spec PR; its escalation is correctly represented, not rejected or owner-approved. Gate 1 stays closed pending the checked Q13 checkbox and ordinary user approval/merge. Q12, caps, follow-ups and proposed ADR remain intact. No implementation/runtime/review/merge clearance is claimed.