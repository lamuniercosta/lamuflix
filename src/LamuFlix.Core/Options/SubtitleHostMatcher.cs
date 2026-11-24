using System;
using System.Collections.Generic;

namespace LamuFlix.Core.Options;

public static class SubtitleHostMatcher
{
    public static bool IsListed(string host, IReadOnlyList<string> allowedHosts)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(allowedHosts);

        var candidate = host.Trim();
        if (candidate.Length == 0)
        {
            return false;
        }

        foreach (var entry in allowedHosts)
        {
            if (!string.IsNullOrWhiteSpace(entry) && string.Equals(entry.Trim(), candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsBareHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.EndsWith('.'))
        {
            return false;
        }

        return Uri.TryCreate("https://" + trimmed + "/", UriKind.Absolute, out var uri)
            && IsBareHostUri(uri, trimmed);
    }

    private static bool IsBareHostUri(Uri uri, string trimmed) =>
        string.Equals(uri.Host, trimmed, StringComparison.OrdinalIgnoreCase) && HasNoExtras(uri);

    private static bool HasNoExtras(Uri uri) =>
        uri.UserInfo.Length == 0
        && uri.IsDefaultPort
        && uri.AbsolutePath == "/"
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0;
}
