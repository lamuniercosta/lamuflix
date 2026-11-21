# Use TMDb recommendations as a library similarity signal

Status: Accepted

## Context

Metadata similarity alone misses pairs that TMDb recommends together. [ADR-0011](ADR-0011.md) requires library-only candidates and prohibits local peer-history collaborative filtering. [ADR 0046](0046-shared-recommendation-similarity.md) defines the ordered criteria and weighted denominator for For you and Related.

## Decision

Amend ADR-0011 to use the first page of TMDb movie recommendations as an external item-to-item signal. A stored recommendation in either direction creates one binary Crowd link between distinct eligible library movies. Keep all candidates library-only. Use the existing feature load to resolve links into an immutable lookup for pure scoring.

Amend ADR 0046's six-criterion scoring rule for the active Crowd state. Place Crowd after Themes. Add its default weight of 2 to the denominator for every pair while Crowd is active and TMDb is configured, including unlinked pairs. The default denominator is 17 with Crowd active and 15 when inactive. Use the existing reason selection, with “Often liked together with {seed title}” for For you and “Often liked together” for Related.

## Consequences

- Persist nullable, non-unique TMDb identities and source-owned unique recommendation pairs. Retain out-of-library target IDs so later additions can link without re-enrichment. Resolve duplicate identities without selecting an arbitrary library row. Deleting a source cascades its own pairs and preserves other sources' incoming catalogue IDs.
- TMDb enrichment adds one first-page recommendations fetch using the existing resilient client. A successful empty page replaces prior rows. A fetch failure preserves rows for an unchanged source identity and clears stale rows on rematch.
- The existing Api executable exposes a one-off `--backfill-tmdb-recommendations` mode. It checks configuration and migration before dispatching a Core handler, reuses the shared client and commits each movie ID and its links atomically. Already committed movies are skipped on rerun.
- Crowd activation is independent of TMDb enrichment and off by default until same-library DEV-545 leave-one-out top-10 hit rate holds. Report hit rate and MRR. Use coverage and judged pairs to decide whether to set `CrowdActiveStrongMatchThreshold`. Inactive scoring keeps the existing denominator and threshold.

## Rejected options

- TMDb's similar list supplies metadata similarity. The ticket selects the recommendations list for the catalogue signal.
- Local peer-history collaborative filtering requires behavior data and remains outside ADR-0011's boundary.
- Normalizing over only present terms would conflict with the shared scoring model in ADR 0046. Keep Crowd's weight in the active denominator for unlinked pairs.

Accepted by Patron in the [DEV-539 grill](../../specs/DEV-539/CONCLUSIONS.md#q1) and updated per the [plan challenge ruling](../../specs/DEV-539/CONCLUSIONS.md#plan-challenge-adjudication-ledger-h1h2m1m2m4l1-compass-f1-sentry-m2).
