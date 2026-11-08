using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Api.Endpoints;

internal static class StreamEndpoints
{
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string NoSniffValue = "nosniff";
    private const string BinaryContentType = "application/octet-stream";
    private const string ForbiddenTitle = "Forbidden";
    private const string ForbiddenType = "https://tools.ietf.org/html/rfc9110#section-15.5.4";
    private const string LoggerCategory = "LamuFlix.Api.Endpoints.StreamEndpoints";

    internal static RouteGroupBuilder MapStreamEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies/{id:int}/stream", StreamMovieAsync)
            .WithName("StreamMovie")
            .WithSummary("Stream the primary video file of a movie.")
            .Produces(StatusCodes.Status200OK, typeof(Stream), BinaryContentType)
            .Produces(StatusCodes.Status206PartialContent, typeof(Stream), BinaryContentType)
            .Produces(StatusCodes.Status416RangeNotSatisfiable)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return api;
    }

    private static async Task<IResult> StreamMovieAsync(
        int id,
        IQueryHandler<GetMovieDetailsQuery, MovieDetails> handler,
        IMediaLibraryScanner scanner,
        ILibraryFileReader reader,
        FileExtensionContentTypeProvider contentTypeProvider,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new BadHttpRequestException($"Movie id '{id}' is not a valid movie identifier.");
        }

        var logger = loggerFactory.CreateLogger(LoggerCategory);

        try
        {
            var details = await handler.HandleAsync(new GetMovieDetailsQuery(movieId), cancellationToken);
            reader.AuthorizeFolder(details.Path);
            var primary = scanner.ResolvePrimaryVideo(details.Path);
            var contentType = ResolveContentType(contentTypeProvider, primary.Format);
            var stream = reader.OpenRead(primary.Path);
            try
            {
                httpContext.Response.OnStarting(AddNoSniffOnBinarySuccessAsync, httpContext);
                return TypedResults.Stream(
                    new StreamDelivery(stream, movieId.Value, logger),
                    contentType,
                    enableRangeProcessing: true);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
        catch (LibraryFileAccessDeniedException exception)
        {
            logger.LogWarning(
                "Library file policy denied the stream request for movie {MovieId} with {Failure}.",
                movieId.Value,
                exception.GetType().Name);
            return TypedResults.Problem(
                title: ForbiddenTitle,
                type: ForbiddenType,
                statusCode: StatusCodes.Status403Forbidden);
        }
        catch (Exception exception) when (exception is NotFoundException
            or InvalidTransitionException
            or UnauthorizedAccessException
            or IOException)
        {
            logger.LogWarning(
                "The stream request for movie {MovieId} failed with {Failure} before the response started.",
                movieId.Value,
                exception.GetType().Name);
            throw;
        }
    }

    private static Task AddNoSniffOnBinarySuccessAsync(object state)
    {
        var httpContext = (HttpContext)state;
        if (httpContext.Response.StatusCode is StatusCodes.Status200OK or StatusCodes.Status206PartialContent)
        {
            httpContext.Response.Headers[ContentTypeOptionsHeader] = NoSniffValue;
        }

        return Task.CompletedTask;
    }

    private static string ResolveContentType(FileExtensionContentTypeProvider provider, MediaFormat format) =>
        provider.TryGetContentType("." + format.Extension, out var contentType) ? contentType : BinaryContentType;

    private sealed class StreamDelivery(Stream inner, int movieId, ILogger logger) : Stream
    {
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return inner.Read(buffer, offset, count);
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException
                or OperationCanceledException)
            {
                LogFailure(exception);
                throw;
            }
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await inner.ReadAsync(buffer, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException
                or OperationCanceledException)
            {
                LogFailure(exception);
                throw;
            }
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private void LogFailure(Exception exception) =>
            logger.LogWarning(
                "Stream delivery for movie {MovieId} failed after the response started with {Failure}.",
                movieId,
                exception.GetType().Name);
    }
}
