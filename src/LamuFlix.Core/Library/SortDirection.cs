using Ardalis.SmartEnum;

namespace LamuFlix.Core.Library;

public sealed class SortDirection : SmartEnum<SortDirection, int>
{
    public static readonly SortDirection Ascending = new(nameof(Ascending), 0);
    public static readonly SortDirection Descending = new(nameof(Descending), 1);

    private SortDirection(string name, int value)
        : base(name, value)
    {
    }
}
