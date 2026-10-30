using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class OmdbQuotaOptionsTests
{
    public static TheoryData<int, bool> DailyLimitEdges =>
        new()
        {
            { 1, true },
            { int.MaxValue, true },
            { 0, false },
            { -1, false },
        };

    [Fact]
    public void Defaults_DailyLimit_IsOneThousand()
    {
        // arrange
        var options = new OmdbQuotaOptions();

        // act
        var dailyLimit = options.DailyLimit;

        // assert
        dailyLimit.ShouldBe(1000);
    }

    [Fact]
    public void SectionName_MatchesTheOmdbQuotaSection()
    {
        // act
        var sectionName = OmdbQuotaOptions.SectionName;

        // assert
        sectionName.ShouldBe("Omdb:Quota");
    }

    [Fact]
    public void Type_IsSealedRecord()
    {
        // act
        var type = typeof(OmdbQuotaOptions);

        // assert
        type.IsSealed.ShouldBeTrue();
        type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(DailyLimitEdges))]
    public void TryValidateObject_DailyLimit_AtEachEdge_MatchesTheRange(int dailyLimit, bool expectedValid)
    {
        // arrange
        var options = new OmdbQuotaOptions { DailyLimit = dailyLimit };

        // act
        var results = Validate(options);

        // assert
        if (expectedValid)
        {
            results.ShouldBeEmpty();
            return;
        }

        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(OmdbQuotaOptions.DailyLimit));
    }

    private static IReadOnlyList<ValidationResult> Validate(OmdbQuotaOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
