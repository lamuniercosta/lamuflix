using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class LibraryOptionsValidatorTests
{
    private readonly LibraryOptionsValidator validator = new();

    [Fact]
    public void Validate_DefaultsWithoutLegacyRoots_Succeeds()
    {
        // act
        var result = validator.Validate(null, new LibraryOptions { RootPath = "/media/library" });

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData("/media/library")]
    [InlineData("C:/library")]
    [InlineData(@"C:\library")]
    [InlineData(@"\\nas\media\library")]
    [InlineData("//nas/media/library")]
    public void Validate_WellFormedAbsoluteAlias_Succeeds(string alias)
    {
        // arrange
        var options = new LibraryOptions { RootPath = "/media/library", LegacyRootPaths = [alias] };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData("media/library")]
    [InlineData("./library")]
    [InlineData("C:library")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MalformedOrNonAbsoluteAlias_FailsWithIndexedKey(string alias)
    {
        // arrange
        var options = new LibraryOptions { RootPath = "/media/library", LegacyRootPaths = [alias] };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain("Library:LegacyRootPaths[0]");
    }

    [Fact]
    public void Validate_OverlappingAliases_FailsWithConflictDiagnostic()
    {
        // arrange
        var options = new LibraryOptions
        {
            RootPath = "/media/library",
            LegacyRootPaths = ["/media/library", "/media/library/archive"],
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain("overlap");
    }

    [Fact]
    public void Validate_CaseOnlyWindowsAliasOverlap_FailsAsConflict()
    {
        // arrange
        var options = new LibraryOptions
        {
            RootPath = "/media/library",
            LegacyRootPaths = [@"C:\Library", "c:/library"],
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DistinctAliasesAndActiveRoot_Succeeds()
    {
        // arrange
        var options = new LibraryOptions
        {
            RootPath = "/media/library",
            LegacyRootPaths = [@"C:\library", "D:/media/library"],
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        // act
        var thrown = Should.Throw<System.ArgumentNullException>(() => validator.Validate(null, null!));

        // assert
        thrown.ParamName.ShouldBe("options");
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersLibraryValidatorAndKeepsDataAnnotations()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddLamuFlixOptions();

        // assert
        services.Any(descriptor => descriptor.ServiceType == typeof(IValidateOptions<LibraryOptions>)
            && descriptor.ImplementationType == typeof(LibraryOptionsValidator)).ShouldBeTrue();
        services.Any(descriptor => descriptor.ServiceType == typeof(IStartupValidator)).ShouldBeTrue();
        services.Any(descriptor => descriptor.ServiceType == typeof(IValidateOptions<EnrichmentOptions>)).ShouldBeTrue();
        services.Any(descriptor => descriptor.ServiceType == typeof(IValidateOptions<ThumbnailOptions>)).ShouldBeTrue();
    }
}
