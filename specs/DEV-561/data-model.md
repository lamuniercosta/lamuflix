# Data model: DEV-561 outside-library discovery

**Authority**: `brief.md` Q2–Q7; `CONCLUSIONS.md` Q2–Q7; `spec.md` Key Entities.

## Recommendation link (persisted)

Existing `movie_recommendations` / `MovieRecommendationRecord` rows, extended in place:

| Field | Rules |
|---|---|
| `source_movie_id` | Required; owning library movie. Part of composite key. |
| `recommended_tmdb_id` | Required; TMDb catalogue id from the first recommendations page. Part of composite key. May or may not exist in the library. |
| `title` | Nullable string. Discovery excludes null, empty, and whitespace-only titles. Written from first-page recommendations. |
| `release_year` | Nullable int (or equivalent). Optional card metadata; both years must be known and equal for title/year library exclusion. |
| `poster_path` | Nullable string. Optional card image path; web falls back when null or malformed. |
| Uniqueness | Unique on `(source_movie_id, recommended_tmdb_id)` — unchanged. |
| Ownership | Source-owned; cascade delete with source movie — unchanged. |
| Crowd consumers | ID-only binary link consumers remain valid when metadata columns are null. |

## Discovery read projection

`IRecommendationCandidateSource` exposes one set-based `OutsideLibraryRecommendationSnapshot` read. It supplies source ID, seed state and title, candidate TMDb ID and its title/year/poster tuple, and whole-library identity (TMDb ID, title and year). `EfRecommendationCandidateSource` owns the EF read, with no per-candidate queries. Core performs ranking and normalization. The existing library-only Crowd read continues to consume ID-only links.

## Discovery candidate (derived, not persisted)

Computed at read time from titled links. Ordering is mandatory:

1. Reject Disliked first. Assign Liked weight once to liked sources, otherwise Watched weight to watched sources. Watchlist alone is ineligible. Drop null, empty and whitespace-only titles, then deduplicate source/candidate pairs.
2. Group by recommended TMDb ID and, from those eligible titled deduplicated contributors, select one canonical atomic title/year/poster tuple per candidate from the highest-weight contributing seed (lowest library MovieId on tie). An ineligible or losing seed MUST NOT supply fallback identity or displayed metadata.
3. Exclude any candidate whose recommended TMDb ID appears on any library movie (TMDb-ID check covers the whole library).
4. Apply normalized-title plus equal known-year fallback to that canonical tuple only, against library movies that lack a TMDb ID; both years must be known and equal. A missing year cannot establish the match. Normalization is Unicode NFC + trim + collapsed whitespace + ordinal case-insensitive comparison while preserving punctuation and accents.
5. Count remaining unique candidates with at least one eligible seed for the first home-library denominator. Require at least two distinct contributing sources for a card and the second denominator.
6. Sum the contributing weights in a numeric range sufficient for the configured weights and source count. Sort by score descending, then TMDb id ascending, and take at most 20.

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

Two highest-weight contributing library seeds (MovieId ascending on weight ties). Format: `Recommended for {seed title} and {seed title}` as plain text with display-safe title sanitization. Both distinct contributors remain even when titles coincide.

## Lifecycle

| Event | Link rows |
|---|---|
| Successful enrichment or backfill fetch | Replace the source's complete link set in one transaction after fetching first-page metadata. A concurrent reader sees a complete committed old or new set. |
| Enrichment/backfill fetch or replacement write failure | Prior rows preserved. A later eligible run may retry through the existing typed client and resilience policy. |
| Backfill on a matched source with titleless rows | Hydrate title and optional year/poster through the same per-source replacement. |
| Discovery HTTP read | No writes; no provider HTTP |
