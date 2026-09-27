using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Domain;

public class EnrichmentFailureCategoryPropertyTests
{
    private static readonly CancellationTokenSource CancelledSource = CreateCancelledSource();

    [Property]
    [Trait("Category", "Property")]
    public void Closed_set_is_exactly_the_four_static_instances()
    {
        // arrange
        var discovered = typeof(EnrichmentFailureCategory)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null))
            .OfType<EnrichmentFailureCategory>()
            .ToArray();

        // act
        var codes = discovered.Select(category => category.Code).OrderBy(code => code, StringComparer.Ordinal).ToArray();

        // assert
        discovered.Length.ShouldBe(4);
        discovered.Distinct().Count().ShouldBe(4);
        codes.ShouldBe(new[] { "invalid_response", "provider_unavailable", "rate_limited", "unknown" });
        discovered.ShouldContain(EnrichmentFailureCategory.ProviderUnavailable);
        discovered.ShouldContain(EnrichmentFailureCategory.RateLimited);
        discovered.ShouldContain(EnrichmentFailureCategory.InvalidResponse);
        discovered.ShouldContain(EnrichmentFailureCategory.Unknown);
    }

    [Property]
    [Trait("Category", "Property")]
    public void Safe_fields_match_frozen_values()
    {
        // arrange
        (EnrichmentFailureCategory Category, string Code, string Description, bool Retryable)[] expected =
        [
            (EnrichmentFailureCategory.ProviderUnavailable, "provider_unavailable", "The metadata provider is temporarily unavailable.", true),
            (EnrichmentFailureCategory.RateLimited, "rate_limited", "The metadata provider is temporarily rate limiting requests.", true),
            (EnrichmentFailureCategory.InvalidResponse, "invalid_response", "The metadata provider returned an unusable response.", false),
            (EnrichmentFailureCategory.Unknown, "unknown", "The enrichment failed for an unknown reason.", true),
        ];

        // act
        // assert
        foreach (var (category, code, description, retryable) in expected)
        {
            category.Code.ShouldBe(code);
            category.SafeDescription.ShouldBe(description);
            category.SafeDescription.ShouldNotBeNullOrWhiteSpace();
            category.IsRetryable.ShouldBe(retryable);
        }
    }

    [Property]
    [Trait("Category", "Property")]
    public Property Classify_GeneratedException_ReturnsCategoryOrPropagatesCallerCancellation() =>
        Prop.ForAll(
            ExceptionSamples().ToArbitrary(),
            exception =>
            {
                try
                {
                    var category = EnrichmentFailureClassifier.Classify(exception);
                    return category == EnrichmentFailureCategory.ProviderUnavailable
                        || category == EnrichmentFailureCategory.RateLimited
                        || category == EnrichmentFailureCategory.InvalidResponse
                        || category == EnrichmentFailureCategory.Unknown;
                }
                catch (OperationCanceledException canceled)
                {
                    return canceled.CancellationToken.IsCancellationRequested;
                }
            });

    private static FsCheck.Gen<Exception> ExceptionSamples() =>
        FsCheck.Fluent.Gen.Elements<Exception>(
            new HttpRequestException("status", null, HttpStatusCode.TooManyRequests),
            new HttpRequestException("status", null, HttpStatusCode.InternalServerError),
            new HttpRequestException("status", null, (HttpStatusCode)408),
            new HttpRequestException("transport"),
            new TimeoutException("timeout"),
            new TaskCanceledException("timeout", new TimeoutException("timeout")),
            new SocketException((int)SocketError.ConnectionRefused),
            new IOException("io"),
            new JsonException("malformed"),
            new HttpRequestException("status", null, HttpStatusCode.Unauthorized),
            new HttpRequestException("status", null, HttpStatusCode.Forbidden),
            new HttpRequestException("status", null, HttpStatusCode.BadRequest),
            new InvalidOperationException("unrecognized"),
            new OperationCanceledException(CancelledSource.Token));

    private static CancellationTokenSource CreateCancelledSource()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        return source;
    }
}
