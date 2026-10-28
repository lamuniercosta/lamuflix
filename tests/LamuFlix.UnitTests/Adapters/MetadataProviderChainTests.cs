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
    private static readonly MetadataLookup Query = new("Blade Runner", null, new ImdbId("tt0083658"));

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

        // act
        var result = await chain.FindAsync(Query, cancellation.Token);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        primary.LastLookup.ShouldBeSameAs(Query);
        secondary.LastLookup.ShouldBeSameAs(Query);
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
