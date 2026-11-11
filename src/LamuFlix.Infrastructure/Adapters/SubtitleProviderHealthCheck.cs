using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class SubtitleProviderHealthCheck(
    IOptions<SubtitlesOptions> subtitlesOptions,
    IOptions<SubtitleProviderOptions> providerOptions) : IHealthCheck
{
    private const string ReadyDescription =
        "Subtitle acquisition settings are present; provider reachability is not asserted.";

    private const string DisabledOrUnconfiguredDescription =
        "Subtitle acquisition is disabled or unconfigured; existing sidecar serving is unaffected.";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ready = IsReady();
        var state = ready ? TelemetryConstants.SubtitleReadyState : TelemetryConstants.SubtitleDisabledOrUnconfiguredState;
        var description = ready ? ReadyDescription : DisabledOrUnconfiguredDescription;
        return Task.FromResult(HealthCheckResult.Healthy(
            description,
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [TelemetryConstants.SubtitleAcquisitionStateTagName] = state,
            }));
    }

    private bool IsReady() =>
        HasSubtitlesConfigured(subtitlesOptions.Value) && HasProviderConfigured(providerOptions.Value);

    private static bool HasSubtitlesConfigured(SubtitlesOptions subtitles) =>
        subtitles.SaveEnabled && subtitles.PreferredLanguages.Length > 0;

    private static bool HasProviderConfigured(SubtitleProviderOptions provider) =>
        HasCredentials(provider) && HasBounds(provider);

    private static bool HasCredentials(SubtitleProviderOptions provider) =>
        !string.IsNullOrWhiteSpace(provider.ApiKey)
        && !string.IsNullOrWhiteSpace(provider.UserAgent)
        && !string.IsNullOrWhiteSpace(provider.BaseUrl)
        && provider.DownloadAllowedHosts.Length > 0;

    private static bool HasBounds(SubtitleProviderOptions provider) =>
        provider.MaxDownloadBytes > 0 && provider.RequestTimeout > TimeSpan.Zero;
}
