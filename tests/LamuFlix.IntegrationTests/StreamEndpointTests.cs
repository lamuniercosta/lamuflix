using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.Streaming;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(StreamEndpointCollection))]
public sealed class StreamEndpointTests(ApiHostFactory factory)
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const int NonpositiveId = 0;
    private const string MovieFolder = "Heat (1995)";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string BinaryMediaType = "application/octet-stream";
    private const string Mp4MediaType = "video/mp4";
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string NoSniffValue = "nosniff";
    private const string AcceptRangesHeader = "Accept-Ranges";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IPlaybackHistory history = Substitute.For<IPlaybackHistory>();
    private readonly IProcessStarter starter = Substitute.For<IProcessStarter>();
    private readonly IStreamConversionService conversion = Substitute.For<IStreamConversionService>();
    private readonly CaptureLog log = new();

    [Fact]
    public async Task Get_NoRange_ReturnsFullByteExactBodyWithFrozenHeadersAndNosniff()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(4096);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe(payload);
        response.Content.Headers.ContentLength.ShouldBe(payload.Length);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(Mp4MediaType);
        (await AcceptRangesAsync(response)).ShouldBe("bytes");
        NoSniff(response).ShouldBe(NoSniffValue);
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        response.Headers.ETag.ShouldBeNull();
        response.Content.Headers.LastModified.ShouldBeNull();
        reader.Opens.ShouldBe(1);
    }

    [Fact]
    public async Task Get_SatisfiableSingleRange_ReturnsExactSliceWithFrozenHeadersAndNosniff()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(4096);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        request.Headers.Range = new RangeHeaderValue(100, 199);

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        body.ShouldBe(payload[100..200]);
        response.Content.Headers.ContentLength.ShouldBe(100);
        response.Content.Headers.ContentRange.ShouldNotBeNull().ToString().ShouldBe("bytes 100-199/4096");
        (await AcceptRangesAsync(response)).ShouldBe("bytes");
        NoSniff(response).ShouldBe(NoSniffValue);
        reader.Opens.ShouldBe(1);
    }

    [Fact]
    public async Task Get_OpenEndedRange_ReturnsExactSuffixSlice()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(4096);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        request.Headers.Range = new RangeHeaderValue(4000, null);

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        body.ShouldBe(payload[4000..]);
        response.Content.Headers.ContentRange.ShouldNotBeNull().ToString().ShouldBe("bytes 4000-4095/4096");
        NoSniff(response).ShouldBe(NoSniffValue);
    }

    [Fact]
    public async Task Get_UnsatisfiableRange_Returns416WithFrozenContentRangeAndNoNosniff()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(4096);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        request.Headers.Range = new RangeHeaderValue(99999, null);

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentRange.ShouldNotBeNull().ToString().ShouldBe("bytes */4096");
        NoSniff(response).ShouldBeNull();
        body.ShouldNotContain("ProblemDetails");
    }

    [Fact]
    public async Task Get_MultipleRanges_IsCharacterizedWithoutAFrozenStatusPromise()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(4096);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        request.Headers.TryAddWithoutValidation("Range", "bytes=0-9,20-29");

        // act
        var response = await client.SendAsync(request, ct);

        // assert
        new[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.PartialContent,
            HttpStatusCode.RequestedRangeNotSatisfiable,
        }.ShouldContain(response.StatusCode);
    }

    [Fact]
    public async Task Get_MalformedRange_IsCharacterizedWithoutAFrozenStatusPromise()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(512);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        request.Headers.TryAddWithoutValidation("Range", "bytes=not-a-range");

        // act
        var response = await client.SendAsync(request, ct);

        // assert
        new[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.PartialContent,
            HttpStatusCode.RequestedRangeNotSatisfiable,
        }.ShouldContain(response.StatusCode);
    }

    [Fact]
    public async Task Get_UnmappedFormat_FallsBackToOctetStreamWithoutDisposition()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(64);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("zzz"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe(payload);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(BinaryMediaType);
        response.Content.Headers.ContentDisposition.ShouldBeNull();
    }

    [Fact]
    public async Task Get_MappedFormat_UsesTheProviderMappingNotTheStoredFormat()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(64);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("avi"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(Mp4MediaType);
    }

    [Fact]
    public async Task Get_NonpositiveId_ReturnsBadRequestWithoutResolvingOrOpening()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var reader = new RecordingReader("/unused/file.mp4");
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(NonpositiveId), ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        reader.FolderAuthorizations.ShouldBeEmpty();
        reader.Opens.ShouldBe(0);
        NoConversionInvocations();
    }

    [Fact]
    public async Task Get_UnknownMovie_ReturnsNotFoundWithoutResolvingOrOpening()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var reader = new RecordingReader("/unused/file.mp4");
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(UnknownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        body.ShouldNotContain(nameof(NotFoundException));
        reader.FolderAuthorizations.ShouldBeEmpty();
        reader.Opens.ShouldBe(0);
        NoConversionInvocations();
    }

    [Fact]
    public async Task Get_MissingPrimaryFile_ReturnsNotFoundWithoutServingBytes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader("/unused/file.mp4") { OpenFailure = new NotFoundException() };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        body.ShouldNotContain(nameof(NotFoundException));
        reader.Opens.ShouldBe(1);
        NoConversionInvocations();
    }

    [Fact]
    public async Task Get_NoEligiblePrimaryVideo_ReturnsConflictProblem()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Throws(new InvalidTransitionException("Library folder 'x' contains no eligible top-level video file."));
        var reader = new RecordingReader("/unused/file.mp4");
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.GetProperty("status").GetInt32().ShouldBe(409);
        reader.Opens.ShouldBe(0);
    }

    [Fact]
    public async Task Get_ContainmentDenial_ReturnsSanitizedForbiddenFromThePolicyExceptionOnly()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        var reader = new RecordingReader("/unused/file.mp4")
        {
            FolderFailure = new LibraryFileAccessDeniedException($"'{MovieFolder}' is outside the authorized library root."),
        };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var problem = JsonDocument.Parse(body).RootElement.Clone();

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(403);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        body.ShouldNotContain(MovieFolder);
        body.ShouldNotContain("authorized library root");
        body.ShouldNotContain(nameof(LibraryFileAccessDeniedException));
        reader.Opens.ShouldBe(0);
    }

    [Fact]
    public async Task Get_UnexpectedFilesystemAccessDenial_ReturnsSanitizedInternalServerError()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader("/unused/file.mp4") { OpenFailure = new UnauthorizedAccessException("C:/library/x") };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        body.ShouldNotContain("C:/library");
        body.ShouldNotContain(nameof(UnauthorizedAccessException));
    }

    [Fact]
    public async Task Get_OperationalIoFault_ReturnsSanitizedInternalServerErrorNotNullServedBytes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader("/unused/file.mp4") { OpenFailure = new IOException("device not ready at C:/library/x") };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        body.ShouldNotContain("device not ready");
    }

    [Fact]
    public async Task Get_AuthorizeFolder_RunsBeforeResolveAndOpensExactlyOnceAfterResolve()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(256);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        reader.Sequence.ShouldBe(["authorize-folder", "open"]);
        reader.FolderAuthorizations.ShouldBe([MovieFolder]);
        reader.OpenedPaths.ShouldBe([ResolvedPath("mp4")]);
        reader.Opens.ShouldBe(1);
    }

    [Fact]
    public async Task Get_DeniedFolder_PreventsResolverInvocationAndOpen()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        var reader = new RecordingReader("/unused/file.mp4") { FolderFailure = new LibraryFileAccessDeniedException() };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        reader.Opens.ShouldBe(0);
        reader.Sequence.ShouldBe(["authorize-folder"]);
        NoConversionInvocations();
    }

    [Fact]
    public async Task Get_LocalPlayDisabled_StreamsWithZeroHistoryOrProcessSideEffects()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(128);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe(payload);
        host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<FeatureOptions>>()
            .Value.LocalPlay.ShouldBeFalse();
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_SuccessfulStream_TransfersOwnershipSoTheOpenedStreamIsDisposed()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(64);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        reader.OpenedStreams.ShouldHaveSingleItem().Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_ContainmentDenial_LogsTheRequestedMovieIdWithoutFilesystemPaths()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        var reader = new RecordingReader("/unused/file.mp4")
        {
            FolderFailure = new LibraryFileAccessDeniedException($"'{MovieFolder}' escapes the root."),
        };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var entry = log.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.State.ShouldContain(pair => pair.Key == "MovieId" && Equals(pair.Value, KnownId));
        entry.Message.ShouldNotContain(MovieFolder);
    }

    [Fact]
    public async Task Get_MappedPreHeaderError_LogsTheRequestedMovieIdWithoutFilesystemPaths()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader("/unused/file.mp4") { OpenFailure = new NotFoundException() };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var entry = log.Entries.ShouldHaveSingleItem();
        entry.State.ShouldContain(pair => pair.Key == "MovieId" && Equals(pair.Value, KnownId));
        entry.Message.ShouldNotContain(MovieFolder);
        entry.Message.ShouldNotContain("/unused/file.mp4");
    }

    [Fact]
    public async Task Get_MidBodyReadFailure_DoesNotEmitSecondJsonBody()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader("/unused/file.mp4")
        {
            StreamFactory = () => new FailingStream(Payload(1024), failAfterBytes: 128, new IOException("mid-body fault")),
        };
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        using var response = await client.GetAsync(StreamRoute(KnownId), HttpCompletionOption.ResponseHeadersRead, ct);
        var received = new List<byte>();
        var readFailure = await Record.ExceptionAsync(async () =>
        {
            await using var bodyStream = await response.Content.ReadAsStreamAsync(ct);
            var buffer = new byte[64];
            int read;
            while ((read = await bodyStream.ReadAsync(buffer, ct)) > 0)
            {
                received.AddRange(buffer[..read]);
            }
        });

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe(ProblemDetailsJson);
        readFailure.ShouldNotBeNull();
        received.Count.ShouldBeLessThanOrEqualTo(128);
        received.Count.ShouldBeLessThan(1024);
        System.Text.Encoding.UTF8.GetString([.. received]).ShouldNotContain("ProblemDetails");
        var failure = log.Entries.ShouldHaveSingleItem();
        failure.State.ShouldContain(pair => pair.Key == "MovieId" && Equals(pair.Value, KnownId));
        failure.Message.ShouldNotContain("mid-body fault");
    }

    [Fact]
    public async Task Get_MissingFolder_ReturnsSanitizedNotFoundProblem()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new NotFoundException());
        var reader = new RecordingReader("/unused/file.mp4");
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        body.ShouldNotContain(MovieFolder);
        body.ShouldNotContain(nameof(NotFoundException));
        reader.Opens.ShouldBe(0);
    }

    [Fact]
    public void Routes_StreamEndpoint_MirrorsTheLibraryCallerAccessPolicy()
    {
        // arrange
        using var host = new ApiHostFactory();
        var endpoints = host.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .ToArray();

        // act
        var stream = endpoints.First(endpoint => RouteOf(endpoint) == "/api/movies/{id:int}/stream");
        var library = endpoints.First(endpoint =>
            RouteOf(endpoint) == "/api/movies/{id:int}" && IsGet(endpoint));

        // assert
        AuthorizeMetadata(stream).ShouldBe(AuthorizeMetadata(library));
        AllowAnonymousMetadata(stream).ShouldBe(AllowAnonymousMetadata(library));
    }

    [Fact]
    public async Task Get_IfRangeWithoutValidators_IsCharacterizedWithoutAConditionalContract()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(2048);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        request.Headers.Range = new RangeHeaderValue(0, 99);
        request.Headers.TryAddWithoutValidation("If-Range", "\"some-validator\"");

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        new[] { HttpStatusCode.OK, HttpStatusCode.PartialContent }.ShouldContain(response.StatusCode);
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotModified);
        body.Length.ShouldBe(
            response.StatusCode == HttpStatusCode.OK ? payload.Length : 100);
    }

    [Fact]
    public async Task Get_ClientCancellationAfterHeaders_DisposesTheStreamWithoutASecondResponse()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(8192);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        await using var host = CreateHost(reader);
        using var client = host.CreateClient();
        using var abort = new CancellationTokenSource();

        // act
        using var response = await client.GetAsync(
            StreamRoute(KnownId), HttpCompletionOption.ResponseHeadersRead, ct);
        await abort.CancelAsync();
        var cancelled = await Record.ExceptionAsync(async () =>
        {
            await using var bodyStream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
            await bodyStream.CopyToAsync(Stream.Null, abort.Token);
        });

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = cancelled;
        reader.OpenedStreams.ShouldHaveSingleItem().Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_ConvertedFixture_StreamsAacWithCopiedVideoFromAbsoluteScratch()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var ffmpeg = FindExecutable("ffmpeg");
        var ffprobe = FindExecutable("ffprobe");
        if (ffmpeg is null || ffprobe is null)
        {
            Assert.Skip("DEV-572 blocker: ffmpeg and ffprobe must be installed for AC3 fixture metadata evidence.");
        }
        using var temporary = new TemporaryDirectory("dev572-ac3-");
        var source = Path.Combine(temporary.Path, "stream-ac3.mkv");
        var scratch = Path.Combine(temporary.Path, "scratch");
        Directory.CreateDirectory(scratch);
        var runner = new SystemProcessRunner();
        var generated = await runner.RunAsync(
            ffmpeg,
            [
                "-nostdin", "-y",
                "-f", "lavfi", "-i", "color=c=black:s=160x90:r=10:d=2",
                "-f", "lavfi", "-i", "sine=frequency=1000:sample_rate=48000:duration=2",
                "-map", "0:v:0", "-map", "1:a:0",
                "-c:v", "mpeg4", "-q:v", "5",
                "-c:a", "ac3", "-b:a", "96k",
                "-shortest", source,
            ],
            TimeSpan.FromMinutes(1),
            ct);
        Assert.SkipUnless(generated.Success,
            $"DEV-572 blocker: named AC3 fixture generation failed ({generated.FailureReason}).");
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(new PrimaryVideo(new LibraryPath(source), new MediaFormat("mkv")));
        var reader = new RecordingReader(source);
        await using var host = WithRealConversion(factory, reader, scratch, ffmpeg);
        // ReSharper disable once UseAwaitUsing -- HttpClient is IDisposable only on this target.
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(Mp4MediaType);
        NoSniff(response).ShouldBe(NoSniffValue);
        body.Length.ShouldBeGreaterThan(0);
        var published = Directory.GetFiles(scratch, "lfstream-*.mp4").ShouldHaveSingleItem();
        Path.GetDirectoryName(published).ShouldBe(scratch);
        var videoCodec = (await RunToolAsync(runner, ffprobe,
            ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=codec_name", "-of", "default=nw=1:nk=1", published], ct))
            .Trim();
        videoCodec.ShouldBe("mpeg4");
        using var audio = JsonDocument.Parse(await RunToolAsync(runner, ffprobe,
            ["-v", "error", "-select_streams", "a:0", "-show_entries", "stream=codec_name,channels", "-of", "json", published], ct));
        audio.RootElement.GetProperty("streams")[0].GetProperty("codec_name").GetString().ShouldBe("aac");
        audio.RootElement.GetProperty("streams")[0].GetProperty("channels").GetInt32().ShouldBe(2);
        var sourceVideoHash = (await RunToolAsync(runner, ffmpeg,
            ["-v", "error", "-i", source, "-map", "0:v:0", "-c", "copy", "-f", "md5", "-"], ct)).Trim();
        var outputVideoHash = (await RunToolAsync(runner, ffmpeg,
            ["-v", "error", "-i", published, "-map", "0:v:0", "-c", "copy", "-f", "md5", "-"], ct)).Trim();
        outputVideoHash.ShouldBe(sourceVideoHash);
    }

    [Fact]
    public async Task Get_ConvertedContent_SupportsFullAndRangeResponsesWithMp4ContentType()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(4096);
        using var library = new TemporaryLibraryFile(payload);
        using var converted = new TemporaryLibraryFile(Payload(4096, 37));
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        conversion.PrepareAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new StreamConversionResult(converted.Path, true)));
        conversion.OpenReadAsync(Arg.Any<StreamConversionResult>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new StreamReaderLease(new FileStream(
                call.ArgAt<StreamConversionResult>(0).Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))));
        await using var host = CreateHost(reader, setupRawConversion: false);
        // ReSharper disable once UseAwaitUsing -- HttpClient is IDisposable only on this target.
        using var client = host.CreateClient();

        // act
        var full = await client.GetAsync(StreamRoute(KnownId), ct);
        var fullBody = await full.Content.ReadAsByteArrayAsync(ct);
        var rangeRequest = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        rangeRequest.Headers.Range = new RangeHeaderValue(100, 199);
        var range = await client.SendAsync(rangeRequest, ct);
        var rangeBody = await range.Content.ReadAsByteArrayAsync(ct);
        var unsatisfiableRequest = new HttpRequestMessage(HttpMethod.Get, StreamRoute(KnownId));
        unsatisfiableRequest.Headers.Range = new RangeHeaderValue(99999, null);
        var unsatisfiable = await client.SendAsync(unsatisfiableRequest, ct);

        // assert
        full.StatusCode.ShouldBe(HttpStatusCode.OK);
        full.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(Mp4MediaType);
        (await AcceptRangesAsync(full)).ShouldBe("bytes");
        NoSniff(full).ShouldBe(NoSniffValue);
        fullBody.ShouldBe(await File.ReadAllBytesAsync(converted.Path, ct));
        range.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        range.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(Mp4MediaType);
        range.Content.Headers.ContentRange.ShouldNotBeNull().ToString().ShouldBe("bytes 100-199/4096");
        rangeBody.ShouldBe((await File.ReadAllBytesAsync(converted.Path, ct))[100..200]);
        unsatisfiable.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
        unsatisfiable.Content.Headers.ContentRange.ShouldNotBeNull().ToString().ShouldBe("bytes */4096");
        NoSniff(unsatisfiable).ShouldBeNull();
        reader.OpenedStreams.Count.ShouldBe(3);
        reader.OpenedStreams.ShouldAllBe(stream => stream.Disposed);
    }

    [Fact]
    public async Task Get_KnownConversionFailure_UsesExistingErrorPathWithoutRawFallbackOrRedirect()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var payload = Payload(256);
        using var library = new TemporaryLibraryFile(payload);
        DetailsReturning(KnownDetails("mp4"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Resolved("mp4"));
        var reader = new RecordingReader(library.Path);
        conversion.PrepareAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("conversion exploded"));
        await using var host = CreateHost(reader, setupRawConversion: false);
        // ReSharper disable once UseAwaitUsing -- HttpClient is IDisposable only on this target.
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(StreamRoute(KnownId), ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        response.Headers.Location.ShouldBeNull();
        body.ShouldNotBe(payload);
        System.Text.Encoding.UTF8.GetString(body).ShouldNotContain("conversion exploded");
        log.Entries.ShouldContain(entry => entry.Level == LogLevel.Error
            && entry.State.Any(pair => pair.Key == "MovieId" && Equals(pair.Value, KnownId)));
    }

    [Fact]
    public async Task Get_ConcurrentSameSource_SharesOneConversion()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory("dev572-concurrent-");
        var source = temporary.WriteSource("movie.mkv", "source");
        var scratch = Path.Combine(temporary.Path, "scratch");
        var converted = Payload(256);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new ScriptedRunner
        {
            Handler = async (executable, arguments, token) =>
            {
                if (executable == "ffprobe")
                {
                    return Ac3Probe();
                }

                await File.WriteAllBytesAsync(arguments[^1], converted, token);
                started.TrySetResult();
                return await release.Task.WaitAsync(token);
            },
        };
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(new PrimaryVideo(new LibraryPath(source), new MediaFormat("mkv")));
        var reader = new RecordingReader(source);
        await using var baseFactory = new ApiHostFactory();
        await using var host = WithRealConversion(baseFactory, reader, scratch, "ffmpeg", runner);
        // ReSharper disable once UseAwaitUsing -- HttpClient is IDisposable only on this target.
        using var client = host.CreateClient();

        // act
        var first = client.GetAsync(StreamRoute(KnownId), ct);
        await WaitForSignalAsync(started.Task, ct);
        var second = client.GetAsync(StreamRoute(KnownId), ct);
        await Task.Delay(50, ct);
        release.TrySetResult(Succeeded());
        var responses = await Task.WhenAll(first, second);

        // assert
        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        runner.Count("ffmpeg").ShouldBe(1);
        foreach (var response in responses)
        {
            (await response.Content.ReadAsByteArrayAsync(ct)).ShouldBe(converted);
        }
    }

    [Fact]
    public async Task Get_DisconnectThenReattachWithinGrace_ReusesTheInFlightConversion()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory("dev572-reattach-");
        var source = temporary.WriteSource("movie.mkv", "source");
        var scratch = Path.Combine(temporary.Path, "scratch");
        var converted = Payload(256);
        var time = new LamuFlix.UnitTests.ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new ScriptedRunner
        {
            Handler = async (executable, arguments, token) =>
            {
                if (executable == "ffprobe")
                {
                    return Ac3Probe();
                }

                await using var registration = token.Register(() => childCancelled.TrySetResult());
                await File.WriteAllBytesAsync(arguments[^1], converted, token);
                started.TrySetResult();
                return await release.Task.WaitAsync(token);
            },
        };
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(new PrimaryVideo(new LibraryPath(source), new MediaFormat("mkv")));
        var reader = new RecordingReader(source);
        await using var baseFactory = new ApiHostFactory(false, timeProvider: time);
        await using var host = WithRealConversion(baseFactory, reader, scratch, "ffmpeg", runner);
        var service = host.Services.GetRequiredService<IStreamConversionService>();
        using var firstWaiter = new CancellationTokenSource();

        // act
        var first = service.PrepareAsync(KnownId, source, firstWaiter.Token);
        await WaitForSignalAsync(started.Task, ct);
        await firstWaiter.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await first);
        await WaitForSignalAsync(time.WaitForPendingTimerAsync(ct), ct);
        var second = service.PrepareAsync(KnownId, source, ct);
        time.Advance(TimeSpan.FromSeconds(2));
        childCancelled.Task.IsCompleted.ShouldBeFalse();
        release.TrySetResult(Succeeded());

        // assert
        (await second).Converted.ShouldBeTrue();
        runner.Count("ffmpeg").ShouldBe(1);
    }

    [Fact]
    public async Task Get_GraceExpiryAtZeroWaiters_CancelsChildAndDeletesPartialThenReconverts()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory("dev572-expiry-");
        var source = temporary.WriteSource("movie.mkv", "source");
        var scratch = Path.Combine(temporary.Path, "scratch");
        var converted = Payload(256);
        var time = new LamuFlix.UnitTests.ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var conversions = 0;
        var partialPath = string.Empty;
        var runner = new ScriptedRunner
        {
            Handler = async (executable, arguments, token) =>
            {
                if (executable == "ffprobe")
                {
                    return Ac3Probe();
                }

                var index = Interlocked.Increment(ref conversions);
                if (index == 1)
                {
                    partialPath = arguments[^1];
                    await using var registration = token.Register(() => childCancelled.TrySetResult());
                    await File.WriteAllBytesAsync(partialPath, Payload(64), token);
                    started.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }

                await File.WriteAllBytesAsync(arguments[^1], converted, token);
                return Succeeded();
            },
        };
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(new PrimaryVideo(new LibraryPath(source), new MediaFormat("mkv")));
        var reader = new RecordingReader(source);
        await using var baseFactory = new ApiHostFactory(false, timeProvider: time);
        await using var host = WithRealConversion(baseFactory, reader, scratch, "ffmpeg", runner);
        var service = host.Services.GetRequiredService<IStreamConversionService>();
        using var firstWaiter = new CancellationTokenSource();

        // act
        var first = service.PrepareAsync(KnownId, source, firstWaiter.Token);
        await WaitForSignalAsync(started.Task, ct);
        await firstWaiter.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await first);
        await WaitForSignalAsync(time.WaitForPendingTimerAsync(ct), ct);
        time.Advance(TimeSpan.FromSeconds(2));
        await WaitForSignalAsync(childCancelled.Task, ct);
        await WaitUntilAsync(() => !File.Exists(partialPath), ct);
        var second = await service.PrepareAsync(KnownId, source, ct);

        // assert
        second.Converted.ShouldBeTrue();
        (await File.ReadAllBytesAsync(second.Path, ct)).ShouldBe(converted);
        runner.Count("ffmpeg").ShouldBe(2);
    }

    [Fact]
    public async Task CompletedAsset_UnderSizePressure_IsNotDeletedWhileAReaderHoldsIt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory("dev572-reader-");
        var source = temporary.WriteSource("movie.mkv", "source");
        var scratch = Path.Combine(temporary.Path, "scratch");
        var runner = new ScriptedRunner
        {
            Handler = (executable, arguments, _) => Task.FromResult(executable == "ffprobe"
                ? Ac3Probe()
                : WriteConversion(arguments[^1])),
        };
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(temporary.Path);
        var service = new StreamConversionService(
            Microsoft.Extensions.Options.Options.Create(new StreamConversionOptions
            {
                ScratchDirectory = scratch,
                ScratchSizeLimitBytes = 1,
            }),
            Microsoft.Extensions.Options.Options.Create(new ThumbnailOptions { FfmpegPath = "ffmpeg" }),
            runner,
            environment,
            Substitute.For<ILogger<StreamConversionService>>());
        var prepared = await service.PrepareAsync(KnownId, source, ct);
        var lease = await service.OpenReadAsync(prepared, ct);

        // act
        await service.CleanupAsync(ct);
        var held = File.Exists(prepared.Path);
        lease.Dispose();
        await service.CleanupAsync(ct);

        // assert
        held.ShouldBeTrue();
        File.Exists(prepared.Path).ShouldBeFalse();
    }

    private void NoConversionInvocations() =>
        conversion.DidNotReceive().PrepareAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

    private WebApplicationFactory<Program> WithRealConversion(
        WebApplicationFactory<Program> baseFactory,
        RecordingReader reader,
        string scratch,
        string ffmpegPath,
        IProcessRunner? runner = null) =>
        baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "false",
                    [$"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.FfmpegPath)}"] = ffmpegPath,
                    [$"{StreamConversionOptions.SectionName}:{nameof(StreamConversionOptions.ScratchDirectory)}"] = scratch,
                }));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(catalog);
                services.AddSingleton(scanner);
                services.AddSingleton<ILibraryFileReader>(reader);
                services.AddSingleton(starter);
                services.AddSingleton(history);
                services.AddSingleton<ILoggerProvider>(new CaptureLoggerProvider(log));
                if (runner is not null)
                {
                    services.RemoveAll<IProcessRunner>();
                    services.AddSingleton(runner);
                }
            });
        });

    private static string? FindExecutable(string name)
    {
        var extension = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, name + extension);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<string> RunToolAsync(
        IProcessRunner runner,
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(executable, arguments, TimeSpan.FromMinutes(1), cancellationToken);
        result.Success.ShouldBeTrue($"{executable} failed with {result.FailureReason}: {result.StandardError}");
        return result.StandardOutput;
    }

    private static async Task WaitForSignalAsync(Task signal, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        await signal.WaitAsync(linked.Token);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        while (!condition())
        {
            await Task.Delay(10, linked.Token);
        }
    }

    private static ProcessResult Ac3Probe() => new(
        true,
        0,
        "{\"streams\":[{\"codec_type\":\"audio\",\"codec_name\":\"ac3\",\"disposition\":{\"default\":1}}]}",
        string.Empty,
        null);

    private static ProcessResult Succeeded() => new(true, 0, string.Empty, string.Empty, null);

    private static ProcessResult WriteConversion(string outputPath)
    {
        File.WriteAllBytes(outputPath, Payload(256));
        return Succeeded();
    }

    private static string RouteOf(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        endpoint.RoutePattern.RawText ?? string.Empty;

    private static bool IsGet(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        endpoint.Metadata
            .GetMetadata<Microsoft.AspNetCore.Routing.IHttpMethodMetadata>()?
            .HttpMethods.Contains("GET", StringComparer.Ordinal) == true;

    private static string[] AuthorizeMetadata(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        [.. endpoint.Metadata
            .GetOrderedMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
            .Select(data => $"{data.Policy}|{data.Roles}|{data.AuthenticationSchemes}")];

    private static bool AllowAnonymousMetadata(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() is not null;

    [Fact]
    public async Task Document_StreamOperation_DocumentsBinary200And206AndExplicit416()
    {
        // arrange
        await using var development = factory.WithWebHostBuilder(
            builder => builder.UseEnvironment(Environments.Development));
        using var client = development.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // act
        using var documentResponse = await client.GetAsync("/openapi/v1.json", ct);
        var document = await documentResponse.Content.ReadAsStringAsync(ct);
        using var parsed = JsonDocument.Parse(document);
        var operation = parsed.RootElement.GetProperty("paths")
            .GetProperty("/api/movies/{id}/stream")
            .GetProperty("get");

        // assert
        documentResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        operation.GetProperty("operationId").GetString().ShouldBe("StreamMovie");
        operation.GetProperty("summary").GetString().ShouldNotBeNullOrWhiteSpace();
        var responses = operation.GetProperty("responses");
        foreach (var status in new[] { "200", "206" })
        {
            responses.GetProperty(status).GetProperty("content")
                .EnumerateObject().Select(content => content.Name)
                .ShouldBe([BinaryMediaType]);
            var schema = ResolveStreamSchema(parsed.RootElement, responses.GetProperty(status)
                .GetProperty("content")
                .GetProperty(BinaryMediaType)
                .GetProperty("schema"));
            schema.GetProperty("type").GetString().ShouldBe("string");
            schema.GetProperty("format").GetString().ShouldBe("binary");
        }

        responses.TryGetProperty("416", out _).ShouldBeTrue();
        foreach (var status in new[] { "400", "403", "404", "409", "500" })
        {
            var problemSchema = responses.GetProperty(status).GetProperty("content")
                .GetProperty(ProblemDetailsJson).GetProperty("schema");
            problemSchema.GetProperty("$ref").GetString().ShouldBe("#/components/schemas/ProblemDetails");
        }
    }

    private static JsonElement ResolveStreamSchema(JsonElement document, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var referenceValue = reference.GetString().ShouldNotBeNull();
        const string ComponentPrefix = "#/components/schemas/";
        referenceValue.ShouldStartWith(ComponentPrefix);
        return document.GetProperty("components").GetProperty("schemas")
            .GetProperty(referenceValue[ComponentPrefix.Length..]);
    }

    private static string StreamRoute(int id) => $"/api/movies/{id}/stream";

    private static string ResolvedPath(string extension) => $"{MovieFolder}/Heat.{extension}";

    private static MovieDetails KnownDetails(string storedFormat) =>
        new(new MovieId(KnownId), "Heat", new LibraryPath(MovieFolder), new MediaFormat(storedFormat), null, false, false, Review.None, EnrichmentStatus.Enriched);

    private static PrimaryVideo Resolved(string extension) =>
        new(new LibraryPath(ResolvedPath(extension)), new MediaFormat(extension));

    private static byte[] Payload(int length, int offset = 0) =>
        [.. Enumerable.Range(0, length).Select(index => (byte)((index + offset) % 251))];

    private static Task<string?> AcceptRangesAsync(HttpResponseMessage response) =>
        Task.FromResult(response.Headers.TryGetValues(AcceptRangesHeader, out var values) ? values.Single() : null);

    private static string? NoSniff(HttpResponseMessage response) =>
        response.Headers.TryGetValues(ContentTypeOptionsHeader, out var values) ? values.Single() : null;

    private void DetailsReturning(MovieDetails details) =>
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(details);

    private WebApplicationFactory<Program> CreateHost(RecordingReader reader, bool setupRawConversion = true)
    {
        if (setupRawConversion)
        {
            conversion.PrepareAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(new StreamConversionResult(call.ArgAt<string>(1), false)));
        }
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "false",
                }));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(catalog);
                services.AddSingleton(scanner);
                services.AddSingleton<ILibraryFileReader>(reader);
                services.AddSingleton(starter);
                services.AddSingleton(history);
                services.AddSingleton(conversion);
                services.AddSingleton<ILoggerProvider>(new CaptureLoggerProvider(log));
            });
        });
    }

    private sealed class RecordingReader(string streamPath) : ILibraryFileReader
    {
        private readonly List<string> sequence = [];

        public IReadOnlyList<string> Sequence => sequence;

        public List<string> FolderAuthorizations { get; } = [];

        public List<string> OpenedPaths { get; } = [];

        public List<TrackingStream> OpenedStreams { get; } = [];

        public int Opens => OpenedPaths.Count;

        public Exception? FolderFailure { get; init; }

        public Exception? OpenFailure { get; init; }

        public Func<Stream> StreamFactory { get; init; } = () => new FileStream(
            streamPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        public void AuthorizeFolder(LibraryPath folder)
        {
            ArgumentNullException.ThrowIfNull(folder);
            sequence.Add("authorize-folder");
            FolderAuthorizations.Add(folder.Value);
            if (FolderFailure is not null)
            {
                throw FolderFailure;
            }
        }

        public Stream OpenRead(LibraryPath file)
        {
            ArgumentNullException.ThrowIfNull(file);
            sequence.Add("open");
            OpenedPaths.Add(file.Value);
            if (OpenFailure is not null)
            {
                throw OpenFailure;
            }

            var tracking = new TrackingStream(StreamFactory());
            OpenedStreams.Add(tracking);
            return tracking;
        }

        public IReadOnlyList<string> EnumerateFiles(LibraryPath folder)
        {
            ArgumentNullException.ThrowIfNull(folder);
            return [];
        }
    }

    private sealed class TrackingStream(Stream inner) : Stream
    {
        public bool Disposed { get; private set; }

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disposed = true;
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class FailingStream(byte[] payload, int failAfterBytes, Exception failure) : Stream
    {
        private int consumed;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => payload.Length;

        public override long Position
        {
            get => consumed;
            set => consumed = (int)value;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = failAfterBytes - consumed;
            if (remaining <= 0)
            {
                throw failure;
            }

            var toCopy = Math.Min(Math.Min(count, remaining), payload.Length - consumed);
            Array.Copy(payload, consumed, buffer, offset, toCopy);
            consumed += toCopy;
            return toCopy;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            consumed = (int)offset;
            return consumed;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CaptureLog
    {
        private readonly ConcurrentQueue<LogRecord> entries = new();

        public IReadOnlyList<LogRecord> Entries => [.. entries];

        public void Add(LogRecord record) => entries.Enqueue(record);
    }

    private sealed record LogRecord(
        LogLevel Level,
        IReadOnlyList<KeyValuePair<string, object?>> State,
        string Message);

    private sealed class CaptureLoggerProvider(CaptureLog log) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(log, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class CaptureLogger(CaptureLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!category.EndsWith("StreamEndpoints", StringComparison.Ordinal))
            {
                return;
            }

            var pairs = new List<KeyValuePair<string, object?>>();
            if (state is IEnumerable<KeyValuePair<string, object>> structured)
            {
                pairs.AddRange(structured.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)));
            }

            log.Add(new LogRecord(logLevel, pairs, formatter(state, exception)));
        }
    }

    private sealed class ScriptedRunner : IProcessRunner
    {
        private readonly Lock gate = new();

        private readonly List<(string Executable, IReadOnlyList<string> Arguments)> invocations = [];

        public Func<string, IReadOnlyList<string>, CancellationToken, Task<ProcessResult>> Handler { get; set; } =
            (_, _, _) => Task.FromResult(new ProcessResult(false, null, string.Empty, string.Empty, "NonZeroExit"));

        public Task<ProcessResult> RunAsync(
            string executable,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                invocations.Add((executable, arguments));
            }

            return Handler(executable, arguments, cancellationToken);
        }

        public int Count(string executable)
        {
            lock (gate)
            {
                return invocations.Count(invocation => invocation.Executable == executable);
            }
        }
    }

    private sealed class TemporaryDirectory(string prefix) : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            prefix + Guid.NewGuid().ToString("N"));

        public string WriteSource(string name, string content)
        {
            Directory.CreateDirectory(Path);
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort test cleanup.
            }
        }
    }

    private sealed class TemporaryLibraryFile : IDisposable
    {
        public TemporaryLibraryFile(byte[] contents)
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-dev456-stream-" + Guid.NewGuid().ToString("N") + ".mp4");
            File.WriteAllBytes(Path, contents);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _ = exception;
            }
        }
    }
}
