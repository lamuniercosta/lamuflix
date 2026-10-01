using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Playback;

namespace LamuFlix.UnitTests.Playback;

public sealed class DisabledMediaPlayerLauncherTests
{
    [Fact]
    public void Launch_Always_ThrowsFeatureDisabledException()
    {
        // arrange
        var launcher = new DisabledMediaPlayerLauncher();

        // act
        var exception = Should.Throw<FeatureDisabledException>(() =>
            launcher.Launch(new LibraryPath(@"C:\library\film.mkv"), new MediaFormat("mp4")));

        // assert
        exception.Message.ShouldBe("LocalPlay is disabled in this environment.");
    }
}
