using System.Collections.Generic;

namespace LamuFlix.Test;

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
    public string ImdbRating { get; set; } = null!;
    public string ImdbVotes { get; set; } = null!;
    public string ImdbId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Dvd { get; set; } = null!;
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