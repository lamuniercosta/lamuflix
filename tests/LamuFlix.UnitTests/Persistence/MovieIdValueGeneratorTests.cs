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

#pragma warning disable CS8604, CS8625
        var first = generator.Next(null).Value;
        var second = generator.Next(null).Value;
        var third = generator.Next(null).Value;
#pragma warning restore CS8604, CS8625

        first.ShouldBeLessThan(0);
        second.ShouldBe(first - 1);
        third.ShouldBe(second - 1);
    }
}