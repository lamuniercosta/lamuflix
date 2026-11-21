# Data model: DEV-539 Crowd links

**Authority**: `brief.md` Q2–Q3, Q6; ticket AC 2; `spec.md` Key Entities.

## Movie TMDb identity

| Field | Rules |
|---|---|
| `tmdb_id` | Nullable integer (or equivalent EF-mapped numeric) on `movies`. Set on successful TMDb enrichment or successful backfill match. Remains null when unmatched or when TMDb enrichment never assigned it. |

## Recommendation pair

| Field | Rules |
|---|---|
| Source library movie id | Required; identifies the owning library movie. |
| Recommended TMDb id | Required; catalogue id from the first recommendations page. May or may not exist in the library. |
| Uniqueness | Unique on `(source library movie id, recommended TMDb id)`. |
| Ownership | Source-owned rows; successful fetch replaces the source’s set (including empty). |

## Derived Crowd link

Not a persisted row. At library feature load:

1. Index library movies by TMDb id where present.
2. For each stored pair whose recommended TMDb id maps to a distinct library movie, mark an undirected binary link.
3. Ignore self-ids. Do not add reciprocal bonus. Do not call TMDb during scoring.

## Lifecycle

| Event | TMDb id | Recommendation rows |
|---|---|---|
| Successful enrichment + successful recommendations fetch | Set/updated | Replaced with returned set (may be empty) |
| Successful enrichment + failed recommendations fetch, id unchanged | Set/updated | Preserved |
| Successful enrichment + failed recommendations fetch, id changed | Set to new id | Stale prior rows cleared |
| Backfill match | Set together with rows | Written atomically with id |
| Backfill skip (has id / no IMDb) | Unchanged | Unchanged |
| Backfill unmatched / failed | Remains null / incomplete | Not committed as a partial success |
