using LamuFlix.Infrastructure.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQueryGeneratorTests
{
    [Fact]
    [Trait("Category", "Property")]
    public void Generator_ProducesOnlyValidatorAcceptedQueries()
    {
        var validator = new MovieQueryValidator();
        Prop.ForAll(MovieQueryFixture.Queries(), query => validator.Validate(query).IsValid)
            .QuickCheckThrowOnFailure();
    }
}
