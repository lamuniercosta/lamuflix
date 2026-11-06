using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class LikedReviewReasonTests
{
    [Fact]
    public void AddLikedReviewReason_PositiveEvidenceWithEnabledWeights_EmitsOneChip()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.4, source: Source("Alien"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, 0.6, new RecommendationOptions());

        // assert
        builder.Count.ShouldBe(1);
        builder[0].Kind.ShouldBe(ReasonKind.Personal);
        builder[0].Text.ShouldBe("Because you liked Alien");
    }

    [Fact]
    public void AddLikedReviewReason_ZeroLikedAffinity_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.0, source: Source("Alien"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, 0.6, new RecommendationOptions());

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void AddLikedReviewReason_NonPositiveNetContribution_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.4, source: Source("Alien"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, 0.0, new RecommendationOptions());

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void AddLikedReviewReason_DisabledLikedWeight_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.4, source: Source("Alien"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(
            builder,
            evidence,
            0.6,
            new RecommendationOptions { LikedReviewWeight = 0.0 });

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void AddLikedReviewReason_DisabledProfileWeight_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.4, source: Source("Alien"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(
            builder,
            evidence,
            0.6,
            new RecommendationOptions { BlendProfileWeight = 0.0 });

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void AddLikedReviewReason_DisabledReviewChannel_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = new ReviewAffinityEvidence(0.4, 0.0, false, Source("Alien"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, 0.6, new RecommendationOptions());

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void AddLikedReviewReason_NegativeOnlyProfile_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = new ReviewAffinityEvidence(0.0, 0.8, true, null);

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, -0.2, new RecommendationOptions());

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void AddLikedReviewReason_ControlCharactersInTitle_AreSanitized()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.4, source: Source("Alien\u0007\n"));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, 0.6, new RecommendationOptions());

        // assert
        builder.Single().Text.ShouldBe("Because you liked Alien");
    }

    [Fact]
    public void AddLikedReviewReason_BlankTitle_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = Evidence(liked: 0.4, source: Source("   "));

        // act
        RecommendationReasonBuilder.AddLikedReviewReason(builder, evidence, 0.6, new RecommendationOptions());

        // assert
        builder.ShouldBeEmpty();
    }

    [Fact]
    public void ProfileContribution_Formula_AppliesBothReviewCoefficientsWithoutClamping()
    {
        // arrange
        var options = new RecommendationOptions { LikedReviewWeight = 2.0, DislikedReviewWeight = 0.5 };
        var evidence = new ReviewAffinityEvidence(0.3, 1.0, true, null);

        // act
        var contribution = RecommendationScoring.ProfileContribution(0.2, evidence, options);

        // assert
        contribution.ShouldBe(0.2 + (2.0 * 0.3) - (0.5 * 1.0), 1e-12);
    }

    private static ReviewAffinityEvidence Evidence(double liked, LikedSourceEvidence? source) =>
        new(liked, 0.0, true, source);

    private static LikedSourceEvidence Source(string title) =>
        new(new MovieId(1), title, 0.5);
}
