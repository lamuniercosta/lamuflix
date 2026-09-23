using System.Collections.Generic;

namespace LamuFlix.Data.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public IList<MovieActors> Movies { get; set; } = [];
    }
}
