using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Library;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class BrowseMoviesQueryHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly BrowseMoviesQueryHandler handler;
    private readonly Fixture fixture = new();

    public BrowseMoviesQueryHandlerTests()
    {
        handler = new BrowseMoviesQueryHandler(catalog);
    }

    [Fact]
    public async Task HandleAsync_PassesQueryUnchangedAndReturnsPage()
    {
        // arrange
        var query = new MovieQuery { Text = Faker.Lorem.GetFirstWord(), Page = new Page(1, 20) };
        var expected = new PagedResult<MovieSummary>(
            [new MovieSummary(NewId(), "One")],
            1);
        var ct = CancellationToken.None;
        catalog.BrowseAsync(query, ct).Returns(expected);

        // act
        var result = await handler.HandleAsync(new BrowseMoviesQuery(query), ct);

        // assert
        result.ShouldBe(expected);
        await catalog.Received(1).BrowseAsync(query, ct);
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
