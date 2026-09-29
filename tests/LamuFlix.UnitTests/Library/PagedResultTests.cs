using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Library;

public sealed class PagedResultTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_NonDefaultItemsAndSufficientTotal_SetsItemsAndTotalCount()
    {
        var items = ImmutableArray.Create("a");

        var result = new PagedResult<string>(items, 2);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public void Constructor_TotalCountEqualsItemsLength_IsAccepted()
    {
        var items = ImmutableArray.Create(1, 2);

        var result = new PagedResult<int>(items, items.Length);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(items.Length);
    }

    [Fact]
    public void Constructor_DefaultItems_ThrowsArgumentException()
    {
        var exception = Should.Throw<ArgumentException>(() => new PagedResult<int>(default, 0));

        exception.Message.ShouldStartWith("Items must be initialized.");
        exception.ParamName.ShouldBe("items");
    }

    [Fact]
    public void Constructor_NegativeTotalCount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new PagedResult<int>(ImmutableArray<int>.Empty, -1));
    }

    [Fact]
    public void Constructor_TotalCountLessThanItemsLength_ThrowsArgumentOutOfRangeException()
    {
        var items = ImmutableArray.Create("a", "b");

        Should.Throw<ArgumentOutOfRangeException>(() => new PagedResult<string>(items, 1));
    }

    [Fact]
    public void EnrichmentRequested_ToString_FormatsCorrectly()
    {
        var movieId = new MovieId(42);
        var message = new EnrichmentRequested(movieId, 3);

        message.ToString().ShouldBe($"EnrichmentRequested {{ MovieId = {movieId}, Attempt = 3 }}");
    }

    [Fact]
    public void MetadataLookup_ToString_FormatsCorrectly()
    {
        var year = new ReleaseYear(2024, Now);
        var lookup = new MetadataLookup("Inception", year);

        lookup.ToString().ShouldBe($"MetadataLookup {{ Title = Inception, ReleaseYear = {year} }}");
    }

    [Fact]
    public void MetadataLookupResult_Found_ToString_FormatsCorrectly()
    {
        var metadata = new MovieMetadata("Inception");
        var result = new MetadataLookupResult.Found(metadata);

        result.ToString().ShouldBe($"Found {{ Metadata = {metadata} }}");
    }

    [Fact]
    public void MetadataLookupResult_Failed_ToString_FormatsCorrectly()
    {
        var result = new MetadataLookupResult.Failed(EnrichmentFailureCategory.Unknown);

        result.ToString().ShouldBe($"Failed {{ Category = {EnrichmentFailureCategory.Unknown} }}");
    }

    [Fact]
    public void MovieSummary_ToString_FormatsCorrectly()
    {
        var id = new MovieId(7);
        var summary = new MovieSummary(id, "The Matrix");

        summary.ToString().ShouldBe($"MovieSummary {{ Id = {id}, Title = The Matrix }}");
    }

    [Fact]
    public void ScannedMovie_ToString_FormatsCorrectly()
    {
        var path = new LibraryPath("/movies/matrix.mkv");
        var format = new MediaFormat(".mkv");
        var movie = new ScannedMovie(path, "The Matrix", format);

        movie.ToString().ShouldBe($"ScannedMovie {{ Path = {path}, Title = The Matrix, Format = {format} }}");
    }

    [Fact]
    public void MovieDetails_ToString_FormatsCorrectly()
    {
        var id = new MovieId(10);
        var path = new LibraryPath("/movies/matrix.mkv");
        var format = new MediaFormat(".mkv");
        var metadata = new MovieMetadata("The Matrix");
        var details = new MovieDetails(id, "The Matrix", path, format, metadata);

        details.ToString().ShouldBe($"MovieDetails {{ Id = {id}, Title = The Matrix, Path = {path}, Format = {format}, Metadata = {metadata} }}");
    }
}
