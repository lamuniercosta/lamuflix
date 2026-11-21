using System;

namespace LamuFlix.Evaluation.RealLibrary;

public static class PlotEmbeddingEndpointPolicy
{
    public static bool IsLocalEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return false;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.Ordinal)
            || uri.Host.Equals("[::1]", StringComparison.Ordinal)
            || uri.Host.Equals("::1", StringComparison.Ordinal);
    }
}

public static class PlotEmbeddingScratchPolicy
{
    public static bool IsScratchConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return connectionString.Contains("scratch", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("evaluation", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("memory", StringComparison.OrdinalIgnoreCase);
    }
}

public static class PlotEmbeddingTokenBudget
{
    public static bool IsWithinLimit(int tokenCount) =>
        tokenCount >= 0 && tokenCount <= PlotEmbeddingExperiment.ProposedTokenLimit;
}
