using Ardalis.SmartEnum;

namespace LamuFlix.Core.Domain;

public sealed class Review : SmartEnum<Review, int>
{
    // Stryker disable once all : static enum member is initialised once per mutant-test server, so a mutant here is baked into every later mutant (ADR 0016)
    public static readonly Review None = new("none", 0);
    // Stryker disable once all : static enum member is initialised once per mutant-test server, so a mutant here is baked into every later mutant (ADR 0016)
    public static readonly Review Liked = new("liked", 1);
    // Stryker disable once all : static enum member is initialised once per mutant-test server, so a mutant here is baked into every later mutant (ADR 0016)
    public static readonly Review Disliked = new("disliked", 2);

    private Review(string name, int value)
        : base(name, value)
    {
    }

    public bool IsNone => Is(None);

    public bool IsLiked => Is(Liked);

    public bool IsDisliked => Is(Disliked);

    public static bool AreEqual(Review? first, Review? second) =>
        first is null ? second is null : first.Is(second);

    public bool Is(Review? other) => other is not null && Equals(other);

    public static bool TryParse(string? name, out Review review)
    {
        if (name is null || !TryFromName(name, false, out var parsed))
        {
            review = None;
            return false;
        }

        review = parsed;
        return true;
    }
}
