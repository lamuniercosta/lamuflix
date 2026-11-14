using System;

namespace LamuFlix.Core.Pipeline;

public sealed class DuplicateLibraryPathException : Exception
{
    public DuplicateLibraryPathException()
        : base("A movie already holds this library path.")
    {
    }

    public DuplicateLibraryPathException(string message)
        : base(message)
    {
    }

    public DuplicateLibraryPathException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
