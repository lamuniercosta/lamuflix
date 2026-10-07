using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class GetGenresQueryHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly GetGenresQueryHandler handler;

    public GetGenresQueryHandlerTests()
    {
        handler = new GetGenresQueryHandler(catalog);
    }

    [Fact]
    public async Task HandleAsync_StoredGenres_ReturnsFacetsFromCatalog()
    {
        // arrange
        IReadOnlyList<GenreFacet> expected = [new GenreFacet(1, "Drama"), new GenreFacet(2, "Comedy")];
        var ct = CancellationToken.None;
        catalog.GetGenresAsync(ct).Returns(expected);

        // act
        var result = await handler.HandleAsync(new GetGenresQuery(), ct);

        // assert
        result.ShouldBe(expected);
        await catalog.Received(1).GetGenresAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_NoStoredGenres_ReturnsEmptyFacets()
    {
        // arrange
        var ct = CancellationToken.None;
        catalog.GetGenresAsync(ct).Returns([]);

        // act
        var result = await handler.HandleAsync(new GetGenresQuery(), ct);

        // assert
        result.ShouldBeEmpty();
        await catalog.Received(1).GetGenresAsync(ct);
    }
}
