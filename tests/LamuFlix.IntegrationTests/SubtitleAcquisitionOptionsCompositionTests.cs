using System;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class SubtitleAcquisitionOptionsCompositionTests(ApiHostFactory factory)
    : IClassFixture<ApiHostFactory>
{
    [Fact]
    public void StartedComposition_BindsSaveEnabledFalseWithEmptyPreferences()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var subtitles = scope.ServiceProvider.GetRequiredService<IOptions<SubtitlesOptions>>().Value;

        // assert
        subtitles.SaveEnabled.ShouldBeFalse();
        subtitles.PreferredLanguages.ShouldBeEmpty();
    }

    [Fact]
    public void StartedComposition_SurfacesUnconfiguredAcquisitionAsAPrepareTimeFailureNotAStartupFailure()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var exception = Should.Throw<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<IOptions<SubtitleProviderOptions>>().Value);

        // assert
        exception.Message.ShouldContain(nameof(SubtitleProviderOptions.ApiKey));
        exception.Message.ShouldContain(nameof(SubtitleProviderOptions.MaxDownloadBytes));
        exception.Message.ShouldContain(nameof(SubtitleProviderOptions.RequestTimeout));
    }

    [Fact]
    public void StartedComposition_RegistersTheSubtitleProviderAdapterWithoutOpeningAcquisitionOptions()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var provider = scope.ServiceProvider.GetService<ISubtitleProvider>();
        var options = scope.ServiceProvider.GetService<IOptions<SubtitleProviderOptions>>();

        // assert
        provider.ShouldBeOfType<OpenSubtitlesSubtitleProvider>();
        options.ShouldNotBeNull();
    }

    [Fact]
    public void StartedComposition_RegistersAcquisitionValidatorsButNoStartupValidator()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var subtitlesValidators = scope.ServiceProvider.GetServices<IValidateOptions<SubtitlesOptions>>();
        var providerValidators = scope.ServiceProvider.GetServices<IValidateOptions<SubtitleProviderOptions>>();
        var startupValidators = scope.ServiceProvider.GetServices<IStartupValidator>().ToArray();

        // assert
        subtitlesValidators.ShouldContain(validator => validator is SubtitlesOptionsValidator);
        providerValidators.ShouldContain(validator => validator is SubtitleProviderOptionsValidator);
        startupValidators.ShouldNotContain(validator =>
            (validator.GetType().FullName ?? string.Empty).Contains("Subtitle", StringComparison.Ordinal));
    }
}
