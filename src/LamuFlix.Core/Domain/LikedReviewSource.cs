using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record LikedReviewSource(
    MovieId MovieId,
    string Title,
    ImmutableDictionary<string, double> Genres,
    ImmutableDictionary<string, double> Cast,
    ImmutableDictionary<string, double> Directors)
{
    public static LikedReviewSource From(MovieId movieId, string title, ReviewProfileChannels channels)
    {
        ArgumentNullException.ThrowIfNull(channels);

        return new LikedReviewSource(movieId, title, channels.Genres, channels.Cast, channels.Directors);
    }
}
