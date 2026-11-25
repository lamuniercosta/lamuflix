---
status: accepted
---

# Allow a separate For You row for movies outside the library

**Ticket** DEV-561 · **Plan review** 2026-11-24

This decision amends decision 2 of [ADR-0011](ADR-0011.md). The main For You list and the Related rail remain library-only and retain their existing eligibility rules.

For You may show one separate read-only discovery row. DEV-561 Wanted 8 supplies its heading. Eligible movies come from the locally persisted first-page TMDb recommendations already fetched for library movies under [ADR 0048](0048-tmdb-crowd-similarity.md). Every recommendation in the row gives a reason grounded in library seed titles.

Serving the row uses local data and makes zero provider HTTP calls. Viewing history and taste data stay on the machine. The row offers an outbound movie information link. Playback, requests, downloads, purchases, new provider lists, and peer-history collaborative filtering remain outside this decision.

## Consequences

The existing first-page links gain nullable title, release year and poster path. Crowd still consumes the IDs only. The existing operator backfill may refresh matched sources with titleless links on rerun, a scoped extension of the already-committed-movie skip in [ADR 0048](0048-tmdb-crowd-similarity.md).

## Rejected options

Candidate IMDb lookup would require an additional provider call because the existing recommendation response lacks candidate IMDb IDs. Blanket suppression would hide unrelated candidates when a library record lacks a TMDb ID. The owner selected the local TMDb ID and title/year rule in Q3. Fetching a second or different TMDb list exceeds the existing first-page boundary.

Basis: DEV-561 Wanted 1, 6–8, Done when, and Out of scope. Patron approved this boundary in [the DEV-561 conclusions](../../specs/DEV-561/CONCLUSIONS.md#q1--adr-0049-boundary-amending-adr-0011-decision-2).
