# DEV-336 assumptions

Patron taste rulings, accepted at grill Q2; authority and full exchange in CONCLUSIONS.md.

- [assumed] Name set fields Genres, Directors, Writers, Cast; Directors preserves the whole persisted list and Cast maps Actors.
- [assumed] Use ImmutableHashSet<string> with StringComparer.OrdinalIgnoreCase for all four sets, including empty defaults.
- [assumed] Use int? MetaScore and string? FullPlot; FullPlot maps Synopsis.
- [assumed] Preserve persisted text verbatim; no trimming, case rewriting, or blank filtering; no Movie field fallback.
- [assumed] Name the sole mapping entry point MovieFeaturesMapper.From(Movie movie) in LamuFlix.Core.Features.Recommendations.
- [assumed] One optional-default constructor with genres, directors, writers, cast, releaseYear, runtime, imdbRating, metaScore, fullPlot parameters; get-only properties, no init setters or second factory.
- [assumed] Test file paths are Features/Recommendations/MovieFeaturesTests.cs, MovieFeaturesMapperTests.cs, and MovieFeaturesPropertyTests.cs under tests/LamuFlix.UnitTests/.
- [assumed] Patron Q6: Preserve null elements at runtime in each set, collapse duplicate nulls, use null-safe equality and a zero hash contribution for null; retain Q4 public types.
- [assumed] Patron Q7: MovieFeaturesMapper is a public static class with sole public static From(Movie) mapping member and no instance construction or state.
