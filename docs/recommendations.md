# Recommendation similarity

Related ranking, For You MMR redundancy, and collection block ranking share one ordered scorer. `SimilarityScorer.Score` returns a `SimilarityResult` carrying the criterion entries in table order: six when Crowd is omitted, seven when Crowd is active. Every caller consumes that same function and the same weight/reason vocabulary. The durable architecture decision is [ADR 0046](adr/0046-shared-recommendation-similarity.md), amended by [ADR 0048](adr/0048-tmdb-crowd-similarity.md) for Crowd.

## Criteria

| Order | Criterion | Default weight | Value | Reason |
| --- | --- | --- | --- | --- |
| 1 | Collection | 3 | 1 for matching TMDb collection IDs, otherwise 0 | Same collection: {name} |
| 2 | Director | 3 | 1 for at least one shared director, otherwise 0 | Same director: {names} |
| 3 | Themes | 3 | Rarity-weighted keyword cosine divided by ThemesCeiling, capped at 1 | Shared themes: {top 3 by rarity} |
| 3a | Crowd | 2 | 1 when a resolved Crowd link joins the two distinct library movies, otherwise 0; present only while Crowd is active and TMDb is configured | For you: Often liked together with {seed title}; Related: Often liked together |
| 4 | Plot | 2 | Plot-embedding cosine rescaled between the configured floor and ceiling, clamped to [0, 1] | Similar story |
| 5 | Cast | 2 | min(1, shared actors / 2) | Shares cast: {names} |
| 6 | Genre | 2 | Rarity-weighted genre overlap | Same genres: {names} |

The six pre-Crowd default weights sum to 15. Crowd adds 2 more when it is active, making the active default 17.

## Score and contributions

- Contribution = weight × value ÷ the sum of the configured weights in the current table. The denominator is 15 at defaults while Crowd is inactive and 17 at defaults while Crowd is active, in both cases including when a criterion has no data.
- Score is the sum of those contributions. A missing input yields value 0 and keeps its place in the denominator.
- At defaults, genre-only similarity is at most 2/15 while Crowd is inactive and 2/17 while it is active.
- Overriding weights changes the denominator to the new sum; contributions still reconcile to Score.

## Crowd

- Crowd sits immediately after Themes whenever it participates, and both consumers share the same entry.
- Its value is binary: 1 when a resolved Crowd link joins the pair, 0 otherwise. Either stored recommendation direction yields the same single link, so reciprocity adds nothing.
- Crowd is omitted entirely, with the six-weight denominator restored, when `Recommendations:CrowdEnabled` is off or TMDb is unconfigured (`Tmdb:Enabled` false or a blank `Tmdb:ApiKey`).
- Its weight is `Recommendations:Weights:Crowd`, 2 by default. Startup validation requires the effective weight sum to be finite and positive in both the Crowd-inactive and Crowd-active modes.
- While Crowd is active and `Recommendations:CrowdActiveStrongMatchThreshold` is set, that value replaces `MinimumSimilarity` as the strong-match bar; otherwise the existing bar applies.

## Themes

`N` is the count of movies in the library population and `n_k` is the distinct movie count holding keyword `k` in that population.

- Keep keywords with `n_k` of at least `Recommendations:MinimumKeywordMovies` (default 2).
- Exclude stoplisted names case-insensitively. The defaults are `aftercreditsstinger`, `duringcreditsstinger`, `sequel`, and `3d animation`.
- Each retained keyword weighs `ln(N / n_k)`. The cosine numerator is the sum of squared shared-keyword weights; the denominator is the product of the two vector norms.
- An empty or zero-norm vector yields raw cosine zero and Themes value zero.
- Themes value is `min(1, raw cosine / ThemesCeiling)`, with `Recommendations:ThemesCeiling` defaulting to 0.35.
- Evidence lists up to three shared counted keyword names in descending rarity weight.

## Plot

Plot value is `clamp((cosine − floor) / (ceiling − floor), 0, 1)`, with `Recommendations:PlotFloor` defaulting to 0.16 and `Recommendations:PlotCeiling` to 0.45. Plot carries its value without matched names. Collection, Director, Cast, and Genre carry their matched names.

## Reasons

Reasons are generated in table order:

- Collection, Director, and Cast require a positive value.
- Crowd requires a positive value and emits `Often liked together with {seed title}` for For you (the winning seed title is quoted as literal data) or `Often liked together` for Related.
- Themes, Plot, and Genre require a positive value at or above their display threshold: `Recommendations:ThemesDisplayThreshold` (default 0.3), `Recommendations:PlotDisplayThreshold` (default 0.5), and `Recommendations:GenreDisplayThreshold` (default 0.5).
- A positive value below threshold keeps its evidence and its score contribution but emits no reason.
- A non-match emits no reason even at a zero threshold.

## Keyword-count snapshot

One `IMovieCatalog` read returns `N` and the distinct `n_k` values from one library population. A singleton cache backed by a scoped calculator, mirroring the genre weights cache, serves those counts and invalidates after successful commits that change the counted population, including keyword-membership mutations. Each recommendation operation resolves one immutable snapshot and passes it into every scored pair. Cache lifetime follows invalidation, not a timer.

## Retired signals

Release year, runtime, IMDb rating, writers, and “Identical feature profile” no longer participate in similarity scoring or reasons. The separate Similar story threshold is gone; the Plot reason comes from the shared scorer.

## Pinning

`tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityCriteriaTableTests.cs` pins the six-row table: the order, the six default weights, the value rules, the reason texts, the denominator of 15, and the contribution sum equalling Score. `tests/LamuFlix.UnitTests/Features/Recommendations/CrowdScoringTests.cs` pins the active seven-row table: Crowd after Themes, the default weight of 2, the binary value, the denominator of 17 for linked and unlinked pairs, the reason copy and the Crowd-active bar. Change a table and its tests together.
