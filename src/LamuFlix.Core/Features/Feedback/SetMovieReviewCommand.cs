using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Feedback;

public sealed record SetMovieReviewCommand(MovieId Id, string? Review);
