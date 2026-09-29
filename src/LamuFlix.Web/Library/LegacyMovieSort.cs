using Ardalis.SmartEnum;

namespace LamuFlix.Web.Library;

public sealed class LegacyMovieSort : SmartEnum<LegacyMovieSort, int>
{
    public static readonly LegacyMovieSort Id = new(nameof(Id), 0);
    public static readonly LegacyMovieSort Title = new(nameof(Title), 1);
    public static readonly LegacyMovieSort Year = new(nameof(Year), 2);
    public static readonly LegacyMovieSort Duration = new(nameof(Duration), 3);
    public static readonly LegacyMovieSort ImdbRating = new(nameof(ImdbRating), 4);
    public static readonly LegacyMovieSort MetaScore = new(nameof(MetaScore), 5);
    public static readonly LegacyMovieSort RottenTomatoes = new(nameof(RottenTomatoes), 6);

    private LegacyMovieSort(string name, int value)
        : base(name, value)
    {
    }
}
