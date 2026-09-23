using System.Collections.Generic;

namespace LamuFlix.Data.Models
{
    public class Collection
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public IList<Movie> Movies { get; set; } = [];
    }
}
