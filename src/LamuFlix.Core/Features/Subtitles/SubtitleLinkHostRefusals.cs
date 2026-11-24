using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace LamuFlix.Core.Features.Subtitles;

// Process-lifetime record of a download link host the provider served that DownloadAllowedHosts refuses.
// Once recorded, search and download stay disabled until the API restarts.
public sealed class SubtitleLinkHostRefusals
{
    private string? _refusedHost;

    public void Record(string host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Volatile.Write(ref _refusedHost, host);
    }

    public bool TryGetRefused([NotNullWhen(true)] out string? host)
    {
        host = Volatile.Read(ref _refusedHost);
        return host is not null;
    }
}
