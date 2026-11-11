using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using NSubstitute;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class ThumbnailPathResolverTests
{
    [Fact]
    public void TryResolveSource_AnExactFileMatch_ResolvesToTheRequestedPath()
    {
        // arrange
        var movie = "movie.mp4";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "movie.mp4", out var source);

        // assert
        resolved.ShouldBeTrue();
        source.ShouldBe("movie.mp4");
    }

    [Fact]
    public void TryResolveSource_AVideoFileInsideTheMovieFolder_ResolvesToTheRequestedFile()
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "library/movies/a/feature.mkv", out var source);

        // assert
        resolved.ShouldBeTrue();
        source.ShouldBe("library/movies/a/feature.mkv");
    }

    [Fact]
    public void TryResolveSource_AFolderRequest_MatchesTheFolderItself()
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "library/movies/a", out var source);

        // assert
        resolved.ShouldBeTrue();
        source.ShouldBe("library/movies/a");
    }

    [Fact]
    public void TryResolveSource_AFileOutsideTheMovieFolder_DoesNotResolve()
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "library/movies/b/feature.mkv", out var source);

        // assert
        resolved.ShouldBeFalse();
        source.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryResolveSource_ASiblingPrefixFolder_DoesNotResolve()
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "library/movies/ab/feature.mkv", out var source);

        // assert
        resolved.ShouldBeFalse();
        source.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryResolveSource_ANestedFileInsideTheMovieFolder_DoesNotResolve()
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "library/movies/a/extras/trailer.mp4", out var source);

        // assert
        resolved.ShouldBeFalse();
        source.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryResolveSource_ACaseInsensitiveMatch_Resolves()
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "LIBRARY/MOVIES/A/FEATURE.MKV", out var source);

        // assert
        resolved.ShouldBeTrue();
        source.ShouldBe("LIBRARY/MOVIES/A/FEATURE.MKV");
    }

    [Fact]
    public void TryResolveSource_AFileUnderTheRootIdentityMovie_Resolves()
    {
        // arrange
        var movie = ".";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, "movie.mp4", out var source);

        // assert
        resolved.ShouldBeTrue();
        source.ShouldBe("movie.mp4");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolveSource_ABlankRequest_DoesNotResolve(string request)
    {
        // arrange
        var movie = "library/movies/a";

        // act
        var resolved = ThumbnailPathResolver.TryResolveSource(movie, request, out var source);

        // assert
        resolved.ShouldBeFalse();
        source.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("library/movies/a", "library/movies/a", true)]
    [InlineData("library/movies/a", "LIBRARY/MOVIES/A", true)]
    [InlineData("library/movies/a", "library/movies/a/feature.mkv", false)]
    [InlineData("library/movies/a", "library/movies/b", false)]
    public void IsFolderRequest_DistinguishesFolderRequestsFromFileRequests(string movie, string request, bool expected)
    {
        // act
        var folder = ThumbnailPathResolver.IsFolderRequest(movie, request);

        // assert
        folder.ShouldBe(expected);
    }

    [Fact]
    public void ResolvePublishPath_WithoutAScanner_FallsBackToTheMoviePath()
    {
        // arrange
        var movie = new LibraryPath("library/movies/a");

        // act
        var published = ThumbnailPathResolver.ResolvePublishPath(movie, scanner: null);

        // assert
        published.ShouldBe("library/movies/a");
    }

    [Fact]
    public void ResolvePublishPath_WithAPrimaryVideo_PublishesTheVideoFile()
    {
        // arrange
        var movie = new LibraryPath("library/movies/a");
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(movie).Returns(new PrimaryVideo(new LibraryPath("library/movies/a/feature.mkv"), new MediaFormat("mkv")));

        // act
        var published = ThumbnailPathResolver.ResolvePublishPath(movie, scanner);

        // assert
        published.ShouldBe("library/movies/a/feature.mkv");
    }

    [Fact]
    public void ResolvePublishPath_WithoutAPrimaryVideo_ReturnsNullSoNothingIsPublished()
    {
        // arrange
        var movie = new LibraryPath("library/movies/a");
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(movie).Returns(_ => throw new NotFoundException());

        // act
        var published = ThumbnailPathResolver.ResolvePublishPath(movie, scanner);

        // assert
        published.ShouldBeNull();
    }

    [Fact]
    public void ResolvePublishPath_WhenTheFolderIsGone_ReturnsNullSoNothingIsPublished()
    {
        // arrange
        var movie = new LibraryPath("library/movies/a");
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(movie).Returns(_ => throw new InvalidTransitionException("gone"));

        // act
        var published = ThumbnailPathResolver.ResolvePublishPath(movie, scanner);

        // assert
        published.ShouldBeNull();
    }
}
