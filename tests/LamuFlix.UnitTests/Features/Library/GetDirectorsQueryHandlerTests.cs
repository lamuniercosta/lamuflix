using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class GetDirectorsQueryHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly GetDirectorsQueryHandler handler;

    public GetDirectorsQueryHandlerTests()
    {
        handler = new GetDirectorsQueryHandler(catalog);
    }

    [Fact]
    public async Task HandleAsync_StoredDirectors_ReturnsFacetsFromCatalog()
    {
        // arrange
        IReadOnlyList<DirectorFacet> expected = [new DirectorFacet(1, "Ann", 2), new DirectorFacet(2, "Bob", 1)];
        var ct = CancellationToken.None;
        catalog.GetDirectorsAsync(ct).Returns(expected);

        // act
        var result = await handler.HandleAsync(new GetDirectorsQuery(), ct);

        // assert
        result.ShouldBe(expected);
        await catalog.Received(1).GetDirectorsAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_NoStoredDirectors_ReturnsEmptyFacets()
    {
        // arrange
        var ct = CancellationToken.None;
        catalog.GetDirectorsAsync(ct).Returns([]);

        // act
        var result = await handler.HandleAsync(new GetDirectorsQuery(), ct);

        // assert
        result.ShouldBeEmpty();
        await catalog.Received(1).GetDirectorsAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_IsForwardedToCatalog()
    {
        // arrange
        using var source = new CancellationTokenSource();
        var ct = source.Token;
        catalog.GetDirectorsAsync(ct).Returns([]);

        // act
        await handler.HandleAsync(new GetDirectorsQuery(), ct);

        // assert
        await catalog.Received(1).GetDirectorsAsync(ct);
    }
}
