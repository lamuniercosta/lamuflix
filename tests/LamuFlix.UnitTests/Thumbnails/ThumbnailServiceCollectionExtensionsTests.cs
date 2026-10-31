using System;
using System.Collections.Generic;
using System.IO;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.Thumbnails;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace LamuFlix.UnitTests.Thumbnails;

public sealed class ThumbnailServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLamuFlixThumbnails_RegistersStatelessSingletons()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var services = NewServices(temporary.Path, localPlay: false);

        // act
        services.AddLamuFlixThumbnails();

        // assert
        using var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IProcessRunner>();
        var generator = provider.GetRequiredService<IThumbnailGenerator>();
        runner.ShouldBeOfType<SystemProcessRunner>();
        generator.ShouldBeOfType<FfmpegThumbnailGenerator>();
        provider.GetRequiredService<IProcessRunner>().ShouldBeSameAs(runner);
        provider.GetRequiredService<IThumbnailGenerator>().ShouldBeSameAs(generator);
    }

    [Fact]
    public void AddLamuFlixThumbnails_DoesNoEagerDirectoryOrProcessWork()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var services = NewServices(temporary.Path, localPlay: false, thumbnailsEnabled: true);
        services.AddLamuFlixThumbnails();
        using var provider = services.BuildServiceProvider();

        // act
        _ = provider.GetRequiredService<IProcessRunner>();
        _ = provider.GetRequiredService<IThumbnailGenerator>();

        // assert
        Directory.Exists(Path.Combine(temporary.Path, "cache", "thumbnails")).ShouldBeFalse();
    }

    [Fact]
    public void AddLamuFlixThumbnails_RemainsAvailableWhenLocalPlayIsDisabled()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var services = NewServices(temporary.Path, localPlay: false);
        services.AddLamuFlixPlayback();
        services.AddLamuFlixThumbnails();

        // act
        using var provider = services.BuildServiceProvider();
        var launcher = provider.GetRequiredService<IMediaPlayerLauncher>();
        var generator = provider.GetRequiredService<IThumbnailGenerator>();

        // assert
        launcher.ShouldBeOfType<DisabledMediaPlayerLauncher>();
        generator.ShouldBeOfType<FfmpegThumbnailGenerator>();
    }

    private static ServiceCollection NewServices(
        string contentRootPath,
        bool localPlay,
        bool thumbnailsEnabled = false)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] =
                localPlay ? "true" : "false",
            [$"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.Enabled)}"] =
                thumbnailsEnabled ? "true" : "false",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(contentRootPath);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(environment);
        services.AddLamuFlixOptions();
        return services;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-thumbnails-composition-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
