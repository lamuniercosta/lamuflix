using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Playback;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiEndToEndPlaybackTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture), IClassFixture<ApiEndToEndFixture>
{
    private const string ProblemDetailsJson = "application/problem+json";
    private const string PlayerPath = "C:/players/vlc.exe";
    private const string FolderName = "Heat (1995)";
    private const string OriginalVideo = "Heat.avi";
    private const string ReplacementVideo = "Heat.mkv";
    private const int TeardownDeleteAttempts = 5;

    private static readonly byte[] VideoBytes = [0x1A, 0x45, 0xDF, 0xA3];

    private static readonly string OmdbBody = """
        {
          "Response": "True",
          "Type": "movie",
          "Title": "Heat",
          "Year": "1995",
          "Runtime": "170 min",
          "Plot": "A crew of thieves is hunted by a detective.",
          "Genre": "Crime, Drama",
          "Director": "Michael Mann",
          "Writer": "Michael Mann",
          "Actors": "Al Pacino, Robert De Niro",
          "Metascore": "76",
          "imdbRating": "8.3",
          "imdbID": "tt0113277"
        }
        """;

    private string? ownedTempRoot;

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await DeleteOwnedTempRootAsync();
    }

    [Fact]
    public async Task PlayMovie_LocalPlayDisabled_ReturnsForbiddenProblemWithoutProcessStart()
    {
        // arrange
        var starter = Substitute.For<IProcessStarter>();
        var client = await StartHostAsync(configureTestServices: services =>
        {
            services.RemoveAll<IProcessStarter>();
            services.AddSingleton(starter);
        });
        var movie = await SeedAsync(MovieCatalogSeed.Create("Heat", "play-heat"));
        var id = movie.Id.ShouldNotBeNull().Value;
        var resolved = Services.GetServices<IProcessStarter>().ToList();
        resolved.ShouldHaveSingleItem().ShouldBeSameAs(starter);
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(id), null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        body.ShouldNotContain("LocalPlay is disabled");
        body.ShouldNotContain(nameof(FeatureDisabledException));
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public async Task PlayMovie_ImportedThenSwappedAndRescanned_LaunchesTheResolvedFileAndRecordsHistory()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = CreateOwnedImportFolder();
        var starter = Substitute.For<IProcessStarter>();
        var client = await StartHostAsync(
            InstallOmdbStub,
            services =>
            {
                services.RemoveAll<IProcessStarter>();
                services.AddSingleton(starter);
            },
            LocalPlaySettings(".mkv").Concat(LibraryRootSettings()));
        var id = await ImportAsync(client, FolderName, cancellationToken);
        SwapVideoFile(folder);

        using var rescan = await client.PostAsync(RescanRoute(id), null, cancellationToken);
        rescan.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rescanBody = await rescan.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        rescanBody.GetProperty("changed").GetBoolean().ShouldBeTrue();
        rescanBody.GetProperty("oldFormat").GetString().ShouldBe("avi");
        rescanBody.GetProperty("newFormat").GetString().ShouldBe("mkv");

        // act
        using var play = await client.PostAsync(PlayRoute(id), null, cancellationToken);

        // assert
        play.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var resolvedFile = Path.Combine(folder, ReplacementVideo);
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
            info.FileName == PlayerPath
            && info.ArgumentList.Count == 1
            && info.ArgumentList[0] == resolvedFile));
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var events = await context.PlaybackEvents.AsNoTracking()
            .Where(playbackEvent => playbackEvent.MovieId == new MovieId(id))
            .ToListAsync(cancellationToken);
        events.ShouldHaveSingleItem().PlayedAt.ShouldBe(Clock.GetUtcNow(), TimeSpan.FromMilliseconds(1));
        (await ReadFormatAsync(id, cancellationToken)).ShouldBe("mkv");
    }

    [Fact]
    public async Task PlayMovie_SelectedFileMissing_ReturnsSafeNotFoundWithoutProcessStartOrHistory()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        CreateOwnedImportFolder();
        var missingFile = Path.Combine(ownedTempRoot.ShouldNotBeNull(), FolderName, "Absent.mkv");
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(new PrimaryVideo(new LibraryPath(missingFile), new MediaFormat("mkv")));
        var starter = Substitute.For<IProcessStarter>();
        var client = await StartHostAsync(
            InstallOmdbStub,
            services =>
            {
                services.RemoveAll<IMediaLibraryScanner>();
                services.AddSingleton(scanner);
                services.RemoveAll<IProcessStarter>();
                services.AddSingleton(starter);
            },
            LocalPlaySettings(".mkv").Concat(LibraryRootSettings()));
        var movie = await SeedAsync(MovieCatalogSeed.Create("Heat", "play-missing-file"));
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        using var play = await client.PostAsync(PlayRoute(id), null, cancellationToken);
        var body = await play.Content.ReadAsStringAsync(cancellationToken);

        // assert
        play.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        play.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("title").GetString().ShouldBe("Not Found");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        body.ShouldNotContain("Not found.");
        body.ShouldNotContain(nameof(NotFoundException));
        body.ShouldNotContain("Absent.mkv");
        body.ShouldNotContain(ownedTempRoot);
        body.ShouldNotContain(PlayerPath);
        body.ShouldNotContain("127.0.0.1");
        body.ShouldNotContain("X-Forwarded-For");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var events = await context.PlaybackEvents.AsNoTracking()
            .Where(playbackEvent => playbackEvent.MovieId == new MovieId(id))
            .ToListAsync(cancellationToken);
        events.ShouldBeEmpty();
    }

    private static async Task<int> ImportAsync(HttpClient client, string folder, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("/api/movies/import", new { folderPath = folder }, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var location = response.Headers.Location.ShouldNotBeNull().OriginalString;
        return int.Parse(location["/api/movies/".Length..], System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<string?> ReadFormatAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var movieId = new MovieId(id);
        return await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => movie.Format.Extension)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private string CreateOwnedImportFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lamuflix-dev438-play-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        ownedTempRoot = root;
        var folder = Path.Combine(root, FolderName);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, OriginalVideo), VideoBytes);
        return folder;
    }

    private static void SwapVideoFile(string folder)
    {
        File.Delete(Path.Combine(folder, OriginalVideo));
        File.WriteAllBytes(Path.Combine(folder, ReplacementVideo), VideoBytes);
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

            if (attempt < TeardownDeleteAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(40 * attempt), TestContext.Current.CancellationToken);
            }
        }

        if (lastFailure is not null)
        {
            throw new IOException("Playback host test teardown could not delete its owned temporary folder.", lastFailure);
        }
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

    private static void InstallOmdbStub(WireMockServer server) =>
        server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(OmdbBody));

    private static KeyValuePair<string, string?>[] LocalPlaySettings(string mappedFormat) =>
    [
        new("Features:LocalPlay", "true"),
        new("Playback:Players:0:Name", "vlc"),
        new("Playback:Players:0:ExecutablePath", PlayerPath),
        new("Playback:Players:0:Formats:0", mappedFormat),
    ];

    /// <summary>
    /// Points <c>Library:RootPath</c> at the owned temporary library so the relative folder identity the
    /// import persists still resolves to the physical files during rescan and playback.
    /// </summary>
    private KeyValuePair<string, string?>[] LibraryRootSettings() =>
    [
        new(
            $"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}",
            ownedTempRoot.ShouldNotBeNull()),
    ];

    private static string PlayRoute(int id) => $"/api/movies/{id}/play";

    private static string RescanRoute(int id) => $"/api/movies/{id}/rescan";
}
