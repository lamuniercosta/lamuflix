using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Features.Enrichment;

public class EnrichmentFailureClassifierTests
{
    [Theory]
    [InlineData(429, "rate_limited")]
    [InlineData(500, "provider_unavailable")]
    [InlineData(502, "provider_unavailable")]
    [InlineData(503, "provider_unavailable")]
    [InlineData(504, "provider_unavailable")]
    [InlineData(599, "provider_unavailable")]
    [InlineData(408, "provider_unavailable")]
    [InlineData(401, "invalid_response")]
    [InlineData(403, "invalid_response")]
    [InlineData(400, "invalid_response")]
    [InlineData(404, "invalid_response")]
    [InlineData(499, "invalid_response")]
    [InlineData(100, "unknown")]
    [InlineData(200, "unknown")]
    [InlineData(300, "unknown")]
    [InlineData(600, "unknown")]
    [InlineData(999, "unknown")]
    public void Classify_HttpStatus_MapsToFrozenCategory(int statusCode, string expectedCode)
    {
        // arrange
        var exception = new HttpRequestException("request failed", inner: null, (HttpStatusCode)statusCode);

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void Classify_NullHttpStatus_ReturnsProviderUnavailable()
    {
        // arrange
        var exception = new HttpRequestException("request failed", new JsonException("malformed"));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_UnmappedStatusWithInnerTimeout_DoesNotFallThrough()
    {
        // arrange
        var exception = new HttpRequestException("request failed", new TimeoutException(), (HttpStatusCode)200);

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_TimeoutException_ReturnsProviderUnavailable()
    {
        // arrange
        var exception = new TimeoutException("timed out");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_TimeoutShapedTaskCanceledException_ReturnsProviderUnavailable()
    {
        // arrange
        var exception = new TaskCanceledException("timed out", new TimeoutException());

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_CancelledTimeoutShapedOperationCanceled_ReturnsProviderUnavailable()
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
    public void Classify_SocketException_ReturnsProviderUnavailable()
    {
        // arrange
        var exception = new SocketException((int)SocketError.ConnectionRefused);

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_IOException_ReturnsProviderUnavailable()
    {
        // arrange
        var exception = new IOException("read failed");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_JsonException_ReturnsInvalidResponse()
    {
        // arrange
        var exception = new JsonException("malformed");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public void Classify_UnrecognizedException_ReturnsUnknown()
    {
        // arrange
        var exception = new InvalidOperationException("unrecognized");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_CallerCancelled_Rethrows()
    {
        // arrange
        using var source = new CancellationTokenSource();
        source.Cancel();
        var exception = new OperationCanceledException(source.Token);

        // act
        var thrown = Assert.Throws<OperationCanceledException>(() => EnrichmentFailureClassifier.Classify(exception));

        // assert
        thrown.CancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public void Classify_BareOperationCanceled_ReturnsUnknown()
    {
        // arrange
        var exception = new OperationCanceledException("bare");

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_Null_ThrowsArgumentNullException()
    {
        // arrange
        // act
#pragma warning disable CS8604, CS8625
        var thrown = Should.Throw<ArgumentNullException>(() => EnrichmentFailureClassifier.Classify(null));
#pragma warning restore CS8604, CS8625

        // assert
        thrown.ParamName.ShouldBe("exception");
    }

    [Fact]
    public void Classify_NestedAggregateWithHttpStatus_OuterStatusWins()
    {
        // arrange
        var http = new HttpRequestException("request failed", new TimeoutException(), (HttpStatusCode)429);
        var exception = new AggregateException(new AggregateException(http, new IOException("read failed")));

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.RateLimited);
    }

    [Fact]
    public void Classify_Http429WithInnerTimeout_PrefersStatus()
    {
        // arrange
        var exception = new HttpRequestException("request failed", new TimeoutException(), (HttpStatusCode)429);

        // act
        var category = EnrichmentFailureClassifier.Classify(exception);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.RateLimited);
    }

    [Fact]
    public void Classify_CyclicInnerException_ReturnsUnknownWithoutLooping()
    {
        // arrange
        var left = new InvalidOperationException("left");
        var right = new InvalidOperationException("right");
        Link(left, right);
        Link(right, left);

        // act
        var category = EnrichmentFailureClassifier.Classify(left);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public void Classify_CyclicTimeout_ReturnsProviderUnavailableWithoutLooping()
    {
        // arrange
        var left = new TimeoutException("left");
        var right = new InvalidOperationException("right");
        Link(left, right);
        Link(right, left);

        // act
        var category = EnrichmentFailureClassifier.Classify(left);

        // assert
        category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public void Classify_CallerCancelledTaskCanceled_Rethrows()
    {
        // arrange
        using var source = new CancellationTokenSource();
        source.Cancel();
        var exception = new TaskCanceledException(Task.FromCanceled(source.Token));

        // act
        var thrown = Assert.Throws<TaskCanceledException>(() => EnrichmentFailureClassifier.Classify(exception));

        // assert
        thrown.CancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    private static void Link(Exception outer, Exception inner)
    {
        var field = typeof(Exception).GetField("_innerException", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        field.SetValue(outer, inner);
    }
}
