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
    public void Defaults_RecommendationCaps_MatchTheAgreedValues()
    {
        var options = new RecommendationOptions();

        options.MaxPerBestSeed.ShouldBe(3);
        options.MaxPerCollection.ShouldBe(1);
        options.MaxExcludeCount.ShouldBe(50);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    public void TryValidateObject_RecommendationCapBoundaries_AreAccepted(
        int maxPerBestSeed,
        int maxPerCollection,
        int maxExcludeCount)
    {
        var options = new RecommendationOptions
        {
            MaxPerBestSeed = maxPerBestSeed,
            MaxPerCollection = maxPerCollection,
            MaxExcludeCount = maxExcludeCount,
        };

        Validate(options).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, -1)]
    public void TryValidateObject_InvalidRecommendationCaps_ReportsTheInvalidMember(
        int maxPerBestSeed,
        int maxPerCollection,
        int maxExcludeCount)
    {
        var options = new RecommendationOptions
        {
            MaxPerBestSeed = maxPerBestSeed,
            MaxPerCollection = maxPerCollection,
            MaxExcludeCount = maxExcludeCount,
        };

        var results = Validate(options);

        results.SelectMany(result => result.MemberNames).ShouldContain(
            maxPerBestSeed < 1 ? nameof(RecommendationOptions.MaxPerBestSeed)
            : maxPerCollection < 1 ? nameof(RecommendationOptions.MaxPerCollection)
            : nameof(RecommendationOptions.MaxExcludeCount));
    }

    [Theory]
    [InlineData(nameof(RecommendationOptions.MaxPerBestSeed), "0")]
    [InlineData(nameof(RecommendationOptions.MaxPerCollection), "0")]
    [InlineData(nameof(RecommendationOptions.MaxExcludeCount), "-1")]
    public void StartupValidation_InvalidRecommendationCaps_FailsValidation(string memberName, string value)
    {
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{memberName}"] = value;

        var exception = ValidateStartup(values);

        exception.Message.ShouldContain(memberName);
    }

    [Fact]
    public void Defaults_ThemesOptions_MatchTheNormativeContract()
    {
        // arrange
        var options = new RecommendationOptions();

        // act / assert
        options.ThemeStoplist.ShouldBe(["aftercreditsstinger", "duringcreditsstinger", "sequel", "3d animation"]);
        options.MinimumKeywordMovies.ShouldBe(2);
        options.ThemesCeiling.ShouldBe(0.35);
        options.PlotFloor.ShouldBe(0.16);
        options.PlotCeiling.ShouldBe(0.45);
        options.ThemesDisplayThreshold.ShouldBe(0.3);
        options.PlotDisplayThreshold.ShouldBe(0.5);
        options.GenreDisplayThreshold.ShouldBe(0.5);
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
        options.AlreadyWatchedPenalty.ShouldBe(0.25);
    }

    public static TheoryData<string, double, bool> BlendCoefficientEdges =>
        new()
        {
            { nameof(RecommendationOptions.BlendSelectedWeight), 0.0, false },
            { nameof(RecommendationOptions.BlendSelectedWeight), 1.0, true },
            { nameof(RecommendationOptions.BlendSelectedWeight), -0.0001, false },
            { nameof(RecommendationOptions.BlendSelectedWeight), double.NaN, false },
            { nameof(RecommendationOptions.BlendSelectedWeight), double.PositiveInfinity, false },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), 0.0, true },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), 0.25, true },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), -0.0001, false },
            { nameof(RecommendationOptions.AlreadyWatchedPenalty), double.NegativeInfinity, false },
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
    public void TryValidateObject_SelectedWeightZero_ReportsSelectedMember()
    {
        // arrange
        var options = new RecommendationOptions { BlendSelectedWeight = 0.0 };

        // act
        var results = Validate(options);

        // assert
        var members = results.SelectMany(result => result.MemberNames).ToArray();
        members.ShouldContain(nameof(RecommendationOptions.BlendSelectedWeight));
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

    [Fact]
    public void TryValidateObject_AllScoringWeightsZero_IsRejected()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights { Collection = 0, Director = 0, Themes = 0, Plot = 0, Cast = 0, Genres = 0 },
        };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(RecommendationOptions.Weights));
    }

    [Theory]
    [InlineData(nameof(RecommendationOptions.ThemesCeiling), 0.0)]
    [InlineData(nameof(RecommendationOptions.PlotFloor), 1.1)]
    [InlineData(nameof(RecommendationOptions.PlotCeiling), double.NaN)]
    [InlineData(nameof(RecommendationOptions.MinimumKeywordMovies), 0.0)]
    [InlineData(nameof(RecommendationOptions.ThemesDisplayThreshold), double.PositiveInfinity)]
    [InlineData(nameof(RecommendationOptions.PlotDisplayThreshold), -0.1)]
    [InlineData(nameof(RecommendationOptions.GenreDisplayThreshold), 1.1)]
    public void TryValidateObject_InvalidScoringSetting_IsRejected(string member, double value)
    {
        // arrange
        var options = member switch
        {
            nameof(RecommendationOptions.ThemesCeiling) => new RecommendationOptions { ThemesCeiling = value },
            nameof(RecommendationOptions.PlotFloor) => new RecommendationOptions { PlotFloor = value },
            nameof(RecommendationOptions.PlotCeiling) => new RecommendationOptions { PlotCeiling = value },
            nameof(RecommendationOptions.MinimumKeywordMovies) => new RecommendationOptions { MinimumKeywordMovies = (int)value },
            nameof(RecommendationOptions.ThemesDisplayThreshold) => new RecommendationOptions { ThemesDisplayThreshold = value },
            nameof(RecommendationOptions.PlotDisplayThreshold) => new RecommendationOptions { PlotDisplayThreshold = value },
            _ => new RecommendationOptions { GenreDisplayThreshold = value },
        };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(member);
    }

    [Fact]
    public void TryValidateObject_EqualPlotBounds_AreRejected()
    {
        // arrange
        var options = new RecommendationOptions { PlotFloor = 0.45, PlotCeiling = 0.45 };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(RecommendationOptions.PlotCeiling));
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
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.ThemesCeiling)}"] = "0.4";
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
        bound.ThemesCeiling.ShouldBe(0.4);
        bound.Weights.Director.ShouldBe(4);
        bound.BlendSelectedWeight.ShouldBe(1.0);
        bound.AlreadyWatchedPenalty.ShouldBe(0.25);
    }

    [Fact]
    public void StartupValidation_BlendCoefficients_BindsConfiguredValues()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.BlendSelectedWeight)}"] = "2.5";
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.AlreadyWatchedPenalty)}"] = "0.75";
        using var provider = Build(values);

        // act
        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        // assert
        var bound = provider.GetRequiredService<IOptions<RecommendationOptions>>().Value;
        bound.BlendSelectedWeight.ShouldBe(2.5);
        bound.AlreadyWatchedPenalty.ShouldBe(0.75);
    }

    [Theory]
    [InlineData(nameof(RecommendationOptions.BlendSelectedWeight), "-1")]
    [InlineData(nameof(RecommendationOptions.AlreadyWatchedPenalty), "Infinity")]
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
    public void StartupValidation_SelectedWeightZero_FailsValidation()
    {
        // arrange
        var values = ValidRequired();
        values[$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.BlendSelectedWeight)}"] = "0";

        // act
        var exception = ValidateStartup(values);

        // assert
        exception.Message.ShouldContain(nameof(RecommendationOptions.BlendSelectedWeight));
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

    [Fact]
    public void Defaults_NearestSeedOptions_MatchTheAgreedValues()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var defaults = (
            options.SeedWeightLiked,
            options.SeedWeightWatched,
            options.SeedWeightWatchlist,
            options.DislikedSeedWeight,
            options.RecencyFloor,
            options.SeedRankMultipliers);

        // assert
        defaults.SeedWeightLiked.ShouldBe(1.0);
        defaults.SeedWeightWatched.ShouldBe(0.6);
        defaults.SeedWeightWatchlist.ShouldBe(0.4);
        defaults.DislikedSeedWeight.ShouldBe(1.0);
        defaults.RecencyFloor.ShouldBe(0.25);
        defaults.SeedRankMultipliers.ShouldBe([1.0, 0.5, 0.25]);
    }

    [Fact]
    public void SeedRankMultipliers_DefaultsArePerInstance()
    {
        // arrange
        var first = new RecommendationOptions();

        // act
        var second = new RecommendationOptions();

        // assert
        ReferenceEquals(first.SeedRankMultipliers, second.SeedRankMultipliers).ShouldBeFalse();
        second.SeedRankMultipliers.ShouldBe([1.0, 0.5, 0.25]);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.6, true)]
    [InlineData(1.0, true)]
    [InlineData(-0.0001, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void TryValidateObject_SeedWeight_AtEachEdge_MatchesTheFiniteNonNegativeRule(double value, bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions
        {
            SeedWeightLiked = value,
            SeedWeightWatched = value,
            SeedWeightWatchlist = value,
            DislikedSeedWeight = value,
        };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.SeedWeightLiked), expectedValid);
    }

    [Theory]
    [InlineData(0.25, true)]
    [InlineData(1.0, true)]
    [InlineData(-0.0001, false)]
    [InlineData(1.0001, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void TryValidateObject_RecencyFloor_AtEachEdge_MatchesTheUnitRangeRule(double value, bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { RecencyFloor = value };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.RecencyFloor), expectedValid);
    }

    [Fact]
    public void TryValidateObject_RecencyFloorZero_IsRejected()
    {
        // arrange
        var options = new RecommendationOptions { RecencyFloor = 0.0 };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(RecommendationOptions.RecencyFloor));
    }

    [Fact]
    public void TryValidateObject_SeedRankMultipliers_DefaultsAreAccepted()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    public static TheoryData<double[], bool> SeedRankMultiplierEdges =>
        new()
        {
            { [1.0, 0.5, 0.25], true },
            { [1.0], true },
            { [0.0, 0.0], true },
            { [], false },
            { [0.5, 1.0], false },
            { [-0.1], false },
            { [double.NaN], false },
            { [double.PositiveInfinity], false },
        };

    [Theory]
    [MemberData(nameof(SeedRankMultiplierEdges))]
    public void TryValidateObject_SeedRankMultipliers_MatchTheNonIncreasingFiniteRule(
        double[] multipliers,
        bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { SeedRankMultipliers = multipliers };

        // act
        var results = Validate(options);

        // assert
        AssertEdge(results, nameof(RecommendationOptions.SeedRankMultipliers), expectedValid);
    }

    [Fact]
    public void Validate_SeedRankMultipliersNull_ReturnsSingleRequiredFieldResult()
    {
        // arrange
        var options = new RecommendationOptions { SeedRankMultipliers = null };

        // act
        var results = Validate(options);

        // assert
        results.ShouldHaveSingleItem().MemberNames.ShouldContain(nameof(RecommendationOptions.SeedRankMultipliers));
    }

    private static RecommendationOptions WithBlendCoefficient(string memberName, double value) =>
        memberName switch
        {
            nameof(RecommendationOptions.BlendSelectedWeight) => new() { BlendSelectedWeight = value },
            _ => new() { AlreadyWatchedPenalty = value },
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
