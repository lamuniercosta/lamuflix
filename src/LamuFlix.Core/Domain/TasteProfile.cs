using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record TasteProfile(
    ImmutableDictionary<string, double> GenrePreferences,
    ImmutableArray<float> PlotEmbedding)
{
    public ReviewProfileChannels Liked { get; init; } = ReviewProfileChannels.Empty;

    public ReviewProfileChannels Disliked { get; init; } = ReviewProfileChannels.Empty;

    public ImmutableArray<LikedReviewSource> LikedSources { get; init; } = [];

    public ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> PlaybackByMovieId { get; init; } =
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty;

    public bool IsEmpty =>
        GenrePreferences.IsEmpty
        && PlotEmbedding.IsDefaultOrEmpty
        && Liked.IsEmpty
        && Disliked.IsEmpty;
}
