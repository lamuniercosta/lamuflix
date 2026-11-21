# Rank For you by nearest seeds

**Status:** Proposed

A taste-profile centroid can blend distinct interests into a score that cannot explain which movie led to a recommendation. DEV-543 ranks For you against individual movies using the shared DEV-518 pair scorer. The seed that explains a result and its similarity evidence come from the same comparison.

## For you ranking

Load the library features once through the DEV-544 whole-library feature source. Use that snapshot for candidates and seeds, with the same feature mapping on both sides. Join retained playback timestamps by movie ID. Follow the DEV-510 cache shape and keep per-movie embeddings out of cached profile data.

Resolve one signal per movie before applying recency. Disliked movies are negative seeds only. The positive precedence and default weights are liked (1.0), watched without review (0.6), then watchlist (0.4). Multiply positive weights by the existing exponential playback decay where playback exists, with a configurable floor of 0.25. Positives without playback keep their full weight.

For each unwatched, undisliked candidate, exclude self-pairs and omit non-finite pair results. Keep the DEV-518 scorer's missing-feature semantics. Sum the three highest usable positive weighted similarities with default rank multipliers of 1, 0.5 and 0.25, using fewer terms when fewer are available. Subtract the highest usable disliked-seed similarity multiplied by the configured disliked weight, which defaults to 1.0. The penalty is zero when no usable disliked pair exists.

Admit a candidate when at least one positive pair meets the shared DEV-544 policy on raw similarity and breakdown. The defaults are similarity of at least 0.225 or a Collection, Director or Themes term with value 1. Publish qualifying candidates in descending signed-aggregate order. Break ties by the existing secondary keys where applicable, then by ascending movie ID. Apply the existing request limit directly to that order, with fewer results when fewer candidates qualify. Remove the MMR and genre-weight selection call from the For you handler. The shared selection helpers and their options remain available to their existing callers. DEV-541 owns caps, paging and exclusions. Watchlist-only seeds use the same ranking. Return an empty result when no candidate qualifies.

Choose the explanation seed from all strong positive pairs by highest recency-adjusted weighted similarity, breaking ties by ascending seed movie ID. That pair can fall outside the top three scoring contributions. Emit the signal-specific seed line first in the existing reasons list, followed by DEV-518 evidence from that pair. The [brief's reason table](../../specs/DEV-543/brief.md#scoring-and-reasons) defines the three templates.

## Related ranking and consequences

Related scores candidates using DEV-518 similarity to the selected movie and the existing watched penalty. A dislike excludes its own movie if it was eligible. Remove the taste-profile contribution and profile reasons so reviewing an unrelated movie leaves existing result scores stable. DEV-544 owns the Related candidate pool and its pool-level application of the shared strong-match policy. The Related handler uses the pool as supplied by DEV-544 and retains its existing selection. The disliked-seed aggregate penalty is specific to For you.

Both surfaces use the same DEV-544 options to define a strong pair. Before tuning the shared policy, use DEV-545 to evaluate recommendation quality on both For you and Related. If the results conflict, take the evidence to Patron before proposing any per-surface change. Pairwise scoring increases work with candidate and seed counts, so DEV-545 captures the before and after quality and warm runtime evidence. Legacy profile members require recorded caller dispositions before removal.

On delivery, this decision supersedes the signed taste-profile formula in [ADR 0034](0034-explicit-movie-reviews.md). The movie review decision continues to govern. Include the supersession note, glossary update and reason documentation with this ADR in the delivery PR. See the [DEV-543 brief and Q1–Q8 rulings](../../specs/DEV-543/brief.md#grill-answers) and the [task sequence](../../specs/DEV-543/tasks.md#phase-6-user-story-5---decision-record-and-glossary-priority-p2).
