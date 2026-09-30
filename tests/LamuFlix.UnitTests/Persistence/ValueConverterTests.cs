using System;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence.Converters;
using LamuFlix.UnitTests.Features;

namespace LamuFlix.UnitTests.Persistence;

public sealed class ValueConverterTests
{
    private static readonly TimeProvider Clock = new FixedTimeProvider(
        new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void MovieId_ValidValue_RoundTrips()
    {
        var converter = new MovieIdConverter();

        var model = converter.ConvertFromProvider(42).ShouldBeOfType<MovieId>();

        model.Value.ShouldBe(42);
        converter.ConvertToProvider(model).ShouldBe(42);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MovieId_InvalidNonNull_Throws(int value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new MovieIdConverter(), value);

    [Fact]
    public void MovieId_Null_IsPreserved() => ConverterAssert.NullIsPreserved(new MovieIdConverter());

    [Fact]
    public void MovieId_ConvertToProvider_MapsNullAndValueExplicitly()
    {
        var converter = new MovieIdConverter();

        converter.ConvertToProvider(null).ShouldBeNull();
        converter.ConvertToProvider(new MovieId(5)).ShouldBe(5);
    }

    [Fact]
    public void LibraryPath_ValidValue_RoundTrips()
    {
        var converter = new LibraryPathConverter();
        const string path = "C:/library/incoming/movie.mkv";

        var model = converter.ConvertFromProvider(path).ShouldBeOfType<LibraryPath>();

        model.Value.ShouldBe(path);
        converter.ConvertToProvider(model).ShouldBe(path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foo/../bar")]
    public void LibraryPath_InvalidNonNull_Throws(string value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new LibraryPathConverter(), value);

    [Fact]
    public void LibraryPath_Null_IsPreserved() => ConverterAssert.NullIsPreserved(new LibraryPathConverter());

    [Fact]
    public void MediaFormat_ValidValue_StoresExtension()
    {
        var converter = new MediaFormatConverter();

        var model = converter.ConvertFromProvider("mkv").ShouldBeOfType<MediaFormat>();

        model.Extension.ShouldBe("mkv");
        converter.ConvertToProvider(model).ShouldBe("mkv");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    public void MediaFormat_InvalidNonNull_Throws(string value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new MediaFormatConverter(), value);

    [Fact]
    public void MediaFormat_Null_IsPreserved() => ConverterAssert.NullIsPreserved(new MediaFormatConverter());

    [Fact]
    public void ImdbId_ValidValue_RoundTrips()
    {
        var converter = new ImdbIdConverter();
        const string id = "tt1234567";

        var model = converter.ConvertFromProvider(id).ShouldBeOfType<ImdbId>();

        model.Value.ShouldBe(id);
        converter.ConvertToProvider(model).ShouldBe(id);
    }

    [Theory]
    [InlineData("tt123")]
    [InlineData("TT1234567")]
    [InlineData("not-an-id")]
    public void ImdbId_InvalidNonNull_Throws(string value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new ImdbIdConverter(), value);

    [Fact]
    public void ImdbId_Null_IsPreserved() => ConverterAssert.NullIsPreserved(new ImdbIdConverter());

    [Fact]
    public void ImdbRating_ValidValue_RoundTrips()
    {
        var converter = new ImdbRatingConverter();

        var model = converter.ConvertFromProvider(8.5m).ShouldBeOfType<ImdbRating>();

        model.Value.ShouldBe(8.5m);
        converter.ConvertToProvider(model).ShouldBe(8.5m);
    }

    [Theory]
    [InlineData(10.1)]
    [InlineData(-0.1)]
    [InlineData(1.23)]
    public void ImdbRating_InvalidNonNull_Throws(double value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new ImdbRatingConverter(), (decimal)value);

    [Fact]
    public void ImdbRating_Null_IsPreserved() => ConverterAssert.NullIsPreserved(new ImdbRatingConverter());

    [Fact]
    public void Runtime_ValidValue_RoundTrips()
    {
        var converter = new RuntimeConverter();

        var model = converter.ConvertFromProvider(142).ShouldBeOfType<Runtime>();

        model.Minutes.ShouldBe(142);
        converter.ConvertToProvider(model).ShouldBe(142);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-8)]
    public void Runtime_InvalidNonNull_Throws(int value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new RuntimeConverter(), value);

    [Fact]
    public void Runtime_Null_IsPreserved() => ConverterAssert.NullIsPreserved(new RuntimeConverter());

    [Fact]
    public void ReleaseYear_ValidValue_RoundTrips()
    {
        var converter = new ReleaseYearConverter(Clock);

        var model = converter.ConvertFromProvider(1999).ShouldBeOfType<ReleaseYear>();

        model.Value.ShouldBe(1999);
        converter.ConvertToProvider(model).ShouldBe(1999);
    }

    [Theory]
    [InlineData(1887)]
    [InlineData(2032)]
    public void ReleaseYear_InvalidNonNull_Throws(int value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new ReleaseYearConverter(Clock), value);

    [Fact]
    public void ReleaseYear_Null_IsPreserved() =>
        ConverterAssert.NullIsPreserved(new ReleaseYearConverter(Clock));

    [Fact]
    public void ReleaseYear_NullTimeProvider_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => new ReleaseYearConverter(null!));

    [Theory]
    [InlineData(0, "Pending")]
    [InlineData(1, "Enriched")]
    [InlineData(2, "NotFound")]
    [InlineData(3, "Failed")]
    public void EnrichmentStatus_KnownValues_AreAccepted(int value, string name)
    {
        var converter = new EnrichmentStatusConverter();

        var model = converter.ConvertFromProvider(value).ShouldBeOfType<EnrichmentStatus>();

        model.Name.ShouldBe(name);
        model.Value.ShouldBe(value);
        converter.ConvertToProvider(model).ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public void EnrichmentStatus_UnknownInt_Throws(int value) =>
        ConverterAssert.InvalidNonNullProviderThrows(new EnrichmentStatusConverter(), value);

    [Fact]
    public void EnrichmentStatus_Null_IsPreserved() =>
        ConverterAssert.NullIsPreserved(new EnrichmentStatusConverter());

    [Theory]
    [InlineData("provider_unavailable")]
    [InlineData("rate_limited")]
    [InlineData("invalid_response")]
    [InlineData("unknown")]
    public void EnrichmentFailureCategory_KnownCode_ResolvesSingleton(string code)
    {
        var converter = new EnrichmentFailureCategoryConverter();

        var model = converter.ConvertFromProvider(code).ShouldBeOfType<EnrichmentFailureCategory>();

        model.Code.ShouldBe(code);
        converter.ConvertToProvider(model).ShouldBe(code);
        ReferenceEquals(model, ConvertKnown(code)).ShouldBeTrue();
    }

    [Fact]
    public void EnrichmentFailureCategory_UnknownCode_Throws()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            new EnrichmentFailureCategoryConverter().ConvertFromProvider("not_a_category"));

        exception.Message.ShouldBe("Invalid EnrichmentFailureCategory provider value.");
    }

    [Fact]
    public void InvalidMovieIdProviderValue_UsesModelNameInExceptionMessage()
    {
        var exception = Should.Throw<InvalidOperationException>(() => new MovieIdConverter().ConvertFromProvider(0));

        exception.Message.ShouldBe($"Invalid {typeof(MovieId).Name} provider value.");
    }

    [Fact]
    public void EnrichmentFailureCategory_Null_IsPreserved() =>
        ConverterAssert.NullIsPreserved(new EnrichmentFailureCategoryConverter());

    private static EnrichmentFailureCategory ConvertKnown(string code) => code switch
    {
        "provider_unavailable" => EnrichmentFailureCategory.ProviderUnavailable,
        "rate_limited" => EnrichmentFailureCategory.RateLimited,
        "invalid_response" => EnrichmentFailureCategory.InvalidResponse,
        "unknown" => EnrichmentFailureCategory.Unknown,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };
}
