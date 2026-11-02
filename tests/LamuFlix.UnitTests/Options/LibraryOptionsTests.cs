using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class LibraryOptionsTests
{
    [Fact]
    public void Defaults_RootPath_IsEmpty()
    {
        // arrange
        var options = new LibraryOptions();

        // act
        var rootPath = options.RootPath;

        // assert
        rootPath.ShouldBe(string.Empty);
    }

    [Fact]
    public void LibraryOptions_MaxConcurrency_DefaultIsFour()
    {
        // arrange
        var options = new LibraryOptions();

        // act
        var maxConcurrency = options.MaxConcurrency;

        // assert
        maxConcurrency.ShouldBe(4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(int.MaxValue)]
    public void LibraryOptions_MaxConcurrency_WithinRange_PassesDataAnnotationValidation(int maxConcurrency)
    {
        // arrange
        var options = new LibraryOptions { RootPath = "/library", MaxConcurrency = maxConcurrency };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void LibraryOptions_MaxConcurrency_OutOfRange_FailsDataAnnotationValidation(int maxConcurrency)
    {
        // arrange
        var options = new LibraryOptions { RootPath = "/library", MaxConcurrency = maxConcurrency };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(LibraryOptions.MaxConcurrency));
    }

    [Fact]
    public void TryValidateObject_DefaultRootPath_ReportsRootPathAsRequired()
    {
        // arrange
        var options = new LibraryOptions();

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(LibraryOptions.RootPath));
    }

    private static IReadOnlyList<ValidationResult> Validate(LibraryOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
