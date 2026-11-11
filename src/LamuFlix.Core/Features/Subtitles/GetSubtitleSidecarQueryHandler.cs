using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class GetSubtitleSidecarQueryHandler(
    IMovieCatalog catalog,
    IMediaLibraryScanner scanner,
    ILibraryFileReader reader,
    ISubtitleSidecarStore store,
    IOptions<LibraryOptions> libraryOptions,
    ILogger<GetSubtitleSidecarQueryHandler> logger)
    : IQueryHandler<GetSubtitleSidecarQuery, SubtitleSidecarResult>
{
    public async Task<SubtitleSidecarResult> HandleAsync(
        GetSubtitleSidecarQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        SubtitleDiagnostics.TagMovie(query.Id);
        using var activity = SubtitleTelemetry.StartServe();
        var started = Stopwatch.GetTimestamp();
        try
        {
            var target = await SubtitlePreparationTarget.ResolveAsync(
                catalog, scanner, reader, query.Id, cancellationToken);

            var fileName = SubtitleSidecarName.For(target.PrimaryFileName);
            var content = await store.ReadAsync(
                target.Folder,
                target.PrimaryFileName,
                libraryOptions.Value.MaxSubtitleInputBytes,
                cancellationToken);

            SubtitleTelemetry.Complete(
                activity, query.Id, TelemetryConstants.SucceededOutcome, Stopwatch.GetElapsedTime(started));
            logger.LogInformation("Subtitle sidecar served for {MovieId}.", query.Id.Value);
            return new SubtitleSidecarResult(content, fileName);
        }
        catch (Exception exception)
        {
            SubtitleDiagnostics.TagFailure(exception);
            SubtitleTelemetry.Fail(activity, query.Id, exception);
            throw;
        }
    }
}
