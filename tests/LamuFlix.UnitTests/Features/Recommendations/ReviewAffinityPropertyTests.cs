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

public sealed class ReviewAffinityPropertyTests
{
    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
                ["Horror"] = 40,
                ["SciFi"] = 5,
            }));

    private static readonly string[] GenrePool = ["Drama", "Comedy", "Horror", "SciFi"];

    [Property]
    [Trait("Category", "Property")]
    public void EvaluateReviewAffinity_GeneratedProfiles_StayBounded() =>
        Prop.ForAll(
            AffinityGen().ToArbitrary(),
            input =>
            {
                var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                    [.. input.CandidateGenres],
                    [],
                    [],
                    ImmutableArray<float>.Empty,
                    input.Profile,
                    input.Weights,
                    Table);

                return double.IsFinite(evidence.LikedAffinity)
                    && double.IsFinite(evidence.DislikedAffinity)
                    && evidence.LikedAffinity is >= 0.0 and <= 1.0
                    && evidence.DislikedAffinity is >= 0.0 and <= 1.0;
            }).QuickCheckThrowOnFailure();

    [Property]
    [Trait("Category", "Property")]
    public void EvaluateReviewAffinity_IdenticalGenreProfile_YieldsUnitAffinity()
    {
        Prop.ForAll(
            AffinityGen().ToArbitrary(),
            input =>
            {
                if (input.CandidateGenres.IsDefaultOrEmpty)
                {
                    return true;
                }

                var profile = input.Profile with
                {
                    Liked = Channels(input.CandidateGenres),
                    Disliked = ReviewProfileChannels.Empty,
                };
                var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                    [.. input.CandidateGenres],
                    [],
                    [],
                    ImmutableArray<float>.Empty,
                    profile,
                    new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 },
                    Table);

                return Math.Abs(evidence.LikedAffinity - 1.0) < 1e-9;
            }).QuickCheckThrowOnFailure();
    }

    [Property]
    [Trait("Category", "Property")]
    public void ProfileContribution_IsSignedAndUnclamped() =>
        Prop.ForAll(
            Arb.From(Gen.Choose(0, 100).Select(value => value / 100.0)),
            Arb.From(Gen.Choose(0, 100).Select(value => value / 100.0)),
            Arb.From(Gen.Choose(0, 100).Select(value => value / 100.0)),
            (baseline, liked, disliked) =>
            {
                var options = new RecommendationOptions
                {
                    LikedReviewWeight = 1.0,
                    DislikedReviewWeight = 1.0,
                };
                var evidence = new ReviewAffinityEvidence(liked, disliked, true, null);

                var contribution = RecommendationScoring.ProfileContribution(baseline, evidence, options);

                return Math.Abs(contribution - (baseline + liked - disliked)) < 1e-12;
            }).QuickCheckThrowOnFailure();

    [Property]
    [Trait("Category", "Property")]
    public void EvaluateReviewAffinity_MissingMetadata_NeverInventsSignal() =>
        Prop.ForAll(
            AffinityGen().ToArbitrary(),
            input =>
            {
                var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                    [],
                    [],
                    [],
                    ImmutableArray<float>.Empty,
                    ReviewlessProfile(input.Profile),
                    input.Weights,
                    Table);

                return evidence.LikedAffinity == 0.0
                    && evidence.DislikedAffinity == 0.0
                    && !evidence.HasReviewChannel
                    && evidence.LikedSource is null;
            }).QuickCheckThrowOnFailure();

    [Fact]
    public void Validate_InvalidReviewCoefficients_AreRejected()
    {
        // arrange
        var zero = RecommendationOptionsValidator(new RecommendationOptions { LikedReviewWeight = 0.0 });
        var negative = RecommendationOptionsValidator(new RecommendationOptions { DislikedReviewWeight = -1.0 });
        var nonFinite = RecommendationOptionsValidator(new RecommendationOptions { LikedReviewWeight = double.NaN });

        // assert
        zero.ShouldNotBeEmpty();
        negative.ShouldNotBeEmpty();
        nonFinite.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_NoEnabledReviewChannel_IsRejected()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights
            {
                Director = 0.0,
                Cast = 0.0,
                Genres = 0.0,
                ReviewPlotWeight = 0.0,
            },
        };

        // act
        var results = RecommendationOptionsValidator(options);

        // assert
        results.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_Defaults_AreValid()
    {
        // arrange / act
        var results = RecommendationOptionsValidator(new RecommendationOptions());

        // assert
        results.ShouldBeEmpty();
    }

    private sealed class StubTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed record GenreCase(int Count, List<string> Genres);

    [Property]
    [Trait("Category", "Property")]
    public void EvaluateReviewAffinity_EqualPerMovieWeight_IsInvariantToLikedCount()
    {
        Prop.ForAll(
            Arb.From(
                Gen.Choose(1, 4).SelectMany(count =>
                    Gen.Choose(1, 3)
                        .SelectMany(length => Gen.ListOf(Gen.Elements(GenrePool), length))
                        .Select(genres => new GenreCase(count, genres)))),
            (input) =>
            {
                var single = ProfileOf(1, input.Genres);
                var many = ProfileOf(input.Count, input.Genres);
                var weights = new SimilarityWeights { Cast = 0.0, Director = 0.0, ReviewPlotWeight = 0.0 };

                var singleAffinity = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                    [.. input.Genres],
                    [],
                    [],
                    ImmutableArray<float>.Empty,
                    single,
                    weights,
                    Table).LikedAffinity;
                var manyAffinity = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                    [.. input.Genres],
                    [],
                    [],
                    ImmutableArray<float>.Empty,
                    many,
                    weights,
                    Table).LikedAffinity;

                return Math.Abs(singleAffinity - manyAffinity) < 1e-9;
            }).QuickCheckThrowOnFailure();
    }

    private static TasteProfile ProfileOf(int likedCount, IEnumerable<string> genres)
    {
        var movies = Enumerable.Range(1, likedCount)
            .Select(id => new TasteProfileMovie(
                new MovieId(id),
                [.. genres],
                ImmutableArray<float>.Empty,
                ImmutableArray<DateTimeOffset>.Empty,
                IsWatched: false)
            {
                Review = Review.Liked,
                Title = $"Liked {id}",
            })
            .ToImmutableArray();

        return new TasteProfileCalculator(new StubTimeProvider(), new RecommendationOptions())
            .Calculate(movies, Table);
    }

    private static TasteProfile ReviewlessProfile(TasteProfile profile) =>
        profile with
        {
            Liked = ReviewProfileChannels.Empty,
            Disliked = ReviewProfileChannels.Empty,
            LikedSources = [],
        };

    private static List<System.ComponentModel.DataAnnotations.ValidationResult> RecommendationOptionsValidator(
        RecommendationOptions options)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options,
            new System.ComponentModel.DataAnnotations.ValidationContext(options),
            results,
            validateAllProperties: true);
        return results;
    }

    private static Gen<AffinityCase> AffinityGen() =>
        from candidateGenres in GenOfGenres()
        from likedGenres in GenOfGenres()
        from dislikedGenres in GenOfGenres()
        from disableCast in Gen.Elements(false, true)
        from disableDirector in Gen.Elements(false, true)
        from disablePlot in Gen.Elements(false, true)
        from disableGenres in Gen.Elements(false, true)
        select new AffinityCase(
            [.. candidateGenres],
            new TasteProfile(
                ImmutableDictionary<string, double>.Empty,
                ImmutableArray<float>.Empty)
            {
                Liked = Channels(likedGenres),
                Disliked = Channels(dislikedGenres),
            },
            new SimilarityWeights
            {
                Genres = disableGenres ? 0.0 : 3.0,
                Cast = disableCast ? 0.0 : 2.0,
                Director = disableDirector ? 0.0 : 3.0,
                ReviewPlotWeight = disablePlot ? 0.0 : 1.0,
            });

    private static Gen<List<string>> GenOfGenres() =>
        Gen.Choose(0, 3).SelectMany(count => Gen.ListOf(Gen.Elements(GenrePool), count));

    private static ReviewProfileChannels Channels(IEnumerable<string> genres) =>
        new(
            ImmutableArray<float>.Empty,
            genres
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableDictionary(genre => genre, Table.GetWeight, StringComparer.OrdinalIgnoreCase),
            ImmutableDictionary<string, double>.Empty,
            ImmutableDictionary<string, double>.Empty);

    private sealed record AffinityCase(
        ImmutableArray<string> CandidateGenres,
        TasteProfile Profile,
        SimilarityWeights Weights);
}

