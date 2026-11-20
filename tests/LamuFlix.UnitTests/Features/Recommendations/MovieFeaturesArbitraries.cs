using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed record MovieFeaturesPair(MovieFeatures First, MovieFeatures Second);

public sealed record SelfSimilarityProfile(MovieFeatures Value);

public static class MovieFeaturesArbitraries
{
    private static readonly string[] GenreTokens = ["Drama", "Comedy", "Noir", "Sci-Fi", "Doc", "Thriller"];
    private static readonly string[] PersonTokens = ["Nolan", "Wong", "Campion", "Lee", "Kurosawa", "Miyazaki"];
    private static readonly string[] KeywordTokens = ["alpha", "beta", "gamma", "delta", "anchor-theme"];
    private static readonly string[] BlankTokens = ["", " ", "\t", "  "];
    private static readonly string[] CollectionNames = ["Saga", "Trilogy", "Quartet", ""];

    private static readonly float[][] Embeddings =
    [
        [1f, 0f],
        [0f, 1f],
        [0.6f, 0.8f],
        [-1f, 0f],
        [0.3f, 0.4f, 0.5f],
        [-0.2f, 0.9f],
    ];

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
            FullProfile(),
            BlankHeavyProfile());

    private static Gen<MovieFeatures> SingleFieldProfile() =>
        Gen.Choose(0, 5).SelectMany(field => field switch
        {
            0 => Build(Set(GenreToken(), 1, 3), EmptySet(), EmptySet(), EmptySet(), NullCollection(), NullCollectionName(), EmptyEmbedding()),
            1 => Build(EmptySet(), Set(NameToken(), 1, 3), EmptySet(), EmptySet(), NullCollection(), NullCollectionName(), EmptyEmbedding()),
            2 => Build(EmptySet(), EmptySet(), Set(NameToken(), 1, 3), EmptySet(), NullCollection(), NullCollectionName(), EmptyEmbedding()),
            3 => Build(EmptySet(), EmptySet(), EmptySet(), Set(KeywordToken(), 1, 3), NullCollection(), NullCollectionName(), EmptyEmbedding()),
            4 => Build(EmptySet(), EmptySet(), EmptySet(), EmptySet(), Collection(), CollectionName(), EmptyEmbedding()),
            _ => Build(EmptySet(), EmptySet(), EmptySet(), EmptySet(), NullCollection(), NullCollectionName(), Embedding()),
        });

    private static Gen<MovieFeatures> FullProfile() =>
        Build(
            Gen.OneOf(EmptySet(), Set(GenreToken(), 1, 4), MixedSet(GenreToken())),
            Gen.OneOf(EmptySet(), Set(NameToken(), 1, 4), MixedSet(NameToken())),
            Gen.OneOf(EmptySet(), Set(NameToken(), 0, 4), MixedSet(NameToken())),
            Gen.OneOf(EmptySet(), Set(KeywordToken(), 0, 4), MixedSet(KeywordToken())),
            Gen.OneOf(NullCollection(), Collection()),
            Gen.OneOf(NullCollectionName(), CollectionName()),
            Gen.OneOf(EmptyEmbedding(), Embedding()));

    private static Gen<MovieFeatures> BlankHeavyProfile() =>
        Build(
            Set(BlankToken(), 1, 3),
            Set(BlankToken(), 0, 2),
            Set(BlankToken(), 0, 2),
            Set(BlankToken(), 0, 2),
            NullCollection(),
            CollectionName(),
            EmptyEmbedding());

    private static Gen<MovieFeatures> Build(
        Gen<string[]> genres,
        Gen<string[]> directors,
        Gen<string[]> cast,
        Gen<string[]> keywordNames,
        Gen<int?> collectionId,
        Gen<string?> collectionName,
        Gen<float[]> plotEmbedding) =>
        from genreValues in genres
        from directorValues in directors
        from castValues in cast
        from keywordValues in keywordNames
        from collection in collectionId
        from name in collectionName
        from embedding in plotEmbedding
        select new MovieFeatures(
            genreValues,
            directorValues,
            null,
            castValues,
            collectionId: collection,
            keywordNames: keywordValues,
            collectionName: name,
            plotEmbedding: embedding);

    private static Gen<MovieFeaturesPair> Pair() =>
        from first in GeneralProfile()
        from branch in Gen.Choose(0, 4)
        from second in SecondProfile(first, branch)
        select new MovieFeaturesPair(first, second);

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
        from cast in Recased(source.Cast)
        from keywordNames in Recased(source.KeywordNames)
        select new MovieFeatures(
            genres,
            directors,
            null,
            cast,
            collectionId: source.CollectionId,
            keywordNames: keywordNames,
            collectionName: RecaseOrNull(source.CollectionName),
            plotEmbedding: source.PlotEmbedding.IsDefault ? null : [.. source.PlotEmbedding]);

    private static Gen<MovieFeatures> PartialMissing(MovieFeatures source) =>
        from mask in Gen.Choose(1, 255)
        select new MovieFeatures(
            Keep(source.Genres, (mask & 1) != 0),
            Keep(source.Directors, (mask & 2) != 0),
            null,
            Keep(source.Cast, (mask & 4) != 0),
            collectionId: (mask & 8) != 0 ? source.CollectionId : null,
            keywordNames: Keep(source.KeywordNames, (mask & 16) != 0),
            collectionName: (mask & 32) != 0 ? source.CollectionName : null,
            plotEmbedding: (mask & 64) != 0 ? [.. source.PlotEmbedding] : null);

    private static Gen<MovieFeatures> Overlapping(MovieFeatures source) =>
        from genres in SharedAndAdded(source.Genres)
        from directors in SharedAndAdded(source.Directors)
        from cast in SharedAndAdded(source.Cast)
        from keywordNames in SharedAndAddedKeywords(source.KeywordNames)
        from collection in Gen.OneOf(Gen.Constant(source.CollectionId), Collection().Select(value => (int?)value))
        select new MovieFeatures(
            genres,
            directors,
            null,
            cast,
            collectionId: collection,
            keywordNames: keywordNames,
            collectionName: source.CollectionName,
            plotEmbedding: source.PlotEmbedding.IsDefault ? null : [.. source.PlotEmbedding]);

    private static Gen<MovieFeatures> Disjoint(MovieFeatures source) =>
        from genres in Set(Prefixed(GenreToken()), 0, 4)
        from directors in Set(Prefixed(NameToken()), 0, 4)
        from cast in Set(Prefixed(NameToken()), 0, 4)
        from keywordNames in Set(Prefixed(KeywordToken()), 0, 4)
        select new MovieFeatures(
            genres,
            directors,
            null,
            cast,
            collectionId: ShiftedCollection(source.CollectionId),
            keywordNames: keywordNames,
            collectionName: source.CollectionName,
            plotEmbedding: source.PlotEmbedding.IsDefault ? null : [.. source.PlotEmbedding]);

    private static Gen<SelfSimilarityProfile> SelfSimilarityProfileGen() =>
        Build(
            Set(GenreToken(), 1, 2),
            DistinctNameSet(1, 2),
            DistinctNameSet(2, 4),
            AnchorKeywords(),
            Collection(),
            CollectionName(),
            Gen.Elements(Embeddings))
            .Select(profile => new SelfSimilarityProfile(profile));

    private static Gen<string[]> AnchorKeywords() =>
        from anchorCount in Gen.Choose(1, 2)
        from extras in Gen.Choose(0, 2)
        from names in Gen.ListOf(KeywordToken(), extras)
        select AnchorKeywordSet(anchorCount, names);

    private static string[] AnchorKeywordSet(int anchorCount, IEnumerable<string> names)
    {
        var result = new List<string> { "anchor-theme" };
        for (var index = 1; index < anchorCount; index++)
        {
            result.Add("anchor-theme");
        }

        result.AddRange(names);
        return [.. result];
    }

    private static Gen<SimilarityWeights> Weight() =>
        from family in Gen.Choose(0, 5)
        from weights in WeightFamily(family)
        select weights;

    private static Gen<SimilarityWeights> WeightFamily(int family) =>
        family switch
        {
            0 => Gen.Constant(FromVector(SinglePositiveVector(PositiveMod((family * 7) + 3, 6)))),
            1 => Gen.Constant(new SimilarityWeights()),
            2 => Gen.Constant(FromVector([1.0, 1.0, 1.0, 1.0, 1.0, 1.0])),
            3 => MixedWeights(),
            4 => Gen.Constant(FromVector(ExtremeVector(2, 5))),
            _ => Gen.Constant(FromVector(CoupleVector(0, 3))),
        };

    private static double[] SinglePositiveVector(int index)
    {
        var values = new double[6];
        values[index] = 1.0;
        return values;
    }

    private static double[] CoupleVector(int first, int second)
    {
        var values = new double[6];
        values[first] = 3.0;
        values[second] = 2.0;
        return values;
    }

    private static double[] ExtremeVector(int big, int small)
    {
        double[] values = [1e-300, 1e-300, 1e-300, 1e-300, 1e-300, 1e-300];
        values[big] = 1e300;
        values[small] = 1e-300;
        return values;
    }

    private static Gen<SimilarityWeights> MixedWeights()
    {
        var weight = Gen.Elements(0.0, 0.25, 0.5, 1.0, 2.0, 3.0, 10.0, 1e6);

        return
            from collection in weight
            from director in weight
            from themes in weight
            from plot in weight
            from cast in weight
            from genres in weight
            select EnsurePositive(FromVector([collection, director, themes, plot, cast, genres]));
    }

    private static SimilarityWeights EnsurePositive(SimilarityWeights weights) =>
        weights.Collection + weights.Director + weights.Themes + weights.Plot + weights.Cast + weights.Genres > 0.0
            ? weights
            : weights with { Director = 1.0 };

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

    private static Gen<string[]> Set(Gen<string> token, int minimum, int maximum) =>
        from count in Gen.Choose(minimum, maximum)
        from values in Gen.ListOf(token, count)
        select values.ToArray();

    private static Gen<string[]> DistinctNameSet(int minimum, int maximum) =>
        from count in Gen.Choose(minimum, maximum)
        from order in Gen.Shuffle(PersonTokens)
        from shape in Gen.Choose(0, 2)
        select order.Take(count).Select(name => Recase(name, shape)).ToArray();

    private static Gen<string[]> MixedSet(Gen<string> token) =>
        from names in Set(token, 0, 3)
        from blanks in Set(BlankToken(), 0, 2)
        select names.Concat(blanks).ToArray();

    private static Gen<int> CollectionId() => Gen.Choose(1, 5000);

    private static Gen<int?> Collection() => CollectionId().Select(value => (int?)value);

    private static Gen<int?> NullCollection() => Gen.Constant<int?>(null);

    private static Gen<string?> CollectionName() =>
        Gen.Elements(CollectionNames).Select(name => (string?)name);

    private static Gen<string?> NullCollectionName() => Gen.Constant<string?>(null);

    private static Gen<float[]> Embedding() => Gen.Elements(Embeddings.Take(5).ToArray());

    private static Gen<float[]> EmptyEmbedding() => Gen.Constant(Array.Empty<float>());

    private static Gen<string> NameToken() =>
        from token in Gen.Elements(PersonTokens)
        from shape in Gen.Choose(0, 2)
        select Recase(token, shape);

    private static Gen<string> GenreToken() =>
        from token in Gen.Elements(GenreTokens)
        from shape in Gen.Choose(0, 2)
        select Recase(token, shape);

    private static Gen<string> KeywordToken() =>
        from token in Gen.Elements(KeywordTokens)
        from shape in Gen.Choose(0, 2)
        select Recase(token, shape);

    private static Gen<string> BlankToken() => Gen.Elements(BlankTokens);

    private static Gen<string> Prefixed(Gen<string> token) => token.Select(value => "z-" + value);

    private static string Recase(string value, int shape) =>
        shape == 0 ? value : new string([.. value.Select(SwapCase)]);

    private static string? RecaseOrNull(string? value) => value is null ? null : Recase(value, 1);

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
        select order.Select(value => value is null ? value : Recase(value, shape)).ToArray();

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

    private static Gen<string[]> SharedAndAddedKeywords(ImmutableHashSet<string> source)
    {
        var values = source.ToArray();
        var minimum = values.Length == 0 ? 0 : 1;

        return
            from count in Gen.Choose(minimum, values.Length)
            from addedCount in Gen.Choose(0, 2)
            from added in Gen.ListOf(KeywordToken(), addedCount)
            select values.Take(count).Concat(added).ToArray();
    }

    private static Gen<string> BlankTokenOrName() => Gen.OneOf(NameToken(), BlankToken());

    private static Gen<string[]> EmptySet() => Gen.Constant(Array.Empty<string>());

    private static string[] Keep(ImmutableHashSet<string> values, bool keep) => keep ? [.. values] : [];

    private static int? ShiftedCollection(int? value) => value is null ? null : value + 1;

    private static SimilarityWeights FromVector(double[] values) =>
        new()
        {
            Collection = values[0],
            Director = values[1],
            Themes = values[2],
            Plot = values[3],
            Cast = values[4],
            Genres = values[5],
        };

    private static double[] Vector(SimilarityWeights weights) =>
    [
        weights.Collection,
        weights.Director,
        weights.Themes,
        weights.Plot,
        weights.Cast,
        weights.Genres,
    ];

    private static IEnumerable<MovieFeatures> ShrinkProfile(MovieFeatures movie) =>
        ShrinkSets(movie).Concat(ShrinkSignals(movie)).Where(candidate => !candidate.Equals(movie));

    private static IEnumerable<MovieFeatures> ShrinkSets(MovieFeatures movie)
    {
        foreach (var value in movie.Genres)
        {
            yield return WithSets(movie, movie.Genres.Remove(value), movie.Directors, movie.Cast, movie.KeywordNames);
        }

        foreach (var value in movie.Directors)
        {
            yield return WithSets(movie, movie.Genres, movie.Directors.Remove(value), movie.Cast, movie.KeywordNames);
        }

        foreach (var value in movie.Cast)
        {
            yield return WithSets(movie, movie.Genres, movie.Directors, movie.Cast.Remove(value), movie.KeywordNames);
        }

        foreach (var value in movie.KeywordNames)
        {
            yield return WithSets(movie, movie.Genres, movie.Directors, movie.Cast, movie.KeywordNames.Remove(value));
        }
    }

    private static IEnumerable<MovieFeatures> ShrinkSignals(MovieFeatures movie)
    {
        if (movie.CollectionId is not null)
        {
            yield return WithSignals(movie, null, movie.CollectionName, movie.PlotEmbedding);
        }

        if (movie.CollectionName is not null)
        {
            yield return WithSignals(movie, movie.CollectionId, null, movie.PlotEmbedding);
        }

        if (!movie.PlotEmbedding.IsDefaultOrEmpty)
        {
            yield return WithSignals(movie, movie.CollectionId, movie.CollectionName, ImmutableArray<float>.Empty);
        }
    }

    private static MovieFeatures WithSets(
        MovieFeatures source,
        ImmutableHashSet<string> genres,
        ImmutableHashSet<string> directors,
        ImmutableHashSet<string> cast,
        ImmutableHashSet<string> keywordNames) =>
        new(
            genres,
            directors,
            null,
            cast,
            collectionId: source.CollectionId,
            keywordNames: keywordNames,
            collectionName: source.CollectionName,
            plotEmbedding: source.PlotEmbedding.IsDefault ? null : [.. source.PlotEmbedding]);

    private static MovieFeatures WithSignals(
        MovieFeatures source,
        int? collectionId,
        string? collectionName,
        ImmutableArray<float> plotEmbedding) =>
        new(
            source.Genres,
            source.Directors,
            null,
            source.Cast,
            collectionId: collectionId,
            keywordNames: source.KeywordNames,
            collectionName: collectionName,
            plotEmbedding: plotEmbedding.IsDefault ? null : [.. plotEmbedding]);

    private static IEnumerable<MovieFeaturesPair> ShrinkPair(MovieFeaturesPair pair) =>
        ShrinkProfile(pair.First)
            .Select(value => pair with { First = value })
            .Concat(ShrinkProfile(pair.Second).Select(value => pair with { Second = value }));

    private static IEnumerable<SelfSimilarityProfile> ShrinkSelfSimilarityProfile(SelfSimilarityProfile profile) =>
        ShrinkProfile(profile.Value)
            .Where(IsSelfSimilarityEligible)
            .Select(value => new SelfSimilarityProfile(value));

    private static IEnumerable<SimilarityWeights> ShrinkWeights(SimilarityWeights weights)
    {
        var values = Vector(weights);

        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] <= 0.0)
            {
                continue;
            }

            double[] candidate = [.. values];
            candidate[index] = 0.0;
            if (candidate.Sum() > 0.0)
            {
                yield return FromVector(candidate);
            }
        }
    }

    private static IEnumerable<GenreWeights> ShrinkGenreTable(GenreWeights table)
    {
        if (GenreTokens.Any(genre => Math.Abs(table.GetWeight(genre) - 1.0) > 1e-12))
        {
            yield return EmptyTable();
        }
    }

    private static bool IsSelfSimilarityEligible(MovieFeatures movie) =>
        movie.CollectionId is not null
        && movie.Genres.Any(value => !string.IsNullOrWhiteSpace(value))
        && movie.Directors.Any(value => !string.IsNullOrWhiteSpace(value))
        && movie.Cast.Count(value => !string.IsNullOrWhiteSpace(value)) >= 2
        && movie.KeywordNames.Any(value => !string.IsNullOrWhiteSpace(value))
        && !movie.PlotEmbedding.IsDefaultOrEmpty
        && movie.PlotEmbedding.Any(value => value != 0f);

    private static int PositiveMod(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }
}
