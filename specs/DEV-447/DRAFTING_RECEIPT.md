# DEV-447 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-03
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-447-spec` · branch `feature/447-spec`
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` in one ask (plus requirements checklist + this receipt). Grill already closed (`brief.md` Q1–Q8); mandatory `before_specify` grill hook not re-executed. `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge). `.specify/feature.json` already pointed at `specs/DEV-447` and was left unchanged.

**Inputs**: frozen `specs/DEV-447/brief.md` (Q1–Q8), `CONCLUSIONS.md`, Maestri notes `DEV-447`, `recon-DEV-447` (incl. ticket text lines 96–145), `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only).

**Artifacts written (Quill-owned)**:

- `specs/DEV-447/spec.md`
- `specs/DEV-447/plan.md`
- `specs/DEV-447/tasks.md`
- `specs/DEV-447/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, ADR bodies (none for this ticket), constitution, production/test code, `harness.yml`.

**Owner checkbox**: none (brief Status; Q8). No constitution amendment, ADR, ASSUMPTIONS.md, or glossary/`CONTEXT.md` rewrite authorized.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS and recon facts.

**needs decision**: none. Brief Q1–Q8 freeze automatic eligibility, suspicious conjunction, detail bounds, rejected-ID exclusion, file/test allowlists, AC1/AC2 evidence split, gates/ordering, and loop discipline with no open gaps for drafting.

**Encoding notes (not new decisions)**:

- No `research.md` / `data-model.md` / `contracts/` — public contract chain untouched; decisions frozen in brief (precedent DEV-426/DEV-439).
- SMOKE-001/002 encode brief Q6 live AC2 via existing manual-match / `PUT .../imdb` under test-portal-policy; not a new web e2e suite (Size M, no ui tag; web out of scope).
- Unrelated branch `harness.yml` modification called out to preserve and exclude from this task staging (brief Q5, Q7).
- Synthetic Memento / O Brother stubs are ticket-derived; recon line 94 raw incident payloads remain unavailable — never claimed as captured traffic (brief Status).

## Analyze round 1 repairs (Quill; 2026-11-03)

Applied Keel's numbered fix list only. Preserved `brief.md`, `CONCLUSIONS.md`, and unrelated `harness.yml`. No new decisions or scope changes. **Does not claim analyze clean** — Keel rereads and reruns analyze.

| # | Severity | Disposition | Files |
|---|---|---|---|
| 1 | MEDIUM | Applied — removed `[P]` from T002/T003; Dependencies/order require T002 → T003 → T004 | `tasks.md` |
| 2 | MEDIUM | Applied — AC1 both titles assigned to unit files and `MetadataProviderLookupTests.cs` WireMock in T002; T004 explicitly requires both AC1 selections there | `tasks.md` |
| 3 | MEDIUM | Applied — removed literal complexity `15`; build/refactor thresholds from `harness.yml` only | `plan.md` |
| 4 | MEDIUM | Applied — local tasks.md sections disambiguated from canonical Phase 3/4/5; replaced T00N with T002–T004; seat ownership explicit (Wisp/Keel/Gauge/Anvil/Cog) | `plan.md`, `tasks.md` |
| 5 | MEDIUM | Applied — Cog `/refactor` once is required stage; code edits remain conditional | `plan.md`, `tasks.md` (T009) |
| 6 | LOW | Applied — Principle II labelled Explicit Handlers and Decorators; V Validated Inputs / Consistent Error Responses | `plan.md` |
| 7 | LOW | Applied — removed "or equivalent"; AC2 needs `/api/movies/7` and `/api/movies/29` | `spec.md` |

## Analyze repair round 2 (Quill; 2026-11-03)

One remaining MEDIUM: restore numbered local `## Phase N:` headings for task-pipeline Phase 3 step 2 `/implement Phase N` dispatch, while keeping the explicit canonical-phase disclaimer. Draft consistency repair only — not a PR review round. Preserved `brief.md`, `CONCLUSIONS.md`, unrelated `harness.yml`. Task IDs, sequential deps, and round-1 coverage repairs unchanged. **Does not claim analyze clean.**

| Item | Disposition | Files |
|---|---|---|
| MEDIUM missing numbered phase headings | Applied — restored local Phase 0–9 headings (`## Phase N:`); disclaimer states LOCAL tasks.md phase IDs ≠ task-pipeline phases; seat map Wisp/Keel/Gauge Pickup, Anvil phases 2–4, Cog phase 5, Gauge phase 6, local 8–9 ↔ canonical Phase 4/5; mirrored in plan Ordering | `tasks.md`, `plan.md` |

## Plan challenge repair round 1 (Quill; 2026-11-03)

Applied Keel's numbered 10-item fix list from plan-challenge round 1 Patron clarifications (brief.md / CONCLUSIONS.md) plus findings-DEV-447-Sentry, Compass, Ledger. Preserved task IDs, scope, Q4, review caps, unrelated harness.yml. Did **not** mark PLAN FROZEN or claim analyze clean — Keel verifies. Facts from recon-DEV-447 citations in brief/findings only; no new searches or measurements. No `needs decision` / `needs recon` gaps encountered while applying the list.

| # | Disposition | Paths |
|---|---|---|
| 1 | Applied — Classify/Map before eligibility on automatic direct + fetched detail; InvalidResponse/MissingTitle preserved; only mapped Found → fallback/NotFound; preservation regressions named | `spec.md` (Scope item 7; Edge Cases; FR-004a/FR-006–FR-009); `plan.md` (Approach; Test Strategy; Risks); `tasks.md` (T003; T006) |
| 2 | Applied — Q4 residual wrong-identity risk in US3/AC1 guidance + plan Risks; no denylist/new heuristic; AC1/AC2 mandatory; T001 Wisp nonsecret Type/title/year/rating/director/runtime observation | `spec.md` (US3; FR-005/FR-012; Assumptions); `plan.md` (Risks; Approach pickup); `tasks.md` (T001) |
| 3 | Applied — automatic-only guard entry points; shared Classify/FoundOutcome/FetchByIdAsync must not unconditionally enforce guards | `spec.md` (Edge Cases; FR-007; Key Entities); `plan.md` (Approach); `tasks.md` (T006) |
| 4 | Applied — separate valid OMDb Found (zero TMDb) from rejected mapped detail NotFound (TMDb invoked); Type repair must not invert short-circuit | `plan.md` (Test Strategy); `tasks.md` (T004; T008) |
| 5 | Applied — T003 names Detail helper/direct fixture repair Type=movie + ≥1 usable Q4 field; preserve Requests.Count==1 | `tasks.md` (T003); `plan.md` (Test Strategy Bounds) |
| 6 | Applied — T007/T008 preserve mapper edges via ManualImdbId or eligible automatic fixtures; outside primary four = payload-only | `spec.md` (FR-011; Edge Cases); `tasks.md` (T007; T008) |
| 7 | Applied — private helpers inside two production files only; unmet gate needs ruling; T003 WireMock=`MetadataProviderLookupTests` vs unit existing HTTP stubs | `plan.md` (Constraints; Approach; Test Strategy); `tasks.md` (T003; T006; T009) |
| 8 | Applied — new-behavior red where feasible; green locks for manual/failure/one-request; both AC1 IDs in unit+WireMock; T004 reuses T002 AC1 | `plan.md` (Approach; Ordering Phase 2); `tasks.md` (Phase 2 Goal/Independent Test; T002–T004; Dependencies; Sequencing) |
| 9 | Applied — mutation const/no new static init; FakeTimeProvider/seeded Random; no unrelated comment cleanup; T001 actual DTO visibility recon | `plan.md` (Constraints); `tasks.md` (Conventions; T001; T005) |
| 10 | Applied — owners T010/T014 Gauge; T017 Gauge+Conductor env; T019 Rigger/Gauge/Conductor; T020 Conductor; format cites Q7/Phase 3 step 5; Phase 0 Patron→Rigger→user→Keel; retained numbered Phase N + disclaimer (L6 declined) | `plan.md` (Approach Phase A); `tasks.md` (Gate 1; Phase 0; T010; T014; T017; T019; T020); `checklists/requirements.md` |

**Checklist**: refreshed notes for plan-challenge repair round 1; still does not claim analyze/plan-challenge clean.

## Plan challenge repair round 2 (Quill; 2026-11-03)

One numbered precision fix only (implements already-approved FR-004a; no new decision). Preserved task IDs, scope, Q4, caps, harness.yml. Did **not** mark PLAN FROZEN. Does **not** claim plan-challenge clean — Keel verifies.

| # | Disposition | Paths |
|---|---|---|
| 1 | Applied — T003 now requires green preservation tests on **both** automatic direct and automatic fetched-detail paths: Response=True unusable/missing Title retains Failed(InvalidResponse) + MissingTitle logging and no TMDb invocation; existing direct invalid-response assertions stay intact. plan.md Classification preservation row mirrors both-path coverage. | `tasks.md` (T003; Sequencing T003); `plan.md` (Test Strategy Classification preservation row) |
