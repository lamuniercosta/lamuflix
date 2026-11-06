namespace LamuFlix.Core.Domain;

public sealed record LikedReviewSource(
    MovieId MovieId,
    string Title,
    ReviewProfileChannels Channels);
