using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LamuFlix.Evaluation.IntegrationTests.TestSupport;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.TestSupport;

namespace LamuFlix.Evaluation.IntegrationTests.Entry;

public sealed class EvaluationCommandTests
{
    private const string DefaultSeed = "356";

    [Fact]
    public void Help_PrintsTheDocumentedSurfaceWithoutPublishing()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("metrics-summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-Help",
            "-OutputPath",
            output);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        result.StandardOutput.ShouldContain("run-evaluation.ps1");
        result.StandardOutput.ShouldContain("-FixturePath");
        result.StandardOutput.ShouldContain("-OutputPath");
        result.StandardOutput.ShouldContain("-Seed");
        File.Exists(output).ShouldBeFalse();
    }

    [Fact]
    public void Defaults_FromAnUnrelatedWorkingDirectory_PublishUnderTheRepositoryRoot()
    {
        // arrange
        using var directory = new TemporaryDirectory();

        // act
        var result = EvaluationCommand.Run(directory.FullPath);

        // assert
        try
        {
            result.ExitCode.ShouldBe(0, result.Describe());
            var output = EvaluationCommand.DefaultOutputPath;
            File.Exists(output).ShouldBeTrue(result.Describe());

            var summary = ReadSummary(output);
            summary.ShouldSatisfyAllConditions(
                value => value.GetProperty("schemaVersion").GetInt32().ShouldBe(1),
                value => value.GetProperty("fixtureId").GetString()
                    .ShouldBe(EvaluationCommandFixtures.BaselineFixtureId),
                value => value.GetProperty("seed").GetInt32().ShouldBe(356),
                value => value.GetProperty("cutoff").GetInt32().ShouldBe(MetricCalculator.Cutoff),
                value => value.GetProperty("observationCount").GetInt32()
                    .ShouldBe(EvaluationCommandFixtures.BaselineObservationCount),
                value => value.GetProperty("hitCount").GetInt32()
                    .ShouldBe(EvaluationCommandFixtures.BaselineHitCount),
                value => value.GetProperty("hitRateAt10").GetDouble()
                    .ShouldBe(EvaluationCommandFixtures.BaselineHitRateAt10));

            var text = File.ReadAllText(output);
            text.ShouldStartWith("{\"schemaVersion\":1,");
            text.ShouldEndWith("\"mrr\":" + summary.GetProperty("mrr").GetRawText() + "}");
            text.ShouldNotContain(EvaluationCommand.RepositoryRoot);
        }
        finally
        {
            if (File.Exists(EvaluationCommand.DefaultOutputPath))
            {
                File.Delete(EvaluationCommand.DefaultOutputPath);
            }
        }
    }

    [Fact]
    public void ExplicitOverrides_GovernFixtureOutputAndSeed()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var fixture = EvaluationCommandFixtures.WriteBaselineCopy(directory.Combine("fixture.json"));
        var output = directory.Combine("overridden", "summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            fixture,
            "-OutputPath",
            output,
            "-Seed",
            "42");

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        var summary = ReadSummary(output);
        summary.GetProperty("fixtureId").GetString().ShouldBe(EvaluationCommandFixtures.BaselineFixtureId);
        summary.GetProperty("seed").GetInt32().ShouldBe(42);
        summary.GetProperty("observationCount").GetInt32()
            .ShouldBe(EvaluationCommandFixtures.BaselineObservationCount);
    }

    [Fact]
    public void MissingFixture_ExitsNonzeroWithoutPublishing()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            directory.Combine("absent.json"),
            "-OutputPath",
            output);

        // assert
        result.ExitCode.ShouldNotBe(0, result.Describe());
        File.Exists(output).ShouldBeFalse();
        result.Output.ShouldContain("absent.json");
    }

    [Theory]
    [InlineData("duplicate-events")]
    [InlineData("malformed")]
    public void InvalidFixture_ExitsNonzeroWithoutPublishing(string kind)
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var fixture = kind == "malformed"
            ? EvaluationCommandFixtures.WriteMalformedFixture(directory.Combine("fixture.json"))
            : EvaluationCommandFixtures.WriteDuplicateEventFixture(directory.Combine("fixture.json"));
        var output = directory.Combine("summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            fixture,
            "-OutputPath",
            output);

        // assert
        result.ExitCode.ShouldNotBe(0, result.Describe());
        File.Exists(output).ShouldBeFalse();
        Directory.GetFiles(directory.FullPath).ShouldBe([fixture]);
    }

    [Fact]
    public void EmptyEventArray_PublishesZeroCountsAndMetrics()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var fixture = EvaluationCommandFixtures.WriteEmptyEventsFixture(directory.Combine("empty.json"));
        var output = directory.Combine("summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            fixture,
            "-OutputPath",
            output,
            "-Seed",
            DefaultSeed);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        var summary = ReadSummary(output);
        summary.ShouldSatisfyAllConditions(
            value => value.GetProperty("fixtureId").GetString().ShouldBe("dev-356-empty"),
            value => value.GetProperty("observationCount").GetInt32().ShouldBe(0),
            value => value.GetProperty("hitCount").GetInt32().ShouldBe(0),
            value => value.GetProperty("reciprocalRankSum").GetDouble().ShouldBe(0.0),
            value => value.GetProperty("hitRateAt10").GetDouble().ShouldBe(0.0),
            value => value.GetProperty("mrr").GetDouble().ShouldBe(0.0));
        File.ReadAllBytes(output)[^1].ShouldNotBe((byte)'\n');
    }

    [Theory]
    [InlineData("output-is-a-directory")]
    [InlineData("output-parent-is-a-file")]
    public void OutputWriteFailure_ExitsNonzeroWithoutPublishingPartialSuccess(string kind)
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var blocking = directory.Combine("blocking-entry");
        string output;
        if (kind == "output-is-a-directory")
        {
            Directory.CreateDirectory(blocking);
            output = blocking;
        }
        else
        {
            File.WriteAllText(blocking, "blocking file");
            output = Path.Combine(blocking, "summary.json");
        }

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            EvaluationCommand.FixturePath,
            "-OutputPath",
            output);

        // assert
        result.ExitCode.ShouldNotBe(0, result.Describe());
        if (kind == "output-is-a-directory")
        {
            Directory.GetFileSystemEntries(blocking).ShouldBeEmpty();
        }
        else
        {
            File.ReadAllText(blocking).ShouldBe("blocking file");
        }

        Directory.GetFileSystemEntries(directory.FullPath)
            .Any(entry => entry.Contains("partial", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void InvalidSeed_ExitsNonzeroWithoutPublishing(string seed)
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            EvaluationCommand.FixturePath,
            "-OutputPath",
            output,
            "-Seed",
            seed);

        // assert
        result.ExitCode.ShouldNotBe(0, result.Describe());
        File.Exists(output).ShouldBeFalse();
    }

    [Fact]
    public void BlankOutputPath_ExitsNonzeroWithoutPublishing()
    {
        // arrange
        using var directory = new TemporaryDirectory();

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            EvaluationCommand.FixturePath,
            "-OutputPath",
            string.Empty);

        // assert
        result.ExitCode.ShouldNotBe(0, result.Describe());
        Directory.GetFiles(directory.FullPath).ShouldBeEmpty();
    }

    [Fact]
    public void RepeatedSuccessfulRuns_PublishByteIdenticalSummaries()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");
        var arguments = new[]
        {
            "-FixturePath", EvaluationCommand.FixturePath,
            "-OutputPath", output,
            "-Seed", DefaultSeed,
        };

        // act
        var first = EvaluationCommand.Run(directory.FullPath, arguments);
        var firstBytes = File.ReadAllBytes(output);
        var second = EvaluationCommand.Run(directory.FullPath, arguments);
        var secondBytes = File.ReadAllBytes(output);

        // assert
        first.ExitCode.ShouldBe(0, first.Describe());
        second.ExitCode.ShouldBe(0, second.Describe());
        secondBytes.ShouldBe(firstBytes);
    }

    [Fact]
    public void PresentationPermutedFixture_PublishesByteIdenticalSummary()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var permutedFixture =
            EvaluationCommandFixtures.WritePresentationPermutedBaseline(directory.Combine("permuted.json"));
        var originalOutput = directory.Combine("original-summary.json");
        var permutedOutput = directory.Combine("permuted-summary.json");

        // act
        var original = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", EvaluationCommand.FixturePath,
            "-OutputPath", originalOutput,
            "-Seed", DefaultSeed);
        var permuted = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", permutedFixture,
            "-OutputPath", permutedOutput,
            "-Seed", DefaultSeed);

        // assert
        original.ExitCode.ShouldBe(0, original.Describe());
        permuted.ExitCode.ShouldBe(0, permuted.Describe());
        File.ReadAllBytes(permutedOutput).ShouldBe(File.ReadAllBytes(originalOutput));
    }

    [Fact]
    public void Help_DocumentsTheTmdbSignalsMode()
    {
        // arrange
        using var directory = new TemporaryDirectory();

        // act
        var result = EvaluationCommand.Run(directory.FullPath, "-Help");

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        result.StandardOutput.ShouldContain("-TmdbSignals");
        result.StandardOutput.ShouldContain("Disabled");
        result.StandardOutput.ShouldContain("Enabled");
    }

    [Fact]
    public void TmdbSignalsUnset_UsesTheDisabledMode()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var fixture = WriteSignalModeFixture(directory.Combine("fixture.json"));
        var output = directory.Combine("summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath",
            fixture,
            "-OutputPath",
            output,
            "-Seed",
            DefaultSeed);

        // assert
        result.ExitCode.ShouldBe(0, result.Describe());
        var summary = ReadSummary(output);
        summary.GetProperty("observationCount").GetInt32().ShouldBe(1);
        summary.GetProperty("hitCount").GetInt32().ShouldBe(1);
        summary.GetProperty("mrr").GetDouble().ShouldBe(0.5, 1e-12);
    }

    [Fact]
    public void ExplicitDisabledMode_MatchesTheDefaultRunByteForByte()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var fixture = WriteSignalModeFixture(directory.Combine("fixture.json"));
        var defaultOutput = directory.Combine("default-summary.json");
        var disabledOutput = directory.Combine("disabled-summary.json");

        // act
        var defaultRun = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", fixture,
            "-OutputPath", defaultOutput,
            "-Seed", DefaultSeed);
        var disabledRun = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", fixture,
            "-OutputPath", disabledOutput,
            "-Seed", DefaultSeed,
            "-TmdbSignals", "Disabled");

        // assert
        defaultRun.ExitCode.ShouldBe(0, defaultRun.Describe());
        disabledRun.ExitCode.ShouldBe(0, disabledRun.Describe());
        File.ReadAllBytes(disabledOutput).ShouldBe(File.ReadAllBytes(defaultOutput));
    }

    [Fact]
    public void EnabledMode_ReachesTheReplayAndChangesThePublishedMetrics()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var fixture = WriteSignalModeFixture(directory.Combine("fixture.json"));
        var disabledOutput = directory.Combine("disabled-summary.json");
        var enabledOutput = directory.Combine("enabled-summary.json");

        // act
        var disabled = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", fixture,
            "-OutputPath", disabledOutput,
            "-Seed", DefaultSeed);
        var enabled = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", fixture,
            "-OutputPath", enabledOutput,
            "-Seed", DefaultSeed,
            "-TmdbSignals", "Enabled");

        // assert
        disabled.ExitCode.ShouldBe(0, disabled.Describe());
        enabled.ExitCode.ShouldBe(0, enabled.Describe());

        var disabledSummary = ReadSummary(disabledOutput);
        var enabledSummary = ReadSummary(enabledOutput);
        enabledSummary.GetProperty("observationCount").GetInt32()
            .ShouldBe(disabledSummary.GetProperty("observationCount").GetInt32());
        enabledSummary.GetProperty("hitCount").GetInt32()
            .ShouldBe(disabledSummary.GetProperty("hitCount").GetInt32());
        enabledSummary.GetProperty("mrr").GetDouble().ShouldBe(1.0 / 3.0, 1e-12);
    }

    [Theory]
    [InlineData("enabled")]
    [InlineData("On")]
    [InlineData("1")]
    public void InvalidTmdbSignals_ExitsNonzeroWithoutPublishing(string mode)
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");

        // act
        var result = EvaluationCommand.Run(
            directory.FullPath,
            "-FixturePath", EvaluationCommand.FixturePath,
            "-OutputPath", output,
            "-TmdbSignals", mode);

        // assert
        result.ExitCode.ShouldNotBe(0, result.Describe());
        File.Exists(output).ShouldBeFalse();
    }

    private static string WriteSignalModeFixture(string path)
    {
        const string fixture = """
            {
              "fixtureId": "dev-358-signal-mode",
              "ranking": {
                "lambda": 0.5,
                "similarityWeights": {
                  "genres": 3.0,
                  "keyword": 1.0,
                  "collection": 2.0
                }
              },
              "catalog": [
                { "movieId": 1, "features": { "genres": ["Drama"], "keywords": [5001], "collectionId": 201 } },
                { "movieId": 2, "features": { "genres": ["Comedy"], "keywords": [5001], "collectionId": 201 } },
                { "movieId": 3, "features": { "genres": ["Comedy"], "keywords": [5002], "collectionId": 202 } }
              ],
              "events": [
                { "eventId": "e1", "userId": "u1", "movieId": 2, "timestamp": "2026-01-01T00:00:00Z" }
              ]
            }
            """;

        File.WriteAllText(path, fixture);
        return path;
    }

    private static JsonElement ReadSummary(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return document.RootElement.Clone();
    }
}
