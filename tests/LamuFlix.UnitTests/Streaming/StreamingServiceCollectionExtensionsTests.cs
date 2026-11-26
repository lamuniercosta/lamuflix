using System;
using System.IO;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Streaming;
using LamuFlix.Infrastructure.Thumbnails;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.Streaming;

public sealed class StreamingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLamuFlixStreaming_RegistersSingletonPortAndStartupValidation()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var services = NewServices(temporary.Path);

        // act
        services.AddLamuFlixStreaming();

        // assert
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IStreamConversionService>();
        service.ShouldBeOfType<StreamConversionService>();
        provider.GetRequiredService<IStreamConversionService>().ShouldBeSameAs(service);
        provider.GetServices<IValidateOptions<StreamConversionOptions>>()
            .ShouldContain(validator => validator is StreamConversionOptionsValidator);
    }

    [Fact]
    public void AddLamuFlixStreaming_UsesAssumedDefaultsWithoutFeatureConfiguration()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var services = NewServices(temporary.Path);
        services.AddLamuFlixStreaming();

        // act
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<StreamConversionOptions>>().Value;

        // assert
        options.MaxConcurrentConversions.ShouldBe(2);
        options.IdleExpiry.ShouldBe(TimeSpan.FromMinutes(60));
        options.ScratchSizeLimitBytes.ShouldBe(20L * 1024 * 1024 * 1024);
        options.ScratchDirectory.ShouldBe("cache/stream-conversion");
    }

    [Fact]
    public void StreamConversionOptionsValidator_RejectsNonPositiveLimitsAndBlankScratch()
    {
        // arrange
        var validator = new StreamConversionOptionsValidator();
        var options = new StreamConversionOptions
        {
            MaxConcurrentConversions = 0,
            IdleExpiry = TimeSpan.Zero,
            ScratchSizeLimitBytes = 0,
            ScratchDirectory = " ",
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Succeeded.ShouldBeFalse();
        (result.Failures ?? []).Count().ShouldBe(4);
    }

    private static ServiceCollection NewServices(string contentRootPath)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(contentRootPath);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(environment);
        services.AddLogging();
        services.AddSingleton(Substitute.For<IProcessRunner>());
        services.AddLamuFlixOptions();
        return services;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-streaming-di-" + Guid.NewGuid().ToString("N"));
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
