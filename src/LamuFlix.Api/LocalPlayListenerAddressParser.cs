using System;
using System.Collections.Generic;
using System.Net;

namespace LamuFlix.Api;

public enum LocalPlayListenerKind
{
    ProvenLoopback,
    Wildcard,
    NonLoopbackLiteral,
    Hostname,
    UnixSocket,
    NamedPipe,
    Unprovable,
}

public readonly record struct LocalPlayListenerClassification(LocalPlayListenerKind Kind, string Address)
{
    public bool IsProvenLoopback => Kind == LocalPlayListenerKind.ProvenLoopback;
}

public static class LocalPlayListenerAddressParser
{
    private const string LocalhostHost = "localhost";
    private const string PipeHost = "pipe";
    private const string UnixScheme = "unix";

    public static IReadOnlyList<string> Split(string? addressList)
    {
        var entries = new List<string>();
        if (string.IsNullOrWhiteSpace(addressList))
        {
            return entries;
        }

        foreach (var part in addressList.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            entries.Add(part);
        }

        return entries;
    }

    public static LocalPlayListenerClassification Classify(string? address)
    {
        var text = address?.Trim() ?? string.Empty;
        if (TryClassifyWithoutAuthority(text, out var early))
        {
            return early;
        }

        var authority = ReadAuthority(text);
        if (authority is null)
        {
            return new(LocalPlayListenerKind.Unprovable, text);
        }

        var host = ExtractHost(authority);
        return string.IsNullOrEmpty(host)
            ? new(LocalPlayListenerKind.Unprovable, text)
            : ClassifyHost(host, text);
    }

    private static bool TryClassifyWithoutAuthority(string text, out LocalPlayListenerClassification classification)
    {
        if (text.Length == 0)
        {
            classification = new(LocalPlayListenerKind.Unprovable, text);
            return true;
        }

        if (text.StartsWith(UnixScheme + ":", StringComparison.OrdinalIgnoreCase))
        {
            classification = new(LocalPlayListenerKind.UnixSocket, text);
            return true;
        }

        classification = default;
        return false;
    }

    public static bool IsProvenLoopback(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return IPAddress.IsLoopback(Normalize(address));
    }

    public static IPAddress Normalize(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private static string? ReadAuthority(string text)
    {
        var schemeEnd = text.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd <= 0)
        {
            return null;
        }

        var scheme = text[..schemeEnd];
        if (string.Equals(scheme, UnixScheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!IsHttpScheme(scheme))
        {
            return null;
        }

        var authority = text[(schemeEnd + 3)..];
        var pathStart = authority.IndexOf('/');
        return pathStart < 0 ? authority : authority[..pathStart];
    }

    private static bool IsHttpScheme(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static string? ExtractHost(string authority)
    {
        var target = authority;
        var userInfoEnd = target.LastIndexOf('@');
        if (userInfoEnd >= 0)
        {
            target = target[(userInfoEnd + 1)..];
        }

        if (target.Length == 0)
        {
            return null;
        }

        if (target[0] == '[')
        {
            var closingBracket = target.IndexOf(']');
            return closingBracket > 1 ? target[1..closingBracket] : null;
        }

        var portSeparator = target.IndexOf(':');
        return portSeparator < 0 ? target : target[..portSeparator];
    }

    private static LocalPlayListenerClassification ClassifyHost(string host, string text)
    {
        if (string.Equals(host, PipeHost, StringComparison.OrdinalIgnoreCase))
        {
            return new(LocalPlayListenerKind.NamedPipe, text);
        }

        if (string.Equals(host, UnixScheme, StringComparison.OrdinalIgnoreCase))
        {
            return new(LocalPlayListenerKind.UnixSocket, text);
        }

        if (IsWildcard(host))
        {
            return new(LocalPlayListenerKind.Wildcard, text);
        }

        return ClassifyLiteralOrHostname(host, text);
    }

    private static LocalPlayListenerClassification ClassifyLiteralOrHostname(string host, string text)
    {
        if (IPAddress.TryParse(host, out var address))
        {
            return IsProvenLoopback(address)
                ? new(LocalPlayListenerKind.ProvenLoopback, text)
                : new(LocalPlayListenerKind.NonLoopbackLiteral, text);
        }

        return string.Equals(host, LocalhostHost, StringComparison.OrdinalIgnoreCase)
            ? new(LocalPlayListenerKind.ProvenLoopback, text)
            : new(LocalPlayListenerKind.Hostname, text);
    }

    private static bool IsWildcard(string host)
    {
        if (host is "*" or "+")
        {
            return true;
        }

        return IPAddress.TryParse(host, out var address)
            && (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any));
    }
}
