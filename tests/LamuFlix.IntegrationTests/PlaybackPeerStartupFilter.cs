using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LamuFlix.IntegrationTests;

internal sealed class PlaybackPeerStartupFilter : IStartupFilter
{
    internal const string PeerHeader = "X-Test-Remote-Peer";
    internal const string NullPeerValue = "none";
    internal const string ProvenanceNameHeader = "X-Test-Provenance-Name";
    internal const string ProvenanceValueHeader = "X-Test-Provenance-Value";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return app =>
        {
            app.Use(static (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = ResolvePeer(context.Request.Headers);
                InjectProvenance(context);
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    private static void InjectProvenance(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.TryGetValue(ProvenanceNameHeader, out var name) || string.IsNullOrEmpty(name))
        {
            return;
        }

        var value = headers.TryGetValue(ProvenanceValueHeader, out var supplied) ? supplied.ToString() : string.Empty;
        headers.Remove(ProvenanceNameHeader);
        headers.Remove(ProvenanceValueHeader);
        headers[name.ToString()] = new StringValues(value);
    }

    private static IPAddress? ResolvePeer(IHeaderDictionary headers) =>
        headers.TryGetValue(PeerHeader, out var value)
            ? ParsePeer(value.ToString())
            : IPAddress.Loopback;

    private static IPAddress? ParsePeer(string value) =>
        string.Equals(value, NullPeerValue, StringComparison.OrdinalIgnoreCase)
            ? null
            : IPAddress.TryParse(value, out var peer) ? peer : null;
}
