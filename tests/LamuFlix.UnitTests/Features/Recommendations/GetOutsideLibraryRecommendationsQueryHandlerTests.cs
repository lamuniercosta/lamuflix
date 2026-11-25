using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetOutsideLibraryRecommendationsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_LocalSnapshotOnly_ReturnsRankedCards()
    {
        // arrange
        var source = Substitute.For<IRecommendationCandidateSource>();
        source.GetOutsideLibraryRecommendationsAsync(Arg.Any<CancellationToken>())
            .Returns(Snapshot());
        var handler = Handler(source);

        // act
        var result = await handler.HandleAsync(
            new GetOutsideLibraryRecommendationsQuery(),
            TestContext.Current.CancellationToken);

        // assert
        result.ShouldHaveSingleItem().TmdbId.ShouldBe(100);
        await source.Received(1).GetOutsideLibraryRecommendationsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptySnapshot_ReturnsEmptyList()
    {
        // arrange
        var source = Substitute.For<IRecommendationCandidateSource>();
        source.GetOutsideLibraryRecommendationsAsync(Arg.Any<CancellationToken>())
            .Returns(new OutsideLibraryRecommendationSnapshot([], []));
        var handler = Handler(source);

        // act
        var result = await handler.HandleAsync(
            new GetOutsideLibraryRecommendationsQuery(),
            TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_CancelledRequest_DoesNotReadTheSource()
    {
        // arrange
        var source = Substitute.For<IRecommendationCandidateSource>();
        var handler = Handler(source);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        var failure = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await handler.HandleAsync(new GetOutsideLibraryRecommendationsQuery(), cancellation.Token));

        // assert
        failure.CancellationToken.ShouldBe(cancellation.Token);
        await source.DidNotReceiveWithAnyArgs()
            .GetOutsideLibraryRecommendationsAsync(Arg.Any<CancellationToken>());
    }

    private static GetOutsideLibraryRecommendationsQueryHandler Handler(IRecommendationCandidateSource source) =>
        new(source, Microsoft.Extensions.Options.Options.Create(new RecommendationOptions()));

    private static OutsideLibraryRecommendationSnapshot Snapshot()
    {
        OutsideLibraryRecommendationContribution Seed(int sourceId) =>
            new(
                new MovieId(sourceId),
                "Seed",
                Review.Liked,
                false,
                false,
                100,
                "Candidate",
                2001,
                null);

        return new OutsideLibraryRecommendationSnapshot([Seed(1), Seed(2)], []);
    }
}
