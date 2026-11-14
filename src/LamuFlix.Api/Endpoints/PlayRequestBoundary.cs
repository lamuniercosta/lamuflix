using System;
using System.Collections.Generic;
using System.Net;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Http;

namespace LamuFlix.Api.Endpoints;

internal static class PlayRequestBoundary
{
    private const string RefusalMessage =
        "Local playback requests must arrive from a proven loopback peer, without proxy provenance headers, and from a local browser origin.";

    private const string OriginHeaderName = "Origin";
    private const string FetchSiteHeaderName = "Sec-Fetch-Site";
    private const string CrossSiteFetchSite = "cross-site";
    private const string OpaqueOrigin = "null";
    private const string LocalhostHost = "localhost";

    private static readonly IReadOnlyList<string> ProvenanceHeaders =
    [
        "Forwarded",
        "X-Forwarded-For",
        "X-Forwarded-Host",
        "X-Forwarded-Proto",
        "X-Real-IP",
    ];

    internal static void EnsureProvenLocalRequest(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (!IsProvenLoopbackPeer(httpContext.Connection.RemoteIpAddress)
            || HasProvenanceHeader(httpContext.Request.Headers)
            || IsCrossSiteBrowserRequest(httpContext.Request.Headers))
        {
            throw new FeatureDisabledException(RefusalMessage);
        }
    }

    private static bool IsProvenLoopbackPeer(IPAddress? peer) =>
        peer is not null && LocalPlayListenerAddressParser.IsProvenLoopback(peer);

    private static bool HasProvenanceHeader(IHeaderDictionary headers)
    {
        foreach (var header in ProvenanceHeaders)
        {
            if (headers.ContainsKey(header))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCrossSiteBrowserRequest(IHeaderDictionary headers) =>
        HasUnprovenOrigin(headers) || HasCrossSiteFetchMetadata(headers);

    private static bool HasUnprovenOrigin(IHeaderDictionary headers) =>
        headers.TryGetValue(OriginHeaderName, out var values)
        && (values.Count != 1 || !IsProvenLocalOrigin(values[0]));

    private static bool HasCrossSiteFetchMetadata(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(FetchSiteHeaderName, out var values))
        {
            return false;
        }

        foreach (var value in values)
        {
            if (string.Equals(value?.Trim(), CrossSiteFetchSite, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProvenLocalOrigin(string? origin)
    {
        var text = origin?.Trim();
        return !string.IsNullOrEmpty(text)
            && !string.Equals(text, OpaqueOrigin, StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && IsProvenLoopbackOrigin(uri);
    }

    private static bool IsProvenLoopbackOrigin(Uri origin) =>
        IsHttpScheme(origin.Scheme)
        && origin.UserInfo.Length == 0
        && IsProvenLoopbackHost(origin.Host);

    private static bool IsHttpScheme(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool IsProvenLoopbackHost(string host)
    {
        var candidate = host.Trim('[', ']');
        if (IPAddress.TryParse(candidate, out var address))
        {
            return LocalPlayListenerAddressParser.IsProvenLoopback(address);
        }

        return string.Equals(candidate, LocalhostHost, StringComparison.OrdinalIgnoreCase);
    }
}
