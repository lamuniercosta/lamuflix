using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class OllamaResilienceOptionsTests
{
    public static TheoryData<int, bool> MaxRetryAttemptEdges =>
        new()
        {
            { 1, true },
            { int.MaxValue, true },
            { 0, false },
            { -1, false },
        };

    public static TheoryData<string, bool> BaseDelayEdges =>
        new()
        {
            { "00:00:00", true },
            { "1.00:00:00", true },
            { "-00:00:00.0000001", false },
            { "1.00:00:00.0000001", false },
        };

    public static TheoryData<string, bool> AttemptTimeoutEdges =>
        new()
        {
            { "00:00:00.010", true },
            { "1.00:00:00", true },
            { "00:00:00.0099999", false },
            { "1.00:00:00.0000001", false },
        };

    public static TheoryData<string, bool> TotalTimeoutEdges =>
        new()
        {
            { "00:00:00.010", true },
            { "1.00:00:00", true },
            { "00:00:00.0099999", false },
            { "1.00:00:00.0000001", false },
        };

    public static TheoryData<double, bool> FailureRatioEdges =>
        new()
        {
            { 0.0, true },
            { 1.0, true },
            { -0.0001, false },
            { 1.0001, false },
        };

    public static TheoryData<string, bool> SamplingDurationEdges =>
        new()
        {
            { "00:00:00.500", true },
            { "1.00:00:00", true },
            { "00:00:00.4999999", false },
            { "1.00:00:00.0000001", false },
        };

    public static TheoryData<int, bool> MinimumThroughputEdges =>
        new()
        {
            { 2, true },
            { int.MaxValue, true },
            { 1, false },
            { 0, false },
        };

    public static TheoryData<string, bool> BreakDurationEdges =>
        new()
        {
            { "00:00:00.500", true },
            { "1.00:00:00", true },
            { "00:00:00.4999999", false },
            { "1.00:00:00.0000001", false },
        };

    public static TheoryData<string, string, string, string, bool> CrossFieldEdges =>
        new()
        {
            { "00:00:00.500", "00:00:00.500", "00:00:01.000", string.Empty, true },
            { "00:00:00.500", "00:00:00.400", "00:00:01.000", nameof(OllamaResilienceOptions.AttemptTimeout), false },
            { "00:00:00.500", "00:00:00.500", "00:00:00.999", nameof(OllamaResilienceOptions.SamplingDuration), false },
        };

    [Fact]
    public void Defaults_ResilienceNumbers_MatchTheAgreedValues()
    {
        // arrange
        var options = new OllamaResilienceOptions();

        // act
        var defaults = (
            options.MaxRetryAttempts,
            options.BaseDelay,
            options.AttemptTimeout,
            options.TotalTimeout,
            options.FailureRatio,
            options.SamplingDuration,
            options.MinimumThroughput,
            options.BreakDuration);

        // assert
        defaults.MaxRetryAttempts.ShouldBe(3);
        defaults.BaseDelay.ShouldBe(TimeSpan.FromSeconds(1));
        defaults.AttemptTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        defaults.TotalTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        defaults.FailureRatio.ShouldBe(0.1);
        defaults.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(30));
        defaults.MinimumThroughput.ShouldBe(100);
        defaults.BreakDuration.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SectionName_IsTheVendorMandatedResilienceKey()
    {
        // arrange
        var sectionName = OllamaResilienceOptions.SectionName;

        // act
        var isExpected = string.Equals(sectionName, "Ollama:Resilience", StringComparison.Ordinal);

        // assert
        isExpected.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateObject_Defaults_AreValid()
    {
        // arrange
        var options = new OllamaResilienceOptions();

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(MaxRetryAttemptEdges))]
    public void TryValidateObject_MaxRetryAttempts_AtEachEdge_MatchesTheLibraryRange(int attempts, bool expectedValid)
    {
        // act
        var results = ValidateMember(attempts, nameof(OllamaResilienceOptions.MaxRetryAttempts));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.MaxRetryAttempts), expectedValid);
    }

    [Theory]
    [MemberData(nameof(BaseDelayEdges))]
    public void TryValidateObject_BaseDelay_AtEachEdge_MatchesTheLibraryRange(string delay, bool expectedValid)
    {
        // act
        var results = ValidateMember(Parse(delay), nameof(OllamaResilienceOptions.BaseDelay));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.BaseDelay), expectedValid);
    }

    [Theory]
    [MemberData(nameof(AttemptTimeoutEdges))]
    public void TryValidateObject_AttemptTimeout_AtEachEdge_MatchesTheLibraryRange(string timeout, bool expectedValid)
    {
        // act
        var results = ValidateMember(Parse(timeout), nameof(OllamaResilienceOptions.AttemptTimeout));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.AttemptTimeout), expectedValid);
    }

    [Theory]
    [MemberData(nameof(TotalTimeoutEdges))]
    public void TryValidateObject_TotalTimeout_AtEachEdge_MatchesTheLibraryRange(string timeout, bool expectedValid)
    {
        // act
        var results = ValidateMember(Parse(timeout), nameof(OllamaResilienceOptions.TotalTimeout));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.TotalTimeout), expectedValid);
    }

    [Theory]
    [MemberData(nameof(FailureRatioEdges))]
    public void TryValidateObject_FailureRatio_AtEachEdge_MatchesTheLibraryRange(double ratio, bool expectedValid)
    {
        // act
        var results = ValidateMember(ratio, nameof(OllamaResilienceOptions.FailureRatio));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.FailureRatio), expectedValid);
    }

    [Theory]
    [MemberData(nameof(SamplingDurationEdges))]
    public void TryValidateObject_SamplingDuration_AtEachEdge_MatchesTheLibraryRange(string duration, bool expectedValid)
    {
        // act
        var results = ValidateMember(Parse(duration), nameof(OllamaResilienceOptions.SamplingDuration));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.SamplingDuration), expectedValid);
    }

    [Theory]
    [MemberData(nameof(MinimumThroughputEdges))]
    public void TryValidateObject_MinimumThroughput_AtEachEdge_MatchesTheLibraryRange(int throughput, bool expectedValid)
    {
        // act
        var results = ValidateMember(throughput, nameof(OllamaResilienceOptions.MinimumThroughput));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.MinimumThroughput), expectedValid);
    }

    [Theory]
    [MemberData(nameof(BreakDurationEdges))]
    public void TryValidateObject_BreakDuration_AtEachEdge_MatchesTheLibraryRange(string duration, bool expectedValid)
    {
        // act
        var results = ValidateMember(Parse(duration), nameof(OllamaResilienceOptions.BreakDuration));

        // assert
        AssertEdge(results, nameof(OllamaResilienceOptions.BreakDuration), expectedValid);
    }

    [Theory]
    [MemberData(nameof(CrossFieldEdges))]
    public void TryValidateObject_CrossFieldRules_EnforceTheStandardHandlerInvariants(
        string attemptTimeout,
        string totalTimeout,
        string samplingDuration,
        string blamedMember,
        bool expectedValid)
    {
        // arrange
        var options = new OllamaResilienceOptions
        {
            AttemptTimeout = Parse(attemptTimeout),
            TotalTimeout = Parse(totalTimeout),
            SamplingDuration = Parse(samplingDuration),
        };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, blamedMember, expectedValid);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_DurationsMeetingTheCrossFieldRules_AreAccepted(int attemptTicks, int totalExtraTicks, int samplingExtraTicks)
    {
        var attempt = TimeSpan.FromTicks(100_000 + (uint)attemptTicks % 36_000_000_000L);
        var total = attempt + TimeSpan.FromTicks((uint)totalExtraTicks % (TimeSpan.FromDays(1).Ticks - attempt.Ticks));
        var samplingFloor = Math.Max(attempt.Ticks * 2, TimeSpan.FromMilliseconds(500).Ticks);
        var sampling = TimeSpan.FromTicks(samplingFloor)
            + TimeSpan.FromTicks((uint)samplingExtraTicks % (TimeSpan.FromDays(1).Ticks - samplingFloor));

        var options = new OllamaResilienceOptions
        {
            AttemptTimeout = attempt,
            TotalTimeout = total,
            SamplingDuration = sampling,
        };

        return Validate(options).Count == 0;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_AttemptTimeoutLargerThanTotalTimeout_IsRejected(int attemptTicks, int extraTicks)
    {
        var total = TimeSpan.FromTicks(100_000 + (uint)extraTicks % 36_000_000_000L);
        var attempt = total + TimeSpan.FromTicks(1 + ((uint)attemptTicks % 1_000_000));

        var options = new OllamaResilienceOptions
        {
            AttemptTimeout = attempt,
            TotalTimeout = total,
            SamplingDuration = TimeSpan.FromDays(1),
        };

        return Validate(options)
            .SelectMany(result => result.MemberNames)
            .Contains(nameof(OllamaResilienceOptions.AttemptTimeout));
    }

    private static TimeSpan Parse(string value) => TimeSpan.Parse(value, CultureInfo.InvariantCulture);

    private static IReadOnlyList<ValidationResult> Validate(OllamaResilienceOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    private static IReadOnlyList<ValidationResult> ValidateMember(object? value, string memberName)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateProperty(
            value,
            new ValidationContext(new OllamaResilienceOptions()) { MemberName = memberName },
            results);
        return results;
    }

    private static void AssertEdge(IReadOnlyList<ValidationResult> results, string memberName, bool expectedValid)
    {
        if (expectedValid)
        {
            results.ShouldBeEmpty();
            return;
        }

        results.SelectMany(result => result.MemberNames).ShouldContain(memberName);
    }
}
