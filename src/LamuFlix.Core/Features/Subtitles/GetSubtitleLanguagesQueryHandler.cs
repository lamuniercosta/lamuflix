using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class GetSubtitleLanguagesQueryHandler(IOptions<SubtitlesOptions> subtitlesOptions)
    : IQueryHandler<GetSubtitleLanguagesQuery, GetSubtitleLanguagesResult>
{
    public Task<GetSubtitleLanguagesResult> HandleAsync(
        GetSubtitleLanguagesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var preferences = subtitlesOptions.Value.PreferredLanguages;
        var ordered = SupportedSubtitleLanguages.OrderedFor(preferences);
        var defaultLanguage = FirstPreference(preferences);

        return Task.FromResult(new GetSubtitleLanguagesResult(defaultLanguage, [.. ordered]));
    }

    private static string? FirstPreference(string[] preferences)
    {
        foreach (var preference in preferences)
        {
            if (SupportedSubtitleLanguages.TryResolve(preference, out var language))
            {
                return language.Code;
            }
        }

        return null;
    }
}
