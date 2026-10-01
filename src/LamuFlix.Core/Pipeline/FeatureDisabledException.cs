using System;

namespace LamuFlix.Core.Pipeline;

public sealed class FeatureDisabledException : Exception
{
    public FeatureDisabledException(string message)
        : base(message)
    {
    }
}
