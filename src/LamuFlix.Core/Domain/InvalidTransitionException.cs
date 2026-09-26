using System;

namespace LamuFlix.Core.Domain;

public sealed class InvalidTransitionException : InvalidOperationException
{
    public InvalidTransitionException()
        : this("Unknown", "Unknown")
    {
    }

    public InvalidTransitionException(string message)
        : base(message)
    {
        Action = string.Empty;
        State = string.Empty;
    }

    public InvalidTransitionException(string message, Exception innerException)
        : base(message, innerException)
    {
        Action = string.Empty;
        State = string.Empty;
    }

    public InvalidTransitionException(string action, string state)
        : base($"Cannot {action} from {state}.")
    {
        Action = action;
        State = state;
    }

    public string Action { get; }

    public string State { get; }
}
