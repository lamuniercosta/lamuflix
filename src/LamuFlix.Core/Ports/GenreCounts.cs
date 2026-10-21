using System.Collections.Generic;

namespace LamuFlix.Core.Ports;

public sealed record GenreCounts(long TotalMovies, IReadOnlyDictionary<string, long> MoviesWithGenre);
