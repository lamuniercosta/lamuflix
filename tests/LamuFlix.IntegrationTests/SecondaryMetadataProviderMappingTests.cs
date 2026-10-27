using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class SecondaryMetadataProviderMappingTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string PosterBase = "https://image.tmdb.org/t/p/w500";

    public static TheoryData<string, string> OptionalAbsentFixtures =>
        new()
        {
            { "optional-fields-absent.json", "Fight Club" },
            { "optional-fields-null.json", "Fight Club" },
        };

    public static TheoryData<string> AdversarialPosterFixtures =>
        new()
        {
            "scheme.json",
            "host.json",
            "backslash.json",
            "control.json",
            "dot-segment.json",
            "dotdot-segment.json",
            "double-leading-slash.json",
            "missing-leading-slash.json",
        };

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_RecordedDetailsFixture_MapsCoreFieldsAndLeavesRatingsAndCreditsEmpty()
    {
        // arrange
        probe.StubFoundLookup();
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("The Matrix");
        metadata.ReleaseYear.ShouldNotBeNull().Value.ShouldBe(1999);
        metadata.Runtime.ShouldNotBeNull().Minutes.ShouldBe(136);
        metadata.Synopsis.ShouldNotBeNullOrWhiteSpace();
        metadata.PosterUrl.ShouldBe(string.Concat(PosterBase, "/f89U3ADr1oiB1s9GkdPOEpXUk5H.jpg"));
        metadata.ImdbId.ShouldNotBeNull().Value.ShouldBe("tt0133093");
        metadata.ImdbRating.ShouldBeNull();
        metadata.RottenTomatoesRating.ShouldBeNull();
        metadata.MetaScore.ShouldBeNull();
        metadata.Directors.ShouldBeEmpty();
        metadata.Actors.ShouldBeEmpty();
        metadata.Genres.ShouldBeEmpty();
        metadata.Writers.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(OptionalAbsentFixtures))]
    public async Task FindAsync_AbsentOrNullOptionalFields_StaysFoundWithThoseFieldsAbsent(
        string fixtureName,
        string expectedTitle)
    {
        // arrange
        probe.StubFoundLookup(detailsBody: SecondaryMetadataProviderProbe.Fixture("Details", fixtureName));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe(expectedTitle);
        metadata.Synopsis.ShouldBeNull();
        metadata.ReleaseYear.ShouldBeNull();
        metadata.Runtime.ShouldBeNull();
        metadata.PosterUrl.ShouldBeNull();
        metadata.ImdbId.ShouldBeNull();
        metadata.ImdbRating.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_DomainInvalidValues_NullsOnlyTheInvalidFields()
    {
        // arrange
        probe.StubFoundLookup(detailsBody: SecondaryMetadataProviderProbe.Fixture("Details", "invalid-values.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("The Matrix");
        metadata.ReleaseYear.ShouldBeNull();
        metadata.Runtime.ShouldBeNull();
        metadata.Synopsis.ShouldBeNull();
        metadata.ImdbId.ShouldBeNull();
        metadata.PosterUrl.ShouldBe(string.Concat(PosterBase, "/f89U3ADr1oiB1s9GkdPOEpXUk5H.jpg"));
    }

    [Fact]
    public async Task FindAsync_BlankDetailsTitle_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubFoundLookup(detailsBody: SecondaryMetadataProviderProbe.Fixture("Details", "blank-title.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Theory]
    [InlineData("malformed.json")]
    [InlineData("wrong-shape.json")]
    public async Task FindAsync_MalformedOrWrongShapedDetailsBody_ReturnsFailedInvalidResponse(string fixtureName)
    {
        // arrange
        probe.StubFoundLookup(detailsBody: SecondaryMetadataProviderProbe.Fixture("Details", fixtureName));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MalformedSearchBody_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "malformed.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_WrongShapedFindBody_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubFind("tt0133093", SecondaryMetadataProviderProbe.Fixture("Find", "wrong-shape.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Theory]
    [MemberData(nameof(AdversarialPosterFixtures))]
    public async Task FindAsync_AdversarialPosterPath_LeavesThePosterAbsentWithoutThrowing(string fixtureName)
    {
        // arrange
        probe.StubFoundLookup(detailsBody: SecondaryMetadataProviderProbe.Fixture("Poster", fixtureName));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("The Matrix");
        metadata.PosterUrl.ShouldBeNull();
    }

    private static async Task<MetadataLookupResult> FindAsync(
        IServiceProvider services,
        string title,
        CancellationToken ct) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup(title, null), ct);

    private static async Task<MetadataLookupResult> FindByImdbAsync(
        IServiceProvider services,
        string imdbId,
        CancellationToken ct) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("ignored title", null, new ImdbId(imdbId)), ct);

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;
}
