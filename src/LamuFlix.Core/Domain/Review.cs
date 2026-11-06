using Ardalis.SmartEnum;

namespace LamuFlix.Core.Domain;

public sealed class Review : SmartEnum<Review, int>
{
    public static readonly Review None = new("none", 0);
    public static readonly Review Liked = new("liked", 1);
    public static readonly Review Disliked = new("disliked", 2);

    private Review(string name, int value)
        : base(name, value)
    {
    }
}
