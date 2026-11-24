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

Computed at read time from titled links. Ordering is mandatory:

1. Drop blank-title links; keep only eligible Liked/Watched (non-Disliked) contributors after source/candidate pair dedupe.
2. Group by recommended TMDb ID and, from those eligible titled deduplicated contributors, select one canonical atomic title/year/poster tuple per candidate from the highest-weight contributing seed (lowest library MovieId on tie). An ineligible or losing seed MUST NOT supply fallback identity or displayed metadata.
3. Exclude any candidate whose recommended TMDb ID appears on any library movie (TMDb-ID check covers the whole library).
4. Apply normalized-title plus equal known-year fallback to that canonical tuple only, against library movies that lack a TMDb ID; both years must be known and equal. A missing year cannot establish the match. Normalization is Unicode NFC + trim + collapsed whitespace + ordinal case-insensitive comparison while preserving punctuation and accents.
5. Count remaining candidates; keep those with at least two distinct contributing sources before returning any card.
6. Sort by summed weight descending, then TMDb id ascending; take at most 20.

Home-library ≥1-seed and ≥2-seed acceptance counts MUST use this same Option A exclusion (steps 3–4) before denominators are reported.

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
