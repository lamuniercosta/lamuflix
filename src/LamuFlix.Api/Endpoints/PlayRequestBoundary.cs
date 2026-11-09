using System;
using System.Collections.Generic;
using System.Net;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Http;

namespace LamuFlix.Api.Endpoints;

internal static class PlayRequestBoundary
{
    private const string RefusalMessage =
        "Local playback requests must arrive from a proven loopback peer without proxy provenance headers.";

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
            || HasProvenanceHeader(httpContext.Request.Headers))
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
}
