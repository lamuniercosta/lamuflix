using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.TestSupport;

namespace LamuFlix.Evaluation.Entry;

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

    private static JsonElement ReadSummary(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return document.RootElement.Clone();
    }
}
