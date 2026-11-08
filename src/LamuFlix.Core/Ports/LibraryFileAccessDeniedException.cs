using System;

namespace LamuFlix.Core.Ports;

public sealed class LibraryFileAccessDeniedException : Exception
{
    public LibraryFileAccessDeniedException()
        : base("Access to the library file was denied.")
    {
    }

    public LibraryFileAccessDeniedException(string message)
        : base(message)
    {
    }

    public LibraryFileAccessDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
