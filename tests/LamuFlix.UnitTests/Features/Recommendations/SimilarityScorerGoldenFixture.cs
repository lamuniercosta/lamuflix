using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed record GoldenMovie(string Id, MovieFeatures Features);

public sealed record GoldenSeed(string SeedId, IReadOnlyList<string> ExpectedTopThree);

public static class SimilarityScorerGoldenFixture
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly MovieCluster Drama = new("Drama", 1990, 100, 7.0m);
    private static readonly MovieCluster Comedy = new("Comedy", 2000, 120, 8.0m);
    private static readonly MovieCluster Horror = new("Horror", 2010, 140, 6.0m);

    public static SimilarityWeights Weights { get; } = new()
    {
        Director = 3.0,
        Writers = 2.0,
        Cast = 2.0,
        Genres = 3.0,
        YearProximity = 1.0,
        RuntimeProximity = 0.5,
        RatingBoost = 0.5,
    };

    public static IReadOnlyList<GoldenMovie> Movies { get; } =
    [
        Movie(Drama, "A-01", ["Ana"], ["Dora", "Emil"], ["Fay"]),
        Movie(Drama, "A-02", ["Ana", "Bruno"], ["Dora", "Emil"], ["Fay"]),
        Movie(Drama, "A-03", ["Ana"], ["Dora", "Emil"], ["Gus"]),
        Movie(Drama, "A-04", ["Ana"], ["Dora"], ["Gus"]),
        Movie(Drama, "A-05", ["Ana", "Bruno"], ["Dora"], ["Fay"]),
        Movie(Drama, "A-06", ["Celia"], ["Dora"], ["Gus"]),
        Movie(Drama, "A-07", ["Ana", "Bruno"], ["Dora"], ["Gus"]),
        Movie(Drama, "A-08", ["Celia"], ["Dora", "Emil"], ["Fay"]),
        Movie(Drama, "A-09", ["Ana"], ["Dora"], ["Fay"]),
        Movie(Drama, "A-10", ["Ana", "Bruno"], ["Dora", "Emil"], ["Gus"]),
        Movie(Comedy, "B-01", ["Hugo", "Iris"], ["Kara"], ["Nils"]),
        Movie(Comedy, "B-02", ["Jonas"], ["Kara", "Liam"], ["Mira"]),
        Movie(Comedy, "B-03", ["Hugo", "Iris"], ["Kara", "Liam"], ["Mira"]),
        Movie(Comedy, "B-04", ["Hugo", "Iris"], ["Kara", "Liam"], ["Nils"]),
        Movie(Comedy, "B-05", ["Jonas"], ["Kara"], ["Nils"]),
        Movie(Comedy, "B-06", ["Hugo"], ["Kara"], ["Mira"]),
        Movie(Comedy, "B-07", ["Hugo"], ["Kara", "Liam"], ["Nils"]),
        Movie(Comedy, "B-08", ["Hugo", "Iris"], ["Kara"], ["Mira"]),
        Movie(Comedy, "B-09", ["Hugo"], ["Kara"], ["Nils"]),
        Movie(Comedy, "B-10", ["Hugo"], ["Kara", "Liam"], ["Mira"]),
        Movie(Horror, "C-01", ["Olga"], ["Rosa"], ["Tara"]),
        Movie(Horror, "C-02", ["Olga"], ["Rosa", "Sven"], ["Tara"]),
        Movie(Horror, "C-03", ["Olga"], ["Rosa", "Sven"], ["Ugo"]),
        Movie(Horror, "C-04", ["Olga", "Pavel"], ["Rosa"], ["Ugo"]),
        Movie(Horror, "C-05", ["Quinn"], ["Rosa", "Sven"], ["Tara"]),
        Movie(Horror, "C-06", ["Olga", "Pavel"], ["Rosa"], ["Tara"]),
        Movie(Horror, "C-07", ["Quinn"], ["Rosa"], ["Ugo"]),
        Movie(Horror, "C-08", ["Olga", "Pavel"], ["Rosa", "Sven"], ["Ugo"]),
        Movie(Horror, "C-09", ["Olga", "Pavel"], ["Rosa", "Sven"], ["Tara"]),
        Movie(Horror, "C-10", ["Olga"], ["Rosa"], ["Ugo"]),
    ];

    public static IReadOnlyList<GoldenSeed> Seeds { get; } =
    [
        new("A-05", ["A-02", "A-09", "A-07"]),
        new("B-08", ["B-03", "B-06", "B-01"]),
        new("C-06", ["C-09", "C-01", "C-04"]),
    ];

    public static GenreCounts LibraryGenreCounts { get; } = BuildGenreCounts();

    private static GoldenMovie Movie(
        MovieCluster cluster,
        string id,
        string[] directors,
        string[] writers,
        string[] cast) =>
        new(
            id,
            new MovieFeatures(
                genres: [cluster.Genre],
                directors: directors,
                writers: writers,
                cast: cast,
                releaseYear: new ReleaseYear(cluster.Year, Now),
                runtime: new Runtime(cluster.Minutes),
                imdbRating: new ImdbRating(cluster.Rating)));

    private static GenreCounts BuildGenreCounts()
    {
        var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var genre in Movies.SelectMany(movie => movie.Features.Genres))
        {
            counts[genre] = counts.TryGetValue(genre, out var current) ? current + 1 : 1;
        }

        return new GenreCounts(Movies.Count, counts);
    }

    private sealed record MovieCluster(string Genre, int Year, int Minutes, decimal Rating);
}
