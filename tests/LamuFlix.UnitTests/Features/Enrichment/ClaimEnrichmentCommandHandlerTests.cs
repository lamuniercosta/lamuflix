using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ClaimEnrichmentCommandHandlerTests
{
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly ClaimEnrichmentCommandHandler handler;
    private readonly Fixture fixture = new();

    public ClaimEnrichmentCommandHandlerTests()
    {
        handler = new ClaimEnrichmentCommandHandler(movies);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ReturnsClaimResultUnchanged(bool claimed)
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.TryClaimForEnrichmentAsync(id, ct).Returns(claimed);

        // act
        var result = await handler.HandleAsync(new ClaimEnrichmentCommand(id), ct);

        // assert
        result.ShouldBe(claimed);
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        // arrange
        var id = NewId();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        movies.TryClaimForEnrichmentAsync(id, ct).Returns(true);

        // act
        await handler.HandleAsync(new ClaimEnrichmentCommand(id), ct);

        // assert
        await movies.Received(1).TryClaimForEnrichmentAsync(id, ct);
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
