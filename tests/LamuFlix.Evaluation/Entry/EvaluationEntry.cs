using System;
using System.Globalization;
using System.IO;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Output;
using LamuFlix.Evaluation.Replay;

namespace LamuFlix.Evaluation.Entry;

/// <summary>
/// The intentional evaluation entry selected by <c>scripts/run-evaluation.ps1</c>
/// (TB002 Mode A). Publication requires the mode marker and the transported,
/// validated parameters, so ordinary suite execution of the evaluation project
/// never publishes. The child performs validation, replay and publication itself.
/// </summary>
public static class EvaluationEntry
{
    public const string ModeVariable = "LAMUFLIX_EVALUATION_MODE";
    public const string ModeValue = "publish";
    public const string FixturePathVariable = "LAMUFLIX_EVALUATION_FIXTURE_PATH";
    public const string OutputPathVariable = "LAMUFLIX_EVALUATION_OUTPUT_PATH";
    public const string SeedVariable = "LAMUFLIX_EVALUATION_SEED";

    public static bool IsRequested(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        return string.Equals(readVariable(ModeVariable), ModeValue, StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates, replays and publishes when the mode marker is present.
    /// Returns the published path, or <see langword="null"/> when this execution
    /// was ordinary suite execution and therefore published nothing.
    /// </summary>
    public static string? Publish(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        if (!IsRequested(readVariable))
        {
            return null;
        }

        var fixturePath = Required(readVariable, FixturePathVariable);
        var outputPath = Required(readVariable, OutputPathVariable);
        var seed = ReadSeed(readVariable);

        // The parse clock only bounds movie release-year plausibility; replay reads
        // every history-dependent input from a clock fixed to each target timestamp.
        var fixture = EvaluationFixtureLoader.LoadFromJson(
            File.ReadAllText(fixturePath),
            TimeProvider.System);
        var result = EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(seed));
        var summary = MetricsSummary.FromReplay(fixture.FixtureId, seed, result);
        MetricsSummarySerializer.WriteToFile(summary, outputPath);

        return outputPath;
    }

    private static string Required(Func<string, string?> readVariable, string name)
    {
        var value = readVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{name}' is required for intentional publication.");
        }

        return value;
    }

    private static int ReadSeed(Func<string, string?> readVariable)
    {
        var text = Required(readVariable, SeedVariable);
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed) || seed < 0)
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{SeedVariable}' must be a non-negative integer.");
        }

        return seed;
    }
}
