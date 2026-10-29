using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class GetCollectionsQueryHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly GetCollectionsQueryHandler handler;

    public GetCollectionsQueryHandlerTests()
    {
        handler = new GetCollectionsQueryHandler(catalog);
    }

    [Fact]
    public async Task HandleAsync_StoredCollections_ReturnsFacetsFromCatalog()
    {
        // arrange
        IReadOnlyList<CollectionFacet> expected = [new CollectionFacet(10, "Alien", 2), new CollectionFacet(20, "Matrix", 1)]; var ct = CancellationToken.None;
        catalog.GetCollectionsAsync(ct).Returns(expected);

        // act
        var result = await handler.HandleAsync(new GetCollectionsQuery(), ct);

        // assert
        result.ShouldBe(expected);
        await catalog.Received(1).GetCollectionsAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_NoStoredCollections_ReturnsEmptyFacets()
    {
        // arrange
        var ct = CancellationToken.None;
        catalog.GetCollectionsAsync(ct).Returns([]);

        // act
        var result = await handler.HandleAsync(new GetCollectionsQuery(), ct);

        // assert
        result.ShouldBeEmpty();
        await catalog.Received(1).GetCollectionsAsync(ct);
    }
}
