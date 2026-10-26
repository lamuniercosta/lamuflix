# DEV-335 — Plan challenge adjudication

Date: 2026-10-26. Keel, Phase 2, plan challenge/fix Rounds 1 and 2 complete (cap 2).
Worktree: `F:/Dev/LamuFlix.worktrees/feature-335-spec`; branch `feature/335-spec`; base HEAD `52e443666b5e767edc93befc20eb55de2e77787b`.

Status: **PLAN FROZEN FOR GATE 1 — NO BLOCKING PLAN FINDINGS**. All three mandatory M axes received. No missing axis. No Critical findings. This record concerns plan readiness, not implementation correctness or review clearance.

## Evidence and authority

Read shared notes `findings-DEV-335-Sentry` (49 lines), `findings-DEV-335-Ledger` (22), `findings-DEV-335-Compass` (33), `recon-DEV-335` and task note `DEV-335`, plus feature brief/spec/plan/tasks/design/conclusions/assumptions and constitution. Source-code facts are supplied by recon and the axes, not a new Keel reconnaissance sweep. Source anchors below are the reports/recon citations. User authorization covers this repair/adjudication; Quill owns drafting edits.

Patron approved in-scope amendments 1-5; complete exchange and correction are in CONCLUSIONS.md. The original clause 5 named incorrect suites; its explicit correction alone controls the survivor envelope. Brief appended amendment items 1-7 consolidate all decisions before drafting. No ticket delivery or constitution departure, new dependency/schema/layer, or owner checkbox. Palette design exists and was read before challenge adjudication. Invalid-state semantics now derive from Patron, superseding the earlier unruled nearest-extent design sentence.

## Every axis finding

Disposition `Accept` requires the named documentation repair, then verification below. `Partial` accepts the demonstrated requirement without the unsupported extra mechanism. `Record` is non-blocking and authorizes no cleanup. Findings that duplicate one another retain separate receipts.

| Finding | Report severity | Disposition and rationale | Repair / proof assignment |
|---|---|---|---|
| Sentry H1 | High | Accept lexical-vs-numeric split. Numeric rounding equality cannot reject textual 7.00; strict wire grammar is now Patron-ruled. Do not confuse typed decimal scale with lexical validity. | Brief amendment 1; FR-004/005, T003/T008/T013/T016. |
| Sentry H2 | High | Accept. Extent-based detection would erase the active rated-only filter; partial invalid groups must also reach HTTP rather than disappear. | Brief 2-3; T018/T021/T022; explicit absent/full-span/both-empty cases. |
| Sentry H3 | High | Accept float-noise risk. Normalize slider-generated values once on interaction, shortest <=1dp output; never normalize pasted invalid URL values. | Brief 2; T018/T019/T021-T023. |
| Sentry M1 | Medium | Accept. Refetch alone does not prove rating parameter wiring. | Brief 6; T020 test-local rating-dependent MSW override + captured request keys + visible grid/count. |
| Sentry M2 | Medium | Accept clarification; open bounds are already approved Q2/Q6, not a scope expansion. | Brief 1; T004/T009: null valid, validate each supplied bound, compare only when both exist. |
| Sentry M3 | Medium | Accept with Patron F1 ruling: preserve raw invalid inputs to existing 422 UI, not coercion to open bounds. | Brief 3/6; FR-009/edge cases and T018-T020. No libraryQueries.ts edit. |
| Sentry M4 | Medium | Partial. Real PostgreSQL translation proof and defensive-empty total/page cases accepted. Existing suite posture retained; no evidence warrants a new client-evaluation switch or harness. | Brief 6; T010/T011. Source basis recon 50-52/85-89, report M4. |
| Sentry L1 | Low | Record existing duplicate-key collapse precedent; no duplicate-handling redesign authorized. New invalid-value/partial pass-through ruling does not expand to duplicate-key policy. Backend duplicate 422 remains covered. | Q3/Q8; T003/T013, no extra web scope. |
| Sentry L2 | Low | Record existing unknown-key behavior. Canonical minRating/maxRating only; typo rejection is outside frozen scope. Optional extra test not required. | Q1/Q5/Q8; no repair. |
| Sentry L3 | Low | Accept clarification consolidated with H1. Patron chooses strict unsigned ASCII and no whitespace; leading zeros allowed. | Brief 1; T003/T013 failure matrix. |
| Ledger M1 | Medium | Accept public validity-oracle reuse, without accepting an unrun InspectCode failure as established. Use existing ImdbRating.TryCreate; do not expose private IsValid or edit its file. | Brief 1; T009/T011; recon 50. |
| Ledger M2 | Medium | Accept one identical lexical contract and matrix across both backend paths. No new shared parser abstraction/file mandated. | Brief 1; T003/T008/T013/T016. |
| Ledger M3 | Medium | Accept, duplicates Sentry H3/Compass F4. Choose shortest canonical wire representation; captions remain one decimal. | Brief 2; T018/T021/T022. |
| Ledger L1 | Low | Record implementation constraint: small WhereRating and decimal private helpers if necessary, no generic framework. No new prerequisite reconnaissance or helper file. | Brief 7; T011. |
| Ledger L2 | Low | Accept casing clarification by Patron. Preserve PascalCase documented/generated names and canonical camelCase raw query keys; exact contract assertion remains exact. | Brief 4; FR-007/008, T014-T017/T022; design section 5. |
| Ledger L3 | Low | Record existing suppression and comment discipline. New request properties stay within applicable scoped suppression; no explanatory comments. | Brief 7; T015. |
| Ledger L4 | Low | Partial. Clarify configured threshold ownership; do not add mandatory observed-red gating absent a settled requirement. Existing before-or-with tests and passing phase checkpoints remain. | Brief 7; task header, T025 and phase ordering. |
| Compass F1 | High | Accept semantic hole. Patron explicitly rules raw invalid/partial transport, safe [0,10] display, existing 422 UI, replacement only on deliberate rating interaction/Clear all. | Brief 3; FR-009/edge cases, design section 4, T018-T023. |
| Compass F2 | Medium | Accept conditional rating-specific survivor coverage. Three exact recon-named existing suites admitted, correcting Patron original filename error. No speculative unrelated sweep. | Brief 5; plan test envelope, T002/T003/T010/T025 as appropriate. |
| Compass F3 | Low | Accept clarity, duplicates Sentry M1. Use test-local server.use; generated static handlers are not hand-edited to simulate filtering. | Brief 6; T020. |
| Compass F4 | Low | Accept, duplicates Sentry H3/Ledger M3. Normalize only interaction output; preserve raw URL failures. | Brief 2; FR-009, T018/T021/T022. |

Security axis conclusion is reported PASS by Sentry, not a locally executed security gate. Keep parameterized EF, existing error templates, no reflected raw-input errors, no auth/CORS/secrets/LocalPlay/Process.Start changes. Regression notes retain exact OpenAPI parameter equality, absent-rating sorting/projection behavior and untouched libraryUrl.ts; no new production sweep is authorized.

## Read-only Spec Kit consistency analysis

Skill used: speckit-analyze, read-only analysis of supplied artifacts; prerequisite command returned FEATURE_DIR = the named specs/DEV-335 with tasks.md present. `.specify/extensions.yml` has no before_analyze/after_analyze hooks. Constitution I/III/V/IX, API/contract and stack rules checked. No constitution exception identified. The separately authorized drafting repairs are performed by Quill after this analysis; analysis itself edits no spec/plan/tasks.

Initial findings: wire lexical ambiguity (Sentry H1/Ledger M2), presence/formatting ambiguity (H2/H3/M3/F4), invalid-URL semantic gap (Compass F1), conditional survivor envelope omission (Compass F2), and request-aware composed-test precision (Sentry M1/Compass F3). No unrelated new finding or new formal review round. Every FR/SC already has a task path; repairs refine that path.

| Requirement | Task coverage |
|---|---|
| FR-001 | T002/T006/T007 |
| FR-002 | T003/T008/T013/T016/T018/T021/T022 |
| FR-003 | T010-T012/T018-T024 |
| FR-004 | T003/T004/T008/T009/T013/T016 |
| FR-005 | T003-T005/T008/T009 |
| FR-006 | T010-T012 |
| FR-007 | T013-T016 |
| FR-008 | T014/T017/T022/T026 |
| FR-009 | T018-T024 |
| FR-010 | T002-T005/T010/T013/T014/T018-T020/T025-T027 |
| FR-011 | T001 scope guard; constraints across all implementation tasks; T027 review |
| SC-001/002 | T010-T012 |
| SC-003 | T003/T004/T008/T009/T013/T016 |
| SC-004 | T014/T017/T026 |
| SC-005 | T018-T024 |
| SC-006 | T025-T027 |

Metrics at initial analysis: 11 FR, 6 SC, 27 tasks; 17/17 mapped requirement/outcome coverage, 0 unmapped tasks (T001 is the mandatory pickup prerequisite), 0 Critical constitution issues. Five consolidated ambiguity/underspecification clusters above; no harmful duplicate requirements (repeated report findings are merged into common fixes). Buildable SC coverage is retained even though no performance/business KPI is invented.

## Repair verification and freeze

Pending Quill Round 1 response and independent artifact read-back. No freeze, Gate 1, implementation clearance, test/gate result, commit, PR publication or merge claimed by this pending record. Existing brief and conclusions are append-only. Final freeze receipt below will pin exact untracked artifact hashes; base HEAD alone does not contain these working bytes.


## Final independent verification — Round 2/2 complete

Quill returned Round 1 edits to spec/plan/tasks/design. Keel independently read back those artifacts, then issued one final numbered fix list confined to tasks T021/T022: valid complete groups emit both canonical keys; invalid/partial groups forward exact raw key presence/value without manufacturing a pair, even when valid-group active detection is false. Quill returned a plain-text completion receipt (recorded in CONCLUSIONS.md); final T021/T022 read-back confirms it. This uses the second and final fix round; no third round or new formal axis review was performed.

| Closed repair cluster | Independently verified artifact anchors |
|---|---|
| Strict lexical rule / numeric validity / null order | spec.md:91-92; plan.md:133-141; tasks.md:35-49,83-91 |
| Presence / full-span / canonical formatting / float normalization | spec.md:60-76,96; plan.md:123-139,158-164; tasks.md:107-119; design.md:18-26,98-122 |
| Invalid/partial raw transport and 422 UI | spec.md:65,73-77,96; plan.md:127,162,173; tasks.md:107-119, especially final T021/T022 at 115/117; design.md:105,116-122 |
| Request-aware composed proof | spec.md:97; plan.md:173; tasks.md:111 |
| Conditional corrected survivor envelope | spec.md:97; plan.md:103-106,169; tasks.md:33,35,63,131; Patron correction in CONCLUSIONS.md |
| Casing / exact OpenAPI equality / generated contract | spec.md:46,94-95; plan.md:125,150-154; tasks.md:83-93,117; design.md:132-141 |
| Existing PostgreSQL translation / defensive-empty count and page | spec.md:93,97; plan.md:141-148,171; tasks.md:63-69 |
| Gate configuration / phase checkpoints / scope | spec.md:97-98; plan.md:179-203; tasks.md:7,21,131-141 |

All accepted and accepted parts of partial dispositions above are closed as **documentation repairs verified**, not as implementation proof. Non-blocking Low records remain recorded. Final read-only consistency result: 11 FR + 6 SC mapped to 27 tasks; no uncovered buildable requirement, unmapped delivery task, unresolved blocking ambiguity or constitution conflict. The final partial-input clarification closes the residual inconsistency without changing requirements/task counts.

Boundary check: `git diff 52e443666b5e767edc93befc20eb55de2e77787b...HEAD --name-status` is empty; all working changes shown by status are untracked files under specs/DEV-335. HEAD alone therefore does not pin these artifacts. No tracked/production changes or implementation gate executions. Pre-append byte prefixes were verified against initial hashes: brief C825E84191688ACA789DFAA974472D9B8BA280C06C27CE07FB2728DC2C0DABB3; CONCLUSIONS D7E5581D0A7160012E1862F9B2CBEA07B6A74BD26E987FD233E0B14FEBB11E97. Source-code facts remain recon/axis-derived; no new source search or measurement by Keel.

**Keel freeze signoff: PLAN FROZEN FOR GATE 1.** All 3 M axes present; 21/21 findings adjudicated; 0 unresolved blocking plan findings; 2/2 allowed fix rounds consumed; no owner checkbox. This final receipt and appended brief/CONCLUSIONS status supersede pending-freeze authoring language in the draft artifacts. Gate 1 approval, owner merge, Phase B pickup and implementation clearance remain separate and unclaimed. Next: Bernstein routes the frozen plan to Patron provisional Gate 1 / spec-PR process. Dependency/main drift checks remain T001 before Phase B. No code tests, gates, commit, PR, tracker mutation or merge performed here.

### Frozen working-byte manifest (SHA-256)

The following hashes cover final planning/decision bytes, including appended brief/CONCLUSIONS closure, and exclude this self-referential receipt.

| Artifact | SHA-256 |
|---|---|
| brief.md | 895F8D9D3EE74075BBC19A016B5EAE07E8906217A1A05D2088D1BF2CDD801CC7 |
| CONCLUSIONS.md | 580AA5AECE41654965EA198F33B979A1B50BE03BF2DAF213AD6CC16FC76F6B3D |
| ASSUMPTIONS.md | 169D017777C6E1D41E1009DDCDFC9E9A68C7E7353F6688A2DC214BD42446A994 |
| spec.md | 99F983FF690B480A381B50BD2DBA143BFBA6F2831B4F7045897D739C76743785 |
| plan.md | 8D3AF33E875E5EEF9883D39C2064CD626B6F94CD3B19511A79E199D9CCF428A4 |
| tasks.md | D95EF5E459D2B887D5E26863927A4F3E753236C7B1C9BC47C313F3FFFBA6F176 |
| design.md | 3F9733C5C04E0C6872109EB5885183927D1D28134ADC0B26BF264DCA4D496493 |
