using System.Collections.Immutable;

namespace LamuFlix.Evaluation.Replay;

public sealed class AsOfHistoryTests
{
    [Fact]
    public void CatalogGenreWeights_MixedCaseGenres_CountAsSingleIdentity()
    {
        // arrange
        var catalog = ImmutableArray.Create(
            ReplayFixtureFactory.Movie(1, "Drama"),
            ReplayFixtureFactory.Movie(2, "drama"));

        // act
        var weights = AsOfHistory.CatalogGenreWeights(catalog);

        // assert
        weights.GetWeight("Drama").ShouldBe(1.0);
        weights.GetWeight("drama").ShouldBe(1.0);
    }
}
