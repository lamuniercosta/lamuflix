using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LamuFlix.Web.Models.Helper;

namespace LamuFlix.Web.Models.Movies
{
    // ReSharper disable NullableWarningSuppressionIsUsed
    // MVC model binder (MoviesController.cs:25)
    public class MoviesFilterViewModel : FilterViewModel
    {
        [DataMapping("Title")]
        public new string SearchField { get; set; } = null!;
        [DataMapping("Year")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global -- MVC-bound filter input consumed by MovieService
        public int? Year { get; set; }
        [DataMapping("Directors", "DirectorId")]
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // Written by the ASP.NET Core MVC model binder (MoviesController.cs:25)
        public int? DirectorId { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        [DataMapping("CollectionId")]
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // Written by the ASP.NET Core MVC model binder (MoviesController.cs:25)
        public int? CollectionId { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        [DataMapping("Genres", "GenreId")]
        // ReSharper disable once CollectionNeverUpdated.Global -- MVC-bound collection filter
        public IList<int> GenreIds { get; set; } = [];
        [DataMapping("Actors", "ActorId")]
        // ReSharper disable CollectionNeverUpdated.Global
        // Written by the ASP.NET Core MVC model binder (MoviesController.cs:25)
        public IList<int> ActorIds { get; set; } = [];
        // ReSharper restore CollectionNeverUpdated.Global
    }
    // ReSharper restore NullableWarningSuppressionIsUsed

    // ReSharper disable NullableWarningSuppressionIsUsed
    // In-product initializer MovieService.cs:136,375,388
    public class MoviesListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int Year { get; set; }
        public int Duration { get; set; }
        [DisplayFormat(DataFormatString = "{0:0.0}")]
        public decimal IMDB { get; set; }
        public int MetaScore { get; set; }
        public int RottenTomatoes { get; set; }
        public string Poster { get; set; } = null!;
        public bool IsInWatchList { get; set; }

        public string WatchListIcon
        {
            get
            {
                if (IsInWatchList)
                {
                    return "fa fa-star fa-3x";
                }
                else
                {
                    return "fa fa-star-o fa-3x";
                }
            }
        }

        public string WatchListMethod
        {
            get
            {
                if (IsInWatchList)
                {
                    return $"RemoveFromWatchList('{Id}')";
                }
                else
                {
                    return $"AddToWatchList('{Id}')";
                }
            }
        }

        public string WatchListTitle
        {
            get
            {
                if (IsInWatchList)
                {
                    return "Remover da Lista";
                }
                else
                {
                    return "Adicionar à Lista";
                }
            }
        }

    }
    // ReSharper restore NullableWarningSuppressionIsUsed

    // ReSharper disable NullableWarningSuppressionIsUsed
    // MVC model binder (MoviesController.cs:90)
    public class ImportMovieFolderViewModel
    {
        public string Movie { get; set; } = null!;
        public string CollectionName { get; set; } = null!;
        public string MovieId { get; set; } = null!;

        public void Clear()
        {
            this.CollectionName = "";
            this.Movie = "";
        }
    }
    // ReSharper restore NullableWarningSuppressionIsUsed

    // ReSharper disable NullableWarningSuppressionIsUsed
    // Newtonsoft.Json deserialization
    public class ApiDataModel
    {
        public string Title { get; set; } = null!;
        public string Year { get; set; } = null!;
        public string Rated { get; set; } = null!;
        public string Released { get; set; } = null!;
        public string Runtime { get; set; } = null!;
        public string Genre { get; set; } = null!;
        public string Director { get; set; } = null!;
        public string Writer { get; set; } = null!;
        public string Actors { get; set; } = null!;
        public string Plot { get; set; } = null!;
        public string Language { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string Awards { get; set; } = null!;
        public string Poster { get; set; } = null!;
        public IList<RatingsModel> Ratings { get; set; } = [];
        public string Metascore { get; set; } = null!;
        public string imdbRating { get; set; } = null!;
        public string imdbVotes { get; set; } = null!;
        public string imdbID { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string DVD { get; set; } = null!;
        public string BoxOffice { get; set; } = null!;
        public string Production { get; set; } = null!;
        public string Website { get; set; } = null!;
        public string Response { get; set; } = null!;
    }
    // ReSharper restore NullableWarningSuppressionIsUsed

    // ReSharper disable NullableWarningSuppressionIsUsed
    // Newtonsoft.Json deserialization
    public class RatingsModel
    {
        public string Source { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
    // ReSharper restore NullableWarningSuppressionIsUsed
}
