using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
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
        var folderPath = CreateOwnedImportFolder();
        var client = await StartHostAsync(InstallOmdbStub);

        // act
        var response = await client.PostAsJsonAsync(ImportRoute, new { folderPath }, cancellationToken);
        var importBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        importBody.ShouldBe(string.Empty);
        var location = response.Headers.Location.ShouldNotBeNull();
        location.IsAbsoluteUri.ShouldBeFalse();
        var id = ParseImportedId(location.OriginalString);

        // assert
        var persisted = await ReadPersistedMovieAsync(id, cancellationToken);
        persisted.ShouldNotBeNull();
        persisted.Title.ShouldBe(FolderTitle);
        persisted.Path.ShouldBe(folderPath);
        persisted.Format.ShouldBe(ExpectedFormat);

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

        // assert
        var measured = Fixture.Server.LogEntries.ShouldHaveSingleItem().RequestMessage.ShouldNotBeNull();
        measured.Method.ShouldBe("GET");
        measured.AbsolutePath.ShouldBe("/");
        var query = measured.Query.ShouldNotBeNull();
        query.Keys.ShouldBe(["apikey", "t", "type"], ignoreOrder: true);
        query["apikey"].ShouldContain(MetadataProviderProbe.SentinelApiKey);
        query["t"].ShouldContain(FolderTitle);
        query["type"].ShouldContain("movie");
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

    private sealed record PersistedImport(string Title, string Path, string Format);

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
