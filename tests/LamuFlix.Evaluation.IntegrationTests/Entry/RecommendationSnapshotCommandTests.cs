using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using LamuFlix.Evaluation.IntegrationTests.TestSupport;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LamuFlix.Evaluation.IntegrationTests.Entry;

public sealed class RecommendationSnapshotCommandTests : IDisposable
{
    private const string ForYouPath = "/api/recommendations/for-you";

    private static readonly int[] SeedIds = [11, 12, 13, 14, 15];

    private static readonly int[] HomeSeedIds = [575, 518, 515, 519, 831];

    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "lamuflix-snapshot-" + Guid.NewGuid().ToString("N"));

    public RecommendationSnapshotCommandTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup must never fail a test.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup must never fail a test.
        }
    }

    [Fact]
    public void Capture_WritesByteIdenticalArtifactsForRepeatedCaptures()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        var first = Path.Combine(root, "first.json");
        var second = Path.Combine(root, "second.json");

        // act
        var firstRun = Capture(server, first);
        var secondRun = Capture(server, second);

        // assert
        firstRun.ExitCode.ShouldBe(0, firstRun.Describe());
        secondRun.ExitCode.ShouldBe(0, secondRun.Describe());
        File.ReadAllBytes(second).ShouldBe(File.ReadAllBytes(first));
    }

    [Fact]
    public void Capture_PreservesExactApiOrderAndEveryReasonKindAndText()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var lists = document.RootElement.GetProperty("lists");

        lists.GetArrayLength().ShouldBe(6);
        var forYou = lists[0];
        forYou.GetProperty("kind").GetString().ShouldBe("for-you");
        forYou.GetProperty("targetId").ValueKind.ShouldBe(JsonValueKind.Null);
        forYou.GetProperty("status").GetString().ShouldBe("ok");

        var rows = forYou.GetProperty("rows").EnumerateArray().ToArray();
        rows.Select(row => row.GetProperty("movieId").GetInt32()).ShouldBe([77, 66, 55]);
        rows.Select(row => row.GetProperty("rank").GetInt32()).ShouldBe([1, 2, 3]);
        rows.Select(row => row.GetProperty("title").GetString()).ShouldBe(["Alpha", "Beta", "Gamma"]);

        var firstReasons = rows[0].GetProperty("reasons").EnumerateArray().ToArray();
        firstReasons.Length.ShouldBe(2);
        firstReasons[0].GetProperty("kind").GetString().ShouldBe("structural");
        firstReasons[0].GetProperty("text").GetString().ShouldBe("Same director: Ada");
        firstReasons[1].GetProperty("kind").GetString().ShouldBe("personal");
        firstReasons[1].GetProperty("text").GetString().ShouldBe("Matches your ratings");
    }

    [Fact]
    public void Capture_SendsExplicitForYouAndRelatedTakes()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        QueryValues(server, ForYouPath, "take").ShouldBe(["30"]);
        foreach (var seed in SeedIds)
        {
            QueryValues(server, $"/api/movies/{seed}/recommendations", "take").ShouldBe(["10"]);
        }
    }

    [Fact]
    public void Capture_DefaultsToTheLookedUpHomeSeedIdsWithoutAnyExplicitSeeds()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        foreach (var seed in HomeSeedIds)
        {
            server.Given(Request.Create().WithPath($"/api/movies/{seed}/recommendations").UsingGet())
                .RespondWith(Json(200, "[]"));
        }

        var path = Path.Combine(root, "capture.json");

        // act
        var result = EvaluationCommand.RunSnapshot(
            root,
            "-Mode",
            "Capture",
            "-BaseUrl",
            server.Urls[0],
            "-OutputPath",
            path);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        document.RootElement.GetProperty("targets").EnumerateArray()
            .Select(target => target.GetInt32())
            .ShouldBe(HomeSeedIds);
        foreach (var seed in HomeSeedIds)
        {
            QueryValues(server, $"/api/movies/{seed}/recommendations", "take").ShouldBe(["10"]);
        }
    }

    [Fact]
    public void Capture_RecordsEmptyRelatedResponsesAsEmptyInsteadOfFailing()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var relatedLists = document.RootElement.GetProperty("lists").EnumerateArray()
            .Where(list => list.GetProperty("kind").GetString() == "related")
            .ToArray();

        relatedLists.Length.ShouldBe(5);
        relatedLists[0].GetProperty("status").GetString().ShouldBe("empty");
        relatedLists[0].GetProperty("rows").GetArrayLength().ShouldBe(0);
        relatedLists[1].GetProperty("status").GetString().ShouldBe("ok");
        relatedLists[1].GetProperty("targetId").GetInt32().ShouldBe(12);
    }

    [Theory]
    [InlineData("11,12,13,14")]
    [InlineData("11,12,13,14,15,16")]
    [InlineData("11,12,13,14,0")]
    [InlineData("11,12,13,14,-5")]
    [InlineData("11,12,13,14,abc")]
    [InlineData("11,12,13,14,11")]
    public void Capture_RejectsInvalidSeedIdsBeforeAnyHttpRequest(string seeds)
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        var path = Path.Combine(root, "capture.json");

        // act
        var result = EvaluationCommand.RunSnapshot(
            root,
            "-Mode",
            "Capture",
            "-BaseUrl",
            server.Urls[0],
            "-SeedIds",
            seeds,
            "-OutputPath",
            path);

        // assert
        result.ExitCode.ShouldNotBe(0);
        File.Exists(path).ShouldBeFalse(result.Describe());
        server.LogEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Capture_FailsOnHttpErrorWithoutPublishingAnArtifact()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        server.Given(Request.Create().WithPath($"/api/movies/{SeedIds[3]}/recommendations").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("{}"));
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldNotBe(0);
        File.Exists(path).ShouldBeFalse(result.Describe());
        Directory.GetFiles(root).ShouldBeEmpty();
    }

    [Fact]
    public void Capture_FailsOnMalformedJsonWithoutPublishingAnArtifact()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        server.Given(Request.Create().WithPath(ForYouPath).UsingGet())
            .RespondWith(Json(200, "not json"));
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldNotBe(0);
        File.Exists(path).ShouldBeFalse(result.Describe());
    }

    [Theory]
    [InlineData("[{\"id\":77,\"title\":\"Alpha\",\"score\":0.5}]")]
    [InlineData("[{\"id\":77,\"title\":\"Alpha\",\"score\":0.5,\"reasons\":[{\"kind\":\"bogus\",\"text\":\"x\"}]}]")]
    [InlineData("[{\"id\":0,\"title\":\"Alpha\",\"score\":0.5,\"reasons\":[]}]")]
    [InlineData("[{\"id\":77,\"title\":\"Alpha\",\"score\":\"0.5\",\"reasons\":[]}]")]
    [InlineData("{\"id\":77}")]
    public void Capture_FailsOnSchemaViolationsWithoutPublishingAnArtifact(string body)
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        server.Given(Request.Create().WithPath(ForYouPath).UsingGet())
            .RespondWith(Json(200, body));
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldNotBe(0);
        File.Exists(path).ShouldBeFalse(result.Describe());
    }

    [Fact]
    public void Capture_FailsOnDuplicateMovieIdsWithoutPublishingAnArtifact()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        server.Given(Request.Create().WithPath(ForYouPath).UsingGet())
            .RespondWith(Json(
                200,
                "[{\"id\":77,\"title\":\"Alpha\",\"score\":0.5,\"reasons\":[]},"
                + "{\"id\":77,\"title\":\"Alpha again\",\"score\":0.4,\"reasons\":[]}]"));
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldNotBe(0);
        File.Exists(path).ShouldBeFalse(result.Describe());
    }

    [Fact]
    public void Capture_WritesCompactUtf8WithoutBomOrTerminalNewline()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        var path = Path.Combine(root, "capture.json");

        // act
        var result = Capture(server, path);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        var bytes = File.ReadAllBytes(path);
        bytes[0].ShouldBe((byte)'{');
        bytes[^1].ShouldBe((byte)'}');
        bytes.ShouldAllBe(value => value != (byte)'\n' && value != (byte)'\r');
        Encoding.UTF8.GetString(bytes).ShouldNotContain("\"timestamp\"");
        Encoding.UTF8.GetString(bytes).ShouldNotContain("\"host\"");
        Encoding.UTF8.GetString(bytes).ShouldNotContain("\"elapsed\"");
    }

    [Fact]
    public void Capture_RoundTripsAdversarialFiniteNumbersUnderAPortugueseCulture()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        StubSuccessfulLibrary(server);
        server.Given(Request.Create().WithPath(ForYouPath).UsingGet())
            .RespondWith(Json(
                200,
                "[{\"id\":77,\"title\":\"Alpha\",\"score\":0.1,\"reasons\":[]},"
                + "{\"id\":66,\"title\":\"Beta\",\"score\":1E-300,\"reasons\":[]},"
                + "{\"id\":55,\"title\":\"Gamma\",\"score\":1.7976931348623157E+308,\"reasons\":[]}]"));
        var path = Path.Combine(root, "capture.json");
        var original = CultureInfo.CurrentCulture;

        try
        {
            // act
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var result = Capture(server, path);

            // assert
            result.ExitCode.ShouldBe(0, result.Describe());
            var text = Encoding.UTF8.GetString(File.ReadAllBytes(path));
            text.ShouldContain("\"score\":0.1,");
            text.ShouldContain("\"score\":1E-300,");
            text.ShouldContain("\"score\":1.7976931348623157E+308,");
            text.ShouldNotContain("\"score\":0,1");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Diff_PrintsAllFiveGroupsForCompatibleCaptures()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        var before = Path.Combine(root, "before.json");
        var after = Path.Combine(root, "after.json");
        StubSuccessfulLibrary(server);
        Capture(server, before).ExitCode.ShouldBe(0);

        server.Reset();
        StubSuccessfulLibrary(server, reordered: true);
        Capture(server, after).ExitCode.ShouldBe(0);

        // act
        var result = Diff(before, after);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        foreach (var heading in new[] { "moved up", "moved down", "entered", "left", "reasons changed" })
        {
            result.StandardOutput.ShouldContain(heading + ":");
        }

        result.StandardOutput.ShouldContain("movie 66 \"Beta\" rank 2 -> 1");
        result.StandardOutput.ShouldContain("movie 77 \"Alpha\" rank 1 -> 2");
        result.StandardOutput.ShouldContain("movie 88 \"Delta\" rank 3");
        result.StandardOutput.ShouldContain("movie 55 \"Gamma\" rank 3");
        result.StandardOutput.ShouldContain("summary: moved up 1, moved down 1, entered 1, left 1, reasons changed 1");
    }

    [Fact]
    public void Diff_PrintsEveryGroupEvenWhenNoRowMoved()
    {
        // arrange
        using var server = SnapshotWireMock.Start();
        var before = Path.Combine(root, "before.json");
        var after = Path.Combine(root, "after.json");
        StubSuccessfulLibrary(server);
        Capture(server, before).ExitCode.ShouldBe(0);
        Capture(server, after).ExitCode.ShouldBe(0);

        // act
        var result = Diff(before, after);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        result.StandardOutput.ShouldContain("moved up:");
        result.StandardOutput.ShouldContain("moved down:");
        result.StandardOutput.ShouldContain("entered:");
        result.StandardOutput.ShouldContain("left:");
        result.StandardOutput.ShouldContain("reasons changed:");
        result.StandardOutput.ShouldContain("summary: moved up 0, moved down 0, entered 0, left 0, reasons changed 0");
    }

    [Fact]
    public void Diff_RejectsMixedModeOperands()
    {
        // arrange
        var before = Path.Combine(root, "capture.json");
        var after = Path.Combine(root, "compare.json");
        WriteCapture(before);
        WriteCompare(after);

        // act
        var result = EvaluationCommand.RunSnapshot(root, "-Mode", "Diff", "-Before", before, "-After", after);

        // assert
        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldContain("same-mode");
    }

    [Fact]
    public void Diff_AcceptsCompatibleCompareArtifacts()
    {
        // arrange
        var before = Path.Combine(root, "compare-before.json");
        var after = Path.Combine(root, "compare-after.json");
        WriteCompare(before);
        WriteCompare(after, score: 0.25);

        // act
        var result = EvaluationCommand.RunSnapshot(root, "-Mode", "Diff", "-Before", before, "-After", after);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        result.StandardOutput.ShouldContain("moved up:");
        result.StandardOutput.ShouldContain("reasons changed:");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[11],\"lists\":[]}")]
    [InlineData("{\"mode\":\"bogus\",\"targets\":[11],\"lists\":[]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[11,11],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":[]}]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[11],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":[{\"rank\":1,\"movieId\":7,\"title\":\"A\",\"score\":1,\"reasons\":[]},{\"rank\":2,\"movieId\":7,\"title\":\"B\",\"score\":1,\"reasons\":[]}]}]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[11],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":[{\"rank\":2,\"movieId\":7,\"title\":\"A\",\"score\":1,\"reasons\":[]}]}]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[11],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"empty\",\"rows\":[{\"rank\":1,\"movieId\":7,\"title\":\"A\",\"score\":1,\"reasons\":[]}]}]}")]
    public void Diff_RejectsMalformedOrDuplicateOperands(string body)
    {
        // arrange
        var before = Path.Combine(root, "before.json");
        var after = Path.Combine(root, "after.json");
        WriteCapture(after);
        File.WriteAllText(before, body);

        // act
        var result = EvaluationCommand.RunSnapshot(root, "-Mode", "Diff", "-Before", before, "-After", after);

        // assert
        result.ExitCode.ShouldNotBe(0);
    }

    [Fact]
    public void Diff_RejectsOperandsWithDifferentTargets()
    {
        // arrange
        var before = Path.Combine(root, "before.json");
        var after = Path.Combine(root, "after.json");
        WriteCapture(before);
        WriteCapture(after, targets: [11, 12, 13, 14, 99]);

        // act
        var result = EvaluationCommand.RunSnapshot(root, "-Mode", "Diff", "-Before", before, "-After", after);

        // assert
        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldContain("targets");
    }

    [Fact]
    public void Diff_RequiresBothOperands()
    {
        // act
        var result = EvaluationCommand.RunSnapshot(root, "-Mode", "Diff", "-Before", "capture.json");

        // assert
        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldContain("-Before");
    }

    [Fact]
    public void Help_DocumentsTheCaptureAndDiffSurface()
    {
        // act
        var result = EvaluationCommand.RunSnapshot(root, "-Help");

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        result.StandardOutput.ShouldContain("run-recommendation-snapshot.ps1");
        result.StandardOutput.ShouldContain("-Mode");
        result.StandardOutput.ShouldContain("-BaseUrl");
        result.StandardOutput.ShouldContain("-SeedIds");
        result.StandardOutput.ShouldContain("-OutputPath");
        result.StandardOutput.ShouldContain("-Before");
        result.StandardOutput.ShouldContain("-After");
        result.StandardOutput.ShouldContain("A Fistful of Dollars");
        result.StandardOutput.ShouldContain("The Wind Rises");
        result.StandardOutput.ShouldContain("575");
        result.StandardOutput.ShouldContain("831");
        result.StandardOutput.ShouldContain("http://localhost:3100");
    }

    private CommandResult Capture(WireMockServer server, string path) =>
        EvaluationCommand.RunSnapshot(
            root,
            "-Mode",
            "Capture",
            "-BaseUrl",
            server.Urls[0],
            "-SeedIds",
            string.Join(',', SeedIds),
            "-OutputPath",
            path);

    private CommandResult Diff(string before, string after) =>
        EvaluationCommand.RunSnapshot(root, "-Mode", "Diff", "-Before", before, "-After", after);

    private static string[] QueryValues(WireMockServer server, string path, string name)
    {
        var values = new List<string>();
        foreach (var entry in server.LogEntries)
        {
            if (entry.RequestMessage is not { Query: { } query } request
                || !string.Equals(request.Path, path, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var pair in query.Where(pair => string.Equals(pair.Key, name, StringComparison.Ordinal)))
            {
                values.AddRange(pair.Value);
            }
        }

        return [.. values];
    }

    private static void StubSuccessfulLibrary(WireMockServer server, bool reordered = false)
    {
        var forYou = reordered
            ? "[{\"id\":66,\"title\":\"Beta\",\"score\":0.7,\"reasons\":[]},"
              + "{\"id\":77,\"title\":\"Alpha\",\"score\":0.5,\"reasons\":[{\"kind\":\"structural\",\"text\":\"Same director: Ada\"}]},"
              + "{\"id\":88,\"title\":\"Delta\",\"score\":0.1,\"reasons\":[]}]"
            : "[{\"id\":77,\"title\":\"Alpha\",\"score\":0.5,\"reasons\":[{\"kind\":\"structural\",\"text\":\"Same director: Ada\"},"
              + "{\"kind\":\"personal\",\"text\":\"Matches your ratings\"}]},"
              + "{\"id\":66,\"title\":\"Beta\",\"score\":0.7,\"reasons\":[]},"
              + "{\"id\":55,\"title\":\"Gamma\",\"score\":0.2,\"reasons\":[]}]";

        server.Given(Request.Create().WithPath(ForYouPath).UsingGet())
            .RespondWith(Json(200, forYou));

        foreach (var (seed, body) in new[]
                 {
                     (SeedIds[0], "[]"),
                     (SeedIds[1], "[{\"id\":66,\"title\":\"Beta\",\"score\":0.30000000000000004,\"reasons\":[{\"kind\":\"structural\",\"text\":\"Shares Drama\"}]}]"),
                     (SeedIds[2], "[]"),
                     (SeedIds[3], "[]"),
                     (SeedIds[4], "[]"),
                 })
        {
            server.Given(Request.Create().WithPath($"/api/movies/{seed}/recommendations").UsingGet())
                .RespondWith(Json(200, body));
        }
    }

    private static IResponseBuilder Json(int status, string body) =>
        Response.Create()
            .WithStatusCode(status)
            .WithHeader("Content-Type", "application/json")
            .WithBody(body);

    private static void WriteCapture(string path, int[]? targets = null) =>
        File.WriteAllText(
            path,
            "{\"mode\":\"capture\",\"targets\":[" + string.Join(',', targets ?? SeedIds) + "],\"lists\":["
            + "{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":["
            + "{\"rank\":1,\"movieId\":7,\"title\":\"Alpha\",\"score\":1,\"reasons\":[]}]}]}");

    private static void WriteCompare(string path, double score = 0.5) =>
        File.WriteAllText(
            path,
            "{\"mode\":\"compare\",\"targets\":[" + string.Join(',', SeedIds) + "],\"lists\":["
            + "{\"kind\":\"compare\",\"targetId\":11,\"take\":null,\"status\":\"ok\",\"rows\":["
            + "{\"rank\":1,\"movieId\":66,\"title\":\"Beta\",\"score\":"
            + score.ToString("R", CultureInfo.InvariantCulture)
            + ",\"reasons\":[],\"diagnostics\":{\"breakdown\":[],\"rawPlotCosine\":null,"
            + "\"plotRank\":null,\"inPool\":false}}]}]}");
}
