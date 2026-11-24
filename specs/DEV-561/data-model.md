# Data model: DEV-561 outside-library discovery

**Authority**: `brief.md` Q2–Q7; `CONCLUSIONS.md` Q2–Q7; `spec.md` Key Entities.

## Recommendation link (persisted)

Existing `movie_recommendations` / `MovieRecommendationRecord` rows, extended in place:

| Field | Rules |
|---|---|
| `source_movie_id` | Required; owning library movie. Part of composite key. |
| `recommended_tmdb_id` | Required; TMDb catalogue id from the first recommendations page. Part of composite key. May or may not exist in the library. |
| `title` | Nullable string. Discovery counts only non-blank titles. Written from first-page recommendations. |
| `release_year` | Nullable int (or equivalent). Optional card metadata; both years must be known and equal for title/year library exclusion. |
| `poster_path` | Nullable string. Optional card image path; web may fall back when null. |
| Uniqueness | Unique on `(source_movie_id, recommended_tmdb_id)` — unchanged. |
| Ownership | Source-owned; cascade delete with source movie — unchanged. |
| Crowd consumers | ID-only binary link consumers remain valid when metadata columns are null. |

## Discovery candidate (derived, not persisted)

Computed at read time from titled links:

1. Exclude blank-title links.
2. Exclude candidates whose `recommended_tmdb_id` appears on any library movie.
3. Exclude candidates that match a library movie lacking TMDb id when normalized titles equal and both release years are known and equal.
4. Aggregate eligible Liked/Watched (non-Disliked) source contributions per candidate TMDb id after pair dedupe.
5. Keep candidates with at least two distinct contributing sources.
6. Select one atomic title/year/poster tuple from the highest-weight contributing seed (lowest MovieId on tie).
7. Sort by summed weight descending, then TMDb id ascending; take at most 20.

## Discovery seed contribution (derived)

| Rule | Detail |
|---|---|
| Disliked | Rejected first; never contributes. |
| Liked | Contributes once at Liked weight (default 2). |
| Watched (not liked) | Contributes once at Watched weight (default 1). |
| Watchlist only | Ineligible. |
| Liked + Watched | One contribution at Liked weight. |

## Discovery reason (derived)

Two highest-weight contributing library seeds (MovieId ascending on weight ties). Format: `Recommended for {seed title} and {seed title}` with display-safe title sanitization. Both distinct contributors remain even when titles coincide.

## Lifecycle

| Event | Link rows |
|---|---|
| Successful enrichment recommendations fetch | Replace source rows with id + title + optional year/poster |
| Enrichment/backfill fetch failure | Prior rows preserved |
| Backfill on matched source with ID-only rows | Hydrate titles (and optional year/poster) for that source |
| Discovery HTTP read | No writes; no provider HTTP |
