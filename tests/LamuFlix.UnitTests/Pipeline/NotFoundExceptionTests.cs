using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Pipeline;

public sealed class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsNotFoundMessage()
    {
        // act
        var thrown = new NotFoundException();

        // assert
        thrown.Message.ShouldBe("Not found.");
    }
}
