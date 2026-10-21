using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed record MovieFeaturesPair(string Branch, MovieFeatures First, MovieFeatures Second);

public sealed record SelfSimilarityProfile(string Branch, MovieFeatures Value);

public static class MovieFeaturesArbitraries
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] GenreTokens = ["Drama", "Comedy", "Noir", "Sci-Fi", "Doc", "Thriller"];
    private static readonly string[] PersonTokens = ["Nolan", "Wong", "Campion", "Lee", "Kurosawa", "Miyazaki"];
    private static readonly string[] BlankTokens = [null!, "", " ", "\t", "  "];

    public static Arbitrary<MovieFeatures> Profiles() => Arb.From(GeneralProfile(), ShrinkProfile);

    public static Arbitrary<MovieFeaturesPair> Pairs() => Arb.From(Pair(), ShrinkPair);

    public static Arbitrary<SimilarityWeights> Weights() => Arb.From(Weight(), ShrinkWeights);

    public static Arbitrary<GenreWeights> GenreTables() => Arb.From(GenreTable(), ShrinkGenreTable);

    public static Arbitrary<SelfSimilarityProfile> SelfSimilarityProfiles() =>
        Arb.From(SelfSimilarityProfileGen(), ShrinkSelfSimilarityProfile);

    private static Gen<MovieFeatures> GeneralProfile() =>
        Gen.OneOf(
            Gen.Constant(new MovieFeatures()),
            SingleFieldProfile(),
            Profile(TokenSet(GenreToken()), NameSet(), NameSet(), NameSet()),
            Profile(Set(BlankToken(), 1, 3), NameSet(), NameSet(), NameSet()));

    private static Gen<MovieFeatures> SingleFieldProfile() =>
        Gen.Choose(0, 6).SelectMany(field => field switch
        {
            0 => Build(Set(GenreToken(), 1, 3), EmptySet(), EmptySet(), EmptySet(), NullYears(), NullRuntimes(), NullRatings()),
            1 => Build(EmptySet(), Set(NameToken(), 1, 3), EmptySet(), EmptySet(), NullYears(), NullRuntimes(), NullRatings()),
            2 => Build(EmptySet(), EmptySet(), Set(NameToken(), 1, 3), EmptySet(), NullYears(), NullRuntimes(), NullRatings()),
            3 => Build(EmptySet(), EmptySet(), EmptySet(), Set(NameToken(), 1, 3), NullYears(), NullRuntimes(), NullRatings()),
            4 => Build(EmptySet(), EmptySet(), EmptySet(), EmptySet(), Nullable(Year()), NullRuntimes(), NullRatings()),
            5 => Build(EmptySet(), EmptySet(), EmptySet(), EmptySet(), NullYears(), Nullable(RuntimeGen()), NullRatings()),
            _ => Build(EmptySet(), EmptySet(), EmptySet(), EmptySet(), NullYears(), NullRuntimes(), Nullable(Rating())),
        });

    private static Gen<MovieFeatures> Profile(
        Gen<string[]> genres,
        Gen<string[]> directors,
        Gen<string[]> writers,
        Gen<string[]> cast) =>
        Build(genres, directors, writers, cast, Nullable(Year()), Nullable(RuntimeGen()), Nullable(Rating()));

    private static Gen<MovieFeatures> Build(
        Gen<string[]> genres,
        Gen<string[]> directors,
        Gen<string[]> writers,
        Gen<string[]> cast,
        Gen<ReleaseYear?> releaseYear,
        Gen<Runtime?> runtime,
        Gen<ImdbRating?> imdbRating) =>
        from genreValues in genres
        from directorValues in directors
        from writerValues in writers
        from castValues in cast
        from year in releaseYear
        from minutes in runtime
        from rating in imdbRating
        from metadata in Metadata()
        select new MovieFeatures(
            genreValues,
            directorValues,
            writerValues,
            castValues,
            year,
            minutes,
            rating,
            metadata.MetaScore,
            metadata.FullPlot);

    private static Gen<MovieFeaturesPair> Pair() =>
        from first in GeneralProfile()
        from branch in Gen.Choose(0, 4)
        from second in SecondProfile(first, branch)
        select new MovieFeaturesPair(PairLabel(branch), first, second);

    private static Gen<MovieFeatures> SecondProfile(MovieFeatures first, int branch) =>
        branch switch
        {
            0 => Gen.Constant(first),
            1 => CaseVariant(first),
            2 => PartialMissing(first),
            3 => Overlapping(first),
            _ => Disjoint(first),
        };

    private static Gen<MovieFeatures> CaseVariant(MovieFeatures source) =>
        from genres in Recased(source.Genres)
        from directors in Recased(source.Directors)
        from writers in Recased(source.Writers)
        from cast in Recased(source.Cast)
        select new MovieFeatures(
            genres,
            directors,
            writers,
            cast,
            source.ReleaseYear,
            source.Runtime,
            source.ImdbRating,
            source.MetaScore,
            source.FullPlot);

    private static Gen<MovieFeatures> PartialMissing(MovieFeatures source) =>
        from mask in Gen.Choose(1, 127)
        select new MovieFeatures(
            Keep(source.Genres, (mask & 1) != 0),
            Keep(source.Directors, (mask & 2) != 0),
            Keep(source.Writers, (mask & 4) != 0),
            Keep(source.Cast, (mask & 8) != 0),
            (mask & 16) != 0 ? source.ReleaseYear : null,
            (mask & 32) != 0 ? source.Runtime : null,
            (mask & 64) != 0 ? source.ImdbRating : null,
            source.MetaScore,
            source.FullPlot);

    private static Gen<MovieFeatures> Overlapping(MovieFeatures source) =>
        from genres in SharedAndAdded(source.Genres)
        from directors in SharedAndAdded(source.Directors)
        from writers in SharedAndAdded(source.Writers)
        from cast in SharedAndAdded(source.Cast)
        select new MovieFeatures(
            genres,
            directors,
            writers,
            cast,
            source.ReleaseYear,
            source.Runtime,
            source.ImdbRating,
            source.MetaScore,
            source.FullPlot);

    private static Gen<MovieFeatures> Disjoint(MovieFeatures source) =>
        from genres in Set(Prefixed(GenreToken()), 0, 4)
        from directors in Set(Prefixed(NameToken()), 0, 4)
        from writers in Set(Prefixed(NameToken()), 0, 4)
        from cast in Set(Prefixed(NameToken()), 0, 4)
        select new MovieFeatures(
            genres,
            directors,
            writers,
            cast,
            Shifted(source.ReleaseYear),
            Shifted(source.Runtime),
            Shifted(source.ImdbRating),
            source.MetaScore,
            source.FullPlot);

    private static Gen<SelfSimilarityProfile> SelfSimilarityProfileGen() =>
        from anchor in Gen.Choose(0, 5)
        from profile in Profile(SelfSimilarityGenres(), NameSet(), NameSet(), NameSet())
        select new SelfSimilarityProfile(AnchorLabel(anchor), ApplyAnchor(profile, anchor));

    private static Gen<string[]> SelfSimilarityGenres() =>
        Gen.OneOf(
            Gen.Constant(Array.Empty<string>()),
            Set(GenreToken(), 1, 4),
            MixedGenreSet());

    private static Gen<string[]> MixedGenreSet() =>
        from names in Set(GenreToken(), 1, 3)
        from blanks in Set(BlankToken(), 1, 2)
        select names.Concat(blanks).ToArray();

    private static MovieFeatures ApplyAnchor(MovieFeatures profile, int anchor) =>
        new(
            profile.Genres,
            WithToken(profile.Directors, anchor == 0 ? "Anchor" : null),
            WithToken(profile.Writers, anchor == 1 ? "Anchor" : null),
            WithToken(profile.Cast, anchor == 2 ? "Anchor" : null),
            anchor == 3 ? profile.ReleaseYear ?? new ReleaseYear(2000, Now) : profile.ReleaseYear,
            anchor == 4 ? profile.Runtime ?? new Runtime(90) : profile.Runtime,
            anchor == 5 ? profile.ImdbRating ?? new ImdbRating(7.0m) : profile.ImdbRating,
            profile.MetaScore,
            profile.FullPlot);

    private static Gen<SimilarityWeights> Weight() =>
        from family in Gen.Choose(0, 5)
        from weights in WeightFamily(family)
        select weights;

    private static Gen<SimilarityWeights> WeightFamily(int family) =>
        family switch
        {
            0 => Gen.Constant(FromVector(new double[7])),
            1 => Gen.Constant(FromVector([.. Enumerable.Repeat(double.MaxValue, 7)])),
            2 => SinglePositive(),
            3 => Gen.Constant(new SimilarityWeights()),
            4 => MixedWeights(),
            _ => ExtremeRatio(),
        };

    private static Gen<SimilarityWeights> SinglePositive() =>
        Gen.Choose(0, 6).Select(index => FromVector(SinglePositiveVector(index)));

    private static double[] SinglePositiveVector(int index)
    {
        var values = new double[7];
        values[index] = 1.0;
        return values;
    }

    private static Gen<SimilarityWeights> MixedWeights()
    {
        var weight = Gen.Elements(0.0, 0.25, 0.5, 1.0, 2.0, 3.0, 10.0, 1e6, double.MaxValue);

        return
            from director in weight
            from writers in weight
            from cast in weight
            from genres in weight
            from year in weight
            from runtime in weight
            from rating in weight
            select FromVector([director, writers, cast, genres, year, runtime, rating]);
    }

    private static Gen<SimilarityWeights> ExtremeRatio()
    {
        var tiny = Gen.Elements(double.Epsilon, 1e-300, 1e-9);

        return
            from big in Gen.Choose(0, 6)
            from offset in Gen.Choose(1, 6)
            from tinyValue in tiny
            from filler in Gen.Elements(0.0, 1e-12)
            select ExtremeVector(big, (big + offset) % 7, tinyValue, filler);
    }

    private static SimilarityWeights ExtremeVector(int big, int small, double tinyValue, double filler)
    {
        var values = Enumerable.Repeat(filler, 7).ToArray();
        values[big] = double.MaxValue;
        values[small] = tinyValue;
        return FromVector(values);
    }

    private static Gen<GenreWeights> GenreTable() =>
        Gen.OneOf(
            Gen.Constant(EmptyTable()),
            PopulatedTable(),
            SparseTable());

    private static Gen<GenreWeights> PopulatedTable() =>
        Gen.Choose(1, 400).SelectMany(total =>
        {
            var entry =
                from genre in Gen.Elements(GenreTokens)
                from frequency in Gen.Choose(0, total)
                select (Genre: genre, Frequency: (long)frequency);

            return
                from count in Gen.Choose(0, 5)
                from entries in Gen.ListOf(entry, count)
                select GenreWeights.Calculate(new GenreCounts(total, Counts(entries)));
        });

    private static Gen<GenreWeights> SparseTable()
    {
        var entry = Gen.Elements(
            ("Drama", 1L),
            ("Comedy", 1L),
            ("Noir", 0L),
            ("Doc", 1L));

        return
            from total in Gen.Elements(1, 2, 10, 100)
            from count in Gen.Choose(0, 4)
            from entries in Gen.ListOf(entry, count)
            select GenreWeights.Calculate(new GenreCounts(total, Counts(entries)));
    }

    private static GenreWeights EmptyTable() =>
        GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));

    private static Dictionary<string, long> Counts(IEnumerable<(string Genre, long Frequency)> entries) =>
        entries
            .GroupBy(entry => entry.Genre, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Frequency, StringComparer.OrdinalIgnoreCase);

    private static Gen<string[]> TokenSet(Gen<string> token) => Set(Gen.OneOf(token, BlankToken()), 0, 4);

    private static Gen<string[]> NameSet() => Set(NameToken(), 0, 4);

    private static Gen<string[]> Set(Gen<string> token, int minimum, int maximum) =>
        from count in Gen.Choose(minimum, maximum)
        from values in Gen.ListOf(token, count)
        select values.ToArray();

    private static Gen<string> NameToken() =>
        from token in Gen.Elements(PersonTokens)
        from shape in Gen.Choose(0, 2)
        select Recase(token, shape);

    private static Gen<string> GenreToken() =>
        from token in Gen.Elements(GenreTokens)
        from shape in Gen.Choose(0, 2)
        select Recase(token, shape);

    private static Gen<string> BlankToken() => Gen.Elements(BlankTokens);

    private static Gen<string> Prefixed(Gen<string> token) => token.Select(value => "z-" + value);

    private static string Recase(string value, int shape) =>
        shape == 0 ? value : new string([.. value.Select(SwapCase)]);

    private static char SwapCase(char value) =>
        value switch
        {
            >= 'a' and <= 'z' => (char)(value - 32),
            >= 'A' and <= 'Z' => (char)(value + 32),
            _ => value,
        };

    private static Gen<string[]> Recased(ImmutableHashSet<string> values) =>
        from order in Gen.Shuffle(values.ToArray())
        from shape in Gen.Choose(0, 2)
        select order.Select(value => value is null ? null! : Recase(value, shape)).ToArray();

    private static Gen<string[]> SharedAndAdded(ImmutableHashSet<string> source)
    {
        var values = source.ToArray();
        var minimum = values.Length == 0 ? 0 : 1;

        return
            from count in Gen.Choose(minimum, values.Length)
            from addedCount in Gen.Choose(0, 2)
            from added in Gen.ListOf(BlankTokenOrName(), addedCount)
            select values.Take(count).Concat(added).ToArray();
    }

    private static Gen<string> BlankTokenOrName() => Gen.OneOf(NameToken(), BlankToken());

    private static Gen<(int? MetaScore, string? FullPlot)> Metadata() =>
        from metaScore in Gen.OneOf(Gen.Constant<int?>(null), Gen.Choose(0, 100).Select(value => (int?)value))
        from fullPlot in Gen.OneOf(Gen.Constant<string?>(null), Gen.Elements("plot", "A story", " ").Select(value => (string?)value))
        select (metaScore, fullPlot);

    private static Gen<ReleaseYear> Year() =>
        Gen.Choose(1888, Now.Year + 5).Select(value => new ReleaseYear(value, Now));

    private static Gen<Runtime> RuntimeGen() => Gen.Choose(1, 600).Select(value => new Runtime(value));

    private static Gen<ImdbRating> Rating() => Gen.Choose(0, 100).Select(value => new ImdbRating(value / 10.0m));

    private static Gen<ReleaseYear?> Nullable(Gen<ReleaseYear> source) =>
        Gen.OneOf(Gen.Constant<ReleaseYear?>(null), source.Select(value => (ReleaseYear?)value));

    private static Gen<Runtime?> Nullable(Gen<Runtime> source) =>
        Gen.OneOf(Gen.Constant<Runtime?>(null), source.Select(value => (Runtime?)value));

    private static Gen<ImdbRating?> Nullable(Gen<ImdbRating> source) =>
        Gen.OneOf(Gen.Constant<ImdbRating?>(null), source.Select(value => (ImdbRating?)value));

    private static Gen<string[]> EmptySet() => Gen.Constant(Array.Empty<string>());

    private static Gen<ReleaseYear?> NullYears() => Gen.Constant<ReleaseYear?>(null);

    private static Gen<Runtime?> NullRuntimes() => Gen.Constant<Runtime?>(null);

    private static Gen<ImdbRating?> NullRatings() => Gen.Constant<ImdbRating?>(null);

    private static string[] Keep(ImmutableHashSet<string> values, bool keep) => keep ? [.. values] : [];

    private static string[] WithToken(ImmutableHashSet<string> values, string? token) =>
        token is null ? [.. values] : [.. values, token];

    private static ReleaseYear? Shifted(ReleaseYear? value) =>
        value is null ? null : new ReleaseYear(Math.Min(value.Value + 3, Now.Year + 5), Now);

    private static Runtime? Shifted(Runtime? value) =>
        value is null ? null : new Runtime(value.Minutes + 7);

    private static ImdbRating? Shifted(ImdbRating? value) =>
        value is null ? null : new ImdbRating(Math.Min(value.Value + 0.5m, 10.0m));

    private static SimilarityWeights FromVector(double[] values) =>
        new()
        {
            Director = values[0],
            Writers = values[1],
            Cast = values[2],
            Genres = values[3],
            YearProximity = values[4],
            RuntimeProximity = values[5],
            RatingBoost = values[6],
        };

    private static double[] Vector(SimilarityWeights weights) =>
    [
        weights.Director,
        weights.Writers,
        weights.Cast,
        weights.Genres,
        weights.YearProximity,
        weights.RuntimeProximity,
        weights.RatingBoost,
    ];

    private static IEnumerable<MovieFeatures> ShrinkProfile(MovieFeatures movie) =>
        ShrinkSets(movie).Concat(ShrinkScalars(movie)).Where(candidate => !candidate.Equals(movie));

    private static IEnumerable<MovieFeatures> ShrinkSets(MovieFeatures movie)
    {
        foreach (var value in movie.Genres)
        {
            yield return WithSets(movie, Without(movie.Genres, value), movie.Directors, movie.Writers, movie.Cast);
        }

        foreach (var value in movie.Directors)
        {
            yield return WithSets(movie, movie.Genres, Without(movie.Directors, value), movie.Writers, movie.Cast);
        }

        foreach (var value in movie.Writers)
        {
            yield return WithSets(movie, movie.Genres, movie.Directors, Without(movie.Writers, value), movie.Cast);
        }

        foreach (var value in movie.Cast)
        {
            yield return WithSets(movie, movie.Genres, movie.Directors, movie.Writers, Without(movie.Cast, value));
        }
    }

    private static IEnumerable<MovieFeatures> ShrinkScalars(MovieFeatures movie)
    {
        if (movie.ReleaseYear is { } year)
        {
            yield return WithScalars(movie, null, movie.Runtime, movie.ImdbRating);

            if (year.Value > 1888)
            {
                yield return WithScalars(movie, new ReleaseYear(1888, Now), movie.Runtime, movie.ImdbRating);
            }
        }

        if (movie.Runtime is { } runtime)
        {
            yield return WithScalars(movie, movie.ReleaseYear, null, movie.ImdbRating);

            if (runtime.Minutes > 1)
            {
                yield return WithScalars(movie, movie.ReleaseYear, new Runtime(1), movie.ImdbRating);
            }
        }

        if (movie.ImdbRating is { } rating)
        {
            yield return WithScalars(movie, movie.ReleaseYear, movie.Runtime, null);

            if (rating.Value > 0.0m)
            {
                yield return WithScalars(movie, movie.ReleaseYear, movie.Runtime, new ImdbRating(0.0m));
            }
        }
    }

    private static IEnumerable<MovieFeaturesPair> ShrinkPair(MovieFeaturesPair pair) =>
        ShrinkProfile(pair.First)
            .Select(value => pair with { First = value })
            .Concat(ShrinkProfile(pair.Second).Select(value => pair with { Second = value }));

    private static IEnumerable<SelfSimilarityProfile> ShrinkSelfSimilarityProfile(SelfSimilarityProfile profile) =>
        ShrinkProfile(profile.Value)
            .Where(IsSelfSimilarityEligible)
            .Select(value => profile with { Value = value });

    private static IEnumerable<SimilarityWeights> ShrinkWeights(SimilarityWeights weights)
    {
        var values = Vector(weights);

        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] > 0.0)
            {
                double[] candidate = [.. values];
                candidate[index] = 0.0;
                yield return FromVector(candidate);
            }
        }
    }

    private static IEnumerable<GenreWeights> ShrinkGenreTable(GenreWeights table)
    {
        if (GenreTokens.Any(genre => table.GetWeight(genre) != 1.0))
        {
            yield return EmptyTable();
        }
    }

    private static bool IsSelfSimilarityEligible(MovieFeatures movie) =>
        HasNonBlankScoringToken(movie) && HasEligibleGenres(movie);

    private static bool HasNonBlankScoringToken(MovieFeatures movie) =>
        movie.Directors.Any(value => !string.IsNullOrWhiteSpace(value))
        || movie.Writers.Any(value => !string.IsNullOrWhiteSpace(value))
        || movie.Cast.Any(value => !string.IsNullOrWhiteSpace(value))
        || movie.Genres.Any(value => !string.IsNullOrWhiteSpace(value))
        || movie.ReleaseYear is not null
        || movie.Runtime is not null
        || movie.ImdbRating is not null;

    private static bool HasEligibleGenres(MovieFeatures movie) =>
        movie.Genres.Count == 0 || movie.Genres.Any(value => !string.IsNullOrWhiteSpace(value));

    private static ImmutableHashSet<string> Without(ImmutableHashSet<string> values, string value) =>
        ImmutableHashSet.CreateRange(
            StringComparer.OrdinalIgnoreCase,
            values.Where(candidate => !ReferenceEquals(candidate, value)));

    private static MovieFeatures WithSets(
        MovieFeatures source,
        ImmutableHashSet<string> genres,
        ImmutableHashSet<string> directors,
        ImmutableHashSet<string> writers,
        ImmutableHashSet<string> cast) =>
        new(
            genres,
            directors,
            writers,
            cast,
            source.ReleaseYear,
            source.Runtime,
            source.ImdbRating,
            source.MetaScore,
            source.FullPlot);

    private static MovieFeatures WithScalars(
        MovieFeatures source,
        ReleaseYear? releaseYear,
        Runtime? runtime,
        ImdbRating? imdbRating) =>
        new(
            source.Genres,
            source.Directors,
            source.Writers,
            source.Cast,
            releaseYear,
            runtime,
            imdbRating,
            source.MetaScore,
            source.FullPlot);

    private static string PairLabel(int branch) =>
        branch switch
        {
            0 => "identical",
            1 => "case-variant",
            2 => "partial-missing",
            3 => "overlapping",
            _ => "disjoint",
        };

    private static string AnchorLabel(int anchor) =>
        anchor switch
        {
            0 => "director-anchor",
            1 => "writer-anchor",
            2 => "cast-anchor",
            3 => "year-anchor",
            4 => "runtime-anchor",
            _ => "rating-anchor",
        };
}
