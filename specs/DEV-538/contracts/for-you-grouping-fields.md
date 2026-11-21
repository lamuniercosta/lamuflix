# Contract note: For You grouping fields (DEV-538)

**Status**: Draft — contingent on owner checkbox amending Done when line 54  
**Authority**: `brief.md` Q1–Q2, Q7; `CONCLUSIONS.md`; `recon-DEV-538:49-54`  
**Route**: existing `GET /api/recommendations/for-you` (single request retained)

## Page envelope

Successful For You body becomes (coherent example: one series-eligible item, paging available, seeds present). Endpoint mapping copies `HasSeeds` from the Core For You result onto `ForYouRecommendationsPageDto`. Null `startedCollectionId` is **omitted** on the wire — never serialized as `null`.

```json
{
  "items": [
    {
      "movieId": 101,
      "title": "Example Sequel",
      "startedCollectionId": 7
    },
    {
      "movieId": 202,
      "title": "Standalone Match"
    }
  ],
  "hasMore": true,
  "hasSeeds": true
}
```

| Field | Required | Meaning |
|---|---|---|
| `items` | yes | Existing ranked page slice (DEV-541) |
| `hasMore` | yes | Existing paging flag (DEV-541) |
| `hasSeeds` | yes | `true` when any liked, watched, or watchlist seed exists **before** candidate selection, strong-match filtering, caps, paging, or swap exclusions; otherwise `false`. Required on every successful For You response, including empty `items`. Mapped from Core `HasSeeds`. |

## Item field

| Field | Required | Meaning |
|---|---|---|
| `startedCollectionId` | no (optional nullable) | Non-null `int` when the candidate belongs to a collection that has at least one watched movie in the library. Used by the client for Wanted rule 1a (at most one movie per collection). **Omitted on the wire when null** (`JsonIgnoreCondition.WhenWritingNull`) — do not emit `"startedCollectionId": null`. |

Populate `StartedCollectionId` **only** in the For You projection. Related (`/movies/{id}/recommendations`) keeps null and therefore omits the property, preserving prior Related JSON aside from fields already owned by DEV-542.

## Test split

- **HTTP / integration**: assert populated collection ID, omitted null cases, both empty-page `hasSeeds` boolean values, and Related JSON free of `startedCollectionId` when null (Related omission lives here).
- **OpenAPI / generated types**: assert For You schema/type shape only — required boolean `hasSeeds` on the page envelope and optional nullable item field `startedCollectionId`. Do not use OpenAPI or generated-type checks to prove Related omission.

## Unchanged

- No new route; no second For You request for grouping.
- DEV-542 `seed` / `evidence` remain as landed.
- DEV-541 offset/take/exclude and `hasMore` semantics remain as landed.
- Movie-specific Related bare-list (or its landed shape) is not wrapped in the For You envelope.

## Contract chain

1. Update C# DTOs (`ForYouRecommendationsPageDto`, `RecommendedMovieDto`).
2. Refresh OpenAPI snapshot / `web/src/api/openapi.json`.
3. Regenerate `web/src/api/types.ts` (and MSW handlers via existing generate path).
4. Web consumes generated types only — no hand-rolled duplicates.
