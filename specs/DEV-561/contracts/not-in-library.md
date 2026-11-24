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

1. C# response DTO in `src/LamuFlix.Core/Features/Recommendations/` (Core Recommendations)
2. Regenerate and commit `web/src/api/openapi.json` (OpenAPI snapshot; drift verification green)
3. Regenerate `web/src/api/types.ts`
4. `web/src/features/for-you/` consumes generated types only (no hand-rolled duplicates)

Endpoint registration uses `TypedResults`. Error paths use shared RFC 7807 `ProblemDetails`. Empty eligible / config-off cases remain HTTP 200 `[]`.

## Client-derived URL

Web derives the outbound TMDb movie page URL from `tmdbId`. The API does not return a URL field.
