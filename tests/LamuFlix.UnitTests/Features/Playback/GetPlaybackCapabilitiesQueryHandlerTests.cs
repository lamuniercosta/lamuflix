using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Playback;

namespace LamuFlix.UnitTests.Features.Playback;

public sealed class GetPlaybackCapabilitiesQueryHandlerTests
{
    private readonly GetPlaybackCapabilitiesQueryHandler handler = new();

    [Fact]
    public async Task HandleAsync_LocalPlayEnabled_ReturnsTheEnabledFlag()
    {
        // arrange
        var query = new GetPlaybackCapabilitiesQuery(true);
        var ct = CancellationToken.None;

        // act
        var result = await handler.HandleAsync(query, ct);

        // assert
        result.LocalPlayEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_LocalPlayDisabled_ReturnsTheDisabledFlag()
    {
        // arrange
        var query = new GetPlaybackCapabilitiesQuery(false);
        var ct = CancellationToken.None;

        // act
        var result = await handler.HandleAsync(query, ct);

        // assert
        result.LocalPlayEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_CompletedQuery_ReturnsAnAlreadyCompletedTask()
    {
        // arrange
        var query = new GetPlaybackCapabilitiesQuery(true);
        var ct = CancellationToken.None;

        // act
        var handling = handler.HandleAsync(query, ct);

        // assert
        handling.IsCompletedSuccessfully.ShouldBeTrue();
        (await handling).LocalPlayEnabled.ShouldBeTrue();
    }
}
