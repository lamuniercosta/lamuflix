using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class GetMovieDetailsQueryHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly GetMovieDetailsQueryHandler handler;
    private readonly Fixture fixture = new();

    public GetMovieDetailsQueryHandlerTests()
    {
        handler = new GetMovieDetailsQueryHandler(catalog);
    }

    [Fact]
    public async Task HandleAsync_StoredMovie_ReturnsDetails()
    {
        // arrange
        var id = NewId();
        var details = new MovieDetails(
            id,
            "Details",
            new LibraryPath("C:/library/a"),
            new MediaFormat("mkv"),
            null);
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(details);

        // act
        var result = await handler.HandleAsync(new GetMovieDetailsQuery(id), ct);

        // assert
        result.ShouldBe(details);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFound()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns((MovieDetails?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new GetMovieDetailsQuery(id), ct));
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
