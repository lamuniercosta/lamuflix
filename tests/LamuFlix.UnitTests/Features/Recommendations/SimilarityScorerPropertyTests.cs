using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerPropertyTests
{
    private const double SymmetryTolerance = 1e-12;
    private const double ReconciliationTolerance = 1e-12;
    private static readonly string[] KeywordVocabulary = ["alpha", "beta", "gamma", "delta", "anchor-theme"];
    private static readonly string[] CriterionOrder =
        ["Collection", "Director", "Themes", "Plot", "Cast", "Genre"];

    private static GenreWeights Table => GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 1,
                ["Comedy"] = 90,
                ["Shared"] = 50,
            }));

    private static KeywordCounts Counts => new(
        100,
        new Dictionary<string, long>
        {
            ["alpha"] = 10,
            ["beta"] = 20,
            ["gamma"] = 40,
            ["delta"] = 2,
            ["anchor-theme"] = 50,
        });

    [Property(MaxTest = 100, Replay = "338001,100003")]
    [Trait("Category", "Property")]
    public bool Score_GeneratedProfiles_IsFiniteAndWithinUnitInterval(
        string[]? genres,
        string[]? directors,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(cast), unchecked(seed + 7919));

        // act
        var result = SimilarityScorer.Score(first, second, Options(Weights(seed)), Table, Counts);

        // assert
        return double.IsFinite(result.Score) && result.Score >= 0.0 && result.Score <= 1.0;
    }

    [Property(MaxTest = 100, Replay = "338002,200009")]
    [Trait("Category", "Property")]
    public bool Score_SwappedArguments_AreSymmetricWithinTolerance(
        string[]? genres,
        string[]? directors,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(cast), unchecked(seed + 7919));
        var options = Options(Weights(seed));

        // act
        var forward = SimilarityScorer.Score(first, second, options, Table, Counts).Score;
        var reverse = SimilarityScorer.Score(second, first, options, Table, Counts).Score;

        // assert
        return Math.Abs(forward - reverse) <= SymmetryTolerance;
    }

    [Property(MaxTest = 100, Replay = "338003,300011")]
    [Trait("Category", "Property")]
    public bool Score_ProfileWithEveryScoringFeatureAgainstItself_IsExactlyOne(int seed)
    {
        // arrange
        var movie = AnchoredProfile(seed);

        // act
        var result = SimilarityScorer.Score(movie, movie, Options(new SimilarityWeights()), Table, Counts);

        // assert
        return Math.Abs(result.Score - 1.0) <= SymmetryTolerance;
    }

    [Property(MaxTest = 100, Replay = "338004,400001")]
    [Trait("Category", "Property")]
    public bool Score_Scoring_LeavesInputSnapshotsUnchanged(
        string[]? genres,
        string[]? directors,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(cast), unchecked(seed + 7919));
        var snapshot = new Snapshot(first);

        // act
        _ = SimilarityScorer.Score(first, second, Options(Weights(seed)), Table, Counts);

        // assert
        return snapshot.Matches(first);
    }

    [Property(MaxTest = 100, Replay = "338005,500021")]
    [Trait("Category", "Property")]
    public bool Score_SwappedArguments_ProduceIdenticalReasons(
        string[]? genres,
        string[]? directors,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(cast), unchecked(seed + 7919));
        var options = Options(Weights(seed));

        // act
        var forward = SimilarityScorer.Score(first, second, options, Table, Counts);
        var reverse = SimilarityScorer.Score(second, first, options, Table, Counts);

        // assert
        return forward.Reasons.Select(reason => reason.Text)
            .SequenceEqual(reverse.Reasons.Select(reason => reason.Text), StringComparer.OrdinalIgnoreCase);
    }

    [Property(MaxTest = 100, Replay = "338006,600007")]
    [Trait("Category", "Property")]
    public bool Score_GeneratedProfiles_BreakdownReconcilesToScore(
        string[]? genres,
        string[]? directors,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(cast), unchecked(seed + 7919));

        // act
        var result = SimilarityScorer.Score(first, second, Options(Weights(seed)), Table, Counts);

        // assert
        return result.Breakdown.Select(entry => entry.Name).SequenceEqual(CriterionOrder)
            && Math.Abs(result.Score - result.Breakdown.Sum(entry => entry.Contribution)) <= ReconciliationTolerance;
    }

    [Property(MaxTest = 100, Replay = "338007,700001")]
    [Trait("Category", "Property")]
    public bool Score_ThemesOnly_IsZeroWhenEitherSideHasNoCountedKeyword(int seed, int branch)
    {
        // arrange
        var first = new MovieFeatures(keywordNames: Keywords(seed));
        var second = new MovieFeatures(keywordNames: PositiveMod(branch, 2) == 0 ? [] : Keywords(unchecked(seed + 7919)));
        var options = Options(OnlyThemes());

        // act
        var forward = SimilarityScorer.Score(first, second, options, Table, Counts).Score;
        var reverse = SimilarityScorer.Score(second, first, options, Table, Counts).Score;

        // assert
        return forward.Equals(reverse)
            && (first.KeywordNames.Count == 0 || second.KeywordNames.Count == 0
                ? forward.Equals(0.0)
                : forward is >= 0.0 and <= 1.0);
    }

    [Property(MaxTest = 100, Replay = "338008,800003")]
    [Trait("Category", "Property")]
    public bool Score_CollectionIds_IsOneWhenEqualAndZeroWhenDifferent(int firstId, int secondId)
    {
        // arrange
        var first = new MovieFeatures(collectionId: Positive(firstId));
        var second = new MovieFeatures(collectionId: Positive(secondId));
        var options = Options(OnlyCollection());

        // act
        var score = SimilarityScorer.Score(first, second, options, Table, Counts).Score;

        // assert
        return score.Equals(Positive(firstId) == Positive(secondId) ? 1.0 : 0.0);
    }

    private static int Positive(int value) => (int)(Math.Abs((long)value) % 5000) + 1;

    private static MovieFeatures AnchoredProfile(int seed) =>
        new(
            genres: ["Drama"],
            directors: ["Anchor"],
            cast: ["Actor A", "Actor B"],
            collectionId: Positive(seed),
            keywordNames: ["alpha", "anchor-theme"],
            collectionName: "Saga",
            plotEmbedding: [1f, 0f]);

    private static SimilarityWeights OnlyThemes() =>
        new()
        {
            Collection = 0.0,
            Director = 0.0,
            Themes = 1.0,
            Plot = 0.0,
            Cast = 0.0,
            Genres = 0.0,
        };

    private static SimilarityWeights OnlyCollection() =>
        new()
        {
            Collection = 1.0,
            Director = 0.0,
            Themes = 0.0,
            Plot = 0.0,
            Cast = 0.0,
            Genres = 0.0,
        };

    private static RecommendationOptions Options(SimilarityWeights weights) =>
        new() { Weights = weights };

    private static string[] Keywords(int seed)
    {
        var count = PositiveMod(seed, 4);
        return [.. Enumerable.Range(0, count)
            .Select(index => KeywordVocabulary[PositiveMod((seed / 7) + (index * 3), KeywordVocabulary.Length)])];
    }

    private static MovieFeatures Profile(
        string[]? genres,
        string[]? directors,
        string[]? cast,
        int seed)
    {
        var embedding = (seed & 8) == 0
            ? (IEnumerable<float>)[1f, 0f]
            : (seed & 16) == 0 ? [0.6f, 0.8f] : null;

        return new MovieFeatures(
            CleanGenres(genres),
            directors,
            null,
            cast,
            collectionId: (seed & 32) == 0 ? Positive(seed / 3) : null,
            keywordNames: Keywords(seed),
            collectionName: (seed & 64) == 0 ? "Saga" : null,
            plotEmbedding: embedding);
    }

    private static SimilarityWeights Weights(int seed) =>
        PositiveMod(seed, 5) switch
        {
            0 => new SimilarityWeights(),
            1 => new SimilarityWeights
            {
                Collection = 0.25,
                Director = 0.5,
                Themes = 1.0,
                Plot = 2.0,
                Cast = 3.0,
                Genres = 10.0,
            },
            2 => new SimilarityWeights
            {
                Collection = 1.0,
                Director = 1.0,
                Themes = 1.0,
                Plot = 1.0,
                Cast = 1.0,
                Genres = 1.0,
            },
            3 => new SimilarityWeights
            {
                Collection = 0.0,
                Director = 1.0,
                Themes = 0.0,
                Plot = 0.0,
                Cast = 0.0,
                Genres = 1e6,
            },
            _ => new SimilarityWeights
            {
                Collection = 1e-6,
                Director = 0.0,
                Themes = 1e6,
                Plot = 1e-6,
                Cast = 0.0,
                Genres = 1e-6,
            },
        };

    private static string[] CleanGenres(string[]? genres) =>
        [.. (genres ?? []).Where(value => !string.IsNullOrWhiteSpace(value))];

    private static string[]? Vary(string[]? values) =>
        values?.Reverse().Select(ToggleCase).ToArray();

    private static string ToggleCase(string value) =>
        new([.. value.Select(ToggleCase)]);

    private static char ToggleCase(char value) =>
        value switch
        {
            >= 'a' and <= 'z' => (char)(value - 32),
            >= 'A' and <= 'Z' => (char)(value + 32),
            _ => value,
        };

    private static int PositiveMod(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    private sealed class Snapshot
    {
        private readonly string[] _genres;
        private readonly string[] _directors;
        private readonly string[] _cast;
        private readonly string[] _keywordNames;
        private readonly int? _collectionId;
        private readonly string? _collectionName;
        private readonly float[] _plotEmbedding;

        public Snapshot(MovieFeatures movie)
        {
            _genres = [.. movie.Genres];
            _directors = [.. movie.Directors];
            _cast = [.. movie.Cast];
            _keywordNames = [.. movie.KeywordNames];
            _collectionId = movie.CollectionId;
            _collectionName = movie.CollectionName;
            _plotEmbedding = [.. movie.PlotEmbedding];
        }

        public bool Matches(MovieFeatures movie) =>
            movie.Genres.SetEquals(_genres)
            && movie.Directors.SetEquals(_directors)
            && movie.Cast.SetEquals(_cast)
            && movie.KeywordNames.SetEquals(_keywordNames)
            && movie.CollectionId == _collectionId
            && string.Equals(movie.CollectionName, _collectionName, StringComparison.Ordinal)
            && movie.PlotEmbedding.SequenceEqual(_plotEmbedding);
    }
}
