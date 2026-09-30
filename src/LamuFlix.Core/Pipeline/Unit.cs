namespace LamuFlix.Core.Pipeline;

public sealed record Unit
{
    private Unit()
    {
    }

    public static readonly Unit Value = new();
}
