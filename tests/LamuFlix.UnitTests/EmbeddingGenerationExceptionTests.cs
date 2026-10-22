using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests;

public sealed class EmbeddingGenerationExceptionTests
{
    public static TheoryData<EnrichmentFailureCategory> AllowedCategories =>
        new()
        {
            EnrichmentFailureCategory.ProviderUnavailable,
            EnrichmentFailureCategory.RateLimited,
            EnrichmentFailureCategory.InvalidResponse,
        };

    public static TheoryData<EnrichmentFailureCategory> DisallowedCategories =>
        new()
        {
            EnrichmentFailureCategory.Unknown,
            EnrichmentFailureCategory.DuplicateImdbId,
        };

    [Theory]
    [MemberData(nameof(AllowedCategories))]
    public void Constructor_AllowedCategory_CarriesTheExistingCategory(EnrichmentFailureCategory category)
    {
        // act
        var exception = new EmbeddingGenerationException(category, "sanitized message");

        // assert
        exception.Category.ShouldBeSameAs(category);
        exception.Message.ShouldBe("sanitized message");
    }

    [Theory]
    [MemberData(nameof(AllowedCategories))]
    public void Constructor_AllowedCategoryWithInnerException_PreservesTheInnerException(EnrichmentFailureCategory category)
    {
        // arrange
        var inner = new TimeoutException("inner");

        // act
        var exception = new EmbeddingGenerationException(category, "sanitized message", inner);

        // assert
        exception.Category.ShouldBeSameAs(category);
        exception.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void Category_IsTheExistingEnrichmentFailureCategoryType()
    {
        // act
        var categoryType = typeof(EmbeddingGenerationException).GetProperty(nameof(EmbeddingGenerationException.Category))?.PropertyType;

        // assert
        categoryType.ShouldNotBeNull();
        categoryType.ShouldBe(typeof(EnrichmentFailureCategory));
        categoryType.IsEnum.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(DisallowedCategories))]
    public void Constructor_CategoryOutsideTheAllowedSet_IsRejected(EnrichmentFailureCategory category)
    {
        // act
        var exception = Should.Throw<ArgumentException>(
            () => new EmbeddingGenerationException(category, "sanitized message"));

        // assert
        exception.Message.ShouldContain(nameof(EmbeddingGenerationException.Category));
    }

    [Fact]
    public void Constructor_NullCategory_IsRejected()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(
            () => new EmbeddingGenerationException(null!));

        // assert
        exception.ParamName.ShouldBe("category");
    }

    [Fact]
    public void Constructor_NoMessage_ProducesASanitizedCategoryMessage()
    {
        // act
        var exception = new EmbeddingGenerationException(EnrichmentFailureCategory.ProviderUnavailable);

        // assert
        exception.Category.ShouldBeSameAs(EnrichmentFailureCategory.ProviderUnavailable);
        exception.Message.ShouldContain(EnrichmentFailureCategory.ProviderUnavailable.Code);
    }

    [Fact]
    public void Constructor_TwoArguments_NullCategory_IsRejected()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(
            () => new EmbeddingGenerationException(null!, "sanitized message"));

        // assert
        exception.ParamName.ShouldBe("category");
    }

    [Fact]
    public void Constructor_ThreeArguments_NullCategory_IsRejected()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(
            () => new EmbeddingGenerationException(null!, "sanitized message", new TimeoutException("inner")));

        // assert
        exception.ParamName.ShouldBe("category");
    }

    [Theory]
    [MemberData(nameof(DisallowedCategories))]
    public void Constructor_ThreeArguments_CategoryOutsideTheAllowedSet_IsRejected(EnrichmentFailureCategory category)
    {
        // act
        var exception = Should.Throw<ArgumentException>(
            () => new EmbeddingGenerationException(category, "sanitized message", new TimeoutException("inner")));

        // assert
        exception.Message.ShouldContain(nameof(EmbeddingGenerationException.Category));
    }

    [Theory]
    [MemberData(nameof(DisallowedCategories))]
    public void Constructor_CategoryOutsideTheAllowedSet_MessageNamesTheAllowedSet(EnrichmentFailureCategory category)
    {
        // act
        var exception = Should.Throw<ArgumentException>(
            () => new EmbeddingGenerationException(category, "sanitized message"));

        // assert
        exception.Message.ShouldBe("Only ProviderUnavailable, RateLimited and InvalidResponse categories are allowed. (Parameter 'category')");
    }
}
