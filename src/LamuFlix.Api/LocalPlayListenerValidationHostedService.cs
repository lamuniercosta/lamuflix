using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api;

public sealed class LocalPlayListenerValidationHostedService(
    IOptions<FeatureOptions> features,
    IConfiguration configuration,
    IServer server) : IHostedService
{
    private const string KestrelSectionName = "Kestrel";
    private const string KestrelEndpointsSectionName = "Endpoints";
    private const string KestrelUrlKey = "Url";
    public const string DocumentedDefaultAddress = "http://localhost:5000";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!features.Value.LocalPlay)
        {
            return Task.CompletedTask;
        }

        var observed = ObserveListeners();
        if (!observed.All(static listener => listener.IsProvenLoopback))
        {
            throw new InvalidOperationException(RefusalMessage(observed));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IReadOnlyList<LocalPlayListenerClassification> ObserveListeners()
    {
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        var configured = ObserveConfiguredListeners(addressFeature);
        var reported = ClassifyAll(addressFeature?.Addresses);
        return [.. configured, .. reported];
    }

    private IReadOnlyList<LocalPlayListenerClassification> ObserveConfiguredListeners(IServerAddressesFeature? feature)
    {
        var urlFamily = LocalPlayListenerAddressParser.Split(configuration[WebHostDefaults.ServerUrlsKey]);
        var kestrelEndpoints = ReadKestrelEndpointUrls();

        if (kestrelEndpoints.Count > 0)
        {
            return ClassifyAll(PrefersHostingUrls(feature) && urlFamily.Count > 0 ? urlFamily : kestrelEndpoints);
        }

        if (urlFamily.Count > 0)
        {
            return ClassifyAll(urlFamily);
        }

        var portsShorthand = ReadPortsShorthand();
        return portsShorthand.Count > 0
            ? ClassifyAll(portsShorthand)
            : ClassifyAll([DocumentedDefaultAddress]);
    }

    private bool PrefersHostingUrls(IServerAddressesFeature? feature)
    {
        if (feature is not null && feature.PreferHostingUrls)
        {
            return true;
        }

        var configured = configuration[WebHostDefaults.PreferHostingUrlsKey];
        return !string.IsNullOrWhiteSpace(configured)
            && bool.TryParse(configured, out var prefer)
            && prefer;
    }

    private IReadOnlyList<string> ReadKestrelEndpointUrls()
    {
        var endpoints = configuration.GetSection(KestrelSectionName).GetSection(KestrelEndpointsSectionName);
        return endpoints.Exists()
            ? [.. endpoints.GetChildren().Select(static endpoint => endpoint[KestrelUrlKey] ?? string.Empty)]
            : [];
    }

    private IReadOnlyList<string> ReadPortsShorthand() =>
    [
        .. PortAddress(configuration[WebHostDefaults.HttpPortsKey], Uri.UriSchemeHttp),
        .. PortAddress(configuration[WebHostDefaults.HttpsPortsKey], Uri.UriSchemeHttps),
    ];

    private static IEnumerable<string> PortAddress(string? ports, string scheme)
    {
        if (string.IsNullOrWhiteSpace(ports))
        {
            yield break;
        }

        foreach (var port in ports.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return $"{scheme}://*:{port}";
        }
    }

    private static IReadOnlyList<LocalPlayListenerClassification> ClassifyAll(IEnumerable<string>? addresses) =>
        [.. (addresses ?? []).Select(LocalPlayListenerAddressParser.Classify)];

    private static string RefusalMessage(IReadOnlyList<LocalPlayListenerClassification> listeners)
    {
        var offender = listeners.FirstOrDefault(static listener => !listener.IsProvenLoopback);
        return "Features:LocalPlay requires every effective listener to be proven loopback; "
            + $"refusing startup for '{offender.Address}' ({offender.Kind}).";
    }
}
