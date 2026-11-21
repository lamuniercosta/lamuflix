using System;
using System.Collections.Immutable;

namespace LamuFlix.Evaluation.RealLibrary;

public static class PlotEmbeddingPreflight
{
    public const string NonLocalEndpointMessage =
        "Configured Ollama endpoint is not local; refusing remote model probes.";

    public const string ScratchIdentityMessage =
        "Scratch evaluation store identity is unverified; refusing readiness without a scratch lane marker.";

    public static PlotEmbeddingPreflightResult Check(
        string? endpoint,
        Func<string, bool> isModelServed,
        string? scratchConnectionString,
        Func<bool> isScratchStoreReachable)
    {
        ArgumentNullException.ThrowIfNull(isModelServed);
        ArgumentNullException.ThrowIfNull(isScratchStoreReachable);

        var missing = ImmutableArray.CreateBuilder<string>();

        if (!PlotEmbeddingEndpointPolicy.IsLocalEndpoint(endpoint))
        {
            missing.Add(NonLocalEndpointMessage);
        }
        else
        {
            CheckModels(isModelServed, missing);
        }

        CheckScratch(scratchConnectionString, isScratchStoreReachable, missing);

        var prerequisites = missing.ToImmutable();

        return new PlotEmbeddingPreflightResult(prerequisites.IsEmpty, prerequisites);
    }

    private static void CheckModels(Func<string, bool> isModelServed, ImmutableArray<string>.Builder missing)
    {
        if (!isModelServed(PlotEmbeddingExperiment.BaselineModelName))
        {
            missing.Add($"Ollama model '{PlotEmbeddingExperiment.BaselineModelName}' is not served at the configured endpoint.");
        }

        if (!isModelServed(PlotEmbeddingExperiment.AlternateModelName))
        {
            missing.Add($"Ollama model '{PlotEmbeddingExperiment.AlternateModelName}' is not served at the configured endpoint.");
        }
    }

    private static void CheckScratch(
        string? scratchConnectionString,
        Func<bool> isScratchStoreReachable,
        ImmutableArray<string>.Builder missing)
    {
        if (!PlotEmbeddingScratchPolicy.IsScratchConnectionString(scratchConnectionString))
        {
            missing.Add(ScratchIdentityMessage);
            return;
        }

        if (!isScratchStoreReachable())
        {
            missing.Add("Scratch evaluation store is unreachable.");
        }
    }
}

public sealed record PlotEmbeddingPreflightResult(
    bool CanRun,
    ImmutableArray<string> MissingPrerequisites);
