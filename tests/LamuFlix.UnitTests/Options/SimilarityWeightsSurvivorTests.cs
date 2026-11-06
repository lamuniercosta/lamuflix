using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SimilarityWeightsSurvivorTests
{
    [Fact]
    public void HasEnabledReviewChannel_AllChannelsDisabled_IsFalse()
    {
        // arrange
        var weights = new SimilarityWeights
        {
            Genres = 0.0,
            Cast = 0.0,
            Director = 0.0,
            ReviewPlotWeight = 0.0,
        };

        // act
        var enabled = weights.HasEnabledReviewChannel;

        // assert
        enabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1.0, 0.0, 0.0, 0.0)]
    [InlineData(0.0, 2.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 3.0, 0.0)]
    [InlineData(0.0, 0.0, 0.0, 1.0)]
    public void HasEnabledReviewChannel_SingleChannelEnabled_IsTrue(
        double genres,
        double cast,
        double director,
        double plot)
    {
        // arrange
        var weights = new SimilarityWeights
        {
            Genres = genres,
            Cast = cast,
            Director = director,
            ReviewPlotWeight = plot,
        };

        // act
        var enabled = weights.HasEnabledReviewChannel;

        // assert
        enabled.ShouldBeTrue();
    }
}
