using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Playback;
using NSubstitute;

namespace LamuFlix.UnitTests.Playback;

public sealed class ProcessMediaPlayerLauncherTests
{
    private const string PlayerPath = "/players/vlc.exe";
    private const string SelectedFile = "/library/film.mkv";

    private readonly IProcessStarter starter = Substitute.For<IProcessStarter>();

    [Fact]
    public void EnsureEnabled_EnabledLauncher_ReturnsWithoutStartingProcess()
    {
        // arrange
        var launcher = NewLauncher([], ExistingFiles(SelectedFile));

        // act
        var result = Record.Exception(launcher.EnsureEnabled);

        // assert
        result.ShouldBeNull();
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public void Launch_MatchingFormat_StartsConfiguredPlayerWithFileAsSingleArgument()
    {
        // arrange
        var launcher = NewLauncher(
            [
                new PlayerConfig
                {
                    Name = "vlc",
                    ExecutablePath = PlayerPath,
                    Formats = ["mkv", ".mp4"],
                },
            ],
            ExistingFiles(SelectedFile));
        var file = new LibraryPath(SelectedFile);
        var format = new MediaFormat("mkv");

        // act
        launcher.Launch(file, format);

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == PlayerPath
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
                    ExecutablePath = PlayerPath,
                    Formats = [".MKV"],
                },
            ],
            ExistingFiles(SelectedFile));
        var file = new LibraryPath(SelectedFile);
        var format = new MediaFormat("Mkv");

        // act
        launcher.Launch(file, format);

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == PlayerPath
            && psi.ArgumentList.Count == 1
            && psi.ArgumentList[0] == file.Value));
    }

    [Fact]
    public void Launch_SelectedFileMissing_ThrowsParameterlessNotFoundWithoutStartingProcessOrLeakingPath()
    {
        // arrange
        var launcher = NewLauncher(Players("mkv"), ExistingFiles("/library/other.mkv"));
        var file = new LibraryPath(SelectedFile);

        // act
        var exception = Should.Throw<NotFoundException>(() => launcher.Launch(file, new MediaFormat("mkv")));

        // assert
        exception.Message.ShouldBe("Not found.");
        Describe(exception).ShouldNotContain(SelectedFile);
        Describe(exception).ShouldNotContain("film");
        Describe(exception).ShouldNotContain(PlayerPath);
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Theory]
    [InlineData("lnk")]
    [InlineData("bat")]
    [InlineData("cmd")]
    [InlineData("msi")]
    [InlineData("scr")]
    [InlineData("avi")]
    public void Launch_UnmappedFormat_ThrowsValidationExceptionWithFormatLevelDetailWithoutStartingProcess(
        string extension)
    {
        // arrange
        var launcher = NewLauncher(Players("mkv"), ExistingFiles($"/library/film.{extension}"));
        var file = new LibraryPath($"/library/film.{extension}");
        var format = new MediaFormat(extension);

        // act
        var exception = Should.Throw<ValidationException>(() => launcher.Launch(file, format));

        // assert
        exception.Errors.Keys.ShouldContain("format");
        exception.Errors["format"].ShouldHaveSingleItem().ShouldContain(extension);
        Describe(exception).ShouldNotContain($"/library/film.{extension}");
        Describe(exception).ShouldNotContain("film.");
        Describe(exception).ShouldNotContain(PlayerPath);
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public void Launch_MissingSelectedFileAndUnmappedFormat_ThrowsNotFoundBecauseExistenceWins()
    {
        // arrange
        var launcher = NewLauncher(Players("mkv"), ExistingFiles("/library/other.mkv"));
        var file = new LibraryPath("/library/film.avi");

        // act
        var exception = Record.Exception(() => launcher.Launch(file, new MediaFormat("avi")));

        // assert
        exception.ShouldBeOfType<NotFoundException>();
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public void Launch_HostileFileName_KeepsShellCommandLineEmpty()
    {
        // arrange
        var hostileValue = "/library/my movie & del evidence.mkv";
        var launcher = NewLauncher(Players("mkv"), ExistingFiles(hostileValue));
        var hostile = new LibraryPath(hostileValue);
        ProcessStartInfo? captured = null;
        starter.When(s => s.Start(Arg.Any<ProcessStartInfo>()))
            .Do(call => captured = call.Arg<ProcessStartInfo>());

        // act
        launcher.Launch(hostile, new MediaFormat("mkv"));

        // assert
        captured.ShouldNotBeNull();
        captured.FileName.ShouldBe(PlayerPath);
        captured.ArgumentList.Count.ShouldBe(1);
        captured.ArgumentList[0].ShouldBe(hostile.Value);
        captured.Arguments.ShouldBeEmpty();
        captured.UseShellExecute.ShouldBeTrue();
    }

    [Fact]
    public void Launch_ProcessStarterFails_PropagatesWithoutTranslation()
    {
        // arrange
        var launcher = NewLauncher(Players("mkv"), ExistingFiles(SelectedFile));
        var failure = new InvalidOperationException("player could not be started");
        starter.When(s => s.Start(Arg.Any<ProcessStartInfo>())).Throw(failure);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => launcher.Launch(new LibraryPath(SelectedFile), new MediaFormat("mkv")));

        // assert
        exception.ShouldBeSameAs(failure);
    }

    private static string Describe(Exception exception) =>
        string.Join(" ", exception.Message, exception.ToString());

    private static PlayerConfig[] Players(params string[] formats) =>
    [
        new PlayerConfig
        {
            Name = "vlc",
            ExecutablePath = PlayerPath,
            Formats = formats,
        },
    ];

    private static IFileSystem ExistingFiles(params string[] paths) =>
        new MockFileSystem(paths.ToDictionary<string, string, MockFileData>(
            path => path,
            _ => new MockFileData("video")));

    private ProcessMediaPlayerLauncher NewLauncher(PlayerConfig[] players, IFileSystem fileSystem) =>
        new(Microsoft.Extensions.Options.Options.Create(new PlaybackOptions { Players = players }), starter, fileSystem);
}
