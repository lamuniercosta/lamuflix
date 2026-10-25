using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.Replay;
using LamuFlix.Evaluation.TestSupport;

namespace LamuFlix.Evaluation.Output;

/// <summary>
/// T014/T015 — FR-006 summary contract: exact ordered fields, compact UTF-8
/// without BOM or trailing newline, finite invariant-culture unrounded numbers,
/// and no volatile metadata. Serialization is deterministic for identical input.
/// </summary>
public sealed class MetricsSummarySerializerTests
{
    private const string ExpectedJson =
        "{\"schemaVersion\":1,\"fixtureId\":\"fixture\",\"seed\":42,\"cutoff\":10,"
        + "\"observationCount\":4,\"hitCount\":2,\"reciprocalRankSum\":0.75,"
        + "\"hitRateAt10\":0.5,\"mrr\":0.1875}";

    [Fact]
    public void SerializeToUtf8Bytes_WritesExactOrderedCompactJson()
    {
        // arrange
        var summary = Summary();

        // act
        var bytes = MetricsSummarySerializer.SerializeToUtf8Bytes(summary);

        // assert
        Encoding.UTF8.GetString(bytes).ShouldBe(ExpectedJson);
    }

    [Fact]
    public void SerializeToUtf8Bytes_WritesUtf8WithoutBomOrTrailingNewline()
    {
        // act
        var bytes = MetricsSummarySerializer.SerializeToUtf8Bytes(Summary());

        // assert
        bytes[0].ShouldNotBe((byte)0xEF);
        bytes[^1].ShouldNotBe((byte)'\n');
        bytes[^1].ShouldNotBe((byte)'\r');
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetString(bytes).ShouldBe(ExpectedJson);
    }

    [Fact]
    public void SerializeToUtf8Bytes_KeepsNumbersUnroundedAndInvariantCulture()
    {
        // arrange: thirds are not representable, so any rounding would show here
        var summary = Summary() with
        {
            ObservationCount = 3,
            HitCount = 1,
            ReciprocalRankSum = 1.0 / 3.0,
            HitRateAt10 = 1.0 / 3.0,
            Mrr = 1.0 / 9.0,
        };

        // act
        var text = Encoding.UTF8.GetString(MetricsSummarySerializer.SerializeToUtf8Bytes(summary));

        // assert
        text.ShouldContain("\"reciprocalRankSum\":" + (1.0 / 3.0).ToString("R", CultureInfo.InvariantCulture));
        text.ShouldContain("\"mrr\":" + (1.0 / 9.0).ToString("R", CultureInfo.InvariantCulture));
        text.ShouldNotContain("0,333");
        text.ShouldNotContain("\"generatedAt\"");
        text.ShouldNotContain("timestamp");
        text.ShouldNotContain("path");
    }

    [Fact]
    public void SerializeToUtf8Bytes_IsByteStableForEqualInput()
    {
        // act
        var first = MetricsSummarySerializer.SerializeToUtf8Bytes(Summary());
        var second = MetricsSummarySerializer.SerializeToUtf8Bytes(Summary());

        // assert
        second.ShouldBe(first);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void SerializeToUtf8Bytes_RejectsNonFiniteNumbers(double value)
    {
        // arrange
        var summary = Summary() with { Mrr = value };

        // act / assert
        Should.Throw<ArgumentException>(() => MetricsSummarySerializer.SerializeToUtf8Bytes(summary));
    }

    [Fact]
    public void SerializeToUtf8Bytes_RejectsBlankFixtureId()
    {
        // arrange
        var summary = Summary() with { FixtureId = " " };

        // act / assert
        Should.Throw<ArgumentException>(() => MetricsSummarySerializer.SerializeToUtf8Bytes(summary));
    }

    [Fact]
    public void WriteToFile_WritesTheSameBytesAndLeavesNoPartialFile()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("nested", "metrics-summary.json");
        var summary = Summary();

        // act
        MetricsSummarySerializer.WriteToFile(summary, path);

        // assert
        File.ReadAllBytes(path).ShouldBe(MetricsSummarySerializer.SerializeToUtf8Bytes(summary));
        var directoryPath = Path.GetDirectoryName(path);
        directoryPath.ShouldNotBeNull();
        Directory.GetFiles(directoryPath).ShouldBe([path]);
    }

    [Fact]
    public void WriteToFile_WhenTheTargetExists_ReplacesItCompletely()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("metrics-summary.json");
        File.WriteAllText(path, new string('x', 4096));

        // act
        MetricsSummarySerializer.WriteToFile(Summary(), path);

        // assert
        Encoding.UTF8.GetString(File.ReadAllBytes(path)).ShouldBe(ExpectedJson);
    }

    [Fact]
    public void FromReplay_EmptyObservationSet_SerializesZeroCountsAndMetrics()
    {
        // arrange
        var replay = new EvaluationReplayResult(356, [], new EvaluationMetrics(0, 0, 0.0, 0.0, 0.0));

        // act
        var text = Encoding.UTF8.GetString(
            MetricsSummarySerializer.SerializeToUtf8Bytes(
                MetricsSummary.FromReplay("dev-356-empty", 356, replay)));

        // assert
        text.ShouldBe(
            "{\"schemaVersion\":1,\"fixtureId\":\"dev-356-empty\",\"seed\":356,\"cutoff\":10,"
            + "\"observationCount\":0,\"hitCount\":0,\"reciprocalRankSum\":0,"
            + "\"hitRateAt10\":0,\"mrr\":0}");
    }

    [Fact]
    public void FromReplay_CarriesReplayMetricsAndDeclaredMetadata()
    {
        // arrange
        var metrics = new EvaluationMetrics(12, 9, 3463.0 / 1260.0, 0.75, 3463.0 / 15120.0);
        var replay = new EvaluationReplayResult(42, [], metrics);

        // act
        var summary = MetricsSummary.FromReplay("dev-356-baseline", 42, replay);

        // assert
        summary.ShouldSatisfyAllConditions(
            value => value.SchemaVersion.ShouldBe(MetricsSummary.CurrentSchemaVersion),
            value => value.FixtureId.ShouldBe("dev-356-baseline"),
            value => value.Seed.ShouldBe(42),
            value => value.Cutoff.ShouldBe(MetricCalculator.Cutoff),
            value => value.ObservationCount.ShouldBe(12),
            value => value.HitCount.ShouldBe(9),
            value => value.ReciprocalRankSum.ShouldBe(metrics.ReciprocalRankSum),
            value => value.HitRateAt10.ShouldBe(0.75),
            value => value.Mrr.ShouldBe(metrics.Mrr));
    }

    [Fact]
    public void SerializeToUtf8Bytes_DeclaresExactlyTheRuledFields()
    {
        // act
        var text = Encoding.UTF8.GetString(MetricsSummarySerializer.SerializeToUtf8Bytes(Summary()));

        // assert: FR-006 field order, no extras
        var positions = new[]
        {
            "\"schemaVersion\"", "\"fixtureId\"", "\"seed\"", "\"cutoff\"", "\"observationCount\"",
            "\"hitCount\"", "\"reciprocalRankSum\"", "\"hitRateAt10\"", "\"mrr\"",
        }
            .Select(name => text.IndexOf(name, StringComparison.Ordinal))
            .ToArray();
        positions.ShouldAllBe(position => position >= 0);
        positions.ShouldBe([.. positions.Order()]);
        text.Count(character => character == ':').ShouldBe(9);
    }

    private static MetricsSummary Summary() =>
        new(
            MetricsSummary.CurrentSchemaVersion,
            "fixture",
            42,
            MetricCalculator.Cutoff,
            4,
            2,
            0.75,
            0.5,
            0.1875);
}
