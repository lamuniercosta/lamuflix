using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;

namespace LamuFlix.UnitTests.Persistence;

internal static class TasteProfileCacheBudgetFixture
{
    private const int PerSourceCastEntries = 56;

    private const int PerSourceDirectorEntries = 2;

    private const double AggregateCastPerSource = 37.5;

    private const int AggregateDirectorEntries = 325;

    private const int AggregateGenreEntries = 22;

    private const int DislikedCastEntries = 120;

    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    internal static CachedProfile Envelope(int likedSourceCount) =>
        new(Profile(likedSourceCount), 7, CreatedAt);

    internal static TasteProfile Profile(int likedSourceCount)
    {
        var genres = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < 14; index++)
        {
            genres[string.Concat("Genre ", index.ToString("D2", CultureInfo.InvariantCulture))] = 0.05 + (index / 1000.0);
        }

        return new TasteProfile(genres.ToImmutable(), Vector(384, 384f))
        {
            Liked = AggregateChannels(likedSourceCount),
            Disliked = ImmutableChannels(),
            LikedSources = Sources(likedSourceCount),
        };
    }

    private static ReviewProfileChannels AggregateChannels(int likedSourceCount)
    {
        var castEntries = (int)Math.Ceiling(AggregateCastPerSource * likedSourceCount);

        var cast = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < castEntries; index++)
        {
            cast[string.Concat(
                "Ensemble Performer ",
                index.ToString("D6", CultureInfo.InvariantCulture))] = index / (double)castEntries;
        }

        var directors = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < AggregateDirectorEntries; index++)
        {
            directors[string.Concat(
                "Ensemble Director ",
                index.ToString("D3", CultureInfo.InvariantCulture))] = index / (double)AggregateDirectorEntries;
        }

        var genres = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < AggregateGenreEntries; index++)
        {
            genres[string.Concat(
                "Ensemble Genre ",
                index.ToString("D2", CultureInfo.InvariantCulture))] = index / (double)AggregateGenreEntries;
        }

        return new ReviewProfileChannels(Vector(384, 17f), genres.ToImmutable(), cast.ToImmutable(), directors.ToImmutable());
    }

    private static ReviewProfileChannels ImmutableChannels()
    {
        var genres = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < 9; index++)
        {
            genres[string.Concat(
                "Disliked Genre ",
                index.ToString("D2", CultureInfo.InvariantCulture))] = index / 9.0;
        }

        var cast = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < DislikedCastEntries; index++)
        {
            cast[string.Concat(
                "Disliked Performer ",
                index.ToString("D3", CultureInfo.InvariantCulture))] = index / (double)DislikedCastEntries;
        }

        return new ReviewProfileChannels(
            Vector(384, 11f),
            genres.ToImmutable(),
            cast.ToImmutable(),
            ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase));
    }

    private static ImmutableArray<LikedReviewSource> Sources(int likedSourceCount)
    {
        var builder = ImmutableArray.CreateBuilder<LikedReviewSource>(likedSourceCount);

        for (var source = 0; source < likedSourceCount; source++)
        {
            var cast = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
            for (var member = 0; member < PerSourceCastEntries; member++)
            {
                cast[string.Concat(
                    "Perf ",
                    source.ToString("D4", CultureInfo.InvariantCulture),
                    " ",
                    member.ToString("D2", CultureInfo.InvariantCulture))] = 1.0;
            }

            var directors = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
            for (var director = 0; director < PerSourceDirectorEntries; director++)
            {
                directors[string.Concat(
                    "Dir ",
                    source.ToString("D4", CultureInfo.InvariantCulture),
                    " ",
                    director.ToString("D1", CultureInfo.InvariantCulture))] = 1.0;
            }

            var genres = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);
            for (var genre = 0; genre < 3; genre++)
            {
                genres[string.Concat(
                    "Genre ",
                    ((source + genre) % 14).ToString("D2", CultureInfo.InvariantCulture))] = 1.0;
            }

            builder.Add(new LikedReviewSource(
                new MovieId(source + 1),
                string.Concat("Liked source ", source.ToString("D4", CultureInfo.InvariantCulture)),
                genres.ToImmutable(),
                cast.ToImmutable(),
                directors.ToImmutable()));
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<float> Vector(int length, float modulus)
    {
        var builder = ImmutableArray.CreateBuilder<float>(length);

        for (var index = 0; index < length; index++)
        {
            builder.Add((index % (int)modulus) / modulus);
        }

        return builder.MoveToImmutable();
    }

    internal static IReadOnlyList<int> SourceCounts => [4, 145, 300];
}
