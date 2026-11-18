using System.Collections.Immutable;

namespace LamuFlix.Evaluation.RealLibrary;

public static class RecommendationArtifactContract
{
    public const string CaptureMode = "capture";
    public const string CompareMode = "compare";
    public const string ForYouListKind = "for-you";
    public const string RelatedListKind = "related";
    public const string CompareListKind = "compare";
    public const string OkStatus = "ok";
    public const string EmptyStatus = "empty";
    public const string StructuralReasonKind = "structural";
    public const string PersonalReasonKind = "personal";
}

public sealed record RecommendationArtifactEnvelope(
    string Mode,
    ImmutableArray<int> Targets,
    ImmutableArray<RecommendationArtifactList> Lists);

public sealed record RecommendationArtifactList(
    string Kind,
    int? TargetId,
    int? Take,
    string Status,
    ImmutableArray<RecommendationArtifactRow> Rows);

public sealed record RecommendationArtifactRow(
    int Rank,
    int MovieId,
    string Title,
    double Score,
    ImmutableArray<RecommendationArtifactReason> Reasons,
    RecommendationPairDiagnostics? Diagnostics);

public sealed record RecommendationArtifactReason(string Kind, string Text);

public sealed record RecommendationPairDiagnostics(
    ImmutableArray<RecommendationCriterionContribution> Breakdown,
    double? RawPlotCosine,
    int? PlotRank,
    bool InPool);

public sealed record RecommendationCriterionContribution(
    string Criterion,
    double Value,
    double Weight,
    double Contribution);
