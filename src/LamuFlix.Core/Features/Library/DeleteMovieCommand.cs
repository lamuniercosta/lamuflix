using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Library;

public sealed record DeleteMovieCommand(MovieId Id);
