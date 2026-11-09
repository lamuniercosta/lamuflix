using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LamuFlix.UnitTests.Playback;

public sealed class PlaybackServiceCollectionExtensionsTests
{
    private const string PlayerPath = "/players/vlc.exe";
    private const string SelectedFile = "/library/film.mkv";

    [Theory]
    [InlineData(true, typeof(ProcessMediaPlayerLauncher))]
    [InlineData(false, typeof(DisabledMediaPlayerLauncher))]
    public void AddLamuFlixPlayback_ResolvesLauncherFromLocalPlayFlag(bool localPlay, Type expected)
    {
        // arrange
        var services = NewServices(localPlay, []);

        // act
        services.AddLamuFlixPlayback();

        // assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMediaPlayerLauncher>().ShouldBeOfType(expected);
    }

    [Fact]
    public void AddLamuFlixPlayback_EnabledLauncher_LaunchesConfiguredPlayerWithoutRealProcess()
    {
        // arrange
        var services = NewServices(true, new Dictionary<string, string?>
        {
            ["Playback:Players:0:Name"] = "vlc",
            ["Playback:Players:0:ExecutablePath"] = PlayerPath,
            ["Playback:Players:0:Formats:0"] = "mkv",
        });
        services.AddLamuFlixPlayback();
        var starter = Substitute.For<IProcessStarter>();
        services.AddSingleton(starter);
        var file = new LibraryPath(SelectedFile);

        // act
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMediaPlayerLauncher>().Launch(file, new MediaFormat("mkv"));

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == PlayerPath
            && psi.ArgumentList.Count == 1
            && psi.ArgumentList[0] == file.Value));
    }

    [Fact]
    public void AddLamuFlixPlayback_EnabledLauncher_ResolvesExistingFileSystemAndFailsNotFoundForMissingFile()
    {
        // arrange
        var services = NewServices(true, new Dictionary<string, string?>
        {
            ["Playback:Players:0:Name"] = "vlc",
            ["Playback:Players:0:ExecutablePath"] = PlayerPath,
            ["Playback:Players:0:Formats:0"] = "mkv",
        });
        services.AddLamuFlixPlayback();
        var starter = Substitute.For<IProcessStarter>();
        services.AddSingleton(starter);

        // act
        using var provider = services.BuildServiceProvider();
        var launcher = provider.GetRequiredService<IMediaPlayerLauncher>();
        var exception = Should.Throw<NotFoundException>(
            () => launcher.Launch(new LibraryPath("/library/absent.mkv"), new MediaFormat("mkv")));

        // assert
        exception.Message.ShouldBe("Not found.");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "LocalPlay is disabled in this environment.")]
    public void AddLamuFlixPlayback_ResolvedLauncher_GuardsEnsureEnabledFromLocalPlayFlag(bool localPlay, string? expectedMessage)
    {
        // arrange
        var services = NewServices(localPlay, []);
        services.AddLamuFlixPlayback();

        // act
        using var provider = services.BuildServiceProvider();
        var launcher = provider.GetRequiredService<IMediaPlayerLauncher>();
        var exception = Record.Exception(launcher.EnsureEnabled);

        // assert
        if (expectedMessage is null)
        {
            exception.ShouldBeNull();
            return;
        }

        exception.ShouldBeOfType<FeatureDisabledException>().Message.ShouldBe(expectedMessage);
    }

    private static ServiceCollection NewServices(bool localPlay, Dictionary<string, string?> values)
    {
        values[$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = localPlay ? "true" : "false";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IFileSystem>(new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [SelectedFile] = new MockFileData("video"),
        }));
        services.AddLamuFlixOptions();
        return services;
    }
}
