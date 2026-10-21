using System;
using System.Collections.Generic;
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
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 1,
                ["Comedy"] = 90,
                ["Shared"] = 50,
            }));

    [Property(MaxTest = 100, Replay = "338001,100003")]
    [Trait("Category", "Property")]
    public bool Score_GeneratedProfiles_IsFiniteAndWithinUnitInterval(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, writers, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(writers), Vary(cast), unchecked(seed + 7919));

        // act
        var result = SimilarityScorer.Score(first, second, Weights(seed), Table);

        // assert
        return double.IsFinite(result.Score) && result.Score >= 0.0 && result.Score <= 1.0;
    }

    [Property(MaxTest = 100, Replay = "338002,200009")]
    [Trait("Category", "Property")]
    public bool Score_SwappedArguments_AreSymmetricWithinTolerance(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, writers, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(writers), Vary(cast), unchecked(seed + 7919));
        var weights = Weights(seed);

        // act
        var forward = SimilarityScorer.Score(first, second, weights, Table).Score;
        var reverse = SimilarityScorer.Score(second, first, weights, Table).Score;

        // assert
        return Math.Abs(forward - reverse) <= SymmetryTolerance;
    }

    [Property(MaxTest = 100, Replay = "338003,300011")]
    [Trait("Category", "Property")]
    public bool Score_ProfileWithScoringFeatureAgainstItself_IsExactlyOne(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast,
        int seed)
    {
        // arrange
        var movie = Profile(genres, AppendAnchor(directors), writers, cast, seed);

        // act
        var result = SimilarityScorer.Score(movie, movie, Weights(seed), Table);

        // assert
        return result.Score == 1.0;
    }

    [Property(MaxTest = 100, Replay = "338004,400001")]
    [Trait("Category", "Property")]
    public bool Score_Scoring_LeavesInputSnapshotsUnchanged(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast,
        int seed)
    {
        // arrange
        var first = Profile(genres, directors, writers, cast, seed);
        var second = Profile(Vary(genres), Vary(directors), Vary(writers), Vary(cast), unchecked(seed + 7919));
        var snapshot = new Snapshot(first);

        // act
        _ = SimilarityScorer.Score(first, second, Weights(seed), Table);

        // assert
        return snapshot.Matches(first);
    }

    private static MovieFeatures Profile(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast,
        int seed)
    {
        var releaseYear = (seed & 1) == 0
            ? new ReleaseYear(1888 + PositiveMod(seed, 140), Now)
            : null;
        var runtime = (seed & 2) == 0
            ? new Runtime(1 + PositiveMod(seed / 7, 500))
            : null;
        var rating = (seed & 4) == 0
            ? new ImdbRating(PositiveMod(seed / 13, 101) / 10.0m)
            : null;

        return new MovieFeatures(
            CleanGenres(genres),
            directors,
            writers,
            cast,
            releaseYear,
            runtime,
            rating);
    }

    private static SimilarityWeights Weights(int seed) =>
        PositiveMod(seed, 6) switch
        {
            0 => new SimilarityWeights
            {
                Director = 0.0,
                Writers = 0.0,
                Cast = 0.0,
                Genres = 0.0,
                YearProximity = 0.0,
                RuntimeProximity = 0.0,
                RatingBoost = 0.0,
            },
            1 => new SimilarityWeights(),
            2 => new SimilarityWeights
            {
                Director = double.MaxValue,
                Writers = double.MaxValue,
                Cast = double.MaxValue,
                Genres = double.MaxValue,
                YearProximity = double.MaxValue,
                RuntimeProximity = double.MaxValue,
                RatingBoost = double.MaxValue,
            },
            3 => new SimilarityWeights
            {
                Director = 1.0,
                Writers = 0.0,
                Cast = 0.0,
                Genres = 0.0,
                YearProximity = 0.0,
                RuntimeProximity = 0.0,
                RatingBoost = 0.0,
            },
            4 => new SimilarityWeights
            {
                Director = 0.0,
                Writers = 0.0,
                Cast = 0.0,
                Genres = 0.0,
                YearProximity = 1.0,
                RuntimeProximity = 0.01,
                RatingBoost = 0.0,
            },
            _ => new SimilarityWeights
            {
                Director = 0.0,
                Writers = 0.0,
                Cast = 0.0,
                Genres = 0.0,
                YearProximity = double.MaxValue,
                RuntimeProximity = double.Epsilon,
                RatingBoost = 0.0,
            },
        };

    private static string[] CleanGenres(string[]? genres) =>
        [.. (genres ?? []).Where(value => !string.IsNullOrWhiteSpace(value))];

    private static string[] AppendAnchor(string[]? directors) =>
        [.. (directors ?? []), "Anchor"];

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
        private readonly string[] _writers;
        private readonly string[] _cast;
        private readonly int? _releaseYear;
        private readonly int? _runtime;
        private readonly decimal? _imdbRating;

        public Snapshot(MovieFeatures movie)
        {
            _genres = [.. movie.Genres];
            _directors = [.. movie.Directors];
            _writers = [.. movie.Writers];
            _cast = [.. movie.Cast];
            _releaseYear = movie.ReleaseYear?.Value;
            _runtime = movie.Runtime?.Minutes;
            _imdbRating = movie.ImdbRating?.Value;
        }

        public bool Matches(MovieFeatures movie) =>
            movie.Genres.SetEquals(_genres)
            && movie.Directors.SetEquals(_directors)
            && movie.Writers.SetEquals(_writers)
            && movie.Cast.SetEquals(_cast)
            && movie.ReleaseYear?.Value == _releaseYear
            && movie.Runtime?.Minutes == _runtime
            && movie.ImdbRating?.Value == _imdbRating;
    }
}
