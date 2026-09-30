using System.Collections.Generic;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class GenreRecord
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ICollection<MovieRecord> Movies { get; } = new HashSet<MovieRecord>();
}
