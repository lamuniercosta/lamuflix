# DEV-541 grill conclusions

---

## Q1. Stable paging and exclusions

**Keel asked**

> [from Keel] /grill-with-docs DEV-541. First decision: should stable paging mean slices of a single deterministic capped MMR ranking for the same inputs, with excludes applied before ranking and caps, then offset and take applied and hasMore based on an additional cap-eligible result? I recommend yes, using the existing ascending movie-ID tie-break, because excluded movies should consume neither diversity quota nor page positions. Changing exclusions between requests would change the ranking and require restarting at offset zero. Basis: recon-DEV-541 lines 12 and 18-21. Please rule on this decision only. I will record the full exchange in specs/DEV-541/CONCLUSIONS.md.

**Patron answered**

> [from Patron] Confirmed: stable paging is a slice (offset, take) of one deterministic full-ranking capped MMR list (ascending movie-ID tie-break, caps applied inside the selection loop over the whole ranking), with exclude filtered before MMR/caps and hasMore true iff at least one cap-eligible result exists past offset + take (DEV-541 Wanted 2, 4, 5, 7; Open points: Caps).

Identical ranking inputs yield stable slices. An excluded movie consumes neither a page slot nor a cap slot. An updated exclusion set can change the ranking, so paging with that set starts at offset zero. A full page can have `hasMore: false` if the caps leave no additional eligible result.

---

## Q2. Seed and collection eligibility

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q2. For the ticket defaults of three per best seed and one per collection, I recommend that each selected movie consume one slot from its existing best-seed ID and, when it has a collection ID, one slot from that collection. A movie with no collection ID would consume only its seed slot. Skip a candidate if either applicable cap is full, and count selections across the whole ranking before slicing. This keeps the caps independent and avoids grouping unrelated standalone movies together. Basis: DEV-541 caps and recon-DEV-541 intake evidence on MMR and MovieFeaturesMapper. Do you confirm these cap semantics?

**Patron answered**

> [from Patron] Confirmed: each selected movie consumes one slot from its best-seed ID (default cap 3) and, only when CollectionId is non-null, one slot from that collection (default cap 1, where only the earliest eligible unwatched collection member by release year, null year last, tie-break lower movie ID, may take the collection slot); candidates with a full applicable cap are skipped inside the MMR loop across the whole ranking before slicing (DEV-541 Wanted 2-3; Open points: Caps, Collection rule).

A standalone movie uses only a seed slot. Collection eligibility follows release order, with a deterministic movie-ID tie-break. Applying the caps to each page independently would violate the ruling.


---

## Q3. Collection ordering and promotion

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q3. For collection eligibility, I recommend choosing the earliest unwatched member from the eligible, exclusion-filtered candidates before MMR. If that representative is excluded, the next eligible member can represent the collection. If MMR later cannot select it because its best-seed cap is full, do not promote a later member. This preserves the earliest-eligible rule while allowing exclusions to remove a candidate before ranking. Do you confirm this boundary, including how the rule applies when the configured collection cap exceeds one?

**Patron answered**

> [from Patron] Confirmed: order each collection's eligible, exclusion-filtered unwatched candidates (already clearing the strong-match bar) by release year ascending, null year last, tie-break lower movie ID, and keep only the earliest MaxPerCollection (default 1) member(s) before MMR; an excluded member is removed before that cut so the next eligible member advances, whereas a kept representative skipped inside MMR because its best-seed cap is full is never backfilled by a later collection member (DEV-541 Wanted 2-3; Open points: Collection rule).

Preserve the existing strong-match eligibility bar. Apply the release-order cut before MMR and then apply the caps during MMR. Cover exclusions and seed saturation as distinct cases.


---

## Q4. HTTP input validation

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q4. What is the input-validation contract for offset and exclude? I recommend offset default 0, a single nonnegative integer, and repeated exclude values carrying one movie ID each, with distinct IDs used in ranking and an explicit bound on raw values so duplicate input cannot bypass it. Keep take validation as it is and reject malformed or over-limit input through the existing validation response. Please confirm or supply the ticket-defined serialization, bounds, and invalid-ID behavior.

**Patron answered**

> [from Patron] Confirmed: offset defaults to 0 and must be a single integer >= 0 (no upper bound, parsed with take's CultureInfo.InvariantCulture / BadHttpRequestException 400 pattern and validated in GetForYouRecommendationsQueryValidator); exclude accepts 0..MaxExcludeCount raw repeated query values (exclude=1&exclude=2, default option MaxExcludeCount = 50 matching CandidatePoolSize), where each raw entry must parse as a valid positive MovieId (> 0 via MovieId.TryCreate) or fail with 400 before deduplicating for ranking and silently ignoring unknown library IDs (DEV-541 Wanted 5, Done when, Open points; RecommendationEndpoints.cs:97-119, MovieId.cs:20-32).

The raw limit counts duplicates, and ranking consumes distinct valid IDs. Offsets beyond the ranking return an empty page. Avoid arithmetic overflow when checking for an extra result with a large valid offset.


---

## Q5. Candidate pool and ranking horizon

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q5. For CandidatePoolSize, I recommend preserving its existing ceiling on inputs to MMR, applying exclusions and the collection release-order cut before that ceiling so removed candidates cannot occupy a pool slot, and computing hasMore solely from the resulting capped ranking. This keeps paging stable and prevents larger offsets from expanding the ranking universe. Do you confirm the ordering and pool boundary, or does the ticket require a different one?

**Patron answered**

> [from Patron] Correction from ticket text (DEV-541 Problem 4, Wanted 7; GetForYouRecommendationsQueryHandler.cs:65-83): CandidatePoolSize is NOT an MMR input ceiling in For-You (only an upper bound on take). The handler scores all eligible movies clearing the strong-match bar, applies exclude and the collection release-year cut, runs capped MMR across that full remaining candidate list (selecting up to offset + take + 1 items is sufficient because MMR is prefix-stable), slices [offset, offset + take), and sets hasMore when a selected item exists at index offset + take.

CandidatePoolSize bounds take. Every candidate that survives eligibility, exclusions, and the collection cut may participate in ranking. A prefix with one extra selected item can answer hasMore while preserving the full-ranking semantics in Q1.


---

## Q6. Contract migration and caller recon

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q6. The follow-up recon found the API producer, endpoint test, and generated client chain, but it could not enumerate concrete web callers. I recommend an initial Wisp implementation-pickup check for bare-array callers, then include only the mechanical response.items adaptation for any callers that need it to preserve current behavior, alongside the ticket-named generated contract and mock changes. This allows planning without claiming the caller inventory is complete and keeps UI behavior within the ticket boundary. Do you confirm this treatment?

**Patron answered**

> [from Patron] Confirmed: update the contract chain (OpenAPI snapshot, web/src/api/openapi.json, web/src/api/types.ts), mock fixture/handler (web/src/mocks/fixtures.ts forYouRecommendationsData where handlers.ts:46 sources success.GetForYouRecommendations), and mechanically adapt any existing web callers and tests consuming the bare For-You array to unwrap response.items so the web build/test gate stays green without adding DEV-517 UI behavior (§2.3 item 6 forced edit; DEV-541 Wanted 6, Done when, Out of scope).

Patron authorized only response-shape adaptations to existing callers and tests. The follow-up recon did not establish the complete caller inventory. Confirm it at pickup before editing consumers.


---

## Q7. Scope and task ordering

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q7. I recommend freezing the implementation scope to For-You caps, collection ordering, stable paging and exclusions, the endpoint and contract-chain migration, and focused verification. Keep DEV-517 load-more and swap, DEV-538 reason grouping, DEV-542 card layout and text, and DEV-543 seed weights, threshold and formula out. Sequence caller recon first, then Core options, ranking and unit tests, then API validation, wrapper and HTTP tests, then OpenAPI snapshot and generated web contract, mock and consumer migration, then verification. Do you confirm this scope and dependency order?

**Patron answered**

> [from Patron] Confirmed: freeze DEV-541 scope and dependency order as stated (caller recon -> Core options/ranking/caps/unit tests -> API query/validation/page DTO/HTTP tests -> OpenAPI snapshot and generated web contract/mocks/mechanical caller unwrap -> verification), keeping DEV-517, DEV-538, DEV-542, and DEV-543 out of scope (DEV-541 Wanted, Out of scope, Where it lands, Needs).

Each phase builds on the confirmed output of the preceding phase. Any other work is a follow-up issue, not a finding in this round.


---

## Q8. Acceptance and gate evidence

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q8. I recommend acceptance evidence covering deterministic ties and disjoint stable slices, seed and collection caps including release-year and exclusion cases, empty and beyond-end pages, hasMore at both sides of an exact page boundary, and HTTP offset/exclude validation, dispatch, and wrapped JSON. Verify the OpenAPI snapshot and generated web contract, run the existing web build/test gate, and apply the normal applicable Size M backend gates. Can Quill use this test and gate strategy, with a regression for an offset large enough to overflow a naive offset plus take calculation?

**Patron answered**

> [from Patron] Confirmed: adopt that acceptance evidence and Size M verification matrix, including the overflow-safe large-offset regression test (e.g. offset near int.MaxValue so slicing/hasMore does not overflow offset + take + 1), OpenAPI snapshot/contract verification, existing web build/test gate, and standard backend gates (DEV-541 Done when; AGENTS.md Verification commands).

Plan focused unit and HTTP integration coverage plus the OpenAPI, generated client, and web checks. Report each applicable gate using its actual result.


---

## Q9. Review closing bar

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q9. For the Size M code-review and ship-review closing bar, I recommend requiring Risk, Standards, and Spec reports, closing every accepted in-scope finding at every severity including Low, and recording a disposition with rationale for all other findings. Required gates must meet their configured criteria. This catches small contract or paging defects while allowing documented out-of-scope follow-ups. Do you confirm that bar?

**Patron answered**

> [from Patron] Confirmed: adopt the standard Size M closing bar (Risk, Standards, and Spec review reports; every accepted in-scope finding closed at all severities including Low; explicit rationale recorded for any non-accepted or out-of-scope finding; all required gates meeting their configured criteria in harness.yml).

Every axis needs an explicit report. Accepted Low findings have the same closure obligation as other accepted findings.


---

## Q10. Review round cap and grill closure

**Keel asked**

> [from Keel] DEV-541 /grill-with-docs Q10. I recommend the task-pipeline cap of two review rounds, with any third-round findings routed to user review through the PR. This gives one remediation and re-review cycle within the agreed closing bar. Do you confirm the cap and that the DEV-541 grill can close on Q1-Q10?

**Patron answered**

> [from Patron] Confirmed: adopt the task-pipeline two-round review cap (any third-round findings route to user review on the PR), and the DEV-541 grill is closed on Q1-Q10.

The confirmed decisions are ready for the Phase 2 brief and Spec Kit drafting by Quill.

---

## Gate 1 Ruling (Phase 2 Step 7)

**Verdict**: `gate1: provisional` marked in `specs/DEV-541/spec.md`; zero structural owner checkboxes (`blocked: structural`).

- `specs/DEV-541/checklists/requirements.md` User-Visible Outcome items are all ticked (`[x]`) with `SMOKE-001` defined in `spec.md` (`spec.md` lines 21–29; `checklists/requirements.md` lines 32–37).
- Plan challenge is frozen by Keel with all 21 findings dispositioned and `/speckit-analyze` clean (`brief.md`, `plan.md`, `tasks.md`).
- No ticket contradiction or constitution departure exists (`spec.md` §Owner question — Gate 1; `brief.md` Q1–Q10; `specs/PRODUCT.md` §5).

---

## Gate 1 Merge Receipt

**Verdict**: Gate 1 verified.

- Owner Lamunier Costa (`lamuniercosta`) merged the spec PR into `main` on 2026-11-19 at 17:16:23 UTC. [Spec PR #447](https://github.com/lamuniercosta/lamuflix/pull/447)
- Merge commit: `a79e38dfd9d930e7e123df36487c943240f31943`.
- Phase A base: `1ac83297c1ba8ffc936987e8b198ce8dc612b01a`.

---

## Phase 3 Step 6 Ruling — `ForYouRecommendationsPage` shape and `harness.yml` mutation config

**Verdict**: (1) `ForYouRecommendationsPage` does not require `IReadOnlyList<RecommendedMovie>` — a pure positional record `ForYouRecommendationsPage(ImmutableList<RecommendedMovie> Items, bool HasMore)` satisfies the spec; (2) adding a `harness.yml` mutation exclusion or lowering any gate threshold is prohibited.

- `specs/DEV-541/spec.md` (§Key Entities line 169; FR-006 line 159; FR-009 line 162), `specs/DEV-541/plan.md` (§Approach step 3 line 66), and `specs/DEV-541/tasks.md` (T009 line 94) specify `ForYouRecommendationsPage` only as the Core page record carrying the `Items` slice and `HasMore`, with no `IReadOnlyList<RecommendedMovie>` requirement; callers reading the slice should use `.Items`.
- `AGENTS.md` (§Non-negotiables; §Configuration) and `specs/DEV-541/tasks.md` (Conventions line 16; T016 line 126) treat `harness.yml` as the single source of truth and prohibit editing `harness.yml` exclusions or lowering thresholds to bypass a failing mutation gate.

