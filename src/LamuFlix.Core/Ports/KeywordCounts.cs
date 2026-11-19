using System.Collections.Generic;

namespace LamuFlix.Core.Ports;

public sealed record KeywordCounts(long TotalMovies, IReadOnlyDictionary<string, long> MoviesWithKeyword);