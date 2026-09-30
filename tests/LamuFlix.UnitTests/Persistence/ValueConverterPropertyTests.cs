using System;
using System.Globalization;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence.Converters;
using LamuFlix.UnitTests.Features;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.UnitTests.Persistence;

public sealed class ValueConverterPropertyTests
{
    private static readonly TimeProvider Clock = new FixedTimeProvider(
        new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    [Trait("Category", "Property")]
    public void MovieId_ValidValues_RoundTripProviderToModelToProvider() =>
        RoundTrip(new MovieIdConverter(), Gen.Choose(1, 10_000));

    [Fact]
    [Trait("Category", "Property")]
    public void LibraryPath_ValidValues_RoundTripProviderToModelToProvider()
    {
        var paths = from folder in Gen.Elements("library", "media", "incoming")
                    from file in Gen.Elements("movie", "film", "title")
                    select folder + "/" + file;
        RoundTrip(new LibraryPathConverter(), paths);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void MediaFormat_ValidValues_RoundTripProviderToModelToProvider() =>
        RoundTrip(new MediaFormatConverter(), Gen.Elements("mkv", "mp4", "avi", "mov"));

    [Fact]
    [Trait("Category", "Property")]
    public void ImdbId_ValidValues_RoundTripProviderToModelToProvider()
    {
        var ids = from width in Gen.Elements(7, 8)
                  from n in Gen.Choose(0, 9_999_999)
                  select "tt" + n.ToString("D" + width, CultureInfo.InvariantCulture);
        RoundTrip(new ImdbIdConverter(), ids);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ImdbRating_ValidValues_RoundTripProviderToModelToProvider()
    {
        var ratings = Gen.Choose(0, 100).Select(tenths => tenths / 10m);
        RoundTrip(new ImdbRatingConverter(), ratings);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Runtime_ValidValues_RoundTripProviderToModelToProvider() =>
        RoundTrip(new RuntimeConverter(), Gen.Choose(1, 600));

    [Fact]
    [Trait("Category", "Property")]
    public void ReleaseYear_ValidValues_RoundTripProviderToModelToProvider() =>
        RoundTrip(new ReleaseYearConverter(Clock), Gen.Choose(1888, 2031));

    [Fact]
    [Trait("Category", "Property")]
    public void EnrichmentStatus_Mapping_IsBijectionOnZeroThroughThree()
    {
        var converter = new EnrichmentStatusConverter();
        Prop.ForAll(
                Arb.From(Gen.Choose(0, 3)),
                value =>
                {
                    var model = converter.ConvertFromProvider(value).ShouldBeOfType<EnrichmentStatus>();
                    converter.ConvertToProvider(model).ShouldBe(value);
                    converter.ConvertFromProvider(model.Value).ShouldBe(model);
                })
            .QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void EnrichmentFailureCategory_KnownCodes_RoundTripProviderToModelToProvider()
    {
        var codes = Gen.Elements(
            EnrichmentFailureCategory.ProviderUnavailable.Code,
            EnrichmentFailureCategory.RateLimited.Code,
            EnrichmentFailureCategory.InvalidResponse.Code,
            EnrichmentFailureCategory.Unknown.Code);
        RoundTrip(new EnrichmentFailureCategoryConverter(), codes);
    }

    private static void RoundTrip<TProvider>(ValueConverter converter, Gen<TProvider> providers)
    {
        Prop.ForAll(
                Arb.From(providers),
                value =>
                {
                    var model = converter.ConvertFromProvider(value);
                    model.ShouldNotBeNull();
                    converter.ConvertToProvider(model).ShouldBe(value);
                })
            .QuickCheckThrowOnFailure();
    }
}
