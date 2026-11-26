# Data model: DEV-571 candidate IMDb evidence and shared OMDb quota

**Authority**: `brief.md` Q1–Q3 + Q3/Q4 update + Phase 2 plan-challenge decisions; `CONCLUSIONS.md` Q1–Q3 / Q3–Q4 update / Phase 2 ruling; `spec.md` Key Entities. Exact CLR names for the **new** candidate-evidence types are Build choices inside this envelope. Landed quota and candidate-source CLR/table names are fixed by recon.

## Candidate IMDb evidence (persisted, ticket-owned — only new table/migration)

Durable cache of discovery-candidate IMDb/collection evidence. Not the enrichment movie `ImdbRating` copy (recon: `EfMovieRepository` enrichment path is distinct). **Established as a provider evidence model before migration**; cache store read/write and expiry finish before discovery selection wiring.

| Field | Rules |
|---|---|
| TMDb ID | Required key. One evidence row per candidate TMDb ID. |
| IMDb ID | Retained when known; confirmed absence is an explicit outcome, not a fabricated ID. |
| Collection ID | Positive TMDb collection ID when present; absent/unknown means the title is individually eligible for collection dedupe. When membership becomes known during evidence resolution, selection rechecks represented-collection status before admission. |
| Parsed rating | Stored numeric rating when known so floors are recomputed from evidence, not only a pass flag. **Any known** rating below 6.5 is decisive even when votes are unknown/absent. |
| Parsed votes | Stored vote count when known; **any known** votes below 1,000 are decisive even when rating is unknown/absent. A known floor failure with unknown votes is still complete known-below-floor evidence. Unknown votes with a valid at/above-floor rating remain unresolved and MUST NOT be written as complete evidence. |
| Outcome | Distinguishes complete evidence (pass or known floor failure, including known floor failure with unknown/absent counterpart), confirmed absence (explicit provider-confirmed per recon lines 5–8), and unresolved only after those outcomes are evaluated. |
| UTC expiry | From injectable time seam. Exact Patron: "Fresh successful evidence, whether it passes or is known below floor, expires after 30 days. Confirmed absence of an IMDb rating/ID expires after 7 days, because an external ID or rating can appear later." |
| Uniqueness | Unique on TMDb ID. |
| Recompute | Floor eligibility is always recomputed from stored rating/votes against inclusive 6.5 / 1,000 floors with decisive known-failure precedence. |
| Durable reuse | Must be proven across distinct scopes/context instances against real relational persistence for both expiry windows. |

### Evidence lifecycle

Evaluate in order: known floor failures, then confirmed absence, then unresolved. A known floor failure with unknown votes is stored for 30 days — the unresolved/no-row rule applies only after known floor failures and confirmed absence are evaluated.

| Event | Persistence |
|---|---|
| Fresh complete pass or known floor failure (including known floor failure with unknown/absent counterpart) | Upsert evidence; 30-day expiry. Exact Patron: "Fresh successful evidence, whether it passes or is known below floor, expires after 30 days." |
| Confirmed absent IMDb rating/ID | Upsert confirmed-absence outcome; 7-day expiry. Exact Patron: "Confirmed absence of an IMDb rating/ID expires after 7 days, because an external ID or rating can appear later." |
| Unresolved only after known floor failures and confirmed absence are evaluated — transient-failure, deferred, malformed, and unknown-votes (valid at/above-floor rating with unknown votes) | No durable row. Exact Patron: "Unresolved, transient-failure, deferred, and unknown-votes evidence gets no durable entry and is retried later under Q1." |
| Expired evidence | Treated as missing for cache purposes; may refresh within live budget. |
| Concurrent identical misses (in-process) | Coalesce on an **Infrastructure instance shared across participating loads** (avoid a per-request map or static mutable state). One TMDb-ID leader covers the whole provider/cache-miss operation; waiters share the outcome and never send or charge under that flight; no promotion within the flight; waiter cancellation is independent. Transient failure or leader cancellation leaves waiters deferred and permits independent later loads to retry after cleanup. Cross-process quota still uses atomic reservation independent of the per-load live-attempt cap. |

## Shared OMDb daily quota reservation (persisted, **reused** — not a new ticket table)

Application-wide request accounting for every OMDb consumer, request, and user via the landed seam: `MetadataQuotaHandler` → `IOmdbRequestGate` → `EfMetadataRequestGate` → `omdb_daily_usage`, configured by `OmdbQuotaOptions` (recon lines 23–28). **No new quota counter table or port.** Under Gate 1 / T000, remove the hard-coded default allowance of 1,000 in the same implementation step as disabled normalization. Preserve landed enrichment quota settlement.

| Field | Rules |
|---|---|
| UTC calendar date | Reservation key from injectable `TimeProvider` seam (UTC date only). Each actual attempt (including retries) reserves for its **current** UTC date. Crossing midnight does not replenish the current load's per-load ceiling or restart a cache-only load. Rollover needs no background job. |
| Reserved / consumed count | Atomically incremented/reserved before every outbound OMDb attempt on the landed gate path, after IMDb request-ID validation on the discovery path. |
| Configured daily allowance | Under Gate 1 / T000, bind the raw `Omdb:Quota:DailyLimit` key in existing options binding before typed conversion to a positive enabled allowance or explicit disabled/no-allowance state (missing, zero, negative, malformed/overflow → disabled; positive → enabled). Shared pool — not per-feature; **no 1000 default**, fallback, or bypass. Committed appsettings/AppHost/Compose edits outside ruling; Q4 provisioning for environments. |
| Charge rules | Every outbound attempt charges, including failure, timeout, and each retry. Cache hits and coalesced waiters charge nothing and make no remote call. Malformed input ID before send charges nothing. Malformed ID echoed by OMDb after a send remains charged as unresolved. Enforced at the individual outbound-send boundary, independent of the per-load live-attempt cap. |
| Fail closed / disabled | Under Gate 1 / T000, implement runtime host-up disabled behaviour for absent, zero, or invalid configured allowance (including malformed nonnumeric input): host stays up; shared gate denies outbound OMDb for discovery and enrichment; zero live OMDb sends; cached and confirmed-absence evaluation continues; enrichment settlement retained; other options remain startup-validated. Current challenge-base HEAD baseline (recon 26–28): missing key defaults to 1,000; zero/negative fail startup; nonnumeric-specific handling/test not found. |
| Concurrency | Atomic reservation must be correct across processes and parallel separate loads/consumers; prove parallel reservations and two-consumer shared accounting (charged attempts attributable to each consumer; no new metric stack or pool split) in real relational tests; prove no-default, configured positive, and disabled paths under Gate 1 / T000. |

## Collection representative selection (derived, not persisted)

Computed during the discovery load over DEV-561-ordered candidates by Infrastructure `EfRecommendationCandidateSource` calling Core pure rules and returning `OutsideLibraryRecommendationSnapshot`:

1. Fix the **per-load ceiling** at load start from open slots (≤20) and remaining shared capacity. Resolve candidates **serially** in DEV-561 order with no speculative parallel candidates.
2. Start from unchanged DEV-561 ranked candidate sequence (liked weight 2, watched weight 1, score descending, TMDb ID ascending, seed/exclusion rules intact). **Retain candidates beyond any former top-20 cut-off** so backfill can admit a lower-ranked survivor into an open slot.
3. Walk in that order. Skip a candidate whose known positive collection ID is already represented by an admitted title (no lookup; zero outbound to both providers). Fresh cached collection membership is used until evidence expiry unless fresher membership is already available without a remote lookup.
4. For a candidate that can still occupy an open slot among the remaining admitted capacity (≤20), resolve IMDb evidence (cache first; else live within the fixed per-load ceiling and shared bounds at the individual-send boundary). Before each TMDb `external_ids` call require room for a following OMDb send, no preceding denial, and room within the same numeric per-load ceiling; count every `external_ids` call including failures and empty IDs; no additional configured budget or TTL. Validate the IMDb request ID before constructing/sending OMDb or reserving. A send-boundary denial defers that candidate without an outbound send, does not fail the load, and switches the rest to cache/confirmed-absence only.
5. If collection membership becomes known only during evidence resolution, **recheck** represented-collection membership **before admission**.
6. Apply FR eligibility: reject any known floor failure (decisive even when counterpart unknown/absent); admit confirmed absence and complete passes; defer unresolved (including valid at/above-floor rating with unknown votes).
7. On admit, if the title has a positive collection ID, mark that collection represented.
8. Stop at 20 admitted representatives or when candidates are exhausted. Shorter rows are allowed when evidence is deferred, denied, or budgets are exhausted. After either bound is exhausted or a send-boundary denial has switched the load, continue only with cached evidence and confirmed absence (no fresh resolution).

## Outside-library discovery card (unchanged contract)

Existing DEV-561 card projection (`tmdbId`, `title`, nullable `releaseYear`, nullable `posterPath`, `reason`). This ticket filters membership only; it does not add or remove public fields or statuses. Display prerequisites support filling from fresh cached complete evidence and confirmed-absent evidence even when live OMDb work is disabled.

## Relationship to enrichment persistence

Movie enrichment may still copy OMDb rating onto library movie records. That path is not the candidate evidence cache and must continue to succeed through the guarded OMDb adapter after Q4 enablement, preserving the landed enrichment quota settlement (`brief.md` Q4 / Q3–Q4 update; recon lines 5–8, 23–28).
