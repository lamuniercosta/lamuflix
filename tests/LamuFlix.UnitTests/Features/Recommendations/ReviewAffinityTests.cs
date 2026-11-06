using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class ReviewAffinityTests
{
    private const double Tolerance = 1e-9;

    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
                ["Horror"] = 40,
            }));

    [Fact]
    public void EvaluateReviewAffinity_IdenticalLikedProfile_ReturnsUnitAffinity()
    {
        // arrange
        var profile = Profile(
            liked: Channels(["Drama"], ["Actor A"], ["Director A"]));

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.LikedAffinity.ShouldBe(1.0, Tolerance);
        evidence.HasReviewChannel.ShouldBeTrue();
    }

    [Fact]
    public void EvaluateReviewAffinity_DisjointLikedProfile_ReturnsZeroAffinity()
    {
        // arrange
        var profile = Profile(
            liked: Channels(["Horror"], ["Actor Z"], ["Director Z"]));

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.LikedAffinity.ShouldBe(0.0, Tolerance);
        evidence.HasReviewChannel.ShouldBeTrue();
    }

    [Fact]
    public void EvaluateReviewAffinity_EmptyProfile_ReportsNoUsableChannel()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty);

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.LikedAffinity.ShouldBe(0.0, Tolerance);
        evidence.DislikedAffinity.ShouldBe(0.0, Tolerance);
        evidence.HasReviewChannel.ShouldBeFalse();
    }

    [Fact]
    public void EvaluateReviewAffinity_DisabledChannels_AreOmittedFromTheAverage()
    {
        // arrange
        var profile = Profile(
            liked: Channels(["Drama"], ["Actor Z"], ["Director Z"]));
        var weights = new SimilarityWeights { Director = 0.0, Cast = 0.0, ReviewPlotWeight = 0.0 };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            weights,
            Table);

        // assert
        evidence.LikedAffinity.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void EvaluateReviewAffinity_SeparateLikedAndDislikedChannels_ComputeIndependently()
    {
        // arrange
        var profile = Profile(
            liked: Channels(["Drama"], [], []),
            disliked: Channels(["Drama"], [], []));

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            [],
            [],
            ImmutableArray<float>.Empty,
            profile,
            new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 },
            Table);

        // assert
        evidence.LikedAffinity.ShouldBe(1.0, Tolerance);
        evidence.DislikedAffinity.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void EvaluateReviewAffinity_AffinitiesStayBoundedForPartialOverlap()
    {
        // arrange
        var profile = Profile(
            liked: Channels(["Drama", "Comedy"], [], []));

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama", "Horror"],
            [],
            [],
            ImmutableArray<float>.Empty,
            profile,
            new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 },
            Table);

        // assert
        (evidence.LikedAffinity > 0.0).ShouldBeTrue();
        (evidence.LikedAffinity < 1.0).ShouldBeTrue();
    }

    [Fact]
    public void EvaluateReviewAffinity_TwoLikedSources_SelectsGreatestAffinity()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = Channels(["Drama"], [], []),
            LikedSources =
            [
                new LikedReviewSource(new MovieId(9), "Weak", Channels(["Horror"], [], [])),
                new LikedReviewSource(new MovieId(4), "Strong", Channels(["Drama"], [], [])),
            ],
        };
        var weights = new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            [],
            [],
            ImmutableArray<float>.Empty,
            profile,
            weights,
            Table);

        // assert
        evidence.LikedSource.HasValue.ShouldBeTrue();
        evidence.LikedSource.GetValueOrDefault().MovieId.Value.ShouldBe(4);
        evidence.LikedSource.GetValueOrDefault().Title.ShouldBe("Strong");
    }

    [Fact]
    public void EvaluateReviewAffinity_EqualAffinitySources_BreakTiesByLowerMovieId()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = Channels(["Drama"], [], []),
            LikedSources =
            [
                new LikedReviewSource(new MovieId(7), "Higher", Channels(["Drama"], [], [])),
                new LikedReviewSource(new MovieId(2), "Lower", Channels(["Drama"], [], [])),
            ],
        };
        var weights = new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            [],
            [],
            ImmutableArray<float>.Empty,
            profile,
            weights,
            Table);

        // assert
        evidence.LikedSource.HasValue.ShouldBeTrue();
        evidence.LikedSource.GetValueOrDefault().MovieId.Value.ShouldBe(2);
    }

    [Fact]
    public void EvaluateReviewAffinity_NoSupportedLikedSource_ReturnsNullSource()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = Channels(["Horror"], [], []),
            LikedSources = [new LikedReviewSource(new MovieId(1), "Horror", Channels(["Horror"], [], []))],
        };
        var weights = new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            [],
            [],
            ImmutableArray<float>.Empty,
            profile,
            weights,
            Table);

        // assert
        evidence.LikedAffinity.ShouldBe(0.0, Tolerance);
        evidence.LikedSource.ShouldBeNull();
    }

    private static TasteProfile Profile(ReviewProfileChannels liked, ReviewProfileChannels? disliked = null) =>
        new(ImmutableDictionary<string, double>.Empty, ImmutableArray<float>.Empty)
        {
            Liked = liked,
            Disliked = disliked ?? ReviewProfileChannels.Empty,
        };

    private static ReviewProfileChannels Channels(
        string[] genres,
        string[] cast,
        string[] directors) =>
        new(
            ImmutableArray<float>.Empty,
            Preferences(genres, Table.GetWeight),
            Preferences(cast, _ => 1.0),
            Preferences(directors, _ => 1.0));

    private static ImmutableDictionary<string, double> Preferences(string[] names, Func<string, double> scale) =>
        names.ToImmutableDictionary(name => name, scale, StringComparer.OrdinalIgnoreCase);
}
