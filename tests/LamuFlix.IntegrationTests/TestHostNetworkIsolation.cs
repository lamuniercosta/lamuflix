using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.IntegrationTests;

internal static class TestHostNetworkIsolation
{
    public const string BlockedRequestMessagePrefix = "Test host blocked a request to a non-loopback host: ";

    private const string UserSecretsFileName = "secrets.json";
    private const string UnreachableBaseUrl = "https://example.invalid/";
    private const string RelativeUriHost = "(relative uri)";

    public static IReadOnlyList<KeyValuePair<string, string?>> SafeProviderSettings { get; } =
    [
        new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}", UnreachableBaseUrl),
        new($"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}", "false"),
        new($"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}", UnreachableBaseUrl),
        new($"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}", string.Empty),
        new($"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.BaseUrl)}", string.Empty),
        new($"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}", string.Empty),
    ];

    public static void Apply(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureAppConfiguration(static (_, configuration) => RemoveUserSecrets(configuration));
        builder.ConfigureTestServices(BlockNonLoopbackHttp);
    }

    public static bool IsUserSecretsSource(IConfigurationSource source) =>
        source is JsonConfigurationSource { Path: { } path }
        && path.EndsWith(UserSecretsFileName, StringComparison.OrdinalIgnoreCase);

    private static void RemoveUserSecrets(IConfigurationBuilder configuration)
    {
        for (var index = configuration.Sources.Count - 1; index >= 0; index--)
        {
            if (IsUserSecretsSource(configuration.Sources[index]))
            {
                configuration.Sources.RemoveAt(index);
            }
        }
    }

    private static void BlockNonLoopbackHttp(IServiceCollection services) =>
        services.ConfigureHttpClientDefaults(static client =>
            client.AddHttpMessageHandler(static () => new LoopbackOnlyHandler()));

    private static bool IsLoopback(HttpRequestMessage request) =>
        request.RequestUri is { IsAbsoluteUri: true, IsLoopback: true };

    private static HttpRequestException Blocked(HttpRequestMessage request) =>
        new(BlockedRequestMessagePrefix + DescribeHost(request.RequestUri));

    private static string DescribeHost(Uri? uri) =>
        uri is { IsAbsoluteUri: true } ? uri.Host : RelativeUriHost;

    private sealed class LoopbackOnlyHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return IsLoopback(request)
                ? base.SendAsync(request, cancellationToken)
                : Task.FromException<HttpResponseMessage>(Blocked(request));
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return IsLoopback(request) ? base.Send(request, cancellationToken) : throw Blocked(request);
        }
    }
}
