using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed record GoldenMovie(string Id, MovieFeatures Features);

public sealed record GoldenSeed(string SeedId, IReadOnlyList<string> ExpectedTopThree);

public static class SimilarityScorerGoldenFixture
{
    private const string CollectionName = "Saga";

    public static SimilarityWeights Weights => new();

    public static IReadOnlyList<GoldenMovie> Movies { get; } =
    [
        Movie("A-01", "Drama", "Ana", ["Dora", "Emil"], ["family"], null, Embedding(0.44)),
        Movie("A-02", "Drama", "Ana", ["Dora", "Emil", "Fay"], ["family", "grief"], 11, Embedding(0.42)),
        Movie("A-03", "Drama", "Ana", ["Dora"], ["grief"], null, Embedding(0.40)),
        Movie("A-04", "Drama", "Ana", ["Fay"], ["war"], null, Embedding(0.36)),
        Movie("A-05", "Drama", "Ana", ["Dora", "Emil"], ["family", "grief"], 11, [1f, 0f]),
        Movie("A-06", "Drama", "Bruno", ["Dora", "Emil"], ["family"], 11, Embedding(0.33)),
        Movie("A-07", "Drama", "Bruno", ["Dora", "Fay"], ["grief", "war"], 11, Embedding(0.30)),
        Movie("A-08", "Drama", "Bruno", ["Dora", "Emil"], ["family", "war"], null, Embedding(0.25)),
        Movie("A-09", "Drama", "Bruno", ["Emil", "Fay"], ["family", "grief"], null, Embedding(0.20)),
        Movie("A-10", "Drama", "Bruno", ["Dora", "Emil", "Fay"], ["grief"], null, Embedding(0.16)),
        Movie("B-01", "Comedy", "Hugo", ["Kara", "Liam"], ["parody"], null, Embedding(0.44)),
        Movie("B-02", "Comedy", "Hugo", ["Kara", "Mira"], ["road", "heist"], 22, Embedding(0.42)),
        Movie("B-03", "Comedy", "Hugo", ["Kara", "Liam", "Mira"], ["parody", "heist"], 22, Embedding(0.40)),
        Movie("B-04", "Comedy", "Hugo", ["Liam"], ["road"], null, Embedding(0.36)),
        Movie("B-05", "Comedy", "Hugo", ["Kara", "Liam"], ["parody"], null, Embedding(0.33)),
        Movie("B-06", "Comedy", "Iris", ["Kara", "Liam", "Mira"], ["parody", "road"], 22, Embedding(0.30)),
        Movie("B-07", "Comedy", "Iris", ["Mira"], ["heist"], null, Embedding(0.25)),
        Movie("B-08", "Comedy", "Iris", ["Kara", "Mira"], ["road", "heist"], 22, [1f, 0f]),
        Movie("B-09", "Comedy", "Iris", ["Kara", "Liam"], ["parody", "heist"], null, Embedding(0.20)),
        Movie("B-10", "Comedy", "Iris", ["Liam", "Mira"], ["road"], null, Embedding(0.16)),
        Movie("C-01", "Horror", "Olga", ["Rosa", "Sven"], ["ghost"], null, Embedding(0.44)),
        Movie("C-02", "Horror", "Olga", ["Rosa", "Tara"], ["ghost", "ritual"], 33, Embedding(0.42)),
        Movie("C-03", "Horror", "Olga", ["Rosa", "Sven", "Tara"], ["woods"], 33, Embedding(0.40)),
        Movie("C-04", "Horror", "Olga", ["Sven"], ["ritual"], null, Embedding(0.36)),
        Movie("C-05", "Horror", "Olga", ["Rosa", "Sven"], ["ghost", "woods"], null, Embedding(0.33)),
        Movie("C-06", "Horror", "Quinn", ["Rosa", "Sven"], ["ghost", "ritual"], 33, [1f, 0f]),
        Movie("C-07", "Horror", "Quinn", ["Tara"], ["woods"], null, Embedding(0.30)),
        Movie("C-08", "Horror", "Quinn", ["Rosa", "Tara"], ["ghost", "woods"], 33, Embedding(0.25)),
        Movie("C-09", "Horror", "Quinn", ["Rosa", "Sven", "Tara"], ["ritual"], null, Embedding(0.20)),
        Movie("C-10", "Horror", "Quinn", ["Sven", "Tara"], ["ghost"], null, Embedding(0.16)),
    ];

    public static IReadOnlyList<GoldenSeed> Seeds { get; } =
    [
        new("A-05", ["A-02", "A-01", "A-06"]),
        new("B-08", ["B-06", "B-02", "B-03"]),
        new("C-06", ["C-08", "C-02", "C-09"]),
    ];

    public static GenreCounts LibraryGenreCounts => new(
        Movies.Count,
        Movies
            .SelectMany(movie => movie.Features.Genres)
            .GroupBy(genre => genre, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (long)group.Count(), StringComparer.OrdinalIgnoreCase));

    public static KeywordCounts LibraryKeywordCounts => new(
        Movies.Count,
        Movies
            .SelectMany(movie => movie.Features.KeywordNames)
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (long)group.Count(), StringComparer.OrdinalIgnoreCase));

    private static float[] Embedding(double cosine) =>
        [(float)cosine, (float)Math.Sqrt(1.0 - (cosine * cosine))];

    private static GoldenMovie Movie(
        string id,
        string genre,
        string director,
        string[] cast,
        string[] keywords,
        int? collectionId,
        float[] plotEmbedding) =>
        new(
            id,
            new MovieFeatures(
                genres: [genre],
                directors: [director],
                cast: cast,
                collectionId: collectionId,
                keywordNames: keywords,
                collectionName: collectionId is null ? null : CollectionName,
                plotEmbedding: plotEmbedding));
}
