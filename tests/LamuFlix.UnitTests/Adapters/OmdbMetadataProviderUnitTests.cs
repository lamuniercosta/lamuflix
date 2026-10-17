using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.UnitTests.Features;
using LamuFlix.UnitTests.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.UnitTests.Adapters;

public sealed class OmdbMetadataProviderUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    private const string RichBody = """
        {
          "Response": "True",
          "Title": "Blade Runner",
          "Year": "1982",
          "Runtime": "117 min",
          "Plot": "Full plot.",
          "Genre": "Sci-Fi, Thriller",
          "Director": "Ridley Scott",
          "Writer": "Hampton Fancher, David Webb Peoples",
          "Actors": "Harrison Ford, Rutger Hauer, Sean Young",
          "Metascore": "89",
          "imdbRating": "8.1",
          "imdbID": "tt0083658",
          "Poster": "https://example.test/blade-runner.jpg"
        }
        """;

    [Fact]
    public async Task FindAsync_RichBody_MapsGenresCastCrewMetascoreAndFullPlotQuery()
    {
        // arrange
        var handler = new StubHandler(HttpStatusCode.OK, RichBody);
        var provider = Provider(handler);
        var lookup = new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now));

        // act
        var result = await provider.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Genres.ShouldBe(["Sci-Fi", "Thriller"]);
        metadata.Writers.ShouldBe(["Hampton Fancher", "David Webb Peoples"]);
        metadata.Directors.ShouldBe(["Ridley Scott"]);
        metadata.Actors.ShouldBe(["Harrison Ford", "Rutger Hauer", "Sean Young"]);
        metadata.MetaScore.ShouldBe((short)89);
        metadata.Synopsis.ShouldBe("Full plot.");
        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.Query.ShouldContain("plot=full");
        handler.RequestUri.Query.ShouldContain("y=1982");
    }

    [Fact]
    public async Task FindAsync_LookupWithoutYear_OmitsYearQueryButKeepsPlotFull()
    {
        // arrange
        var handler = new StubHandler(HttpStatusCode.OK, RichBody);
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.Query.ShouldBe("?apikey=key&t=Blade%20Runner&type=movie&plot=full");
    }

    [Fact]
    public async Task FindAsync_NaCreditsAndBadMetascore_YieldsEmptyCollectionsAndNullScore()
    {
        // arrange
        var body = """
            { "Response": "True", "Title": "Solaris", "Genre": "N/A", "Writer": "N/A", "Director": "", "Actors": "  ", "Metascore": "N/A" }
            """;
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Genres.ShouldBeEmpty();
        metadata.Writers.ShouldBeEmpty();
        metadata.Directors.ShouldBeEmpty();
        metadata.Actors.ShouldBeEmpty();
        metadata.MetaScore.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_DuplicateAndPaddedCredits_DedupesAndTrimsKeepingFirstCasing()
    {
        // arrange
        var body = """
            { "Response": "True", "Title": "Dune", "Genre": "Sci-Fi, sci-fi ,  Adventure ", "Writer": "Frank Herbert, FRANK HERBERT", "Actors": "Timothee, Timothee ", "Metascore": "150" }
            """;
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Dune", null), TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Genres.ShouldBe(["Sci-Fi", "Adventure"]);
        metadata.Writers.ShouldBe(["Frank Herbert"]);
        metadata.Actors.ShouldBe(["Timothee"]);
        metadata.MetaScore.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_MovieNotFound_ReturnsNotFound()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "False", "Error": "Movie not found!" }"""));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Missing", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_OtherErrorBody_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "False", "Error": "Invalid API key!" }"""));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MissingTitle_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "True", "Title": "N/A" }"""));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MalformedJson_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, "not json"));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_Unauthorized_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.Unauthorized, string.Empty));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_RateLimited_ReturnsRateLimited()
    {
        // arrange
        var provider = Provider(new StubHandler((HttpStatusCode)429, string.Empty));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.RateLimited);
    }

    [Fact]
    public async Task FindAsync_ServerError_ReturnsProviderUnavailable()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.InternalServerError, string.Empty), logger);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        logger.Messages.ShouldContain(message => message.Contains("500", StringComparison.Ordinal)
            && message.Contains("failure .", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_NotFoundStatus_ReturnsInvalidResponse()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.NotFound, string.Empty), logger);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        logger.Messages.ShouldContain(message => message.Contains("404", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_NullLookup_ThrowsArgumentNullException()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, RichBody));

        // act
        var failure = await Should.ThrowAsync<ArgumentNullException>(
            () => provider.FindAsync(null!, TestContext.Current.CancellationToken));

        // assert
        failure.ParamName.ShouldBe("lookup");
    }

    [Fact]
    public async Task FindAsync_RichBody_MapsScalarsAndQueryShape()
    {
        // arrange
        var handler = new StubHandler(HttpStatusCode.OK, RichBody);
        var provider = Provider(handler);
        var lookup = new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now));

        // act
        var result = await provider.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Title.ShouldBe("Blade Runner");
        metadata.Synopsis.ShouldBe("Full plot.");
        metadata.ReleaseYear.ShouldNotBeNull().Value.ShouldBe(1982);
        metadata.Runtime.ShouldNotBeNull().Minutes.ShouldBe(117);
        metadata.ImdbRating.ShouldNotBeNull().Value.ShouldBe(8.1m);
        metadata.PosterUrl.ShouldBe("https://example.test/blade-runner.jpg");
        handler.RequestUri.ShouldNotBeNull();
        var query = handler.RequestUri.Query;
        query.ShouldContain("apikey=key");
        query.ShouldContain("t=Blade%20Runner");
        query.ShouldContain("type=movie");
        query.ShouldContain("plot=full");
        query.ShouldContain("y=1982");
    }

    [Fact]
    public async Task FindAsync_PlotNa_ReturnsNullSynopsis()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Plot": "N/A" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.Synopsis.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_PosterNa_ReturnsNullPoster()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Poster": "N/A" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.PosterUrl.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_PosterBlank_ReturnsNullPoster()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Poster": "   " }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.PosterUrl.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_YearNonNumeric_ReturnsNullReleaseYear()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Year": "abcd" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.ReleaseYear.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_YearEmpty_ReturnsNullReleaseYear()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Year": "" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.ReleaseYear.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_YearMissing_ReturnsNullReleaseYear()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.ReleaseYear.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_RuntimeMissing_ReturnsNullRuntime()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.Runtime.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_YearFiveDigits_ParsesFirstFour()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Year": "19822" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.ReleaseYear.ShouldNotBeNull().Value.ShouldBe(1982);
    }

    [Fact]
    public async Task FindAsync_RuntimeNonNumeric_ReturnsNullRuntime()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Runtime": "long" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.Runtime.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_RatingNonNumeric_ReturnsNullRating()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "imdbRating": "good" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.ImdbRating.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_MetaScoreZero_IsValid()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Metascore": "0" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.MetaScore.ShouldBe((short)0);
    }

    [Fact]
    public async Task FindAsync_MetaScoreHundred_IsValid()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Metascore": "100" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.MetaScore.ShouldBe((short)100);
    }

    [Fact]
    public async Task FindAsync_MetaScoreNegative_ReturnsNull()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Metascore": "-1" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.MetaScore.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_MetaScoreAboveHundred_ReturnsNull()
    {
        // arrange
        var body = """{ "Response": "True", "Title": "Solaris", "Metascore": "101" }""";
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.MetaScore.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_TimeoutException_ReturnsProviderUnavailable()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new ThrowingHandler(new OperationCanceledException("timeout")), logger);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        logger.Messages.ShouldContain(message => message.Contains("none", StringComparison.Ordinal)
            && message.Contains("failure .", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_CancelledTokenWithFaultedHandler_ThrowsOperationCanceled()
    {
        // arrange
        var provider = Provider(new ThrowingHandler(new OperationCanceledException("cancelled")));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.FindAsync(new MetadataLookup("Blade Runner", null), cancelled.Token));
    }

    [Fact]
    public async Task FindAsync_HttpRequestException_ReturnsProviderUnavailable()
    {
        // arrange
        var provider = Provider(new ThrowingHandler(new HttpRequestException("boom")));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_UnexpectedException_Propagates()
    {
        // arrange
        var provider = Provider(new ThrowingHandler(new InvalidOperationException("boom")));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_MalformedJson_LogsMalformedBody()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, "not json"), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains("malformed body", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_NullBody_LogsMalformedBody()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, "null"), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains("malformed body", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_MissingTitle_LogsMissingTitle()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "True", "Title": "N/A" }"""), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains("missing or unusable title", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_OtherErrorBody_LogsUnusableBody()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "False", "Error": "Invalid API key!" }"""), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains("unusable body", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_ServerError_LogsNumericStatusCode()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.InternalServerError, string.Empty), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains("500", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_RateLimited_LogsNumericStatusCode()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler((HttpStatusCode)429, string.Empty), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains("429", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_HttpRequestException_LogsExceptionTypeName()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new ThrowingHandler(new HttpRequestException("boom")), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.ShouldContain(message => message.Contains(nameof(HttpRequestException), StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_Unauthorized_LogsInvalidApiKey()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = Provider(new StubHandler(HttpStatusCode.Unauthorized, string.Empty), logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        logger.Messages.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task FindAsync_Found_RecordsHealthyState()
    {
        // arrange
        var state = new MetadataProviderHealthState();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, RichBody), state);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        Health(state).ShouldBe((null, false));
    }

    [Fact]
    public async Task FindAsync_NotFound_RecordsHealthyState()
    {
        // arrange
        var state = new MetadataProviderHealthState();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "False", "Error": "Movie not found!" }"""), state);

        // act
        await provider.FindAsync(new MetadataLookup("Missing", null), TestContext.Current.CancellationToken);

        // assert
        Health(state).ShouldBe((null, false));
    }

    [Fact]
    public async Task FindAsync_InvalidResponse_RecordsFailureCategory()
    {
        // arrange
        var state = new MetadataProviderHealthState();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, "not json"), state);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        Health(state).ShouldBe((EnrichmentFailureCategory.InvalidResponse, false));
    }

    [Fact]
    public async Task FindAsync_Unauthorized_RecordsUnauthorizedState()
    {
        // arrange
        var state = new MetadataProviderHealthState();
        var provider = Provider(new StubHandler(HttpStatusCode.Unauthorized, string.Empty), state);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        Health(state).ShouldBe((EnrichmentFailureCategory.InvalidResponse, true));
    }

    [Fact]
    public async Task FindAsync_FailedResult_SetsErrorActivityStatus()
    {
        // arrange
        string? status = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "LamuFlix",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        listener.ActivityStopped += activity => status = activity.Status.ToString();
        var provider = Provider(new StubHandler(HttpStatusCode.OK, "not json"));

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        status.ShouldBe(ActivityStatusCode.Error.ToString());
    }

    private static OmdbMetadataProvider Provider(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/" });
        return new OmdbMetadataProvider(http, options, new FixedTimeProvider(Now), NullLogger<OmdbMetadataProvider>.Instance, new MetadataProviderHealthState());
    }

    private static OmdbMetadataProvider Provider(StubHandler handler, CapturingLogger logger)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/" });
        return new OmdbMetadataProvider(http, options, new FixedTimeProvider(Now), logger, new MetadataProviderHealthState());
    }

    private static OmdbMetadataProvider Provider(StubHandler handler, MetadataProviderHealthState state)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/" });
        return new OmdbMetadataProvider(http, options, new FixedTimeProvider(Now), NullLogger<OmdbMetadataProvider>.Instance, state);
    }

    private static OmdbMetadataProvider Provider(ThrowingHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/" });
        return new OmdbMetadataProvider(http, options, new FixedTimeProvider(Now), NullLogger<OmdbMetadataProvider>.Instance, new MetadataProviderHealthState());
    }

    private static OmdbMetadataProvider Provider(ThrowingHandler handler, CapturingLogger logger)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/" });
        return new OmdbMetadataProvider(http, options, new FixedTimeProvider(Now), logger, new MetadataProviderHealthState());
    }

    private static (EnrichmentFailureCategory? Category, bool WasUnauthorized) Health(MetadataProviderHealthState state)
    {
        var current = typeof(MetadataProviderHealthState).GetProperty(
            "Current",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(state);
        current.ShouldNotBeNull();
        var category = (EnrichmentFailureCategory?)current.GetType().GetProperty("Category")?.GetValue(current);
        var wasUnauthorized = (bool)(current.GetType().GetProperty("WasUnauthorized")?.GetValue(current) ?? throw new InvalidOperationException("WasUnauthorized missing."));
        return (category, wasUnauthorized);
    }

    private sealed class ThrowingHandler(Exception failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromException<HttpResponseMessage>(failure);
    }

    private sealed class CapturingLogger : ILogger<OmdbMetadataProvider>
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<string> messages = [];

        public System.Collections.Generic.IReadOnlyCollection<string> Messages => messages;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    [Collection(nameof(MetricsCollection))]
    public sealed class DurationMetricTests
    {
        [Fact]
        public async Task FindAsync_Success_RecordsDurationWithOmdbProvider()
        {
            // arrange
            using var capture = new DurationCapture();
            var provider = Provider(new StubHandler(HttpStatusCode.OK, RichBody));

            // act
            await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

            // assert
            var measured = capture.Measurements.ShouldHaveSingleItem();
            measured.Length.ShouldBe(1);
            measured[0].Key.ShouldBe(TelemetryConstants.ProviderTagName);
            measured[0].Value.ShouldBe(TelemetryConstants.OmdbProviderName);
        }

        [Fact]
        public async Task FindAsync_Failure_RecordsDurationWithOmdbProvider()
        {
            // arrange
            using var capture = new DurationCapture();
            var provider = Provider(new StubHandler(HttpStatusCode.OK, "not json"));

            // act
            await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

            // assert
            var measured = capture.Measurements.ShouldHaveSingleItem();
            measured.Length.ShouldBe(1);
            measured[0].Key.ShouldBe(TelemetryConstants.ProviderTagName);
            measured[0].Value.ShouldBe(TelemetryConstants.OmdbProviderName);
        }

        private sealed class DurationCapture : IDisposable
        {
            private readonly ConcurrentQueue<KeyValuePair<string, object?>[]> measurements = [];
            private readonly MeterListener listener = new();

            public DurationCapture()
            {
                listener.InstrumentPublished = (instrument, meterListener) =>
                {
                    if (string.Equals(instrument.Meter.Name, TelemetryConstants.ActivitySourceName, StringComparison.Ordinal)
                        && string.Equals(instrument.Name, TelemetryConstants.EnrichmentDurationMetricName, StringComparison.Ordinal))
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>(
                    (_, _, tags, _) => measurements.Enqueue(tags.ToArray()));
                listener.Start();
            }

            public IReadOnlyList<KeyValuePair<string, object?>[]> Measurements => [.. measurements];

            public void Dispose() => listener.Dispose();
        }
    }
}
