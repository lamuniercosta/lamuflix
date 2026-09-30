using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence.ValueGenerators;

namespace LamuFlix.UnitTests.Persistence;

public sealed class MovieIdValueGeneratorTests
{
    [Fact]
    public void GeneratesTemporaryValues_IsTrue() =>
        new MovieIdValueGenerator().GeneratesTemporaryValues.ShouldBeTrue();

    [Fact]
    public void Next_GeneratesSequentialNegativeMovieIds()
    {
        var generator = new MovieIdValueGenerator();

        generator.Next(null!).Value.ShouldBe(-1);
        generator.Next(null!).Value.ShouldBe(-2);
        generator.Next(null!).Value.ShouldBe(-3);
    }
}