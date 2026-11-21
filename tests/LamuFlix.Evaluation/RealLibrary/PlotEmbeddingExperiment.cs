namespace LamuFlix.Evaluation.RealLibrary;

public enum PlotEmbeddingVariant
{
    BaselinePlot = 0,
    EnrichedText = 1,
    AlternateModel = 2,
}

public static class PlotEmbeddingExperiment
{
    public const string BaselineModelName = "all-minilm";

    public const string AlternateModelName = "nomic-embed-text:v1.5";

    public const int EnrichedKeywordCap = 3;

    public const int ProposedTokenLimit = 256;
}
