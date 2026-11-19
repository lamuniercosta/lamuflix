---

description: "Task list for DEV-525 restore subtitle search and apply"
---

# Tasks: Restore subtitle search and apply

**Input**: Design documents — public `specs/DEV-525/spec.md`, private `plan.md`, grill-closed `brief.md` (Q1–Q3), `CONCLUSIONS.md`; Maestri notes `DEV-525`, `recon-DEV-525` (lines 9–12, 14–21, 26–31); `specs/PRODUCT.md`.

**Status**: PLAN FROZEN after round 1 adjudication and plan/task read-back. Gate 1, Build and runtime acceptance remain pending.

**Tests**: Required (`brief.md` Approach; FR-001–FR-013; SC-001–SC-007; SMOKE-001). TDD optional. Playwright E2E through real API with deterministic test-only fixture is mandatory. No new test framework/dependency. Never mock the data-access driver for behaviour that depends on real translation — use existing integration patterns where backend persistence is implicated.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/reproduction tasks are never `[P]`.

**Conventions**: Exact production fix files are selected only after US1 reproduction evidence (`brief.md` Approach step 1). E2E fixture/spec files are authorized by Q2. STOP for cited Patron ruling before any §2.3-protected change or tracked file outside the plan envelope. STOP `needs recon` / `needs decision` if a required fact/decision is missing. No invented cause; no live secrets; no CI workflow edits; no new dependencies; no hand-edits to generated OpenAPI/TS; never lower harness thresholds. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats).

**Pipeline note**: Local Phase 0–6 headings are for task dispatch. Stage 10 refers to the canonical implementation PR step.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

**Mechanical naming**: Use `web/e2e/dev525-subtitle-search.e2e.ts` for ticket-specific regression traceability, following the DEV-454 precedent (recon line 27).

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (US1 running-app reproduction + diagnosis) — blocks production fix; unlocks fixture design against diagnosed trigger
Phase 2   T002 → T004   (US2 E2E fixture + fail-before receipt; T003 asserts success outcomes; T004 records those assertions failing pre-fix)
Phase 3   T005 → T007   (US3 coverage → production fix → pass-after; sequential only — no T005 parallel with T004)
Phase 4   T008 after T007; T009 prepares evidence after T008; T009 publishes and T010 records hosted CI after the stage-10 implementation PR opens
Phase 5   T011 after T007 → T012 pre-PR review dispositions → stage-10 implementation PR → T012 publication and closure with T009/T010 evidence
Phase 6   T013          (delivery only with running-app retest, PR screenshot + both receipts, applicable gates, code-review closure, hosted e2e-smoke)
```

Ordering per brief and Q2: **Gate 1 → T001 → T002–T004 → T005–T007**. T008 and applicable local gates (T011) may start after T007. T012 establishes pre-PR dispositions after T011. Stage 10 opens the implementation PR after local gates and pre-PR review dispositions. T009 may prepare after T008 and can publish once that PR opens. T010 can run independently after PR creation. T012 closes after evidence and review publication. T013 depends on all receipts.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-525 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-525 in `specs/DEV-525/CONCLUSIONS.md`. Receipt only. T001–T013 MUST NOT proceed without verified merge (brief and Q3).

---

## Phase 1: User Story 1 - Reproduce the running-app failure (Priority: P1) (blocks production edits)

**Purpose**: Reproduce subtitle search/apply failure in the running app; record baseline revision, steps, expected vs observed, sanitized evidence; identify failing boundary; select smallest production fix file set from evidence.
**Independent Test**: Written reproduction record exists before any production fix commit.
**Order**: T001 after T000. **STOP** on failed evidence gate (`brief.md` Approach step 1; FR-001; SC-001).

- [ ] T001 [US1] **Build lane**. Reproduce after T000 and record the running-app revision, exact steps, expected and observed results, user-visible error and available HTTP status or mapping, and sanitized evidence (FR-001; SC-001). Locate the failing boundary using the plan investigation points. Record whether overlapping searches or retry are implicated, ruled out, or undetermined. Select the smallest fix from the evidence. **STOP** `needs recon:` if no failing mechanism is reproduced. **STOP** `needs decision:` for a configuration- or data-only cause until the correction and regression proof are authorized. **STOP** for a cited Patron ruling on any required §2.3-covered change.

**Checkpoint**: T002+ may proceed for fixture/spec authoring against the diagnosed trigger; T005+ unlocks only with T001 evidence naming the fix set.

---

## Phase 2: User Story 2 - Fail-before browser regression (Priority: P1)

**Goal**: Minimal test-only provider wiring + new Playwright spec that fails on the pre-fix revision while preserving the diagnosed trigger.
**Independent Test**: Fail-before receipt on pre-fix revision with fixture; no live provider secrets.
**Order**: T002 → T004 after T001 diagnosis of the trigger (fixture must preserve that trigger).

### Tests / harness for User Story 2

- [ ] T002 [US2] **Build lane**. Extend the existing stub on its existing listener using `web/e2e/run.mjs` and `web/e2e/stub.mjs`. Use `web/e2e/fixtures.mjs` or `web/e2e/compose.e2e.yml` only if needed within Q2. Confirm the provider endpoint can be set through existing Options. **STOP** for a cited Patron ruling if the override is unavailable. Direct the real API to the fixture with ephemeral test-only settings and a runtime-generated dummy credential if required. Add no service, port, dependency or workflow change. Map assertion-relevant fixture fields to the provider contract the application consumes. Keep the fixture minimal, record unknowns, and stop if the required mapping cannot be established (FR-005/006; Q2; recon 29).
- [ ] T003 [US2] **Build lane**. Add `web/e2e/dev525-subtitle-search.e2e.ts` for existing Chromium discovery via `web/playwright.config.ts` and `npm run e2e`. Use real search and download endpoints, asserting returned results and an active/applied subtitle. Include the diagnosed overlapping search or retry action in this same scenario when T001 implicates it (SMOKE-001; FR-005/007).
- [ ] T004 [US2] **Build lane**. Run the T003 scenario on the pre-fix revision with the T002 fixture. Record the revision, run ID, timestamp, relevant provider field mapping, T001 trigger, overlap/retry disposition, and corresponding failing assertion (FR-007; SC-003). If search fails first, the apply assertion may not run. Keep both assertions in the scenario for T007. **STOP** if faithful reproduction at the diagnosed assertion cannot be established.

**Checkpoint**: Fail-before receipt exists; hosted discovery path unchanged.

---

## Phase 3: User Story 3 - Fix demonstrated cause and pass-after (Priority: P1) 🎯 MVP

**Goal**: Smallest production fix for the demonstrated cause; update only directly affected existing coverage; pass-after Playwright proof.
**Independent Test**: Same Playwright scenario passes; DEV-501 contract preserved unless ruled otherwise.
**Order**: T005 → T006 → T007 after T004 (sequential; T005 is not parallel with T004).

### Tests for User Story 3

- [ ] T005 [US3] **Build lane**. After T004, extend only existing suites directly affected per T001 and within the plan's authorized envelope. See candidate suites in plan Project Structure. Record unaffected candidates as not needed per T001. Identify existing coverage for missing and invalid acquisition settings and their request-time conflict responses. Route any gap for decision before claiming preservation. Follow C# test standards where applicable: `Method_Condition_Expected` names, existing assertion and mocking libraries, AAA headers, and avoid disk-bound tests (FR-003/010).

### Implementation for User Story 3

- [ ] T006 [US3] **Build lane**. Implement the smallest fix in the T001-selected files inside the existing vertical slice and generated web client. Preserve the DEV-501 search, download and languages route/DTO/status contract. If an authorized §2.3(4) ruling requires contract changes, regenerate OpenAPI and then TypeScript. For any changed remote subtitle content boundary, check relevant existing safeguards: bounded response size for downloads, destination containment for writes and safe text rendering for captions. Record untouched boundaries or that download/write/render were untouched for a search-only change. Report unrelated hardening separately (FR-002–FR-004, FR-010; SC-002/006).
- [ ] T007 [US3] **Build lane**. Rerun the unchanged T003 scenario with the same fixture against the fixed revision and verify both returned results and an active/applied subtitle. Record run ID, revision, timestamp, overlap/retry disposition and both passing assertions. Run the T005-extended suites where applicable and require green results with assertions intact (FR-007; SC-003; SMOKE-001).

**Checkpoint**: US3 independently testable — search/apply restored with pass-after proof.

---

## Phase 4: User Story 4 - Running-app verification, screenshot, hosted CI (Priority: P2)

**Goal**: Successful running-app retest; PR screenshot attached with both fail-before and pass-after regression receipts; green hosted CI including `e2e-smoke` execution of the new spec.
**Independent Test**: Screenshot and both regression receipts on PR; hosted `e2e-smoke` shows the new spec; running-app success record.
**Order**: T008 follows T007. T009 prepares after T008 and publishes when the stage-10 implementation PR opens. T010 records hosted CI after the PR opens and may proceed independently of T009. Identify runtime and CI evidence by run ID, timestamp and revision. Isolate local runtime and gate runs with separate settings, fixtures and stack resources, or serialize if they share resources.

- [ ] T008 [US4] **Build lane**. Repeat T001 in the running app. Confirm search and apply succeed and the observed error surface clears. If T007 passes and the running app still fails, reopen T001 and block delivery (FR-008; SC-002).
- [ ] T009 [US4] **Build lane**. After T008, capture a working-search screenshot and prepare T004 and T007 receipts. Remove keys, tokens, connection strings, sensitive hosts and settings dumps from receipts. Crop or blur sensitive screenshot content as needed and check all three artifacts before publishing. Attach them to the stage-10 implementation PR after it opens and verify the attachments there (FR-008; SC-003/005; Q2).
- [ ] T010 [US4] **Build lane**. After the stage-10 implementation PR opens, record green hosted CI on its head, including `e2e-smoke` executing the new spec via `npm run e2e`. Verify the spec ran and record the hosted run ID, timestamp and head (FR-009; SC-004; recon 27–28).

**Checkpoint**: Ticket Done-when evidence for runtime retest, PR screenshot with both regression receipts, and hosted E2E is present.

---

## Phase 5: Gates and capped three-axis review

**Purpose**: Honest backend/web/contract gates and capped three-axis **code review** under the closing bar. Plan challenge is a separate pre-Gate 1 step (plan Drafting), not this phase.
**Order**: T011 may start after T007. T012 begins after T011 with pre-PR review dispositions. Stage 10 opens the implementation PR when those dispositions and local gates are ready. T012 closes after T009, T010 and PR review publication.

- [ ] T011 **Build lane**. After T007, run and record the applicable checks, commands, results and honest status labels in plan Test Strategy (FR-013; Q3). Record the existing acquisition validation and conflict verification named in T005 and whether the affected behavior changed. Run `OpenApiContractTests`, compare API/OpenAPI for all DEV-501 subtitle operations, and check generated TypeScript by temporary generation or equivalent generator comparison, even when tracked generated files have no diff. Record the E2E TypeScript check.
- [ ] T012 **Build lane** / review owners per pipeline. After T011, obtain Risk, Standards and Spec reports and fix or explicitly dispose all verified in-scope findings through Low within two rounds. Report unresolved blockers at the cap to Bernstein. Once applicable local gates and those dispositions are ready, follow stage 10 to open the implementation PR. Pin the review to the implementation revision. If that revision changes, reverify affected review and gate evidence against the new head. Publish and verify review findings and dispositions on the PR. Record closure once publication and T009/T010 evidence are present. The plan challenge precedes Gate 1 (FR-012; SC-007; Q3).

---

## Phase 6: Delivery handoff

- [ ] T013 **Bernstein** / **Rigger** / owner merge path. After T008–T012, prepare merge-bar proof using plan Test Strategy. Verify the running-app retest, sanitized screenshot and both receipts on the implementation PR, applicable gates, published three-axis review closure, and hosted e2e-smoke execution for the implementation head. Only the user merges.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: No Build dependencies — Gate 1 blocks everything else
- **Phase 1 (US1)**: Depends on T000 — blocks production fix (T005+)
- **Phase 2 (US2)**: Depends on T001 trigger diagnosis — fail-before before fix
- **Phase 3 (US3)**: Depends on T004 fail-before receipt
- **Phase 4 (US4)**: T008 follows T007. T009 prepares after T008. T009 publishes and T010 records hosted CI once the stage-10 implementation PR exists.
- **Phase 5**: T011 follows T007. T012 prepares review dispositions after T011. Stage 10 opens the PR, then T012 closes after T009, T010 and verified review publication.
- **Phase 6 (T013)**: Depends on T008–T012 — running-app retest, screenshot + both receipts, hosted e2e-smoke, applicable gates, and code-review closure are all required

### User Story Dependencies

- **User Story 1 (P1)**: After Gate 1 — no dependency on other stories
- **User Story 2 (P1)**: Needs US1 diagnosed trigger to preserve fail-before
- **User Story 3 (P1)**: Needs US1 fix-set selection and US2 fail-before receipt
- **User Story 4 (P2)**: Needs US3 pass-after

### Parallel Opportunities

- Author order for US3 is sequential: T004 → T005 → T006 → T007 (no T005 overlap with T004)
- After T007, T008 and T011 can run independently when resources allow. After stage-10 PR opening, T009 publication and T010 hosted CI recording can proceed independently.
- T012 final closure requires on-PR evidence and review publication. T013 requires T008–T012.
- No `[P]` tasks across shared evidence files

### Parallel Example: after pass-after (T007)

```text
Local: T008 retest and T009 bundle preparation can proceed while T011 gates run on isolated resources.
After T011: T012 review dispositions, then stage-10 implementation PR opening.
On implementation PR: T009 evidence publication and T010 hosted CI recording, then T012 closure.
```

---

## Implementation Strategy

### MVP First (User Stories 1–3)

1. Complete Phase 0: Gate 1
2. Complete Phase 1: Reproduction/diagnosis
3. Complete Phase 2: Fail-before E2E
4. Complete Phase 3: Fix + pass-after
5. **STOP and VALIDATE**: Search/apply restored with red/green E2E receipts

### Incremental Delivery

1. Gate 1 → foundation for Build
2. Reproduction → unlocks honest fixture/fix selection
3. Fail-before → proves regression value
4. Fix + pass-after → user-visible MVP
5. Running-app retest + PR screenshot with both receipts + hosted e2e-smoke + applicable gates + review closure → ticket delivery evidence (none bypassable)

---

## Notes

- `[P]` tasks = different files, no incomplete dependencies
- `[Story]` label maps task to US1–US4 from `spec.md`
- Verify fail-before before implementing the production fix
- Commit after each task or logical group in the Build worktree
- Avoid: vague tasks, inventing cause, live provider secrets, CI workflow edits, cross-story dependency cycles
