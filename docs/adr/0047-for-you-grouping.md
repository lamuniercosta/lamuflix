# Group For You recommendations on the client

**Status** — Proposed. The response additions require the owner to approve the amendment to Done when line 54 in the [DEV-538 spec](../../specs/DEV-538/spec.md#owner-question--gate-1). Gate 1 also requires the owner to merge the spec PR.

## Context

DEV-538 presents reasons for an existing ranked recommendation page. DEV-541 supplies paging and exclusions, DEV-542 supplies one best seed and card evidence, DEV-543 supplies the ranked strong-match set, and DEV-517 supplies Load more and Swap behavior. The existing response cannot identify a candidate's membership in a collection with a watched movie or distinguish between two causes of an empty page. The ticket calls for both distinctions within a single initial request. See [brief Q1–Q4](../../specs/DEV-538/brief.md#settled-decisions).

## Decision

### Response contract

Use the existing `GET /api/recommendations/for-you` and extend its response with two fields, subject to the owner amendment.

- Add optional nullable integer `startedCollectionId` to the shared recommendation item DTO. Populate it only in the For You projection when the candidate belongs to a collection with at least one watched movie in the library. The ID lets the client choose one candidate per started collection. Apply `JsonIgnoreCondition.WhenWritingNull` so the API omits the field for candidates without a qualifying collection and preserves the Related JSON payload.
- Return `{ items, hasMore, hasSeeds }` on every successful For You response. Compute the required boolean `hasSeeds` from the presence of any eligible liked, watched, or watchlist seed before candidate selection, strong-match filtering, caps, paging, or exclusions. A successful initial response with zero items uses this flag to distinguish an empty seed set from an empty result with seeds.

Compute both values in the existing For You handler using the read paths verified at T001. Watched membership in the library qualifies a collection independently of dislike, candidate eligibility, and exclusions. Use the liked, watched, and watchlist seed predicate on every successful path, including empty eligible candidates, the ranking signal guard, and offset past the end. Reuse existing reads without adding per-movie round trips. Map the values through the existing Core result, endpoint, and DTOs. Append `int? StartedCollectionId = null` to the shared DTO. Regenerate OpenAPI and TypeScript from the C# DTOs before web consumption. The generated TypeScript item field is an optional nullable number. Ranking, candidate eligibility, scores, paging semantics, and storage schema stay unchanged. See [brief Q1, Q2, and Q7](../../specs/DEV-538/brief.md#settled-decisions).

### Grouping and caps

Run pure grouping on the client over the first ranked response. Deduplicate by movie ID, retaining the first API occurrence. Render “Next in a series you started” first. Pick one movie per `startedCollectionId` by earliest known release year, with null or omitted `releaseYear` treated as unknown. Order unknown years last and break ties by API rank, including ties between two unknown years. Use the same ordering within the series row.

Assign the remaining movies by their single best seed. Group liked and watched movies by machine-readable best-seed movie ID and reported signal. T001 verifies those landed DEV-542 fields and the title or label source. Select up to four liked or watched seed rows with at least two candidates each, ordered by descending group size and then first API appearance. Follow them with “Close to your watchlist” and place “More for you” last.

Keep the **four seed-row maximum** and **six-card maximum per themed row** together in one client file. Fold every singleton themed row, including a singleton series row, into More for you. Send unselected seed groups and cards above the themed card cap there directly as well. More for you can have one card and has no themed card cap. Seed rows, the watchlist row, and More for you preserve API rank within each row, including folded cards. See [brief Q3 and Q5](../../specs/DEV-538/brief.md#settled-decisions).

## Consequences

- The client can build all initial rows from one response. Themed rows stay fixed during DEV-517 Load more and Swap, which act only on More for you and exclude and deduplicate against movie IDs in every rendered row. Allow one More for you action in flight and ignore obsolete replies after a page reset. An empty follow-up preserves rendered cards. A successful initial response with zero items selects the empty state using `hasSeeds`, even with `hasMore: true`. If the initial response contains items and More for you has zero cards with `hasMore: true`, its labelled region still exposes Load more.
- HTTP tests cover the serialized field values, omission behavior, and Related compatibility. OpenAPI and generated-type checks cover schema shape. Component tests cover one initial request, deterministic grouping and caps, both empty states, and cross-row deduplication during follow-up requests. See [implementation plan](../../specs/DEV-538/plan.md) and [tasks](../../specs/DEV-538/tasks.md).
