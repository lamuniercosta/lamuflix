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
    public void StartupValidation_RecommendationsSection_BindsWithoutCodeChanges()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.Take)}"] = "7";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.CandidatePoolSize)}"] = "25";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.MmrLambda)}"] = "0.5";
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
        bound.Weights.Director.ShouldBe(4);
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
