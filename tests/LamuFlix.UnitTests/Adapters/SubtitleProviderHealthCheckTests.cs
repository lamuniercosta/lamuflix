using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests.Adapters;

public sealed class SubtitleProviderHealthCheckTests
{
    private const string SentinelKey = "sentinel-subtitle-api-key";

    [Fact]
    public async Task CheckHealthAsync_DefaultConfiguration_ReportsDisabledOrUnconfigured()
    {
        // arrange
        var healthCheck = CreateHealthCheck(new SubtitlesOptions(), new SubtitleProviderOptions());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        AcquisitionState(result).ShouldBe(TelemetryConstants.SubtitleDisabledOrUnconfiguredState);
        (result.Description ?? string.Empty).ShouldNotContain(SentinelKey);
    }

    [Fact]
    public async Task CheckHealthAsync_SaveEnabledButNoLanguages_ReportsDisabledOrUnconfigured()
    {
        // arrange
        var healthCheck = CreateHealthCheck(
            new SubtitlesOptions { SaveEnabled = true },
            ConfiguredProvider());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        AcquisitionState(result).ShouldBe(TelemetryConstants.SubtitleDisabledOrUnconfiguredState);
    }

    [Fact]
    public async Task CheckHealthAsync_EnabledWithMissingApiKey_ReportsDisabledOrUnconfigured()
    {
        // arrange
        var provider = ConfiguredProvider() with { ApiKey = string.Empty };
        var healthCheck = CreateHealthCheck(EnabledSubtitles(), provider);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        AcquisitionState(result).ShouldBe(TelemetryConstants.SubtitleDisabledOrUnconfiguredState);
    }

    [Theory]
    [InlineData("UserAgent")]
    [InlineData("BaseUrl")]
    public async Task CheckHealthAsync_EnabledWithBlankCredentialField_ReportsDisabledOrUnconfigured(string field)
    {
        // arrange
        var provider = ConfiguredProvider();
        provider = field switch
        {
            "UserAgent" => provider with { UserAgent = "  " },
            "BaseUrl" => provider with { BaseUrl = string.Empty },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown credential field."),
        };
        var healthCheck = CreateHealthCheck(EnabledSubtitles(), provider);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        AcquisitionState(result).ShouldBe(TelemetryConstants.SubtitleDisabledOrUnconfiguredState);
    }

    [Theory]
    [InlineData("DownloadAllowedHosts")]
    [InlineData("MaxDownloadBytes")]
    [InlineData("RequestTimeout")]
    public async Task CheckHealthAsync_EnabledWithMissingBound_ReportsDisabledOrUnconfigured(string field)
    {
        // arrange
        var provider = ConfiguredProvider();
        provider = field switch
        {
            "DownloadAllowedHosts" => provider with { DownloadAllowedHosts = [] },
            "MaxDownloadBytes" => provider with { MaxDownloadBytes = 0 },
            "RequestTimeout" => provider with { RequestTimeout = TimeSpan.Zero },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown bound field."),
        };
        var healthCheck = CreateHealthCheck(EnabledSubtitles(), provider);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        AcquisitionState(result).ShouldBe(TelemetryConstants.SubtitleDisabledOrUnconfiguredState);
    }

    [Fact]
    public async Task CheckHealthAsync_FullyConfigured_ReportsReadyAndNeverUnhealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledSubtitles(), ConfiguredProvider());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        AcquisitionState(result).ShouldBe(TelemetryConstants.SubtitleReadyState);
    }

    [Fact]
    public async Task CheckHealthAsync_MetadataNeverLeaksTheApiKey()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledSubtitles(), ConfiguredProvider());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        var serialized = string.Join(
            '|',
            result.Data.Select(entry => $"{entry.Key}={entry.Value}"));
        serialized.ShouldNotContain(SentinelKey);
        (result.Description ?? string.Empty).ShouldNotContain(SentinelKey);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledSubtitles(), ConfiguredProvider());

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => healthCheck.CheckHealthAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    private static string? AcquisitionState(HealthCheckResult result) =>
        result.Data.TryGetValue(TelemetryConstants.SubtitleAcquisitionStateTagName, out var value)
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract // health data may carry a null value at runtime
            ? value?.ToString()
            : null;

    private static SubtitlesOptions EnabledSubtitles() =>
        new() { SaveEnabled = true, PreferredLanguages = ["en"] };

    private static SubtitleProviderOptions ConfiguredProvider() =>
        new()
        {
            ApiKey = SentinelKey,
            UserAgent = "LamuFlix/test",
            BaseUrl = "https://api.opensubtitles.com",
            DownloadAllowedHosts = ["dl.opensubtitles.org"],
            MaxDownloadBytes = 1024,
            RequestTimeout = TimeSpan.FromSeconds(5),
        };

    private static IHealthCheck CreateHealthCheck(SubtitlesOptions subtitles, SubtitleProviderOptions provider) =>
        new SubtitleProviderHealthCheck(
            Microsoft.Extensions.Options.Options.Create(subtitles),
            Microsoft.Extensions.Options.Options.Create(provider));
}
