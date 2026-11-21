using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;

namespace LamuFlix.UnitTests.Persistence;

internal static class TasteProfileCacheBudgetFixture
{
    private const int GenreEntries = 14;

    private const int PlaybackEntryFloors = 4;

    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    internal static CachedProfile Envelope(int playbackEntryCount) =>
        new(Profile(playbackEntryCount), 7, CreatedAt);

    internal static TasteProfile Profile(int playbackEntryCount)
    {
        var genres = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < GenreEntries; index++)
        {
            genres[string.Concat("Genre ", index.ToString("D2", CultureInfo.InvariantCulture))] = 0.05 + (index / 1000.0);
        }

        var playback = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<DateTimeOffset>>();
        for (var index = 0; index < playbackEntryCount; index++)
        {
            playback[new MovieId(index + 1)] =
            [
                CreatedAt.AddDays(-(index % PlaybackEntryFloors)),
                CreatedAt.AddDays(-(index % (PlaybackEntryFloors * 30))),
            ];
        }

        return new TasteProfile(genres.ToImmutable())
        {
            PlaybackByMovieId = playback.ToImmutable(),
        };
    }

    internal static IReadOnlyList<int> PlaybackEntryCounts => [4, 145, 13500];
}
