using Ardalis.SmartEnum;

namespace LamuFlix.Core.Library;

public sealed class MovieSort : SmartEnum<MovieSort, int>
{
    public static readonly MovieSort Title = new(nameof(Title), 0);
    public static readonly MovieSort Year = new(nameof(Year), 1);
    public static readonly MovieSort Rating = new(nameof(Rating), 2);
    public static readonly MovieSort Runtime = new(nameof(Runtime), 3);

    private MovieSort(string name, int value)
        : base(name, value)
    {
    }
}
