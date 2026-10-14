using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class EnrichmentFailureClassifierSurvivorTests
{
    [Fact]
    public void Classify_NullException_ThrowsArgumentNullExceptionNamingException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => EnrichmentFailureClassifier.Classify(null!));

        // assert
        thrown.ParamName.ShouldBe("exception");
    }

    [Theory]
    [InlineData(100, "unknown")]
    [InlineData(200, "unknown")]
    [InlineData(300, "unknown")]
    [InlineData(399, "unknown")]
    [InlineData(400, "invalid_response")]
    [InlineData(407, "invalid_response")]
    [InlineData(408, "provider_unavailable")]
    [InlineData(409, "invalid_response")]
    [InlineData(429, "rate_limited")]
    [InlineData(430, "invalid_response")]
    [InlineData(499, "invalid_response")]
    [InlineData(500, "provider_unavailable")]
    [InlineData(501, "provider_unavailable")]
    [InlineData(598, "provider_unavailable")]
    [InlineData(599, "provider_unavailable")]
    [InlineData(600, "unknown")]
    public void Classify_HttpStatusBoundary_MapsToExactCategory(int statusCode, string expectedCode)
    {
        // arrange
        var exception = new HttpRequestException("request failed", inner: null, (HttpStatusCode)statusCode);

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void Classify_CancelledOperationWithoutTimeout_RethrowsTheSameCancellation()
    {
        // arrange
        using var source = new CancellationTokenSource();
        source.Cancel();
        var exception = new OperationCanceledException(source.Token);

        // act
        var thrown = Should.Throw<OperationCanceledException>(() => EnrichmentFailureClassifier.Classify(exception));

        // assert
        thrown.ShouldBeSameAs(exception);
    }

    [Fact]
    public void Classify_UncancelledOperationWithoutInnerException_ReturnsUnknown()
    {
        // arrange
        var exception = new OperationCanceledException("stopped");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_UncancelledOperationWithJsonInner_WalksToInvalidResponse()
    {
        // arrange
        var exception = new OperationCanceledException("stopped", new JsonException("bad"));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public void Classify_CancelledOperationWithTimeoutInner_ReturnsProviderUnavailable()
    {
        // arrange
        using var source = new CancellationTokenSource();
        source.Cancel();
        var exception = new OperationCanceledException("timed out", new TimeoutException(), source.Token);

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_ExceptionWithNoCategoryAnywhere_ReturnsUnknown()
    {
        // arrange
        var exception = new InvalidOperationException("nothing maps");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_InnerExceptionWithCategory_WalksToTheInnerCategory()
    {
        // arrange
        var exception = new InvalidOperationException("outer", new JsonException("inner"));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public void Classify_AggregateWithSingleCategorizedInner_ReturnsTheInnerCategory()
    {
        // arrange
        var exception = new AggregateException(new JsonException("bad"));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public void Classify_AggregateWithoutInnerExceptions_ReturnsUnknown()
    {
        // arrange
        var exception = new AggregateException();

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_AggregateWithUncategorizedThenCategorizedInner_SkipsToTheCategory()
    {
        // arrange
        var exception = new AggregateException(
            new InvalidOperationException("nothing maps"),
            new JsonException("bad"));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public void Classify_AggregateWithTwoCategorizedInner_ReturnsTheFirstInnerCategory()
    {
        // arrange
        var exception = new AggregateException(
            new TimeoutException("first"),
            new JsonException("second"));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }
}
