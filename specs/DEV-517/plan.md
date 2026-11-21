# Implementation Plan: For You load more and suggestion replacement

**Branch**: `feature/517-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-517-spec`) | **Date**: 2026-11-21 | **Spec**: `specs/DEV-517/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q8); `specs/DEV-517/CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-517`, `recon-DEV-517` (HEAD `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN after round-one [plan adjudication](PLAN_ADJUDICATION.md). Gate 1, Build and runtime acceptance remain pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` / new `contracts/` — grill Q1–Q8 freeze inventable unknowns; no schema or public API shape change; DEV-541 contract is consumed as-is.

## Summary

Add For You **Load more** and per-card **replace** on the existing DEV-541 paging contract. Own visit state in the For You feature: ordered rendered items, committed replaced IDs, offset/buffer/exhaustion for the active exclusion set, and one in-flight operation. Initial/append wire `exclude` carries only committed replaced IDs; a replacement scan adds only its target tentatively and commits it on success. Load more advances `offset` by `take` under unchanged committed exclusions and appends after client-side skip of rendered/retired IDs. Replace restarts at `offset=0`, scans for the first unseen candidate, and commits card + exclusion + scan state together. Failures roll back uncommitted work; exhaustion keeps the original card with non-error status. Edits stay in the five named web files; no backend or dependency changes.

## Technical Context

**Language/Version**: React 19 / TypeScript / Vite (`web/`) per `specs/PRODUCT.md` §1. No production C# changes in the frozen file envelope.

**Primary Dependencies**: Existing DEV-541 For You endpoint and generated types (`take`, `offset`, `exclude`, `items`, `hasMore`); existing OpenAPI client in `web/src/api/client.ts`; existing React Query usage in the For You feature. No new NuGet/npm dependency (`brief.md` Frozen scope; Q6).

**Storage**: No schema or migration change.

**Testing**: Vitest and React Testing Library in `ForYouPage.test.tsx`, reusing its existing client stubbing and assertions. Playwright in `web/e2e/for-you-review.e2e.ts` against the running app. Run existing `forYouApi.test.ts`, MovieCard and card-action contrast regressions unchanged.

**Target Platform**: Local API + Compose SPA; running-app e2e and PR screenshots are delivery evidence.

**Project Type**: Existing SPA feature folder consuming generated API types; shared presentational `MovieCard`.

**Performance Goals**: No new timing SLA. Avoid overlapping in-flight scans; prefer buffered unconsumed candidates before extra fetches (`brief.md` Approach).

**Constraints**: Charter §2.3 care-list — no new dependency, layer, schema, API shape, LocalPlay/secrets/`Process.Start`, or unnamed-file rewrite (Q6; `PRODUCT.md` §5). Contract chain remains C# DTO → OpenAPI → generated TypeScript (consume only). Q4 forbids hardcoded client MaxExcludeCount and typed limit discriminators. Closing bar: Size M Risk/Sentry, Standards/Ledger, Spec/Compass; zero open in-scope Critical/High/Medium; every accepted finding including Low closed with evidence at reviewed HEAD; two-round caps for plan challenge and implementation review; green applicable web gates; running-app e2e receipt; PR screenshots of both controls (`brief.md` Q8).

**Scale/Scope**: Size M; bounded to the five named web files and visit-local UI behaviour on `/for-you`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox: ticket delivery is preserved; Patron Q1–Q8 closed inventable plan choices without ticket contradiction or constitution departure.

| Principle / rule | Gate | Plan response |
|---|---|---|
| V (Validated inputs / errors) | ProblemDetails | **PASS** — client treats generic HTTP 400 / request failure as failed replacement or failed append per Q4/Q5; no new error taxonomy or API status invention. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, Process.Start, or options changes; no hardcoded MaxExcludeCount (Q4; AGENTS.md non-negotiable on hardcoded thresholds). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS (design)** — spec-local visit/scan/committed-exclusions terms clarify ticket/brief meanings only; no root `CONTEXT.md` edit required for this UI ticket. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — component TDD for visit transitions; Playwright e2e for running-app Done when; no new framework; do not mock data-access drivers (N/A — no DB tests in envelope). |
| Tech stack / API docs | OpenAPI chain | **PASS** — consume generated DEV-541 types; pickup confirms contract. On drift, stop and report to Bernstein; any upstream correction uses the normal C# DTO → OpenAPI → TypeScript chain outside this five-file change; resume pickup only after verifying the corrected generated contract on the intended base (`PRODUCT.md` §1, §5 item 4). |
| Governance / Static-Analysis Gates | Honest exits | Web gates must pass for changed web files. Unchanged C# analyzer/complexity/InspectCode/mutation scope that exits 2 empty MUST be reported `SKIPPED (scope-empty)`, never PASS. Never lower thresholds. |
| Size M closing bar | Two-round caps; axes | Plan challenge and implementation review each ≤ 2 rounds; Risk/Sentry, Standards/Ledger, Spec/Compass required; unresolved blockers at cap return to Conductor with PR disposition (Q8). |

**Post-design re-check**: Still PASS — design adds no port, handler, schema, API field, dependency, or LocalPlay surface. Optional `MovieCard` action remains presentation-only.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-517/
├── brief.md                 # Grill-closed input (Keel)
├── CONCLUSIONS.md           # Patron rulings Q1–Q8
├── ASSUMPTIONS.md           # Patron [assumed] taste/UX tags
├── ticket-text.txt
├── spec.md                  # This feature specification
├── plan.md                  # This file
├── tasks.md                 # /speckit-tasks output
├── PLAN_ADJUDICATION.md      # Round-one dispositions and freeze
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md      # Quill drafting receipt / needs decision
```

Resolve the private mirror via `PRIVATE_TICKET_DIR` from the Spec Kit prerequisites script. The Gate 1 commit surface is `specs/DEV-517/`.

### Source Code (repository root)

```text
web/src/features/for-you/ForYouPage.tsx      # Visit ownership, Load more, per-card action wiring, pending/error/exhaustion UI
web/src/features/for-you/forYouApi.ts        # Typed DEV-541 slice access; preserve items + hasMore
web/src/library/MovieCard.tsx                # Optional stateless action slot; non–For You callers unchanged
web/src/features/for-you/ForYouPage.test.tsx # Component TDD for visit transitions
web/e2e/for-you-review.e2e.ts                # Running-app e2e for both controls + same-visit exclusion

# Read-only inputs (do not hand-edit)
web/src/api/client.ts
web/src/api/types.ts
web/src/api/openapi.json
```

**Structure Decision**: Stay inside the existing For You feature folder and shared `MovieCard`. No new top-level folder, feature project, or state-management library (Q6; `PRODUCT.md` §5 items 1–2, 6).

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase 0 — Research

Grill Q1–Q8 and `recon-DEV-517` already settle inventable behaviour and integration facts. No separate `research.md`. Pickup still reconfirms DEV-541 generated query/response shape before edits (`brief.md` Test and task order step 2). If pickup finds contract drift or missing `offset`/`exclude`/`hasMore`, stop and report to Bernstein; any upstream correction uses the normal C# DTO → OpenAPI → TypeScript chain outside this five-file change; resume pickup only after verifying the corrected generated contract on the intended base — do not invent a second paging mechanism and do not regenerate or change API artifacts inside this ticket’s file envelope.

## Phase 1 — Design & contracts

No new public API contract. Document consumption only:

| Concern | Decision (cited) |
|---|---|
| Paging mechanism | DEV-541 only: `take`, `offset`, `exclude`, `items`, `hasMore` (ticket Needs; Q2) |
| Wire exclude contents | Initial/append: committed replaced IDs only. Replacement scan: committed set plus only the target ID tentatively (commit on success). Never other rendered IDs (Q2; brief Approach §2–3) |
| Replace scan | `offset=0` restart + client skip of rendered/retired IDs (Q2) |
| MaxExcludeCount | Server-enforced; client rollback on failure; no numeric cap / discriminator (Q4) |
| Visit boundary | Continuous mount until leave/reload (Q1) |
| Concurrency | One in-flight Load more or replace per visit (Q5) |
| Visit state and query cache | `ForYouPage` owns the mounted visit and seeds it once from a typed, uncached initial slice at offset zero. Keep the ordered grid, exclusions and cursor after initial commit across incidental focus/invalidation/refetch and action retry. A fresh mount starts a fresh initial request; initial failure may be retried. Keep the existing `useForYou`, `forYouQueryKey`, `FOR_YOU_TAKE` and `ForYouError` exports compatible. Add direct typed slice access through the existing client; initial/append/replace requests for this page bypass React Query’s cache and use their explicit `take`, `offset` and `exclude`. Keep orchestration in the page and typed transport in the existing API module. Extract cohesive pure transition logic inside the authorized feature files only where it simplifies the implementation. |
| Append fill and exhaustion | Load more consumes eligible buffered rows from the committed exclusion set, then fetches consecutive slices until it has `take` unseen items or the endpoint reports `hasMore=false`. Advance by requested `take` per fetch and skip rendered, retired and already staged IDs. Commit the append and next cursor together in rank order. An empty terminal append leaves the grid in place and removes Load more when there are no eligible buffered rows; a failed multi-slice append rolls back all staged progress. |
| Swap commit and wire set | Use a deduplicated set for `exclude`: committed IDs only for initial/append and the committed set plus only the target for replacement. On swap success, replace the previous offset, buffer and `hasMore` together with the exclusion set and card, retaining eligible unused rows only from the successful replacement scan. Failure or true exhaustion preserves the previous committed scan; failures use static client-owned feedback and manual retry, including HTTP 400, without interpolating server error content. |
| Request lifetime | One in-flight action per visit. Own an AbortController and visit identity in the page, pass the visit signal through every slice fetch, abort on cleanup, and check the identity before further requests and before any state commit. Scans advance by `take` and stop for their result, endpoint exhaustion, request failure or visit cancellation. Visit logic needs no timers or randomness. |
| Card action | Render the optional replace button outside the card’s navigation/play affordances. Reuse existing card action styling and accessible naming conventions. The slot presents the feature’s replace action while `MovieFeedbackActions` handles feedback mutations and card retry handles existing card failures. Test real card wiring through ForYouPage, including pointer/keyboard activation without opening/playing the movie and absence of the optional control when omitted. Run existing MovieCard and card-action contrast suites unchanged. |
| File envelope | Five named web files (Q6). Existing API and card regression suites are exercised as-is. |

No `data-model.md` (client visit state is behaviour, not persisted schema). No new `contracts/` file (no DTO/route change).

## Phase 2 — Implementation strategy (for tasks.md)

Order from `brief.md` Test and task order:

1. Complete the spec-PR gate in `task-pipeline` (T000) before Build.
2. Verify the generated DEV-541 query/response contract, initial entry point, repeated-key `exclude` serialization and legacy API export callers (T001).
3. Record local red component-test results before production wiring (T002–T003).
4. Wire typed slice requests + visit state; connect page Load more and optional card action; keep non–For You card path covered.
5. Run applicable web gates, then run the live e2e and publish PR screenshots of both controls (T006–T008). Use the existing running-app Playwright spec and its supported data setup. Give the DEV-517 journey isolated or reset library/feedback state; it must run independently of the DEV-541 like test and must preserve that test’s fresh-start assumption. Preflight against the live endpoint with the scenario's take and initial exclusions: require at least `2 × take + 2` distinct ranked candidates for the initial page, full append, replacement and follow-on. Also verify the planned target’s exclusion leaves at least two unseen candidates outside the initial and appended grid. Fail setup explicitly with observed and required counts. Keep setup in the existing e2e file and record the reset/seed procedure.


## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Overlapping Load more / replace corrupts offset or exclusions | Single in-flight lock; commit only to active visit (Q5) |
| Excluding rendered IDs burns MaxExcludeCount / warps ranking | Wire exclude = committed replaced IDs for append; replacement adds only its target tentatively; client skip rendered (Q2) |
| Client hardcodes exclude ceiling | Forbidden by Q4; treat 400 as failed replacement |
| Late responses after navigation | Abort all outstanding visit requests in cleanup and reject late results by visit identity (T003–T005). |
| DEV-541 drift at pickup | Confirm contract first; on drift stop and report to Bernstein; upstream correction outside this five-file change; resume after verifying corrected contract on intended base; no second pager |
| Shared MovieCard regression | Verify the optional control’s absence and independent activation in T003, and run the existing shared-card suites in T006. |

## Agent context

`.specify/feature.json` already pins `feature_directory` to `specs/DEV-517`. Update SPECKIT plan markers in agent docs only if the harness agent-context script is run for this ticket; do not expand scope into unrelated docs.
