using System.Linq;
using FluentValidation.TestHelper;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Feedback;
using LamuFlix.Infrastructure.Pipeline;

namespace LamuFlix.UnitTests.Features.Feedback;

public sealed class SetMovieReviewCommandValidatorTests
{
    private readonly SetMovieReviewCommandValidator validator = new();

    public static TheoryData<string?> InvalidReviewNames => new()
    {
        { (string?)null },
        { string.Empty },
        { "   " },
        { "Liked" },
        { "LIKED" },
        { "like" },
        { "2" },
    };

    public static TheoryData<string> ValidReviewNames => new() { "none", "liked", "disliked" };

    [Theory]
    [MemberData(nameof(ValidReviewNames))]
    public void Validate_KnownLowercaseName_HasNoErrors(string name)
    {
        // act
        var result = validator.TestValidate(new SetMovieReviewCommand(new MovieId(1), name));

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(InvalidReviewNames))]
    public void Validate_UnknownOrMissingName_ReportsTheReviewField(string? name)
    {
        // act
        var result = validator.TestValidate(new SetMovieReviewCommand(new MovieId(1), name));

        // assert
        result.Errors.ShouldContain(error => error.PropertyName == "review");
    }

    [Fact]
    public void Validate_InvalidName_ReportsOneReviewError()
    {
        // act
        var result = validator.TestValidate(new SetMovieReviewCommand(new MovieId(1), "Liked"));

        // assert
        result.Errors.Count(error => error.PropertyName == "review").ShouldBe(1);
    }
}
