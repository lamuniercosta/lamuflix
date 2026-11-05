using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Library;

public sealed record RescanMovieResult(MovieId MovieId, bool Changed, MediaFormat OldFormat, MediaFormat NewFormat);
