namespace LamuFlix.Core.Features.Recommendations;

public enum ReasonKind
{
    Structural,
    Personal,
}

// ReSharper disable NotAccessedPositionalProperty.Global // Public scorer contract consumed by callers and tests
public sealed record Reason(ReasonKind Kind, string Text)
{
    public static Reason Structural(string text) => new(ReasonKind.Structural, text);

    public static Reason Personal(string text) => new(ReasonKind.Personal, text);
}
// ReSharper restore NotAccessedPositionalProperty.Global
