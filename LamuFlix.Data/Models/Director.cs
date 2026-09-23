using System.Collections.Generic;

namespace LamuFlix.Data.Models
{
    public class Director
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public IList<MovieDirectors> Movies { get; set; } = [];
    }
}
