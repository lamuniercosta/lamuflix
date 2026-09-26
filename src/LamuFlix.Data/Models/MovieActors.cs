namespace LamuFlix.Data.Models;

// ReSharper disable NullableWarningSuppressionIsUsed
// EF Core entity materialization
public class MovieActors
{
    public int MovieId { get; set; }
    public int ActorId { get; set; }

    public Movie Movie { get; set; } = null!;
    public Actor Actor { get; set; } = null!;
}
// ReSharper restore NullableWarningSuppressionIsUsed