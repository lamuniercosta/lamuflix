# Research: DEV-561 Outside-library discovery on For You

**Date**: 2026-11-24  
**Authority**: `brief.md` Q1–Q12, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ticket Open-point defaults as amended by Q3 Option A, `recon-DEV-561`, ADR 0049.

## Decision: Separate read-only For You discovery row (ADR 0049)

- **Decision**: Amend ADR-0011 decision 2 to permit one labelled **Not in your library yet** row sourced only from locally persisted first-page TMDb recommendations already fetched for library movies; main For You and Related stay library-only; zero request-time provider HTTP; viewing/taste data stay on the machine.
- **Rationale**: Ticket Wanted 1 / Done when 1; Patron Q1; ADR 0049 Accepted.
- **Alternatives considered**: Mixing outside movies into the main For You grid (rejected); Related outside movies (out of scope); new discover/trending/similar lists (out of scope).

## Decision: Additive metadata on DEV-539 link table

- **Decision**: Add nullable `title`, `release_year`, and `poster_path` to `movie_recommendations` via compatible additive EF migration; reuse enrichment and `--backfill-tmdb-recommendations` to write/hydrate metadata; preserve composite key, cascade, and ID-only Crowd consumers.
- **Rationale**: Ticket Wanted 3 / Where it lands; Patron Q2; recon shows current ID-only rows.
- **Alternatives considered**: Second store or new provider endpoint (rejected); non-nullable columns that break legacy ID-only Crowd rows (rejected).

## Decision: Owner Option A library exclusion

- **Decision**: Exclude by candidate TMDb ID across the library; for library movies without a TMDb ID, match normalized title plus equal known release year; no candidate IMDb persistence and no blanket suppression of unresolved IMDb-only library records.
- **Rationale**: Owner ruling on PR #492; Patron Q3 Option A; recommendations payload has no `imdb_id`; preserves “no extra TMDb call.”
- **Alternatives considered**: Option B external_ids lookups during ingest/backfill (rejected by owner); suppressing candidates whenever any IMDb-only library movie exists (would truncate the row).

## Decision: Local read contract and empty semantics

- **Decision**: `GET /api/recommendations/not-in-library` returns HTTP 200 card array (`tmdbId`, `title`, nullable `releaseYear`, nullable `posterPath`, `reason`) or `[]` when TMDb is disabled, ApiKey is blank, or no candidates qualify; For You contract unchanged; regenerate OpenAPI/TS from C# DTO; web derives TMDb URL from `tmdbId`.
- **Rationale**: Ticket Open-point default route; Patron Q4.
- **Alternatives considered**: Expanding For You DTO in place (rejected); IMDb outbound link (Open-point alternative, not chosen).

## Decision: Deterministic seed weights and ranking

- **Decision**: Reject Disliked first; one contribution per eligible Liked (default 2) or Watched (default 1) library movie; Watchlist alone ineligible; bind weights in `RecommendationOptions`; dedupe pairs; require two distinct seeds; sort weight desc / TMDb id asc; cap 20.
- **Rationale**: Ticket Wanted 4–5 / Done when 3–4; Patron Q5; `[assumed]` tie-break in `ASSUMPTIONS.md`.
- **Alternatives considered**: Watchlist as a seed (rejected); single-seed quality bar (rejected without owner ruling).

## Decision: Web hide-on-empty/loading/error and attribution

- **Decision**: Independent section below the library grid; show only on successful nonempty response; hide while loading, empty, or failed; distinct read-only cards; day-verified TMDb credit beside the visible section; README feature list without taking DEV-559 provider attribution.
- **Rationale**: Ticket Wanted 8–9 / Done when 6–9, 11; Patron Q9; `[assumed]` loading/error suppression in `ASSUMPTIONS.md`.
- **Alternatives considered**: Skeleton/error placeholder for the discovery section (not chosen); Not interested in this ticket (Open-point default out).
