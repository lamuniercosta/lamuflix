using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class RecommendationOptionsTests
{
    public static TheoryData<int, bool> TakeEdges =>
        new()
        {
            { 1, true },
            { 10, true },
            { 0, false },
            { -1, false },
        };

    public static TheoryData<int, bool> CandidatePoolSizeEdges =>
        new()
        {
            { 10, true },
            { 50, true },
            { 0, false },
            { -1, false },
        };

    public static TheoryData<double, bool> MmrLambdaEdges =>
        new()
        {
            { 0.0, true },
            { 0.7, true },
            { 1.0, true },
            { -0.0001, false },
            { 1.0001, false },
        };

    [Fact]
    public void SectionName_IsRecommendations()
    {
        // arrange
        var sectionName = RecommendationOptions.SectionName;

        // act
        var isExpected = string.Equals(sectionName, "Recommendations", StringComparison.Ordinal);

        // assert
        isExpected.ShouldBeTrue();
    }

    public static TheoryData<double, bool> SimilarStoryThresholdEdges =>
        new()
        {
            { -1.0, true },
            { -0.5, true },
            { 0.0, true },
            { 0.5, true },
            { 1.0, true },
            { -1.0001, false },
            { 1.0001, false },
            { double.NaN, false },
            { double.PositiveInfinity, false },
            { double.NegativeInfinity, false },
        };

    [Fact]
    public void Defaults_TakeCandidatePoolSizeAndMmrLambda_MatchTheAgreedValues()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var defaults = (options.Take, options.CandidatePoolSize, options.MmrLambda);

        // assert
        defaults.Take.ShouldBe(10);
        defaults.CandidatePoolSize.ShouldBe(50);
        defaults.MmrLambda.ShouldBe(0.7);
    }

    [Fact]
    public void Defaults_SimilarStoryThreshold_IsTheAmendedCalibratedMidpoint()
    {
        // arrange
        var options = new RecommendationOptions();

        // act / assert
        options.SimilarStoryThreshold.ShouldBe(0.5);
    }

    [Fact]
    public void Defaults_Weights_AreInitialized()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var weights = options.Weights;

        // assert
        weights.ShouldNotBeNull();
        weights.ShouldBe(new SimilarityWeights());
    }

    [Fact]
    public void Defaults_BlendCoefficients_MatchTheAgreedValues()
    {
        // arrange
        var options = new RecommendationOptions();

        // act / assert
        options.BlendSelectedWeight.ShouldBe(1.0);
        options.BlendProfileWeight.ShouldBe(1.0);
        options.AlreadyWatchedPenalty.ShouldBe(0.25);
        options.NotInterestedPenalty.ShouldBe(1.0);
    }

    public static TheoryData<string, double, bool> BlendCoefficientEdges =>
        new()
        {
            { nameof(RecommendationOptions.BlendSelectedWeight), 0.0, true },
            { nameof(RecommendationOptions.BlendSelectedWeight), 1.0, true },
            { nameof(RecommendationOptions.BlendSelectedWeight), -0.0001, false },
            { nameof(RecommendationOptions.BlendSelectedWeight), double.NaN, false },
            { nameof(RecommendationOptions.BlendSelectedWeight), double.PositiveInfinity, false },
            { nameof(RecommendationOptions.BlendProfileWeight), 0.0, true },
            { nameof(RecommendationOptions.BlendProfileWeight), 2.5, true },
            { nameof(RecommendationOptions.BlendProfileWeight), -0.0001, false },
            { nameof(RecommendationOptions.BlendProfileWeight), double.NaN, false },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), 0.0, true },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), 0.25, true },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), -0.0001, false },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), double.NegativeInfinity, false },
            { nameof(RecommendationOptions.NotInterestedPenalty), 0.0, true },
            { nameof(RecommendationOptions.NotInterestedPenalty), 1.0, true },
            { nameof(RecommendationOptions.NotInterestedPenalty), -0.0001, false },
            { nameof(RecommendationOptions.NotInterestedPenalty), double.NaN, false },
        };

    [Theory]
    [MemberData(nameof(BlendCoefficientEdges))]
    public void TryValidateObject_BlendCoefficient_AtEachEdge_MatchesTheNonNegativeFiniteRule(
        string memberName,
        double value,
        bool expectedValid)
    {
        // arrange
        var options = WithBlendCoefficient(memberName, value);

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, memberName, expectedValid);
    }

    [Fact]
    public void TryValidateObject_SelectedAndProfileWeightsBothZero_ReportsBothMembers()
    {
        // arrange
        var options = new RecommendationOptions { BlendSelectedWeight = 0.0, BlendProfileWeight = 0.0 };

        // act
        var results = Validate(options);

        // assert
        var members = results.SelectMany(result => result.MemberNames).ToArray();
        members.ShouldContain(nameof(RecommendationOptions.BlendSelectedWeight));
        members.ShouldContain(nameof(RecommendationOptions.BlendProfileWeight));
    }

    [Fact]
    public void TryValidateObject_Defaults_AreValid()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(TakeEdges))]
    public void TryValidateObject_Take_AtEachEdge_MatchesThePositiveRange(int take, bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { Take = take, CandidatePoolSize = Math.Max(take, 1) };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.Take), expectedValid);
    }

    [Theory]
    [MemberData(nameof(CandidatePoolSizeEdges))]
    public void TryValidateObject_CandidatePoolSize_AtEachEdge_MatchesThePositiveRange(
        int candidatePoolSize,
        bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { Take = 1, CandidatePoolSize = candidatePoolSize };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.CandidatePoolSize), expectedValid);
    }

    [Theory]
    [MemberData(nameof(MmrLambdaEdges))]
    public void TryValidateObject_MmrLambda_AtEachEdge_MatchesTheUnitRange(double mmrLambda, bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { MmrLambda = mmrLambda };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.MmrLambda), expectedValid);
    }

    [Theory]
    [MemberData(nameof(SimilarStoryThresholdEdges))]
    public void TryValidateObject_SimilarStoryThreshold_AtEachEdge_MatchesTheFiniteUnitRange(
        double similarStoryThreshold,
        bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { SimilarStoryThreshold = similarStoryThreshold };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.SimilarStoryThreshold), expectedValid);
    }

    [Fact]
    public void TryValidateObject_CandidatePoolSizeBelowTake_ReportsCandidatePoolSize()
    {
        // arrange
        var options = new RecommendationOptions { Take = 10, CandidatePoolSize = 9 };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames)
            .ShouldContain(nameof(RecommendationOptions.CandidatePoolSize));
    }

    [Fact]
    public void TryValidateObject_NegativeNestedWeight_ReportsTheWeightMember()
    {
        // arrange
        var options = new RecommendationOptions { Weights = new SimilarityWeights { Director = -1 } };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(SimilarityWeights.Director));
    }

    [Fact]
    public void Validate_WeightsNull_ReturnsSingleRequiredFieldResult()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed - deliberate null exercises the required-field guard.
        var options = new RecommendationOptions { Weights = null! };

        // act
        var results = options.Validate(new ValidationContext(options)).ToArray();

        // assert
        results.Length.ShouldBe(1);
        results[0].ErrorMessage.ShouldBe("The Weights field is required.");
        results[0].MemberNames.ShouldContain(nameof(RecommendationOptions.Weights));
    }

    [Fact]
    public void StartupValidation_RecommendationsSection_BindsWithoutCodeChanges()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.Take)}"] = "7";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.CandidatePoolSize)}"] = "25";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.MmrLambda)}"] = "0.5";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.SimilarStoryThreshold)}"] = "0.6";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.Weights)}:{nameof(SimilarityWeights.Director)}"] = "4";
        using var provider = Build(values);

        // act
        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        // assert
        var bound = provider.GetRequiredService<IOptions<RecommendationOptions>>().Value;
        bound.Take.ShouldBe(7);
        bound.CandidatePoolSize.ShouldBe(25);
        bound.MmrLambda.ShouldBe(0.5);
        bound.SimilarStoryThreshold.ShouldBe(0.6);
        bound.Weights.Director.ShouldBe(4);
        bound.BlendSelectedWeight.ShouldBe(1.0);
        bound.BlendProfileWeight.ShouldBe(1.0);
        bound.AlreadyWatchedPenalty.ShouldBe(0.25);
        bound.NotInterestedPenalty.ShouldBe(1.0);
    }

    [Fact]
    public void StartupValidation_BlendCoefficients_BindsConfiguredValues()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.BlendSelectedWeight)}"] = "2.5";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.BlendProfileWeight)}"] = "0.5";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.AlreadyWatchedPenalty)}"] = "0.75";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.NotInterestedPenalty)}"] = "3";
        using var provider = Build(values);

        // act
        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        // assert
        var bound = provider.GetRequiredService<IOptions<RecommendationOptions>>().Value;
        bound.BlendSelectedWeight.ShouldBe(2.5);
        bound.BlendProfileWeight.ShouldBe(0.5);
        bound.AlreadyWatchedPenalty.ShouldBe(0.75);
        bound.NotInterestedPenalty.ShouldBe(3.0);
    }

    [Theory]
    [InlineData(nameof(RecommendationOptions.BlendSelectedWeight), "-1")]
    [InlineData(nameof(RecommendationOptions.BlendProfileWeight), "NaN")]
    [InlineData(nameof(RecommendationOptions.AlreadyWatchedPenalty), "Infinity")]
    [InlineData(nameof(RecommendationOptions.NotInterestedPenalty), "-0.5")]
    public void StartupValidation_InvalidBlendCoefficient_FailsValidation(string memberName, string value)
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{memberName}"] = value;

        // act
        var exception = ValidateStartup(values);

        // assert
        exception.Message.ShouldContain(memberName);
    }

    [Fact]
    public void StartupValidation_SelectedAndProfileWeightsBothZero_FailsValidation()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.BlendSelectedWeight)}"] = "0";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.BlendProfileWeight)}"] = "0";

        // act
        var exception = ValidateStartup(values);

        // assert
        exception.Message.ShouldContain(nameof(RecommendationOptions.BlendSelectedWeight));
        exception.Message.ShouldContain(nameof(RecommendationOptions.BlendProfileWeight));
    }

    [Fact]
    public void StartupValidation_SimilarStoryThresholdDefaults_ToTheCalibratedMidpoint()
    {
        // arrange
        var values = ValidRequired();
        using var provider = Build(values);

        // act
        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        // assert
        provider.GetRequiredService<IOptions<RecommendationOptions>>().Value
            .SimilarStoryThreshold.ShouldBe(0.5);
    }

    [Theory]
    [InlineData("1.0001")]
    [InlineData("-1.0001")]
    public void StartupValidation_SimilarStoryThresholdOutsideUnitRange_FailsValidation(string value)
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.SimilarStoryThreshold)}"] = value;

        // act
        var exception = ValidateStartup(values);

        // assert
        exception.Message.ShouldContain(nameof(RecommendationOptions.SimilarStoryThreshold));
    }

    [Fact]
    public void StartupValidation_NegativeNestedWeight_FailsValidation()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.Weights)}:{nameof(SimilarityWeights.Cast)}"] = "-1";

        // act
        var exception = ValidateStartup(values);

        // assert
        exception.Message.ShouldContain(nameof(SimilarityWeights.Cast));
    }

    [Fact]
    public void StartupValidation_CandidatePoolSizeBelowTake_FailsValidation()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.Take)}"] = "10";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.CandidatePoolSize)}"] = "9";

        // act
        var exception = ValidateStartup(values);

        // assert
        exception.Message.ShouldContain(nameof(RecommendationOptions.CandidatePoolSize));
    }

    private static RecommendationOptions WithBlendCoefficient(string memberName, double value) =>
        memberName switch
        {
            nameof(RecommendationOptions.BlendSelectedWeight) => new() { BlendSelectedWeight = value },
            nameof(RecommendationOptions.BlendProfileWeight) => new() { BlendProfileWeight = value },
            nameof(RecommendationOptions.AlreadyWatchedPenalty) => new() { AlreadyWatchedPenalty = value },
            _ => new() { NotInterestedPenalty = value },
        };

    private static IReadOnlyList<ValidationResult> Validate(RecommendationOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
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

    private static OptionsValidationException ValidateStartup(Dictionary<string, string?> values)
    {
        using var provider = Build(values);
        var validators = provider.GetServices<IStartupValidator>().ToArray();
        validators.ShouldNotBeEmpty();
        return Should.Throw<OptionsValidationException>(() =>
        {
            foreach (var validator in validators)
            {
                validator.Validate();
            }
        });
    }

    private static ServiceProvider Build(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> ValidRequired() =>
        new()
        {
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = @"C:\library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "test-key",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://www.omdbapi.com/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = "true",
        };
}
