using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitlePreparationSurvivorTests
{
    [Fact]
    public void Require_NullProviderOptions_ThrowsArgumentNullException()
    {
        // arrange
        var subtitles = Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions
        {
            SaveEnabled = true,
            PreferredLanguages = ["en"],
        });

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleAcquisitionSettings.Require(subtitles, null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Select_NullPreferredLanguages_ThrowsArgumentNullException()
    {
        // arrange
        SubtitleCandidate[] candidates = [new(1, "en", DownloadCount: 1, MatchesMovieHash: false)];

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleSelection.Select(candidates, null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Select_NullCandidateEntry_IsSkipped()
    {
        // arrange
        SubtitleCandidate?[] candidates = [null, new(1, "en", DownloadCount: 1, MatchesMovieHash: false)];

        // act
        var selected = SubtitleSelection.Select(candidates!, ["en"]);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(1);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void For_DotStemPrimaryVideoName_ThrowsArgumentException(string primaryFileName)
    {
        // act
        var act = () => SubtitleSidecarName.For(primaryFileName);

        // assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Fail_WithNullActivity_StillRecordsTheOutcomeMetric()
    {
        // act
        var act = () => SubtitleTelemetry.Fail(null, new MovieId(7), new NotFoundException());

        // assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Fail_WithNullMovieId_ThrowsArgumentNullException()
    {
        // act
        var act = () => SubtitleTelemetry.Fail(null, null!, new NotFoundException());

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task TelemetryFail_PreservesTheOriginalExceptionFromSidecarServe()
    {
        // arrange
        var store = Substitute.For<ISubtitleSidecarStore>();
        var catalog = Substitute.For<IMovieCatalog>();
        var scanner = Substitute.For<IMediaLibraryScanner>();
        var reader = Substitute.For<ILibraryFileReader>();
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        var handler = new GetSubtitleSidecarQueryHandler(
            catalog,
            scanner,
            reader,
            store,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = "/library" }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetSubtitleSidecarQueryHandler>.Instance);

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleSidecarQuery(new MovieId(42)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        await store.DidNotReceiveWithAnyArgs().ReadAsync(
            default!, default!, default, TestContext.Current.CancellationToken);
    }
}

