using System;

namespace LamuFlix.Core.Ports;

public sealed class DuplicateImdbIdException : Exception
{
    public DuplicateImdbIdException()
        : base("Another movie already holds this IMDb id.")
    {
    }

    public DuplicateImdbIdException(string message)
        : base(message)
    {
    }

    public DuplicateImdbIdException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
