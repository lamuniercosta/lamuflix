using System.Diagnostics;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Playback;
using NSubstitute;

namespace LamuFlix.UnitTests.Playback;

public sealed class ProcessMediaPlayerLauncherTests
{
    private readonly IProcessStarter starter = Substitute.For<IProcessStarter>();

    [Fact]
    public void Launch_MatchingFormat_StartsConfiguredPlayerWithFileAsSingleArgument()
    {
        // arrange
        var launcher = NewLauncher(
        [
            new PlayerConfig
            {
                Name = "vlc",
                ExecutablePath = @"C:\Players\vlc.exe",
                Formats = ["mkv", ".mp4"],
            },
        ]);
        var file = new LibraryPath(@"C:\library\film.mkv");
        var format = new MediaFormat("mkv");

        // act
        launcher.Launch(file, format);

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == @"C:\Players\vlc.exe"
            && psi.ArgumentList.Count == 1
            && psi.ArgumentList[0] == file.Value
            && psi.UseShellExecute));
    }

    [Fact]
    public void Launch_FormatMatchIgnoresCaseAndLeadingDot()
    {
        // arrange
        var launcher = NewLauncher(
        [
            new PlayerConfig
            {
                Name = "vlc",
                ExecutablePath = @"C:\Players\vlc.exe",
                Formats = [".MKV"],
            },
        ]);
        var file = new LibraryPath(@"C:\library\film.mkv");
        var format = new MediaFormat("Mkv");

        // act
        launcher.Launch(file, format);

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == @"C:\Players\vlc.exe"
            && psi.ArgumentList.Count == 1
            && psi.ArgumentList[0] == file.Value));
    }

    [Fact]
    public void Launch_UnmappedFormat_FallsBackToOsAssociation()
    {
        // arrange
        var launcher = NewLauncher(
        [
            new PlayerConfig
            {
                Name = "vlc",
                ExecutablePath = @"C:\Players\vlc.exe",
                Formats = ["mkv"],
            },
        ]);
        var file = new LibraryPath(@"C:\library\film.avi");
        var format = new MediaFormat("avi");

        // act
        launcher.Launch(file, format);

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == file.Value
            && psi.ArgumentList.Count == 0
            && psi.UseShellExecute));
    }

    [Fact]
    public void Launch_HostileFileName_KeepsShellCommandLineEmpty()
    {
        // arrange
        var launcher = NewLauncher(
        [
            new PlayerConfig
            {
                Name = "vlc",
                ExecutablePath = @"C:\Players\vlc.exe",
                Formats = ["mkv"],
            },
        ]);
        var hostile = new LibraryPath(@"C:\library\my movie & del evidence.mkv");
        ProcessStartInfo? captured = null;
        starter.When(s => s.Start(Arg.Any<ProcessStartInfo>()))
            .Do(call => captured = call.Arg<ProcessStartInfo>());

        // act
        launcher.Launch(hostile, new MediaFormat("mkv"));

        // assert
        captured.ShouldNotBeNull();
        captured.FileName.ShouldBe(@"C:\Players\vlc.exe");
        captured.ArgumentList.Count.ShouldBe(1);
        captured.ArgumentList[0].ShouldBe(hostile.Value);
        captured.Arguments.ShouldBeEmpty();
        captured.UseShellExecute.ShouldBeTrue();
    }

    private ProcessMediaPlayerLauncher NewLauncher(PlayerConfig[] players) =>
        new(Microsoft.Extensions.Options.Options.Create(new PlaybackOptions { Players = players }), starter);
}
