using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndImportTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string ImportRoute = "/api/movies/import";
    private const string DetailsRoutePrefix = "/api/movies/";
    private const string ApplicationJson = "application/json";
    private const string FolderName = "Blade Runner (1982)";
    private const string FolderTitle = "Blade Runner";
    private const string VideoFileName = "blade runner.mkv";
    private const string ExpectedFormat = "mkv";
    private const string ExpectedSynopsis = "A blade runner must pursue and terminate four replicants.";
    private const string ExpectedImdbId = "tt0083658";
    private const int ExpectedRuntime = 117;
    private const int ExpectedYear = 1982;
    private const decimal ExpectedImdbRating = 8.1m;
    private const int TeardownDeleteAttempts = 5;

    private static readonly byte[] VideoBytes = [0x1A, 0x45, 0xDF, 0xA3];
    private static readonly TimeSpan TeardownRetryBaseDelay = TimeSpan.FromMilliseconds(40);
    private static readonly TimeSpan GateArrivalTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TraceAncestryTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TracePollInterval = TimeSpan.FromMilliseconds(50);

    private static readonly string OmdbBody = $$"""
        {
          "Response": "True",
          "Title": "{{FolderTitle}}",
          "Year": "{{ExpectedYear}}",
          "Runtime": "{{ExpectedRuntime}} min",
          "Plot": "{{ExpectedSynopsis}}",
          "imdbRating": "{{ExpectedImdbRating.ToString(CultureInfo.InvariantCulture)}}",
          "imdbID": "{{ExpectedImdbId}}"
        }
        """;

    private string? ownedTempRoot;

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await DeleteOwnedTempRootAsync();
    }

    [Fact]
    public async Task ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        while (Activity.Current is not null)
        {
            Activity.Current.Stop();
        }

        var gate = new ApiImportEnrichmentGate();
        using var capture = new ApiImportTraceCapture();
        var folderPath = CreateOwnedImportFolder();
        try
        {
            // arrange
            var client = await StartHostAsync(InstallOmdbStub, gate.InstallInto);
            var requestTraceId = ActivityTraceId.CreateRandom();
            var traceparent = $"00-{requestTraceId.ToHexString()}-{ActivitySpanId.CreateRandom().ToHexString()}-01";
            var tracestate = $"lamuflix-dev318={ActivitySpanId.CreateRandom().ToHexString()}";
            using var request = new HttpRequestMessage(HttpMethod.Post, ImportRoute);
            request.Content = JsonContent.Create(new { folderPath });
            request.Headers.Add(TraceContextCarrier.TraceParentHeader, traceparent);
            request.Headers.Add(TraceContextCarrier.TraceStateHeader, tracestate);
            Activity.Current.ShouldBeNull("The import POST must carry only its unique traceparent with no ambient test Activity.");

            // act
            using var response = await client.SendAsync(request, cancellationToken);
            var importBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // assert
            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            importBody.ShouldBe(string.Empty);
            var location = response.Headers.Location.ShouldNotBeNull();
            location.IsAbsoluteUri.ShouldBeFalse();
            var id = ParseImportedId(location.OriginalString);

            // act
            ProcessEnrichmentCommand arrival;
            try
            {
                arrival = await gate.Arrival.WaitAsync(GateArrivalTimeout, cancellationToken);
            }
            catch (TimeoutException exception)
            {
                throw new InvalidOperationException($"The enrichment gate never observed a consumer arrival within {(int)GateArrivalTimeout.TotalSeconds} seconds; the production EnrichmentConsumer did not reach the gate.", exception);
            }

            // assert
            arrival.MovieId.ShouldBe(new MovieId(id));
            var persisted = await ReadPersistedMovieAsync(id, cancellationToken);
            persisted.ShouldNotBeNull();
            persisted.Title.ShouldBe(FolderTitle);
            persisted.Path.ShouldBe(folderPath);
            persisted.Format.ShouldBe(ExpectedFormat);
            var pending = await ReadFreshStatusAsync(id, cancellationToken);
            pending.ShouldBe(EnrichmentStatus.Pending);
            gate.Release();

            // act
            await WaitForStatusAsync(new MovieId(id), EnrichmentStatus.Enriched);
            var details = await client.GetAsync($"{DetailsRoutePrefix}{id}", cancellationToken);
            var detailsBody = await details.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

            // assert
            details.StatusCode.ShouldBe(HttpStatusCode.OK);
            details.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
            ShouldCarryExactly(detailsBody, "id", "title", "path", "format", "metadata");
            detailsBody.GetProperty("id").GetInt32().ShouldBe(id);
            detailsBody.GetProperty("title").GetString().ShouldBe(FolderTitle);
            detailsBody.GetProperty("path").GetString().ShouldBe(folderPath);
            detailsBody.GetProperty("format").GetString().ShouldBe(ExpectedFormat);
            var metadata = detailsBody.GetProperty("metadata");
            ShouldCarryExactly(metadata, "title", "synopsis", "releaseYear", "runtime", "imdbRating", "imdbId");
            metadata.GetProperty("title").GetString().ShouldBe(FolderTitle);
            metadata.GetProperty("synopsis").GetString().ShouldBe(ExpectedSynopsis);
            metadata.GetProperty("releaseYear").GetInt32().ShouldBe(ExpectedYear);
            metadata.GetProperty("runtime").GetInt32().ShouldBe(ExpectedRuntime);
            metadata.GetProperty("imdbRating").GetDecimal().ShouldBe(ExpectedImdbRating);
            metadata.GetProperty("imdbId").GetString().ShouldBe(ExpectedImdbId);

            ShouldHaveSingleMeasuredProviderRequest();
            // assert
            var ancestry = await WaitForTraceAncestryAsync(capture, requestTraceId, cancellationToken);
            ShouldProveTraceAncestry(ancestry, requestTraceId, tracestate);
        }
        finally
        {
            gate.Release();
        }
    }

    private static void ShouldProveTraceAncestry(TraceAncestry ancestry, ActivityTraceId requestTraceId, string tracestate)
    {
        ancestry.Server.TraceId.ShouldBe(requestTraceId);
        ancestry.Enqueue.TraceId.ShouldBe(requestTraceId);
        ancestry.Publish.TraceId.ShouldBe(requestTraceId);
        ancestry.Consumer.TraceId.ShouldBe(requestTraceId);
        ancestry.ProcessingHandler.TraceId.ShouldBe(requestTraceId);
        ancestry.Lookup.TraceId.ShouldBe(requestTraceId);
        foreach (var import in ancestry.ImportAncestry)
        {
            import.TraceId.ShouldBe(requestTraceId);
            ShouldDescendFrom(import, ancestry.Server, ancestry.BySpanId);
        }

        ShouldDescendFrom(ancestry.Enqueue, ancestry.Server, ancestry.BySpanId);
        ancestry.Publish.ParentSpanId.ShouldBe(ancestry.Enqueue.SpanId);
        ancestry.Consumer.ParentSpanId.ShouldBe(ancestry.Publish.SpanId);
        ShouldDescendFrom(ancestry.ProcessingHandler, ancestry.Consumer, ancestry.BySpanId);
        ShouldDescendFrom(ancestry.Lookup, ancestry.ProcessingHandler, ancestry.BySpanId);
        ancestry.Consumer.Kind.ShouldBe(ActivityKind.Consumer);
        ancestry.Enqueue.TraceStateString.ShouldBe(tracestate);
        ancestry.Consumer.TraceStateString.ShouldBe(tracestate);
        ancestry.Consumer.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_TransientFileLock_DeletesOwnedFolderAfterRetry()
    {
        // arrange
        var folder = CreateOwnedImportFolder();
        var root = Directory.GetParent(folder).ShouldNotBeNull().FullName;
        var lockedFile = Path.Combine(folder, "locked.bin");
        File.WriteAllBytes(lockedFile, [0x01]);
        var stream = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);
        var deleting = Task.Run(() => DisposeAsync().AsTask(), TestContext.Current.CancellationToken);
        try
        {
            // act
            await Task.Delay(TimeSpan.FromMilliseconds(150), TestContext.Current.CancellationToken);
            await stream.DisposeAsync();
            await deleting.WaitAsync(TestContext.Current.CancellationToken);

            // assert
            Directory.Exists(root).ShouldBeFalse();
        }
        finally
        {
            await stream.DisposeAsync();
            if (!deleting.IsCompleted)
            {
                await deleting.WaitAsync(TestContext.Current.CancellationToken);
            }

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DisposeAsync_FileLockedForEveryAttempt_ReportsFailureAndLeavesTheOwnedFolder()
    {
        // arrange
        var folder = CreateOwnedImportFolder();
        var root = Directory.GetParent(folder).ShouldNotBeNull().FullName;
        var lockedFile = Path.Combine(folder, "locked.bin");
        File.WriteAllBytes(lockedFile, [0x01]);
        var block = OwnedFolderDeleteBlock.Hold(folder, lockedFile);
        try
        {
            // act
            var error = await Should.ThrowAsync<IOException>(() => DisposeAsync().AsTask());

            // assert
            var failure = error.InnerException.ShouldNotBeNull();
            error.Message.ShouldBe(DescribeImportTeardownFailure(TeardownDeleteAttempts, failure));
            error.Message.ShouldNotContain(root);
            Directory.Exists(root).ShouldBeTrue();
        }
        finally
        {
            await block.DisposeAsync();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private void ShouldHaveSingleMeasuredProviderRequest()
    {
        var measuredEntry = Fixture.Server.LogEntries
            .Where(entry => HasQueryValue(entry, "apikey", MetadataProviderProbe.SentinelApiKey)
                && HasQueryValue(entry, "t", FolderTitle)
                && HasQueryValue(entry, "type", "movie"))
            .ShouldHaveSingleItem("Expected exactly one measured OMDb GET filtered by the scenario sentinel apikey and selected t/type values; a duplicate matching request fails this positive-path count.");
        var measured = measuredEntry.RequestMessage.ShouldNotBeNull();
        measured.Method.ShouldBe("GET");
        measured.AbsolutePath.ShouldBe("/");
        var query = measured.Query.ShouldNotBeNull();
        query.Keys.ShouldBe(["apikey", "t", "type"], ignoreOrder: true);
        query["apikey"].ShouldContain(MetadataProviderProbe.SentinelApiKey);
        query["t"].ShouldContain(FolderTitle);
        query["type"].ShouldContain("movie");
    }

    private static bool HasQueryValue(WireMock.Logging.ILogEntry entry, string key, string value) =>
        entry.RequestMessage?.Query?.Any(pair =>
            string.Equals(pair.Key, key, StringComparison.Ordinal) && pair.Value.Contains(value)) == true;

    private static void InstallOmdbStub(WireMockServer server) =>
        server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(OmdbBody));

    private static int ParseImportedId(string relativeLocation)
    {
        relativeLocation.ShouldStartWith(DetailsRoutePrefix);
        var idText = relativeLocation[DetailsRoutePrefix.Length..];
        int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            .ShouldBe(true, $"import Location '{relativeLocation}' does not end in a generated movie id");
        return id;
    }

    private static void ShouldCarryExactly(JsonElement element, params string[] names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private static readonly (string Name, Func<Activity, bool> Predicate)[] RequiredTraceSpans =
    [
        ("Enrichment.Enqueue span", IsEnqueueSpan),
        ("publisher publish span", IsPublishSpan),
        ("Enrichment.Process consumer span", IsConsumerSpan),
        ("processing handler span", IsProcessingHandlerSpan),
        ("Metadata.Lookup span", IsLookupSpan),
    ];

    private static bool IsEnqueueSpan(Activity activity) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName == TelemetryConstants.EnrichmentEnqueue;

    private static bool IsPublishSpan(Activity activity) =>
        activity.Source.Name == TelemetryConstants.RabbitMqPublisherActivitySourceName;

    private static bool IsConsumerSpan(Activity activity) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName == TelemetryConstants.EnrichmentProcess;

    private static bool IsProcessingHandlerSpan(Activity activity) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName == nameof(ProcessEnrichmentCommand);

    private static bool IsLookupSpan(Activity activity) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName == TelemetryConstants.MetadataLookup;

    private static bool IsImportAncestrySpan(Activity activity) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName != TelemetryConstants.EnrichmentEnqueue
        && activity.OperationName != TelemetryConstants.EnrichmentProcess
        && activity.OperationName != nameof(ProcessEnrichmentCommand)
        && activity.OperationName != TelemetryConstants.MetadataLookup;

    private static async Task<TraceAncestry> WaitForTraceAncestryAsync(
        ApiImportTraceCapture capture,
        ActivityTraceId requestTraceId,
        CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TraceAncestryTimeout;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            var ancestry = FindTraceAncestry(capture.Stopped, requestTraceId);
            if (ancestry is not null)
            {
                return ancestry;
            }

            await Task.Delay(TracePollInterval, cancellationToken);
        }

        var final = FindTraceAncestry(capture.Stopped, requestTraceId);
        if (final is not null)
        {
            return final;
        }

        throw new InvalidOperationException(
            $"The request trace {requestTraceId} did not produce the full span ancestry within {(int)TraceAncestryTimeout.TotalSeconds} seconds. " +
            DescribeTraceShortfall(capture.Stopped, requestTraceId));
    }

    private static TraceAncestry? FindTraceAncestry(IReadOnlyList<Activity> stopped, ActivityTraceId requestTraceId)
    {
        var trace = stopped.Where(activity => activity.TraceId == requestTraceId).ToArray();
        var matches = new Activity[RequiredTraceSpans.Length];
        for (var index = 0; index < RequiredTraceSpans.Length; index++)
        {
            var count = 0;
            foreach (var activity in trace)
            {
                if (RequiredTraceSpans[index].Predicate(activity))
                {
                    count++;
                    matches[index] = activity;
                }
            }

            if (count != 1)
            {
                return null;
            }
        }

        var importAncestry = trace.Where(IsImportAncestrySpan).ToArray();
        if (importAncestry.Length == 0)
        {
            return null;
        }

        var bySpanId = trace.ToDictionary(activity => activity.SpanId.ToHexString());
        var server = FindServerAncestor(matches[0], bySpanId);
        if (server is null)
        {
            return null;
        }

        return new TraceAncestry(
            server,
            matches[0],
            matches[1],
            matches[2],
            matches[3],
            matches[4],
            importAncestry,
            bySpanId);
    }

    private static Activity? FindServerAncestor(Activity enqueue, IReadOnlyDictionary<string, Activity> bySpanId)
    {
        var current = enqueue;
        for (var hops = 0; hops <= bySpanId.Count; hops++)
        {
            if (!bySpanId.TryGetValue(current.ParentSpanId.ToHexString(), out var parent))
            {
                return null;
            }

            if (parent.Source.Name == ApiImportTraceCapture.AspNetCoreSourceName)
            {
                return parent;
            }

            current = parent;
        }

        return null;
    }

    private static string DescribeTraceShortfall(IReadOnlyList<Activity> stopped, ActivityTraceId requestTraceId)
    {
        var trace = stopped.Where(activity => activity.TraceId == requestTraceId).ToArray();
        var counts = RequiredTraceSpans
            .Select(span => $"{span.Name}={trace.Count(span.Predicate)}")
            .Append($"import ancestry spans={trace.Count(IsImportAncestrySpan)}")
            .Append($"API server spans={trace.Count(activity => activity.Source.Name == ApiImportTraceCapture.AspNetCoreSourceName)}");
        var observed = trace.Select(activity => $"{activity.Source.Name}:{activity.OperationName}");
        return $"Expected one of each required span: {string.Join(", ", counts)}. " +
            $"Observed {trace.Length} stopped spans with the request trace id: {string.Join(", ", observed)}.";
    }

    private static void ShouldDescendFrom(
        Activity activity,
        Activity ancestor,
        IReadOnlyDictionary<string, Activity> bySpanId)
    {
        var current = activity;
        for (var hops = 0; hops <= bySpanId.Count; hops++)
        {
            if (!bySpanId.TryGetValue(current.ParentSpanId.ToHexString(), out var parent))
            {
                break;
            }

            if (parent.SpanId == ancestor.SpanId)
            {
                return;
            }

            current = parent;
        }

        throw new InvalidOperationException(
            $"'{activity.OperationName}' ({activity.SpanId}) does not descend from '{ancestor.OperationName}' ({ancestor.SpanId}).");
    }

    private string CreateOwnedImportFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lamuflix-dev313-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        ownedTempRoot = root;
        var folder = Path.Combine(root, FolderName);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, VideoFileName), VideoBytes);
        return folder;
    }

    private async Task DeleteOwnedTempRootAsync()
    {
        if (ownedTempRoot is not { } root)
        {
            return;
        }

        ownedTempRoot = null;
        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= TeardownDeleteAttempts; attempt++)
        {
            lastFailure = DeleteOwnedFolderOnce(root);
            if (lastFailure is null)
            {
                return;
            }

            WriteImportTeardownDiagnostic(attempt, lastFailure);
            if (attempt < TeardownDeleteAttempts)
            {
                await Task.Delay(TeardownRetryBaseDelay * attempt);
            }
        }

        if (lastFailure is null)
        {
            return;
        }

        throw new IOException(DescribeImportTeardownFailure(TeardownDeleteAttempts, lastFailure), lastFailure);
    }

    private static Exception? DeleteOwnedFolderOnce(string root)
    {
        try
        {
            Directory.Delete(root, recursive: true);
            return null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            return ex is DirectoryNotFoundException ? null : ex;
        }
    }

    private static void WriteImportTeardownDiagnostic(int attempt, Exception failure) =>
        TestContext.Current.TestOutputHelper?.WriteLine(DescribeImportTeardownAttempt(attempt, failure));

    private static string DescribeImportTeardownFailure(int attempts, Exception failure) =>
        $"Import teardown left the owned temporary folder in place after {attempts} attempts. {DescribeImportTeardownAttempt(attempts, failure)}";

    private static string DescribeImportTeardownAttempt(int attempt, Exception failure) =>
        $"attempt {attempt} failed: {failure.GetType().Name} HResult=0x{failure.HResult:X8}";

    private async Task<PersistedImport?> ReadPersistedMovieAsync(int id, CancellationToken cancellationToken)
    {
        var movieId = new MovieId(id);
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var record = await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => new
            {
                movie.Title,
                Path = movie.LibraryPath.Value,
                Format = movie.Format.Extension
            })
            .SingleOrDefaultAsync(cancellationToken);
        return record is null
            ? null
            : new PersistedImport(record.Title, record.Path, record.Format);
    }

    private async Task<EnrichmentStatus?> ReadFreshStatusAsync(int id, CancellationToken cancellationToken)
    {
        var movieId = new MovieId(id);
        await using var context = Fixture.Postgres.CreateMigratedContext();
        return await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => (EnrichmentStatus?)movie.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record PersistedImport(string Title, string Path, string Format);

    private sealed record TraceAncestry(
        Activity Server,
        Activity Enqueue,
        Activity Publish,
        Activity Consumer,
        Activity ProcessingHandler,
        Activity Lookup,
        IReadOnlyList<Activity> ImportAncestry,
        IReadOnlyDictionary<string, Activity> BySpanId);

    private sealed class OwnedFolderDeleteBlock : IAsyncDisposable
    {
        private const UnixFileMode UnixDirectoryWithoutWrite = UnixFileMode.UserRead | UnixFileMode.UserExecute;
        private const UnixFileMode UnixDirectoryWritable =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

        private readonly FileStream? stream;
        private readonly string? unixFolder;

        private OwnedFolderDeleteBlock(FileStream? stream, string? unixFolder)
        {
            this.stream = stream;
            this.unixFolder = unixFolder;
        }

        public static OwnedFolderDeleteBlock Hold(string folder, string lockedFile)
        {
            if (OperatingSystem.IsWindows())
            {
                return new OwnedFolderDeleteBlock(
                    new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None),
                    unixFolder: null);
            }

            new DirectoryInfo(folder).UnixFileMode = UnixDirectoryWithoutWrite;
            return new OwnedFolderDeleteBlock(stream: null, folder);
        }

        public async ValueTask DisposeAsync()
        {
            if (stream is not null)
            {
                await stream.DisposeAsync();
            }

            if (unixFolder is not null)
            {
                new DirectoryInfo(unixFolder).UnixFileMode = UnixDirectoryWritable;
            }
        }
    }
}
