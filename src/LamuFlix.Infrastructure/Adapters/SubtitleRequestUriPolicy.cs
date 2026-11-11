using System;
using System.Collections.Generic;

namespace LamuFlix.Infrastructure.Adapters;

internal static class SubtitleRequestUriPolicy
{
    public static bool IsPermitted(Uri? uri, IReadOnlyList<string> allowedHosts)
    {
        if (uri is null || !uri.IsAbsoluteUri || allowedHosts.Count == 0)
        {
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !(uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback))
        {
            return false;
        }

        return IsAllowedHost(uri.Host, allowedHosts);
    }

    public static bool IsAllowedHost(string host, IReadOnlyList<string> allowedHosts)
    {
        foreach (var allowed in allowedHosts)
        {
            if (!string.IsNullOrWhiteSpace(allowed) && string.Equals(allowed.Trim(), host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

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
