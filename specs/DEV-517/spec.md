# Feature Specification: For You load more and suggestion replacement

**Feature Branch**: `feature/517-spec`

**Created**: 2026-11-21

**Status**: gate1: provisional

**Input**: Ticket DEV-517 (parent DEV-490; size M); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-517`, `recon-DEV-517`; `specs/PRODUCT.md`. Constitution cited only in the plan. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope; Q1–Q8; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR.

## Clarifications

### Session 2026-11-21

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (For You Load more and per-card replacement; visit-owned exclusions; DEV-541 `take`/`offset`/`exclude`/`items`/`hasMore` as sole server paging; named web files only; TDD then web gates, running-app e2e, PR screenshots), visit boundary (Q1), exclude-only-replaced + offset scan with client skip of rendered IDs (Q2), server-enforced MaxExcludeCount with rollback on failed replacement and no client numeric cap (Q4 superseding Q3), single in-flight operation and non-error exhaustion (Q5), feature-owned state with optional MovieCard action slot (Q6), verification sequence (Q7), and Size M closing bar with two-round caps (Q8) are frozen in `brief.md` / `CONCLUSIONS.md`. Button wording and card-control placement remain Patron `[assumed]` taste in `ASSUMPTIONS.md`. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: For You page (`/for-you`) recommendation grid
- **What the user sees or does**: A page-level **Load more** control appends the next available suggestions to the existing grid without reloading the page. While a Load more or replacement is in flight, further Load more / replace interactions for that visit stay locked. When no further eligible suggestions remain, Load more is no longer offered as an available next-page action.
- **What it needs to show up**: Default Compose stack; For You recommendations available through the existing DEV-541 endpoint; enough distinct ranked candidates beyond the first page so append is observable.

- **Where**: Each For You recommendation card (shared `MovieCard` with an optional action supplied only by For You)
- **What the user sees or does**: A per-card replace control swaps that slot to the next unseen suggestion. A successfully replaced movie stays out of the grid for the rest of the continuous mounted For You visit (including later Load more, replacement, retry, and incidental refetch). Leaving For You or reloading starts a new visit. A failed replacement keeps the original card and shows retryable replacement feedback. An exhausted replacement (no unseen candidate) keeps the original card and shows a concise non-error exhaustion status. Non–For You `MovieCard` callers look and behave as before when the optional action is absent.
- **What it needs to show up**: Same stack as above; at least one replaceable card with a next unseen ranked candidate under the visit’s committed exclusions.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001** (Load more — extend `web/e2e/for-you-review.e2e.ts`): **Given** a library with enough ranked For You candidates beyond the first page, **When** the viewer opens For You and activates Load more, **Then** additional suggestions append to the existing grid without a full page reload and previously shown cards remain.
- **SMOKE-002** (Replace and same-visit exclusion — same e2e file): **Given** For You showing at least one card with a next unseen candidate, **When** the viewer replaces that card and then loads more or replaces again in the same visit, **Then** the slot shows a different suggestion and the retired movie ID does not reappear for the remainder of that visit.
- **SMOKE-003** (PR screenshots — delivery evidence): **Given** both controls are visible in the running app, **When** the delivery PR is prepared, **Then** it includes screenshots of Load more and the per-card replace control.

## Scope

**In scope** (`brief.md` Frozen scope / Approach and state transitions / Files / Test and task order / Closing bar; Q1–Q8; ticket Wanted / Done when / Needs):

1. **Visit-owned For You state**: Own ordered rendered items, committed replaced IDs, scan position and exhaustion for the active exclusion set, and one pending operation on the continuous mounted For You visit. Preserve initial load plus initial error/empty/retry. Incidental refetches and action retries keep successful visit exclusions. Discard or abort late results after leaving the visit (Q1, Q5).
2. **Load more on DEV-541 slices**: Send only committed replaced IDs in `exclude`. Advance server `offset` by the requested `take` per fetched slice under that set. Skip IDs already rendered or retired; keep received order; consume any unconsumed candidates from the current scan before fetching more; append without rearranging the grid. Exhaustion when there are no eligible buffered items and the endpoint reports `hasMore=false` (Q2).
3. **In-place replacement**: Tentatively add the target ID to exclusions; restart at `offset=0` with that set; scan slices in order for the first candidate absent from the grid and retirement set; replace in place; commit the new card, exclusion, and restarted scan state together; retain eligible unused rows from the last slice for later Load more (Q2, Q5).
4. **Failure, exhaustion, and serialization**: At most one in-flight Load more or replacement per visit. Failed or exhausted replacement leaves the card, committed exclusions, and prior paging intact. Request failure shows retryable replacement feedback; exhaustion shows concise non-error status. Failed append preserves the grid and committed paging for retry. Server enforces `MaxExcludeCount`; rejected requests (including HTTP 400) use the ordinary failed-replacement path with no hardcoded client numeric cap, no error-message parsing, and no API expansion (Q4 superseding Q3; Q5).
5. **Frontend file envelope**: Focused edits in `web/src/features/for-you/ForYouPage.tsx`, `web/src/features/for-you/forYouApi.ts`, optional stateless action on `web/src/library/MovieCard.tsx`, plus `web/src/features/for-you/ForYouPage.test.tsx` and `web/e2e/for-you-review.e2e.ts`. Consume generated DEV-541 types via existing `web/src/api/client.ts` / `web/src/api/types.ts` (read-only). No production backend changes, new dependency, new layer, or rewrite of unnamed files (Q6).
6. **Verification**: Confirm DEV-541 contract at pickup; TDD in `ForYouPage.test.tsx` for the brief’s listed transitions; applicable web gates; running-app e2e proving append, in-place replace, follow-on append/replace, and retired-ID absence; PR screenshots of both controls. Report empty-scope C# gates as `SKIPPED (scope-empty)` on exit 2 (Q7, Q8).

**Out of scope**:

- A second paging mechanism beyond DEV-541 `offset` / `exclude` / deterministic order.
- Production backend, OpenAPI shape, DTO, or generated-type edits inside this five-file change. On pickup contract drift, stop and report to Bernstein; any upstream correction uses the normal C# DTO → OpenAPI → TypeScript chain outside this ticket’s file envelope, then resume only after verifying the corrected generated contract on the intended base.
- Hardcoded client `MaxExcludeCount` / default 50 threshold, limit-specific UI latch, or typed limit discriminator.
- New NuGet/npm dependency; new project, top-level folder, or architectural layer; database schema; `Features:LocalPlay`, secrets, or `Process.Start`.
- DEV-538 grouping, DEV-542 seed/evidence presentation changes beyond the optional card action slot, DEV-543 formula work.
- Claiming PLAN FROZEN, Gate 1 merge, Build PASS, or runtime acceptance from this draft.
- Anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Wanted / Done when / Needs are already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions. Only Keel declares PLAN FROZEN after independent analyze/reread **and** completion of plan challenge and adjudication under Q8. Gate 1 follows separately after freeze.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Visit**
The continuous mounted For You page session. Successfully replaced movie IDs stay excluded across Load more, replacement, retry, and incidental refetch until the user leaves For You or reloads.
_Avoid_: treating each click as a new visit; persisting exclusions across remount

**Committed exclusions**
Replaced movie IDs already accepted into visit state. Wire `exclude` for initial load and Load more (append) carries only these committed IDs. A replacement scan may add only its target ID tentatively for that request and commits it only on success; all other rendered IDs stay out of the payload.
_Avoid_: putting visible grid IDs into `exclude`; treating a tentative replacement target as committed before success

**Scan**
Client walk of DEV-541 offset slices under one exclusion set, skipping already-rendered or retired IDs, optionally buffering unused eligible rows from the last slice.
_Avoid_: a second server ranking or client-only re-sort of the grid

**In-flight operation**
At most one pending Load more or replacement for the active visit; success commits only to that visit; late results after leave/unmount are discarded or aborted.
_Avoid_: overlapping scans that race offset / exclusions / rendered IDs

Consumed vocabulary (already in use; not redefined): For You, DEV-541 page envelope `{ items, hasMore }`, `take`, `offset`, `exclude`, MaxExcludeCount, contract chain C# DTO → OpenAPI → TypeScript.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load more appends the next suggestions (Priority: P1) 🎯 MVP

A viewer on For You can append the next ranked suggestions to the current grid without reloading the page, using the existing DEV-541 paging contract.

**Why this priority**: Ticket Wanted item 1 and Done when require a working Load more control in the running app.

**Independent Test**: Component tests cover successive offsets under an unchanged exclusion set, append and deduplication, buffer-before-fetch, exhaustion when `hasMore=false` with no eligible buffer, failed append preserving grid/paging, and single in-flight locking with Load more. E2e SMOKE-001 proves append without reload against a library with enough ranked candidates.

**Acceptance Scenarios**:

1. **Given** For You has rendered an initial page with `hasMore` true, **When** the viewer activates Load more, **Then** the next eligible suggestions append to the existing grid without a page reload and without rearranging already shown cards.
2. **Given** a committed exclusion set, **When** Load more requests the next slice, **Then** the request sends only committed replaced IDs in `exclude`, advances `offset` by the requested `take` for that set, skips IDs already rendered or retired, and preserves received order among newly appended items.
3. **Given** unconsumed eligible candidates remain from the current scan, **When** Load more runs, **Then** those candidates are used before another fetch.
4. **Given** no eligible buffered items and the endpoint returns `hasMore=false`, **When** the scan completes, **Then** Load more is not offered as an available next-page action.
5. **Given** a Load more request fails, **When** the failure is handled, **Then** the existing grid and committed paging remain so the operation can be retried.
6. **Given** a Load more or replacement is already in flight, **When** the viewer tries another Load more or replace, **Then** the second interaction does not start until the pending operation finishes (or is discarded with the visit).

---

### User Story 2 - Replace a suggestion for the rest of the visit (Priority: P1)

A viewer can replace one For You card with the next unseen suggestion; the retired movie stays absent for the remainder of that mounted visit.

**Why this priority**: Ticket Wanted item 2 and Done when require in-app replace plus same-visit non-return of the replaced suggestion, with e2e proof and screenshots.

**Independent Test**: Component tests cover in-place replacement with offset-zero restart, skipping visible IDs across slices, buffering unused rows, atomic commit of card + exclusion + scan state, rollback on HTTP 400 and other failures, exhaustion non-error status, Load more continuity after success and after failed swap, visit reset, and late-result discard. E2e SMOKE-002 proves replace then follow-on append/replace with retired ID absent. SMOKE-003 captures both controls on the PR.

**Acceptance Scenarios**:

1. **Given** a For You card with a next unseen ranked candidate, **When** the viewer activates replace on that card, **Then** that slot updates in place to the first candidate not already rendered or retired, under a scan that restarts at `offset=0` with the target ID tentatively excluded.
2. **Given** a successful replacement, **When** the visit continues with Load more or further replacements, **Then** the retired movie ID does not reappear for the rest of the mounted visit, including after incidental refetch or retry.
3. **Given** a successful replacement that leaves unused eligible rows in the last fetched slice, **When** a later Load more runs, **Then** those rows remain available so append can continue in rank order without skipping them.
4. **Given** a replacement request fails (including HTTP 400 from server MaxExcludeCount enforcement), **When** the failure is handled, **Then** the uncommitted exclusion is rolled back, the last committed grid / exclusions / offset / buffer / `hasMore` stay intact, retryable replacement feedback is shown, and Load more remains usable under the committed state.
5. **Given** the ranking yields no unseen candidate (`hasMore=false` with nothing eligible left to show), **When** replacement completes that scan, **Then** the original card, committed exclusions, and paging remain, with a concise non-error exhaustion status.
6. **Given** the viewer leaves For You or reloads, **When** For You mounts again, **Then** a new visit starts without the prior visit’s committed exclusions.
7. **Given** shared `MovieCard` is rendered outside For You without the optional action, **When** the card displays, **Then** existing non–For You behaviour is unchanged.

---

### Edge Cases

- Replacement or Load more still in flight when the visit unmounts: discard or abort; do not apply late results to a new visit.
- Empty initial For You / initial error / retry: remain available; successful visit exclusions still apply once a visit has committed them.
- Server rejects exclude payload (HTTP 400) with no typed limit discriminator: treat as ordinary failed replacement (rollback + retryable error); do not latch a permanent client limit state or parse error messages.
- Load more after a failed swap: continues under the last committed exclusion set and paging.
- Duplicate IDs across slices: client skips already rendered or retired IDs; does not rearrange the grid.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: For You MUST provide a Load more control that appends the next eligible suggestions without reloading the page.
- **FR-002**: Each For You recommendation card MUST offer a replace control that swaps that slot to the next unseen suggestion.
- **FR-003**: For initial load and Load more (append), wire `exclude` MUST carry only committed replaced movie IDs (never currently rendered IDs). A replacement scan MAY add only its target ID tentatively and MUST commit that ID only on success; all other rendered IDs MUST stay out of the payload. Load more MUST advance `offset` by the requested `take` under the committed exclusion set.
- **FR-004**: Replacement MUST restart ranking consumption at `offset=0` with the target ID tentatively excluded, scan slices in order, skip rendered/retired IDs, and replace in place on success.
- **FR-005**: A successfully replaced movie ID MUST remain absent from the grid for the continuous mounted For You visit, including subsequent Load more, replacement, retry, and incidental refetch.
- **FR-006**: Leaving For You or reloading MUST start a new visit without prior committed exclusions.
- **FR-007**: The feature MUST use the existing DEV-541 contract (`take`, `offset`, `exclude`, `items`, `hasMore`) as the sole server paging mechanism.
- **FR-008**: At most one Load more or replacement operation MAY be in flight per visit; results MUST commit only to the active visit; late results after leave/unmount MUST be discarded or aborted.
- **FR-009**: Failed append MUST preserve the existing grid and committed paging for retry.
- **FR-010**: Failed replacement (including HTTP 400) MUST roll back the uncommitted exclusion, preserve committed grid/exclusions/paging, show retryable replacement feedback, and leave Load more usable.
- **FR-011**: Exhausted replacement (no unseen candidate) MUST leave the original card, committed exclusions, and paging intact with a concise non-error exhaustion status.
- **FR-012**: The client MUST NOT hardcode a MaxExcludeCount / default-50 threshold, parse error bodies for a limit discriminator, or expand the API for limit signalling (Q4).
- **FR-013**: Unconsumed eligible candidates from the current scan MUST be available to the next Load more before an unnecessary fetch that would skip them.
- **FR-014**: Changes MUST stay within the named web implementation and test files; shared `MovieCard` MAY gain only an optional stateless action supplied by For You; non–For You callers MUST keep current behaviour.
- **FR-015**: Delivery MUST include green applicable web gates, a running-app e2e covering both controls and same-visit exclusion, and PR screenshots of both controls; empty-scope C# gates MUST be reported as `SKIPPED (scope-empty)` on exit 2.

### Key Entities

- **For You visit state**: Ordered rendered recommendations, committed replaced IDs, scan offset/buffer/exhaustion for the active exclusion set, and pending-operation flag for one mounted visit.
- **Recommendation slice**: DEV-541 response `{ items, hasMore }` for a given `take`, `offset`, and `exclude` set.
- **Optional card action**: Presentation-only replace affordance on `MovieCard`, owned and supplied by For You.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A viewer can append at least one additional For You page of suggestions via Load more without a full page reload, with prior cards still visible.
- **SC-002**: A viewer can replace a For You card in place and see a different suggestion in that slot.
- **SC-003**: After a successful replacement, the retired movie does not reappear during the same mounted visit across at least one follow-on Load more or replacement.
- **SC-004**: Component tests cover the brief’s listed transitions (initial states; unchanged exclusions and successive offsets; append/dedup; in-place replace with offset-zero restart; skip visible IDs; buffer unused rows; exhaustion; rollback on HTTP 400 and other failures; retry; Load more continuity; one in-flight operation; visit reset with late results discarded).
- **SC-005**: Running-app e2e in `web/e2e/for-you-review.e2e.ts` proves both controls and same-visit exclusion; the delivery PR shows screenshots of both controls.
- **SC-006**: Applicable web gates pass; unchanged C# gates that exit 2 for empty scope are labelled `SKIPPED (scope-empty)`, never PASS.
- **SC-007**: Non–For You `MovieCard` usage without the optional action remains behaviourally unchanged in targeted coverage.

## Assumptions

- Continuous mounted For You visit boundary, retryable replacement error copy posture, single in-flight locking, non-error exhaustion notice, and card control placement/label wording are Patron `[assumed]` taste or UX rulings recorded in `ASSUMPTIONS.md` / `CONCLUSIONS.md` (Q1, Q4, Q5, Q6). Exact microcopy strings are not invented here beyond the ticket’s “Load more” label and the replace-control intent.
- DEV-541 is present on the intended Build base with generated `take` / `offset` / `exclude` / `items` / `hasMore`; pickup confirms the contract before feature edits (`brief.md` Test and task order step 2).
- Existing frontend stack (React 19 / TypeScript / Vite, React Query, generated OpenAPI client) is reused; no new state-management library (`PRODUCT.md` §5 item 2; Q6).
- Default Compose stack plus a library with enough distinct ranked candidates is sufficient for e2e and screenshot evidence.
