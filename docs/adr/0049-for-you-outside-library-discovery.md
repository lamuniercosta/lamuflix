---
status: accepted
---

# Allow a separate For You row for movies outside the library

This decision amends decision 2 of [ADR-0011](ADR-0011.md). The main For You list and the Related rail remain library-only and retain their existing eligibility rules.

For You may show one separate row titled **Not in your library yet**. It is a read-only discovery row. Eligible movies come from the locally persisted first-page TMDb recommendations already fetched for library movies under [ADR 0048](0048-tmdb-crowd-similarity.md). Every recommendation in the row gives a reason grounded in library seed titles.

Serving the row uses local data and makes zero provider HTTP calls. Viewing history and taste data stay on the machine. The row offers an outbound movie information link. Playback, requests, downloads, purchases, new provider lists, and peer-history collaborative filtering remain outside this decision.

Basis: DEV-561 Wanted 1, 6–8, Done when, and Out of scope. Patron approved this boundary in [the DEV-561 conclusions](../../specs/DEV-561/CONCLUSIONS.md#q1--adr-0049-boundary-amending-adr-0011-decision-2).