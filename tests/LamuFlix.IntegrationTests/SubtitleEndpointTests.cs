using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(SubtitleEndpointCollection))]
public sealed class SubtitleEndpointTests(ApiHostFactory factory)
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string CacheControlHeader = "Cache-Control";
    private const string NoStoreDirective = "no-store";
    private const string PrivateDirective = "private";
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string NoSniffValue = "nosniff";
    private const string Stem = "Heat.1995.1080p";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();

    [Fact]
    public async Task Get_EligibleSidecarMatrix_ReturnsFrozenTracksInFrozenOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.en.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p-EN.SRT");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.pob.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p-POB.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.default.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p-default.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.forced.srt");
        library.WriteMovieFile(MovieFolder, "unrelated.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt.bak");
        library.WriteMovieFile(Path.Combine(MovieFolder, "nested"), "Heat.1995.1080p.de.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var tracks = TrackElements(body);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        tracks.Select(track => track.GetProperty("label").GetString()).ShouldBe(
        [
            "Default",
            "Default (2)",
            "English",
            "English (2)",
            "Portuguese (Brazil)",
            "Portuguese (Brazil) (2)",
            "Heat.1995.1080p.forced",
            "Subtitles",
        ]);
        tracks.Select(track => track.GetProperty("language").GetString()).ShouldBe(
        [
            "und",
            "und",
            "en",
            "en",
            "pt-BR",
            "pt-BR",
            "und",
            "und",
        ]);
        tracks.Select(track => track.GetProperty("isDefault").GetBoolean()).ShouldBe(
        [
            true,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
        ]);
        body.ShouldNotContain("nested");
        body.ShouldNotContain(PrimaryFileName);
        body.ShouldNotContain(library.Root);
    }

    [Fact]
    public async Task Get_NoEligibleSidecars_ReturnsEmptyArray()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, "Another Movie.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        body.ShouldBe("[]");
    }

    [Fact]
    public async Task Get_RepeatedListingsOfUnchangedSet_ReturnStableOpaqueDistinctNonOrdinalTokens()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.en.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.pob.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var first = TrackElements(await (await client.GetAsync(SubtitlesRoute(KnownId), ct))
            .Content.ReadAsStringAsync(ct));
        var second = TrackElements(await (await client.GetAsync(SubtitlesRoute(KnownId), ct))
            .Content.ReadAsStringAsync(ct));

        // assert
        var firstIds = first.Select(track => track.GetProperty("id").GetString()).ToArray();
        var secondIds = second.Select(track => track.GetProperty("id").GetString()).ToArray();
        secondIds.ShouldBe(firstIds);
        firstIds.ShouldAllBe(id => !string.IsNullOrWhiteSpace(id));
        firstIds.Distinct(StringComparer.Ordinal).Count().ShouldBe(firstIds.Length);
        firstIds.ShouldNotContain("0");
        firstIds.ShouldNotContain("1");
        firstIds.ShouldNotContain("2");
        foreach (var id in firstIds)
        {
            var token = id.ShouldNotBeNull();
            token.ShouldNotContain("/");
            token.ShouldNotContain("+");
            token.ShouldNotContain("=");
            token.ShouldNotContain(".srt");
        }
    }

    [Fact]
    public async Task Get_UnknownMovie_ReturnsSanitizedNotFoundProblemDetailsWithNoStoreAndTraceId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(UnknownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.NotFound);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
    }

    [Fact]
    public async Task Get_MovieFolderWithoutPrimaryVideo_ReturnsNotFoundProblemDetails()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain("Heat.1995.1080p.srt");
    }

    [Fact]
    public async Task Get_LibraryFolderOutsideAuthorizedRoot_ReturnsForbiddenThroughSingleHandler()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var outside = new TemporaryLibrary();
        outside.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        outside.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        DetailsReturning(KnownDetails(outside.Root));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Forbidden);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(outside.Root);
        body.ShouldNotContain(PrimaryFileName);
    }

    [Fact]
    public async Task Get_UnexpectedFailure_ReturnsGenericSanitizedProblemDetailsWithNoStoreAndTraceId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        const string sentinel = "sentinel-subtitle-list-failure";
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(sentinel));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.InternalServerError);
        problem.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(sentinel);
        body.ShouldNotContain(nameof(InvalidOperationException));
        body.ShouldNotContain("stackTrace");
        body.ShouldNotContain(library.Root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetContent_Utf8AccentsWithOptionalBom_ReturnsWebVttHeaderAndLiteralCueText(bool withBom)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.en.srt", withBom
            ? SrtWithBom(Document(
                "1",
                "00:00:01,000 --> 00:00:02,500",
                "Ação & Paixão <b>não</b> é HTML",
                "",
                "2",
                "00:01:02,004 --> 00:01:03,000",
                "Segunda linha"))
            : Utf8(Document(
                "1",
                "00:00:01,000 --> 00:00:02,500",
                "Ação & Paixão <b>não</b> é HTML",
                "",
                "2",
                "00:01:02,004 --> 00:01:03,000",
                "Segunda linha")));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contentType = response.Content.Headers.ContentType.ShouldNotBeNull();
        contentType.MediaType.ShouldBe("text/vtt");
        contentType.CharSet.ShouldBe("utf-8");
        response.Headers.GetValues(ContentTypeOptionsHeader).ShouldBe([NoSniffValue]);
        AssertFrozenNoStore(response);
        body.ShouldStartWith("WEBVTT\n\n");
        body.ShouldContain("00:00:01.000 --> 00:00:02.500");
        body.ShouldContain("00:01:02.004 --> 00:01:03.000");
        body.ShouldContain("Ação &amp; Paixão &lt;b&gt;não&lt;/b&gt; é HTML");
        body.ShouldContain("Segunda linha");
        body.ShouldNotContain("\r");
        body.ShouldNotContain("<b>");
        body.ShouldNotContain("1\n00:00:01");
    }

    [Fact]
    public async Task GetContent_Windows1252AccentedFixture_DecodesThroughTheMandatedFallback()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(
            MovieFolder,
            $"{Stem}.pob.srt",
            Windows1252Srt("1", "00:00:03,250 --> 00:00:04,000", "Ação e coração"));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldContain("Ação e coração");
        body.ShouldContain("00:00:03.250 --> 00:00:04.000");
    }

    [Theory]
    [InlineData(0xFF, 0xFE)]
    [InlineData(0xFE, 0xFF)]
    public async Task GetContent_Utf16OrUtf32ByteOrderMark_ReturnsSanitizedUnprocessableEntity(
        byte first,
        byte second)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(
            MovieFolder,
            $"{Stem}.srt",
            [first, second, 0x31, 0x00, 0x0A, 0x00]);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.UnprocessableEntity);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
        body.ShouldNotContain(Stem);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1\n00:00:01,000 --> 00:00:02,000\n")]
    [InlineData("not a subtitle document at all")]
    [InlineData("1\n00:00:01,000 --> 00:00:02,000\nfirst\n\n2\nno timing line\nsecond")]
    public async Task GetContent_EmptyCueLessOrUnparseableDocument_ReturnsSanitizedUnprocessableEntity(
        string document)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Utf8(document));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
        body.ShouldNotContain("first");
    }

    [Theory]
    [InlineData("1\n00:00:02,000 --> 00:00:01,000\nreversed")]
    [InlineData("1\n-00:00:01,000 --> 00:00:02,000\nnegative")]
    public async Task GetContent_ReversedOrNegativeInterval_ReturnsSanitizedUnprocessableEntity(string document)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Utf8(document));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        AssertFrozenNoStore(response);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain("reversed");
        body.ShouldNotContain("negative");
    }

    [Fact]
    public async Task GetContent_CueBoundaryFixtures_PreserveLiteralWordsAndValidOverlap()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Srt(
            "1",
            "00:00:01,000 --> 00:00:05,000",
            "NOTE this is literal cue text",
            "STYLE is literal too, and REGION as well",
            "",
            "2",
            "00:00:02,000 --> 00:00:03,000",
            "Overlapping cue with 5 > 4 and a -> b",
            "",
            "3",
            "00:00:06,000 --> 00:00:07,000",
            "Final cue"));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldContain("NOTE this is literal cue text");
        body.ShouldContain("STYLE is literal too, and REGION as well");
        body.ShouldContain("Overlapping cue with 5 &gt; 4 and a -&gt; b");
        body.ShouldContain("00:00:01.000 --> 00:00:05.000");
        body.ShouldContain("00:00:02.000 --> 00:00:03.000");
        body.ShouldContain("Final cue");
        body.ShouldNotContain("NOTE\n");
    }

    [Fact]
    public async Task GetContent_PayloadArrowInsideCue_ReturnsSanitizedUnprocessableEntity()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Srt(
            "1",
            "00:00:01,000 --> 00:00:02,000",
            "forged --> 00:00:09,000"));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        AssertFrozenNoStore(response);
        body.ShouldNotContain("forged");
    }

    [Theory]
    [InlineData(nameof(LibraryOptions.MaxSubtitleInputBytes))]
    [InlineData(nameof(LibraryOptions.MaxSubtitleCues))]
    [InlineData(nameof(LibraryOptions.MaxSubtitleLineCharacters))]
    [InlineData(nameof(LibraryOptions.MaxSubtitleOutputBytes))]
    public async Task GetContent_FirstStepOverConfiguredBound_ReturnsUnprocessableEntity(string bound)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        var document = BoundedDocument();
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Utf8(document));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, Overrides(bound, FirstOver(bound, document)));
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        AssertFrozenNoStore(response);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
        body.ShouldNotContain("bounded cue text");
    }

    [Theory]
    [InlineData(nameof(LibraryOptions.MaxSubtitleInputBytes))]
    [InlineData(nameof(LibraryOptions.MaxSubtitleCues))]
    [InlineData(nameof(LibraryOptions.MaxSubtitleLineCharacters))]
    [InlineData(nameof(LibraryOptions.MaxSubtitleOutputBytes))]
    public async Task GetContent_ExactlyAtConfiguredBound_ReturnsOk(string bound)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        var document = BoundedDocument();
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Utf8(document));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, Overrides(bound, ExactBound(bound, document)));
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, token), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldContain("bounded cue text");
    }

    [Fact]
    public async Task GetContent_UnknownTamperedOtherMovieOrVanishedToken_ReturnsNotFoundProblemDetails()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.en.srt", Srt(
            "1", "00:00:01,000 --> 00:00:02,000", "listed"));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);

        // act
        var tampered = await client.GetAsync(ContentRoute(KnownId, token + "x"), ct);
        var unknown = await client.GetAsync(ContentRoute(KnownId, "unknown-token"), ct);
        var otherMovie = await client.GetAsync(ContentRoute(KnownId + 1, token), ct);
        library.DeleteMovieFile(MovieFolder, $"{Stem}.en.srt");
        var vanished = await client.GetAsync(ContentRoute(KnownId, token), ct);

        // assert
        foreach (var response in new[] { tampered, unknown, otherMovie, vanished })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            AssertFrozenNoStore(response);
        }

        var problem = JsonSerializer.Deserialize<JsonElement>(await tampered.Content.ReadAsStringAsync(ct));
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.NotFound);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetContent_RangeHeader_ReturnsFullOkWithoutAcceptRangesOrPartialContent()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Srt(
            "1", "00:00:01,000 --> 00:00:02,000", "ranged cue"));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();
        var token = await ListSingleToken(client, ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, ContentRoute(KnownId, token));
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 4);

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.AcceptRanges.ShouldBeEmpty();
        response.Content.Headers.ContentRange.ShouldBeNull();
        body.ShouldStartWith("WEBVTT");
    }

    [Fact]
    public async Task GetContent_LibraryFolderOutsideAuthorizedRoot_ReturnsForbiddenThroughSingleHandler()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var outside = new TemporaryLibrary();
        outside.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        outside.WriteMovieFile(MovieFolder, $"{Stem}.srt", Srt(
            "1", "00:00:01,000 --> 00:00:02,000", "outside"));
        DetailsReturning(KnownDetails(outside.Root));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, "any-token"), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Forbidden);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(outside.Root);
    }

    [Fact]
    public async Task GetContent_LinkedSidecarBeyondAuthorizedFolder_FailsClosedWithoutServingIt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var outside = new TemporaryLibrary();
        var outsideFile = outside.WriteMovieFile(
            MovieFolder,
            "outside.srt",
            Srt("1", "00:00:01,000 --> 00:00:02,000", "unauthorized bytes"));
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        CreateFileLinkOrFail(Path.Combine(library.Root, MovieFolder, $"{Stem}.srt"), outsideFile);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var list = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var listBody = await list.Content.ReadAsStringAsync(ct);
        var content = await client.GetAsync(ContentRoute(KnownId, "any-token"), ct);
        var contentBody = await content.Content.ReadAsStringAsync(ct);

        // assert
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        listBody.ShouldBe("[]");
        content.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        contentBody.ShouldNotContain("unauthorized bytes");
        contentBody.ShouldNotContain(outside.Root);
    }

    [Fact]
    public async Task GetContent_UnexpectedFailure_ReturnsGenericSanitizedProblemDetailsHeaders()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        const string sentinel = "sentinel-subtitle-content-failure";
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(sentinel));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ContentRoute(KnownId, "any-token"), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        response.Headers.GetValues(ContentTypeOptionsHeader).ShouldBe([NoSniffValue]);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(sentinel);
        body.ShouldNotContain("stackTrace");
        body.ShouldNotContain(library.Root);
    }

    [Fact]
    public async Task Get_OneBadTrackAmongMany_KeepsListingAndOtherTrackContentAvailable()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, $"{Stem}.en.srt", Srt(
            "1", "00:00:01,000 --> 00:00:02,000", "good track"));
        library.WriteMovieFile(MovieFolder, $"{Stem}.srt", Utf8("broken document"));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var list = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var tracks = TrackElements(await list.Content.ReadAsStringAsync(ct));
        var bad = tracks.Single(track => track.GetProperty("isDefault").GetBoolean() == false
                                        && track.GetProperty("label").GetString() == "Subtitles");
        var good = tracks.Single(track => track.GetProperty("label").GetString() == "English");
        var badContent = await client.GetAsync(
            ContentRoute(KnownId, bad.GetProperty("id").GetString().ShouldNotBeNull()), ct);
        var goodContent = await client.GetAsync(
            ContentRoute(KnownId, good.GetProperty("id").GetString().ShouldNotBeNull()), ct);

        // assert
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        badContent.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        goodContent.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await goodContent.Content.ReadAsStringAsync(ct)).ShouldContain("good track");
    }

    /// <summary>
    /// The frozen policy is <c>private, no-store</c>. Framework serialization may reorder directives,
    /// and directive order carries no HTTP meaning, so the directive set is asserted instead.
    /// </summary>
    private static void AssertFrozenNoStore(HttpResponseMessage response)
    {
        var values = response.Headers.GetValues(CacheControlHeader).ShouldHaveSingleItem();
        var directives = values.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        directives.ShouldContain(PrivateDirective);
        directives.ShouldContain(NoStoreDirective);
        directives.Length.ShouldBe(2);
    }

    private static JsonElement[] TrackElements(string body)
    {
        using var document = JsonDocument.Parse(body);
        return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
    }

    private static async Task<string> ListSingleToken(HttpClient client, CancellationToken cancellationToken)
    {
        var list = await client.GetAsync(SubtitlesRoute(KnownId), cancellationToken);
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        return FirstTrackToken(await list.Content.ReadAsStringAsync(cancellationToken));
    }

    private static byte[] Utf8(string document) => Encoding.UTF8.GetBytes(document);

    private static string Document(params string[] lines) => string.Join("\n", lines) + "\n";

    private static byte[] Srt(params string[] lines) => Utf8(Document(lines));

    private static byte[] SrtWithBom(string document) =>
        [.. new byte[] { 0xEF, 0xBB, 0xBF }, .. Utf8(document)];
    private static byte[] Windows1252Srt(string index, string timing, string text) =>
        CodePagesEncodingProvider.Instance.GetEncoding(1252).ShouldNotBeNull()
            .GetBytes(string.Join("\n", index, timing, text) + "\n");

    private static string BoundedDocument() => Document(
        "1",
        "00:00:01,000 --> 00:00:02,000",
        "bounded cue text",
        "",
        "2",
        "00:00:03,000 --> 00:00:04,000",
        "second bounded cue");

    private static int ExactBound(string bound, string document) => bound switch
    {
        nameof(LibraryOptions.MaxSubtitleInputBytes) => Utf8(document).Length,
        nameof(LibraryOptions.MaxSubtitleCues) => 2,
        nameof(LibraryOptions.MaxSubtitleLineCharacters) => "second bounded cue".Length,
        nameof(LibraryOptions.MaxSubtitleOutputBytes) => OutputBytes(document),
        _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, "Unknown subtitle bound."),
    };

    private static int FirstOver(string bound, string document) => ExactBound(bound, document) - 1;

    private static int OutputBytes(string document) =>
        Encoding.UTF8.GetByteCount(SubtitleWebVttConverter.Convert(
            document,
            new SubtitleConversionLimits(1000, 1000, 1_000_000)));

    private static Dictionary<string, string?> Overrides(string bound, int value) =>
        new()
        {
            [$"{LibraryOptions.SectionName}:{bound}"] = value.ToString(CultureInfo.InvariantCulture),
        };

    private static void CreateFileLinkOrFail(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Assert.Fail($"A real symlink fixture is mandatory for this proof but could not be created: {exception.Message}");
        }
    }

    private static string SubtitlesRoute(int id) => $"/api/movies/{id}/subtitles";

    private static string ContentRoute(int id, string trackId) => $"/api/movies/{id}/subtitles/{trackId}/content";

    private static string FirstTrackToken(string body) =>
        TrackElements(body).First().GetProperty("id").GetString()
        ?? throw new InvalidOperationException("The subtitle track list must expose an id.");


    private static MovieDetails KnownDetails(string storedPath) =>
        new(
            new MovieId(KnownId),
            "Heat",
            new LibraryPath(storedPath),
            new MediaFormat("mkv"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched);

    private void DetailsReturning(MovieDetails details) =>
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(details);

    private WebApplicationFactory<Program> CreateHost(
        string libraryRoot,
        IReadOnlyDictionary<string, string?>? additionalSettings = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Features:LocalPlay"] = "false",
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = libraryRoot,
            };
            foreach (var setting in additionalSettings ?? new Dictionary<string, string?>())
            {
                settings[setting.Key] = setting.Value;
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services => services.AddSingleton(catalog));
        });

    private sealed class TemporaryLibrary : IDisposable
    {
        public TemporaryLibrary()
        {
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-subtitles-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WriteMoviePrimary(string movieFolder, string fileName) =>
            WriteMovieFile(movieFolder, fileName, [0x1A, 0x45, 0xDF, 0xA3]);

        public string WriteMovieFile(string movieFolder, string fileName, byte[]? contents = null)
        {
            var directory = Path.Combine(Root, movieFolder);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, contents ?? [0x31, 0x0A]);
            return path;
        }

        public void DeleteMovieFile(string movieFolder, string fileName) =>
            File.Delete(Path.Combine(Root, movieFolder, fileName));

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
