using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace LamuFlix.Infrastructure.Adapters;

public static class MetadataProviderServiceCollectionExtensions
{
    public static IServiceCollection AddMetadataProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHttpClient<OmdbMetadataProvider>((provider, client) =>
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<OmdbOptions>>().Value.BaseUrl))
            .AddStandardResilienceHandler()
            .Configure((target, provider) => Configure(
                target,
                provider.GetRequiredService<IOptions<MetadataProviderResilienceOptions>>().Value));

        services
            .AddHttpClient<TmdbMetadataProvider>((provider, client) =>
                client.BaseAddress = NormalizeBaseUrl(provider.GetRequiredService<IOptions<TmdbOptions>>().Value.BaseUrl))
            .AddStandardResilienceHandler()
            .Configure((target, provider) => Configure(
                target,
                provider.GetRequiredService<IOptions<MetadataProviderResilienceOptions>>().Value));

        services.AddKeyedTransient<IMetadataProvider>(
            TelemetryConstants.OmdbProviderName,
            (provider, _) => provider.GetRequiredService<OmdbMetadataProvider>());
        services.AddKeyedTransient<IMetadataProvider>(
            TelemetryConstants.TmdbProviderName,
            (provider, _) => provider.GetRequiredService<TmdbMetadataProvider>());

        services.AddTransient<IMetadataProvider>(provider =>
        {
            var chain = new List<IMetadataProvider>
            {
                provider.GetRequiredKeyedService<IMetadataProvider>(TelemetryConstants.OmdbProviderName),
            };

            if (provider.GetRequiredService<IOptions<TmdbOptions>>().Value.Enabled)
            {
                chain.Add(provider.GetRequiredKeyedService<IMetadataProvider>(TelemetryConstants.TmdbProviderName));
            }

            return new ChainedMetadataProvider(chain);
        });

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<MetadataProviderHealthState>();
        services.TryAddSingleton<SecondaryMetadataProviderHealthState>();

        // The matching AddCheck lives in MetadataProviderServiceCollectionExtensions; AddHealthChecks() is in ServiceDefaults.
        services.AddHealthChecks()
            .AddCheck<MetadataProviderHealthCheck>("metadata-provider")
            .AddCheck<SecondaryMetadataProviderHealthCheck>("metadata-provider-tmdb");

        return services;
    }

    private static Uri NormalizeBaseUrl(string baseUrl) =>
        new(baseUrl.EndsWith('/') ? baseUrl : string.Concat(baseUrl, "/"));

    private static void Configure(HttpStandardResilienceOptions target, MetadataProviderResilienceOptions options)
    {
        target.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
        target.Retry.Delay = options.BaseDelay;
        target.Retry.BackoffType = DelayBackoffType.Exponential;
        target.Retry.UseJitter = true;
        target.Retry.ShouldHandle = static arguments =>
            new ValueTask<bool>(ShouldRetry(arguments.Outcome));
        target.Retry.ShouldRetryAfterHeader = true;

        target.AttemptTimeout.Timeout = options.AttemptTimeout;
        target.TotalRequestTimeout.Timeout = options.TotalTimeout;

        target.CircuitBreaker.FailureRatio = options.FailureRatio;
        target.CircuitBreaker.SamplingDuration = options.SamplingDuration;
        target.CircuitBreaker.MinimumThroughput = options.MinimumThroughput;
        target.CircuitBreaker.BreakDuration = options.BreakDuration;
        target.CircuitBreaker.ShouldHandle = static arguments =>
            new ValueTask<bool>(ShouldRetry(arguments.Outcome));
    }

    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome) =>
        IsRetryableFailure(outcome.Exception) || IsRetryableStatus(outcome.Result);

    private static bool IsRetryableFailure(Exception? exception) =>
        exception is HttpRequestException or TimeoutRejectedException;

    private static bool IsRetryableStatus(HttpResponseMessage? response) =>
        IsTransient(response) || IsThrottled(response);

    private static bool IsTransient(HttpResponseMessage? response) =>
        response?.StatusCode >= HttpStatusCode.InternalServerError;

    private static bool IsThrottled(HttpResponseMessage? response) =>
        response?.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
}