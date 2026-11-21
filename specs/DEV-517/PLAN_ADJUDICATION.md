# DEV-517 plan adjudication

**PLAN FROZEN** after round 1, 2026-11-21. Gate 1, implementation, runtime acceptance and merge clearance remain pending.

Reviewed the untracked plan draft in `feature/517-spec` at source HEAD `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`. Evidence includes Maestri notes `findings-DEV-517-Sentry` (10), `findings-DEV-517-Ledger` (10), `findings-DEV-517-Compass` (5), `DEV-517` and `recon-DEV-517`, plus the brief, conclusions, spec, plan, tasks and requirements checklist. Source HEAD identifies the worktree reviewed. The draft Markdown was outside that commit. Patron subsequently recorded the refinement in the documentation-only commit `3baaf360` on the same branch.

| Axis | Accepted | Accepted in part | Follow-up | Rejected | Total |
|---|---:|---:|---:|---:|---:|
| Risk / Sentry | 8 | 0 | 1 | 1 | 10 |
| Standards / Ledger | 8 | 2 | 0 | 0 | 10 |
| Spec / Compass | 5 | 0 | 0 | 0 | 5 |
| **Total** | **21** | **2** | **1** | **1** | **25** |

## Risk / Sentry

| Finding | Disposition | Rationale and applied evidence |
|---|---|---|
| H1 — stale slice cache (note lines 9–12) | Accepted | The legacy take-only key cannot distinguish DEV-541 slices. Use direct typed requests for this page with explicit parameters and one initial commit per mount. Phase 1 defines the cache boundary. T003/T004 prove post-swap paging uses the new scan and keeps committed state after invalidation. |
| H2 — sequential scan budget (14–17) | Follow-up | A fetch/time cap could suppress a valid first unseen suggestion or show false exhaustion. Patron retained scanning to the requested result or endpoint exhaustion, under one in-flight operation with cleanup cancellation. Phase 1 and T003–T005 require advancing offsets and stopping on failure or cancelled visit. Large-ranking latency remains a follow-up candidate if measured. |
| M1 — repeated 400s (21–24) | Rejected | Q4 and the refinement keep generic failure and user-initiated retry without an automatic retry loop, cap, or card latch. Repeated-key pickup and payload/rollback tests guard request construction. T003 requires static feedback and retry correctness. The proposed status/body logging is outside the necessary feature work. |
| M2 — visit cancellation (26–29) | Accepted | Phase 1 assigns a visit-owned AbortController, slice signal propagation and identity checks before requests and commits. T003 exercises actual abort and stale response handling across a Strict Mode remount during a multi-slice scan. |
| M3 — prior cursor/buffer invalid under new excludes (31–34) | Accepted | Q2 and DEV-541 bind progress to the active set. On swap success T005 replaces offset, buffer and `hasMore` with progress from the successful scan. T003 proves the next append uses the new buffer and never falls back to the prior scan. Failures retain the committed snapshot. |
| M4 — duplicate-only and short slices (36–39) | Accepted | Patron’s refinement fills Load more from active buffer and consecutive slices up to `take` or true exhaustion. T002/T004 cover duplicate-only intermediate and terminal slices, empty terminal UI, ordering and all-or-nothing rollback. |
| L1 — card navigation interaction (43–46) | Accepted | Place the optional button outside navigation/play affordances. T003/T005 check pointer and keyboard activation without opening or playing the movie. |
| L2 — duplicate excludes count against the raw limit (48–51) | Accepted | T003/T005 require set construction and a retry with distinct excludes. T001 confirms repeated query keys on the wire. |
| L3 — raw error content (53–56) | Accepted | T003–T005 require client-owned static action failure feedback and exercise a server body that never appears in the card. This implements Q4 without reading server details for presentation. |
| L4 — sufficient live ranked candidates (58–61) | Accepted | T007 checks at least `2 × take + 2` distinct ranked candidates, then verifies that the planned exclusion leaves two unseen options outside the first two pages. An explicit setup failure identifies insufficient data. |

## Standards / Ledger

| Finding | Disposition | Rationale and applied evidence |
|---|---|---|
| M1 — legacy API exports and test envelope (note lines 5–7) | Accepted | Preserve all legacy exports and add the typed slice path. T001 inventories callers, T002/T003 exercise the additive path in the authorized page suite, and T006 runs the existing API suite unchanged. Q6's five-file envelope still applies. |
| M2 — shared-card regression coverage (9–11) | Accepted | T003 covers the actual action wiring from the feature and asserts the control is absent when omitted. T006 runs the existing colocated suites unchanged. This tests the feature integration and retains Q6's file envelope. |
| M3 — visit-state boundary (13–15) | Accepted in part | Phase 1 specifies a visit-owned state boundary, transport in `forYouApi.ts`, and cache-free page requests. Extract pure logic inside the authorized files if it simplifies the transitions. An exported reducer in the transport module is unnecessary to make the behavior testable through the page, and the draft does not establish a need for that interface. |
| L1 — test libraries (18) | Accepted | Reuse the page test’s client stub, Vitest/RTL and existing assertions in T002/T003. |
| L2 — test style (20) | Accepted | The task conventions require the existing file’s test naming and arrangement and the AGENTS.md comment rules. |
| L3 — red checkpoint publication (22) | Accepted | Record local red results before production wiring. The task conventions distinguish those receipts from passing published commits. |
| L4 — timing and randomness (24) | Accepted | Phase 1 uses visit identity and cancellation without timers or randomness. |
| L5 — existing card action mechanisms (26) | Accepted | Phase 1 explains the slot’s presentation role alongside the card’s feedback mutations and retry behavior. Share existing styling and accessible naming. T006 includes the card-action contrast suite. |
| L6 — document/process duplication (28) | Accepted in part | Resolve the private mirror via `PRIVATE_TICKET_DIR`, condense the plan gate sequence and trim unrelated PASS rows. T000 retains the Gate 1 receipt that blocks pickup. T008 retains screenshot publishing with PR opening because the artifact must be readable on the open PR before T009, while remaining individually identifiable. |
| L7 — live e2e data source (30) | Accepted | T007 uses the existing running-app Playwright spec and supported setup with live endpoint preflight and an isolated/reset state. Any necessary test-local setup stays in the authorized e2e file. |

## Spec / Compass

| Finding | Disposition | Rationale and applied evidence |
|---|---|---|
| M-1 — slice test home (note lines 9–11) | Accepted | T002/T003 exercise transport parameters and page outcomes through the existing page test seam. T001 separately records actual repeated-key serialization. Legacy exports and their unchanged tests stay supported. |
| M-2 — incidental refetch behavior (13–15) | Accepted | Patron recorded the committed grid, exclusions and cursor preservation ruling in CONCLUSIONS.md and ASSUMPTIONS.md. The brief and Phase 1 spell it out. T003 tests focus/invalidation after swap, ordered-grid preservation and subsequent paging. |
| L-1 — smoke label collision (17–19) | Accepted | T007 and T008 namespace DEV-517's e2e and screenshot receipts. The screenshot receipt is `DEV-517 SMOKE-003` alongside the pre-existing e2e label. |
| L-2 — wire format at pickup (21–23) | Accepted | T001 records multi-ID repeated-key serialization on the intended Build base. The DEV-541 generated OpenAPI files remain read-only under Q6. |
| L-3 — e2e test isolation and preflight (25–27) | Accepted | T007 requires isolated or reset library/feedback state, independence from the existing like scenario and its fresh-start requirement, and a live ranked-candidate preflight before driving the controls. |

The round meets Q8. All 25 findings have dispositions and there are no open in-scope Critical, High or Medium plan findings. Patron's refinements preserve the ticket and constitution, with no structural owner question. The ordinary Gate 1 merge remains the Build prerequisite.
