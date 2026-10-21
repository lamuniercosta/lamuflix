using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerGoldenTests
{
    private static readonly GenreWeights GenreTable =
        GenreWeights.Calculate(SimilarityScorerGoldenFixture.LibraryGenreCounts);

    [Fact]
    public void Score_DramaSeed_ProducesExpectedOrderedTopThree() => AssertExpectedTopThree("A-05");

    [Fact]
    public void Score_ComedySeed_ProducesExpectedOrderedTopThree() => AssertExpectedTopThree("B-08");

    [Fact]
    public void Score_HorrorSeed_ProducesExpectedOrderedTopThree() => AssertExpectedTopThree("C-06");

    private static void AssertExpectedTopThree(string seedId)
    {
        // arrange
        var expected = SimilarityScorerGoldenFixture.Seeds.Single(seed => seed.SeedId == seedId);
        var seedMovie = SimilarityScorerGoldenFixture.Movies.Single(movie => movie.Id == seedId);

        // act
        var ranking = SimilarityScorerGoldenFixture.Movies
            .Where(movie => movie.Id != seedId)
            .Select(movie => new RankedMovie(
                movie.Id,
                SimilarityScorer.Score(
                    seedMovie.Features,
                    movie.Features,
                    SimilarityScorerGoldenFixture.Weights,
                    GenreTable).Score))
            .OrderByDescending(entry => entry.Score)
            .ToList();

        // assert
        ranking.Count.ShouldBe(29);
        ranking[0].Score.ShouldBeGreaterThan(ranking[1].Score);
        ranking[1].Score.ShouldBeGreaterThan(ranking[2].Score);
        ranking[2].Score.ShouldBeGreaterThan(ranking[3].Score);
        ranking.Take(3).Select(entry => entry.Id).ShouldBe(expected.ExpectedTopThree);
    }

    private readonly record struct RankedMovie(string Id, double Score);
}
