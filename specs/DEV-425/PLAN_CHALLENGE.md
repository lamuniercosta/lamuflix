# DEV-425 Plan challenge adjudication

Status: PLAN FROZEN — Phase 2 step 6 complete. All Critical/High/Medium findings resolved after two fix rounds. Gate 1 remains user-owned and uncleared.

## Identity, authority and budget

- Worktree: F:/Dev/LamuFlix.worktrees/feature-425-spec; branch feature/425-spec.
- Reviewed head: 5c9169df8ac787539e5650f2478a9f8c166f6d3b; clean before adjudication edits. Intake base: f698aeceb6b6af50a397d076b83ed178566641b0.
- All three required reports read in full: findings-DEV-425-Sentry (33 lines, R1-R10), findings-DEV-425-Ledger (30 lines, M1-M3/L1-L4/I1-I2), findings-DEV-425-Compass (32 lines, 1-7).
- 26 entries total: 0 Critical, 0 High, 7 Medium, 12 Low, 7 Informational. Duplicate concerns retain their own disposition and cross-reference; they are not erased from the denominator.
- Authority: brief Q1-Q8 and appended PC1; CONCLUSIONS.md PC1 records full cited Patron ruling; task-pipeline Phase 2 step 6 and standing budgets. Facts remain the supplied recon-DEV-425 and axis evidence; Keel did no independent source reconnaissance.
- Closing bar: no unresolved Critical/High/Medium. Low is recorded nonblocking. At most 2 fix rounds, at most 2 fix commits per round. Round 1 assigns Quill plan/tasks and Palette design, disjoint ownership; Keel owns decision/adjudication records. No production/test gates in Phase A.

## All findings and dispositions

Evidence paths below are relative to specs/DEV-425 unless otherwise stated; line numbers identify the reviewed head, with saved closure passages appended below.

| Axis / ID | Severity | Disposition | Evidence, rationale and action |
|---|---|---|---|
| Sentry R1 | Medium | ACCEPT - CLOSED | plan.md:142 and tasks.md T023 under-pin Q2. Pin singular string? DirectorId, ReadNumber raw-query empty-means-null mode, raw IQueryCollection count duplicate rejection; add absent/empty and duplicate API proofs. brief Q2 and PC1 authorize, no new API decision. |
| Sentry R2 | Medium | ACCEPT - CLOSED | plan.md:143; tasks.md T041/T042 omit malformed-forward proof. Forward every nonempty raw scalar verbatim, including abc and 12.5, never formatBound empty fallback. Q2 and Q3 prohibit broadening. |
| Sentry R3 | Low | ACCEPT - CLOSED | T040 must name abc parse/serialize/commit roundtrip; Q2 already requires raw preservation. Same narrow correction as R2. |
| Sentry R4 | Low | RETAIN - covered checkpoint | T001/T002 already explicitly require equality and hash changes and tests; brief Q2/Q6. No new task needed. Implementation review must check both custom members; Phase A text is not proof implementation works. |
| Sentry R5 | Low | ACCEPT - CLOSED | T011 covers null passthrough; add null returns unchanged query to T013 to prevent singular/list guard confusion. Q2/Q6 common path, no scope change. |
| Sentry R6 | Low | ACCEPT - CLOSED | plan.md:141 says Movies.Count or equivalent; replace ambiguity with distinct linked library movie count, retain Name then Id and T010 shared-movie proof. No invented measured query expression: exact EF expression chosen and tested at Build. |
| Sentry R7 | Low | ACCEPT - CLOSED | brief Q6 API cancellation convention plus FR-007. Pin token forwarding through endpoint, handler, port and EF call in T005/T012/T022. No new endpoint behavior. |
| Sentry R8 | Informational | NOTED - no ticket | Q3/Q4 expressly choose independent per-host full-facet fetch, client-only search and no endpoint pagination. Accepted scale trade; no optimization work authorized. |
| Sentry R9 | Informational | NOTED - no ticket | Axis verifies auth parity. No required role query parameter is not an auth-policy ruling; keep existing group parity, no authorization change. |
| Sentry R10 | Informational | NOTED - no ticket | Axis strict-int evidence supports Q2: fractional/overflow rejected; signs/whitespace follow existing int32 semantics. No positive/existence validation added. |
| Ledger M1 | Medium | ACCEPT - CLOSED | plan.md source/test list and T001 omit generator/fixture targets. PC1 explicitly names MovieQueryFixture.cs, MovieQueryGeneratorTests.cs and conditional survivor files. Same issue as Compass 1; both retained. |
| Ledger M2 | Medium | ACCEPT IN PART - CLOSED | T005 lacks concrete handler-test file. PC1 names GetDirectorsQueryHandlerTests.cs and conditional AddHandlerTests.cs. Reject implied mandatory BrowseMoviesQueryHandlerTests edit: no demonstrated missing behavior there, Q6 allows only necessary chain edits; existing browse/API proofs remain. |
| Ledger M3 | Medium | ACCEPT CLARIFICATION - CLOSED | tasks.md Core heading promises numeric order despite T005-before-T004; plan.md:131 already combines Core+Persistence. PC1 permits temporary compile-red, requires behavioral red after compile closure and one green checkpoint; retain T003,T005,T004,T006. Reject reordering T004 before T005: prior analyze I2 and PC1 settle dependency order. |
| Ledger L1 | Low | ACCEPT - CLOSED | design component tree is conceptual in FilterPanel.tsx; PC1 forbids mandated extra files/abstractions. Complexity remains later gate work, no new abstraction justified by diagram labels. |
| Ledger L2 | Low | ACCEPT - CLOSED | Pin tests to combobox role; remove wrap-at-ends per PC1 (also Compass 2). type=search can remain with explicit role. |
| Ledger L3 | Low | NOTED - no ticket | Group placement is Palette presentation within Q5; descriptive Mode/Boldness prose is not a normative behavioral requirement. No demonstrated defect; no extra group-order test needed solely for this observation. Severity retained Low, not relabelled to close the bar. |
| Ledger L4 | Low | REJECT CHANGE - nonblocking | plan Constitution Check gives both numbered principles/section names and line locations. Names already preserve semantic identity when lines drift; deleting useful current evidence is unnecessary churn. Refresh citations on actual constitution drift during pickup. |
| Ledger I1 | Informational | NOTED - no ticket | plan Principle IX names approved stack; AGENTS already governs naming, AAA exemptions and comment rules. Do not duplicate every repo-wide rule in every task or broaden code edits. |
| Ledger I2 | Informational | NOTED - no ticket | Contract chain and gate semantics already match Q6 and gate expectations. Retain those constraints; no fix. |
| Compass 1 | Medium | ACCEPT - CLOSED | T001 and plan test list omit field-by-field MovieQueryFixture generator. PC1 and Ledger M1 prescribe concrete nonnull DirectorId generator scope and property proof; no vacuous null-only roundtrip. |
| Compass 2 | Medium | ACCEPT - CLOSED | design keyboard section invents wrap-at-ends beyond Q5. PC1 removes it; no new assumption/wrap behavior added. |
| Compass 3 | Low | ACCEPT IN PART - CLOSED | Controls and active-descendant references must exist. Reject suggestion to omit aria-expanded while closed: Q5/PC1 require false closed and true open. Assert mounted controls/descendant references in T043. |
| Compass 4 | Low | NOTED - no ticket | Axis reports name-based ingestion collapses same names. Q4/FR-003 still requires separate database identities; Testcontainers seed them. Ingestion changes are explicitly out of scope. Record limitation here; do not weaken identity contract or imply runtime same-name ingestion is delivered. |
| Compass 5 | Low | ACCEPT - CLOSED | T041 vague test target; PC1 authorizes concrete new web/src/library/browseParams.test.ts. |
| Compass 6 | Informational | REJECT COPY CHANGE - no ticket | Copy is approved Q5/ASSUMPTIONS; consistency observation does not override Patron wording. Keep Could not load directors. |
| Compass 7 | Informational | NOTED - evidence limit | Ticket identity relies on preserved DEV-425 note:26-30 and brief/CONCLUSIONS transcription. Keel has not re-fetched YouTrack or reconstructed overwritten recon. No contradiction shown and no delivery change authorized. |

## Fix round 1 numbered routing

Quill owns only plan.md and tasks.md. Palette owns only design.md. Both use the amended brief PC1 and full Patron ruling in CONCLUSIONS.md; no production/test edits, no additional decisions. Each commits once, preserving unrelated changes. Keel independently reads exact saved diffs before closure.

1. Quill: Sentry R1 - explicit API scalar/empty/duplicate parse shape and API tests.
2. Quill: Sentry R2/R3 - malformed/fractional raw forwarding and serialization tests, no numeric empty fallback; name new browseParams.test.ts (Compass 5).
3. Quill: Ledger M1/M2 and Compass 1 - concrete generator/fixture/non-null property proof and handler-test file set, conditional survivor/registration scope per PC1; no mandatory unrelated browse-handler edits.
4. Quill: Ledger M3 - explicit T003,T005,T004,T006 dependency order, temporary compile-red versus behavioral-red proof and one combined green checkpoint; preserve IDs and no stubs.
5. Quill: Sentry R5/R6/R7 - null unchanged-query pin, distinct linked count, end-to-end facet cancellation; no new query expression assumed from absent recon.
6. Quill: T043 asserts combobox role and PC1 exact ARIA mounted-reference rules (Ledger L2/Compass 3). Palette: remove wrap claim, retain Arrow navigation, correct ARIA rules, mark conceptual in-file component tree (Compass 2/3, Ledger L1/L2). No new files/abstractions.

## Closure receipt

Initial state (superseded by saved read-backs below): all 7 Medium required correction before freeze. gate1: provisional, spec PR, user Gate 1 and Build remain separate.

### Round 1 Palette read-back

- Commit 182c11ddbe8c92a196d8197062f90614c6848d8d; independent exact diff 5c9169df8ac787539e5650f2478a9f8c166f6d3b...182c11ddbe8c92a196d8197062f90614c6848d8d read in full for design.md.
- Only design.md changed in this fix commit. Component tree now explicitly conceptual within FilterPanel.tsx; no extraction or abstraction mandate. Closed default omits controls and retains expanded=false; keyboard section conditionally references mounted listbox and existing active descendant, using explicit combobox role on search input. Wrap-at-ends text removed; Q5 Arrow navigation retained.
- Compass 2 design blocker CLOSED. Ledger L1 and design half of Ledger L2 / Compass 3 CLOSED. Quill T043 assertions remain pending. No interaction/copy/scope change observed in the diff.

### Round 1 Quill read-back and final correction

- Commit 23c2f8dec35fcb2c29d8e4a0636615e7f909ab5b; full exact diff 182c11ddbe8c92a196d8197062f90614c6848d8d...23c2f8dec35fcb2c29d8e4a0636615e7f909ab5b read independently for plan.md/tasks.md. Committed round-1 file list contains only design.md, plan.md, tasks.md; two fix commits total.
- R1/R2, Ledger M1/M2/M3, Compass 1 original Medium corrections are present: explicit raw API duplicate counts/EmptyMeansNull/absent-empty tests; malformed raw forwarding; named nonnull fixture/generator/handler targets and conditional files; explicit dependency order/compile-red versus behavioral-red. Original seven Medium entries now corrected, including Palette Compass 2.
- R3/R5/R6/R7 and Compass 5 corrections are present: abc serialize/commit survival, null unchanged, distinct linked count, token end-to-end, concrete browseParams.test.ts. Ledger L2/Compass 3 exact mounted-reference rules are present in T043 and agree with design.
- PC2-R1 (Medium, regression, accepted): tasks.md T043 replaces generic keyboard/ARIA semantics with detailed role/attribute assertions but drops explicit assurance of the rest of Q5/FR-014 keyboard/a11y coverage. Restore Enter commits selection, Escape closes without selection change, visible focus, associated label, and listbox/option roles with selected state, keeping all newly added ARIA rules. Existing brief Q5 and spec FR-014 decide these; no new behavior or Patron ruling needed. Plan stays unfrozen pending this saved correction.

## Fix round 2 numbered routing (final available round)

1. Quill owns tasks.md only: restore the full Q5/FR-014 keyboard and a11y test commitment in T043 (Enter selection; Escape close without predicate change; visible focus; associated label; listbox/option and selected semantics) alongside existing role/expanded/controls/active-descendant rules. One narrow documentation fix commit. Do not edit plan/design/spec/decision files or add scope. Keel reads the saved diff. No third correction round is authorized.

## Final read-back and plan freeze

PLAN FROZEN. Keel closes Phase 2 step 6 at corrected artifact HEAD `a9ae7a37438b856677e346d19f7e09a63a3c10a7` on feature/425-spec, with PC1 decision/adjudication records saved in this worktree for Rigger to commit at the next pipeline step.

Round 2 exact diff `23c2f8dec35fcb2c29d8e4a0636615e7f909ab5b...a9ae7a37438b856677e346d19f7e09a63a3c10a7` was read in full: only T043 changed in tasks.md. Enter selection, Escape close without selection/predicate change, visible focus, associated label and listbox/option selected-state assertions are restored; all new ARIA rules remain. PC2-R1 CLOSED. No remaining Critical, High or Medium finding and no missing axis.

| Above-bar finding | Saved closure evidence at final artifact HEAD |
|---|---|
| Sentry R1 | plan.md:148; tasks.md:52,54 — string scalar, raw-query duplicate count, EmptyMeansNull, absent/empty and 422 proofs |
| Sentry R2 | plan.md:149; tasks.md:78-80 — malformed raw survival, abc/12.5 verbatim API forwarding, no numeric empty fallback |
| Ledger M1 | plan.md:114-116,159; tasks.md:28 — concrete nonnull DirectorId fixture/generator and conditional survivor scope |
| Ledger M2 | plan.md:117-122,162; tasks.md:31 — concrete handler test, conditional registration test; no speculative extra browse-handler edit |
| Ledger M3 | plan.md:129,137-139; tasks.md:9,24-33,42 — actual dependency order, compile-red then behavioral-red, one Core+Persistence checkpoint, no stubs |
| Compass 1 | Same generator evidence as Ledger M1; duplicate finding independently dispositioned |
| Compass 2 | design.md:100 — Arrow/Enter/Escape remain; wrap claim removed under PC1 |
| PC2-R1 | tasks.md:81; design.md:98-100 — full Q5/FR-014 keyboard/a11y proof plus mounted-reference rules |

Accepted Low corrections: Sentry R3 at tasks.md:78; R5 at tasks.md:38,40; R6 at plan.md:147 and tasks.md:37,39; R7 at tasks.md:30-32,39,53. Ledger L1 at design.md Component tree; Ledger L2 and Compass 3 at design.md:98-100 and tasks.md:81; Compass 5 at tasks.md:79. Sentry R4 remains an explicit implementation-review checkpoint in tasks.md:28-29. Other Low/Informational entries retain their individually cited nonblocking dispositions above, without scope expansion or invented ticket work.

Budget used: two correction rounds of two allowed; round 1 used two fix commits (Palette 182c11dd, Quill 23c2f8de), round 2 used one fix commit (Quill a9ae7a37). No third round. No severity demotion, waiver, ticket-text change, constitution departure or owner checkbox.

Independent verification: saved diffs for all three fix commits read; reviewed round-1 committed file list was design.md/plan.md/tasks.md only; round 2 touched tasks.md only. `git diff --check f698aeceb6b6af50a397d076b83ed178566641b0...a9ae7a37438b856677e346d19f7e09a63a3c10a7` emitted no whitespace error (exit 0). This is documentation verification, not a backend/web gate or runtime proof. No production code or tests edited/run by Keel.

Next: Patron separately marks gate1: provisional; Rigger commits decision/adjudication records, opens the specs-only PR and sets Gate 1 under task-pipeline Phase 2 step 7. User owns the spec merge. This freeze does not approve Gate 1, authorize Build, establish runtime readiness, or clear a merge bar.
