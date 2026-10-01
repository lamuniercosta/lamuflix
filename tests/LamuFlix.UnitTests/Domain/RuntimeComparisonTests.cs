using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class RuntimeComparisonTests
{
    [Fact]
    public void CompareToAndOperators_OrderMinutesAndHandleNull()
    {
        Runtime lower = new Runtime(90);
        Runtime higher = new Runtime(120);
        Runtime? missing = null;
        Runtime? alsoMissing = null;

        lower.CompareTo(higher).ShouldBeLessThan(0);
        higher.CompareTo(lower).ShouldBeGreaterThan(0);
        lower.CompareTo(new Runtime(90)).ShouldBe(0);
        lower.CompareTo(null).ShouldBeGreaterThan(0);
        (lower < higher).ShouldBeTrue();
        (lower <= new Runtime(90)).ShouldBeTrue();
        (higher > lower).ShouldBeTrue();
        (higher >= new Runtime(120)).ShouldBeTrue();
        (missing < lower).ShouldBeTrue();
        (missing <= alsoMissing).ShouldBeTrue();
        (lower > missing).ShouldBeTrue();
        (lower >= missing).ShouldBeTrue();
        (missing < alsoMissing).ShouldBeFalse();
        (missing <= lower).ShouldBeTrue();
        (missing > lower).ShouldBeFalse();
        (missing >= lower).ShouldBeFalse();
        (missing >= alsoMissing).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Comparisons_AgreeWithIntegerOrdering()
    {
        Prop.ForAll<int, int>((left, right) =>
        {
            Runtime lower = new(Math.Abs(left % 10000) + 1);
            Runtime higher = new(Math.Abs(right % 10000) + 1);

            return OrderedComparisonsAgree(lower, higher)
                && NullComparisonsAgree(lower);
        }).QuickCheckThrowOnFailure();
    }

    private static bool OrderedComparisonsAgree(Runtime lower, Runtime higher)
    {
        var comparison = lower.Minutes.CompareTo(higher.Minutes);

        return lower.CompareTo(higher) == comparison
            && (lower < higher) == (comparison < 0)
            && (lower <= higher) == (comparison <= 0)
            && (lower > higher) == (comparison > 0)
            && (lower >= higher) == (comparison >= 0);
    }

    private static bool NullComparisonsAgree(Runtime lower) =>
        MissingComparisonsAgree(lower) && BothMissingComparisonsAgree();

    private static bool MissingComparisonsAgree(Runtime lower)
    {
        Runtime? missing = null;

        return (missing < lower)
            && (lower > missing)
            && !(missing > lower)
            && !(missing >= lower);
    }

    private static bool BothMissingComparisonsAgree()
    {
        Runtime? missing = null;
        Runtime? alsoMissing = null;

        return (missing <= alsoMissing)
            && (missing >= alsoMissing);
    }
}