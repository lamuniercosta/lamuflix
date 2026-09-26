using System.Collections.Generic;

namespace LamuFlix.Data.Models;

public class Genre
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public IList<MovieGenre> Movies { get; set; } = [];
}