# Contract: GET /api/recommendations/not-in-library

**Authority**: `brief.md` Q4; `CONCLUSIONS.md` Q4; `spec.md` FR-010–FR-011.

## Route

- **Method / path**: `GET /api/recommendations/not-in-library`
- **Registration**: `RecommendationEndpoints` under the existing `/api` route-group authorization convention
- **Side effects**: None. Local read only. Zero provider HTTP calls.

## Success response

- **Status**: `200`
- **Body**: JSON array of cards

| Field | Type | Notes |
|---|---|---|
| `tmdbId` | number (int) | Recommended TMDb catalogue id |
| `title` | string | Non-blank display title |
| `releaseYear` | number \| null | Optional |
| `posterPath` | string \| null | Optional; client builds image URL if present |
| `reason` | string | Two-seed explanation |

Empty eligible set, `Tmdb:Enabled` false, or blank `Tmdb:ApiKey` → `200` with `[]`.

## Unchanged contracts

- `GET /api/recommendations/for-you` request/response shape and library movie ordering remain unchanged.
- Related recommendations remain library-only.

## Codegen

1. C# response DTO in Core/Api recommendation feature
2. Regenerate `web/src/api/openapi.json`
3. Regenerate `web/src/api/types.ts`
4. `web/src/features/for-you/` consumes generated types only (no hand-rolled duplicates)

## Client-derived URL

Web derives the outbound TMDb movie page URL from `tmdbId`. The API does not return a URL field.
