using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Watchlist;

public sealed record RemoveFromWatchlistCommand(MovieId Id);
