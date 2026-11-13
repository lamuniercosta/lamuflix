using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleLookupRequest(
    string? MovieHash,
    long FileSize,
    string? Title,
    IReadOnlyList<string> PreferredLanguages);

public static class SubtitleLookup
{
    public static async Task<SubtitleCandidate?> FindAsync(
        ISubtitleProvider provider,
        SubtitleLookupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrWhiteSpace(request.MovieHash))
        {
            var hashResult = await provider.SearchAsync(
                new SubtitleSearch(request.MovieHash, request.FileSize, Title: null, request.PreferredLanguages),
                cancellationToken);
            if (SubtitleSelection.Select(hashResult.Candidates, request.PreferredLanguages) is { } hashMatch)
            {
                return hashMatch;
            }
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return null;
        }

        var titleResult = await provider.SearchAsync(
            new SubtitleSearch(MovieHash: null, request.FileSize, request.Title, request.PreferredLanguages),
            cancellationToken);
        return SubtitleSelection.Select(titleResult.Candidates, request.PreferredLanguages);
    }
}
