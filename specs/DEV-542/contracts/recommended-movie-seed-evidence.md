# Contract: Recommended movie seed and evidence (DEV-542)

**Source**: Ticket Wanted / Done when (`recon-DEV-542:52–65,89–93`); Patron Q2–Q3 (`brief.md`; `CONCLUSIONS.md`).
**Status**: Ruled public API shape for this ticket. Not an implementation commit.

## Surface

Existing recommendation JSON responses on:

- `GET /recommendations/for-you`
- `GET /movies/{id}/recommendations`

Routes and documented status behaviour are preserved. Shared DTO remains `RecommendedMovieDto`.

## Removed

| JSON / type | Disposition |
|---|---|
| `reasons` | Removed from the response; not retained alongside the new fields |
| `RecommendedMovieReasonDto` | Removed |
| Reason `kind` / `text` mapping (`ToReasonDto`) | Retired where unused after the switch |

## Added / required fields

### `seed` (For You only)

| JSON name | Type | Notes |
|---|---|---|
| `movieId` | integer | Seed movie id |
| `title` | string | Seed title used in the visible line |
| `signal` | string enum | One of `liked`, `watched`, `watchlist` |

- For You: every item MUST include a populated `seed` object.
- Related: the `seed` key MUST be omitted entirely (do not serialize null).
- OpenAPI describes `seed` as optional on the shared DTO to preserve one generated type.

### `evidence` (both surfaces)

Array of objects, strongest first. Empty array is valid.

| JSON name | Type | Notes |
|---|---|---|
| `criterion` | string | Evidence criterion identity from DEV-518 |
| `text` | string | Server-built display text (web renders as delivered) |
| `matchedNames` | string array | Full ordered matched names; may be longer than the names shown in `text` |

Name formatting rule when more than three matched names: server builds `text` with the first three names plus `+N` (example: five names → three names and `+2`) while `matchedNames` retains all five.

## Invariants

- Preserve movie identity, score, and recommendation order for the same inputs (no scoring/ranking/selection change).
- Contract chain: C# DTO → OpenAPI (`web/src/api/openapi.json` + drift snapshot) → `npm run generate:api` → `web/src/api/types.ts` and `web/src/mocks/handlers.ts` regenerated together.
- Web consumes generated types only; no hand-rolled duplicates or hand-edits of generated files.
- Pending-enrichment Related behaviour that returns an empty list remains unchanged.

## Out of contract for this ticket

- New routes or status codes.
- Grouping For You by seed (DEV-538).
- Load more / swap (DEV-517).
- Changes to which movies are recommended or how they are scored/ranked.
