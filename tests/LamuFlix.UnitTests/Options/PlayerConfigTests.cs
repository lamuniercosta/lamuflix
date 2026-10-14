using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class PlayerConfigTests
{
    [Fact]
    public void Defaults_NameAndExecutablePath_AreEmptyAndFormatsIsEmpty()
    {
        // arrange
        var config = new PlayerConfig();

        // act
        var name = config.Name;
        var executablePath = config.ExecutablePath;
        var formats = config.Formats;

        // assert
        name.ShouldBe(string.Empty);
        executablePath.ShouldBe(string.Empty);
        formats.ShouldBeEmpty();
    }
}
