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
