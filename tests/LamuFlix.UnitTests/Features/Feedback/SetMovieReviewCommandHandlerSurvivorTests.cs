using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Feedback;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Feedback;

public sealed class SetMovieReviewCommandHandlerSurvivorTests
{
    [Fact]
    public async Task HandleAsync_NullCommand_ThrowsArgumentNullException()
    {
        // arrange
        var handler = new SetMovieReviewCommandHandler(Substitute.For<IMovieRepository>());

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = await Should.ThrowAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        // assert
        failure.ParamName.ShouldBe("command");
    }
}
