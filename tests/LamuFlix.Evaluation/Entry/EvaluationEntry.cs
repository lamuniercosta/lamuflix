using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Output;
using LamuFlix.Evaluation.RealLibrary;
using LamuFlix.Evaluation.Replay;

namespace LamuFlix.Evaluation.Entry;

public static class EvaluationEntry
{
    public const string ModeVariable = "LAMUFLIX_EVALUATION_MODE";
    public const string ModeValue = "publish";
    public const string LeaveOneOutModeValue = "leave-one-out";
    public const string CompareModeValue = "compare";
    public const string QualityModeValue = "quality";
    public const string FixturePathVariable = "LAMUFLIX_EVALUATION_FIXTURE_PATH";
    public const string OutputPathVariable = "LAMUFLIX_EVALUATION_OUTPUT_PATH";
    public const string SeedVariable = "LAMUFLIX_EVALUATION_SEED";
    public const string TmdbSignalsVariable = "LAMUFLIX_EVALUATION_TMDB_SIGNALS";
    public const string SelectedIdVariable = "LAMUFLIX_EVALUATION_SELECTED_ID";
    public const string OtherIdVariable = "LAMUFLIX_EVALUATION_OTHER_ID";
    public const string CrowdEnabledVariable = "LAMUFLIX_EVALUATION_CROWD_ENABLED";
    public const string CrowdActiveThresholdVariable = "LAMUFLIX_EVALUATION_CROWD_ACTIVE_THRESHOLD";
    public const string TmdbConfiguredVariable = "LAMUFLIX_EVALUATION_TMDB_CONFIGURED";

    private const string DefaultLeaveOneOutOutputRelative = "artifacts/recommendations/leave-one-out.json";
    private const string DefaultQualityOutputRelative = "artifacts/recommendations/quality.json";
    private const string DefaultCompareDirectoryRelative = "artifacts/recommendations";

    // The harness only needs the activation predicate to see a nonblank key; this is not a credential.
    private const string PlaceholderTmdbKey = "evaluation" + "-placeholder";

    public static bool IsRequested(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        return string.Equals(readVariable(ModeVariable), ModeValue, StringComparison.Ordinal);
    }

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

        var fixture = EvaluationFixtureLoader.LoadFromJson(
            File.ReadAllText(fixturePath),
            TimeProvider.System);
        var result = EvaluationReplay.Run(fixture, ResolveOptions(readVariable, seed));
        var summary = MetricsSummary.FromReplay(fixture.FixtureId, seed, result);
        MetricsSummarySerializer.WriteToFile(summary, outputPath);

        return outputPath;
    }

    public static EvaluationReplayOptions ResolveOptions(Func<string, string?> readVariable, int seed)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        return EvaluationReplayOptions.Default(seed, readVariable(TmdbSignalsVariable));
    }

    public static bool IsRealLibraryRequested(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        return readVariable(ModeVariable) is LeaveOneOutModeValue or CompareModeValue or QualityModeValue;
    }

    public static async Task<string?> RunRealLibraryAsync(
        Func<string, string?> readVariable,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        if (!IsRealLibraryRequested(readVariable))
        {
            return null;
        }

        var outputPath = ResolveRealLibraryOutput(readVariable);
        var connectionString = RequireCredential(readVariable);

        await using var db = RealLibrarySnapshotLoader.OpenContext(connectionString);
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);

        return readVariable(ModeVariable) switch
        {
            LeaveOneOutModeValue => await RunLeaveOneOutAsync(snapshot, readVariable, outputPath, ct),
            QualityModeValue => RunQuality(snapshot, readVariable, outputPath, ct),
            _ => await RunCompareAsync(snapshot, readVariable, outputPath, ct),
        };
    }

    private static async Task<string?> RunLeaveOneOutAsync(
        RealLibrarySnapshot snapshot,
        Func<string, string?> readVariable,
        string outputPath,
        CancellationToken ct)
    {
        var result = await LeaveOneOutRunner.RunAsync(
            snapshot,
            ResolveRecommendationOptions(readVariable),
            TimeProvider.System,
            ct,
            ResolveTmdbOptions(readVariable));
        LeaveOneOutReportSerializer.WriteToFile(result, outputPath);

        return outputPath;
    }

    private static string RunQuality(
        RealLibrarySnapshot snapshot,
        Func<string, string?> readVariable,
        string outputPath,
        CancellationToken ct)
    {
        var report = QualityReportRunner.Run(
            snapshot,
            ResolveRecommendationOptions(readVariable),
            QualityReportRunner.ReadParameters(readVariable),
            ct,
            ResolveTmdbOptions(readVariable));
        ct.ThrowIfCancellationRequested();
        QualityReportSerializer.WriteToFile(report, outputPath);

        return outputPath;
    }

    private static Task<string?> RunCompareAsync(
        RealLibrarySnapshot snapshot,
        Func<string, string?> readVariable,
        string outputPath,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var selectedId = ReadPositiveId(readVariable, SelectedIdVariable)
            ?? throw new InvalidOperationException(
                $"Evaluation parameter '{SelectedIdVariable}' is required for compare mode.");
        var otherId = ReadPositiveId(readVariable, OtherIdVariable);

        var report = CompareRunner.Run(snapshot, ResolveRecommendationOptions(readVariable), selectedId, otherId, ct, ResolveTmdbOptions(readVariable));
        ct.ThrowIfCancellationRequested();
        RecommendationArtifactSerializer.WriteToFile(report.Envelope, outputPath);

        return Task.FromResult<string?>(outputPath);
    }

    /// <summary>
    /// Resolves the scoring options for a real-library mode. The checked-in defaults apply unless the
    /// nonsecret Crowd markers switch the criterion on and optionally set the active strong-match bar.
    /// </summary>
    internal static RecommendationOptions ResolveRecommendationOptions(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        var options = new RecommendationOptions();
        var enabledText = readVariable(CrowdEnabledVariable);
        if (!string.IsNullOrWhiteSpace(enabledText))
        {
            if (!bool.TryParse(enabledText, out var enabled))
            {
                throw new InvalidOperationException(
                    $"Evaluation parameter '{CrowdEnabledVariable}' must be true or false; got '{enabledText}'.");
            }

            options = options with { CrowdEnabled = enabled };
        }

        var thresholdText = readVariable(CrowdActiveThresholdVariable);
        if (!string.IsNullOrWhiteSpace(thresholdText))
        {
            if (!double.TryParse(
                    thresholdText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var threshold)
                || !double.IsFinite(threshold)
                || threshold is < 0 or > 1)
            {
                throw new InvalidOperationException(
                    $"Evaluation parameter '{CrowdActiveThresholdVariable}' must be a finite decimal in [0, 1]; got '{thresholdText}'.");
            }

            options = options with { CrowdActiveStrongMatchThreshold = threshold };
        }

        return options;
    }

    /// <summary>
    /// Resolves the TMDb configuration the production activation predicate reads. The harness never
    /// needs the real key: a nonsecret marker supplies a placeholder key and the enabled flag.
    /// </summary>
    internal static TmdbOptions ResolveTmdbOptions(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        var configuredText = readVariable(TmdbConfiguredVariable);
        if (string.IsNullOrWhiteSpace(configuredText))
        {
            return new TmdbOptions();
        }

        if (!bool.TryParse(configuredText, out var configured))
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{TmdbConfiguredVariable}' must be true or false; got '{configuredText}'.");
        }

        return configured
            ? new TmdbOptions { Enabled = true, ApiKey = PlaceholderTmdbKey }
            : new TmdbOptions();
    }

    internal static string ResolveRealLibraryOutput(Func<string, string?> readVariable)
    {
        var declared = readVariable(OutputPathVariable);
        if (!string.IsNullOrWhiteSpace(declared))
        {
            return EvaluationPaths.ResolveRelative(declared);
        }

        return readVariable(ModeVariable) switch
        {
            LeaveOneOutModeValue => EvaluationPaths.Resolve(null, DefaultLeaveOneOutOutputRelative),
            QualityModeValue => EvaluationPaths.Resolve(null, DefaultQualityOutputRelative),
            _ => EvaluationPaths.ResolveRelative(DefaultCompareOutput(readVariable)),
        };
    }

    private static string DefaultCompareOutput(Func<string, string?> readVariable)
    {
        var selected = ReadPositiveId(readVariable, SelectedIdVariable)
            ?? throw new InvalidOperationException(
                $"Evaluation parameter '{SelectedIdVariable}' is required for compare mode.");
        var other = ReadPositiveId(readVariable, OtherIdVariable);

        return other is null
            ? Path.Combine(
                DefaultCompareDirectoryRelative,
                "compare-" + selected.ToString(CultureInfo.InvariantCulture) + ".json")
            : Path.Combine(
                DefaultCompareDirectoryRelative,
                "pair-"
                + selected.ToString(CultureInfo.InvariantCulture)
                + "-"
                + other.Value.ToString(CultureInfo.InvariantCulture)
                + ".json");
    }

    private static string RequireCredential(Func<string, string?> readVariable)
    {
        if (!RealLibrarySnapshotLoader.IsCredentialPresent(readVariable))
        {
            throw new InvalidOperationException(
                $"The process environment must already provide '{RealLibrarySnapshotLoader.CredentialVariableName}'. "
                + "Provide it outside the evaluation command; it is never read back, echoed, or logged here.");
        }

        return readVariable(RealLibrarySnapshotLoader.CredentialVariableName)
            ?? throw new InvalidOperationException(
                $"The process environment must already provide '{RealLibrarySnapshotLoader.CredentialVariableName}'.");
    }

    private static int? ReadPositiveId(Func<string, string?> readVariable, string name)
    {
        var text = readVariable(name);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{name}' must be a positive integer; got '{text}'.");
        }

        return value;
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

public sealed class RealLibraryModeScope : IDisposable
{
    private readonly Action<string, string?> write;
    private readonly Dictionary<string, string?> prior = new(StringComparer.Ordinal);

    public RealLibraryModeScope(
        Func<string, string?> read,
        Action<string, string?> write,
        IReadOnlyDictionary<string, string?> markers)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(markers);
        this.write = write ?? throw new ArgumentNullException(nameof(write));

        foreach (var (name, value) in markers)
        {
            prior[name] = read(name);
            write(name, value);
        }
    }

    public void Dispose()
    {
        foreach (var (name, value) in prior)
        {
            write(name, value);
        }
    }
}
