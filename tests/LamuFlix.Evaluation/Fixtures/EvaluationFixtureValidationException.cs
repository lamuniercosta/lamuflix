using System;

namespace LamuFlix.Evaluation.Fixtures;

public sealed class EvaluationFixtureValidationException : InvalidOperationException
{
    public EvaluationFixtureValidationException()
        : this("Evaluation fixture is invalid.")
    {
    }

    public EvaluationFixtureValidationException(string message)
        : base(message)
    {
    }

    public EvaluationFixtureValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
