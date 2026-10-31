using System.Text.Json;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class ThumbnailRequestedTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Constructor_Request_KeepsMovieIdAndFilePath()
    {
        // arrange
        var movieId = new MovieId(71);

        // act
        var request = new ThumbnailRequested(movieId, "C:/library/a/movie.mkv");

        // assert
        request.MovieId.ShouldBe(movieId);
        request.FilePath.ShouldBe("C:/library/a/movie.mkv");
    }

    [Fact]
    public void JsonRoundTrip_Request_PreservesMovieIdAndFilePath()
    {
        // arrange
        var request = new ThumbnailRequested(new MovieId(71), "C:/library/a/movie.mkv");

        // act
        var restored = JsonSerializer.Deserialize<ThumbnailRequested>(
            JsonSerializer.SerializeToUtf8Bytes(request, SerializerOptions),
            SerializerOptions);

        // assert
        restored.ShouldNotBeNull();
        restored.MovieId.Value.ShouldBe(71);
        restored.FilePath.ShouldBe("C:/library/a/movie.mkv");
    }
}
