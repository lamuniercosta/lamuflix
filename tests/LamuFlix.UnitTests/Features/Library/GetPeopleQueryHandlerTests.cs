using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class GetPeopleQueryHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly GetPeopleQueryHandler handler;

    public GetPeopleQueryHandlerTests()
    {
        handler = new GetPeopleQueryHandler(catalog);
    }

    [Fact]
    public async Task HandleAsync_StoredActors_ReturnsFacetsFromCatalog()
    {
        // arrange
        IReadOnlyList<PersonFacet> expected = [new PersonFacet(1, "Ada"), new PersonFacet(2, "Bert")];
        var ct = CancellationToken.None;
        catalog.GetPeopleAsync(ct).Returns(expected);

        // act
        var result = await handler.HandleAsync(new GetPeopleQuery(), ct);

        // assert
        result.ShouldBe(expected);
        await catalog.Received(1).GetPeopleAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_NoStoredActors_ReturnsEmptyFacets()
    {
        // arrange
        var ct = CancellationToken.None;
        catalog.GetPeopleAsync(ct).Returns([]);

        // act
        var result = await handler.HandleAsync(new GetPeopleQuery(), ct);

        // assert
        result.ShouldBeEmpty();
        await catalog.Received(1).GetPeopleAsync(ct);
    }
}
