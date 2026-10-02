using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(MetadataProviderCollection.Name)]
public sealed class MetadataProviderLookupTests(MetadataProviderProbe probe) : IClassFixture<MetadataProviderProbe>, IAsyncLifetime
{
    private const string FullBody = """
        {
          "Response": "True",
          "Title": "Blade Runner",
          "Year": "1982",
          "Runtime": "117 min",
          "Plot": "A blade runner must pursue and terminate four replicants.",
          "imdbRating": "8.1",
          "imdbID": "tt0083658"
        }
        """;

    private const string SparseBody = """
        {
          "Response": "True",
          "Title": "Solaris",
          "Plot": "N/A"
        }
        """;

    private const string NotFoundBody = """
        { "Response": "False", "Error": "Movie not found!" }
        """;

    private const string OtherErrorBody = """
        { "Response": "False", "Error": "Invalid API key!" }
        """;

    public static TheoryData<string, string> DomainInvalidBodies =>
        new()
        {
            { nameof(MovieMetadata.ReleaseYear), FoundBody("1850", "167 min", "8.0", "tt0069293") },
            { nameof(MovieMetadata.Runtime), FoundBody("1972", "0 min", "8.0", "tt0069293") },
            { nameof(MovieMetadata.ImdbRating), FoundBody("1972", "167 min", "12.5", "tt0069293") },
            { nameof(MovieMetadata.ImdbId), FoundBody("1972", "167 min", "8.0", "not-an-id") },
        };

    public static TheoryData<string> UnusableTitles =>
        new() { "", "N/A" };

    public static TheoryData<int> NonRetryableStatuses => new() { 400, 403, 404 };

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_FullBody_MapsEveryField()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Blade Runner", TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Blade Runner");
        metadata.Synopsis.ShouldBe("A blade runner must pursue and terminate four replicants.");
        metadata.ReleaseYear.ShouldNotBeNull().Value.ShouldBe(1982);
        metadata.Runtime.ShouldNotBeNull().Minutes.ShouldBe(117);
        metadata.ImdbRating.ShouldNotBeNull().Value.ShouldBe(8.1m);
        metadata.ImdbId.ShouldNotBeNull().Value.ShouldBe("tt0083658");
    }

    [Fact]
    public async Task FindAsync_NotAvailableOrAbsentOptionalFields_LeavesThemNull()
    {
        // arrange
        StubBody(200, SparseBody);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Solaris");
        NullFields(metadata).ShouldBe(
        [
            nameof(MovieMetadata.Synopsis),
            nameof(MovieMetadata.ReleaseYear),
            nameof(MovieMetadata.Runtime),
            nameof(MovieMetadata.ImdbRating),
            nameof(MovieMetadata.ImdbId),
        ]);
    }

    [Theory]
    [MemberData(nameof(DomainInvalidBodies))]
    public async Task FindAsync_DomainInvalidOptionalValue_NullsOnlyThatField(string memberName, string body)
    {
        // arrange
        StubBody(200, body);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        NullFields(Metadata(result)).ShouldBe([memberName]);
    }

    [Fact]
    public async Task FindAsync_RangeYear_TakesTheLeadingFourDigits()
    {
        // arrange
        StubBody(200, FoundBody("2010–2014", "108 min", "7.9", "tt1716849"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Social Network", TestContext.Current.CancellationToken);

        // assert
        Metadata(result).ReleaseYear.ShouldNotBeNull().Value.ShouldBe(2010);
    }

    [Fact]
    public async Task FindAsync_UnderPtBrCulture_ParsesTheRatingInvariantly()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();
        var original = CultureInfo.CurrentCulture;

        // act
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var result = await FindAsync(services, "Blade Runner", TestContext.Current.CancellationToken);

            // assert
            Metadata(result).ImdbRating.ShouldNotBeNull().Value.ShouldBe(8.1m);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task FindAsync_MovieNotFoundError_ReturnsNotFound()
    {
        // arrange
        StubBody(200, NotFoundBody);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Nonexistent", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_OtherProviderError_ReturnsFailedInvalidResponse()
    {
        // arrange
        StubBody(200, OtherErrorBody);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MissingResponseFlag_ReturnsFailedInvalidResponse()
    {
        // arrange
        StubBody(200, """{ "Title": "Solaris" }""");
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Theory]
    [MemberData(nameof(UnusableTitles))]
    public async Task FindAsync_UnusableTitle_ReturnsFailedInvalidResponse(string title)
    {
        // arrange
        StubBody(200, title.Length == 0 ? FoundBodyRaw("") : FoundBodyRaw("N/A"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MissingTitle_ReturnsFailedInvalidResponse()
    {
        // arrange
        StubBody(200, """{ "Response": "True", "Year": "1972" }""");
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MalformedJson_ReturnsFailedInvalidResponse()
    {
        // arrange
        StubBody(200, "{ \"Response\": \"True\", ");
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_DtoIncompatibleJson_ReturnsFailedInvalidResponse()
    {
        // arrange
        StubBody(200, """{ "Response": "True", "Title": "Solaris", "Year": 1972 }""");
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_Unauthorized_ReturnsFailedInvalidResponseAfterOneRequestWithAdviceLog()
    {
        // arrange
        StubBody(401, string.Empty);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestCount.ShouldBe(1);
        probe.Logs.Entries.ShouldContain(entry => entry.Message.Contains(
            "Check the OMDb API key configuration.",
            StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(NonRetryableStatuses))]
    public async Task FindAsync_NonRetryableStatus_ReturnsFailedInvalidResponseAfterOneRequest(int statusCode)
    {
        // arrange
        StubBody(statusCode, string.Empty);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var token = cancelled.Token;

        // act
        var act = () => FindAsync(services, "Solaris", token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task FindAsync_AnyLookup_SendsTitleTypeAndKey()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "Blade Runner", TestContext.Current.CancellationToken);

        // assert
        probe.QueryValues("t").ShouldBe(["Blade Runner"]);
        probe.QueryValues("type").ShouldBe(["movie"]);
        probe.QueryValues("apikey").ShouldBe([MetadataProviderProbe.SentinelApiKey]);
        probe.HasQuery("y").ShouldBeFalse();
    }

    [Fact]
    public async Task FindAsync_WithReleaseYear_AlsoSendsTheYear()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "Blade Runner", 1982, TestContext.Current.CancellationToken);

        // assert
        probe.QueryValues("y").ShouldBe(["1982"]);
    }

    [Fact]
    public async Task FindAsync_ReservedAndNonAsciiTitle_ArrivesEscaped()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "Tom & Jerry's #1 — Érase", TestContext.Current.CancellationToken);

        // assert
        probe.QueryValues("t").ShouldBe(["Tom & Jerry's #1 — Érase"]);
    }

    [Fact]
    public async Task FindAsync_AcrossOutcomes_NeverLogsTheApiKey()
    {
        // arrange
        var services = probe.BuildServices();

        // act
        await ExerciseEveryOutcomeAsync(services);

        // assert
        var corpus = string.Join(
            "\n",
            probe.Logs.Entries.SelectMany(entry => new[]
            {
                entry.Message,
                entry.ExceptionText,
                string.Join("\n", entry.State.Select(pair => $"{pair.Key}={pair.Value}")),
            }));
        corpus.ShouldNotContain(MetadataProviderProbe.SentinelApiKey);
    }

    [Fact]
    public async Task FindAsync_AcrossOutcomes_NeverTagsAnActivityWithTheApiKey()
    {
        // arrange
        var services = probe.BuildServices();

        // act
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName, "System.Net.Http");
        await ExerciseEveryOutcomeAsync(services);

        // assert
        var corpus = string.Join("\n", capture.Activities.Select(Describe));
        corpus.ShouldNotContain(MetadataProviderProbe.SentinelApiKey);
    }

    [Fact]
    public async Task FindAsync_UnderAmbientActivity_OpensExactlyOneMetadataLookupSpanWithoutOutcomeTags()
    {
        // arrange
        StubBody(200, FullBody);
        var services = probe.BuildServices();

        // act
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName);
        using var ambient = new Activity("test.ambient").Start();
        await FindAsync(services, "Blade Runner", TestContext.Current.CancellationToken);

        // assert
        var spans = LookupSpans(capture, ambient.TraceId);
        spans.Count.ShouldBe(1);
        spans[0].ParentId.ShouldBe(ambient.Id);
        spans[0].GetTagItem(TelemetryConstants.ErrorType).ShouldBeNull();
        spans[0].Status.ShouldBe(ActivityStatusCode.Unset);
        spans[0].Tags.Any(tag => tag.Key.Contains("status", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    }

    [Fact]
    public async Task FindAsync_WhenFailed_TagsTheSpanWithTheCategoryCode()
    {
        // arrange
        StubBody(500, string.Empty);
        var services = probe.BuildServices();

        // act
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName);
        using var ambient = new Activity("test.ambient").Start();
        await FindAsync(services, "Solaris", TestContext.Current.CancellationToken);

        // assert
        var spans = LookupSpans(capture, ambient.TraceId);
        spans.Count.ShouldBe(1);
        spans[0].GetTagItem(TelemetryConstants.ErrorType)
            .ShouldBe(EnrichmentFailureCategory.ProviderUnavailable.Code);
        spans[0].Status.ShouldBe(ActivityStatusCode.Error);
    }

    private async Task ExerciseEveryOutcomeAsync(IServiceProvider services)
    {
        var ct = TestContext.Current.CancellationToken;
        StubBody(401, string.Empty);
        await FindAsync(services, "Solaris", ct);

        probe.Reset();
        StubBody(500, string.Empty);
        await FindAsync(services, "Solaris", ct);

        probe.Reset();
        using var slow = WireMockServer.Start();
        slow.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)).WithBody(FullBody));
        var slowServices = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = slow.Urls[0],
        });
        await FindAsync(slowServices, "Solaris", ct);

        probe.Reset();
        StubBody(200, FullBody);
        await FindAsync(services, "Solaris", ct);
    }

    private static async Task<MetadataLookupResult> FindAsync(IServiceProvider services, string title, CancellationToken ct) =>
        await FindAsync(services, title, year: null, ct);

    private static async Task<MetadataLookupResult> FindAsync(
        IServiceProvider services,
        string title,
        int? year,
        CancellationToken ct)
    {
        var releaseYear = year is null ? null : new ReleaseYear(year.Value, MetadataProviderProbe.FixedNow);
        var lookup = new MetadataLookup(title, releaseYear);
        return await services.GetRequiredService<IMetadataProvider>().FindAsync(lookup, ct);
    }

    private void StubBody(int statusCode, string body) =>
        probe.Server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBody(body));

    private static string FoundBody(string year, string runtime, string rating, string imdbId) =>
        FoundBodyRaw("Solaris", year, runtime, rating, imdbId);

    private static string FoundBodyRaw(string title, string year = "1972", string runtime = "167 min", string rating = "8.0", string imdbId = "tt0069293") =>
        $$"""
          { "Response": "True", "Title": "{{title}}", "Year": "{{year}}", "Runtime": "{{runtime}}", "Plot": "A psychologist...", "imdbRating": "{{rating}}", "imdbID": "{{imdbId}}" }
          """;

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;

    private static IReadOnlyList<string> NullFields(MovieMetadata metadata) =>
    [
        .. NullField(metadata.Synopsis is null, nameof(MovieMetadata.Synopsis)),
        .. NullField(metadata.ReleaseYear is null, nameof(MovieMetadata.ReleaseYear)),
        .. NullField(metadata.Runtime is null, nameof(MovieMetadata.Runtime)),
        .. NullField(metadata.ImdbRating is null, nameof(MovieMetadata.ImdbRating)),
        .. NullField(metadata.ImdbId is null, nameof(MovieMetadata.ImdbId)),
    ];

    private static IReadOnlyList<string> NullField(bool isNull, string name) =>
        isNull ? [name] : [];

    private static IReadOnlyList<Activity> LookupSpans(ActivityCapture capture, ActivityTraceId traceId) =>
        [.. capture.Activities.Where(activity =>
            string.Equals(activity.DisplayName, TelemetryConstants.MetadataLookup, StringComparison.Ordinal)
            && activity.TraceId == traceId)];

    private static string Describe(Activity activity) =>
        string.Join(
            "\n",
            [activity.DisplayName,
             activity.OperationName,
             activity.StatusDescription,
             .. activity.Tags.Select(tag => $"{tag.Key}={tag.Value}")]);

    private sealed class ActivityCapture : IDisposable
    {
        private readonly ConcurrentBag<Activity> activities = [];
        private readonly ActivityListener listener;

        public ActivityCapture(params string[] sourceNames)
        {
            listener = new ActivityListener
            {
                ShouldListenTo = source => Array.IndexOf(sourceNames, source.Name) >= 0,
                Sample = AlwaysSample,
                ActivityStopped = activity => activities.Add(activity),
            };
            ActivitySource.AddActivityListener(listener);
        }

        private static ActivitySamplingResult AlwaysSample(ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllData;

        public IReadOnlyList<Activity> Activities
        {
            get
            {
                lock (activities)
                {
                    return [.. activities];
                }
            }
        }

        public void Dispose() => listener.Dispose();
    }
}
