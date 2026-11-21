using System;
using System.Collections.Immutable;

namespace LamuFlix.Evaluation.RealLibrary;

public static class PlotEmbeddingPreflight
{
    public static PlotEmbeddingPreflightResult Check(
        Func<string, bool> isModelServed,
        Func<bool> isScratchStoreReachable)
    {
        ArgumentNullException.ThrowIfNull(isModelServed);
        ArgumentNullException.ThrowIfNull(isScratchStoreReachable);

        var missing = ImmutableArray.CreateBuilder<string>();

        if (!isModelServed(PlotEmbeddingExperiment.BaselineModelName))
        {
            missing.Add($"Ollama model '{PlotEmbeddingExperiment.BaselineModelName}' is not served at the configured endpoint.");
        }

        if (!isModelServed(PlotEmbeddingExperiment.AlternateModelName))
        {
            missing.Add($"Ollama model '{PlotEmbeddingExperiment.AlternateModelName}' is not served at the configured endpoint.");
        }

        if (!isScratchStoreReachable())
        {
            missing.Add("Scratch evaluation store is unreachable.");
        }

        var prerequisites = missing.ToImmutable();

        return new PlotEmbeddingPreflightResult(prerequisites.IsEmpty, prerequisites);
    }
}

public sealed record PlotEmbeddingPreflightResult(
    bool CanRun,
    ImmutableArray<string> MissingPrerequisites);
