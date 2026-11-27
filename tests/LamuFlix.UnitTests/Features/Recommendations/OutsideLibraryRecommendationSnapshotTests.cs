using System;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryRecommendationSnapshotTests
{
    [Fact]
    public void Constructor_NullContributions_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
            new OutsideLibraryRecommendationSnapshot(null!, []));

        // assert
        failure.ParamName.ShouldBe("contributions");
    }

    [Fact]
    public void Constructor_NullLibrary_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
            new OutsideLibraryRecommendationSnapshot([], null!));

        // assert
        failure.ParamName.ShouldBe("library");
    }
}
