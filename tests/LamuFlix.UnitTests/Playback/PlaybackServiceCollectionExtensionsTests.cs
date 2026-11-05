using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            ["Playback:Players:0:ExecutablePath"] = @"C:\Players\vlc.exe",
            ["Playback:Players:0:Formats:0"] = "mkv",
        });
        services.AddLamuFlixPlayback();
        var starter = Substitute.For<IProcessStarter>();
        services.AddSingleton(starter);
        var file = new LibraryPath(@"C:\library\film.mkv");

        // act
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMediaPlayerLauncher>().Launch(file, new MediaFormat("mkv"));

        // assert
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(psi =>
            psi.FileName == @"C:\Players\vlc.exe"
            && psi.ArgumentList.Count == 1
            && psi.ArgumentList[0] == file.Value));
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
        services.AddLamuFlixOptions();
        return services;
    }
}
