using System.Reflection;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class OmdbQuotaOptionsTests
{
    public static TheoryData<int, bool> DailyLimitStates =>
        new()
        {
            { 1, true },
            { int.MaxValue, true },
            { 0, false },
            { -1, false },
        };

    [Fact]
    public void Defaults_DailyLimit_IsAbsent()
    {
        // arrange
        var options = new OmdbQuotaOptions();

        // act
        var dailyLimit = options.DailyLimit;

        // assert
        dailyLimit.ShouldBe(0);
        options.IsEnabled.ShouldBeFalse();
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
    [MemberData(nameof(DailyLimitStates))]
    public void IsEnabled_IsTrueOnlyForAPositiveAllowance(int dailyLimit, bool expected)
    {
        // arrange
        var options = new OmdbQuotaOptions { DailyLimit = dailyLimit };

        // act
        var enabled = options.IsEnabled;

        // assert
        enabled.ShouldBe(expected);
    }
}
