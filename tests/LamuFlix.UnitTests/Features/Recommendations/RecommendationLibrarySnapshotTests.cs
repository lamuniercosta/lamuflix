using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class RecommendationLibrarySnapshotTests
{
    [Fact]
    public void Count_MultipleEntries_ReturnsEntryCount()
    {
        // arrange
        var snapshot = new RecommendationLibrarySnapshot(
            [Entry(90, "Saga Starter"), Entry(2, "Candidate")],
            CrowdLinks.Empty);

        // act
        var count = snapshot.Count;

        // assert
        count.ShouldBe(2);
    }

    [Fact]
    public void Count_NoEntries_ReturnsZero()
    {
        // arrange
        var snapshot = new RecommendationLibrarySnapshot([], CrowdLinks.Empty);

        // act
        var count = snapshot.Count;

        // assert
        count.ShouldBe(0);
    }

    [Fact]
    public void Ctor_NullEntries_ThrowsArgumentNullException()
    {
        // arrange, act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => new RecommendationLibrarySnapshot(null!, CrowdLinks.Empty));

        // assert
        failure.ParamName.ShouldBe("entries");
    }

    [Fact]
    public void Ctor_NullCrowdLinks_ThrowsArgumentNullException()
    {
        // arrange, act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => new RecommendationLibrarySnapshot([], null!));

        // assert
        failure.ParamName.ShouldBe("crowdLinks");
    }

    private static RecommendationLibraryEntry Entry(int id, string title) =>
        new(
            new MovieFeatures(genres: ["Drama"]),
            new MovieSummary(
                new MovieId(id),
                title,
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                false,
                false,
                Review.None));
}
