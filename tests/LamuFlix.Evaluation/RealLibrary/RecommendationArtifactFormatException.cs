using System;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class RecommendationArtifactFormatException : Exception
{
    public RecommendationArtifactFormatException(string message)
        : base(message)
    {
    }

    public RecommendationArtifactFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
