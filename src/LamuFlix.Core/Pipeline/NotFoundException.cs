using System;

namespace LamuFlix.Core.Pipeline;

public sealed class NotFoundException : Exception
{
    public NotFoundException()
        : base("Not found.")
    {
    }
}
