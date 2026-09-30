using System.Collections.Generic;

namespace LamuFlix.Test;

public class ApiDataModel
{
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Title { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Year { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Rated { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Released { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Runtime { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Genre { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Director { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Writer { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Actors { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Plot { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Language { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Country { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Awards { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Poster { get; set; } = null!;
    public IList<RatingsModel> Ratings { get; set; } = [];
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Metascore { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string ImdbRating { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string ImdbVotes { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string ImdbId { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Type { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Dvd { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string BoxOffice { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Production { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Website { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Response { get; set; } = null!;
}

public class RatingsModel
{
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Source { get; set; } = null!;
    // ReSharper disable once NullableWarningSuppressionIsUsed Newtonsoft.Json deserialization populates this property
    public string Value { get; set; } = null!;
}