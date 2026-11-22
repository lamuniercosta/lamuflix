---

description: "Task list for DEV-517 For You load more and suggestion replacement"
---

# Tasks: For You load more and suggestion replacement

**Input**: Design documents from `specs/DEV-517/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q8), `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-517`, `recon-DEV-517`; `specs/PRODUCT.md`.

**Status**: PLAN FROZEN under [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md). Gate 1 is complete (T000). Build and runtime acceptance remain pending.

**Tests**: Required (`brief.md` Test and task order; FR-001–FR-015; SC-001–SC-007; SMOKE-001–SMOKE-003; Done when). TDD: author failing component tests for both stories before production feature wiring. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/checkpoint tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` (`ForYouPage.tsx`, `forYouApi.ts`, `MovieCard.tsx`, `ForYouPage.test.tsx`, `for-you-review.e2e.ts`). Read-only: `web/src/api/client.ts`, `types.ts`, `openapi.json`. STOP for cited Patron ruling before any other tracked file or §2.3 protected surface. STOP `needs recon` / `needs decision` if a required path/fact/decision is missing. No second paging mechanism; no production backend change inside this five-file envelope; no hardcoded MaxExcludeCount; no new dependency/layer; never lower harness thresholds; never hand-edit generated OpenAPI/TS. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats). Reuse the existing Vitest/RTL assertions, client stub and test naming/arrangement in `ForYouPage.test.tsx`. Follow the AGENTS.md comment exemptions. Record red tests as local checkpoints before wiring and publish only passing commits using `DEV-517 - subject`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (plan challenge + Q8 adjudication → Keel PLAN FROZEN → Patron gate1: provisional → Rigger opens Spec PR; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (pickup DEV-541 contract confirmation) — blocks feature edits
Phase 2   T002 → T003   (failing component tests for US1 then US2 — both before production wiring)
Phase 3   T004 → T005   (production wiring: append first, then replace; separate green checkpoints)
Phase 4   T006 → T008   (web gates + running-app e2e + PR screenshots; open delivery PR for review posting)
Phase 5   T009          (Size M three-axis implementation review on the open delivery PR; max two review rounds)
Phase 6   T010          (final delivery evidence + merge-bar verification after review closure — user merges)
```

Ordering per `brief.md` Test and task order: **plan challenge + Q8 adjudication → Keel PLAN FROZEN → Patron provisional Gate 1 → Rigger opens Spec PR → owner Gate 1 merge → DEV-541 pickup confirm → failing component tests for both stories → typed slices + visit state + page/card controls → web gates + running-app e2e + screenshots → open delivery PR → implementation review (findings posted on that PR; ≤2 rounds) → final delivery evidence / merge-bar after review closure**. Exercise Load more after a swap and after a failed swap. Check shared card path without the optional action. No dependency cycle.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Complete plan challenge and Q8 adjudication, freeze the plan, obtain Patron provisional Gate 1, open the Spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Prerequisite before Rigger opens the Spec PR**: Plan challenge and Q8 adjudication complete; **Keel** declares PLAN FROZEN; **Patron** sets `gate1: provisional` (`spec.md` Precedence; task-pipeline Phase 2 steps 6–7).
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination only after that prerequisite; verified user-merge receipt in `CONCLUSIONS.md`, or stop. Owner merge remains the Build blocker. No Build tasks before that receipt.

- [x] T000 **Bernstein** coordinates plan challenge / Q8 adjudication through Keel PLAN FROZEN and Patron `gate1: provisional`, then **Rigger** prepares/opens the DEV-517 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-517 in `specs/DEV-517/CONCLUSIONS.md`. Receipt only. T001–T010 MUST NOT proceed without verified merge (`brief.md` Closing bar; Q8; `spec.md` Precedence; no structural owner checkbox invented by Quill).

---

## Phase 1: Pickup DEV-541 contract confirmation (blocks feature edits)

**Purpose**: Confirm the DEV-541 generated query/response contract and initial For You path on the intended Build base before editing.
**Independent Test**: The written pickup records the generated query/response contract, initial call path, repeated-key wire serialization and the inventory of legacy For You API export consumers.
**Order**: T001 after T000. **STOP** on unresolved contract drift (`brief.md` Test and task order step 2; FR-007).

- [x] T001 **Build lane**. After T000: In the Build worktree, confirm DEV-541 on the intended base: generated query params and `{ items, hasMore }` response used by `web/src/features/for-you/forYouApi.ts` via `web/src/api/client.ts` / `web/src/api/types.ts`, and the initial For You path in `web/src/features/for-you/ForYouPage.tsx`. Verify a multi-ID request serializes repeated `exclude` keys, for example `exclude=1&exclude=2`, and inventory callers/tests of `useForYou`, `forYouQueryKey`, `FOR_YOU_TAKE` and `ForYouError`. Record evidence. If contract drift exists, **stop and report to Bernstein**. Do not regenerate or change API artifacts inside this five-file change. Any upstream correction uses the normal C# DTO → OpenAPI → generated TypeScript chain **outside** this ticket’s file envelope. Resume T001 only after verifying the corrected generated contract on the intended base. Do not add a second paging mechanism. Do not invent API fields.

**Checkpoint**: T002+ unlocks only after DEV-541 confirmation (or verified corrected contract on the intended base) is on record.

---

## Phase 2: Failing component tests for both stories (before production wiring)

**Goal**: Author failing Vitest/RTL coverage for Load more and replace/visit transitions before any production feature wiring.
**Independent Test**: `ForYouPage.test.tsx` contains focused cases for US1 and US2, with a local red receipt for each story before production wiring.
**Order**: T002 then T003 after T001. Both complete before Phase 3.

### Tests for User Story 1

- [x] T002 [US1] **Build lane**. After T001: Extend `web/src/features/for-you/ForYouPage.test.tsx` with failing coverage for Load more visit behaviour: initial load/error/empty/retry still available; unchanged exclusion sets with successive offsets; append without rearranging prior cards; deduplication of already rendered/retired IDs; use of unconsumed buffered candidates before fetch; exhaustion when no eligible buffer and `hasMore=false`; failed append preserves grid and committed paging; **failed append then retry from the same committed scan**; at most one in-flight Load more/replace (cross-action locking assertions that need a replace path may stay red until T005). Cover a slice containing only rendered/retired IDs with `hasMore=true` followed by a later eligible slice, a duplicate-only terminal slice that removes Load more, and rollback after a failure following partial progress. Verify ranked append up to `take`. Assert the offset and exclusion arguments at the existing client seam, including repeated query keys if it observes HTTP. Record the local red result before production wiring (FR-001, FR-003, FR-007–FR-009, FR-013; SC-004).

### Tests for User Story 2

- [x] T003 [US2] **Build lane**. After T002: Extend `web/src/features/for-you/ForYouPage.test.tsx` with failing coverage for replacement and visit guarantees: tentative exclude + `offset=0` restart; first unseen candidate replaces in place; committed exclusion persists across Load more/replace/retry/incidental refetch; unused last-slice rows retained for later Load more; HTTP 400 and other failures roll back uncommitted exclusion and preserve committed grid/paging with retryable feedback; **failed replacement then retry**; exhaustion leaves original card with non-error status; Load more usable after failed swap; **a replacement whose first slice contains only visible/retired items with `hasMore=true` — assert the next offset request uses the same tentative exclusion set, then the first eligible replacement, then Load more from the retained buffer**; single in-flight lock; leave/remount starts a new visit; late results discarded. Assert that a post-swap append cannot reuse the pre-swap buffer, cursor, `hasMore` or cache entry. With a prior committed exclusion, fail a later replacement and retry it. Assert the request carries each committed ID and the tentative target exactly once, the committed view survives failure, and server error detail stays out of the UI. Exercise focus/invalidation after a swap and check the ordered grid and committed cursor stay intact. In Strict Mode, leave during a multi-slice scan, verify the actual request signal aborts, let an obsolete response settle and confirm no further request or commit affects the remounted visit. Exercise real `MovieCard` pointer and keyboard activation without navigation/play, and verify the optional control is absent when its prop is omitted (FR-002, FR-004–FR-006, FR-008, FR-010–FR-012, FR-014; SC-004, SC-007).

**Checkpoint**: Failing component tests for both stories are authored and red. No production feature wiring for the new controls yet.

---

## Phase 3: Production wiring (append then replace; separate green checkpoints)

**Goal**: Wire typed DEV-541 slices, visit state, Load more, and per-card replace within the five-file envelope.
**Order**: T004 then T005 after T003.

### Implementation for User Story 1

- [x] T004 [US1] **Build lane**. After T002 and T003 are authored and failing: Update `web/src/features/for-you/forYouApi.ts` to provide typed DEV-541 slice access preserving `items` and `hasMore` (`take`, `offset`, `exclude`). Update `web/src/features/for-you/ForYouPage.tsx` to own visit append state: for initial/append send only committed replaced IDs in `exclude`; advance offset by requested `take`; skip rendered/retired IDs; keep received order; buffer unused eligible rows; render Load more with pending/exhaustion behaviour; preserve initial load/error/empty/retry; support failed-append retry from the same committed scan. Keep all legacy exports compatible. Use additive uncached typed slices, mount-only initial seeding, stable visit ownership after initial commit and a visit-scoped abort signal with identity checks. Fill append from the active buffer and consecutive slices up to `take` or true exhaustion. Atomically commit eligible rows and the new cursor, including an empty terminal result that removes Load more. Use static client-owned feedback for failures. Make append-only T002 cases green. Do not require replacement-created exclusions/buffer or cross-action locking cases that need replace to pass at this checkpoint (FR-001, FR-003, FR-007–FR-009, FR-013–FR-014).

**Checkpoint (Load more)**: Append-only component cases that this wiring can exercise are green (including failed append then retry from the same committed scan). Replacement-created exclusions/buffer and cross-action locking remain for T005.

### Implementation for User Story 2

- [x] T005 [US2] **Build lane**. After T004: Wire replace orchestration in `web/src/features/for-you/ForYouPage.tsx` and add an optional stateless action slot on `web/src/library/MovieCard.tsx` supplied only by For You (Patron-assumed placement/wording per `ASSUMPTIONS.md`; ticket label intent is replace). For a replacement scan, add only the target ID tentatively to `exclude` and commit it only on success; keep all other rendered IDs out of the payload. Enforce Q4 (no client numeric MaxExcludeCount; no error-message parsing; no API expansion). Serialize Load more and replace; commit card + exclusion + scan state together on success. Cover failed-replacement retry; multi-slice replacement (first slice only visible/retired with `hasMore=true` → next offset with same tentative exclusion set → first eligible replacement → Load more from retained buffer); Load more after success and after failed swap. Deduplicate the wire exclusion set, abort on visit cleanup, reject obsolete results and replace the pre-swap cursor/buffer/`hasMore` only on swap success. Use static client-owned failure copy and manual retry. Put the optional action outside navigation/play affordances and follow existing card action styles and accessible names. Make remaining T002/T003 cases green, including replacement-created exclusions/buffer and cross-action locking (FR-002, FR-004–FR-006, FR-008, FR-010–FR-012, FR-014).

**Checkpoint (replace)**: Both user stories component-test green, including replacement-created exclusions/buffer and cross-action locking. Running-app proof still belongs to Phase 4.

---

## Phase 4: Web gates, running-app e2e, screenshots, and open delivery PR

**Purpose**: Prove Done when in the assembled app, satisfy applicable web verification, and open the delivery PR so review rounds can post findings.
**Independent Test**: Green applicable web gates; e2e receipt for append, replace, follow-on action, retired-ID absence; screenshot paths recorded; delivery PR open for review posting (FR-015; SC-001–SC-003, SC-005–SC-006; SMOKE-001–SMOKE-003).
**Order**: T006 → T007 → T008 after T005.

- [x] T006 **Build lane**. After T005: Run applicable web gates for the changed web files. Run the existing `forYouApi.test.ts`, MovieCard and `cardActionContrast.test.tsx` suites unchanged. Record verdicts. If any C# analyzer/complexity/InspectCode/mutation gate runs with empty scope and exits 2, record `SKIPPED (scope-empty)` — never PASS. Do not lower thresholds (FR-015; SC-006; Q7/Q8).
- [x] T007 [US1] [US2] **Build lane**. After T006: Extend `web/e2e/for-you-review.e2e.ts` against the running app. Use the existing running-app Playwright spec and its supported data setup. Give the DEV-517 journey isolated or reset library/feedback state; it must run independently of the DEV-541 like test and must preserve that test’s fresh-start assumption. Preflight against the live endpoint with the scenario's take and initial exclusions: require at least `2 × take + 2` distinct ranked candidates for the initial page, full append, replacement and follow-on. Also verify the planned target’s exclusion leaves at least two unseen candidates outside the initial and appended grid. Fail setup explicitly with observed and required counts. Keep setup in the existing e2e file and record the reset/seed procedure. Drive Load more and verify no reload and no reordering of existing cards. Replace in place and perform another append or replacement in the same visit. Assert the retired ID remains absent. Record separate running-app receipts as `DEV-517 SMOKE-001` and `DEV-517 SMOKE-002` (FR-015; SC-001–SC-003, SC-005).
- [ ] T008 **Build lane**. After T007: Capture screenshots of both controls (Load more and per-card replace). Open (or update) the **delivery PR** with those screenshots attached/readable and the e2e/gate evidence so far, so each T009 review round can post findings and its summary on that PR. Label the screenshot receipt `DEV-517 SMOKE-003` to distinguish it from the existing DEV-541 e2e title. Record screenshot paths and viewport/context. Final merge-bar verification stays in T010 after review closure (SMOKE-003; FR-015; SC-005; Done when; Q8).

**Checkpoint**: Delivery evidence bundle ready and delivery PR open for review posting (gates + e2e receipt + screenshot paths).

---

## Phase 5: Size M technical review / remediation

**Purpose**: Close in-scope findings under the frozen closing bar on the open delivery PR.
**Order**: T009 after T008 (delivery PR must already be open).

- [ ] T009 **Build lane** / named review seats per pipeline. After T008: With the delivery PR already open, run Size M Risk/Sentry, Standards/Ledger, and Spec/Compass **implementation review** on the frozen Q1–Q8 scope. Maximum **two implementation review rounds** (`brief.md` Q8; `plan.md` Constitution Check Size M closing bar). Within each review round, remediation stays inside that two-round review cap and under the separate pipeline remediation limit of ≤2 fix commits per round (task-pipeline Review cap / Remediation cap). Each review round MUST post findings and its summary on that PR. Close every in-scope Critical, High, or Medium finding; provide evidence at the exact reviewed HEAD for every accepted finding including Low; give each other disposition an explicit cited rationale. At the two-round review cap, return any blocker still open to the Conductor and record disposition on the PR. Anything outside Load more / replace / named files / DEV-541 consumption is a follow-up issue (Q8).

---

## Phase 6: Final delivery evidence and merge-bar closure

**Purpose**: After review closure, complete final delivery evidence and merge-bar verification; user merges.
**Order**: T010 after T009.

- [ ] T010 **Build lane** / pipeline owners. After T009 review closure: Complete final delivery evidence on the PR (both-control screenshots attached/readable, e2e receipt, gate verdicts including honest `SKIPPED (scope-empty)` labels) and perform merge-bar verification. User merge authority remains with the owner; Quill/Build do not merge (`brief.md` Closing bar; Q8).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work.
- **Phase 1 (T001)**: Depends on T000; blocks feature edits.
- **Phase 2 (T002–T003)**: Depends on T001; failing component tests for both stories before production wiring.
- **Phase 3 (T004–T005)**: Depends on T003; production wiring with separate green checkpoints (append-only, then replace/exclusions/buffer/locking).
- **Phase 4 (T006–T008)**: Depends on T005; verification, Done when evidence, and open delivery PR.
- **Phase 5 (T009)**: Depends on T008 (delivery PR open); review findings posted on that PR.
- **Phase 6 (T010)**: Depends on T009; final delivery evidence and merge-bar after review closure.

### User Story Dependencies

- **User Story 1 (P1)**: After T001 — failing tests in T002; append wiring in T004; full Done when still needs T007–T008.
- **User Story 2 (P1)**: Failing tests in T003 before any production wiring; replace wiring in T005 after T004 append primitives.
- US2 is not optional relative to ticket Done when; both stories are required for delivery.

### Parallel Opportunities

- No same-file `[P]` tasks inside `ForYouPage.tsx` / `ForYouPage.test.tsx` / `MovieCard.tsx` / `forYouApi.ts`.
- T006 gate runs are not `[P]` with T007 (e2e depends on implemented UI).
- Screenshot capture and delivery-PR open (T008) follow a green e2e path (T007).

### Parallel Example: none for core wiring

```text
# Same-file For You visit state prevents parallel US1/US2 implementation tasks.
# Keep T002 → T003 (failing tests both stories) → T004 → T005 (wire append then replace) sequential.
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. T000 Gate 1 receipt
2. T001 DEV-541 pickup confirm
3. T002–T003 failing component tests for both stories
4. T004 Load more visit append (append-only green checkpoint)
5. **STOP and VALIDATE** append-only component behaviour
6. Continue to T005 replace (required for ticket Done when)

### Incremental Delivery

1. Gate 1 + pickup
2. Failing tests for both stories
3. Load more component-complete (append-only checkpoint)
4. Replace + visit exclusion component-complete (exclusions/buffer/locking checkpoint)
5. Web gates + running-app e2e + screenshots + open delivery PR
6. Review on open PR + final delivery evidence / merge-bar

### Notes

- Taste microcopy/placement for the replace control stays within `ASSUMPTIONS.md`; do not invent new Patron rulings in tasks.
- Record component/web-gate results separately from the running-app e2e receipt (`brief.md` Test and task order step 5).
