using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

internal static class MovieMetadataFixture
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    internal static MovieMetadata Create(string title, string? posterUrl) =>
        new(
            title,
            "Synopsis",
            new ReleaseYear(2000, Now),
            new Runtime(100),
            new ImdbRating(7.5m),
            new ImdbId("tt0000001"),
            posterUrl);
}
