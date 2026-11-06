using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class ReviewSurvivorTests
{
    [Fact]
    public void SmartEnum_Names_MatchTheWireContract()
    {
        // arrange / act / assert
        Review.None.Name.ShouldBe("none");
        Review.Liked.Name.ShouldBe("liked");
        Review.Disliked.Name.ShouldBe("disliked");
    }

    [Fact]
    public void SmartEnum_Values_AreZeroOneTwo()
    {
        // arrange / act / assert
        Review.None.Value.ShouldBe(0);
        Review.Liked.Value.ShouldBe(1);
        Review.Disliked.Value.ShouldBe(2);
    }
}
