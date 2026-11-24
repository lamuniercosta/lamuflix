using System;
using System.Collections.Generic;
using LamuFlix.Core.Options;

namespace LamuFlix.Infrastructure.Adapters;

internal static class SubtitleRequestUriPolicy
{
    public static bool IsPermitted(Uri? uri, IReadOnlyList<string> allowedHosts)
    {
        if (uri is null || !uri.IsAbsoluteUri || allowedHosts.Count == 0)
        {
            return false;
        }

        return IsAllowedScheme(uri) && IsAllowedHost(uri.Host, allowedHosts);
    }

    private static bool IsAllowedScheme(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedHost(string host, IReadOnlyList<string> allowedHosts) =>
        SubtitleHostMatcher.IsListed(host, allowedHosts);

    public static Uri? TryBuildEndpoint(string baseUrl, string relativePath)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var root))
        {
            return null;
        }

        var normalized = root.AbsoluteUri.EndsWith('/') ? root.AbsoluteUri : string.Concat(root.AbsoluteUri, "/");
        return Uri.TryCreate(new Uri(normalized, UriKind.Absolute), relativePath, out var endpoint) ? endpoint : null;
    }
}
