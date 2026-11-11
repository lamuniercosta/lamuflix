using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class SubtitleProviderOptionsValidator : IValidateOptions<SubtitleProviderOptions>
{
    private const string BaseUrlFailure =
        "The BaseUrl field must be an absolute https URL, or http on a loopback host.";

    public ValidateOptionsResult Validate(string? name, SubtitleProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        AddCredentialFailures(options, failures);
        AddHostFailures(options, failures);
        AddBoundFailures(options, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddCredentialFailures(SubtitleProviderOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add($"{Field(nameof(SubtitleProviderOptions.ApiKey))} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.UserAgent))
        {
            failures.Add($"{Field(nameof(SubtitleProviderOptions.UserAgent))} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !IsHttpsOrLoopbackHttp(options.BaseUrl))
        {
            failures.Add($"{Field(nameof(SubtitleProviderOptions.BaseUrl))} {BaseUrlFailure}");
        }
    }

    private static void AddHostFailures(SubtitleProviderOptions options, List<string> failures)
    {
        if (options.DownloadAllowedHosts.Length == 0)
        {
            failures.Add($"{Field(nameof(SubtitleProviderOptions.DownloadAllowedHosts))} must list at least one allowed download host.");
        }

        for (var index = 0; index < options.DownloadAllowedHosts.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(options.DownloadAllowedHosts[index]))
            {
                failures.Add($"{Field(nameof(SubtitleProviderOptions.DownloadAllowedHosts))}[{index}] must not be blank.");
            }
        }
    }

    private static void AddBoundFailures(SubtitleProviderOptions options, List<string> failures)
    {
        if (options.MaxDownloadBytes <= 0)
        {
            failures.Add($"{Field(nameof(SubtitleProviderOptions.MaxDownloadBytes))} must be greater than zero.");
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            failures.Add($"{Field(nameof(SubtitleProviderOptions.RequestTimeout))} must be greater than zero.");
        }
    }

    private static string Field(string name) => $"{SubtitleProviderOptions.SectionName}:{name}";

    private static bool IsHttpsOrLoopbackHttp(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri)
        && (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback));
}
