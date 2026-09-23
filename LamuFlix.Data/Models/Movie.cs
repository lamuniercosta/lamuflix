using System.Collections.Generic;

namespace LamuFlix.Data.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Synopsis { get; set; }
        public int Year { get; set; }
        public int Duration { get; set; }
        public string? Poster { get; set; }
        public decimal ImdbRating { get; set; }
        public int RottenTomatoes { get; set; }
        public int MetaScore { get; set; }
        public string Location { get; set; } = null!;
        public string Format { get; set; } = null!;
        public int? CollectionId { get; set; }
        public string? IdImdb { get; set; }
        public bool IsInWatchList { get; set; }
        public MovieEnrichmentStatus Status { get; set; } = MovieEnrichmentStatus.Pending;

        public Collection Collection { get; set; } = null!;
        public IList<MovieGenre> Genres { get; set; } = [];
        public IList<MovieActors> Actors { get; set; } = [];
        public IList<MovieDirectors> Directors { get; set; } = [];
    }
}
