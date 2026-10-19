using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public sealed record MatchMovieImdbIdCommand(MovieId Id, ImdbId ImdbId);
