# DEV-428 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-30
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-428-spec` · branch `feature/428-spec`
**Method**: Spec Kit drafting from frozen `brief.md` (grill closed Q7/12), Phase 2 analyze round 1 repair (C1/C2/U1), then Phase 2 step 7 plan-challenge repair per `adjudication-DEV-428-plan` Quill 8-item list (Patron P1–P20). Templates: `.specify/templates/{spec,plan,tasks}-template.md`. Form precedent: `specs/DEV-426/`. `/speckit-analyze` and plan challenge owned by Keel (Quill applies listed repairs only).

**Inputs**: frozen `specs/DEV-428/brief.md` (Q1–Q7 + Post-grill Analyze Ruling U1 + Phase 2 Step 7 P1–P20), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, Maestri notes `DEV-428`, `recon-DEV-428`, `recon-DEV-428-2`, `adjudication-DEV-428-plan`, `adjudication-DEV-17-plan` (cited substance only), `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only; no constitution edits), Conductor Phase 2 step 4 round 1 fix list (C1/C2/U1), Conductor Phase 2 step 7 Quill repair list (items 1–8).

**Artifacts written (Quill-owned)**:

- `specs/DEV-428/spec.md`
- `specs/DEV-428/plan.md`
- `specs/DEV-428/tasks.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, constitution, ADR bodies, production code, dirty `harness.yml`.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ticket/recon facts, Keel analyze fix list, and Phase 2 Step 7 Patron dispositions.

**Round 1 repair applied (Keel analyze fix list)**:

1. **C1**: Restored full Principle VI Constitution Check and requirements — mandatory `traceparent`/`tracestate` inject/extract via `TraceContextCarrier` / `Propagators.DefaultTextMapPropagator`, producer/consumer Activity with names in `TelemetryConstants`, redelivery `ActivityLink`, error event movie-id context, and mandatory proofs (spec FR-003/004/010/011; plan VI row + approach/tests; tasks T005/T007/T009/T010).
2. **C2**: Principle V compliant 404 representation — RFC 7807 `ProblemDetails` with `traceId` via existing handler path while preserving decided 404 status; ProblemDetails miss assertions (spec FR-009/011 US2/US5; plan V row; tasks T011/T013).
3. **U1**: Incorporated Patron U1 ruling — closed consumer tokens `MalformedMessage`, `IneligibleRequest`, `UnauthorizedSourcePath`, `CachePromotionFailed`; closed log fields `{MovieId}`, `{FailureReason}`, `{Redelivered}`, `{ErrorType}`; zero raw paths (spec FR-005–008/011 US3/US4; plan constraints/tests; tasks T009/T010).

**Phase 2 step 7 plan-challenge repair applied (Quill 8-item list → saved locations)**:

1. **HTTP contract and policy (F1/F8/F11/L1; P1/P8/P11/P12)**: Synchronized FR-009, Scope item 3, US1–US2/US5, plan Constitution V/II rows + endpoint approach + test strategy, T000/T011/T013. Inherited `LibraryEndpoints` caller policy with T000 verification; `{id:int}`; nonpositive/unknown numeric misses; one `NotFoundException` → `GlobalExceptionHandler` mechanism + `.Produces(200, image/jpeg)` / `.ProducesProblem(404)`; uniform sanitized 404 (`status`/`title`/`type` identical; only `traceId`/optional `instance` may vary); serve-time SOI → 404 on corrupt; `nosniff` on 200; restart + startup `IOptions.Value`; competing optional miss mechanisms and hot-reload removed.
2. **Filesystem trust (F2/F3; P2/P3)**: Updated FR-005/FR-007, US3, plan approach/risks/test strategy, T002/T003/T009/T010. Reject temp/canonical/source file and ancestor symlink/reparse; outside targets untouched; prior regular JPEG preserved; own contained temp cleanup only; immediate canonicalize/revalidate at source handoff; residual name-based TOCTOU documented under trusted operator directories; planted-destination and immediate-handoff proofs; races not claimed impossible.
3. **Concurrency and replacement (F5; P5)**: Qualified SAC claims in FR-001, US5, plan Performance Goals/risks/tests, T003/T004/T010/T013. Rare overlapping last-writer-wins; `FileShare.ReadWrite | FileShare.Delete` (+ async); no generation lock/unbounded retries; T000/T002 verify actual .NET 10 target-platform primitive; T003/T013 prove Windows read-during-promotion; no claim these runtime checks already passed.
4. **DI and responsibilities (L2/L3/Compass Low-2; P13/P14/P19)**: FR-004/FR-010, plan approach/structure/VII row, T006/T007/T009/T012/T013. Concrete singleton `ThumbnailCache` registered in `RabbitMqServiceCollectionExtensions`; same instance + `IOptions` for endpoint/consumer; private consumer movie/source helpers; cache owns containment/SOI/open/promotion; T006/T007/T013 prove options/validation/registration including broker-disabled HTTP availability and conditional Program/ServiceDefaults; authorized files/layers unchanged.
5. **Operations and cancellation (F4/F6/F7; P4/P6/P7)**: FR-003/FR-008, US4, Edge Cases, plan Summary/ops/risks, T007/T009/T010/T016. Intentional publish-failure/enrichment-redelivery coupling; confirm-before-ack retained; `BinaryNotFound` disable-or-repair/restart/DLQ runbook; disk monitoring/manual purge with host stopped or disabled after restart; own-contained-temp best-effort cleanup on shutdown before requeue; no sweeper/eviction/alerts/counters/circuit breaker/new outbox.
6. **Trace robustness (F9; P9)**: FR-004, US4 scenario 3, plan VI row/consumer approach, T009/T010. Non-throwing extraction fallback preserving propagation/Activity requirements and terminal failure contract; malformed payload plus corrupt-`traceparent` proof in T010.
7. **Validation and honest proof status (F10/L4/L5; P10/P15/P16)**: FR-011/FR-012, plan pickup/constraints/gates, T000/T002/T003/T005/T009/T016/T022/T023. T000 records exact landed validator/access-policy/handler/platform facts and stops for cited decisions on drift; no invented startup probe/root validator; unique temp dirs + deterministic cleanup; required symlink proof in capable environment (SKIP = missing required evidence, never PASS); mutation-safe `const`/computed literal/static-initialization discipline; no changed gate thresholds.
8. **Receipt and optional polish (L6/Compass Low-1/Low-3; P17/P18/P20)**: This receipt maps each numbered fix; `needs decision: none`. Optional SC-006 citations added on T021–T024; TelemetryConstants identifiers remain T005 implementation-time alignment to T000-landed pattern (P17). Informational ProblemDetails/`nosniff` refinement recorded in Scope precedence / Clarifications / FR-009 without owner checkbox (P20). Plan freeze / Gate 1 **not** claimed.

**Encoding notes (not new decisions)**:

- Routing keys stated as matching queue names (`thumbnail`, `thumbnail.dead-letter`) per existing Features-style exchange convention in recon/`RabbitMqTopology` — applying brief “existing exchange/routing conventions,” not a product choice.
- OpenAPI regeneration cites established opt-in workflow + `npm run generate:api` from prior contract-chain tickets (DEV-80/DEV-425), matching brief “existing generation commands.”
- `TelemetryConstants.cs` bounded edit for thumbnail span-name consts is constitution-forced by Principle VI / C1 (same pattern as Features/Enrichment); not a bespoke metric instrument (Q6 still bars custom metrics/health).
- No owner structural checkbox added (brief: none identified; P20 informational only).

**needs decision:** none.

**needs recon:** none for this document repair. Phase B pickup (T000) still rechecks DEV-17 land + seam/policy/validator/platform drift before coding.

**Clarify**: coverage Clear; no `[NEEDS CLARIFICATION]` markers; Q5 supersedes Q4 queue spelling recorded in spec Assumptions/Clarifications; Phase 2 Step 7 P1–P20 fully encoded.

**Status**: four Spec Kit artifacts repaired for Phase 2 step 7 Quill 8-item list and saved. Gate 1 not approved. PLAN FROZEN not claimed. No production gates run.

**Next**: Keel re-challenge verification against appended brief and all 20 dispositions.
