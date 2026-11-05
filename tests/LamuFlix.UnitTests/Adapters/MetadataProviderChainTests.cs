using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;

namespace LamuFlix.UnitTests.Adapters;

public sealed class MetadataProviderChainTests
{
    private static MetadataLookup Query => new("Blade Runner", null, new ImdbId("tt0083658"));

    public static TheoryData<string, string, MetadataLookupResult, MetadataLookupResult> ResultMatrix()
    {
        var data = new TheoryData<string, string, MetadataLookupResult, MetadataLookupResult>();
        foreach (var primary in Outcomes("Primary"))
        {
            foreach (var secondary in Outcomes("Secondary"))
            {
                data.Add(primary.Name, secondary.Name, primary.Create(), secondary.Create());
            }
        }

        return data;
    }

    public static TheoryData<string, MetadataLookupResult> SingleProviderOutcomes()
    {
        var data = new TheoryData<string, MetadataLookupResult>();
        foreach (var outcome in Outcomes("Solo"))
        {
            data.Add(outcome.Name, outcome.Create());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ResultMatrix))]
    public async Task FindAsync_PrimaryAndSecondaryOutcomes_ReturnsDocumentedFallbackOutcome(
        string primaryOutcome,
        string secondaryOutcome,
        MetadataLookupResult primaryResult,
        MetadataLookupResult secondaryResult)
    {
        // arrange
        var primary = StubMetadataProvider.Returning(primaryResult);
        var secondary = StubMetadataProvider.Returning(secondaryResult);
        var chain = new ChainedMetadataProvider([primary, secondary]);
        var (expected, secondaryExpected) = Expected(primaryResult, secondaryResult);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(expected, $"primary {primaryOutcome}, secondary {secondaryOutcome}");
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(secondaryExpected ? 1 : 0);
    }

    [Theory]
    [MemberData(nameof(SingleProviderOutcomes))]
    public async Task FindAsync_SingleProviderChain_ReturnsProviderResultUnchanged(
        string outcome,
        MetadataLookupResult providerResult)
    {
        // arrange
        var provider = StubMetadataProvider.Returning(providerResult);
        var chain = new ChainedMetadataProvider([provider]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(providerResult, $"single provider {outcome}");
        provider.CallCount.ShouldBe(1);
    }

    [Fact]
    public void Constructor_WithNullProviders_ThrowsArgumentNullException()
    {
        // arrange
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(() => new ChainedMetadataProvider(null!));

        // assert
        exception.ParamName.ShouldBe("providers");
    }

    [Fact]
    public void Constructor_WithEmptyProviders_ThrowsArgumentException()
    {
        // arrange
        // act
        var exception = Should.Throw<ArgumentException>(() => new ChainedMetadataProvider([]));

        // assert
        exception.ParamName.ShouldBe("providers");
    }

    [Fact]
    public async Task FindAsync_CallerMutatesListAfterConstruction_KeepsSnapshotOrder()
    {
        // arrange
        var calls = new List<string>();
        var first = StubMetadataProvider.Running(() => Record(calls, "first"));
        var second = StubMetadataProvider.Running(() => Record(calls, "second"));
        var providers = new List<IMetadataProvider> { first, second };
        var chain = new ChainedMetadataProvider(providers);
        var intruder = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());

        providers.Insert(0, intruder);
        providers.Reverse();

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        calls.ShouldBe(["first", "second"]);
        intruder.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_MultipleProviders_ForwardsLookupInstanceAndCancellationTokenUnchanged()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var chain = new ChainedMetadataProvider([primary, secondary]);
        using var cancellation = new CancellationTokenSource();
        var query = Query;

        // act
        var result = await chain.FindAsync(query, cancellation.Token);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        primary.LastLookup.ShouldBeSameAs(query);
        secondary.LastLookup.ShouldBeSameAs(query);
        primary.LastLookup.ShouldNotBeNull().ManualImdbId.ShouldBe(new ImdbId("tt0083658"));
        primary.LastCancellationToken.ShouldBe(cancellation.Token);
        secondary.LastCancellationToken.ShouldBe(cancellation.Token);
    }

    [Fact]
    public async Task FindAsync_CallerCancellation_PropagatesOperationCanceledExceptionWithoutFallback()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var primary = StubMetadataProvider.Throwing(new OperationCanceledException(cancellation.Token));
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(new MovieMetadata("Secondary")));
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var exception = await Should.ThrowAsync<OperationCanceledException>(
            () => chain.FindAsync(Query, cancellation.Token));

        // assert
        exception.CancellationToken.ShouldBe(cancellation.Token);
        secondary.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_UnexpectedProviderException_PropagatesWithoutFallback()
    {
        // arrange
        var primary = StubMetadataProvider.Throwing(new InvalidOperationException("boom"));
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(new MovieMetadata("Secondary")));
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => chain.FindAsync(Query, TestContext.Current.CancellationToken));

        // assert
        exception.Message.ShouldBe("boom");
        secondary.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_PrimaryProviderUnavailableSecondaryNotFound_ReturnsEligiblePrimaryFailure()
    {
        // arrange
        var primaryFailure = new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable);
        var primary = StubMetadataProvider.Returning(primaryFailure);
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(primaryFailure);
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PrimaryEligibleFailureSecondaryTerminalFailure_ReturnsSecondaryTerminalFailure()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.RateLimited));
        var terminal = new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse);
        var secondary = StubMetadataProvider.Returning(terminal);
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(terminal);
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PrimaryQuotaExhaustedSecondaryFound_ReturnsSecondaryFound()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted));
        var secondaryResult = new MetadataLookupResult.Found(new MovieMetadata("Secondary"));
        var secondary = StubMetadataProvider.Returning(secondaryResult);
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(secondaryResult);
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PrimaryQuotaExhaustedSecondaryNotFound_ReturnsPrimaryQuotaExhausted()
    {
        // arrange
        var primaryFailure = new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted);
        var primary = StubMetadataProvider.Returning(primaryFailure);
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(primaryFailure);
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(1);
    }

    public static TheoryData<string, EnrichmentFailureCategory> NonEligibleCategories()
    {
        var data = new TheoryData<string, EnrichmentFailureCategory>();
        foreach (var category in new[]
        {
            EnrichmentFailureCategory.InvalidResponse,
            EnrichmentFailureCategory.Unknown,
            EnrichmentFailureCategory.DuplicateImdbId,
        })
        {
            data.Add(category.Code, category);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(NonEligibleCategories))]
    public async Task FindAsync_PrimaryNonEligibleFailure_DoesNotFallBack(string categoryCode, EnrichmentFailureCategory category)
    {
        // arrange
        var primaryFailure = new MetadataLookupResult.Failed(category);
        var primary = StubMetadataProvider.Returning(primaryFailure);
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(new MovieMetadata("Secondary")));
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(primaryFailure, $"noneligible category {categoryCode}");
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_BothProvidersNotFound_ReturnsNotFound()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var secondary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var chain = new ChainedMetadataProvider([primary, secondary]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        primary.CallCount.ShouldBe(1);
        secondary.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithSupplementEnabled_MergesOnlySignalsAndCollection()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadata()));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary], supplement);
        var query = Query;

        // act
        var result = await chain.FindAsync(query, TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Primary");
        metadata.Synopsis.ShouldBe("Primary plot");
        metadata.ImdbId.ShouldBe(new ImdbId("tt0133093"));
        metadata.MetaScore.ShouldBe((short)73);
        metadata.Directors.ShouldBe(["Lana Wachowski"]);
        metadata.Writers.ShouldBe(["Lilly Wachowski"]);
        metadata.Genres.ShouldBe(["Action"]);
        metadata.Actors.ShouldBe(["Base Actor"]);
        metadata.CollectionId.ShouldBe(2344);
        metadata.CollectionName.ShouldBe("The Matrix Collection");
        metadata.Signals.ShouldBe(SupplementMetadata().Signals);
        primary.CallCount.ShouldBe(1);
        supplement.CallCount.ShouldBe(1);
        supplement.LastLookup.ShouldBeSameAs(query);
        supplement.LastCancellationToken.ShouldBe(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithManualImdbIdOnly_ForwardsManualIdToSupplement()
    {
        // arrange
        var manualId = new ImdbId("tt0083658");
        var lookup = new MetadataLookup("Blade Runner", null, manualId);
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadataWithoutImdbId()));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        await chain.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var forwarded = supplement.LastLookup.ShouldNotBeNull();
        forwarded.ManualImdbId.ShouldBe(manualId);
        forwarded.Title.ShouldBe("Blade Runner");
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithOmdbImdbIdOnly_ForwardsOmdbIdToSupplement()
    {
        // arrange
        var lookup = new MetadataLookup("Blade Runner", null);
        var omdbId = new ImdbId("tt0133093");
        var primary = StubMetadataProvider.Returning(
            new MetadataLookupResult.Found(new MovieMetadata("Primary", imdbId: omdbId)));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        await chain.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var forwarded = supplement.LastLookup.ShouldNotBeNull();
        forwarded.ManualImdbId.ShouldBe(omdbId);
        forwarded.Title.ShouldBe("Blade Runner");
        forwarded.ReleaseYear.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithDistinctManualAndOmdbIds_ForwardsManualIdToSupplement()
    {
        // arrange
        var manualId = new ImdbId("tt0083658");
        var omdbId = new ImdbId("tt0133093");
        var lookup = new MetadataLookup("Blade Runner", null, manualId);
        var primary = StubMetadataProvider.Returning(
            new MetadataLookupResult.Found(new MovieMetadata("Primary", imdbId: omdbId)));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        await chain.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var forwarded = supplement.LastLookup.ShouldNotBeNull();
        forwarded.ManualImdbId.ShouldBe(manualId);
        forwarded.ManualImdbId.ShouldNotBe(omdbId);
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithoutAnyImdbId_PreservesOriginalLookupInstance()
    {
        // arrange
        var lookup = new MetadataLookup("Blade Runner", null);
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadataWithoutImdbId()));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        await chain.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        supplement.LastLookup.ShouldBeSameAs(lookup);
        supplement.LastLookup.ShouldNotBeNull().ManualImdbId.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithOmdbImdbIdAndSupplementNotFound_KeepsPrimaryFoundWithUnavailableSignals()
    {
        // arrange
        var lookup = new MetadataLookup("Blade Runner", null);
        var omdbId = new ImdbId("tt0133093");
        var primary = StubMetadataProvider.Returning(
            new MetadataLookupResult.Found(new MovieMetadata("Primary", imdbId: omdbId)));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        var result = await chain.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Primary");
        metadata.ImdbId.ShouldBe(omdbId);
        metadata.CollectionId.ShouldBeNull();
        metadata.Signals.ShouldNotBeNull().CollectionAvailability.ShouldBe(SignalAvailability.Unavailable);
        supplement.LastLookup.ShouldNotBeNull().ManualImdbId.ShouldBe(omdbId);
        supplement.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundWithSupplementDisabled_ReturnsPrimaryUnchangedWithoutASupplementCall()
    {
        // arrange
        var primaryResult = new MetadataLookupResult.Found(PrimaryMetadata());
        var primary = StubMetadataProvider.Returning(primaryResult);
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary, supplement]);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(primaryResult);
        primary.CallCount.ShouldBe(1);
        supplement.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_PrimaryFoundAndSupplementNotFound_KeepsPrimaryFoundWithUnavailableSignals()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadata()));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Primary");
        metadata.Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        metadata.Signals.FullCastAvailability.ShouldBe(SignalAvailability.Unavailable);
        metadata.Signals.CollectionAvailability.ShouldBe(SignalAvailability.Unavailable);
        supplement.CallCount.ShouldBe(1);
    }

    public static TheoryData<EnrichmentFailureCategory> SupplementalFailures =>
        [EnrichmentFailureCategory.RateLimited, EnrichmentFailureCategory.ProviderUnavailable, EnrichmentFailureCategory.InvalidResponse];

    [Theory]
    [MemberData(nameof(SupplementalFailures))]
    public async Task FindAsync_PrimaryFoundAndSupplementFailed_KeepsPrimaryFoundWithUnavailableSignals(
        EnrichmentFailureCategory category)
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadata()));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Failed(category));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Primary");
        metadata.Actors.ShouldBe(["Base Actor"]);
        metadata.Signals.ShouldNotBeNull().CollectionAvailability.ShouldBe(SignalAvailability.Unavailable);
        metadata.CollectionId.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_FallbackFoundFromTheSupplementProvider_DoesNotRepeatTheSupplementLookup()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.NotFound());
        var supplementResult = new MetadataLookupResult.Found(SupplementMetadata());
        var supplement = StubMetadataProvider.Returning(supplementResult);
        var chain = new ChainedMetadataProvider([primary, supplement], supplement);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(supplementResult);
        primary.CallCount.ShouldBe(1);
        supplement.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_PrimaryTerminalFailure_DoesNotCallTheSupplement()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse));
        var supplement = StubMetadataProvider.Returning(new MetadataLookupResult.Found(SupplementMetadata()));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        var result = await chain.FindAsync(Query, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        supplement.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_SupplementThrowsUnexpectedException_Propagates()
    {
        // arrange
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadata()));
        var supplement = StubMetadataProvider.Throwing(new InvalidOperationException("supplement boom"));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => chain.FindAsync(Query, TestContext.Current.CancellationToken));

        // assert
        exception.Message.ShouldBe("supplement boom");
    }

    [Fact]
    public async Task FindAsync_SupplementCancellation_Propagates()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var primary = StubMetadataProvider.Returning(new MetadataLookupResult.Found(PrimaryMetadata()));
        var supplement = StubMetadataProvider.Throwing(new OperationCanceledException(cancellation.Token));
        var chain = new ChainedMetadataProvider([primary], supplement);

        // act
        var exception = await Should.ThrowAsync<OperationCanceledException>(
            () => chain.FindAsync(Query, cancellation.Token));

        // assert
        exception.CancellationToken.ShouldBe(cancellation.Token);
    }

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static MovieMetadata PrimaryMetadata() =>
        new(
            "Primary",
            synopsis: "Primary plot",
            imdbId: new ImdbId("tt0133093"),
            metaScore: 73,
            directors: ["Lana Wachowski"],
            actors: ["Base Actor"],
            genres: ["Action"],
            writers: ["Lilly Wachowski"]);

    private static MovieMetadata PrimaryMetadataWithoutImdbId() =>
        new(
            "Primary",
            synopsis: "Primary plot",
            metaScore: 73,
            directors: ["Lana Wachowski"],
            actors: ["Base Actor"],
            genres: ["Action"],
            writers: ["Lilly Wachowski"]);

    private static MovieMetadata SupplementMetadata() =>
        new(
            "Supplement",
            collectionId: 2344,
            collectionName: "The Matrix Collection",
            signals: new MovieSignalSnapshot(
                SignalAvailability.Available,
                [new MovieKeyword(2001, "Dystopia")],
                SignalAvailability.Available,
                ["Keanu Reeves"],
                SignalAvailability.Available));

    private static MetadataLookupResult Record(List<string> calls, string name)
    {
        calls.Add(name);
        return new MetadataLookupResult.NotFound();
    }

    private static (MetadataLookupResult Expected, bool SecondaryCalled) Expected(
        MetadataLookupResult primary,
        MetadataLookupResult secondary)
    {
        if (IsTerminal(primary))
        {
            return (primary, false);
        }

        if (IsTerminal(secondary))
        {
            return (secondary, true);
        }

        return primary is MetadataLookupResult.Failed ? (primary, true) : (secondary, true);
    }

    private static bool IsTerminal(MetadataLookupResult result) =>
        result is MetadataLookupResult.Found
        || (result is MetadataLookupResult.Failed failed && !IsEligible(failed.Category));

    private static bool IsEligible(EnrichmentFailureCategory category) =>
        category == EnrichmentFailureCategory.RateLimited
        || category == EnrichmentFailureCategory.QuotaExhausted
        || category == EnrichmentFailureCategory.ProviderUnavailable;

    private static IReadOnlyList<(string Name, Func<MetadataLookupResult> Create)> Outcomes(string foundTitle) =>
    [
        ("Found", () => new MetadataLookupResult.Found(new MovieMetadata(foundTitle))),
        ("NotFound", () => new MetadataLookupResult.NotFound()),
        .. FailureCategories().Select(category => ($"Failed({category.Code})", (Func<MetadataLookupResult>)(() => new MetadataLookupResult.Failed(category)))),
    ];

    private static IReadOnlyList<EnrichmentFailureCategory> FailureCategories() =>
    [
        .. typeof(EnrichmentFailureCategory)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(EnrichmentFailureCategory))
            .Select(field => field.GetValue(null))
            .OfType<EnrichmentFailureCategory>()
            .OrderBy(category => category.Code, StringComparer.Ordinal),
    ];

    private sealed class StubMetadataProvider : IMetadataProvider
    {
        private readonly Func<MetadataLookupResult> respond;

        private StubMetadataProvider(Func<MetadataLookupResult> respond) => this.respond = respond;

        public int CallCount { get; private set; }

        public MetadataLookup? LastLookup { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public static StubMetadataProvider Returning(MetadataLookupResult result) => new(() => result);

        public static StubMetadataProvider Running(Func<MetadataLookupResult> respond) => new(respond);

        public static StubMetadataProvider Throwing(Exception exception) => new(() => throw exception);

        public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
        {
            CallCount++;
            LastLookup = lookup;
            LastCancellationToken = ct;
            return Task.FromResult(respond());
        }
    }
}
