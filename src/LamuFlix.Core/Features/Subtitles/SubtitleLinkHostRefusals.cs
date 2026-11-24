using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class SubtitleLinkHostRefusals
{
    private string? _refusedHost;

    public bool TryRecord(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        return Interlocked.CompareExchange(ref _refusedHost, host, null) is null;
    }

    public bool TryGetRefused([NotNullWhen(true)] out string? host)
    {
        host = Volatile.Read(ref _refusedHost);
        return host is not null;
    }
}
