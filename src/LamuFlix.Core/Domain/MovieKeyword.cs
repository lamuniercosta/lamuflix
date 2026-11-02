using System;

namespace LamuFlix.Core.Domain;

public sealed record MovieKeyword
{
    public MovieKeyword(int id, string name)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Keyword id must be positive.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Keyword name is required.", nameof(name));
        }

        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }
}
